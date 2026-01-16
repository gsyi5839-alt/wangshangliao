using System;
using System.IO;
using System.Text;

namespace WangShangLiaoBot.Services
{
    /// <summary>
    /// Bet attack range settings service - manages bet limits for different bet types
    /// Settings persist to file: 攻击范围设置.ini
    /// </summary>
    public sealed class BetAttackRangeSettingsService
    {
        private static BetAttackRangeSettingsService _instance;
        public static BetAttackRangeSettingsService Instance => _instance ?? (_instance = new BetAttackRangeSettingsService());

        private readonly string _settingsFile;

        private BetAttackRangeSettingsService()
        {
            _settingsFile = Path.Combine(DataService.Instance.DatabaseDir, "攻击范围设置.ini");
            LoadFromFile();
        }

        #region Bet Limits - Row 1: 单注, 对子, 尾单注, 大边

        /// <summary>单注最小</summary>
        public decimal SingleBetMin { get; set; } = 2;
        /// <summary>单注最大</summary>
        public decimal SingleBetMax { get; set; } = 3000;

        /// <summary>对子最小</summary>
        public decimal PairMin { get; set; } = 2;
        /// <summary>对子最大</summary>
        public decimal PairMax { get; set; } = 500;

        /// <summary>尾单注最小</summary>
        public decimal TailSingleMin { get; set; } = 0;
        /// <summary>尾单注最大</summary>
        public decimal TailSingleMax { get; set; } = 0;

        /// <summary>大边最小</summary>
        public decimal BigEdgeMin { get; set; } = 0;
        /// <summary>大边最大</summary>
        public decimal BigEdgeMax { get; set; } = 0;

        #endregion

        #region Bet Limits - Row 2: 组合, 顺子, 尾组合, 小边

        /// <summary>组合最小</summary>
        public decimal CombinationMin { get; set; } = 2;
        /// <summary>组合最大</summary>
        public decimal CombinationMax { get; set; } = 1000;

        /// <summary>顺子最小</summary>
        public decimal StraightMin { get; set; } = 2;
        /// <summary>顺子最大</summary>
        public decimal StraightMax { get; set; } = 500;

        /// <summary>尾组合最小</summary>
        public decimal TailCombinationMin { get; set; } = 0;
        /// <summary>尾组合最大</summary>
        public decimal TailCombinationMax { get; set; } = 0;

        /// <summary>小边最小</summary>
        public decimal SmallEdgeMin { get; set; } = 0;
        /// <summary>小边最大</summary>
        public decimal SmallEdgeMax { get; set; } = 0;

        #endregion

        #region Bet Limits - Row 3: 数字, 豹子, 尾数字, 边

        /// <summary>数字最小</summary>
        public decimal DigitMin { get; set; } = 2;
        /// <summary>数字最大</summary>
        public decimal DigitMax { get; set; } = 500;

        /// <summary>豹子最小</summary>
        public decimal LeopardMin { get; set; } = 2;
        /// <summary>豹子最大</summary>
        public decimal LeopardMax { get; set; } = 200;

        /// <summary>尾数字最小</summary>
        public decimal TailDigitMin { get; set; } = 0;
        /// <summary>尾数字最大</summary>
        public decimal TailDigitMax { get; set; } = 0;

        /// <summary>边最小</summary>
        public decimal EdgeMin { get; set; } = 0;
        /// <summary>边最大</summary>
        public decimal EdgeMax { get; set; } = 0;

        #endregion

        #region Bet Limits - Row 4: 极数, 半顺, 和, 中

        /// <summary>极数最小</summary>
        public decimal ExtremeMin { get; set; } = 2;
        /// <summary>极数最大</summary>
        public decimal ExtremeMax { get; set; } = 500;

        /// <summary>半顺最小</summary>
        public decimal HalfStraightMin { get; set; } = 0;
        /// <summary>半顺最大</summary>
        public decimal HalfStraightMax { get; set; } = 0;

        /// <summary>和最小</summary>
        public decimal SumMin { get; set; } = 0;
        /// <summary>和最大</summary>
        public decimal SumMax { get; set; } = 0;

