using System.Drawing;
using System.Windows.Forms;

namespace WangShangLiaoBot.Controls.OtherSettings.Faq
{
    /// <summary>
    /// 常见问题 - 不处理私聊消息 面板
    /// </summary>
    public sealed class NoPrivateChatPanel : UserControl
    {
        private TextBox txtContent;

        public NoPrivateChatPanel()
        {
            BackColor = SystemColors.Control;
            Font = new Font("Microsoft YaHei UI", 9F);
            Dock = DockStyle.Fill;

            InitializeUI();
        }

        private void InitializeUI()
        {
            SuspendLayout();

            // Content text box (read-only)
            txtContent = new TextBox
            {
                Location = new Point(10, 10),
                Size = new Size(580, 280),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = SystemColors.Window,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Microsoft YaHei UI", 9F),
                Text = @"未发现设置原因
如不能正常处理私聊消息，请检查框架日志，是否接收到对方的私聊消息，并截图联系售后"
            };
            Controls.Add(txtContent);

            // Handle resize
            Resize += (s, e) =>
            {
                txtContent.Size = new Size(Width - 20, Height - 20);
            };

            ResumeLayout(false);
        }
    }
}

