using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Timers;

namespace WangShangLiaoBot.Services
{
    /// <summary>
    /// 封盘定时任务服务 - 基于招财狗(ZCG)的封盘系统
    /// 支持多种彩种的封盘提醒、封盘、发送规则功能
    /// </summary>
    public sealed class SealingService : IDisposable
    {
        private static SealingService _instance;
        public static SealingService Instance => _instance ?? (_instance = new SealingService());

        private SealingConfig _config;
        private readonly object _lock = new object();

        // 定时器
        private Timer _mainTimer;
        private bool _isRunning;
        private string _currentPeriod;
        private DateTime _nextDrawTime;
        private SealingState _currentState = SealingState.Accepting;

        // 事件
        public event Action<string, string> OnSendMessage; // teamId, message
        public event Action<string> OnMuteGroup;           // teamId
        public event Action<string, string> OnPeriodChange; // oldPeriod, newPeriod
        #pragma warning disable CS0067 // 预留接口
        public event Action<string> OnUnmuteGroup;         // teamId - 预留，解禁时触发
        public event Action<string, int> OnRemind;         // period, secondsRemaining - 预留，提醒时触发
        #pragma warning restore CS0067

        private SealingService()
        {
            LoadConfig();
            InitTimer();
        }

        private string ConfigPath => Path.Combine(DataService.Instance.DatabaseDir, "sealing-config.ini");

        #region 配置管理

        public SealingConfig GetConfig()
        {
            lock (_lock)
            {
                return _config ?? SealingConfig.CreateDefault();
            }
        }

        public void SaveConfig(SealingConfig config)
        {
            lock (_lock)
            {
                _config = config;
                SaveConfigToFile(config);
            }
        }

        private void LoadConfig()
        {
            try
            {
                if (!File.Exists(ConfigPath))
                {
                    _config = SealingConfig.CreateDefault();
                    return;
                }

                var config = new SealingConfig();
                var lines = File.ReadAllLines(ConfigPath, Encoding.UTF8);

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;
                    var idx = line.IndexOf('=');
                    if (idx <= 0) continue;

                    var key = line.Substring(0, idx).Trim();
                    var value = line.Substring(idx + 1).Trim();

                    ParseConfigLine(config, key, value);
                }

                _config = config;
            }
            catch
            {
                _config = SealingConfig.CreateDefault();
            }
        }

        private void ParseConfigLine(SealingConfig config, string key, string value)
        {
            switch (key)
            {
                case "LotteryType":
                    if (int.TryParse(value, out var lt)) config.LotteryType = (LotteryType)lt;
                    break;
                case "DrawIntervalSeconds":
                    if (int.TryParse(value, out var di)) config.DrawIntervalSeconds = di;
                    break;
                case "ReminderEnabled":
                    config.ReminderEnabled = value.ToLower() == "true" || value == "1" || value == "真";
                    break;
                case "ReminderSeconds":
                    if (int.TryParse(value, out var rs)) config.ReminderSeconds = rs;
                    break;
                case "ReminderContent":
                    config.ReminderContent = value;
                    break;
                case "SealingSeconds":
                    if (int.TryParse(value, out var ss)) config.SealingSeconds = ss;
                    break;
                case "SealingContent":
                    config.SealingContent = value;
                    break;
                case "RuleEnabled":
                    config.RuleEnabled = value.ToLower() == "true" || value == "1" || value == "真";
                    break;
                case "RuleSeconds":
                    if (int.TryParse(value, out var rus)) config.RuleSeconds = rus;
                    break;
                case "RuleContent":
                    config.RuleContent = value;
                    break;
                case "MuteBeforeSeconds":
                    if (int.TryParse(value, out var mbs)) config.MuteBeforeSeconds = mbs;
                    break;
                case "AutoMute":
                    config.AutoMute = value.ToLower() == "true" || value == "1" || value == "真";
                    break;
                case "TeamId":
                    config.TeamId = value;
                    break;
            }
        }