        /// <summary>中最小</summary>
        public decimal MiddleMin { get; set; } = 0;
        /// <summary>中最大</summary>
        public decimal MiddleMax { get; set; } = 0;

        #endregion

        #region Bet Limits - Row 5: 龙虎, 杂, 三军, 总额封顶

        /// <summary>龙虎最小</summary>
        public decimal DragonTigerMin { get; set; } = 0;
        /// <summary>龙虎最大</summary>
        public decimal DragonTigerMax { get; set; } = 0;

        /// <summary>杂最小</summary>
        public decimal MixedMin { get; set; } = 0;
        /// <summary>杂最大</summary>
        public decimal MixedMax { get; set; } = 0;

        /// <summary>三军最小</summary>
        public decimal ThreeArmyMin { get; set; } = 0;
        /// <summary>三军最大</summary>
        public decimal ThreeArmyMax { get; set; } = 0;

        /// <summary>总额封顶</summary>
        public decimal TotalLimit { get; set; } = 60000;

        #endregion

        #region Over Range Message

        /// <summary>超范围提示内容</summary>
        public string OverRangeHintMsg { get; set; } = "@qq 您攻击的[[下注内容]]分数不能[高低],请及时修改攻击";

        #endregion

        #region Validation Methods

        /// <summary>
        /// Check if bet amount is within valid range for the given bet type
        /// Returns null if valid, or error message if invalid
        /// </summary>
        public string ValidateBetAmount(string betType, decimal amount)
        {
            decimal min = 0, max = 0;
            string typeName = betType;
            var upperType = (betType ?? "").ToUpper();

            switch (upperType)
            {
                // 大小单双 (单注)
                case "D":
                case "X":
                case "DS":
                case "S":
                case "DD":
                case "XD":
                case "XS":
                case "大":
                case "小":
                case "单":
                case "双":
                case "大单":
                case "大双":
                case "小单":
                case "小双":
                    min = SingleBetMin;
                    max = SingleBetMax;
                    typeName = "单注";
                    break;

                // 对子
                case "DZ":
                case "对子":
                case "对":
                    min = PairMin;
                    max = PairMax;
                    typeName = "对子";
                    break;

                // 组合
                case "ZH":
                case "组合":
                case "组":
                    min = CombinationMin;
                    max = CombinationMax;
                    typeName = "组合";
                    break;

                // 顺子
                case "SZ":
                case "顺子":
                case "顺":
                    min = StraightMin;
                    max = StraightMax;
                    typeName = "顺子";
                    break;

                // 豹子
                case "BZ":
                case "豹子":
                case "豹":
                    min = LeopardMin;
                    max = LeopardMax;
                    typeName = "豹子";
                    break;

                // 极数 (极大/极小)
                case "J":
                case "JD":
                case "JX":
                case "极":
                case "极大":
                case "极小":
                    min = ExtremeMin;
                    max = ExtremeMax;
                    typeName = "极数";
                    break;

                // 半顺
                case "BS":
                case "半顺":
                    min = HalfStraightMin;
                    max = HalfStraightMax;
                    typeName = "半顺";
                    break;

                // 和
                case "HE":
                case "和":
                case "合":
                    min = SumMin;
                    max = SumMax;
                    typeName = "和";
                    break;

                // 中
                case "Z":
                case "中":
                    min = MiddleMin;
                    max = MiddleMax;
                    typeName = "中";
                    break;

                // 龙虎
                case "L":
                case "H":
                case "LH":
                case "龙":
                case "虎":
                case "龙虎":
                    min = DragonTigerMin;
                    max = DragonTigerMax;
                    typeName = "龙虎";
                    break;

                // 杂
                case "ZA":
                case "杂":
                    min = MixedMin;
                    max = MixedMax;
                    typeName = "杂";
                    break;

                // 三军
                case "SJ":
                case "三军":
                    min = ThreeArmyMin;
                    max = ThreeArmyMax;
                    typeName = "三军";
                    break;

                // 尾单注 (尾大/尾小/尾单/尾双)
                case "WD":
                case "WX":
                case "WDD":
                case "WDS":
                case "尾大":
                case "尾小":
                case "尾单":
                case "尾双":
                    min = TailSingleMin;
                    max = TailSingleMax;
                    typeName = "尾单注";
                    break;

                // 尾组合
                case "WZH":
                case "尾组合":
                    min = TailCombinationMin;
                    max = TailCombinationMax;
                    typeName = "尾组合";
                    break;

                // 尾数字
                case "WSZ":
                case "尾数字":
                    min = TailDigitMin;
                    max = TailDigitMax;
                    typeName = "尾数字";
                    break;

                // 大边
                case "DB":
                case "大边":
                    min = BigEdgeMin;
                    max = BigEdgeMax;
                    typeName = "大边";
                    break;

                // 小边
                case "XB":
                case "小边":
                    min = SmallEdgeMin;
                    max = SmallEdgeMax;
                    typeName = "小边";
                    break;

                // 边
                case "B":
                case "边":
                    min = EdgeMin;
                    max = EdgeMax;
                    typeName = "边";
                    break;

                // 数字 (0-27)
                default:
                    if (int.TryParse(betType, out var num) && num >= 0 && num <= 27)
                    {
                        min = DigitMin;
                        max = DigitMax;
                        typeName = "数字";
                    }
                    break;
            }

            // If no range configured (both 0), skip validation
            if (min == 0 && max == 0)
                return null;

            // Check if amount is within range
            if (amount < min)
            {
                return FormatOverRangeMessage(typeName, amount, "低于" + min);
            }
            if (max > 0 && amount > max)
            {
                return FormatOverRangeMessage(typeName, amount, "高于" + max);
            }

            return null;
        }

