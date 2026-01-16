using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using WangShangLiaoBot.Services;

namespace WangShangLiaoBot.Controls
{
    /// <summary>
    /// 夜宵/返点设置控件
    /// </summary>
    public class BonusSettingsControl : UserControl
    {
        private TabControl tabControl;

        // 夜宵设置
        private CheckBox chkNightSnackEnabled;
        private CheckBox chkNotifyInGroup;
        private ComboBox cboCalculationMethod;
        private DataGridView dgvNightSnackRules;
        private DataGridView dgvWinRules;
        private DataGridView dgvLoseRules;

        // 流水返点设置
        private CheckBox chkRebateEnabled;
        private NumericUpDown nudDefaultPercent;
        private NumericUpDown nudDefaultMinBets;
        private TextBox txtCommand;
        private TextBox txtHasRebateReply;
        private TextBox txtNoRebateReply;
        private TextBox txtNotEnoughBetsReply;
        private DataGridView dgvTierRules;

        private Button btnSave;
        private Label lblStatus;

        public BonusSettingsControl()
        {
            InitializeComponent();
            LoadConfig();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.Size = new Size(800, 600);
            this.BackColor = Color.White;

            // 标题
            var lblTitle = new Label
            {
                Text = "🎁 夜宵/流水返点设置",
                Font = new Font("Microsoft YaHei", 14, FontStyle.Bold),
                Location = new Point(20, 15),
                AutoSize = true
            };
            this.Controls.Add(lblTitle);

            // 计算方式
            var lblMethod = new Label { Text = "计算方式:", Location = new Point(20, 50), AutoSize = true };
            this.Controls.Add(lblMethod);
            cboCalculationMethod = new ComboBox
            {
                Location = new Point(90, 48),
                Width = 150,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cboCalculationMethod.Items.AddRange(new object[] { "按把数", "按流水", "按输赢" });
            cboCalculationMethod.SelectedIndex = 0;
            this.Controls.Add(cboCalculationMethod);

            // Tab控件
            tabControl = new TabControl
            {
                Location = new Point(20, 80),
                Size = new Size(760, 420)
            };
            this.Controls.Add(tabControl);

            // 夜宵设置Tab
            var tabNightSnack = new TabPage("夜宵设置");
            InitNightSnackTab(tabNightSnack);
            tabControl.TabPages.Add(tabNightSnack);

            // 流水返点Tab
            var tabRebate = new TabPage("流水返点");
            InitRebateTab(tabRebate);
            tabControl.TabPages.Add(tabRebate);

            // 保存按钮
            btnSave = new Button
            {
                Text = "💾 保存配置",
                Location = new Point(20, 510),
                Size = new Size(120, 35),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnSave.Click += BtnSave_Click;
            this.Controls.Add(btnSave);

            lblStatus = new Label
            {
                Text = "",
                Location = new Point(150, 518),
                AutoSize = true,
                ForeColor = Color.Green
            };
            this.Controls.Add(lblStatus);

            this.ResumeLayout();
        }

        private void InitNightSnackTab(TabPage tab)
        {
            chkNightSnackEnabled = new CheckBox
            {
                Text = "启用夜宵功能",
                Location = new Point(15, 15),
                AutoSize = true
            };
            tab.Controls.Add(chkNightSnackEnabled);

            chkNotifyInGroup = new CheckBox
            {
                Text = "群内通知",
                Location = new Point(150, 15),
                AutoSize = true
            };
            tab.Controls.Add(chkNotifyInGroup);

            // 把数规则
            var lblBetsRules = new Label { Text = "把数规则 (流水范围-把数-奖励):", Location = new Point(15, 45), AutoSize = true };
            tab.Controls.Add(lblBetsRules);

            dgvNightSnackRules = new DataGridView
            {
                Location = new Point(15, 70),
                Size = new Size(350, 120),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = true,
                RowHeadersVisible = false
            };
            dgvNightSnackRules.Columns.Add("MinTurnover", "最低流水");
            dgvNightSnackRules.Columns.Add("MaxTurnover", "最高流水");
            dgvNightSnackRules.Columns.Add("MinBets", "最低把数");
            dgvNightSnackRules.Columns.Add("Bonus", "奖励");
            tab.Controls.Add(dgvNightSnackRules);

            // 输赢规则
            var lblWinRules = new Label { Text = "赢钱规则 (金额=奖励):", Location = new Point(380, 45), AutoSize = true };
            tab.Controls.Add(lblWinRules);

            dgvWinRules = new DataGridView
            {
                Location = new Point(380, 70),
                Size = new Size(180, 120),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = true,
                RowHeadersVisible = false
            };
            dgvWinRules.Columns.Add("Amount", "赢钱金额");
            dgvWinRules.Columns.Add("Bonus", "奖励");
            tab.Controls.Add(dgvWinRules);

            var lblLoseRules = new Label { Text = "输钱规则:", Location = new Point(570, 45), AutoSize = true };
            tab.Controls.Add(lblLoseRules);

            dgvLoseRules = new DataGridView
            {
                Location = new Point(570, 70),
                Size = new Size(170, 120),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = true,
                RowHeadersVisible = false
            };
            dgvLoseRules.Columns.Add("Amount", "输钱金额");
            dgvLoseRules.Columns.Add("Bonus", "奖励");
            tab.Controls.Add(dgvLoseRules);
        }

        private void InitRebateTab(TabPage tab)
        {
            chkRebateEnabled = new CheckBox
            {
                Text = "启用流水返点",
                Location = new Point(15, 15),
                AutoSize = true
            };
            tab.Controls.Add(chkRebateEnabled);

            var lblPercent = new Label { Text = "默认返点比例(%):", Location = new Point(15, 45), AutoSize = true };
            tab.Controls.Add(lblPercent);
            nudDefaultPercent = new NumericUpDown
            {
                Location = new Point(130, 43),
                Width = 80,
                Minimum = 0,
                Maximum = 100,
                DecimalPlaces = 1,
                Increment = 0.1m
            };
            tab.Controls.Add(nudDefaultPercent);

            var lblMinBets = new Label { Text = "最低把数:", Location = new Point(220, 45), AutoSize = true };
            tab.Controls.Add(lblMinBets);
            nudDefaultMinBets = new NumericUpDown
            {
                Location = new Point(290, 43),
                Width = 80,
                Minimum = 0,
                Maximum = 1000
            };
            tab.Controls.Add(nudDefaultMinBets);

            var lblCommand = new Label { Text = "触发命令:", Location = new Point(15, 75), AutoSize = true };
            tab.Controls.Add(lblCommand);
            txtCommand = new TextBox { Location = new Point(85, 73), Width = 100 };
            tab.Controls.Add(txtCommand);

            // 回复模板
            var lblHasReply = new Label { Text = "有返点回复:", Location = new Point(15, 105), AutoSize = true };
            tab.Controls.Add(lblHasReply);
            txtHasRebateReply = new TextBox
            {
                Location = new Point(100, 103),
                Width = 350,
                Height = 40,
                Multiline = true
            };
            tab.Controls.Add(txtHasRebateReply);

            var lblNoReply = new Label { Text = "无返点回复:", Location = new Point(15, 150), AutoSize = true };
            tab.Controls.Add(lblNoReply);
            txtNoRebateReply = new TextBox
            {
                Location = new Point(100, 148),
                Width = 350
            };
            tab.Controls.Add(txtNoRebateReply);

            var lblNotEnough = new Label { Text = "把数不足回复:", Location = new Point(15, 180), AutoSize = true };
            tab.Controls.Add(lblNotEnough);
            txtNotEnoughBetsReply = new TextBox
            {
                Location = new Point(110, 178),
                Width = 340
            };
            tab.Controls.Add(txtNotEnoughBetsReply);

            // 阶梯规则
            var lblTier = new Label { Text = "阶梯返点规则 (流水=返点%):", Location = new Point(15, 210), AutoSize = true };
            tab.Controls.Add(lblTier);

            dgvTierRules = new DataGridView
            {
                Location = new Point(15, 235),
                Size = new Size(250, 120),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = true,
                RowHeadersVisible = false
            };
            dgvTierRules.Columns.Add("MinTurnover", "最低流水");
            dgvTierRules.Columns.Add("Percent", "返点比例(%)");
            tab.Controls.Add(dgvTierRules);

            // 变量说明
            var lblVars = new Label
            {
                Text = "可用变量:\n[艾特] - @玩家\n[旺旺] - 玩家昵称\n[分数] - 返点金额\n[余粮] - 当前余额\n[把数] - 把数要求\n[换行] - 换行",
                Location = new Point(500, 105),
                AutoSize = true,
                ForeColor = Color.Gray
            };
            tab.Controls.Add(lblVars);
        }

        private void LoadConfig()
        {
            var config = BonusService.Instance.GetConfig();

            cboCalculationMethod.SelectedIndex = (int)config.CalculationMethod;

            // 夜宵设置
            chkNightSnackEnabled.Checked = config.NightSnack.Enabled;
            chkNotifyInGroup.Checked = config.NightSnack.NotifyInGroup;

            dgvNightSnackRules.Rows.Clear();
            foreach (var rule in config.NightSnack.Rules)
            {
                dgvNightSnackRules.Rows.Add(rule.MinTurnover, rule.MaxTurnover, rule.MinBets, rule.Bonus);
            }

            dgvWinRules.Rows.Clear();
            foreach (var rule in config.NightSnack.WinRules)
            {
                dgvWinRules.Rows.Add(rule.Amount, rule.Bonus);
            }

            dgvLoseRules.Rows.Clear();
            foreach (var rule in config.NightSnack.LoseRules)
            {
                dgvLoseRules.Rows.Add(rule.Amount, rule.Bonus);
            }

            // 流水返点设置
            chkRebateEnabled.Checked = config.TurnoverRebate.Enabled;
            nudDefaultPercent.Value = config.TurnoverRebate.DefaultPercent;
            nudDefaultMinBets.Value = config.TurnoverRebate.DefaultMinBets;
            txtCommand.Text = config.TurnoverRebate.Command;
            txtHasRebateReply.Text = config.TurnoverRebate.HasRebateReply;
            txtNoRebateReply.Text = config.TurnoverRebate.NoRebateReply;
            txtNotEnoughBetsReply.Text = config.TurnoverRebate.NotEnoughBetsReply;

            dgvTierRules.Rows.Clear();
            foreach (var rule in config.TurnoverRebate.TierRules)
            {
                dgvTierRules.Rows.Add(rule.MinTurnover, rule.Percent);
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            var config = new BonusConfig
            {
                CalculationMethod = (BonusCalculationMethod)cboCalculationMethod.SelectedIndex,
                NightSnack = new NightSnackConfig
                {
                    Enabled = chkNightSnackEnabled.Checked,
                    NotifyInGroup = chkNotifyInGroup.Checked
                },
                TurnoverRebate = new TurnoverRebateConfig
                {
                    Enabled = chkRebateEnabled.Checked,
                    DefaultPercent = nudDefaultPercent.Value,
                    DefaultMinBets = (int)nudDefaultMinBets.Value,
                    Command = txtCommand.Text,
                    HasRebateReply = txtHasRebateReply.Text,
                    NoRebateReply = txtNoRebateReply.Text,
                    NotEnoughBetsReply = txtNotEnoughBetsReply.Text
                }
            };

            // 解析夜宵规则
            foreach (DataGridViewRow row in dgvNightSnackRules.Rows)
            {
                if (row.IsNewRow || row.Cells[0].Value == null) continue;
                config.NightSnack.Rules.Add(new NightSnackRule
                {
                    MinTurnover = decimal.Parse(row.Cells[0].Value.ToString()),
                    MaxTurnover = decimal.Parse(row.Cells[1].Value.ToString()),
                    MinBets = int.Parse(row.Cells[2].Value.ToString()),
                    Bonus = decimal.Parse(row.Cells[3].Value.ToString())
                });
            }

            // 解析输赢规则
            foreach (DataGridViewRow row in dgvWinRules.Rows)
            {
                if (row.IsNewRow || row.Cells[0].Value == null) continue;
                config.NightSnack.WinRules.Add(new WinLoseRule
                {
                    Amount = decimal.Parse(row.Cells[0].Value.ToString()),
                    Bonus = decimal.Parse(row.Cells[1].Value.ToString())
                });
            }

            foreach (DataGridViewRow row in dgvLoseRules.Rows)
            {
                if (row.IsNewRow || row.Cells[0].Value == null) continue;
                config.NightSnack.LoseRules.Add(new WinLoseRule
                {
                    Amount = decimal.Parse(row.Cells[0].Value.ToString()),
                    Bonus = decimal.Parse(row.Cells[1].Value.ToString())
                });
            }

            // 解析阶梯规则
            foreach (DataGridViewRow row in dgvTierRules.Rows)
            {
                if (row.IsNewRow || row.Cells[0].Value == null) continue;
                config.TurnoverRebate.TierRules.Add(new TurnoverTierRule
                {
                    MinTurnover = decimal.Parse(row.Cells[0].Value.ToString()),
                    Percent = decimal.Parse(row.Cells[1].Value.ToString())
                });
            }

            BonusService.Instance.SaveConfig(config);
            lblStatus.Text = "✓ 配置已保存";
        }
    }
}
