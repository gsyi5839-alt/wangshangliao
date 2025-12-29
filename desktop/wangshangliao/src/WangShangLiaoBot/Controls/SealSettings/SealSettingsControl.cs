using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace WangShangLiaoBot.Controls.SealSettings
{
    /// <summary>
    /// Main seal settings control with tabs for PC/加拿大/比特/北京
    /// </summary>
    public class SealSettingsControl : UserControl
    {
        // Tab control and pages
        private TabControl _tabControl;
        private SealTabPage _tabPC;
        private SealTabPage _tabCanada;
        private SealTabPage _tabBitcoin;
        private SealTabPage _tabBeijing;
        
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
        /// Event raised when settings are saved
        /// </summary>
        public event EventHandler SettingsSaved;
        
        /// <summary>
        /// Event raised when unmute button is clicked
        /// </summary>
        public event EventHandler UnmuteClicked;
        
        public SealSettingsControl()
        {
            _configPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, 
                "Data", "seal_settings.xml");
            
            InitializeComponent();
            LoadSettings();
        }
        
        private void InitializeComponent()
        {
            this.Size = new Size(520, 500);
            this.BackColor = SystemColors.Control;
            
            // Create tab control
            _tabControl = new TabControl
            {
                Location = new Point(5, 5),
                Size = new Size(505, 430),
                Dock = DockStyle.Top
            };
            
            // Create tab pages in order: PC, 加拿大, 比特, 北京
            _tabPC = new SealTabPage("PC", "PC");
            _tabCanada = new SealTabPage("加拿大", "加拿大");
            _tabBitcoin = new SealTabPage("比特", "比特");
            _tabBeijing = new SealTabPage("北京", "北京");
            
            // Set default checkbox states based on design
            _tabPC.SetPanel1Enabled(true, false);   // 70秒提醒: 私聊发送勾选
            _tabPC.SetPanel3Enabled(true, false);   // 1秒发送规矩: 私聊发送勾选
            
            _tabCanada.SetPanel1Enabled(true, false);  // 70秒提醒: 私聊发送勾选
            _tabCanada.SetPanel3Enabled(true, true);   // 30秒发送规矩: 私聊发送和图片都勾选
            
            _tabBitcoin.SetPanel1Enabled(true, false); // 25秒提醒: 私聊发送勾选
            _tabBitcoin.SetPanel3Enabled(true, false); // 1秒发送规矩: 私聊发送勾选
            
            _tabControl.TabPages.Add(_tabPC);
            _tabControl.TabPages.Add(_tabCanada);
            _tabControl.TabPages.Add(_tabBitcoin);
            _tabControl.TabPages.Add(_tabBeijing);
            
            // Save button
            _btnSave = new Button
            {
                Text = "保存设置",
                Location = new Point(415, 440),
                Size = new Size(80, 25)
            };
            _btnSave.Click += BtnSave_Click;
            
            // Status panel at bottom
            _statusPanel = new Panel
            {
                Location = new Point(0, 470),
                Size = new Size(520, 30),
                Dock = DockStyle.Bottom
            };
            
            // Player count label
            _lblPlayerCount = new Label
            {
                Text = "玩家人数: 0",
                Location = new Point(5, 8),
                AutoSize = true
            };
            
            // Total score label
            _lblTotalScore = new Label
            {
                Text = "总分数: 0",
                Location = new Point(90, 8),
                AutoSize = true
            };
            
            // Current period profit label
            _lblCurrentProfit = new Label
            {
                Text = "本期盈利: 0",
                Location = new Point(175, 8),
                AutoSize = true
            };
            
            // Today profit label
            _lblTodayProfit = new Label
            {
                Text = "今天总盈利: 0",
                Location = new Point(280, 8),
                AutoSize = true
            };
            
            // Unmute button
            _btnUnmute = new Button
            {
                Text = "解除禁言",
                Location = new Point(420, 3),
                Size = new Size(70, 24)
            };
            _btnUnmute.Click += BtnUnmute_Click;
            
            // Add controls to status panel
            _statusPanel.Controls.Add(_lblPlayerCount);
            _statusPanel.Controls.Add(_lblTotalScore);
            _statusPanel.Controls.Add(_lblCurrentProfit);
            _statusPanel.Controls.Add(_lblTodayProfit);
            _statusPanel.Controls.Add(_btnUnmute);
            
            // Add controls to main control
            this.Controls.Add(_tabControl);
            this.Controls.Add(_btnSave);
            this.Controls.Add(_statusPanel);
        }
        
        private void BtnSave_Click(object sender, EventArgs e)
        {
            SaveSettings();
            SettingsSaved?.Invoke(this, EventArgs.Empty);
            MessageBox.Show("设置已保存", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        
        private void BtnUnmute_Click(object sender, EventArgs e)
        {
            UnmuteClicked?.Invoke(this, EventArgs.Empty);
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
        /// Get all seal settings data
        /// </summary>
        public SealSettingsData GetAllSettings()
        {
            return new SealSettingsData
            {
                PC = _tabPC.GetData(),
                Canada = _tabCanada.GetData(),
                Bitcoin = _tabBitcoin.GetData(),
                Beijing = _tabBeijing.GetData()
            };
        }
        
        /// <summary>
        /// Load seal settings from data
        /// </summary>
        public void LoadAllSettings(SealSettingsData data)
        {
            if (data == null) return;
            
            _tabPC.LoadData(data.PC);
            _tabCanada.LoadData(data.Canada);
            _tabBitcoin.LoadData(data.Bitcoin);
            _tabBeijing.LoadData(data.Beijing);
        }
        
        /// <summary>
        /// Save settings to XML file
        /// </summary>
        public void SaveSettings()
        {
            try
            {
                var data = GetAllSettings();
                
                // Ensure directory exists
                var dir = Path.GetDirectoryName(_configPath);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                
                var serializer = new XmlSerializer(typeof(SealSettingsData));
                using (var writer = new StreamWriter(_configPath))
                {
                    serializer.Serialize(writer, data);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存设置失败: {ex.Message}", "错误", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                
                var serializer = new XmlSerializer(typeof(SealSettingsData));
                using (var reader = new StreamReader(_configPath))
                {
                    var data = (SealSettingsData)serializer.Deserialize(reader);
                    LoadAllSettings(data);
                }
            }
            catch (Exception ex)
            {
                // Log error but don't show message on startup
                System.Diagnostics.Debug.WriteLine($"Load seal settings failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Get settings for a specific game type
        /// </summary>
        public SealTabData GetSettingsForGame(string gameType)
        {
            switch (gameType)
            {
                case "PC":
                    return _tabPC.GetData();
                case "加拿大":
                    return _tabCanada.GetData();
                case "比特":
                    return _tabBitcoin.GetData();
                case "北京":
                    return _tabBeijing.GetData();
                default:
                    return null;
            }
        }
    }
    
    /// <summary>
    /// Data class for all seal settings
    /// </summary>
    [Serializable]
    public class SealSettingsData
    {
        public SealTabData PC { get; set; }
        public SealTabData Canada { get; set; }
        public SealTabData Bitcoin { get; set; }
        public SealTabData Beijing { get; set; }
    }
}

