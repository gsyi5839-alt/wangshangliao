using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using WangShangLiaoBot.Models.Betting;

namespace WangShangLiaoBot.Services.Betting
{
    /// <summary>
    /// Parse bet messages from group chat into structured bet items.
    /// Supports multiple bet types:
    /// - DXDS (大小单双): XD/XS/DD/DS, 大单/大双/小单/小双/大/小/单/双
    /// - Pairs (对子): DZ, 对子, 对
    /// - Combination (组合): ZH, 组合, 组
    /// - Straight (顺子): SZ, 顺子, 顺
    /// - Leopard (豹子): BZ, 豹子, 豹
    /// - Digits (数字): 0-27
    /// - Extreme (极数): JD/JX, 极大/极小/极
    /// - HalfStraight (半顺): BS, 半顺
    /// - Sum (和): HE, 和, 合
    /// - Middle (中): Z, 中
    /// - DragonTiger (龙虎): L/H/LH, 龙/虎/龙虎
    /// - Mixed (杂): ZA, 杂
    /// - ThreeArmy (三军): SJ, 三军
    /// - TailBets (尾): 尾大/尾小/尾单/尾双
    /// - Edge (边): DB/XB/B, 大边/小边/边
    /// </summary>
    public static class BetMessageParser
    {
        // Basic DXDS patterns
        private static readonly Regex TokenAmountCode = new Regex(@"(\d+(\.\d+)?)([A-Za-z]{1,4})", RegexOptions.Compiled);
        private static readonly Regex TokenCodeAmount = new Regex(@"([A-Za-z]{1,4})(\d+(\.\d+)?)", RegexOptions.Compiled);
        
        // Extended Chinese patterns - matches Chinese keywords followed by amount
        private static readonly Regex TokenChineseExtended = new Regex(
            @"(大单|大双|小单|小双|大边|小边|极大|极小|尾大|尾小|尾单|尾双|尾组合|尾数字|龙虎|" +
            @"对子|组合|顺子|豹子|半顺|三军|" +
            @"大|小|单|双|对|组|顺|豹|极|和|合|中|龙|虎|杂|边)\s*(\d+(\.\d+)?)", 
            RegexOptions.Compiled);

        // 招财狗(ZCG)拼音简写格式: da100, x50, dad100, das50, xd30, xs40, dz50, sz100, bz30, long50, hu50
        private static readonly Regex TokenZcgPinyin = new Regex(
            @"(?i)(dad|das|xdd|xds|dads|long|hu|bao|dui|shun|bz|sz|dz|bs|za|jd|jx|da|xiao|dan|shuang|d|x|s)\s*(\d+(\.\d+)?)",
            RegexOptions.Compiled);

