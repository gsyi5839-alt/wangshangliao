using System;
using System.Drawing;
using System.Windows.Forms;

namespace WangShangLiaoBot.Controls.SealSettings
{
    /// <summary>
    /// Seal settings tab page for a specific game type (PC/加拿大/比特/北京)
    /// Contains 4 message panels and image send checkbox
    /// </summary>
    public class SealTabPage : TabPage
    {
        // Message panels (4 rows)
        private SealMessagePanel _panel1;  // X秒提醒
        private SealMessagePanel _panel2;  // X秒封盘
        private SealMessagePanel _panel3;  // X秒发送规矩
        private SealMessagePanel _panel4;  // X秒提醒 (additional)
        
        // Image send checkbox (top right)
        private CheckBox _chkImageSend;
        
        // Container panel
        private Panel _contentPanel;
        
        // Game type identifier
        public string GameType { get; private set; }
        
        // Properties
        public bool ImageSendEnabled
        {
            get => _chkImageSend.Checked;
            set => _chkImageSend.Checked = value;
        }
        
        /// <summary>
        /// Create seal tab page for specified game type
        /// </summary>
        /// <param name="gameType">Game type (PC/加拿大/比特/北京)</param>
        /// <param name="tabTitle">Tab title</param>
        public SealTabPage(string gameType, string tabTitle)
        {
            this.GameType = gameType;
            this.Text = tabTitle;
            InitializeComponent();
        }
        
        private void InitializeComponent()
        {
            this.Padding = new Padding(5);
            
            // Title label
            var titleLabel = new Label
            {
                Text = $"{GameType}封盘",
                Location = new Point(10, 5),
                Font = new Font("Microsoft YaHei", 9, FontStyle.Bold),
                AutoSize = true
            };
            
            // Image send checkbox (top right)
            _chkImageSend = new CheckBox
            {
                Text = "图片发送",
                Location = new Point(420, 5),
                AutoSize = true
            };
            
            // Content panel for message panels
            _contentPanel = new Panel
            {
                Location = new Point(5, 25),
                Size = new Size(480, 380),
                AutoScroll = true
            };
            
            // Create 4 message panels with different labels based on game type
            string[] labels = GetLabelsForGameType();
            int[] defaultSeconds = GetDefaultSecondsForGameType();
            
            _panel1 = new SealMessagePanel(labels[0], defaultSeconds[0]);
            _panel2 = new SealMessagePanel(labels[1], defaultSeconds[1]);
            _panel3 = new SealMessagePanel(labels[2], defaultSeconds[2]);
            _panel4 = new SealMessagePanel(labels[3], defaultSeconds[3]);
            
            // Add panels in reverse order (Dock.Top stacks from bottom)
            _contentPanel.Controls.Add(_panel4);
            _contentPanel.Controls.Add(_panel3);
            _contentPanel.Controls.Add(_panel2);
            _contentPanel.Controls.Add(_panel1);
            
            // Add to tab page
            this.Controls.Add(titleLabel);
            this.Controls.Add(_chkImageSend);
            this.Controls.Add(_contentPanel);
        }
        
        /// <summary>
        /// Get label texts based on game type
        /// </summary>
        private string[] GetLabelsForGameType()
        {
            // Default labels for all game types
            return new string[]
            {
                "秒提醒",      // Row 1
                "秒封盘",      // Row 2  
                "秒发送规矩",  // Row 3
                "秒提醒"       // Row 4 (additional)
            };
        }
        
        /// <summary>
        /// Get default seconds based on game type
        /// </summary>
        private int[] GetDefaultSecondsForGameType()
        {
            switch (GameType)
            {
                case "PC":
                    return new int[] { 70, 48, 1, 0 };
                case "加拿大":
                    return new int[] { 70, 39, 30, 25 };
                case "比特":
                    return new int[] { 25, 10, 1, 0 };
                case "北京":
                default:
                    return new int[] { 0, 0, 0, 0 };
            }
        }
        
        /// <summary>
        /// Get all settings from this tab
        /// </summary>
        public SealTabData GetData()
        {
            return new SealTabData
            {
                GameType = this.GameType,
                ImageSendEnabled = this.ImageSendEnabled,
                Message1 = _panel1.GetData(),
                Message2 = _panel2.GetData(),
                Message3 = _panel3.GetData(),
                Message4 = _panel4.GetData()
            };
        }
        
        /// <summary>
        /// Load settings into this tab
        /// </summary>
        public void LoadData(SealTabData data)
        {
            if (data == null) return;
            
            this.ImageSendEnabled = data.ImageSendEnabled;
            _panel1.LoadData(data.Message1);
            _panel2.LoadData(data.Message2);
            _panel3.LoadData(data.Message3);
            _panel4.LoadData(data.Message4);
        }
        
        /// <summary>
        /// Set checkbox checked state for panel 1
        /// </summary>
        public void SetPanel1Enabled(bool privateChatEnabled, bool imageEnabled)
        {
            _panel1.PrivateChatEnabled = privateChatEnabled;
            _panel1.ImageEnabled = imageEnabled;
        }
        
        /// <summary>
        /// Set checkbox checked state for panel 2
        /// </summary>
        public void SetPanel2Enabled(bool privateChatEnabled, bool imageEnabled)
        {
            _panel2.PrivateChatEnabled = privateChatEnabled;
            _panel2.ImageEnabled = imageEnabled;
        }
        
        /// <summary>
        /// Set checkbox checked state for panel 3
        /// </summary>
        public void SetPanel3Enabled(bool privateChatEnabled, bool imageEnabled)
        {
            _panel3.PrivateChatEnabled = privateChatEnabled;
            _panel3.ImageEnabled = imageEnabled;
        }
    }
    
    /// <summary>
    /// Data class for seal tab settings
    /// </summary>
    [Serializable]
    public class SealTabData
    {
        public string GameType { get; set; }
        public bool ImageSendEnabled { get; set; }
        public SealMessageData Message1 { get; set; }
        public SealMessageData Message2 { get; set; }
        public SealMessageData Message3 { get; set; }
        public SealMessageData Message4 { get; set; }
    }
}

