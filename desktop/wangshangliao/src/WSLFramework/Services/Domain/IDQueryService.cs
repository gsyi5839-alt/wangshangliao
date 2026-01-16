using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading.Tasks;
using WSLFramework.Utils;

namespace WSLFramework.Services
{
    /// <summary>
    /// ID查询服务 - 根据旺商聊深度连接协议第十四节实现
    /// 支持三种ID体系的相互转换：
    /// - 旺商聊号: 6-10位数字 (如 621705120)
    /// - NIM accid: 10位数字 (如 1948408648)
    /// - 旺商聊uid: 7-8位数字 (如 9502248)
    /// </summary>
    public class IDQueryService
    {
        #region 单例模式

        private static readonly Lazy<IDQueryService> _instance =
            new Lazy<IDQueryService>(() => new IDQueryService());

        public static IDQueryService Instance => _instance.Value;

        #endregion

        #region 常量

        // ID类型
        public const int ID_TYPE_NIM_TO_WSL = 0;   // NIM ID → 旺商聊号
        public const int ID_TYPE_WSL_TO_NIM = 1;   // 旺商聊号 → NIM ID
        public const int ID_TYPE_GROUP = 2;        // 群组ID查询

        // 成功返回值特征 (Base64解码后长度)
        public const int SUCCESS_MIN_LENGTH = 90;
        public const int SUCCESS_MAX_LENGTH = 200;

        // 已知返回码
        public const int CODE_SUCCESS = 0;
        public const int CODE_UNAUTHORIZED = 401;
        public const int CODE_NOT_FOUND = 1001;
        public const int CODE_NO_USER_ID = -10243;
        public const int CODE_MSG_ERROR = -10261;

        #endregion

        #region 私有字段

        private CDPBridge _cdpBridge;
        private XPluginClient _xpluginClient;

        // ID映射缓存 (旺商聊号 -> NIM ID)
        private readonly ConcurrentDictionary<string, string> _wslToNimCache;
        // ID映射缓存 (NIM ID -> 旺商聊号)
        private readonly ConcurrentDictionary<string, string> _nimToWslCache;
        // 群组ID映射缓存 (旺商聊群号 -> NIM tid)
        private readonly ConcurrentDictionary<string, string> _groupIdCache;

        #endregion

        #region 事件

        public event Action<string> OnLog;

        #endregion

        #region 构造函数

        private IDQueryService()
        {
            _wslToNimCache = new ConcurrentDictionary<string, string>();
            _nimToWslCache = new ConcurrentDictionary<string, string>();
            _groupIdCache = new ConcurrentDictionary<string, string>();

            // 初始化已知ID映射 (从文档中提取)
            InitializeKnownMappings();
        }

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化服务
        /// </summary>
        public void Initialize(CDPBridge cdpBridge, XPluginClient xpluginClient = null)
        {
            _cdpBridge = cdpBridge;
            _xpluginClient = xpluginClient;
        }

        /// <summary>
        /// 初始化已知ID映射
        /// 根据旺商聊深度连接协议第十四节的已知映射表
        /// </summary>
        private void InitializeKnownMappings()
        {
            // 用户ID映射 (旺商聊号 -> NIM accid)
            AddMapping("621705120", "1948408648"); // 机器人账号
            AddMapping("781361487", "1628907626"); // 用户1
            AddMapping("324085447", "1719936235"); // 用户2
            AddMapping("184800772", "1700699909"); // 用户3
            AddMapping("82840376", "1391351554");  // 用户5
            AddMapping("982576571", "2092166259"); // 用户6

            // 群组ID映射 (旺商聊群号 -> NIM tid)
            AddGroupMapping("3962369093", "40821608989");

            Log($"已初始化 {_wslToNimCache.Count} 个用户ID映射, {_groupIdCache.Count} 个群组ID映射");
        }

        /// <summary>
        /// 添加ID映射
        /// </summary>
        public void AddMapping(string wslId, string nimId)
        {
            if (!string.IsNullOrEmpty(wslId) && !string.IsNullOrEmpty(nimId))
            {
                _wslToNimCache[wslId] = nimId;
                _nimToWslCache[nimId] = wslId;
            }
        }

        /// <summary>
        /// 添加群组ID映射
        /// </summary>
        public void AddGroupMapping(string wslGroupId, string nimTid)
        {
            if (!string.IsNullOrEmpty(wslGroupId) && !string.IsNullOrEmpty(nimTid))
            {
                _groupIdCache[wslGroupId] = nimTid;
            }
        }

        #endregion

        #region 缓存查询

        /// <summary>
        /// 从缓存获取NIM ID
        /// </summary>
        public string GetNimIdFromCache(string wslId)
        {
            return _wslToNimCache.TryGetValue(wslId, out var nimId) ? nimId : null;
        }

        /// <summary>
        /// 从缓存获取旺商聊号
        /// </summary>
        public string GetWslIdFromCache(string nimId)
        {
            return _nimToWslCache.TryGetValue(nimId, out var wslId) ? wslId : null;
        }

