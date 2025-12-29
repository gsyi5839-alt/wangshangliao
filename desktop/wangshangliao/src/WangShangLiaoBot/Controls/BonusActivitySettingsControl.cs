using System;
using System.Drawing;
using System.Windows.Forms;
using WangShangLiaoBot.Services;

namespace WangShangLiaoBot.Controls
{
    /// <summary>
    /// 送分活动设置控件 - 猜数字活动
    /// </summary>
    public sealed class BonusActivitySettingsControl : UserControl
    {
        private GroupBox grpBonus;
        
        // Row 1: Guess number switch and forbidden numbers
        private CheckBox chkGuessEnabled;
        private Label lblForbidden;
        private TextBox txtForbiddenNumbers;
        
        // Row 2: Reward rules (multi-line)
        private TextBox txtRewardRules;
        
        // Description labels (red)
        private Label lblRuleDesc;
        
        // Checkbox group - Left column
        private CheckBox chkNoBigSingleSmallDouble;
        private CheckBox chkNoBigDoubleSmallSingle;
        private CheckBox chkNoSingleBet;
        private CheckBox chkSingleBetCalc;
        private CheckBox chkManualAddScore;
        
        // Checkbox group - Right column
        private CheckBox chkNoKillCombo;
        private CheckBox chkNoMultiCombo;
        private CheckBox chkNoOppositeBet;
        private CheckBox chkShowGuessResult;
        
        // Save button
        private Button btnSave;

        public BonusActivitySettingsControl()
        {
            BackColor = SystemColors.Control;
            Font = new Font("Microsoft YaHei UI", 9F);
            MinimumSize = new Size(500, 380);
            Size = new Size(550, 400);
            AutoScroll = true;
            Dock = DockStyle.Fill;
            
            InitializeUI();
            LoadSettings();
        }

        private void InitializeUI()
        {
            SuspendLayout();

            // ================= GroupBox: 账单设置 =================
            grpBonus = new GroupBox
            {
                Text = "账单设置",
                Location = new Point(10, 10),
                Size = new Size(520, 370),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };
            Controls.Add(grpBonus);

            // Row 1: Guess number switch and forbidden numbers
            chkGuessEnabled = new CheckBox
            {
                Text = "猜数字 开启/关闭",
                Location = new Point(15, 25),
                Size = new Size(130, 20)
            };
            grpBonus.Controls.Add(chkGuessEnabled);

            lblForbidden = new Label
            {
                Text = "不可猜数字:",
                Location = new Point(165, 27),
                AutoSize = true
            };
            grpBonus.Controls.Add(lblForbidden);

            txtForbiddenNumbers = new TextBox
            {
                Location = new Point(250, 24),
                Size = new Size(100, 23)
            };
            grpBonus.Controls.Add(txtForbiddenNumbers);

            // Row 2: Reward rules (multi-line)
            txtRewardRules = new TextBox
            {
                Location = new Point(15, 55),
                Size = new Size(490, 60),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };
            grpBonus.Controls.Add(txtRewardRules);

            // Description labels (red)
            lblRuleDesc = new Label
            {
                Text = "猜数字填写规则:\r\n" +
                       "如下注总额超过1000(不包含1000)猜中奖励188\r\n" +
                       "下注总额超过5000(不包含5000)猜中奖励588\r\n" +
                       "则填写 5000=588|1000=188(多个规则\"|\"隔开)支持\r\n" +
                       "一个或多个猜数字规则,请从多到少规范填写",
                Location = new Point(15, 120),
                Size = new Size(490, 80),
                ForeColor = Color.Red
            };
            grpBonus.Controls.Add(lblRuleDesc);

            // Checkbox group - Left column
            int leftX = 15;
            int rightX = 260;
            int startY = 205;
            int rowHeight = 25;

            chkNoBigSingleSmallDouble = new CheckBox
            {
                Text = "纯大单小双下注不可猜",
                Location = new Point(leftX, startY),
                Size = new Size(180, 20)
            };
            grpBonus.Controls.Add(chkNoBigSingleSmallDouble);

            chkNoKillCombo = new CheckBox
            {
                Text = "杀组合不可猜",
                Location = new Point(rightX, startY),
                Size = new Size(150, 20)
            };
            grpBonus.Controls.Add(chkNoKillCombo);

            chkNoBigDoubleSmallSingle = new CheckBox
            {
                Text = "纯大双小单下注不可猜",
                Location = new Point(leftX, startY + rowHeight),
                Size = new Size(180, 20)
            };
            grpBonus.Controls.Add(chkNoBigDoubleSmallSingle);

            chkNoMultiCombo = new CheckBox
            {
                Text = "多组合不可猜",
                Location = new Point(rightX, startY + rowHeight),
                Size = new Size(150, 20)
            };
            grpBonus.Controls.Add(chkNoMultiCombo);

            chkNoSingleBet = new CheckBox
            {
                Text = "纯单注下注不可猜",
                Location = new Point(leftX, startY + rowHeight * 2),
                Size = new Size(180, 20)
            };
            grpBonus.Controls.Add(chkNoSingleBet);

            chkNoOppositeBet = new CheckBox
            {
                Text = "相反下注不可猜",
                Location = new Point(rightX, startY + rowHeight * 2),
                Size = new Size(150, 20)
            };
            grpBonus.Controls.Add(chkNoOppositeBet);

            chkSingleBetCalc = new CheckBox
            {
                Text = "单注计算",
                Location = new Point(leftX, startY + rowHeight * 3),
                Size = new Size(150, 20)
            };
            grpBonus.Controls.Add(chkSingleBetCalc);

            chkShowGuessResult = new CheckBox
            {
                Text = "猜中显示",
                Location = new Point(rightX, startY + rowHeight * 3),
                Size = new Size(150, 20)
            };
            grpBonus.Controls.Add(chkShowGuessResult);

            chkManualAddScore = new CheckBox
            {
                Text = "手动加分",
                Location = new Point(leftX, startY + rowHeight * 4),
                Size = new Size(150, 20)
            };
            grpBonus.Controls.Add(chkManualAddScore);

            // Save button
            btnSave = new Button
            {
                Text = "保存设置",
                Location = new Point(210, 330),
                Size = new Size(100, 28)
            };
            btnSave.Click += BtnSave_Click;
            grpBonus.Controls.Add(btnSave);

            ResumeLayout(false);
        }

