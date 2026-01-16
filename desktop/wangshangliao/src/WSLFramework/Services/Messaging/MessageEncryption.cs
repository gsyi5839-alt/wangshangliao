using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using WSLFramework.Utils;

namespace WSLFramework.Services
{
    /// <summary>
    /// 消息加密/解密服务 - 根据最底层消息协议文档实现
    /// 处理 msg_attach.b 二进制结构和 @消息格式
    /// </summary>
    public class MessageEncryption
    {
        #region 单例模式

        private static readonly Lazy<MessageEncryption> _instance =
            new Lazy<MessageEncryption>(() => new MessageEncryption());

        public static MessageEncryption Instance => _instance.Value;

        #endregion

        #region 常量 - msg_attach.b 二进制结构

        /// <summary>Magic1 标识 (版本1)</summary>
        public static readonly byte[] MAGIC_V1 = { 0x09, 0x1A };

        /// <summary>Magic1 标识 (版本2)</summary>
        public static readonly byte[] MAGIC_V2 = { 0x09, 0x19 };

        /// <summary>Version 标识 (固定)</summary>
        public static readonly byte[] VERSION = { 0x49, 0x1F };

        /// <summary>Cipher 标识 "ci"</summary>
        public static readonly byte[] CIPHER_INDICATOR = { 0x63, 0x69 }; // "ci"

        /// <summary>消息头部总长度 (16字节)</summary>
        public const int MESSAGE_HEADER_LENGTH = 16;

        /// <summary>数据库加密密钥</summary>
        public const string DB_ENCRYPT_KEY = "YXSDK";

        #endregion

        #region 常量 - @消息格式

        /// <summary>@消息格式: [LQ:@旺商聊号]</summary>
        public const string AT_FORMAT = "[LQ:@{0}]";

        /// <summary>@消息正则表达式</summary>
        private static readonly Regex AT_REGEX = new Regex(@"\[LQ:@(\d+)\]", RegexOptions.Compiled);

        /// <summary>@全体成员</summary>
        public const string AT_ALL = "[LQ:@all]";

        #endregion

        #region 私有字段

        private readonly JavaScriptSerializer _serializer;

        #endregion

        #region 事件

        public event Action<string> OnLog;

        #endregion

        #region 构造函数

        private MessageEncryption()
        {
            _serializer = new JavaScriptSerializer();
        }

        #endregion

        #region Base64URL 编解码

        /// <summary>
        /// Base64URL 解码
        /// </summary>
        public byte[] Base64UrlDecode(string base64Url)
        {
            if (string.IsNullOrEmpty(base64Url))
                return new byte[0];

            try
            {
                // Base64URL → 标准 Base64
                var base64 = base64Url
                    .Replace('-', '+')
                    .Replace('_', '/');

                // 补齐 padding
                var padding = 4 - (base64.Length % 4);
                if (padding < 4)
                {
                    base64 += new string('=', padding);
                }

                return Convert.FromBase64String(base64);
            }
            catch (Exception ex)
            {
                Log($"Base64URL 解码失败: {ex.Message}");
                return new byte[0];
            }
        }

        /// <summary>
        /// Base64URL 编码
        /// </summary>
        public string Base64UrlEncode(byte[] data)
        {
            if (data == null || data.Length == 0)
                return "";

            // 标准 Base64 → Base64URL
            return Convert.ToBase64String(data)
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');
        }

        #endregion

        #region 消息头解析

