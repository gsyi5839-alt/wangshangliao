using System;
using System.Collections.Generic;

namespace WSLFramework.Services.EventDriven
{
    #region 事件基类

    /// <summary>
    /// 旺商聊事件基类 - 参考 Lagrange.Core 的 EventBase
    /// </summary>
    public abstract class WSLEventBase
    {
        /// <summary>事件时间戳</summary>
        public DateTime EventTime { get; set; } = DateTime.Now;

        /// <summary>事件ID</summary>
        public string EventId { get; set; } = Guid.NewGuid().ToString("N");
    }

    #endregion

    #region 系统事件

    /// <summary>
    /// 机器人上线事件
    /// </summary>
    public class BotOnlineEvent : WSLEventBase
    {
        public string BotId { get; set; }
        public string BotNick { get; set; }
        public OnlineReason Reason { get; set; }

        public enum OnlineReason
        {
            Login,
            Reconnect
        }

        public BotOnlineEvent(string botId, string botNick, OnlineReason reason = OnlineReason.Login)
        {
            BotId = botId;
            BotNick = botNick;
            Reason = reason;
        }
    }

    /// <summary>
    /// 机器人下线事件
    /// </summary>
    public class BotOfflineEvent : WSLEventBase
    {
        public string BotId { get; set; }
        public OfflineReason Reason { get; set; }
        public string Message { get; set; }

        public enum OfflineReason
        {
            Logout,
            Kicked,
            NetworkError,
            ServerDisconnect,
            Reconnecting
        }

        public BotOfflineEvent(string botId, OfflineReason reason, string message = "")
        {
            BotId = botId;
            Reason = reason;
            Message = message;
        }
    }

    /// <summary>
    /// 日志事件
    /// </summary>
    public class BotLogEvent : WSLEventBase
    {
        public string Tag { get; set; }
        public LogLevel Level { get; set; }
        public string Message { get; set; }

        public BotLogEvent(string tag, LogLevel level, string message)
        {
            Tag = tag;
            Level = level;
            Message = message;
        }

        public override string ToString()
        {
            return $"[{EventTime:HH:mm:ss}][{Level}][{Tag}] {Message}";
        }
    }

    /// <summary>
    /// 连接状态变化事件
    /// </summary>
    public class ConnectionStateEvent : WSLEventBase
    {
        public bool IsConnected { get; set; }
        public ConnectionType Type { get; set; }
        public string Endpoint { get; set; }

        public enum ConnectionType
        {
            NIM,    // 网易云信
            CDP,    // Chrome DevTools
            API,    // HTTP API
            TCP     // 纯TCP长连接 (Lagrange风格)
        }

        public ConnectionStateEvent(bool isConnected, ConnectionType type, string endpoint = "")
        {
            IsConnected = isConnected;
            Type = type;
            Endpoint = endpoint;
        }
    }

    #endregion

    #region 消息事件

    /// <summary>
    /// 消息链 - 参考 Lagrange.Core 的 MessageChain
    /// </summary>
    public class WSLMessageChain : List<IMessageEntity>
    {
        public MessageType Type { get; set; }
        public string GroupId { get; set; }
        public string SenderId { get; set; }
        public string SenderNick { get; set; }
        public string SenderCard { get; set; }  // 群名片
        public string MessageId { get; set; }
        public long Timestamp { get; set; }
        public string RawJson { get; set; }

        /// <summary>获取纯文本内容</summary>
        public string GetPlainText()
        {
            var sb = new System.Text.StringBuilder();
            foreach (var entity in this)
            {
                if (entity is TextEntity text)
                    sb.Append(text.Content);
            }
            return sb.ToString();
        }

        /// <summary>是否包含@机器人</summary>
        public bool HasAtBot(string botId)
        {
            foreach (var entity in this)
            {
                if (entity is AtEntity at && at.TargetId == botId)
                    return true;
            }
            return false;
        }

        public enum MessageType
        {
            Group,
            Friend,
            Temp
        }
    }

    /// <summary>消息实体接口</summary>
    public interface IMessageEntity
    {
        string ToPreviewString();
    }

    /// <summary>文本消息</summary>
    public class TextEntity : IMessageEntity
    {
        public string Content { get; set; }
        public TextEntity(string content) => Content = content;
        public string ToPreviewString() => Content;
    }

