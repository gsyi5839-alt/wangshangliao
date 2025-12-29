using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using WangShangLiaoBot.Models;

namespace WangShangLiaoBot.Services
{
    /// <summary>
    /// Unified template engine for all message rendering.
    /// Supports both "basic tokens" and advanced tokens described in VariableHelpControl.
    /// </summary>
    public static partial class TemplateEngine
    {
        public sealed class RenderContext
        {
            /// <summary>Incoming chat message (auto-reply scenarios)</summary>
            public ChatMessage Message { get; set; }

            /// <summary>Player info (from DataService). Optional.</summary>
            public Player Player { get; set; }

            /// <summary>Score context (up/down score windows). Optional.</summary>
            public ScoreContext Score { get; set; }

            /// <summary>Force date for "today" computations. Defaults to DateTime.Today.</summary>
            public DateTime Today { get; set; } = DateTime.Today;
        }

        public sealed class ScoreContext
        {
            public string WangWangId { get; set; }
            public string Nickname { get; set; }
            public decimal Amount { get; set; }
            public decimal Grain { get; set; } // reserved/current balance shown in score UI
        }

        /// <summary>
        /// Render a template using unified token replacement.
        /// </summary>
        public static string Render(string template, RenderContext ctx)
        {
            if (string.IsNullOrEmpty(template)) return "";

            var now = DateTime.Now;
            var today = ctx?.Today == default ? DateTime.Today : ctx.Today;

            var msg = ctx?.Message;
            var player = ctx?.Player;
            var score = ctx?.Score;

            // Lottery snapshot (single source of truth for lottery variables)
            var lot = LotteryService.Instance;
            var n1 = lot.Number1;
            var n2 = lot.Number2;
            var n3 = lot.Number3;
            var sum = lot.Sum;
            var period = lot.CurrentPeriod ?? "";
            var openTime = lot.OpenTime;

            // Group dimension: when customers choose group dynamically, we must bind tokens to the current teamId.
            var teamId = (msg != null && msg.IsGroupMessage) ? (msg.GroupId ?? "") : "";

            // Determine identity fields
            var nickname =
                score?.Nickname
                ?? msg?.SenderName
                ?? player?.Nickname
                ?? "";

            var wangwangId =
                score?.WangWangId
                ?? msg?.SenderId
                ?? player?.WangWangId
                ?? "";

            var playerNo = wangwangId.Length >= 4 ? wangwangId.Substring(0, 4) : wangwangId;

            // Determine numeric fields
            var scoreAmount = score != null ? score.Amount : (decimal?)null;
            var grain = score != null ? score.Grain : (decimal?)player?.ReservedScore;

            // Bet/attack content: best-effort from Player.Remark (current known bet content)
            var attackRaw = player?.Remark ?? "";
            var attackParts = SplitAttacks(attackRaw);
            var attack1 = attackParts.Count > 0 ? attackParts[0] : "";
            var attack2 = attackParts.Count > 1 ? attackParts[1] : "";
            var attack3 = attackParts.Count > 2 ? attackParts[2] : "";

            var result = template;

            // ===== Special "block" tokens (may expand to multi-line text) =====
            if (result.Contains("[下注核对]"))
                result = result.Replace("[下注核对]", RenderBetCheck(today, teamId, chineseBet: false));
            if (result.Contains("[下注核对2]"))
                result = result.Replace("[下注核对2]", RenderBetCheck(today, teamId, chineseBet: true));

            if (result.Contains("[开奖历史]"))
                result = result.Replace("[开奖历史]", RenderLotteryHistory(today, maxLines: 15));
            if (result.Contains("[龙虎历史]"))
                result = result.Replace("[龙虎历史]", RenderDragonTigerHistory(today, maxLines: 30, asChinese: false));
            if (result.Contains("[龙虎历史2]"))
                result = result.Replace("[龙虎历史2]", RenderDragonTigerHistory(today, maxLines: 30, asChinese: true));
            if (result.Contains("[尾球历史]"))
                result = result.Replace("[尾球历史]", RenderTailHistory(today, maxLines: 30));
            if (result.Contains("[豹顺对历史]"))
                result = result.Replace("[豹顺对历史]", RenderBaoShunDuiHistory(today, maxLines: 30));
            if (result.Contains("[边历史]"))
                result = result.Replace("[边历史]", RenderEdgeHistory(today, maxLines: 30));
            if (result.Contains("[开奖图]"))
                result = result.Replace("[开奖图]", RenderLotteryTrend(today, maxLines: 30));
            if (result.Contains("[中奖玩家]"))
                result = result.Replace("[中奖玩家]", RenderWinnersText(today, teamId));

            if (result.Contains("[今天统计]"))
                result = result.Replace("[今天统计]", RenderTodayStats(today, wangwangId, mode: TodayStatsMode.Full));
            if (result.Contains("[今天统计2]"))
                result = result.Replace("[今天统计2]", RenderTodayStats(today, wangwangId, mode: TodayStatsMode.Simple));
            if (result.Contains("[今天统计盈利]"))
                result = result.Replace("[今天统计盈利]", RenderTodayStats(today, wangwangId, mode: TodayStatsMode.ProfitOnly));
            if (result.Contains("[今天统计流水]"))
                result = result.Replace("[今天统计流水]", RenderTodayStats(today, wangwangId, mode: TodayStatsMode.FlowOnly));
            if (result.Contains("[今天统计期数]"))
                result = result.Replace("[今天统计期数]", RenderTodayStats(today, wangwangId, mode: TodayStatsMode.PeriodOnly));

            if (result.Contains("[最近下注]"))
                result = result.Replace("[最近下注]", RenderRecentBets(today, wangwangId, maxLines: 5));

            if (result.Contains("[账单]"))
                result = result.Replace("[账单]", RenderBill(includeIds: false));
            if (result.Contains("[账单2]"))
                result = result.Replace("[账单2]", RenderBill(includeIds: true));

            // ===== Basic tokens =====
            result = result.Replace("[换行]", "\n");
            result = result.Replace("[时间]", now.ToString("HH:mm:ss"));
            result = result.Replace("[日期]", now.ToString("yyyy-MM-dd"));
            result = result.Replace("[时]", now.Hour.ToString(CultureInfo.InvariantCulture));
            result = result.Replace("[分]", now.Minute.ToString(CultureInfo.InvariantCulture));
            result = result.Replace("[秒]", now.Second.ToString(CultureInfo.InvariantCulture));

            // Aliases / mention tokens
            result = result.Replace("[艾特]", string.IsNullOrEmpty(nickname) ? "" : "@" + nickname);
            result = result.Replace("[公主艾特]", string.IsNullOrEmpty(nickname) ? "" : "@" + nickname);

            // Identity tokens
            result = result.Replace("[昵称]", nickname ?? "");
            // Per 变量说明.md: [旺旺] means first 4 digits (legacy projects also used [玩家号]).
            result = result.Replace("[旺旺]", playerNo ?? "");
            result = result.Replace("[玩家号]", playerNo ?? "");

            // Score tokens
            if (scoreAmount.HasValue)
                result = result.Replace("[分数]", scoreAmount.Value.ToString(CultureInfo.InvariantCulture));
            else
                result = result.Replace("[分数]", "");

            // Alias: [总分] in docs is equivalent to total score
            var totalScore = player != null ? player.Score : (scoreAmount ?? 0m);
            result = result.Replace("[总分]", totalScore.ToString(CultureInfo.InvariantCulture));

            // Grain/reserved tokens (项目中历史上 [留分] 与 [余粮] 常混用；这里默认同值)
            var grainText = grain.HasValue ? grain.Value.ToString(CultureInfo.InvariantCulture) : (player?.ReservedScore.ToString(CultureInfo.InvariantCulture) ?? "");
            result = result.Replace("[余粮]", grainText);
            result = result.Replace("[留分]", grainText);

            // Bet amount token: [下注分] = sum of numeric amounts found in attack string
            result = result.Replace("[下注分]", ExtractBetAmount(attackRaw).ToString(CultureInfo.InvariantCulture));

            // Countdown token (lottery)
            if (result.Contains("[封盘倒计时]"))
                result = result.Replace("[封盘倒计时]", LotteryService.Instance.Countdown.ToString(CultureInfo.InvariantCulture));

            // ===== Lottery tokens =====
            result = result.Replace("[在区]", n1.ToString(CultureInfo.InvariantCulture)); // legacy alias (kept for compatibility)
            result = result.Replace("[一区]", n1.ToString(CultureInfo.InvariantCulture));
            result = result.Replace("[二区]", n2.ToString(CultureInfo.InvariantCulture));
            result = result.Replace("[三区]", n3.ToString(CultureInfo.InvariantCulture));
            result = result.Replace("[开奖号码]", sum.ToString(CultureInfo.InvariantCulture));

            // [开奖时间] uses bold digits (visual emphasis); [开奖时间2] uses normal digits
            var openTimeText = openTime == DateTime.MinValue ? "" : openTime.ToString("HH:mm:ss");
            result = result.Replace("[开奖时间]", ToBoldDigits(openTimeText));
            result = result.Replace("[开奖时间2]", openTimeText);

            // [期数] uses bold digits; [期数2] uses normal digits
            result = result.Replace("[期数]", ToBoldDigits(period));
            result = result.Replace("[期数2]", period);

            // Current derived results
            var dxds = GetDxDsCode(sum);
            var dxdsCn = GetDxDsChinese(sum);
            result = result.Replace("[大小单双]", dxds);
            result = result.Replace("[大小单双2]", dxds.ToLowerInvariant());
            result = result.Replace("[大小单双3]", dxdsCn);

            var baoShunDui = GetBaoShunDui(n1, n2, n3);
            result = result.Replace("[豹顺对子]", baoShunDui);
            result = result.Replace("[豹顺对子2]", baoShunDui.ToLowerInvariant());
            result = result.Replace("[豹顺对子3]", GetBaoShunDuiMixed(n1, n2, n3));

            var lhb = GetDragonTigerLeopardCode(sum);
            result = result.Replace("[龙虎豹]", lhb);
            result = result.Replace("[龙虎豹2]", lhb.ToLowerInvariant());
            result = result.Replace("[龙虎豹3]", GetDragonTigerLeopardMixed(sum));

            result = result.Replace("[09回本]", (sum == 0 || sum == 9) ? (sum.ToString(CultureInfo.InvariantCulture) + "回本") : "");

            // Bill summary tokens
            result = result.Replace("[客户人数]", DataService.Instance.GetAllPlayers().Count.ToString(CultureInfo.InvariantCulture));
            result = result.Replace("[总分数]", RenderBillTotalScore().ToString(CultureInfo.InvariantCulture));

            // Bet/attack tokens (best-effort)
            result = result.Replace("[玩家]", string.IsNullOrWhiteSpace(attackRaw) ? "未攻击" : attackRaw);
            // Per 变量说明.md: [下注] shows "未攻击" when empty; [下注2]/[下注3] are blank when empty.
            result = result.Replace("[下注]", string.IsNullOrWhiteSpace(attackRaw) ? "未攻击" : attackRaw);
            result = result.Replace("[下注2]", string.IsNullOrWhiteSpace(attackRaw) ? "" : attackRaw);
            // [下注3] prefers Chinese display if possible
            result = result.Replace("[下注3]", string.IsNullOrWhiteSpace(attackRaw) ? "" : ToChineseBetText(attackRaw));

            return result;
        }

