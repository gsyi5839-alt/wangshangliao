using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using WangShangLiaoBot.Services;

namespace WangShangLiaoBot.Controls.RebateTool
{
    /// <summary>
    /// 夜宵计算面板 - Night snack calculation panel
    /// </summary>
    public sealed class NightSnackCalcPanel : UserControl
    {
        private readonly Color BorderColor = Color.FromArgb(180, 180, 180);
        private readonly Color HeaderBgColor = Color.FromArgb(240, 240, 240);

        // Left panel - DataGridView
        private DataGridView dgv;

        // Right panel controls
        private Panel pnlRight;
        private Label lblSearchTitle;
        private TextBox txtSearch;
        private Button btnSearch;
        private Button btnCalcStats;
        private Button btnAddBill;
        private GroupBox grpResult;
        private TextBox txtResult;

        // Data
        private readonly BindingList<NightSnackItem> _items = new BindingList<NightSnackItem>();
        private DateTime _startTime = DateTime.Today;
        private DateTime _endTime = DateTime.Now;

        public NightSnackCalcPanel()
        {
            SuspendLayout();
            BackColor = Color.White;
            Dock = DockStyle.Fill;
            Font = new Font("Microsoft YaHei UI", 9F);

            CreateRightPanel();
            CreateGrid();

            ResumeLayout(false);
        }

        private void CreateGrid()
        {
            dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                CellBorderStyle = DataGridViewCellBorderStyle.Single,
                GridColor = BorderColor,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersHeight = 24,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single,
                EnableHeadersVisualStyles = false,
                RowTemplate = { Height = 20 },
                ScrollBars = ScrollBars.Both
            };

            dgv.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = HeaderBgColor,
                ForeColor = Color.Black,
                Font = new Font("Microsoft YaHei UI", 9F),
                Alignment = DataGridViewContentAlignment.MiddleCenter,
                SelectionBackColor = HeaderBgColor,
                SelectionForeColor = Color.Black
            };

            dgv.DefaultCellStyle = new DataGridViewCellStyle
            {
                Font = new Font("Microsoft YaHei UI", 9F),
                SelectionBackColor = Color.FromArgb(0, 120, 215),
                SelectionForeColor = Color.White
            };

            // Add columns based on design
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "WangwangId", HeaderText = "旺旺号", Width = 80 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Nickname", HeaderText = "昵称", Width = 70 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalBetFlow", HeaderText = "总下注流水", Width = 90 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "ComboFlow", HeaderText = "组合流水", Width = 80 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Profit", HeaderText = "盈利", Width = 70 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Reward", HeaderText = "奖励", Width = 70 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Condition", HeaderText = "符号条件", Width = 80 });

            dgv.CellFormatting += Dgv_CellFormatting;

