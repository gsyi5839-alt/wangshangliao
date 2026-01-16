using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using WSLFramework.Protocol;
using WSLFramework.Utils;

namespace WSLFramework.Services
{
    /// <summary>
    /// ZCG 消息投递服务 - 完全匹配旧程序协议格式
    /// 根据 C:\zcg25.12.11 协议文档实现
    /// </summary>
    public class ZCGMessageDelivery
    {
        private static ZCGMessageDelivery _instance;
        public static ZCGMessageDelivery Instance => _instance ?? (_instance = new ZCGMessageDelivery());
        
        public event Action<string> OnLog;
        
        #region 消息类型定义
        
        /// <summary>消息类型</summary>
        public const int MSG_TYPE_PRIVATE = 1001;       // 私聊消息
        public const int MSG_TYPE_GROUP = 1002;         // 群聊消息
        public const int MSG_TYPE_FRIEND_CHANGE = 1003; // 好友列表变动
        public const int MSG_TYPE_NEW_FRIEND = 1015;    // 新朋友申请
        
        /// <summary>消息子类型</summary>
        public const string SUB_TYPE_NORMAL = "0";                          // 普通消息
        public const string SUB_TYPE_GROUP_MUTE_ON = "NOTIFY_TYPE_GROUP_MUTE_1";   // 群禁言开启
        public const string SUB_TYPE_GROUP_MUTE_OFF = "NOTIFY_TYPE_GROUP_MUTE_0";  // 群禁言解除
        public const string SUB_TYPE_NAME_UPDATE = "NOTIFY_TYPE_USER_UPDATE_NAME"; // 改名片
        
        #endregion
        
        #region 插件投递消息格式
        
        /// <summary>
        /// 构建插件投递消息格式
        /// 格式: 机器人账号=xxx，主动账号=xxx，被动账号=xxx，群号=xxx，内容=xxx，消息ID=xxx，消息类型=xxx，消息时间=xxx
        /// </summary>
        public string BuildPluginDeliveryMessage(
            string robotAccount,
            string activeAccount,
            string passiveAccount,
            string groupId,
            string content,
            string messageId,
            int messageType,
            long messageTime,
            string subType = "0",
            string rawMessage = "")
        {
            var sb = new StringBuilder();
            sb.Append($"机器人账号={robotAccount}，");
            sb.Append($"主动账号={activeAccount}，");
            sb.Append($"被动账号={passiveAccount}，");
            sb.Append($"群号={groupId}，");
            sb.Append($"内容={EscapeContent(content)}，");
            sb.Append($"消息ID={messageId}，");
            sb.Append($"消息类型={messageType}，");
            sb.Append($"消息时间={messageTime}，");
            sb.Append($"消息子类型={subType}");
            
            if (!string.IsNullOrEmpty(rawMessage))
            {
                sb.Append($"，原始消息={rawMessage}");
            }
            
            return sb.ToString();
        }
        
        /// <summary>
        /// 构建消息队列格式 (旧格式兼容)
        /// 格式: 消息队列:登录账号=xxx★操作账号=xxx★...
        /// </summary>
        public string BuildMessageQueue(
            string loginAccount,
            string operateAccount,
            string targetAccount,
            string groupId,
            string content,
            string messageId,
            int messageType,
            long time)
        {
            var sb = new StringBuilder();
            sb.Append("消息队列:");
            sb.Append($"登录账号={loginAccount}★");
            sb.Append($"操作账号={operateAccount}★");
            sb.Append($"目标账号={targetAccount}★");
            sb.Append($"群号={groupId}★");
            sb.Append($"内容={EscapeContent(content)}★");
            sb.Append($"消息ID={messageId}★");
            sb.Append($"消息类型={messageType}★");
            sb.Append($"时间={time}");
            
            return sb.ToString();
        }
        
