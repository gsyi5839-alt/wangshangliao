using System;
using System.Threading;
using System.Threading.Tasks;
using WSLFramework.Utils;

namespace WSLFramework.Services
{
    /// <summary>
    /// 开奖周期管理器 - 完全匹配ZCG招财狗的210秒周期和封盘逻辑
    /// 基于深度逆向分析实现
    /// </summary>
    public class PeriodManager : IDisposable
    {
        private static readonly Lazy<PeriodManager> _instance = 
            new Lazy<PeriodManager>(() => new PeriodManager(), LazyThreadSafetyMode.ExecutionAndPublication);
        public static PeriodManager Instance => _instance.Value;
        
        private Timer _timer;
        private CancellationTokenSource _cts;
        
        // 常量 - ZCG协议规范
        public const int PERIOD_SECONDS = 210;       // 每期210秒（3分30秒）
        public const int CLOSE_BET_SECONDS = 30;     // 封盘前30秒
        public const int WARNING_40_SECONDS = 40;    // 40秒提醒
        public const int WARNING_20_SECONDS = 20;    // 20秒提醒
        public const int WARNING_10_SECONDS = 10;    // 10秒提醒 (核对)
        public const int WARNING_STUCK_SECONDS = -20; // 封盘后20秒发卡奖提示
        
        // 状态
        public bool IsRunning { get; private set; }
        public PeriodState State { get; private set; } = PeriodState.Idle;
        public string CurrentPeriod { get; private set; }
        public int Countdown { get; private set; }
        
        // 配置
        public bool AutoMuteEnabled { get; set; } = true;      // 封盘自动禁言
        public bool AutoAnnounceEnabled { get; set; } = true;  // 自动播报
        public bool BetCloseEnabled { get; set; } = true;      // 启用封盘
        
        // 事件
        public event Action<string> OnLog;
        public event Action<PeriodState, int> OnStateChanged;
        public event Action<int> OnCountdown;
        public event Action<string> OnWarning40;           // 40秒倒计时
        public event Action<string> OnWarning20;           // 20秒倒计时
        public event Action<string> OnBetClose;            // 封盘 (封盘线)
        public event Action<string> OnCheckNotify;         // 核对消息 (封盘后10秒)
        public event Action<string> OnStuckNotify;         // 卡奖提示 (封盘后约20秒)
        public event Action<string> OnBetOpen;             // 开盘
        public event Action<string> OnNewPeriod;           // 新一期开始
        public event Action<bool> OnMuteRequest;           // 禁言请求(true=禁言, false=解禁)
        
        private bool _warned40 = false;
        private bool _warned20 = false;
        private bool _betClosed = false;
        private bool _checkSent = false;    // 核对消息是否已发送
        private bool _stuckSent = false;    // 卡奖提示是否已发送
        private DateTime _betCloseTime;     // 封盘时间
        
        private PeriodManager()
        {
        }
        
        /// <summary>
        /// 启动周期管理器
        /// </summary>
        public void Start()
        {
            if (IsRunning) return;
            
            _cts = new CancellationTokenSource();
            IsRunning = true;
            State = PeriodState.Betting;
            
            // 计算当前期号
            CurrentPeriod = CalculateCurrentPeriod();
            Countdown = CalculateCountdown();
            
            // 启动定时器，每秒更新
            _timer = new Timer(TimerCallback, null, 0, 1000);
            
            Log($"周期管理器已启动，当前期号: {CurrentPeriod}, 倒计时: {Countdown}秒");
        }
        
        /// <summary>
        /// 停止周期管理器
        /// </summary>
        public void Stop()
        {
            _timer?.Change(Timeout.Infinite, Timeout.Infinite);
            _timer?.Dispose();
            _cts?.Cancel();
            
            IsRunning = false;
            State = PeriodState.Idle;
            
            Log("周期管理器已停止");
        }
        
