using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Timers;
using WangShangLiaoBot.Models;
using WangShangLiaoBot.Services;
using WangShangLiaoBot.Services.HPSocket;
using WangShangLiaoBot.Forms.Settings;
using WangShangLiaoBot.Controls;
using WangShangLiaoBot.Controls.BetProcess;
using WangShangLiaoBot.Controls.Odds;

namespace WangShangLiaoBot.Forms
{
    public partial class MainForm : Form
    {
        private void InitializeEvents()
        {
            // Subscribe to ChatService events
            ChatService.Instance.OnConnectionChanged += OnConnectionChanged;
            ChatService.Instance.OnLog += OnServiceLog;
            
            // Subscribe to AutoReplyService events
            AutoReplyService.Instance.OnStatusChanged += OnAutoReplyStatusChanged;
            AutoReplyService.Instance.OnLog += OnServiceLog;
            
            // Subscribe to AdminCommandService events for ScoreForm UI updates
            AdminCommandService.Instance.OnPendingRequestAdded += OnPendingScoreRequestAdded;
            
            // Subscribe to Logger events
            Logger.OnLog += (msg, level) => OnServiceLog(msg);
            
            // Button click events - Mute/Unmute
            btnMuteAll.Click += btnMuteAll_Click;
            btnUnmuteAll.Click += btnUnmuteAll_Click;

            // Rebate tool top bar events
            if (_rebateToolCtrl != null)
            {
                _rebateToolCtrl.OnClearDataRequested += RebateTool_OnClearDataRequested;
                _rebateToolCtrl.OnOperationLogRequested += RebateTool_OnOperationLogRequested;
            }
        }

        private void OnAutoReplyStatusChanged(bool running)
        {
            // Update UI if needed
        }

        private void OnServiceLog(string message)
        {
            // Log to file or status bar
            if (InvokeRequired)
            {
                try { Invoke(new Action(() => OnServiceLog(message))); }
                catch { }
                return;
            }
            lblStatus.Text = message;
        }

        private void btnScoreWindow_Click(object sender, EventArgs e)
        {
            if (_scoreForm == null || _scoreForm.IsDisposed)
            {
                _scoreForm = new ScoreForm();
            }
            _scoreForm.Show();
            _scoreForm.BringToFront();
        }

