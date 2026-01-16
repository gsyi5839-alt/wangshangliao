using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace WangShangLiaoBot.Services.PureClient
{
    /// <summary>
    /// 旺商聊NIM直连客户端
    /// 直接与旺商聊服务器通信，绕过xclient
    /// </summary>
    public class NimDirectClient : IDisposable
    {
        #region 服务器配置

        /// <summary>
        /// 发现的服务器架构:
        /// 1. API服务器 (HTTPS): 120.233.185.185:443 - 认证、配置获取
        /// 2. 长连接服务器: 120.236.198.109:47437 - 消息推送
        /// 3. 心跳服务器 (HTTP): 47.245.110.70:8080 - 心跳/状态
        /// </summary>
        public static readonly ServerInfo[] SERVERS = new ServerInfo[]
        {
            new ServerInfo { Host = "120.236.198.109", Port = 47437, Name = "长连接服务器", Type = ServerType.PushServer },
            new ServerInfo { Host = "120.233.185.185", Port = 443, Name = "API服务器", Type = ServerType.ApiServer },
            new ServerInfo { Host = "47.245.110.70", Port = 8080, Name = "心跳服务器", Type = ServerType.HeartbeatServer },
        };

        /// <summary>
        /// xclient本地端口
        /// </summary>
        public const int XCLIENT_LOCAL_PORT = 21303;  // JSON-RPC服务
        public const int XCLIENT_HTTP_PORT = 21308;   // HTTP服务

        // 协议版本
        private const int PROTOCOL_VERSION = 1;
        
        // 魔数 (推测值，需要通过抓包验证)
        private const uint MAGIC_NUMBER = 0x4E494D00; // "NIM\0"

        #endregion

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

        // 认证信息
        private string _appKey;
        private string _accid;
        private string _token;
        private byte[] _sessionKey;

        // 事件
        public event Action<NimMessage> OnMessageReceived;
        public event Action<JObject> OnNotification;
        public event Action<Exception> OnError;
        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action OnLoggedIn;

        public bool IsConnected => _isConnected;

        public NimDirectClient()
        {
            _pendingRequests = new ConcurrentDictionary<int, TaskCompletionSource<NimResponse>>();
        }

        #region 连接管理

        /// <summary>
        /// 连接到服务器
        /// </summary>
        public async Task ConnectAsync(string host = null, int port = 0, bool useSsl = true)
        {
            if (_isConnected)
            {
                throw new InvalidOperationException("Already connected");
            }

            // 使用默认服务器
            if (string.IsNullOrEmpty(host))
            {
                var server = SERVERS[0];
                host = server.Host;
                port = server.Port;
            }

            _useSsl = useSsl;

            Console.WriteLine($"[NimDirectClient] Connecting to {host}:{port} (SSL: {useSsl})...");

            _tcpClient = new TcpClient();
            _tcpClient.ReceiveTimeout = 30000;
            _tcpClient.SendTimeout = 10000;
            _tcpClient.NoDelay = true;

            await _tcpClient.ConnectAsync(host, port);
            _networkStream = _tcpClient.GetStream();

            if (useSsl)
            {
                // SSL握手
                _sslStream = new SslStream(_networkStream, false, ValidateServerCertificate);
                await _sslStream.AuthenticateAsClientAsync(host);
                _activeStream = _sslStream;
                Console.WriteLine("[NimDirectClient] SSL handshake completed");
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

            Console.WriteLine("[NimDirectClient] Connected!");
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        public void Disconnect()
        {
            if (!_isConnected) return;

            _isConnected = false;
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

            // 取消所有等待的请求
            foreach (var kvp in _pendingRequests)
            {
                kvp.Value.TrySetCanceled();
            }
            _pendingRequests.Clear();

            OnDisconnected?.Invoke();
            Console.WriteLine("[NimDirectClient] Disconnected");
        }

        private bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
        {
            // 开发阶段接受所有证书
            // 生产环境应该验证证书
            if (errors != SslPolicyErrors.None)
            {
                Console.WriteLine($"[NimDirectClient] SSL Certificate warning: {errors}");
            }
            return true;
        }

        #endregion

        #region 认证

        /// <summary>
        /// 登录
        /// </summary>
        public async Task<bool> LoginAsync(string appKey, string accid, string token)
        {
            Console.WriteLine($"[NimDirectClient] Login: appKey={appKey.Substring(0, 8)}..., accid={accid}");

            _appKey = appKey;
            _accid = accid;
            _token = token;

            // 构建登录请求
            var loginRequest = new JObject
            {
                ["cmd"] = "login",
                ["appKey"] = appKey,
                ["accid"] = accid,
                ["token"] = token,
                ["platform"] = "web",
                ["version"] = "9.20.15",
                ["deviceId"] = GenerateDeviceId()
            };

            try
            {
                var response = await SendRequestAsync(loginRequest);
                
                if (response?.Code == 200)
                {
                    // 提取session key
                    if (response.Data?["sessionKey"] != null)
                    {
                        _sessionKey = Convert.FromBase64String(response.Data["sessionKey"].ToString());
                    }

                    OnLoggedIn?.Invoke();
                    Console.WriteLine("[NimDirectClient] Login successful!");
                    return true;
                }

                Console.WriteLine($"[NimDirectClient] Login failed: {response?.Message ?? "Unknown error"}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NimDirectClient] Login error: {ex.Message}");
                OnError?.Invoke(ex);
                return false;
            }
        }

        #endregion

        #region 消息发送

        /// <summary>
        /// 发送群消息
        /// </summary>
        public async Task<bool> SendTeamMessageAsync(string teamId, string text)
        {
            var msg = new JObject
            {
                ["cmd"] = "send",
                ["scene"] = "team",
                ["to"] = teamId,
                ["type"] = "text",
                ["body"] = text,
                ["msgId"] = GenerateMsgId()
            };

            var response = await SendRequestAsync(msg);
            return response?.Code == 200;
        }

        /// <summary>
        /// 发送私聊消息
        /// </summary>
        public async Task<bool> SendP2PMessageAsync(string to, string text)
        {
            var msg = new JObject
            {
                ["cmd"] = "send",
                ["scene"] = "p2p",
                ["to"] = to,
                ["type"] = "text",
                ["body"] = text,
                ["msgId"] = GenerateMsgId()
            };

            var response = await SendRequestAsync(msg);
            return response?.Code == 200;
        }

        #endregion

        #region 群组操作

        /// <summary>
        /// 获取群成员
        /// </summary>
        public async Task<JArray> GetTeamMembersAsync(string teamId)
        {
            var request = new JObject
            {
                ["cmd"] = "getTeamMembers",
                ["teamId"] = teamId
            };

            var response = await SendRequestAsync(request);
            return response?.Data?["members"] as JArray;
        }

        /// <summary>
        /// 禁言群成员
        /// </summary>
        public async Task<bool> MuteTeamMemberAsync(string teamId, string memberId, int seconds)
        {
            var request = new JObject
            {
                ["cmd"] = "muteTeamMember",
                ["teamId"] = teamId,
                ["memberId"] = memberId,
                ["duration"] = seconds
            };

            var response = await SendRequestAsync(request);
            return response?.Code == 200;
        }

        /// <summary>
        /// 踢出群成员
        /// </summary>
        public async Task<bool> KickTeamMemberAsync(string teamId, string memberId)
        {
            var request = new JObject
            {
                ["cmd"] = "kickTeamMember",
                ["teamId"] = teamId,
                ["memberId"] = memberId
            };

            var response = await SendRequestAsync(request);
            return response?.Code == 200;
        }

        #endregion

        #region 协议实现

        /// <summary>
        /// 发送请求并等待响应
        /// </summary>
        private async Task<NimResponse> SendRequestAsync(JObject request, int timeoutMs = 30000)
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

        /// <summary>
        /// 发送数据包
        /// </summary>
        private async Task SendPacketAsync(JObject data)
        {
            if (!_isConnected || _activeStream == null)
            {
                throw new InvalidOperationException("Not connected");
            }

            var json = data.ToString(Formatting.None);
            var payload = Encoding.UTF8.GetBytes(json);

            // 可选: 加密payload
            if (_sessionKey != null && _sessionKey.Length > 0)
            {
                payload = EncryptPayload(payload);
            }

            // 构建数据包: [Magic(4)] [Version(2)] [Flags(2)] [Length(4)] [Payload]
            using (var ms = new MemoryStream())
            {
                var writer = new BinaryWriter(ms);
                
                writer.Write(MAGIC_NUMBER);         // 4 bytes magic
                writer.Write((ushort)PROTOCOL_VERSION); // 2 bytes version
                writer.Write((ushort)(_sessionKey != null ? 1 : 0)); // 2 bytes flags (1 = encrypted)
                writer.Write(payload.Length);       // 4 bytes length
                writer.Write(payload);              // payload

                var packet = ms.ToArray();
                await _activeStream.WriteAsync(packet, 0, packet.Length);
                await _activeStream.FlushAsync();
            }
        }

        /// <summary>
        /// 接收循环
        /// </summary>
        private async Task ReceiveLoop(CancellationToken token)
        {
            var headerBuffer = new byte[12]; // Magic(4) + Version(2) + Flags(2) + Length(4)

            try
            {
                while (!token.IsCancellationRequested && _isConnected)
                {
                    // 读取头部
                    var bytesRead = await ReadExactAsync(_activeStream, headerBuffer, 0, 12, token);
                    if (bytesRead < 12) break;

                    // 解析头部
                    using (var ms = new MemoryStream(headerBuffer))
                    {
                        var reader = new BinaryReader(ms);
                        var magic = reader.ReadUInt32();
                        var version = reader.ReadUInt16();
                        var flags = reader.ReadUInt16();
                        var length = reader.ReadInt32();

                        // 验证魔数
                        if (magic != MAGIC_NUMBER)
                        {
                            Console.WriteLine($"[NimDirectClient] Invalid magic: {magic:X8}");
                            // 尝试重新同步
                            continue;
                        }

                        // 读取payload
                        var payload = new byte[length];
                        bytesRead = await ReadExactAsync(_activeStream, payload, 0, length, token);
                        if (bytesRead < length) break;

                        // 解密
                        if ((flags & 1) != 0 && _sessionKey != null)
                        {
                            payload = DecryptPayload(payload);
                        }

                        // 解析JSON
                        var json = Encoding.UTF8.GetString(payload);
                        ProcessMessage(json);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 正常取消
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NimDirectClient] Receive error: {ex.Message}");
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

        /// <summary>
        /// 处理接收到的消息
        /// </summary>
        private void ProcessMessage(string json)
        {
            try
            {
                var obj = JObject.Parse(json);
                var cmd = obj["cmd"]?.ToString();
                var seq = obj["seq"]?.Value<int>() ?? 0;

                // 响应
                if (seq > 0 && _pendingRequests.TryGetValue(seq, out var tcs))
                {
                    var response = new NimResponse
                    {
                        Code = obj["code"]?.Value<int>() ?? 0,
                        Message = obj["msg"]?.ToString(),
                        Data = obj["data"] as JObject
                    };
                    tcs.TrySetResult(response);
                    return;
                }

                // 推送消息
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
                        // 心跳响应
                        break;

                    default:
                        Console.WriteLine($"[NimDirectClient] Unknown cmd: {cmd}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NimDirectClient] Parse error: {ex.Message}");
            }
        }

        private NimMessage ParseNimMessage(JObject obj)
        {
            return new NimMessage
            {
                Scene = obj["scene"]?.ToString(),
                From = obj["from"]?.ToString(),
                To = obj["to"]?.ToString(),
                Type = obj["type"]?.ToString(),
                Body = obj["body"]?.ToString(),
                Time = obj["time"]?.Value<long>() ?? 0,
                MsgId = obj["msgId"]?.ToString()
            };
        }

        #endregion

        #region 加密/解密

        private byte[] EncryptPayload(byte[] data)
        {
            if (_sessionKey == null || _sessionKey.Length < 32)
                return data;

            var nonce = WslCrypto.GenerateNonce();
            return WslCrypto.Encrypt(data, _sessionKey, nonce);
        }

        private byte[] DecryptPayload(byte[] data)
        {
            if (_sessionKey == null || _sessionKey.Length < 32)
                return data;

            return WslCrypto.Decrypt(data, _sessionKey);
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
                        await SendPacketAsync(new JObject { ["cmd"] = "ping" });
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

    public class ServerInfo
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string Name { get; set; }
        public ServerType Type { get; set; }
    }

    public enum ServerType
    {
        ApiServer,        // HTTPS API服务器 (认证/配置)
        PushServer,       // 长连接服务器 (消息推送)
        HeartbeatServer   // HTTP心跳服务器
    }

    public class NimResponse
    {
        public int Code { get; set; }
        public string Message { get; set; }
        public JObject Data { get; set; }
    }

    public class NimMessage
    {
        public string Scene { get; set; }  // "p2p" or "team"
        public string From { get; set; }
        public string To { get; set; }
        public string Type { get; set; }   // "text", "image", etc.
        public string Body { get; set; }
        public long Time { get; set; }
        public string MsgId { get; set; }
    }

    #endregion
}
