using System;
using System.Drawing;
using System.Windows.Forms;

namespace WangShangLiaoBot.Forms
{
    /// <summary>
    /// 启动画面 - 在主窗口加载时显示
    /// </summary>
    public class SplashForm : Form
    {
        private Label lblTitle;
        private Label lblStatus;
        private ProgressBar progressBar;
        
        public SplashForm()
        {
            InitializeComponent();
        }
        
        private void InitializeComponent()
        {
            this.SuspendLayout();
            
            // Form settings
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Size = new Size(350, 150);
            this.BackColor = Color.FromArgb(45, 45, 48);
            this.ShowInTaskbar = false;
            this.TopMost = true;
            
            // Title
            lblTitle = new Label();
            lblTitle.Text = "旺商聊机器人";
            lblTitle.Font = new Font("Microsoft YaHei UI", 18F, FontStyle.Bold);
            lblTitle.ForeColor = Color.White;
            lblTitle.AutoSize = true;
            lblTitle.Location = new Point(80, 25);
            this.Controls.Add(lblTitle);
            
            // Status
            lblStatus = new Label();
            lblStatus.Text = "正在加载...";
            lblStatus.Font = new Font("Microsoft YaHei UI", 10F);
            lblStatus.ForeColor = Color.LightGray;
            lblStatus.AutoSize = true;
            lblStatus.Location = new Point(125, 70);
            this.Controls.Add(lblStatus);
            
            // Progress bar
            progressBar = new ProgressBar();
            progressBar.Style = ProgressBarStyle.Marquee;
            progressBar.MarqueeAnimationSpeed = 30;
            progressBar.Location = new Point(50, 105);
            progressBar.Size = new Size(250, 15);
            this.Controls.Add(progressBar);
            
            this.ResumeLayout(false);
        }
        
        public void UpdateStatus(string status)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => UpdateStatus(status)));
                return;
            }
            lblStatus.Text = status;
        }
    }
}
