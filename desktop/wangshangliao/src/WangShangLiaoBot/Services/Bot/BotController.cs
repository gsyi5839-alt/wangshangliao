using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WangShangLiaoBot.Models;
using WangShangLiaoBot.Services.Betting;
using WangShangLiaoBot.Services.Bot.Handlers;
using WangShangLiaoBot.Services.XClient;
using LotteryResult = WangShangLiaoBot.Services.LotteryResult;

namespace WangShangLiaoBot.Services.Bot
{
    /// <summary>
    /// 机器人主控制器 - 整合所有服务，提供统一的启动/停止接口
    /// 连接旺商聊的核心框架
    /// </summary>
    public sealed class BotController
    {
        private static BotController _instance;
        public static BotController Instance => _instance ?? (_instance = new BotController());

        // 消息处理器
        private readonly SpeechDetectionHandler _speechHandler;
        private readonly BetHandler _betHandler;
        private readonly ScoreHandler _scoreHandler;
        private readonly AutoReplyHandler _autoReplyHandler;
        private readonly TrusteeHandler _trusteeHandler;
        private readonly GuessNumberHandler _guessHandler;
        private readonly BonusHandler _bonusHandler;

        // 状态
        private bool _isRunning;
        private string _currentTeamId;
        private string _currentPeriod;

        // 事件
        public event Action<string> OnLog;
        public event Action<bool> OnRunningStateChanged;
        public event Action<string, decimal, decimal> OnDepositRequest;   // playerId, amount, balance
        public event Action<string, decimal, decimal> OnWithdrawRequest;  // playerId, amount, balance

        public bool IsRunning => _isRunning;
        public string CurrentTeamId => _currentTeamId;
        public string CurrentPeriod => _currentPeriod;

        private BotController()
        {
            // 初始化处理器
            _speechHandler = new SpeechDetectionHandler();
            _betHandler = new BetHandler();
            _scoreHandler = new ScoreHandler();
            _autoReplyHandler = new AutoReplyHandler();
            _trusteeHandler = new TrusteeHandler();
            _guessHandler = new GuessNumberHandler();
            _bonusHandler = new BonusHandler();

            // 注册处理器到调度器 (按优先级注册)
            MessageDispatcher.Instance.RegisterHandler(_speechHandler);      // 优先级 1000 (最高)
            MessageDispatcher.Instance.RegisterHandler(_betHandler);         // 优先级 100
            MessageDispatcher.Instance.RegisterHandler(_scoreHandler);       // 优先级 90
            MessageDispatcher.Instance.RegisterHandler(_trusteeHandler);     // 优先级 60
            MessageDispatcher.Instance.RegisterHandler(_guessHandler);       // 优先级 55
            MessageDispatcher.Instance.RegisterHandler(_bonusHandler);       // 优先级 45
            MessageDispatcher.Instance.RegisterHandler(_autoReplyHandler);   // 优先级 10

            // 绑定事件
            BindEvents();
        }

