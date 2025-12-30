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
    /// Statistics All Panel - 统计所有面板（按设计图：左上统计蓝字 + 中间按钮；左侧上表+下表；右侧顶栏+大边框统计+右下时间）
    /// </summary>
    public class StatisticsAllPanel : UserControl
    {
        // =====================================================
        // Top Area - Summary (left) + Buttons (center)
        // =====================================================
        private Panel pnlTop;
        private Panel pnlTopSummary;
        private Label lblUpDownTotal;
        private Label lblBetProfitTotal;
        private Label lblPlayerCount;
        private Label lblWinLoseStats;
        private Button btnStatistics;
        private Button btnExportRecord;

        // =====================================================
        // Main Area - Left (tables) + Right (details)
        // =====================================================
        private Panel pnlMain;
        private Panel pnlLeft;
        private Panel pnlRight;

        // Left - top table
        private DataGridView dgvPlayers;

        // Left - middle toolbar
        private Panel pnlLeftToolbar;
        private RadioButton rbAscending;
        private RadioButton rbDescending;
        private TextBox txtSearch;
        private Button btnSearch;

        // Left - bottom detail table
        private Panel pnlLeftDetails;
        private Label lblDetailTitle;
        private DataGridView dgvDetails;

        // Right - top bar (export bill + checkbox)
        private Panel pnlRightTopBar;
        private Button btnExportBill;
        private CheckBox chkBillFormat2;

        // Right - bordered content (split into 2 boxes: stats + times)
        private Panel pnlRightStatsBox;
        private Panel pnlRightTimesBox;
        private TableLayoutPanel tlpRightStats;
        private Label lblRightTimesTitle;
        private TabControl tabRightBottom;
        private TabPage tabRightTimes;
        private TabPage tabRightBattle;
        private TextBox txtRightTimes;
        private TextBox txtBattleReport;

        // Data (sample)
        private BindingList<PlayerStatItem> _playerStats;

        // Right value labels by key
        private readonly Dictionary<string, Label> _rightValueLabels = new Dictionary<string, Label>();

        private struct UpDownAgg
        {
            public decimal Up;
            public decimal Down;
        }

        // Colors
        private readonly Color BorderColor = Color.FromArgb(180, 180, 180);
        private readonly Color HeaderBgColor = Color.FromArgb(240, 240, 240);
        private readonly Color SummaryBlue = Color.FromArgb(0, 0, 255);
        private readonly Color RedColor = Color.FromArgb(200, 0, 0);
        private readonly Color GreenColor = Color.FromArgb(0, 128, 0);
        private readonly Color BlueColor = Color.FromArgb(0, 0, 200);

        public StatisticsAllPanel()
        {
            _playerStats = new BindingList<PlayerStatItem>();
            InitializeComponent();
            // Default load: last 24 hours (call RefreshData explicitly from container for accurate range)
            RefreshData(DateTime.Now.Date, DateTime.Now);
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            BackColor = Color.White;
            Dock = DockStyle.Fill;
            Font = new Font("Microsoft YaHei UI", 9F);

            CreateMain();
            CreateTop();

            ResumeLayout(false);
        }

        private void CreateTop()
        {
            pnlTop = new Panel
            {
                Height = 55,
                Dock = DockStyle.Top,
                BackColor = Color.White
            };

            // Left summary block (blue text)
            pnlTopSummary = new Panel
            {
                Dock = DockStyle.Left,
                Width = 240,
                BackColor = Color.FromArgb(245, 245, 245),
                Padding = new Padding(6, 4, 6, 4)
            };

            lblUpDownTotal = new Label
            {
                Text = "上下分总统计: +17646",
                AutoSize = true,
                Location = new Point(6, 4),
                ForeColor = SummaryBlue
            };
            pnlTopSummary.Controls.Add(lblUpDownTotal);

            lblBetProfitTotal = new Label
            {
                Text = "下注盈利统计: +14147",
                AutoSize = true,
                Location = new Point(6, 22),
                ForeColor = SummaryBlue
            };
            pnlTopSummary.Controls.Add(lblBetProfitTotal);

            lblPlayerCount = new Label
            {
                Text = "统计人数: 53",
                AutoSize = true,
                Location = new Point(140, 22),
                ForeColor = SummaryBlue
            };
            pnlTopSummary.Controls.Add(lblPlayerCount);

            lblWinLoseStats = new Label
            {
                Text = "赢: 3人+460    输: 50人-14607",
                AutoSize = true,
                Location = new Point(6, 40),
                ForeColor = SummaryBlue
            };
            pnlTopSummary.Controls.Add(lblWinLoseStats);

            btnStatistics = new Button
            {
                Text = "统计战况",
                Size = new Size(90, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White
            };
            btnStatistics.FlatAppearance.BorderColor = BorderColor;
            btnStatistics.Click += BtnStatistics_Click;
            pnlTop.Controls.Add(btnStatistics);

            btnExportRecord = new Button
            {
                Text = "导出记录",
                Size = new Size(90, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White
            };
            btnExportRecord.FlatAppearance.BorderColor = BorderColor;
            btnExportRecord.Click += BtnExportRecord_Click;
            pnlTop.Controls.Add(btnExportRecord);
            pnlTop.Controls.Add(pnlTopSummary);

            // Layout buttons exactly like design: aligned to the top-right of LEFT area (before right panel)
            void LayoutTopButtons()
            {
                int y = 12;
                int gap = 12;

                // Right edge of left area: total width - right panel width - padding
                int rightEdge = Width - (pnlRight?.Width ?? 0) - 10;
                // Left edge after summary block
                int leftEdge = pnlTopSummary.Right + 10;

                // If window too small, fallback to keep within top panel
                rightEdge = Math.Max(rightEdge, leftEdge + btnStatistics.Width + gap + btnExportRecord.Width);

                btnExportRecord.Location = new Point(Math.Max(leftEdge, rightEdge - btnExportRecord.Width), y);
                btnStatistics.Location = new Point(Math.Max(leftEdge, btnExportRecord.Left - gap - btnStatistics.Width), y);
            }

            // Hook both top resize and parent resize (since right panel width changes with window)
            pnlTop.Resize += (s, e) => LayoutTopButtons();
            this.Resize += (s, e) => LayoutTopButtons();
            LayoutTopButtons();

            Controls.Add(pnlTop);
        }

        private void CreateMain()
        {
            pnlMain = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };

            CreateRight();
            CreateLeft();

            Controls.Add(pnlMain);
        }

        private void CreateLeft()
        {
            pnlLeft = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(5, 0, 5, 5)
            };

            // Bottom detail area (left only)
            pnlLeftDetails = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 260,
                BackColor = Color.White
            };

            lblDetailTitle = new Label
            {
                Text = "详细分数变动时间",
                AutoSize = true,
                Location = new Point(0, 6)
            };
            pnlLeftDetails.Controls.Add(lblDetailTitle);

            dgvDetails = CreateDetailsGrid();
            dgvDetails.Location = new Point(0, 26);
            dgvDetails.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dgvDetails.Size = new Size(pnlLeftDetails.Width, pnlLeftDetails.Height - 26);
            pnlLeftDetails.Controls.Add(dgvDetails);
            pnlLeftDetails.Resize += (s, e) =>
            {
                dgvDetails.Size = new Size(pnlLeftDetails.Width, pnlLeftDetails.Height - 26);
            };

            // Toolbar between grids
            pnlLeftToolbar = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 32,
                BackColor = Color.White
            };

            rbAscending = new RadioButton
            {
                Text = "正序",
                AutoSize = true,
                Location = new Point(5, 8),
                Checked = true
            };
            pnlLeftToolbar.Controls.Add(rbAscending);

            rbDescending = new RadioButton
            {
                Text = "倒序",
                AutoSize = true,
                Location = new Point(65, 8)
            };
            pnlLeftToolbar.Controls.Add(rbDescending);

            txtSearch = new TextBox
            {
                Size = new Size(160, 24)
            };
            pnlLeftToolbar.Controls.Add(txtSearch);

            btnSearch = new Button
            {
                Text = "搜索旺旺号",
                Size = new Size(95, 24),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White
            };
            btnSearch.FlatAppearance.BorderColor = BorderColor;
            pnlLeftToolbar.Controls.Add(btnSearch);
            btnSearch.Click += BtnSearch_Click;
            txtSearch.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    BtnSearch_Click(this, EventArgs.Empty);
                }
            };

            pnlLeftToolbar.Resize += (s, e) =>
            {
                int rightPadding = 5;
                btnSearch.Location = new Point(pnlLeftToolbar.Width - btnSearch.Width - rightPadding, 4);
                txtSearch.Location = new Point(btnSearch.Left - txtSearch.Width - 8, 6);
            };

            // Top players grid
            dgvPlayers = CreatePlayersGrid();
            dgvPlayers.Dock = DockStyle.Fill;

            pnlLeft.Controls.Add(dgvPlayers);
            pnlLeft.Controls.Add(pnlLeftToolbar);
            pnlLeft.Controls.Add(pnlLeftDetails);

            pnlMain.Controls.Add(pnlLeft);
        }

        private void CreateRight()
        {
            pnlRight = new Panel
            {
                Dock = DockStyle.Right,
                Width = 420,
                BackColor = Color.White,
                Padding = new Padding(5, 0, 5, 5)
            };

            // Top bar: 导出流水账 + checkbox (outside border box, like design)
            pnlRightTopBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 30,
                BackColor = Color.White
            };

            btnExportBill = new Button
            {
                Text = "导出流水账",
                Size = new Size(100, 24),
                Location = new Point(0, 3),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White
            };
            btnExportBill.FlatAppearance.BorderColor = BorderColor;
            pnlRightTopBar.Controls.Add(btnExportBill);
            btnExportBill.Click += BtnExportBill_Click;

            chkBillFormat2 = new CheckBox
            {
                Text = "流水账格式2",
                AutoSize = true,
                Location = new Point(btnExportBill.Right + 10, 7)
            };
            pnlRightTopBar.Controls.Add(chkBillFormat2);

            // Two bordered boxes: top stats box + bottom times box (as design)
            pnlRightTimesBox = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 180,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(6)
            };

            pnlRightStatsBox = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(6)
            };

            tlpRightStats = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = false,
                ColumnCount = 4,
                RowCount = 0
            };
            tlpRightStats.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));  // left title
            tlpRightStats.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 105)); // left value
            tlpRightStats.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));  // right title
            tlpRightStats.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 105)); // right value

            // Rows (按截图二字段补齐)
            AddRightRow("名字:", "Name", "", RedColor, "账号:", "Account", "", BlueColor);
            AddRightRow("余额:", "Balance", "0", RedColor, "下分:", "DownScore", "0", RedColor);
            AddRightRow("上分:", "UpScore", "0", RedColor, "活动分:", "ActivityScore", "0", RedColor);
            AddRightRow("分差:", "Diff", "0", RedColor, "下注盈利:", "BetProfit", "0", RedColor);
            AddRightRow("实际盈利:", "ActualProfit", "0", RedColor, "总下注:", "TotalBet", "0", RedColor);
            AddRightRow("次数:", "Times", "0", RedColor, "无视回本次数:", "IgnoreReturn", "0", RedColor);
            AddRightRow("超无视次数:", "SuperIgnore", "0", RedColor, "流水比:", "FlowRatio", "0", RedColor);
            AddRightRow("最大单注:", "MaxBet", "0", RedColor, "大小单双率:", "BigSmallOddEven", "0%", RedColor);
            AddRightRow("一中奖率:", "HitRate", "0%", RedColor, "大单小双率:", "BigSingleSmallDouble", "0%", RedColor);
            AddRightRow("大双小单率:", "BigDoubleSmallSingle", "0%", RedColor, "豹顺对子率:", "PairRate", "0%", RedColor);
            AddRightRow("极数特码率:", "SpecialCode", "0%", RedColor, "约顺对子率:", "AgreePairRate", "0%", RedColor);
            AddRightRow("一龙虎豹率:", "DragonTiger", "0%", RedColor, "尾大小单双率:", "TailBigSmallOddEven", "0%", RedColor);
            AddRightRow("尾组合率:", "TailCombo", "0%", RedColor, "前大小单双率:", "FrontBigSmallOddEven", "0%", RedColor);
            AddRightRow("前组合率:", "FrontCombo", "0%", RedColor, "", "", "", RedColor);

            pnlRightStatsBox.Controls.Add(tlpRightStats);

            // Times title + textbox (separate box)
            lblRightTimesTitle = new Label
            {
                Text = "详细分数变动时间:",
                AutoSize = true,
                Location = new Point(6, 6)
            };
            pnlRightTimesBox.Controls.Add(lblRightTimesTitle);

            tabRightBottom = new TabControl
            {
                Location = new Point(6, 26),
                Size = new Size(pnlRightTimesBox.Width - 12, pnlRightTimesBox.Height - 32),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            tabRightTimes = new TabPage { Text = "详细分数变动时间" };
            tabRightBattle = new TabPage { Text = "统计战况" };

            txtRightTimes = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BorderStyle = BorderStyle.None
            };
            tabRightTimes.Controls.Add(txtRightTimes);

            txtBattleReport = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BorderStyle = BorderStyle.None
            };
            tabRightBattle.Controls.Add(txtBattleReport);

            tabRightBottom.TabPages.Add(tabRightTimes);
            tabRightBottom.TabPages.Add(tabRightBattle);
            pnlRightTimesBox.Controls.Add(tabRightBottom);

            pnlRightTimesBox.Resize += (s, e) =>
            {
                tabRightBottom.Size = new Size(pnlRightTimesBox.Width - 12, pnlRightTimesBox.Height - 32);
            };

            pnlRight.Controls.Add(pnlRightStatsBox);
            pnlRight.Controls.Add(pnlRightTimesBox);
            pnlRight.Controls.Add(pnlRightTopBar);

            pnlMain.Controls.Add(pnlRight);
        }

        private DataGridView CreatePlayersGrid()
        {
            var grid = new DataGridView
            {
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

            grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = HeaderBgColor,
                ForeColor = Color.Black,
                Font = new Font("Microsoft YaHei UI", 9F),
                Alignment = DataGridViewContentAlignment.MiddleCenter,
                SelectionBackColor = HeaderBgColor,
                SelectionForeColor = Color.Black
            };

            grid.DefaultCellStyle = new DataGridViewCellStyle
            {
                Font = new Font("Microsoft YaHei UI", 9F),
                SelectionBackColor = Color.FromArgb(0, 120, 215),
                SelectionForeColor = Color.White
            };

            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "WangwangId", HeaderText = "旺旺号", Width = 90 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Nickname", HeaderText = "昵称", Width = 70 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalUp", HeaderText = "总上分", Width = 80 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalDown", HeaderText = "总下分", Width = 80 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Diff", HeaderText = "分差", Width = 70 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "BetProfit", HeaderText = "下注盈利", Width = 90 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Balance", HeaderText = "余额", Width = 70 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Rounds", HeaderText = "把数", Width = 70 });

            grid.SelectionChanged += DgvPlayers_SelectionChanged;
            grid.CellFormatting += DgvPlayers_CellFormatting;

            return grid;
        }

        private DataGridView CreateDetailsGrid()
        {
            var grid = new DataGridView
            {
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

            grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = HeaderBgColor,
                ForeColor = Color.Black,
                Font = new Font("Microsoft YaHei UI", 9F),
                Alignment = DataGridViewContentAlignment.MiddleCenter,
                SelectionBackColor = HeaderBgColor,
                SelectionForeColor = Color.Black
            };

            grid.DefaultCellStyle = new DataGridViewCellStyle
            {
                Font = new Font("Microsoft YaHei UI", 9F)
            };

            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Time", HeaderText = "", Width = 130 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Score", HeaderText = "分数", Width = 60 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "BetContent", HeaderText = "下注内容", Width = 240 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Profit", HeaderText = "盈亏", Width = 80 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalFlow", HeaderText = "总流水", Width = 90 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Odds", HeaderText = "赔率倍", Width = 90 });

            grid.CellFormatting += DgvDetails_CellFormatting;
            return grid;
        }

        private void AddRightRow(
            string leftTitle, string leftKey, string leftDefault, Color leftValueColor,
            string rightTitle, string rightKey, string rightDefault, Color rightValueColor)
        {
            int row = tlpRightStats.RowCount++;
            tlpRightStats.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var lt = CreateRightTitleLabel(leftTitle);
            var lv = CreateRightValueLabel(leftKey, leftDefault, leftValueColor);
            var rt = CreateRightTitleLabel(rightTitle);
            var rv = CreateRightValueLabel(rightKey, rightDefault, rightValueColor);

            tlpRightStats.Controls.Add(lt, 0, row);
            tlpRightStats.Controls.Add(lv, 1, row);
            tlpRightStats.Controls.Add(rt, 2, row);
            tlpRightStats.Controls.Add(rv, 3, row);
        }

        private Label CreateRightTitleLabel(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                ForeColor = RedColor,
                Margin = new Padding(0, 2, 0, 2)
            };
        }

        private Label CreateRightValueLabel(string key, string text, Color color)
        {
            var lbl = new Label
            {
                Text = text,
                AutoSize = true,
                ForeColor = color,
                Margin = new Padding(0, 2, 0, 2)
            };

            if (!string.IsNullOrWhiteSpace(key)) _rightValueLabels[key] = lbl;
            return lbl;
        }

        private void UpdateTopSummaryFromData(List<PlayerStatItem> items)
        {
            if (items == null) items = new List<PlayerStatItem>();

            long totalDiff = 0;
            long totalBetProfit = 0;
            int winCount = 0;
            int loseCount = 0;
            long winAmount = 0;
            long loseAmount = 0;

            foreach (var it in items)
            {
                totalDiff += it.Diff;
                totalBetProfit += it.BetProfit;

                if (it.BetProfit >= 0)
                {
                    winCount++;
                    winAmount += it.BetProfit;
                }
                else
                {
                    loseCount++;
                    loseAmount += Math.Abs(it.BetProfit);
                }
            }

            lblUpDownTotal.Text = $"上下分总统计: {(totalDiff >= 0 ? "+" : "")}{totalDiff}";
            lblBetProfitTotal.Text = $"下注盈利统计: {(totalBetProfit >= 0 ? "+" : "")}{totalBetProfit}";
            lblPlayerCount.Text = $"统计人数: {items.Count}";
            lblWinLoseStats.Text = $"赢: {winCount}人+{winAmount}    输: {loseCount}人-{loseAmount}";
        }

        private void UpdatePlayerDetails(PlayerStatItem player)
        {
            if (player == null) return;

            // Right stats values (颜色按设计：红字为主，账号蓝字)
            SetRightValue("Name", player.Nickname, RedColor);
            SetRightValue("Account", player.WangwangId, BlueColor);
            SetRightValue("Balance", player.Balance.ToString(), RedColor);
            SetRightValue("UpScore", player.TotalUp.ToString(), RedColor);
            SetRightValue("DownScore", player.TotalDown.ToString(), RedColor);
            SetRightValue("ActivityScore", "0", RedColor);
            SetRightValue("Diff", player.Diff.ToString(), RedColor);
            SetRightValue("BetProfit", player.BetProfit.ToString(), RedColor);
            SetRightValue("ActualProfit", player.TotalUp.ToString(), RedColor);
            SetRightValue("TotalBet", "202", RedColor);
            SetRightValue("Times", player.Rounds.ToString(), RedColor);
            SetRightValue("SuperIgnore", "6", RedColor);
            SetRightValue("IgnoreReturn", "2", RedColor);
            SetRightValue("MaxBet", "50", RedColor);
            SetRightValue("FlowRatio", "4", RedColor);

            // Rates (sample values from design screenshot)
            SetRightValue("HitRate", "0%", RedColor);
            SetRightValue("BigSmallOddEven", "52.48%", RedColor);
            SetRightValue("BigDoubleSmallSingle", "32.67%", RedColor);
            SetRightValue("BigSingleSmallDouble", "14.85%", RedColor);
            SetRightValue("SpecialCode", "0%", RedColor);
            SetRightValue("PairRate", "0%", RedColor);
            SetRightValue("AgreePairRate", "0%", RedColor);
            SetRightValue("DragonTiger", "0%", RedColor);
            SetRightValue("TailBigSmallOddEven", "0%", RedColor);
            SetRightValue("TailCombo", "0%", RedColor);
            SetRightValue("FrontBigSmallOddEven", "0%", RedColor);
            SetRightValue("FrontCombo", "0%", RedColor);
        }

        private void SetRightValue(string key, string value, Color color)
        {
            if (!_rightValueLabels.TryGetValue(key, out var lbl)) return;
            lbl.Text = value;
            lbl.ForeColor = color;
        }

        private void DgvPlayers_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvPlayers.CurrentRow != null && dgvPlayers.CurrentRow.Index >= 0 &&
                dgvPlayers.CurrentRow.Index < _playerStats.Count)
            {
                UpdatePlayerDetails(_playerStats[dgvPlayers.CurrentRow.Index]);
            }
        }

        private void DgvPlayers_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _playerStats.Count) return;

            var player = _playerStats[e.RowIndex];
            var colName = dgvPlayers.Columns[e.ColumnIndex].Name;

            if (colName == "Diff")
                e.CellStyle.ForeColor = player.Diff >= 0 ? RedColor : GreenColor;
            else if (colName == "BetProfit")
                e.CellStyle.ForeColor = player.BetProfit >= 0 ? RedColor : GreenColor;
        }

        private void DgvDetails_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;

            var colName = dgvDetails.Columns[e.ColumnIndex].Name;
            if ((colName == "Profit" || colName == "TotalFlow") && e.Value != null)
            {
                string val = e.Value.ToString();
                if (val.StartsWith("-"))
                    e.CellStyle.ForeColor = GreenColor;
                else if (val.StartsWith("+"))
                    e.CellStyle.ForeColor = RedColor;
            }
        }

        public void RefreshData(DateTime startTime, DateTime endTime)
        {
            try
            {
                if (endTime < startTime)
                {
                    var t = startTime;
                    startTime = endTime;
                    endTime = t;
                }

                var items = LoadStatistics(startTime, endTime);
                RenderPlayers(items);

                UpdateTopSummaryFromData(items);

                // Keep selection
                if (dgvPlayers.Rows.Count > 0)
                {
                    dgvPlayers.ClearSelection();
                    dgvPlayers.Rows[0].Selected = true;
                    dgvPlayers.CurrentCell = dgvPlayers.Rows[0].Cells[0];
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"统计加载失败: {ex.Message}", "回水工具", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        public void ClearData()
        {
            _playerStats.Clear();
            dgvPlayers.Rows.Clear();
            dgvDetails.Rows.Clear();
            txtRightTimes.Text = "";
            UpdateTopSummaryFromData(new List<PlayerStatItem>());
        }

        // =====================================================
        // Button handlers
        // =====================================================

        private void BtnSearch_Click(object sender, EventArgs e)
        {
            var key = (txtSearch.Text ?? "").Trim();
            if (string.IsNullOrEmpty(key)) return;

            for (int i = 0; i < dgvPlayers.Rows.Count; i++)
            {
                var cell = dgvPlayers.Rows[i].Cells[0].Value?.ToString() ?? "";
                if (cell.Contains(key))
                {
                    dgvPlayers.ClearSelection();
                    dgvPlayers.Rows[i].Selected = true;
                    dgvPlayers.CurrentCell = dgvPlayers.Rows[i].Cells[0];
                    dgvPlayers.FirstDisplayedScrollingRowIndex = Math.Max(0, i - 3);
                    return;
                }
            }

            MessageBox.Show("未找到该旺旺号", "搜索", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnStatistics_Click(object sender, EventArgs e)
        {
            // 简单统计战况：按下注盈利排序输出Top
            if (_playerStats.Count == 0)
            {
                if (txtBattleReport != null) txtBattleReport.Text = "暂无统计数据";
                if (tabRightBottom != null) tabRightBottom.SelectedTab = tabRightBattle;
                return;
            }

            var topWin = _playerStats.OrderByDescending(x => x.BetProfit).Take(10).ToList();
            var topLose = _playerStats.OrderBy(x => x.BetProfit).Take(10).ToList();

            var sb = new StringBuilder();
            sb.AppendLine(lblUpDownTotal.Text);
            sb.AppendLine(lblBetProfitTotal.Text);
            sb.AppendLine(lblPlayerCount.Text);
            sb.AppendLine(lblWinLoseStats.Text);
            sb.AppendLine();
            sb.AppendLine("盈利TOP10:");
            foreach (var x in topWin) sb.AppendLine($"{x.Nickname}({x.WangwangId}) 盈利:{x.BetProfit}");
            sb.AppendLine();
            sb.AppendLine("亏损TOP10:");
            foreach (var x in topLose) sb.AppendLine($"{x.Nickname}({x.WangwangId}) 盈利:{x.BetProfit}");

            if (txtBattleReport != null) txtBattleReport.Text = sb.ToString();
            if (tabRightBottom != null) tabRightBottom.SelectedTab = tabRightBattle;
        }

        private void BtnExportRecord_Click(object sender, EventArgs e)
        {
            if (dgvPlayers.Rows.Count == 0)
            {
                MessageBox.Show("没有可导出的记录", "导出记录", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var sfd = new SaveFileDialog())
            {
                sfd.Filter = "CSV文件|*.csv|文本文件|*.txt";
                sfd.FileName = $"统计所有-{DateTime.Now:yyyyMMdd-HHmmss}.csv";
                if (sfd.ShowDialog() != DialogResult.OK) return;

                var sb = new StringBuilder();
                // header
                var headers = dgvPlayers.Columns.Cast<DataGridViewColumn>().Select(c => Csv(c.HeaderText)).ToArray();
                sb.AppendLine(string.Join(",", headers));
                // rows
                foreach (DataGridViewRow r in dgvPlayers.Rows)
                {
                    if (r.IsNewRow) continue;
                    var cells = r.Cells.Cast<DataGridViewCell>().Select(c => Csv(c.Value?.ToString() ?? "")).ToArray();
                    sb.AppendLine(string.Join(",", cells));
                }

                File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
                MessageBox.Show("导出成功", "导出记录", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BtnExportBill_Click(object sender, EventArgs e)
        {
            var player = GetSelectedPlayer();
            if (player == null)
            {
                MessageBox.Show("请先选择一个玩家", "导出流水账", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var sfd = new SaveFileDialog())
            {
                sfd.Filter = "文本文件|*.txt";
                sfd.FileName = $"流水账-{player.Nickname}-{player.WangwangId}-{DateTime.Now:yyyyMMdd-HHmmss}.txt";
                if (sfd.ShowDialog() != DialogResult.OK) return;

                var sb = new StringBuilder();
                if (!chkBillFormat2.Checked)
                {
                    sb.AppendLine("【导出流水账】");
                    sb.AppendLine($"名字:{player.Nickname}");
                    sb.AppendLine($"账号:{player.WangwangId}");
                    sb.AppendLine($"总上分:{player.TotalUp}");
                    sb.AppendLine($"总下分:{player.TotalDown}");
                    sb.AppendLine($"分差:{player.Diff}");
                    sb.AppendLine($"下注盈利:{player.BetProfit}");
                    sb.AppendLine($"余额:{player.Balance}");
                    sb.AppendLine($"把数:{player.Rounds}");
                }
                else
                {
                    sb.AppendLine("【导出流水账 格式2】");
                    sb.AppendLine($"{player.Nickname}|{player.WangwangId}|上分:{player.TotalUp}|下分:{player.TotalDown}|分差:{player.Diff}|下注盈亏:{player.BetProfit}|余额:{player.Balance}|把数:{player.Rounds}");
                }

                sb.AppendLine();
                sb.AppendLine("【明细】");
                // header
                var headers = dgvDetails.Columns.Cast<DataGridViewColumn>().Select(c => c.HeaderText).ToArray();
                sb.AppendLine(string.Join("\t", headers));
                foreach (DataGridViewRow r in dgvDetails.Rows)
                {
                    if (r.IsNewRow) continue;
                    var cells = r.Cells.Cast<DataGridViewCell>().Select(c => (c.Value?.ToString() ?? "").Replace("\t", " ")).ToArray();
                    sb.AppendLine(string.Join("\t", cells));
                }

                sb.AppendLine();
                sb.AppendLine("【详细分数变动时间】");
                sb.AppendLine(txtRightTimes.Text ?? "");

                File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
                MessageBox.Show("导出成功", "导出流水账", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private static string Csv(string s)
        {
            if (s == null) return "\"\"";
            s = s.Replace("\"", "\"\"");
            return $"\"{s}\"";
        }

        // =====================================================
        // Data loading (best-effort based on current codebase data files)
        // =====================================================

        private List<PlayerStatItem> LoadStatistics(DateTime startTime, DateTime endTime)
        {
            var ds = DataService.Instance;

            // 1) Load up/down records from 上下分.db
            var scoreAggByDay = ReadUpDownAggByDay(startTime, endTime);
            var scoreAggTotal = new Dictionary<string, UpDownAgg>();
            foreach (var dayKv in scoreAggByDay)
            {
                foreach (var p in dayKv.Value)
                {
                    if (!scoreAggTotal.TryGetValue(p.Key, out var t)) t = new UpDownAgg();
                    t.Up += p.Value.Up;
                    t.Down += p.Value.Down;
                    scoreAggTotal[p.Key] = t;
                }
            }

            // 2) Compute rounds by reading bet files (Bets folder)
            var rounds = ComputeRounds(startTime, endTime);

            // 3) Build stats per player from player list + daily stats
            var players = ds.GetAllPlayers();
            var list = new List<PlayerStatItem>(players.Count);

            foreach (var p in players)
            {
                var id = p.WangWangId;
                if (string.IsNullOrWhiteSpace(id)) continue;

                scoreAggTotal.TryGetValue(id, out var udTotal);

                decimal stakeTotal = 0m;
                decimal betProfit = 0m;

                // per-day betProfit approximation = (LastScore-StartScore) - (Up-Down) for that day
                for (var day = startTime.Date; day <= endTime.Date; day = day.AddDays(1))
                {
                    var dateKey = day.ToString("yyyy-MM-dd");
                    var startKey = $"Daily:{dateKey}:{id}:StartScore";
                    var lastKey = $"Daily:{dateKey}:{id}:LastScore";
                    var stakeKey = $"Daily:{dateKey}:{id}:Stake";

                    var startStr = ds.GetSetting(startKey, "");
                    var lastStr = ds.GetSetting(lastKey, "");
                    var stakeStr = ds.GetSetting(stakeKey, "");

                    // only include day if any key exists
                    if (string.IsNullOrEmpty(startStr) && string.IsNullOrEmpty(lastStr) && string.IsNullOrEmpty(stakeStr))
                        continue;

                    decimal startScore = ParseDec(startStr, 0m);
                    decimal lastScore = ParseDec(lastStr, startScore);
                    decimal dayStake = ParseDec(stakeStr, 0m);
                    stakeTotal += dayStake;

                    UpDownAgg dayAgg = default;
                    if (scoreAggByDay.TryGetValue(dateKey, out var perDay) && perDay.TryGetValue(id, out var ud))
                        dayAgg = ud;

                    betProfit += (lastScore - startScore) - (dayAgg.Up - dayAgg.Down);
                }

                // filter: only show players with activity
                bool hasActivity = udTotal.Up != 0m || udTotal.Down != 0m || stakeTotal != 0m || betProfit != 0m;
                if (!hasActivity) continue;

                int upInt = (int)Math.Round(udTotal.Up, 0);
                int downInt = (int)Math.Round(udTotal.Down, 0);
                int diffInt = upInt - downInt;
                int betProfitInt = (int)Math.Round(betProfit, 0);
                int balanceInt = (int)Math.Round(p.Score, 0);
                int roundsInt = rounds.TryGetValue(id, out var r) ? r : 0;

                list.Add(new PlayerStatItem
                {
                    WangwangId = id,
                    Nickname = p.Nickname ?? "",
                    TotalUp = upInt,
                    TotalDown = downInt,
                    Diff = diffInt,
                    BetProfit = betProfitInt,
                    Balance = balanceInt,
                    Rounds = roundsInt
                });
            }

            // Default sort: by wangwangId (stable), can be extended with radio buttons later
            list = list.OrderByDescending(x => x.BetProfit).ToList();
            return list;
        }

        private void RenderPlayers(List<PlayerStatItem> items)
        {
            _playerStats.Clear();
            dgvPlayers.Rows.Clear();

            foreach (var it in items)
            {
                _playerStats.Add(it);
                dgvPlayers.Rows.Add(it.WangwangId, it.Nickname, it.TotalUp, it.TotalDown, it.Diff, it.BetProfit, it.Balance, it.Rounds);
            }
        }

        private PlayerStatItem GetSelectedPlayer()
        {
            if (dgvPlayers.CurrentRow == null) return null;
            int idx = dgvPlayers.CurrentRow.Index;
            if (idx < 0 || idx >= _playerStats.Count) return null;
            return _playerStats[idx];
        }

        private static decimal ParseDec(string s, decimal defaultValue)
        {
            if (string.IsNullOrWhiteSpace(s)) return defaultValue;
            if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v)) return v;
            if (decimal.TryParse(s, out v)) return v;
            return defaultValue;
        }

        // Read up/down records grouped by dayKey -> playerId -> (up, down)
        private Dictionary<string, Dictionary<string, UpDownAgg>> ReadUpDownAggByDay(DateTime startTime, DateTime endTime)
        {
            var result = new Dictionary<string, Dictionary<string, UpDownAgg>>();
            try
            {
                var scoreDb = Path.Combine(DataService.Instance.GroupMemberCacheDir, "..", "上下分.db");
                scoreDb = Path.GetFullPath(scoreDb);
                if (!File.Exists(scoreDb)) return result;

                var lines = File.ReadAllLines(scoreDb, Encoding.UTF8);
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var parts = line.Split('|');
                    if (parts.Length < 6) continue;

                    if (!DateTime.TryParse(parts[0], out var time)) continue;
                    if (time < startTime || time > endTime) continue;

                    var id = parts[1]?.Trim() ?? "";
                    if (string.IsNullOrEmpty(id)) continue;

                    var type = parts[3] ?? "";
                    decimal.TryParse(parts[4], NumberStyles.Any, CultureInfo.InvariantCulture, out var amt);
                    if (amt == 0m) decimal.TryParse(parts[4], out amt);
                    amt = Math.Abs(amt);

                    var dayKey = time.ToString("yyyy-MM-dd");
                    if (!result.TryGetValue(dayKey, out var dict))
                    {
                        dict = new Dictionary<string, UpDownAgg>();
                        result[dayKey] = dict;
                    }

                    if (!dict.TryGetValue(id, out var cur)) cur = new UpDownAgg();

                    if (type.Contains("上")) cur.Up += amt;
                    else if (type.Contains("下")) cur.Down += amt;
                    dict[id] = cur;
                }
            }
            catch { }

            return result;
        }

        // Compute rounds by scanning bet files in DatabaseDir\\Bets for the time window (best-effort)
        private Dictionary<string, int> ComputeRounds(DateTime startTime, DateTime endTime)
        {
            var dict = new Dictionary<string, HashSet<string>>();
            try
            {
                var baseDir = Path.Combine(DataService.Instance.DatabaseDir, "Bets");
                if (!Directory.Exists(baseDir)) return new Dictionary<string, int>();

                for (var day = startTime.Date; day <= endTime.Date; day = day.AddDays(1))
                {
                    var dayDir = Path.Combine(baseDir, day.ToString("yyyy-MM-dd"));
                    if (!Directory.Exists(dayDir)) continue;

                    foreach (var teamDir in Directory.GetDirectories(dayDir))
                    {
                        foreach (var betFile in Directory.GetFiles(teamDir, "bets-*.txt"))
                        {
                            var period = Path.GetFileNameWithoutExtension(betFile).Substring("bets-".Length);
                            var keyPeriod = $"{day:yyyyMMdd}:{Path.GetFileName(teamDir)}:{period}";

                            var lines = File.ReadAllLines(betFile, Encoding.UTF8);
                            foreach (var l in lines)
                            {
                                if (string.IsNullOrWhiteSpace(l)) continue;
                                var parts = l.Split('\t');
                                if (parts.Length < 9) continue;
                                var playerId = parts[3];
                                if (string.IsNullOrWhiteSpace(playerId)) continue;

                                if (!dict.TryGetValue(playerId, out var set))
                                {
                                    set = new HashSet<string>();
                                    dict[playerId] = set;
                                }
                                set.Add(keyPeriod);
                            }
                        }
                    }
                }
            }
            catch { }

            return dict.ToDictionary(kv => kv.Key, kv => kv.Value.Count);
        }
    }

    public class PlayerStatItem
    {
        public string WangwangId { get; set; }
        public string Nickname { get; set; }
        public int TotalUp { get; set; }
        public int TotalDown { get; set; }
        public int Diff { get; set; }
        public int BetProfit { get; set; }
        public int Balance { get; set; }
        public int Rounds { get; set; }
    }

    public class TransactionDetail
    {
        public string Time { get; set; }
        public int Score { get; set; }
        public string BetContent { get; set; }
        public int Profit { get; set; }
        public int TotalFlow { get; set; }
        public string Odds { get; set; }
    }
}
