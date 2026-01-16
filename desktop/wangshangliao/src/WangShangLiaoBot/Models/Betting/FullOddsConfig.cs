using System.Collections.Generic;

namespace WangShangLiaoBot.Models.Betting
{
    /// <summary>
    /// 完整赔率配置 - 基于招财狗软件的赔率系统
    /// Full odds configuration based on ZCG (招财狗) betting system
    /// </summary>
    public sealed class FullOddsConfig
    {
        #region 基础玩法赔率

        /// <summary>大小单双赔率 (默认1.8)</summary>
        public decimal SingleBetOdds { get; set; } = 1.8m;

        /// <summary>组合赔率 - 大单/大双/小单/小双 (默认3.8)</summary>
        public decimal CombinationOdds { get; set; } = 3.8m;

        /// <summary>大单小双赔率 (默认5)</summary>
        public decimal BigOddSmallEvenOdds { get; set; } = 5m;

        /// <summary>大双小单赔率 (默认5)</summary>
        public decimal BigEvenSmallOddOdds { get; set; } = 5m;

        /// <summary>极大极小赔率 (默认11)</summary>
        public decimal ExtremeOdds { get; set; } = 11m;

        #endregion

        #region 特殊玩法赔率

        /// <summary>对子赔率 (默认2)</summary>
        public decimal PairOdds { get; set; } = 2m;

        /// <summary>顺子赔率 (默认11)</summary>
        public decimal StraightOdds { get; set; } = 11m;

        /// <summary>半顺赔率 (默认1.7)</summary>
        public decimal HalfStraightOdds { get; set; } = 1.7m;

        /// <summary>豹子赔率 (默认59)</summary>
        public decimal LeopardOdds { get; set; } = 59m;

        /// <summary>杂赔率 (默认2.2)</summary>
        public decimal MixedOdds { get; set; } = 2.2m;

        #endregion

        #region 龙虎玩法赔率

        /// <summary>龙虎赔率 (默认1.92)</summary>
        public decimal DragonTigerOdds { get; set; } = 1.92m;

        /// <summary>和赔率 (龙虎和)</summary>
        public decimal DragonTigerTieOdds { get; set; } = 0m;

        #endregion

        #region 尾球玩法赔率

        /// <summary>尾单注赔率 (默认1.4)</summary>
        public decimal TailSingleOdds { get; set; } = 1.4m;

        /// <summary>尾组合赔率 (默认3.8)</summary>
        public decimal TailCombinationOdds { get; set; } = 3.8m;

        /// <summary>尾数字赔率 (默认5)</summary>
        public decimal TailDigitOdds { get; set; } = 5m;

        /// <summary>尾09赔率 (-1表示禁止)</summary>
        public decimal Tail09SingleOdds { get; set; } = -1m;

        /// <summary>尾09组合赔率 (-1表示禁止)</summary>
        public decimal Tail09CombinationOdds { get; set; } = -1m;

        #endregion

        #region 边中玩法赔率

        /// <summary>大边赔率</summary>
        public decimal BigEdgeOdds { get; set; } = 0m;

        /// <summary>小边赔率</summary>
        public decimal SmallEdgeOdds { get; set; } = 0m;

        /// <summary>边赔率</summary>
        public decimal EdgeOdds { get; set; } = 0m;

        /// <summary>中赔率</summary>
        public decimal MiddleOdds { get; set; } = 0m;

        #endregion

        #region 二七玩法赔率

        /// <summary>二七单注赔率 (默认1.7)</summary>
        public decimal TwoSevenSingleOdds { get; set; } = 1.7m;

        /// <summary>二七组合赔率 (默认4.9)</summary>
        public decimal TwoSevenCombinationOdds { get; set; } = 4.9m;

        #endregion

        #region 数字赔率 (0-27)

