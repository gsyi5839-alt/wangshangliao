using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using WSLFramework.Utils;

namespace WSLFramework.Protocol
{
    /// <summary>
    /// ZCG 消息投递格式 (逆向分析所得)
    /// 格式: 机器人账号=X，主动账号=Y，被动账号=Z，群号=G，内容=C，消息ID=M，消息类型=T，消息时间=TM，消息子类型=ST，原始消息=JSON
    /// </summary>
    public class ZCGPluginMessage
    {
        /// <summary>机器人账号 (RQQ)</summary>
        public string RobotAccount { get; set; } = "";
        /// <summary>主动账号 (发送消息的用户)</summary>
        public string ActiveAccount { get; set; } = "";
        /// <summary>被动账号 (被操作的用户，如被@)</summary>
        public string PassiveAccount { get; set; } = "";
        /// <summary>群号 (私聊为空)</summary>
        public string GroupId { get; set; } = "";
        /// <summary>消息内容 (解密后)</summary>
        public string Content { get; set; } = "";
        /// <summary>消息ID</summary>
        public string MsgId { get; set; } = "";
        /// <summary>消息类型 (1001=私聊, 1002=群聊, 1003=关系变动)</summary>
        public int MsgType { get; set; }
        /// <summary>消息时间 (毫秒时间戳)</summary>
        public long MsgTime { get; set; }
        /// <summary>消息子类型 (0=普通, NOTIFY_TYPE_GROUP_MUTE_1=禁言等)</summary>
        public string SubType { get; set; } = "0";
        /// <summary>原始消息JSON</summary>
        public string RawMessage { get; set; } = "";
        
        // 扩展字段
        /// <summary>发送者昵称</summary>
        public string Nickname { get; set; } = "";
        /// <summary>短ID (后4位)</summary>
        public string ShortId => GetShortId(ActiveAccount);
        /// <summary>是否为群消息</summary>
        public bool IsGroupMessage => MsgType == 1002;
        /// <summary>是否为私聊消息</summary>
        public bool IsPrivateMessage => MsgType == 1001;
        /// <summary>消息时间</summary>
        public DateTime MessageTime => DateTimeOffset.FromUnixTimeMilliseconds(MsgTime).LocalDateTime;
        
        /// <summary>
        /// 序列化为插件投递格式
        /// </summary>
        public string ToPluginFormat()
        {
            var sb = new StringBuilder();
            sb.Append($"机器人账号={RobotAccount}，");
            sb.Append($"主动账号={ActiveAccount}，");
            sb.Append($"被动账号={PassiveAccount}，");
            sb.Append($"群号={GroupId}，");
            sb.Append($"内容={Content}，");
            sb.Append($"消息ID={MsgId}，");
            sb.Append($"消息类型={MsgType}，");
            sb.Append($"消息时间={MsgTime}，");
            sb.Append($"消息子类型={SubType}，");
            sb.Append($"原始消息={RawMessage}");
            return sb.ToString();
        }
        
        /// <summary>
        /// 从插件投递格式解析
        /// </summary>
        public static ZCGPluginMessage FromPluginFormat(string format)
        {
            var msg = new ZCGPluginMessage();
            if (string.IsNullOrEmpty(format)) return msg;
            
            // 支持中英文逗号分隔
            var parts = Regex.Split(format, "[，,]");
            
            foreach (var part in parts)
            {
                var idx = part.IndexOf('=');
                if (idx < 0) continue;
                
                var key = part.Substring(0, idx).Trim();
                var value = part.Substring(idx + 1);
                
                switch (key)
                {
                    case "机器人账号": msg.RobotAccount = value; break;
                    case "主动账号": msg.ActiveAccount = value; break;
                    case "被动账号": msg.PassiveAccount = value; break;
                    case "群号": msg.GroupId = value; break;
                    case "内容": msg.Content = value; break;
                    case "消息ID": msg.MsgId = value; break;
                    case "消息类型":
                        int.TryParse(value, out var type);
                        msg.MsgType = type;
                        break;
                    case "消息时间":
                        long.TryParse(value, out var time);
                        msg.MsgTime = time;
                        break;
                    case "消息子类型": msg.SubType = value; break;
                    case "原始消息": msg.RawMessage = value; break;
                }
            }
            
            return msg;
        }
        
