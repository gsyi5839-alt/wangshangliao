using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Timers;
using WangShangLiaoBot.Models.Betting;
using WangShangLiaoBot.Services.Betting;

namespace WangShangLiaoBot.Services
{
    /// <summary>
    /// 托管自动下注服务 - 基于招财狗(ZCG)的私聊版托系统
    /// 支持分数段策略配置，自动根据余额选择下注内容
    /// </summary>
    public sealed class TrusteeService
    {
        private static TrusteeService _instance;
        public static TrusteeService Instance => _instance ?? (_instance = new TrusteeService());

        private TrusteeConfig _config;
        private readonly Dictionary<string, TrusteePlayer> _trustees = new Dictionary<string, TrusteePlayer>();
        private readonly object _lock = new object();
        private Timer _autoCheckTimer;

        // 事件
        public event Action<string, string, string> OnAutobet; // teamId, playerId, betContent
        public event Action<string> OnLog;

        private TrusteeService()
        {
            LoadConfig();
            InitTimer();
        }

        private string ConfigPath => Path.Combine(DataService.Instance.DatabaseDir, "trustee-config.ini");

        #region 配置管理

        public TrusteeConfig GetConfig()
        {
            lock (_lock)
            {
                return _config ?? TrusteeConfig.CreateDefault();
            }
        }

        public void SaveConfig(TrusteeConfig config)
        {
            lock (_lock)
            {
                _config = config;
                SaveConfigToFile(config);
            }
        }

        private void LoadConfig()
        {
            try
            {
                if (!File.Exists(ConfigPath))
                {
                    _config = TrusteeConfig.CreateDefault();
                    return;
                }

                var config = new TrusteeConfig();
                var lines = File.ReadAllLines(ConfigPath, Encoding.UTF8);
                TrusteeStrategy currentStrategy = null;

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;

                    // 检测策略段
                    var strategyMatch = Regex.Match(line, @"\[策略_(\d+)\]");
                    if (strategyMatch.Success)
                    {
                        if (currentStrategy != null)
                            config.Strategies.Add(currentStrategy);
                        currentStrategy = new TrusteeStrategy();
                        continue;
                    }

                    var idx = line.IndexOf('=');
                    if (idx <= 0) continue;

                    var key = line.Substring(0, idx).Trim();
                    var value = line.Substring(idx + 1).Trim();

                    if (currentStrategy != null)
                    {
                        // 策略配置
                        switch (key)
                        {
                            case "MinBalance":
                                if (decimal.TryParse(value, out var min)) currentStrategy.MinBalance = min;
                                break;
                            case "MaxBalance":
                                if (decimal.TryParse(value, out var max)) currentStrategy.MaxBalance = max;
                                break;
                            case "BetContent":
                                currentStrategy.BetContents = value.Split('|').ToList();
                                break;
                        }
                    }
                    else
                    {
                        // 全局配置
                        switch (key)
                        {
                            case "Enabled":
                                config.Enabled = value == "true" || value == "1" || value == "真";
                                break;
                            case "DelayAfterDraw":
                                if (int.TryParse(value, out var dad)) config.DelayAfterDraw = dad;
                                break;
                            case "DelayBeforeSeal":
                                if (int.TryParse(value, out var dbs)) config.DelayBeforeSeal = dbs;
                                break;
                            case "AutoDeposit":
                                config.AutoDeposit = value == "true" || value == "1" || value == "真";
                                break;
                            case "AutoWithdraw":
                                config.AutoWithdraw = value == "true" || value == "1" || value == "真";
                                break;
                            case "DepositDelayMin":
                                if (int.TryParse(value, out var ddm)) config.DepositDelayMin = ddm;
                                break;
                            case "DepositDelayMax":
                                if (int.TryParse(value, out var ddx)) config.DepositDelayMax = ddx;
                                break;
                            case "WithdrawDelayMin":
                                if (int.TryParse(value, out var wdm)) config.WithdrawDelayMin = wdm;
                                break;
                            case "WithdrawDelayMax":
                                if (int.TryParse(value, out var wdx)) config.WithdrawDelayMax = wdx;
                                break;
                        }
                    }
                }

                if (currentStrategy != null)
                    config.Strategies.Add(currentStrategy);

                _config = config;
            }
            catch
            {
                _config = TrusteeConfig.CreateDefault();
            }
        }

