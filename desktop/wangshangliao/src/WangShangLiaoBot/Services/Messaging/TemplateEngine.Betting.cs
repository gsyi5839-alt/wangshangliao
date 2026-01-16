using System;
using System.Globalization;
using System.Linq;
using System.Text;
using WangShangLiaoBot.Services.Betting;

namespace WangShangLiaoBot.Services
{
    /// <summary>
    /// Template engine helpers (betting & settlement).
    /// Split from TemplateEngine.cs to keep each file under the line limit.
    /// </summary>
    public static partial class TemplateEngine
    {
        /// <summary>
        /// Render bet check list for the current group (teamId) and current betting period.
        /// This is the real implementation for tokens: [下注核对] / [下注核对2].
        /// Supports VariableBetLater setting for different output formats.
        /// </summary>
        private static string RenderBetCheck(DateTime day, string teamId, bool chineseBet)
        {
            if (string.IsNullOrWhiteSpace(teamId)) return "";

            // For pre-bet checking, bets are recorded under NextPeriod (default in BetLedgerService).
            var period = LotteryService.Instance.NextPeriod;
            if (string.IsNullOrEmpty(period))
                period = LotteryService.Instance.CurrentPeriod ?? "";
            if (string.IsNullOrEmpty(period)) return "";

            var bets = BetLedgerService.Instance.ReadBets(day, teamId.Trim(), period);
            if (bets == null || bets.Count == 0) return "";

            // Check VariableBetLater setting (变量先注后分)
            var betSettings = BetProcessSettingsService.Instance;
            var betLater = betSettings.VariableBetLater;

            var sb = new StringBuilder();
            foreach (var g in bets.GroupBy(b => b.PlayerId))
            {
                var playerId = g.Key ?? "";
                var nick = g.Select(x => x.PlayerNick).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? "玩家";
                var p4 = playerId.Length >= 4 ? playerId.Substring(0, 4) : playerId;

                // Use first captured score as "before" snapshot.
                var before = g.First().ScoreBefore;
                var player = DataService.Instance.GetPlayer(playerId);
                var after = player != null ? player.Score : before;

                // Merge bet texts
                var betText = string.Join(" ", g.Select(x => chineseBet ? x.NormalizedText : x.RawText)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.Trim()));

                // Format based on VariableBetLater setting:
                // betLater=true:  名字(前4位) [下注内容] 下注前积分 - 下注后积分
                // betLater=false: 名字(前4位) 下注前积分 - [下注内容] _下注后积分
                if (betLater)
                {
                    sb.AppendLine($"{nick}({p4}) [{betText}] {before.ToString(CultureInfo.InvariantCulture)} - {after.ToString(CultureInfo.InvariantCulture)}");
                }
                else
                {
                    sb.AppendLine($"{nick}({p4}) {before.ToString(CultureInfo.InvariantCulture)} - [{betText}] _{after.ToString(CultureInfo.InvariantCulture)}");
                }
            }

            return sb.ToString().TrimEnd();
        }

        /// <summary>
        /// Render winners/settlement text for the current group (teamId) and current period.
        /// Token: [中奖玩家]
        /// </summary>
        private static string RenderWinnersText(DateTime day, string teamId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(teamId)) return "";
                var period = LotteryService.Instance.CurrentPeriod ?? "";
                if (string.IsNullOrEmpty(period)) return "";
                return BetSettlementService.Instance.ReadWinnersText(day, teamId.Trim(), period);
            }
            catch
            {
                return "";
            }
        }
    }
}


