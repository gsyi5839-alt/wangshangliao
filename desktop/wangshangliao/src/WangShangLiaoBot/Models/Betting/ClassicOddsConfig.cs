namespace WangShangLiaoBot.Models.Betting
{
    /// <summary>
    /// Classic play odds config used by the settlement engine.
    /// These values must come from user settings (no hard-coded odds in code).
    /// </summary>
    public sealed class ClassicOddsConfig
    {
        /// <summary>大小单双赔率 (e.g., 1.8)</summary>
        public decimal DxdsOdds { get; set; }

        /// <summary>大单小双赔率</summary>
        public decimal BigOddSmallEvenOdds { get; set; }

        /// <summary>大双小单赔率</summary>
        public decimal BigEvenSmallOddOdds { get; set; }
    }
}


