using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using WangShangLiaoBot.Models.Betting;

namespace WangShangLiaoBot.Services.Betting
{
    /// <summary>
    /// 二七玩法服务 - 基于招财狗(ZCG)的二七特殊玩法
    /// 开奖号码为2或7时触发特殊赔率
    /// </summary>
    public sealed class TwoSevenService
    {
        private static TwoSevenService _instance;
        public static TwoSevenService Instance => _instance ?? (_instance = new TwoSevenService());

        private TwoSevenConfig _config;
        private readonly object _lock = new object();

        // 事件
        public event Action<string> OnLog;

        // 二七特殊号码
        private static readonly int[] TwoSevenNumbers = { 2, 7 };

        private TwoSevenService()
        {
            LoadConfig();
        }

        private string ConfigPath => Path.Combine(DataService.Instance.DatabaseDir, "two-seven-config.ini");

        #region 配置管理

        public TwoSevenConfig GetConfig()
        {
            lock (_lock)
            {
                return _config ?? TwoSevenConfig.CreateDefault();
            }
        }

        public void SaveConfig(TwoSevenConfig config)
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
                    _config = TwoSevenConfig.CreateDefault();
                    return;
                }

                var config = new TwoSevenConfig();
                var lines = File.ReadAllLines(ConfigPath, Encoding.UTF8);

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;

                    var idx = line.IndexOf('=');
                    if (idx <= 0) continue;

                    var key = line.Substring(0, idx).Trim();
                    var value = line.Substring(idx + 1).Trim();

                    switch (key)
                    {
                        case "Enabled":
                            config.Enabled = value == "true" || value == "1" || value == "真";
                            break;
                        case "SingleMaxAmount":
                            if (decimal.TryParse(value, out var sma)) config.SingleMaxAmount = sma;
                            break;
                        case "SingleOdds":
                            if (decimal.TryParse(value, out var so)) config.SingleOdds = so;
                            break;
                        case "ComboMaxAmount":
                            if (decimal.TryParse(value, out var cma)) config.ComboMaxAmount = cma;
                            break;
                        case "ComboOdds":
                            if (decimal.TryParse(value, out var co)) config.ComboOdds = co;
                            break;
                        case "CustomNumbers":
                            config.CustomNumbers = value.Split(new[] { ',', '|' }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(s => int.TryParse(s.Trim(), out var n) ? n : -1)
                                .Where(n => n >= 0 && n <= 27)
                                .ToList();
                            break;
                    }
                }

