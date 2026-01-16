using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using WSLFramework.Utils;

namespace WSLFramework.Services
{
    /// <summary>
    /// 上下分服务 - 完全匹配招财狗上下分功能
    /// 线程安全实现，支持ZCG命令格式
    /// </summary>
    public class ScoreService
    {
        private static readonly Lazy<ScoreService> _lazy = new Lazy<ScoreService>(() => new ScoreService());
        public static ScoreService Instance => _lazy.Value;
        
        // 玩家余额 - 线程安全
        private readonly ConcurrentDictionary<string, PlayerBalance> _balances;
        
        // 上下分记录 - 线程安全
        private readonly ConcurrentBag<ScoreRecord> _records;
        
        // 待处理的上下分请求
        private readonly ConcurrentDictionary<string, ScoreRequest> _pendingRequests;
        
        // 配置
        public int MinScore { get; set; } = 10;
        public int MaxScore { get; set; } = 100000;
        public bool AutoUpEnabled { get; set; } = true;
        public bool AutoDownEnabled { get; set; } = true;
        public bool TrusteeEnabled { get; set; } = false;  // 托管上分开关
        
        // 托管列表
        private readonly ConcurrentDictionary<string, string> _trustees;  // 托管ID -> 托管昵称
        
        public event Action<string> OnLog;
        public event Action<ScoreRecord> OnScoreChanged;
        public event Action<string, string> OnUpRequest;   // playerId, formatted response
        public event Action<string, string> OnDownRequest; // playerId, formatted response
        
        private ScoreService()
        {
            _balances = new ConcurrentDictionary<string, PlayerBalance>();
            _records = new ConcurrentBag<ScoreRecord>();
            _pendingRequests = new ConcurrentDictionary<string, ScoreRequest>();
            _trustees = new ConcurrentDictionary<string, string>();
        }
        
        #region ZCG命令解析
        
        /// <summary>
        /// 解析ZCG上下分命令
        /// 上分: c100, +100, c1000, +1000
        /// 下分: 下100, -100, 下1000, -1000
        /// </summary>
        public ScoreCommand ParseZCGCommand(string content, string senderId)
        {
            if (string.IsNullOrWhiteSpace(content))
                return null;
                
            content = content.Trim();
            
            // BUG FIX: 使用 TryParse 防止溢出异常
            int amount;
            
            // 上分命令: c100 或 +100
            var upMatch = Regex.Match(content, @"^[cC](\d+)$", RegexOptions.IgnoreCase);
            if (upMatch.Success)
            {
                // BUG FIX: 使用 TryParse 并检查范围
                if (!int.TryParse(upMatch.Groups[1].Value, out amount) || amount <= 0 || amount > MaxScore)
                {
                    Log($"上分金额无效或超出范围: {upMatch.Groups[1].Value}");
                    return null;
                }
                return new ScoreCommand
                {
                    PlayerId = senderId,
                    Amount = amount,
                    IsUp = true,
                    RawCommand = content
                };
            }
            
            // 上分命令: +100
            var upMatch2 = Regex.Match(content, @"^\+(\d+)$");
            if (upMatch2.Success)
            {
                if (!int.TryParse(upMatch2.Groups[1].Value, out amount) || amount <= 0 || amount > MaxScore)
                {
                    Log($"上分金额无效或超出范围: {upMatch2.Groups[1].Value}");
                    return null;
                }
                return new ScoreCommand
                {
                    PlayerId = senderId,
                    Amount = amount,
                    IsUp = true,
                    RawCommand = content
                };
            }
            
            // 下分命令: 下100 或 x100
            var downMatch = Regex.Match(content, @"^下(\d+)$");
            if (downMatch.Success)
            {
                if (!int.TryParse(downMatch.Groups[1].Value, out amount) || amount <= 0 || amount > MaxScore)
                {
                    Log($"下分金额无效或超出范围: {downMatch.Groups[1].Value}");
                    return null;
                }
                return new ScoreCommand
                {
                    PlayerId = senderId,
                    Amount = amount,
                    IsUp = false,
                    RawCommand = content
                };
            }
            
            // 下分命令: x100 或 X100
            var downMatchX = Regex.Match(content, @"^[xX](\d+)$", RegexOptions.IgnoreCase);
            if (downMatchX.Success)
            {
                if (!int.TryParse(downMatchX.Groups[1].Value, out amount) || amount <= 0 || amount > MaxScore)
                {
                    Log($"下分金额无效或超出范围: {downMatchX.Groups[1].Value}");
                    return null;
                }
                return new ScoreCommand
                {
                    PlayerId = senderId,
                    Amount = amount,
                    IsUp = false,
                    RawCommand = content
                };
            }
            
            // 下分命令: -100
            var downMatch2 = Regex.Match(content, @"^-(\d+)$");
            if (downMatch2.Success)
            {
                if (!int.TryParse(downMatch2.Groups[1].Value, out amount) || amount <= 0 || amount > MaxScore)
                {
                    Log($"下分金额无效或超出范围: {downMatch2.Groups[1].Value}");
                    return null;
                }
                return new ScoreCommand
                {
                    PlayerId = senderId,
                    Amount = amount,
                    IsUp = false,
                    RawCommand = content
                };
            }
            
            return null;
        }
        