        private void SaveConfigToFile(SealingConfig config)
        {
            try
            {
                Directory.CreateDirectory(DataService.Instance.DatabaseDir);
                var sb = new StringBuilder();
                sb.AppendLine("# 封盘配置 - 自动生成");
                sb.AppendLine($"LotteryType={(int)config.LotteryType}");
                sb.AppendLine($"DrawIntervalSeconds={config.DrawIntervalSeconds}");
                sb.AppendLine($"ReminderEnabled={config.ReminderEnabled}");
                sb.AppendLine($"ReminderSeconds={config.ReminderSeconds}");
                sb.AppendLine($"ReminderContent={config.ReminderContent}");
                sb.AppendLine($"SealingSeconds={config.SealingSeconds}");
                sb.AppendLine($"SealingContent={config.SealingContent}");
                sb.AppendLine($"RuleEnabled={config.RuleEnabled}");
                sb.AppendLine($"RuleSeconds={config.RuleSeconds}");
                sb.AppendLine($"RuleContent={config.RuleContent}");
                sb.AppendLine($"MuteBeforeSeconds={config.MuteBeforeSeconds}");
                sb.AppendLine($"AutoMute={config.AutoMute}");
                sb.AppendLine($"TeamId={config.TeamId}");

                File.WriteAllText(ConfigPath, sb.ToString(), Encoding.UTF8);
            }
            catch { }
        }

        #endregion

        #region 定时任务

        private void InitTimer()
        {
            _mainTimer = new Timer(1000); // 每秒检查一次
            _mainTimer.Elapsed += OnTimerTick;
            _mainTimer.AutoReset = true;
        }

        /// <summary>
        /// 启动封盘服务
        /// </summary>
        public void Start(string currentPeriod, DateTime nextDrawTime)
        {
            lock (_lock)
            {
                _currentPeriod = currentPeriod;
                _nextDrawTime = nextDrawTime;
                _currentState = SealingState.Accepting;
                _isRunning = true;
                _mainTimer.Start();

                Logger.Info($"[封盘服务] 启动 - 当前期:{currentPeriod}, 开奖时间:{nextDrawTime:HH:mm:ss}");
            }
        }

        /// <summary>
        /// 停止封盘服务
        /// </summary>
        public void Stop()
        {
            lock (_lock)
            {
                _isRunning = false;
                _mainTimer.Stop();
                Logger.Info("[封盘服务] 已停止");
            }
        }

        /// <summary>
        /// 更新期号和开奖时间
        /// </summary>
        public void UpdatePeriod(string newPeriod, DateTime newDrawTime)
        {
            lock (_lock)
            {
                var oldPeriod = _currentPeriod;
                _currentPeriod = newPeriod;
                _nextDrawTime = newDrawTime;
                _currentState = SealingState.Accepting;

                OnPeriodChange?.Invoke(oldPeriod, newPeriod);
                Logger.Info($"[封盘服务] 期号更新:{oldPeriod} -> {newPeriod}, 开奖时间:{newDrawTime:HH:mm:ss}");
            }
        }