        /// <summary>
        /// 从NIM消息创建
        /// </summary>
        public static ZCGPluginMessage FromNimMessage(string robotAccount, string fromId, string toId, 
            string groupId, string content, string msgId, int msgType, long msgTime, 
            string subType = "0", string rawJson = "")
        {
            return new ZCGPluginMessage
            {
                RobotAccount = robotAccount,
                ActiveAccount = fromId,
                PassiveAccount = toId,
                GroupId = groupId,
                Content = content,
                MsgId = msgId,
                MsgType = msgType,
                MsgTime = msgTime,
                SubType = subType,
                RawMessage = rawJson
            };
        }
        
        private static string GetShortId(string fullId)
        {
            if (string.IsNullOrEmpty(fullId) || fullId.Length < 4)
                return fullId ?? "";
            return fullId.Substring(fullId.Length - 4);
        }
    }
    
    /// <summary>
    /// ZCG 开奖结果格式化 (逆向分析所得)
    /// </summary>
    public static class ZCGLotteryFormat
    {
        /// <summary>
        /// 格式化开奖结果消息
        /// 格式: 開:X + Y + Z = N DAS -- H
        /// </summary>
        public static string FormatOpenResult(int num1, int num2, int num3, string period)
        {
            var sum = num1 + num2 + num3;
            var sizeOddEven = GetSizeOddEven(sum);
            var dragonTiger = GetDragonTiger(num1, num3);
            
            return $"开:{num1}+{num2}+{num3}={sum} {sizeOddEven} 期{period}期";
        }
        
        /// <summary>
        /// 格式化详细开奖账单
        /// </summary>
        public static string FormatDetailedResult(int num1, int num2, int num3, string period,
            int playerCount, decimal totalBet, List<LotteryHistoryItem> history = null)
        {
            var sum = num1 + num2 + num3;
            var sizeOddEven = GetSizeOddEven(sum);
            var dragonTiger = GetDragonTiger(num1, num3);
            var special = GetSpecialType(num1, num2, num3);
            
            var sb = new StringBuilder();
            sb.AppendLine($"開:{num1} + {num2} + {num3} = {sum} {sizeOddEven} -- {dragonTiger}");
            sb.AppendLine($"人數:{playerCount}  總分:{totalBet:0}");
            sb.AppendLine("----------------------");
            sb.AppendLine("");
            sb.AppendLine("----------------------");
            
            // 历史记录
            if (history != null && history.Count > 0)
            {
                // 和值历史
                var sumHistory = string.Join(" ", history.ConvertAll(h => h.Sum.ToString("D2")));
                sb.AppendLine($"ls：{sumHistory}");
                
                // 龙虎豹历史 (L=小, H=大, B=单)
                var sizeHistory = string.Join(" ", history.ConvertAll(h => GetSizeChar(h.Sum)));
                sb.AppendLine($"龙虎豹ls：{sizeHistory}");
                
                // 尾数历史
                var tailHistory = string.Join(" ", history.ConvertAll(h => (h.Sum % 10).ToString()));
                sb.AppendLine($"尾球ls：{tailHistory}");
                
                // 特殊形态历史
                var specialHistory = string.Join(" ", history.ConvertAll(h => GetSpecialShort(h.Num1, h.Num2, h.Num3)));
                sb.AppendLine($"豹顺对历史：{specialHistory}");
            }
            
            return sb.ToString();
        }
        
        /// <summary>
        /// 格式化封盘提醒
        /// </summary>
        public static string FormatSealWarning(int seconds)
        {
            return $"--距离封盘时间还有{seconds}秒--\n改注加注带改 或者 加";
        }
        
        /// <summary>
        /// 格式化封盘通知
        /// </summary>
        public static string FormatSealNotice()
        {
            return "==加封盘线==\n以上有钱的都接\n==庄显为准==";
        }
        
        /// <summary>
        /// 格式化卡奖提示
        /// </summary>
        public static string FormatStuckWarning()
        {
            return "本群如遇卡奖情况，十分钟官网没开奖，本期无效！！！！";
        }
        
