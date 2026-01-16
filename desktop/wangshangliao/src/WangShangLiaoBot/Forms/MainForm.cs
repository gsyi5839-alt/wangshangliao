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
    /// 
    /// This is a partial class. Methods are organized in separate files:
    /// - MainForm.Billing.cs - 账单相关方法
    /// - MainForm.Framework.cs - 框架连接相关方法
    /// - MainForm.Lottery.cs - 开奖服务相关方法
    /// - MainForm.Players.cs - 玩家管理相关方法
    /// - MainForm.RebateTool.cs - 回水工具相关方法
    /// - MainForm.Settings.cs - 设置相关方法
    /// - MainForm.Startup.cs - 启动/关闭相关方法
    /// - MainForm.UIEvents.cs - UI事件处理方法
    /// </summary>
    public partial class MainForm : Form
    {
        /// <summary>
        /// 艾特变昵称开关：用于 AutoReplyService 处理消息时，是否自动同步玩家昵称。
        /// 默认开启；会在 MainForm 构造时按 UI 勾选状态初始化，并监听勾选变化。
        /// </summary>
        public static bool EnableAtNicknameUpdate { get; private set; } = true;

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
        
        // Note: _rebateToolCtrl is defined in MainForm.Designer.cs
        
        // 缺失的 UI 控件占位符（可能在 Designer 中定义或延迟初始化）
        #pragma warning disable CS0649
        private Button btnSendImage;
        private Button btnSyncNicknames;
        #pragma warning restore CS0649
        
        // 辅助字段（供部分类使用）
        private bool _muteGroupChanging;
        private int _reconnectAttempts;
        private const int MaxReconnectAttempts = 5;
        private bool _autoMuteTriggered;
        private string _lastMutedPeriod;
        private int _totalPlayerCount;
        private const int MAX_DISPLAY_PLAYERS = 100;
        private bool _settingsControlsInitialized;
        private List<Player> filteredPlayers = new List<Player>();
        
        // Hot key 常量
        private const int WM_HOTKEY = 0x0312;
        private const int HOTKEY_ID_F10 = 1;
        private const int HOTKEY_ID_F12 = 2;
        private const int VK_F10 = 0x79;
        private const int VK_F12 = 0x7B;
        
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        
        public MainForm()
        {
            InitializeComponent();

            // 同步"支持变昵称"勾选到全局开关（供 AutoReplyService 使用）
            try
            {
                EnableAtNicknameUpdate = chkSupportNickChange?.Checked ?? true;
                if (chkSupportNickChange != null)
                {
                    chkSupportNickChange.CheckedChanged += (s, e) =>
                    {
                        EnableAtNicknameUpdate = chkSupportNickChange.Checked;
                    };
                }
            }
            catch
            {
                // ignore UI init edge cases
            }

            // ★★★ 绑定Shown事件，确保窗口显示后连接副框架 ★★★
            this.Shown += MainForm_Shown;

            InitializeEvents();
            InitializeLotteryService();
            InitializeSettingsControls();
            LoadConfig();
            LoadPlayerData();
        }
    }
}