        /// <summary>
        /// Check if total bet amount exceeds limit
        /// </summary>
        public string ValidateTotalAmount(decimal totalAmount, string betContent)
        {
            if (TotalLimit <= 0) return null;

            if (totalAmount > TotalLimit)
            {
                return FormatOverRangeMessage("总额", totalAmount, "高于" + TotalLimit);
            }

            return null;
        }

        /// <summary>
        /// Format over range message with placeholders
        /// @qq -> placeholder for player mention (will be replaced by TemplateEngine)
        /// [下注内容] / [[下注内容]] -> actual bet content/type
        /// [高低] -> "高于X" or "低于X"
        /// </summary>
        private string FormatOverRangeMessage(string betContent, decimal amount, string highLow)
        {
            var msg = OverRangeHintMsg;
            msg = msg.Replace("[下注内容]", betContent);
            msg = msg.Replace("[[下注内容]]", betContent);
            msg = msg.Replace("[高低]", highLow);
            // Note: @qq placeholder will be replaced by TemplateEngine.Render()
            return msg;
        }

        #endregion

        #region Load/Save

        /// <summary>
        /// Load settings from file
        /// </summary>
        public void LoadFromFile()
        {
            try
            {
                if (!File.Exists(_settingsFile)) return;

                var lines = File.ReadAllLines(_settingsFile, Encoding.UTF8);
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;
                    var idx = line.IndexOf('=');
                    if (idx <= 0) continue;

                    var key = line.Substring(0, idx).Trim();
                    var value = line.Substring(idx + 1).Trim();

                    switch (key)
                    {
                        // Row 1
                        case "SingleBetMin": decimal.TryParse(value, out var v1); SingleBetMin = v1; break;
                        case "SingleBetMax": decimal.TryParse(value, out var v2); SingleBetMax = v2; break;
                        case "PairMin": decimal.TryParse(value, out var v3); PairMin = v3; break;
                        case "PairMax": decimal.TryParse(value, out var v4); PairMax = v4; break;
                        case "TailSingleMin": decimal.TryParse(value, out var v5); TailSingleMin = v5; break;
                        case "TailSingleMax": decimal.TryParse(value, out var v6); TailSingleMax = v6; break;
                        case "BigEdgeMin": decimal.TryParse(value, out var v7); BigEdgeMin = v7; break;
                        case "BigEdgeMax": decimal.TryParse(value, out var v8); BigEdgeMax = v8; break;

                        // Row 2
                        case "CombinationMin": decimal.TryParse(value, out var v9); CombinationMin = v9; break;
                        case "CombinationMax": decimal.TryParse(value, out var v10); CombinationMax = v10; break;
                        case "StraightMin": decimal.TryParse(value, out var v11); StraightMin = v11; break;
                        case "StraightMax": decimal.TryParse(value, out var v12); StraightMax = v12; break;
                        case "TailCombinationMin": decimal.TryParse(value, out var v13); TailCombinationMin = v13; break;
                        case "TailCombinationMax": decimal.TryParse(value, out var v14); TailCombinationMax = v14; break;
                        case "SmallEdgeMin": decimal.TryParse(value, out var v15); SmallEdgeMin = v15; break;
                        case "SmallEdgeMax": decimal.TryParse(value, out var v16); SmallEdgeMax = v16; break;

                        // Row 3
                        case "DigitMin": decimal.TryParse(value, out var v17); DigitMin = v17; break;
                        case "DigitMax": decimal.TryParse(value, out var v18); DigitMax = v18; break;
                        case "LeopardMin": decimal.TryParse(value, out var v19); LeopardMin = v19; break;
                        case "LeopardMax": decimal.TryParse(value, out var v20); LeopardMax = v20; break;
                        case "TailDigitMin": decimal.TryParse(value, out var v21); TailDigitMin = v21; break;
                        case "TailDigitMax": decimal.TryParse(value, out var v22); TailDigitMax = v22; break;
                        case "EdgeMin": decimal.TryParse(value, out var v23); EdgeMin = v23; break;
                        case "EdgeMax": decimal.TryParse(value, out var v24); EdgeMax = v24; break;

                        // Row 4
                        case "ExtremeMin": decimal.TryParse(value, out var v25); ExtremeMin = v25; break;
                        case "ExtremeMax": decimal.TryParse(value, out var v26); ExtremeMax = v26; break;
                        case "HalfStraightMin": decimal.TryParse(value, out var v27); HalfStraightMin = v27; break;
                        case "HalfStraightMax": decimal.TryParse(value, out var v28); HalfStraightMax = v28; break;
                        case "SumMin": decimal.TryParse(value, out var v29); SumMin = v29; break;
                        case "SumMax": decimal.TryParse(value, out var v30); SumMax = v30; break;
                        case "MiddleMin": decimal.TryParse(value, out var v31); MiddleMin = v31; break;
                        case "MiddleMax": decimal.TryParse(value, out var v32); MiddleMax = v32; break;

                        // Row 5
                        case "DragonTigerMin": decimal.TryParse(value, out var v33); DragonTigerMin = v33; break;
                        case "DragonTigerMax": decimal.TryParse(value, out var v34); DragonTigerMax = v34; break;
                        case "MixedMin": decimal.TryParse(value, out var v35); MixedMin = v35; break;
                        case "MixedMax": decimal.TryParse(value, out var v36); MixedMax = v36; break;
                        case "ThreeArmyMin": decimal.TryParse(value, out var v37); ThreeArmyMin = v37; break;
                        case "ThreeArmyMax": decimal.TryParse(value, out var v38); ThreeArmyMax = v38; break;
                        case "TotalLimit": decimal.TryParse(value, out var v39); TotalLimit = v39; break;

                        // Message
                        case "OverRangeHintMsg": OverRangeHintMsg = value; break;
                    }
                }

                Logger.Info("[BetAttackRangeSettings] Settings loaded from file");
            }
            catch (Exception ex)
            {
                Logger.Error($"[BetAttackRangeSettings] Load error: {ex.Message}");
            }
        }

