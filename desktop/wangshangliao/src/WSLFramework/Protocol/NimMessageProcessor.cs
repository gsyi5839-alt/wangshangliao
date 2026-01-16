using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

namespace WSLFramework.Protocol
{
    /// <summary>
    /// NIM消息处理器 - 完全匹配旺商聊的NIM协议
    /// 基于深度逆向分析实现
    /// </summary>
    public class NimMessageProcessor
    {
        private static readonly JavaScriptSerializer _serializer = new JavaScriptSerializer();
        
        #region 消息类型常量
        
        // NIM消息类型
        public const int NIM_MSG_TYPE_TEXT = 0;         // 文本消息
        public const int NIM_MSG_TYPE_IMAGE = 1;        // 图片消息
        public const int NIM_MSG_TYPE_AUDIO = 2;        // 音频消息
        public const int NIM_MSG_TYPE_VIDEO = 3;        // 视频消息
        public const int NIM_MSG_TYPE_NOTIFY = 5;       // 通知消息
        public const int NIM_MSG_TYPE_FILE = 6;         // 文件消息
        public const int NIM_MSG_TYPE_CUSTOM = 100;     // 自定义消息
        
        // 消息来源类型（消息类型码）
        public const int SOURCE_TYPE_P2P = 1001;        // 点对点私聊
        public const int SOURCE_TYPE_TEAM = 1002;       // 群聊
        public const int SOURCE_TYPE_FRIEND_NOTIFY = 1003;   // 好友通知
        public const int SOURCE_TYPE_SYSTEM = 1015;     // 系统通知
        
        // 群通知类型
        public const string NOTIFY_GROUP_MUTE_ON = "NOTIFY_TYPE_GROUP_MUTE_1";    // 禁言
        public const string NOTIFY_GROUP_MUTE_OFF = "NOTIFY_TYPE_GROUP_MUTE_0";   // 解禁
        public const string NOTIFY_USER_UPDATE_NAME = "NOTIFY_TYPE_USER_UPDATE_NAME";  // 修改名片
        
        #endregion
        
        #region 消息解析
        
        /// <summary>
        /// 解析NIM原始JSON消息
        /// </summary>
        public static NimMessage ParseNimMessage(string rawJson)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(rawJson))
                    return null;
                
                var msg = new NimMessage { RawJson = rawJson };
                
                // 解析根层JSON
                var root = _serializer.Deserialize<Dictionary<string, object>>(rawJson);
                if (root == null)
                    return null;
                
                // 解析rescode
                if (root.ContainsKey("rescode"))
                    msg.ResCode = Convert.ToInt32(root["rescode"]);
                
                // 解析content
                if (root.ContainsKey("content"))
                {
                    var content = root["content"] as Dictionary<string, object>;
                    if (content != null)
                    {
                        ParseNimContent(msg, content);
                    }
                }
                
                return msg;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"解析NIM消息失败: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 解析NIM消息内容
        /// </summary>
        private static void ParseNimContent(NimMessage msg, Dictionary<string, object> content)
        {
            // 基础字段
            if (content.ContainsKey("msg_type"))
                msg.MsgType = Convert.ToInt32(content["msg_type"]);
            
            if (content.ContainsKey("from_id"))
                msg.FromId = content["from_id"]?.ToString();
            
            if (content.ContainsKey("from_nick"))
                msg.FromNick = content["from_nick"]?.ToString();
            
            if (content.ContainsKey("to_accid"))
                msg.ToAccId = content["to_accid"]?.ToString();
            
            if (content.ContainsKey("talk_id"))
                msg.TalkId = content["talk_id"]?.ToString();
            
            if (content.ContainsKey("to_type"))
                msg.ToType = Convert.ToInt32(content["to_type"]);
            
            if (content.ContainsKey("time"))
                msg.Timestamp = Convert.ToInt64(content["time"]);
            
            if (content.ContainsKey("server_msg_id"))
                msg.ServerMsgId = content["server_msg_id"]?.ToString();
            
            if (content.ContainsKey("client_msg_id"))
                msg.ClientMsgId = content["client_msg_id"]?.ToString();
            
            // 消息体
            if (content.ContainsKey("msg_body"))
                msg.MsgBody = content["msg_body"]?.ToString();
            
            // msg_attach处理
            if (content.ContainsKey("msg_attach"))
            {
                var attachStr = content["msg_attach"]?.ToString();
                if (!string.IsNullOrEmpty(attachStr))
                {
                    msg.MsgAttachRaw = attachStr;
                    ParseMsgAttach(msg, attachStr);
                }
            }
        }
        
