using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using WSLFramework.Utils;

namespace WSLFramework.Services
{
    /// <summary>
    /// 云信客户端服务 - 对接旺商聊/网易云信SDK
    /// 基于招财狗 YX_Client.dll 逆向分析
    /// </summary>
    public class YXClientService : IDisposable
    {
        private readonly JavaScriptSerializer _serializer;
        private readonly HttpClient _httpClient;
        
        // 连接信息
        public string Account { get; private set; }
        public string JwtToken { get; private set; }
        public string Wwid { get; private set; }
        public string Nickname { get; private set; }
        public bool IsLoggedIn { get; private set; }
        
        // 当前群
        public string CurrentGroupId { get; set; }
        public string CurrentGroupCloudId { get; set; }
        
        // 消息类型常量 (从日志分析)
        public const int MSG_TYPE_P2P = 1001;
        public const int MSG_TYPE_GROUP = 1002;
        public const int MSG_TYPE_NOTIFY = 1003;
        
        // 通知类型
        public const string NOTIFY_GROUP_MUTE_ON = "NOTIFY_TYPE_GROUP_MUTE_1";
        public const string NOTIFY_GROUP_MUTE_OFF = "NOTIFY_TYPE_GROUP_MUTE_0";
        public const string NOTIFY_USER_UPDATE_NAME = "NOTIFY_TYPE_USER_UPDATE_NAME";
        
        // 事件
        public event Action<string> OnLog;
        public event Action<bool> OnLoginStateChanged;
        public event Action<YXMessage> OnMessageReceived;
        public event Action<string, bool> OnGroupMuteChanged;  // groupId, isMuted
        public event Action<string, string, string> OnMemberNameChanged;  // groupId, accid, newName
        
        public YXClientService()
        {
            _serializer = new JavaScriptSerializer();
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }
        
        #region 登录相关
        