        // ===== Implementations for advanced tokens =====

        private enum TodayStatsMode
        {
            Full,
            Simple,
            ProfitOnly,
            FlowOnly,
            PeriodOnly,
        }

        private static string RenderTodayStats(DateTime day, string wangwangId, TodayStatsMode mode)
        {
            if (string.IsNullOrEmpty(wangwangId)) return "";

            // These stats are maintained by DataService.UpdateDailyStats(...) (updated on SavePlayer).
            var dateKey = day.ToString("yyyy-MM-dd");
            var ds = DataService.Instance;

            decimal startScore = ds.GetDailyDecimal(dateKey, wangwangId, "StartScore", defaultValue: 0m);
            decimal lastScore = ds.GetDailyDecimal(dateKey, wangwangId, "LastScore", defaultValue: startScore);
            decimal flow = ds.GetDailyDecimal(dateKey, wangwangId, "Flow", defaultValue: 0m);

            var player = ds.GetPlayer(wangwangId);
            var currentScore = player?.Score ?? lastScore;
            var profit = currentScore - startScore;

            // Period count is global today (lottery updates), not per-player, but still useful.
            var periodCount = ds.GetDailyInt(dateKey, key: "LotteryPeriodCount", defaultValue: 0);

            switch (mode)
            {
                case TodayStatsMode.ProfitOnly:
                    return profit.ToString(CultureInfo.InvariantCulture);
                case TodayStatsMode.FlowOnly:
                    return flow.ToString(CultureInfo.InvariantCulture);
                case TodayStatsMode.PeriodOnly:
                    return periodCount.ToString(CultureInfo.InvariantCulture);
                case TodayStatsMode.Simple:
                    return $"盈利:{profit} 流水:{flow}";
                case TodayStatsMode.Full:
                default:
                    // "补亏" is not tracked in current codebase; keep as 0 for now (extensible later).
                    var buKui = 0m;
                    // "回粮" best-effort: use ReservedScore (留分) since it’s displayed as “操库/余粮” in UI.
                    var huiLiang = player?.ReservedScore ?? 0m;
                    return $"盈利:{profit} 补亏:{buKui} 回粮:{huiLiang} 流水:{flow} 期数:{periodCount}";
            }
        }

