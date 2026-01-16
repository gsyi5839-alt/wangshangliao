using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using WSLFramework.Utils;

namespace WSLFramework.Services
{
    /// <summary>
    /// 开奖服务 - 获取加拿大28等彩票开奖数据
    /// 基于招财狗逆向分析
    /// </summary>
    public class LotteryService : IDisposable
    {
        private static readonly Lazy<LotteryService> _lazy = 
            new Lazy<LotteryService>(() => new LotteryService());
        public static LotteryService Instance => _lazy.Value;

        private static readonly HttpClient _httpClient;
        private CancellationTokenSource _cts;
        private readonly JavaScriptSerializer _serializer;
        
        // 历史开奖记录缓存
        private readonly List<LotteryResult> _resultHistory = new List<LotteryResult>();
        private const int MAX_HISTORY_COUNT = 100;
        
        // API配置
        public string ApiUrl { get; set; } = "https://www.bcapi.cn/api/jnd28/";
        public string BackupUrl { get; set; } = "https://api.bcapi.cn/api/jnd28/";
        public string Token { get; set; } = "";
        public int PollInterval { get; set; } = 3000; // 轮询间隔(毫秒)
        
        // 状态
        public bool IsRunning { get; private set; }
        public LotteryResult LastResult { get; private set; }
        public string CurrentPeriod { get; private set; }
        
        // 事件
        public event Action<LotteryResult> OnNewResult;
        public event Action<string> OnLog;
        public event Action<int> OnCountdown; // 倒计时事件(秒)
        
        static LotteryService()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        }
        
        public LotteryService()
        {
            _serializer = new JavaScriptSerializer();
        }
        
        /// <summary>
        /// 启动开奖轮询
        /// </summary>
        public async Task StartAsync()
        {
            if (IsRunning) return;
            
            _cts = new CancellationTokenSource();
            IsRunning = true;
            Log("开奖服务已启动");
            
            _ = Task.Run(PollLoop);
        }
        
        /// <summary>
        /// 停止开奖轮询
        /// </summary>
        public void Stop()
        {
            _cts?.Cancel();
            IsRunning = false;
            Log("开奖服务已停止");
        }
        
        /// <summary>
        /// 设置彩种类型
        /// </summary>
        public void SetLotteryType(int typeId)
        {
            switch (typeId)
            {
                case 1: // 加拿大28
                    ApiUrl = "https://www.bcapi.cn/api/jnd28/";
                    BackupUrl = "https://api.bcapi.cn/api/jnd28/";
                    break;
                case 2: // 北京28
                    ApiUrl = "https://www.bcapi.cn/api/bj28/";
                    BackupUrl = "https://api.bcapi.cn/api/bj28/";
                    break;
                case 3: // 台湾28
                    ApiUrl = "https://www.bcapi.cn/api/tw28/";
                    BackupUrl = "https://api.bcapi.cn/api/tw28/";
                    break;
                case 4: // 澳洲28
                    ApiUrl = "https://www.bcapi.cn/api/act28/";
                    BackupUrl = "https://api.bcapi.cn/api/act28/";
                    break;
            }
            Log($"已切换开奖接口: {ApiUrl}");
        }
        
        /// <summary>
        /// 轮询循环
        /// </summary>
        private async Task PollLoop()
        {
            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    var result = await FetchLatestResultAsync();
                    if (result != null && result.Period != CurrentPeriod)
                    {
                        CurrentPeriod = result.Period;
                        LastResult = result;
                        OnNewResult?.Invoke(result);
                        Log($"新开奖: 期{result.Period} = {result.Num1}+{result.Num2}+{result.Num3}={result.Sum} {result.GetResultString()}");
                    }
                    
                    // 计算倒计时
                    if (result != null)
                    {
                        var countdown = CalculateCountdown();
                        OnCountdown?.Invoke(countdown);
                    }
                }
                catch (Exception ex)
                {
                    Log($"轮询异常: {ex.Message}");
                }
                
