using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;
using WSLFramework.Utils;

namespace WSLFramework.Services
{
    /// <summary>
    /// 旺商聊本地存储读取服务
    /// 从客户端本地 config.json 读取已登录用户信息和群成员数据
    /// </summary>
    public class WangShangLiaoLocalStorage
    {
        #region 单例模式

        private static readonly Lazy<WangShangLiaoLocalStorage> _instance =
            new Lazy<WangShangLiaoLocalStorage>(() => new WangShangLiaoLocalStorage());

        public static WangShangLiaoLocalStorage Instance => _instance.Value;

        #endregion

        #region 私有字段

        private readonly JavaScriptSerializer _serializer;
        private string _configPath;
        private Dictionary<string, object> _cachedConfig;
        private DateTime _lastLoadTime;
        private readonly TimeSpan _cacheExpiry = TimeSpan.FromSeconds(30);

        #endregion

        #region 事件

        public event Action<string> OnLog;

        #endregion

        #region 构造函数

        private WangShangLiaoLocalStorage()
        {
            _serializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
            _configPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "wangshangliao", "config.json"
            );
        }

        #endregion

        #region 配置路径

        /// <summary>
        /// 设置自定义配置路径
        /// </summary>
        public void SetConfigPath(string path)
        {
            _configPath = path;
            _cachedConfig = null;
        }

        /// <summary>
        /// 获取当前配置文件路径
        /// </summary>
        public string GetConfigPath() => _configPath;

        /// <summary>
        /// 检查配置文件是否存在
        /// </summary>
        public bool ConfigExists() => File.Exists(_configPath);

        #endregion

        #region 登录信息读取 (WangShangLiaoTokenReader)

