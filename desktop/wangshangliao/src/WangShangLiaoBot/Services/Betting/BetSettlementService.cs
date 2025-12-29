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
    /// Settle bets for a lottery period when LotteryService publishes a result.
    /// - Reads bet ledger from BetLedgerService
    /// - Calculates profit/loss using odds configured in OddsConfigService
    /// - Updates player scores via DataService.SavePlayer
    /// - Writes settlement output used by template tokens like [中奖玩家]
    /// </summary>
    public sealed class BetSettlementService
    {
        private static BetSettlementService _instance;
        public static BetSettlementService Instance => _instance ?? (_instance = new BetSettlementService());

        private BetSettlementService() { }

        public bool IsRunning { get; private set; }

        public void Start()
        {
            if (IsRunning) return;
            LotteryService.Instance.OnResultUpdated += OnLotteryResult;
            IsRunning = true;
        }

        public void Stop()
        {
            if (!IsRunning) return;
            LotteryService.Instance.OnResultUpdated -= OnLotteryResult;
            IsRunning = false;
        }

        private void OnLotteryResult(LotteryResult result)
        {
            try
            {
                if (!IsRunning) return;
                if (result == null || string.IsNullOrEmpty(result.Period)) return;

                var odds = OddsConfigService.Instance.LoadClassicOdds();
                if (odds == null)
                {
                    // No odds configured -> do not settle to avoid hidden hard-coded values
                    return;
                }

                var day = DateTime.Today;
                var period = result.Period;

                var sum = result.Sum;
                var dxds = GetDxdsCode(sum);

                // Settle per group (teamId) to avoid mixing multiple groups when customers choose group dynamically.
                var all = BetLedgerService.Instance.ReadBets(day, teamId: null, period: period);
                if (all.Count == 0) return;

                foreach (var team in all.GroupBy(b => b.TeamId ?? "unknown-team"))
                {
                    var teamId = team.Key;
                    var bets = team.ToList();
                    if (bets.Count == 0) continue;

                    var settlements = new List<SettlementEntry>();

                    foreach (var g in bets.GroupBy(b => b.PlayerId))
                    {
                        var playerId = g.Key;
                        var player = DataService.Instance.GetOrCreatePlayer(playerId, g.First().PlayerNick);
                        var scoreBefore = player.Score;

                        decimal stake = 0m;
                        decimal profit = 0m;
                        var detail = new StringBuilder();

                        foreach (var b in g)
                        {
                            stake += b.TotalAmount;

                            // Re-parse from raw text to structured items for settlement
                            if (!BetMessageParser.TryParse(b.RawText, out var items, out var t, out var norm))
                                continue;

                            foreach (var it in items)
                            {
                                var p = SettleItem(it, sum, dxds, odds);
                                profit += p;
                                if (detail.Length > 0) detail.Append(" ");
                                detail.Append($"{it.Code}{it.Amount}:{p.ToString(CultureInfo.InvariantCulture)}");
                            }
                        }

                        var scoreAfter = scoreBefore + profit;
                        player.Score = scoreAfter;
                        DataService.Instance.SavePlayer(player);

                        settlements.Add(new SettlementEntry
                        {
                            Period = period,
                            PlayerId = playerId,
                            PlayerNick = player.Nickname ?? g.First().PlayerNick,
                            Stake = stake,
                            Profit = profit,
                            ScoreBefore = scoreBefore,
                            ScoreAfter = scoreAfter,
                            Detail = detail.ToString()
                        });
                    }

                    WriteSettlement(day, teamId, period, settlements, sum, dxds);
                }
            }
            catch
            {
                // avoid breaking lottery loop
            }
        }

        private static decimal SettleItem(BetItem it, int sum, string dxds, ClassicOddsConfig odds)
        {
            if (it == null || it.Amount <= 0) return 0m;

            // Only settle classic DXDS for now (real odds exist in ClassicPlay settings)
            if (it.Kind == BetKind.Dxds)
            {
                var win = string.Equals(it.Code, dxds, StringComparison.OrdinalIgnoreCase);
                return win ? it.Amount * odds.DxdsOdds : -it.Amount;
            }

            // Optional: allow Big/Small or Odd/Even if user used Chinese single tokens
            if (it.Kind == BetKind.BigSmall)
            {
                var bs = sum >= 14 ? "大" : "小";
                var win = string.Equals(it.Code, bs, StringComparison.OrdinalIgnoreCase);
                return win ? it.Amount * odds.DxdsOdds : -it.Amount;
            }

            if (it.Kind == BetKind.OddEven)
            {
                var oe = (sum % 2 == 0) ? "双" : "单";
                var win = string.Equals(it.Code, oe, StringComparison.OrdinalIgnoreCase);
                return win ? it.Amount * odds.DxdsOdds : -it.Amount;
            }

            return 0m;
        }

        private string GetSettlementFile(DateTime day, string teamId, string period)
        {
            var safeTeam = string.IsNullOrWhiteSpace(teamId) ? "unknown-team" : teamId.Trim();
            var dir = Path.Combine(DataService.Instance.DatabaseDir, "Bets", day.ToString("yyyy-MM-dd"), safeTeam);
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, $"settle-{period}.txt");
        }

        private void WriteSettlement(DateTime day, string teamId, string period, List<SettlementEntry> list, int sum, string dxds)
        {
            try
            {
                var file = GetSettlementFile(day, teamId, period);
                var sb = new StringBuilder();
                sb.AppendLine($"期号{period} 号码合计={sum} [{dxds}]");

                foreach (var s in list.OrderByDescending(x => x.Profit))
                {
                    sb.AppendLine($"{s.PlayerNick}({Short4(s.PlayerId)}) 盈亏:{s.Profit} 下注:{s.Stake} {s.ScoreBefore}->{s.ScoreAfter}");
                }

                File.WriteAllText(file, sb.ToString(), Encoding.UTF8);
            }
            catch { }
        }

        public string ReadWinnersText(DateTime day, string teamId, string period)
        {
            try
            {
                var file = GetSettlementFile(day, teamId, period);
                if (!File.Exists(file)) return "";
                return File.ReadAllText(file, Encoding.UTF8);
            }
            catch
            {
                return "";
            }
        }

        private static string Short4(string id)
        {
            if (string.IsNullOrEmpty(id)) return "";
            return id.Length >= 4 ? id.Substring(0, 4) : id;
        }

        private static string GetDxdsCode(int sum)
        {
            var bigSmall = sum >= 14 ? "D" : "X";
            var oddEven = (sum % 2 == 0) ? "S" : "D";
            return bigSmall + oddEven;
        }
    }
}