        /// <summary>
        /// Save settings to file
        /// </summary>
        public void SaveToFile()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_settingsFile));

                var sb = new StringBuilder();
                sb.AppendLine("# 攻击范围设置 (下注范围限制)");
                sb.AppendLine($"# 更新时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine();

                // Row 1
                sb.AppendLine("# === Row 1: 单注, 对子, 尾单注, 大边 ===");
                sb.AppendLine($"SingleBetMin={SingleBetMin}");
                sb.AppendLine($"SingleBetMax={SingleBetMax}");
                sb.AppendLine($"PairMin={PairMin}");
                sb.AppendLine($"PairMax={PairMax}");
                sb.AppendLine($"TailSingleMin={TailSingleMin}");
                sb.AppendLine($"TailSingleMax={TailSingleMax}");
                sb.AppendLine($"BigEdgeMin={BigEdgeMin}");
                sb.AppendLine($"BigEdgeMax={BigEdgeMax}");
                sb.AppendLine();

                // Row 2
                sb.AppendLine("# === Row 2: 组合, 顺子, 尾组合, 小边 ===");
                sb.AppendLine($"CombinationMin={CombinationMin}");
                sb.AppendLine($"CombinationMax={CombinationMax}");
                sb.AppendLine($"StraightMin={StraightMin}");
                sb.AppendLine($"StraightMax={StraightMax}");
                sb.AppendLine($"TailCombinationMin={TailCombinationMin}");
                sb.AppendLine($"TailCombinationMax={TailCombinationMax}");
                sb.AppendLine($"SmallEdgeMin={SmallEdgeMin}");
                sb.AppendLine($"SmallEdgeMax={SmallEdgeMax}");
                sb.AppendLine();

                // Row 3
                sb.AppendLine("# === Row 3: 数字, 豹子, 尾数字, 边 ===");
                sb.AppendLine($"DigitMin={DigitMin}");
                sb.AppendLine($"DigitMax={DigitMax}");
                sb.AppendLine($"LeopardMin={LeopardMin}");
                sb.AppendLine($"LeopardMax={LeopardMax}");
                sb.AppendLine($"TailDigitMin={TailDigitMin}");
                sb.AppendLine($"TailDigitMax={TailDigitMax}");
                sb.AppendLine($"EdgeMin={EdgeMin}");
                sb.AppendLine($"EdgeMax={EdgeMax}");
                sb.AppendLine();

                // Row 4
                sb.AppendLine("# === Row 4: 极数, 半顺, 和, 中 ===");
                sb.AppendLine($"ExtremeMin={ExtremeMin}");
                sb.AppendLine($"ExtremeMax={ExtremeMax}");
                sb.AppendLine($"HalfStraightMin={HalfStraightMin}");
                sb.AppendLine($"HalfStraightMax={HalfStraightMax}");
                sb.AppendLine($"SumMin={SumMin}");
                sb.AppendLine($"SumMax={SumMax}");
                sb.AppendLine($"MiddleMin={MiddleMin}");
                sb.AppendLine($"MiddleMax={MiddleMax}");
                sb.AppendLine();

                // Row 5
                sb.AppendLine("# === Row 5: 龙虎, 杂, 三军, 总额封顶 ===");
                sb.AppendLine($"DragonTigerMin={DragonTigerMin}");
                sb.AppendLine($"DragonTigerMax={DragonTigerMax}");
                sb.AppendLine($"MixedMin={MixedMin}");
                sb.AppendLine($"MixedMax={MixedMax}");
                sb.AppendLine($"ThreeArmyMin={ThreeArmyMin}");
                sb.AppendLine($"ThreeArmyMax={ThreeArmyMax}");
                sb.AppendLine($"TotalLimit={TotalLimit}");
                sb.AppendLine();

                // Message
                sb.AppendLine("# === 超范围提示 ===");
                sb.AppendLine($"OverRangeHintMsg={OverRangeHintMsg}");

                File.WriteAllText(_settingsFile, sb.ToString(), Encoding.UTF8);
                Logger.Info("[BetAttackRangeSettings] Settings saved to file");
            }
            catch (Exception ex)
            {
                Logger.Error($"[BetAttackRangeSettings] Save error: {ex.Message}");
            }
        }

        #endregion
    }
}

