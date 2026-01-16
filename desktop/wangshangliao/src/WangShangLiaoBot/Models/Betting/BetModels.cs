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
        
        /// <summary>是否为二七玩法下注</summary>
        public bool IsTwoSeven { get; set; }
        
        /// <summary>赔率</summary>
        public decimal Odds { get; set; } = 1.0m;
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
        
        // ========== 兼容属性（从首个 Item 获取）==========
        /// <summary>首个下注项的种类（兼容旧代码）</summary>
        public BetKind Kind => Items?.Count > 0 ? Items[0].Kind : BetKind.Unknown;
        /// <summary>首个下注项的代码（兼容旧代码）</summary>
        public string Code => Items?.Count > 0 ? Items[0].Code : "";
        /// <summary>首个下注项的金额（兼容旧代码）</summary>
        public decimal Amount => Items?.Count > 0 ? Items[0].Amount : 0;
        /// <summary>赔率（兼容旧代码，默认1.0）</summary>
        public decimal Odds { get; set; } = 1.0m;
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

    /// <summary>
    /// Bet kind enumeration for all supported bet types
    /// </summary>
    public enum BetKind
    {
        /// <summary>Unknown bet type</summary>
        Unknown,
        /// <summary>Classic Canada28 DXDS combo (XD/XS/DD/DS)</summary>
        Dxds,
        /// <summary>Big/Small only (大/小)</summary>
        BigSmall,
        /// <summary>Odd/Even only (单/双)</summary>
        OddEven,
        /// <summary>Pair (对子)</summary>
        Pair,
        /// <summary>Combination (组合)</summary>
        Combination,
        /// <summary>Straight (顺子)</summary>
        Straight,
        /// <summary>Leopard (豹子)</summary>
        Leopard,
        /// <summary>Digit bet 0-27 (数字)</summary>
        Digit,
        /// <summary>Extreme (极数/极大/极小)</summary>
        Extreme,
        /// <summary>Half straight (半顺)</summary>
        HalfStraight,
        /// <summary>Sum (和/合)</summary>
        Sum,
        /// <summary>Middle (中)</summary>
        Middle,
        /// <summary>Dragon/Tiger (龙虎)</summary>
        DragonTiger,
        /// <summary>Mixed (杂)</summary>
        Mixed,
        /// <summary>Three Army (三军)</summary>
        ThreeArmy,
        /// <summary>Edge bet (边/大边/小边)</summary>
        Edge,
        /// <summary>Tail single bet (尾单注/尾大/尾小/尾单/尾双)</summary>
        TailSingle,
        /// <summary>Tail combination (尾组合)</summary>
        TailCombination,
        /// <summary>Tail digit (尾数字)</summary>
        TailDigit
    }
}