            Controls.Add(dgv);
            dgv.BringToFront();
        }

        private void CreateRightPanel()
        {
            pnlRight = new Panel
            {
                Dock = DockStyle.Right,
                Width = 250,
                BackColor = Color.White,
                Padding = new Padding(10)
            };
            Controls.Add(pnlRight);

            // Search section
            lblSearchTitle = new Label
            {
                Text = "搜索旺旺号",
                Location = new Point(10, 10),
                AutoSize = true
            };
            pnlRight.Controls.Add(lblSearchTitle);

            txtSearch = new TextBox
            {
                Location = new Point(10, 30),
                Size = new Size(140, 23)
            };
            pnlRight.Controls.Add(txtSearch);

            btnSearch = new Button
            {
                Text = "开始搜索",
                Location = new Point(10, 60),
                Size = new Size(100, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White
            };
            btnSearch.FlatAppearance.BorderColor = BorderColor;
            btnSearch.Click += BtnSearch_Click;
            pnlRight.Controls.Add(btnSearch);

            // Action buttons
            btnCalcStats = new Button
            {
                Text = "计算统计",
                Location = new Point(160, 30),
                Size = new Size(80, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White
            };
            btnCalcStats.FlatAppearance.BorderColor = BorderColor;
            btnCalcStats.Click += BtnCalcStats_Click;
            pnlRight.Controls.Add(btnCalcStats);

            btnAddBill = new Button
            {
                Text = "加入账单",
                Location = new Point(160, 65),
                Size = new Size(80, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White
            };
            btnAddBill.FlatAppearance.BorderColor = BorderColor;
            btnAddBill.Click += BtnAddBill_Click;
            pnlRight.Controls.Add(btnAddBill);

            // Result section
            grpResult = new GroupBox
            {
                Text = "计算结果",
                Location = new Point(5, 100),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            pnlRight.Controls.Add(grpResult);

            txtResult = new TextBox
            {
                Location = new Point(10, 20),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BorderStyle = BorderStyle.FixedSingle,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            grpResult.Controls.Add(txtResult);

            // Resize handler
            pnlRight.Resize += (s, e) =>
            {
                grpResult.Size = new Size(pnlRight.Width - 15, pnlRight.Height - grpResult.Top - 10);
                txtResult.Size = new Size(grpResult.Width - 25, grpResult.Height - 35);
            };

            grpResult.Size = new Size(235, 350);
            txtResult.Size = new Size(210, 315);
        }

        private void Dgv_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var col = dgv.Columns[e.ColumnIndex].Name;
            if (col == "Profit" || col == "Reward")
            {
                var s = e.Value?.ToString() ?? "";
                if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ||
                    decimal.TryParse(s, out v))
                {
                    e.CellStyle.ForeColor = v >= 0 ? Color.FromArgb(200, 0, 0) : Color.FromArgb(0, 128, 0);
                }
            }
        }

        private void BtnSearch_Click(object sender, EventArgs e)
        {
            var searchText = txtSearch.Text.Trim();
            if (string.IsNullOrEmpty(searchText))
            {
                // Show all if empty
                RenderItems(_items.ToList());
                return;
            }

            // Filter items by wangwang ID
            var filtered = _items.Where(x =>
                x.WangwangId.Contains(searchText) ||
                x.Nickname.Contains(searchText)).ToList();

            RenderItems(filtered);
        }

        private void BtnCalcStats_Click(object sender, EventArgs e)
        {
            // Load data and calculate statistics
            var items = LoadNightSnackData();
            _items.Clear();
            foreach (var item in items)
            {
                _items.Add(item);
            }
            RenderItems(items);

            // Calculate totals
            decimal totalFlow = items.Sum(x => x.TotalBetFlow);
            decimal totalCombo = items.Sum(x => x.ComboFlow);
            decimal totalProfit = items.Sum(x => x.Profit);
            decimal totalReward = items.Sum(x => x.Reward);

            var sb = new StringBuilder();
            sb.AppendLine($"统计时间：");
            sb.AppendLine($"{_startTime:yyyy-MM-dd HH:mm}");
            sb.AppendLine($"→ {_endTime:yyyy-MM-dd HH:mm}");
            sb.AppendLine();
            sb.AppendLine($"玩家数：{items.Count}");
            sb.AppendLine($"总下注流水：{totalFlow}");
            sb.AppendLine($"总组合流水：{totalCombo}");
            sb.AppendLine($"总盈利：{totalProfit}");
            sb.AppendLine($"总奖励：{totalReward}");

            txtResult.Text = sb.ToString();
        }

        private void BtnAddBill_Click(object sender, EventArgs e)
        {
            if (_items.Count == 0)
            {
                MessageBox.Show("没有数据可加入账单，请先点击【计算统计】", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Export to file
            var fileName = $"夜宵计算_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            var savePath = Path.Combine(DataService.Instance.DatabaseDir, fileName);

            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("旺旺号,昵称,总下注流水,组合流水,盈利,奖励,符号条件");
                foreach (var item in _items)
                {
                    sb.AppendLine($"{item.WangwangId},{item.Nickname},{item.TotalBetFlow},{item.ComboFlow},{item.Profit},{item.Reward},{item.Condition}");
                }
                File.WriteAllText(savePath, sb.ToString(), Encoding.UTF8);

                txtResult.Text += $"\r\n\r\n已导出账单：\r\n{savePath}";
                MessageBox.Show($"已加入账单并导出到：\r\n{savePath}", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导出失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RenderItems(List<NightSnackItem> items)
        {
            dgv.Rows.Clear();
            foreach (var item in items)
            {
                dgv.Rows.Add(
                    item.WangwangId,
                    item.Nickname,
                    item.TotalBetFlow.ToString(CultureInfo.InvariantCulture),
                    item.ComboFlow.ToString(CultureInfo.InvariantCulture),
                    item.Profit.ToString(CultureInfo.InvariantCulture),
                    item.Reward.ToString(CultureInfo.InvariantCulture),
                    item.Condition
                );
            }
        }

        private List<NightSnackItem> LoadNightSnackData()
        {
            var result = new List<NightSnackItem>();
            var ds = DataService.Instance;

            try
            {
                // Get all players
                var players = ds.GetAllPlayers();
                var playerStats = new Dictionary<string, NightSnackItem>();

                // Initialize player stats
                foreach (var p in players)
                {
                    if (string.IsNullOrWhiteSpace(p.WangWangId)) continue;
                    playerStats[p.WangWangId] = new NightSnackItem
                    {
                        WangwangId = p.WangWangId,
                        Nickname = p.Nickname ?? ""
                    };
                }

                // Load bet data from Daily settings
                for (var day = _startTime.Date; day <= _endTime.Date; day = day.AddDays(1))
                {
                    var dateKey = day.ToString("yyyy-MM-dd");
                    foreach (var kv in playerStats)
                    {
                        var id = kv.Key;
                        var item = kv.Value;

                        // Stake (total bet flow)
                        var stakeStr = ds.GetSetting($"Daily:{dateKey}:{id}:Stake", "");
                        if (!string.IsNullOrEmpty(stakeStr))
                        {
                            item.TotalBetFlow += ParseDec(stakeStr, 0);
                        }

                        // Score changes for profit calculation
                        var startStr = ds.GetSetting($"Daily:{dateKey}:{id}:StartScore", "");
                        var lastStr = ds.GetSetting($"Daily:{dateKey}:{id}:LastScore", "");
                        if (!string.IsNullOrEmpty(startStr) && !string.IsNullOrEmpty(lastStr))
                        {
                            var start = ParseDec(startStr, 0);
                            var last = ParseDec(lastStr, 0);
                            item.Profit += (last - start);
                        }
                    }
                }

                // Calculate rewards based on settings
                var rewardRate = ParseDec(ds.GetSetting("NightSnack:RewardRate", "0"), 0);
                if (rewardRate > 1) rewardRate /= 100;

                foreach (var kv in playerStats)
                {
                    var item = kv.Value;
                    if (item.TotalBetFlow > 0)
                    {
                        item.Reward = Math.Round(item.TotalBetFlow * rewardRate, 2);
                        item.ComboFlow = item.TotalBetFlow; // Simplified, can be enhanced
                        item.Condition = item.TotalBetFlow > 0 ? "√" : "";
                        result.Add(item);
                    }
                }

                // Sort by total bet flow descending
                result = result.OrderByDescending(x => x.TotalBetFlow).ToList();
            }
            catch { }

            return result;
        }

        private static decimal ParseDec(string s, decimal defaultValue)
        {
            if (string.IsNullOrWhiteSpace(s)) return defaultValue;
            if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v)) return v;
            if (decimal.TryParse(s, out v)) return v;
            return defaultValue;
        }

        public void RefreshData(DateTime startTime, DateTime endTime)
        {
            _startTime = startTime;
            _endTime = endTime;
        }
    }

    internal class NightSnackItem
    {
        public string WangwangId { get; set; } = "";
        public string Nickname { get; set; } = "";
        public decimal TotalBetFlow { get; set; }
        public decimal ComboFlow { get; set; }
        public decimal Profit { get; set; }
        public decimal Reward { get; set; }
        public string Condition { get; set; } = "";
    }
}

