// ChatAutoBotInject - 注入DLL入口点
// 此DLL将被注入到目标聊天软件进程中

using System;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using EasyHook;
using ChatAutoBotInterface;

namespace ChatAutoBotInject
{
    /// <summary>
    /// 注入DLL入口点类
    /// 实现 IEntryPoint 接口，由 EasyHook 自动调用
    /// </summary>
    public class EntryPoint : IEntryPoint
    {
        // IPC通信接口
        private IChatBotController _controller;
        
        // Hook对象
        private LocalHook _sendHook;
        private LocalHook _recvHook;
        private LocalHook _wsaSendHook;
        private LocalHook _wsaRecvHook;
        private LocalHook _sendMessageHook;
        
        // 消息队列
        private Queue<ChatMessage> _receivedMessages = new Queue<ChatMessage>();
        private object _lockObj = new object();
        
        // 运行状态
        private bool _isRunning = true;
        
        // 当前实例引用（用于静态Hook回调）
        private static EntryPoint _instance;

        /// <summary>
        /// 构造函数 - 由EasyHook在注入时调用
        /// 在这里进行初始化，连接到主控程序
        /// </summary>
        /// <param name="context">远程Hook上下文</param>
        /// <param name="channelName">IPC通道名称</param>
        public EntryPoint(RemoteHooking.IContext context, string channelName)
        {
            _instance = this;
            
            try
            {
                // 连接到主控程序的IPC服务
                _controller = RemoteHooking.IpcConnectClient<ChatBotControllerBase>(channelName);
                
                // 测试连接
                _controller.Ping();
                
                _controller.OnLog("INFO", "IPC连接成功");
            }
            catch (Exception ex)
            {
                // 连接失败时记录错误
                throw new Exception("无法连接到主控程序: " + ex.Message);
            }
        }

        /// <summary>
        /// 主运行方法 - 由EasyHook在注入成功后调用
        /// 在这里安装Hook并开始监控
        /// </summary>
        /// <param name="context">远程Hook上下文</param>
        /// <param name="channelName">IPC通道名称</param>
        public void Run(RemoteHooking.IContext context, string channelName)
        {
            try
            {
                // 报告注入成功
                string processName = System.Diagnostics.Process.GetCurrentProcess().ProcessName;
                _controller.OnInjected(RemoteHooking.GetCurrentProcessId(), processName);
                _controller.OnLog("INFO", $"已注入到进程: {processName} (PID: {RemoteHooking.GetCurrentProcessId()})");

                // 安装网络层Hook
                InstallNetworkHooks();
                
                // 安装UI层Hook
                InstallUIHooks();

                // 唤醒目标进程（如果是CreateAndInject创建的）
                RemoteHooking.WakeUpProcess();

                _controller.OnLog("INFO", "所有Hook安装完成，开始监控...");

                // 主循环
                MainLoop();
            }
            catch (Exception ex)
            {
                _controller.OnError(ex);
                _controller.OnLog("ERROR", $"运行时错误: {ex.Message}");
            }
            finally
            {
                // 清理Hook
                CleanupHooks();
            }
        }

