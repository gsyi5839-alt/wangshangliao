using System;
using System.Collections.Generic;
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
    /// 庄家盈利面板 - Banker Profit Panel
    /// </summary>
    public sealed class BankerProfitPanel : UserControl
    {
        private readonly Color BorderColor = Color.FromArgb(180, 180, 180);
        private readonly Color HeaderBgColor = Color.FromArgb(240, 240, 240);

        // Left - DataGridView
        private DataGridView dgv;

        // Right panel
        private Panel pnlRight;
        private Button btnQuery;
        private Button btnExport;

        // Data
        private List<BankerProfitRecord> _allRecords = new List<BankerProfitRecord>();
        private DateTime _startTime = DateTime.Today;
        private DateTime _endTime = DateTime.Now;

        public BankerProfitPanel()
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

            // Columns based on design
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "DrawTime", HeaderText = "开奖时间", Width = 90 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalProfit", HeaderText = "总盈利", Width = 70 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalFlow", HeaderText = "总流水", Width = 70 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalUp", HeaderText = "总上分", Width = 70 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalDown", HeaderText = "总下分", Width = 70 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalPeriods", HeaderText = "总期数", Width = 60 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "BillRemaining", HeaderText = "账单余分", Width = 70 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalRebate", HeaderText = "总反水", Width = 70 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "ActualProfit", HeaderText = "实际盈利", Width = 70 });

            dgv.CellFormatting += Dgv_CellFormatting;

            Controls.Add(dgv);
            dgv.BringToFront();
        }

        private void CreateRightPanel()
        {
            pnlRight = new Panel
            {
                Dock = DockStyle.Right,
                Width = 150,
                BackColor = Color.White,
                Padding = new Padding(10)
            };
            Controls.Add(pnlRight);

            int y = 50;

            // Query button
            btnQuery = new Button
            {
                Text = "开始查询",
                Location = new Point(20, y),
                Size = new Size(100, 35),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White
            };
            btnQuery.FlatAppearance.BorderColor = BorderColor;
            btnQuery.Click += BtnQuery_Click;
            pnlRight.Controls.Add(btnQuery);

            y += 55;

            // Export button
            btnExport = new Button
            {
                Text = "导出",
                Location = new Point(20, y),
                Size = new Size(100, 35),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White
            };
            btnExport.FlatAppearance.BorderColor = BorderColor;
            btnExport.Click += BtnExport_Click;
            pnlRight.Controls.Add(btnExport);
        }

        private void Dgv_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var col = dgv.Columns[e.ColumnIndex].Name;
            if (col == "TotalProfit" || col == "ActualProfit")
            {
                var s = e.Value?.ToString() ?? "";
                if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ||
                    decimal.TryParse(s, out v))
                {
                    e.CellStyle.ForeColor = v >= 0 ? Color.FromArgb(0, 128, 0) : Color.FromArgb(200, 0, 0);
                }
            }
        }

        private void BtnQuery_Click(object sender, EventArgs e)
        {
            LoadRecords();
        }

        private void BtnExport_Click(object sender, EventArgs e)
        {
            if (dgv.Rows.Count == 0)
            {
                MessageBox.Show("没有数据可导出", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var fileName = $"庄家盈利_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            var savePath = Path.Combine(DataService.Instance.DatabaseDir, fileName);

            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("开奖时间,总盈利,总流水,总上分,总下分,总期数,账单余分,总反水,实际盈利");
                foreach (DataGridViewRow row in dgv.Rows)
                {
                    sb.AppendLine($"{row.Cells[0].Value},{row.Cells[1].Value},{row.Cells[2].Value},{row.Cells[3].Value},{row.Cells[4].Value},{row.Cells[5].Value},{row.Cells[6].Value},{row.Cells[7].Value},{row.Cells[8].Value}");
                }
                File.WriteAllText(savePath, sb.ToString(), Encoding.UTF8);
                MessageBox.Show($"已导出到：\r\n{savePath}", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导出失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadRecords()
        {
            _allRecords.Clear();
            var ds = DataService.Instance;

            try
            {
                // Load from 庄家盈利.db file
                var dbPath = Path.Combine(ds.DatabaseDir, "庄家盈利.db");
                if (File.Exists(dbPath))
                {
                    var lines = File.ReadAllLines(dbPath, Encoding.UTF8);
                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        var parts = line.Split('|');
                        if (parts.Length >= 9)
                        {
                            var timeStr = parts[0];
                            if (DateTime.TryParse(timeStr, out var dt))
                            {
                                if (dt >= _startTime && dt <= _endTime)
                                {
                                    var record = new BankerProfitRecord
                                    {
                                        DrawTime = dt,
                                        TotalProfit = ParseDec(parts[1], 0),
                                        TotalFlow = ParseDec(parts[2], 0),
                                        TotalUp = ParseDec(parts[3], 0),
                                        TotalDown = ParseDec(parts[4], 0),
                                        TotalPeriods = (int)ParseDec(parts[5], 0),
                                        BillRemaining = ParseDec(parts[6], 0),
                                        TotalRebate = ParseDec(parts[7], 0),
                                        ActualProfit = ParseDec(parts[8], 0)
                                    };
                                    _allRecords.Add(record);
                                }
                            }
                        }
                    }
                }

                // Sort by time descending
                _allRecords = _allRecords.OrderByDescending(r => r.DrawTime).ToList();

                RenderRecords(_allRecords);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载数据失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RenderRecords(List<BankerProfitRecord> records)
        {
            dgv.Rows.Clear();
            foreach (var r in records)
            {
                dgv.Rows.Add(
                    r.DrawTime.ToString("yyyy-MM-dd"),
                    r.TotalProfit.ToString(CultureInfo.InvariantCulture),
                    r.TotalFlow.ToString(CultureInfo.InvariantCulture),
                    r.TotalUp.ToString(CultureInfo.InvariantCulture),
                    r.TotalDown.ToString(CultureInfo.InvariantCulture),
                    r.TotalPeriods.ToString(),
                    r.BillRemaining.ToString(CultureInfo.InvariantCulture),
                    r.TotalRebate.ToString(CultureInfo.InvariantCulture),
                    r.ActualProfit.ToString(CultureInfo.InvariantCulture)
                );
            }
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

    internal class BankerProfitRecord
    {
        public DateTime DrawTime { get; set; }
        public decimal TotalProfit { get; set; }
        public decimal TotalFlow { get; set; }
        public decimal TotalUp { get; set; }
        public decimal TotalDown { get; set; }
        public int TotalPeriods { get; set; }
        public decimal BillRemaining { get; set; }
        public decimal TotalRebate { get; set; }
        public decimal ActualProfit { get; set; }
    }
}

