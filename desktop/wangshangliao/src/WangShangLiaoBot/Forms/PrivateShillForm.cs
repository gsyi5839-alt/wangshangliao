using System;
using System.Drawing;
using System.Windows.Forms;
using WangShangLiaoBot.Services;

namespace WangShangLiaoBot.Forms
{
    /// <summary>
    /// 私聊版托设置窗口
    /// </summary>
    public sealed class PrivateShillForm : Form
    {
        // Top settings
        private CheckBox chkEnabled;
        private Label lblAfterLottery;
        private NumericUpDown nudAfterLotteryDelay;
        private Label lblAfterLotterySuffix;
        private Label lblBeforeSeal;
        private TextBox txtBeforeSealTime;
        private Label lblBeforeSealSuffix;
        
        // Left: Shill list
        private Label lblShillListTitle;
        private TextBox txtShillList;
        
        // Right: Bet settings
        private Label lblBetSettings;
        private NumericUpDown nudBetRange1Min, nudBetRange1Max;
        private TextBox txtBetRange1;
        private NumericUpDown nudBetRange2Min, nudBetRange2Max;
        private TextBox txtBetRange2;
        private NumericUpDown nudBetRange3Min, nudBetRange3Max;
        private TextBox txtBetRange3;
        private NumericUpDown nudBetRange4Min, nudBetRange4Max;
        private TextBox txtBetRange4;
        
        // Score settings
        private Label lblScoreSettings;
        private Label lblScoreLowPrefix;
        private NumericUpDown nudScoreLowThreshold;
        private Label lblScoreLowSuffix;
        private TextBox txtScoreLowMsg;
        private Label lblScoreHighPrefix;
        private NumericUpDown nudScoreHighThreshold;
        private Label lblScoreHighSuffix;
        private TextBox txtScoreHighMsg;
        
        // Speed settings
        private Label lblSpeedSettings;
        private NumericUpDown nudSpeedMin, nudSpeedMax;
        private Label lblSpeedSuffix;
        
        // Save button
        private Button btnSave;

        public PrivateShillForm()
        {
            InitializeForm();
            InitializeUI();
            LoadSettings();
        }

        private void InitializeForm()
        {
            Text = "私聊版托";
            Size = new Size(580, 480);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            Font = new Font("Microsoft YaHei UI", 9F);
        }

