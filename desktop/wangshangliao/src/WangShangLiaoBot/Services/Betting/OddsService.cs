using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using WangShangLiaoBot.Models.Betting;

namespace WangShangLiaoBot.Services.Betting
{
    /// <summary>
    /// 赔率服务 - 提供完整的赔率计算和管理功能
    /// 基于招财狗(ZCG)软件的赔率系统实现
    /// </summary>
    public sealed class OddsService
    {
        private static OddsService _instance;
        public static OddsService Instance => _instance ?? (_instance = new OddsService());

        private FullOddsConfig _config;
        private readonly object _lock = new object();

        private OddsService()
        {
            LoadConfig();
        }

        private string ConfigPath => Path.Combine(DataService.Instance.DatabaseDir, "odds-full.ini");

        #region 配置加载/保存

        /// <summary>
        /// 获取当前赔率配置
        /// </summary>
        public FullOddsConfig GetConfig()
        {
            lock (_lock)
            {
                return _config ?? FullOddsConfig.CreateDefault();
            }
        }

        /// <summary>
        /// 保存赔率配置
        /// </summary>
        public void SaveConfig(FullOddsConfig config)
        {
            if (config == null) return;
            lock (_lock)
            {
                _config = config;
                SaveToFile(config);
            }
        }

        private void LoadConfig()
        {
            try
            {
                if (!File.Exists(ConfigPath))
                {
                    _config = FullOddsConfig.CreateDefault();
                    return;
                }

                var config = new FullOddsConfig();
                var lines = File.ReadAllLines(ConfigPath, Encoding.UTF8);

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;
                    var idx = line.IndexOf('=');
                    if (idx <= 0) continue;

                    var key = line.Substring(0, idx).Trim();
                    var value = line.Substring(idx + 1).Trim();

                    ParseConfigLine(config, key, value);
                }

                _config = config;
            }
            catch
            {
                _config = FullOddsConfig.CreateDefault();
            }
        }

