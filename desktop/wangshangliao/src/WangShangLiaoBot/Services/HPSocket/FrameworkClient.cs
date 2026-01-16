using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using HPSocket;
using HPSocket.Tcp;
using WangShangLiaoBot.Utils;

namespace WangShangLiaoBot.Services.HPSocket
{
    /// <summary>
    /// 框架消息类型 (与服务端保持一致)
    /// </summary>
    public enum FrameworkMessageType
    {
        // 连接管理
        Login = 1,
        LoginResult = 2,
        Heartbeat = 3,
        Logout = 4,
        
        // API调用
        ApiRequest = 10,
        ApiResponse = 11,
        ReceiveMessage = 12, // 旧版兼容
        
        // 发送消息
        SendGroupMessage = 20,
        SendPrivateMessage = 21,
        
        // 接收消息 (副框架转发)
        ReceiveGroupMessage = 30,
        ReceivePrivateMessage = 31,
        
        // 消息队列
        MessageQueue = 40,
        
        // 群操作
        GroupOperation = 50,
        GetGroupList = 51,
        GetGroupMembers = 52,
        GetGroupInfo = 53,
        
        // 算账控制
        StartAccounting = 60,
        StopAccounting = 61,
        SetActiveGroup = 62,
        GetBoundGroup = 63,
        GetAccountInfo = 64,
        AccountInfo = 65,
        
        // ===== 配置同步消息类型 (70-79) =====
        /// <summary>同步全部配置</summary>
        SyncFullConfig = 70,
        /// <summary>同步赔率配置</summary>
        SyncOddsConfig = 71,
        /// <summary>同步封盘配置</summary>
        SyncSealingConfig = 72,
        /// <summary>同步托管配置</summary>
        SyncTrusteeConfig = 73,
        /// <summary>同步自动回复配置</summary>
        SyncAutoReplyConfig = 74,
        /// <summary>同步话术模板配置</summary>
        SyncTemplateConfig = 75,
        /// <summary>同步基本设置</summary>
        SyncBasicConfig = 76,
        /// <summary>配置同步响应</summary>
        SyncConfigResponse = 79,
        
        // ===== 开奖相关消息类型 (80-89) =====
        /// <summary>开奖结果通知</summary>
        LotteryResult = 80,
        /// <summary>封盘通知</summary>
        SealingNotify = 81,
        /// <summary>封盘提醒通知</summary>
        ReminderNotify = 82,
        /// <summary>期号更新</summary>
        PeriodUpdate = 83,
        
        // 系统
        Error = 90,
        Notification = 91,
        CDPCommand = 100,
        CDPResponse = 101
    }
    
    /// <summary>
    /// 框架消息
    /// </summary>
    public class FrameworkMessage
    {
        public int Id { get; set; }
        public FrameworkMessageType Type { get; set; }
        public string SenderId { get; set; }
        public string ReceiverId { get; set; }
        public string GroupId { get; set; }          // 群ID
        public string LoginAccount { get; set; }     // 登录账号 (RQQ)
        public string Content { get; set; }
        public string Extra { get; set; }
        public long Timestamp { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        
        public FrameworkMessage()
        {
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            Success = true;
        }
        
        public FrameworkMessage(FrameworkMessageType type, string content = null) : this()
        {
            Type = type;
            Content = content;
        }
        
        public string ToJson()
        {
            var serializer = new JavaScriptSerializer();
            return serializer.Serialize(this);
        }
        
        public static FrameworkMessage FromJson(string json)
        {
            try
            {
                var serializer = new JavaScriptSerializer();
                return serializer.Deserialize<FrameworkMessage>(json);
            }
            catch
            {
                return null;
            }
        }
    }
    
    /// <summary>
    /// 框架客户端 - 连接到旺商聊框架服务端
    /// </summary>
    public class FrameworkClient : IDisposable
    {
        private static FrameworkClient _instance;
        private static readonly object _lock = new object();
        
