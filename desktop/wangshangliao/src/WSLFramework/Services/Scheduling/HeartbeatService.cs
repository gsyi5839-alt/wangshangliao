using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using WSLFramework.Utils;

namespace WSLFramework.Services
{
    /// <summary>
    /// 心跳检测服务 - 提供本地 HTTP 心跳接口
    /// 端口: 51234
    /// 接口: GET /ping
    /// 根据旺商聊深度连接协议实现
    /// </summary>
    public class HeartbeatService : IDisposable
    {
        #region 单例模式

        private static readonly Lazy<HeartbeatService> _instance =
            new Lazy<HeartbeatService>(() => new HeartbeatService());

        public static HeartbeatService Instance => _instance.Value;

        #endregion

        #region 常量

        public const int DEFAULT_PORT = 51234;
        public const string PING_PATH = "/ping";

        #endregion

        #region 私有字段

        private HttpListener _listener;
        private CancellationTokenSource _cts;
        private Task _listenerTask;
        private readonly JavaScriptSerializer _serializer;
        private volatile bool _isRunning;
        private volatile bool _isDisposed;
        private volatile bool _useExternalService; // 是否使用外部服务（如 xplugin.exe）

        #endregion

        #region 公共属性

        /// <summary>监听端口</summary>
        public int Port { get; set; } = DEFAULT_PORT;

        /// <summary>是否正在运行</summary>
        public bool IsRunning => _isRunning;

        /// <summary>当前用户ID</summary>
        public long CurrentUserId { get; set; }
        
        /// <summary>当前用户昵称（机器人名称）</summary>
        public string CurrentUserName { get; set; }

        /// <summary>设备ID</summary>
        public int DeviceId { get; set; } = 0;

        /// <summary>是否在线</summary>
        public bool IsOnline { get; set; } = true;

        /// <summary>是否使用外部心跳服务（如 xplugin.exe）</summary>
        public bool UseExternalService => _useExternalService;

        #endregion

        #region 事件

        public event Action<string> OnLog;
        public event Action<bool> OnStatusChanged;

        #endregion

        #region 构造函数

        private HeartbeatService()
        {
            _serializer = new JavaScriptSerializer();
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 启动心跳服务
        /// </summary>
        public async Task<bool> StartAsync()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(HeartbeatService));

            if (_isRunning)
                return true;

            // 首先检查端口是否已被占用（可能是老的 xplugin.exe）
            if (IsPortInUse(Port))
            {
                Log($"⚠ 端口 {Port} 已被占用（可能是 xplugin.exe），将使用已有的心跳服务");
                _useExternalService = true;
                
                // 验证外部服务是否可用
                var isAvailable = await CheckExternalServiceAsync();
                if (isAvailable)
                {
                    Log($"✓ 检测到已有心跳服务运行中，将复用该服务");
                    return true;
                }
                else
                {
                    Log($"⚠ 外部心跳服务不可用，尝试使用备用端口");
                    _useExternalService = false;
                }
            }

            // 尝试启动本地服务
            try
            {
                _listener = new HttpListener();
                // 注意: 使用 + 表示任何主机名需要管理员权限
                // 使用具体地址更安全
                _listener.Prefixes.Add($"http://127.0.0.1:{Port}/");
                // 不添加 localhost 前缀，避免冲突

                _cts = new CancellationTokenSource();

                _listener.Start();
                _isRunning = true;

                Log($"✓ 心跳服务已启动，监听端口: {Port}");
                Log($"  心跳接口: http://127.0.0.1:{Port}{PING_PATH}");

                // 启动监听任务
                _listenerTask = Task.Run(() => ListenLoopAsync(_cts.Token));

                return true;
            }
            catch (HttpListenerException ex) when (ex.ErrorCode == 183 || ex.ErrorCode == 32)
            {
                // ERROR_ALREADY_EXISTS (183) 或 ERROR_SHARING_VIOLATION (32)
                Log($"⚠ 端口 {Port} 已被其他服务占用，将复用已有服务");
                _useExternalService = true;
                return true;
            }
            catch (Exception ex)
            {
                Log($"⚠ 心跳服务启动失败: {ex.Message}，将尝试使用已有服务");
                _useExternalService = true;
                return true; // 不返回 false，因为可能有外部服务可用
            }
        }