        private void ParseConfigLine(FullOddsConfig config, string key, string value)
        {
            if (!decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
                decimal.TryParse(value, out d);

            switch (key)
            {
                // 基础赔率
                case "SingleBetOdds": config.SingleBetOdds = d; break;
                case "CombinationOdds": config.CombinationOdds = d; break;
                case "BigOddSmallEvenOdds": config.BigOddSmallEvenOdds = d; break;
                case "BigEvenSmallOddOdds": config.BigEvenSmallOddOdds = d; break;
                case "ExtremeOdds": config.ExtremeOdds = d; break;

                // 特殊玩法
                case "PairOdds": config.PairOdds = d; break;
                case "StraightOdds": config.StraightOdds = d; break;
                case "HalfStraightOdds": config.HalfStraightOdds = d; break;
                case "LeopardOdds": config.LeopardOdds = d; break;
                case "MixedOdds": config.MixedOdds = d; break;

                // 龙虎
                case "DragonTigerOdds": config.DragonTigerOdds = d; break;
                case "DragonTigerTieOdds": config.DragonTigerTieOdds = d; break;

                // 尾球
                case "TailSingleOdds": config.TailSingleOdds = d; break;
                case "TailCombinationOdds": config.TailCombinationOdds = d; break;
                case "TailDigitOdds": config.TailDigitOdds = d; break;

                // 边中
                case "BigEdgeOdds": config.BigEdgeOdds = d; break;
                case "SmallEdgeOdds": config.SmallEdgeOdds = d; break;
                case "EdgeOdds": config.EdgeOdds = d; break;
                case "MiddleOdds": config.MiddleOdds = d; break;

                // 二七
                case "TwoSevenSingleOdds": config.TwoSevenSingleOdds = d; break;
                case "TwoSevenCombinationOdds": config.TwoSevenCombinationOdds = d; break;

                // 数字赔率
                case "DefaultDigitOdds": config.DefaultDigitOdds = d; break;

                // 限额
                case "SingleMinBet": config.SingleMinBet = d; break;
                case "SingleMaxBet": config.SingleMaxBet = d; break;
                case "CombinationMinBet": config.CombinationMinBet = d; break;
                case "CombinationMaxBet": config.CombinationMaxBet = d; break;
                case "DigitMinBet": config.DigitMinBet = d; break;
                case "DigitMaxBet": config.DigitMaxBet = d; break;
                case "DragonTigerMinBet": config.DragonTigerMinBet = d; break;
                case "DragonTigerMaxBet": config.DragonTigerMaxBet = d; break;
                case "PairMinBet": config.PairMinBet = d; break;
                case "PairMaxBet": config.PairMaxBet = d; break;
                case "StraightMinBet": config.StraightMinBet = d; break;
                case "StraightMaxBet": config.StraightMaxBet = d; break;
                case "LeopardMinBet": config.LeopardMinBet = d; break;
                case "LeopardMaxBet": config.LeopardMaxBet = d; break;
                case "TotalMaxBet": config.TotalMaxBet = d; break;

                // 极值
                case "ExtremeHighStart": config.ExtremeHighStart = (int)d; break;
                case "ExtremeHighEnd": config.ExtremeHighEnd = (int)d; break;
                case "ExtremeLowStart": config.ExtremeLowStart = (int)d; break;
                case "ExtremeLowEnd": config.ExtremeLowEnd = (int)d; break;

                // 龙虎号码
                case "DragonNumbers": config.DragonNumbers = value; break;
                case "TigerNumbers": config.TigerNumbers = value; break;
                case "LeopardDTNumbers": config.LeopardDTNumbers = value; break;

                default:
                    // 数字赔率 (Digit_0, Digit_1, ..., Digit_27)
                    if (key.StartsWith("Digit_") && int.TryParse(key.Substring(6), out var digit))
                    {
                        config.DigitOdds[digit] = d;
                    }
                    break;
            }
        }

        private void SaveToFile(FullOddsConfig config)
        {
            try
            {
                Directory.CreateDirectory(DataService.Instance.DatabaseDir);
                var sb = new StringBuilder();
                sb.AppendLine("# 完整赔率配置 - 自动生成");
                sb.AppendLine("# Full Odds Configuration");
                sb.AppendLine();

                sb.AppendLine("# === 基础玩法赔率 ===");
                sb.AppendLine($"SingleBetOdds={config.SingleBetOdds.ToString(CultureInfo.InvariantCulture)}");
                sb.AppendLine($"CombinationOdds={config.CombinationOdds.ToString(CultureInfo.InvariantCulture)}");
                sb.AppendLine($"BigOddSmallEvenOdds={config.BigOddSmallEvenOdds.ToString(CultureInfo.InvariantCulture)}");
                sb.AppendLine($"BigEvenSmallOddOdds={config.BigEvenSmallOddOdds.ToString(CultureInfo.InvariantCulture)}");
                sb.AppendLine($"ExtremeOdds={config.ExtremeOdds.ToString(CultureInfo.InvariantCulture)}");
                sb.AppendLine();

                sb.AppendLine("# === 特殊玩法赔率 ===");
                sb.AppendLine($"PairOdds={config.PairOdds.ToString(CultureInfo.InvariantCulture)}");
                sb.AppendLine($"StraightOdds={config.StraightOdds.ToString(CultureInfo.InvariantCulture)}");
                sb.AppendLine($"HalfStraightOdds={config.HalfStraightOdds.ToString(CultureInfo.InvariantCulture)}");
                sb.AppendLine($"LeopardOdds={config.LeopardOdds.ToString(CultureInfo.InvariantCulture)}");
                sb.AppendLine($"MixedOdds={config.MixedOdds.ToString(CultureInfo.InvariantCulture)}");
                sb.AppendLine();

                sb.AppendLine("# === 龙虎玩法赔率 ===");
                sb.AppendLine($"DragonTigerOdds={config.DragonTigerOdds.ToString(CultureInfo.InvariantCulture)}");
                sb.AppendLine($"DragonTigerTieOdds={config.DragonTigerTieOdds.ToString(CultureInfo.InvariantCulture)}");
                sb.AppendLine();

                sb.AppendLine("# === 尾球玩法赔率 ===");
                sb.AppendLine($"TailSingleOdds={config.TailSingleOdds.ToString(CultureInfo.InvariantCulture)}");
                sb.AppendLine($"TailCombinationOdds={config.TailCombinationOdds.ToString(CultureInfo.InvariantCulture)}");
                sb.AppendLine($"TailDigitOdds={config.TailDigitOdds.ToString(CultureInfo.InvariantCulture)}");
                sb.AppendLine();

                sb.AppendLine("# === 边中玩法赔率 ===");
                sb.AppendLine($"BigEdgeOdds={config.BigEdgeOdds.ToString(CultureInfo.InvariantCulture)}");
                sb.AppendLine($"SmallEdgeOdds={config.SmallEdgeOdds.ToString(CultureInfo.InvariantCulture)}");
                sb.AppendLine($"EdgeOdds={config.EdgeOdds.ToString(CultureInfo.InvariantCulture)}");
                sb.AppendLine($"MiddleOdds={config.MiddleOdds.ToString(CultureInfo.InvariantCulture)}");
                sb.AppendLine();

                sb.AppendLine("# === 二七玩法赔率 ===");
                sb.AppendLine($"TwoSevenSingleOdds={config.TwoSevenSingleOdds.ToString(CultureInfo.InvariantCulture)}");
                sb.AppendLine($"TwoSevenCombinationOdds={config.TwoSevenCombinationOdds.ToString(CultureInfo.InvariantCulture)}");
                sb.AppendLine();

                sb.AppendLine("# === 数字赔率 (0-27) ===");
                sb.AppendLine($"DefaultDigitOdds={config.DefaultDigitOdds.ToString(CultureInfo.InvariantCulture)}");
                for (int i = 0; i <= 27; i++)
                {
                    var odds = config.GetDigitOdds(i);
                    sb.AppendLine($"Digit_{i}={odds.ToString(CultureInfo.InvariantCulture)}");
                }
                sb.AppendLine();

                sb.AppendLine("# === 下注限额 ===");
                sb.AppendLine($"SingleMinBet={config.SingleMinBet.ToString(CultureInfo.InvariantCulture)}");
                sb.AppendLine($"SingleMaxBet={config.SingleMaxBet.ToString(CultureInfo.InvariantCulture)}");
                sb.AppendLine($"CombinationMinBet={config.CombinationMinBet.ToString(CultureInfo.InvariantCulture)}");
                sb.AppendLine($"CombinationMaxBet={config.CombinationMaxBet.ToString(CultureInfo.InvariantCulture)}");
                sb.AppendLine($"DigitMinBet={config.DigitMinBet.ToString(CultureInfo.InvariantCulture)}");
                sb.AppendLine($"DigitMaxBet={config.DigitMaxBet.ToString(CultureInfo.InvariantCulture)}");
                sb.AppendLine($"DragonTigerMinBet={config.DragonTigerMinBet.ToString(CultureInfo.InvariantCulture)}");
                sb.AppendLine($"DragonTigerMaxBet={config.DragonTigerMaxBet.ToString(CultureInfo.InvariantCulture)}");
                sb.AppendLine($"PairMinBet={config.PairMinBet.ToString(CultureInfo.InvariantCulture)}");
                sb.AppendLine($"PairMaxBet={config.PairMaxBet.ToString(CultureInfo.InvariantCulture)}");
                sb.AppendLine($"StraightMinBet={config.StraightMinBet.ToString(CultureInfo.InvariantCulture)}");
                sb.AppendLine($"StraightMaxBet={config.StraightMaxBet.ToString(CultureInfo.InvariantCulture)}");
                sb.AppendLine($"LeopardMinBet={config.LeopardMinBet.ToString(CultureInfo.InvariantCulture)}");
                sb.AppendLine($"LeopardMaxBet={config.LeopardMaxBet.ToString(CultureInfo.InvariantCulture)}");
                sb.AppendLine($"TotalMaxBet={config.TotalMaxBet.ToString(CultureInfo.InvariantCulture)}");
                sb.AppendLine();

                sb.AppendLine("# === 极值设置 ===");
                sb.AppendLine($"ExtremeHighStart={config.ExtremeHighStart}");
                sb.AppendLine($"ExtremeHighEnd={config.ExtremeHighEnd}");
                sb.AppendLine($"ExtremeLowStart={config.ExtremeLowStart}");
                sb.AppendLine($"ExtremeLowEnd={config.ExtremeLowEnd}");
                sb.AppendLine();

                sb.AppendLine("# === 龙虎豹号码 ===");
                sb.AppendLine($"DragonNumbers={config.DragonNumbers}");
                sb.AppendLine($"TigerNumbers={config.TigerNumbers}");
                sb.AppendLine($"LeopardDTNumbers={config.LeopardDTNumbers}");

                File.WriteAllText(ConfigPath, sb.ToString(), Encoding.UTF8);
            }
            catch
            {
                // ignore
            }
        }

        #endregion

        #region 赔率查询

        /// <summary>
        /// 根据下注类型获取赔率
        /// </summary>
        public decimal GetOdds(BetKind kind, string code = null)
        {
            var config = GetConfig();

            switch (kind)
            {
                case BetKind.BigSmall:
                case BetKind.OddEven:
                    return config.SingleBetOdds;

                case BetKind.Dxds:
                    // 大单/大双/小单/小双
                    if (code == "DD" || code == "DS" || code == "XD" || code == "XS")
                        return config.CombinationOdds;
                    return config.SingleBetOdds;

                case BetKind.Pair:
                    return config.PairOdds;

                case BetKind.Straight:
                    return config.StraightOdds;

                case BetKind.HalfStraight:
                    return config.HalfStraightOdds;

                case BetKind.Leopard:
                    return config.LeopardOdds;

                case BetKind.Mixed:
                    return config.MixedOdds;

                case BetKind.Extreme:
                    return config.ExtremeOdds;

                case BetKind.DragonTiger:
                    if (code == "HE" || code == "和")
                        return config.DragonTigerTieOdds;
                    return config.DragonTigerOdds;

                case BetKind.Digit:
                    if (int.TryParse(code, out var digit))
                        return config.GetDigitOdds(digit);
                    return config.DefaultDigitOdds;

                case BetKind.TailSingle:
                    return config.TailSingleOdds;

                case BetKind.TailCombination:
                    return config.TailCombinationOdds;

                case BetKind.TailDigit:
                    return config.TailDigitOdds;

                case BetKind.Edge:
                    if (code == "DB") return config.BigEdgeOdds;
                    if (code == "XB") return config.SmallEdgeOdds;
                    return config.EdgeOdds;

                case BetKind.Middle:
                    return config.MiddleOdds;

                case BetKind.Sum:
                    return config.DragonTigerTieOdds;

                default:
                    return 0m;
            }
        }

        /// <summary>
        /// 验证下注金额是否在限额范围内
        /// </summary>
        public (bool isValid, string errorMessage) ValidateBetAmount(BetKind kind, decimal amount)
        {
            var config = GetConfig();
            decimal min, max;

            switch (kind)
            {
                case BetKind.BigSmall:
                case BetKind.OddEven:
                case BetKind.Dxds:
                    min = config.SingleMinBet;
                    max = config.SingleMaxBet;
                    break;

                case BetKind.Combination:
                    min = config.CombinationMinBet;
                    max = config.CombinationMaxBet;
                    break;

                case BetKind.Digit:
                    min = config.DigitMinBet;
                    max = config.DigitMaxBet;
                    break;

                case BetKind.DragonTiger:
                    min = config.DragonTigerMinBet;
                    max = config.DragonTigerMaxBet;
                    break;

                case BetKind.Pair:
                    min = config.PairMinBet;
                    max = config.PairMaxBet;
                    break;

                case BetKind.Straight:
                    min = config.StraightMinBet;
                    max = config.StraightMaxBet;
                    break;

                case BetKind.Leopard:
                    min = config.LeopardMinBet;
                    max = config.LeopardMaxBet;
                    break;

                default:
                    min = config.SingleMinBet;
                    max = config.SingleMaxBet;
                    break;
            }

            if (amount < min)
                return (false, $"下注金额不能低于{min}");

            if (amount > max)
                return (false, $"下注金额不能高于{max}");

            return (true, null);
        }

        /// <summary>
        /// 验证总下注金额是否超过限额
        /// </summary>
        public (bool isValid, string errorMessage) ValidateTotalBet(decimal totalAmount)
        {
            var config = GetConfig();
            if (totalAmount > config.TotalMaxBet)
                return (false, $"总下注金额不能超过{config.TotalMaxBet}");
            return (true, null);
        }

        #endregion

        #region 开奖结果计算

        /// <summary>
        /// 计算单个下注项的盈亏
        /// </summary>
        /// <param name="item">下注项</param>
        /// <param name="lotteryResult">开奖结果 (如 "8+5+6=19")</param>
        /// <returns>盈亏金额 (正数为盈利，负数为亏损)</returns>
        public decimal CalculateProfit(BetItem item, string lotteryResult)
        {
            // 解析开奖结果
            if (!TryParseLotteryResult(lotteryResult, out var d1, out var d2, out var d3, out var sum))
                return -item.Amount; // 无法解析，视为输

            var odds = GetOdds(item.Kind, item.Code);
            if (odds <= 0) return -item.Amount;

            bool isWin = CheckIsWin(item.Kind, item.Code, d1, d2, d3, sum);

            return isWin ? item.Amount * (odds - 1) : -item.Amount;
        }

        /// <summary>
        /// 解析开奖结果
        /// </summary>
        public bool TryParseLotteryResult(string result, out int d1, out int d2, out int d3, out int sum)
        {
            d1 = d2 = d3 = sum = 0;

            if (string.IsNullOrEmpty(result)) return false;

            // 格式: "8+5+6=19" 或 "8 5 6 19"
            var match = System.Text.RegularExpressions.Regex.Match(result, @"(\d+)\s*[+\s]\s*(\d+)\s*[+\s]\s*(\d+)\s*[=\s]\s*(\d+)");
            if (match.Success)
            {
                d1 = int.Parse(match.Groups[1].Value);
                d2 = int.Parse(match.Groups[2].Value);
                d3 = int.Parse(match.Groups[3].Value);
                sum = int.Parse(match.Groups[4].Value);
                return true;
            }

            // 简单格式: 只有和值
            if (int.TryParse(result.Trim(), out sum))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 判断是否中奖
        /// </summary>
        private bool CheckIsWin(BetKind kind, string code, int d1, int d2, int d3, int sum)
        {
            var config = GetConfig();

            switch (kind)
            {
                case BetKind.BigSmall:
                    if (code == "D" || code == "大") return sum >= 14;
                    if (code == "X" || code == "小") return sum <= 13;
                    return false;

                case BetKind.OddEven:
                    if (code == "单" || code == "D") return sum % 2 == 1;
                    if (code == "双" || code == "S") return sum % 2 == 0;
                    return false;

                case BetKind.Dxds:
                    bool isBig = sum >= 14;
                    bool isOdd = sum % 2 == 1;
                    switch (code)
                    {
                        case "DD": return isBig && isOdd;   // 大单
                        case "DS": return isBig && !isOdd;  // 大双
                        case "XD": return !isBig && isOdd;  // 小单
                        case "XS": return !isBig && !isOdd; // 小双
                    }
                    return false;

                case BetKind.Digit:
                    if (int.TryParse(code, out var targetDigit))
                        return sum == targetDigit;
                    return false;

                case BetKind.Extreme:
                    if (code == "JD" || code == "极大")
                        return sum >= config.ExtremeHighStart && sum <= config.ExtremeHighEnd;
                    if (code == "JX" || code == "极小")
                        return sum >= config.ExtremeLowStart && sum <= config.ExtremeLowEnd;
                    return false;

                case BetKind.Pair:
                    return (d1 == d2) || (d2 == d3) || (d1 == d3);

                case BetKind.Straight:
                    var sorted = new[] { d1, d2, d3 }.OrderBy(x => x).ToArray();
                    return (sorted[2] - sorted[1] == 1) && (sorted[1] - sorted[0] == 1);

                case BetKind.Leopard:
                    return d1 == d2 && d2 == d3;

                case BetKind.DragonTiger:
                    var dragonNums = ParseNumberList(config.DragonNumbers);
                    var tigerNums = ParseNumberList(config.TigerNumbers);
                    if (code == "L" || code == "龙") return dragonNums.Contains(sum);
                    if (code == "H" || code == "虎") return tigerNums.Contains(sum);
                    return false;

                case BetKind.HalfStraight:
                    var sortedHs = new[] { d1, d2, d3 }.OrderBy(x => x).ToArray();
                    int diff1 = sortedHs[1] - sortedHs[0];
                    int diff2 = sortedHs[2] - sortedHs[1];
                    return (diff1 == 1 || diff2 == 1) && !(diff1 == 1 && diff2 == 1);

                case BetKind.Mixed:
                    // 杂六：三个数字都不相同且不是顺子
                    if (d1 == d2 || d2 == d3 || d1 == d3) return false;
                    var sortedMix = new[] { d1, d2, d3 }.OrderBy(x => x).ToArray();
                    if ((sortedMix[2] - sortedMix[1] == 1) && (sortedMix[1] - sortedMix[0] == 1)) return false;
                    return true;

                default:
                    return false;
            }
        }

        private HashSet<int> ParseNumberList(string numbers)
        {
            var result = new HashSet<int>();
            if (string.IsNullOrEmpty(numbers)) return result;

            foreach (var num in numbers.Split(','))
            {
                if (int.TryParse(num.Trim(), out var n))
                    result.Add(n);
            }
            return result;
        }

        #endregion
    }
}
