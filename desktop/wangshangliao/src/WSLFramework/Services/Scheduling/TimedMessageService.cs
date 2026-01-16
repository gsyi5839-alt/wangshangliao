using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WSLFramework.Utils;

namespace WSLFramework.Services
{
    /// <summary>
    /// 定时消息服务 - 完全匹配ZCG的定时消息功能
    /// 包括倒计时提醒、封盘通知、开奖提示、账单发送等
    /// </summary>
    public class TimedMessageService
    {
        private static readonly Lazy<TimedMessageService> _lazy = 
            new Lazy<TimedMessageService>(() => new TimedMessageService());
        public static TimedMessageService Instance => _lazy.Value;
        
        // CDP桥接（备用发送方式）
        private CDPBridge _cdpBridge;
        
        // NIM服务（优先使用，直接发送消息）
        private NIMService _nimService;
        
        // 开奖周期管理器
        private PeriodManager _periodManager;
        
        // 群管理服务
        private GroupManagementService _groupManager;
        
        // 是否使用NIM发送
        private bool _useNIMForSending = true;
        
        // 活跃群列表
        private readonly ConcurrentDictionary<string, bool> _activeGroups;
        
        // 配置
        public bool CountdownEnabled { get; set; } = true;      // 是否启用倒计时提醒
        public bool CloseNotifyEnabled { get; set; } = true;    // 是否启用封盘通知
        public bool OpenNotifyEnabled { get; set; } = true;     // 是否启用开奖通知
        public bool BillEnabled { get; set; } = true;           // 是否发送账单
        public bool MuteOnClose { get; set; } = true;           // 封盘时禁言
        public bool UnmuteOnOpen { get; set; } = true;          // 开盘时解禁
        
        // 倒计时提醒时间点（秒）
        public List<int> CountdownPoints { get; set; } = new List<int> { 40, 30, 20, 10 };
        
        // 事件
        public event Action<string> OnLog;
        public event Action<string, string> OnMessageSent;  // groupId, message
        
        private TimedMessageService()
        {
            _activeGroups = new ConcurrentDictionary<string, bool>();
        }
        
        /// <summary>
        /// 初始化服务
        /// </summary>
        public void Initialize(CDPBridge cdpBridge, PeriodManager periodManager, GroupManagementService groupManager)
        {
            _cdpBridge = cdpBridge;
            _periodManager = periodManager;
            _groupManager = groupManager;
            
            // 获取 NIM 服务实例 (优先使用)
            _nimService = NIMService.Instance;
            _useNIMForSending = _nimService?.IsInitialized ?? false;
            
            // 订阅周期管理器事件 - 消息发送顺序完全匹配旧软件
            if (_periodManager != null)
            {
                _periodManager.OnCountdown += HandleCountdown;
                _periodManager.OnBetOpen += HandleBettingOpened;
                _periodManager.OnNewPeriod += HandleNewPeriodString;
                
                // 消息发送顺序 (完全匹配旧软件):
                // 1. 40秒提醒 → 订阅 OnWarning40
                // 2. 20秒提醒 → 订阅 OnWarning20
                // 3. 封盘线 → 订阅 OnBetClose
                // 4. 核对 (封盘后10秒) → 订阅 OnCheckNotify
                // 5. 卡奖提示 (封盘后20秒) → 订阅 OnStuckNotify
                _periodManager.OnWarning40 += HandleWarning40Message;
                _periodManager.OnWarning20 += HandleWarning20Message;
                _periodManager.OnBetClose += HandleBettingClosed;
                _periodManager.OnCheckNotify += HandleCheckNotifyMessage;
                _periodManager.OnStuckNotify += HandleStuckNotifyMessage;
                
                // 订阅禁言请求
                _periodManager.OnMuteRequest += HandleMuteRequest;
            }
        }
        
