using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using WSLFramework.Utils;

namespace WSLFramework.Services
{
    /// <summary>
    /// 旺商聊 HTTP API 服务
    /// 根据旺商聊深度连接协议实现
    /// </summary>
    public class WangShangLiaoHttpApi : IDisposable
    {
        #region 单例模式

        private static readonly Lazy<WangShangLiaoHttpApi> _instance =
            new Lazy<WangShangLiaoHttpApi>(() => new WangShangLiaoHttpApi());

        public static WangShangLiaoHttpApi Instance => _instance.Value;

        #endregion

        #region 常量 - API端点

        // 授权验证
        public const string API_ACTIVATION_CODE = "/v1/plugins/activation-code";
        // 群禁言
        public const string API_GROUP_MUTE = "/v1/group/set-group-mute";
        // 好友申请处理
        public const string API_FRIEND_REQUEST = "/v1/plugins/handle-relation-ask-friend";
        // ID查询
        public const string API_GET_GID = "/v1/plugins/get-gid";
        // 用户信息查询 (根据旺商聊深度连接协议第十四节)
        public const string API_GET_USERINFO = "/v1/plugins/get-userinfo-by-id";

        // muteMode 常量 (根据旺商聊深度连接协议)
        public const string MUTE_ALL = "MUTE_ALL";       // 全员禁言 - 除管理员外全部禁言
        public const string MUTE_MEMBER = "MUTE_MEMBER"; // 成员禁言 - 指定成员禁言
        public const string MUTE_NO = "MUTE_NO";         // 解除禁言 - 解除全员禁言

        // friendState 常量
        public const string FRIEND_AGREE = "FRIENDLOG_STATE_AGREE";
        public const string FRIEND_REFUSE = "FRIENDLOG_STATE_REFUSE";

        // ID查询类型 (根据旺商聊深度连接协议第十四节)
        public const int ID_TYPE_NIM_TO_WSL = 0;   // NIM ID → 旺商聊号
        public const int ID_TYPE_WSL_TO_NIM = 1;   // 旺商聊号 → NIM ID
        public const int ID_TYPE_GROUP = 2;        // 群组ID查询

        // 新增API端点 (根据旺商聊深度连接协议第十六节)
        public const string API_ENCODE_MSG = "/v1/plugins/encode-msg";          // 消息编码
        public const string API_USER_GROUP_INFO = "/v1/plugins/user-group-info"; // 用户群信息
        public const string API_USER_LOGIN = "/v1/user/login";                   // 用户登录

        // 登录类型
        public const string LOGIN_TYPE_ACCOUNT_PWD = "LOGIN_TYPE_ACCOUNT_PWD";   // 账号密码登录
        public const string LOGIN_TYPE_TOKEN = "LOGIN_TYPE_TOKEN";                // Token登录

        // 消息会话类型 (根据旺商聊深度连接协议第十五节)
        public const string MSG_KIND_P2P = "MSG_KIND_P2P";       // 私聊
        public const string MSG_KIND_GROUP = "MSG_KIND_GROUP";   // 群聊

        // 消息格式
        public const string MSG_FORMAT_TEXT = "MSG_TEXT";         // 文本消息
        public const string MSG_FORMAT_IMAGE = "MSG_IMAGE";       // 图片消息

        // 消息角色
        public const string MSG_ROLE_MINE = "MSG_MINE";           // 自己发送
        public const string MSG_ROLE_MEMBER = "MSG_MEMBER";       // 成员消息

        // 账号类型
        public const string ACCOUNT_MEMBER = "ACCOUNT_MEMBER";    // 成员
        public const string ACCOUNT_SYSTEM = "ACCOUNT_SYSTEM";    // 系统

        #endregion

        #region 私有字段

        private readonly HttpClient _httpClient;
        private readonly JavaScriptSerializer _serializer;
        private string _baseUrl = "https://yiyong.netease.im"; // 主API服务器（逆向获取）
        private string _token;
        private string _userId;
        private string _groupToken;
        private volatile bool _isDisposed;

        #endregion

        #region 公共属性

        /// <summary>API基础URL</summary>
        public string BaseUrl
        {
            get => _baseUrl;
            set => _baseUrl = value?.TrimEnd('/') ?? "";
        }