        /// <summary>
        /// 解析msg_attach字段
        /// </summary>
        private static void ParseMsgAttach(NimMessage msg, string attachJson)
        {
            try
            {
                var attach = _serializer.Deserialize<Dictionary<string, object>>(attachJson);
                if (attach == null)
                    return;
                
                // 检查是否有b字段（自定义消息内容）
                if (attach.ContainsKey("b"))
                {
                    var bValue = attach["b"]?.ToString();
                    if (!string.IsNullOrEmpty(bValue))
                    {
                        msg.MsgAttachB = bValue;
                        ParseMsgAttachB(msg, bValue);
                    }
                }
                
                // 解析群通知数据
                if (attach.ContainsKey("data"))
                {
                    var data = attach["data"] as Dictionary<string, object>;
                    if (data != null)
                    {
                        ParseNotifyData(msg, data);
                    }
                }
                
                // 解析id字段
                if (attach.ContainsKey("id"))
                {
                    msg.AttachId = Convert.ToInt32(attach["id"]);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"解析msg_attach失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 解析msg_attach.b字段（URL-safe Base64编码的protobuf数据）
        /// 头部结构: 4字节魔数 + 2字节版本 + 2字节类型 + 4字节长度
        /// </summary>
        private static void ParseMsgAttachB(NimMessage msg, string base64Str)
        {
            try
            {
                // URL-safe Base64解码
                var decoded = UrlSafeBase64Decode(base64Str);
                if (decoded == null || decoded.Length < 12)
                    return;
                
                // 解析头部
                var magic = BitConverter.ToString(decoded, 0, 4).Replace("-", "");
                msg.AttachBMagic = magic;
                
                // 尝试提取文本内容
                var text = TryExtractText(decoded, 12);
                if (!string.IsNullOrEmpty(text))
                {
                    msg.DecodedContent = text;
                    
                    // 尝试解析其中的JSON
                    if (text.Contains("{") && text.Contains("}"))
                    {
                        var start = text.IndexOf("{");
                        var end = text.LastIndexOf("}") + 1;
                        if (start < end)
                        {
                            var jsonStr = text.Substring(start, end - start);
                            try
                            {
                                var jsonData = _serializer.Deserialize<Dictionary<string, object>>(jsonStr);
                                if (jsonData != null)
                                {
                                    msg.EmbeddedJson = jsonData;
                                    
                                    // 提取groupId和nicknameCiphertext
                                    if (jsonData.ContainsKey("groupId"))
                                        msg.GroupId = jsonData["groupId"]?.ToString();
                                    if (jsonData.ContainsKey("nicknameCiphertext"))
                                        msg.NicknameCiphertext = jsonData["nicknameCiphertext"]?.ToString();
                                }
                            }
                            catch { }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"解析msg_attach.b失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 解析群通知数据
        /// </summary>
        private static void ParseNotifyData(NimMessage msg, Dictionary<string, object> data)
        {
            // 解析team_info（群信息）
            if (data.ContainsKey("team_info"))
            {
                var teamInfo = data["team_info"] as Dictionary<string, object>;
                if (teamInfo != null)
                {
                    msg.TeamInfo = new TeamInfo();
                    
                    if (teamInfo.ContainsKey("tid"))
                        msg.TeamInfo.Tid = teamInfo["tid"]?.ToString();
                    if (teamInfo.ContainsKey("mute_all"))
                        msg.TeamInfo.MuteAll = Convert.ToInt32(teamInfo["mute_all"]);
                    if (teamInfo.ContainsKey("mute_type"))
                        msg.TeamInfo.MuteType = Convert.ToInt32(teamInfo["mute_type"]);
                    if (teamInfo.ContainsKey("update_timetag"))
                        msg.TeamInfo.UpdateTimetag = Convert.ToInt64(teamInfo["update_timetag"]);
                }
            }
            
            // 解析name_cards（成员名片）
            if (data.ContainsKey("name_cards"))
            {
                var nameCards = data["name_cards"] as System.Collections.ArrayList;
                if (nameCards != null)
                {
                    msg.NameCards = new List<NameCard>();
                    foreach (var card in nameCards)
                    {
                        var cardDict = card as Dictionary<string, object>;
                        if (cardDict != null)
                        {
                            var nameCard = new NameCard();
                            
                            if (cardDict.ContainsKey("accid"))
                                nameCard.AccId = cardDict["accid"]?.ToString();
                            if (cardDict.ContainsKey("name"))
                                nameCard.Name = cardDict["name"]?.ToString();
                            if (cardDict.ContainsKey("icon"))
                                nameCard.Icon = cardDict["icon"]?.ToString();
                            if (cardDict.ContainsKey("gender"))
                                nameCard.Gender = Convert.ToInt32(cardDict["gender"]);
                            if (cardDict.ContainsKey("ex"))
                            {
                                var exStr = cardDict["ex"]?.ToString();
                                if (!string.IsNullOrEmpty(exStr))
                                {
                                    // 解析ex中的nickname_ciphertext
                                    try
                                    {
                                        var exData = _serializer.Deserialize<Dictionary<string, object>>(exStr);
                                        if (exData != null && exData.ContainsKey("nickname_ciphertext"))
                                        {
                                            nameCard.NicknameCiphertext = exData["nickname_ciphertext"]?.ToString();
                                        }
                                    }
                                    catch { }
                                }
                            }
                            
                            msg.NameCards.Add(nameCard);
                        }
                    }
                }
            }
        }
        
        #endregion
        
        #region 辅助方法
        
        /// <summary>
        /// URL-safe Base64解码
        /// </summary>
        public static byte[] UrlSafeBase64Decode(string input)
        {
            try
            {
                if (string.IsNullOrEmpty(input))
                    return null;
                
                // 替换URL-safe字符
                var base64 = input.Replace('-', '+').Replace('_', '/');
                
                // 补齐padding
                var padding = (4 - base64.Length % 4) % 4;
                base64 += new string('=', padding);
                
                return Convert.FromBase64String(base64);
            }
            catch
            {
                return null;
            }
        }
        
        /// <summary>
        /// 尝试从字节数组提取文本
        /// </summary>
        private static string TryExtractText(byte[] data, int startOffset)
        {
            try
            {
                if (data == null || startOffset >= data.Length)
                    return null;
                
                // 尝试UTF-8解码
                var text = Encoding.UTF8.GetString(data, startOffset, data.Length - startOffset);
                
                // 提取可见字符
                var sb = new StringBuilder();
                foreach (var c in text)
                {
                    if (c >= 32 && c < 127 || c >= 0x4E00 && c <= 0x9FFF) // ASCII可见字符或中文
                    {
                        sb.Append(c);
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
        /// 判断是否是群禁言通知
        /// </summary>
        public static bool IsGroupMuteNotify(NimMessage msg)
        {
            return msg != null && 
                   msg.MsgType == NIM_MSG_TYPE_NOTIFY && 
                   msg.TeamInfo != null;
        }
        
        /// <summary>
        /// 判断是否是群消息
        /// </summary>
        public static bool IsGroupMessage(NimMessage msg)
        {
            return msg != null && msg.ToType == 1;
        }
        
        /// <summary>
        /// 判断是否是私聊消息
        /// </summary>
        public static bool IsPrivateMessage(NimMessage msg)
        {
            return msg != null && msg.ToType == 0;
        }
        
        #endregion
    }
    
    #region 数据模型
    
    /// <summary>
    /// NIM消息
    /// </summary>
    public class NimMessage
    {
        // 原始数据
        public string RawJson { get; set; }
        public int ResCode { get; set; }
        
        // 消息基础信息
        public int MsgType { get; set; }
        public string FromId { get; set; }
        public string FromNick { get; set; }
        public string ToAccId { get; set; }
        public string TalkId { get; set; }
        public int ToType { get; set; }           // 0=私聊, 1=群聊
        public long Timestamp { get; set; }
        public string ServerMsgId { get; set; }
        public string ClientMsgId { get; set; }
        
        // 消息内容
        public string MsgBody { get; set; }
        public string MsgAttachRaw { get; set; }
        public string MsgAttachB { get; set; }
        public string AttachBMagic { get; set; }
        public string DecodedContent { get; set; }
        public int AttachId { get; set; }
        
        // 内嵌JSON数据
        public Dictionary<string, object> EmbeddedJson { get; set; }
        public string GroupId { get; set; }
        public string NicknameCiphertext { get; set; }
        
        // 群信息
        public TeamInfo TeamInfo { get; set; }
        public List<NameCard> NameCards { get; set; }
        
        // 判断方法
        public bool IsMuted => TeamInfo?.MuteAll == 1;
        public bool IsGroupMessage => ToType == 1;
        public bool IsPrivateMessage => ToType == 0;
    }
    
    /// <summary>
    /// 群信息
    /// </summary>
    public class TeamInfo
    {
        public string Tid { get; set; }
        public int MuteAll { get; set; }
        public int MuteType { get; set; }
        public long UpdateTimetag { get; set; }
    }
    
    /// <summary>
    /// 成员名片
    /// </summary>
    public class NameCard
    {
        public string AccId { get; set; }
        public string Name { get; set; }
        public string Icon { get; set; }
        public int Gender { get; set; }
        public string NicknameCiphertext { get; set; }
        public long CreateTimetag { get; set; }
        public long UpdateTimetag { get; set; }
    }
    
    #endregion
}
