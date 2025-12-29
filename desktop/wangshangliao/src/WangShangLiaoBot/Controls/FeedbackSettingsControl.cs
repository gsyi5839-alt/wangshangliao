using System;
using System.Windows.Forms;
using WangShangLiaoBot.Services;

namespace WangShangLiaoBot.Controls
{
    /// <summary>
    /// 消息反馈设置控件
    /// </summary>
    public partial class FeedbackSettingsControl : UserControl
    {
        public FeedbackSettingsControl()
        {
            InitializeComponent();
        }

        public void LoadSettings()
        {
            var config = ConfigService.Instance.Config;
            txtFeedbackWangWang.Text = config.FeedbackWangWangId;
            txtFeedbackGroup.Text = config.FeedbackGroupId;
            chkFeedbackToWangWang.Checked = config.FeedbackToWangWang;
            chkFeedbackToGroup.Checked = config.FeedbackToGroup;
            chkBetCheckFeedback.Checked = config.BetCheckFeedback;
            chkBetSummaryFeedback.Checked = config.BetSummaryFeedback;
            chkProfitFeedback.Checked = config.ProfitFeedback;
            chkBillSendFeedback.Checked = config.BillSendFeedback;
        }

        public void SaveSettings()
        {
            var config = ConfigService.Instance.Config;
            config.FeedbackWangWangId = txtFeedbackWangWang.Text.Trim();
            config.FeedbackGroupId = txtFeedbackGroup.Text.Trim();
            config.FeedbackToWangWang = chkFeedbackToWangWang.Checked;
            config.FeedbackToGroup = chkFeedbackToGroup.Checked;
            config.BetCheckFeedback = chkBetCheckFeedback.Checked;
            config.BetSummaryFeedback = chkBetSummaryFeedback.Checked;
            config.ProfitFeedback = chkProfitFeedback.Checked;
            config.BillSendFeedback = chkBillSendFeedback.Checked;
        }

        private void btnSaveSettings_Click(object sender, EventArgs e)
        {
            SaveSettings();
            ConfigService.Instance.SaveConfig();
            MessageBox.Show("设置已保存", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}