        /// <summary>
        /// 安装网络层Hook
        /// </summary>
        private void InstallNetworkHooks()
        {
            try
            {
                // Hook send 函数
                _sendHook = LocalHook.Create(
                    LocalHook.GetProcAddress("ws2_32.dll", "send"),
                    new SendDelegate(SendHook),
                    this);
                _sendHook.ThreadACL.SetExclusiveACL(new int[] { 0 });
                _controller.OnLog("INFO", "Hook send() 成功");
            }
            catch (Exception ex)
            {
                _controller.OnLog("WARN", $"Hook send() 失败: {ex.Message}");
            }

            try
            {
                // Hook recv 函数
                _recvHook = LocalHook.Create(
                    LocalHook.GetProcAddress("ws2_32.dll", "recv"),
                    new RecvDelegate(RecvHook),
                    this);
                _recvHook.ThreadACL.SetExclusiveACL(new int[] { 0 });
                _controller.OnLog("INFO", "Hook recv() 成功");
            }
            catch (Exception ex)
            {
                _controller.OnLog("WARN", $"Hook recv() 失败: {ex.Message}");
            }

            try
            {
                // Hook WSASend 函数
                _wsaSendHook = LocalHook.Create(
                    LocalHook.GetProcAddress("ws2_32.dll", "WSASend"),
                    new WSASendDelegate(WSASendHook),
                    this);
                _wsaSendHook.ThreadACL.SetExclusiveACL(new int[] { 0 });
                _controller.OnLog("INFO", "Hook WSASend() 成功");
            }
            catch (Exception ex)
            {
                _controller.OnLog("WARN", $"Hook WSASend() 失败: {ex.Message}");
            }

            try
            {
                // Hook WSARecv 函数
                _wsaRecvHook = LocalHook.Create(
                    LocalHook.GetProcAddress("ws2_32.dll", "WSARecv"),
                    new WSARecvDelegate(WSARecvHook),
                    this);
                _wsaRecvHook.ThreadACL.SetExclusiveACL(new int[] { 0 });
                _controller.OnLog("INFO", "Hook WSARecv() 成功");
            }
            catch (Exception ex)
            {
                _controller.OnLog("WARN", $"Hook WSARecv() 失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 安装UI层Hook
        /// </summary>
        private void InstallUIHooks()
        {
            try
            {
                // Hook SendMessageW 函数
                _sendMessageHook = LocalHook.Create(
                    LocalHook.GetProcAddress("user32.dll", "SendMessageW"),
                    new SendMessageWDelegate(SendMessageWHook),
                    this);
                _sendMessageHook.ThreadACL.SetExclusiveACL(new int[] { 0 });
                _controller.OnLog("INFO", "Hook SendMessageW() 成功");
            }
            catch (Exception ex)
            {
                _controller.OnLog("WARN", $"Hook SendMessageW() 失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 主循环 - 处理消息、自动回复、群发等
        /// </summary>
        private void MainLoop()
        {
            while (_isRunning)
            {
                try
                {
                    // 心跳检测
                    _controller.Ping();

                    // 处理接收到的消息
                    ProcessReceivedMessages();

                    // 检查并执行群发任务
                    ProcessBroadcastTask();

                    Thread.Sleep(500);
                }
                catch (Exception)
                {
                    // 主控程序断开连接
                    _isRunning = false;
                    break;
                }
            }
        }

        /// <summary>
        /// 处理接收到的消息，执行自动回复
        /// </summary>
        private void ProcessReceivedMessages()
        {
            List<ChatMessage> messages = new List<ChatMessage>();
            
            lock (_lockObj)
            {
                while (_receivedMessages.Count > 0)
                {
                    messages.Add(_receivedMessages.Dequeue());
                }
            }

            foreach (var msg in messages)
            {
                // 通知主控程序
                _controller.OnMessageReceived(msg);

                // 获取自动回复规则
                var rules = _controller.GetAutoReplyRules();
                
                foreach (var rule in rules)
                {
                    if (!rule.Enabled) continue;

                    // 检查是否匹配触发关键词
                    bool matched = false;
                    foreach (var keyword in rule.TriggerKeywords)
                    {
                        try
                        {
                            if (Regex.IsMatch(msg.Content, keyword, RegexOptions.IgnoreCase))
                            {
                                matched = true;
                                break;
                            }
                        }
                        catch
                        {
                            // 如果正则表达式无效，尝试简单匹配
                            if (msg.Content.Contains(keyword))
                            {
                                matched = true;
                                break;
                            }
                        }
                    }

                    if (matched)
                    {
                        string replyContent;

                        if (rule.UseAI)
                        {
                            // 使用AI回复
                            replyContent = _controller.GetAIReply(rule.AIPrompt, msg.Content);
                        }
                        else
                        {
                            // 随机选择预设回复
                            var random = new Random();
                            replyContent = rule.ReplyContents[random.Next(rule.ReplyContents.Count)];
                        }

                        if (!string.IsNullOrEmpty(replyContent))
                        {
                            // 延迟模拟人工输入
                            Thread.Sleep(rule.DelayMs);

                            // 发送回复
                            SendMessage(msg.SenderId, replyContent);

                            _controller.OnLog("INFO", $"自动回复: {msg.SenderName} <- {replyContent}");
                        }

                        break; // 只匹配第一个规则
                    }
                }
            }
        }

        /// <summary>
        /// 处理群发任务
        /// </summary>
        private void ProcessBroadcastTask()
        {
            var task = _controller.GetPendingBroadcastTask();
            if (task == null) return;

            _controller.OnLog("INFO", $"开始执行群发任务: {task.Name}");

            foreach (var targetId in task.TargetIds)
            {
                if (!_isRunning) break;

                try
                {
                    SendMessage(targetId, task.Content);
                    task.SentCount++;
                    _controller.OnLog("INFO", $"群发进度: {task.SentCount}/{task.TargetIds.Count}");
                }
                catch (Exception ex)
                {
                    _controller.OnLog("ERROR", $"群发失败 [{targetId}]: {ex.Message}");
                }

                Thread.Sleep(task.IntervalMs);
            }

            task.Completed = true;
            _controller.OnLog("INFO", $"群发任务完成: {task.Name}");
        }

        /// <summary>
        /// 发送消息（需要根据目标软件具体实现）
        /// </summary>
        /// <param name="targetId">目标ID</param>
        /// <param name="content">消息内容</param>
        private void SendMessage(string targetId, string content)
        {
            // TODO: 根据目标软件的具体实现来发送消息
            // 方法1: 模拟UI操作（FindWindow + SendMessage）
            // 方法2: 调用软件内部的发送函数
            // 方法3: 构造网络数据包发送
            
            _controller.OnLog("DEBUG", $"SendMessage: {targetId} <- {content}");
            
            // 这里是示例实现，实际需要根据软件逆向分析结果来写
            // SimulateTypingAndSend(targetId, content);
        }

        /// <summary>
        /// 清理所有Hook
        /// </summary>
        private void CleanupHooks()
        {
            _sendHook?.Dispose();
            _recvHook?.Dispose();
            _wsaSendHook?.Dispose();
            _wsaRecvHook?.Dispose();
            _sendMessageHook?.Dispose();
        }

        #region Hook委托和实现

        // ===== send Hook =====
        [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
        delegate int SendDelegate(IntPtr socket, IntPtr buffer, int length, int flags);

        [DllImport("ws2_32.dll", SetLastError = true)]
        static extern int send(IntPtr socket, IntPtr buffer, int length, int flags);

        static int SendHook(IntPtr socket, IntPtr buffer, int length, int flags)
        {
            try
            {
                // 捕获发送的数据
                byte[] data = new byte[length];
                Marshal.Copy(buffer, data, 0, length);

                _instance?.ProcessOutgoingData(data);
            }
            catch { }

            // 调用原始函数
            return send(socket, buffer, length, flags);
        }

        // ===== recv Hook =====
        [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
        delegate int RecvDelegate(IntPtr socket, IntPtr buffer, int length, int flags);

        [DllImport("ws2_32.dll", SetLastError = true)]
        static extern int recv(IntPtr socket, IntPtr buffer, int length, int flags);

        static int RecvHook(IntPtr socket, IntPtr buffer, int length, int flags)
        {
            // 调用原始函数
            int result = recv(socket, buffer, length, flags);

            try
            {
                if (result > 0)
                {
                    // 捕获接收的数据
                    byte[] data = new byte[result];
                    Marshal.Copy(buffer, data, 0, result);

                    _instance?.ProcessIncomingData(data);
                }
            }
            catch { }

            return result;
        }

        // ===== WSASend Hook =====
        [StructLayout(LayoutKind.Sequential)]
        struct WSABUF
        {
            public uint len;
            public IntPtr buf;
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
        delegate int WSASendDelegate(
            IntPtr socket,
            IntPtr lpBuffers,
            uint dwBufferCount,
            out uint lpNumberOfBytesSent,
            uint dwFlags,
            IntPtr lpOverlapped,
            IntPtr lpCompletionRoutine);

        [DllImport("ws2_32.dll", SetLastError = true)]
        static extern int WSASend(
            IntPtr socket,
            IntPtr lpBuffers,
            uint dwBufferCount,
            out uint lpNumberOfBytesSent,
            uint dwFlags,
            IntPtr lpOverlapped,
            IntPtr lpCompletionRoutine);

        static int WSASendHook(
            IntPtr socket,
            IntPtr lpBuffers,
            uint dwBufferCount,
            out uint lpNumberOfBytesSent,
            uint dwFlags,
            IntPtr lpOverlapped,
            IntPtr lpCompletionRoutine)
        {
            try
            {
                // 捕获WSA发送的数据
                for (uint i = 0; i < dwBufferCount; i++)
                {
                    WSABUF buf = Marshal.PtrToStructure<WSABUF>(
                        lpBuffers + (int)(i * Marshal.SizeOf<WSABUF>()));
                    
                    if (buf.len > 0 && buf.buf != IntPtr.Zero)
                    {
                        byte[] data = new byte[buf.len];
                        Marshal.Copy(buf.buf, data, 0, (int)buf.len);
                        _instance?.ProcessOutgoingData(data);
                    }
                }
            }
            catch { }

            return WSASend(socket, lpBuffers, dwBufferCount, out lpNumberOfBytesSent, 
                          dwFlags, lpOverlapped, lpCompletionRoutine);
        }

        // ===== WSARecv Hook =====
        [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
        delegate int WSARecvDelegate(
            IntPtr socket,
            IntPtr lpBuffers,
            uint dwBufferCount,
            out uint lpNumberOfBytesRecvd,
            ref uint lpFlags,
            IntPtr lpOverlapped,
            IntPtr lpCompletionRoutine);

        [DllImport("ws2_32.dll", SetLastError = true)]
        static extern int WSARecv(
            IntPtr socket,
            IntPtr lpBuffers,
            uint dwBufferCount,
            out uint lpNumberOfBytesRecvd,
            ref uint lpFlags,
            IntPtr lpOverlapped,
            IntPtr lpCompletionRoutine);

        static int WSARecvHook(
            IntPtr socket,
            IntPtr lpBuffers,
            uint dwBufferCount,
            out uint lpNumberOfBytesRecvd,
            ref uint lpFlags,
            IntPtr lpOverlapped,
            IntPtr lpCompletionRoutine)
        {
            int result = WSARecv(socket, lpBuffers, dwBufferCount, out lpNumberOfBytesRecvd,
                                ref lpFlags, lpOverlapped, lpCompletionRoutine);

            try
            {
                if (result == 0 && lpNumberOfBytesRecvd > 0)
                {
                    for (uint i = 0; i < dwBufferCount; i++)
                    {
                        WSABUF buf = Marshal.PtrToStructure<WSABUF>(
                            lpBuffers + (int)(i * Marshal.SizeOf<WSABUF>()));
                        
                        if (buf.len > 0 && buf.buf != IntPtr.Zero)
                        {
                            int bytesToCopy = (int)Math.Min(buf.len, lpNumberOfBytesRecvd);
                            byte[] data = new byte[bytesToCopy];
                            Marshal.Copy(buf.buf, data, 0, bytesToCopy);
                            _instance?.ProcessIncomingData(data);
                        }
                    }
                }
            }
            catch { }

            return result;
        }

        // ===== SendMessageW Hook =====
        [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true, CharSet = CharSet.Unicode)]
        delegate IntPtr SendMessageWDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern IntPtr SendMessageW(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        const uint WM_SETTEXT = 0x000C;
        const uint WM_GETTEXT = 0x000D;

        static IntPtr SendMessageWHook(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            try
            {
                // 监控文本设置操作
                if (msg == WM_SETTEXT && lParam != IntPtr.Zero)
                {
                    string text = Marshal.PtrToStringUni(lParam);
                    _instance?._controller?.OnLog("UI", $"SetText: {text}");
                }
            }
            catch { }

            return SendMessageW(hWnd, msg, wParam, lParam);
        }

        #endregion

        #region 数据处理方法

        /// <summary>
        /// 处理接收到的网络数据
        /// </summary>
        private void ProcessIncomingData(byte[] data)
        {
            try
            {
                // 尝试解析数据
                // TODO: 根据目标软件的协议格式进行解析
                
                // 这里是示例：尝试解析为UTF8文本
                string text = Encoding.UTF8.GetString(data);
                
                // 记录日志（用于分析协议）
                _controller.OnLog("RECV", $"[{data.Length}字节] {TruncateForLog(text)}");

                // 尝试解析消息
                ChatMessage msg = TryParseMessage(data, text);
                if (msg != null)
                {
                    lock (_lockObj)
                    {
                        _receivedMessages.Enqueue(msg);
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// 处理发送的网络数据
        /// </summary>
        private void ProcessOutgoingData(byte[] data)
        {
            try
            {
                string text = Encoding.UTF8.GetString(data);
                _controller.OnLog("SEND", $"[{data.Length}字节] {TruncateForLog(text)}");
            }
            catch { }
        }

        /// <summary>
        /// 尝试解析消息
        /// TODO: 需要根据目标软件的协议格式具体实现
        /// </summary>
        private ChatMessage TryParseMessage(byte[] data, string text)
        {
            // 这里需要根据目标软件的具体协议来解析
            // 可能是JSON、XML、Protobuf或自定义格式
            
            // 示例：简单的JSON解析尝试
            if (text.Contains("\"content\"") || text.Contains("\"msg\"") || text.Contains("\"text\""))
            {
                return new ChatMessage
                {
                    MessageId = Guid.NewGuid().ToString(),
                    SenderId = "unknown",
                    SenderName = "未知",
                    ReceiverId = "self",
                    ReceiverName = "我",
                    Type = MessageType.Text,
                    Content = text,
                    Timestamp = DateTime.Now,
                    RawData = data
                };
            }

            return null;
        }

        /// <summary>
        /// 截断文本用于日志显示
        /// </summary>
        private string TruncateForLog(string text)
        {
            // 移除换行符，截断到200字符
            text = text.Replace("\r", "").Replace("\n", " ");
            if (text.Length > 200)
            {
                text = text.Substring(0, 200) + "...";
            }
            return text;
        }

        #endregion
    }
}

