namespace WangShangLiaoBot.Controls.Odds
{
    partial class OtherPlaySettingsControl
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
            // ===== 二七玩法区域 =====
            this.grpTwoSeven = new System.Windows.Forms.GroupBox();
            this.chkTwoSevenEnabled = new System.Windows.Forms.CheckBox();
            this.lblSingleExceed = new System.Windows.Forms.Label();
            this.txtSingleExceed = new System.Windows.Forms.TextBox();
            this.lblSingleOdds = new System.Windows.Forms.Label();
            this.txtSingleOdds = new System.Windows.Forms.TextBox();
            this.lblComboExceed = new System.Windows.Forms.Label();
            this.txtComboExceed = new System.Windows.Forms.TextBox();
            this.lblComboOdds = new System.Windows.Forms.Label();
            this.txtComboOdds = new System.Windows.Forms.TextBox();
            
            // ===== 反向开奖玩法区域 =====
            this.grpReverseLottery = new System.Windows.Forms.GroupBox();
            this.chkReverseLotteryEnabled = new System.Windows.Forms.CheckBox();
            this.lblBetMaxRatio = new System.Windows.Forms.Label();
            this.txtBetMaxRatio = new System.Windows.Forms.TextBox();
            this.lblBetMaxPercent = new System.Windows.Forms.Label();
            this.lblProfitDeduct = new System.Windows.Forms.Label();
            this.txtProfitDeduct = new System.Windows.Forms.TextBox();
            this.lblProfitDeductPercent = new System.Windows.Forms.Label();
            this.lblSupportTypes = new System.Windows.Forms.Label();
            
            // ===== 长龙玩法区域 =====
            this.grpDragonStreak = new System.Windows.Forms.GroupBox();
            this.chkDragonStreakEnabled = new System.Windows.Forms.CheckBox();
            this.lblStreakHint = new System.Windows.Forms.Label();
            this.lblStreak1Times = new System.Windows.Forms.Label();
            this.txtStreak1Times = new System.Windows.Forms.TextBox();
            this.lblStreak1Reduce = new System.Windows.Forms.Label();
            this.txtStreak1Reduce = new System.Windows.Forms.TextBox();
            this.lblStreak2Times = new System.Windows.Forms.Label();
            this.txtStreak2Times = new System.Windows.Forms.TextBox();
            this.lblStreak2Reduce = new System.Windows.Forms.Label();
            this.txtStreak2Reduce = new System.Windows.Forms.TextBox();
            
            // Save button
            this.btnSave = new System.Windows.Forms.Button();
            
            this.SuspendLayout();
            
            // ==========================================
            // 二七玩法 GroupBox
            // ==========================================
            this.grpTwoSeven.Location = new System.Drawing.Point(10, 10);
            this.grpTwoSeven.Size = new System.Drawing.Size(280, 130);
            this.grpTwoSeven.Text = "二七玩法";
            this.grpTwoSeven.Name = "grpTwoSeven";
            
            // Enable checkbox
            this.chkTwoSevenEnabled.Location = new System.Drawing.Point(15, 25);
            this.chkTwoSevenEnabled.Size = new System.Drawing.Size(100, 20);
            this.chkTwoSevenEnabled.Text = "开启/关闭";
            this.chkTwoSevenEnabled.Name = "chkTwoSevenEnabled";
            this.grpTwoSeven.Controls.Add(this.chkTwoSevenEnabled);
            
            // Single bet exceed threshold row
            this.lblSingleExceed.Location = new System.Drawing.Point(15, 55);
            this.lblSingleExceed.Size = new System.Drawing.Size(65, 20);
            this.lblSingleExceed.Text = "单注总额超";
            this.lblSingleExceed.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblSingleExceed.Name = "lblSingleExceed";
            this.grpTwoSeven.Controls.Add(this.lblSingleExceed);
            
            this.txtSingleExceed.Location = new System.Drawing.Point(82, 55);
            this.txtSingleExceed.Size = new System.Drawing.Size(60, 22);
            this.txtSingleExceed.Name = "txtSingleExceed";
            this.grpTwoSeven.Controls.Add(this.txtSingleExceed);
            
