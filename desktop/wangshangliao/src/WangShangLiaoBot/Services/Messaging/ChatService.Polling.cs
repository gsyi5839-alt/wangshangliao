using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Automation;
using WangShangLiaoBot.Models;
using WangShangLiaoBot.Utils;

namespace WangShangLiaoBot.Services
{
    public partial class ChatService
    {
        #region Message Polling - Real-time message monitoring
        
        /// <summary>
        /// Start message polling to monitor new messages in real-time
        /// </summary>
        /// <param name="intervalMs">Polling interval in milliseconds (default 2000ms)</param>
        public void StartMessagePolling(int intervalMs = 2000)
        {
            if (IsPollingMessages)
            {
                Log("消息轮询已在运行中");
                return;
            }
            
            if (!IsConnected)
            {
                Log("未连接，无法启动消息轮询");
                return;
            }
            
            _messagePollingTimer = new System.Timers.Timer(intervalMs);
            _messagePollingTimer.Elapsed += async (s, e) => await PollMessagesAsync();
            _messagePollingTimer.AutoReset = true;
            _messagePollingTimer.Start();
            
            IsPollingMessages = true;
            _processedMessageHashes.Clear();
            
            Log($"消息轮询已启动，间隔 {intervalMs}ms");
        }
        
        /// <summary>
        /// Stop message polling
        /// </summary>
        public void StopMessagePolling()
        {
            if (!IsPollingMessages)
            {
                return;
            }
            
            _messagePollingTimer?.Stop();
            _messagePollingTimer?.Dispose();
            _messagePollingTimer = null;
            
            IsPollingMessages = false;
            Log("消息轮询已停止");
        }
        
        /// <summary>
        /// Poll for new messages and trigger OnMessageReceived event
        /// </summary>
        private async Task PollMessagesAsync()
        {
            if (!IsConnected) return;
            
            try
            {
                // Get recent messages from the chat window
                var script = @"
(function() {
    var messages = [];
    
    // WangShangLiao uses .msg-item class for messages
    var msgItems = document.querySelectorAll('.msg-item, [class*=""message-content""]');
    var count = msgItems.length;
    
    // Only process last 10 messages to reduce load
    var startIndex = Math.max(0, count - 10);
    
    for (var i = startIndex; i < count; i++) {
        var el = msgItems[i];
        var text = (el.innerText || '').trim();
        if (text && text.length > 0 && text.length < 2000) {
            var className = el.className || '';
            var isSent = className.includes('self-msg') || className.includes('self') || className.includes('right');
            
            // Try to get sender info from parent elements
            var parent = el.parentElement;
            var senderName = '';
            var senderId = '';
            
            // Look for sender name in nearby elements
            if (parent) {
                var nameEl = parent.querySelector('[class*=""name""], [class*=""nick""]');
                if (nameEl) {
                    senderName = (nameEl.innerText || '').trim();
                }
                
                // Try to get user ID from data attributes
                var userEl = parent.closest('[data-account], [data-id], [data-userid]');
                if (userEl) {
                    senderId = userEl.getAttribute('data-account') || 
                               userEl.getAttribute('data-id') || 
                               userEl.getAttribute('data-userid') || '';
                }
            }
            
            messages.push({
                text: text.substring(0, 1000),
                sender: senderName || (isSent ? '我' : '未知'),
                senderId: senderId,
                isSent: isSent,
                index: i,
                hash: text.substring(0, 100) + '_' + i
            });
        }
    }
    
    return JSON.stringify(messages);
})();";
                
                var result = await ExecuteScriptWithResultAsync(script);
                
                if (!string.IsNullOrEmpty(result) && result.StartsWith("["))
                {
                    // Parse messages
                    var msgMatches = System.Text.RegularExpressions.Regex.Matches(
                        result,
                        @"\{\s*""text""\s*:\s*""((?:[^""\\]|\\.)*)""\s*,\s*""sender""\s*:\s*""([^""]*)""\s*,\s*""senderId""\s*:\s*""([^""]*)""\s*,\s*""isSent""\s*:\s*(true|false)\s*,\s*""index""\s*:\s*(\d+)\s*,\s*""hash""\s*:\s*""([^""]*)""",
                        System.Text.RegularExpressions.RegexOptions.Singleline);
                    
                    foreach (System.Text.RegularExpressions.Match match in msgMatches)
                    {
                        var text = System.Text.RegularExpressions.Regex.Unescape(match.Groups[1].Value);
                        var sender = match.Groups[2].Value;
                        var senderId = match.Groups[3].Value;
                        var isSent = match.Groups[4].Value == "true";
                        var index = int.Parse(match.Groups[5].Value);
                        var hash = match.Groups[6].Value;
                        
                        // Skip if already processed or is self message
                        if (_processedMessageHashes.ContainsKey(hash) || isSent)
                        {
                            continue;
                        }
                        
                        // Mark as processed (线程安全)
                        _processedMessageHashes.TryAdd(hash, 0);
                        
                        // Keep hash set from growing too large
                        if (_processedMessageHashes.Count > 1000)
                        {
                            _processedMessageHashes.Clear();
                        }
                        
                        // Create message object
                        var chatMessage = new ChatMessage
                        {
                            Content = text,
                            SenderName = sender,
                            SenderId = senderId,
                            Time = DateTime.Now,
                            IsSelf = isSent,
                            IsGroupMessage = true
                        };
                        
                        Log($"[新消息] {sender}: {text.Substring(0, Math.Min(50, text.Length))}...");
                        
                        // Trigger event for auto-reply processing
                        OnMessageReceived?.Invoke(chatMessage);
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"轮询消息异常: {ex.Message}");
            }
        }
        
        #endregion

    }
}
