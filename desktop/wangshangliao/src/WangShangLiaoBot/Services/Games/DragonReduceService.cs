using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace WangShangLiaoBot.Services
{
    /// <summary>
    /// 长龙减赔服务 - 基于招财狗(ZCG)的长龙玩法
    /// 当某个结果连续开出时，自动减少该结果的赔率
    /// </summary>
    public sealed class DragonReduceService
    {
        private static DragonReduceService _instance;
        public static DragonReduceService Instance => _instance ?? (_instance = new DragonReduceService());

        private DragonReduceConfig _config;
        private readonly Dictionary<string, DragonTracker> _trackers = new Dictionary<string, DragonTracker>();
        private readonly object _lock = new object();

        // 事件
        public event Action<string, string, int, decimal> OnDragonReduce; // category, result, count, reducedOdds
        public event Action<string> OnLog;

        private DragonReduceService()
        {
            LoadConfig();
        }

        private string ConfigPath => Path.Combine(DataService.Instance.DatabaseDir, "dragon-reduce-config.ini");

        #region 配置管理

        public DragonReduceConfig GetConfig()
        {
            lock (_lock)
            {
                return _config ?? DragonReduceConfig.CreateDefault();
            }
        }

        public void SaveConfig(DragonReduceConfig config)
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
                    _config = DragonReduceConfig.CreateDefault();
                    return;
                }

                var config = new DragonReduceConfig();
                var lines = File.ReadAllLines(ConfigPath, Encoding.UTF8);

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;

                    var idx = line.IndexOf('=');
                    if (idx <= 0) continue;

                    var key = line.Substring(0, idx).Trim();
                    var value = line.Substring(idx + 1).Trim();

                    switch (key)
                    {
                        case "Enabled":
                            config.Enabled = value == "true" || value == "1" || value == "真";
                            break;
                        case "Rules":
                            config.Rules = ParseRules(value);
                            break;
                        case "TrackedCategories":
                            config.TrackedCategories = value.Split(new[] { '|', ',' }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(s => s.Trim())
                                .ToList();
                            break;
                    }
                }

                _config = config;
            }
            catch
            {
                _config = DragonReduceConfig.CreateDefault();
            }
        }

        private List<DragonRule> ParseRules(string value)
        {
            var rules = new List<DragonRule>();
            var parts = value.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in parts)
            {
                var idx = part.IndexOf('=');
                if (idx <= 0) continue;

                if (int.TryParse(part.Substring(0, idx).Trim(), out var count) &&
                    decimal.TryParse(part.Substring(idx + 1).Trim(), out var reduce))
                {
                    rules.Add(new DragonRule { ConsecutiveCount = count, ReduceOdds = reduce });
                }
            }

            return rules.OrderBy(r => r.ConsecutiveCount).ToList();
        }

        private void SaveConfigToFile(DragonReduceConfig config)
        {
            try
            {
                Directory.CreateDirectory(DataService.Instance.DatabaseDir);
                var sb = new StringBuilder();
                sb.AppendLine("# 长龙减赔配置 - 自动生成");
                sb.AppendLine($"Enabled={config.Enabled}");
                sb.AppendLine($"Rules={string.Join("|", config.Rules.Select(r => $"{r.ConsecutiveCount}={r.ReduceOdds}"))}");
                sb.AppendLine($"TrackedCategories={string.Join("|", config.TrackedCategories)}");

                File.WriteAllText(ConfigPath, sb.ToString(), Encoding.UTF8);
            }
            catch { }
        }

        #endregion

        #region 长龙跟踪

        /// <summary>
        /// 记录开奖结果
        /// </summary>
        public void RecordResult(int sum, string bigSmall, string oddEven, string special)
        {
            var config = GetConfig();
            if (!config.Enabled) return;

            lock (_lock)
            {
                // 跟踪大小
                if (config.TrackedCategories.Contains("大小") || config.TrackedCategories.Contains("BigSmall"))
                {
                    TrackCategory("大小", bigSmall);
                }

                // 跟踪单双
                if (config.TrackedCategories.Contains("单双") || config.TrackedCategories.Contains("OddEven"))
                {
                    TrackCategory("单双", oddEven);
                }

                // 跟踪组合 (大单、大双、小单、小双)
                if (config.TrackedCategories.Contains("组合") || config.TrackedCategories.Contains("Combo"))
                {
                    var combo = bigSmall + oddEven;
                    TrackCategory("组合", combo);
                }

                // 跟踪特殊 (豹顺对)
                if ((config.TrackedCategories.Contains("特殊") || config.TrackedCategories.Contains("Special"))
                    && !string.IsNullOrEmpty(special))
                {
                    TrackCategory("特殊", special);
                }
            }
        }

        private void TrackCategory(string category, string result)
        {
            if (string.IsNullOrEmpty(result)) return;

            var key = $"{category}_{result}";
            var oppositeKey = GetOppositeKey(category, result);

            // 获取或创建追踪器
            if (!_trackers.TryGetValue(key, out var tracker))
            {
                tracker = new DragonTracker { Category = category, Result = result };
                _trackers[key] = tracker;
            }

            // 增加连续计数
            tracker.ConsecutiveCount++;
            tracker.LastUpdateTime = DateTime.Now;
            tracker.History.Add(new DragonHistoryEntry
            {
                Result = result,
                Time = DateTime.Now,
                IsMatch = true
            });

            // 重置对立结果的计数
            if (!string.IsNullOrEmpty(oppositeKey) && _trackers.TryGetValue(oppositeKey, out var oppositeTracker))
            {
                oppositeTracker.ConsecutiveCount = 0;
            }

            // 检查是否触发减赔
            var reduction = GetOddsReduction(tracker.ConsecutiveCount);
            if (reduction > 0)
            {
                Log($"[长龙] {category}-{result} 连开{tracker.ConsecutiveCount}次，减赔{reduction:F2}");
                OnDragonReduce?.Invoke(category, result, tracker.ConsecutiveCount, reduction);
            }

            // 清理历史 (保留最近100条)
            if (tracker.History.Count > 100)
            {
                tracker.History = tracker.History.Skip(tracker.History.Count - 100).ToList();
            }
        }

        private string GetOppositeKey(string category, string result)
        {
            switch (category)
            {
                case "大小":
                    return result == "大" ? "大小_小" : "大小_大";
                case "单双":
                    return result == "单" ? "单双_双" : "单双_单";
                case "组合":
                    // 大单<->小双, 大双<->小单
                    if (result == "大单") return "组合_小双";
                    if (result == "小双") return "组合_大单";
                    if (result == "大双") return "组合_小单";
                    if (result == "小单") return "组合_大双";
                    break;
            }
            return null;
        }

        #endregion

        #region 赔率计算

        /// <summary>
        /// 获取减赔值
        /// </summary>
        public decimal GetOddsReduction(int consecutiveCount)
        {
            var config = GetConfig();
            if (!config.Enabled) return 0;

            decimal reduction = 0;
            foreach (var rule in config.Rules)
            {
                if (consecutiveCount >= rule.ConsecutiveCount)
                {
                    reduction = rule.ReduceOdds;
                }
            }

            return reduction;
        }

        /// <summary>
        /// 获取调整后的赔率
        /// </summary>
        public decimal GetAdjustedOdds(string category, string result, decimal baseOdds)
        {
            var config = GetConfig();
            if (!config.Enabled) return baseOdds;

            var key = $"{category}_{result}";

            lock (_lock)
            {
                if (_trackers.TryGetValue(key, out var tracker))
                {
                    var reduction = GetOddsReduction(tracker.ConsecutiveCount);
                    var adjustedOdds = baseOdds - reduction;
                    return Math.Max(adjustedOdds, 1.0m); // 最低1倍
                }
            }

            return baseOdds;
        }

        /// <summary>
        /// 获取当前长龙状态
        /// </summary>
        public List<DragonStatus> GetCurrentDragons()
        {
            var result = new List<DragonStatus>();
            var config = GetConfig();

            lock (_lock)
            {
                foreach (var tracker in _trackers.Values.Where(t => t.ConsecutiveCount >= 3))
                {
                    var reduction = GetOddsReduction(tracker.ConsecutiveCount);
                    result.Add(new DragonStatus
                    {
                        Category = tracker.Category,
                        Result = tracker.Result,
                        ConsecutiveCount = tracker.ConsecutiveCount,
                        OddsReduction = reduction,
                        LastUpdateTime = tracker.LastUpdateTime
                    });
                }
            }

            return result.OrderByDescending(d => d.ConsecutiveCount).ToList();
        }

        /// <summary>
        /// 获取历史记录
        /// </summary>
        public string GetHistoryString(string category, int count = 20)
        {
            lock (_lock)
            {
                var trackers = _trackers.Values
                    .Where(t => t.Category == category)
                    .ToList();

                if (trackers.Count == 0) return "";

                var allHistory = trackers
                    .SelectMany(t => t.History)
                    .OrderByDescending(h => h.Time)
                    .Take(count)
                    .Select(h => h.Result.Substring(0, 1))
                    .Reverse();

                return string.Join("", allHistory);
            }
        }

        /// <summary>
        /// 重置统计
        /// </summary>
        public void Reset()
        {
            lock (_lock)
            {
                _trackers.Clear();
                Log("[长龙] 统计已重置");
            }
        }

        #endregion

        private void Log(string message)
        {
            OnLog?.Invoke(message);
            Logger.Info(message);
        }
    }

    #region 长龙配置和模型

    /// <summary>
    /// 长龙减赔配置
    /// </summary>
    public class DragonReduceConfig
    {
        /// <summary>是否启用</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>减赔规则</summary>
        public List<DragonRule> Rules { get; set; } = new List<DragonRule>();

        /// <summary>跟踪的分类</summary>
        public List<string> TrackedCategories { get; set; } = new List<string>();

        public static DragonReduceConfig CreateDefault()
        {
            return new DragonReduceConfig
            {
                Enabled = true,
                Rules = new List<DragonRule>
                {
                    new DragonRule { ConsecutiveCount = 3, ReduceOdds = 0.1m },
                    new DragonRule { ConsecutiveCount = 6, ReduceOdds = 0.2m },
                    new DragonRule { ConsecutiveCount = 9, ReduceOdds = 0.3m },
                    new DragonRule { ConsecutiveCount = 12, ReduceOdds = 0.4m }
                },
                TrackedCategories = new List<string>
                {
                    "大小",
                    "单双",
                    "组合"
                }
            };
        }
    }

    /// <summary>
    /// 减赔规则
    /// </summary>
    public class DragonRule
    {
        /// <summary>连续次数</summary>
        public int ConsecutiveCount { get; set; }

        /// <summary>减少的赔率</summary>
        public decimal ReduceOdds { get; set; }
    }

    /// <summary>
    /// 长龙追踪器
    /// </summary>
    public class DragonTracker
    {
        public string Category { get; set; }
        public string Result { get; set; }
        public int ConsecutiveCount { get; set; }
        public DateTime LastUpdateTime { get; set; }
        public List<DragonHistoryEntry> History { get; set; } = new List<DragonHistoryEntry>();
    }

    /// <summary>
    /// 历史记录条目
    /// </summary>
    public class DragonHistoryEntry
    {
        public string Result { get; set; }
        public DateTime Time { get; set; }
        public bool IsMatch { get; set; }
    }

    /// <summary>
    /// 长龙状态
    /// </summary>
    public class DragonStatus
    {
        public string Category { get; set; }
        public string Result { get; set; }
        public int ConsecutiveCount { get; set; }
        public decimal OddsReduction { get; set; }
        public DateTime LastUpdateTime { get; set; }
    }

    #endregion
}
