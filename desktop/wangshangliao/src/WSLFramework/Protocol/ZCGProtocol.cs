using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;

namespace WSLFramework.Protocol
{
    /// <summary>
    /// 招财狗通信协议 - 完全匹配原协议 (基于深度逆向分析)
    /// 根据旺商聊连接底层协议完整文档更新
    /// </summary>
    public static class ZCGProtocol
    {
        #region HPSocket PACK 模式配置
        /// <summary>PACK模式包头标志 "ZK" (0x5A4B)</summary>
        public const ushort PACK_HEADER_FLAG = 0x5A4B;
        /// <summary>旧版包头标志 (兼容)</summary>
        public const ushort PACK_HEADER_FLAG_LEGACY = 0xFF;
        /// <summary>最大包大小 1MB</summary>
        public const uint MAX_PACK_SIZE = 0x100000;
        /// <summary>默认通信端口</summary>
        public const ushort DEFAULT_PORT = 14746;
        /// <summary>HTTPS API端口</summary>
        public const ushort HTTPS_PORT = 443;
        /// <summary>HTTP备用端口</summary>
        public const ushort HTTP_PORT = 8080;
        #endregion
        
        #region 心跳协议配置
        /// <summary>心跳周期(秒)</summary>
        public const int HEARTBEAT_INTERVAL = 60;
        /// <summary>心跳端点</summary>
        public const string HEARTBEAT_ENDPOINT = "/ping";
        /// <summary>心跳成功码</summary>
        public const int HEARTBEAT_SUCCESS = 0;
        /// <summary>心跳未登录码</summary>
        public const int HEARTBEAT_NOT_LOGGED_IN = 403;
        /// <summary>心跳未登录错误号</summary>
        public const int HEARTBEAT_ERRNO_NOT_LOGGED_IN = 50;
        /// <summary>心跳会话过期错误号</summary>
        public const int HEARTBEAT_ERRNO_SESSION_EXPIRED = 51;
        /// <summary>心跳被踢错误号</summary>
        public const int HEARTBEAT_ERRNO_KICKED = 52;
        #endregion
        
        #region NIM SDK 配置
        /// <summary>数据库加密密钥</summary>
        public const string DB_ENCRYPT_KEY = "YXSDK";
        /// <summary>通信加密算法 (1=AES-128-CBC)</summary>
        public const int COMM_ENCRYPT_ALGO = 1;
        /// <summary>密钥协商算法 (1=ECDH)</summary>
        public const int KEY_NEGO_ALGO = 1;
        /// <summary>握手类型 (1=标准握手)</summary>
        public const int HANDSHAKE_TYPE = 1;
        /// <summary>SDK日志级别 (5=详细)</summary>
        public const int SDK_LOG_LEVEL = 5;
        #endregion
        
        #region NIM 登录状态
        /// <summary>未登录</summary>
        public const int LOGIN_STATE_NONE = 0;
        /// <summary>正在登录</summary>
        public const int LOGIN_STATE_LOGGING_IN = 1;
        /// <summary>登录成功</summary>
        public const int LOGIN_STATE_LOGGED_IN = 2;
        /// <summary>登录失败</summary>
        public const int LOGIN_STATE_FAILED = 3;
        /// <summary>正在登出</summary>
        public const int LOGIN_STATE_LOGGING_OUT = 4;
        #endregion
        
        #region HTTP 响应码
        /// <summary>成功</summary>
        public const int HTTP_CODE_SUCCESS = 0;
        /// <summary>未授权</summary>
        public const int HTTP_CODE_UNAUTHORIZED = 401;
        /// <summary>禁止访问</summary>
        public const int HTTP_CODE_FORBIDDEN = 403;
        /// <summary>参数错误</summary>
        public const int HTTP_CODE_PARAM_ERROR = 1001;
        /// <summary>Token无效</summary>
        public const int HTTP_CODE_TOKEN_INVALID = -236;
        /// <summary>授权过期</summary>
        public const int HTTP_CODE_AUTH_EXPIRED = -10243;
        #endregion
        
