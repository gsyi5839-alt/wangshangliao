using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HPSocket;
using HPSocket.Tcp;
using WSLFramework.Models;
using WSLFramework.Protocol;
using WSLFramework.Utils;
using WSLFramework.Services.EventDriven;

namespace WSLFramework.Services
{
    /// <summary>
    /// 框架服务端 - 使用 HPSocket PACK 模式实现
    /// 完全匹配招财狗ZCG协议，增强异常处理和线程安全
    /// </summary>
    public partial class FrameworkServer : IDisposable
    {
        #region 私有字段

        private TcpPackServer _server;
#pragma warning disable CS0649 // CDP已废弃，保留字段兼容旧代码
        private CDPBridge _cdpBridge;
#pragma warning restore CS0649
        private NIMService _nimService;  // NIM SDK 服务 (直接发送消息)
        private ZCGApiHandler _apiHandler;
        private PeriodManager _periodManager;
        private GroupManagementService _groupManager;
#pragma warning disable CS0414 // 算账状态由主框架管理
        private bool _isAccountingStarted = false;
#pragma warning restore CS0414
        private string _activeGroupId;              // 活跃群号
        private bool _useNIMForSending = true;      // 是否使用NIM发送消息
        
        // 新增: 事件驱动系统 (参考Lagrange.Core架构)
        private WSLBotContext _botContext;
        private bool _useEventDrivenMode = false;   // 是否启用事件驱动模式
        
        // 线程安全集合
        private readonly ConcurrentDictionary<IntPtr, ClientInfo> _clients;
        private readonly ConcurrentQueue<FrameworkMessage> _messageQueue;
        private readonly SemaphoreSlim _cdpLock = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _nimLoginLock = new SemaphoreSlim(1, 1);
        private readonly object _serverLock = new object();

        // 取消令牌
        private CancellationTokenSource _cts;

        // 配置
        private volatile bool _isDisposed;
        private volatile bool _isStarting;

        #endregion

        #region 公共属性

        /// <summary>服务端端口</summary>
        public ushort Port { get; set; } = ZCGProtocol.DEFAULT_PORT;

        /// <summary>是否正在运行</summary>
        public bool IsRunning => _server?.HasStarted ?? false;

        /// <summary>CDP是否已连接</summary>
        public bool IsCDPConnected => _cdpBridge?.IsConnected ?? false;
        
        /// <summary>NIM是否已连接</summary>
        public bool IsNIMConnected => _nimService?.IsLoggedIn ?? false;
        
        /// <summary>CDP桥接实例</summary>
        public CDPBridge CDPBridge => _cdpBridge;
        
        /// <summary>NIM服务实例</summary>
        public NIMService NIMService => _nimService;
        
        /// <summary>PACK模式包头标志 (使用兼容值0xFF)</summary>
        public ushort PackHeaderFlag { get; set; } = ZCGProtocol.PACK_HEADER_FLAG_LEGACY;

        /// <summary>最大包大小</summary>
        public uint MaxPackSize { get; set; } = ZCGProtocol.MAX_PACK_SIZE;

        /// <summary>当前登录账号</summary>
        public string CurrentLoginAccount { get; private set; }

        /// <summary>API处理器</summary>
        public ZCGApiHandler ApiHandler => _apiHandler;
        
        /// <summary>事件驱动上下文 (Lagrange风格)</summary>
        public WSLBotContext BotContext => _botContext;
        
        /// <summary>是否启用事件驱动模式</summary>
        public bool UseEventDrivenMode
        {
            get => _useEventDrivenMode;
            set => _useEventDrivenMode = value;
        }
        
        /// <summary>
        /// 设置活跃群号 - 供外部调用
        /// </summary>
        public void SetActiveGroup(string groupId)
        {
            if (string.IsNullOrEmpty(groupId)) return;
            
            _activeGroupId = groupId;
            TimedMessageService.Instance.AddActiveGroup(groupId);
            NimDirectClient.Instance?.SetActiveGroup(groupId);
            
            Log($"[公共方法] 设置活跃群: {groupId}");
        }
        
        /// <summary>
        /// 处理群消息 - 供外部调用 (BotLoginService 转发)
        /// 完整消息处理流程: 下注解析 → 上下分 → 查询 → 自动回复
        /// </summary>
        public void HandleGroupMessage(string groupId, string fromId, string content)
        {
            try
            {
                Log($"[外部消息] 群消息: {groupId} | {fromId}: {content?.Substring(0, Math.Min(50, content?.Length ?? 0))}");
                
                // 检查是否是活跃群
                if (!string.IsNullOrEmpty(_activeGroupId) && groupId != _activeGroupId)
                {
                    // 不是活跃群，忽略
                    return;
                }
                
                // 触发事件
                OnWangShangLiaoMessage?.Invoke(groupId, fromId, content);
                
                // 转发给主框架
                BroadcastGroupMessage(groupId, fromId, content);
                
                // ★★★ 接入自动回复和下注处理 ★★★
                if (!string.IsNullOrEmpty(content) && _isAccountingStarted)
                {
                    _ = ProcessGroupMessageAsync(groupId, fromId, content);
                }
            }
            catch (Exception ex)
            {
                // 优化: 记录完整异常信息，包含堆栈跟踪
                LogException("[外部消息] 处理群消息异常", ex);
            }
        }
        