        /// <summary>
        /// 检查端口是否被占用
        /// </summary>
        private bool IsPortInUse(int port)
        {
            try
            {
                var properties = System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties();
                var listeners = properties.GetActiveTcpListeners();
                foreach (var endpoint in listeners)
                {
                    if (endpoint.Port == port)
                        return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 检查外部心跳服务是否可用
        /// </summary>
        private async Task<bool> CheckExternalServiceAsync()
        {
            try
            {
                using (var client = new System.Net.Http.HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(2);
                    var response = await client.GetAsync($"http://127.0.0.1:{Port}{PING_PATH}");
                    return response.IsSuccessStatusCode;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 停止心跳服务
        /// </summary>
        public async Task StopAsync()
        {
            if (!_isRunning)
                return;

            try
            {
                _cts?.Cancel();
                _listener?.Stop();

                if (_listenerTask != null)
                {
                    await Task.WhenAny(_listenerTask, Task.Delay(3000));
                }

                _isRunning = false;
                Log("心跳服务已停止");
            }
            catch (Exception ex)
            {
                Log($"停止心跳服务异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 设置在线状态
        /// </summary>
        public void SetOnlineStatus(bool online)
        {
            var changed = IsOnline != online;
            IsOnline = online;
            
            if (changed)
            {
                OnStatusChanged?.Invoke(online);
                Log($"在线状态变更: {(online ? "在线" : "离线")}");
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// HTTP 监听循环
        /// </summary>
        private async Task ListenLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested && _listener?.IsListening == true)
            {
                try
                {
                    var context = await _listener.GetContextAsync();
                    _ = Task.Run(() => HandleRequestAsync(context));
                }
                catch (HttpListenerException) when (token.IsCancellationRequested)
                {
                    // 正常停止
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    if (!token.IsCancellationRequested)
                    {
                        Log($"HTTP 监听异常: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// 处理 HTTP 请求
        /// </summary>
        private async Task HandleRequestAsync(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            try
            {
                string responseJson;
                int statusCode = 200;

                // 处理 /ping 请求
                if (request.Url.AbsolutePath == PING_PATH)
                {
                    responseJson = HandlePingRequest();
                }
                // 处理 /status 请求 (扩展)
                else if (request.Url.AbsolutePath == "/status")
                {
                    responseJson = HandleStatusRequest();
                }
                else
                {
                    statusCode = 404;
                    responseJson = _serializer.Serialize(new
                    {
                        code = 404,
                        msg = "Not Found"
                    });
                }

                // 发送响应
                response.StatusCode = statusCode;
                response.ContentType = "application/json";
                response.Headers.Add("Access-Control-Allow-Origin", "*");

                byte[] buffer = Encoding.UTF8.GetBytes(responseJson);
                response.ContentLength64 = buffer.Length;
                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            }
            catch (Exception ex)
            {
                Log($"处理请求异常: {ex.Message}");
            }
            finally
            {
                try
                {
                    response.Close();
                }
                catch { }
            }
        }

        /// <summary>
        /// 处理 /ping 请求
        /// 成功响应: {"id":0,"code":0,"uid":9502248}
        /// 失败响应: {"id":0,"code":403,"errno":50,"msg":"设备[0]掉线了!"}
        /// </summary>
        private string HandlePingRequest()
        {
            if (IsOnline)
            {
                return _serializer.Serialize(new
                {
                    id = DeviceId,
                    code = 0,
                    uid = CurrentUserId
                });
            }
            else
            {
                return _serializer.Serialize(new
                {
                    id = DeviceId,
                    code = 403,
                    errno = 50,
                    msg = $"设备[{DeviceId}]掉线了!"
                });
            }
        }

        /// <summary>
        /// 处理 /status 请求 (扩展接口)
        /// </summary>
        private string HandleStatusRequest()
        {
            return _serializer.Serialize(new
            {
                code = 0,
                data = new
                {
                    online = IsOnline,
                    deviceId = DeviceId,
                    userId = CurrentUserId,
                    uptime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                }
            });
        }

        private void Log(string message)
        {
            Logger.Info($"[Heartbeat] {message}");
            OnLog?.Invoke(message);
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_isDisposed)
                return;
            _isDisposed = true;

            try
            {
                _cts?.Cancel();
                _listener?.Close();
            }
            catch { }

            _cts?.Dispose();
        }

        #endregion
    }
}
