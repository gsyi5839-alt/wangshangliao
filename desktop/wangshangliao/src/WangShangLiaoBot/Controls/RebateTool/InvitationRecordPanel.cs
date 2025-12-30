using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using WangShangLiaoBot.Services;

namespace WangShangLiaoBot.Controls.RebateTool
{
    /// <summary>
    /// 邀请记录面板 - Invitation Record Panel
    /// </summary>
    public sealed class InvitationRecordPanel : UserControl
    {
        private readonly Color BorderColor = Color.FromArgb(180, 180, 180);
        private readonly Color HeaderBgColor = Color.FromArgb(240, 240, 240);

        // Left - DataGridView
        private DataGridView dgv;

        // Right panel
        private Panel pnlRight;
        private Label lblInviter;
        private TextBox txtInviter;
        private Label lblJoiner;
        private TextBox txtJoiner;
        private Button btnQuery;
        private Button btnExport;

        // Data
        private List<InvitationRecord> _allRecords = new List<InvitationRecord>();
        private DateTime _startTime = DateTime.Today;
        private DateTime _endTime = DateTime.Now;

        public InvitationRecordPanel()
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
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Time", HeaderText = "时间", Width = 130 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "GroupNo", HeaderText = "群号", Width = 100 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Joiner", HeaderText = "进群人", Width = 90 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "JoinerNickname", HeaderText = "昵称", Width = 70 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Inviter", HeaderText = "邀请人", Width = 90 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "InviterNickname", HeaderText = "昵称", Width = 70 });

            Controls.Add(dgv);
            dgv.BringToFront();
        }

        private void CreateRightPanel()
        {
            pnlRight = new Panel
            {
                Dock = DockStyle.Right,
                Width = 180,
                BackColor = Color.White,
                Padding = new Padding(10)
            };
            Controls.Add(pnlRight);

            int y = 15;

            // Inviter search
            lblInviter = new Label
            {
                Text = "邀请人",
                Location = new Point(10, y + 3),
                AutoSize = true
            };
            pnlRight.Controls.Add(lblInviter);

            txtInviter = new TextBox
            {
                Location = new Point(70, y),
                Size = new Size(100, 23),
                BorderStyle = BorderStyle.FixedSingle
            };
            pnlRight.Controls.Add(txtInviter);

            y += 30;

            // Joiner search
            lblJoiner = new Label
            {
                Text = "进群人",
                Location = new Point(10, y + 3),
                AutoSize = true
            };
            pnlRight.Controls.Add(lblJoiner);

            txtJoiner = new TextBox
            {
                Location = new Point(70, y),
                Size = new Size(100, 23),
                BorderStyle = BorderStyle.FixedSingle
            };
            pnlRight.Controls.Add(txtJoiner);

            y += 40;

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

            var fileName = $"邀请记录_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            var savePath = Path.Combine(DataService.Instance.DatabaseDir, fileName);

            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("时间,群号,进群人,昵称,邀请人,昵称");
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

        private void LoadRecords()
        {
            _allRecords.Clear();
            var ds = DataService.Instance;

            try
            {
                // Load from 邀请记录.db file
                var dbPath = Path.Combine(ds.DatabaseDir, "邀请记录.db");
                if (File.Exists(dbPath))
                {
                    var lines = File.ReadAllLines(dbPath, Encoding.UTF8);
                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        var parts = line.Split('|');
                        if (parts.Length >= 6)
                        {
                            var timeStr = parts[0];
                            if (DateTime.TryParse(timeStr, out var dt))
                            {
                                if (dt >= _startTime && dt <= _endTime)
                                {
                                    var record = new InvitationRecord
                                    {
                                        Time = dt,
                                        GroupNo = parts[1],
                                        Joiner = parts[2],
                                        JoinerNickname = parts[3],
                                        Inviter = parts[4],
                                        InviterNickname = parts[5]
                                    };
                                    _allRecords.Add(record);
                                }
                            }
                        }
                    }
                }

                // Filter by search criteria
                var filtered = _allRecords.AsEnumerable();

                var inviterSearch = txtInviter.Text.Trim();
                if (!string.IsNullOrEmpty(inviterSearch))
                {
                    filtered = filtered.Where(r =>
                        r.Inviter.Contains(inviterSearch) ||
                        r.InviterNickname.Contains(inviterSearch));
                }

                var joinerSearch = txtJoiner.Text.Trim();
                if (!string.IsNullOrEmpty(joinerSearch))
                {
                    filtered = filtered.Where(r =>
                        r.Joiner.Contains(joinerSearch) ||
                        r.JoinerNickname.Contains(joinerSearch));
                }

                // Sort by time descending
                var result = filtered.OrderByDescending(r => r.Time).ToList();

                RenderRecords(result);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载数据失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RenderRecords(List<InvitationRecord> records)
        {
            dgv.Rows.Clear();
            foreach (var r in records)
            {
                dgv.Rows.Add(
                    r.Time.ToString("yyyy-MM-dd HH:mm:ss"),
                    r.GroupNo,
                    r.Joiner,
                    r.JoinerNickname,
                    r.Inviter,
                    r.InviterNickname
                );
            }
        }

        public void RefreshData(DateTime startTime, DateTime endTime)
        {
            _startTime = startTime;
            _endTime = endTime;
        }
    }

    internal class InvitationRecord
    {
        public DateTime Time { get; set; }
        public string GroupNo { get; set; } = "";
        public string Joiner { get; set; } = "";
        public string JoinerNickname { get; set; } = "";
        public string Inviter { get; set; } = "";
        public string InviterNickname { get; set; } = "";
    }
}

