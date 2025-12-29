using System;
using System.Windows.Forms;
using WangShangLiaoBot.Services;

namespace WangShangLiaoBot.Controls
{
    /// <summary>
    /// 禁言、核对设置控件
    /// </summary>
    public partial class MuteSettingsControl : UserControl
    {
        public MuteSettingsControl()
        {
            InitializeComponent();
        }

        public void LoadSettings()
        {
            var config = ConfigService.Instance.Config;
            numMuteSeconds.Value = config.MuteBeforeSeconds;
            chkBetDataTimer.Checked = true;
            numBetDataSeconds.Value = config.BetDataDelaySeconds;
            chkBetImageSend.Checked = config.BetDataImageSend;
            chkGroupTaskNotify.Checked = config.GroupTaskNotify;
        }

        public void SaveSettings()
        {
            var config = ConfigService.Instance.Config;
            config.MuteBeforeSeconds = (int)numMuteSeconds.Value;
            config.BetDataDelaySeconds = (int)numBetDataSeconds.Value;
            config.BetDataImageSend = chkBetImageSend.Checked;
            config.GroupTaskNotify = chkGroupTaskNotify.Checked;
        }

        private void btnSetBetContent_Click(object sender, EventArgs e)
        {
            MessageBox.Show("设置下注数据内容功能开发中...", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}

