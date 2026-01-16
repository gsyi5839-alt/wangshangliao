using System;

namespace WangShangLiaoBot.Models
{
    /// <summary>
    /// Chat message model - fully compatible with WangShangLiao NIM SDK fields
    /// </summary>
    [Serializable]
    public class ChatMessage
    {
        // ========== Core Message IDs ==========
        /// <summary>Server message ID (idServer from NIM SDK)</summary>
        public string Id { get; set; }
        /// <summary>Client message ID (idClient from NIM SDK, used for recall)</summary>
        public string IdClient { get; set; }
        /// <summary>Session ID (sessionId from NIM SDK)</summary>
        public string SessionId { get; set; }
        /// <summary>消息ID别名（兼容旧代码）</summary>
        public string MsgId => Id;
        
        // ========== Sender Info ==========
        /// <summary>Sender account ID (from field in NIM SDK)</summary>
        public string SenderId { get; set; }
        /// <summary>发送者ID别名（兼容旧代码）</summary>
        public string From => SenderId;
        /// <summary>Sender nickname (fromNick field in NIM SDK)</summary>
        public string SenderName { get; set; }
        /// <summary>发送者昵称别名（兼容旧代码）</summary>
        public string FromNick => SenderName;
        /// <summary>Sender client type (fromClientType from NIM SDK)</summary>
        public int SenderClientType { get; set; }
        /// <summary>Sender device ID (fromDeviceId from NIM SDK)</summary>
        public string SenderDeviceId { get; set; }
        
        // ========== Message Content ==========
        /// <summary>Text content (text field in NIM SDK)</summary>
        public string Content { get; set; }
        /// <summary>Raw content JSON for custom messages (content field in NIM SDK)</summary>
        public string RawContent { get; set; }
        
        // ========== Target Info ==========
        /// <summary>Target/recipient (to field in NIM SDK - teamId for group, account for p2p)</summary>
        public string GroupId { get; set; }
        /// <summary>群ID别名（兼容旧代码）</summary>
        public string TeamId => GroupId;
        /// <summary>Scene type: p2p or team (scene field in NIM SDK)</summary>
        public string Scene { get; set; }
        /// <summary>Message flow direction: in or out (flow field in NIM SDK)</summary>
        public string Flow { get; set; }
        
        // ========== Time & Status ==========
        /// <summary>Message timestamp</summary>
        public DateTime Time { get; set; }
        /// <summary>Message status (status field in NIM SDK)</summary>
        public string Status { get; set; }
        
        // ========== Message Flags ==========
        /// <summary>Is this a group message (scene == 'team')</summary>
        public bool IsGroupMessage { get; set; }
        /// <summary>Is this message sent by self (flow == 'out')</summary>
        public bool IsSelf { get; set; }
        /// <summary>Is this a local message only</summary>
        public bool IsLocal { get; set; }
        /// <summary>Is this a resent message</summary>
        public bool IsResend { get; set; }
        
        // ========== Message Type ==========
        /// <summary>Message type enum</summary>
        public MessageType Type { get; set; }
        /// <summary>Raw type string from NIM SDK (text/custom/image/file/geo/tip etc)</summary>
        public string TypeRaw { get; set; }
        
        // ========== NIM SDK Message Flags ==========
        /// <summary>Can be stored in history (isHistoryable)</summary>
        public bool IsHistoryable { get; set; }
        /// <summary>Can be roamed (isRoamingable)</summary>
        public bool IsRoamingable { get; set; }
        /// <summary>Can be synced (isSyncable)</summary>
        public bool IsSyncable { get; set; }
        /// <summary>Can be pushed (isPushable)</summary>
        public bool IsPushable { get; set; }
        /// <summary>Can be offline (isOfflinable)</summary>
        public bool IsOfflinable { get; set; }
        /// <summary>Counts as unread (isUnreadable)</summary>
        public bool IsUnreadable { get; set; }
        /// <summary>Need to push nickname (needPushNick)</summary>
        public bool NeedPushNick { get; set; }
        /// <summary>Need message receipt (needMsgReceipt)</summary>
        public bool NeedMsgReceipt { get; set; }
        
        // ========== Processing Status ==========
        /// <summary>Has this message been processed by the bot</summary>
        public bool IsProcessed { get; set; }
        
        // ========== Bot Detection ==========
        /// <summary>Is sender a bot account (nickname is MD5 hash)</summary>
        public bool IsBot { get; set; }
        /// <summary>Message feature tags (bot, muted, admin etc)</summary>
        public string Tags { get; set; }
    }
    
    /// <summary>
    /// 消息类型枚举
    /// </summary>
    public enum MessageType
    {
        /// <summary>文本消息</summary>
        Text,
        /// <summary>图片消息</summary>
        Image,
        /// <summary>表情消息</summary>
        Emoji,
        /// <summary>文件消息</summary>
        File,
        /// <summary>系统消息</summary>
        System,
        /// <summary>自定义消息（竞品机器人常用）</summary>
        Custom
    }
}

