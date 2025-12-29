using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using WangShangLiaoBot.Models;
using WangShangLiaoBot.Models.Betting;

namespace WangShangLiaoBot.Services.Betting
{
    /// <summary>
    /// Capture bet messages from group chat and persist as real bet ledgers.
    /// Source: ChatService.OnMessageReceived (hook polling in CDP mode).
    /// </summary>
    public sealed class BetLedgerService
    {
        private static BetLedgerService _instance;
        public static BetLedgerService Instance => _instance ?? (_instance = new BetLedgerService());

        private BetLedgerService() { }

        public bool IsRunning { get; private set; }

        public void Start()
        {
            if (IsRunning) return;
            ChatService.Instance.OnMessageReceived += HandleMessage;
            IsRunning = true;
        }

        public void Stop()
        {
            if (!IsRunning) return;
            ChatService.Instance.OnMessageReceived -= HandleMessage;
            IsRunning = false;
        }

        private void HandleMessage(ChatMessage msg)
        {
            try
            {
                if (!IsRunning) return;
                if (msg == null) return;
                if (msg.IsSelf) return;
                if (!msg.IsGroupMessage) return; // bets must come from group chat
                if (msg.Type != MessageType.Text) return;

                if (!BetMessageParser.TryParse(msg.Content, out var items, out var total, out var normalized))
                    return;

                // Determine period: default to next period for pre-bet
                var period = LotteryService.Instance.NextPeriod;
                if (string.IsNullOrEmpty(period))
                    period = LotteryService.Instance.CurrentPeriod ?? "";
                if (string.IsNullOrEmpty(period)) return;

                var player = DataService.Instance.GetOrCreatePlayer(msg.SenderId, msg.SenderName);
                var record = new BetRecord
                {
                    Time = msg.Time == default ? DateTime.Now : msg.Time,
                    Period = period,
                    TeamId = msg.GroupId ?? "",
                    PlayerId = msg.SenderId ?? "",
                    PlayerNick = msg.SenderName ?? "",
                    RawText = msg.Content ?? "",
                    NormalizedText = normalized ?? "",
                    Items = items ?? new List<BetItem>(),
                    TotalAmount = total,
                    ScoreBefore = player?.Score ?? 0m
                };

                AppendBetRecord(record);

                // Track stake for today's stats (no hard-coded values; derived from captured bet amount)
                var dateKey = DateTime.Today.ToString("yyyy-MM-dd");
                var stake = DataService.Instance.GetDailyDecimal(dateKey, record.PlayerId, "Stake", 0m);
                DataService.Instance.SetDailyDecimal(dateKey, record.PlayerId, "Stake", stake + record.TotalAmount);
            }
            catch
            {
                // swallow to avoid breaking message loop
            }
        }

        /// <summary>
        /// Get bet file path for a day+period.
        /// </summary>
        public string GetBetFile(DateTime day, string teamId, string period)
        {
            var safeTeam = string.IsNullOrWhiteSpace(teamId) ? "unknown-team" : teamId.Trim();
            var dir = Path.Combine(DataService.Instance.DatabaseDir, "Bets", day.ToString("yyyy-MM-dd"), safeTeam);
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, $"bets-{period}.txt");
        }

        /// <summary>
        /// Append one bet record to ledger file (tab separated).
        /// </summary>
        private void AppendBetRecord(BetRecord r)
        {
            var file = GetBetFile(DateTime.Today, r.TeamId, r.Period);

            // Format (TSV):
            // time  period  teamId  playerId  nick  scoreBefore  total  normalized  raw
            var line = string.Join("\t", new[]
            {
                r.Time.ToString("HH:mm:ss"),
                r.Period ?? "",
                r.TeamId ?? "",
                r.PlayerId ?? "",
                (r.PlayerNick ?? "").Replace("\t"," "),
                r.ScoreBefore.ToString(CultureInfo.InvariantCulture),
                r.TotalAmount.ToString(CultureInfo.InvariantCulture),
                (r.NormalizedText ?? "").Replace("\t"," "),
                (r.RawText ?? "").Replace("\t"," ")
            });

            File.AppendAllText(file, line + Environment.NewLine, Encoding.UTF8);
        }

        /// <summary>
        /// Read bet records for a day+period.
        /// This is used by settlement and template variables like [下注核对].
        /// </summary>
        public List<BetRecord> ReadBets(DateTime day, string period)
        {
            // Backward compatible: read all groups for the period (rarely used by templates now).
            return ReadBets(day, teamId: null, period: period);
        }

        /// <summary>
        /// Read bet records for a day+teamId+period.
        /// </summary>
        public List<BetRecord> ReadBets(DateTime day, string teamId, string period)
        {
            var list = new List<BetRecord>();
            try
            {
                var baseDir = Path.Combine(DataService.Instance.DatabaseDir, "Bets", day.ToString("yyyy-MM-dd"));
                if (!Directory.Exists(baseDir)) return list;

                // If teamId not specified, read from all team folders
                var teamDirs = string.IsNullOrWhiteSpace(teamId)
                    ? Directory.GetDirectories(baseDir)
                    : new[] { Path.Combine(baseDir, teamId.Trim()) };

                foreach (var dir in teamDirs)
                {
                    var file = Path.Combine(dir, $"bets-{period}.txt");
                    if (!File.Exists(file)) continue;

                    var lines = File.ReadAllLines(file, Encoding.UTF8);
                    foreach (var line in lines.Where(l => !string.IsNullOrWhiteSpace(l)))
                    {
                        var parts = line.Split('\t');
                        if (parts.Length < 9) continue;
                        decimal.TryParse(parts[5], NumberStyles.Any, CultureInfo.InvariantCulture, out var scoreBefore);
                        decimal.TryParse(parts[6], NumberStyles.Any, CultureInfo.InvariantCulture, out var total);
                        list.Add(new BetRecord
                        {
                            Time = DateTime.Today,
                            Period = parts[1],
                            TeamId = parts[2],
                            PlayerId = parts[3],
                            PlayerNick = parts[4],
                            ScoreBefore = scoreBefore,
                            TotalAmount = total,
                            NormalizedText = parts[7],
                            RawText = parts[8]
                        });
                    }
                }
            }
            catch { }
            return list;
        }
    }
}