        /// <summary>
        /// 检查是否为余额查询命令
        /// 命令: 1, 2, 查, 余额
        /// </summary>
        public bool IsBalanceQuery(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return false;
                
            content = content.Trim();
            return content == "1" || content == "2" || content == "查" || content == "余额";
        }
        
        #endregion
        
        #region ZCG格式响应
        
        /// <summary>
        /// 格式化余额查询响应 - ZCG格式
        /// </summary>
        public string FormatBalanceResponse(string playerId, string nickname = null)
        {
            var balance = GetBalance(playerId);
            return ZCGResponseFormatter.FormatBalanceQuery(playerId, nickname ?? playerId, balance);
        }
        
        /// <summary>
        /// 格式化上分响应 - ZCG格式
        /// </summary>
        public string FormatUpResponse(string playerId, int amount, bool success, int newBalance)
        {
            if (success)
            {
                return ZCGResponseFormatter.FormatUpSuccess(playerId, amount, newBalance);
            }
            else
            {
                return ZCGResponseFormatter.FormatUpRequest(playerId, amount);
            }
        }
        
        /// <summary>
        /// 格式化下分响应 - ZCG格式
        /// </summary>
        public string FormatDownResponse(string playerId, int amount, bool success, int newBalance)
        {
            if (success)
            {
                return ZCGResponseFormatter.FormatDownSuccess(playerId, amount, newBalance);
            }
            else
            {
                var currentBalance = GetBalance(playerId);
                return ZCGResponseFormatter.FormatInsufficientBalance(playerId, amount, currentBalance);
            }
        }
        
        #endregion
        
        #region 上下分处理
        
        /// <summary>
        /// 处理上下分命令
        /// </summary>
        public ScoreResult ProcessCommand(ScoreCommand cmd, string nickname = null)
        {
            if (cmd == null)
                return new ScoreResult { Success = false, Message = "无效的上下分命令" };
                
            // 检查金额范围
            if (cmd.Amount < MinScore)
            {
                return new ScoreResult 
                { 
                    Success = false, 
                    Message = $"最低{(cmd.IsUp ? "上分" : "下分")}金额: {MinScore}",
                    FormattedResponse = $"[LQ:@{cmd.PlayerId}] 最低{(cmd.IsUp ? "上分" : "下分")}金额: {MinScore}"
                };
            }
                
            if (cmd.Amount > MaxScore)
            {
                return new ScoreResult 
                { 
                    Success = false, 
                    Message = $"最高{(cmd.IsUp ? "上分" : "下分")}金额: {MaxScore}",
                    FormattedResponse = $"[LQ:@{cmd.PlayerId}] 最高{(cmd.IsUp ? "上分" : "下分")}金额: {MaxScore}"
                };
            }
            
            if (cmd.IsUp)
            {
                return ProcessUp(cmd.PlayerId, cmd.Amount, nickname);
            }
            else
            {
                return ProcessDown(cmd.PlayerId, cmd.Amount, nickname);
            }
        }
        
