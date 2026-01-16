using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace WangShangLiaoBot.Services
{
    /// <summary>
    /// 上下分服务 - 管理玩家余额和上下分记录
    /// 基于招财狗(ZCG)软件的上下分系统实现
    /// </summary>
    public sealed class ScoreService
    {
        private static ScoreService _instance;
        public static ScoreService Instance => _instance ?? (_instance = new ScoreService());

        private readonly Dictionary<string, PlayerScore> _scores = new Dictionary<string, PlayerScore>();
        private readonly List<ScoreTransaction> _transactions = new List<ScoreTransaction>();
        private readonly object _lock = new object();
        
        /// <summary>内存中保留的最大交易记录数（防止内存泄漏）</summary>
        private const int MAX_TRANSACTIONS_IN_MEMORY = 5000;

        private ScoreService()
        {
            LoadScores();
        }

        private string ScoresPath => Path.Combine(DataService.Instance.DatabaseDir, "player-scores.ini");
        private string TransactionsPath => Path.Combine(DataService.Instance.DatabaseDir, "score-transactions.log");

        #region 玩家余额管理

        /// <summary>
        /// 获取玩家余额
        /// </summary>
        public decimal GetBalance(string playerId)
        {
            lock (_lock)
            {
                return _scores.TryGetValue(playerId, out var score) ? score.Balance : 0m;
            }
        }

        /// <summary>
        /// 获取玩家积分信息
        /// </summary>
        public PlayerScore GetPlayerScore(string playerId)
        {
            lock (_lock)
            {
                if (_scores.TryGetValue(playerId, out var score))
                    return score.Clone();
                return new PlayerScore { PlayerId = playerId, Balance = 0m };
            }
        }

        /// <summary>
        /// 设置玩家余额
        /// </summary>
        public void SetBalance(string playerId, decimal balance, string playerNick = null)
        {
            lock (_lock)
            {
                if (!_scores.TryGetValue(playerId, out var score))
                {
                    score = new PlayerScore { PlayerId = playerId };
                    _scores[playerId] = score;
                }

                score.Balance = balance;
                if (!string.IsNullOrEmpty(playerNick))
                    score.PlayerNick = playerNick;
                score.LastUpdateTime = DateTime.Now;
            }
        }

        /// <summary>
        /// 上分 (增加余额) - Deposit别名
        /// </summary>
        public decimal Deposit(string playerId, decimal amount, string reason, string operatorId = null, string playerNick = null)
        {
            return AddScore(playerId, amount, reason, operatorId, playerNick);
        }

        /// <summary>
        /// 上分 (增加余额)
        /// </summary>
        /// <returns>操作后的余额</returns>
        public decimal AddScore(string playerId, decimal amount, string reason, string operatorId = null, string playerNick = null)
        {
            lock (_lock)
            {
                if (!_scores.TryGetValue(playerId, out var score))
                {
                    score = new PlayerScore { PlayerId = playerId };
                    _scores[playerId] = score;
                }

                var balanceBefore = score.Balance;
                score.Balance += amount;
                score.TotalDeposit += amount;
                score.LastUpdateTime = DateTime.Now;

                if (!string.IsNullOrEmpty(playerNick))
                    score.PlayerNick = playerNick;

                // 记录交易
                var transaction = new ScoreTransaction
                {
                    TransactionId = Guid.NewGuid().ToString("N"),
                    PlayerId = playerId,
                    PlayerNick = playerNick ?? score.PlayerNick,
                    Type = ScoreTransactionType.Deposit,
                    Amount = amount,
                    BalanceBefore = balanceBefore,
                    BalanceAfter = score.Balance,
                    Reason = reason,
                    OperatorId = operatorId,
                    Time = DateTime.Now
                };
                AddTransactionWithLimit(transaction);

                SaveScores();
                AppendTransaction(transaction);

                return score.Balance;
            }
        }

        /// <summary>
        /// 下分 (减少余额)
        /// </summary>
        /// <returns>(是否成功, 操作后余额, 错误信息)</returns>
        public (bool success, decimal balance, string error) DeductScore(
            string playerId, decimal amount, string reason, 
            string operatorId = null, bool allowNegative = false)
        {
            lock (_lock)
            {
                if (!_scores.TryGetValue(playerId, out var score))
                {
                    return (false, 0m, "玩家不存在");
                }

                if (!allowNegative && score.Balance < amount)
                {
                    return (false, score.Balance, "余额不足");
                }

                var balanceBefore = score.Balance;
                score.Balance -= amount;
                score.TotalWithdraw += amount;
                score.LastUpdateTime = DateTime.Now;

                // 记录交易
                var transaction = new ScoreTransaction
                {
                    TransactionId = Guid.NewGuid().ToString("N"),
                    PlayerId = playerId,
                    PlayerNick = score.PlayerNick,
                    Type = ScoreTransactionType.Withdraw,
                    Amount = amount,
                    BalanceBefore = balanceBefore,
                    BalanceAfter = score.Balance,
                    Reason = reason,
                    OperatorId = operatorId,
                    Time = DateTime.Now
                };
                AddTransactionWithLimit(transaction);

                SaveScores();
                AppendTransaction(transaction);

                return (true, score.Balance, null);
            }
        }

        /// <summary>
        /// 扣除下注金额
        /// </summary>
        public (bool success, decimal balance, string error) DeductBet(string playerId, decimal amount, string period)
        {
            return DeductScore(playerId, amount, $"下注扣除-第{period}期", allowNegative: false);
        }

        /// <summary>
        /// 结算中奖金额
        /// </summary>
        public decimal AddWinnings(string playerId, decimal amount, string period)
        {
            return AddScore(playerId, amount, $"中奖结算-第{period}期");
        }

        /// <summary>
        /// 添加回水
        /// </summary>
        public decimal AddRebate(string playerId, decimal amount, string reason = "回水")
        {
            return AddScore(playerId, amount, reason);
        }

        #endregion

        #region 统计查询

        /// <summary>
        /// 获取玩家今日统计
        /// </summary>
        public PlayerDailyStats GetTodayStats(string playerId)
        {
            lock (_lock)
            {
                var today = DateTime.Today;
                var todayTransactions = _transactions.Where(t =>
                    t.PlayerId == playerId && t.Time.Date == today).ToList();

                return new PlayerDailyStats
                {
                    PlayerId = playerId,
                    Date = today,
                    TotalDeposit = todayTransactions.Where(t => t.Type == ScoreTransactionType.Deposit).Sum(t => t.Amount),
                    TotalWithdraw = todayTransactions.Where(t => t.Type == ScoreTransactionType.Withdraw).Sum(t => t.Amount),
                    TotalBet = todayTransactions.Where(t => t.Type == ScoreTransactionType.Bet).Sum(t => t.Amount),
                    TotalWin = todayTransactions.Where(t => t.Type == ScoreTransactionType.Win).Sum(t => t.Amount),
                    TotalRebate = todayTransactions.Where(t => t.Type == ScoreTransactionType.Rebate).Sum(t => t.Amount),
                    BetCount = todayTransactions.Count(t => t.Type == ScoreTransactionType.Bet)
                };
            }
        }

        /// <summary>
        /// 获取今日下注次数
        /// </summary>
        public int GetTodayBetCount(string playerId)
        {
            lock (_lock)
            {
                var today = DateTime.Today;
                return _transactions.Count(t =>
                    t.PlayerId == playerId &&
                    t.Time.Date == today &&
                    t.Type == ScoreTransactionType.Bet);
            }
        }

        /// <summary>
        /// 获取今日流水
        /// </summary>
        public decimal GetTodayTurnover(string playerId)
        {
            lock (_lock)
            {
                var today = DateTime.Today;
                return _transactions.Where(t =>
                    t.PlayerId == playerId &&
                    t.Time.Date == today &&
                    t.Type == ScoreTransactionType.Bet).Sum(t => t.Amount);
            }
        }

        /// <summary>
        /// 获取所有有余额的玩家
        /// </summary>
        public List<PlayerScore> GetAllPlayersWithBalance()
        {
            lock (_lock)
            {
                return _scores.Values.Where(s => s.Balance > 0).Select(s => s.Clone()).ToList();
            }
        }

        /// <summary>
        /// 获取玩家交易记录
        /// </summary>
        public List<ScoreTransaction> GetTransactions(string playerId, DateTime? startTime = null, DateTime? endTime = null)
        {
            lock (_lock)
            {
                var query = _transactions.Where(t => t.PlayerId == playerId);
                
                if (startTime.HasValue)
                    query = query.Where(t => t.Time >= startTime.Value);
                if (endTime.HasValue)
                    query = query.Where(t => t.Time < endTime.Value);
                
                return query.ToList();
            }
        }

        /// <summary>
        /// 获取所有玩家
        /// </summary>
        public List<PlayerScore> GetAllPlayers()
        {
            lock (_lock)
            {
                return _scores.Values.Select(s => s.Clone()).ToList();
            }
        }

        /// <summary>
        /// 添加交易记录（带内存限制，防止内存泄漏）
        /// </summary>
        private void AddTransactionWithLimit(ScoreTransaction transaction)
        {
            _transactions.Add(transaction);
            
            // 当内存中交易记录超过限制时，移除最旧的记录
            if (_transactions.Count > MAX_TRANSACTIONS_IN_MEMORY)
            {
                var toRemove = _transactions.Count - MAX_TRANSACTIONS_IN_MEMORY;
                _transactions.RemoveRange(0, toRemove);
                Logger.Info($"[ScoreService] 清理内存中过期交易记录: {toRemove}条");
            }
        }

        #endregion

        #region 数据持久化

        private void LoadScores()
        {
            lock (_lock)
            {
                try
                {
                    if (!File.Exists(ScoresPath)) return;

                    var lines = File.ReadAllLines(ScoresPath, Encoding.UTF8);
                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;

                        var parts = line.Split('|');
                        if (parts.Length < 2) continue;

                        var playerId = parts[0];
                        if (!decimal.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var balance))
                            continue;

                        var score = new PlayerScore
                        {
                            PlayerId = playerId,
                            Balance = balance
                        };

                        if (parts.Length > 2) score.PlayerNick = parts[2];
                        if (parts.Length > 3 && decimal.TryParse(parts[3], NumberStyles.Any, CultureInfo.InvariantCulture, out var deposit))
                            score.TotalDeposit = deposit;
                        if (parts.Length > 4 && decimal.TryParse(parts[4], NumberStyles.Any, CultureInfo.InvariantCulture, out var withdraw))
                            score.TotalWithdraw = withdraw;

                        _scores[playerId] = score;
                    }
                }
                catch
                {
                    // ignore
                }
            }
        }

        private void SaveScores()
        {
            try
            {
                Directory.CreateDirectory(DataService.Instance.DatabaseDir);
                var sb = new StringBuilder();
                sb.AppendLine("# 玩家积分数据 - 自动生成");
                sb.AppendLine("# 格式: PlayerId|Balance|Nick|TotalDeposit|TotalWithdraw");

                foreach (var score in _scores.Values)
                {
                    sb.AppendLine($"{score.PlayerId}|{score.Balance.ToString(CultureInfo.InvariantCulture)}|{score.PlayerNick ?? ""}|{score.TotalDeposit.ToString(CultureInfo.InvariantCulture)}|{score.TotalWithdraw.ToString(CultureInfo.InvariantCulture)}");
                }

                File.WriteAllText(ScoresPath, sb.ToString(), Encoding.UTF8);
            }
            catch
            {
                // ignore
            }
        }

        private void AppendTransaction(ScoreTransaction transaction)
        {
            try
            {
                Directory.CreateDirectory(DataService.Instance.DatabaseDir);
                var line = $"{transaction.Time:yyyy-MM-dd HH:mm:ss}|{transaction.TransactionId}|{transaction.PlayerId}|{transaction.Type}|{transaction.Amount.ToString(CultureInfo.InvariantCulture)}|{transaction.BalanceBefore.ToString(CultureInfo.InvariantCulture)}|{transaction.BalanceAfter.ToString(CultureInfo.InvariantCulture)}|{transaction.Reason}\n";
                File.AppendAllText(TransactionsPath, line, Encoding.UTF8);
            }
            catch
            {
                // ignore
            }
        }

        #endregion
    }

    #region 数据模型

    /// <summary>
    /// 玩家积分信息
    /// </summary>
    public class PlayerScore
    {
        public string PlayerId { get; set; }
        public string PlayerNick { get; set; }
        public decimal Balance { get; set; }
        public decimal TotalDeposit { get; set; }
        public decimal TotalWithdraw { get; set; }
        public DateTime LastUpdateTime { get; set; }

        public PlayerScore Clone()
        {
            return new PlayerScore
            {
                PlayerId = PlayerId,
                PlayerNick = PlayerNick,
                Balance = Balance,
                TotalDeposit = TotalDeposit,
                TotalWithdraw = TotalWithdraw,
                LastUpdateTime = LastUpdateTime
            };
        }
    }

    /// <summary>
    /// 积分交易记录
    /// </summary>
    public class ScoreTransaction
    {
        public string TransactionId { get; set; }
        public string PlayerId { get; set; }
        public string PlayerNick { get; set; }
        public ScoreTransactionType Type { get; set; }
        public decimal Amount { get; set; }
        public decimal BalanceBefore { get; set; }
        public decimal BalanceAfter { get; set; }
        public string Reason { get; set; }
        public string OperatorId { get; set; }
        public DateTime Time { get; set; }
    }

    /// <summary>
    /// 交易类型
    /// </summary>
    public enum ScoreTransactionType
    {
        Deposit,    // 上分
        Withdraw,   // 下分
        Bet,        // 下注扣除
        Win,        // 中奖
        Rebate,     // 回水
        Bonus,      // 奖励
        Adjust      // 调整
    }

    /// <summary>
    /// 玩家每日统计
    /// </summary>
    public class PlayerDailyStats
    {
        public string PlayerId { get; set; }
        public DateTime Date { get; set; }
        public decimal TotalDeposit { get; set; }
        public decimal TotalWithdraw { get; set; }
        public decimal TotalBet { get; set; }
        public decimal TotalWin { get; set; }
        public decimal TotalRebate { get; set; }
        public int BetCount { get; set; }

        public decimal NetProfit => TotalWin - TotalBet;
    }

    #endregion
}
