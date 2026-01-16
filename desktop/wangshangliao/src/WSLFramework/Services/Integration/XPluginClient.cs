using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WSLFramework.Protocol;
using WSLFramework.Utils;

namespace WSLFramework.Services
{
    /// <summary>
    /// XPlugin 框架客户端 - 连接到 xplugin.exe 的 TCP 端口
    /// 根据旺商聊深度连接协议实现
    /// 端口: 14745
    /// </summary>
    public class XPluginClient : IDisposable
    {
        #region 常量

        public const string DEFAULT_HOST = "127.0.0.1";
        public const int DEFAULT_PORT = 14745;
        public const int RECV_BUFFER_SIZE = 65536;
        public const int DEFAULT_TIMEOUT = 5000;

        #endregion

        #region 已知成功返回值 (根据最底层消息协议文档)

        /// <summary>
        /// 已知的成功返回值 (Base64编码)
        /// </summary>
        public static readonly string[] KNOWN_SUCCESS_RETURNS = new[]
        {
            "TlllEPH6nt6j+I+wy69fZw==",   // 取绑定群成功(空) - 16字节
            "4PtLK0IVuLMRkWlrzZJH3w==",   // 发送消息成功 - 16字节
            "Dg15wK9Ua6C+fcRgZoN3NQ==",   // 发送消息成功 (文档标准返回值) - 16字节
        };

        /// <summary>
        /// 发送消息成功的固定返回值 (根据最底层消息协议)
        /// Base64解码后: 16字节
        /// 十六进制: 0E-0D-79-C0-AF-54-6B-A0-BE-7D-C4-60-66-83-77-35
        /// 字节分解:
        ///   [0-3]  0E-0D-79-C0  - 消息确认头
        ///   [4-7]  AF-54-6B-A0  - 会话标识
        ///   [8-11] BE-7D-C4-60  - 时间戳片段
        ///   [12-15] 66-83-77-35 - 校验和
        /// </summary>
        public const string SEND_MESSAGE_SUCCESS = "Dg15wK9Ua6C+fcRgZoN3NQ==";

        /// <summary>
        /// 业务返回值头部 (改群名片/ID互查等)
        /// 十六进制: 88-81-1C-C9
        /// </summary>
        public static readonly byte[] BUSINESS_RESPONSE_HEADER = { 0x88, 0x81, 0x1C, 0xC9 };

        /// <summary>
        /// 发送成功返回值头部
        /// 十六进制: 0E-0D-79-C0
        /// </summary>
        public static readonly byte[] SEND_SUCCESS_HEADER = { 0x0E, 0x0D, 0x79, 0xC0 };

        /// <summary>
        /// 禁言成功返回值长度范围 (40-70字节)
        /// </summary>
        public const int MUTE_SUCCESS_MIN_LENGTH = 40;
        public const int MUTE_SUCCESS_MAX_LENGTH = 70;

        /// <summary>
        /// ID查询成功返回值长度范围 (90-200字节)
        /// </summary>
        public const int ID_QUERY_SUCCESS_MIN_LENGTH = 90;
        public const int ID_QUERY_SUCCESS_MAX_LENGTH = 200;

        /// <summary>
        /// 改群名片成功返回值最小长度 (80字节)
        /// </summary>
        public const int CHANGE_CARD_SUCCESS_MIN_LENGTH = 80;

        /// <summary>
        /// 短返回值长度 (16字节 - 固定成功返回)
        /// </summary>
        public const int SHORT_RESPONSE_LENGTH = 16;

        /// <summary>
        /// 短返回值长度阈值 (<=32字节通常是成功)
        /// </summary>
        public const int SHORT_RESPONSE_THRESHOLD = 32;

        #endregion

        #region 私有字段

        private TcpClient _client;
        private NetworkStream _stream;
        private CancellationTokenSource _cts;
        private Task _receiveTask;
        private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _pendingRequests;
        private readonly StringBuilder _receiveBuffer;
        private readonly object _sendLock = new object();
        private volatile bool _isConnected;
        private volatile bool _isDisposed;