        private static string RenderRecentBets(DateTime day, string contactId, int maxLines)
        {
            if (string.IsNullOrEmpty(contactId)) return "";
            try
            {
                var logs = DataService.Instance.GetMessageLogs(day, contactId)
                    .Where(l => l.Direction == "接收")
                    .Select(l => l.Content)
                    .Where(c => !string.IsNullOrWhiteSpace(c))
                    .Reverse() // newest first
                    .Take(maxLines)
                    .Reverse()
                    .ToList();

                return logs.Count == 0 ? "" : string.Join("\n", logs);
            }
            catch
            {
                return "";
            }
        }

        private static string RenderLotteryHistory(DateTime day, int maxLines)
        {
            try
            {
                var file = DataService.Instance.GetLotteryHistoryFile(day);
                if (!File.Exists(file)) return "";
                // NOTE: .NET Framework 4.7.2 LINQ doesn't have TakeLast().
                var all = File.ReadAllLines(file, Encoding.UTF8)
                    .Where(l => !string.IsNullOrWhiteSpace(l))
                    .ToList();
                var skip = Math.Max(0, all.Count - maxLines);
                var lines = all.Skip(skip).ToList();
                return string.Join("\n", lines);
            }
            catch
            {
                return "";
            }
        }

        private static string RenderDragonTigerHistory(DateTime day, int maxLines, bool asChinese)
        {
            var rows = ReadLotteryRows(day, maxLines);
            if (rows.Count == 0) return "";

            var sb = new StringBuilder();
            foreach (var r in rows)
            {
                var code = GetDragonTigerLeopardCode(r.Sum);
                sb.Append(asChinese ? ToDragonTigerLeopardChinese(code) : code);
            }
            return sb.ToString();
        }

