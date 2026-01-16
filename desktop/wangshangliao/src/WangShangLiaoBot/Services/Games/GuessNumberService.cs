using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace WangShangLiaoBot.Services
{
    /// <summary>
    /// 猜数字游戏服务 - 基于招财狗(ZCG)的猜数字系统
    /// 玩家猜中开奖号码可获得奖励积分
    /// </summary>
    public sealed class GuessNumberService
    {
        private static GuessNumberService _instance;
        public static GuessNumberService Instance => _instance ?? (_instance = new GuessNumberService());

        private GuessNumberConfig _config;
        private readonly Dictionary<string, PeriodGuesses> _periodGuesses = new Dictionary<string, PeriodGuesses>();
        private readonly object _lock = new object();

        // 事件
        public event Action<string, string, int, decimal> OnGuessSuccess; // playerId, nick, number, reward
        public event Action<string> OnLog;

        private GuessNumberService()
        {
            LoadConfig();
        }

        private string ConfigPath => Path.Combine(DataService.Instance.DatabaseDir, "guess-number-config.ini");

        #region 配置管理

        public GuessNumberConfig GetConfig()
        {
            lock (_lock)
            {
                return _config ?? GuessNumberConfig.CreateDefault();
            }
        }

        public void SaveConfig(GuessNumberConfig config)
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
                    _config = GuessNumberConfig.CreateDefault();
                    return;
                }

                var config = new GuessNumberConfig();
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
                        case "ShowWinner":
                            config.ShowWinner = value == "true" || value == "1" || value == "真";
                            break;
                        case "ForbiddenNumbers":
                            config.ForbiddenNumbers = value.Split(new[] { '|', ',' }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(s => int.TryParse(s.Trim(), out var n) ? n : -1)
                                .Where(n => n >= 0 && n <= 27)
                                .ToList();
                            break;
                        case "RewardRules":
                            // 格式: 5000=588|1000=188|500=20|100=8
                            config.RewardRules = ParseRewardRules(value);
                            break;
                        case "MaxGuessPerPeriod":
                            if (int.TryParse(value, out var max)) config.MaxGuessPerPeriod = max;
                            break;
                        case "Keyword":
                            config.Keyword = value;
                            break;
                    }
                }

                _config = config;
            }
            catch
            {
                _config = GuessNumberConfig.CreateDefault();
            }
        }

        private List<RewardRule> ParseRewardRules(string value)
        {
            var rules = new List<RewardRule>();
            var parts = value.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in parts)
            {
                var idx = part.IndexOf('=');
                if (idx <= 0) continue;

                if (decimal.TryParse(part.Substring(0, idx).Trim(), out var minBalance) &&
                    decimal.TryParse(part.Substring(idx + 1).Trim(), out var reward))
                {
                    rules.Add(new RewardRule { MinBalance = minBalance, Reward = reward });
                }
            }

            // 按余额从高到低排序
            return rules.OrderByDescending(r => r.MinBalance).ToList();
        }

        private void SaveConfigToFile(GuessNumberConfig config)
        {
            try
            {
                Directory.CreateDirectory(DataService.Instance.DatabaseDir);
                var sb = new StringBuilder();
                sb.AppendLine("# 猜数字配置 - 自动生成");
                sb.AppendLine($"Enabled={config.Enabled}");
                sb.AppendLine($"ShowWinner={config.ShowWinner}");
                sb.AppendLine($"ForbiddenNumbers={string.Join("|", config.ForbiddenNumbers)}");
                sb.AppendLine($"RewardRules={string.Join("|", config.RewardRules.Select(r => $"{r.MinBalance}={r.Reward}"))}");
                sb.AppendLine($"MaxGuessPerPeriod={config.MaxGuessPerPeriod}");
                sb.AppendLine($"Keyword={config.Keyword}");

                File.WriteAllText(ConfigPath, sb.ToString(), Encoding.UTF8);
            }
            catch { }
        }

        #endregion

        #region 猜数字逻辑

        /// <summary>
        /// 解析猜数字消息
        /// </summary>
        public GuessResult TryGuess(string playerId, string playerNick, string message, string periodNumber)
        {
            var config = GetConfig();
            if (!config.Enabled)
            {
                return new GuessResult { Success = false, Message = "猜数字功能未开启" };
            }

            // 检查关键词
            if (!string.IsNullOrEmpty(config.Keyword))
            {
                if (!message.Contains(config.Keyword))
                    return new GuessResult { Success = false };
            }

            // 提取数字
            var numbers = ExtractGuessNumbers(message, config.Keyword);
            if (numbers.Count == 0)
            {
                return new GuessResult { Success = false };
            }

            // 检查禁止数字
            var forbidden = numbers.FirstOrDefault(n => config.ForbiddenNumbers.Contains(n));
            if (forbidden != 0 || config.ForbiddenNumbers.Contains(0))
            {
                if (numbers.Any(n => config.ForbiddenNumbers.Contains(n)))
                {
                    return new GuessResult
                    {
                        Success = false,
                        Message = $"禁止猜测数字: {string.Join(",", config.ForbiddenNumbers)}"
                    };
                }
            }

            lock (_lock)
            {
                // 获取或创建期号猜测记录
                if (!_periodGuesses.TryGetValue(periodNumber, out var periodGuess))
                {
                    periodGuess = new PeriodGuesses { PeriodNumber = periodNumber };
                    _periodGuesses[periodNumber] = periodGuess;
                }

                // 检查是否已达到猜测次数上限
                var playerGuessCount = periodGuess.Guesses.Count(g => g.PlayerId == playerId);
                if (playerGuessCount >= config.MaxGuessPerPeriod)
                {
                    return new GuessResult
                    {
                        Success = false,
                        Message = $"本期已猜测{playerGuessCount}次，已达上限"
                    };
                }

                // 记录猜测
                foreach (var num in numbers)
                {
                    periodGuess.Guesses.Add(new GuessEntry
                    {
                        PlayerId = playerId,
                        PlayerNick = playerNick,
                        GuessNumber = num,
                        GuessTime = DateTime.Now
                    });
                }

                Log($"[猜数字] {playerNick} 第{periodNumber}期猜测: {string.Join(",", numbers)}");

                return new GuessResult
                {
                    Success = true,
                    Numbers = numbers,
                    Message = $"已记录猜测: {string.Join(",", numbers)}"
                };
            }
        }

        /// <summary>
        /// 开奖结算
        /// </summary>
        public List<GuessWinner> Settle(string periodNumber, int winningNumber)
        {
            var config = GetConfig();
            var winners = new List<GuessWinner>();

            lock (_lock)
            {
                if (!_periodGuesses.TryGetValue(periodNumber, out var periodGuess))
                {
                    return winners;
                }

                // 找出猜中的玩家
                var winningGuesses = periodGuess.Guesses
                    .Where(g => g.GuessNumber == winningNumber)
                    .GroupBy(g => g.PlayerId)
                    .ToList();

                foreach (var group in winningGuesses)
                {
                    var playerId = group.Key;
                    var firstGuess = group.First();
                    var balance = ScoreService.Instance.GetBalance(playerId);

                    // 计算奖励
                    var reward = CalculateReward(balance);
                    if (reward <= 0) continue;

                    // 发放奖励
                    ScoreService.Instance.Deposit(playerId, reward, $"猜中{winningNumber}奖励", "系统");

                    var winner = new GuessWinner
                    {
                        PlayerId = playerId,
                        PlayerNick = firstGuess.PlayerNick,
                        GuessNumber = winningNumber,
                        Reward = reward,
                        BalanceBeforeReward = balance
                    };
                    winners.Add(winner);

                    OnGuessSuccess?.Invoke(playerId, firstGuess.PlayerNick, winningNumber, reward);
                    Log($"[猜数字] {firstGuess.PlayerNick} 猜中{winningNumber}，奖励{reward:F2}");
                }

                // 清理过期数据
                CleanupOldPeriods(periodNumber);
            }

            return winners;
        }

        private List<int> ExtractGuessNumbers(string message, string keyword)
        {
            var numbers = new List<int>();

            // 移除关键词
            if (!string.IsNullOrEmpty(keyword))
            {
                message = message.Replace(keyword, " ");
            }

            // 提取所有0-27的数字
            var matches = Regex.Matches(message, @"\b(\d{1,2})\b");
            foreach (Match match in matches)
            {
                if (int.TryParse(match.Groups[1].Value, out var num) && num >= 0 && num <= 27)
                {
                    if (!numbers.Contains(num))
                        numbers.Add(num);
                }
            }

            return numbers;
        }

        private decimal CalculateReward(decimal balance)
        {
            var config = GetConfig();

            foreach (var rule in config.RewardRules)
            {
                if (balance >= rule.MinBalance)
                {
                    return rule.Reward;
                }
            }

            return 0;
        }

        private void CleanupOldPeriods(string currentPeriod)
        {
            // 只保留最近10期
            var periodNumbers = _periodGuesses.Keys
                .OrderByDescending(p => p)
                .Skip(10)
                .ToList();

            foreach (var p in periodNumbers)
            {
                _periodGuesses.Remove(p);
            }
        }

        /// <summary>
        /// 获取某期的猜测统计
        /// </summary>
        public Dictionary<int, int> GetPeriodStats(string periodNumber)
        {
            var stats = new Dictionary<int, int>();

            lock (_lock)
            {
                if (!_periodGuesses.TryGetValue(periodNumber, out var periodGuess))
                {
                    return stats;
                }

                foreach (var guess in periodGuess.Guesses)
                {
                    if (!stats.ContainsKey(guess.GuessNumber))
                        stats[guess.GuessNumber] = 0;
                    stats[guess.GuessNumber]++;
                }
            }

            return stats;
        }

        #endregion

        private void Log(string message)
        {
            OnLog?.Invoke(message);
            Logger.Info(message);
        }
    }

    #region 猜数字配置和模型

    /// <summary>
    /// 猜数字配置
    /// </summary>
    public class GuessNumberConfig
    {
        /// <summary>是否启用</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>显示猜中玩家</summary>
        public bool ShowWinner { get; set; } = true;

        /// <summary>禁止猜测的数字</summary>
        public List<int> ForbiddenNumbers { get; set; } = new List<int>();

        /// <summary>奖励规则 (按余额阶梯)</summary>
        public List<RewardRule> RewardRules { get; set; } = new List<RewardRule>();

        /// <summary>每期最大猜测次数</summary>
        public int MaxGuessPerPeriod { get; set; } = 3;

        /// <summary>触发关键词</summary>
        public string Keyword { get; set; } = "猜";

        public static GuessNumberConfig CreateDefault()
        {
            return new GuessNumberConfig
            {
                Enabled = true,
                ShowWinner = true,
                ForbiddenNumbers = new List<int> { 13, 14 },
                MaxGuessPerPeriod = 3,
                Keyword = "猜",
                RewardRules = new List<RewardRule>
                {
                    new RewardRule { MinBalance = 5000, Reward = 588 },
                    new RewardRule { MinBalance = 1000, Reward = 188 },
                    new RewardRule { MinBalance = 500, Reward = 20 },
                    new RewardRule { MinBalance = 100, Reward = 8 }
                }
            };
        }
    }

    /// <summary>
    /// 奖励规则
    /// </summary>
    public class RewardRule
    {
        public decimal MinBalance { get; set; }
        public decimal Reward { get; set; }
    }

    /// <summary>
    /// 猜测结果
    /// </summary>
    public class GuessResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public List<int> Numbers { get; set; } = new List<int>();
    }

    /// <summary>
    /// 期号猜测记录
    /// </summary>
    public class PeriodGuesses
    {
        public string PeriodNumber { get; set; }
        public List<GuessEntry> Guesses { get; set; } = new List<GuessEntry>();
    }

    /// <summary>
    /// 单条猜测记录
    /// </summary>
    public class GuessEntry
    {
        public string PlayerId { get; set; }
        public string PlayerNick { get; set; }
        public int GuessNumber { get; set; }
        public DateTime GuessTime { get; set; }
    }

    /// <summary>
    /// 猜中赢家
    /// </summary>
    public class GuessWinner
    {
        public string PlayerId { get; set; }
        public string PlayerNick { get; set; }
        public int GuessNumber { get; set; }
        public decimal Reward { get; set; }
        public decimal BalanceBeforeReward { get; set; }
    }

    #endregion
}
