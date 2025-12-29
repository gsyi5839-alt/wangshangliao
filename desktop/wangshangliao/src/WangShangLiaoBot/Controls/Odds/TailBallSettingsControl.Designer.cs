namespace WangShangLiaoBot.Controls.Odds
{
    partial class TailBallSettingsControl
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
            // 顶部开关
            this.chkTailBallEnabled = new System.Windows.Forms.CheckBox();
            this.chkTailBallNotCountClassic = new System.Windows.Forms.CheckBox();
            
            // 无13 14赔率组
            this.grpNo1314Odds = new System.Windows.Forms.GroupBox();
            this.lblOdds1314BigSmall = new System.Windows.Forms.Label();
            this.txtOdds1314BigSmall = new System.Windows.Forms.TextBox();
            this.lblOdds1314Combo = new System.Windows.Forms.Label();
            this.txtOdds1314Combo = new System.Windows.Forms.TextBox();
            this.lblOdds1314Special = new System.Windows.Forms.Label();
            this.txtOdds1314Special = new System.Windows.Forms.TextBox();
            
            // 尾球开0 9赔率组
            this.grpOdds09 = new System.Windows.Forms.GroupBox();
            this.lblOdds09BigSmall = new System.Windows.Forms.Label();
            this.txtOdds09BigSmall = new System.Windows.Forms.TextBox();
            this.lblOdds09Combo = new System.Windows.Forms.Label();
            this.txtOdds09Combo = new System.Windows.Forms.TextBox();
            
            // 有13 14赔率组
            this.grpWith1314Odds = new System.Windows.Forms.GroupBox();
            this.rbSingleBet = new System.Windows.Forms.RadioButton();
            this.rbTotalBet = new System.Windows.Forms.RadioButton();
            this.lblTailBallOver1 = new System.Windows.Forms.Label();
            this.txtTailBallOver1 = new System.Windows.Forms.TextBox();
            this.lblOddsWith1314BigSmall = new System.Windows.Forms.Label();
            this.txtOddsWith1314BigSmall = new System.Windows.Forms.TextBox();
            this.lblTailBallOver2 = new System.Windows.Forms.Label();
            this.txtTailBallOver2 = new System.Windows.Forms.TextBox();
            this.lblOddsWith1314Combo = new System.Windows.Forms.Label();
            this.txtOddsWith1314Combo = new System.Windows.Forms.TextBox();
            
            // 其他复选框
            this.chkOtherGameCountTotal = new System.Windows.Forms.CheckBox();
            this.chkForbid09 = new System.Windows.Forms.CheckBox();
            this.chkFrontBallEnabled = new System.Windows.Forms.CheckBox();
            this.chkMiddleBallEnabled = new System.Windows.Forms.CheckBox();
            
            // 说明文本
            this.lblExplanationTitle = new System.Windows.Forms.Label();
            this.lblExplanation = new System.Windows.Forms.Label();
            
            // 按钮
            this.btnSave = new System.Windows.Forms.Button();
            
            this.grpNo1314Odds.SuspendLayout();
            this.grpOdds09.SuspendLayout();
            this.grpWith1314Odds.SuspendLayout();
            this.SuspendLayout();
            
            // ========================================
            // 顶部开关
            // ========================================
            this.chkTailBallEnabled.Text = "尾球玩法  开启/关闭";
            this.chkTailBallEnabled.Location = new System.Drawing.Point(10, 10);
            this.chkTailBallEnabled.AutoSize = true;
            
            this.chkTailBallNotCountClassic.Text = "尾球玩法不算进经典玩法总注";
            this.chkTailBallNotCountClassic.Location = new System.Drawing.Point(180, 10);
            this.chkTailBallNotCountClassic.AutoSize = true;
            
            // ========================================
            // 无13 14赔率组
            // ========================================
            this.grpNo1314Odds.Text = "无13 14赔率";
            this.grpNo1314Odds.Location = new System.Drawing.Point(10, 38);
            this.grpNo1314Odds.Size = new System.Drawing.Size(160, 105);
            this.grpNo1314Odds.Controls.Add(this.lblOdds1314BigSmall);
            this.grpNo1314Odds.Controls.Add(this.txtOdds1314BigSmall);
            this.grpNo1314Odds.Controls.Add(this.lblOdds1314Combo);
            this.grpNo1314Odds.Controls.Add(this.txtOdds1314Combo);
            this.grpNo1314Odds.Controls.Add(this.lblOdds1314Special);
            this.grpNo1314Odds.Controls.Add(this.txtOdds1314Special);
            
            this.lblOdds1314BigSmall.Text = "尾大小单双赔率";
            this.lblOdds1314BigSmall.Location = new System.Drawing.Point(8, 22);
            this.lblOdds1314BigSmall.AutoSize = true;
            
            this.txtOdds1314BigSmall.Location = new System.Drawing.Point(100, 19);
            this.txtOdds1314BigSmall.Size = new System.Drawing.Size(50, 21);
            this.txtOdds1314BigSmall.Text = "1.4";
            
            this.lblOdds1314Combo.Text = "尾组合赔率";
            this.lblOdds1314Combo.Location = new System.Drawing.Point(8, 50);
            this.lblOdds1314Combo.AutoSize = true;
            
            this.txtOdds1314Combo.Location = new System.Drawing.Point(100, 47);
            this.txtOdds1314Combo.Size = new System.Drawing.Size(50, 21);
            this.txtOdds1314Combo.Text = "3.8";
            
            this.lblOdds1314Special.Text = "尾特码赔率";
            this.lblOdds1314Special.Location = new System.Drawing.Point(8, 78);
            this.lblOdds1314Special.AutoSize = true;
            
            this.txtOdds1314Special.Location = new System.Drawing.Point(100, 75);
            this.txtOdds1314Special.Size = new System.Drawing.Size(50, 21);
            this.txtOdds1314Special.Text = "5";
            
            // ========================================
            // 尾球开0 9赔率组
            // ========================================
            this.grpOdds09.Text = "尾球开0  9赔率";
            this.grpOdds09.Location = new System.Drawing.Point(180, 38);
            this.grpOdds09.Size = new System.Drawing.Size(160, 75);
            this.grpOdds09.Controls.Add(this.lblOdds09BigSmall);
            this.grpOdds09.Controls.Add(this.txtOdds09BigSmall);
            this.grpOdds09.Controls.Add(this.lblOdds09Combo);
            this.grpOdds09.Controls.Add(this.txtOdds09Combo);
            
            this.lblOdds09BigSmall.Text = "尾大小单双赔率";
            this.lblOdds09BigSmall.Location = new System.Drawing.Point(8, 22);
            this.lblOdds09BigSmall.AutoSize = true;
            
            this.txtOdds09BigSmall.Location = new System.Drawing.Point(100, 19);
            this.txtOdds09BigSmall.Size = new System.Drawing.Size(50, 21);
            this.txtOdds09BigSmall.Text = "-1";
            
            this.lblOdds09Combo.Text = "尾组合赔率";
            this.lblOdds09Combo.Location = new System.Drawing.Point(8, 50);
            this.lblOdds09Combo.AutoSize = true;
            
            this.txtOdds09Combo.Location = new System.Drawing.Point(100, 47);
            this.txtOdds09Combo.Size = new System.Drawing.Size(50, 21);
            this.txtOdds09Combo.Text = "-1";
            
            // ========================================
            // 有13 14赔率组
            // ========================================
            this.grpWith1314Odds.Text = "有13 14赔率";
            this.grpWith1314Odds.Location = new System.Drawing.Point(10, 148);
            this.grpWith1314Odds.Size = new System.Drawing.Size(200, 155);
            this.grpWith1314Odds.Controls.Add(this.rbSingleBet);
            this.grpWith1314Odds.Controls.Add(this.rbTotalBet);
            this.grpWith1314Odds.Controls.Add(this.lblTailBallOver1);
            this.grpWith1314Odds.Controls.Add(this.txtTailBallOver1);
            this.grpWith1314Odds.Controls.Add(this.lblOddsWith1314BigSmall);
            this.grpWith1314Odds.Controls.Add(this.txtOddsWith1314BigSmall);
            this.grpWith1314Odds.Controls.Add(this.lblTailBallOver2);
            this.grpWith1314Odds.Controls.Add(this.txtTailBallOver2);
            this.grpWith1314Odds.Controls.Add(this.lblOddsWith1314Combo);
            this.grpWith1314Odds.Controls.Add(this.txtOddsWith1314Combo);
            
            this.rbSingleBet.Text = "算单注";
            this.rbSingleBet.Location = new System.Drawing.Point(10, 20);
            this.rbSingleBet.AutoSize = true;
            this.rbSingleBet.Checked = true;
            
            this.rbTotalBet.Text = "算总注";
            this.rbTotalBet.Location = new System.Drawing.Point(80, 20);
            this.rbTotalBet.AutoSize = true;
            
            this.lblTailBallOver1.Text = "尾球超";
            this.lblTailBallOver1.Location = new System.Drawing.Point(10, 48);
            this.lblTailBallOver1.AutoSize = true;
            
            this.txtTailBallOver1.Location = new System.Drawing.Point(55, 45);
            this.txtTailBallOver1.Size = new System.Drawing.Size(55, 21);
            this.txtTailBallOver1.Text = "1000";
            
            this.lblOddsWith1314BigSmall.Text = "尾大小单双赔率";
            this.lblOddsWith1314BigSmall.Location = new System.Drawing.Point(10, 75);
            this.lblOddsWith1314BigSmall.AutoSize = true;
            
            this.txtOddsWith1314BigSmall.Location = new System.Drawing.Point(105, 72);
            this.txtOddsWith1314BigSmall.Size = new System.Drawing.Size(45, 21);
            this.txtOddsWith1314BigSmall.Text = "0";
            
            this.lblTailBallOver2.Text = "尾球超";
            this.lblTailBallOver2.Location = new System.Drawing.Point(10, 102);
            this.lblTailBallOver2.AutoSize = true;
            
            this.txtTailBallOver2.Location = new System.Drawing.Point(55, 99);
            this.txtTailBallOver2.Size = new System.Drawing.Size(55, 21);
            this.txtTailBallOver2.Text = "1000";
            
            this.lblOddsWith1314Combo.Text = "尾组合赔率";
            this.lblOddsWith1314Combo.Location = new System.Drawing.Point(10, 129);
            this.lblOddsWith1314Combo.AutoSize = true;
            
            this.txtOddsWith1314Combo.Location = new System.Drawing.Point(105, 126);
            this.txtOddsWith1314Combo.Size = new System.Drawing.Size(45, 21);
            this.txtOddsWith1314Combo.Text = "0";
            
            // ========================================
            // 其他复选框
            // ========================================
            this.chkOtherGameCountTotal.Text = "其他玩法算进总注";
            this.chkOtherGameCountTotal.Location = new System.Drawing.Point(10, 310);
            this.chkOtherGameCountTotal.AutoSize = true;
            
            this.chkForbid09.Text = "禁止点0  9";
            this.chkForbid09.Location = new System.Drawing.Point(10, 335);
            this.chkForbid09.AutoSize = true;
            
            this.chkFrontBallEnabled.Text = "前球玩法  开启/关闭";
            this.chkFrontBallEnabled.Location = new System.Drawing.Point(10, 360);
            this.chkFrontBallEnabled.AutoSize = true;
            
            this.chkMiddleBallEnabled.Text = "中球玩法  开启/关闭";
            this.chkMiddleBallEnabled.Location = new System.Drawing.Point(10, 385);
            this.chkMiddleBallEnabled.AutoSize = true;
            
            // ========================================
            // 说明文本
            // ========================================
            this.lblExplanationTitle.Text = "前球、中球说明：";
            this.lblExplanationTitle.Location = new System.Drawing.Point(350, 148);
            this.lblExplanationTitle.AutoSize = true;
            this.lblExplanationTitle.Font = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Bold);
            
            this.lblExplanation.Text = "前球计算为一区，其他设置与尾球完全相同\r\n" +
                                       "关键字：前、q、qian、首\r\n" +
                                       "如qda100，前大100，首大100\r\n\r\n" +
                                       "中球计算为二区，其他设置与尾球完全相同\r\n" +
                                       "关键字：中、z、zhong\r\n" +
                                       "如zda100，中大100";
            this.lblExplanation.Location = new System.Drawing.Point(350, 172);
            this.lblExplanation.Size = new System.Drawing.Size(240, 130);
            
            // ========================================
            // 保存按钮
            // ========================================
            this.btnSave.Text = "保存设置";
            this.btnSave.Location = new System.Drawing.Point(220, 360);
            this.btnSave.Size = new System.Drawing.Size(90, 35);
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            
            // ========================================
            // UserControl
            // ========================================
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.chkTailBallEnabled);
            this.Controls.Add(this.chkTailBallNotCountClassic);
            this.Controls.Add(this.grpNo1314Odds);
            this.Controls.Add(this.grpOdds09);
            this.Controls.Add(this.grpWith1314Odds);
            this.Controls.Add(this.chkOtherGameCountTotal);
            this.Controls.Add(this.chkForbid09);
            this.Controls.Add(this.chkFrontBallEnabled);
            this.Controls.Add(this.chkMiddleBallEnabled);
            this.Controls.Add(this.lblExplanationTitle);
            this.Controls.Add(this.lblExplanation);
            this.Controls.Add(this.btnSave);
            this.Name = "TailBallSettingsControl";
            this.Size = new System.Drawing.Size(600, 420);
            
            this.grpNo1314Odds.ResumeLayout(false);
            this.grpNo1314Odds.PerformLayout();
            this.grpOdds09.ResumeLayout(false);
            this.grpOdds09.PerformLayout();
            this.grpWith1314Odds.ResumeLayout(false);
            this.grpWith1314Odds.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }
        
        #endregion
        
        // 顶部开关
        private System.Windows.Forms.CheckBox chkTailBallEnabled;
        private System.Windows.Forms.CheckBox chkTailBallNotCountClassic;
        
        // 无13 14赔率组
        private System.Windows.Forms.GroupBox grpNo1314Odds;
        private System.Windows.Forms.Label lblOdds1314BigSmall;
        private System.Windows.Forms.TextBox txtOdds1314BigSmall;
        private System.Windows.Forms.Label lblOdds1314Combo;
        private System.Windows.Forms.TextBox txtOdds1314Combo;
        private System.Windows.Forms.Label lblOdds1314Special;
        private System.Windows.Forms.TextBox txtOdds1314Special;
        
        // 尾球开0 9赔率组
        private System.Windows.Forms.GroupBox grpOdds09;
        private System.Windows.Forms.Label lblOdds09BigSmall;
        private System.Windows.Forms.TextBox txtOdds09BigSmall;
        private System.Windows.Forms.Label lblOdds09Combo;
        private System.Windows.Forms.TextBox txtOdds09Combo;
        
        // 有13 14赔率组
        private System.Windows.Forms.GroupBox grpWith1314Odds;
        private System.Windows.Forms.RadioButton rbSingleBet;
        private System.Windows.Forms.RadioButton rbTotalBet;
        private System.Windows.Forms.Label lblTailBallOver1;
        private System.Windows.Forms.TextBox txtTailBallOver1;
        private System.Windows.Forms.Label lblOddsWith1314BigSmall;
        private System.Windows.Forms.TextBox txtOddsWith1314BigSmall;
        private System.Windows.Forms.Label lblTailBallOver2;
        private System.Windows.Forms.TextBox txtTailBallOver2;
        private System.Windows.Forms.Label lblOddsWith1314Combo;
        private System.Windows.Forms.TextBox txtOddsWith1314Combo;
        
        // 其他复选框
        private System.Windows.Forms.CheckBox chkOtherGameCountTotal;
        private System.Windows.Forms.CheckBox chkForbid09;
        private System.Windows.Forms.CheckBox chkFrontBallEnabled;
        private System.Windows.Forms.CheckBox chkMiddleBallEnabled;
        
        // 说明文本
        private System.Windows.Forms.Label lblExplanationTitle;
        private System.Windows.Forms.Label lblExplanation;
        
        // 按钮
        private System.Windows.Forms.Button btnSave;
    }
}

