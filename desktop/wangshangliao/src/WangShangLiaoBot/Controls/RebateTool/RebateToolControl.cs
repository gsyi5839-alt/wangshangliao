using System;
using System.Drawing;
using System.Windows.Forms;

namespace WangShangLiaoBot.Controls.RebateTool
{
    /// <summary>
    /// Rebate Tool main control - contains top navigation bar and tab pages
    /// 回水工具主控件 - 包含顶部导航栏和二级标签页
    /// </summary>
    public class RebateToolControl : UserControl
    {
        // =====================================================
        // Top Navigation Bar Controls (一级导航栏)
        // =====================================================
        
        private ComboBox cmbQuickSelect;          // Quick time selection dropdown
        private Label lblQueryTime;               // "查询时间" label
        private DateTimePicker dtpStartDate;      // Start date picker
        private DateTimePicker dtpStartTime;      // Start time picker
        private Label lblArrow;                   // "→" arrow label
        private DateTimePicker dtpEndDate;        // End date picker
        private DateTimePicker dtpEndTime;        // End time picker
        private Button btnClearData;              // Clear data button
        private Button btnOperationLog;           // Operation log button
        
        // =====================================================
        // Second Level Tab Controls (二级标签栏)
        // =====================================================
        
        private Panel pnlTabBar;                  // Tab bar panel
        private Panel pnlContent;                 // Content panel
        
        // Tab buttons array
        private Button[] _tabButtons;
        private int _selectedTabIndex = 0;
        
        // Tab names
        private readonly string[] TabNames = new string[]
        {
            "统计所有",
            "回水计算",
            "回水设置",
            "单独查询",
            "夜宵计算",
            "夜宵设置",
            "上下分记录",
            "艾特分记录",
            "每期盈利",
            "庄家盈利",
            "邀请记录",
            "记录删除",
            "自动反水"
        };
        
        // Tab content panels (placeholder for future content)
        private Panel[] _tabPanels;
        
        // Colors
        private readonly Color TabSelectedColor = Color.White;
        private readonly Color TabNormalColor = Color.FromArgb(240, 240, 240);
        private readonly Color TabBorderColor = Color.FromArgb(180, 180, 180);
        private readonly Color HeaderBgColor = Color.FromArgb(250, 250, 250);
        
        public RebateToolControl()
        {
            InitializeComponent();
            InitializeTabPanels();
            SelectTab(0);
        }
        
        /// <summary>
        /// Initialize all UI components
        /// </summary>
        private void InitializeComponent()
        {
            this.SuspendLayout();
            
            // Main control settings
            this.BackColor = Color.White;
            this.Dock = DockStyle.Fill;
            this.Font = new Font("Microsoft YaHei UI", 9F);
            
            // Create content panel first (will be at bottom due to Fill dock)
            CreateContentPanel();
            
            // Create tab bar (below navigation bar)
            CreateTabBar();
            
            // Create navigation bar (at top)
            CreateTopNavigationBar();
            
            this.ResumeLayout(false);
        }
        
