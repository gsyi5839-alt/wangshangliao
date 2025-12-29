namespace WangShangLiaoBot.Controls.Odds
{
    partial class ClassicPlaySettingsControl
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
            this.SuspendLayout();
            
            // ===== Left Column =====
            // Group 1: 大-小-单-双超 设置
            this.grpSizeOddEven = new System.Windows.Forms.GroupBox();
            this.grpSizeOddEven.Location = new System.Drawing.Point(5, 5);
            this.grpSizeOddEven.Size = new System.Drawing.Size(200, 145);
            
            // Title with input: 大-小-单-双超 13|14 设置
            this.lblSizeTitle = CreateLabel("大-小-单-双超", 10, 0, 80);
            this.txtSize1314 = new System.Windows.Forms.TextBox();
            this.txtSize1314.Text = "13|14";
            this.txtSize1314.Location = new System.Drawing.Point(90, -2);
            this.txtSize1314.Size = new System.Drawing.Size(45, 20);
            this.txtSize1314.Font = new System.Drawing.Font("Microsoft YaHei UI", 8F);
            this.lblSizeTitle2 = CreateLabel("设置", 138, 0, 30);
            this.grpSizeOddEven.Controls.Add(this.lblSizeTitle);
            this.grpSizeOddEven.Controls.Add(this.txtSize1314);
            this.grpSizeOddEven.Controls.Add(this.lblSizeTitle2);
            
            this.rbSumBet = new System.Windows.Forms.RadioButton();
            this.rbSumBet.Text = "算总注";
            this.rbSumBet.Location = new System.Drawing.Point(10, 18);
            this.rbSumBet.Size = new System.Drawing.Size(65, 20);
            this.rbSumBet.Checked = true;
            
            this.rbSingleBet = new System.Windows.Forms.RadioButton();
            this.rbSingleBet.Text = "算单注";
            this.rbSingleBet.Location = new System.Drawing.Point(80, 18);
            this.rbSingleBet.Size = new System.Drawing.Size(65, 20);
            
            // Row 1: 2 - 10000
            this.numRow1Min = CreateNumeric(10, 42, 45, 2);
            this.lblRow1Dash = CreateLabel("-", 58, 44, 10);
            this.numRow1Max = CreateNumeric(70, 42, 55, 10000);
            this.lblRow1Odds = CreateLabel("赔率", 130, 44, 30);
            this.numRow1Odds = CreateNumeric(160, 42, 35, 0);
            
            // Row 2: 10001 - 60000
            this.numRow2Min = CreateNumeric(10, 68, 45, 10001);
            this.lblRow2Dash = CreateLabel("-", 58, 70, 10);
            this.numRow2Max = CreateNumeric(70, 68, 55, 60000);
            this.lblRow2Odds = CreateLabel("赔率", 130, 70, 30);
            this.numRow2Odds = CreateNumeric(160, 68, 35, 0);
            
            // Row 3: 60001 -
            this.numRow3Min = CreateNumeric(10, 94, 45, 60001);
            this.lblRow3Dash = CreateLabel("-", 58, 96, 10);
            this.numRow3Max = CreateNumeric(70, 94, 55, 0);
            this.lblRow3Odds = CreateLabel("赔率", 130, 96, 30);
            this.numRow3Odds = CreateNumeric(160, 94, 35, 0);
            
            // Row 4: 600001 -
            this.numRow4Min = CreateNumeric(10, 120, 45, 600001);
            this.lblRow4Dash = CreateLabel("-", 58, 122, 10);
            this.numRow4Max = CreateNumeric(70, 120, 55, 0);
            this.lblRow4Odds = CreateLabel("赔率", 130, 122, 30);
            this.numRow4Odds = CreateNumeric(160, 120, 35, 0);
            
            this.grpSizeOddEven.Controls.AddRange(new System.Windows.Forms.Control[] {
                rbSumBet, rbSingleBet,
                numRow1Min, lblRow1Dash, numRow1Max, lblRow1Odds, numRow1Odds,
                numRow2Min, lblRow2Dash, numRow2Max, lblRow2Odds, numRow2Odds,
                numRow3Min, lblRow3Dash, numRow3Max, lblRow3Odds, numRow3Odds,
                numRow4Min, lblRow4Dash, numRow4Max, lblRow4Odds, numRow4Odds
            });
            
            // Group 2: 大双-小单超 设置
            this.grpBigDoubleLittleSingle = new System.Windows.Forms.GroupBox();
            this.grpBigDoubleLittleSingle.Text = "大双-小单超13 14设置";
            this.grpBigDoubleLittleSingle.Location = new System.Drawing.Point(5, 152);
            this.grpBigDoubleLittleSingle.Size = new System.Drawing.Size(200, 55);
            
            this.lblBDLS1 = CreateLabel("总注超", 10, 20, 42);
            this.numBDLS1 = CreateNumeric(52, 18, 45, 0);
            this.lblBDLS1Odds = CreateLabel("赔率", 100, 20, 30);
            this.numBDLS1Odds = CreateNumeric(130, 18, 35, 0);
            
            this.lblBDLS2 = CreateLabel("总注超", 10, 38, 42);
            this.numBDLS2 = CreateNumeric(52, 36, 45, 1000);
            this.lblBDLS2Odds = CreateLabel("赔率", 100, 38, 30);
            this.numBDLS2Odds = CreateNumeric(130, 36, 35, 0);
            
            this.grpBigDoubleLittleSingle.Controls.AddRange(new System.Windows.Forms.Control[] {
                lblBDLS1, numBDLS1, lblBDLS1Odds, numBDLS1Odds,
                lblBDLS2, numBDLS2, lblBDLS2Odds, numBDLS2Odds
            });
            
            // Group 3: 大单-小双超 设置
            this.grpBigSingleLittleDouble = new System.Windows.Forms.GroupBox();
            this.grpBigSingleLittleDouble.Text = "大单-小双超12 15设置";
            this.grpBigSingleLittleDouble.Location = new System.Drawing.Point(5, 209);
            this.grpBigSingleLittleDouble.Size = new System.Drawing.Size(200, 55);
            
            this.lblBSLD1 = CreateLabel("总注超", 10, 20, 42);
            this.numBSLD1 = CreateNumeric(52, 18, 50, 50000);
            this.lblBSLD1Odds = CreateLabel("赔率", 105, 20, 30);
            this.numBSLD1Odds = CreateNumeric(135, 18, 35, 0);
            
            this.lblBSLD2 = CreateLabel("总注超", 10, 38, 42);
            this.numBSLD2 = CreateNumeric(52, 36, 50, 100000);
            this.lblBSLD2Odds = CreateLabel("赔率", 105, 38, 30);
            this.numBSLD2Odds = CreateNumeric(135, 36, 35, 0);
            
            this.grpBigSingleLittleDouble.Controls.AddRange(new System.Windows.Forms.Control[] {
                lblBSLD1, numBSLD1, lblBSLD1Odds, numBSLD1Odds,
                lblBSLD2, numBSLD2, lblBSLD2Odds, numBSLD2Odds
            });
            
            // Group 4: 豹/顺/对子
            this.grpLeopardSequencePair = new System.Windows.Forms.GroupBox();
            this.grpLeopardSequencePair.Text = "豹/顺/对子";
            this.grpLeopardSequencePair.Location = new System.Drawing.Point(5, 266);
            this.grpLeopardSequencePair.Size = new System.Drawing.Size(200, 200);
            
            this.chkLSPSwitch = new System.Windows.Forms.CheckBox();
            this.chkLSPSwitch.Text = "开/关";
            this.chkLSPSwitch.Location = new System.Drawing.Point(10, 18);
            this.chkLSPSwitch.Size = new System.Drawing.Size(55, 18);
            this.chkLSPSwitch.Checked = true;
            
            this.chkPairReturn = new System.Windows.Forms.CheckBox();
            this.chkPairReturn.Text = "对子回本";
            this.chkPairReturn.Location = new System.Drawing.Point(10, 36);
            this.chkPairReturn.Size = new System.Drawing.Size(75, 18);
            this.chkPairReturn.Checked = true;
            
            this.chkSequenceReturn = new System.Windows.Forms.CheckBox();
            this.chkSequenceReturn.Text = "顺子回本";
            this.chkSequenceReturn.Location = new System.Drawing.Point(90, 36);
            this.chkSequenceReturn.Size = new System.Drawing.Size(75, 18);
            this.chkSequenceReturn.Checked = true;
            
            this.chkLeopardReturn = new System.Windows.Forms.CheckBox();
            this.chkLeopardReturn.Text = "豹子回本";
            this.chkLeopardReturn.Location = new System.Drawing.Point(10, 54);
            this.chkLeopardReturn.Size = new System.Drawing.Size(75, 18);
            
            this.chkLeopardKill = new System.Windows.Forms.CheckBox();
            this.chkLeopardKill.Text = "豹子通杀";
            this.chkLeopardKill.Location = new System.Drawing.Point(90, 54);
            this.chkLeopardKill.Size = new System.Drawing.Size(75, 18);
            
            this.chkHalfMixed = new System.Windows.Forms.CheckBox();
            this.chkHalfMixed.Text = "半顺 杂 开/关";
            this.chkHalfMixed.Location = new System.Drawing.Point(10, 72);
            this.chkHalfMixed.Size = new System.Drawing.Size(100, 18);
            
            this.chk09Return = new System.Windows.Forms.CheckBox();
            this.chk09Return.Text = "0, 9回本";
            this.chk09Return.Location = new System.Drawing.Point(10, 90);
            this.chk09Return.Size = new System.Drawing.Size(75, 18);
            
            this.chk1314Return = new System.Windows.Forms.CheckBox();
            this.chk1314Return.Text = "1314对/顺/豹子回本";
            this.chk1314Return.Location = new System.Drawing.Point(10, 108);
            this.chk1314Return.Size = new System.Drawing.Size(140, 18);
            
            this.chkNumReturn = new System.Windows.Forms.CheckBox();
            this.chkNumReturn.Text = "数字开对/顺/豹子回本";
            this.chkNumReturn.Location = new System.Drawing.Point(10, 126);
            this.chkNumReturn.Size = new System.Drawing.Size(145, 18);
            
            this.chkNum1314Return = new System.Windows.Forms.CheckBox();
            this.chkNum1314Return.Text = "数字开1314对/顺/豹子回本";
            this.chkNum1314Return.Location = new System.Drawing.Point(10, 144);
            this.chkNum1314Return.Size = new System.Drawing.Size(170, 18);
            
            this.chkExtremeReturn = new System.Windows.Forms.CheckBox();
            this.chkExtremeReturn.Text = "极数开对/顺/豹子回本";
            this.chkExtremeReturn.Location = new System.Drawing.Point(10, 162);
            this.chkExtremeReturn.Size = new System.Drawing.Size(145, 18);
            
            this.chk1314PairReturn = new System.Windows.Forms.CheckBox();
            this.chk1314PairReturn.Text = "开1314，中对子回本";
            this.chk1314PairReturn.Location = new System.Drawing.Point(10, 180);
            this.chk1314PairReturn.Size = new System.Drawing.Size(135, 18);
            this.chk1314PairReturn.Checked = true;
            
            this.grpLeopardSequencePair.Controls.AddRange(new System.Windows.Forms.Control[] {
                chkLSPSwitch, chkPairReturn, chkSequenceReturn, chkLeopardReturn, chkLeopardKill,
                chkHalfMixed, chk09Return, chk1314Return, chkNumReturn, chkNum1314Return,
                chkExtremeReturn, chk1314PairReturn
            });
            
            // Checkbox: 890, 910算顺子
            this.chk890910Sequence = new System.Windows.Forms.CheckBox();
            this.chk890910Sequence.Text = "890, 910算顺子";
            this.chk890910Sequence.Location = new System.Drawing.Point(10, 468);
            this.chk890910Sequence.Size = new System.Drawing.Size(120, 18);
            this.chk890910Sequence.Checked = true;
            
            // ===== Middle Column =====
            // Group 5: 超无视
            this.grpIgnoreOver = new System.Windows.Forms.GroupBox();
            this.grpIgnoreOver.Text = "超无视 (13-14记无视回本)";
            this.grpIgnoreOver.Location = new System.Drawing.Point(210, 5);
            this.grpIgnoreOver.Size = new System.Drawing.Size(180, 95);
            
            this.rbIgnoreSumBet = new System.Windows.Forms.RadioButton();
            this.rbIgnoreSumBet.Text = "算总注";
            this.rbIgnoreSumBet.Location = new System.Drawing.Point(10, 18);
            this.rbIgnoreSumBet.Size = new System.Drawing.Size(65, 18);
            this.rbIgnoreSumBet.Checked = true;
            
            this.rbIgnoreSingleBet = new System.Windows.Forms.RadioButton();
            this.rbIgnoreSingleBet.Text = "算单注";
            this.rbIgnoreSingleBet.Location = new System.Drawing.Point(80, 18);
            this.rbIgnoreSingleBet.Size = new System.Drawing.Size(65, 18);
            
            this.lblIgnoreAmount = CreateLabel("金额超", 10, 40, 42);
            this.numIgnoreAmount = CreateNumeric(52, 38, 55, 0);
            this.lblIgnoreText = CreateLabel("记超无视", 110, 40, 60);
            
            this.chkKillDoubleIgnore = new System.Windows.Forms.CheckBox();
            this.chkKillDoubleIgnore.Text = "杀双多组对压不记超无视";
            this.chkKillDoubleIgnore.Location = new System.Drawing.Point(10, 58);
            this.chkKillDoubleIgnore.Size = new System.Drawing.Size(165, 18);
            
            this.chkNo1314Odds = new System.Windows.Forms.CheckBox();
            this.chkNo1314Odds.Text = "无13 14赔率";
            this.chkNo1314Odds.Location = new System.Drawing.Point(10, 76);
            this.chkNo1314Odds.Size = new System.Drawing.Size(100, 18);
            
            this.grpIgnoreOver.Controls.AddRange(new System.Windows.Forms.Control[] {
                rbIgnoreSumBet, rbIgnoreSingleBet, lblIgnoreAmount, numIgnoreAmount, lblIgnoreText,
                chkKillDoubleIgnore, chkNo1314Odds
            });
            
            // Odds settings panel (middle)
            this.pnlOddsSettings = new System.Windows.Forms.Panel();
            this.pnlOddsSettings.Location = new System.Drawing.Point(210, 102);
            this.pnlOddsSettings.Size = new System.Drawing.Size(180, 310);
            
            int oddsY = 0;
            this.lblOdds1 = CreateLabel("大小单双赔率", 0, oddsY, 85);
            this.numOdds1 = CreateNumericDecimal(90, oddsY - 2, 45, 1.8m);
            oddsY += 22;
            
            this.lblOdds2 = CreateLabel("大单小双赔率", 0, oddsY, 85);
            this.numOdds2 = CreateNumericDecimal(90, oddsY - 2, 45, 5);
            oddsY += 22;
            
            this.lblOdds3 = CreateLabel("大双小单赔率", 0, oddsY, 85);
            this.numOdds3 = CreateNumericDecimal(90, oddsY - 2, 45, 5);
            oddsY += 22;
            
            this.lblOdds4 = CreateLabel("极大极小赔率", 0, oddsY, 85);
            this.numOdds4 = CreateNumericDecimal(90, oddsY - 2, 45, 0);
            oddsY += 22;
            
            this.lblOdds5 = CreateLabel("特码数字赔率", 0, oddsY, 85);
            this.numOdds5 = CreateNumericDecimal(90, oddsY - 2, 45, 9);
            oddsY += 22;
            
            this.lblOdds6 = CreateLabel("对子赔率", 0, oddsY, 85);
            this.numOdds6 = CreateNumericDecimal(90, oddsY - 2, 45, 2);
            oddsY += 22;
            
            this.lblOdds7 = CreateLabel("顺子赔率", 0, oddsY, 85);
            this.numOdds7 = CreateNumericDecimal(90, oddsY - 2, 45, 5);
            oddsY += 22;
            
            this.lblOdds8 = CreateLabel("半顺赔率", 0, oddsY, 85);
            this.numOdds8 = CreateNumericDecimal(90, oddsY - 2, 45, 0);
            oddsY += 22;
            
            this.lblOdds9 = CreateLabel("豹子赔率", 0, oddsY, 85);
            this.numOdds9 = CreateNumericDecimal(90, oddsY - 2, 45, 49);
            oddsY += 22;
            
            this.lblOdds10 = CreateLabel("杂赔率", 0, oddsY, 85);
            this.numOdds10 = CreateNumericDecimal(90, oddsY - 2, 45, 0);
            oddsY += 22;
            
            this.lblOdds11 = CreateLabel("大边赔率", 0, oddsY, 85);
            this.numOdds11 = CreateNumericDecimal(90, oddsY - 2, 45, 0);
            oddsY += 22;
            
            this.lblOdds12 = CreateLabel("小边赔率", 0, oddsY, 85);
            this.numOdds12 = CreateNumericDecimal(90, oddsY - 2, 45, 0);
            oddsY += 22;
            
            this.lblOdds13 = CreateLabel("中赔率", 0, oddsY, 85);
            this.numOdds13 = CreateNumericDecimal(90, oddsY - 2, 45, 0);
            oddsY += 22;
            
            this.lblOdds14 = CreateLabel("边历史", 0, oddsY, 85);
            this.numOdds14 = CreateNumericDecimal(90, oddsY - 2, 45, 0);
            
            this.pnlOddsSettings.Controls.AddRange(new System.Windows.Forms.Control[] {
                lblOdds1, numOdds1, lblOdds2, numOdds2, lblOdds3, numOdds3,
                lblOdds4, numOdds4, lblOdds5, numOdds5, lblOdds6, numOdds6,
                lblOdds7, numOdds7, lblOdds8, numOdds8, lblOdds9, numOdds9,
                lblOdds10, numOdds10, lblOdds11, numOdds11, lblOdds12, numOdds12,
                lblOdds13, numOdds13, lblOdds14, numOdds14
            });
            
            // ===== Right Column =====
            // Group 6: 极数
            this.grpExtreme = new System.Windows.Forms.GroupBox();
            this.grpExtreme.Text = "极数";
            this.grpExtreme.Location = new System.Drawing.Point(395, 5);
            this.grpExtreme.Size = new System.Drawing.Size(130, 55);
            
            this.lblExtremeMax = CreateLabel("极大", 10, 20, 28);
            this.numExtremeMax1 = CreateNumeric(40, 18, 35, 22);
            this.lblExtremeMaxDash = CreateLabel("-", 78, 20, 10);
            this.numExtremeMax2 = CreateNumeric(90, 18, 35, 27);
            
            this.lblExtremeMin = CreateLabel("极小", 10, 38, 28);
            this.numExtremeMin1 = CreateNumeric(40, 36, 35, 0);
            this.lblExtremeMinDash = CreateLabel("-", 78, 38, 10);
            this.numExtremeMin2 = CreateNumeric(90, 36, 35, 5);
            
            this.grpExtreme.Controls.AddRange(new System.Windows.Forms.Control[] {
                lblExtremeMax, numExtremeMax1, lblExtremeMaxDash, numExtremeMax2,
                lblExtremeMin, numExtremeMin1, lblExtremeMinDash, numExtremeMin2
            });
            
            // Group 7: 单独数字赔率设置
            this.grpSingleDigitOdds = new System.Windows.Forms.GroupBox();
            this.grpSingleDigitOdds.Text = "单独数字赔率设置";
            this.grpSingleDigitOdds.Location = new System.Drawing.Point(395, 62);
            this.grpSingleDigitOdds.Size = new System.Drawing.Size(210, 165);
            
            // DataGridView for digits 0-9
            this.dgvDigitOdds = new System.Windows.Forms.DataGridView();
            this.dgvDigitOdds.Location = new System.Drawing.Point(10, 18);
            this.dgvDigitOdds.Size = new System.Drawing.Size(100, 110);
            this.dgvDigitOdds.RowHeadersVisible = false;
            this.dgvDigitOdds.AllowUserToAddRows = false;
            this.dgvDigitOdds.AllowUserToDeleteRows = false;
            this.dgvDigitOdds.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvDigitOdds.BackgroundColor = System.Drawing.Color.White;
            this.dgvDigitOdds.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvDigitOdds.RowTemplate.Height = 18;
            this.colDigit = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colDigit.HeaderText = "数字";
            this.colDigit.Name = "colDigit";
            this.colDigit.Width = 40;
            this.colOddsValue = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colOddsValue.HeaderText = "赔率";
            this.colOddsValue.Name = "colOddsValue";
            this.dgvDigitOdds.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
                this.colDigit, this.colOddsValue
            });
            
            // Second column for editing
            this.lblDigit2 = CreateLabel("数字", 115, 20, 30);
            this.lblOddsVal2 = CreateLabel("赔率", 160, 20, 30);
            this.txtDigit2 = new System.Windows.Forms.TextBox();
            this.txtDigit2.Location = new System.Drawing.Point(115, 38);
            this.txtDigit2.Size = new System.Drawing.Size(35, 23);
            this.txtOddsVal2 = new System.Windows.Forms.TextBox();
            this.txtOddsVal2.Location = new System.Drawing.Point(160, 38);
            this.txtOddsVal2.Size = new System.Drawing.Size(35, 23);
            
            this.btnModifyAdd = new System.Windows.Forms.Button();
            this.btnModifyAdd.Text = "修改/添加";
            this.btnModifyAdd.Location = new System.Drawing.Point(115, 65);
            this.btnModifyAdd.Size = new System.Drawing.Size(80, 25);
            
            this.btnDeleteSelected = new System.Windows.Forms.Button();
            this.btnDeleteSelected.Text = "删除选中";
            this.btnDeleteSelected.Location = new System.Drawing.Point(115, 93);
            this.btnDeleteSelected.Size = new System.Drawing.Size(80, 25);
            
            this.chkSingleDigitOdds = new System.Windows.Forms.CheckBox();
            this.chkSingleDigitOdds.Text = "单独数字赔率";
            this.chkSingleDigitOdds.Location = new System.Drawing.Point(10, 132);
            this.chkSingleDigitOdds.Size = new System.Drawing.Size(100, 18);
            
            this.grpSingleDigitOdds.Controls.AddRange(new System.Windows.Forms.Control[] {
                dgvDigitOdds, lblDigit2, lblOddsVal2, txtDigit2, txtOddsVal2,
                btnModifyAdd, btnDeleteSelected, chkSingleDigitOdds
            });
            
            // Group 8: 特码格式-选择
            this.grpSpecialFormat = new System.Windows.Forms.GroupBox();
            this.grpSpecialFormat.Text = "特码格式-选择";
            this.grpSpecialFormat.Location = new System.Drawing.Point(395, 230);
            this.grpSpecialFormat.Size = new System.Drawing.Size(210, 105);
            
            this.lblSpecialChars = CreateLabel("特码下注字眼:", 10, 20, 80);
            this.txtSpecialChars = new System.Windows.Forms.TextBox();
            this.txtSpecialChars.Text = "操|草|点|+|*|'|T";
            this.txtSpecialChars.Location = new System.Drawing.Point(90, 18);
            this.txtSpecialChars.Size = new System.Drawing.Size(110, 23);
            
            this.rbSpecialFirst = new System.Windows.Forms.RadioButton();
            this.rbSpecialFirst.Text = "特码T金额";
            this.rbSpecialFirst.Location = new System.Drawing.Point(10, 45);
            this.rbSpecialFirst.Size = new System.Drawing.Size(85, 18);
            
            this.rbAmountFirst = new System.Windows.Forms.RadioButton();
            this.rbAmountFirst.Text = "金额T特码";
            this.rbAmountFirst.Location = new System.Drawing.Point(100, 45);
            this.rbAmountFirst.Size = new System.Drawing.Size(85, 18);
            this.rbAmountFirst.Checked = true;
            
            this.chkSmallAmountSpecial = new System.Windows.Forms.CheckBox();
            this.chkSmallAmountSpecial.Text = "以小的金额为特码(30起推荐)";
            this.chkSmallAmountSpecial.Location = new System.Drawing.Point(10, 65);
            this.chkSmallAmountSpecial.Size = new System.Drawing.Size(190, 18);
            
            this.lblMaxPayout = CreateLabel("单点最高赔付", 10, 85, 80);
            this.numMaxPayout = CreateNumeric(90, 83, 55, 5000);
            
            this.grpSpecialFormat.Controls.AddRange(new System.Windows.Forms.Control[] {
                lblSpecialChars, txtSpecialChars, rbSpecialFirst, rbAmountFirst,
                chkSmallAmountSpecial, lblMaxPayout, numMaxPayout
            });
            
            // Additional settings
            this.lblMaxDigitCount = CreateLabel("最大t数字个数", 395, 340, 85);
            this.numMaxDigitCount = CreateNumeric(485, 338, 45, 3);
            
            // Note label
            this.lblNote = new System.Windows.Forms.Label();
            this.lblNote.Text = "注：所有赔率设置都是不包含本金\n赔率0代表回本\n赔率-1代表杀";
            this.lblNote.Location = new System.Drawing.Point(395, 370);
            this.lblNote.Size = new System.Drawing.Size(200, 50);
            this.lblNote.ForeColor = System.Drawing.Color.Red;
            
            // Save button
            this.btnSave = new System.Windows.Forms.Button();
            this.btnSave.Text = "保存设置";
            this.btnSave.Location = new System.Drawing.Point(510, 420);
            this.btnSave.Size = new System.Drawing.Size(90, 30);
            
            // ===== Add all controls to form =====
            this.Controls.AddRange(new System.Windows.Forms.Control[] {
                grpSizeOddEven, grpBigDoubleLittleSingle, grpBigSingleLittleDouble,
                grpLeopardSequencePair, chk890910Sequence,
                grpIgnoreOver, pnlOddsSettings,
                grpExtreme, grpSingleDigitOdds, grpSpecialFormat,
                lblMaxDigitCount, numMaxDigitCount, lblNote, btnSave
            });
            
            this.Size = new System.Drawing.Size(620, 470);
            this.BackColor = System.Drawing.Color.White;
            this.AutoScroll = true;
            
            this.ResumeLayout(false);
        }
        
        // Helper methods
        private System.Windows.Forms.Label CreateLabel(string text, int x, int y, int width)
        {
            return new System.Windows.Forms.Label
            {
                Text = text,
                Location = new System.Drawing.Point(x, y),
                Size = new System.Drawing.Size(width, 18),
                Font = new System.Drawing.Font("Microsoft YaHei UI", 8F)
            };
        }
        
        private System.Windows.Forms.NumericUpDown CreateNumeric(int x, int y, int width, decimal value)
        {
            var num = new System.Windows.Forms.NumericUpDown();
            num.Location = new System.Drawing.Point(x, y);
            num.Size = new System.Drawing.Size(width, 23);
            num.Maximum = 9999999;
            num.Minimum = -1;
            num.Value = value;
            num.Font = new System.Drawing.Font("Microsoft YaHei UI", 8F);
            return num;
        }
        
        private System.Windows.Forms.NumericUpDown CreateNumericDecimal(int x, int y, int width, decimal value)
        {
            var num = new System.Windows.Forms.NumericUpDown();
            num.Location = new System.Drawing.Point(x, y);
            num.Size = new System.Drawing.Size(width, 23);
            num.Maximum = 999;
            num.Minimum = -1;
            num.DecimalPlaces = 1;
            num.Increment = 0.1m;
            num.Value = value;
            num.Font = new System.Drawing.Font("Microsoft YaHei UI", 8F);
            return num;
        }

        #endregion

        // Group 1: Size Odd Even
        private System.Windows.Forms.GroupBox grpSizeOddEven;
        private System.Windows.Forms.Label lblSizeTitle, lblSizeTitle2;
        private System.Windows.Forms.TextBox txtSize1314;
        private System.Windows.Forms.RadioButton rbSumBet;
        private System.Windows.Forms.RadioButton rbSingleBet;
        private System.Windows.Forms.NumericUpDown numRow1Min, numRow1Max, numRow1Odds;
        private System.Windows.Forms.NumericUpDown numRow2Min, numRow2Max, numRow2Odds;
        private System.Windows.Forms.NumericUpDown numRow3Min, numRow3Max, numRow3Odds;
        private System.Windows.Forms.NumericUpDown numRow4Min, numRow4Max, numRow4Odds;
        private System.Windows.Forms.Label lblRow1Dash, lblRow1Odds;
        private System.Windows.Forms.Label lblRow2Dash, lblRow2Odds;
        private System.Windows.Forms.Label lblRow3Dash, lblRow3Odds;
        private System.Windows.Forms.Label lblRow4Dash, lblRow4Odds;
        
        // Group 2: Big Double Little Single
        private System.Windows.Forms.GroupBox grpBigDoubleLittleSingle;
        private System.Windows.Forms.Label lblBDLS1, lblBDLS1Odds, lblBDLS2, lblBDLS2Odds;
        private System.Windows.Forms.NumericUpDown numBDLS1, numBDLS1Odds, numBDLS2, numBDLS2Odds;
        
        // Group 3: Big Single Little Double
        private System.Windows.Forms.GroupBox grpBigSingleLittleDouble;
        private System.Windows.Forms.Label lblBSLD1, lblBSLD1Odds, lblBSLD2, lblBSLD2Odds;
        private System.Windows.Forms.NumericUpDown numBSLD1, numBSLD1Odds, numBSLD2, numBSLD2Odds;
        
        // Group 4: Leopard Sequence Pair
        private System.Windows.Forms.GroupBox grpLeopardSequencePair;
        private System.Windows.Forms.CheckBox chkLSPSwitch, chkPairReturn, chkSequenceReturn;
        private System.Windows.Forms.CheckBox chkLeopardReturn, chkLeopardKill, chkHalfMixed;
        private System.Windows.Forms.CheckBox chk09Return, chk1314Return, chkNumReturn;
        private System.Windows.Forms.CheckBox chkNum1314Return, chkExtremeReturn, chk1314PairReturn;
        private System.Windows.Forms.CheckBox chk890910Sequence;
        
        // Group 5: Ignore Over
        private System.Windows.Forms.GroupBox grpIgnoreOver;
        private System.Windows.Forms.RadioButton rbIgnoreSumBet, rbIgnoreSingleBet;
        private System.Windows.Forms.Label lblIgnoreAmount, lblIgnoreText;
        private System.Windows.Forms.NumericUpDown numIgnoreAmount;
        private System.Windows.Forms.CheckBox chkKillDoubleIgnore, chkNo1314Odds;
        
        // Odds panel
        private System.Windows.Forms.Panel pnlOddsSettings;
        private System.Windows.Forms.Label lblOdds1, lblOdds2, lblOdds3, lblOdds4, lblOdds5;
        private System.Windows.Forms.Label lblOdds6, lblOdds7, lblOdds8, lblOdds9, lblOdds10;
        private System.Windows.Forms.Label lblOdds11, lblOdds12, lblOdds13, lblOdds14;
        private System.Windows.Forms.NumericUpDown numOdds1, numOdds2, numOdds3, numOdds4, numOdds5;
        private System.Windows.Forms.NumericUpDown numOdds6, numOdds7, numOdds8, numOdds9, numOdds10;
        private System.Windows.Forms.NumericUpDown numOdds11, numOdds12, numOdds13, numOdds14;
        
        // Group 6: Extreme
        private System.Windows.Forms.GroupBox grpExtreme;
        private System.Windows.Forms.Label lblExtremeMax, lblExtremeMaxDash, lblExtremeMin, lblExtremeMinDash;
        private System.Windows.Forms.NumericUpDown numExtremeMax1, numExtremeMax2, numExtremeMin1, numExtremeMin2;
        
        // Group 7: Single Digit Odds
        private System.Windows.Forms.GroupBox grpSingleDigitOdds;
        private System.Windows.Forms.DataGridView dgvDigitOdds;
        private System.Windows.Forms.DataGridViewTextBoxColumn colDigit, colOddsValue;
        private System.Windows.Forms.Label lblDigit2, lblOddsVal2;
        private System.Windows.Forms.TextBox txtDigit2, txtOddsVal2;
        private System.Windows.Forms.Button btnModifyAdd, btnDeleteSelected;
        private System.Windows.Forms.CheckBox chkSingleDigitOdds;
        
        // Group 8: Special Format
        private System.Windows.Forms.GroupBox grpSpecialFormat;
        private System.Windows.Forms.Label lblSpecialChars, lblMaxPayout;
        private System.Windows.Forms.TextBox txtSpecialChars;
        private System.Windows.Forms.RadioButton rbSpecialFirst, rbAmountFirst;
        private System.Windows.Forms.CheckBox chkSmallAmountSpecial;
        private System.Windows.Forms.NumericUpDown numMaxPayout;
        
        // Additional
        private System.Windows.Forms.Label lblMaxDigitCount, lblNote;
        private System.Windows.Forms.NumericUpDown numMaxDigitCount;
        private System.Windows.Forms.Button btnSave;
    }
}