        private void OnTimerTick(object sender, ElapsedEventArgs e)
        {
            if (!_isRunning) return;

            try
            {
                var config = GetConfig();
                var now = DateTime.Now;
                var secondsToNext = (int)(_nextDrawTime - now).TotalSeconds;

                // 状态机处理
                switch (_currentState)
                {
                    case SealingState.Accepting:
                        HandleAcceptingState(config, secondsToNext);
                        break;

                    case SealingState.Reminded:
                        HandleRemindedState(config, secondsToNext);
                        break;

                    case SealingState.Sealed:
                        HandleSealedState(config, secondsToNext);
                        break;

                    case SealingState.RuleSent:
                        HandleRuleSentState(config, secondsToNext);
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[封盘服务] 定时器异常: {ex.Message}");
            }
        }

        private void HandleAcceptingState(SealingConfig config, int secondsToNext)
        {
            // 检查是否需要发送提醒
            if (config.ReminderEnabled && secondsToNext <= config.ReminderSeconds && secondsToNext > config.SealingSeconds)
            {
                SendReminder(config, secondsToNext);
                _currentState = SealingState.Reminded;
            }
            // 直接进入封盘
            else if (secondsToNext <= config.SealingSeconds)
            {
                SendSealing(config);
                _currentState = SealingState.Sealed;
            }
        }

        private void HandleRemindedState(SealingConfig config, int secondsToNext)
        {
            // 检查是否需要封盘
            if (secondsToNext <= config.SealingSeconds)
            {
                SendSealing(config);
                _currentState = SealingState.Sealed;
            }
        }

        private void HandleSealedState(SealingConfig config, int secondsToNext)
        {
            // 检查是否需要禁言
            if (config.AutoMute && secondsToNext <= config.MuteBeforeSeconds)
            {
                OnMuteGroup?.Invoke(config.TeamId);
            }

            // 检查是否需要发送规则
            if (config.RuleEnabled && secondsToNext <= config.RuleSeconds)
            {
                SendRule(config);
                _currentState = SealingState.RuleSent;
            }
        }

        private void HandleRuleSentState(SealingConfig config, int secondsToNext)
        {
            // 等待开奖
            if (secondsToNext <= 0)
            {
                _currentState = SealingState.WaitingResult;
            }
        }

        private void SendReminder(SealingConfig config, int secondsToNext)
        {
            if (string.IsNullOrEmpty(config.TeamId)) return;

            var content = config.ReminderContent;
            content = content.Replace("[封盘倒计时]", secondsToNext.ToString());
            content = content.Replace("[期数]", _currentPeriod);

            OnSendMessage?.Invoke(config.TeamId, content);
            Logger.Info($"[封盘服务] 发送提醒 - 剩余{secondsToNext}秒");
        }

        private void SendSealing(SealingConfig config)
        {
            if (string.IsNullOrEmpty(config.TeamId)) return;

            var content = config.SealingContent;
            content = content.Replace("[期数]", _currentPeriod);

            OnSendMessage?.Invoke(config.TeamId, content);
            Logger.Info($"[封盘服务] 发送封盘线 - 第{_currentPeriod}期");
        }

        private void SendRule(SealingConfig config)
        {
            if (string.IsNullOrEmpty(config.TeamId)) return;

            var content = config.RuleContent;
            content = content.Replace("[期数]", _currentPeriod);

            OnSendMessage?.Invoke(config.TeamId, content);
            Logger.Info($"[封盘服务] 发送规则 - 第{_currentPeriod}期");
        }

        #endregion

        #region 状态查询

        /// <summary>
        /// 获取当前状态
        /// </summary>
        public SealingState GetCurrentState()
        {
            lock (_lock)
            {
                return _currentState;
            }
        }

        /// <summary>
        /// 是否正在封盘中
        /// </summary>
        public bool IsSealed()
        {
            lock (_lock)
            {
                return _currentState >= SealingState.Sealed;
            }
        }

        /// <summary>
        /// 获取距离开奖的秒数
        /// </summary>
        public int GetSecondsToNext()
        {
            lock (_lock)
            {
                return (int)(_nextDrawTime - DateTime.Now).TotalSeconds;
            }
        }

        /// <summary>
        /// 获取当前期号
        /// </summary>
        public string GetCurrentPeriod()
        {
            lock (_lock)
            {
                return _currentPeriod;
            }
        }

        /// <summary>
        /// 计算指定时间的期号
        /// </summary>
        public static string CalculatePeriodNumber(LotteryType lotteryType, DateTime time)
        {
            // 加拿大28 每天第一期从 00:00:00 开始
            var dayStart = time.Date;
            var elapsed = time - dayStart;
            
            int interval;
            switch (lotteryType)
            {
                case LotteryType.Canada28:
                    interval = 210; // 3.5分钟
                    break;
                case LotteryType.Bit28:
                    interval = 60;  // 1分钟
                    break;
                case LotteryType.Beijing28:
                    interval = 300; // 5分钟
                    break;
                default:
                    interval = 210;
                    break;
            }
            
            var periodInDay = (int)(elapsed.TotalSeconds / interval) + 1;
            return $"{time:yyyyMMdd}{periodInDay:D3}";
        }

        #endregion

        public void Dispose()
        {
            _mainTimer?.Stop();
            _mainTimer?.Dispose();
        }
    }

