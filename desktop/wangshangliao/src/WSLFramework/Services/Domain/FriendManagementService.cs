using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WSLFramework.Protocol;
using WSLFramework.Utils;

namespace WSLFramework.Services
{
    /// <summary>
    /// 好友管理服务 - 完全匹配ZCG的好友管理功能
    /// 基于深度逆向分析: ww_是否朋友及是否好友_查询, ww_ID查询
    /// </summary>
    public class FriendManagementService
    {
        private static readonly Lazy<FriendManagementService> _lazy = 
            new Lazy<FriendManagementService>(() => new FriendManagementService());
        public static FriendManagementService Instance => _lazy.Value;
        
        // CDP桥接
        private CDPBridge _cdpBridge;
        
        // 好友缓存
        private readonly ConcurrentDictionary<string, FriendInfo> _friends;
        
        // 待处理的好友申请
        private readonly ConcurrentQueue<FriendApply> _pendingApplies;
        
        // 处理中标记 (用于Interlocked原子操作)
#pragma warning disable CS0414 // 保留给并发处理
        private int _processing = 0;
#pragma warning restore CS0414
        
        // 配置
        public bool AutoAcceptFriend { get; set; } = true;
        public bool AutoSendWelcome { get; set; } = true;
        public string WelcomeTemplate { get; set; } = "{nickname}({shortId})\n欢迎入团，钱不要多，运气要帅…\n未开盘期间请先来一波";
        public string AlreadyFriendTemplate { get; set; } = "{nickname}({shortId})\n欢迎回来，我们已是好友，现在可以开始聊天了";
        
        // 事件
        public event EventHandler<FriendApplyEventArgs> OnFriendApply;
        public event EventHandler<FriendAddedEventArgs> OnFriendAdded;
        
        private FriendManagementService()
        {
            _friends = new ConcurrentDictionary<string, FriendInfo>();
            _pendingApplies = new ConcurrentQueue<FriendApply>();
        }
        
        /// <summary>
        /// 初始化服务
        /// </summary>
        public void Initialize(CDPBridge cdpBridge)
        {
            _cdpBridge = cdpBridge;
            Logger.Info("好友管理服务已初始化");
        }
        
        #region 好友申请处理
        