        /// <summary>
        /// 格式化核对消息
        /// </summary>
        public static string FormatCheckMessage()
        {
            return "核对\n-------------------\n";
        }
        
        /// <summary>
        /// 格式化余额回复
        /// </summary>
        public static string FormatBalanceReply(string userId, string shortId, decimal balance)
        {
            return $"[@{userId}] ({shortId})\n当前余额:{balance:0}";
        }
        
        /// <summary>
        /// 格式化余额不足回复
        /// </summary>
        public static string FormatInsufficientBalance(string userId, string shortId, decimal balance)
        {
            return $"[@{userId}] ({shortId})\n老板，您的账户余额不足！\n当前余粮:{balance:0}";
        }
        
        /// <summary>
        /// 格式化上分成功回复
        /// </summary>
        public static string FormatUpSuccess(string nickname, string shortId, decimal amount, decimal balance)
        {
            return $"{nickname}({shortId})\n上分成功！祝您大吉大利！\n上分:{amount:0}\n余粮:{balance:0}";
        }
        
        /// <summary>
        /// 格式化下分成功回复
        /// </summary>
        public static string FormatDownSuccess(string nickname, string shortId, decimal amount, decimal balance)
        {
            return $"{nickname}({shortId})\n下分成功！\n下分:{amount:0}\n余粮:{balance:0}";
        }
        
        /// <summary>
        /// 格式化下注确认回复
        /// </summary>
        public static string FormatBetConfirm(string userId, string shortId, string betType, 
            decimal amount, decimal balance)
        {
            return $"[@{userId}] ({shortId})\n{betType}{amount:0} 已录取\n余粮:{balance:0}";
        }
        
        /// <summary>
        /// 格式化下注失败回复 (余额不足)
        /// </summary>
        public static string FormatBetFailed(string userId, string shortId, string betType, decimal amount)
        {
            return $"[@{userId}] ({shortId})\n余粮不足，上分后录取：{betType}{amount:0}";
        }
        
        /// <summary>
        /// 获取大小单双代码
        /// </summary>
        private static string GetSizeOddEven(int sum)
        {
            var size = sum >= 14 ? "D" : "X";  // D=大, X=小
            var oddEven = sum % 2 == 1 ? "A" : "S"; // A=单(奇), S=双(偶)
            var combined = $"{size}{oddEven}";
            
            // 完整标识: DAD=大单大, DAS=大单双, XAD=小单大...
            var last = sum >= 14 ? "D" : "S";
            return $"{combined}{last}";
        }
        
        /// <summary>
        /// 获取龙虎结果
        /// </summary>
        private static string GetDragonTiger(int first, int last)
        {
            if (first > last) return "L"; // 龙
            if (first < last) return "H"; // 虎
            return "B"; // 和 (豹?)
        }
        
        /// <summary>
        /// 获取大小字符
        /// </summary>
        private static string GetSizeChar(int sum)
        {
            if (sum >= 14)
                return sum % 2 == 1 ? "H" : "L";
            else
                return sum % 2 == 1 ? "B" : "L";
        }
        
        /// <summary>
        /// 获取特殊形态
        /// </summary>
        private static string GetSpecialType(int n1, int n2, int n3)
        {
            // 豹子 (三个相同)
            if (n1 == n2 && n2 == n3) return "豹子";
            
            // 顺子 (三个连续)
            var sorted = new[] { n1, n2, n3 };
            Array.Sort(sorted);
            if (sorted[2] - sorted[1] == 1 && sorted[1] - sorted[0] == 1) return "顺子";
            
            // 对子 (两个相同)
            if (n1 == n2 || n2 == n3 || n1 == n3) return "对子";
            
            return "杂";
        }
        
        /// <summary>
        /// 获取特殊形态简写
        /// </summary>
        private static string GetSpecialShort(int n1, int n2, int n3)
        {
            if (n1 == n2 && n2 == n3) return "豹";
            
            var sorted = new[] { n1, n2, n3 };
            Array.Sort(sorted);
            if (sorted[2] - sorted[1] == 1 && sorted[1] - sorted[0] == 1) return "顺";
            
            if (n1 == n2 || n2 == n3 || n1 == n3) return "对";
            
            return "--";
        }
    }
    
