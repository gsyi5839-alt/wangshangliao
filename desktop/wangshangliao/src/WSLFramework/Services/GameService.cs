using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using WSLFramework.Utils;

namespace WSLFramework.Services
{
    /// <summary>
    /// 游戏服务 - 整合开奖、下注、消息处理的核心服务
    /// 对应招财狗的游戏核心逻辑
    /// </summary>
    public class GameService : IDisposable
    {
        private readonly PlayerService _playerService;
        private readonly LotteryService _lotteryService;
        private CDPBridge _cdpBridge;
        
        private System.Timers.Timer _msgPollTimer;
        private CancellationTokenSource _cts;
        
        // 游戏状态
        public bool IsRunning { get; private set; }
        public GameState State { get; private set; } = GameState.Idle;
        public string CurrentPeriod { get; private set; }
        public int Countdown { get; private set; }
        
        // 配置
        public string GroupId { get; set; }              // 当前群号
        public int BetCloseSeconds { get; set; } = 30;   // 封盘前秒数
        public bool AutoAnnounce { get; set; } = true;   // 自动播报开奖
        public bool AutoSettle { get; set; } = true;     // 自动结算
        public bool AutoMute { get; set; } = false;      // 自动禁言
        
        // 事件
        public event Action<string> OnLog;
        public event Action<GameState, int> OnStateChanged;      // 状态变化(状态, 倒计时)
        public event Action<string, string> OnSendMessage;       // 需要发送消息(群号, 内容)
        public event Action<LotteryResult> OnNewResult;          // 新开奖
        public event Action<SettlementResult> OnSettlement;      // 结算完成
        public event Action<GroupMessageEvent> OnGroupMessage;   // 收到群消息
        
        public GameService(PlayerService playerService)
        {
            _playerService = playerService;
            _lotteryService = new LotteryService();
            
            // 开奖服务事件
            _lotteryService.OnNewResult += HandleNewResult;
            _lotteryService.OnCountdown += HandleCountdown;
            _lotteryService.OnLog += msg => Log(msg);
        }
        
        /// <summary>
        /// 设置 CDP 桥接
        /// </summary>
        public void SetCDPBridge(CDPBridge bridge)
        {
            _cdpBridge = bridge;
        }
        
        /// <summary>
        /// 设置彩种类型
        /// </summary>
        public void SetLotteryType(string typeName)
        {
            var typeMap = new Dictionary<string, int>
            {
                { "加拿大", 1 },
                { "北京28", 2 },
                { "台湾28", 3 },
                { "澳洲28", 4 }
            };
            
            if (typeMap.TryGetValue(typeName, out int typeId))
            {
                _lotteryService?.SetLotteryType(typeId);
                ConfigService.Instance.LotteryType = typeId;
                Log($"已切换彩种: {typeName}");
            }
        }
        
        /// <summary>
        /// 启动游戏服务
        /// </summary>
        public async Task StartAsync()
        {
            if (IsRunning) return;
            
            _cts = new CancellationTokenSource();
            IsRunning = true;
            State = GameState.Idle;
            
            // 加载玩家数据
            _playerService.LoadData();
            
            // 启动开奖轮询
            await _lotteryService.StartAsync();
            
            // 启动消息轮询
            StartMessagePolling();
            
            // 启用CDP消息监听
            if (_cdpBridge?.IsConnected == true)
            {
                await _cdpBridge.EnableMessageListeningAsync();
            }
            
            Log("游戏服务已启动");
        }
        
        /// <summary>
        /// 停止游戏服务
        /// </summary>
        public void Stop()
        {
            _cts?.Cancel();
            _lotteryService.Stop();
            _msgPollTimer?.Stop();
            
            // 保存数据
            _playerService.SaveData();
            
            IsRunning = false;
            State = GameState.Idle;
            
            Log("游戏服务已停止");
        }
        
        /// <summary>
        /// 处理群消息
        /// </summary>
        public async Task<string> ProcessMessageAsync(string senderId, string senderNick, string content)
        {
            if (string.IsNullOrEmpty(content)) return null;
            
            content = content.Trim();
            
            // 解析下注
            var bet = MessageParser.ParseBet(content);
            if (bet == null || !bet.IsValid)
            {
                return null;
            }
            
            // 处理上分请求
            if (bet.BetType == "UP")
            {
                return HandleUpRequest(senderId, senderNick, bet.Amount);
            }
            
            // 处理下分请求
            if (bet.BetType == "DOWN")
            {
                return HandleDownRequest(senderId, senderNick, bet.Amount);
            }
            
            // 处理余额查询
            if (bet.IsQuery)
            {
                return HandleQuery(senderId, senderNick);
            }
            
            // 处理下注
            if (State != GameState.Betting)
            {
                return $"@{senderNick} 当前无法下注，请等待开盘";
            }
            
            var result = _playerService.PlaceBet(senderId, bet.BetType, bet.Amount, CurrentPeriod);
            return $"@{senderNick} {result.Message}";
        }
        
        /// <summary>
        /// 处理上分请求
        /// </summary>
        private string HandleUpRequest(string playerId, string nickname, int amount)
        {
            // 触发上分请求事件，由UI处理
            Log($"收到上分请求: {nickname} 上{amount}");
            return null; // 返回null表示不自动回复，等待手动处理
        }
        
        /// <summary>
        /// 处理下分请求
        /// </summary>
        private string HandleDownRequest(string playerId, string nickname, int amount)
        {
            // 触发下分请求事件，由UI处理
            Log($"收到下分请求: {nickname} 下{amount}");
            return null;
        }
        
        /// <summary>
        /// 处理余额查询
        /// </summary>
        private string HandleQuery(string playerId, string nickname)
        {
            var balance = _playerService.QueryBalance(playerId);
            return $"@{nickname}\n{balance}";
        }
        
