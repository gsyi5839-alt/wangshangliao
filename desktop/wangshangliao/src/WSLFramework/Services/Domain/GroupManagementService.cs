using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WSLFramework.Utils;

namespace WSLFramework.Services
{
    /// <summary>
    /// 群管理服务 - 完全匹配ZCG的群管理功能
    /// 包含禁言、解禁、群名片修改、踢人等功能
    /// </summary>
    public class GroupManagementService
    {
        private static readonly Lazy<GroupManagementService> _lazy = 
            new Lazy<GroupManagementService>(() => new GroupManagementService());
        public static GroupManagementService Instance => _lazy.Value;
        
        // CDP桥接（用于执行群操作）
        private CDPBridge _cdpBridge;
        
        // 群禁言状态
        private readonly ConcurrentDictionary<string, bool> _groupMuteState;
        
        // 成员禁言状态
        private readonly ConcurrentDictionary<string, DateTime> _memberMuteUntil;
        
        // 配置
        public bool AutoMuteOnClose { get; set; } = true;   // 封盘时自动禁言
        public bool AutoUnmuteOnOpen { get; set; } = true;  // 开盘时自动解禁
        public bool AutoKickOnBadWords { get; set; } = false; // 发送违禁词自动踢人
        public int MuteDuration { get; set; } = 0;          // 默认禁言时长（0=永久）
        
        // 事件
        public event Action<string> OnLog;
        public event Action<string, bool> OnGroupMuteChanged;  // groupId, isMuted
        public event Action<string, string, bool> OnMemberMuteChanged;  // groupId, memberId, isMuted
        public event Action<string, string> OnMemberKicked;  // groupId, memberId
        
        private GroupManagementService()
        {
            _groupMuteState = new ConcurrentDictionary<string, bool>();
            _memberMuteUntil = new ConcurrentDictionary<string, DateTime>();
        }
        
        /// <summary>
        /// 初始化CDP桥接
        /// </summary>
        public void Initialize(CDPBridge cdpBridge)
        {
            _cdpBridge = cdpBridge;
        }
        
        #region 全体禁言
        
