// ElectronInjector.cs - Electron应用自动化控制器
// 通过 Chrome DevTools Protocol 控制 Electron 应用

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ChatAutoBot
{
    /// <summary>
    /// Electron 应用自动化控制器
    /// 通过 Chrome DevTools Protocol (CDP) 实现
    /// </summary>
    public class ElectronInjector : IDisposable
    {
        private Process _process;
        private ClientWebSocket _webSocket;
        private int _debugPort;
        private string _wsUrl;
        private int _messageId = 1;
        private Dictionary<int, TaskCompletionSource<string>> _pendingRequests = new Dictionary<int, TaskCompletionSource<string>>();
        private CancellationTokenSource _cts = new CancellationTokenSource();
        
        // 事件
        public event Action<string> OnLog;
        public event Action<string> OnMessageReceived;
        public event Action<string> OnError;

        /// <summary>
        /// 启动目标应用并开启远程调试
        /// </summary>
        /// <param name="exePath">Electron应用路径</param>
        /// <param name="debugPort">调试端口（默认9222）</param>
        public async Task<bool> LaunchWithDebug(string exePath, int debugPort = 9222)
        {
            _debugPort = debugPort;

            try
            {
                Log($"正在启动: {exePath}");
                Log($"调试端口: {debugPort}");

                // 启动 Electron 应用，开启远程调试
                var startInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = $"--remote-debugging-port={debugPort}",
                    UseShellExecute = false,
                    WorkingDirectory = Path.GetDirectoryName(exePath)
                };

                _process = Process.Start(startInfo);

                // 等待应用启动
                await Task.Delay(3000);

                // 获取 WebSocket 调试 URL
                if (!await GetDebugUrl())
                {
                    Log("无法获取调试URL，尝试其他方法...");
                    return false;
                }

                // 连接 WebSocket
                await ConnectWebSocket();

                Log("连接成功！");
                return true;
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"启动失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 附加到已运行的进程
        /// 注意：需要目标进程已开启调试端口
        /// </summary>
        public async Task<bool> AttachToProcess(int debugPort = 9222)
        {
            _debugPort = debugPort;

            try
            {
                if (!await GetDebugUrl())
                {
                    Log("无法连接到调试端口，请确保目标应用以调试模式运行");
                    Log("提示: 可以创建快捷方式，添加参数 --remote-debugging-port=9222");
                    return false;
                }

                await ConnectWebSocket();
                return true;
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"附加失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取调试URL
        /// </summary>
        private async Task<bool> GetDebugUrl()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(5);
                    var response = await client.GetStringAsync($"http://127.0.0.1:{_debugPort}/json");
                    
                    Log($"获取到调试信息: {response.Substring(0, Math.Min(200, response.Length))}...");

                    // 解析 JSON 获取 webSocketDebuggerUrl
                    var match = Regex.Match(response, "\"webSocketDebuggerUrl\"\\s*:\\s*\"([^\"]+)\"");
                    if (match.Success)
                    {
                        _wsUrl = match.Groups[1].Value;
                        Log($"WebSocket URL: {_wsUrl}");
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"获取调试URL失败: {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// 连接 WebSocket
        /// </summary>
        private async Task ConnectWebSocket()
        {
            _webSocket = new ClientWebSocket();
            await _webSocket.ConnectAsync(new Uri(_wsUrl), _cts.Token);
            
            Log("WebSocket 已连接");

            // 启动消息接收循环
            _ = ReceiveLoop();
        }

        /// <summary>
        /// WebSocket 消息接收循环
        /// </summary>
        private async Task ReceiveLoop()
        {
            var buffer = new byte[65536];

            try
            {
                while (_webSocket.State == WebSocketState.Open && !_cts.IsCancellationRequested)
                {
                    var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);
                    
                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        HandleMessage(message);
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        break;
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                OnError?.Invoke($"WebSocket 错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理收到的消息
        /// </summary>
        private void HandleMessage(string message)
        {
            try
            {
                // 检查是否是响应消息
                var idMatch = Regex.Match(message, "\"id\"\\s*:\\s*(\\d+)");
                if (idMatch.Success)
                {
                    int id = int.Parse(idMatch.Groups[1].Value);
                    if (_pendingRequests.TryGetValue(id, out var tcs))
                    {
                        _pendingRequests.Remove(id);
                        tcs.SetResult(message);
                        return;
                    }
                }

                // 检查是否是事件消息
                var methodMatch = Regex.Match(message, "\"method\"\\s*:\\s*\"([^\"]+)\"");
                if (methodMatch.Success)
                {
                    string method = methodMatch.Groups[1].Value;
                    
                    // 处理控制台消息
                    if (method == "Runtime.consoleAPICalled")
                    {
                        var argsMatch = Regex.Match(message, "\"value\"\\s*:\\s*\"([^\"]+)\"");
                        if (argsMatch.Success)
                        {
                            string value = argsMatch.Groups[1].Value;
                            // 检查是否是我们注入的消息监听器发送的
                            if (value.StartsWith("[ChatBot]"))
                            {
                                OnMessageReceived?.Invoke(value);
                            }
                        }
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// 发送 CDP 命令
        /// </summary>
        public async Task<string> SendCommand(string method, object parameters = null)
        {
            var id = _messageId++;
            var tcs = new TaskCompletionSource<string>();
            _pendingRequests[id] = tcs;

            string paramsJson = parameters != null ? 
                SimpleJsonSerialize(parameters) : "{}";

            string message = $"{{\"id\":{id},\"method\":\"{method}\",\"params\":{paramsJson}}}";
            
            var bytes = Encoding.UTF8.GetBytes(message);
            await _webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, _cts.Token);

            // 等待响应，超时5秒
            var timeoutTask = Task.Delay(5000);
            var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);
            
            if (completedTask == timeoutTask)
            {
                _pendingRequests.Remove(id);
                throw new TimeoutException("命令超时");
            }

            return await tcs.Task;
        }

        /// <summary>
        /// 在页面中执行 JavaScript
        /// </summary>
        public async Task<string> ExecuteScript(string script)
        {
            var result = await SendCommand("Runtime.evaluate", new
            {
                expression = script,
                returnByValue = true,
                awaitPromise = true
            });

            return result;
        }

        /// <summary>
        /// 启用控制台消息监听
        /// </summary>
        public async Task EnableConsoleListener()
        {
            await SendCommand("Runtime.enable");
            Log("控制台监听已启用");
        }

        /// <summary>
        /// 注入消息监听脚本
        /// </summary>
        public async Task InjectMessageListener()
        {
            // 注入脚本来监听 NIM SDK 的消息
            string script = @"
(function() {
    console.log('[ChatBot] 消息监听器注入中...');
    
    // 工具函数：ArrayBuffer 转 Base64
    function arrayBufferToBase64(buffer) {
        var binary = '';
        var bytes = new Uint8Array(buffer);
        var len = bytes.byteLength;
        for (var i = 0; i < len; i++) {
            binary += String.fromCharCode(bytes[i]);
        }
        return btoa(binary);
    }
    
    // 工具函数：尝试解析 Protobuf 为可读格式
    function tryParseProtobuf(data) {
        try {
            var bytes = new Uint8Array(data);
            // 尝试找到可读的字符串部分
            var str = '';
            for (var i = 0; i < bytes.length; i++) {
                if (bytes[i] >= 32 && bytes[i] < 127) {
                    str += String.fromCharCode(bytes[i]);
                } else if (str.length > 3) {
                    str += ' ';
                }
            }
            return str.trim();
        } catch(e) {
            return '';
        }
    }
    
    // 监听窗口消息事件
    window.addEventListener('message', function(e) {
        if (e.data && typeof e.data === 'object') {
            console.log('[ChatBot] WindowMessage: ' + JSON.stringify(e.data).substring(0, 500));
        }
    });
    
    // Hook XMLHttpRequest
    var originalXHROpen = XMLHttpRequest.prototype.open;
    var originalXHRSend = XMLHttpRequest.prototype.send;
    
    XMLHttpRequest.prototype.open = function(method, url) {
        this._chatbot_url = url;
        return originalXHROpen.apply(this, arguments);
    };
    
    XMLHttpRequest.prototype.send = function(body) {
        var self = this;
        var url = this._chatbot_url;
        
        this.addEventListener('load', function() {
            if (url && (url.includes('netease') || url.includes('nim') || url.includes('im'))) {
                try {
                    console.log('[ChatBot] XHR Response [' + url.split('?')[0] + ']: ' + self.responseText.substring(0, 300));
                } catch(e) {}
            }
        });
        
        if (body && url && (url.includes('netease') || url.includes('nim') || url.includes('im'))) {
            console.log('[ChatBot] XHR Request [' + url.split('?')[0] + ']: ' + JSON.stringify(body).substring(0, 300));
        }
        
        return originalXHRSend.apply(this, arguments);
    };
    
    // Hook WebSocket - 增强版，支持二进制解析
    var OriginalWebSocket = window.WebSocket;
    window.WebSocket = function(url, protocols) {
        console.log('[ChatBot] WebSocket 连接: ' + url);
        
        var ws = protocols ? new OriginalWebSocket(url, protocols) : new OriginalWebSocket(url);
        
        // 保存引用以便后续访问
        if (url.includes('netease')) {
            window.__CHATBOT_NIM_WS__ = ws;
        }
        
        var originalSend = ws.send.bind(ws);
        ws.send = function(data) {
            try {
                if (typeof data === 'string') {
                    console.log('[ChatBot] WS Send: ' + data.substring(0, 500));
                } else if (data instanceof ArrayBuffer || data instanceof Uint8Array) {
                    var buf = data instanceof Uint8Array ? data.buffer : data;
                    var readable = tryParseProtobuf(buf);
                    if (readable.length > 10) {
                        console.log('[ChatBot] WS Send [' + buf.byteLength + 'B]: ' + readable.substring(0, 200));
                    }
                }
            } catch(e) {}
            return originalSend(data);
        };
        
        ws.addEventListener('message', function(e) {
            try {
                if (typeof e.data === 'string') {
                    console.log('[ChatBot] WS Recv: ' + e.data.substring(0, 500));
                } else if (e.data instanceof Blob) {
                    e.data.arrayBuffer().then(function(buf) {
                        var readable = tryParseProtobuf(buf);
                        if (readable.length > 10) {
                            console.log('[ChatBot] WS Recv [' + buf.byteLength + 'B]: ' + readable.substring(0, 300));
                        }
                    });
                } else if (e.data instanceof ArrayBuffer) {
                    var readable = tryParseProtobuf(e.data);
                    if (readable.length > 10) {
                        console.log('[ChatBot] WS Recv [' + e.data.byteLength + 'B]: ' + readable.substring(0, 300));
                    }
                }
            } catch(e) {}
        });
        
        return ws;
    };
    window.WebSocket.prototype = OriginalWebSocket.prototype;
    
    // 尝试 Hook NIM SDK 的消息回调
    function hookNIM() {
        // 尝试找到 NIM 实例
        var nimInstance = window.nim || window.NIM || window.__NIM__;
        if (nimInstance && nimInstance.getInstance) {
            console.log('[ChatBot] 尝试 Hook NIM SDK...');
            var instance = nimInstance.getInstance();
            if (instance && instance.on) {
                // 监听消息事件
                instance.on('msg', function(msg) {
                    console.log('[ChatBot] NIM MSG: ' + JSON.stringify(msg).substring(0, 500));
                });
                instance.on('sysmsg', function(msg) {
                    console.log('[ChatBot] NIM SYSMSG: ' + JSON.stringify(msg).substring(0, 500));
                });
                console.log('[ChatBot] NIM SDK Hook 成功!');
            }
        }
    }
    
    // 监控 DOM 变化，捕获新消息
    function setupMsgObserver() {
        var chatContainer = document.querySelector('.message-list, .chat-messages, [class*=""message""][class*=""list""]');
        if (chatContainer) {
            var observer = new MutationObserver(function(mutations) {
                mutations.forEach(function(m) {
                    if (m.addedNodes.length > 0) {
                        m.addedNodes.forEach(function(node) {
                            if (node.nodeType === 1 && node.innerText) {
                                var text = node.innerText.trim();
                                if (text.length > 0 && text.length < 500) {
                                    console.log('[ChatBot] DOM NewMsg: ' + text.substring(0, 200));
                                }
                            }
                        });
                    }
                });
            });
            observer.observe(chatContainer, { childList: true, subtree: true });
            console.log('[ChatBot] DOM 消息监控已启用');
        }
    }
    
    // 尝试访问 Vue 实例并监控消息状态
    setTimeout(function() {
        var vueApp = document.querySelector('#app')?.__vue_app__;
        if (vueApp) {
            console.log('[ChatBot] 检测到 Vue App');
            window.__CHATBOT_VUE__ = vueApp;
            
            // 尝试获取 Vue 组件的消息数据
            try {
                var app = vueApp._instance;
                if (app && app.proxy) {
                    console.log('[ChatBot] Vue Proxy 可用');
                }
            } catch(e) {}
        }
        
        hookNIM();
        setupMsgObserver();
    }, 2000);
    
    console.log('[ChatBot] 消息监听器注入完成!');
    return 'OK';
})();
";
            
            var result = await ExecuteScript(script);
            Log("消息监听脚本已注入");
        }

        /// <summary>
        /// 获取当前页面信息
        /// </summary>
        public async Task<string> GetPageInfo()
        {
            string script = @"
JSON.stringify({
    url: window.location.href,
    title: document.title,
    hasVue: !!document.querySelector('#app')?.__vue_app__,
    hasPinia: !!window.__PINIA__,
    hasNIM: !!window.NIM
});
";
            return await ExecuteScript(script);
        }

        /// <summary>
        /// 模拟发送消息（需要根据实际UI结构调整）
        /// </summary>
        public async Task SendChatMessage(string content)
        {
            // 转义消息内容中的特殊字符
            string escapedContent = content
                .Replace("\\", "\\\\")
                .Replace("'", "\\'")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r");
            
            string script = @"
(function() {
    // 精确查找聊天输入框（排除搜索框）
    var inputBox = null;
    
    // 方法1: 查找 placeholder 包含'输入消息'的输入框
    var allInputs = document.querySelectorAll('textarea, input, [contenteditable]');
    for (var i = 0; i < allInputs.length; i++) {
        var inp = allInputs[i];
        var placeholder = (inp.getAttribute('placeholder') || '').toLowerCase();
        // 选择聊天输入框（包含消息、输入等关键词），排除搜索框
        if (placeholder.indexOf('消息') >= 0 || placeholder.indexOf('输入消息') >= 0) {
            inputBox = inp;
            break;
        }
    }
    
    // 方法2: 如果没找到，查找底部的 textarea（通常是聊天输入框）
    if (!inputBox) {
        var textareas = document.querySelectorAll('textarea');
        if (textareas.length > 0) {
            // 选择最后一个 textarea（通常在页面底部是聊天输入框）
            inputBox = textareas[textareas.length - 1];
        }
    }
    
    // 方法3: 通过 class 名称查找
    if (!inputBox) {
        inputBox = document.querySelector('[class*=""editor""] textarea') ||
                   document.querySelector('[class*=""input""][class*=""message""]') ||
                   document.querySelector('[class*=""chat""] textarea');
    }
    
    if (!inputBox) {
        return 'ERROR: 找不到聊天输入框，请确保已打开聊天对话';
    }
    
    // 聚焦输入框
    inputBox.focus();
    inputBox.click();
    
    // 设置内容
    var msgContent = '" + escapedContent + @"';
    
    if (inputBox.tagName === 'TEXTAREA' || inputBox.tagName === 'INPUT') {
        // 清空并设置新内容
        inputBox.value = msgContent;
        // 触发 input 事件让 Vue 响应
        inputBox.dispatchEvent(new Event('input', { bubbles: true }));
        inputBox.dispatchEvent(new Event('change', { bubbles: true }));
    } else if (inputBox.getAttribute('contenteditable')) {
        inputBox.innerText = msgContent;
        inputBox.dispatchEvent(new Event('input', { bubbles: true }));
    }
    
    // 短暂等待 Vue 更新状态
    return new Promise(function(resolve) {
        setTimeout(function() {
            // 查找并点击发送按钮
            var btns = document.querySelectorAll('button, [role=""button""], .btn');
            var sendBtn = null;
            for (var i = 0; i < btns.length; i++) {
                var txt = (btns[i].innerText || '').trim();
                if (txt === '发送' || txt.indexOf('发送') >= 0) {
                    sendBtn = btns[i];
                    break;
                }
            }
            
            if (sendBtn) {
                sendBtn.click();
                resolve('SUCCESS: 通过点击发送按钮发送');
            } else {
                // 模拟回车发送
                var enterEvent = new KeyboardEvent('keydown', {
                    key: 'Enter',
                    code: 'Enter',
                    keyCode: 13,
                    which: 13,
                    bubbles: true
                });
                inputBox.dispatchEvent(enterEvent);
                resolve('SUCCESS: 通过回车键发送');
            }
        }, 100);
    });
})();
";
            var result = await ExecuteScript(script);
            Log($"发送消息结果: {result}");
        }

        /// <summary>
        /// 获取聊天列表（需要根据实际UI结构调整）
        /// </summary>
        public async Task<string> GetChatList()
        {
            string script = @"
(function() {
    var chats = [];
    
    // 尝试从 Pinia store 获取
    if (window.__CHATBOT_PINIA__) {
        // 需要根据实际 store 结构调整
    }
    
    // 尝试从 DOM 获取
    var chatItems = document.querySelectorAll('.chat-item, .conversation-item, [class*=""chat""][class*=""item""]');
    chatItems.forEach(function(item) {
        chats.push({
            text: item.innerText.substring(0, 100),
            id: item.getAttribute('data-id') || item.id
        });
    });
    
    return JSON.stringify(chats);
})();
";
            return await ExecuteScript(script);
        }

        private void Log(string message)
        {
            OnLog?.Invoke(message);
        }

        #region 自动化功能

        // ==================== A. 自动回复功能 ====================
        
        private bool _autoReplyEnabled = false;
        private string _autoReplyMessage = "您好，我现在忙，稍后回复您！";
        private CancellationTokenSource _autoReplyCts;
        private HashSet<string> _repliedMessages = new HashSet<string>(); // 已回复的消息ID，防止重复回复

        /// <summary>
        /// 启用自动回复
        /// </summary>
        /// <param name="replyMessage">自动回复的消息内容</param>
        public void EnableAutoReply(string replyMessage = null)
        {
            if (!string.IsNullOrEmpty(replyMessage))
                _autoReplyMessage = replyMessage;
            
            _autoReplyEnabled = true;
            _autoReplyCts = new CancellationTokenSource();
            
            // 启动自动回复监控
            Task.Run(() => AutoReplyLoop(_autoReplyCts.Token));
            
            Log($"自动回复已启用，回复内容: {_autoReplyMessage}");
        }

        /// <summary>
        /// 禁用自动回复
        /// </summary>
        public void DisableAutoReply()
        {
            _autoReplyEnabled = false;
            _autoReplyCts?.Cancel();
            Log("自动回复已禁用");
        }

        /// <summary>
        /// 自动回复循环
        /// </summary>
        private async Task AutoReplyLoop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested && _autoReplyEnabled)
            {
                try
                {
                    // 检查是否有新消息
                    var newMessages = await CheckNewMessages();
                    
                    if (!string.IsNullOrEmpty(newMessages) && newMessages != "[]")
                    {
                        Log($"[自动回复] 检测到新消息: {newMessages}");
                        
                        // 发送自动回复
                        await SendChatMessage(_autoReplyMessage);
                        Log($"[自动回复] 已发送: {_autoReplyMessage}");
                    }
                    
                    // 每2秒检查一次
                    await Task.Delay(2000, ct);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Log($"[自动回复] 错误: {ex.Message}");
                    await Task.Delay(5000, ct);
                }
            }
        }

        /// <summary>
        /// 检查是否有新消息
        /// </summary>
        private async Task<string> CheckNewMessages()
        {
            string script = @"
(function() {
    // 查找未读消息标记
    var unreadBadges = document.querySelectorAll('[class*=""unread""], [class*=""badge""], .red-dot');
    var hasUnread = false;
    
    for (var i = 0; i < unreadBadges.length; i++) {
        var badge = unreadBadges[i];
        var text = badge.innerText || badge.textContent || '';
        if (text.trim() !== '' && text.trim() !== '0') {
            hasUnread = true;
            break;
        }
        // 检查是否有小红点
        if (badge.classList.contains('red-dot') || 
            window.getComputedStyle(badge).display !== 'none') {
            hasUnread = true;
            break;
        }
    }
    
    // 检查聊天列表中的最新消息
    var lastMsg = document.querySelector('[class*=""message""]:last-child, .msg-item:last-child');
    var lastMsgText = lastMsg ? lastMsg.innerText : '';
    
    return JSON.stringify({
        hasUnread: hasUnread,
        lastMessage: lastMsgText.substring(0, 200)
    });
})();
";
            return await ExecuteScript(script);
        }

        // ==================== B. 批量群发功能 ====================

        /// <summary>
        /// 批量群发消息
        /// </summary>
        /// <param name="message">要发送的消息</param>
        /// <param name="contactIds">联系人ID列表（为空则发送给所有联系人）</param>
        /// <param name="delayMs">每条消息间隔（毫秒）</param>
        public async Task BatchSendMessage(string message, List<string> contactIds = null, int delayMs = 2000)
        {
            Log($"开始批量群发: {message}");
            
            // 如果没有指定联系人，获取所有联系人
            if (contactIds == null || contactIds.Count == 0)
            {
                var contacts = await GetContactList();
                contactIds = contacts;
            }
            
            int successCount = 0;
            int failCount = 0;
            
            foreach (var contactId in contactIds)
            {
                try
                {
                    Log($"[群发] 正在发送给: {contactId}");
                    
                    // 切换到该联系人的聊天窗口
                    bool switched = await SwitchToContact(contactId);
                    
                    if (switched)
                    {
                        // 等待页面加载
                        await Task.Delay(500);
                        
                        // 发送消息
                        await SendChatMessage(message);
                        successCount++;
                        Log($"[群发] 发送成功: {contactId}");
                    }
                    else
                    {
                        failCount++;
                        Log($"[群发] 切换联系人失败: {contactId}");
                    }
                    
                    // 间隔等待
                    await Task.Delay(delayMs);
                }
                catch (Exception ex)
                {
                    failCount++;
                    Log($"[群发] 发送失败 ({contactId}): {ex.Message}");
                }
            }
            
            Log($"[群发] 完成！成功: {successCount}, 失败: {failCount}");
        }

        /// <summary>
        /// 切换到指定联系人的聊天窗口
        /// </summary>
        private async Task<bool> SwitchToContact(string contactId)
        {
            string script = $@"
(function() {{
    // 方法1: 通过联系人ID点击
    var contactItem = document.querySelector('[data-id=""{contactId}""]') ||
                      document.querySelector('[data-session-id*=""{contactId}""]') ||
                      document.querySelector('[data-uid=""{contactId}""]');
    
    if (contactItem) {{
        contactItem.click();
        return 'SUCCESS';
    }}
    
    // 方法2: 通过联系人名称点击（如果 contactId 是名称）
    var allItems = document.querySelectorAll('[class*=""session""], [class*=""contact""], [class*=""chat-item""]');
    for (var i = 0; i < allItems.length; i++) {{
        var item = allItems[i];
        var text = item.innerText || '';
        if (text.indexOf('{contactId}') >= 0) {{
            item.click();
            return 'SUCCESS';
        }}
    }}
    
    return 'NOT_FOUND';
}})();
";
            var result = await ExecuteScript(script);
            return result != null && result.Contains("SUCCESS");
        }

        // ==================== C. 获取联系人列表 ====================

        /// <summary>
        /// 获取联系人列表
        /// </summary>
        public async Task<List<string>> GetContactList()
        {
            string script = @"
(function() {
    var contacts = [];
    
    // 查找会话列表中的所有联系人
    var sessionItems = document.querySelectorAll(
        '[class*=""session""] [class*=""item""], ' +
        '[class*=""chat""][class*=""list""] > div, ' +
        '[class*=""conversation""][class*=""item""], ' +
        '.session-list > div'
    );
    
    sessionItems.forEach(function(item, index) {
        var id = item.getAttribute('data-id') || 
                 item.getAttribute('data-session-id') || 
                 item.getAttribute('data-uid') ||
                 'contact_' + index;
        
        var name = '';
        var nameEl = item.querySelector('[class*=""name""], [class*=""title""], .nickname');
        if (nameEl) {
            name = nameEl.innerText.trim();
        } else {
            name = item.innerText.split('\n')[0].trim();
        }
        
        var lastMsg = '';
        var msgEl = item.querySelector('[class*=""msg""], [class*=""content""], .last-message');
        if (msgEl) {
            lastMsg = msgEl.innerText.trim();
        }
        
        if (name) {
            contacts.push({
                id: id,
                name: name,
                lastMessage: lastMsg.substring(0, 50)
            });
        }
    });
    
    return JSON.stringify(contacts);
})();
";
            var result = await ExecuteScript(script);
            var contacts = new List<string>();
            
            try
            {
                // 简单解析 JSON 数组
                if (!string.IsNullOrEmpty(result) && result.Contains("["))
                {
                    // 提取 value 字段
                    var match = Regex.Match(result, @"""value"":""(.+?)""");
                    if (match.Success)
                    {
                        var json = match.Groups[1].Value
                            .Replace("\\\"", "\"")
                            .Replace("\\\\", "\\");
                        
                        // 提取所有 id
                        var idMatches = Regex.Matches(json, @"""id"":""([^""]+)""");
                        foreach (Match m in idMatches)
                        {
                            contacts.Add(m.Groups[1].Value);
                        }
                        
                        // 也提取 name 作为备用
                        if (contacts.Count == 0)
                        {
                            var nameMatches = Regex.Matches(json, @"""name"":""([^""]+)""");
                            foreach (Match m in nameMatches)
                            {
                                contacts.Add(m.Groups[1].Value);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"解析联系人列表失败: {ex.Message}");
            }
            
            return contacts;
        }

        /// <summary>
        /// 获取联系人详细信息（返回格式化字符串）
        /// </summary>
        public async Task<string> GetContactListFormatted()
        {
            string script = @"
(function() {
    var contacts = [];
    
    // 查找会话列表
    var sessionItems = document.querySelectorAll(
        '[class*=""session""] [class*=""item""], ' +
        '[class*=""chat""][class*=""list""] > div, ' +
        '[class*=""conversation""], ' +
        '.session-list > div, ' +
        '[class*=""list""] [class*=""item""]'
    );
    
    sessionItems.forEach(function(item, index) {
        if (!item.innerText || item.innerText.trim().length === 0) return;
        
        var id = item.getAttribute('data-id') || 
                 item.getAttribute('data-session-id') || 
                 item.getAttribute('data-uid') ||
                 index.toString();
        
        var text = item.innerText.trim().split('\n');
        var name = text[0] || '未知';
        var lastMsg = text.length > 1 ? text[1] : '';
        
        contacts.push(index + '. ' + name + ' (ID:' + id + ')' + (lastMsg ? ' - ' + lastMsg.substring(0, 30) : ''));
    });
    
    return contacts.join('\\n');
})();
";
            return await ExecuteScript(script);
        }

        // ==================== D. 关键词回复功能 ====================

        private Dictionary<string, string> _keywordReplies = new Dictionary<string, string>();
        private bool _keywordReplyEnabled = false;
        private CancellationTokenSource _keywordReplyCts;

        /// <summary>
        /// 添加关键词回复规则
        /// </summary>
        public void AddKeywordReply(string keyword, string reply)
        {
            _keywordReplies[keyword.ToLower()] = reply;
            Log($"已添加关键词规则: '{keyword}' -> '{reply}'");
        }

        /// <summary>
        /// 移除关键词回复规则
        /// </summary>
        public void RemoveKeywordReply(string keyword)
        {
            if (_keywordReplies.ContainsKey(keyword.ToLower()))
            {
                _keywordReplies.Remove(keyword.ToLower());
                Log($"已移除关键词规则: '{keyword}'");
            }
        }

        /// <summary>
        /// 启用关键词回复
        /// </summary>
        public void EnableKeywordReply()
        {
            _keywordReplyEnabled = true;
            _keywordReplyCts = new CancellationTokenSource();
            
            Task.Run(() => KeywordReplyLoop(_keywordReplyCts.Token));
            
            Log($"关键词回复已启用，共 {_keywordReplies.Count} 条规则");
        }

        /// <summary>
        /// 禁用关键词回复
        /// </summary>
        public void DisableKeywordReply()
        {
            _keywordReplyEnabled = false;
            _keywordReplyCts?.Cancel();
            Log("关键词回复已禁用");
        }

        /// <summary>
        /// 关键词回复循环
        /// </summary>
        private async Task KeywordReplyLoop(CancellationToken ct)
        {
            string lastProcessedMsg = "";
            
            while (!ct.IsCancellationRequested && _keywordReplyEnabled)
            {
                try
                {
                    // 获取最新收到的消息
                    var latestMsg = await GetLatestReceivedMessage();
                    
                    if (!string.IsNullOrEmpty(latestMsg) && latestMsg != lastProcessedMsg)
                    {
                        lastProcessedMsg = latestMsg;
                        
                        // 检查是否匹配关键词
                        foreach (var kv in _keywordReplies)
                        {
                            if (latestMsg.ToLower().Contains(kv.Key))
                            {
                                Log($"[关键词回复] 匹配关键词 '{kv.Key}', 回复: {kv.Value}");
                                await SendChatMessage(kv.Value);
                                break; // 只回复一次
                            }
                        }
                    }
                    
                    await Task.Delay(1500, ct);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Log($"[关键词回复] 错误: {ex.Message}");
                    await Task.Delay(5000, ct);
                }
            }
        }

        /// <summary>
        /// 获取最新收到的消息
        /// </summary>
        private async Task<string> GetLatestReceivedMessage()
        {
            string script = @"
(function() {
    // 查找聊天消息列表中最后一条对方发送的消息
    var messages = document.querySelectorAll(
        '[class*=""message""][class*=""left""], ' +
        '[class*=""msg""][class*=""other""], ' +
        '[class*=""bubble""][class*=""left""], ' +
        '.message-item.left, ' +
        '.msg-left'
    );
    
    if (messages.length > 0) {
        var lastMsg = messages[messages.length - 1];
        var content = lastMsg.querySelector('[class*=""content""], [class*=""text""], .msg-text');
        if (content) {
            return content.innerText.trim();
        }
        return lastMsg.innerText.trim().substring(0, 200);
    }
    
    // 备用方法：获取所有消息中的最后一条
    var allMsgs = document.querySelectorAll('[class*=""message""][class*=""content""], .msg-content');
    if (allMsgs.length > 0) {
        return allMsgs[allMsgs.length - 1].innerText.trim().substring(0, 200);
    }
    
    return '';
})();
";
            var result = await ExecuteScript(script);
            
            // 提取实际的消息内容
            if (!string.IsNullOrEmpty(result))
            {
                var match = Regex.Match(result, @"""value"":""(.*)""");
                if (match.Success)
                {
                    return match.Groups[1].Value
                        .Replace("\\n", "\n")
                        .Replace("\\\"", "\"");
                }
            }
            
            return "";
        }

        /// <summary>
        /// 列出所有关键词规则
        /// </summary>
        public string ListKeywordRules()
        {
            if (_keywordReplies.Count == 0)
                return "暂无关键词规则";
            
            var sb = new StringBuilder();
            int i = 1;
            foreach (var kv in _keywordReplies)
            {
                sb.AppendLine($"{i}. 关键词: '{kv.Key}' -> 回复: '{kv.Value}'");
                i++;
            }
            return sb.ToString();
        }

        #endregion

        /// <summary>
        /// 简单的 JSON 序列化（用于 CDP 参数）
        /// </summary>
        private string SimpleJsonSerialize(object obj)
        {
            if (obj == null) return "null";
            
            var sb = new StringBuilder();
            sb.Append("{");
            
            var properties = obj.GetType().GetProperties();
            bool first = true;
            
            foreach (var prop in properties)
            {
                var value = prop.GetValue(obj);
                if (value == null) continue;
                
                if (!first) sb.Append(",");
                first = false;
                
                sb.Append($"\"{prop.Name}\":");
                
                if (value is string strVal)
                {
                    // 转义字符串中的特殊字符
                    strVal = strVal.Replace("\\", "\\\\")
                                   .Replace("\"", "\\\"")
                                   .Replace("\n", "\\n")
                                   .Replace("\r", "\\r")
                                   .Replace("\t", "\\t");
                    sb.Append($"\"{strVal}\"");
                }
                else if (value is bool boolVal)
                {
                    sb.Append(boolVal ? "true" : "false");
                }
                else if (value is int || value is long || value is double || value is float)
                {
                    sb.Append(value.ToString());
                }
                else
                {
                    sb.Append($"\"{value}\"");
                }
            }
            
            sb.Append("}");
            return sb.ToString();
        }

        public void Dispose()
        {
            _cts.Cancel();
            _webSocket?.Dispose();
            // 注意：不要自动关闭目标进程
        }
    }
}