        #endregion

        #region 公共属性

        /// <summary>服务器地址</summary>
        public string Host { get; set; } = DEFAULT_HOST;

        /// <summary>服务器端口</summary>
        public int Port { get; set; } = DEFAULT_PORT;

        /// <summary>是否已连接</summary>
        public bool IsConnected => _isConnected && _client?.Connected == true;

        /// <summary>超时时间(毫秒)</summary>
        public int Timeout { get; set; } = DEFAULT_TIMEOUT;

        #endregion

        #region 事件

        public event Action<bool> OnConnectionChanged;
        public event Action<string> OnMessageReceived;
        public event Action<string> OnLog;

        #endregion

        #region 构造函数

        public XPluginClient()
        {
            _pendingRequests = new ConcurrentDictionary<string, TaskCompletionSource<string>>();
            _receiveBuffer = new StringBuilder();
        }

        public XPluginClient(string host, int port) : this()
        {
            Host = host;
            Port = port;
        }

        #endregion

        #region 连接管理

        /// <summary>
        /// 连接到 xplugin 框架
        /// </summary>
        public async Task<bool> ConnectAsync()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(XPluginClient));

            if (IsConnected)
                return true;

            try
            {
                _client = new TcpClient();
                _client.ReceiveTimeout = Timeout;
                _client.SendTimeout = Timeout;

                Log($"正在连接到 {Host}:{Port}...");

                var connectTask = _client.ConnectAsync(Host, Port);
                var delayTask = Task.Delay(Timeout);
                var completedTask = await Task.WhenAny(connectTask, delayTask);

                if (completedTask == delayTask)
                {
                    // BUG FIX: 超时时需要清理并等待连接任务完成或取消
                    Log($"连接超时");
                    try
                    {
                        _client?.Close();
                        _client?.Dispose();
                    }
                    catch { /* 忽略清理异常 */ }
                    _client = null;
                    
                    // 等待连接任务完成（通常会因为 socket 关闭而失败）
                    try { await connectTask; } catch { /* 忽略取消异常 */ }
                    return false;
                }

                // BUG FIX: 检查连接任务是否有异常
                if (connectTask.IsFaulted)
                {
                    Log($"连接失败: {connectTask.Exception?.InnerException?.Message ?? "未知错误"}");
                    return false;
                }

                await connectTask;

                _stream = _client.GetStream();
                _isConnected = true;
                _cts = new CancellationTokenSource();

                Log($"✓ 已连接到 {Host}:{Port}");
                OnConnectionChanged?.Invoke(true);

                // 启动接收任务
                _receiveTask = Task.Run(() => ReceiveLoopAsync(_cts.Token));

                return true;
            }
            catch (Exception ex)
            {
                Log($"连接失败: {ex.Message}");
                _isConnected = false;
                // BUG FIX: 确保清理资源
                try
                {
                    _client?.Close();
                    _client?.Dispose();
                }
                catch { /* 忽略清理异常 */ }
                _client = null;
                return false;
            }
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        public async Task DisconnectAsync()
        {
            if (!_isConnected)
                return;

            try
            {
                _cts?.Cancel();
                _isConnected = false;

                _stream?.Close();
                _client?.Close();

                if (_receiveTask != null)
                {
                    await Task.WhenAny(_receiveTask, Task.Delay(1000));
                }

                OnConnectionChanged?.Invoke(false);
                Log("已断开连接");
            }
            catch (Exception ex)
            {
                Log($"断开连接异常: {ex.Message}");
            }
        }

        #endregion

        #region 接收消息

