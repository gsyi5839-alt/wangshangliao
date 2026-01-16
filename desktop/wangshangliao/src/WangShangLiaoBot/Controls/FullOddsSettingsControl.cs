using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using WangShangLiaoBot.Models.Betting;
using WangShangLiaoBot.Services.Betting;

namespace WangShangLiaoBot.Controls
{
    /// <summary>
    /// 完整赔率配置控件 - 基于招财狗的赔率系统
    /// </summary>
    public class FullOddsSettingsControl : UserControl
    {
        #region 控件

        private TabControl tabControl;
        private TabPage tabBasic;
        private TabPage tabDigit;
        private TabPage tabLimit;
        private TabPage tabDragonTiger;

        // 基础赔率
        private NumericUpDown nudSingleBetOdds;
        private NumericUpDown nudCombinationOdds;
        private NumericUpDown nudBigOddSmallEvenOdds;
        private NumericUpDown nudBigEvenSmallOddOdds;
        private NumericUpDown nudExtremeOdds;
        private NumericUpDown nudPairOdds;
        private NumericUpDown nudStraightOdds;
        private NumericUpDown nudHalfStraightOdds;
        private NumericUpDown nudLeopardOdds;
        private NumericUpDown nudMixedOdds;

        // 数字赔率
        private NumericUpDown[] nudDigitOdds = new NumericUpDown[28];

        // 限额
        private NumericUpDown nudSingleMinBet;
        private NumericUpDown nudSingleMaxBet;
        private NumericUpDown nudDigitMinBet;
        private NumericUpDown nudDigitMaxBet;
        private NumericUpDown nudLeopardMaxBet;
        private NumericUpDown nudTotalMaxBet;

        // 龙虎
        private NumericUpDown nudDragonTigerOdds;
        private TextBox txtDragonNumbers;
        private TextBox txtTigerNumbers;
        private TextBox txtLeopardDTNumbers;

        // 极值
        private NumericUpDown nudExtremeHighStart;
        private NumericUpDown nudExtremeHighEnd;
        private NumericUpDown nudExtremeLowStart;
        private NumericUpDown nudExtremeLowEnd;

        private Button btnSave;
        private Button btnReset;

        #endregion

        public FullOddsSettingsControl()
        {
            InitializeComponent();
            LoadSettings();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.Size = new Size(850, 550);

            tabControl = new TabControl
            {
                Location = new Point(10, 10),
                Size = new Size(830, 480)
            };

            CreateBasicTab();
            CreateDigitTab();
            CreateLimitTab();
            CreateDragonTigerTab();

            this.Controls.Add(tabControl);

            // 按钮
            btnSave = new Button
            {
                Text = "保存设置",
                Location = new Point(10, 500),
                Size = new Size(100, 35),
                BackColor = Color.FromArgb(46, 204, 113),
                ForeColor = Color.White
            };
            btnSave.Click += BtnSave_Click;
            this.Controls.Add(btnSave);

            btnReset = new Button
            {
                Text = "恢复默认",
                Location = new Point(120, 500),
                Size = new Size(100, 35)
            };
            btnReset.Click += BtnReset_Click;
            this.Controls.Add(btnReset);

            this.ResumeLayout(false);
        }