        /// <summary>
        /// 解析 msg_attach.b 的二进制头部
        /// </summary>
        public MsgAttachHeader ParseMsgAttachHeader(byte[] data)
        {
            if (data == null || data.Length < MESSAGE_HEADER_LENGTH)
            {
                return null;
            }

            try
            {
                var header = new MsgAttachHeader();

                // Magic (0-1)
                header.Magic = new byte[] { data[0], data[1] };
                header.IsValidMagic = (data[0] == MAGIC_V1[0] && data[1] == MAGIC_V1[1]) ||
                                      (data[0] == MAGIC_V2[0] && data[1] == MAGIC_V2[1]);

                // Version (2-3)
                header.Version = new byte[] { data[2], data[3] };
                header.IsValidVersion = data[2] == VERSION[0] && data[3] == VERSION[1];

                // MsgMeta (4-7)
                header.MsgMeta = BitConverter.ToUInt32(data, 4);

                // Cipher (8-9)
                header.Cipher = new byte[] { data[8], data[9] };
                header.IsEncrypted = data[8] == CIPHER_INDICATOR[0] && data[9] == CIPHER_INDICATOR[1];

                // Padding (10-15)
                header.Padding = new byte[6];
                Array.Copy(data, 10, header.Padding, 0, 6);

                // 加密数据 (16+)
                if (data.Length > MESSAGE_HEADER_LENGTH)
                {
                    header.EncryptedData = new byte[data.Length - MESSAGE_HEADER_LENGTH];
                    Array.Copy(data, MESSAGE_HEADER_LENGTH, header.EncryptedData, 0, header.EncryptedData.Length);
                }

                return header;
            }
            catch (Exception ex)
            {
                Log($"解析消息头失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 从 msg_attach JSON 中提取并解析二进制数据
        /// </summary>
        public MsgAttachHeader ParseMsgAttachJson(string msgAttachJson)
        {
            if (string.IsNullOrEmpty(msgAttachJson))
                return null;

            try
            {
                var attach = _serializer.Deserialize<Dictionary<string, object>>(msgAttachJson);
                if (attach != null && attach.ContainsKey("b"))
                {
                    var base64Url = attach["b"]?.ToString();
                    if (!string.IsNullOrEmpty(base64Url))
                    {
                        var data = Base64UrlDecode(base64Url);
                        return ParseMsgAttachHeader(data);
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"解析 msg_attach JSON 失败: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// 尝试从加密数据中提取可读文本
        /// </summary>
        public string TryExtractText(MsgAttachHeader header)
        {
            if (header?.EncryptedData == null || header.EncryptedData.Length == 0)
                return "";

            try
            {
                // 尝试直接解码为 UTF-8 (如果未加密)
                var text = Encoding.UTF8.GetString(header.EncryptedData);
                
                // 过滤不可打印字符
                text = Regex.Replace(text, @"[^\x20-\x7e\u4e00-\u9fff\n\r]", "");
                return text.Trim();
            }
            catch
            {
                return "";
            }
        }

        #endregion

        #region @消息处理

        /// <summary>
        /// 创建 @消息内容 (基础版)
        /// </summary>
        /// <param name="targetId">被@的旺商聊号</param>
        /// <param name="message">消息内容</param>
        public string CreateAtMessage(string targetId, string message)
        {
            if (string.IsNullOrEmpty(targetId))
                return message;

            return string.Format(AT_FORMAT, targetId) + " " + message;
        }
        
        /// <summary>
        /// 创建 @消息内容 (带简称) - ZCG标准格式
        /// 格式: [LQ:@旺商聊号] (简称)\n消息内容
        /// </summary>
        /// <param name="targetId">被@的旺商聊号</param>
        /// <param name="message">消息内容</param>
        /// <param name="showShortId">是否显示简称</param>
        public string CreateAtMessageWithShortId(string targetId, string message, bool showShortId = true)
        {
            if (string.IsNullOrEmpty(targetId))
                return message;

            var shortId = targetId.Length > 4 ? targetId.Substring(0, 4) : targetId;
            
            if (showShortId)
            {
                return $"[LQ:@{targetId}] ({shortId})\n{message}";
            }
            else
            {
                return $"[LQ:@{targetId}] {message}";
            }
        }
        
        /// <summary>
        /// 创建多人 @消息
        /// </summary>
        /// <param name="targetIds">被@的旺商聊号列表</param>
        /// <param name="message">消息内容</param>
        public string CreateMultiAtMessage(IEnumerable<string> targetIds, string message)
        {
            if (targetIds == null)
                return message;
                
            var atParts = targetIds.Select(id => $"[LQ:@{id}]");
            return string.Join(" ", atParts) + "\n" + message;
        }

        /// <summary>
        /// 创建 @全体成员消息
        /// </summary>
        public string CreateAtAllMessage(string message)
        {
            return AT_ALL + " " + message;
        }

        /// <summary>
        /// 解析消息中的 @目标
        /// </summary>
        /// <param name="content">消息内容</param>
        /// <returns>被@的旺商聊号列表</returns>
        public List<string> ParseAtTargets(string content)
        {
            var targets = new List<string>();

            if (string.IsNullOrEmpty(content))
                return targets;

            var matches = AT_REGEX.Matches(content);
            foreach (Match match in matches)
            {
                if (match.Groups.Count > 1)
                {
                    targets.Add(match.Groups[1].Value);
                }
            }

            return targets;
        }

        /// <summary>
        /// 检查消息是否包含 @
        /// </summary>
        public bool HasAtMention(string content)
        {
            return !string.IsNullOrEmpty(content) && AT_REGEX.IsMatch(content);
        }

        /// <summary>
        /// 移除消息中的 @ 标记
        /// </summary>
        public string RemoveAtMarks(string content)
        {
            if (string.IsNullOrEmpty(content))
                return content;

            return AT_REGEX.Replace(content, "").Replace(AT_ALL, "").Trim();
        }

        /// <summary>
        /// 替换 @ 标记为昵称
        /// </summary>
        /// <param name="content">消息内容</param>
        /// <param name="idToNickname">ID到昵称的映射</param>
        public string ReplaceAtWithNickname(string content, Dictionary<string, string> idToNickname)
        {
            if (string.IsNullOrEmpty(content) || idToNickname == null)
                return content;

            return AT_REGEX.Replace(content, match =>
            {
                var id = match.Groups[1].Value;
                if (idToNickname.TryGetValue(id, out var nickname))
                {
                    return $"@{nickname}";
                }
                return $"@{id}";
            });
        }

        #endregion

        #region 消息类型判断

        /// <summary>
        /// 判断是否为自定义加密消息
        /// </summary>
        public bool IsEncryptedMessage(int msgType, string msgAttach)
        {
            // msg_type=100 且 msg_attach 包含 {"b":"..."}
            if (msgType != 100)
                return false;

            if (string.IsNullOrEmpty(msgAttach))
                return false;

            try
            {
                var attach = _serializer.Deserialize<Dictionary<string, object>>(msgAttach);
                return attach != null && attach.ContainsKey("b");
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region 辅助方法

        private void Log(string message)
        {
            Logger.Info($"[MsgEncrypt] {message}");
            OnLog?.Invoke(message);
        }

        #endregion
    }

    #region 消息头数据结构

    /// <summary>
    /// msg_attach.b 消息头结构
    /// </summary>
    public class MsgAttachHeader
    {
        /// <summary>Magic 标识 (2字节)</summary>
        public byte[] Magic { get; set; }

        /// <summary>版本标识 (2字节)</summary>
        public byte[] Version { get; set; }

        /// <summary>消息元数据 (4字节)</summary>
        public uint MsgMeta { get; set; }

        /// <summary>加密标识 (2字节, "ci")</summary>
        public byte[] Cipher { get; set; }

        /// <summary>填充 (6字节)</summary>
        public byte[] Padding { get; set; }

        /// <summary>加密数据</summary>
        public byte[] EncryptedData { get; set; }

        /// <summary>是否为有效的 Magic</summary>
        public bool IsValidMagic { get; set; }

        /// <summary>是否为有效的 Version</summary>
        public bool IsValidVersion { get; set; }

        /// <summary>是否加密 (包含 "ci" 标识)</summary>
        public bool IsEncrypted { get; set; }

        /// <summary>是否为有效的消息头</summary>
        public bool IsValid => IsValidMagic && IsValidVersion;

        /// <summary>
        /// 获取 Magic 的十六进制表示
        /// </summary>
        public string MagicHex => Magic != null ? BitConverter.ToString(Magic) : "";

        /// <summary>
        /// 获取加密数据长度
        /// </summary>
        public int EncryptedDataLength => EncryptedData?.Length ?? 0;

        /// <summary>
        /// 转换为调试字符串
        /// </summary>
        public override string ToString()
        {
            return $"Magic={MagicHex}, Valid={IsValid}, Encrypted={IsEncrypted}, DataLen={EncryptedDataLength}";
        }
    }

    #endregion

    #region NIM 消息类型常量 (完整定义)

    /// <summary>
    /// NIM 消息类型 - 根据最底层消息协议文档
    /// </summary>
    public static class NIMMessageTypes
    {
        /// <summary>文本消息</summary>
        public const int TEXT = 0;

        /// <summary>图片消息</summary>
        public const int IMAGE = 1;

        /// <summary>语音消息</summary>
        public const int VOICE = 2;

        /// <summary>视频消息</summary>
        public const int VIDEO = 3;

        /// <summary>地理位置</summary>
        public const int LOCATION = 4;

        /// <summary>通知消息 (群事件)</summary>
        public const int NOTIFICATION = 5;

        /// <summary>文件消息</summary>
        public const int FILE = 6;

        /// <summary>提示消息</summary>
        public const int TIP = 10;

        /// <summary>自定义消息</summary>
        public const int CUSTOM = 100;

        /// <summary>未知类型</summary>
        public const int UNKNOWN = 1000;

        /// <summary>
        /// 获取消息类型名称
        /// </summary>
        public static string GetTypeName(int msgType)
        {
            switch (msgType)
            {
                case TEXT: return "文本";
                case IMAGE: return "图片";
                case VOICE: return "语音";
                case VIDEO: return "视频";
                case LOCATION: return "位置";
                case NOTIFICATION: return "通知";
                case FILE: return "文件";
                case TIP: return "提示";
                case CUSTOM: return "自定义";
                default: return $"未知({msgType})";
            }
        }
    }

    /// <summary>
    /// NIM 会话类型
    /// </summary>
    public static class NIMSessionTypes
    {
        /// <summary>点对点私聊</summary>
        public const int P2P = 0;

        /// <summary>群组聊天</summary>
        public const int TEAM = 1;
    }

    /// <summary>
    /// 插件消息类型 (框架投递)
    /// </summary>
    public static class PluginMsgTypes
    {
        /// <summary>私聊消息</summary>
        public const int PRIVATE = 1001;

        /// <summary>群聊消息</summary>
        public const int GROUP = 1002;

        /// <summary>好友请求</summary>
        public const int FRIEND_REQUEST = 1003;

        /// <summary>好友申请通知</summary>
        public const int FRIEND_APPLY = 1015;
    }

    #endregion

    #region ID映射服务

    /// <summary>
    /// ID映射缓存 - 旺商聊号 ↔ NIM ID
    /// </summary>
    public class IDMappingCache
    {
        private static readonly Lazy<IDMappingCache> _instance =
            new Lazy<IDMappingCache>(() => new IDMappingCache());

        public static IDMappingCache Instance => _instance.Value;

        // 旺商聊号 -> NIM ID
        private readonly Dictionary<string, string> _wslToNim = new Dictionary<string, string>();
        
        // NIM ID -> 旺商聊号
        private readonly Dictionary<string, string> _nimToWsl = new Dictionary<string, string>();

        private readonly object _lock = new object();

        private IDMappingCache()
        {
            // 预置已知的映射关系 (根据文档)
            AddMapping("621705120", "1628907626");  // 机器人
            AddMapping("982576571", "2092166259");  // 用户
            AddMapping("781361487", "1948408648");  // 用户
            AddMapping("3962369093", "40821608989"); // 群组
        }

        /// <summary>
        /// 添加映射关系
        /// </summary>
        public void AddMapping(string wslId, string nimId)
        {
            if (string.IsNullOrEmpty(wslId) || string.IsNullOrEmpty(nimId))
                return;

            lock (_lock)
            {
                _wslToNim[wslId] = nimId;
                _nimToWsl[nimId] = wslId;
            }
        }

        /// <summary>
        /// 旺商聊号 -> NIM ID
        /// </summary>
        public string GetNimId(string wslId)
        {
            if (string.IsNullOrEmpty(wslId))
                return null;

            lock (_lock)
            {
                _wslToNim.TryGetValue(wslId, out var nimId);
                return nimId;
            }
        }

        /// <summary>
        /// NIM ID -> 旺商聊号
        /// </summary>
        public string GetWslId(string nimId)
        {
            if (string.IsNullOrEmpty(nimId))
                return null;

            lock (_lock)
            {
                _nimToWsl.TryGetValue(nimId, out var wslId);
                return wslId;
            }
        }

        /// <summary>
        /// 检查是否有旺商聊号的映射
        /// </summary>
        public bool HasWslMapping(string wslId)
        {
            lock (_lock)
            {
                return _wslToNim.ContainsKey(wslId);
            }
        }

        /// <summary>
        /// 检查是否有NIM ID的映射
        /// </summary>
        public bool HasNimMapping(string nimId)
        {
            lock (_lock)
            {
                return _nimToWsl.ContainsKey(nimId);
            }
        }

        /// <summary>
        /// 获取所有映射数量
        /// </summary>
        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _wslToNim.Count;
                }
            }
        }

        /// <summary>
        /// 清空缓存
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _wslToNim.Clear();
                _nimToWsl.Clear();
            }
        }
    }

    #endregion
}
