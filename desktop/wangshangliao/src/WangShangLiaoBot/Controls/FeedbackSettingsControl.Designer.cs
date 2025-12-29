namespace WangShangLiaoBot.Controls
{
    partial class FeedbackSettingsControl
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
            this.grpFeedback = new System.Windows.Forms.GroupBox();
            this.lblFeedbackWangWang = new System.Windows.Forms.Label();
            this.txtFeedbackWangWang = new System.Windows.Forms.TextBox();
            this.lblFeedbackGroup = new System.Windows.Forms.Label();
            this.txtFeedbackGroup = new System.Windows.Forms.TextBox();
            this.chkFeedbackToWangWang = new System.Windows.Forms.CheckBox();
            this.chkFeedbackToGroup = new System.Windows.Forms.CheckBox();
            this.chkBetCheckFeedback = new System.Windows.Forms.CheckBox();
            this.chkBetSummaryFeedback = new System.Windows.Forms.CheckBox();
            this.chkProfitFeedback = new System.Windows.Forms.CheckBox();
            this.chkBillSendFeedback = new System.Windows.Forms.CheckBox();
            this.btnSaveSettings = new System.Windows.Forms.Button();
            
            this.grpFeedback.SuspendLayout();
            this.SuspendLayout();
            
            // grpFeedback
            this.grpFeedback.Text = "消息反馈";
            this.grpFeedback.Location = new System.Drawing.Point(3, 3);
            this.grpFeedback.Size = new System.Drawing.Size(290, 160);
            this.grpFeedback.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.grpFeedback.Controls.Add(this.lblFeedbackWangWang);
            this.grpFeedback.Controls.Add(this.txtFeedbackWangWang);
            this.grpFeedback.Controls.Add(this.lblFeedbackGroup);
            this.grpFeedback.Controls.Add(this.txtFeedbackGroup);
            this.grpFeedback.Controls.Add(this.chkFeedbackToWangWang);
            this.grpFeedback.Controls.Add(this.chkFeedbackToGroup);
            this.grpFeedback.Controls.Add(this.chkBetCheckFeedback);
            this.grpFeedback.Controls.Add(this.chkBetSummaryFeedback);
            this.grpFeedback.Controls.Add(this.chkProfitFeedback);
            this.grpFeedback.Controls.Add(this.chkBillSendFeedback);
            this.grpFeedback.Controls.Add(this.btnSaveSettings);
            
            // lblFeedbackWangWang
            this.lblFeedbackWangWang.Text = "旺旺号:";
            this.lblFeedbackWangWang.Location = new System.Drawing.Point(10, 20);
            this.lblFeedbackWangWang.Size = new System.Drawing.Size(48, 15);
            
            // txtFeedbackWangWang
            this.txtFeedbackWangWang.Location = new System.Drawing.Point(58, 17);
            this.txtFeedbackWangWang.Size = new System.Drawing.Size(222, 21);
            this.txtFeedbackWangWang.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            
            // lblFeedbackGroup
            this.lblFeedbackGroup.Text = "群号:";
            this.lblFeedbackGroup.Location = new System.Drawing.Point(10, 45);
            this.lblFeedbackGroup.Size = new System.Drawing.Size(36, 15);
            
            // txtFeedbackGroup
            this.txtFeedbackGroup.Location = new System.Drawing.Point(46, 42);
            this.txtFeedbackGroup.Size = new System.Drawing.Size(234, 21);
            this.txtFeedbackGroup.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            
            // chkFeedbackToWangWang
            this.chkFeedbackToWangWang.Text = "反馈到旺旺号";
            this.chkFeedbackToWangWang.Location = new System.Drawing.Point(10, 68);
            this.chkFeedbackToWangWang.Size = new System.Drawing.Size(100, 18);
            
            // chkFeedbackToGroup
            this.chkFeedbackToGroup.Text = "反馈到群里";
            this.chkFeedbackToGroup.Location = new System.Drawing.Point(115, 68);
            this.chkFeedbackToGroup.Size = new System.Drawing.Size(90, 18);
            
            // chkBetCheckFeedback
            this.chkBetCheckFeedback.Text = "下注核对反馈";
            this.chkBetCheckFeedback.Location = new System.Drawing.Point(10, 90);
            this.chkBetCheckFeedback.Size = new System.Drawing.Size(100, 18);
            
            // chkBetSummaryFeedback
            this.chkBetSummaryFeedback.Text = "下注汇总反馈";
            this.chkBetSummaryFeedback.Location = new System.Drawing.Point(115, 90);
            this.chkBetSummaryFeedback.Size = new System.Drawing.Size(100, 18);
            
            // chkProfitFeedback
            this.chkProfitFeedback.Text = "开奖盈利反馈";
            this.chkProfitFeedback.Location = new System.Drawing.Point(10, 112);
            this.chkProfitFeedback.Size = new System.Drawing.Size(100, 18);
            
            // chkBillSendFeedback
            this.chkBillSendFeedback.Text = "发送账单反馈";
            this.chkBillSendFeedback.Location = new System.Drawing.Point(115, 112);
            this.chkBillSendFeedback.Size = new System.Drawing.Size(100, 18);
            
            // btnSaveSettings
            this.btnSaveSettings.Text = "保存设置";
            this.btnSaveSettings.Location = new System.Drawing.Point(200, 130);
            this.btnSaveSettings.Size = new System.Drawing.Size(80, 25);
            this.btnSaveSettings.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            this.btnSaveSettings.Click += new System.EventHandler(this.btnSaveSettings_Click);
            
            // FeedbackSettingsControl
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.grpFeedback);
            this.Size = new System.Drawing.Size(298, 168);
            this.Dock = System.Windows.Forms.DockStyle.Top;
            
            this.grpFeedback.ResumeLayout(false);
            this.grpFeedback.PerformLayout();
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.GroupBox grpFeedback;
        private System.Windows.Forms.Label lblFeedbackWangWang;
        private System.Windows.Forms.TextBox txtFeedbackWangWang;
        private System.Windows.Forms.Label lblFeedbackGroup;
        private System.Windows.Forms.TextBox txtFeedbackGroup;
        private System.Windows.Forms.CheckBox chkFeedbackToWangWang;
        private System.Windows.Forms.CheckBox chkFeedbackToGroup;
        private System.Windows.Forms.CheckBox chkBetCheckFeedback;
        private System.Windows.Forms.CheckBox chkBetSummaryFeedback;
        private System.Windows.Forms.CheckBox chkProfitFeedback;
        private System.Windows.Forms.CheckBox chkBillSendFeedback;
        private System.Windows.Forms.Button btnSaveSettings;
    }
}

