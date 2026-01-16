using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using WSLFramework.Utils;

namespace WSLFramework.Services
{
    /// <summary>
    /// 群成员管理服务 - 根据群成员获取协议完整文档实现
    /// 支持框架API、HTTP API、ID转换等功能
    /// </summary>
    public class GroupMemberService
    {
        #region 单例模式

        private static readonly Lazy<GroupMemberService> _instance =
            new Lazy<GroupMemberService>(() => new GroupMemberService());

        public static GroupMemberService Instance => _instance.Value;

        #endregion

        #region 常量 - 成员类型

        /// <summary>群主</summary>
        public const int MEMBER_TYPE_OWNER = 0;

        /// <summary>普通成员</summary>
        public const int MEMBER_TYPE_NORMAL = 1;

        /// <summary>管理员</summary>
        public const int MEMBER_TYPE_ADMIN = 2;

        #endregion

        #region 常量 - ID转换类型

        /// <summary>NIM accid → 旺商聊号</summary>
        public const int ID_TYPE_NIM_TO_WSL = 0;

        /// <summary>旺商聊号 → NIM accid</summary>
        public const int ID_TYPE_WSL_TO_NIM = 1;

        /// <summary>旺商聊群号 → NIM tid</summary>
        public const int ID_TYPE_GROUP_WSL_TO_NIM = 2;

        #endregion

        #region 私有字段

        private XPluginClient _xpluginClient;
        private WangShangLiaoHttpApi _httpApi;
        private readonly JavaScriptSerializer _serializer;
        
        // 群成员缓存 {群号: {成员ID: 成员信息}}
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, GroupMember>> _memberCache;
        
        // 群信息缓存 {群号: 群信息}
        private readonly ConcurrentDictionary<string, GroupInfo> _groupCache;

        // 自动收集的成员 (通过消息自动收集)
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, CollectedMember>> _collectedMembers;

        #endregion

        #region 事件

        public event Action<string> OnLog;
        public event Action<string, List<GroupMember>> OnMembersLoaded;
        public event Action<string, GroupMember> OnNewMemberDiscovered;

        #endregion

        #region 构造函数

        private GroupMemberService()
        {
            _serializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
            _memberCache = new ConcurrentDictionary<string, ConcurrentDictionary<string, GroupMember>>();
            _groupCache = new ConcurrentDictionary<string, GroupInfo>();
            _collectedMembers = new ConcurrentDictionary<string, ConcurrentDictionary<string, CollectedMember>>();
        }

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化服务
        /// </summary>
        public void Initialize(XPluginClient xpluginClient, WangShangLiaoHttpApi httpApi = null)
        {
            _xpluginClient = xpluginClient;
            _httpApi = httpApi ?? WangShangLiaoHttpApi.Instance;
            Log("群成员服务初始化完成");
        }

        #endregion

        #region 框架API - 获取绑定群

        /// <summary>
        /// 获取绑定的群列表
        /// API: 取绑定群|机器人号
        /// </summary>
        public async Task<List<string>> GetBindGroupsAsync(string robotId)
        {
            var groups = new List<string>();

            if (_xpluginClient == null || !_xpluginClient.IsConnected)
            {
                Log("XPlugin 未连接，无法获取绑定群");
                return groups;
            }

            try
            {
                var response = await _xpluginClient.GetBindGroupsAsync(robotId);
                Log($"取绑定群响应: {response?.Substring(0, Math.Min(100, response?.Length ?? 0))}...");

                // 解析 Base64 响应
                var result = DecodeBase64Response(response);
                if (result != null && result.ContainsKey("groups"))
                {
                    var groupArray = result["groups"] as object[];
                    if (groupArray != null)
                    {
                        foreach (var g in groupArray)
                        {
                            groups.Add(g.ToString());
                        }
                    }
                }

                Log($"获取到 {groups.Count} 个绑定群");
            }
            catch (Exception ex)
            {
                Log($"获取绑定群失败: {ex.Message}");
            }

            return groups;
        }

        #endregion

        #region 框架API - 获取群资料