        /// <summary>
        /// 处理上分
        /// </summary>
        public ScoreResult ProcessUp(string playerId, int amount, string nickname = null)
        {
            // 检查是否启用自动上分
            if (!AutoUpEnabled)
            {
                // 如果是托管成员，允许自动上分
                if (TrusteeEnabled && _trustees.ContainsKey(playerId))
                {
                    return DoUp(playerId, amount, nickname, true);
                }
                
                // 创建待处理请求
                var request = new ScoreRequest
                {
                    PlayerId = playerId,
                    Amount = amount,
                    IsUp = true,
                    RequestTime = DateTime.Now
                };
                _pendingRequests[playerId + "_UP_" + DateTime.Now.Ticks] = request;
                
                var response = ZCGResponseFormatter.FormatUpRequest(playerId, amount);
                OnUpRequest?.Invoke(playerId, response);
                
                return new ScoreResult
                {
                    Success = false,
                    Message = "自动上分未开启，已记录请求",
                    FormattedResponse = response
                };
            }
            
            return DoUp(playerId, amount, nickname, false);
        }
        
        /// <summary>
        /// 执行上分 - 修复并发安全问题
        /// </summary>
        private ScoreResult DoUp(string playerId, int amount, string nickname, bool isTrustee)
        {
            var balance = _balances.GetOrAdd(playerId, id => new PlayerBalance { PlayerId = id, Nickname = nickname });
            
            // BUG FIX: 使用锁来保证余额修改的原子性
            lock (balance)
            {
                if (nickname != null)
                    balance.Nickname = nickname;
                
                var oldBalance = balance.Balance;
                balance.Balance += amount;
                balance.TotalUp += amount;
                balance.LastUpdateTime = DateTime.Now;
                
                // 记录
                var record = new ScoreRecord
                {
                    PlayerId = playerId,
                    Nickname = nickname,
                    Amount = amount,
                    IsUp = true,
                    BalanceBefore = oldBalance,
                    BalanceAfter = balance.Balance,
                    Time = DateTime.Now,
                    IsTrustee = isTrustee
                };
                _records.Add(record);
                
                Log($"上分: 玩家={playerId}, 金额={amount}, 余额={balance.Balance}{(isTrustee ? " (托管)" : "")}");
                OnScoreChanged?.Invoke(record);
                
                string response;
                if (isTrustee)
                {
                    response = ZCGResponseFormatter.FormatTrusteeUpSuccess(playerId, amount, balance.Balance);
                }
                else
                {
                    response = ZCGResponseFormatter.FormatUpSuccess(playerId, amount, balance.Balance);
                }
                
                return new ScoreResult
                {
                    Success = true,
                    Message = "上分成功",
                    NewBalance = balance.Balance,
                    Record = record,
                    FormattedResponse = response
                };
            }
        }
        
        /// <summary>
        /// 处理下分 - 修复并发安全问题
        /// </summary>
        public ScoreResult ProcessDown(string playerId, int amount, string nickname = null)
        {
            var balance = _balances.GetOrAdd(playerId, id => new PlayerBalance { PlayerId = id, Nickname = nickname });
            
            if (nickname != null)
                balance.Nickname = nickname;
            
            // BUG FIX: 使用锁来保证余额检查和扣除的原子性
            // 避免 TOCTOU (Time-of-check Time-of-use) 并发问题
            lock (balance)
            {
                // 检查余额
                if (balance.Balance < amount)
                {
                    var response = ZCGResponseFormatter.FormatInsufficientBalance(playerId, amount, balance.Balance);
                    return new ScoreResult
                    {
                        Success = false,
                        Message = "余额不足",
                        NewBalance = balance.Balance,
                        FormattedResponse = response
                    };
                }
                
                // 检查是否启用自动下分
                if (!AutoDownEnabled)
                {
                    var request = new ScoreRequest
                    {
                        PlayerId = playerId,
                        Amount = amount,
                        IsUp = false,
                        RequestTime = DateTime.Now
                    };
                    _pendingRequests[playerId + "_DOWN_" + DateTime.Now.Ticks] = request;
                    
                    var response = ZCGResponseFormatter.FormatDownPending(playerId, amount);
                    OnDownRequest?.Invoke(playerId, response);
                    
                    return new ScoreResult
                    {
                        Success = false,
                        Message = "自动下分未开启，已记录请求",
                        FormattedResponse = response
                    };
                }
                
                var oldBalance = balance.Balance;
                balance.Balance -= amount;
                balance.TotalDown += amount;
                balance.LastUpdateTime = DateTime.Now;
                
                // 记录
                var record = new ScoreRecord
                {
                    PlayerId = playerId,
                    Nickname = nickname,
                    Amount = amount,
                    IsUp = false,
                    BalanceBefore = oldBalance,
                    BalanceAfter = balance.Balance,
                    Time = DateTime.Now
                };
                _records.Add(record);
                
                Log($"下分: 玩家={playerId}, 金额={amount}, 余额={balance.Balance}");
                OnScoreChanged?.Invoke(record);
                
                var downResponse = ZCGResponseFormatter.FormatDownSuccess(playerId, amount, balance.Balance);
                
                return new ScoreResult
                {
                    Success = true,
                    Message = "下分成功",
                    NewBalance = balance.Balance,
                    Record = record,
                    FormattedResponse = downResponse
                };
            }
        }
        
