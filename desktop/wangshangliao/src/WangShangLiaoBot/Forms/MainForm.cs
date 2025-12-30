using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Timers;
using WangShangLiaoBot.Models;
using WangShangLiaoBot.Services;
using WangShangLiaoBot.Forms.Settings;
using WangShangLiaoBot.Controls;
using WangShangLiaoBot.Controls.BetProcess;
using WangShangLiaoBot.Controls.Odds;

namespace WangShangLiaoBot.Forms
{
    /// <summary>
    /// Main control window - Score management system
    /// </summary>
    public partial class MainForm : Form
    {
        // Score form reference
        private ScoreForm _scoreForm;

        // Player data list
        private List<Player> _players = new List<Player>();

        // Settings controls - Bill Settings tab
        private BillSendSettingsControl _billSendCtrl;
        private BillFormatSettingsControl _billFormatCtrl;
        private GroupTaskSettingsControl _groupTaskCtrl;
        private BillReplaceCommandControl _billReplaceCtrl;
        private BasicSettingsControl _basicSettingsCtrl;
        private MuteSettingsControl _muteSettingsCtrl;
        private FeedbackSettingsControl _feedbackSettingsCtrl;

        // Settings controls - Bet Process tab
        private TabControl _betProcessTabControl;
        private BetBasicSettingsControl _betBasicSettingsCtrl;
        private BetAttackRangeControl _betAttackRangeCtrl;
        
        // Settings controls - Odds tab (玩法赔率设置)
        private TabControl _oddsTabControl;
        private ClassicPlaySettingsControl _classicPlayCtrl;
        private TailBallSettingsControl _tailBallCtrl;
        private DragonTigerSettingsControl _dragonTigerCtrl;
        private ThreeArmySettingsControl _threeArmyCtrl;
        private PositionBallSettingsControl _positionBallCtrl;
        private OtherPlaySettingsControl _otherPlayCtrl;
        
        // Settings controls - Auto Reply tab (自动回复设置)
        private TabControl _autoReplyTabControl;
        private AutoReplySettingsControl _replyCtrl;
        private Controls.AutoReply.InternalReplySettingsControl _internalReplyCtrl;
        private Controls.AutoReply.VariableHelpControl _variableHelpCtrl;
        
        // Settings controls - Blacklist/Spam tab (黑名单/刷屏检测)
        private BlacklistSpamSettingsControl _blacklistSpamCtrl;
        
        // Settings controls - Card tab (名片)
        private CardSettingsControl _cardSettingsCtrl;
        
        // Settings controls - Trustee tab (托管设置)
        private TrusteeSettingsControl _trusteeSettingsCtrl;
        
        // Settings controls - Bonus tab (送分活动)
        private BonusActivitySettingsControl _bonusSettingsCtrl;
        
        // Settings controls - Shill tab (托设置)
        private ShillSettingsControl _shillSettingsCtrl;
        
        // Settings controls - Other tab (其他设置)
        private OtherSettingsControl _otherSettingsCtrl;
        
        // Settings controls - Seal Settings tabs (封盘设置)
        private Controls.SealSettings.SealTabPanel _sealPanelPC;
        private Controls.SealSettings.SealTabPanel _sealPanelCanada;
        private Controls.SealSettings.SealTabPanel _sealPanelBitcoin;
        private Controls.SealSettings.SealTabPanel _sealPanelBeijing;
        
        public MainForm()
        {
            InitializeComponent();
            InitializeEvents();
            InitializeLotteryService();
            InitializeSettingsControls();
            LoadConfig();
            LoadPlayerData();
        }
        