        /// <summary>
        /// 定时器回调
        /// 消息发送顺序 (完全匹配旧软件):
        /// 1. 40秒 - 40秒提醒
        /// 2. 20秒 - 20秒提醒
        /// 3. 0秒 (封盘) - 封盘线
        /// 4. 封盘后10秒 - 核对
        /// 5. 封盘后约20秒 - 卡奖提示
        /// </summary>
        private void TimerCallback(object state)
        {
            if (_cts?.IsCancellationRequested == true) return;
            
            try
            {
                // 计算倒计时
                var newCountdown = CalculateCountdown();
                var previousCountdown = Countdown;
                Countdown = newCountdown;
                
                // 检查是否进入新一期
                if (newCountdown > previousCountdown && previousCountdown <= 5)
                {
                    HandleNewPeriod();
                }
                
                // 触发倒计时事件
                OnCountdown?.Invoke(Countdown);
                
                // 1. 40秒提醒 (封盘前40秒)
                if (Countdown <= WARNING_40_SECONDS && Countdown > CLOSE_BET_SECONDS && !_warned40)
                {
                    _warned40 = true;
                    HandleWarning40();
                }
                
                // 2. 20秒提醒 (封盘前20秒)
                if (Countdown <= WARNING_20_SECONDS && Countdown > CLOSE_BET_SECONDS && !_warned20)
                {
                    _warned20 = true;
                    HandleWarning20();
                }
                
                // 3. 封盘 (倒计时到30秒时封盘，发送封盘线)
                if (BetCloseEnabled && Countdown <= CLOSE_BET_SECONDS && !_betClosed)
                {
                    _betClosed = true;
                    _betCloseTime = DateTime.Now;
                    HandleBetClose();
                }
                
                // 封盘后的消息（根据封盘时间计算）
                if (_betClosed && _betCloseTime != DateTime.MinValue)
                {
                    var secondsSinceClose = (DateTime.Now - _betCloseTime).TotalSeconds;
                    
                    // 4. 核对消息 (封盘后10秒)
                    if (!_checkSent && secondsSinceClose >= 10)
                    {
                        _checkSent = true;
                        HandleCheckNotify();
                    }
                    
                    // 5. 卡奖提示 (封盘后约20秒)
                    if (!_stuckSent && secondsSinceClose >= 20)
                    {
                        _stuckSent = true;
                        HandleStuckNotify();
                    }
                }
                
                // 更新状态
                UpdateState();
            }
            catch (Exception ex)
            {
                Log($"周期管理器错误: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 处理新一期开始
        /// </summary>
        private void HandleNewPeriod()
        {
            // 重置所有标志
            _warned40 = false;
            _warned20 = false;
            _betClosed = false;
            _checkSent = false;
            _stuckSent = false;
            _betCloseTime = DateTime.MinValue;
            
            // 计算新期号
            var newPeriod = CalculateCurrentPeriod();
            var previousPeriod = CurrentPeriod;
            CurrentPeriod = newPeriod;
            
            Log($"新一期开始: {CurrentPeriod} (上一期: {previousPeriod})");
            
            // 解禁
            if (AutoMuteEnabled)
            {
                OnMuteRequest?.Invoke(false);
            }
            
            // 触发事件
            OnBetOpen?.Invoke(CurrentPeriod);
            OnNewPeriod?.Invoke(CurrentPeriod);
            
            State = PeriodState.Betting;
            OnStateChanged?.Invoke(State, Countdown);
        }
        
        /// <summary>
        /// 处理40秒警告 - 完全匹配ZCG旧程序格式
        /// </summary>
        private void HandleWarning40()
        {
            if (!AutoAnnounceEnabled) return;
            
            // 完全匹配旧程序: --距离封盘时间还有40秒--\n改注加注带改 或者 加
            var msg = $"--距离封盘时间还有{WARNING_40_SECONDS}秒--\n改注加注带改 或者 加";
            Log($"[消息顺序1] 40秒提醒: {msg}");
            OnWarning40?.Invoke(msg);
        }
        
        /// <summary>
        /// 处理20秒警告 - 完全匹配ZCG旧程序格式
        /// </summary>
        private void HandleWarning20()
        {
            if (!AutoAnnounceEnabled) return;
            
            // 完全匹配旧程序: --距离封盘时间还有20秒--
            var msg = $"--距离封盘时间还有{WARNING_20_SECONDS}秒--";
            Log($"[消息顺序2] 20秒提醒: {msg}");
            OnWarning20?.Invoke(msg);
        }
        
        /// <summary>
        /// 处理核对消息 (封盘后10秒发送)
        /// </summary>
        private void HandleCheckNotify()
        {
            if (!AutoAnnounceEnabled) return;
            
            // 完全匹配旧程序: 核对\n-------------------\n
            var msg = "核对\n-------------------\n";
            Log($"[消息顺序4] 核对消息 (封盘后10秒): {msg}");
            OnCheckNotify?.Invoke(msg);
        }
        
        /// <summary>
        /// 处理卡奖提示 (封盘后约20秒发送)
        /// </summary>
        private void HandleStuckNotify()
        {
            if (!AutoAnnounceEnabled) return;
            
            // 完全匹配旧程序
            var msg = "本群如遇卡奖情况，十分钟官网没开奖，本期无效，无需纠结！！！！";
            Log($"[消息顺序5] 卡奖提示 (封盘后20秒): {msg}");
            OnStuckNotify?.Invoke(msg);
        }
        
        /// <summary>
        /// 处理封盘 - 只发送封盘线消息 (核对和卡奖提示在后续时间点发送)
        /// </summary>
        private void HandleBetClose()
        {
            State = PeriodState.Closed;
            OnStateChanged?.Invoke(State, Countdown);
            
            // 禁言
            if (AutoMuteEnabled)
            {
                OnMuteRequest?.Invoke(true);
            }
            
            if (AutoAnnounceEnabled)
            {
                // 完全匹配旧程序: ==加封盘线==\n以上有钱的都接\n==庄显为准==
                // 注意：核对消息和卡奖提示由 HandleCheckNotify 和 HandleStuckNotify 在正确时间点发送
                Log($"[消息顺序3] 封盘线");
                OnBetClose?.Invoke(CurrentPeriod);  // 传期号给事件处理器
            }
        }
        
        /// <summary>
        /// 更新状态
        /// </summary>
        private void UpdateState()
        {
            var newState = Countdown <= CLOSE_BET_SECONDS ? PeriodState.Closed : PeriodState.Betting;
            
            if (newState != State)
            {
                State = newState;
                OnStateChanged?.Invoke(State, Countdown);
            }
        }
        
        /// <summary>
        /// 计算当前期号 - ZCG格式
        /// 期号 = 基准期号 + (当前时间 - 基准时间) / 210秒
        /// </summary>
        public static string CalculateCurrentPeriod()
        {
            // 基准点: 2026年1月11日00:00:00 UTC+8 为期号3382900
            var baseTime = new DateTime(2026, 1, 11, 0, 0, 0, DateTimeKind.Local);
            var basePeriod = 3382900L;
            
            var now = DateTime.Now;
            var secondsSinceBase = (long)(now - baseTime).TotalSeconds;
            var periodsSinceBase = secondsSinceBase / PERIOD_SECONDS;
            
            return (basePeriod + periodsSinceBase).ToString();
        }
        
        /// <summary>
        /// 计算下一期开奖倒计时
        /// </summary>
        public static int CalculateCountdown()
        {
            var now = DateTime.Now;
            // 从每天0点开始计算
            var secondsInDay = (int)(now - now.Date).TotalSeconds;
            var remaining = PERIOD_SECONDS - (secondsInDay % PERIOD_SECONDS);
            return remaining == PERIOD_SECONDS ? 0 : remaining;
        }
        
        /// <summary>
        /// 获取格式化的倒计时字符串
        /// </summary>
        public string GetCountdownString()
        {
            var minutes = Countdown / 60;
            var seconds = Countdown % 60;
            return $"{minutes}:{seconds:D2}";
        }
        
        /// <summary>
        /// 检查当前是否可以下注
        /// </summary>
        public bool CanBet()
        {
            return State == PeriodState.Betting && Countdown > CLOSE_BET_SECONDS;
        }
        
        private void Log(string message)
        {
            Logger.Info($"[周期] {message}");
            OnLog?.Invoke(message);
        }
        
        public void Dispose()
        {
            Stop();
            _cts?.Dispose();
        }
    }
    
    /// <summary>
    /// 周期状态
    /// </summary>
    public enum PeriodState
    {
        Idle,      // 空闲
        Betting,   // 下注中
        Closed,    // 已封盘
        Settling   // 结算中
    }
}
