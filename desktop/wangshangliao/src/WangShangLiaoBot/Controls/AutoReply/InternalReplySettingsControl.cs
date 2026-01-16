using System;
using System.Windows.Forms;
using WangShangLiaoBot.Services;

namespace WangShangLiaoBot.Controls.AutoReply
{
    /// <summary>
    /// Internal reply settings control
    /// Handles system message templates for various betting scenarios
    /// </summary>
    public partial class InternalReplySettingsControl : UserControl
    {
        public InternalReplySettingsControl()
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
            
            // Left panel - betting scenario messages
            txtBetDisplay.Text = config.InternalBetDisplay;
            txtCancelBet.Text = config.InternalCancelBet;
            txtFuzzyRemind.Text = config.InternalFuzzyRemind;
            txtAttackValid.Text = config.InternalAttackValid;
            txtBetNoDown.Text = config.InternalBetNoDown;
            txtBetNoDown2.Text = config.InternalBetNoDown2;
            txtDownProcessing.Text = config.InternalDownProcessing;
            txtSealedUnprocessed.Text = config.InternalSealedUnprocessed;
            txtCancelTrustee.Text = config.InternalCancelTrustee;
            txtForbid09.Text = config.InternalForbid09;
            
            // Right panel - up/down score settings
            txtUpDownMin.Text = config.InternalUpDownMin;
            txtUpDownMin2.Text = config.InternalUpDownMin2;
            txtUpDownMax.Text = config.InternalUpDownMax;
            txtUpDownPlayer.Text = config.InternalUpDownPlayer;
            
            // Personal data feedback
            txtDataKeyword.Text = config.InternalDataKeyword;
            txtDataBill.Text = config.InternalDataBill;
            txtDataNoAttack.Text = config.InternalDataNoAttack;
            txtDataHasAttack.Text = config.InternalDataHasAttack;
            
            // Group rules keywords and content (left bottom)
            txtGroupRulesKeyword.Text = config.InternalGroupRulesKeyword;
            txtGroupRules.Text = config.InternalGroupRules;
            
            // Private chat tail
            txtTailUnsealed.Text = config.InternalTailUnsealed;
            txtTailSealed.Text = config.InternalTailSealed;
        }
        
        /// <summary>
        /// Save settings to configuration
        /// </summary>
        public void SaveSettings()
        {
            var config = ConfigService.Instance.Config;
            
            // Left panel - betting scenario messages
            config.InternalBetDisplay = txtBetDisplay.Text;
            config.InternalCancelBet = txtCancelBet.Text;
            config.InternalFuzzyRemind = txtFuzzyRemind.Text;
            config.InternalAttackValid = txtAttackValid.Text;
            config.InternalBetNoDown = txtBetNoDown.Text;
            config.InternalBetNoDown2 = txtBetNoDown2.Text;
            config.InternalDownProcessing = txtDownProcessing.Text;
            config.InternalSealedUnprocessed = txtSealedUnprocessed.Text;
            config.InternalCancelTrustee = txtCancelTrustee.Text;
            config.InternalForbid09 = txtForbid09.Text;
            
            // Right panel - up/down score settings
            config.InternalUpDownMin = txtUpDownMin.Text;
            config.InternalUpDownMin2 = txtUpDownMin2.Text;
            config.InternalUpDownMax = txtUpDownMax.Text;
            config.InternalUpDownPlayer = txtUpDownPlayer.Text;
            
            // Personal data feedback
            config.InternalDataKeyword = txtDataKeyword.Text;
            config.InternalDataBill = txtDataBill.Text;
            config.InternalDataNoAttack = txtDataNoAttack.Text;
            config.InternalDataHasAttack = txtDataHasAttack.Text;
            
            // Group rules keywords and content (left bottom)
            config.InternalGroupRulesKeyword = txtGroupRulesKeyword.Text;
            config.InternalGroupRules = txtGroupRules.Text;
            
            // Private chat tail
            config.InternalTailUnsealed = txtTailUnsealed.Text;
            config.InternalTailSealed = txtTailSealed.Text;
            
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
                MessageBox.Show("内部回复设置已保存！", "保存成功", 
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

