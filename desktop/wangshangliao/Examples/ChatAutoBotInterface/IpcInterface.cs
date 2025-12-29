// ChatAutoBotInterface - IPC通信接口
// 用于主控程序和注入DLL之间的通信

using System;
using System.Collections.Generic;

namespace ChatAutoBotInterface
{
    /// <summary>
    /// 消息类型枚举
    /// </summary>
    public enum MessageType
    {
        Text,           // 文本消息
        Image,          // 图片消息
        Voice,          // 语音消息
        File,           // 文件消息
        System,         // 系统消息
        Unknown         // 未知类型
    }

    /// <summary>
    /// 聊天消息数据结构
    /// </summary>
    [Serializable]
    public class ChatMessage
    {
        /// <summary>
        /// 消息ID
        /// </summary>
        public string MessageId { get; set; }

        /// <summary>
        /// 发送者ID
        /// </summary>
        public string SenderId { get; set; }

        /// <summary>
        /// 发送者昵称
        /// </summary>
        public string SenderName { get; set; }

        /// <summary>
        /// 接收者ID（个人或群组）
        /// </summary>
        public string ReceiverId { get; set; }

        /// <summary>
        /// 接收者名称
        /// </summary>
        public string ReceiverName { get; set; }

        /// <summary>
        /// 是否为群消息
        /// </summary>
        public bool IsGroupMessage { get; set; }

        /// <summary>
        /// 消息类型
        /// </summary>
        public MessageType Type { get; set; }

        /// <summary>
        /// 消息内容
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// 消息时间戳
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 原始数据（用于调试）
        /// </summary>
        public byte[] RawData { get; set; }

        public override string ToString()
        {
            return $"[{Timestamp:HH:mm:ss}] {SenderName}({SenderId}) -> {ReceiverName}: {Content}";
        }
    }

    /// <summary>
    /// 自动回复规则
    /// </summary>
    [Serializable]
    public class AutoReplyRule
    {
        /// <summary>
        /// 规则ID
        /// </summary>
        public string RuleId { get; set; }

        /// <summary>
        /// 规则名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// 触发关键词列表（支持正则表达式）
        /// </summary>
        public List<string> TriggerKeywords { get; set; }

        /// <summary>
        /// 回复内容列表（随机选择一个）
        /// </summary>
        public List<string> ReplyContents { get; set; }

        /// <summary>
        /// 回复延迟（毫秒），模拟人工输入
        /// </summary>
        public int DelayMs { get; set; }

        /// <summary>
        /// 是否使用AI回复
        /// </summary>
        public bool UseAI { get; set; }

        /// <summary>
        /// AI提示词
        /// </summary>
        public string AIPrompt { get; set; }

        public AutoReplyRule()
        {
            TriggerKeywords = new List<string>();
            ReplyContents = new List<string>();
            DelayMs = 1000;
        }
    }

    /// <summary>
    /// 群发任务
    /// </summary>
    [Serializable]
    public class BroadcastTask
    {
        /// <summary>
        /// 任务ID
        /// </summary>
        public string TaskId { get; set; }

        /// <summary>
        /// 任务名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 目标ID列表
        /// </summary>
        public List<string> TargetIds { get; set; }

        /// <summary>
        /// 发送内容
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// 每条消息间隔（毫秒）
        /// </summary>
        public int IntervalMs { get; set; }

        /// <summary>
        /// 是否已完成
        /// </summary>
        public bool Completed { get; set; }

        /// <summary>
        /// 已发送数量
        /// </summary>
        public int SentCount { get; set; }

        public BroadcastTask()
        {
            TargetIds = new List<string>();
            IntervalMs = 2000;
        }
    }

    /// <summary>
    /// IPC通信接口 - 由主控程序实现，注入DLL调用
    /// </summary>
    public interface IChatBotController
    {
        /// <summary>
        /// 报告注入成功
        /// </summary>
        /// <param name="processId">目标进程ID</param>
        /// <param name="processName">目标进程名称</param>
        void OnInjected(int processId, string processName);