        /// <summary>
        /// 接收消息循环
        /// </summary>
        private async Task ReceiveLoopAsync(CancellationToken token)
        {
            var buffer = new byte[RECV_BUFFER_SIZE];

            while (!token.IsCancellationRequested && _isConnected)
            {
                try
                {
                    if (_stream == null || !_stream.CanRead)
                        break;

                    var bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length, token);

                    if (bytesRead == 0)
                    {
                        Log("服务器关闭连接");
                        break;
                    }

                    var data = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    ProcessReceivedData(data);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex) when (!token.IsCancellationRequested)
                {
                    Log($"接收数据异常: {ex.Message}");
                    break;
                }
            }

            _isConnected = false;
            OnConnectionChanged?.Invoke(false);
        }

        /// <summary>
        /// 处理接收到的数据
        /// </summary>
        private void ProcessReceivedData(string data)
        {
            _receiveBuffer.Append(data);

            // 按换行分割消息
            var content = _receiveBuffer.ToString();
            var lines = content.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

            // 检查最后是否有未完成的行
            bool lastLineComplete = content.EndsWith("\n");

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrEmpty(line))
                    continue;

                // 如果是最后一行且不完整，保留在缓冲区
                if (i == lines.Length - 1 && !lastLineComplete)
                {
                    _receiveBuffer.Clear();
                    _receiveBuffer.Append(line);
                    break;
                }

                ProcessMessage(line);
            }

            if (lastLineComplete)
            {
                _receiveBuffer.Clear();
            }
        }

        /// <summary>
        /// 处理单条消息
        /// </summary>
        private void ProcessMessage(string message)
        {
            Log($"收到: {message}");

            // 检查是否是 API 响应 (包含 "返回结果:")
            if (message.Contains("返回结果:"))
            {
                // 提取 API 名称作为请求 ID
                var parts = message.Split(ZCGProtocol.API_SEPARATOR);
                if (parts.Length > 0)
                {
                    var apiName = parts[0];
                    if (_pendingRequests.TryRemove(apiName, out var tcs))
                    {
                        tcs.TrySetResult(message);
                    }
                }
            }

            // 触发消息接收事件
            OnMessageReceived?.Invoke(message);
        }

        #endregion

        #region 发送消息

        /// <summary>
        /// 发送 API 调用
        /// </summary>
        public async Task<string> SendApiAsync(string apiCall, int timeout = 0)
        {
            if (!IsConnected)
            {
                Log("未连接到服务器");
                return null;
            }

            try
            {
                // 提取 API 名称作为请求 ID
                var apiName = apiCall.Split(ZCGProtocol.API_SEPARATOR)[0];
                var tcs = new TaskCompletionSource<string>();

                // 注册等待响应
                _pendingRequests[apiName] = tcs;

                // 发送请求
                var data = apiCall + "\n";
                await SendRawAsync(data);

                Log($"发送: {apiCall}");

                // 等待响应
                var actualTimeout = timeout > 0 ? timeout : Timeout;
                if (await Task.WhenAny(tcs.Task, Task.Delay(actualTimeout)) == tcs.Task)
                {
                    return await tcs.Task;
                }
                else
                {
                    _pendingRequests.TryRemove(apiName, out _);
                    Log($"API调用超时: {apiName}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Log($"发送API异常: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 发送原始数据
        /// </summary>
        public async Task<bool> SendRawAsync(string data)
        {
            if (!IsConnected || _stream == null)
                return false;

            try
            {
                var bytes = Encoding.UTF8.GetBytes(data);

                lock (_sendLock)
                {
                    _stream.Write(bytes, 0, bytes.Length);
                    _stream.Flush();
                }

                return true;
            }
            catch (Exception ex)
            {
                Log($"发送数据异常: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region ZCG API 方法

        /// <summary>
        /// 获取在线账号
        /// API: 云信_获取在线账号
        /// </summary>
        public Task<string> GetOnlineAccountsAsync()
        {
            return SendApiAsync("云信_获取在线账号");
        }

        /// <summary>
        /// 获取绑定群
        /// API: 取绑定群|{机器人号}
        /// </summary>
        public Task<string> GetBindGroupsAsync(string robotId)
        {
            return SendApiAsync($"取绑定群|{robotId}");
        }

        /// <summary>
        /// 发送群消息
        /// API: 发送群消息（文本）|{机器人号}|{内容}|{群号}|{类型}|{标志}
        /// </summary>
        /// <param name="robotId">机器人号</param>
        /// <param name="content">消息内容</param>
        /// <param name="groupId">群号</param>
        /// <param name="type">类型: 1=普通消息</param>
        /// <param name="flag">标志: 0=正常发送</param>
        public Task<string> SendGroupMessageAsync(string robotId, string content, string groupId, int type = 1, int flag = 0)
        {
            // 转义换行符
            content = content.Replace("\n", "\\n");
            return SendApiAsync($"发送群消息（文本）|{robotId}|{content}|{groupId}|{type}|{flag}");
        }

        /// <summary>
        /// 发送私聊消息
        /// API: 发送好友消息|{机器人号}|{内容}|{目标号}
        /// </summary>
        public Task<string> SendPrivateMessageAsync(string robotId, string content, string targetId)
        {
            content = content.Replace("\n", "\\n");
            return SendApiAsync($"发送好友消息|{robotId}|{content}|{targetId}");
        }

        /// <summary>
        /// 群禁言/解禁
        /// API: ww_群禁言解禁|{机器人号}|{群号}|{动作}
        /// </summary>
        /// <param name="robotId">机器人号</param>
        /// <param name="groupId">群号</param>
        /// <param name="mute">true=禁言, false=解禁</param>
        public Task<string> SetGroupMuteAsync(string robotId, string groupId, bool mute)
        {
            var action = mute ? "1" : "2";
            return SendApiAsync($"ww_群禁言解禁|{robotId}|{groupId}|{action}");
        }

        /// <summary>
        /// 修改群名片
        /// API: ww_改群名片|{机器人号}|{群号}|{用户号}|{新名片}
        /// </summary>
        public Task<string> UpdateGroupCardAsync(string robotId, string groupId, string userId, string newCard)
        {
            return SendApiAsync($"ww_改群名片|{robotId}|{groupId}|{userId}|{newCard}");
        }

        /// <summary>
        /// 获取群资料
        /// API: ww_获取群资料|{机器人号}|{群号}
        /// </summary>
        public Task<string> GetGroupInfoAsync(string robotId, string groupId)
        {
            return SendApiAsync($"ww_获取群资料|{robotId}|{groupId}");
        }

        /// <summary>
        /// ID互查
        /// API: ww_ID互查|{机器人号}|{旺商聊号}
        /// </summary>
        public Task<string> QueryIdAsync(string robotId, string wangshangliaoId)
        {
            return SendApiAsync($"ww_ID互查|{robotId}|{wangshangliaoId}");
        }
        
        /// <summary>
        /// 获取用户资料
        /// API: 取个人资料|{机器人号}|{用户号}
        /// </summary>
        public Task<string> GetUserProfileAsync(string robotId, string userId)
        {
            return SendApiAsync($"取个人资料|{robotId}|{userId}");
        }
        
        /// <summary>
        /// 获取群成员列表
        /// API: 取群成员|{机器人号}|{群号}
        /// </summary>
        public Task<string> GetGroupMembersAsync(string robotId, string groupId)
        {
            return SendApiAsync($"取群成员|{robotId}|{groupId}");
        }

        /// <summary>
        /// 授权验证
        /// API: ww_xp限制接口|{状态}|{软件ID}|{授权码}|{用户名}|{时间1}|{时间2}
        /// </summary>
        public Task<string> AuthenticateAsync(bool active, string softwareId, string authCode, string userName, string time1, string time2)
        {
            var status = active ? "真" : "假";
            return SendApiAsync($"ww_xp限制接口|{status}|{softwareId}|{authCode}|{userName}|{time1}|{time2}");
        }

        /// <summary>
        /// 发送群消息（简化版）
        /// API: 发送群消息（文本）|{机器人号}|{内容}|{群号}|1|0
        /// </summary>
        public Task<string> SendGroupTextAsync(string robotId, string content, string groupId)
        {
            return SendGroupMessageAsync(robotId, content, groupId, 1, 0);
        }

        /// <summary>
        /// 开启群禁言
        /// API: ww_群禁言解禁|{机器人号}|{群号}|1
        /// </summary>
        public Task<string> MuteGroupAsync(string robotId, string groupId)
        {
            return SetGroupMuteAsync(robotId, groupId, true);
        }

        /// <summary>
        /// 关闭群禁言
        /// API: ww_群禁言解禁|{机器人号}|{群号}|2
        /// </summary>
        public Task<string> UnmuteGroupAsync(string robotId, string groupId)
        {
            return SetGroupMuteAsync(robotId, groupId, false);
        }

        #endregion

        #region 响应解析

        /// <summary>
        /// 解析 API 响应
        /// </summary>
        public static ApiParseResult ParseApiResponse(string response)
        {
            var result = new ApiParseResult();

            if (string.IsNullOrEmpty(response))
            {
                result.Success = false;
                result.Error = "响应为空";
                return result;
            }

            // 提取返回结果
            var returnIndex = response.IndexOf("返回结果:");
            if (returnIndex < 0)
            {
                result.Success = false;
                result.Error = "响应格式错误";
                result.RawResponse = response;
                return result;
            }

            var parts = response.Substring(0, returnIndex).Split(ZCGProtocol.API_SEPARATOR);
            result.ApiName = parts.Length > 0 ? parts[0] : "";
            result.Parameters = new string[Math.Max(0, parts.Length - 1)];
            if (parts.Length > 1)
            {
                Array.Copy(parts, 1, result.Parameters, 0, parts.Length - 1);
            }

            // 提取 Base64 结果
            result.Base64Result = response.Substring(returnIndex + 5).Trim();
            result.RawResponse = response;

            // 判断是否成功
            result.Success = IsSuccessResult(result.Base64Result);

            return result;
        }

        /// <summary>
        /// 判断是否为成功返回
        /// </summary>
        public static bool IsSuccessResult(string base64Result)
        {
            // 检查已知成功返回值
            foreach (var known in KNOWN_SUCCESS_RETURNS)
            {
                if (base64Result == known)
                    return true;
            }

            try
            {
                var decoded = Convert.FromBase64String(base64Result);
                // 短响应(<=32字节)通常是成功
                return decoded.Length <= SHORT_RESPONSE_THRESHOLD;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 判断发送消息是否成功
        /// 根据旺商聊深度连接协议第十五节
        /// </summary>
        public static bool IsSendMessageSuccess(string base64Result)
        {
            return base64Result == SEND_MESSAGE_SUCCESS;
        }

        /// <summary>
        /// 判断禁言操作是否成功
        /// 根据旺商聊深度连接协议第十三节
        /// 成功返回: 48-64字节的加密数据
        /// </summary>
        public static bool IsMuteSuccess(string base64Result)
        {
            try
            {
                var decoded = Convert.FromBase64String(base64Result);
                return decoded.Length >= MUTE_SUCCESS_MIN_LENGTH && 
                       decoded.Length <= MUTE_SUCCESS_MAX_LENGTH;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 判断ID查询是否成功
        /// 根据旺商聊深度连接协议第十四节
        /// 成功返回: 100-150字节的加密数据
        /// </summary>
        public static bool IsIdQuerySuccess(string base64Result)
        {
            try
            {
                var decoded = Convert.FromBase64String(base64Result);
                return decoded.Length >= ID_QUERY_SUCCESS_MIN_LENGTH && 
                       decoded.Length <= ID_QUERY_SUCCESS_MAX_LENGTH;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 解码 Base64 返回值
        /// </summary>
        public static byte[] DecodeResult(string base64Result)
        {
            try
            {
                return Convert.FromBase64String(base64Result);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 尝试将返回值解析为JSON (用于错误响应)
        /// </summary>
        public static Dictionary<string, object> TryParseAsJson(string base64Result)
        {
            try
            {
                var decoded = Convert.FromBase64String(base64Result);
                var text = Encoding.UTF8.GetString(decoded);
                
                // 尝试解析为JSON
                if (text.StartsWith("{") && text.EndsWith("}"))
                {
                    var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
                    return serializer.Deserialize<Dictionary<string, object>>(text);
                }
            }
            catch
            {
                // 忽略解析错误
            }
            return null;
        }

        /// <summary>
        /// 获取返回值的错误信息 (如果有)
        /// </summary>
        public static string GetErrorMessage(string base64Result)
        {
            var json = TryParseAsJson(base64Result);
            if (json != null)
            {
                if (json.ContainsKey("msg"))
                    return json["msg"]?.ToString();
                if (json.ContainsKey("message"))
                    return json["message"]?.ToString();
            }
            return null;
        }

        /// <summary>
        /// 获取返回值的错误码 (如果有)
        /// </summary>
        public static int? GetErrorCode(string base64Result)
        {
            var json = TryParseAsJson(base64Result);
            if (json != null)
            {
                if (json.ContainsKey("code"))
                    return Convert.ToInt32(json["code"]);
            }
            return null;
        }

        /// <summary>
        /// 判断改群名片是否成功
        /// 根据最底层消息协议文档第六节
        /// 成功: Base64返回, 长度≥80字节, 头部88-81-1C-C9
        /// </summary>
        public static bool IsChangeCardSuccess(string base64Result)
        {
            try
            {
                var decoded = Convert.FromBase64String(base64Result);
                
                // 检查长度
                if (decoded.Length < CHANGE_CARD_SUCCESS_MIN_LENGTH)
                    return false;
                
                // 检查头部
                if (decoded.Length >= 4)
                {
                    return decoded[0] == BUSINESS_RESPONSE_HEADER[0] &&
                           decoded[1] == BUSINESS_RESPONSE_HEADER[1] &&
                           decoded[2] == BUSINESS_RESPONSE_HEADER[2] &&
                           decoded[3] == BUSINESS_RESPONSE_HEADER[3];
                }
                
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 判断是否为业务成功返回 (改群名片/ID互查等)
        /// 头部: 88-81-1C-C9
        /// </summary>
        public static bool IsBusinessSuccess(string base64Result)
        {
            try
            {
                var decoded = Convert.FromBase64String(base64Result);
                
                if (decoded.Length >= 4)
                {
                    return decoded[0] == BUSINESS_RESPONSE_HEADER[0] &&
                           decoded[1] == BUSINESS_RESPONSE_HEADER[1] &&
                           decoded[2] == BUSINESS_RESPONSE_HEADER[2] &&
                           decoded[3] == BUSINESS_RESPONSE_HEADER[3];
                }
                
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 解析返回值类型
        /// 根据最底层消息协议文档第五节
        /// </summary>
        public static ApiReturnType ParseReturnType(string base64Result)
        {
            // 固定成功值
            if (base64Result == SEND_MESSAGE_SUCCESS)
            {
                return ApiReturnType.SendSuccess;
            }

            try
            {
                var decoded = Convert.FromBase64String(base64Result);
                
                // 检查头部
                if (decoded.Length >= 4)
                {
                    // 业务返回 (88-81-1C-C9)
                    if (decoded[0] == BUSINESS_RESPONSE_HEADER[0] &&
                        decoded[1] == BUSINESS_RESPONSE_HEADER[1] &&
                        decoded[2] == BUSINESS_RESPONSE_HEADER[2] &&
                        decoded[3] == BUSINESS_RESPONSE_HEADER[3])
                    {
                        return ApiReturnType.BusinessResponse;
                    }
                    
                    // 发送成功 (0E-0D-79-C0)
                    if (decoded[0] == SEND_SUCCESS_HEADER[0] &&
                        decoded[1] == SEND_SUCCESS_HEADER[1] &&
                        decoded[2] == SEND_SUCCESS_HEADER[2] &&
                        decoded[3] == SEND_SUCCESS_HEADER[3])
                    {
                        return ApiReturnType.SendSuccess;
                    }
                }
                
                // 短返回 (16字节)
                if (decoded.Length == SHORT_RESPONSE_LENGTH)
                {
                    return ApiReturnType.AckResponse;
                }
                
                // 尝试解析为JSON (错误情况)
                var text = Encoding.UTF8.GetString(decoded);
                if (text.StartsWith("{") && text.Contains("\"code\""))
                {
                    return ApiReturnType.JsonError;
                }
                
                return ApiReturnType.Unknown;
            }
            catch
            {
                return ApiReturnType.DecodeError;
            }
        }

        #endregion

        #region 辅助方法

        private void Log(string message)
        {
            Logger.Info($"[XPlugin] {message}");
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
                _stream?.Close();
                _client?.Close();
            }
            catch { }

            _cts?.Dispose();
        }

        #endregion
    }

    #region API 解析结果

    /// <summary>
    /// API 解析结果
    /// </summary>
    public class ApiParseResult
    {
        public bool Success { get; set; }
        public string ApiName { get; set; }
        public string[] Parameters { get; set; }
        public string Base64Result { get; set; }
        public string RawResponse { get; set; }
        public string Error { get; set; }

        /// <summary>
        /// 获取解码后的结果
        /// </summary>
        public byte[] GetDecodedResult()
        {
            return XPluginClient.DecodeResult(Base64Result);
        }

        /// <summary>
        /// 获取返回类型
        /// </summary>
        public ApiReturnType GetReturnType()
        {
            return XPluginClient.ParseReturnType(Base64Result);
        }
    }

    #endregion

    #region API 返回类型枚举 (根据最底层消息协议文档)

    /// <summary>
    /// API 返回值类型
    /// </summary>
    public enum ApiReturnType
    {
        /// <summary>未知类型</summary>
        Unknown = 0,

        /// <summary>发送成功 (固定16字节, 头部: 0E-0D-79-C0)</summary>
        SendSuccess = 1,

        /// <summary>业务返回 (变长, 头部: 88-81-1C-C9, 如改群名片/ID互查)</summary>
        BusinessResponse = 2,

        /// <summary>确认返回 (16字节)</summary>
        AckResponse = 3,

        /// <summary>JSON错误返回</summary>
        JsonError = 4,

        /// <summary>解码失败</summary>
        DecodeError = 5
    }

    #endregion

    #region 消息类型常量 (根据最底层消息协议文档)

    /// <summary>
    /// 插件消息类型码
    /// </summary>
    public static class PluginMessageType
    {
        /// <summary>普通消息 (私聊/群聊文本)</summary>
        public const int NORMAL = 1001;

        /// <summary>群事件消息 (禁言/改名片等)</summary>
        public const int GROUP_EVENT = 1002;

        /// <summary>好友请求/系统通知</summary>
        public const int FRIEND_REQUEST = 1003;

        /// <summary>好友申请通知</summary>
        public const int FRIEND_APPLY = 1015;
    }

    /// <summary>
    /// 消息子类型
    /// </summary>
    public static class PluginMessageSubType
    {
        /// <summary>普通消息</summary>
        public const string NORMAL = "0";

        /// <summary>群禁言开启</summary>
        public const string GROUP_MUTE_ON = "NOTIFY_TYPE_GROUP_MUTE_1";

        /// <summary>群禁言解除</summary>
        public const string GROUP_MUTE_OFF = "NOTIFY_TYPE_GROUP_MUTE_0";

        /// <summary>用户改名片</summary>
        public const string USER_UPDATE_NAME = "NOTIFY_TYPE_USER_UPDATE_NAME";
    }

    #endregion
}
