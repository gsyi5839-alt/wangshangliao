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
    /// 单独查询面板 - Individual player query panel
    /// </summary>
    public sealed class SingleQueryPanel : UserControl
    {
        private readonly Color BorderColor = Color.FromArgb(180, 180, 180);
        private readonly Color HeaderBgColor = Color.FromArgb(240, 240, 240);
        private readonly Color RedTextColor = Color.FromArgb(200, 0, 0);

        // Left panel controls
        private Panel pnlLeft;
        private Label lblSearchTitle;
        private TextBox txtWangwangId;
        private Button btnSearch;
        private RadioButton rbAsc;
        private RadioButton rbDesc;
        private Button btnExport;
        private DataGridView dgvResults;

        // Right panel controls
        private Panel pnlRight;
        private Panel pnlPlayerInfo;
        private Panel pnlTimeDetails;

        // Player info labels
        private Label lblName;
        private Label lblAccountId;
        private Label lblBalance;
        private Label lblUpScore;
        private Label lblDownScore;
        private Label lblScoreDiff;
        private Label lblActivityScore;
        private Label lblActualProfit;
        private Label lblBetProfit;
        private Label lblBetCount;
        private Label lblTotalBet;
        private Label lblIgnoreCount;
        private Label lblIgnoreReturnCount;
        private Label lblMaxSingleBet;
        private Label lblFlowRatio;
        private Label lblWinRate;
        private Label lblBigSmallRate;
        private Label lblBigDoubleSmallSingleRate;
        private Label lblBigSingleSmallDoubleRate;
        private Label lblSpecialCodeRate;
        private Label lblLeopardSeqPairRate;
        private Label lblDragonTigerRate;
        private Label lblTailBigSmallRate;
        private Label lblTailComboRate;
        private Label lblFrontBigSmallRate;
        private Label lblFrontComboRate;

        // Time details
        private Label lblTimeDetailsTitle;
        private TextBox txtTimeDetails;

        // Data
        private DateTime _startTime = DateTime.Today;
        private DateTime _endTime = DateTime.Now;

        public SingleQueryPanel()
        {
            SuspendLayout();
            BackColor = Color.White;
            Dock = DockStyle.Fill;
            Font = new Font("Microsoft YaHei UI", 9F);

            CreateLeftPanel();
            CreateRightPanel();

            ResumeLayout(false);
        }

        private void CreateLeftPanel()
        {
            pnlLeft = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(5)
            };
            Controls.Add(pnlLeft);

            // Search title
            lblSearchTitle = new Label
            {
                Text = "搜索旺旺号",
                Location = new Point(5, 8),
                AutoSize = true
            };
            pnlLeft.Controls.Add(lblSearchTitle);

            // Search textbox
            txtWangwangId = new TextBox
            {
                Location = new Point(10, 28),
                Size = new Size(120, 23)
            };
            pnlLeft.Controls.Add(txtWangwangId);

            // Search button
            btnSearch = new Button
            {
                Text = "开始搜索",
                Location = new Point(10, 55),
                Size = new Size(80, 26),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White
            };
            btnSearch.FlatAppearance.BorderColor = BorderColor;
            btnSearch.Click += BtnSearch_Click;
            pnlLeft.Controls.Add(btnSearch);

            // Sort radio buttons
            rbAsc = new RadioButton
            {
                Text = "正序",
                Location = new Point(10, 90),
                AutoSize = true,
                Checked = true
            };
            pnlLeft.Controls.Add(rbAsc);

            rbDesc = new RadioButton
            {
                Text = "倒序",
                Location = new Point(70, 90),
                AutoSize = true
            };
            pnlLeft.Controls.Add(rbDesc);

            // Export button
            btnExport = new Button
            {
                Text = "导出明细",
                Location = new Point(140, 55),
                Size = new Size(80, 26),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White
            };
            btnExport.FlatAppearance.BorderColor = BorderColor;
            btnExport.Click += BtnExport_Click;
            pnlLeft.Controls.Add(btnExport);

            // Results grid
            dgvResults = new DataGridView
            {
                Location = new Point(5, 118),
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
                ScrollBars = ScrollBars.Both,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left
            };

            dgvResults.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = HeaderBgColor,
                ForeColor = Color.Black,
                Font = new Font("Microsoft YaHei UI", 9F),
                Alignment = DataGridViewContentAlignment.MiddleCenter,
                SelectionBackColor = HeaderBgColor,
                SelectionForeColor = Color.Black
            };

            dgvResults.DefaultCellStyle = new DataGridViewCellStyle
            {
                Font = new Font("Microsoft YaHei UI", 9F),
                SelectionBackColor = Color.FromArgb(0, 120, 215),
                SelectionForeColor = Color.White
            };

            // Add columns
            dgvResults.Columns.Add(new DataGridViewTextBoxColumn { Name = "LotteryResult", HeaderText = "开奖结果", Width = 180 });
            dgvResults.Columns.Add(new DataGridViewTextBoxColumn { Name = "Score", HeaderText = "分数", Width = 55 });
            dgvResults.Columns.Add(new DataGridViewTextBoxColumn { Name = "BetContent", HeaderText = "下注内容", Width = 120 });
            dgvResults.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProfitLoss", HeaderText = "盈亏", Width = 55 });
            dgvResults.Columns.Add(new DataGridViewTextBoxColumn { Name = "Total", HeaderText = "总", Width = 40 });

            dgvResults.CellFormatting += DgvResults_CellFormatting;
            pnlLeft.Controls.Add(dgvResults);

            // Resize handler for grid
            pnlLeft.Resize += (s, e) =>
            {
                dgvResults.Size = new Size(pnlLeft.Width - 10, pnlLeft.Height - dgvResults.Top - 10);
            };
            dgvResults.Size = new Size(450, 300);
        }

        private void CreateRightPanel()
        {
            pnlRight = new Panel
            {
                Dock = DockStyle.Right,
                Width = 330,
                Padding = new Padding(5)
            };
            Controls.Add(pnlRight);

            // Player info panel with border
            pnlPlayerInfo = new Panel
            {
                Location = new Point(5, 5),
                BorderStyle = BorderStyle.FixedSingle,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            pnlRight.Controls.Add(pnlPlayerInfo);

            CreatePlayerInfoLabels();

            // Time details panel with border
            pnlTimeDetails = new Panel
            {
                BorderStyle = BorderStyle.FixedSingle,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            pnlRight.Controls.Add(pnlTimeDetails);

            lblTimeDetailsTitle = new Label
            {
                Text = "详细分数变动时间:",
                Location = new Point(5, 5),
                AutoSize = true,
                ForeColor = Color.Black
            };
            pnlTimeDetails.Controls.Add(lblTimeDetailsTitle);

            txtTimeDetails = new TextBox
            {
                Location = new Point(5, 25),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BorderStyle = BorderStyle.None,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            pnlTimeDetails.Controls.Add(txtTimeDetails);

            // Resize handler
            pnlRight.Resize += (s, e) =>
            {
                pnlPlayerInfo.Size = new Size(pnlRight.Width - 10, 240);
                pnlTimeDetails.Location = new Point(5, pnlPlayerInfo.Bottom + 10);
                pnlTimeDetails.Size = new Size(pnlRight.Width - 10, pnlRight.Height - pnlTimeDetails.Top - 10);
                txtTimeDetails.Size = new Size(pnlTimeDetails.Width - 15, pnlTimeDetails.Height - 35);
            };

            pnlPlayerInfo.Size = new Size(320, 240);
            pnlTimeDetails.Location = new Point(5, 255);
            pnlTimeDetails.Size = new Size(320, 200);
            txtTimeDetails.Size = new Size(305, 165);
        }

        private void CreatePlayerInfoLabels()
        {
            int col1X = 5;
            int col2X = 160;
            int y = 5;
            int rowH = 18;

            // Row 1: 名字 / 账号
            lblName = CreateInfoLabel("名字:", col1X, y);
            lblAccountId = CreateInfoLabel("账号:", col2X, y);

            // Row 2: 余额
            y += rowH;
            lblBalance = CreateInfoLabel("余额:", col1X, y);

            // Row 3: 上分 / 下分
            y += rowH;
            lblUpScore = CreateInfoLabel("上分:", col1X, y);
            lblDownScore = CreateInfoLabel("下分:", col2X, y);

            // Row 4: 分差 / 活动分
            y += rowH;
            lblScoreDiff = CreateInfoLabel("分差:", col1X, y);
            lblActivityScore = CreateInfoLabel("活动分:", col2X, y);

            // Row 5: 实际盈利 / 下注盈利
            y += rowH;
            lblActualProfit = CreateInfoLabel("实际盈利:", col1X, y);
            lblBetProfit = CreateInfoLabel("下注盈利:", col2X, y);

            // Row 6: 次数 / 总下注
            y += rowH;
            lblBetCount = CreateInfoLabel("次数:", col1X, y);
            lblTotalBet = CreateInfoLabel("总下注:", col2X, y);

            // Row 7: 超无视次数 / 无视回本次数
            y += rowH;
            lblIgnoreCount = CreateInfoLabel("超无视次数:", col1X, y);
            lblIgnoreReturnCount = CreateInfoLabel("无视回本次数:", col2X, y);

            // Row 8: 最大单注 / 流水比
            y += rowH;
            lblMaxSingleBet = CreateInfoLabel("最大单注:", col1X, y);
            lblFlowRatio = CreateInfoLabel("流水比:", col2X, y);

            // Row 9: 中奖率 / 大小单双率
            y += rowH;
            lblWinRate = CreateInfoLabel("——中奖率:", col1X, y);
            lblBigSmallRate = CreateInfoLabel("大小单双率:", col2X, y);

            // Row 10: 大双小单率 / 大单小双率
            y += rowH;
            lblBigDoubleSmallSingleRate = CreateInfoLabel("大双小单率:", col1X, y);
            lblBigSingleSmallDoubleRate = CreateInfoLabel("大单小双率:", col2X, y);

            // Row 11: 极数特码率 / 豹顺对子率
            y += rowH;
            lblSpecialCodeRate = CreateInfoLabel("极数特码率:", col1X, y);
            lblLeopardSeqPairRate = CreateInfoLabel("豹顺对子率:", col2X, y);

            // Row 12: 一龙虎豹率 / 尾大小单双率
            y += rowH;
            lblDragonTigerRate = CreateInfoLabel("一龙虎豹率:", col1X, y);
            lblTailBigSmallRate = CreateInfoLabel("尾大小单双率:", col2X, y);

            // Row 13: 尾组合率 / 前大小单双率
            y += rowH;
            lblTailComboRate = CreateInfoLabel("尾组合率:", col1X, y);
            lblFrontBigSmallRate = CreateInfoLabel("前大小单双率:", col2X, y);

            // Row 14: 前组合率
            y += rowH;
            lblFrontComboRate = CreateInfoLabel("前组合率:", col1X, y);
        }

        private Label CreateInfoLabel(string prefix, int x, int y)
        {
            var lbl = new Label
            {
                Text = prefix,
                Location = new Point(x, y),
                AutoSize = true,
                ForeColor = RedTextColor
            };
            pnlPlayerInfo.Controls.Add(lbl);
            return lbl;
        }

        private void DgvResults_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var col = dgvResults.Columns[e.ColumnIndex].Name;
            if (col == "ProfitLoss" || col == "Total")
            {
                var s = e.Value?.ToString() ?? "";
                if (decimal.TryParse(s.Replace("+", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ||
                    decimal.TryParse(s.Replace("+", ""), out v))
                {
                    e.CellStyle.ForeColor = v >= 0 ? Color.FromArgb(0, 128, 0) : Color.FromArgb(200, 0, 0);
                }
            }
        }

        private void BtnSearch_Click(object sender, EventArgs e)
        {
            var wangwangId = txtWangwangId.Text.Trim();
            if (string.IsNullOrEmpty(wangwangId))
            {
                MessageBox.Show("请输入旺旺号", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            LoadPlayerData(wangwangId);
        }

        private void BtnExport_Click(object sender, EventArgs e)
        {
            if (dgvResults.Rows.Count == 0)
            {
                MessageBox.Show("没有数据可导出", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var wangwangId = txtWangwangId.Text.Trim();
            var fileName = $"单独查询_{wangwangId}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            var savePath = Path.Combine(DataService.Instance.DatabaseDir, fileName);

            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("开奖结果,分数,下注内容,盈亏,总");
                foreach (DataGridViewRow row in dgvResults.Rows)
                {
                    sb.AppendLine($"{row.Cells[0].Value},{row.Cells[1].Value},{row.Cells[2].Value},{row.Cells[3].Value},{row.Cells[4].Value}");
                }
                File.WriteAllText(savePath, sb.ToString(), Encoding.UTF8);
                MessageBox.Show($"导出成功: {savePath}", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导出失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadPlayerData(string wangwangId)
        {
            var ds = DataService.Instance;
            var player = ds.GetPlayer(wangwangId);

            // Clear previous data
            dgvResults.Rows.Clear();
            ClearPlayerInfo();

            if (player == null)
            {
                MessageBox.Show("未找到该玩家", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Load player info
            lblName.Text = $"名字:{player.Nickname}";
            lblAccountId.Text = $"账号:{wangwangId}";
            lblBalance.Text = $"余额:{player.Score}";

            // Calculate up/down scores from 上下分.db
            var (upTotal, downTotal, timeDetails) = LoadUpDownScores(wangwangId);
            lblUpScore.Text = $"上分:{upTotal}";
            lblDownScore.Text = $"下分:{downTotal}";
            lblScoreDiff.Text = $"分差:{upTotal - downTotal}";
            lblActivityScore.Text = "活动分:0";

            // Load bet statistics
            var betStats = LoadBetStatistics(wangwangId);
            lblActualProfit.Text = $"实际盈利:{betStats.ActualProfit}";
            lblBetProfit.Text = $"下注盈利:{betStats.BetProfit}";
            lblBetCount.Text = $"次数:{betStats.BetCount}";
            lblTotalBet.Text = $"总下注:{betStats.TotalBet}";
            lblIgnoreCount.Text = $"超无视次数:{betStats.IgnoreCount}";
            lblIgnoreReturnCount.Text = $"无视回本次数:{betStats.IgnoreReturnCount}";
            lblMaxSingleBet.Text = $"最大单注:{betStats.MaxSingleBet}";
            lblFlowRatio.Text = $"流水比:{betStats.FlowRatio:F1}";
            lblWinRate.Text = $"——中奖率:{betStats.WinRate:F2}%";
            lblBigSmallRate.Text = $"大小单双率:{betStats.BigSmallRate:F2}%";
            lblBigDoubleSmallSingleRate.Text = $"大双小单率:{betStats.BigDoubleSmallSingleRate:F2}%";
            lblBigSingleSmallDoubleRate.Text = $"大单小双率:{betStats.BigSingleSmallDoubleRate:F2}%";
            lblSpecialCodeRate.Text = $"极数特码率:{betStats.SpecialCodeRate:F0}%";
            lblLeopardSeqPairRate.Text = $"豹顺对子率:{betStats.LeopardSeqPairRate:F0}%";
            lblDragonTigerRate.Text = $"一龙虎豹率:{betStats.DragonTigerRate:F0}%";
            lblTailBigSmallRate.Text = $"尾大小单双率:{betStats.TailBigSmallRate:F0}%";
            lblTailComboRate.Text = $"尾组合率:{betStats.TailComboRate:F0}%";
            lblFrontBigSmallRate.Text = $"前大小单双率:{betStats.FrontBigSmallRate:F0}%";
            lblFrontComboRate.Text = $"前组合率:{betStats.FrontComboRate:F0}%";

            // Load time details
            txtTimeDetails.Text = timeDetails + $"\r\n总共上分{upTotal}, 下分{downTotal}, 分差+{upTotal - downTotal}";

            // Load bet records into grid
            LoadBetRecords(wangwangId);
        }

        private void ClearPlayerInfo()
        {
            lblName.Text = "名字:";
            lblAccountId.Text = "账号:";
            lblBalance.Text = "余额:";
            lblUpScore.Text = "上分:";
            lblDownScore.Text = "下分:";
            lblScoreDiff.Text = "分差:";
            lblActivityScore.Text = "活动分:";
            lblActualProfit.Text = "实际盈利:";
            lblBetProfit.Text = "下注盈利:";
            lblBetCount.Text = "次数:";
            lblTotalBet.Text = "总下注:";
            lblIgnoreCount.Text = "超无视次数:";
            lblIgnoreReturnCount.Text = "无视回本次数:";
            lblMaxSingleBet.Text = "最大单注:";
            lblFlowRatio.Text = "流水比:";
            lblWinRate.Text = "——中奖率:";
            lblBigSmallRate.Text = "大小单双率:";
            lblBigDoubleSmallSingleRate.Text = "大双小单率:";
            lblBigSingleSmallDoubleRate.Text = "大单小双率:";
            lblSpecialCodeRate.Text = "极数特码率:";
            lblLeopardSeqPairRate.Text = "豹顺对子率:";
            lblDragonTigerRate.Text = "一龙虎豹率:";
            lblTailBigSmallRate.Text = "尾大小单双率:";
            lblTailComboRate.Text = "尾组合率:";
            lblFrontBigSmallRate.Text = "前大小单双率:";
            lblFrontComboRate.Text = "前组合率:";
            txtTimeDetails.Text = "";
        }

        private (decimal up, decimal down, string details) LoadUpDownScores(string wangwangId)
        {
            decimal up = 0, down = 0;
            var sb = new StringBuilder();

            try
            {
                var scoreDb = Path.Combine(DataService.Instance.GroupMemberCacheDir, "..", "上下分.db");
                scoreDb = Path.GetFullPath(scoreDb);
                if (!File.Exists(scoreDb)) return (0, 0, "");

                var lines = File.ReadAllLines(scoreDb, Encoding.UTF8);
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var parts = line.Split('|');
                    if (parts.Length < 6) continue;

                    var id = parts[1]?.Trim() ?? "";
                    if (id != wangwangId) continue;

                    if (!DateTime.TryParse(parts[0], out var time)) continue;
                    if (time < _startTime || time > _endTime) continue;

                    var type = parts[3] ?? "";
                    decimal.TryParse(parts[4], NumberStyles.Any, CultureInfo.InvariantCulture, out var amt);
                    if (amt == 0) decimal.TryParse(parts[4], out amt);
                    amt = Math.Abs(amt);

                    if (type.Contains("上"))
                    {
                        up += amt;
                        sb.AppendLine($"{time:yyyy-MM-dd HH:mm:ss}>>上分, {amt}");
                    }
                    else if (type.Contains("下"))
                    {
                        down += amt;
                        sb.AppendLine($"{time:yyyy-MM-dd HH:mm:ss}>>下分, {amt}");
                    }
                }
            }
            catch { }

            return (up, down, sb.ToString().TrimEnd());
        }

        private BetStatistics LoadBetStatistics(string wangwangId)
        {
            var stats = new BetStatistics();

            try
            {
                var baseDir = Path.Combine(DataService.Instance.DatabaseDir, "Bets");
                if (!Directory.Exists(baseDir)) return stats;

                var betList = new List<BetRecord>();

                for (var day = _startTime.Date; day <= _endTime.Date; day = day.AddDays(1))
                {
                    var dayDir = Path.Combine(baseDir, day.ToString("yyyy-MM-dd"));
                    if (!Directory.Exists(dayDir)) continue;

                    foreach (var teamDir in Directory.GetDirectories(dayDir))
                    {
                        foreach (var betFile in Directory.GetFiles(teamDir, "bets-*.txt"))
                        {
                            var lines = File.ReadAllLines(betFile, Encoding.UTF8);
                            foreach (var line in lines)
                            {
                                if (string.IsNullOrWhiteSpace(line)) continue;
                                var parts = line.Split('\t');
                                if (parts.Length < 9) continue;

                                var playerId = parts[3];
                                if (playerId != wangwangId) continue;

                                decimal.TryParse(parts[6], NumberStyles.Any, CultureInfo.InvariantCulture, out var betAmt);
                                decimal.TryParse(parts[8], NumberStyles.Any, CultureInfo.InvariantCulture, out var winAmt);

                                betList.Add(new BetRecord
                                {
                                    BetAmount = betAmt,
                                    WinAmount = winAmt
                                });

                                stats.TotalBet += betAmt;
                                stats.BetProfit += winAmt;
                                stats.BetCount++;
                                if (betAmt > stats.MaxSingleBet) stats.MaxSingleBet = betAmt;
                                if (winAmt > 0) stats.WinCount++;
                            }
                        }
                    }
                }

                if (stats.BetCount > 0)
                {
                    stats.WinRate = (decimal)stats.WinCount / stats.BetCount * 100;
                    stats.FlowRatio = stats.TotalBet > 0 ? stats.TotalBet / Math.Max(1, Math.Abs(stats.BetProfit)) : 0;
                }
            }
            catch { }

            return stats;
        }

        private void LoadBetRecords(string wangwangId)
        {
            var records = new List<BetDisplayRecord>();

            try
            {
                var baseDir = Path.Combine(DataService.Instance.DatabaseDir, "Bets");
                if (!Directory.Exists(baseDir)) return;

                for (var day = _startTime.Date; day <= _endTime.Date; day = day.AddDays(1))
                {
                    var dayDir = Path.Combine(baseDir, day.ToString("yyyy-MM-dd"));
                    if (!Directory.Exists(dayDir)) continue;

                    foreach (var teamDir in Directory.GetDirectories(dayDir))
                    {
                        foreach (var betFile in Directory.GetFiles(teamDir, "bets-*.txt"))
                        {
                            var period = Path.GetFileNameWithoutExtension(betFile).Substring("bets-".Length);
                            var lines = File.ReadAllLines(betFile, Encoding.UTF8);

                            foreach (var line in lines)
                            {
                                if (string.IsNullOrWhiteSpace(line)) continue;
                                var parts = line.Split('\t');
                                if (parts.Length < 9) continue;

                                var playerId = parts[3];
                                if (playerId != wangwangId) continue;

                                decimal.TryParse(parts[6], NumberStyles.Any, CultureInfo.InvariantCulture, out var betAmt);
                                decimal.TryParse(parts[8], NumberStyles.Any, CultureInfo.InvariantCulture, out var winAmt);

                                var lotteryResult = parts.Length > 9 ? parts[9] : "";
                                var betContent = parts[5];

                                records.Add(new BetDisplayRecord
                                {
                                    Period = period,
                                    LotteryResult = lotteryResult,
                                    Score = betAmt,
                                    BetContent = betContent,
                                    ProfitLoss = winAmt,
                                    Total = winAmt
                                });
                            }
                        }
                    }
                }

                // Also load up/down records
                var scoreDb = Path.Combine(DataService.Instance.GroupMemberCacheDir, "..", "上下分.db");
                scoreDb = Path.GetFullPath(scoreDb);
                if (File.Exists(scoreDb))
                {
                    var lines = File.ReadAllLines(scoreDb, Encoding.UTF8);
                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        var parts = line.Split('|');
                        if (parts.Length < 6) continue;

                        var id = parts[1]?.Trim() ?? "";
                        if (id != wangwangId) continue;

                        if (!DateTime.TryParse(parts[0], out var time)) continue;
                        if (time < _startTime || time > _endTime) continue;

                        var type = parts[3] ?? "";
                        decimal.TryParse(parts[4], NumberStyles.Any, CultureInfo.InvariantCulture, out var amt);
                        if (amt == 0) decimal.TryParse(parts[4], out amt);

                        var prefix = type.Contains("上") ? "+" : "";
                        records.Add(new BetDisplayRecord
                        {
                            Period = $"{time:yyyy-MM-dd HH:mm:ss}  {type}",
                            LotteryResult = "",
                            Score = amt,
                            BetContent = type.Contains("上") ? $"c{amt}" : $"-{amt}",
                            ProfitLoss = type.Contains("上") ? amt : -amt,
                            Total = type.Contains("上") ? amt : -amt,
                            SortTime = time
                        });
                    }
                }
            }
            catch { }

            // Sort
            if (rbAsc.Checked)
                records = records.OrderBy(r => r.Period).ToList();
            else
                records = records.OrderByDescending(r => r.Period).ToList();

            // Add to grid
            decimal runningTotal = 0;
            foreach (var r in records)
            {
                runningTotal += r.ProfitLoss;
                dgvResults.Rows.Add(
                    r.Period + (string.IsNullOrEmpty(r.LotteryResult) ? "" : $" {r.LotteryResult}"),
                    r.Score,
                    r.BetContent,
                    (r.ProfitLoss >= 0 ? "+" : "") + r.ProfitLoss,
                    (runningTotal >= 0 ? "+" : "") + runningTotal
                );
            }
        }

        public void RefreshData(DateTime startTime, DateTime endTime)
        {
            _startTime = startTime;
            _endTime = endTime;

            // Re-search if we have a wangwang ID
            if (!string.IsNullOrEmpty(txtWangwangId.Text.Trim()))
            {
                LoadPlayerData(txtWangwangId.Text.Trim());
            }
        }
    }

    internal class BetStatistics
    {
        public decimal ActualProfit { get; set; }
        public decimal BetProfit { get; set; }
        public int BetCount { get; set; }
        public decimal TotalBet { get; set; }
        public int IgnoreCount { get; set; }
        public int IgnoreReturnCount { get; set; }
        public decimal MaxSingleBet { get; set; }
        public decimal FlowRatio { get; set; }
        public int WinCount { get; set; }
        public decimal WinRate { get; set; }
        public decimal BigSmallRate { get; set; }
        public decimal BigDoubleSmallSingleRate { get; set; }
        public decimal BigSingleSmallDoubleRate { get; set; }
        public decimal SpecialCodeRate { get; set; }
        public decimal LeopardSeqPairRate { get; set; }
        public decimal DragonTigerRate { get; set; }
        public decimal TailBigSmallRate { get; set; }
        public decimal TailComboRate { get; set; }
        public decimal FrontBigSmallRate { get; set; }
        public decimal FrontComboRate { get; set; }
    }

    internal class BetRecord
    {
        public decimal BetAmount { get; set; }
        public decimal WinAmount { get; set; }
    }

    internal class BetDisplayRecord
    {
        public string Period { get; set; }
        public string LotteryResult { get; set; }
        public decimal Score { get; set; }
        public string BetContent { get; set; }
        public decimal ProfitLoss { get; set; }
        public decimal Total { get; set; }
        public DateTime SortTime { get; set; } = DateTime.MinValue;
    }
}

