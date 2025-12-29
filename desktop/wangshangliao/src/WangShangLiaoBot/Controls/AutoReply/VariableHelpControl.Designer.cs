namespace WangShangLiaoBot.Controls.AutoReply
{
    partial class VariableHelpControl
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
            this.txtVariableHelp = new System.Windows.Forms.TextBox();
            this.btnExport = new System.Windows.Forms.Button();
            
            this.SuspendLayout();
            
            // ==========================================
            // Variable Help TextBox
            // ==========================================
            this.txtVariableHelp.Location = new System.Drawing.Point(10, 10);
            this.txtVariableHelp.Size = new System.Drawing.Size(620, 340);
            this.txtVariableHelp.Multiline = true;
            this.txtVariableHelp.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtVariableHelp.ReadOnly = true;
            this.txtVariableHelp.BackColor = System.Drawing.Color.White;
            this.txtVariableHelp.Font = new System.Drawing.Font("Consolas", 9F);
            this.txtVariableHelp.Name = "txtVariableHelp";
            
            // ==========================================
            // Export Button
            // ==========================================
            this.btnExport.Location = new System.Drawing.Point(530, 360);
            this.btnExport.Size = new System.Drawing.Size(100, 28);
            this.btnExport.Text = "导出txt";
            this.btnExport.Name = "btnExport";
            this.btnExport.UseVisualStyleBackColor = true;
            this.btnExport.Click += new System.EventHandler(this.btnExport_Click);
            
            // ==========================================
            // Main Control Setup
            // ==========================================
            this.Controls.Add(this.txtVariableHelp);
            this.Controls.Add(this.btnExport);
            
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Name = "VariableHelpControl";
            this.Size = new System.Drawing.Size(650, 400);
            
            this.ResumeLayout(false);
            this.PerformLayout();
        }
        
        #endregion
        
        private System.Windows.Forms.TextBox txtVariableHelp;
        private System.Windows.Forms.Button btnExport;
    }
}

