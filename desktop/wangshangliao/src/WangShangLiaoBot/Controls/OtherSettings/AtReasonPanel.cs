using System;
using System.Drawing;
using System.Windows.Forms;
using WangShangLiaoBot.Services;

namespace WangShangLiaoBot.Controls.OtherSettings
{
    /// <summary>
    /// 艾特分原因 - 设置面板
    /// </summary>
    public sealed class AtReasonPanel : UserControl
    {
        private Label lblReasonsTitle;
        private TextBox txtReasons;
        private CheckBox chkPrivateChatNotify;
        private CheckBox chkAllowNoReason;
        private Label lblActivityTitle;
        private TextBox txtActivityReasons;
        private Button btnSave;

        public AtReasonPanel()
        {
            BackColor = SystemColors.Control;
            Font = new Font("Microsoft YaHei UI", 9F);
            Dock = DockStyle.Fill;
            AutoScroll = true;

            InitializeUI();
            LoadSettings();
        }

        private void InitializeUI()
        {
            SuspendLayout();

            int y = 15;

            // Label: 原因用"|"隔开
            lblReasonsTitle = new Label
            {
                Text = "原因用\"|\"隔开",
                Location = new Point(15, y),
                AutoSize = true
            };
            Controls.Add(lblReasonsTitle);
            y += 22;

            // TextBox: 原因列表
            txtReasons = new TextBox
            {
                Location = new Point(15, y),
                Size = new Size(350, 80),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };
            Controls.Add(txtReasons);
            y += 90;

            // CheckBox: 私聊加分群内提醒
            chkPrivateChatNotify = new CheckBox
            {
                Text = "私聊加分群内提醒",
                Location = new Point(15, y),
                Size = new Size(200, 20)
            };
            Controls.Add(chkPrivateChatNotify);
            y += 25;

            // CheckBox: 私聊加分允许无理由
            chkAllowNoReason = new CheckBox
            {
                Text = "私聊加分允许无理由",
                Location = new Point(15, y),
                Size = new Size(200, 20)
            };
            Controls.Add(chkAllowNoReason);
            y += 35;

            // Label: 艾特分统计为活动分，原因用"|"隔开
            lblActivityTitle = new Label
            {
                Text = "艾特分统计为活动分，原因用\"|\"隔开",
                Location = new Point(15, y),
                AutoSize = true
            };
            Controls.Add(lblActivityTitle);
            y += 22;

            // TextBox: 活动分原因
            txtActivityReasons = new TextBox
            {
                Location = new Point(15, y),
                Size = new Size(350, 80),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };
            Controls.Add(txtActivityReasons);
            y += 95;

            // Button: 保存设置
            btnSave = new Button
            {
                Text = "保存设置",
                Location = new Point(280, y),
                Size = new Size(85, 28)
            };
            btnSave.Click += BtnSave_Click;
            Controls.Add(btnSave);

            ResumeLayout(false);
        }

        private void LoadSettings()
        {
            var s = AtReasonService.Instance;
            txtReasons.Text = s.Reasons;
            chkPrivateChatNotify.Checked = s.PrivateChatNotifyInGroup;
            chkAllowNoReason.Checked = s.PrivateChatAllowNoReason;
            txtActivityReasons.Text = s.ActivityReasons;
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            var s = AtReasonService.Instance;
            s.Reasons = txtReasons.Text.Trim();
            s.PrivateChatNotifyInGroup = chkPrivateChatNotify.Checked;
            s.PrivateChatAllowNoReason = chkAllowNoReason.Checked;
            s.ActivityReasons = txtActivityReasons.Text.Trim();

            MessageBox.Show("艾特分原因设置已保存", "提示", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
