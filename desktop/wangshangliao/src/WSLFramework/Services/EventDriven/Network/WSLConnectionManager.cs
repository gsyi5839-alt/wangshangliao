using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text;
using System.Web.Script.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace WSLFramework.Services.EventDriven.Network
{
    /// <summary>
    /// 连接状态
    /// </summary>
    public enum ConnectionState
    {
        Disconnected,
        Connecting,
        Connected,
        Authenticating,
        Authenticated,
        Reconnecting
    }

    /// <summary>
    /// 连接配置
    /// </summary>
    public class WSLConnectionConfig
    {
        public string Host { get; set; } = "127.0.0.1";
        public int Port { get; set; } = 8899;
        public string Account { get; set; }
        public string Token { get; set; }
        public int HeartbeatIntervalMs { get; set; } = 30000;
        public int ReconnectDelayMs { get; set; } = 5000;
        public int MaxReconnectAttempts { get; set; } = 10;
        public int ConnectionTimeoutMs { get; set; } = 10000;
    }

    /// <summary>
    /// 消息事件参数
    /// </summary>
    public class MessageReceivedEventArgs : EventArgs
    {
        public string MessageId { get; set; }
        public string FromId { get; set; }
        public string ToId { get; set; }
        public string SenderNick { get; set; }
        public string Content { get; set; }
        public bool IsGroup { get; set; }
        public long Timestamp { get; set; }
        public string RawData { get; set; }
    }

    /// <summary>
    /// 连接管理器 - 替代CDP桥接
    /// 针对 .NET Framework 4.7.2 兼容
    /// </summary>
    public class WSLConnectionManager : IDisposable
    {
        #region 事件

        public event Action<ConnectionState, string> OnStateChanged;
        public event Action<MessageReceivedEventArgs> OnGroupMessageReceived;
        public event Action<MessageReceivedEventArgs> OnPrivateMessageReceived;
        public event Action<string, string> OnSystemNotify;
        public event Action<uint, string> OnApiResponse;
        public event Action<string> OnLog;

        #endregion

        #region 字段

        private WSLPacketListener _listener;
        private WSLConnectionConfig _config;
        private ConnectionState _state = ConnectionState.Disconnected;
        private int _reconnectAttempts = 0;
        private bool _isDisposed = false;
        private CancellationTokenSource _reconnectCts;
        private readonly JavaScriptSerializer _serializer = new JavaScriptSerializer();

        private readonly ConcurrentDictionary<uint, TaskCompletionSource<string>> _pendingRequests 
            = new ConcurrentDictionary<uint, TaskCompletionSource<string>>();

        #endregion

        #region 属性

        public ConnectionState State => _state;
        public bool IsConnected => _state == ConnectionState.Connected || _state == ConnectionState.Authenticated;
        public WSLConnectionConfig Config => _config;

        #endregion

        #region 构造函数

        public WSLConnectionManager()
        {
            _config = new WSLConnectionConfig();
        }

        public WSLConnectionManager(WSLConnectionConfig config)
        {
            _config = config ?? new WSLConnectionConfig();
        }

        #endregion

        #region 连接方法

        public async Task<bool> StartAsync(WSLConnectionConfig config = null)
        {
            if (config != null)
            {
                _config = config;
            }

            if (_state != ConnectionState.Disconnected)
            {
                Log("已经在连接中或已连接");
                return false;
            }

            return await ConnectInternalAsync();
        }

        private async Task<bool> ConnectInternalAsync()
        {
            try
            {
                SetState(ConnectionState.Connecting, "正在连接...");

                _listener = new WSLPacketListener(_config.HeartbeatIntervalMs);
                _listener.OnPacketReceived += HandlePacket;
                _listener.OnDisconnected += HandleDisconnect;
                _listener.OnError += HandleError;
                _listener.OnLog += msg => Log(msg);

                bool connected = await _listener.ConnectAndStartAsync(_config.Host, _config.Port);
                
                if (!connected)
                {
                    SetState(ConnectionState.Disconnected, "连接失败");
                    return false;
                }

                SetState(ConnectionState.Connected, "已连接");
                _reconnectAttempts = 0;

                if (!string.IsNullOrEmpty(_config.Account) && !string.IsNullOrEmpty(_config.Token))
                {
                    await AuthenticateAsync();
                }

                return true;
            }
            catch (Exception e)
            {
                Log($"连接异常: {e.Message}");
                SetState(ConnectionState.Disconnected, $"连接异常: {e.Message}");
                return false;
            }
        }

        public async Task<bool> AuthenticateAsync(string account = null, string token = null)
        {
            if (_listener == null || !_listener.Connected)
            {
                Log("未连接，无法认证");
                return false;
            }

            if (!string.IsNullOrEmpty(account)) _config.Account = account;
            if (!string.IsNullOrEmpty(token)) _config.Token = token;

            SetState(ConnectionState.Authenticating, "正在认证...");

            bool result = await _listener.LoginAsync(_config.Account, _config.Token);
            
            if (result)
            {
                SetState(ConnectionState.Authenticated, "认证成功");
            }
            else
            {
                SetState(ConnectionState.Connected, "认证失败");
            }

            return result;
        }

        private readonly object _stopLock = new object();
        private bool _isStopping = false;

        public void Stop()
        {
            lock (_stopLock)
            {
                if (_isStopping) return;
                _isStopping = true;
            }

            try
            {
                // 先取消重连
                try
                {
                    _reconnectCts?.Cancel();
                    _reconnectCts?.Dispose();
                    _reconnectCts = null;
                }
                catch { }
                
                // 关闭监听器
                var listener = _listener;
                _listener = null;
                if (listener != null)
                {
                    listener.Close();
                }
                
                SetState(ConnectionState.Disconnected, "已停止");
            }
            finally
            {
                lock (_stopLock)
                {
                    _isStopping = false;
                }
            }
        }

        #endregion

        #region 发送方法

        public async Task<bool> SendGroupMessageAsync(string groupId, string content)
        {
            if (!IsConnected || _listener == null)
            {
                Log("未连接，无法发送消息");
                return false;
            }

            return await _listener.SendMessageAsync(groupId, content, true);
        }

        public async Task<bool> SendPrivateMessageAsync(string userId, string content)
        {
            if (!IsConnected || _listener == null)
            {
                Log("未连接，无法发送消息");
                return false;
            }

            return await _listener.SendMessageAsync(userId, content, false);
        }

        public async Task<string> SendApiRequestAsync(string apiName, string parameters, int timeoutMs = 5000)
        {
            if (!IsConnected || _listener == null)
            {
                throw new InvalidOperationException("未连接");
            }

            bool sent = await _listener.SendApiRequestAsync(apiName, parameters);
            if (!sent)
            {
                throw new InvalidOperationException("发送失败");
            }

            return null;
        }

        #endregion

        #region 数据包处理

        private void HandlePacket(WSLPacket packet)
        {
            try
            {
                switch (packet.Type)
                {
                    case WSLPacketType.LoginAck:
                        HandleLoginAck(packet);
                        break;

                    case WSLPacketType.GroupMessage:
                        HandleGroupMessage(packet);
                        break;

                    case WSLPacketType.PrivateMessage:
                        HandlePrivateMessage(packet);
                        break;

                    case WSLPacketType.SystemNotify:
                        HandleSystemNotify(packet);
                        break;

                    case WSLPacketType.ApiResponse:
                        HandleApiResponse(packet);
                        break;

                    case WSLPacketType.Error:
                        HandleErrorPacket(packet);
                        break;

                    default:
                        Log($"收到未知类型数据包: {packet.Type}");
                        break;
                }
            }
            catch (Exception e)
            {
                Log($"处理数据包异常: {e.Message}");
            }
        }

        private void HandleLoginAck(WSLPacket packet)
        {
            string payload = packet.GetPayloadString();
            Log($"登录响应: {payload}");

            try
            {
                var dict = _serializer.Deserialize<Dictionary<string, object>>(payload);
                if (dict != null && dict.ContainsKey("success") && (bool)dict["success"])
                {
                    SetState(ConnectionState.Authenticated, "登录成功");
                }
                else
                {
                    string msg = dict != null && dict.ContainsKey("message") ? dict["message"]?.ToString() : "登录失败";
                    SetState(ConnectionState.Connected, msg);
                }
            }
            catch
            {
                // 简单处理
            }
        }

        private void HandleGroupMessage(WSLPacket packet)
        {
            string payload = packet.GetPayloadString();
            Log($"收到群消息: {payload}");

            try
            {
                var dict = _serializer.Deserialize<Dictionary<string, object>>(payload);
                if (dict == null) return;

                var args = new MessageReceivedEventArgs
                {
                    IsGroup = true,
                    RawData = payload,
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                };

                if (dict.ContainsKey("messageId"))
                    args.MessageId = dict["messageId"]?.ToString();
                if (dict.ContainsKey("fromId"))
                    args.FromId = dict["fromId"]?.ToString();
                if (dict.ContainsKey("groupId"))
                    args.ToId = dict["groupId"]?.ToString();
                if (dict.ContainsKey("content"))
                    args.Content = dict["content"]?.ToString();
                if (dict.ContainsKey("senderNick"))
                    args.SenderNick = dict["senderNick"]?.ToString();
                if (dict.ContainsKey("timestamp"))
                {
                    long ts;
                    if (long.TryParse(dict["timestamp"]?.ToString(), out ts))
                        args.Timestamp = ts;
                }

                OnGroupMessageReceived?.Invoke(args);
            }
            catch (Exception e)
            {
                Log($"解析群消息失败: {e.Message}");
            }
        }

        private void HandlePrivateMessage(WSLPacket packet)
        {
            string payload = packet.GetPayloadString();
            Log($"收到私聊消息: {payload}");

            try
            {
                var dict = _serializer.Deserialize<Dictionary<string, object>>(payload);
                if (dict == null) return;

                var args = new MessageReceivedEventArgs
                {
                    IsGroup = false,
                    RawData = payload,
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                };

                if (dict.ContainsKey("messageId"))
                    args.MessageId = dict["messageId"]?.ToString();
                if (dict.ContainsKey("fromId"))
                    args.FromId = dict["fromId"]?.ToString();
                if (dict.ContainsKey("toId"))
                    args.ToId = dict["toId"]?.ToString();
                if (dict.ContainsKey("content"))
                    args.Content = dict["content"]?.ToString();
                if (dict.ContainsKey("senderNick"))
                    args.SenderNick = dict["senderNick"]?.ToString();
                if (dict.ContainsKey("timestamp"))
                {
                    long ts;
                    if (long.TryParse(dict["timestamp"]?.ToString(), out ts))
                        args.Timestamp = ts;
                }

                OnPrivateMessageReceived?.Invoke(args);
            }
            catch (Exception e)
            {
                Log($"解析私聊消息失败: {e.Message}");
            }
        }

        private void HandleSystemNotify(WSLPacket packet)
        {
            string payload = packet.GetPayloadString();
            Log($"收到系统通知: {payload}");

            try
            {
                var dict = _serializer.Deserialize<Dictionary<string, object>>(payload);
                string type = dict != null && dict.ContainsKey("type") ? dict["type"]?.ToString() : "unknown";
                string data = dict != null && dict.ContainsKey("data") ? _serializer.Serialize(dict["data"]) : payload;
                OnSystemNotify?.Invoke(type, data);
            }
            catch
            {
                OnSystemNotify?.Invoke("raw", payload);
            }
        }

        private void HandleApiResponse(WSLPacket packet)
        {
            string payload = packet.GetPayloadString();
            Log($"收到API响应: {payload}");

            OnApiResponse?.Invoke(packet.Sequence, payload);

            TaskCompletionSource<string> tcs;
            if (_pendingRequests.TryRemove(packet.Sequence, out tcs))
            {
                tcs.TrySetResult(payload);
            }
        }

        private void HandleErrorPacket(WSLPacket packet)
        {
            string payload = packet.GetPayloadString();
            Log($"收到错误包: {payload}");
        }

        #endregion

        #region 断开/重连处理

        private void HandleDisconnect(string reason)
        {
            Log($"连接断开: {reason}");
            
            // BUG修复: 检查是否正在停止或已销毁
            if (_isDisposed || _isStopping)
            {
                SetState(ConnectionState.Disconnected, "已停止");
                return;
            }

            StartReconnect();
        }

        private void HandleError(Exception e, byte[] data)
        {
            Log($"网络错误: {e.Message}");
        }

        /// <summary>
        /// 启动重连 - 内部方法，异步执行
        /// </summary>
        private void StartReconnect()
        {
            // BUG修复: 使用 Task.Run 而不是 async void，确保异常被正确捕获
            Task.Run(async () =>
            {
                try
                {
                    await ReconnectLoopAsync();
                }
                catch (Exception ex)
                {
                    Log($"重连异常: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// 重连循环 - 实际的重连逻辑
        /// </summary>
        private async Task ReconnectLoopAsync()
        {
            if (_isDisposed || _isStopping) return;
            if (_state == ConnectionState.Reconnecting) return;

            try
            {
                _reconnectCts?.Cancel();
                _reconnectCts?.Dispose();
            }
            catch { }
            
            _reconnectCts = new CancellationTokenSource();
            var token = _reconnectCts.Token;

            SetState(ConnectionState.Reconnecting, "正在重连...");

            try
            {
                while (!token.IsCancellationRequested && _reconnectAttempts < _config.MaxReconnectAttempts)
                {
                    _reconnectAttempts++;
                    Log($"重连尝试 {_reconnectAttempts}/{_config.MaxReconnectAttempts}");

                    try
                    {
                        await Task.Delay(_config.ReconnectDelayMs, token);
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }

                    // BUG修复: 检查是否正在停止
                    if (_isDisposed || _isStopping) return;

                    if (_listener != null)
                    {
                        _listener.Close();
                        _listener = null;
                    }

                    bool connected = await ConnectInternalAsync();
                    if (connected)
                    {
                        Log("重连成功");
                        return;
                    }
                }

                if (!token.IsCancellationRequested && !_isDisposed && !_isStopping)
                {
                    SetState(ConnectionState.Disconnected, "重连失败，已达最大尝试次数");
                }
            }
            catch (Exception ex)
            {
                Log($"重连异常: {ex.Message}");
                if (!_isDisposed && !_isStopping)
                {
                    SetState(ConnectionState.Disconnected, $"重连异常: {ex.Message}");
                }
            }
        }

        #endregion

        #region 辅助方法

        private void SetState(ConnectionState state, string message)
        {
            _state = state;
            Log($"状态变更: {state} - {message}");
            OnStateChanged?.Invoke(state, message);
        }

        private void Log(string message)
        {
            OnLog?.Invoke($"[WSLConnectionManager] {message}");
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            _reconnectCts?.Cancel();
            _reconnectCts?.Dispose();
            
            if (_listener != null)
            {
                _listener.Close();
            }
            
            foreach (var tcs in _pendingRequests.Values)
            {
                tcs.TrySetCanceled();
            }
            _pendingRequests.Clear();
        }

        #endregion
    }
}