        /// <summary>
        /// 处理好友申请通知
        /// 基于日志: friendApply:{applyId, fromId, toId}
        /// </summary>
        public async Task HandleFriendApplyAsync(string applyId, string fromId, string toId)
        {
            try
            {
                Logger.Info($"收到好友申请: applyId={applyId}, fromId={fromId}, toId={toId}");
                
                var apply = new FriendApply
                {
                    ApplyId = applyId,
                    FromId = fromId,
                    ToId = toId,
                    ApplyTime = DateTime.Now
                };
                
                _pendingApplies.Enqueue(apply);
                
                // 触发事件
                OnFriendApply?.Invoke(this, new FriendApplyEventArgs { Apply = apply });
                
                // 自动同意
                if (AutoAcceptFriend)
                {
                    await AcceptFriendApplyAsync(apply);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"处理好友申请失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 同意好友申请
        /// </summary>
        public async Task AcceptFriendApplyAsync(FriendApply apply)
        {
            try
            {
                if (_cdpBridge == null)
                {
                    Logger.Warning("CDP未连接，无法同意好友申请");
                    return;
                }
                
                // 通过CDP接受好友申请
                // 调用WSL的好友接受API
                await _cdpBridge.AcceptFriendRequestAsync(apply.FromId);
                
                Logger.Info($"已同意好友申请: {apply.FromId}");
                
                // 标记为已处理
                apply.Handled = true;
                apply.Accepted = true;
            }
            catch (Exception ex)
            {
                Logger.Error($"同意好友申请失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 拒绝好友申请
        /// </summary>
        public async Task RejectFriendApplyAsync(FriendApply apply, string reason = "")
        {
            try
            {
                if (_cdpBridge == null)
                {
                    Logger.Warning("CDP未连接，无法拒绝好友申请");
                    return;
                }
                
                // 通过CDP拒绝好友申请
                await _cdpBridge.RejectFriendRequestAsync(apply.FromId, reason);
                
                Logger.Info($"已拒绝好友申请: {apply.FromId}");
                
                // 标记为已处理
                apply.Handled = true;
                apply.Accepted = false;
            }
            catch (Exception ex)
            {
                Logger.Error($"拒绝好友申请失败: {ex.Message}");
            }
        }
        
        #endregion
        
        #region 好友状态变化处理
        
        /// <summary>
        /// 处理好友状态变化通知
        /// 基于日志: friendStateChange:{fromId, opr, targetId}
        /// opr: 1=添加好友成功
        /// </summary>
        public async Task HandleFriendStateChangeAsync(string fromId, int operation, string targetId)
        {
            try
            {
                Logger.Info($"好友状态变化: fromId={fromId}, opr={operation}, targetId={targetId}");
                
                if (operation == 1) // 添加好友成功
                {
                    // 缓存好友信息
                    var friend = new FriendInfo
                    {
                        FriendId = fromId,
                        AddTime = DateTime.Now
                    };
                    _friends.TryAdd(fromId, friend);
                    
                    // 触发事件
                    OnFriendAdded?.Invoke(this, new FriendAddedEventArgs { FriendId = fromId });
                    
                    // 发送欢迎消息
                    if (AutoSendWelcome)
                    {
                        await SendWelcomeMessageAsync(fromId);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"处理好友状态变化失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 发送欢迎消息
        /// 基于日志: 发送好友消息|621705120|娜一(3240)\n欢迎入团...
        /// </summary>
        private async Task SendWelcomeMessageAsync(string friendId)
        {
            try
            {
                if (_cdpBridge == null)
                    return;
                
                // 查询好友信息获取昵称
                var userInfo = await _cdpBridge.GetUserInfoAsync(friendId);
                
                var nickname = userInfo?.nickname ?? "朋友";
                var shortId = ZCGFullApiSpec.GetShortId(friendId);
                
                // 格式化欢迎消息
                var message = WelcomeTemplate
                    .Replace("{nickname}", nickname)
                    .Replace("{shortId}", shortId)
                    .Replace("{friendId}", friendId);
                
                // 发送私聊消息
                await _cdpBridge.SendPrivateMessageAsync(friendId, message);
                
                Logger.Info($"已发送欢迎消息给: {friendId}");
            }
            catch (Exception ex)
            {
                Logger.Error($"发送欢迎消息失败: {ex.Message}");
            }
        }
        
        #endregion
        
        #region 好友查询
        
        /// <summary>
        /// 检查是否为好友
        /// API: ww_是否朋友及是否好友_查询|机器人ID|查询ID|1
        /// </summary>
        public async Task<bool> IsFriendAsync(string userId)
        {
            try
            {
                // 先检查缓存
                if (_friends.ContainsKey(userId))
                    return true;
                
                // 通过CDP查询
                if (_cdpBridge != null)
                {
                    return await _cdpBridge.CheckIsFriendAsync(userId);
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Logger.Error($"检查好友状态失败: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 查询用户信息
        /// API: ww_ID查询|机器人ID|查询ID
        /// </summary>
        public async Task<FriendInfo> QueryUserInfoAsync(string userId)
        {
            try
            {
                // 先检查缓存
                if (_friends.TryGetValue(userId, out var cached))
                    return cached;
                
                // 通过CDP查询
                if (_cdpBridge != null)
                {
                    var userInfo = await _cdpBridge.GetUserInfoAsync(userId);
                    if (userInfo != null)
                    {
                        var friend = new FriendInfo
                        {
                            FriendId = userId,
                            Nickname = userInfo.nickname,
                            Avatar = userInfo.avatar,
                            AddTime = DateTime.Now
                        };
                        
                        // 缓存
                        _friends.TryAdd(userId, friend);
                        
                        return friend;
                    }
                }
                
                return null;
            }
            catch (Exception ex)
            {
                Logger.Error($"查询用户信息失败: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 获取好友列表
        /// </summary>
        public List<FriendInfo> GetFriendList()
        {
            return new List<FriendInfo>(_friends.Values);
        }
        
        #endregion
        
        #region 私聊消息处理
        
        /// <summary>
        /// 处理私聊消息
        /// 基于日志: 私聊消息处理逻辑
        /// </summary>
        public async Task HandlePrivateMessageAsync(string senderId, string content, int countdown)
        {
            try
            {
                Logger.Info($"收到私聊消息: senderId={senderId}, content={content}");
                
                string response = null;
                
                // 余额查询
                if (ZCGFullApiSpec.IsBalanceQuery(content))
                {
                    var balance = ScoreService.Instance.GetBalance(senderId);
                    response = ZCGFullApiSpec.FormatBalanceResponse(senderId, (int)balance);
                }
                // 历史记录查询
                else if (ZCGFullApiSpec.IsHistoryQuery(content))
                {
                    // 获取历史记录并格式化
                    response = await GetFormattedHistoryAsync();
                }
                // 下注命令
                else
                {
                    var bets = ZCGFullApiSpec.ParseBetCommands(content);
                    if (bets.Count > 0)
                    {
                        response = await ProcessBetsAsync(senderId, bets);
                    }
                }
                
                // 发送响应
                if (!string.IsNullOrEmpty(response))
                {
                    // 私聊响应添加倒计时后缀
                    var suffix = $"\n或业务量已结束还有{countdown}秒";
                    
                    // 同时在群里@回复
                    await SendGroupAtResponseAsync(senderId, response);
                    
                    // 发送私聊响应
                    await _cdpBridge.SendPrivateMessageAsync(senderId, response + suffix);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"处理私聊消息失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 处理下注
        /// </summary>
        private async Task<string> ProcessBetsAsync(string playerId, List<BetCommand> bets)
        {
            var balance = ScoreService.Instance.GetBalance(playerId);
            var totalBet = 0;
            
            foreach (var bet in bets)
            {
                totalBet += bet.Amount;
            }
            
            if (balance < totalBet)
            {
                return ZCGFullApiSpec.FormatBetFailedResponse(playerId, bets);
            }
            
            // 扣除余额并记录下注
            foreach (var bet in bets)
            {
                ScoreService.Instance.DeductBet(playerId, bet.Amount);
                SettlementService.Instance.AddBet(playerId, bet.BetType, bet.Amount);
            }
            
            var shortId = ZCGFullApiSpec.GetShortId(playerId);
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"[LQ:@{playerId}] ({shortId})");
            sb.Append("已录入:");
            foreach (var bet in bets)
            {
                sb.Append($"{ZCGFullApiSpec.GetBetTypeDisplay(bet.BetType)}{bet.Amount} ");
            }
            
            return sb.ToString().TrimEnd();
        }
        
        /// <summary>
        /// 在群里发送@响应
        /// </summary>
        private async Task SendGroupAtResponseAsync(string userId, string message)
        {
            try
            {
                // 获取用户所在的群
                var userGroups = await _cdpBridge.GetUserGroupsAsync(userId);
                if (userGroups != null && userGroups.Count > 0)
                {
                    // 发送到第一个群
                    await _cdpBridge.SendGroupMessageAsync(userGroups[0], message);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"发送群@响应失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 获取格式化的历史记录
        /// </summary>
        private async Task<string> GetFormattedHistoryAsync()
        {
            // 从彩票服务获取历史数据
            var lottery = LotteryService.Instance;
            var history = new List<int>();
            var bigSmall = new List<string>();
            var lastDigits = new List<int>();
            
            // 获取最近10期结果
            var results = await lottery.GetRecentResultsAsync(10);
            
            foreach (var result in results)
            {
                history.Add(result.Sum);
                bigSmall.Add(result.IsBig ? "B" : (result.IsSmall ? "L" : "H"));
                lastDigits.Add(result.Sum % 10);
            }
            
            return ZCGFullApiSpec.FormatHistoryResponse(history, bigSmall, lastDigits);
        }
        
        #endregion
    }
    
    #region 数据模型
    
    /// <summary>
    /// 好友信息
    /// </summary>
    public class FriendInfo
    {
        public string FriendId { get; set; }
        public string Nickname { get; set; }
        public string Avatar { get; set; }
        public DateTime AddTime { get; set; }
        public bool IsOnline { get; set; }
    }
    
    /// <summary>
    /// 好友申请
    /// </summary>
    public class FriendApply
    {
        public string ApplyId { get; set; }
        public string FromId { get; set; }
        public string ToId { get; set; }
        public DateTime ApplyTime { get; set; }
        public bool Handled { get; set; }
        public bool Accepted { get; set; }
    }
    
    /// <summary>
    /// 好友申请事件参数
    /// </summary>
    public class FriendApplyEventArgs : EventArgs
    {
        public FriendApply Apply { get; set; }
    }
    
    /// <summary>
    /// 好友添加事件参数
    /// </summary>
    public class FriendAddedEventArgs : EventArgs
    {
        public string FriendId { get; set; }
    }
    
    #endregion
}
