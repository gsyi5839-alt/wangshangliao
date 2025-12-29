using System;
using System.Drawing;
using System.Windows.Forms;
using WangShangLiaoBot.Controls.SealSettings;

namespace WangShangLiaoBot.Forms
{
    /// <summary>
    /// Seal settings form - contains SealSettingsControl with tabs for PC/加拿大/比特/北京
    /// </summary>
    public class SealSettingsForm : Form
    {
        private SealSettingsControl _sealSettingsControl;
        
        public SealSettingsForm()
        {
            InitializeComponent();
        }
        
        private void InitializeComponent()
        {
            this.Text = "封盘设置";
            this.Size = new Size(540, 560);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            
            // Create seal settings control
            _sealSettingsControl = new SealSettingsControl
            {
                Dock = DockStyle.Fill
            };
            
            // Subscribe to events
            _sealSettingsControl.SettingsSaved += SealSettingsControl_SettingsSaved;
            _sealSettingsControl.UnmuteClicked += SealSettingsControl_UnmuteClicked;
            
            this.Controls.Add(_sealSettingsControl);
        }
        
        private void SealSettingsControl_SettingsSaved(object sender, EventArgs e)
        {
            // Settings saved notification handled by control itself
        }
        
        private void SealSettingsControl_UnmuteClicked(object sender, EventArgs e)
        {
            // Handle unmute request
            try
            {
                var chatService = Services.ChatService.Instance;
                if (chatService != null && chatService.IsConnected)
                {
                    // Send unmute command to group
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
            _sealSettingsControl?.UpdateStatus(playerCount, totalScore, currentProfit, todayProfit);
        }
        
        /// <summary>
        /// Get seal settings for a specific game type
        /// </summary>
        public SealTabData GetSettingsForGame(string gameType)
        {
            return _sealSettingsControl?.GetSettingsForGame(gameType);
        }
        
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Save settings when form closes
            _sealSettingsControl?.SaveSettings();
            base.OnFormClosing(e);
        }
    }
}

