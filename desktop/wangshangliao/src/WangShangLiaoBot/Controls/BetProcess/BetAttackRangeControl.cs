using System;
using System.Windows.Forms;
using WangShangLiaoBot.Services;

namespace WangShangLiaoBot.Controls.BetProcess
{
    /// <summary>
    /// Bet attack range settings control - Contains bet limit configurations
    /// </summary>
    public partial class BetAttackRangeControl : UserControl
    {
        public BetAttackRangeControl()
        {
            InitializeComponent();
            btnSaveSettings.Click += BtnSaveSettings_Click;
        }

        /// <summary>
        /// Load settings from config
        /// </summary>
        public void LoadSettings()
        {
            var settings = BetAttackRangeSettingsService.Instance;

            // Row 1: 单注, 对子, 尾单注, 大边
            nudSingleBetMin.Value = settings.SingleBetMin;
            nudSingleBetMax.Value = settings.SingleBetMax;
            nudPairMin.Value = settings.PairMin;
            nudPairMax.Value = settings.PairMax;
            nudTailSingleMin.Value = settings.TailSingleMin;
            nudTailSingleMax.Value = settings.TailSingleMax;
            nudBigEdgeMin.Value = settings.BigEdgeMin;
            nudBigEdgeMax.Value = settings.BigEdgeMax;

            // Row 2: 组合, 顺子, 尾组合, 小边
            nudCombinationMin.Value = settings.CombinationMin;
            nudCombinationMax.Value = settings.CombinationMax;
            nudStraightMin.Value = settings.StraightMin;
            nudStraightMax.Value = settings.StraightMax;
            nudTailCombinationMin.Value = settings.TailCombinationMin;
            nudTailCombinationMax.Value = settings.TailCombinationMax;
            nudSmallEdgeMin.Value = settings.SmallEdgeMin;
            nudSmallEdgeMax.Value = settings.SmallEdgeMax;

            // Row 3: 数字, 豹子, 尾数字, 边
            nudDigitMin.Value = settings.DigitMin;
            nudDigitMax.Value = settings.DigitMax;
            nudLeopardMin.Value = settings.LeopardMin;
            nudLeopardMax.Value = settings.LeopardMax;
            nudTailDigitMin.Value = settings.TailDigitMin;
            nudTailDigitMax.Value = settings.TailDigitMax;
            nudEdgeMin.Value = settings.EdgeMin;
            nudEdgeMax.Value = settings.EdgeMax;

            // Row 4: 极数, 半顺, 和, 中
            nudExtremeMin.Value = settings.ExtremeMin;
            nudExtremeMax.Value = settings.ExtremeMax;
            nudHalfStraightMin.Value = settings.HalfStraightMin;
            nudHalfStraightMax.Value = settings.HalfStraightMax;
            nudSumMin.Value = settings.SumMin;
            nudSumMax.Value = settings.SumMax;
            nudMiddleMin.Value = settings.MiddleMin;
            nudMiddleMax.Value = settings.MiddleMax;

            // Row 5: 龙虎, 杂, 三军, 总额封顶
            nudDragonTigerMin.Value = settings.DragonTigerMin;
            nudDragonTigerMax.Value = settings.DragonTigerMax;
            nudMixedMin.Value = settings.MixedMin;
            nudMixedMax.Value = settings.MixedMax;
            nudThreeArmyMin.Value = settings.ThreeArmyMin;
            nudThreeArmyMax.Value = settings.ThreeArmyMax;
            nudTotalLimit.Value = settings.TotalLimit;

            // Over range hint
            txtOverRangeHint.Text = settings.OverRangeHintMsg;
        }

        /// <summary>
        /// Save settings to config
        /// </summary>
        public void SaveSettings()
        {
            var settings = BetAttackRangeSettingsService.Instance;

            // Row 1: 单注, 对子, 尾单注, 大边
            settings.SingleBetMin = nudSingleBetMin.Value;
            settings.SingleBetMax = nudSingleBetMax.Value;
            settings.PairMin = nudPairMin.Value;
            settings.PairMax = nudPairMax.Value;
            settings.TailSingleMin = nudTailSingleMin.Value;
            settings.TailSingleMax = nudTailSingleMax.Value;
            settings.BigEdgeMin = nudBigEdgeMin.Value;
            settings.BigEdgeMax = nudBigEdgeMax.Value;

            // Row 2: 组合, 顺子, 尾组合, 小边
            settings.CombinationMin = nudCombinationMin.Value;
            settings.CombinationMax = nudCombinationMax.Value;
            settings.StraightMin = nudStraightMin.Value;
            settings.StraightMax = nudStraightMax.Value;
            settings.TailCombinationMin = nudTailCombinationMin.Value;
            settings.TailCombinationMax = nudTailCombinationMax.Value;
            settings.SmallEdgeMin = nudSmallEdgeMin.Value;
            settings.SmallEdgeMax = nudSmallEdgeMax.Value;

            // Row 3: 数字, 豹子, 尾数字, 边
            settings.DigitMin = nudDigitMin.Value;
            settings.DigitMax = nudDigitMax.Value;
            settings.LeopardMin = nudLeopardMin.Value;
            settings.LeopardMax = nudLeopardMax.Value;
            settings.TailDigitMin = nudTailDigitMin.Value;
            settings.TailDigitMax = nudTailDigitMax.Value;
            settings.EdgeMin = nudEdgeMin.Value;
            settings.EdgeMax = nudEdgeMax.Value;

            // Row 4: 极数, 半顺, 和, 中
            settings.ExtremeMin = nudExtremeMin.Value;
            settings.ExtremeMax = nudExtremeMax.Value;
            settings.HalfStraightMin = nudHalfStraightMin.Value;
            settings.HalfStraightMax = nudHalfStraightMax.Value;
            settings.SumMin = nudSumMin.Value;
            settings.SumMax = nudSumMax.Value;
            settings.MiddleMin = nudMiddleMin.Value;
            settings.MiddleMax = nudMiddleMax.Value;

            // Row 5: 龙虎, 杂, 三军, 总额封顶
            settings.DragonTigerMin = nudDragonTigerMin.Value;
            settings.DragonTigerMax = nudDragonTigerMax.Value;
            settings.MixedMin = nudMixedMin.Value;
            settings.MixedMax = nudMixedMax.Value;
            settings.ThreeArmyMin = nudThreeArmyMin.Value;
            settings.ThreeArmyMax = nudThreeArmyMax.Value;
            settings.TotalLimit = nudTotalLimit.Value;

            // Over range hint
            settings.OverRangeHintMsg = txtOverRangeHint.Text;

            // Save to file
            settings.SaveToFile();
        }

        private void BtnSaveSettings_Click(object sender, EventArgs e)
        {
            SaveSettings();
            MessageBox.Show("攻击范围设置已保存", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
