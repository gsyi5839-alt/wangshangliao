using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WSLFramework.Utils;

namespace WSLFramework.Services
{
    /// <summary>
    /// 账单结算服务 - 完全匹配ZCG的结算功能和账单格式
    /// </summary>
    public class SettlementService
    {
        private static readonly Lazy<SettlementService> _lazy = 
            new Lazy<SettlementService>(() => new SettlementService());
        public static SettlementService Instance => _lazy.Value;
        
        // 待结算下注
        private readonly ConcurrentDictionary<string, List<PendingBet>> _pendingBets;
        
        // 结算历史
        private readonly ConcurrentBag<SettlementRecord> _settlementHistory;
        
        // 服务依赖
        private ScoreService _scoreService;
        private TimedMessageService _messageService;
        
        // 配置
        public bool AutoSettlement { get; set; } = true;   // 自动结算
        public bool SendBill { get; set; } = true;         // 发送账单
        public bool NotifyWinner { get; set; } = true;     // 通知中奖者
        public bool NotifyLoser { get; set; } = false;     // 通知未中奖者
        
        // 赔率配置
        public Dictionary<string, decimal> Odds { get; set; } = new Dictionary<string, decimal>
        {
            { "BIG", 1.95m },       // 大
            { "SMALL", 1.95m },     // 小
            { "ODD", 1.95m },       // 单
            { "EVEN", 1.95m },      // 双
            { "BIG_ODD", 2.95m },   // 大单
            { "BIG_EVEN", 2.95m },  // 大双
            { "SMALL_ODD", 2.95m }, // 小单
            { "SMALL_EVEN", 2.95m },// 小双
            { "LEOPARD", 30m },     // 豹子
            { "STRAIGHT", 6m },    // 顺子
            { "PAIR", 3m },        // 对子
            { "HALF_STRAIGHT", 2m }, // 半顺
            { "EXTREME_BIG", 9m }, // 极大
            { "EXTREME_SMALL", 9m } // 极小
        };
        
        // 事件
        public event Action<string> OnLog;
        public event Action<SettlementRecord> OnSettlementComplete;
        public event Action<string, string, int> OnPlayerWin;  // playerId, betType, winAmount
        public event Action<string, string, int> OnPlayerLose; // playerId, betType, loseAmount
        
        private SettlementService()
        {
            _pendingBets = new ConcurrentDictionary<string, List<PendingBet>>();
            _settlementHistory = new ConcurrentBag<SettlementRecord>();
        }
        
        /// <summary>
        /// 初始化服务
        /// </summary>
        public void Initialize(ScoreService scoreService, TimedMessageService messageService)
        {
            _scoreService = scoreService;
            _messageService = messageService;
        }
        
        #region 下注管理
        
        /// <summary>
        /// 添加下注
        /// </summary>
        public SettlementBetResult PlaceBet(string period, string playerId, string nickname, string betType, int amount, string groupId)
        {
            // 检查余额
            var balance = _scoreService?.GetBalance(playerId) ?? 0;
            if (balance < amount)
            {
                return new SettlementBetResult
                {
                    Success = false,
                    Message = "余额不足",
                    FormattedResponse = ZCGResponseFormatter.FormatBetInsufficientBalance(playerId, betType, amount, balance)
                };
            }
            
            // 扣除下注金额
            if (_scoreService != null)
            {
                _scoreService.DeductBet(playerId, amount);
            }
            
            // 添加到待结算列表
            var bet = new PendingBet
            {
                Period = period,
                PlayerId = playerId,
                Nickname = nickname,
                BetType = betType,
                Amount = amount,
                GroupId = groupId,
                BetTime = DateTime.Now
            };
            
            var bets = _pendingBets.GetOrAdd(period, _ => new List<PendingBet>());
            lock (bets)
            {
                bets.Add(bet);
            }
            
            var newBalance = _scoreService?.GetBalance(playerId) ?? 0;
            Log($"下注成功: period={period}, player={playerId}, type={betType}, amount={amount}");
            
            return new SettlementBetResult
            {
                Success = true,
                Message = "下注成功",
                NewBalance = newBalance,
                FormattedResponse = ZCGResponseFormatter.FormatBetSuccess(playerId, betType, amount, newBalance)
            };
        }
        
        /// <summary>
        /// 获取指定期数的下注列表
        /// </summary>
        public List<PendingBet> GetPendingBets(string period)
        {
            if (_pendingBets.TryGetValue(period, out var bets))
            {
                lock (bets)
                {
                    return bets.ToList();
                }
            }
            return new List<PendingBet>();
        }
        
        /// <summary>
        /// 获取玩家当期下注
        /// </summary>
        public List<PendingBet> GetPlayerBets(string period, string playerId)
        {
            var bets = GetPendingBets(period);
            return bets.Where(b => b.PlayerId == playerId).ToList();
        }
        
