using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WSLFramework.Utils;

namespace WSLFramework.Services
{
    /// <summary>
    /// 旺商聊字段映射服务
    /// 统一旺商聊字段与主框架字段的映射关系
    /// 
    /// 字段映射关系：
    /// ┌─────────────────────────────────────────────────────────────────────┐
    /// │  旺商聊字段              主框架字段              用途                │
    /// ├─────────────────────────────────────────────────────────────────────┤
    /// │  userInfo.uid           PlayerId               玩家唯一标识         │
    /// │  userInfo.wwid          PlayerId               玩家唯一标识(别名)   │
    /// │  userInfo.nimId         NimAccountId           NIM账号(踢人/禁言)   │
    /// │  userInfo.nickName      PlayerNick             玩家昵称             │
    /// │  group.groupAccount     GroupId                群号(用户看到的)     │
    /// │  group.groupId          TeamId/InternalId      群内部ID(NIM操作)    │
    /// │  group.groupCloudId     NimTeamId              云端群ID             │
    /// │  member.userId          PlayerId               成员UID              │
    /// │  member.nimId           NimAccountId           成员NIM账号          │
    /// │  member.groupMemberNick PlayerNick             成员群昵称           │
    /// │  member.groupRole       Role                   成员角色             │
    /// └─────────────────────────────────────────────────────────────────────┘
    /// </summary>
    public class WslFieldMapper
    {
        private static WslFieldMapper _instance;
        public static WslFieldMapper Instance => _instance ?? (_instance = new WslFieldMapper());

        // 缓存
        private readonly Dictionary<string, string> _uidToNimIdCache = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _nimIdToUidCache = new Dictionary<string, string>();
        private readonly Dictionary<string, WslGroupMemberInfo> _memberCache = new Dictionary<string, WslGroupMemberInfo>();
        private readonly object _lock = new object();

        public event Action<string> OnLog;

        private WslFieldMapper() { }

        private void Log(string message) => OnLog?.Invoke($"[FieldMapper] {message}");