        private void CreateBasicTab()
        {
            tabBasic = new TabPage { Text = "基础赔率" };
            tabControl.TabPages.Add(tabBasic);

            var panel = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            tabBasic.Controls.Add(panel);

            int y = 20;
            int col1X = 20, col2X = 250, col3X = 480;

            // 第一列
            AddOddsRow(panel, "大小单双赔率:", col1X, y, out nudSingleBetOdds, 1.8m);
            AddOddsRow(panel, "组合赔率(大单等):", col1X, y + 40, out nudCombinationOdds, 3.8m);
            AddOddsRow(panel, "大单小双赔率:", col1X, y + 80, out nudBigOddSmallEvenOdds, 5m);
            AddOddsRow(panel, "大双小单赔率:", col1X, y + 120, out nudBigEvenSmallOddOdds, 5m);
            AddOddsRow(panel, "极大极小赔率:", col1X, y + 160, out nudExtremeOdds, 11m);

            // 第二列
            AddOddsRow(panel, "对子赔率:", col2X, y, out nudPairOdds, 2m);
            AddOddsRow(panel, "顺子赔率:", col2X, y + 40, out nudStraightOdds, 11m);
            AddOddsRow(panel, "半顺赔率:", col2X, y + 80, out nudHalfStraightOdds, 1.7m);
            AddOddsRow(panel, "豹子赔率:", col2X, y + 120, out nudLeopardOdds, 59m);
            AddOddsRow(panel, "杂赔率:", col2X, y + 160, out nudMixedOdds, 2.2m);

            // 极值设置
            y = 230;
            var grpExtreme = new GroupBox
            {
                Text = "极值范围设置",
                Location = new Point(col1X, y),
                Size = new Size(400, 100)
            };

            var lbl1 = new Label { Text = "极大范围:", Location = new Point(20, 30), AutoSize = true };
            nudExtremeHighStart = new NumericUpDown { Location = new Point(90, 27), Size = new Size(60, 23), Minimum = 0, Maximum = 27, Value = 22 };
            var lbl2 = new Label { Text = "至", Location = new Point(155, 30), AutoSize = true };
            nudExtremeHighEnd = new NumericUpDown { Location = new Point(175, 27), Size = new Size(60, 23), Minimum = 0, Maximum = 27, Value = 27 };

            var lbl3 = new Label { Text = "极小范围:", Location = new Point(20, 60), AutoSize = true };
            nudExtremeLowStart = new NumericUpDown { Location = new Point(90, 57), Size = new Size(60, 23), Minimum = 0, Maximum = 27, Value = 0 };
            var lbl4 = new Label { Text = "至", Location = new Point(155, 60), AutoSize = true };
            nudExtremeLowEnd = new NumericUpDown { Location = new Point(175, 57), Size = new Size(60, 23), Minimum = 0, Maximum = 27, Value = 5 };

            grpExtreme.Controls.AddRange(new Control[] { lbl1, nudExtremeHighStart, lbl2, nudExtremeHighEnd, lbl3, nudExtremeLowStart, lbl4, nudExtremeLowEnd });
            panel.Controls.Add(grpExtreme);
        }

        private void CreateDigitTab()
        {
            tabDigit = new TabPage { Text = "数字赔率(0-27)" };
            tabControl.TabPages.Add(tabDigit);

            var panel = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            tabDigit.Controls.Add(panel);

            // 默认赔率表
            decimal[] defaultOdds = { 665, 99, 49, 39, 29, 19, 16, 15, 14, 14, 13, 12, 11, 10, 10, 11, 12, 13, 14, 14, 15, 16, 19, 29, 39, 49, 99, 665 };

            int cols = 7;
            int startX = 20, startY = 20;
            int colWidth = 110, rowHeight = 50;

            for (int i = 0; i <= 27; i++)
            {
                int col = i % cols;
                int row = i / cols;
                int x = startX + col * colWidth;
                int y = startY + row * rowHeight;

                var lbl = new Label
                {
                    Text = $"数字{i}:",
                    Location = new Point(x, y + 3),
                    AutoSize = true
                };
                panel.Controls.Add(lbl);

                nudDigitOdds[i] = new NumericUpDown
                {
                    Location = new Point(x + 50, y),
                    Size = new Size(55, 23),
                    Minimum = 0,
                    Maximum = 1000,
                    DecimalPlaces = 0,
                    Value = defaultOdds[i]
                };
                panel.Controls.Add(nudDigitOdds[i]);
            }

            // 快速设置按钮
            var btnSetAll = new Button
            {
                Text = "批量设置",
                Location = new Point(startX, startY + 5 * rowHeight + 10),
                Size = new Size(80, 30)
            };
            btnSetAll.Click += (s, e) =>
            {
                var input = Microsoft.VisualBasic.Interaction.InputBox("输入赔率(应用到所有数字):", "批量设置", "10");
                if (decimal.TryParse(input, out var odds))
                {
                    for (int i = 0; i <= 27; i++)
                        nudDigitOdds[i].Value = odds;
                }
            };
            panel.Controls.Add(btnSetAll);

            var btnDefault = new Button
            {
                Text = "恢复默认",
                Location = new Point(startX + 90, startY + 5 * rowHeight + 10),
                Size = new Size(80, 30)
            };
            btnDefault.Click += (s, e) =>
            {
                for (int i = 0; i <= 27; i++)
                    nudDigitOdds[i].Value = defaultOdds[i];
            };
            panel.Controls.Add(btnDefault);
        }

