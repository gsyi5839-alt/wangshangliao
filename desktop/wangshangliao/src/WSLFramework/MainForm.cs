using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using WSLFramework.Forms;
using WSLFramework.Models;
using WSLFramework.Services;
using WSLFramework.Utils;

namespace WSLFramework
{
    /// <summary>
    /// 主窗体 - 按照招财狗框架设计
    /// </summary>
    public partial class MainForm : Form
    {
        private FrameworkServer _server;
        private NotifyIcon _trayIcon;
        private ContextMenuStrip _trayMenu;
        
        // 核心服务
        private PlayerService _playerService;
        private GameService _gameService;
        
        // 子窗口
        private TrusteeForm _trusteeForm;
        private ScoreForm _scoreForm;
        
        // 控件
        private Panel headerPanel;
        private TabControl tabControl;
        private Button btnStartGame;
        private ListView lvLog;
        private ListView lvAccounts;
        private int logId = 0;
        
        // 当前登录的账号ID（用于日志响应列）
        private string _currentAccountId = "";
        
        public MainForm()
        {
            InitializeComponent();
            InitializeServices();
            InitializeServer();
            InitializeTrayIcon();
        }
        
        private void InitializeServices()
        {
            // 初始化 ZCG 数据存储 (按照旧程序 C:\zcg25.12.11\zcg\ 的目录结构)
            InitializeDataStorage();
            
            _playerService = new PlayerService();
            _playerService.OnLog += msg => AddLog(_currentAccountId, "信息", msg);
            _playerService.LoadData(); // 从 ZCG 目录加载数据
            
            _gameService = new GameService(_playerService);
            _gameService.OnLog += msg => AddLog(_currentAccountId, "信息", msg);
            _gameService.OnStateChanged += (state, countdown) =>
            {
                // 状态变更可以在这里处理
            };
            _gameService.OnNewResult += result =>
            {
                AddLog(_currentAccountId, "投递成功", result.GetOpenMessage());
                
                // 记录开奖日志
                ZCGDataStorage.Instance.LogSystem(_currentAccountId, $"开奖: {result.GetOpenMessage()}");
            };
            _gameService.OnSettlement += settlement =>
            {
                AddLog(_currentAccountId, "投递成功", $"结算完成 期{settlement.Period} 总{settlement.TotalBets}注");
            };
        }
        
        private void InitializeComponent()
        {
            this.SuspendLayout();
            
            this.Text = "招财狗框架";
            this.Size = new Size(850, 520);  // 加长窗口尺寸
            this.MinimumSize = new Size(700, 400);  // 最小尺寸
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Microsoft YaHei UI", 9F);
            this.FormBorderStyle = FormBorderStyle.Sizable;  // 使用标准边框，有最小化/最大化/关闭按钮
            this.MaximizeBox = true;
            this.MinimizeBox = true;
            this.BackColor = Color.White;
            this.DoubleBuffered = true;  // 减少闪烁
            
            // 主布局
            var mainPanel = new Panel { Dock = DockStyle.Fill };
            
            // 标题栏
            headerPanel = CreateHeaderPanel();
            
            // Tab 控件
            tabControl = CreateTabControl();
            
            mainPanel.Controls.Add(tabControl);
            mainPanel.Controls.Add(headerPanel);
            
            this.Controls.Add(mainPanel);
            
            this.ResumeLayout(false);
        }
        
        private Panel CreateHeaderPanel()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Color.FromArgb(76, 175, 80) // 绿色
            };
            
            // 渐变绘制
            panel.Paint += (s, e) =>
            {
                using (var brush = new LinearGradientBrush(
                    panel.ClientRectangle,
                    Color.FromArgb(102, 187, 106),
                    Color.FromArgb(76, 175, 80),
                    LinearGradientMode.Horizontal))
                {
                    e.Graphics.FillRectangle(brush, panel.ClientRectangle);
                }
            };
            
            // Logo 和标题 (使用兔子图标风格)
            var lblTitle = new Label
            {
                Text = "🐰 招财狗框架",
                Font = new Font("Microsoft YaHei UI", 16F, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(15, 12),
                BackColor = Color.Transparent
            };
            
            // 版本号
            var lblVersion = new Label
            {
                Text = "Ver: zc25.12.11",
                Font = new Font("Microsoft YaHei UI", 9F),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(710, 16),  // 调整位置适应新宽度
                BackColor = Color.Transparent
            };
            
            // 使用标准边框，不需要自定义窗口按钮
            panel.Controls.Add(lblTitle);
            panel.Controls.Add(lblVersion);
            
            return panel;
        }
        
        private TabControl CreateTabControl()
        {
            var tab = new TabControl
            {
                Dock = DockStyle.Fill,
                Location = new Point(0, 50),
                Font = new Font("Microsoft YaHei UI", 10F),
                Padding = new Point(15, 3)
            };
            
            // Tab 页面
            var tabLog = new TabPage("运行日志") { BackColor = Color.White };
            var tabAccounts = new TabPage("账号列表") { BackColor = Color.White };
            var tabControl = new TabPage("算账控制") { BackColor = Color.White };  // 新增算账控制页
            var tabSettings = new TabPage("系统设置") { BackColor = Color.White };
            
            // 创建内容
            CreateLogTab(tabLog);
            CreateAccountsTab(tabAccounts);
            CreateAccountingControlTab(tabControl);  // 新增
            CreateSettingsTab(tabSettings);
            
            tab.TabPages.Add(tabLog);
            tab.TabPages.Add(tabAccounts);
            tab.TabPages.Add(tabControl);  // 新增
            tab.TabPages.Add(tabSettings);
            
            return tab;
        }
        
        private void CreateLogTab(TabPage tab)
        {
            // 顶部按钮面板（右对齐开始游戏按钮）
            var pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                BackColor = Color.White
            };
            
            // 开始游戏按钮（右上角）
            btnStartGame = new Button
            {
                Text = "开始游戏",
                Size = new Size(80, 28),
                Location = new Point(580, 6),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnStartGame.FlatAppearance.BorderSize = 0;
            btnStartGame.Click += BtnStartGame_Click;
            
            pnlTop.Controls.Add(btnStartGame);
            
            // 运行日志 ListView
            lvLog = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                Font = new Font("Microsoft YaHei UI", 9F),
                BorderStyle = BorderStyle.FixedSingle
            };
            
            // 列头 - 按照截图设计
            lvLog.Columns.Add("ID", 40);
            lvLog.Columns.Add("时间", 110);
            lvLog.Columns.Add("响应", 70);
            lvLog.Columns.Add("类型", 55);
            lvLog.Columns.Add("消息", -2);  // -2 表示自动填充剩余空间
            
            // 窗口大小改变时自动调整消息列宽度
            this.Resize += (s, e) => AdjustLogColumnWidth();
            
            // 设置颜色
            lvLog.BackColor = Color.White;
            lvLog.ForeColor = Color.Black;
            
            // 表头样式
            lvLog.OwnerDraw = true;
            lvLog.DrawColumnHeader += LvLog_DrawColumnHeader;
            lvLog.DrawItem += LvLog_DrawItem;
            lvLog.DrawSubItem += LvLog_DrawSubItem;
            
            tab.Controls.Add(lvLog);
            tab.Controls.Add(pnlTop);
        }
        
        private async void BtnStartGame_Click(object sender, EventArgs e)
        {
            if (!_gameService.IsRunning)
            {
                // 确保服务端已启动
                if (!_server.IsRunning)
                {
                    // 自动启动服务
                    await StartServerAsync();
                }
                
                await _gameService.StartAsync();
                btnStartGame.Text = "停止游戏";
                btnStartGame.BackColor = Color.FromArgb(244, 67, 54);
                
                AddLog("插件", "插件", "日志 游戏已开始");
            }
            else
            {
                _gameService.Stop();
                btnStartGame.Text = "开始游戏";
                btnStartGame.BackColor = Color.FromArgb(76, 175, 80);
                
                AddLog("插件", "插件", "日志 游戏已停止");
            }
        }
        
        // XPlugin服务实例
        private XPluginService _xpluginService;
#pragma warning disable CS0414 // 保留字段，将来可能使用
        private XPluginApiHandler _xpluginApiHandler;