        /// <summary>
        /// 将 UID 转换为 NIM AccountId（用于踢人/禁言等操作）
        /// </summary>
        public async Task<string> GetNimIdByUidAsync(string uid, string teamInternalId)
        {
            if (string.IsNullOrEmpty(uid)) return null;

            // 检查缓存
            lock (_lock)
            {
                if (_uidToNimIdCache.TryGetValue(uid, out var cachedNimId))
                    return cachedNimId;
            }

            // 从 CDP 获取群成员列表
            try
            {
                var members = await CDPService.Instance.GetGroupMembersAsync(teamInternalId);
                foreach (var m in members)
                {
                    lock (_lock)
                    {
                        var uidStr = m.UserId.ToString();
                        if (!string.IsNullOrEmpty(m.NimId))
                        {
                            _uidToNimIdCache[uidStr] = m.NimId;
                            _nimIdToUidCache[m.NimId] = uidStr;
                            _memberCache[$"{teamInternalId}_{uidStr}"] = m;
                        }

                        if (uidStr == uid)
                        {
                            return m.NimId;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"获取成员NimId失败: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// 将 NIM AccountId 转换为 UID
        /// </summary>
        public async Task<string> GetUidByNimIdAsync(string nimId, string teamInternalId)
        {
            if (string.IsNullOrEmpty(nimId)) return null;

            // 检查缓存
            lock (_lock)
            {
                if (_nimIdToUidCache.TryGetValue(nimId, out var cachedUid))
                    return cachedUid;
            }

            // 从 CDP 获取
            try
            {
                var members = await CDPService.Instance.GetGroupMembersAsync(teamInternalId);
                foreach (var m in members)
                {
                    lock (_lock)
                    {
                        var uidStr = m.UserId.ToString();
                        if (!string.IsNullOrEmpty(m.NimId))
                        {
                            _uidToNimIdCache[uidStr] = m.NimId;
                            _nimIdToUidCache[m.NimId] = uidStr;

                            if (m.NimId == nimId)
                            {
                                return uidStr;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"获取成员UID失败: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// 获取群成员详细信息
        /// </summary>
        public async Task<WslGroupMemberInfo> GetMemberInfoAsync(string uid, string teamInternalId)
        {
            var cacheKey = $"{teamInternalId}_{uid}";

            // 检查缓存
            lock (_lock)
            {
                if (_memberCache.TryGetValue(cacheKey, out var cached))
                    return cached;
            }

            // 从 CDP 获取
            try
            {
                var members = await CDPService.Instance.GetGroupMembersAsync(teamInternalId);
                foreach (var m in members)
                {
                    var memberUid = m.UserId.ToString();
                    var key = $"{teamInternalId}_{memberUid}";

                    lock (_lock)
                    {
                        _memberCache[key] = m;
                        if (!string.IsNullOrEmpty(m.NimId))
                        {
                            _uidToNimIdCache[memberUid] = m.NimId;
                            _nimIdToUidCache[m.NimId] = memberUid;
                        }

                        if (memberUid == uid)
                        {
                            return m;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"获取成员信息失败: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// 预加载群成员（建议在启动时调用）
        /// </summary>
        public async Task PreloadGroupMembersAsync(string teamInternalId)
        {
            try
            {
                Log($"预加载群 {teamInternalId} 成员...");
                var members = await CDPService.Instance.GetGroupMembersAsync(teamInternalId);

                lock (_lock)
                {
                    foreach (var m in members)
                    {
                        var uid = m.UserId.ToString();
                        var key = $"{teamInternalId}_{uid}";
                        _memberCache[key] = m;

                        if (!string.IsNullOrEmpty(m.NimId))
                        {
                            _uidToNimIdCache[uid] = m.NimId;
                            _nimIdToUidCache[m.NimId] = uid;
                        }
                    }
                }

                Log($"预加载完成: {members.Count} 个成员");
            }
            catch (Exception ex)
            {
                Log($"预加载失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 清除缓存
        /// </summary>
        public void ClearCache()
        {
            lock (_lock)
            {
                _uidToNimIdCache.Clear();
                _nimIdToUidCache.Clear();
                _memberCache.Clear();
            }
        }

        /// <summary>
        /// 获取群角色文本
        /// </summary>
        public static string GetRoleText(string groupRole)
        {
            switch (groupRole)
            {
                case "GROUP_ROLE_OWNER": return "群主";
                case "GROUP_ROLE_ADMIN": return "管理员";
                case "GROUP_ROLE_MEMBER": return "成员";
                default: return groupRole ?? "成员";
            }
        }

        /// <summary>
        /// 获取在线状态文本
        /// </summary>
        public static string GetOnlineStateText(string onlineState)
        {
            switch (onlineState)
            {
                case "ONLINE_STATE_ONLINE": return "在线";
                case "ONLINE_STATE_OFFLINE": return "离线";
                case "ONLINE_STATE_BUSY": return "忙碌";
                default: return onlineState ?? "离线";
            }
        }
    }

    /// <summary>
    /// 统一的玩家信息模型 - 用于主框架交互
    /// </summary>
    public class UnifiedPlayerInfo
    {
        /// <summary>玩家ID (UID/WWID)</summary>
        public string PlayerId { get; set; }

        /// <summary>NIM账号ID (用于踢人/禁言)</summary>
        public string NimAccountId { get; set; }

        /// <summary>玩家昵称</summary>
        public string PlayerNick { get; set; }

        /// <summary>群内昵称</summary>
        public string GroupNick { get; set; }

        /// <summary>显示名称（优先群昵称）</summary>
        public string DisplayName => !string.IsNullOrEmpty(GroupNick) ? GroupNick : PlayerNick;

        /// <summary>群角色</summary>
        public string Role { get; set; }

        /// <summary>是否群主</summary>
        public bool IsOwner => Role == "GROUP_ROLE_OWNER";

        /// <summary>是否管理员</summary>
        public bool IsAdmin => Role == "GROUP_ROLE_ADMIN";

        /// <summary>头像</summary>
        public string Avatar { get; set; }

        /// <summary>VIP等级</summary>
        public string VipLevel { get; set; }

        /// <summary>账号状态</summary>
        public string AccountState { get; set; }

        /// <summary>从 WslGroupMemberInfo 创建</summary>
        public static UnifiedPlayerInfo FromMember(WslGroupMemberInfo member)
        {
            if (member == null) return null;
            return new UnifiedPlayerInfo
            {
                PlayerId = member.UserId.ToString(),
                NimAccountId = member.NimId,
                PlayerNick = member.UserNick,
                GroupNick = member.GroupMemberNick,
                Role = member.GroupRole,
                Avatar = member.UserAvatar,
                VipLevel = member.VipLevel,
                AccountState = member.AccountState
            };
        }

        /// <summary>从 WslFriendInfo 创建</summary>
        public static UnifiedPlayerInfo FromFriend(WslFriendInfo friend)
        {
            if (friend == null) return null;
            return new UnifiedPlayerInfo
            {
                PlayerId = friend.Uid.ToString(),
                NimAccountId = friend.NimId,
                PlayerNick = friend.Nickname,
                GroupNick = friend.MarkName,
                Avatar = friend.Avatar,
                VipLevel = friend.VipLevel
            };
        }

        /// <summary>从 UserId 和基本信息创建</summary>
        public static UnifiedPlayerInfo FromBasicInfo(string userId, string nimId, string nickname)
        {
            return new UnifiedPlayerInfo
            {
                PlayerId = userId,
                NimAccountId = nimId,
                PlayerNick = nickname
            };
        }
    }

    /// <summary>
    /// 统一的群信息模型
    /// </summary>
    public class UnifiedGroupInfo
    {
        /// <summary>群号（用户看到的，如 3962369093）</summary>
        public string GroupId { get; set; }

        /// <summary>群内部ID（NIM用，如 1176721）</summary>
        public string TeamId { get; set; }

        /// <summary>云端群ID</summary>
        public string NimTeamId { get; set; }

        /// <summary>群名称</summary>
        public string GroupName { get; set; }

        /// <summary>成员数量</summary>
        public int MemberCount { get; set; }

        /// <summary>我的角色</summary>
        public string MyRole { get; set; }

        /// <summary>我的群昵称</summary>
        public string MyNickName { get; set; }

        /// <summary>头像</summary>
        public string Avatar { get; set; }

        /// <summary>从 WslGroupInfo 创建</summary>
        public static UnifiedGroupInfo FromGroup(WslGroupInfo group)
        {
            if (group == null) return null;
            return new UnifiedGroupInfo
            {
                GroupId = group.GroupId,
                TeamId = group.InternalId,
                NimTeamId = group.CloudId,
                GroupName = group.Name,
                MemberCount = group.MemberNum,
                MyRole = group.MyRole,
                MyNickName = group.MyNickName,
                Avatar = group.Avatar
            };
        }
    }
}
