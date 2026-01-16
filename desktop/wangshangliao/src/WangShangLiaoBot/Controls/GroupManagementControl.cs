using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using WangShangLiaoBot.Services;

namespace WangShangLiaoBot.Controls
{
    /// <summary>
    /// 群管理设置控件 - 整合发言检测、锁名片、进群欢迎
    /// </summary>
    public class GroupManagementControl : UserControl
    {
        private TabControl tabControl;

        // 发言检测Tab
        private CheckBox chkSpeechEnabled;
        private NumericUpDown nudMuteCharLimit;
        private NumericUpDown nudKickCharLimit;
        private NumericUpDown nudMuteLineLimit;
        private CheckBox chkImageMute;
        private NumericUpDown nudImageKickCount;
        private NumericUpDown nudMuteDuration;
        private CheckBox chkWithdrawViolation;
        private CheckBox chkZeroBalanceMute;
        private CheckBox chkAutoBlacklistOnKick;
        private CheckBox chkAutoBlacklistOnAdminKick;
        private TextBox txtForbiddenWords;
        private ListView lvBlacklist;

        // 锁名片Tab
        private CheckBox chkCardLockEnabled;
        private NumericUpDown nudMaxChangeCount;
        private CheckBox chkKickOnExceed;
        private CheckBox chkNotifyInGroup;
        private CheckBox chkAutoResetCard;
        private TextBox txtWarningTemplate;
        private TextBox txtKickTemplate;
        private ListView lvCardInfo;

        // 进群欢迎Tab
        private CheckBox chkPrivateWelcome;
        private CheckBox chkGroupWelcome;
        private TextBox txtPrivateWelcomeMsg;
        private TextBox txtGroupWelcomeMsg;
        private CheckBox chkAutoAcceptFriend;
        private CheckBox chkAutoAcceptBill;
        private CheckBox chkAutoAcceptTrustee;
        private NumericUpDown nudWelcomeDelay;

        private Button btnSave;
        private Label lblStatus;