        /// <summary>
        /// 从缓存获取群组NIM ID
        /// </summary>
        public string GetGroupNimIdFromCache(string wslGroupId)
        {
            return _groupIdCache.TryGetValue(wslGroupId, out var nimTid) ? nimTid : null;
        }

        #endregion

        #region TCP API查询 (通过xplugin)

        /// <summary>
        /// 通过TCP API查询ID (ww_ID互查)
        /// 命令格式: ww_ID互查|{机器人号}|{查询ID}
        /// </summary>
        public async Task<IDQueryResult> QueryIdViaTcpAsync(string robotId, string queryId)
        {
            var result = new IDQueryResult { QueryId = queryId };

            // 先检查缓存
            var cachedNimId = GetNimIdFromCache(queryId);
            if (!string.IsNullOrEmpty(cachedNimId))
            {
                result.Success = true;
                result.WslId = queryId;
                result.NimId = cachedNimId;
                result.FromCache = true;
                return result;
            }

            // 检查是否为NIM ID查询
            var cachedWslId = GetWslIdFromCache(queryId);
            if (!string.IsNullOrEmpty(cachedWslId))
            {
                result.Success = true;
                result.WslId = cachedWslId;
                result.NimId = queryId;
                result.FromCache = true;
                return result;
            }

            // 通过xplugin查询
            if (_xpluginClient != null && _xpluginClient.IsConnected)
            {
                try
                {
                    var response = await _xpluginClient.QueryIdAsync(robotId, queryId);
                    result = ParseTcpResponse(queryId, response);

                    // 缓存成功结果
                    if (result.Success && !string.IsNullOrEmpty(result.WslId) && !string.IsNullOrEmpty(result.NimId))
                    {
                        AddMapping(result.WslId, result.NimId);
                    }
                }
                catch (Exception ex)
                {
                    result.Error = ex.Message;
                    Log($"TCP ID查询异常: {ex.Message}");
                }
            }
            else
            {
                result.Error = "xplugin未连接";
            }

            return result;
        }

        /// <summary>
        /// 解析TCP响应
        /// 返回格式: ww_ID互查|{机器人号}|{查询ID}|返回结果:{Base64}
        /// </summary>
        private IDQueryResult ParseTcpResponse(string queryId, string response)
        {
            var result = new IDQueryResult { QueryId = queryId };

            if (string.IsNullOrEmpty(response))
            {
                result.Error = "空响应";
                return result;
            }

            // 提取Base64返回值
            var idx = response.IndexOf("返回结果:");
            if (idx < 0)
            {
                result.Error = "无返回结果";
                return result;
            }

            var b64Result = response.Substring(idx + 5).Trim();
            result.RawBase64 = b64Result;

            try
            {
                var decoded = Convert.FromBase64String(b64Result);
                result.DecodedLength = decoded.Length;

                // 根据返回长度判断成功/失败
                // 成功: 100-150字节的加密数据
                // 失败: 通常<50字节或>200字节
                if (decoded.Length >= SUCCESS_MIN_LENGTH && decoded.Length <= SUCCESS_MAX_LENGTH)
                {
                    result.Success = true;
                    result.WslId = queryId;
                    // NIM ID需要从加密数据中解析，这里无法直接获取
                    Log($"ID查询成功: {queryId}, 数据长度={decoded.Length}");
                }
                else
                {
                    // 尝试解析JSON错误信息
                    var text = Encoding.UTF8.GetString(decoded);
                    if (text.Contains("code"))
                    {
                        result.Error = text;
                    }
                    else
                    {
                        result.Error = $"查询失败，数据长度={decoded.Length}";
                    }
                    Log($"ID查询失败: {queryId}, {result.Error}");
                }
            }
            catch (Exception ex)
            {
                result.Error = $"Base64解码失败: {ex.Message}";
            }

            return result;
        }

        #endregion

        #region HTTP API查询