        /// <summary>
        /// 构建 RQQ 格式消息 (旧协议格式)
        /// </summary>
        public string BuildRQQMessage(
            string robotQQ,
            string groupId,
            string fromQQ,
            string beingQQ,
            string remark,
            string data,
            string content,
            string messageId,
            int messageType,
            long messageTime)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"RQQ:{robotQQ}");
            sb.AppendLine($"群:{groupId}");
            sb.AppendLine($"fromQQ:{fromQQ}");
            sb.AppendLine($"beingQQ:{beingQQ}");
            sb.AppendLine($"备注:{remark}");
            sb.AppendLine($"数据:{data}");
            sb.AppendLine($"内容:{content}");
            sb.AppendLine($"消息ID:{messageId}");
            sb.AppendLine($"消息类型:{messageType}");
            sb.AppendLine($"消息时间:{messageTime}");
            
            return sb.ToString();
        }
        
        #endregion
        
        #region @消息格式
        
        /// <summary>
        /// 构建 @消息内容
        /// 格式: [LQ:@旺商聊号] (简称)\n消息内容
        /// </summary>
        public string BuildAtMessage(string targetUserId, string nickname, string message)
        {
            // 获取简称 (前4位)
            var shortId = targetUserId.Length > 4 ? targetUserId.Substring(0, 4) : targetUserId;
            
            return $"[LQ:@{targetUserId}] ({shortId})\n{message}";
        }
        
        /// <summary>
        /// 构建带昵称的 @消息
        /// 格式: @昵称 (简称)\n消息内容
        /// </summary>
        public string BuildAtMessageWithNick(string targetUserId, string nickname, string message)
        {
            var shortId = targetUserId.Length > 4 ? targetUserId.Substring(0, 4) : targetUserId;
            var displayNick = string.IsNullOrEmpty(nickname) ? shortId : nickname;
            
            return $"[LQ:@{targetUserId}] @{displayNick} ({shortId})\n{message}";
        }
        
        /// <summary>
        /// 解析 @消息，提取被@的用户ID
        /// </summary>
        public List<string> ParseAtUsers(string content)
        {
            var users = new List<string>();
            var regex = new Regex(@"\[LQ:@(\d+)\]");
            var matches = regex.Matches(content);
            
            foreach (Match match in matches)
            {
                if (match.Groups.Count > 1)
                {
                    users.Add(match.Groups[1].Value);
                }
            }
            
            return users;
        }
        
        /// <summary>
        /// 移除 @消息标记，获取纯文本
        /// </summary>
        public string RemoveAtMarks(string content)
        {
            // 移除 [LQ:@xxx] 格式
            var result = Regex.Replace(content, @"\[LQ:@\d+\]\s*", "");
            // 移除 (xxxx) 简称格式
            result = Regex.Replace(result, @"\(\d{4}\)\s*\n?", "");
            return result.Trim();
        }
        
        #endregion
        
        #region 自动回复模板
        
        /// <summary>
        /// 构建余额不足提示
        /// </summary>
        public string BuildBalanceInsufficientReply(string userId, string nickname, decimal balance)
        {
            return BuildAtMessage(userId, nickname, 
                $"老板，您的账户余额不足！\n当前余粮:{balance:F0}");
        }
        
        /// <summary>
        /// 构建上分请求审核回复
        /// </summary>
        public string BuildUpRequestReply(string userId, string nickname)
        {
            return BuildAtMessage(userId, nickname, 
                "亲，您的上分请求我们正在火速审核~");
        }
        
        /// <summary>
        /// 构建上分成功回复
        /// </summary>
        public string BuildUpSuccessReply(string userId, string nickname, decimal amount, decimal balance)
        {
            return BuildAtMessage(userId, nickname, 
                $"{amount:F0}已到账，余粮:{balance:F0}，祝您好运发大财！");
        }
        
        /// <summary>
        /// 构建下分查询回复
        /// </summary>
        public string BuildDownQueryReply(string userId, string nickname, decimal balance)
        {
            return BuildAtMessage(userId, nickname, 
                $"您的余粮:{balance:F0}，请在下期开奖后申请下分！");
        }
        
        /// <summary>
        /// 构建下分处理中回复
        /// </summary>
        public string BuildDownProcessingReply(string userId, string nickname, decimal amount)
        {
            return BuildAtMessage(userId, nickname, 
                $"下分:{amount:F0}正在处理中请等待确认！\n\n");
        }
        
        /// <summary>
        /// 构建下分勿催回复
        /// </summary>
        public string BuildDownNoUrgeReply(string userId, string nickname)
        {
            return BuildAtMessage(userId, nickname, 
                "正在处理中请勿催促，三天内下分到账无需纠结！");
        }
        
        /// <summary>
        /// 构建下注显示回复
        /// </summary>
        public string BuildBetShowReply(string userId, string nickname, string betContent, decimal balance)
        {
            return BuildAtMessage(userId, nickname, 
                $"已录取{betContent}，余粮:{balance:F0}");
        }
        
        /// <summary>
        /// 构建余额不足上分后录取回复
        /// </summary>
        public string BuildBetPendingReply(string userId, string nickname, string betContent)
        {
            return BuildAtMessage(userId, nickname, 
                $"余额不足，上分后录取，{betContent}");
        }
        
        /// <summary>
        /// 构建封盘下注无效回复
        /// </summary>
        public string BuildBetClosedReply(string userId, string nickname, int remainSeconds)
        {
            return BuildAtMessage(userId, nickname, 
                $"下注无效，下注已封，请等待下期\n剩余:{remainSeconds}秒");
        }
        
        /// <summary>
        /// 构建查询回复 (发1)
        /// </summary>
        public string BuildQueryReply(string userId, string nickname, decimal balance, bool hasAttack, string attackContent = "")
        {
            if (balance <= 0)
            {
                return BuildAtMessage(userId, nickname, 
                    "查询：老板您的账户余额不足！\n当前余粮:0");
            }
            else if (hasAttack && !string.IsNullOrEmpty(attackContent))
            {
                return BuildAtMessage(userId, nickname, 
                    $"已录取{attackContent}，余粮:{balance:F0}");
            }
            else
            {
                return BuildAtMessage(userId, nickname, 
                    $"老板余粮还有{balance:F0}，余粮:{balance:F0}");
            }
        }
        
        /// <summary>
        /// 构建托管成功回复
        /// </summary>
        public string BuildTrusteeSuccessReply(string userId, string nickname)
        {
            return BuildAtMessage(userId, nickname, 
                "已为您设置为托管成员");
        }
        
        /// <summary>
        /// 构建取消托管回复
        /// </summary>
        public string BuildTrusteeCancelReply(string userId, string nickname)
        {
            return BuildAtMessage(userId, nickname, 
                "已取消托管身份，请自行下注！");
        }
        
        /// <summary>
        /// 构建取消下注回复
        /// </summary>
        public string BuildBetCancelReply(string userId, string nickname)
        {
            return BuildAtMessage(userId, nickname, 
                "取消下注成功！！！");
        }
        
        /// <summary>
        /// 构建模糊匹配提醒
        /// </summary>
        public string BuildFuzzyMatchReply(string userId, string nickname, string matchedContent)
        {
            return BuildAtMessage(userId, nickname, 
                $"已为您模糊匹配下注:{matchedContent}，请检查后进行下注");
        }
        
        #endregion
        
        #region API调用格式
        
        /// <summary>
        /// 构建 API 调用消息
        /// 格式: API名称|参数1|参数2|...|参数N
        /// </summary>
        public string BuildApiCall(string apiName, params string[] args)
        {
            if (args == null || args.Length == 0)
            {
                return apiName;
            }
            return $"{apiName}|{string.Join("|", args)}";
        }
        
        /// <summary>
        /// 解析 API 返回结果
        /// 格式: API调用内容|返回结果:Base64编码数据
        /// </summary>
        public (string apiCall, string result) ParseApiResponse(string response)
        {
            var parts = response.Split(new[] { "|返回结果:" }, StringSplitOptions.None);
            if (parts.Length >= 2)
            {
                var result = parts[1];
                // 尝试 Base64 解码
                try
                {
                    var bytes = Convert.FromBase64String(result);
                    result = Encoding.UTF8.GetString(bytes);
                }
                catch { }
                
                return (parts[0], result);
            }
            return (response, "");
        }
        
        /// <summary>
        /// 构建发送群消息API调用
        /// </summary>
        public string BuildSendGroupMessageApi(string robotId, string content, string groupId, int msgType = 1, int extParam = 0)
        {
            return BuildApiCall("发送群消息（文本）", robotId, content, groupId, msgType.ToString(), extParam.ToString());
        }
        
        /// <summary>
        /// 构建发送好友消息API调用
        /// </summary>
        public string BuildSendFriendMessageApi(string robotId, string content, string targetId)
        {
            return BuildApiCall("发送好友消息", robotId, content, targetId);
        }
        
        /// <summary>
        /// 构建群禁言API调用
        /// </summary>
        public string BuildGroupMuteApi(string robotId, string groupId, bool mute)
        {
            return BuildApiCall("ww_群禁言解禁", robotId, groupId, mute ? "1" : "2");
        }
        
        /// <summary>
        /// 构建改群名片API调用
        /// </summary>
        public string BuildChangeCardApi(string robotId, string groupId, string userId, string newCard)
        {
            return BuildApiCall("ww_改群名片", robotId, groupId, userId, newCard);
        }
        
        /// <summary>
        /// 构建ID互查API调用
        /// </summary>
        public string BuildIdQueryApi(string robotId, string targetId)
        {
            return BuildApiCall("ww_ID互查", robotId, targetId);
        }
        
        /// <summary>
        /// 构建获取绑定群API调用
        /// </summary>
        public string BuildGetBoundGroupsApi(string robotId)
        {
            return BuildApiCall("取绑定群", robotId);
        }
        
        /// <summary>
        /// 构建获取在线账号API调用
        /// </summary>
        public string BuildGetOnlineAccountsApi()
        {
            return "云信_获取在线账号";
        }
        
        #endregion
        
        #region 工具方法
        
        /// <summary>
        /// 转义消息内容中的特殊字符
        /// </summary>
        private string EscapeContent(string content)
        {
            if (string.IsNullOrEmpty(content)) return "";
            
            // 转义换行符
            return content.Replace("\n", "\\n").Replace("\r", "");
        }
        
        /// <summary>
        /// 还原消息内容中的转义字符
        /// </summary>
        public string UnescapeContent(string content)
        {
            if (string.IsNullOrEmpty(content)) return "";
            
            return content.Replace("\\n", "\n");
        }
        
        /// <summary>
        /// 获取用户简称 (前4位)
        /// </summary>
        public string GetShortId(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return "";
            return userId.Length > 4 ? userId.Substring(0, 4) : userId;
        }
        
        private void Log(string message)
        {
            Logger.Info($"[ZCGMessageDelivery] {message}");
            OnLog?.Invoke(message);
        }
        
        #endregion
    }
    
    /// <summary>
    /// 插件投递消息解析结果
    /// </summary>
    public class PluginDeliveryMessage
    {
        public string RobotAccount { get; set; }
        public string ActiveAccount { get; set; }
        public string PassiveAccount { get; set; }
        public string GroupId { get; set; }
        public string Content { get; set; }
        public string MessageId { get; set; }
        public int MessageType { get; set; }
        public long MessageTime { get; set; }
        public string SubType { get; set; }
        public string RawMessage { get; set; }
        
        /// <summary>
        /// 从插件投递格式解析消息
        /// </summary>
        public static PluginDeliveryMessage Parse(string message)
        {
            var result = new PluginDeliveryMessage();
            
            // 解析格式: 机器人账号=xxx，主动账号=xxx，...
            var regex = new Regex(@"(\w+)=([^，]+)");
            var matches = regex.Matches(message);
            
            foreach (Match match in matches)
            {
                var key = match.Groups[1].Value;
                var value = match.Groups[2].Value;
                
                switch (key)
                {
                    case "机器人账号": result.RobotAccount = value; break;
                    case "主动账号": result.ActiveAccount = value; break;
                    case "被动账号": result.PassiveAccount = value; break;
                    case "群号": result.GroupId = value; break;
                    case "内容": result.Content = value.Replace("\\n", "\n"); break;
                    case "消息ID": result.MessageId = value; break;
                    case "消息类型": int.TryParse(value, out var mt); result.MessageType = mt; break;
                    case "消息时间": long.TryParse(value, out var time); result.MessageTime = time; break;
                    case "消息子类型": result.SubType = value; break;
                    case "原始消息": result.RawMessage = value; break;
                }
            }
            
            return result;
        }
    }
}