        public GroupManagementControl()
        {
            InitializeComponent();
            LoadConfig();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.Size = new Size(850, 650);
            this.BackColor = Color.White;

            // 标题
            var lblTitle = new Label
            {
                Text = "👥 群管理设置",
                Font = new Font("Microsoft YaHei", 14, FontStyle.Bold),
                Location = new Point(20, 15),
                AutoSize = true
            };
            this.Controls.Add(lblTitle);

            // Tab控件
            tabControl = new TabControl
            {
                Location = new Point(20, 50),
                Size = new Size(810, 500)
            };
            this.Controls.Add(tabControl);

            // 发言检测Tab
            var tabSpeech = new TabPage("发言检测");
            InitSpeechTab(tabSpeech);
            tabControl.TabPages.Add(tabSpeech);

            // 锁名片Tab
            var tabCardLock = new TabPage("锁名片");
            InitCardLockTab(tabCardLock);
            tabControl.TabPages.Add(tabCardLock);

            // 进群欢迎Tab
            var tabWelcome = new TabPage("进群欢迎");
            InitWelcomeTab(tabWelcome);
            tabControl.TabPages.Add(tabWelcome);

            // 保存按钮
            btnSave = new Button
            {
                Text = "💾 保存配置",
                Location = new Point(20, 560),
                Size = new Size(120, 35),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnSave.Click += BtnSave_Click;
            this.Controls.Add(btnSave);

            lblStatus = new Label
            {
                Text = "",
                Location = new Point(150, 568),
                AutoSize = true,
                ForeColor = Color.Green
            };
            this.Controls.Add(lblStatus);

            this.ResumeLayout();
        }

        #region 发言检测Tab

        private void InitSpeechTab(TabPage tab)
        {
            chkSpeechEnabled = new CheckBox { Text = "启用发言检测", Location = new Point(15, 15), AutoSize = true };
            tab.Controls.Add(chkSpeechEnabled);

            // 字数限制
            var lblMuteChar = new Label { Text = "禁言字数限制:", Location = new Point(15, 50), AutoSize = true };
            tab.Controls.Add(lblMuteChar);
            nudMuteCharLimit = new NumericUpDown { Location = new Point(110, 48), Width = 80, Maximum = 1000 };
            tab.Controls.Add(nudMuteCharLimit);

            var lblKickChar = new Label { Text = "踢出字数限制:", Location = new Point(200, 50), AutoSize = true };
            tab.Controls.Add(lblKickChar);
            nudKickCharLimit = new NumericUpDown { Location = new Point(295, 48), Width = 80, Maximum = 1000 };
            tab.Controls.Add(nudKickCharLimit);

            var lblMuteLine = new Label { Text = "禁言行数限制:", Location = new Point(385, 50), AutoSize = true };
            tab.Controls.Add(lblMuteLine);
            nudMuteLineLimit = new NumericUpDown { Location = new Point(480, 48), Width = 60, Maximum = 100 };
            tab.Controls.Add(nudMuteLineLimit);

            // 图片检测
            chkImageMute = new CheckBox { Text = "图片禁言", Location = new Point(15, 85), AutoSize = true };
            tab.Controls.Add(chkImageMute);

            var lblImageKick = new Label { Text = "图片踢出次数:", Location = new Point(110, 85), AutoSize = true };
            tab.Controls.Add(lblImageKick);
            nudImageKickCount = new NumericUpDown { Location = new Point(205, 83), Width = 60, Maximum = 20 };
            tab.Controls.Add(nudImageKickCount);

            var lblMuteDur = new Label { Text = "禁言时长(分钟):", Location = new Point(280, 85), AutoSize = true };
            tab.Controls.Add(lblMuteDur);
            nudMuteDuration = new NumericUpDown { Location = new Point(385, 83), Width = 60, Maximum = 1440 };
            tab.Controls.Add(nudMuteDuration);

            // 其他选项
            chkWithdrawViolation = new CheckBox { Text = "违规撤回", Location = new Point(15, 120), AutoSize = true };
            tab.Controls.Add(chkWithdrawViolation);

            chkZeroBalanceMute = new CheckBox { Text = "0分玩家只能上分否则禁言", Location = new Point(110, 120), AutoSize = true };
            tab.Controls.Add(chkZeroBalanceMute);

            chkAutoBlacklistOnKick = new CheckBox { Text = "被机器人踢出加黑名单", Location = new Point(15, 150), AutoSize = true };
            tab.Controls.Add(chkAutoBlacklistOnKick);

            chkAutoBlacklistOnAdminKick = new CheckBox { Text = "被管理员踢出加黑名单", Location = new Point(200, 150), AutoSize = true };
            tab.Controls.Add(chkAutoBlacklistOnAdminKick);

            // 敏感词
            var lblForbidden = new Label { Text = "敏感词 (用|分隔):", Location = new Point(15, 185), AutoSize = true };
            tab.Controls.Add(lblForbidden);
            txtForbiddenWords = new TextBox { Location = new Point(120, 183), Width = 400 };
            tab.Controls.Add(txtForbiddenWords);

            // 黑名单
            var lblBlacklist = new Label { Text = "黑名单:", Location = new Point(15, 220), AutoSize = true };
            tab.Controls.Add(lblBlacklist);

            lvBlacklist = new ListView
            {
                Location = new Point(15, 245),
                Size = new Size(300, 180),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true
            };
            lvBlacklist.Columns.Add("用户ID", 280);
            tab.Controls.Add(lvBlacklist);

            var btnRemoveBlacklist = new Button
            {
                Text = "移除选中",
                Location = new Point(15, 430),
                Size = new Size(80, 25)
            };
            btnRemoveBlacklist.Click += (s, e) =>
            {
                if (lvBlacklist.SelectedItems.Count > 0)
                {
                    var playerId = lvBlacklist.SelectedItems[0].Text;
                    SpeechDetectionService.Instance.RemoveFromBlacklist(playerId);
                    RefreshBlacklist();
                }
            };
            tab.Controls.Add(btnRemoveBlacklist);
        }

        private void RefreshBlacklist()
        {
            lvBlacklist.Items.Clear();
            foreach (var id in SpeechDetectionService.Instance.GetBlacklist())
            {
                lvBlacklist.Items.Add(id);
            }
        }

        #endregion

        #region 锁名片Tab

        private void InitCardLockTab(TabPage tab)
        {
            chkCardLockEnabled = new CheckBox { Text = "启用锁名片", Location = new Point(15, 15), AutoSize = true };
            tab.Controls.Add(chkCardLockEnabled);

            var lblMaxChange = new Label { Text = "最大修改次数:", Location = new Point(15, 50), AutoSize = true };
            tab.Controls.Add(lblMaxChange);
            nudMaxChangeCount = new NumericUpDown { Location = new Point(110, 48), Width = 60, Minimum = 1, Maximum = 100 };
            tab.Controls.Add(nudMaxChangeCount);

            chkKickOnExceed = new CheckBox { Text = "超次数踢人", Location = new Point(180, 50), AutoSize = true };
            tab.Controls.Add(chkKickOnExceed);

            chkNotifyInGroup = new CheckBox { Text = "群内通知", Location = new Point(280, 50), AutoSize = true };
            tab.Controls.Add(chkNotifyInGroup);

            chkAutoResetCard = new CheckBox { Text = "自动重置名片", Location = new Point(370, 50), AutoSize = true };
            tab.Controls.Add(chkAutoResetCard);

            // 模板
            var lblWarning = new Label { Text = "警告模板:", Location = new Point(15, 90), AutoSize = true };
            tab.Controls.Add(lblWarning);
            txtWarningTemplate = new TextBox { Location = new Point(85, 88), Width = 400 };
            tab.Controls.Add(txtWarningTemplate);

            var lblKick = new Label { Text = "踢出模板:", Location = new Point(15, 120), AutoSize = true };
            tab.Controls.Add(lblKick);
            txtKickTemplate = new TextBox { Location = new Point(85, 118), Width = 400 };
            tab.Controls.Add(txtKickTemplate);

            var lblVars = new Label
            {
                Text = "变量: [旺旺]=昵称, [次数]=修改次数, [剩余]=剩余次数, [限制]=最大次数",
                Location = new Point(15, 145),
                AutoSize = true,
                ForeColor = Color.Gray
            };
            tab.Controls.Add(lblVars);

            // 名片列表
            var lblCards = new Label { Text = "已记录名片:", Location = new Point(15, 175), AutoSize = true };
            tab.Controls.Add(lblCards);

            lvCardInfo = new ListView
            {
                Location = new Point(15, 200),
                Size = new Size(500, 200),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true
            };
            lvCardInfo.Columns.Add("用户ID", 120);
            lvCardInfo.Columns.Add("原始名片", 120);
            lvCardInfo.Columns.Add("当前名片", 120);
            lvCardInfo.Columns.Add("修改次数", 70);
            tab.Controls.Add(lvCardInfo);

            var btnResetAll = new Button
            {
                Text = "重置所有计数",
                Location = new Point(15, 410),
                Size = new Size(100, 25)
            };
            btnResetAll.Click += (s, e) =>
            {
                CardLockService.Instance.ResetAllChangeCounts();
                RefreshCardInfo();
            };
            tab.Controls.Add(btnResetAll);
        }

        private void RefreshCardInfo()
        {
            lvCardInfo.Items.Clear();
            foreach (var info in CardLockService.Instance.GetAllCardInfo())
            {
                var item = new ListViewItem(info.PlayerId);
                item.SubItems.Add(info.OriginalCard);
                item.SubItems.Add(info.CurrentCard);
                item.SubItems.Add(info.ChangeCount.ToString());
                lvCardInfo.Items.Add(item);
            }
        }

        #endregion

        #region 进群欢迎Tab

        private void InitWelcomeTab(TabPage tab)
        {
            chkPrivateWelcome = new CheckBox { Text = "私聊欢迎", Location = new Point(15, 15), AutoSize = true };
            tab.Controls.Add(chkPrivateWelcome);

            chkGroupWelcome = new CheckBox { Text = "群内欢迎", Location = new Point(120, 15), AutoSize = true };
            tab.Controls.Add(chkGroupWelcome);

            var lblDelay = new Label { Text = "欢迎延迟(毫秒):", Location = new Point(220, 15), AutoSize = true };
            tab.Controls.Add(lblDelay);
            nudWelcomeDelay = new NumericUpDown { Location = new Point(320, 13), Width = 80, Maximum = 10000 };
            tab.Controls.Add(nudWelcomeDelay);

            // 私聊欢迎消息
            var lblPrivateMsg = new Label { Text = "私聊欢迎消息:", Location = new Point(15, 50), AutoSize = true };
            tab.Controls.Add(lblPrivateMsg);
            txtPrivateWelcomeMsg = new TextBox
            {
                Location = new Point(15, 75),
                Width = 400,
                Height = 60,
                Multiline = true
            };
            tab.Controls.Add(txtPrivateWelcomeMsg);

            // 群内欢迎消息
            var lblGroupMsg = new Label { Text = "群内欢迎消息:", Location = new Point(15, 145), AutoSize = true };
            tab.Controls.Add(lblGroupMsg);
            txtGroupWelcomeMsg = new TextBox
            {
                Location = new Point(15, 170),
                Width = 400,
                Height = 60,
                Multiline = true
            };
            tab.Controls.Add(txtGroupWelcomeMsg);

            // 自动同意
            var lblAutoAccept = new Label { Text = "自动同意申请:", Location = new Point(15, 245), AutoSize = true };
            tab.Controls.Add(lblAutoAccept);

            chkAutoAcceptFriend = new CheckBox { Text = "好友申请", Location = new Point(110, 245), AutoSize = true };
            tab.Controls.Add(chkAutoAcceptFriend);

            chkAutoAcceptBill = new CheckBox { Text = "账单玩家入群", Location = new Point(200, 245), AutoSize = true };
            tab.Controls.Add(chkAutoAcceptBill);

            chkAutoAcceptTrustee = new CheckBox { Text = "托管玩家入群", Location = new Point(320, 245), AutoSize = true };
            tab.Controls.Add(chkAutoAcceptTrustee);

            var lblVars = new Label
            {
                Text = "变量: [旺旺]=昵称, [昵称]=昵称",
                Location = new Point(15, 280),
                AutoSize = true,
                ForeColor = Color.Gray
            };
            tab.Controls.Add(lblVars);
        }

        #endregion

        private void LoadConfig()
        {
            // 发言检测配置
            var speechConfig = SpeechDetectionService.Instance.GetConfig();
            chkSpeechEnabled.Checked = speechConfig.Enabled;
            nudMuteCharLimit.Value = speechConfig.MuteCharLimit;
            nudKickCharLimit.Value = speechConfig.KickCharLimit;
            nudMuteLineLimit.Value = speechConfig.MuteLineLimit;
            chkImageMute.Checked = speechConfig.ImageMuteEnabled;
            nudImageKickCount.Value = speechConfig.ImageKickCount;
            nudMuteDuration.Value = speechConfig.MuteDuration;
            chkWithdrawViolation.Checked = speechConfig.WithdrawViolation;
            chkZeroBalanceMute.Checked = speechConfig.ZeroBalanceMuteIfNotDeposit;
            chkAutoBlacklistOnKick.Checked = speechConfig.AutoBlacklistOnKick;
            chkAutoBlacklistOnAdminKick.Checked = speechConfig.AutoBlacklistOnAdminKick;
            txtForbiddenWords.Text = string.Join("|", speechConfig.ForbiddenWords);
            RefreshBlacklist();

            // 锁名片配置
            var cardConfig = CardLockService.Instance.GetConfig();
            chkCardLockEnabled.Checked = cardConfig.Enabled;
            nudMaxChangeCount.Value = cardConfig.MaxChangeCount;
            chkKickOnExceed.Checked = cardConfig.KickOnExceed;
            chkNotifyInGroup.Checked = cardConfig.NotifyInGroup;
            chkAutoResetCard.Checked = cardConfig.AutoResetCard;
            txtWarningTemplate.Text = cardConfig.WarningTemplate;
            txtKickTemplate.Text = cardConfig.KickTemplate;
            RefreshCardInfo();

            // 进群欢迎配置
            var welcomeConfig = WelcomeService.Instance.GetConfig();
            chkPrivateWelcome.Checked = welcomeConfig.PrivateWelcomeEnabled;
            chkGroupWelcome.Checked = welcomeConfig.GroupWelcomeEnabled;
            txtPrivateWelcomeMsg.Text = welcomeConfig.PrivateWelcomeMessage;
            txtGroupWelcomeMsg.Text = welcomeConfig.GroupWelcomeMessage;
            chkAutoAcceptFriend.Checked = welcomeConfig.AutoAcceptFriend;
            chkAutoAcceptBill.Checked = welcomeConfig.AutoAcceptJoinFromBill;
            chkAutoAcceptTrustee.Checked = welcomeConfig.AutoAcceptJoinFromTrustee;
            nudWelcomeDelay.Value = welcomeConfig.WelcomeDelayMs;
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            // 保存发言检测配置
            var speechConfig = new SpeechDetectionConfig
            {
                Enabled = chkSpeechEnabled.Checked,
                MuteCharLimit = (int)nudMuteCharLimit.Value,
                KickCharLimit = (int)nudKickCharLimit.Value,
                MuteLineLimit = (int)nudMuteLineLimit.Value,
                ImageMuteEnabled = chkImageMute.Checked,
                ImageKickCount = (int)nudImageKickCount.Value,
                MuteDuration = (int)nudMuteDuration.Value,
                WithdrawViolation = chkWithdrawViolation.Checked,
                ZeroBalanceMuteIfNotDeposit = chkZeroBalanceMute.Checked,
                AutoBlacklistOnKick = chkAutoBlacklistOnKick.Checked,
                AutoBlacklistOnAdminKick = chkAutoBlacklistOnAdminKick.Checked,
                ForbiddenWords = txtForbiddenWords.Text.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries).ToList()
            };
            SpeechDetectionService.Instance.SaveConfig(speechConfig);

            // 保存锁名片配置
            var cardConfig = new CardLockConfig
            {
                Enabled = chkCardLockEnabled.Checked,
                MaxChangeCount = (int)nudMaxChangeCount.Value,
                KickOnExceed = chkKickOnExceed.Checked,
                NotifyInGroup = chkNotifyInGroup.Checked,
                AutoResetCard = chkAutoResetCard.Checked,
                WarningTemplate = txtWarningTemplate.Text,
                KickTemplate = txtKickTemplate.Text
            };
            CardLockService.Instance.SaveConfig(cardConfig);

            // 保存进群欢迎配置
            var welcomeConfig = new WelcomeConfig
            {
                PrivateWelcomeEnabled = chkPrivateWelcome.Checked,
                GroupWelcomeEnabled = chkGroupWelcome.Checked,
                PrivateWelcomeMessage = txtPrivateWelcomeMsg.Text,
                GroupWelcomeMessage = txtGroupWelcomeMsg.Text,
                AutoAcceptFriend = chkAutoAcceptFriend.Checked,
                AutoAcceptJoinFromBill = chkAutoAcceptBill.Checked,
                AutoAcceptJoinFromTrustee = chkAutoAcceptTrustee.Checked,
                WelcomeDelayMs = (int)nudWelcomeDelay.Value
            };
            WelcomeService.Instance.SaveConfig(welcomeConfig);

            lblStatus.Text = "✓ 配置已保存";
        }
    }
}
