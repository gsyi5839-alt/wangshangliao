namespace WangShangLiaoBot.Controls.BetProcess
{
    partial class BetBasicSettingsControl
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            // === Section: 下注设置 ===
            this.grpBetSettings = new System.Windows.Forms.GroupBox();
            this.chkAllowModifyBet = new System.Windows.Forms.CheckBox();
            this.chkProhibitCancel = new System.Windows.Forms.CheckBox();
            this.chkShowBet = new System.Windows.Forms.CheckBox();
            this.chkVariableBetLater = new System.Windows.Forms.CheckBox();
            this.chkAutoProcessBeforeScore = new System.Windows.Forms.CheckBox();
            this.chkSendSealBeforeProcess = new System.Windows.Forms.CheckBox();

            // === Section: 重复下注不带加或改处理 ===
            this.grpRepeatBet = new System.Windows.Forms.GroupBox();
            this.rdoCalcRepeat = new System.Windows.Forms.RadioButton();
            this.rdoSameNotCalc = new System.Windows.Forms.RadioButton();
            this.rdoLastBet = new System.Windows.Forms.RadioButton();
            this.rdoFirstBet = new System.Windows.Forms.RadioButton();

            // === Section: 模糊匹配 ===
            this.chkFuzzyMatch = new System.Windows.Forms.CheckBox();
            this.chkFuzzyMatchSupport = new System.Windows.Forms.CheckBox();
            this.chkNoBillRemind = new System.Windows.Forms.CheckBox();
            this.txtNoBillRemindContent = new System.Windows.Forms.TextBox();

            // === Section: 组合下注无效 ===
            this.chkCombinationInvalid = new System.Windows.Forms.CheckBox();
            this.txtCombinationInvalidMsg = new System.Windows.Forms.TextBox();
            this.chkMultiCombinationInvalid = new System.Windows.Forms.CheckBox();
            this.txtMultiCombinationInvalidMsg = new System.Windows.Forms.TextBox();
            this.chkSingleOppositeInvalid = new System.Windows.Forms.CheckBox();
            this.txtSingleOppositeInvalidMsg = new System.Windows.Forms.TextBox();
            this.chkMaxCombination = new System.Windows.Forms.CheckBox();
            this.nudMaxCombination = new System.Windows.Forms.NumericUpDown();
            this.txtMaxCombinationMsg = new System.Windows.Forms.TextBox();

            // === Section: 群开关 ===
            this.grpGroupSwitch = new System.Windows.Forms.GroupBox();
            this.chkPinyinBetOnly = new System.Windows.Forms.CheckBox();
            this.rdoChineseBetWithPinyin = new System.Windows.Forms.RadioButton();
            this.txtPinyinExample = new System.Windows.Forms.TextBox();
            this.rdoChineseBetNoPinyin = new System.Windows.Forms.RadioButton();
            this.chkReceiveGroupBet = new System.Windows.Forms.CheckBox();
            this.chkAutoMuteUnmute = new System.Windows.Forms.CheckBox();

            // === Section: 好友开关 ===
            this.grpFriendSwitch = new System.Windows.Forms.GroupBox();
            this.chkEnableFriendChat = new System.Windows.Forms.CheckBox();
            this.chkAutoAgreeFriend = new System.Windows.Forms.CheckBox();
            this.chkOnlyMemberBet = new System.Windows.Forms.CheckBox();
            this.chkFriendBetNotInGroup = new System.Windows.Forms.CheckBox();
            this.chkFriendScoreNotInGroup = new System.Windows.Forms.CheckBox();
            this.chkFriendQueryInGroup = new System.Windows.Forms.CheckBox();

            // === Section: 其他设置 ===
            this.chkGlobalImageSend = new System.Windows.Forms.CheckBox();
            this.lblFontSize = new System.Windows.Forms.Label();
            this.nudFontSize = new System.Windows.Forms.NumericUpDown();
            this.lblPixel = new System.Windows.Forms.Label();
            this.chkHistoryShow = new System.Windows.Forms.CheckBox();
            this.nudHistoryPeriod = new System.Windows.Forms.NumericUpDown();
            this.lblPeriod = new System.Windows.Forms.Label();
            this.chkGlobalDigitLower = new System.Windows.Forms.CheckBox();
            this.chkUpperDigitUse = new System.Windows.Forms.CheckBox();

            // === Save Button ===
            this.btnSaveSettings = new System.Windows.Forms.Button();

            this.SuspendLayout();

            // =====================================================
            // Row 1: 下注设置 checkboxes
            // =====================================================
            int row1Y = 5;

            this.chkAllowModifyBet.Text = "允许改加注";
            this.chkAllowModifyBet.Location = new System.Drawing.Point(10, row1Y);
            this.chkAllowModifyBet.Size = new System.Drawing.Size(85, 18);
            this.chkAllowModifyBet.Checked = true;

            this.chkProhibitCancel.Text = "禁止取消";
            this.chkProhibitCancel.Location = new System.Drawing.Point(100, row1Y);
            this.chkProhibitCancel.Size = new System.Drawing.Size(75, 18);

            this.chkShowBet.Text = "下注显示";
            this.chkShowBet.Location = new System.Drawing.Point(180, row1Y);
            this.chkShowBet.Size = new System.Drawing.Size(75, 18);
            this.chkShowBet.Checked = true;

            this.chkVariableBetLater.Text = "变量先注后分";
            this.chkVariableBetLater.Location = new System.Drawing.Point(260, row1Y);
            this.chkVariableBetLater.Size = new System.Drawing.Size(100, 18);
            this.chkVariableBetLater.Checked = true;

            // Row 2
            int row2Y = 25;

            this.chkAutoProcessBeforeScore.Text = "上分后自动处理之前下注";
            this.chkAutoProcessBeforeScore.Location = new System.Drawing.Point(10, row2Y);
            this.chkAutoProcessBeforeScore.Size = new System.Drawing.Size(165, 18);

            this.chkSendSealBeforeProcess.Text = "发送封盘后上分不用处理之前下注";
            this.chkSendSealBeforeProcess.Location = new System.Drawing.Point(180, row2Y);
            this.chkSendSealBeforeProcess.Size = new System.Drawing.Size(210, 18);

            // =====================================================
            // Row 3: 重复下注不带加或改处理
            // =====================================================
            int row3Y = 48;

            var lblRepeatBet = new System.Windows.Forms.Label();
            lblRepeatBet.Text = "重复下注不带加或改处理";
            lblRepeatBet.Location = new System.Drawing.Point(10, row3Y);
            lblRepeatBet.Size = new System.Drawing.Size(145, 18);

            this.rdoCalcRepeat.Text = "算成加注(不推荐使用)";
            this.rdoCalcRepeat.Location = new System.Drawing.Point(155, row3Y);
            this.rdoCalcRepeat.Size = new System.Drawing.Size(140, 18);

            this.rdoSameNotCalc.Text = "同注不算 不同等于加注";
            this.rdoSameNotCalc.Location = new System.Drawing.Point(300, row3Y);
            this.rdoSameNotCalc.Size = new System.Drawing.Size(155, 18);

            int row4Y = 68;

            this.rdoLastBet.Text = "算最后一次下注(推荐)";
            this.rdoLastBet.Location = new System.Drawing.Point(155, row4Y);
            this.rdoLastBet.Size = new System.Drawing.Size(145, 18);
            this.rdoLastBet.Checked = true;

            this.rdoFirstBet.Text = "算前第一次下注";
            this.rdoFirstBet.Location = new System.Drawing.Point(300, row4Y);
            this.rdoFirstBet.Size = new System.Drawing.Size(115, 18);

            // =====================================================
            // Row 5: 模糊匹配
            // =====================================================
            int row5Y = 93;

            this.chkFuzzyMatch.Text = "模糊匹配下注开/关";
            this.chkFuzzyMatch.Location = new System.Drawing.Point(10, row5Y);
            this.chkFuzzyMatch.Size = new System.Drawing.Size(130, 18);
            this.chkFuzzyMatch.Checked = true;

            this.chkFuzzyMatchSupport.Text = "模糊匹配支持提醒";
            this.chkFuzzyMatchSupport.Location = new System.Drawing.Point(145, row5Y);
            this.chkFuzzyMatchSupport.Size = new System.Drawing.Size(125, 18);
            this.chkFuzzyMatchSupport.Checked = true;

            int row6Y = 113;

            this.chkNoBillRemind.Text = "无账单下注提醒";
            this.chkNoBillRemind.Location = new System.Drawing.Point(10, row6Y);
            this.chkNoBillRemind.Size = new System.Drawing.Size(110, 18);
            this.chkNoBillRemind.Checked = true;

            this.txtNoBillRemindContent.Text = "@QQ 无账单提醒[下注内容]";
            this.txtNoBillRemindContent.Location = new System.Drawing.Point(125, row6Y);
            this.txtNoBillRemindContent.Size = new System.Drawing.Size(200, 21);

            // =====================================================
            // Row 7-10: 组合下注无效
            // =====================================================
            int row7Y = 138;

            this.chkCombinationInvalid.Text = "杀组合下注无效";
            this.chkCombinationInvalid.Location = new System.Drawing.Point(10, row7Y);
            this.chkCombinationInvalid.Size = new System.Drawing.Size(110, 18);
            this.chkCombinationInvalid.Checked = true;

            this.txtCombinationInvalidMsg.Text = "本群不支持相反攻击,攻击无效 请重新攻击!";
            this.txtCombinationInvalidMsg.Location = new System.Drawing.Point(125, row7Y);
            this.txtCombinationInvalidMsg.Size = new System.Drawing.Size(280, 21);

            int row8Y = 163;

            this.chkMultiCombinationInvalid.Text = "多组合下注无效";
            this.chkMultiCombinationInvalid.Location = new System.Drawing.Point(10, row8Y);
            this.chkMultiCombinationInvalid.Size = new System.Drawing.Size(110, 18);
            this.chkMultiCombinationInvalid.Checked = true;

            this.txtMultiCombinationInvalidMsg.Text = "本群不支持多组合攻击,攻击无效 请重新攻击!";
            this.txtMultiCombinationInvalidMsg.Location = new System.Drawing.Point(125, row8Y);
            this.txtMultiCombinationInvalidMsg.Size = new System.Drawing.Size(280, 21);

            int row9Y = 188;

            this.chkSingleOppositeInvalid.Text = "单注反下注无效";
            this.chkSingleOppositeInvalid.Location = new System.Drawing.Point(10, row9Y);
            this.chkSingleOppositeInvalid.Size = new System.Drawing.Size(110, 18);
            this.chkSingleOppositeInvalid.Checked = true;

            this.txtSingleOppositeInvalidMsg.Text = "@qq 本群不支持对下,变相对下攻击无效 请重新";
            this.txtSingleOppositeInvalidMsg.Location = new System.Drawing.Point(125, row9Y);
            this.txtSingleOppositeInvalidMsg.Size = new System.Drawing.Size(280, 21);

            int row10Y = 213;

            this.chkMaxCombination.Text = "最多";
            this.chkMaxCombination.Location = new System.Drawing.Point(10, row10Y);
            this.chkMaxCombination.Size = new System.Drawing.Size(50, 18);
            this.chkMaxCombination.Checked = true;

            this.nudMaxCombination.Value = 3;
            this.nudMaxCombination.Minimum = 1;
            this.nudMaxCombination.Maximum = 10;
            this.nudMaxCombination.Location = new System.Drawing.Point(60, row10Y);
            this.nudMaxCombination.Size = new System.Drawing.Size(45, 21);

            var lblCombinationUnit = new System.Windows.Forms.Label();
            lblCombinationUnit.Text = "个组合";
            lblCombinationUnit.Location = new System.Drawing.Point(108, row10Y + 2);
            lblCombinationUnit.Size = new System.Drawing.Size(45, 18);

            this.txtMaxCombinationMsg.Text = "@qq 本群不支持对下,变相对下攻击无效 请重新";
            this.txtMaxCombinationMsg.Location = new System.Drawing.Point(155, row10Y);
            this.txtMaxCombinationMsg.Size = new System.Drawing.Size(250, 21);

            // =====================================================
            // Row 11-14: 群开关 (Left side)
            // =====================================================
            int row11Y = 243;

            var lblGroupSwitch = new System.Windows.Forms.Label();
            lblGroupSwitch.Text = "群开关";
            lblGroupSwitch.Location = new System.Drawing.Point(10, row11Y);
            lblGroupSwitch.Size = new System.Drawing.Size(45, 18);

            this.chkPinyinBetOnly.Text = "开启仅支持拼音下注";
            this.chkPinyinBetOnly.Location = new System.Drawing.Point(60, row11Y);
            this.chkPinyinBetOnly.Size = new System.Drawing.Size(140, 18);

            int row12Y = 263;

            this.rdoChineseBetWithPinyin.Text = "中文下注有效并且提醒下次拼音下注";
            this.rdoChineseBetWithPinyin.Location = new System.Drawing.Point(10, row12Y);
            this.rdoChineseBetWithPinyin.Size = new System.Drawing.Size(220, 18);
            this.rdoChineseBetWithPinyin.Checked = true;

            this.txtPinyinExample.Text = "请拼音下柱谢谢";
            this.txtPinyinExample.Location = new System.Drawing.Point(235, row12Y);
            this.txtPinyinExample.Size = new System.Drawing.Size(100, 21);

            int row13Y = 283;

            this.rdoChineseBetNoPinyin.Text = "中文下注无效并且提醒下次拼音下注";
            this.rdoChineseBetNoPinyin.Location = new System.Drawing.Point(10, row13Y);
            this.rdoChineseBetNoPinyin.Size = new System.Drawing.Size(220, 18);

            int row14Y = 303;

            this.chkReceiveGroupBet.Text = "接收群聊下注";
            this.chkReceiveGroupBet.Location = new System.Drawing.Point(10, row14Y);
            this.chkReceiveGroupBet.Size = new System.Drawing.Size(100, 18);

            this.chkAutoMuteUnmute.Text = "自动禁言解禁群";
            this.chkAutoMuteUnmute.Location = new System.Drawing.Point(115, row14Y);
            this.chkAutoMuteUnmute.Size = new System.Drawing.Size(110, 18);

            // =====================================================
            // Row 11-14: 好友开关 (Right side)
            // =====================================================
            int rightX = 360;

            var lblFriendSwitch = new System.Windows.Forms.Label();
            lblFriendSwitch.Text = "好友开关";
            lblFriendSwitch.Location = new System.Drawing.Point(rightX, row11Y);
            lblFriendSwitch.Size = new System.Drawing.Size(55, 18);

            this.chkEnableFriendChat.Text = "开启好友私聊下注";
            this.chkEnableFriendChat.Location = new System.Drawing.Point(rightX + 60, row11Y);
            this.chkEnableFriendChat.Size = new System.Drawing.Size(130, 18);

            this.chkAutoAgreeFriend.Text = "自动同意好友添加";
            this.chkAutoAgreeFriend.Location = new System.Drawing.Point(rightX, row12Y);
            this.chkAutoAgreeFriend.Size = new System.Drawing.Size(130, 18);

            this.chkOnlyMemberBet.Text = "只接群成员下注";
            this.chkOnlyMemberBet.Location = new System.Drawing.Point(rightX, row13Y);
            this.chkOnlyMemberBet.Size = new System.Drawing.Size(115, 18);

            this.chkFriendBetNotInGroup.Text = "私聊下注不在群内反馈";
            this.chkFriendBetNotInGroup.Location = new System.Drawing.Point(rightX, row14Y);
            this.chkFriendBetNotInGroup.Size = new System.Drawing.Size(150, 18);

            int row15Y = 323;

            this.chkFriendScoreNotInGroup.Text = "私聊上下分不在群内反馈";
            this.chkFriendScoreNotInGroup.Location = new System.Drawing.Point(rightX, row15Y);
            this.chkFriendScoreNotInGroup.Size = new System.Drawing.Size(160, 18);

            int row16Y = 343;

            this.chkFriendQueryInGroup.Text = "私聊词库『在』群内反馈";
            this.chkFriendQueryInGroup.Location = new System.Drawing.Point(rightX, row16Y);
            this.chkFriendQueryInGroup.Size = new System.Drawing.Size(160, 18);

            // =====================================================
            // Row 15-16: 其他设置 (Left side)
            // =====================================================
            this.chkGlobalImageSend.Text = "全局图片发送,文字大小";
            this.chkGlobalImageSend.Location = new System.Drawing.Point(10, row15Y);
            this.chkGlobalImageSend.Size = new System.Drawing.Size(150, 18);

            this.nudFontSize.Value = 19;
            this.nudFontSize.Minimum = 8;
            this.nudFontSize.Maximum = 72;
            this.nudFontSize.Location = new System.Drawing.Point(165, row15Y);
            this.nudFontSize.Size = new System.Drawing.Size(50, 21);

            this.lblPixel.Text = "像素";
            this.lblPixel.Location = new System.Drawing.Point(218, row15Y + 2);
            this.lblPixel.Size = new System.Drawing.Size(30, 18);

            this.chkHistoryShow.Text = "历史显示";
            this.chkHistoryShow.Location = new System.Drawing.Point(10, row16Y);
            this.chkHistoryShow.Size = new System.Drawing.Size(75, 18);
            this.chkHistoryShow.Checked = true;

            this.nudHistoryPeriod.Value = 11;
            this.nudHistoryPeriod.Minimum = 1;
            this.nudHistoryPeriod.Maximum = 100;
            this.nudHistoryPeriod.Location = new System.Drawing.Point(90, row16Y);
            this.nudHistoryPeriod.Size = new System.Drawing.Size(50, 21);

            this.lblPeriod.Text = "期";
            this.lblPeriod.Location = new System.Drawing.Point(143, row16Y + 2);
            this.lblPeriod.Size = new System.Drawing.Size(20, 18);

            this.chkGlobalDigitLower.Text = "全局数字小写";
            this.chkGlobalDigitLower.Location = new System.Drawing.Point(170, row16Y);
            this.chkGlobalDigitLower.Size = new System.Drawing.Size(100, 18);

            this.chkUpperDigitUse.Text = "大写数字采用①②③";
            this.chkUpperDigitUse.Location = new System.Drawing.Point(275, row16Y);
            this.chkUpperDigitUse.Size = new System.Drawing.Size(140, 18);

            // =====================================================
            // Save Button
            // =====================================================
            this.btnSaveSettings.Text = "保存设置";
            this.btnSaveSettings.Location = new System.Drawing.Point(530, 340);
            this.btnSaveSettings.Size = new System.Drawing.Size(75, 25);

            // =====================================================
            // Add all controls to form
            // =====================================================
            this.Controls.Add(this.chkAllowModifyBet);
            this.Controls.Add(this.chkProhibitCancel);
            this.Controls.Add(this.chkShowBet);
            this.Controls.Add(this.chkVariableBetLater);
            this.Controls.Add(this.chkAutoProcessBeforeScore);
            this.Controls.Add(this.chkSendSealBeforeProcess);

            this.Controls.Add(lblRepeatBet);
            this.Controls.Add(this.rdoCalcRepeat);
            this.Controls.Add(this.rdoSameNotCalc);
            this.Controls.Add(this.rdoLastBet);
            this.Controls.Add(this.rdoFirstBet);

            this.Controls.Add(this.chkFuzzyMatch);
            this.Controls.Add(this.chkFuzzyMatchSupport);
            this.Controls.Add(this.chkNoBillRemind);
            this.Controls.Add(this.txtNoBillRemindContent);

            this.Controls.Add(this.chkCombinationInvalid);
            this.Controls.Add(this.txtCombinationInvalidMsg);
            this.Controls.Add(this.chkMultiCombinationInvalid);
            this.Controls.Add(this.txtMultiCombinationInvalidMsg);
            this.Controls.Add(this.chkSingleOppositeInvalid);
            this.Controls.Add(this.txtSingleOppositeInvalidMsg);
            this.Controls.Add(this.chkMaxCombination);
            this.Controls.Add(this.nudMaxCombination);
            this.Controls.Add(lblCombinationUnit);
            this.Controls.Add(this.txtMaxCombinationMsg);

            this.Controls.Add(lblGroupSwitch);
            this.Controls.Add(this.chkPinyinBetOnly);
            this.Controls.Add(this.rdoChineseBetWithPinyin);
            this.Controls.Add(this.txtPinyinExample);
            this.Controls.Add(this.rdoChineseBetNoPinyin);
            this.Controls.Add(this.chkReceiveGroupBet);
            this.Controls.Add(this.chkAutoMuteUnmute);

            this.Controls.Add(lblFriendSwitch);
            this.Controls.Add(this.chkEnableFriendChat);
            this.Controls.Add(this.chkAutoAgreeFriend);
            this.Controls.Add(this.chkOnlyMemberBet);
            this.Controls.Add(this.chkFriendBetNotInGroup);
            this.Controls.Add(this.chkFriendScoreNotInGroup);
            this.Controls.Add(this.chkFriendQueryInGroup);

            this.Controls.Add(this.chkGlobalImageSend);
            this.Controls.Add(this.nudFontSize);
            this.Controls.Add(this.lblPixel);
            this.Controls.Add(this.chkHistoryShow);
            this.Controls.Add(this.nudHistoryPeriod);
            this.Controls.Add(this.lblPeriod);
            this.Controls.Add(this.chkGlobalDigitLower);
            this.Controls.Add(this.chkUpperDigitUse);

            this.Controls.Add(this.btnSaveSettings);

            // =====================================================
            // BetBasicSettingsControl - Main container
            // =====================================================
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Size = new System.Drawing.Size(620, 380);

            this.ResumeLayout(false);
            this.PerformLayout();
        }

        // Section: 下注设置
        private System.Windows.Forms.GroupBox grpBetSettings;
        private System.Windows.Forms.CheckBox chkAllowModifyBet;
        private System.Windows.Forms.CheckBox chkProhibitCancel;
        private System.Windows.Forms.CheckBox chkShowBet;
        private System.Windows.Forms.CheckBox chkVariableBetLater;
        private System.Windows.Forms.CheckBox chkAutoProcessBeforeScore;
        private System.Windows.Forms.CheckBox chkSendSealBeforeProcess;

        // Section: 重复下注处理
        private System.Windows.Forms.GroupBox grpRepeatBet;
        private System.Windows.Forms.RadioButton rdoCalcRepeat;
        private System.Windows.Forms.RadioButton rdoSameNotCalc;
        private System.Windows.Forms.RadioButton rdoLastBet;
        private System.Windows.Forms.RadioButton rdoFirstBet;

        // Section: 模糊匹配
        private System.Windows.Forms.CheckBox chkFuzzyMatch;
        private System.Windows.Forms.CheckBox chkFuzzyMatchSupport;
        private System.Windows.Forms.CheckBox chkNoBillRemind;
        private System.Windows.Forms.TextBox txtNoBillRemindContent;

        // Section: 组合下注无效
        private System.Windows.Forms.CheckBox chkCombinationInvalid;
        private System.Windows.Forms.TextBox txtCombinationInvalidMsg;
        private System.Windows.Forms.CheckBox chkMultiCombinationInvalid;
        private System.Windows.Forms.TextBox txtMultiCombinationInvalidMsg;
        private System.Windows.Forms.CheckBox chkSingleOppositeInvalid;
        private System.Windows.Forms.TextBox txtSingleOppositeInvalidMsg;
        private System.Windows.Forms.CheckBox chkMaxCombination;
        private System.Windows.Forms.NumericUpDown nudMaxCombination;
        private System.Windows.Forms.TextBox txtMaxCombinationMsg;

        // Section: 群开关
        private System.Windows.Forms.GroupBox grpGroupSwitch;
        private System.Windows.Forms.CheckBox chkPinyinBetOnly;
        private System.Windows.Forms.RadioButton rdoChineseBetWithPinyin;
        private System.Windows.Forms.TextBox txtPinyinExample;
        private System.Windows.Forms.RadioButton rdoChineseBetNoPinyin;
        private System.Windows.Forms.CheckBox chkReceiveGroupBet;
        private System.Windows.Forms.CheckBox chkAutoMuteUnmute;

        // Section: 好友开关
        private System.Windows.Forms.GroupBox grpFriendSwitch;
        private System.Windows.Forms.CheckBox chkEnableFriendChat;
        private System.Windows.Forms.CheckBox chkAutoAgreeFriend;
        private System.Windows.Forms.CheckBox chkOnlyMemberBet;
        private System.Windows.Forms.CheckBox chkFriendBetNotInGroup;
        private System.Windows.Forms.CheckBox chkFriendScoreNotInGroup;
        private System.Windows.Forms.CheckBox chkFriendQueryInGroup;

        // Section: 其他设置
        private System.Windows.Forms.CheckBox chkGlobalImageSend;
        private System.Windows.Forms.Label lblFontSize;
        private System.Windows.Forms.NumericUpDown nudFontSize;
        private System.Windows.Forms.Label lblPixel;
        private System.Windows.Forms.CheckBox chkHistoryShow;
        private System.Windows.Forms.NumericUpDown nudHistoryPeriod;
        private System.Windows.Forms.Label lblPeriod;
        private System.Windows.Forms.CheckBox chkGlobalDigitLower;
        private System.Windows.Forms.CheckBox chkUpperDigitUse;

        // Save Button
        private System.Windows.Forms.Button btnSaveSettings;
    }
}
