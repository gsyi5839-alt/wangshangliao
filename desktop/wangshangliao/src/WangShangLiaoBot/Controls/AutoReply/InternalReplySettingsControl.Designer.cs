namespace WangShangLiaoBot.Controls.AutoReply
{
    partial class InternalReplySettingsControl
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
            // Root layout
            this.tblRoot = new System.Windows.Forms.TableLayoutPanel();
            this.pnlBottom = new System.Windows.Forms.FlowLayoutPanel();

            // ===== Left Panel Controls =====
            this.panelLeft = new System.Windows.Forms.Panel();
            this.tblLeft = new System.Windows.Forms.TableLayoutPanel();
            
            // 下注显示
            this.lblBetDisplay = new System.Windows.Forms.Label();
            this.txtBetDisplay = new System.Windows.Forms.TextBox();
            
            // 取消下注
            this.lblCancelBet = new System.Windows.Forms.Label();
            this.txtCancelBet = new System.Windows.Forms.TextBox();
            
            // 模糊提醒
            this.lblFuzzyRemind = new System.Windows.Forms.Label();
            this.txtFuzzyRemind = new System.Windows.Forms.TextBox();
            
            // 攻击上分有效
            this.lblAttackValid = new System.Windows.Forms.Label();
            this.txtAttackValid = new System.Windows.Forms.TextBox();
            
            // 下注不能下分
            this.lblBetNoDown = new System.Windows.Forms.Label();
            this.txtBetNoDown = new System.Windows.Forms.TextBox();
            
            // 下注不能下分(2)
            this.lblBetNoDown2 = new System.Windows.Forms.Label();
            this.txtBetNoDown2 = new System.Windows.Forms.TextBox();
            
            // 下分正在处理
            this.lblDownProcessing = new System.Windows.Forms.Label();
            this.txtDownProcessing = new System.Windows.Forms.TextBox();
            
            // 已封盘未处理
            this.lblSealedUnprocessed = new System.Windows.Forms.Label();
            this.txtSealedUnprocessed = new System.Windows.Forms.TextBox();
            
            // 取消托管成功
            this.lblCancelTrustee = new System.Windows.Forms.Label();
            this.txtCancelTrustee = new System.Windows.Forms.TextBox();
            
            // 禁止点09
            this.lblForbid09 = new System.Windows.Forms.Label();
            this.txtForbid09 = new System.Windows.Forms.TextBox();
            
            // 进群/群规
            this.lblGroupRulesKeyword = new System.Windows.Forms.Label();
            this.txtGroupRulesKeyword = new System.Windows.Forms.TextBox();
            this.lblGroupRules = new System.Windows.Forms.Label();
            this.txtGroupRules = new System.Windows.Forms.TextBox();
            
            // ===== Right Panel Controls =====
            this.panelRight = new System.Windows.Forms.Panel();
            this.tblRight = new System.Windows.Forms.TableLayoutPanel();
            
            // 上下分 Group
            this.grpUpDown = new System.Windows.Forms.GroupBox();
            this.tblUpDown = new System.Windows.Forms.TableLayoutPanel();
            this.lblUpDownMin = new System.Windows.Forms.Label();
            this.txtUpDownMin = new System.Windows.Forms.TextBox();
            this.lblUpDownMin2 = new System.Windows.Forms.Label();
            this.txtUpDownMin2 = new System.Windows.Forms.TextBox();
            this.lblUpDownMax = new System.Windows.Forms.Label();
            this.txtUpDownMax = new System.Windows.Forms.TextBox();
            this.lblUpDownPlayer = new System.Windows.Forms.Label();
            this.txtUpDownPlayer = new System.Windows.Forms.TextBox();
            
            // 个人数据反馈 Group
            this.grpPersonalData = new System.Windows.Forms.GroupBox();
            this.tblPersonalData = new System.Windows.Forms.TableLayoutPanel();
            this.lblDataKeyword = new System.Windows.Forms.Label();
            this.txtDataKeyword = new System.Windows.Forms.TextBox();
            this.lblDataBill = new System.Windows.Forms.Label();
            this.txtDataBill = new System.Windows.Forms.TextBox();
            this.lblDataNoAttack = new System.Windows.Forms.Label();
            this.txtDataNoAttack = new System.Windows.Forms.TextBox();
            this.lblDataHasAttack = new System.Windows.Forms.Label();
            this.txtDataHasAttack = new System.Windows.Forms.TextBox();
            
            // 私聊尾巴 Group
            this.grpPrivateTail = new System.Windows.Forms.GroupBox();
            this.tblPrivateTail = new System.Windows.Forms.TableLayoutPanel();
            this.lblTailUnsealed = new System.Windows.Forms.Label();
            this.txtTailUnsealed = new System.Windows.Forms.TextBox();
            this.lblTailSealed = new System.Windows.Forms.Label();
            this.txtTailSealed = new System.Windows.Forms.TextBox();
            
            // Save button
            this.btnSave = new System.Windows.Forms.Button();
            
            this.SuspendLayout();

            // ==========================================
            // Root layout (2 columns + bottom save row)
            // ==========================================
            this.tblRoot.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tblRoot.ColumnCount = 2;
            this.tblRoot.RowCount = 2;
            this.tblRoot.Padding = new System.Windows.Forms.Padding(6);
            this.tblRoot.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblRoot.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblRoot.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tblRoot.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));

            // Bottom bar (save button right aligned)
            this.pnlBottom.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlBottom.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.pnlBottom.WrapContents = false;
            this.pnlBottom.Padding = new System.Windows.Forms.Padding(0, 6, 0, 0);

            this.btnSave.Text = "保存设置";
            this.btnSave.Size = new System.Drawing.Size(100, 30);
            this.btnSave.Margin = new System.Windows.Forms.Padding(0, 0, 0, 0);
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            this.pnlBottom.Controls.Add(this.btnSave);

            // ==========================================
            // Left panel (table)
            // ==========================================
            this.panelLeft.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelLeft.Padding = new System.Windows.Forms.Padding(0);
            this.panelLeft.Controls.Add(this.tblLeft);

            this.tblLeft.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tblLeft.ColumnCount = 2;
            this.tblLeft.RowCount = 12;
            this.tblLeft.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 75F));
            this.tblLeft.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tblLeft.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F)); // 下注显示
            this.tblLeft.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F)); // 取消下注
            this.tblLeft.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F)); // 模糊提醒
            this.tblLeft.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F)); // 攻击上分有效
            this.tblLeft.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F)); // 下注不能下分
            this.tblLeft.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F)); // 下注不能下分2
            this.tblLeft.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F)); // 下分正在处理
            this.tblLeft.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F)); // 已封盘未处理
            this.tblLeft.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F)); // 取消托管成功
            this.tblLeft.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F)); // 禁止点09
            this.tblLeft.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F)); // 群规关键词
            this.tblLeft.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));  // 进群/群规

            ConfigureLabel(this.lblBetDisplay, "下注显示");
            ConfigureTextBox(this.txtBetDisplay);
            this.txtBetDisplay.Text = "[昵称]";
            this.tblLeft.Controls.Add(this.lblBetDisplay, 0, 0);
            this.tblLeft.Controls.Add(this.txtBetDisplay, 1, 0);

            ConfigureLabel(this.lblCancelBet, "取消下注");
            ConfigureTextBox(this.txtCancelBet);
            this.txtCancelBet.Text = "[昵称] Qu Xiao";
            this.tblLeft.Controls.Add(this.lblCancelBet, 0, 1);
            this.tblLeft.Controls.Add(this.txtCancelBet, 1, 1);

            ConfigureLabel(this.lblFuzzyRemind, "模糊提醒");
            ConfigureTextBox(this.txtFuzzyRemind);
            this.txtFuzzyRemind.Text = "[昵称] Yu Bu Zu Shang Fen Hou Lu Qu";
            this.tblLeft.Controls.Add(this.lblFuzzyRemind, 0, 2);
            this.tblLeft.Controls.Add(this.txtFuzzyRemind, 1, 2);

            ConfigureLabel(this.lblAttackValid, "攻击上分\r\n有效");
            ConfigureTextBox(this.txtAttackValid);
            this.tblLeft.Controls.Add(this.lblAttackValid, 0, 3);
            this.tblLeft.Controls.Add(this.txtAttackValid, 1, 3);

            ConfigureLabel(this.lblBetNoDown, "下注不能\r\n下分");
            ConfigureTextBox(this.txtBetNoDown);
            this.tblLeft.Controls.Add(this.lblBetNoDown, 0, 4);
            this.tblLeft.Controls.Add(this.txtBetNoDown, 1, 4);

            ConfigureLabel(this.lblBetNoDown2, "下注不能\r\n下分");
            ConfigureTextBox(this.txtBetNoDown2);
            this.tblLeft.Controls.Add(this.lblBetNoDown2, 0, 5);
            this.tblLeft.Controls.Add(this.txtBetNoDown2, 1, 5);

            ConfigureLabel(this.lblDownProcessing, "下分正在\r\n处理");
            ConfigureTextBox(this.txtDownProcessing);
            this.txtDownProcessing.Text = "[昵称] Shao Deng";
            this.tblLeft.Controls.Add(this.lblDownProcessing, 0, 6);
            this.tblLeft.Controls.Add(this.txtDownProcessing, 1, 6);

            ConfigureLabel(this.lblSealedUnprocessed, "已封盘\r\n未处理");
            ConfigureTextBox(this.txtSealedUnprocessed);
            this.txtSealedUnprocessed.Text = "[昵称]慢作业结束攻击要快！姿势要帅！";
            this.tblLeft.Controls.Add(this.lblSealedUnprocessed, 0, 7);
            this.tblLeft.Controls.Add(this.txtSealedUnprocessed, 1, 7);

            ConfigureLabel(this.lblCancelTrustee, "取消托管\r\n成功");
            ConfigureTextBox(this.txtCancelTrustee);
            this.tblLeft.Controls.Add(this.lblCancelTrustee, 0, 8);
            this.tblLeft.Controls.Add(this.txtCancelTrustee, 1, 8);

            ConfigureLabel(this.lblForbid09, "禁止点09");
            ConfigureTextBox(this.txtForbid09);
            this.tblLeft.Controls.Add(this.lblForbid09, 0, 9);
            this.tblLeft.Controls.Add(this.txtForbid09, 1, 9);

            ConfigureLabel(this.lblGroupRulesKeyword, "群规关键词");
            ConfigureTextBox(this.txtGroupRulesKeyword);
            this.txtGroupRulesKeyword.Text = "群规|规则|新人|福利|玩法";
            this.tblLeft.Controls.Add(this.lblGroupRulesKeyword, 0, 10);
            this.tblLeft.Controls.Add(this.txtGroupRulesKeyword, 1, 10);

            ConfigureLabel(this.lblGroupRules, "进群\r\n群规");
            this.lblGroupRules.TextAlign = System.Drawing.ContentAlignment.TopLeft;
            this.txtGroupRules.Multiline = true;
            this.txtGroupRules.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtGroupRules.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tblLeft.Controls.Add(this.lblGroupRules, 0, 11);
            this.tblLeft.Controls.Add(this.txtGroupRules, 1, 11);

            // ==========================================
            // Right panel (groups stacked)
            // ==========================================
            this.panelRight.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelRight.Padding = new System.Windows.Forms.Padding(0);
            this.panelRight.Controls.Add(this.tblRight);

            this.tblRight.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tblRight.ColumnCount = 1;
            this.tblRight.RowCount = 4;
            this.tblRight.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tblRight.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 120F)); // 上下分
            this.tblRight.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 150F)); // 个人数据反馈
            this.tblRight.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 100F)); // 私聊尾巴
            this.tblRight.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));  // filler

            // ----- 上下分 Group -----
            this.grpUpDown.Text = "上下分";
            this.grpUpDown.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpUpDown.Padding = new System.Windows.Forms.Padding(8, 12, 8, 8);
            this.grpUpDown.Controls.Add(this.tblUpDown);

            ConfigureInnerTable(this.tblUpDown, 4);
            ConfigureLabel(this.lblUpDownMin, "下分最少次数");
            ConfigureTextBox(this.txtUpDownMin);
            ConfigureLabel(this.lblUpDownMin2, "");
            ConfigureTextBox(this.txtUpDownMin2);
            ConfigureLabel(this.lblUpDownMax, "下分一次回");
            ConfigureTextBox(this.txtUpDownMax);
            ConfigureLabel(this.lblUpDownPlayer, "客户上分回复");
            ConfigureTextBox(this.txtUpDownPlayer);

            this.tblUpDown.Controls.Add(this.lblUpDownMin, 0, 0);
            this.tblUpDown.Controls.Add(this.txtUpDownMin, 1, 0);
            this.tblUpDown.Controls.Add(this.lblUpDownMin2, 0, 1);
            this.tblUpDown.Controls.Add(this.txtUpDownMin2, 1, 1);
            this.tblUpDown.Controls.Add(this.lblUpDownMax, 0, 2);
            this.tblUpDown.Controls.Add(this.txtUpDownMax, 1, 2);
            this.tblUpDown.Controls.Add(this.lblUpDownPlayer, 0, 3);
            this.tblUpDown.Controls.Add(this.txtUpDownPlayer, 1, 3);

            this.tblRight.Controls.Add(this.grpUpDown, 0, 0);

            // ----- 个人数据反馈 Group -----
            this.grpPersonalData.Text = "个人数据反馈";
            this.grpPersonalData.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpPersonalData.Padding = new System.Windows.Forms.Padding(8, 12, 8, 8);
            this.grpPersonalData.Controls.Add(this.tblPersonalData);

            ConfigureInnerTable(this.tblPersonalData, 4);
            ConfigureLabel(this.lblDataKeyword, "关键字");
            ConfigureTextBox(this.txtDataKeyword);
            ConfigureLabel(this.lblDataBill, "账单0分");
            ConfigureTextBox(this.txtDataBill);
            ConfigureLabel(this.lblDataNoAttack, "有分无攻击");
            ConfigureTextBox(this.txtDataNoAttack);
            ConfigureLabel(this.lblDataHasAttack, "有分有攻击");
            ConfigureTextBox(this.txtDataHasAttack);

            this.tblPersonalData.Controls.Add(this.lblDataKeyword, 0, 0);
            this.tblPersonalData.Controls.Add(this.txtDataKeyword, 1, 0);
            this.tblPersonalData.Controls.Add(this.lblDataBill, 0, 1);
            this.tblPersonalData.Controls.Add(this.txtDataBill, 1, 1);
            this.tblPersonalData.Controls.Add(this.lblDataNoAttack, 0, 2);
            this.tblPersonalData.Controls.Add(this.txtDataNoAttack, 1, 2);
            this.tblPersonalData.Controls.Add(this.lblDataHasAttack, 0, 3);
            this.tblPersonalData.Controls.Add(this.txtDataHasAttack, 1, 3);

            this.tblRight.Controls.Add(this.grpPersonalData, 0, 1);

            // ----- 私聊尾巴 Group -----
            this.grpPrivateTail.Text = "私聊尾巴";
            this.grpPrivateTail.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpPrivateTail.Padding = new System.Windows.Forms.Padding(8, 12, 8, 8);
            this.grpPrivateTail.Controls.Add(this.tblPrivateTail);

            ConfigureInnerTable(this.tblPrivateTail, 2);
            ConfigureLabel(this.lblTailUnsealed, "未封盘尾巴");
            ConfigureTextBox(this.txtTailUnsealed);
            ConfigureLabel(this.lblTailSealed, "已封盘尾巴");
            ConfigureTextBox(this.txtTailSealed);

            this.tblPrivateTail.Controls.Add(this.lblTailUnsealed, 0, 0);
            this.tblPrivateTail.Controls.Add(this.txtTailUnsealed, 1, 0);
            this.tblPrivateTail.Controls.Add(this.lblTailSealed, 0, 1);
            this.tblPrivateTail.Controls.Add(this.txtTailSealed, 1, 1);

            this.tblRight.Controls.Add(this.grpPrivateTail, 0, 2);

            // ==========================================
            // Main Control Setup
            // ==========================================
            this.tblRoot.Controls.Add(this.panelLeft, 0, 0);
            this.tblRoot.Controls.Add(this.panelRight, 1, 0);
            this.tblRoot.Controls.Add(this.pnlBottom, 0, 1);
            this.tblRoot.SetColumnSpan(this.pnlBottom, 2);
            this.Controls.Add(this.tblRoot);

            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Name = "InternalReplySettingsControl";
            this.Dock = System.Windows.Forms.DockStyle.Fill;

            this.ResumeLayout(false);
        }

        private static void ConfigureLabel(System.Windows.Forms.Label label, string text)
        {
            label.AutoSize = false;
            label.Dock = System.Windows.Forms.DockStyle.Fill;
            label.Text = text;
            label.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            label.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
        }

        private static void ConfigureTextBox(System.Windows.Forms.TextBox textBox)
        {
            textBox.Dock = System.Windows.Forms.DockStyle.Fill;
            textBox.Margin = new System.Windows.Forms.Padding(0, 3, 0, 3);
        }

        private static void ConfigureInnerTable(System.Windows.Forms.TableLayoutPanel table, int rows)
        {
            table.Dock = System.Windows.Forms.DockStyle.Fill;
            table.ColumnCount = 3;
            table.RowCount = rows;
            table.ColumnStyles.Clear();
            table.RowStyles.Clear();
            table.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 90F));
            table.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 240F));
            table.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            for (int i = 0; i < rows; i++)
                table.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
        }
        
        #endregion
        
        // Left panel controls
        private System.Windows.Forms.TableLayoutPanel tblRoot;
        private System.Windows.Forms.FlowLayoutPanel pnlBottom;
        private System.Windows.Forms.Panel panelLeft;
        private System.Windows.Forms.TableLayoutPanel tblLeft;
        private System.Windows.Forms.Label lblBetDisplay;
        private System.Windows.Forms.TextBox txtBetDisplay;
        private System.Windows.Forms.Label lblCancelBet;
        private System.Windows.Forms.TextBox txtCancelBet;
        private System.Windows.Forms.Label lblFuzzyRemind;
        private System.Windows.Forms.TextBox txtFuzzyRemind;
        private System.Windows.Forms.Label lblAttackValid;
        private System.Windows.Forms.TextBox txtAttackValid;
        private System.Windows.Forms.Label lblBetNoDown;
        private System.Windows.Forms.TextBox txtBetNoDown;
        private System.Windows.Forms.Label lblBetNoDown2;
        private System.Windows.Forms.TextBox txtBetNoDown2;
        private System.Windows.Forms.Label lblDownProcessing;
        private System.Windows.Forms.TextBox txtDownProcessing;
        private System.Windows.Forms.Label lblSealedUnprocessed;
        private System.Windows.Forms.TextBox txtSealedUnprocessed;
        private System.Windows.Forms.Label lblCancelTrustee;
        private System.Windows.Forms.TextBox txtCancelTrustee;
        private System.Windows.Forms.Label lblForbid09;
        private System.Windows.Forms.TextBox txtForbid09;
        private System.Windows.Forms.Label lblGroupRulesKeyword;
        private System.Windows.Forms.TextBox txtGroupRulesKeyword;
        private System.Windows.Forms.Label lblGroupRules;
        private System.Windows.Forms.TextBox txtGroupRules;
        
        // Right panel controls
        private System.Windows.Forms.Panel panelRight;
        private System.Windows.Forms.TableLayoutPanel tblRight;
        
        // 上下分 Group
        private System.Windows.Forms.GroupBox grpUpDown;
        private System.Windows.Forms.TableLayoutPanel tblUpDown;
        private System.Windows.Forms.Label lblUpDownMin;
        private System.Windows.Forms.TextBox txtUpDownMin;
        private System.Windows.Forms.Label lblUpDownMin2;
        private System.Windows.Forms.TextBox txtUpDownMin2;
        private System.Windows.Forms.Label lblUpDownMax;
        private System.Windows.Forms.TextBox txtUpDownMax;
        private System.Windows.Forms.Label lblUpDownPlayer;
        private System.Windows.Forms.TextBox txtUpDownPlayer;
        
        // 个人数据反馈 Group
        private System.Windows.Forms.GroupBox grpPersonalData;
        private System.Windows.Forms.TableLayoutPanel tblPersonalData;
        private System.Windows.Forms.Label lblDataKeyword;
        private System.Windows.Forms.TextBox txtDataKeyword;
        private System.Windows.Forms.Label lblDataBill;
        private System.Windows.Forms.TextBox txtDataBill;
        private System.Windows.Forms.Label lblDataNoAttack;
        private System.Windows.Forms.TextBox txtDataNoAttack;
        private System.Windows.Forms.Label lblDataHasAttack;
        private System.Windows.Forms.TextBox txtDataHasAttack;
        
        // 私聊尾巴 Group
        private System.Windows.Forms.GroupBox grpPrivateTail;
        private System.Windows.Forms.TableLayoutPanel tblPrivateTail;
        private System.Windows.Forms.Label lblTailUnsealed;
        private System.Windows.Forms.TextBox txtTailUnsealed;
        private System.Windows.Forms.Label lblTailSealed;
        private System.Windows.Forms.TextBox txtTailSealed;
        
        // Save button
        private System.Windows.Forms.Button btnSave;
    }
}

