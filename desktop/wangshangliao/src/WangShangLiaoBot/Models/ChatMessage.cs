using System;

namespace WangShangLiaoBot.Models
{
    /// <summary>
    /// 聊天消息模型
    /// </summary>
    [Serializable]
    public class ChatMessage
    {
        /// <summary>消息ID (服务器端)</summary>
        public string Id { get; set; }
        /// <summary>消息ID (客户端，用于撤回等操作)</summary>
        public string IdClient { get; set; }
        /// <summary>发送者ID</summary>
        public string SenderId { get; set; }
        /// <summary>发送者昵称</summary>
        public string SenderName { get; set; }
        /// <summary>消息内容</summary>
        public string Content { get; set; }
        /// <summary>消息时间</summary>
        public DateTime Time { get; set; }
        /// <summary>是否是群消息</summary>
        public bool IsGroupMessage { get; set; }
        /// <summary>群ID（如果是群消息）</summary>
        public string GroupId { get; set; }
        /// <summary>是否是自己发送的消息</summary>
        public bool IsSelf { get; set; }
        /// <summary>消息类型</summary>
        public MessageType Type { get; set; }
        /// <summary>是否已处理</summary>
        public bool IsProcessed { get; set; }
        
        // Enhanced fields for bot detection (based on competitor analysis)
        /// <summary>是否为机器人账号（昵称为MD5哈希）</summary>
        public bool IsBot { get; set; }
        /// <summary>消息特征标签（机器人,禁言,管理等）</summary>
        public string Tags { get; set; }
        /// <summary>原始content字段（用于custom消息解码）</summary>
        public string RawContent { get; set; }
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

