using System;
using System.Collections.Generic;

namespace WangShangLiaoBot.Models.Betting
{
    /// <summary>
    /// A single parsed bet item.
    /// The project currently focuses on Canada28 and classic bet tokens like XD/DS.
    /// </summary>
    public sealed class BetItem
    {
        /// <summary>Bet kind (e.g., DXDS combo, Big/Small, Odd/Even)</summary>
        public BetKind Kind { get; set; }

        /// <summary>Normalized code (e.g., XD/XS/DD/DS, 大/小/单/双)</summary>
        public string Code { get; set; }

        /// <summary>Bet amount</summary>
        public decimal Amount { get; set; }
    }

    /// <summary>
    /// A bet record captured from group chat for a specific lottery period.
    /// This is the "real data structure" produced by group messages.
    /// </summary>
    public sealed class BetRecord
    {
        public DateTime Time { get; set; }

        /// <summary>Lottery period the bet is assigned to (usually nextPeriod)</summary>
        public string Period { get; set; }

        /// <summary>NIM teamId when IsGroupMessage=true</summary>
        public string TeamId { get; set; }

        public string PlayerId { get; set; }
        public string PlayerNick { get; set; }

        /// <summary>Raw chat text (original message)</summary>
        public string RawText { get; set; }

        /// <summary>Normalized bet text (used for [下注核对2] style display)</summary>
        public string NormalizedText { get; set; }

        public List<BetItem> Items { get; set; } = new List<BetItem>();

        /// <summary>Total stake across all items</summary>
        public decimal TotalAmount { get; set; }

        /// <summary>Snapshot of player's score at capture time (for [下注核对] semantics)</summary>
        public decimal ScoreBefore { get; set; }
    }

    /// <summary>
    /// Settlement result for one player within a period.
    /// </summary>
    public sealed class SettlementEntry
    {
        public string Period { get; set; }
        public string PlayerId { get; set; }
        public string PlayerNick { get; set; }
        public decimal Stake { get; set; }
        public decimal Profit { get; set; }
        public decimal ScoreBefore { get; set; }
        public decimal ScoreAfter { get; set; }

        /// <summary>Human readable summary of matched items</summary>
        public string Detail { get; set; }
    }

    public enum BetKind
    {
        /// <summary>Classic Canada28 DXDS combo (XD/XS/DD/DS)</summary>
        Dxds,
        /// <summary>Big/Small only</summary>
        BigSmall,
        /// <summary>Odd/Even only</summary>
        OddEven
    }
}


