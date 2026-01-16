using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using WSLFramework.Models;
using WSLFramework.Utils;

namespace WSLFramework.Services.Core
{
    /// <summary>
    /// 配置同步管理器 - 带确认机制的配置同步服务
    /// 解决主框架与副框架配置同步丢失问题
    /// </summary>
    public class ConfigSyncManager
    {
        #region 单例模式

        private static readonly Lazy<ConfigSyncManager> _instance =
            new Lazy<ConfigSyncManager>(() => new ConfigSyncManager());

        public static ConfigSyncManager Instance => _instance.Value;

        #endregion

        #region 常量

        /// <summary>同步超时时间(毫秒)</summary>
        private const int SYNC_TIMEOUT_MS = 5000;

        /// <summary>最大重试次数</summary>
        private const int MAX_RETRY_COUNT = 3;

        /// <summary>重试间隔(毫秒)</summary>
        private const int RETRY_INTERVAL_MS = 1000;

        #endregion

        #region 私有字段

        private readonly JavaScriptSerializer _serializer;
        private readonly ConcurrentDictionary<string, PendingSyncRequest> _pendingRequests;
        private readonly object _lock = new object();
        private int _requestId = 0;

        #endregion

        #region 事件

        /// <summary>日志事件</summary>
        public event Action<string> OnLog;

        /// <summary>同步请求发送事件 (供 FrameworkServer 订阅并发送)</summary>
        public event Action<ConfigSyncRequest> OnSyncRequestSend;

        /// <summary>同步失败事件</summary>
        public event Action<string, string> OnSyncFailed; // (configType, errorMessage)

        /// <summary>同步成功事件</summary>
        public event Action<string> OnSyncSuccess; // (configType)

        #endregion

        #region 构造函数

