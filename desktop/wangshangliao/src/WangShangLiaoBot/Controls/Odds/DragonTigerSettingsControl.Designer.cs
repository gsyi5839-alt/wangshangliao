namespace WangShangLiaoBot.Controls.Odds
{
    partial class DragonTigerSettingsControl
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
            // 顶部开关和模式选择
            this.chkDragonTigerEnabled = new System.Windows.Forms.CheckBox();
            this.rbDragonTigerFight = new System.Windows.Forms.RadioButton();
            this.rbDragonTigerLeopard = new System.Windows.Forms.RadioButton();
            this.lblNote1 = new System.Windows.Forms.Label();
            this.lblNote2 = new System.Windows.Forms.Label();
            
            // 规则设置
            this.cboZone1 = new System.Windows.Forms.ComboBox();
            this.cboCompare = new System.Windows.Forms.ComboBox();
            this.cboZone2 = new System.Windows.Forms.ComboBox();
            this.lblRuleDesc = new System.Windows.Forms.Label();
            this.chkDrawReturn = new System.Windows.Forms.CheckBox();
            this.chkLeopardKillAll = new System.Windows.Forms.CheckBox();
            
            // 赔率设置（右侧）
            this.lblDragonTigerOdds = new System.Windows.Forms.Label();
            this.txtDragonTigerOdds = new System.Windows.Forms.TextBox();
            this.lblDrawOdds = new System.Windows.Forms.Label();
            this.txtDrawOdds = new System.Windows.Forms.TextBox();
            this.lblBetOverAmount = new System.Windows.Forms.Label();
            this.txtBetOverAmount = new System.Windows.Forms.TextBox();
            this.lblDragonTigerOdds2 = new System.Windows.Forms.Label();
            this.txtDragonTigerOdds2 = new System.Windows.Forms.TextBox();
            this.lblDrawOdds2 = new System.Windows.Forms.Label();
            this.txtDrawOdds2 = new System.Windows.Forms.TextBox();
            
            // 龙虎豹号码定义
            this.lblLeopardOdds = new System.Windows.Forms.Label();
            this.txtLeopardOdds = new System.Windows.Forms.TextBox();
            this.lblDragon = new System.Windows.Forms.Label();
            this.txtDragonNumbers = new System.Windows.Forms.TextBox();
            this.lblTiger = new System.Windows.Forms.Label();
            this.txtTigerNumbers = new System.Windows.Forms.TextBox();
            this.lblLeopard = new System.Windows.Forms.Label();
            this.txtLeopardNumbers = new System.Windows.Forms.TextBox();
            
            // 按钮
            this.btnSave = new System.Windows.Forms.Button();
            
            this.SuspendLayout();
            
            // ========================================
            // 顶部开关和模式选择
            // ========================================
            this.chkDragonTigerEnabled.Text = "龙虎玩法  开启/关闭";
            this.chkDragonTigerEnabled.Location = new System.Drawing.Point(10, 10);
            this.chkDragonTigerEnabled.AutoSize = true;
            
            this.rbDragonTigerFight.Text = "龙虎斗";
            this.rbDragonTigerFight.Location = new System.Drawing.Point(10, 35);
            this.rbDragonTigerFight.AutoSize = true;
            this.rbDragonTigerFight.Checked = true;
            
            this.rbDragonTigerLeopard.Text = "龙虎豹";
            this.rbDragonTigerLeopard.Location = new System.Drawing.Point(85, 35);
            this.rbDragonTigerLeopard.AutoSize = true;
            
            this.lblNote1.Text = "每个群龙虎斗规矩略有不同  请根据自己的规矩设置";
            this.lblNote1.Location = new System.Drawing.Point(10, 60);
            this.lblNote1.AutoSize = true;
            this.lblNote1.ForeColor = System.Drawing.Color.Gray;
            
            this.lblNote2.Text = "不懂请咨询售后";
            this.lblNote2.Location = new System.Drawing.Point(10, 78);
            this.lblNote2.AutoSize = true;
            this.lblNote2.ForeColor = System.Drawing.Color.Gray;
            
            // ========================================
            // 规则设置
            // ========================================
            this.cboZone1.Location = new System.Drawing.Point(10, 105);
            this.cboZone1.Size = new System.Drawing.Size(55, 21);
            this.cboZone1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboZone1.Items.AddRange(new object[] { "一区", "二区", "三区" });
            this.cboZone1.SelectedIndex = 0;
            
            this.cboCompare.Location = new System.Drawing.Point(72, 105);
            this.cboCompare.Size = new System.Drawing.Size(55, 21);
            this.cboCompare.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboCompare.Items.AddRange(new object[] { "大于", "小于", "等于" });
            this.cboCompare.SelectedIndex = 0;
            
            this.cboZone2.Location = new System.Drawing.Point(134, 105);
            this.cboZone2.Size = new System.Drawing.Size(55, 21);
            this.cboZone2.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboZone2.Items.AddRange(new object[] { "一区", "二区", "三区" });
            this.cboZone2.SelectedIndex = 0;
            
            this.lblRuleDesc.Text = "则开龙，小于则开虎，相等则开和";
            this.lblRuleDesc.Location = new System.Drawing.Point(195, 108);
            this.lblRuleDesc.AutoSize = true;
            
            this.chkDrawReturn.Text = "开和，龙虎回本";
            this.chkDrawReturn.Location = new System.Drawing.Point(10, 135);
            this.chkDrawReturn.AutoSize = true;
            
            this.chkLeopardKillAll.Text = "豹子通杀龙虎和";
            this.chkLeopardKillAll.Location = new System.Drawing.Point(10, 160);
            this.chkLeopardKillAll.AutoSize = true;
            
            // ========================================
            // 赔率设置（右侧）
            // ========================================
            int rightX = 390;
            int rightY = 105;
            int lineH = 28;
            
            this.lblDragonTigerOdds.Text = "龙虎赔率";
            this.lblDragonTigerOdds.Location = new System.Drawing.Point(rightX, rightY + 3);
            this.lblDragonTigerOdds.AutoSize = true;
            
            this.txtDragonTigerOdds.Location = new System.Drawing.Point(rightX + 60, rightY);
            this.txtDragonTigerOdds.Size = new System.Drawing.Size(55, 21);
            this.txtDragonTigerOdds.Text = "0.6";
            
            rightY += lineH;
            this.lblDrawOdds.Text = "和赔率";
            this.lblDrawOdds.Location = new System.Drawing.Point(rightX, rightY + 3);
            this.lblDrawOdds.AutoSize = true;
            
            this.txtDrawOdds.Location = new System.Drawing.Point(rightX + 60, rightY);
            this.txtDrawOdds.Size = new System.Drawing.Size(55, 21);
            
            rightY += lineH;
            this.lblBetOverAmount.Text = "龙虎和下注总额超";
            this.lblBetOverAmount.Location = new System.Drawing.Point(rightX - 50, rightY + 3);
            this.lblBetOverAmount.AutoSize = true;
            
            this.txtBetOverAmount.Location = new System.Drawing.Point(rightX + 60, rightY);
            this.txtBetOverAmount.Size = new System.Drawing.Size(55, 21);
            
            rightY += lineH;
            this.lblDragonTigerOdds2.Text = "龙虎赔率";
            this.lblDragonTigerOdds2.Location = new System.Drawing.Point(rightX, rightY + 3);
            this.lblDragonTigerOdds2.AutoSize = true;
            
            this.txtDragonTigerOdds2.Location = new System.Drawing.Point(rightX + 60, rightY);
            this.txtDragonTigerOdds2.Size = new System.Drawing.Size(55, 21);
            
            rightY += lineH;
            this.lblDrawOdds2.Text = "和赔率";
            this.lblDrawOdds2.Location = new System.Drawing.Point(rightX, rightY + 3);
            this.lblDrawOdds2.AutoSize = true;
            
            this.txtDrawOdds2.Location = new System.Drawing.Point(rightX + 60, rightY);
            this.txtDrawOdds2.Size = new System.Drawing.Size(55, 21);
            
            // ========================================
            // 龙虎豹号码定义
            // ========================================
            int bottomY = 200;
            
            this.lblLeopardOdds.Text = "龙虎豹赔率";
            this.lblLeopardOdds.Location = new System.Drawing.Point(10, bottomY + 3);
            this.lblLeopardOdds.AutoSize = true;
            
            this.txtLeopardOdds.Location = new System.Drawing.Point(80, bottomY);
            this.txtLeopardOdds.Size = new System.Drawing.Size(55, 21);
            this.txtLeopardOdds.Text = "0.6";
            
            bottomY += 30;
            this.lblDragon.Text = "龙";
            this.lblDragon.Location = new System.Drawing.Point(10, bottomY + 3);
            this.lblDragon.AutoSize = true;
            
            this.txtDragonNumbers.Location = new System.Drawing.Point(30, bottomY);
            this.txtDragonNumbers.Size = new System.Drawing.Size(260, 21);
            this.txtDragonNumbers.Text = "00, 03, 06, 09, 12, 15, 18, 21, 24, 27";
            
            bottomY += 28;
            this.lblTiger.Text = "虎";
            this.lblTiger.Location = new System.Drawing.Point(10, bottomY + 3);
            this.lblTiger.AutoSize = true;
            
            this.txtTigerNumbers.Location = new System.Drawing.Point(30, bottomY);
            this.txtTigerNumbers.Size = new System.Drawing.Size(260, 21);
            this.txtTigerNumbers.Text = "01, 04, 07, 10, 13, 16, 19, 22, 25";
            
            bottomY += 28;
            this.lblLeopard.Text = "豹";
            this.lblLeopard.Location = new System.Drawing.Point(10, bottomY + 3);
            this.lblLeopard.AutoSize = true;
            
            this.txtLeopardNumbers.Location = new System.Drawing.Point(30, bottomY);
            this.txtLeopardNumbers.Size = new System.Drawing.Size(260, 21);
            this.txtLeopardNumbers.Text = "02, 05, 08, 11, 14, 17, 20, 23, 26";
            
            // ========================================
            // 保存按钮
            // ========================================
            this.btnSave.Text = "保存设置";
            this.btnSave.Location = new System.Drawing.Point(380, 320);
            this.btnSave.Size = new System.Drawing.Size(90, 35);
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            
            // ========================================
            // UserControl
            // ========================================
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.chkDragonTigerEnabled);
            this.Controls.Add(this.rbDragonTigerFight);
            this.Controls.Add(this.rbDragonTigerLeopard);
            this.Controls.Add(this.lblNote1);
            this.Controls.Add(this.lblNote2);
            this.Controls.Add(this.cboZone1);
            this.Controls.Add(this.cboCompare);
            this.Controls.Add(this.cboZone2);
            this.Controls.Add(this.lblRuleDesc);
            this.Controls.Add(this.chkDrawReturn);
            this.Controls.Add(this.chkLeopardKillAll);
            this.Controls.Add(this.lblDragonTigerOdds);
            this.Controls.Add(this.txtDragonTigerOdds);
            this.Controls.Add(this.lblDrawOdds);
            this.Controls.Add(this.txtDrawOdds);
            this.Controls.Add(this.lblBetOverAmount);
            this.Controls.Add(this.txtBetOverAmount);
            this.Controls.Add(this.lblDragonTigerOdds2);
            this.Controls.Add(this.txtDragonTigerOdds2);
            this.Controls.Add(this.lblDrawOdds2);
            this.Controls.Add(this.txtDrawOdds2);
            this.Controls.Add(this.lblLeopardOdds);
            this.Controls.Add(this.txtLeopardOdds);
            this.Controls.Add(this.lblDragon);
            this.Controls.Add(this.txtDragonNumbers);
            this.Controls.Add(this.lblTiger);
            this.Controls.Add(this.txtTigerNumbers);
            this.Controls.Add(this.lblLeopard);
            this.Controls.Add(this.txtLeopardNumbers);
            this.Controls.Add(this.btnSave);
            this.Name = "DragonTigerSettingsControl";
            this.Size = new System.Drawing.Size(520, 380);
            
            this.ResumeLayout(false);
            this.PerformLayout();
        }
        
        #endregion
        
        // 顶部开关和模式选择
        private System.Windows.Forms.CheckBox chkDragonTigerEnabled;
        private System.Windows.Forms.RadioButton rbDragonTigerFight;
        private System.Windows.Forms.RadioButton rbDragonTigerLeopard;
        private System.Windows.Forms.Label lblNote1;
        private System.Windows.Forms.Label lblNote2;
        
        // 规则设置
        private System.Windows.Forms.ComboBox cboZone1;
        private System.Windows.Forms.ComboBox cboCompare;
        private System.Windows.Forms.ComboBox cboZone2;
        private System.Windows.Forms.Label lblRuleDesc;
        private System.Windows.Forms.CheckBox chkDrawReturn;
        private System.Windows.Forms.CheckBox chkLeopardKillAll;
        
        // 赔率设置
        private System.Windows.Forms.Label lblDragonTigerOdds;
        private System.Windows.Forms.TextBox txtDragonTigerOdds;
        private System.Windows.Forms.Label lblDrawOdds;
        private System.Windows.Forms.TextBox txtDrawOdds;
        private System.Windows.Forms.Label lblBetOverAmount;
        private System.Windows.Forms.TextBox txtBetOverAmount;
        private System.Windows.Forms.Label lblDragonTigerOdds2;
        private System.Windows.Forms.TextBox txtDragonTigerOdds2;
        private System.Windows.Forms.Label lblDrawOdds2;
        private System.Windows.Forms.TextBox txtDrawOdds2;
        
        // 龙虎豹号码定义
        private System.Windows.Forms.Label lblLeopardOdds;
        private System.Windows.Forms.TextBox txtLeopardOdds;
        private System.Windows.Forms.Label lblDragon;
        private System.Windows.Forms.TextBox txtDragonNumbers;
        private System.Windows.Forms.Label lblTiger;
        private System.Windows.Forms.TextBox txtTigerNumbers;
        private System.Windows.Forms.Label lblLeopard;
        private System.Windows.Forms.TextBox txtLeopardNumbers;
        
        // 按钮
        private System.Windows.Forms.Button btnSave;
    }
}