                _config = config;
            }
            catch
            {
                _config = TwoSevenConfig.CreateDefault();
            }
        }

        private void SaveConfigToFile(TwoSevenConfig config)
        {
            try
            {
                Directory.CreateDirectory(DataService.Instance.DatabaseDir);
                var sb = new StringBuilder();
                sb.AppendLine("# 二七玩法配置 - 自动生成");
                sb.AppendLine($"Enabled={config.Enabled}");
                sb.AppendLine($"SingleMaxAmount={config.SingleMaxAmount}");
                sb.AppendLine($"SingleOdds={config.SingleOdds}");
                sb.AppendLine($"ComboMaxAmount={config.ComboMaxAmount}");
                sb.AppendLine($"ComboOdds={config.ComboOdds}");
                sb.AppendLine($"CustomNumbers={string.Join(",", config.CustomNumbers)}");

                File.WriteAllText(ConfigPath, sb.ToString(), Encoding.UTF8);
            }
            catch { }
        }

        #endregion

        #region 二七玩法逻辑

        /// <summary>
        /// 检查是否为二七号码
        /// </summary>
        public bool IsTwoSevenNumber(int number)
        {
            var config = GetConfig();
            if (!config.Enabled) return false;

            if (config.CustomNumbers.Count > 0)
            {
                return config.CustomNumbers.Contains(number);
            }

            return TwoSevenNumbers.Contains(number);
        }

        /// <summary>
        /// 获取二七玩法的赔率
        /// </summary>
        public decimal GetTwoSevenOdds(BetKind kind)
        {
            var config = GetConfig();
            if (!config.Enabled) return 0;

            switch (kind)
            {
                case BetKind.BigSmall:
                case BetKind.OddEven:
                case BetKind.Dxds:
                    return config.SingleOdds;

                case BetKind.Combination:
                    return config.ComboOdds;

                default:
                    return 0;
            }
        }

        /// <summary>
        /// 获取二七玩法的限额
        /// </summary>
        public decimal GetTwoSevenLimit(BetKind kind)
        {
            var config = GetConfig();
            if (!config.Enabled) return 0;

            switch (kind)
            {
                case BetKind.BigSmall:
                case BetKind.OddEven:
                case BetKind.Dxds:
                    return config.SingleMaxAmount;

                case BetKind.Combination:
                    return config.ComboMaxAmount;

                default:
                    return 0;
            }
        }

        /// <summary>
        /// 计算二七玩法的中奖金额
        /// </summary>
        public TwoSevenSettlement CalculateWinnings(int winningNumber, List<BetRecord> bets)
        {
            var config = GetConfig();
            var result = new TwoSevenSettlement
            {
                WinningNumber = winningNumber,
                IsTwoSeven = IsTwoSevenNumber(winningNumber)
            };

            if (!config.Enabled || !result.IsTwoSeven)
            {
                return result;
            }

            foreach (var bet in bets)
            {
                // 检查是否适用二七赔率
                var twoSevenOdds = GetTwoSevenOdds(bet.Kind);
                if (twoSevenOdds <= 0) continue;

                // 检查下注是否超过二七限额
                var limit = GetTwoSevenLimit(bet.Kind);
                var effectiveAmount = Math.Min(bet.Amount, limit);

                // 判断是否中奖
                var isWin = CheckBetWin(bet, winningNumber);
                if (!isWin) continue;

                // 计算奖金 (使用二七特殊赔率)
                var winAmount = effectiveAmount * twoSevenOdds;

                result.Entries.Add(new TwoSevenEntry
                {
                    PlayerId = bet.PlayerId,
                    BetKind = bet.Kind,
                    BetAmount = bet.Amount,
                    EffectiveAmount = effectiveAmount,
                    NormalOdds = bet.Odds,
                    TwoSevenOdds = twoSevenOdds,
                    WinAmount = winAmount
                });

                result.TotalWinnings += winAmount;
            }

            if (result.Entries.Count > 0)
            {
                Log($"[二七玩法] 开奖号码{winningNumber}触发二七规则，{result.Entries.Count}人中奖，总奖金{result.TotalWinnings:F2}");
            }

            return result;
        }

        private bool CheckBetWin(BetRecord bet, int winningNumber)
        {
            var bigSmall = winningNumber >= 14 ? "大" : "小";
            var oddEven = winningNumber % 2 == 1 ? "单" : "双";
            var combo = bigSmall + oddEven;

            switch (bet.Kind)
            {
                case BetKind.BigSmall:
                    return (bet.Code == "大" && bigSmall == "大") ||
                           (bet.Code == "小" && bigSmall == "小");

                case BetKind.OddEven:
                    return (bet.Code == "单" && oddEven == "单") ||
                           (bet.Code == "双" && oddEven == "双");

                case BetKind.Dxds:
                    return bet.Code == combo ||
                           (bet.Code == bigSmall) ||
                           (bet.Code == oddEven);

                case BetKind.Combination:
                    return bet.Code == combo;

                default:
                    return false;
            }
        }

        /// <summary>
        /// 解析二七玩法下注
        /// </summary>
        public List<BetItem> ParseTwoSevenBets(string message)
        {
            var config = GetConfig();
            if (!config.Enabled) return new List<BetItem>();

            var bets = new List<BetItem>();

            // 二七单注格式: 27大100, 27小50, 27单30, 27双20
            var singlePattern = @"(?:27|二七)\s*(大|小|单|双)\s*(\d+)";
            var singleMatches = Regex.Matches(message, singlePattern, RegexOptions.IgnoreCase);

            foreach (Match match in singleMatches)
            {
                var code = match.Groups[1].Value;
                if (decimal.TryParse(match.Groups[2].Value, out var amount))
                {
                    var kind = code == "大" || code == "小" ? BetKind.BigSmall : BetKind.OddEven;
                    bets.Add(new BetItem
                    {
                        Kind = kind,
                        Code = code,
                        Amount = Math.Min(amount, config.SingleMaxAmount),
                        IsTwoSeven = true
                    });
                }
            }

            // 二七组合格式: 27大单100, 27小双50
            var comboPattern = @"(?:27|二七)\s*(大单|大双|小单|小双)\s*(\d+)";
            var comboMatches = Regex.Matches(message, comboPattern, RegexOptions.IgnoreCase);

            foreach (Match match in comboMatches)
            {
                var code = match.Groups[1].Value;
                if (decimal.TryParse(match.Groups[2].Value, out var amount))
                {
                    bets.Add(new BetItem
                    {
                        Kind = BetKind.Combination,
                        Code = code,
                        Amount = Math.Min(amount, config.ComboMaxAmount),
                        IsTwoSeven = true
                    });
                }
            }

            return bets;
        }

        /// <summary>
        /// 验证二七下注是否有效
        /// </summary>
        public TwoSevenValidation ValidateBet(BetItem bet)
        {
            var config = GetConfig();
            var result = new TwoSevenValidation { IsValid = true, Bet = bet };

            if (!config.Enabled)
            {
                result.IsValid = false;
                result.Message = "二七玩法未开启";
                return result;
            }

            // 检查限额
            var limit = GetTwoSevenLimit(bet.Kind);
            if (bet.Amount > limit)
            {
                result.AdjustedAmount = limit;
                result.Message = $"二七玩法{bet.Kind}最高限额{limit}";
            }

            return result;
        }

        #endregion

        private void Log(string message)
        {
            OnLog?.Invoke(message);
            Logger.Info(message);
        }
    }

    #region 二七玩法配置和模型

    /// <summary>
    /// 二七玩法配置
    /// </summary>
    public class TwoSevenConfig
    {
        /// <summary>是否启用</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>单注最高限额</summary>
        public decimal SingleMaxAmount { get; set; } = 49999;

        /// <summary>单注赔率</summary>
        public decimal SingleOdds { get; set; } = 1.7m;

        /// <summary>组合最高限额</summary>
        public decimal ComboMaxAmount { get; set; } = 29999;

        /// <summary>组合赔率</summary>
        public decimal ComboOdds { get; set; } = 4.9m;

        /// <summary>自定义二七号码 (默认2,7)</summary>
        public List<int> CustomNumbers { get; set; } = new List<int>();

        public static TwoSevenConfig CreateDefault()
        {
            return new TwoSevenConfig
            {
                Enabled = true,
                SingleMaxAmount = 49999,
                SingleOdds = 1.7m,
                ComboMaxAmount = 29999,
                ComboOdds = 4.9m,
                CustomNumbers = new List<int> { 2, 7 }
            };
        }
    }

    /// <summary>
    /// 二七结算结果
    /// </summary>
    public class TwoSevenSettlement
    {
        public int WinningNumber { get; set; }
        public bool IsTwoSeven { get; set; }
        public decimal TotalWinnings { get; set; }
        public List<TwoSevenEntry> Entries { get; set; } = new List<TwoSevenEntry>();
    }

    /// <summary>
    /// 二七结算条目
    /// </summary>
    public class TwoSevenEntry
    {
        public string PlayerId { get; set; }
        public BetKind BetKind { get; set; }
        public decimal BetAmount { get; set; }
        public decimal EffectiveAmount { get; set; }
        public decimal NormalOdds { get; set; }
        public decimal TwoSevenOdds { get; set; }
        public decimal WinAmount { get; set; }
    }

    /// <summary>
    /// 二七下注验证结果
    /// </summary>
    public class TwoSevenValidation
    {
        public bool IsValid { get; set; }
        public BetItem Bet { get; set; }
        public decimal? AdjustedAmount { get; set; }
        public string Message { get; set; }
    }

    #endregion
}
