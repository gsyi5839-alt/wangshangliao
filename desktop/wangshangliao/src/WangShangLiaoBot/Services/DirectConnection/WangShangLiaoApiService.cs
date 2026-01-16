using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Web.Script.Serialization;
using System.Threading.Tasks;

namespace WangShangLiaoBot.Services.DirectConnection
{
    /// <summary>
    /// 旺商聊API服务 - 基于逆向分析的完整API端点
    /// 
    /// 这些API端点是从旺商聊Electron应用中提取的
    /// 可通过xclient.exe代理或直接HTTP调用
    /// </summary>
    public sealed class WangShangLiaoApiService
    {
        private static WangShangLiaoApiService _instance;
        public static WangShangLiaoApiService Instance => _instance ?? (_instance = new WangShangLiaoApiService());

        private readonly HttpClient _httpClient;
        private string _baseUrl;
        private string _token;
        private string _nimId;

        public event Action<string> OnLog;

        #region API端点定义 (从逆向分析获取)

        public static class ApiUrls
        {
            // 用户相关
            public const string UpdateSession = "/v1/user/update-session";
            public const string RefreshToken = "/v1/user/RefreshToken";
            public const string Login = "/v1/user/login";
            public const string Logout = "/v1/user/logout";
            public const string Register = "/v1/user/register";
            public const string GetAutoRepliesOnlineState = "/v1/user/get-auto-replies-online-state";

            // 验证相关
            public const string AnonSendCode = "/v1/verify/sms-anon";
            public const string SendCode = "/v1/verify/sms";
            public const string Verify = "/v1/verify/verify";

            // 设置相关
            public const string SetAvatar = "/v1/settings/avatar";
            public const string SetSelfNickName = "/v1/settings/self-nick-name";
            public const string QueryAppSettings = "/v1/settings/query-app-settings";
            public const string SetAutoReply = "/v1/settings/set-auto-reply";
            public const string GetSystemSetting = "/v1/settings/get-system-setting";
            public const string SetRingP2p = "/v1/settings/ring-p2p";
            public const string SetRingGroup = "/v1/settings/ring-group";
            public const string SetNotifyState = "/v1/settings/set-notify-state";
            public const string SetSessionTop = "/v1/settings/set-session-top";
            public const string SetAudioVideoChat = "/v1/settings/audio-video-chat";
            public const string ModifyPassword = "/v1/settings/password";
            public const string P2pSearch = "/v1/settings/p2p-search";
            public const string P2pVerify = "/v1/settings/p2p-verify";
            public const string GroupVerify = "/v1/settings/group-verify";
            public const string GetSensitiveWords = "/v1/settings/get-sensitive-words";
            public const string SetSessionHide = "/v1/settings/set-session-hide";
            public const string GetLineGroup = "/v1/settings/get-line-group";
            public const string GetAgreement = "/v1/settings/get-agreement";
            public const string GetSysCfg = "/v1/settings/get-sys-cfg";

            // 好友相关
            public const string GetFriendList = "/v1/friend/get-friend-list";
            public const string GetFriendPermission = "/v1/user/get-friend-permission";
            public const string SetFriendPermission = "/v1/user/set-friend-permission";
            public const string DelFriend = "/v1/friend/del-friend";
            public const string AddBlack = "/v1/friend/friend-black-setting";
            public const string GetBlackList = "/v1/friend/get-black-list";
            public const string SetBlack = "/v1/friend/friend-black-setting";
            public const string SetFriendRemark = "/v1/friend/friend-set-remark";
            public const string SearchConversation = "/v1/friend/search-conversation";
            public const string AddFriendApply = "/v1/friend/add-friend-apply";
            public const string GetFriendApplyList = "/v1/friend/friend-apply-list";
            public const string DealFriendApply = "/v1/friend/friend-apply-handler";
            public const string AddFriendClass = "/v1/friend/add-friend-class";
            public const string DelFriendClass = "/v1/friend/del-friend-class";
            public const string GetFriendClass = "/v1/friend/get-friend-class";
            public const string UpdateFriendClass = "/v1/friend/update-friend-class";
            public const string SetFriendClass = "/v1/friend/friend-set-class";
            public const string SetFriendTop = "/v1/friend/set-friend-top";
            public const string SetBackground = "/v1/friend/set-background";
            public const string SetFriendNotice = "/v1/friend/set-friend-notice";
            public const string GetFriendInfo = "/v1/friend/get-friend-info";