        /// <summary>
        /// 添加下注（简化版，不扣余额）
        /// </summary>
        public void AddBet(string playerId, string betType, int amount)
        {
            var currentPeriod = PeriodManager.CalculateCurrentPeriod();
            
            var bet = new PendingBet
            {
                Period = currentPeriod,
                PlayerId = playerId,
                Nickname = playerId,
                BetType = betType,
                Amount = amount,
                GroupId = "",
                BetTime = DateTime.Now
            };
            
            var bets = _pendingBets.GetOrAdd(currentPeriod, _ => new List<PendingBet>());
            lock (bets)
            {
                bets.Add(bet);
            }
            
            Log($"添加下注: period={currentPeriod}, player={playerId}, type={betType}, amount={amount}");
        }
        
        #endregion
        
        #region 结算
        
        /// <summary>
        /// 执行结算
        /// </summary>
        public async Task<SettlementRecord> SettleAsync(LotteryResult result)
        {
            if (result == null)
                return null;
                
            Log($"开始结算: period={result.Period}");
            
            var bets = GetPendingBets(result.Period);
            if (bets.Count == 0)
            {
                Log($"没有待结算的下注: period={result.Period}");
                return null;
            }
            
            var record = new SettlementRecord
            {
                Period = result.Period,
                Result = result,
                SettleTime = DateTime.Now,
                BetRecords = new List<BetRecord>()
            };
            
            int totalWin = 0;
            int totalLose = 0;
            
            // 按群分组
            var groupedBets = bets.GroupBy(b => b.GroupId);
            
            foreach (var group in groupedBets)
            {
                var groupId = group.Key;
                var groupBets = new List<BetRecord>();
                
                foreach (var bet in group)
                {
                    var isWin = CheckWin(bet.BetType, result);
                    var winAmount = 0;
                    
                    if (isWin)
                    {
                        var odds = GetOdds(bet.BetType);
                        winAmount = (int)(bet.Amount * odds);
                        
                        // 返还本金 + 奖金
                        if (_scoreService != null)
                        {
                            _scoreService.AddWinnings(bet.PlayerId, winAmount);
                        }
                        
                        totalWin += winAmount;
                        OnPlayerWin?.Invoke(bet.PlayerId, bet.BetType, winAmount);
                    }
                    else
                    {
                        totalLose += bet.Amount;
                        OnPlayerLose?.Invoke(bet.PlayerId, bet.BetType, bet.Amount);
                    }
                    
                    var betRecord = new BetRecord
                    {
                        PlayerId = bet.PlayerId,
                        Nickname = bet.Nickname,
                        BetType = bet.BetType,
                        Amount = bet.Amount,
                        IsWin = isWin,
                        WinAmount = winAmount,
                        Period = result.Period,
                        Time = bet.BetTime
                    };
                    
                    groupBets.Add(betRecord);
                    record.BetRecords.Add(betRecord);
                }
                
                // 发送账单到群
                if (SendBill && _messageService != null)
                {
                    await _messageService.SendBillAsync(result, groupBets, totalWin, totalLose);
                }
            }
            
            record.TotalWin = totalWin;
            record.TotalLose = totalLose;
            record.PlayerCount = bets.Select(b => b.PlayerId).Distinct().Count();
            record.BetCount = bets.Count;
            
            // 清除已结算的下注
            _pendingBets.TryRemove(result.Period, out _);
            
            // 保存结算记录
            _settlementHistory.Add(record);
            
            OnSettlementComplete?.Invoke(record);
            Log($"结算完成: period={result.Period}, players={record.PlayerCount}, totalWin={totalWin}, totalLose={totalLose}");
            
            return record;
        }
        
        /// <summary>
        /// 检查是否中奖
        /// </summary>
        public bool CheckWin(string betType, LotteryResult result)
        {
            if (result == null)
                return false;
                
            var sum = result.Sum;
            
            switch (betType)
            {
                case "BIG":
                    return sum >= 14;
                case "SMALL":
                    return sum <= 13;
                case "ODD":
                    return sum % 2 == 1;
                case "EVEN":
                    return sum % 2 == 0;
                case "BIG_ODD":
                    return sum >= 14 && sum % 2 == 1;
                case "BIG_EVEN":
                    return sum >= 14 && sum % 2 == 0;
                case "SMALL_ODD":
                    return sum <= 13 && sum % 2 == 1;
                case "SMALL_EVEN":
                    return sum <= 13 && sum % 2 == 0;
                case "LEOPARD":
                    return result.IsLeopard;
                case "STRAIGHT":
                    return result.IsStraight;
                case "PAIR":
                    return result.IsPair;
                case "HALF_STRAIGHT":
                    return result.IsHalfStraight;
                case "EXTREME_BIG":
                    return sum >= 22;
                case "EXTREME_SMALL":
                    return sum <= 5;
                default:
                    // 数字下注
                    if (betType.StartsWith("NUM_"))
                    {
                        if (int.TryParse(betType.Substring(4), out var num))
                        {
                            return sum == num;
                        }
                    }
                    return false;
            }
        }
        
