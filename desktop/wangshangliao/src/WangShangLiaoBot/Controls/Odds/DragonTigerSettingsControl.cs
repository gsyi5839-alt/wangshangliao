using System;
using System.Drawing;
using System.Windows.Forms;
using WangShangLiaoBot.Services;

namespace WangShangLiaoBot.Controls.Odds
{
    /// <summary>
    /// 龙虎玩法设置控件
    /// </summary>
    public partial class DragonTigerSettingsControl : UserControl
    {
        public DragonTigerSettingsControl()
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
            
            // 基本设置
            chkDragonTigerEnabled.Checked = config.DragonTigerEnabled;
            rbDragonTigerFight.Checked = config.DragonTigerMode == 0;
            rbDragonTigerLeopard.Checked = config.DragonTigerMode == 1;
            
            // 规则设置
            cboZone1.SelectedIndex = config.DragonTigerZone1;
            cboCompare.SelectedIndex = config.DragonTigerCompare;
            cboZone2.SelectedIndex = config.DragonTigerZone2;
            chkDrawReturn.Checked = config.DragonTigerDrawReturn;
            chkLeopardKillAll.Checked = config.DragonTigerLeopardKillAll;
            
            // 赔率设置
            txtDragonTigerOdds.Text = config.DragonTigerOdds.ToString();
            txtDrawOdds.Text = config.DragonTigerDrawOdds.ToString();
            txtBetOverAmount.Text = config.DragonTigerBetOverAmount.ToString();
            txtDragonTigerOdds2.Text = config.DragonTigerOdds2.ToString();
            txtDrawOdds2.Text = config.DragonTigerDrawOdds2.ToString();
            
            // 龙虎豹赔率
            txtLeopardOdds.Text = config.DragonTigerLeopardOdds.ToString();
            
            // 号码定义
            txtDragonNumbers.Text = config.DragonNumbers;
            txtTigerNumbers.Text = config.TigerNumbers;
            txtLeopardNumbers.Text = config.LeopardNumbers;
        }
        
        /// <summary>
        /// 保存设置
        /// </summary>
        public void SaveSettings()
        {
            var config = ConfigService.Instance.Config;
            
            // 基本设置
            config.DragonTigerEnabled = chkDragonTigerEnabled.Checked;
            config.DragonTigerMode = rbDragonTigerFight.Checked ? 0 : 1;
            
            // 规则设置
            config.DragonTigerZone1 = cboZone1.SelectedIndex;
            config.DragonTigerCompare = cboCompare.SelectedIndex;
            config.DragonTigerZone2 = cboZone2.SelectedIndex;
            config.DragonTigerDrawReturn = chkDrawReturn.Checked;
            config.DragonTigerLeopardKillAll = chkLeopardKillAll.Checked;
            
            // 赔率设置
            decimal val;
            if (decimal.TryParse(txtDragonTigerOdds.Text, out val))
                config.DragonTigerOdds = val;
            if (decimal.TryParse(txtDrawOdds.Text, out val))
                config.DragonTigerDrawOdds = val;
            int intVal;
            if (int.TryParse(txtBetOverAmount.Text, out intVal))
                config.DragonTigerBetOverAmount = intVal;
            if (decimal.TryParse(txtDragonTigerOdds2.Text, out val))
                config.DragonTigerOdds2 = val;
            if (decimal.TryParse(txtDrawOdds2.Text, out val))
                config.DragonTigerDrawOdds2 = val;
            
            // 龙虎豹赔率
            if (decimal.TryParse(txtLeopardOdds.Text, out val))
                config.DragonTigerLeopardOdds = val;
            
            // 号码定义
            config.DragonNumbers = txtDragonNumbers.Text.Trim();
            config.TigerNumbers = txtTigerNumbers.Text.Trim();
            config.LeopardNumbers = txtLeopardNumbers.Text.Trim();
            
            ConfigService.Instance.SaveConfig();
            Logger.Info("龙虎玩法设置已保存");
        }
        
        #endregion
        
        #region 事件处理
        
        private void btnSave_Click(object sender, EventArgs e)
        {
            SaveSettings();
            MessageBox.Show("龙虎玩法设置已保存！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        
        #endregion
    }
}


