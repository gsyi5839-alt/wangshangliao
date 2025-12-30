using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace WangShangLiaoBot.Services
{
    /// <summary>
    /// Lottery service - Fetches lottery results from API
    /// API: bcapi.cn - Canada 28 (jnd28)
    /// Rate limit: 30 seconds / 40 requests (1s interval) - 官方限制
    /// Supports dynamic configuration from server admin console
    /// </summary>
    public class LotteryService
    {
        private static LotteryService _instance;
        public static LotteryService Instance => _instance ?? (_instance = new LotteryService());
        
        // API configuration (loaded from server)
        private LotteryApiConfig _apiConfig;
        private bool _useBackupUrl = false;
        private int _consecutiveErrors = 0;
        
        // Default fallback configuration
        private const string DEFAULT_API_TEMPLATE = "https://bcapi.cn/token/{token}/code/jnd28/rows/1.json";
        private const int DEFAULT_REQUEST_INTERVAL_MS = 2000; // 2 seconds default
        
        // Timer for auto refresh
        private System.Timers.Timer _refreshTimer;
        private System.Timers.Timer _highFreqTimer; // High frequency timer for pre-result fetching
        private DateTime _lastRequestTime = DateTime.MinValue;
        private int _requestIntervalMs = DEFAULT_REQUEST_INTERVAL_MS;
        private int _requestsInLast30s = 0;
        private DateTime _last30sWindowStart = DateTime.Now;
        private int _maxRequestsPer30s = 40;
        
        // Current lottery data
        public string CurrentPeriod { get; private set; } = "";
        public string NextPeriod { get; private set; } = "";
        public int Number1 { get; private set; } = 0;
        public int Number2 { get; private set; } = 0;
        public int Number3 { get; private set; } = 0;
        public int Sum { get; private set; } = 0;
        public DateTime OpenTime { get; private set; } = DateTime.MinValue;
        public int Countdown { get; private set; } = 60;
        
        // High frequency mode flag (enabled when countdown is low)
        // 注意：官方限制 30秒内超过40次会被封禁1天，单次请求间隔需>=1秒
        private bool _highFreqMode = false;
        private const int HIGH_FREQ_THRESHOLD = 30; // Enable high freq when countdown <= 30s
        private const int HIGH_FREQ_INTERVAL_MS = 1000; // 1 second interval (官方最低要求)
        
        // Events
        public event Action<LotteryResult> OnResultUpdated;
        public event Action<int> OnCountdownUpdated;
        public event Action<string> OnError;
        
        private LotteryService()
        {
            // Initialize timer for countdown (1 second interval)
            _refreshTimer = new System.Timers.Timer(1000);
            _refreshTimer.Elapsed += OnTimerTick;
            _refreshTimer.AutoReset = true;
            
            // Initialize high frequency timer (1s interval - 官方最低要求)
            _highFreqTimer = new System.Timers.Timer(HIGH_FREQ_INTERVAL_MS);
            _highFreqTimer.Elapsed += OnHighFreqTick;
            _highFreqTimer.AutoReset = true;
        }
        
        /// <summary>
        /// Load lottery API configuration from server
        /// </summary>
        public async Task LoadApiConfigAsync()
        {
            try
            {
                var apis = await ClientPortalService.Instance.GetLotteryApisAsync().ConfigureAwait(false);
                if (apis != null && apis.Count > 0)
                {
                    // Use first enabled API (typically jnd28)
                    _apiConfig = apis[0];
                    _requestIntervalMs = Math.Max(_apiConfig.request_interval, 100);
                    _maxRequestsPer30s = _apiConfig.max_requests_per_30s;
                    _useBackupUrl = false;
                    _consecutiveErrors = 0;
                    
                    Logger.Info($"[LotteryService] Loaded API config: {_apiConfig.name} ({_apiConfig.code})");
                    Logger.Info($"[LotteryService] Interval: {_requestIntervalMs}ms, Max: {_maxRequestsPer30s}/30s");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[LotteryService] Failed to load API config: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Start auto refresh service
        /// </summary>
        public async void Start()
        {
            // Load API configuration from server
            await LoadApiConfigAsync().ConfigureAwait(false);
            
            _refreshTimer.Start();
            // Fetch immediately on start
            FetchLatestResult();
            Logger.Info("开奖服务已启动");
        }
        
        /// <summary>
        /// Stop auto refresh service
        /// </summary>
        public void Stop()
        {
            _refreshTimer.Stop();
            _highFreqTimer.Stop();
            _highFreqMode = false;
            Logger.Info("开奖服务已停止");
        }
        
        /// <summary>
        /// Timer tick - Update countdown and fetch new results
        /// </summary>
        private void OnTimerTick(object sender, ElapsedEventArgs e)
        {
            // Update countdown
            Countdown--;
            if (Countdown < 0) Countdown = 0;
            
            OnCountdownUpdated?.Invoke(Countdown);
            
            // Check if we should enter high frequency mode
            if (Countdown <= HIGH_FREQ_THRESHOLD && Countdown > 0 && !_highFreqMode)
            {
                _highFreqMode = true;
                _highFreqTimer.Start();
                Logger.Info($"[LotteryService] Entering high-freq mode, countdown: {Countdown}s");
            }
            else if ((Countdown > HIGH_FREQ_THRESHOLD || Countdown <= 0) && _highFreqMode)
            {
                _highFreqMode = false;
                _highFreqTimer.Stop();
                Logger.Info($"[LotteryService] Exiting high-freq mode");
            }
            
            // Regular fetch every 30 seconds when not in high freq mode
            if (!_highFreqMode && (Countdown <= 0 || (DateTime.Now - _lastRequestTime).TotalSeconds >= 30))
            {
                FetchLatestResult();
            }
        }
        
        /// <summary>
        /// High frequency timer tick - Fetch results at 0.5s interval
        /// </summary>
        private void OnHighFreqTick(object sender, ElapsedEventArgs e)
        {
            if (_highFreqMode && CanMakeRequest())
            {
                FetchLatestResult();
            }
        }
        
        /// <summary>
        /// Check if we can make a request based on rate limiting
        /// </summary>
        private bool CanMakeRequest()
        {
            // Reset 30 second window if needed
            var now = DateTime.Now;
            if ((now - _last30sWindowStart).TotalSeconds >= 30)
            {
                _requestsInLast30s = 0;
                _last30sWindowStart = now;
            }
            
            // Check if we've exceeded max requests per 30 seconds
            if (_requestsInLast30s >= _maxRequestsPer30s)
            {
                return false;
            }
            
            // Check minimum interval between requests
            // 官方要求：单个IP单个token访问间隔为1秒或以上
            var timeSinceLastRequest = (now - _lastRequestTime).TotalMilliseconds;
            var minInterval = Math.Max(HIGH_FREQ_INTERVAL_MS, _requestIntervalMs); // 至少1秒
            if (timeSinceLastRequest < minInterval)
            {
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Fetch latest lottery result from API
        /// </summary>
        public void FetchLatestResult()
        {
            // Rate limiting check
            if (!CanMakeRequest())
            {
                return; // Skip if too frequent or rate limited
            }
            
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    _lastRequestTime = DateTime.Now;
                    _requestsInLast30s++;
                    
                    using (var client = new WebClient())
                    {
                        client.Encoding = Encoding.UTF8;
                        // Use a safe User-Agent (avoid blocked ones like Go-http-client)
                        client.Headers.Add("User-Agent", "BocailBot/1.0 (Windows NT 10.0; Win64; x64)");

                        var apiUrl = BuildApiUrl();
                        if (string.IsNullOrWhiteSpace(apiUrl))
                        {
                            OnError?.Invoke("开奖接口未配置，请先登录并在管理系统配置开奖接口");
                            return;
                        }

                        var json = client.DownloadString(apiUrl);
                        _consecutiveErrors = 0; // Reset error counter on success
                        ParseResult(json);
                    }
                }
                catch (Exception ex)
                {
                    _consecutiveErrors++;
                    Logger.Error($"获取开奖数据失败 (错误{_consecutiveErrors}): {ex.Message}");
                    
                    // Switch to backup URL after 3 consecutive errors
                    if (_consecutiveErrors >= 3 && _apiConfig != null && !string.IsNullOrWhiteSpace(_apiConfig.backup_url))
                    {
                        _useBackupUrl = !_useBackupUrl;
                        Logger.Info($"[LotteryService] Switching to {(_useBackupUrl ? "backup" : "primary")} URL");
                        _consecutiveErrors = 0;
                    }
                    
                    OnError?.Invoke(ex.Message);
                }
            });
        }

        /// <summary>
        /// Build lottery API URL from config.
        /// Priority:
        /// 1) Server-provided API config (_apiConfig)
        /// 2) Config.LotteryApiUrl (may include {token})
        /// 3) Default template (requires Config.LotteryApiToken)
        /// </summary>
        private string BuildApiUrl()
        {
            try
            {
                // Priority 1: Use server-provided API config
                if (_apiConfig != null)
                {
                    var baseUrl = _useBackupUrl && !string.IsNullOrWhiteSpace(_apiConfig.backup_url)
                        ? _apiConfig.backup_url
                        : _apiConfig.api_url;
                    
                    // Replace placeholders
                    var url = baseUrl
                        .Replace("{token}", _apiConfig.token)
                        .Replace("{code}", _apiConfig.code)
                        .Replace("{rows}", _apiConfig.rows_count.ToString())
                        .Replace("{format}", _apiConfig.format_type);
                    
                    return url;
                }
                
                // Priority 2 & 3: Fall back to local config
                var cfg = ConfigService.Instance.Config;
                if (cfg == null) return "";

                var localUrl = (cfg.LotteryApiUrl ?? "").Trim();
                var token = (cfg.LotteryApiToken ?? "").Trim();

                if (!string.IsNullOrWhiteSpace(localUrl))
                {
                    if (localUrl.Contains("{token}"))
                        return localUrl.Replace("{token}", token);
                    return localUrl;
                }

                if (string.IsNullOrWhiteSpace(token)) return "";
                return DEFAULT_API_TEMPLATE.Replace("{token}", token);
            }
            catch
            {
                return "";
            }
        }
        
        /// <summary>
        /// Get current API status for debugging
        /// </summary>
        public string GetApiStatus()
        {
            if (_apiConfig != null)
            {
                return $"接口: {_apiConfig.name} ({_apiConfig.code}), " +
                       $"使用{(_useBackupUrl ? "备用" : "主")}地址, " +
                       $"请求数: {_requestsInLast30s}/{_maxRequestsPer30s}/30s, " +
                       $"间隔: {_requestIntervalMs}ms";
            }
            return "使用本地配置";
        }
        
        /// <summary>
        /// Parse API response JSON
        /// Format: {"rows":1,"info":"...","code":"jnd28","data":[{"expect":"3373755","opencode":"0,9,1","opentime":"2025-12-19 17:22:30"}]}
        /// </summary>
        private void ParseResult(string json)
        {
            try
            {
                // Simple JSON parsing without external libraries
                // Extract expect (period number)
                var expectMatch = System.Text.RegularExpressions.Regex.Match(json, @"""expect""\s*:\s*""(\d+)""");
                if (!expectMatch.Success) return;
                var newPeriod = expectMatch.Groups[1].Value;
                
                // Extract opencode (lottery numbers)
                var opencodeMatch = System.Text.RegularExpressions.Regex.Match(json, @"""opencode""\s*:\s*""([^""]+)""");
                if (!opencodeMatch.Success) return;
                var opencode = opencodeMatch.Groups[1].Value;
                
                // Extract opentime
                var opentimeMatch = System.Text.RegularExpressions.Regex.Match(json, @"""opentime""\s*:\s*""([^""]+)""");
                DateTime openTime = DateTime.MinValue;
                if (opentimeMatch.Success)
                {
                    DateTime.TryParse(opentimeMatch.Groups[1].Value, out openTime);
                }
                
                // Parse numbers (format: "0,9,1")
                var numbers = opencode.Split(',');
                if (numbers.Length >= 3)
                {
                    int n1, n2, n3;
                    if (int.TryParse(numbers[0].Trim(), out n1) &&
                        int.TryParse(numbers[1].Trim(), out n2) &&
                        int.TryParse(numbers[2].Trim(), out n3))
                    {
                        // Check if this is a new period
                        if (newPeriod != CurrentPeriod)
                        {
                            CurrentPeriod = newPeriod;
                            NextPeriod = (long.Parse(newPeriod) + 1).ToString();
                            Number1 = n1;
                            Number2 = n2;
                            Number3 = n3;
                            Sum = n1 + n2 + n3;
                            OpenTime = openTime;
                            
                            // Calculate countdown based on open time
                            // Canada 28 opens every 3.5 minutes (210 seconds)
                            if (openTime != DateTime.MinValue)
                            {
                                var elapsed = (DateTime.Now - openTime).TotalSeconds;
                                Countdown = Math.Max(0, 210 - (int)elapsed);
                                if (Countdown > 210) Countdown = 210;
                            }
                            else
                            {
                                Countdown = 60; // Default countdown
                            }
                            
                            // Notify listeners
                            var result = new LotteryResult
                            {
                                Period = CurrentPeriod,
                                NextPeriod = NextPeriod,
                                Number1 = Number1,
                                Number2 = Number2,
                                Number3 = Number3,
                                Sum = Sum,
                                OpenTime = OpenTime,
                                Countdown = Countdown
                            };
                            
                            Logger.Info(string.Format("开奖更新: 期号{0}, 号码{1}+{2}+{3}={4}", 
                                CurrentPeriod, Number1, Number2, Number3, Sum));

                            // Persist history for template variables like [开奖历史] and maintain daily period count.
                            try
                            {
                                var line = string.Format("期号{0} 号码{1}+{2}+{3}={4}", CurrentPeriod, Number1, Number2, Number3, Sum);
                                DataService.Instance.AppendLotteryHistory(DateTime.Today, line);
                            }
                            catch { }
                            
                            OnResultUpdated?.Invoke(result);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(string.Format("解析开奖数据失败: {0}", ex.Message));
                OnError?.Invoke(ex.Message);
            }
        }
        
        /// <summary>
        /// Manual refresh
        /// </summary>
        public void Refresh()
        {
            _lastRequestTime = DateTime.MinValue; // Reset rate limit
            FetchLatestResult();
        }
    }
    
    /// <summary>
    /// Lottery result data model
    /// </summary>
    public class LotteryResult
    {
        public string Period { get; set; }
        public string NextPeriod { get; set; }
        public int Number1 { get; set; }
        public int Number2 { get; set; }
        public int Number3 { get; set; }
        public int Sum { get; set; }
        public DateTime OpenTime { get; set; }
        public int Countdown { get; set; }
    }
}