        private static string RenderTailHistory(DateTime day, int maxLines)
        {
            var rows = ReadLotteryRows(day, maxLines);
            if (rows.Count == 0) return "";
            return string.Join("", rows.Select(r => (r.Sum % 10).ToString(CultureInfo.InvariantCulture)));
        }

        private static string RenderBaoShunDuiHistory(DateTime day, int maxLines)
        {
            var rows = ReadLotteryRows(day, maxLines);
            if (rows.Count == 0) return "";

            var sb = new StringBuilder();
            foreach (var r in rows)
            {
                sb.Append(GetBaoShunDuiMixed(r.N1, r.N2, r.N3));
            }
            return sb.ToString();
        }

        private static string RenderEdgeHistory(DateTime day, int maxLines)
        {
            var rows = ReadLotteryRows(day, maxLines);
            if (rows.Count == 0) return "";
            return string.Join("", rows.Select(r => IsEdge(r.Sum) ? "边" : "中"));
        }

        private static string RenderLotteryTrend(DateTime day, int maxLines)
        {
            // Text-based trend (no image generation in current codebase).
            var rows = ReadLotteryRows(day, maxLines);
            if (rows.Count == 0) return "";
            var sums = rows.Select(r => r.Sum.ToString(CultureInfo.InvariantCulture).PadLeft(2, '0'));
            return "趋势:" + string.Join(" ", sums);
        }

        private sealed class LotteryRow
        {
            public int N1 { get; set; }
            public int N2 { get; set; }
            public int N3 { get; set; }
            public int Sum { get; set; }
        }