        private void InitializeUI()
        {
            SuspendLayout();

            int y = 15;

            // Enable switch
            chkEnabled = new CheckBox
            {
                Text = "私聊版托开关(开启后艾特会变成昵称发送)",
                Location = new Point(15, y),
                Size = new Size(350, 20)
            };
            Controls.Add(chkEnabled);
            y += 30;

            // After lottery delay
            lblAfterLottery = new Label
            {
                Text = "开奖后",
                Location = new Point(15, y + 3),
                AutoSize = true
            };
            Controls.Add(lblAfterLottery);

            nudAfterLotteryDelay = new NumericUpDown
            {
                Location = new Point(60, y),
                Size = new Size(60, 23),
                Minimum = 1,
                Maximum = 120,
                Value = 20
            };
            Controls.Add(nudAfterLotteryDelay);

            lblAfterLotterySuffix = new Label
            {
                Text = "秒内不下注",
                Location = new Point(125, y + 3),
                AutoSize = true
            };
            Controls.Add(lblAfterLotterySuffix);
            y += 28;

            // Before seal time
            lblBeforeSeal = new Label
            {
                Text = "封盘前",
                Location = new Point(15, y + 3),
                AutoSize = true
            };
            Controls.Add(lblBeforeSeal);

            txtBeforeSealTime = new TextBox
            {
                Location = new Point(60, y),
                Size = new Size(60, 23),
                Text = "wan20"
            };
            Controls.Add(txtBeforeSealTime);

            lblBeforeSealSuffix = new Label
            {
                Text = "秒停止下注",
                Location = new Point(125, y + 3),
                AutoSize = true
            };
            Controls.Add(lblBeforeSealSuffix);
            y += 35;

            // Shill list (left)
            lblShillListTitle = new Label
            {
                Text = "一行一个托",
                Location = new Point(15, y),
                AutoSize = true
            };
            Controls.Add(lblShillListTitle);

            txtShillList = new TextBox
            {
                Location = new Point(15, y + 20),
                Size = new Size(100, 280),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };
            Controls.Add(txtShillList);

            // Bet settings (right)
            int rightX = 130;
            lblBetSettings = new Label
            {
                Text = "下注设置:",
                Location = new Point(rightX, y),
                AutoSize = true
            };
            Controls.Add(lblBetSettings);

            y += 22;
            CreateBetRangeRow(rightX, ref y, out nudBetRange1Min, out nudBetRange1Max, out txtBetRange1, 50, 400);
            CreateBetRangeRow(rightX, ref y, out nudBetRange2Min, out nudBetRange2Max, out txtBetRange2, 401, 800);
            CreateBetRangeRow(rightX, ref y, out nudBetRange3Min, out nudBetRange3Max, out txtBetRange3, 801, 1300);
            CreateBetRangeRow(rightX, ref y, out nudBetRange4Min, out nudBetRange4Max, out txtBetRange4, 1301, 10000);

            y += 5;
            // Score settings
            lblScoreSettings = new Label
            {
                Text = "上下分设置:",
                Location = new Point(rightX, y),
                AutoSize = true
            };
            Controls.Add(lblScoreSettings);
            y += 22;

            // Score low
            lblScoreLowPrefix = new Label
            {
                Text = "分数低于",
                Location = new Point(rightX, y + 3),
                AutoSize = true
            };
            Controls.Add(lblScoreLowPrefix);

            nudScoreLowThreshold = new NumericUpDown
            {
                Location = new Point(rightX + 55, y),
                Size = new Size(60, 23),
                Minimum = 0,
                Maximum = 99999,
                Value = 100
            };
            Controls.Add(nudScoreLowThreshold);

            lblScoreLowSuffix = new Label
            {
                Text = "分发送:",
                Location = new Point(rightX + 120, y + 3),
                AutoSize = true
            };
            Controls.Add(lblScoreLowSuffix);

            txtScoreLowMsg = new TextBox
            {
                Location = new Point(rightX + 170, y),
                Size = new Size(250, 23)
            };
            Controls.Add(txtScoreLowMsg);
            y += 28;

            // Score high
            lblScoreHighPrefix = new Label
            {
                Text = "分数高于",
                Location = new Point(rightX, y + 3),
                AutoSize = true
            };
            Controls.Add(lblScoreHighPrefix);

            nudScoreHighThreshold = new NumericUpDown
            {
                Location = new Point(rightX + 55, y),
                Size = new Size(60, 23),
                Minimum = 0,
                Maximum = 999999,
                Value = 5999
            };
            Controls.Add(nudScoreHighThreshold);

            lblScoreHighSuffix = new Label
            {
                Text = "分发送:",
                Location = new Point(rightX + 120, y + 3),
                AutoSize = true
            };
            Controls.Add(lblScoreHighSuffix);

            txtScoreHighMsg = new TextBox
            {
                Location = new Point(rightX + 170, y),
                Size = new Size(250, 23)
            };
            Controls.Add(txtScoreHighMsg);
            y += 35;

            // Speed settings
            lblSpeedSettings = new Label
            {
                Text = "速度设置:",
                Location = new Point(rightX, y),
                AutoSize = true
            };
            Controls.Add(lblSpeedSettings);
            y += 22;

            nudSpeedMin = new NumericUpDown
            {
                Location = new Point(rightX, y),
                Size = new Size(50, 23),
                Minimum = 1,
                Maximum = 60,
                Value = 3
            };
            Controls.Add(nudSpeedMin);

            var lblSpeedMid = new Label
            {
                Text = "到",
                Location = new Point(rightX + 55, y + 3),
                AutoSize = true
            };
            Controls.Add(lblSpeedMid);

            nudSpeedMax = new NumericUpDown
            {
                Location = new Point(rightX + 75, y),
                Size = new Size(50, 23),
                Minimum = 1,
                Maximum = 120,
                Value = 10
            };
            Controls.Add(nudSpeedMax);

            lblSpeedSuffix = new Label
            {
                Text = "秒随机选择一个托行动",
                Location = new Point(rightX + 130, y + 3),
                AutoSize = true
            };
            Controls.Add(lblSpeedSuffix);

            // Save button
            btnSave = new Button
            {
                Text = "保存设置",
                Location = new Point(460, 400),
                Size = new Size(90, 28)
            };
            btnSave.Click += BtnSave_Click;
            Controls.Add(btnSave);

            ResumeLayout(false);
        }

