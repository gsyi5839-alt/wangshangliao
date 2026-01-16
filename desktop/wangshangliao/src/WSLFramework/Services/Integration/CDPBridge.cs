using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using WSLFramework.Utils;

namespace WSLFramework.Services
{
    /// <summary>
    /// CDP 桥接服务 - 连接旺商聊 Chrome DevTools Protocol
    /// </summary>
    public class CDPBridge : IDisposable
    {
        private ClientWebSocket _webSocket;
        private CancellationTokenSource _cts;
        private int _commandId = 0;
        private readonly ConcurrentDictionary<int, TaskCompletionSource<string>> _pendingCommands;
        
        public bool IsConnected => _webSocket?.State == WebSocketState.Open;
        public string WebSocketUrl { get; private set; }
        public string MyWangShangId { get; private set; }  // 当前登录用户的旺商聊ID
        
        public event Action<bool> OnConnectionChanged;
        public event Action<string> OnMessageReceived;
        public event Action<string> OnLog;
        public event Action<GroupMessageEvent> OnGroupMessage;  // 群消息事件
        
        // 消息轮询
        private System.Timers.Timer _pollTimer;
        private bool _isPolling = false;
        private HashSet<string> _processedMessageIds = new HashSet<string>();
        
        public CDPBridge()
        {
            _pendingCommands = new ConcurrentDictionary<int, TaskCompletionSource<string>>();
        }
        
        /// <summary>
        /// 启动消息轮询
        /// </summary>
        public void StartMessagePolling(int intervalMs = 500)
        {
            if (_pollTimer != null) return;
            
            _pollTimer = new System.Timers.Timer(intervalMs);
            _pollTimer.Elapsed += async (s, e) => await PollMessagesAsync();
            _pollTimer.Start();
            Log("消息轮询已启动");
        }
        
        /// <summary>
        /// 停止消息轮询
        /// </summary>
        public void StopMessagePolling()
        {
            _pollTimer?.Stop();
            _pollTimer?.Dispose();
            _pollTimer = null;
            Log("消息轮询已停止");
        }
        
