using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Globalization;

namespace WangShangLiaoBot.Services
{
    /// <summary>
    /// 夜宵/流水返点服务 - 基于招财狗(ZCG)的返点系统
    /// 支持多种计算方式：把数、流水、输赢
    /// </summary>
    public sealed class BonusService
    {
        private static BonusService _instance;
        public static BonusService Instance => _instance ?? (_instance = new BonusService());

        private BonusConfig _config;
        private readonly object _lock = new object();

        // 事件
        public event Action<string, string, decimal, string> OnBonusGiven; // playerId, nick, amount, type
        public event Action<string> OnLog;

        private BonusService()
        {
            LoadConfig();
        }

        private string ConfigPath => Path.Combine(DataService.Instance.DatabaseDir, "bonus-config.ini");

        #region 配置管理

        public BonusConfig GetConfig()
        {
            lock (_lock)
            {
                return _config ?? BonusConfig.CreateDefault();
            }
        }

        public void SaveConfig(BonusConfig config)
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
                    _config = BonusConfig.CreateDefault();
                    return;
                }

                var config = new BonusConfig();
                var lines = File.ReadAllLines(ConfigPath, Encoding.UTF8);
                string currentSection = "";

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;

                    if (line.StartsWith("[") && line.EndsWith("]"))
                    {
                        currentSection = line.Substring(1, line.Length - 2);
                        continue;
                    }

                    var idx = line.IndexOf('=');
                    if (idx <= 0) continue;

                    var key = line.Substring(0, idx).Trim();
                    var value = line.Substring(idx + 1).Trim();