        /// <summary>
        /// 数字赔率表 (0-27)
        /// 默认赔率: 0=665, 1=99, 2=49, 3=39, 4=29, 5=19, 6=16, 7=15,
        /// 8=14, 9=14, 10=13, 11=12, 12=11, 13=10, 14=10, 15=11, 16=12,
        /// 17=13, 18=14, 19=14, 20=15, 21=16, 22=19, 23=29, 24=39,
        /// 25=49, 26=99, 27=665
        /// </summary>
        public Dictionary<int, decimal> DigitOdds { get; set; } = new Dictionary<int, decimal>
        {
            { 0, 665m }, { 1, 99m }, { 2, 49m }, { 3, 39m }, { 4, 29m },
            { 5, 19m }, { 6, 16m }, { 7, 15m }, { 8, 14m }, { 9, 14m },
            { 10, 13m }, { 11, 12m }, { 12, 11m }, { 13, 10m }, { 14, 10m },
            { 15, 11m }, { 16, 12m }, { 17, 13m }, { 18, 14m }, { 19, 14m },
            { 20, 15m }, { 21, 16m }, { 22, 19m }, { 23, 29m }, { 24, 39m },
            { 25, 49m }, { 26, 99m }, { 27, 665m }
        };

        /// <summary>默认数字赔率 (当DigitOdds未配置时使用)</summary>
        public decimal DefaultDigitOdds { get; set; } = 10m;

        #endregion

        #region 下注限额

        /// <summary>单注下限</summary>
        public decimal SingleMinBet { get; set; } = 20m;

        /// <summary>单注上限</summary>
        public decimal SingleMaxBet { get; set; } = 50000m;

        /// <summary>组合下限</summary>
        public decimal CombinationMinBet { get; set; } = 20m;

        /// <summary>组合上限</summary>
        public decimal CombinationMaxBet { get; set; } = 30000m;

        /// <summary>数字下限</summary>
        public decimal DigitMinBet { get; set; } = 20m;

        /// <summary>数字上限</summary>
        public decimal DigitMaxBet { get; set; } = 20000m;

        /// <summary>龙虎下限</summary>
        public decimal DragonTigerMinBet { get; set; } = 20m;

        /// <summary>龙虎上限</summary>
        public decimal DragonTigerMaxBet { get; set; } = 10000m;

        /// <summary>对子下限</summary>
        public decimal PairMinBet { get; set; } = 20m;

        /// <summary>对子上限</summary>
        public decimal PairMaxBet { get; set; } = 10000m;

        /// <summary>顺子下限</summary>
        public decimal StraightMinBet { get; set; } = 20m;

        /// <summary>顺子上限</summary>
        public decimal StraightMaxBet { get; set; } = 10000m;

        /// <summary>豹子下限</summary>
        public decimal LeopardMinBet { get; set; } = 20m;

        /// <summary>豹子上限</summary>
        public decimal LeopardMaxBet { get; set; } = 2000m;

        /// <summary>总额上限</summary>
        public decimal TotalMaxBet { get; set; } = 60000m;

        #endregion

        #region 极值设置

        /// <summary>极大范围起始值 (默认22)</summary>
        public int ExtremeHighStart { get; set; } = 22;

        /// <summary>极大范围结束值 (默认27)</summary>
        public int ExtremeHighEnd { get; set; } = 27;

        /// <summary>极小范围起始值 (默认0)</summary>
        public int ExtremeLowStart { get; set; } = 0;

        /// <summary>极小范围结束值 (默认5)</summary>
        public int ExtremeLowEnd { get; set; } = 5;

        #endregion

        #region 龙虎豹自定义开奖号码

        /// <summary>龙对应的开奖号码 (默认: 00,03,06,09,12,15,18,21,24,27)</summary>
        public string DragonNumbers { get; set; } = "00,03,06,09,12,15,18,21,24,27";

        /// <summary>虎对应的开奖号码 (默认: 01,04,07,10,13,16,19,22,25)</summary>
        public string TigerNumbers { get; set; } = "01,04,07,10,13,16,19,22,25";

        /// <summary>豹对应的开奖号码 (默认: 02,05,08,11,14,17,20,23,26)</summary>
        public string LeopardDTNumbers { get; set; } = "02,05,08,11,14,17,20,23,26";

        #endregion

        /// <summary>
        /// 获取指定数字的赔率
        /// </summary>
        public decimal GetDigitOdds(int digit)
        {
            if (digit < 0 || digit > 27) return 0m;
            return DigitOdds.TryGetValue(digit, out var odds) ? odds : DefaultDigitOdds;
        }

        /// <summary>
        /// 创建默认配置
        /// </summary>
        public static FullOddsConfig CreateDefault()
        {
            return new FullOddsConfig();
        }
    }
}
