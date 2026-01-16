namespace WangShangLiaoBot.Forms
{
    partial class MainForm
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
            // ===== Menu Strip =====
            this.menuStrip = new System.Windows.Forms.MenuStrip();
            this.menuCustomer = new System.Windows.Forms.ToolStripMenuItem();
            this.menuScoreSettings = new System.Windows.Forms.ToolStripMenuItem();
            this.menuLockSettings = new System.Windows.Forms.ToolStripMenuItem();
            this.menuRebateTools = new System.Windows.Forms.ToolStripMenuItem();
            this.menuAbout = new System.Windows.Forms.ToolStripMenuItem();
            this.menuTestConnection = new System.Windows.Forms.ToolStripMenuItem();
            this.menuRunLog = new System.Windows.Forms.ToolStripMenuItem();
            this.menuAccountList = new System.Windows.Forms.ToolStripMenuItem();
            this.menuSystemSettings = new System.Windows.Forms.ToolStripMenuItem();
            this.menuYxProxy = new System.Windows.Forms.ToolStripMenuItem();
            
            // ===== Top Toolbar =====
            this.panelTopBar = new System.Windows.Forms.Panel();
            this.lblConnectionStatus = new System.Windows.Forms.Label();
            this.btnScoreWindow = new System.Windows.Forms.Button();
            this.btnViewAccount = new System.Windows.Forms.Button();
            this.lblF10Tip = new System.Windows.Forms.Label();
            
            // ===== Left Panel - Lottery Info =====
            this.panelLeft = new System.Windows.Forms.Panel();
            this.lblCurrentPeriod = new System.Windows.Forms.Label();
            this.lblPeriodNumber = new System.Windows.Forms.Label();
            this.lblCountdownTitle = new System.Windows.Forms.Label();
            this.lblCountdown = new System.Windows.Forms.Label();
            this.lblResult1 = new System.Windows.Forms.Label();
            this.lblPlus1 = new System.Windows.Forms.Label();
            this.lblResult2 = new System.Windows.Forms.Label();
            this.lblPlus2 = new System.Windows.Forms.Label();
            this.lblResult3 = new System.Windows.Forms.Label();
            this.lblEquals = new System.Windows.Forms.Label();
            this.lblResultSum = new System.Windows.Forms.Label();
            this.lblNextPeriod = new System.Windows.Forms.Label();
            this.lblNextPeriodNumber = new System.Windows.Forms.Label();
            this.lblNoLotteryTip = new System.Windows.Forms.Label();
            this.btnRefreshLottery = new System.Windows.Forms.Button();
            this.btnManualCalc = new System.Windows.Forms.Button();
            
            // ===== Middle Panel - Buttons =====
            this.panelMiddle = new System.Windows.Forms.Panel();
            this.btnSendBill = new System.Windows.Forms.Button();
            this.btnImportBill = new System.Windows.Forms.Button();
            this.btnImportBet = new System.Windows.Forms.Button();
            this.cmbLotterySelect = new System.Windows.Forms.ComboBox();
            this.cmbCountry = new System.Windows.Forms.ComboBox();
            this.btnCopyBill = new System.Windows.Forms.Button();
            this.btnClearBet = new System.Windows.Forms.Button();
            this.btnFixLottery = new System.Windows.Forms.Button();
            this.cmbChannel = new System.Windows.Forms.ComboBox();
            this.lblChannelBackup = new System.Windows.Forms.Label();
            this.btnBetSummary = new System.Windows.Forms.Button();
            this.btnClearZero = new System.Windows.Forms.Button();
            this.btnExportBill = new System.Windows.Forms.Button();
            this.btnStopCalc = new System.Windows.Forms.Button();
            this.btnDetailProfit = new System.Windows.Forms.Button();
            this.btnDeleteBill = new System.Windows.Forms.Button();
            this.btnHistoryBill = new System.Windows.Forms.Button();
            this.btnMuteAll = new System.Windows.Forms.Button();
            this.btnUnmuteAll = new System.Windows.Forms.Button();
            
            // ===== Right Panel - Checkboxes =====
            this.panelRight = new System.Windows.Forms.Panel();
            this.chkMuteGroup = new System.Windows.Forms.CheckBox();
            this.chkStopAfterPeriod = new System.Windows.Forms.CheckBox();
            this.chkSupportNickChange = new System.Windows.Forms.CheckBox();
            this.btnSyncTime = new System.Windows.Forms.Button();
            this.btnChatLog = new System.Windows.Forms.Button();
            
            // ===== Player Info Panel =====
            this.panelPlayerInfo = new System.Windows.Forms.Panel();
            this.lblWangWangId = new System.Windows.Forms.Label();
            this.txtWangWangId = new System.Windows.Forms.TextBox();
            this.lblNickname = new System.Windows.Forms.Label();
            this.txtNickname = new System.Windows.Forms.TextBox();
            this.lblScore = new System.Windows.Forms.Label();
            this.txtScore = new System.Windows.Forms.TextBox();
            this.btnModifyInfo = new System.Windows.Forms.Button();
            this.btnSearchPlayer = new System.Windows.Forms.Button();
            this.chkShowTuoPlayer = new System.Windows.Forms.CheckBox();
            this.lblClientBox = new System.Windows.Forms.Label();
            this.txtClientBox = new System.Windows.Forms.TextBox();
            this.rdoAdd10 = new System.Windows.Forms.RadioButton();
            this.rdoSub10 = new System.Windows.Forms.RadioButton();
            
            // ===== Player List =====
            this.listPlayers = new System.Windows.Forms.ListView();
            
            // ===== Status Strip =====
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.lblStatus = new System.Windows.Forms.ToolStripStatusLabel();
            
            this.menuStrip.SuspendLayout();
            this.panelTopBar.SuspendLayout();
            this.panelLeft.SuspendLayout();
            this.panelMiddle.SuspendLayout();
            this.panelRight.SuspendLayout();
            this.panelPlayerInfo.SuspendLayout();
            this.statusStrip.SuspendLayout();
            this.SuspendLayout();
            
            // ===== Menu Strip Configuration =====
            this.menuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.menuCustomer, this.menuScoreSettings, this.menuLockSettings, 
                this.menuRebateTools, this.menuRunLog, this.menuAccountList, 
                this.menuSystemSettings, this.menuTestConnection, this.menuAbout
            });
            // YX代理菜单已移除 - 不再需要，已改用NIM SDK直连
            this.menuStrip.Location = new System.Drawing.Point(0, 0);
            this.menuStrip.Size = new System.Drawing.Size(680, 25);
            this.menuStrip.BackColor = System.Drawing.SystemColors.Control;
            
            this.menuCustomer.Text = "客户管理";
            this.menuCustomer.Click += new System.EventHandler(this.menuCustomer_Click);
            this.menuScoreSettings.Text = "算账设置";
            this.menuScoreSettings.Click += new System.EventHandler(this.menuScoreSettings_Click);
            this.menuLockSettings.Text = "封盘设置";
            this.menuLockSettings.Click += new System.EventHandler(this.menuLockSettings_Click);
            this.menuRebateTools.Text = "回水工具";
            this.menuRebateTools.Click += new System.EventHandler(this.menuRebateTools_Click);
            this.menuAbout.Text = "关于";
            this.menuTestConnection.Text = "测试连接";
            this.menuTestConnection.Click += new System.EventHandler(this.menuTestConnection_Click);
            this.menuRunLog.Text = "运行日志";
            this.menuRunLog.Click += new System.EventHandler(this.menuRunLog_Click);
            this.menuAccountList.Text = "账号列表";
            this.menuAccountList.Click += new System.EventHandler(this.menuAccountList_Click);
            this.menuSystemSettings.Text = "系统设置";
            this.menuSystemSettings.Click += new System.EventHandler(this.menuSystemSettings_Click);
            this.menuYxProxy.Text = "YX代理";
            this.menuYxProxy.Click += new System.EventHandler(this.menuYxProxy_Click);
            
            // ===== Top Toolbar Configuration =====
            this.panelTopBar.BackColor = System.Drawing.SystemColors.Control;
            this.panelTopBar.Controls.Add(this.lblConnectionStatus);
            this.panelTopBar.Controls.Add(this.btnScoreWindow);
            this.panelTopBar.Controls.Add(this.btnViewAccount);
            this.panelTopBar.Controls.Add(this.lblF10Tip);
            this.panelTopBar.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelTopBar.Location = new System.Drawing.Point(0, 25);
            this.panelTopBar.Size = new System.Drawing.Size(810, 28);
            
            this.lblConnectionStatus.Text = "未连框架";
            this.lblConnectionStatus.ForeColor = System.Drawing.Color.Red;
            this.lblConnectionStatus.Location = new System.Drawing.Point(5, 5);
            this.lblConnectionStatus.AutoSize = true;
            this.lblConnectionStatus.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblConnectionStatus.Padding = new System.Windows.Forms.Padding(3, 1, 3, 1);
            this.lblConnectionStatus.Cursor = System.Windows.Forms.Cursors.Hand;
            this.lblConnectionStatus.Click += new System.EventHandler(this.lblConnectionStatus_Click);
            
            this.btnScoreWindow.Text = "上下分窗";
            this.btnScoreWindow.Location = new System.Drawing.Point(80, 2);
            this.btnScoreWindow.Size = new System.Drawing.Size(70, 23);
            this.btnScoreWindow.Click += new System.EventHandler(this.btnScoreWindow_Click);
            
            this.btnViewAccount.Text = "查看账号信息";
            this.btnViewAccount.Location = new System.Drawing.Point(155, 2);
            this.btnViewAccount.Size = new System.Drawing.Size(90, 23);
            this.btnViewAccount.Click += new System.EventHandler(this.btnViewAccount_Click);
            
            this.lblF10Tip.Text = "F10隐藏显示窗口";
            this.lblF10Tip.Location = new System.Drawing.Point(560, 5);
            this.lblF10Tip.AutoSize = true;
            this.lblF10Tip.ForeColor = System.Drawing.Color.Blue;
            this.lblF10Tip.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            
            // ===== Left Panel - Lottery Info Configuration =====
            this.panelLeft.BackColor = System.Drawing.SystemColors.Control;
            this.panelLeft.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelLeft.Location = new System.Drawing.Point(5, 56);
            this.panelLeft.Size = new System.Drawing.Size(150, 148);
            this.panelLeft.Controls.Add(this.lblCurrentPeriod);
            this.panelLeft.Controls.Add(this.lblPeriodNumber);
            this.panelLeft.Controls.Add(this.lblCountdownTitle);
            this.panelLeft.Controls.Add(this.lblCountdown);
            this.panelLeft.Controls.Add(this.lblResult1);
            this.panelLeft.Controls.Add(this.lblPlus1);
            this.panelLeft.Controls.Add(this.lblResult2);
            this.panelLeft.Controls.Add(this.lblPlus2);
            this.panelLeft.Controls.Add(this.lblResult3);
            this.panelLeft.Controls.Add(this.lblEquals);
            this.panelLeft.Controls.Add(this.lblResultSum);
            this.panelLeft.Controls.Add(this.lblNextPeriod);
            this.panelLeft.Controls.Add(this.lblNextPeriodNumber);
            this.panelLeft.Controls.Add(this.lblNoLotteryTip);
            this.panelLeft.Controls.Add(this.btnRefreshLottery);
            this.panelLeft.Controls.Add(this.btnManualCalc);
            
            // Current period
            this.lblCurrentPeriod.Text = "本期";
            this.lblCurrentPeriod.Location = new System.Drawing.Point(3, 3);
            this.lblCurrentPeriod.AutoSize = true;
            
            this.lblPeriodNumber.Text = "3373708";
            this.lblPeriodNumber.ForeColor = System.Drawing.Color.Blue;
            this.lblPeriodNumber.Location = new System.Drawing.Point(30, 3);
            this.lblPeriodNumber.AutoSize = true;
            
            // Countdown
            this.lblCountdownTitle.Text = "倒计时";
            this.lblCountdownTitle.Location = new System.Drawing.Point(3, 22);
            this.lblCountdownTitle.AutoSize = true;
            
            this.lblCountdown.Text = "115";
            this.lblCountdown.ForeColor = System.Drawing.Color.Red;
            this.lblCountdown.Font = new System.Drawing.Font("Arial", 14F, System.Drawing.FontStyle.Bold);
            this.lblCountdown.Location = new System.Drawing.Point(50, 18);
            this.lblCountdown.AutoSize = true;
            
            // Results display
            this.lblResult1.Text = "1";
            this.lblResult1.ForeColor = System.Drawing.Color.Red;
            this.lblResult1.Font = new System.Drawing.Font("Arial", 16F, System.Drawing.FontStyle.Bold);
            this.lblResult1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblResult1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblResult1.Location = new System.Drawing.Point(3, 45);
            this.lblResult1.Size = new System.Drawing.Size(22, 25);
            
            this.lblPlus1.Text = "+";
            this.lblPlus1.Location = new System.Drawing.Point(26, 50);
            this.lblPlus1.AutoSize = true;
            
            this.lblResult2.Text = "1";
            this.lblResult2.ForeColor = System.Drawing.Color.Red;
            this.lblResult2.Font = new System.Drawing.Font("Arial", 16F, System.Drawing.FontStyle.Bold);
            this.lblResult2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblResult2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblResult2.Location = new System.Drawing.Point(38, 45);
            this.lblResult2.Size = new System.Drawing.Size(22, 25);
            
            this.lblPlus2.Text = "+";
            this.lblPlus2.Location = new System.Drawing.Point(61, 50);
            this.lblPlus2.AutoSize = true;
            
            this.lblResult3.Text = "3";
            this.lblResult3.ForeColor = System.Drawing.Color.Red;
            this.lblResult3.Font = new System.Drawing.Font("Arial", 16F, System.Drawing.FontStyle.Bold);
            this.lblResult3.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblResult3.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblResult3.Location = new System.Drawing.Point(73, 45);
            this.lblResult3.Size = new System.Drawing.Size(22, 25);
            
            this.lblEquals.Text = "=";
            this.lblEquals.Location = new System.Drawing.Point(96, 50);
            this.lblEquals.AutoSize = true;
            
            this.lblResultSum.Text = "14";
            this.lblResultSum.ForeColor = System.Drawing.Color.Blue;
            this.lblResultSum.Font = new System.Drawing.Font("Arial", 14F, System.Drawing.FontStyle.Bold);
            this.lblResultSum.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblResultSum.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblResultSum.Location = new System.Drawing.Point(108, 45);
            this.lblResultSum.Size = new System.Drawing.Size(36, 25);
            
            // Next period
            this.lblNextPeriod.Text = "下期";
            this.lblNextPeriod.ForeColor = System.Drawing.Color.Red;
            this.lblNextPeriod.Location = new System.Drawing.Point(3, 75);
            this.lblNextPeriod.AutoSize = true;
            
            this.lblNextPeriodNumber.Text = "3373709";
            this.lblNextPeriodNumber.ForeColor = System.Drawing.Color.Red;
            this.lblNextPeriodNumber.Location = new System.Drawing.Point(30, 75);
            this.lblNextPeriodNumber.AutoSize = true;
            
            this.lblNoLotteryTip.Text = "不开奖时点这里";
            this.lblNoLotteryTip.ForeColor = System.Drawing.Color.Blue;
            this.lblNoLotteryTip.Location = new System.Drawing.Point(3, 95);
            this.lblNoLotteryTip.AutoSize = true;
            this.lblNoLotteryTip.Cursor = System.Windows.Forms.Cursors.Hand;
            
            this.btnRefreshLottery.Text = "刷新开奖";
            this.btnRefreshLottery.Location = new System.Drawing.Point(3, 115);
            this.btnRefreshLottery.Size = new System.Drawing.Size(60, 25);
            this.btnRefreshLottery.Click += new System.EventHandler(this.btnRefreshLottery_Click);
            
            this.btnManualCalc.Text = "手动算账";
            this.btnManualCalc.Location = new System.Drawing.Point(68, 115);
            this.btnManualCalc.Size = new System.Drawing.Size(60, 25);
            this.btnManualCalc.Click += new System.EventHandler(this.btnManualCalc_Click);
            
            // ===== Middle Panel - Buttons Configuration =====
            this.panelMiddle.BackColor = System.Drawing.SystemColors.Control;
            this.panelMiddle.Location = new System.Drawing.Point(195, 56);
            this.panelMiddle.Size = new System.Drawing.Size(360, 120);
            this.panelMiddle.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left;
            
            // Row 1
            this.btnSendBill.Text = "发送账单";
            this.btnSendBill.Location = new System.Drawing.Point(3, 3);
            this.btnSendBill.Size = new System.Drawing.Size(65, 23);
            
            this.btnImportBill.Text = "导入账单";
            this.btnImportBill.Location = new System.Drawing.Point(70, 3);
            this.btnImportBill.Size = new System.Drawing.Size(65, 23);
            
            this.btnImportBet.Text = "导入下注";
            this.btnImportBet.Location = new System.Drawing.Point(137, 3);
            this.btnImportBet.Size = new System.Drawing.Size(65, 23);
            
            this.cmbLotterySelect.Text = "开奖选择";
            this.cmbLotterySelect.Location = new System.Drawing.Point(204, 3);
            this.cmbLotterySelect.Size = new System.Drawing.Size(70, 23);
            this.cmbLotterySelect.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbLotterySelect.Items.AddRange(new object[] { "开奖选择", "手动开奖", "自动开奖" });
            this.cmbLotterySelect.SelectedIndex = 0;
            
            this.cmbCountry.Text = "加拿大";
            this.cmbCountry.Location = new System.Drawing.Point(276, 3);
            this.cmbCountry.Size = new System.Drawing.Size(80, 23);
            this.cmbCountry.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbCountry.Items.AddRange(new object[] { "加拿大", "澳洲", "新西兰" });
            this.cmbCountry.SelectedIndex = 0;
            
            // Row 2
            this.btnCopyBill.Text = "复制账单";
            this.btnCopyBill.Location = new System.Drawing.Point(3, 29);
            this.btnCopyBill.Size = new System.Drawing.Size(65, 23);
            
            this.btnClearBet.Text = "清空下注";
            this.btnClearBet.Location = new System.Drawing.Point(70, 29);
            this.btnClearBet.Size = new System.Drawing.Size(65, 23);
            
            this.btnFixLottery.Text = "修正开奖";
            this.btnFixLottery.Location = new System.Drawing.Point(137, 29);
            this.btnFixLottery.Size = new System.Drawing.Size(65, 23);
            
            this.cmbChannel.Text = "通道";
            this.cmbChannel.Location = new System.Drawing.Point(204, 29);
            this.cmbChannel.Size = new System.Drawing.Size(70, 23);
            this.cmbChannel.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbChannel.Items.AddRange(new object[] { "通道", "通道1", "通道2", "通道3" });
            this.cmbChannel.SelectedIndex = 0;
            
            this.lblChannelBackup.Text = "通道3备用";
            this.lblChannelBackup.Location = new System.Drawing.Point(276, 32);
            this.lblChannelBackup.AutoSize = true;
            
            // Row 3
            this.btnBetSummary.Text = "下注汇总";
            this.btnBetSummary.Location = new System.Drawing.Point(3, 55);
            this.btnBetSummary.Size = new System.Drawing.Size(65, 23);
            
            this.btnClearZero.Text = "清除零分";
            this.btnClearZero.Location = new System.Drawing.Point(70, 55);
            this.btnClearZero.Size = new System.Drawing.Size(65, 23);
            
            this.btnExportBill.Text = "导出账单";
            this.btnExportBill.Location = new System.Drawing.Point(137, 55);
            this.btnExportBill.Size = new System.Drawing.Size(65, 23);
            this.btnExportBill.Click += new System.EventHandler(this.btnExportBill_Click);
            
            this.btnStopCalc.Text = "开始算账";
            this.btnStopCalc.Location = new System.Drawing.Point(204, 55);
            this.btnStopCalc.Size = new System.Drawing.Size(65, 23);
            this.btnStopCalc.BackColor = System.Drawing.Color.LightGreen;
            this.btnStopCalc.Click += new System.EventHandler(this.btnStopCalc_Click);
            
            // Row 4
            this.btnDetailProfit.Text = "详细盈利";
            this.btnDetailProfit.Location = new System.Drawing.Point(3, 81);
            this.btnDetailProfit.Size = new System.Drawing.Size(65, 23);
            
            this.btnDeleteBill.Text = "删除账单";
            this.btnDeleteBill.Location = new System.Drawing.Point(70, 81);
            this.btnDeleteBill.Size = new System.Drawing.Size(65, 23);
            
            this.btnHistoryBill.Text = "历史账单";
            this.btnHistoryBill.Location = new System.Drawing.Point(137, 81);
            this.btnHistoryBill.Size = new System.Drawing.Size(65, 23);
            
            this.btnMuteAll.Text = "全体禁言";
            this.btnMuteAll.Location = new System.Drawing.Point(204, 81);
            this.btnMuteAll.Size = new System.Drawing.Size(65, 23);
            
            this.btnUnmuteAll.Text = "全体解禁";
            this.btnUnmuteAll.Location = new System.Drawing.Point(271, 81);
            this.btnUnmuteAll.Size = new System.Drawing.Size(65, 23);
            
            this.panelMiddle.Controls.AddRange(new System.Windows.Forms.Control[] {
                this.btnSendBill, this.btnImportBill, this.btnImportBet, 
                this.cmbLotterySelect, this.cmbCountry,
                this.btnCopyBill, this.btnClearBet, this.btnFixLottery, 
                this.cmbChannel, this.lblChannelBackup,
                this.btnBetSummary, this.btnClearZero, this.btnExportBill, this.btnStopCalc,
                this.btnDetailProfit, this.btnDeleteBill, this.btnHistoryBill, 
                this.btnMuteAll, this.btnUnmuteAll
            });
            
            // ===== Right Panel - Checkboxes Configuration =====
            this.panelRight.BackColor = System.Drawing.SystemColors.Control;
            this.panelRight.Location = new System.Drawing.Point(560, 56);
            this.panelRight.Size = new System.Drawing.Size(165, 120);
            this.panelRight.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            this.panelRight.Controls.Add(this.chkMuteGroup);
            this.panelRight.Controls.Add(this.chkStopAfterPeriod);
            this.panelRight.Controls.Add(this.chkSupportNickChange);
            this.panelRight.Controls.Add(this.btnSyncTime);
            this.panelRight.Controls.Add(this.btnChatLog);
            
            this.chkMuteGroup.Text = "启停禁言群";
            this.chkMuteGroup.Location = new System.Drawing.Point(5, 5);
            this.chkMuteGroup.AutoSize = true;
            this.chkMuteGroup.Checked = true;
            
            this.chkStopAfterPeriod.Text = "开完本期停";
            this.chkStopAfterPeriod.Location = new System.Drawing.Point(5, 28);
            this.chkStopAfterPeriod.AutoSize = true;
            this.chkStopAfterPeriod.Checked = true;
            
            this.chkSupportNickChange.Text = "支持变昵称";
            this.chkSupportNickChange.Location = new System.Drawing.Point(5, 51);
            this.chkSupportNickChange.AutoSize = true;
            this.chkSupportNickChange.Checked = true;
            
            this.btnSyncTime.Text = "校准时间";
            this.btnSyncTime.Location = new System.Drawing.Point(5, 75);
            this.btnSyncTime.Size = new System.Drawing.Size(70, 23);
            
            this.btnChatLog.Text = "聊天日志";
            this.btnChatLog.Location = new System.Drawing.Point(80, 75);
            this.btnChatLog.Size = new System.Drawing.Size(70, 23);
            this.btnChatLog.Click += new System.EventHandler(this.btnChatLog_Click);
            
            // ===== Player Info Panel Configuration =====
            this.panelPlayerInfo.BackColor = System.Drawing.SystemColors.Control;
            this.panelPlayerInfo.Location = new System.Drawing.Point(5, 210);
            this.panelPlayerInfo.Size = new System.Drawing.Size(800, 55);
            this.panelPlayerInfo.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.panelPlayerInfo.Controls.Add(this.lblWangWangId);
            this.panelPlayerInfo.Controls.Add(this.txtWangWangId);
            this.panelPlayerInfo.Controls.Add(this.lblNickname);
            this.panelPlayerInfo.Controls.Add(this.txtNickname);
            this.panelPlayerInfo.Controls.Add(this.lblScore);
            this.panelPlayerInfo.Controls.Add(this.txtScore);
            this.panelPlayerInfo.Controls.Add(this.btnModifyInfo);
            this.panelPlayerInfo.Controls.Add(this.btnSearchPlayer);
            this.panelPlayerInfo.Controls.Add(this.chkShowTuoPlayer);
            this.panelPlayerInfo.Controls.Add(this.lblClientBox);
            this.panelPlayerInfo.Controls.Add(this.txtClientBox);
            this.panelPlayerInfo.Controls.Add(this.rdoAdd10);
            this.panelPlayerInfo.Controls.Add(this.rdoSub10);
            
            this.lblWangWangId.Text = "旺旺号";
            this.lblWangWangId.Location = new System.Drawing.Point(5, 8);
            this.lblWangWangId.AutoSize = true;
            
            this.txtWangWangId.Location = new System.Drawing.Point(50, 5);
            this.txtWangWangId.Size = new System.Drawing.Size(80, 21);
            
            this.lblNickname.Text = "昵称";
            this.lblNickname.Location = new System.Drawing.Point(135, 8);
            this.lblNickname.AutoSize = true;
            
            this.txtNickname.Location = new System.Drawing.Point(163, 5);
            this.txtNickname.Size = new System.Drawing.Size(50, 21);
            
            this.lblScore.Text = "分数";
            this.lblScore.Location = new System.Drawing.Point(218, 8);
            this.lblScore.AutoSize = true;
            
            this.txtScore.Location = new System.Drawing.Point(246, 5);
            this.txtScore.Size = new System.Drawing.Size(50, 21);
            
            this.btnModifyInfo.Text = "修改信息";
            this.btnModifyInfo.Location = new System.Drawing.Point(300, 3);
            this.btnModifyInfo.Size = new System.Drawing.Size(65, 23);
            this.btnModifyInfo.Click += new System.EventHandler(this.btnModifyInfo_Click);
            
            this.btnSearchPlayer.Text = "搜索玩家";
            this.btnSearchPlayer.Location = new System.Drawing.Point(368, 3);
            this.btnSearchPlayer.Size = new System.Drawing.Size(65, 23);
            this.btnSearchPlayer.Click += new System.EventHandler(this.btnSearchPlayer_Click);
            
            this.chkShowTuoPlayer.Text = "显示托玩家";
            this.chkShowTuoPlayer.Location = new System.Drawing.Point(440, 6);
            this.chkShowTuoPlayer.AutoSize = true;
            
            this.lblClientBox.Text = "客户框";
            this.lblClientBox.Location = new System.Drawing.Point(530, 8);
            this.lblClientBox.AutoSize = true;
            
            this.txtClientBox.Location = new System.Drawing.Point(5, 30);
            this.txtClientBox.Size = new System.Drawing.Size(520, 21);
            
            this.rdoAdd10.Text = "加10个";
            this.rdoAdd10.Location = new System.Drawing.Point(530, 30);
            this.rdoAdd10.AutoSize = true;
            this.rdoAdd10.Checked = true;
            
            this.rdoSub10.Text = "减10个";
            this.rdoSub10.Location = new System.Drawing.Point(600, 30);
            this.rdoSub10.AutoSize = true;
            
            // ===== Player List Configuration =====
            this.listPlayers.View = System.Windows.Forms.View.Details;
            this.listPlayers.FullRowSelect = true;
            this.listPlayers.GridLines = true;
            this.listPlayers.Location = new System.Drawing.Point(5, 270);
            this.listPlayers.Size = new System.Drawing.Size(800, 560);
            this.listPlayers.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.listPlayers.Columns.Add("玩家旺旺号", 85);
            this.listPlayers.Columns.Add("玩家昵称", 65);
            this.listPlayers.Columns.Add("分数", 50);
            this.listPlayers.Columns.Add("留分", 50);
            this.listPlayers.Columns.Add("下注内容", 350);
            this.listPlayers.Columns.Add("时间", 65);
            this.listPlayers.SelectedIndexChanged += new System.EventHandler(this.listPlayers_SelectedIndexChanged);
            
            // ===== Status Strip =====
            this.statusStrip.Items.Add(this.lblStatus);
            this.statusStrip.Location = new System.Drawing.Point(0, 543);
            this.statusStrip.Size = new System.Drawing.Size(680, 22);
            this.lblStatus.Text = "就绪";
            
            // ===== Settings TabControl (内嵌设置页面) =====
            this.tabSettings = new System.Windows.Forms.TabControl();
            this.tabBillSettings = new System.Windows.Forms.TabPage();
            this.tabBetProcess = new System.Windows.Forms.TabPage();
            this.tabOdds = new System.Windows.Forms.TabPage();
            this.tabAutoReply = new System.Windows.Forms.TabPage();
            this.tabBlacklist = new System.Windows.Forms.TabPage();
            this.tabCard = new System.Windows.Forms.TabPage();
            this.tabTrustee = new System.Windows.Forms.TabPage();
            this.tabBonus = new System.Windows.Forms.TabPage();
            this.tabTrusteeSettings = new System.Windows.Forms.TabPage();
            this.tabOther = new System.Windows.Forms.TabPage();
            
            this.tabSettings.Controls.Add(this.tabBillSettings);
            this.tabSettings.Controls.Add(this.tabBetProcess);
            this.tabSettings.Controls.Add(this.tabOdds);
            this.tabSettings.Controls.Add(this.tabAutoReply);
            this.tabSettings.Controls.Add(this.tabBlacklist);
            this.tabSettings.Controls.Add(this.tabCard);
            this.tabSettings.Controls.Add(this.tabTrustee);
            this.tabSettings.Controls.Add(this.tabBonus);
            this.tabSettings.Controls.Add(this.tabTrusteeSettings);
            this.tabSettings.Controls.Add(this.tabOther);
            this.tabSettings.Location = new System.Drawing.Point(0, 25);
            this.tabSettings.Size = new System.Drawing.Size(810, 810);
            this.tabSettings.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.tabSettings.Visible = false; // 默认隐藏
            
            this.tabBillSettings.Text = "账单设置";
            this.tabBillSettings.Padding = new System.Windows.Forms.Padding(3);
            this.tabBetProcess.Text = "下注处理";
            this.tabBetProcess.Padding = new System.Windows.Forms.Padding(3);
            this.tabOdds.Text = "玩法赔率设置";
            this.tabOdds.Padding = new System.Windows.Forms.Padding(3);
            this.tabAutoReply.Text = "自动回复";
            this.tabAutoReply.Padding = new System.Windows.Forms.Padding(3);
            this.tabBlacklist.Text = "黑名单/刷屏检测";
            this.tabBlacklist.Padding = new System.Windows.Forms.Padding(3);
            this.tabCard.Text = "名片";
            this.tabCard.Padding = new System.Windows.Forms.Padding(3);
            this.tabTrustee.Text = "托管设置";
            this.tabTrustee.Padding = new System.Windows.Forms.Padding(3);
            this.tabBonus.Text = "送分活动";
            this.tabBonus.Padding = new System.Windows.Forms.Padding(3);
            this.tabTrusteeSettings.Text = "托设置";
            this.tabTrusteeSettings.Padding = new System.Windows.Forms.Padding(3);
            this.tabOther.Text = "其他设置";
            this.tabOther.Padding = new System.Windows.Forms.Padding(3);
            
            // ===== Seal Settings TabControl (封盘设置页面) =====
            this.tabSealSettings = new System.Windows.Forms.TabControl();
            this.tabSealPC = new System.Windows.Forms.TabPage();
            this.tabSealCanada = new System.Windows.Forms.TabPage();
            this.tabSealBitcoin = new System.Windows.Forms.TabPage();
            this.tabSealBeijing = new System.Windows.Forms.TabPage();
            
            this.tabSealSettings.Controls.Add(this.tabSealPC);
            this.tabSealSettings.Controls.Add(this.tabSealCanada);
            this.tabSealSettings.Controls.Add(this.tabSealBitcoin);
            this.tabSealSettings.Controls.Add(this.tabSealBeijing);
            this.tabSealSettings.Location = new System.Drawing.Point(0, 25);
            this.tabSealSettings.Size = new System.Drawing.Size(810, 810);
            this.tabSealSettings.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.tabSealSettings.Visible = false; // 默认隐藏
            
            this.tabSealPC.Text = "PC";
            this.tabSealPC.Padding = new System.Windows.Forms.Padding(3);
            this.tabSealCanada.Text = "加拿大";
            this.tabSealCanada.Padding = new System.Windows.Forms.Padding(3);
            this.tabSealBitcoin.Text = "比特";
            this.tabSealBitcoin.Padding = new System.Windows.Forms.Padding(3);
            this.tabSealBeijing.Text = "北京";
            this.tabSealBeijing.Padding = new System.Windows.Forms.Padding(3);
            
            // ===== Rebate Tool Panel (回水工具页面) =====
            this.pnlRebateTool = new System.Windows.Forms.Panel();
            this.pnlRebateTool.Location = new System.Drawing.Point(0, 25);
            this.pnlRebateTool.Size = new System.Drawing.Size(810, 810);
            this.pnlRebateTool.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.pnlRebateTool.Visible = false; // 默认隐藏
            this._rebateToolCtrl = new WangShangLiaoBot.Controls.RebateTool.RebateToolControl();
            this._rebateToolCtrl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlRebateTool.Controls.Add(this._rebateToolCtrl);
            
            // ===== Form Configuration =====
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(810, 858);
            this.Controls.Add(this.pnlRebateTool);
            this.Controls.Add(this.tabSealSettings);
            this.Controls.Add(this.tabSettings);
            this.Controls.Add(this.listPlayers);
            this.Controls.Add(this.panelPlayerInfo);
            this.Controls.Add(this.panelRight);
            this.Controls.Add(this.panelMiddle);
            this.Controls.Add(this.panelLeft);
            this.Controls.Add(this.panelTopBar);
            this.Controls.Add(this.statusStrip);
            this.Controls.Add(this.menuStrip);
            this.MainMenuStrip = this.menuStrip;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            this.MaximizeBox = true;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "旺商聊机器人 v4.29";
            
            // 设置窗口图标
            string iconPath = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Resources", "app.ico");
            if (System.IO.File.Exists(iconPath))
            {
                this.Icon = new System.Drawing.Icon(iconPath);
            }
            
            this.menuStrip.ResumeLayout(false);
            this.menuStrip.PerformLayout();
            this.panelTopBar.ResumeLayout(false);
            this.panelTopBar.PerformLayout();
            this.panelLeft.ResumeLayout(false);
            this.panelLeft.PerformLayout();
            this.panelMiddle.ResumeLayout(false);
            this.panelMiddle.PerformLayout();
            this.panelRight.ResumeLayout(false);
            this.panelRight.PerformLayout();
            this.panelPlayerInfo.ResumeLayout(false);
            this.panelPlayerInfo.PerformLayout();
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }
        
        #endregion
        
        // Menu
        private System.Windows.Forms.MenuStrip menuStrip;
        private System.Windows.Forms.ToolStripMenuItem menuCustomer;
        private System.Windows.Forms.ToolStripMenuItem menuScoreSettings;
        private System.Windows.Forms.ToolStripMenuItem menuLockSettings;
        private System.Windows.Forms.ToolStripMenuItem menuRebateTools;
        private System.Windows.Forms.ToolStripMenuItem menuAbout;
        private System.Windows.Forms.ToolStripMenuItem menuTestConnection;
        private System.Windows.Forms.ToolStripMenuItem menuRunLog;
        private System.Windows.Forms.ToolStripMenuItem menuAccountList;
        private System.Windows.Forms.ToolStripMenuItem menuSystemSettings;
        private System.Windows.Forms.ToolStripMenuItem menuYxProxy;
        
        // Top toolbar
        private System.Windows.Forms.Panel panelTopBar;
        private System.Windows.Forms.Label lblConnectionStatus;
        private System.Windows.Forms.Button btnScoreWindow;
        private System.Windows.Forms.Button btnViewAccount;
        private System.Windows.Forms.Label lblF10Tip;
        
        // Left panel - Lottery info
        private System.Windows.Forms.Panel panelLeft;
        private System.Windows.Forms.Label lblCurrentPeriod;
        private System.Windows.Forms.Label lblPeriodNumber;
        private System.Windows.Forms.Label lblCountdownTitle;
        private System.Windows.Forms.Label lblCountdown;
        private System.Windows.Forms.Label lblResult1;
        private System.Windows.Forms.Label lblPlus1;
        private System.Windows.Forms.Label lblResult2;
        private System.Windows.Forms.Label lblPlus2;
        private System.Windows.Forms.Label lblResult3;
        private System.Windows.Forms.Label lblEquals;
        private System.Windows.Forms.Label lblResultSum;
        private System.Windows.Forms.Label lblNextPeriod;
        private System.Windows.Forms.Label lblNextPeriodNumber;
        private System.Windows.Forms.Label lblNoLotteryTip;
        private System.Windows.Forms.Button btnRefreshLottery;
        private System.Windows.Forms.Button btnManualCalc;
        
        // Middle panel - Buttons
        private System.Windows.Forms.Panel panelMiddle;
        private System.Windows.Forms.Button btnSendBill;
        private System.Windows.Forms.Button btnImportBill;
        private System.Windows.Forms.Button btnImportBet;
        private System.Windows.Forms.ComboBox cmbLotterySelect;
        private System.Windows.Forms.ComboBox cmbCountry;
        private System.Windows.Forms.Button btnCopyBill;
        private System.Windows.Forms.Button btnClearBet;
        private System.Windows.Forms.Button btnFixLottery;
        private System.Windows.Forms.ComboBox cmbChannel;
        private System.Windows.Forms.Label lblChannelBackup;
        private System.Windows.Forms.Button btnBetSummary;
        private System.Windows.Forms.Button btnClearZero;
        private System.Windows.Forms.Button btnExportBill;
        private System.Windows.Forms.Button btnStopCalc;
        private System.Windows.Forms.Button btnDetailProfit;
        private System.Windows.Forms.Button btnDeleteBill;
        private System.Windows.Forms.Button btnHistoryBill;
        private System.Windows.Forms.Button btnMuteAll;
        private System.Windows.Forms.Button btnUnmuteAll;
        
        // Right panel - Checkboxes
        private System.Windows.Forms.Panel panelRight;
        private System.Windows.Forms.CheckBox chkMuteGroup;
        private System.Windows.Forms.CheckBox chkStopAfterPeriod;
        private System.Windows.Forms.CheckBox chkSupportNickChange;
        private System.Windows.Forms.Button btnSyncTime;
        private System.Windows.Forms.Button btnChatLog;
        
        // Player info panel
        private System.Windows.Forms.Panel panelPlayerInfo;
        private System.Windows.Forms.Label lblWangWangId;
        private System.Windows.Forms.TextBox txtWangWangId;
        private System.Windows.Forms.Label lblNickname;
        private System.Windows.Forms.TextBox txtNickname;
        private System.Windows.Forms.Label lblScore;
        private System.Windows.Forms.TextBox txtScore;
        private System.Windows.Forms.Button btnModifyInfo;
        private System.Windows.Forms.Button btnSearchPlayer;
        private System.Windows.Forms.CheckBox chkShowTuoPlayer;
        private System.Windows.Forms.Label lblClientBox;
        private System.Windows.Forms.TextBox txtClientBox;
        private System.Windows.Forms.RadioButton rdoAdd10;
        private System.Windows.Forms.RadioButton rdoSub10;
        
        // Player list
        private System.Windows.Forms.ListView listPlayers;
        
        // Status strip
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel lblStatus;
        
        // Settings TabControl (内嵌设置页面)
        private System.Windows.Forms.TabControl tabSettings;
        private System.Windows.Forms.TabPage tabBillSettings;
        private System.Windows.Forms.TabPage tabBetProcess;
        private System.Windows.Forms.TabPage tabOdds;
        private System.Windows.Forms.TabPage tabAutoReply;
        private System.Windows.Forms.TabPage tabBlacklist;
        private System.Windows.Forms.TabPage tabCard;
        private System.Windows.Forms.TabPage tabTrustee;
        private System.Windows.Forms.TabPage tabBonus;
        private System.Windows.Forms.TabPage tabTrusteeSettings;
        private System.Windows.Forms.TabPage tabOther;
        
        // Seal Settings TabControl (封盘设置页面)
        private System.Windows.Forms.TabControl tabSealSettings;
        private System.Windows.Forms.TabPage tabSealPC;
        private System.Windows.Forms.TabPage tabSealCanada;
        private System.Windows.Forms.TabPage tabSealBitcoin;
        private System.Windows.Forms.TabPage tabSealBeijing;
        
        // Rebate Tool Panel (回水工具页面)
        private System.Windows.Forms.Panel pnlRebateTool;
        private WangShangLiaoBot.Controls.RebateTool.RebateToolControl _rebateToolCtrl;
    }
}
