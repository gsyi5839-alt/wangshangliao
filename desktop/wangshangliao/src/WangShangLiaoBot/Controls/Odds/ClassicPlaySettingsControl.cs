using System;
using System.Drawing;
using System.Windows.Forms;
using WangShangLiaoBot.Services;
using WangShangLiaoBot.Models.Betting;
using WangShangLiaoBot.Services.Betting;

namespace WangShangLiaoBot.Controls.Odds
{
    /// <summary>
    /// Classic play settings control - manages classic game odds configuration
    /// Includes: Size/Odd/Even, Leopard/Sequence/Pair, Special code settings
    /// </summary>
    public partial class ClassicPlaySettingsControl : UserControl
    {
        /// <summary>
        /// Constructor - initializes the control
        /// </summary>
        public ClassicPlaySettingsControl()
        {
            InitializeComponent();
            InitializeEvents();
            LoadDefaultData();
            LoadSettings(); // Load saved settings from config
        }
        
        /// <summary>
        /// Initialize event handlers
        /// </summary>
        private void InitializeEvents()
        {
            btnSave.Click += BtnSave_Click;
            btnModifyAdd.Click += BtnModifyAdd_Click;
            btnDeleteSelected.Click += BtnDeleteSelected_Click;
            dgvDigitOdds.SelectionChanged += DgvDigitOdds_SelectionChanged;
        }
        
        /// <summary>
        /// Load default data for digit odds grid
        /// </summary>
        private void LoadDefaultData()
        {
            // Initialize digit odds with default values
            for (int i = 0; i <= 9; i++)
            {
                dgvDigitOdds.Rows.Add(i.ToString(), "9");
            }
        }
        
