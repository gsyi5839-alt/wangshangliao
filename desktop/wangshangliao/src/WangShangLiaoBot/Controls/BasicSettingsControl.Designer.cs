namespace WangShangLiaoBot.Controls
{
    partial class BasicSettingsControl
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
            this.grpBasicSettings = new System.Windows.Forms.GroupBox();
            this.lblAdminId = new System.Windows.Forms.Label();
            this.txtAdminId = new System.Windows.Forms.TextBox();
            this.lblAdminTip = new System.Windows.Forms.Label();
            this.lblGroupId = new System.Windows.Forms.Label();
            this.txtGroupId = new System.Windows.Forms.TextBox();
            this.btnSaveAdmin = new System.Windows.Forms.Button();
            this.btnViewInviteLog = new System.Windows.Forms.Button();
            
            this.grpBasicSettings.SuspendLayout();
            this.SuspendLayout();
            
            // grpBasicSettings
            this.grpBasicSettings.Text = "基本设置";
            this.grpBasicSettings.Location = new System.Drawing.Point(3, 3);
            this.grpBasicSettings.Size = new System.Drawing.Size(290, 140);
            this.grpBasicSettings.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.grpBasicSettings.Controls.Add(this.lblAdminId);
            this.grpBasicSettings.Controls.Add(this.txtAdminId);
            this.grpBasicSettings.Controls.Add(this.lblAdminTip);
            this.grpBasicSettings.Controls.Add(this.lblGroupId);
            this.grpBasicSettings.Controls.Add(this.txtGroupId);
            this.grpBasicSettings.Controls.Add(this.btnSaveAdmin);
            this.grpBasicSettings.Controls.Add(this.btnViewInviteLog);
            
            // lblAdminId
            this.lblAdminId.Text = "管理旺旺号:";
            this.lblAdminId.Location = new System.Drawing.Point(10, 20);
            this.lblAdminId.Size = new System.Drawing.Size(70, 15);
            
            // txtAdminId
            this.txtAdminId.Location = new System.Drawing.Point(80, 17);
            this.txtAdminId.Size = new System.Drawing.Size(200, 21);
            this.txtAdminId.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            
            // lblAdminTip
            this.lblAdminTip.Text = "管理员: 对机器人有绝对的控制权，多个管理号用@分开";
            this.lblAdminTip.Location = new System.Drawing.Point(10, 42);
            this.lblAdminTip.Size = new System.Drawing.Size(270, 15);
            this.lblAdminTip.ForeColor = System.Drawing.Color.Gray;
            this.lblAdminTip.Font = new System.Drawing.Font("宋体", 8F);
            
            // lblGroupId
            this.lblGroupId.Text = "绑定群号:";
            this.lblGroupId.Location = new System.Drawing.Point(10, 62);
            this.lblGroupId.Size = new System.Drawing.Size(60, 15);
            
            // txtGroupId
            this.txtGroupId.Location = new System.Drawing.Point(70, 59);
            this.txtGroupId.Size = new System.Drawing.Size(210, 21);
            this.txtGroupId.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            
            // btnSaveAdmin
            this.btnSaveAdmin.Text = "保存管理号和群号";
            this.btnSaveAdmin.Location = new System.Drawing.Point(10, 88);
            this.btnSaveAdmin.Size = new System.Drawing.Size(130, 25);
            this.btnSaveAdmin.Click += new System.EventHandler(this.btnSaveAdmin_Click);
            
            // btnViewInviteLog
            this.btnViewInviteLog.Text = "查看群成员邀请记录";
            this.btnViewInviteLog.Location = new System.Drawing.Point(145, 88);
            this.btnViewInviteLog.Size = new System.Drawing.Size(135, 25);
            this.btnViewInviteLog.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            this.btnViewInviteLog.Click += new System.EventHandler(this.btnViewInviteLog_Click);
            
            // BasicSettingsControl
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.grpBasicSettings);
            this.Size = new System.Drawing.Size(298, 148);
            this.Dock = System.Windows.Forms.DockStyle.Top;
            
            this.grpBasicSettings.ResumeLayout(false);
            this.grpBasicSettings.PerformLayout();
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.GroupBox grpBasicSettings;
        private System.Windows.Forms.Label lblAdminId;
        private System.Windows.Forms.TextBox txtAdminId;
        private System.Windows.Forms.Label lblAdminTip;
        private System.Windows.Forms.Label lblGroupId;
        private System.Windows.Forms.TextBox txtGroupId;
        private System.Windows.Forms.Button btnSaveAdmin;
        private System.Windows.Forms.Button btnViewInviteLog;
    }
}