        /// <summary>
        /// Create top navigation bar with time selection controls
        /// </summary>
        private void CreateTopNavigationBar()
        {
            // Container panel for top navigation
            var pnlTopNav = new Panel
            {
                Height = 35,
                Dock = DockStyle.Top,
                BackColor = HeaderBgColor,
                Padding = new Padding(5, 5, 5, 5)
            };
            
            int xPos = 5;
            
            // Quick select time dropdown
            cmbQuickSelect = new ComboBox
            {
                Location = new Point(xPos, 5),
                Size = new Size(100, 23),
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat
            };
            cmbQuickSelect.Items.AddRange(new object[] 
            { 
                "快速选择时间",
                "今天",
                "昨天",
                "本周",
                "上周",
                "本月",
                "上月"
            });
            cmbQuickSelect.SelectedIndex = 0;
            cmbQuickSelect.SelectedIndexChanged += CmbQuickSelect_SelectedIndexChanged;
            pnlTopNav.Controls.Add(cmbQuickSelect);
            xPos += 110;
            
            // Query time label
            lblQueryTime = new Label
            {
                Text = "查询时间",
                Location = new Point(xPos, 8),
                AutoSize = true
            };
            pnlTopNav.Controls.Add(lblQueryTime);
            xPos += 60;
            
            // Start date picker
            dtpStartDate = new DateTimePicker
            {
                Location = new Point(xPos, 5),
                Size = new Size(110, 23),
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "yyyy年MM月dd日",
                Value = DateTime.Today
            };
            pnlTopNav.Controls.Add(dtpStartDate);
            xPos += 115;
            
            // Start time picker
            dtpStartTime = new DateTimePicker
            {
                Location = new Point(xPos, 5),
                Size = new Size(75, 23),
                Format = DateTimePickerFormat.Time,
                ShowUpDown = true,
                Value = DateTime.Today.AddHours(20)
            };
            pnlTopNav.Controls.Add(dtpStartTime);
            xPos += 80;
            
            // Arrow label
            lblArrow = new Label
            {
                Text = "→",
                Location = new Point(xPos, 8),
                AutoSize = true
            };
            pnlTopNav.Controls.Add(lblArrow);
            xPos += 25;
            
            // End date picker
            dtpEndDate = new DateTimePicker
            {
                Location = new Point(xPos, 5),
                Size = new Size(110, 23),
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "yyyy年MM月dd日",
                Value = DateTime.Today.AddDays(1)
            };
            pnlTopNav.Controls.Add(dtpEndDate);
            xPos += 115;
            
            // End time picker
            dtpEndTime = new DateTimePicker
            {
                Location = new Point(xPos, 5),
                Size = new Size(75, 23),
                Format = DateTimePickerFormat.Time,
                ShowUpDown = true,
                Value = DateTime.Today.AddDays(1).AddHours(20)
            };
            pnlTopNav.Controls.Add(dtpEndTime);
            
            // Clear data button (right aligned)
            btnClearData = new Button
            {
                Text = "一键清除数据",
                Size = new Size(90, 25),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnClearData.FlatAppearance.BorderColor = TabBorderColor;
            btnClearData.Click += BtnClearData_Click;
            pnlTopNav.Controls.Add(btnClearData);
            
            // Operation log button (right aligned)
            btnOperationLog = new Button
            {
                Text = "操作记录",
                Size = new Size(75, 25),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnOperationLog.FlatAppearance.BorderColor = TabBorderColor;
            btnOperationLog.Click += BtnOperationLog_Click;
            pnlTopNav.Controls.Add(btnOperationLog);
            
            // Position right-aligned buttons
            pnlTopNav.Resize += (s, e) =>
            {
                btnOperationLog.Location = new Point(pnlTopNav.Width - btnOperationLog.Width - 10, 5);
                btnClearData.Location = new Point(btnOperationLog.Left - btnClearData.Width - 10, 5);
            };
            
            this.Controls.Add(pnlTopNav);
        }
        
        /// <summary>
        /// Create second level tab bar
        /// </summary>
        private void CreateTabBar()
        {
            pnlTabBar = new Panel
            {
                Height = 28,
                Dock = DockStyle.Top,
                BackColor = TabNormalColor,
                Padding = new Padding(0)
            };
            
            _tabButtons = new Button[TabNames.Length];
            int xPos = 0;
            
            for (int i = 0; i < TabNames.Length; i++)
            {
                var btn = new Button
                {
                    Text = TabNames[i],
                    Tag = i,
                    FlatStyle = FlatStyle.Flat,
                    Size = new Size(GetTabWidth(TabNames[i]), 27),
                    Location = new Point(xPos, 0),
                    BackColor = TabNormalColor,
                    ForeColor = Color.Black,
                    Cursor = Cursors.Hand,
                    Font = new Font("Microsoft YaHei UI", 9F)
                };
                btn.FlatAppearance.BorderSize = 0;
                btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(230, 230, 230);
                btn.Click += TabButton_Click;
                
                _tabButtons[i] = btn;
                pnlTabBar.Controls.Add(btn);
                xPos += btn.Width;
            }
            
            // Add bottom border line
            var borderLine = new Panel
            {
                Height = 1,
                Dock = DockStyle.Bottom,
                BackColor = TabBorderColor
            };
            pnlTabBar.Controls.Add(borderLine);
            
            this.Controls.Add(pnlTabBar);
        }
        
        /// <summary>
        /// Calculate tab button width based on text length
        /// </summary>
        private int GetTabWidth(string text)
        {
            // Base width + character count adjustment
            return Math.Max(60, text.Length * 14 + 16);
        }
        
        /// <summary>
        /// Create content panel for tab pages
        /// </summary>
        private void CreateContentPanel()
        {
            pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(5)
            };
            
            this.Controls.Add(pnlContent);
            
            // Ensure correct z-order (content at bottom)
            pnlContent.SendToBack();
        }
        
        /// <summary>
        /// Initialize placeholder panels for each tab
        /// </summary>
        private void InitializeTabPanels()
        {
            _tabPanels = new Panel[TabNames.Length];
            
            for (int i = 0; i < TabNames.Length; i++)
            {
                var panel = new Panel
                {
                    Dock = DockStyle.Fill,
                    BackColor = Color.White,
                    Visible = false,
                    Tag = TabNames[i]
                };
                
                // Add placeholder label (for development)
                var lblPlaceholder = new Label
                {
                    Text = $"【{TabNames[i]}】\n\n内容开发中...",
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    ForeColor = Color.Gray,
                    Font = new Font("Microsoft YaHei UI", 12F)
                };
                panel.Controls.Add(lblPlaceholder);
                
                _tabPanels[i] = panel;
                pnlContent.Controls.Add(panel);
            }
        }
        
        /// <summary>
        /// Select a tab by index
        /// </summary>
        public void SelectTab(int index)
        {
            if (index < 0 || index >= TabNames.Length)
                return;
            
            _selectedTabIndex = index;
            
            // Update tab button styles
            for (int i = 0; i < _tabButtons.Length; i++)
            {
                if (i == index)
                {
                    _tabButtons[i].BackColor = TabSelectedColor;
                    _tabButtons[i].FlatAppearance.BorderSize = 1;
                    _tabButtons[i].FlatAppearance.BorderColor = TabBorderColor;
                }
                else
                {
                    _tabButtons[i].BackColor = TabNormalColor;
                    _tabButtons[i].FlatAppearance.BorderSize = 0;
                }
            }
            
            // Show/hide tab panels
            for (int i = 0; i < _tabPanels.Length; i++)
            {
                _tabPanels[i].Visible = (i == index);
            }
            
            // Fire tab changed event
            OnTabChanged?.Invoke(this, index);
        }
        
        // =====================================================
        // Event Handlers
        // =====================================================
        
        private void TabButton_Click(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.Tag is int index)
            {
                SelectTab(index);
            }
        }
        
