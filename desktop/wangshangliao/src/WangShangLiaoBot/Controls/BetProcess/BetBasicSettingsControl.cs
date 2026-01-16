using System;
using System.Windows.Forms;
using WangShangLiaoBot.Services;

namespace WangShangLiaoBot.Controls.BetProcess
{
    /// <summary>
    /// Bet processing basic settings control - Contains all checkbox options for bet handling
    /// Implements UI for: 基本设置, 攻击范围 (下注处理)
    /// </summary>
    public partial class BetBasicSettingsControl : UserControl
    {
        public BetBasicSettingsControl()
        {
            InitializeComponent();
            
            // Wire up events
            btnSaveSettings.Click += BtnSaveSettings_Click;
            
            // Load settings on initialization
            LoadSettings();
        }

        /// <summary>
        /// Load settings from BetProcessSettingsService
        /// </summary>
        public void LoadSettings()
        {
            try
            {
                var settings = BetProcessSettingsService.Instance;

                // === 基本设置 Row 1-2 ===
                chkAllowModifyBet.Checked = settings.AllowModifyBet;
                chkProhibitCancel.Checked = settings.ProhibitCancel;
                chkShowBet.Checked = settings.ShowBet;
                chkVariableBetLater.Checked = settings.VariableBetLater;
                chkAutoProcessBeforeScore.Checked = settings.AutoProcessBeforeScore;
                chkSendSealBeforeProcess.Checked = settings.SendSealBeforeProcess;

                // === 重复下注处理 Row 3-4 ===
                switch (settings.RepeatBetMode)
                {
                    case 0: rdoCalcRepeat.Checked = true; break;
                    case 1: rdoSameNotCalc.Checked = true; break;
                    case 2: rdoLastBet.Checked = true; break;
                    case 3: rdoFirstBet.Checked = true; break;
                    default: rdoLastBet.Checked = true; break;
                }

                // === 模糊匹配 Row 5-6 ===
                chkFuzzyMatch.Checked = settings.FuzzyMatchEnabled;
                chkFuzzyMatchSupport.Checked = settings.FuzzyMatchSupportRemind;
                chkNoBillRemind.Checked = settings.NoBillRemindEnabled;
                txtNoBillRemindContent.Text = settings.NoBillRemindContent;

                // === 组合下注无效 Row 7-10 ===
                chkCombinationInvalid.Checked = settings.CombinationInvalidEnabled;
                txtCombinationInvalidMsg.Text = settings.CombinationInvalidMsg;
                chkMultiCombinationInvalid.Checked = settings.MultiCombinationInvalidEnabled;
                txtMultiCombinationInvalidMsg.Text = settings.MultiCombinationInvalidMsg;
                chkSingleOppositeInvalid.Checked = settings.SingleOppositeInvalidEnabled;
                txtSingleOppositeInvalidMsg.Text = settings.SingleOppositeInvalidMsg;
                chkMaxCombination.Checked = settings.MaxCombinationEnabled;
                nudMaxCombination.Value = Math.Min(Math.Max(settings.MaxCombinationCount, 1), 10);
                txtMaxCombinationMsg.Text = settings.MaxCombinationMsg;

                // === 群开关 Row 11-14 ===
                chkPinyinBetOnly.Checked = settings.PinyinBetOnly;
                if (settings.ChineseBetMode == 0)
                    rdoChineseBetWithPinyin.Checked = true;
                else
                    rdoChineseBetNoPinyin.Checked = true;
                txtPinyinExample.Text = settings.PinyinRemindContent;
                chkReceiveGroupBet.Checked = settings.ReceiveGroupBet;
                chkAutoMuteUnmute.Checked = settings.AutoMuteUnmute;

                // === 好友开关 Row 11-16 ===
                chkEnableFriendChat.Checked = settings.EnableFriendChat;
                chkAutoAgreeFriend.Checked = settings.AutoAgreeFriend;
                chkOnlyMemberBet.Checked = settings.OnlyMemberBet;
                chkFriendBetNotInGroup.Checked = settings.FriendBetNotInGroup;
                chkFriendScoreNotInGroup.Checked = settings.FriendScoreNotInGroup;
                chkFriendQueryInGroup.Checked = settings.FriendQueryInGroup;

                // === 其他设置 Row 15-16 ===
                chkGlobalImageSend.Checked = settings.GlobalImageSendEnabled;
                nudFontSize.Value = Math.Min(Math.Max(settings.FontSize, 8), 72);
                chkHistoryShow.Checked = settings.HistoryShowEnabled;
                nudHistoryPeriod.Value = Math.Min(Math.Max(settings.HistoryPeriodCount, 1), 100);
                chkGlobalDigitLower.Checked = settings.GlobalDigitLower;
                chkUpperDigitUse.Checked = settings.UpperDigitUseCircled;
            }
            catch (Exception ex)
            {
                Logger.Error($"[BetBasicSettings] LoadSettings error: {ex.Message}");
            }
        }