        #endregion
        
        #region 托管管理
        
        /// <summary>
        /// 添加托管
        /// </summary>
        public void AddTrustee(string playerId, string trusteeName)
        {
            _trustees[playerId] = trusteeName;
            Log($"添加托管: {playerId} -> {trusteeName}");
        }
        
        /// <summary>
        /// 移除托管
        /// </summary>
        public void RemoveTrustee(string playerId)
        {
            _trustees.TryRemove(playerId, out _);
            Log($"移除托管: {playerId}");
        }
        
        /// <summary>
        /// 是否为托管
        /// </summary>
        public bool IsTrustee(string playerId)
        {
            return _trustees.ContainsKey(playerId);
        }
        
        /// <summary>
        /// 获取所有托管
        /// </summary>
        public IEnumerable<KeyValuePair<string, string>> GetAllTrustees()
        {
            return _trustees;
        }
        
        #endregion
        
        #region 余额管理
        
        /// <summary>
        /// 获取玩家余额
        /// </summary>
        public int GetBalance(string playerId)
        {
            return _balances.TryGetValue(playerId, out var balance) ? balance.Balance : 0;
        }
        
        /// <summary>
        /// 设置玩家余额
        /// </summary>
        public void SetBalance(string playerId, int amount, string nickname = null)
        {
            var balance = _balances.GetOrAdd(playerId, id => new PlayerBalance { PlayerId = id });
            lock (balance)
            {
                balance.Balance = amount;
                balance.LastUpdateTime = DateTime.Now;
                if (nickname != null)
                    balance.Nickname = nickname;
            }
        }
        
        /// <summary>
        /// 扣除下注金额 - 修复并发安全问题
        /// </summary>
        public bool DeductBet(string playerId, int amount)
        {
            if (!_balances.TryGetValue(playerId, out var balance))
                return false;
            
            // BUG FIX: 使用锁来保证余额检查和扣除的原子性
            lock (balance)
            {
                if (balance.Balance < amount)
                    return false;
                    
                balance.Balance -= amount;
                balance.TotalBet += amount;
                balance.LastUpdateTime = DateTime.Now;
                
                return true;
            }
        }
        
        /// <summary>
        /// 添加中奖金额
        /// </summary>
        public void AddWinnings(string playerId, int amount)
        {
            var balance = _balances.GetOrAdd(playerId, id => new PlayerBalance { PlayerId = id });
            lock (balance)
            {
                balance.Balance += amount;
                balance.TotalWin += amount;
                balance.LastUpdateTime = DateTime.Now;
            }
        }
        
        /// <summary>
        /// 获取留分
        /// </summary>
        public int GetReservedScore(string playerId)
        {
            return _balances.TryGetValue(playerId, out var balance) ? balance.ReservedScore : 0;
        }
        
        /// <summary>
        /// 设置留分
        /// </summary>
        public void SetReservedScore(string playerId, int amount)
        {
            var balance = _balances.GetOrAdd(playerId, id => new PlayerBalance { PlayerId = id });
            balance.ReservedScore = amount;
            balance.LastUpdateTime = DateTime.Now;
            Log($"设置留分: {playerId} -> {amount}");
        }
        
