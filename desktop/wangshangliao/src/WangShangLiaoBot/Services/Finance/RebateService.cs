using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace WangShangLiaoBot.Services
{
    /// <summary>
    /// 回水计算服务 - 基于招财狗(ZCG)的回水系统
    /// 支持4种回水方式：按组合比例、按下注次数、按下注流水、按输分
    /// </summary>
    public sealed class RebateService
    {
        private static RebateService _instance;
        public static RebateService Instance => _instance ?? (_instance = new RebateService());

        private RebateConfig _config;
        private readonly object _lock = new object();

        private RebateService()
        {
            LoadConfig();
        }

        private string ConfigPath => Path.Combine(DataService.Instance.DatabaseDir, "rebate-config.ini");

        #region 配置

        public RebateConfig GetConfig()
        {
            lock (_lock)
            {
                return _config ?? RebateConfig.CreateDefault();
            }
        }

        public void SaveConfig(RebateConfig config)
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
                    _config = RebateConfig.CreateDefault();
                    return;
                }

                var config = new RebateConfig();
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
                        case "Mode": 
                            if (int.TryParse(value, out var mode)) 
                                config.Mode = (RebateMode)mode; 
                            break;
                        case "DefaultPercent":
                            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var dp))
                                config.DefaultPercent = dp;
                            break;
                        case "MinBetCount":
                            if (int.TryParse(value, out var mc))
                                config.MinBetCount = mc;
                            break;
                        case "ExcludeMultiCombo":
                            config.ExcludeMultiCombo = value.ToLower() == "true" || value == "1";
                            break;
                        case "ExcludeKillCombo":
                            config.ExcludeKillCombo = value.ToLower() == "true" || value == "1";
                            break;
                        case "ReturnTurnoverPercent":
                            config.ReturnTurnoverPercent = value.ToLower() == "true" || value == "1";
                            break;
                        default:
                            // 解析阶梯配置: Tier_1_Min, Tier_1_Max, Tier_1_Percent
                            if (key.StartsWith("Tier_"))
                            {
                                ParseTierConfig(config, key, value);
                            }
                            break;
                    }
                }

                _config = config;
            }
            catch
            {
                _config = RebateConfig.CreateDefault();
            }
        }

        private void ParseTierConfig(RebateConfig config, string key, string value)
        {
            var parts = key.Split('_');
            if (parts.Length != 3) return;

            if (!int.TryParse(parts[1], out var tierIndex)) return;
            tierIndex--; // 转为0索引

            while (config.Tiers.Count <= tierIndex)
            {
                config.Tiers.Add(new RebateTier());
            }

            if (!decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var d)) return;

            switch (parts[2])
            {
                case "Min": config.Tiers[tierIndex].Min = d; break;
                case "Max": config.Tiers[tierIndex].Max = d; break;
                case "Percent": config.Tiers[tierIndex].Percent = d; break;
            }
        }

        private void SaveConfigToFile(RebateConfig config)
        {
            try
            {
                Directory.CreateDirectory(DataService.Instance.DatabaseDir);
                var sb = new StringBuilder();
                sb.AppendLine("# 回水配置 - 自动生成");
                sb.AppendLine($"Mode={(int)config.Mode}");
                sb.AppendLine($"DefaultPercent={config.DefaultPercent.ToString(CultureInfo.InvariantCulture)}");
                sb.AppendLine($"MinBetCount={config.MinBetCount}");
                sb.AppendLine($"ExcludeMultiCombo={config.ExcludeMultiCombo}");
                sb.AppendLine($"ExcludeKillCombo={config.ExcludeKillCombo}");
                sb.AppendLine($"ReturnTurnoverPercent={config.ReturnTurnoverPercent}");

                for (int i = 0; i < config.Tiers.Count; i++)
                {
                    var tier = config.Tiers[i];
                    sb.AppendLine($"Tier_{i + 1}_Min={tier.Min.ToString(CultureInfo.InvariantCulture)}");
                    sb.AppendLine($"Tier_{i + 1}_Max={tier.Max.ToString(CultureInfo.InvariantCulture)}");
                    sb.AppendLine($"Tier_{i + 1}_Percent={tier.Percent.ToString(CultureInfo.InvariantCulture)}");
                }

                File.WriteAllText(ConfigPath, sb.ToString(), Encoding.UTF8);
            }
            catch
            {
                // ignore
            }
        }

        #endregion

        #region 回水计算

        /// <summary>
        /// 计算玩家回水
        /// </summary>
        /// <param name="playerId">玩家ID</param>
        /// <param name="totalBet">总下注额</param>
        /// <param name="betCount">下注次数</param>
        /// <param name="totalLoss">总输分 (正数表示输)</param>
        /// <param name="comboPercent">组合比例 (0-100)</param>
        /// <returns>(回水金额, 错误信息)</returns>
        public (decimal rebate, string error) CalculateRebate(
            string playerId,
            decimal totalBet,
            int betCount,
            decimal totalLoss,
            decimal comboPercent = 0)
        {
            var config = GetConfig();

            // 检查下注次数是否达标
            if (config.MinBetCount > 0 && betCount < config.MinBetCount)
            {
                return (0m, $"把数不足{config.MinBetCount}把");
            }

            decimal baseValue;
            switch (config.Mode)
            {
                case RebateMode.ComboPercent:
                    // 按组合比例
                    baseValue = comboPercent;
                    break;

                case RebateMode.BetCount:
                    // 按下注次数
                    baseValue = betCount;
                    break;

                case RebateMode.Turnover:
                    // 按下注流水
                    baseValue = totalBet;
                    break;

                case RebateMode.Loss:
                    // 按输分
                    baseValue = totalLoss > 0 ? totalLoss : 0;
                    break;

                default:
                    return (0m, "未设置回水方式");
            }

            // 查找适用的阶梯
            var percent = config.DefaultPercent;
            foreach (var tier in config.Tiers)
            {
                if (baseValue >= tier.Min && baseValue <= tier.Max)
                {
                    percent = tier.Percent;
                    break;
                }
            }

            if (percent <= 0)
            {
                return (0m, null);
            }

            // 计算回水金额
            decimal rebate;
            if (config.ReturnTurnoverPercent && config.Mode == RebateMode.Turnover)
            {
                // 返流水百分比
                rebate = totalBet * (percent / 100m);
            }
            else if (config.Mode == RebateMode.Loss)
            {
                // 按输分比例返回
                rebate = totalLoss * (percent / 100m);
            }
            else
            {
                // 其他模式：按固定百分比返回下注额
                rebate = totalBet * (percent / 100m);
            }

            return (Math.Round(rebate, 2), null);
        }

        /// <summary>
        /// 处理玩家回水
        /// </summary>
        public (bool success, decimal rebate, string message) ProcessRebate(
            string playerId,
            string playerNick,
            decimal totalBet,
            int betCount,
            decimal totalLoss,
            decimal comboPercent = 0)
        {
            var (rebate, error) = CalculateRebate(playerId, totalBet, betCount, totalLoss, comboPercent);

            if (!string.IsNullOrEmpty(error))
            {
                return (false, 0m, error);
            }

            if (rebate <= 0)
            {
                return (true, 0m, "无回水");
            }

            // 添加回水到玩家账户
            var newBalance = ScoreService.Instance.AddRebate(playerId, rebate, "回水");

            var template = MessageTemplateService.Instance.GetTemplate("返点_有回水回复");
            var variables = new Dictionary<string, string>
            {
                ["艾特"] = $"@{playerNick}",
                ["旺旺"] = playerNick,
                ["分数"] = rebate.ToString("F2", CultureInfo.InvariantCulture),
                ["余粮"] = newBalance.ToString("F2", CultureInfo.InvariantCulture)
            };

            var message = MessageTemplateService.Instance.RenderText(template, variables);
            return (true, rebate, message);
        }

        #endregion
    }

    #region 配置模型

    /// <summary>
    /// 回水方式
    /// </summary>
    public enum RebateMode
    {
        /// <summary>按组合比例</summary>
        ComboPercent = 0,
        /// <summary>按下注次数</summary>
        BetCount = 1,
        /// <summary>按下注流水</summary>
        Turnover = 2,
        /// <summary>按输分</summary>
        Loss = 3
    }

    /// <summary>
    /// 回水配置
    /// </summary>
    public class RebateConfig
    {
        /// <summary>回水方式</summary>
        public RebateMode Mode { get; set; } = RebateMode.Turnover;

        /// <summary>默认返点百分比</summary>
        public decimal DefaultPercent { get; set; } = 1m;

        /// <summary>最小下注次数</summary>
        public int MinBetCount { get; set; } = 1;

        /// <summary>排除多组合</summary>
        public bool ExcludeMultiCombo { get; set; } = false;

        /// <summary>排除杀组合</summary>
        public bool ExcludeKillCombo { get; set; } = false;

        /// <summary>返流水百分比 (流水模式下生效)</summary>
        public bool ReturnTurnoverPercent { get; set; } = true;

        /// <summary>阶梯配置</summary>
        public List<RebateTier> Tiers { get; set; } = new List<RebateTier>();

        public static RebateConfig CreateDefault()
        {
            return new RebateConfig
            {
                Mode = RebateMode.Turnover,
                DefaultPercent = 1m,
                MinBetCount = 1,
                Tiers = new List<RebateTier>
                {
                    // 默认阶梯: 按流水
                    new RebateTier { Min = 100m, Max = 10000m, Percent = 6m },
                    new RebateTier { Min = 10001m, Max = 30000m, Percent = 8m },
                    new RebateTier { Min = 30001m, Max = 2000000m, Percent = 10m }
                }
            };
        }
    }

    /// <summary>
    /// 回水阶梯
    /// </summary>
    public class RebateTier
    {
        /// <summary>最小值</summary>
        public decimal Min { get; set; }

        /// <summary>最大值</summary>
        public decimal Max { get; set; }

        /// <summary>返点百分比</summary>
        public decimal Percent { get; set; }
    }

    #endregion
}
