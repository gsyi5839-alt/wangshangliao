using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using WangShangLiaoBot.Models;
using WangShangLiaoBot.Services;

namespace WangShangLiaoBot.Controls
{
    /// <summary>
    /// 运行日志 - 带网格线的表格视图 (DataGrid with Grid Lines)
    /// </summary>
    public sealed class RunLogControl : UserControl
    {
        private DataGridView dgvLog;
        private Button btnStartGame;
        private BindingList<LogDisplayItem> _logItems;
        private readonly object _updateLock = new object();

        public RunLogControl()
        {
            BackColor = SystemColors.Control;
            Font = new Font("Microsoft YaHei UI", 9F);
            Dock = DockStyle.Fill;

            _logItems = new BindingList<LogDisplayItem>();
            InitializeUI();
            LoadExistingLogs();
            SubscribeToLogService();
        }

        private void InitializeUI()
        {
            SuspendLayout();

            // Top toolbar panel
            var panelToolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 32,
                BackColor = SystemColors.Control,
                Padding = new Padding(5, 3, 5, 3)
            };

            // Start game button (green) - 放在右侧
            btnStartGame = new Button
            {
                Text = "开始游戏",
                Size = new Size(75, 24),
                BackColor = Color.FromArgb(144, 238, 144),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Microsoft YaHei UI", 9F),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnStartGame.FlatAppearance.BorderColor = Color.FromArgb(100, 180, 100);
            btnStartGame.FlatAppearance.BorderSize = 1;
            btnStartGame.Click += BtnStartGame_Click;
            
            // Set initial position
            btnStartGame.Location = new Point(panelToolbar.Width - btnStartGame.Width - 8, 4);
            panelToolbar.Controls.Add(btnStartGame);

            // DataGridView - 表格视图带网格线和滚动条
            dgvLog = new DataGridView
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
                // Grid Lines - 网格线设置
                CellBorderStyle = DataGridViewCellBorderStyle.Single,
                GridColor = Color.FromArgb(180, 180, 180),
                // Header settings
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersHeight = 22,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single,
                EnableHeadersVisualStyles = false,
                // Row settings
                RowTemplate = { Height = 20 },
                // 滚动条设置 - 水平和垂直
                ScrollBars = ScrollBars.Both
            };

            // 强制启用滚动条 - 通过反射设置
            typeof(DataGridView).InvokeMember("DoubleBuffered",
                System.Reflection.BindingFlags.SetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                null, dgvLog, new object[] { true });

            // Header style - 表头样式
            dgvLog.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(235, 235, 235),
                ForeColor = Color.Black,
                Font = new Font("Microsoft YaHei UI", 9F),
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                SelectionBackColor = Color.FromArgb(235, 235, 235),
                SelectionForeColor = Color.Black,
                Padding = new Padding(2, 0, 0, 0)
            };

