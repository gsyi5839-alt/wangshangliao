using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

namespace WSLFramework.Models
{
    #region 消息类型枚举

    /// <summary>
    /// ZCG消息类型 - 匹配旺商聊NIM协议
    /// </summary>
    public enum ZCGMessageType
    {
        /// <summary>私聊消息 (P2P)</summary>
        PrivateMessage = 1001,
        /// <summary>群消息 (Team)</summary>
        GroupMessage = 1002,
        /// <summary>关系变动 (好友请求等)</summary>
        RelationChange = 1003,
        /// <summary>系统通知</summary>
        SystemNotify = 1015,
    }

    /// <summary>
    /// 框架内部消息类型
    /// </summary>
    public enum FrameworkMessageType
    {
        /// <summary>登录请求</summary>
        Login = 1,
        /// <summary>登录结果</summary>
        LoginResult = 2,
        /// <summary>心跳</summary>
        Heartbeat = 3,
        /// <summary>API调用请求</summary>
        ApiRequest = 10,
        /// <summary>API调用响应</summary>
        ApiResponse = 11,
        /// <summary>发送群消息</summary>
        SendGroupMessage = 20,
        /// <summary>发送私聊消息</summary>
        SendPrivateMessage = 21,
        /// <summary>收到群消息</summary>
        ReceiveGroupMessage = 30,
        /// <summary>收到私聊消息</summary>
        ReceivePrivateMessage = 31,
        /// <summary>消息队列推送</summary>
        MessageQueue = 40,
        /// <summary>群操作 (禁言/解禁等)</summary>
        GroupOperation = 50,
        /// <summary>开始算账 (接管群聊)</summary>
        StartAccounting = 60,
        /// <summary>停止算账</summary>
        StopAccounting = 61,
        /// <summary>设置活跃群</summary>
        SetActiveGroup = 62,
        /// <summary>获取绑定群号</summary>
        GetBoundGroup = 63,
        /// <summary>获取账号信息</summary>
        GetAccountInfo = 64,
        /// <summary>账号信息响应</summary>
        AccountInfo = 65,
        
        // ===== 配置同步消息类型 (70-89) =====
        /// <summary>同步全部配置</summary>
        SyncFullConfig = 70,
        /// <summary>同步赔率配置</summary>
        SyncOddsConfig = 71,
        /// <summary>同步封盘配置</summary>
        SyncSealingConfig = 72,
        /// <summary>同步托管配置</summary>
        SyncTrusteeConfig = 73,
        /// <summary>同步自动回复配置</summary>
        SyncAutoReplyConfig = 74,
        /// <summary>同步话术模板配置</summary>
        SyncTemplateConfig = 75,
        /// <summary>同步基本设置</summary>
        SyncBasicConfig = 76,
        /// <summary>配置同步响应</summary>
        SyncConfigResponse = 79,
        
        // ===== 开奖相关消息类型 (80-89) =====
        /// <summary>开奖结果通知</summary>
        LotteryResult = 80,
        /// <summary>封盘通知</summary>
        SealingNotify = 81,
        /// <summary>封盘提醒通知</summary>
        ReminderNotify = 82,
        /// <summary>期号更新</summary>
        PeriodUpdate = 83,
        
        /// <summary>CDP命令</summary>
        CDPCommand = 100,
        /// <summary>CDP响应</summary>
        CDPResponse = 101,
        /// <summary>错误</summary>
        Error = 999
    }

    /// <summary>
    /// 消息解析后类型 - 群消息附加信息
    /// </summary>
    public enum ParsedMessageType
    {
        /// <summary>普通消息</summary>
        Normal = 0,
        /// <summary>群禁言通知 (mute_all=1)</summary>
        GroupMuteEnable = 1,
        /// <summary>群解禁通知 (mute_all=0)</summary>
        GroupMuteDisable = 2,
        /// <summary>用户更新昵称</summary>
        UserUpdateName = 3,
        /// <summary>用户加入群</summary>
        UserJoinTeam = 4,
        /// <summary>用户退出群</summary>
        UserLeaveTeam = 5,
        /// <summary>用户被踢出群</summary>
        UserKicked = 6,
    }