        /// <summary>
        /// Save settings to BetProcessSettingsService
        /// </summary>
        public void SaveSettings()
        {
            try
            {
                var settings = BetProcessSettingsService.Instance;

                // === 基本设置 Row 1-2 ===
                settings.AllowModifyBet = chkAllowModifyBet.Checked;
                settings.ProhibitCancel = chkProhibitCancel.Checked;
                settings.ShowBet = chkShowBet.Checked;
                settings.VariableBetLater = chkVariableBetLater.Checked;
                settings.AutoProcessBeforeScore = chkAutoProcessBeforeScore.Checked;
                settings.SendSealBeforeProcess = chkSendSealBeforeProcess.Checked;

                // === 重复下注处理 Row 3-4 ===
                if (rdoCalcRepeat.Checked) settings.RepeatBetMode = 0;
                else if (rdoSameNotCalc.Checked) settings.RepeatBetMode = 1;
                else if (rdoLastBet.Checked) settings.RepeatBetMode = 2;
                else if (rdoFirstBet.Checked) settings.RepeatBetMode = 3;

                // === 模糊匹配 Row 5-6 ===
                settings.FuzzyMatchEnabled = chkFuzzyMatch.Checked;
                settings.FuzzyMatchSupportRemind = chkFuzzyMatchSupport.Checked;
                settings.NoBillRemindEnabled = chkNoBillRemind.Checked;
                settings.NoBillRemindContent = txtNoBillRemindContent.Text;

                // === 组合下注无效 Row 7-10 ===
                settings.CombinationInvalidEnabled = chkCombinationInvalid.Checked;
                settings.CombinationInvalidMsg = txtCombinationInvalidMsg.Text;
                settings.MultiCombinationInvalidEnabled = chkMultiCombinationInvalid.Checked;
                settings.MultiCombinationInvalidMsg = txtMultiCombinationInvalidMsg.Text;
                settings.SingleOppositeInvalidEnabled = chkSingleOppositeInvalid.Checked;
                settings.SingleOppositeInvalidMsg = txtSingleOppositeInvalidMsg.Text;
                settings.MaxCombinationEnabled = chkMaxCombination.Checked;
                settings.MaxCombinationCount = (int)nudMaxCombination.Value;
                settings.MaxCombinationMsg = txtMaxCombinationMsg.Text;

                // === 群开关 Row 11-14 ===
                settings.PinyinBetOnly = chkPinyinBetOnly.Checked;
                settings.ChineseBetMode = rdoChineseBetWithPinyin.Checked ? 0 : 1;
                settings.PinyinRemindContent = txtPinyinExample.Text;
                settings.ReceiveGroupBet = chkReceiveGroupBet.Checked;
                settings.AutoMuteUnmute = chkAutoMuteUnmute.Checked;

                // === 好友开关 Row 11-16 ===
                settings.EnableFriendChat = chkEnableFriendChat.Checked;
                settings.AutoAgreeFriend = chkAutoAgreeFriend.Checked;
                settings.OnlyMemberBet = chkOnlyMemberBet.Checked;
                settings.FriendBetNotInGroup = chkFriendBetNotInGroup.Checked;
                settings.FriendScoreNotInGroup = chkFriendScoreNotInGroup.Checked;
                settings.FriendQueryInGroup = chkFriendQueryInGroup.Checked;

                // === 其他设置 Row 15-16 ===
                settings.GlobalImageSendEnabled = chkGlobalImageSend.Checked;
                settings.FontSize = (int)nudFontSize.Value;
                settings.HistoryShowEnabled = chkHistoryShow.Checked;
                settings.HistoryPeriodCount = (int)nudHistoryPeriod.Value;
                settings.GlobalDigitLower = chkGlobalDigitLower.Checked;
                settings.UpperDigitUseCircled = chkUpperDigitUse.Checked;

                // Save to file
                settings.SaveToFile();

                MessageBox.Show("下注处理设置已保存", "保存成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Logger.Error($"[BetBasicSettings] SaveSettings error: {ex.Message}");
                MessageBox.Show($"保存失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Save button click handler
        /// </summary>
        private void BtnSaveSettings_Click(object sender, EventArgs e)
        {
            SaveSettings();
        }
    }
}