    /// <summary>
    /// 历史开奖记录项
    /// </summary>
    public class LotteryHistoryItem
    {
        public int Num1 { get; set; }
        public int Num2 { get; set; }
        public int Num3 { get; set; }
        public int Sum => Num1 + Num2 + Num3;
        public string Period { get; set; }
    }
    
    /// <summary>
    /// ZCG API 完整列表 (逆向分析所得)
    /// </summary>
    public static class ZCGApiList
    {
        // ===== 消息发送类 =====
        /// <summary>发送群消息（文本）</summary>
        public const string SEND_GROUP_MSG = "发送群消息（文本）";
        /// <summary>发送好友消息</summary>
        public const string SEND_FRIEND_MSG = "发送好友消息";
        /// <summary>发送群消息(图片)</summary>
        public const string SEND_GROUP_IMG = "发送群消息(图片)";
        
        // ===== 群管理类 =====
        /// <summary>群禁言/解禁</summary>
        public const string GROUP_MUTE = "ww_群禁言解禁";
        /// <summary>修改群名片</summary>
        public const string MODIFY_CARD = "ww_改群名片";
        /// <summary>获取群资料</summary>
        public const string GET_GROUP_INFO = "ww_获取群资料";
        /// <summary>获取群成员</summary>
        public const string GET_GROUP_MEMBERS = "ww_获取群成员";
        /// <summary>踢群成员</summary>
        public const string KICK_MEMBER = "ww_踢群成员";
        
        // ===== 用户信息类 =====
        /// <summary>ID互查</summary>
        public const string ID_LOOKUP = "ww_ID互查";
        /// <summary>获取用户资料</summary>
        public const string GET_USER_INFO = "ww_ID资料";
        
        // ===== 账号管理类 =====
        /// <summary>获取在线账号</summary>
        public const string GET_ONLINE_ACCOUNTS = "云信_获取在线账号";
        /// <summary>获取绑定群</summary>
        public const string GET_BOUND_GROUPS = "取绑定群";
        
        /// <summary>
        /// 构建发送群消息API调用
        /// </summary>
        public static string BuildSendGroupMsg(string robotId, string content, string groupId, 
            int type = 1, int subType = 0)
        {
            return $"{SEND_GROUP_MSG}|{robotId}|{content}|{groupId}|{type}|{subType}";
        }
        
        /// <summary>
        /// 构建发送好友消息API调用
        /// </summary>
        public static string BuildSendFriendMsg(string robotId, string content, string friendId)
        {
            return $"{SEND_FRIEND_MSG}|{robotId}|{content}|{friendId}";
        }
        
        /// <summary>
        /// 构建群禁言API调用
        /// </summary>
        public static string BuildGroupMute(string robotId, string groupId, bool mute)
        {
            var mode = mute ? "1" : "2";  // 1=禁言, 2=解禁
            return $"{GROUP_MUTE}|{robotId}|{groupId}|{mode}";
        }
        
        /// <summary>
        /// 构建修改群名片API调用
        /// </summary>
        public static string BuildModifyCard(string robotId, string groupId, string userId, string newCard)
        {
            return $"{MODIFY_CARD}|{robotId}|{groupId}|{userId}|{newCard}";
        }
        
        /// <summary>
        /// 构建ID互查API调用
        /// </summary>
        public static string BuildIdLookup(string robotId, string targetId)
        {
            return $"{ID_LOOKUP}|{robotId}|{targetId}";
        }
        
        /// <summary>
        /// 解析API响应
        /// </summary>
        public static (bool Success, string Result) ParseApiResponse(string response)
        {
            if (string.IsNullOrEmpty(response))
                return (false, "空响应");
                
            // 查找返回结果
            var idx = response.IndexOf("返回结果:");
            if (idx < 0)
                return (false, response);
                
            var base64Result = response.Substring(idx + 5);
            try
            {
                var decoded = ZCGProtocol.DecryptApiResult(base64Result);
                var success = !decoded.Contains("失败") && !decoded.Contains("错误");
                return (success, decoded);
            }
            catch
            {
                return (false, base64Result);
            }
        }
    }
}