                    switch (currentSection)
                    {
                        case "夜宵设置":
                            ParseNightSnackConfig(config.NightSnack, key, value);
                            break;
                        case "流水返点":
                            ParseTurnoverRebateConfig(config.TurnoverRebate, key, value);
                            break;
                        case "":
                            // 全局配置
                            if (key == "CalculationMethod")
                            {
                                if (Enum.TryParse<BonusCalculationMethod>(value, out var method))
                                    config.CalculationMethod = method;
                            }
                            break;
                    }
                }

                _config = config;
            }
            catch
            {
                _config = BonusConfig.CreateDefault();
            }
        }

        private void ParseNightSnackConfig(NightSnackConfig config, string key, string value)
        {
            switch (key)
            {
                case "Enabled":
                    config.Enabled = value == "true" || value == "1" || value == "真";
                    break;
                case "NotifyInGroup":
                    config.NotifyInGroup = value == "true" || value == "1" || value == "真";
                    break;
                case "Rules":
                    config.Rules = ParseNightSnackRules(value);
                    break;
                case "WinRules":
                    config.WinRules = ParseWinLoseRules(value);
                    break;
                case "LoseRules":
                    config.LoseRules = ParseWinLoseRules(value);
                    break;
            }
        }

        private void ParseTurnoverRebateConfig(TurnoverRebateConfig config, string key, string value)
        {
            switch (key)
            {
                case "Enabled":
                    config.Enabled = value == "true" || value == "1" || value == "真";
                    break;
                case "DefaultPercent":
                    if (decimal.TryParse(value, out var dp)) config.DefaultPercent = dp;
                    break;
                case "DefaultMinBets":
                    if (int.TryParse(value, out var dmb)) config.DefaultMinBets = dmb;
                    break;
                case "Command":
                    config.Command = value;
                    break;
                case "DepositReason":
                    config.DepositReason = value;
                    break;
                case "HasRebateReply":
                    config.HasRebateReply = value;
                    break;
                case "NoRebateReply":
                    config.NoRebateReply = value;
                    break;
                case "NotEnoughBetsReply":
                    config.NotEnoughBetsReply = value;
                    break;
                case "TierRules":
                    config.TierRules = ParseTierRules(value);
                    break;
            }
        }

        private List<NightSnackRule> ParseNightSnackRules(string value)
        {
            // 格式: 30001-50000把数30送2888|50001-100000把数50送5888
            var rules = new List<NightSnackRule>();
            var parts = value.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in parts)
            {
                var match = System.Text.RegularExpressions.Regex.Match(part, 
                    @"(\d+)-(\d+)把数(\d+)送(\d+(?:\.\d+)?)");
                if (match.Success)
                {
                    rules.Add(new NightSnackRule
                    {
                        MinTurnover = decimal.Parse(match.Groups[1].Value),
                        MaxTurnover = decimal.Parse(match.Groups[2].Value),
                        MinBets = int.Parse(match.Groups[3].Value),
                        Bonus = decimal.Parse(match.Groups[4].Value)
                    });
                }
            }

            return rules.OrderBy(r => r.MinTurnover).ToList();
        }

        private List<WinLoseRule> ParseWinLoseRules(string value)
        {
            // 格式: 280000=888|500000=1888
            var rules = new List<WinLoseRule>();
            var parts = value.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in parts)
            {
                var idx = part.IndexOf('=');
                if (idx <= 0) continue;

                if (decimal.TryParse(part.Substring(0, idx).Trim(), out var amount) &&
                    decimal.TryParse(part.Substring(idx + 1).Trim(), out var bonus))
                {
                    rules.Add(new WinLoseRule { Amount = amount, Bonus = bonus });
                }
            }

            return rules.OrderBy(r => r.Amount).ToList();
        }

        private List<TurnoverTierRule> ParseTierRules(string value)
        {
            // 格式: 10000=0.5|50000=1|100000=1.5
            var rules = new List<TurnoverTierRule>();
            var parts = value.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in parts)
            {
                var idx = part.IndexOf('=');
                if (idx <= 0) continue;

                if (decimal.TryParse(part.Substring(0, idx).Trim(), out var turnover) &&
                    decimal.TryParse(part.Substring(idx + 1).Trim(), out var percent))
                {
                    rules.Add(new TurnoverTierRule { MinTurnover = turnover, Percent = percent });
                }
            }

            return rules.OrderByDescending(r => r.MinTurnover).ToList();
        }

        private void SaveConfigToFile(BonusConfig config)
        {
            try
            {
                Directory.CreateDirectory(DataService.Instance.DatabaseDir);
                var sb = new StringBuilder();
                sb.AppendLine("# 夜宵/流水返点配置 - 自动生成");
                sb.AppendLine($"CalculationMethod={config.CalculationMethod}");
                sb.AppendLine();

                sb.AppendLine("[夜宵设置]");
                sb.AppendLine($"Enabled={config.NightSnack.Enabled}");
                sb.AppendLine($"NotifyInGroup={config.NightSnack.NotifyInGroup}");
                sb.AppendLine($"Rules={string.Join("|", config.NightSnack.Rules.Select(r => $"{r.MinTurnover}-{r.MaxTurnover}把数{r.MinBets}送{r.Bonus}"))}");
                sb.AppendLine($"WinRules={string.Join("|", config.NightSnack.WinRules.Select(r => $"{r.Amount}={r.Bonus}"))}");
                sb.AppendLine($"LoseRules={string.Join("|", config.NightSnack.LoseRules.Select(r => $"{r.Amount}={r.Bonus}"))}");
                sb.AppendLine();

                sb.AppendLine("[流水返点]");
                sb.AppendLine($"Enabled={config.TurnoverRebate.Enabled}");
                sb.AppendLine($"DefaultPercent={config.TurnoverRebate.DefaultPercent.ToString(CultureInfo.InvariantCulture)}");
                sb.AppendLine($"DefaultMinBets={config.TurnoverRebate.DefaultMinBets}");
                sb.AppendLine($"Command={config.TurnoverRebate.Command}");
                sb.AppendLine($"DepositReason={config.TurnoverRebate.DepositReason}");
                sb.AppendLine($"HasRebateReply={config.TurnoverRebate.HasRebateReply}");
                sb.AppendLine($"NoRebateReply={config.TurnoverRebate.NoRebateReply}");
                sb.AppendLine($"NotEnoughBetsReply={config.TurnoverRebate.NotEnoughBetsReply}");
                sb.AppendLine($"TierRules={string.Join("|", config.TurnoverRebate.TierRules.Select(r => $"{r.MinTurnover}={r.Percent}"))}");

                File.WriteAllText(ConfigPath, sb.ToString(), Encoding.UTF8);
            }
            catch { }
        }

        #endregion

        #region 夜宵计算

        /// <summary>
        /// 计算夜宵奖励 (按把数)
        /// </summary>
        public NightSnackResult CalculateNightSnack(string playerId, string playerNick)
        {
            var config = GetConfig();
            if (!config.NightSnack.Enabled)
            {
                return new NightSnackResult { Success = false, Message = "夜宵功能未开启" };
            }

            // 获取今日统计
            var stats = GetBonusPlayerStats(playerId);

            // 查找匹配的规则
            foreach (var rule in config.NightSnack.Rules.OrderByDescending(r => r.MinTurnover))
            {
                if (stats.TotalTurnover >= rule.MinTurnover && 
                    stats.TotalTurnover <= rule.MaxTurnover &&
                    stats.TotalBets >= rule.MinBets)
                {
                    // 发放奖励
                    ScoreService.Instance.Deposit(playerId, rule.Bonus, "夜宵奖励", "系统");

                    Log($"[夜宵] {playerNick} 流水{stats.TotalTurnover:F2}, {stats.TotalBets}把, 奖励{rule.Bonus:F2}");
                    OnBonusGiven?.Invoke(playerId, playerNick, rule.Bonus, "夜宵");

                    return new NightSnackResult
                    {
                        Success = true,
                        Bonus = rule.Bonus,
                        TotalTurnover = stats.TotalTurnover,
                        TotalBets = stats.TotalBets,
                        Message = $"夜宵奖励 {rule.Bonus:F2}"
                    };
                }
            }

            return new NightSnackResult
            {
                Success = false,
                TotalTurnover = stats.TotalTurnover,
                TotalBets = stats.TotalBets,
                Message = "未达到夜宵条件"
            };
        }

        /// <summary>
        /// 计算夜宵奖励 (按输赢)
        /// </summary>
        public NightSnackResult CalculateNightSnackByWinLose(string playerId, string playerNick)
        {
            var config = GetConfig();
            if (!config.NightSnack.Enabled)
            {
                return new NightSnackResult { Success = false, Message = "夜宵功能未开启" };
            }

            var stats = GetBonusPlayerStats(playerId);

            // 赢钱规则
            if (stats.NetProfit > 0)
            {
                foreach (var rule in config.NightSnack.WinRules.OrderByDescending(r => r.Amount))
                {
                    if (stats.NetProfit >= rule.Amount)
                    {
                        ScoreService.Instance.Deposit(playerId, rule.Bonus, "夜宵奖励(赢)", "系统");
                        Log($"[夜宵] {playerNick} 今日赢{stats.NetProfit:F2}, 奖励{rule.Bonus:F2}");
                        OnBonusGiven?.Invoke(playerId, playerNick, rule.Bonus, "夜宵(赢)");

                        return new NightSnackResult
                        {
                            Success = true,
                            Bonus = rule.Bonus,
                            NetProfit = stats.NetProfit,
                            Message = $"夜宵奖励(赢) {rule.Bonus:F2}"
                        };
                    }
                }
            }
            // 输钱规则
            else if (stats.NetProfit < 0)
            {
                var loss = Math.Abs(stats.NetProfit);
                foreach (var rule in config.NightSnack.LoseRules.OrderByDescending(r => r.Amount))
                {
                    if (loss >= rule.Amount)
                    {
                        ScoreService.Instance.Deposit(playerId, rule.Bonus, "夜宵奖励(输)", "系统");
                        Log($"[夜宵] {playerNick} 今日输{loss:F2}, 奖励{rule.Bonus:F2}");
                        OnBonusGiven?.Invoke(playerId, playerNick, rule.Bonus, "夜宵(输)");

                        return new NightSnackResult
                        {
                            Success = true,
                            Bonus = rule.Bonus,
                            NetProfit = stats.NetProfit,
                            Message = $"夜宵奖励(输) {rule.Bonus:F2}"
                        };
                    }
                }
            }

            return new NightSnackResult
            {
                Success = false,
                NetProfit = stats.NetProfit,
                Message = "未达到夜宵条件"
            };
        }

        #endregion

        #region 流水返点

        /// <summary>
        /// 计算流水返点
        /// </summary>
        public TurnoverRebateResult CalculateTurnoverRebate(string playerId, string playerNick)
        {
            var config = GetConfig();
            if (!config.TurnoverRebate.Enabled)
            {
                return new TurnoverRebateResult { Success = false, Message = "流水返点功能未开启" };
            }

            var stats = GetBonusPlayerStats(playerId);

            // 检查把数要求
            if (stats.TotalBets < config.TurnoverRebate.DefaultMinBets)
            {
                var reply = config.TurnoverRebate.NotEnoughBetsReply
                    .Replace("[把数]", config.TurnoverRebate.DefaultMinBets.ToString());

                return new TurnoverRebateResult
                {
                    Success = false,
                    TotalBets = stats.TotalBets,
                    MinBetsRequired = config.TurnoverRebate.DefaultMinBets,
                    Message = reply
                };
            }

            // 计算返点比例
            var percent = config.TurnoverRebate.DefaultPercent;
            foreach (var tier in config.TurnoverRebate.TierRules)
            {
                if (stats.TotalTurnover >= tier.MinTurnover)
                {
                    percent = tier.Percent;
                    break;
                }
            }

            // 计算返点金额
            var rebateAmount = stats.TotalTurnover * percent / 100m;
            if (rebateAmount <= 0)
            {
                var reply = config.TurnoverRebate.NoRebateReply;
                return new TurnoverRebateResult
                {
                    Success = false,
                    TotalTurnover = stats.TotalTurnover,
                    Message = reply
                };
            }

            // 发放返点
            ScoreService.Instance.Deposit(playerId, rebateAmount, config.TurnoverRebate.DepositReason, "系统");

            Log($"[流水返点] {playerNick} 流水{stats.TotalTurnover:F2}, 返点{percent}%, 金额{rebateAmount:F2}");
            OnBonusGiven?.Invoke(playerId, playerNick, rebateAmount, "流水返点");

            var successReply = config.TurnoverRebate.HasRebateReply
                .Replace("[分数]", rebateAmount.ToString("F2"))
                .Replace("[余粮]", ScoreService.Instance.GetBalance(playerId).ToString("F2"));

            return new TurnoverRebateResult
            {
                Success = true,
                TotalTurnover = stats.TotalTurnover,
                TotalBets = stats.TotalBets,
                Percent = percent,
                RebateAmount = rebateAmount,
                Message = successReply
            };
        }

        /// <summary>
        /// 检查消息是否为返点命令
        /// </summary>
        public bool IsRebateCommand(string message)
        {
            var config = GetConfig();
            if (string.IsNullOrEmpty(config.TurnoverRebate.Command)) return false;

            return message.Trim() == config.TurnoverRebate.Command ||
                   message.Trim() == "反水" ||
                   message.Trim() == "返水" ||
                   message.Trim() == "回水";
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 获取玩家每日统计
        /// </summary>
        private BonusPlayerStats GetBonusPlayerStats(string playerId)
        {
            var stats = new BonusPlayerStats();

            try
            {
                var today = DateTime.Today;

                // 从ScoreService获取今日交易记录
                var transactions = ScoreService.Instance.GetTransactions(playerId, today, today.AddDays(1));

                // 计算总流水 (下注总额)
                stats.TotalTurnover = transactions
                    .Where(t => t.Type == ScoreTransactionType.Bet)
                    .Sum(t => Math.Abs(t.Amount));

                // 计算把数
                stats.TotalBets = transactions.Count(t => t.Type == ScoreTransactionType.Bet);

                // 计算净盈亏
                var winnings = transactions
                    .Where(t => t.Type == ScoreTransactionType.Win)
                    .Sum(t => t.Amount);

                var bets = transactions
                    .Where(t => t.Type == ScoreTransactionType.Bet)
                    .Sum(t => Math.Abs(t.Amount));

                stats.NetProfit = winnings - bets;
            }
            catch (Exception ex)
            {
                Log($"[返点] 获取玩家统计异常: {ex.Message}");
            }

            return stats;
        }

        #endregion

        private void Log(string message)
        {
            OnLog?.Invoke(message);
            Logger.Info(message);
        }
    }

    #region 返点配置和模型

    /// <summary>
    /// 返点计算方式
    /// </summary>
    public enum BonusCalculationMethod
    {
        /// <summary>按把数</summary>
        ByBets = 0,
        /// <summary>按流水</summary>
        ByTurnover = 1,
        /// <summary>按输赢</summary>
        ByWinLose = 2
    }

    /// <summary>
    /// 返点总配置
    /// </summary>
    public class BonusConfig
    {
        public BonusCalculationMethod CalculationMethod { get; set; } = BonusCalculationMethod.ByBets;
        public NightSnackConfig NightSnack { get; set; } = new NightSnackConfig();
        public TurnoverRebateConfig TurnoverRebate { get; set; } = new TurnoverRebateConfig();

        public static BonusConfig CreateDefault()
        {
            return new BonusConfig
            {
                CalculationMethod = BonusCalculationMethod.ByBets,
                NightSnack = new NightSnackConfig
                {
                    Enabled = true,
                    NotifyInGroup = true,
                    Rules = new List<NightSnackRule>
                    {
                        new NightSnackRule { MinTurnover = 30001, MaxTurnover = 50000, MinBets = 30, Bonus = 2888 },
                        new NightSnackRule { MinTurnover = 50001, MaxTurnover = 100000, MinBets = 50, Bonus = 5888 },
                        new NightSnackRule { MinTurnover = 100001, MaxTurnover = 500000, MinBets = 100, Bonus = 8888 }
                    },
                    WinRules = new List<WinLoseRule>
                    {
                        new WinLoseRule { Amount = 280000, Bonus = 888 },
                        new WinLoseRule { Amount = 500000, Bonus = 1888 }
                    },
                    LoseRules = new List<WinLoseRule>
                    {
                        new WinLoseRule { Amount = 280000, Bonus = 888 },
                        new WinLoseRule { Amount = 500000, Bonus = 1888 }
                    }
                },
                TurnoverRebate = new TurnoverRebateConfig
                {
                    Enabled = true,
                    DefaultPercent = 1m,
                    DefaultMinBets = 1,
                    Command = "反水",
                    DepositReason = "反水",
                    HasRebateReply = "[艾特]([旺旺])[换行]本次回水[分数],余粮：[余粮]",
                    NoRebateReply = "[艾特]([旺旺])[换行]本次回水0",
                    NotEnoughBetsReply = "[艾特]([旺旺])[换行]把数不足[把数]把",
                    TierRules = new List<TurnoverTierRule>
                    {
                        new TurnoverTierRule { MinTurnover = 100000, Percent = 1.5m },
                        new TurnoverTierRule { MinTurnover = 50000, Percent = 1.0m },
                        new TurnoverTierRule { MinTurnover = 10000, Percent = 0.5m }
                    }
                }
            };
        }
    }

    /// <summary>
    /// 夜宵配置
    /// </summary>
    public class NightSnackConfig
    {
        public bool Enabled { get; set; } = true;
        public bool NotifyInGroup { get; set; } = true;
        public List<NightSnackRule> Rules { get; set; } = new List<NightSnackRule>();
        public List<WinLoseRule> WinRules { get; set; } = new List<WinLoseRule>();
        public List<WinLoseRule> LoseRules { get; set; } = new List<WinLoseRule>();
    }

    /// <summary>
    /// 夜宵规则
    /// </summary>
    public class NightSnackRule
    {
        public decimal MinTurnover { get; set; }
        public decimal MaxTurnover { get; set; }
        public int MinBets { get; set; }
        public decimal Bonus { get; set; }
    }

    /// <summary>
    /// 输赢规则
    /// </summary>
    public class WinLoseRule
    {
        public decimal Amount { get; set; }
        public decimal Bonus { get; set; }
    }

    /// <summary>
    /// 流水返点配置
    /// </summary>
    public class TurnoverRebateConfig
    {
        public bool Enabled { get; set; } = true;
        public decimal DefaultPercent { get; set; } = 1m;
        public int DefaultMinBets { get; set; } = 1;
        public string Command { get; set; } = "反水";
        public string DepositReason { get; set; } = "反水";
        public string HasRebateReply { get; set; }
        public string NoRebateReply { get; set; }
        public string NotEnoughBetsReply { get; set; }
        public List<TurnoverTierRule> TierRules { get; set; } = new List<TurnoverTierRule>();
    }

    /// <summary>
    /// 流水阶梯规则
    /// </summary>
    public class TurnoverTierRule
    {
        public decimal MinTurnover { get; set; }
        public decimal Percent { get; set; }
    }

    /// <summary>
    /// 夜宵结果
    /// </summary>
    public class NightSnackResult
    {
        public bool Success { get; set; }
        public decimal Bonus { get; set; }
        public decimal TotalTurnover { get; set; }
        public int TotalBets { get; set; }
        public decimal NetProfit { get; set; }
        public string Message { get; set; }
    }

    /// <summary>
    /// 流水返点结果
    /// </summary>
    public class TurnoverRebateResult
    {
        public bool Success { get; set; }
        public decimal TotalTurnover { get; set; }
        public int TotalBets { get; set; }
        public int MinBetsRequired { get; set; }
        public decimal Percent { get; set; }
        public decimal RebateAmount { get; set; }
        public string Message { get; set; }
    }

    /// <summary>
    /// 奖金玩家每日统计 (用于回水计算)
    /// </summary>
    public class BonusPlayerStats
    {
        public decimal TotalTurnover { get; set; }
        public int TotalBets { get; set; }
        public decimal NetProfit { get; set; }
    }

    #endregion
}
