using System;

namespace WangShangLiaoBot.Models
{
    /// <summary>
    /// 玩家/联系人模型
    /// </summary>
    [Serializable]
    public class Player
    {
        /// <summary>旺旺号</summary>
        public string WangWangId { get; set; }
        /// <summary>昵称</summary>
        public string Nickname { get; set; }
        /// <summary>分数/余额</summary>
        public decimal Score { get; set; }
        /// <summary>留分</summary>
        public decimal ReservedScore { get; set; }
        /// <summary>是否是托</summary>
        public bool IsTuo { get; set; }
        /// <summary>是否在黑名单</summary>
        public bool IsBlacklisted { get; set; }
        /// <summary>上次活动时间</summary>
        public DateTime LastActiveTime { get; set; }
        /// <summary>备注</summary>
        public string Remark { get; set; }
    }
    
    /// <summary>
    /// 上下分请求模型
    /// </summary>
    [Serializable]
    public class ScoreRequest
    {
        /// <summary>请求ID</summary>
        public string Id { get; set; }
        /// <summary>玩家旺旺号</summary>
        public string WangWangId { get; set; }
        /// <summary>玩家昵称</summary>
        public string Nickname { get; set; }
        /// <summary>请求类型（上分/下分）</summary>
        public ScoreRequestType Type { get; set; }
        /// <summary>请求金额</summary>
        public decimal Amount { get; set; }
        /// <summary>当前余粮</summary>
        public decimal CurrentBalance { get; set; }
        /// <summary>请求时间</summary>
        public DateTime RequestTime { get; set; }
        /// <summary>请求信息</summary>
        public string Message { get; set; }
        /// <summary>处理状态</summary>
        public ScoreRequestStatus Status { get; set; }
        /// <summary>处理次数</summary>
        public int ProcessCount { get; set; }
    }
    
    /// <summary>
    /// 上下分请求类型
    /// </summary>
    public enum ScoreRequestType
    {
        /// <summary>上分</summary>
        Up,
        /// <summary>下分</summary>
        Down
    }
    
    /// <summary>
    /// 上下分请求状态
    /// </summary>
    public enum ScoreRequestStatus
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
    /// 联系人信息模型
    /// </summary>
    [Serializable]
    public class ContactInfo
    {
        /// <summary>昵称/名称</summary>
        public string Name { get; set; }
        /// <summary>旺商号</summary>
        public string WangShangId { get; set; }
        /// <summary>NIM SDK ID</summary>
        public string NimId { get; set; }
        /// <summary>类型（session/contact/friend）</summary>
        public string Type { get; set; }
        /// <summary>头像URL</summary>
        public string Avatar { get; set; }
        /// <summary>是否在线</summary>
        public bool IsOnline { get; set; }
        /// <summary>最后消息</summary>
        public string LastMessage { get; set; }
        /// <summary>最后消息时间</summary>
        public DateTime LastMessageTime { get; set; }
        
        public override string ToString()
        {
            return $"{Name} ({WangShangId})";
        }
    }
    
    /// <summary>
    /// 完整账户信息模型（用于账号列表功能）
    /// </summary>
    [Serializable]
    public class FullAccountInfo
    {
        /// <summary>用户UID</summary>
        public long Uid { get; set; }
        /// <summary>旺商号</summary>
        public string AccountId { get; set; }
        /// <summary>NIM ID</summary>
        public string NimId { get; set; }
        /// <summary>昵称</summary>
        public string NickName { get; set; }
        /// <summary>手机号</summary>
        public string Phone { get; set; }
        /// <summary>头像</summary>
        public string Avatar { get; set; }
        /// <summary>群列表</summary>
        public System.Collections.Generic.List<GroupInfo> Groups { get; set; }
        
        public FullAccountInfo()
        {
            Groups = new System.Collections.Generic.List<GroupInfo>();
        }
    }
    
    /// <summary>
    /// 群信息模型
    /// </summary>
    [Serializable]
    public class GroupInfo
    {
        /// <summary>群ID</summary>
        public long GroupId { get; set; }
        /// <summary>群名称</summary>
        public string GroupName { get; set; }
        /// <summary>NIM群ID</summary>
        public string NimGroupId { get; set; }
        /// <summary>角色（owner/member）</summary>
        public string Role { get; set; }
        /// <summary>成员数量</summary>
        public int MemberCount { get; set; }
        
        public override string ToString()
        {
            return $"{GroupName} (ID: {GroupId})";
        }
    }
}