        /// <summary>认证Token</summary>
        public string Token
        {
            get => _token;
            set => _token = value;
        }

        /// <summary>用户ID</summary>
        public string UserId
        {
            get => _userId;
            set => _userId = value;
        }

        /// <summary>群组Token</summary>
        public string GroupToken
        {
            get => _groupToken;
            set => _groupToken = value;
        }

        #endregion

        #region 事件

        public event Action<string> OnLog;

        #endregion

        #region 构造函数

        public WangShangLiaoHttpApi()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
            _serializer = new JavaScriptSerializer();

            // 设置默认请求头
            _httpClient.DefaultRequestHeaders.Add("X-Device", "1");
            _httpClient.DefaultRequestHeaders.Add("User-Agent", 
                "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36");
        }

        #endregion

        #region HTTP API 方法

        /// <summary>
        /// 授权验证
        /// POST /v1/plugins/activation-code
        /// </summary>
        public async Task<ApiResponse> ActivateAsync(string type, string activationCode, string userName)
        {
            var requestData = new Dictionary<string, object>
            {
                { "type", type },
                { "token", activationCode },
                { "user", userName }
            };

            return await PostAsync(API_ACTIVATION_CODE, requestData);
        }

        /// <summary>
        /// 设置群禁言
        /// POST /v1/group/set-group-mute
        /// </summary>
        /// <param name="groupId">群ID (数字)</param>
        /// <param name="mute">true=禁言, false=解禁</param>
        public async Task<ApiResponse> SetGroupMuteAsync(long groupId, bool mute)
        {
            var requestData = new Dictionary<string, object>
            {
                { "groupId", groupId },
                { "muteMode", mute ? MUTE_ALL : MUTE_NO }
            };

            return await PostAsync(API_GROUP_MUTE, requestData);
        }

        /// <summary>
        /// 设置群禁言模式 - 完整版本
        /// POST /v1/group/set-group-mute
        /// 根据旺商聊深度连接协议第十三节
        /// </summary>
        /// <param name="groupId">群ID (数字)</param>
        /// <param name="muteMode">禁言模式: MUTE_ALL, MUTE_MEMBER, MUTE_NO</param>
        /// <param name="memberId">成员ID (仅MUTE_MEMBER模式需要)</param>
        public async Task<ApiResponse> SetGroupMuteModeAsync(long groupId, string muteMode, long? memberId = null)
        {
            var requestData = new Dictionary<string, object>
            {
                { "groupId", groupId },
                { "muteMode", muteMode }
            };

            // MUTE_MEMBER 模式需要指定成员ID
            if (muteMode == MUTE_MEMBER && memberId.HasValue)
            {
                requestData["memberId"] = memberId.Value;
            }

            return await PostAsync(API_GROUP_MUTE, requestData);
        }

        /// <summary>
        /// 全员禁言
        /// </summary>
        public Task<ApiResponse> MuteAllAsync(long groupId)
        {
            return SetGroupMuteModeAsync(groupId, MUTE_ALL);
        }

        /// <summary>
        /// 解除全员禁言
        /// </summary>
        public Task<ApiResponse> UnmuteAllAsync(long groupId)
        {
            return SetGroupMuteModeAsync(groupId, MUTE_NO);
        }

        /// <summary>
        /// 禁言指定成员
        /// </summary>
        public Task<ApiResponse> MuteMemberAsync(long groupId, long memberId)
        {
            return SetGroupMuteModeAsync(groupId, MUTE_MEMBER, memberId);
        }

        /// <summary>
        /// 处理好友申请
        /// POST /v1/plugins/handle-relation-ask-friend
        /// </summary>
        /// <param name="fromId">申请者ID</param>
        /// <param name="userId">接收者ID (当前用户)</param>
        /// <param name="agree">true=同意, false=拒绝</param>
        public async Task<ApiResponse> HandleFriendRequestAsync(long fromId, long userId, bool agree)
        {
            var requestData = new Dictionary<string, object>
            {
                { "fromId", fromId },
                { "userId", userId },
                { "state", agree ? FRIEND_AGREE : FRIEND_REFUSE }
            };

            return await PostAsync(API_FRIEND_REQUEST, requestData);
        }