        private static List<LotteryRow> ReadLotteryRows(DateTime day, int maxLines)
        {
            var list = new List<LotteryRow>();
            try
            {
                var file = DataService.Instance.GetLotteryHistoryFile(day);
                if (!File.Exists(file)) return list;
                var lines = File.ReadAllLines(file, Encoding.UTF8)
                    .Where(l => !string.IsNullOrWhiteSpace(l))
                    .ToList();
                var skip = Math.Max(0, lines.Count - maxLines);
                foreach (var line in lines.Skip(skip))
                {
                    // Expected format: 期号{period} 号码{n1}+{n2}+{n3}={sum}
                    var m = Regex.Match(line, @"号码(\d+)\+(\d+)\+(\d+)=(\d+)");
                    if (!m.Success) continue;
                    if (!int.TryParse(m.Groups[1].Value, out var a)) continue;
                    if (!int.TryParse(m.Groups[2].Value, out var b)) continue;
                    if (!int.TryParse(m.Groups[3].Value, out var c)) continue;
                    if (!int.TryParse(m.Groups[4].Value, out var s)) continue;
                    list.Add(new LotteryRow { N1 = a, N2 = b, N3 = c, Sum = s });
                }
            }
            catch
            {
                // ignore
            }
            return list;
        }

        private static decimal RenderBillTotalScore()
        {
            try
            {
                var players = DataService.Instance.GetAllPlayers();
                if (players == null) return 0m;
                return players.Sum(p => p.Score);
            }
            catch
            {
                return 0m;
            }
        }

        private static string RenderBill(bool includeIds)
        {
            try
            {
                var config = ConfigService.Instance.Config;
                var players = DataService.Instance.GetAllPlayers();
                if (players == null || players.Count == 0) return "";

                // Apply filters (same semantics as UI settings)
                if (config.HideLostPlayers)
                    players = players.Where(p => p.Score >= config.BillHideThreshold).ToList();
                if (!config.KeepZeroScoreBill)
                    players = players.Where(p => p.Score != 0).ToList();

                // Sort by score desc, stable output
                players = players.OrderByDescending(p => p.Score).ToList();

                var sb = new StringBuilder();
                foreach (var p in players)
                {
                    var name = string.IsNullOrWhiteSpace(p.Nickname) ? "玩家" : p.Nickname.Trim();
                    var id = p.WangWangId ?? "";
                    if (includeIds)
                        sb.Append($"{name}({id}$)={p.Score} ");
                    else
                        sb.Append($"{name}{p.Score} ");
                }
                return sb.ToString().Trim();
            }
            catch
            {
                return "";
            }
        }

        private static decimal ExtractBetAmount(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return 0m;
            try
            {
                decimal total = 0m;
                foreach (Match m in Regex.Matches(raw, @"(\d+(\.\d+)?)"))
                {
                    if (decimal.TryParse(m.Groups[1].Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var val))
                        total += val;
                }
                return total;
            }
            catch
            {
                return 0m;
            }
        }

        private static string ToChineseBetText(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "";

            // Best-effort translator for common bet codes (used by [下注核对2] and [下注3]).
            // Examples:
            // - "2000XD 20000DS" => "小单2000 大双20000"
            // - "小单2000 大双20000" => kept as-is
            var tokens = SplitAttacks(raw);
            if (tokens.Count == 0) tokens.Add(raw.Trim());

            var sb = new StringBuilder();
            foreach (var t in tokens)
            {
                var part = TranslateSingleBetToken(t);
                if (string.IsNullOrWhiteSpace(part)) continue;
                if (sb.Length > 0) sb.Append(" ");
                sb.Append(part);
            }
            return sb.ToString().Trim();
        }

        private static string TranslateSingleBetToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return "";
            var t = token.Trim();

            // Already Chinese-like: contains 大/小/单/双 and digits
            if (Regex.IsMatch(t, @"[大小单双].*\d+"))
                return t;

            // Pattern 1: amount + code (e.g., 2000XD)
            var m1 = Regex.Match(t, @"^(\d+(\.\d+)?)([A-Za-z]{1,4})$");
            if (m1.Success)
            {
                var amount = m1.Groups[1].Value;
                var code = m1.Groups[3].Value.ToUpperInvariant();
                var cn = CodeToChinese(code);
                return string.IsNullOrEmpty(cn) ? t : (cn + amount);
            }