        /// <summary>
        /// 处理新开奖
        /// </summary>
        private async void HandleNewResult(LotteryResult result)
        {
            try
            {
                CurrentPeriod = result.Period;
                _playerService.CurrentPeriod = result.Period;
                
                Log($"新开奖: {result.GetOpenMessage()}");
                OnNewResult?.Invoke(result);
                
                // 结算上一期
                if (AutoSettle)
                {
                    var settlement = _playerService.Settlement(result.Period, result);
                    if (settlement.TotalBets > 0)
                    {
                        OnSettlement?.Invoke(settlement);
                        
                        // 发送账单
                        if (AutoAnnounce && !string.IsNullOrEmpty(GroupId))
                        {
                            var billMsg = settlement.GetBillMessage();
                            OnSendMessage?.Invoke(GroupId, billMsg);
                            
                            if (_cdpBridge?.IsConnected == true)
                            {
                                await _cdpBridge.SendGroupMessageAsync(GroupId, billMsg);
                            }
                        }
                    }
                }
                
                // 播报开奖
                if (AutoAnnounce && !string.IsNullOrEmpty(GroupId))
                {
                    var announceMsg = result.GetOpenMessage();
                    OnSendMessage?.Invoke(GroupId, announceMsg);
                    
                    if (_cdpBridge?.IsConnected == true)
                    {
                        await _cdpBridge.SendGroupMessageAsync(GroupId, announceMsg);
                    }
                }
                
                // 更新状态为下注中
                State = GameState.Betting;
                OnStateChanged?.Invoke(State, 210);
            }
            catch (Exception ex)
            {
                Log($"处理开奖异常: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 处理倒计时
        /// </summary>
        private async void HandleCountdown(int seconds)
        {
            Countdown = seconds;
            
            // 封盘检查
            if (State == GameState.Betting && seconds <= BetCloseSeconds)
            {
                State = GameState.Closed;
                OnStateChanged?.Invoke(State, seconds);
                
                // 发送封盘提示
                if (AutoAnnounce && !string.IsNullOrEmpty(GroupId))
                {
                    var closeMsg = $"⏰ 距离开奖还有 {seconds} 秒，已封盘！";
                    OnSendMessage?.Invoke(GroupId, closeMsg);
                    
                    if (_cdpBridge?.IsConnected == true)
                    {
                        await _cdpBridge.SendGroupMessageAsync(GroupId, closeMsg);
                    }
                }
                
                // 自动禁言
                if (AutoMute && _cdpBridge?.IsConnected == true)
                {
                    await _cdpBridge.MuteAllAsync(GroupId, true);
                }
            }
            else if (State == GameState.Closed && seconds > BetCloseSeconds)
            {
                // 开盘
                State = GameState.Betting;
                OnStateChanged?.Invoke(State, seconds);
                
                // 自动解禁
                if (AutoMute && _cdpBridge?.IsConnected == true)
                {
                    await _cdpBridge.MuteAllAsync(GroupId, false);
                }
            }
            
            OnStateChanged?.Invoke(State, seconds);
        }
        
        /// <summary>
        /// 启动消息轮询
        /// </summary>
        private void StartMessagePolling()
        {
            _msgPollTimer = new System.Timers.Timer(500); // 500ms轮询
            _msgPollTimer.Elapsed += async (s, e) =>
            {
                if (_cdpBridge?.IsConnected == true)
                {
                    var msg = await _cdpBridge.GetLastMessageAsync();
                    if (msg != null)
                    {
                        OnGroupMessage?.Invoke(msg);
                        
                        // 自动处理消息
                        var reply = await ProcessMessageAsync(msg.SenderId, msg.SenderNick, msg.Content);
                        if (!string.IsNullOrEmpty(reply))
                        {
                            await _cdpBridge.SendGroupMessageAsync(msg.GroupId, reply);
                        }
                    }
                }
            };
            _msgPollTimer.Start();
        }
        
        /// <summary>
        /// 手动上分
        /// </summary>
        public string ManualUp(string playerId, int amount)
        {
            var result = _playerService.AddScore(playerId, amount);
            return result.Message;
        }
        
        /// <summary>
        /// 手动下分
        /// </summary>
        public string ManualDown(string playerId, int amount)
        {
            var result = _playerService.DeductScore(playerId, amount);
            return result.Message;
        }
        
        /// <summary>
        /// 获取当前下注列表
        /// </summary>
        public List<BetRecord> GetCurrentBets()
        {
            return _playerService.GetCurrentBets();
        }
        
        /// <summary>
        /// 获取所有玩家
        /// </summary>
        public List<PlayerInfo> GetAllPlayers()
        {
            return _playerService.GetAllPlayers();
        }
        
        /// <summary>
        /// 取消当期所有下注
        /// </summary>
        public void CancelAllBets()
        {
            _playerService.CancelAllBets(CurrentPeriod);
            Log("已取消当期所有下注");
        }
        
        /// <summary>
        /// 设置开奖API配置
        /// </summary>
        public void SetLotteryConfig(string apiUrl, string backupUrl, string token)
        {
            _lotteryService.ApiUrl = apiUrl;
            _lotteryService.BackupUrl = backupUrl;
            _lotteryService.Token = token;
        }
        
        private void Log(string message)
        {
            Logger.Info($"[游戏] {message}");
            OnLog?.Invoke(message);
        }
        
        public void Dispose()
        {
            Stop();
            _msgPollTimer?.Dispose();
            _lotteryService?.Dispose();
        }
    }
    
    /// <summary>
    /// 游戏状态
    /// </summary>
    public enum GameState
    {
        Idle,      // 空闲
        Betting,   // 下注中
        Closed,    // 已封盘
        Settling   // 结算中
    }
}