        private void CreateBetRangeRow(int x, ref int y, 
            out NumericUpDown nudMin, out NumericUpDown nudMax, out TextBox txtBets,
            int minDefault, int maxDefault)
        {
            nudMin = new NumericUpDown
            {
                Location = new Point(x, y),
                Size = new Size(55, 23),
                Minimum = 0,
                Maximum = 99999,
                Value = minDefault
            };
            Controls.Add(nudMin);

            var lblTo = new Label
            {
                Text = "到",
                Location = new Point(x + 58, y + 3),
                AutoSize = true
            };
            Controls.Add(lblTo);

            nudMax = new NumericUpDown
            {
                Location = new Point(x + 78, y),
                Size = new Size(60, 23),
                Minimum = 0,
                Maximum = 99999,
                Value = maxDefault
            };
            Controls.Add(nudMax);

            var lblSuffix = new Label
            {
                Text = "分下注:",
                Location = new Point(x + 142, y + 3),
                AutoSize = true
            };
            Controls.Add(lblSuffix);

            txtBets = new TextBox
            {
                Location = new Point(x + 195, y),
                Size = new Size(225, 23)
            };
            Controls.Add(txtBets);

            y += 28;
        }

        private void LoadSettings()
        {
            var s = PrivateShillService.Instance;
            chkEnabled.Checked = s.Enabled;
            nudAfterLotteryDelay.Value = ClampValue(s.AfterLotteryDelay, nudAfterLotteryDelay);
            txtBeforeSealTime.Text = s.BeforeSealTime;
            txtShillList.Text = s.ShillListText;
            
            nudBetRange1Min.Value = s.BetRange1Min;
            nudBetRange1Max.Value = s.BetRange1Max;
            txtBetRange1.Text = s.BetRange1Bets;
            
            nudBetRange2Min.Value = s.BetRange2Min;
            nudBetRange2Max.Value = s.BetRange2Max;
            txtBetRange2.Text = s.BetRange2Bets;
            
            nudBetRange3Min.Value = s.BetRange3Min;
            nudBetRange3Max.Value = s.BetRange3Max;
            txtBetRange3.Text = s.BetRange3Bets;
            
            nudBetRange4Min.Value = s.BetRange4Min;
            nudBetRange4Max.Value = s.BetRange4Max;
            txtBetRange4.Text = s.BetRange4Bets;
            
            nudScoreLowThreshold.Value = s.ScoreLowThreshold;
            txtScoreLowMsg.Text = s.ScoreLowMessages;
            nudScoreHighThreshold.Value = s.ScoreHighThreshold;
            txtScoreHighMsg.Text = s.ScoreHighMessages;
            
            nudSpeedMin.Value = ClampValue(s.SpeedMin, nudSpeedMin);
            nudSpeedMax.Value = ClampValue(s.SpeedMax, nudSpeedMax);
        }

        private decimal ClampValue(int v, NumericUpDown nud)
        {
            if (v < (int)nud.Minimum) return nud.Minimum;
            if (v > (int)nud.Maximum) return nud.Maximum;
            return v;
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            var s = PrivateShillService.Instance;
            s.Enabled = chkEnabled.Checked;
            s.AfterLotteryDelay = (int)nudAfterLotteryDelay.Value;
            s.BeforeSealTime = txtBeforeSealTime.Text.Trim();
            s.ShillListText = txtShillList.Text;
            
            s.BetRange1Min = (int)nudBetRange1Min.Value;
            s.BetRange1Max = (int)nudBetRange1Max.Value;
            s.BetRange1Bets = txtBetRange1.Text.Trim();
            
            s.BetRange2Min = (int)nudBetRange2Min.Value;
            s.BetRange2Max = (int)nudBetRange2Max.Value;
            s.BetRange2Bets = txtBetRange2.Text.Trim();
            
            s.BetRange3Min = (int)nudBetRange3Min.Value;
            s.BetRange3Max = (int)nudBetRange3Max.Value;
            s.BetRange3Bets = txtBetRange3.Text.Trim();
            
            s.BetRange4Min = (int)nudBetRange4Min.Value;
            s.BetRange4Max = (int)nudBetRange4Max.Value;
            s.BetRange4Bets = txtBetRange4.Text.Trim();
            
            s.ScoreLowThreshold = (int)nudScoreLowThreshold.Value;
            s.ScoreLowMessages = txtScoreLowMsg.Text.Trim();
            s.ScoreHighThreshold = (int)nudScoreHighThreshold.Value;
            s.ScoreHighMessages = txtScoreHighMsg.Text.Trim();
            
            s.SpeedMin = (int)nudSpeedMin.Value;
            s.SpeedMax = (int)nudSpeedMax.Value;

            MessageBox.Show("设置已保存", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}

