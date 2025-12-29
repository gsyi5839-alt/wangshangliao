namespace WangShangLiaoBot.Controls
{
    partial class OddsSettingsControl
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
            // Initialize TabControl for sub-tabs
            this.tabOdds = new System.Windows.Forms.TabControl();
            this.tabClassic = new System.Windows.Forms.TabPage();
            this.tabTailBall = new System.Windows.Forms.TabPage();
            this.tabDragonTiger = new System.Windows.Forms.TabPage();
            this.tabThreeArmy = new System.Windows.Forms.TabPage();
            this.tabPositionBall = new System.Windows.Forms.TabPage();
            this.tabOther = new System.Windows.Forms.TabPage();
            
            this.tabOdds.SuspendLayout();
            this.SuspendLayout();
            
            // ===== TabControl =====
            this.tabOdds.Controls.Add(this.tabClassic);
            this.tabOdds.Controls.Add(this.tabTailBall);
            this.tabOdds.Controls.Add(this.tabDragonTiger);
            this.tabOdds.Controls.Add(this.tabThreeArmy);
            this.tabOdds.Controls.Add(this.tabPositionBall);
            this.tabOdds.Controls.Add(this.tabOther);
            this.tabOdds.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabOdds.Location = new System.Drawing.Point(0, 0);
            this.tabOdds.Name = "tabOdds";
            this.tabOdds.SelectedIndex = 0;
            this.tabOdds.Size = new System.Drawing.Size(880, 560);
            
            // ----- Tab 1: Classic Play (经典玩法) -----
            this.tabClassic.Text = "经典玩法";
            this.tabClassic.Padding = new System.Windows.Forms.Padding(3);
            this.tabClassic.Size = new System.Drawing.Size(872, 531);
            this.tabClassic.UseVisualStyleBackColor = true;
            
            // ----- Tab 2: Tail Ball Play (尾球玩法) -----
            this.tabTailBall.Text = "尾球玩法";
            this.tabTailBall.Padding = new System.Windows.Forms.Padding(3);
            this.tabTailBall.Size = new System.Drawing.Size(872, 531);
            this.tabTailBall.UseVisualStyleBackColor = true;
            
            // ----- Tab 3: Dragon Tiger Play (龙虎玩法) -----
            this.tabDragonTiger.Text = "龙虎玩法";
            this.tabDragonTiger.Padding = new System.Windows.Forms.Padding(3);
            this.tabDragonTiger.Size = new System.Drawing.Size(872, 531);
            this.tabDragonTiger.UseVisualStyleBackColor = true;
            
            // ----- Tab 4: Three Army Play (三军玩法) -----
            this.tabThreeArmy.Text = "三军玩法";
            this.tabThreeArmy.Padding = new System.Windows.Forms.Padding(3);
            this.tabThreeArmy.Size = new System.Drawing.Size(872, 531);
            this.tabThreeArmy.UseVisualStyleBackColor = true;
            
            // ----- Tab 5: Position Ball Play (定位球玩法) -----
            this.tabPositionBall.Text = "定位球玩法";
            this.tabPositionBall.Padding = new System.Windows.Forms.Padding(3);
            this.tabPositionBall.Size = new System.Drawing.Size(872, 531);
            this.tabPositionBall.UseVisualStyleBackColor = true;
            
            // ----- Tab 6: Other Play (其他玩法) -----
            this.tabOther.Text = "其他玩法";
            this.tabOther.Padding = new System.Windows.Forms.Padding(3);
            this.tabOther.Size = new System.Drawing.Size(872, 531);
            this.tabOther.UseVisualStyleBackColor = true;
            
            // ===== OddsSettingsControl =====
            this.Controls.Add(this.tabOdds);
            this.Size = new System.Drawing.Size(880, 560);
            this.BackColor = System.Drawing.Color.White;
            
            this.tabOdds.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.TabControl tabOdds;
        private System.Windows.Forms.TabPage tabClassic;
        private System.Windows.Forms.TabPage tabTailBall;
        private System.Windows.Forms.TabPage tabDragonTiger;
        private System.Windows.Forms.TabPage tabThreeArmy;
        private System.Windows.Forms.TabPage tabPositionBall;
        private System.Windows.Forms.TabPage tabOther;
    }
}

