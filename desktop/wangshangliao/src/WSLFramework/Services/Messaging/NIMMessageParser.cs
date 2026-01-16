using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using WSLFramework.Utils;

namespace WSLFramework.Services
{
    /// <summary>
    /// NIM 消息解析器 - 根据旺商聊深度连接协议实现
    /// 负责解析 NIM SDK 消息格式和 Protobuf 数据
    /// </summary>
    public class NIMMessageParser
    {
        #region 常量 - 消息类型 (msg_type)

        public const int MSG_TYPE_TEXT = 0;           // 文本消息
        public const int MSG_TYPE_IMAGE = 1;          // 图片消息
        public const int MSG_TYPE_VOICE = 2;          // 语音消息
        public const int MSG_TYPE_VIDEO = 3;          // 视频消息
        public const int MSG_TYPE_NOTIFICATION = 5;   // 系统通知 (群信息变更等)
        public const int MSG_TYPE_CUSTOM = 100;       // 自定义消息 (旺商聊业务消息)

        #endregion

        #region 常量 - 会话类型 (to_type)

        public const int TO_TYPE_P2P = 0;             // 私聊
        public const int TO_TYPE_TEAM = 1;            // 群聊

        #endregion

        #region 常量 - 设备类型 (from_client_type)

        public const int CLIENT_TYPE_IOS = 2;         // iOS
        public const int CLIENT_TYPE_ANDROID = 4;     // Android
        public const int CLIENT_TYPE_WINDOWS = 16;    // Windows Desktop
        public const int CLIENT_TYPE_WEB = 32;        // Web/服务器

        #endregion

        #region 常量 - 返回码 (rescode)

        public const int RESCODE_SUCCESS = 200;       // 成功

        #endregion

        #region 常量 - 消息子类型 (msg_sub_type)

        public const string SUB_TYPE_NORMAL = "0";
        public const string SUB_TYPE_GROUP_MUTE_ON = "NOTIFY_TYPE_GROUP_MUTE_1";   // 群禁言开启
        public const string SUB_TYPE_GROUP_MUTE_OFF = "NOTIFY_TYPE_GROUP_MUTE_0";  // 群禁言解除
        public const string SUB_TYPE_USER_UPDATE_NAME = "NOTIFY_TYPE_USER_UPDATE_NAME"; // 用户改名片

        #endregion

        #region 常量 - 插件消息类型码 (根据最底层消息协议文档)

        /// <summary>普通消息 (私聊/群聊文本)</summary>
        public const int PLUGIN_MSG_TYPE_NORMAL = 1001;
        /// <summary>群事件消息 (禁言/改名片等)</summary>
        public const int PLUGIN_MSG_TYPE_GROUP_EVENT = 1002;
        /// <summary>好友请求/系统通知</summary>
        public const int PLUGIN_MSG_TYPE_FRIEND_REQUEST = 1003;
        /// <summary>好友申请通知</summary>
        public const int PLUGIN_MSG_TYPE_FRIEND_APPLY = 1015;

        #endregion

        #region 常量 - Protobuf 头部

        // 魔数 (前4字节)
        private static readonly byte[] PROTOBUF_MAGIC = { 0x09, 0x1A, 0x49, 0x1F };
        // Protobuf头部长度
        private const int PROTOBUF_HEADER_SIZE = 12;
        // 可读内容起始偏移 (跳过更多头部)
        private const int PROTOBUF_CONTENT_OFFSET = 27;

        #endregion

        #region 私有字段

        private readonly JavaScriptSerializer _serializer;

        #endregion

        #region 构造函数

        public NIMMessageParser()
        {
            _serializer = new JavaScriptSerializer();
        }

        #endregion

        #region 单例模式

        private static readonly Lazy<NIMMessageParser> _instance =
            new Lazy<NIMMessageParser>(() => new NIMMessageParser());

        public static NIMMessageParser Instance => _instance.Value;

        #endregion

        #region 消息解析

        /// <summary>
        /// 解析 NIM 消息 JSON
        /// </summary>
        public NIMMessage ParseNIMMessage(string json)
        {
            try
            {
                var dict = _serializer.Deserialize<Dictionary<string, object>>(json);
                var message = new NIMMessage { RawJson = json };

                // 解析顶层字段
                if (dict.ContainsKey("rescode"))
                    message.ResCode = Convert.ToInt32(dict["rescode"]);
                if (dict.ContainsKey("feature"))
                    message.Feature = Convert.ToInt32(dict["feature"]);

                // 解析 content 字段
                if (dict.ContainsKey("content"))
                {
                    var content = dict["content"] as Dictionary<string, object>;
                    if (content != null)
                    {
                        ParseContent(message, content);
                    }
                }

                return message;
            }
            catch (Exception ex)
            {
                Logger.Error($"[NIMParser] 解析消息失败: {ex.Message}");
                return new NIMMessage { RawJson = json, ParseError = ex.Message };
            }
        }

