using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WangShangLiaoBot.Models;

namespace WangShangLiaoBot.Services
{
    /// <summary>
    /// 自动回复服务 - 处理自动回复、关键词回复等
    /// </summary>
    public class AutoReplyService
    {
        private static AutoReplyService _instance;
        private static readonly object _lock = new object();
        
        /// <summary>单例实例</summary>
        public static AutoReplyService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                            _instance = new AutoReplyService();
                    }
                }
                return _instance;
            }
        }
        
        private CancellationTokenSource _cts;
        private bool _isRunning;
        
        /// <summary>是否正在运行</summary>
        public bool IsRunning => _isRunning;
        
        /// <summary>处理的消息计数</summary>
        public int ProcessedCount { get; private set; }
        
        /// <summary>状态变更事件</summary>
        public event Action<bool> OnStatusChanged;
        
        /// <summary>日志事件</summary>
        public event Action<string> OnLog;
        
        private AutoReplyService() { }
        
        /// <summary>
        /// 启动自动回复服务
        /// </summary>
        public void Start()
        {
            if (_isRunning) return;
            
            _cts = new CancellationTokenSource();
            _isRunning = true;
            ProcessedCount = 0;
            
            // 订阅消息事件
            ChatService.Instance.OnMessageReceived += HandleMessage;
            
            OnStatusChanged?.Invoke(true);
            Log("自动回复服务已启动");
        }
        
        /// <summary>
        /// 停止自动回复服务
        /// </summary>
        public void Stop()
        {
            if (!_isRunning) return;
            
            _cts?.Cancel();
            _isRunning = false;
            
            // 取消订阅
            ChatService.Instance.OnMessageReceived -= HandleMessage;
            
            OnStatusChanged?.Invoke(false);
            Log("自动回复服务已停止");
        }
        
        /// <summary>
        /// 处理接收到的消息
        /// </summary>
        private async void HandleMessage(ChatMessage message)
        {
            if (!_isRunning || message.IsProcessed) return;
            
            try
            {
                var config = ConfigService.Instance.Config;
                
                // 检查是否在黑名单
                if (config.Blacklist.Contains(message.SenderId))
                {
                    Log($"忽略黑名单用户消息: {message.SenderId}");
                    return;
                }
                
                string reply = null;
                
                // 1. 检查关键词回复
                reply = CheckKeywordReply(message.Content, config.KeywordRules);
                
                // 2. 如果没有关键词匹配，使用默认自动回复
                if (string.IsNullOrEmpty(reply) && config.EnableAutoReply)
                {
                    reply = config.AutoReplyContent;
                }
                
                // 3. 发送回复
                if (!string.IsNullOrEmpty(reply))
                {
                    // Render via unified template engine (variables are backed by real DataService/LotteryService sources)
                    var player = DataService.Instance.GetOrCreatePlayer(message.SenderId, message.SenderName);
                    reply = TemplateEngine.Render(reply, new TemplateEngine.RenderContext
                    {
                        Message = message,
                        Player = player,
                        Today = DateTime.Today
                    });
                    
                    await ChatService.Instance.SendMessageAsync(reply);
                    ProcessedCount++;
                    Log($"自动回复 [{message.SenderName}]: {reply}");
                }
                
                message.IsProcessed = true;
            }
            catch (Exception ex)
            {
                Log($"处理消息异常: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 检查关键词回复
        /// </summary>
        private string CheckKeywordReply(string content, List<KeywordReplyRule> rules)
        {
            if (string.IsNullOrEmpty(content) || rules == null) return null;
            
            foreach (var rule in rules.Where(r => r.Enabled))
            {
                // 支持多关键词，用|分隔
                var keywords = rule.Keyword.Split('|');
                foreach (var keyword in keywords)
                {
                    if (content.Contains(keyword.Trim()))
                    {
                        return rule.Reply;
                    }
                }
            }
            
            return null;
        }
        
        // NOTE: Variable replacement is now unified in TemplateEngine.
        
        /// <summary>
        /// 添加关键词规则
        /// </summary>
        public void AddKeywordRule(string keyword, string reply)
        {
            var config = ConfigService.Instance.Config;
            config.KeywordRules.Add(new KeywordReplyRule
            {
                Keyword = keyword,
                Reply = reply,
                Enabled = true
            });
            ConfigService.Instance.SaveConfig();
            Log($"添加关键词规则: {keyword} -> {reply}");
        }
        
        /// <summary>
        /// 删除关键词规则
        /// </summary>
        public void RemoveKeywordRule(string keyword)
        {
            var config = ConfigService.Instance.Config;
            var rule = config.KeywordRules.FirstOrDefault(r => r.Keyword == keyword);
            if (rule != null)
            {
                config.KeywordRules.Remove(rule);
                ConfigService.Instance.SaveConfig();
                Log($"删除关键词规则: {keyword}");
            }
        }
        
        private void Log(string message)
        {
            Logger.Info($"[AutoReply] {message}");
            OnLog?.Invoke(message);
        }
    }
}