        /// <summary>
        /// ID查询
        /// POST /v1/plugins/get-gid
        /// </summary>
        /// <param name="id">要查询的ID</param>
        /// <param name="type">1=用户ID查询, 2=群组ID查询</param>
        public async Task<ApiResponse> GetGidAsync(string id, int type)
        {
            var requestData = new Dictionary<string, object>
            {
                { "id", id },
                { "type", type }
            };

            return await PostAsync(API_GET_GID, requestData);
        }

        /// <summary>
        /// 查询用户ID
        /// </summary>
        public Task<ApiResponse> GetUserGidAsync(string userId)
        {
            return GetGidAsync(userId, 1);
        }

        /// <summary>
        /// 查询群组ID
        /// </summary>
        public Task<ApiResponse> GetGroupGidAsync(string groupId)
        {
            return GetGidAsync(groupId, ID_TYPE_GROUP);
        }

        #endregion

        #region 用户信息查询 (根据旺商聊深度连接协议第十四节)

        /// <summary>
        /// 通过ID获取用户信息
        /// POST /v1/plugins/get-userinfo-by-id
        /// 根据旺商聊深度连接协议第十四节
        /// </summary>
        /// <param name="nimId">NIM ID</param>
        /// <param name="groupId">群组ID (可选)</param>
        public async Task<ApiResponse> GetUserInfoByNimIdAsync(long nimId, string groupId = null)
        {
            var requestData = new Dictionary<string, object>
            {
                { "nimId", nimId }
            };

            if (!string.IsNullOrEmpty(groupId))
            {
                requestData["group_id"] = groupId;
            }

            return await PostAsync(API_GET_USERINFO, requestData);
        }

        /// <summary>
        /// 通过旺商聊号获取用户信息
        /// POST /v1/plugins/get-userinfo-by-id
        /// </summary>
        /// <param name="wslId">旺商聊号</param>
        /// <param name="type">查询类型: 0=NIM→WSL, 1=WSL→NIM, 2=群组</param>
        public async Task<ApiResponse> GetUserInfoByWslIdAsync(long wslId, int type = ID_TYPE_WSL_TO_NIM)
        {
            var requestData = new Dictionary<string, object>
            {
                { "id", wslId },
                { "type", type }
            };

            return await PostAsync(API_GET_USERINFO, requestData);
        }

        /// <summary>
        /// NIM ID 转 旺商聊号
        /// </summary>
        public Task<ApiResponse> NimIdToWslIdAsync(long nimId)
        {
            return GetUserInfoByWslIdAsync(nimId, ID_TYPE_NIM_TO_WSL);
        }

        /// <summary>
        /// 旺商聊号 转 NIM ID
        /// </summary>
        public Task<ApiResponse> WslIdToNimIdAsync(long wslId)
        {
            return GetUserInfoByWslIdAsync(wslId, ID_TYPE_WSL_TO_NIM);
        }

        /// <summary>
        /// 通用ID查询用户信息
        /// POST /v1/plugins/get-userinfo-by-id
        /// </summary>
        /// <param name="id">用户ID</param>
        /// <param name="type">0=NIM accid, 1=旺商聊号</param>
        public async Task<ApiResponse> GetUserInfoByIdAsync(string id, int type = 1)
        {
            var requestData = new Dictionary<string, object>
            {
                { "id", id },
                { "type", type }
            };

            return await PostAsync(API_GET_USERINFO, requestData);
        }

        #endregion

        #region 群组查询

        /// <summary>
        /// 查询群组ID (别名)
        /// </summary>
        public Task<ApiResponse> GetGroupInfoAsync(string groupId)
        {
            return GetGidAsync(groupId, 2);
        }

        #endregion

        #region 用户登录 (根据旺商聊深度连接协议第十六节)

        /// <summary>
        /// 用户登录 - 账号密码方式
        /// POST /v1/user/login
        /// </summary>
        /// <param name="account">账号</param>
        /// <param name="password">密码</param>
        public async Task<ApiResponse> LoginAsync(string account, string password)
        {
            var requestData = new Dictionary<string, object>
            {
                { "account", account },
                { "passwd", password },
                { "type", LOGIN_TYPE_ACCOUNT_PWD }
            };

            return await PostAsync(API_USER_LOGIN, requestData);
        }