        public static FrameworkClient Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                            _instance = new FrameworkClient();
                    }
                }
                return _instance;
            }
        }
        
        private TcpPackClient _client;
        private int _messageId = 0;
        private System.Timers.Timer _heartbeatTimer;
        
        // 连接配置
        public string Host { get; set; } = "127.0.0.1";
        public ushort Port { get; set; } = 14746;
        public bool IsConnected { get; private set; }
        
        /// <summary>登录账号 (机器人QQ/RQQ)</summary>
        public string LoginAccount { get; set; }
        
        // 事件
        public event Action<bool> OnConnectionChanged;
        public event Action<FrameworkMessage> OnMessageReceived;
        public event Action<string> OnLog;
        
        private FrameworkClient()
        {
            InitializeClient();
            InitializeHeartbeat();
        }
        
        private void InitializeClient()
        {
            _client = new TcpPackClient();
            
            // Pack 模式设置
            _client.PackHeaderFlag = 0xFF;
            _client.MaxPackSize = 0x100000;
            
            // ★★★ 保持连接设置 - 防止空闲断开 ★★★
            _client.KeepAliveTime = 60000;      // 60秒后开始发送心跳
            _client.KeepAliveInterval = 20000;  // 每20秒发送一次心跳
            
            // 绑定事件
            _client.OnConnect += Client_OnConnect;
            _client.OnReceive += Client_OnReceive;
            _client.OnClose += Client_OnClose;
        }
        
        private void InitializeHeartbeat()
        {
            _heartbeatTimer = new System.Timers.Timer(30000); // 30秒
            _heartbeatTimer.Elapsed += async (s, e) =>
            {
                if (IsConnected)
                {
                    await SendHeartbeatAsync();
                }
            };
        }
        
        /// <summary>
        /// 连接到框架服务端
        /// 【优化】减少等待时间，使用轮询检查连接状态
        /// </summary>
        public async Task<bool> ConnectAsync(string host = null, ushort? port = null)
        {
            if (host != null) Host = host;
            if (port != null) Port = port.Value;
            
            return await Task.Run(() =>
            {
                try
                {
                    Log($"正在连接到框架服务端 {Host}:{Port}...");
                    
                    if (_client.Connect(Host, Port))
                    {
                        Log("连接请求已发送");
                        
                        // 【优化】使用轮询检查，最多等待500ms，每50ms检查一次
                        for (int i = 0; i < 10; i++)
                        {
                            if (IsConnected)
                            {
                                _heartbeatTimer.Start();
                                Log($"连接成功 (耗时约 {i * 50}ms)");
                                return true;
                            }
                            Thread.Sleep(50);
                        }
                        
                        // 超时后再检查一次
                        if (IsConnected)
                        {
                            _heartbeatTimer.Start();
                            return true;
                        }
                    }
                    
                    Log($"连接失败: {_client.ErrorMessage}");
                    return false;
                }
                catch (Exception ex)
                {
                    Log($"连接异常: {ex.Message}");
                    return false;
                }
            });
        }
        
        /// <summary>
        /// 断开连接
        /// </summary>
        public void Disconnect()
        {
            try
            {
                _heartbeatTimer?.Stop();
                
                if (_client != null && IsConnected)
                {
                    _client.Stop();
                    Log("已断开与框架的连接");
                }
            }
            catch (Exception ex)
            {
                Log($"断开连接异常: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 发送消息（同步）
        /// </summary>
        public bool Send(FrameworkMessage message)
        {
            if (!IsConnected)
            {
                Log("未连接到框架服务端");
                return false;
            }
            
            try
            {
                if (message.Id == 0)
                    message.Id = Interlocked.Increment(ref _messageId);
                
                var json = message.ToJson();
                var bytes = Encoding.UTF8.GetBytes(json);
                return _client.Send(bytes, bytes.Length);
            }
            catch (Exception ex)
            {
                Log($"发送消息异常: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 发送消息（异步）- 通用方法
        /// </summary>
        public async Task<bool> SendAsync(FrameworkMessage message)
        {
            return await Task.Run(() => Send(message));
        }
        
        /// <summary>
        /// 发送原始字节数据（异步）
        /// </summary>
        public async Task<bool> SendAsync(byte[] data)
        {
            if (!IsConnected)
            {
                Log("未连接到框架服务端");
                return false;
            }
            
            return await Task.Run(() =>
            {
                try
                {
                    return _client.Send(data, data.Length);
                }
                catch (Exception ex)
                {
                    Log($"发送数据异常: {ex.Message}");
                    return false;
                }
            });
        }
        
        /// <summary>
        /// 发送字符串消息（异步）
        /// </summary>
        public async Task<bool> SendAsync(string content, FrameworkMessageType type)
        {
            var message = new FrameworkMessage(type, content);
            return await SendAsync(message);
        }
        
        /// <summary>
        /// 发送登录请求
        /// </summary>
        public async Task<bool> LoginAsync(string clientId = null)
        {
            var message = new FrameworkMessage(FrameworkMessageType.Login, clientId ?? Environment.MachineName);
            return await Task.Run(() => Send(message));
        }
        
        /// <summary>
        /// 发送心跳
        /// </summary>
        public async Task SendHeartbeatAsync()
        {
            var message = new FrameworkMessage(FrameworkMessageType.Heartbeat);
            await Task.Run(() => Send(message));
        }
        
        /// <summary>
        /// 发送群消息
        /// </summary>
        public async Task<bool> SendGroupMessageAsync(string groupId, string content)
        {
            var message = new FrameworkMessage(FrameworkMessageType.SendGroupMessage, content)
            {
                ReceiverId = groupId
            };
            return await Task.Run(() => Send(message));
        }
        
        /// <summary>
        /// 发送私聊消息
        /// </summary>
        public async Task<bool> SendPrivateMessageAsync(string userId, string content)
        {
            var message = new FrameworkMessage(FrameworkMessageType.SendPrivateMessage, content)
            {
                ReceiverId = userId
            };
            return await Task.Run(() => Send(message));
        }
        
        /// <summary>
        /// 发送 CDP 命令
        /// </summary>
        public async Task<bool> SendCDPCommandAsync(string jsCode)
        {
            var message = new FrameworkMessage(FrameworkMessageType.CDPCommand, jsCode);
            return await Task.Run(() => Send(message));
        }
        
        /// <summary>
        /// 开始算账 - 通知副框架接管群聊
        /// </summary>
        public async Task<bool> StartAccountingAsync(string groupId = null)
        {
            Log($"发送开始算账命令, 群ID: {groupId ?? "(默认)"}");
            var message = new FrameworkMessage(FrameworkMessageType.StartAccounting, groupId ?? "")
            {
                GroupId = groupId
            };
            return await Task.Run(() => Send(message));
        }
        
        /// <summary>
        /// 停止算账 - 通知副框架停止接管
        /// </summary>
        public async Task<bool> StopAccountingAsync()
        {
            Log("发送停止算账命令");
            var message = new FrameworkMessage(FrameworkMessageType.StopAccounting);
            return await Task.Run(() => Send(message));
        }
        
        /// <summary>
        /// 设置活跃群 - 通知副框架当前操作的群
        /// </summary>
        public async Task<bool> SetActiveGroupAsync(string groupId)
        {
            if (string.IsNullOrEmpty(groupId))
            {
                Log("群ID不能为空");
                return false;
            }
            Log($"设置活跃群: {groupId}");
            var message = new FrameworkMessage(FrameworkMessageType.SetActiveGroup, groupId)
            {
                GroupId = groupId
            };
            return await Task.Run(() => Send(message));
        }
        
        /// <summary>
        /// 从副框架获取绑定群号和群名
        /// </summary>
        public async Task<(string GroupId, string GroupName)> GetBoundGroupAsync()
        {
            Log("获取副框架绑定群号...");
            
            // 创建等待响应的TaskCompletionSource
            var tcs = new System.Threading.Tasks.TaskCompletionSource<(string, string)>();
            
            // 临时存储响应处理器
            Action<FrameworkMessage> handler = null;
            var handlerRemoved = false;
            var handlerLock = new object();
            
            handler = (msg) =>
            {
                if (msg.Type == FrameworkMessageType.GetBoundGroup && !string.IsNullOrEmpty(msg.Content))
                {
                    lock (handlerLock)
                    {
                        if (handlerRemoved) return;
                        handlerRemoved = true;
                        OnBoundGroupReceived -= handler;
                    }
                    
                    try
                    {
                        var parts = msg.Content.Split('|');
                        var groupId = parts.Length > 0 ? parts[0] : "";
                        var groupName = parts.Length > 1 ? parts[1] : "";
                        tcs.TrySetResult((groupId, groupName));
                    }
                    catch
                    {
                        tcs.TrySetResult(("", ""));
                    }
                }
            };
            OnBoundGroupReceived += handler;
            
            // 发送请求
            var message = new FrameworkMessage(FrameworkMessageType.GetBoundGroup, "");
            Send(message);
            
            // 等待响应，超时3秒
            var timeoutTask = System.Threading.Tasks.Task.Delay(3000);
            var completedTask = await System.Threading.Tasks.Task.WhenAny(tcs.Task, timeoutTask);
            
            if (completedTask == timeoutTask)
            {
                // 安全移除handler，防止重复移除
                lock (handlerLock)
                {
                    if (!handlerRemoved)
                    {
                        handlerRemoved = true;
                OnBoundGroupReceived -= handler;
                    }
                }
                Log("获取绑定群号超时");
                return ("", "");
            }
            
            var result = await tcs.Task;
            Log($"获取到绑定群号: {result.Item1}, 群名: {result.Item2}");
            return result;
        }
        
        /// <summary>绑定群号响应事件（内部使用）</summary>
        private event Action<FrameworkMessage> OnBoundGroupReceived;
        
        /// <summary>
        /// 从副框架获取完整账号信息
        /// </summary>
        public async Task<FrameworkAccountInfo> GetAccountInfoAsync()
        {
            Log("获取副框架账号信息...");
            
            var tcs = new System.Threading.Tasks.TaskCompletionSource<FrameworkAccountInfo>();
            var handlerRemoved = false;
            var handlerLock = new object();
            
            Action<FrameworkMessage> handler = null;
            handler = (msg) =>
            {
                if (msg.Type == FrameworkMessageType.AccountInfo)
                {
                    lock (handlerLock)
                    {
                        if (handlerRemoved) return;
                        handlerRemoved = true;
                        OnAccountInfoReceived -= handler;
                    }
                    
                    try
                    {
                        var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
                        var info = serializer.Deserialize<FrameworkAccountInfo>(msg.Content);
                        tcs.TrySetResult(info);
                    }
                    catch
                    {
                        tcs.TrySetResult(null);
                    }
                }
            };
            OnAccountInfoReceived += handler;
            
            // 发送请求
            var message = new FrameworkMessage(FrameworkMessageType.GetAccountInfo, "");
            Send(message);
            
            // 等待响应，超时5秒
            var timeoutTask = System.Threading.Tasks.Task.Delay(5000);
            var completedTask = await System.Threading.Tasks.Task.WhenAny(tcs.Task, timeoutTask);
            
            if (completedTask == timeoutTask)
            {
                lock (handlerLock)
                {
                    if (!handlerRemoved)
                    {
                        handlerRemoved = true;
                OnAccountInfoReceived -= handler;
                    }
                }
                Log("获取账号信息超时");
                return null;
            }
            
            var result = await tcs.Task;
            if (result != null)
            {
                Log($"获取到账号信息: {result.Nickname} (WWID: {result.Wwid})");
            }
            return result;
        }
        
        /// <summary>账号信息响应事件（内部使用）</summary>
        private event Action<FrameworkMessage> OnAccountInfoReceived;
        
        /// <summary>
        /// 发送群操作指令到副框架（禁言/解禁/踢人等）
        /// </summary>
        /// <param name="operation">操作类型: mute_all, unmute_all, mute_member, kick_member</param>
        /// <param name="groupId">群号</param>
        /// <param name="memberId">成员ID（禁言/踢人时需要）</param>
        public async Task<(bool Success, string Message)> SendGroupOperationAsync(string operation, string groupId, string memberId)
        {
            Log($"发送群操作: {operation}, 群号: {groupId}, 成员: {memberId ?? "(全体)"}");
            
            var tcs = new System.Threading.Tasks.TaskCompletionSource<(bool, string)>();
            var handlerRemoved = false;
            var handlerLock = new object();
            
            Action<FrameworkMessage> handler = null;
            handler = (msg) =>
            {
                if (msg.Type == FrameworkMessageType.GroupOperation)
                {
                    lock (handlerLock)
                    {
                        if (handlerRemoved) return;
                        handlerRemoved = true;
                    OnGroupOperationResult -= handler;
                    }
                    tcs.TrySetResult((msg.Success, msg.ErrorMessage ?? msg.Content ?? ""));
                }
            };
            OnGroupOperationResult += handler;
            
            // 构建消息
            var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
            var content = serializer.Serialize(new
            {
                Operation = operation,
                GroupId = groupId,
                MemberId = memberId
            });
            
            var message = new FrameworkMessage(FrameworkMessageType.GroupOperation, content);
            Send(message);
            
            // 等待响应，超时10秒
            var timeoutTask = System.Threading.Tasks.Task.Delay(10000);
            var completedTask = await System.Threading.Tasks.Task.WhenAny(tcs.Task, timeoutTask);
            
            if (completedTask == timeoutTask)
            {
                lock (handlerLock)
                {
                    if (!handlerRemoved)
                    {
                        handlerRemoved = true;
                OnGroupOperationResult -= handler;
                    }
                }
                Log("群操作超时");
                return (false, "操作超时");
            }
            
            return await tcs.Task;
        }
        
        /// <summary>群操作结果事件（内部使用）</summary>
        private event Action<FrameworkMessage> OnGroupOperationResult;
        
        #region 配置同步方法

        /// <summary>
        /// 同步全量配置到副框架
        /// </summary>
        public async Task<bool> SyncFullConfigAsync(string configJson)
        {
            Log("同步全量配置到副框架...");
            var message = new FrameworkMessage(FrameworkMessageType.SyncFullConfig, configJson)
            {
                Extra = "full"
            };
            return await SendAsync(message);
        }

        /// <summary>
        /// 同步赔率配置
        /// </summary>
        public async Task<bool> SyncOddsConfigAsync(string oddsJson)
        {
            Log("同步赔率配置...");
            var message = new FrameworkMessage(FrameworkMessageType.SyncOddsConfig, oddsJson)
            {
                Extra = "odds"
            };
            return await SendAsync(message);
        }

        /// <summary>
        /// 同步封盘配置
        /// </summary>
        public async Task<bool> SyncSealingConfigAsync(string sealingJson)
        {
            Log("同步封盘配置...");
            var message = new FrameworkMessage(FrameworkMessageType.SyncSealingConfig, sealingJson)
            {
                Extra = "sealing"
            };
            return await SendAsync(message);
        }

        /// <summary>
        /// 同步托管配置
        /// </summary>
        public async Task<bool> SyncTrusteeConfigAsync(string trusteeJson)
        {
            Log("同步托管配置...");
            var message = new FrameworkMessage(FrameworkMessageType.SyncTrusteeConfig, trusteeJson)
            {
                Extra = "trustee"
            };
            return await SendAsync(message);
        }

        /// <summary>
        /// 同步自动回复配置
        /// </summary>
        public async Task<bool> SyncAutoReplyConfigAsync(bool enabled, string rulesJson)
        {
            Log($"同步自动回复配置 (启用={enabled})...");
            var message = new FrameworkMessage(FrameworkMessageType.SyncAutoReplyConfig, rulesJson)
            {
                Success = enabled,
                Extra = "autoreply"
            };
            return await SendAsync(message);
        }

        /// <summary>
        /// 同步话术模板配置
        /// </summary>
        public async Task<bool> SyncTemplateConfigAsync(string templateJson)
        {
            Log("同步话术模板配置...");
            var message = new FrameworkMessage(FrameworkMessageType.SyncTemplateConfig, templateJson)
            {
                Extra = "template"
            };
            return await SendAsync(message);
        }

        /// <summary>
        /// 同步基本设置
        /// </summary>
        public async Task<bool> SyncBasicConfigAsync(string groupId, string adminId, string myWwid, int debugPort)
        {
            Log($"同步基本配置 (群号={groupId})...");
            var message = new FrameworkMessage(FrameworkMessageType.SyncBasicConfig, $"{groupId}|{adminId}|{myWwid}|{debugPort}")
            {
                GroupId = groupId,
                Extra = "basic"
            };
            return await SendAsync(message);
        }

        /// <summary>
        /// 发送开奖结果通知
        /// </summary>
        public async Task<bool> SendLotteryResultAsync(string period, int num1, int num2, int num3, int sum, int countdown)
        {
            Log($"发送开奖结果: 期号={period}, 号码={num1}+{num2}+{num3}={sum}");
            var message = new FrameworkMessage(FrameworkMessageType.LotteryResult, $"{period}|{num1},{num2},{num3}|{sum}|{countdown}")
            {
                Extra = $"{{\"period\":\"{period}\",\"num1\":{num1},\"num2\":{num2},\"num3\":{num3},\"sum\":{sum},\"countdown\":{countdown}}}"
            };
            return await SendAsync(message);
        }

        /// <summary>
        /// 发送封盘通知
        /// </summary>
        public async Task<bool> SendSealingNotifyAsync(string period, string content)
        {
            Log($"发送封盘通知: 期号={period}");
            var message = new FrameworkMessage(FrameworkMessageType.SealingNotify, content)
            {
                Extra = period
            };
            return await SendAsync(message);
        }

        /// <summary>
        /// 发送封盘提醒
        /// </summary>
        public async Task<bool> SendReminderNotifyAsync(string period, int secondsToSeal, string content)
        {
            Log($"发送封盘提醒: 期号={period}, 剩余={secondsToSeal}秒");
            var message = new FrameworkMessage(FrameworkMessageType.ReminderNotify, content)
            {
                Extra = $"{{\"period\":\"{period}\",\"seconds\":{secondsToSeal}}}"
            };
            return await SendAsync(message);
        }

        /// <summary>
        /// 发送期号更新
        /// </summary>
        public async Task<bool> SendPeriodUpdateAsync(string currentPeriod, string nextPeriod, int countdown)
        {
            var message = new FrameworkMessage(FrameworkMessageType.PeriodUpdate, $"{currentPeriod}|{nextPeriod}|{countdown}")
            {
                Extra = "period"
            };
            return await SendAsync(message);
        }

        /// <summary>
        /// 发送群操作通知（禁言/解禁）给副框架记录
        /// </summary>
        /// <param name="groupId">群号</param>
        /// <param name="isMute">true=禁言, false=解禁</param>
        /// <param name="groupName">群名（可选）</param>
        /// <param name="isAutomatic">是否自动操作</param>
        public async Task<bool> NotifyGroupMuteOperationAsync(string groupId, bool isMute, string groupName = null, bool isAutomatic = false)
        {
            var action = isMute ? "禁言" : "解禁";
            var mode = isAutomatic ? "自动" : "手动";
            Log($"通知副框架群操作: {mode}{action} 群号={groupId}");
            
            var json = $"{{\"groupId\":\"{groupId}\",\"groupName\":\"{groupName ?? ""}\",\"isMute\":{isMute.ToString().ToLower()},\"isAutomatic\":{isAutomatic.ToString().ToLower()},\"time\":\"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\"}}";
            var message = new FrameworkMessage(FrameworkMessageType.GroupOperation, json)
            {
                GroupId = groupId,
                Extra = isMute ? "mute" : "unmute"
            };
            return await SendAsync(message);
        }
        
        /// <summary>
        /// 【新方法】请求副框架执行群禁言/解禁
        /// 不再通过主框架CDP，而是让副框架使用其登录的账号执行
        /// </summary>
        /// <param name="groupId">群号</param>
        /// <param name="isMute">true=禁言, false=解禁</param>
        /// <param name="memberId">成员ID（为空则全体禁言）</param>
        /// <param name="isAutomatic">是否自动操作</param>
        /// <returns>操作结果</returns>
        public async Task<(bool Success, string Message)> MuteGroupViaFrameworkAsync(string groupId, bool isMute, string memberId = null, bool isAutomatic = false)
        {
            if (!IsConnected)
            {
                return (false, "未连接到副框架");
            }
            
            var action = isMute ? "禁言" : "解禁";
            var mode = isAutomatic ? "自动" : "手动";
            Log($"请求副框架执行{mode}{action}: 群号={groupId}, 成员={memberId ?? "全体"}");
            
            var json = $"{{\"groupId\":\"{groupId}\",\"memberId\":\"{memberId ?? ""}\",\"isMute\":{isMute.ToString().ToLower()},\"isAutomatic\":{isAutomatic.ToString().ToLower()},\"time\":\"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\"}}";
            var message = new FrameworkMessage(FrameworkMessageType.GroupOperation, json)
            {
                GroupId = groupId,
                Extra = isMute ? "mute" : "unmute"
            };
            
            // 发送并等待响应
            var sent = await SendAsync(message);
            if (!sent)
            {
                return (false, "发送请求失败");
            }
            
            // 简单返回成功，实际结果由副框架异步处理
            // 如果需要同步等待结果，可以实现请求-响应机制
            return (true, $"{action}请求已发送到副框架");
        }
        
        /// <summary>
        /// 【新方法】请求副框架执行全体禁言
        /// </summary>
        public async Task<(bool Success, string Message)> MuteAllViaFrameworkAsync(string groupId, bool isAutomatic = false)
        {
            return await MuteGroupViaFrameworkAsync(groupId, true, null, isAutomatic);
        }
        
        /// <summary>
        /// 【新方法】请求副框架执行全体解禁
        /// </summary>
        public async Task<(bool Success, string Message)> UnmuteAllViaFrameworkAsync(string groupId, bool isAutomatic = false)
        {
            return await MuteGroupViaFrameworkAsync(groupId, false, null, isAutomatic);
        }

        #endregion

        #region ZCG原版兼容API - 基于逆向分析

        /// <summary>
        /// 【ZCG兼容】发送群消息 - 使用原版API格式
        /// API格式: 发送群消息（文本）|{机器人号}|{内容}|{群号}|{类型}|{标志}
        /// </summary>
        public async Task<bool> SendGroupMessageZcgAsync(string groupId, string content, string robotQQ = null)
        {
            var rqq = robotQQ ?? LoginAccount;
            if (string.IsNullOrEmpty(rqq))
            {
                Log("未设置机器人QQ");
                return false;
            }

            // 转义换行符
            content = content.Replace("\n", "\\n");

            var message = new FrameworkMessage(FrameworkMessageType.ApiRequest, 
                $"发送群消息（文本）|{rqq}|{content}|{groupId}|1|0")
            {
                GroupId = groupId,
                LoginAccount = rqq
            };

            return await SendAsync(message);
        }

        /// <summary>
        /// 【ZCG兼容】发送私聊消息
        /// API格式: 发送好友消息|{机器人号}|{内容}|{目标号}
        /// </summary>
        public async Task<bool> SendPrivateMessageZcgAsync(string userId, string content, string robotQQ = null)
        {
            var rqq = robotQQ ?? LoginAccount;
            if (string.IsNullOrEmpty(rqq)) return false;

            content = content.Replace("\n", "\\n");

            var message = new FrameworkMessage(FrameworkMessageType.ApiRequest,
                $"发送好友消息|{rqq}|{content}|{userId}")
            {
                ReceiverId = userId,
                LoginAccount = rqq
            };

            return await SendAsync(message);
        }

        /// <summary>
        /// 【ZCG兼容】群禁言/解禁
        /// API格式: ww_群禁言解禁|{机器人号}|{群号}|{动作:1禁言/2解禁}
        /// </summary>
        public async Task<bool> MuteGroupZcgAsync(string groupId, bool mute, string robotQQ = null)
        {
            var rqq = robotQQ ?? LoginAccount;
            if (string.IsNullOrEmpty(rqq)) return false;

            var action = mute ? "1" : "2";
            var message = new FrameworkMessage(FrameworkMessageType.ApiRequest,
                $"ww_群禁言解禁|{rqq}|{groupId}|{action}")
            {
                GroupId = groupId,
                LoginAccount = rqq
            };

            Log($"[ZCG兼容] 群{(mute ? "禁言" : "解禁")}: {groupId}");
            return await SendAsync(message);
        }

        /// <summary>
        /// 【ZCG兼容】修改群名片
        /// API格式: ww_改群名片|{机器人号}|{群号}|{用户号}|{新名片}
        /// </summary>
        public async Task<bool> SetMemberCardZcgAsync(string groupId, string userId, string newCard, string robotQQ = null)
        {
            var rqq = robotQQ ?? LoginAccount;
            if (string.IsNullOrEmpty(rqq)) return false;

            var message = new FrameworkMessage(FrameworkMessageType.ApiRequest,
                $"ww_改群名片|{rqq}|{groupId}|{userId}|{newCard}")
            {
                GroupId = groupId,
                LoginAccount = rqq
            };

            return await SendAsync(message);
        }

        /// <summary>
        /// 【ZCG兼容】获取绑定群
        /// API格式: 取绑定群|{机器人号}
        /// </summary>
        public async Task<string> GetBoundGroupZcgAsync(string robotQQ = null)
        {
            var rqq = robotQQ ?? LoginAccount;
            if (string.IsNullOrEmpty(rqq)) return null;

            var message = new FrameworkMessage(FrameworkMessageType.ApiRequest,
                $"取绑定群|{rqq}")
            {
                LoginAccount = rqq
            };

            await SendAsync(message);
            return null; // 异步返回，实际结果通过事件获取
        }

        /// <summary>
        /// 【ZCG兼容】获取在线账号
        /// API格式: 云信_获取在线账号
        /// </summary>
        public async Task<bool> GetOnlineAccountsZcgAsync()
        {
            var message = new FrameworkMessage(FrameworkMessageType.ApiRequest,
                "云信_获取在线账号");

            return await SendAsync(message);
        }

        /// <summary>
        /// 【ZCG兼容】ID互查
        /// API格式: ww_ID互查|{机器人号}|{旺商聊号}
        /// </summary>
        public async Task<bool> LookupIdZcgAsync(string wangshangliaoId, string robotQQ = null)
        {
            var rqq = robotQQ ?? LoginAccount;
            if (string.IsNullOrEmpty(rqq)) return false;

            var message = new FrameworkMessage(FrameworkMessageType.ApiRequest,
                $"ww_ID互查|{rqq}|{wangshangliaoId}")
            {
                LoginAccount = rqq
            };

            return await SendAsync(message);
        }

        #endregion
        
        #region Client Events
        
        private HandleResult Client_OnConnect(IClient sender)
        {
            IsConnected = true;
            Log("已连接到框架服务端");
            OnConnectionChanged?.Invoke(true);
            
            // 自动发送登录请求（异步执行并捕获异常）
            Task.Run(async () =>
            {
                try
                {
                    var loginResult = await LoginAsync();
                    if (!loginResult)
                    {
                        Log("自动登录失败，请检查连接状态");
                    }
                }
                catch (Exception ex)
                {
                    Log($"自动登录异常: {ex.Message}");
                }
            });
            
            return HandleResult.Ok;
        }
        
        private HandleResult Client_OnReceive(IClient sender, byte[] data)
        {
            try
            {
                var json = Encoding.UTF8.GetString(data);
                var message = FrameworkMessage.FromJson(json);
                
                if (message != null)
                {
                    // 心跳消息不记录日志，避免刷屏
                    if (message.Type != FrameworkMessageType.Heartbeat)
                    {
                        Log($"收到框架消息: {message.Type}");
                    }
                    OnMessageReceived?.Invoke(message);
                    
                    // 处理特定消息
                    ProcessMessage(message);
                }
            }
            catch (Exception ex)
            {
                Log($"处理消息异常: {ex.Message}");
            }
            
            return HandleResult.Ok;
        }
        
        private HandleResult Client_OnClose(IClient sender, SocketOperation socketOperation, int errorCode)
        {
            IsConnected = false;
            _heartbeatTimer?.Stop();
            Log($"与框架的连接已断开 (操作: {socketOperation}, 错误码: {errorCode})");
            OnConnectionChanged?.Invoke(false);
            return HandleResult.Ok;
        }
        
        #endregion
        
        private void ProcessMessage(FrameworkMessage message)
        {
            switch (message.Type)
            {
                case FrameworkMessageType.LoginResult:
                    if (message.Success)
                    {
                        Log("登录框架成功");
                    }
                    else
                    {
                        Log($"登录框架失败: {message.ErrorMessage}");
                    }
                    break;
                    
                case FrameworkMessageType.ReceiveMessage:
                    // 处理收到的聊天消息 - 转发到 ChatService
                    Log($"收到聊天消息: GroupId={message.GroupId}, From={message.SenderId}, Content={message.Content}");
                    
                    // 触发消息接收事件，让 ChatService 和其他服务处理
                    OnGroupMessageReceived?.Invoke(message);
                    break;
                    
                case FrameworkMessageType.GetBoundGroup:
                    // 处理获取绑定群号响应
                    Log($"收到绑定群号响应: {message.Content}");
                    OnBoundGroupReceived?.Invoke(message);
                    break;
                    
                case FrameworkMessageType.AccountInfo:
                    // 处理账号信息响应（包括主动推送和请求响应）
                    Log($"收到账号信息");
                    OnAccountInfoReceived?.Invoke(message);
                    
                    // 解析并更新本地配置
                    try
                    {
                        var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
                        var info = serializer.Deserialize<FrameworkAccountInfo>(message.Content);
                        if (info != null)
                        {
                            // 触发账号变化事件，让 UI 可以更新
                            OnAccountChanged?.Invoke(info);
                            Log($"账号信息已更新: {info.Nickname} ({info.AccountId}), 群号: {info.GroupId}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log($"解析账号信息失败: {ex.Message}");
                    }
                    break;
                    
                case FrameworkMessageType.GroupOperation:
                    // 处理群操作结果
                    Log($"收到群操作结果: Success={message.Success}, Message={message.ErrorMessage ?? message.Content}");
                    OnGroupOperationResult?.Invoke(message);
                    break;
                    
                case FrameworkMessageType.Error:
                    Log($"框架错误: {message.ErrorMessage}");
                    break;
                    
                case FrameworkMessageType.Notification:
                    Log($"框架通知: {message.Content}");
                    break;
            }
        }
        
        /// <summary>
        /// 群消息接收事件 - 用于接管群聊
        /// </summary>
        public event Action<FrameworkMessage> OnGroupMessageReceived;
        
        /// <summary>
        /// 账号变化事件 - 副框架换账号时触发
        /// </summary>
        public event Action<FrameworkAccountInfo> OnAccountChanged;
        
        private void Log(string message)
        {
            var logMessage = $"[框架客户端] {message}";
            Logger.Info(logMessage);
            OnLog?.Invoke(logMessage);
        }
        
        public void Dispose()
        {
            Disconnect();
            _heartbeatTimer?.Dispose();
            _client?.Dispose();
        }
    }
    
    /// <summary>
    /// 副框架账号信息
    /// </summary>
    public class FrameworkAccountInfo
    {
        /// <summary>账号ID (用户看到的账号)</summary>
        public string AccountId { get; set; }
        
        /// <summary>旺旺号 (内部ID)</summary>
        public string Wwid { get; set; }
        
        /// <summary>昵称</summary>
        public string Nickname { get; set; }
        
        /// <summary>绑定群号</summary>
        public string GroupId { get; set; }
        
        /// <summary>群名称</summary>
        public string GroupName { get; set; }
        
        /// <summary>NIM ID</summary>
        public string NimId { get; set; }
        
        /// <summary>是否已登录</summary>
        public bool IsLoggedIn { get; set; }
    }
}