            this.lblSingleOdds.Location = new System.Drawing.Point(150, 55);
            this.lblSingleOdds.Size = new System.Drawing.Size(30, 20);
            this.lblSingleOdds.Text = "赔率";
            this.lblSingleOdds.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblSingleOdds.Name = "lblSingleOdds";
            this.grpTwoSeven.Controls.Add(this.lblSingleOdds);
            
            this.txtSingleOdds.Location = new System.Drawing.Point(185, 55);
            this.txtSingleOdds.Size = new System.Drawing.Size(60, 22);
            this.txtSingleOdds.Name = "txtSingleOdds";
            this.grpTwoSeven.Controls.Add(this.txtSingleOdds);
            
            // Combo bet exceed threshold row
            this.lblComboExceed.Location = new System.Drawing.Point(15, 85);
            this.lblComboExceed.Size = new System.Drawing.Size(65, 20);
            this.lblComboExceed.Text = "组合总额超";
            this.lblComboExceed.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblComboExceed.Name = "lblComboExceed";
            this.grpTwoSeven.Controls.Add(this.lblComboExceed);
            
            this.txtComboExceed.Location = new System.Drawing.Point(82, 85);
            this.txtComboExceed.Size = new System.Drawing.Size(60, 22);
            this.txtComboExceed.Name = "txtComboExceed";
            this.grpTwoSeven.Controls.Add(this.txtComboExceed);
            
            this.lblComboOdds.Location = new System.Drawing.Point(150, 85);
            this.lblComboOdds.Size = new System.Drawing.Size(30, 20);
            this.lblComboOdds.Text = "赔率";
            this.lblComboOdds.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblComboOdds.Name = "lblComboOdds";
            this.grpTwoSeven.Controls.Add(this.lblComboOdds);
            
            this.txtComboOdds.Location = new System.Drawing.Point(185, 85);
            this.txtComboOdds.Size = new System.Drawing.Size(60, 22);
            this.txtComboOdds.Name = "txtComboOdds";
            this.grpTwoSeven.Controls.Add(this.txtComboOdds);
            
            // ==========================================
            // 反向开奖玩法 GroupBox
            // ==========================================
            this.grpReverseLottery.Location = new System.Drawing.Point(300, 10);
            this.grpReverseLottery.Size = new System.Drawing.Size(280, 130);
            this.grpReverseLottery.Text = "反向开奖玩法";
            this.grpReverseLottery.Name = "grpReverseLottery";
            
            // Enable checkbox
            this.chkReverseLotteryEnabled.Location = new System.Drawing.Point(15, 25);
            this.chkReverseLotteryEnabled.Size = new System.Drawing.Size(100, 20);
            this.chkReverseLotteryEnabled.Text = "开启/关闭";
            this.chkReverseLotteryEnabled.Name = "chkReverseLotteryEnabled";
            this.grpReverseLottery.Controls.Add(this.chkReverseLotteryEnabled);
            
            // Bet max ratio row
            this.lblBetMaxRatio.Location = new System.Drawing.Point(15, 55);
            this.lblBetMaxRatio.Size = new System.Drawing.Size(130, 20);
            this.lblBetMaxRatio.Text = "下注总额最多为总分的";
            this.lblBetMaxRatio.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblBetMaxRatio.Name = "lblBetMaxRatio";
            this.grpReverseLottery.Controls.Add(this.lblBetMaxRatio);
            
            this.txtBetMaxRatio.Location = new System.Drawing.Point(150, 55);
            this.txtBetMaxRatio.Size = new System.Drawing.Size(50, 22);
            this.txtBetMaxRatio.Name = "txtBetMaxRatio";
            this.grpReverseLottery.Controls.Add(this.txtBetMaxRatio);
            
            this.lblBetMaxPercent.Location = new System.Drawing.Point(205, 55);
            this.lblBetMaxPercent.Size = new System.Drawing.Size(20, 20);
            this.lblBetMaxPercent.Text = "%";
            this.lblBetMaxPercent.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblBetMaxPercent.Name = "lblBetMaxPercent";
            this.grpReverseLottery.Controls.Add(this.lblBetMaxPercent);
            
