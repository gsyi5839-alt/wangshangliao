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
    /// 回水计算面板（按设计图实现：左侧表格 + 右侧按钮/结果框）
    /// </summary>
    public sealed class RebateCalcPanel : UserControl
    {
        private readonly Color BorderColor = Color.FromArgb(180, 180, 180);
        private readonly Color HeaderBgColor = Color.FromArgb(240, 240, 240);

        private DataGridView dgv;
        private Panel pnlRight;
        private Button btnCalc;
        private Button btnAddBill;
        private BorderPanel pnlResult;
        private Label lblResultTitle;
        private TextBox txtResult;

        private readonly BindingList<RebateCalcItem> _items = new BindingList<RebateCalcItem>();

        public RebateCalcPanel()
        {
            SuspendLayout();
            BackColor = Color.White;
            Dock = DockStyle.Fill;
            Font = new Font("Microsoft YaHei UI", 9F);

            CreateRight();
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

            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "WangwangId", HeaderText = "旺旺号", Width = 90 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Nickname", HeaderText = "昵称", Width = 70 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Profit", HeaderText = "盈亏", Width = 70 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalFlow", HeaderText = "总下注流水", Width = 110 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Ratio", HeaderText = "组合占比", Width = 90 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "BetCount", HeaderText = "下注次数", Width = 90 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Rebate", HeaderText = "回水", Width = 70 });

            dgv.CellFormatting += Dgv_CellFormatting;

            Controls.Add(dgv);
            dgv.BringToFront();
        }

        private void CreateRight()
        {
            pnlRight = new Panel
            {
                Dock = DockStyle.Right,
                Width = 260,
                BackColor = Color.White,
                Padding = new Padding(10, 10, 10, 10)
            };

            btnCalc = new Button
            {
                Text = "计算统计",
                Location = new Point(10, 10),
                Size = new Size(110, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White
            };
            btnCalc.FlatAppearance.BorderColor = BorderColor;
            btnCalc.Click += BtnCalc_Click;
            pnlRight.Controls.Add(btnCalc);

            btnAddBill = new Button
            {
                Text = "加入账单",
                Location = new Point(10, 55),
                Size = new Size(110, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White
            };
            btnAddBill.FlatAppearance.BorderColor = BorderColor;
            btnAddBill.Click += BtnAddBill_Click;
            pnlRight.Controls.Add(btnAddBill);

            // Result box - use custom border to keep line color consistent with the whole UI
            // Title is OUTSIDE the border box (as design)
            lblResultTitle = new Label
            {
                Text = "计算结果",
                AutoSize = true,
                Location = new Point(10, 105),
                BackColor = Color.White
            };
            pnlRight.Controls.Add(lblResultTitle);

            pnlResult = new BorderPanel
            {
                BorderColor = BorderColor,
                Location = new Point(10, 125),
                Size = new Size(240, 500),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.White,
                Padding = new Padding(8, 22, 8, 8)
            };

            txtResult = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BorderStyle = BorderStyle.None
            };
            pnlResult.Controls.Add(txtResult);

            pnlRight.Controls.Add(pnlResult);

            // Keep border box below title on resize (no overlap)
            pnlRight.Resize += (s, e) =>
            {
                // Maintain fixed gap between title and box
                pnlResult.Location = new Point(10, lblResultTitle.Bottom + 4);
                pnlResult.Size = new Size(pnlRight.ClientSize.Width - 20, pnlRight.ClientSize.Height - pnlResult.Top - 10);
            };

            Controls.Add(pnlRight);
        }

        private void Dgv_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var col = dgv.Columns[e.ColumnIndex].Name;
            if (col == "Profit" || col == "Rebate")
            {
                var s = e.Value?.ToString() ?? "";
                if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ||
                    decimal.TryParse(s, out v))
                {
                    e.CellStyle.ForeColor = v >= 0 ? Color.FromArgb(200, 0, 0) : Color.FromArgb(0, 128, 0);
                }
            }
        }

        // =====================================================
        // Public API
        // =====================================================

        public void RefreshData(DateTime startTime, DateTime endTime)
        {
            // no auto-loading here to avoid surprising user; keep current selection
            // Users can click “加入账单” to load
        }

        // =====================================================
        // Button logic (打通基础功能)
        // =====================================================

        private void BtnAddBill_Click(object sender, EventArgs e)
        {
            // 从现有数据源汇总：上下分.db + 设置.ini daily stake + Bets 把数
            var host = FindForm();
            DateTime start = DateTime.Today;
            DateTime end = DateTime.Now;

            // best-effort: if parent is RebateToolControl, it exposes QueryStartTime/QueryEndTime
            var parent = Parent;
            while (parent != null && !(parent is RebateToolControl)) parent = parent.Parent;
            var rtc = parent as RebateToolControl;
            if (rtc != null)
            {
                start = rtc.QueryStartTime;
                end = rtc.QueryEndTime;
            }

            var list = LoadItems(start, end);
            Render(list);

            txtResult.Text = $"已加入账单：{list.Count}条\r\n时间范围：{start:yyyy-MM-dd HH:mm:ss} → {end:yyyy-MM-dd HH:mm:ss}\r\n";
        }

        private void BtnCalc_Click(object sender, EventArgs e)
        {
            if (_items.Count == 0)
            {
                txtResult.Text = "暂无数据，请先点击【加入账单】。\r\n";
                return;
            }

            // 回水比例：从设置读取（可在后续回水设置里配置）
            // 支持两种写法：0.01 表示1%；1 表示1%
            var rateStr = DataService.Instance.GetSetting("RebateTool:RebateRate", "0");
            var rate = ParseDec(rateStr, 0m);
            if (rate > 1m) rate = rate / 100m;

            decimal totalFlow = 0m;
            decimal totalRebate = 0m;

            foreach (var it in _items)
            {
                it.Rebate = Math.Round(it.TotalFlow * rate, 2);
                totalFlow += it.TotalFlow;
                totalRebate += it.Rebate;
            }

            // Re-render with rebate column updated
            Render(_items.ToList());

            var sb = new StringBuilder();
            sb.AppendLine($"回水比例: {rate:P2}");
            sb.AppendLine($"合计总下注流水: {totalFlow}");
            sb.AppendLine($"合计回水: {totalRebate}");
            sb.AppendLine();
            sb.AppendLine("明细（前20）:");
            foreach (var it in _items.OrderByDescending(x => x.Rebate).Take(20))
            {
                sb.AppendLine($"{it.Nickname}({it.WangwangId}) 流水:{it.TotalFlow} 回水:{it.Rebate} 占比:{it.Ratio:P2} 次数:{it.BetCount}");
            }

            if (rate == 0m)
                sb.AppendLine("\r\n提示：未配置回水比例（设置键 RebateTool:RebateRate），当前回水均为0。");

            txtResult.Text = sb.ToString();
        }

        private void Render(List<RebateCalcItem> list)
        {
            _items.Clear();
            dgv.Rows.Clear();

            foreach (var it in list)
            {
                _items.Add(it);
                dgv.Rows.Add(
                    it.WangwangId,
                    it.Nickname,
                    it.Profit.ToString(CultureInfo.InvariantCulture),
                    it.TotalFlow.ToString(CultureInfo.InvariantCulture),
                    $"{it.Ratio:P2}",
                    it.BetCount,
                    it.Rebate.ToString(CultureInfo.InvariantCulture));
            }
        }

        private List<RebateCalcItem> LoadItems(DateTime startTime, DateTime endTime)
        {
            if (endTime < startTime)
            {
                var t = startTime; startTime = endTime; endTime = t;
            }

            // stake by day from settings.ini (Daily:*:Stake)
            var stakeByPlayer = new Dictionary<string, decimal>();
            for (var day = startTime.Date; day <= endTime.Date; day = day.AddDays(1))
            {
                var dateKey = day.ToString("yyyy-MM-dd");
                foreach (var p in DataService.Instance.GetAllPlayers())
                {
                    var id = p.WangWangId;
                    if (string.IsNullOrWhiteSpace(id)) continue;
                    var stakeKey = $"Daily:{dateKey}:{id}:Stake";
                    var stakeStr = DataService.Instance.GetSetting(stakeKey, "");
                    if (string.IsNullOrEmpty(stakeStr)) continue;

                    var stake = ParseDec(stakeStr, 0m);
                    if (!stakeByPlayer.TryGetValue(id, out var cur)) cur = 0m;
                    stakeByPlayer[id] = cur + stake;
                }
            }

            // rounds count (period count) from Bets folder
            var rounds = ComputeRounds(startTime, endTime);

            // profit approx: use StatisticsAllPanel style daily profit approximation (best effort)
            var profitByPlayer = ComputeBetProfitApprox(startTime, endTime);

            var totalFlowAll = stakeByPlayer.Values.Sum();
            if (totalFlowAll <= 0m) totalFlowAll = 1m;

            var list = new List<RebateCalcItem>();
            foreach (var kv in stakeByPlayer.OrderByDescending(x => x.Value))
            {
                var id = kv.Key;
                var flow = kv.Value;
                if (flow <= 0m) continue;
                var player = DataService.Instance.GetPlayer(id);
                var nick = player?.Nickname ?? "";
                rounds.TryGetValue(id, out var cnt);
                profitByPlayer.TryGetValue(id, out var profit);

                list.Add(new RebateCalcItem
                {
                    WangwangId = id,
                    Nickname = nick,
                    Profit = profit,
                    TotalFlow = flow,
                    Ratio = flow / totalFlowAll,
                    BetCount = cnt,
                    Rebate = 0m
                });
            }

            return list;
        }

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

        private Dictionary<string, decimal> ComputeBetProfitApprox(DateTime startTime, DateTime endTime)
        {
            var ds = DataService.Instance;
            var result = new Dictionary<string, decimal>();
            var players = ds.GetAllPlayers();

            // up/down by day from 上下分.db (to subtract from score delta)
            var upDownByDay = ReadUpDownAggByDay(startTime, endTime);

            foreach (var p in players)
            {
                var id = p.WangWangId;
                if (string.IsNullOrWhiteSpace(id)) continue;

                decimal betProfit = 0m;
                for (var day = startTime.Date; day <= endTime.Date; day = day.AddDays(1))
                {
                    var dateKey = day.ToString("yyyy-MM-dd");
                    var startStr = ds.GetSetting($"Daily:{dateKey}:{id}:StartScore", "");
                    var lastStr = ds.GetSetting($"Daily:{dateKey}:{id}:LastScore", "");
                    if (string.IsNullOrEmpty(startStr) && string.IsNullOrEmpty(lastStr)) continue;

                    var startScore = ParseDec(startStr, 0m);
                    var lastScore = ParseDec(lastStr, startScore);

                    var ud = upDownByDay.TryGetValue(dateKey, out var dict) && dict.TryGetValue(id, out var agg)
                        ? agg
                        : default(UpDownAgg);

                    betProfit += (lastScore - startScore) - (ud.Up - ud.Down);
                }

                if (betProfit != 0m) result[id] = betProfit;
            }

            return result;
        }

        private struct UpDownAgg
        {
            public decimal Up;
            public decimal Down;
        }

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

        private static decimal ParseDec(string s, decimal defaultValue)
        {
            if (string.IsNullOrWhiteSpace(s)) return defaultValue;
            if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v)) return v;
            if (decimal.TryParse(s, out v)) return v;
            return defaultValue;
        }
    }

    public sealed class RebateCalcItem
    {
        public string WangwangId { get; set; }
        public string Nickname { get; set; }
        public decimal Profit { get; set; }
        public decimal TotalFlow { get; set; }
        public decimal Ratio { get; set; }
        public int BetCount { get; set; }
        public decimal Rebate { get; set; }
    }

    internal sealed class BorderPanel : Panel
    {
        public Color BorderColor { get; set; } = Color.FromArgb(180, 180, 180);

        public BorderPanel()
        {
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer, true);
            UpdateStyles();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var g = e.Graphics;
            var r = ClientRectangle;
            r.Width -= 1;
            r.Height -= 1;

            using (var pen = new Pen(BorderColor))
            {
                g.DrawRectangle(pen, r);
            }
        }
    }
}