        /// <summary>
        /// 解析 content 字段
        /// </summary>
        private void ParseContent(NIMMessage message, Dictionary<string, object> content)
        {
            // 基础字段
            if (content.ContainsKey("client_msg_id"))
                message.ClientMsgId = content["client_msg_id"]?.ToString();
            if (content.ContainsKey("server_msg_id"))
                message.ServerMsgId = Convert.ToInt64(content["server_msg_id"]);
            if (content.ContainsKey("time"))
                message.Time = Convert.ToInt64(content["time"]);
            if (content.ContainsKey("from_id"))
                message.FromId = content["from_id"]?.ToString();
            if (content.ContainsKey("from_nick"))
                message.FromNick = content["from_nick"]?.ToString();
            if (content.ContainsKey("from_client_type"))
                message.FromClientType = Convert.ToInt32(content["from_client_type"]);
            if (content.ContainsKey("to_accid"))
                message.ToAccid = content["to_accid"]?.ToString();
            if (content.ContainsKey("to_type"))
                message.ToType = Convert.ToInt32(content["to_type"]);
            if (content.ContainsKey("talk_id"))
                message.TalkId = content["talk_id"]?.ToString();
            if (content.ContainsKey("msg_type"))
                message.MsgType = Convert.ToInt32(content["msg_type"]);
            if (content.ContainsKey("msg_sub_type"))
                message.MsgSubType = content["msg_sub_type"]?.ToString() ?? "0";
            if (content.ContainsKey("msg_body"))
                message.MsgBody = content["msg_body"]?.ToString();
            if (content.ContainsKey("msg_attach"))
                message.MsgAttach = content["msg_attach"]?.ToString();

            // 解析 msg_attach 中的消息内容
            if (!string.IsNullOrEmpty(message.MsgAttach))
            {
                var (rawBytes, textContent) = DecodeMsgAttach(message.MsgAttach);
                message.DecodedContent = textContent;
                message.DecodedBytes = rawBytes;
            }
        }

        #endregion

        #region msg_attach 解码

