using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using WSLFramework.Utils;

namespace WSLFramework.Services
{
    /// <summary>
    /// 旺商聊完整API客户端
    /// 根据逆向分析文档实现 (213个API端点)
    /// 主服务器: https://yiyong.netease.im
    /// </summary>
    public class WangShangLiaoApiClient : IDisposable
    {
        #region 单例
        private static readonly Lazy<WangShangLiaoApiClient> _instance = 
            new Lazy<WangShangLiaoApiClient>(() => new WangShangLiaoApiClient());
        public static WangShangLiaoApiClient Instance => _instance.Value;
        #endregion
        
        #region 服务器地址 (根据逆向文档)
        public const string SERVER_MAIN = "https://yiyong.netease.im";           // 主API
        public const string SERVER_LBS = "https://lbs.netease.im";               // IM服务
        public const string SERVER_RTC = "https://nrtc.netease.im";              // 音视频
        public const string SERVER_ROOM = "https://roomkit.netease.im";          // 会议室
        public const string SERVER_STATIC = "https://yiyong-static.nosdn.127.net"; // 静态资源
        public const string SERVER_STAT = "https://statistic.live.126.net";      // 统计
        public const string SERVER_TEST = "https://qxdevacc.qixin02.xyz";        // 测试服
        #endregion
        
        #region API端点常量
        // === 用户服务 ===
        public const string API_LOGIN = "/v1/login";
        public const string API_USER_LOGIN = "/v1/user/login";
        public const string API_USER_LOGOUT = "/v1/user/logout";
        public const string API_USER_REGISTER = "/v1/user/register";
        public const string API_CHECK_TOKEN = "/v1/checkToken";
        public const string API_ANONYMOUS_LOGIN = "/v1/anonymous/login";
        
        // === 验证服务 ===
        public const string API_VERIFY_SMS = "/v1/verify/sms";
        public const string API_VERIFY_SMS_ANON = "/v1/verify/sms-anon";
        public const string API_VERIFY_CODE = "/v1/verify/verify";
        
        // === 好友服务 ===
        public const string API_FRIEND_ADD_APPLY = "/v1/friend/add-friend-apply";
        public const string API_FRIEND_ADD_CLASS = "/v1/friend/add-friend-class";
        public const string API_FRIEND_DELETE = "/v1/friend/del-friend";
        public const string API_FRIEND_APPLY_HANDLER = "/v1/friend/friend-apply-handler";
        public const string API_FRIEND_APPLY_LIST = "/v1/friend/friend-apply-list";
        public const string API_FRIEND_LIST = "/v1/friend/get-friend-list";
        public const string API_FRIEND_INFO = "/v1/friend/get-friend-info";
        public const string API_FRIEND_BLACK_LIST = "/v1/friend/get-black-list";
        
        // === 群组服务 ===
        public const string API_GROUP_CREATE = "/v1/group/create";
        public const string API_GROUP_DISMISS = "/v1/group/group-dismiss";
        public const string API_GROUP_LEAVE = "/v1/group/leave-group";
        public const string API_GROUP_TRANSFER = "/v1/group/group-transfer";
        public const string API_GROUP_ADD_MEMBER = "/v1/group/add-group-member";
        public const string API_GROUP_REMOVE_MEMBER = "/v1/group/remove-group-member";
        public const string API_GROUP_ADD_MANAGE = "/v1/group/add-group-manage";
        public const string API_GROUP_DEL_MANAGE = "/v1/group/del-group-manage";
        public const string API_GROUP_APPLY_JOIN = "/v1/group/apply-join-group";
        public const string API_GROUP_MANAGE_APPLY = "/v1/group/group-manage-apply";
        public const string API_GROUP_INVITE = "/v1/group/group-member-invite";
        public const string API_GROUP_APPLY_LIST = "/v1/group/get-group-apply-list";
        public const string API_GROUP_INFO = "/v1/group/get-group-info";
        public const string API_GROUP_LIST = "/v1/group/get-group-list";
        public const string API_GROUP_MEMBERS = "/v1/group/get-group-members";
        public const string API_GROUP_MEMBER_INFO = "/v1/group/get-group-member-info";
        public const string API_GROUP_HISTORY = "/v1/group/get-group-history-message";
        public const string API_GROUP_EXIST = "/v1/group/exist";
        public const string API_GROUP_GET_GID = "/v1/group/get-gid";
        public const string API_GROUP_NIM_INFO = "/v1/group/get-nim-group";
        public const string API_GROUP_SEARCH = "/v1/group/SearchGroup";
        
