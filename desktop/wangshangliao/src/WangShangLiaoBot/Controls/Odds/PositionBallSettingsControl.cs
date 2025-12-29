using System;
using System.Drawing;
using System.Windows.Forms;
using WangShangLiaoBot.Services;

namespace WangShangLiaoBot.Controls.Odds
{
    /// <summary>
    /// Position ball game settings control
    /// Handles odds configuration and bet range settings for position ball gameplay
    /// </summary>
    public partial class PositionBallSettingsControl : UserControl
    {
        public PositionBallSettingsControl()
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
            
            // Main switch
            chkPositionBallEnabled.Checked = config.PositionBallEnabled;
            
            // Single bet odds and range
            txtSingleOdds.Text = config.PositionBallSingleOdds.ToString();
            txtSingleRangeMin.Text = config.PositionBallSingleRangeMin.ToString();
            txtSingleRangeMax.Text = config.PositionBallSingleRangeMax.ToString();
            
            // Combo bet odds and range
            txtComboOdds.Text = config.PositionBallComboOdds.ToString();
            txtComboRangeMin.Text = config.PositionBallComboRangeMin.ToString();
            txtComboRangeMax.Text = config.PositionBallComboRangeMax.ToString();
            
            // Special code odds and range
            txtSpecialOdds.Text = config.PositionBallSpecialOdds.ToString();
            txtSpecialRangeMin.Text = config.PositionBallSpecialRangeMin.ToString();
            txtSpecialRangeMax.Text = config.PositionBallSpecialRangeMax.ToString();
        }
        
        /// <summary>
        /// Save settings to configuration
        /// </summary>
        public void SaveSettings()
        {
            var config = ConfigService.Instance.Config;
            
            // Main switch
            config.PositionBallEnabled = chkPositionBallEnabled.Checked;
            
            // Single bet odds and range
            decimal decVal;
            int intVal;
            
            if (decimal.TryParse(txtSingleOdds.Text, out decVal))
                config.PositionBallSingleOdds = decVal;
            if (int.TryParse(txtSingleRangeMin.Text, out intVal))
                config.PositionBallSingleRangeMin = intVal;
            if (int.TryParse(txtSingleRangeMax.Text, out intVal))
                config.PositionBallSingleRangeMax = intVal;
            
            // Combo bet odds and range
            if (decimal.TryParse(txtComboOdds.Text, out decVal))
                config.PositionBallComboOdds = decVal;
            if (int.TryParse(txtComboRangeMin.Text, out intVal))
                config.PositionBallComboRangeMin = intVal;
            if (int.TryParse(txtComboRangeMax.Text, out intVal))
                config.PositionBallComboRangeMax = intVal;
            
            // Special code odds and range
            if (decimal.TryParse(txtSpecialOdds.Text, out decVal))
                config.PositionBallSpecialOdds = decVal;
            if (int.TryParse(txtSpecialRangeMin.Text, out intVal))
                config.PositionBallSpecialRangeMin = intVal;
            if (int.TryParse(txtSpecialRangeMax.Text, out intVal))
                config.PositionBallSpecialRangeMax = intVal;
            
            ConfigService.Instance.SaveConfig();
            Logger.Info("定位球玩法设置已保存");
        }
        
        #endregion
        
        #region Validation Methods
        
        /// <summary>
        /// Validate all input fields
        /// </summary>
        /// <returns>True if all fields are valid, false otherwise</returns>
        private bool ValidateInputs()
        {
            // Validate single bet range
            if (!ValidateRangeInputs(txtSingleRangeMin, txtSingleRangeMax, "单注"))
                return false;
            
            // Validate combo bet range
            if (!ValidateRangeInputs(txtComboRangeMin, txtComboRangeMax, "组合"))
                return false;
            
            // Validate special code range
            if (!ValidateRangeInputs(txtSpecialRangeMin, txtSpecialRangeMax, "特码"))
                return false;
            
            // Validate odds values
            if (!ValidateOddsInput(txtSingleOdds, "单注赔率"))
                return false;
            if (!ValidateOddsInput(txtComboOdds, "组合赔率"))
                return false;
            if (!ValidateOddsInput(txtSpecialOdds, "特码赔率"))
                return false;
            
            return true;
        }
        
        /// <summary>
        /// Validate range input pair (min <= max)
        /// </summary>
        private bool ValidateRangeInputs(TextBox minBox, TextBox maxBox, string fieldName)
        {
            int minVal, maxVal;
            
            // Check if min value is valid
            if (!int.TryParse(minBox.Text, out minVal) || minVal < 0)
            {
                ShowValidationError($"{fieldName}下注范围最小值必须是非负整数！");
                minBox.Focus();
                return false;
            }
            
            // Check if max value is valid
            if (!int.TryParse(maxBox.Text, out maxVal) || maxVal < 0)
            {
                ShowValidationError($"{fieldName}下注范围最大值必须是非负整数！");
                maxBox.Focus();
                return false;
            }
            
            // Check if min <= max
            if (minVal > maxVal)
            {
                ShowValidationError($"{fieldName}下注范围设置错误：最小值不能大于最大值！");
                minBox.Focus();
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Validate odds input value
        /// </summary>
        private bool ValidateOddsInput(TextBox oddsBox, string fieldName)
        {
            decimal oddsVal;
            if (!decimal.TryParse(oddsBox.Text, out oddsVal) || oddsVal < 0)
            {
                ShowValidationError($"{fieldName}必须是非负数字！");
                oddsBox.Focus();
                return false;
            }
            return true;
        }
        
        /// <summary>
        /// Show validation error message
        /// </summary>
        private void ShowValidationError(string message)
        {
            MessageBox.Show(message, "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        
        #endregion
        
        #region Event Handlers
        
        /// <summary>
        /// Save button click handler
        /// </summary>
        private void btnSave_Click(object sender, EventArgs e)
        {
            // Validate inputs before saving
            if (!ValidateInputs())
                return;
            
            SaveSettings();
            MessageBox.Show("定位球玩法设置已保存！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        
        #endregion
    }
}