        // 特码格式: 操13/100, 草13 100, 点13/50, T13 30
        private static readonly Regex TokenSpecialDigit = new Regex(
            @"[操草点艹T=]\s*(\d{1,2})\s*[/\s]\s*(\d+(\.\d+)?)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);
        
        // Digit pattern: matches pure numbers 0-27 followed by amount (e.g., "13 100" or "13/100")
        private static readonly Regex TokenDigitBet = new Regex(@"(?<!\d)([0-2]?\d)\s*[/\s]\s*(\d+(\.\d+)?)(?!\d)", RegexOptions.Compiled);
        
        // Digit pattern alternative: amount followed by digit target (e.g., "100点13")
        private static readonly Regex TokenDigitBetAlt = new Regex(@"(\d+(\.\d+)?)\s*[点]\s*([0-2]?\d)(?!\d)", RegexOptions.Compiled);

        /// <summary>
        /// Try parse a chat message content into bet items.
        /// Returns false when content doesn't look like a bet.
        /// </summary>
        public static bool TryParse(string content, out List<BetItem> items, out decimal total, out string normalized)
        {
            items = new List<BetItem>();
            total = 0m;
            normalized = "";
            if (string.IsNullOrWhiteSpace(content)) return false;

            // Basic guard: ignore common non-bet texts
            var txt = content.Trim();
            if (txt.Length < 2) return false;
            if (txt.Contains("[") && txt.Contains("]")) return false; // template text, not bet

            // Normalize separators
            txt = txt.Replace("\r", " ").Replace("\n", " ");
            txt = txt.Replace("，", " ").Replace(",", " ").Replace("|", " ").Replace("；", " ").Replace(";", " ");

            var addedPositions = new HashSet<int>(); // Track matched positions to avoid duplicates

            // 1) Extended Chinese tokens: 对子100, 组合50, 顺子100, 豹子50, etc.
            foreach (Match m in TokenChineseExtended.Matches(txt))
            {
                if (addedPositions.Contains(m.Index)) continue;
                
                var codeCn = m.Groups[1].Value;
                var amountStr = m.Groups[2].Value;
                if (!decimal.TryParse(amountStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var amt))
                    decimal.TryParse(amountStr, out amt);
                if (amt <= 0) continue;

                var (code, kind) = MapChineseToCodeAndKind(codeCn);
                items.Add(new BetItem { Kind = kind, Code = code, Amount = amt });
                total += amt;
                addedPositions.Add(m.Index);
            }

            // 2) Code tokens: 2000XD / XD2000 / DZ100 / 100DZ etc.
            foreach (Match m in TokenAmountCode.Matches(txt))
            {
                if (addedPositions.Contains(m.Index)) continue;
                
                var amountStr = m.Groups[1].Value;
                var code = (m.Groups[3].Value ?? "").ToUpperInvariant();
                var (mappedCode, kind) = MapCodeToKind(code);
                if (kind == BetKind.Unknown) continue;
                
                if (!decimal.TryParse(amountStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var amt))
                    decimal.TryParse(amountStr, out amt);
                if (amt <= 0) continue;
                
                items.Add(new BetItem { Kind = kind, Code = mappedCode, Amount = amt });
                total += amt;
                addedPositions.Add(m.Index);
            }

            foreach (Match m in TokenCodeAmount.Matches(txt))
            {
                if (addedPositions.Contains(m.Index)) continue;
                
                var code = (m.Groups[1].Value ?? "").ToUpperInvariant();
                var amountStr = m.Groups[2].Value;
                var (mappedCode, kind) = MapCodeToKind(code);
                if (kind == BetKind.Unknown) continue;
                
                if (!decimal.TryParse(amountStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var amt))
                    decimal.TryParse(amountStr, out amt);
                if (amt <= 0) continue;
                
                items.Add(new BetItem { Kind = kind, Code = mappedCode, Amount = amt });
                total += amt;
                addedPositions.Add(m.Index);
            }

            // 3) Digit bets: "13 100" or "13/100" (bet on number 13 with amount 100)
            foreach (Match m in TokenDigitBet.Matches(txt))
            {
                if (addedPositions.Contains(m.Index)) continue;
                
                var digitStr = m.Groups[1].Value;
                var amountStr = m.Groups[2].Value;
                
                if (!int.TryParse(digitStr, out var digit)) continue;
                if (digit < 0 || digit > 27) continue; // Valid range for Canada28
                
                if (!decimal.TryParse(amountStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var amt))
                    decimal.TryParse(amountStr, out amt);
                if (amt <= 0) continue;
                
                items.Add(new BetItem { Kind = BetKind.Digit, Code = digit.ToString(), Amount = amt });
                total += amt;
                addedPositions.Add(m.Index);
            }

            // 4) Alternative digit pattern: "100点13"
            foreach (Match m in TokenDigitBetAlt.Matches(txt))
            {
                if (addedPositions.Contains(m.Index)) continue;
                
                var amountStr = m.Groups[1].Value;
                var digitStr = m.Groups[3].Value;
                
                if (!int.TryParse(digitStr, out var digit)) continue;
                if (digit < 0 || digit > 27) continue;
                
                if (!decimal.TryParse(amountStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var amt))
                    decimal.TryParse(amountStr, out amt);
                if (amt <= 0) continue;
                
                items.Add(new BetItem { Kind = BetKind.Digit, Code = digit.ToString(), Amount = amt });
                total += amt;
                addedPositions.Add(m.Index);
            }

            // 5) 招财狗拼音格式: da100, x50, dad100, das50, dz50, sz100, bz30
            foreach (Match m in TokenZcgPinyin.Matches(txt))
            {
                if (addedPositions.Contains(m.Index)) continue;

                var pinyinCode = m.Groups[1].Value.ToLowerInvariant();
                var amountStr = m.Groups[2].Value;

                if (!decimal.TryParse(amountStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var amt))
                    decimal.TryParse(amountStr, out amt);
                if (amt <= 0) continue;

                var (code, kind) = MapZcgPinyinToCodeAndKind(pinyinCode);
                if (kind == BetKind.Unknown) continue;

                items.Add(new BetItem { Kind = kind, Code = code, Amount = amt });
                total += amt;
                addedPositions.Add(m.Index);
            }

            // 6) 特码格式: 操13/100, 草13 100
            foreach (Match m in TokenSpecialDigit.Matches(txt))
            {
                if (addedPositions.Contains(m.Index)) continue;

                var digitStr = m.Groups[1].Value;
                var amountStr = m.Groups[2].Value;

                if (!int.TryParse(digitStr, out var digit)) continue;
                if (digit < 0 || digit > 27) continue;

                if (!decimal.TryParse(amountStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var amt))
                    decimal.TryParse(amountStr, out amt);
                if (amt <= 0) continue;

                items.Add(new BetItem { Kind = BetKind.Digit, Code = digit.ToString(), Amount = amt });
                total += amt;
                addedPositions.Add(m.Index);
            }

            if (items.Count == 0) return false;

            normalized = ToChinese(items);
            return true;
        }

        /// <summary>
        /// Map English code to internal code and bet kind
        /// </summary>
        private static (string code, BetKind kind) MapCodeToKind(string code)
        {
            switch (code)
            {
                // DXDS codes
                case "XD": return ("XD", BetKind.Dxds);
                case "XS": return ("XS", BetKind.Dxds);
                case "DD": return ("DD", BetKind.Dxds);
                case "DS": return ("DS", BetKind.Dxds);
                case "D": return ("D", BetKind.BigSmall);  // 大
                case "X": return ("X", BetKind.BigSmall);  // 小
                case "S": return ("S", BetKind.OddEven);   // 双
                
                // Pair (对子)
                case "DZ": return ("DZ", BetKind.Pair);
                
                // Combination (组合)
                case "ZH": return ("ZH", BetKind.Combination);
                
                // Straight (顺子)
                case "SZ": return ("SZ", BetKind.Straight);
                
                // Leopard (豹子)
                case "BZ": return ("BZ", BetKind.Leopard);
                
                // Extreme (极数)
                case "JD": return ("JD", BetKind.Extreme);  // 极大
                case "JX": return ("JX", BetKind.Extreme);  // 极小
                case "J": return ("J", BetKind.Extreme);    // 极
                
                // HalfStraight (半顺)
                case "BS": return ("BS", BetKind.HalfStraight);
                
                // Sum/和
                case "HE": return ("HE", BetKind.Sum);
                
                // Middle/中
                case "Z": return ("Z", BetKind.Middle);
                
                // Dragon/Tiger (龙虎)
                case "L": return ("L", BetKind.DragonTiger);   // 龙
                case "H": return ("H", BetKind.DragonTiger);   // 虎
                case "LH": return ("LH", BetKind.DragonTiger); // 龙虎
                
                // Mixed (杂)
                case "ZA": return ("ZA", BetKind.Mixed);
                
                // ThreeArmy (三军)
                case "SJ": return ("SJ", BetKind.ThreeArmy);
                
                // Edge (边)
                case "DB": return ("DB", BetKind.Edge);  // 大边
                case "XB": return ("XB", BetKind.Edge);  // 小边
                case "B": return ("B", BetKind.Edge);    // 边
                
                // Tail bets (尾)
                case "WD": return ("WD", BetKind.TailSingle);  // 尾大
                case "WX": return ("WX", BetKind.TailSingle);  // 尾小
                case "WDD": return ("WDD", BetKind.TailSingle); // 尾单
                case "WDS": return ("WDS", BetKind.TailSingle); // 尾双
                
                default: return (code, BetKind.Unknown);
            }
        }

        /// <summary>
        /// Map ZCG (招财狗) pinyin shortcodes to internal code and bet kind
        /// Supports: da(大), x/xiao(小), d/dan(单), s/shuang(双), 
        /// dad(大单), das(大双), xd/xdd(小单), xs/xds(小双),
        /// dz/dui(对子), sz/shun(顺子), bz/bao(豹子), bs(半顺),
        /// long(龙), hu(虎), jd(极大), jx(极小), za(杂)
        /// </summary>
        private static (string code, BetKind kind) MapZcgPinyinToCodeAndKind(string pinyin)
        {
            switch (pinyin)
            {
                // 大小单双
                case "da": return ("D", BetKind.BigSmall);       // 大
                case "x":
                case "xiao": return ("X", BetKind.BigSmall);     // 小
                case "d":
                case "dan": return ("单", BetKind.OddEven);      // 单
                case "s":
                case "shuang": return ("双", BetKind.OddEven);   // 双

                // 组合 (大单/大双/小单/小双)
                case "dad":
                case "dads": return ("DD", BetKind.Dxds);        // 大单
                case "das": return ("DS", BetKind.Dxds);         // 大双
                case "xd":
                case "xdd": return ("XD", BetKind.Dxds);         // 小单
                case "xs":
                case "xds": return ("XS", BetKind.Dxds);         // 小双

                // 特殊玩法
                case "dz":
                case "dui": return ("DZ", BetKind.Pair);         // 对子
                case "sz":
                case "shun": return ("SZ", BetKind.Straight);    // 顺子
                case "bz":
                case "bao": return ("BZ", BetKind.Leopard);      // 豹子
                case "bs": return ("BS", BetKind.HalfStraight);  // 半顺
                case "za": return ("ZA", BetKind.Mixed);         // 杂

                // 龙虎
                case "long": return ("L", BetKind.DragonTiger);  // 龙
                case "hu": return ("H", BetKind.DragonTiger);    // 虎

                // 极值
                case "jd": return ("JD", BetKind.Extreme);       // 极大
                case "jx": return ("JX", BetKind.Extreme);       // 极小

                default: return (pinyin, BetKind.Unknown);
            }
        }

        /// <summary>
        /// Map Chinese text to internal code and bet kind
        /// </summary>
        private static (string code, BetKind kind) MapChineseToCodeAndKind(string cn)
        {
            switch (cn)
            {
                // DXDS
                case "小单": return ("XD", BetKind.Dxds);
                case "小双": return ("XS", BetKind.Dxds);
                case "大单": return ("DD", BetKind.Dxds);
                case "大双": return ("DS", BetKind.Dxds);
                case "大": return ("D", BetKind.BigSmall);
                case "小": return ("X", BetKind.BigSmall);
                case "单": return ("单", BetKind.OddEven);
                case "双": return ("双", BetKind.OddEven);
                
                // Pair (对子)
                case "对子":
                case "对": return ("DZ", BetKind.Pair);
                
                // Combination (组合)
                case "组合":
                case "组": return ("ZH", BetKind.Combination);
                
                // Straight (顺子)
                case "顺子":
                case "顺": return ("SZ", BetKind.Straight);
                
                // Leopard (豹子)
                case "豹子":
                case "豹": return ("BZ", BetKind.Leopard);
                
                // Extreme (极数)
                case "极大": return ("JD", BetKind.Extreme);
                case "极小": return ("JX", BetKind.Extreme);
                case "极": return ("J", BetKind.Extreme);
                
                // HalfStraight (半顺)
                case "半顺": return ("BS", BetKind.HalfStraight);
                
                // Sum/和
                case "和":
                case "合": return ("HE", BetKind.Sum);
                
                // Middle/中
                case "中": return ("Z", BetKind.Middle);
                
                // Dragon/Tiger (龙虎)
                case "龙": return ("L", BetKind.DragonTiger);
                case "虎": return ("H", BetKind.DragonTiger);
                case "龙虎": return ("LH", BetKind.DragonTiger);
                
                // Mixed (杂)
                case "杂": return ("ZA", BetKind.Mixed);
                
                // ThreeArmy (三军)
                case "三军": return ("SJ", BetKind.ThreeArmy);
                
                // Edge (边)
                case "大边": return ("DB", BetKind.Edge);
                case "小边": return ("XB", BetKind.Edge);
                case "边": return ("B", BetKind.Edge);
                
                // Tail bets (尾)
                case "尾大": return ("WD", BetKind.TailSingle);
                case "尾小": return ("WX", BetKind.TailSingle);
                case "尾单": return ("WDD", BetKind.TailSingle);
                case "尾双": return ("WDS", BetKind.TailSingle);
                case "尾组合": return ("WZH", BetKind.TailCombination);
                case "尾数字": return ("WSZ", BetKind.TailDigit);
                
                default: return (cn, BetKind.Unknown);
            }
        }

        /// <summary>
        /// Convert bet items into a human readable Chinese string.
        /// </summary>
        private static string ToChinese(List<BetItem> items)
        {
            var sb = new StringBuilder();
            foreach (var it in items)
            {
                var label = CodeToChinese(it.Code, it.Kind);
                if (sb.Length > 0) sb.Append(" ");
                sb.Append(label);
                sb.Append(it.Amount.ToString(CultureInfo.InvariantCulture));
            }
            return sb.ToString();
        }

        /// <summary>
        /// Convert internal code to Chinese display text
        /// </summary>
        private static string CodeToChinese(string code, BetKind kind)
        {
            switch (code)
            {
                // DXDS
                case "XD": return "小单";
                case "XS": return "小双";
                case "DD": return "大单";
                case "DS": return "大双";
                case "D": return "大";
                case "X": return "小";
                case "单": return "单";
                case "双": return "双";
                
                // Pair
                case "DZ": return "对子";
                
                // Combination
                case "ZH": return "组合";
                
                // Straight
                case "SZ": return "顺子";
                
                // Leopard
                case "BZ": return "豹子";
                
                // Extreme
                case "JD": return "极大";
                case "JX": return "极小";
                case "J": return "极";
                
                // HalfStraight
                case "BS": return "半顺";
                
                // Sum
                case "HE": return "和";
                
                // Middle
                case "Z": return "中";
                
                // DragonTiger
                case "L": return "龙";
                case "H": return "虎";
                case "LH": return "龙虎";
                
                // Mixed
                case "ZA": return "杂";
                
                // ThreeArmy
                case "SJ": return "三军";
                
                // Edge
                case "DB": return "大边";
                case "XB": return "小边";
                case "B": return "边";
                
                // Tail
                case "WD": return "尾大";
                case "WX": return "尾小";
                case "WDD": return "尾单";
                case "WDS": return "尾双";
                case "WZH": return "尾组合";
                case "WSZ": return "尾数字";
                
                default:
                    // For digit bets, return the number directly
                    if (kind == BetKind.Digit) return code;
                    return code ?? "";
            }
        }
    }
}
