using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using WSLFramework.Utils;

namespace WSLFramework.Services.Scheduling
{
    /// <summary>
    /// 通用限流器 - 滑动窗口算法实现
    /// 用于控制 API 请求频率，防止触发服务端限制
    /// </summary>
    public class RateLimiter
    {
        #region 私有字段

        private readonly int _maxRequests;           // 时间窗口内最大请求数
        private readonly TimeSpan _windowSize;       // 时间窗口大小
        private readonly TimeSpan _minInterval;      // 最小请求间隔
        
        private readonly ConcurrentQueue<DateTime> _requestTimes;
        private readonly SemaphoreSlim _semaphore;
        private DateTime _lastRequestTime = DateTime.MinValue;
        private readonly object _lock = new object();

        #endregion

        #region 事件

        public event Action<string> OnLog;
        public event Action<int, int> OnRateLimitApproaching; // (当前请求数, 最大请求数)

        #endregion

        #region 构造函数

        /// <summary>
        /// 创建限流器
        /// </summary>
        /// <param name="maxRequests">时间窗口内最大请求数</param>
        /// <param name="windowSize">时间窗口大小</param>
        /// <param name="minInterval">最小请求间隔</param>
        public RateLimiter(int maxRequests, TimeSpan windowSize, TimeSpan minInterval)
        {
            _maxRequests = maxRequests;
            _windowSize = windowSize;
            _minInterval = minInterval;
            _requestTimes = new ConcurrentQueue<DateTime>();
            _semaphore = new SemaphoreSlim(1, 1);
        }

        /// <summary>
        /// 创建开奖接口限流器 (bcapi.cn 限制: 30秒40次，间隔>=1秒)
        /// </summary>
        public static RateLimiter CreateLotteryApiLimiter()
        {
            return new RateLimiter(
                maxRequests: 35,                           // 留有余量，实际限制40
                windowSize: TimeSpan.FromSeconds(30),
                minInterval: TimeSpan.FromMilliseconds(1100) // 留有余量，实际限制1秒
            );
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 等待直到可以发送请求 (异步)
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>等待的毫秒数</returns>
        public async Task<int> WaitAsync(CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                return await WaitInternalAsync(cancellationToken);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// 尝试获取请求许可 (非阻塞)
        /// </summary>
        /// <returns>是否获取成功</returns>
        public bool TryAcquire()
        {
            lock (_lock)
            {
                CleanupOldRequests();

                // 检查时间窗口内请求数
                if (_requestTimes.Count >= _maxRequests)
                    return false;

                // 检查最小间隔
                var timeSinceLastRequest = DateTime.UtcNow - _lastRequestTime;
                if (timeSinceLastRequest < _minInterval)
                    return false;

                // 记录请求
                RecordRequest();
                return true;
            }
        }

        /// <summary>
        /// 记录一次请求 (已发送后调用)
        /// </summary>
        public void RecordRequest()
        {
            lock (_lock)
            {
                var now = DateTime.UtcNow;
                _requestTimes.Enqueue(now);
                _lastRequestTime = now;
            }
        }

        /// <summary>
        /// 获取当前时间窗口内的请求数
        /// </summary>
        public int GetCurrentRequestCount()
        {
            lock (_lock)
            {
                CleanupOldRequests();
                return _requestTimes.Count;
            }
        }

        /// <summary>
        /// 获取距离下次可请求的等待时间
        /// </summary>
        public TimeSpan GetWaitTime()
        {
            lock (_lock)
            {
                CleanupOldRequests();
                return GetWaitTimeInternal();
            }
        }

        /// <summary>
        /// 重置限流器状态
        /// </summary>
        public void Reset()
        {
            lock (_lock)
            {
                while (_requestTimes.TryDequeue(out _)) { }
                _lastRequestTime = DateTime.MinValue;
            }
            Log("[限流器] 已重置");
        }

        /// <summary>
        /// 获取统计信息
        /// </summary>
        public RateLimiterStats GetStats()
        {
            lock (_lock)
            {
                CleanupOldRequests();
                
                // 在锁内直接计算等待时间，避免重复加锁
                var waitTime = GetWaitTimeInternal();
                
                return new RateLimiterStats
                {
                    CurrentRequests = _requestTimes.Count,
                    MaxRequests = _maxRequests,
                    WindowSizeMs = (int)_windowSize.TotalMilliseconds,
                    MinIntervalMs = (int)_minInterval.TotalMilliseconds,
                    LastRequestTime = _lastRequestTime,
                    WaitTimeMs = (int)waitTime.TotalMilliseconds
                };
            }
        }
        
        /// <summary>
        /// 内部计算等待时间 (不加锁，供已加锁的方法调用)
        /// </summary>
        private TimeSpan GetWaitTimeInternal()
        {
            var now = DateTime.UtcNow;
            var waitForInterval = TimeSpan.Zero;
            var waitForWindow = TimeSpan.Zero;

            // 计算最小间隔等待时间
            var timeSinceLastRequest = now - _lastRequestTime;
            if (timeSinceLastRequest < _minInterval)
            {
                waitForInterval = _minInterval - timeSinceLastRequest;
            }

            // 计算窗口等待时间
            if (_requestTimes.Count >= _maxRequests)
            {
                if (_requestTimes.TryPeek(out var oldestRequest))
                {
                    var windowEnd = oldestRequest + _windowSize;
                    if (windowEnd > now)
                    {
                        waitForWindow = windowEnd - now;
                    }
                }
            }

            // 返回较大的等待时间
            return waitForInterval > waitForWindow ? waitForInterval : waitForWindow;
        }

        #endregion

        #region 私有方法

        private async Task<int> WaitInternalAsync(CancellationToken cancellationToken)
        {
            var totalWaitMs = 0;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var waitTime = GetWaitTime();
                if (waitTime <= TimeSpan.Zero)
                {
                    RecordRequest();
                    
                    // 检查是否接近限制，触发警告
                    var currentCount = GetCurrentRequestCount();
                    if (currentCount >= _maxRequests * 0.8)
                    {
                        OnRateLimitApproaching?.Invoke(currentCount, _maxRequests);
                    }
                    
                    return totalWaitMs;
                }

                var waitMs = (int)Math.Ceiling(waitTime.TotalMilliseconds);
                Log($"[限流器] 等待 {waitMs}ms (当前请求数: {GetCurrentRequestCount()}/{_maxRequests})");
                
                await Task.Delay(waitMs, cancellationToken);
                totalWaitMs += waitMs;
            }
        }

        private void CleanupOldRequests()
        {
            var cutoff = DateTime.UtcNow - _windowSize;
            
            while (_requestTimes.TryPeek(out var oldest) && oldest < cutoff)
            {
                _requestTimes.TryDequeue(out _);
            }
        }

        private void Log(string message)
        {
            Logger.Debug(message);
            OnLog?.Invoke(message);
        }

        #endregion
    }