        /// <summary>
        /// 设置下注内容
        /// </summary>
        public void SetBetContent(string playerId, string betContent)
        {
            var balance = _balances.GetOrAdd(playerId, id => new PlayerBalance { PlayerId = id });
            balance.BetContent = betContent;
            balance.LastUpdateTime = DateTime.Now;
        }
        
        /// <summary>
        /// 更新群名片
        /// </summary>
        public void UpdateGroupCard(string playerId, string groupCard)
        {
            var balance = _balances.GetOrAdd(playerId, id => new PlayerBalance { PlayerId = id });
            balance.GroupCard = groupCard;
            balance.LastUpdateTime = DateTime.Now;
        }
        
        /// <summary>
        /// 获取玩家信息
        /// </summary>
        public PlayerBalance GetPlayerBalance(string playerId)
        {
            _balances.TryGetValue(playerId, out var balance);
            return balance;
        }
        
        /// <summary>
        /// 获取所有玩家
        /// </summary>
        public IEnumerable<PlayerBalance> GetAllPlayers()
        {
            return _balances.Values;
        }
        
        #endregion
        
        #region 记录查询
        
        /// <summary>
        /// 获取上下分记录
        /// </summary>
        public IEnumerable<ScoreRecord> GetRecords(string playerId = null, DateTime? from = null, DateTime? to = null)
        {
            foreach (var record in _records)
            {
                if (!string.IsNullOrEmpty(playerId) && record.PlayerId != playerId)
                    continue;
                if (from.HasValue && record.Time < from.Value)
                    continue;
                if (to.HasValue && record.Time > to.Value)
                    continue;
                    
                yield return record;
            }
        }
        
        /// <summary>
        /// 获取今日统计
        /// </summary>
        public DailyStats GetTodayStats()
        {
            var today = DateTime.Today;
            var stats = new DailyStats { Date = today };
            
            foreach (var record in _records)
            {
                if (record.Time.Date != today) continue;
                
                if (record.IsUp)
                {
                    stats.TotalUp += record.Amount;
                    stats.UpCount++;
                }
                else
                {
                    stats.TotalDown += record.Amount;
                    stats.DownCount++;
                }
            }
            
            return stats;
        }
        
        /// <summary>
        /// 获取待处理请求
        /// </summary>
        public IEnumerable<ScoreRequest> GetPendingRequests()
        {
            return _pendingRequests.Values;
        }
        
        /// <summary>
        /// 批准待处理请求
        /// </summary>
        public ScoreResult ApproveRequest(string requestId)
        {
            if (_pendingRequests.TryRemove(requestId, out var request))
            {
                if (request.IsUp)
                {
                    return DoUp(request.PlayerId, request.Amount, null, false);
                }
                else
                {
                    return ProcessDown(request.PlayerId, request.Amount);
                }
            }
            return new ScoreResult { Success = false, Message = "请求不存在" };
        }
        
        #endregion
        
        #region 数据清理
        
        /// <summary>
        /// 清除玩家数据
        /// </summary>
        public void ClearPlayer(string playerId)
        {
            _balances.TryRemove(playerId, out _);
        }
        
        /// <summary>
        /// 清除所有数据
        /// </summary>
        public void ClearAll()
        {
            _balances.Clear();
            while (_records.TryTake(out _)) { }
            _pendingRequests.Clear();
        }
        
