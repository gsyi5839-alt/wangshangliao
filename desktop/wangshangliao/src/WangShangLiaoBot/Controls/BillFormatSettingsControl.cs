using System;
using System.Windows.Forms;
using WangShangLiaoBot.Services;

namespace WangShangLiaoBot.Controls
{
    /// <summary>
    /// 账单格式设置控件
    /// </summary>
    public partial class BillFormatSettingsControl : UserControl
    {
        public BillFormatSettingsControl()
        {
            InitializeComponent();
        }

        public void LoadSettings()
        {
            var config = ConfigService.Instance.Config;
            numBillColumns.Value = config.BillColumns;
            chkBillImageSend.Checked = config.BillImageSend;
            chkBillPrivateReply.Checked = config.BillSecondReply;
        }

        public void SaveSettings()
        {
            var config = ConfigService.Instance.Config;
            config.BillColumns = (int)numBillColumns.Value;
            config.BillImageSend = chkBillImageSend.Checked;
            config.BillSecondReply = chkBillPrivateReply.Checked;
        }
    }
}

