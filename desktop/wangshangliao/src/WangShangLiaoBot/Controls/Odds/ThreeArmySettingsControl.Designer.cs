namespace WangShangLiaoBot.Controls.Odds
{
    partial class ThreeArmySettingsControl
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
            // 开关
            this.chkThreeArmyEnabled = new System.Windows.Forms.CheckBox();
            
            // 赔率设置
            this.lblOdds = new System.Windows.Forms.Label();
            this.txtOdds1 = new System.Windows.Forms.TextBox();
            this.txtOdds2 = new System.Windows.Forms.TextBox();
            this.txtOdds3 = new System.Windows.Forms.TextBox();
            
            // 按钮
            this.btnSave = new System.Windows.Forms.Button();
            
            this.SuspendLayout();
            
            // ========================================
            // 开关
            // ========================================
            this.chkThreeArmyEnabled.Text = "三军玩法  开启/关闭";
            this.chkThreeArmyEnabled.Location = new System.Drawing.Point(10, 10);
            this.chkThreeArmyEnabled.AutoSize = true;
            
            // ========================================
            // 赔率设置
            // ========================================
            this.lblOdds.Text = "三军赔率";
            this.lblOdds.Location = new System.Drawing.Point(10, 45);
            this.lblOdds.AutoSize = true;
            
            this.txtOdds1.Location = new System.Drawing.Point(75, 42);
            this.txtOdds1.Size = new System.Drawing.Size(50, 21);
            
            this.txtOdds2.Location = new System.Drawing.Point(135, 42);
            this.txtOdds2.Size = new System.Drawing.Size(50, 21);
            
            this.txtOdds3.Location = new System.Drawing.Point(195, 42);
            this.txtOdds3.Size = new System.Drawing.Size(50, 21);
            
            // ========================================
            // 保存按钮
            // ========================================
            this.btnSave.Text = "保存设置";
            this.btnSave.Location = new System.Drawing.Point(100, 85);
            this.btnSave.Size = new System.Drawing.Size(90, 30);
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            
            // ========================================
            // UserControl
            // ========================================
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.chkThreeArmyEnabled);
            this.Controls.Add(this.lblOdds);
            this.Controls.Add(this.txtOdds1);
            this.Controls.Add(this.txtOdds2);
            this.Controls.Add(this.txtOdds3);
            this.Controls.Add(this.btnSave);
            this.Name = "ThreeArmySettingsControl";
            this.Size = new System.Drawing.Size(520, 380);
            
            this.ResumeLayout(false);
            this.PerformLayout();
        }
        
        #endregion
        
        // 开关
        private System.Windows.Forms.CheckBox chkThreeArmyEnabled;
        
        // 赔率设置
        private System.Windows.Forms.Label lblOdds;
        private System.Windows.Forms.TextBox txtOdds1;
        private System.Windows.Forms.TextBox txtOdds2;
        private System.Windows.Forms.TextBox txtOdds3;
        
        // 按钮
        private System.Windows.Forms.Button btnSave;
    }
}


