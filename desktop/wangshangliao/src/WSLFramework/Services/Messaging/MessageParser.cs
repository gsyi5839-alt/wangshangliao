using System;
using System.Collections.Generic;
using System.Text;
using System.Web.Script.Serialization;
using WSLFramework.Utils;

namespace WSLFramework.Services
{
    /// <summary>
    /// 旺商聊消息解析服务 - 解析云信协议消息
    /// 从招财狗逆向分析得出
    /// </summary>
    public class MessageParser
    {
        private static readonly JavaScriptSerializer _serializer = new JavaScriptSerializer();
        
        // 消息类型常量
        public const int MSG_TYPE_P2P = 1001;        // 私聊消息
        public const int MSG_TYPE_GROUP = 1002;      // 群消息
        public const int MSG_TYPE_NOTIFY = 1003;     // 关系变动通知
        
        // 群通知类型
        public const string NOTIFY_GROUP_MUTE_ON = "NOTIFY_TYPE_GROUP_MUTE_1";   // 开启禁言
        public const string NOTIFY_GROUP_MUTE_OFF = "NOTIFY_TYPE_GROUP_MUTE_0";  // 解除禁言
        public const string NOTIFY_USER_UPDATE_NAME = "NOTIFY_TYPE_USER_UPDATE_NAME"; // 改名片
        
        /// <summary>
        /// 解析原始消息
        /// </summary>
        public static ParsedMessage Parse(string rawJson)
        {
            try
            {
                var msg = new ParsedMessage { RawJson = rawJson };
                var data = _serializer.Deserialize<Dictionary<string, object>>(rawJson);
                
                if (data == null) return msg;
                
                // 解析基本字段
                if (data.ContainsKey("content"))
                {
                    var content = data["content"] as Dictionary<string, object>;
                    if (content != null)
                    {
                        msg.ServerMsgId = GetLong(content, "server_msg_id");
                        msg.Time = GetLong(content, "time");
                        msg.MsgType = GetInt(content, "msg_type");
                        msg.FromId = GetString(content, "from_id");
                        msg.FromNick = GetString(content, "from_nick");
                        msg.ToAccid = GetString(content, "to_accid");
                        msg.ToType = GetInt(content, "to_type"); // 0=P2P, 1=群聊
                        msg.TalkId = GetString(content, "talk_id");
                        msg.ClientMsgId = GetString(content, "client_msg_id");
                        
                        // 解析消息附件
                        var msgAttach = GetString(content, "msg_attach");
                        if (!string.IsNullOrEmpty(msgAttach))
                        {
                            msg.MsgAttach = msgAttach;
                            ParseMsgAttach(msg, msgAttach);
                        }
                    }
                }
                
                return msg;
            }
            catch (Exception ex)
            {
                Logger.Error($"解析消息异常: {ex.Message}");
                return new ParsedMessage { RawJson = rawJson, Error = ex.Message };
            }
        }
        
