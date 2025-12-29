using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Timers;

namespace WangShangLiaoBot.Services
{
    /// <summary>
    /// Lottery service - Fetches lottery results from API
    /// API: bcapi.cn - Canada 28 (jnd28)
    /// Rate limit: 30 seconds / 60 requests (0.5s interval)
    /// </summary>
    public class LotteryService
    {
        private static LotteryService _instance;
        public static LotteryService Instance => _instance ?? (_instance = new LotteryService());
        
        // API configuration
        // NOTE:
        // - The token should be configured from the server (admin console) and pulled after login.
        // - We keep only a non-secret template here; token is stored in AppConfig.
        private const string API_TEMPLATE = "https://bcapi.cn/token/{token}/code/jnd28/rows/1.json";
        private const int REQUEST_INTERVAL_MS = 2000; // 2 seconds interval (safe margin)
        
        // Timer for auto refresh
        private System.Timers.Timer _refreshTimer;
        private DateTime _lastRequestTime = DateTime.MinValue;
        
        // Current lottery data
        public string CurrentPeriod { get; private set; } = "";
        public string NextPeriod { get; private set; } = "";
        public int Number1 { get; private set; } = 0;
        public int Number2 { get; private set; } = 0;
        public int Number3 { get; private set; } = 0;
        public int Sum { get; private set; } = 0;
        public DateTime OpenTime { get; private set; } = DateTime.MinValue;
        public int Countdown { get; private set; } = 60;
        
        // Events
        public event Action<LotteryResult> OnResultUpdated;
        public event Action<int> OnCountdownUpdated;
        public event Action<string> OnError;
        
        private LotteryService()
        {
            // Initialize timer for countdown
            _refreshTimer = new System.Timers.Timer(1000);
            _refreshTimer.Elapsed += OnTimerTick;
            _refreshTimer.AutoReset = true;
        }
        
        /// <summary>
        /// Start auto refresh service
        /// </summary>
        public void Start()
        {
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
            
            // Fetch new result when countdown reaches 0 or every 30 seconds
            if (Countdown <= 0 || (DateTime.Now - _lastRequestTime).TotalSeconds >= 30)
            {
                FetchLatestResult();
            }
        }
        
        /// <summary>
        /// Fetch latest lottery result from API
        /// </summary>
        public void FetchLatestResult()
        {
            // Rate limiting check
            var timeSinceLastRequest = (DateTime.Now - _lastRequestTime).TotalMilliseconds;
            if (timeSinceLastRequest < REQUEST_INTERVAL_MS)
            {
                return; // Skip if too frequent
            }
            
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    _lastRequestTime = DateTime.Now;
                    
                    using (var client = new WebClient())
                    {
                        client.Encoding = Encoding.UTF8;
                        client.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

                        var apiUrl = BuildApiUrl();
                        if (string.IsNullOrWhiteSpace(apiUrl))
                        {
                            OnError?.Invoke("开奖接口未配置，请先登录并在管理系统配置开奖 Token");
                            return;
                        }

                        var json = client.DownloadString(apiUrl);
                        ParseResult(json);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(string.Format("获取开奖数据失败: {0}", ex.Message));
                    OnError?.Invoke(ex.Message);
                }
            });
        }

        /// <summary>
        /// Build lottery API URL from config.
        /// Priority:
        /// 1) Config.LotteryApiUrl (may include {token})
        /// 2) Default template (requires Config.LotteryApiToken)
        /// </summary>
        private string BuildApiUrl()
        {
            try
            {
                var cfg = ConfigService.Instance.Config;
                if (cfg == null) return "";

                var url = (cfg.LotteryApiUrl ?? "").Trim();
                var token = (cfg.LotteryApiToken ?? "").Trim();

                if (!string.IsNullOrWhiteSpace(url))
                {
                    if (url.Contains("{token}"))
                        return url.Replace("{token}", token);
                    return url;
                }

                if (string.IsNullOrWhiteSpace(token)) return "";
                return API_TEMPLATE.Replace("{token}", token);
            }
            catch
            {
                return "";
            }
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