        // === 群设置 ===
        public const string API_GROUP_SET_MUTE = "/v1/group/set-group-mute";
        public const string API_GROUP_SET_MEMBER_MUTE = "/v1/group/set-member-mute";
        public const string API_GROUP_SET_NAME = "/v1/group/set-group-name";
        public const string API_GROUP_SET_AVATAR = "/v1/group/set-group-avatar";
        public const string API_GROUP_SET_NICKNAME = "/v1/group/set-member-nickname";
        public const string API_GROUP_SET_NOTICE_MODE = "/v1/group/set-notice-mode";
        public const string API_GROUP_SET_PRIVATE_CHAT = "/v1/group/set-private-chat";
        public const string API_GROUP_UNMUTE_MEMBER = "/v1/group/SetGroupMemberUnMute";
        public const string API_GROUP_SET_TOP = "/v1/group/SetGroupTop";
        public const string API_GROUP_CANCEL_MUTE = "/v1/group/member-mute-cancel";
        
        // === 群公告 ===
        public const string API_GROUP_ADD_NOTICE = "/v1/group/add-notice";
        public const string API_GROUP_DEL_NOTICE = "/v1/group/notice-del";
        public const string API_GROUP_NOTICE_INFO = "/v1/group/notice-info";
        public const string API_GROUP_NOTICE_LIST = "/v1/group/notice-list";
        public const string API_GROUP_TOP_NOTICE = "/v1/group/top-notice";
        public const string API_GROUP_MSG_ROLLBACK = "/v1/group/message-rollback";
        
        // === 收藏服务 ===
        public const string API_COLLECT_ADD = "/v1/collect/add-collect";
        public const string API_COLLECT_DEL = "/v1/collect/del-collect";
        public const string API_COLLECT_LIST = "/v1/collect/get-collects";
        
        // === 设置服务 ===
        public const string API_SETTINGS_AVATAR = "/v1/settings/avatar";
        public const string API_SETTINGS_NICKNAME = "/v1/settings/self-nick-name";
        public const string API_SETTINGS_PASSWORD = "/v1/settings/password";
        public const string API_SETTINGS_AUTO_REPLY = "/v1/settings/set-auto-reply";
        public const string API_SETTINGS_LINE_GROUP = "/v1/settings/get-line-group";
        public const string API_SETTINGS_SYS_CFG = "/v1/settings/get-sys-cfg";
        public const string API_SETTINGS_SENSITIVE = "/v1/settings/get-sensitive-words";
        
        // === 插件服务 ===
        public const string API_PLUGIN_MUTE_MEMBER = "/v1/plugins/group-mute-member";
        public const string API_PLUGIN_ACTIVATION = "/v1/plugins/activation-code";
        public const string API_PLUGIN_GET_GID = "/v1/plugins/get-gid";
        public const string API_PLUGIN_USERINFO = "/v1/plugins/get-userinfo-by-id";
        public const string API_PLUGIN_FRIEND_HANDLER = "/v1/plugins/handle-relation-ask-friend";
        public const string API_PLUGIN_USER_GROUP = "/v1/plugins/user-group-info";
        
        // === RTC/音视频 ===
        public const string API_RTC_NEW_ROOM = "/v1/rtc/new_room";
        public const string API_RTC_GET_ROOM = "/v1/rtc/get_room";
        public const string API_RTC_DEL_ROOM = "/v1/rtc/del_room";
        public const string API_RTC_ROOM_TOKEN = "/v1/rtc/get_room_token";
        public const string API_RTC_LIVE_ROOM = "/v1/rtc/get_live_room";
        public const string API_RTC_NEW_LIVE = "/v1/rtc/new_live_room";
        public const string API_RTC_CLOSE_LIVE = "/v1/rtc/close_live_room";
        #endregion
        