        /// <summary>
        /// 解析消息附件 (msg_attach)
        /// </summary>
        private static void ParseMsgAttach(ParsedMessage msg, string attachJson)
        {
            try
            {
                var attach = _serializer.Deserialize<Dictionary<string, object>>(attachJson);
                if (attach == null) return;
                
                // 解析 "b" 字段 - Base64 编码的消息内容
                if (attach.ContainsKey("b"))
                {
                    var b = attach["b"] as string;
                    if (!string.IsNullOrEmpty(b))
                    {
                        msg.MessageContent = DecodeMessageContent(b);
                    }
                }
                
                // 解析 "data" 字段 - 群通知数据
                if (attach.ContainsKey("data"))
                {
                    var dataObj = attach["data"] as Dictionary<string, object>;
                    if (dataObj != null)
                    {
                        // 解析群信息
                        if (dataObj.ContainsKey("team_info"))
                        {
                            var teamInfo = dataObj["team_info"] as Dictionary<string, object>;
                            if (teamInfo != null)
                            {
                                msg.TeamId = GetString(teamInfo, "tid");
                                msg.MuteAll = GetInt(teamInfo, "mute_all") == 1;
                            }
                        }
                        
                        // 解析成员名片信息
                        if (dataObj.ContainsKey("name_cards"))
                        {
                            var nameCards = dataObj["name_cards"] as object[];
                            if (nameCards != null && nameCards.Length > 0)
                            {
                                var card = nameCards[0] as Dictionary<string, object>;
                                if (card != null)
                                {
                                    msg.MemberAccid = GetString(card, "accid");
                                    msg.MemberName = GetString(card, "name");
                                    
                                    // 解析加密的昵称
                                    var ex = GetString(card, "ex");
                                    if (!string.IsNullOrEmpty(ex))
                                    {
                                        try
                                        {
                                            var exData = _serializer.Deserialize<Dictionary<string, object>>(ex);
                                            if (exData != null && exData.ContainsKey("nickname_ciphertext"))
                                            {
                                                msg.NicknameCiphertext = exData["nickname_ciphertext"] as string;
                                            }
                                        }
                                        catch { }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"解析消息附件异常: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 解码消息内容 (Base64 + 自定义协议)
        /// </summary>
        private static string DecodeMessageContent(string encoded)
        {
            try
            {
                // Base64 URL-safe 解码
                var base64 = encoded.Replace('-', '+').Replace('_', '/');
                var padLen = (4 - base64.Length % 4) % 4;
                base64 = base64 + new string('=', padLen);
                
                var bytes = Convert.FromBase64String(base64);
                
                // 跳过协议头，尝试提取文本内容
                // 协议头通常是固定的几个字节
                if (bytes.Length > 20)
                {
                    // 尝试查找 UTF-8 文本
                    var text = TryExtractText(bytes);
                    if (!string.IsNullOrEmpty(text))
                    {
                        return text;
                    }
                }
                
                // 如果无法解码，返回原始 Base64
                return encoded;
            }
            catch
            {
                return encoded;
            }
        }
        
        /// <summary>
        /// 尝试从二进制数据中提取文本
        /// </summary>
        private static string TryExtractText(byte[] bytes)
        {
            try
            {
                // 查找可打印字符区域
                var sb = new StringBuilder();
                bool inText = false;
                int textStart = 0;
                
                for (int i = 0; i < bytes.Length; i++)
                {
                    var b = bytes[i];
                    // 检查是否为可打印 ASCII 或中文 UTF-8
                    if (b >= 0x20 && b < 0x7F || b >= 0xC0)
                    {
                        if (!inText)
                        {
                            inText = true;
                            textStart = i;
                        }
                    }
                    else
                    {
                        if (inText && i - textStart > 3)
                        {
                            var text = Encoding.UTF8.GetString(bytes, textStart, i - textStart);
                            if (IsValidText(text))
                            {
                                sb.Append(text);
                            }
                        }
                        inText = false;
                    }
                }
                
                if (inText && bytes.Length - textStart > 3)
                {
                    var text = Encoding.UTF8.GetString(bytes, textStart, bytes.Length - textStart);
                    if (IsValidText(text))
                    {
                        sb.Append(text);
                    }
                }
                
                return sb.ToString();
            }
            catch
            {
                return null;
            }
        }
        
        /// <summary>
        /// 检查是否为有效文本
        /// </summary>
        private static bool IsValidText(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            
            int validChars = 0;
            foreach (var c in text)
            {
                if (char.IsLetterOrDigit(c) || char.IsPunctuation(c) || c == ' ')
                {
                    validChars++;
                }
            }
            
            return validChars > text.Length / 2;
        }
        
        /// <summary>
        /// 解析下注消息
        /// </summary>
        public static BetInfo ParseBet(string content)
        {
            if (string.IsNullOrEmpty(content)) return null;
            
            content = content.Trim().ToUpper();
            var bet = new BetInfo { RawContent = content };
            
            // 匹配下注格式: D100, X50, DD200, DS150, XD100, XS100, 数字0-27
            if (System.Text.RegularExpressions.Regex.IsMatch(content, @"^(DD|DS|XD|XS|D|X|S|单|双|大|小)\d+$"))
            {
                // 提取下注类型和金额
                var match = System.Text.RegularExpressions.Regex.Match(content, @"^([A-Z]+|[单双大小]+)(\d+)$");
                if (match.Success)
                {
                    bet.BetType = NormalizeBetType(match.Groups[1].Value);
                    bet.Amount = int.Parse(match.Groups[2].Value);
                    bet.IsValid = true;
                }
            }
            // 匹配数字下注: 0-27
            else if (System.Text.RegularExpressions.Regex.IsMatch(content, @"^\d{1,2}\s*[压押]\s*\d+$"))
            {
                var match = System.Text.RegularExpressions.Regex.Match(content, @"^(\d{1,2})\s*[压押]\s*(\d+)$");
                if (match.Success)
                {
                    var num = int.Parse(match.Groups[1].Value);
                    if (num >= 0 && num <= 27)
                    {
                        bet.BetType = "NUM_" + num;
                        bet.Amount = int.Parse(match.Groups[2].Value);
                        bet.IsValid = true;
                    }
                }
            }
            // 上分请求: 上500, +500
            else if (System.Text.RegularExpressions.Regex.IsMatch(content, @"^(上|\+)\s*\d+$"))
            {
                var match = System.Text.RegularExpressions.Regex.Match(content, @"^(上|\+)\s*(\d+)$");
                if (match.Success)
                {
                    bet.BetType = "UP";
                    bet.Amount = int.Parse(match.Groups[2].Value);
                    bet.IsUpDown = true;
                    bet.IsValid = true;
                }
            }
            // 下分请求: 查500, 下500, -500
            else if (System.Text.RegularExpressions.Regex.IsMatch(content, @"^(查|下|-)\s*\d+$"))
            {
                var match = System.Text.RegularExpressions.Regex.Match(content, @"^(查|下|-)\s*(\d+)$");
                if (match.Success)
                {
                    bet.BetType = "DOWN";
                    bet.Amount = int.Parse(match.Groups[2].Value);
                    bet.IsUpDown = true;
                    bet.IsValid = true;
                }
            }
            // 查询余额: 1, 2, 3, 余额, 查
            else if (content == "1" || content == "2" || content == "3" || content == "余额" || content == "查")
            {
                bet.BetType = "QUERY";
                bet.IsQuery = true;
                bet.IsValid = true;
            }
            
            return bet;
        }
        
        /// <summary>
        /// 规范化下注类型
        /// </summary>
        private static string NormalizeBetType(string type)
        {
            switch (type.ToUpper())
            {
                case "D":
                case "大":
                    return "BIG";
                case "X":
                case "小":
                    return "SMALL";
                case "S":
                case "单":
                    return "ODD";
                case "双":
                    return "EVEN";
                case "DD":
                case "大单":
                    return "BIG_ODD";
                case "DS":
                case "大双":
                    return "BIG_EVEN";
                case "XD":
                case "小单":
                    return "SMALL_ODD";
                case "XS":
                case "小双":
                    return "SMALL_EVEN";
                default:
                    return type;
            }
        }
        
        #region 辅助方法
        
        private static string GetString(Dictionary<string, object> dict, string key)
        {
            if (dict.ContainsKey(key))
            {
                return dict[key]?.ToString() ?? "";
            }
            return "";
        }
        
        private static int GetInt(Dictionary<string, object> dict, string key)
        {
            if (dict.ContainsKey(key))
            {
                if (int.TryParse(dict[key]?.ToString(), out int val))
                {
                    return val;
                }
            }
            return 0;
        }
        
        private static long GetLong(Dictionary<string, object> dict, string key)
        {
            if (dict.ContainsKey(key))
            {
                if (long.TryParse(dict[key]?.ToString(), out long val))
                {
                    return val;
                }
            }
            return 0;
        }
        
        #endregion
    }
    
    /// <summary>
    /// 解析后的消息
    /// </summary>
    public class ParsedMessage
    {
        // 原始数据
        public string RawJson { get; set; }
        public string Error { get; set; }
        
        // 基本字段
        public long ServerMsgId { get; set; }
        public long Time { get; set; }
        public int MsgType { get; set; }
        public string FromId { get; set; }
        public string FromNick { get; set; }
        public string ToAccid { get; set; }
        public int ToType { get; set; }
        public string TalkId { get; set; }
        public string ClientMsgId { get; set; }
        public string MsgAttach { get; set; }
        
        // 解析后的内容
        public string MessageContent { get; set; }
        
        // 群通知相关
        public string TeamId { get; set; }
        public bool MuteAll { get; set; }
        public string MemberAccid { get; set; }
        public string MemberName { get; set; }
        public string NicknameCiphertext { get; set; }
        
        // 判断消息类型
        public bool IsP2P => MsgType == MessageParser.MSG_TYPE_P2P || ToType == 0;
        public bool IsGroup => MsgType == MessageParser.MSG_TYPE_GROUP || ToType == 1;
        public bool IsNotify => MsgType == MessageParser.MSG_TYPE_NOTIFY;
        
        // 时间戳转换
        public DateTime MessageTime => DateTimeOffset.FromUnixTimeMilliseconds(Time).LocalDateTime;
    }
    
    /// <summary>
    /// 下注信息
    /// </summary>
    public class BetInfo
    {
        public string RawContent { get; set; }
        public string BetType { get; set; }
        public int Amount { get; set; }
        public bool IsValid { get; set; }
        public bool IsUpDown { get; set; }
        public bool IsQuery { get; set; }
        
        // 下注类型判断
        public bool IsBig => BetType == "BIG" || BetType == "BIG_ODD" || BetType == "BIG_EVEN";
        public bool IsSmall => BetType == "SMALL" || BetType == "SMALL_ODD" || BetType == "SMALL_EVEN";
        public bool IsOdd => BetType == "ODD" || BetType == "BIG_ODD" || BetType == "SMALL_ODD";
        public bool IsEven => BetType == "EVEN" || BetType == "BIG_EVEN" || BetType == "SMALL_EVEN";
        public bool IsNumber => BetType?.StartsWith("NUM_") == true;
        public int BetNumber => IsNumber ? int.Parse(BetType.Substring(4)) : -1;
    }
}
