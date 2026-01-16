using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using WSLFramework.Services;

namespace WSLFramework.Forms
{
    /// <summary>
    /// 上下分处理窗口 - 处理玩家上分下分请求
    /// 对应招财狗的上下分处理功能
    /// </summary>
    public class ScoreForm : Form
    {
        private PlayerService _playerService;
        private Action<string, string> _sendMessageCallback;
        
        // 控件
        private TabControl tabControl;
#pragma warning disable CS0169 // 保留给上下分列表UI
        private ListView lvUpRequests;
        private ListView lvDownRequests;
#pragma warning restore CS0169
        private ListView lvPlayers;
        
        // 上分操作
        private Label lblUpPlayer;
        private TextBox txtUpContent;
        private NumericUpDown numUpAmount;
        private Button btnUpApprove;
        private Button btnUpReject;
        private Button btnUpIgnore;
        
        // 下分操作
        private Label lblDownPlayer;
        private TextBox txtDownContent;
        private NumericUpDown numDownAmount;
        private Button btnDownApprove;
        private Button btnDownReject;
        private Button btnDownIgnore;
        
        // 待处理请求队列
        private Queue<ScoreRequest> _upRequests = new Queue<ScoreRequest>();
        private Queue<ScoreRequest> _downRequests = new Queue<ScoreRequest>();
        private ScoreRequest _currentUpRequest;
        private ScoreRequest _currentDownRequest;
        
        public ScoreForm(PlayerService playerService, Action<string, string> sendMessageCallback)
        {
            _playerService = playerService;
            _sendMessageCallback = sendMessageCallback;
            InitializeComponent();
            LoadPlayers();
        }
        
