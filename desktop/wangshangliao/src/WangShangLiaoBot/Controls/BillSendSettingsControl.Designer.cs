namespace WangShangLiaoBot.Controls
{
    partial class BillSendSettingsControl
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
            this.lblTitle = new System.Windows.Forms.Label();
            this.chkLotterySend = new System.Windows.Forms.CheckBox();
            this.chkWithR = new System.Windows.Forms.CheckBox();
            this.chkImageSend = new System.Windows.Forms.CheckBox();
            this.txtLotteryFormat = new System.Windows.Forms.TextBox();
            
            this.SuspendLayout();
            
            // lblTitle
            this.lblTitle.Text = "开奖发送↓";
            this.lblTitle.Location = new System.Drawing.Point(3, 5);
            this.lblTitle.Size = new System.Drawing.Size(60, 15);
            
            // chkLotterySend
            this.chkLotterySend.Text = "开奖发送↓";
            this.chkLotterySend.Location = new System.Drawing.Point(3, 3);
            this.chkLotterySend.Size = new System.Drawing.Size(80, 18);
            this.chkLotterySend.Checked = true;
            
            // chkWithR
            this.chkWithR.Text = "带R";
            this.chkWithR.Location = new System.Drawing.Point(88, 3);
            this.chkWithR.Size = new System.Drawing.Size(48, 18);
            
            // chkImageSend
            this.chkImageSend.Text = "图片发送";
            this.chkImageSend.Location = new System.Drawing.Point(140, 3);
            this.chkImageSend.Size = new System.Drawing.Size(75, 18);
            
            // txtLotteryFormat
            this.txtLotteryFormat.Text = "开: [一区]+[二区]+[三区]=[开奖号码][大小单双] 第[期数]期";
            this.txtLotteryFormat.Location = new System.Drawing.Point(3, 25);
            this.txtLotteryFormat.Size = new System.Drawing.Size(325, 21);
            
            // BillSendSettingsControl
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.chkLotterySend);
            this.Controls.Add(this.chkWithR);
            this.Controls.Add(this.chkImageSend);
            this.Controls.Add(this.txtLotteryFormat);
            this.Size = new System.Drawing.Size(332, 52);
            
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.CheckBox chkLotterySend;
        private System.Windows.Forms.CheckBox chkWithR;
        private System.Windows.Forms.CheckBox chkImageSend;
        private System.Windows.Forms.TextBox txtLotteryFormat;
    }
}