        private void CmbQuickSelect_SelectedIndexChanged(object sender, EventArgs e)
        {
            var now = DateTime.Now;
            
            switch (cmbQuickSelect.SelectedIndex)
            {
                case 1: // Today
                    dtpStartDate.Value = DateTime.Today;
                    dtpStartTime.Value = DateTime.Today;
                    dtpEndDate.Value = DateTime.Today;
                    dtpEndTime.Value = now;
                    break;
                case 2: // Yesterday
                    dtpStartDate.Value = DateTime.Today.AddDays(-1);
                    dtpStartTime.Value = DateTime.Today.AddDays(-1);
                    dtpEndDate.Value = DateTime.Today.AddDays(-1);
                    dtpEndTime.Value = DateTime.Today.AddDays(-1).AddHours(23).AddMinutes(59);
                    break;
                case 3: // This week
                    var startOfWeek = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek + 1);
                    dtpStartDate.Value = startOfWeek;
                    dtpStartTime.Value = startOfWeek;
                    dtpEndDate.Value = DateTime.Today;
                    dtpEndTime.Value = now;
                    break;
                case 4: // Last week
                    var lastWeekStart = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek - 6);
                    var lastWeekEnd = lastWeekStart.AddDays(6);
                    dtpStartDate.Value = lastWeekStart;
                    dtpStartTime.Value = lastWeekStart;
                    dtpEndDate.Value = lastWeekEnd;
                    dtpEndTime.Value = lastWeekEnd.AddHours(23).AddMinutes(59);
                    break;
                case 5: // This month
                    dtpStartDate.Value = new DateTime(now.Year, now.Month, 1);
                    dtpStartTime.Value = new DateTime(now.Year, now.Month, 1);
                    dtpEndDate.Value = DateTime.Today;
                    dtpEndTime.Value = now;
                    break;
                case 6: // Last month
                    var lastMonth = now.AddMonths(-1);
                    dtpStartDate.Value = new DateTime(lastMonth.Year, lastMonth.Month, 1);
                    dtpStartTime.Value = new DateTime(lastMonth.Year, lastMonth.Month, 1);
                    dtpEndDate.Value = new DateTime(lastMonth.Year, lastMonth.Month, 
                        DateTime.DaysInMonth(lastMonth.Year, lastMonth.Month));
                    dtpEndTime.Value = dtpEndDate.Value.AddHours(23).AddMinutes(59);
                    break;
            }
        }
        
        private void BtnClearData_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "确定要清除所有数据吗？此操作不可恢复！",
                "确认清除",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);
            
            if (result == DialogResult.Yes)
            {
                OnClearDataRequested?.Invoke(this, EventArgs.Empty);
            }
        }
        
        private void BtnOperationLog_Click(object sender, EventArgs e)
        {
            OnOperationLogRequested?.Invoke(this, EventArgs.Empty);
        }
        
        // =====================================================
        // Public Events
        // =====================================================
        
        /// <summary>
        /// Fired when tab selection changes
        /// </summary>
        public event EventHandler<int> OnTabChanged;
        
        /// <summary>
        /// Fired when clear data button is clicked
        /// </summary>
        public event EventHandler OnClearDataRequested;
        
        /// <summary>
        /// Fired when operation log button is clicked
        /// </summary>
        public event EventHandler OnOperationLogRequested;
        
        // =====================================================
        // Public Properties
        // =====================================================
        
        /// <summary>
        /// Get query start datetime
        /// </summary>
        public DateTime QueryStartTime => 
            dtpStartDate.Value.Date.Add(dtpStartTime.Value.TimeOfDay);
        
        /// <summary>
        /// Get query end datetime
        /// </summary>
        public DateTime QueryEndTime => 
            dtpEndDate.Value.Date.Add(dtpEndTime.Value.TimeOfDay);
        
        /// <summary>
        /// Get selected tab index
        /// </summary>
        public int SelectedTabIndex => _selectedTabIndex;
        
        /// <summary>
        /// Get selected tab name
        /// </summary>
        public string SelectedTabName => TabNames[_selectedTabIndex];
        
        /// <summary>
        /// Get content panel for a specific tab (for adding custom content)
        /// </summary>
        public Panel GetTabPanel(int index)
        {
            if (index >= 0 && index < _tabPanels.Length)
                return _tabPanels[index];
            return null;
        }
        
        /// <summary>
        /// Get content panel by tab name
        /// </summary>
        public Panel GetTabPanel(string tabName)
        {
            var index = Array.IndexOf(TabNames, tabName);
            return GetTabPanel(index);
        }
    }
}