                await Task.Delay(PollInterval, _cts.Token);
            }
        }
        
        /// <summary>
        /// 获取最新开奖结果
        /// </summary>
        public async Task<LotteryResult> FetchLatestResultAsync()
        {
            try
            {
                var url = BuildApiUrl();
                var response = await _httpClient.GetStringAsync(url);
                
                return ParseApiResponse(response);
            }
            catch (Exception ex)
            {
                // 尝试备用URL
                try
                {
                    var url = BuildApiUrl(true);
                    var response = await _httpClient.GetStringAsync(url);
                    return ParseApiResponse(response);
                }
                catch
                {
                    Log($"获取开奖数据失败: {ex.Message}");
                    return null;
                }
            }
        }
        
        /// <summary>
        /// 获取历史开奖记录
        /// </summary>
        public async Task<List<LotteryResult>> FetchHistoryAsync(int count = 10)
        {
            try
            {
                var url = BuildApiUrl() + $"&num={count}";
                var response = await _httpClient.GetStringAsync(url);
                
                // 解析多条记录
                var results = new List<LotteryResult>();
                var data = _serializer.Deserialize<Dictionary<string, object>>(response);
                
                if (data != null && data.ContainsKey("data"))
                {
                    var list = data["data"] as object[];
                    if (list != null)
                    {
                        foreach (var item in list)
                        {
                            var dict = item as Dictionary<string, object>;
                            if (dict != null)
                            {
                                results.Add(ParseResultDict(dict));
                            }
                        }
                    }
                }
                
                return results;
            }
            catch (Exception ex)
            {
                Log($"获取历史记录失败: {ex.Message}");
                return new List<LotteryResult>();
            }
        }
        
        /// <summary>
        /// 获取最近的开奖结果（优先从缓存）
        /// </summary>
        public async Task<List<LotteryResult>> GetRecentResultsAsync(int count = 10)
        {
            // 优先从缓存获取
            lock (_resultHistory)
            {
                if (_resultHistory.Count >= count)
                {
                    return _resultHistory.GetRange(0, count);
                }
            }
            
            // 缓存不足，从API获取
            return await FetchHistoryAsync(count);
        }
        
        /// <summary>
        /// 添加到历史缓存
        /// </summary>
        private void AddToHistory(LotteryResult result)
        {
            if (result == null) return;
            
            lock (_resultHistory)
            {
                // 检查是否已存在
                if (_resultHistory.Exists(r => r.Period == result.Period))
                    return;
                
                // 插入到最前面
                _resultHistory.Insert(0, result);
                
                // 限制缓存大小
                while (_resultHistory.Count > MAX_HISTORY_COUNT)
                {
                    _resultHistory.RemoveAt(_resultHistory.Count - 1);
                }
            }
        }
        
        /// <summary>
        /// 构建API URL
        /// </summary>
        private string BuildApiUrl(bool useBackup = false)
        {
            var baseUrl = useBackup ? BackupUrl : ApiUrl;
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return $"{baseUrl}?token={Token}&t={timestamp}";
        }
        
        /// <summary>
        /// 解析API响应
        /// </summary>
        private LotteryResult ParseApiResponse(string response)
        {
            try
            {
                var data = _serializer.Deserialize<Dictionary<string, object>>(response);
                if (data == null) return null;
                
                // 处理单条记录
                if (data.ContainsKey("data"))
                {
                    var resultData = data["data"];
                    
                    // 可能是数组或对象
                    if (resultData is object[] arr && arr.Length > 0)
                    {
                        var dict = arr[0] as Dictionary<string, object>;
                        if (dict != null) return ParseResultDict(dict);
                    }
                    else if (resultData is Dictionary<string, object> dict)
                    {
                        return ParseResultDict(dict);
                    }
                }
                
                // 直接解析顶层
                return ParseResultDict(data);
            }
            catch (Exception ex)
            {
                Log($"解析开奖响应失败: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 解析结果字典
        /// </summary>
        private LotteryResult ParseResultDict(Dictionary<string, object> dict)
        {
            var result = new LotteryResult();
            
            // 期号
            if (dict.ContainsKey("issue") || dict.ContainsKey("expect") || dict.ContainsKey("period"))
            {
                result.Period = (dict.ContainsKey("issue") ? dict["issue"] :
                               dict.ContainsKey("expect") ? dict["expect"] :
                               dict["period"])?.ToString() ?? "";
            }
            
            // 开奖号码
            if (dict.ContainsKey("code") || dict.ContainsKey("opencode"))
            {
                var code = (dict.ContainsKey("code") ? dict["code"] : dict["opencode"])?.ToString() ?? "";
                var nums = code.Split(new[] { '+', ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (nums.Length >= 3)
                {
                    int.TryParse(nums[0], out int n1);
                    int.TryParse(nums[1], out int n2);
                    int.TryParse(nums[2], out int n3);
                    result.Num1 = n1;
                    result.Num2 = n2;
                    result.Num3 = n3;
                    result.Sum = n1 + n2 + n3;
                }
            }
            
            // 单独的数字字段
            if (dict.ContainsKey("n1")) { int n; if (int.TryParse(dict["n1"]?.ToString(), out n)) result.Num1 = n; }
            if (dict.ContainsKey("n2")) { int n; if (int.TryParse(dict["n2"]?.ToString(), out n)) result.Num2 = n; }
            if (dict.ContainsKey("n3")) { int n; if (int.TryParse(dict["n3"]?.ToString(), out n)) result.Num3 = n; }
            if (dict.ContainsKey("sum")) { int n; if (int.TryParse(dict["sum"]?.ToString(), out n)) result.Sum = n; }
            
            // 开奖时间
            if (dict.ContainsKey("opentime") || dict.ContainsKey("time"))
            {
                var timeStr = (dict.ContainsKey("opentime") ? dict["opentime"] : dict["time"])?.ToString();
                if (!string.IsNullOrEmpty(timeStr))
                {
                    DateTime.TryParse(timeStr, out DateTime time);
                    result.OpenTime = time;
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// 计算下一期开奖倒计时
        /// </summary>
        private int CalculateCountdown()
        {
            // 加拿大28每210秒开一期
            var now = DateTime.Now;
            var secondsInDay = (int)(now - now.Date).TotalSeconds;
            var periodSeconds = 210;
            var remaining = periodSeconds - (secondsInDay % periodSeconds);
            return remaining;
        }
        
        private void Log(string message)
        {
            Logger.Info($"[开奖] {message}");
            OnLog?.Invoke(message);
        }
        
        public void Dispose()
        {
            Stop();
            _cts?.Dispose();
        }
    }
    
    /// <summary>
    /// 开奖结果
    /// </summary>
    public class LotteryResult
    {
        public string Period { get; set; }       // 期号
        public int Num1 { get; set; }            // 第一个数字
        public int Num2 { get; set; }            // 第二个数字
        public int Num3 { get; set; }            // 第三个数字
        public int Sum { get; set; }             // 和值
        public DateTime OpenTime { get; set; }   // 开奖时间
        
        // 判断大小单双
        public bool IsBig => Sum >= 14;
        public bool IsSmall => Sum < 14;
        public bool IsOdd => Sum % 2 == 1;
        public bool IsEven => Sum % 2 == 0;
        
        // 判断特殊形态
        public bool IsLeopard => Num1 == Num2 && Num2 == Num3;  // 豹子
        public bool IsStraight => IsStraightNumbers(Num1, Num2, Num3); // 顺子
        public bool IsPair => !IsLeopard && (Num1 == Num2 || Num2 == Num3 || Num1 == Num3); // 对子
        public bool IsHalfStraight => !IsStraight && HasConsecutive(Num1, Num2, Num3); // 半顺
        
        // 判断极值
        public bool IsExtremeBig => Sum == 27;
        public bool IsExtremeSmall => Sum == 0;
        
        /// <summary>
        /// 获取结果字符串 (如 "DAD" = 大单大)
        /// </summary>
        public string GetResultString()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append(IsBig ? "D" : "X");   // 大/小
            sb.Append(IsOdd ? "D" : "S");   // 单/双
            
            // 特殊形态
            if (IsLeopard) sb.Append(" 豹");
            else if (IsStraight) sb.Append(" 顺");
            else if (IsPair) sb.Append(" 对");
            else if (IsHalfStraight) sb.Append(" 半");
            else sb.Append(" 杂");
            
            return sb.ToString();
        }
        
        /// <summary>
        /// 获取开奖消息
        /// </summary>
        public string GetOpenMessage()
        {
            return $"开:{Num1}+{Num2}+{Num3}={Sum:D2} {GetResultString()} 期{Period}期";
        }
        
        /// <summary>
        /// 检查是否中奖
        /// </summary>
        public bool IsWin(string betType)
        {
            switch (betType)
            {
                case "BIG": return IsBig;
                case "SMALL": return IsSmall;
                case "ODD": return IsOdd;
                case "EVEN": return IsEven;
                case "BIG_ODD": return IsBig && IsOdd;
                case "BIG_EVEN": return IsBig && IsEven;
                case "SMALL_ODD": return IsSmall && IsOdd;
                case "SMALL_EVEN": return IsSmall && IsEven;
                case "LEOPARD": return IsLeopard;
                case "STRAIGHT": return IsStraight;
                case "PAIR": return IsPair;
                default:
                    if (betType.StartsWith("NUM_"))
                    {
                        int num = int.Parse(betType.Substring(4));
                        return Sum == num;
                    }
                    return false;
            }
        }
        
        /// <summary>
        /// 获取赔率
        /// </summary>
        public static decimal GetOdds(string betType)
        {
            switch (betType)
            {
                case "BIG":
                case "SMALL":
                case "ODD":
                case "EVEN":
                    return 1.95m;
                case "BIG_ODD":
                case "BIG_EVEN":
                case "SMALL_ODD":
                case "SMALL_EVEN":
                    return 3.8m;
                case "LEOPARD":
                    return 59m;
                case "STRAIGHT":
                    return 11m;
                case "PAIR":
                    return 2m;
                default:
                    if (betType.StartsWith("NUM_"))
                    {
                        // 根据数字不同赔率不同
                        int num = int.Parse(betType.Substring(4));
                        if (num == 0 || num == 27) return 995m;
                        if (num == 1 || num == 26) return 69m;
                        if (num == 2 || num == 25) return 35m;
                        if (num == 3 || num == 24) return 22m;
                        if (num == 4 || num == 23) return 16m;
                        if (num == 5 || num == 22) return 12m;
                        if (num == 6 || num == 21) return 10m;
                        if (num == 7 || num == 20) return 8.4m;
                        if (num == 8 || num == 19) return 7.4m;
                        if (num == 9 || num == 18) return 6.7m;
                        if (num == 10 || num == 17) return 6.2m;
                        if (num == 11 || num == 16) return 5.8m;
                        if (num == 12 || num == 15) return 5.7m;
                        if (num == 13 || num == 14) return 5.5m;
                    }
                    return 1m;
            }
        }
        
        private static bool IsStraightNumbers(int a, int b, int c)
        {
            var nums = new[] { a, b, c };
            Array.Sort(nums);
            return nums[1] == nums[0] + 1 && nums[2] == nums[1] + 1;
        }
        
        private static bool HasConsecutive(int a, int b, int c)
        {
            return Math.Abs(a - b) == 1 || Math.Abs(b - c) == 1 || Math.Abs(a - c) == 1;
        }
    }
}