        /// <summary>
        /// 从配置文件加载登录信息
        /// </summary>
        public bool LoadConfigFromFile(string configPath)
        {
            try
            {
                if (!File.Exists(configPath))
                {
                    Log($"配置文件不存在: {configPath}");
                    return false;
                }
                
                var lines = File.ReadAllLines(configPath, Encoding.Default);
                var section = "";
                
                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    
                    if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                    {
                        section = trimmed.Substring(1, trimmed.Length - 2);
                        continue;
                    }
                    
                    var parts = trimmed.Split(new[] { '=' }, 2);
                    if (parts.Length != 2) continue;
                    
                    var key = parts[0].Trim();
                    var value = parts[1].Trim();
                    
                    // 读取账号信息 (section 是云信accid，如 "1628907626")
                    if (long.TryParse(section, out _))
                    {
                        switch (key.ToLower())
                        {
                            case "账号":
                            case "account":
                                Account = value;
                                break;
                            case "jwttoken":
                                JwtToken = value;
                                break;
                            case "qun":
                                // 解密群ID
                                CurrentGroupId = DecryptGroupId(value);
                                break;
                            case "nickname":
                            case "昵称":
                                Nickname = value;
                                break;
                        }
                        
                        if (string.IsNullOrEmpty(Wwid))
                        {
                            Wwid = section;
                        }
                    }
                }
                
                Log($"配置加载完成: 账号={Account}, wwid={Wwid}");
                return !string.IsNullOrEmpty(Account) && !string.IsNullOrEmpty(JwtToken);
            }
            catch (Exception ex)
            {
                Log($"加载配置失败: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 解密群ID (Base64)
        /// </summary>
        private string DecryptGroupId(string encrypted)
        {
            try
            {
                var bytes = Convert.FromBase64String(encrypted);
                // 可能是AES加密，暂时返回原值
                return encrypted;
            }
            catch
            {
                return encrypted;
            }
        }
        
        /// <summary>
        /// 使用JWT Token登录
        /// </summary>
        public async Task<bool> LoginAsync(string jwtToken = null)
        {
            try
            {
                if (!string.IsNullOrEmpty(jwtToken))
                {
                    JwtToken = jwtToken;
                }
                
                if (string.IsNullOrEmpty(JwtToken))
                {
                    Log("JWT Token为空，无法登录");
                    return false;
                }
                
                // 实际实现需要调用云信SDK
                // 这里模拟登录成功
                IsLoggedIn = true;
                OnLoginStateChanged?.Invoke(true);
                Log($"登录成功: {Account}");
                
                return true;
            }
            catch (Exception ex)
            {
                Log($"登录失败: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 登出
        /// </summary>
        public void Logout()
        {
            IsLoggedIn = false;
            OnLoginStateChanged?.Invoke(false);
            Log("已登出");
        }
        
        #endregion
        
        #region 消息发送
        
        /// <summary>
        /// 发送群消息
        /// </summary>
        public async Task<bool> SendGroupMessageAsync(string groupId, string content)
        {
            if (!IsLoggedIn)
            {
                Log("未登录，无法发送消息");
                return false;
            }
            
            try
            {
                Log($"发送群消息: 群={groupId}, 内容={content.Substring(0, Math.Min(50, content.Length))}...");
                
                // 实际需要通过云信SDK发送
                // 这里返回成功，实际发送由CDPBridge处理
                return true;
            }
            catch (Exception ex)
            {
                Log($"发送群消息失败: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 发送私聊消息
        /// </summary>
        public async Task<bool> SendP2PMessageAsync(string toAccid, string content)
        {
            if (!IsLoggedIn)
            {
                Log("未登录，无法发送消息");
                return false;
            }
            
            try
            {
                Log($"发送私聊: 目标={toAccid}, 内容={content.Substring(0, Math.Min(50, content.Length))}...");
                return true;
            }
            catch (Exception ex)
            {
                Log($"发送私聊失败: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 发送@消息
        /// </summary>
        public async Task<bool> SendAtMessageAsync(string groupId, string atAccid, string content)
        {
            // 格式: [LQ:@QQ号] 或 [@QQ号]
            var atContent = $"[@{atAccid}] {content}";
            return await SendGroupMessageAsync(groupId, atContent);
        }
        
        #endregion
        
        #region 群管理
        
        /// <summary>
        /// 全群禁言
        /// </summary>
        public async Task<bool> MuteAllAsync(string groupId, bool mute)
        {
            try
            {
                Log($"设置全群禁言: 群={groupId}, 禁言={mute}");
                
                // 实际需要调用云信SDK
                OnGroupMuteChanged?.Invoke(groupId, mute);
                return true;
            }
            catch (Exception ex)
            {
                Log($"设置禁言失败: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 禁言成员
        /// </summary>
        public async Task<bool> MuteMemberAsync(string groupId, string accid, int duration = 0)
        {
            try
            {
                Log($"禁言成员: 群={groupId}, 成员={accid}, 时长={duration}秒");
                return true;
            }
            catch (Exception ex)
            {
                Log($"禁言成员失败: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 设置群成员名片
        /// </summary>
        public async Task<bool> SetMemberCardAsync(string groupId, string accid, string card)
        {
            try
            {
                Log($"设置名片: 群={groupId}, 成员={accid}, 名片={card}");
                OnMemberNameChanged?.Invoke(groupId, accid, card);
                return true;
            }
            catch (Exception ex)
            {
                Log($"设置名片失败: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 踢出群成员
        /// </summary>
        public async Task<bool> KickMemberAsync(string groupId, string accid)
        {
            try
            {
                Log($"踢出成员: 群={groupId}, 成员={accid}");
                return true;
            }
            catch (Exception ex)
            {
                Log($"踢出成员失败: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 获取群信息
        /// </summary>
        public async Task<YXGroupInfo> GetGroupInfoAsync(string groupId)
        {
            try
            {
                Log($"获取群信息: {groupId}");
                
                return new YXGroupInfo
                {
                    GroupId = groupId,
                    GroupCloudId = CurrentGroupCloudId,
                    GroupName = "群聊",
                    MemberCount = 0
                };
            }
            catch (Exception ex)
            {
                Log($"获取群信息失败: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 获取群成员列表
        /// </summary>
        public async Task<List<YXMemberInfo>> GetGroupMembersAsync(string groupId)
        {
            try
            {
                Log($"获取群成员: {groupId}");
                return new List<YXMemberInfo>();
            }
            catch (Exception ex)
            {
                Log($"获取群成员失败: {ex.Message}");
                return new List<YXMemberInfo>();
            }
        }
        
        #endregion
        
        #region 消息处理
        
        /// <summary>
        /// 解析云信消息
        /// </summary>
        public YXMessage ParseMessage(string rawJson)
        {
            try
            {
                var data = _serializer.Deserialize<Dictionary<string, object>>(rawJson);
                if (data == null) return null;
                
                var msg = new YXMessage { RawJson = rawJson };
                
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
                        msg.ToType = GetInt(content, "to_type");
                        msg.TalkId = GetString(content, "talk_id");
                        msg.ClientMsgId = GetString(content, "client_msg_id");
                        
                        // 解析消息附件
                        var msgAttach = GetString(content, "msg_attach");
                        if (!string.IsNullOrEmpty(msgAttach))
                        {
                            ParseMsgAttach(msg, msgAttach);
                        }
                    }
                }
                
                return msg;
            }
            catch (Exception ex)
            {
                Log($"解析消息失败: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 解析消息附件
        /// </summary>
        private void ParseMsgAttach(YXMessage msg, string attachJson)
        {
            try
            {
                var attach = _serializer.Deserialize<Dictionary<string, object>>(attachJson);
                if (attach == null) return;
                
                // 解析 "b" 字段 - Base64编码的消息内容
                if (attach.ContainsKey("b"))
                {
                    var b = attach["b"] as string;
                    if (!string.IsNullOrEmpty(b))
                    {
                        msg.Content = DecodeMessageContent(b);
                    }
                }
                
                // 解析通知数据
                if (attach.ContainsKey("data"))
                {
                    var dataObj = attach["data"] as Dictionary<string, object>;
                    if (dataObj != null)
                    {
                        // 群信息
                        if (dataObj.ContainsKey("team_info"))
                        {
                            var teamInfo = dataObj["team_info"] as Dictionary<string, object>;
                            if (teamInfo != null)
                            {
                                msg.TeamId = GetString(teamInfo, "tid");
                                msg.MuteAll = GetInt(teamInfo, "mute_all") == 1;
                            }
                        }
                        
                        // 成员名片
                        if (dataObj.ContainsKey("name_cards"))
                        {
                            var nameCards = dataObj["name_cards"] as object[];
                            if (nameCards != null && nameCards.Length > 0)
                            {
                                var card = nameCards[0] as Dictionary<string, object>;
                                if (card != null)
                                {
                                    msg.MemberAccid = GetString(card, "accid");
                                    msg.MemberCard = GetString(card, "name");
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"解析附件失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 解码消息内容
        /// </summary>
        private string DecodeMessageContent(string encoded)
        {
            try
            {
                // Base64 URL-safe 解码
                var base64 = encoded.Replace('-', '+').Replace('_', '/');
                var padLen = (4 - base64.Length % 4) % 4;
                base64 = base64 + new string('=', padLen);
                
                var bytes = Convert.FromBase64String(base64);
                
                // 尝试提取UTF-8文本
                if (bytes.Length > 10)
                {
                    // 跳过协议头，查找文本
                    for (int i = 0; i < bytes.Length - 5; i++)
                    {
                        // 检查是否是UTF-8中文开始
                        if (bytes[i] >= 0xE0 && bytes[i] <= 0xEF)
                        {
                            try
                            {
                                var text = Encoding.UTF8.GetString(bytes, i, bytes.Length - i);
                                if (!string.IsNullOrWhiteSpace(text))
                                {
                                    return text.TrimEnd('\0');
                                }
                            }
                            catch { }
                        }
                    }
                }
                
                return encoded;
            }
            catch
            {
                return encoded;
            }
        }
        
        /// <summary>
        /// 处理收到的消息
        /// </summary>
        public void HandleIncomingMessage(YXMessage msg)
        {
            if (msg == null) return;
            
            // 触发消息事件
            OnMessageReceived?.Invoke(msg);
            
            // 处理特殊通知
            if (!string.IsNullOrEmpty(msg.NotifyType))
            {
                switch (msg.NotifyType)
                {
                    case NOTIFY_GROUP_MUTE_ON:
                        OnGroupMuteChanged?.Invoke(msg.TeamId, true);
                        break;
                    case NOTIFY_GROUP_MUTE_OFF:
                        OnGroupMuteChanged?.Invoke(msg.TeamId, false);
                        break;
                    case NOTIFY_USER_UPDATE_NAME:
                        OnMemberNameChanged?.Invoke(msg.TeamId, msg.MemberAccid, msg.MemberCard);
                        break;
                }
            }
        }
        
        #endregion
        
        #region 辅助方法
        
        private string GetString(Dictionary<string, object> dict, string key)
        {
            if (dict.ContainsKey(key))
            {
                return dict[key]?.ToString() ?? "";
            }
            return "";
        }
        
        private int GetInt(Dictionary<string, object> dict, string key)
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
        
        private long GetLong(Dictionary<string, object> dict, string key)
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
        
        private void Log(string message)
        {
            Logger.Info($"[YX] {message}");
            OnLog?.Invoke(message);
        }
        
        #endregion
        
        public void Dispose()
        {
            Logout();
            _httpClient?.Dispose();
        }
    }
    
    #region 数据模型
    
    /// <summary>
    /// 云信消息
    /// </summary>
    public class YXMessage
    {
        public string RawJson { get; set; }
        public long ServerMsgId { get; set; }
        public long Time { get; set; }
        public int MsgType { get; set; }
        public string FromId { get; set; }
        public string FromNick { get; set; }
        public string ToAccid { get; set; }
        public int ToType { get; set; }
        public string TalkId { get; set; }
        public string ClientMsgId { get; set; }
        public string Content { get; set; }
        
        // 通知相关
        public string NotifyType { get; set; }
        public string TeamId { get; set; }
        public bool MuteAll { get; set; }
        public string MemberAccid { get; set; }
        public string MemberCard { get; set; }
        
        public bool IsP2P => ToType == 0;
        public bool IsGroup => ToType == 1;
        public DateTime MessageTime => DateTimeOffset.FromUnixTimeMilliseconds(Time).LocalDateTime;
    }
    
    /// <summary>
    /// 群信息
    /// </summary>
    public class YXGroupInfo
    {
        public string GroupId { get; set; }
        public string GroupCloudId { get; set; }
        public string GroupName { get; set; }
        public string GroupAvatar { get; set; }
        public string GroupNickName { get; set; }
        public int MemberCount { get; set; }
        public string GroupType { get; set; }
    }
    
    /// <summary>
    /// 成员信息
    /// </summary>
    public class YXMemberInfo
    {
        public string Accid { get; set; }
        public string QQ { get; set; }
        public string Nickname { get; set; }
        public string Card { get; set; }
        public int Type { get; set; }  // 0=成员, 1=管理员, 2=群主
        public bool Muted { get; set; }
    }
    
    /// <summary>
    /// QQ号与云信accid映射
    /// </summary>
    public class QQAccidMap
    {
        // 从日志分析的映射关系
        public static readonly Dictionary<string, string> QQToAccid = new Dictionary<string, string>
        {
            { "621705120", "1628907626" },
            { "781361487", "1948408648" },
            { "982576571", "2092166259" },
            { "82840376", "1391351554" },
            { "250416936", "1898203199" }
        };
        
        public static string GetAccid(string qq)
        {
            return QQToAccid.ContainsKey(qq) ? QQToAccid[qq] : qq;
        }
        
        public static string GetQQ(string accid)
        {
            foreach (var kv in QQToAccid)
            {
                if (kv.Value == accid) return kv.Key;
            }
            return accid;
        }
    }
    
    #endregion
}