        #region 客户端类型 (from_client_type)
        /// <summary>PC客户端</summary>
        public const int CLIENT_TYPE_PC = 1;
        /// <summary>iOS</summary>
        public const int CLIENT_TYPE_IOS = 2;
        /// <summary>Android</summary>
        public const int CLIENT_TYPE_ANDROID = 4;
        /// <summary>Web</summary>
        public const int CLIENT_TYPE_WEB = 8;
        /// <summary>Android (新)</summary>
        public const int CLIENT_TYPE_ANDROID_NEW = 16;
        /// <summary>服务端</summary>
        public const int CLIENT_TYPE_SERVER = 32;
        /// <summary>Mac</summary>
        public const int CLIENT_TYPE_MAC = 64;
        #endregion

        #region 消息分隔符和格式
        /// <summary>字段分隔符 (API调用)</summary>
        public const char API_SEPARATOR = '|';
        /// <summary>消息队列字段分隔符</summary>
        public const char QUEUE_SEPARATOR = '★';
        /// <summary>返回结果前缀</summary>
        public const string RESULT_PREFIX = "返回结果:";
        /// <summary>消息队列前缀</summary>
        public const string MSG_QUEUE_PREFIX = "消息队列:";
        /// <summary>行分隔符</summary>
        public static readonly string[] LINE_SEPARATORS = { "\r\n", "\n" };
        #endregion

        #region NIM消息类型 (逆向分析所得)
        /// <summary>
        /// NIM消息类型代码
        /// </summary>
        public enum NimMsgType
        {
            /// <summary>私聊消息 (P2P)</summary>
            P2PMessage = 1001,
            /// <summary>群消息 (Team)</summary>
            TeamMessage = 1002,
            /// <summary>关系变动 (好友申请/通过)</summary>
            RelationshipChange = 1003,
            /// <summary>系统通知</summary>
            SystemNotification = 1015
        }
        
        /// <summary>
        /// 业务消息类型
        /// </summary>
        public enum MsgType
        {
            // 系统消息 0-9
            SystemLog = 0,
            ClientConnect = 1,
            ClientDisconnect = 2,
            Heartbeat = 3,
            ServerInit = 4,
            ServerShutdown = 5,
            
            // 群消息 10-19
            GroupMessage = 10,
            GroupSend = 11,
            GroupAtMessage = 12,
            GroupCardChange = 13,
            GroupMemberJoin = 14,
            GroupMemberLeave = 15,
            GroupNotice = 16,
            GroupMute = 17,
            GroupUnmute = 18,
            
            // 私聊消息 20-29
            PrivateMessage = 20,
            PrivateSend = 21,
            FriendMessage = 22,
            FriendSend = 23,
            FriendRequest = 24,
            FriendAccept = 25,

            // 业务消息 30-39
            BetMessage = 30,
            ScoreUp = 31,
            ScoreDown = 32,
            QueryScore = 33,
            QueryHistory = 34,

            // 开奖相关 40-49
            LotteryOpen = 40,
            LotterySeal = 41,
            LotteryResult = 42,

            // 托管相关 50-59
            TrusteeStart = 50,
            TrusteeStop = 51,

            // 响应消息 100+
            Response = 100,
            Error = 999
        }
        #endregion

        #region API结果加密解密
        /// <summary>
        /// 加密API返回结果（Base64编码）
        /// </summary>
        public static string EncryptApiResult(string result)
        {
            if (string.IsNullOrEmpty(result))
                return "";
            try
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(result);
                return Convert.ToBase64String(bytes);
            }
            catch
            {
                return result;
            }
        }