        /// <summary>
        /// 设置全体禁言 - 匹配ZCG的NOTIFY_TYPE_GROUP_MUTE_1/0
        /// </summary>
        public async Task<bool> SetGroupMuteAsync(string groupId, bool mute)
        {
            if (_cdpBridge == null || !_cdpBridge.IsConnected)
            {
                Log($"CDP未连接，无法设置群禁言");
                return false;
            }
            
            try
            {
                Log($"设置群禁言: groupId={groupId}, mute={mute}");
                
                var result = await _cdpBridge.MuteAllAsync(groupId, mute);
                
                if (result)
                {
                    _groupMuteState[groupId] = mute;
                    OnGroupMuteChanged?.Invoke(groupId, mute);
                    Log($"群禁言{(mute ? "开启" : "关闭")}成功: {groupId}");
                }
                else
                {
                    Log($"群禁言操作失败: {groupId}");
                }
                
                return result;
            }
            catch (Exception ex)
            {
                Log($"设置群禁言异常: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 开启全体禁言（封盘时调用）
        /// </summary>
        public async Task<bool> MuteGroupAsync(string groupId)
        {
            return await SetGroupMuteAsync(groupId, true);
        }
        
        /// <summary>
        /// 关闭全体禁言（开盘时调用）
        /// </summary>
        public async Task<bool> UnmuteGroupAsync(string groupId)
        {
            return await SetGroupMuteAsync(groupId, false);
        }
        
        /// <summary>
        /// 获取群禁言状态
        /// </summary>
        public bool IsGroupMuted(string groupId)
        {
            return _groupMuteState.TryGetValue(groupId, out var muted) && muted;
        }
        
        #endregion
        
        #region 个人禁言
        
        /// <summary>
        /// 禁言成员
        /// </summary>
        public async Task<bool> MuteMemberAsync(string groupId, string memberId, int duration = 0)
        {
            if (_cdpBridge == null || !_cdpBridge.IsConnected)
            {
                Log($"CDP未连接，无法禁言成员");
                return false;
            }
            
            try
            {
                Log($"禁言成员: groupId={groupId}, memberId={memberId}, duration={duration}");
                
                var result = await _cdpBridge.MuteMemberAsync(groupId, memberId, duration);
                
                if (result)
                {
                    var key = $"{groupId}_{memberId}";
                    if (duration > 0)
                    {
                        _memberMuteUntil[key] = DateTime.Now.AddSeconds(duration);
                    }
                    else
                    {
                        _memberMuteUntil[key] = DateTime.MaxValue;
                    }
                    OnMemberMuteChanged?.Invoke(groupId, memberId, true);
                    Log($"禁言成员成功: {memberId}");
                }
                
                return result;
            }
            catch (Exception ex)
            {
                Log($"禁言成员异常: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 解除成员禁言
        /// </summary>
        public async Task<bool> UnmuteMemberAsync(string groupId, string memberId)
        {
            if (_cdpBridge == null || !_cdpBridge.IsConnected)
            {
                Log($"CDP未连接，无法解禁成员");
                return false;
            }
            
            try
            {
                Log($"解禁成员: groupId={groupId}, memberId={memberId}");
                
                var result = await _cdpBridge.UnmuteMemberAsync(groupId, memberId);
                
                if (result)
                {
                    var key = $"{groupId}_{memberId}";
                    _memberMuteUntil.TryRemove(key, out _);
                    OnMemberMuteChanged?.Invoke(groupId, memberId, false);
                    Log($"解禁成员成功: {memberId}");
                }
                
                return result;
            }
            catch (Exception ex)
            {
                Log($"解禁成员异常: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 检查成员是否被禁言
        /// </summary>
        public bool IsMemberMuted(string groupId, string memberId)
        {
            var key = $"{groupId}_{memberId}";
            if (_memberMuteUntil.TryGetValue(key, out var until))
            {
                return DateTime.Now < until;
            }
            return false;
        }
        
        #endregion
        
        #region 群名片
        
        /// <summary>
        /// 修改群名片
        /// </summary>
        public async Task<bool> UpdateMemberCardAsync(string groupId, string memberId, string newCard)
        {
            if (_cdpBridge == null || !_cdpBridge.IsConnected)
            {
                Log($"CDP未连接，无法修改群名片");
                return false;
            }
            
            try
            {
                Log($"修改群名片: groupId={groupId}, memberId={memberId}, newCard={newCard}");
                
                var result = await _cdpBridge.UpdateMemberCardAsync(groupId, memberId, newCard);
                
                if (result)
                {
                    Log($"修改群名片成功: {memberId} -> {newCard}");
                }
                
                return result;
            }
            catch (Exception ex)
            {
                Log($"修改群名片异常: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 自动修改群名片（根据规则）
        /// 格式: 昵称(短ID)
        /// </summary>
        public async Task<bool> AutoUpdateMemberCardAsync(string groupId, string memberId, string nickname, string shortId)
        {
            var newCard = $"{nickname}({shortId})";
            return await UpdateMemberCardAsync(groupId, memberId, newCard);
        }
        
        #endregion
        
        #region 踢人
        
        /// <summary>
        /// 踢出成员
        /// </summary>
        public async Task<bool> KickMemberAsync(string groupId, string memberId, string reason = null)
        {
            if (_cdpBridge == null || !_cdpBridge.IsConnected)
            {
                Log($"CDP未连接，无法踢出成员");
                return false;
            }
            
            try
            {
                Log($"踢出成员: groupId={groupId}, memberId={memberId}, reason={reason}");
                
                var result = await _cdpBridge.KickMemberAsync(groupId, memberId);
                
                if (result)
                {
                    OnMemberKicked?.Invoke(groupId, memberId);
                    Log($"踢出成员成功: {memberId}");
                }
                
                return result;
            }
            catch (Exception ex)
            {
                Log($"踢出成员异常: {ex.Message}");
                return false;
            }
        }
        
        #endregion
        
        #region 与开奖周期集成
        
        /// <summary>
        /// 封盘时自动禁言
        /// </summary>
        public async Task OnBettingClosedAsync(string groupId)
        {
            if (AutoMuteOnClose)
            {
                await MuteGroupAsync(groupId);
            }
        }
        
        /// <summary>
        /// 开盘时自动解禁
        /// </summary>
        public async Task OnBettingOpenedAsync(string groupId)
        {
            if (AutoUnmuteOnOpen)
            {
                await UnmuteGroupAsync(groupId);
            }
        }
        
        /// <summary>
        /// 处理NIM群通知消息
        /// 根据旺商聊深度连接协议第十三节
        /// NOTIFY_TYPE_GROUP_MUTE_1 = 禁言开启
        /// NOTIFY_TYPE_GROUP_MUTE_0 = 禁言解除
        /// </summary>
        public void HandleGroupNotification(string notifyType, string groupId, dynamic data)
        {
            try
            {
                if (notifyType == "NOTIFY_TYPE_GROUP_MUTE_1")
                {
                    _groupMuteState[groupId] = true;
                    OnGroupMuteChanged?.Invoke(groupId, true);
                    Log($"收到群禁言通知: {groupId}");
                }
                else if (notifyType == "NOTIFY_TYPE_GROUP_MUTE_0")
                {
                    _groupMuteState[groupId] = false;
                    OnGroupMuteChanged?.Invoke(groupId, false);
                    Log($"收到群解禁通知: {groupId}");
                }
            }
            catch (Exception ex)
            {
                Log($"处理群通知异常: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 处理NIM群通知消息 - 带team_info解析
        /// 根据旺商聊深度连接协议第十三节
        /// team_info: { mute_all: 0/1, mute_type: 0/1, tid: "群NIM ID", update_timetag: 时间戳 }
        /// </summary>
        public void HandleGroupNotificationWithTeamInfo(string groupId, Dictionary<string, object> teamInfo)
        {
            try
            {
                if (teamInfo == null || !teamInfo.ContainsKey("mute_all"))
                {
                    Log($"team_info为空或无效");
                    return;
                }
                
                var muteAll = Convert.ToInt32(teamInfo["mute_all"]);
                var muteType = teamInfo.ContainsKey("mute_type") ? Convert.ToInt32(teamInfo["mute_type"]) : 0;
                var tid = teamInfo.ContainsKey("tid") ? teamInfo["tid"]?.ToString() : groupId;
                var updateTimetag = teamInfo.ContainsKey("update_timetag") ? Convert.ToInt64(teamInfo["update_timetag"]) : 0;
                
                // 更新禁言状态
                var isMuted = muteAll == 1;
                _groupMuteState[groupId] = isMuted;
                OnGroupMuteChanged?.Invoke(groupId, isMuted);
                
                Log($"处理群通知: groupId={groupId}, tid={tid}, mute_all={muteAll}, mute_type={muteType}, update_timetag={updateTimetag}");
            }
            catch (Exception ex)
            {
                Log($"处理群通知异常: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 从原始JSON消息解析禁言通知
        /// 根据旺商聊深度连接协议
        /// </summary>
        public void ParseAndHandleMuteNotification(string rawJson, string groupId)
        {
            try
            {
                if (string.IsNullOrEmpty(rawJson))
                {
                    return;
                }
                
                // 尝试解析JSON中的team_info
                // 格式: {"data":{"team_info":{"mute_all":1,"mute_type":1,"tid":"40821608989","update_timetag":1768112829565}}}
                var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
                var json = serializer.Deserialize<Dictionary<string, object>>(rawJson);
                
                if (json != null && json.ContainsKey("data"))
                {
                    var data = json["data"] as Dictionary<string, object>;
                    if (data != null && data.ContainsKey("team_info"))
                    {
                        var teamInfo = data["team_info"] as Dictionary<string, object>;
                        if (teamInfo != null)
                        {
                            HandleGroupNotificationWithTeamInfo(groupId, teamInfo);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"解析禁言通知异常: {ex.Message}");
            }
        }
        
        #endregion
        
        #region 获取群成员
        
        /// <summary>
        /// 获取群成员列表
        /// </summary>
        public async Task<List<GroupMemberInfo>> GetGroupMembersAsync(string groupId)
        {
            if (_cdpBridge == null || !_cdpBridge.IsConnected)
            {
                Log($"CDP未连接，无法获取群成员");
                return new List<GroupMemberInfo>();
            }
            
            try
            {
                var members = await _cdpBridge.GetTeamMembersAsync(groupId);
                var result = new List<GroupMemberInfo>();
                
                foreach (var m in members)
                {
                    result.Add(new GroupMemberInfo
                    {
                        MemberId = m.accid,
                        Nickname = m.nickname,
                        Card = m.card,
                        Role = (MemberRole)m.type,
                        IsMuted = m.muted || IsMemberMuted(groupId, m.accid)
                    });
                }
                
                return result;
            }
            catch (Exception ex)
            {
                Log($"获取群成员异常: {ex.Message}");
                return new List<GroupMemberInfo>();
            }
        }
        
        #endregion
        
        private void Log(string message)
        {
            Logger.Info($"[GroupMgmt] {message}");
            OnLog?.Invoke(message);
        }
    }
    
    /// <summary>
    /// 群成员信息
    /// </summary>
    public class GroupMemberInfo
    {
        public string MemberId { get; set; }
        public string Nickname { get; set; }
        public string Card { get; set; }
        public MemberRole Role { get; set; }
        public bool IsMuted { get; set; }
        
        /// <summary>
        /// 获取显示名称（优先群名片）
        /// </summary>
        public string DisplayName => !string.IsNullOrEmpty(Card) ? Card : Nickname;
    }
    
    /// <summary>
    /// 成员角色
    /// </summary>
    public enum MemberRole
    {
        Member = 0,    // 普通成员
        Admin = 1,     // 管理员
        Owner = 2      // 群主
    }
}
