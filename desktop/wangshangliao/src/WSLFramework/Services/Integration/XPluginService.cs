using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using WSLFramework.Protocol;

namespace WSLFramework.Services
{
    /// <summary>
    /// XPlugin服务 - 自主实现的旺商聊框架插件系统
    /// 参考旧程序xplugin.exe的功能,通过CDP与旺商聊Electron应用交互
    /// 
    /// 核心架构:
    /// 主程序 → XPluginService → CDPBridge → 旺商聊Electron
    /// 
    /// 支持的API (参考旧系统):
    /// - 发送群消息（文本）: 发送群聊文本消息
    /// - 云信_获取在线账号: 获取当前在线的机器人账号
    /// - 取绑定群: 获取机器人绑定的群列表
    /// - ww_群禁言解禁: 群全员禁言/解禁
    /// - ww_改群名片: 修改群成员名片
    /// - ww_ID互查: 旺商聊ID和NIM accid互查
    /// </summary>
    public class XPluginService : IDisposable
    {
        #region 单例模式
        private static XPluginService _instance;
        private static readonly object _lock = new object();
        
        public static XPluginService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                            _instance = new XPluginService();
                    }
                }
                return _instance;
            }
        }
        #endregion

        #region 私有字段
        private CDPBridge _cdpBridge;
        private bool _isRunning;
        private readonly ConcurrentDictionary<string, AccountInfo> _onlineAccounts;
        private readonly ConcurrentDictionary<string, List<string>> _boundGroups;
        private readonly JavaScriptSerializer _jsonSerializer;
        private CancellationTokenSource _cts;
        private System.Timers.Timer _statusTimer;
        #endregion

        #region 事件
        /// <summary>日志事件</summary>
        public event Action<string> OnLog;
        /// <summary>API调用事件 (apiName, params, result)</summary>
        public event Action<string, string[], string> OnApiCall;
        /// <summary>消息接收事件 (groupId, senderId, content) - 预留扩展</summary>
#pragma warning disable CS0067 // 事件预留给将来使用
        public event Action<string, string, string> OnMessageReceived;