        /// <summary>
        /// DataGridView selection changed - fill edit fields
        /// </summary>
        private void DgvDigitOdds_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvDigitOdds.SelectedRows.Count > 0)
            {
                var row = dgvDigitOdds.SelectedRows[0];
                txtDigit2.Text = row.Cells["colDigit"].Value?.ToString() ?? "";
                txtOddsVal2.Text = row.Cells["colOddsValue"].Value?.ToString() ?? "";
            }
        }
        
        /// <summary>
        /// Modify/Add button click - update or add digit odds
        /// </summary>
        private void BtnModifyAdd_Click(object sender, EventArgs e)
        {
            var digit = txtDigit2.Text.Trim();
            var odds = txtOddsVal2.Text.Trim();
            
            if (string.IsNullOrEmpty(digit))
            {
                MessageBox.Show("请输入数字！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            // Find existing row
            bool found = false;
            foreach (DataGridViewRow row in dgvDigitOdds.Rows)
            {
                if (row.Cells["colDigit"].Value?.ToString() == digit)
                {
                    row.Cells["colOddsValue"].Value = odds;
                    found = true;
                    break;
                }
            }
            
            if (!found)
            {
                dgvDigitOdds.Rows.Add(digit, odds);
            }
            
            txtDigit2.Clear();
            txtOddsVal2.Clear();
        }
        
        /// <summary>
        /// Delete selected button click - remove selected digit odds
        /// </summary>
        private void BtnDeleteSelected_Click(object sender, EventArgs e)
        {
            if (dgvDigitOdds.SelectedRows.Count > 0)
            {
                var result = MessageBox.Show("确定要删除选中的项目吗？", "确认删除",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                
                if (result == DialogResult.Yes)
                {
                    foreach (DataGridViewRow row in dgvDigitOdds.SelectedRows)
                    {
                        if (!row.IsNewRow)
                        {
                            dgvDigitOdds.Rows.Remove(row);
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show("请先选择要删除的项目！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        
        /// <summary>
        /// Save button click - save all settings
        /// </summary>
        private void BtnSave_Click(object sender, EventArgs e)
        {
            try
            {
                var config = ConfigService.Instance.Config;
                
                // ===== 大小单双超设置 =====
                config.SizeOddEvenNumbers = txtSize1314.Text;
                config.SizeOddEvenSumBet = rbSumBet.Checked;
                config.SizeRow1Min = numRow1Min.Value;
                config.SizeRow1Max = numRow1Max.Value;
                config.SizeRow1Odds = numRow1Odds.Value;
                config.SizeRow2Min = numRow2Min.Value;
                config.SizeRow2Max = numRow2Max.Value;
                config.SizeRow2Odds = numRow2Odds.Value;
                config.SizeRow3Min = numRow3Min.Value;
                config.SizeRow3Max = numRow3Max.Value;
                config.SizeRow3Odds = numRow3Odds.Value;
                config.SizeRow4Min = numRow4Min.Value;
                config.SizeRow4Max = numRow4Max.Value;
                config.SizeRow4Odds = numRow4Odds.Value;
                
                // ===== 大双小单超设置 =====
                config.BigDoubleSmallSingle1Amount = numBDLS1.Value;
                config.BigDoubleSmallSingle1Odds = numBDLS1Odds.Value;
                config.BigDoubleSmallSingle2Amount = numBDLS2.Value;
                config.BigDoubleSmallSingle2Odds = numBDLS2Odds.Value;
                
                // ===== 大单小双超设置 =====
                config.BigSingleSmallDouble1Amount = numBSLD1.Value;
                config.BigSingleSmallDouble1Odds = numBSLD1Odds.Value;
                config.BigSingleSmallDouble2Amount = numBSLD2.Value;
                config.BigSingleSmallDouble2Odds = numBSLD2Odds.Value;
                
                // ===== 豹/顺/对子设置 =====
                config.LeopardSequencePairEnabled = chkLSPSwitch.Checked;
                config.PairReturn = chkPairReturn.Checked;
                config.SequenceReturn = chkSequenceReturn.Checked;
                config.LeopardReturn = chkLeopardReturn.Checked;
                config.LeopardKillAll = chkLeopardKill.Checked;
                config.HalfMixedEnabled = chkHalfMixed.Checked;
                config.Digit09Return = chk09Return.Checked;
                config.Digit1314Return = chk1314Return.Checked;
                config.NumLSPReturn = chkNumReturn.Checked;
                config.Num1314LSPReturn = chkNum1314Return.Checked;
                config.ExtremeLSPReturn = chkExtremeReturn.Checked;
                config.Open1314PairReturn = chk1314PairReturn.Checked;
                config.Seq890910AsSequence = chk890910Sequence.Checked;
                
                // ===== 超无视设置 =====
                config.IgnoreOverSumBet = rbIgnoreSumBet.Checked;
                config.IgnoreOverAmount = numIgnoreAmount.Value;
                config.KillDoubleIgnore = chkKillDoubleIgnore.Checked;
                config.No1314Odds = chkNo1314Odds.Checked;
                
                // ===== 赔率设置 =====
                config.DxdsOdds = numOdds1.Value;
                config.BigOddSmallEvenOdds = numOdds2.Value;
                config.BigEvenSmallOddOdds = numOdds3.Value;
                config.ExtremeOdds = numOdds4.Value;
                config.DigitOdds = numOdds5.Value;
                config.PairOdds = numOdds6.Value;
                config.StraightOdds = numOdds7.Value;
                config.HalfStraightOdds = numOdds8.Value;
                config.LeopardOdds = numOdds9.Value;
                config.MixedOdds = numOdds10.Value;
                config.BigEdgeOdds = numOdds11.Value;
                config.SmallEdgeOdds = numOdds12.Value;
                config.MiddleOdds = numOdds13.Value;
                config.EdgeHistoryOdds = numOdds14.Value;
                
                // ===== 极数设置 =====
                config.ExtremeMax = (int)numExtremeMax1.Value;
                config.ExtremeMaxEnd = (int)numExtremeMax2.Value;
                config.ExtremeMin = (int)numExtremeMin1.Value;
                config.ExtremeMinEnd = (int)numExtremeMin2.Value;
                
                // ===== 单独数字赔率设置 =====
                config.SingleDigitOddsEnabled = chkSingleDigitOdds.Checked;
                config.SingleDigitOddsList = SaveDigitOddsToString();
                
                // ===== 特码格式设置 =====
                config.SpecialCodeChars = txtSpecialChars.Text;
                config.SpecialCodeFirst = rbSpecialFirst.Checked;
                config.SmallAmountAsSpecial = chkSmallAmountSpecial.Checked;
                config.MaxSinglePayout = numMaxPayout.Value;
                config.MaxDigitCount = (int)numMaxDigitCount.Value;
                
                // Save to OddsConfigService for settlement use
                var oddsCfg = new ClassicOddsConfig
                {
                    DxdsOdds = config.DxdsOdds,
                    BigOddSmallEvenOdds = config.BigOddSmallEvenOdds,
                    BigEvenSmallOddOdds = config.BigEvenSmallOddOdds
                };
                OddsConfigService.Instance.SaveClassicOdds(oddsCfg);
                
                // Save config
                ConfigService.Instance.SaveConfig();
                
                MessageBox.Show("经典玩法设置已保存！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        /// <summary>
        /// Save digit odds grid to string format "0:9,1:9,..."
        /// </summary>
        private string SaveDigitOddsToString()
        {
            var parts = new System.Collections.Generic.List<string>();
            foreach (DataGridViewRow row in dgvDigitOdds.Rows)
            {
                if (row.IsNewRow) continue;
                var digit = row.Cells["colDigit"].Value?.ToString() ?? "";
                var odds = row.Cells["colOddsValue"].Value?.ToString() ?? "9";
                if (!string.IsNullOrEmpty(digit))
                {
                    parts.Add($"{digit}:{odds}");
                }
            }
            return string.Join(",", parts);
        }
        
        /// <summary>
        /// Load digit odds from string format "0:9,1:9,..."
        /// </summary>
        private void LoadDigitOddsFromString(string data)
        {
            dgvDigitOdds.Rows.Clear();
            if (string.IsNullOrEmpty(data)) return;
            
            var parts = data.Split(',');
            foreach (var part in parts)
            {
                var kv = part.Split(':');
                if (kv.Length == 2)
                {
                    dgvDigitOdds.Rows.Add(kv[0], kv[1]);
                }
            }
        }
        
        /// <summary>
        /// Load settings from config
        /// </summary>
        public void LoadSettings()
        {
            var config = ConfigService.Instance.Config;
            if (config == null) return;
            
            // ===== 大小单双超设置 =====
            txtSize1314.Text = config.SizeOddEvenNumbers ?? "13|14";
            rbSumBet.Checked = config.SizeOddEvenSumBet;
            rbSingleBet.Checked = !config.SizeOddEvenSumBet;
            numRow1Min.Value = SafeDecimal(config.SizeRow1Min, numRow1Min.Minimum, numRow1Min.Maximum);
            numRow1Max.Value = SafeDecimal(config.SizeRow1Max, numRow1Max.Minimum, numRow1Max.Maximum);
            numRow1Odds.Value = SafeDecimal(config.SizeRow1Odds, numRow1Odds.Minimum, numRow1Odds.Maximum);
            numRow2Min.Value = SafeDecimal(config.SizeRow2Min, numRow2Min.Minimum, numRow2Min.Maximum);
            numRow2Max.Value = SafeDecimal(config.SizeRow2Max, numRow2Max.Minimum, numRow2Max.Maximum);
            numRow2Odds.Value = SafeDecimal(config.SizeRow2Odds, numRow2Odds.Minimum, numRow2Odds.Maximum);
            numRow3Min.Value = SafeDecimal(config.SizeRow3Min, numRow3Min.Minimum, numRow3Min.Maximum);
            numRow3Max.Value = SafeDecimal(config.SizeRow3Max, numRow3Max.Minimum, numRow3Max.Maximum);
            numRow3Odds.Value = SafeDecimal(config.SizeRow3Odds, numRow3Odds.Minimum, numRow3Odds.Maximum);
            numRow4Min.Value = SafeDecimal(config.SizeRow4Min, numRow4Min.Minimum, numRow4Min.Maximum);
            numRow4Max.Value = SafeDecimal(config.SizeRow4Max, numRow4Max.Minimum, numRow4Max.Maximum);
            numRow4Odds.Value = SafeDecimal(config.SizeRow4Odds, numRow4Odds.Minimum, numRow4Odds.Maximum);
            
            // ===== 大双小单超设置 =====
            numBDLS1.Value = SafeDecimal(config.BigDoubleSmallSingle1Amount, numBDLS1.Minimum, numBDLS1.Maximum);
            numBDLS1Odds.Value = SafeDecimal(config.BigDoubleSmallSingle1Odds, numBDLS1Odds.Minimum, numBDLS1Odds.Maximum);
            numBDLS2.Value = SafeDecimal(config.BigDoubleSmallSingle2Amount, numBDLS2.Minimum, numBDLS2.Maximum);
            numBDLS2Odds.Value = SafeDecimal(config.BigDoubleSmallSingle2Odds, numBDLS2Odds.Minimum, numBDLS2Odds.Maximum);
            
            // ===== 大单小双超设置 =====
            numBSLD1.Value = SafeDecimal(config.BigSingleSmallDouble1Amount, numBSLD1.Minimum, numBSLD1.Maximum);
            numBSLD1Odds.Value = SafeDecimal(config.BigSingleSmallDouble1Odds, numBSLD1Odds.Minimum, numBSLD1Odds.Maximum);
            numBSLD2.Value = SafeDecimal(config.BigSingleSmallDouble2Amount, numBSLD2.Minimum, numBSLD2.Maximum);
            numBSLD2Odds.Value = SafeDecimal(config.BigSingleSmallDouble2Odds, numBSLD2Odds.Minimum, numBSLD2Odds.Maximum);
            
            // ===== 豹/顺/对子设置 =====
            chkLSPSwitch.Checked = config.LeopardSequencePairEnabled;
            chkPairReturn.Checked = config.PairReturn;
            chkSequenceReturn.Checked = config.SequenceReturn;
            chkLeopardReturn.Checked = config.LeopardReturn;
            chkLeopardKill.Checked = config.LeopardKillAll;
            chkHalfMixed.Checked = config.HalfMixedEnabled;
            chk09Return.Checked = config.Digit09Return;
            chk1314Return.Checked = config.Digit1314Return;
            chkNumReturn.Checked = config.NumLSPReturn;
            chkNum1314Return.Checked = config.Num1314LSPReturn;
            chkExtremeReturn.Checked = config.ExtremeLSPReturn;
            chk1314PairReturn.Checked = config.Open1314PairReturn;
            chk890910Sequence.Checked = config.Seq890910AsSequence;
            
            // ===== 超无视设置 =====
            rbIgnoreSumBet.Checked = config.IgnoreOverSumBet;
            rbIgnoreSingleBet.Checked = !config.IgnoreOverSumBet;
            numIgnoreAmount.Value = SafeDecimal(config.IgnoreOverAmount, numIgnoreAmount.Minimum, numIgnoreAmount.Maximum);
            chkKillDoubleIgnore.Checked = config.KillDoubleIgnore;
            chkNo1314Odds.Checked = config.No1314Odds;
            
            // ===== 赔率设置 =====
            numOdds1.Value = SafeDecimal(config.DxdsOdds, numOdds1.Minimum, numOdds1.Maximum);
            numOdds2.Value = SafeDecimal(config.BigOddSmallEvenOdds, numOdds2.Minimum, numOdds2.Maximum);
            numOdds3.Value = SafeDecimal(config.BigEvenSmallOddOdds, numOdds3.Minimum, numOdds3.Maximum);
            numOdds4.Value = SafeDecimal(config.ExtremeOdds, numOdds4.Minimum, numOdds4.Maximum);
            numOdds5.Value = SafeDecimal(config.DigitOdds, numOdds5.Minimum, numOdds5.Maximum);
            numOdds6.Value = SafeDecimal(config.PairOdds, numOdds6.Minimum, numOdds6.Maximum);
            numOdds7.Value = SafeDecimal(config.StraightOdds, numOdds7.Minimum, numOdds7.Maximum);
            numOdds8.Value = SafeDecimal(config.HalfStraightOdds, numOdds8.Minimum, numOdds8.Maximum);
            numOdds9.Value = SafeDecimal(config.LeopardOdds, numOdds9.Minimum, numOdds9.Maximum);
            numOdds10.Value = SafeDecimal(config.MixedOdds, numOdds10.Minimum, numOdds10.Maximum);
            numOdds11.Value = SafeDecimal(config.BigEdgeOdds, numOdds11.Minimum, numOdds11.Maximum);
            numOdds12.Value = SafeDecimal(config.SmallEdgeOdds, numOdds12.Minimum, numOdds12.Maximum);
            numOdds13.Value = SafeDecimal(config.MiddleOdds, numOdds13.Minimum, numOdds13.Maximum);
            numOdds14.Value = SafeDecimal(config.EdgeHistoryOdds, numOdds14.Minimum, numOdds14.Maximum);
            
            // ===== 极数设置 =====
            numExtremeMax1.Value = SafeDecimal(config.ExtremeMax, numExtremeMax1.Minimum, numExtremeMax1.Maximum);
            numExtremeMax2.Value = SafeDecimal(config.ExtremeMaxEnd, numExtremeMax2.Minimum, numExtremeMax2.Maximum);
            numExtremeMin1.Value = SafeDecimal(config.ExtremeMin, numExtremeMin1.Minimum, numExtremeMin1.Maximum);
            numExtremeMin2.Value = SafeDecimal(config.ExtremeMinEnd, numExtremeMin2.Minimum, numExtremeMin2.Maximum);
            
            // ===== 单独数字赔率设置 =====
            chkSingleDigitOdds.Checked = config.SingleDigitOddsEnabled;
            LoadDigitOddsFromString(config.SingleDigitOddsList);
            
            // ===== 特码格式设置 =====
            txtSpecialChars.Text = config.SpecialCodeChars ?? "操|草|点|+|*|'|T";
            rbSpecialFirst.Checked = config.SpecialCodeFirst;
            rbAmountFirst.Checked = !config.SpecialCodeFirst;
            chkSmallAmountSpecial.Checked = config.SmallAmountAsSpecial;
            numMaxPayout.Value = SafeDecimal(config.MaxSinglePayout, numMaxPayout.Minimum, numMaxPayout.Maximum);
            numMaxDigitCount.Value = SafeDecimal(config.MaxDigitCount, numMaxDigitCount.Minimum, numMaxDigitCount.Maximum);
        }

        /// <summary>
        /// Clamp a decimal value into NumericUpDown range.
        /// </summary>
        private decimal SafeDecimal(decimal v, decimal min, decimal max)
        {
            if (v < min) return min;
            if (v > max) return max;
            return v;
        }
    }
}