        private void LoadSettings()
        {
            var s = BonusActivityService.Instance;
            chkGuessEnabled.Checked = s.GuessEnabled;
            txtForbiddenNumbers.Text = s.ForbiddenNumbers;
            txtRewardRules.Text = s.RewardRules;
            chkNoBigSingleSmallDouble.Checked = s.NoBigSingleSmallDouble;
            chkNoBigDoubleSmallSingle.Checked = s.NoBigDoubleSmallSingle;
            chkNoSingleBet.Checked = s.NoSingleBet;
            chkSingleBetCalc.Checked = s.SingleBetCalc;
            chkManualAddScore.Checked = s.ManualAddScore;
            chkNoKillCombo.Checked = s.NoKillCombo;
            chkNoMultiCombo.Checked = s.NoMultiCombo;
            chkNoOppositeBet.Checked = s.NoOppositeBet;
            chkShowGuessResult.Checked = s.ShowGuessResult;
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            var s = BonusActivityService.Instance;
            s.GuessEnabled = chkGuessEnabled.Checked;
            s.ForbiddenNumbers = txtForbiddenNumbers.Text.Trim();
            s.RewardRules = txtRewardRules.Text.Trim();
            s.NoBigSingleSmallDouble = chkNoBigSingleSmallDouble.Checked;
            s.NoBigDoubleSmallSingle = chkNoBigDoubleSmallSingle.Checked;
            s.NoSingleBet = chkNoSingleBet.Checked;
            s.SingleBetCalc = chkSingleBetCalc.Checked;
            s.ManualAddScore = chkManualAddScore.Checked;
            s.NoKillCombo = chkNoKillCombo.Checked;
            s.NoMultiCombo = chkNoMultiCombo.Checked;
            s.NoOppositeBet = chkNoOppositeBet.Checked;
            s.ShowGuessResult = chkShowGuessResult.Checked;

            MessageBox.Show("设置已保存", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}