        /// <summary>
        /// 读取已登录用户的Token信息
        /// </summary>
        public LoginInfo ReadLoginInfo()
        {
            try
            {
                var config = LoadConfig();
                if (config == null) return null;

                if (!config.TryGetValue("production_account", out var accountsObj))
                {
                    Log("未找到 production_account 字段");
                    return null;
                }

                // 处理不同的数组类型 (ArrayList 或 object[])
                System.Collections.IList accounts = null;
                if (accountsObj is object[] objArray)
                {
                    accounts = objArray;
                }
                else if (accountsObj is System.Collections.ArrayList arrayList)
                {
                    accounts = arrayList;
                }
                
                if (accounts == null || accounts.Count == 0)
                {
                    Log($"production_account 为空或类型不支持: {accountsObj?.GetType().Name}");
                    return null;
                }

                var firstAccount = accounts[0] as Dictionary<string, object>;
                if (firstAccount == null)
                {
                    Log($"无法解析第一个账号，类型: {accounts[0]?.GetType().Name}");
                    return null;
                }

                if (!firstAccount.TryGetValue("userInfo", out var userInfoObj))
                {
                    Log("未找到 userInfo 字段");
                    return null;
                }

                var userInfo = userInfoObj as Dictionary<string, object>;
                if (userInfo == null)
                {
                    Log($"无法解析 userInfo，类型: {userInfoObj?.GetType().Name}");
                    return null;
                }

                var loginInfo = new LoginInfo
                {
                    Uid = GetLong(userInfo, "uid"),
                    NickName = GetString(userInfo, "nickName"),
                    NimId = GetLong(userInfo, "nimId"),
                    NimToken = GetString(userInfo, "nimToken"),
                    AccountId = GetLong(userInfo, "accountId"),
                    Token = GetString(userInfo, "token"),
                    AccountState = GetString(userInfo, "accountState")
                };

                Log($"成功读取登录信息: UID={loginInfo.Uid}, NimId={loginInfo.NimId}");
                return loginInfo;
            }
            catch (Exception ex)
            {
                Log($"读取登录信息失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 检查用户是否已登录
        /// </summary>
        public bool IsLoggedIn()
        {
            var info = ReadLoginInfo();
            return info != null && !string.IsNullOrEmpty(info.Token);
        }

        #endregion

        #region 群成员读取 (WangShangLiaoGroupReader)

        /// <summary>
        /// 读取群成员列表
        /// </summary>
        /// <param name="nimGroupId">NIM群ID</param>
        public List<LocalGroupMember> GetGroupMembers(long nimGroupId)
        {
            var result = new List<LocalGroupMember>();
            try
            {
                var config = LoadConfig();
                if (config == null) return result;

                var key = $"gMembers_{nimGroupId}";
                if (!config.TryGetValue(key, out var groupDataObj))
                {
                    Log($"未找到群成员数据: {key}");
                    return result;
                }

                var groupData = groupDataObj as Dictionary<string, object>;
                if (groupData == null)
                {
                    Log($"无法解析群成员数据: {key}");
                    return result;
                }

                if (!groupData.TryGetValue("groupMemberInfo", out var membersObj))
                {
                    Log("未找到 groupMemberInfo 字段");
                    return result;
                }

                var members = membersObj as object[];
                if (members == null)
                {
                    Log("groupMemberInfo 不是数组");
                    return result;
                }

                foreach (var memberObj in members)
                {
                    var member = memberObj as Dictionary<string, object>;
                    if (member == null) continue;

                    result.Add(new LocalGroupMember
                    {
                        UserId = GetLong(member, "userId"),
                        NimId = GetLong(member, "nimId"),
                        UserNick = GetString(member, "userNick"),
                        GroupMemberNick = GetString(member, "groupMemberNick"),
                        Role = ParseGroupRole(GetString(member, "groupRole")),
                        Avatar = GetString(member, "userAvatar"),
                        AccountState = GetString(member, "accountState"),
                        VipLevel = GetString(member, "vipLevel"),
                        InitialPinyin = GetString(member, "initialPinyin")
                    });
                }

                Log($"成功读取群 {nimGroupId} 成员: {result.Count} 人");
            }
            catch (Exception ex)
            {
                Log($"读取群成员失败: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// 通过群号查找NIM群ID
        /// </summary>
        /// <param name="groupAccount">旺商聊群号</param>
        public long? FindNimGroupId(string groupAccount)
        {
            try
            {
                if (!File.Exists(_configPath))
                {
                    Log("配置文件不存在");
                    return null;
                }

                var json = File.ReadAllText(_configPath);

                // 搜索 "groupAccount": "{群号}" 或 "groupAccount":"{群号}"
                var searchPatterns = new[]
                {
                    $"\"groupAccount\": \"{groupAccount}\"",
                    $"\"groupAccount\":\"{groupAccount}\""
                };

                int idx = -1;
                foreach (var pattern in searchPatterns)
                {
                    idx = json.IndexOf(pattern, StringComparison.Ordinal);
                    if (idx >= 0) break;
                }

                if (idx < 0)
                {
                    Log($"未找到群号 {groupAccount}");
                    return null;
                }

                // 向前搜索 gMembers_ 键
                var prefix = "\"gMembers_";
                var startIdx = json.LastIndexOf(prefix, idx, StringComparison.Ordinal);
                if (startIdx < 0)
                {
                    Log($"未找到对应的 gMembers_ 键");
                    return null;
                }

                startIdx += prefix.Length;
                var endIdx = json.IndexOf("\"", startIdx, StringComparison.Ordinal);
                if (endIdx < 0)
                {
                    Log("无法解析 NIM群ID");
                    return null;
                }

                var nimIdStr = json.Substring(startIdx, endIdx - startIdx);
                if (long.TryParse(nimIdStr, out var nimId))
                {
                    Log($"群号 {groupAccount} -> NIM群ID {nimId}");
                    return nimId;
                }

                Log($"无效的 NIM群ID: {nimIdStr}");
                return null;
            }
            catch (Exception ex)
            {
                Log($"查找NIM群ID失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 获取所有已缓存的群ID列表
        /// </summary>
        public List<long> GetAllCachedGroupIds()
        {
            var result = new List<long>();
            try
            {
                var config = LoadConfig();
                if (config == null) return result;

                foreach (var key in config.Keys)
                {
                    if (key.StartsWith("gMembers_"))
                    {
                        var nimIdStr = key.Substring("gMembers_".Length);
                        if (long.TryParse(nimIdStr, out var nimId))
                        {
                            result.Add(nimId);
                        }
                    }
                }

                Log($"找到 {result.Count} 个缓存的群");
            }
            catch (Exception ex)
            {
                Log($"获取群列表失败: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// 获取群公告
        /// </summary>
        public List<string> GetGroupNotices(long nimGroupId)
        {
            var result = new List<string>();
            try
            {
                var config = LoadConfig();
                if (config == null) return result;

                var key = $"gNotices_{nimGroupId}";
                if (!config.TryGetValue(key, out var noticesObj))
                {
                    return result;
                }

                var notices = noticesObj as object[];
                if (notices != null)
                {
                    foreach (var notice in notices)
                    {
                        var noticeDict = notice as Dictionary<string, object>;
                        if (noticeDict != null && noticeDict.TryGetValue("content", out var content))
                        {
                            result.Add(content?.ToString() ?? "");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"获取群公告失败: {ex.Message}");
            }

            return result;
        }

        #endregion

        #region 群成员统计

        /// <summary>
        /// 获取群成员统计
        /// </summary>
        public GroupMemberStats GetGroupStats(long nimGroupId)
        {
            var members = GetGroupMembers(nimGroupId);
            return new GroupMemberStats
            {
                TotalCount = members.Count,
                OwnerCount = members.Count(m => m.Role == LocalGroupRole.Owner),
                AdminCount = members.Count(m => m.Role == LocalGroupRole.Admin),
                MemberCount = members.Count(m => m.Role == LocalGroupRole.Member)
            };
        }

        /// <summary>
        /// 获取群主信息
        /// </summary>
        public LocalGroupMember GetGroupOwner(long nimGroupId)
        {
            var members = GetGroupMembers(nimGroupId);
            return members.FirstOrDefault(m => m.Role == LocalGroupRole.Owner);
        }

        /// <summary>
        /// 获取管理员列表
        /// </summary>
        public List<LocalGroupMember> GetGroupAdmins(long nimGroupId)
        {
            var members = GetGroupMembers(nimGroupId);
            return members.Where(m => m.Role == LocalGroupRole.Admin).ToList();
        }

        /// <summary>
        /// 获取普通成员列表（排除群主和管理员）
        /// </summary>
        public List<LocalGroupMember> GetNormalMembers(long nimGroupId)
        {
            var members = GetGroupMembers(nimGroupId);
            return members.Where(m => m.Role == LocalGroupRole.Member).ToList();
        }

        /// <summary>
        /// 根据旺旺号查找成员
        /// </summary>
        public LocalGroupMember FindMemberByUserId(long nimGroupId, long userId)
        {
            var members = GetGroupMembers(nimGroupId);
            return members.FirstOrDefault(m => m.UserId == userId);
        }

        /// <summary>
        /// 根据NIM ID查找成员
        /// </summary>
        public LocalGroupMember FindMemberByNimId(long nimGroupId, long nimId)
        {
            var members = GetGroupMembers(nimGroupId);
            return members.FirstOrDefault(m => m.NimId == nimId);
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 加载配置文件（带缓存）
        /// </summary>
        private Dictionary<string, object> LoadConfig()
        {
            if (_cachedConfig != null && DateTime.Now - _lastLoadTime < _cacheExpiry)
            {
                return _cachedConfig;
            }

            if (!File.Exists(_configPath))
            {
                Log($"配置文件不存在: {_configPath}");
                return null;
            }

            try
            {
                var json = File.ReadAllText(_configPath);
                _cachedConfig = _serializer.Deserialize<Dictionary<string, object>>(json);
                _lastLoadTime = DateTime.Now;
                return _cachedConfig;
            }
            catch (Exception ex)
            {
                Log($"加载配置文件失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 强制刷新缓存
        /// </summary>
        public void RefreshCache()
        {
            _cachedConfig = null;
        }

        private static long GetLong(Dictionary<string, object> dict, string key)
        {
            if (dict.TryGetValue(key, out var value))
            {
                if (value is long l) return l;
                if (value is int i) return i;
                if (value is decimal d) return (long)d;
                if (value is double db) return (long)db;
                if (long.TryParse(value?.ToString(), out var parsed)) return parsed;
            }
            return 0;
        }

        private static string GetString(Dictionary<string, object> dict, string key)
        {
            if (dict.TryGetValue(key, out var value))
            {
                return value?.ToString() ?? "";
            }
            return "";
        }

        private static LocalGroupRole ParseGroupRole(string role)
        {
            switch (role)
            {
                case "GROUP_ROLE_OWNER": return LocalGroupRole.Owner;
                case "GROUP_ROLE_ADMIN": return LocalGroupRole.Admin;
                default: return LocalGroupRole.Member;
            }
        }

        private void Log(string message)
        {
            Logger.Info($"[LocalStorage] {message}");
            OnLog?.Invoke(message);
        }

        #endregion
    }

    #region 数据模型

    /// <summary>
    /// 登录信息
    /// </summary>
    public class LoginInfo
    {
        /// <summary>旺旺号</summary>
        public long Uid { get; set; }

        /// <summary>昵称</summary>
        public string NickName { get; set; }

        /// <summary>NIM ID</summary>
        public long NimId { get; set; }

        /// <summary>NIM Token (用于IM连接)</summary>
        public string NimToken { get; set; }

        /// <summary>账号ID</summary>
        public long AccountId { get; set; }

        /// <summary>API Token</summary>
        public string Token { get; set; }

        /// <summary>账号状态</summary>
        public string AccountState { get; set; }

        /// <summary>是否有效</summary>
        public bool IsValid => Uid > 0 && !string.IsNullOrEmpty(Token);
    }

    /// <summary>
    /// 本地群成员信息
    /// </summary>
    public class LocalGroupMember
    {
        /// <summary>旺旺号</summary>
        public long UserId { get; set; }

        /// <summary>NIM ID</summary>
        public long NimId { get; set; }

        /// <summary>用户昵称</summary>
        public string UserNick { get; set; }

        /// <summary>群内昵称</summary>
        public string GroupMemberNick { get; set; }

        /// <summary>角色</summary>
        public LocalGroupRole Role { get; set; }

        /// <summary>头像ID</summary>
        public string Avatar { get; set; }

        /// <summary>账号状态</summary>
        public string AccountState { get; set; }

        /// <summary>VIP等级</summary>
        public string VipLevel { get; set; }

        /// <summary>昵称首字母</summary>
        public string InitialPinyin { get; set; }

        /// <summary>显示名称 (优先群内昵称)</summary>
        public string DisplayName => !string.IsNullOrEmpty(GroupMemberNick) ? GroupMemberNick : UserNick;

        /// <summary>头像URL</summary>
        public string AvatarUrl => !string.IsNullOrEmpty(Avatar) 
            ? $"https://yiyong-static.nosdn.127.net/avatar/{Avatar}" 
            : "";

        /// <summary>是否为群主</summary>
        public bool IsOwner => Role == LocalGroupRole.Owner;

        /// <summary>是否为管理员</summary>
        public bool IsAdmin => Role == LocalGroupRole.Admin;

        /// <summary>是否为管理层（群主或管理员）</summary>
        public bool IsManager => Role == LocalGroupRole.Owner || Role == LocalGroupRole.Admin;
    }

    /// <summary>
    /// 群成员角色
    /// </summary>
    public enum LocalGroupRole
    {
        /// <summary>群主</summary>
        Owner,

        /// <summary>管理员</summary>
        Admin,

        /// <summary>普通成员</summary>
        Member
    }

    /// <summary>
    /// 群成员统计
    /// </summary>
    public class GroupMemberStats
    {
        /// <summary>总人数</summary>
        public int TotalCount { get; set; }

        /// <summary>群主数量</summary>
        public int OwnerCount { get; set; }

        /// <summary>管理员数量</summary>
        public int AdminCount { get; set; }

        /// <summary>普通成员数量</summary>
        public int MemberCount { get; set; }
    }

    #endregion

    #region 静态便捷类

    /// <summary>
    /// 旺商聊群成员读取器 (静态便捷类)
    /// </summary>
    public static class WangShangLiaoGroupReader
    {
        /// <summary>
        /// 读取群成员列表
        /// </summary>
        public static List<LocalGroupMember> GetGroupMembers(long nimGroupId)
        {
            return WangShangLiaoLocalStorage.Instance.GetGroupMembers(nimGroupId);
        }

        /// <summary>
        /// 通过群号查找NIM群ID
        /// </summary>
        public static long? FindNimGroupId(string groupAccount)
        {
            return WangShangLiaoLocalStorage.Instance.FindNimGroupId(groupAccount);
        }

        /// <summary>
        /// 获取群主信息
        /// </summary>
        public static LocalGroupMember GetGroupOwner(long nimGroupId)
        {
            return WangShangLiaoLocalStorage.Instance.GetGroupOwner(nimGroupId);
        }

        /// <summary>
        /// 获取管理员列表
        /// </summary>
        public static List<LocalGroupMember> GetGroupAdmins(long nimGroupId)
        {
            return WangShangLiaoLocalStorage.Instance.GetGroupAdmins(nimGroupId);
        }

        /// <summary>
        /// 获取普通成员列表
        /// </summary>
        public static List<LocalGroupMember> GetNormalMembers(long nimGroupId)
        {
            return WangShangLiaoLocalStorage.Instance.GetNormalMembers(nimGroupId);
        }

        /// <summary>
        /// 获取群成员统计
        /// </summary>
        public static GroupMemberStats GetGroupStats(long nimGroupId)
        {
            return WangShangLiaoLocalStorage.Instance.GetGroupStats(nimGroupId);
        }
    }

    /// <summary>
    /// 旺商聊Token读取器 (静态便捷类)
    /// </summary>
    public static class WangShangLiaoTokenReader
    {
        /// <summary>
        /// 读取已登录用户的Token信息
        /// </summary>
        public static LoginInfo ReadFromLocalStorage()
        {
            return WangShangLiaoLocalStorage.Instance.ReadLoginInfo();
        }

        /// <summary>
        /// 检查用户是否已登录
        /// </summary>
        public static bool IsLoggedIn()
        {
            return WangShangLiaoLocalStorage.Instance.IsLoggedIn();
        }
    }

    #endregion
}