        private void BindEvents()
        {
            // 消息调度器事件
            MessageDispatcher.Instance.OnSendGroupMessage += SendGroupMessage;
            MessageDispatcher.Instance.OnSendPrivateMessage += SendPrivateMessage;
            MessageDispatcher.Instance.OnLog += Log;

            // 封盘服务事件
            SealingService.Instance.OnSendMessage += SendGroupMessage;
            SealingService.Instance.OnMuteGroup += MuteGroup;
            SealingService.Instance.OnPeriodChange += OnPeriodChanged;
            SealingService.Instance.OnRemind += OnSealingRemind;

            // 结算服务事件
            AutoSettlementService.Instance.OnSendMessage += SendGroupMessage;
            AutoSettlementService.Instance.OnSettlementComplete += OnAutoSettlementComplete;
            
            // 开奖服务事件
            LotteryService.Instance.OnResultUpdated += OnLotteryResultUpdated;

            // 托管服务事件
            TrusteeService.Instance.OnAutobet += OnTrusteeAutoBet;
            TrusteeService.Instance.OnLog += Log;

            // 猜数字服务事件
            GuessNumberService.Instance.OnGuessSuccess += OnGuessSuccess;
            GuessNumberService.Instance.OnLog += Log;

            // 长龙减赔服务事件
            DragonReduceService.Instance.OnDragonReduce += OnDragonReduce;
            DragonReduceService.Instance.OnLog += Log;

            // 返点服务事件
            BonusService.Instance.OnBonusGiven += OnBonusGiven;
            BonusService.Instance.OnLog += Log;

            // 发言检测服务事件
            SpeechDetectionService.Instance.OnMutePlayer += MutePlayer;
            SpeechDetectionService.Instance.OnKickPlayer += KickPlayer;
            SpeechDetectionService.Instance.OnWithdrawMessage += WithdrawMessage;
            SpeechDetectionService.Instance.OnSendWarning += (tid, pid, msg) => SendGroupMessage(tid, msg);
            SpeechDetectionService.Instance.OnLog += Log;

            // 锁名片服务事件
            CardLockService.Instance.OnCardChanged += OnCardChanged;
            CardLockService.Instance.OnKickPlayer += KickPlayer;
            CardLockService.Instance.OnSendWarning += (tid, msg) => SendGroupMessage(tid, msg);
            CardLockService.Instance.OnResetCard += ResetPlayerCard;
            CardLockService.Instance.OnLog += Log;

            // 进群欢迎服务事件
            WelcomeService.Instance.OnSendPrivateMessage += SendPrivateMessage;
            WelcomeService.Instance.OnSendGroupMessage += SendGroupMessage;
            WelcomeService.Instance.OnAcceptFriendRequest += AcceptFriendRequest;
            WelcomeService.Instance.OnAcceptJoinRequest += AcceptJoinRequest;
            WelcomeService.Instance.OnLog += Log;

            // 二七玩法服务事件
            TwoSevenService.Instance.OnLog += Log;

            // 上下分处理器事件
            _scoreHandler.OnDepositRequest += (pid, nick, amount, tid) =>
            {
                var balance = ScoreService.Instance.GetBalance(pid);
                OnDepositRequest?.Invoke(pid, amount, balance);
            };
            _scoreHandler.OnWithdrawRequest += (pid, nick, amount, tid) =>
            {
                var balance = ScoreService.Instance.GetBalance(pid);
                OnWithdrawRequest?.Invoke(pid, amount, balance);
            };

            // 自动回复处理器事件
            _autoReplyHandler.OnSendImage += SendImage;
            _autoReplyHandler.OnGetLotteryHistory += GetLotteryHistory;
        }

        #region 新服务事件处理

        private void OnSealingRemind(string message, int secondsToSeal)
        {
            // 封盘提醒时触发托管下注
            if (_isRunning && !string.IsNullOrEmpty(_currentTeamId))
            {
                TrusteeService.Instance.TriggerAutoBet(_currentTeamId, secondsToSeal);
            }
        }

        private void OnAutoSettlementComplete(string period, int playerCount, decimal totalProfit)
        {
            Log($"[机器人] 第{period}期自动结算完成，玩家数: {playerCount}，总盈利: {totalProfit:F2}");
        }

        private void OnLotteryResultUpdated(LotteryResult result)
        {
            if (result == null) return;
            
            var periodNumber = result.Period;
            var winningNumber = result.Sum;
            
            // 结算完成后处理
            // 1. 结算猜数字
            var winners = GuessNumberService.Instance.Settle(periodNumber, winningNumber);
            if (winners.Count > 0 && !string.IsNullOrEmpty(_currentTeamId))
            {
                var config = GuessNumberService.Instance.GetConfig();
                if (config.ShowWinner)
                {
                    var winnerList = string.Join("\n", winners.Select(w => 
                        $"🎉 {w.PlayerNick} 猜中{w.GuessNumber}，奖励{w.Reward:F2}"));
                    SendGroupMessage(_currentTeamId, $"【猜数字开奖】第{periodNumber}期\n开奖号码: {winningNumber}\n{winnerList}");
                }
            }

            // 2. 记录长龙
            var bigSmall = winningNumber >= 14 ? "大" : "小";
            var oddEven = winningNumber % 2 == 1 ? "单" : "双";
            var special = GetSpecialResult(winningNumber);
            DragonReduceService.Instance.RecordResult(winningNumber, bigSmall, oddEven, special);

            // 3. 通知托管服务开奖完成
            if (!string.IsNullOrEmpty(_currentTeamId))
            {
                TrusteeService.Instance.OnDrawComplete(_currentTeamId);
            }

            Log($"[机器人] 第{periodNumber}期结算完成，开奖号码: {winningNumber}");
        }