        /// <summary>
        /// 用户登录 - Token方式
        /// POST /v1/user/login
        /// </summary>
        /// <param name="token">登录Token</param>
        public async Task<ApiResponse> LoginWithTokenAsync(string token)
        {
            var requestData = new Dictionary<string, object>
            {
                { "token", token },
                { "type", LOGIN_TYPE_TOKEN }
            };

            return await PostAsync(API_USER_LOGIN, requestData);
        }
        
        #endregion

        #region 消息编码 (根据旺商聊深度连接协议第十六节)

        /// <summary>
        /// 编码消息
        /// POST /v1/plugins/encode-msg
        /// 将消息内容编码为NIM可发送的格式
        /// </summary>
        public async Task<ApiResponse> EncodeMessageAsync(EncodeMessageRequest request)
        {
            return await PostAsync(API_ENCODE_MSG, request.ToDictionary());
        }

        /// <summary>
        /// 编码群消息(简化版)
        /// </summary>
        /// <param name="fromId">发送者ID</param>
        /// <param name="fromName">发送者昵称</param>
        /// <param name="toGroupId">目标群ID</param>
        /// <param name="content">消息内容</param>
        public async Task<ApiResponse> EncodeGroupMessageAsync(long fromId, string fromName, long toGroupId, string content)
        {
            var request = new EncodeMessageRequest
            {
                FromId = fromId,
                FromName = fromName,
                ToId = toGroupId,
                Content = content,
                MsgSession = MSG_KIND_GROUP,
                MsgFormat = MSG_FORMAT_TEXT
            };

            return await EncodeMessageAsync(request);
        }

        /// <summary>
        /// 编码私聊消息(简化版)
        /// </summary>
        public async Task<ApiResponse> EncodeP2PMessageAsync(long fromId, string fromName, long toUserId, string content)
        {
            var request = new EncodeMessageRequest
            {
                FromId = fromId,
                FromName = fromName,
                ToId = toUserId,
                Content = content,
                MsgSession = MSG_KIND_P2P,
                MsgFormat = MSG_FORMAT_TEXT
            };

            return await EncodeMessageAsync(request);
        }

        /// <summary>
        /// 编码带@的消息
        /// </summary>
        public async Task<ApiResponse> EncodeAtMessageAsync(long fromId, string fromName, long toGroupId, 
            string content, List<AtInfo> atList)
        {
            var request = new EncodeMessageRequest
            {
                FromId = fromId,
                FromName = fromName,
                ToId = toGroupId,
                Content = content,
                MsgSession = MSG_KIND_GROUP,
                MsgFormat = MSG_FORMAT_TEXT,
                AtList = atList
            };

            return await EncodeMessageAsync(request);
        }

        #endregion

        #region 用户群信息 (根据旺商聊深度连接协议第十六节)

        /// <summary>
        /// 获取用户群信息
        /// POST /v1/plugins/user-group-info
        /// </summary>
        /// <param name="groupId">群ID</param>
        public async Task<ApiResponse> GetUserGroupInfoAsync(long groupId)
        {
            var requestData = new Dictionary<string, object>
            {
                { "groupId", groupId }
            };

            return await PostAsync(API_USER_GROUP_INFO, requestData);
        }

        #endregion

        #region 通用HTTP方法

