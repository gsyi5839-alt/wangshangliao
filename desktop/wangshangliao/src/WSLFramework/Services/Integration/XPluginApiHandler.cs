using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WSLFramework.Protocol;

namespace WSLFramework.Services
{
    /// <summary>
    /// XPlugin API处理器 - 解析和路由API请求
    /// 
    /// 支持的API格式 (与旧系统兼容):
    /// API名称|参数1|参数2|...|返回结果:Base64编码结果
    /// 
    /// 示例:
    /// 发送群消息（文本）|621705120|开:9 + 2 + 5 = 16|3962369093|1|0|返回结果:xxx
    /// ww_群禁言解禁|621705120|3962369093|2|返回结果:xxx
    /// </summary>
    public class XPluginApiHandler
    {
        #region 常量定义

        // API名称
        public const string API_SEND_GROUP_MSG = "发送群消息（文本）";
        public const string API_SEND_GROUP_MSG_TEXT = "发送群消息(文本版)";
        public const string API_GET_ONLINE_ACCOUNTS = "云信_获取在线账号";
        public const string API_GET_BOUND_GROUPS = "取绑定群";
        public const string API_GROUP_MUTE = "ww_群禁言解禁";
        public const string API_MODIFY_GROUP_CARD = "ww_改群名片";
        public const string API_ID_LOOKUP = "ww_ID互查";
        public const string API_GET_GROUP_MEMBERS = "ww_获取群成员";
        public const string API_GET_ALL_ACCOUNTS = "插件_获取所有账号";
        public const string API_GET_GROUPS = "取群群";
        public const string API_GET_USER_INFO = "ww_ID资料";
        public const string API_ADD_FRIEND = "ww_添加好友并备注_单向";
        public const string API_SEND_FRIEND_MSG = "发送好友消息";
        public const string API_FRAMEWORK_AUTH = "ww_xp框架接口";

        // 分隔符
        public const char SEPARATOR = '|';
        public const string RESULT_PREFIX = "返回结果:";

        #endregion

        #region 字段和属性

        private readonly XPluginService _xpluginService;
        
        /// <summary>日志事件</summary>
        public event Action<string> OnLog;

        /// <summary>API调用日志事件 (timestamp, apiCall, result)</summary>
        public event Action<DateTime, string, string> OnApiLog;

        #endregion

        #region 构造函数