        private void OnTrusteeAutoBet(string teamId, string playerId, string betContent)
        {
            // 托管自动下注
            Log($"[托管] 玩家 {playerId} 自动下注: {betContent}");

            // 创建虚拟消息上下文并处理下注
            var context = new MessageContext
            {
                TeamId = teamId,
                SenderId = playerId,
                SenderNick = playerId, // 实际应获取昵称
                Text = betContent,
                IsFromBot = false,
                IsSealed = false
            };

            // 使用下注处理器处理
            Task.Run(async () =>
            {
                var result = await _betHandler.HandleAsync(context);
                if (result.IsHandled && !string.IsNullOrEmpty(result.ReplyMessage))
                {
                    // 托管下注结果私聊通知
                    SendPrivateMessage(playerId, $"[托管下注]\n{result.ReplyMessage}");
                }
            });
        }

        private void OnGuessSuccess(string playerId, string nick, int number, decimal reward)
        {
            Log($"[猜数字] {nick} 猜中{number}，奖励{reward:F2}");
        }

        private void OnDragonReduce(string category, string result, int count, decimal reduction)
        {
            Log($"[长龙] {category}-{result} 连开{count}次，减赔{reduction:F2}");
        }

        private void OnBonusGiven(string playerId, string nick, decimal amount, string type)
        {
            Log($"[返点] {nick} 获得{type}奖励 {amount:F2}");
        }

        private string GetSpecialResult(int sum)
        {
            // 判断特殊结果 (需要知道三个数字)
            // 这里简化处理，实际应从开奖数据获取
            return "";
        }

        // 发言检测事件处理
        private void MutePlayer(string teamId, string playerId, int minutes)
        {
            Log($"[群管理] 禁言玩家 {playerId} {minutes}分钟");
            // 调用ChatService执行禁言
            // ChatService.Instance.MutePlayerAsync(teamId, playerId, minutes);
        }

        private void KickPlayer(string teamId, string playerId)
        {
            Log($"[群管理] 踢出玩家 {playerId}");
            // 调用ChatService执行踢人
            // ChatService.Instance.KickPlayerAsync(teamId, playerId);
        }

        private void WithdrawMessage(string teamId, string messageId)
        {
            Log($"[群管理] 撤回消息 {messageId}");
            // 调用ChatService执行撤回
            // ChatService.Instance.WithdrawMessageAsync(teamId, messageId);
        }

        // 锁名片事件处理
        private void OnCardChanged(string teamId, string playerId, string oldCard, string newCard)
        {
            Log($"[锁名片] 玩家 {playerId} 修改名片: {oldCard} -> {newCard}");
        }

        private void ResetPlayerCard(string teamId, string playerId, string originalCard)
        {
            Log($"[锁名片] 重置玩家 {playerId} 名片为 {originalCard}");
            // 调用ChatService执行重置名片
            // ChatService.Instance.SetPlayerCardAsync(teamId, playerId, originalCard);
        }

        // 进群欢迎事件处理
        private void AcceptFriendRequest(string requestId, bool accept)
        {
            Log($"[好友申请] {(accept ? "同意" : "拒绝")} 请求 {requestId}");
            // 调用ChatService处理好友请求
            // ChatService.Instance.HandleFriendRequestAsync(requestId, accept);
        }