        /// <summary>
        /// 初始化设置页面控件
        /// </summary>
        private void InitializeSettingsControls()
        {
            // =====================================================
            // 账单设置 Tab - 使用两个面板实现自适应布局
            // =====================================================
            tabBillSettings.AutoScroll = true;
            
            // 左侧面板 - 固定宽度，可滚动
            var pnlBillLeft = new Panel();
            pnlBillLeft.Location = new System.Drawing.Point(0, 0);
            pnlBillLeft.Size = new System.Drawing.Size(340, 500);
            pnlBillLeft.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom;
            pnlBillLeft.AutoScroll = true;
            tabBillSettings.Controls.Add(pnlBillLeft);
            
            // 右侧面板 - 自适应宽度
            var pnlBillRight = new Panel();
            pnlBillRight.Location = new System.Drawing.Point(345, 0);
            pnlBillRight.Size = new System.Drawing.Size(450, 500);
            pnlBillRight.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            pnlBillRight.AutoScroll = true;
            tabBillSettings.Controls.Add(pnlBillRight);
            
            // 左侧面板控件
            _billSendCtrl = new BillSendSettingsControl();
            _billSendCtrl.Location = new System.Drawing.Point(3, 3);
            pnlBillLeft.Controls.Add(_billSendCtrl);

            _billFormatCtrl = new BillFormatSettingsControl();
            _billFormatCtrl.Location = new System.Drawing.Point(3, 58);
            pnlBillLeft.Controls.Add(_billFormatCtrl);

            _groupTaskCtrl = new GroupTaskSettingsControl();
            _groupTaskCtrl.Location = new System.Drawing.Point(3, 250);
            pnlBillLeft.Controls.Add(_groupTaskCtrl);

            _billReplaceCtrl = new BillReplaceCommandControl();
            _billReplaceCtrl.Location = new System.Drawing.Point(3, 355);
            pnlBillLeft.Controls.Add(_billReplaceCtrl);

            // 右侧面板控件 - 使用 Dock.Top 自适应宽度，按反序添加（后加的在上面）
            _feedbackSettingsCtrl = new FeedbackSettingsControl();
            _feedbackSettingsCtrl.Padding = new Padding(3, 5, 3, 5);
            pnlBillRight.Controls.Add(_feedbackSettingsCtrl);

            _muteSettingsCtrl = new MuteSettingsControl();
            _muteSettingsCtrl.Padding = new Padding(3, 5, 3, 5);
            pnlBillRight.Controls.Add(_muteSettingsCtrl);

            _basicSettingsCtrl = new BasicSettingsControl();
            _basicSettingsCtrl.Padding = new Padding(3, 5, 3, 5);
            pnlBillRight.Controls.Add(_basicSettingsCtrl);

            // =====================================================
            // 下注处理 Tab - 包含二级 TabControl
            // =====================================================
            _betProcessTabControl = new TabControl();
            _betProcessTabControl.Location = new System.Drawing.Point(0, 0);
            _betProcessTabControl.Size = new System.Drawing.Size(670, 450);
            _betProcessTabControl.Dock = DockStyle.Fill;

            // 基本设置 子标签
            var tabBasicSettings = new TabPage();
            tabBasicSettings.Text = "基本设置";
            tabBasicSettings.Padding = new Padding(3);
            _betProcessTabControl.TabPages.Add(tabBasicSettings);

            _betBasicSettingsCtrl = new BetBasicSettingsControl();
            _betBasicSettingsCtrl.Location = new System.Drawing.Point(5, 5);
            _betBasicSettingsCtrl.Dock = DockStyle.Fill;
            tabBasicSettings.Controls.Add(_betBasicSettingsCtrl);

            // 攻击范围 子标签
            var tabAttackRange = new TabPage();
            tabAttackRange.Text = "攻击范围";
            tabAttackRange.Padding = new Padding(3);
            _betProcessTabControl.TabPages.Add(tabAttackRange);

            _betAttackRangeCtrl = new BetAttackRangeControl();
            _betAttackRangeCtrl.Location = new System.Drawing.Point(5, 5);
            _betAttackRangeCtrl.Dock = DockStyle.Fill;
            tabAttackRange.Controls.Add(_betAttackRangeCtrl);

            tabBetProcess.Controls.Add(_betProcessTabControl);

            // =====================================================
            // 黑名单/刷屏检测 Tab
            // =====================================================
            tabBlacklist.AutoScroll = true;
            _blacklistSpamCtrl = new BlacklistSpamSettingsControl();
            _blacklistSpamCtrl.Dock = DockStyle.Fill;
            tabBlacklist.Controls.Add(_blacklistSpamCtrl);
            
            // =====================================================
            // 名片设置 Tab
            // =====================================================
            tabCard.AutoScroll = true;
            _cardSettingsCtrl = new CardSettingsControl();
            _cardSettingsCtrl.Dock = DockStyle.Fill;
            tabCard.Controls.Add(_cardSettingsCtrl);
            
            // =====================================================
            // 托管设置 Tab
            // =====================================================
            tabTrustee.AutoScroll = true;
            _trusteeSettingsCtrl = new TrusteeSettingsControl();
            _trusteeSettingsCtrl.Dock = DockStyle.Fill;
            tabTrustee.Controls.Add(_trusteeSettingsCtrl);
            
            // =====================================================
            // 送分活动 Tab
            // =====================================================
            tabBonus.AutoScroll = true;
            _bonusSettingsCtrl = new BonusActivitySettingsControl();
            _bonusSettingsCtrl.Dock = DockStyle.Fill;
            tabBonus.Controls.Add(_bonusSettingsCtrl);
            
            // =====================================================
            // 托设置 Tab
            // =====================================================
            tabTrusteeSettings.AutoScroll = true;
            _shillSettingsCtrl = new ShillSettingsControl();
            _shillSettingsCtrl.Dock = DockStyle.Fill;
            tabTrusteeSettings.Controls.Add(_shillSettingsCtrl);
            
            // =====================================================
            // 其他设置 Tab
            // =====================================================
            tabOther.AutoScroll = true;
            _otherSettingsCtrl = new OtherSettingsControl();
            _otherSettingsCtrl.Dock = DockStyle.Fill;
            tabOther.Controls.Add(_otherSettingsCtrl);
            
            // =====================================================
            // 玩法赔率设置 Tab - 包含二级 TabControl
            // =====================================================
            _oddsTabControl = new TabControl();
            _oddsTabControl.Location = new System.Drawing.Point(0, 0);
            _oddsTabControl.Size = new System.Drawing.Size(670, 450);
            _oddsTabControl.Dock = DockStyle.Fill;
            
            // 经典玩法 子标签
            var tabClassicPlay = new TabPage();
            tabClassicPlay.Text = "经典玩法";
            tabClassicPlay.Padding = new Padding(3);
            tabClassicPlay.AutoScroll = true;
            _oddsTabControl.TabPages.Add(tabClassicPlay);
            
            _classicPlayCtrl = new ClassicPlaySettingsControl();
            _classicPlayCtrl.Location = new System.Drawing.Point(0, 0);
            _classicPlayCtrl.Dock = DockStyle.Fill;
            tabClassicPlay.Controls.Add(_classicPlayCtrl);
            
            // 尾球玩法 子标签
            var tabTailBall = new TabPage();
            tabTailBall.Text = "尾球玩法";
            tabTailBall.Padding = new Padding(3);
            _oddsTabControl.TabPages.Add(tabTailBall);
            _tailBallCtrl = new TailBallSettingsControl();
            _tailBallCtrl.Dock = DockStyle.Fill;
            tabTailBall.Controls.Add(_tailBallCtrl);
            
            // 龙虎玩法 子标签
            var tabDragonTiger = new TabPage();
            tabDragonTiger.Text = "龙虎玩法";
            tabDragonTiger.Padding = new Padding(3);
            _oddsTabControl.TabPages.Add(tabDragonTiger);
            _dragonTigerCtrl = new DragonTigerSettingsControl();
            _dragonTigerCtrl.Dock = DockStyle.Fill;
            tabDragonTiger.Controls.Add(_dragonTigerCtrl);
            
            // 三军玩法 子标签
            var tabThreeArmy = new TabPage();
            tabThreeArmy.Text = "三军玩法";
            tabThreeArmy.Padding = new Padding(3);
            _oddsTabControl.TabPages.Add(tabThreeArmy);
            _threeArmyCtrl = new ThreeArmySettingsControl();
            _threeArmyCtrl.Dock = DockStyle.Fill;
            tabThreeArmy.Controls.Add(_threeArmyCtrl);
            
            // 定位球玩法 子标签
            var tabPositionBall = new TabPage();
            tabPositionBall.Text = "定位球玩法";
            tabPositionBall.Padding = new Padding(3);
            _oddsTabControl.TabPages.Add(tabPositionBall);
            _positionBallCtrl = new PositionBallSettingsControl();
            _positionBallCtrl.Dock = DockStyle.Fill;
            tabPositionBall.Controls.Add(_positionBallCtrl);
            
            // 其他玩法 子标签
            var tabOtherPlay = new TabPage();
            tabOtherPlay.Text = "其他玩法";
            tabOtherPlay.Padding = new Padding(3);
            _oddsTabControl.TabPages.Add(tabOtherPlay);
            _otherPlayCtrl = new OtherPlaySettingsControl();
            _otherPlayCtrl.Dock = DockStyle.Fill;
            tabOtherPlay.Controls.Add(_otherPlayCtrl);
            
            tabOdds.Controls.Add(_oddsTabControl);
            
            // =====================================================
            // 自动回复 Tab - 包含二级 TabControl
            // =====================================================
            _autoReplyTabControl = new TabControl();
            _autoReplyTabControl.Location = new System.Drawing.Point(0, 0);
            _autoReplyTabControl.Size = new System.Drawing.Size(670, 450);
            _autoReplyTabControl.Dock = DockStyle.Fill;
            
            // 回复 子标签
            var tabReply = new TabPage();
            tabReply.Text = "回复";
            tabReply.Padding = new Padding(3);
            _autoReplyTabControl.TabPages.Add(tabReply);
            _replyCtrl = new AutoReplySettingsControl();
            _replyCtrl.Dock = DockStyle.Fill;
            tabReply.Controls.Add(_replyCtrl);
            
            // 内部回复 子标签
            var tabInternalReply = new TabPage();
            tabInternalReply.Text = "内部回复";
            tabInternalReply.Padding = new Padding(3);
            _autoReplyTabControl.TabPages.Add(tabInternalReply);
            _internalReplyCtrl = new Controls.AutoReply.InternalReplySettingsControl();
            _internalReplyCtrl.Dock = DockStyle.Fill;
            tabInternalReply.Controls.Add(_internalReplyCtrl);
            
            // 变量说明 子标签
            var tabVariableHelp = new TabPage();
            tabVariableHelp.Text = "变量说明";
            tabVariableHelp.Padding = new Padding(3);
            _autoReplyTabControl.TabPages.Add(tabVariableHelp);
            _variableHelpCtrl = new Controls.AutoReply.VariableHelpControl();
            _variableHelpCtrl.Dock = DockStyle.Fill;
            tabVariableHelp.Controls.Add(_variableHelpCtrl);
            
            tabAutoReply.Controls.Add(_autoReplyTabControl);
            
            // =====================================================
            // 封盘设置 Tab - 初始化各游戏类型的封盘设置面板
            // =====================================================
            _sealPanelPC = new Controls.SealSettings.SealTabPanel("PC");
            _sealPanelPC.Dock = DockStyle.Fill;
            tabSealPC.Controls.Add(_sealPanelPC);
            
            _sealPanelCanada = new Controls.SealSettings.SealTabPanel("加拿大");
            _sealPanelCanada.Dock = DockStyle.Fill;
            tabSealCanada.Controls.Add(_sealPanelCanada);
            
            _sealPanelBitcoin = new Controls.SealSettings.SealTabPanel("比特");
            _sealPanelBitcoin.Dock = DockStyle.Fill;
            tabSealBitcoin.Controls.Add(_sealPanelBitcoin);
            
            _sealPanelBeijing = new Controls.SealSettings.SealTabPanel("北京");
            _sealPanelBeijing.Dock = DockStyle.Fill;
            tabSealBeijing.Controls.Add(_sealPanelBeijing);
        }
        
