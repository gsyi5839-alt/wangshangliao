using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Collections.Generic;
using WSLFramework.Utils;

namespace WSLFramework.Services
{
    /// <summary>
    /// 旺商聊 NIM 直连客户端
    /// 直接与旺商聊服务器通信
    /// </summary>
    public class NimDirectClient : IDisposable
    {
        #region 单例
        private static NimDirectClient _instance;
        private static readonly object _lock = new object();
        
        public static NimDirectClient Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                            _instance = new NimDirectClient();
                    }
                }
                return _instance;
            }
        }
        #endregion

        #region 服务器配置
        public static readonly ServerConfig[] SERVERS = new ServerConfig[]
        {
            new ServerConfig { Host = "120.236.198.109", Port = 47437, Name = "长连接服务器", Type = ServerType.PushServer },
            new ServerConfig { Host = "120.233.185.185", Port = 443, Name = "API服务器", Type = ServerType.ApiServer },
            new ServerConfig { Host = "47.245.110.70", Port = 8080, Name = "心跳服务器", Type = ServerType.HeartbeatServer },
        };

        private const int PROTOCOL_VERSION = 1;
        private const uint MAGIC_NUMBER = 0x4E494D00; // "NIM\0"
        #endregion

        #region 私有字段
        private TcpClient _tcpClient;
        private SslStream _sslStream;
        private NetworkStream _networkStream;
        private Stream _activeStream;
        private CancellationTokenSource _cts;
        private Task _receiveTask;
        private int _sequenceId;
        private readonly ConcurrentDictionary<int, TaskCompletionSource<NimResponse>> _pendingRequests;
        private Timer _heartbeatTimer;
        private bool _isConnected;
        private bool _useSsl;
        private readonly JavaScriptSerializer _serializer;

        // 认证信息
        private string _appKey;
        private string _accid;
        private string _token;
        private byte[] _sessionKey;
        private string _activeGroupId;
        #endregion

        #region 公共属性
        public bool IsConnected => _isConnected;
        public bool IsLoggedIn { get; private set; }
        public string CurrentAccid => _accid;
        public string ActiveGroupId => _activeGroupId;
        #endregion

        #region 事件
        public event Action<NimDirectMessage> OnMessageReceived;
        public event Action<Dictionary<string, object>> OnNotification;
        public event Action<Exception> OnError;
        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action OnLoggedIn;
        public event Action<string> OnLog;
        #endregion

        private NimDirectClient()
        {
            _pendingRequests = new ConcurrentDictionary<int, TaskCompletionSource<NimResponse>>();
            _serializer = new JavaScriptSerializer();
        }

        private void Log(string message)
        {
            Logger.Info($"[NimDirect] {message}");
            OnLog?.Invoke(message);
        }

        #region 连接管理

        /// <summary>
        /// 连接到服务器
        /// </summary>
        public async Task<bool> ConnectAsync(string host = null, int port = 0, bool useSsl = true)
        {
            if (_isConnected)
            {
                Log("已经连接");
                return true;
            }

            if (string.IsNullOrEmpty(host))
            {
                var server = SERVERS[0];
                host = server.Host;
                port = server.Port;
            }

            _useSsl = useSsl;

            Log($"正在连接 {host}:{port} (SSL: {useSsl})...");

            try
            {
                _tcpClient = new TcpClient();
                _tcpClient.ReceiveTimeout = 30000;
                _tcpClient.SendTimeout = 10000;
                _tcpClient.NoDelay = true;

                await _tcpClient.ConnectAsync(host, port);
                _networkStream = _tcpClient.GetStream();

                if (useSsl)
                {
                    _sslStream = new SslStream(_networkStream, false, ValidateServerCertificate);
                    await _sslStream.AuthenticateAsClientAsync(host);
                    _activeStream = _sslStream;
                    Log("SSL 握手完成");
                }
                else
                {
                    _activeStream = _networkStream;
                }

                _isConnected = true;
                _cts = new CancellationTokenSource();
                _receiveTask = Task.Run(() => ReceiveLoop(_cts.Token));

                OnConnected?.Invoke();
                StartHeartbeat();

                Log("✓ 连接成功!");
                return true;
            }
            catch (Exception ex)
            {
                Log($"连接失败: {ex.Message}");
                OnError?.Invoke(ex);
                return false;
            }
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        public void Disconnect()
        {
            if (!_isConnected) return;

            _isConnected = false;
            IsLoggedIn = false;
            StopHeartbeat();
            _cts?.Cancel();

            try
            {
                _sslStream?.Close();
                _networkStream?.Close();
                _tcpClient?.Close();
            }
            catch { }

            _sslStream = null;
            _networkStream = null;
            _tcpClient = null;
            _activeStream = null;

            foreach (var kvp in _pendingRequests)
            {
                kvp.Value.TrySetCanceled();
            }
            _pendingRequests.Clear();

            OnDisconnected?.Invoke();
            Log("已断开连接");
        }

        private bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
        {
            return true; // 开发环境接受所有证书
        }

        #endregion

        #region 认证

        /// <summary>
        /// 登录 NIM
        /// </summary>
        public async Task<bool> LoginAsync(string appKey, string accid, string token)
        {
            if (!_isConnected)
            {
                Log("未连接，尝试先连接...");
                var connected = await ConnectAsync();
                if (!connected) return false;
            }

            Log($"登录: accid={accid}");

            _appKey = appKey;
            _accid = accid;
            _token = token;

            var loginRequest = new Dictionary<string, object>
            {
                { "cmd", "login" },
                { "appKey", appKey },
                { "accid", accid },
                { "token", token },
                { "platform", "web" },
                { "version", "9.20.15" },
                { "deviceId", GenerateDeviceId() }
            };

            try
            {
                var response = await SendRequestAsync(loginRequest);
                
                if (response?.Code == 200)
                {
                    if (response.Data != null && response.Data.ContainsKey("sessionKey"))
                    {
                        _sessionKey = Convert.FromBase64String(response.Data["sessionKey"].ToString());
                    }

                    IsLoggedIn = true;
                    OnLoggedIn?.Invoke();
                    Log($"✓ 登录成功! accid={accid}");
                    return true;
                }

                Log($"登录失败: {response?.Message ?? "未知错误"}");
                return false;
            }
            catch (Exception ex)
            {
                Log($"登录异常: {ex.Message}");
                OnError?.Invoke(ex);
                return false;
            }
        }

        /// <summary>
        /// 使用 NIM Token 直接登录
        /// </summary>
        public async Task<bool> LoginWithTokenAsync(string accid, string nimToken)
        {
            // 默认 AppKey (旺商聊云信SDK AppKey)
            string appKey = "45c6af3c98409b18a84451215d0bdd6e";
            return await LoginAsync(appKey, accid, nimToken);
        }
        
        /// <summary>
        /// 从CDPBridge自动获取NIM凭证并登录
        /// 这是移植自主框架的核心功能
        /// </summary>
        public async Task<bool> LoginFromCDPAsync(CDPBridge cdpBridge)
        {
            if (cdpBridge == null)
            {
                Log("CDPBridge 为空，无法获取NIM凭证");
                return false;
            }
            
            if (!cdpBridge.IsConnected)
            {
                Log("CDP 未连接，尝试连接...");
                var cdpConnected = await cdpBridge.ConnectAsync();
                if (!cdpConnected)
                {
                    Log("CDP 连接失败");
                    return false;
                }
            }
            
            try
            {
                Log("正在从CDP获取NIM凭证...");
                
                // 执行 JavaScript 获取 NIM token
                var js = @"
                (function() {
                    var result = { nimId: '', nimToken: '', wwid: '', nickname: '' };
                    try {
                        var managestate = localStorage.getItem('managestate');
                        if (managestate) {
                            var ms = JSON.parse(managestate);
                            if (ms.userInfo) {
                                result.nimId = (ms.userInfo.nimId || '').toString();
                                result.nimToken = ms.userInfo.nimToken || '';
                                result.wwid = (ms.userInfo.accountId || ms.userInfo.uid || '').toString();
                                result.nickname = ms.userInfo.nickName || '';
                            }
                        }
                        // 备用: linestate
                        if (!result.nimId) {
                            var linestate = localStorage.getItem('linestate');
                            if (linestate) {
                                var ls = JSON.parse(linestate);
                                if (ls.userInfo) {
                                    result.nimId = (ls.userInfo.nimId || '').toString();
                                    result.nimToken = ls.userInfo.nimToken || '';
                                    result.wwid = (ls.userInfo.accountId || '').toString();
                                    result.nickname = ls.userInfo.nickName || '';
                                }
                            }
                        }
                    } catch(e) {
                        result.error = e.message;
                    }
                    return JSON.stringify(result);
                })();
                ";
                
                var response = await cdpBridge.EvaluateAsync(js);
                Log($"CDP NIM凭证响应: {response?.Substring(0, Math.Min(200, response?.Length ?? 0))}");
                
                // 解析响应获取NIM凭证
                var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
                var cdpResult = serializer.Deserialize<Dictionary<string, object>>(response);
                
                string nimId = null;
                string nimToken = null;
                
                if (cdpResult != null && cdpResult.ContainsKey("result"))
                {
                    var result = cdpResult["result"] as Dictionary<string, object>;
                    if (result != null && result.ContainsKey("result"))
                    {
                        var innerResult = result["result"] as Dictionary<string, object>;
                        if (innerResult != null && innerResult.ContainsKey("value"))
                        {
                            var valueStr = innerResult["value"]?.ToString();
                            var value = serializer.Deserialize<Dictionary<string, object>>(valueStr);
                            
                            nimId = value?.ContainsKey("nimId") == true ? value["nimId"]?.ToString() : "";
                            nimToken = value?.ContainsKey("nimToken") == true ? value["nimToken"]?.ToString() : "";
                        }
                    }
                }
                
                if (!string.IsNullOrEmpty(nimId) && !string.IsNullOrEmpty(nimToken))
                {
                    Log($"✓ 从CDP获取到NIM凭证: accid={nimId}");
                    return await LoginWithTokenAsync(nimId, nimToken);
                }
                else
                {
                    Log("! 未获取到有效的NIM凭证，请确保旺商聊已登录");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log($"从CDP获取NIM凭证异常: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 获取当前登录状态信息
        /// </summary>
        public NimLoginStatus GetLoginStatus()
        {
            return new NimLoginStatus
            {
                IsConnected = _isConnected,
                IsLoggedIn = IsLoggedIn,
                Accid = _accid,
                ActiveGroupId = _activeGroupId
            };
        }

        #endregion

        #region 消息发送

        /// <summary>
        /// 设置活跃群
        /// </summary>
        public void SetActiveGroup(string groupId)
        {
            _activeGroupId = groupId;
            Log($"设置活跃群: {groupId}");
        }

        /// <summary>
        /// 发送群消息
        /// </summary>
        public async Task<bool> SendTeamMessageAsync(string teamId, string text)
        {
            if (!IsLoggedIn)
            {
                Log("未登录，无法发送消息");
                return false;
            }

            var msg = new Dictionary<string, object>
            {
                { "cmd", "send" },
                { "scene", "team" },
                { "to", teamId },
                { "type", "text" },
                { "body", text },
                { "msgId", GenerateMsgId() }
            };

            Log($"发送群消息到 {teamId}: {text.Substring(0, Math.Min(50, text.Length))}...");
            var response = await SendRequestAsync(msg);
            
            if (response?.Code == 200)
            {
                Log($"✓ 群消息发送成功");
                return true;
            }
            
            Log($"群消息发送失败: {response?.Message}");
            return false;
        }

        /// <summary>
        /// 发送消息到活跃群
        /// </summary>
        public async Task<bool> SendToActiveGroupAsync(string text)
        {
            if (string.IsNullOrEmpty(_activeGroupId))
            {
                Log("未设置活跃群");
                return false;
            }
            return await SendTeamMessageAsync(_activeGroupId, text);
        }

        /// <summary>
        /// 发送私聊消息
        /// </summary>
        public async Task<bool> SendP2PMessageAsync(string to, string text)
        {
            if (!IsLoggedIn)
            {
                Log("未登录，无法发送消息");
                return false;
            }

            var msg = new Dictionary<string, object>
            {
                { "cmd", "send" },
                { "scene", "p2p" },
                { "to", to },
                { "type", "text" },
                { "body", text },
                { "msgId", GenerateMsgId() }
            };

            var response = await SendRequestAsync(msg);
            return response?.Code == 200;
        }

        #endregion

        #region 群组操作

        /// <summary>
        /// 获取群成员
        /// </summary>
        public async Task<List<Dictionary<string, object>>> GetTeamMembersAsync(string teamId)
        {
            var request = new Dictionary<string, object>
            {
                { "cmd", "getTeamMembers" },
                { "teamId", teamId }
            };

            var response = await SendRequestAsync(request);
            if (response?.Data != null && response.Data.ContainsKey("members"))
            {
                return response.Data["members"] as List<Dictionary<string, object>>;
            }
            return null;
        }

        /// <summary>
        /// 禁言群成员
        /// </summary>
        public async Task<bool> MuteTeamMemberAsync(string teamId, string memberId, int seconds)
        {
            var request = new Dictionary<string, object>
            {
                { "cmd", "muteTeamMember" },
                { "teamId", teamId },
                { "memberId", memberId },
                { "duration", seconds }
            };

            var response = await SendRequestAsync(request);
            return response?.Code == 200;
        }

        /// <summary>
        /// 全员禁言/解禁
        /// </summary>
        public async Task<bool> MuteTeamAllAsync(string teamId, bool mute)
        {
            var request = new Dictionary<string, object>
            {
                { "cmd", "muteTeamAll" },
                { "teamId", teamId },
                { "mute", mute }
            };

            var response = await SendRequestAsync(request);
            return response?.Code == 200;
        }

        /// <summary>
        /// 踢出群成员
        /// </summary>
        public async Task<bool> KickTeamMemberAsync(string teamId, string memberId)
        {
            var request = new Dictionary<string, object>
            {
                { "cmd", "kickTeamMember" },
                { "teamId", teamId },
                { "memberId", memberId }
            };

            var response = await SendRequestAsync(request);
            return response?.Code == 200;
        }

        #endregion

        #region 协议实现

        private async Task<NimResponse> SendRequestAsync(Dictionary<string, object> request, int timeoutMs = 30000)
        {
            var seqId = Interlocked.Increment(ref _sequenceId);
            request["seq"] = seqId;

            var tcs = new TaskCompletionSource<NimResponse>();
            _pendingRequests[seqId] = tcs;

            try
            {
                await SendPacketAsync(request);

                using (var cts = new CancellationTokenSource(timeoutMs))
                {
                    cts.Token.Register(() => tcs.TrySetException(new TimeoutException()));
                    return await tcs.Task;
                }
            }
            finally
            {
                _pendingRequests.TryRemove(seqId, out _);
            }
        }

        private async Task SendPacketAsync(Dictionary<string, object> data)
        {
            if (!_isConnected || _activeStream == null)
            {
                throw new InvalidOperationException("未连接");
            }

            var json = _serializer.Serialize(data);
            var payload = Encoding.UTF8.GetBytes(json);

            if (_sessionKey != null && _sessionKey.Length > 0)
            {
                payload = EncryptPayload(payload);
            }

            using (var ms = new MemoryStream())
            {
                var writer = new BinaryWriter(ms);
                
                writer.Write(MAGIC_NUMBER);
                writer.Write((ushort)PROTOCOL_VERSION);
                writer.Write((ushort)(_sessionKey != null ? 1 : 0));
                writer.Write(payload.Length);
                writer.Write(payload);

                var packet = ms.ToArray();
                await _activeStream.WriteAsync(packet, 0, packet.Length);
                await _activeStream.FlushAsync();
            }
        }

        private async Task ReceiveLoop(CancellationToken token)
        {
            var headerBuffer = new byte[12];

            try
            {
                while (!token.IsCancellationRequested && _isConnected)
                {
                    var bytesRead = await ReadExactAsync(_activeStream, headerBuffer, 0, 12, token);
                    if (bytesRead < 12) break;

                    using (var ms = new MemoryStream(headerBuffer))
                    {
                        var reader = new BinaryReader(ms);
                        var magic = reader.ReadUInt32();
                        var version = reader.ReadUInt16();
                        var flags = reader.ReadUInt16();
                        var length = reader.ReadInt32();

                        if (magic != MAGIC_NUMBER)
                        {
                            Log($"无效魔数: {magic:X8}");
                            continue;
                        }

                        var payload = new byte[length];
                        bytesRead = await ReadExactAsync(_activeStream, payload, 0, length, token);
                        if (bytesRead < length) break;

                        if ((flags & 1) != 0 && _sessionKey != null)
                        {
                            payload = DecryptPayload(payload);
                        }

                        var json = Encoding.UTF8.GetString(payload);
                        ProcessMessage(json);
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Log($"接收错误: {ex.Message}");
                OnError?.Invoke(ex);
            }
            finally
            {
                if (_isConnected)
                {
                    Disconnect();
                }
            }
        }

        private void ProcessMessage(string json)
        {
            try
            {
                var obj = _serializer.Deserialize<Dictionary<string, object>>(json);
                var cmd = obj.ContainsKey("cmd") ? obj["cmd"]?.ToString() : "";
                var seq = obj.ContainsKey("seq") ? Convert.ToInt32(obj["seq"]) : 0;

                if (seq > 0 && _pendingRequests.TryGetValue(seq, out var tcs))
                {
                    var response = new NimResponse
                    {
                        Code = obj.ContainsKey("code") ? Convert.ToInt32(obj["code"]) : 0,
                        Message = obj.ContainsKey("msg") ? obj["msg"]?.ToString() : "",
                        Data = obj.ContainsKey("data") ? obj["data"] as Dictionary<string, object> : null
                    };
                    tcs.TrySetResult(response);
                    return;
                }

                switch (cmd)
                {
                    case "msg":
                        var nimMsg = ParseNimMessage(obj);
                        if (nimMsg != null)
                        {
                            OnMessageReceived?.Invoke(nimMsg);
                        }
                        break;

                    case "notify":
                        OnNotification?.Invoke(obj);
                        break;

                    case "pong":
                        break;

                    default:
                        Log($"未知命令: {cmd}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Log($"解析错误: {ex.Message}");
            }
        }

        private NimDirectMessage ParseNimMessage(Dictionary<string, object> obj)
        {
            return new NimDirectMessage
            {
                Scene = obj.ContainsKey("scene") ? obj["scene"]?.ToString() : "",
                From = obj.ContainsKey("from") ? obj["from"]?.ToString() : "",
                To = obj.ContainsKey("to") ? obj["to"]?.ToString() : "",
                Type = obj.ContainsKey("type") ? obj["type"]?.ToString() : "",
                Body = obj.ContainsKey("body") ? obj["body"]?.ToString() : "",
                Time = obj.ContainsKey("time") ? Convert.ToInt64(obj["time"]) : 0,
                MsgId = obj.ContainsKey("msgId") ? obj["msgId"]?.ToString() : ""
            };
        }

        #endregion

        #region 加密/解密

        private byte[] EncryptPayload(byte[] data)
        {
            if (_sessionKey == null || _sessionKey.Length < 32)
                return data;

            var iv = WslCrypto.GenerateIV();
            var encrypted = WslCrypto.EncryptCBC(data, _sessionKey, iv);
            
            // 输出: IV + Encrypted
            var result = new byte[iv.Length + encrypted.Length];
            Array.Copy(iv, 0, result, 0, iv.Length);
            Array.Copy(encrypted, 0, result, iv.Length, encrypted.Length);
            return result;
        }

        private byte[] DecryptPayload(byte[] data)
        {
            if (_sessionKey == null || _sessionKey.Length < 32)
                return data;

            if (data.Length < 16) return data;

            var iv = new byte[16];
            var encrypted = new byte[data.Length - 16];
            Array.Copy(data, 0, iv, 0, 16);
            Array.Copy(data, 16, encrypted, 0, encrypted.Length);

            return WslCrypto.DecryptCBC(encrypted, _sessionKey, iv);
        }

        #endregion

        #region 辅助方法

        private async Task<int> ReadExactAsync(Stream stream, byte[] buffer, int offset, int count, CancellationToken token)
        {
            var totalRead = 0;
            while (totalRead < count)
            {
                var read = await stream.ReadAsync(buffer, offset + totalRead, count - totalRead, token);
                if (read == 0) break;
                totalRead += read;
            }
            return totalRead;
        }

        private void StartHeartbeat()
        {
            _heartbeatTimer = new Timer(async _ =>
            {
                try
                {
                    if (_isConnected)
                    {
                        await SendPacketAsync(new Dictionary<string, object> { { "cmd", "ping" } });
                    }
                }
                catch { }
            }, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
        }

        private void StopHeartbeat()
        {
            _heartbeatTimer?.Dispose();
            _heartbeatTimer = null;
        }

        private string GenerateDeviceId()
        {
            return Guid.NewGuid().ToString("N").Substring(0, 16);
        }

        private string GenerateMsgId()
        {
            return $"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}-{Guid.NewGuid().ToString("N").Substring(0, 8)}";
        }

        #endregion

        public void Dispose()
        {
            Disconnect();
        }
    }

    #region 辅助类

    public class ServerConfig
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string Name { get; set; }
        public ServerType Type { get; set; }
    }

    public enum ServerType
    {
        ApiServer,
        PushServer,
        HeartbeatServer
    }

    public class NimResponse
    {
        public int Code { get; set; }
        public string Message { get; set; }
        public Dictionary<string, object> Data { get; set; }
    }

    public class NimDirectMessage
    {
        public string Scene { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string Type { get; set; }
        public string Body { get; set; }
        public long Time { get; set; }
        public string MsgId { get; set; }
    }
    
    /// <summary>
    /// NIM登录状态信息
    /// </summary>
    public class NimLoginStatus
    {
        public bool IsConnected { get; set; }
        public bool IsLoggedIn { get; set; }
        public string Accid { get; set; }
        public string ActiveGroupId { get; set; }
    }

    #endregion
}
