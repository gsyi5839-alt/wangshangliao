using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;
using WSLFramework.Utils;

namespace WSLFramework.Services
{
    /// <summary>
    /// 玩家管理服务 - 上下分、余额、下注记录管理
    /// 基于招财狗逆向分析
    /// </summary>
    public class PlayerService
    {
        private readonly ConcurrentDictionary<string, PlayerInfo> _players;
        private readonly ConcurrentDictionary<string, List<BetRecord>> _periodBets;
        private readonly List<string> _trustees; // 托管名单
        private readonly JavaScriptSerializer _serializer;
        private string _dataPath;
        
        // 配置
        public bool AutoUpEnabled { get; set; } = true;      // 托查钱自动上分
        public bool AutoDownEnabled { get; set; } = true;    // 托回钱自动下分
        public int AutoUpDelayMin { get; set; } = 5;         // 上分延迟最小(秒)
        public int AutoUpDelayMax { get; set; } = 10;        // 上分延迟最大(秒)
        public int AutoDownDelayMin { get; set; } = 10;      // 下分延迟最小(秒)
        public int AutoDownDelayMax { get; set; } = 20;      // 下分延迟最大(秒)
        public bool AutoAcceptTrustee { get; set; } = true;  // 自动同意托加群
        
        // 当前期号
        public string CurrentPeriod { get; set; }
        
        // 事件
        public event Action<string> OnLog;
#pragma warning disable CS0067 // 保留给将来的上下分请求处理
        public event Action<PlayerInfo, string> OnUpRequest;    // 上分请求
        public event Action<PlayerInfo, string> OnDownRequest;  // 下分请求
#pragma warning restore CS0067
        
        public PlayerService()
        {
            _players = new ConcurrentDictionary<string, PlayerInfo>();
            _periodBets = new ConcurrentDictionary<string, List<BetRecord>>();
            _trustees = new List<string>();
            _serializer = new JavaScriptSerializer();
            
            // 默认数据路径
            _dataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
            if (!Directory.Exists(_dataPath))
            {
                Directory.CreateDirectory(_dataPath);
            }
        }
        
        #region 玩家管理
        
        /// <summary>
        /// 获取或创建玩家
        /// </summary>
        public PlayerInfo GetOrCreatePlayer(string playerId, string nickname = null)
        {
            return _players.GetOrAdd(playerId, id => new PlayerInfo
            {
                PlayerId = id,
                Nickname = nickname ?? id,
                Balance = 0,
                CreateTime = DateTime.Now
            });
        }
        
        /// <summary>
        /// 获取玩家信息
        /// </summary>
        public PlayerInfo GetPlayer(string playerId)
        {
            _players.TryGetValue(playerId, out var player);
            return player;
        }
        
        /// <summary>
        /// 更新玩家昵称
        /// </summary>
        public void UpdateNickname(string playerId, string nickname)
        {
            var player = GetOrCreatePlayer(playerId);
            player.Nickname = nickname;
        }
        
        /// <summary>
        /// 获取所有玩家
        /// </summary>
        public List<PlayerInfo> GetAllPlayers()
        {
            return _players.Values.ToList();
        }
        
        #endregion
        
        #region 上下分管理
        
        /// <summary>
        /// 上分
        /// </summary>
        public UpDownResult AddScore(string playerId, int amount, string reason = "上分")
        {
            var player = GetOrCreatePlayer(playerId);
            
            if (amount <= 0)
            {
                return new UpDownResult { Success = false, Message = "金额必须大于0" };
            }
            
            player.Balance += amount;
            player.TotalUp += amount;
            player.LastActiveTime = DateTime.Now;
            
            // 记录流水
            player.Transactions.Add(new Transaction
            {
                Type = TransactionType.Up,
                Amount = amount,
                Balance = player.Balance,
                Reason = reason,
                Time = DateTime.Now
            });
            
            Log($"玩家 {player.DisplayName} 上分 {amount}, 余额: {player.Balance}");
            
            return new UpDownResult
            {
                Success = true,
                Message = $"上分成功！当前余额: {player.Balance}",
                Balance = player.Balance
            };
        }
        