        private void AcceptJoinRequest(string requestId, bool accept)
        {
            Log($"[入群申请] {(accept ? "同意" : "拒绝")} 请求 {requestId}");
            // 调用ChatService处理入群请求
            // ChatService.Instance.HandleJoinRequestAsync(requestId, accept);
        }

        /// <summary>
        /// 处理成员进群事件
        /// </summary>
        public async Task OnMemberJoinedAsync(string teamId, string playerId, string playerNick)
        {
            var isSealed = SealingService.Instance.GetCurrentState() >= SealingState.Sealed;
            await WelcomeService.Instance.OnMemberJoined(teamId, playerId, playerNick, isSealed);
        }

        /// <summary>
        /// 处理成员离开事件
        /// </summary>
        public void OnMemberLeft(string teamId, string playerId, string playerNick, bool isKicked, string operatorId)
        {
            WelcomeService.Instance.OnMemberLeft(teamId, playerId, playerNick, isKicked, operatorId);
        }

        /// <summary>
        /// 处理名片修改事件
        /// </summary>
        public void OnCardModified(string teamId, string playerId, string newCard)
        {
            CardLockService.Instance.OnCardChange(teamId, playerId, newCard);
        }

        #endregion

        #region 启动/停止

        /// <summary>
        /// 启动机器人
        /// </summary>
        public async Task<bool> StartAsync(string teamId)
        {
            if (_isRunning)
            {
                Log("[机器人] 已在运行中");
                return true;
            }

            try
            {
                Log($"[机器人] 正在启动...");

                // 1. 连接旺商聊
                var chatService = ChatService.Instance;
                if (!chatService.IsConnected)
                {
                    Log("[机器人] 正在连接旺商聊...");
                    var connected = await chatService.ConnectAsync();
                    if (!connected)
                    {
                        Log("[机器人] 连接旺商聊失败！");
                        return false;
                    }
                }

                // 2. 设置当前群
                _currentTeamId = teamId;
                _betHandler.EnableTeam(teamId);

                // 3. 启动消息调度器
                MessageDispatcher.Instance.Start();

                // 4. 订阅消息接收事件
                chatService.OnMessageReceived -= OnMessageReceived;
                chatService.OnMessageReceived += OnMessageReceived;

                // 5. 计算当前期号和开奖时间
                CalculateCurrentPeriod();

                // 6. 启动封盘服务
                var sealingConfig = SealingService.Instance.GetConfig();
                sealingConfig.TeamId = teamId;
                SealingService.Instance.SaveConfig(sealingConfig);

                var nextDrawTime = CalculateNextDrawTime();
                SealingService.Instance.Start(_currentPeriod, nextDrawTime);

                _isRunning = true;
                OnRunningStateChanged?.Invoke(true);

                Log($"[机器人] 启动成功！当前期号:{_currentPeriod}, 群:{teamId}");
                return true;
            }
            catch (Exception ex)
            {
                Log($"[机器人] 启动异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 停止机器人
        /// </summary>
        public void Stop()
        {
            if (!_isRunning) return;

            try
            {
                Log("[机器人] 正在停止...");

                // 停止各服务
                MessageDispatcher.Instance.Stop();
                SealingService.Instance.Stop();

                // 取消消息订阅
                ChatService.Instance.OnMessageReceived -= OnMessageReceived;

                // 禁用群
                if (!string.IsNullOrEmpty(_currentTeamId))
                {
                    _betHandler.DisableTeam(_currentTeamId);
                }

                _isRunning = false;
                OnRunningStateChanged?.Invoke(false);

                Log("[机器人] 已停止");
            }
            catch (Exception ex)
            {
                Log($"[机器人] 停止异常: {ex.Message}");
            }
        }

        #endregion

        #region 消息处理

        private void OnMessageReceived(ChatMessage message)
        {
            if (!_isRunning) return;

            // 忽略自己发送的消息
            if (message.IsSelf) return;

            // 只处理当前群的消息
            if (message.IsGroupMessage && message.TeamId != _currentTeamId) return;

            // 入队处理
            MessageDispatcher.Instance.EnqueueMessage(message);
        }

        #endregion

        #region 消息发送

        private void SendGroupMessage(string teamId, string content)
        {
            try
            {
                if (string.IsNullOrEmpty(content)) return;

                // 优先使用XPluginProtocol (ZCG原版兼容)
                var xplugin = XClient.XPluginProtocol.Instance;
                if (xplugin.IsConnected)
                {
                    _ = xplugin.SendGroupMessageAsync(teamId, content);
                    Log($"[发送群消息-XPlugin] {teamId}: {content.Substring(0, Math.Min(50, content.Length))}...");
                    return;
                }

                // 回退到ChatService
                _ = ChatService.Instance.SendTextAsync("team", teamId, content);
                Log($"[发送群消息] {teamId}: {content.Substring(0, Math.Min(50, content.Length))}...");
            }
            catch (Exception ex)
            {
                Log($"[发送群消息] 失败: {ex.Message}");
            }
        }

        private void SendPrivateMessage(string userId, string content)
        {
            try
            {
                if (string.IsNullOrEmpty(content)) return;

                // 优先使用XPluginProtocol (ZCG原版兼容)
                var xplugin = XClient.XPluginProtocol.Instance;
                if (xplugin.IsConnected)
                {
                    _ = xplugin.SendPrivateMessageAsync(userId, content);
                    Log($"[发送私聊-XPlugin] {userId}: {content.Substring(0, Math.Min(50, content.Length))}...");
                    return;
                }

                _ = ChatService.Instance.SendTextAsync("p2p", userId, content);
                Log($"[发送私聊] {userId}: {content.Substring(0, Math.Min(50, content.Length))}...");
            }
            catch (Exception ex)
            {
                Log($"[发送私聊] 失败: {ex.Message}");
            }
        }

        private void SendImage(string teamId, string imageFolder)
        {
            try
            {
                // 从图片文件夹获取图片并发送
                var imagePath = System.IO.Path.Combine(
                    DataService.Instance.DatabaseDir, "Images", imageFolder);

                if (System.IO.Directory.Exists(imagePath))
                {
                    var files = System.IO.Directory.GetFiles(imagePath, "*.png");
                    if (files.Length == 0)
                        files = System.IO.Directory.GetFiles(imagePath, "*.jpg");

                    if (files.Length > 0)
                    {
                        _ = ChatService.Instance.SendImageAsync("team", teamId, files[0]);
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"[发送图片] 失败: {ex.Message}");
            }
        }

        private void MuteGroup(string teamId)
        {
            try
            {
                // 优先使用XPluginProtocol (ZCG原版兼容)
                var xplugin = XClient.XPluginProtocol.Instance;
                if (xplugin.IsConnected)
                {
                    _ = xplugin.MuteGroupAsync(teamId, true);
                    Log($"[禁言-XPlugin] 群{teamId}已禁言");
                    return;
                }

                Log($"[禁言] 群{teamId}已禁言");
                // 通过ChatService执行禁言操作
                // 具体实现取决于旺商聊的API
            }
            catch (Exception ex)
            {
                Log($"[禁言] 失败: {ex.Message}");
            }
        }

        #endregion

        #region 开奖处理

        /// <summary>
        /// 处理开奖结果
        /// </summary>
        public async Task ProcessLotteryResultAsync(int d1, int d2, int d3, int sum)
        {
            try
            {
                Log($"[开奖] 第{_currentPeriod}期: {d1}+{d2}+{d3}={sum}");

                var result = new LotteryResult
                {
                    Period = _currentPeriod,
                    Dice1 = d1,
                    Dice2 = d2,
                    Dice3 = d3,
                    Sum = sum
                };

                // 结算
                await AutoSettlementService.Instance.ProcessLotteryResultAsync(
                    _currentPeriod, result, _currentTeamId);

                // 解禁言
                // UnmuteGroup(_currentTeamId);

                // 切换到下一期
                CalculateCurrentPeriod();
                var nextDrawTime = CalculateNextDrawTime();
                SealingService.Instance.UpdatePeriod(_currentPeriod, nextDrawTime);
                _betHandler.SetCurrentPeriod(_currentPeriod);

                Log($"[开奖] 切换到下一期: {_currentPeriod}");
            }
            catch (Exception ex)
            {
                Log($"[开奖] 处理异常: {ex.Message}");
            }
        }

        private void OnPeriodChanged(string oldPeriod, string newPeriod)
        {
            _currentPeriod = newPeriod;
            _betHandler.SetCurrentPeriod(newPeriod);
        }

        #endregion

        #region 期号计算

        private void CalculateCurrentPeriod()
        {
            var config = SealingService.Instance.GetConfig();
            var now = DateTime.Now;
            var baseTime = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0);
            var secondsToday = (int)(now - baseTime).TotalSeconds;
            var periodNum = secondsToday / config.DrawIntervalSeconds;

            _currentPeriod = now.ToString("yyyyMMdd") + periodNum.ToString("D3");
            _betHandler.SetCurrentPeriod(_currentPeriod);
        }

        private DateTime CalculateNextDrawTime()
        {
            var config = SealingService.Instance.GetConfig();
            var now = DateTime.Now;
            var baseTime = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0);
            var secondsToday = (int)(now - baseTime).TotalSeconds;
            var currentPeriodNum = secondsToday / config.DrawIntervalSeconds;

            return baseTime.AddSeconds((currentPeriodNum + 1) * config.DrawIntervalSeconds);
        }

        private string GetLotteryHistory(int count)
        {
            // 获取开奖历史
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"最近{count}期开奖历史:");
            sb.AppendLine("------------------------");

            try
            {
                // 从LotteryService获取历史数据
                var history = LotteryService.Instance.GetRecentResults(count);
                if (history != null && history.Count > 0)
                {
                    foreach (var result in history)
                    {
                        sb.AppendLine($"第{result.PeriodNumber}期: {result.Numbers} = {result.Sum}");
                    }
                }
                else
                {
                    sb.AppendLine("暂无历史数据");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[BotController] 获取开奖历史失败: {ex.Message}");
                sb.AppendLine("获取历史数据失败");
            }

            return sb.ToString();
        }

        #endregion

        #region 配置方法

        /// <summary>
        /// 设置上下分配置
        /// </summary>
        public void SetScoreConfig(ScoreHandlerConfig config)
        {
            _scoreHandler.SetConfig(config);
        }

        /// <summary>
        /// 添加自动回复规则
        /// </summary>
        public void AddAutoReplyRule(AutoReplyRule rule)
        {
            _autoReplyHandler.AddRule(rule);
        }

        /// <summary>
        /// 获取当前赔率配置
        /// </summary>
        public Models.Betting.FullOddsConfig GetOddsConfig()
        {
            return OddsService.Instance.GetConfig();
        }

        /// <summary>
        /// 设置赔率配置
        /// </summary>
        public void SetOddsConfig(Models.Betting.FullOddsConfig config)
        {
            OddsService.Instance.SaveConfig(config);
        }

        #endregion

        #region 管理操作

        /// <summary>
        /// 手动上分
        /// </summary>
        public decimal ManualDeposit(string playerId, decimal amount, string reason = "管理员上分")
        {
            return ScoreService.Instance.AddScore(playerId, amount, reason);
        }

        /// <summary>
        /// 手动下分
        /// </summary>
        public (bool success, decimal balance, string error) ManualWithdraw(string playerId, decimal amount, string reason = "管理员下分")
        {
            return ScoreService.Instance.DeductScore(playerId, amount, reason);
        }

        /// <summary>
        /// 获取玩家余额
        /// </summary>
        public decimal GetPlayerBalance(string playerId)
        {
            return ScoreService.Instance.GetBalance(playerId);
        }

        /// <summary>
        /// 获取当前期下注核对
        /// </summary>
        public string GetBetCheckMessage()
        {
            return AutoSettlementService.Instance.GenerateBetCheckMessage(_currentPeriod, _currentTeamId);
        }

        #endregion

        private void Log(string message)
        {
            OnLog?.Invoke(message);
            Logger.Info(message);
        }
    }
}