    /// <summary>@消息</summary>
    public class AtEntity : IMessageEntity
    {
        public string TargetId { get; set; }
        public string TargetNick { get; set; }
        public AtEntity(string targetId, string targetNick = "")
        {
            TargetId = targetId;
            TargetNick = targetNick;
        }
        public string ToPreviewString() => $"[@{TargetNick ?? TargetId}]";
    }

    /// <summary>图片消息</summary>
    public class ImageEntity : IMessageEntity
    {
        public string Url { get; set; }
        public string FileId { get; set; }
        public ImageEntity(string url, string fileId = "")
        {
            Url = url;
            FileId = fileId;
        }
        public string ToPreviewString() => "[图片]";
    }

    /// <summary>
    /// 群消息事件 - 核心事件
    /// </summary>
    public class GroupMessageEvent : WSLEventBase
    {
        public WSLMessageChain Chain { get; set; }

        /// <summary>群号/ID</summary>
        public string GroupUin { get; set; }
        
        /// <summary>发送者ID</summary>
        public string FriendUin { get; set; }
        
        /// <summary>消息ID</summary>
        public string MessageId { get; set; }
        
        /// <summary>消息时间</summary>
        public DateTime Time { get; set; }
        
        /// <summary>文本内容</summary>
        public string Content { get; set; }
        
        /// <summary>原始数据</summary>
        public string RawData { get; set; }

        /// <summary>快捷属性 (兼容Chain模式)</summary>
        public string GroupId => GroupUin ?? Chain?.GroupId;
        public string SenderId => FriendUin ?? Chain?.SenderId;
        public string SenderNick => Chain?.SenderNick;

        public GroupMessageEvent() { }

        public GroupMessageEvent(WSLMessageChain chain)
        {
            Chain = chain;
        }

        /// <summary>获取预览文本</summary>
        public string ToPreviewText()
        {
            return Content ?? Chain?.GetPlainText() ?? "[空消息]";
        }
    }

    /// <summary>
    /// 私聊消息事件
    /// </summary>
    public class FriendMessageEvent : WSLEventBase
    {
        public WSLMessageChain Chain { get; set; }
        
        /// <summary>好友ID</summary>
        public string FriendUin { get; set; }
        
        /// <summary>发送者昵称</summary>
        public string SenderNickname { get; set; }
        
        /// <summary>消息ID</summary>
        public string MessageId { get; set; }
        
        /// <summary>消息时间</summary>
        public DateTime Time { get; set; }
        
        /// <summary>文本内容</summary>
        public string Content { get; set; }
        
        /// <summary>原始数据</summary>
        public string RawData { get; set; }

        /// <summary>快捷属性</summary>
        public string FriendId => FriendUin ?? Chain?.SenderId;
        public string FriendNick => SenderNickname ?? Chain?.SenderNick;

        public FriendMessageEvent() { }

        public FriendMessageEvent(WSLMessageChain chain)
        {
            Chain = chain;
        }

        /// <summary>获取预览文本</summary>
        public string ToPreviewText()
        {
            return Content ?? Chain?.GetPlainText() ?? "[空消息]";
        }
    }

    #endregion

    #region 群管理事件

    /// <summary>
    /// 群禁言事件
    /// </summary>
    public class GroupMuteEvent : WSLEventBase
    {
        public string GroupId { get; set; }
        public string OperatorId { get; set; }
        public string TargetId { get; set; }     // 为空表示全群禁言
        public bool IsMuted { get; set; }        // true=禁言, false=解禁
        public int Duration { get; set; }        // 禁言时长(秒), 0=永久

        public bool IsAllMuted => string.IsNullOrEmpty(TargetId);

        public GroupMuteEvent(string groupId, bool isMuted, string operatorId = "", string targetId = "", int duration = 0)
        {
            GroupId = groupId;
            IsMuted = isMuted;
            OperatorId = operatorId;
            TargetId = targetId;
            Duration = duration;
        }
    }

    /// <summary>
    /// 群成员变动事件
    /// </summary>
    public class GroupMemberChangeEvent : WSLEventBase
    {
        public string GroupId { get; set; }
        public string MemberId { get; set; }
        public string MemberNick { get; set; }
        public ChangeType Type { get; set; }
        public string OperatorId { get; set; }

        public enum ChangeType
        {
            Join,       // 主动加群
            Invite,     // 被邀请
            Leave,      // 主动退群
            Kick        // 被踢
        }