        private ConfigSyncManager()
        {
            _serializer = new JavaScriptSerializer();
            _pendingRequests = new ConcurrentDictionary<string, PendingSyncRequest>();
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 同步配置 (带确认机制，异步)
        /// </summary>
        /// <param name="configType">配置类型</param>
        /// <param name="configData">配置数据</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>同步结果</returns>
        public async Task<ConfigSyncResult> SyncConfigAsync(
            string configType, 
            object configData, 
            CancellationToken cancellationToken = default)
        {
            var requestId = GenerateRequestId();
            var request = new ConfigSyncRequest
            {
                RequestId = requestId,
                ConfigType = configType,
                ConfigData = _serializer.Serialize(configData),
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                RequireAck = true
            };

            Log($"[配置同步] 开始同步: {configType} (RequestId={requestId})");

            // 创建等待句柄
            var pendingRequest = new PendingSyncRequest
            {
                Request = request,
                CompletionSource = new TaskCompletionSource<ConfigSyncResult>(),
                RetryCount = 0
            };

            _pendingRequests[requestId] = pendingRequest;

            try
            {
                // 发送请求
                return await SendWithRetryAsync(pendingRequest, cancellationToken);
            }
            finally
            {
                // 清理
                _pendingRequests.TryRemove(requestId, out _);
            }
        }

        /// <summary>
        /// 同步配置 (不等待确认)
        /// </summary>
        public void SyncConfigFireAndForget(string configType, object configData)
        {
            var requestId = GenerateRequestId();
            var request = new ConfigSyncRequest
            {
                RequestId = requestId,
                ConfigType = configType,
                ConfigData = _serializer.Serialize(configData),
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                RequireAck = false
            };

            Log($"[配置同步] 发送(无确认): {configType}");
            OnSyncRequestSend?.Invoke(request);
        }

        /// <summary>
        /// 处理同步响应 (副框架返回的确认)
        /// </summary>
        public void HandleSyncResponse(ConfigSyncResponse response)
        {
            if (response == null || string.IsNullOrEmpty(response.RequestId))
            {
                Log("[配置同步] 收到无效响应");
                return;
            }

            if (_pendingRequests.TryGetValue(response.RequestId, out var pending))
            {
                var result = new ConfigSyncResult
                {
                    Success = response.Success,
                    ConfigType = pending.Request.ConfigType,
                    RequestId = response.RequestId,
                    ErrorMessage = response.ErrorMessage,
                    ResponseTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - pending.Request.Timestamp
                };

                Log($"[配置同步] 收到响应: {pending.Request.ConfigType} " +
                    $"(Success={response.Success}, Time={result.ResponseTime}ms)");

                pending.CompletionSource.TrySetResult(result);

                if (response.Success)
                {
                    OnSyncSuccess?.Invoke(pending.Request.ConfigType);
                }
                else
                {
                    OnSyncFailed?.Invoke(pending.Request.ConfigType, response.ErrorMessage);
                }
            }
            else
            {
                Log($"[配置同步] 收到未知请求的响应: {response.RequestId}");
            }
        }

        /// <summary>
        /// 获取待处理的同步请求数量
        /// </summary>
        public int GetPendingCount()
        {
            return _pendingRequests.Count;
        }

        /// <summary>
        /// 取消所有待处理的同步请求
        /// </summary>
        public void CancelAllPending()
        {
            foreach (var kvp in _pendingRequests)
            {
                kvp.Value.CompletionSource.TrySetCanceled();
            }
            _pendingRequests.Clear();
            Log("[配置同步] 已取消所有待处理请求");
        }

        #endregion

        #region 批量同步

        /// <summary>
        /// 批量同步多个配置
        /// </summary>
        public async Task<ConfigSyncBatchResult> SyncBatchAsync(
            ConfigSyncBatchRequest batch,
            CancellationToken cancellationToken = default)
        {
            // BUG修复: 空引用检查
            if (batch == null || batch.Configs == null)
            {
                return new ConfigSyncBatchResult
                {
                    TotalCount = 0,
                    SuccessCount = 0,
                    FailedCount = 0,
                    StartTime = DateTime.UtcNow,
                    EndTime = DateTime.UtcNow
                };
            }
            
            var result = new ConfigSyncBatchResult
            {
                TotalCount = batch.Configs.Count,
                StartTime = DateTime.UtcNow
            };

            Log($"[配置同步] 开始批量同步: {batch.Configs.Count} 项配置");

            foreach (var config in batch.Configs)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var syncResult = await SyncConfigAsync(config.Key, config.Value, cancellationToken);
                    if (syncResult.Success)
                    {
                        result.SuccessCount++;
                    }
                    else
                    {
                        result.FailedCount++;
                        result.FailedConfigs.Add(config.Key);
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    result.FailedCount++;
                    result.FailedConfigs.Add(config.Key);
                    Log($"[配置同步] 批量同步失败: {config.Key} - {ex.Message}");
                }
            }

            result.EndTime = DateTime.UtcNow;
            Log($"[配置同步] 批量同步完成: 成功={result.SuccessCount}, 失败={result.FailedCount}, " +
                $"耗时={result.Duration.TotalMilliseconds:F0}ms");

            return result;
        }

        #endregion

        #region 私有方法

        private string GenerateRequestId()
        {
            var id = Interlocked.Increment(ref _requestId);
            return $"SYNC_{DateTime.UtcNow:yyyyMMddHHmmss}_{id:D6}";
        }

