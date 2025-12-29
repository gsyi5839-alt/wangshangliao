namespace WangShangLiaoBot.Controls.Odds
{
    partial class PositionBallSettingsControl
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
        
        #region Component Designer generated code
        
        private void InitializeComponent()
        {
            // Main switch checkbox
            this.chkPositionBallEnabled = new System.Windows.Forms.CheckBox();
            
            // Single bet odds row
            this.lblSingleOdds = new System.Windows.Forms.Label();
            this.txtSingleOdds = new System.Windows.Forms.TextBox();
            this.lblSingleRange = new System.Windows.Forms.Label();
            this.txtSingleRangeMin = new System.Windows.Forms.TextBox();
            this.lblSingleRangeSep = new System.Windows.Forms.Label();
            this.txtSingleRangeMax = new System.Windows.Forms.TextBox();
            
            // Combo bet odds row
            this.lblComboOdds = new System.Windows.Forms.Label();
            this.txtComboOdds = new System.Windows.Forms.TextBox();
            this.lblComboRange = new System.Windows.Forms.Label();
            this.txtComboRangeMin = new System.Windows.Forms.TextBox();
            this.lblComboRangeSep = new System.Windows.Forms.Label();
            this.txtComboRangeMax = new System.Windows.Forms.TextBox();
            
            // Special code odds row
            this.lblSpecialOdds = new System.Windows.Forms.Label();
            this.txtSpecialOdds = new System.Windows.Forms.TextBox();
            this.lblSpecialRange = new System.Windows.Forms.Label();
            this.txtSpecialRangeMin = new System.Windows.Forms.TextBox();
            this.lblSpecialRangeSep = new System.Windows.Forms.Label();
            this.txtSpecialRangeMax = new System.Windows.Forms.TextBox();
            
            // Save button
            this.btnSave = new System.Windows.Forms.Button();
            
            // Format description group
            this.grpFormatDescription = new System.Windows.Forms.GroupBox();
            this.lblFormatDescription = new System.Windows.Forms.Label();
            
            this.SuspendLayout();
            
            // ========================================
            // Main switch - Position ball enabled
            // ========================================
            this.chkPositionBallEnabled.Text = "定位球玩法  开启/关闭";
            this.chkPositionBallEnabled.Location = new System.Drawing.Point(10, 15);
            this.chkPositionBallEnabled.Size = new System.Drawing.Size(180, 20);
            this.chkPositionBallEnabled.Font = new System.Drawing.Font("微软雅黑", 9F);
            
            // ========================================
            // Row 1: Single bet odds (单注赔率)
            // ========================================
            int row1Y = 50;
            
            this.lblSingleOdds.Text = "单注赔率";
            this.lblSingleOdds.Location = new System.Drawing.Point(10, row1Y + 3);
            this.lblSingleOdds.AutoSize = true;
            this.lblSingleOdds.Font = new System.Drawing.Font("微软雅黑", 9F);
            
            this.txtSingleOdds.Location = new System.Drawing.Point(80, row1Y);
            this.txtSingleOdds.Size = new System.Drawing.Size(55, 23);
            this.txtSingleOdds.Font = new System.Drawing.Font("微软雅黑", 9F);
            
            this.lblSingleRange.Text = "下注范围";
            this.lblSingleRange.Location = new System.Drawing.Point(150, row1Y + 3);
            this.lblSingleRange.AutoSize = true;
            this.lblSingleRange.Font = new System.Drawing.Font("微软雅黑", 9F);
            
            this.txtSingleRangeMin.Location = new System.Drawing.Point(220, row1Y);
            this.txtSingleRangeMin.Size = new System.Drawing.Size(65, 23);
            this.txtSingleRangeMin.Font = new System.Drawing.Font("微软雅黑", 9F);
            
            this.lblSingleRangeSep.Text = "-";
            this.lblSingleRangeSep.Location = new System.Drawing.Point(290, row1Y + 3);
            this.lblSingleRangeSep.AutoSize = true;
            this.lblSingleRangeSep.Font = new System.Drawing.Font("微软雅黑", 9F);
            
            this.txtSingleRangeMax.Location = new System.Drawing.Point(305, row1Y);
            this.txtSingleRangeMax.Size = new System.Drawing.Size(65, 23);
            this.txtSingleRangeMax.Font = new System.Drawing.Font("微软雅黑", 9F);
            
            // ========================================
            // Row 2: Combo bet odds (组合赔率)
            // ========================================
            int row2Y = 85;
            
            this.lblComboOdds.Text = "组合赔率";
            this.lblComboOdds.Location = new System.Drawing.Point(10, row2Y + 3);
            this.lblComboOdds.AutoSize = true;
            this.lblComboOdds.Font = new System.Drawing.Font("微软雅黑", 9F);
            
            this.txtComboOdds.Location = new System.Drawing.Point(80, row2Y);
            this.txtComboOdds.Size = new System.Drawing.Size(55, 23);
            this.txtComboOdds.Font = new System.Drawing.Font("微软雅黑", 9F);
            
            this.lblComboRange.Text = "下注范围";
            this.lblComboRange.Location = new System.Drawing.Point(150, row2Y + 3);
            this.lblComboRange.AutoSize = true;
            this.lblComboRange.Font = new System.Drawing.Font("微软雅黑", 9F);
            
            this.txtComboRangeMin.Location = new System.Drawing.Point(220, row2Y);
            this.txtComboRangeMin.Size = new System.Drawing.Size(65, 23);
            this.txtComboRangeMin.Font = new System.Drawing.Font("微软雅黑", 9F);
            
            this.lblComboRangeSep.Text = "-";
            this.lblComboRangeSep.Location = new System.Drawing.Point(290, row2Y + 3);
            this.lblComboRangeSep.AutoSize = true;
            this.lblComboRangeSep.Font = new System.Drawing.Font("微软雅黑", 9F);
            
            this.txtComboRangeMax.Location = new System.Drawing.Point(305, row2Y);
            this.txtComboRangeMax.Size = new System.Drawing.Size(65, 23);
            this.txtComboRangeMax.Font = new System.Drawing.Font("微软雅黑", 9F);
            
            // ========================================
            // Row 3: Special code odds (特码赔率)
            // ========================================
            int row3Y = 120;
            
            this.lblSpecialOdds.Text = "特码赔率";
            this.lblSpecialOdds.Location = new System.Drawing.Point(10, row3Y + 3);
            this.lblSpecialOdds.AutoSize = true;
            this.lblSpecialOdds.Font = new System.Drawing.Font("微软雅黑", 9F);
            
            this.txtSpecialOdds.Location = new System.Drawing.Point(80, row3Y);
            this.txtSpecialOdds.Size = new System.Drawing.Size(55, 23);
            this.txtSpecialOdds.Font = new System.Drawing.Font("微软雅黑", 9F);
            
            this.lblSpecialRange.Text = "下注范围";
            this.lblSpecialRange.Location = new System.Drawing.Point(150, row3Y + 3);
            this.lblSpecialRange.AutoSize = true;
            this.lblSpecialRange.Font = new System.Drawing.Font("微软雅黑", 9F);
            
            this.txtSpecialRangeMin.Location = new System.Drawing.Point(220, row3Y);
            this.txtSpecialRangeMin.Size = new System.Drawing.Size(65, 23);
            this.txtSpecialRangeMin.Font = new System.Drawing.Font("微软雅黑", 9F);
            
            this.lblSpecialRangeSep.Text = "-";
            this.lblSpecialRangeSep.Location = new System.Drawing.Point(290, row3Y + 3);
            this.lblSpecialRangeSep.AutoSize = true;
            this.lblSpecialRangeSep.Font = new System.Drawing.Font("微软雅黑", 9F);
            
            this.txtSpecialRangeMax.Location = new System.Drawing.Point(305, row3Y);
            this.txtSpecialRangeMax.Size = new System.Drawing.Size(65, 23);
            this.txtSpecialRangeMax.Font = new System.Drawing.Font("微软雅黑", 9F);
            
            // ========================================
            // Save button
            // ========================================
            this.btnSave.Text = "保存设置";
            this.btnSave.Location = new System.Drawing.Point(145, 165);
            this.btnSave.Size = new System.Drawing.Size(90, 30);
            this.btnSave.Font = new System.Drawing.Font("微软雅黑", 9F);
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            
            // ========================================
            // Format description group box
            // ========================================
            this.grpFormatDescription.Text = "下注格式说明";
            this.grpFormatDescription.Location = new System.Drawing.Point(10, 210);
            this.grpFormatDescription.Size = new System.Drawing.Size(360, 150);
            this.grpFormatDescription.Font = new System.Drawing.Font("微软雅黑", 9F);
            
            this.lblFormatDescription.Text = 
                "1/大/100    1球大，下注100\r\n" +
                "2/4/100     2球4，下注100\r\n" +
                "3/456/100   3球4、5、6，各下注100\r\n" +
                "哈1/大      全下1/大，哈可以放后面 1/大哈";
            this.lblFormatDescription.Location = new System.Drawing.Point(15, 25);
            this.lblFormatDescription.Size = new System.Drawing.Size(330, 110);
            this.lblFormatDescription.Font = new System.Drawing.Font("微软雅黑", 9F);
            this.lblFormatDescription.ForeColor = System.Drawing.Color.FromArgb(64, 64, 64);
            
            this.grpFormatDescription.Controls.Add(this.lblFormatDescription);
            
            // ========================================
            // UserControl
            // ========================================
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Font = new System.Drawing.Font("微软雅黑", 9F);
            this.BackColor = System.Drawing.SystemColors.Control;
            
            this.Controls.Add(this.chkPositionBallEnabled);
            this.Controls.Add(this.lblSingleOdds);
            this.Controls.Add(this.txtSingleOdds);
            this.Controls.Add(this.lblSingleRange);
            this.Controls.Add(this.txtSingleRangeMin);
            this.Controls.Add(this.lblSingleRangeSep);
            this.Controls.Add(this.txtSingleRangeMax);
            this.Controls.Add(this.lblComboOdds);
            this.Controls.Add(this.txtComboOdds);
            this.Controls.Add(this.lblComboRange);
            this.Controls.Add(this.txtComboRangeMin);
            this.Controls.Add(this.lblComboRangeSep);
            this.Controls.Add(this.txtComboRangeMax);
            this.Controls.Add(this.lblSpecialOdds);
            this.Controls.Add(this.txtSpecialOdds);
            this.Controls.Add(this.lblSpecialRange);
            this.Controls.Add(this.txtSpecialRangeMin);
            this.Controls.Add(this.lblSpecialRangeSep);
            this.Controls.Add(this.txtSpecialRangeMax);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.grpFormatDescription);
            
            this.Name = "PositionBallSettingsControl";
            this.Size = new System.Drawing.Size(520, 380);
            
            this.ResumeLayout(false);
            this.PerformLayout();
        }
        
        #endregion
        
        // Main switch
        private System.Windows.Forms.CheckBox chkPositionBallEnabled;
        
        // Single bet odds row
        private System.Windows.Forms.Label lblSingleOdds;
        private System.Windows.Forms.TextBox txtSingleOdds;
        private System.Windows.Forms.Label lblSingleRange;
        private System.Windows.Forms.TextBox txtSingleRangeMin;
        private System.Windows.Forms.Label lblSingleRangeSep;
        private System.Windows.Forms.TextBox txtSingleRangeMax;
        
        // Combo bet odds row
        private System.Windows.Forms.Label lblComboOdds;
        private System.Windows.Forms.TextBox txtComboOdds;
        private System.Windows.Forms.Label lblComboRange;
        private System.Windows.Forms.TextBox txtComboRangeMin;
        private System.Windows.Forms.Label lblComboRangeSep;
        private System.Windows.Forms.TextBox txtComboRangeMax;
        
        // Special code odds row
        private System.Windows.Forms.Label lblSpecialOdds;
        private System.Windows.Forms.TextBox txtSpecialOdds;
        private System.Windows.Forms.Label lblSpecialRange;
        private System.Windows.Forms.TextBox txtSpecialRangeMin;
        private System.Windows.Forms.Label lblSpecialRangeSep;
        private System.Windows.Forms.TextBox txtSpecialRangeMax;
        
        // Save button
        private System.Windows.Forms.Button btnSave;
        
        // Format description
        private System.Windows.Forms.GroupBox grpFormatDescription;
        private System.Windows.Forms.Label lblFormatDescription;
    }
}