        private void SaveConfigToFile(TrusteeConfig config)
        {
            try
            {
                Directory.CreateDirectory(DataService.Instance.DatabaseDir);
                var sb = new StringBuilder();
                sb.AppendLine("# 托管配置 - 自动生成");
                sb.AppendLine($"Enabled={config.Enabled}");
                sb.AppendLine($"DelayAfterDraw={config.DelayAfterDraw}");
                sb.AppendLine($"DelayBeforeSeal={config.DelayBeforeSeal}");
                sb.AppendLine($"AutoDeposit={config.AutoDeposit}");
                sb.AppendLine($"AutoWithdraw={config.AutoWithdraw}");
                sb.AppendLine($"DepositDelayMin={config.DepositDelayMin}");
                sb.AppendLine($"DepositDelayMax={config.DepositDelayMax}");
                sb.AppendLine($"WithdrawDelayMin={config.WithdrawDelayMin}");
                sb.AppendLine($"WithdrawDelayMax={config.WithdrawDelayMax}");
                sb.AppendLine();

                for (int i = 0; i < config.Strategies.Count; i++)
                {
                    var s = config.Strategies[i];
                    sb.AppendLine($"[策略_{i + 1}]");
                    sb.AppendLine($"MinBalance={s.MinBalance.ToString(CultureInfo.InvariantCulture)}");
                    sb.AppendLine($"MaxBalance={s.MaxBalance.ToString(CultureInfo.InvariantCulture)}");
                    sb.AppendLine($"BetContent={string.Join("|", s.BetContents)}");
                    sb.AppendLine();
                }

                File.WriteAllText(ConfigPath, sb.ToString(), Encoding.UTF8);
            }
            catch { }
        }

        #endregion

        #region 托管管理

        /// <summary>
        /// 添加托管玩家
        /// </summary>
        public bool AddTrustee(string playerId, string playerNick, string teamId, string customBet = null)
        {
            lock (_lock)
            {
                if (_trustees.ContainsKey(playerId))
                    return false;

                _trustees[playerId] = new TrusteePlayer
                {
                    PlayerId = playerId,
                    PlayerNick = playerNick,
                    TeamId = teamId,
                    CustomBetContent = customBet,
                    StartTime = DateTime.Now,
                    TotalBets = 0,
                    IsActive = true
                };

                Log($"[托管] 玩家 {playerNick} 开启托管");
                return true;
            }
        }

