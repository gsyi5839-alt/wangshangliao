using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace WangShangLiaoBot.Services
{
    /// <summary>
    /// 猜数字记录
    /// </summary>
    public class GuessRecord
    {
        public string PlayerId { get; set; }
        public int GuessNumber { get; set; }
        public int Reward { get; set; }
    }

    // Note: GuessWinner class is defined in GuessNumberService.cs

    /// <summary>
    /// 送分活动服务 - 猜数字活动管理
    /// </summary>
    public sealed class BonusActivityService
    {
        private static BonusActivityService _instance;
        public static BonusActivityService Instance => _instance ?? (_instance = new BonusActivityService());

        private BonusActivityService() { }

        // Settings keys
        private const string KeyGuessEnabled = "Bonus:GuessEnabled";
        private const string KeyForbiddenNumbers = "Bonus:ForbiddenNumbers";
        private const string KeyRewardRules = "Bonus:RewardRules";
        private const string KeyNoBigSingleSmallDouble = "Bonus:NoBigSingleSmallDouble";
        private const string KeyNoBigDoubleSmallSingle = "Bonus:NoBigDoubleSmallSingle";
        private const string KeyNoSingleBet = "Bonus:NoSingleBet";
        private const string KeySingleBetCalc = "Bonus:SingleBetCalc";
        private const string KeyManualAddScore = "Bonus:ManualAddScore";
        private const string KeyNoKillCombo = "Bonus:NoKillCombo";
        private const string KeyNoMultiCombo = "Bonus:NoMultiCombo";
        private const string KeyNoOppositeBet = "Bonus:NoOppositeBet";
        private const string KeyShowGuessResult = "Bonus:ShowGuessResult";

        /// <summary>猜数字开关</summary>
        public bool GuessEnabled
        {
            get => GetBool(KeyGuessEnabled, false);
            set => SetBool(KeyGuessEnabled, value);
        }

        /// <summary>不可猜数字（如13|14）</summary>
        public string ForbiddenNumbers
        {
            get => GetString(KeyForbiddenNumbers, "13|14");
            set => SetString(KeyForbiddenNumbers, value);
        }

        /// <summary>奖励规则（如5000=588|1000=188）</summary>
        public string RewardRules
        {
            get => GetString(KeyRewardRules, "5000=588|1000=188");
            set => SetString(KeyRewardRules, value);
        }

        /// <summary>纯大单小双下注不可猜</summary>
        public bool NoBigSingleSmallDouble
        {
            get => GetBool(KeyNoBigSingleSmallDouble, false);
            set => SetBool(KeyNoBigSingleSmallDouble, value);
        }

        /// <summary>纯大双小单下注不可猜</summary>
        public bool NoBigDoubleSmallSingle
        {
            get => GetBool(KeyNoBigDoubleSmallSingle, false);
            set => SetBool(KeyNoBigDoubleSmallSingle, value);
        }

        /// <summary>纯单注下注不可猜</summary>
        public bool NoSingleBet
        {
            get => GetBool(KeyNoSingleBet, false);
            set => SetBool(KeyNoSingleBet, value);
        }

        /// <summary>单注计算</summary>
        public bool SingleBetCalc
        {
            get => GetBool(KeySingleBetCalc, false);
            set => SetBool(KeySingleBetCalc, value);
        }

        /// <summary>手动加分</summary>
        public bool ManualAddScore
        {
            get => GetBool(KeyManualAddScore, false);
            set => SetBool(KeyManualAddScore, value);
        }

        /// <summary>杀组合不可猜</summary>
        public bool NoKillCombo
        {
            get => GetBool(KeyNoKillCombo, false);
            set => SetBool(KeyNoKillCombo, value);
        }

        /// <summary>多组合不可猜</summary>
        public bool NoMultiCombo
        {
            get => GetBool(KeyNoMultiCombo, false);
            set => SetBool(KeyNoMultiCombo, value);
        }

        /// <summary>相反下注不可猜</summary>
        public bool NoOppositeBet
        {
            get => GetBool(KeyNoOppositeBet, false);
            set => SetBool(KeyNoOppositeBet, value);
        }

        /// <summary>猜中显示</summary>
        public bool ShowGuessResult
        {
            get => GetBool(KeyShowGuessResult, false);
            set => SetBool(KeyShowGuessResult, value);
        }

        /// <summary>
        /// 解析奖励规则
        /// </summary>
        public List<Tuple<int, int>> ParseRewardRules()
        {
            var result = new List<Tuple<int, int>>();
            if (string.IsNullOrWhiteSpace(RewardRules)) return result;

            var rules = RewardRules.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var rule in rules)
            {
                var parts = rule.Split('=');
                if (parts.Length == 2 &&
                    int.TryParse(parts[0].Trim(), out var threshold) &&
                    int.TryParse(parts[1].Trim(), out var reward))
                {
                    result.Add(Tuple.Create(threshold, reward));
                }
            }
            // Sort by threshold descending (check higher thresholds first)
            return result.OrderByDescending(x => x.Item1).ToList();
        }

        /// <summary>
        /// 检查数字是否可猜
        /// </summary>
        public bool IsNumberAllowed(int number)
        {
            if (string.IsNullOrWhiteSpace(ForbiddenNumbers)) return true;

            var forbidden = ForbiddenNumbers.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            return !forbidden.Any(f => int.TryParse(f.Trim(), out var n) && n == number);
        }

        /// <summary>
        /// 计算猜中奖励
        /// </summary>
        public int CalculateReward(int betAmount)
        {
            var rules = ParseRewardRules();
            foreach (var rule in rules)
            {
                if (betAmount > rule.Item1)
                    return rule.Item2;
            }
            return 0;
        }

        /// <summary>
        /// 处理猜数字命令
        /// </summary>
        public (bool Success, string Message) ProcessGuessCommand(string senderId, string message, int betAmount)
        {
            if (!GuessEnabled)
                return (false, null);

            // Parse guess number from message (e.g. "猜12" or "猜 12")
            var match = Regex.Match(message, @"猜\s*(\d+)");
            if (!match.Success)
                return (false, null);

            if (!int.TryParse(match.Groups[1].Value, out var guessNumber))
                return (false, "猜数字格式错误");

            // Check if number is forbidden
            if (!IsNumberAllowed(guessNumber))
                return (true, $"数字 {guessNumber} 不可猜");

            // Calculate reward based on bet amount
            var reward = CalculateReward(betAmount);
            if (reward <= 0)
                return (true, "下注金额不满足猜数字条件");

            // Store the guess for later verification
            StoreGuess(senderId, guessNumber, reward);

            return (true, $"猜数字 {guessNumber} 已记录，如开奖号码和为 {guessNumber}，将获得 {reward} 分奖励");
        }

        /// <summary>
        /// 验证猜数字结果
        /// </summary>
        public List<GuessWinner> VerifyGuesses(int actualSum)
        {
            var winners = new List<GuessWinner>();
            var guesses = GetStoredGuesses();

            foreach (var guess in guesses)
            {
                if (guess.GuessNumber == actualSum)
                {
                    var winner = new GuessWinner { PlayerId = guess.PlayerId, Reward = (decimal)guess.Reward };
                    winners.Add(winner);
                    
                    if (!ManualAddScore)
                    {
                        // 自动加分 - 调用ScoreService添加奖励分数
                        try
                        {
                            ScoreService.Instance.AddScore(guess.PlayerId, guess.Reward, $"猜数字中奖-猜中{actualSum}");
                            Logger.Info($"[猜数字] 玩家 {guess.PlayerId} 猜中 {actualSum}，自动加分 {guess.Reward}");
                        }
                        catch (Exception ex)
                        {
                            Logger.Error($"[猜数字] 自动加分失败: {ex.Message}");
                        }
                    }
                }
            }

            // Clear guesses after verification
            ClearGuesses();
            return winners;
        }

        // In-memory storage for current round guesses
        private readonly List<GuessRecord> _currentGuesses = new List<GuessRecord>();
        private readonly object _guessLock = new object();

        private void StoreGuess(string playerId, int guessNumber, int reward)
        {
            lock (_guessLock)
            {
                // Remove existing guess from same player
                _currentGuesses.RemoveAll(x => x.PlayerId == playerId);
                _currentGuesses.Add(new GuessRecord { PlayerId = playerId, GuessNumber = guessNumber, Reward = reward });
            }
        }

        private List<GuessRecord> GetStoredGuesses()
        {
            lock (_guessLock)
            {
                return _currentGuesses.ToList();
            }
        }

        private void ClearGuesses()
        {
            lock (_guessLock)
            {
                _currentGuesses.Clear();
            }
        }

        private bool GetBool(string key, bool defaultValue)
        {
            var s = DataService.Instance.GetSetting(key, defaultValue ? "1" : "0");
            return s == "1" || (bool.TryParse(s, out var b) && b);
        }

        private void SetBool(string key, bool value)
        {
            DataService.Instance.SaveSetting(key, value ? "1" : "0");
        }

        private string GetString(string key, string defaultValue)
        {
            return DataService.Instance.GetSetting(key, defaultValue);
        }

        private void SetString(string key, string value)
        {
            DataService.Instance.SaveSetting(key, value ?? "");
        }
    }
}

