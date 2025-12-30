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
    /// 每期盈利面板 - Per Period Profit Panel
    /// </summary>
    public sealed class PerPeriodProfitPanel : UserControl
    {
        private readonly Color BorderColor = Color.FromArgb(180, 180, 180);
        private readonly Color HeaderBgColor = Color.FromArgb(240, 240, 240);

        // Left - DataGridView
        private DataGridView dgv;

        // Right panel
        private Panel pnlRight;
        private RadioButton rbAscending;
        private RadioButton rbDescending;
        private Button btnQuery;
        private Button btnExport;

        // Attack section
        private Label lblAttackTitle;
        private TextBox txtAttack;
        private Button btnCopyAttack;

        // Bill section
        private Label lblBillTitle;
        private TextBox txtBill;
        private Button btnCopyBill;

        // Data
        private List<PeriodProfitRecord> _allRecords = new List<PeriodProfitRecord>();
        private DateTime _startTime = DateTime.Today;
        private DateTime _endTime = DateTime.Now;

        public PerPeriodProfitPanel()
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
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "DrawTime", HeaderText = "开奖时间", Width = 130 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "PeriodNo", HeaderText = "期号", Width = 70 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "DrawCode", HeaderText = "开奖码", Width = 75 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "LotteryType", HeaderText = "彩种", Width = 55 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "BankerProfit", HeaderText = "庄家盈利", Width = 65 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Type", HeaderText = "类型", Width = 50 });

            dgv.SelectionChanged += Dgv_SelectionChanged;
            dgv.CellFormatting += Dgv_CellFormatting;

            Controls.Add(dgv);
            dgv.BringToFront();
        }

        private void CreateRightPanel()
        {
            pnlRight = new Panel
            {
                Dock = DockStyle.Right,
                Width = 280,
                BackColor = Color.White,
                Padding = new Padding(5)
            };
            Controls.Add(pnlRight);

            int y = 10;

            // Sort order radios
            rbAscending = new RadioButton
            {
                Text = "正序",
                Location = new Point(20, y),
                AutoSize = true
            };
            pnlRight.Controls.Add(rbAscending);

            rbDescending = new RadioButton
            {
                Text = "倒序",
                Location = new Point(90, y),
                AutoSize = true,
                Checked = true
            };
            pnlRight.Controls.Add(rbDescending);

            y += 35;

            // Query button
            btnQuery = new Button
            {
                Text = "开始查询",
                Location = new Point(70, y),
                Size = new Size(100, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White
            };
            btnQuery.FlatAppearance.BorderColor = BorderColor;
            btnQuery.Click += BtnQuery_Click;
            pnlRight.Controls.Add(btnQuery);

            y += 45;

            // Export button
            btnExport = new Button
            {
                Text = "导出",
                Location = new Point(70, y),
                Size = new Size(100, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White
            };
            btnExport.FlatAppearance.BorderColor = BorderColor;
            btnExport.Click += BtnExport_Click;
            pnlRight.Controls.Add(btnExport);

            y += 45;

            // Attack section
            lblAttackTitle = new Label
            {
                Text = "攻击",
                Location = new Point(5, y),
                AutoSize = true
            };
            pnlRight.Controls.Add(lblAttackTitle);

            y += 20;

            txtAttack = new TextBox
            {
                Location = new Point(5, y),
                Size = new Size(215, 150),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Microsoft YaHei UI", 8F)
            };
            pnlRight.Controls.Add(txtAttack);

            btnCopyAttack = new Button
            {
                Text = "复制",
                Location = new Point(225, y),
                Size = new Size(50, 24),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White
            };
            btnCopyAttack.FlatAppearance.BorderColor = BorderColor;
            btnCopyAttack.Click += BtnCopyAttack_Click;
            pnlRight.Controls.Add(btnCopyAttack);

            y += 160;

            // Bill section
            lblBillTitle = new Label
            {
                Text = "账单",
                Location = new Point(5, y),
                AutoSize = true
            };
            pnlRight.Controls.Add(lblBillTitle);

            y += 20;

            txtBill = new TextBox
            {
                Location = new Point(5, y),
                Size = new Size(215, 150),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Microsoft YaHei UI", 8F)
            };
            pnlRight.Controls.Add(txtBill);

            btnCopyBill = new Button
            {
                Text = "复制",
                Location = new Point(225, y),
                Size = new Size(50, 24),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White
            };
            btnCopyBill.FlatAppearance.BorderColor = BorderColor;
            btnCopyBill.Click += BtnCopyBill_Click;
            pnlRight.Controls.Add(btnCopyBill);
        }

        private void Dgv_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var col = dgv.Columns[e.ColumnIndex].Name;
            if (col == "BankerProfit")
            {
                var s = e.Value?.ToString() ?? "";
                if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ||
                    decimal.TryParse(s, out v))
                {
                    e.CellStyle.ForeColor = v >= 0 ? Color.FromArgb(0, 128, 0) : Color.FromArgb(200, 0, 0);
                }
            }
        }

        private void Dgv_SelectionChanged(object sender, EventArgs e)
        {
            if (dgv.SelectedRows.Count == 0) return;

            var row = dgv.SelectedRows[0];
            var periodNo = row.Cells["PeriodNo"].Value?.ToString() ?? "";

            // Update titles
            lblAttackTitle.Text = $"{periodNo}攻击";
            lblBillTitle.Text = $"{periodNo}账单";

            // Find record and display details
            var record = _allRecords.FirstOrDefault(r => r.PeriodNo == periodNo);
            if (record != null)
            {
                txtAttack.Text = record.AttackDetails;
                txtBill.Text = record.BillDetails;
            }
            else
            {
                txtAttack.Text = "";
                txtBill.Text = "";
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

            var fileName = $"每期盈利_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            var savePath = Path.Combine(DataService.Instance.DatabaseDir, fileName);

            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("开奖时间,期号,开奖码,彩种,庄家盈利,类型");
                foreach (DataGridViewRow row in dgv.Rows)
                {
                    sb.AppendLine($"{row.Cells[0].Value},{row.Cells[1].Value},{row.Cells[2].Value},{row.Cells[3].Value},{row.Cells[4].Value},{row.Cells[5].Value}");
                }
                File.WriteAllText(savePath, sb.ToString(), Encoding.UTF8);
                MessageBox.Show($"已导出到：\r\n{savePath}", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导出失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnCopyAttack_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(txtAttack.Text))
            {
                Clipboard.SetText(txtAttack.Text);
                MessageBox.Show("已复制攻击详情", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BtnCopyBill_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(txtBill.Text))
            {
                Clipboard.SetText(txtBill.Text);
                MessageBox.Show("已复制账单详情", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void LoadRecords()
        {
            _allRecords.Clear();
            var ds = DataService.Instance;

            try
            {
                // Load from 每期盈利.db file
                var dbPath = Path.Combine(ds.DatabaseDir, "每期盈利.db");
                if (File.Exists(dbPath))
                {
                    var lines = File.ReadAllLines(dbPath, Encoding.UTF8);
                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        var parts = line.Split('|');
                        if (parts.Length >= 5)
                        {
                            var timeStr = parts[0];
                            if (DateTime.TryParse(timeStr, out var dt))
                            {
                                if (dt >= _startTime && dt <= _endTime)
                                {
                                    var record = new PeriodProfitRecord
                                    {
                                        DrawTime = dt,
                                        PeriodNo = parts[1],
                                        DrawCode = parts[2],
                                        LotteryType = parts[3],
                                        BankerProfit = ParseDec(parts[4], 0),
                                        Type = parts.Length > 5 ? parts[5] : "加拿大",
                                        AttackDetails = parts.Length > 6 ? parts[6].Replace("\\n", "\r\n") : "",
                                        BillDetails = parts.Length > 7 ? parts[7].Replace("\\n", "\r\n") : ""
                                    };
                                    _allRecords.Add(record);
                                }
                            }
                        }
                    }
                }

                // Sort
                if (rbDescending.Checked)
                    _allRecords = _allRecords.OrderByDescending(r => r.DrawTime).ToList();
                else
                    _allRecords = _allRecords.OrderBy(r => r.DrawTime).ToList();

                RenderRecords(_allRecords);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载数据失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RenderRecords(List<PeriodProfitRecord> records)
        {
            dgv.Rows.Clear();
            foreach (var r in records)
            {
                dgv.Rows.Add(
                    r.DrawTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    r.PeriodNo,
                    r.DrawCode,
                    r.LotteryType,
                    r.BankerProfit.ToString(CultureInfo.InvariantCulture),
                    r.Type
                );
            }

            // Clear details
            txtAttack.Text = "";
            txtBill.Text = "";
            lblAttackTitle.Text = "攻击";
            lblBillTitle.Text = "账单";
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

    internal class PeriodProfitRecord
    {
        public DateTime DrawTime { get; set; }
        public string PeriodNo { get; set; } = "";
        public string DrawCode { get; set; } = "";
        public string LotteryType { get; set; } = "";
        public decimal BankerProfit { get; set; }
        public string Type { get; set; } = "";
        public string AttackDetails { get; set; } = "";
        public string BillDetails { get; set; } = "";
    }
}