        #region 错误码
        public const int CODE_SUCCESS = 0;
        public const int CODE_AUTH_REQUIRED = 10010;
        public const int CODE_UNAUTHORIZED = 401;
        public const int CODE_FORBIDDEN = 403;
        public const int CODE_NOT_FOUND = 404;
        public const int CODE_PARAM_ERROR = 1001;
        public const int CODE_TOKEN_INVALID = 1002;
        public const int CODE_ACCOUNT_NOT_EXIST = 1003;
        public const int CODE_PASSWORD_ERROR = 1004;
        public const int CODE_VERIFY_ERROR = 1005;
        public const int CODE_REQUEST_FREQUENT = 1006;
        public const int CODE_ALREADY_FRIEND = 1007;
        public const int CODE_ALREADY_PROCESSED = 1008;
        public const int CODE_LIMIT_REACHED = 1009;
        public const int CODE_NO_PERMISSION = 1010;
        public const int CODE_ALREADY_IN_GROUP = 1011;
        public const int CODE_ROLLBACK_TIMEOUT = 1012;
        #endregion
        
        #region 私有字段
        private readonly HttpClient _httpClient;
        private readonly JavaScriptSerializer _serializer;
        private string _baseUrl = SERVER_MAIN;
        private string _xToken;
        private string _xId;
        private string _appKey;
        private string _deviceId;
        private volatile bool _disposed;
        #endregion
        
        #region 属性
        public string Token { get => _xToken; set => _xToken = value; }
        public string UserId { get => _xId; set => _xId = value; }
        public string AppKey { get => _appKey; set => _appKey = value; }
        public string DeviceId { get => _deviceId; set => _deviceId = value; }
        public string BaseUrl { get => _baseUrl; set => _baseUrl = value?.TrimEnd('/'); }
        
        public event Action<string> OnLog;
        #endregion
        
        #region 构造函数
        private WangShangLiaoApiClient()
        {
            ServicePointManager.ServerCertificateValidationCallback = (s, c, ch, e) => true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11;
            
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            _serializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
            _deviceId = GenerateDeviceId();
        }
        #endregion
        
        #region 用户认证API
        
        /// <summary>
        /// 用户登录 (短信验证码)
        /// </summary>
        public async Task<ApiResult> LoginAsync(string phone, string code)
        {
            return await PostAsync(API_USER_LOGIN, new
            {
                phone = phone,
                code = code,
                deviceId = _deviceId,
                platform = "windows"
            });
        }
        
        /// <summary>
        /// 用户登录 (密码)
        /// </summary>
        public async Task<ApiResult> LoginWithPasswordAsync(string phone, string password)
        {
            return await PostAsync(API_USER_LOGIN, new
            {
                phone = phone,
                password = password,
                deviceId = _deviceId,
                platform = "windows"
            });
        }
        
        /// <summary>
        /// Token检查
        /// </summary>
        public async Task<ApiResult> CheckTokenAsync()
        {
            return await PostAsync(API_CHECK_TOKEN, new { });
        }
        
        /// <summary>
        /// 用户登出
        /// </summary>
        public async Task<ApiResult> LogoutAsync()
        {
            return await PostAsync(API_USER_LOGOUT, new { });
        }
        
        /// <summary>
        /// 发送短信验证码
        /// </summary>
        public async Task<ApiResult> SendSmsAsync(string phone, string type = "login")
        {
            return await PostAsync(API_VERIFY_SMS, new { phone, type });
        }
        
        /// <summary>
        /// 校验验证码
        /// </summary>
        public async Task<ApiResult> VerifyCodeAsync(string phone, string code)
        {
            return await PostAsync(API_VERIFY_CODE, new { phone, code });
        }
        
        #endregion
        
        #region 好友API
        
        /// <summary>
        /// 获取好友列表
        /// </summary>
        public async Task<ApiResult> GetFriendListAsync()
        {
            return await GetAsync(API_FRIEND_LIST);
        }
        
        /// <summary>
        /// 获取好友申请列表
        /// </summary>
        public async Task<ApiResult> GetFriendApplyListAsync()
        {
            return await GetAsync(API_FRIEND_APPLY_LIST);
        }
        
        /// <summary>
        /// 添加好友
        /// </summary>
        public async Task<ApiResult> AddFriendAsync(long targetId, string verifyMsg = "")
        {
            return await PostAsync(API_FRIEND_ADD_APPLY, new
            {
                targetId = targetId,
                verifyMsg = verifyMsg
            });
        }
        
