using System.Drawing;
using System.Windows.Forms;
using WangShangLiaoBot.Controls.OtherSettings;

namespace WangShangLiaoBot.Controls
{
    /// <summary>
    /// 其他设置控件 - 包含二级Tab
    /// </summary>
    public sealed class OtherSettingsControl : UserControl
    {
        private TabControl tabOther;
        
        // Tab pages
        private TabPage tabAtReason;
        private TabPage tabSensitiveOp;
        private TabPage tabSettlementTime;
        private TabPage tabLotteryDelay;
        private TabPage tabAdminCommand;
        private TabPage tabSendMessage;
        private TabPage tabCommandHelp;
        private TabPage tabFaq;

        // Panels
        private AtReasonPanel _atReasonPanel;
        private SensitiveOperationPanel _sensitiveOpPanel;
        private SettlementTimePanel _settlementTimePanel;
        private LotteryDelayPanel _lotteryDelayPanel;
        private AdminCommandReplacePanel _adminCommandPanel;
        private SendMessageReplacePanel _sendMessagePanel;
        private CommandHelpPanel _commandHelpPanel;
        private FaqPanel _faqPanel;

        public OtherSettingsControl()
        {
            BackColor = SystemColors.Control;
            Font = new Font("Microsoft YaHei UI", 9F);
            MinimumSize = new Size(600, 420);
            Size = new Size(650, 450);

            InitializeUI();
        }

        private void InitializeUI()
        {
            SuspendLayout();

            // Main TabControl
            tabOther = new TabControl
            {
                Dock = DockStyle.Fill
            };
            Controls.Add(tabOther);

            // Tab 1: 艾特分原因
            tabAtReason = new TabPage { Text = "艾特分原因", Padding = new Padding(3) };
            tabOther.TabPages.Add(tabAtReason);
            _atReasonPanel = new AtReasonPanel { Dock = DockStyle.Fill };
            tabAtReason.Controls.Add(_atReasonPanel);

            // Tab 2: 敏感操作开关
            tabSensitiveOp = new TabPage { Text = "敏感操作开关", Padding = new Padding(3) };
            tabOther.TabPages.Add(tabSensitiveOp);
            _sensitiveOpPanel = new SensitiveOperationPanel { Dock = DockStyle.Fill };
            tabSensitiveOp.Controls.Add(_sensitiveOpPanel);

            // Tab 3: 结算时间
            tabSettlementTime = new TabPage { Text = "结算时间", Padding = new Padding(3) };
            tabOther.TabPages.Add(tabSettlementTime);
            _settlementTimePanel = new SettlementTimePanel { Dock = DockStyle.Fill };
            tabSettlementTime.Controls.Add(_settlementTimePanel);

            // Tab 4: 开奖延迟
            tabLotteryDelay = new TabPage { Text = "开奖延迟", Padding = new Padding(3) };
            tabOther.TabPages.Add(tabLotteryDelay);
            _lotteryDelayPanel = new LotteryDelayPanel { Dock = DockStyle.Fill };
            tabLotteryDelay.Controls.Add(_lotteryDelayPanel);

            // Tab 5: 管理命令替换
            tabAdminCommand = new TabPage { Text = "管理命令替换", Padding = new Padding(3) };
            tabOther.TabPages.Add(tabAdminCommand);
            _adminCommandPanel = new AdminCommandReplacePanel { Dock = DockStyle.Fill };
            tabAdminCommand.Controls.Add(_adminCommandPanel);

            // Tab 6: 发送消息替换
            tabSendMessage = new TabPage { Text = "发送消息替换", Padding = new Padding(3) };
            tabOther.TabPages.Add(tabSendMessage);
            _sendMessagePanel = new SendMessageReplacePanel { Dock = DockStyle.Fill };
            tabSendMessage.Controls.Add(_sendMessagePanel);

            // Tab 7: 命令说明
            tabCommandHelp = new TabPage { Text = "命令说明", Padding = new Padding(3) };
            tabOther.TabPages.Add(tabCommandHelp);
            _commandHelpPanel = new CommandHelpPanel { Dock = DockStyle.Fill };
            tabCommandHelp.Controls.Add(_commandHelpPanel);

            // Tab 8: 常见问题
            tabFaq = new TabPage { Text = "常见问题", Padding = new Padding(3) };
            tabOther.TabPages.Add(tabFaq);
            _faqPanel = new FaqPanel { Dock = DockStyle.Fill };
            tabFaq.Controls.Add(_faqPanel);

            ResumeLayout(false);
        }
    }
}

