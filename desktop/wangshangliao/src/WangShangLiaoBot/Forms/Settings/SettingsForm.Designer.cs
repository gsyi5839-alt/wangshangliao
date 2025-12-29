namespace WangShangLiaoBot.Forms.Settings
{
    partial class SettingsForm
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

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.tabSettings = new System.Windows.Forms.TabControl();
            this.tabBillSettings = new System.Windows.Forms.TabPage();
            this.tabBetProcess = new System.Windows.Forms.TabPage();
            this.tabOdds = new System.Windows.Forms.TabPage();
            this.tabAutoReply = new System.Windows.Forms.TabPage();
            this.tabBlacklist = new System.Windows.Forms.TabPage();
            this.tabCard = new System.Windows.Forms.TabPage();
            this.tabTrustee = new System.Windows.Forms.TabPage();
            this.tabBonus = new System.Windows.Forms.TabPage();
            this.tabTrusteeSettings = new System.Windows.Forms.TabPage();
            this.tabOther = new System.Windows.Forms.TabPage();
            
            this.tabSettings.SuspendLayout();
            this.SuspendLayout();
            
            // tabSettings
            this.tabSettings.Controls.Add(this.tabBillSettings);
            this.tabSettings.Controls.Add(this.tabBetProcess);
            this.tabSettings.Controls.Add(this.tabOdds);
            this.tabSettings.Controls.Add(this.tabAutoReply);
            this.tabSettings.Controls.Add(this.tabBlacklist);
            this.tabSettings.Controls.Add(this.tabCard);
            this.tabSettings.Controls.Add(this.tabTrustee);
            this.tabSettings.Controls.Add(this.tabBonus);
            this.tabSettings.Controls.Add(this.tabTrusteeSettings);
            this.tabSettings.Controls.Add(this.tabOther);
            this.tabSettings.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabSettings.Location = new System.Drawing.Point(0, 0);
            this.tabSettings.Name = "tabSettings";
            this.tabSettings.SelectedIndex = 0;
            this.tabSettings.Size = new System.Drawing.Size(900, 600);
            this.tabSettings.TabIndex = 0;
            
            // tabBillSettings - 账单设置
            this.tabBillSettings.Text = "账单设置";
            this.tabBillSettings.Padding = new System.Windows.Forms.Padding(3);
            this.tabBillSettings.Size = new System.Drawing.Size(892, 571);
            this.tabBillSettings.UseVisualStyleBackColor = true;
            
            // tabBetProcess - 下注处理
            this.tabBetProcess.Text = "下注处理";
            this.tabBetProcess.Padding = new System.Windows.Forms.Padding(3);
            this.tabBetProcess.Size = new System.Drawing.Size(892, 571);
            this.tabBetProcess.UseVisualStyleBackColor = true;
            
            // tabOdds - 玩法赔率设置
            this.tabOdds.Text = "玩法赔率设置";
            this.tabOdds.Padding = new System.Windows.Forms.Padding(3);
            this.tabOdds.Size = new System.Drawing.Size(892, 571);
            this.tabOdds.UseVisualStyleBackColor = true;
            
            // tabAutoReply - 自动回复
            this.tabAutoReply.Text = "自动回复";
            this.tabAutoReply.Padding = new System.Windows.Forms.Padding(3);
            this.tabAutoReply.Size = new System.Drawing.Size(892, 571);
            this.tabAutoReply.UseVisualStyleBackColor = true;
            
            // tabBlacklist - 黑名单/刷屏检测
            this.tabBlacklist.Text = "黑名单/刷屏检测";
            this.tabBlacklist.Padding = new System.Windows.Forms.Padding(3);
            this.tabBlacklist.Size = new System.Drawing.Size(892, 571);
            this.tabBlacklist.UseVisualStyleBackColor = true;
            
            // tabCard - 名片
            this.tabCard.Text = "名片";
            this.tabCard.Padding = new System.Windows.Forms.Padding(3);
            this.tabCard.Size = new System.Drawing.Size(892, 571);
            this.tabCard.UseVisualStyleBackColor = true;
            
            // tabTrustee - 托管设置
            this.tabTrustee.Text = "托管设置";
            this.tabTrustee.Padding = new System.Windows.Forms.Padding(3);
            this.tabTrustee.Size = new System.Drawing.Size(892, 571);
            this.tabTrustee.UseVisualStyleBackColor = true;
            
            // tabBonus - 送分活动
            this.tabBonus.Text = "送分活动";
            this.tabBonus.Padding = new System.Windows.Forms.Padding(3);
            this.tabBonus.Size = new System.Drawing.Size(892, 571);
            this.tabBonus.UseVisualStyleBackColor = true;
            
            // tabTrusteeSettings - 托设置
            this.tabTrusteeSettings.Text = "托设置";
            this.tabTrusteeSettings.Padding = new System.Windows.Forms.Padding(3);
            this.tabTrusteeSettings.Size = new System.Drawing.Size(892, 571);
            this.tabTrusteeSettings.UseVisualStyleBackColor = true;
            
            // tabOther - 其他设置
            this.tabOther.Text = "其他设置";
            this.tabOther.Padding = new System.Windows.Forms.Padding(3);
            this.tabOther.Size = new System.Drawing.Size(892, 571);
            this.tabOther.UseVisualStyleBackColor = true;
            
            // SettingsForm
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(900, 600);
            this.Controls.Add(this.tabSettings);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SettingsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "算账设置";
            this.Load += new System.EventHandler(this.SettingsForm_Load);
            
            this.tabSettings.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.TabControl tabSettings;
        private System.Windows.Forms.TabPage tabBillSettings;
        private System.Windows.Forms.TabPage tabBetProcess;
        private System.Windows.Forms.TabPage tabOdds;
        private System.Windows.Forms.TabPage tabAutoReply;
        private System.Windows.Forms.TabPage tabBlacklist;
        private System.Windows.Forms.TabPage tabCard;
        private System.Windows.Forms.TabPage tabTrustee;
        private System.Windows.Forms.TabPage tabBonus;
        private System.Windows.Forms.TabPage tabTrusteeSettings;
        private System.Windows.Forms.TabPage tabOther;
    }
}