        /// <summary>
        /// Add placeholder label to a tab page
        /// </summary>
        private void AddPlaceholderToTab(TabPage tab, string text)
        {
            var lbl = new Label
            {
                Text = text,
                AutoSize = false,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Font = new System.Drawing.Font("Microsoft YaHei UI", 12F)
            };
            tab.Controls.Add(lbl);
        }
        
        /// <summary>
        /// Initialize event subscriptions
        /// </summary>
        private void InitializeEvents()
        {
            // Subscribe to ChatService events
            ChatService.Instance.OnConnectionChanged += OnConnectionChanged;
            ChatService.Instance.OnLog += OnServiceLog;
            
            // Subscribe to AutoReplyService events
            AutoReplyService.Instance.OnStatusChanged += OnAutoReplyStatusChanged;
            AutoReplyService.Instance.OnLog += OnServiceLog;
            
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

        private void RebateTool_OnOperationLogRequested(object sender, EventArgs e)
        {
            try
            {
                var logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "运行日志");
                Directory.CreateDirectory(logDir);
                var file = Path.Combine(logDir, $"{DateTime.Now:yyyy-MM-dd}.log");
                if (!File.Exists(file)) File.WriteAllText(file, "", Encoding.UTF8);

                Process.Start(new ProcessStartInfo
                {
                    FileName = "notepad.exe",
                    Arguments = $"\"{file}\"",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开操作记录失败: {ex.Message}", "回水工具", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void RebateTool_OnClearDataRequested(object sender, EventArgs e)
        {
            try
            {
                var confirm = MessageBox.Show(
                    "将执行以下清理：\n\n" +
                    "1) 删除 Data\\数据库\\Bets 下所有下注/结算文件\n" +
                    "2) 清空 Data\\设置.ini 中 Daily:* 统计缓存（不影响玩家资料/其他设置）\n\n" +
                    "确定继续吗？",
                    "确认清除回水工具数据",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (confirm != DialogResult.Yes) return;

                // 1) delete Bets folder
                var betsDir = Path.Combine(DataService.Instance.DatabaseDir, "Bets");
                if (Directory.Exists(betsDir))
                    Directory.Delete(betsDir, recursive: true);

                // 2) remove Daily:* from settings ini
                var settingsIni = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "设置.ini");
                if (File.Exists(settingsIni))
                {
                    var lines = File.ReadAllLines(settingsIni, Encoding.UTF8);
                    var kept = lines.Where(l => !l.StartsWith("Daily:", StringComparison.OrdinalIgnoreCase)).ToArray();
                    File.WriteAllLines(settingsIni, kept, Encoding.UTF8);
                }

                MessageBox.Show("清除完成。", "回水工具", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"清除失败: {ex.Message}", "回水工具", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        
        /// <summary>
        /// Initialize lottery service
        /// </summary>
        private void InitializeLotteryService()
        {
            // Subscribe to lottery service events
            LotteryService.Instance.OnResultUpdated += OnLotteryResultUpdated;
            LotteryService.Instance.OnCountdownUpdated += OnCountdownUpdated;
            LotteryService.Instance.OnError += OnLotteryError;
            
            // Start lottery service
            LotteryService.Instance.Start();
        }
        
        /// <summary>
        /// Lottery result updated callback
        /// </summary>
        private void OnLotteryResultUpdated(LotteryResult result)
        {
            if (InvokeRequired)
            {
                try { Invoke(new Action(() => OnLotteryResultUpdated(result))); }
                catch { }
                return;
            }
            
            // Update UI with new lottery result
            lblPeriodNumber.Text = result.Period;
            lblNextPeriodNumber.Text = result.NextPeriod;
            lblResult1.Text = result.Number1.ToString();
            lblResult2.Text = result.Number2.ToString();
            lblResult3.Text = result.Number3.ToString();
            lblResultSum.Text = result.Sum.ToString();
            
            lblStatus.Text = string.Format("开奖更新: {0} | {1}+{2}+{3}={4}", 
                result.Period, result.Number1, result.Number2, result.Number3, result.Sum);
        }
        
        /// <summary>
        /// Countdown updated callback
        /// </summary>
        private void OnCountdownUpdated(int countdown)
        {
            if (InvokeRequired)
            {
                try { Invoke(new Action(() => OnCountdownUpdated(countdown))); }
                catch { }
                return;
            }
            
            lblCountdown.Text = countdown.ToString();
            
            // Change color based on countdown value
            if (countdown <= 10)
                lblCountdown.ForeColor = Color.Red;
            else
                lblCountdown.ForeColor = Color.Black;
        }
        
        /// <summary>
        /// Lottery error callback
        /// </summary>
        private void OnLotteryError(string error)
        {
            if (InvokeRequired)
            {
                try { Invoke(new Action(() => OnLotteryError(error))); }
                catch { }
                return;
            }
            
            lblStatus.Text = "开奖接口错误: " + error;
        }
        
        /// <summary>
        /// Load configuration
        /// </summary>
        private void LoadConfig()
        {
            var config = ConfigService.Instance.Config;
            
            // 加载设置控件的值
            _billSendCtrl?.LoadSettings();
            _billFormatCtrl?.LoadSettings();
            _groupTaskCtrl?.LoadSettings();
            _basicSettingsCtrl?.LoadSettings();
            _muteSettingsCtrl?.LoadSettings();
            _feedbackSettingsCtrl?.LoadSettings();
        }
        
        /// <summary>
        /// Load player data from cache
        /// </summary>
        private void LoadPlayerData()
        {
            _players = DataService.Instance.GetAllPlayers();
            RefreshPlayerList();
        }
        
        /// <summary>
        /// Refresh player list view
        /// </summary>
        private void RefreshPlayerList()
        {
            listPlayers.Items.Clear();
            
            var displayPlayers = chkShowTuoPlayer.Checked 
                ? _players 
                : _players.Where(p => !p.IsTuo).ToList();
            
            foreach (var player in displayPlayers)
            {
                var item = new ListViewItem(player.WangWangId);
                item.SubItems.Add(player.Nickname);
                item.SubItems.Add(player.Score.ToString());
                item.SubItems.Add(player.ReservedScore.ToString());
                item.SubItems.Add(player.Remark ?? ""); // Bet content
                item.SubItems.Add(player.LastActiveTime.ToString("HH:mm"));
                item.Tag = player;
                listPlayers.Items.Add(item);
            }
            
            lblStatus.Text = string.Format("共 {0} 个玩家", displayPlayers.Count);
        }
        
        /// <summary>
        /// Connection status changed
        /// </summary>
        private void OnConnectionChanged(bool connected)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnConnectionChanged(connected)));
                return;
            }
            
            if (connected)
            {
                lblConnectionStatus.Text = "已连框架";
                lblConnectionStatus.ForeColor = Color.Green;
                
                // Auto-start services when connected
                try
                {
                    // Start run log service
                    Services.RunLogService.Instance.Start();
                    Logger.Info("[MainForm] RunLogService started on connection");
                    
                    // Start spam detection service
                    Services.Spam.SpamDetectionService.Instance.Start();
                    Logger.Info("[MainForm] SpamDetectionService started on connection");
                    
                    // Start bet services
                    Services.Betting.BetLedgerService.Instance.Start();
                    Services.Betting.BetSettlementService.Instance.Start();
                    Logger.Info("[MainForm] Bet services started on connection");
                }
                catch (Exception ex)
                {
                    Logger.Error($"[MainForm] Failed to start services: {ex.Message}");
                }
            }
            else
            {
                lblConnectionStatus.Text = "未连框架";
                lblConnectionStatus.ForeColor = Color.Red;
                
                // Stop services when disconnected
                try
                {
                    Services.RunLogService.Instance.Stop();
                    Services.Spam.SpamDetectionService.Instance.Stop();
                    Services.Betting.BetLedgerService.Instance.Stop();
                    Services.Betting.BetSettlementService.Instance.Stop();
                }
                catch { }
            }
        }
        
        /// <summary>
        /// Click connection status label to toggle connect/disconnect
        /// </summary>
        private async void lblConnectionStatus_Click(object sender, EventArgs e)
        {
            if (ChatService.Instance.IsConnected)
            {
                // Disconnect
                lblConnectionStatus.Text = "断开中...";
                lblConnectionStatus.ForeColor = Color.Orange;
                lblStatus.Text = "正在断开连接...";
                
                await ChatService.Instance.DisconnectAsync();
                
                MessageBox.Show("已断开与旺商聊的连接", "断开连接", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                // Connect
                lblConnectionStatus.Text = "连接中...";
                lblConnectionStatus.ForeColor = Color.Orange;
                lblStatus.Text = "正在连接旺商聊，请在旺商聊中登录...";
                
                var success = await ChatService.Instance.LaunchAndConnectAsync();
                
                if (!success)
                {
                    lblConnectionStatus.Text = "未连框架";
                    lblConnectionStatus.ForeColor = Color.Red;
                    
                    if (ChatService.Instance.IsRunningWithoutDebug)
                    {
                        // 旺商聊已运行但没有调试端口，提供重启选项
                        var result = MessageBox.Show(
                            "旺商聊已运行，但未开启调试端口。\n\n" +
                            "需要以调试模式重启旺商聊才能连接。\n" +
                            "（会自动关闭并重新打开旺商聊，登录状态会保留）\n\n" +
                            "是否立即重启？",
                            "需要重启旺商聊",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question);
                        
                        if (result == DialogResult.Yes)
                        {
                            lblConnectionStatus.Text = "重启中...";
                            lblConnectionStatus.ForeColor = Color.Orange;
                            lblStatus.Text = "正在重启旺商聊...";
                            
                            success = await ChatService.Instance.ForceRestartAndConnectAsync();
                            
                            if (success)
                            {
                                lblStatus.Text = "连接成功！";
                            }
                            else
                            {
                                lblConnectionStatus.Text = "未连框架";
                                lblConnectionStatus.ForeColor = Color.Red;
                                lblStatus.Text = "重启后连接失败";
                                MessageBox.Show("重启后仍无法连接，请检查旺商聊是否正常", "连接失败",
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return;
                            }
                        }
                        else
                        {
                            lblStatus.Text = "已取消连接";
                            return;
                        }
                    }
                    else
                    {
                        lblStatus.Text = "连接失败";
                        MessageBox.Show("请先启动旺商聊并登录", "连接失败", 
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                }
                
                // If connected successfully, show confirmation and get account info
                if (success && ChatService.Instance.IsConnected)
                {
                    // 显示连接模式
                    var modeText = ChatService.Instance.Mode == ConnectionMode.CDP ? "CDP模式" : "UI自动化模式";
                    lblStatus.Text = "连接成功！(" + modeText + ") 正在获取账号...";
                    
                    string myAccount = null;
                    
                    // CDP 模式下尝试获取账号（等待几秒让用户登录）
                    if (ChatService.Instance.Mode == ConnectionMode.CDP)
                    {
                        // 尝试获取账号，最多等待10秒
                        for (int i = 0; i < 10; i++)
                        {
                            myAccount = await ChatService.Instance.GetMyAccountAsync();
                            if (!string.IsNullOrEmpty(myAccount))
                            {
                                break;
                            }
                            lblStatus.Text = string.Format("等待旺商聊登录... ({0}/10秒)", i + 1);
                            await Task.Delay(1000);
                        }
                    }
                    
                    var msg = "✓ 已成功连接旺商聊！\n\n";
                    msg += "连接模式: " + modeText + "\n";
                    
                    if (!string.IsNullOrEmpty(myAccount))
                    {
                        // Save to config
                        ConfigService.Instance.Config.AdminWangWangId = myAccount;
                        ConfigService.Instance.SaveConfig();
                        
                        msg += "我的旺商号: " + myAccount;
                        lblStatus.Text = "已连接 (" + modeText + ") - 账号: " + myAccount;
                    }
                    else
                    {
                        msg += "提示: 请确保旺商聊已登录\n";
                        msg += "如未登录，请先在旺商聊中登录后重新连接";
                        lblStatus.Text = "已连接 (" + modeText + ")";
                    }
                    
                    MessageBox.Show(msg, "连接成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
        
        /// <summary>
        /// Auto reply status changed
        /// </summary>
        private void OnAutoReplyStatusChanged(bool running)
        {
            // Update UI if needed
        }
        
        /// <summary>
        /// Service log event
        /// </summary>
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
        
        // ===== Button Event Handlers =====
        
        /// <summary>
        /// Open score window
        /// </summary>
        private void btnScoreWindow_Click(object sender, EventArgs e)
        {
            if (_scoreForm == null || _scoreForm.IsDisposed)
            {
                _scoreForm = new ScoreForm();
            }
            _scoreForm.Show();
            _scoreForm.BringToFront();
        }
        
        /// <summary>
        /// Open run log window
        /// </summary>
        private void menuRunLog_Click(object sender, EventArgs e)
        {
            using (var form = new Form())
            {
                form.Text = "运行日志";
                form.Size = new Size(700, 500);
                form.StartPosition = FormStartPosition.CenterParent;
                
                var logCtrl = new RunLogControl();
                logCtrl.Dock = DockStyle.Fill;
                form.Controls.Add(logCtrl);
                
                form.ShowDialog(this);
            }
        }
        
        /// <summary>
        /// Open account list
        /// </summary>
        private void menuAccountList_Click(object sender, EventArgs e)
        {
            using (var form = new AccountListForm())
            {
                form.ShowDialog(this);
            }
        }
        
        /// <summary>
        /// Open system settings window
        /// </summary>
        private void menuSystemSettings_Click(object sender, EventArgs e)
        {
            using (var form = new Form())
            {
                form.Text = "系统设置";
                form.Size = new Size(620, 400);
                form.StartPosition = FormStartPosition.CenterParent;
                
                var settingsCtrl = new SystemSettingsControl();
                settingsCtrl.Dock = DockStyle.Fill;
                form.Controls.Add(settingsCtrl);
                
                form.ShowDialog(this);
            }
        }
        
        /// <summary>
        /// View account info
        /// </summary>
        private void btnViewAccount_Click(object sender, EventArgs e)
        {
            var config = ConfigService.Instance.Config;
            var info = string.Format(
                "管理旺旺号: {0}\n" +
                "绑定群号: {1}\n" +
                "绑定群名: {2}\n" +
                "软件路径: {3}",
                config.AdminWangWangId,
                config.GroupId,
                config.GroupName,
                config.WangShangLiaoPath);
            MessageBox.Show(info, "账号信息", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        
        /// <summary>
        /// Test connection - send a test message
        /// </summary>
        private async void menuTestConnection_Click(object sender, EventArgs e)
        {
            if (!ChatService.Instance.IsConnected)
            {
                MessageBox.Show("请先连接旺商聊！\n\n点击左上角的\"已连框架\"按钮进行连接。", 
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
        
        /// <summary>
        /// Refresh lottery result - Manual refresh from API
        /// </summary>
        private void btnRefreshLottery_Click(object sender, EventArgs e)
        {
            lblStatus.Text = "正在刷新开奖数据...";
            LotteryService.Instance.Refresh();
        }
        
        /// <summary>
        /// Manual calculation
        /// </summary>
        private void btnManualCalc_Click(object sender, EventArgs e)
        {
            MessageBox.Show("手动算账功能", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        
        /// <summary>
        /// Mute all group members - 全体禁言
        /// Uses the group account from CurrentAccount in AccountService
        /// </summary>
        private async void btnMuteAll_Click(object sender, EventArgs e)
        {
            lblStatus.Text = "正在执行全体禁言...";
            
            if (!ChatService.Instance.IsConnected)
            {
                MessageBox.Show("请先连接旺商聊！\n\n提示：点击[已连框架]后再操作", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            // Get group account from current account in AccountService
            var currentAccount = AccountService.Instance.CurrentAccount;
            string groupAccount = currentAccount?.GroupId;
            
            // Debug log
            Logger.Info($"[全体禁言] CurrentAccount: {(currentAccount != null ? currentAccount.Nickname : "null")}, GroupId: {groupAccount ?? "null"}");
            
            // Fallback: if no current account, try to get from first account with GroupId
            if (string.IsNullOrEmpty(groupAccount))
            {
                var firstAccount = AccountService.Instance.Accounts?.Find(a => !string.IsNullOrEmpty(a.GroupId));
                if (firstAccount != null)
                {
                    groupAccount = firstAccount.GroupId;
                    Logger.Info($"[全体禁言] Using fallback account: {firstAccount.Nickname}, GroupId: {groupAccount}");
                }
            }
            
            // Confirm with user, showing which group will be muted
            var confirmMsg = string.IsNullOrEmpty(groupAccount) 
                ? "确定要开启全体禁言吗？\n\n将使用当前旺商聊会话的群聊\n\n注意：\n1. 需要群主权限" 
                : $"确定要开启全体禁言吗？\n\n目标群号: {groupAccount}\n\n注意：\n1. 需要群主权限";
            
            var result = MessageBox.Show(confirmMsg, "全体禁言", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            
            if (result == DialogResult.Yes)
            {
                btnMuteAll.Enabled = false;
                btnMuteAll.Text = "禁言中...";
                
                try
                {
                    // Pass groupAccount to MuteAllAsync
                    var (success, groupName, message) = await ChatService.Instance.MuteAllAsync(groupAccount);
                    if (success)
                    {
                        var successMsg = string.IsNullOrEmpty(groupName) 
                            ? "全体禁言已开启！" 
                            : $"全体禁言已开启！\n\n群名: {groupName}";
                        MessageBox.Show(successMsg, "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        lblStatus.Text = $"已开启全体禁言: {groupName ?? groupAccount ?? "当前群"}";
                    }
                    else
                    {
                        MessageBox.Show($"全体禁言失败！\n\n原因: {message}\n\n可能原因：\n1. 需要群主权限\n2. 群号不正确", "失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        lblStatus.Text = $"全体禁言失败: {message}";
                    }

                    // =====================================================
                    // Enable real-time message capture via NIM hook (CDP only)
                    // This is required for "group chat produced bet ledgers".
                    // =====================================================
                    if (ChatService.Instance.Mode == ConnectionMode.CDP)
                    {
                        try
                        {
                            await ChatService.Instance.InstallMessageHookAsync();
                            ChatService.Instance.StartHookedMessagePolling(1000);
                        }
                        catch { }
                    }

                    // Start run log service
                    try
                    {
                        Services.RunLogService.Instance.Start();
                    }
                    catch { }

                    // Start bet pipeline (capture + settlement)
                    try
                    {
                        Services.Betting.BetLedgerService.Instance.Start();
                        Services.Betting.BetSettlementService.Instance.Start();
                    }
                    catch { }

                    // Start spam detection pipeline (blacklist / spam rules)
                    try
                    {
                        Services.Spam.SpamDetectionService.Instance.Start();
                    }
                    catch { }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"全体禁言异常: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    lblStatus.Text = $"全体禁言异常: {ex.Message}";
                }
                finally
                {
                    btnMuteAll.Enabled = true;
                    btnMuteAll.Text = "全体禁言";
                }
            }
        }
        
        /// <summary>
        /// Unmute all group members - 全体解禁
        /// Uses the group account from CurrentAccount in AccountService
        /// </summary>
        private async void btnUnmuteAll_Click(object sender, EventArgs e)
        {
            lblStatus.Text = "正在执行全体解禁...";
            
            if (!ChatService.Instance.IsConnected)
            {
                MessageBox.Show("请先连接旺商聊！\n\n提示：点击[已连框架]后再操作", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            // Get group account from current account in AccountService
            var currentAccount = AccountService.Instance.CurrentAccount;
            string groupAccount = currentAccount?.GroupId;
            
            // Debug log
            Logger.Info($"[全体解禁] CurrentAccount: {(currentAccount != null ? currentAccount.Nickname : "null")}, GroupId: {groupAccount ?? "null"}");
            
            // Fallback: if no current account, try to get from first account with GroupId
            if (string.IsNullOrEmpty(groupAccount))
            {
                var firstAccount = AccountService.Instance.Accounts?.Find(a => !string.IsNullOrEmpty(a.GroupId));
                if (firstAccount != null)
                {
                    groupAccount = firstAccount.GroupId;
                    Logger.Info($"[全体解禁] Using fallback account: {firstAccount.Nickname}, GroupId: {groupAccount}");
                }
            }
            
            // Confirm with user, showing which group will be unmuted
            var confirmMsg = string.IsNullOrEmpty(groupAccount) 
                ? "确定要解除全体禁言吗？\n\n将使用当前旺商聊会话的群聊\n\n注意：\n1. 需要群主权限" 
                : $"确定要解除全体禁言吗？\n\n目标群号: {groupAccount}\n\n注意：\n1. 需要群主权限";
            
            var result = MessageBox.Show(confirmMsg, "全体解禁", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            
            if (result == DialogResult.Yes)
            {
                btnUnmuteAll.Enabled = false;
                btnUnmuteAll.Text = "解禁中...";
                
                try
                {
                    // Pass groupAccount to UnmuteAllAsync
                    var (success, groupName, message) = await ChatService.Instance.UnmuteAllAsync(groupAccount);
                    if (success)
                    {
                        var successMsg = string.IsNullOrEmpty(groupName) 
                            ? "全体禁言已解除！" 
                            : $"全体禁言已解除！\n\n群名: {groupName}";
                        MessageBox.Show(successMsg, "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        lblStatus.Text = $"已解除全体禁言: {groupName ?? groupAccount ?? "当前群"}";
                    }
                    else
                    {
                        MessageBox.Show($"全体解禁失败！\n\n原因: {message}\n\n可能原因：\n1. 需要群主权限\n2. 群号不正确", "失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        lblStatus.Text = $"全体解禁失败: {message}";
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"全体解禁异常: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    lblStatus.Text = $"全体解禁异常: {ex.Message}";
                }
                finally
                {
                    btnUnmuteAll.Enabled = true;
                    btnUnmuteAll.Text = "全体解禁";
                }
            }
        }
        
        /// <summary>
        /// Open bill export dialog
        /// </summary>
        private void btnExportBill_Click(object sender, EventArgs e)
        {
            using (var form = new BillExportForm())
            {
                form.ShowDialog(this);
                // Refresh player list after import
                LoadPlayerData();
            }
        }
        
        /// <summary>
        /// Modify player info
        /// </summary>
        private void btnModifyInfo_Click(object sender, EventArgs e)
        {
            var wangwangId = txtWangWangId.Text.Trim();
            var nickname = txtNickname.Text.Trim();
            var scoreText = txtScore.Text.Trim();
            
            if (string.IsNullOrEmpty(wangwangId))
            {
                MessageBox.Show("请输入旺旺号", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            decimal score;
            if (!decimal.TryParse(scoreText, out score))
            {
                score = 0;
            }
            
            // Find or create player
            var player = _players.FirstOrDefault(p => p.WangWangId == wangwangId);
            if (player == null)
            {
                player = new Player 
                { 
                    WangWangId = wangwangId,
                    LastActiveTime = DateTime.Now
                };
                _players.Add(player);
            }
            
            player.Nickname = nickname;
            player.Score = score;
            player.LastActiveTime = DateTime.Now;
            
            // Save to file
            DataService.Instance.SavePlayer(player);
            RefreshPlayerList();
            
            MessageBox.Show("玩家信息已更新", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        
        /// <summary>
        /// Search player
        /// </summary>
        private void btnSearchPlayer_Click(object sender, EventArgs e)
        {
            var searchText = txtWangWangId.Text.Trim();
            if (string.IsNullOrEmpty(searchText))
            {
                RefreshPlayerList();
                return;
            }
            
            var found = _players.Where(p => 
                p.WangWangId.Contains(searchText) || 
                (p.Nickname != null && p.Nickname.Contains(searchText))).ToList();
            
            listPlayers.Items.Clear();
            foreach (var player in found)
            {
                var item = new ListViewItem(player.WangWangId);
                item.SubItems.Add(player.Nickname);
                item.SubItems.Add(player.Score.ToString());
                item.SubItems.Add(player.ReservedScore.ToString());
                item.SubItems.Add(player.Remark ?? "");
                item.SubItems.Add(player.LastActiveTime.ToString("HH:mm"));
                item.Tag = player;
                listPlayers.Items.Add(item);
            }
            
            lblStatus.Text = string.Format("搜索到 {0} 个玩家", found.Count);
        }
        
        /// <summary>
        /// Player list selection changed
        /// </summary>
        private void listPlayers_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listPlayers.SelectedItems.Count > 0)
            {
                var player = listPlayers.SelectedItems[0].Tag as Player;
                if (player != null)
                {
                    txtWangWangId.Text = player.WangWangId;
                    txtNickname.Text = player.Nickname;
                    txtScore.Text = player.Score.ToString();
                }
            }
        }
        
        /// <summary>
        /// Open chat log
        /// </summary>
        private void btnChatLog_Click(object sender, EventArgs e)
        {
            // Open chat log directory
            var logDir = DataService.Instance.MessageLogDir;
            if (Directory.Exists(logDir))
            {
                System.Diagnostics.Process.Start("explorer.exe", logDir);
            }
            else
            {
                MessageBox.Show("日志目录不存在", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        
        /// <summary>
        /// F10 key to toggle window visibility
        /// </summary>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.F10)
            {
                this.Visible = !this.Visible;
                return true;
            }
            if (keyData == Keys.F11)
            {
                btnScoreWindow_Click(null, null);
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
        
        /// <summary>
        /// 算账设置菜单点击事件 - 切换到设置视图
        /// </summary>
        private void menuScoreSettings_Click(object sender, EventArgs e)
        {
            // 显示设置界面
            ShowSettingsView();
        }
        
        /// <summary>
        /// 封盘设置菜单点击事件 - 切换到封盘设置视图
        /// </summary>
        private void menuLockSettings_Click(object sender, EventArgs e)
        {
            ShowSealSettingsView();
        }
        
        /// <summary>
        /// 回水工具菜单点击事件 - 切换到回水工具视图
        /// </summary>
        private void menuRebateTools_Click(object sender, EventArgs e)
        {
            ShowRebateToolView();
        }
        
        /// <summary>
        /// 显示回水工具视图（不弹窗，切换页面）
        /// </summary>
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
        
        /// <summary>
        /// 显示封盘设置视图
        /// </summary>
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
        
        /// <summary>
        /// 客户管理菜单点击事件 - 切换回主界面
        /// </summary>
        private void menuCustomer_Click(object sender, EventArgs e)
        {
            // 显示客户管理界面
            ShowCustomerView();
        }
        
        /// <summary>
        /// 显示客户管理视图
        /// </summary>
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
        
        /// <summary>
        /// 显示设置视图
        /// </summary>
        private void ShowSettingsView()
        {
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
        
        /// <summary>
        /// 窗口大小改变事件 - 自动调整内容区域
        /// </summary>
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            
            // 计算内容区域大小
            int contentWidth = this.ClientSize.Width;
            int contentHeight = this.ClientSize.Height - menuStrip.Height - statusStrip.Height;
            
            // 调整设置页面大小
            if (tabSettings.Visible)
            {
                tabSettings.Size = new System.Drawing.Size(contentWidth, contentHeight);
            }
            
            // 调整封盘设置页面大小
            if (tabSealSettings.Visible)
            {
                tabSealSettings.Size = new System.Drawing.Size(contentWidth, contentHeight);
            }
            
            // 调整回水工具页面大小
            if (pnlRebateTool.Visible)
            {
                pnlRebateTool.Size = new System.Drawing.Size(contentWidth, contentHeight);
            }
        }
        
        /// <summary>
        /// Form closing event
        /// </summary>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Stop lottery service
            LotteryService.Instance.Stop();
            
            AutoReplyService.Instance.Stop();
            // IMPORTANT: Do not block UI thread on async calls (can cause freeze on exit).
            // Fire-and-forget disconnect because the app is closing anyway.
            try
            {
                System.Threading.Tasks.Task.Run(async () =>
                {
                    try { await ChatService.Instance.DisconnectAsync(); } catch { /* ignore on shutdown */ }
                });
            }
            catch
            {
                // ignore
            }
            base.OnFormClosing(e);
        }
    }
}