        public XPluginApiHandler(XPluginService xpluginService)
        {
            _xpluginService = xpluginService ?? throw new ArgumentNullException(nameof(xpluginService));
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 处理API调用请求
        /// </summary>
        /// <param name="apiCall">API调用字符串</param>
        /// <returns>带返回结果的完整API字符串</returns>
        public async Task<string> HandleApiCallAsync(string apiCall)
        {
            if (string.IsNullOrEmpty(apiCall))
            {
                return BuildResult(apiCall, XPluginService.ApiResult.Error("空请求"));
            }

            try
            {
                // 解析API调用
                var (apiName, args) = ParseApiCall(apiCall);
                
                if (string.IsNullOrEmpty(apiName))
                {
                    return BuildResult(apiCall, XPluginService.ApiResult.Error("无效的API名称"));
                }

                Log($"处理API: {apiName}, 参数数量: {args.Length}");

                // 路由到对应的处理方法
                var result = await RouteApiCallAsync(apiName, args);

                // 记录API日志
                var fullResult = BuildResult(apiCall, result);
                OnApiLog?.Invoke(DateTime.Now, apiCall, result.ToBase64());

                return fullResult;
            }
            catch (Exception ex)
            {
                Log($"API处理异常: {ex.Message}");
                return BuildResult(apiCall, XPluginService.ApiResult.Error(ex.Message));
            }
        }

        /// <summary>
        /// 解析API调用字符串
        /// </summary>
        public (string ApiName, string[] Args) ParseApiCall(string apiCall)
        {
            if (string.IsNullOrEmpty(apiCall))
                return (null, Array.Empty<string>());

            var parts = apiCall.Split(SEPARATOR);
            if (parts.Length == 0)
                return (null, Array.Empty<string>());

            var apiName = parts[0];
            var args = new List<string>();

            for (int i = 1; i < parts.Length; i++)
            {
                // 跳过返回结果部分
                if (parts[i].StartsWith(RESULT_PREFIX))
                    continue;
                args.Add(parts[i]);
            }

            return (apiName, args.ToArray());
        }

        /// <summary>
        /// 构建带返回结果的API字符串
        /// </summary>
        public string BuildResult(string apiCall, XPluginService.ApiResult result)
        {
            // 移除已有的返回结果
            var cleanCall = apiCall;
            var resultIndex = apiCall.IndexOf(RESULT_PREFIX);
            if (resultIndex > 0)
            {
                cleanCall = apiCall.Substring(0, resultIndex - 1); // -1 移除分隔符
            }

            return $"{cleanCall}{SEPARATOR}{RESULT_PREFIX}{result.ToBase64()}";
        }

        /// <summary>
        /// 构建API调用字符串
        /// </summary>
        public string BuildApiCall(string apiName, params string[] args)
        {
            var parts = new List<string> { apiName };
            parts.AddRange(args);
            return string.Join(SEPARATOR.ToString(), parts);
        }

        #endregion

        #region API路由

        /// <summary>
        /// 路由API调用到对应的处理方法
        /// </summary>
        private async Task<XPluginService.ApiResult> RouteApiCallAsync(string apiName, string[] args)
        {
            switch (apiName)
            {
                // 发送群消息
                case API_SEND_GROUP_MSG:
                case API_SEND_GROUP_MSG_TEXT:
                    return await HandleSendGroupMessageAsync(args);

                // 获取在线账号
                case API_GET_ONLINE_ACCOUNTS:
                case API_GET_ALL_ACCOUNTS:
                    return await HandleGetOnlineAccountsAsync(args);

                // 获取绑定群
                case API_GET_BOUND_GROUPS:
                case API_GET_GROUPS:
                    return await HandleGetBoundGroupsAsync(args);

                // 群禁言解禁
                case API_GROUP_MUTE:
                    return await HandleGroupMuteAsync(args);

                // 修改群名片
                case API_MODIFY_GROUP_CARD:
                    return await HandleModifyGroupCardAsync(args);

                // ID互查
                case API_ID_LOOKUP:
                case API_GET_USER_INFO:
                    return await HandleIdLookupAsync(args);

                // 获取群成员
                case API_GET_GROUP_MEMBERS:
                    return await HandleGetGroupMembersAsync(args);

                // 框架接口 (心跳/认证)
                case API_FRAMEWORK_AUTH:
                    return await HandleFrameworkAuthAsync(args);

                default:
                    Log($"未知的API: {apiName}");
                    return XPluginService.ApiResult.Error($"未知的API: {apiName}");
            }
        }

        #endregion

        #region API处理方法

        /// <summary>
        /// 处理发送群消息
        /// 格式: 发送群消息（文本）|机器人号|消息内容|群号|类型|子类型
        /// </summary>
        private async Task<XPluginService.ApiResult> HandleSendGroupMessageAsync(string[] args)
        {
            if (args.Length < 3)
            {
                return XPluginService.ApiResult.Error("参数不足: 需要机器人号|消息内容|群号");
            }

            var robotId = args[0];
            var content = args[1];
            var groupId = args[2];
            var type = args.Length > 3 && int.TryParse(args[3], out var t) ? t : 1;
            var subType = args.Length > 4 && int.TryParse(args[4], out var st) ? st : 0;

            return await _xpluginService.SendGroupMessageAsync(robotId, content, groupId, type, subType);
        }

        /// <summary>
        /// 处理获取在线账号
        /// 格式: 云信_获取在线账号
        /// </summary>
        private async Task<XPluginService.ApiResult> HandleGetOnlineAccountsAsync(string[] args)
        {
            return await _xpluginService.GetOnlineAccountsAsync();
        }

        /// <summary>
        /// 处理获取绑定群
        /// 格式: 取绑定群|机器人号
        /// </summary>
        private async Task<XPluginService.ApiResult> HandleGetBoundGroupsAsync(string[] args)
        {
            var robotId = args.Length > 0 ? args[0] : "";
            return await _xpluginService.GetBoundGroupsAsync(robotId);
        }

        /// <summary>
        /// 处理群禁言解禁
        /// 格式: ww_群禁言解禁|机器人号|群号|操作(1=禁言,2=解禁)
        /// </summary>
        private async Task<XPluginService.ApiResult> HandleGroupMuteAsync(string[] args)
        {
            if (args.Length < 3)
            {
                return XPluginService.ApiResult.Error("参数不足: 需要机器人号|群号|操作");
            }

            var robotId = args[0];
            var groupId = args[1];
            var operation = int.TryParse(args[2], out var op) ? op : 2;

            return await _xpluginService.SetGroupMuteAsync(robotId, groupId, operation);
        }

        /// <summary>
        /// 处理修改群名片
        /// 格式: ww_改群名片|机器人号|群号|用户ID|新名片
        /// </summary>
        private async Task<XPluginService.ApiResult> HandleModifyGroupCardAsync(string[] args)
        {
            if (args.Length < 4)
            {
                return XPluginService.ApiResult.Error("参数不足: 需要机器人号|群号|用户ID|新名片");
            }

            var robotId = args[0];
            var groupId = args[1];
            var userId = args[2];
            var newCard = args[3];

            return await _xpluginService.ModifyGroupCardAsync(robotId, groupId, userId, newCard);
        }

        /// <summary>
        /// 处理ID互查
        /// 格式: ww_ID互查|ID
        /// </summary>
        private async Task<XPluginService.ApiResult> HandleIdLookupAsync(string[] args)
        {
            if (args.Length < 1)
            {
                return XPluginService.ApiResult.Error("参数不足: 需要ID");
            }

            return await _xpluginService.LookupIdAsync(args[0]);
        }

        /// <summary>
        /// 处理获取群成员
        /// 格式: ww_获取群成员|机器人号|群号
        /// </summary>
        private async Task<XPluginService.ApiResult> HandleGetGroupMembersAsync(string[] args)
        {
            if (args.Length < 2)
            {
                return XPluginService.ApiResult.Error("参数不足: 需要机器人号|群号");
            }

            return await _xpluginService.GetGroupMembersAsync(args[0], args[1]);
        }

        /// <summary>
        /// 处理框架认证
        /// 格式: ww_xp框架接口|操作|参数
        /// </summary>
        private async Task<XPluginService.ApiResult> HandleFrameworkAuthAsync(string[] args)
        {
            // 返回框架状态
            var status = new Dictionary<string, object>
            {
                { "running", _xpluginService.IsRunning },
                { "cdp_connected", _xpluginService.IsCDPConnected },
                { "online_accounts", _xpluginService.OnlineAccountCount },
                { "version", "1.0.0" },
                { "framework", "WSLFramework XPlugin" }
            };

            var json = new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(status);
            return XPluginService.ApiResult.Success(json);
        }

        #endregion

        #region 辅助方法

        private void Log(string message)
        {
            OnLog?.Invoke($"[XPluginAPI] {message}");
        }

        #endregion
    }
}
