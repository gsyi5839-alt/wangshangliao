using System.Drawing;
using System.Windows.Forms;

namespace WangShangLiaoBot.Controls.OtherSettings.Faq
{
    /// <summary>
    /// 常见问题 - 不艾特但显示昵称 面板
    /// </summary>
    public sealed class NoAtButShowNickPanel : UserControl
    {
        private TextBox txtContent;

        public NoAtButShowNickPanel()
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
                Text = @"【开启了】私聊版托：因私聊托本身是虚拟玩家，不在群里，没办法在群里艾特，所有的艾特只能变成昵称发送
请将以上问题解决后再次测试"
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

