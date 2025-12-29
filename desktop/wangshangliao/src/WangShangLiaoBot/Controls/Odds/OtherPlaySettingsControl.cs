using System;
using System.Drawing;
using System.Windows.Forms;
using WangShangLiaoBot.Services;

namespace WangShangLiaoBot.Controls.Odds
{
    /// <summary>
    /// Other gameplay settings control
    /// Handles configuration for TwoSeven, ReverseLottery, and DragonStreak gameplay
    /// </summary>
    public partial class OtherPlaySettingsControl : UserControl
    {
        public OtherPlaySettingsControl()
        {
            InitializeComponent();
            LoadSettings();
        }
        
        #region Load and Save Settings
        
        /// <summary>
        /// Load settings from configuration
        /// </summary>
        public void LoadSettings()
        {
            var config = ConfigService.Instance.Config;
            
            // ===== 二七玩法设置 =====
            chkTwoSevenEnabled.Checked = config.TwoSevenEnabled;
            txtSingleExceed.Text = config.TwoSevenSingleExceed.ToString();
            txtSingleOdds.Text = config.TwoSevenSingleOdds.ToString();
            txtComboExceed.Text = config.TwoSevenComboExceed.ToString();
            txtComboOdds.Text = config.TwoSevenComboOdds.ToString();
            
            // ===== 反向开奖玩法设置 =====
            chkReverseLotteryEnabled.Checked = config.ReverseLotteryEnabled;
            txtBetMaxRatio.Text = config.ReverseLotteryBetMaxRatio.ToString();
            txtProfitDeduct.Text = config.ReverseLotteryProfitDeduct.ToString();
            
            // ===== 长龙玩法设置 =====
            chkDragonStreakEnabled.Checked = config.DragonStreakEnabled;
            txtStreak1Times.Text = config.DragonStreak1Times.ToString();
            txtStreak1Reduce.Text = config.DragonStreak1Reduce.ToString();
            txtStreak2Times.Text = config.DragonStreak2Times.ToString();
            txtStreak2Reduce.Text = config.DragonStreak2Reduce.ToString();
        }
        
        /// <summary>
        /// Save settings to configuration
        /// </summary>
        public void SaveSettings()
        {
            var config = ConfigService.Instance.Config;
            
            // ===== 二七玩法设置 =====
            config.TwoSevenEnabled = chkTwoSevenEnabled.Checked;
            
            if (decimal.TryParse(txtSingleExceed.Text, out decimal singleExceed))
                config.TwoSevenSingleExceed = singleExceed;
            
            if (decimal.TryParse(txtSingleOdds.Text, out decimal singleOdds))
                config.TwoSevenSingleOdds = singleOdds;
            
            if (decimal.TryParse(txtComboExceed.Text, out decimal comboExceed))
                config.TwoSevenComboExceed = comboExceed;
            
            if (decimal.TryParse(txtComboOdds.Text, out decimal comboOdds))
                config.TwoSevenComboOdds = comboOdds;
            
            // ===== 反向开奖玩法设置 =====
            config.ReverseLotteryEnabled = chkReverseLotteryEnabled.Checked;
            
            if (decimal.TryParse(txtBetMaxRatio.Text, out decimal betMaxRatio))
                config.ReverseLotteryBetMaxRatio = betMaxRatio;
            
            if (decimal.TryParse(txtProfitDeduct.Text, out decimal profitDeduct))
                config.ReverseLotteryProfitDeduct = profitDeduct;
            
            // ===== 长龙玩法设置 =====
            config.DragonStreakEnabled = chkDragonStreakEnabled.Checked;
            
            if (int.TryParse(txtStreak1Times.Text, out int streak1Times))
                config.DragonStreak1Times = streak1Times;
            
            if (decimal.TryParse(txtStreak1Reduce.Text, out decimal streak1Reduce))
                config.DragonStreak1Reduce = streak1Reduce;
            
            if (int.TryParse(txtStreak2Times.Text, out int streak2Times))
                config.DragonStreak2Times = streak2Times;
            
            if (decimal.TryParse(txtStreak2Reduce.Text, out decimal streak2Reduce))
                config.DragonStreak2Reduce = streak2Reduce;
            
            // Save to file
            ConfigService.Instance.SaveConfig();
        }
        
        #endregion
        
        #region Event Handlers
        
        /// <summary>
        /// Handle save button click event
        /// </summary>
        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                SaveSettings();
                MessageBox.Show("其他玩法设置已保存！", "保存成功", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存失败：{ex.Message}", "保存错误", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        #endregion
    }
}