        /// <summary>
        /// 获取赔率
        /// </summary>
        public decimal GetOdds(string betType)
        {
            if (Odds.TryGetValue(betType, out var odds))
                return odds;
                
            // 数字下注默认赔率
            if (betType.StartsWith("NUM_"))
            {
                if (int.TryParse(betType.Substring(4), out var num))
                {
                    // 特殊数字赔率
                    if (num == 0 || num == 27)
                        return 50m;
                    if (num == 1 || num == 26)
                        return 30m;
                    if (num == 2 || num == 25)
                        return 20m;
                    if (num >= 3 && num <= 24)
                        return 9m;
                }
            }
            
            return 1.95m;
        }
        
        #endregion
        
        #region 账单格式化
        
        /// <summary>
        /// 生成完整账单文本 - ZCG格式
        /// </summary>
        public string GenerateBillText(LotteryResult result, List<BetRecord> bets)
        {
            var sb = new StringBuilder();
            
            // 开奖结果头部
            var sum = result.Sum;
            var resultCode = GetResultCode(result);
            var shapeCode = GetShapeCode(result);
            
            sb.AppendLine($"開:{result.Num1} + {result.Num2} + {result.Num3} = {sum:D2} {resultCode} {shapeCode}");
            sb.AppendLine($"人數:{bets.Select(b => b.PlayerId).Distinct().Count()}  总分:{bets.Sum(b => b.IsWin ? b.WinAmount - b.Amount : -b.Amount)}");
            sb.AppendLine("----------------------");
            
            // 下注明细
            foreach (var bet in bets)
            {
                var betTypeDisplay = GetBetTypeDisplay(bet.BetType);
                var status = bet.IsWin ? "✓" : "✗";
                var change = bet.IsWin ? $"+{bet.WinAmount}" : $"-{bet.Amount}";
                
                sb.AppendLine($"{bet.Nickname}: {betTypeDisplay}{bet.Amount} {status} {change}");
            }
            
            sb.AppendLine("----------------------");
            
            return sb.ToString();
        }
        
        /// <summary>
        /// 获取结果代码
        /// </summary>
        private string GetResultCode(LotteryResult result)
        {
            var sb = new StringBuilder();
            
            // D=大, X=小
            sb.Append(result.Sum >= 14 ? "D" : "X");
            
            // A=单, S=双
            sb.Append(result.Sum % 2 == 1 ? "A" : "S");
            
            // 特殊形态
            if (result.IsLeopard)
                sb.Append("B");  // 豹子
            else if (result.IsStraight)
                sb.Append("S");  // 顺子
            else if (result.IsPair)
                sb.Append("D");  // 对子
            
            return sb.ToString();
        }
        
        /// <summary>
        /// 获取形态代码
        /// </summary>
        private string GetShapeCode(LotteryResult result)
        {
            if (result.IsLeopard)
                return "BZ";  // 豹子
            if (result.IsStraight)
                return "SZ";  // 顺子
            if (result.IsPair)
                return "DZ";  // 对子
            if (result.IsHalfStraight)
                return "BS";  // 半顺
            return "--";      // 杂
        }
        
        /// <summary>
        /// 获取下注类型显示名称
        /// </summary>
        public string GetBetTypeDisplay(string betType)
        {
            switch (betType)
            {
                case "BIG": return "大";
                case "SMALL": return "小";
                case "ODD": return "单";
                case "EVEN": return "双";
                case "BIG_ODD": return "大单";
                case "BIG_EVEN": return "大双";
                case "SMALL_ODD": return "小单";
                case "SMALL_EVEN": return "小双";
                case "LEOPARD": return "豹子";
                case "STRAIGHT": return "顺子";
                case "PAIR": return "对子";
                case "HALF_STRAIGHT": return "半顺";
                case "EXTREME_BIG": return "极大";
                case "EXTREME_SMALL": return "极小";
                default:
                    if (betType.StartsWith("NUM_"))
                        return betType.Substring(4);
                    return betType;
            }
        }
        
        #endregion
        
        #region 当前期操作
        
        /// <summary>
        /// 获取当前期号
        /// </summary>
        private string GetCurrentPeriod()
        {
            return PeriodManager.CalculateCurrentPeriod();
        }
        