            // Profit deduct row
            this.lblProfitDeduct.Location = new System.Drawing.Point(15, 85);
            this.lblProfitDeduct.Size = new System.Drawing.Size(130, 20);
            this.lblProfitDeduct.Text = "每次结算扣除盈利的";
            this.lblProfitDeduct.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblProfitDeduct.Name = "lblProfitDeduct";
            this.grpReverseLottery.Controls.Add(this.lblProfitDeduct);
            
            this.txtProfitDeduct.Location = new System.Drawing.Point(150, 85);
            this.txtProfitDeduct.Size = new System.Drawing.Size(50, 22);
            this.txtProfitDeduct.Name = "txtProfitDeduct";
            this.grpReverseLottery.Controls.Add(this.txtProfitDeduct);
            
            this.lblProfitDeductPercent.Location = new System.Drawing.Point(205, 85);
            this.lblProfitDeductPercent.Size = new System.Drawing.Size(20, 20);
            this.lblProfitDeductPercent.Text = "%";
            this.lblProfitDeductPercent.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblProfitDeductPercent.Name = "lblProfitDeductPercent";
            this.grpReverseLottery.Controls.Add(this.lblProfitDeductPercent);
            
            // Support types label
            this.lblSupportTypes.Location = new System.Drawing.Point(15, 110);
            this.lblSupportTypes.Size = new System.Drawing.Size(250, 16);
            this.lblSupportTypes.Text = "支持单注、组合、龙虎、对顺豹";
            this.lblSupportTypes.ForeColor = System.Drawing.Color.Gray;
            this.lblSupportTypes.Name = "lblSupportTypes";
            this.grpReverseLottery.Controls.Add(this.lblSupportTypes);
            
            // ==========================================
            // 长龙玩法 GroupBox
            // ==========================================
            this.grpDragonStreak.Location = new System.Drawing.Point(10, 150);
            this.grpDragonStreak.Size = new System.Drawing.Size(280, 150);
            this.grpDragonStreak.Text = "长龙玩法";
            this.grpDragonStreak.Name = "grpDragonStreak";
            
            // Enable checkbox
            this.chkDragonStreakEnabled.Location = new System.Drawing.Point(15, 25);
            this.chkDragonStreakEnabled.Size = new System.Drawing.Size(100, 20);
            this.chkDragonStreakEnabled.Text = "开启/关闭";
            this.chkDragonStreakEnabled.Name = "chkDragonStreakEnabled";
            this.grpDragonStreak.Controls.Add(this.chkDragonStreakEnabled);
            
            // Hint label
            this.lblStreakHint.Location = new System.Drawing.Point(15, 50);
            this.lblStreakHint.Size = new System.Drawing.Size(120, 20);
            this.lblStreakHint.Text = "大小单双连续出现";
            this.lblStreakHint.Name = "lblStreakHint";
            this.grpDragonStreak.Controls.Add(this.lblStreakHint);
            
            // Streak row 1
            this.txtStreak1Times.Location = new System.Drawing.Point(15, 75);
            this.txtStreak1Times.Size = new System.Drawing.Size(40, 22);
            this.txtStreak1Times.Name = "txtStreak1Times";
            this.grpDragonStreak.Controls.Add(this.txtStreak1Times);
            
            this.lblStreak1Reduce.Location = new System.Drawing.Point(60, 75);
            this.lblStreak1Reduce.Size = new System.Drawing.Size(70, 20);
            this.lblStreak1Reduce.Text = "次以上赔率减";
            this.lblStreak1Reduce.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblStreak1Reduce.Name = "lblStreak1Reduce";
            this.grpDragonStreak.Controls.Add(this.lblStreak1Reduce);
            
            this.txtStreak1Reduce.Location = new System.Drawing.Point(135, 75);
            this.txtStreak1Reduce.Size = new System.Drawing.Size(50, 22);
            this.txtStreak1Reduce.Name = "txtStreak1Reduce";
            this.grpDragonStreak.Controls.Add(this.txtStreak1Reduce);
            
