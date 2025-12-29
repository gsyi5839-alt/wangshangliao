namespace WangShangLiaoBot.Forms
{
    partial class ScoreForm
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
            // 菜单栏
            this.menuStrip = new System.Windows.Forms.MenuStrip();
            this.menuUpDown = new System.Windows.Forms.ToolStripMenuItem();
            this.menuSettings = new System.Windows.Forms.ToolStripMenuItem();
            this.menuSettings2 = new System.Windows.Forms.ToolStripMenuItem();
            this.menuText = new System.Windows.Forms.ToolStripMenuItem();
            
            // 面板容器（用于切换不同页面）
            this.panelMain = new System.Windows.Forms.Panel();
            this.panelUpDown = new System.Windows.Forms.Panel();
            this.panelSettings = new System.Windows.Forms.Panel();
            this.panelSettings2 = new System.Windows.Forms.Panel();
            this.panelText = new System.Windows.Forms.Panel();
            
            // ===== 上分/下分页面控件 =====
            // 上分管理区
            this.grpUp = new System.Windows.Forms.GroupBox();
            this.lblUpStatus = new System.Windows.Forms.Label();
            this.lblUpSpeakContent = new System.Windows.Forms.Label();
            this.txtUpSpeakContent = new System.Windows.Forms.TextBox();
            this.lblRequestUpScore = new System.Windows.Forms.Label();
            this.txtRequestUpScore = new System.Windows.Forms.TextBox();
            this.btnModifyUpScore = new System.Windows.Forms.Button();
            this.btnUpArrived = new System.Windows.Forms.Button();
            this.btnUpNotArrived = new System.Windows.Forms.Button();
            this.btnUpIgnore = new System.Windows.Forms.Button();
            this.listUpRequests = new System.Windows.Forms.ListView();
            this.colUpPlayer = new System.Windows.Forms.ColumnHeader();
            this.colUpNickname = new System.Windows.Forms.ColumnHeader();
            this.colUpInfo = new System.Windows.Forms.ColumnHeader();
            this.colUpGrain = new System.Windows.Forms.ColumnHeader();
            this.colUpCount = new System.Windows.Forms.ColumnHeader();
            
            // 下分管理区
            this.grpDown = new System.Windows.Forms.GroupBox();
            this.lblDownStatus = new System.Windows.Forms.Label();
            this.lblDownSpeakContent = new System.Windows.Forms.Label();
            this.txtDownSpeakContent = new System.Windows.Forms.TextBox();
            this.lblRequestDownScore = new System.Windows.Forms.Label();
            this.txtRequestDownScore = new System.Windows.Forms.TextBox();
            this.btnModifyDownScore = new System.Windows.Forms.Button();
            this.lblDownGrain = new System.Windows.Forms.Label();
            this.txtDownGrain = new System.Windows.Forms.TextBox();
            this.btnDownCheck = new System.Windows.Forms.Button();
            this.btnDownReject = new System.Windows.Forms.Button();
            this.btnDownIgnore = new System.Windows.Forms.Button();
            this.listDownRequests = new System.Windows.Forms.ListView();
            this.colDownPlayer = new System.Windows.Forms.ColumnHeader();
            this.colDownNickname = new System.Windows.Forms.ColumnHeader();
            this.colDownInfo = new System.Windows.Forms.ColumnHeader();
            this.colDownGrain = new System.Windows.Forms.ColumnHeader();
            this.colDownCount = new System.Windows.Forms.ColumnHeader();
            
            // ===== 设置页面控件 =====
            this.chkAutoShowWindow = new System.Windows.Forms.CheckBox();
            this.chkAutoHideWindow = new System.Windows.Forms.CheckBox();
            this.chkFollowMainWindow = new System.Windows.Forms.CheckBox();
            this.chkUpScoreWindowTop = new System.Windows.Forms.CheckBox();
            this.chkClientDownRemind = new System.Windows.Forms.CheckBox();
            this.chkSameTimeDownScore = new System.Windows.Forms.CheckBox();
            this.chkForbidCancelDown = new System.Windows.Forms.CheckBox();
            this.chkUpMsgFeedback = new System.Windows.Forms.CheckBox();
            this.chkDownMsgFeedback = new System.Windows.Forms.CheckBox();
            this.chkClientDownReply = new System.Windows.Forms.CheckBox();
            this.lblMinRounds = new System.Windows.Forms.Label();
            this.txtMinRounds = new System.Windows.Forms.TextBox();
            this.lblMinRoundsDesc = new System.Windows.Forms.Label();
            this.txtMinScore = new System.Windows.Forms.TextBox();
            this.lblMinScoreDesc = new System.Windows.Forms.Label();
            this.chkUpDownMsgFilter = new System.Windows.Forms.CheckBox();
            this.lblAutoUpScore = new System.Windows.Forms.Label();
            this.chkAutoUpOverTime = new System.Windows.Forms.CheckBox();
            this.txtAutoUpTime = new System.Windows.Forms.TextBox();
            this.lblAutoUpTimeDesc = new System.Windows.Forms.Label();
            this.txtClientDownReplyContent = new System.Windows.Forms.TextBox();
            this.lblUpKeyword = new System.Windows.Forms.Label();
            this.txtUpKeyword = new System.Windows.Forms.TextBox();
            this.lblDownKeyword = new System.Windows.Forms.Label();
            this.txtDownKeyword = new System.Windows.Forms.TextBox();
            this.btnSaveSettings = new System.Windows.Forms.Button();
            
            // ===== 设置2页面控件（提示音） =====
            this.lblSoundTitle = new System.Windows.Forms.Label();
            this.chkUpScoreSound = new System.Windows.Forms.CheckBox();
            this.rbUpSoundDefault = new System.Windows.Forms.RadioButton();
            this.rbUpSoundDingDong = new System.Windows.Forms.RadioButton();
            this.chkUpCustomSound = new System.Windows.Forms.CheckBox();
            this.btnSetUpCustomSound = new System.Windows.Forms.Button();
            this.btnTestUpSound = new System.Windows.Forms.Button();
            
            this.chkDownScoreSound = new System.Windows.Forms.CheckBox();
            this.rbDownSoundDefault = new System.Windows.Forms.RadioButton();
            this.rbDownSoundDingDong = new System.Windows.Forms.RadioButton();
            this.chkDownCustomSound = new System.Windows.Forms.CheckBox();
            this.btnSetDownCustomSound = new System.Windows.Forms.Button();
            this.btnTestDownSound = new System.Windows.Forms.Button();
            
            this.chkLotterySound = new System.Windows.Forms.CheckBox();
            this.rbLotterySoundDefault = new System.Windows.Forms.RadioButton();
            this.rbLotterySoundDingDong = new System.Windows.Forms.RadioButton();
            this.chkLotteryCustomSound = new System.Windows.Forms.CheckBox();
            this.btnSetLotteryCustomSound = new System.Windows.Forms.Button();
            this.btnTestLotterySound = new System.Windows.Forms.Button();
            
            this.chkSealSound = new System.Windows.Forms.CheckBox();
            this.rbSealSoundDefault = new System.Windows.Forms.RadioButton();
            this.rbSealSoundDingDong = new System.Windows.Forms.RadioButton();
            this.chkSealCustomSound = new System.Windows.Forms.CheckBox();
            this.btnSetSealCustomSound = new System.Windows.Forms.Button();
            this.btnTestSealSound = new System.Windows.Forms.Button();
            
            this.txtWavNote = new System.Windows.Forms.TextBox();
            
            // ===== 提示文本页面控件 =====
            this.lblNotArrivedText = new System.Windows.Forms.Label();
            this.txtNotArrivedText = new System.Windows.Forms.TextBox();
            this.lblZeroArrivedText = new System.Windows.Forms.Label();
            this.txtZeroArrivedText = new System.Windows.Forms.TextBox();
            this.lblHasScoreText = new System.Windows.Forms.Label();
            this.txtHasScoreText = new System.Windows.Forms.TextBox();
            this.lblArrivedScoreText = new System.Windows.Forms.Label();
            this.txtArrivedScoreText = new System.Windows.Forms.TextBox();
            this.lblNoScoreText = new System.Windows.Forms.Label();
            this.txtNoScoreText = new System.Windows.Forms.TextBox();
            this.lblCheckScoreText = new System.Windows.Forms.Label();
            this.txtCheckScoreText = new System.Windows.Forms.TextBox();
            this.lblReSaveText = new System.Windows.Forms.Label();
            this.txtReSaveText = new System.Windows.Forms.TextBox();
            this.lblDontRushText = new System.Windows.Forms.Label();
            this.txtDontRushText = new System.Windows.Forms.TextBox();
            this.lblRejectText = new System.Windows.Forms.Label();
            this.txtRejectText = new System.Windows.Forms.TextBox();
            this.lblDownScoreText = new System.Windows.Forms.Label();
            this.txtDownScoreText = new System.Windows.Forms.TextBox();
            this.txtDownScoreText2 = new System.Windows.Forms.TextBox();
            this.btnSaveText = new System.Windows.Forms.Button();
            
            this.menuStrip.SuspendLayout();
            this.panelMain.SuspendLayout();
            this.panelUpDown.SuspendLayout();
            this.panelSettings.SuspendLayout();
            this.panelSettings2.SuspendLayout();
            this.panelText.SuspendLayout();
            this.grpUp.SuspendLayout();
            this.grpDown.SuspendLayout();
            this.SuspendLayout();
            
            // ========================================
            // 菜单栏
            // ========================================
            this.menuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.menuUpDown,
                this.menuSettings,
                this.menuSettings2,
                this.menuText
            });
            this.menuStrip.Location = new System.Drawing.Point(0, 0);
            this.menuStrip.Size = new System.Drawing.Size(294, 25);
            
            this.menuUpDown.Name = "menuUpDown";
            this.menuUpDown.Text = "上分/下分";
            this.menuUpDown.Click += new System.EventHandler(this.menuUpDown_Click);
            
            this.menuSettings.Name = "menuSettings";
            this.menuSettings.Text = "设置";
            this.menuSettings.Click += new System.EventHandler(this.menuSettings_Click);
            
            this.menuSettings2.Name = "menuSettings2";
            this.menuSettings2.Text = "设置2";
            this.menuSettings2.Click += new System.EventHandler(this.menuSettings2_Click);
            
            this.menuText.Name = "menuText";
            this.menuText.Text = "提示文本";
            this.menuText.Click += new System.EventHandler(this.menuText_Click);
            
            // ========================================
            // 主面板容器
            // ========================================
            this.panelMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelMain.Location = new System.Drawing.Point(0, 25);
            this.panelMain.Controls.Add(this.panelUpDown);
            this.panelMain.Controls.Add(this.panelSettings);
            this.panelMain.Controls.Add(this.panelSettings2);
            this.panelMain.Controls.Add(this.panelText);
            
            // ========================================
            // 上分/下分页面
            // ========================================
            this.panelUpDown.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelUpDown.Controls.Add(this.grpUp);
            this.panelUpDown.Controls.Add(this.grpDown);
            
            // 上分管理组
            this.grpUp.Text = "上分管理";
            this.grpUp.Location = new System.Drawing.Point(5, 5);
            this.grpUp.Size = new System.Drawing.Size(282, 190);
            this.grpUp.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.grpUp.Controls.Add(this.lblUpStatus);
            this.grpUp.Controls.Add(this.lblUpSpeakContent);
            this.grpUp.Controls.Add(this.txtUpSpeakContent);
            this.grpUp.Controls.Add(this.lblRequestUpScore);
            this.grpUp.Controls.Add(this.txtRequestUpScore);
            this.grpUp.Controls.Add(this.btnModifyUpScore);
            this.grpUp.Controls.Add(this.btnUpArrived);
            this.grpUp.Controls.Add(this.btnUpNotArrived);
            this.grpUp.Controls.Add(this.btnUpIgnore);
            this.grpUp.Controls.Add(this.listUpRequests);
            
            this.lblUpStatus.Text = "暂无玩家上分";
            this.lblUpStatus.ForeColor = System.Drawing.Color.Gray;
            this.lblUpStatus.Location = new System.Drawing.Point(8, 18);
            this.lblUpStatus.AutoSize = true;
            
            this.lblUpSpeakContent.Text = "喊话内容:";
            this.lblUpSpeakContent.Location = new System.Drawing.Point(8, 40);
            this.lblUpSpeakContent.AutoSize = true;
            
            this.txtUpSpeakContent.Location = new System.Drawing.Point(70, 37);
            this.txtUpSpeakContent.Size = new System.Drawing.Size(200, 21);
            this.txtUpSpeakContent.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            
            this.lblRequestUpScore.Text = "请求上分:";
            this.lblRequestUpScore.Location = new System.Drawing.Point(8, 65);
            this.lblRequestUpScore.AutoSize = true;
            
            this.txtRequestUpScore.Location = new System.Drawing.Point(70, 62);
            this.txtRequestUpScore.Size = new System.Drawing.Size(70, 21);
            
            this.btnModifyUpScore.Text = "修改上分";
            this.btnModifyUpScore.Location = new System.Drawing.Point(150, 60);
            this.btnModifyUpScore.Size = new System.Drawing.Size(70, 25);
            this.btnModifyUpScore.Click += new System.EventHandler(this.btnModifyUpScore_Click);
            
            this.btnUpArrived.Text = "@喊到";
            this.btnUpArrived.Location = new System.Drawing.Point(8, 90);
            this.btnUpArrived.Size = new System.Drawing.Size(65, 25);
            this.btnUpArrived.Click += new System.EventHandler(this.btnUpArrived_Click);
            
            this.btnUpNotArrived.Text = "@喊没到";
            this.btnUpNotArrived.Location = new System.Drawing.Point(78, 90);
            this.btnUpNotArrived.Size = new System.Drawing.Size(70, 25);
            this.btnUpNotArrived.Click += new System.EventHandler(this.btnUpNotArrived_Click);
            
            this.btnUpIgnore.Text = "忽略";
            this.btnUpIgnore.Location = new System.Drawing.Point(153, 90);
            this.btnUpIgnore.Size = new System.Drawing.Size(55, 25);
            this.btnUpIgnore.Click += new System.EventHandler(this.btnUpIgnore_Click);
            
            this.listUpRequests.View = System.Windows.Forms.View.Details;
            this.listUpRequests.FullRowSelect = true;
            this.listUpRequests.GridLines = true;
            this.listUpRequests.Location = new System.Drawing.Point(8, 120);
            this.listUpRequests.Size = new System.Drawing.Size(266, 62);
            this.listUpRequests.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right | System.Windows.Forms.AnchorStyles.Bottom;
            this.listUpRequests.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
                this.colUpPlayer,
                this.colUpNickname,
                this.colUpInfo,
                this.colUpGrain,
                this.colUpCount
            });
            
            this.colUpPlayer.Text = "玩家";
            this.colUpPlayer.Width = 50;
            this.colUpNickname.Text = "昵称";
            this.colUpNickname.Width = 55;
            this.colUpInfo.Text = "信息...";
            this.colUpInfo.Width = 55;
            this.colUpGrain.Text = "余粮";
            this.colUpGrain.Width = 45;
            this.colUpCount.Text = "次数";
            this.colUpCount.Width = 45;
            
            // 下分管理组
            this.grpDown.Text = "下分管理";
            this.grpDown.Location = new System.Drawing.Point(5, 200);
            this.grpDown.Size = new System.Drawing.Size(282, 220);
            this.grpDown.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right | System.Windows.Forms.AnchorStyles.Bottom;
            this.grpDown.Controls.Add(this.lblDownStatus);
            this.grpDown.Controls.Add(this.lblDownSpeakContent);
            this.grpDown.Controls.Add(this.txtDownSpeakContent);
            this.grpDown.Controls.Add(this.lblRequestDownScore);
            this.grpDown.Controls.Add(this.txtRequestDownScore);
            this.grpDown.Controls.Add(this.btnModifyDownScore);
            this.grpDown.Controls.Add(this.lblDownGrain);
            this.grpDown.Controls.Add(this.txtDownGrain);
            this.grpDown.Controls.Add(this.btnDownCheck);
            this.grpDown.Controls.Add(this.btnDownReject);
            this.grpDown.Controls.Add(this.btnDownIgnore);
            this.grpDown.Controls.Add(this.listDownRequests);
            
            this.lblDownStatus.Text = "暂无玩家下分";
            this.lblDownStatus.ForeColor = System.Drawing.Color.Gray;
            this.lblDownStatus.Location = new System.Drawing.Point(8, 18);
            this.lblDownStatus.AutoSize = true;
            
            this.lblDownSpeakContent.Text = "喊话内容:";
            this.lblDownSpeakContent.Location = new System.Drawing.Point(8, 40);
            this.lblDownSpeakContent.AutoSize = true;
            
            this.txtDownSpeakContent.Location = new System.Drawing.Point(70, 37);
            this.txtDownSpeakContent.Size = new System.Drawing.Size(200, 21);
            this.txtDownSpeakContent.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            
            this.lblRequestDownScore.Text = "请求下分:";
            this.lblRequestDownScore.Location = new System.Drawing.Point(8, 65);
            this.lblRequestDownScore.AutoSize = true;
            
            this.txtRequestDownScore.Location = new System.Drawing.Point(70, 62);
            this.txtRequestDownScore.Size = new System.Drawing.Size(70, 21);
            
            this.btnModifyDownScore.Text = "修改下分";
            this.btnModifyDownScore.Location = new System.Drawing.Point(150, 60);
            this.btnModifyDownScore.Size = new System.Drawing.Size(70, 25);
            this.btnModifyDownScore.Click += new System.EventHandler(this.btnModifyDownScore_Click);
            
            this.lblDownGrain.Text = "余粮:";
            this.lblDownGrain.Location = new System.Drawing.Point(8, 90);
            this.lblDownGrain.AutoSize = true;
            
            this.txtDownGrain.Location = new System.Drawing.Point(45, 87);
            this.txtDownGrain.Size = new System.Drawing.Size(80, 21);
            
            this.btnDownCheck.Text = "@喊查";
            this.btnDownCheck.Location = new System.Drawing.Point(8, 115);
            this.btnDownCheck.Size = new System.Drawing.Size(60, 25);
            this.btnDownCheck.Click += new System.EventHandler(this.btnDownCheck_Click);
            
            this.btnDownReject.Text = "@拒绝";
            this.btnDownReject.Location = new System.Drawing.Point(73, 115);
            this.btnDownReject.Size = new System.Drawing.Size(60, 25);
            this.btnDownReject.Click += new System.EventHandler(this.btnDownReject_Click);
            
            this.btnDownIgnore.Text = "忽略";
            this.btnDownIgnore.Location = new System.Drawing.Point(138, 115);
            this.btnDownIgnore.Size = new System.Drawing.Size(55, 25);
            this.btnDownIgnore.Click += new System.EventHandler(this.btnDownIgnore_Click);
            
            this.listDownRequests.View = System.Windows.Forms.View.Details;
            this.listDownRequests.FullRowSelect = true;
            this.listDownRequests.GridLines = true;
            this.listDownRequests.Location = new System.Drawing.Point(8, 145);
            this.listDownRequests.Size = new System.Drawing.Size(266, 67);
            this.listDownRequests.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right | System.Windows.Forms.AnchorStyles.Bottom;
            this.listDownRequests.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
                this.colDownPlayer,
                this.colDownNickname,
                this.colDownInfo,
                this.colDownGrain,
                this.colDownCount
            });
            
            this.colDownPlayer.Text = "玩家";
            this.colDownPlayer.Width = 50;
            this.colDownNickname.Text = "昵称";
            this.colDownNickname.Width = 55;
            this.colDownInfo.Text = "信息...";
            this.colDownInfo.Width = 55;
            this.colDownGrain.Text = "余粮";
            this.colDownGrain.Width = 45;
            this.colDownCount.Text = "次数";
            this.colDownCount.Width = 45;
            
            // ========================================
            // 设置页面
            // ========================================
            this.panelSettings.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelSettings.Visible = false;
            this.panelSettings.AutoScroll = true;
            this.panelSettings.Controls.Add(this.chkAutoShowWindow);
            this.panelSettings.Controls.Add(this.chkAutoHideWindow);
            this.panelSettings.Controls.Add(this.chkFollowMainWindow);
            this.panelSettings.Controls.Add(this.chkUpScoreWindowTop);
            this.panelSettings.Controls.Add(this.chkClientDownRemind);
            this.panelSettings.Controls.Add(this.chkSameTimeDownScore);
            this.panelSettings.Controls.Add(this.chkForbidCancelDown);
            this.panelSettings.Controls.Add(this.chkUpMsgFeedback);
            this.panelSettings.Controls.Add(this.chkDownMsgFeedback);
            this.panelSettings.Controls.Add(this.chkClientDownReply);
            this.panelSettings.Controls.Add(this.txtClientDownReplyContent);
            this.panelSettings.Controls.Add(this.lblMinRounds);
            this.panelSettings.Controls.Add(this.txtMinRounds);
            this.panelSettings.Controls.Add(this.lblMinRoundsDesc);
            this.panelSettings.Controls.Add(this.txtMinScore);
            this.panelSettings.Controls.Add(this.lblMinScoreDesc);
            this.panelSettings.Controls.Add(this.chkUpDownMsgFilter);
            this.panelSettings.Controls.Add(this.lblAutoUpScore);
            this.panelSettings.Controls.Add(this.chkAutoUpOverTime);
            this.panelSettings.Controls.Add(this.txtAutoUpTime);
            this.panelSettings.Controls.Add(this.lblAutoUpTimeDesc);
            this.panelSettings.Controls.Add(this.lblUpKeyword);
            this.panelSettings.Controls.Add(this.txtUpKeyword);
            this.panelSettings.Controls.Add(this.lblDownKeyword);
            this.panelSettings.Controls.Add(this.txtDownKeyword);
            this.panelSettings.Controls.Add(this.btnSaveSettings);
            
            int settingsY = 10;
            int lineHeight = 25;
            
            this.chkAutoShowWindow.Text = "自动显示上下分窗口(F11)";
            this.chkAutoShowWindow.Location = new System.Drawing.Point(10, settingsY);
            this.chkAutoShowWindow.AutoSize = true;
            this.chkAutoShowWindow.Checked = true;
            
            settingsY += lineHeight;
            this.chkAutoHideWindow.Text = "自动隐藏上下分窗口(无上下分时)";
            this.chkAutoHideWindow.Location = new System.Drawing.Point(10, settingsY);
            this.chkAutoHideWindow.AutoSize = true;
            
            settingsY += lineHeight;
            this.chkFollowMainWindow.Text = "跟随主窗口移动";
            this.chkFollowMainWindow.Location = new System.Drawing.Point(10, settingsY);
            this.chkFollowMainWindow.AutoSize = true;
            this.chkFollowMainWindow.Checked = true;
            
            this.chkUpScoreWindowTop.Text = "上下分窗口置顶";
            this.chkUpScoreWindowTop.Location = new System.Drawing.Point(140, settingsY);
            this.chkUpScoreWindowTop.AutoSize = true;
            
            settingsY += lineHeight;
            this.chkClientDownRemind.Text = "客户下分勿催提醒";
            this.chkClientDownRemind.Location = new System.Drawing.Point(10, settingsY);
            this.chkClientDownRemind.AutoSize = true;
            this.chkClientDownRemind.Checked = true;
            
            this.chkSameTimeDownScore.Text = "同时下注下分";
            this.chkSameTimeDownScore.Location = new System.Drawing.Point(140, settingsY);
            this.chkSameTimeDownScore.AutoSize = true;
            
            settingsY += lineHeight;
            this.chkForbidCancelDown.Text = "禁止取消请求下分";
            this.chkForbidCancelDown.Location = new System.Drawing.Point(10, settingsY);
            this.chkForbidCancelDown.AutoSize = true;
            this.chkForbidCancelDown.Checked = true;
            
            settingsY += lineHeight;
            this.chkUpMsgFeedback.Text = "上分消息反馈至旺旺或群";
            this.chkUpMsgFeedback.Location = new System.Drawing.Point(10, settingsY);
            this.chkUpMsgFeedback.AutoSize = true;
            this.chkUpMsgFeedback.Checked = true;
            
            settingsY += lineHeight;
            this.chkDownMsgFeedback.Text = "下分消息反馈至旺旺或群";
            this.chkDownMsgFeedback.Location = new System.Drawing.Point(10, settingsY);
            this.chkDownMsgFeedback.AutoSize = true;
            this.chkDownMsgFeedback.Checked = true;
            
            settingsY += lineHeight;
            this.chkClientDownReply.Text = "客户下分回复";
            this.chkClientDownReply.Location = new System.Drawing.Point(10, settingsY);
            this.chkClientDownReply.AutoSize = true;
            this.chkClientDownReply.Checked = true;
            
            settingsY += lineHeight;
            this.txtClientDownReplyContent.Location = new System.Drawing.Point(10, settingsY);
            this.txtClientDownReplyContent.Size = new System.Drawing.Size(265, 45);
            this.txtClientDownReplyContent.Multiline = true;
            this.txtClientDownReplyContent.Text = "已收到[昵称][分数]请求，请稍等";
            this.txtClientDownReplyContent.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            
            settingsY += 50;
            this.lblMinRounds.Text = "上分后";
            this.lblMinRounds.Location = new System.Drawing.Point(10, settingsY + 3);
            this.lblMinRounds.AutoSize = true;
            
            this.txtMinRounds.Location = new System.Drawing.Point(55, settingsY);
            this.txtMinRounds.Size = new System.Drawing.Size(35, 21);
            this.txtMinRounds.Text = "6";
            
            this.lblMinRoundsDesc.Text = "把起才可再次下分";
            this.lblMinRoundsDesc.Location = new System.Drawing.Point(95, settingsY + 3);
            this.lblMinRoundsDesc.AutoSize = true;
            
            settingsY += lineHeight;
            this.txtMinScore.Location = new System.Drawing.Point(10, settingsY);
            this.txtMinScore.Size = new System.Drawing.Size(50, 21);
            this.txtMinScore.Text = "50";
            
            this.lblMinScoreDesc.Text = "分以下下分只能一次回";
            this.lblMinScoreDesc.Location = new System.Drawing.Point(65, settingsY + 3);
            this.lblMinScoreDesc.AutoSize = true;
            
            settingsY += lineHeight;
            this.chkUpDownMsgFilter.Text = "上下分消息重复过滤";
            this.chkUpDownMsgFilter.Location = new System.Drawing.Point(10, settingsY);
            this.chkUpDownMsgFilter.AutoSize = true;
            this.chkUpDownMsgFilter.Checked = true;
            
            settingsY += lineHeight;
            this.lblAutoUpScore.Text = "玩家自动上分";
            this.lblAutoUpScore.Location = new System.Drawing.Point(10, settingsY);
            this.lblAutoUpScore.AutoSize = true;
            
            settingsY += lineHeight;
            this.chkAutoUpOverTime.Text = "上分超过";
            this.chkAutoUpOverTime.Location = new System.Drawing.Point(10, settingsY);
            this.chkAutoUpOverTime.AutoSize = true;
            
            this.txtAutoUpTime.Location = new System.Drawing.Point(85, settingsY - 2);
            this.txtAutoUpTime.Size = new System.Drawing.Size(35, 21);
            this.txtAutoUpTime.Text = "0";
            
            this.lblAutoUpTimeDesc.Text = "秒未处理自动上分, 名单 隔开";
            this.lblAutoUpTimeDesc.Location = new System.Drawing.Point(125, settingsY + 1);
            this.lblAutoUpTimeDesc.AutoSize = true;
            
            settingsY += lineHeight + 10;
            this.lblUpKeyword.Text = "上分关键字";
            this.lblUpKeyword.Location = new System.Drawing.Point(10, settingsY + 3);
            this.lblUpKeyword.AutoSize = true;
            
            this.txtUpKeyword.Location = new System.Drawing.Point(75, settingsY);
            this.txtUpKeyword.Size = new System.Drawing.Size(50, 21);
            this.txtUpKeyword.Text = "查|c";
            
            this.lblDownKeyword.Text = "下分关键字";
            this.lblDownKeyword.Location = new System.Drawing.Point(135, settingsY + 3);
            this.lblDownKeyword.AutoSize = true;
            
            this.txtDownKeyword.Location = new System.Drawing.Point(200, settingsY);
            this.txtDownKeyword.Size = new System.Drawing.Size(50, 21);
            this.txtDownKeyword.Text = "回";
            
            settingsY += lineHeight + 15;
            this.btnSaveSettings.Text = "保存设置";
            this.btnSaveSettings.Location = new System.Drawing.Point(185, settingsY);
            this.btnSaveSettings.Size = new System.Drawing.Size(80, 28);
            this.btnSaveSettings.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            this.btnSaveSettings.Click += new System.EventHandler(this.btnSaveSettings_Click);
            
            // ========================================
            // 设置2页面（提示音）
            // ========================================
            this.panelSettings2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelSettings2.Visible = false;
            this.panelSettings2.AutoScroll = true;
            this.panelSettings2.Controls.Add(this.lblSoundTitle);
            this.panelSettings2.Controls.Add(this.chkUpScoreSound);
            this.panelSettings2.Controls.Add(this.rbUpSoundDefault);
            this.panelSettings2.Controls.Add(this.rbUpSoundDingDong);
            this.panelSettings2.Controls.Add(this.chkUpCustomSound);
            this.panelSettings2.Controls.Add(this.btnSetUpCustomSound);
            this.panelSettings2.Controls.Add(this.btnTestUpSound);
            this.panelSettings2.Controls.Add(this.chkDownScoreSound);
            this.panelSettings2.Controls.Add(this.rbDownSoundDefault);
            this.panelSettings2.Controls.Add(this.rbDownSoundDingDong);
            this.panelSettings2.Controls.Add(this.chkDownCustomSound);
            this.panelSettings2.Controls.Add(this.btnSetDownCustomSound);
            this.panelSettings2.Controls.Add(this.btnTestDownSound);
            this.panelSettings2.Controls.Add(this.chkLotterySound);
            this.panelSettings2.Controls.Add(this.rbLotterySoundDefault);
            this.panelSettings2.Controls.Add(this.rbLotterySoundDingDong);
            this.panelSettings2.Controls.Add(this.chkLotteryCustomSound);
            this.panelSettings2.Controls.Add(this.btnSetLotteryCustomSound);
            this.panelSettings2.Controls.Add(this.btnTestLotterySound);
            this.panelSettings2.Controls.Add(this.chkSealSound);
            this.panelSettings2.Controls.Add(this.rbSealSoundDefault);
            this.panelSettings2.Controls.Add(this.rbSealSoundDingDong);
            this.panelSettings2.Controls.Add(this.chkSealCustomSound);
            this.panelSettings2.Controls.Add(this.btnSetSealCustomSound);
            this.panelSettings2.Controls.Add(this.btnTestSealSound);
            this.panelSettings2.Controls.Add(this.txtWavNote);
            
            int sound2Y = 10;
            int soundLineHeight = 30;
            
            this.lblSoundTitle.Text = "提示音";
            this.lblSoundTitle.Font = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Bold);
            this.lblSoundTitle.Location = new System.Drawing.Point(10, sound2Y);
            this.lblSoundTitle.AutoSize = true;
            
            // 上分提示音
            sound2Y += 25;
            this.chkUpScoreSound.Text = "上分提示音";
            this.chkUpScoreSound.Location = new System.Drawing.Point(10, sound2Y);
            this.chkUpScoreSound.AutoSize = true;
            this.chkUpScoreSound.Checked = true;
            
            this.rbUpSoundDefault.Text = "默认";
            this.rbUpSoundDefault.Location = new System.Drawing.Point(100, sound2Y);
            this.rbUpSoundDefault.AutoSize = true;
            this.rbUpSoundDefault.Checked = true;
            
            this.rbUpSoundDingDong.Text = "叮咚";
            this.rbUpSoundDingDong.Location = new System.Drawing.Point(155, sound2Y);
            this.rbUpSoundDingDong.AutoSize = true;
            
            sound2Y += soundLineHeight;
            this.chkUpCustomSound.Text = "上分使用自定义提示音";
            this.chkUpCustomSound.Location = new System.Drawing.Point(10, sound2Y);
            this.chkUpCustomSound.AutoSize = true;
            
            sound2Y += soundLineHeight;
            this.btnSetUpCustomSound.Text = "设置自定义提示音";
            this.btnSetUpCustomSound.Location = new System.Drawing.Point(25, sound2Y);
            this.btnSetUpCustomSound.Size = new System.Drawing.Size(120, 25);
            
            this.btnTestUpSound.Text = "测试提示音";
            this.btnTestUpSound.Location = new System.Drawing.Point(155, sound2Y);
            this.btnTestUpSound.Size = new System.Drawing.Size(85, 25);
            
            // 下分提示音
            sound2Y += soundLineHeight + 10;
            this.chkDownScoreSound.Text = "下分提示音";
            this.chkDownScoreSound.Location = new System.Drawing.Point(10, sound2Y);
            this.chkDownScoreSound.AutoSize = true;
            this.chkDownScoreSound.Checked = true;
            
            this.rbDownSoundDefault.Text = "默认";
            this.rbDownSoundDefault.Location = new System.Drawing.Point(100, sound2Y);
            this.rbDownSoundDefault.AutoSize = true;
            this.rbDownSoundDefault.Checked = true;
            
            this.rbDownSoundDingDong.Text = "叮咚";
            this.rbDownSoundDingDong.Location = new System.Drawing.Point(155, sound2Y);
            this.rbDownSoundDingDong.AutoSize = true;
            
            sound2Y += soundLineHeight;
            this.chkDownCustomSound.Text = "下分使用自定义提示音";
            this.chkDownCustomSound.Location = new System.Drawing.Point(10, sound2Y);
            this.chkDownCustomSound.AutoSize = true;
            
            sound2Y += soundLineHeight;
            this.btnSetDownCustomSound.Text = "设置自定义提示音";
            this.btnSetDownCustomSound.Location = new System.Drawing.Point(25, sound2Y);
            this.btnSetDownCustomSound.Size = new System.Drawing.Size(120, 25);
            
            this.btnTestDownSound.Text = "测试提示音";
            this.btnTestDownSound.Location = new System.Drawing.Point(155, sound2Y);
            this.btnTestDownSound.Size = new System.Drawing.Size(85, 25);
            
            // 开奖提示音
            sound2Y += soundLineHeight + 10;
            this.chkLotterySound.Text = "开奖提示音";
            this.chkLotterySound.Location = new System.Drawing.Point(10, sound2Y);
            this.chkLotterySound.AutoSize = true;
            
            this.rbLotterySoundDefault.Text = "默认";
            this.rbLotterySoundDefault.Location = new System.Drawing.Point(100, sound2Y);
            this.rbLotterySoundDefault.AutoSize = true;
            this.rbLotterySoundDefault.Checked = true;
            
            this.rbLotterySoundDingDong.Text = "叮咚";
            this.rbLotterySoundDingDong.Location = new System.Drawing.Point(155, sound2Y);
            this.rbLotterySoundDingDong.AutoSize = true;
            
            sound2Y += soundLineHeight;
            this.chkLotteryCustomSound.Text = "开奖使用自定义提示音";
            this.chkLotteryCustomSound.Location = new System.Drawing.Point(10, sound2Y);
            this.chkLotteryCustomSound.AutoSize = true;
            
            sound2Y += soundLineHeight;
            this.btnSetLotteryCustomSound.Text = "设置自定义提示音";
            this.btnSetLotteryCustomSound.Location = new System.Drawing.Point(25, sound2Y);
            this.btnSetLotteryCustomSound.Size = new System.Drawing.Size(120, 25);
            
            this.btnTestLotterySound.Text = "测试提示音";
            this.btnTestLotterySound.Location = new System.Drawing.Point(155, sound2Y);
            this.btnTestLotterySound.Size = new System.Drawing.Size(85, 25);
            
            // 封盘提示音
            sound2Y += soundLineHeight + 10;
            this.chkSealSound.Text = "封盘提示音";
            this.chkSealSound.Location = new System.Drawing.Point(10, sound2Y);
            this.chkSealSound.AutoSize = true;
            
            this.rbSealSoundDefault.Text = "默认";
            this.rbSealSoundDefault.Location = new System.Drawing.Point(100, sound2Y);
            this.rbSealSoundDefault.AutoSize = true;
            this.rbSealSoundDefault.Checked = true;
            
            this.rbSealSoundDingDong.Text = "叮咚";
            this.rbSealSoundDingDong.Location = new System.Drawing.Point(155, sound2Y);
            this.rbSealSoundDingDong.AutoSize = true;
            
            sound2Y += soundLineHeight;
            this.chkSealCustomSound.Text = "封盘使用自定义提示音";
            this.chkSealCustomSound.Location = new System.Drawing.Point(10, sound2Y);
            this.chkSealCustomSound.AutoSize = true;
            
            sound2Y += soundLineHeight;
            this.btnSetSealCustomSound.Text = "设置自定义提示音";
            this.btnSetSealCustomSound.Location = new System.Drawing.Point(25, sound2Y);
            this.btnSetSealCustomSound.Size = new System.Drawing.Size(120, 25);
            
            this.btnTestSealSound.Text = "测试提示音";
            this.btnTestSealSound.Location = new System.Drawing.Point(155, sound2Y);
            this.btnTestSealSound.Size = new System.Drawing.Size(85, 25);
            
            sound2Y += soundLineHeight + 15;
            this.txtWavNote.Text = "服务器上只能播放wav，在线转wav: https://on";
            this.txtWavNote.Location = new System.Drawing.Point(10, sound2Y);
            this.txtWavNote.Size = new System.Drawing.Size(265, 21);
            this.txtWavNote.ReadOnly = true;
            this.txtWavNote.ForeColor = System.Drawing.Color.Gray;
            this.txtWavNote.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            
            // ========================================
            // 提示文本页面
            // ========================================
            this.panelText.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelText.Visible = false;
            this.panelText.AutoScroll = true;
            this.panelText.Controls.Add(this.lblNotArrivedText);
            this.panelText.Controls.Add(this.txtNotArrivedText);
            this.panelText.Controls.Add(this.lblZeroArrivedText);
            this.panelText.Controls.Add(this.txtZeroArrivedText);
            this.panelText.Controls.Add(this.lblHasScoreText);
            this.panelText.Controls.Add(this.txtHasScoreText);
            this.panelText.Controls.Add(this.lblArrivedScoreText);
            this.panelText.Controls.Add(this.txtArrivedScoreText);
            this.panelText.Controls.Add(this.lblNoScoreText);
            this.panelText.Controls.Add(this.txtNoScoreText);
            this.panelText.Controls.Add(this.lblCheckScoreText);
            this.panelText.Controls.Add(this.txtCheckScoreText);
            this.panelText.Controls.Add(this.lblReSaveText);
            this.panelText.Controls.Add(this.txtReSaveText);
            this.panelText.Controls.Add(this.lblDontRushText);
            this.panelText.Controls.Add(this.txtDontRushText);
            this.panelText.Controls.Add(this.lblRejectText);
            this.panelText.Controls.Add(this.txtRejectText);
            this.panelText.Controls.Add(this.lblDownScoreText);
            this.panelText.Controls.Add(this.txtDownScoreText);
            this.panelText.Controls.Add(this.txtDownScoreText2);
            this.panelText.Controls.Add(this.btnSaveText);
            
            int textY = 10;
            int textLineHeight = 30;
            
            this.lblNotArrivedText.Text = "没到词:";
            this.lblNotArrivedText.Location = new System.Drawing.Point(10, textY + 3);
            this.lblNotArrivedText.AutoSize = true;
            
            this.txtNotArrivedText.Location = new System.Drawing.Point(65, textY);
            this.txtNotArrivedText.Size = new System.Drawing.Size(210, 21);
            this.txtNotArrivedText.Text = "@qq 没到";
            
            textY += textLineHeight;
            this.lblZeroArrivedText.Text = "0分\r\n到词:";
            this.lblZeroArrivedText.Location = new System.Drawing.Point(10, textY);
            this.lblZeroArrivedText.Size = new System.Drawing.Size(50, 28);
            
            this.txtZeroArrivedText.Location = new System.Drawing.Point(65, textY);
            this.txtZeroArrivedText.Size = new System.Drawing.Size(210, 21);
            this.txtZeroArrivedText.Text = "@qq [分数]到";
            
            textY += textLineHeight;
            this.lblHasScoreText.Text = "有分\r\n到词:";
            this.lblHasScoreText.Location = new System.Drawing.Point(10, textY);
            this.lblHasScoreText.Size = new System.Drawing.Size(50, 28);
            
            this.txtHasScoreText.Location = new System.Drawing.Point(65, textY);
            this.txtHasScoreText.Size = new System.Drawing.Size(210, 21);
            this.txtHasScoreText.Text = "@qq [分数]到";
            
            textY += textLineHeight;
            this.lblArrivedScoreText.Text = "到分\r\n无余:";
            this.lblArrivedScoreText.Location = new System.Drawing.Point(10, textY);
            this.lblArrivedScoreText.Size = new System.Drawing.Size(50, 28);
            
            this.txtArrivedScoreText.Location = new System.Drawing.Point(65, textY);
            this.txtArrivedScoreText.Size = new System.Drawing.Size(210, 21);
            
            textY += textLineHeight;
            this.lblNoScoreText.Text = "查分词:";
            this.lblNoScoreText.Location = new System.Drawing.Point(10, textY + 3);
            this.lblNoScoreText.AutoSize = true;
            
            this.txtNoScoreText.Location = new System.Drawing.Point(65, textY);
            this.txtNoScoreText.Size = new System.Drawing.Size(210, 21);
            this.txtNoScoreText.Text = "@qq [分数]查收注意：通过其它方式打";
            
            textY += textLineHeight;
            this.lblCheckScoreText.Text = "重存词:";
            this.lblCheckScoreText.Location = new System.Drawing.Point(10, textY + 3);
            this.lblCheckScoreText.AutoSize = true;
            
            this.txtCheckScoreText.Location = new System.Drawing.Point(65, textY);
            this.txtCheckScoreText.Size = new System.Drawing.Size(210, 21);
            
            textY += textLineHeight;
            this.lblReSaveText.Text = "勿催词:";
            this.lblReSaveText.Location = new System.Drawing.Point(10, textY + 3);
            this.lblReSaveText.AutoSize = true;
            
            this.txtReSaveText.Location = new System.Drawing.Point(65, textY);
            this.txtReSaveText.Size = new System.Drawing.Size(210, 21);
            this.txtReSaveText.Text = "@qq 勿催";
            
            textY += textLineHeight;
            this.lblDontRushText.Text = "拒绝词:";
            this.lblDontRushText.Location = new System.Drawing.Point(10, textY + 3);
            this.lblDontRushText.AutoSize = true;
            
            this.txtDontRushText.Location = new System.Drawing.Point(65, textY);
            this.txtDontRushText.Size = new System.Drawing.Size(210, 21);
            this.txtDontRushText.Text = "@qq 6把首冲后10把50起回谢谢";
            
            textY += textLineHeight;
            this.lblRejectText.Text = "下分\r\n分词:";
            this.lblRejectText.Location = new System.Drawing.Point(10, textY);
            this.lblRejectText.Size = new System.Drawing.Size(50, 28);
            
            this.txtRejectText.Location = new System.Drawing.Point(65, textY);
            this.txtRejectText.Size = new System.Drawing.Size(210, 21);
            this.txtRejectText.Text = "@qq 账单分不足，下芬失败";
            
            textY += textLineHeight;
            this.lblDownScoreText.Text = "";
            this.lblDownScoreText.Location = new System.Drawing.Point(10, textY + 3);
            this.lblDownScoreText.AutoSize = true;
            
            this.txtDownScoreText.Location = new System.Drawing.Point(65, textY);
            this.txtDownScoreText.Size = new System.Drawing.Size(210, 21);
            
            textY += textLineHeight + 15;
            this.btnSaveText.Text = "保存设置";
            this.btnSaveText.Location = new System.Drawing.Point(185, textY);
            this.btnSaveText.Size = new System.Drawing.Size(80, 28);
            this.btnSaveText.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            this.btnSaveText.Click += new System.EventHandler(this.btnSaveText_Click);
            
            // ========================================
            // Form
            // ========================================
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(294, 430);
            this.MinimumSize = new System.Drawing.Size(300, 400);
            this.Controls.Add(this.panelMain);
            this.Controls.Add(this.menuStrip);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            this.MainMenuStrip = this.menuStrip;
            this.Name = "ScoreForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "上下分处理 - F11可隐藏";
            
            // 设置窗口图标
            string iconPath = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Resources", "app.ico");
            if (System.IO.File.Exists(iconPath))
            {
                this.Icon = new System.Drawing.Icon(iconPath);
            }
            
            this.menuStrip.ResumeLayout(false);
            this.menuStrip.PerformLayout();
            this.panelMain.ResumeLayout(false);
            this.panelUpDown.ResumeLayout(false);
            this.panelSettings.ResumeLayout(false);
            this.panelSettings.PerformLayout();
            this.panelSettings2.ResumeLayout(false);
            this.panelSettings2.PerformLayout();
            this.panelText.ResumeLayout(false);
            this.panelText.PerformLayout();
            this.grpUp.ResumeLayout(false);
            this.grpUp.PerformLayout();
            this.grpDown.ResumeLayout(false);
            this.grpDown.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }
        
        #endregion
        
        // 菜单栏
        private System.Windows.Forms.MenuStrip menuStrip;
        private System.Windows.Forms.ToolStripMenuItem menuUpDown;
        private System.Windows.Forms.ToolStripMenuItem menuSettings;
        private System.Windows.Forms.ToolStripMenuItem menuSettings2;
        private System.Windows.Forms.ToolStripMenuItem menuText;
        
        // 面板容器
        private System.Windows.Forms.Panel panelMain;
        private System.Windows.Forms.Panel panelUpDown;
        private System.Windows.Forms.Panel panelSettings;
        private System.Windows.Forms.Panel panelSettings2;
        private System.Windows.Forms.Panel panelText;
        
        // 上分管理区
        private System.Windows.Forms.GroupBox grpUp;
        private System.Windows.Forms.Label lblUpStatus;
        private System.Windows.Forms.Label lblUpSpeakContent;
        private System.Windows.Forms.TextBox txtUpSpeakContent;
        private System.Windows.Forms.Label lblRequestUpScore;
        private System.Windows.Forms.TextBox txtRequestUpScore;
        private System.Windows.Forms.Button btnModifyUpScore;
        private System.Windows.Forms.Button btnUpArrived;
        private System.Windows.Forms.Button btnUpNotArrived;
        private System.Windows.Forms.Button btnUpIgnore;
        private System.Windows.Forms.ListView listUpRequests;
        private System.Windows.Forms.ColumnHeader colUpPlayer;
        private System.Windows.Forms.ColumnHeader colUpNickname;
        private System.Windows.Forms.ColumnHeader colUpInfo;
        private System.Windows.Forms.ColumnHeader colUpGrain;
        private System.Windows.Forms.ColumnHeader colUpCount;
        
        // 下分管理区
        private System.Windows.Forms.GroupBox grpDown;
        private System.Windows.Forms.Label lblDownStatus;
        private System.Windows.Forms.Label lblDownSpeakContent;
        private System.Windows.Forms.TextBox txtDownSpeakContent;
        private System.Windows.Forms.Label lblRequestDownScore;
        private System.Windows.Forms.TextBox txtRequestDownScore;
        private System.Windows.Forms.Button btnModifyDownScore;
        private System.Windows.Forms.Label lblDownGrain;
        private System.Windows.Forms.TextBox txtDownGrain;
        private System.Windows.Forms.Button btnDownCheck;
        private System.Windows.Forms.Button btnDownReject;
        private System.Windows.Forms.Button btnDownIgnore;
        private System.Windows.Forms.ListView listDownRequests;
        private System.Windows.Forms.ColumnHeader colDownPlayer;
        private System.Windows.Forms.ColumnHeader colDownNickname;
        private System.Windows.Forms.ColumnHeader colDownInfo;
        private System.Windows.Forms.ColumnHeader colDownGrain;
        private System.Windows.Forms.ColumnHeader colDownCount;
        
        // 设置页面
        private System.Windows.Forms.CheckBox chkAutoShowWindow;
        private System.Windows.Forms.CheckBox chkAutoHideWindow;
        private System.Windows.Forms.CheckBox chkFollowMainWindow;
        private System.Windows.Forms.CheckBox chkUpScoreWindowTop;
        private System.Windows.Forms.CheckBox chkClientDownRemind;
        private System.Windows.Forms.CheckBox chkSameTimeDownScore;
        private System.Windows.Forms.CheckBox chkForbidCancelDown;
        private System.Windows.Forms.CheckBox chkUpMsgFeedback;
        private System.Windows.Forms.CheckBox chkDownMsgFeedback;
        private System.Windows.Forms.CheckBox chkClientDownReply;
        private System.Windows.Forms.Label lblMinRounds;
        private System.Windows.Forms.TextBox txtMinRounds;
        private System.Windows.Forms.Label lblMinRoundsDesc;
        private System.Windows.Forms.TextBox txtMinScore;
        private System.Windows.Forms.Label lblMinScoreDesc;
        private System.Windows.Forms.CheckBox chkUpDownMsgFilter;
        private System.Windows.Forms.Label lblAutoUpScore;
        private System.Windows.Forms.CheckBox chkAutoUpOverTime;
        private System.Windows.Forms.TextBox txtAutoUpTime;
        private System.Windows.Forms.Label lblAutoUpTimeDesc;
        private System.Windows.Forms.TextBox txtClientDownReplyContent;
        private System.Windows.Forms.Label lblUpKeyword;
        private System.Windows.Forms.TextBox txtUpKeyword;
        private System.Windows.Forms.Label lblDownKeyword;
        private System.Windows.Forms.TextBox txtDownKeyword;
        private System.Windows.Forms.Button btnSaveSettings;
        
        // 设置2页面（提示音）
        private System.Windows.Forms.Label lblSoundTitle;
        private System.Windows.Forms.CheckBox chkUpScoreSound;
        private System.Windows.Forms.RadioButton rbUpSoundDefault;
        private System.Windows.Forms.RadioButton rbUpSoundDingDong;
        private System.Windows.Forms.CheckBox chkUpCustomSound;
        private System.Windows.Forms.Button btnSetUpCustomSound;
        private System.Windows.Forms.Button btnTestUpSound;
        
        private System.Windows.Forms.CheckBox chkDownScoreSound;
        private System.Windows.Forms.RadioButton rbDownSoundDefault;
        private System.Windows.Forms.RadioButton rbDownSoundDingDong;
        private System.Windows.Forms.CheckBox chkDownCustomSound;
        private System.Windows.Forms.Button btnSetDownCustomSound;
        private System.Windows.Forms.Button btnTestDownSound;
        
        private System.Windows.Forms.CheckBox chkLotterySound;
        private System.Windows.Forms.RadioButton rbLotterySoundDefault;
        private System.Windows.Forms.RadioButton rbLotterySoundDingDong;
        private System.Windows.Forms.CheckBox chkLotteryCustomSound;
        private System.Windows.Forms.Button btnSetLotteryCustomSound;
        private System.Windows.Forms.Button btnTestLotterySound;
        
        private System.Windows.Forms.CheckBox chkSealSound;
        private System.Windows.Forms.RadioButton rbSealSoundDefault;
        private System.Windows.Forms.RadioButton rbSealSoundDingDong;
        private System.Windows.Forms.CheckBox chkSealCustomSound;
        private System.Windows.Forms.Button btnSetSealCustomSound;
        private System.Windows.Forms.Button btnTestSealSound;
        
        private System.Windows.Forms.TextBox txtWavNote;
        
        // 提示文本页面
        private System.Windows.Forms.Label lblNotArrivedText;
        private System.Windows.Forms.TextBox txtNotArrivedText;
        private System.Windows.Forms.Label lblZeroArrivedText;
        private System.Windows.Forms.TextBox txtZeroArrivedText;
        private System.Windows.Forms.Label lblHasScoreText;
        private System.Windows.Forms.TextBox txtHasScoreText;
        private System.Windows.Forms.Label lblArrivedScoreText;
        private System.Windows.Forms.TextBox txtArrivedScoreText;
        private System.Windows.Forms.Label lblNoScoreText;
        private System.Windows.Forms.TextBox txtNoScoreText;
        private System.Windows.Forms.Label lblCheckScoreText;
        private System.Windows.Forms.TextBox txtCheckScoreText;
        private System.Windows.Forms.Label lblReSaveText;
        private System.Windows.Forms.TextBox txtReSaveText;
        private System.Windows.Forms.Label lblDontRushText;
        private System.Windows.Forms.TextBox txtDontRushText;
        private System.Windows.Forms.Label lblRejectText;
        private System.Windows.Forms.TextBox txtRejectText;
        private System.Windows.Forms.Label lblDownScoreText;
        private System.Windows.Forms.TextBox txtDownScoreText;
        private System.Windows.Forms.TextBox txtDownScoreText2;
        private System.Windows.Forms.Button btnSaveText;
    }
}