        /// <summary>
        /// 生成当前账单文本
        /// </summary>
        public string GenerateCurrentBillText()
        {
            var period = GetCurrentPeriod();
            var bets = GetPendingBets(period);
            
            if (bets.Count == 0)
                return null;
            
            var sb = new StringBuilder();
            sb.AppendLine($"【第 {period} 期账单】");
            sb.AppendLine($"下注人数: {bets.Select(b => b.PlayerId).Distinct().Count()}");
            sb.AppendLine($"下注总数: {bets.Count}");
            sb.AppendLine($"下注金额: {bets.Sum(b => b.Amount)}");
            sb.AppendLine("----------------------");
            
            // 按玩家分组显示
            var grouped = bets.GroupBy(b => b.PlayerId);
            foreach (var group in grouped)
            {
                var nickname = group.First().Nickname;
                var playerBets = string.Join(", ", group.Select(b => $"{GetBetTypeDisplay(b.BetType)}{b.Amount}"));
                sb.AppendLine($"{nickname}: {playerBets}");
            }
            
            sb.AppendLine("----------------------");
            return sb.ToString();
        }
        
        /// <summary>
        /// 清空当前期下注
        /// </summary>
        public void ClearCurrentPeriodBets()
        {
            var period = GetCurrentPeriod();
            _pendingBets.TryRemove(period, out _);
            Log($"已清空期号 {period} 的所有下注");
        }
        
        /// <summary>
        /// 获取当前期下注列表
        /// </summary>
        public List<PendingBet> GetCurrentPeriodBets()
        {
            var period = GetCurrentPeriod();
            return GetPendingBets(period);
        }
        
        #endregion
        
        #region 历史查询
        
        /// <summary>
        /// 获取最近N条结算历史
        /// </summary>
        public List<SettlementRecord> GetSettlementHistory(int count)
        {
            return _settlementHistory
                .OrderByDescending(s => s.SettleTime)
                .Take(count)
                .ToList();
        }
        
        /// <summary>
        /// 获取结算历史（按时间范围）
        /// </summary>
        public IEnumerable<SettlementRecord> GetSettlementHistory(DateTime? from = null, DateTime? to = null)
        {
            foreach (var record in _settlementHistory)
            {
                if (from.HasValue && record.SettleTime < from.Value)
                    continue;
                if (to.HasValue && record.SettleTime > to.Value)
                    continue;
                yield return record;
            }
        }
        
        /// <summary>
        /// 获取今日统计
        /// </summary>
        public SettlementStats GetTodayStats()
        {
            var today = DateTime.Today;
            var stats = new SettlementStats { Date = today };
            
            foreach (var record in _settlementHistory)
            {
                if (record.SettleTime.Date != today)
                    continue;
                    
                stats.TotalPeriods++;
                stats.TotalBets += record.BetCount;
                stats.TotalPlayers += record.PlayerCount;
                stats.TotalWin += record.TotalWin;
                stats.TotalLose += record.TotalLose;
            }
            
            return stats;
        }
        
        /// <summary>
        /// 获取玩家结算历史
        /// </summary>
        public IEnumerable<BetRecord> GetPlayerBetHistory(string playerId, int limit = 100)
        {
            var records = new List<BetRecord>();
            
            foreach (var settlement in _settlementHistory.OrderByDescending(s => s.SettleTime))
            {
                foreach (var bet in settlement.BetRecords.Where(b => b.PlayerId == playerId))
                {
                    records.Add(bet);
                    if (records.Count >= limit)
                        return records;
                }
            }
            
            return records;
        }
        
        #endregion
        
        private void Log(string message)
        {
            Logger.Info($"[Settlement] {message}");
            OnLog?.Invoke(message);
        }
    }
    
    /// <summary>
    /// 待结算下注
    /// </summary>
    public class PendingBet
    {
        public string Period { get; set; }
        public string PlayerId { get; set; }
        public string Nickname { get; set; }
        public string BetType { get; set; }
        public int Amount { get; set; }
        public string GroupId { get; set; }
        public DateTime BetTime { get; set; }
    }
    
    /// <summary>
    /// 结算下注结果
    /// </summary>
    public class SettlementBetResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int NewBalance { get; set; }
        public string FormattedResponse { get; set; }
    }
    
    /// <summary>
    /// 结算记录
    /// </summary>
    public class SettlementRecord
    {
        public string Period { get; set; }
        public LotteryResult Result { get; set; }
        public DateTime SettleTime { get; set; }
        public List<BetRecord> BetRecords { get; set; }
        public int PlayerCount { get; set; }
        public int BetCount { get; set; }
        public int TotalWin { get; set; }
        public int TotalLose { get; set; }
        
        public int NetProfit => TotalLose - TotalWin;  // 庄家盈利
    }
    
    /// <summary>
    /// 结算统计
    /// </summary>
    public class SettlementStats
    {
        public DateTime Date { get; set; }
        public int TotalPeriods { get; set; }
        public int TotalBets { get; set; }
        public int TotalPlayers { get; set; }
        public int TotalWin { get; set; }
        public int TotalLose { get; set; }
        public int NetProfit => TotalLose - TotalWin;
    }
}