        /// <summary>
        /// 解密API返回结果（Base64解码）
        /// </summary>
        public static string DecryptApiResult(string base64)
        {
            if (string.IsNullOrEmpty(base64))
                return "";
            try
            {
                var bytes = Convert.FromBase64String(base64);
                return System.Text.Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                return base64;
            }
        }
        #endregion
    }

    /// <summary>
    /// API调用格式定义 (逆向分析所得)
    /// 格式: API名称|参数1|参数2|...|返回结果:Base64编码结果
    /// </summary>
    public static class ZCGApiFormat
    {
        // API名称常量
        public const string API_GET_GROUPS = "取群群";
        public const string API_SEND_GROUP_MSG = "发送群消息(文本版)";
        public const string API_GET_ALL_ACCOUNTS = "插件_获取所有账号";
        public const string API_GROUP_MUTE = "ww_群禁言解禁";
        public const string API_GET_GROUP_MEMBERS = "ww_获取群成员";
        public const string API_MODIFY_GROUP_CARD = "ww_修改群名片";
        public const string API_GET_USER_INFO = "ww_ID资料";
        public const string API_ADD_FRIEND = "ww_添加好友并备注_单向";
        public const string API_SEND_FRIEND_MSG = "发送好友消息";
        public const string API_FRAMEWORK_AUTH = "ww_xp框架接口";

        /// <summary>
        /// 构建API调用字符串
        /// </summary>
        public static string BuildApiCall(string apiName, params string[] args)
        {
            var parts = new List<string> { apiName };
            parts.AddRange(args);
            return string.Join("|", parts);
        }

        /// <summary>
        /// 解析API调用字符串
        /// </summary>
        public static (string ApiName, string[] Args, string Result) ParseApiCall(string apiCall)
        {
            var parts = apiCall.Split('|');
            if (parts.Length == 0) return (string.Empty, Array.Empty<string>(), string.Empty);

            var apiName = parts[0];
            var args = new List<string>();
            string result = null;

            for (int i = 1; i < parts.Length; i++)
            {
                if (parts[i].StartsWith(ZCGProtocol.RESULT_PREFIX))
                {
                    result = parts[i].Substring(ZCGProtocol.RESULT_PREFIX.Length);
                }
                else
                {
                    args.Add(parts[i]);
                }
            }

            return (apiName, args.ToArray(), result);
        }
        
        /// <summary>
        /// 追加返回结果
        /// </summary>
        public static string AppendResult(string apiCall, string base64Result)
        {
            return $"{apiCall}|{ZCGProtocol.RESULT_PREFIX}{base64Result}";
        }
    }
    
    /// <summary>
    /// ZCG 消息结构 (收发日志格式)
    /// </summary>
    public class ZCGMessage
    {
        /// <summary>机器人QQ/账号 (RQQ)</summary>
        public string RQQ { get; set; } = "";
        /// <summary>群号</summary>
        public string GroupId { get; set; } = "";
        /// <summary>发送者QQ (fromQQ)</summary>
        public string FromQQ { get; set; } = "";
        /// <summary>被操作者QQ (beingQQ)</summary>
        public string BeingQQ { get; set; } = "";
        /// <summary>消息类型描述/备注</summary>
        public string TypeDesc { get; set; } = "";
        /// <summary>消息内容/数据</summary>
        public string Content { get; set; } = "";
        /// <summary>时间戳</summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;
        /// <summary>消息ID</summary>
        public string MsgId { get; set; } = "";
        /// <summary>NIM消息类型代码</summary>
        public int TypeCode { get; set; }
        /// <summary>昵称</summary>
        public string Nickname { get; set; } = "";
        /// <summary>短ID (后4位)</summary>
        public string ShortId { get; set; } = "";
        /// <summary>原始JSON消息</summary>
        public string RawJson { get; set; } = "";
        
        /// <summary>
        /// 序列化为日志格式 (zcg收发日志)
        /// </summary>
        public string ToLogFormat()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{Timestamp:yyyy/M/d H:m:s}    ");
            sb.AppendLine($"RQQ:{RQQ}");
            sb.AppendLine($"群:{GroupId}");
            sb.AppendLine($"fromQQ:{FromQQ}");
            sb.AppendLine($"beingQQ:{BeingQQ}");
            sb.AppendLine(TypeDesc);
            sb.AppendLine("数据：");
            sb.AppendLine(Content);
            return sb.ToString();
        }
        
        /// <summary>
        /// 从日志格式解析
        /// </summary>
        public static ZCGMessage FromLogFormat(string log)
        {
            var msg = new ZCGMessage();
            var lines = log.Split(ZCGProtocol.LINE_SEPARATORS, StringSplitOptions.None);
            bool isDataSection = false;
            var dataLines = new List<string>();
            
            foreach (var line in lines)
            {
                if (line.StartsWith("RQQ:"))
                    msg.RQQ = line.Substring(4);
                else if (line.StartsWith("群:"))
                    msg.GroupId = line.Substring(2);
                else if (line.StartsWith("fromQQ:"))
                    msg.FromQQ = line.Substring(7);
                else if (line.StartsWith("beingQQ:"))
                    msg.BeingQQ = line.Substring(8);
                else if (line.StartsWith("备注:") || line.Contains("消息") || line.Contains("日志"))
                {
                    msg.TypeDesc = line;
                    isDataSection = false;
                }
                else if (line.StartsWith("数据：") || line.StartsWith("数据:"))
                {
                    isDataSection = true;
                }
                else if (isDataSection && !string.IsNullOrEmpty(line))
                {
                    dataLines.Add(line);
                }
                else if (line.Length > 10 && Regex.IsMatch(line, @"^\d{4}/\d{1,2}/\d{1,2}"))
                {
                    // 解析时间戳
                    if (DateTime.TryParse(line.Trim(), out var dt))
                        msg.Timestamp = dt;
                }
            }

            msg.Content = string.Join("\r\n", dataLines);
            return msg;
        }
        
        /// <summary>
        /// 提取短ID (后4位)
        /// </summary>
        public static string GetShortId(string fullId)
        {
            if (string.IsNullOrEmpty(fullId) || fullId.Length < 4)
                return fullId ?? "";
            return fullId.Substring(fullId.Length - 4);
        }
    }
    
    /// <summary>
    /// 消息队列格式 (插件通信格式)
    /// 格式: 消息队列:登录账号=xxx★操作账号=xxx★发送账号=xxx★群号=xxx★内容=xxx★消息ID=xxx★消息类型=xxx★消息时间=xxx★消息解析后=xxx★原始消息=JSON
    /// </summary>
    public class ZCGQueueMessage
    {
        public string LoginAccount { get; set; } = "";
        public string OperateAccount { get; set; } = "";
        public string SendAccount { get; set; } = "";
        public string GroupId { get; set; } = "";
        public string Content { get; set; } = "";
        public string MsgId { get; set; } = "";
        public int MsgType { get; set; }
        public long MsgTime { get; set; }
        public string ParsedMsg { get; set; } = "";
        public string RawMsg { get; set; } = "";

        /// <summary>
        /// 序列化为队列消息格式
        /// </summary>
        public string ToQueueFormat()
        {
            var parts = new[]
            {
                $"登录账号={LoginAccount}",
                $"操作账号={OperateAccount}",
                $"发送账号={SendAccount}",
                $"群号={GroupId}",
                $"内容={Content}",
                $"消息ID={MsgId}",
                $"消息类型={MsgType}",
                $"消息时间={MsgTime}",
                $"消息解析后={ParsedMsg}",
                $"原始消息={RawMsg}"
            };
            return ZCGProtocol.MSG_QUEUE_PREFIX + string.Join(ZCGProtocol.QUEUE_SEPARATOR.ToString(), parts);
        }

        /// <summary>
        /// 从队列消息格式解析
        /// </summary>
        public static ZCGQueueMessage FromQueueFormat(string queue)
        {
            var msg = new ZCGQueueMessage();
            if (!queue.StartsWith(ZCGProtocol.MSG_QUEUE_PREFIX))
                return msg;

            var content = queue.Substring(ZCGProtocol.MSG_QUEUE_PREFIX.Length);
            var parts = content.Split(ZCGProtocol.QUEUE_SEPARATOR);

            foreach (var part in parts)
            {
                var idx = part.IndexOf('=');
                if (idx < 0) continue;

                var key = part.Substring(0, idx);
                var value = part.Substring(idx + 1);

                switch (key)
                {
                    case "登录账号": msg.LoginAccount = value; break;
                    case "操作账号": msg.OperateAccount = value; break;
                    case "发送账号": msg.SendAccount = value; break;
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
                    case "消息解析后": msg.ParsedMsg = value; break;
                    case "原始消息": msg.RawMsg = value; break;
                }
            }

            return msg;
        }
    }

    /// <summary>
    /// 配置文件加密解密 (逆向分析所得)
    /// </summary>
    public static class ZCGCrypto
    {
        private const byte XOR_KEY = 0x10;
        private const string VALUE_SUFFIX = "20CB5D79B";
        private const string BOOL_FALSE = "ACC920CB5D79B";
        private const string BOOL_TRUE = "C5F620CB5D79B";
        private const int CHAR_OFFSET = 0x20;
        
        /// <summary>
        /// 解密配置值 (文本)
        /// </summary>
        public static string Decrypt(string encrypted)
        {
            if (string.IsNullOrEmpty(encrypted)) return "";
                
            try
            {
                // 移除后缀
                var data = encrypted;
                if (data.EndsWith(VALUE_SUFFIX))
                    data = data.Substring(0, data.Length - VALUE_SUFFIX.Length);
                else if (data.EndsWith("CB5D79B"))
                    data = data.Substring(0, data.Length - 7);
                    
                // 十六进制解码 + XOR
                var bytes = new byte[data.Length / 2];
                for (int i = 0; i < bytes.Length; i++)
                {
                    bytes[i] = (byte)(Convert.ToByte(data.Substring(i * 2, 2), 16) ^ XOR_KEY);
                }
                
                return Encoding.GetEncoding("GB2312").GetString(bytes);
            }
            catch
            {
                return encrypted;
            }
        }
        
        /// <summary>
        /// 加密配置值 (文本)
        /// </summary>
        public static string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return VALUE_SUFFIX;
                
            try
            {
                var bytes = Encoding.GetEncoding("GB2312").GetBytes(plainText);
                var sb = new StringBuilder();
                
                foreach (var b in bytes)
                {
                    sb.Append((b ^ XOR_KEY).ToString("X2"));
                }
                
                sb.Append(VALUE_SUFFIX);
                return sb.ToString();
            }
            catch
            {
                return plainText;
            }
        }

        /// <summary>
        /// 解密数值 (每位字符-0x20)
        /// </summary>
        public static string DecryptNumber(string encrypted)
        {
            if (string.IsNullOrEmpty(encrypted)) return "0";

            try
            {
                var data = encrypted;
                if (data.EndsWith(VALUE_SUFFIX))
                    data = data.Substring(0, data.Length - VALUE_SUFFIX.Length);

                var sb = new StringBuilder();
                for (int i = 0; i < data.Length; i += 2)
                {
                    var b = Convert.ToByte(data.Substring(i, 2), 16);
                    sb.Append((char)(b - CHAR_OFFSET));
                }
                return sb.ToString();
            }
            catch
            {
                return "0";
            }
        }

        /// <summary>
        /// 加密数值 (每位字符+0x20)
        /// </summary>
        public static string EncryptNumber(string number)
        {
            if (string.IsNullOrEmpty(number)) return "50" + VALUE_SUFFIX; // '0'

            var sb = new StringBuilder();
            foreach (var c in number)
            {
                sb.Append(((byte)c + CHAR_OFFSET).ToString("X2"));
            }
            sb.Append(VALUE_SUFFIX);
            return sb.ToString();
        }
        
        /// <summary>
        /// 解密布尔值
        /// </summary>
        public static bool DecryptBool(string value)
        {
            return value == BOOL_TRUE || value?.Contains("C5F6") == true;
        }
        
        /// <summary>
        /// 加密布尔值
        /// </summary>
        public static string EncryptBool(bool value)
        {
            return value ? BOOL_TRUE : BOOL_FALSE;
        }

        /// <summary>
        /// URL-safe Base64 解码
        /// </summary>
        public static byte[] DecodeUrlSafeBase64(string input)
        {
            var standard = input.Replace('-', '+').Replace('_', '/');
            switch (standard.Length % 4)
            {
                case 2: standard += "=="; break;
                case 3: standard += "="; break;
            }
            return Convert.FromBase64String(standard);
        }

        /// <summary>
        /// URL-safe Base64 编码
        /// </summary>
        public static string EncodeUrlSafeBase64(byte[] data)
        {
            return Convert.ToBase64String(data)
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');
        }
    }

    /// <summary>
    /// msg_attach.b Protobuf 消息解析器 (逆向分析所得)
    /// </summary>
    public static class MsgAttachParser
    {
        /// <summary>
        /// 解析 msg_attach JSON 中的 b 字段
        /// </summary>
        public static MsgAttachData Parse(string base64B)
        {
            try
            {
                var data = ZCGCrypto.DecodeUrlSafeBase64(base64B);
                return ParseProtobuf(data);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 解析 Protobuf 数据
        /// </summary>
        private static MsgAttachData ParseProtobuf(byte[] data)
        {
            if (data == null || data.Length < 9) return null;

            var result = new MsgAttachData
            {
                RawData = data,
                TotalLength = data.Length
            };

            // 检查是否是 Protobuf 格式 (field 1, wire type 1)
            if (data[0] == 0x09)
            {
                // Field 1: fixed64 - 包含魔数和版本信息
                result.Field1 = BitConverter.ToUInt64(data, 1);

                // Field 2: fixed64 - Unix时间戳(毫秒)
                if (data.Length > 17 && data[9] == 0x11)
                {
                    result.Timestamp = BitConverter.ToUInt64(data, 10);
                    result.MessageTime = DateTimeOffset.FromUnixTimeMilliseconds((long)(result.Timestamp / 1000000)).DateTime;
                }

                // Field 3: fixed64
                if (data.Length > 26 && data[18] == 0x19)
                {
                    result.Field3 = BitConverter.ToUInt64(data, 19);
                }

                // Field 4: bytes - 主要消息内容
                if (data.Length > 28 && data[27] == 0x22)
                {
                    int offset = 28;
                    int length = ReadVarint(data, ref offset);
                    if (offset + length <= data.Length)
                    {
                        result.ContentData = new byte[length];
                        Array.Copy(data, offset, result.ContentData, 0, length);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 读取 Varint
        /// </summary>
        private static int ReadVarint(byte[] data, ref int offset)
        {
            int result = 0;
            int shift = 0;
            while (offset < data.Length)
            {
                byte b = data[offset++];
                result |= (b & 0x7F) << shift;
                if ((b & 0x80) == 0) break;
                shift += 7;
        }
            return result;
        }
    }

    /// <summary>
    /// msg_attach.b 解析结果
    /// </summary>
    public class MsgAttachData
    {
        public byte[] RawData { get; set; }
        public int TotalLength { get; set; }
        public ulong Field1 { get; set; }
        public ulong Timestamp { get; set; }
        public DateTime MessageTime { get; set; }
        public ulong Field3 { get; set; }
        public byte[] ContentData { get; set; }
    }
    
    #region 框架间通信JSON格式 (根据旺商聊连接底层协议完整文档)
    
    /// <summary>
    /// 框架请求格式 (主框架 → xplugin)
    /// </summary>
    public class FrameworkRequest
    {
        /// <summary>命令名称</summary>
        public string cmd { get; set; }
        /// <summary>参数字典</summary>
        public Dictionary<string, object> @params { get; set; }
        
        public FrameworkRequest()
        {
            @params = new Dictionary<string, object>();
        }
        
        public FrameworkRequest(string command, Dictionary<string, object> parameters = null)
        {
            cmd = command;
            @params = parameters ?? new Dictionary<string, object>();
        }
        
        /// <summary>添加参数</summary>
        public FrameworkRequest AddParam(string key, object value)
        {
            @params[key] = value;
            return this;
        }
    }
    
    /// <summary>
    /// 框架响应格式 (xplugin → 主框架)
    /// </summary>
    public class FrameworkResponse
    {
        /// <summary>状态码 (0=成功)</summary>
        public int code { get; set; }
        /// <summary>消息</summary>
        public string msg { get; set; }
        /// <summary>数据</summary>
        public object data { get; set; }
        
        public bool IsSuccess => code == 0;
        
        public static FrameworkResponse Success(object data = null, string message = "OK")
        {
            return new FrameworkResponse { code = 0, msg = message, data = data };
        }
        
        public static FrameworkResponse Error(int code, string message)
        {
            return new FrameworkResponse { code = code, msg = message };
        }
    }
    
    /// <summary>
    /// 心跳响应格式
    /// </summary>
    public class HeartbeatResponse
    {
        /// <summary>请求ID (固定0)</summary>
        public int id { get; set; }
        /// <summary>状态码 (0=成功, 403=未登录)</summary>
        public int code { get; set; }
        /// <summary>用户UID (NIM内部ID)</summary>
        public long uid { get; set; }
        /// <summary>错误码</summary>
        public int errno { get; set; }
        /// <summary>错误消息</summary>
        public string msg { get; set; }
        
        public bool IsSuccess => code == 0;
        public bool IsNotLoggedIn => code == 403 || errno == 50;
    }
    
    /// <summary>
    /// NIM SDK 初始化配置
    /// </summary>
    public class NIMSDKConfig
    {
        /// <summary>应用密钥</summary>
        public string app_key { get; set; }
        /// <summary>全局配置</summary>
        public NIMGlobalConfig global_config { get; set; }
        
        public NIMSDKConfig()
        {
            global_config = new NIMGlobalConfig();
        }
        
        /// <summary>创建默认配置</summary>
        public static NIMSDKConfig CreateDefault(string appKey)
        {
            return new NIMSDKConfig
            {
                app_key = appKey,
                global_config = NIMGlobalConfig.CreateDefault()
            };
        }
    }
    
    /// <summary>
    /// NIM SDK 全局配置
    /// </summary>
    public class NIMGlobalConfig
    {
        /// <summary>通信加密算法 (1=AES-128-CBC)</summary>
        public int comm_enca { get; set; } = ZCGProtocol.COMM_ENCRYPT_ALGO;
        /// <summary>数据库加密密钥</summary>
        public string db_encrypt_key { get; set; } = ZCGProtocol.DB_ENCRYPT_KEY;
        /// <summary>握手类型 (1=标准握手)</summary>
        public int hand_shake_type { get; set; } = ZCGProtocol.HANDSHAKE_TYPE;
        /// <summary>密钥协商算法 (1=ECDH)</summary>
        public int nego_key_neca { get; set; } = ZCGProtocol.KEY_NEGO_ALGO;
        /// <summary>预加载附件</summary>
        public bool preload_attach { get; set; } = true;
        /// <summary>预加载图片名称模板</summary>
        public string preload_image_name_template { get; set; } = "thumb_";
        /// <summary>预加载图片质量</summary>
        public int preload_image_quality { get; set; } = -1;
        /// <summary>预加载图片尺寸</summary>
        public string preload_image_resize { get; set; } = "";
        /// <summary>SDK日志级别 (5=详细)</summary>
        public int sdk_log_level { get; set; } = ZCGProtocol.SDK_LOG_LEVEL;
        /// <summary>重新登录前是否需要更新LBS</summary>
        public bool need_update_lbs_befor_relogin { get; set; } = false;
        /// <summary>使用HTTPS</summary>
        public bool use_https { get; set; } = true;
        
        public static NIMGlobalConfig CreateDefault()
        {
            return new NIMGlobalConfig();
        }
    }
    
    /// <summary>
    /// msg_attach.b 二进制结构头部 (根据文档第17节)
    /// </summary>
    public class MsgAttachBinaryHeader
    {
        /// <summary>Magic标识 (0x09)</summary>
        public byte Magic { get; set; }
        /// <summary>消息类型标识 (0x85E2=P2P, 0x1949=群消息, 0x1C49=群通知)</summary>
        public ushort Type { get; set; }
        /// <summary>子类型</summary>
        public ushort SubType { get; set; }
        /// <summary>会话ID低位</summary>
        public ushort SessionLo { get; set; }
        /// <summary>会话ID高位</summary>
        public ushort SessionHi { get; set; }
        /// <summary>标志位</summary>
        public byte Flags { get; set; }
        /// <summary>序列号低位</summary>
        public ushort SeqLo { get; set; }
        /// <summary>标签 (4字节, "ci\x00\x00")</summary>
        public byte[] Tag { get; set; }
        /// <summary>加密指示器</summary>
        public byte CipherIndicator { get; set; }
        
        /// <summary>是否为P2P消息</summary>
        public bool IsP2P => Type == 0x85E2;
        /// <summary>是否为群消息</summary>
        public bool IsTeamMessage => Type == 0x1949;
        /// <summary>是否为群通知</summary>
        public bool IsTeamNotification => Type == 0x1C49;
        
        /// <summary>
        /// 从字节数组解析
        /// </summary>
        public static MsgAttachBinaryHeader Parse(byte[] data)
        {
            if (data == null || data.Length < 17) return null;
            
            return new MsgAttachBinaryHeader
            {
                Magic = data[0],
                Type = BitConverter.ToUInt16(data, 1),
                SubType = BitConverter.ToUInt16(data, 3),
                SessionLo = BitConverter.ToUInt16(data, 5),
                SessionHi = BitConverter.ToUInt16(data, 7),
                Flags = data[9],
                SeqLo = BitConverter.ToUInt16(data, 10),
                Tag = new byte[] { data[12], data[13], data[14], data[15] },
                CipherIndicator = data[16]
            };
        }
    }
    
    /// <summary>
    /// 群通知类型
    /// </summary>
    public static class TeamNotifyTypes
    {
        /// <summary>开启全员禁言</summary>
        public const string GROUP_MUTE_ON = "NOTIFY_TYPE_GROUP_MUTE_1";
        /// <summary>关闭全员禁言</summary>
        public const string GROUP_MUTE_OFF = "NOTIFY_TYPE_GROUP_MUTE_0";
        /// <summary>用户修改昵称</summary>
        public const string USER_UPDATE_NAME = "NOTIFY_TYPE_USER_UPDATE_NAME";
        /// <summary>用户加入</summary>
        public const string USER_JOIN = "NOTIFY_TYPE_USER_JOIN";
        /// <summary>用户离开</summary>
        public const string USER_LEAVE = "NOTIFY_TYPE_USER_LEAVE";
    }
    
    #endregion
}