        /// <summary>
        /// 通过HTTP API查询用户信息
        /// POST /v1/plugins/get-userinfo-by-id
        /// </summary>
        public async Task<IDQueryResult> QueryUserInfoViaHttpAsync(long queryId, int type = ID_TYPE_WSL_TO_NIM)
        {
            var result = new IDQueryResult { QueryId = queryId.ToString() };

            try
            {
                var api = WangShangLiaoHttpApi.Instance;
                var response = await api.GetUserInfoByWslIdAsync(queryId, type);

                result.HttpCode = response.Code;

                if (response.Success)
                {
                    result.Success = true;
                    if (type == ID_TYPE_WSL_TO_NIM)
                    {
                        result.WslId = queryId.ToString();
                    }
                    else
                    {
                        result.NimId = queryId.ToString();
                    }
                    // 从response.Data中提取详细信息
                    if (response.Data != null)
                    {
                        result.UserInfo = response.Data;
                    }
                    Log($"HTTP ID查询成功: {queryId}");
                }
                else
                {
                    result.Error = response.Message;
                    result.HttpCode = response.Code;
                    Log($"HTTP ID查询失败: {queryId}, code={response.Code}, msg={response.Message}");
                }
            }
            catch (Exception ex)
            {
                result.Error = ex.Message;
                Log($"HTTP ID查询异常: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// 通过HTTP API查询群组ID
        /// POST /v1/plugins/get-gid
        /// </summary>
        public async Task<IDQueryResult> QueryGroupIdViaHttpAsync(string groupId)
        {
            var result = new IDQueryResult { QueryId = groupId, IsGroup = true };

            // 先检查缓存
            var cachedNimTid = GetGroupNimIdFromCache(groupId);
            if (!string.IsNullOrEmpty(cachedNimTid))
            {
                result.Success = true;
                result.WslId = groupId;
                result.NimId = cachedNimTid;
                result.FromCache = true;
                return result;
            }

            try
            {
                var api = WangShangLiaoHttpApi.Instance;
                var response = await api.GetGroupGidAsync(groupId);

                result.HttpCode = response.Code;

                if (response.Success)
                {
                    result.Success = true;
                    result.WslId = groupId;
                    if (response.Data != null)
                    {
                        var data = response.Data as System.Collections.Generic.Dictionary<string, object>;
                        if (data != null && data.ContainsKey("groupAccount"))
                        {
                            result.GroupAccount = data["groupAccount"]?.ToString();
                        }
                    }
                    // 缓存结果
                    if (!string.IsNullOrEmpty(result.NimId))
                    {
                        AddGroupMapping(groupId, result.NimId);
                    }
                    Log($"群组ID查询成功: {groupId}");
                }
                else
                {
                    result.Error = response.Message;
                    Log($"群组ID查询失败: {groupId}, code={response.Code}, msg={response.Message}");
                }
            }
            catch (Exception ex)
            {
                result.Error = ex.Message;
                Log($"群组ID查询异常: {ex.Message}");
            }

            return result;
        }

        #endregion

        #region 批量查询

        /// <summary>
        /// 批量查询ID
        /// </summary>
        public async Task<System.Collections.Generic.Dictionary<string, IDQueryResult>> BatchQueryAsync(
            string robotId, string[] queryIds)
        {
            var results = new System.Collections.Generic.Dictionary<string, IDQueryResult>();

            foreach (var id in queryIds)
            {
                var result = await QueryIdViaTcpAsync(robotId, id);
                results[id] = result;

                // 添加延迟防止请求过快
                await Task.Delay(100);
            }

            return results;
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 判断是否为有效的旺商聊号格式
        /// </summary>
        public static bool IsValidWslId(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            if (id.Length < 6 || id.Length > 10) return false;
            foreach (var c in id)
            {
                if (!char.IsDigit(c)) return false;
            }
            return true;
        }

        /// <summary>
        /// 判断是否为有效的NIM ID格式
        /// </summary>
        public static bool IsValidNimId(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            if (id.Length != 10) return false;
            foreach (var c in id)
            {
                if (!char.IsDigit(c)) return false;
            }
            return true;
        }

        /// <summary>
        /// 获取缓存统计
        /// </summary>
        public string GetCacheStats()
        {
            return $"用户映射: {_wslToNimCache.Count}, 群组映射: {_groupIdCache.Count}";
        }

        private void Log(string message)
        {
            Logger.Info($"[IDQuery] {message}");
            OnLog?.Invoke(message);
        }

        #endregion
    }

    /// <summary>
    /// ID查询结果
    /// </summary>
    public class IDQueryResult
    {
        /// <summary>查询的ID</summary>
        public string QueryId { get; set; }
        /// <summary>是否成功</summary>
        public bool Success { get; set; }
        /// <summary>旺商聊号</summary>
        public string WslId { get; set; }
        /// <summary>NIM accid</summary>
        public string NimId { get; set; }
        /// <summary>旺商聊uid</summary>
        public string WslUid { get; set; }
        /// <summary>群组账号</summary>
        public string GroupAccount { get; set; }
        /// <summary>是否为群组查询</summary>
        public bool IsGroup { get; set; }
        /// <summary>是否来自缓存</summary>
        public bool FromCache { get; set; }
        /// <summary>原始Base64返回</summary>
        public string RawBase64 { get; set; }
        /// <summary>解码后数据长度</summary>
        public int DecodedLength { get; set; }
        /// <summary>HTTP返回码</summary>
        public int HttpCode { get; set; }
        /// <summary>错误信息</summary>
        public string Error { get; set; }
        /// <summary>用户详细信息</summary>
        public object UserInfo { get; set; }

        public override string ToString()
        {
            if (Success)
            {
                return $"成功: WSL={WslId}, NIM={NimId}" + (FromCache ? " (缓存)" : "");
            }
            else
            {
                return $"失败: {Error}";
            }
        }
    }
}