        /// <summary>
        /// 获取群资料
        /// API: ww_获取群资料|机器人号|群号
        /// </summary>
        public async Task<GroupInfo> GetGroupInfoAsync(string robotId, string groupId)
        {
            // 先检查缓存
            if (_groupCache.TryGetValue(groupId, out var cached))
            {
                return cached;
            }

            if (_xpluginClient == null || !_xpluginClient.IsConnected)
            {
                Log("XPlugin 未连接，无法获取群资料");
                return null;
            }

            try
            {
                var response = await _xpluginClient.GetGroupInfoAsync(robotId, groupId);
                Log($"获取群资料响应: {response?.Substring(0, Math.Min(100, response?.Length ?? 0))}...");

                var result = DecodeBase64Response(response);
                if (result != null)
                {
                    var groupInfo = ParseGroupInfo(result);
                    if (groupInfo != null)
                    {
                        groupInfo.WslGroupId = groupId;
                        _groupCache[groupId] = groupInfo;
                        return groupInfo;
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"获取群资料失败: {ex.Message}");
            }

            return null;
        }

        #endregion

        #region 框架API - 获取群成员

        /// <summary>
        /// 获取群成员列表
        /// API: 取群成员|机器人号|群号
        /// </summary>
        public async Task<List<GroupMember>> GetGroupMembersAsync(string robotId, string groupId)
        {
            var members = new List<GroupMember>();

            if (_xpluginClient == null || !_xpluginClient.IsConnected)
            {
                Log("XPlugin 未连接，无法获取群成员");
                return members;
            }

            try
            {
                var apiCall = $"取群成员|{robotId}|{groupId}";
                var response = await _xpluginClient.SendApiAsync(apiCall);
                Log($"获取群成员响应: {response?.Substring(0, Math.Min(100, response?.Length ?? 0))}...");

                var result = DecodeBase64Response(response);
                if (result != null && result.ContainsKey("members"))
                {
                    var memberArray = result["members"] as object[];
                    if (memberArray != null)
                    {
                        foreach (var m in memberArray)
                        {
                            var memberDict = m as Dictionary<string, object>;
                            if (memberDict != null)
                            {
                                var member = ParseGroupMember(memberDict);
                                if (member != null)
                                {
                                    members.Add(member);
                                    
                                    // 更新缓存
                                    UpdateMemberCache(groupId, member);
                                }
                            }
                        }
                    }
                }

                Log($"群 {groupId} 获取到 {members.Count} 个成员");
                OnMembersLoaded?.Invoke(groupId, members);
            }
            catch (Exception ex)
            {
                Log($"获取群成员失败: {ex.Message}");
            }

            return members;
        }

        #endregion

        #region HTTP API - ID转换

        /// <summary>
        /// ID互查 - 旺商聊号/NIM ID 互相转换
        /// </summary>
        /// <param name="id">要转换的ID</param>
        /// <param name="idType">0=NIM→旺商聊, 1=旺商聊→NIM, 2=群旺商聊→NIM tid</param>
        public async Task<string> ConvertIdAsync(string id, int idType)
        {
            if (_httpApi == null)
            {
                Log("HTTP API 未初始化");
                return null;
            }

            try
            {
                var result = await _httpApi.GetGidAsync(id, idType);
                if (result != null && result.Code == 0 && result.Data != null)
                {
                    var data = result.Data as Dictionary<string, object>;
                    if (data != null && data.ContainsKey("gid"))
                    {
                        return data["gid"]?.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"ID转换失败: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// 旺商聊群号 → NIM tid
        /// </summary>
        public async Task<string> WslGroupToNimTidAsync(string wslGroupId)
        {
            return await ConvertIdAsync(wslGroupId, ID_TYPE_GROUP_WSL_TO_NIM);
        }

        /// <summary>
        /// 旺商聊号 → NIM accid
        /// </summary>
        public async Task<string> WslIdToNimIdAsync(string wslId)
        {
            return await ConvertIdAsync(wslId, ID_TYPE_WSL_TO_NIM);
        }

        /// <summary>
        /// NIM accid → 旺商聊号
        /// </summary>
        public async Task<string> NimIdToWslIdAsync(string nimId)
        {
            return await ConvertIdAsync(nimId, ID_TYPE_NIM_TO_WSL);
        }

        #endregion

        #region HTTP API - 获取用户信息

        /// <summary>
        /// 通过ID获取用户详细信息
        /// </summary>
        /// <param name="id">用户ID</param>
        /// <param name="idType">0=NIM accid, 1=旺商聊号</param>
        public async Task<UserInfo> GetUserInfoAsync(string id, int idType = 1)
        {
            if (_httpApi == null)
            {
                Log("HTTP API 未初始化");
                return null;
            }

            try
            {
                var result = await _httpApi.GetUserInfoByIdAsync(id, idType);
                if (result != null && result.Code == 0 && result.Data != null)
                {
                    var data = result.Data as Dictionary<string, object>;
                    if (data != null)
                    {
                        return new UserInfo
                        {
                            WslId = data.ContainsKey("account") ? data["account"]?.ToString() : "",
                            NimId = data.ContainsKey("nimId") ? data["nimId"]?.ToString() : "",
                            Nickname = data.ContainsKey("nickname") ? data["nickname"]?.ToString() : "",
                            GroupId = data.ContainsKey("group_id") ? data["group_id"]?.ToString() : ""
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"获取用户信息失败: {ex.Message}");
            }

            return null;
        }

        #endregion

        #region 消息自动收集

        /// <summary>
        /// 从群消息中收集发送者信息
        /// 格式: 机器人账号=X，主动账号=Y，群号=Z...
        /// </summary>
        public void CollectFromMessage(string groupId, string senderId, string senderNick = null)
        {
            if (string.IsNullOrEmpty(groupId) || string.IsNullOrEmpty(senderId))
                return;

            // 初始化群
            var groupMembers = _collectedMembers.GetOrAdd(groupId, 
                _ => new ConcurrentDictionary<string, CollectedMember>());

            var isNew = !groupMembers.ContainsKey(senderId);

            // 更新成员信息
            var member = groupMembers.GetOrAdd(senderId, _ => new CollectedMember
            {
                WslId = senderId,
                FirstSeen = DateTime.Now
            });

            member.MessageCount++;
            member.LastSeen = DateTime.Now;
            if (!string.IsNullOrEmpty(senderNick))
            {
                member.Nickname = senderNick;
            }

            // 通知新成员发现
            if (isNew)
            {
                Log($"发现新成员: 群{groupId} - {senderId}");
                OnNewMemberDiscovered?.Invoke(groupId, new GroupMember
                {
                    WslId = senderId,
                    Nickname = senderNick ?? senderId
                });
            }
        }

        /// <summary>
        /// 获取自动收集的成员列表
        /// </summary>
        public List<CollectedMember> GetCollectedMembers(string groupId)
        {
            if (_collectedMembers.TryGetValue(groupId, out var members))
            {
                return new List<CollectedMember>(members.Values);
            }
            return new List<CollectedMember>();
        }

        #endregion

        #region 缓存管理

        /// <summary>
        /// 更新成员缓存
        /// </summary>
        private void UpdateMemberCache(string groupId, GroupMember member)
        {
            var groupMembers = _memberCache.GetOrAdd(groupId,
                _ => new ConcurrentDictionary<string, GroupMember>());
            
            groupMembers[member.WslId ?? member.NimAccid] = member;

            // 同步更新ID映射
            if (!string.IsNullOrEmpty(member.WslId) && !string.IsNullOrEmpty(member.NimAccid))
            {
                IDMappingCache.Instance.AddMapping(member.WslId, member.NimAccid);
            }
        }

        /// <summary>
        /// 从缓存获取成员
        /// </summary>
        public GroupMember GetMemberFromCache(string groupId, string memberId)
        {
            if (_memberCache.TryGetValue(groupId, out var groupMembers))
            {
                if (groupMembers.TryGetValue(memberId, out var member))
                {
                    return member;
                }
            }
            return null;
        }

        /// <summary>
        /// 获取缓存的所有群成员
        /// </summary>
        public List<GroupMember> GetCachedMembers(string groupId)
        {
            if (_memberCache.TryGetValue(groupId, out var groupMembers))
            {
                return new List<GroupMember>(groupMembers.Values);
            }
            return new List<GroupMember>();
        }

        /// <summary>
        /// 清除指定群的缓存
        /// </summary>
        public void ClearGroupCache(string groupId)
        {
            _memberCache.TryRemove(groupId, out _);
            _groupCache.TryRemove(groupId, out _);
        }

        /// <summary>
        /// 清除所有缓存
        /// </summary>
        public void ClearAllCache()
        {
            _memberCache.Clear();
            _groupCache.Clear();
        }

        #endregion

        #region 本地存储集成

        /// <summary>
        /// 从本地存储加载群成员（旺商聊客户端缓存）
        /// </summary>
        /// <param name="wslGroupId">旺商聊群号</param>
        public List<GroupMember> LoadMembersFromLocalStorage(string wslGroupId)
        {
            var result = new List<GroupMember>();
            
            try
            {
                var localStorage = WangShangLiaoLocalStorage.Instance;
                
                // 通过群号查找 NIM 群 ID
                var nimGroupId = localStorage.FindNimGroupId(wslGroupId);
                if (!nimGroupId.HasValue)
                {
                    Log($"未在本地存储中找到群 {wslGroupId} 的 NIM 群 ID");
                    return result;
                }
                
                // 从本地存储读取成员
                var localMembers = localStorage.GetGroupMembers(nimGroupId.Value);
                
                foreach (var local in localMembers)
                {
                    var member = new GroupMember
                    {
                        WslId = local.UserId.ToString(),
                        NimAccid = local.NimId.ToString(),
                        Nickname = local.UserNick,
                        GroupCard = local.GroupMemberNick,
                        MemberType = ConvertRole(local.Role)
                    };
                    
                    result.Add(member);
                    UpdateMemberCache(wslGroupId, member);
                }
                
                Log($"从本地存储加载群 {wslGroupId} 成员: {result.Count} 人");
            }
            catch (Exception ex)
            {
                Log($"从本地存储加载群成员失败: {ex.Message}");
            }
            
            return result;
        }

        /// <summary>
        /// 获取登录用户信息（从本地存储）
        /// </summary>
        public LoginInfo GetLoginInfoFromLocalStorage()
        {
            return WangShangLiaoLocalStorage.Instance.ReadLoginInfo();
        }

        /// <summary>
        /// 转换角色类型
        /// </summary>
        private int ConvertRole(LocalGroupRole role)
        {
            switch (role)
            {
                case LocalGroupRole.Owner: return MEMBER_TYPE_OWNER;
                case LocalGroupRole.Admin: return MEMBER_TYPE_ADMIN;
                default: return MEMBER_TYPE_NORMAL;
            }
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 解码 Base64 响应
        /// </summary>
        private Dictionary<string, object> DecodeBase64Response(string response)
        {
            if (string.IsNullOrEmpty(response))
                return null;

            try
            {
                // 提取 Base64 部分
                var base64Part = response;
                if (response.Contains("返回结果:"))
                {
                    var idx = response.IndexOf("返回结果:");
                    base64Part = response.Substring(idx + 5).Trim();
                    var endIdx = base64Part.IndexOf('\n');
                    if (endIdx > 0)
                        base64Part = base64Part.Substring(0, endIdx);
                }

                // URL-safe Base64 转标准
                var standard = base64Part.Replace('-', '+').Replace('_', '/');
                var padding = 4 - (standard.Length % 4);
                if (padding < 4)
                {
                    standard += new string('=', padding);
                }

                var decoded = Convert.FromBase64String(standard);
                var json = System.Text.Encoding.UTF8.GetString(decoded);

                return _serializer.Deserialize<Dictionary<string, object>>(json);
            }
            catch (Exception ex)
            {
                Log($"解码 Base64 响应失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 解析群信息
        /// </summary>
        private GroupInfo ParseGroupInfo(Dictionary<string, object> data)
        {
            if (data == null) return null;

            return new GroupInfo
            {
                NimTid = data.ContainsKey("tid") ? data["tid"]?.ToString() : "",
                Name = data.ContainsKey("name") ? data["name"]?.ToString() : "",
                OwnerId = data.ContainsKey("owner") ? data["owner"]?.ToString() : "",
                MemberCount = data.ContainsKey("member_count") ? Convert.ToInt32(data["member_count"]) : 0,
                CreateTime = data.ContainsKey("create_time") ? Convert.ToInt64(data["create_time"]) : 0
            };
        }

        /// <summary>
        /// 解析群成员
        /// </summary>
        private GroupMember ParseGroupMember(Dictionary<string, object> data)
        {
            if (data == null) return null;

            return new GroupMember
            {
                WslId = data.ContainsKey("旺商聊号") ? data["旺商聊号"]?.ToString() : "",
                NimAccid = data.ContainsKey("nim_accid") ? data["nim_accid"]?.ToString() :
                           data.ContainsKey("accid") ? data["accid"]?.ToString() : "",
                Nickname = data.ContainsKey("昵称") ? data["昵称"]?.ToString() :
                           data.ContainsKey("nick") ? data["nick"]?.ToString() : "",
                GroupCard = data.ContainsKey("群名片") ? data["群名片"]?.ToString() : "",
                MemberType = data.ContainsKey("type") ? Convert.ToInt32(data["type"]) :
                             data.ContainsKey("角色") ? ParseMemberRole(data["角色"]?.ToString()) : MEMBER_TYPE_NORMAL
            };
        }

        /// <summary>
        /// 解析成员角色
        /// </summary>
        private int ParseMemberRole(string role)
        {
            if (string.IsNullOrEmpty(role)) return MEMBER_TYPE_NORMAL;
            
            switch (role)
            {
                case "群主": return MEMBER_TYPE_OWNER;
                case "管理员": return MEMBER_TYPE_ADMIN;
                default: return MEMBER_TYPE_NORMAL;
            }
        }

        /// <summary>
        /// 获取成员角色名称
        /// </summary>
        public static string GetMemberTypeName(int memberType)
        {
            switch (memberType)
            {
                case MEMBER_TYPE_OWNER: return "群主";
                case MEMBER_TYPE_ADMIN: return "管理员";
                default: return "成员";
            }
        }

        private void Log(string message)
        {
            Logger.Info($"[GroupMember] {message}");
            OnLog?.Invoke(message);
        }

        #endregion
    }

    #region 数据模型

    /// <summary>
    /// 群信息
    /// </summary>
    public class GroupInfo
    {
        /// <summary>旺商聊群号</summary>
        public string WslGroupId { get; set; }

        /// <summary>NIM tid</summary>
        public string NimTid { get; set; }

        /// <summary>群名称</summary>
        public string Name { get; set; }

        /// <summary>群主ID</summary>
        public string OwnerId { get; set; }

        /// <summary>成员数量</summary>
        public int MemberCount { get; set; }

        /// <summary>创建时间戳</summary>
        public long CreateTime { get; set; }

        /// <summary>群公告</summary>
        public string Announcement { get; set; }
    }

    /// <summary>
    /// 群成员
    /// </summary>
    public class GroupMember
    {
        /// <summary>旺商聊号</summary>
        public string WslId { get; set; }

        /// <summary>NIM accid</summary>
        public string NimAccid { get; set; }

        /// <summary>昵称</summary>
        public string Nickname { get; set; }

        /// <summary>群名片</summary>
        public string GroupCard { get; set; }

        /// <summary>成员类型 (0=群主, 1=成员, 2=管理员)</summary>
        public int MemberType { get; set; }

        /// <summary>显示名称 (优先群名片，其次昵称)</summary>
        public string DisplayName => !string.IsNullOrEmpty(GroupCard) ? GroupCard : Nickname;

        /// <summary>是否为群主</summary>
        public bool IsOwner => MemberType == GroupMemberService.MEMBER_TYPE_OWNER;

        /// <summary>是否为管理员</summary>
        public bool IsAdmin => MemberType == GroupMemberService.MEMBER_TYPE_ADMIN;
    }

    /// <summary>
    /// 用户信息 (HTTP API返回)
    /// </summary>
    public class UserInfo
    {
        /// <summary>旺商聊号</summary>
        public string WslId { get; set; }

        /// <summary>NIM accid</summary>
        public string NimId { get; set; }

        /// <summary>昵称</summary>
        public string Nickname { get; set; }

        /// <summary>业务系统群ID</summary>
        public string GroupId { get; set; }
    }

    /// <summary>
    /// 自动收集的成员信息
    /// </summary>
    public class CollectedMember
    {
        /// <summary>旺商聊号</summary>
        public string WslId { get; set; }

        /// <summary>昵称</summary>
        public string Nickname { get; set; }

        /// <summary>首次发现时间</summary>
        public DateTime FirstSeen { get; set; }

        /// <summary>最后活跃时间</summary>
        public DateTime LastSeen { get; set; }

        /// <summary>消息数量</summary>
        public int MessageCount { get; set; }
    }

    #endregion
}