            // 群组相关 - 核心API
            public const string CreateGroup = "/v1/group/create";
            public const string GetGroupInfoLite = "/v1/group/GetGroupInfoLite";
            public const string SearchGroup = "/v1/group/SearchGroup";
            public const string DismissGroup = "/v1/group/group-dismiss";
            public const string GetTopNotice = "/v1/group/top-notice";
            public const string AddGroupNotice = "/v1/group/add-notice";
            public const string UpdateGroupNotice = "/v1/group/notice-opt";
            public const string DelGroupNotice = "/v1/group/notice-del";
            public const string GetNoticeList = "/v1/group/notice-list";
            public const string GetNoticeInfo = "/v1/group/notice-info";
            public const string GetNoticeReaderList = "/v1/group/notice-reader-list";
            public const string SetNoticeRead = "/v1/group/set-notice-read";
            public const string GetGroupInfo = "/v1/group/get-group-info";
            public const string AddGroupManage = "/v1/group/add-group-manage";
            public const string DelGroupManage = "/v1/group/del-group-manage";
            public const string AddGroupMember = "/v1/group/add-group-member";
            public const string RemoveGroupMember = "/v1/group/remove-group-member";
            public const string RemoveMemberInGroup = "/v1/group/remove-member-in-group";
            public const string RemoveManagerInGroup = "/v1/group/remove-manager-in-group";
            public const string GetGroupMembers = "/v1/group/get-group-members";
            public const string GetGroupMemberInfo = "/v1/group/get-group-member-info";
            public const string ApplyJoinGroup = "/v1/group/apply-join-group";
            public const string GetGroupApplyList = "/v1/group/get-group-apply-list";
            public const string SetGroupTop = "/v1/group/SetGroupTop";
            public const string SetGroupShake = "/v1/group/set-group-shake";
            public const string SetGroupMute = "/v1/group/set-group-mute";
            public const string SetMemberRemark = "/v1/group/set-member-remark";
            public const string SetGroupRemark = "/v1/group/set-group-remark";
            public const string SetGroupAvatar = "/v1/group/set-group-avatar";
            public const string SetGroupName = "/v1/group/set-group-name";
            public const string SetMemberNickname = "/v1/group/set-member-nickname";
            public const string SetNoticeMode = "/v1/group/set-notice-mode";
            public const string GetApplyLogs = "/v1/group/get-apply-logs";
            public const string GroupMemberInvite = "/v1/group/group-member-invite";
            public const string GroupManageApply = "/v1/group/group-manage-apply";
            public const string GroupUpdate = "/v1/group/group-upgrade";
            public const string GetNimGroup = "/v1/group/get-nim-group";
            public const string GetNimUserGroups = "/v1/group/get-nim-user-groups";
            public const string GroupTransfer = "/v1/group/group-transfer";
            public const string GetGroupHistoryMessage = "/v1/group/get-group-history-message";
            public const string GetGroupList = "/v1/group/get-group-list";
            public const string SetNicknameMode = "/v1/group/set-nickname-mode";
            public const string SetEnterLimit = "/v1/group/set-enter-limit";
            public const string SetFileMode = "/v1/group/set-file-mode";
            public const string SetGroupCallChat = "/v1/group/set-audio-video-chat";
            public const string SetCheckWordMode = "/v1/group/set-check-word-mode";
            public const string SetMemberMute = "/v1/group/set-member-mute";
            public const string MemberMuteCancel = "/v1/group/member-mute-cancel";
            public const string SetGroupMemberUnMute = "/v1/group/SetGroupMemberUnMute";
            public const string SetSearchMode = "/v1/group/set-search-mode";
            public const string SetPrivateChat = "/v1/group/set-private-chat";
            public const string LeaveGroup = "/v1/group/leave-group";
            public const string SetRedEnvelopeSettings = "/v1/group/set-red-envelope-settings";
            public const string GetGid = "/v1/group/get-gid";
            public const string GetGroupMembership = "/v1/group/get-group-membership";
            public const string GroupInfoText = "/v1/group/group-info-ext";
            public const string GetEnterLimit = "/v1/group/get-enter-limit";
            public const string GetGroupInfoService = "/v1/group/get-group-info-service";
            public const string UpdateGroupUserMuteService = "/v1/group/update-group-user-mute-service";
            public const string MessageRollback = "/v1/group/message-rollback";
            public const string GetGroupCloudForService = "/v1/group/GetGroupCloudForService";
            public const string GroupNumLimit = "/v1/group/group-num-limit";
            public const string GroupHelperList = "/v1/group/group-helper-list";
            public const string ActivationSuperGroup = "/v1/group/activation-supergroup";
            public const string GroupExist = "/v1/group/exist";
            public const string GetGroupByAccount = "/v1/group/get-group-by-account";
            public const string UserExistGroup = "/v1/group/user-exist-group";
            public const string DelApplyLogs = "/v1/group/del-apply-logs";
            public const string GetGroupQrcode = "/v1/qr-code/get-qrcode";
            public const string GroupMuteMember = "/v1/plugins/group-mute-member";
            public const string SetGroupTopMsg = "v1/group/set-group-top-msg";
            public const string RemoveGroupTopMsg = "v1/group/remove-group-top-msg";
            public const string SetShareCard = "/v1/group/set-share-card";

