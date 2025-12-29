using System;
using System.Drawing;
using System.Windows.Forms;
using WangShangLiaoBot.Services;

namespace WangShangLiaoBot.Controls.Odds
{
    /// <summary>
    /// 三军玩法设置控件
    /// </summary>
    public partial class ThreeArmySettingsControl : UserControl
    {
        public ThreeArmySettingsControl()
        {
            InitializeComponent();
            LoadSettings();
        }
        
        #region 加载和保存设置
        
        /// <summary>
        /// 加载设置
        /// </summary>
        public void LoadSettings()
        {
            var config = ConfigService.Instance.Config;
            
            chkThreeArmyEnabled.Checked = config.ThreeArmyEnabled;
            txtOdds1.Text = config.ThreeArmyOdds1.ToString();
            txtOdds2.Text = config.ThreeArmyOdds2.ToString();
            txtOdds3.Text = config.ThreeArmyOdds3.ToString();
        }
        
        /// <summary>
        /// 保存设置
        /// </summary>
        public void SaveSettings()
        {
            var config = ConfigService.Instance.Config;
            
            config.ThreeArmyEnabled = chkThreeArmyEnabled.Checked;
            
            decimal val;
            if (decimal.TryParse(txtOdds1.Text, out val))
                config.ThreeArmyOdds1 = val;
            if (decimal.TryParse(txtOdds2.Text, out val))
                config.ThreeArmyOdds2 = val;
            if (decimal.TryParse(txtOdds3.Text, out val))
                config.ThreeArmyOdds3 = val;
            
            ConfigService.Instance.SaveConfig();
            Logger.Info("三军玩法设置已保存");
        }
        
        #endregion
        
        #region 事件处理
        
        private void btnSave_Click(object sender, EventArgs e)
        {
            SaveSettings();
            MessageBox.Show("三军玩法设置已保存！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        
        #endregion
    }
}


