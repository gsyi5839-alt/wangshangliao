using System;
using System.Windows.Forms;
using WangShangLiaoBot.Services;

namespace WangShangLiaoBot.Forms.Settings
{
    /// <summary>
    /// 账单设置窗体
    /// </summary>
    public partial class BillSettingsForm : Form
    {
        public BillSettingsForm()
        {
            InitializeComponent();
            LoadSettings();
        }

        /// <summary>
        /// 加载设置
        /// </summary>
        private void LoadSettings()
        {
            var config = ConfigService.Instance.Config;
            
            // 左侧 - 开奖发送设置
            chkLotteryNotify.Checked = config.EnableLotteryNotify;
            chkWith8.Checked = config.LotteryWith8;
            chkImageSend.Checked = config.LotteryImageSend;
            numPeriodCount.Value = config.PeriodCount;
            
            // 账单格式
            numBillColumns.Value = config.BillColumns;
            chkBillImageSend.Checked = config.BillImageSend;
            chkBillSecondReply.Checked = config.BillSecondReply;
            
            // 群作业设置
            chkGroupTaskSend.Checked = config.GroupTaskSend;
            chkHideLostPlayers.Checked = config.HideLostPlayers;
            chkKeepZeroScore.Checked = config.KeepZeroScoreBill;
            chkKeepRecent10.Checked = config.KeepRecent10Tasks;
            chkAutoApprovePlayer.Checked = config.AutoApprovePlayer;
            numBillMinDigits.Value = config.BillMinDigits;
            numHideThreshold.Value = config.BillHideThreshold;
            
            // 右侧 - 基本设置
            txtAdminIds.Text = config.AdminWangWangId;
            txtGroupId.Text = config.GroupId;
            
            // 禁言设置
            numMuteBeforeSeconds.Value = config.MuteBeforeSeconds;
            numBetDataDelay.Value = config.BetDataDelaySeconds;
            chkBetDataImage.Checked = config.BetDataImageSend;
            chkGroupTaskNotify.Checked = config.GroupTaskNotify;
            
            // 消息反馈
            txtFeedbackWangWang.Text = config.FeedbackWangWangId;
            txtFeedbackGroup.Text = config.FeedbackGroupId;
            chkFeedbackToWangWang.Checked = config.FeedbackToWangWang;
            chkFeedbackToGroup.Checked = config.FeedbackToGroup;
            chkBetCheckFeedback.Checked = config.BetCheckFeedback;
            chkBetSummaryFeedback.Checked = config.BetSummaryFeedback;
            chkProfitFeedback.Checked = config.ProfitFeedback;
            chkBillSendFeedback.Checked = config.BillSendFeedback;
        }

        /// <summary>
        /// 保存设置
        /// </summary>
        private void SaveSettings()
        {
            var config = ConfigService.Instance.Config;
            
            // 左侧设置
            config.EnableLotteryNotify = chkLotteryNotify.Checked;
            config.LotteryWith8 = chkWith8.Checked;
            config.LotteryImageSend = chkImageSend.Checked;
            config.PeriodCount = (int)numPeriodCount.Value;
            
            config.BillColumns = (int)numBillColumns.Value;
            config.BillImageSend = chkBillImageSend.Checked;
            config.BillSecondReply = chkBillSecondReply.Checked;
            
            config.GroupTaskSend = chkGroupTaskSend.Checked;
            config.HideLostPlayers = chkHideLostPlayers.Checked;
            config.KeepZeroScoreBill = chkKeepZeroScore.Checked;
            config.KeepRecent10Tasks = chkKeepRecent10.Checked;
            config.AutoApprovePlayer = chkAutoApprovePlayer.Checked;
            config.BillMinDigits = (int)numBillMinDigits.Value;
            config.BillHideThreshold = (int)numHideThreshold.Value;
            
            // 右侧设置
            config.AdminWangWangId = txtAdminIds.Text.Trim();
            config.GroupId = txtGroupId.Text.Trim();
            
            config.MuteBeforeSeconds = (int)numMuteBeforeSeconds.Value;
            config.BetDataDelaySeconds = (int)numBetDataDelay.Value;
            config.BetDataImageSend = chkBetDataImage.Checked;
            config.GroupTaskNotify = chkGroupTaskNotify.Checked;
            
            config.FeedbackWangWangId = txtFeedbackWangWang.Text.Trim();
            config.FeedbackGroupId = txtFeedbackGroup.Text.Trim();
            config.FeedbackToWangWang = chkFeedbackToWangWang.Checked;
            config.FeedbackToGroup = chkFeedbackToGroup.Checked;
            config.BetCheckFeedback = chkBetCheckFeedback.Checked;
            config.BetSummaryFeedback = chkBetSummaryFeedback.Checked;
            config.ProfitFeedback = chkProfitFeedback.Checked;
            config.BillSendFeedback = chkBillSendFeedback.Checked;
            
            ConfigService.Instance.SaveConfig();
        }

        private void btnSaveAdmin_Click(object sender, EventArgs e)
        {
            var config = ConfigService.Instance.Config;
            config.AdminWangWangId = txtAdminIds.Text.Trim();
            config.GroupId = txtGroupId.Text.Trim();
            ConfigService.Instance.SaveConfig();
            MessageBox.Show("管理号和群号已保存", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnViewInviteLog_Click(object sender, EventArgs e)
        {
            MessageBox.Show("查看群成员邀请记录功能开发中...", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnSetBetDataContent_Click(object sender, EventArgs e)
        {
            MessageBox.Show("设置下注数据内容功能开发中...", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnSaveAll_Click(object sender, EventArgs e)
        {
            SaveSettings();
            MessageBox.Show("所有设置已保存", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}

