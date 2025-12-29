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
    /// This is best-effort and focuses on Canada28 classic tokens described in variable help:
    /// - "2000XD 20000DS"
    /// - "小单2000 大双20000"
    /// - "大100 小100 单100 双100" (optional)
    /// </summary>
    public static class BetMessageParser
    {
        private static readonly Regex TokenAmountCode = new Regex(@"(\d+(\.\d+)?)([A-Za-z]{1,4})", RegexOptions.Compiled);
        private static readonly Regex TokenCodeAmount = new Regex(@"([A-Za-z]{1,4})(\d+(\.\d+)?)", RegexOptions.Compiled);
        private static readonly Regex TokenChinese = new Regex(@"(大单|大双|小单|小双|大|小|单|双)\s*(\d+(\.\d+)?)", RegexOptions.Compiled);

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

            // 1) Chinese tokens: 小单2000 大双20000
            foreach (Match m in TokenChinese.Matches(txt))
            {
                var codeCn = m.Groups[1].Value;
                var amountStr = m.Groups[2].Value;
                if (!decimal.TryParse(amountStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var amt))
                    decimal.TryParse(amountStr, out amt);
                if (amt <= 0) continue;

                var mapped = ChineseToCode(codeCn);
                if (!string.IsNullOrEmpty(mapped))
                {
                    items.Add(new BetItem { Kind = BetKind.Dxds, Code = mapped, Amount = amt });
                    total += amt;
                }
                else
                {
                    items.Add(new BetItem { Kind = GuessKind(codeCn), Code = codeCn, Amount = amt });
                    total += amt;
                }
            }

            // 2) Code tokens: 2000XD / XD2000
            foreach (Match m in TokenAmountCode.Matches(txt))
            {
                var amountStr = m.Groups[1].Value;
                var code = (m.Groups[3].Value ?? "").ToUpperInvariant();
                if (!IsDxdsCode(code)) continue;
                if (!decimal.TryParse(amountStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var amt))
                    decimal.TryParse(amountStr, out amt);
                if (amt <= 0) continue;
                items.Add(new BetItem { Kind = BetKind.Dxds, Code = code, Amount = amt });
                total += amt;
            }

            foreach (Match m in TokenCodeAmount.Matches(txt))
            {
                var code = (m.Groups[1].Value ?? "").ToUpperInvariant();
                var amountStr = m.Groups[2].Value;
                if (!IsDxdsCode(code)) continue;
                if (!decimal.TryParse(amountStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var amt))
                    decimal.TryParse(amountStr, out amt);
                if (amt <= 0) continue;
                items.Add(new BetItem { Kind = BetKind.Dxds, Code = code, Amount = amt });
                total += amt;
            }

            if (items.Count == 0) return false;

            normalized = ToChinese(items);
            return true;
        }

        private static bool IsDxdsCode(string code)
        {
            return code == "XD" || code == "XS" || code == "DD" || code == "DS";
        }

        private static string ChineseToCode(string cn)
        {
            switch (cn)
            {
                case "小单": return "XD";
                case "小双": return "XS";
                case "大单": return "DD";
                case "大双": return "DS";
                default: return "";
            }
        }

        private static BetKind GuessKind(string cn)
        {
            if (cn == "大" || cn == "小") return BetKind.BigSmall;
            if (cn == "单" || cn == "双") return BetKind.OddEven;
            return BetKind.Dxds;
        }

        /// <summary>
        /// Convert bet items into a human readable Chinese string.
        /// </summary>
        private static string ToChinese(List<BetItem> items)
        {
            var sb = new StringBuilder();
            foreach (var it in items)
            {
                var label = it.Code;
                if (it.Kind == BetKind.Dxds) label = CodeToChinese(it.Code);
                if (sb.Length > 0) sb.Append(" ");
                sb.Append(label);
                sb.Append(it.Amount.ToString(CultureInfo.InvariantCulture));
            }
            return sb.ToString();
        }

        private static string CodeToChinese(string code)
        {
            switch (code)
            {
                case "XD": return "小单";
                case "XS": return "小双";
                case "DD": return "大单";
                case "DS": return "大双";
                default: return code ?? "";
            }
        }
    }
}