        private void InitializeComponent()
        {
            this.Text = "上下分处理 - F11可隐藏";
            this.Size = new Size(500, 600);
            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(Screen.PrimaryScreen.WorkingArea.Width - 520, 100);
            this.Font = new Font("Microsoft YaHei UI", 9F);
            this.TopMost = true;
            this.ShowInTaskbar = false;
            this.FormBorderStyle = FormBorderStyle.SizableToolWindow;
            
            // 创建 TabControl
            tabControl = new TabControl
            {
                Dock = DockStyle.Fill
            };
            
            var tabUpDown = new TabPage("上分/下分");
            var tabSettings = new TabPage("设置");
            var tabSettings2 = new TabPage("设置2");
            var tabTips = new TabPage("提示文本");
            
            CreateUpDownTab(tabUpDown);
            
            tabControl.TabPages.Add(tabUpDown);
            tabControl.TabPages.Add(tabSettings);
            tabControl.TabPages.Add(tabSettings2);
            tabControl.TabPages.Add(tabTips);
            
            this.Controls.Add(tabControl);
            
            // F11 隐藏/显示
            this.KeyPreview = true;
            this.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.F11)
                {
                    this.Visible = !this.Visible;
                }
            };
        }
        
        private void CreateUpDownTab(TabPage tab)
        {
            // 上分管理
            var gbUp = new GroupBox
            {
                Text = "上分管理",
                Location = new Point(10, 10),
                Size = new Size(460, 200)
            };
            
            lblUpPlayer = new Label
            {
                Location = new Point(10, 25),
                Size = new Size(440, 25),
                Text = "暂无玩家上分"
            };
            
            var lblUpContent = new Label
            {
                Text = "喊话内容:",
                Location = new Point(10, 55),
                AutoSize = true
            };
            
            txtUpContent = new TextBox
            {
                Location = new Point(80, 52),
                Size = new Size(370, 25),
                ReadOnly = true
            };
            
            var lblUpAmount = new Label
            {
                Text = "请求上分:",
                Location = new Point(10, 85),
                AutoSize = true
            };
            
            numUpAmount = new NumericUpDown
            {
                Location = new Point(80, 82),
                Size = new Size(100, 25),
                Maximum = 10000000,
                Minimum = 0
            };
            
            var btnModifyUp = new Button
            {
                Text = "修改上分",
                Location = new Point(190, 80),
                Size = new Size(80, 28),
                FlatStyle = FlatStyle.Flat
            };
            
            btnUpApprove = new Button
            {
                Text = "@喊到",
                Location = new Point(10, 120),
                Size = new Size(80, 35),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White
            };
            btnUpApprove.FlatAppearance.BorderSize = 0;
            btnUpApprove.Click += BtnUpApprove_Click;
            
            btnUpReject = new Button
            {
                Text = "@喊没到",
                Location = new Point(100, 120),
                Size = new Size(80, 35),
                FlatStyle = FlatStyle.Flat
            };
            btnUpReject.Click += BtnUpReject_Click;
            
            btnUpIgnore = new Button
            {
                Text = "忽略",
                Location = new Point(190, 120),
                Size = new Size(80, 35),
                FlatStyle = FlatStyle.Flat
            };
            btnUpIgnore.Click += BtnUpIgnore_Click;
            
            // 玩家列表
            lvPlayers = new ListView
            {
                Location = new Point(10, 160),
                Size = new Size(440, 30),
                View = View.Details,
                FullRowSelect = true,
                HeaderStyle = ColumnHeaderStyle.Nonclickable
            };
            lvPlayers.Columns.Add("玩家", 100);
            lvPlayers.Columns.Add("昵称", 80);
            lvPlayers.Columns.Add("信息", 80);
            lvPlayers.Columns.Add("余粮", 80);
            lvPlayers.Columns.Add("次数", 60);
            
            gbUp.Controls.Add(lblUpPlayer);
            gbUp.Controls.Add(lblUpContent);
            gbUp.Controls.Add(txtUpContent);
            gbUp.Controls.Add(lblUpAmount);
            gbUp.Controls.Add(numUpAmount);
            gbUp.Controls.Add(btnModifyUp);
            gbUp.Controls.Add(btnUpApprove);
            gbUp.Controls.Add(btnUpReject);
            gbUp.Controls.Add(btnUpIgnore);
            gbUp.Controls.Add(lvPlayers);
            
            // 下分管理
            var gbDown = new GroupBox
            {
                Text = "下分管理",
                Location = new Point(10, 220),
                Size = new Size(460, 200)
            };
            
            lblDownPlayer = new Label
            {
                Location = new Point(10, 25),
                Size = new Size(440, 25),
                Text = "暂无玩家下分"
            };
            
            var lblDownContent = new Label
            {
                Text = "喊话内容:",
                Location = new Point(10, 55),
                AutoSize = true
            };
            
            txtDownContent = new TextBox
            {
                Location = new Point(80, 52),
                Size = new Size(370, 25),
                ReadOnly = true
            };
            
            var lblDownAmount = new Label
            {
                Text = "请求下分:",
                Location = new Point(10, 85),
                AutoSize = true
            };
            
            numDownAmount = new NumericUpDown
            {
                Location = new Point(80, 82),
                Size = new Size(100, 25),
                Maximum = 10000000,
                Minimum = 0
            };
            
            var lblDownBalance = new Label
            {
                Text = "余粮:",
                Location = new Point(190, 85),
                AutoSize = true
            };
            
            var btnModifyDown = new Button
            {
                Text = "修改下分",
                Location = new Point(290, 80),
                Size = new Size(80, 28),
                FlatStyle = FlatStyle.Flat
            };
            
            btnDownApprove = new Button
            {
                Text = "@喊查",
                Location = new Point(10, 120),
                Size = new Size(80, 35),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(33, 150, 243),
                ForeColor = Color.White
            };
            btnDownApprove.FlatAppearance.BorderSize = 0;
            btnDownApprove.Click += BtnDownApprove_Click;
            
            btnDownReject = new Button
            {
                Text = "@拒绝",
                Location = new Point(100, 120),
                Size = new Size(80, 35),
                FlatStyle = FlatStyle.Flat
            };
            btnDownReject.Click += BtnDownReject_Click;
            
            btnDownIgnore = new Button
            {
                Text = "忽略",
                Location = new Point(190, 120),
                Size = new Size(80, 35),
                FlatStyle = FlatStyle.Flat
            };
            btnDownIgnore.Click += BtnDownIgnore_Click;
            
            // 下分玩家列表
            var lvDownPlayers = new ListView
            {
                Location = new Point(10, 160),
                Size = new Size(440, 30),
                View = View.Details,
                FullRowSelect = true,
                HeaderStyle = ColumnHeaderStyle.Nonclickable
            };
            lvDownPlayers.Columns.Add("玩家", 100);
            lvDownPlayers.Columns.Add("昵称", 80);
            lvDownPlayers.Columns.Add("信息", 80);
            lvDownPlayers.Columns.Add("余粮", 80);
            lvDownPlayers.Columns.Add("次数", 60);
            
            gbDown.Controls.Add(lblDownPlayer);
            gbDown.Controls.Add(lblDownContent);
            gbDown.Controls.Add(txtDownContent);
            gbDown.Controls.Add(lblDownAmount);
            gbDown.Controls.Add(numDownAmount);
            gbDown.Controls.Add(lblDownBalance);
            gbDown.Controls.Add(btnModifyDown);
            gbDown.Controls.Add(btnDownApprove);
            gbDown.Controls.Add(btnDownReject);
            gbDown.Controls.Add(btnDownIgnore);
            gbDown.Controls.Add(lvDownPlayers);
            
            // 底部状态
            var pnlStatus = new Panel
            {
                Location = new Point(10, 430),
                Size = new Size(460, 30)
            };
            
            var lblPlayers = new Label
            {
                Text = "玩家人数: 0",
                Location = new Point(0, 5),
                AutoSize = true
            };
            
            var lblTotalScore = new Label
            {
                Text = "总分数: 0",
                Location = new Point(120, 5),
                AutoSize = true
            };
            
            var lblProfit = new Label
            {
                Text = "本期盈利: 0, 今天总盈利:0",
                Location = new Point(220, 5),
                AutoSize = true
            };
            
            var btnUnmute = new Button
            {
                Text = "解除禁言",
                Location = new Point(400, 0),
                Size = new Size(70, 28),
                FlatStyle = FlatStyle.Flat
            };
            
            pnlStatus.Controls.Add(lblPlayers);
            pnlStatus.Controls.Add(lblTotalScore);
            pnlStatus.Controls.Add(lblProfit);
            pnlStatus.Controls.Add(btnUnmute);
            
            tab.Controls.Add(gbUp);
            tab.Controls.Add(gbDown);
            tab.Controls.Add(pnlStatus);
        }
        
        /// <summary>
        /// 加载玩家列表
        /// </summary>
        private void LoadPlayers()
        {
            lvPlayers.Items.Clear();
            var players = _playerService.GetAllPlayers();
            
            foreach (var p in players)
            {
                var item = new ListViewItem(p.PlayerId);
                item.SubItems.Add(p.Nickname);
                item.SubItems.Add("");
                item.SubItems.Add(p.Balance.ToString());
                item.SubItems.Add("0");
                lvPlayers.Items.Add(item);
            }
        }
        
        /// <summary>
        /// 添加上分请求
        /// </summary>
        public void AddUpRequest(string playerId, string nickname, string content, int amount)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => AddUpRequest(playerId, nickname, content, amount)));
                return;
            }
            
            var request = new ScoreRequest
            {
                PlayerId = playerId,
                Nickname = nickname,
                Content = content,
                Amount = amount,
                Time = DateTime.Now,
                Type = ScoreRequestType.Up
            };
            
            _upRequests.Enqueue(request);
            
            if (_currentUpRequest == null)
            {
                ShowNextUpRequest();
            }
        }
        
        /// <summary>
        /// 添加下分请求
        /// </summary>
        public void AddDownRequest(string playerId, string nickname, string content, int amount)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => AddDownRequest(playerId, nickname, content, amount)));
                return;
            }
            
            var request = new ScoreRequest
            {
                PlayerId = playerId,
                Nickname = nickname,
                Content = content,
                Amount = amount,
                Time = DateTime.Now,
                Type = ScoreRequestType.Down
            };
            
            _downRequests.Enqueue(request);
            
            if (_currentDownRequest == null)
            {
                ShowNextDownRequest();
            }
        }
        
        private void ShowNextUpRequest()
        {
            if (_upRequests.Count > 0)
            {
                _currentUpRequest = _upRequests.Dequeue();
                lblUpPlayer.Text = $"{_currentUpRequest.Nickname} ({_currentUpRequest.PlayerId})<{_currentUpRequest.Time:HH:mm}>";
                txtUpContent.Text = _currentUpRequest.Content;
                numUpAmount.Value = _currentUpRequest.Amount;
                
                // 更新玩家列表显示
                UpdatePlayerInList(_currentUpRequest.PlayerId, _currentUpRequest.Nickname, _currentUpRequest.Amount);
            }
            else
            {
                _currentUpRequest = null;
                lblUpPlayer.Text = "暂无玩家上分";
                txtUpContent.Clear();
                numUpAmount.Value = 0;
            }
        }
        
        private void ShowNextDownRequest()
        {
            if (_downRequests.Count > 0)
            {
                _currentDownRequest = _downRequests.Dequeue();
                lblDownPlayer.Text = $"{_currentDownRequest.Nickname} ({_currentDownRequest.PlayerId})<{_currentDownRequest.Time:HH:mm}>";
                txtDownContent.Text = _currentDownRequest.Content;
                numDownAmount.Value = _currentDownRequest.Amount;
            }
            else
            {
                _currentDownRequest = null;
                lblDownPlayer.Text = "暂无玩家下分";
                txtDownContent.Clear();
                numDownAmount.Value = 0;
            }
        }
        
        private void UpdatePlayerInList(string playerId, string nickname, int amount)
        {
            foreach (ListViewItem item in lvPlayers.Items)
            {
                if (item.Text == playerId)
                {
                    item.SubItems[3].Text = amount.ToString();
                    return;
                }
            }
            
            // 如果不存在，添加新行
            var newItem = new ListViewItem(playerId);
            newItem.SubItems.Add(nickname);
            newItem.SubItems.Add("");
            newItem.SubItems.Add(amount.ToString());
            newItem.SubItems.Add("0");
            lvPlayers.Items.Add(newItem);
        }
        
        #region 上分处理
        
        private void BtnUpApprove_Click(object sender, EventArgs e)
        {
            if (_currentUpRequest == null) return;
            
            var amount = (int)numUpAmount.Value;
            var result = _playerService.AddScore(_currentUpRequest.PlayerId, amount);
            
            if (result.Success)
            {
                // 发送回复消息
                _sendMessageCallback?.Invoke(
                    _currentUpRequest.PlayerId,
                    $"@{_currentUpRequest.Nickname} ({_currentUpRequest.PlayerId.Substring(Math.Max(0, _currentUpRequest.PlayerId.Length - 4))})\n" +
                    $"上分成功！祝您大吉大利！\n" +
                    $"上分: {amount}\n" +
                    $"当前余粮: {result.Balance}"
                );
            }
            
            LoadPlayers();
            ShowNextUpRequest();
        }
        
        private void BtnUpReject_Click(object sender, EventArgs e)
        {
            if (_currentUpRequest == null) return;
            
            // 发送拒绝消息
            _sendMessageCallback?.Invoke(
                _currentUpRequest.PlayerId,
                $"@{_currentUpRequest.Nickname}\n上分请求已拒绝，请确认金额后重新申请"
            );
            
            ShowNextUpRequest();
        }
        
        private void BtnUpIgnore_Click(object sender, EventArgs e)
        {
            ShowNextUpRequest();
        }
        
        #endregion
        
        #region 下分处理
        
        private void BtnDownApprove_Click(object sender, EventArgs e)
        {
            if (_currentDownRequest == null) return;
            
            var amount = (int)numDownAmount.Value;
            var result = _playerService.DeductScore(_currentDownRequest.PlayerId, amount);
            
            if (result.Success)
            {
                // 发送回复消息
                _sendMessageCallback?.Invoke(
                    _currentDownRequest.PlayerId,
                    $"@{_currentDownRequest.Nickname}\n" +
                    $"下分成功！\n" +
                    $"下分: {amount}\n" +
                    $"当前余粮: {result.Balance}"
                );
            }
            else
            {
                MessageBox.Show(result.Message, "下分失败");
            }
            
            LoadPlayers();
            ShowNextDownRequest();
        }
        
        private void BtnDownReject_Click(object sender, EventArgs e)
        {
            if (_currentDownRequest == null) return;
            
            // 发送拒绝消息
            _sendMessageCallback?.Invoke(
                _currentDownRequest.PlayerId,
                $"@{_currentDownRequest.Nickname}\n下分请求已拒绝，请确认余额后重新申请"
            );
            
            ShowNextDownRequest();
        }
        
        private void BtnDownIgnore_Click(object sender, EventArgs e)
        {
            ShowNextDownRequest();
        }
        
        #endregion
    }
    
    /// <summary>
    /// 上下分请求
    /// </summary>
    public class ScoreRequest
    {
        public string PlayerId { get; set; }
        public string Nickname { get; set; }
        public string Content { get; set; }
        public int Amount { get; set; }
        public DateTime Time { get; set; }
        public ScoreRequestType Type { get; set; }
    }
    
    public enum ScoreRequestType
    {
        Up,
        Down
    }
}