        /// <summary>
        /// POST 请求
        /// </summary>
        private async Task<ApiResponse> PostAsync(string endpoint, object data)
        {
            try
            {
                var url = $"{_baseUrl}{endpoint}";
                var json = _serializer.Serialize(data);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // 添加认证头
                if (!string.IsNullOrEmpty(_token))
                {
                    content.Headers.TryAddWithoutValidation("x-token", _token);
                }
                if (!string.IsNullOrEmpty(_userId))
                {
                    content.Headers.TryAddWithoutValidation("x-id", _userId);
                }
                if (!string.IsNullOrEmpty(_groupToken))
                {
                    content.Headers.TryAddWithoutValidation("x-group-token", _groupToken);
                }

                Log($"POST {url}");
                Log($"  Body: {json}");

                var response = await _httpClient.PostAsync(url, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                Log($"  Response: {responseBody}");

                // 解析响应
                var result = ParseResponse(responseBody);
                result.HttpStatusCode = (int)response.StatusCode;

                return result;
            }
            catch (Exception ex)
            {
                Log($"POST 请求异常: {ex.Message}");
                return new ApiResponse
                {
                    Success = false,
                    Code = -1,
                    Message = ex.Message
                };
            }
        }

        /// <summary>
        /// GET 请求
        /// </summary>
        private async Task<ApiResponse> GetAsync(string endpoint)
        {
            try
            {
                var url = $"{_baseUrl}{endpoint}";

                Log($"GET {url}");

                var response = await _httpClient.GetAsync(url);
                var responseBody = await response.Content.ReadAsStringAsync();

                Log($"  Response: {responseBody}");

                var result = ParseResponse(responseBody);
                result.HttpStatusCode = (int)response.StatusCode;

                return result;
            }
            catch (Exception ex)
            {
                Log($"GET 请求异常: {ex.Message}");
                return new ApiResponse
                {
                    Success = false,
                    Code = -1,
                    Message = ex.Message
                };
            }
        }

        /// <summary>
        /// 解析API响应
        /// </summary>
        private ApiResponse ParseResponse(string json)
        {
            try
            {
                var dict = _serializer.Deserialize<Dictionary<string, object>>(json);
                var result = new ApiResponse
                {
                    RawJson = json
                };

                if (dict.ContainsKey("code"))
                {
                    result.Code = Convert.ToInt32(dict["code"]);
                    result.Success = result.Code == 0;
                }

                if (dict.ContainsKey("msg"))
                {
                    result.Message = dict["msg"]?.ToString();
                }

                if (dict.ContainsKey("data"))
                {
                    result.Data = dict["data"];
                }

                if (dict.ContainsKey("errno"))
                {
                    result.Errno = Convert.ToInt32(dict["errno"]);
                }

                return result;
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    Success = false,
                    Code = -1,
                    Message = $"解析响应失败: {ex.Message}",
                    RawJson = json
                };
            }
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 设置认证信息
        /// </summary>
        public void SetAuth(string token, string userId, string groupToken = null)
        {
            _token = token;
            _userId = userId;
            _groupToken = groupToken;
        }

        /// <summary>
        /// 清除认证信息
        /// </summary>
        public void ClearAuth()
        {
            _token = null;
            _userId = null;
            _groupToken = null;
        }

        private void Log(string message)
        {
            Logger.Info($"[HttpApi] {message}");
            OnLog?.Invoke(message);
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_isDisposed)
                return;
            _isDisposed = true;

            _httpClient?.Dispose();
        }