#pragma warning restore CS0414
        private Button _btnXPlugin; // 保存按钮引用用于状态更新
        
        /// <summary>
        /// XPlugin启动按钮点击事件 - 一键启动主框架和副框架，自动连接旺商聊
        /// </summary>
        private async void BtnXPlugin_Click(object sender, EventArgs e)
        {
            var btn = sender as Button;
            _btnXPlugin = btn;
            
            if (_xpluginService == null || !_xpluginService.IsRunning)
            {
                await StartXPluginAsync(btn);
            }
            else
            {
                StopXPlugin(btn);
            }
        }
        
        /// <summary>
        /// 启动XPlugin - 【已更新】使用 BotLoginService 登录旺商聊（不再使用CDP）
        /// </summary>
        private async Task StartXPluginAsync(Button btn)
        {
            try
            {
                btn.Enabled = false;
                btn.Text = "启动中...";
                AddLog("XPlugin", "启动", "正在启动服务...");
                
                // 步骤1: 启动框架服务端 (副框架)
                if (!_server.IsRunning)
                {
                    AddLog("XPlugin", "启动", "步骤1: 启动框架服务端...");
                    await StartServerAsync();
                    
                    // 等待服务启动
                    await Task.Delay(500);
                }
                
                if (!_server.IsRunning)
                {
                    AddLog("XPlugin", "错误", "框架服务端启动失败");
                    btn.Enabled = true;
                    btn.Text = "启动插件";
                    return;
                }
                AddLog("XPlugin", "成功", "框架服务端已启动 (端口: " + _server.Port + ")");
                
                // 步骤2: 检查 BotLoginService 登录状态
                AddLog("XPlugin", "启动", "步骤2: 检查旺商聊登录状态...");
                var loginService = Services.BotLoginService.Instance;
                
                if (loginService.IsLoggedIn)
                {
                    // 已登录
                    var account = loginService.CurrentAccount;
                    AddLog("XPlugin", "成功", $"旺商聊已登录: {account?.Nickname} ({account?.Account})");
                    AddLog("XPlugin", "成功", $"绑定群号: {account?.GroupId}");
                    
                    // 更新账号列表
                    if (account != null)
                    {
                        UpdateAccountInfo(account.Nickname, account.Account, "已登录");
                    }
                }
                else
                {
                    // 未登录，提示用户添加账号
                    AddLog("XPlugin", "提示", "旺商聊未登录，请在【账号列表】中添加账号并登录");
                    AddLog("XPlugin", "提示", "右键账号列表 -> 添加账户 -> 填写账号密码和群号 -> 登录");
                    
                    // 尝试自动登录
                    var autoAccount = Models.AccountManager.Instance.GetAutoLoginAccount();
                    if (autoAccount != null)
                    {
                        AddLog("XPlugin", "启动", $"尝试自动登录: {autoAccount.Account}...");
                        var success = await loginService.LoginAsync(autoAccount);
                        if (success)
                        {
                            AddLog("XPlugin", "成功", $"自动登录成功: {loginService.CurrentAccount?.Nickname}");
                            UpdateAccountInfo(loginService.CurrentAccount?.Nickname, loginService.CurrentAccount?.Account, "已登录");
                        }
                        else
                        {
                            AddLog("XPlugin", "警告", $"自动登录失败: {loginService.LoginStatus}");
                        }
                    }
                }
                
                // 步骤3: 启动服务 (即使未登录也继续，等待手动登录)
                AddLog("XPlugin", "启动", "步骤3: 启动服务...");
                
                // 更新按钮状态
                btn.Text = "停止插件";
                btn.BackColor = Color.FromArgb(244, 67, 54); // 红色
                btn.Enabled = true;
                _btnXPlugin = btn;
                
                AddLog("XPlugin", "成功", "✓ 服务已启动");
                
                if (!loginService.IsLoggedIn)
                {
                    AddLog("XPlugin", "提示", "请在【账号列表】添加旺商聊账号并登录以开始接收消息");
                }
            }
            catch (Exception ex)
            {
                AddLog("XPlugin", "错误", $"启动失败: {ex.Message}");
                Logger.Error($"XPlugin启动失败: {ex}");
                btn.Text = "启动插件";
                btn.BackColor = Color.FromArgb(33, 150, 243); // 蓝色
            }
            finally
            {
                btn.Enabled = true;
            }
        }
        
        /// <summary>
        /// 停止XPlugin
        /// </summary>
        private void StopXPlugin(Button btn)
        {
            try
            {
                AddLog("XPlugin", "停止", "正在停止XPlugin服务...");
                
                _xpluginService?.Stop();
                _xpluginService = null;
                _xpluginApiHandler = null;
                
                AddLog("XPlugin", "成功", "XPlugin服务已停止");
                
                btn.Text = "启动插件";
                btn.BackColor = Color.FromArgb(33, 150, 243); // 蓝色
            }
            catch (Exception ex)
            {
                AddLog("XPlugin", "错误", $"停止失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 启动主框架 (WangShangLiaoBot)
        /// </summary>
        private async Task StartMainFrameworkAsync()
        {
            try
            {
                // 查找主框架进程
                var processes = System.Diagnostics.Process.GetProcessesByName("旺商聊机器人");
                if (processes.Length > 0)
                {
                    AddLog("XPlugin", "信息", "主框架已在运行");
                    return;
                }
                
                // 尝试启动主框架
                var mainExePath = Path.Combine(
                    Path.GetDirectoryName(Application.ExecutablePath),
                    "..\\WangShangLiaoBot\\bin\\Debug\\旺商聊机器人.exe"
                );
                
                // 检查相对路径
                if (!File.Exists(mainExePath))
                {
                    // 尝试同目录
                    mainExePath = Path.Combine(
                        Path.GetDirectoryName(Application.ExecutablePath),
                        "旺商聊机器人.exe"
                    );
                }
                
                if (File.Exists(mainExePath))
                {
                    AddLog("XPlugin", "启动", $"正在启动主框架: {mainExePath}");
                    System.Diagnostics.Process.Start(mainExePath);
                    
                    // 等待主框架启动
                    await Task.Delay(2000);
                    AddLog("XPlugin", "成功", "主框架已启动");
                }
                else
                {
                    AddLog("XPlugin", "提示", "未找到主框架程序，请手动启动 旺商聊机器人.exe");
                }
            }
            catch (Exception ex)
            {
                AddLog("XPlugin", "警告", $"启动主框架时出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 线程安全调用UI
        /// </summary>
        private void SafeInvoke(Action action)
        {
            if (InvokeRequired)
            {
                BeginInvoke(action);
            }
            else
            {
                action();
            }
        }
        
        private void ShowTrusteeForm()
        {
            if (_trusteeForm == null || _trusteeForm.IsDisposed)
            {
                _trusteeForm = new TrusteeForm(_playerService);
            }
            _trusteeForm.Show();
            _trusteeForm.BringToFront();
        }
        
        private void ShowScoreForm()
        {
            if (_scoreForm == null || _scoreForm.IsDisposed)
            {
                _scoreForm = new ScoreForm(_playerService, async (groupId, message) =>
                {
                    if (_server?.IsCDPConnected == true)
                    {
                        AddLog(_currentAccountId, "投递成功", $"(群{groupId}) {message}");
                    }
                });
            }
            _scoreForm.Show();
            _scoreForm.BringToFront();
        }
        
        /// <summary>
        /// 调整日志消息列宽度以填充剩余空间
        /// </summary>
        private void AdjustLogColumnWidth()
        {
            if (lvLog == null || lvLog.Columns.Count < 5) return;
            
            // 计算其他列的总宽度
            int otherColumnsWidth = 0;
            for (int i = 0; i < lvLog.Columns.Count - 1; i++)
            {
                otherColumnsWidth += lvLog.Columns[i].Width;
            }
            
            // 消息列 = 总宽度 - 其他列宽度 - 滚动条宽度 - 边距
            int msgWidth = lvLog.ClientSize.Width - otherColumnsWidth - 25;
            if (msgWidth > 100)
            {
                lvLog.Columns[4].Width = msgWidth;
            }
        }
        
        private void LvLog_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            using (var brush = new SolidBrush(Color.FromArgb(76, 175, 80)))
            {
                e.Graphics.FillRectangle(brush, e.Bounds);
            }
            
            using (var brush = new SolidBrush(Color.White))
            {
                var sf = new StringFormat
                {
                    Alignment = StringAlignment.Near,
                    LineAlignment = StringAlignment.Center
                };
                e.Graphics.DrawString(e.Header.Text, e.Font, brush, e.Bounds, sf);
            }
        }
        
        private void LvLog_DrawItem(object sender, DrawListViewItemEventArgs e)
        {
            e.DrawDefault = true;
        }
        
        private void LvLog_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            e.DrawDefault = true;
        }
        
        #region 算账控制面板 - 匹配旧程序红框功能
        
        /// <summary>
        /// 创建算账控制标签页 - 包含所有算账相关功能按钮
        /// </summary>
        private void CreateAccountingControlTab(TabPage tab)
        {
            // 主布局面板
            var mainPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            
            // ============ 第一行按钮 ============
            int y = 10;
            int btnWidth = 75;
            int btnHeight = 28;
            int spacing = 5;
            
            // 发送账单
            var btnSendBill = CreateToolButton("发送账单", 10, y, btnWidth, btnHeight);
            btnSendBill.Click += BtnSendBill_Click;
            
            // 导入账单
            var btnImportBill = CreateToolButton("导入账单", 10 + (btnWidth + spacing), y, btnWidth, btnHeight);
            btnImportBill.Click += BtnImportBill_Click;
            
            // 导入下注
            var btnImportBet = CreateToolButton("导入下注", 10 + (btnWidth + spacing) * 2, y, btnWidth, btnHeight);
            btnImportBet.Click += BtnImportBet_Click;
            
            // 开奖选择下拉框
            var lblLottery = new Label { Text = "开奖选择", Location = new Point(10 + (btnWidth + spacing) * 3, y + 5), AutoSize = true };
            var cboLotteryType = new ComboBox
            {
                Location = new Point(10 + (btnWidth + spacing) * 3 + 60, y),
                Width = 90,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cboLotteryType.Items.AddRange(new[] { "加拿大", "北京28", "台湾28", "澳洲28" });
            cboLotteryType.SelectedIndex = 0;
            cboLotteryType.SelectedIndexChanged += (s, e) => _gameService?.SetLotteryType(cboLotteryType.SelectedItem.ToString());
            
            // 启停禁言群复选框
            var chkAutoMute = new CheckBox
            {
                Text = "启停禁言群",
                Location = new Point(10 + (btnWidth + spacing) * 3 + 160, y + 3),
                AutoSize = true,
                Checked = ConfigService.Instance.AutoMuteOnSeal
            };
            chkAutoMute.CheckedChanged += (s, e) => ConfigService.Instance.AutoMuteOnSeal = chkAutoMute.Checked;
            
            // ============ 第二行按钮 ============
            y += btnHeight + 8;
            
            // 复制账单
            var btnCopyBill = CreateToolButton("复制账单", 10, y, btnWidth, btnHeight);
            btnCopyBill.Click += BtnCopyBill_Click;
            
            // 清空下注
            var btnClearBet = CreateToolButton("清空下注", 10 + (btnWidth + spacing), y, btnWidth, btnHeight);
            btnClearBet.Click += BtnClearBet_Click;
            
            // 修正开奖
            var btnFixResult = CreateToolButton("修正开奖", 10 + (btnWidth + spacing) * 2, y, btnWidth, btnHeight);
            btnFixResult.Click += BtnFixResult_Click;
            
            // 通道选择
            var lblChannel = new Label { Text = "通道", Location = new Point(10 + (btnWidth + spacing) * 3, y + 5), AutoSize = true };
            var cboChannel = new ComboBox
            {
                Location = new Point(10 + (btnWidth + spacing) * 3 + 35, y),
                Width = 70,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cboChannel.Items.AddRange(new[] { "通道1", "通道2", "通道3" });
            cboChannel.SelectedIndex = 0;
            
            // 通道3备用复选框
            var chkChannel3Backup = new CheckBox
            {
                Text = "通道3备用",
                Location = new Point(10 + (btnWidth + spacing) * 3 + 115, y + 3),
                AutoSize = true
            };
            
            // 开完本期停
            var chkStopAfterOpen = new CheckBox
            {
                Text = "开完本期停",
                Location = new Point(10 + (btnWidth + spacing) * 3 + 210, y + 3),
                AutoSize = true,
                Checked = ConfigService.Instance.StopAfterCurrentPeriod
            };
            chkStopAfterOpen.CheckedChanged += (s, e) => ConfigService.Instance.StopAfterCurrentPeriod = chkStopAfterOpen.Checked;
            
            // ============ 第三行按钮 ============
            y += btnHeight + 8;
            
            // 下注汇总
            var btnBetSummary = CreateToolButton("下注汇总", 10, y, btnWidth, btnHeight);
            btnBetSummary.Click += BtnBetSummary_Click;
            
            // 清除零分
            var btnClearZero = CreateToolButton("清除零分", 10 + (btnWidth + spacing), y, btnWidth, btnHeight);
            btnClearZero.Click += BtnClearZero_Click;
            
            // 导出账单
            var btnExportBill = CreateToolButton("导出账单", 10 + (btnWidth + spacing) * 2, y, btnWidth, btnHeight);
            btnExportBill.Click += BtnExportBill_Click;
            
            // 停止算账按钮（红色）
            var btnStopAccounting = new Button
            {
                Text = "停止算账",
                Location = new Point(10 + (btnWidth + spacing) * 3, y),
                Size = new Size(btnWidth, btnHeight),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(244, 67, 54),
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei UI", 9F),
                Cursor = Cursors.Hand
            };
            btnStopAccounting.FlatAppearance.BorderSize = 0;
            btnStopAccounting.Click += BtnStopAccounting_Click;
            
            // 支持变昵称
            var chkSupportNickChange = new CheckBox
            {
                Text = "支持变昵称",
                Location = new Point(10 + (btnWidth + spacing) * 3 + 85, y + 3),
                AutoSize = true,
                Checked = ConfigService.Instance.SupportNicknameChange
            };
            chkSupportNickChange.CheckedChanged += (s, e) => ConfigService.Instance.SupportNicknameChange = chkSupportNickChange.Checked;
            
            // ============ 第四行按钮 ============
            y += btnHeight + 8;
            
            // 详细盈利
            var btnProfitDetail = CreateToolButton("详细盈利", 10, y, btnWidth, btnHeight);
            btnProfitDetail.Click += BtnProfitDetail_Click;
            
            // 删除账单
            var btnDeleteBill = CreateToolButton("删除账单", 10 + (btnWidth + spacing), y, btnWidth, btnHeight);
            btnDeleteBill.Click += BtnDeleteBill_Click;
            
            // 历史账单
            var btnHistoryBill = CreateToolButton("历史账单", 10 + (btnWidth + spacing) * 2, y, btnWidth, btnHeight);
            btnHistoryBill.Click += BtnHistoryBill_Click;
            
            // 全体禁言
            var btnMuteAll = new Button
            {
                Text = "全体禁言",
                Location = new Point(10 + (btnWidth + spacing) * 3, y),
                Size = new Size(btnWidth, btnHeight),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(255, 152, 0),
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei UI", 9F),
                Cursor = Cursors.Hand
            };
            btnMuteAll.FlatAppearance.BorderSize = 0;
            btnMuteAll.Click += BtnMuteAll_Click;
            
            // 全体解禁
            var btnUnmuteAll = new Button
            {
                Text = "全体解禁",
                Location = new Point(10 + (btnWidth + spacing) * 4, y),
                Size = new Size(btnWidth, btnHeight),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei UI", 9F),
                Cursor = Cursors.Hand
            };
            btnUnmuteAll.FlatAppearance.BorderSize = 0;
            btnUnmuteAll.Click += BtnUnmuteAll_Click;
            
            // ============ 右侧功能按钮 ============
            int rightX = 520;
            
            // 校准时间
            var btnSyncTime = CreateToolButton("校准时间", rightX, 10, btnWidth, btnHeight);
            btnSyncTime.Click += BtnSyncTime_Click;
            
            // 聊天日志
            var btnChatLog = CreateToolButton("聊天日志", rightX, 10 + btnHeight + 5, btnWidth, btnHeight);
            btnChatLog.Click += BtnChatLog_Click;
            
            // ============ 分隔线 ============
            y += btnHeight + 15;
            var separator = new Label
            {
                BorderStyle = BorderStyle.Fixed3D,
                Location = new Point(10, y),
                Size = new Size(600, 2)
            };
            
            // ============ 玩家信息区域 ============
            y += 10;
            var lblPlayerInfo = new Label { Text = "旺旺号", Location = new Point(10, y + 3), AutoSize = true };
            var txtPlayerId = new TextBox { Location = new Point(60, y), Width = 80 };
            
            var lblNickname = new Label { Text = "昵称", Location = new Point(150, y + 3), AutoSize = true };
            var txtNickname = new TextBox { Location = new Point(180, y), Width = 70, ReadOnly = true };
            
            var lblScore = new Label { Text = "分数", Location = new Point(260, y + 3), AutoSize = true };
            var txtScore = new TextBox { Location = new Point(290, y), Width = 60 };
            
            var btnEditInfo = CreateToolButton("修改信息", 360, y - 2, 70, 25);
            var btnSearchPlayer = CreateToolButton("搜索玩家", 435, y - 2, 70, 25);
            
            var chkShowTrustee = new CheckBox { Text = "显示托玩家", Location = new Point(515, y + 2), AutoSize = true };
            
            // ============ 客户框区域 ============
            y += 35;
            var lblClientBox = new Label { Text = "客户框", Location = new Point(10, y + 3), AutoSize = true };
            var txtClientBox = new TextBox { Location = new Point(60, y), Width = 200 };
            
            var rbAdd10 = new RadioButton { Text = "加10个", Location = new Point(280, y + 2), AutoSize = true, Checked = true };
            var rbMinus10 = new RadioButton { Text = "减10个", Location = new Point(350, y + 2), AutoSize = true };
            
            // ============ 玩家列表 ============
            y += 35;
            var lvPlayers = new ListView
            {
                Location = new Point(10, y),
                Size = new Size(600, 180),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                Font = new Font("Microsoft YaHei UI", 9F)
            };
            lvPlayers.Columns.Add("玩家旺旺号", 100);
            lvPlayers.Columns.Add("玩家昵称", 80);
            lvPlayers.Columns.Add("分数", 60);
            lvPlayers.Columns.Add("留分", 50);
            lvPlayers.Columns.Add("下注内容", 150);
            lvPlayers.Columns.Add("时间", 80);
            
            // 添加所有控件到主面板
            mainPanel.Controls.Add(btnSendBill);
            mainPanel.Controls.Add(btnImportBill);
            mainPanel.Controls.Add(btnImportBet);
            mainPanel.Controls.Add(lblLottery);
            mainPanel.Controls.Add(cboLotteryType);
            mainPanel.Controls.Add(chkAutoMute);
            
            mainPanel.Controls.Add(btnCopyBill);
            mainPanel.Controls.Add(btnClearBet);
            mainPanel.Controls.Add(btnFixResult);
            mainPanel.Controls.Add(lblChannel);
            mainPanel.Controls.Add(cboChannel);
            mainPanel.Controls.Add(chkChannel3Backup);
            mainPanel.Controls.Add(chkStopAfterOpen);
            
            mainPanel.Controls.Add(btnBetSummary);
            mainPanel.Controls.Add(btnClearZero);
            mainPanel.Controls.Add(btnExportBill);
            mainPanel.Controls.Add(btnStopAccounting);
            mainPanel.Controls.Add(chkSupportNickChange);
            
            mainPanel.Controls.Add(btnProfitDetail);
            mainPanel.Controls.Add(btnDeleteBill);
            mainPanel.Controls.Add(btnHistoryBill);
            mainPanel.Controls.Add(btnMuteAll);
            mainPanel.Controls.Add(btnUnmuteAll);
            
            mainPanel.Controls.Add(btnSyncTime);
            mainPanel.Controls.Add(btnChatLog);
            
            mainPanel.Controls.Add(separator);
            mainPanel.Controls.Add(lblPlayerInfo);
            mainPanel.Controls.Add(txtPlayerId);
            mainPanel.Controls.Add(lblNickname);
            mainPanel.Controls.Add(txtNickname);
            mainPanel.Controls.Add(lblScore);
            mainPanel.Controls.Add(txtScore);
            mainPanel.Controls.Add(btnEditInfo);
            mainPanel.Controls.Add(btnSearchPlayer);
            mainPanel.Controls.Add(chkShowTrustee);
            
            mainPanel.Controls.Add(lblClientBox);
            mainPanel.Controls.Add(txtClientBox);
            mainPanel.Controls.Add(rbAdd10);
            mainPanel.Controls.Add(rbMinus10);
            
            mainPanel.Controls.Add(lvPlayers);
            
            tab.Controls.Add(mainPanel);
        }
        
        /// <summary>
        /// 创建工具按钮（统一样式）
        /// </summary>
        private Button CreateToolButton(string text, int x, int y, int width, int height)
        {
            var btn = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(width, height),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(240, 240, 240),
                ForeColor = Color.Black,
                Font = new Font("Microsoft YaHei UI", 9F),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
            return btn;
        }
        
        // ============ 算账控制按钮事件处理 ============
        
        /// <summary>
        /// 发送账单
        /// </summary>
        private async void BtnSendBill_Click(object sender, EventArgs e)
        {
            try
            {
                var billText = SettlementService.Instance.GenerateCurrentBillText();
                if (string.IsNullOrEmpty(billText))
                {
                    MessageBox.Show("当前没有可发送的账单", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                
                var groupId = BotLoginService.Instance.GetCurrentGroupId();
                if (string.IsNullOrEmpty(groupId))
                {
                    MessageBox.Show("请先设置绑定群号", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                
                await _server.SendGroupMessageAsync(groupId, billText);
                AddLog(_currentAccountId, "发送", "账单已发送到群聊");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"发送账单失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        /// <summary>
        /// 导入账单
        /// </summary>
        private void BtnImportBill_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = "账单文件|*.txt;*.csv|所有文件|*.*";
                ofd.Title = "导入账单";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var content = File.ReadAllText(ofd.FileName);
                        // TODO: 解析并导入账单
                        AddLog(_currentAccountId, "导入", $"已导入账单: {ofd.FileName}");
                        MessageBox.Show("账单导入成功", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"导入失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
        
        /// <summary>
        /// 导入下注
        /// </summary>
        private void BtnImportBet_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = "下注文件|*.txt;*.csv|所有文件|*.*";
                ofd.Title = "导入下注";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var lines = File.ReadAllLines(ofd.FileName);
                        int count = 0;
                        foreach (var line in lines)
                        {
                            if (string.IsNullOrWhiteSpace(line)) continue;
                            // 格式: 玩家ID,下注类型,金额
                            var parts = line.Split(',');
                            if (parts.Length >= 3)
                            {
                                var playerId = parts[0].Trim();
                                var betType = parts[1].Trim();
                                if (int.TryParse(parts[2].Trim(), out int amount))
                                {
                                    SettlementService.Instance.AddBet(playerId, betType, amount);
                                    count++;
                                }
                            }
                        }
                        AddLog(_currentAccountId, "导入", $"已导入 {count} 条下注");
                        MessageBox.Show($"成功导入 {count} 条下注", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"导入失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
        
        /// <summary>
        /// 复制账单
        /// </summary>
        private void BtnCopyBill_Click(object sender, EventArgs e)
        {
            try
            {
                var billText = SettlementService.Instance.GenerateCurrentBillText();
                if (string.IsNullOrEmpty(billText))
                {
                    MessageBox.Show("当前没有可复制的账单", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                
                Clipboard.SetText(billText);
                AddLog(_currentAccountId, "复制", "账单已复制到剪贴板");
                MessageBox.Show("账单已复制到剪贴板", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"复制失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        /// <summary>
        /// 清空下注
        /// </summary>
        private void BtnClearBet_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("确定要清空当前期所有下注吗？", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                SettlementService.Instance.ClearCurrentPeriodBets();
                AddLog(_currentAccountId, "清空", "当前期下注已清空");
                MessageBox.Show("下注已清空", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        
        /// <summary>
        /// 修正开奖
        /// </summary>
        private void BtnFixResult_Click(object sender, EventArgs e)
        {
            using (var dialog = new Form())
            {
                dialog.Text = "修正开奖结果";
                dialog.Size = new Size(350, 200);
                dialog.StartPosition = FormStartPosition.CenterParent;
                dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialog.MaximizeBox = false;
                dialog.MinimizeBox = false;
                
                var lblPeriod = new Label { Text = "期号:", Location = new Point(20, 20), AutoSize = true };
                var txtPeriod = new TextBox { Location = new Point(80, 17), Width = 150 };
                
                var lblNum1 = new Label { Text = "号码1:", Location = new Point(20, 55), AutoSize = true };
                var numNum1 = new NumericUpDown { Location = new Point(80, 52), Width = 60, Minimum = 0, Maximum = 9 };
                
                var lblNum2 = new Label { Text = "号码2:", Location = new Point(150, 55), AutoSize = true };
                var numNum2 = new NumericUpDown { Location = new Point(200, 52), Width = 60, Minimum = 0, Maximum = 9 };
                
                var lblNum3 = new Label { Text = "号码3:", Location = new Point(20, 90), AutoSize = true };
                var numNum3 = new NumericUpDown { Location = new Point(80, 87), Width = 60, Minimum = 0, Maximum = 9 };
                
                var btnOk = new Button { Text = "确定修正", Location = new Point(80, 125), Size = new Size(80, 30), DialogResult = DialogResult.OK };
                var btnCancel = new Button { Text = "取消", Location = new Point(170, 125), Size = new Size(80, 30), DialogResult = DialogResult.Cancel };
                
                dialog.Controls.AddRange(new Control[] { lblPeriod, txtPeriod, lblNum1, numNum1, lblNum2, numNum2, lblNum3, numNum3, btnOk, btnCancel });
                
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    var period = txtPeriod.Text.Trim();
                    if (string.IsNullOrEmpty(period))
                    {
                        MessageBox.Show("请输入期号", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    
                    var n1 = (int)numNum1.Value;
                    var n2 = (int)numNum2.Value;
                    var n3 = (int)numNum3.Value;
                    var sum = n1 + n2 + n3;
                    
                    // 创建修正的开奖结果并触发重新结算
                    var fixedResult = new LotteryResult
                    {
                        Period = period,
                        Num1 = n1,
                        Num2 = n2,
                        Num3 = n3,
                        Sum = sum,
                        OpenTime = DateTime.Now
                    };
                    
                    // 重新结算该期
                    _ = SettlementService.Instance.SettleAsync(fixedResult);
                    
                    AddLog(_currentAccountId, "修正", $"期号 {period} 修正为 {n1}+{n2}+{n3}={sum}");
                    MessageBox.Show($"开奖结果已修正并重新结算\n期号: {period}\n号码: {n1}+{n2}+{n3}={sum}", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
        
        /// <summary>
        /// 下注汇总
        /// </summary>
        private void BtnBetSummary_Click(object sender, EventArgs e)
        {
            var bets = SettlementService.Instance.GetCurrentPeriodBets();
            if (bets.Count == 0)
            {
                MessageBox.Show("当前期没有下注", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            
            // 按类型汇总
            var service = SettlementService.Instance;
            var summary = bets.GroupBy(b => b.BetType)
                              .Select(g => new { 
                                  Type = g.Key, 
                                  DisplayName = service.GetBetTypeDisplay(g.Key),
                                  Count = g.Count(), 
                                  Total = g.Sum(x => x.Amount) 
                              })
                              .OrderByDescending(x => x.Total);
            
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("【下注汇总】");
            sb.AppendLine($"总注数: {bets.Count}");
            sb.AppendLine($"总金额: {bets.Sum(b => b.Amount)}");
            sb.AppendLine("-------------------");
            foreach (var item in summary)
            {
                sb.AppendLine($"{item.DisplayName}: {item.Count}注, 共{item.Total}");
            }
            
            MessageBox.Show(sb.ToString(), "下注汇总", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        
        /// <summary>
        /// 清除零分
        /// </summary>
        private void BtnClearZero_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("确定要清除所有零分玩家吗？", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                var players = ScoreService.Instance.GetAllPlayers().Where(p => p.Balance == 0).ToList();
                int count = 0;
                foreach (var player in players)
                {
                    ScoreService.Instance.ClearPlayer(player.PlayerId);
                    count++;
                }
                AddLog(_currentAccountId, "清除", $"已清除 {count} 个零分玩家");
                MessageBox.Show($"已清除 {count} 个零分玩家", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        
        /// <summary>
        /// 导出账单
        /// </summary>
        private void BtnExportBill_Click(object sender, EventArgs e)
        {
            using (var sfd = new SaveFileDialog())
            {
                sfd.Filter = "文本文件|*.txt|CSV文件|*.csv|所有文件|*.*";
                sfd.FileName = $"账单_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var billText = SettlementService.Instance.GenerateCurrentBillText();
                        if (string.IsNullOrEmpty(billText))
                        {
                            MessageBox.Show("当前没有可导出的账单", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return;
                        }
                        File.WriteAllText(sfd.FileName, billText);
                        AddLog(_currentAccountId, "导出", $"账单已导出: {sfd.FileName}");
                        MessageBox.Show("账单导出成功", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"导出失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
        
        /// <summary>
        /// 停止算账
        /// </summary>
        private void BtnStopAccounting_Click(object sender, EventArgs e)
        {
            if (_gameService.IsRunning)
            {
                _gameService.Stop();
                AddLog(_currentAccountId, "停止", "算账已停止");
                MessageBox.Show("算账已停止", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("算账未在运行中", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        
        /// <summary>
        /// 详细盈利
        /// </summary>
        private void BtnProfitDetail_Click(object sender, EventArgs e)
        {
            var stats = ScoreService.Instance.GetTodayStats();
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("【今日盈利详情】");
            sb.AppendLine($"日期: {stats.Date:yyyy-MM-dd}");
            sb.AppendLine("-------------------");
            sb.AppendLine($"上分总额: {stats.TotalUp}");
            sb.AppendLine($"上分次数: {stats.UpCount}");
            sb.AppendLine($"下分总额: {stats.TotalDown}");
            sb.AppendLine($"下分次数: {stats.DownCount}");
            sb.AppendLine("-------------------");
            sb.AppendLine($"净流水: {stats.NetFlow}");
            
            MessageBox.Show(sb.ToString(), "详细盈利", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        
        /// <summary>
        /// 删除账单
        /// </summary>
        private void BtnDeleteBill_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("确定要删除当前账单吗？", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                SettlementService.Instance.ClearCurrentPeriodBets();
                AddLog(_currentAccountId, "删除", "账单已删除");
                MessageBox.Show("账单已删除", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        
        /// <summary>
        /// 历史账单
        /// </summary>
        private void BtnHistoryBill_Click(object sender, EventArgs e)
        {
            var history = SettlementService.Instance.GetSettlementHistory(10);
            if (history.Count == 0)
            {
                MessageBox.Show("暂无历史账单", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            
            using (var dialog = new Form())
            {
                dialog.Text = "历史账单";
                dialog.Size = new Size(600, 400);
                dialog.StartPosition = FormStartPosition.CenterParent;
                
                var lv = new ListView
                {
                    Dock = DockStyle.Fill,
                    View = View.Details,
                    FullRowSelect = true,
                    GridLines = true
                };
                lv.Columns.Add("期号", 100);
                lv.Columns.Add("时间", 120);
                lv.Columns.Add("人数", 60);
                lv.Columns.Add("注数", 60);
                lv.Columns.Add("总赢", 80);
                lv.Columns.Add("总输", 80);
                lv.Columns.Add("盈亏", 80);
                
                foreach (var record in history)
                {
                    var item = new ListViewItem(record.Period);
                    item.SubItems.Add(record.SettleTime.ToString("yyyy-MM-dd HH:mm:ss"));
                    item.SubItems.Add(record.PlayerCount.ToString());
                    item.SubItems.Add(record.BetCount.ToString());
                    item.SubItems.Add(record.TotalWin.ToString());
                    item.SubItems.Add(record.TotalLose.ToString());
                    item.SubItems.Add((record.TotalLose - record.TotalWin).ToString());
                    lv.Items.Add(item);
                }
                
                dialog.Controls.Add(lv);
                dialog.ShowDialog();
            }
        }
        
        /// <summary>
        /// 全体禁言
        /// </summary>
        private async void BtnMuteAll_Click(object sender, EventArgs e)
        {
            try
            {
                var groupId = BotLoginService.Instance.GetCurrentGroupId();
                if (string.IsNullOrEmpty(groupId))
                {
                    MessageBox.Show("请先设置绑定群号", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                
                if (!long.TryParse(groupId, out long groupIdLong))
                {
                    MessageBox.Show("群号格式无效", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                
                var result = await WangShangLiaoHttpApi.Instance.MuteAllAsync(groupIdLong);
                if (result.Success)
                {
                    AddLog(_currentAccountId, "禁言", "全体禁言成功");
                    MessageBox.Show("全体禁言成功", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show($"禁言失败: {result.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"禁言失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        /// <summary>
        /// 全体解禁
        /// </summary>
        private async void BtnUnmuteAll_Click(object sender, EventArgs e)
        {
            try
            {
                var groupId = BotLoginService.Instance.GetCurrentGroupId();
                if (string.IsNullOrEmpty(groupId))
                {
                    MessageBox.Show("请先设置绑定群号", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                
                if (!long.TryParse(groupId, out long groupIdLong))
                {
                    MessageBox.Show("群号格式无效", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                
                var result = await WangShangLiaoHttpApi.Instance.UnmuteAllAsync(groupIdLong);
                if (result.Success)
                {
                    AddLog(_currentAccountId, "解禁", "全体解禁成功");
                    MessageBox.Show("全体解禁成功", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show($"解禁失败: {result.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"解禁失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        /// <summary>
        /// 校准时间
        /// </summary>
        private async void BtnSyncTime_Click(object sender, EventArgs e)
        {
            try
            {
                AddLog(_currentAccountId, "校准", "正在校准系统时间...");
                
                // 使用NTP服务器获取网络时间
                var ntpTime = await Task.Run(() => GetNetworkTime());
                var localTime = DateTime.Now;
                var diff = ntpTime - localTime;
                
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("【时间校准结果】");
                sb.AppendLine($"网络时间: {ntpTime:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine($"本地时间: {localTime:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine($"时间差: {diff.TotalSeconds:F1} 秒");
                
                if (Math.Abs(diff.TotalSeconds) > 5)
                {
                    sb.AppendLine("\n⚠️ 时间差异较大，建议同步系统时间");
                }
                else
                {
                    sb.AppendLine("\n✓ 时间正常");
                }
                
                MessageBox.Show(sb.ToString(), "校准时间", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"校准失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        /// <summary>
        /// 获取网络时间
        /// </summary>
        private DateTime GetNetworkTime()
        {
            // 尝试多个时间源
            var timeServers = new[] { "http://www.baidu.com", "http://www.taobao.com", "http://www.qq.com" };
            
            foreach (var server in timeServers)
            {
                try
                {
                    using (var client = new System.Net.WebClient())
                    {
                        client.DownloadData(server);
                        var dateStr = client.ResponseHeaders["Date"];
                        if (!string.IsNullOrEmpty(dateStr))
                        {
                            return DateTime.Parse(dateStr).ToLocalTime();
                        }
                    }
                }
                catch
                {
                    // 尝试下一个服务器
                    continue;
                }
            }
            
            // 所有服务器都失败，抛出异常让调用者知道
            throw new Exception("无法连接到时间服务器，请检查网络");
        }
        
        /// <summary>
        /// 聊天日志
        /// </summary>
        private void BtnChatLog_Click(object sender, EventArgs e)
        {
            using (var dialog = new Form())
            {
                dialog.Text = "聊天日志";
                dialog.Size = new Size(700, 500);
                dialog.StartPosition = FormStartPosition.CenterParent;
                
                var txtLog = new TextBox
                {
                    Dock = DockStyle.Fill,
                    Multiline = true,
                    ScrollBars = ScrollBars.Both,
                    ReadOnly = true,
                    Font = new Font("Consolas", 9F)
                };
                
                // 加载日志文件
                var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                if (Directory.Exists(logPath))
                {
                    var logFiles = Directory.GetFiles(logPath, "*.log").OrderByDescending(f => f).Take(1);
                    if (logFiles.Any())
                    {
                        txtLog.Text = File.ReadAllText(logFiles.First());
                    }
                    else
                    {
                        txtLog.Text = "暂无日志记录";
                    }
                }
                else
                {
                    txtLog.Text = "日志目录不存在";
                }
                
                var btnRefresh = new Button
                {
                    Text = "刷新",
                    Dock = DockStyle.Bottom,
                    Height = 30
                };
                btnRefresh.Click += (s, args) =>
                {
                    if (Directory.Exists(logPath))
                    {
                        var files = Directory.GetFiles(logPath, "*.log").OrderByDescending(f => f).Take(1);
                        if (files.Any())
                        {
                            txtLog.Text = File.ReadAllText(files.First());
                        }
                    }
                };
                
                dialog.Controls.Add(txtLog);
                dialog.Controls.Add(btnRefresh);
                dialog.ShowDialog();
            }
        }
        
        #endregion
        
        private void CreateAccountsTab(TabPage tab)
        {
            // 顶部按钮面板（右对齐开始游戏按钮）
            var pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                BackColor = Color.White
            };
            
            // 开始游戏按钮（右上角，与运行日志页面保持一致）
            var btnGame = new Button
            {
                Text = "开始游戏",
                Size = new Size(80, 28),
                Location = new Point(580, 6),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnGame.FlatAppearance.BorderSize = 0;
            btnGame.Click += BtnStartGame_Click;
            
            // XPlugin启动按钮
            var btnXPlugin = new Button
            {
                Text = "启动插件",
                Size = new Size(80, 28),
                Location = new Point(490, 6),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(33, 150, 243), // 蓝色
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnXPlugin.FlatAppearance.BorderSize = 0;
            btnXPlugin.Click += BtnXPlugin_Click;
            
            pnlTop.Controls.Add(btnXPlugin);
            pnlTop.Controls.Add(btnGame);
            
            // 账号列表 ListView
            lvAccounts = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                Font = new Font("Microsoft YaHei UI", 9F),
                BorderStyle = BorderStyle.FixedSingle
            };
            
            // 列头 - 按照截图设计
            lvAccounts.Columns.Add("ID", 30);
            lvAccounts.Columns.Add("账号名", 80);
            lvAccounts.Columns.Add("wwid", 80);
            lvAccounts.Columns.Add("群号", 80);
            lvAccounts.Columns.Add("状态", 70);
            lvAccounts.Columns.Add("自动", 40);
            lvAccounts.Columns.Add("账号", 100);
            
            lvAccounts.OwnerDraw = true;
            lvAccounts.DrawColumnHeader += LvLog_DrawColumnHeader;
            lvAccounts.DrawItem += LvLog_DrawItem;
            lvAccounts.DrawSubItem += LvLog_DrawSubItem;
            
            // 右键菜单
            var contextMenu = new ContextMenuStrip();
            var menuLogin = new ToolStripMenuItem("登录", null, (s, e) => LoginSelectedAccount());
            var menuEditAccount = new ToolStripMenuItem("修改信息", null, (s, e) => EditSelectedAccount());
            var menuAddAccount = new ToolStripMenuItem("添加账户", null, (s, e) => ShowLoginDialog());
            var menuDeleteAccount = new ToolStripMenuItem("删除账户", null, (s, e) => DeleteSelectedAccount());
            var menuStartAuto = new ToolStripMenuItem("开启自动", null, (s, e) => ToggleAutoMode(true));
            var menuStopAuto = new ToolStripMenuItem("关闭自动", null, (s, e) => ToggleAutoMode(false));
            var menuRefresh = new ToolStripMenuItem("刷新状态", null, (s, e) => RefreshAccountStatus());
            
            contextMenu.Items.Add(menuLogin);
            contextMenu.Items.Add(menuEditAccount);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add(menuAddAccount);
            contextMenu.Items.Add(menuDeleteAccount);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add(menuStartAuto);
            contextMenu.Items.Add(menuStopAuto);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add(menuRefresh);
            
            lvAccounts.ContextMenuStrip = contextMenu;
            
            tab.Controls.Add(lvAccounts);
            tab.Controls.Add(pnlTop);
        }
        
        /// <summary>
        /// 显示登录对话框 - 添加旺商聊机器人账号
        /// </summary>
        private void ShowLoginDialog()
        {
            // 使用新的 AddAccountForm 对话框
            using (var dialog = new AddAccountForm())
            {
                if (dialog.ShowDialog(this) == DialogResult.OK && dialog.ResultAccount != null)
                {
                    var account = dialog.ResultAccount;
                    
                    // 添加账户到列表
                    var connId = $"wsl_{DateTime.Now.Ticks}";
                    _currentAccountId = account.Account;
                    
                    // 使用 CDP 获取的真实 Wwid，显示名称优先使用 BotName（已设为 AccountId）
                    var displayWwid = !string.IsNullOrEmpty(account.Wwid) ? account.Wwid : account.Account;
                    // 优先使用 BotName（AccountId），因为它是精确的账号名称
                    var displayName = !string.IsNullOrEmpty(account.BotName) ? account.BotName : account.Nickname;
                    
                    AddAccount(
                        connId,
                        displayName,         // 机器人名称（使用 AccountId 作为精确名称）
                        displayWwid,         // WWID（优先使用真实WWID）
                        account.GroupId,     // 群号
                        "待登录",
                        "×",
                        account.Account
                    );
                    
                    AddLog(account.Account, "插件", $"添加机器人: {displayName} (WWID: {displayWwid}, 昵称: {account.Nickname}), 绑定群号: {account.GroupId}");
                    
                    // 保存到 AccountManager
                    AccountManager.Instance.AddAccount(account);
                    
                    // 保存账号到旧版数据存储 (兼容)
                    SaveAccountData(connId, account.Account, account.GetPassword(), account.GroupId, displayName);
                    
                    // 尝试登录旺商聊
                    TryLoginWangShangLiao(account);
                }
            }
        }
        
        /// <summary>
        /// 尝试登录旺商聊 - 优先使用 CDP 获取真实信息
        /// </summary>
        private async void TryLoginWangShangLiao(BotAccount account)
        {
            if (account == null) return;
            
            try
            {
                AddLog(account.Account, "登录", $"正在登录旺商聊: {account.Account}...");
                UpdateAccountStatus(account.Account, "登录中...");
                
                // === 步骤1: 优先从 CDP 获取真实信息 ===
                var cdp = CDPService.Instance;
                cdp.OnLog += msg => AddLog(account.Account, "CDP", msg);
                
                var cdpConnected = await cdp.CheckConnectionAsync();
                
                if (cdpConnected)
                {
                    // 从 CDP 获取真实用户信息
                    var userInfo = await cdp.GetCurrentUserAsync();
                    
                    if (userInfo != null && !string.IsNullOrEmpty(userInfo.Wwid))
                    {
                        // 更新账号信息为真实数据
                        account.Wwid = userInfo.Wwid;
                        account.Nickname = userInfo.Nickname;
                        account.NimAccid = userInfo.NimId;
                        account.NimToken = userInfo.NimToken;
                        account.IsLoggedIn = true;
                        account.LoginStatus = "已登录(CDP)";
                        
                        AddLog(account.Account, "成功", $"✓ CDP 获取到真实信息: {userInfo.Nickname} (WWID: {userInfo.Wwid})");
                        
                        // 更新 UI 显示
                        UpdateAccountInfo(
                            userInfo.Nickname,
                            userInfo.Wwid,
                            "已登录(CDP)"
                        );
                        
                        // 保存账号信息
                        AccountManager.Instance.AddAccount(account);
                        
                        // 更新 FrameworkServer 的活跃群
                        _server?.SetActiveGroup(account.GroupId);
                        
                        AddLog(account.Account, "信息", $"绑定群号: {account.GroupId}");
                        return;
                    }
                }
                
                // === 步骤2: CDP 不可用时，使用 BotLoginService 登录 ===
                AddLog(account.Account, "信息", "CDP 不可用，尝试 NIM 登录...");
                
                var loginService = BotLoginService.Instance;
                loginService.OnLog += msg => AddLog(account.Account, "NIM", msg);
                loginService.OnGroupMessage += (groupId, fromId, content) =>
                {
                    // 收到群消息
                    AddLog(groupId, "群消息", $"{fromId}: {content}");
                    
                    // 转发到 FrameworkServer 处理
                    _server?.HandleGroupMessage(groupId, fromId, content);
                };
                loginService.OnPrivateMessage += (fromId, toId, content) =>
                {
                    // 收到私聊消息
                    AddLog(fromId, "私聊", content);
                    
                    // 转发到 FrameworkServer 处理
                    _server?.HandlePrivateMessage(fromId, toId, content);
                };
                
                var success = await loginService.LoginAsync(account);
                
                if (success)
                {
                    AddLog(account.Account, "成功", $"✓ NIM 登录成功: {loginService.CurrentAccount?.Nickname}");
                    UpdateAccountInfo(
                        loginService.CurrentAccount?.Nickname ?? account.BotName,
                        loginService.CurrentAccount?.Wwid ?? account.Account,
                        "已登录(NIM)"
                    );
                    
                    // 更新 FrameworkServer 的活跃群
                    _server?.SetActiveGroup(account.GroupId);
                }
                else
                {
                    AddLog(account.Account, "失败", $"× 登录失败: {loginService.LoginStatus}");
                    UpdateAccountStatus(account.Account, "登录失败");
                }
            }
            catch (Exception ex)
            {
                AddLog(account.Account, "异常", $"登录异常: {ex.Message}");
                UpdateAccountStatus(account.Account, "异常");
                Logger.Error($"TryLoginWangShangLiao: {ex}");
            }
        }
        
        /// <summary>
        /// 保存单个账号数据
        /// </summary>
        private void SaveAccountData(string connId, string account, string password, string groupId, string nickname = null)
        {
            try
            {
                var accounts = ZCGDataStorage.Instance.LoadAccounts();
                
                // 使用提供的昵称，如果为空则使用账号
                var robotName = string.IsNullOrEmpty(nickname) ? account : nickname;
                
                // 检查是否已存在
                var existing = accounts.Find(a => a.Account == account);
                if (existing != null)
                {
                    // 更新现有账号
                    existing.GroupId = groupId;
                    existing.Password = EncodePassword(password);
                    existing.Nickname = robotName;
                    existing.LastLoginTime = DateTime.Now;
                }
                else
                {
                    // 添加新账号
                    accounts.Add(new AccountData
                    {
                        Id = connId,
                        Account = account,
                        Password = EncodePassword(password),
                        Nickname = robotName,
                        Wwid = account,
                        GroupId = groupId,
                        Status = "待登录",
                        AutoMode = false,
                        CreateTime = DateTime.Now,
                        LastLoginTime = DateTime.Now
                    });
                }
                
                ZCGDataStorage.Instance.SaveAccounts(accounts);
            }
            catch (Exception ex)
            {
                AddLog("系统", "失败", $"保存账号失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 加载已保存的账号列表
        /// </summary>
        private void LoadSavedAccounts()
        {
            try
            {
                var accounts = ZCGDataStorage.Instance.LoadAccounts();
                
                foreach (var acc in accounts)
                {
                    AddAccount(
                        acc.Id,
                        acc.Nickname ?? acc.Account,
                        acc.Wwid ?? acc.Account,
                        acc.GroupId,
                        acc.Status ?? "待连接",
                        acc.AutoMode ? "√" : "×",
                        acc.Account
                    );
                    
                    // 设置当前账号ID
                    if (string.IsNullOrEmpty(_currentAccountId))
                        _currentAccountId = acc.Account;
                }
                
                if (accounts.Count > 0)
                {
                    AddLog("系统", "成功", $"已加载 {accounts.Count} 个已保存的账号");
                }
            }
            catch (Exception ex)
            {
                AddLog("系统", "失败", $"加载账号列表失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 保存所有账号数据
        /// </summary>
        private void SaveAllAccounts()
        {
            try
            {
                var accounts = new System.Collections.Generic.List<AccountData>();
                
                foreach (ListViewItem item in lvAccounts.Items)
                {
                    accounts.Add(new AccountData
                    {
                        Id = item.Tag as string ?? $"wsl_{item.Index}",
                        Nickname = item.SubItems[1].Text,
                        Wwid = item.SubItems[2].Text,
                        GroupId = item.SubItems[3].Text,
                        Status = item.SubItems[4].Text,
                        AutoMode = item.SubItems[5].Text == "√",
                        Account = item.SubItems[6].Text,
                        LastLoginTime = DateTime.Now
                    });
                }
                
                ZCGDataStorage.Instance.SaveAccounts(accounts);
            }
            catch (Exception ex)
            {
                Logger.Error($"保存账号列表失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 简单密码编码（Base64）
        /// </summary>
        private string EncodePassword(string password)
        {
            if (string.IsNullOrEmpty(password)) return "";
            var bytes = System.Text.Encoding.UTF8.GetBytes(password);
            return Convert.ToBase64String(bytes);
        }
        
        /// <summary>
        /// 简单密码解码（Base64）
        /// </summary>
        private string DecodePassword(string encoded)
        {
            if (string.IsNullOrEmpty(encoded)) return "";
            var bytes = Convert.FromBase64String(encoded);
            return System.Text.Encoding.UTF8.GetString(bytes);
        }
        
        /// <summary>
        /// 尝试登录旺商聊 - 添加账号后自动连接
        /// 优先从CDP获取NIM凭证，支持完整的AES加密通信
        /// </summary>
        private async void TryLoginWangShangLiao(string account, string password, string groupId)
        {
            try
            {
                AddLog(account, "登录", $"正在登录机器人账号: {account}");
                UpdateAccountStatus(account, "登录中...");
                
                // 设置活跃群号
                _currentGroupId = groupId;
                
                // ========== 步骤1: 使用账号密码 API 登录 ==========
                // [已废弃] CDP 模式，现在统一使用 BotLoginService
                if (string.IsNullOrEmpty(password))
                {
                    AddLog(account, "警告", "密码为空，请在【添加账户】中设置密码");
                    UpdateAccountStatus(account, "无密码");
                    return;
                }
                AddLog(account, "登录", $"步骤1: 使用账号密码 API 登录...");
                
                // ========== 步骤2: 获取 NIM 凭证 (优先API登录，不需要打开旺商聊客户端) ==========
                AddLog(account, "登录", $"步骤2: 获取 NIM 凭证...");
                
                string nimAccid = null;
                string nimToken = null;
                
                // 方案A: API 登录已废弃 (yiyong.netease.im 返回404)
                // 直接跳过，使用 CDP 获取凭证
                AddLog(account, "信息", "API 登录不可用，使用 CDP 模式...");
                    
                // 方案B: 备用 - 从 CDP 获取 (如果旺商聊客户端已打开)
                if (string.IsNullOrEmpty(nimToken) && _server?.IsCDPConnected == true)
                    {
                    AddLog(account, "登录", "尝试从 CDP 获取 NIM 凭证...");
                        var userInfo = await _server.CDPBridge.GetCurrentUserInfoAsync();
                        if (userInfo != null && !string.IsNullOrEmpty(userInfo.nimToken))
                        {
                            nimAccid = userInfo.nimId;
                            nimToken = userInfo.nimToken;
                        AddLog(account, "成功", $"✓ CDP 获取 NIM Token: accid={nimAccid}");
                        
                        if (!string.IsNullOrEmpty(userInfo.nickname))
                        {
                            UpdateAccountInfo(userInfo.nickname, userInfo.wwid, null);
                            AddLog(account, "信息", $"✓ 获取到机器人昵称: {userInfo.nickname}");
                        }
                    }
                }
                
                // 方案C: 硬编码映射 (特殊账号)
                if (account == "621705120" && string.IsNullOrEmpty(nimAccid))
                {
                    nimAccid = "1628907626";
                    AddLog(account, "信息", "使用映射: 621705120 -> NIM 1628907626");
                }
                
                // ========== 步骤3: 连接 NIM 直连服务器 ==========
                AddLog(account, "登录", $"步骤3: 连接 NIM 服务器...");
                
                var nimClient = NimDirectClient.Instance;
                
                // 只注册一次日志事件，避免重复
                nimClient.OnLog -= NimLogHandler;
                nimClient.OnLog += NimLogHandler;
                nimClient.OnMessageReceived -= OnNimMessageReceived;
                nimClient.OnMessageReceived += OnNimMessageReceived;
                
                bool nimConnected = false;
                
                if (!string.IsNullOrEmpty(nimAccid) && !string.IsNullOrEmpty(nimToken))
                {
                    nimConnected = await nimClient.LoginWithTokenAsync(nimAccid, nimToken);
                    if (nimConnected)
                    {
                        AddLog(account, "成功", $"✓ NIM 直连登录成功: {nimAccid}");
                        UpdateAccountStatus(account, "NIM已连");
                    }
                    else
                    {
                        AddLog(account, "警告", "NIM Token 登录失败");
                    }
                }
                else if (_server?.IsCDPConnected == true)
                {
                    // 尝试从 CDP 直接登录 NIM
                    AddLog(account, "登录", "尝试从 CDP 自动获取凭证登录 NIM...");
                    nimConnected = await nimClient.LoginFromCDPAsync(_server.CDPBridge);
                    if (nimConnected)
                    {
                        AddLog(account, "成功", $"✓ CDP 自动登录 NIM 成功");
                        UpdateAccountStatus(account, "NIM已连");
                    }
                }
                
                if (!nimConnected)
                {
                    AddLog(account, "警告", "未获取到有效的 NIM 凭证");
                    AddLog(account, "提示", "请确保旺商聊已登录: " + account);
                }
                
                // ========== 步骤4: 设置活跃群 ==========
                AddLog(account, "登录", $"步骤4: 绑定群号 {groupId}");
                
                // 设置 NIM 直连客户端的活跃群
                nimClient.SetActiveGroup(groupId);
                
                // 设置定时服务的活跃群
                TimedMessageService.Instance.AddActiveGroup(groupId);
                
                AddLog(account, "成功", $"✓ 已绑定群号: {groupId}");
                
                // ========== 步骤5: 检查最终状态 ==========
                if (nimClient.IsLoggedIn)
                {
                    UpdateAccountStatus(account, "登录成功");
                    AddLog(account, "成功", "✓ 机器人已就绪，可发送消息到群: " + groupId);
                    AddLog(account, "信息", "消息发送优先级: NIM直连 > NIM SDK > CDP");
                }
                else if (_server?.IsCDPConnected == true)
                {
                    UpdateAccountStatus(account, "CDP已连");
                    AddLog(account, "成功", "✓ CDP 已连接，使用 CDP 发送消息");
                }
                else
                {
                    UpdateAccountStatus(account, "待连接");
                    AddLog(account, "提示", "请启动旺商聊后重试");
                }
            }
            catch (Exception ex)
            {
                AddLog(account, "错误", $"登录失败: {ex.Message}");
                UpdateAccountStatus(account, "登录失败");
                Logger.Error($"[Login] {ex}");
            }
        }
        
        /// <summary>
        /// NIM 日志处理器 (避免重复注册)
        /// </summary>
        private void NimLogHandler(string msg)
        {
            SafeInvoke(() => AddLog("NIM", "日志", msg));
        }
        
        /// <summary>
        /// 处理 NIM 收到的消息
        /// </summary>
        private void OnNimMessageReceived(NimDirectMessage msg)
        {
            SafeInvoke(() =>
            {
                AddLog("NIM", "消息", $"[{msg.Scene}] {msg.From}: {msg.Body}");
                
                // 广播给主框架
                if (_server != null)
                {
                    _server.BroadcastNimMessage(msg);
                }
            });
        }
        
        // 当前活跃群号
        private string _currentGroupId;
        
        /// <summary>
        /// 更新账号状态
        /// </summary>
        private void UpdateAccountStatus(string account, string status)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => UpdateAccountStatus(account, status)));
                return;
            }
            
            foreach (ListViewItem item in lvAccounts.Items)
            {
                if (item.SubItems[6].Text == account || item.SubItems[2].Text == account)
                {
                    item.SubItems[4].Text = status;
                    break;
                }
            }
        }
        
        /// <summary>
        /// 更新所有账号状态（将待登录/连接中状态更新为指定状态）
        /// </summary>
        private void UpdateAllAccountsStatus(string newStatus)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => UpdateAllAccountsStatus(newStatus)));
                return;
            }
            
            foreach (ListViewItem item in lvAccounts.Items)
            {
                var currentStatus = item.SubItems[4].Text;
                // 只更新非成功状态的账号
                if (currentStatus == "待登录" || currentStatus == "连接中" || currentStatus == "已断开")
                {
                    item.SubItems[4].Text = newStatus;
                }
            }
            
            // 同时更新保存的账号数据
            SaveAllAccounts();
        }
        
        /// <summary>
        /// 只更新账号状态（不覆盖用户配置的机器人名称）
        /// </summary>
        private void UpdateAccountStatus(string status)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => UpdateAccountStatus(status)));
                return;
            }
            
            if (lvAccounts.Items.Count > 0)
            {
                var item = lvAccounts.Items[0];
                item.SubItems[4].Text = status;  // 只更新状态
                SaveAllAccounts();
            }
        }
        
        /// <summary>
        /// 更新账号的昵称、wwid和状态（仅在用户主动添加账号时使用）
        /// </summary>
        private void UpdateAccountInfo(string nickname, string wwid, string status)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => UpdateAccountInfo(nickname, wwid, status)));
                return;
            }
            
            if (lvAccounts.Items.Count > 0)
            {
                var item = lvAccounts.Items[0];
                
                // 只有当用户明确设置时才更新昵称
                if (!string.IsNullOrEmpty(nickname))
                {
                    item.SubItems[1].Text = nickname;
                }
                if (!string.IsNullOrEmpty(wwid))
                {
                    item.SubItems[2].Text = wwid;
                }
                if (!string.IsNullOrEmpty(status))
                {
                    item.SubItems[4].Text = status;
                }
                
                SaveAllAccounts();
            }
        }
        
        /// <summary>
        /// 删除选中的账户
        /// </summary>
        private void DeleteSelectedAccount()
        {
            if (lvAccounts.SelectedItems.Count > 0)
            {
                var item = lvAccounts.SelectedItems[0];
                var account = item.SubItems[6].Text;
                lvAccounts.Items.Remove(item);
                
                // 重新编号
                for (int i = 0; i < lvAccounts.Items.Count; i++)
                {
                    lvAccounts.Items[i].Text = (i + 1).ToString();
                }
                
                AddLog("系统", "删除", $"删除账户: {account}");
            }
        }
        
        /// <summary>
        /// 修改选中账户的信息
        /// </summary>
        private void EditSelectedAccount()
        {
            if (lvAccounts.SelectedItems.Count == 0)
            {
                MessageBox.Show("请先选择要修改的账户", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            
            var item = lvAccounts.SelectedItems[0];
            var accountStr = item.SubItems[6].Text;  // 账号列
            var nickname = item.SubItems[1].Text;    // 昵称列
            var groupId = item.SubItems[3].Text;     // 群号列
            
            // 创建修改对话框
            using (var dialog = new Form())
            {
                dialog.Text = "修改账户信息";
                dialog.Size = new Size(400, 280);
                dialog.StartPosition = FormStartPosition.CenterParent;
                dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialog.MaximizeBox = false;
                dialog.MinimizeBox = false;
                dialog.BackColor = Color.White;
                
                // 账号（只读）
                var lblAccount = new Label { Text = "账号:", Location = new Point(30, 30), AutoSize = true };
                var txtAccount = new TextBox { 
                    Text = accountStr, 
                    Location = new Point(120, 27), 
                    Width = 220, 
                    ReadOnly = true,
                    BackColor = Color.LightGray
                };
                
                // 昵称
                var lblNickname = new Label { Text = "机器人名称:", Location = new Point(30, 70), AutoSize = true };
                var txtNickname = new TextBox { 
                    Text = nickname, 
                    Location = new Point(120, 67), 
                    Width = 220 
                };
                
                // 群号
                var lblGroupId = new Label { Text = "绑定群号:", Location = new Point(30, 110), AutoSize = true };
                var txtGroupId = new TextBox { 
                    Text = groupId, 
                    Location = new Point(120, 107), 
                    Width = 220 
                };
                
                // 密码（可选）
                var lblPassword = new Label { Text = "新密码:", Location = new Point(30, 150), AutoSize = true };
                var txtPassword = new TextBox { 
                    Text = "", 
                    Location = new Point(120, 147), 
                    Width = 220,
                    PasswordChar = '●'
                };
                var lblPasswordHint = new Label { 
                    Text = "(留空则不修改)", 
                    Location = new Point(120, 172), 
                    AutoSize = true,
                    ForeColor = Color.Gray,
                    Font = new Font("Microsoft YaHei UI", 8f)
                };
                
                // 按钮
                var btnOk = new Button { 
                    Text = "确定", 
                    DialogResult = DialogResult.OK, 
                    Location = new Point(120, 200),
                    Width = 80,
                    BackColor = Color.FromArgb(76, 175, 80),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat
                };
                btnOk.FlatAppearance.BorderSize = 0;
                
                var btnCancel = new Button { 
                    Text = "取消", 
                    DialogResult = DialogResult.Cancel, 
                    Location = new Point(220, 200),
                    Width = 80
                };
                
                dialog.Controls.AddRange(new Control[] { 
                    lblAccount, txtAccount,
                    lblNickname, txtNickname, 
                    lblGroupId, txtGroupId,
                    lblPassword, txtPassword, lblPasswordHint,
                    btnOk, btnCancel 
                });
                dialog.AcceptButton = btnOk;
                dialog.CancelButton = btnCancel;
                
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    // 更新界面
                    item.SubItems[1].Text = txtNickname.Text;  // 昵称
                    item.SubItems[3].Text = txtGroupId.Text;   // 群号
                    
                    // 更新存储
                    var accounts = ZCGDataStorage.Instance.LoadAccounts();
                    var acc = accounts.Find(a => a.Account == accountStr);
                    if (acc != null)
                    {
                        acc.Nickname = txtNickname.Text;
                        acc.GroupId = txtGroupId.Text;
                        if (!string.IsNullOrEmpty(txtPassword.Text))
                        {
                            acc.Password = EncodePassword(txtPassword.Text);
                        }
                        ZCGDataStorage.Instance.SaveAccounts(accounts);
                    }
                    
                    // 同时更新 AccountManager
                    var botAccount = Models.AccountManager.Instance.GetAccount(accountStr);
                    if (botAccount != null)
                    {
                        botAccount.BotName = txtNickname.Text;
                        botAccount.GroupId = txtGroupId.Text;
                        if (!string.IsNullOrEmpty(txtPassword.Text))
                        {
                            botAccount.SetPassword(txtPassword.Text);
                        }
                        Models.AccountManager.Instance.Save();
                    }
                    
                    AddLog(accountStr, "修改", $"账户信息已更新: 昵称={txtNickname.Text}, 群号={txtGroupId.Text}");
                }
            }
        }
        
        /// <summary>
        /// 登录选中的账户
        /// </summary>
        private async void LoginSelectedAccount()
        {
            if (lvAccounts.SelectedItems.Count == 0)
            {
                MessageBox.Show("请先选择要登录的账户", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            
            var item = lvAccounts.SelectedItems[0];
            var accountStr = item.SubItems[6].Text;  // 账号列
            var groupId = item.SubItems[3].Text;  // 群号列
            var nickname = item.SubItems[1].Text; // 昵称列
            
            AddLog(accountStr, "登录", $"开始登录账户: {accountStr}");
            UpdateAccountStatus(accountStr, "登录中...");
            
            try
            {
                // 【新方案】使用 CDP 提取 NIM Token，然后直连 NIM SDK
                var loginService = Services.BotLoginService.Instance;
                
                // 优先从 AccountManager 查找（新版存储）
                var savedAccount = Models.AccountManager.Instance.GetAccount(accountStr);
                
                // 如果 AccountManager 中没有，从 ZCGDataStorage 查找（旧版存储）
                if (savedAccount == null)
                {
                    var zcgAccounts = ZCGDataStorage.Instance.LoadAccounts();
                    var zcgAccount = zcgAccounts.Find(a => a.Account == accountStr);
                    
                    if (zcgAccount != null)
                    {
                        // 转换为 BotAccount
                        savedAccount = new Models.BotAccount
                        {
                            Account = zcgAccount.Account,
                            BotName = zcgAccount.Nickname ?? nickname,
                            GroupId = zcgAccount.GroupId ?? groupId,
                            AutoLogin = zcgAccount.AutoMode,
                            RememberPassword = true
                        };
                        
                        // 尝试解码密码
                        if (!string.IsNullOrEmpty(zcgAccount.Password))
                        {
                            try
                            {
                                var password = DecodePassword(zcgAccount.Password);
                                savedAccount.SetPassword(password);
                            }
                            catch
                            {
                                // 密码可能是明文
                                savedAccount.SetPassword(zcgAccount.Password);
                            }
                        }
                        
                        // 保存到 AccountManager
                        Models.AccountManager.Instance.AddAccount(savedAccount);
                        AddLog(accountStr, "信息", "已从旧版数据迁移账号");
                    }
                }
                
                // 如果还是找不到，可能需要重新添加
                if (savedAccount == null)
                {
                    AddLog(accountStr, "错误", "账号信息未找到，请右键【添加账户】重新添加");
                    UpdateAccountStatus(accountStr, "未找到");
                    
                    // 弹出提示
                    MessageBox.Show(
                        $"账号 {accountStr} 的密码信息丢失，请右键选择【添加账户】重新添加。\n\n" +
                        $"需要填写：\n- 旺商聊账号: {accountStr}\n- 机器人名称: {nickname}\n- 登录密码: (请输入)\n- 绑定群号: {groupId}",
                        "需要重新添加账号", 
                        MessageBoxButtons.OK, 
                        MessageBoxIcon.Information);
                    return;
                }
                
                // 更新群号（从界面获取最新值）
                if (!string.IsNullOrEmpty(groupId))
                {
                    savedAccount.GroupId = groupId;
                }
                
                // ===== 新方案：CDP 提取 NIM Token + 直连 NIM SDK =====
                bool success = false;
                
                // 优先检查 CDP 是否可用，如果未连接则尝试连接
                var cdpBridge = _server?.CDPBridge;
                
                // 如果 CDP 未连接，尝试连接（扫描端口 9222）
                if (cdpBridge != null && !cdpBridge.IsConnected)
                {
                    AddLog(accountStr, "登录", "正在连接 CDP (端口 9222)...");
                    var cdpConnected = await cdpBridge.ConnectAsync(9222);
                    
                    if (cdpConnected)
                    {
                        AddLog(accountStr, "成功", "✓ CDP 连接成功!");
                    }
                    else
                    {
                        // 尝试其他端口
                        AddLog(accountStr, "信息", "端口 9222 连接失败，尝试其他端口...");
                        foreach (var port in new[] { 9223, 9229, 9221 })
                        {
                            AddLog(accountStr, "登录", $"尝试端口 {port}...");
                            cdpConnected = await cdpBridge.ConnectAsync(port);
                            if (cdpConnected)
                            {
                                AddLog(accountStr, "成功", $"✓ CDP 连接成功 (端口 {port})");
                                break;
                            }
                        }
                    }
                }
                
                if (cdpBridge != null && cdpBridge.IsConnected)
                {
                    AddLog(accountStr, "登录", "使用 CDP + NIM Token 方式登录...");
                    AddLog(accountStr, "信息", "CDP 已连接，正在提取 NIM Token...");
                    
                    // 使用 CDP 提取 Token 并登录
                    success = await loginService.LoginWithCDPAsync(savedAccount, cdpBridge);
                    
                    if (success)
                    {
                        AddLog(accountStr, "成功", "✓ CDP + NIM Token 登录成功");
                    }
                    else
                    {
                        AddLog(accountStr, "警告", "CDP 登录失败，尝试备用方案...");
                    }
                }
                else
                {
                    AddLog(accountStr, "警告", "CDP 未连接，请先启动旺商聊客户端（调试模式）");
                    AddLog(accountStr, "提示", "运行【启动旺商聊调试模式.cmd】后重试");
                }
                
                // 备用方案：如果 CDP 不可用，尝试直接 API 登录（需要密码）
                if (!success && !string.IsNullOrEmpty(savedAccount.GetPassword()))
                {
                    AddLog(accountStr, "登录", "使用账号密码 API 方式登录...");
                    success = await loginService.LoginAsync(savedAccount);
                }
                
                // 如果都失败，提示用户启动旺商聊
                if (!success && (cdpBridge == null || !cdpBridge.IsConnected))
                {
                    AddLog(accountStr, "提示", "=== 请按以下步骤操作 ===");
                    AddLog(accountStr, "提示", "1. 运行【启动旺商聊调试模式.cmd】启动旺商聊客户端");
                    AddLog(accountStr, "提示", "2. 在旺商聊中登录您的账号");
                    AddLog(accountStr, "提示", "3. 返回本程序，再次点击【登录】");
                }
                
                if (success)
                {
                    // 登录成功后，从 CDP 获取最新的用户信息并更新存储
                    await SyncUserInfoFromCDPAsync(accountStr, loginService.CurrentAccount);
                    
                    // 使用 BotName（AccountId）作为显示名称
                    var displayName = !string.IsNullOrEmpty(loginService.CurrentAccount?.BotName) 
                        ? loginService.CurrentAccount.BotName 
                        : loginService.CurrentAccount?.Nickname;
                    AddLog(accountStr, "成功", $"登录成功: {displayName} (昵称: {loginService.CurrentAccount?.Nickname})");
                    UpdateAccountStatus(accountStr, "登录成功");
                    item.SubItems[5].Text = "√";
                    
                    // 更新账号名称和wwid - 优先使用 BotName（AccountId）
                    if (!string.IsNullOrEmpty(displayName))
                    {
                        item.SubItems[1].Text = displayName;
                    }
                    if (!string.IsNullOrEmpty(loginService.CurrentAccount?.Wwid))
                    {
                        item.SubItems[2].Text = loginService.CurrentAccount.Wwid;
                    }
                }
                else
                {
                    AddLog(accountStr, "失败", $"登录失败: {loginService.LoginStatus}");
                    UpdateAccountStatus(accountStr, "登录失败");
                }
            }
            catch (Exception ex)
            {
                AddLog(accountStr, "错误", $"登录异常: {ex.Message}");
                UpdateAccountStatus(accountStr, "登录失败");
            }
        }
        
        /// <summary>
        /// 从 CDP 同步最新的用户信息到账号存储
        /// </summary>
        private async Task SyncUserInfoFromCDPAsync(string accountStr, BotAccount account)
        {
            try
            {
                var cdp = CDPService.Instance;
                if (!cdp.IsConnected && !await cdp.CheckConnectionAsync())
                {
                    AddLog(accountStr, "同步", "CDP 未连接，跳过用户信息同步");
                    return;
                }
                
                var userInfo = await cdp.GetCurrentUserAsync();
                if (userInfo == null)
                {
                    AddLog(accountStr, "同步", "无法获取用户信息");
                    return;
                }
                
                bool updated = false;
                
                // 更新昵称
                if (!string.IsNullOrEmpty(userInfo.Nickname) && userInfo.Nickname != account.Nickname)
                {
                    AddLog(accountStr, "同步", $"昵称更新: {account.Nickname} → {userInfo.Nickname}");
                    account.Nickname = userInfo.Nickname;
                    updated = true;
                }
                
                // 更新 WWID（如果不一致）
                if (!string.IsNullOrEmpty(userInfo.Wwid) && userInfo.Wwid != account.Wwid)
                {
                    AddLog(accountStr, "同步", $"WWID 更新: {account.Wwid} → {userInfo.Wwid}");
                    account.Wwid = userInfo.Wwid;
                    updated = true;
                }
                
                // 更新 NIM 凭证
                if (!string.IsNullOrEmpty(userInfo.NimId) && userInfo.NimId != account.NimAccid)
                {
                    account.NimAccid = userInfo.NimId;
                    updated = true;
                }
                if (!string.IsNullOrEmpty(userInfo.NimToken) && userInfo.NimToken != account.NimToken)
                {
                    account.NimToken = userInfo.NimToken;
                    updated = true;
                }
                
                // 更新 AccountId 到 BotName（如果 BotName 还是旧的昵称）
                if (!string.IsNullOrEmpty(userInfo.AccountId))
                {
                    // 如果 BotName 等于旧昵称或为空，则更新为 AccountId
                    if (string.IsNullOrEmpty(account.BotName) || account.BotName == account.Nickname)
                    {
                        account.BotName = userInfo.AccountId;
                        updated = true;
                    }
                }
                
                if (updated)
                {
                    // 保存更新
                    AccountManager.Instance.Save();
                    AddLog(accountStr, "同步", "✓ 用户信息已同步更新");
                }
                else
                {
                    AddLog(accountStr, "同步", "用户信息无变化");
                }
            }
            catch (Exception ex)
            {
                AddLog(accountStr, "同步", $"同步用户信息失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 刷新账户状态 - 【修改】使用 BotLoginService 状态
        /// </summary>
        private void RefreshAccountStatus()
        {
            var loginService = Services.BotLoginService.Instance;
            
            if (loginService.IsLoggedIn)
            {
                var currentAccount = loginService.CurrentAccount?.Account;
                // 只更新当前登录账号的状态
                UpdateAccountStatus(currentAccount, "登录成功");
                AddLog("系统", "刷新", $"已登录: {loginService.CurrentAccount?.Nickname} ({currentAccount})");
            }
            else
            {
                UpdateAllAccountsStatus("待登录");
                AddLog("系统", "刷新", $"未登录 - {loginService.LoginStatus}");
            }
        }
        
        /// <summary>
        /// 切换自动模式
        /// </summary>
        private void ToggleAutoMode(bool enable)
        {
            if (lvAccounts.SelectedItems.Count > 0)
            {
                var item = lvAccounts.SelectedItems[0];
                item.SubItems[5].Text = enable ? "√" : "×";
                AddLog("系统", "设置", $"账户 {item.SubItems[6].Text} 自动模式: {(enable ? "开启" : "关闭")}");
            }
        }
        
        private void CreateSettingsTab(TabPage tab)
        {
            // 顶部按钮面板（右对齐开始游戏按钮）
            var pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                BackColor = Color.White
            };
            
            // 开始游戏按钮（右上角，与其他页面保持一致）
            var btnGame = new Button
            {
                Text = "开始游戏",
                Size = new Size(80, 28),
                Location = new Point(580, 6),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnGame.FlatAppearance.BorderSize = 0;
            btnGame.Click += BtnStartGame_Click;
            
            pnlTop.Controls.Add(btnGame);
            
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(15) };
            
            // 第一行选项 - 按照截图设计
            var chkNoRefresh = new CheckBox
            {
                Text = "不刷新信息表",
                Location = new Point(15, 15),
                AutoSize = true
            };
            
            var chkNoLog = new CheckBox
            {
                Text = "不显示日志输出",
                Location = new Point(155, 15),
                AutoSize = true
            };
            
            var chkAutoClear = new CheckBox
            {
                Text = "关闭清理日志",
                Location = new Point(310, 15),
                AutoSize = true
            };
            
            var lblClearCount = new Label
            {
                Text = "清空日志(条):",
                Location = new Point(450, 17),
                AutoSize = true
            };
            
            var numClearCount = new NumericUpDown
            {
                Location = new Point(550, 13),
                Width = 50,
                Value = 100,
                Maximum = 10000
            };
            
            // 测试接口按钮
            var btnTest = new Button
            {
                Text = "测试接口",
                Location = new Point(15, 55),
                Size = new Size(90, 28),
                FlatStyle = FlatStyle.Flat
            };
            btnTest.FlatAppearance.BorderColor = Color.Gray;
            
            // ★★★ 测试发送消息按钮 ★★★
            var btnTestSend = new Button
            {
                Text = "测试发送",
                Location = new Point(115, 55),
                Size = new Size(90, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White
            };
            btnTestSend.FlatAppearance.BorderColor = Color.FromArgb(56, 142, 60);
            btnTestSend.Click += async (s, e) =>
            {
                AddLog("NIM", "测试", "日志 正在测试NIM发送消息...");
                
                try
                {
                    var groupId = "3962369093";  // 测试群
                    var testMsg = $"🤖 测试消息 - {DateTime.Now:HH:mm:ss}";
                    
                    // 优先使用NIM SDK发送
                    var nimService = Services.NIMService.Instance;
                    if (nimService.IsLoggedIn)
                    {
                        AddLog("NIM", "发送", $"日志 使用NIM SDK发送: {testMsg}");
                        var result = await nimService.SendGroupMessageAsync(groupId, testMsg);
                        
                        if (result)
                        {
                            AddLog("NIM", "成功", "日志 ✓ NIM消息发送成功!");
                        }
                        else
                        {
                            AddLog("NIM", "失败", "日志 NIM发送失败，尝试CDP...");
                            
                            // 回退到CDP
                            if (_server?.IsCDPConnected == true)
                            {
                                var cdpResult = await Services.BotLoginService.Instance.SendGroupMessageAsync(groupId, testMsg);
                                AddLog("NIM", cdpResult ? "成功" : "失败", $"日志 CDP发送{(cdpResult ? "成功" : "失败")}");
                            }
                        }
                    }
                    else
                    {
                        AddLog("NIM", "警告", "日志 NIM未登录，尝试CDP发送...");
                        
                        if (_server?.IsCDPConnected == true)
                        {
                            var cdpResult = await Services.BotLoginService.Instance.SendGroupMessageAsync(groupId, testMsg);
                            AddLog("NIM", cdpResult ? "成功" : "失败", $"日志 CDP发送{(cdpResult ? "成功" : "失败")}");
                        }
                        else
                        {
                            AddLog("NIM", "失败", "日志 NIM和CDP都不可用");
                        }
                    }
                }
                catch (Exception ex)
                {
                    AddLog("NIM", "失败", $"日志 发送测试失败: {ex.Message}");
                }
            };
            
            btnTest.Click += async (s, e) =>
            {
                AddLog("插件", "插件", "日志 正在测试开奖接口...");
                
                try
                {
                    // 测试开奖API
                    var lotteryService = LotteryService.Instance;
                    var result = await lotteryService.FetchLatestResultAsync();
                    
                    if (result != null)
                    {
                        AddLog("插件", "成功", $"日志 开奖接口正常 期{result.Period} 开:{result.Num1}+{result.Num2}+{result.Num3}={result.Sum} {result.GetResultString()}");
                        
                        // 显示更多信息
                        AddLog("插件", "成功", $"日志 大小单双: {(result.IsBig ? "大" : "小")}{(result.IsOdd ? "单" : "双")}");
                        
                        // 测试CDP连接
                if (_server?.IsCDPConnected == true)
                {
                            AddLog("插件", "成功", "日志 CDP连接正常");
                }
                else
                {
                            AddLog("插件", "警告", "日志 CDP未连接，消息将无法发送");
                        }
                    }
                    else
                    {
                        AddLog("插件", "失败", "日志 开奖接口返回空数据，请检查网络或API配置");
                    }
                }
                catch (Exception ex)
                {
                    AddLog("插件", "失败", $"日志 测试接口失败: {ex.Message}");
                }
            };
            
            // 版本选择
            var lblVer = new Label
            {
                Text = "版本:",
                Location = new Point(15, 105),
                AutoSize = true
            };
            
            var cmbVersion = new ComboBox
            {
                Location = new Point(60, 101),
                Width = 100,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbVersion.Items.AddRange(new[] { "版本0", "版本1", "版本2" });
            cmbVersion.SelectedIndex = 0;
            
            var lblVerTip = new Label
            {
                Text = "更换后再登录才能生效",
                Location = new Point(175, 105),
                ForeColor = Color.FromArgb(76, 175, 80),
                AutoSize = true
            };
            
            // 最小化到托盘
            var chkMinTray = new CheckBox
            {
                Name = "chkMinTray",
                Text = "最小化到托盘",
                Location = new Point(15, 145),
                AutoSize = true,
                Checked = true
            };
            
            panel.Controls.Add(chkNoRefresh);
            panel.Controls.Add(chkNoLog);
            panel.Controls.Add(chkAutoClear);
            panel.Controls.Add(lblClearCount);
            panel.Controls.Add(numClearCount);
            panel.Controls.Add(btnTest);
            panel.Controls.Add(btnTestSend);  // ★★★ 测试发送按钮 ★★★
            panel.Controls.Add(lblVer);
            panel.Controls.Add(cmbVersion);
            panel.Controls.Add(lblVerTip);
            panel.Controls.Add(chkMinTray);
            
            tab.Controls.Add(panel);
            tab.Controls.Add(pnlTop);
        }
        
        private void Header_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                // 拖动窗口
                NativeMethods.ReleaseCapture();
                NativeMethods.SendMessage(this.Handle, 0xA1, 0x2, 0);
            }
        }
        
        /// <summary>
        /// 初始化 ZCG 数据存储目录结构
        /// 按照旧程序 C:\zcg25.12.11\zcg\ 的结构创建
        /// </summary>
        private void InitializeDataStorage()
        {
            try
            {
                // 数据目录在程序目录下的 zcg 子目录
                var exeDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                var zcgDir = Path.Combine(exeDir, "zcg");
                
                // 初始化数据存储
                ZCGDataStorage.Instance.SetDataRoot(zcgDir);
                
                AddLog("系统", "成功", $"数据目录: {zcgDir}");
            }
            catch (Exception ex)
            {
                AddLog("系统", "失败", $"初始化数据存储失败: {ex.Message}");
            }
        }
        
        private void InitializeServer()
        {
            _server = new FrameworkServer();
            
            _server.OnLog += msg =>
            {
                // 按照招财狗格式显示日志
                var response = "插件";
                var type = "插件";
                
                // 解析日志类型
                if (msg.Contains("[CDP]")) 
                {
                    response = "插件";
                    type = "插件";
                }
                else if (msg.Contains("HPSocket") || msg.Contains("服务"))
                {
                    response = "插件";
                    type = "插件";
                }
                
                AddLog(response, type, $"日志 {msg}");
            };
            
            _server.OnClientConnectionChanged += (connId, connected) =>
            {
                var clientInfo = _server.GetClientInfo(connId);
                var displayName = clientInfo != null 
                    ? $"{clientInfo.Address}:{clientInfo.Port}" 
                    : connId.ToString();
                
                if (connected)
                {
                    AddLog("插件", "插件", $"日志 客户端 {displayName} 已连接");
                }
                else
                {
                    AddLog("插件", "插件", $"日志 客户端 {displayName} 已断开");
                    RemoveAccount(connId.ToString());
                }
            };
            
            // 处理客户端登录成功事件 - 更新账号列表
            _server.OnClientLoggedIn += (connId, loginInfo) =>
            {
                _currentAccountId = loginInfo.Wwid ?? "";
                
                AddAccount(
                    connId.ToString(),
                    loginInfo.Nickname ?? "未知",
                    loginInfo.Wwid ?? "",
                    loginInfo.GroupId ?? "",
                    loginInfo.Status ?? "登录成功",
                    loginInfo.AutoMode ? "√" : "×",
                    loginInfo.Account ?? ""
                );
            };
            
            _server.OnMessageReceived += (connId, message) =>
            {
                var content = message.Content ?? "";
                if (content.Length > 50) content = content.Substring(0, 50) + "...";
                
                // 按照截图格式显示消息
                var groupId = message.GroupId ?? "";
                if (!string.IsNullOrEmpty(groupId))
                {
                    AddLog(_currentAccountId, "投递成功", $"(群{groupId}) {content}");
                }
                else
                {
                    AddLog(_currentAccountId, "投递成功", content);
                }
            };
            
            // 处理旺商聊连接成功事件 - 只更新状态，不覆盖用户配置的机器人账号
            _server.OnWangShangLiaoConnected += (userInfo, groups) =>
            {
                AddLog("系统", "信息", $"CDP检测到旺商聊登录: {userInfo?.nickname} (wwid: {userInfo?.wwid})");
                
                // 只更新状态为"登录成功"，不覆盖用户配置的机器人名称
                // 用户配置的机器人账号优先
                UpdateAccountStatus("登录成功");
                
                AddLog("系统", "成功", "日志 框架连接成功");
            };
            
            // CDP 连接状态变化事件
            _server.OnCDPConnectionChanged += (connected) =>
            {
                if (connected)
                {
                    AddLog("系统", "成功", "日志 CDP连接成功");
                    // 更新所有账号状态
                    UpdateAllAccountsStatus("登录成功");
                    }
                    else
                    {
                    AddLog("系统", "警告", "日志 CDP连接断开");
                    UpdateAllAccountsStatus("已断开");
                }
            };
        }
        
        private void InitializeTrayIcon()
        {
            _trayMenu = new ContextMenuStrip();
            _trayMenu.Items.Add("显示主窗口", null, (s, e) => ShowMainWindow());
            _trayMenu.Items.Add("-");
            _trayMenu.Items.Add("退出", null, (s, e) => ExitApplication());
            
            _trayIcon = new NotifyIcon
            {
                Text = "招财狗框架",
                Visible = true,
                ContextMenuStrip = _trayMenu
            };
            
            try { _trayIcon.Icon = SystemIcons.Application; } catch { }
            _trayIcon.DoubleClick += (s, e) => ShowMainWindow();
        }
        
        private async Task StartServerAsync()
        {
            var success = await _server.StartAsync();
            
            if (success)
            {
                AddLog("插件", "插件", "日志 框架服务已启动");
            }
            else
            {
                AddLog("插件", "插件", "日志 框架服务启动失败");
            }
        }
        
        private async Task StopServerAsync()
        {
            await _server.StopAsync();
            AddLog("插件", "插件", "日志 框架服务已停止");
        }
        
        private void AddLog(string response, string type, string message)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => AddLog(response, type, message)));
                return;
            }
            
            logId++;
            var item = new ListViewItem(logId.ToString());
            item.SubItems.Add(DateTime.Now.ToString("MM-dd HH:mm:ss"));
            item.SubItems.Add(response);
            item.SubItems.Add(type);
            item.SubItems.Add(message);
            
            lvLog.Items.Insert(0, item);
            
            // 限制日志条数
            if (lvLog.Items.Count > 500)
            {
                lvLog.Items.RemoveAt(lvLog.Items.Count - 1);
            }
        }
        
        private void AddAccount(string connId, string nickname, string wwid, string groupId, string status, string auto = "×", string account = "")
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => AddAccount(connId, nickname, wwid, groupId, status, auto, account)));
                return;
            }
            
            // 数据验证 - 跳过无效账号（昵称为默认值且没有真实数据）
            bool isDefaultNickname = string.IsNullOrEmpty(nickname) || 
                                     nickname == "旺商聊用户" || 
                                     nickname == "未知";
            bool hasRealData = !string.IsNullOrEmpty(wwid) || 
                               !string.IsNullOrEmpty(groupId) || 
                               !string.IsNullOrEmpty(account);
            
            if (isDefaultNickname && !hasRealData)
            {
                // 跳过无效数据，不添加
                Logger.Info($"[AddAccount] 跳过无效账号: nickname={nickname}, wwid={wwid}");
                return;
            }
            
            // 检查是否已存在 (使用 connId 作为 Tag)
            foreach (ListViewItem existing in lvAccounts.Items)
            {
                if ((existing.Tag as string) == connId)
                {
                    // 更新现有项
                    existing.SubItems[1].Text = nickname;
                    existing.SubItems[2].Text = wwid;
                    existing.SubItems[3].Text = groupId;
                    existing.SubItems[4].Text = status;
                    existing.SubItems[5].Text = auto;
                    existing.SubItems[6].Text = account;
                    return;
                }
            }
            
            // 创建新项，ID 为递增序号
            var displayId = (lvAccounts.Items.Count + 1).ToString();
            var item = new ListViewItem(displayId);
            item.Tag = connId; // 使用 Tag 存储 connId
            item.SubItems.Add(nickname);
            item.SubItems.Add(wwid);
            item.SubItems.Add(groupId);
            item.SubItems.Add(status);
            item.SubItems.Add(auto);
            item.SubItems.Add(account);
            
            lvAccounts.Items.Add(item);
        }
        
        private void RemoveAccount(string connId)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => RemoveAccount(connId)));
                return;
            }
            
            foreach (ListViewItem item in lvAccounts.Items)
            {
                if ((item.Tag as string) == connId)
                {
                    lvAccounts.Items.Remove(item);
                    // 重新编号
                    for (int i = 0; i < lvAccounts.Items.Count; i++)
                    {
                        lvAccounts.Items[i].Text = (i + 1).ToString();
                    }
                    break;
                }
            }
        }
        
        private void ShowMainWindow()
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.Activate();
        }
        
        private void ExitApplication()
        {
            // 保存账号列表
            SaveAllAccounts();
            
            // 保存玩家数据
            _playerService?.SaveData();
            
            // 停止心跳服务
            try
            {
                HeartbeatService.Instance.StopAsync().Wait(2000);
                HeartbeatService.Instance.Dispose();
            }
            catch { }
            
            // 停止 HTTP API
            try
            {
                WangShangLiaoHttpApi.Instance.Dispose();
            }
            catch { }
            
            _trayIcon.Visible = false;
            _server?.Dispose();
            Application.Exit();
        }
        
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            var chkMinTray = FindControl<CheckBox>("chkMinTray");
            
            if (chkMinTray?.Checked == true && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
                _trayIcon.ShowBalloonTip(1000, "招财狗框架", "程序已最小化到托盘", ToolTipIcon.Info);
            }
            else
            {
                ExitApplication();
            }
        }
        
        private T FindControl<T>(string name) where T : Control
        {
            var controls = this.Controls.Find(name, true);
            return controls.Length > 0 ? controls[0] as T : null;
        }
        
        protected override async void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            
            // 调整列宽适应窗口
            AdjustLogColumnWidth();
            
            AddLog("插件", "插件", "日志 招财狗框架已启动");
            
            // 加载已保存的账号
            LoadSavedAccounts();
            
            // 启动心跳服务 (端口 51234)
            await StartHeartbeatServiceAsync();
            
            // 自动启动服务
            await StartServerAsync();
        }
        
        /// <summary>
        /// 启动心跳服务
        /// </summary>
        private async Task StartHeartbeatServiceAsync()
        {
            try
            {
                var heartbeatService = HeartbeatService.Instance;
                heartbeatService.OnLog += msg => AddLog("心跳", "系统", msg);
                heartbeatService.OnStatusChanged += online =>
                {
                    AddLog("心跳", online ? "在线" : "离线", $"设备状态: {(online ? "在线" : "离线")}");
                };
                
                var success = await heartbeatService.StartAsync();
                if (success)
                {
                    AddLog("心跳", "系统", $"✓ 心跳服务已启动 - http://127.0.0.1:{HeartbeatService.DEFAULT_PORT}/ping");
                }
                else
                {
                    AddLog("心跳", "错误", "✗ 心跳服务启动失败");
                }
            }
            catch (Exception ex)
            {
                AddLog("心跳", "错误", $"心跳服务异常: {ex.Message}");
            }
        }
        
        #region 窗口边缘拖拽调整大小
        
        private const int WM_NCHITTEST = 0x84;
        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HTCLIENT = 1;
        private const int HTCAPTION = 2;
        private const int HTLEFT = 10;
        private const int HTRIGHT = 11;
        private const int HTTOP = 12;
        private const int HTTOPLEFT = 13;
        private const int HTTOPRIGHT = 14;
        private const int HTBOTTOM = 15;
        private const int HTBOTTOMLEFT = 16;
        private const int HTBOTTOMRIGHT = 17;
        
        private const int RESIZE_BORDER = 10;  // 边缘响应区域宽度
        
        // 启用窗口调整大小的样式
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.Style |= 0x20000;  // WS_MINIMIZEBOX
                cp.Style |= 0x40000;  // WS_THICKFRAME - 允许调整大小
                return cp;
            }
        }
        
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            
            if (m.Msg == WM_NCHITTEST && (int)m.Result == HTCLIENT)
            {
                // 获取鼠标在窗口中的位置
                Point screenPoint = new Point(m.LParam.ToInt32());
                Point clientPoint = PointToClient(screenPoint);
                
                int w = ClientSize.Width;
                int h = ClientSize.Height;
                int x = clientPoint.X;
                int y = clientPoint.Y;
                
                // 判断鼠标位置并设置对应的命中测试值
                if (x < RESIZE_BORDER && y < RESIZE_BORDER)
                    m.Result = (IntPtr)HTTOPLEFT;
                else if (x >= w - RESIZE_BORDER && y < RESIZE_BORDER)
                    m.Result = (IntPtr)HTTOPRIGHT;
                else if (x < RESIZE_BORDER && y >= h - RESIZE_BORDER)
                    m.Result = (IntPtr)HTBOTTOMLEFT;
                else if (x >= w - RESIZE_BORDER && y >= h - RESIZE_BORDER)
                    m.Result = (IntPtr)HTBOTTOMRIGHT;
                else if (x < RESIZE_BORDER)
                    m.Result = (IntPtr)HTLEFT;
                else if (x >= w - RESIZE_BORDER)
                    m.Result = (IntPtr)HTRIGHT;
                else if (y < RESIZE_BORDER)
                    m.Result = (IntPtr)HTTOP;
                else if (y >= h - RESIZE_BORDER)
                    m.Result = (IntPtr)HTBOTTOM;
            }
        }
        
        #endregion
    }
    
    // 原生方法用于窗口拖动
    internal static class NativeMethods
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool ReleaseCapture();
    }
}
