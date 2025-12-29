using System.Drawing;
using System.Windows.Forms;
using WangShangLiaoBot.Controls.OtherSettings.Faq;

namespace WangShangLiaoBot.Controls.OtherSettings
{
    /// <summary>
    /// 常见问题 - 主面板（包含二级标签）
    /// </summary>
    public sealed class FaqPanel : UserControl
    {
        private TabControl tabFaq;
        private TabPage tabNoPrivateChat;
        private TabPage tabNoAtButShowNick;

        private NoPrivateChatPanel _noPrivateChatPanel;
        private NoAtButShowNickPanel _noAtButShowNickPanel;

        public FaqPanel()
        {
            BackColor = SystemColors.Control;
            Font = new Font("Microsoft YaHei UI", 9F);
            Dock = DockStyle.Fill;

            InitializeUI();
        }

        private void InitializeUI()
        {
            SuspendLayout();

            // Inner TabControl for sub-tabs
            tabFaq = new TabControl
            {
                Dock = DockStyle.Fill
            };

            // Tab: 不处理私聊消息
            tabNoPrivateChat = new TabPage("不处理私聊消息");
            _noPrivateChatPanel = new NoPrivateChatPanel { Dock = DockStyle.Fill };
            tabNoPrivateChat.Controls.Add(_noPrivateChatPanel);
            tabFaq.TabPages.Add(tabNoPrivateChat);

            // Tab: 不艾特但显示昵称
            tabNoAtButShowNick = new TabPage("不艾特但显示昵称");
            _noAtButShowNickPanel = new NoAtButShowNickPanel { Dock = DockStyle.Fill };
            tabNoAtButShowNick.Controls.Add(_noAtButShowNickPanel);
            tabFaq.TabPages.Add(tabNoAtButShowNick);

            Controls.Add(tabFaq);

            ResumeLayout(false);
        }
    }
}
