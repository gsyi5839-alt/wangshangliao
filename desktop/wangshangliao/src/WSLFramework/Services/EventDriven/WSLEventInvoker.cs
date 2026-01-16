using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace WSLFramework.Services.EventDriven
{
    /// <summary>
    /// 旺商聊事件调度器 - 参考 Lagrange.Core 的 EventInvoker 设计
    /// 实现事件驱动模式，替代 CDP 轮询
    /// </summary>
    public partial class WSLEventInvoker : IDisposable
    {
        private const string Tag = "WSLEventInvoker";

        private readonly Dictionary<Type, Action<WSLEventBase>> _events;
        private readonly object _lockObj = new object();
        private bool _disposed;

        /// <summary>
        /// 事件委托定义
        /// </summary>
        public delegate void WSLEvent<in TEvent>(WSLBotContext context, TEvent e) where TEvent : WSLEventBase;

        #region 事件定义

        /// <summary>登录成功事件</summary>
        public event WSLEvent<BotOnlineEvent> OnBotOnlineEvent;

        /// <summary>下线事件</summary>
        public event WSLEvent<BotOfflineEvent> OnBotOfflineEvent;

        /// <summary>日志事件</summary>
        public event WSLEvent<BotLogEvent> OnBotLogEvent;

        /// <summary>群消息事件 - 核心事件</summary>
        public event WSLEvent<GroupMessageEvent> OnGroupMessageReceived;

        /// <summary>私聊消息事件</summary>
        public event WSLEvent<FriendMessageEvent> OnFriendMessageReceived;

        /// <summary>群禁言事件</summary>
        public event WSLEvent<GroupMuteEvent> OnGroupMuteEvent;

        /// <summary>群成员变动事件</summary>
        public event WSLEvent<GroupMemberChangeEvent> OnGroupMemberChangeEvent;

        /// <summary>群名片变更事件</summary>
        public event WSLEvent<GroupCardChangeEvent> OnGroupCardChangeEvent;

        /// <summary>连接状态变化事件</summary>
        public event WSLEvent<ConnectionStateEvent> OnConnectionStateChanged;

        /// <summary>开奖结果事件</summary>
        public event WSLEvent<LotteryResultEvent> OnLotteryResultReceived;

        /// <summary>下注事件</summary>
        public event WSLEvent<BetEvent> OnBetReceived;

        /// <summary>上下分事件</summary>
        public event WSLEvent<ScoreChangeEvent> OnScoreChangeReceived;

        #endregion

        private readonly WSLBotContext _context;

        public WSLEventInvoker(WSLBotContext context)
        {
            _context = context;
            _events = new Dictionary<Type, Action<WSLEventBase>>();

            // 注册所有事件处理器
            RegisterAllEvents();
        }

        private void RegisterAllEvents()
        {
            RegisterEvent((BotOnlineEvent e) => OnBotOnlineEvent?.Invoke(_context, e));
            RegisterEvent((BotOfflineEvent e) => OnBotOfflineEvent?.Invoke(_context, e));
            RegisterEvent((BotLogEvent e) => OnBotLogEvent?.Invoke(_context, e));
            RegisterEvent((GroupMessageEvent e) => OnGroupMessageReceived?.Invoke(_context, e));
            RegisterEvent((FriendMessageEvent e) => OnFriendMessageReceived?.Invoke(_context, e));
            RegisterEvent((GroupMuteEvent e) => OnGroupMuteEvent?.Invoke(_context, e));
            RegisterEvent((GroupMemberChangeEvent e) => OnGroupMemberChangeEvent?.Invoke(_context, e));
            RegisterEvent((GroupCardChangeEvent e) => OnGroupCardChangeEvent?.Invoke(_context, e));
            RegisterEvent((ConnectionStateEvent e) => OnConnectionStateChanged?.Invoke(_context, e));
            RegisterEvent((LotteryResultEvent e) => OnLotteryResultReceived?.Invoke(_context, e));
            RegisterEvent((BetEvent e) => OnBetReceived?.Invoke(_context, e));
            RegisterEvent((ScoreChangeEvent e) => OnScoreChangeReceived?.Invoke(_context, e));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RegisterEvent<TEvent>(Action<TEvent> action) where TEvent : WSLEventBase
        {
            lock (_lockObj)
            {
                _events[typeof(TEvent)] = e => action((TEvent)e);
            }
        }

        /// <summary>
        /// 投递事件 - 异步执行，不阻塞调用方
        /// </summary>
        public void PostEvent(WSLEventBase e)
        {
            if (_disposed) return;

            Task.Run(() =>
            {
                try
                {
                    Action<WSLEventBase> action;
                    lock (_lockObj)
                    {
                        if (!_events.TryGetValue(e.GetType(), out action))
                        {
                            // BUG修复: 避免递归调用 PostEvent 导致栈溢出
                            // 直接写入控制台或使用同步日志
                            Console.WriteLine($"[{Tag}][Warning] Event {e.GetType().Name} is not registered but pushed to invoker");
                            return;
                        }
                    }
                    action(e);
                }
                catch (Exception ex)
                {
                    // BUG修复: 避免递归调用 PostEvent 导致栈溢出
                    Console.WriteLine($"[{Tag}][Error] Event handler error: {ex}");
                }
            });
        }

        /// <summary>
        /// 同步投递事件 - 用于关键事件需要立即处理
        /// </summary>
        public void PostEventSync(WSLEventBase e)
        {
            if (_disposed) return;

            try
            {
                Action<WSLEventBase> action;
                lock (_lockObj)
                {
                    if (!_events.TryGetValue(e.GetType(), out action))
                    {
                        return;
                    }
                }
                action(e);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{Tag}] Event handler error: {ex}");
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            lock (_lockObj)
            {
                _events.Clear();
            }
            GC.SuppressFinalize(this);
        }
    }

    #region 日志级别

    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error,
        Fatal
    }

    #endregion
}
