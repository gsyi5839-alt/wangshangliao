using System;
using System.Drawing;
using System.Windows.Forms;

namespace WangShangLiaoBot.Controls.SealSettings
{
    /// <summary>
    /// Single seal message setting panel (one row in seal settings)
    /// Contains: seconds input, label, checkboxes, message textbox, image area
    /// </summary>
    public class SealMessagePanel : Panel
    {
        // Controls
        private NumericUpDown _secondsInput;
        private Label _labelText;
        private CheckBox _chkPrivateChat;
        private CheckBox _chkImage;
        private TextBox _messageBox;
        private PictureBox _imageBox;
        private Button _btnImportImage;
        private Button _btnClearImage;
        
        // Properties
        public int Seconds
        {
            get => (int)_secondsInput.Value;
            set => _secondsInput.Value = value;
        }
        
        public string LabelText
        {
            get => _labelText.Text;
            set => _labelText.Text = value;
        }
        
        public bool PrivateChatEnabled
        {
            get => _chkPrivateChat.Checked;
            set => _chkPrivateChat.Checked = value;
        }
        
        public bool ImageEnabled
        {
            get => _chkImage.Checked;
            set => _chkImage.Checked = value;
        }
        
        public string MessageContent
        {
            get => _messageBox.Text;
            set => _messageBox.Text = value;
        }
        
        public Image MessageImage
        {
            get => _imageBox.Image;
            set => _imageBox.Image = value;
        }
        
        /// <summary>
        /// Create seal message panel
        /// </summary>
        /// <param name="labelText">Label text (秒提醒/秒封盘/秒发送规矩)</param>
        /// <param name="defaultSeconds">Default seconds value</param>
        public SealMessagePanel(string labelText, int defaultSeconds = 0)
        {
            InitializeComponent(labelText, defaultSeconds);
        }
        
        private void InitializeComponent(string labelText, int defaultSeconds)
        {
            this.Height = 90;
            this.Dock = DockStyle.Top;
            this.Padding = new Padding(5, 5, 5, 0);
            
            // Seconds input
            _secondsInput = new NumericUpDown
            {
                Location = new Point(5, 5),
                Width = 50,
                Maximum = 999,
                Minimum = 0,
                Value = defaultSeconds
            };
            
            // Label
            _labelText = new Label
            {
                Text = labelText,
                Location = new Point(60, 8),
                AutoSize = true
            };
            
            // Private chat checkbox
            _chkPrivateChat = new CheckBox
            {
                Text = "私聊发送",
                Location = new Point(145, 7),
                AutoSize = true
            };
            
            // Image checkbox
            _chkImage = new CheckBox
            {
                Text = "图片",
                Location = new Point(220, 7),
                AutoSize = true
            };
            
            // Message textbox
            _messageBox = new TextBox
            {
                Location = new Point(5, 30),
                Width = 260,
                Height = 55,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };
            
            // Image display box
            _imageBox = new PictureBox
            {
                Location = new Point(275, 5),
                Width = 100,
                Height = 80,
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom
            };
            
            // Import image button
            _btnImportImage = new Button
            {
                Text = "导入图片",
                Location = new Point(385, 15),
                Width = 70,
                Height = 25
            };
            _btnImportImage.Click += BtnImportImage_Click;
            
            // Clear image button
            _btnClearImage = new Button
            {
                Text = "清除图片",
                Location = new Point(385, 50),
                Width = 70,
                Height = 25
            };
            _btnClearImage.Click += BtnClearImage_Click;
            
            // Add controls
            this.Controls.Add(_secondsInput);
            this.Controls.Add(_labelText);
            this.Controls.Add(_chkPrivateChat);
            this.Controls.Add(_chkImage);
            this.Controls.Add(_messageBox);
            this.Controls.Add(_imageBox);
            this.Controls.Add(_btnImportImage);
            this.Controls.Add(_btnClearImage);
        }
        
        private void BtnImportImage_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = "图片文件|*.png;*.jpg;*.jpeg;*.gif;*.bmp";
                ofd.Title = "选择图片";
                
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        _imageBox.Image = Image.FromFile(ofd.FileName);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"加载图片失败: {ex.Message}", "错误", 
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
        
        private void BtnClearImage_Click(object sender, EventArgs e)
        {
            if (_imageBox.Image != null)
            {
                _imageBox.Image.Dispose();
                _imageBox.Image = null;
            }
        }
        
        /// <summary>
        /// Get settings as data object
        /// </summary>
        public SealMessageData GetData()
        {
            return new SealMessageData
            {
                Seconds = this.Seconds,
                PrivateChatEnabled = this.PrivateChatEnabled,
                ImageEnabled = this.ImageEnabled,
                MessageContent = this.MessageContent
            };
        }
        
        /// <summary>
        /// Load settings from data object
        /// </summary>
        public void LoadData(SealMessageData data)
        {
            if (data == null) return;
            
            this.Seconds = data.Seconds;
            this.PrivateChatEnabled = data.PrivateChatEnabled;
            this.ImageEnabled = data.ImageEnabled;
            this.MessageContent = data.MessageContent;
        }
    }
    
    /// <summary>
    /// Data class for seal message settings
    /// </summary>
    [Serializable]
    public class SealMessageData
    {
        public int Seconds { get; set; }
        public bool PrivateChatEnabled { get; set; }
        public bool ImageEnabled { get; set; }
        public string MessageContent { get; set; }
        public string ImagePath { get; set; }
    }
}

