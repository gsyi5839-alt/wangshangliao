using System;
using WangShangLiaoBot.Utils;

namespace WangShangLiaoBot.Models
{
    /// <summary>
    /// 运行日志条目 - 记录我方机器人运行日志
    /// 使用消息分类系统记录所有操作（开奖、封盘、下注等）
    /// </summary>
    public class RunLogEntry
    {
        /// <summary>日志ID（自增）</summary>
        public int Id { get; set; }
        
        /// <summary>时间</summary>
        public DateTime Time { get; set; }
        
        /// <summary>响应/期号</summary>
        public string Period { get; set; }
        
        /// <summary>日志类型</summary>
        public RunLogType LogType { get; set; }
        
        /// <summary>消息内容</summary>
        public string Message { get; set; }
        
        /// <summary>群号</summary>
        public string GroupId { get; set; }
        
        /// <summary>发送者ID</summary>
        public string SenderId { get; set; }
        
        /// <summary>发送者昵称</summary>
        public string SenderName { get; set; }
        
        // 增强字段（用于消息分类和特征识别）
        /// <summary>是否为机器人账号</summary>
        public bool IsBot { get; set; }
        
        /// <summary>消息特征标签</summary>
        public string Tags { get; set; }
        
        /// <summary>消息类型分类（开奖/下注/封盘等）</summary>
        public CompetitorMessageType CompetitorType { get; set; }
        
        /// <summary>格式化时间显示</summary>
        public string TimeDisplay => Time.ToString("MM-dd HH:mm:ss");
        
        /// <summary>格式化类型显示</summary>
        public string TypeDisplay => GetTypeDisplay();
        
        /// <summary>消息类型显示</summary>
        public string CompetitorTypeDisplay => GetCompetitorTypeDisplay();
        
        private string GetTypeDisplay()
        {
            switch (LogType)
            {
                case RunLogType.SendSuccess: return "投递成功";
                case RunLogType.SendFailed: return "投递失败";
                case RunLogType.ReceiveGroup: return "群消息";
                case RunLogType.ReceiveFriend: return "好友消息";
                case RunLogType.Plugin: return "插件";
                case RunLogType.System: return "系统";
                case RunLogType.Hook: return "Hook";
                case RunLogType.Seal: return "封盘";
                case RunLogType.Unseal: return "开盘";
                case RunLogType.Lottery: return "开奖";
                case RunLogType.BetConfirm: return "下注确认";
                default: return "未知";
            }
        }
        
        private string GetCompetitorTypeDisplay()
        {
            switch (CompetitorType)
            {
                case CompetitorMessageType.Rules: return "规则说明";
                case CompetitorMessageType.Settlement: return "结算账单";
                case CompetitorMessageType.LotteryResult: return "开奖结果";
                case CompetitorMessageType.History: return "历史记录";
                case CompetitorMessageType.AttackReply: return "下注确认";
                case CompetitorMessageType.BalanceReply: return "余额查询";
                case CompetitorMessageType.InsufficientBalance: return "余额不足";
                case CompetitorMessageType.MuteEnable: return "封盘";
                case CompetitorMessageType.MuteDisable: return "开盘";
                default: return "";
            }
        }
    }
    
    /// <summary>
    /// 日志类型枚举 - 我方机器人运行日志类型
    /// </summary>
    public enum RunLogType
    {
        /// <summary>投递成功</summary>
        SendSuccess,
        /// <summary>投递失败</summary>
        SendFailed,
        /// <summary>收到群消息</summary>
        ReceiveGroup,
        /// <summary>收到好友消息</summary>
        ReceiveFriend,
        /// <summary>插件消息</summary>
        Plugin,
        /// <summary>系统消息</summary>
        System,
        /// <summary>Hook消息</summary>
        Hook,
        /// <summary>封盘事件</summary>
        Seal,
        /// <summary>开盘事件</summary>
        Unseal,
        /// <summary>开奖事件</summary>
        Lottery,
        /// <summary>下注确认</summary>
        BetConfirm
    }
}
