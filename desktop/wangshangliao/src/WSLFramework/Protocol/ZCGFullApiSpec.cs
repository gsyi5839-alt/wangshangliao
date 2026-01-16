using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace WSLFramework.Protocol
{
    /// <summary>
    /// ZCG完整API规范 - 基于深度逆向分析
    /// 日志来源: c:\zcg25.12.11\YX_Clinent\log1\*调用日志.txt
    /// </summary>
    public static class ZCGFullApiSpec
    {
        #region API名称常量
        
        // 群管理API
        public const string API_GET_CURRENT_GROUP = "取当群";
        public const string API_GET_BOT_ACCOUNT = "机器_读取机器账号";
        public const string API_SEND_GROUP_MSG = "发送群消息(文本版)";
        public const string API_SEND_FRIEND_MSG = "发送好友消息";
        public const string API_GROUP_MUTE = "ww_群禁言解禁";
        public const string API_MODIFY_CARD = "ww_修改群片";
        public const string API_QUERY_ID = "ww_ID查询";
        public const string API_CHECK_FRIEND = "ww_是否朋友及是否好友_查询";
        public const string API_GET_GROUP_MEMBERS = "ww_获取群成员";
        public const string API_LOGIN = "ww_xp登陆接口";
        
        // 消息类型
        public const int MSG_TYPE_TEXT = 0;        // 文本消息
        public const int MSG_TYPE_IMAGE = 1;       // 图片消息
        public const int MSG_TYPE_AT = 2;          // @消息
        
        // 禁言状态
        public const int MUTE_ENABLE = 1;          // 禁言
        public const int MUTE_DISABLE = 2;         // 解禁
        
        #endregion
        
        #region API调用格式解析
        
        /// <summary>
        /// 解析API调用日志行
        /// 格式: 时间   API名称|参数1|参数2|...|返回结果:Base64编码
        /// </summary>
        public static ZCGApiCall ParseApiLog(string logLine)
        {
            if (string.IsNullOrWhiteSpace(logLine))
                return null;
                
            // 匹配时间和API内容
            var match = Regex.Match(logLine, @"^\d+年\d+月\d+日\d+时\d+分\d+秒?\s+(.+)$");
            if (!match.Success)
                return null;
                
            var content = match.Groups[1].Value;
            
            // 分割API名称和参数
            var parts = content.Split('|');
            if (parts.Length < 1)
                return null;
                
            var call = new ZCGApiCall
            {
                ApiName = parts[0],
                Parameters = new List<string>(),
                RawContent = content
            };
            
            // 解析参数和返回值
            for (int i = 1; i < parts.Length; i++)
            {
                var part = parts[i];
                if (part.StartsWith("返回结果:"))
                {
                    call.ReturnValue = part.Substring(5);
                }
                else
                {
                    call.Parameters.Add(part);
                }
            }
            
            return call;
        }
        
        /// <summary>
        /// 构建API调用字符串
        /// </summary>
        public static string BuildApiCall(string apiName, params string[] parameters)
        {
            var sb = new StringBuilder();
            sb.Append(apiName);
            
            foreach (var param in parameters)
            {
                sb.Append("|");
                sb.Append(param ?? "");
            }
            
            return sb.ToString();
        }
        
        #endregion
        
        #region 具体API构建
        
        /// <summary>
        /// 构建发送群消息API调用
        /// 格式: 发送群消息(文本版)|机器人ID|消息内容|群ID|消息类型|是否@|返回结果:Base64
        /// </summary>
        public static string BuildSendGroupMessage(string botId, string message, string groupId, int msgType = 0, int isAt = 0)
        {
            return BuildApiCall(API_SEND_GROUP_MSG, botId, message, groupId, msgType.ToString(), isAt.ToString());
        }
        
        /// <summary>
        /// 构建发送好友消息API调用
        /// 格式: 发送好友消息|机器人ID|消息内容|好友ID|返回结果:Base64
        /// </summary>
        public static string BuildSendFriendMessage(string botId, string message, string friendId)
        {
            return BuildApiCall(API_SEND_FRIEND_MSG, botId, message, friendId);
        }
        
        /// <summary>
        /// 构建群禁言/解禁API调用
        /// 格式: ww_群禁言解禁|机器人ID|群ID|操作(1=禁言,2=解禁)|返回结果:Base64
        /// </summary>
        public static string BuildGroupMute(string botId, string groupId, bool mute)
        {
            return BuildApiCall(API_GROUP_MUTE, botId, groupId, mute ? "1" : "2");
        }
        
        /// <summary>
        /// 构建修改群名片API调用
        /// 格式: ww_修改群片|机器人ID|群ID|成员ID|新名片|返回结果:Base64
        /// </summary>
        public static string BuildModifyCard(string botId, string groupId, string memberId, string newCard)
        {
            return BuildApiCall(API_MODIFY_CARD, botId, groupId, memberId, newCard);
        }
        
        /// <summary>
        /// 构建ID查询API调用
        /// 格式: ww_ID查询|机器人ID|查询ID|返回结果:Base64
        /// </summary>
        public static string BuildIdQuery(string botId, string queryId)
        {
            return BuildApiCall(API_QUERY_ID, botId, queryId);
        }
        
        /// <summary>
        /// 构建好友查询API调用
        /// 格式: ww_是否朋友及是否好友_查询|机器人ID|查询ID|查询类型|返回结果:Base64
        /// </summary>
        public static string BuildFriendCheck(string botId, string queryId, int checkType = 1)
        {
            return BuildApiCall(API_CHECK_FRIEND, botId, queryId, checkType.ToString());
        }
        
        /// <summary>
        /// 构建获取群成员API调用
        /// 格式: ww_获取群成员|机器人ID|群ID|返回结果:Base64
        /// </summary>
        public static string BuildGetGroupMembers(string botId, string groupId)
        {
            return BuildApiCall(API_GET_GROUP_MEMBERS, botId, groupId);
        }
        
        /// <summary>
        /// 构建获取当前群API调用
        /// 格式: 取当群|机器人ID|返回结果:Base64
        /// </summary>
        public static string BuildGetCurrentGroup(string botId)
        {
            return BuildApiCall(API_GET_CURRENT_GROUP, botId);
        }
        
        /// <summary>
        /// 构建获取机器人账号API调用
        /// 格式: 机器_读取机器账号|返回结果:Base64
        /// </summary>
        public static string BuildGetBotAccount()
        {
            return API_GET_BOT_ACCOUNT;
        }
        
        #endregion
        
        #region 消息类型解析
        
        /// <summary>
        /// NIM消息类型
        /// </summary>
        public enum NimMessageType
        {
            PrivateChat = 1001,     // 私聊消息
            GroupChat = 1002,       // 群消息
            FriendNotify = 1003,    // 好友状态变化
            SystemNotify = 1015     // 系统通知
        }
        
        /// <summary>
        /// 群通知类型
        /// </summary>
        public enum GroupNotifyType
        {
            None = 0,
            GroupMute = 1,          // NOTIFY_TYPE_GROUP_MUTE_1 (禁言)
            GroupUnmute = 2,        // NOTIFY_TYPE_GROUP_MUTE_0 (解禁)
            UserUpdateName = 3      // NOTIFY_TYPE_USER_UPDATE_NAME (修改名片)
        }
        
        /// <summary>
        /// 解析群通知类型
        /// </summary>
        public static GroupNotifyType ParseGroupNotifyType(string notifyTypeString)
        {
            if (string.IsNullOrEmpty(notifyTypeString))
                return GroupNotifyType.None;
                
            if (notifyTypeString.Contains("GROUP_MUTE_1"))
                return GroupNotifyType.GroupMute;
            if (notifyTypeString.Contains("GROUP_MUTE_0"))
                return GroupNotifyType.GroupUnmute;
            if (notifyTypeString.Contains("USER_UPDATE_NAME"))
                return GroupNotifyType.UserUpdateName;
                
            return GroupNotifyType.None;
        }
        
        #endregion
        
        #region 特殊消息命令
        
        /// <summary>
        /// 解析余额查询命令
        /// 命令: 1, 2, 查, 余额
        /// </summary>
        public static bool IsBalanceQuery(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return false;
            content = content.Trim();
            return content == "1" || content == "查" || content == "余额";
        }
        
        /// <summary>
        /// 解析历史记录查询命令
        /// 命令: 2
        /// </summary>
        public static bool IsHistoryQuery(string content)
        {
            return !string.IsNullOrWhiteSpace(content) && content.Trim() == "2";
        }
        
        /// <summary>
        /// 解析下注命令
        /// 格式: 类型+金额 或 类型金额
        /// 示例: d100, x100, das500, xs500, xd500
        /// </summary>
        public static List<BetCommand> ParseBetCommands(string content)
        {
            var bets = new List<BetCommand>();
            if (string.IsNullOrWhiteSpace(content))
                return bets;
            
            content = content.ToLower().Trim();
            
            // 匹配模式: 类型+金额
            var patterns = new Dictionary<string, string>
            {
                { @"d(\d+)", "BIG" },           // 大
                { @"x(\d+)", "SMALL" },         // 小
                { @"da(\d+)", "BIG_ODD" },      // 大单
                { @"ds(\d+)", "BIG_EVEN" },     // 大双
                { @"xa(\d+)", "SMALL_ODD" },    // 小单
                { @"xs(\d+)", "SMALL_EVEN" },   // 小双
                { @"xd(\d+)", "SMALL_EVEN" },   // 小双 (别名)
                { @"das(\d+)", "BIG_ODD" },     // 大单 (另一种写法)
                { @"bz(\d+)", "LEOPARD" },      // 豹子
                { @"sz(\d+)", "STRAIGHT" },     // 顺子
                { @"dz(\d+)", "PAIR" },         // 对子
                { @"a(\d+)", "ODD" },           // 单
                { @"s(\d+)", "EVEN" },          // 双
            };
            
            foreach (var pattern in patterns)
            {
                var matches = Regex.Matches(content, pattern.Key, RegexOptions.IgnoreCase);
                foreach (Match match in matches)
                {
                    if (int.TryParse(match.Groups[1].Value, out int amount))
                    {
                        bets.Add(new BetCommand
                        {
                            BetType = pattern.Value,
                            Amount = amount,
                            RawText = match.Value
                        });
                    }
                }
            }
            
            return bets;
        }
        
        /// <summary>
        /// 解析上分命令
        /// 格式: c金额, +金额
        /// </summary>
        public static int ParseUpCommand(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return 0;
            
            content = content.Trim().ToLower();
            
            var match = Regex.Match(content, @"^[c+](\d+)$");
            if (match.Success && int.TryParse(match.Groups[1].Value, out int amount))
            {
                return amount;
            }
            
            return 0;
        }
        
        /// <summary>
        /// 解析下分命令
        /// 格式: 下金额, -金额
        /// </summary>
        public static int ParseDownCommand(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return 0;
            
            content = content.Trim();
            
            var match = Regex.Match(content, @"^[下\-](\d+)$");
            if (match.Success && int.TryParse(match.Groups[1].Value, out int amount))
            {
                return amount;
            }
            
            return 0;
        }
        
        #endregion
        
        #region 响应格式化
        
        /// <summary>
        /// 格式化余额查询响应 - ZCG格式
        /// 格式: [LQ:@QQ号] (短ID)\n老板，您的账户还是零！\n当前余额:0
        /// </summary>
        public static string FormatBalanceResponse(string playerId, int balance)
        {
            var shortId = GetShortId(playerId);
            var sb = new StringBuilder();
            
            sb.AppendLine($"[LQ:@{playerId}] ({shortId})");
            
            if (balance <= 0)
            {
                sb.AppendLine("老板，您的账户还是零！");
            }
            
            sb.Append($"当前余额:{balance}");
            
            return sb.ToString();
        }
        
        /// <summary>
        /// 格式化历史记录响应 - ZCG格式
        /// </summary>
        public static string FormatHistoryResponse(List<int> history, List<string> bigSmall, List<int> lastDigits)
        {
            var sb = new StringBuilder();
            
            // 历史数字
            sb.Append("ls:");
            foreach (var num in history)
            {
                sb.Append($"{num:D2} ");
            }
            sb.AppendLine();
            
            // 大小历史
            sb.Append("大小总ls:");
            foreach (var bs in bigSmall)
            {
                sb.Append($"{bs} ");
            }
            sb.AppendLine();
            
            // 尾数历史
            sb.Append("β维ls:");
            foreach (var digit in lastDigits)
            {
                sb.Append($"{digit} ");
            }
            sb.AppendLine();
            
            // 顺子历史
            sb.Append("加顺子历史开奖 ");
            for (int i = 0; i < history.Count; i++)
            {
                sb.Append("-- ");
            }
            
            return sb.ToString();
        }
        
        /// <summary>
        /// 格式化下注失败响应 - ZCG格式（余额不足）
        /// </summary>
        public static string FormatBetFailedResponse(string playerId, List<BetCommand> bets)
        {
            var shortId = GetShortId(playerId);
            var sb = new StringBuilder();
            
            sb.AppendLine($"[LQ:@{playerId}] ({shortId})");
            sb.Append("余额不足，上分后录取:");
            
            foreach (var bet in bets)
            {
                sb.Append($"{GetBetTypeDisplay(bet.BetType)}{bet.Amount} ");
            }
            
            return sb.ToString().TrimEnd();
        }
        
        /// <summary>
        /// 格式化新好友欢迎消息 - ZCG格式
        /// </summary>
        public static string FormatNewFriendWelcome(string nickname, string shortId)
        {
            return $"{nickname}({shortId})\n欢迎入团，钱不要多，运气要帅…\n未开盘期间请先来一波\n或业务量已结束";
        }
        
        /// <summary>
        /// 格式化群名片修改通知 - ZCG格式
        /// </summary>
        public static string FormatCardChangeNotice(string nickname, string shortId, string newCard)
        {
            return $"{nickname}号({shortId})群名片自动修改为:{newCard}";
        }
        
        /// <summary>
        /// 格式化私聊后缀 - ZCG格式
        /// </summary>
        public static string FormatPrivateMessageSuffix(int countdown)
        {
            return $"\n或业务量已结束还有{countdown}秒";
        }
        
        /// <summary>
        /// 获取短ID（后四位）
        /// </summary>
        public static string GetShortId(string playerId)
        {
            if (string.IsNullOrEmpty(playerId))
                return "0000";
            return playerId.Length > 4 ? playerId.Substring(playerId.Length - 4) : playerId;
        }
        
        /// <summary>
        /// 获取下注类型显示名称
        /// </summary>
        public static string GetBetTypeDisplay(string betType)
        {
            switch (betType)
            {
                case "BIG": return "D";
                case "SMALL": return "X";
                case "ODD": return "A";
                case "EVEN": return "S";
                case "BIG_ODD": return "DA";
                case "BIG_EVEN": return "DS";
                case "SMALL_ODD": return "XA";
                case "SMALL_EVEN": return "XS";
                case "LEOPARD": return "BZ";
                case "STRAIGHT": return "SZ";
                case "PAIR": return "DZ";
                default: return betType;
            }
        }
        
        #endregion
    }
    
    /// <summary>
    /// API调用信息
    /// </summary>
    public class ZCGApiCall
    {
        public string ApiName { get; set; }
        public List<string> Parameters { get; set; }
        public string ReturnValue { get; set; }
        public string RawContent { get; set; }
    }
    
    /// <summary>
    /// 下注命令
    /// </summary>
    public class BetCommand
    {
        public string BetType { get; set; }
        public int Amount { get; set; }
        public string RawText { get; set; }
    }
}
