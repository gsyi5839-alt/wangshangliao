using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace WSLFramework.Protocol
{
    /// <summary>
    /// ZCG API 处理器 - 处理所有API调用请求和响应
    /// </summary>
    public class ZCGApiHandler : IDisposable
    {
        #region 私有字段
        private readonly ConcurrentDictionary<string, ApiRequest> _pendingRequests;
        private readonly ConcurrentQueue<ApiRequest> _requestQueue;
        private readonly SemaphoreSlim _queueSemaphore;
        private readonly CancellationTokenSource _cts;
        private readonly object _lockObj = new object();
        private int _requestIdCounter;
        private bool _disposed;
        #endregion

        #region 事件
        /// <summary>API调用事件</summary>
        public event EventHandler<ApiCallEventArgs> OnApiCall;
        /// <summary>API响应事件</summary>
        public event EventHandler<ApiResponseEventArgs> OnApiResponse;
        /// <summary>API错误事件</summary>
        public event EventHandler<ApiErrorEventArgs> OnApiError;
        #endregion

        public ZCGApiHandler()
        {
            _pendingRequests = new ConcurrentDictionary<string, ApiRequest>();
            _requestQueue = new ConcurrentQueue<ApiRequest>();
            _queueSemaphore = new SemaphoreSlim(0);
            _cts = new CancellationTokenSource();
            _requestIdCounter = 0;
        }

        #region API 调用方法

        /// <summary>
        /// 取群群 - 获取账号所在的群列表
        /// </summary>
        public Task<string> GetGroupsAsync(string account, int timeoutMs = 5000)
        {
            return SendApiRequestAsync(ZCGApiFormat.API_GET_GROUPS, timeoutMs, account);
        }

        /// <summary>
        /// 发送群消息(文本版)
        /// </summary>
        /// <param name="account">登录账号</param>
        /// <param name="content">消息内容</param>
        /// <param name="groupId">群号</param>
        /// <param name="type">类型 (1=普通, 2=@消息)</param>
        /// <param name="flag">标志 (0=普通)</param>
        public Task<string> SendGroupMessageAsync(string account, string content, string groupId, int type = 1, int flag = 0, int timeoutMs = 5000)
        {
            return SendApiRequestAsync(ZCGApiFormat.API_SEND_GROUP_MSG, timeoutMs, 
                account, content, groupId, type.ToString(), flag.ToString());
        }

        /// <summary>
        /// 插件_获取所有账号
        /// </summary>
        public Task<string> GetAllAccountsAsync(int timeoutMs = 5000)
        {
            return SendApiRequestAsync(ZCGApiFormat.API_GET_ALL_ACCOUNTS, timeoutMs);
        }

        /// <summary>
        /// ww_群禁言解禁
        /// </summary>
        /// <param name="account">登录账号</param>
        /// <param name="groupId">群号</param>
        /// <param name="mode">模式 (1=禁言, 2=解禁)</param>
        public Task<string> GroupMuteAsync(string account, string groupId, int mode, int timeoutMs = 5000)
        {
            return SendApiRequestAsync(ZCGApiFormat.API_GROUP_MUTE, timeoutMs, account, groupId, mode.ToString());
        }

        /// <summary>
        /// ww_获取群成员
        /// </summary>
        public Task<string> GetGroupMembersAsync(string account, string groupId, int timeoutMs = 10000)
        {
            return SendApiRequestAsync(ZCGApiFormat.API_GET_GROUP_MEMBERS, timeoutMs, account, groupId);
        }

        /// <summary>
        /// ww_修改群名片
        /// </summary>
        public Task<string> ModifyGroupCardAsync(string account, string groupId, string userId, string nickname, int timeoutMs = 5000)
        {
            return SendApiRequestAsync(ZCGApiFormat.API_MODIFY_GROUP_CARD, timeoutMs, account, groupId, userId, nickname);
        }

        /// <summary>
        /// ww_ID资料 - 获取用户资料
        /// </summary>
        public Task<string> GetUserInfoAsync(string account, string userId, int timeoutMs = 5000)
        {
            return SendApiRequestAsync(ZCGApiFormat.API_GET_USER_INFO, timeoutMs, account, userId);
        }

        /// <summary>
        /// ww_添加好友并备注_单向
        /// </summary>
        public Task<string> AddFriendAsync(string account, string userId, string remark, int timeoutMs = 5000)
        {
            return SendApiRequestAsync(ZCGApiFormat.API_ADD_FRIEND, timeoutMs, account, userId, remark);
        }

        /// <summary>
        /// 发送好友消息
        /// </summary>
        public Task<string> SendFriendMessageAsync(string account, string content, string friendId, int timeoutMs = 5000)
        {
            return SendApiRequestAsync(ZCGApiFormat.API_SEND_FRIEND_MSG, timeoutMs, account, content, friendId);
        }

        /// <summary>
        /// ww_xp框架接口 - 框架验证
        /// </summary>
        public Task<string> FrameworkAuthAsync(string version, string username, string key, string xx, string time1, string time2, int timeoutMs = 10000)
        {
            return SendApiRequestAsync(ZCGApiFormat.API_FRAMEWORK_AUTH, timeoutMs, version, username, key, xx, time1, time2);
        }

        #endregion

        #region 核心方法

        /// <summary>
        /// 发送API请求
        /// </summary>
        private async Task<string> SendApiRequestAsync(string apiName, int timeoutMs, params string[] args)
        {
            var requestId = GenerateRequestId();
            var apiCall = ZCGApiFormat.BuildApiCall(apiName, args);
            
            var request = new ApiRequest
            {
                RequestId = requestId,
                ApiName = apiName,
                ApiCall = apiCall,
                CreateTime = DateTime.Now,
                TimeoutMs = timeoutMs,
                TaskCompletionSource = new TaskCompletionSource<string>()
            };

            _pendingRequests[requestId] = request;

            try
            {
                // 触发API调用事件
                OnApiCall?.Invoke(this, new ApiCallEventArgs
                {
                    RequestId = requestId,
                    ApiName = apiName,
                    ApiCall = apiCall,
                    Args = args
                });

                // 等待响应或超时
                using (var cts = new CancellationTokenSource(timeoutMs))
                {
                    var completedTask = await Task.WhenAny(
                        request.TaskCompletionSource.Task,
                        Task.Delay(timeoutMs, cts.Token)
                    );

                    if (completedTask == request.TaskCompletionSource.Task)
                    {
                        return await request.TaskCompletionSource.Task;
                    }
                    else
                    {
                        throw new TimeoutException($"API请求超时: {apiName}");
                    }
                }
            }
            finally
            {
                _pendingRequests.TryRemove(requestId, out _);
            }
        }

        /// <summary>
        /// 处理API响应
        /// </summary>
        public void HandleResponse(string apiCall, string base64Result)
        {
            var (apiName, args, _) = ZCGApiFormat.ParseApiCall(apiCall);
            
            // 查找匹配的待处理请求
            foreach (var kvp in _pendingRequests)
            {
                var request = kvp.Value;
                if (request.ApiName == apiName && !request.TaskCompletionSource.Task.IsCompleted)
                {
                    request.TaskCompletionSource.TrySetResult(base64Result);
                    
                    OnApiResponse?.Invoke(this, new ApiResponseEventArgs
                    {
                        RequestId = request.RequestId,
                        ApiName = apiName,
                        Result = base64Result
                    });
                    
                    return;
                }
            }
        }

        /// <summary>
        /// 处理API错误
        /// </summary>
        public void HandleError(string apiName, string errorMessage)
        {
            foreach (var kvp in _pendingRequests)
            {
                var request = kvp.Value;
                if (request.ApiName == apiName && !request.TaskCompletionSource.Task.IsCompleted)
                {
                    request.TaskCompletionSource.TrySetException(new Exception(errorMessage));
                    
                    OnApiError?.Invoke(this, new ApiErrorEventArgs
                    {
                        RequestId = request.RequestId,
                        ApiName = apiName,
                        ErrorMessage = errorMessage
                    });
                    
                    return;
                }
            }
        }

        /// <summary>
        /// 解析API返回的Base64结果
        /// </summary>
        public static byte[] DecodeResult(string base64Result)
        {
            try
            {
                return Convert.FromBase64String(base64Result);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 生成请求ID
        /// </summary>
        private string GenerateRequestId()
        {
            var id = Interlocked.Increment(ref _requestIdCounter);
            return $"REQ_{DateTime.Now:HHmmss}_{id:D6}";
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _cts.Cancel();
            _queueSemaphore.Dispose();
            _cts.Dispose();

            // 取消所有待处理的请求
            foreach (var kvp in _pendingRequests)
            {
                kvp.Value.TaskCompletionSource.TrySetCanceled();
            }
            _pendingRequests.Clear();
        }

        #endregion
    }

    #region 辅助类型

    /// <summary>
    /// API请求
    /// </summary>
    internal class ApiRequest
    {
        public string RequestId { get; set; }
        public string ApiName { get; set; }
        public string ApiCall { get; set; }
        public DateTime CreateTime { get; set; }
        public int TimeoutMs { get; set; }
        public TaskCompletionSource<string> TaskCompletionSource { get; set; }
    }

    /// <summary>
    /// API调用事件参数
    /// </summary>
    public class ApiCallEventArgs : EventArgs
    {
        public string RequestId { get; set; }
        public string ApiName { get; set; }
        public string ApiCall { get; set; }
        public string[] Args { get; set; }
    }

    /// <summary>
    /// API响应事件参数
    /// </summary>
    public class ApiResponseEventArgs : EventArgs
    {
        public string RequestId { get; set; }
        public string ApiName { get; set; }
        public string Result { get; set; }
    }

    /// <summary>
    /// API错误事件参数
    /// </summary>
    public class ApiErrorEventArgs : EventArgs
    {
        public string RequestId { get; set; }
        public string ApiName { get; set; }
        public string ErrorMessage { get; set; }
    }

    #endregion
}
