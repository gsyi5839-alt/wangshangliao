using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Web.Script.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace WangShangLiaoBot.Services.DirectConnection
{
    /// <summary>
    /// XClient直连服务 - 直接与旺商聊xclient.exe通信
    /// 基于逆向分析的通信协议
    /// 
    /// 通信架构:
    /// 旺商聊Electron App ←→ xclient.exe(21303) ←→ 云信服务器
    /// 本服务直接与xclient通信，绕过Electron层
    /// </summary>
    public sealed class XClientService : IDisposable
    {
        private static XClientService _instance;
        public static XClientService Instance => _instance ?? (_instance = new XClientService());

        // 通信端口
        private const int XCLIENT_PORT = 21303;
        private const string XCLIENT_HOST = "127.0.0.1";

        // 连接状态
        private TcpClient _tcpClient;
        private NetworkStream _stream;
        private StreamReader _reader;
        private StreamWriter _writer;
        private bool _isConnected;
        private CancellationTokenSource _cts;

        // 请求队列
        private readonly ConcurrentDictionary<string, TaskCompletionSource<XClientResponse>> _pendingRequests 
            = new ConcurrentDictionary<string, TaskCompletionSource<XClientResponse>>();

        // 事件
        public event Action<string> OnLog;
        public event Action<bool> OnConnectionStateChanged;
        public event Action<XClientMessage> OnMessageReceived;
        public event Action<XClientMessage> OnGroupMessage;
        public event Action<XClientMessage> OnPrivateMessage;

        public bool IsConnected => _isConnected;

        private XClientService()
        {
        }

        #region 连接管理

        /// <summary>
        /// 连接到xclient服务
        /// </summary>
        public async Task<bool> ConnectAsync()
        {
            if (_isConnected)
            {
                Log("[XClient] 已连接");
                return true;
            }

            try
            {
                _tcpClient = new TcpClient();
                await _tcpClient.ConnectAsync(XCLIENT_HOST, XCLIENT_PORT);

                _stream = _tcpClient.GetStream();
                _reader = new StreamReader(_stream, Encoding.UTF8);
                _writer = new StreamWriter(_stream, Encoding.UTF8) { AutoFlush = true };

                _isConnected = true;
                _cts = new CancellationTokenSource();

                // 启动消息接收循环
                _ = Task.Run(() => ReceiveLoopAsync(_cts.Token));

                Log($"[XClient] 连接成功 {XCLIENT_HOST}:{XCLIENT_PORT}");
                OnConnectionStateChanged?.Invoke(true);

                return true;
            }
            catch (Exception ex)
            {
                Log($"[XClient] 连接失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        public void Disconnect()
        {
            _cts?.Cancel();
            _reader?.Dispose();
            _writer?.Dispose();
            _stream?.Dispose();
            _tcpClient?.Dispose();

            _isConnected = false;
            Log("[XClient] 已断开连接");
            OnConnectionStateChanged?.Invoke(false);
        }

        /// <summary>
        /// 消息接收循环
        /// </summary>
        private async Task ReceiveLoopAsync(CancellationToken ct)
        {
            var buffer = new byte[65536];
            var sb = new StringBuilder();

            while (!ct.IsCancellationRequested && _isConnected)
            {
                try
                {
                    var bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length, ct);
                    if (bytesRead == 0)
                    {
                        // 连接断开
                        break;
                    }

                    var data = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    sb.Append(data);

                    // 尝试解析完整JSON消息
                    var content = sb.ToString();
                    if (TryParseMessages(content, out var messages, out var remaining))
                    {
                        sb.Clear();
                        if (!string.IsNullOrEmpty(remaining))
                        {
                            sb.Append(remaining);
                        }

                        foreach (var msg in messages)
                        {
                            ProcessMessage(msg);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Log($"[XClient] 接收错误: {ex.Message}");
                    await Task.Delay(1000, ct);
                }
            }

            _isConnected = false;
            OnConnectionStateChanged?.Invoke(false);
        }

        private bool TryParseMessages(string content, out XClientMessage[] messages, out string remaining)
        {
            messages = Array.Empty<XClientMessage>();
            remaining = content;

            var list = new System.Collections.Generic.List<XClientMessage>();
            var startIdx = 0;
            var braceCount = 0;
            var inString = false;
            var escape = false;

            for (int i = 0; i < content.Length; i++)
            {
                var c = content[i];

                if (escape)
                {
                    escape = false;
                    continue;
                }

                if (c == '\\')
                {
                    escape = true;
                    continue;
                }

                if (c == '"')
                {
                    inString = !inString;
                    continue;
                }

                if (inString) continue;

                if (c == '{')
                {
                    if (braceCount == 0) startIdx = i;
                    braceCount++;
                }
                else if (c == '}')
                {
                    braceCount--;
                    if (braceCount == 0)
                    {
                        var json = content.Substring(startIdx, i - startIdx + 1);
                        try
                        {
                            var msg = new JavaScriptSerializer().Deserialize<XClientMessage>(json);
                            if (msg != null) list.Add(msg);
                        }
                        catch { }
                        startIdx = i + 1;
                    }
                }
            }

            if (list.Count > 0)
            {
                messages = list.ToArray();
                remaining = braceCount > 0 ? content.Substring(startIdx) : "";
                return true;
            }

            return false;
        }

        private void ProcessMessage(XClientMessage msg)
        {
            try
            {
                // 检查是否为响应
                if (!string.IsNullOrEmpty(msg.RequestId) && 
                    _pendingRequests.TryRemove(msg.RequestId, out var tcs))
                {
                    tcs.TrySetResult(new XClientResponse { Success = true, Data = msg.Data });
                    return;
                }

                // 触发消息事件
                OnMessageReceived?.Invoke(msg);

                // 按类型分发
                switch (msg.Type?.ToLower())
                {
                    case "groupmsg":
                    case "teammsg":
                        OnGroupMessage?.Invoke(msg);
                        break;
                    case "privatemsg":
                    case "p2pmsg":
                        OnPrivateMessage?.Invoke(msg);
                        break;
                }
            }
            catch (Exception ex)
            {
                Log($"[XClient] 处理消息错误: {ex.Message}");
            }
        }

        #endregion

        #region 消息发送

        /// <summary>
        /// 发送请求并等待响应
        /// </summary>
        public async Task<XClientResponse> SendRequestAsync(string type, object data, int timeoutMs = 5000)
        {
            if (!_isConnected)
            {
                return new XClientResponse { Success = false, Error = "未连接" };
            }

            var requestId = Guid.NewGuid().ToString("N");
            var tcs = new TaskCompletionSource<XClientResponse>();
            _pendingRequests[requestId] = tcs;

            try
            {
                var request = new
                {
                    type,
                    requestId,
                    data,
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                };

                var json = new JavaScriptSerializer().Serialize(request);
                await _writer.WriteLineAsync(json);

                using var cts = new CancellationTokenSource(timeoutMs);
                cts.Token.Register(() => tcs.TrySetResult(new XClientResponse { Success = false, Error = "超时" }));

                return await tcs.Task;
            }
            catch (Exception ex)
            {
                return new XClientResponse { Success = false, Error = ex.Message };
            }
            finally
            {
                _pendingRequests.TryRemove(requestId, out _);
            }
        }

        /// <summary>
        /// 发送消息（不等待响应）
        /// </summary>
        public async Task SendAsync(string type, object data)
        {
            if (!_isConnected) return;

            try
            {
                var message = new { type, data, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() };
                var json = new JavaScriptSerializer().Serialize(message);
                await _writer.WriteLineAsync(json);
            }
            catch (Exception ex)
            {
                Log($"[XClient] 发送失败: {ex.Message}");
            }
        }

        #endregion

        #region 业务API

        /// <summary>
        /// 发送群消息
        /// </summary>
        public async Task<bool> SendGroupMessageAsync(string teamId, string text)
        {
            var response = await SendRequestAsync("sendGroupMsg", new
            {
                scene = "team",
                to = teamId,
                text
            });

            return response.Success;
        }

        /// <summary>
        /// 发送私聊消息
        /// </summary>
        public async Task<bool> SendPrivateMessageAsync(string userId, string text)
        {
            var response = await SendRequestAsync("sendP2PMsg", new
            {
                scene = "p2p",
                to = userId,
                text
            });

            return response.Success;
        }

        /// <summary>
        /// 获取群成员列表
        /// </summary>
        public async Task<string> GetGroupMembersAsync(string teamId)
        {
            var response = await SendRequestAsync("getTeamMembers", new { teamId });
            return response.Success ? response.Data?.ToString() : null;
        }

        /// <summary>
        /// 禁言群成员
        /// </summary>
        public async Task<bool> MuteMemberAsync(string teamId, string userId, int minutes)
        {
            var response = await SendRequestAsync("muteTeamMember", new
            {
                teamId,
                account = userId,
                mute = minutes > 0,
                muteTime = minutes * 60
            });

            return response.Success;
        }

        /// <summary>
        /// 踢出群成员
        /// </summary>
        public async Task<bool> KickMemberAsync(string teamId, string userId)
        {
            var response = await SendRequestAsync("leaveTeam", new
            {
                teamId,
                accounts = new[] { userId }
            });

            return response.Success;
        }

        /// <summary>
        /// 撤回消息
        /// </summary>
        public async Task<bool> RecallMessageAsync(string teamId, string msgId)
        {
            var response = await SendRequestAsync("recallMsg", new { teamId, msgId });
            return response.Success;
        }

        #endregion

        private void Log(string message)
        {
            OnLog?.Invoke(message);
            Utils.Logger.Info(message);
        }

        public void Dispose()
        {
            Disconnect();
        }
    }

    #region 消息模型

    public class XClientMessage
    {
        public string Type { get; set; }
        public string RequestId { get; set; }
        public string Scene { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string Text { get; set; }
        public object Data { get; set; }
        public long Timestamp { get; set; }
    }

    public class XClientResponse
    {
        public bool Success { get; set; }
        public string Error { get; set; }
        public object Data { get; set; }
    }

    #endregion
}
