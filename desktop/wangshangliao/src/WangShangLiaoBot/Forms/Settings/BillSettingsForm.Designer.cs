namespace WangShangLiaoBot.Forms.Settings
{
    partial class BillSettingsForm
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

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            // 左侧面板 - 开奖发送设置
            this.panelLeft = new System.Windows.Forms.Panel();
            this.grpLotterySend = new System.Windows.Forms.GroupBox();
            this.chkLotteryNotify = new System.Windows.Forms.CheckBox();
            this.chkWith8 = new System.Windows.Forms.CheckBox();
            this.chkImageSend = new System.Windows.Forms.CheckBox();
            this.lblPeriodCount = new System.Windows.Forms.Label();
            this.numPeriodCount = new System.Windows.Forms.NumericUpDown();
            this.lblPeriodUnit = new System.Windows.Forms.Label();
            this.lblMealCode = new System.Windows.Forms.Label();
            this.txtMealCodeFormat = new System.Windows.Forms.TextBox();
            
            // 账单格式
            this.grpBillFormat = new System.Windows.Forms.GroupBox();
            this.lblBillContent = new System.Windows.Forms.Label();
            this.lblBillFormat = new System.Windows.Forms.Label();
            this.lblBillColumns = new System.Windows.Forms.Label();
            this.numBillColumns = new System.Windows.Forms.NumericUpDown();
            this.chkBillImageSend = new System.Windows.Forms.CheckBox();
            this.chkBillSecondReply = new System.Windows.Forms.CheckBox();
            this.lblHistory = new System.Windows.Forms.Label();
            this.txtHistory = new System.Windows.Forms.TextBox();
            
            // 群作业设置
            this.grpGroupTask = new System.Windows.Forms.GroupBox();
            this.chkGroupTaskSend = new System.Windows.Forms.CheckBox();
            this.chkHideLostPlayers = new System.Windows.Forms.CheckBox();
            this.chkKeepZeroScore = new System.Windows.Forms.CheckBox();
            this.chkKeepRecent10 = new System.Windows.Forms.CheckBox();
            this.chkAutoApprovePlayer = new System.Windows.Forms.CheckBox();
            this.lblBillMinDigits = new System.Windows.Forms.Label();
            this.numBillMinDigits = new System.Windows.Forms.NumericUpDown();
            this.lblHideThreshold = new System.Windows.Forms.Label();
            this.numHideThreshold = new System.Windows.Forms.NumericUpDown();
            
            // 账单替换命令
            this.grpBillReplace = new System.Windows.Forms.GroupBox();
            this.txtBillReplaceHelp = new System.Windows.Forms.RichTextBox();
            
            // 右侧面板 - 基本设置
            this.panelRight = new System.Windows.Forms.Panel();
            this.grpBasicSettings = new System.Windows.Forms.GroupBox();
            this.lblAdminIds = new System.Windows.Forms.Label();
            this.txtAdminIds = new System.Windows.Forms.TextBox();
            this.lblAdminTip = new System.Windows.Forms.Label();
            this.lblGroupId = new System.Windows.Forms.Label();
            this.txtGroupId = new System.Windows.Forms.TextBox();
            this.btnSaveAdmin = new System.Windows.Forms.Button();
            this.btnViewInviteLog = new System.Windows.Forms.Button();
            
            // 禁言、核对
            this.grpMuteSettings = new System.Windows.Forms.GroupBox();
            this.lblMuteBefore = new System.Windows.Forms.Label();
            this.numMuteBeforeSeconds = new System.Windows.Forms.NumericUpDown();
            this.lblMuteUnit = new System.Windows.Forms.Label();
            this.chkBetDataTimer = new System.Windows.Forms.CheckBox();
            this.numBetDataDelay = new System.Windows.Forms.NumericUpDown();
            this.lblBetDataUnit = new System.Windows.Forms.Label();
            this.chkBetDataImage = new System.Windows.Forms.CheckBox();
            this.chkGroupTaskNotify = new System.Windows.Forms.CheckBox();
            this.btnSetBetDataContent = new System.Windows.Forms.Button();
            
            // 消息反馈
            this.grpFeedback = new System.Windows.Forms.GroupBox();
            this.lblFeedbackWangWang = new System.Windows.Forms.Label();
            this.txtFeedbackWangWang = new System.Windows.Forms.TextBox();
            this.lblFeedbackGroup = new System.Windows.Forms.Label();
            this.txtFeedbackGroup = new System.Windows.Forms.TextBox();
            this.chkFeedbackToWangWang = new System.Windows.Forms.CheckBox();
            this.chkFeedbackToGroup = new System.Windows.Forms.CheckBox();
            this.chkBetCheckFeedback = new System.Windows.Forms.CheckBox();
            this.chkBetSummaryFeedback = new System.Windows.Forms.CheckBox();
            this.chkProfitFeedback = new System.Windows.Forms.CheckBox();
            this.chkBillSendFeedback = new System.Windows.Forms.CheckBox();
            this.btnSaveAll = new System.Windows.Forms.Button();

            // panelLeft
            this.panelLeft.Location = new System.Drawing.Point(5, 5);
            this.panelLeft.Size = new System.Drawing.Size(340, 520);
            
            // grpLotterySend - 开奖发送
            this.grpLotterySend.Text = "开奖发送";
            this.grpLotterySend.Location = new System.Drawing.Point(5, 5);
            this.grpLotterySend.Size = new System.Drawing.Size(330, 120);
            
            this.chkLotteryNotify.Text = "开奖发送";
            this.chkLotteryNotify.Location = new System.Drawing.Point(10, 20);
            this.chkLotteryNotify.Size = new System.Drawing.Size(80, 20);
            
            this.chkWith8.Text = "带8";
            this.chkWith8.Location = new System.Drawing.Point(100, 20);
            this.chkWith8.Size = new System.Drawing.Size(50, 20);
            
            this.chkImageSend.Text = "图片发送";
            this.chkImageSend.Location = new System.Drawing.Point(160, 20);
            this.chkImageSend.Size = new System.Drawing.Size(80, 20);
            
            this.lblPeriodCount.Text = "[期数]";
            this.lblPeriodCount.Location = new System.Drawing.Point(10, 45);
            this.lblPeriodCount.Size = new System.Drawing.Size(45, 20);
            
            this.numPeriodCount.Location = new System.Drawing.Point(55, 43);
            this.numPeriodCount.Size = new System.Drawing.Size(50, 23);
            this.numPeriodCount.Minimum = 1;
            this.numPeriodCount.Maximum = 100;
            this.numPeriodCount.Value = 21;
            
            this.lblPeriodUnit.Text = "期";
            this.lblPeriodUnit.Location = new System.Drawing.Point(108, 45);
            this.lblPeriodUnit.Size = new System.Drawing.Size(20, 20);
            
            this.lblMealCode.Text = "取餐码:";
            this.lblMealCode.Location = new System.Drawing.Point(10, 70);
            this.lblMealCode.Size = new System.Drawing.Size(50, 20);
            
            this.txtMealCodeFormat.Text = "[一区] + [二区] + [三区] = [开奖号码]";
            this.txtMealCodeFormat.Location = new System.Drawing.Point(60, 68);
            this.txtMealCodeFormat.Size = new System.Drawing.Size(260, 23);
            this.txtMealCodeFormat.ReadOnly = true;
            
            this.grpLotterySend.Controls.Add(this.chkLotteryNotify);
            this.grpLotterySend.Controls.Add(this.chkWith8);
            this.grpLotterySend.Controls.Add(this.chkImageSend);
            this.grpLotterySend.Controls.Add(this.lblPeriodCount);
            this.grpLotterySend.Controls.Add(this.numPeriodCount);
            this.grpLotterySend.Controls.Add(this.lblPeriodUnit);
            this.grpLotterySend.Controls.Add(this.lblMealCode);
            this.grpLotterySend.Controls.Add(this.txtMealCodeFormat);
            
            // grpBillFormat - 账单格式
            this.grpBillFormat.Text = "账单格式";
            this.grpBillFormat.Location = new System.Drawing.Point(5, 130);
            this.grpBillFormat.Size = new System.Drawing.Size(330, 100);
            
            this.lblBillContent.Text = "账单内容↓";
            this.lblBillContent.Location = new System.Drawing.Point(10, 20);
            this.lblBillContent.Size = new System.Drawing.Size(70, 20);
            this.lblBillContent.ForeColor = System.Drawing.Color.Red;
            
            this.lblBillFormat.Text = "账单格式↓";
            this.lblBillFormat.Location = new System.Drawing.Point(85, 20);
            this.lblBillFormat.Size = new System.Drawing.Size(70, 20);
            this.lblBillFormat.ForeColor = System.Drawing.Color.Red;
            
            this.lblBillColumns.Text = "横";
            this.lblBillColumns.Location = new System.Drawing.Point(160, 20);
            this.lblBillColumns.Size = new System.Drawing.Size(20, 20);
            
            this.numBillColumns.Location = new System.Drawing.Point(180, 18);
            this.numBillColumns.Size = new System.Drawing.Size(40, 23);
            this.numBillColumns.Minimum = 1;
            this.numBillColumns.Maximum = 10;
            this.numBillColumns.Value = 4;
            
            this.chkBillImageSend.Text = "图片发送";
            this.chkBillImageSend.Location = new System.Drawing.Point(230, 20);
            this.chkBillImageSend.Size = new System.Drawing.Size(80, 20);
            
            this.chkBillSecondReply.Text = "开发账单秒顺回复";
            this.chkBillSecondReply.Location = new System.Drawing.Point(10, 45);
            this.chkBillSecondReply.Size = new System.Drawing.Size(140, 20);
            
            this.lblHistory.Text = "历史:";
            this.lblHistory.Location = new System.Drawing.Point(10, 70);
            this.lblHistory.Size = new System.Drawing.Size(40, 20);
            
            this.txtHistory.Text = "[开奖历史]";
            this.txtHistory.Location = new System.Drawing.Point(50, 68);
            this.txtHistory.Size = new System.Drawing.Size(270, 23);
            
            this.grpBillFormat.Controls.Add(this.lblBillContent);
            this.grpBillFormat.Controls.Add(this.lblBillFormat);
            this.grpBillFormat.Controls.Add(this.lblBillColumns);
            this.grpBillFormat.Controls.Add(this.numBillColumns);
            this.grpBillFormat.Controls.Add(this.chkBillImageSend);
            this.grpBillFormat.Controls.Add(this.chkBillSecondReply);
            this.grpBillFormat.Controls.Add(this.lblHistory);
            this.grpBillFormat.Controls.Add(this.txtHistory);
            
            // grpGroupTask - 群作业设置
            this.grpGroupTask.Text = "群作业设置";
            this.grpGroupTask.Location = new System.Drawing.Point(5, 235);
            this.grpGroupTask.Size = new System.Drawing.Size(330, 130);
            
            this.chkGroupTaskSend.Text = "群作业账单发送";
            this.chkGroupTaskSend.Location = new System.Drawing.Point(10, 20);
            this.chkGroupTaskSend.Size = new System.Drawing.Size(120, 20);
            
            this.chkHideLostPlayers.Text = "[账单]不显示输光玩家";
            this.chkHideLostPlayers.Location = new System.Drawing.Point(140, 20);
            this.chkHideLostPlayers.Size = new System.Drawing.Size(150, 20);
            
            this.chkKeepZeroScore.Text = "零分不删除账单";
            this.chkKeepZeroScore.Location = new System.Drawing.Point(10, 45);
            this.chkKeepZeroScore.Size = new System.Drawing.Size(120, 20);
            
            this.chkKeepRecent10.Text = "只保留近10期群作业";
            this.chkKeepRecent10.Location = new System.Drawing.Point(140, 45);
            this.chkKeepRecent10.Size = new System.Drawing.Size(150, 20);
            
            this.chkAutoApprovePlayer.Text = "账单玩家进群自动同意";
            this.chkAutoApprovePlayer.Location = new System.Drawing.Point(10, 70);
            this.chkAutoApprovePlayer.Size = new System.Drawing.Size(160, 20);
            
            this.lblBillMinDigits.Text = "账单不足";
            this.lblBillMinDigits.Location = new System.Drawing.Point(180, 70);
            this.lblBillMinDigits.Size = new System.Drawing.Size(55, 20);
            
            this.numBillMinDigits.Location = new System.Drawing.Point(235, 68);
            this.numBillMinDigits.Size = new System.Drawing.Size(40, 23);
            this.numBillMinDigits.Minimum = 1;
            this.numBillMinDigits.Maximum = 10;
            this.numBillMinDigits.Value = 4;
            
            this.lblHideThreshold.Text = "[账单]小于";
            this.lblHideThreshold.Location = new System.Drawing.Point(10, 98);
            this.lblHideThreshold.Size = new System.Drawing.Size(65, 20);
            
            this.numHideThreshold.Location = new System.Drawing.Point(75, 96);
            this.numHideThreshold.Size = new System.Drawing.Size(50, 23);
            this.numHideThreshold.Value = 0;
            
            this.grpGroupTask.Controls.Add(this.chkGroupTaskSend);
            this.grpGroupTask.Controls.Add(this.chkHideLostPlayers);
            this.grpGroupTask.Controls.Add(this.chkKeepZeroScore);
            this.grpGroupTask.Controls.Add(this.chkKeepRecent10);
            this.grpGroupTask.Controls.Add(this.chkAutoApprovePlayer);
            this.grpGroupTask.Controls.Add(this.lblBillMinDigits);
            this.grpGroupTask.Controls.Add(this.numBillMinDigits);
            this.grpGroupTask.Controls.Add(this.lblHideThreshold);
            this.grpGroupTask.Controls.Add(this.numHideThreshold);
            
            // grpBillReplace - 账单替换命令
            this.grpBillReplace.Text = "账单替换命令↓";
            this.grpBillReplace.Location = new System.Drawing.Point(5, 370);
            this.grpBillReplace.Size = new System.Drawing.Size(330, 148);
            
            this.txtBillReplaceHelp.ReadOnly = true;
            this.txtBillReplaceHelp.WordWrap = false;
            this.txtBillReplaceHelp.Location = new System.Drawing.Point(10, 18);
            this.txtBillReplaceHelp.Size = new System.Drawing.Size(310, 122);
            this.txtBillReplaceHelp.Font = new System.Drawing.Font("宋体", 9F);
            this.txtBillReplaceHelp.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtBillReplaceHelp.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Both;
            this.txtBillReplaceHelp.Text = "账单替换命令\r\n" +
                "---------------\r\n" +
                "[一区]  自动替换为开奖第一个号码\r\n" +
                "[二区]  自动替换为开奖第二个号码\r\n" +
                "[三区]  自动替换为开奖第三个号码\r\n" +
                "[开奖号码]  自动替换为开奖总和号码\r\n" +
                "[开奖时间]  自动替换为开奖时间\r\n" +
                "[开奖时间2] 自动替换为开奖时间,小写\r\n" +
                "[期数]      自动替换为开奖期数,加粗数字\r\n" +
                "[期数2]     自动替换为开奖期数,正常数字\r\n" +
                "[开奖历史]  自动替换为开奖历史\r\n" +
                "[账单]      自动替换为账单,张三100 李四100\r\n" +
                "[账单2]     自动替换为账单,张三(123456789$)=100\r\n" +
                "[大小单双]  自动替换为开奖大小单双\r\n" +
                "[客户人数]  自动替换为客户人数\r\n" +
                "[总分数]    自动替换为账单总分数\r\n" +
                "[豹顺对子]  自动替换为豹子顺子对子\r\n" +
                "[龙虎豹]    自动替换为龙虎豹\r\n" +
                "[龙虎历史]  自动替换为龙虎豹历史\r\n" +
                "[09回本]\r\n" +
                "[大小单双2]  自动替换为开奖大小单双,小写\r\n" +
                "[豹顺对子2]  自动替换为豹子顺子对子,小写\r\n" +
                "[龙虎豹2]    自动替换为龙虎豹,小写\r\n" +
                "[豹顺对子3]  自动替换为豹顺对子显示成字母，半杂显示成汉字\r\n" +
                "[龙虎豹3]    自动替换为龙虎豹,汉字\r\n" +
                "[开奖图]     自动替换为开奖走势图\r\n" +
                "[中奖玩家]   自动替换为中奖玩家信息\r\n" +
                "---------------\r\n" +
                "账单内容开奖发送都可以加上以上动态随意更改 有问题反馈";
            
            this.grpBillReplace.Controls.Add(this.txtBillReplaceHelp);
            
            this.panelLeft.Controls.Add(this.grpLotterySend);
            this.panelLeft.Controls.Add(this.grpBillFormat);
            this.panelLeft.Controls.Add(this.grpGroupTask);
            this.panelLeft.Controls.Add(this.grpBillReplace);
            
            // panelRight - 右侧面板
            this.panelRight.Location = new System.Drawing.Point(350, 5);
            this.panelRight.Size = new System.Drawing.Size(340, 520);
            
            // grpBasicSettings - 基本设置
            this.grpBasicSettings.Text = "基本设置";
            this.grpBasicSettings.Location = new System.Drawing.Point(5, 5);
            this.grpBasicSettings.Size = new System.Drawing.Size(330, 130);
            
            this.lblAdminIds.Text = "管理旺旺号:";
            this.lblAdminIds.Location = new System.Drawing.Point(10, 22);
            this.lblAdminIds.Size = new System.Drawing.Size(75, 20);
            
            this.txtAdminIds.Location = new System.Drawing.Point(85, 20);
            this.txtAdminIds.Size = new System.Drawing.Size(235, 23);
            
            this.lblAdminTip.Text = "管理员: 对机器人有绝对的控制权，多个管理号用@分开";
            this.lblAdminTip.Location = new System.Drawing.Point(10, 48);
            this.lblAdminTip.Size = new System.Drawing.Size(310, 20);
            this.lblAdminTip.ForeColor = System.Drawing.Color.Gray;
            
            this.lblGroupId.Text = "绑定群号:";
            this.lblGroupId.Location = new System.Drawing.Point(10, 73);
            this.lblGroupId.Size = new System.Drawing.Size(65, 20);
            
            this.txtGroupId.Location = new System.Drawing.Point(75, 71);
            this.txtGroupId.Size = new System.Drawing.Size(245, 23);
            
            this.btnSaveAdmin.Text = "保存管理号和群号";
            this.btnSaveAdmin.Location = new System.Drawing.Point(10, 100);
            this.btnSaveAdmin.Size = new System.Drawing.Size(120, 25);
            this.btnSaveAdmin.Click += new System.EventHandler(this.btnSaveAdmin_Click);
            
            this.btnViewInviteLog.Text = "查看群成员邀请记录";
            this.btnViewInviteLog.Location = new System.Drawing.Point(140, 100);
            this.btnViewInviteLog.Size = new System.Drawing.Size(130, 25);
            this.btnViewInviteLog.Click += new System.EventHandler(this.btnViewInviteLog_Click);
            
            this.grpBasicSettings.Controls.Add(this.lblAdminIds);
            this.grpBasicSettings.Controls.Add(this.txtAdminIds);
            this.grpBasicSettings.Controls.Add(this.lblAdminTip);
            this.grpBasicSettings.Controls.Add(this.lblGroupId);
            this.grpBasicSettings.Controls.Add(this.txtGroupId);
            this.grpBasicSettings.Controls.Add(this.btnSaveAdmin);
            this.grpBasicSettings.Controls.Add(this.btnViewInviteLog);
            
            // grpMuteSettings - 禁言、核对
            this.grpMuteSettings.Text = "禁言、核对";
            this.grpMuteSettings.Location = new System.Drawing.Point(5, 140);
            this.grpMuteSettings.Size = new System.Drawing.Size(330, 120);
            
            this.lblMuteBefore.Text = "封盘前";
            this.lblMuteBefore.Location = new System.Drawing.Point(10, 22);
            this.lblMuteBefore.Size = new System.Drawing.Size(45, 20);
            
            this.numMuteBeforeSeconds.Location = new System.Drawing.Point(55, 20);
            this.numMuteBeforeSeconds.Size = new System.Drawing.Size(50, 23);
            this.numMuteBeforeSeconds.Value = 2;
            
            this.lblMuteUnit.Text = "秒禁言群(提前禁言)";
            this.lblMuteUnit.Location = new System.Drawing.Point(108, 22);
            this.lblMuteUnit.Size = new System.Drawing.Size(130, 20);
            
            this.chkBetDataTimer.Text = "计时";
            this.chkBetDataTimer.Location = new System.Drawing.Point(10, 48);
            this.chkBetDataTimer.Size = new System.Drawing.Size(50, 20);
            this.chkBetDataTimer.Checked = true;
            
            this.numBetDataDelay.Location = new System.Drawing.Point(60, 46);
            this.numBetDataDelay.Size = new System.Drawing.Size(50, 23);
            this.numBetDataDelay.Value = 10;
            
            this.lblBetDataUnit.Text = "秒发送下注数据到群";
            this.lblBetDataUnit.Location = new System.Drawing.Point(113, 48);
            this.lblBetDataUnit.Size = new System.Drawing.Size(130, 20);
            
            this.chkBetDataImage.Text = "图片发送";
            this.chkBetDataImage.Location = new System.Drawing.Point(10, 73);
            this.chkBetDataImage.Size = new System.Drawing.Size(80, 20);
            
            this.chkGroupTaskNotify.Text = "群作业发送";
            this.chkGroupTaskNotify.Location = new System.Drawing.Point(100, 73);
            this.chkGroupTaskNotify.Size = new System.Drawing.Size(90, 20);
            
            this.btnSetBetDataContent.Text = "设置下注数据内容";
            this.btnSetBetDataContent.Location = new System.Drawing.Point(200, 70);
            this.btnSetBetDataContent.Size = new System.Drawing.Size(120, 25);
            this.btnSetBetDataContent.Click += new System.EventHandler(this.btnSetBetDataContent_Click);
            
            this.grpMuteSettings.Controls.Add(this.lblMuteBefore);
            this.grpMuteSettings.Controls.Add(this.numMuteBeforeSeconds);
            this.grpMuteSettings.Controls.Add(this.lblMuteUnit);
            this.grpMuteSettings.Controls.Add(this.chkBetDataTimer);
            this.grpMuteSettings.Controls.Add(this.numBetDataDelay);
            this.grpMuteSettings.Controls.Add(this.lblBetDataUnit);
            this.grpMuteSettings.Controls.Add(this.chkBetDataImage);
            this.grpMuteSettings.Controls.Add(this.chkGroupTaskNotify);
            this.grpMuteSettings.Controls.Add(this.btnSetBetDataContent);
            
            // grpFeedback - 消息反馈
            this.grpFeedback.Text = "消息反馈";
            this.grpFeedback.Location = new System.Drawing.Point(5, 265);
            this.grpFeedback.Size = new System.Drawing.Size(330, 200);
            
            this.lblFeedbackWangWang.Text = "旺旺号:";
            this.lblFeedbackWangWang.Location = new System.Drawing.Point(10, 22);
            this.lblFeedbackWangWang.Size = new System.Drawing.Size(50, 20);
            
            this.txtFeedbackWangWang.Location = new System.Drawing.Point(60, 20);
            this.txtFeedbackWangWang.Size = new System.Drawing.Size(260, 23);
            
            this.lblFeedbackGroup.Text = "群号:";
            this.lblFeedbackGroup.Location = new System.Drawing.Point(10, 48);
            this.lblFeedbackGroup.Size = new System.Drawing.Size(40, 20);
            
            this.txtFeedbackGroup.Location = new System.Drawing.Point(50, 46);
            this.txtFeedbackGroup.Size = new System.Drawing.Size(270, 23);
            
            this.chkFeedbackToWangWang.Text = "反馈到旺旺";
            this.chkFeedbackToWangWang.Location = new System.Drawing.Point(10, 75);
            this.chkFeedbackToWangWang.Size = new System.Drawing.Size(90, 20);
            
            this.chkFeedbackToGroup.Text = "反馈到群里";
            this.chkFeedbackToGroup.Location = new System.Drawing.Point(110, 75);
            this.chkFeedbackToGroup.Size = new System.Drawing.Size(90, 20);
            
            this.chkBetCheckFeedback.Text = "下注核对反馈";
            this.chkBetCheckFeedback.Location = new System.Drawing.Point(10, 100);
            this.chkBetCheckFeedback.Size = new System.Drawing.Size(100, 20);
            
            this.chkBetSummaryFeedback.Text = "下注汇总反馈";
            this.chkBetSummaryFeedback.Location = new System.Drawing.Point(120, 100);
            this.chkBetSummaryFeedback.Size = new System.Drawing.Size(100, 20);
            
            this.chkProfitFeedback.Text = "开奖盈利反馈";
            this.chkProfitFeedback.Location = new System.Drawing.Point(10, 125);
            this.chkProfitFeedback.Size = new System.Drawing.Size(100, 20);
            
            this.chkBillSendFeedback.Text = "发送账单反馈";
            this.chkBillSendFeedback.Location = new System.Drawing.Point(120, 125);
            this.chkBillSendFeedback.Size = new System.Drawing.Size(100, 20);
            
            this.btnSaveAll.Text = "保存设置";
            this.btnSaveAll.Location = new System.Drawing.Point(10, 160);
            this.btnSaveAll.Size = new System.Drawing.Size(310, 30);
            this.btnSaveAll.Click += new System.EventHandler(this.btnSaveAll_Click);
            
            this.grpFeedback.Controls.Add(this.lblFeedbackWangWang);
            this.grpFeedback.Controls.Add(this.txtFeedbackWangWang);
            this.grpFeedback.Controls.Add(this.lblFeedbackGroup);
            this.grpFeedback.Controls.Add(this.txtFeedbackGroup);
            this.grpFeedback.Controls.Add(this.chkFeedbackToWangWang);
            this.grpFeedback.Controls.Add(this.chkFeedbackToGroup);
            this.grpFeedback.Controls.Add(this.chkBetCheckFeedback);
            this.grpFeedback.Controls.Add(this.chkBetSummaryFeedback);
            this.grpFeedback.Controls.Add(this.chkProfitFeedback);
            this.grpFeedback.Controls.Add(this.chkBillSendFeedback);
            this.grpFeedback.Controls.Add(this.btnSaveAll);
            
            this.panelRight.Controls.Add(this.grpBasicSettings);
            this.panelRight.Controls.Add(this.grpMuteSettings);
            this.panelRight.Controls.Add(this.grpFeedback);
            
            // BillSettingsForm
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(700, 530);
            this.Controls.Add(this.panelLeft);
            this.Controls.Add(this.panelRight);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "BillSettingsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "账单设置";
            
            ((System.ComponentModel.ISupportInitialize)(this.numPeriodCount)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numBillColumns)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numBillMinDigits)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numHideThreshold)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numMuteBeforeSeconds)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numBetDataDelay)).EndInit();
            
            this.grpLotterySend.ResumeLayout(false);
            this.grpBillFormat.ResumeLayout(false);
            this.grpGroupTask.ResumeLayout(false);
            this.grpBillReplace.ResumeLayout(false);
            this.grpBasicSettings.ResumeLayout(false);
            this.grpMuteSettings.ResumeLayout(false);
            this.grpFeedback.ResumeLayout(false);
            this.panelLeft.ResumeLayout(false);
            this.panelRight.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion

        // 左侧面板
        private System.Windows.Forms.Panel panelLeft;
        private System.Windows.Forms.GroupBox grpLotterySend;
        private System.Windows.Forms.CheckBox chkLotteryNotify;
        private System.Windows.Forms.CheckBox chkWith8;
        private System.Windows.Forms.CheckBox chkImageSend;
        private System.Windows.Forms.Label lblPeriodCount;
        private System.Windows.Forms.NumericUpDown numPeriodCount;
        private System.Windows.Forms.Label lblPeriodUnit;
        private System.Windows.Forms.Label lblMealCode;
        private System.Windows.Forms.TextBox txtMealCodeFormat;
        
        private System.Windows.Forms.GroupBox grpBillFormat;
        private System.Windows.Forms.Label lblBillContent;
        private System.Windows.Forms.Label lblBillFormat;
        private System.Windows.Forms.Label lblBillColumns;
        private System.Windows.Forms.NumericUpDown numBillColumns;
        private System.Windows.Forms.CheckBox chkBillImageSend;
        private System.Windows.Forms.CheckBox chkBillSecondReply;
        private System.Windows.Forms.Label lblHistory;
        private System.Windows.Forms.TextBox txtHistory;
        
        private System.Windows.Forms.GroupBox grpGroupTask;
        private System.Windows.Forms.CheckBox chkGroupTaskSend;
        private System.Windows.Forms.CheckBox chkHideLostPlayers;
        private System.Windows.Forms.CheckBox chkKeepZeroScore;
        private System.Windows.Forms.CheckBox chkKeepRecent10;
        private System.Windows.Forms.CheckBox chkAutoApprovePlayer;
        private System.Windows.Forms.Label lblBillMinDigits;
        private System.Windows.Forms.NumericUpDown numBillMinDigits;
        private System.Windows.Forms.Label lblHideThreshold;
        private System.Windows.Forms.NumericUpDown numHideThreshold;
        
        private System.Windows.Forms.GroupBox grpBillReplace;
        private System.Windows.Forms.RichTextBox txtBillReplaceHelp;
        
        // 右侧面板
        private System.Windows.Forms.Panel panelRight;
        private System.Windows.Forms.GroupBox grpBasicSettings;
        private System.Windows.Forms.Label lblAdminIds;
        private System.Windows.Forms.TextBox txtAdminIds;
        private System.Windows.Forms.Label lblAdminTip;
        private System.Windows.Forms.Label lblGroupId;
        private System.Windows.Forms.TextBox txtGroupId;
        private System.Windows.Forms.Button btnSaveAdmin;
        private System.Windows.Forms.Button btnViewInviteLog;
        
        private System.Windows.Forms.GroupBox grpMuteSettings;
        private System.Windows.Forms.Label lblMuteBefore;
        private System.Windows.Forms.NumericUpDown numMuteBeforeSeconds;
        private System.Windows.Forms.Label lblMuteUnit;
        private System.Windows.Forms.CheckBox chkBetDataTimer;
        private System.Windows.Forms.NumericUpDown numBetDataDelay;
        private System.Windows.Forms.Label lblBetDataUnit;
        private System.Windows.Forms.CheckBox chkBetDataImage;
        private System.Windows.Forms.CheckBox chkGroupTaskNotify;
        private System.Windows.Forms.Button btnSetBetDataContent;
        
        private System.Windows.Forms.GroupBox grpFeedback;
        private System.Windows.Forms.Label lblFeedbackWangWang;
        private System.Windows.Forms.TextBox txtFeedbackWangWang;
        private System.Windows.Forms.Label lblFeedbackGroup;
        private System.Windows.Forms.TextBox txtFeedbackGroup;
        private System.Windows.Forms.CheckBox chkFeedbackToWangWang;
        private System.Windows.Forms.CheckBox chkFeedbackToGroup;
        private System.Windows.Forms.CheckBox chkBetCheckFeedback;
        private System.Windows.Forms.CheckBox chkBetSummaryFeedback;
        private System.Windows.Forms.CheckBox chkProfitFeedback;
        private System.Windows.Forms.CheckBox chkBillSendFeedback;
        private System.Windows.Forms.Button btnSaveAll;
    }
}

