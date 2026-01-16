using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WangShangLiaoBot.Models;
using WangShangLiaoBot.Services.Betting;

namespace WangShangLiaoBot.Services.Bot
{
    /// <summary>
    /// 消息调度器 - 负责分发和处理收到的消息
    /// 基于招财狗(ZCG)的消息处理流程实现
    /// </summary>
    public sealed class MessageDispatcher
    {
        private static MessageDispatcher _instance;
        public static MessageDispatcher Instance => _instance ?? (_instance = new MessageDispatcher());

        private readonly Queue<ChatMessage> _messageQueue = new Queue<ChatMessage>();
        private readonly object _queueLock = new object();
        private CancellationTokenSource _cts;
        private Task _processingTask;
        private bool _isRunning;

        // 消息处理器列表
        private readonly List<IMessageHandler> _handlers = new List<IMessageHandler>();

        // 事件
        public event Action<string, string> OnSendGroupMessage;    // teamId, message
        public event Action<string, string> OnSendPrivateMessage;  // userId, message
        public event Action<string> OnLog;

        private MessageDispatcher() { }

        #region 处理器管理

        /// <summary>
        /// 注册消息处理器
        /// </summary>
        public void RegisterHandler(IMessageHandler handler)
        {
            if (!_handlers.Contains(handler))
            {
                _handlers.Add(handler);
                _handlers.Sort((a, b) => a.Priority.CompareTo(b.Priority));
            }
        }

        /// <summary>
        /// 移除消息处理器
        /// </summary>
        public void RemoveHandler(IMessageHandler handler)
        {
            _handlers.Remove(handler);
        }

        #endregion

        #region 启动/停止

        /// <summary>
        /// 启动消息调度器
        /// </summary>
        public void Start()
        {
            if (_isRunning) return;

            _cts = new CancellationTokenSource();
            _isRunning = true;

            _processingTask = Task.Run(ProcessMessageLoop, _cts.Token);
            Log("[消息调度器] 已启动");
        }

        /// <summary>
        /// 停止消息调度器
        /// </summary>
        public void Stop()
        {
            _isRunning = false;
            _cts?.Cancel();
            _processingTask?.Wait(1000);
            Log("[消息调度器] 已停止");
        }

        #endregion

        #region 消息入队

        /// <summary>
        /// 接收消息并入队处理
        /// </summary>
        public void EnqueueMessage(ChatMessage message)
        {
            if (message == null) return;

            lock (_queueLock)
            {
                _messageQueue.Enqueue(message);
            }
        }

        #endregion

        #region 消息处理循环

        private async Task ProcessMessageLoop()
        {
            while (_isRunning && !_cts.Token.IsCancellationRequested)
            {
                ChatMessage message = null;

                lock (_queueLock)
                {
                    if (_messageQueue.Count > 0)
                    {
                        message = _messageQueue.Dequeue();
                    }
                }

                if (message != null)
                {
                    await ProcessMessageAsync(message);
                }
                else
                {
                    await Task.Delay(50, _cts.Token);
                }
            }
        }

        private async Task ProcessMessageAsync(ChatMessage message)
        {
            try
            {
                var context = new MessageContext
                {
                    Message = message,
                    IsGroupMessage = message.IsGroupMessage,
                    TeamId = message.TeamId,
                    SenderId = message.From,
                    SenderNick = message.FromNick,
                    Content = message.Content,
                    Handled = false
                };

                // 按优先级调用处理器
                foreach (var handler in _handlers)
                {
                    if (context.Handled) break;
                    if (!handler.CanHandle(context)) continue;

                    try
                    {
                        await handler.HandleAsync(context);
                    }
                    catch (Exception ex)
                    {
                        Log($"[消息调度器] 处理器 {handler.GetType().Name} 异常: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"[消息调度器] 处理消息异常: {ex.Message}");
            }
        }

        #endregion

        #region 发送消息

        /// <summary>
        /// 发送群消息
        /// </summary>
        public void SendGroupMessage(string teamId, string content)
        {
            OnSendGroupMessage?.Invoke(teamId, content);
        }

        /// <summary>
        /// 发送私聊消息
        /// </summary>
        public void SendPrivateMessage(string userId, string content)
        {
            OnSendPrivateMessage?.Invoke(userId, content);
        }

        #endregion

        private void Log(string message)
        {
            OnLog?.Invoke(message);
            Logger.Info(message);
        }
    }

    #region 消息上下文和处理器接口

    /// <summary>
    /// 消息上下文
    /// </summary>
    public class MessageContext
    {
        public ChatMessage Message { get; set; }
        public bool IsGroupMessage { get; set; }
        public string TeamId { get; set; }
        public string SenderId { get; set; }
        public string SenderNick { get; set; }
        public string Content { get; set; }
        public bool Handled { get; set; }
        
        // 额外属性（兼容 Handler 使用）
        public string Text { get => Content; set => Content = value; }
        public string MessageId => Message?.MsgId;
        public bool IsFromBot { get; set; }
        public bool IsSealed { get; set; }
        public bool IsImage { get; set; }

        /// <summary>
        /// 回复消息
        /// </summary>
        public void Reply(string content)
        {
            if (IsGroupMessage)
            {
                MessageDispatcher.Instance.SendGroupMessage(TeamId, content);
            }
            else
            {
                MessageDispatcher.Instance.SendPrivateMessage(SenderId, content);
            }
        }

        /// <summary>
        /// 回复并艾特发送者
        /// </summary>
        public void ReplyWithAt(string content)
        {
            var atContent = $"@{SenderNick} {content}";
            Reply(atContent);
        }
    }

    /// <summary>
    /// 消息处理结果
    /// </summary>
    public class HandlerResult
    {
        /// <summary>是否已处理</summary>
        public bool IsHandled { get; private set; }

        /// <summary>回复内容</summary>
        public string Reply { get; private set; }
        
        /// <summary>回复内容（别名）</summary>
        public string ReplyMessage => Reply;

        /// <summary>是否应该继续传递给下一个处理器</summary>
        public bool ShouldContinue { get; private set; }

        private HandlerResult() { }

        /// <summary>创建已处理结果（带回复）</summary>
        public static HandlerResult Handled(string reply = null)
        {
            return new HandlerResult { IsHandled = true, Reply = reply, ShouldContinue = false };
        }

        /// <summary>创建未处理结果</summary>
        public static HandlerResult NotHandled()
        {
            return new HandlerResult { IsHandled = false, Reply = null, ShouldContinue = true };
        }

        /// <summary>创建已处理但继续传递的结果</summary>
        public static HandlerResult Continue(string reply = null)
        {
            return new HandlerResult { IsHandled = true, Reply = reply, ShouldContinue = true };
        }
    }

    /// <summary>
    /// 消息处理器接口
    /// </summary>
    public interface IMessageHandler
    {
        /// <summary>
        /// 处理器优先级 (数字越小优先级越高)
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// 是否可以处理该消息
        /// </summary>
        bool CanHandle(MessageContext context);

        /// <summary>
        /// 处理消息
        /// </summary>
        Task<HandlerResult> HandleAsync(MessageContext context);
    }

    #endregion
}
