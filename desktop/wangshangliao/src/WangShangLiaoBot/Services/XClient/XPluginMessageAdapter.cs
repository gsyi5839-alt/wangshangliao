using System;
using System.Text.RegularExpressions;
using WangShangLiaoBot.Models;

namespace WangShangLiaoBot.Services.XClient
{
    /// <summary>
    /// 接收消息数据模型 - 用于XPlugin消息适配
    /// </summary>
    public class ReceivedMessage
    {
        public string SessionType { get; set; }  // "team" or "p2p"
        public string From { get; set; }
        public string To { get; set; }
        public string Content { get; set; }
        public string MessageId { get; set; }
        public long Timestamp { get; set; }
        public string SenderNick { get; set; }
        public string RawData { get; set; }
    }

    /// <summary>
    /// XPlugin消息适配器 - 将ZCG原版消息格式转换为项目内部格式
    /// 
    /// ZCG原版消息投递格式 (逆向分析获得):
    /// 机器人账号={RQQ}，主动账号={fromQQ}，被动账号={toQQ}，群号={groupId}，
    /// 内容={content}，消息ID={msgId}，消息类型={msgType}，消息时间={timestamp}，
    /// 消息子类型={subType}，原始消息={rawJson}
    /// </summary>
    public static class XPluginMessageAdapter
    {
        /// <summary>
        /// 将XPlugin消息转换为ReceivedMessage
        /// </summary>
        public static ReceivedMessage ToReceivedMessage(XPluginMessage xMsg)
        {
            if (xMsg == null) return null;

            return new ReceivedMessage
            {
                SessionType = xMsg.IsGroupMessage ? "team" : "p2p",
                From = xMsg.FromQQ,
                To = xMsg.IsGroupMessage ? xMsg.GroupId : xMsg.RobotQQ,
                Content = UnescapeContent(xMsg.Content),
                MessageId = xMsg.MessageId,
                Timestamp = xMsg.Timestamp,
                SenderNick = ExtractNickFromRaw(xMsg.RawJson),
                RawData = xMsg.RawJson
            };
        }

        /// <summary>
        /// 将ReceivedMessage转换为XPlugin发送格式
        /// </summary>
        public static string ToSendFormat(ReceivedMessage msg, string robotQQ)
        {
            if (msg == null) return null;

            var content = EscapeContent(msg.Content);

            if (msg.SessionType == "team")
            {
                // 群消息: 发送群消息（文本）|{RQQ}|{内容}|{群号}|{类型}|{标志}
                return $"发送群消息（文本）|{robotQQ}|{content}|{msg.To}|1|0";
            }
            else
            {
                // 私聊消息: 发送好友消息|{RQQ}|{内容}|{目标号}
                return $"发送好友消息|{robotQQ}|{content}|{msg.To}";
            }
        }

        /// <summary>
        /// 解析原始投递字符串
        /// </summary>
        public static ReceivedMessage ParseDeliveryString(string line)
        {
            var xMsg = XPluginMessage.Parse(line);
            return ToReceivedMessage(xMsg);
        }

        /// <summary>
        /// 转义内容中的特殊字符
        /// </summary>
        public static string EscapeContent(string content)
        {
            if (string.IsNullOrEmpty(content)) return content;

            return content
                .Replace("\\", "\\\\")
                .Replace("\r\n", "\\n")
                .Replace("\n", "\\n")
                .Replace("\r", "\\n")
                .Replace("|", "\\|");
        }

        /// <summary>
        /// 反转义内容中的特殊字符
        /// </summary>
        public static string UnescapeContent(string content)
        {
            if (string.IsNullOrEmpty(content)) return content;

            return content
                .Replace("\\n", "\n")
                .Replace("\\|", "|")
                .Replace("\\\\", "\\");
        }

        /// <summary>
        /// 从原始JSON中提取发送者昵称
        /// </summary>
        private static string ExtractNickFromRaw(string rawJson)
        {
            if (string.IsNullOrEmpty(rawJson)) return null;

            try
            {
                // 尝试提取 "nick" 或 "senderNick" 字段
                var nickMatch = Regex.Match(rawJson, @"""(?:nick|senderNick)""\s*:\s*""([^""]+)""");
                if (nickMatch.Success)
                {
                    return nickMatch.Groups[1].Value;
                }
            }
            catch { }

            return null;
        }

        /// <summary>
        /// 解析消息类型代码
        /// </summary>
        public static string GetMessageTypeName(int msgType)
        {
            switch (msgType)
            {
                case 1001: return "私聊消息";
                case 1002: return "群消息";
                case 1003: return "系统消息";
                case 1015: return "好友申请";
                default: return $"未知({msgType})";
            }
        }

        /// <summary>
        /// 构建禁言API字符串
        /// </summary>
        public static string BuildMuteApi(string robotQQ, string groupId, bool mute)
        {
            // ww_群禁言解禁|{机器人号}|{群号}|{动作:1禁言/2解禁}
            return $"ww_群禁言解禁|{robotQQ}|{groupId}|{(mute ? "1" : "2")}";
        }

        /// <summary>
        /// 构建改名片API字符串
        /// </summary>
        public static string BuildSetCardApi(string robotQQ, string groupId, string userId, string newCard)
        {
            // ww_改群名片|{机器人号}|{群号}|{用户号}|{新名片}
            return $"ww_改群名片|{robotQQ}|{groupId}|{userId}|{newCard}";
        }

        /// <summary>
        /// 构建获取群资料API字符串
        /// </summary>
        public static string BuildGetGroupInfoApi(string robotQQ, string groupId)
        {
            // ww_获取群资料|{机器人号}|{群号}
            return $"ww_获取群资料|{robotQQ}|{groupId}";
        }

        /// <summary>
        /// 构建ID互查API字符串
        /// </summary>
        public static string BuildLookupIdApi(string robotQQ, string wangshangliaoId)
        {
            // ww_ID互查|{机器人号}|{旺商聊号}
            return $"ww_ID互查|{robotQQ}|{wangshangliaoId}";
        }

        /// <summary>
        /// 构建获取绑定群API字符串
        /// </summary>
        public static string BuildGetBoundGroupApi(string robotQQ)
        {
            // 取绑定群|{机器人号}
            return $"取绑定群|{robotQQ}";
        }

        /// <summary>
        /// 构建获取在线账号API字符串
        /// </summary>
        public static string BuildGetOnlineAccountsApi()
        {
            // 云信_获取在线账号
            return "云信_获取在线账号";
        }
    }
}