        /// <summary>
        /// 下分
        /// </summary>
        public UpDownResult DeductScore(string playerId, int amount, string reason = "下分")
        {
            var player = GetPlayer(playerId);
            
            if (player == null)
            {
                return new UpDownResult { Success = false, Message = "玩家不存在" };
            }
            
            if (amount <= 0)
            {
                return new UpDownResult { Success = false, Message = "金额必须大于0" };
            }
            
            if (player.Balance < amount)
            {
                return new UpDownResult { Success = false, Message = $"余额不足！当前余额: {player.Balance}" };
            }
            
            player.Balance -= amount;
            player.TotalDown += amount;
            player.LastActiveTime = DateTime.Now;
            
            // 记录流水
            player.Transactions.Add(new Transaction
            {
                Type = TransactionType.Down,
                Amount = amount,
                Balance = player.Balance,
                Reason = reason,
                Time = DateTime.Now
            });
            
            Log($"玩家 {player.DisplayName} 下分 {amount}, 余额: {player.Balance}");
            
            return new UpDownResult
            {
                Success = true,
                Message = $"下分成功！当前余额: {player.Balance}",
                Balance = player.Balance
            };
        }
        
        /// <summary>
        /// 查询余额
        /// </summary>
        public string QueryBalance(string playerId)
        {
            var player = GetPlayer(playerId);
            if (player == null)
            {
                return "您还没有账户，请先上分";
            }
            
            return $"当前余额: {player.Balance}\n总上分: {player.TotalUp}\n总下分: {player.TotalDown}";
        }
        
        #endregion
        
        #region 下注管理
        
        /// <summary>
        /// 下注
        /// </summary>
        public BetPlaceResult PlaceBet(string playerId, string betType, int amount, string period = null)
        {
            var player = GetOrCreatePlayer(playerId);
            period = period ?? CurrentPeriod;
            
            if (string.IsNullOrEmpty(period))
            {
                return new BetPlaceResult { Success = false, Message = "当前无法下注，请等待新一期" };
            }
            
            if (amount <= 0)
            {
                return new BetPlaceResult { Success = false, Message = "下注金额必须大于0" };
            }
            
            if (player.Balance < amount)
            {
                return new BetPlaceResult 
                { 
                    Success = false, 
                    Message = $"余粮不足！当前余额: {player.Balance}，上分后录取：{betType}{amount}" 
                };
            }
            
            // 扣除余额
            player.Balance -= amount;
            player.TotalBet += amount;
            player.LastActiveTime = DateTime.Now;
            
            // 创建下注记录
            var bet = new BetRecord
            {
                Period = period,
                PlayerId = playerId,
                Nickname = player.Nickname,
                BetType = betType,
                Amount = amount,
                Odds = LotteryResult.GetOdds(betType),
                Time = DateTime.Now,
                Status = BetStatus.Pending
            };
            
            // 添加到期号下注列表
            var periodBets = _periodBets.GetOrAdd(period, p => new List<BetRecord>());
            lock (periodBets)
            {
                periodBets.Add(bet);
            }
            
            // 记录流水
            player.Transactions.Add(new Transaction
            {
                Type = TransactionType.Bet,
                Amount = -amount,
                Balance = player.Balance,
                Reason = $"下注 {betType} {amount}",
                Time = DateTime.Now
            });
            
            Log($"玩家 {player.DisplayName} 下注 {betType} {amount}, 余额: {player.Balance}");
            
            return new BetPlaceResult
            {
                Success = true,
                Message = $"下注成功！{betType}{amount}，余额: {player.Balance}",
                Bet = bet
            };
        }
        
        /// <summary>
        /// 结算指定期号的下注
        /// </summary>
        public SettlementResult Settlement(string period, LotteryResult result)
        {
            if (!_periodBets.TryGetValue(period, out var bets))
            {
                return new SettlementResult { Period = period, TotalBets = 0 };
            }
            
            var settlement = new SettlementResult
            {
                Period = period,
                Result = result,
                TotalBets = bets.Count
            };
            
            lock (bets)
            {
                foreach (var bet in bets)
                {
                    var player = GetPlayer(bet.PlayerId);
                    if (player == null) continue;
                    
                    bool win = result.IsWin(bet.BetType);
                    bet.IsWin = win;
                    bet.Status = BetStatus.Settled;
                    
                    if (win)
                    {
                        // 计算奖金
                        var winAmount = (int)(bet.Amount * bet.Odds);
                        bet.WinAmount = winAmount;
                        
                        player.Balance += winAmount;
                        player.TotalWin += winAmount - bet.Amount;
                        
                        settlement.TotalWin += winAmount - bet.Amount;
                        settlement.WinCount++;
                        
                        // 记录流水
                        player.Transactions.Add(new Transaction
                        {
                            Type = TransactionType.Win,
                            Amount = winAmount,
                            Balance = player.Balance,
                            Reason = $"中奖 {bet.BetType} {bet.Amount}×{bet.Odds}",
                            Time = DateTime.Now
                        });
                        
                        Log($"玩家 {player.DisplayName} 中奖 {bet.BetType}，获得 {winAmount}");
                    }
                    else
                    {
                        bet.WinAmount = 0;
                        player.TotalLose += bet.Amount;
                        settlement.TotalLose += bet.Amount;
                        settlement.LoseCount++;
                        
                        Log($"玩家 {player.DisplayName} 未中 {bet.BetType}，损失 {bet.Amount}");
                    }
                    
                    settlement.BetRecords.Add(bet);
                }
            }
            
            return settlement;
        }
        
