using System;

namespace WangShangLiaoBot.Models
{
    /// <summary>
    /// 机器人账号信息
    /// </summary>
    [Serializable]
    public class BotAccount
    {
        /// <summary>ID</summary>
        public int Id { get; set; }
        
        /// <summary>昵称</summary>
        public string Nickname { get; set; }
        
        /// <summary>旺商号(wwid)</summary>
        public string WangWangId { get; set; }
        
        /// <summary>绑定群号</summary>
        public string GroupId { get; set; }
        
        /// <summary>登录状态</summary>
        public AccountStatus Status { get; set; }
        
        /// <summary>是否自动登录</summary>
        public bool AutoLogin { get; set; }
        
        /// <summary>账号(手机号)</summary>
        public string Phone { get; set; }
        
        /// <summary>密码</summary>
        public string Password { get; set; }
        
        /// <summary>调试端口</summary>
        public int DebugPort { get; set; }
        
        /// <summary>旺商聊路径</summary>
        public string ExePath { get; set; }
        
        /// <summary>最后登录时间</summary>
        public DateTime LastLoginTime { get; set; }
        
        /// <summary>创建时间</summary>
        public DateTime CreateTime { get; set; }
        
        /// <summary>备注</summary>
        public string Remark { get; set; }
        
        public BotAccount()
        {
            Status = AccountStatus.Offline;
            DebugPort = 9222;
            CreateTime = DateTime.Now;
        }
        
        public override string ToString()
        {
            return string.Format("{0} ({1})", Nickname ?? "未知", WangWangId ?? "");
        }
    }
    
    /// <summary>
    /// 账号状态
    /// </summary>
    public enum AccountStatus
    {
        /// <summary>离线</summary>
        Offline,
        /// <summary>登录中</summary>
        Logging,
        /// <summary>登录成功</summary>
        Online,
        /// <summary>登录失败</summary>
        Failed
    }
}