        public GroupMemberChangeEvent(string groupId, string memberId, ChangeType type, string operatorId = "")
        {
            GroupId = groupId;
            MemberId = memberId;
            Type = type;
            OperatorId = operatorId;
        }
    }

    /// <summary>
    /// 群名片变更事件
    /// </summary>
    public class GroupCardChangeEvent : WSLEventBase
    {
        public string GroupId { get; set; }
        public string MemberId { get; set; }
        public string OldCard { get; set; }
        public string NewCard { get; set; }

        public GroupCardChangeEvent(string groupId, string memberId, string oldCard, string newCard)
        {
            GroupId = groupId;
            MemberId = memberId;
            OldCard = oldCard;
            NewCard = newCard;
        }
    }

    #endregion

    #region 业务事件

    /// <summary>
    /// 开奖结果事件
    /// </summary>
    public class LotteryResultEvent : WSLEventBase
    {
        public string LotteryCode { get; set; }  // 彩种代码, 如 jnd28
        public string Period { get; set; }       // 期号
        public int[] Numbers { get; set; }       // 开奖号码 [4, 3, 6]
        public int Sum { get; set; }             // 和值
        public bool IsBig { get; set; }          // 大
        public bool IsOdd { get; set; }          // 单
        public string SpecialType { get; set; }  // 豹子/顺子/对子
        public DateTime OpenTime { get; set; }   // 开奖时间

        public LotteryResultEvent(string code, string period, int[] numbers)
        {
            LotteryCode = code;
            Period = period;
            Numbers = numbers;
            Sum = numbers[0] + numbers[1] + numbers[2];
            IsBig = Sum >= 14;
            IsOdd = Sum % 2 == 1;
            OpenTime = DateTime.Now;

            // 计算特殊类型
            if (numbers[0] == numbers[1] && numbers[1] == numbers[2])
                SpecialType = "豹子";
            else
            {
                var sorted = (int[])numbers.Clone();
                Array.Sort(sorted);
                if (sorted[2] - sorted[1] == 1 && sorted[1] - sorted[0] == 1)
                    SpecialType = "顺子";
                else if (numbers[0] == numbers[1] || numbers[1] == numbers[2] || numbers[0] == numbers[2])
                    SpecialType = "对子";
                else
                    SpecialType = "杂";
            }
        }

        /// <summary>获取大小单双代码</summary>
        public string GetSizeOddCode()
        {
            var size = IsBig ? "D" : "X";   // D=大, X=小
            var odd = IsOdd ? "A" : "S";    // A=单, S=双
            return $"{size}{odd}";
        }
    }

    /// <summary>
    /// 下注事件
    /// </summary>
    public class BetEvent : WSLEventBase
    {
        public string GroupId { get; set; }
        public string PlayerId { get; set; }
        public string PlayerNick { get; set; }
        public string Period { get; set; }
        public string RawText { get; set; }         // 原始下注文本
        public List<BetItem> Items { get; set; }    // 解析后的下注项
        public decimal TotalAmount { get; set; }    // 总金额
        public bool IsValid { get; set; }           // 是否有效

        public class BetItem
        {
            public string Code { get; set; }        // DD, XS, 13, 豹子...
            public decimal Amount { get; set; }
            public decimal Odds { get; set; }
        }

        public BetEvent(string groupId, string playerId, string rawText)
        {
            GroupId = groupId;
            PlayerId = playerId;
            RawText = rawText;
            Items = new List<BetItem>();
        }
    }

    /// <summary>
    /// 上下分事件
    /// </summary>
    public class ScoreChangeEvent : WSLEventBase
    {
        public string GroupId { get; set; }
        public string PlayerId { get; set; }
        public string PlayerNick { get; set; }
        public ChangeType Type { get; set; }
        public decimal Amount { get; set; }
        public decimal BalanceBefore { get; set; }
        public decimal BalanceAfter { get; set; }
        public string Remark { get; set; }

        public enum ChangeType
        {
            Deposit,    // 上分
            Withdraw,   // 下分
            Win,        // 中奖
            Lose,       // 输分
            Rebate      // 回水
        }

        public ScoreChangeEvent(string playerId, ChangeType type, decimal amount)
        {
            PlayerId = playerId;
            Type = type;
            Amount = amount;
        }
    }

    #endregion
}