        /// <summary>
        /// 获取当期下注列表
        /// </summary>
        public List<BetRecord> GetCurrentBets()
        {
            if (string.IsNullOrEmpty(CurrentPeriod)) return new List<BetRecord>();
            
            if (_periodBets.TryGetValue(CurrentPeriod, out var bets))
            {
                lock (bets)
                {
                    return bets.ToList();
                }
            }
            return new List<BetRecord>();
        }
        
        /// <summary>
        /// 取消当期所有下注
        /// </summary>
        public void CancelAllBets(string period = null)
        {
            period = period ?? CurrentPeriod;
            if (string.IsNullOrEmpty(period)) return;
            
            if (_periodBets.TryGetValue(period, out var bets))
            {
                lock (bets)
                {
                    foreach (var bet in bets)
                    {
                        if (bet.Status == BetStatus.Pending)
                        {
                            var player = GetPlayer(bet.PlayerId);
                            if (player != null)
                            {
                                player.Balance += bet.Amount;
                                bet.Status = BetStatus.Cancelled;
                            }
                        }
                    }
                }
            }
        }
        
        #endregion
        
        #region 托管名单管理
        
        /// <summary>
        /// 添加托管
        /// </summary>
        public void AddTrustee(string playerId)
        {
            if (!_trustees.Contains(playerId))
            {
                _trustees.Add(playerId);
                Log($"添加托管: {playerId}");
            }
        }
        
        /// <summary>
        /// 移除托管
        /// </summary>
        public void RemoveTrustee(string playerId)
        {
            _trustees.Remove(playerId);
            Log($"移除托管: {playerId}");
        }
        
        /// <summary>
        /// 检查是否是托管
        /// </summary>
        public bool IsTrustee(string playerId)
        {
            return _trustees.Contains(playerId);
        }
        
        /// <summary>
        /// 获取托管名单
        /// </summary>
        public List<string> GetTrustees()
        {
            return _trustees.ToList();
        }
        
        #endregion
        
        #region 数据持久化
        