        private void OnPendingScoreRequestAdded(string wangWangId, decimal amount, string reason, string type)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnPendingScoreRequestAdded(wangWangId, amount, reason, type)));
                return;
            }
            
            // Ensure ScoreForm exists
            if (_scoreForm == null || _scoreForm.IsDisposed)
            {
                _scoreForm = new ScoreForm();
            }
            
            // Get or create player and update their info
            var player = DataService.Instance.GetOrCreatePlayer(wangWangId, null);
            var nickname = player?.Nickname ?? wangWangId;
            var grain = player?.Score.ToString() ?? "0"; // 余粮 = 当前分数
            var count = player?.BetCount.ToString() ?? "0"; // 次数 = 下注次数
            var speakContent = reason; // Original message content for display
            
            // Update player data in main list
            if (player != null)
            {
                // Update player's remark to show the request content (下注内容列)
                player.Remark = $"{type}:{reason}";
                player.LastActiveTime = DateTime.Now;
                DataService.Instance.SavePlayer(player);
                
                // Refresh main window player list to show updated data (reload from database)
                RefreshPlayerList(reloadFromDatabase: true);
            }
            
            // Add to appropriate list based on type
            if (type == "上分")
            {
                _scoreForm.AddUpScoreRequest(wangWangId, nickname, amount.ToString(), grain, count, speakContent);
            }
            else if (type == "下分")
            {
                _scoreForm.AddDownScoreRequest(wangWangId, nickname, amount.ToString(), grain, count, speakContent);
            }
        }

        private async void menuTestConnection_Click(object sender, EventArgs e)
        {
            // 检查副框架连接状态
            var frameworkClient = Services.HPSocket.FrameworkClient.Instance;
            if (!frameworkClient.IsConnected)
            {
                MessageBox.Show("请先连接副框架！\n\n副框架（招财狗框架）需要先启动并连接旺商聊", 
                    "未连接", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            // Show test dialog
            using (var dialog = new Form())
            {
                dialog.Text = "测试连接";
                dialog.Size = new Size(400, 250);
                dialog.StartPosition = FormStartPosition.CenterParent;
                dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialog.MaximizeBox = false;
                dialog.MinimizeBox = false;
                
                var lblInfo = new Label();
                lblInfo.Text = "请先在旺商聊中打开一个聊天窗口，然后输入测试消息：";
                lblInfo.Location = new Point(10, 15);
                lblInfo.Size = new Size(370, 20);
                dialog.Controls.Add(lblInfo);
                
                var txtMessage = new TextBox();
                txtMessage.Text = "测试消息 - 来自旺商聊机器人";
                txtMessage.Location = new Point(10, 45);
                txtMessage.Size = new Size(360, 21);
                dialog.Controls.Add(txtMessage);
                
                var lblStatus = new Label();
                lblStatus.Text = "连接状态: " + (ChatService.Instance.Mode == ConnectionMode.CDP ? "CDP模式" : "UI自动化模式");
                lblStatus.Location = new Point(10, 80);
                lblStatus.Size = new Size(360, 20);
                lblStatus.ForeColor = Color.Blue;
                dialog.Controls.Add(lblStatus);
                
                var btnSend = new Button();
                btnSend.Text = "发送测试消息";
                btnSend.Location = new Point(10, 110);
                btnSend.Size = new Size(120, 30);
                btnSend.Click += async (s, args) =>
                {
                    btnSend.Enabled = false;
                    btnSend.Text = "发送中...";
                    
                    try
                    {
                        // Allow test dialog to also verify template engine rendering (date/time/countdown/lottery history etc.)
                        var rendered = TemplateEngine.Render(txtMessage.Text, new TemplateEngine.RenderContext
                        {
                            Today = DateTime.Today
                        });
                        var success = await ChatService.Instance.SendMessageAsync(rendered);
                        if (success)
                        {
                            lblStatus.Text = "✓ 消息发送成功！";
                            lblStatus.ForeColor = Color.Green;
                        }
                        else
                        {
                            lblStatus.Text = "✗ 消息发送失败，请确保已打开聊天窗口";
                            lblStatus.ForeColor = Color.Red;
                        }
                    }
                    catch (Exception ex)
                    {
                        lblStatus.Text = "✗ 发送异常: " + ex.Message;
                        lblStatus.ForeColor = Color.Red;
                    }
                    
                    btnSend.Enabled = true;
                    btnSend.Text = "发送测试消息";
                };
                dialog.Controls.Add(btnSend);
                
                var btnGetContacts = new Button();
                btnGetContacts.Text = "获取联系人";
                btnGetContacts.Location = new Point(140, 110);
                btnGetContacts.Size = new Size(100, 30);
                btnGetContacts.Click += async (s, args) =>
                {
                    btnGetContacts.Enabled = false;
                    btnGetContacts.Text = "获取中...";
                    
                    try
                    {
                        var contacts = await ChatService.Instance.GetContactListAsync();
                        if (contacts != null && contacts.Count > 0)
                        {
                            lblStatus.Text = string.Format("✓ 获取到 {0} 个联系人", contacts.Count);
                            lblStatus.ForeColor = Color.Green;
                        }
                        else
                        {
                            lblStatus.Text = "✗ 未获取到联系人";
                            lblStatus.ForeColor = Color.Red;
                        }
                    }
                    catch (Exception ex)
                    {
                        lblStatus.Text = "✗ 获取异常: " + ex.Message;
                        lblStatus.ForeColor = Color.Red;
                    }
                    
                    btnGetContacts.Enabled = true;
                    btnGetContacts.Text = "获取联系人";
                };
                dialog.Controls.Add(btnGetContacts);
                
                var btnGetAccount = new Button();
                btnGetAccount.Text = "获取我的账号";
                btnGetAccount.Location = new Point(250, 110);
                btnGetAccount.Size = new Size(110, 30);
                btnGetAccount.Click += async (s, args) =>
                {
                    btnGetAccount.Enabled = false;
                    btnGetAccount.Text = "获取中...";
                    
                    try
                    {
                        var account = await ChatService.Instance.GetMyAccountAsync();
                        if (!string.IsNullOrEmpty(account))
                        {
                            lblStatus.Text = "✓ 我的旺商号: " + account;
                            lblStatus.ForeColor = Color.Green;
                        }
                        else
                        {
                            lblStatus.Text = "✗ 未能获取账号";
                            lblStatus.ForeColor = Color.Red;
                        }
                    }
                    catch (Exception ex)
                    {
                        lblStatus.Text = "✗ 获取异常: " + ex.Message;
                        lblStatus.ForeColor = Color.Red;
                    }
                    
                    btnGetAccount.Enabled = true;
                    btnGetAccount.Text = "获取我的账号";
                };
                dialog.Controls.Add(btnGetAccount);
                
                // Add "Get Messages" button to test message reading
                var btnGetMessages = new Button();
                btnGetMessages.Text = "获取消息";
                btnGetMessages.Location = new Point(10, 150);
                btnGetMessages.Size = new Size(100, 30);
                btnGetMessages.Click += async (s, args) =>
                {
                    btnGetMessages.Enabled = false;
                    btnGetMessages.Text = "获取中...";
                    
                    try
                    {
                        var messages = await ChatService.Instance.GetChatMessagesAsync();
                        if (messages.Count > 0)
                        {
                            lblStatus.Text = $"✓ 获取到 {messages.Count} 条消息";
                            lblStatus.ForeColor = Color.Green;
                            
                            // Show first few messages
                            var msgText = string.Join("\n", messages.Take(3).Select(m => 
                                $"[{(m.IsSelf ? "我" : m.SenderName)}]: {(m.Content?.Length > 30 ? m.Content.Substring(0, 30) + "..." : m.Content)}"));
                            MessageBox.Show($"最近消息:\n\n{msgText}", "消息列表", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            lblStatus.Text = "✗ 未获取到消息";
                            lblStatus.ForeColor = Color.Red;
                        }
                    }
                    catch (Exception ex)
                    {
                        lblStatus.Text = "✗ 获取异常: " + ex.Message;
                        lblStatus.ForeColor = Color.Red;
                    }
                    
                    btnGetMessages.Enabled = true;
                    btnGetMessages.Text = "获取消息";
                };
                dialog.Controls.Add(btnGetMessages);
                
                var btnClose = new Button();
                btnClose.Text = "关闭";
                btnClose.Location = new Point(290, 170);
                btnClose.Size = new Size(80, 30);
                btnClose.Click += (s, args) => dialog.Close();
                dialog.Controls.Add(btnClose);
                
                dialog.ShowDialog(this);
            }
        }

        private void menuScoreSettings_Click(object sender, EventArgs e)
        {
            // 显示设置界面
            ShowSettingsView();
        }

        private void menuLockSettings_Click(object sender, EventArgs e)
        {
            ShowSealSettingsView();
        }

        private void menuRebateTools_Click(object sender, EventArgs e)
        {
            ShowRebateToolView();
        }

        private void menuCustomer_Click(object sender, EventArgs e)
        {
            // 显示客户管理界面
            ShowCustomerView();
        }

        private void ShowSealSettingsView()
        {
            // 隐藏主界面控件
            panelLeft.Visible = false;
            panelMiddle.Visible = false;
            panelRight.Visible = false;
            panelPlayerInfo.Visible = false;
            listPlayers.Visible = false;
            
            // 隐藏顶部工具栏
            panelTopBar.Visible = false;
            
            // 隐藏算账设置TabControl
            tabSettings.Visible = false;
            
            // 隐藏回水工具
            pnlRebateTool.Visible = false;
            
            // 显示封盘设置TabControl，并调整位置紧贴菜单栏
            tabSealSettings.Location = new System.Drawing.Point(0, menuStrip.Height);
            tabSealSettings.Size = new System.Drawing.Size(this.ClientSize.Width, this.ClientSize.Height - menuStrip.Height - statusStrip.Height);
            tabSealSettings.Visible = true;
        }

        private void ShowCustomerView()
        {
            // 显示主界面控件
            panelLeft.Visible = true;
            panelMiddle.Visible = true;
            panelRight.Visible = true;
            panelPlayerInfo.Visible = true;
            listPlayers.Visible = true;
            
            // 显示顶部工具栏（整个面板）
            panelTopBar.Visible = true;
            
            // 隐藏设置TabControl
            tabSettings.Visible = false;
            
            // 隐藏封盘设置TabControl
            tabSealSettings.Visible = false;
            
            // 隐藏回水工具
            pnlRebateTool.Visible = false;
        }

        private void ShowSettingsView()
        {
            // 【性能优化】设置页控件很重，仅在首次打开设置时初始化
            if (!_settingsControlsInitialized)
            {
                try
                {
                    Cursor = Cursors.WaitCursor;
                    InitializeSettingsControls();
                    _settingsControlsInitialized = true;
                    LoadConfig(); // 初始化控件后再回填配置
                }
                catch (Exception ex)
                {
                    Logger.Error($"[MainForm] 初始化设置控件失败: {ex.Message}");
                }
                finally
                {
                    Cursor = Cursors.Default;
                }
            }

            // 隐藏主界面控件
            panelLeft.Visible = false;
            panelMiddle.Visible = false;
            panelRight.Visible = false;
            panelPlayerInfo.Visible = false;
            listPlayers.Visible = false;
            
            // 隐藏顶部工具栏（整个面板）
            panelTopBar.Visible = false;
            
            // 隐藏封盘设置TabControl
            tabSealSettings.Visible = false;
            
            // 隐藏回水工具
            pnlRebateTool.Visible = false;
            
            // 显示设置TabControl，并调整位置和大小自适应窗口
            tabSettings.Location = new System.Drawing.Point(0, menuStrip.Height);
            tabSettings.Size = new System.Drawing.Size(this.ClientSize.Width, this.ClientSize.Height - menuStrip.Height - statusStrip.Height);
            tabSettings.Visible = true;
        }

        private void btnChatLog_Click(object sender, EventArgs e)
        {
            try
            {
                // Get today's message logs
                var logs = DataService.Instance.GetMessageLogs(DateTime.Today);
                
                // Create chat log dialog
                using (var form = new Form())
                {
                    form.Text = $"聊天日志 - {DateTime.Today:yyyy-MM-dd}";
                    form.Size = new System.Drawing.Size(650, 550);
                    form.FormBorderStyle = FormBorderStyle.Sizable;
                    form.StartPosition = FormStartPosition.CenterParent;
                    form.MinimumSize = new System.Drawing.Size(500, 400);
                    
                    // Stats panel at top
                    var pnlStats = new Panel();
                    pnlStats.Dock = DockStyle.Top;
                    pnlStats.Height = 35;
                    pnlStats.Padding = new Padding(10, 5, 10, 5);
                    
                    var sentCount = logs.Count(l => l.Direction == "发送");
                    var receivedCount = logs.Count(l => l.Direction == "接收");
                    
                    var lblStats = new Label();
                    lblStats.Text = $"📊 今日消息: 共 {logs.Count} 条 | 发送: {sentCount} | 接收: {receivedCount}";
                    lblStats.Dock = DockStyle.Fill;
                    lblStats.Font = new System.Drawing.Font("Microsoft YaHei", 9F);
                    pnlStats.Controls.Add(lblStats);
                    form.Controls.Add(pnlStats);
                    
                    // Chat log text box
                    var txtLog = new RichTextBox();
                    txtLog.Dock = DockStyle.Fill;
                    txtLog.Font = new System.Drawing.Font("Microsoft YaHei", 9F);
                    txtLog.ReadOnly = true;
                    txtLog.BackColor = System.Drawing.Color.White;
                    txtLog.WordWrap = true;
                    
                    // Populate chat log (newest first)
                    var sb = new System.Text.StringBuilder();
                    foreach (var log in logs.OrderByDescending(l => l.Time))
                    {
                        var direction = log.Direction == "发送" ? "📤" : "📥";
                        var name = !string.IsNullOrEmpty(log.ContactName) ? log.ContactName : log.ContactId;
                        var content = log.Content ?? "";
                        if (content.Length > 200) content = content.Substring(0, 200) + "...";
                        
                        sb.AppendLine($"[{log.Time:HH:mm:ss}] {direction} {name}");
                        sb.AppendLine($"  {content}");
                        sb.AppendLine();
                    }
                    txtLog.Text = sb.ToString();
                    form.Controls.Add(txtLog);
                    
                    // Ensure txtLog is below pnlStats
                    txtLog.BringToFront();
                    pnlStats.BringToFront();
                    
                    // Button panel at bottom
                    var pnlButtons = new Panel();
                    pnlButtons.Dock = DockStyle.Bottom;
                    pnlButtons.Height = 45;
                    pnlButtons.Padding = new Padding(10);
                    
                    // Filter combo box
                    var lblFilter = new Label { Text = "筛选:", Location = new System.Drawing.Point(10, 12), AutoSize = true };
                    pnlButtons.Controls.Add(lblFilter);
                    
                    var cmbFilter = new ComboBox();
                    cmbFilter.Location = new System.Drawing.Point(50, 8);
                    cmbFilter.Size = new System.Drawing.Size(80, 25);
                    cmbFilter.DropDownStyle = ComboBoxStyle.DropDownList;
                    cmbFilter.Items.AddRange(new object[] { "全部", "发送", "接收" });
                    cmbFilter.SelectedIndex = 0;
                    cmbFilter.SelectedIndexChanged += (s, args) =>
                    {
                        var filter = cmbFilter.SelectedItem.ToString();
                        var filtered = logs.AsEnumerable();
                        if (filter == "发送") filtered = logs.Where(l => l.Direction == "发送");
                        else if (filter == "接收") filtered = logs.Where(l => l.Direction == "接收");
                        
                        var fsb = new System.Text.StringBuilder();
                        foreach (var log in filtered.OrderByDescending(l => l.Time))
                        {
                            var direction = log.Direction == "发送" ? "📤" : "📥";
                            var name = !string.IsNullOrEmpty(log.ContactName) ? log.ContactName : log.ContactId;
                            var content = log.Content ?? "";
                            if (content.Length > 200) content = content.Substring(0, 200) + "...";
                            
                            fsb.AppendLine($"[{log.Time:HH:mm:ss}] {direction} {name}");
                            fsb.AppendLine($"  {content}");
                            fsb.AppendLine();
                        }
                        txtLog.Text = fsb.ToString();
                        lblStats.Text = $"📊 筛选结果: {filtered.Count()} 条";
                    };
                    pnlButtons.Controls.Add(cmbFilter);
                    
                    var btnRefresh = new Button();
                    btnRefresh.Text = "刷新";
                    btnRefresh.Location = new System.Drawing.Point(140, 8);
                    btnRefresh.Size = new System.Drawing.Size(60, 28);
                    btnRefresh.Click += (s, args) =>
                    {
                        logs = DataService.Instance.GetMessageLogs(DateTime.Today);
                        cmbFilter.SelectedIndex = 0;
                        sentCount = logs.Count(l => l.Direction == "发送");
                        receivedCount = logs.Count(l => l.Direction == "接收");
                        lblStats.Text = $"📊 今日消息: 共 {logs.Count} 条 | 发送: {sentCount} | 接收: {receivedCount}";
                        
                        var rsb = new System.Text.StringBuilder();
                        foreach (var log in logs.OrderByDescending(l => l.Time))
                        {
                            var direction = log.Direction == "发送" ? "📤" : "📥";
                            var name = !string.IsNullOrEmpty(log.ContactName) ? log.ContactName : log.ContactId;
                            var content = log.Content ?? "";
                            if (content.Length > 200) content = content.Substring(0, 200) + "...";
                            
                            rsb.AppendLine($"[{log.Time:HH:mm:ss}] {direction} {name}");
                            rsb.AppendLine($"  {content}");
                            rsb.AppendLine();
                        }
                        txtLog.Text = rsb.ToString();
                    };
                    pnlButtons.Controls.Add(btnRefresh);
                    
                    var btnCopy = new Button();
                    btnCopy.Text = "复制全部";
                    btnCopy.Location = new System.Drawing.Point(210, 8);
                    btnCopy.Size = new System.Drawing.Size(70, 28);
                    btnCopy.Click += (s, args) =>
                    {
                        if (!string.IsNullOrEmpty(txtLog.Text))
                        {
                            Clipboard.SetText(txtLog.Text);
                            MessageBox.Show("已复制到剪贴板", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    };
                    pnlButtons.Controls.Add(btnCopy);
                    
                    var btnOpenDir = new Button();
                    btnOpenDir.Text = "打开目录";
                    btnOpenDir.Location = new System.Drawing.Point(290, 8);
                    btnOpenDir.Size = new System.Drawing.Size(70, 28);
                    btnOpenDir.Click += (s, args) =>
                    {
                        var logDir = DataService.Instance.MessageLogDir;
                        if (Directory.Exists(logDir))
                        {
                            System.Diagnostics.Process.Start("explorer.exe", logDir);
                        }
                    };
                    pnlButtons.Controls.Add(btnOpenDir);
                    
                    var btnYesterday = new Button();
                    btnYesterday.Text = "昨日";
                    btnYesterday.Location = new System.Drawing.Point(370, 8);
                    btnYesterday.Size = new System.Drawing.Size(50, 28);
                    btnYesterday.Click += (s, args) =>
                    {
                        var yesterdayLogs = DataService.Instance.GetMessageLogs(DateTime.Today.AddDays(-1));
                        if (yesterdayLogs.Count == 0)
                        {
                            MessageBox.Show("昨日无聊天记录", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return;
                        }
                        
                        form.Text = $"聊天日志 - {DateTime.Today.AddDays(-1):yyyy-MM-dd}";
                        logs = yesterdayLogs;
                        sentCount = logs.Count(l => l.Direction == "发送");
                        receivedCount = logs.Count(l => l.Direction == "接收");
                        lblStats.Text = $"📊 昨日消息: 共 {logs.Count} 条 | 发送: {sentCount} | 接收: {receivedCount}";
                        cmbFilter.SelectedIndex = 0;
                        
                        var ysb = new System.Text.StringBuilder();
                        foreach (var log in logs.OrderByDescending(l => l.Time))
                        {
                            var direction = log.Direction == "发送" ? "📤" : "📥";
                            var name = !string.IsNullOrEmpty(log.ContactName) ? log.ContactName : log.ContactId;
                            var content = log.Content ?? "";
                            if (content.Length > 200) content = content.Substring(0, 200) + "...";
                            
                            ysb.AppendLine($"[{log.Time:HH:mm:ss}] {direction} {name}");
                            ysb.AppendLine($"  {content}");
                            ysb.AppendLine();
                        }
                        txtLog.Text = ysb.ToString();
                    };
                    pnlButtons.Controls.Add(btnYesterday);
                    
                    var btnClose = new Button();
                    btnClose.Text = "关闭";
                    btnClose.Location = new System.Drawing.Point(560, 8);
                    btnClose.Size = new System.Drawing.Size(60, 28);
                    btnClose.DialogResult = DialogResult.OK;
                    pnlButtons.Controls.Add(btnClose);
                    
                    form.Controls.Add(pnlButtons);
                    form.AcceptButton = btnClose;
                    
                    form.ShowDialog(this);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[MainForm] Chat log error: {ex.Message}");
                MessageBox.Show($"获取聊天日志失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowRebateToolView()
        {
            // 隐藏主界面控件
            panelLeft.Visible = false;
            panelMiddle.Visible = false;
            panelRight.Visible = false;
            panelPlayerInfo.Visible = false;
            listPlayers.Visible = false;
            
            // 隐藏顶部工具栏
            panelTopBar.Visible = false;
            
            // 隐藏算账设置TabControl
            tabSettings.Visible = false;
            
            // 隐藏封盘设置TabControl
            tabSealSettings.Visible = false;
            
            // 显示回水工具控件，并调整位置紧贴菜单栏
            pnlRebateTool.Location = new System.Drawing.Point(0, menuStrip.Height);
            pnlRebateTool.Size = new System.Drawing.Size(this.ClientSize.Width, this.ClientSize.Height - menuStrip.Height - statusStrip.Height);
            pnlRebateTool.Visible = true;
        }

        private void menuRunLog_Click(object sender, EventArgs e)
        {
            try
            {
                var logDir = Path.Combine(Application.StartupPath, "Logs");
                if (!Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }
                Process.Start("explorer.exe", logDir);
            }
            catch (Exception ex)
            {
                Logger.Error($"[MainForm] Open log folder error: {ex.Message}");
                MessageBox.Show($"打开日志目录失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void menuAccountList_Click(object sender, EventArgs e)
        {
            try
            {
                // 显示账号列表窗口
                using (var form = new Form())
                {
                    form.Text = "账号列表";
                    form.Size = new Size(600, 400);
                    form.StartPosition = FormStartPosition.CenterParent;
                    form.FormBorderStyle = FormBorderStyle.Sizable;
                    
                    var lvAccounts = new ListView();
                    lvAccounts.Dock = DockStyle.Fill;
                    lvAccounts.View = View.Details;
                    lvAccounts.FullRowSelect = true;
                    lvAccounts.GridLines = true;
                    lvAccounts.Columns.Add("账号ID", 150);
                    lvAccounts.Columns.Add("昵称", 150);
                    lvAccounts.Columns.Add("状态", 80);
                    lvAccounts.Columns.Add("登录时间", 120);
                    
                    // 从配置获取账号信息
                    var config = AppConfig.Instance;
                    if (!string.IsNullOrEmpty(config.WangWangId))
                    {
                        var item = new ListViewItem(config.WangWangId);
                        item.SubItems.Add(config.Nickname ?? "");
                        item.SubItems.Add(ChatService.Instance.IsConnected ? "已连接" : "未连接");
                        item.SubItems.Add(DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
                        lvAccounts.Items.Add(item);
                    }
                    
                    form.Controls.Add(lvAccounts);
                    form.ShowDialog(this);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[MainForm] Account list error: {ex.Message}");
                MessageBox.Show($"显示账号列表失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void menuSystemSettings_Click(object sender, EventArgs e)
        {
            try
            {
                // 打开系统设置窗口
                using (var form = new SystemSettingsForm())
                {
                    form.ShowDialog(this);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[MainForm] System settings error: {ex.Message}");
                MessageBox.Show($"打开系统设置失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// YX代理服务菜单点击事件 - 启动模拟xplugin的代理服务
        /// </summary>
        private void menuYxProxy_Click(object sender, EventArgs e)
        {
            try
            {
                var proxy = Services.XClient.YxSdkProxyServer.Instance;
                
                if (proxy.IsRunning)
                {
                    // 已运行，询问是否停止
                    var result = MessageBox.Show(
                        "YX代理服务正在运行中。\n\n是否停止服务？",
                        "YX代理服务",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);
                    
                    if (result == DialogResult.Yes)
                    {
                        proxy.Stop();
                        MessageBox.Show("YX代理服务已停止。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        menuYxProxy.Text = "YX代理";
                    }
                }
                else
                {
                    // 绑定日志事件
                    proxy.OnLog += msg => Logger.Info(msg);
                    
                    // 绑定消息事件
                    proxy.OnSendMessageRequest += (scene, targetId, content) =>
                    {
                        Logger.Info($"[YX代理] 发送消息请求: Scene={scene}, Target={targetId}, Content={content?.Substring(0, Math.Min(50, content?.Length ?? 0))}...");
                        // 转发到ChatService
                        _ = ChatService.Instance.SendTextAsync(scene, targetId, content);
                    };
                    
                    // 加载ZCG配置中的NIM凭证
                    var zcgPath = @"C:\Users\Administrator\Desktop\zcg25.2.15";
                    proxy.LoadNimCredentials(zcgPath);
                    
                    // 启动代理服务
                    if (proxy.Start())
                    {
                        MessageBox.Show(
                            "YX代理服务已启动！\n\n" +
                            "端口: 5749\n" +
                            "协议: HPSocket Pack\n\n" +
                            "现在可以启动621705120.exe进行连接测试。",
                            "YX代理服务",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                        menuYxProxy.Text = "YX代理(运行中)";
                    }
                    else
                    {
                        MessageBox.Show(
                            "YX代理服务启动失败！\n\n" +
                            "请检查端口5749是否被占用。",
                            "错误",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[MainForm] YX代理服务错误: {ex.Message}");
                MessageBox.Show($"YX代理服务错误: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

    }
}