            // 图片检查
            public const string ImageCheck = "/v1/user/image-check";

            // 收藏相关
            public const string AddCollect = "/v1/collect/add-collect";
            public const string DelCollect = "/v1/collect/del-collect";
            public const string GetCollects = "/v1/collect/get-collects";

            // 表情相关
            public const string StickerSets = "/v1/user/sticker-sets";
            public const string GetStickerInfo = "/v1/user/get-sticker-set-info";
            public const string AddSticker = "/v1/user/add-sticker-set";
            public const string DelSticker = "/v1/user/del-sticker-set";
            public const string StickerSetAdds = "/v1/user/sticker-set-adds";
            public const string AddStickerCollect = "/v1/user/add-sticker-collect";
            public const string StickerCollects = "/v1/user/sticker-collects";
            public const string DelStickerCollect = "/v1/user/del-sticker-collects";
            public const string MoveStickerCollect = "/v1/user/collected-sticker-move-to-first";

            // 举报相关
            public const string GetReportDetail = "/v1/report/get-report-detail";

            // 朋友圈相关
            public const string GetMomentSetting = "/v1/settings/get-moment-setting";
            public const string ChangeMomentBg = "/v1/settings/change-moment-setting-background";
            public const string AddFeed = "/v1/moment/add_feed";
            public const string DelFeed = "/v1/moment/del_feed";
            public const string AddComment = "/v1/moment/add_comment";
            public const string DelComment = "/v1/moment/del_comment";
            public const string AddLike = "/v1/moment/add_like";
            public const string DelLike = "/v1/moment/del_like";

            // RTC相关
            public const string GetRtcNewRoom = "/v1/rtc/new_room";
            public const string GetRtcRoom = "/v1/rtc/get_room";
            public const string GetRoomToken = "/v1/rtc/get_room_token";
            public const string DelRtcRoom = "/v1/rtc/del_room";
            public const string DelRoomMember = "/v1/rtc/del_room_member";

            // 企业相关
            public const string GetEnterpriseOwner = "/v1/enterprise/get_owner";
            public const string GetEnterpriseGoods = "/v1/enterprise/get_goods";
            public const string AddEnterpriseOrder = "/v1/enterprise/add_order";
            public const string GetStaffList = "/v1/enterprise/get_staffs";
            public const string AddStaff = "/v1/enterprise/add_staff";
            public const string DelStaff = "/v1/enterprise/del_staff";
        }

        #endregion

        private WangShangLiaoApiService()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        #region 初始化和认证

        /// <summary>
        /// 初始化服务
        /// </summary>
        public void Initialize(string baseUrl, string token, string nimId)
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _token = token;
            _nimId = nimId;

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            _httpClient.DefaultRequestHeaders.Add("X-Nim-Id", nimId);
        }

        /// <summary>
        /// 更新Token
        /// </summary>
        public void UpdateToken(string token)
        {
            _token = token;
            _httpClient.DefaultRequestHeaders.Remove("Authorization");
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        }

        #endregion

        #region 通用请求方法

        /// <summary>
        /// 发送GET请求
        /// </summary>
        public async Task<ApiResponse<T>> GetAsync<T>(string endpoint, Dictionary<string, string> queryParams = null)
        {
            try
            {
                var url = BuildUrl(endpoint, queryParams);
                var response = await _httpClient.GetAsync(url);
                return await ParseResponse<T>(response);
            }
            catch (Exception ex)
            {
                Log($"[API] GET {endpoint} 失败: {ex.Message}");
                return new ApiResponse<T> { Success = false, Error = ex.Message };
            }
        }

