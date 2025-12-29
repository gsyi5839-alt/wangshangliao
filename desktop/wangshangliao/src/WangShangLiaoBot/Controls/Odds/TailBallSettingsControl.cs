using System;
using System.Drawing;
using System.Windows.Forms;
using WangShangLiaoBot.Services;

namespace WangShangLiaoBot.Controls.Odds
{
    /// <summary>
    /// 尾球玩法设置控件
    /// </summary>
    public partial class TailBallSettingsControl : UserControl
    {
        public TailBallSettingsControl()
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
            chkTailBallEnabled.Checked = config.TailBallEnabled;
            chkTailBallNotCountClassic.Checked = config.TailBallNotCountClassic;
            
            // 无13 14赔率
            txtOdds1314BigSmall.Text = config.TailOdds1314BigSmall.ToString();
            txtOdds1314Combo.Text = config.TailOdds1314Combo.ToString();
            txtOdds1314Special.Text = config.TailOdds1314Special.ToString();
            
            // 尾球开0 9赔率
            txtOdds09BigSmall.Text = config.TailOdds09BigSmall.ToString();
            txtOdds09Combo.Text = config.TailOdds09Combo.ToString();
            
            // 有13 14赔率
            rbSingleBet.Checked = config.TailBetTypeSingle;
            rbTotalBet.Checked = !config.TailBetTypeSingle;
            txtTailBallOver1.Text = config.TailBallOver1.ToString();
            txtOddsWith1314BigSmall.Text = config.TailOddsWith1314BigSmall.ToString();
            txtTailBallOver2.Text = config.TailBallOver2.ToString();
            txtOddsWith1314Combo.Text = config.TailOddsWith1314Combo.ToString();
            
            // 其他设置
            chkOtherGameCountTotal.Checked = config.OtherGameCountTotal;
            chkForbid09.Checked = config.TailForbid09;
            chkFrontBallEnabled.Checked = config.FrontBallEnabled;
            chkMiddleBallEnabled.Checked = config.MiddleBallEnabled;
        }
        
        /// <summary>
        /// 保存设置
        /// </summary>
        public void SaveSettings()
        {
            var config = ConfigService.Instance.Config;
            
            // 基本设置
            config.TailBallEnabled = chkTailBallEnabled.Checked;
            config.TailBallNotCountClassic = chkTailBallNotCountClassic.Checked;
            
            // 无13 14赔率
            decimal val;
            if (decimal.TryParse(txtOdds1314BigSmall.Text, out val))
                config.TailOdds1314BigSmall = val;
            if (decimal.TryParse(txtOdds1314Combo.Text, out val))
                config.TailOdds1314Combo = val;
            if (decimal.TryParse(txtOdds1314Special.Text, out val))
                config.TailOdds1314Special = val;
            
            // 尾球开0 9赔率
            if (decimal.TryParse(txtOdds09BigSmall.Text, out val))
                config.TailOdds09BigSmall = val;
            if (decimal.TryParse(txtOdds09Combo.Text, out val))
                config.TailOdds09Combo = val;
            
            // 有13 14赔率
            config.TailBetTypeSingle = rbSingleBet.Checked;
            int intVal;
            if (int.TryParse(txtTailBallOver1.Text, out intVal))
                config.TailBallOver1 = intVal;
            if (decimal.TryParse(txtOddsWith1314BigSmall.Text, out val))
                config.TailOddsWith1314BigSmall = val;
            if (int.TryParse(txtTailBallOver2.Text, out intVal))
                config.TailBallOver2 = intVal;
            if (decimal.TryParse(txtOddsWith1314Combo.Text, out val))
                config.TailOddsWith1314Combo = val;
            
            // 其他设置
            config.OtherGameCountTotal = chkOtherGameCountTotal.Checked;
            config.TailForbid09 = chkForbid09.Checked;
            config.FrontBallEnabled = chkFrontBallEnabled.Checked;
            config.MiddleBallEnabled = chkMiddleBallEnabled.Checked;
            
            ConfigService.Instance.SaveConfig();
            Logger.Info("尾球玩法设置已保存");
        }
        
        #endregion
        
        #region 事件处理
        
        private void btnSave_Click(object sender, EventArgs e)
        {
            SaveSettings();
            MessageBox.Show("尾球玩法设置已保存！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        
        #endregion
    }
}