        /// <summary>
        /// 异步处理群消息 - 下注/上下分/查询/自动回复
        /// </summary>
        private async Task ProcessGroupMessageAsync(string groupId, string fromId, string content)
        {
            try
            {
                // 1. 使用下注解析器解析消息
                var betParser = BetParserService.Instance;
                var parseResult = betParser.Parse(content);
                
                if (parseResult != null && parseResult.IsValid)
                {
                    // 处理下注/上下分/查询
                    if (parseResult.IsQuery)
                    {
                        // 余额查询
                        await HandleBalanceQueryInternalAsync(groupId, fromId);
                    }
                    else if (parseResult.IsUp)
                    {
                        // 上分请求
                        await HandleUpRequestInternalAsync(groupId, fromId, (int)parseResult.Amount);
                    }
                    else if (parseResult.IsDown)
                    {
                        // 下分请求
                        await HandleDownRequestInternalAsync(groupId, fromId, (int)parseResult.Amount);
                    }
                    else
                    {
                        // 下注请求
                        await HandleBetRequestInternalAsync(groupId, fromId, parseResult);
                    }
                    return;
                }
                
                // 2. 检查自动回复
                var replyService = AutoReplyService.Instance;
                var reply = replyService.GetReply(content);
                if (!string.IsNullOrEmpty(reply))
                {
                    Log($"[自动回复] 触发: {content} -> {reply.Substring(0, Math.Min(30, reply.Length))}...");
                    await SendGroupMessageViaCDPAsync(groupId, reply);
                }
            }
            catch (Exception ex)
            {
                Log($"[消息处理] 异常: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 处理余额查询 (内部方法)
        /// </summary>
        private async Task HandleBalanceQueryInternalAsync(string groupId, string fromId)
        {
            try
            {
                var scoreService = ScoreService.Instance;
                var balance = scoreService.GetBalance(fromId);
                var response = ZCGResponseFormatter.FormatBalanceQuery(fromId, null, balance);
                await SendGroupMessageViaCDPAsync(groupId, response);
                Log($"[余额查询] {fromId} 余额: {balance}");
            }
            catch (Exception ex)
            {
                Log($"[余额查询] 异常: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 处理上分请求 (内部方法)
        /// </summary>
        private async Task HandleUpRequestInternalAsync(string groupId, string fromId, int amount)
        {
            try
            {
                // 上分请求不自动处理，只发送提示等待人工确认
                var response = ZCGResponseFormatter.FormatUpRequest(fromId, amount);
                await SendGroupMessageViaCDPAsync(groupId, response);
                Log($"[上分请求] {fromId} 申请上分: {amount}");
            }
            catch (Exception ex)
            {
                Log($"[上分请求] 异常: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 处理下分请求 (内部方法)
        /// </summary>
        private async Task HandleDownRequestInternalAsync(string groupId, string fromId, int amount)
        {
            try
            {
                var scoreService = ScoreService.Instance;
                var balance = scoreService.GetBalance(fromId);
                
                if (balance < amount)
                {
                    var response = ZCGResponseFormatter.FormatInsufficientBalance(fromId, amount, balance);
                    await SendGroupMessageViaCDPAsync(groupId, response);
                    return;
                }
                
                // 下分请求不自动处理，只发送提示等待人工确认
                var replyResponse = ZCGResponseFormatter.FormatDownRequest(fromId, amount, balance);
                await SendGroupMessageViaCDPAsync(groupId, replyResponse);
                Log($"[下分请求] {fromId} 申请下分: {amount}, 余额: {balance}");
            }
            catch (Exception ex)
            {
                Log($"[下分请求] 异常: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 处理下注请求 (内部方法)
        /// </summary>
        private async Task HandleBetRequestInternalAsync(string groupId, string fromId, BetResult bet)
        {
            try
            {
                var scoreService = ScoreService.Instance;
                var balance = scoreService.GetBalance(fromId);
                
                // 检查余额
                if (balance < (int)bet.Amount)
                {
                    var response = ZCGResponseFormatter.FormatBetInsufficientBalance(
                        fromId, bet.BetType, (int)bet.Amount, balance);
                    await SendGroupMessageViaCDPAsync(groupId, response);
                    return;
                }
                
                // 扣除下注金额
                var deductResult = scoreService.DeductBet(fromId, (int)bet.Amount);
                if (!deductResult)
                {
                    Log($"[下注] 扣除金额失败: {fromId}");
                    return;
                }
                
                // 发送下注成功回复
                var newBalance = scoreService.GetBalance(fromId);
                var response2 = ZCGResponseFormatter.FormatBetSuccess(
                    fromId, bet.BetType, (int)bet.Amount, newBalance);
                await SendGroupMessageViaCDPAsync(groupId, response2);
                
                Log($"[下注成功] {fromId} -> {bet.BetType}{bet.Amount}, 余额: {newBalance}");
            }
            catch (Exception ex)
            {
                Log($"[下注] 异常: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 处理私聊消息 - 供外部调用 (BotLoginService 转发)
        /// </summary>
        public void HandlePrivateMessage(string fromId, string toId, string content)
        {
            try
            {
                Log($"[外部消息] 私聊: {fromId}: {content?.Substring(0, Math.Min(50, content?.Length ?? 0))}");
                
                // 转发给主框架
                BroadcastPrivateMessage(fromId, toId, content);
            }
            catch (Exception ex)
            {
                // 优化: 记录完整异常信息，包含堆栈跟踪
                LogException("[外部消息] 处理私聊消息异常", ex);
            }
        }
        
        /// <summary>
        /// 记录异常日志 (包含完整堆栈信息)
        /// </summary>
        private void LogException(string context, Exception ex)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{context}:");
            sb.AppendLine($"  类型: {ex.GetType().Name}");
            sb.AppendLine($"  消息: {ex.Message}");
            if (ex.InnerException != null)
            {
                sb.AppendLine($"  内部异常: {ex.InnerException.Message}");
            }
            sb.AppendLine($"  堆栈: {ex.StackTrace}");
            
            Log(sb.ToString());
            
            // 同时记录到文件日志
            Logger.Error(context, ex);
        }
        
        /// <summary>
        /// 发送群消息 - 供外部调用 (MainForm 等)
        /// </summary>
        public async Task<bool> SendGroupMessageAsync(string groupId, string content)
        {
            return await SendGroupMessageViaCDPAsync(groupId, content);
        }
        
        /// <summary>
        /// 广播群消息给所有连接的主框架
        /// 优化: 使用快照遍历，保持与其他广播方法一致
        /// </summary>
        private void BroadcastGroupMessage(string groupId, string fromId, string content)
        {
            var clientSnapshot = GetClientSnapshot();
            if (clientSnapshot.Length == 0)
                return;
            
            var msg = new FrameworkMessage
            {
                Type = FrameworkMessageType.ReceiveGroupMessage,
                GroupId = groupId,
                SenderId = fromId,
                Content = content,
                Timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds()
            };
            
            // 使用快照遍历
            foreach (var connId in clientSnapshot)
            {
                if (_clients.ContainsKey(connId))
                    SendToClient(connId, msg);
            }
        }
        
        /// <summary>
        /// 广播私聊消息给所有连接的主框架
        /// 优化: 使用快照遍历，保持与其他广播方法一致
        /// </summary>
        private void BroadcastPrivateMessage(string fromId, string toId, string content)
        {
            var clientSnapshot = GetClientSnapshot();
            if (clientSnapshot.Length == 0)
                return;
            
            var msg = new FrameworkMessage
            {
                Type = FrameworkMessageType.ReceivePrivateMessage,
                SenderId = fromId,
                ReceiverId = toId,
                Content = content,
                Timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds()
            };
            
            // 使用快照遍历
            foreach (var connId in clientSnapshot)
            {
                if (_clients.ContainsKey(connId))
                    SendToClient(connId, msg);
            }
        }
        
        /// <summary>
        /// 账号变化时主动通知所有连接的主框架
        /// </summary>
        private async Task NotifyAccountChangedAsync()
        {
            try
            {
                if (_clients.Count == 0)
                {
                    Log("[账号变化] 没有连接的主框架，跳过通知");
                    return;
                }
                
                Log("[账号变化] 开始通知主框架...");
                
                // 获取最新的账号信息
                var loginService = BotLoginService.Instance;
                var currentAccount = loginService.CurrentAccount;
                var cdp = CDPService.Instance;
                var userInfo = await cdp.GetCurrentUserAsync();
                
                var groupId = _activeGroupId ?? currentAccount?.GroupId ?? "";
                var groupName = "";
                
                if (!string.IsNullOrEmpty(groupId) && cdp.IsConnected)
                {
                    var groups = await cdp.GetGroupListAsync();
                    var group = groups?.FirstOrDefault(g => g.GroupId == groupId || g.InternalId == groupId);
                    if (group != null) groupName = group.Name;
                }
                
                var accountInfo = new
                {
                    AccountId = userInfo?.AccountId ?? currentAccount?.Account ?? "",
                    Wwid = userInfo?.Wwid ?? currentAccount?.Wwid ?? "",
                    Nickname = userInfo?.Nickname ?? currentAccount?.Nickname ?? "",
                    GroupId = groupId,
                    GroupName = groupName,
                    NimId = userInfo?.NimId ?? currentAccount?.NimAccid ?? "",
                    IsLoggedIn = loginService.IsLoggedIn
                };
                
                var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
                var jsonContent = serializer.Serialize(accountInfo);
                
                var msg = new FrameworkMessage
                {
                    Type = FrameworkMessageType.AccountInfo,
                    Content = jsonContent,
                    Success = true
                };
                
                // 广播给所有主框架
                foreach (var connId in _clients.Keys)
                {
                    SendToClient(connId, msg);
                }
                
                Log($"[账号变化] 已通知 {_clients.Count} 个主框架: {accountInfo.Nickname} ({accountInfo.AccountId})");
            }
            catch (Exception ex)
            {
                Log($"[账号变化] 通知失败: {ex.Message}");
            }
        }

        #endregion

        #region 事件

        /// <summary>日志事件</summary>
        public event Action<string> OnLog;

        /// <summary>客户端连接状态变化</summary>
        public event Action<IntPtr, bool> OnClientConnectionChanged;

        /// <summary>消息接收事件</summary>
        public event Action<IntPtr, FrameworkMessage> OnMessageReceived;

        /// <summary>客户端登录事件</summary>
        public event Action<IntPtr, AccountLoginInfo> OnClientLoggedIn;

        /// <summary>旺商聊连接事件</summary>
        public event Action<WangShangLiaoUserInfo, WangShangLiaoGroupInfo[]> OnWangShangLiaoConnected;

        /// <summary>CDP连接状态变化事件</summary>
        public event Action<bool> OnCDPConnectionChanged;

        /// <summary>消息队列推送事件</summary>
        public event Action<ZCGMessageQueue> OnMessageQueueReceived;

        /// <summary>API调用事件</summary>
        public event Action<string, string[], string> OnApiCall;
        
        /// <summary>旺商聊群消息事件 (groupId, fromId, content)</summary>
        public event Action<string, string, string> OnWangShangLiaoMessage;

        #endregion

        #region 构造函数
        
        public FrameworkServer()
        {
            _clients = new ConcurrentDictionary<IntPtr, ClientInfo>();
            _messageQueue = new ConcurrentQueue<FrameworkMessage>();
            _cts = new CancellationTokenSource();
            
            try
            {
                InitializeComponents();
            }
            catch (Exception ex)
            {
                Log($"初始化失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 初始化所有组件
        /// </summary>
        private void InitializeComponents()
        {
            // 初始化 HPSocket
            InitializeHPSocket();

            // 初始化 NIM SDK (优先使用，直接发送消息)
            InitializeNIM();
            
            // 初始化 CDP 桥接 (API不可用时的备用方案)
            InitializeCDP();
            
            // 初始化 BotLoginService (账号密码登录旺商聊)
            InitializeBotLoginService();

            // 初始化 API 处理器
            InitializeApiHandler();
            
            // 初始化群管理服务
            InitializeGroupManager();
            
            // 初始化周期管理和定时消息服务
            InitializePeriodAndTimedServices();
            
            // 初始化事件驱动系统 (Lagrange风格)
            InitializeEventDrivenSystem();

            Log("框架服务端初始化完成");
        }
        
        /// <summary>
        /// 初始化 BotLoginService - 替代 CDP，使用账号密码登录旺商聊
        /// </summary>
        private void InitializeBotLoginService()
        {
            try
            {
                Log("[BotLogin] 初始化账号登录服务...");
                
                var loginService = BotLoginService.Instance;
                loginService.OnLog += msg => Log(msg);
                loginService.OnLoginStateChanged += async (loggedIn, status) =>
                {
                    Log($"[BotLogin] 登录状态变化: {status}");
                    
                    if (loggedIn)
                    {
                        var account = loginService.CurrentAccount;
                        if (account != null)
                        {
                            _activeGroupId = account.GroupId;
                            Log($"[BotLogin] ✓ 已登录: {account.Nickname} ({account.Account}), 绑定群: {account.GroupId}");
                            
                            // 账号变化时主动推送给所有连接的主框架
                            await NotifyAccountChangedAsync();
                        }
                    }
                    else
                    {
                        // 登出时也通知主框架
                        await NotifyAccountChangedAsync();
                    }
                };
                loginService.OnGroupMessage += (groupId, fromId, content) =>
                {
                    // 转发群消息给主框架
                    HandleGroupMessage(groupId, fromId, content);
                };
                loginService.OnPrivateMessage += (fromId, toId, content) =>
                {
                    // 转发私聊消息给主框架
                    HandlePrivateMessage(fromId, toId, content);
                };
                
                // 加载已保存的账号
                Models.AccountManager.Instance.Load();
                var savedAccounts = Models.AccountManager.Instance.Accounts;
                Log($"[BotLogin] 已加载 {savedAccounts.Count} 个已保存的账号");
                
                // 尝试自动登录
                _ = TryAutoLoginAsync();
                
                Log("[BotLogin] ✓ 账号登录服务初始化完成");
            }
            catch (Exception ex)
            {
                Log($"[BotLogin] 初始化异常: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 尝试自动登录已保存的账号
        /// ★★★ 双账号模式: CDP(监控) + NIM SDK(机器人) ★★★
        /// </summary>
        private async System.Threading.Tasks.Task TryAutoLoginAsync()
        {
            try
            {
                var autoLoginAccount = Models.AccountManager.Instance.GetAutoLoginAccount();
                
                // ★★★ 优先使用双账号模式 ★★★
                // CDP连接客户端(监控账号) + NIM SDK直连(机器人账号)
                if (_cdpBridge != null && IsCDPConnected)
                {
                    Log($"[BotLogin] 检测到CDP已连接，尝试双账号模式...");
                    
                    var success = await BotLoginService.Instance.InitDualAccountModeAsync(_cdpBridge, autoLoginAccount);
                    
                    if (success)
                    {
                        var sendMode = BotLoginService.Instance.GetSendMode();
                        Log($"[BotLogin] ✓ 双账号模式初始化完成");
                        Log($"[BotLogin] ✓ 消息发送模式: {sendMode}");
                        
                        // ★★★ 尝试获取群名称 ★★★
                        await TryFetchGroupNameAsync(autoLoginAccount);
                        return;
                    }
                }
                
                // 回退: 单账号模式
                if (autoLoginAccount != null)
                {
                    Log($"[BotLogin] 尝试自动登录: {autoLoginAccount.Account}");
                    await BotLoginService.Instance.LoginAsync(autoLoginAccount);
                    
                    // ★★★ 尝试获取群名称 ★★★
                    await TryFetchGroupNameAsync(autoLoginAccount);
                }
                else
                {
                    Log("[BotLogin] 没有自动登录账号，请在账号列表中添加并登录");
                }
            }
            catch (Exception ex)
            {
                Log($"[BotLogin] 自动登录异常: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 尝试获取群名称并保存
        /// </summary>
        private async System.Threading.Tasks.Task TryFetchGroupNameAsync(Models.BotAccount account)
        {
            if (account == null || string.IsNullOrEmpty(account.GroupId))
                return;
                
            // 如果已有群名称则跳过
            if (!string.IsNullOrEmpty(account.GroupName))
            {
                Log($"[群名称] 已有群名称: {account.GroupName}");
                return;
            }
            
            try
            {
                Log($"[群名称] 正在获取群 {account.GroupId} 的名称...");
                
                // 方式1: 通过CDP获取群列表
                if (_cdpBridge != null && IsCDPConnected)
                {
                    var cdpService = CDPService.Instance;
                    if (cdpService.IsConnected)
                    {
                        var groups = await cdpService.GetGroupListAsync();
                        if (groups != null)
                        {
                            var group = groups.FirstOrDefault(g => 
                                g.GroupId == account.GroupId || g.InternalId == account.GroupId);
                            if (group != null && !string.IsNullOrEmpty(group.Name))
                            {
                                account.GroupName = group.Name;
                                Models.AccountManager.Instance.Save();
                                Log($"[群名称] ✓ 从CDP获取群名称: {group.Name}");
                                return;
                            }
                        }
                    }
                }
                
                Log($"[群名称] 无法自动获取，请手动设置群名称");
            }
            catch (Exception ex)
            {
                Log($"[群名称] 获取失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 初始化事件驱动系统 - 参考Lagrange.Core架构
        /// 使用纯TCP长连接，完全移除CDP依赖
        /// </summary>
        private void InitializeEventDrivenSystem()
        {
            try
            {
                // 创建配置 - 纯TCP长连接模式
                var config = new WSLBotConfig
                {
                    BotId = CurrentLoginAccount ?? "",
                    ServerHost = "127.0.0.1",  // TCP服务器地址
                    ServerPort = 8899,         // TCP端口
                    HeartbeatInterval = 30000, // 30秒心跳
                    AutoReconnect = true,
                    ReconnectInterval = 5000,  // 5秒重连间隔
                    MaxReconnectAttempts = 10
                };
                
                // 创建机器人上下文 - 无CDP依赖
                _botContext = new WSLBotContext(config);
                
                // 注册日志事件
                _botContext.OnLog += msg => Log($"[EventDriven/TCP] {msg}");
                
                // 注册核心事件处理器
                _botContext.Invoker.OnGroupMessageReceived += OnEventDriven_GroupMessage;
                _botContext.Invoker.OnFriendMessageReceived += OnEventDriven_FriendMessage;
                _botContext.Invoker.OnGroupMuteEvent += OnEventDriven_GroupMute;
                _botContext.Invoker.OnBotOnlineEvent += OnEventDriven_BotOnline;
                _botContext.Invoker.OnBotOfflineEvent += OnEventDriven_BotOffline;
                _botContext.Invoker.OnConnectionStateChanged += OnEventDriven_ConnectionState;
                _botContext.Invoker.OnLotteryResultReceived += OnEventDriven_LotteryResult;
                
                Log("事件驱动系统初始化完成 - Lagrange风格纯TCP架构 (无CDP)");
            }
            catch (Exception ex)
            {
                Log($"事件驱动系统初始化失败: {ex.Message}");
            }
        }
        
        #region 事件驱动处理器 (TCP长连接，无CDP)
        
        private void OnEventDriven_GroupMessage(WSLBotContext ctx, EventDriven.GroupMessageEvent e)
        {
            if (!_useEventDrivenMode) return;
            
            // 过滤：只处理绑定群号的消息
            if (!string.IsNullOrEmpty(_activeGroupId) && e.GroupId != _activeGroupId)
            {
                // 不是绑定的群，忽略消息
                return;
            }
            
            Log($"[TCP] 群消息: {e.GroupId} | {e.SenderId}: {e.ToPreviewText()}");
            
            // 转发到旧的事件系统
            OnWangShangLiaoMessage?.Invoke(e.GroupId, e.SenderId, e.ToPreviewText());
        }
        
        private async void OnEventDriven_FriendMessage(WSLBotContext ctx, EventDriven.FriendMessageEvent e)
        {
            if (!_useEventDrivenMode) return;
            
            Log($"[TCP] 私聊消息: {e.FriendId}: {e.ToPreviewText()}");
            
            // 处理私聊查询命令 (数字1=余额, 数字2=历史)
            try
            {
                await ctx.ProcessPrivateMessageAsync(e);
            }
            catch (Exception ex)
            {
                Log($"[TCP] 处理私聊消息异常: {ex.Message}");
            }
        }
        
        private void OnEventDriven_GroupMute(WSLBotContext ctx, EventDriven.GroupMuteEvent e)
        {
            if (!_useEventDrivenMode) return;
            
            Log($"[TCP] 群禁言: {e.GroupId} -> {(e.IsMuted ? "禁言" : "解禁")}");
        }
        
        private void OnEventDriven_BotOnline(WSLBotContext ctx, EventDriven.BotOnlineEvent e)
        {
            Log($"[TCP] 机器人上线: {e.BotId} ({e.Reason})");
        }
        
        private void OnEventDriven_BotOffline(WSLBotContext ctx, EventDriven.BotOfflineEvent e)
        {
            Log($"[TCP] 机器人下线: {e.Reason}");
        }
        
        private void OnEventDriven_ConnectionState(WSLBotContext ctx, EventDriven.ConnectionStateEvent e)
        {
            Log($"[TCP] 连接状态: {(e.IsConnected ? "已连接" : "已断开")} ({e.Type})");
            
            // 同步CDP连接状态事件
            OnCDPConnectionChanged?.Invoke(e.IsConnected);
        }
        
        private void OnEventDriven_LotteryResult(WSLBotContext ctx, EventDriven.LotteryResultEvent e)
        {
            Log($"[TCP] 开奖: {e.Period} -> {e.Sum} {e.GetSizeOddCode()}");
        }
        
        #endregion
        
        /// <summary>
        /// 启用事件驱动模式并连接 (纯TCP，替代CDP)
        /// </summary>
        public async Task<bool> EnableEventDrivenModeAsync()
        {
            if (_botContext == null)
            {
                Log("事件驱动系统未初始化");
                return false;
            }
            
            _useEventDrivenMode = true;
            
            // 使用纯TCP长连接
            var connected = await _botContext.StartAsync();
            if (connected)
            {
                Log("事件驱动模式已启用 - Lagrange风格TCP长连接 (无CDP)");
                Log($"  服务器: {_botContext.Config.ServerHost}:{_botContext.Config.ServerPort}");
                Log($"  心跳间隔: {_botContext.Config.HeartbeatInterval}ms");
                Log($"  自动重连: {_botContext.Config.AutoReconnect}");
            }
            else
            {
                Log("事件驱动模式启用失败 - TCP连接失败");
            }
            
            return connected;
        }
        
        /// <summary>
        /// 禁用事件驱动模式
        /// </summary>
        public void DisableEventDrivenMode()
        {
            _useEventDrivenMode = false;
            _botContext?.Stop();
            Log("事件驱动模式已禁用");
        }
        
        /// <summary>
        /// 获取TCP连接状态 (替代CDPBridge)
        /// </summary>
        public bool IsTCPConnected => _botContext?.IsConnected ?? false;
        
        /// <summary>
        /// 初始化群管理服务
        /// </summary>
        private void InitializeGroupManager()
        {
            _groupManager = GroupManagementService.Instance;
            _groupManager.Initialize(_cdpBridge);
            Log("群管理服务初始化完成");
        }
        
        /// <summary>
        /// 初始化周期管理和定时消息服务
        /// </summary>
        private void InitializePeriodAndTimedServices()
        {
            // 使用单例模式获取周期管理器
            _periodManager = PeriodManager.Instance;
            _periodManager.OnLog += msg => Log($"[Period] {msg}");
            
            // 初始化定时消息服务
            TimedMessageService.Instance.Initialize(_cdpBridge, _periodManager, _groupManager);
            TimedMessageService.Instance.OnLog += msg => Log($"[TimedMsg] {msg}");
            TimedMessageService.Instance.OnMessageSent += (groupId, message) =>
            {
                Log($"[TimedMsg] 发送到群 {groupId}: {message.Substring(0, Math.Min(50, message.Length))}...");
            };
            
            Log("周期管理和定时消息服务初始化完成");
        }

        #endregion

        #region 组件初始化
        
        /// <summary>
        /// 初始化 HPSocket PACK 服务端
        /// </summary>
        private void InitializeHPSocket()
        {
            _server = new TcpPackServer();
            
            // PACK 模式设置 - 匹配ZCG协议
            _server.PackHeaderFlag = PackHeaderFlag;
            _server.MaxPackSize = MaxPackSize;
            
            // ★★★ 保持连接设置 - 防止空闲断开 ★★★
            // 启用TCP KeepAlive
            _server.KeepAliveTime = 60000;      // 60秒后开始发送心跳
            _server.KeepAliveInterval = 20000;  // 每20秒发送一次心跳
            
            // 绑定事件
            _server.OnPrepareListen += OnPrepareListen;
            _server.OnAccept += OnAccept;
            _server.OnReceive += OnReceive;
            _server.OnClose += OnClose;
            _server.OnShutdown += OnShutdown;
            
            Log($"HPSocket PACK 服务端初始化完成 (PackHeaderFlag=0x{PackHeaderFlag:X2}, MaxPackSize={MaxPackSize})");
        }
        
        /// <summary>
        /// 初始化 NIM SDK 服务
        /// </summary>
        private void InitializeNIM()
        {
            _nimService = NIMService.Instance;
            _nimService.OnLog += msg => Log(msg);
            _nimService.OnLoginStateChanged += loggedIn =>
            {
                Log($"NIM 登录状态: {(loggedIn ? "已登录" : "已登出")}");
                if (loggedIn)
                {
                    _useNIMForSending = true;
                    Log("✓ NIM SDK 已就绪，将使用 NIM 发送消息");
                }
            };
            _nimService.OnMessageReceived += HandleNIMMessage;
            
            // 初始化 NIM SDK
            if (_nimService.Initialize())
            {
                Log("✓ NIM SDK 初始化成功");
            }
            else
            {
                Log("! NIM SDK 初始化失败，将使用 CDP 方式");
                _useNIMForSending = false;
            }
        }
        
        /// <summary>
        /// 从 CDP 获取 NIM 凭证并登录 (支持多种NIM客户端)
        /// 优先使用 NimDirectClient，备用 NIMService
        /// </summary>
        private async Task<bool> LoginNIMFromCDPAsync()
        {
            if (_cdpBridge == null || !_cdpBridge.IsConnected)
            {
                Log("[NIM] CDP 未连接，无法获取 NIM 凭证");
                return false;
            }
            
            try
            {
                Log("[NIM] 正在从旺商聊获取 NIM 凭证...");
                
                // 方案1: 使用 NimDirectClient (推荐，支持完整AES加密)
                var nimDirect = NimDirectClient.Instance;
                nimDirect.OnLog += msg => Log($"[NimDirect] {msg}");
                
                var directLoginResult = await nimDirect.LoginFromCDPAsync(_cdpBridge);
                if (directLoginResult)
                {
                    Log("[NIM] ✓ NimDirectClient 登录成功!");
                    _useNIMForSending = true;
                    return true;
                }
                
                // 方案2: 备用 - 使用旧 NIMService
                Log("[NIM] NimDirectClient 失败，尝试 NIMService...");
                
                // 执行 JavaScript 获取 NIM token
                var js = @"
                (function() {
                    var result = { nimId: '', nimToken: '', wwid: '' };
                    try {
                        var managestate = localStorage.getItem('managestate');
                        if (managestate) {
                            var ms = JSON.parse(managestate);
                            if (ms.userInfo) {
                                result.nimId = (ms.userInfo.nimId || '').toString();
                                result.nimToken = ms.userInfo.nimToken || '';
                                result.wwid = (ms.userInfo.accountId || '').toString();
                            }
                        }
                    } catch(e) {}
                    return JSON.stringify(result);
                })();
                ";
                
                var response = await _cdpBridge.EvaluateAsync(js);
                Log($"[NIM] CDP 响应: {response?.Substring(0, Math.Min(100, response?.Length ?? 0))}");
                
                // 解析响应
                var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
                var responseData = serializer.Deserialize<Dictionary<string, object>>(response);
                
                if (responseData != null && responseData.ContainsKey("result"))
                {
                    var result = responseData["result"] as Dictionary<string, object>;
                    if (result != null && result.ContainsKey("result"))
                    {
                        var innerResult = result["result"] as Dictionary<string, object>;
                        if (innerResult != null && innerResult.ContainsKey("value"))
                        {
                            var valueStr = innerResult["value"]?.ToString();
                            var value = serializer.Deserialize<Dictionary<string, object>>(valueStr);
                            
                            var nimId = value?.ContainsKey("nimId") == true ? value["nimId"]?.ToString() : "";
                            var nimToken = value?.ContainsKey("nimToken") == true ? value["nimToken"]?.ToString() : "";
                            
                            if (!string.IsNullOrEmpty(nimId) && !string.IsNullOrEmpty(nimToken))
                            {
                                Log($"[NIM] 获取到凭证: accid={nimId}");
                                
                                // 登录 NIM
                                var loginResult = await _nimService.LoginAsync(nimId, nimToken);
                                if (loginResult)
                                {
                                    Log($"[NIM] ✓ NIM SDK 登录成功!");
                                    _useNIMForSending = true;
                                    return true;
                                }
                                else
                                {
                                    Log($"[NIM] NIM SDK 登录失败");
                                }
                            }
                            else
                            {
                                Log($"[NIM] 未获取到有效的 NIM 凭证");
                            }
                        }
                    }
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Log($"[NIM] 获取凭证异常: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 获取当前 NIM 连接状态
        /// </summary>
        public NimLoginStatus GetNimStatus()
        {
            return NimDirectClient.Instance.GetLoginStatus();
        }
        
        /// <summary>
        /// 处理 NIM 收到的消息
        /// </summary>
        private void HandleNIMMessage(NIMServiceMessage msg)
        {
            try
            {
                if (msg.IsGroupMessage)
                {
                    // 过滤：只处理绑定群号的消息
                    if (!string.IsNullOrEmpty(_activeGroupId) && msg.ToId != _activeGroupId)
                    {
                        // 不是绑定的群，忽略消息
                        return;
                    }
                    
                    Log($"[NIM] 收到群消息: from={msg.FromId}, group={msg.ToId}, body={msg.MsgBody}");
                    
                    // 转发给主框架
                    var frameworkMsg = new FrameworkMessage
                    {
                        Type = FrameworkMessageType.ReceiveGroupMessage,
                        Content = msg.MsgBody,
                        SenderId = msg.FromId,
                        ReceiverId = msg.ToId,
                        GroupId = msg.ToId,
                        LoginAccount = _nimService?.CurrentAccid ?? "",
                        Timestamp = msg.Time
                    };
                    
                    // 序列化消息并广播
                    var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
                    var jsonMsg = serializer.Serialize(frameworkMsg);
                    BroadcastToAllClients(jsonMsg);
                }
                else
                {
                    // 私聊消息 - 直接转发（不过滤）
                    Log($"[NIM] 收到私聊消息: from={msg.FromId}, body={msg.MsgBody}");
                    
                    var frameworkMsg = new FrameworkMessage
                    {
                        Type = FrameworkMessageType.ReceivePrivateMessage,
                        Content = msg.MsgBody,
                        SenderId = msg.FromId,
                        ReceiverId = msg.ToId,
                        LoginAccount = _nimService?.CurrentAccid ?? "",
                        Timestamp = msg.Time
                    };
                    
                    var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
                    var jsonMsg = serializer.Serialize(frameworkMsg);
                    BroadcastToAllClients(jsonMsg);
                }
            }
            catch (Exception ex)
            {
                Log($"处理 NIM 消息异常: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 初始化 CDP 桥接 - API不可用时的备用方案
        /// </summary>
        private void InitializeCDP()
        {
            try
            {
                _cdpBridge = new CDPBridge();
                _cdpBridge.OnLog += msg => Log($"[CDP] {msg}");
                _cdpBridge.OnConnectionChanged += connected =>
                {
                    Log($"[CDP] 连接状态: {(connected ? "已连接" : "已断开")}");
                    
                    // 触发 CDP 连接状态变化事件
                    OnCDPConnectionChanged?.Invoke(connected);
                    
                    // CDP连接成功后尝试获取 NIM Token
                    if (connected)
                    {
                        _ = TryGetNimTokenFromCDP();
                    }
                };

                // 订阅消息接收事件
                _cdpBridge.OnMessageReceived += HandleCDPMessage;
                
                // 订阅群聊消息事件并转发给主框架
                _cdpBridge.OnGroupMessage += OnGroupMessageReceived;
                
                Log("[CDP] CDP 桥接已初始化 (API备用方案)");
            }
            catch (Exception ex)
            {
                Log($"[CDP] 初始化失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 尝试从 CDP 获取 NIM Token
        /// ★★★ 注意: 双账号模式下，不要覆盖机器人账号的凭证 ★★★
        /// </summary>
        private async System.Threading.Tasks.Task TryGetNimTokenFromCDP()
        {
            try
            {
                if (_cdpBridge == null || !IsCDPConnected) return;
                
                Log("[CDP] 尝试从旺商聊客户端获取 NIM 凭证...");
                
                var userInfo = await _cdpBridge.GetCurrentUserInfoAsync();
                if (userInfo != null && !string.IsNullOrEmpty(userInfo.nimToken))
                {
                    Log($"[CDP] ✓ 获取客户端 NIM 凭证: accid={userInfo.nimId} (客户端账号)");
                    
                    // ★★★ 双账号模式检查 ★★★
                    // 如果机器人账号已有NIM凭证，且与客户端账号不同，不要覆盖
                    var currentAccount = BotLoginService.Instance.CurrentAccount;
                    if (currentAccount != null && !string.IsNullOrEmpty(currentAccount.NimAccid))
                    {
                        if (currentAccount.NimAccid != userInfo.nimId)
                        {
                            Log($"[CDP] ★ 双账号模式: 机器人凭证(nimId={currentAccount.NimAccid}) != 客户端凭证(nimId={userInfo.nimId})");
                            Log($"[CDP] ★ 保留机器人凭证，不覆盖");
                            return; // 不覆盖机器人凭证
                        }
                    }
                    
                    // 只有当没有机器人凭证时，才使用客户端凭证
                    if (currentAccount != null && string.IsNullOrEmpty(currentAccount.NimAccid))
                    {
                        Log("[CDP] 机器人账号没有NIM凭证，使用客户端凭证...");
                        currentAccount.NimAccid = userInfo.nimId;
                        currentAccount.NimToken = userInfo.nimToken;
                        currentAccount.Wwid = userInfo.wwid;
                        currentAccount.Nickname = userInfo.nickname;
                        
                        // 保存更新
                        Models.AccountManager.Instance.AddAccount(currentAccount);
                        
                        // 使用获取到的凭证登录 NIM
                        await BotLoginService.Instance.LoginWithNimTokenAsync(
                            currentAccount.NimAccid, 
                            currentAccount.NimToken);
                    }
                }
                else
                {
                    Log("[CDP] 未能获取 NIM 凭证");
                }
            }
            catch (Exception ex)
            {
                Log($"[CDP] 获取 NIM 凭证失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 处理收到的群聊消息
        /// </summary>
        private void OnGroupMessageReceived(GroupMessageEvent evt)
        {
            try
            {
                // 过滤：只处理绑定群号的消息
                if (!string.IsNullOrEmpty(_activeGroupId) && evt.GroupId != _activeGroupId)
                {
                    // 不是绑定的群，忽略消息
                    return;
                }
                
                var robotId = _cdpBridge?.MyWangShangId ?? "";
                Log($"收到群消息: GroupId={evt.GroupId}, From={evt.FromId}, Content={evt.Content}");
                
                // 1. 构建插件投递格式消息 (旧程序完全兼容格式)
                var pluginMessage = ZCGMessageDelivery.Instance.BuildPluginDeliveryMessage(
                    robotAccount: robotId,
                    activeAccount: evt.FromId,
                    passiveAccount: robotId,
                    groupId: evt.GroupId,
                    content: evt.Content,
                    messageId: evt.MessageId ?? Guid.NewGuid().ToString(),
                    messageType: ZCGMessageDelivery.MSG_TYPE_GROUP,
                    messageTime: evt.Time > 0 ? evt.Time : DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                    subType: ZCGMessageDelivery.SUB_TYPE_NORMAL);
                
                // 2. 构建 JSON 格式的 FrameworkMessage
                var extraData = new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(
                    new 
                    {
                        MessageId = evt.MessageId ?? "",
                        GroupId = evt.GroupId ?? "",
                        RobotId = robotId,
                        MessageType = ZCGMessageDelivery.MSG_TYPE_GROUP.ToString(),
                        Time = evt.Time.ToString(),
                        FromNick = evt.SenderNick ?? "",
                        PluginMessage = pluginMessage  // 包含完整的插件投递格式
                    });
                    
                var frameworkMsg = new FrameworkMessage
                {
                    Type = FrameworkMessageType.ReceiveGroupMessage,
                    Content = evt.Content,
                    SenderId = evt.FromId,
                    ReceiverId = evt.GroupId,
                    GroupId = evt.GroupId,
                    LoginAccount = robotId,
                    Timestamp = DateTime.Now.Ticks,
                    Extra = extraData
                };
                
                // 3. 发送 JSON 格式消息
                BroadcastFrameworkMessage(frameworkMsg);
                
                // 4. 同时发送插件投递格式消息 (给旧版客户端)
                BroadcastRawMessage(pluginMessage);
                
                // 5. 触发 WangShangLiao 消息事件 (供 MainForm 处理)
                OnWangShangLiaoMessage?.Invoke(evt.GroupId, evt.FromId, evt.Content);
            }
            catch (Exception ex)
            {
                // BUG修复: 使用完整异常记录
                LogException("[CDP] 处理群消息异常", ex);
            }
        }
        
        /// <summary>
        /// 广播 FrameworkMessage 给所有连接的客户端
        /// 优化: 使用快照遍历，避免频繁 ToList() 调用
        /// </summary>
        private void BroadcastFrameworkMessage(FrameworkMessage message)
        {
            // 获取快照，避免遍历时集合变化
            var clientSnapshot = GetClientSnapshot();
            if (clientSnapshot.Length == 0)
            {
                Log("没有连接的客户端，消息未发送");
                return;
            }
            
            var json = message.ToJson();
            var data = System.Text.Encoding.UTF8.GetBytes(json);
            
            BroadcastDataToClients(clientSnapshot, data, "FrameworkMessage");
        }
        
        /// <summary>
        /// 广播原始消息给所有连接的客户端 (插件投递格式)
        /// 优化: 使用快照遍历
        /// </summary>
        private void BroadcastRawMessage(string message)
        {
            var clientSnapshot = GetClientSnapshot();
            if (clientSnapshot.Length == 0)
            {
                Log("没有连接的客户端，原始消息未发送");
                return;
            }
            
            // 使用 GBK 编码 (旧程序使用 GBK)
            var data = System.Text.Encoding.GetEncoding("GBK").GetBytes(message);
            
            BroadcastDataToClients(clientSnapshot, data, "原始消息");
        }
        
        /// <summary>
        /// 广播消息给所有连接的客户端 (旧方法，保持兼容)
        /// 优化: 使用快照遍历
        /// </summary>
        private void BroadcastToAllClients(string message)
        {
            var clientSnapshot = GetClientSnapshot();
            if (clientSnapshot.Length == 0)
            {
                Log("没有连接的客户端，消息未发送");
                return;
            }
            
            var data = System.Text.Encoding.UTF8.GetBytes(message);
            
            BroadcastDataToClients(clientSnapshot, data, "消息");
        }
        
        /// <summary>
        /// 获取客户端连接ID快照 (线程安全)
        /// BUG修复: 使用 List 避免数组越界问题
        /// </summary>
        private IntPtr[] GetClientSnapshot()
        {
            // ConcurrentDictionary.Keys 在遍历过程中可能变化
            // 使用 List 动态添加，避免数组越界
            var snapshot = new System.Collections.Generic.List<IntPtr>();
            
            try
            {
                foreach (var key in _clients.Keys)
                {
                    snapshot.Add(key);
                }
            }
            catch (InvalidOperationException)
            {
                // 集合在遍历时被修改，返回当前已收集的快照
                Logger.Debug("[GetClientSnapshot] 遍历时集合被修改，返回部分快照");
            }
            
            return snapshot.ToArray();
        }
        
        /// <summary>
        /// 向多个客户端广播数据 (统一发送逻辑)
        /// </summary>
        private void BroadcastDataToClients(IntPtr[] clients, byte[] data, string messageType)
        {
            var successCount = 0;
            var failCount = 0;
            
            foreach (var connId in clients)
            {
                try
                {
                    // 检查连接是否仍然有效
                    if (!_clients.ContainsKey(connId))
                        continue;
                        
                    _server?.Send(connId, data, data.Length);
                    successCount++;
                }
                catch (Exception ex)
                {
                    failCount++;
                    // 只在调试模式下记录详细错误
                    Logger.Debug($"发送{messageType}给客户端 {connId} 失败: {ex.Message}");
                }
            }
            
            if (successCount > 0 || failCount > 0)
            {
                Log($"{messageType}广播完成: 成功={successCount}, 失败={failCount}");
            }
        }
        
        /// <summary>
        /// 广播消息给所有连接的客户端 (兼容旧代码)
        /// </summary>
        private void BroadcastToAllClientsLegacy(string message)
        {
            if (_clients.Count == 0)
            {
                Log("没有连接的客户端，消息未发送");
                return;
            }
            
            var data = System.Text.Encoding.UTF8.GetBytes(message);
            
            foreach (var connId in _clients.Keys.ToList())
            {
                try
                {
                    _server?.Send(connId, data, data.Length);
                    Log($"消息已发送给客户端 {connId}");
                }
                catch (Exception ex)
                {
                    Log($"发送消息给客户端 {connId} 失败: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 初始化 API 处理器
        /// </summary>
        private void InitializeApiHandler()
        {
            _apiHandler = new ZCGApiHandler();
            _apiHandler.OnApiCall += (sender, e) =>
            {
                Log($"[API] 调用: {e.ApiName}|{string.Join("|", e.Args)}");
            };
            _apiHandler.OnApiResponse += (sender, e) =>
            {
                Log($"[API] 响应: {e.ApiName} -> {e.Result}");
            };
        }

        #endregion
        
        // [HPSocket 事件处理] moved to FrameworkServer.HPSocket.cs

        
        // [数据处理] moved to FrameworkServer.DataProcessing.cs


        // [消息处理器] moved to FrameworkServer.MessageHandlers.cs


        // [配置同步处理] moved to FrameworkServer.ConfigSync.cs


        // [开奖相关处理] moved to FrameworkServer.Lottery.cs


        // [API执行] moved to FrameworkServer.ApiExecute.cs


        // [CDP消息处理] moved to FrameworkServer.Cdp.cs


        // [服务端控制] moved to FrameworkServer.ServerControl.cs

        
        // [发送消息] moved to FrameworkServer.Sending.cs

        
        // [辅助方法] moved to FrameworkServer.Helpers.cs


        // [IDisposable] moved to FrameworkServer.Dispose.cs

    }

    #region 客户端信息类
    
    /// <summary>
    /// 客户端信息（匹配招财狗协议）
    /// </summary>
    public class ClientInfo
    {
        public IntPtr ConnId { get; set; }
        public string Address { get; set; }
        public ushort Port { get; set; }
        public DateTime ConnectTime { get; set; } = DateTime.Now;
        public bool LoggedIn { get; set; }
        public string UserId { get; set; }
        
        // 账号信息
        public string Nickname { get; set; }
        public string Wwid { get; set; }
        public string GroupId { get; set; }
        public string Account { get; set; }
        public bool AutoMode { get; set; }
        public string Status { get; set; } = "已连接";
        
        public override string ToString()
        {
            if (LoggedIn)
            {
                return $"{Nickname} (wwid={Wwid}, 群号={GroupId})";
            }
            return $"{Address}:{Port} (ConnID={ConnId})";
        }
    }

    #endregion
}
