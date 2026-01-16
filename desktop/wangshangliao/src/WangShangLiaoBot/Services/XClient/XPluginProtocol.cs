using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WangShangLiaoBot.Services.XClient
{
    /// <summary>
    /// XPlugin协议客户端 - 与ZCG原版xplugin.exe完全兼容
    /// 基于逆向分析获取的通信协议实现
    /// 
    /// 通信端口: 14745 (与ZCG原版一致)
    /// API格式: {API名称}|{参数1}|{参数2}|...|返回结果:{Base64}
    /// </summary>
    public sealed class XPluginProtocol : IDisposable
    {
        #region 单例

        private static XPluginProtocol _instance;
        private static readonly object _instanceLock = new object();

        public static XPluginProtocol Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_instanceLock)
                    {
                        if (_instance == null)
                            _instance = new XPluginProtocol();
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region 常量 - 基于逆向分析

        /// <summary>默认通信端口 (与ZCG原版一致)</summary>
        public const int DEFAULT_PORT = 14745;

        /// <summary>备用端口 (项目使用)</summary>
        public const int BACKUP_PORT = 14746;

        /// <summary>本地主机</summary>
        public const string LOCAL_HOST = "127.0.0.1";

        /// <summary>消息类型 - 群消息</summary>
        public const int MSG_TYPE_GROUP = 1002;

        /// <summary>消息类型 - 私聊消息</summary>
        public const int MSG_TYPE_PRIVATE = 1001;

        /// <summary>消息类型 - 系统通知</summary>
        public const int MSG_TYPE_SYSTEM = 1003;

        /// <summary>消息类型 - 好友申请</summary>
        public const int MSG_TYPE_FRIEND_REQUEST = 1015;

        /// <summary>已知成功返回值</summary>
        private static readonly string[] KNOWN_SUCCESS_RETURNS = new[]
        {
            "TlllEPH6nt6j+I+wy69fZw==",  // 取绑定群成功(空)
            "4PtLK0IVuLMRkWlrzZJH3w==",  // 发送消息成功
            "iIEcyahRpLaUj9x+zriHjv0yoGMMK77efcp2lIzf0/Q=" // 授权验证成功
        };

        #endregion

        #region 字段

        private TcpClient _client;
        private NetworkStream _stream;
        private StreamReader _reader;
        private StreamWriter _writer;
        private CancellationTokenSource _cts;
        private Task _receiveTask;

        private bool _isConnected;
        private int _port = DEFAULT_PORT;
        private string _robotQQ;

        private readonly ConcurrentDictionary<string, TaskCompletionSource<XPluginResponse>> _pendingRequests
            = new ConcurrentDictionary<string, TaskCompletionSource<XPluginResponse>>();

        private readonly object _sendLock = new object();

        #endregion

        #region 属性

        public bool IsConnected => _isConnected && _client?.Connected == true;
        public string RobotQQ => _robotQQ;
        public int Port => _port;

        #endregion

        #region 事件

        public event Action<string> OnLog;
        public event Action<bool> OnConnectionChanged;
        public event Action<XPluginMessage> OnMessageReceived;
        public event Action<XPluginMessage> OnGroupMessage;
        public event Action<XPluginMessage> OnPrivateMessage;
        public event Action<XPluginMessage> OnSystemMessage;

        #endregion

        private XPluginProtocol()
        {
        }

        #region 连接管理

        /// <summary>
        /// 连接到xplugin服务
        /// </summary>
        public async Task<bool> ConnectAsync(string host = LOCAL_HOST, int port = DEFAULT_PORT)
        {
            if (_isConnected)
            {
                Log("[XPlugin] 已连接");
                return true;
            }

            _port = port;

            try
            {
                Log($"[XPlugin] 正在连接 {host}:{port}...");

                _client = new TcpClient();
                _client.ReceiveTimeout = 30000;
                _client.SendTimeout = 10000;
                _client.NoDelay = true;

                await _client.ConnectAsync(host, port);

                _stream = _client.GetStream();
                _reader = new StreamReader(_stream, Encoding.GetEncoding("GBK"));
                _writer = new StreamWriter(_stream, Encoding.GetEncoding("GBK")) { AutoFlush = true };

                _isConnected = true;
                _cts = new CancellationTokenSource();

                // 启动接收循环
                _receiveTask = Task.Run(() => ReceiveLoopAsync(_cts.Token));

                Log($"[XPlugin] 连接成功 {host}:{port}");
                OnConnectionChanged?.Invoke(true);

                return true;
            }
            catch (Exception ex)
            {
                Log($"[XPlugin] 连接失败: {ex.Message}");

                // 尝试备用端口
                if (port == DEFAULT_PORT)
                {
                    Log($"[XPlugin] 尝试备用端口 {BACKUP_PORT}...");
                    return await ConnectAsync(host, BACKUP_PORT);
                }

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
            _cts?.Cancel();

            try
            {
                _reader?.Dispose();
                _writer?.Dispose();
                _stream?.Dispose();
                _client?.Dispose();
            }
            catch { }

            _reader = null;
            _writer = null;
            _stream = null;
            _client = null;

            // 取消所有等待中的请求
            foreach (var kv in _pendingRequests)
            {
                kv.Value.TrySetCanceled();
            }
            _pendingRequests.Clear();

            Log("[XPlugin] 已断开连接");
            OnConnectionChanged?.Invoke(false);
        }

        /// <summary>
        /// 接收循环
        /// </summary>
        private async Task ReceiveLoopAsync(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested && _isConnected)
                {
                    var line = await _reader.ReadLineAsync();
                    if (string.IsNullOrEmpty(line)) continue;

                    ProcessReceivedLine(line);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Log($"[XPlugin] 接收错误: {ex.Message}");
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
        /// 处理接收到的行
        /// </summary>
        private void ProcessReceivedLine(string line)
        {
            try
            {
                // 检查是否是API响应
                if (line.Contains("返回结果:"))
                {
                    ProcessApiResponse(line);
                    return;
                }

                // 检查是否是消息投递
                if (line.Contains("机器人账号="))
                {
                    ProcessMessageDelivery(line);
                    return;
                }

                // 其他消息
                Log($"[XPlugin] 收到: {line.Substring(0, Math.Min(100, line.Length))}");
            }
            catch (Exception ex)
            {
                Log($"[XPlugin] 处理消息错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理API响应 - 支持ZCG真实协议格式
        /// 格式: {API中文名}|{API内部名}|{参数}|{JSON返回}
        /// 示例: 发送群消息|Group_SendMsg|621705120|9999222222|测试|3
        ///       置全群禁言|Group_SayState|621705120|9999222222|1|{"code":0,"msg":"OK","data":{...}}
        /// </summary>
        private void ProcessApiResponse(string line)
        {
            var parts = line.Split('|');
            if (parts.Length < 2) return;

            var apiName = parts[0]; // 中文名
            var apiInternal = parts.Length > 1 ? parts[1] : ""; // 内部名

            // 查找JSON结果 (从末尾开始找{})
            string jsonResult = null;
            var lastPart = parts[parts.Length - 1];
            if (lastPart.StartsWith("{") && lastPart.EndsWith("}"))
            {
                jsonResult = lastPart;
            }

            // 检查是否有等待的请求
            var requestKey = line.Replace("|", "_");
            foreach (var kv in _pendingRequests.ToArray())
            {
                // 模糊匹配：检查请求key是否包含API名
                if (kv.Key.Contains(apiName) || kv.Key.Contains(apiInternal))
                {
                    var success = IsSuccessJson(jsonResult);
                    kv.Value.TrySetResult(new XPluginResponse
                    {
                        Success = success,
                        ApiName = apiName,
                        ApiInternalName = apiInternal,
                        ResultJson = jsonResult,
                        RawLine = line
                    });
                    _pendingRequests.TryRemove(kv.Key, out _);
                    break;
                }
            }
        }

        /// <summary>
        /// 判断JSON返回是否成功
        /// </summary>
        private bool IsSuccessJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return true; // 无返回视为成功
            
            try
            {
                // 检查 "code":0 表示成功
                if (json.Contains("\"code\":0") || json.Contains("\"code\": 0"))
                    return true;
                
                // 检查失败码
                if (json.Contains("\"code\":1051") || json.Contains("\"code\":-1"))
                    return false;
                
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 处理消息投递 (与ZCG原版格式完全兼容)
        /// 格式: 机器人账号={RQQ}，主动账号={fromQQ}，被动账号={toQQ}，群号={groupId}，
        ///       内容={content}，消息ID={msgId}，消息类型={msgType}，消息时间={timestamp}，
        ///       消息子类型={subType}，原始消息={rawJson}
        /// </summary>
        private void ProcessMessageDelivery(string line)
        {
            var msg = XPluginMessage.Parse(line);
            if (msg == null) return;

            // 触发通用事件
            OnMessageReceived?.Invoke(msg);

            // 按类型分发
            switch (msg.MessageType)
            {
                case MSG_TYPE_GROUP:
                    OnGroupMessage?.Invoke(msg);
                    break;
                case MSG_TYPE_PRIVATE:
                    OnPrivateMessage?.Invoke(msg);
                    break;
                case MSG_TYPE_SYSTEM:
                case MSG_TYPE_FRIEND_REQUEST:
                    OnSystemMessage?.Invoke(msg);
                    break;
            }
        }

        #endregion

        #region API调用 - 与ZCG原版格式完全兼容

        /// <summary>
        /// 发送API请求
        /// </summary>
        private async Task<XPluginResponse> SendApiAsync(string apiCall, int timeoutMs = 10000)
        {
            if (!IsConnected)
            {
                return new XPluginResponse { Success = false, Error = "未连接" };
            }

            var requestKey = apiCall.Replace("|", "_");
            var tcs = new TaskCompletionSource<XPluginResponse>();
            _pendingRequests[requestKey] = tcs;

            try
            {
                lock (_sendLock)
                {
                    _writer.WriteLine(apiCall);
                }

                using (var cts = new CancellationTokenSource(timeoutMs))
                {
                    cts.Token.Register(() =>
                    {
                        tcs.TrySetResult(new XPluginResponse { Success = false, Error = "超时" });
                    });

                    return await tcs.Task;
                }
            }
            catch (Exception ex)
            {
                return new XPluginResponse { Success = false, Error = ex.Message };
            }
            finally
            {
                _pendingRequests.TryRemove(requestKey, out _);
            }
        }

        /// <summary>
        /// 发送群消息 (文本) - 基于逆向分析的真实协议
        /// API格式: 发送群消息|Group_SendMsg|{机器人号}|{群号}|{内容}|{类型:3=普通}
        /// </summary>
        public async Task<bool> SendGroupMessageAsync(string groupId, string content, int type = 3)
        {
            if (string.IsNullOrEmpty(_robotQQ))
            {
                Log("[XPlugin] 未设置机器人QQ");
                return false;
            }

            // 转义换行符
            content = content.Replace("\n", "\\n");

            var api = $"发送群消息|Group_SendMsg|{_robotQQ}|{groupId}|{content}|{type}";
            var response = await SendApiAsync(api);

            if (response.Success)
            {
                Log($"[XPlugin] 群消息发送成功: {groupId}");
            }
            else
            {
                Log($"[XPlugin] 群消息发送失败: {response.Error}");
            }

            return response.Success;
        }

        /// <summary>
        /// 发送私聊消息 - 基于逆向分析的真实协议
        /// API格式: 发送好友消息|Friend_SendMsg|{机器人号}|{好友号}|{内容}
        /// </summary>
        public async Task<bool> SendPrivateMessageAsync(string userId, string content)
        {
            if (string.IsNullOrEmpty(_robotQQ))
            {
                Log("[XPlugin] 未设置机器人QQ");
                return false;
            }

            content = content.Replace("\n", "\\n");

            var api = $"发送好友消息|Friend_SendMsg|{_robotQQ}|{userId}|{content}";
            var response = await SendApiAsync(api);

            return response.Success;
        }

        /// <summary>
        /// 全群禁言/解禁 - 基于逆向分析的真实协议
        /// API格式: 置全群禁言|Group_SayState|{机器人号}|{群号}|{1=禁言/0=解禁}
        /// </summary>
        public async Task<bool> MuteGroupAsync(string groupId, bool mute)
        {
            if (string.IsNullOrEmpty(_robotQQ)) return false;

            var action = mute ? "1" : "0";
            var api = $"置全群禁言|Group_SayState|{_robotQQ}|{groupId}|{action}";
            var response = await SendApiAsync(api);

            if (response.Success)
            {
                Log($"[XPlugin] 群{(mute ? "禁言" : "解禁")}成功: {groupId}");
            }

            return response.Success;
        }

        /// <summary>
        /// 单人禁言/解禁 - 基于逆向分析的真实协议
        /// API格式: 置群成员禁言|Group_UserSayState|{机器人号}|{群号}|{用户号}|{1=禁言/0=解禁}|{分钟数}
        /// </summary>
        public async Task<bool> MuteMemberAsync(string groupId, string userId, bool mute, int minutes = 3000)
        {
            if (string.IsNullOrEmpty(_robotQQ)) return false;

            var action = mute ? "1" : "0";
            var mins = mute ? minutes : 0;
            var api = $"置群成员禁言|Group_UserSayState|{_robotQQ}|{groupId}|{userId}|{action}|{mins}";
            var response = await SendApiAsync(api);

            if (response.Success)
            {
                Log($"[XPlugin] 成员{(mute ? "禁言" : "解禁")}成功: {groupId}/{userId}");
            }

            return response.Success;
        }

        /// <summary>
        /// 修改群名片 - 基于逆向分析的真实协议
        /// API格式: 置群成员名片|Group_UserSetCardName|{机器人号}|{群号}|{用户号}|{新名片}
        /// </summary>
        public async Task<bool> SetMemberCardAsync(string groupId, string userId, string newCard)
        {
            if (string.IsNullOrEmpty(_robotQQ)) return false;

            var api = $"置群成员名片|Group_UserSetCardName|{_robotQQ}|{groupId}|{userId}|{newCard}";
            var response = await SendApiAsync(api);

            return response.Success;
        }

        /// <summary>
        /// 踢人出群 - 基于逆向分析的真实协议
        /// API格式: 踢出群聊|Group_DelteUser|{机器人号}|{群号}|{用户号}
        /// </summary>
        public async Task<bool> KickMemberAsync(string groupId, string userId)
        {
            if (string.IsNullOrEmpty(_robotQQ)) return false;

            var api = $"踢出群聊|Group_DelteUser|{_robotQQ}|{groupId}|{userId}";
            var response = await SendApiAsync(api);

            if (response.Success)
            {
                Log($"[XPlugin] 踢人成功: {groupId}/{userId}");
            }

            return response.Success;
        }

        /// <summary>
        /// 撤回群消息 - 基于逆向分析的真实协议
        /// API格式: 消息撤回_群|Group_WithdrawMessage|{机器人号}|{消息ID}|{群号}|{用户号}
        /// </summary>
        public async Task<bool> RecallGroupMessageAsync(string groupId, string userId, string messageId)
        {
            if (string.IsNullOrEmpty(_robotQQ)) return false;

            var api = $"消息撤回_群|Group_WithdrawMessage|{_robotQQ}|{messageId}|{groupId}|{userId}";
            var response = await SendApiAsync(api);

            return response.Success;
        }

        /// <summary>
        /// 获取群资料 - 基于逆向分析的真实协议
        /// API格式: 取群资料|Group_GetInfo|{机器人号}|{群号}
        /// </summary>
        public async Task<string> GetGroupInfoAsync(string groupId)
        {
            if (string.IsNullOrEmpty(_robotQQ)) return null;

            var api = $"取群资料|Group_GetInfo|{_robotQQ}|{groupId}";
            var response = await SendApiAsync(api);

            return response.Success ? response.ResultJson : null;
        }

        /// <summary>
        /// 获取个人资料 - 基于逆向分析的真实协议
        /// API格式: 取个人资料|GetInfo|{机器人号}|{用户号}
        /// </summary>
        public async Task<string> GetUserInfoAsync(string userId)
        {
            if (string.IsNullOrEmpty(_robotQQ)) return null;

            var api = $"取个人资料|GetInfo|{_robotQQ}|{userId}";
            var response = await SendApiAsync(api);

            return response.Success ? response.ResultJson : null;
        }

        /// <summary>
        /// 获取邀请人 - 基于逆向分析的真实协议
        /// API格式: 获取邀请我的人|Group_InquiryPassiveInviter|{机器人号}|{群号}|{用户号}
        /// </summary>
        public async Task<string> GetInviterAsync(string groupId, string userId)
        {
            if (string.IsNullOrEmpty(_robotQQ)) return null;

            var api = $"获取邀请我的人|Group_InquiryPassiveInviter|{_robotQQ}|{groupId}|{userId}";
            var response = await SendApiAsync(api);

            return response.Success ? response.ResultJson : null;
        }

        /// <summary>
        /// 处理好友申请 - 基于逆向分析的真实协议
        /// API格式: 置好友添加请求|Friend_SetApply|{机器人号}|{用户号}|{1=同意/0=拒绝}
        /// </summary>
        public async Task<bool> HandleFriendRequestAsync(string userId, bool accept)
        {
            if (string.IsNullOrEmpty(_robotQQ)) return false;

            var action = accept ? "1" : "0";
            var api = $"置好友添加请求|Friend_SetApply|{_robotQQ}|{userId}|{action}";
            var response = await SendApiAsync(api);

            return response.Success;
        }

        /// <summary>
        /// 获取绑定群
        /// API格式: 取绑定群|{机器人号}
        /// </summary>
        public async Task<string> GetBoundGroupAsync()
        {
            if (string.IsNullOrEmpty(_robotQQ)) return null;

            var api = $"取绑定群|{_robotQQ}";
            var response = await SendApiAsync(api);

            return response.Success ? response.ResultJson : null;
        }

        /// <summary>
        /// 获取在线账号
        /// API格式: 云信_获取在线账号
        /// </summary>
        public async Task<string> GetOnlineAccountsAsync()
        {
            var api = "云信_获取在线账号";
            var response = await SendApiAsync(api);

            return response.Success ? response.ResultJson : null;
        }

        /// <summary>
        /// ID互查 (旺商聊号 ↔ NIM accid)
        /// API格式: ww_ID互查|{机器人号}|{旺商聊号}
        /// </summary>
        public async Task<string> LookupIdAsync(string wangshangliaoId)
        {
            if (string.IsNullOrEmpty(_robotQQ)) return null;

            var api = $"ww_ID互查|{_robotQQ}|{wangshangliaoId}";
            var response = await SendApiAsync(api);

            return response.Success ? response.ResultJson : null;
        }

        /// <summary>
        /// 授权验证
        /// API格式: ww_xp限制接口|{状态}|{软件ID}|{授权码}|{用户名}|{时间1}|{时间2}
        /// </summary>
        public async Task<bool> VerifyAuthAsync(string softwareId, string authCode, string username)
        {
            var api = $"ww_xp限制接口|真|{softwareId}|{authCode}|{username}|0|0";
            var response = await SendApiAsync(api, 15000);

            return response.Success;
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 设置机器人QQ
        /// </summary>
        public void SetRobotQQ(string robotQQ)
        {
            _robotQQ = robotQQ;
            Log($"[XPlugin] 设置机器人QQ: {robotQQ}");
        }

        /// <summary>
        /// 解码Base64返回值
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

        private void Log(string message)
        {
            OnLog?.Invoke(message);
            WangShangLiaoBot.Services.Logger.Info($"[XPlugin] {message}");
        }

        public void Dispose()
        {
            Disconnect();
        }

        #endregion
    }

    #region 数据模型

    /// <summary>
    /// XPlugin API响应 - 支持ZCG真实协议
    /// </summary>
    public class XPluginResponse
    {
        public bool Success { get; set; }
        public string ApiName { get; set; }
        public string ApiInternalName { get; set; }
        public string ResultJson { get; set; }
        public string RawLine { get; set; }
        public string Error { get; set; }

        /// <summary>
        /// 获取返回码
        /// </summary>
        public int GetCode()
        {
            if (string.IsNullOrEmpty(ResultJson)) return 0;
            try
            {
                // 简单提取 "code": 值
                var match = System.Text.RegularExpressions.Regex.Match(ResultJson, @"""code""\s*:\s*(-?\d+)");
                if (match.Success && int.TryParse(match.Groups[1].Value, out var code))
                    return code;
                return 0;
            }
            catch
            {
                return -1;
            }
        }

        /// <summary>
        /// 获取返回消息
        /// </summary>
        public string GetMessage()
        {
            if (string.IsNullOrEmpty(ResultJson)) return "";
            try
            {
                var match = System.Text.RegularExpressions.Regex.Match(ResultJson, @"""msg""\s*:\s*""([^""]+)""");
                if (match.Success)
                    return match.Groups[1].Value;
                return "";
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// 获取data部分
        /// </summary>
        public string GetData()
        {
            if (string.IsNullOrEmpty(ResultJson)) return null;
            try
            {
                var dataIndex = ResultJson.IndexOf("\"data\":");
                if (dataIndex >= 0)
                {
                    var start = ResultJson.IndexOf('{', dataIndex);
                    if (start >= 0)
                    {
                        // 简单提取到匹配的 }
                        var depth = 0;
                        for (var i = start; i < ResultJson.Length; i++)
                        {
                            if (ResultJson[i] == '{') depth++;
                            else if (ResultJson[i] == '}') depth--;
                            if (depth == 0)
                                return ResultJson.Substring(start, i - start + 1);
                        }
                    }
                }
                return null;
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>
    /// XPlugin消息 - 与ZCG原版投递格式完全兼容
    /// </summary>
    public class XPluginMessage
    {
        /// <summary>机器人账号 (RQQ)</summary>
        public string RobotQQ { get; set; }

        /// <summary>主动账号 (发送者)</summary>
        public string FromQQ { get; set; }

        /// <summary>被动账号 (被@或被操作者)</summary>
        public string ToQQ { get; set; }

        /// <summary>群号</summary>
        public string GroupId { get; set; }

        /// <summary>内容</summary>
        public string Content { get; set; }

        /// <summary>消息ID</summary>
        public string MessageId { get; set; }

        /// <summary>消息类型 (1001私聊/1002群聊/1003系统)</summary>
        public int MessageType { get; set; }

        /// <summary>消息时间戳</summary>
        public long Timestamp { get; set; }

        /// <summary>消息子类型</summary>
        public string SubType { get; set; }

        /// <summary>原始消息JSON</summary>
        public string RawJson { get; set; }

        /// <summary>
        /// 解析消息投递字符串
        /// 格式: 机器人账号={RQQ}，主动账号={fromQQ}，被动账号={toQQ}，群号={groupId}，
        ///       内容={content}，消息ID={msgId}，消息类型={msgType}，消息时间={timestamp}，
        ///       消息子类型={subType}，原始消息={rawJson}
        /// </summary>
        public static XPluginMessage Parse(string line)
        {
            if (string.IsNullOrEmpty(line) || !line.Contains("机器人账号="))
                return null;

            try
            {
                var msg = new XPluginMessage();

                // 使用正则提取各字段
                msg.RobotQQ = ExtractField(line, "机器人账号");
                msg.FromQQ = ExtractField(line, "主动账号");
                msg.ToQQ = ExtractField(line, "被动账号");
                msg.GroupId = ExtractField(line, "群号");
                msg.Content = ExtractField(line, "内容");
                msg.MessageId = ExtractField(line, "消息ID");
                msg.SubType = ExtractField(line, "消息子类型");

                var msgTypeStr = ExtractField(line, "消息类型");
                if (int.TryParse(msgTypeStr, out var msgType))
                    msg.MessageType = msgType;

                var timestampStr = ExtractField(line, "消息时间");
                if (long.TryParse(timestampStr, out var timestamp))
                    msg.Timestamp = timestamp;

                // 原始消息在最后，需要特殊处理
                var rawIndex = line.IndexOf("原始消息=");
                if (rawIndex >= 0)
                {
                    msg.RawJson = line.Substring(rawIndex + 5);
                }

                return msg;
            }
            catch
            {
                return null;
            }
        }

        private static string ExtractField(string line, string fieldName)
        {
            var startPattern = fieldName + "=";
            var startIndex = line.IndexOf(startPattern);
            if (startIndex < 0) return "";

            startIndex += startPattern.Length;
            var endIndex = line.IndexOf("，", startIndex);
            if (endIndex < 0) endIndex = line.Length;

            return line.Substring(startIndex, endIndex - startIndex);
        }

        /// <summary>是否为群消息</summary>
        public bool IsGroupMessage => MessageType == XPluginProtocol.MSG_TYPE_GROUP;

        /// <summary>是否为私聊消息</summary>
        public bool IsPrivateMessage => MessageType == XPluginProtocol.MSG_TYPE_PRIVATE;
    }

    #endregion
}