        private void CreateLimitTab()
        {
            tabLimit = new TabPage { Text = "下注限额" };
            tabControl.TabPages.Add(tabLimit);

            var panel = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            tabLimit.Controls.Add(panel);

            int y = 20;

            AddLimitRow(panel, "单注下限:", 20, y, out nudSingleMinBet, 20);
            AddLimitRow(panel, "单注上限:", 200, y, out nudSingleMaxBet, 50000);

            y += 50;
            AddLimitRow(panel, "数字下限:", 20, y, out nudDigitMinBet, 20);
            AddLimitRow(panel, "数字上限:", 200, y, out nudDigitMaxBet, 20000);

            y += 50;
            AddLimitRow(panel, "豹子上限:", 20, y, out nudLeopardMaxBet, 2000);
            AddLimitRow(panel, "总额上限:", 200, y, out nudTotalMaxBet, 60000);
        }

        private void CreateDragonTigerTab()
        {
            tabDragonTiger = new TabPage { Text = "龙虎玩法" };
            tabControl.TabPages.Add(tabDragonTiger);

            var panel = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            tabDragonTiger.Controls.Add(panel);

            int y = 20;

            var lbl1 = new Label { Text = "龙虎赔率:", Location = new Point(20, y + 3), AutoSize = true };
            nudDragonTigerOdds = new NumericUpDown
            {
                Location = new Point(100, y),
                Size = new Size(80, 23),
                Minimum = 0,
                Maximum = 100,
                DecimalPlaces = 2,
                Value = 1.92m
            };
            panel.Controls.AddRange(new Control[] { lbl1, nudDragonTigerOdds });

            y += 50;
            var lbl2 = new Label { Text = "龙对应号码:", Location = new Point(20, y + 3), AutoSize = true };
            txtDragonNumbers = new TextBox
            {
                Location = new Point(100, y),
                Size = new Size(400, 23),
                Text = "00,03,06,09,12,15,18,21,24,27"
            };
            panel.Controls.AddRange(new Control[] { lbl2, txtDragonNumbers });

            y += 40;
            var lbl3 = new Label { Text = "虎对应号码:", Location = new Point(20, y + 3), AutoSize = true };
            txtTigerNumbers = new TextBox
            {
                Location = new Point(100, y),
                Size = new Size(400, 23),
                Text = "01,04,07,10,13,16,19,22,25"
            };
            panel.Controls.AddRange(new Control[] { lbl3, txtTigerNumbers });

            y += 40;
            var lbl4 = new Label { Text = "豹对应号码:", Location = new Point(20, y + 3), AutoSize = true };
            txtLeopardDTNumbers = new TextBox
            {
                Location = new Point(100, y),
                Size = new Size(400, 23),
                Text = "02,05,08,11,14,17,20,23,26"
            };
            panel.Controls.AddRange(new Control[] { lbl4, txtLeopardDTNumbers });

            y += 50;
            var note = new Label
            {
                Text = "说明: 龙虎豹根据和值对应不同的号码判定中奖。可自定义每种类型对应的和值。",
                Location = new Point(20, y),
                Size = new Size(500, 40),
                ForeColor = Color.Gray
            };
            panel.Controls.Add(note);
        }

        private void AddOddsRow(Panel panel, string label, int x, int y, out NumericUpDown nud, decimal defaultValue)
        {
            var lbl = new Label { Text = label, Location = new Point(x, y + 3), AutoSize = true };
            nud = new NumericUpDown
            {
                Location = new Point(x + 110, y),
                Size = new Size(80, 23),
                Minimum = 0,
                Maximum = 1000,
                DecimalPlaces = 2,
                Value = defaultValue
            };
            panel.Controls.AddRange(new Control[] { lbl, nud });
        }

        private void AddLimitRow(Panel panel, string label, int x, int y, out NumericUpDown nud, decimal defaultValue)
        {
            var lbl = new Label { Text = label, Location = new Point(x, y + 3), AutoSize = true };
            nud = new NumericUpDown
            {
                Location = new Point(x + 70, y),
                Size = new Size(100, 23),
                Minimum = 0,
                Maximum = 10000000,
                DecimalPlaces = 0,
                Value = defaultValue
            };
            panel.Controls.AddRange(new Control[] { lbl, nud });
        }