        /// <summary>
        /// 添加活跃群
        /// </summary>
        public void AddActiveGroup(string groupId)
        {
            _activeGroups[groupId] = true;
            Log($"添加活跃群: {groupId}");
        }
        
        /// <summary>
        /// 移除活跃群
        /// </summary>
        public void RemoveActiveGroup(string groupId)
        {
            _activeGroups.TryRemove(groupId, out _);
            Log($"移除活跃群: {groupId}");
        }
        
        /// <summary>
        /// 获取所有活跃群
        /// </summary>
        public IEnumerable<string> GetActiveGroups()
        {
            return _activeGroups.Keys;
        }
        
        #region 定时消息处理
        
        /// <summary>
        /// 处理倒计时 - 每秒触发，但只在特定时间点发送消息
        /// 注意：40秒和10秒警告由 OnWarning40/OnWarning10 事件专门处理
        /// </summary>
        private async void HandleCountdown(int seconds)
        {
            if (!CountdownEnabled)
                return;
                
            // 40秒和10秒由专门的警告事件处理，这里不重复发送
            if (seconds == 40 || seconds == 10)
                return;
            
            // 只在其他指定的倒计时点发送消息 (30秒, 20秒等)
            if (!CountdownPoints.Contains(seconds))
                return;
            
            Log($"发送倒计时提醒: {seconds}秒");
            
            // 使用统一的倒计时格式（匹配ZCG旧程序）
            var message = ZCGResponseFormatter.FormatCountdown(seconds);
            await SendToAllGroupsAsync(message);
        }
        
        /// <summary>
        /// [消息顺序1] 处理40秒警告
        /// </summary>
        private async void HandleWarning40Message(string message)
        {
            if (!CountdownEnabled)
                return;
            
            Log($"[顺序1] 发送40秒倒计时: {message}");
            await SendToAllGroupsAsync(message);
        }
        
        /// <summary>
        /// [消息顺序2] 处理20秒警告
        /// </summary>
        private async void HandleWarning20Message(string message)
        {
            if (!CountdownEnabled)
                return;
            
            Log($"[顺序2] 发送20秒倒计时: {message}");
            await SendToAllGroupsAsync(message);
        }
        
        /// <summary>
        /// [消息顺序4] 处理核对消息 (封盘后10秒)
        /// </summary>
        private async void HandleCheckNotifyMessage(string message)
        {
            if (!CloseNotifyEnabled)
                return;
            
            Log($"[顺序4] 发送核对消息 (封盘后10秒): {message}");
            await SendToAllGroupsAsync(message);
        }
        
        /// <summary>
        /// [消息顺序5] 处理卡奖提示 (封盘后约20秒)
        /// </summary>
        private async void HandleStuckNotifyMessage(string message)
        {
            if (!CloseNotifyEnabled)
                return;
            
            Log($"[顺序5] 发送卡奖提示 (封盘后20秒): {message}");
            await SendToAllGroupsAsync(message);
        }
        