        /// <summary>
        /// 解码 msg_attach 中的消息体
        /// 格式: {"b":"URL安全Base64"}
        /// 返回: (原始字节, 可读文本)
        /// </summary>
        public (byte[] rawBytes, string textContent) DecodeMsgAttach(string msgAttach)
        {
            try
            {
                // 解析 JSON
                var attach = _serializer.Deserialize<Dictionary<string, object>>(msgAttach);
                if (attach == null || !attach.ContainsKey("b"))
                {
                    return (null, "");
                }

                var bContent = attach["b"]?.ToString();
                if (string.IsNullOrEmpty(bContent))
                {
                    return (null, "");
                }

                // URL安全Base64 转标准 Base64
                var standardBase64 = bContent.Replace('-', '+').Replace('_', '/');
                
                // 补齐 padding
                var padding = 4 - (standardBase64.Length % 4);
                if (padding < 4)
                {
                    standardBase64 += new string('=', padding);
                }

                // 解码
                var decoded = Convert.FromBase64String(standardBase64);

                // 提取可读文本 (跳过 Protobuf 头部)
                var text = "";
                if (decoded.Length > PROTOBUF_CONTENT_OFFSET)
                {
                    var payload = new byte[decoded.Length - PROTOBUF_CONTENT_OFFSET];
                    Array.Copy(decoded, PROTOBUF_CONTENT_OFFSET, payload, 0, payload.Length);
                    var rawText = Encoding.UTF8.GetString(payload);
                    
                    // 清理不可打印字符，保留中英文和常见符号
                    text = CleanText(rawText);
                }

                return (decoded, text.Trim());
            }
            catch (Exception ex)
            {
                Logger.Error($"[NIMParser] 解码 msg_attach 失败: {ex.Message}");
                return (null, $"解码错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 清理文本，移除不可打印字符
        /// </summary>
        private string CleanText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";

            var sb = new StringBuilder();
            foreach (var c in text)
            {
                // 保留可打印ASCII、中文、换行符
                if ((c >= 0x20 && c <= 0x7E) ||  // ASCII可打印
                    (c >= 0x4E00 && c <= 0x9FFF) ||  // 中文
                    c == '\n' || c == '\r')
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        #endregion

        #region 插件投递格式解析

        /// <summary>
        /// 解析插件投递格式的消息
        /// 格式: 机器人账号=X，主动账号=Y，被动账号=Z，群号=G，内容=C，消息ID=M，消息类型=T，消息时间=TS，消息子类型=ST，原始消息={...JSON...}
        /// </summary>
        public PluginMessage ParsePluginMessage(string text)
        {
            var result = new PluginMessage { RawText = text };

            try
            {
                // 使用正则匹配
                var pattern = @"机器人账号=([^，]*)，主动账号=([^，]*)，被动账号=([^，]*)，群号=([^，]*)，内容=([^，]*)，消息ID=([^，]+)，消息类型=([^，]+)，消息时间=([^，]+)，消息子类型=([^，]+)，原始消息=(.*)";
                var match = Regex.Match(text, pattern, RegexOptions.Singleline);

                if (match.Success)
                {
                    result.RobotId = match.Groups[1].Value;
                    result.FromId = match.Groups[2].Value;
                    result.ToId = match.Groups[3].Value;
                    result.GroupId = match.Groups[4].Value;
                    result.Content = match.Groups[5].Value;
                    result.MsgId = match.Groups[6].Value;
                    result.MsgType = match.Groups[7].Value;
                    result.MsgTime = match.Groups[8].Value;
                    result.MsgSubType = match.Groups[9].Value;
                    result.RawJson = match.Groups[10].Value;
                    result.ParseSuccess = true;
                }
                else
                {
                    // 尝试简单的键值对解析
                    var parts = text.Split('，');
                    foreach (var part in parts)
                    {
                        var kv = part.Split('=');
                        if (kv.Length >= 2)
                        {
                            var key = kv[0].Trim();
                            var value = string.Join("=", kv, 1, kv.Length - 1).Trim();

                            switch (key)
                            {
                                case "机器人账号": result.RobotId = value; break;
                                case "主动账号": result.FromId = value; break;
                                case "被动账号": result.ToId = value; break;
                                case "群号": result.GroupId = value; break;
                                case "内容": result.Content = value; break;
                                case "消息ID": result.MsgId = value; break;
                                case "消息类型": result.MsgType = value; break;
                                case "消息时间": result.MsgTime = value; break;
                                case "消息子类型": result.MsgSubType = value; break;
                                case "原始消息": result.RawJson = value; break;
                            }
                        }
                    }
                    result.ParseSuccess = !string.IsNullOrEmpty(result.RobotId);
                }
            }
            catch (Exception ex)
            {
                result.ParseError = ex.Message;
            }

            return result;
        }

        #endregion

        #region 消息类型判断

        /// <summary>
        /// 判断是否为群消息
        /// </summary>
        public bool IsGroupMessage(NIMMessage message)
        {
            return message?.ToType == TO_TYPE_TEAM;
        }

        /// <summary>
        /// 判断是否为私聊消息
        /// </summary>
        public bool IsP2PMessage(NIMMessage message)
        {
            return message?.ToType == TO_TYPE_P2P;
        }

        /// <summary>
        /// 判断是否为自定义消息 (旺商聊业务消息)
        /// </summary>
        public bool IsCustomMessage(NIMMessage message)
        {
            return message?.MsgType == MSG_TYPE_CUSTOM;
        }

        /// <summary>
        /// 判断是否为系统通知
        /// </summary>
        public bool IsNotification(NIMMessage message)
        {
            return message?.MsgType == MSG_TYPE_NOTIFICATION;
        }

        /// <summary>
        /// 判断是否为群禁言通知
        /// </summary>
        public bool IsGroupMuteNotification(NIMMessage message)
        {
            return message?.MsgSubType == SUB_TYPE_GROUP_MUTE_ON || 
                   message?.MsgSubType == SUB_TYPE_GROUP_MUTE_OFF;
        }

        /// <summary>
        /// 判断是否为群禁言开启
        /// </summary>
        public bool IsGroupMuteOn(NIMMessage message)
        {
            return message?.MsgSubType == SUB_TYPE_GROUP_MUTE_ON;
        }

        /// <summary>
        /// 判断是否为群禁言解除
        /// </summary>
        public bool IsGroupMuteOff(NIMMessage message)
        {
            return message?.MsgSubType == SUB_TYPE_GROUP_MUTE_OFF;
        }

        #endregion

        #region 团队信息解析 (禁言通知)

        /// <summary>
        /// 解析团队信息 (用于禁言通知)
        /// msg_attach 格式: {"data":{"team_info":{"mute_all":1,"mute_type":1,"tid":"xxx","update_timetag":xxx}}}
        /// </summary>
        public TeamInfo ParseTeamInfo(string msgAttach)
        {
            try
            {
                var attach = _serializer.Deserialize<Dictionary<string, object>>(msgAttach);
                if (attach == null)
                    return null;

                var data = attach.ContainsKey("data") ? attach["data"] as Dictionary<string, object> : null;
                if (data == null)
                    return null;

                var teamInfo = data.ContainsKey("team_info") ? data["team_info"] as Dictionary<string, object> : null;
                if (teamInfo == null)
                    return null;

                return new TeamInfo
                {
                    MuteAll = teamInfo.ContainsKey("mute_all") ? Convert.ToInt32(teamInfo["mute_all"]) : 0,
                    MuteType = teamInfo.ContainsKey("mute_type") ? Convert.ToInt32(teamInfo["mute_type"]) : 0,
                    Tid = teamInfo.ContainsKey("tid") ? teamInfo["tid"]?.ToString() : "",
                    UpdateTimetag = teamInfo.ContainsKey("update_timetag") ? Convert.ToInt64(teamInfo["update_timetag"]) : 0
                };
            }
            catch (Exception ex)
            {
                Logger.Error($"[NIMParser] 解析 team_info 失败: {ex.Message}");
                return null;
            }
        }

        #endregion

        #region 设备类型转换

        /// <summary>
        /// 获取设备类型名称
        /// </summary>
        public string GetClientTypeName(int clientType)
        {
            switch (clientType)
            {
                case CLIENT_TYPE_IOS: return "iOS";
                case CLIENT_TYPE_ANDROID: return "Android";
                case CLIENT_TYPE_WINDOWS: return "Windows";
                case CLIENT_TYPE_WEB: return "Web/Server";
                default: return $"Unknown({clientType})";
            }
        }

        #endregion
    }

    #region 数据模型

    /// <summary>
    /// NIM 消息
    /// </summary>
    public class NIMMessage
    {
        // 顶层字段
        public int ResCode { get; set; }
        public int Feature { get; set; }
        public string RawJson { get; set; }
        public string ParseError { get; set; }

        // content 字段
        public string ClientMsgId { get; set; }
        public long ServerMsgId { get; set; }
        public long Time { get; set; }
        public string FromId { get; set; }
        public string FromNick { get; set; }
        public int FromClientType { get; set; }
        public string ToAccid { get; set; }
        public int ToType { get; set; }
        public string TalkId { get; set; }
        public int MsgType { get; set; }
        public string MsgSubType { get; set; }
        public string MsgBody { get; set; }
        public string MsgAttach { get; set; }

        // 解码后的内容
        public string DecodedContent { get; set; }
        public byte[] DecodedBytes { get; set; }

        /// <summary>
        /// 消息时间 (DateTime)
        /// </summary>
        public DateTime MessageTime => DateTimeOffset.FromUnixTimeMilliseconds(Time).LocalDateTime;

        /// <summary>
        /// 是否为群消息
        /// </summary>
        public bool IsGroupMessage => ToType == NIMMessageParser.TO_TYPE_TEAM;

        /// <summary>
        /// 是否为私聊消息
        /// </summary>
        public bool IsP2PMessage => ToType == NIMMessageParser.TO_TYPE_P2P;

        /// <summary>
        /// 是否成功
        /// </summary>
        public bool IsSuccess => ResCode == NIMMessageParser.RESCODE_SUCCESS;
    }

    /// <summary>
    /// 插件投递消息格式
    /// </summary>
    public class PluginMessage
    {
        public string RobotId { get; set; }
        public string FromId { get; set; }
        public string ToId { get; set; }
        public string GroupId { get; set; }
        public string Content { get; set; }
        public string MsgId { get; set; }
        public string MsgType { get; set; }
        public string MsgTime { get; set; }
        public string MsgSubType { get; set; }
        public string RawJson { get; set; }
        public string RawText { get; set; }
        public bool ParseSuccess { get; set; }
        public string ParseError { get; set; }

        /// <summary>
        /// 是否为群消息
        /// </summary>
        public bool IsGroupMessage => MsgType == "1002";

        /// <summary>
        /// 是否为私聊消息
        /// </summary>
        public bool IsP2PMessage => MsgType == "1001";

        /// <summary>
        /// 是否为系统通知
        /// </summary>
        public bool IsSystemNotification => MsgType == "1003";

        /// <summary>
        /// 是否为好友申请
        /// </summary>
        public bool IsFriendRequest => MsgType == "1015";
    }

    /// <summary>
    /// 团队/群组信息
    /// </summary>
    public class TeamInfo
    {
        /// <summary>是否全员禁言 (1=是, 0=否)</summary>
        public int MuteAll { get; set; }
        /// <summary>禁言类型</summary>
        public int MuteType { get; set; }
        /// <summary>群NIM ID</summary>
        public string Tid { get; set; }
        /// <summary>更新时间戳(毫秒)</summary>
        public long UpdateTimetag { get; set; }

        /// <summary>
        /// 是否禁言状态
        /// </summary>
        public bool IsMuted => MuteAll == 1;

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdateTime => DateTimeOffset.FromUnixTimeMilliseconds(UpdateTimetag).LocalDateTime;
    }

    #endregion
}
