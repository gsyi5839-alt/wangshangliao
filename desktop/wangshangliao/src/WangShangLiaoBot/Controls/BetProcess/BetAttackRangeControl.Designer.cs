namespace WangShangLiaoBot.Controls.BetProcess
{
    partial class BetAttackRangeControl
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
            this.SuspendLayout();

            // =====================================================
            // Title Label - Outside the border
            // =====================================================
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblTitle.Text = "下注范围-不在范围内下注无效";
            this.lblTitle.Location = new System.Drawing.Point(8, 5);
            this.lblTitle.AutoSize = true;

            // =====================================================
            // GroupBox - Border around bet items (match design)
            // =====================================================
            this.grpBetItems = new System.Windows.Forms.GroupBox();
            this.grpBetItems.Text = "";
            this.grpBetItems.Location = new System.Drawing.Point(8, 22);
            this.grpBetItems.Size = new System.Drawing.Size(510, 138);

            // =====================================================
            // Layout Constants - Optimized spacing to prevent overlap
            // =====================================================
            int baseY = 12;   // Y inside groupbox
            int rowH = 24;    // Row height
            
            // Column X positions - calculated for label widths
            // 2-char: 32 + 38 + 10 + 38 = 118px
            // 3-char: 48 + 38 + 10 + 38 = 134px
            int c1 = 5;       // 单注, 组合, 数字, 极数, 龙虎 (2字)
            int c2 = 125;     // 对子, 顺子, 豹子, 半顺, 杂 (2字)
            int c3 = 245;     // 尾单注, 尾组合, 尾数字, 和, 三军 (3字/2字)
            int c4 = 385;     // 大边, 小边, 边, 中, 总额封顶 (2字/1字)

            // =====================================================
            // Row 1: 单注, 对子, 尾单注, 大边
            // =====================================================
            int y1 = baseY;
            CreateBetItem("单注", c1, y1, out lblSingleBet, out nudSingleBetMin, out lblSingleBetDash, out nudSingleBetMax, 2, 3000);
            CreateBetItem("对子", c2, y1, out lblPair, out nudPairMin, out lblPairDash, out nudPairMax, 2, 500);
            CreateBetItem("尾单注", c3, y1, out lblTailSingle, out nudTailSingleMin, out lblTailSingleDash, out nudTailSingleMax, 0, 0);
            CreateBetItem("大边", c4, y1, out lblBigEdge, out nudBigEdgeMin, out lblBigEdgeDash, out nudBigEdgeMax, 0, 0);

            // =====================================================
            // Row 2: 组合, 顺子, 尾组合, 小边
            // =====================================================
            int y2 = baseY + rowH;
            CreateBetItem("组合", c1, y2, out lblCombination, out nudCombinationMin, out lblCombinationDash, out nudCombinationMax, 2, 1000);
            CreateBetItem("顺子", c2, y2, out lblStraight, out nudStraightMin, out lblStraightDash, out nudStraightMax, 2, 500);
            CreateBetItem("尾组合", c3, y2, out lblTailCombination, out nudTailCombinationMin, out lblTailCombinationDash, out nudTailCombinationMax, 0, 0);
            CreateBetItem("小边", c4, y2, out lblSmallEdge, out nudSmallEdgeMin, out lblSmallEdgeDash, out nudSmallEdgeMax, 0, 0);

            // =====================================================
            // Row 3: 数字, 豹子, 尾数字, 边
            // =====================================================
            int y3 = baseY + rowH * 2;
            CreateBetItem("数字", c1, y3, out lblDigit, out nudDigitMin, out lblDigitDash, out nudDigitMax, 2, 500);
            CreateBetItem("豹子", c2, y3, out lblLeopard, out nudLeopardMin, out lblLeopardDash, out nudLeopardMax, 2, 200);
            CreateBetItem("尾数字", c3, y3, out lblTailDigit, out nudTailDigitMin, out lblTailDigitDash, out nudTailDigitMax, 0, 0);
            CreateBetItem("边", c4, y3, out lblEdge, out nudEdgeMin, out lblEdgeDash, out nudEdgeMax, 0, 0);

            // =====================================================
            // Row 4: 极数, 半顺, 和, 中
            // =====================================================
            int y4 = baseY + rowH * 3;
            CreateBetItem("极数", c1, y4, out lblExtreme, out nudExtremeMin, out lblExtremeDash, out nudExtremeMax, 2, 500);
            CreateBetItem("半顺", c2, y4, out lblHalfStraight, out nudHalfStraightMin, out lblHalfStraightDash, out nudHalfStraightMax, 0, 0);
            CreateBetItem("和", c3, y4, out lblSum, out nudSumMin, out lblSumDash, out nudSumMax, 0, 0);
            CreateBetItem("中", c4, y4, out lblMiddle, out nudMiddleMin, out lblMiddleDash, out nudMiddleMax, 0, 0);

            // =====================================================
            // Row 5: 龙虎, 杂, 三军, 总额封顶
            // =====================================================
            int y5 = baseY + rowH * 4;
            CreateBetItem("龙虎", c1, y5, out lblDragonTiger, out nudDragonTigerMin, out lblDragonTigerDash, out nudDragonTigerMax, 0, 0);
            CreateBetItem("杂", c2, y5, out lblMixed, out nudMixedMin, out lblMixedDash, out nudMixedMax, 0, 0);
            CreateBetItem("三军", c3, y5, out lblThreeArmy, out nudThreeArmyMin, out lblThreeArmyDash, out nudThreeArmyMax, 0, 0);

            // 总额封顶 - Only one input box (4 chars need 56px width)
            this.lblTotalLimit = new System.Windows.Forms.Label();
            this.lblTotalLimit.Text = "总额封顶";
            this.lblTotalLimit.Location = new System.Drawing.Point(c4, y5 + 2);
            this.lblTotalLimit.Size = new System.Drawing.Size(56, 15);
            this.lblTotalLimit.AutoSize = false;

            this.nudTotalLimit = new System.Windows.Forms.NumericUpDown();
            this.nudTotalLimit.Minimum = 0;
            this.nudTotalLimit.Maximum = 9999999;
            this.nudTotalLimit.Value = 60000;
            this.nudTotalLimit.Location = new System.Drawing.Point(c4 + 58, y5);
            this.nudTotalLimit.Size = new System.Drawing.Size(58, 21);

            // =====================================================
            // Add controls to GroupBox
            // =====================================================
            AddToGroup(lblSingleBet, nudSingleBetMin, lblSingleBetDash, nudSingleBetMax);
            AddToGroup(lblPair, nudPairMin, lblPairDash, nudPairMax);
            AddToGroup(lblTailSingle, nudTailSingleMin, lblTailSingleDash, nudTailSingleMax);
            AddToGroup(lblBigEdge, nudBigEdgeMin, lblBigEdgeDash, nudBigEdgeMax);

            AddToGroup(lblCombination, nudCombinationMin, lblCombinationDash, nudCombinationMax);
            AddToGroup(lblStraight, nudStraightMin, lblStraightDash, nudStraightMax);
            AddToGroup(lblTailCombination, nudTailCombinationMin, lblTailCombinationDash, nudTailCombinationMax);
            AddToGroup(lblSmallEdge, nudSmallEdgeMin, lblSmallEdgeDash, nudSmallEdgeMax);

            AddToGroup(lblDigit, nudDigitMin, lblDigitDash, nudDigitMax);
            AddToGroup(lblLeopard, nudLeopardMin, lblLeopardDash, nudLeopardMax);
            AddToGroup(lblTailDigit, nudTailDigitMin, lblTailDigitDash, nudTailDigitMax);
            AddToGroup(lblEdge, nudEdgeMin, lblEdgeDash, nudEdgeMax);

            AddToGroup(lblExtreme, nudExtremeMin, lblExtremeDash, nudExtremeMax);
            AddToGroup(lblHalfStraight, nudHalfStraightMin, lblHalfStraightDash, nudHalfStraightMax);
            AddToGroup(lblSum, nudSumMin, lblSumDash, nudSumMax);
            AddToGroup(lblMiddle, nudMiddleMin, lblMiddleDash, nudMiddleMax);

            AddToGroup(lblDragonTiger, nudDragonTigerMin, lblDragonTigerDash, nudDragonTigerMax);
            AddToGroup(lblMixed, nudMixedMin, lblMixedDash, nudMixedMax);
            AddToGroup(lblThreeArmy, nudThreeArmyMin, lblThreeArmyDash, nudThreeArmyMax);
            this.grpBetItems.Controls.Add(this.lblTotalLimit);
            this.grpBetItems.Controls.Add(this.nudTotalLimit);

            // =====================================================
            // Right Side - Description Box (outside groupbox)
            // =====================================================
            this.txtRangeDescription = new System.Windows.Forms.RichTextBox();
            this.txtRangeDescription.Text =
                "超范围提示说明\r\n" +
                "[下注内容]自动替换玩家\r\n" +
                "超范围的下注内容\r\n" +
                "[高低]根据玩家下注情况\r\n" +
                "替换为高于封顶分数,\r\n" +
                "如\"高于10000\"或\"低于\r\n" +
                "20\"";
            this.txtRangeDescription.ReadOnly = true;
            this.txtRangeDescription.Location = new System.Drawing.Point(525, 22);
            this.txtRangeDescription.Size = new System.Drawing.Size(145, 120);
            this.txtRangeDescription.BackColor = System.Drawing.SystemColors.Window;
            this.txtRangeDescription.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.txtRangeDescription.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.None;

            // =====================================================
            // Bottom - Hint Area
            // =====================================================
            int hintY = 165;

            this.lblOverRangeHint = new System.Windows.Forms.Label();
            this.lblOverRangeHint.Text = "超范围提示";
            this.lblOverRangeHint.Location = new System.Drawing.Point(8, hintY + 3);
            this.lblOverRangeHint.Size = new System.Drawing.Size(65, 15);

            this.txtOverRangeHint = new System.Windows.Forms.TextBox();
            this.txtOverRangeHint.Text = "@qq 您攻击的[[下注内容]]分数不能[高低],请及时修改攻击";
            this.txtOverRangeHint.Location = new System.Drawing.Point(75, hintY);
            this.txtOverRangeHint.Size = new System.Drawing.Size(430, 21);

            // Save Button
            this.btnSaveSettings = new System.Windows.Forms.Button();
            this.btnSaveSettings.Text = "保存设置";
            this.btnSaveSettings.Location = new System.Drawing.Point(590, hintY - 1);
            this.btnSaveSettings.Size = new System.Drawing.Size(75, 23);

            // =====================================================
            // Add main controls to form
            // =====================================================
            this.Controls.Add(this.lblTitle);
            this.Controls.Add(this.grpBetItems);
            this.Controls.Add(this.txtRangeDescription);
            this.Controls.Add(this.lblOverRangeHint);
            this.Controls.Add(this.txtOverRangeHint);
            this.Controls.Add(this.btnSaveSettings);

            // =====================================================
            // Main Container Settings
            // =====================================================
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Size = new System.Drawing.Size(680, 195);

            this.ResumeLayout(false);
            this.PerformLayout();
        }

        /// <summary>
        /// Create bet item: Label + MinNud + Dash + MaxNud
        /// Optimized label width for Chinese characters
        /// </summary>
        private void CreateBetItem(string text, int x, int y,
            out System.Windows.Forms.Label lbl,
            out System.Windows.Forms.NumericUpDown nudMin,
            out System.Windows.Forms.Label lblDash,
            out System.Windows.Forms.NumericUpDown nudMax,
            decimal minVal, decimal maxVal)
        {
            // Label width: 1 char=16px, 2 chars=32px, 3 chars=48px
            int lblW = text.Length == 1 ? 16 : (text.Length == 2 ? 32 : 48);
            int nudW = 38;
            int dashW = 10;

            lbl = new System.Windows.Forms.Label();
            lbl.Text = text;
            lbl.Location = new System.Drawing.Point(x, y + 2);
            lbl.Size = new System.Drawing.Size(lblW, 15);
            lbl.AutoSize = false;

            nudMin = new System.Windows.Forms.NumericUpDown();
            nudMin.Minimum = 0;
            nudMin.Maximum = 9999999;
            nudMin.Value = minVal;
            nudMin.Location = new System.Drawing.Point(x + lblW, y);
            nudMin.Size = new System.Drawing.Size(nudW, 21);

            lblDash = new System.Windows.Forms.Label();
            lblDash.Text = "-";
            lblDash.Location = new System.Drawing.Point(x + lblW + nudW, y + 2);
            lblDash.Size = new System.Drawing.Size(dashW, 15);
            lblDash.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

            nudMax = new System.Windows.Forms.NumericUpDown();
            nudMax.Minimum = 0;
            nudMax.Maximum = 9999999;
            nudMax.Value = maxVal;
            nudMax.Location = new System.Drawing.Point(x + lblW + nudW + dashW, y);
            nudMax.Size = new System.Drawing.Size(nudW, 21);
        }

        /// <summary>
        /// Add bet item controls to groupbox
        /// </summary>
        private void AddToGroup(System.Windows.Forms.Label lbl,
            System.Windows.Forms.NumericUpDown nudMin,
            System.Windows.Forms.Label lblDash,
            System.Windows.Forms.NumericUpDown nudMax)
        {
            this.grpBetItems.Controls.Add(lbl);
            this.grpBetItems.Controls.Add(nudMin);
            this.grpBetItems.Controls.Add(lblDash);
            this.grpBetItems.Controls.Add(nudMax);
        }

        // Title
        private System.Windows.Forms.Label lblTitle;

        // GroupBox for bet items (border)
        private System.Windows.Forms.GroupBox grpBetItems;

        // Row 1
        private System.Windows.Forms.Label lblSingleBet;
        private System.Windows.Forms.NumericUpDown nudSingleBetMin;
        private System.Windows.Forms.Label lblSingleBetDash;
        private System.Windows.Forms.NumericUpDown nudSingleBetMax;

        private System.Windows.Forms.Label lblPair;
        private System.Windows.Forms.NumericUpDown nudPairMin;
        private System.Windows.Forms.Label lblPairDash;
        private System.Windows.Forms.NumericUpDown nudPairMax;

        private System.Windows.Forms.Label lblTailSingle;
        private System.Windows.Forms.NumericUpDown nudTailSingleMin;
        private System.Windows.Forms.Label lblTailSingleDash;
        private System.Windows.Forms.NumericUpDown nudTailSingleMax;

        private System.Windows.Forms.Label lblBigEdge;
        private System.Windows.Forms.NumericUpDown nudBigEdgeMin;
        private System.Windows.Forms.Label lblBigEdgeDash;
        private System.Windows.Forms.NumericUpDown nudBigEdgeMax;

        // Row 2
        private System.Windows.Forms.Label lblCombination;
        private System.Windows.Forms.NumericUpDown nudCombinationMin;
        private System.Windows.Forms.Label lblCombinationDash;
        private System.Windows.Forms.NumericUpDown nudCombinationMax;

        private System.Windows.Forms.Label lblStraight;
        private System.Windows.Forms.NumericUpDown nudStraightMin;
        private System.Windows.Forms.Label lblStraightDash;
        private System.Windows.Forms.NumericUpDown nudStraightMax;

        private System.Windows.Forms.Label lblTailCombination;
        private System.Windows.Forms.NumericUpDown nudTailCombinationMin;
        private System.Windows.Forms.Label lblTailCombinationDash;
        private System.Windows.Forms.NumericUpDown nudTailCombinationMax;

        private System.Windows.Forms.Label lblSmallEdge;
        private System.Windows.Forms.NumericUpDown nudSmallEdgeMin;
        private System.Windows.Forms.Label lblSmallEdgeDash;
        private System.Windows.Forms.NumericUpDown nudSmallEdgeMax;

        // Row 3
        private System.Windows.Forms.Label lblDigit;
        private System.Windows.Forms.NumericUpDown nudDigitMin;
        private System.Windows.Forms.Label lblDigitDash;
        private System.Windows.Forms.NumericUpDown nudDigitMax;

        private System.Windows.Forms.Label lblLeopard;
        private System.Windows.Forms.NumericUpDown nudLeopardMin;
        private System.Windows.Forms.Label lblLeopardDash;
        private System.Windows.Forms.NumericUpDown nudLeopardMax;

        private System.Windows.Forms.Label lblTailDigit;
        private System.Windows.Forms.NumericUpDown nudTailDigitMin;
        private System.Windows.Forms.Label lblTailDigitDash;
        private System.Windows.Forms.NumericUpDown nudTailDigitMax;

        private System.Windows.Forms.Label lblEdge;
        private System.Windows.Forms.NumericUpDown nudEdgeMin;
        private System.Windows.Forms.Label lblEdgeDash;
        private System.Windows.Forms.NumericUpDown nudEdgeMax;

        // Row 4
        private System.Windows.Forms.Label lblExtreme;
        private System.Windows.Forms.NumericUpDown nudExtremeMin;
        private System.Windows.Forms.Label lblExtremeDash;
        private System.Windows.Forms.NumericUpDown nudExtremeMax;

        private System.Windows.Forms.Label lblHalfStraight;
        private System.Windows.Forms.NumericUpDown nudHalfStraightMin;
        private System.Windows.Forms.Label lblHalfStraightDash;
        private System.Windows.Forms.NumericUpDown nudHalfStraightMax;

        private System.Windows.Forms.Label lblSum;
        private System.Windows.Forms.NumericUpDown nudSumMin;
        private System.Windows.Forms.Label lblSumDash;
        private System.Windows.Forms.NumericUpDown nudSumMax;

        private System.Windows.Forms.Label lblMiddle;
        private System.Windows.Forms.NumericUpDown nudMiddleMin;
        private System.Windows.Forms.Label lblMiddleDash;
        private System.Windows.Forms.NumericUpDown nudMiddleMax;

        // Row 5
        private System.Windows.Forms.Label lblDragonTiger;
        private System.Windows.Forms.NumericUpDown nudDragonTigerMin;
        private System.Windows.Forms.Label lblDragonTigerDash;
        private System.Windows.Forms.NumericUpDown nudDragonTigerMax;

        private System.Windows.Forms.Label lblMixed;
        private System.Windows.Forms.NumericUpDown nudMixedMin;
        private System.Windows.Forms.Label lblMixedDash;
        private System.Windows.Forms.NumericUpDown nudMixedMax;

        private System.Windows.Forms.Label lblThreeArmy;
        private System.Windows.Forms.NumericUpDown nudThreeArmyMin;
        private System.Windows.Forms.Label lblThreeArmyDash;
        private System.Windows.Forms.NumericUpDown nudThreeArmyMax;

        private System.Windows.Forms.Label lblTotalLimit;
        private System.Windows.Forms.NumericUpDown nudTotalLimit;

        // Right description
        private System.Windows.Forms.RichTextBox txtRangeDescription;

        // Bottom hint
        private System.Windows.Forms.Label lblOverRangeHint;
        private System.Windows.Forms.TextBox txtOverRangeHint;
        private System.Windows.Forms.Button btnSaveSettings;
    }
}
