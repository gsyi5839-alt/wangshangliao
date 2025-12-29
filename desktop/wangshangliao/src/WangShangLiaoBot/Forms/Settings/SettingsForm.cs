using System;
using System.Windows.Forms;
using WangShangLiaoBot.Controls;

namespace WangShangLiaoBot.Forms.Settings
{
    /// <summary>
    /// 算账设置主窗体 - 包含所有设置标签页
    /// </summary>
    public partial class SettingsForm : Form
    {
        // Tab controls
        private BillSettingsForm billSettingsControl;
        private AutoReplySettingsControl autoReplyControl;
        private OddsSettingsControl oddsSettingsControl;
        private BlacklistSpamSettingsControl blacklistSpamControl;
        
        public SettingsForm()
        {
            InitializeComponent();
        }
        
        private void SettingsForm_Load(object sender, EventArgs e)
        {
            // Initialize tab contents
            InitializeTabContents();
            
            // Default to first tab
            tabSettings.SelectedIndex = 0;
        }
        
        /// <summary>
        /// Initialize all tab page contents with their controls
        /// </summary>
        private void InitializeTabContents()
        {
            // Tab 1: Bill Settings - embedded form
            InitializeBillSettingsTab();
            
            // Tab 3: Odds Settings (玩法赔率设置)
            InitializeOddsSettingsTab();
            
            // Tab 4: Auto Reply Settings
            InitializeAutoReplyTab();

            // Tab 5: Blacklist / Spam
            InitializeBlacklistSpamTab();
            
            // Other tabs - placeholder labels
            InitializePlaceholderTabs();
        }
        
        /// <summary>
        /// Initialize Bill Settings tab with BillSettingsForm content
        /// </summary>
        private void InitializeBillSettingsTab()
        {
            // Create BillSettingsForm and embed its controls
            billSettingsControl = new BillSettingsForm();
            billSettingsControl.TopLevel = false;
            billSettingsControl.FormBorderStyle = FormBorderStyle.None;
            billSettingsControl.Dock = DockStyle.Fill;
            billSettingsControl.Visible = true;
            
            tabBillSettings.Controls.Add(billSettingsControl);
        }
        
        /// <summary>
        /// Initialize Odds Settings tab (玩法赔率设置)
        /// </summary>
        private void InitializeOddsSettingsTab()
        {
            oddsSettingsControl = new OddsSettingsControl();
            oddsSettingsControl.Dock = DockStyle.Fill;
            tabOdds.Controls.Add(oddsSettingsControl);
        }
        
        /// <summary>
        /// Initialize Auto Reply tab
        /// </summary>
        private void InitializeAutoReplyTab()
        {
            autoReplyControl = new AutoReplySettingsControl();
            autoReplyControl.Dock = DockStyle.Fill;
            tabAutoReply.Controls.Add(autoReplyControl);
        }

        /// <summary>
        /// Initialize Blacklist/Spam tab
        /// </summary>
        private void InitializeBlacklistSpamTab()
        {
            blacklistSpamControl = new BlacklistSpamSettingsControl();
            blacklistSpamControl.Dock = DockStyle.Fill;
            tabBlacklist.Controls.Add(blacklistSpamControl);
        }
        
        /// <summary>
        /// Initialize placeholder tabs with "Coming Soon" labels
        /// </summary>
        private void InitializePlaceholderTabs()
        {
            // Tab 2: Bet Process
            AddPlaceholderLabel(tabBetProcess, "下注处理设置开发中...");
            
            // Tab 5: Blacklist
            // 已实现：tabBlacklist 在 InitializeBlacklistSpamTab() 中加载
            
            // Tab 6: Card
            AddPlaceholderLabel(tabCard, "名片设置开发中...");
            
            // Tab 7: Trustee
            AddPlaceholderLabel(tabTrustee, "托管设置开发中...");
            
            // Tab 8: Bonus
            AddPlaceholderLabel(tabBonus, "送分活动设置开发中...");
            
            // Tab 9: Trustee Settings
            AddPlaceholderLabel(tabTrusteeSettings, "托设置开发中...");
            
            // Tab 10: Other
            AddPlaceholderLabel(tabOther, "其他设置开发中...");
        }
        
        /// <summary>
        /// Add placeholder label to a tab page
        /// </summary>
        private void AddPlaceholderLabel(TabPage tab, string text)
        {
            var label = new Label
            {
                Text = text,
                AutoSize = false,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Font = new System.Drawing.Font("Microsoft YaHei UI", 14F, System.Drawing.FontStyle.Regular)
            };
            tab.Controls.Add(label);
        }
    }
}