    #endregion

    #region ZCG消息队列格式

    /// <summary>
    /// ZCG消息队列 - 匹配 "消息队列:登录账号=xxx★操作账号=xxx★..." 格式
    /// </summary>
    public class ZCGMessageQueue
    {
        /// <summary>字段分隔符</summary>
        public const char SEPARATOR = '★';
        /// <summary>消息队列前缀</summary>
        public const string PREFIX = "消息队列:";

        /// <summary>登录账号</summary>
        public string LoginAccount { get; set; }

        /// <summary>操作账号</summary>
        public string OperateAccount { get; set; }

        /// <summary>发送账号</summary>
        public string SendAccount { get; set; }

        /// <summary>群号</summary>
        public string GroupId { get; set; }

        /// <summary>内容</summary>
        public string Content { get; set; }

        /// <summary>消息ID</summary>
        public string MessageId { get; set; }

        /// <summary>消息类型 (1001/1002/1003/1015)</summary>
        public int MessageType { get; set; }

        /// <summary>消息时间 (毫秒时间戳)</summary>
        public long MessageTime { get; set; }

        /// <summary>消息解析后 (如: NOTIFY_TYPE_GROUP_MUTE_1)</summary>
        public string ParsedType { get; set; }

        /// <summary>原始消息JSON</summary>
        public string RawMessage { get; set; }

        /// <summary>
        /// 转换为ZCG格式字符串
        /// </summary>
        public string ToZCGFormat()
        {
            var parts = new List<string>
            {
                $"登录账号={LoginAccount ?? ""}",
                $"操作账号={OperateAccount ?? ""}",
                $"发送账号={SendAccount ?? ""}",
                $"群号={GroupId ?? ""}",
                $"内容={Content ?? ""}",
                $"消息ID={MessageId ?? ""}",
                $"消息类型={MessageType}",
                $"消息时间={MessageTime}",
                $"消息解析后={ParsedType ?? ""}",
                $"原始消息={RawMessage ?? ""}"
            };

            return PREFIX + string.Join(SEPARATOR.ToString(), parts);
        }

        /// <summary>
        /// 从ZCG格式字符串解析
        /// </summary>
        public static ZCGMessageQueue FromZCGFormat(string data)
        {
            if (string.IsNullOrEmpty(data))
                return null;

            // 移除前缀
            if (data.StartsWith(PREFIX))
                data = data.Substring(PREFIX.Length);

            var result = new ZCGMessageQueue();
            var parts = data.Split(SEPARATOR);

            foreach (var part in parts)
            {
                var idx = part.IndexOf('=');
                if (idx <= 0) continue;

                var key = part.Substring(0, idx).Trim();
                var value = part.Substring(idx + 1);

                switch (key)
                {
                    case "登录账号": result.LoginAccount = value; break;
                    case "操作账号": result.OperateAccount = value; break;
                    case "发送账号": result.SendAccount = value; break;
                    case "群号": result.GroupId = value; break;
                    case "内容": result.Content = value; break;
                    case "消息ID": result.MessageId = value; break;
                    case "消息类型":
                        int.TryParse(value, out int msgType);
                        result.MessageType = msgType;
                        break;
                    case "消息时间":
                        long.TryParse(value, out long msgTime);
                        result.MessageTime = msgTime;
                        break;
                    case "消息解析后": result.ParsedType = value; break;
                    case "原始消息": result.RawMessage = value; break;
                }
            }

            return result;
        }

