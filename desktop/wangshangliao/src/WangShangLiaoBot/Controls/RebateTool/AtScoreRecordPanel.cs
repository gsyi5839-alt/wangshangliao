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
    /// 艾特分记录面板 - At Score Record Panel
    /// </summary>
    public sealed class AtScoreRecordPanel : UserControl
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
        private Label lblTotalAtUp;
        private Label lblTotalAtDown;
        private Label lblSearchTitle;
        private TextBox txtSearch;
        private Button btnSearch;

        // Data
        private List<AtScoreRecord> _allRecords = new List<AtScoreRecord>();
        private DateTime _startTime = DateTime.Today;
        private DateTime _endTime = DateTime.Now;

        public AtScoreRecordPanel()
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
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "OperationTime", HeaderText = "操作时间", Width = 130 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "WangwangId", HeaderText = "旺旺号", Width = 85 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Nickname", HeaderText = "昵称", Width = 50 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Action", HeaderText = "动作", Width = 65 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Score", HeaderText = "分数", Width = 50 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "BillScore", HeaderText = "账单分", Width = 60 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Reason", HeaderText = "理由", Width = 50 });

            dgv.CellFormatting += Dgv_CellFormatting;

            Controls.Add(dgv);
            dgv.BringToFront();
        }

        private void CreateRightPanel()
        {
            pnlRight = new Panel
            {
                Dock = DockStyle.Right,
                Width = 200,
                BackColor = Color.White,
                Padding = new Padding(10)
            };
            Controls.Add(pnlRight);

            int y = 10;

            // Sort order radios
            rbAscending = new RadioButton
            {
                Text = "正序",
                Location = new Point(15, y),
                AutoSize = true
            };
            pnlRight.Controls.Add(rbAscending);

            rbDescending = new RadioButton
            {
                Text = "倒序",
                Location = new Point(85, y),
                AutoSize = true,
                Checked = true
            };
            pnlRight.Controls.Add(rbDescending);

            y += 35;

            // Query button
            btnQuery = new Button
            {
                Text = "开始查询",
                Location = new Point(35, y),
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
                Location = new Point(35, y),
                Size = new Size(100, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White
            };
            btnExport.FlatAppearance.BorderColor = BorderColor;
            btnExport.Click += BtnExport_Click;
            pnlRight.Controls.Add(btnExport);

            y += 50;

            // Statistics labels
            lblTotalAtUp = new Label
            {
                Text = "总艾特上分：0",
                Location = new Point(10, y),
                AutoSize = true
            };
            pnlRight.Controls.Add(lblTotalAtUp);

            y += 22;

            lblTotalAtDown = new Label
            {
                Text = "总艾特下分：0",
                Location = new Point(10, y),
                AutoSize = true
            };
            pnlRight.Controls.Add(lblTotalAtDown);

            y += 40;

            // Search section
            lblSearchTitle = new Label
            {
                Text = "单独查询旺旺号",
                Location = new Point(10, y),
                AutoSize = true
            };
            pnlRight.Controls.Add(lblSearchTitle);

            y += 22;

            txtSearch = new TextBox
            {
                Location = new Point(10, y),
                Size = new Size(150, 23),
                BorderStyle = BorderStyle.FixedSingle
            };
            pnlRight.Controls.Add(txtSearch);

            y += 35;

            btnSearch = new Button
            {
                Text = "开始搜索",
                Location = new Point(35, y),
                Size = new Size(100, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White
            };
            btnSearch.FlatAppearance.BorderColor = BorderColor;
            btnSearch.Click += BtnSearch_Click;
            pnlRight.Controls.Add(btnSearch);
        }

        private void Dgv_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var col = dgv.Columns[e.ColumnIndex].Name;
            if (col == "Action")
            {
                var val = e.Value?.ToString() ?? "";
                if (val.Contains("上分"))
                    e.CellStyle.ForeColor = Color.Blue;
                else if (val.Contains("下分"))
                    e.CellStyle.ForeColor = Color.FromArgb(200, 0, 0);
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

            var fileName = $"艾特分记录_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            var savePath = Path.Combine(DataService.Instance.DatabaseDir, fileName);

            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("操作时间,旺旺号,昵称,动作,分数,账单分,理由");
                foreach (DataGridViewRow row in dgv.Rows)
                {
                    sb.AppendLine($"{row.Cells[0].Value},{row.Cells[1].Value},{row.Cells[2].Value},{row.Cells[3].Value},{row.Cells[4].Value},{row.Cells[5].Value},{row.Cells[6].Value}");
                }
                File.WriteAllText(savePath, sb.ToString(), Encoding.UTF8);
                MessageBox.Show($"已导出到：\r\n{savePath}", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导出失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnSearch_Click(object sender, EventArgs e)
        {
            var searchText = txtSearch.Text.Trim();
            if (string.IsNullOrEmpty(searchText))
            {
                RenderRecords(_allRecords);
                return;
            }

            var filtered = _allRecords.Where(r =>
                r.WangwangId.Contains(searchText) ||
                r.Nickname.Contains(searchText)).ToList();

            RenderRecords(filtered);
        }

        private void LoadRecords()
        {
            _allRecords.Clear();
            var ds = DataService.Instance;

            try
            {
                // Load from 艾特分.db file
                var dbPath = Path.Combine(ds.DatabaseDir, "艾特分.db");
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
                                    var record = new AtScoreRecord
                                    {
                                        OperationTime = dt,
                                        WangwangId = parts[1],
                                        Nickname = parts[2],
                                        Action = parts[3],
                                        Score = ParseDec(parts[4], 0),
                                        BillScore = parts.Length > 5 ? ParseDec(parts[5], 0) : ParseDec(parts[4], 0),
                                        Reason = parts.Length > 6 ? parts[6] : "无理"
                                    };
                                    _allRecords.Add(record);
                                }
                            }
                        }
                    }
                }

                // Sort
                if (rbDescending.Checked)
                    _allRecords = _allRecords.OrderByDescending(r => r.OperationTime).ToList();
                else
                    _allRecords = _allRecords.OrderBy(r => r.OperationTime).ToList();

                RenderRecords(_allRecords);
                UpdateStatistics();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载数据失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RenderRecords(List<AtScoreRecord> records)
        {
            dgv.Rows.Clear();
            foreach (var r in records)
            {
                dgv.Rows.Add(
                    r.OperationTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    r.WangwangId,
                    r.Nickname,
                    r.Action,
                    r.Score.ToString(CultureInfo.InvariantCulture),
                    r.BillScore.ToString(CultureInfo.InvariantCulture),
                    r.Reason
                );
            }
        }

        private void UpdateStatistics()
        {
            var upRecords = _allRecords.Where(r => r.Action.Contains("上分")).ToList();
            var downRecords = _allRecords.Where(r => r.Action.Contains("下分")).ToList();

            decimal totalUp = upRecords.Sum(r => r.Score);
            decimal totalDown = downRecords.Sum(r => r.Score);

            lblTotalAtUp.Text = $"总艾特上分：{totalUp}";
            lblTotalAtDown.Text = $"总艾特下分：{totalDown}";
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

    internal class AtScoreRecord
    {
        public DateTime OperationTime { get; set; }
        public string WangwangId { get; set; } = "";
        public string Nickname { get; set; } = "";
        public string Action { get; set; } = "";
        public decimal Score { get; set; }
        public decimal BillScore { get; set; }
        public string Reason { get; set; } = "";
    }
}