            // Streak row 2
            this.txtStreak2Times.Location = new System.Drawing.Point(15, 105);
            this.txtStreak2Times.Size = new System.Drawing.Size(40, 22);
            this.txtStreak2Times.Name = "txtStreak2Times";
            this.grpDragonStreak.Controls.Add(this.txtStreak2Times);
            
            this.lblStreak2Reduce.Location = new System.Drawing.Point(60, 105);
            this.lblStreak2Reduce.Size = new System.Drawing.Size(70, 20);
            this.lblStreak2Reduce.Text = "次以上赔率减";
            this.lblStreak2Reduce.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblStreak2Reduce.Name = "lblStreak2Reduce";
            this.grpDragonStreak.Controls.Add(this.lblStreak2Reduce);
            
            this.txtStreak2Reduce.Location = new System.Drawing.Point(135, 105);
            this.txtStreak2Reduce.Size = new System.Drawing.Size(50, 22);
            this.txtStreak2Reduce.Name = "txtStreak2Reduce";
            this.grpDragonStreak.Controls.Add(this.txtStreak2Reduce);
            
            // ==========================================
            // Save Button
            // ==========================================
            this.btnSave.Location = new System.Drawing.Point(350, 200);
            this.btnSave.Size = new System.Drawing.Size(100, 30);
            this.btnSave.Text = "保存设置";
            this.btnSave.Name = "btnSave";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            
            // ==========================================
            // Main Control Setup
            // ==========================================
            this.Controls.Add(this.grpTwoSeven);
            this.Controls.Add(this.grpReverseLottery);
            this.Controls.Add(this.grpDragonStreak);
            this.Controls.Add(this.btnSave);
            
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Name = "OtherPlaySettingsControl";
            this.Size = new System.Drawing.Size(600, 320);
            
            this.ResumeLayout(false);
        }
        
        #endregion
        
        // ===== 二七玩法控件 =====
        private System.Windows.Forms.GroupBox grpTwoSeven;
        private System.Windows.Forms.CheckBox chkTwoSevenEnabled;
        private System.Windows.Forms.Label lblSingleExceed;
        private System.Windows.Forms.TextBox txtSingleExceed;
        private System.Windows.Forms.Label lblSingleOdds;
        private System.Windows.Forms.TextBox txtSingleOdds;
        private System.Windows.Forms.Label lblComboExceed;
        private System.Windows.Forms.TextBox txtComboExceed;
        private System.Windows.Forms.Label lblComboOdds;
        private System.Windows.Forms.TextBox txtComboOdds;
        
        // ===== 反向开奖玩法控件 =====
        private System.Windows.Forms.GroupBox grpReverseLottery;
        private System.Windows.Forms.CheckBox chkReverseLotteryEnabled;
        private System.Windows.Forms.Label lblBetMaxRatio;
        private System.Windows.Forms.TextBox txtBetMaxRatio;
        private System.Windows.Forms.Label lblBetMaxPercent;
        private System.Windows.Forms.Label lblProfitDeduct;
        private System.Windows.Forms.TextBox txtProfitDeduct;
        private System.Windows.Forms.Label lblProfitDeductPercent;
        private System.Windows.Forms.Label lblSupportTypes;
        
        // ===== 长龙玩法控件 =====
        private System.Windows.Forms.GroupBox grpDragonStreak;
        private System.Windows.Forms.CheckBox chkDragonStreakEnabled;
        private System.Windows.Forms.Label lblStreakHint;
        private System.Windows.Forms.Label lblStreak1Times;
        private System.Windows.Forms.TextBox txtStreak1Times;
        private System.Windows.Forms.Label lblStreak1Reduce;
        private System.Windows.Forms.TextBox txtStreak1Reduce;
        private System.Windows.Forms.Label lblStreak2Times;
        private System.Windows.Forms.TextBox txtStreak2Times;
        private System.Windows.Forms.Label lblStreak2Reduce;
        private System.Windows.Forms.TextBox txtStreak2Reduce;
        
        // Save button
        private System.Windows.Forms.Button btnSave;
    }
}

