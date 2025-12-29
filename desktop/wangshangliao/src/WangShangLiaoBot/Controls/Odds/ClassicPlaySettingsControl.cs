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
                // Save key odds to OddsConfigService (no hard-coded values in settlement)
                var cfg = new ClassicOddsConfig
                {
                    DxdsOdds = (decimal)numOdds1.Value,
                    BigOddSmallEvenOdds = (decimal)numOdds2.Value,
                    BigEvenSmallOddOdds = (decimal)numOdds3.Value
                };
                OddsConfigService.Instance.SaveClassicOdds(cfg);
                
                MessageBox.Show("经典玩法设置已保存！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        /// <summary>
        /// Load settings from config
        /// </summary>
        public void LoadSettings()
        {
            // Load odds from OddsConfigService
            var cfg = OddsConfigService.Instance.LoadClassicOdds();
            if (cfg == null) return;

            // NumericUpDown holds decimals
            numOdds1.Value = SafeDecimal(cfg.DxdsOdds, numOdds1.Minimum, numOdds1.Maximum);
            numOdds2.Value = SafeDecimal(cfg.BigOddSmallEvenOdds, numOdds2.Minimum, numOdds2.Maximum);
            numOdds3.Value = SafeDecimal(cfg.BigEvenSmallOddOdds, numOdds3.Minimum, numOdds3.Maximum);
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