        private async Task<ConfigSyncResult> SendWithRetryAsync(
            PendingSyncRequest pending,
            CancellationToken cancellationToken)
        {
            while (pending.RetryCount <= MAX_RETRY_COUNT)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // BUG修复: 每次重试前确保 CompletionSource 是新的（避免竞态条件）
                if (pending.RetryCount > 0)
                {
                    // 创建新的 CompletionSource 并原子更新
                    var newTcs = new TaskCompletionSource<ConfigSyncResult>();
                    var oldTcs = pending.CompletionSource;
                    pending.CompletionSource = newTcs;
                    
                    // 尝试取消旧的（如果还没完成）
                    oldTcs.TrySetCanceled();
                }

                // 发送请求
                OnSyncRequestSend?.Invoke(pending.Request);

                // 等待响应
                using (var timeoutCts = new CancellationTokenSource(SYNC_TIMEOUT_MS))
                using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken, timeoutCts.Token))
                {
                    try
                    {
                        var task = pending.CompletionSource.Task;
                        var delayTask = Task.Delay(SYNC_TIMEOUT_MS, linkedCts.Token);
                        var completedTask = await Task.WhenAny(task, delayTask);

                        if (completedTask == task)
                        {
                            // 成功收到响应
                            return await task;
                        }

                        // 超时，准备重试
                        pending.RetryCount++;
                        if (pending.RetryCount <= MAX_RETRY_COUNT)
                        {
                            Log($"[配置同步] 等待响应超时，重试 ({pending.RetryCount}/{MAX_RETRY_COUNT}): " +
                                $"{pending.Request.ConfigType}");
                            
                            await Task.Delay(RETRY_INTERVAL_MS, cancellationToken);
                        }
                    }
                    catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
                    {
                        // 超时，继续重试
                        pending.RetryCount++;
                        if (pending.RetryCount <= MAX_RETRY_COUNT)
                        {
                            Log($"[配置同步] 等待响应超时，重试 ({pending.RetryCount}/{MAX_RETRY_COUNT}): " +
                                $"{pending.Request.ConfigType}");
                            
                            await Task.Delay(RETRY_INTERVAL_MS, cancellationToken);
                        }
                    }
                }
            }

            // 重试耗尽
            var failResult = new ConfigSyncResult
            {
                Success = false,
                ConfigType = pending.Request.ConfigType,
                RequestId = pending.Request.RequestId,
                ErrorMessage = $"同步超时，已重试 {MAX_RETRY_COUNT} 次"
            };

            Log($"[配置同步] 同步失败(重试耗尽): {pending.Request.ConfigType}");
            OnSyncFailed?.Invoke(pending.Request.ConfigType, failResult.ErrorMessage);

            return failResult;
        }

        private void Log(string message)
        {
            Logger.Info(message);
            OnLog?.Invoke(message);
        }

        #endregion
    }

    #region 数据结构

    /// <summary>
    /// 配置同步请求
    /// </summary>
    public class ConfigSyncRequest
    {
        public string RequestId { get; set; }
        public string ConfigType { get; set; }
        public string ConfigData { get; set; }
        public long Timestamp { get; set; }
        public bool RequireAck { get; set; }
    }

    /// <summary>
    /// 配置同步响应
    /// </summary>
    public class ConfigSyncResponse
    {
        public string RequestId { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public long Timestamp { get; set; }
    }

    /// <summary>
    /// 配置同步结果
    /// </summary>
    public class ConfigSyncResult
    {
        public bool Success { get; set; }
        public string ConfigType { get; set; }
        public string RequestId { get; set; }
        public string ErrorMessage { get; set; }
        public long ResponseTime { get; set; }
    }

    /// <summary>
    /// 待处理的同步请求
    /// </summary>
    internal class PendingSyncRequest
    {
        public ConfigSyncRequest Request { get; set; }
        public TaskCompletionSource<ConfigSyncResult> CompletionSource { get; set; }
        public int RetryCount { get; set; }
    }

    /// <summary>
    /// 批量同步请求
    /// </summary>
    public class ConfigSyncBatchRequest
    {
        public System.Collections.Generic.Dictionary<string, object> Configs { get; set; }
            = new System.Collections.Generic.Dictionary<string, object>();
    }

    /// <summary>
    /// 批量同步结果
    /// </summary>
    public class ConfigSyncBatchResult
    {
        public int TotalCount { get; set; }
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        public System.Collections.Generic.List<string> FailedConfigs { get; set; }
            = new System.Collections.Generic.List<string>();
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration => EndTime - StartTime;
        public bool AllSuccess => FailedCount == 0;
    }

    /// <summary>
    /// 配置类型常量
    /// </summary>
    public static class ConfigTypes
    {
        public const string ODDS = "Odds";              // 赔率配置
        public const string SEALING = "Sealing";        // 封盘配置
        public const string AUTO_REPLY = "AutoReply";   // 自动回复
        public const string TRUSTEE = "Trustee";        // 托管配置
        public const string TEMPLATE = "Template";      // 消息模板
        public const string BASIC = "Basic";            // 基本设置
        public const string BET_RANGE = "BetRange";     // 下注范围
        public const string FULL = "Full";              // 全量配置
    }

    #endregion
}