        private void LoadSettings()
        {
            var config = OddsService.Instance.GetConfig();

            nudSingleBetOdds.Value = config.SingleBetOdds;
            nudCombinationOdds.Value = config.CombinationOdds;
            nudBigOddSmallEvenOdds.Value = config.BigOddSmallEvenOdds;
            nudBigEvenSmallOddOdds.Value = config.BigEvenSmallOddOdds;
            nudExtremeOdds.Value = config.ExtremeOdds;
            nudPairOdds.Value = config.PairOdds;
            nudStraightOdds.Value = config.StraightOdds;
            nudHalfStraightOdds.Value = config.HalfStraightOdds;
            nudLeopardOdds.Value = config.LeopardOdds;
            nudMixedOdds.Value = config.MixedOdds;

            nudExtremeHighStart.Value = config.ExtremeHighStart;
            nudExtremeHighEnd.Value = config.ExtremeHighEnd;
            nudExtremeLowStart.Value = config.ExtremeLowStart;
            nudExtremeLowEnd.Value = config.ExtremeLowEnd;

            for (int i = 0; i <= 27; i++)
            {
                nudDigitOdds[i].Value = config.GetDigitOdds(i);
            }

            nudSingleMinBet.Value = config.SingleMinBet;
            nudSingleMaxBet.Value = config.SingleMaxBet;
            nudDigitMinBet.Value = config.DigitMinBet;
            nudDigitMaxBet.Value = config.DigitMaxBet;
            nudLeopardMaxBet.Value = config.LeopardMaxBet;
            nudTotalMaxBet.Value = config.TotalMaxBet;

            nudDragonTigerOdds.Value = config.DragonTigerOdds;
            txtDragonNumbers.Text = config.DragonNumbers;
            txtTigerNumbers.Text = config.TigerNumbers;
            txtLeopardDTNumbers.Text = config.LeopardDTNumbers;
        }

        private void SaveSettings()
        {
            var config = new FullOddsConfig
            {
                SingleBetOdds = nudSingleBetOdds.Value,
                CombinationOdds = nudCombinationOdds.Value,
                BigOddSmallEvenOdds = nudBigOddSmallEvenOdds.Value,
                BigEvenSmallOddOdds = nudBigEvenSmallOddOdds.Value,
                ExtremeOdds = nudExtremeOdds.Value,
                PairOdds = nudPairOdds.Value,
                StraightOdds = nudStraightOdds.Value,
                HalfStraightOdds = nudHalfStraightOdds.Value,
                LeopardOdds = nudLeopardOdds.Value,
                MixedOdds = nudMixedOdds.Value,

                ExtremeHighStart = (int)nudExtremeHighStart.Value,
                ExtremeHighEnd = (int)nudExtremeHighEnd.Value,
                ExtremeLowStart = (int)nudExtremeLowStart.Value,
                ExtremeLowEnd = (int)nudExtremeLowEnd.Value,

                SingleMinBet = nudSingleMinBet.Value,
                SingleMaxBet = nudSingleMaxBet.Value,
                DigitMinBet = nudDigitMinBet.Value,
                DigitMaxBet = nudDigitMaxBet.Value,
                LeopardMaxBet = nudLeopardMaxBet.Value,
                TotalMaxBet = nudTotalMaxBet.Value,

                DragonTigerOdds = nudDragonTigerOdds.Value,
                DragonNumbers = txtDragonNumbers.Text,
                TigerNumbers = txtTigerNumbers.Text,
                LeopardDTNumbers = txtLeopardDTNumbers.Text
            };

            for (int i = 0; i <= 27; i++)
            {
                config.DigitOdds[i] = nudDigitOdds[i].Value;
            }

            OddsService.Instance.SaveConfig(config);
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            SaveSettings();
            MessageBox.Show("赔率设置已保存！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnReset_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("确定要恢复默认赔率设置吗？", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                var config = FullOddsConfig.CreateDefault();
                OddsService.Instance.SaveConfig(config);
                LoadSettings();
                MessageBox.Show("已恢复默认设置！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}