#pragma warning restore CS0067
        /// <summary>状态变化事件</summary>
        public event Action<bool> OnStatusChanged;
        #endregion

        #region 公共属性
        /// <summary>是否正在运行</summary>
        public bool IsRunning => _isRunning;
        /// <summary>CDP是否已连接</summary>
        public bool IsCDPConnected => _cdpBridge?.IsConnected ?? false;
        /// <summary>当前在线账号数</summary>
        public int OnlineAccountCount => _onlineAccounts.Count;
        /// <summary>配置端口 (默认14746,与旧系统一致)</summary>
        public ushort Port { get; set; } = 14746;
        #endregion

        #region 构造函数
        private XPluginService()
        {
            _onlineAccounts = new ConcurrentDictionary<string, AccountInfo>();
            _boundGroups = new ConcurrentDictionary<string, List<string>>();
            _jsonSerializer = new JavaScriptSerializer();
            _cts = new CancellationTokenSource();
        }
        #endregion

        #region 初始化和启动
        /// <summary>
        /// 初始化服务
        /// </summary>
        public void Initialize(CDPBridge cdpBridge)
        {
            _cdpBridge = cdpBridge;
            Log("XPlugin服务初始化完成");
        }

        /// <summary>
        /// 启动服务
        /// </summary>
        public async Task<bool> StartAsync()
        {
            if (_isRunning)
            {
                Log("XPlugin服务已经在运行");
                return true;
            }

            try
            {
                Log("正在启动XPlugin服务...");

                // 检查CDP连接
                if (_cdpBridge == null)
                {
                    Log("错误: CDP桥接未初始化");
                    return false;
                }

                if (!_cdpBridge.IsConnected)
                {
                    Log("CDP未连接,尝试连接...");
                    var connected = await _cdpBridge.ConnectAsync();
                    if (!connected)
                    {
                        Log("错误: CDP连接失败");
                        return false;
                    }
                }

                // 获取当前登录账号信息
                var userInfo = await _cdpBridge.GetCurrentUserInfoAsync();
                if (userInfo != null && !string.IsNullOrEmpty(userInfo.wwid))
                {
                    var account = new AccountInfo
                    {
                        WWID = userInfo.wwid,
                        NimId = userInfo.nimId,
                        Nickname = userInfo.nickname,
                        IsOnline = true,
                        LoginTime = DateTime.Now
                    };
                    _onlineAccounts[userInfo.wwid] = account;
                    Log($"检测到在线账号: {userInfo.wwid} ({userInfo.nickname})");
                }

                // 启动状态监控定时器
                StartStatusMonitor();

                _isRunning = true;
                OnStatusChanged?.Invoke(true);
                Log("XPlugin服务启动成功");
                return true;
            }
            catch (Exception ex)
            {
                Log($"XPlugin服务启动失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 停止服务
        /// </summary>
        public void Stop()
        {
            if (!_isRunning)
                return;

            try
            {
                Log("正在停止XPlugin服务...");
                
                _cts.Cancel();
                _statusTimer?.Stop();
                _statusTimer?.Dispose();
                _onlineAccounts.Clear();
                _boundGroups.Clear();

                _isRunning = false;
                OnStatusChanged?.Invoke(false);
                Log("XPlugin服务已停止");
            }
            catch (Exception ex)
            {
                Log($"停止服务时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 启动状态监控
        /// </summary>
        private void StartStatusMonitor()
        {
            _statusTimer = new System.Timers.Timer(30000); // 30秒检查一次
            _statusTimer.Elapsed += async (s, e) =>
            {
                try
                {
                    if (_cdpBridge != null && _cdpBridge.IsConnected)
                    {
                        // 刷新账号状态
                        await RefreshAccountStatusAsync();
                    }
                }
                catch (Exception ex)
                {
                    Log($"状态监控出错: {ex.Message}");
                }
            };
            _statusTimer.Start();
        }

        /// <summary>
        /// 刷新账号状态
        /// </summary>
        private async Task RefreshAccountStatusAsync()
        {
            try
            {
                var userInfo = await _cdpBridge.GetCurrentUserInfoAsync();
                if (userInfo != null && !string.IsNullOrEmpty(userInfo.wwid))
                {
                    if (_onlineAccounts.TryGetValue(userInfo.wwid, out var account))
                    {
                        account.IsOnline = true;
                        account.Nickname = userInfo.nickname;
                    }
                }
            }
            catch
            {
                // 静默处理
            }
        }
        #endregion

        #region API实现 - 发送群消息

        /// <summary>
        /// 发送群消息（文本）
        /// 格式: 机器人号|消息内容|群号|类型|子类型
        /// </summary>
        public async Task<ApiResult> SendGroupMessageAsync(string robotId, string content, string groupId, int type = 1, int subType = 0)
        {
            var args = new[] { robotId, content, groupId, type.ToString(), subType.ToString() };
            Log($"[API] 发送群消息（文本）| {robotId} | {groupId} | {content.Substring(0, Math.Min(50, content.Length))}...");

            try
            {
                if (!_isRunning || !IsCDPConnected)
                {
                    return ApiResult.Error("服务未运行或CDP未连接");
                }

                // 通过CDP发送消息
                var success = await _cdpBridge.SendGroupMessageAsync(groupId, content);
                
                var result = success 
                    ? ApiResult.Success("发送成功") 
                    : ApiResult.Error("发送失败");

                OnApiCall?.Invoke("发送群消息（文本）", args, result.ToBase64());
                return result;
            }
            catch (Exception ex)
            {
                Log($"发送群消息失败: {ex.Message}");
                return ApiResult.Error(ex.Message);
            }
        }

        #endregion

        #region API实现 - 获取在线账号

        /// <summary>
        /// 云信_获取在线账号
        /// 返回当前在线的机器人账号列表
        /// </summary>
        public async Task<ApiResult> GetOnlineAccountsAsync()
        {
            Log("[API] 云信_获取在线账号");

            try
            {
                if (!_isRunning)
                {
                    return ApiResult.Error("服务未运行");
                }

                // 刷新账号状态
                await RefreshAccountStatusAsync();

                // 构建返回数据
                var accounts = new List<Dictionary<string, object>>();
                foreach (var kvp in _onlineAccounts)
                {
                    if (kvp.Value.IsOnline)
                    {
                        accounts.Add(new Dictionary<string, object>
                        {
                            { "wwid", kvp.Key },
                            { "nimId", kvp.Value.NimId },
                            { "nickname", kvp.Value.Nickname },
                            { "loginTime", kvp.Value.LoginTime.ToString("yyyy-MM-dd HH:mm:ss") }
                        });
                    }
                }

                var result = ApiResult.Success(_jsonSerializer.Serialize(accounts));
                OnApiCall?.Invoke("云信_获取在线账号", Array.Empty<string>(), result.ToBase64());
                return result;
            }
            catch (Exception ex)
            {
                Log($"获取在线账号失败: {ex.Message}");
                return ApiResult.Error(ex.Message);
            }
        }

        #endregion

        #region API实现 - 取绑定群

        /// <summary>
        /// 取绑定群
        /// 获取指定机器人绑定的群列表
        /// </summary>
        public async Task<ApiResult> GetBoundGroupsAsync(string robotId)
        {
            var args = new[] { robotId };
            Log($"[API] 取绑定群 | {robotId}");

            try
            {
                if (!_isRunning || !IsCDPConnected)
                {
                    return ApiResult.Error("服务未运行或CDP未连接");
                }

                // 通过CDP获取群列表
                var groups = await _cdpBridge.GetGroupListAsync();
                
                if (groups != null)
                {
                    // 更新缓存
                    var groupIds = new List<string>();
                    foreach (var group in groups)
                    {
                        if (!string.IsNullOrEmpty(group.groupId))
                        {
                            groupIds.Add(group.groupId);
                        }
                    }
                    _boundGroups[robotId] = groupIds;

                    var result = ApiResult.Success(_jsonSerializer.Serialize(groups));
                    OnApiCall?.Invoke("取绑定群", args, result.ToBase64());
                    return result;
                }

                return ApiResult.Error("获取群列表失败");
            }
            catch (Exception ex)
            {
                Log($"获取绑定群失败: {ex.Message}");
                return ApiResult.Error(ex.Message);
            }
        }

        #endregion

        #region API实现 - 群禁言解禁

        /// <summary>
        /// ww_群禁言解禁
        /// 操作: 1=禁言, 2=解禁
        /// </summary>
        public async Task<ApiResult> SetGroupMuteAsync(string robotId, string groupId, int operation)
        {
            var args = new[] { robotId, groupId, operation.ToString() };
            var opName = operation == 1 ? "禁言" : "解禁";
            Log($"[API] ww_群禁言解禁 | {robotId} | {groupId} | {opName}");

            try
            {
                if (!_isRunning || !IsCDPConnected)
                {
                    return ApiResult.Error("服务未运行或CDP未连接");
                }

                // 通过CDP设置禁言状态
                bool mute = operation == 1;
                var success = await _cdpBridge.SetGroupMuteAsync(groupId, mute);

                var result = success 
                    ? ApiResult.Success($"群{opName}成功") 
                    : ApiResult.Error($"群{opName}失败");

                OnApiCall?.Invoke("ww_群禁言解禁", args, result.ToBase64());
                return result;
            }
            catch (Exception ex)
            {
                Log($"群禁言解禁失败: {ex.Message}");
                return ApiResult.Error(ex.Message);
            }
        }

        #endregion

        #region API实现 - 修改群名片

        /// <summary>
        /// ww_改群名片
        /// 修改群成员的群内昵称
        /// </summary>
        public async Task<ApiResult> ModifyGroupCardAsync(string robotId, string groupId, string userId, string newCard)
        {
            var args = new[] { robotId, groupId, userId, newCard };
            Log($"[API] ww_改群名片 | {robotId} | {groupId} | {userId} | {newCard}");

            try
            {
                if (!_isRunning || !IsCDPConnected)
                {
                    return ApiResult.Error("服务未运行或CDP未连接");
                }

                // 通过CDP修改群名片 (使用 ModifyMemberCardAsync 或 UpdateMemberCardAsync)
                var success = await _cdpBridge.ModifyMemberCardAsync(groupId, userId, newCard);

                var result = success 
                    ? ApiResult.Success("修改成功") 
                    : ApiResult.Error("修改失败");

                OnApiCall?.Invoke("ww_改群名片", args, result.ToBase64());
                return result;
            }
            catch (Exception ex)
            {
                Log($"修改群名片失败: {ex.Message}");
                return ApiResult.Error(ex.Message);
            }
        }

        #endregion

        #region API实现 - ID互查

        /// <summary>
        /// ww_ID互查
        /// 旺商聊ID和NIM accid互相查询
        /// </summary>
        public async Task<ApiResult> LookupIdAsync(string id)
        {
            var args = new[] { id };
            Log($"[API] ww_ID互查 | {id}");

            try
            {
                if (!_isRunning || !IsCDPConnected)
                {
                    return ApiResult.Error("服务未运行或CDP未连接");
                }

                // 通过CDP查询ID (使用 GetUserInfoAsync)
                var userInfo = await _cdpBridge.GetUserInfoAsync(id);
                
                if (userInfo != null)
                {
                    var data = new Dictionary<string, object>
                    {
                        { "wwid", userInfo.wwid ?? id },
                        { "nimId", userInfo.nimId ?? "" },
                        { "nickname", userInfo.nickname ?? "" },
                        { "avatar", userInfo.avatar ?? "" }
                    };
                    var result = ApiResult.Success(_jsonSerializer.Serialize(data));
                    OnApiCall?.Invoke("ww_ID互查", args, result.ToBase64());
                    return result;
                }

                return ApiResult.Error("ID查询失败");
            }
            catch (Exception ex)
            {
                Log($"ID互查失败: {ex.Message}");
                return ApiResult.Error(ex.Message);
            }
        }

        #endregion

        #region API实现 - 获取群成员

        /// <summary>
        /// ww_获取群成员
        /// 获取群成员列表
        /// </summary>
        public async Task<ApiResult> GetGroupMembersAsync(string robotId, string groupId)
        {
            var args = new[] { robotId, groupId };
            Log($"[API] ww_获取群成员 | {robotId} | {groupId}");

            try
            {
                if (!_isRunning || !IsCDPConnected)
                {
                    return ApiResult.Error("服务未运行或CDP未连接");
                }

                // 通过CDP获取群成员
                var members = await _cdpBridge.GetGroupMembersAsync(groupId);
                
                if (members != null)
                {
                    var result = ApiResult.Success(_jsonSerializer.Serialize(members));
                    OnApiCall?.Invoke("ww_获取群成员", args, result.ToBase64());
                    return result;
                }

                return ApiResult.Error("获取群成员失败");
            }
            catch (Exception ex)
            {
                Log($"获取群成员失败: {ex.Message}");
                return ApiResult.Error(ex.Message);
            }
        }

        #endregion

        #region 辅助方法

        private void Log(string message)
        {
            OnLog?.Invoke($"[XPlugin] {message}");
        }

        public void Dispose()
        {
            Stop();
            _cts?.Dispose();
            _statusTimer?.Dispose();
        }

        #endregion

        #region 内部类

        /// <summary>
        /// 账号信息
        /// </summary>
        public class AccountInfo
        {
            public string WWID { get; set; }
            public string NimId { get; set; }
            public string Nickname { get; set; }
            public bool IsOnline { get; set; }
            public DateTime LoginTime { get; set; }
        }

        /// <summary>
        /// API调用结果
        /// </summary>
        public class ApiResult
        {
            public int Code { get; set; }
            public string Message { get; set; }
            public string Data { get; set; }

            public bool IsSuccess => Code == 0;

            public static ApiResult Success(string data = null)
            {
                return new ApiResult { Code = 0, Message = "OK", Data = data };
            }

            public static ApiResult Error(string message)
            {
                return new ApiResult { Code = -1, Message = message };
            }

            /// <summary>
            /// 转换为Base64编码的结果字符串 (与旧系统兼容)
            /// </summary>
            public string ToBase64()
            {
                var json = new JavaScriptSerializer().Serialize(this);
                return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
            }
        }

        #endregion
    }
}