        /// <summary>
        /// 移除托管玩家
        /// </summary>
        public bool RemoveTrustee(string playerId)
        {
            lock (_lock)
            {
                if (_trustees.TryGetValue(playerId, out var player))
                {
                    _trustees.Remove(playerId);
                    Log($"[托管] 玩家 {player.PlayerNick} 取消托管，共下注 {player.TotalBets} 次");
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// 检查玩家是否在托管中
        /// </summary>
        public bool IsTrustee(string playerId)
        {
            lock (_lock)
            {
                return _trustees.ContainsKey(playerId) && _trustees[playerId].IsActive;
            }
        }

        /// <summary>
        /// 获取托管玩家列表
        /// </summary>
        public List<TrusteePlayer> GetTrustees()
        {
            lock (_lock)
            {
                return _trustees.Values.ToList();
            }
        }

        #endregion

        #region 自动下注

        private void InitTimer()
        {
            _autoCheckTimer = new Timer(1000);
            _autoCheckTimer.Elapsed += OnTimerTick;
            _autoCheckTimer.AutoReset = true;
        }

        /// <summary>
        /// 启动托管服务
        /// </summary>
        public void Start()
        {
            _autoCheckTimer.Start();
            Log("[托管服务] 已启动");
        }

        /// <summary>
        /// 停止托管服务
        /// </summary>
        public void Stop()
        {
            _autoCheckTimer.Stop();
            Log("[托管服务] 已停止");
        }

        private void OnTimerTick(object sender, ElapsedEventArgs e)
        {
            // 检查是否应该下注的时机由外部调用 TriggerAutoBet
        }

        /// <summary>
        /// 触发托管下注 (在封盘提醒时调用)
        /// </summary>
        public void TriggerAutoBet(string teamId, int secondsToSeal)
        {
            var config = GetConfig();
            if (!config.Enabled) return;

            // 检查封盘状态 - 如果已封盘则不下注
            try
            {
                var sealingState = SealingService.Instance.GetCurrentState();
                if (sealingState >= SealingState.Sealed)
                {
                    Log($"[托管] 当前已封盘，跳过下注");
                    return;
                }
            }
            catch { /* SealingService可能未初始化，忽略 */ }

            // 封盘前不下注时间检查
            if (secondsToSeal <= config.DelayBeforeSeal)
            {
                Log($"[托管] 距离封盘仅{secondsToSeal}秒，跳过下注");
                return;
            }

            lock (_lock)
            {
                foreach (var player in _trustees.Values.Where(p => p.IsActive && p.TeamId == teamId))
                {
                    try
                    {
                        ProcessPlayerAutoBet(player);
                    }
                    catch (Exception ex)
                    {
                        Log($"[托管] 玩家 {player.PlayerNick} 下注异常: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// 开奖后延迟检查 (用于判断是否继续托管)
        /// </summary>
        public void OnDrawComplete(string teamId)
        {
            var config = GetConfig();

            lock (_lock)
            {
                foreach (var player in _trustees.Values.Where(p => p.TeamId == teamId))
                {
                    // 获取玩家余额
                    var balance = ScoreService.Instance.GetBalance(player.PlayerId);

                    // 检查是否余额不足需要取消托管
                    var strategy = FindStrategy(balance);
                    if (strategy == null)
                    {
                        Log($"[托管] 玩家 {player.PlayerNick} 余额 {balance:F2} 无匹配策略，取消托管");
                        player.IsActive = false;
                    }
                }
            }
        }

        private void ProcessPlayerAutoBet(TrusteePlayer player)
        {
            var config = GetConfig();
            // 获取玩家余额
            var balance = ScoreService.Instance.GetBalance(player.PlayerId);

            // 获取下注内容
            string betContent;
            if (!string.IsNullOrEmpty(player.CustomBetContent))
            {
                betContent = player.CustomBetContent;
            }
            else
            {
                var strategy = FindStrategy(balance);
                if (strategy == null)
                {
                    Log($"[托管] 玩家 {player.PlayerNick} 余额 {balance:F2} 无匹配策略");
                    return;
                }
                betContent = SelectBetContent(strategy, player.TotalBets);
            }

            if (string.IsNullOrEmpty(betContent))
            {
                Log($"[托管] 玩家 {player.PlayerNick} 无有效下注内容");
                return;
            }

            // 触发下注事件
            OnAutobet?.Invoke(player.TeamId, player.PlayerId, betContent);
            player.TotalBets++;
            player.LastBetTime = DateTime.Now;

            Log($"[托管] 玩家 {player.PlayerNick} 自动下注: {betContent}");
        }

        private TrusteeStrategy FindStrategy(decimal balance)
        {
            var config = GetConfig();
            return config.Strategies.FirstOrDefault(s =>
                balance >= s.MinBalance && balance <= s.MaxBalance);
        }

        private string SelectBetContent(TrusteeStrategy strategy, int betIndex)
        {
            if (strategy.BetContents == null || strategy.BetContents.Count == 0)
                return null;

            // 循环选择下注内容
            var index = betIndex % strategy.BetContents.Count;
            return strategy.BetContents[index];
        }

        #endregion

        private void Log(string message)
        {
            OnLog?.Invoke(message);
            Logger.Info(message);
        }
    }

    #region 托管配置和模型

    /// <summary>
    /// 托管配置
    /// </summary>
    public class TrusteeConfig
    {
        /// <summary>是否启用托管</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>开奖后不下注时间(秒)</summary>
        public int DelayAfterDraw { get; set; } = 10;

        /// <summary>封盘前不下注时间(秒)</summary>
        public int DelayBeforeSeal { get; set; } = 5;

        /// <summary>托管自动上分</summary>
        public bool AutoDeposit { get; set; } = true;

        /// <summary>托管自动下分</summary>
        public bool AutoWithdraw { get; set; } = true;

        /// <summary>上分延迟最小秒数</summary>
        public int DepositDelayMin { get; set; } = 5;

        /// <summary>上分延迟最大秒数</summary>
        public int DepositDelayMax { get; set; } = 10;

        /// <summary>下分延迟最小秒数</summary>
        public int WithdrawDelayMin { get; set; } = 10;

        /// <summary>下分延迟最大秒数</summary>
        public int WithdrawDelayMax { get; set; } = 20;

        /// <summary>分数段策略</summary>
        public List<TrusteeStrategy> Strategies { get; set; } = new List<TrusteeStrategy>();

        public static TrusteeConfig CreateDefault()
        {
            return new TrusteeConfig
            {
                Strategies = new List<TrusteeStrategy>
                {
                    // 100-500分: 小额下注
                    new TrusteeStrategy
                    {
                        MinBalance = 100,
                        MaxBalance = 500,
                        BetContents = new List<string>
                        {
                            "da20|x22|d24|s26",
                            "dad28|das30|小单32|小双36",
                            "大60 大单20 大双20",
                            "小60 小双40",
                            "对子50|豹子30|顺子60"
                        }
                    },
                    // 501-1000分
                    new TrusteeStrategy
                    {
                        MinBalance = 501,
                        MaxBalance = 1000,
                        BetContents = new List<string>
                        {
                            "da40|x44|d48|s52",
                            "dad56|das60|小单64|小双72",
                            "大120 大单40 大双40",
                            "小120 小双80",
                            "对子100|豹子60|顺子120"
                        }
                    },
                    // 1001-5000分
                    new TrusteeStrategy
                    {
                        MinBalance = 1001,
                        MaxBalance = 5000,
                        BetContents = new List<string>
                        {
                            "da200|x220|d240|s260",
                            "dad280|das300|小单320|小双360",
                            "大660 大单120 大双120",
                            "小660 小双340",
                            "对子500|豹子300|顺子600"
                        }
                    },
                    // 5001-20000分
                    new TrusteeStrategy
                    {
                        MinBalance = 5001,
                        MaxBalance = 20000,
                        BetContents = new List<string>
                        {
                            "da1120|x1500|d1300|s1200",
                            "dad990|das960|小单950|小双910",
                            "大900 大单200 大双200",
                            "小700 小双300",
                            "对子890|豹子860|顺子1200"
                        }
                    }
                }
            };
        }
    }

    /// <summary>
    /// 托管策略 (分数段)
    /// </summary>
    public class TrusteeStrategy
    {
        /// <summary>最低余额</summary>
        public decimal MinBalance { get; set; }

        /// <summary>最高余额</summary>
        public decimal MaxBalance { get; set; }

        /// <summary>下注内容列表 (循环使用)</summary>
        public List<string> BetContents { get; set; } = new List<string>();
    }

    /// <summary>
    /// 托管玩家
    /// </summary>
    public class TrusteePlayer
    {
        public string PlayerId { get; set; }
        public string PlayerNick { get; set; }
        public string TeamId { get; set; }
        public string CustomBetContent { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? LastBetTime { get; set; }
        public int TotalBets { get; set; }
        public bool IsActive { get; set; }
    }

    #endregion
}