    /// <summary>
    /// 限流器统计信息
    /// </summary>
    public class RateLimiterStats
    {
        /// <summary>当前窗口内请求数</summary>
        public int CurrentRequests { get; set; }
        
        /// <summary>最大请求数限制</summary>
        public int MaxRequests { get; set; }
        
        /// <summary>时间窗口大小(毫秒)</summary>
        public int WindowSizeMs { get; set; }
        
        /// <summary>最小请求间隔(毫秒)</summary>
        public int MinIntervalMs { get; set; }
        
        /// <summary>最后请求时间</summary>
        public DateTime LastRequestTime { get; set; }
        
        /// <summary>需要等待的时间(毫秒)</summary>
        public int WaitTimeMs { get; set; }
        
        /// <summary>剩余可用请求数</summary>
        public int RemainingRequests => Math.Max(0, MaxRequests - CurrentRequests);
        
        /// <summary>使用率百分比 (防止除零)</summary>
        public double UsagePercent => MaxRequests > 0 ? (double)CurrentRequests / MaxRequests * 100 : 0;
    }

    /// <summary>
    /// 多接口限流管理器 - 管理多个 API 的限流状态
    /// </summary>
    public class RateLimiterManager
    {
        #region 单例模式

        private static readonly Lazy<RateLimiterManager> _instance =
            new Lazy<RateLimiterManager>(() => new RateLimiterManager());

        public static RateLimiterManager Instance => _instance.Value;

        #endregion

        #region 私有字段

        private readonly ConcurrentDictionary<string, RateLimiter> _limiters;

        #endregion

        #region 事件

        public event Action<string> OnLog;

        #endregion

        #region 构造函数

        private RateLimiterManager()
        {
            _limiters = new ConcurrentDictionary<string, RateLimiter>();
            
            // 注册默认限流器
            RegisterLotteryApiLimiter();
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 获取或创建限流器
        /// </summary>
        public RateLimiter GetLimiter(string name)
        {
            return _limiters.GetOrAdd(name, _ => CreateDefaultLimiter());
        }

        /// <summary>
        /// 注册限流器
        /// </summary>
        public void RegisterLimiter(string name, RateLimiter limiter)
        {
            _limiters[name] = limiter;
            Log($"[限流管理器] 已注册: {name}");
        }

        /// <summary>
        /// 移除限流器
        /// </summary>
        public bool RemoveLimiter(string name)
        {
            return _limiters.TryRemove(name, out _);
        }

        /// <summary>
        /// 获取开奖API限流器
        /// </summary>
        public RateLimiter GetLotteryApiLimiter()
        {
            return GetLimiter("lottery_api");
        }

        /// <summary>
        /// 等待开奖API限流
        /// </summary>
        public Task<int> WaitForLotteryApiAsync(CancellationToken cancellationToken = default)
        {
            return GetLotteryApiLimiter().WaitAsync(cancellationToken);
        }

        /// <summary>
        /// 获取所有限流器状态
        /// </summary>
        public ConcurrentDictionary<string, RateLimiterStats> GetAllStats()
        {
            var stats = new ConcurrentDictionary<string, RateLimiterStats>();
            foreach (var kvp in _limiters)
            {
                stats[kvp.Key] = kvp.Value.GetStats();
            }
            return stats;
        }

        #endregion

        #region 私有方法

        private void RegisterLotteryApiLimiter()
        {
            var limiter = RateLimiter.CreateLotteryApiLimiter();
            limiter.OnLog += msg => Log(msg);
            limiter.OnRateLimitApproaching += (current, max) =>
            {
                Log($"[限流警告] 开奖API接近限制: {current}/{max} (使用率: {current * 100.0 / max:F1}%)");
            };
            _limiters["lottery_api"] = limiter;
        }

        private RateLimiter CreateDefaultLimiter()
        {
            // 默认: 60秒100次，间隔500ms
            return new RateLimiter(100, TimeSpan.FromSeconds(60), TimeSpan.FromMilliseconds(500));
        }

        private void Log(string message)
        {
            Logger.Info(message);
            OnLog?.Invoke(message);
        }

        #endregion
    }
}