        /// <summary>
        /// 报告收到新消息
        /// </summary>
        /// <param name="message">消息内容</param>
        void OnMessageReceived(ChatMessage message);

        /// <summary>
        /// 报告消息发送成功
        /// </summary>
        /// <param name="message">消息内容</param>
        void OnMessageSent(ChatMessage message);

        /// <summary>
        /// 报告异常
        /// </summary>
        /// <param name="ex">异常信息</param>
        void OnError(Exception ex);

        /// <summary>
        /// 报告日志
        /// </summary>
        /// <param name="level">日志级别</param>
        /// <param name="message">日志内容</param>
        void OnLog(string level, string message);

        /// <summary>
        /// 心跳检测
        /// </summary>
        void Ping();

        /// <summary>
        /// 获取自动回复规则列表
        /// </summary>
        /// <returns>规则列表</returns>
        List<AutoReplyRule> GetAutoReplyRules();

        /// <summary>
        /// 获取待执行的群发任务
        /// </summary>
        /// <returns>群发任务</returns>
        BroadcastTask GetPendingBroadcastTask();

        /// <summary>
        /// 请求AI回复
        /// </summary>
        /// <param name="prompt">提示词</param>
        /// <param name="userMessage">用户消息</param>
        /// <returns>AI回复内容</returns>
        string GetAIReply(string prompt, string userMessage);
    }

    /// <summary>
    /// IPC服务端实现基类
    /// </summary>
    public class ChatBotControllerBase : MarshalByRefObject, IChatBotController
    {
        // 事件委托
        public event Action<int, string> Injected;
        public event Action<ChatMessage> MessageReceived;
        public event Action<ChatMessage> MessageSent;
        public event Action<Exception> Error;
        public event Action<string, string> Log;

        // 数据存储
        private List<AutoReplyRule> _autoReplyRules = new List<AutoReplyRule>();
        private Queue<BroadcastTask> _broadcastTasks = new Queue<BroadcastTask>();
        private Func<string, string, string> _aiReplyHandler;

        public void OnInjected(int processId, string processName)
        {
            Injected?.Invoke(processId, processName);
        }

        public void OnMessageReceived(ChatMessage message)
        {
            MessageReceived?.Invoke(message);
        }

        public void OnMessageSent(ChatMessage message)
        {
            MessageSent?.Invoke(message);
        }

        public void OnError(Exception ex)
        {
            Error?.Invoke(ex);
        }

        public void OnLog(string level, string message)
        {
            Log?.Invoke(level, message);
        }

        public void Ping()
        {
            // 心跳检测，不做任何事
        }

        public List<AutoReplyRule> GetAutoReplyRules()
        {
            lock (_autoReplyRules)
            {
                return new List<AutoReplyRule>(_autoReplyRules);
            }
        }

        public BroadcastTask GetPendingBroadcastTask()
        {
            lock (_broadcastTasks)
            {
                if (_broadcastTasks.Count > 0)
                {
                    return _broadcastTasks.Dequeue();
                }
            }
            return null;
        }

        public string GetAIReply(string prompt, string userMessage)
        {
            return _aiReplyHandler?.Invoke(prompt, userMessage);
        }

        // 公开方法：添加自动回复规则
        public void AddAutoReplyRule(AutoReplyRule rule)
        {
            lock (_autoReplyRules)
            {
                _autoReplyRules.Add(rule);
            }
        }

        // 公开方法：移除自动回复规则
        public void RemoveAutoReplyRule(string ruleId)
        {
            lock (_autoReplyRules)
            {
                _autoReplyRules.RemoveAll(r => r.RuleId == ruleId);
            }
        }

        // 公开方法：添加群发任务
        public void AddBroadcastTask(BroadcastTask task)
        {
            lock (_broadcastTasks)
            {
                _broadcastTasks.Enqueue(task);
            }
        }

        // 公开方法：设置AI回复处理函数
        public void SetAIReplyHandler(Func<string, string, string> handler)
        {
            _aiReplyHandler = handler;
        }

        // 保持远程对象不过期
        public override object InitializeLifetimeService()
        {
            return null;
        }
    }
}