            // Default cell style
            dgvLog.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.White,
                ForeColor = Color.Black,
                Font = new Font("Microsoft YaHei UI", 9F),
                SelectionBackColor = Color.FromArgb(200, 220, 255),
                SelectionForeColor = Color.Black,
                Padding = new Padding(2, 0, 0, 0)
            };

            // Add columns - 固定列：ID, 时间, 响应, 类型, 消息
            dgvLog.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Id",
                HeaderText = "ID",
                DataPropertyName = "Id",
                Width = 35,
                SortMode = DataGridViewColumnSortMode.NotSortable
            });
            dgvLog.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Time",
                HeaderText = "时间",
                DataPropertyName = "TimeDisplay",
                Width = 95,
                SortMode = DataGridViewColumnSortMode.NotSortable
            });
            dgvLog.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Period",
                HeaderText = "响应",
                DataPropertyName = "Period",
                Width = 100,
                SortMode = DataGridViewColumnSortMode.NotSortable
            });
            dgvLog.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Type",
                HeaderText = "类型",
                DataPropertyName = "TypeDisplay",
                Width = 65,
                SortMode = DataGridViewColumnSortMode.NotSortable
            });
            dgvLog.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Message",
                HeaderText = "消息",
                DataPropertyName = "Message",
                Width = 500, // 加宽确保水平滚动条出现
                MinimumWidth = 200,
                SortMode = DataGridViewColumnSortMode.NotSortable
            });

            // Cell formatting - 单元格颜色
            dgvLog.CellFormatting += DgvLog_CellFormatting;

            // Bind data source
            dgvLog.DataSource = _logItems;

            // Add controls - order matters!
            Controls.Add(dgvLog);
            Controls.Add(panelToolbar);

            ResumeLayout(false);

            // 加载后确保滚动条正常
            this.Load += (s, e) => {
                // 添加测试数据以显示滚动条效果
                if (_logItems.Count == 0)
                {
                    AddTestData();
                }
            };
        }

        /// <summary>
        /// 添加测试数据 - 足够多以显示滚动条
        /// </summary>
        private void AddTestData()
        {
            for (int i = 1; i <= 50; i++)
            {
                var entry = new RunLogEntry();
                entry.Id = i;
                entry.Time = DateTime.Now.AddSeconds(-60 + i);

                if (i % 3 == 0)
                {
                    entry.LogType = RunLogType.Plugin;
                    entry.Period = "";
                    entry.Message = "日志 发来消息: 发送群消息[Group_SendMsg]";
                }
                else if (i % 5 == 0)
                {
                    entry.LogType = RunLogType.ReceiveFriend;
                    entry.SenderId = "484274564";
                    entry.Message = $"{i}....16558453379511--1766286766785";
                }
                else
                {
                    entry.LogType = RunLogType.SendSuccess;
                    entry.Period = "530502151";
                    entry.GroupId = "333338888";
                    entry.Message = $"(群333338888) 用户{i}\\n攻击: X{i * 10} DAS{i}";
                }

                _logItems.Add(new LogDisplayItem(entry));
            }
        }

        /// <summary>
        /// Cell formatting - 类型列蓝色，响应列特殊格式蓝色
        /// </summary>
        private void DgvLog_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            var colName = dgvLog.Columns[e.ColumnIndex].Name;

            // Type column - blue text
            if (colName == "Type" && e.Value != null)
            {
                e.CellStyle.ForeColor = Color.Blue;
            }

            // Period column - blue for special values
            if (colName == "Period" && e.Value != null)
            {
                var val = e.Value.ToString();
                if (val.Contains("好友") || val == "插件")
                {
                    e.CellStyle.ForeColor = Color.Blue;
                }
            }

            // Time column - blue for plugin entries
            if (colName == "Time")
            {
                var row = dgvLog.Rows[e.RowIndex];
                var typeCell = row.Cells["Type"];
                if (typeCell.Value != null && typeCell.Value.ToString() == "插件")
                {
                    e.CellStyle.ForeColor = Color.Blue;
                }
            }
        }

        /// <summary>
        /// Load existing logs
        /// </summary>
        private void LoadExistingLogs()
        {
            var entries = RunLogService.Instance.GetEntries();
            foreach (var entry in entries)
            {
                _logItems.Add(new LogDisplayItem(entry));
            }

            ScrollToBottom();
        }

        /// <summary>
        /// Subscribe to log service
        /// </summary>
        private void SubscribeToLogService()
        {
            RunLogService.Instance.OnNewEntry += OnNewLogEntry;
        }

        /// <summary>
        /// Handle new log entry
        /// </summary>
        private void OnNewLogEntry(RunLogEntry entry)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<RunLogEntry>(OnNewLogEntry), entry);
                return;
            }

            lock (_updateLock)
            {
                _logItems.Add(new LogDisplayItem(entry));

                // Keep max 500 items
                while (_logItems.Count > 500)
                    _logItems.RemoveAt(0);
            }

            ScrollToBottom();
        }

        private void ScrollToBottom()
        {
            if (dgvLog.Rows.Count > 0)
            {
                dgvLog.FirstDisplayedScrollingRowIndex = dgvLog.Rows.Count - 1;
            }
        }

        /// <summary>
        /// Start/Stop button click
        /// </summary>
        private void BtnStartGame_Click(object sender, EventArgs e)
        {
            if (!RunLogService.Instance.IsRunning)
            {
                RunLogService.Instance.Start();
                btnStartGame.Text = "停止游戏";
                btnStartGame.BackColor = Color.FromArgb(255, 180, 180);
            }
            else
            {
                RunLogService.Instance.Stop();
                btnStartGame.Text = "开始游戏";
                btnStartGame.BackColor = Color.FromArgb(144, 238, 144);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                RunLogService.Instance.OnNewEntry -= OnNewLogEntry;
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// Display item for binding
        /// </summary>
        private class LogDisplayItem
        {
            public int Id { get; set; }
            public string TimeDisplay { get; set; }
            public string Period { get; set; }
            public string TypeDisplay { get; set; }
            public string Message { get; set; }

            public LogDisplayItem(RunLogEntry entry)
            {
                Id = entry.Id;
                TimeDisplay = entry.Time.ToString("MM-dd HH:mm:ss");

                // Format period based on type
                switch (entry.LogType)
                {
                    case RunLogType.ReceiveFriend:
                        Period = $"好友[{entry.SenderId}]";
                        break;
                    case RunLogType.Plugin:
                    case RunLogType.Hook:
                        Period = "插件";
                        break;
                    default:
                        Period = string.IsNullOrEmpty(entry.Period) ? "" : entry.Period;
                        break;
                }

                TypeDisplay = entry.TypeDisplay;
                Message = entry.Message?.Replace("\\n", " ").Replace("\n", " ");
            }
        }
    }
}