        /// <summary>
        /// 轮询群聊消息
        /// </summary>
        private async Task PollMessagesAsync()
        {
            if (!IsConnected || _isPolling) return;
            
            _isPolling = true;
            try
            {
                // 使用更安全的方式获取消息 - 直接从 localStorage 读取，不订阅 Vuex store
                // 避免可能触发的 UI 副作用
                // 增强版: 支持解码 type=100 的加密消息 (msg_attach.b)
                var js = @"
                    (function() {
                        try {
                            var msgs = [];
                            
                            // URL安全 Base64 解码函数
                            function urlSafeBase64Decode(input) {
                                if (!input) return null;
                                var base64 = input.replace(/-/g, '+').replace(/_/g, '/');
                                var padding = 4 - (base64.length % 4);
                                if (padding < 4) base64 += '===='.substring(0, padding);
                                try {
                                    return atob(base64);
                                } catch(e) {
                                    return null;
                                }
                            }
                            
                            // 从 attach.b 提取文本内容
                            function decodeAttachB(attachB) {
                                if (!attachB) return '';
                                try {
                                    var decoded = urlSafeBase64Decode(attachB);
                                    if (!decoded || decoded.length < 16) return '';
                                    
                                    // 跳过 16 字节头部
                                    var content = decoded.substring(16);
                                    
                                    // 过滤不可打印字符，保留中文和常见字符
                                    var result = '';
                                    for (var i = 0; i < content.length; i++) {
                                        var code = content.charCodeAt(i);
                                        // 保留: ASCII可打印字符(0x20-0x7E) + 中文(0x4E00-0x9FFF) + 常见标点
                                        if ((code >= 0x20 && code <= 0x7E) || 
                                            (code >= 0x4E00 && code <= 0x9FFF) ||
                                            code === 0x0A || code === 0x0D) {
                                            result += content[i];
                                        }
                                    }
                                    return result.trim();
                                } catch(e) {
                                    return '';
                                }
                            }
                            
                            // 获取消息文本内容
                            function getMessageText(msg) {
                                // 优先使用 text 字段
                                if (msg.text) return msg.text;
                                if (msg.content) return msg.content;
                                
                                // type=100 是自定义加密消息
                                if (msg.type === 100 && msg.attach) {
                                    var attach = msg.attach;
                                    if (typeof attach === 'string') {
                                        try { attach = JSON.parse(attach); } catch(e) {}
                                    }
                                    if (attach && attach.b) {
                                        return decodeAttachB(attach.b);
                                    }
                                }
                                
                                // 尝试从 custom 字段获取
                                if (msg.custom) {
                                    try {
                                        var custom = typeof msg.custom === 'string' ? JSON.parse(msg.custom) : msg.custom;
                                        if (custom && custom.content) return custom.content;
                                    } catch(e) {}
                                }
                                
                                return '';
                            }
                            
                            // 从 managestate 获取当前会话的消息列表
                            var managestate = localStorage.getItem('managestate');
                            if (managestate) {
                                var state = JSON.parse(managestate);
                                
                                // 获取 chatMessageList 中最近的消息
                                if (state.chatMessageList && Array.isArray(state.chatMessageList)) {
                                    var lastIndex = window.__WSL_LAST_MSG_INDEX__ || 0;
                                    var currentList = state.chatMessageList;
                                    
                                    // 只处理新消息
                                    for (var i = lastIndex; i < currentList.length; i++) {
                                        var msg = currentList[i];
                                        if (msg && msg.idClient) {
                                            var text = getMessageText(msg);
                                            msgs.push({
                                                id: msg.idClient || msg.idServer || '',
                                                from: (msg.from || '').toString(),
                                                to: (msg.to || '').toString(),
                                                type: msg.type || 0,
                                                text: text,
                                                time: msg.time || Date.now(),
                                                sessionId: msg.sessionId || msg.target || '',
                                                rawAttach: msg.attach ? (typeof msg.attach === 'string' ? msg.attach : JSON.stringify(msg.attach)) : ''
                                            });
                                        }
                                    }
                                    
                                    // 更新索引
                                    window.__WSL_LAST_MSG_INDEX__ = currentList.length;
                                }
                            }
                            
                            return JSON.stringify(msgs);
                        } catch(e) {
                            return '[]';
                        }
                    })();
                ";
                
                var response = await EvaluateAsync(js);
                var msgJson = ExtractCDPValue(new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(response));
                
                if (!string.IsNullOrEmpty(msgJson) && msgJson != "[]")
                {
                    var messages = new JavaScriptSerializer().Deserialize<List<Dictionary<string, object>>>(msgJson);
                    foreach (var msg in messages)
                    {
                        var msgId = msg.ContainsKey("id") ? msg["id"]?.ToString() : "";
                        if (string.IsNullOrEmpty(msgId) || _processedMessageIds.Contains(msgId)) continue;
                        
                        _processedMessageIds.Add(msgId);
                        
                        // 限制已处理消息ID集合大小
                        if (_processedMessageIds.Count > 1000)
                        {
                            _processedMessageIds.Clear();
                        }
                        
                        var msgType = msg.ContainsKey("type") ? Convert.ToInt32(msg["type"]) : 0;
                        var content = msg.ContainsKey("text") ? msg["text"]?.ToString() : "";
                        var rawAttach = msg.ContainsKey("rawAttach") ? msg["rawAttach"]?.ToString() : "";
                        
                        // 如果是加密消息且JS解码失败，尝试C#解码
                        if (msgType == 100 && string.IsNullOrEmpty(content) && !string.IsNullOrEmpty(rawAttach))
                        {
                            content = TryDecodeAttachB(rawAttach);
                        }
                        
                        var evt = new GroupMessageEvent
                        {
                            MessageId = msgId,
                            FromId = msg.ContainsKey("from") ? msg["from"]?.ToString() : "",
                            GroupId = msg.ContainsKey("sessionId") ? msg["sessionId"]?.ToString().Replace("team-", "") : "",
                            Content = content,
                            Time = msg.ContainsKey("time") ? Convert.ToInt64(msg["time"]) : 0,
                            MsgType = msgType
                        };
                        
                        if (!string.IsNullOrEmpty(evt.Content))
                        {
                            var typeStr = msgType == 100 ? "[加密消息]" : "[普通消息]";
                            Log($"收到群消息 {typeStr}: from={evt.FromId}, group={evt.GroupId}, content={evt.Content}");
                            OnGroupMessage?.Invoke(evt);
                        }
                        else if (msgType == 100)
                        {
                            Log($"[警告] 加密消息解码失败: id={msgId}, attach长度={rawAttach?.Length ?? 0}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // 静默处理轮询异常
                if (ex.Message.Contains("WebSocket"))
                {
                    Log($"消息轮询异常: {ex.Message}");
                }
            }
            finally
            {
                _isPolling = false;
            }
        }
        
        /// <summary>
        /// C# 端解码 attach.b (备用方法，当JS解码失败时使用)
        /// </summary>
        private string TryDecodeAttachB(string rawAttach)
        {
            if (string.IsNullOrEmpty(rawAttach)) return "";
            
            try
            {
                // 解析 JSON
                var attach = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(rawAttach);
                if (attach == null || !attach.ContainsKey("b")) return "";
                
                var b = attach["b"]?.ToString();
                if (string.IsNullOrEmpty(b)) return "";
                
                // URL安全Base64解码
                var base64 = b.Replace('-', '+').Replace('_', '/');
                var padding = 4 - (base64.Length % 4);
                if (padding < 4) base64 += new string('=', padding);
                
                var data = Convert.FromBase64String(base64);
                if (data.Length < 16) return "";
                
                // 跳过16字节头部
                var content = new byte[data.Length - 16];
                Array.Copy(data, 16, content, 0, content.Length);
                
                // 尝试UTF-8解码
                var text = Encoding.UTF8.GetString(content);
                
                // 过滤不可打印字符
                var result = new StringBuilder();
                foreach (var c in text)
                {
                    if (c >= 0x20 && c <= 0x7E) result.Append(c);  // ASCII
                    else if (c >= 0x4E00 && c <= 0x9FFF) result.Append(c);  // 中文
                    else if (c == '\n' || c == '\r') result.Append(c);  // 换行
                }
                
                return result.ToString().Trim();
            }
            catch (Exception ex)
            {
                Log($"[解码] attach.b 解码失败: {ex.Message}");
                return "";
            }
        }
        
        /// <summary>
        /// 连接到旺商聊 CDP
        /// </summary>
        public async Task<bool> ConnectAsync(int port = 9222)
        {
            try
            {
                Log($"正在连接到 CDP 端口 {port}...");
                
                // 获取页面信息
                using (var http = new HttpClient())
                {
                    http.Timeout = TimeSpan.FromSeconds(5);
                    
                    string response;
                    try
                    {
                        response = await http.GetStringAsync($"http://127.0.0.1:{port}/json");
                    }
                    catch (HttpRequestException ex)
                    {
                        Log($"无法连接到 CDP 端口 {port}: {ex.Message}");
                        Log("请确保旺商聊已启动并启用了 --remote-debugging-port=9222");
                        return false;
                    }
                    
                    // BUG FIX: 检查响应是否为空
                    if (string.IsNullOrEmpty(response))
                    {
                        Log("CDP 返回空响应");
                        return false;
                    }
                    
                    var serializer = new JavaScriptSerializer();
                    var pages = serializer.Deserialize<dynamic[]>(response);
                    
                    // BUG FIX: 检查 pages 是否为 null 或空
                    if (pages == null || pages.Length == 0)
                    {
                        Log("未找到任何 CDP 页面");
                        return false;
                    }
                    
                    foreach (var page in pages)
                    {
                        // BUG FIX: 安全访问字典
                        if (page == null) continue;
                        
                        try
                        {
                            var pageDict = page as IDictionary<string, object>;
                            if (pageDict != null && pageDict.ContainsKey("title"))
                            {
                                var title = pageDict["title"]?.ToString() ?? "";
                                if (title == "旺商聊" || title.Contains("旺商聊"))
                                {
                                    if (pageDict.ContainsKey("webSocketDebuggerUrl"))
                        {
                                        WebSocketUrl = pageDict["webSocketDebuggerUrl"]?.ToString();
                            break;
                                    }
                                }
                            }
                        }
                        catch
                        {
                            // 忽略单个页面的解析错误，继续尝试下一个
                            continue;
                        }
                    }
                }
                
                if (string.IsNullOrEmpty(WebSocketUrl))
                {
                    Log("未找到旺商聊页面");
                    return false;
                }
                
                Log($"找到旺商聊 WebSocket: {WebSocketUrl}");
                
                // 连接 WebSocket
                _webSocket = new ClientWebSocket();
                _cts = new CancellationTokenSource();
                
                await _webSocket.ConnectAsync(new Uri(WebSocketUrl), _cts.Token);
                
                Log("CDP WebSocket 连接成功");
                OnConnectionChanged?.Invoke(true);
                
                // 启动接收消息任务
                _ = Task.Run(ReceiveLoop);
                
                return true;
            }
            catch (Exception ex)
            {
                Log($"CDP 连接失败: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 断开连接
        /// </summary>
        public async Task DisconnectAsync()
        {
            try
            {
                _cts?.Cancel();
                
                if (_webSocket?.State == WebSocketState.Open)
                {
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                }
                
                _webSocket?.Dispose();
                _webSocket = null;
                
                Log("CDP 连接已断开");
                OnConnectionChanged?.Invoke(false);
            }
            catch (Exception ex)
            {
                Log($"断开连接异常: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 发送 CDP 命令
        /// </summary>
        public async Task<string> SendCommandAsync(string method, object parameters = null, int timeout = 10000)
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("CDP 未连接");
            }
            
            var id = Interlocked.Increment(ref _commandId);
            var tcs = new TaskCompletionSource<string>();
            _pendingCommands[id] = tcs;
            
            var command = new
            {
                id = id,
                method = method,
                @params = parameters ?? new { }
            };
            
            var serializer = new JavaScriptSerializer();
            var json = serializer.Serialize(command);
            var bytes = Encoding.UTF8.GetBytes(json);
            
            await _webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, _cts.Token);
            
            // 等待响应
            using (var cts = new CancellationTokenSource(timeout))
            {
                cts.Token.Register(() => tcs.TrySetCanceled());
                return await tcs.Task;
            }
        }
        
        /// <summary>
        /// 执行 JavaScript
        /// </summary>
        public async Task<string> EvaluateAsync(string expression, int timeout = 10000)
        {
            return await SendCommandAsync("Runtime.evaluate", new { expression = expression }, timeout);
        }
        
        /// <summary>
        /// 发送群消息
        /// </summary>
        public async Task<bool> SendGroupMessageAsync(string teamId, string message)
        {
            try
            {
                // 【BUG修复】先切换到目标群聊，确保消息发送到正确的群
                if (!string.IsNullOrEmpty(teamId))
                {
                    var switched = await SwitchToGroupAsync(teamId);
                    if (!switched)
                    {
                        Log($"[CDP] 警告: 无法切换到群 {teamId}，尝试直接发送");
                    }
                    else
                    {
                        // 等待UI切换完成
                        await Task.Delay(300);
                    }
                }
                
                // 处理 @消息格式 [LQ:@旺商聊号]
                var processedMessage = ProcessAtMessage(message);
                var escapedMessage = processedMessage.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "");
                
                var js = $@"
                    (function() {{
                        try {{
                            // 查找输入框 (旺商聊使用 #con_edit contenteditable div)
                            var editor = document.querySelector('#con_edit') || 
                                         document.querySelector('[contenteditable=""true""]') ||
                                         document.querySelector('.ql-editor');
                            if (!editor) {{
                                return JSON.stringify({{success: false, error: '未找到输入框'}});
                            }}
                            
                            // 设置消息内容 (使用 innerText 保留换行)
                            editor.innerText = '{escapedMessage}'.replace(/\\n/g, '\n');
                            editor.dispatchEvent(new Event('input', {{ bubbles: true }}));
                            
                            // 查找发送按钮 (旺商聊按钮类名包含 blue-color)
                            setTimeout(function() {{
                                var sendBtn = document.querySelector('button[class*=""blue-color""]') ||
                                              document.querySelector('.send-btn') ||
                                              Array.from(document.querySelectorAll('button')).find(b => b.textContent.includes('发送'));
                            if (sendBtn) {{
                                sendBtn.click();
                            }}
                            }}, 100);
                            
                            return JSON.stringify({{success: true}});
                        }} catch (e) {{
                            return JSON.stringify({{success: false, error: e.message}});
                        }}
                    }})();
                ";
                
                var result = await EvaluateAsync(js);
                Log($"发送群消息结果: {result}");
                return result.Contains("\"success\":true");
            }
            catch (Exception ex)
            {
                Log($"发送群消息失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 发送 @消息
        /// </summary>
        /// <param name="teamId">群ID</param>
        /// <param name="targetId">被@的旺商聊号</param>
        /// <param name="message">消息内容</param>
        public async Task<bool> SendAtMessageAsync(string teamId, string targetId, string message)
        {
            // 构造 @消息格式: [LQ:@旺商聊号] 消息内容
            var atMessage = MessageEncryption.Instance.CreateAtMessage(targetId, message);
            return await SendGroupMessageAsync(teamId, atMessage);
        }

        /// <summary>
        /// 发送 @全体消息
        /// </summary>
        public async Task<bool> SendAtAllMessageAsync(string teamId, string message)
        {
            var atAllMessage = MessageEncryption.Instance.CreateAtAllMessage(message);
            return await SendGroupMessageAsync(teamId, atAllMessage);
        }

        /// <summary>
        /// 处理 @消息格式
        /// 将 [LQ:@旺商聊号] 转换为可显示格式
        /// </summary>
        private string ProcessAtMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
                return message;

            // 检查是否包含 @消息
            if (!MessageEncryption.Instance.HasAtMention(message))
                return message;

            // 获取 @目标列表
            var targets = MessageEncryption.Instance.ParseAtTargets(message);
            
            // 尝试将 [LQ:@id] 替换为 @昵称 格式
            // 这里简单处理，实际应该查询用户昵称
            var processed = message;
            foreach (var target in targets)
            {
                processed = processed.Replace($"[LQ:@{target}]", $"@{target}");
            }
            
            return processed.Replace("[LQ:@all]", "@全体成员");
        }
        
        /// <summary>
        /// 获取当前会话消息
        /// </summary>
        public async Task<string> GetMessagesAsync()
        {
            var js = @"
                (function() {
                    try {
                        var messages = [];
                        var msgItems = document.querySelectorAll('.message-item');
                        msgItems.forEach(function(item) {
                            var nickname = item.querySelector('.nickname');
                            var content = item.querySelector('.message-content');
                            if (nickname && content) {
                                messages.push({
                                    sender: nickname.textContent,
                                    content: content.textContent
                                });
                            }
                        });
                        return JSON.stringify(messages);
                    } catch (e) {
                        return JSON.stringify([]);
                    }
                })();
            ";
            
            return await EvaluateAsync(js);
        }
        
        /// <summary>
        /// 手动解析用户信息JSON，避免JavaScriptSerializer的问题
        /// </summary>
        private WangShangLiaoUserInfo ParseUserInfoJson(string json)
        {
            try
            {
                if (string.IsNullOrEmpty(json)) return null;
                
                var userInfo = new WangShangLiaoUserInfo();
                
                // 使用正则表达式提取字段值
                userInfo.nickname = ExtractJsonValue(json, "nickname");
                userInfo.wwid = ExtractJsonValue(json, "wwid");
                userInfo.account = ExtractJsonValue(json, "account");
                userInfo.avatar = ExtractJsonValue(json, "avatar");
                userInfo.nimId = ExtractJsonValue(json, "nimId");
                userInfo.nimToken = ExtractJsonValue(json, "nimToken");
                userInfo.error = ExtractJsonValue(json, "error");
                
                // 如果有错误，返回null
                if (!string.IsNullOrEmpty(userInfo.error))
                {
                    Log($"用户信息包含错误: {userInfo.error}");
                    return null;
                }
                
                return userInfo;
            }
            catch (Exception ex)
            {
                Log($"手动解析用户信息失败: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 从JSON字符串中提取指定字段的值
        /// </summary>
        private string ExtractJsonValue(string json, string fieldName)
        {
            try
            {
                // 匹配 "fieldName":"value" 或 "fieldName":""
                var pattern = $"\"{fieldName}\"\\s*:\\s*\"([^\"]*)\"";
                var match = System.Text.RegularExpressions.Regex.Match(json, pattern);
                if (match.Success)
                {
                    var value = match.Groups[1].Value;
                    // 解码 Unicode 转义字符
                    value = System.Text.RegularExpressions.Regex.Unescape(value);
                    return value;
                }
                return "";
            }
            catch
            {
                return "";
            }
        }
        
        /// <summary>
        /// 从CDP响应中提取value值
        /// CDP响应格式: {"id":1,"result":{"result":{"type":"string","value":"..."}}}
        /// </summary>
        private string ExtractCDPValue(Dictionary<string, object> cdpResult)
        {
            try
            {
                if (cdpResult == null) return null;
                
                // 第一层 result
                if (!cdpResult.ContainsKey("result")) return null;
                var result1 = cdpResult["result"] as Dictionary<string, object>;
                if (result1 == null) return null;
                
                // 第二层 result (Runtime.evaluate 的返回)
                if (result1.ContainsKey("result"))
                {
                    var result2 = result1["result"] as Dictionary<string, object>;
                    if (result2 != null && result2.ContainsKey("value"))
                    {
                        return result2["value"]?.ToString();
                    }
                }
                
                // 有时只有一层
                if (result1.ContainsKey("value"))
                {
                    return result1["value"]?.ToString();
                }
                
                return null;
            }
            catch (Exception ex)
            {
                Log($"提取CDP值失败: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 获取当前登录用户信息 - 优先从 friendInfo 获取机器人名称，同时获取 NIM Token
        /// </summary>
        public async Task<WangShangLiaoUserInfo> GetCurrentUserInfoAsync()
        {
            try
            {
                // 获取用户信息和 NIM Token
                var js = @"(function() {
                        try {
        var result = { nickname: '', wwid: '', account: '', avatar: '', nimId: '', nimToken: '', debug: '' };
        
        // 方法1: 从 managestate 获取
                            var managestate = localStorage.getItem('managestate');
                            if (managestate) {
            try {
                                var state = JSON.parse(managestate);
                // 获取机器人昵称 (优先 friendInfo)
                if (state.friendInfo && state.friendInfo.nickName) {
                    result.nickname = state.friendInfo.nickName;
                    result.debug = 'friendInfo.nickName';
                }
                // 获取用户基本信息
                                if (state.userInfo) {
                                    var info = state.userInfo;
                                    result.wwid = (info.accountId || info.uid || '').toString();
                                    result.account = info.phone || info.account || '';
                                    result.avatar = info.avatar || '';
                                    result.nimId = (info.nimId || '').toString();
                    // NIM Token 可能在 userInfo 中
                    result.nimToken = info.nimToken || info.token || '';
                    if (!result.nickname) {
                        result.nickname = info.nickName || info.nickname || '';
                        result.debug = 'userInfo.nickName';
                                }
                            }
                // NIM Token 可能在 state 根级别
                if (!result.nimToken && state.nimToken) {
                    result.nimToken = state.nimToken;
                }
                if (!result.nimToken && state.token) {
                    result.nimToken = state.token;
                }
            } catch(e) { result.debug = 'managestate error:' + e.message; }
        }
        
        // 方法2: 从 linestate 获取
        if (!result.wwid || !result.nimToken) {
                                var linestate = localStorage.getItem('linestate');
                                if (linestate) {
                try {
                                    var line = JSON.parse(linestate);
                                    if (line.userInfo) {
                        if (!result.wwid) result.wwid = (line.userInfo.accountId || line.userInfo.uid || '').toString();
                        if (!result.nickname) result.nickname = line.userInfo.nickName || '';
                        if (!result.nimToken) result.nimToken = line.userInfo.nimToken || line.userInfo.token || '';
                        result.debug = result.debug || 'linestate';
                                    }
                    // Token 可能在根级别
                    if (!result.nimToken && line.nimToken) result.nimToken = line.nimToken;
                    if (!result.nimToken && line.token) result.nimToken = line.token;
                } catch(e) {}
                                }
                            }
        
        // 方法3: 从 nimstate 获取 Token (关键！)
        if (!result.nimToken) {
            var nimstate = localStorage.getItem('nimstate');
            if (nimstate) {
                try {
                    var nim = JSON.parse(nimstate);
                    if (nim.token) {
                        result.nimToken = nim.token;
                        result.debug += ',nimstate.token';
                    }
                    if (!result.nimId && nim.accid) {
                        result.nimId = nim.accid;
                    }
                } catch(e) {}
            }
        }
        
        // 方法4: 从其他 NIM SDK localStorage keys 获取 Token
        if (!result.nimToken) {
            var nimKeys = ['nim_token', 'NIM_TOKEN', 'nim-token', '__nim_token__'];
            for (var i = 0; i < nimKeys.length; i++) {
                var t = localStorage.getItem(nimKeys[i]);
                if (t) { result.nimToken = t; result.debug += ',nim_key:' + nimKeys[i]; break; }
            }
        }
        
        // 方法5: 尝试从全局 window 对象获取
        if (!result.nimToken && typeof window !== 'undefined') {
            if (window.nim && window.nim.token) result.nimToken = window.nim.token;
            if (window.nim && window.nim.account && !result.nimId) result.nimId = window.nim.account;
            if (window.__NIM_TOKEN__) result.nimToken = window.__NIM_TOKEN__;
            else if (window.nimToken) result.nimToken = window.nimToken;
            else if (window.NIM && window.NIM.token) result.nimToken = window.NIM.token;
        }
        
                            return JSON.stringify(result);
    } catch (e) { return JSON.stringify({error: e.message}); }
})();";
                
                var response = await EvaluateAsync(js);
                Log($"获取用户信息原始响应: {(response.Length > 150 ? response.Substring(0, 150) + "..." : response)}");
                
                // 解析 CDP 响应 - 格式: {"id":1,"result":{"result":{"type":"string","value":"..."}}}
                var serializer = new JavaScriptSerializer();
                
                try
                {
                    var cdpResult = serializer.Deserialize<Dictionary<string, object>>(response);
                    string userJson = ExtractCDPValue(cdpResult);
                    
                        if (!string.IsNullOrEmpty(userJson))
                        {
                        Log($"提取的用户JSON: {userJson}");
                        
                        // 手动解析 JSON 避免反序列化问题
                        var userInfo = ParseUserInfoJson(userJson);
                        if (userInfo != null)
                        {
                            Log($"解析成功: nickname={userInfo.nickname}, wwid={userInfo.wwid}");
                            // 保存当前用户的旺商聊ID
                            MyWangShangId = userInfo.wwid;
                            return userInfo;
                        }
                    }
                }
                catch (Exception parseEx)
                {
                    Log($"JSON解析异常: {parseEx.Message}");
                }
                
                return null;
            }
            catch (Exception ex)
            {
                Log($"获取用户信息失败: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 获取群聊列表
        /// </summary>
        public async Task<WangShangLiaoGroupInfo[]> GetGroupListAsync()
        {
            try
            {
                var js = @"
                    (function() {
                        try {
                            var groups = [];
                            
                            // 从 managestate 获取群列表 (旺商聊的主要数据源)
                            var managestate = localStorage.getItem('managestate');
                            if (managestate) {
                                var state = JSON.parse(managestate);
                                if (state.groupList) {
                                    // groupList 是对象: {owner: [...], member: [...]}
                                    var groupList = state.groupList;
                                    
                                    // 获取 owner 群 (我创建的群)
                                    if (groupList.owner && Array.isArray(groupList.owner)) {
                                        groupList.owner.forEach(function(g) {
                                            groups.push({
                                                groupId: (g.groupCloudId || g.groupId || g.groupAccount || '').toString(),
                                                groupName: g.groupName || g.name || '',
                                                memberCount: g.groupMemberNum || 0,
                                                avatar: g.groupAvatar || '',
                                                role: 'owner'
                                            });
                                        });
                                    }
                                    
                                    // 获取 member 群 (我加入的群)
                                    if (groupList.member && Array.isArray(groupList.member)) {
                                        groupList.member.forEach(function(g) {
                                        groups.push({
                                                groupId: (g.groupCloudId || g.groupId || g.groupAccount || '').toString(),
                                                groupName: g.groupName || g.name || '',
                                                memberCount: g.groupMemberNum || 0,
                                                avatar: g.groupAvatar || '',
                                                role: 'member'
                                            });
                                        });
                                    }
                                }
                            }
                            
                            return JSON.stringify(groups);
                        } catch (e) {
                            return JSON.stringify({error: e.message});
                        }
                    })();
                ";
                
                var response = await EvaluateAsync(js);
                Log($"获取群列表: {(response.Length > 100 ? response.Substring(0, 100) + "..." : response)}");
                
                // 解析 CDP 响应 - 格式: {"id":1,"result":{"result":{"type":"string","value":"..."}}}
                var serializer = new JavaScriptSerializer();
                var cdpResult = serializer.Deserialize<Dictionary<string, object>>(response);
                
                string groupsJson = ExtractCDPValue(cdpResult);
                        if (!string.IsNullOrEmpty(groupsJson) && !groupsJson.Contains("error"))
                        {
                            var groups = serializer.Deserialize<WangShangLiaoGroupInfo[]>(groupsJson);
                            return groups ?? new WangShangLiaoGroupInfo[0];
                }
                
                return new WangShangLiaoGroupInfo[0];
            }
            catch (Exception ex)
            {
                Log($"获取群列表失败: {ex.Message}");
                return new WangShangLiaoGroupInfo[0];
            }
        }
        
        #region 群管理功能
        
        /// <summary>
        /// 禁言群成员
        /// </summary>
        public async Task<bool> MuteMemberAsync(string teamId, string accid, int duration = 0)
        {
            try
            {
                // duration=0 为永久禁言，duration>0 为秒数
                var js = $@"
                    (function() {{
                        try {{
                            var store = window.__STORE__ || window.$store;
                            if (store && store.dispatch) {{
                                store.dispatch('team/muteMember', {{
                                    teamId: '{teamId}',
                                    accid: '{accid}',
                                    duration: {duration}
                                }});
                                return JSON.stringify({{success: true}});
                            }}
                            
                            // 备用方案：通过 NIM SDK
                            if (window.nim && window.nim.updateMuteStateInTeam) {{
                                window.nim.updateMuteStateInTeam({{
                                    teamId: '{teamId}',
                                    account: '{accid}',
                                    mute: true,
                                    done: function(err, obj) {{
                                        console.log('禁言结果:', err, obj);
                                    }}
                                }});
                                return JSON.stringify({{success: true}});
                            }}
                            
                            return JSON.stringify({{success: false, error: '未找到禁言接口'}});
                        }} catch (e) {{
                            return JSON.stringify({{success: false, error: e.message}});
                        }}
                    }})();
                ";
                
                var result = await EvaluateAsync(js);
                Log($"禁言 {accid} 结果: {result}");
                return result.Contains("\"success\":true");
            }
            catch (Exception ex)
            {
                Log($"禁言失败: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 解除禁言
        /// </summary>
        public async Task<bool> UnmuteMemberAsync(string teamId, string accid)
        {
            try
            {
                var js = $@"
                    (function() {{
                        try {{
                            var store = window.__STORE__ || window.$store;
                            if (store && store.dispatch) {{
                                store.dispatch('team/unmuteMember', {{
                                    teamId: '{teamId}',
                                    accid: '{accid}'
                                }});
                                return JSON.stringify({{success: true}});
                            }}
                            
                            if (window.nim && window.nim.updateMuteStateInTeam) {{
                                window.nim.updateMuteStateInTeam({{
                                    teamId: '{teamId}',
                                    account: '{accid}',
                                    mute: false,
                                    done: function(err, obj) {{
                                        console.log('解禁结果:', err, obj);
                                    }}
                                }});
                                return JSON.stringify({{success: true}});
                            }}
                            
                            return JSON.stringify({{success: false, error: '未找到解禁接口'}});
                        }} catch (e) {{
                            return JSON.stringify({{success: false, error: e.message}});
                        }}
                    }})();
                ";
                
                var result = await EvaluateAsync(js);
                Log($"解禁 {accid} 结果: {result}");
                return result.Contains("\"success\":true");
            }
            catch (Exception ex)
            {
                Log($"解禁失败: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 全体禁言/解禁
        /// </summary>
        public async Task<bool> MuteAllAsync(string teamId, bool mute)
        {
            try
            {
                var js = $@"
                    (function() {{
                        try {{
                            var store = window.__STORE__ || window.$store;
                            if (store && store.dispatch) {{
                                store.dispatch('team/muteAll', {{
                                    teamId: '{teamId}',
                                    mute: {(mute ? "true" : "false")}
                                }});
                                return JSON.stringify({{success: true}});
                            }}
                            
                            if (window.nim && window.nim.updateTeam) {{
                                window.nim.updateTeam({{
                                    teamId: '{teamId}',
                                    muteType: {(mute ? 1 : 0)},
                                    done: function(err, obj) {{
                                        console.log('全体禁言结果:', err, obj);
                                    }}
                                }});
                                return JSON.stringify({{success: true}});
                            }}
                            
                            return JSON.stringify({{success: false, error: '未找到全体禁言接口'}});
                        }} catch (e) {{
                            return JSON.stringify({{success: false, error: e.message}});
                        }}
                    }})();
                ";
                
                var result = await EvaluateAsync(js);
                Log($"全体{(mute ? "禁言" : "解禁")} 结果: {result}");
                return result.Contains("\"success\":true");
            }
            catch (Exception ex)
            {
                Log($"全体禁言/解禁失败: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 修改群名片
        /// </summary>
        public async Task<bool> UpdateMemberCardAsync(string teamId, string accid, string card)
        {
            try
            {
                var escapedCard = card.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n");
                
                var js = $@"
                    (function() {{
                        try {{
                            var store = window.__STORE__ || window.$store;
                            if (store && store.dispatch) {{
                                store.dispatch('team/updateMemberCard', {{
                                    teamId: '{teamId}',
                                    accid: '{accid}',
                                    card: '{escapedCard}'
                                }});
                                return JSON.stringify({{success: true}});
                            }}
                            
                            if (window.nim && window.nim.updateInfoInTeam) {{
                                window.nim.updateInfoInTeam({{
                                    teamId: '{teamId}',
                                    nick: '{escapedCard}',
                                    done: function(err, obj) {{
                                        console.log('修改名片结果:', err, obj);
                                    }}
                                }});
                                return JSON.stringify({{success: true}});
                            }}
                            
                            return JSON.stringify({{success: false, error: '未找到修改名片接口'}});
                        }} catch (e) {{
                            return JSON.stringify({{success: false, error: e.message}});
                        }}
                    }})();
                ";
                
                var result = await EvaluateAsync(js);
                Log($"修改名片 {accid} -> {card} 结果: {result}");
                return result.Contains("\"success\":true");
            }
            catch (Exception ex)
            {
                Log($"修改名片失败: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 踢出群成员
        /// </summary>
        public async Task<bool> KickMemberAsync(string teamId, string accid)
        {
            try
            {
                var js = $@"
                    (function() {{
                        try {{
                            var store = window.__STORE__ || window.$store;
                            if (store && store.dispatch) {{
                                store.dispatch('team/removeMember', {{
                                    teamId: '{teamId}',
                                    accounts: ['{accid}']
                                }});
                                return JSON.stringify({{success: true}});
                            }}
                            
                            if (window.nim && window.nim.removeTeamMembers) {{
                                window.nim.removeTeamMembers({{
                                    teamId: '{teamId}',
                                    accounts: ['{accid}'],
                                    done: function(err, obj) {{
                                        console.log('踢人结果:', err, obj);
                                    }}
                                }});
                                return JSON.stringify({{success: true}});
                            }}
                            
                            return JSON.stringify({{success: false, error: '未找到踢人接口'}});
                        }} catch (e) {{
                            return JSON.stringify({{success: false, error: e.message}});
                        }}
                    }})();
                ";
                
                var result = await EvaluateAsync(js);
                Log($"踢出 {accid} 结果: {result}");
                return result.Contains("\"success\":true");
            }
            catch (Exception ex)
            {
                Log($"踢人失败: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 获取群成员列表
        /// </summary>
        public async Task<WangShangLiaoMemberInfo[]> GetTeamMembersAsync(string teamId)
        {
            try
            {
                var js = $@"
                    (function() {{
                        try {{
                            var members = [];
                            var store = window.__STORE__ || window.$store;
                            
                            if (store && store.state) {{
                                var teamMembers = store.state.teamMembers || 
                                                  store.state.team?.members ||
                                                  store.getters['team/members'];
                                
                                if (teamMembers && teamMembers['{teamId}']) {{
                                    var list = teamMembers['{teamId}'];
                                    if (Array.isArray(list)) {{
                                        list.forEach(function(m) {{
                                            members.push({{
                                                accid: m.accid || m.account || '',
                                                nickname: m.nick || m.nickname || '',
                                                card: m.card || m.teamNick || '',
                                                type: m.type || 0,
                                                muted: m.muted || false
                                            }});
                                        }});
                                    }}
                                }}
                            }}
                            
                            return JSON.stringify(members);
                        }} catch (e) {{
                            return JSON.stringify([]);
                        }}
                    }})();
                ";
                
                var response = await EvaluateAsync(js);
                
                var serializer = new JavaScriptSerializer();
                var cdpResult = serializer.Deserialize<dynamic>(response);
                
                if (cdpResult != null && cdpResult.ContainsKey("result"))
                {
                    var result = cdpResult["result"];
                    if (result.ContainsKey("value"))
                    {
                        var membersJson = result["value"] as string;
                        if (!string.IsNullOrEmpty(membersJson))
                        {
                            var members = serializer.Deserialize<WangShangLiaoMemberInfo[]>(membersJson);
                            return members ?? new WangShangLiaoMemberInfo[0];
                        }
                    }
                }
                
                return new WangShangLiaoMemberInfo[0];
            }
            catch (Exception ex)
            {
                Log($"获取群成员失败: {ex.Message}");
                return new WangShangLiaoMemberInfo[0];
            }
        }
        
        /// <summary>
        /// 启用消息监听
        /// </summary>
        public async Task<bool> EnableMessageListeningAsync()
        {
            try
            {
                var js = @"
                    (function() {
                        try {
                            if (window.__WSL_MSG_HOOKED__) {
                                return JSON.stringify({success: true, message: '已启用'});
                            }
                            
                            // 注入消息拦截
                            var store = window.__STORE__ || window.$store;
                            if (store) {
                                var origCommit = store.commit.bind(store);
                                store.commit = function(type, payload) {
                                    // 拦截消息
                                    if (type === 'chat/ADD_MSG' || type === 'ADD_MESSAGE') {
                                        window.__WSL_LAST_MSG__ = payload;
                                        console.log('[WSL] 新消息:', payload);
                                    }
                                    return origCommit(type, payload);
                                };
                            }
                            
                            // 注入 NIM 消息回调
                            if (window.nim) {
                                var origOnMsg = window.nim.onmsg;
                                window.nim.onmsg = function(msg) {
                                    window.__WSL_LAST_MSG__ = msg;
                                    console.log('[WSL] NIM消息:', msg);
                                    if (origOnMsg) origOnMsg(msg);
                                };
                            }
                            
                            window.__WSL_MSG_HOOKED__ = true;
                            return JSON.stringify({success: true});
                        } catch (e) {
                            return JSON.stringify({success: false, error: e.message});
                        }
                    })();
                ";
                
                var result = await EvaluateAsync(js);
                Log($"启用消息监听结果: {result}");
                return result.Contains("\"success\":true");
            }
            catch (Exception ex)
            {
                Log($"启用消息监听失败: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 获取最新消息
        /// </summary>
        public async Task<GroupMessageEvent> GetLastMessageAsync()
        {
            try
            {
                var js = @"
                    (function() {
                        try {
                            var msg = window.__WSL_LAST_MSG__;
                            if (!msg) return JSON.stringify(null);
                            
                            window.__WSL_LAST_MSG__ = null;
                            
                            return JSON.stringify({
                                groupId: msg.teamId || msg.to || '',
                                senderId: msg.from || msg.fromId || '',
                                senderNick: msg.fromNick || msg.from_nick || '',
                                content: msg.text || msg.content || '',
                                time: msg.time || 0,
                                msgId: msg.idClient || msg.id || ''
                            });
                        } catch (e) {
                            return JSON.stringify(null);
                        }
                    })();
                ";
                
                var response = await EvaluateAsync(js, 3000);
                
                var serializer = new JavaScriptSerializer();
                var cdpResult = serializer.Deserialize<dynamic>(response);
                
                if (cdpResult != null && cdpResult.ContainsKey("result"))
                {
                    var result = cdpResult["result"];
                    if (result.ContainsKey("value"))
                    {
                        var msgJson = result["value"] as string;
                        if (!string.IsNullOrEmpty(msgJson) && msgJson != "null")
                        {
                            return serializer.Deserialize<GroupMessageEvent>(msgJson);
                        }
                    }
                }
                
                return null;
            }
            catch
            {
                return null;
            }
        }
        
        /// <summary>
        /// 切换到指定群聊
        /// </summary>
        public async Task<bool> SwitchToGroupAsync(string teamId)
        {
            try
            {
                Log($"[CDP] 切换到群聊: {teamId}");
                
                var js = $@"
                    (function() {{
                        try {{
                            // 方案1: 使用 Vuex store 切换会话
                            var store = window.__STORE__ || window.$store;
                            if (store && store.dispatch) {{
                                store.dispatch('chat/switchSession', {{
                                    sessionId: 'team-{teamId}',
                                    scene: 'team',
                                    to: '{teamId}'
                                }});
                                return JSON.stringify({{success: true, method: 'vuex'}});
                            }}
                            
                            // 方案2: 直接操作 router
                            if (window.$router) {{
                                window.$router.push({{ path: '/chat/team/' + '{teamId}' }});
                                return JSON.stringify({{success: true, method: 'router'}});
                            }}
                            
                            // 方案3: 点击会话列表项（多种选择器）
                            var selectors = [
                                '[data-team-id=""{teamId}""]',
                                '[data-id=""{teamId}""]',
                                '[data-session-id=""team-{teamId}""]',
                                '.session-item[title*=""{teamId}""]',
                                '.chat-item[data-tid=""{teamId}""]'
                            ];
                            
                            for (var i = 0; i < selectors.length; i++) {{
                                var item = document.querySelector(selectors[i]);
                                if (item) {{
                                    item.click();
                                    return JSON.stringify({{success: true, method: 'click', selector: selectors[i]}});
                                }}
                            }}
                            
                            // 方案4: 遍历会话列表查找匹配项
                            var sessionItems = document.querySelectorAll('.session-item, .chat-item, .chat-list-item');
                            for (var j = 0; j < sessionItems.length; j++) {{
                                var el = sessionItems[j];
                                // 检查元素的各种属性
                                if (el.dataset && (el.dataset.teamId === '{teamId}' || el.dataset.id === '{teamId}')) {{
                                    el.click();
                                    return JSON.stringify({{success: true, method: 'iterate'}});
                                }}
                            }}
                            
                            return JSON.stringify({{success: false, error: '未找到群聊会话项'}});
                        }} catch (e) {{
                            return JSON.stringify({{success: false, error: e.message}});
                        }}
                    }})();
                ";
                
                var result = await EvaluateAsync(js);
                Log($"[CDP] 切换群聊结果: {result}");
                return result.Contains("\"success\":true");
            }
            catch (Exception ex)
            {
                Log($"切换群聊失败: {ex.Message}");
                return false;
            }
        }
        
        #endregion
        
        /// <summary>
        /// 接收消息循环
        /// </summary>
        private async Task ReceiveLoop()
        {
            var buffer = new byte[65536];
            var messageBuffer = new StringBuilder();
            
            try
            {
                while (_webSocket?.State == WebSocketState.Open && !_cts.IsCancellationRequested)
                {
                    var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);
                    
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        break;
                    }
                    
                    var text = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    messageBuffer.Append(text);
                    
                    if (result.EndOfMessage)
                    {
                        var message = messageBuffer.ToString();
                        messageBuffer.Clear();
                        
                        ProcessCDPMessage(message);
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Log($"CDP 接收异常: {ex.Message}");
            }
            
            OnConnectionChanged?.Invoke(false);
        }
        
        /// <summary>
        /// 处理 CDP 消息
        /// </summary>
        private void ProcessCDPMessage(string message)
        {
            try
            {
                var serializer = new JavaScriptSerializer();
                var obj = serializer.Deserialize<dynamic>(message);
                
                // 处理命令响应
                if (obj.ContainsKey("id"))
                {
                    int id = obj["id"];
                    if (_pendingCommands.TryRemove(id, out var tcs))
                    {
                        tcs.TrySetResult(message);
                    }
                }
                
                // 处理事件
                if (obj.ContainsKey("method"))
                {
                    var method = obj["method"];
                    OnMessageReceived?.Invoke(message);
                }
            }
            catch { }
        }
        
        #region 扩展方法 (匹配ZCG协议)

        /// <summary>
        /// 发送私聊消息
        /// </summary>
        public async Task<bool> SendPrivateMessageAsync(string toAccid, string message)
        {
            if (!IsConnected || string.IsNullOrEmpty(toAccid) || string.IsNullOrEmpty(message))
                return false;

            try
            {
                Log($"发送私聊消息: to={toAccid}, msg={message.Substring(0, Math.Min(50, message.Length))}...");

                // 使用CDP执行JS发送私聊消息
                var escapedMsg = message.Replace("\\", "\\\\")
                                        .Replace("'", "\\'")
                                        .Replace("\n", "\\n")
                                        .Replace("\r", "\\r");

                var js = $@"
                (async () => {{
                    try {{
                        const store = window.__STORE__ || window.$store;
                        if (!store) return 'store not found';
                        
                        // 尝试通过store发送消息
                        if (store.dispatch) {{
                            await store.dispatch('chat/sendP2PMessage', {{
                                toAccid: '{toAccid}',
                                text: '{escapedMsg}'
                            }});
                            return 'ok';
                        }}
                        
                        // 尝试使用NIM SDK直接发送
                        const nim = window.nim || window.__NIM__;
                        if (nim) {{
                            await nim.sendText({{
                                scene: 'p2p',
                                to: '{toAccid}',
                                text: '{escapedMsg}'
                            }});
                            return 'ok';
                        }}
                        
                        return 'no handler';
                    }} catch (e) {{
                        return 'error: ' + e.message;
                    }}
                }})();
                ";

                var result = await EvaluateAsync(js, 15000);
                Log($"发送私聊消息结果: {result}");

                return result?.Contains("ok") == true;
            }
            catch (Exception ex)
            {
                Log($"发送私聊消息异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 设置群禁言/解禁 (封装MuteAllAsync)
        /// </summary>
        public async Task<bool> SetGroupMuteAsync(string teamId, bool mute)
        {
            return await MuteAllAsync(teamId, mute);
        }

        /// <summary>
        /// 获取用户信息
        /// </summary>
        public async Task<WangShangLiaoUserInfo> GetUserInfoAsync(string accid)
        {
            if (!IsConnected || string.IsNullOrEmpty(accid))
                return null;

            try
            {
                Log($"获取用户信息: accid={accid}");

                var js = $@"
                (async () => {{
                    try {{
                        const store = window.__STORE__ || window.$store;
                        if (!store) return JSON.stringify({{error: 'store not found'}});
                        
                        // 从store获取用户信息
                        const state = store.state;
                        if (state && state.user && state.user.userInfo) {{
                            const users = state.user.userInfo;
                            const user = users['{accid}'];
                            if (user) {{
                                return JSON.stringify({{
                                    wwid: user.accid || '{accid}',
                                    nickname: user.nick || user.nickname || '',
                                    avatar: user.avatar || ''
                                }});
                            }}
                        }}
                        
                        // 尝试使用NIM SDK获取
                        const nim = window.nim || window.__NIM__;
                        if (nim && nim.getUser) {{
                            const user = await nim.getUser('{accid}');
                            if (user) {{
                                return JSON.stringify({{
                                    wwid: user.accid || '{accid}',
                                    nickname: user.nick || '',
                                    avatar: user.avatar || ''
                                }});
                            }}
                        }}
                        
                        return JSON.stringify({{error: 'user not found'}});
                    }} catch (e) {{
                        return JSON.stringify({{error: e.message}});
                    }}
                }})();
                ";

                var result = await EvaluateAsync(js, 10000);
                Log($"获取用户信息结果: {result}");

                if (!string.IsNullOrEmpty(result))
                {
                    var serializer = new JavaScriptSerializer();
                    return serializer.Deserialize<WangShangLiaoUserInfo>(result);
                }
            }
            catch (Exception ex)
            {
                Log($"获取用户信息异常: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// 获取群成员列表 (封装GetTeamMembersAsync，返回兼容类型)
        /// </summary>
        public async Task<Models.WangShangLiaoMemberInfo[]> GetGroupMembersAsync(string teamId)
        {
            var members = await GetTeamMembersAsync(teamId);
            if (members == null || members.Length == 0)
                return new Models.WangShangLiaoMemberInfo[0];

            // 转换为Models中的类型
            var result = new Models.WangShangLiaoMemberInfo[members.Length];
            for (int i = 0; i < members.Length; i++)
            {
                result[i] = new Models.WangShangLiaoMemberInfo
                {
                    memberId = members[i].accid,
                    nickname = members[i].nickname,
                    card = members[i].card,
                    role = members[i].type
                };
            }
            return result;
        }

        /// <summary>
        /// 修改成员群名片
        /// </summary>
        public async Task<bool> ModifyMemberCardAsync(string groupId, string accid, string newCard)
        {
            return await UpdateMemberCardAsync(groupId, accid, newCard);
        }

        /// <summary>
        /// 接受好友申请
        /// </summary>
        public async Task<bool> AcceptFriendRequestAsync(string fromId)
        {
            if (!IsConnected || string.IsNullOrEmpty(fromId))
                return false;

            try
            {
                Log($"接受好友申请: fromId={fromId}");

                var js = $@"
                (async () => {{
                    try {{
                        const store = window.__STORE__ || window.$store;
                        if (store && store.dispatch) {{
                            await store.dispatch('friend/acceptFriendRequest', {{
                                accountId: '{fromId}'
                            }});
                            return 'ok';
                        }}
                        
                        const nim = window.nim || window.__NIM__;
                        if (nim && nim.passFriendApply) {{
                            await nim.passFriendApply({{
                                account: '{fromId}'
                            }});
                            return 'ok';
                        }}
                        
                        return 'no handler';
                    }} catch (e) {{
                        return 'error: ' + e.message;
                    }}
                }})();
                ";

                var result = await EvaluateAsync(js, 10000);
                Log($"接受好友申请结果: {result}");

                return result?.Contains("ok") == true;
            }
            catch (Exception ex)
            {
                Log($"接受好友申请异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 拒绝好友申请
        /// </summary>
        public async Task<bool> RejectFriendRequestAsync(string fromId, string reason = "")
        {
            if (!IsConnected || string.IsNullOrEmpty(fromId))
                return false;

            try
            {
                Log($"拒绝好友申请: fromId={fromId}");

                var js = $@"
                (async () => {{
                    try {{
                        const store = window.__STORE__ || window.$store;
                        if (store && store.dispatch) {{
                            await store.dispatch('friend/rejectFriendRequest', {{
                                accountId: '{fromId}',
                                reason: '{reason}'
                            }});
                            return 'ok';
                        }}
                        
                        const nim = window.nim || window.__NIM__;
                        if (nim && nim.rejectFriendApply) {{
                            await nim.rejectFriendApply({{
                                account: '{fromId}'
                            }});
                            return 'ok';
                        }}
                        
                        return 'no handler';
                    }} catch (e) {{
                        return 'error: ' + e.message;
                    }}
                }})();
                ";

                var result = await EvaluateAsync(js, 10000);
                Log($"拒绝好友申请结果: {result}");

                return result?.Contains("ok") == true;
            }
            catch (Exception ex)
            {
                Log($"拒绝好友申请异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 检查是否是好友
        /// </summary>
        public async Task<bool> CheckIsFriendAsync(string userId)
        {
            if (!IsConnected || string.IsNullOrEmpty(userId))
                return false;

            try
            {
                var js = $@"
                (async () => {{
                    try {{
                        const store = window.__STORE__ || window.$store;
                        if (store && store.state && store.state.friend) {{
                            const friends = store.state.friend.friendList || [];
                            return friends.some(f => f.account === '{userId}' || f.accid === '{userId}') ? 'true' : 'false';
                        }}
                        
                        const nim = window.nim || window.__NIM__;
                        if (nim && nim.getRelation) {{
                            const relation = await nim.getRelation('{userId}');
                            return relation && relation.isFriend ? 'true' : 'false';
                        }}
                        
                        return 'false';
                    }} catch (e) {{
                        return 'false';
                    }}
                }})();
                ";

                var result = await EvaluateAsync(js, 5000);
                return result?.Contains("true") == true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 获取用户所在群列表
        /// </summary>
        public async Task<List<string>> GetUserGroupsAsync(string userId)
        {
            var result = new List<string>();

            if (!IsConnected || string.IsNullOrEmpty(userId))
                return result;

            try
            {
                var js = $@"
                (async () => {{
                    try {{
                        const store = window.__STORE__ || window.$store;
                        const groups = [];
                        
                        if (store && store.state) {{
                            const teamList = store.state.teamList || store.state.team?.list || [];
                            for (const team of teamList) {{
                                const members = store.state.teamMembers?.[team.teamId] || [];
                                if (members.some(m => m.accid === '{userId}' || m.account === '{userId}')) {{
                                    groups.push(team.teamId || team.id);
                                }}
                            }}
                        }}
                        
                        return JSON.stringify(groups);
                    }} catch (e) {{
                        return '[]';
                    }}
                }})();
                ";

                var response = await EvaluateAsync(js, 10000);
                var serializer = new JavaScriptSerializer();
                var cdpResult = serializer.Deserialize<dynamic>(response);

                if (cdpResult != null && cdpResult.ContainsKey("result"))
                {
                    var resultValue = cdpResult["result"];
                    if (resultValue.ContainsKey("value"))
                    {
                        var groupsJson = resultValue["value"] as string;
                        if (!string.IsNullOrEmpty(groupsJson))
                        {
                            var groups = serializer.Deserialize<string[]>(groupsJson);
                            if (groups != null)
                            {
                                result.AddRange(groups);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"获取用户群列表异常: {ex.Message}");
            }

            return result;
        }

        #endregion
        
        #region API代理 - 通过CDP调用旺商聊内部API
        
        /// <summary>
        /// 通过CDP代理调用旺商聊API
        /// 旺商聊API需要IP白名单，通过客户端代理请求可以绑过限制
        /// </summary>
        /// <param name="endpoint">API端点，如 /v1/group/get-group-list</param>
        /// <param name="method">HTTP方法: GET 或 POST</param>
        /// <param name="data">POST数据（JSON对象）</param>
        public async Task<CdpApiResponse> ProxyApiCallAsync(string endpoint, string method = "GET", object data = null)
        {
            if (!IsConnected)
                return new CdpApiResponse { Success = false, Code = -1, Message = "CDP未连接" };
            
            try
            {
                Log($"[API代理] {method} {endpoint}");
                
                var dataJson = data != null ? new JavaScriptSerializer().Serialize(data) : "null";
                
                var js = $@"
                (async () => {{
                    try {{
                        // 获取认证Token和API基址
                        var token = '';
                        var userId = '';
                        var apiBase = '';
                        
                        // 从 localStorage 获取
                        var managestate = localStorage.getItem('managestate');
                        if (managestate) {{
                            var ms = JSON.parse(managestate);
                            token = ms.token || (ms.userInfo && ms.userInfo.token) || '';
                            userId = (ms.userInfo && ms.userInfo.uid) || '';
                        }}
                        
                        // 从 linestate 获取API基址
                        var linestate = localStorage.getItem('linestate');
                        if (linestate) {{
                            var ls = JSON.parse(linestate);
                            if (ls.buildIn && ls.buildIn.apiDomain) {{
                                apiBase = ls.buildIn.apiDomain;
                            }} else if (ls.apiDomain) {{
                                apiBase = ls.apiDomain;
                            }}
                        }}
                        
                        // 尝试从全局配置获取
                        if (!apiBase && window.__API_BASE__) {{
                            apiBase = window.__API_BASE__;
                        }}
                        if (!apiBase && window.appConfig && window.appConfig.apiBase) {{
                            apiBase = window.appConfig.apiBase;
                        }}
                        
                        // 默认使用测试服务器
                        if (!apiBase) {{
                            apiBase = 'https://qxdevacc.qixin02.xyz';
                        }}
                        
                        // 确保apiBase以https开头
                        if (apiBase && !apiBase.startsWith('http')) {{
                            apiBase = 'https://' + apiBase;
                        }}
                        
                        console.log('[API代理] 使用基址:', apiBase, 'Token:', token ? '有' : '无');
                        
                        // 构建请求
                        var headers = {{
                            'Content-Type': 'application/json',
                            'x-token': token,
                            'x-id': userId.toString(),
                            'Authorization': 'Bearer ' + token
                        }};
                        
                        var options = {{
                            method: '{method}',
                            headers: headers
                        }};
                        
                        if ('{method}' === 'POST' && {dataJson} !== null) {{
                            options.body = JSON.stringify({dataJson});
                        }}
                        
                        var url = apiBase + '{endpoint}';
                        
                        var response = await fetch(url, options);
                        var result = await response.json();
                        
                        return JSON.stringify({{
                            success: result.code === 0,
                            code: result.code || response.status,
                            message: result.msg || result.error || '',
                            data: result.data || null,
                            raw: result
                        }});
                    }} catch (e) {{
                        return JSON.stringify({{
                            success: false,
                            code: -1,
                            message: e.message
                        }});
                    }}
                }})();
                ";
                
                var result = await EvaluateAsync(js, 15000);
                Log($"[API代理] 响应: {result?.Substring(0, Math.Min(200, result?.Length ?? 0))}...");
                
                if (!string.IsNullOrEmpty(result))
                {
                    var serializer = new JavaScriptSerializer();
                    var response = serializer.Deserialize<Dictionary<string, object>>(result);
                    
                    return new CdpApiResponse
                    {
                        Success = response.ContainsKey("success") && Convert.ToBoolean(response["success"]),
                        Code = response.ContainsKey("code") ? Convert.ToInt32(response["code"]) : -1,
                        Message = response.ContainsKey("message") ? response["message"]?.ToString() : "",
                        Data = response.ContainsKey("data") ? response["data"] : null,
                        RawJson = result
                    };
                }
                
                return new CdpApiResponse { Success = false, Code = -1, Message = "无响应" };
            }
            catch (Exception ex)
            {
                Log($"[API代理] 异常: {ex.Message}");
                return new CdpApiResponse { Success = false, Code = -1, Message = ex.Message };
            }
        }
        
        /// <summary>
        /// 通过CDP获取好友列表
        /// </summary>
        public async Task<CdpApiResponse> GetFriendListViaApiAsync()
        {
            return await ProxyApiCallAsync("/v1/friend/get-friend-list", "GET");
        }
        
        /// <summary>
        /// 通过CDP获取群列表
        /// </summary>
        public async Task<CdpApiResponse> GetGroupListViaApiAsync()
        {
            return await ProxyApiCallAsync("/v1/group/get-group-list", "GET");
        }
        
        /// <summary>
        /// 通过CDP获取群信息
        /// </summary>
        public async Task<CdpApiResponse> GetGroupInfoViaApiAsync(string groupId)
        {
            // groupId 需要转为数字
            if (!long.TryParse(groupId, out long gid) || gid <= 0)
            {
                return new CdpApiResponse { Success = false, Code = -1, Message = "groupId无效" };
            }
            return await ProxyApiCallAsync($"/v1/group/get-group-info?groupId={gid}", "GET");
        }
        
        /// <summary>
        /// 通过CDP获取群成员
        /// </summary>
        public async Task<CdpApiResponse> GetGroupMembersViaApiAsync(string groupId)
        {
            if (!long.TryParse(groupId, out long gid) || gid <= 0)
            {
                return new CdpApiResponse { Success = false, Code = -1, Message = "groupId无效" };
            }
            return await ProxyApiCallAsync($"/v1/group/get-group-members?groupId={gid}", "GET");
        }
        
        /// <summary>
        /// 通过CDP设置群禁言 (需要获取旺商聊内部群ID)
        /// </summary>
        public async Task<CdpApiResponse> SetGroupMuteViaApiAsync(string nimGroupId, bool mute)
        {
            Log($"[API禁言] 输入的NIM群ID: {nimGroupId}, mute={mute}");
            
            // 旺商聊API需要的是内部群ID，不是NIM群ID
            // 尝试从CDP获取当前群的旺商聊群ID
            long wslGroupId = 0;
            
            // 方法1: 直接使用输入的ID（如果是有效数字）
            if (long.TryParse(nimGroupId, out long gid) && gid > 0)
            {
                wslGroupId = gid;
            }
            
            // 方法2: 从CDP获取当前群的旺商聊群ID
            if (wslGroupId == 0)
            {
                var wslGid = await GetWslGroupIdFromCDP(nimGroupId);
                if (wslGid > 0)
                {
                    wslGroupId = wslGid;
                    Log($"[API禁言] 从CDP获取到旺商聊群ID: {wslGroupId}");
                }
            }
            
            if (wslGroupId <= 0)
            {
                Log($"[API禁言] 无法获取有效的旺商聊群ID");
                return new CdpApiResponse { Success = false, Code = -1, Message = "无法获取旺商聊群ID" };
            }
            
            Log($"[API禁言] 设置群禁言: wslGroupId={wslGroupId}, mute={mute}");
            return await ProxyApiCallAsync("/v1/group/set-group-mute", "POST", new
            {
                groupId = wslGroupId,
                muteMode = mute ? "MUTE_ALL" : "MUTE_NO"
            });
        }
        
        /// <summary>
        /// 从CDP获取旺商聊群ID (NIM群ID -> 旺商聊群ID)
        /// </summary>
        private async Task<long> GetWslGroupIdFromCDP(string nimGroupId)
        {
            if (!IsConnected) return 0;
            
            try
            {
                // 从localStorage或Vuex获取群ID映射
                var js = $@"
                (function() {{
                    try {{
                        // 方法1: 从当前聊天状态获取
                        var managestate = localStorage.getItem('managestate');
                        if (managestate) {{
                            var ms = JSON.parse(managestate);
                            // 检查当前会话
                            if (ms.currentSession && ms.currentSession.id) {{
                                var sessionId = ms.currentSession.id;
                                // 如果当前会话就是目标群
                                if (sessionId === '{nimGroupId}' || sessionId.toString() === '{nimGroupId}') {{
                                    // 尝试获取对应的旺商聊群ID
                                    if (ms.currentSession.groupId) {{
                                        return ms.currentSession.groupId.toString();
                                    }}
                                }}
                            }}
                            
                            // 检查群列表
                            if (ms.groupList && Array.isArray(ms.groupList)) {{
                                for (var g of ms.groupList) {{
                                    if (g.nimId === '{nimGroupId}' || g.teamId === '{nimGroupId}' || 
                                        g.nimId == {nimGroupId} || g.teamId == {nimGroupId}) {{
                                        return (g.id || g.groupId || g.gid || 0).toString();
                                    }}
                                }}
                            }}
                        }}
                        
                        // 方法2: 从store获取
                        var store = window.__STORE__ || window.$store;
                        if (store && store.state) {{
                            var teams = store.state.team && store.state.team.teams;
                            if (teams && teams['{nimGroupId}']) {{
                                var team = teams['{nimGroupId}'];
                                if (team.custom) {{
                                    try {{
                                        var custom = JSON.parse(team.custom);
                                        if (custom.gid || custom.groupId) {{
                                            return (custom.gid || custom.groupId).toString();
                                        }}
                                    }} catch(e) {{}}
                                }}
                            }}
                        }}
                        
                        // 方法3: 直接返回输入的ID（可能NIM ID就是旺商聊群ID）
                        return '{nimGroupId}';
                    }} catch(e) {{
                        return '0';
                    }}
                }})();
                ";
                
                var result = await EvaluateAsync(js, 5000);
                if (!string.IsNullOrEmpty(result) && long.TryParse(result.Trim('"'), out long wslGid))
                {
                    return wslGid;
                }
            }
            catch (Exception ex)
            {
                Log($"[API禁言] 获取旺商聊群ID失败: {ex.Message}");
            }
            
            return 0;
        }
        
        /// <summary>
        /// 通过CDP设置成员禁言 (需要获取旺商聊群ID和成员ID)
        /// </summary>
        public async Task<CdpApiResponse> SetMemberMuteViaApiAsync(string nimGroupId, string nimMemberId, int duration = 0)
        {
            Log($"[API禁言] 设置成员禁言: nimGroupId={nimGroupId}, nimMemberId={nimMemberId}");
            
            // 获取旺商聊群ID
            long wslGroupId = await GetWslGroupIdFromCDP(nimGroupId);
            if (wslGroupId <= 0 && long.TryParse(nimGroupId, out long gid))
            {
                wslGroupId = gid;
            }
            
            // 成员ID通常就是NIM accid，也是旺商聊用户ID
            if (!long.TryParse(nimMemberId, out long mid) || mid <= 0)
            {
                return new CdpApiResponse { Success = false, Code = -1, Message = "memberId必须是有效数字" };
            }
            
            if (wslGroupId <= 0)
            {
                return new CdpApiResponse { Success = false, Code = -1, Message = "无法获取旺商聊群ID" };
            }
            
            Log($"[API禁言] 设置成员禁言: wslGroupId={wslGroupId}, memberId={mid}, duration={duration}");
            return await ProxyApiCallAsync("/v1/group/set-member-mute", "POST", new
            {
                groupId = wslGroupId,
                memberId = mid,
                duration = duration
            });
        }
        
        /// <summary>
        /// 通过CDP获取用户ID映射
        /// </summary>
        public async Task<CdpApiResponse> GetGidViaApiAsync(string id, int type = 1)
        {
            return await ProxyApiCallAsync("/v1/plugins/get-gid", "POST", new { id, type });
        }
        
        /// <summary>
        /// 通过CDP获取用户信息
        /// </summary>
        public async Task<CdpApiResponse> GetUserInfoViaApiAsync(string id, int type = 1)
        {
            return await ProxyApiCallAsync("/v1/plugins/get-userinfo-by-id", "POST", new { id, type });
        }

        #endregion
        
        #region ★★★ 群信息获取 (解决群名称为空问题) ★★★
        
        /// <summary>
        /// 获取群信息 (通过 NIM SDK)
        /// 群名称存储在 serverCustom.nickname_ciphertext 中，需要 AES 解密
        /// </summary>
        public async Task<WangShangLiaoGroupInfo> GetTeamInfoAsync(string teamId)
        {
            if (string.IsNullOrEmpty(teamId))
            {
                Log("[群信息] teamId 不能为空");
                return null;
            }
            
            try
            {
                // 使用 nim.getTeam() 获取群信息，将结果存储到全局变量
                var js = $@"
                    (function() {{
                        window.__teamInfoResult = null;
                        try {{
                            if (!window.nim) {{
                                window.__teamInfoResult = JSON.stringify({{error: 'NIM SDK 未初始化'}});
                                return window.__teamInfoResult;
                            }}
                            
                            window.nim.getTeam({{
                                teamId: '{teamId}',
                                done: function(error, team) {{
                                    if (error) {{
                                        window.__teamInfoResult = JSON.stringify({{error: error.message || '获取群信息失败'}});
                                        return;
                                    }}
                                    if (!team) {{
                                        window.__teamInfoResult = JSON.stringify({{error: '群不存在'}});
                                        return;
                                    }}
                                    
                                    // 提取群名称加密字段 (支持两种命名)
                                    var nicknameCipher = '';
                                    if (team.serverCustom) {{
                                        try {{
                                            var sc = typeof team.serverCustom === 'string' 
                                                ? JSON.parse(team.serverCustom) 
                                                : team.serverCustom;
                                            // 支持 nickname_ciphertext 和 nicknameCiphertext 两种命名
                                            nicknameCipher = sc.nickname_ciphertext || sc.nicknameCiphertext || '';
                                        }} catch(e) {{
                                            console.log('解析serverCustom失败:', e);
                                        }}
                                    }}
                                    
                                    window.__teamInfoResult = JSON.stringify({{
                                        teamId: team.teamId || '{teamId}',
                                        name: team.name || '',
                                        nicknameCipher: nicknameCipher,
                                        memberCount: team.memberNum || 0,
                                        avatar: team.avatar || '',
                                        owner: team.owner || '',
                                        createTime: team.createTime || 0,
                                        serverCustomRaw: team.serverCustom || ''
                                    }});
                                }}
                            }});
                            return 'pending';
                        }} catch(e) {{
                            window.__teamInfoResult = JSON.stringify({{error: e.message}});
                            return window.__teamInfoResult;
                        }}
                    }})();
                ";
                
                var response = await EvaluateAsyncWithAwait(js);
                if (string.IsNullOrEmpty(response))
                {
                    Log("[群信息] 获取群信息返回空");
                    return null;
                }
                
                // 解析响应
                var data = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(response);
                if (data == null)
                {
                    Log("[群信息] 解析响应失败");
                    return null;
                }
                
                if (data.ContainsKey("error"))
                {
                    Log($"[群信息] 错误: {data["error"]}");
                    return null;
                }
                
                var info = new WangShangLiaoGroupInfo
                {
                    groupId = data.ContainsKey("teamId") ? data["teamId"]?.ToString() : teamId,
                    memberCount = data.ContainsKey("memberCount") ? Convert.ToInt32(data["memberCount"]) : 0,
                    avatar = data.ContainsKey("avatar") ? data["avatar"]?.ToString() : ""
                };
                
                // 解密群名称
                var nicknameCipher = data.ContainsKey("nicknameCipher") ? data["nicknameCipher"]?.ToString() : "";
                if (!string.IsNullOrEmpty(nicknameCipher))
                {
                    info.groupName = DecryptGroupName(nicknameCipher);
                    Log($"[群信息] 解密群名称成功: {info.groupName}");
                }
                else
                {
                    // 如果没有加密名称，使用原始名称
                    info.groupName = data.ContainsKey("name") ? data["name"]?.ToString() : "";
                    Log($"[群信息] 使用原始群名称: {info.groupName}");
                }
                
                return info;
            }
            catch (Exception ex)
            {
                Log($"[群信息] 获取群信息异常: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 解密群名称 (AES-256-CBC)
        /// 密钥和IV与昵称解密相同
        /// </summary>
        private string DecryptGroupName(string ciphertextBase64)
        {
            if (string.IsNullOrWhiteSpace(ciphertextBase64))
                return "";
            
            try
            {
                // 使用与昵称解密相同的密钥和IV
                // 从 SecureConfigService 获取，如果不存在则使用默认值
                var keyStr = "d6ba6647b7c43b79d0e42ceb2790e342"; // 默认密钥
                var ivStr = "kgWRyiiODMjSCh0m";                   // 默认IV
                
                // 尝试从安全配置获取
                try
                {
                    var secureConfig = WSLFramework.Services.Security.SecureConfigService.Instance;
                    keyStr = secureConfig.Get(WSLFramework.Services.Security.SecureConfigService.KEY_NICKNAME_AES_KEY, keyStr);
                    ivStr = secureConfig.Get(WSLFramework.Services.Security.SecureConfigService.KEY_NICKNAME_AES_IV, ivStr);
                }
                catch { /* 忽略配置读取错误 */ }
                
                var key = Encoding.UTF8.GetBytes(keyStr);
                var iv = Encoding.UTF8.GetBytes(ivStr);
                var cipherBytes = Convert.FromBase64String(ciphertextBase64);
                
                using (var aes = System.Security.Cryptography.Aes.Create())
                {
                    aes.Key = key;
                    aes.IV = iv;
                    aes.Mode = System.Security.Cryptography.CipherMode.CBC;
                    aes.Padding = System.Security.Cryptography.PaddingMode.PKCS7;
                    
                    using (var decryptor = aes.CreateDecryptor())
                    using (var ms = new System.IO.MemoryStream(cipherBytes))
                    using (var cs = new System.Security.Cryptography.CryptoStream(ms, decryptor, System.Security.Cryptography.CryptoStreamMode.Read))
                    using (var sr = new System.IO.StreamReader(cs, Encoding.UTF8))
                    {
                        return sr.ReadToEnd();
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"[群名解密] 解密失败: {ex.Message}");
                return "";
            }
        }
        
        /// <summary>
        /// 执行 JavaScript 并等待异步结果 (通过全局变量)
        /// </summary>
        private async Task<string> EvaluateAsyncWithAwait(string expression)
        {
            try
            {
                // 先执行表达式 (启动异步操作)
                var result = await EvaluateAsync(expression);
                if (string.IsNullOrEmpty(result)) return null;
                
                // 解析CDP响应
                var cdpResult = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(result);
                var value = ExtractCDPValue(cdpResult);
                
                // 如果返回 'pending'，说明是异步操作，需要轮询全局变量
                if (value == "pending")
                {
                    // 轮询等待结果 (最多等待3秒)
                    for (int i = 0; i < 6; i++)
                    {
                        await Task.Delay(500);
                        
                        // 读取全局变量中的结果
                        var checkJs = "window.__teamInfoResult";
                        result = await EvaluateAsync(checkJs);
                        cdpResult = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(result);
                        value = ExtractCDPValue(cdpResult);
                        
                        if (!string.IsNullOrEmpty(value) && value != "null")
                        {
                            Log($"[群信息] 第{i + 1}次轮询获取到结果");
                            return value;
                        }
                    }
                    
                    Log("[群信息] 轮询超时，未获取到结果");
                    return null;
                }
                
                // 直接返回的结果
                return value;
            }
            catch (Exception ex)
            {
                Log($"[EvaluateAsyncWithAwait] 错误: {ex.Message}");
                return null;
            }
        }
        
        #endregion
        
        private void Log(string message)
        {
            Logger.Info($"[CDP] {message}");
            OnLog?.Invoke(message);
        }
        
        public void Dispose()
        {
            DisconnectAsync().Wait(5000);
            _cts?.Dispose();
        }
    }
    
    /// <summary>
    /// 旺商聊用户信息
    /// </summary>
    public class WangShangLiaoUserInfo
    {
        public string nickname { get; set; }
        public string wwid { get; set; }
        public string account { get; set; }
        public string avatar { get; set; }
        public string nimId { get; set; }     // NIM SDK 的用户ID
        public string nimToken { get; set; }  // NIM Token (用于登录)
        public string error { get; set; }
    }
    
    /// <summary>
    /// 旺商聊群组信息
    /// </summary>
    public class WangShangLiaoGroupInfo
    {
        public string groupId { get; set; }
        public string groupName { get; set; }
        public int memberCount { get; set; }
        public string avatar { get; set; }
    }
    
    /// <summary>
    /// 群成员信息
    /// </summary>
    public class WangShangLiaoMemberInfo
    {
        public string accid { get; set; }
        public string nickname { get; set; }
        public string card { get; set; }  // 群名片
        public int type { get; set; }     // 0=普通成员, 1=管理员, 2=群主
        public bool muted { get; set; }   // 是否被禁言
    }
    
    /// <summary>
    /// 群消息事件
    /// </summary>
    public class GroupMessageEvent
    {
        public string GroupId { get; set; }
        public string FromId { get; set; }          // 发送者ID
        public string SenderId { get; set; }        // 别名
        public string SenderNick { get; set; }
        public string Content { get; set; }
        public long Time { get; set; }
        public string MessageId { get; set; }       // 消息ID
        public string MsgId { get; set; }           // 别名
        public int MsgType { get; set; }            // 消息类型 (0=文本, 100=加密)
        public string RawAttach { get; set; }       // 原始附件数据
    }
    
    /// <summary>
    /// CDP API代理响应
    /// </summary>
    public class CdpApiResponse
    {
        public bool Success { get; set; }
        public int Code { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }
        public string RawJson { get; set; }
    }
}
