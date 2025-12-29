namespace WangShangLiaoBot.Controls
{
    partial class MuteSettingsControl
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.grpMute = new System.Windows.Forms.GroupBox();
            this.lblMuteBefore = new System.Windows.Forms.Label();
            this.numMuteSeconds = new System.Windows.Forms.NumericUpDown();
            this.lblMuteUnit = new System.Windows.Forms.Label();
            this.chkBetDataTimer = new System.Windows.Forms.CheckBox();
            this.numBetDataSeconds = new System.Windows.Forms.NumericUpDown();
            this.lblBetDataUnit = new System.Windows.Forms.Label();
            this.chkBetImageSend = new System.Windows.Forms.CheckBox();
            this.chkGroupTaskNotify = new System.Windows.Forms.CheckBox();
            this.btnSetBetContent = new System.Windows.Forms.Button();
            
            this.grpMute.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numMuteSeconds)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numBetDataSeconds)).BeginInit();
            this.SuspendLayout();
            
            // grpMute
            this.grpMute.Text = "禁言、核对";
            this.grpMute.Location = new System.Drawing.Point(3, 3);
            this.grpMute.Size = new System.Drawing.Size(290, 100);
            this.grpMute.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.grpMute.Controls.Add(this.lblMuteBefore);
            this.grpMute.Controls.Add(this.numMuteSeconds);
            this.grpMute.Controls.Add(this.lblMuteUnit);
            this.grpMute.Controls.Add(this.chkBetDataTimer);
            this.grpMute.Controls.Add(this.numBetDataSeconds);
            this.grpMute.Controls.Add(this.lblBetDataUnit);
            this.grpMute.Controls.Add(this.chkBetImageSend);
            this.grpMute.Controls.Add(this.chkGroupTaskNotify);
            this.grpMute.Controls.Add(this.btnSetBetContent);
            
            // lblMuteBefore
            this.lblMuteBefore.Text = "封盘前";
            this.lblMuteBefore.Location = new System.Drawing.Point(10, 20);
            this.lblMuteBefore.Size = new System.Drawing.Size(42, 15);
            
            // numMuteSeconds
            this.numMuteSeconds.Location = new System.Drawing.Point(52, 17);
            this.numMuteSeconds.Size = new System.Drawing.Size(40, 21);
            this.numMuteSeconds.Value = 2;
            
            // lblMuteUnit
            this.lblMuteUnit.Text = "秒禁言群(提前禁言)";
            this.lblMuteUnit.Location = new System.Drawing.Point(94, 20);
            this.lblMuteUnit.Size = new System.Drawing.Size(115, 15);
            
            // chkBetDataTimer
            this.chkBetDataTimer.Text = "计时";
            this.chkBetDataTimer.Location = new System.Drawing.Point(10, 44);
            this.chkBetDataTimer.Size = new System.Drawing.Size(48, 18);
            this.chkBetDataTimer.Checked = true;
            
            // numBetDataSeconds
            this.numBetDataSeconds.Location = new System.Drawing.Point(58, 42);
            this.numBetDataSeconds.Size = new System.Drawing.Size(40, 21);
            this.numBetDataSeconds.Value = 10;
            
            // lblBetDataUnit
            this.lblBetDataUnit.Text = "秒发送下注数据到群";
            this.lblBetDataUnit.Location = new System.Drawing.Point(100, 45);
            this.lblBetDataUnit.Size = new System.Drawing.Size(115, 15);
            
            // chkBetImageSend
            this.chkBetImageSend.Text = "图片发送";
            this.chkBetImageSend.Location = new System.Drawing.Point(10, 68);
            this.chkBetImageSend.Size = new System.Drawing.Size(75, 18);
            
            // chkGroupTaskNotify
            this.chkGroupTaskNotify.Text = "群作业发送";
            this.chkGroupTaskNotify.Location = new System.Drawing.Point(90, 68);
            this.chkGroupTaskNotify.Size = new System.Drawing.Size(85, 18);
            
            // btnSetBetContent
            this.btnSetBetContent.Text = "设置下注数据内容";
            this.btnSetBetContent.Location = new System.Drawing.Point(180, 65);
            this.btnSetBetContent.Size = new System.Drawing.Size(105, 23);
            this.btnSetBetContent.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            this.btnSetBetContent.Click += new System.EventHandler(this.btnSetBetContent_Click);
            
            // MuteSettingsControl
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.grpMute);
            this.Size = new System.Drawing.Size(298, 108);
            this.Dock = System.Windows.Forms.DockStyle.Top;
            
            this.grpMute.ResumeLayout(false);
            this.grpMute.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numMuteSeconds)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numBetDataSeconds)).EndInit();
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.GroupBox grpMute;
        private System.Windows.Forms.Label lblMuteBefore;
        private System.Windows.Forms.NumericUpDown numMuteSeconds;
        private System.Windows.Forms.Label lblMuteUnit;
        private System.Windows.Forms.CheckBox chkBetDataTimer;
        private System.Windows.Forms.NumericUpDown numBetDataSeconds;
        private System.Windows.Forms.Label lblBetDataUnit;
        private System.Windows.Forms.CheckBox chkBetImageSend;
        private System.Windows.Forms.CheckBox chkGroupTaskNotify;
        private System.Windows.Forms.Button btnSetBetContent;
    }
}