        /// <summary>
        /// 保存数据
        /// </summary>
        public void SaveData()
        {
            try
            {
                // 使用 ZCGDataStorage 保存到旧程序目录结构
                var storage = ZCGDataStorage.Instance;
                
                // 保存玩家姓名映射
                var playerNames = new Dictionary<string, string>();
                foreach (var p in _players.Values)
                {
                    playerNames[p.PlayerId] = p.Nickname;
                    
                    // 保存每个玩家的详细资料
                    storage.SavePlayerProfile(p.PlayerId, new PlayerProfile
                    {
                        Id = p.PlayerId,
                        Name = p.Nickname,
                        Nickname = p.Nickname,
                        Balance = p.Balance,
                        TotalBet = p.TotalBet,
                        TotalWin = p.TotalWin,
                        TotalProfit = p.TotalProfit,
                        FirstSeen = p.CreateTime,
                        LastSeen = DateTime.Now,
                        BetCount = p.BetCount,
                        WinCount = p.WinCount
                    });
                }
                storage.SavePlayerNames(playerNames);
                
                // 也保存到本地 data 目录（兼容）
                var playersFile = Path.Combine(_dataPath, "players.json");
                var trusteesFile = Path.Combine(_dataPath, "trustees.json");
                
                File.WriteAllText(playersFile, _serializer.Serialize(_players.Values.ToList()));
                File.WriteAllText(trusteesFile, _serializer.Serialize(_trustees));
                
                Log("数据已保存到 ZCG 目录结构");
            }
            catch (Exception ex)
            {
                Log($"保存数据失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 加载数据
        /// </summary>
        public void LoadData()
        {
            try
            {
                // 优先从 ZCGDataStorage 加载
                var storage = ZCGDataStorage.Instance;
                var playerNames = storage.LoadPlayerNames();
                
                foreach (var kvp in playerNames)
                {
                    var profile = storage.LoadPlayerProfile(kvp.Key);
                    if (profile != null)
                    {
                        _players[kvp.Key] = new PlayerInfo
                        {
                            PlayerId = profile.Id,
                            Nickname = profile.Name ?? profile.Nickname ?? kvp.Value,
                            Balance = profile.Balance,
                            TotalBet = profile.TotalBet,
                            TotalWin = profile.TotalWin,
                            TotalProfit = profile.TotalProfit,
                            CreateTime = profile.FirstSeen,
                            BetCount = profile.BetCount,
                            WinCount = profile.WinCount
                        };
                    }
                    else
                    {
                        _players[kvp.Key] = new PlayerInfo
                        {
                            PlayerId = kvp.Key,
                            Nickname = kvp.Value,
                            Balance = 0,
                            CreateTime = DateTime.Now
                        };
                    }
                }
                
                // 如果 ZCG 没有数据，从本地 data 目录加载
                if (_players.Count == 0)
                {
                    var playersFile = Path.Combine(_dataPath, "players.json");
                    var trusteesFile = Path.Combine(_dataPath, "trustees.json");
                    
                    if (File.Exists(playersFile))
                    {
                        var players = _serializer.Deserialize<List<PlayerInfo>>(File.ReadAllText(playersFile));
                        foreach (var p in players)
                        {
                            _players[p.PlayerId] = p;
                        }
                    }
                    
                    if (File.Exists(trusteesFile))
                    {
                        var trustees = _serializer.Deserialize<List<string>>(File.ReadAllText(trusteesFile));
                        _trustees.Clear();
                        _trustees.AddRange(trustees);
                    }
                }
                
                Log($"数据已加载，共 {_players.Count} 个玩家");
            }
            catch (Exception ex)
            {
                Log($"加载数据失败: {ex.Message}");
            }
        }
        
        #endregion
        
        private void Log(string message)
        {
            Logger.Info($"[玩家] {message}");
            OnLog?.Invoke(message);
        }
    }
    
    #region 数据模型
    
    /// <summary>
    /// 玩家信息 - 根据客户端列表成员协议完善
    /// </summary>
    public class PlayerInfo
    {
        public string PlayerId { get; set; }          // 玩家旺旺号
        public string Nickname { get; set; }          // 玩家昵称
        public string GroupCard { get; set; }         // 群名片
        public string NimAccid { get; set; }          // NIM accid
        public long NimId { get; set; }               // NIM ID (用于消息发送)
        public int Balance { get; set; }              // 分数
        public int ReservedScore { get; set; }        // 留分
        public string BetContent { get; set; }        // 下注内容
        public string LastTime { get; set; }          // 时间
        public int TotalUp { get; set; }
        public int TotalDown { get; set; }
        public int TotalBet { get; set; }
        public int TotalWin { get; set; }
        public int TotalLose { get; set; }
        public int TotalProfit { get; set; }          // 总盈亏
        public int BetCount { get; set; }             // 下注次数
        public int WinCount { get; set; }             // 赢的次数
        public DateTime CreateTime { get; set; }
        public DateTime LastActiveTime { get; set; }
        public List<Transaction> Transactions { get; set; } = new List<Transaction>();
        
        /// <summary>显示名称 (优先群名片，其次昵称)</summary>
        public string DisplayName => !string.IsNullOrEmpty(GroupCard) ? GroupCard : 
                                     (string.IsNullOrEmpty(Nickname) ? PlayerId : $"{Nickname}({PlayerId.Substring(Math.Max(0, PlayerId.Length - 4))})");
        
        /// <summary>盈亏</summary>
        public int Profit => TotalWin - TotalLose;
        
        /// <summary>简称 (取前4位)</summary>
        public string ShortId => PlayerId?.Length > 4 ? PlayerId.Substring(0, 4) : PlayerId;
        
        /// <summary>
        /// 转为客户端列表字典格式
        /// </summary>
        public Dictionary<string, object> ToListDict()
        {
            return new Dictionary<string, object>
            {
                { "旺旺号", PlayerId },
                { "昵称", DisplayName },
                { "分数", Balance },
                { "留分", ReservedScore },
                { "下注内容", BetContent ?? "" },
                { "时间", LastTime ?? LastActiveTime.ToString("HH:mm") }
            };
        }
        
        /// <summary>
        /// 从群成员信息创建玩家
        /// </summary>
        public static PlayerInfo FromGroupMember(LocalGroupMember member)
        {
            if (member == null) return null;
            return new PlayerInfo
            {
                PlayerId = member.UserId.ToString(),
                Nickname = member.DisplayName,
                GroupCard = member.GroupMemberNick,
                NimId = member.NimId,
                Balance = 0,
                ReservedScore = 0,
                CreateTime = DateTime.Now
            };
        }
    }
    
    /// <summary>
    /// 上分请求
    /// </summary>
    public class DepositRequest
    {
        /// <summary>玩家旺旺号</summary>
        public string PlayerId { get; set; }
        
        /// <summary>昵称</summary>
        public string NickName { get; set; }
        
        /// <summary>喊话内容</summary>
        public string Content { get; set; }
        
        /// <summary>请求上分金额</summary>
        public decimal RequestAmount { get; set; }
        
        /// <summary>请求次数</summary>
        public int Count { get; set; }
        
        /// <summary>请求时间</summary>
        public DateTime Time { get; set; }
        
        /// <summary>状态 (待处理/已处理/已拒绝)</summary>
        public RequestStatus Status { get; set; } = RequestStatus.Pending;
    }
    
    /// <summary>
    /// 下分请求
    /// </summary>
    public class WithdrawRequest
    {
        /// <summary>玩家旺旺号</summary>
        public string PlayerId { get; set; }
        
        /// <summary>昵称</summary>
        public string NickName { get; set; }
        
        /// <summary>喊话内容</summary>
        public string Content { get; set; }
        
        /// <summary>请求下分金额</summary>
        public decimal RequestAmount { get; set; }
        
        /// <summary>余粮（当前余额）</summary>
        public decimal Balance { get; set; }
        
        /// <summary>请求次数</summary>
        public int Count { get; set; }
        
        /// <summary>请求时间</summary>
        public DateTime Time { get; set; }
        
        /// <summary>状态 (待处理/已处理/已拒绝)</summary>
        public RequestStatus Status { get; set; } = RequestStatus.Pending;
    }
    
    /// <summary>
    /// 请求状态
    /// </summary>
    public enum RequestStatus
    {
        /// <summary>待处理</summary>
        Pending,
        
        /// <summary>已处理</summary>
        Processed,
        
        /// <summary>已拒绝</summary>
        Rejected,
        
        /// <summary>已忽略</summary>
        Ignored
    }
    
    /// <summary>
    /// 交易记录
    /// </summary>
    public class Transaction
    {
        public TransactionType Type { get; set; }
        public int Amount { get; set; }
        public int Balance { get; set; }
        public string Reason { get; set; }
        public DateTime Time { get; set; }
    }
    
    public enum TransactionType
    {
        Up,      // 上分
        Down,    // 下分
        Bet,     // 下注
        Win,     // 中奖
        Refund   // 退款
    }
    
    /// <summary>
    /// 下注记录
    /// </summary>
    public class BetRecord
    {
        public string Period { get; set; }
        public string PlayerId { get; set; }
        public string Nickname { get; set; }
        public string BetType { get; set; }
        public int Amount { get; set; }
        public decimal Odds { get; set; }
        public DateTime Time { get; set; }
        public BetStatus Status { get; set; }
        public bool IsWin { get; set; }
        public int WinAmount { get; set; }
    }
    
    public enum BetStatus
    {
        Pending,    // 待开奖
        Settled,    // 已结算
        Cancelled   // 已取消
    }
    
    /// <summary>
    /// 上下分结果
    /// </summary>
    public class UpDownResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int Balance { get; set; }
    }
    
    /// <summary>
    /// 下注结果
    /// </summary>
    public class BetPlaceResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public BetRecord Bet { get; set; }
    }
    
    /// <summary>
    /// 结算结果
    /// </summary>
    public class SettlementResult
    {
        public string Period { get; set; }
        public LotteryResult Result { get; set; }
        public int TotalBets { get; set; }
        public int WinCount { get; set; }
        public int LoseCount { get; set; }
        public int TotalWin { get; set; }
        public int TotalLose { get; set; }
        public List<BetRecord> BetRecords { get; set; } = new List<BetRecord>();
        
        public string GetBillMessage()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"開:{Result.Num1} + {Result.Num2} + {Result.Num3} = {Result.Sum} {Result.GetResultString()}");
            sb.AppendLine($"人數:{TotalBets}  总分:{TotalWin - TotalLose}");
            sb.AppendLine("----------------------");
            
            foreach (var bet in BetRecords)
            {
                var status = bet.IsWin ? "✓" : "✗";
                sb.AppendLine($"{bet.Nickname}: {bet.BetType}{bet.Amount} {status} {(bet.IsWin ? "+" + bet.WinAmount : "-" + bet.Amount)}");
            }
            
            sb.AppendLine("----------------------");
            return sb.ToString();
        }
    }
    
    #endregion
}