        /// <summary>
        /// 处理好友申请
        /// </summary>
        public async Task<ApiResult> HandleFriendApplyAsync(long fromId, bool agree)
        {
            return await PostAsync(API_FRIEND_APPLY_HANDLER, new
            {
                fromId = fromId,
                state = agree ? "FRIENDLOG_STATE_AGREE" : "FRIENDLOG_STATE_REFUSE"
            });
        }
        
        /// <summary>
        /// 删除好友
        /// </summary>
        public async Task<ApiResult> DeleteFriendAsync(long friendId)
        {
            return await PostAsync(API_FRIEND_DELETE, new { friendId });
        }
        
        #endregion
        
        #region 群组API
        
        /// <summary>
        /// 获取群列表
        /// </summary>
        public async Task<ApiResult> GetGroupListAsync()
        {
            return await GetAsync(API_GROUP_LIST);
        }
        
        /// <summary>
        /// 获取群信息
        /// </summary>
        public async Task<ApiResult> GetGroupInfoAsync(long groupId)
        {
            return await GetAsync($"{API_GROUP_INFO}?groupId={groupId}");
        }
        
        /// <summary>
        /// 获取群成员列表
        /// </summary>
        public async Task<ApiResult> GetGroupMembersAsync(long groupId)
        {
            return await GetAsync($"{API_GROUP_MEMBERS}?groupId={groupId}");
        }
        
        /// <summary>
        /// 获取群成员信息
        /// </summary>
        public async Task<ApiResult> GetGroupMemberInfoAsync(long groupId, long memberId)
        {
            return await GetAsync($"{API_GROUP_MEMBER_INFO}?groupId={groupId}&memberId={memberId}");
        }
        
        /// <summary>
        /// 设置群禁言
        /// </summary>
        public async Task<ApiResult> SetGroupMuteAsync(long groupId, bool mute)
        {
            return await PostAsync(API_GROUP_SET_MUTE, new
            {
                groupId = groupId,
                muteMode = mute ? "MUTE_ALL" : "MUTE_NO"
            });
        }
        
        /// <summary>
        /// 设置成员禁言
        /// </summary>
        public async Task<ApiResult> SetMemberMuteAsync(long groupId, long memberId, int duration = 0)
        {
            return await PostAsync(API_GROUP_SET_MEMBER_MUTE, new
            {
                groupId = groupId,
                memberId = memberId,
                duration = duration
            });
        }
        
        /// <summary>
        /// 解除成员禁言
        /// </summary>
        public async Task<ApiResult> UnmuteMemberAsync(long groupId, long memberId)
        {
            return await PostAsync(API_GROUP_UNMUTE_MEMBER, new
            {
                groupId = groupId,
                memberId = memberId
            });
        }
        
        /// <summary>
        /// 设置成员昵称
        /// </summary>
        public async Task<ApiResult> SetMemberNicknameAsync(long groupId, long memberId, string nickname)
        {
            return await PostAsync(API_GROUP_SET_NICKNAME, new
            {
                groupId = groupId,
                memberId = memberId,
                nickname = nickname
            });
        }
        
        /// <summary>
        /// 添加群成员
        /// </summary>
        public async Task<ApiResult> AddGroupMemberAsync(long groupId, long[] memberIds)
        {
            return await PostAsync(API_GROUP_ADD_MEMBER, new
            {
                groupId = groupId,
                memberIds = memberIds
            });
        }
        
        /// <summary>
        /// 移除群成员
        /// </summary>
        public async Task<ApiResult> RemoveGroupMemberAsync(long groupId, long memberId)
        {
            return await PostAsync(API_GROUP_REMOVE_MEMBER, new
            {
                groupId = groupId,
                memberId = memberId
            });
        }
        
        /// <summary>
        /// 添加管理员
        /// </summary>
        public async Task<ApiResult> AddGroupManagerAsync(long groupId, long memberId)
        {
            return await PostAsync(API_GROUP_ADD_MANAGE, new
            {
                groupId = groupId,
                memberId = memberId
            });
        }
        
        /// <summary>
        /// 删除管理员
        /// </summary>
        public async Task<ApiResult> RemoveGroupManagerAsync(long groupId, long memberId)
        {
            return await PostAsync(API_GROUP_DEL_MANAGE, new
            {
                groupId = groupId,
                memberId = memberId
            });
        }
        