        #endregion
    }

    #region API 响应模型

    /// <summary>
    /// API 响应结果
    /// </summary>
    public class ApiResponse
    {
        /// <summary>是否成功 (code == 0)</summary>
        public bool Success { get; set; }

        /// <summary>业务返回码</summary>
        public int Code { get; set; }

        /// <summary>HTTP 状态码</summary>
        public int HttpStatusCode { get; set; }

        /// <summary>消息</summary>
        public string Message { get; set; }

        /// <summary>错误码</summary>
        public int Errno { get; set; }

        /// <summary>响应数据</summary>
        public object Data { get; set; }

        /// <summary>原始JSON</summary>
        public string RawJson { get; set; }

        /// <summary>
        /// 检查是否为Token失效
        /// </summary>
        public bool IsTokenInvalid => Code == 401;

        /// <summary>
        /// 检查是否为设备掉线
        /// </summary>
        public bool IsDeviceOffline => Code == 403 && Errno == 50;

        /// <summary>
        /// 检查是否为重复操作
        /// </summary>
        public bool IsDuplicateOperation => Code == 1001;

        /// <summary>
        /// 检查是否为用户不存在
        /// </summary>
        public bool IsUserNotFound => Code == 1001;

        /// <summary>
        /// 检查是否为无用户ID错误
        /// </summary>
        public bool IsNoUserId => Code == -10243;

        /// <summary>
        /// 检查是否为消息错误
        /// </summary>
        public bool IsMessageError => Code == -10261;
    }

    #endregion

    #region 消息编码请求模型 (根据旺商聊深度连接协议第十五节)

    /// <summary>
    /// 消息编码请求
    /// </summary>
    public class EncodeMessageRequest
    {
        /// <summary>发送者ID</summary>
        public long FromId { get; set; }

        /// <summary>发送者昵称</summary>
        public string FromName { get; set; }

        /// <summary>发送者头像</summary>
        public string FromAvatar { get; set; } = "0";

        /// <summary>接收者ID (群ID或用户ID)</summary>
        public long ToId { get; set; }

        /// <summary>消息内容</summary>
        public string Content { get; set; }

        /// <summary>消息会话类型: MSG_KIND_P2P, MSG_KIND_GROUP</summary>
        public string MsgSession { get; set; } = "MSG_KIND_GROUP";

        /// <summary>消息格式: MSG_TEXT, MSG_IMAGE</summary>
        public string MsgFormat { get; set; } = "MSG_TEXT";

        /// <summary>消息角色: MSG_MINE, MSG_MEMBER</summary>
        public string MsgRole { get; set; } = "MSG_MINE";

        /// <summary>账号类型: ACCOUNT_MEMBER, ACCOUNT_SYSTEM</summary>
        public string AccountType { get; set; } = "ACCOUNT_MEMBER";

        /// <summary>设备类型</summary>
        public string MsgDevice { get; set; } = "Desktop";

        /// <summary>消息版本</summary>
        public int MsgVersion { get; set; } = 2;

        /// <summary>@列表</summary>
        public List<AtInfo> AtList { get; set; }

        /// <summary>
        /// 转换为字典(用于JSON序列化)
        /// </summary>
        public Dictionary<string, object> ToDictionary()
        {
            var msg = new Dictionary<string, object>
            {
                { "from", new Dictionary<string, object>
                    {
                        { "id", FromId },
                        { "name", FromName ?? "" },
                        { "avatar", FromAvatar }
                    }
                },
                { "to", new Dictionary<string, object>
                    {
                        { "id", ToId }
                    }
                },
                { "msgDevice", MsgDevice },
                { "created_at", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ") },
                { "msgSession", MsgSession },
                { "msgVersion", MsgVersion },
                { "accountType", AccountType },
                { "msgFormat", MsgFormat },
                { "msgRole", MsgRole },
                { "msgRingtone", "MSG_RINGTONE_NONE" },
                { "appoint", "MSG_APPOINT_NONE" }
            };

            // 处理@功能
            if (AtList != null && AtList.Count > 0)
            {
                var aiteInfoList = new List<Dictionary<string, object>>();
                foreach (var at in AtList)
                {
                    aiteInfoList.Add(new Dictionary<string, object>
                    {
                        { "uid", at.Uid },
                        { "nick", at.Nick },
                        { "start", at.Start.ToString() },
                        { "end", at.End.ToString() }
                    });
                }

                msg["aite"] = new Dictionary<string, object>
                {
                    { "content", new Dictionary<string, object>
                        {
                            { "data", Content },
                            { "title", "" },
                            { "style", "MSG_STYLE_PLAIN" },
                            { "color", "" }
                        }
                    },
                    { "AiTeInfo", aiteInfoList }
                };
            }
            else
            {
                // 普通消息
                msg["content"] = new Dictionary<string, object>
                {
                    { "data", Content },
                    { "title", "" },
                    { "style", "MSG_STYLE_PLAIN" },
                    { "color", "" }
                };
            }

            return new Dictionary<string, object> { { "msg", msg } };
        }
    }

    /// <summary>
    /// @信息 (根据旺商聊深度连接协议第十五节)
    /// </summary>
    public class AtInfo
    {
        /// <summary>被@用户的NIM ID</summary>
        public long Uid { get; set; }

        /// <summary>显示的昵称</summary>
        public string Nick { get; set; }

        /// <summary>@符号在消息中的起始位置</summary>
        public int Start { get; set; }

        /// <summary>@昵称的结束位置</summary>
        public int End { get; set; }
    }

    #endregion
}