        /// <summary>
        /// 从NIM JSON消息创建
        /// </summary>
        public static ZCGMessageQueue FromNIMMessage(string nimJson, string loginAccount)
        {
            if (string.IsNullOrEmpty(nimJson))
                return null;

            try
            {
                var serializer = new JavaScriptSerializer();
                var nimMsg = serializer.Deserialize<Dictionary<string, object>>(nimJson);

                if (!nimMsg.ContainsKey("content"))
                    return null;

                var content = nimMsg["content"] as Dictionary<string, object>;
                if (content == null)
                    return null;

                var result = new ZCGMessageQueue
                {
                    LoginAccount = loginAccount,
                    RawMessage = nimJson
                };

                // 解析基本字段
                if (content.ContainsKey("from_id"))
                    result.SendAccount = content["from_id"]?.ToString();
                
                if (content.ContainsKey("talk_id"))
                    result.OperateAccount = content["talk_id"]?.ToString();

                if (content.ContainsKey("server_msg_id"))
                    result.MessageId = content["server_msg_id"]?.ToString();

                if (content.ContainsKey("time"))
                {
                    long.TryParse(content["time"]?.ToString(), out long time);
                    result.MessageTime = time;
                }

                // 判断消息类型
                if (content.ContainsKey("to_type"))
                {
                    var toType = content["to_type"]?.ToString();
                    result.MessageType = toType == "1" ? 1002 : 1001; // 1=群消息, 0=私聊
                }

                // 群消息处理
                if (result.MessageType == 1002 && content.ContainsKey("to_accid"))
                {
                    result.GroupId = content["to_accid"]?.ToString();
                }

                // 解析消息内容
                if (content.ContainsKey("msg_attach"))
                {
                    var msgAttach = content["msg_attach"]?.ToString();
                    result.Content = ParseMsgAttach(msgAttach, out string parsedType);
                    result.ParsedType = parsedType;
                }
                else if (content.ContainsKey("msg_body"))
                {
                    result.Content = content["msg_body"]?.ToString();
                }

                // 检查特殊消息类型
                if (content.ContainsKey("msg_type"))
                {
                    var msgType = content["msg_type"]?.ToString();
                    if (msgType == "5") // 通知消息
                    {
                        result.ParsedType = ParseNotifyType(content);
                    }
                }

                return result;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 解析 msg_attach 字段
        /// </summary>
        private static string ParseMsgAttach(string msgAttach, out string parsedType)
        {
            parsedType = "";

            if (string.IsNullOrEmpty(msgAttach))
                return "";

            try
            {
                var serializer = new JavaScriptSerializer();
                var attach = serializer.Deserialize<Dictionary<string, object>>(msgAttach);

                // 检查是否包含 b 字段 (Base64编码的Protobuf数据)
                if (attach.ContainsKey("b"))
                {
                    var b64Data = attach["b"]?.ToString();
                    // TODO: 解码 URL-safe Base64 和 Protobuf 数据
                    // 这里先返回原始数据，后续可以添加完整的Protobuf解析
                    return $"[Protobuf Data: {b64Data?.Substring(0, Math.Min(50, b64Data?.Length ?? 0))}...]";
                }

                // 检查群通知类型
                if (attach.ContainsKey("data"))
                {
                    var data = attach["data"] as Dictionary<string, object>;
                    if (data != null && data.ContainsKey("team_info"))
                    {
                        var teamInfo = data["team_info"] as Dictionary<string, object>;
                        if (teamInfo != null && teamInfo.ContainsKey("mute_all"))
                        {
                            var muteAll = teamInfo["mute_all"]?.ToString();
                            parsedType = muteAll == "1" ? "NOTIFY_TYPE_GROUP_MUTE_1" : "NOTIFY_TYPE_GROUP_MUTE_0";
                        }
                    }
                }

                return msgAttach;
            }
            catch
            {
                return msgAttach;
            }
        }

        /// <summary>
        /// 解析通知类型
        /// </summary>
        private static string ParseNotifyType(Dictionary<string, object> content)
        {
            try
            {
                if (content.ContainsKey("msg_attach"))
                {
                    var msgAttach = content["msg_attach"]?.ToString();
                    var serializer = new JavaScriptSerializer();
                    var attach = serializer.Deserialize<Dictionary<string, object>>(msgAttach);

                    if (attach.ContainsKey("data"))
                    {
                        var data = attach["data"] as Dictionary<string, object>;
                        if (data != null && data.ContainsKey("team_info"))
                        {
                            var teamInfo = data["team_info"] as Dictionary<string, object>;
                            if (teamInfo != null && teamInfo.ContainsKey("mute_all"))
                            {
                                return teamInfo["mute_all"]?.ToString() == "1"
                                    ? "NOTIFY_TYPE_GROUP_MUTE_1"
                                    : "NOTIFY_TYPE_GROUP_MUTE_0";
                            }
                        }
                    }
                }
            }
            catch { }

            return "";
        }
    }

    #endregion

    #region 框架消息类

    /// <summary>
    /// 框架通信消息 - 匹配ZCG协议
    /// </summary>
    public class FrameworkMessage
    {
        private static readonly JavaScriptSerializer _serializer = new JavaScriptSerializer();

        #region 属性

        /// <summary>消息ID</summary>
        public string Id { get; set; }

        /// <summary>消息类型</summary>
        public FrameworkMessageType Type { get; set; }

        /// <summary>消息内容</summary>
        public string Content { get; set; }

        /// <summary>发送者ID</summary>
        public string SenderId { get; set; }

        /// <summary>接收者ID</summary>
        public string ReceiverId { get; set; }

        /// <summary>群ID</summary>
        public string GroupId { get; set; }

        /// <summary>登录账号 (RQQ)</summary>
        public string LoginAccount { get; set; }

        /// <summary>是否成功</summary>
        public bool Success { get; set; }

        /// <summary>错误消息</summary>
        public string Error { get; set; }

        /// <summary>时间戳 (毫秒)</summary>
        public long Timestamp { get; set; }

        /// <summary>附加数据 (JSON格式)</summary>
        public string Extra { get; set; }

        /// <summary>API名称 (用于API调用)</summary>
        public string ApiName { get; set; }

        /// <summary>API参数 (用于API调用)</summary>
        public string[] ApiParams { get; set; }

        /// <summary>API返回结果 (Base64编码)</summary>
        public string ApiResult { get; set; }

        /// <summary>消息队列数据</summary>
        public ZCGMessageQueue MessageQueue { get; set; }

        #endregion

        #region 构造函数

        public FrameworkMessage()
        {
            Id = Guid.NewGuid().ToString("N").Substring(0, 8);
            Timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        }

        public FrameworkMessage(FrameworkMessageType type) : this()
        {
            Type = type;
        }

        public FrameworkMessage(FrameworkMessageType type, string content) : this(type)
        {
            Content = content;
        }

        #endregion

        #region 序列化方法

        /// <summary>
        /// 序列化为 JSON
        /// </summary>
        public string ToJson()
        {
            return _serializer.Serialize(this);
        }

        /// <summary>
        /// 从 JSON 反序列化
        /// </summary>
        public static FrameworkMessage FromJson(string json)
        {
            try
            {
                return _serializer.Deserialize<FrameworkMessage>(json);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 转换为ZCG日志格式
        /// </summary>
        public string ToZCGLogFormat()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{DateTime.Now:yyyy/M/d H:m:s}    ");
            sb.AppendLine($"RQQ:{LoginAccount ?? ""}");
            sb.AppendLine($"群:{GroupId ?? ""}");
            sb.AppendLine($"fromQQ:{SenderId ?? ""}");
            sb.AppendLine($"beingQQ:{ReceiverId ?? ""}");
            sb.AppendLine($"备注:{GetOperationType()}");
            sb.AppendLine($"数据:");
            sb.AppendLine(Content ?? "");
            return sb.ToString();
        }

        private string GetOperationType()
        {
            switch (Type)
            {
                case FrameworkMessageType.SendGroupMessage:
                    return "自动回复发群消息";
                case FrameworkMessageType.ReceiveGroupMessage:
                    return "收到群消息";
                case FrameworkMessageType.ReceivePrivateMessage:
                    return "收到私聊消息";
                case FrameworkMessageType.GroupOperation:
                    return "群操作";
                case FrameworkMessageType.ApiRequest:
                    return $"API调用:{ApiName}";
                default:
                    return "自动回复日志";
            }
        }

        #endregion

        #region 工厂方法

        /// <summary>
        /// 创建成功响应
        /// </summary>
        public static FrameworkMessage CreateSuccess(string requestId, FrameworkMessageType type, string content = null)
        {
            return new FrameworkMessage(type)
            {
                Id = requestId,
                Success = true,
                Content = content
            };
        }

        /// <summary>
        /// 创建错误响应
        /// </summary>
        public static FrameworkMessage CreateError(string requestId, string error)
        {
            return new FrameworkMessage(FrameworkMessageType.Error)
            {
                Id = requestId,
                Success = false,
                Error = error
            };
        }

        /// <summary>
        /// 创建API调用请求
        /// </summary>
        public static FrameworkMessage CreateApiRequest(string apiName, params string[] apiParams)
        {
            return new FrameworkMessage(FrameworkMessageType.ApiRequest)
            {
                ApiName = apiName,
                ApiParams = apiParams,
                Content = $"{apiName}|{string.Join("|", apiParams)}"
            };
        }

        /// <summary>
        /// 创建API调用响应
        /// </summary>
        public static FrameworkMessage CreateApiResponse(string requestId, string apiName, string result)
        {
            return new FrameworkMessage(FrameworkMessageType.ApiResponse)
            {
                Id = requestId,
                Success = true,
                ApiName = apiName,
                ApiResult = result,
                Content = $"{apiName}|返回结果:{result}"
            };
        }

        /// <summary>
        /// 创建消息队列推送
        /// </summary>
        public static FrameworkMessage CreateMessageQueue(ZCGMessageQueue queue)
        {
            return new FrameworkMessage(FrameworkMessageType.MessageQueue)
            {
                MessageQueue = queue,
                Content = queue?.ToZCGFormat(),
                GroupId = queue?.GroupId,
                SenderId = queue?.SendAccount,
                LoginAccount = queue?.LoginAccount,
                Timestamp = queue?.MessageTime ?? DateTimeOffset.Now.ToUnixTimeMilliseconds()
            };
        }

        /// <summary>
        /// 创建群消息接收通知
        /// </summary>
        public static FrameworkMessage CreateGroupMessageReceived(string loginAccount, string groupId, string senderId, string content, string messageId = null)
        {
            return new FrameworkMessage(FrameworkMessageType.ReceiveGroupMessage)
            {
                LoginAccount = loginAccount,
                GroupId = groupId,
                SenderId = senderId,
                Content = content,
                Id = messageId ?? Guid.NewGuid().ToString("N").Substring(0, 8)
            };
        }

        /// <summary>
        /// 创建私聊消息接收通知
        /// </summary>
        public static FrameworkMessage CreatePrivateMessageReceived(string loginAccount, string senderId, string content, string messageId = null)
        {
            return new FrameworkMessage(FrameworkMessageType.ReceivePrivateMessage)
            {
                LoginAccount = loginAccount,
                SenderId = senderId,
                Content = content,
                Id = messageId ?? Guid.NewGuid().ToString("N").Substring(0, 8)
            };
        }

        /// <summary>
        /// 创建发送群消息请求
        /// </summary>
        public static FrameworkMessage CreateSendGroupMessage(string loginAccount, string groupId, string content, int msgType = 1, int flag = 0)
        {
            return new FrameworkMessage(FrameworkMessageType.SendGroupMessage)
            {
                LoginAccount = loginAccount,
                GroupId = groupId,
                Content = content,
                ApiName = "发送群消息(文本版)",
                ApiParams = new[] { loginAccount, content, groupId, msgType.ToString(), flag.ToString() }
            };
        }

        /// <summary>
        /// 创建发送私聊消息请求
        /// </summary>
        public static FrameworkMessage CreateSendPrivateMessage(string loginAccount, string receiverId, string content)
        {
            return new FrameworkMessage(FrameworkMessageType.SendPrivateMessage)
            {
                LoginAccount = loginAccount,
                ReceiverId = receiverId,
                Content = content,
                ApiName = "发送好友消息",
                ApiParams = new[] { loginAccount, content, receiverId }
            };
        }

        /// <summary>
        /// 创建群禁言/解禁请求
        /// </summary>
        public static FrameworkMessage CreateGroupMute(string loginAccount, string groupId, bool mute)
        {
            return new FrameworkMessage(FrameworkMessageType.GroupOperation)
            {
                LoginAccount = loginAccount,
                GroupId = groupId,
                Content = mute ? "禁言" : "解禁",
                ApiName = "ww_群禁言解禁",
                ApiParams = new[] { loginAccount, groupId, mute ? "1" : "2" }
            };
        }

        #region 配置同步工厂方法

        /// <summary>
        /// 创建全量配置同步消息
        /// </summary>
        public static FrameworkMessage CreateSyncFullConfig(string configJson)
        {
            return new FrameworkMessage(FrameworkMessageType.SyncFullConfig)
            {
                Content = configJson,
                Extra = "full"
            };
        }

        /// <summary>
        /// 创建赔率配置同步消息
        /// </summary>
        public static FrameworkMessage CreateSyncOddsConfig(string oddsJson)
        {
            return new FrameworkMessage(FrameworkMessageType.SyncOddsConfig)
            {
                Content = oddsJson,
                Extra = "odds"
            };
        }

        /// <summary>
        /// 创建封盘配置同步消息
        /// </summary>
        public static FrameworkMessage CreateSyncSealingConfig(string sealingJson)
        {
            return new FrameworkMessage(FrameworkMessageType.SyncSealingConfig)
            {
                Content = sealingJson,
                Extra = "sealing"
            };
        }

        /// <summary>
        /// 创建托管配置同步消息
        /// </summary>
        public static FrameworkMessage CreateSyncTrusteeConfig(string trusteeJson)
        {
            return new FrameworkMessage(FrameworkMessageType.SyncTrusteeConfig)
            {
                Content = trusteeJson,
                Extra = "trustee"
            };
        }

        /// <summary>
        /// 创建自动回复配置同步消息
        /// </summary>
        public static FrameworkMessage CreateSyncAutoReplyConfig(bool enabled, string rulesJson)
        {
            return new FrameworkMessage(FrameworkMessageType.SyncAutoReplyConfig)
            {
                Content = rulesJson,
                Success = enabled,
                Extra = "autoreply"
            };
        }

        /// <summary>
        /// 创建话术模板配置同步消息
        /// </summary>
        public static FrameworkMessage CreateSyncTemplateConfig(string templateJson)
        {
            return new FrameworkMessage(FrameworkMessageType.SyncTemplateConfig)
            {
                Content = templateJson,
                Extra = "template"
            };
        }

        /// <summary>
        /// 创建基本设置同步消息
        /// </summary>
        public static FrameworkMessage CreateSyncBasicConfig(string groupId, string adminId, string myWwid, int debugPort)
        {
            return new FrameworkMessage(FrameworkMessageType.SyncBasicConfig)
            {
                GroupId = groupId,
                Content = $"{groupId}|{adminId}|{myWwid}|{debugPort}",
                Extra = "basic"
            };
        }

        /// <summary>
        /// 创建开奖结果通知消息
        /// </summary>
        public static FrameworkMessage CreateLotteryResult(string period, int num1, int num2, int num3, int sum, int countdown)
        {
            return new FrameworkMessage(FrameworkMessageType.LotteryResult)
            {
                Content = $"{period}|{num1},{num2},{num3}|{sum}|{countdown}",
                Extra = $"{{\"period\":\"{period}\",\"num1\":{num1},\"num2\":{num2},\"num3\":{num3},\"sum\":{sum},\"countdown\":{countdown}}}"
            };
        }

        /// <summary>
        /// 创建封盘通知消息
        /// </summary>
        public static FrameworkMessage CreateSealingNotify(string period, string content)
        {
            return new FrameworkMessage(FrameworkMessageType.SealingNotify)
            {
                Content = content,
                Extra = period
            };
        }

        /// <summary>
        /// 创建封盘提醒消息
        /// </summary>
        public static FrameworkMessage CreateReminderNotify(string period, int secondsToSeal, string content)
        {
            return new FrameworkMessage(FrameworkMessageType.ReminderNotify)
            {
                Content = content,
                Extra = $"{{\"period\":\"{period}\",\"seconds\":{secondsToSeal}}}"
            };
        }

        /// <summary>
        /// 创建期号更新消息
        /// </summary>
        public static FrameworkMessage CreatePeriodUpdate(string currentPeriod, string nextPeriod, int countdown)
        {
            return new FrameworkMessage(FrameworkMessageType.PeriodUpdate)
            {
                Content = $"{currentPeriod}|{nextPeriod}|{countdown}",
                Extra = "period"
            };
        }

        #endregion

        #endregion
    }

    #endregion

    #region 账号信息类

    /// <summary>
    /// 账号登录信息（匹配招财狗协议）
    /// </summary>
    public class AccountLoginInfo
    {
        private static readonly JavaScriptSerializer _serializer = new JavaScriptSerializer();

        /// <summary>昵称</summary>
        public string Nickname { get; set; }

        /// <summary>旺旺ID / wwid</summary>
        public string Wwid { get; set; }

        /// <summary>群号</summary>
        public string GroupId { get; set; }

        /// <summary>账号</summary>
        public string Account { get; set; }

        /// <summary>自动模式</summary>
        public bool AutoMode { get; set; }

        /// <summary>状态</summary>
        public string Status { get; set; }

        /// <summary>头像URL</summary>
        public string Avatar { get; set; }

        /// <summary>登录时间</summary>
        public DateTime LoginTime { get; set; } = DateTime.Now;

        public string ToJson()
        {
            return _serializer.Serialize(this);
        }

        public static AccountLoginInfo FromJson(string json)
        {
            try
            {
                return _serializer.Deserialize<AccountLoginInfo>(json);
            }
            catch
            {
                return null;
            }
        }

        public override string ToString()
        {
            return $"{Nickname ?? "未知"} (wwid={Wwid}, 群号={GroupId})";
        }
    }

    #endregion

    #region 旺商聊数据模型

    /// <summary>
    /// 旺商聊用户信息
    /// </summary>
    public class WangShangLiaoUserInfo
    {
        public string nickname { get; set; }
        public string wwid { get; set; }
        public string account { get; set; }
        public string avatar { get; set; }
        public string nimId { get; set; }  // NIM SDK 的用户ID
        public string error { get; set; }
    }

    /// <summary>
    /// 旺商聊群组信息
    /// </summary>
    public class WangShangLiaoGroupInfo
    {
        public string groupId { get; set; }
        public string groupName { get; set; }
        public string avatar { get; set; }
        public int memberCount { get; set; }
        public string owner { get; set; }
    }

    /// <summary>
    /// 旺商聊群成员信息
    /// </summary>
    public class WangShangLiaoMemberInfo
    {
        public string memberId { get; set; }
        public string nickname { get; set; }
        public string avatar { get; set; }
        public string card { get; set; } // 群名片
        public int role { get; set; } // 0=普通成员, 1=管理员, 2=群主
    }

    #endregion
}