        /// <summary>
        /// 获取加群申请列表
        /// </summary>
        public async Task<ApiResult> GetGroupApplyListAsync()
        {
            return await GetAsync(API_GROUP_APPLY_LIST);
        }
        
        /// <summary>
        /// 处理加群申请
        /// </summary>
        public async Task<ApiResult> HandleGroupApplyAsync(long groupId, long applyId, bool agree)
        {
            return await PostAsync(API_GROUP_MANAGE_APPLY, new
            {
                groupId = groupId,
                applyId = applyId,
                state = agree ? "APPLY_STATE_AGREE" : "APPLY_STATE_REFUSE"
            });
        }
        
        /// <summary>
        /// 发布群公告
        /// </summary>
        public async Task<ApiResult> AddGroupNoticeAsync(long groupId, string content)
        {
            return await PostAsync(API_GROUP_ADD_NOTICE, new
            {
                groupId = groupId,
                content = content
            });
        }
        
        /// <summary>
        /// 获取群公告列表
        /// </summary>
        public async Task<ApiResult> GetGroupNoticeListAsync(long groupId)
        {
            return await GetAsync($"{API_GROUP_NOTICE_LIST}?groupId={groupId}");
        }
        
        /// <summary>
        /// 撤回消息
        /// </summary>
        public async Task<ApiResult> RollbackMessageAsync(long groupId, string msgId)
        {
            return await PostAsync(API_GROUP_MSG_ROLLBACK, new
            {
                groupId = groupId,
                msgId = msgId
            });
        }
        
        /// <summary>
        /// 获取群历史消息
        /// </summary>
        public async Task<ApiResult> GetGroupHistoryAsync(long groupId, long beginTime = 0, int limit = 100)
        {
            return await GetAsync($"{API_GROUP_HISTORY}?groupId={groupId}&beginTime={beginTime}&limit={limit}");
        }
        
        #endregion
        
        #region 插件API
        
        /// <summary>
        /// ID查询 (NIM ID <-> 旺商聊号)
        /// </summary>
        /// <param name="id">ID</param>
        /// <param name="type">0=NIM→WSL, 1=WSL→NIM, 2=群组</param>
        public async Task<ApiResult> GetGidAsync(string id, int type = 1)
        {
            return await PostAsync(API_PLUGIN_GET_GID, new { id, type });
        }
        
        /// <summary>
        /// 根据ID获取用户信息
        /// </summary>
        public async Task<ApiResult> GetUserInfoByIdAsync(long id, int type = 1)
        {
            return await PostAsync(API_PLUGIN_USERINFO, new { id, type });
        }
        
        /// <summary>
        /// 获取用户群信息
        /// </summary>
        public async Task<ApiResult> GetUserGroupInfoAsync(long groupId)
        {
            return await PostAsync(API_PLUGIN_USER_GROUP, new { groupId });
        }
        
        #endregion
        
        #region 设置API
        
        /// <summary>
        /// 设置自动回复
        /// </summary>
        public async Task<ApiResult> SetAutoReplyAsync(bool enabled, string content)
        {
            return await PostAsync(API_SETTINGS_AUTO_REPLY, new
            {
                enabled = enabled,
                content = content
            });
        }
        
        /// <summary>
        /// 修改昵称
        /// </summary>
        public async Task<ApiResult> SetNicknameAsync(string nickname)
        {
            return await PostAsync(API_SETTINGS_NICKNAME, new { nickname });
        }
        
        /// <summary>
        /// 获取系统配置
        /// </summary>
        public async Task<ApiResult> GetSysConfigAsync()
        {
            return await GetAsync(API_SETTINGS_SYS_CFG);
        }
        
        /// <summary>
        /// 获取敏感词列表
        /// </summary>
        public async Task<ApiResult> GetSensitiveWordsAsync()
        {
            return await GetAsync(API_SETTINGS_SENSITIVE);
        }
        
        #endregion
        
        #region HTTP方法
        