    #region 配置和枚举

    /// <summary>
    /// 彩种类型
    /// </summary>
    public enum LotteryType
    {
        /// <summary>PC蛋蛋/加拿大28 (3.5分钟一期)</summary>
        Canada28 = 1,
        /// <summary>比特28 (1分钟一期)</summary>
        Bit28 = 2,
        /// <summary>北京28 (5分钟一期)</summary>
        Beijing28 = 3
    }

    /// <summary>
    /// 封盘状态
    /// </summary>
    public enum SealingState
    {
        /// <summary>接受下注中</summary>
        Accepting,
        /// <summary>已发送提醒</summary>
        Reminded,
        /// <summary>已封盘</summary>
        Sealed,
        /// <summary>已发送规则</summary>
        RuleSent,
        /// <summary>等待开奖结果</summary>
        WaitingResult
    }

    /// <summary>
    /// 封盘配置
    /// </summary>
    public class SealingConfig
    {
        /// <summary>彩种类型</summary>
        public LotteryType LotteryType { get; set; } = LotteryType.Canada28;
        
        /// <summary>当前彩种类型（兼容别名）</summary>
        public LotteryType CurrentLotteryType => LotteryType;

        /// <summary>开奖间隔秒数 (加拿大28=210秒, 比特28=60秒)</summary>
        public int DrawIntervalSeconds { get; set; } = 210;

        /// <summary>绑定群ID</summary>
        public string TeamId { get; set; }

        #region 提醒配置

        /// <summary>是否启用提醒</summary>
        public bool ReminderEnabled { get; set; } = true;

        /// <summary>提前多少秒发送提醒</summary>
        public int ReminderSeconds { get; set; } = 60;

        /// <summary>提醒内容</summary>
        public string ReminderContent { get; set; } = "--距离封盘时间还有[封盘倒计时]秒--\n改注加注带改 或者 加";

        #endregion

        #region 封盘配置

        /// <summary>提前多少秒封盘</summary>
        public int SealingSeconds { get; set; } = 20;

        /// <summary>封盘内容</summary>
        public string SealingContent { get; set; } = "========封盘线=======\n以上有钱的都接\n=====庄显为准=======";

        #endregion

        #region 规则配置

        /// <summary>是否启用规则发送</summary>
        public bool RuleEnabled { get; set; } = true;

        /// <summary>开奖前多少秒发送规则</summary>
        public int RuleSeconds { get; set; } = 1;

        /// <summary>规则内容</summary>
        public string RuleContent { get; set; } = "本群如遇卡奖情况，十分钟官网没开奖，本期无效，无需纠结！！！！";

        #endregion

        #region 禁言配置

        /// <summary>是否自动禁言</summary>
        public bool AutoMute { get; set; } = true;

        /// <summary>提前多少秒禁言</summary>
        public int MuteBeforeSeconds { get; set; } = 5;

        #endregion

        public static SealingConfig CreateDefault()
        {
            return new SealingConfig();
        }

        /// <summary>
        /// 根据彩种设置默认间隔
        /// </summary>
        public void ApplyLotteryTypeDefaults()
        {
            switch (LotteryType)
            {
                case LotteryType.Canada28:
                    DrawIntervalSeconds = 210; // 3.5分钟
                    ReminderSeconds = 60;
                    SealingSeconds = 20;
                    break;
                case LotteryType.Bit28:
                    DrawIntervalSeconds = 60; // 1分钟
                    ReminderSeconds = 10;
                    SealingSeconds = 5;
                    break;
                case LotteryType.Beijing28:
                    DrawIntervalSeconds = 300; // 5分钟
                    ReminderSeconds = 60;
                    SealingSeconds = 20;
                    break;
            }
        }
    }

    #endregion
}
