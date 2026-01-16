using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using WangShangLiaoBot.Services;

namespace WangShangLiaoBot.Controls
{
    /// <summary>
    /// 托管设置控件
    /// </summary>
    public class TrusteeSettingsControl : UserControl
    {
        #pragma warning disable CS0169 // 字段暂未使用，保留用于后续功能
        private CheckBox chkEnabled;
        private NumericUpDown nudDelayAfterDraw;
        private NumericUpDown nudDelayBeforeSeal;
        private CheckBox chkAutoDeposit;
        private CheckBox chkAutoWithdraw;
        private NumericUpDown nudDepositDelayMin;
        private NumericUpDown nudDepositDelayMax;
        private NumericUpDown nudWithdrawDelayMin;
        private NumericUpDown nudWithdrawDelayMax;
        #pragma warning restore CS0169
        private DataGridView dgvStrategies;
        private Button btnAddStrategy;
        private Button btnRemoveStrategy;
        private Button btnSave;
        private ListView lvTrustees;
        private Button btnStopSelected;
        private Label lblStatus;

        public TrusteeSettingsControl()
        {
            InitializeComponent();
            LoadConfig();
            RefreshTrusteeList();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.Size = new Size(800, 600);
            this.BackColor = Color.White;

            // 标题
            var lblTitle = new Label
            {
                Text = "🤖 托管自动下注设置",
                Font = new Font("Microsoft YaHei", 14, FontStyle.Bold),
                Location = new Point(20, 15),
                AutoSize = true
            };
            this.Controls.Add(lblTitle);

            // 基本设置分组
            var grpBasic = new GroupBox
            {
                Text = "基本设置",
                Location = new Point(20, 50),
                Size = new Size(350, 180)
            };
            this.Controls.Add(grpBasic);

            chkEnabled = new CheckBox
            {
                Text = "启用托管功能",
                Location = new Point(15, 25),
                AutoSize = true
            };
            grpBasic.Controls.Add(chkEnabled);

            var lblDelayAfter = new Label { Text = "开奖后不下注时间(秒):", Location = new Point(15, 55), AutoSize = true };
            grpBasic.Controls.Add(lblDelayAfter);
            nudDelayAfterDraw = new NumericUpDown { Location = new Point(180, 53), Width = 80, Minimum = 0, Maximum = 60 };
            grpBasic.Controls.Add(nudDelayAfterDraw);

            var lblDelayBefore = new Label { Text = "封盘前不下注时间(秒):", Location = new Point(15, 85), AutoSize = true };
            grpBasic.Controls.Add(lblDelayBefore);
            nudDelayBeforeSeal = new NumericUpDown { Location = new Point(180, 83), Width = 80, Minimum = 0, Maximum = 60 };
            grpBasic.Controls.Add(nudDelayBeforeSeal);

            chkAutoDeposit = new CheckBox { Text = "托管自动上分", Location = new Point(15, 115), AutoSize = true };
            grpBasic.Controls.Add(chkAutoDeposit);

            chkAutoWithdraw = new CheckBox { Text = "托管自动下分", Location = new Point(150, 115), AutoSize = true };
            grpBasic.Controls.Add(chkAutoWithdraw);

            // 延迟设置
            var lblDepositDelay = new Label { Text = "上分延迟(秒):", Location = new Point(15, 145), AutoSize = true };
            grpBasic.Controls.Add(lblDepositDelay);
            nudDepositDelayMin = new NumericUpDown { Location = new Point(100, 143), Width = 50, Minimum = 0, Maximum = 60 };
            grpBasic.Controls.Add(nudDepositDelayMin);
            var lblTo1 = new Label { Text = "-", Location = new Point(155, 145), AutoSize = true };
            grpBasic.Controls.Add(lblTo1);
            nudDepositDelayMax = new NumericUpDown { Location = new Point(170, 143), Width = 50, Minimum = 0, Maximum = 60 };
            grpBasic.Controls.Add(nudDepositDelayMax);

            var lblWithdrawDelay = new Label { Text = "下分延迟:", Location = new Point(230, 145), AutoSize = true };
            grpBasic.Controls.Add(lblWithdrawDelay);
            nudWithdrawDelayMin = new NumericUpDown { Location = new Point(290, 143), Width = 50, Minimum = 0, Maximum = 120 };
            grpBasic.Controls.Add(nudWithdrawDelayMin);

            // 策略设置分组
            var grpStrategy = new GroupBox
            {
                Text = "分数段策略配置",
                Location = new Point(20, 240),
                Size = new Size(450, 200)
            };
            this.Controls.Add(grpStrategy);

            dgvStrategies = new DataGridView
            {
                Location = new Point(15, 25),
                Size = new Size(420, 130),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            dgvStrategies.Columns.Add("MinBalance", "最低余额");
            dgvStrategies.Columns.Add("MaxBalance", "最高余额");
            dgvStrategies.Columns.Add("BetContent", "下注内容");
            dgvStrategies.Columns["BetContent"].Width = 200;
            grpStrategy.Controls.Add(dgvStrategies);

            btnAddStrategy = new Button
            {
                Text = "添加策略",
                Location = new Point(15, 165),
                Size = new Size(80, 25)
            };
            btnAddStrategy.Click += BtnAddStrategy_Click;
            grpStrategy.Controls.Add(btnAddStrategy);

            btnRemoveStrategy = new Button
            {
                Text = "删除策略",
                Location = new Point(100, 165),
                Size = new Size(80, 25)
            };
            btnRemoveStrategy.Click += BtnRemoveStrategy_Click;
            grpStrategy.Controls.Add(btnRemoveStrategy);

            // 当前托管列表
            var grpTrustees = new GroupBox
            {
                Text = "当前托管玩家",
                Location = new Point(390, 50),
                Size = new Size(390, 180)
            };
            this.Controls.Add(grpTrustees);

            lvTrustees = new ListView
            {
                Location = new Point(15, 25),
                Size = new Size(360, 110),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true
            };
            lvTrustees.Columns.Add("玩家", 100);
            lvTrustees.Columns.Add("开始时间", 80);
            lvTrustees.Columns.Add("下注次数", 60);
            lvTrustees.Columns.Add("状态", 60);
            grpTrustees.Controls.Add(lvTrustees);

            btnStopSelected = new Button
            {
                Text = "停止选中托管",
                Location = new Point(15, 145),
                Size = new Size(100, 25)
            };
            btnStopSelected.Click += BtnStopSelected_Click;
            grpTrustees.Controls.Add(btnStopSelected);

            var btnRefresh = new Button
            {
                Text = "刷新列表",
                Location = new Point(120, 145),
                Size = new Size(80, 25)
            };
            btnRefresh.Click += (s, e) => RefreshTrusteeList();
            grpTrustees.Controls.Add(btnRefresh);

            // 保存按钮
            btnSave = new Button
            {
                Text = "💾 保存配置",
                Location = new Point(20, 450),
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
                Location = new Point(150, 458),
                AutoSize = true,
                ForeColor = Color.Green
            };
            this.Controls.Add(lblStatus);

            this.ResumeLayout();
        }

        private void LoadConfig()
        {
            var config = TrusteeService.Instance.GetConfig();

            chkEnabled.Checked = config.Enabled;
            nudDelayAfterDraw.Value = config.DelayAfterDraw;
            nudDelayBeforeSeal.Value = config.DelayBeforeSeal;
            chkAutoDeposit.Checked = config.AutoDeposit;
            chkAutoWithdraw.Checked = config.AutoWithdraw;
            nudDepositDelayMin.Value = config.DepositDelayMin;
            nudDepositDelayMax.Value = config.DepositDelayMax;
            nudWithdrawDelayMin.Value = config.WithdrawDelayMin;

            // 加载策略
            dgvStrategies.Rows.Clear();
            foreach (var strategy in config.Strategies)
            {
                dgvStrategies.Rows.Add(
                    strategy.MinBalance,
                    strategy.MaxBalance,
                    string.Join("|", strategy.BetContents)
                );
            }
        }

        private void RefreshTrusteeList()
        {
            lvTrustees.Items.Clear();
            var trustees = TrusteeService.Instance.GetTrustees();

            foreach (var t in trustees)
            {
                var item = new ListViewItem(t.PlayerNick);
                item.SubItems.Add(t.StartTime.ToString("HH:mm:ss"));
                item.SubItems.Add(t.TotalBets.ToString());
                item.SubItems.Add(t.IsActive ? "运行中" : "已停止");
                item.Tag = t.PlayerId;
                lvTrustees.Items.Add(item);
            }
        }

        private void BtnAddStrategy_Click(object sender, EventArgs e)
        {
            dgvStrategies.Rows.Add(0, 1000, "da100|x100");
        }

        private void BtnRemoveStrategy_Click(object sender, EventArgs e)
        {
            if (dgvStrategies.SelectedRows.Count > 0)
            {
                dgvStrategies.Rows.Remove(dgvStrategies.SelectedRows[0]);
            }
        }

        private void BtnStopSelected_Click(object sender, EventArgs e)
        {
            if (lvTrustees.SelectedItems.Count > 0)
            {
                var playerId = lvTrustees.SelectedItems[0].Tag as string;
                if (!string.IsNullOrEmpty(playerId))
                {
                    TrusteeService.Instance.RemoveTrustee(playerId);
                    RefreshTrusteeList();
                }
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            var config = new TrusteeConfig
            {
                Enabled = chkEnabled.Checked,
                DelayAfterDraw = (int)nudDelayAfterDraw.Value,
                DelayBeforeSeal = (int)nudDelayBeforeSeal.Value,
                AutoDeposit = chkAutoDeposit.Checked,
                AutoWithdraw = chkAutoWithdraw.Checked,
                DepositDelayMin = (int)nudDepositDelayMin.Value,
                DepositDelayMax = (int)nudDepositDelayMax.Value,
                WithdrawDelayMin = (int)nudWithdrawDelayMin.Value,
                WithdrawDelayMax = (int)nudWithdrawDelayMin.Value // Using min for max as well
            };

            // 保存策略
            foreach (DataGridViewRow row in dgvStrategies.Rows)
            {
                if (row.Cells[0].Value == null) continue;

                var strategy = new TrusteeStrategy
                {
                    MinBalance = decimal.Parse(row.Cells[0].Value.ToString()),
                    MaxBalance = decimal.Parse(row.Cells[1].Value.ToString()),
                    BetContents = row.Cells[2].Value.ToString().Split('|').ToList()
                };
                config.Strategies.Add(strategy);
            }

            TrusteeService.Instance.SaveConfig(config);
            lblStatus.Text = "✓ 配置已保存";
        }
    }
}