        private async Task<ApiResult> PostAsync(string endpoint, object data)
        {
            try
            {
                var url = _baseUrl + endpoint;
                var json = _serializer.Serialize(data);
                
                Log($"POST {endpoint}");
                
                var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
                
                AddHeaders(request);
                
                var response = await _httpClient.SendAsync(request);
                var responseJson = await response.Content.ReadAsStringAsync();
                
                Log($"  Response: {responseJson.Substring(0, Math.Min(200, responseJson.Length))}...");
                
                return ParseResult(responseJson, (int)response.StatusCode);
            }
            catch (Exception ex)
            {
                Log($"POST 异常: {ex.Message}");
                return new ApiResult { Success = false, Code = -1, Message = ex.Message };
            }
        }
        
        private async Task<ApiResult> GetAsync(string endpoint)
        {
            try
            {
                var url = _baseUrl + endpoint;
                
                Log($"GET {endpoint}");
                
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                AddHeaders(request);
                
                var response = await _httpClient.SendAsync(request);
                var responseJson = await response.Content.ReadAsStringAsync();
                
                Log($"  Response: {responseJson.Substring(0, Math.Min(200, responseJson.Length))}...");
                
                return ParseResult(responseJson, (int)response.StatusCode);
            }
            catch (Exception ex)
            {
                Log($"GET 异常: {ex.Message}");
                return new ApiResult { Success = false, Code = -1, Message = ex.Message };
            }
        }
        
        private void AddHeaders(HttpRequestMessage request)
        {
            request.Headers.Add("X-Platform", "windows");
            request.Headers.Add("X-Version", "2.7.0");
            request.Headers.Add("X-Device-Id", _deviceId);
            request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
            
            if (!string.IsNullOrEmpty(_xToken))
            {
                request.Headers.Add("x-token", _xToken);
                request.Headers.Add("Authorization", $"Bearer {_xToken}");
            }
            if (!string.IsNullOrEmpty(_xId))
            {
                request.Headers.Add("x-id", _xId);
            }
            if (!string.IsNullOrEmpty(_appKey))
            {
                request.Headers.Add("X-App-Key", _appKey);
            }
        }
        
        private ApiResult ParseResult(string json, int httpCode)
        {
            try
            {
                var dict = _serializer.Deserialize<Dictionary<string, object>>(json);
                var result = new ApiResult
                {
                    HttpCode = httpCode,
                    RawJson = json
                };
                
                if (dict.ContainsKey("code"))
                    result.Code = Convert.ToInt32(dict["code"]);
                if (dict.ContainsKey("msg"))
                    result.Message = dict["msg"]?.ToString();
                if (dict.ContainsKey("data"))
                    result.Data = dict["data"];
                
                result.Success = result.Code == CODE_SUCCESS;
                return result;
            }
            catch
            {
                return new ApiResult
                {
                    Success = false,
                    Code = -1,
                    Message = "解析响应失败",
                    RawJson = json,
                    HttpCode = httpCode
                };
            }
        }
        
        private string GenerateDeviceId()
        {
            try
            {
                var machineId = Environment.MachineName + Environment.UserName;
                using (var sha = System.Security.Cryptography.SHA256.Create())
                {
                    var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(machineId));
                    return BitConverter.ToString(hash).Replace("-", "").Substring(0, 32).ToLower();
                }
            }
            catch
            {
                return Guid.NewGuid().ToString("N");
            }
        }
        
        /// <summary>
        /// 设置认证信息
        /// </summary>
        public void SetAuth(string token, string userId)
        {
            _xToken = token;
            _xId = userId;
        }
        
        private void Log(string msg)
        {
            Logger.Info($"[WslApi] {msg}");
            OnLog?.Invoke(msg);
        }
        
        #endregion
        
        #region IDisposable
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _httpClient?.Dispose();
        }
        #endregion
    }
    
    /// <summary>
    /// API响应结果
    /// </summary>
    public class ApiResult
    {
        public bool Success { get; set; }
        public int Code { get; set; }
        public int HttpCode { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }
        public string RawJson { get; set; }
        
        /// <summary>是否需要重新登录</summary>
        public bool NeedReLogin => Code == 401 || Code == 1002 || Code == 10010;
        
        /// <summary>是否权限不足</summary>
        public bool NoPermission => Code == 1010;
        
        /// <summary>获取Data字典</summary>
        public Dictionary<string, object> GetDataDict()
        {
            return Data as Dictionary<string, object>;
        }
    }
}
