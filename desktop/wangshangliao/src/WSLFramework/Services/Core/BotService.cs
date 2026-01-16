using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WSLFramework.Protocol;
using WSLFramework.Utils;

namespace WSLFramework.Services
{
    /// <summary>
    /// 机器人服务 - 核心消息处理
    /// </summary>
    public class BotService
    {
        private static BotService _instance;
        public static BotService Instance => _instance ?? (_instance = new BotService());
        
        private readonly Dictionary<string, AccountInfo> _accounts;
        private readonly AutoReplyService _autoReply;
        private readonly BetParserService _betParser;
        private readonly ScoreService _scoreService;
        
        public event Action<string> OnLog;
        public event Action<ZCGMessage> OnMessageReceived;
        public event Action<ZCGMessage> OnMessageSent;
        
        private BotService()
        {
            _accounts = new Dictionary<string, AccountInfo>();
            _autoReply = AutoReplyService.Instance;
            _betParser = BetParserService.Instance;
            _scoreService = ScoreService.Instance;
        }
        
        /// <summary>
        /// 添加账号
        /// </summary>
        public void AddAccount(string account, string nickname, string wwid, string groupId)
        {
            var info = new AccountInfo
            {
                Account = account,
                Nickname = nickname,
                Wwid = wwid,
                GroupId = groupId,
                Status = "在线",
                AutoMode = false,
                LoginTime = DateTime.Now
            };
            
            _accounts[account] = info;
            Log($"添加账号: {nickname} ({account}), 群号: {groupId}");
        }
        
        /// <summary>
        /// 移除账号
        /// </summary>
        public void RemoveAccount(string account)
        {
            if (_accounts.ContainsKey(account))
            {
                _accounts.Remove(account);
                Log($"移除账号: {account}");
            }
        }
        
        /// <summary>
        /// 获取所有账号
        /// </summary>
        public IEnumerable<AccountInfo> GetAccounts()
        {
            return _accounts.Values;
        }
        
        /// <summary>
        /// 处理收到的消息
        /// </summary>
        public async Task ProcessMessageAsync(ZCGMessage message)
        {
            Log($"收到消息: RQQ={message.RQQ}, 群={message.GroupId}, From={message.FromQQ}");
            OnMessageReceived?.Invoke(message);
            
            // 根据消息类型分发处理
            if (!string.IsNullOrEmpty(message.GroupId) && message.GroupId != "0")
            {
                await ProcessGroupMessageAsync(message);
            }
            else if (!string.IsNullOrEmpty(message.FromQQ))
            {
                await ProcessPrivateMessageAsync(message);
            }
        }
        
        /// <summary>
        /// 处理群消息
        /// </summary>
        private async Task ProcessGroupMessageAsync(ZCGMessage message)
        {
            var content = message.Content?.Trim() ?? "";
            
            // 检查是否是下注消息
            var betResult = _betParser.Parse(content);
            if (betResult != null && betResult.IsValid)
            {
                await ProcessBetAsync(message, betResult);
                return;
            }
            
            // 检查是否是上下分请求
            if (content.Contains("+") || content.Contains("-") || content.StartsWith("c") || content.StartsWith("C") || content.StartsWith("下"))
            {
                var scoreCmd = _scoreService.ParseZCGCommand(content, message.FromQQ);
                if (scoreCmd != null)
                {
                    await ProcessScoreAsync(message, scoreCmd);
                    return;
                }
            }
            
            // 检查自动回复
            var reply = _autoReply.GetReply(content);
            if (!string.IsNullOrEmpty(reply))
            {
                await SendGroupMessageAsync(message.RQQ, message.GroupId, reply);
            }
        }
        
