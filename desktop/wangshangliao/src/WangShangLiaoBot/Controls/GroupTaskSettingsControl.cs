using System;
using System.Windows.Forms;
using WangShangLiaoBot.Services;

namespace WangShangLiaoBot.Controls
{
    /// <summary>
    /// 群作业设置控件
    /// </summary>
    public partial class GroupTaskSettingsControl : UserControl
    {
        public GroupTaskSettingsControl()
        {
            InitializeComponent();
        }

        public void LoadSettings()
        {
            var config = ConfigService.Instance.Config;
            chkGroupTaskSend.Checked = config.GroupTaskSend;
            chkHideLostPlayers.Checked = config.HideLostPlayers;
            chkKeepZeroScore.Checked = config.KeepZeroScoreBill;
            chkKeepRecent10.Checked = config.KeepRecent10Tasks;
            chkAutoApprove.Checked = config.AutoApprovePlayer;
            numBillMinDigits.Value = config.BillMinDigits;
            numHideThreshold.Value = config.BillHideThreshold;
        }

        public void SaveSettings()
        {
            var config = ConfigService.Instance.Config;
            config.GroupTaskSend = chkGroupTaskSend.Checked;
            config.HideLostPlayers = chkHideLostPlayers.Checked;
            config.KeepZeroScoreBill = chkKeepZeroScore.Checked;
            config.KeepRecent10Tasks = chkKeepRecent10.Checked;
            config.AutoApprovePlayer = chkAutoApprove.Checked;
            config.BillMinDigits = (int)numBillMinDigits.Value;
            config.BillHideThreshold = (int)numHideThreshold.Value;
        }
    }
}

