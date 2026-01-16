namespace WangShangLiaoBot.Controls
{
    partial class BillFormatSettingsControl
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
            this.grpBillFormat = new System.Windows.Forms.GroupBox();
            this.lblBillContent = new System.Windows.Forms.Label();
            this.lblBillFormat = new System.Windows.Forms.Label();
            this.lblColumns = new System.Windows.Forms.Label();
            this.numBillColumns = new System.Windows.Forms.NumericUpDown();
            this.chkBillImageSend = new System.Windows.Forms.CheckBox();
            this.chkBillPrivateReply = new System.Windows.Forms.CheckBox();
            this.txtBillFormat1 = new System.Windows.Forms.TextBox();
            this.txtBillFormat2 = new System.Windows.Forms.TextBox();
            this.txtBillTemplate = new System.Windows.Forms.TextBox();
            this.txtHistory1 = new System.Windows.Forms.TextBox();
            this.txtHistory2 = new System.Windows.Forms.TextBox();
            this.txtHistory3 = new System.Windows.Forms.TextBox();
            this.txtHistory4 = new System.Windows.Forms.TextBox();
            
            this.grpBillFormat.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numBillColumns)).BeginInit();
            this.SuspendLayout();
            
            // grpBillFormat - 蓝色边框分组
            this.grpBillFormat.Location = new System.Drawing.Point(0, 0);
            this.grpBillFormat.Size = new System.Drawing.Size(450, 215);
            this.grpBillFormat.ForeColor = System.Drawing.Color.Blue;
            this.grpBillFormat.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.grpBillFormat.Controls.Add(this.lblBillContent);
            this.grpBillFormat.Controls.Add(this.lblBillFormat);
            this.grpBillFormat.Controls.Add(this.lblColumns);
            this.grpBillFormat.Controls.Add(this.numBillColumns);
            this.grpBillFormat.Controls.Add(this.chkBillImageSend);
            this.grpBillFormat.Controls.Add(this.chkBillPrivateReply);
            this.grpBillFormat.Controls.Add(this.txtBillFormat1);
            this.grpBillFormat.Controls.Add(this.txtBillFormat2);
            this.grpBillFormat.Controls.Add(this.txtBillTemplate);
            this.grpBillFormat.Controls.Add(this.txtHistory1);
            this.grpBillFormat.Controls.Add(this.txtHistory2);
            this.grpBillFormat.Controls.Add(this.txtHistory3);
            this.grpBillFormat.Controls.Add(this.txtHistory4);
            
            // lblBillContent
            this.lblBillContent.Text = "账单内容↓";
            this.lblBillContent.Location = new System.Drawing.Point(8, 0);
            this.lblBillContent.Size = new System.Drawing.Size(60, 15);
            this.lblBillContent.ForeColor = System.Drawing.Color.Blue;
            this.lblBillContent.Font = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Underline);
            
            // lblBillFormat
            this.lblBillFormat.Text = "账单格式↓";
            this.lblBillFormat.Location = new System.Drawing.Point(73, 0);
            this.lblBillFormat.Size = new System.Drawing.Size(60, 15);
            this.lblBillFormat.ForeColor = System.Drawing.Color.Blue;
            this.lblBillFormat.Font = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Underline);
            
            // lblColumns
            this.lblColumns.Text = "横";
            this.lblColumns.Location = new System.Drawing.Point(138, 0);
            this.lblColumns.Size = new System.Drawing.Size(15, 15);
            this.lblColumns.ForeColor = System.Drawing.Color.Black;
            
            // numBillColumns
            this.numBillColumns.Location = new System.Drawing.Point(153, -2);
            this.numBillColumns.Size = new System.Drawing.Size(35, 21);
            this.numBillColumns.Minimum = 1;
            this.numBillColumns.Maximum = 10;
            this.numBillColumns.Value = 4;
            
            // chkBillImageSend
            this.chkBillImageSend.Text = "图片发送";
            this.chkBillImageSend.Location = new System.Drawing.Point(193, -1);
            this.chkBillImageSend.Size = new System.Drawing.Size(70, 18);
            this.chkBillImageSend.ForeColor = System.Drawing.Color.Black;
            
            // chkBillPrivateReply
            this.chkBillPrivateReply.Text = "开奖账单私聊回复";
            this.chkBillPrivateReply.Location = new System.Drawing.Point(263, -1);
            this.chkBillPrivateReply.Size = new System.Drawing.Size(65, 18);
            this.chkBillPrivateReply.ForeColor = System.Drawing.Color.Black;
            
            // txtBillFormat1
            this.txtBillFormat1.Text = "開: [一区] + [二区] + [三区] = [开奖号码][大小单双][杀顺对子][龙虎狗]";
            this.txtBillFormat1.Location = new System.Drawing.Point(8, 18);
            this.txtBillFormat1.Size = new System.Drawing.Size(435, 21);
            this.txtBillFormat1.ForeColor = System.Drawing.Color.Black;
            this.txtBillFormat1.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            
            // txtBillFormat2
            this.txtBillFormat2.Text = "人数: [客户人数]  继分: [总分数]";
            this.txtBillFormat2.Location = new System.Drawing.Point(8, 42);
            this.txtBillFormat2.Size = new System.Drawing.Size(435, 21);
            this.txtBillFormat2.ForeColor = System.Drawing.Color.Black;
            this.txtBillFormat2.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            
            // txtBillTemplate
            this.txtBillTemplate.Text = "[账单]";
            this.txtBillTemplate.Location = new System.Drawing.Point(8, 66);
            this.txtBillTemplate.Size = new System.Drawing.Size(435, 21);
            this.txtBillTemplate.ForeColor = System.Drawing.Color.Black;
            this.txtBillTemplate.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            
            // txtHistory1
            this.txtHistory1.Text = "1s: [开奖历史]";
            this.txtHistory1.Location = new System.Drawing.Point(8, 90);
            this.txtHistory1.Size = new System.Drawing.Size(435, 21);
            this.txtHistory1.ForeColor = System.Drawing.Color.Black;
            this.txtHistory1.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            
            // txtHistory2
            this.txtHistory2.Text = "龙虎狗1s: [龙虎历史]";
            this.txtHistory2.Location = new System.Drawing.Point(8, 114);
            this.txtHistory2.Size = new System.Drawing.Size(435, 21);
            this.txtHistory2.ForeColor = System.Drawing.Color.Black;
            this.txtHistory2.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            
            // txtHistory3
            this.txtHistory3.Text = "提1s: [提数历史]";
            this.txtHistory3.Location = new System.Drawing.Point(8, 138);
            this.txtHistory3.Size = new System.Drawing.Size(435, 21);
            this.txtHistory3.ForeColor = System.Drawing.Color.Black;
            this.txtHistory3.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            
            // txtHistory4
            this.txtHistory4.Text = "杀顺对历史: [杀顺对历史]";
            this.txtHistory4.Location = new System.Drawing.Point(8, 162);
            this.txtHistory4.Size = new System.Drawing.Size(435, 21);
            this.txtHistory4.ForeColor = System.Drawing.Color.Black;
            this.txtHistory4.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            
            // BillFormatSettingsControl
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.grpBillFormat);
            this.Size = new System.Drawing.Size(455, 190);
            
            this.grpBillFormat.ResumeLayout(false);
            this.grpBillFormat.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numBillColumns)).EndInit();
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.GroupBox grpBillFormat;
        private System.Windows.Forms.Label lblBillContent;
        private System.Windows.Forms.Label lblBillFormat;
        private System.Windows.Forms.Label lblColumns;
        private System.Windows.Forms.NumericUpDown numBillColumns;
        private System.Windows.Forms.CheckBox chkBillImageSend;
        private System.Windows.Forms.CheckBox chkBillPrivateReply;
        private System.Windows.Forms.TextBox txtBillFormat1;
        private System.Windows.Forms.TextBox txtBillFormat2;
        private System.Windows.Forms.TextBox txtBillTemplate;
        private System.Windows.Forms.TextBox txtHistory1;
        private System.Windows.Forms.TextBox txtHistory2;
        private System.Windows.Forms.TextBox txtHistory3;
        private System.Windows.Forms.TextBox txtHistory4;
    }
}