        /// <summary>
        /// 发送POST请求
        /// </summary>
        public async Task<ApiResponse<T>> PostAsync<T>(string endpoint, object data)
        {
            try
            {
                var url = BuildUrl(endpoint);
                var json = new JavaScriptSerializer().Serialize(data);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, content);
                return await ParseResponse<T>(response);
            }
            catch (Exception ex)
            {
                Log($"[API] POST {endpoint} 失败: {ex.Message}");
                return new ApiResponse<T> { Success = false, Error = ex.Message };
            }
        }

        private string BuildUrl(string endpoint, Dictionary<string, string> queryParams = null)
        {
            var url = $"{_baseUrl}{endpoint}";
            if (queryParams != null && queryParams.Count > 0)
            {
                var query = new List<string>();
                foreach (var kv in queryParams)
                {
                    query.Add($"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}");
                }
                url += "?" + string.Join("&", query);
            }
            return url;
        }

        private async Task<ApiResponse<T>> ParseResponse<T>(HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return new ApiResponse<T>
                {
                    Success = false,
                    Error = $"HTTP {(int)response.StatusCode}: {content}"
                };
            }

            try
            {
                var result = new JavaScriptSerializer().Deserialize<ApiResponse<T>>(content);
                result.Success = true;
                return result;
            }
            catch
            {
                return new ApiResponse<T> { Success = true, RawContent = content };
            }
        }

        #endregion

        #region 群组API

        /// <summary>
        /// 获取群成员列表
        /// </summary>
        public async Task<ApiResponse<GroupMemberListResponse>> GetGroupMembersAsync(string groupId)
        {
            return await PostAsync<GroupMemberListResponse>(ApiUrls.GetGroupMembers, new { groupId });
        }

        /// <summary>
        /// 获取群信息
        /// </summary>
        public async Task<ApiResponse<GroupInfoResponse>> GetGroupInfoAsync(string groupId)
        {
            return await PostAsync<GroupInfoResponse>(ApiUrls.GetGroupInfo, new { groupId });
        }

        /// <summary>
        /// 设置群成员禁言
        /// </summary>
        public async Task<ApiResponse<object>> SetMemberMuteAsync(string groupId, string userId, int minutes)
        {
            return await PostAsync<object>(ApiUrls.SetMemberMute, new
            {
                groupId,
                account = userId,
                mute = minutes > 0,
                duration = minutes * 60
            });
        }

        /// <summary>
        /// 取消群成员禁言
        /// </summary>
        public async Task<ApiResponse<object>> CancelMemberMuteAsync(string groupId, string userId)
        {
            return await PostAsync<object>(ApiUrls.MemberMuteCancel, new { groupId, account = userId });
        }

        /// <summary>
        /// 移除群成员
        /// </summary>
        public async Task<ApiResponse<object>> RemoveGroupMemberAsync(string groupId, string userId)
        {
            return await PostAsync<object>(ApiUrls.RemoveGroupMember, new
            {
                groupId,
                accounts = new[] { userId }
            });
        }

        /// <summary>
        /// 设置群成员昵称
        /// </summary>
        public async Task<ApiResponse<object>> SetMemberNicknameAsync(string groupId, string userId, string nickname)
        {
            return await PostAsync<object>(ApiUrls.SetMemberNickname, new
            {
                groupId,
                account = userId,
                nickName = nickname
            });
        }

        /// <summary>
        /// 消息撤回
        /// </summary>
        public async Task<ApiResponse<object>> MessageRollbackAsync(string groupId, string msgId)
        {
            return await PostAsync<object>(ApiUrls.MessageRollback, new { groupId, msgId });
        }

        /// <summary>
        /// 设置群公告
        /// </summary>
        public async Task<ApiResponse<object>> AddGroupNoticeAsync(string groupId, string content, bool isTop = false)
        {
            return await PostAsync<object>(ApiUrls.AddGroupNotice, new
            {
                groupId,
                content,
                isTop
            });
        }

        /// <summary>
        /// 获取群列表
        /// </summary>
        public async Task<ApiResponse<GroupListResponse>> GetGroupListAsync()
        {
            return await PostAsync<GroupListResponse>(ApiUrls.GetGroupList, new { });
        }

        /// <summary>
        /// 获取群历史消息
        /// </summary>
        public async Task<ApiResponse<HistoryMessageResponse>> GetGroupHistoryMessageAsync(
            string groupId, long lastTime = 0, int limit = 100)
        {
            return await PostAsync<HistoryMessageResponse>(ApiUrls.GetGroupHistoryMessage, new
            {
                groupId,
                lastTime,
                limit
            });
        }

        #endregion

        #region 好友API

        /// <summary>
        /// 获取好友列表
        /// </summary>
        public async Task<ApiResponse<FriendListResponse>> GetFriendListAsync()
        {
            return await PostAsync<FriendListResponse>(ApiUrls.GetFriendList, new { });
        }

        /// <summary>
        /// 处理好友申请
        /// </summary>
        public async Task<ApiResponse<object>> DealFriendApplyAsync(string applyId, bool accept, string message = "")
        {
            return await PostAsync<object>(ApiUrls.DealFriendApply, new
            {
                applyId,
                pass = accept,
                msg = message
            });
        }

        /// <summary>
        /// 获取好友申请列表
        /// </summary>
        public async Task<ApiResponse<FriendApplyListResponse>> GetFriendApplyListAsync()
        {
            return await PostAsync<FriendApplyListResponse>(ApiUrls.GetFriendApplyList, new { });
        }

        #endregion

        #region 用户API

        /// <summary>
        /// 获取系统设置
        /// </summary>
        public async Task<ApiResponse<SystemSettingResponse>> GetSystemSettingAsync()
        {
            return await PostAsync<SystemSettingResponse>(ApiUrls.GetSystemSetting, new { });
        }

        /// <summary>
        /// 设置自动回复
        /// </summary>
        public async Task<ApiResponse<object>> SetAutoReplyAsync(bool enabled, string content)
        {
            return await PostAsync<object>(ApiUrls.SetAutoReply, new
            {
                state = enabled ? 1 : 0,
                content
            });
        }

        /// <summary>
        /// 获取敏感词列表
        /// </summary>
        public async Task<ApiResponse<SensitiveWordsResponse>> GetSensitiveWordsAsync()
        {
            return await PostAsync<SensitiveWordsResponse>(ApiUrls.GetSensitiveWords, new { });
        }

        #endregion

        private void Log(string message)
        {
            OnLog?.Invoke(message);
            Utils.Logger.Info(message);
        }
    }

    #region 响应模型

    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public int Code { get; set; }
        public string Message { get; set; }
        public string Error { get; set; }
        public T Data { get; set; }
        public string RawContent { get; set; }
    }

    public class GroupMemberListResponse
    {
        public List<GroupMemberInfo> Members { get; set; }
    }

    public class GroupMemberInfo
    {
        public string Account { get; set; }
        public string NickName { get; set; }
        public string Avatar { get; set; }
        public int Role { get; set; } // 0=普通, 1=管理员, 2=群主
        public bool IsMute { get; set; }
        public long JoinTime { get; set; }
    }

    public class GroupInfoResponse
    {
        public string GroupId { get; set; }
        public string Name { get; set; }
        public string Avatar { get; set; }
        public string Owner { get; set; }
        public int MemberCount { get; set; }
        public bool IsMute { get; set; }
        public string Intro { get; set; }
    }

    public class GroupListResponse
    {
        public List<GroupInfoResponse> Groups { get; set; }
    }

    public class HistoryMessageResponse
    {
        public List<HistoryMessage> Messages { get; set; }
    }

    public class HistoryMessage
    {
        public string MsgId { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string Type { get; set; }
        public string Text { get; set; }
        public long Time { get; set; }
    }

    public class FriendListResponse
    {
        public List<FriendInfo> Friends { get; set; }
    }

    public class FriendInfo
    {
        public string Account { get; set; }
        public string NickName { get; set; }
        public string Avatar { get; set; }
        public string Remark { get; set; }
    }

    public class FriendApplyListResponse
    {
        public List<FriendApply> Applies { get; set; }
    }

    public class FriendApply
    {
        public string ApplyId { get; set; }
        public string From { get; set; }
        public string Message { get; set; }
        public int Status { get; set; }
        public long Time { get; set; }
    }

    public class SystemSettingResponse
    {
        public bool AutoReplyEnabled { get; set; }
        public string AutoReplyContent { get; set; }
    }

    public class SensitiveWordsResponse
    {
        public List<string> Words { get; set; }
    }

    #endregion
}
