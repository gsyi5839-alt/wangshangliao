using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Web.Script.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace WangShangLiaoBot.Services.DirectConnection
{
    /// <summary>
    /// NIM消息服务 - 处理云信消息的发送和接收
    /// 
    /// 基于逆向分析的消息格式:
    /// - 使用Protobuf编码 (api.common.Message)
    /// - 通过xclient.exe中转
    /// - scene: "team" = 群消息, "p2p" = 私聊
    /// </summary>
    public sealed class NimMessageService
    {
        private static NimMessageService _instance;
        public static NimMessageService Instance => _instance ?? (_instance = new NimMessageService());

        private readonly XClientService _xclient;
        private readonly WangShangLiaoApiService _api;
        private readonly ConcurrentDictionary<string, DateTime> _sentMessages = new();
        private readonly SemaphoreSlim _sendLock = new(1, 1);

        // 消息发送限制
        private const int MIN_SEND_INTERVAL_MS = 100;
        private const int MAX_MESSAGES_PER_SECOND = 5;
        private int _messagesSentThisSecond = 0;
        private DateTime _lastSecondStart = DateTime.UtcNow;

        // 事件
        public event Action<string> OnLog;
        public event Action<NimChatMessage> OnGroupMessageReceived;
        public event Action<NimChatMessage> OnPrivateMessageReceived;
        public event Action<NimSystemMessage> OnSystemMessageReceived;

        public bool IsConnected => _xclient?.IsConnected ?? false;

        private NimMessageService()
        {
            _xclient = XClientService.Instance;
            _api = WangShangLiaoApiService.Instance;

            // 订阅xclient消息
            _xclient.OnGroupMessage += HandleGroupMessage;
            _xclient.OnPrivateMessage += HandlePrivateMessage;
            _xclient.OnMessageReceived += HandleRawMessage;
        }

        #region 消息发送

        /// <summary>
        /// 发送群文本消息
        /// </summary>
        public async Task<SendResult> SendGroupTextAsync(string teamId, string text, string ext = null)
        {
            if (!await CheckRateLimit())
            {
                return new SendResult { Success = false, Error = "发送频率过高" };
            }

            await _sendLock.WaitAsync();
            try
            {
                // 生成消息ID
                var msgId = GenerateMessageId();

                // 构建消息
                var message = new
                {
                    scene = "team",
                    to = teamId,
                    text,
                    msgId,
                    custom = ext,
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                };

                // 通过xclient发送
                var response = await _xclient.SendRequestAsync("sendText", message);

                if (response.Success)
                {
                    _sentMessages[msgId] = DateTime.UtcNow;
                    Log($"[NIM] 群消息已发送 -> {teamId}: {text.Substring(0, Math.Min(50, text.Length))}...");
                    return new SendResult { Success = true, MessageId = msgId };
                }
                else
                {
                    Log($"[NIM] 群消息发送失败: {response.Error}");
                    return new SendResult { Success = false, Error = response.Error };
                }
            }
            finally
            {
                _sendLock.Release();
            }
        }

        /// <summary>
        /// 发送私聊文本消息
        /// </summary>
        public async Task<SendResult> SendPrivateTextAsync(string userId, string text, string ext = null)
        {
            if (!await CheckRateLimit())
            {
                return new SendResult { Success = false, Error = "发送频率过高" };
            }

            await _sendLock.WaitAsync();
            try
            {
                var msgId = GenerateMessageId();

                var message = new
                {
                    scene = "p2p",
                    to = userId,
                    text,
                    msgId,
                    custom = ext,
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                };

                var response = await _xclient.SendRequestAsync("sendText", message);

                if (response.Success)
                {
                    _sentMessages[msgId] = DateTime.UtcNow;
                    Log($"[NIM] 私聊消息已发送 -> {userId}: {text.Substring(0, Math.Min(50, text.Length))}...");
                    return new SendResult { Success = true, MessageId = msgId };
                }
                else
                {
                    Log($"[NIM] 私聊消息发送失败: {response.Error}");
                    return new SendResult { Success = false, Error = response.Error };
                }
            }
            finally
            {
                _sendLock.Release();
            }
        }

        /// <summary>
        /// 发送群图片消息
        /// </summary>
        public async Task<SendResult> SendGroupImageAsync(string teamId, string imageUrl, string ext = null)
        {
            if (!await CheckRateLimit())
            {
                return new SendResult { Success = false, Error = "发送频率过高" };
            }

            var msgId = GenerateMessageId();

            var message = new
            {
                scene = "team",
                to = teamId,
                type = "image",
                file = new { url = imageUrl },
                msgId,
                custom = ext,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            var response = await _xclient.SendRequestAsync("sendImage", message);

            if (response.Success)
            {
                _sentMessages[msgId] = DateTime.UtcNow;
                return new SendResult { Success = true, MessageId = msgId };
            }

            return new SendResult { Success = false, Error = response.Error };
        }

        /// <summary>
        /// 发送自定义消息
        /// </summary>
        public async Task<SendResult> SendCustomMessageAsync(string scene, string to, object content)
        {
            if (!await CheckRateLimit())
            {
                return new SendResult { Success = false, Error = "发送频率过高" };
            }

            var msgId = GenerateMessageId();

            var message = new
            {
                scene,
                to,
                type = "custom",
                content = new JavaScriptSerializer().Serialize(content),
                msgId,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            var response = await _xclient.SendRequestAsync("sendCustom", message);

            return new SendResult
            {
                Success = response.Success,
                MessageId = response.Success ? msgId : null,
                Error = response.Error
            };
        }

        #endregion

        #region 消息接收处理

        private void HandleGroupMessage(XClientMessage msg)
        {
            try
            {
                // 检查是否是自己发送的消息
                if (_sentMessages.ContainsKey(msg.RequestId ?? ""))
                {
                    return; // 忽略自己发送的消息
                }

                var chatMsg = new NimChatMessage
                {
                    MessageId = msg.RequestId ?? GenerateMessageId(),
                    Scene = "team",
                    From = msg.From,
                    To = msg.To,
                    Text = msg.Text,
                    Timestamp = msg.Timestamp > 0 ? msg.Timestamp : DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    RawData = msg.Data
                };

                OnGroupMessageReceived?.Invoke(chatMsg);
            }
            catch (Exception ex)
            {
                Log($"[NIM] 处理群消息错误: {ex.Message}");
            }
        }

        private void HandlePrivateMessage(XClientMessage msg)
        {
            try
            {
                if (_sentMessages.ContainsKey(msg.RequestId ?? ""))
                {
                    return;
                }

                var chatMsg = new NimChatMessage
                {
                    MessageId = msg.RequestId ?? GenerateMessageId(),
                    Scene = "p2p",
                    From = msg.From,
                    To = msg.To,
                    Text = msg.Text,
                    Timestamp = msg.Timestamp > 0 ? msg.Timestamp : DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    RawData = msg.Data
                };

                OnPrivateMessageReceived?.Invoke(chatMsg);
            }
            catch (Exception ex)
            {
                Log($"[NIM] 处理私聊消息错误: {ex.Message}");
            }
        }

        private void HandleRawMessage(XClientMessage msg)
        {
            // 处理系统消息和其他类型
            if (msg.Type?.Contains("sys") == true || msg.Type?.Contains("notify") == true)
            {
                var sysMsg = new NimSystemMessage
                {
                    Type = msg.Type,
                    From = msg.From,
                    To = msg.To,
                    Content = msg.Data?.ToString(),
                    Timestamp = msg.Timestamp
                };

                OnSystemMessageReceived?.Invoke(sysMsg);
            }
        }

        #endregion

        #region 辅助方法

        private async Task<bool> CheckRateLimit()
        {
            var now = DateTime.UtcNow;

            // 重置每秒计数
            if ((now - _lastSecondStart).TotalSeconds >= 1)
            {
                _messagesSentThisSecond = 0;
                _lastSecondStart = now;
            }

            // 检查每秒限制
            if (_messagesSentThisSecond >= MAX_MESSAGES_PER_SECOND)
            {
                await Task.Delay(1000 - (int)(now - _lastSecondStart).TotalMilliseconds);
                _messagesSentThisSecond = 0;
                _lastSecondStart = DateTime.UtcNow;
            }

            _messagesSentThisSecond++;
            return true;
        }

        private string GenerateMessageId()
        {
            return $"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}-{Guid.NewGuid():N}".Substring(0, 32);
        }

        /// <summary>
        /// 清理过期的消息记录
        /// </summary>
        public void CleanupOldMessages()
        {
            var threshold = DateTime.UtcNow.AddMinutes(-5);
            var keysToRemove = new List<string>();

            foreach (var kv in _sentMessages)
            {
                if (kv.Value < threshold)
                {
                    keysToRemove.Add(kv.Key);
                }
            }

            foreach (var key in keysToRemove)
            {
                _sentMessages.TryRemove(key, out _);
            }
        }

        private void Log(string message)
        {
            OnLog?.Invoke(message);
            Utils.Logger.Info(message);
        }

        #endregion
    }

    #region 消息模型

    public class NimChatMessage
    {
        public string MessageId { get; set; }
        public string Scene { get; set; } // "team" or "p2p"
        public string From { get; set; }
        public string To { get; set; }
        public string Text { get; set; }
        public string Type { get; set; } = "text";
        public long Timestamp { get; set; }
        public object RawData { get; set; }

        // 扩展属性
        public string FromNickname { get; set; }
        public bool IsBot { get; set; }
        public string Custom { get; set; }
    }

    public class NimSystemMessage
    {
        public string Type { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string Content { get; set; }
        public long Timestamp { get; set; }
    }

    public class SendResult
    {
        public bool Success { get; set; }
        public string MessageId { get; set; }
        public string Error { get; set; }
    }

    #endregion
}
