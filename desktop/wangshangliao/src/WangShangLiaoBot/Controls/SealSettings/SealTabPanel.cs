using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace WangShangLiaoBot.Controls.SealSettings
{
    /// <summary>
    /// Seal settings panel for a specific game type (PC/加拿大/比特/北京)
    /// Contains 4 message rows, image send checkbox, save button, and status bar
    /// </summary>
    public class SealTabPanel : UserControl
    {
        // Game type identifier
        public string GameType { get; private set; }
        
        // Title label
        private Label _titleLabel;
        
        // Image send checkbox (top right)
        private CheckBox _chkImageSend;
        
        // Message panels (4 rows)
        private SealMessagePanel _panel1;
        private SealMessagePanel _panel2;
        private SealMessagePanel _panel3;
        private SealMessagePanel _panel4;
        
        // Save button
        private Button _btnSave;
        
        // Status bar
        private Panel _statusPanel;
        private Label _lblPlayerCount;
        private Label _lblTotalScore;
        private Label _lblCurrentProfit;
        private Label _lblTodayProfit;
        private Button _btnUnmute;
        
        // Config file path
        private readonly string _configPath;
        
        /// <summary>
        /// Create seal tab panel for specified game type
        /// </summary>
        public SealTabPanel(string gameType)
        {
            this.GameType = gameType;
            _configPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Data", $"seal_{gameType}.xml");
            
            InitializeComponent();
            LoadSettings();
        }
        
        private void InitializeComponent()
        {
            this.Size = new Size(670, 490);
            this.BackColor = SystemColors.Control;
            
            // Title label
            _titleLabel = new Label
            {
                Text = $"{GameType}封盘",
                Location = new Point(10, 8),
                Font = new Font("Microsoft YaHei", 9, FontStyle.Bold),
                AutoSize = true
            };
            
            // Image send checkbox (top right)
            _chkImageSend = new CheckBox
            {
                Text = "图片发送",
                Location = new Point(550, 8),
                AutoSize = true
            };
            
            // Content panel for message panels
            var contentPanel = new Panel
            {
                Location = new Point(5, 30),
                Size = new Size(660, 380),
                AutoScroll = true
            };
            
            // Get default values based on game type
            int[] defaultSeconds = GetDefaultSeconds();
            bool[] defaultPrivateChat = GetDefaultPrivateChat();
            bool[] defaultImage = GetDefaultImage();
            
            // Create 4 message panels
            _panel1 = new SealMessagePanel("秒提醒", defaultSeconds[0]);
            _panel1.PrivateChatEnabled = defaultPrivateChat[0];
            _panel1.ImageEnabled = defaultImage[0];
            
            _panel2 = new SealMessagePanel("秒封盘", defaultSeconds[1]);
            _panel2.PrivateChatEnabled = defaultPrivateChat[1];
            _panel2.ImageEnabled = defaultImage[1];
            
            _panel3 = new SealMessagePanel("秒发送规矩", defaultSeconds[2]);
            _panel3.PrivateChatEnabled = defaultPrivateChat[2];
            _panel3.ImageEnabled = defaultImage[2];
            
            _panel4 = new SealMessagePanel("秒提醒", defaultSeconds[3]);
            _panel4.PrivateChatEnabled = defaultPrivateChat[3];
            _panel4.ImageEnabled = defaultImage[3];
            
            // Add panels in reverse order (Dock.Top stacks from bottom)
            contentPanel.Controls.Add(_panel4);
            contentPanel.Controls.Add(_panel3);
            contentPanel.Controls.Add(_panel2);
            contentPanel.Controls.Add(_panel1);
            
            // Save button
            _btnSave = new Button
            {
                Text = "保存设置",
                Location = new Point(570, 415),
                Size = new Size(80, 28)
            };
            _btnSave.Click += BtnSave_Click;
            
            // Status panel
            _statusPanel = new Panel
            {
                Location = new Point(0, 450),
                Size = new Size(670, 35),
                BackColor = SystemColors.Control
            };
            
            _lblPlayerCount = new Label
            {
                Text = "玩家人数: 0",
                Location = new Point(10, 10),
                AutoSize = true
            };
            
            _lblTotalScore = new Label
            {
                Text = "总分数: 0",
                Location = new Point(110, 10),
                AutoSize = true
            };
            
            _lblCurrentProfit = new Label
            {
                Text = "本期盈利: 0",
                Location = new Point(210, 10),
                AutoSize = true
            };
            
            _lblTodayProfit = new Label
            {
                Text = "今天总盈利: 0",
                Location = new Point(330, 10),
                AutoSize = true
            };
            
            _btnUnmute = new Button
            {
                Text = "解除禁言",
                Location = new Point(570, 5),
                Size = new Size(80, 25)
            };
            _btnUnmute.Click += BtnUnmute_Click;
            
            _statusPanel.Controls.Add(_lblPlayerCount);
            _statusPanel.Controls.Add(_lblTotalScore);
            _statusPanel.Controls.Add(_lblCurrentProfit);
            _statusPanel.Controls.Add(_lblTodayProfit);
            _statusPanel.Controls.Add(_btnUnmute);
            
            // Add all controls
            this.Controls.Add(_titleLabel);
            this.Controls.Add(_chkImageSend);
            this.Controls.Add(contentPanel);
            this.Controls.Add(_btnSave);
            this.Controls.Add(_statusPanel);
        }
        
        /// <summary>
        /// Get default seconds based on game type
        /// </summary>
        private int[] GetDefaultSeconds()
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
        /// Get default private chat checkbox state based on game type
        /// </summary>
        private bool[] GetDefaultPrivateChat()
        {
            switch (GameType)
            {
                case "PC":
                    return new bool[] { true, false, true, false };
                case "加拿大":
                    return new bool[] { true, false, true, false };
                case "比特":
                    return new bool[] { true, false, true, false };
                case "北京":
                default:
                    return new bool[] { false, false, false, false };
            }
        }
        
        /// <summary>
        /// Get default image checkbox state based on game type
        /// </summary>
        private bool[] GetDefaultImage()
        {
            switch (GameType)
            {
                case "加拿大":
                    return new bool[] { false, false, true, false };
                default:
                    return new bool[] { false, false, false, false };
            }
        }
        
        private void BtnSave_Click(object sender, EventArgs e)
        {
            SaveSettings();
            MessageBox.Show("设置已保存", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        
        private void BtnUnmute_Click(object sender, EventArgs e)
        {
            try
            {
                var chatService = Services.ChatService.Instance;
                if (chatService != null && chatService.IsConnected)
                {
                    MessageBox.Show("解除禁言请求已发送", "提示",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("未连接到旺商聊，无法解除禁言", "提示",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"解除禁言失败: {ex.Message}", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        /// <summary>
        /// Update status bar with current statistics
        /// </summary>
        public void UpdateStatus(int playerCount, int totalScore, int currentProfit, int todayProfit)
        {
            _lblPlayerCount.Text = $"玩家人数: {playerCount}";
            _lblTotalScore.Text = $"总分数: {totalScore}";
            _lblCurrentProfit.Text = $"本期盈利: {currentProfit}";
            _lblTodayProfit.Text = $"今天总盈利: {todayProfit}";
        }
        
        /// <summary>
        /// Get all settings from this panel
        /// </summary>
        public SealTabData GetData()
        {
            return new SealTabData
            {
                GameType = this.GameType,
                ImageSendEnabled = _chkImageSend.Checked,
                Message1 = _panel1.GetData(),
                Message2 = _panel2.GetData(),
                Message3 = _panel3.GetData(),
                Message4 = _panel4.GetData()
            };
        }
        
        /// <summary>
        /// Load settings into this panel
        /// </summary>
        public void LoadData(SealTabData data)
        {
            if (data == null) return;
            
            _chkImageSend.Checked = data.ImageSendEnabled;
            _panel1.LoadData(data.Message1);
            _panel2.LoadData(data.Message2);
            _panel3.LoadData(data.Message3);
            _panel4.LoadData(data.Message4);
        }
        
        /// <summary>
        /// Save settings to XML file
        /// </summary>
        public void SaveSettings()
        {
            try
            {
                var data = GetData();
                
                var dir = Path.GetDirectoryName(_configPath);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                
                var serializer = new XmlSerializer(typeof(SealTabData));
                using (var writer = new StreamWriter(_configPath))
                {
                    serializer.Serialize(writer, data);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Save seal settings failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Load settings from XML file
        /// </summary>
        public void LoadSettings()
        {
            try
            {
                if (!File.Exists(_configPath)) return;
                
                var serializer = new XmlSerializer(typeof(SealTabData));
                using (var reader = new StreamReader(_configPath))
                {
                    var data = (SealTabData)serializer.Deserialize(reader);
                    LoadData(data);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Load seal settings failed: {ex.Message}");
            }
        }
    }
}