            // Pattern 2: code + amount (e.g., XD2000)
            var m2 = Regex.Match(t, @"^([A-Za-z]{1,4})(\d+(\.\d+)?)$");
            if (m2.Success)
            {
                var code = m2.Groups[1].Value.ToUpperInvariant();
                var amount = m2.Groups[2].Value;
                var cn = CodeToChinese(code);
                return string.IsNullOrEmpty(cn) ? t : (cn + amount);
            }

            return t;
        }

        private static string CodeToChinese(string code)
        {
            switch (code)
            {
                case "XD": return "小单";
                case "XS": return "小双";
                case "DS": return "大双";
                case "DD": return "大单";
                default: return "";
            }
        }

        private static string GetDxDsCode(int sum)
        {
            // Canada28: 0-13 小, 14-27 大
            var bigSmall = sum >= 14 ? "D" : "X";
            var oddEven = (sum % 2 == 0) ? "S" : "D";
            // X/D + D/S  => XD/XS/DD/DS
            return bigSmall + oddEven;
        }

        private static string GetDxDsChinese(int sum)
        {
            var big = sum >= 14;
            var even = (sum % 2 == 0);
            return (big ? "大" : "小") + (even ? "双" : "单");
        }

        private static string GetBaoShunDui(int a, int b, int c)
        {
            // 豹子：三数相同；对子：两数相同；顺子：三数连续；否则半杂
            if (a == b && b == c) return "豹子";
            if (a == b || a == c || b == c) return "对子";

            var arr = new[] { a, b, c }.OrderBy(x => x).ToArray();
            if (arr[0] + 1 == arr[1] && arr[1] + 1 == arr[2]) return "顺子";
            return "半杂";
        }

        private static string GetBaoShunDuiMixed(int a, int b, int c)
        {
            var t = GetBaoShunDui(a, b, c);
            switch (t)
            {
                case "豹子": return "B";
                case "顺子": return "S";
                case "对子": return "D";
                default: return "半杂";
            }
        }

        private static bool IsEdge(int sum)
        {
            // 常见边：0-5 与 22-27；其余为中
            return sum <= 5 || sum >= 22;
        }

        private static string GetDragonTigerLeopardCode(int sum)
        {
            var cfg = ConfigService.Instance.Config;
            // Config stores sum numbers in strings like "00, 03, ..."
            if (IsInNumberSet(cfg.DragonNumbers, sum)) return "L";
            if (IsInNumberSet(cfg.TigerNumbers, sum)) return "H";
            if (IsInNumberSet(cfg.LeopardNumbers, sum)) return "B";
            return "";
        }

        private static string GetDragonTigerLeopardMixed(int sum)
        {
            var code = GetDragonTigerLeopardCode(sum);
            if (code == "B") return "豹";
            return code;
        }

        private static string ToDragonTigerLeopardChinese(string code)
        {
            switch (code)
            {
                case "L": return "龙";
                case "H": return "虎";
                case "B": return "豹";
                default: return "";
            }
        }

        private static bool IsInNumberSet(string csv, int sum)
        {
            if (string.IsNullOrWhiteSpace(csv)) return false;
            var needle = sum.ToString("00");
            var parts = csv.Split(new[] { ',', '，' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .Where(p => p.Length > 0)
                .ToList();
            return parts.Any(p => string.Equals(p, needle, StringComparison.OrdinalIgnoreCase));
        }

        private static List<string> SplitAttacks(string raw)
        {
            var list = new List<string>();
            if (string.IsNullOrWhiteSpace(raw)) return list;

            // Common separators: | , ， ; ； newline spaces
            var parts = raw
                .Replace("\r", " ")
                .Replace("\n", " ")
                .Split(new[] { '|', ',', '，', ';', '；' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .Where(p => p.Length > 0)
                .ToList();

            if (parts.Count <= 1)
            {
                // Fallback: split by whitespace to get first 1-3 chunks
                var ws = raw.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(p => p.Trim())
                    .Where(p => p.Length > 0)
                    .ToList();
                if (ws.Count > 0) parts = ws;
            }

            foreach (var p in parts)
            {
                if (list.Count >= 3) break;
                list.Add(p);
            }
            return list;
        }
    }
}