        /// <summary>
        /// 处理私聊消息
        /// </summary>
        private async Task ProcessPrivateMessageAsync(ZCGMessage message)
        {
            var content = message.Content?.Trim() ?? "";
            
            // 上下分处理 (私聊上分格式: 玩家ID+金额)
            if (content.Contains("+") || content.Contains("-") || content.StartsWith("c") || content.StartsWith("C") || content.StartsWith("下"))
            {
                var scoreCmd = _scoreService.ParseZCGCommand(content, message.FromQQ);
                if (scoreCmd != null)
                {
                    await ProcessScoreAsync(message, scoreCmd);
                    return;
                }
            }
            
            // 自动回复
            var reply = _autoReply.GetReply(content);
            if (!string.IsNullOrEmpty(reply))
            {
                await SendPrivateMessageAsync(message.RQQ, message.FromQQ, reply);
            }
        }
        
        /// <summary>
        /// 处理下注
        /// </summary>
        private async Task ProcessBetAsync(ZCGMessage message, BetResult bet)
        {
            Log($"下注: 玩家={message.FromQQ}, 类型={bet.BetType}, 金额={bet.Amount}");
            
            // TODO: 记录下注、计算赔率等
            
            // 回复确认
            var shortId = ZCGMessage.GetShortId(message.FromQQ);
            var reply = $"[LQ:@{message.FromQQ}] ({shortId})\n收到下注: {bet.BetType} {bet.Amount}";
            await SendGroupMessageAsync(message.RQQ, message.GroupId, reply);
        }
        
        /// <summary>
        /// 处理上下分
        /// </summary>
        private async Task ProcessScoreAsync(ZCGMessage message, ScoreCommand cmd)
        {
            var result = _scoreService.ProcessCommand(cmd);
            
            var shortId = ZCGMessage.GetShortId(message.FromQQ);
            string reply;
            
            if (result.Success)
            {
                reply = $"({shortId})\n{(cmd.IsUp ? "上分" : "下分")}成功\n" +
                       $"操作金额: {cmd.Amount}\n当前余额: {result.NewBalance}";
            }
            else
            {
                reply = $"({shortId})\n{result.Message}";
            }
            
            // 根据消息来源回复
            if (!string.IsNullOrEmpty(message.GroupId) && message.GroupId != "0")
            {
                await SendGroupMessageAsync(message.RQQ, message.GroupId, reply);
            }
            else
            {
                await SendPrivateMessageAsync(message.RQQ, message.FromQQ, reply);
            }
        }
        
        /// <summary>
        /// 发送群消息
        /// </summary>
        public async Task<bool> SendGroupMessageAsync(string rqq, string groupId, string content)
        {
            var msg = new ZCGMessage
            {
                RQQ = rqq,
                GroupId = groupId,
                Content = content,
                TypeDesc = "发送群消息|Group_SendMsg"
            };
            
            Log($"发送群消息: 群={groupId}, 内容={content.Substring(0, Math.Min(50, content.Length))}...");
            OnMessageSent?.Invoke(msg);
            
            // TODO: 通过CDP实际发送
            return true;
        }
        
        /// <summary>
        /// 发送私聊消息
        /// </summary>
        public async Task<bool> SendPrivateMessageAsync(string rqq, string toQQ, string content)
        {
            var msg = new ZCGMessage
            {
                RQQ = rqq,
                FromQQ = toQQ,
                Content = content,
                TypeDesc = "发送好友消息|Friend_Send"
            };
            
            Log($"发送私聊: 目标={toQQ}, 内容={content.Substring(0, Math.Min(50, content.Length))}...");
            OnMessageSent?.Invoke(msg);
            
            // TODO: 通过CDP实际发送
            return true;
        }
        
        private void Log(string message)
        {
            Logger.Info($"[Bot] {message}");
            OnLog?.Invoke(message);
        }
    }
    
    /// <summary>
    /// 账号信息
    /// </summary>
    public class AccountInfo
    {
        public string Account { get; set; }
        public string Nickname { get; set; }
        public string Wwid { get; set; }
        public string GroupId { get; set; }
        public string Status { get; set; }
        public bool AutoMode { get; set; }
        public DateTime LoginTime { get; set; }
        
        public string ShortId => ZCGMessage.GetShortId(Wwid);
    }
}
