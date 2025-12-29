using System;
using System.Windows.Forms;
using WangShangLiaoBot.Services;

namespace WangShangLiaoBot.Controls
{
    /// <summary>
    /// 开奖发送设置控件
    /// </summary>
    public partial class BillSendSettingsControl : UserControl
    {
        public BillSendSettingsControl()
        {
            InitializeComponent();
        }

        public void LoadSettings()
        {
            var config = ConfigService.Instance.Config;
            chkLotterySend.Checked = config.EnableLotteryNotify;
            chkWithR.Checked = config.LotteryWith8;
            chkImageSend.Checked = config.LotteryImageSend;
        }

        public void SaveSettings()
        {
            var config = ConfigService.Instance.Config;
            config.EnableLotteryNotify = chkLotterySend.Checked;
            config.LotteryWith8 = chkWithR.Checked;
            config.LotteryImageSend = chkImageSend.Checked;
        }
    }
}