        /// <summary>
        /// 处理禁言/解禁请求
        /// </summary>
        private async void HandleMuteRequest(bool mute)
        {
            if (_groupManager == null)
                return;
            
            foreach (var groupId in _activeGroups.Keys)
            {
                try
                {
                    if (mute)
                    {
                        Log($"群 {groupId} 自动禁言");
                        await _groupManager.MuteGroupAsync(groupId);
                    }
                    else
                    {
                        Log($"群 {groupId} 自动解禁");
                        await _groupManager.UnmuteGroupAsync(groupId);
                    }
                }
                catch (Exception ex)
                {
                    Log($"禁言/解禁操作失败 ({groupId}): {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// [消息顺序3] 处理封盘 - 只发送封盘线消息
        /// 注意：核对消息由 HandleCheckNotifyMessage 在封盘后10秒发送
        ///       卡奖提示由 HandleStuckNotifyMessage 在封盘后20秒发送
        ///       禁言操作由 HandleMuteRequest 统一处理
        /// </summary>
        private async void HandleBettingClosed(string period)
        {
            Log($"[顺序3] 处理封盘: 第{period}期");
            
            // 只发送封盘线消息 (匹配旧程序: ==加封盘线==\n以上有钱的都接\n==庄显为准==)
            if (CloseNotifyEnabled)
            {
                await SendToAllGroupsAsync(ZCGResponseFormatter.FormatCloseLine());
            }
            
            // 注意：
            // - 核对消息 (顺序4) 由 PeriodManager.OnCheckNotify 在封盘后10秒触发
            // - 卡奖提示 (顺序5) 由 PeriodManager.OnStuckNotify 在封盘后20秒触发
            // - 禁言操作由 PeriodManager.OnMuteRequest 事件触发
        }
        
        /// <summary>
        /// 处理开盘
        /// </summary>
        private async void HandleBettingOpened(string period)
        {
            Log($"处理开盘: {period}");
            
            // 解除全体禁言
            if (UnmuteOnOpen && _groupManager != null)
            {
                foreach (var groupId in _activeGroups.Keys)
                {
                    await _groupManager.UnmuteGroupAsync(groupId);
                }
            }
            
            // 发送开盘提示
            if (OpenNotifyEnabled)
            {
                var message = $"==新期开始==\n第{period}期已开放下注\n祝各位老板好运！";
                await SendToAllGroupsAsync(message);
            }
        }
        
        /// <summary>
        /// 处理新期开始（使用期号字符串）
        /// </summary>
        private async void HandleNewPeriodString(string period)
        {
            if (string.IsNullOrEmpty(period))
                return;
                
            Log($"处理新期开始: {period}");
            
            // 发送新期开始提示
            var message = $"==新期开始==\n第{period}期已开放下注\n祝各位老板好运！";
            await SendToAllGroupsAsync(message);
        }

        /// <summary>
        /// 处理新期开奖（使用LotteryResult）
        /// </summary>
        public async Task HandleNewPeriodAsync(LotteryResult result)
        {
            if (result == null)
                return;
                
            Log($"处理新期开奖: {result.Period}");
            
            // 发送开奖结果
            var openMessage = ZCGResponseFormatter.FormatOpenResult(
                result.Num1, result.Num2, result.Num3, result.Period);
            await SendToAllGroupsAsync(openMessage);
        }
        
        #endregion
        
        #region 账单发送
        
        /// <summary>
        /// 发送账单 - 匹配ZCG账单格式
        /// </summary>
        public async Task SendBillAsync(LotteryResult result, List<BetRecord> bets, int totalWin, int totalLose)
        {
            if (!BillEnabled || result == null)
                return;
            
            Log($"发送账单: period={result.Period}, bets={bets.Count}");
            
            // 账单前提示
            await SendToAllGroupsAsync(ZCGResponseFormatter.FormatBillNotice());
            
            // 等待一下
            await Task.Delay(500);
            
            // 发送完整账单
            var bill = ZCGResponseFormatter.FormatFullBill(
                result.Num1, result.Num2, result.Num3, result.Period,
                bets, totalWin, totalLose);
            await SendToAllGroupsAsync(bill);
        }
        
        /// <summary>
        /// 发送简化账单（只有头部和统计）
        /// </summary>
        public async Task SendBillHeaderAsync(LotteryResult result, int playerCount, int totalScore)
        {
            if (!BillEnabled || result == null)
                return;
            
            var header = ZCGResponseFormatter.FormatBillHeader(
                result.Num1, result.Num2, result.Num3, result.Period,
                playerCount, totalScore);
            await SendToAllGroupsAsync(header);
        }
        
        #endregion
        
        #region 消息发送
        
        /// <summary>
        /// 向所有活跃群发送消息
        /// </summary>
        public async Task SendToAllGroupsAsync(string message)
        {
            // 检查 CDP 连接状态
            if (_cdpBridge == null)
            {
                Log($"[错误] CDP桥接为空，无法发送消息");
                return;
            }
            
            if (!_cdpBridge.IsConnected)
            {
                Log($"[错误] CDP未连接，无法发送消息");
                return;
            }
            
            // 检查活跃群列表
            var groupCount = _activeGroups.Count;
            Log($"[发送] 准备向 {groupCount} 个活跃群发送消息: {message.Substring(0, Math.Min(30, message.Length))}...");
            
            if (groupCount == 0)
            {
                Log($"[警告] 活跃群列表为空，无法发送消息！请确认已点击开始算账");
                return;
            }
            
            foreach (var groupId in _activeGroups.Keys)
            {
                try
                {
                    Log($"[发送] 正在发送到群: {groupId}");
                    var success = await SendToGroupAsync(groupId, message);
                    Log($"[发送] 群{groupId}发送结果: {(success ? "成功" : "失败")}");
                    // 避免发送过快
                    await Task.Delay(100);
                }
                catch (Exception ex)
                {
                    Log($"[错误] 发送到群{groupId}失败: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// 向指定群发送消息 - 直接在当前群聊窗口发送
        /// 注意：已设置活跃群聊，旺商聊界面应已打开目标群
        /// </summary>
        public async Task<bool> SendToGroupAsync(string groupId, string message)
        {
            // 优先使用 NIM SDK 发送 (直接通过云信服务器，不需要 CDP 调试端口)
            if (_useNIMForSending && _nimService != null && _nimService.IsLoggedIn)
            {
                try
                {
                    Log($"[NIM] 发送群消息: groupId={groupId}");
                    var result = await _nimService.SendGroupMessageAsync(groupId, message);
                    if (result)
                    {
                        OnMessageSent?.Invoke(groupId, message);
                        Log($"[NIM] ✓ 消息发送成功: groupId={groupId}, msg={message.Substring(0, Math.Min(30, message.Length))}...");
                        return true;
                    }
                    Log($"[NIM] 消息发送失败，尝试 CDP 方式");
                }
                catch (Exception ex)
                {
                    Log($"[NIM] 发送异常: {ex.Message}，尝试 CDP 方式");
                }
            }
            
            // 备用: 使用 CDP 发送
            if (_cdpBridge == null || !_cdpBridge.IsConnected)
            {
                Log($"CDP未连接，无法发送消息到群{groupId}");
                return false;
            }
            
            try
            {
                var result = await _cdpBridge.SendGroupMessageAsync(groupId, message);
                
                if (result)
                {
                    OnMessageSent?.Invoke(groupId, message);
                    Log($"[CDP] ✓ 消息发送成功: groupId={groupId}, msg={message.Substring(0, Math.Min(30, message.Length))}...");
                }
                else
                {
                    Log($"[CDP] 消息发送失败: groupId={groupId}");
                }
                
                return result;
            }
            catch (Exception ex)
            {
                Log($"[CDP] 发送消息异常: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 发送私聊消息
        /// </summary>
        public async Task<bool> SendPrivateAsync(string userId, string message)
        {
            if (_cdpBridge == null || !_cdpBridge.IsConnected)
            {
                Log($"CDP未连接，无法发送私聊消息");
                return false;
            }
            
            try
            {
                var result = await _cdpBridge.SendPrivateMessageAsync(userId, message);
                
                if (result)
                {
                    Log($"私聊消息发送成功: userId={userId}");
                }
                
                return result;
            }
            catch (Exception ex)
            {
                Log($"发送私聊消息异常: {ex.Message}");
                return false;
            }
        }
        
        #endregion
        
        #region 特殊消息
        
        /// <summary>
        /// 发送余额查询响应
        /// </summary>
        public async Task SendBalanceResponseAsync(string groupId, string playerId, string nickname, int balance)
        {
            var response = ZCGResponseFormatter.FormatBalanceQuery(playerId, nickname, balance);
            await SendToGroupAsync(groupId, response);
        }
        
        /// <summary>
        /// 发送上分响应
        /// </summary>
        public async Task SendUpResponseAsync(string groupId, string playerId, int amount, int newBalance)
        {
            var response = ZCGResponseFormatter.FormatUpSuccess(playerId, amount, newBalance);
            await SendToGroupAsync(groupId, response);
        }
        
        /// <summary>
        /// 发送下分响应
        /// </summary>
        public async Task SendDownResponseAsync(string groupId, string playerId, int amount, int newBalance)
        {
            var response = ZCGResponseFormatter.FormatDownSuccess(playerId, amount, newBalance);
            await SendToGroupAsync(groupId, response);
        }
        
        /// <summary>
        /// 发送下注响应
        /// </summary>
        public async Task SendBetResponseAsync(string groupId, string playerId, string betType, int amount, int newBalance)
        {
            var response = ZCGResponseFormatter.FormatBetSuccess(playerId, betType, amount, newBalance);
            await SendToGroupAsync(groupId, response);
        }
        
        /// <summary>
        /// 发送封盘期间下注失败响应
        /// </summary>
        public async Task SendBetClosedResponseAsync(string groupId, string playerId)
        {
            var response = ZCGResponseFormatter.FormatBetClosed(playerId);
            await SendToGroupAsync(groupId, response);
        }
        
        /// <summary>
        /// 发送余额不足响应
        /// </summary>
        public async Task SendInsufficientBalanceResponseAsync(string groupId, string playerId, string betType, int amount, int current)
        {
            var response = ZCGResponseFormatter.FormatBetInsufficientBalance(playerId, betType, amount, current);
            await SendToGroupAsync(groupId, response);
        }
        
        #endregion
        
        #region 定时任务
        
#pragma warning disable CS0169 // 保留给定时任务取消
        private CancellationTokenSource _scheduledTaskCts;
#pragma warning restore CS0169
        private readonly ConcurrentDictionary<string, ScheduledMessage> _scheduledMessages = 
            new ConcurrentDictionary<string, ScheduledMessage>();
        
        /// <summary>
        /// 添加定时消息
        /// </summary>
        public string ScheduleMessage(string groupId, string message, TimeSpan delay)
        {
            var id = Guid.NewGuid().ToString();
            var scheduled = new ScheduledMessage
            {
                Id = id,
                GroupId = groupId,
                Message = message,
                ExecuteAt = DateTime.Now.Add(delay)
            };
            _scheduledMessages[id] = scheduled;
            
            Log($"添加定时消息: id={id}, delay={delay.TotalSeconds}s");
            
            // 启动定时发送
            Task.Run(async () =>
            {
                await Task.Delay(delay);
                if (_scheduledMessages.TryRemove(id, out var msg))
                {
                    await SendToGroupAsync(msg.GroupId, msg.Message);
                }
            });
            
            return id;
        }
        
        /// <summary>
        /// 取消定时消息
        /// </summary>
        public bool CancelScheduledMessage(string id)
        {
            return _scheduledMessages.TryRemove(id, out _);
        }
        
        /// <summary>
        /// 添加重复消息
        /// </summary>
        public void StartRepeatingMessage(string groupId, string message, TimeSpan interval, CancellationToken cancellationToken)
        {
            Task.Run(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await SendToGroupAsync(groupId, message);
                    await Task.Delay(interval, cancellationToken);
                }
            }, cancellationToken);
        }
        
        #endregion
        
        private void Log(string message)
        {
            Logger.Info($"[TimedMsg] {message}");
            OnLog?.Invoke(message);
        }
    }
    
    /// <summary>
    /// 定时消息
    /// </summary>
    public class ScheduledMessage
    {
        public string Id { get; set; }
        public string GroupId { get; set; }
        public string Message { get; set; }
        public DateTime ExecuteAt { get; set; }
    }
}
