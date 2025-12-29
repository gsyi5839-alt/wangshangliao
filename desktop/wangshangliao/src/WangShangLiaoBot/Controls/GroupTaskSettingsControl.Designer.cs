namespace WangShangLiaoBot.Controls
{
    partial class GroupTaskSettingsControl
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
            this.chkGroupTaskSend = new System.Windows.Forms.CheckBox();
            this.chkHideLostPlayers = new System.Windows.Forms.CheckBox();
            this.chkKeepZeroScore = new System.Windows.Forms.CheckBox();
            this.chkKeepRecent10 = new System.Windows.Forms.CheckBox();
            this.chkAutoApprove = new System.Windows.Forms.CheckBox();
            this.lblBillMinDigits = new System.Windows.Forms.Label();
            this.numBillMinDigits = new System.Windows.Forms.NumericUpDown();
            this.lblDigitsPad = new System.Windows.Forms.Label();
            this.lblHideThreshold = new System.Windows.Forms.Label();
            this.numHideThreshold = new System.Windows.Forms.NumericUpDown();
            this.lblHideUnit = new System.Windows.Forms.Label();
            
            ((System.ComponentModel.ISupportInitialize)(this.numBillMinDigits)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numHideThreshold)).BeginInit();
            this.SuspendLayout();
            
            // 第一行
            // chkGroupTaskSend
            this.chkGroupTaskSend.Text = "群作业账单发送";
            this.chkGroupTaskSend.Location = new System.Drawing.Point(3, 3);
            this.chkGroupTaskSend.Size = new System.Drawing.Size(105, 18);
            
            // chkHideLostPlayers
            this.chkHideLostPlayers.Text = "[账单]不显示输光玩家";
            this.chkHideLostPlayers.Location = new System.Drawing.Point(113, 3);
            this.chkHideLostPlayers.Size = new System.Drawing.Size(140, 18);
            
            // chkKeepZeroScore
            this.chkKeepZeroScore.Text = "零分不删除账单";
            this.chkKeepZeroScore.Location = new System.Drawing.Point(258, 3);
            this.chkKeepZeroScore.Size = new System.Drawing.Size(108, 18);
            
            // 第二行
            // chkKeepRecent10
            this.chkKeepRecent10.Text = "只保留近10期群作业";
            this.chkKeepRecent10.Location = new System.Drawing.Point(3, 25);
            this.chkKeepRecent10.Size = new System.Drawing.Size(135, 18);
            this.chkKeepRecent10.Checked = true;
            
            // chkAutoApprove
            this.chkAutoApprove.Text = "账单玩家进群自动同意";
            this.chkAutoApprove.Location = new System.Drawing.Point(143, 25);
            this.chkAutoApprove.Size = new System.Drawing.Size(145, 18);
            
            // lblBillMinDigits
            this.lblBillMinDigits.Text = "账单不足";
            this.lblBillMinDigits.Location = new System.Drawing.Point(3, 50);
            this.lblBillMinDigits.Size = new System.Drawing.Size(50, 15);
            
            // numBillMinDigits
            this.numBillMinDigits.Location = new System.Drawing.Point(53, 47);
            this.numBillMinDigits.Size = new System.Drawing.Size(35, 21);
            this.numBillMinDigits.Minimum = 1;
            this.numBillMinDigits.Maximum = 10;
            this.numBillMinDigits.Value = 4;
            
            // lblDigitsPad
            this.lblDigitsPad.Text = "位用0补齐";
            this.lblDigitsPad.Location = new System.Drawing.Point(90, 50);
            this.lblDigitsPad.Size = new System.Drawing.Size(60, 15);
            
            // 第三行
            // lblHideThreshold
            this.lblHideThreshold.Text = "[账单]小于";
            this.lblHideThreshold.Location = new System.Drawing.Point(3, 75);
            this.lblHideThreshold.Size = new System.Drawing.Size(60, 15);
            
            // numHideThreshold
            this.numHideThreshold.Location = new System.Drawing.Point(63, 72);
            this.numHideThreshold.Size = new System.Drawing.Size(35, 21);
            this.numHideThreshold.Value = 0;
            
            // lblHideUnit
            this.lblHideUnit.Text = "不显示";
            this.lblHideUnit.Location = new System.Drawing.Point(100, 75);
            this.lblHideUnit.Size = new System.Drawing.Size(42, 15);
            
            // GroupTaskSettingsControl
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.chkGroupTaskSend);
            this.Controls.Add(this.chkHideLostPlayers);
            this.Controls.Add(this.chkKeepZeroScore);
            this.Controls.Add(this.chkKeepRecent10);
            this.Controls.Add(this.chkAutoApprove);
            this.Controls.Add(this.lblBillMinDigits);
            this.Controls.Add(this.numBillMinDigits);
            this.Controls.Add(this.lblDigitsPad);
            this.Controls.Add(this.lblHideThreshold);
            this.Controls.Add(this.numHideThreshold);
            this.Controls.Add(this.lblHideUnit);
            this.Size = new System.Drawing.Size(332, 98);
            
            ((System.ComponentModel.ISupportInitialize)(this.numBillMinDigits)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numHideThreshold)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.CheckBox chkGroupTaskSend;
        private System.Windows.Forms.CheckBox chkHideLostPlayers;
        private System.Windows.Forms.CheckBox chkKeepZeroScore;
        private System.Windows.Forms.CheckBox chkKeepRecent10;
        private System.Windows.Forms.CheckBox chkAutoApprove;
        private System.Windows.Forms.Label lblBillMinDigits;
        private System.Windows.Forms.NumericUpDown numBillMinDigits;
        private System.Windows.Forms.Label lblDigitsPad;
        private System.Windows.Forms.Label lblHideThreshold;
        private System.Windows.Forms.NumericUpDown numHideThreshold;
        private System.Windows.Forms.Label lblHideUnit;
    }
}