        /// <summary>
        /// 保存上下分记录到 ZCG 数据存储
        /// </summary>
        public void SaveRecords()
        {
            try
            {
                var storage = ZCGDataStorage.Instance;
                var records = new List<ZCGScoreRecord>();
                
                foreach (var r in _records)
                {
                    records.Add(new ZCGScoreRecord
                    {
                        Id = r.Id, // 使用记录自身的ID，避免重复
                        PlayerId = r.PlayerId,
                        PlayerName = r.Nickname,
                        Type = r.IsUp ? "上分" : "下分",
                        Amount = r.Amount,
                        BalanceBefore = r.BalanceBefore,
                        BalanceAfter = r.BalanceAfter,
                        Time = r.Time,
                        Remark = r.Remark
                    });
                }
                
                storage.SaveScoreRecords(records);
                Log($"上下分记录已保存，共 {records.Count} 条");
            }
            catch (Exception ex)
            {
                Log($"保存上下分记录失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 从 ZCG 数据存储加载上下分记录
        /// </summary>
        public void LoadRecords()
        {
            try
            {
                var storage = ZCGDataStorage.Instance;
                var records = storage.LoadScoreRecords();
                
                // 清空现有记录
                while (_records.TryTake(out _)) { }
                
                // 加载记录
                foreach (var r in records)
                {
                    _records.Add(new ScoreRecord
                    {
                        Id = r.Id ?? Guid.NewGuid().ToString("N"), // 保留原ID，如果没有则生成新的
                        PlayerId = r.PlayerId,
                        Nickname = r.PlayerName,
                        Amount = r.Amount,
                        IsUp = r.Type == "上分",
                        BalanceBefore = r.BalanceBefore,
                        BalanceAfter = r.BalanceAfter,
                        Time = r.Time,
                        Remark = r.Remark
                    });
                }
                
                Log($"上下分记录已加载，共 {records.Count} 条");
            }
            catch (Exception ex)
            {
                Log($"加载上下分记录失败: {ex.Message}");
            }
        }
        
        #endregion
        
        private void Log(string message)
        {
            Logger.Info($"[Score] {message}");
            OnLog?.Invoke(message);
        }
    }
    
    /// <summary>
    /// 上下分命令
    /// </summary>
    public class ScoreCommand
    {
        public string PlayerId { get; set; }
        public int Amount { get; set; }
        public bool IsUp { get; set; }
        public string RawCommand { get; set; }
    }
    
    /// <summary>
    /// 上下分请求（待处理）
    /// </summary>
    public class ScoreRequest
    {
        public string PlayerId { get; set; }
        public int Amount { get; set; }
        public bool IsUp { get; set; }
        public DateTime RequestTime { get; set; }
    }
    
    /// <summary>
    /// 上下分结果
    /// </summary>
    public class ScoreResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int NewBalance { get; set; }
        public ScoreRecord Record { get; set; }
        public string FormattedResponse { get; set; }
    }
    
    /// <summary>
    /// 上下分记录
    /// </summary>
    public class ScoreRecord
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N"); // 唯一标识，创建时生成
        public string PlayerId { get; set; }
        public string Nickname { get; set; }
        public int Amount { get; set; }
        public bool IsUp { get; set; }
        public int BalanceBefore { get; set; }
        public int BalanceAfter { get; set; }
        public DateTime Time { get; set; }
        public string Remark { get; set; }
        public bool IsTrustee { get; set; }
    }
    
    /// <summary>
    /// 玩家余额 - 根据客户端列表成员协议完善
    /// </summary>
    public class PlayerBalance
    {
        public string PlayerId { get; set; }          // 玩家旺旺号
        public string Nickname { get; set; }          // 昵称
        public string GroupCard { get; set; }         // 群名片
        public int Balance { get; set; }              // 分数
        public int ReservedScore { get; set; }        // 留分
        public string BetContent { get; set; }        // 下注内容
        public int TotalUp { get; set; }
        public int TotalDown { get; set; }
        public int TotalBet { get; set; }
        public int TotalWin { get; set; }
        public DateTime LastUpdateTime { get; set; }
        
        public int NetProfit => TotalWin - TotalBet;
        
        /// <summary>显示名称</summary>
        public string DisplayName => !string.IsNullOrEmpty(GroupCard) ? GroupCard : Nickname ?? PlayerId;
        
        /// <summary>简称</summary>
        public string ShortId => PlayerId?.Length > 4 ? PlayerId.Substring(0, 4) : PlayerId;
    }
    
    /// <summary>
    /// 每日统计
    /// </summary>
    public class DailyStats
    {
        public DateTime Date { get; set; }
        public int TotalUp { get; set; }
        public int TotalDown { get; set; }
        public int UpCount { get; set; }
        public int DownCount { get; set; }
        public int NetFlow => TotalUp - TotalDown;
    }
    
    /// <summary>
    /// 上下分下注记录 - 用于账单格式化
    /// </summary>
    public class ScoreBetRecord
    {
        public string PlayerId { get; set; }
        public string Nickname { get; set; }
        public string BetType { get; set; }
        public int Amount { get; set; }
        public bool IsWin { get; set; }
        public int WinAmount { get; set; }
        public string Period { get; set; }
        public DateTime BetTime { get; set; }
    }
}
