using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Automation;
using WangShangLiaoBot.Models;
using WangShangLiaoBot.Utils;

namespace WangShangLiaoBot.Services
{
    /// <summary>
    /// 聊天服务 - 负责与旺商聊应用通信
    /// </summary>
    public class ChatService
    {
        private static ChatService _instance;
        private static readonly object _lock = new object();
        
        /// <summary>单例实例</summary>
        public static ChatService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                            _instance = new ChatService();
                    }
                }
                return _instance;
            }
        }
        
        private ClientWebSocket _webSocket;
        private CancellationTokenSource _cts;
        private int _messageId = 0;
        
        /// <summary>是否已连接</summary>
        public bool IsConnected { get; private set; }
        
        /// <summary>连接模式</summary>
        public ConnectionMode Mode { get; private set; }
        
        /// <summary>WebSocket URL</summary>
        public string WebSocketUrl { get; private set; }
        
        /// <summary>旺商聊主窗口句柄</summary>
        private IntPtr _mainWindowHandle;
        
        /// <summary>UI Automation 元素</summary>
        private AutomationElement _mainWindow;
        
        /// <summary>检测到的旺商聊路径</summary>
        public string DetectedExePath { get; private set; }
        
        /// <summary>消息轮询定时器</summary>
        private System.Timers.Timer _messagePollingTimer;
        
        /// <summary>Hook消息轮询定时器（NIM SDK hook）</summary>
        private System.Timers.Timer _hookedMessagePollingTimer;
        
        /// <summary>是否正在轮询消息</summary>
        public bool IsPollingMessages { get; private set; }
        
        /// <summary>是否正在轮询Hook消息</summary>
        public bool IsPollingHookedMessages { get; private set; }
        
        /// <summary>已处理的消息内容哈希（防止重复处理）</summary>
        private HashSet<string> _processedMessageHashes = new HashSet<string>();
        
        /// <summary>Hook消息已处理哈希（防止重复处理）</summary>
        private HashSet<string> _processedHookedMessageHashes = new HashSet<string>();
        
        /// <summary>消息接收事件</summary>
        public event Action<ChatMessage> OnMessageReceived;
        
        /// <summary>连接状态变更事件</summary>
        public event Action<bool> OnConnectionChanged;
        
        /// <summary>日志事件</summary>
        public event Action<string> OnLog;
        
        private ChatService() { }
        
        /// <summary>
        /// 自动扫描并获取旺商聊exe路径（多种方式检测）
        /// </summary>
        private string AutoDetectExePath()
        {
            Log("开始自动检测旺商聊安装路径...");
            
            // 1. 从已运行的进程获取路径（最准确）
            try
            {
                var processes = Process.GetProcessesByName("wangshangliao_win_online");
                if (processes.Length > 0)
                {
                    var exePath = processes[0].MainModule?.FileName;
                    if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
                    {
                        Log($"✓ 从运行进程检测到: {exePath}");
                        return exePath;
                    }
                }
            }
            catch { }
            
            // 2. 从注册表查找（卸载信息）
            try
            {
                var regPaths = new[]
                {
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                    @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
                };
                
                foreach (var regPath in regPaths)
                {
                    using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(regPath))
                    {
                        if (key != null)
                        {
                            foreach (var subKeyName in key.GetSubKeyNames())
                            {
                                using (var subKey = key.OpenSubKey(subKeyName))
                                {
                                    var displayName = subKey?.GetValue("DisplayName")?.ToString() ?? "";
                                    if (displayName.Contains("旺商聊") || displayName.Contains("wangshangliao"))
                                    {
                                        var installPath = subKey?.GetValue("InstallLocation")?.ToString();
                                        if (!string.IsNullOrEmpty(installPath))
                                        {
                                            var exePath = Path.Combine(installPath, "wangshangliao_win_online.exe");
                                            if (File.Exists(exePath))
                                            {
                                                Log($"✓ 从注册表检测到: {exePath}");
                                                return exePath;
                                            }
                                        }
                                        
                                        // 尝试从 UninstallString 获取路径
                                        var uninstall = subKey?.GetValue("UninstallString")?.ToString();
                                        if (!string.IsNullOrEmpty(uninstall))
                                        {
                                            var dir = Path.GetDirectoryName(uninstall.Replace("\"", ""));
                                            var exePath = Path.Combine(dir, "wangshangliao_win_online.exe");
                                            if (File.Exists(exePath))
                                            {
                                                Log($"✓ 从卸载路径检测到: {exePath}");
                                                return exePath;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch { }
            
            // 3. 从用户配置获取
            var config = ConfigService.Instance.Config;
            if (!string.IsNullOrEmpty(config.WangShangLiaoPath))
            {
                var configPath = Path.Combine(config.WangShangLiaoPath, 
                    "wangshangliao_win_online", "wangshangliao_win_online.exe");
                if (File.Exists(configPath))
                {
                    Log($"✓ 从配置路径检测到: {configPath}");
                    return configPath;
                }
                
                // 也尝试直接在配置路径下查找
                var directPath = Path.Combine(config.WangShangLiaoPath, "wangshangliao_win_online.exe");
                if (File.Exists(directPath))
                {
                    Log($"✓ 从配置路径检测到: {directPath}");
                    return directPath;
                }
            }
            
            // 4. 扫描常见安装路径
            var commonPaths = new List<string>
            {
                // 中文路径
                @"C:\旺商聊\wangshangliao_win_online\wangshangliao_win_online.exe",
                @"D:\旺商聊\wangshangliao_win_online\wangshangliao_win_online.exe",
                @"E:\旺商聊\wangshangliao_win_online\wangshangliao_win_online.exe",
                @"F:\旺商聊\wangshangliao_win_online\wangshangliao_win_online.exe",
                
                // Program Files
                @"C:\Program Files\wangshangliao_win_online\wangshangliao_win_online.exe",
                @"C:\Program Files (x86)\wangshangliao_win_online\wangshangliao_win_online.exe",
                
                // 用户目录
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
                    @"Programs\wangshangliao_win_online\wangshangliao_win_online.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                    @"wangshangliao_win_online\wangshangliao_win_online.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), 
                    @"旺商聊\wangshangliao_win_online\wangshangliao_win_online.exe"),
                    
                // 用户下载目录
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), 
                    @"Downloads\旺商聊\wangshangliao_win_online\wangshangliao_win_online.exe"),
            };
            
            foreach (var path in commonPaths)
            {
                if (File.Exists(path))
                {
                    Log($"✓ 在常见路径检测到: {path}");
                    return path;
                }
            }
            
            // 5. 扫描所有固定磁盘的根目录
            Log("正在扫描磁盘...");
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (drive.DriveType == DriveType.Fixed && drive.IsReady)
                {
                    // 搜索常见文件夹名
                    var folderNames = new[] { "旺商聊", "wangshangliao", "WangShangLiao" };
                    foreach (var folder in folderNames)
                    {
                        var searchPath = Path.Combine(drive.Name, folder, 
                            "wangshangliao_win_online", "wangshangliao_win_online.exe");
                        if (File.Exists(searchPath))
                        {
                            Log($"✓ 在磁盘 {drive.Name} 检测到: {searchPath}");
                            return searchPath;
                        }
                        
                        // 也检查直接在文件夹下
                        var directPath = Path.Combine(drive.Name, folder, "wangshangliao_win_online.exe");
                        if (File.Exists(directPath))
                        {
                            Log($"✓ 在磁盘 {drive.Name} 检测到: {directPath}");
                            return directPath;
                        }
                    }
                }
            }
            
            // 6. 深度搜索（搜索一级子目录）
            Log("正在深度搜索...");
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (drive.DriveType == DriveType.Fixed && drive.IsReady)
                {
                    try
                    {
                        foreach (var dir in Directory.GetDirectories(drive.Name))
                        {
                            var exePath = Path.Combine(dir, "wangshangliao_win_online", "wangshangliao_win_online.exe");
                            if (File.Exists(exePath))
                            {
                                Log($"✓ 深度搜索检测到: {exePath}");
                                return exePath;
                            }
                        }
                    }
                    catch { } // 忽略权限问题
                }
            }
            
            Log("✗ 未找到旺商聊安装路径");
            return null;
        }
        
        /// <summary>
        /// 启动旺商聊应用（调试模式）
        /// </summary>
        public bool StartApp()
        {
            try
            {
                var config = ConfigService.Instance.Config;
                var exePath = Path.Combine(config.WangShangLiaoPath, 
                    "wangshangliao_win_online", "wangshangliao_win_online.exe");
                
                if (!File.Exists(exePath))
                {
                    Log($"找不到旺商聊程序: {exePath}");
                    return false;
                }
                
                // 检查是否已在运行
                var processes = Process.GetProcessesByName("wangshangliao_win_online");
                if (processes.Length > 0)
                {
                    Log("旺商聊已在运行");
                    return true;
                }
                
                // 启动带调试端口的进程
                var startInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = $"--remote-debugging-port={config.DebugPort}",
                    WorkingDirectory = Path.GetDirectoryName(exePath),
                    UseShellExecute = true
                };
                
                Process.Start(startInfo);
                Log($"旺商聊已启动，调试端口: {config.DebugPort}");
                return true;
            }
            catch (Exception ex)
            {
                Log($"启动旺商聊失败: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 旺商聊是否已运行但未开启调试模式
        /// </summary>
        public bool IsRunningWithoutDebug { get; private set; }
        
        /// <summary>
        /// 自动启动并连接到旺商聊（推荐方法）
        /// 自动扫描端口，尝试连接已运行的旺商聊
        /// </summary>
        public async Task<bool> LaunchAndConnectAsync()
        {
            var config = ConfigService.Instance.Config;
            IsRunningWithoutDebug = false;
            
            Log("========== 开始自动连接 ==========");
            
            // 先尝试检测路径（用于错误提示）
            DetectedExePath = AutoDetectExePath();
            
            // 1. 检查旺商聊是否已运行
            var processes = Process.GetProcessesByName("wangshangliao_win_online");
            if (processes.Length == 0)
            {
                Log("✗ 旺商聊未运行，请先启动旺商聊");
                return false;
            }
            
            Log(string.Format("✓ 检测到旺商聊进程 (PID: {0})", processes[0].Id));
            
            // 2. 先检查进程命令行是否有调试端口
            var cmdLine = GetProcessCommandLine(processes[0].Id);
            if (!string.IsNullOrEmpty(cmdLine))
            {
                var cmdMatch = System.Text.RegularExpressions.Regex.Match(
                    cmdLine, @"--remote-debugging-port=(\d+)");
                if (cmdMatch.Success)
                {
                    var existingPort = int.Parse(cmdMatch.Groups[1].Value);
                    Log(string.Format("✓ 进程已有调试端口: {0}", existingPort));
                    if (await TryConnectToDebugPort(existingPort))
                    {
                        config.DebugPort = existingPort;
                        var wsResult = await ConnectToWebSocket();
                        if (wsResult)
                        {
                            Log("✓ 连接成功！");
                            return true;
                        }
                    }
                }
            }
            
            // 3. 快速扫描常见端口（只扫描几个最常用的）
            Log("正在快速扫描调试端口...");
            var portsToScan = new[] { config.DebugPort, 9222, 9229, 9333 };
            
            foreach (var port in portsToScan)
            {
                if (await TryConnectToDebugPort(port))
                {
                    config.DebugPort = port;
                    var wsResult = await ConnectToWebSocket();
                    if (wsResult)
                    {
                        Log("✓ 连接成功！");
                        return true;
                    }
                }
            }
            
            // 4. 检查 DevToolsActivePort 文件
            Log("检查 DevToolsActivePort...");
            if (await TryInjectDebugPort(processes[0], config.DebugPort))
            {
                await Task.Delay(1000); // 等待端口启动
                if (await TryConnectToDebugPort(config.DebugPort))
                {
                    var wsResult = await ConnectToWebSocket();
                    if (wsResult)
                    {
                        Log("✓ 注入成功并连接！");
                        Mode = ConnectionMode.CDP;
                        return true;
                    }
                }
            }
            
            // 5. CDP 失败，尝试 UI Automation 方式
            Log("CDP 连接失败，尝试 UI Automation 方式...");
            if (await ConnectWithUIAutomationAsync())
            {
                Log("✓ UI Automation 连接成功！");
                return true;
            }
            
            // 6. 如果所有方法都失败，标记状态
            Log("✗ 所有连接方式均失败");
            
            // 从进程获取路径
            if (string.IsNullOrEmpty(DetectedExePath))
            {
                try
                {
                    DetectedExePath = processes[0].MainModule?.FileName;
                    Log(string.Format("从进程获取路径: {0}", DetectedExePath));
                }
                catch { }
            }
            
            IsRunningWithoutDebug = true;
            return false;
        }
        
        /// <summary>
        /// 尝试动态注入调试端口到已运行的 Electron 进程
        /// </summary>
        private async Task<bool> TryInjectDebugPort(Process process, int port)
        {
            try
            {
                Log(string.Format("尝试向进程 {0} 注入调试端口 {1}...", process.Id, port));
                
                // 方法1: 通过命令行参数检查是否已有调试端口
                var cmdLine = GetProcessCommandLine(process.Id);
                if (!string.IsNullOrEmpty(cmdLine))
                {
                    Log(string.Format("进程命令行: {0}", cmdLine));
                    var match = System.Text.RegularExpressions.Regex.Match(
                        cmdLine, @"--remote-debugging-port=(\d+)");
                    if (match.Success)
                    {
                        var existingPort = int.Parse(match.Groups[1].Value);
                        Log(string.Format("✓ 进程已有调试端口: {0}", existingPort));
                        ConfigService.Instance.Config.DebugPort = existingPort;
                        return true;
                    }
                }
                
                // 方法2: 尝试通过 Windows API 发送信号启用调试
                // 注意: 这种方法对大多数 Electron 应用无效，但值得一试
                Log("尝试通过信号启用调试端口...");
                
                // 方法3: 检查用户数据目录中的调试配置
                var userDataPath = GetElectronUserDataPath(process);
                if (!string.IsNullOrEmpty(userDataPath))
                {
                    Log(string.Format("Electron 用户数据目录: {0}", userDataPath));
                    
                    // 检查 DevToolsActivePort 文件
                    var devToolsPortFile = Path.Combine(userDataPath, "DevToolsActivePort");
                    if (File.Exists(devToolsPortFile))
                    {
                        var lines = File.ReadAllLines(devToolsPortFile);
                        if (lines.Length > 0)
                        {
                            int activePort;
                            if (int.TryParse(lines[0].Trim(), out activePort))
                            {
                                Log(string.Format("✓ 从 DevToolsActivePort 文件发现端口: {0}", activePort));
                                ConfigService.Instance.Config.DebugPort = activePort;
                                return true;
                            }
                        }
                    }
                }
                
                Log("✗ 无法动态注入调试端口");
                return false;
            }
            catch (Exception ex)
            {
                Log(string.Format("注入失败: {0}", ex.Message));
                return false;
            }
        }
        
        /// <summary>
        /// 获取进程命令行参数
        /// </summary>
        private string GetProcessCommandLine(int processId)
        {
            try
            {
                using (var searcher = new System.Management.ManagementObjectSearcher(
                    string.Format("SELECT CommandLine FROM Win32_Process WHERE ProcessId = {0}", processId)))
                {
                    foreach (var obj in searcher.Get())
                    {
                        return obj["CommandLine"]?.ToString();
                    }
                }
            }
            catch { }
            return null;
        }
        
        /// <summary>
        /// 获取 Electron 应用的用户数据目录
        /// </summary>
        private string GetElectronUserDataPath(Process process)
        {
            try
            {
                // 尝试从命令行获取
                var cmdLine = GetProcessCommandLine(process.Id);
                if (!string.IsNullOrEmpty(cmdLine))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(
                        cmdLine, @"--user-data-dir=""?([^""]+)""?");
                    if (match.Success)
                    {
                        return match.Groups[1].Value;
                    }
                }
                
                // 默认路径: %APPDATA%\应用名称
                var appName = Path.GetFileNameWithoutExtension(process.MainModule?.FileName ?? "");
                if (!string.IsNullOrEmpty(appName))
                {
                    var defaultPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        appName);
                    if (Directory.Exists(defaultPath))
                    {
                        return defaultPath;
                    }
                    
                    // 也检查 Local AppData
                    var localPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        appName);
                    if (Directory.Exists(localPath))
                    {
                        return localPath;
                    }
                }
            }
            catch { }
            return null;
        }
        
        /// <summary>
        /// 强制重启旺商聊并连接（会关闭已运行的实例）
        /// </summary>
        public async Task<bool> ForceRestartAndConnectAsync()
        {
            var config = ConfigService.Instance.Config;
            
            // 先保存检测到的路径（在关闭进程前）
            if (string.IsNullOrEmpty(DetectedExePath))
            {
                DetectedExePath = AutoDetectExePath();
            }
            
            // 关闭已运行的旺商聊
            Log("========== 强制重启旺商聊 ==========");
            var processes = Process.GetProcessesByName("wangshangliao_win_online");
            Log($"正在关闭 {processes.Length} 个旺商聊进程...");
            
            foreach (var p in processes)
            {
                try 
                { 
                    p.Kill(); 
                    p.WaitForExit(5000);
                    Log($"✓ 进程 {p.Id} 已关闭");
                } 
                catch (Exception ex) 
                { 
                    Log($"✗ 关闭进程 {p.Id} 失败: {ex.Message}"); 
                }
            }
            
            // 等待进程完全退出
            Log("等待进程完全退出...");
            await Task.Delay(2000);
            
            // 再次检查
            processes = Process.GetProcessesByName("wangshangliao_win_online");
            if (processes.Length > 0)
            {
                Log($"⚠ 警告: 仍有 {processes.Length} 个进程未关闭，尝试强制结束...");
                foreach (var p in processes)
                {
                    try { p.Kill(); } catch { }
                }
                await Task.Delay(1000);
            }
            
            IsRunningWithoutDebug = false;
            
            // 检查是否有保存的路径
            if (string.IsNullOrEmpty(DetectedExePath))
            {
                Log("✗ 未找到旺商聊路径，无法重启");
                return false;
            }
            
            // 使用检测到的路径启动
            Log($"正在以调试模式重启旺商聊...");
            Log($"路径: {DetectedExePath}");
            
            var startInfo = new ProcessStartInfo
            {
                FileName = DetectedExePath,
                Arguments = $"--remote-debugging-port={config.DebugPort}",
                WorkingDirectory = Path.GetDirectoryName(DetectedExePath),
                UseShellExecute = true
            };
            
            try
            {
                var proc = Process.Start(startInfo);
                if (proc == null)
                {
                    Log("✗ 启动失败");
                    return false;
                }
                Log($"✓ 旺商聊已启动，进程ID: {proc.Id}");
            }
            catch (Exception ex)
            {
                Log($"✗ 启动失败: {ex.Message}");
                return false;
            }
            
            // 等待应用启动并尝试连接（等待60秒，给用户足够时间登录）
            Log("等待旺商聊启动... (请在60秒内完成登录)");
            for (int i = 0; i < 60; i++)
            {
                await Task.Delay(1000);
                
                if (await TryConnectToDebugPort(config.DebugPort))
                {
                    Log("✓ 调试端口已就绪");
                    return await ConnectToWebSocket();
                }
                
                if ((i + 1) % 10 == 0)
                {
                    Log($"等待中... ({i + 1}/60秒) - 请完成旺商聊登录");
                }
            }
            
            Log("✗ 连接超时");
            return false;
        }
        
        /// <summary>
        /// 尝试连接到调试端口（快速超时检测）
        /// </summary>
        private async Task<bool> TryConnectToDebugPort(int port)
        {
            var url = string.Format("http://127.0.0.1:{0}/json", port);
            
            try
            {
                // 使用 HttpWebRequest 设置短超时（2秒）
                var request = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(url);
                request.Timeout = 2000; // 2秒超时
                request.ReadWriteTimeout = 2000;
                request.Method = "GET";
                
                using (var response = await Task.Run(() => 
                {
                    try { return request.GetResponse(); }
                    catch { return null; }
                }))
                {
                    if (response == null) return false;
                    
                    using (var stream = response.GetResponseStream())
                    using (var reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        var content = reader.ReadToEnd();
                        
                        // 解析 webSocketDebuggerUrl
                        var match = System.Text.RegularExpressions.Regex.Match(
                            content, @"""webSocketDebuggerUrl"":\s*""([^""]+)""");
                        
                        if (match.Success)
                        {
                            WebSocketUrl = match.Groups[1].Value;
                            Log(string.Format("✓ 端口 {0} 可用, WebSocket: {1}", port, WebSocketUrl));
                            return true;
                        }
                    }
                }
            }
            catch
            {
                // 端口不可用，静默失败
            }
            return false;
        }
        
        /// <summary>
        /// 连接到WebSocket
        /// </summary>
        private async Task<bool> ConnectToWebSocket()
        {
            try
            {
                Log(string.Format("WebSocket URL: {0}", WebSocketUrl));
                
                // 如果已有连接，先关闭
                if (_webSocket != null)
                {
                    try
                    {
                        _cts?.Cancel();
                        if (_webSocket.State == WebSocketState.Open)
                        {
                            await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, 
                                "Reconnecting", CancellationToken.None);
                        }
                        _webSocket.Dispose();
                    }
                    catch { }
                }
                
                _webSocket = new ClientWebSocket();
                _cts = new CancellationTokenSource();
                
                await _webSocket.ConnectAsync(new Uri(WebSocketUrl), _cts.Token);
                
                Log("WebSocket 已连接");
                
                // 设置连接状态
                IsConnected = true;
                Mode = ConnectionMode.CDP;
                OnConnectionChanged?.Invoke(true);
                
                // 启动消息接收循环（在后台）
                _ = Task.Run(() => ReceiveLoopAsync());
                
                // 等待接收循环启动
                await Task.Delay(100);
                
                Log("✓ CDP 连接成功！");
                return true;
            }
            catch (Exception ex)
            {
                Log(string.Format("WebSocket连接失败: {0}", ex.Message));
                IsConnected = false;
                OnConnectionChanged?.Invoke(false);
                return false;
            }
        }
        
        /// <summary>
        /// 检测旺商聊是否已登录（使用底层WebSocket直接执行）
        /// </summary>
        private async Task<bool> CheckLoginStatusAsync()
        {
            try
            {
                if (_webSocket == null || _webSocket.State != WebSocketState.Open)
                {
                    return false;
                }
                
                // 检查页面是否包含登录界面的特征
                var script = @"
(function() {
    var bodyText = document.body.innerText || '';
    
    // 登录页面特征
    var loginKeywords = ['欢迎使用', '密码登录', '短信登录', '验证码', '安全登录', '注册账号', '获取验证码'];
    
    // 检查是否在登录页面
    for (var i = 0; i < loginKeywords.length; i++) {
        if (bodyText.indexOf(loginKeywords[i]) !== -1) {
            return 'NOT_LOGGED_IN';
        }
    }
    
    // 检查是否有联系人列表或聊天界面
    var chatElements = document.querySelectorAll('[class*=""session""], [class*=""contact-list""]');
    if (chatElements.length > 0) {
        return 'LOGGED_IN';
    }
    
    // 检查是否有搜索框（主界面特征）
    var searchBox = document.querySelector('input[placeholder*=""搜索""]');
    if (searchBox) {
        return 'LOGGED_IN';
    }
    
    // 检查页面是否有对话列表
    if (bodyText.indexOf('请选择一个对话开始聊天') !== -1) {
        return 'LOGGED_IN';
    }
    
    return 'UNKNOWN';
})();";

                // 直接执行脚本，不通过 ExecuteScriptWithResultAsync（它会检查 IsConnected）
                var result = await ExecuteScriptDirectAsync(script);
                
                if (!string.IsNullOrEmpty(result))
                {
                    Log(string.Format("[DEBUG] 登录检测结果: {0}", result));
                    if (result.Contains("LOGGED_IN"))
                    {
                        return true;
                    }
                    if (result.Contains("NOT_LOGGED_IN"))
                    {
                        return false;
                    }
                }
                
                // 无法确定，默认未登录
                return false;
            }
            catch (Exception ex)
            {
                Log(string.Format("[DEBUG] 登录检测异常: {0}", ex.Message));
                return false;
            }
        }
        
        /// <summary>
        /// 直接执行脚本（不检查 IsConnected 状态，用于登录检测）
        /// </summary>
        private async Task<string> ExecuteScriptDirectAsync(string script)
        {
            if (_webSocket == null || _webSocket.State != WebSocketState.Open)
            {
                return null;
            }
            
            var id = ++_messageId;
            var escapedScript = EscapeJson(script);
            var message = string.Format("{{\"id\":{0},\"method\":\"Runtime.evaluate\",\"params\":{{\"expression\":\"{1}\",\"returnByValue\":true}}}}", id, escapedScript);
            
            try
            {
                // 发送命令
                var bytes = Encoding.UTF8.GetBytes(message);
                await _webSocket.SendAsync(new ArraySegment<byte>(bytes), 
                    WebSocketMessageType.Text, true, CancellationToken.None);
                
                // 等待响应（简单等待）
                var buffer = new byte[65536];
                var result = new StringBuilder();
                
                using (var cts = new CancellationTokenSource(5000)) // 5秒超时
                {
                    try
                    {
                        var receiveResult = await _webSocket.ReceiveAsync(
                            new ArraySegment<byte>(buffer), cts.Token);
                        
                        if (receiveResult.MessageType == WebSocketMessageType.Text)
                        {
                            result.Append(Encoding.UTF8.GetString(buffer, 0, receiveResult.Count));
                            
                            // 提取返回值
                            var match = System.Text.RegularExpressions.Regex.Match(
                                result.ToString(), @"""value""\s*:\s*""([^""]+)""");
                            if (match.Success)
                            {
                                return match.Groups[1].Value;
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        return null;
                    }
                }
            }
            catch
            {
                return null;
            }
            
            return null;
        }
        
        /// <summary>
        /// 连接到旺商聊（需要先手动启动）
        /// </summary>
        public async Task<bool> ConnectAsync()
        {
            var config = ConfigService.Instance.Config;
            
            if (!await TryConnectToDebugPort(config.DebugPort))
            {
                Log("无法连接到调试端口，请确保旺商聊以调试模式运行");
                return false;
            }
            
            return await ConnectToWebSocket();
        }
        
        /// <summary>
        /// 断开连接
        /// </summary>
        public async Task DisconnectAsync()
        {
            try
            {
                _cts?.Cancel();
                if (_webSocket != null && _webSocket.State == WebSocketState.Open)
                {
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, 
                        "Closing", CancellationToken.None);
                }
                IsConnected = false;
                OnConnectionChanged?.Invoke(false);
                Log("已断开连接");
            }
            catch (Exception ex)
            {
                Log($"断开连接失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 发送消息（基于CDP测试验证的版本）
        /// </summary>
        public async Task<bool> SendMessageAsync(string content)
        {
            if (!IsConnected) return false;
            
            try
            {
                // 转义特殊字符
                var escaped = content.Replace("\\", "\\\\")
                                    .Replace("\"", "\\\"")
                                    .Replace("'", "\\'")
                                    .Replace("\n", "\\n")
                                    .Replace("\r", "");
                
                // Script verified by CDP command-line test
                var script = $@"
(function() {{
    var result = {{ success: false, debug: [] }};
    
    // Find input box - 旺商聊使用 contenteditable div
        var inputBox = null;
    var allInputs = document.querySelectorAll('textarea, div[contenteditable=""true""], .ql-editor');
    result.debug.push('Total inputs: ' + allInputs.length);
        
        for (var i = 0; i < allInputs.length; i++) {{
        var el = allInputs[i];
        var rect = el.getBoundingClientRect();
        if (rect.width > 100 && rect.height > 20) {{
            inputBox = el;
            result.debug.push('Using: ' + el.tagName + ' ' + (el.className || '').substring(0,30));
                break;
            }}
        }}
        
        if (!inputBox) {{
        result.error = 'No input box found';
        return JSON.stringify(result);
        }}
        
    // Set message content
        inputBox.focus();
    
    if (inputBox.tagName === 'TEXTAREA') {{
            inputBox.value = '{escaped}';
        }} else {{
            inputBox.innerText = '{escaped}';
    }}
            inputBox.dispatchEvent(new Event('input', {{ bubbles: true }}));
    result.debug.push('Message set');
        
    // Find and click send button - 旺商聊有发送按钮
    var btns = document.querySelectorAll('button');
    result.debug.push('Buttons: ' + btns.length);
    
    for (var b = 0; b < btns.length; b++) {{
        var btn = btns[b];
        if (btn.innerText && btn.innerText.includes('发送')) {{
            result.debug.push('Clicking send button');
            btn.click();
            result.success = true;
            return JSON.stringify(result);
        }}
        }}
        
    // Fallback: use Enter key
    result.debug.push('Using Enter key');
    inputBox.dispatchEvent(new KeyboardEvent('keydown', {{ key: 'Enter', keyCode: 13, bubbles: true }}));
    result.success = true;
    
    return JSON.stringify(result);
}})();";
                
                var result = await ExecuteScriptWithResultAsync(script);
                Log($"发送消息结果: {result}");
                
                // Parse JSON result from script
                // Result format: {"success":true/false,"debug":[...],"error":"..."}
                var success = !string.IsNullOrEmpty(result) && 
                              result.Contains("\"success\":true");
                
                // Log debug info
                var debugMatch = System.Text.RegularExpressions.Regex.Match(
                    result ?? "", @"""debug"":\s*\[(.*?)\]");
                if (debugMatch.Success)
                {
                    Log($"发送调试信息: {debugMatch.Groups[1].Value}");
                }
                
                if (success)
                {
                    Log("✓ 消息发送成功！");
                    // 记录发送日志
                    var chatAccount = await GetCurrentChatAccountAsync();
                    DataService.Instance.LogSentMessage(
                        chatAccount ?? "unknown",
                        chatAccount ?? "当前聊天",
                        content,
                        "text"
                    );
                }
                else
                {
                    // Log error if any
                    var errorMatch = System.Text.RegularExpressions.Regex.Match(
                        result ?? "", @"""error""\s*:\s*""([^""]+)""");
                    if (errorMatch.Success)
                    {
                        Log($"发送失败原因: {errorMatch.Groups[1].Value}");
                    }
                }
                
                return success;
            }
            catch (Exception ex)
            {
                Log($"发送消息失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Send text via NIM SDK (preferred, stable; supports both p2p/team)
        /// 通过 window.nim.sendText 发送文本（推荐：私聊/群聊通用）
        /// </summary>
        /// <param name="scene">"p2p" or "team"</param>
        /// <param name="to">target id; for team: groupCloudId (teamId); for p2p: peer account</param>
        /// <param name="text">message content</param>
        /// <returns>(success, scene, to, message)</returns>
        public async Task<(bool Success, string Scene, string To, string Message)> SendTextAsync(string scene, string to, string text)
        {
            if (!IsConnected || Mode != ConnectionMode.CDP)
            {
                Log("未连接或非CDP模式，无法通过NIM发送消息");
                return (false, scene, to, "未连接或非CDP模式");
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                return (false, scene, to, "消息内容为空");
            }

            try
            {
                var sceneJson = ToJsonString(scene);
                var toJson = ToJsonString(to);
                var textJson = ToJsonString(text);

                var script = $@"
(async function() {{
    var result = {{ success: false, scene: null, to: null, message: '', error: null, code: null }};
    try {{
        if (!window.nim || typeof window.nim.sendText !== 'function') {{
            result.error = 'window.nim.sendText not available';
            result.message = result.error;
            return JSON.stringify(result);
        }}

        var payload = {{ scene: {sceneJson}, to: {toJson}, text: {textJson} }};
        result.scene = payload.scene;
        result.to = payload.to;

        if (!payload.scene || !payload.to) {{
            result.error = 'Missing scene/to';
            result.message = result.error;
            return JSON.stringify(result);
        }}

        var apiResult = await new Promise(function(resolve) {{
            window.nim.sendText({{
                scene: String(payload.scene),
                to: String(payload.to),
                text: String(payload.text || ''),
                done: function(err, msg) {{
                    if (err) resolve({{ success: false, error: err.message || String(err), code: err.code || null }});
                    else resolve({{ success: true, msg: msg || null }});
                }}
            }});
            setTimeout(function() {{ resolve({{ success: false, error: 'Timeout' }}); }}, 8000);
        }});

        if (apiResult.success) {{
            result.success = true;
            result.message = 'Sent';
        }} else {{
            result.error = apiResult.error;
            result.code = apiResult.code;
            result.message = apiResult.error;
        }}
    }} catch(e) {{
        result.error = e.message;
        result.message = 'Exception: ' + e.message;
    }}
    return JSON.stringify(result);
}})();";

                var response = await ExecuteScriptWithResultAsync(script, true);
                Log($"NIM发送结果: {response}");

                var success = !string.IsNullOrEmpty(response) && response.Contains("\"success\":true");
                var respScene = ExtractJsonField(response, "scene") ?? scene;
                var respTo = ExtractJsonField(response, "to") ?? to;
                var message = ExtractJsonField(response, "message") ?? response;
                var error = ExtractJsonField(response, "error");

                if (success)
                {
                    var chatAccount = await GetCurrentChatAccountAsync();
                    DataService.Instance.LogSentMessage(
                        chatAccount ?? "unknown",
                        $"{respScene}:{respTo}",
                        text,
                        "text"
                    );
                    return (true, respScene, respTo, message);
                }

                return (false, respScene, respTo, !string.IsNullOrEmpty(error) ? error : message);
            }
            catch (Exception ex)
            {
                Log($"NIM发送异常: {ex.Message}");
                return (false, scene, to, ex.Message);
            }
        }

        // =====================================================================
        // NIM SDK MESSAGE HOOK INJECTION - 消息钩子注入
        // =====================================================================
        
        /// <summary>
        /// Install message hook into NIM SDK to capture incoming messages
        /// 安装消息钩子到 NIM SDK 以捕获接收的消息
        /// </summary>
        /// <returns>(success, message, hookedEvents count)</returns>
        public async Task<(bool Success, string Message, int HookedEventsCount)> InstallMessageHookAsync()
        {
            if (!IsConnected || Mode != ConnectionMode.CDP)
            {
                return (false, "未连接或非CDP模式", 0);
            }

            try
            {
                var script = @"
(function() {
    var result = { installed: false, message: '', hookedEvents: 0 };
    
    try {
        if (!window.nim) {
            result.message = 'NIM SDK not found';
            return JSON.stringify(result);
        }
        
        // Initialize message storage
        window.__botReceivedMessages = window.__botReceivedMessages || [];
        window.__botMessageCallback = window.__botMessageCallback || null;
        
        // Hook nim.options.onmsg for receiving all messages
        var origOnmsg = window.nim.options ? window.nim.options.onmsg : null;
        
        if (window.nim.options) {
            window.nim.options.onmsg = function(msg) {
                // Store message with content field for custom message decoding
                var msgData = {
                    time: Date.now(),
                    scene: msg.scene,
                    from: msg.from,
                    to: msg.to,
                    type: msg.type,
                    text: msg.text || '',
                    // Flatten nick fields to avoid nested JSON parsing in C# (no external JSON deps)
                    fromNick: (msg.user && (msg.user.groupMemberNick || msg.user.userNick)) || msg.fromNick || '',
                    flow: msg.flow || '',
                    idClient: msg.idClient || '',
                    // Add content field for custom message decoding (competitor bots use this)
                    content: msg.content ? JSON.stringify(msg.content) : ''
                };
                
                window.__botReceivedMessages.push(msgData);
                
                // Keep only last 100 messages
                if (window.__botReceivedMessages.length > 100) {
                    window.__botReceivedMessages.shift();
                }
                
                // Call original handler
                if (origOnmsg) origOnmsg(msg);
            };
            result.hookedEvents++;
        }
        
        // Also hook onmsgs for batch messages
        var origOnmsgs = window.nim.options ? window.nim.options.onmsgs : null;
        if (window.nim.options) {
            window.nim.options.onmsgs = function(msgs) {
                for (var i = 0; i < msgs.length; i++) {
                    var msg = msgs[i];
                    var msgData = {
                        time: Date.now(),
                        scene: msg.scene,
                        from: msg.from,
                        to: msg.to,
                        type: msg.type,
                        text: msg.text || '',
                        fromNick: (msg.user && (msg.user.groupMemberNick || msg.user.userNick)) || msg.fromNick || '',
                        flow: msg.flow || '',
                        idClient: msg.idClient || '',
                        // Add content field for custom message decoding
                        content: msg.content ? JSON.stringify(msg.content) : ''
                    };
                    window.__botReceivedMessages.push(msgData);
                }
                if (window.__botReceivedMessages.length > 100) {
                    window.__botReceivedMessages = window.__botReceivedMessages.slice(-100);
                }
                if (origOnmsgs) origOnmsgs(msgs);
            };
            result.hookedEvents++;
        }
        
        result.installed = true;
        result.message = 'Message hook installed successfully';
        
    } catch(e) {
        result.message = 'Error: ' + e.message;
    }
    
    return JSON.stringify(result);
})();";

                var response = await ExecuteScriptWithResultAsync(script, false);
                Log($"消息钩子安装结果: {response}");

                var success = !string.IsNullOrEmpty(response) && response.Contains("\"installed\":true");
                var message = ExtractJsonField(response, "message") ?? response;
                var hookedEvents = 0;
                var eventsMatch = System.Text.RegularExpressions.Regex.Match(response ?? "", @"""hookedEvents""\s*:\s*(\d+)");
                if (eventsMatch.Success) int.TryParse(eventsMatch.Groups[1].Value, out hookedEvents);

                return (success, message, hookedEvents);
            }
            catch (Exception ex)
            {
                Log($"安装消息钩子异常: {ex.Message}");
                return (false, ex.Message, 0);
            }
        }

        /// <summary>
        /// Get messages captured by the installed hook
        /// 获取钩子捕获的消息
        /// </summary>
        /// <param name="clearAfterGet">Clear messages after getting them</param>
        /// <returns>List of captured messages as JSON string</returns>
        public async Task<string> GetHookedMessagesAsync(bool clearAfterGet = true)
        {
            if (!IsConnected || Mode != ConnectionMode.CDP)
            {
                return "[]";
            }

            try
            {
                var clearFlag = clearAfterGet ? "true" : "false";
                var script = $@"
(function() {{
    var msgs = window.__botReceivedMessages || [];
    if ({clearFlag}) {{
        window.__botReceivedMessages = [];
    }}
    return JSON.stringify(msgs);
}})();";

                var response = await ExecuteScriptWithResultAsync(script, false);
                return response ?? "[]";
            }
            catch (Exception ex)
            {
                Log($"获取钩子消息异常: {ex.Message}");
                return "[]";
            }
        }

        // =====================================================================
        // DOM MESSAGE EXTRACTION - Get decoded messages from UI (competitor method)
        // =====================================================================

        /// <summary>
        /// Get decoded messages directly from DOM elements
        /// This is the competitor's method - messages are already decoded by the client
        /// Returns list of message text content from UI
        /// </summary>
        public async Task<List<DomMessage>> GetDomMessagesAsync(int maxCount = 50)
        {
            var result = new List<DomMessage>();
            
            if (!IsConnected || Mode != ConnectionMode.CDP)
            {
                return result;
            }

            try
            {
                var script = $@"
(function() {{
    var messages = [];
    
    // Find message elements in DOM
    var msgElements = document.querySelectorAll('[class*=""message""], [class*=""msg-item""], [class*=""chat-item""]');
    
    msgElements.forEach(function(el, idx) {{
        if (idx >= {maxCount}) return;
        
        var text = el.innerText || el.textContent;
        if (text && text.length > 3 && text.length < 5000) {{
            // Extract timestamp if present
            var timeMatch = text.match(/(\d{{1,2}}:\d{{2}}:\d{{2}})/);
            var time = timeMatch ? timeMatch[1] : null;
            
            // Clean text
            var cleanText = text.replace(/\s+/g, ' ').trim();
            
            // Check if from bot (contains 管理员 or 天谕机器)
            var isBot = cleanText.includes('管理员') || 
                       cleanText.includes('天谕机器') ||
                       cleanText.includes('客服');
            
            messages.push({{
                text: cleanText.substring(0, 2000),
                time: time,
                isBot: isBot,
                index: idx
            }});
        }}
    }});
    
    return JSON.stringify(messages);
}})();";

                var response = await ExecuteScriptWithResultAsync(script, false);
                
                if (string.IsNullOrEmpty(response) || !response.StartsWith("["))
                    return result;
                
                // Parse JSON response using regex
                var matches = System.Text.RegularExpressions.Regex.Matches(
                    response,
                    @"\{""text""\s*:\s*""((?:[^""\\]|\\.)*)"".*?""time""\s*:\s*(""[^""]*""|null).*?""isBot""\s*:\s*(true|false).*?""index""\s*:\s*(\d+)\}");
                
                foreach (System.Text.RegularExpressions.Match m in matches)
                {
                    var text = System.Text.RegularExpressions.Regex.Unescape(m.Groups[1].Value ?? "");
                    var timeStr = m.Groups[2].Value?.Trim('"');
                    var isBot = m.Groups[3].Value == "true";
                    var index = int.Parse(m.Groups[4].Value);
                    
                    // Classify message type using MessageDecoder
                    var msgType = MessageDecoder.ClassifyMessage(text);
                    var features = MessageDecoder.AnalyzeMessage(text, isBot ? "管理员" : "", "custom");
                    
                    result.Add(new DomMessage
                    {
                        Text = text,
                        Time = timeStr,
                        IsBot = isBot,
                        Index = index,
                        MessageType = msgType,
                        Tags = features.GetTagsString()
                    });
                }
                
                return result;
            }
            catch (Exception ex)
            {
                Log($"获取DOM消息异常: {ex.Message}");
                return result;
            }
        }

        /// <summary>
        /// DOM message data class
        /// </summary>
        public class DomMessage
        {
            public string Text { get; set; }
            public string Time { get; set; }
            public bool IsBot { get; set; }
            public int Index { get; set; }
            public CompetitorMessageType MessageType { get; set; }
            public string Tags { get; set; }
        }

        // =====================================================================
        // HOOKED MESSAGE POLLING - poll messages from NIM hook (recommended)
        // =====================================================================

        /// <summary>
        /// Start polling messages captured by NIM hook.
        /// This provides scene/to/from so we can build real group bet ledgers.
        /// </summary>
        public void StartHookedMessagePolling(int intervalMs = 1000)
        {
            if (IsPollingHookedMessages) return;

            if (!IsConnected || Mode != ConnectionMode.CDP)
            {
                Log("未连接或非CDP模式，无法启动 Hook 消息轮询");
                return;
            }

            _hookedMessagePollingTimer = new System.Timers.Timer(intervalMs);
            _hookedMessagePollingTimer.Elapsed += async (s, e) => await PollHookedMessagesAsync();
            _hookedMessagePollingTimer.AutoReset = true;
            _hookedMessagePollingTimer.Start();

            IsPollingHookedMessages = true;
            _processedHookedMessageHashes.Clear();
            Log($"Hook消息轮询已启动，间隔 {intervalMs}ms");
        }

        /// <summary>
        /// Stop polling messages captured by NIM hook.
        /// </summary>
        public void StopHookedMessagePolling()
        {
            if (!IsPollingHookedMessages) return;

            _hookedMessagePollingTimer?.Stop();
            _hookedMessagePollingTimer?.Dispose();
            _hookedMessagePollingTimer = null;
            IsPollingHookedMessages = false;
            Log("Hook消息轮询已停止");
        }

        // =====================================================================
        // INDEXEDDB DIRECT READ - Most reliable message capture method
        // Based on competitor bot analysis - directly reads NIM SDK local database
        // =====================================================================
        
        /// <summary>IndexedDB 轮询定时器</summary>
        private System.Timers.Timer _indexedDbPollingTimer;
        
        /// <summary>是否正在轮询 IndexedDB</summary>
        public bool IsPollingIndexedDb { get; private set; }
        
        /// <summary>IndexedDB 已处理消息哈希</summary>
        private HashSet<string> _processedIndexedDbHashes = new HashSet<string>();
        
        /// <summary>
        /// Initialize IndexedDB for message reading (get database name)
        /// 初始化 IndexedDB 消息读取（竞品机器人技术）
        /// </summary>
        public async Task<(bool Success, string DbName)> InitializeIndexedDbAsync()
        {
            if (!IsConnected || Mode != ConnectionMode.CDP)
                return (false, null);
            
            try
            {
                var script = @"
(function() {
    return new Promise(function(resolve) {
        indexedDB.databases().then(function(dbs) {
            var nimDb = dbs.find(function(db) { 
                return db.name && db.name.indexOf('nim-') === 0; 
            });
            if (nimDb) {
                window.__nimDbName = nimDb.name;
                window.__lastIndexedDbTime = 0;
                resolve(JSON.stringify({ success: true, dbName: nimDb.name }));
            } else {
                resolve(JSON.stringify({ success: false, error: 'NIM database not found' }));
            }
        }).catch(function(e) {
            resolve(JSON.stringify({ success: false, error: e.message }));
        });
    });
})()";
                
                var response = await ExecuteScriptWithResultAsync(script, true);
                
                var success = !string.IsNullOrEmpty(response) && response.Contains("\"success\":true");
                var dbName = ExtractJsonField(response, "dbName");
                
                if (success)
                    Log($"IndexedDB 初始化成功: {dbName}");
                else
                    Log($"IndexedDB 初始化失败: {response}");
                
                return (success, dbName);
            }
            catch (Exception ex)
            {
                Log($"IndexedDB 初始化异常: {ex.Message}");
                return (false, null);
            }
        }
        
        /// <summary>
        /// Start polling messages from IndexedDB (most reliable method)
        /// 启动 IndexedDB 消息轮询（最可靠的方式，基于竞品机器人分析）
        /// </summary>
        public async Task StartIndexedDbPollingAsync(string targetTeamId = null, int intervalMs = 1000)
        {
            if (IsPollingIndexedDb) return;
            
            if (!IsConnected || Mode != ConnectionMode.CDP)
            {
                Log("未连接或非CDP模式，无法启动 IndexedDB 轮询");
                return;
            }
            
            // Initialize first
            var (success, dbName) = await InitializeIndexedDbAsync();
            if (!success)
            {
                Log("IndexedDB 初始化失败，无法启动轮询");
                return;
            }
            
            _indexedDbPollingTimer = new System.Timers.Timer(intervalMs);
            _indexedDbPollingTimer.Elapsed += async (s, e) => await PollIndexedDbMessagesAsync(targetTeamId);
            _indexedDbPollingTimer.AutoReset = true;
            _indexedDbPollingTimer.Start();
            
            IsPollingIndexedDb = true;
            _processedIndexedDbHashes.Clear();
            Log($"IndexedDB 消息轮询已启动，数据库: {dbName}，间隔: {intervalMs}ms");
        }
        
        /// <summary>
        /// Stop polling messages from IndexedDB
        /// </summary>
        public void StopIndexedDbPolling()
        {
            if (!IsPollingIndexedDb) return;
            
            _indexedDbPollingTimer?.Stop();
            _indexedDbPollingTimer?.Dispose();
            _indexedDbPollingTimer = null;
            IsPollingIndexedDb = false;
            Log("IndexedDB 消息轮询已停止");
        }
        
        /// <summary>
        /// Poll messages directly from IndexedDB
        /// This is the most reliable method based on competitor bot analysis
        /// </summary>
        private async Task PollIndexedDbMessagesAsync(string targetTeamId)
        {
            if (!IsConnected || Mode != ConnectionMode.CDP) return;
            
            try
            {
                var targetFilter = string.IsNullOrEmpty(targetTeamId) 
                    ? "" 
                    : $"if (msg.to !== '{targetTeamId}' && msg.target !== '{targetTeamId}') {{ c.continue(); return; }}";
                
                var script = $@"
(function() {{
    return new Promise(function(resolve) {{
        var lastTime = window.__lastIndexedDbTime || 0;
        var result = {{ msgs: [], error: null }};
        
        if (!window.__nimDbName) {{
            result.error = 'DB not initialized';
            resolve(JSON.stringify(result));
            return;
        }}
        
        var request = indexedDB.open(window.__nimDbName);
        request.onsuccess = function(event) {{
            var db = event.target.result;
            try {{
                var tx = db.transaction('msg1', 'readonly');
                var store = tx.objectStore('msg1');
                var index = store.index('time');
                var range = lastTime > 0 ? IDBKeyRange.lowerBound(lastTime, true) : null;
                var cursor = index.openCursor(range, 'next');
                var msgs = [];
                var maxTime = lastTime;
                
                cursor.onsuccess = function(e) {{
                    var c = e.target.result;
                    if (c) {{
                        var msg = c.value;
                        if (msg.time > maxTime) maxTime = msg.time;
                        
                        {targetFilter}
                        
                        var textContent = '';
                        if (msg.content) {{
                            try {{
                                var contentObj = typeof msg.content === 'string' ? 
                                    JSON.parse(msg.content) : msg.content;
                                if (contentObj.b) {{
                                    var b = contentObj.b.replace(/-/g, '+').replace(/_/g, '/');
                                    while (b.length % 4) b += '=';
                                    var decoded = atob(b);
                                    var bytes = new Uint8Array(decoded.length);
                                    for (var i = 0; i < decoded.length; i++) bytes[i] = decoded.charCodeAt(i);
                                    var text = new TextDecoder('utf-8', {{fatal: false}}).decode(bytes);
                                    var chineseMatch = text.match(/[\u4e00-\u9fff]+/g);
                                    if (chineseMatch) textContent = chineseMatch.join(' ');
                                }}
                            }} catch(e) {{}}
                        }}
                        if (!textContent && msg.text) textContent = msg.text;
                        
                        msgs.push({{
                            time: msg.time,
                            from: msg.from,
                            to: msg.to,
                            type: msg.type,
                            text: textContent.substring(0, 500),
                            fromNick: msg.fromNick || '',
                            flow: msg.flow || '',
                            idClient: msg.idClient || '',
                            content: msg.content ? JSON.stringify(msg.content).substring(0, 200) : ''
                        }});
                        
                        c.continue();
                    }} else {{
                        window.__lastIndexedDbTime = maxTime;
                        result.msgs = msgs;
                        db.close();
                        resolve(JSON.stringify(result));
                    }}
                }};
                
                cursor.onerror = function() {{
                    result.error = 'Cursor error';
                    db.close();
                    resolve(JSON.stringify(result));
                }};
            }} catch(e) {{
                result.error = e.message;
                db.close();
                resolve(JSON.stringify(result));
            }}
        }};
        
        request.onerror = function() {{
            result.error = 'DB open error';
            resolve(JSON.stringify(result));
        }};
    }});
}})()";
                
                var response = await ExecuteScriptWithResultAsync(script, true);
                if (string.IsNullOrEmpty(response)) return;
                
                // Parse messages using regex
                var msgMatches = System.Text.RegularExpressions.Regex.Matches(
                    response,
                    @"\{[^{}]*""time""\s*:\s*(\d+)[^{}]*""from""\s*:\s*""([^""]*)""\s*[^{}]*""to""\s*:\s*""([^""]*)""\s*[^{}]*""type""\s*:\s*""([^""]*)""\s*[^{}]*""text""\s*:\s*""((?:[^""\\]|\\.)*)""[^{}]*""fromNick""\s*:\s*""((?:[^""\\]|\\.)*)""[^{}]*""flow""\s*:\s*""([^""]*)""\s*[^{}]*""idClient""\s*:\s*""([^""]*)""\s*[^{}]*""content""\s*:\s*""((?:[^""\\]|\\.)*)""\s*[^{}]*\}",
                    System.Text.RegularExpressions.RegexOptions.Singleline);
                
                foreach (System.Text.RegularExpressions.Match m in msgMatches)
                {
                    var timeMs = m.Groups[1].Value;
                    var from = m.Groups[2].Value;
                    var to = m.Groups[3].Value;
                    var type = m.Groups[4].Value;
                    var text = System.Text.RegularExpressions.Regex.Unescape(m.Groups[5].Value ?? "");
                    var fromNick = System.Text.RegularExpressions.Regex.Unescape(m.Groups[6].Value ?? "");
                    var flow = m.Groups[7].Value ?? "";
                    var idClient = m.Groups[8].Value ?? "";
                    var contentJson = System.Text.RegularExpressions.Regex.Unescape(m.Groups[9].Value ?? "");
                    
                    // Deduplication
                    var hash = $"{timeMs}|{from}|{to}|{idClient}";
                    if (_processedIndexedDbHashes.Contains(hash)) continue;
                    _processedIndexedDbHashes.Add(hash);
                    if (_processedIndexedDbHashes.Count > 5000) _processedIndexedDbHashes.Clear();
                    
                    long.TryParse(timeMs, out var ms);
                    var dt = ms > 0 ? DateTimeOffset.FromUnixTimeMilliseconds(ms).LocalDateTime : DateTime.Now;
                    
                    var isTeam = !string.IsNullOrEmpty(to) && to.Length > 10;
                    var isOut = string.Equals(flow, "out", StringComparison.OrdinalIgnoreCase);
                    var isCustom = string.Equals(type, "custom", StringComparison.OrdinalIgnoreCase);
                    
                    // Analyze features
                    var features = MessageDecoder.AnalyzeMessage(text, fromNick, type);
                    
                    var msgType = MessageType.Text;
                    if (isCustom) msgType = MessageType.Custom;
                    else if (string.Equals(type, "image", StringComparison.OrdinalIgnoreCase)) msgType = MessageType.Image;
                    
                    var chatMessage = new ChatMessage
                    {
                        Content = string.IsNullOrEmpty(text) && isCustom ? "[自定义消息]" : text,
                        SenderId = from,
                        SenderName = string.IsNullOrEmpty(fromNick) ? from : fromNick,
                        Time = dt,
                        IsSelf = isOut,
                        IsGroupMessage = isTeam,
                        GroupId = isTeam ? to : null,
                        Type = msgType,
                        IdClient = idClient,
                        IsBot = features.IsBot,
                        Tags = features.GetTagsString(),
                        RawContent = contentJson
                    };
                    
                    OnMessageReceived?.Invoke(chatMessage);
                }
            }
            catch (Exception ex)
            {
                Log($"IndexedDB 轮询异常: {ex.Message}");
            }
        }

        /// <summary>
        /// Poll messages from the injected NIM hook and raise OnMessageReceived.
        /// JSON parsing is done via regex to avoid external dependencies.
        /// Enhanced to decode custom messages (competitor bot format)
        /// </summary>
        private async Task PollHookedMessagesAsync()
        {
            if (!IsConnected || Mode != ConnectionMode.CDP) return;

            try
            {
                var json = await GetHookedMessagesAsync(clearAfterGet: true);
                if (string.IsNullOrEmpty(json) || !json.StartsWith("[")) return;

                // Match flattened msgData objects from InstallMessageHookAsync (with content field)
                var matches = System.Text.RegularExpressions.Regex.Matches(
                    json,
                    @"""time""\s*:\s*(\d+)[\s\S]*?""scene""\s*:\s*""([^""]*)""[\s\S]*?""from""\s*:\s*""([^""]*)""[\s\S]*?""to""\s*:\s*""([^""]*)""[\s\S]*?""type""\s*:\s*""([^""]*)""[\s\S]*?""text""\s*:\s*""((?:[^""\\]|\\.)*)""[\s\S]*?""fromNick""\s*:\s*""((?:[^""\\]|\\.)*)""[\s\S]*?""flow""\s*:\s*""([^""]*)""[\s\S]*?""idClient""\s*:\s*""([^""]*)""[\s\S]*?""content""\s*:\s*""((?:[^""\\]|\\.)*)""",
                    System.Text.RegularExpressions.RegexOptions.Singleline);

                foreach (System.Text.RegularExpressions.Match m in matches)
                {
                    var timeMs = m.Groups[1].Value;
                    var scene = m.Groups[2].Value;
                    var from = m.Groups[3].Value;
                    var to = m.Groups[4].Value;
                    var type = m.Groups[5].Value;
                    var text = System.Text.RegularExpressions.Regex.Unescape(m.Groups[6].Value ?? "");
                    var fromNick = System.Text.RegularExpressions.Regex.Unescape(m.Groups[7].Value ?? "");
                    var flow = m.Groups[8].Value ?? "";
                    var idClient = m.Groups[9].Value ?? "";
                    var contentJson = System.Text.RegularExpressions.Regex.Unescape(m.Groups[10].Value ?? "");

                    // Decode custom message content (competitor bot format)
                    var isCustom = string.Equals(type, "custom", StringComparison.OrdinalIgnoreCase);
                    if (isCustom && string.IsNullOrWhiteSpace(text) && !string.IsNullOrWhiteSpace(contentJson))
                    {
                        text = MessageDecoder.GetDisplayContent(text, contentJson, type);
                    }

                    // Skip empty messages but allow decoded custom messages
                    if (string.IsNullOrWhiteSpace(text) && !isCustom) continue;

                    // Enhanced hash with content for better deduplication
                    var hashContent = !string.IsNullOrEmpty(text) ? text.Substring(0, Math.Min(50, text.Length)) : contentJson.GetHashCode().ToString();
                    var hash = $"{timeMs}|{scene}|{from}|{to}|{hashContent}";
                    if (_processedHookedMessageHashes.Contains(hash)) continue;
                    _processedHookedMessageHashes.Add(hash);
                    if (_processedHookedMessageHashes.Count > 2000) _processedHookedMessageHashes.Clear();

                    long.TryParse(timeMs, out var ms);
                    var dt = ms > 0 ? DateTimeOffset.FromUnixTimeMilliseconds(ms).LocalDateTime : DateTime.Now;

                    var isTeam = string.Equals(scene, "team", StringComparison.OrdinalIgnoreCase);
                    var isOut = string.Equals(flow, "out", StringComparison.OrdinalIgnoreCase);

                    // Determine message type based on NIM type field
                    var msgType = MessageType.Text;
                    if (string.Equals(type, "image", StringComparison.OrdinalIgnoreCase))
                        msgType = MessageType.Image;
                    else if (isCustom || string.Equals(type, "emoji", StringComparison.OrdinalIgnoreCase))
                        msgType = MessageType.Custom;
                    else if (!string.Equals(type, "text", StringComparison.OrdinalIgnoreCase))
                        msgType = MessageType.System;

                    // Analyze message features for logging
                    var features = MessageDecoder.AnalyzeMessage(text, fromNick, type);

                    var chatMessage = new ChatMessage
                    {
                        Content = text,
                        SenderId = from,
                        SenderName = string.IsNullOrEmpty(fromNick) ? from : fromNick,
                        Time = dt,
                        IsSelf = isOut,
                        IsGroupMessage = isTeam,
                        GroupId = isTeam ? to : null,
                        Type = msgType,
                        IdClient = idClient,
                        // Enhanced fields for bot detection and logging
                        IsBot = features.IsBot,
                        Tags = features.GetTagsString(),
                        RawContent = contentJson
                    };

                    OnMessageReceived?.Invoke(chatMessage);
                }
            }
            catch (Exception ex)
            {
                Log($"Hook消息轮询异常: {ex.Message}");
            }
        }

        /// <summary>
        /// Send auto-reply message to team (group chat) via NIM SDK
        /// 通过 NIM SDK 发送自动回复消息到群聊
        /// </summary>
        /// <param name="teamId">Group teamId (groupCloudId)</param>
        /// <param name="text">Reply text</param>
        /// <returns>(success, message)</returns>
        public async Task<(bool Success, string Message)> SendAutoReplyAsync(string teamId, string text)
        {
            if (string.IsNullOrEmpty(teamId) || string.IsNullOrEmpty(text))
            {
                return (false, "teamId或text为空");
            }

            var result = await SendTextAsync("team", teamId, text);
            return (result.Success, result.Message);
        }

        /// <summary>
        /// Send text to current session using Pinia appStore.currentSession (CDP only)
        /// 发送到当前会话（自动从 Pinia currentSession 取 scene/to）
        /// </summary>
        public async Task<(bool Success, string Scene, string To, string Message)> SendTextToCurrentSessionAsync(string text)
        {
            if (!IsConnected || Mode != ConnectionMode.CDP)
            {
                return (false, null, null, "未连接或非CDP模式");
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                return (false, null, null, "消息内容为空");
            }

            try
            {
                var textJson = ToJsonString(text);
                var script = $@"
(async function() {{
    var result = {{ success: false, scene: null, to: null, message: '', error: null }};
    try {{
        if (!window.nim || typeof window.nim.sendText !== 'function') {{
            result.error = 'window.nim.sendText not available';
            result.message = result.error;
            return JSON.stringify(result);
        }}

        var app = document.querySelector('#app');
        var gp = app && app.__vue_app__ && app.__vue_app__.config && app.__vue_app__.config.globalProperties;
        var pinia = gp && gp.$pinia;
        var appStore = pinia && pinia._s && pinia._s.get && pinia._s.get('app');
        var s = appStore ? (appStore.currentSession || appStore.currSession) : null;

        var scene = s && (s.scene || s.sessionType || (s.group ? 'team' : null)) || null;
        var to = s && (s.to || (s.group && (s.group.groupCloudId || s.group.teamId)) || null) || null;

        result.scene = scene;
        result.to = to;
        if (!scene || !to) {{
            result.error = 'No current session (scene/to missing)';
            result.message = result.error;
            return JSON.stringify(result);
        }}

        var apiResult = await new Promise(function(resolve) {{
            window.nim.sendText({{
                scene: String(scene),
                to: String(to),
                text: String({textJson} || ''),
                done: function(err, msg) {{
                    if (err) resolve({{ success: false, error: err.message || String(err), code: err.code || null }});
                    else resolve({{ success: true, msg: msg || null }});
                }}
            }});
            setTimeout(function() {{ resolve({{ success: false, error: 'Timeout' }}); }}, 8000);
        }});

        if (apiResult.success) {{
            result.success = true;
            result.message = 'Sent';
        }} else {{
            result.error = apiResult.error;
            result.message = apiResult.error;
        }}
    }} catch(e) {{
        result.error = e.message;
        result.message = 'Exception: ' + e.message;
    }}
    return JSON.stringify(result);
}})();";

                var response = await ExecuteScriptWithResultAsync(script, true);
                Log($"NIM发送(当前会话)结果: {response}");

                var success = !string.IsNullOrEmpty(response) && response.Contains("\"success\":true");
                var respScene = ExtractJsonField(response, "scene");
                var respTo = ExtractJsonField(response, "to");
                var message = ExtractJsonField(response, "message") ?? response;
                var error = ExtractJsonField(response, "error");

                if (success)
                {
                    var chatAccount = await GetCurrentChatAccountAsync();
                    DataService.Instance.LogSentMessage(
                        chatAccount ?? "unknown",
                        $"{respScene}:{respTo}",
                        text,
                        "text"
                    );
                    return (true, respScene, respTo, message);
                }

                return (false, respScene, respTo, !string.IsNullOrEmpty(error) ? error : message);
            }
            catch (Exception ex)
            {
                Log($"NIM发送(当前会话)异常: {ex.Message}");
                return (false, null, null, ex.Message);
            }
        }
        
        /// <summary>
        /// 获取联系人/会话列表（高级 Pinia Store 注入）
        /// </summary>
        public async Task<List<ContactInfo>> GetContactListAsync()
        {
            var list = new List<ContactInfo>();
            if (!IsConnected)
            {
                Log("未连接，无法获取联系人列表");
                return list;
            }
            
            try
            {
                Log("正在通过 Pinia Store 获取联系人列表...");
                
                // High-level injection using Pinia store to get friends and groups
                var script = @"
(function() {
    var result = { friends: [], groups: [], debug: [] };
    
    try {
        var app = document.querySelector('#app');
        if (!app || !app.__vue_app__) {
            result.debug.push('No Vue app found');
            return JSON.stringify(result);
        }
        
        var pinia = app.__vue_app__.config.globalProperties.$pinia;
        if (!pinia) {
            result.debug.push('No Pinia found');
            return JSON.stringify(result);
        }
        
        // Get app store for friend and group lists
        var appStore = pinia._s.get('app');
        if (appStore) {
            // Get friends
            if (appStore.friendList && appStore.friendList.friendList) {
                var fl = appStore.friendList.friendList;
                if (Array.isArray(fl)) {
                    fl.forEach(function(f) {
                        result.friends.push({
                            name: f.nickName || '',
                            accountId: String(f.accountId || ''),
                            nimId: String(f.nimId || ''),
                            uid: f.uid,
                            avatar: f.avatar,
                            alias: f.alias || ''
                        });
                    });
                }
            }
            result.debug.push('Friends: ' + result.friends.length);
            
            // Get groups (owner + member)
            if (appStore.groupList) {
                var gl = appStore.groupList;
                if (gl.owner && Array.isArray(gl.owner)) {
                    gl.owner.forEach(function(g) {
                        result.groups.push({
                            name: g.groupName || '',
                            groupId: String(g.groupId || ''),
                            groupAccount: String(g.groupAccount || ''),
                            memberCount: g.groupMemberNum || g.memberCount,
                            role: 'owner'
                        });
                    });
                }
                if (gl.member && Array.isArray(gl.member)) {
                    gl.member.forEach(function(g) {
                        result.groups.push({
                            name: g.groupName || '',
                            groupId: String(g.groupId || ''),
                            groupAccount: String(g.groupAccount || ''),
                            memberCount: g.groupMemberNum || g.memberCount,
                            role: 'member'
                        });
                        });
                    }
                }
            result.debug.push('Groups: ' + result.groups.length);
            }
    } catch(e) {
        result.debug.push('Error: ' + e.message);
    }
    
    return JSON.stringify(result);
})();";
                
                var result = await ExecuteScriptWithResultAsync(script);
                Log($"联系人数据长度: {result?.Length ?? 0}");
                
                if (!string.IsNullOrEmpty(result))
                {
                    // Parse friends
                    var friendMatches = System.Text.RegularExpressions.Regex.Matches(
                        result,
                        @"""name""\s*:\s*""([^""]*)""\s*,\s*""accountId""\s*:\s*""(\d*)""\s*,\s*""nimId""\s*:\s*""(\d*)""");
                    
                    foreach (System.Text.RegularExpressions.Match match in friendMatches)
                    {
                        var contact = new ContactInfo
                        {
                            Name = System.Text.RegularExpressions.Regex.Unescape(match.Groups[1].Value),
                            WangShangId = match.Groups[2].Value,
                            NimId = match.Groups[3].Value,
                            Type = "friend"
                        };
                        if (!string.IsNullOrEmpty(contact.Name))
                    {
                            list.Add(contact);
                            Log($"  好友: {contact.Name} (旺商号:{contact.WangShangId})");
                        }
                    }
                    
                    // Parse groups - use groupAccount as the real group number
                    // groupAccount = real group number (e.g., 3962369093)
                    // groupId = internal system ID (e.g., 1176721)
                    var groupMatches = System.Text.RegularExpressions.Regex.Matches(
                        result, 
                        @"""name""\s*:\s*""([^""]*)""\s*,\s*""groupId""\s*:\s*""(\d+)""\s*,\s*""groupAccount""\s*:\s*""(\d+)""");
                    
                    foreach (System.Text.RegularExpressions.Match match in groupMatches)
                    {
                        var groupName = System.Text.RegularExpressions.Regex.Unescape(match.Groups[1].Value);
                        var groupInternalId = match.Groups[2].Value;
                        var groupAccount = match.Groups[3].Value;
                    
                        // Avoid duplicates
                        if (!list.Exists(c => c.Name == groupName && c.Type == "group"))
                        {
                            var contact = new ContactInfo
                            {
                                Name = groupName,
                                WangShangId = groupAccount,  // Use groupAccount as the real group number
                                NimId = groupInternalId,     // Store internal ID in NimId field
                                Type = "group"
                            };
                            if (!string.IsNullOrEmpty(contact.Name))
                            {
                                list.Add(contact);
                                Log($"  群组: {contact.Name} (群号:{contact.WangShangId}, 内部ID:{contact.NimId})");
                            }
                        }
                    }
                }
                
                Log($"共获取到 {list.Count} 个联系人/群组");
            }
            catch (Exception ex)
            {
                Log($"获取联系人失败: {ex.Message}");
            }
            
            return list;
        }
        
        /// <summary>
        /// 获取当前聊天对象的旺商号
        /// </summary>
        public async Task<string> GetCurrentChatAccountAsync()
        {
            if (!IsConnected) return null;
            
            try
            {
                // 从聊天窗口头部获取旺商号
                var script = @"
(function() {
    // 尝试从聊天头部获取
    var headerEl = document.querySelector('[class*=""chat-header""], [class*=""session-header""]');
    if (headerEl) {
        var idEl = headerEl.querySelector('[class*=""account""], [class*=""id""]');
        if (idEl) return idEl.innerText.trim();
    }
    
    // 尝试从URL获取
    var url = window.location.href;
    var match = url.match(/sessionId=p2p-(\d+)/);
    if (match) return match[1];
    
    // 尝试从Vue状态获取
    try {
        var app = document.querySelector('#app');
        if (app && app.__vue__) {
            var vm = app.__vue__;
            if (vm.$store && vm.$store.state.chat) {
                return vm.$store.state.chat.currentSessionId || '';
            }
        }
    } catch(e) {}
    
    return '';
})();";
                
                var result = await ExecuteScriptWithResultAsync(script);
                Log($"当前聊天账号: {result}");
                return result;
            }
            catch (Exception ex)
            {
                Log($"获取当前聊天账号失败: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 获取当前聊天窗口的所有消息内容
        /// </summary>
        public async Task<List<ChatMessage>> GetChatMessagesAsync()
        {
            var messages = new List<ChatMessage>();
            if (!IsConnected)
            {
                Log("未连接，无法获取消息");
                return messages;
            }
            
            try
            {
                Log("正在获取聊天消息...");
                
                // Script based on CDP test - 旺商聊使用 .msg-item 和 self-msg 类
                var script = @"
(function() {
    var messages = [];
    
    // 旺商聊消息元素使用 .msg-item 类，自己发送的有 .self-msg 类
    var msgItems = document.querySelectorAll('.msg-item, [class*=""message""]');
    
    msgItems.forEach(function(el, index) {
        var text = (el.innerText || '').trim();
        if (text && text.length > 0 && text.length < 2000) {
            var className = el.className || '';
            // 旺商聊自己发送的消息有 self-msg 类
            var isSent = className.includes('self-msg') || className.includes('self') || className.includes('right');
            
            // 提取消息文本（去除时间戳）
            var lines = text.split('\n');
            var msgText = lines[0] || text;
            var timeText = lines.length > 1 ? lines[lines.length - 1] : '';
            
            messages.push({
                text: msgText.substring(0, 500),
                sender: isSent ? '我' : '对方',
                time: timeText,
                isSent: isSent,
                index: index
            });
        }
    });
    
    return JSON.stringify(messages);
})();";
                
                var result = await ExecuteScriptWithResultAsync(script);
                Log($"获取消息结果长度: {result?.Length ?? 0}");
                
                if (!string.IsNullOrEmpty(result) && result.StartsWith("["))
                {
                    // Parse JSON using regex (simpler approach without external dependencies)
                    var msgMatches = System.Text.RegularExpressions.Regex.Matches(
                        result,
                        @"\{\s*""text""\s*:\s*""((?:[^""\\]|\\.)*)""\s*,\s*""sender""\s*:\s*""((?:[^""\\]|\\.)*)""\s*,\s*""time""\s*:\s*""([^""]*)""\s*,\s*""isSent""\s*:\s*(true|false)",
                        System.Text.RegularExpressions.RegexOptions.Singleline);
                    
                    foreach (System.Text.RegularExpressions.Match match in msgMatches)
                    {
                        var text = System.Text.RegularExpressions.Regex.Unescape(match.Groups[1].Value);
                        var sender = System.Text.RegularExpressions.Regex.Unescape(match.Groups[2].Value);
                        var time = match.Groups[3].Value;
                        var isSent = match.Groups[4].Value == "true";
                        
                        messages.Add(new ChatMessage
                        {
                            Content = text,
                            SenderName = sender,
                            Time = DateTime.Now,
                            IsSelf = isSent
                        });
                    }
                }
                
                Log($"解析到 {messages.Count} 条消息");
            }
            catch (Exception ex)
            {
                Log($"获取聊天消息失败: {ex.Message}");
            }
            
            return messages;
        }
        
        /// <summary>
        /// 获取当前登录用户的旺商号（我的账号）- 高级 Pinia Store 注入
        /// </summary>
        public async Task<string> GetMyAccountAsync()
        {
            if (!IsConnected) return null;
            
            try
            {
                Log("[DEBUG] ========== Getting My Account (Pinia Store) ==========");
                
                // Direct script that returns just the accountId number
                // accountId = 旺商号 (e.g., 82840376)
                // nimId = NIM SDK 内部 ID (e.g., 1391351554)
                var script = @"
(function() {
    try {
        var app = document.querySelector('#app');
        if (app && app.__vue_app__) {
            var pinia = app.__vue_app__.config.globalProperties.$pinia;
            if (pinia) {
                var appStore = pinia._s.get('app');
                if (appStore && appStore.userInfo && appStore.userInfo.accountId) {
                    // Return accountId directly as string
                    return String(appStore.userInfo.accountId);
                }
            }
        }
        return '';
    } catch(e) {
        return '';
    }
})()";
                
                var response = await ExecuteScriptWithResultAsync(script);
                Log($"[DEBUG] Raw response: {response}");
                
                if (!string.IsNullOrEmpty(response))
                {
                    // Response should be just the account number
                    // Clean up any quotes or whitespace
                    var cleaned = response.Trim().Trim('"').Trim();
                    
                    // Verify it's a valid account number (8-12 digits)
                    if (System.Text.RegularExpressions.Regex.IsMatch(cleaned, @"^\d{8,12}$"))
                    {
                        Log($"Got accountId: {cleaned}");
                        return cleaned;
                    }
                    
                    // If not clean, try to extract from JSON
                    var match = System.Text.RegularExpressions.Regex.Match(response, @"(\d{8,12})");
                    if (match.Success)
                    {
                        Log($"Extracted accountId: {match.Groups[1].Value}");
                        return match.Groups[1].Value;
                    }
                }
                
                Log("Failed to get accountId from Pinia");
                return null;
        }
            catch (Exception ex)
            {
                Log($"GetMyAccountAsync error: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 获取完整的账户信息（包括旺商号、昵称、群列表）- 用于账号列表功能
        /// </summary>
        public async Task<FullAccountInfo> GetFullAccountInfoAsync()
        {
            if (!IsConnected) return null;
            
            try
            {
                Log("[DEBUG] Getting full account info from Pinia Store...");
                
                var script = @"
(function() {
    var result = {
        uid: 0,
        accountId: '',
        nimId: '',
        nickName: '',
        phone: '',
        avatar: '',
        groups: []
    };
    
    try {
        var app = document.querySelector('#app');
        if (app && app.__vue_app__) {
            var pinia = app.__vue_app__.config.globalProperties.$pinia;
            if (pinia) {
                var appStore = pinia._s.get('app');
                
                // Get my account info
                if (appStore && appStore.userInfo) {
                    var ui = appStore.userInfo;
                    result.uid = ui.uid || 0;
                    result.accountId = String(ui.accountId || '');
                    result.nimId = String(ui.nimId || '');
                    result.nickName = ui.nickName || '';
                    result.avatar = ui.avatar || '';
                    
                    // Extract phone number
                    if (ui.phone && ui.phone.nationalNumber) {
                        result.phone = String(ui.phone.nationalNumber);
                    }
                }
                
                // Get group list
                // groupAccount = real group number (e.g., 3962369093)
                // groupId = internal system ID (e.g., 1176721)
                if (appStore && appStore.groupList) {
                    if (appStore.groupList.owner) {
                        appStore.groupList.owner.forEach(function(g) {
                            result.groups.push({
                                groupId: g.groupId,
                                groupAccount: String(g.groupAccount || ''),
                                groupName: g.groupName,
                                nimGroupId: g.nimGroupId || '',
                                role: 'owner',
                                memberCount: g.groupMemberNum || g.memberCount || 0
                            });
                        });
                    }
                    if (appStore.groupList.member) {
                        appStore.groupList.member.forEach(function(g) {
                            result.groups.push({
                                groupId: g.groupId,
                                groupAccount: String(g.groupAccount || ''),
                                groupName: g.groupName,
                                nimGroupId: g.nimGroupId || '',
                                role: 'member',
                                memberCount: g.groupMemberNum || g.memberCount || 0
                            });
                        });
                    }
                }
            }
        }
    } catch(e) {
        result.error = e.message;
    }
    
    return JSON.stringify(result);
})()";
                
                var response = await ExecuteScriptWithResultAsync(script);
                Log($"[DEBUG] GetFullAccountInfoAsync response length: {response?.Length ?? 0}");
                
                if (!string.IsNullOrEmpty(response))
                {
                    // Parse JSON using regex (avoid dependency on System.Web.Script)
                    var info = new FullAccountInfo();
                    
                    // Extract basic fields
                    var uidMatch = System.Text.RegularExpressions.Regex.Match(response, @"""uid""\s*:\s*(\d+)");
                    if (uidMatch.Success) info.Uid = long.Parse(uidMatch.Groups[1].Value);
                    
                    var accountIdMatch = System.Text.RegularExpressions.Regex.Match(response, @"""accountId""\s*:\s*""(\d+)""");
                    if (accountIdMatch.Success) info.AccountId = accountIdMatch.Groups[1].Value;
                    
                    var nimIdMatch = System.Text.RegularExpressions.Regex.Match(response, @"""nimId""\s*:\s*""(\d+)""");
                    if (nimIdMatch.Success) info.NimId = nimIdMatch.Groups[1].Value;
                    
                    var nickNameMatch = System.Text.RegularExpressions.Regex.Match(response, @"""nickName""\s*:\s*""([^""]+)""");
                    if (nickNameMatch.Success) info.NickName = System.Text.RegularExpressions.Regex.Unescape(nickNameMatch.Groups[1].Value);
                    
                    var phoneMatch = System.Text.RegularExpressions.Regex.Match(response, @"""phone""\s*:\s*""(\d+)""");
                    if (phoneMatch.Success) info.Phone = phoneMatch.Groups[1].Value;
                    
                    var avatarMatch = System.Text.RegularExpressions.Regex.Match(response, @"""avatar""\s*:\s*""([^""]+)""");
                    if (avatarMatch.Success) info.Avatar = avatarMatch.Groups[1].Value;
                    
                    // Parse groups array
                    var groupsMatch = System.Text.RegularExpressions.Regex.Match(response, @"""groups""\s*:\s*\[(.*?)\]", 
                        System.Text.RegularExpressions.RegexOptions.Singleline);
                    
                    if (groupsMatch.Success)
                    {
                        var groupsJson = groupsMatch.Groups[1].Value;
                        // Match each group object - extract groupId, groupAccount, and groupName
                        var groupMatches = System.Text.RegularExpressions.Regex.Matches(groupsJson,
                            @"\{[^{}]*""groupId""\s*:\s*(\d+)[^{}]*""groupAccount""\s*:\s*""(\d+)""[^{}]*""groupName""\s*:\s*""([^""]*)""\s*[^{}]*\}");
                        
                        foreach (System.Text.RegularExpressions.Match gm in groupMatches)
                        {
                            var groupInfo = new GroupInfo
                            {
                                // Use groupAccount as the real group number for display and binding
                                GroupId = long.Parse(gm.Groups[2].Value),  // groupAccount (real group number)
                                GroupName = System.Text.RegularExpressions.Regex.Unescape(gm.Groups[3].Value),
                                NimGroupId = gm.Groups[1].Value  // Store internal groupId in NimGroupId
                            };
                            
                            // Try to extract other fields
                            var roleMatch = System.Text.RegularExpressions.Regex.Match(gm.Value, @"""role""\s*:\s*""([^""]+)""");
                            if (roleMatch.Success) groupInfo.Role = roleMatch.Groups[1].Value;
                            
                            var memberMatch = System.Text.RegularExpressions.Regex.Match(gm.Value, @"""memberCount""\s*:\s*(\d+)");
                            if (memberMatch.Success) groupInfo.MemberCount = int.Parse(memberMatch.Groups[1].Value);
                            
                            info.Groups.Add(groupInfo);
                            Log($"  群组: {groupInfo.GroupName} (群号:{groupInfo.GroupId}, 内部ID:{groupInfo.NimGroupId})");
                        }
                    }
                    
                    if (!string.IsNullOrEmpty(info.AccountId))
                    {
                        Log($"Got account: {info.NickName} ({info.AccountId}), Groups: {info.Groups.Count}");
                        return info;
                    }
                }
                
                return null;
                }
            catch (Exception ex)
            {
                Log($"GetFullAccountInfoAsync error: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 切换到指定群聊
        /// </summary>
        /// <param name="groupAccount">群号（groupAccount，如 3962369093）</param>
        /// <returns>是否成功</returns>
        public async Task<bool> SwitchToGroupChatAsync(long groupAccount)
        {
            if (!IsConnected)
            {
                Log("未连接，无法切换群聊");
                return false;
            }
            
            try
            {
                Log($"[DEBUG] Switching to group chat by groupAccount: {groupAccount}");
                
                // Search by groupAccount (real group number) instead of internal groupId
                var script = $@"
(function() {{
    var targetGroupAccount = {groupAccount};
    var targetGroupAccountStr = '{groupAccount}';
    var result = {{ success: false, groupName: '', debug: [] }};
    
    try {{
        var app = document.querySelector('#app');
        if (app && app.__vue_app__) {{
            var pinia = app.__vue_app__.config.globalProperties.$pinia;
            if (pinia) {{
                var appStore = pinia._s.get('app');
                
                // Find the target group by groupAccount (real group number)
                var allGroups = [];
                if (appStore.groupList && appStore.groupList.owner) {{
                    allGroups = allGroups.concat(appStore.groupList.owner);
                }}
                if (appStore.groupList && appStore.groupList.member) {{
                    allGroups = allGroups.concat(appStore.groupList.member);
                }}
                
                result.debug.push('Total groups: ' + allGroups.length);
                
                // Find by groupAccount (the real group number)
                var targetGroup = allGroups.find(function(g) {{ 
                    return g.groupAccount === targetGroupAccount || 
                           g.groupAccount === targetGroupAccountStr ||
                           String(g.groupAccount) === targetGroupAccountStr;
                }});
                
                if (targetGroup) {{
                    result.debug.push('Found group: ' + targetGroup.groupName);
                    
                    if (typeof appStore.setCurrentSession === 'function') {{
                        var groupSession = {{
                            groupInfo: targetGroup,
                            sessionType: 'team'
                        }};
                        appStore.setCurrentSession(groupSession);
                        result.success = true;
                        result.groupName = targetGroup.groupName;
                        result.debug.push('Called setCurrentSession');
                    }}
                }} else {{
                    result.debug.push('Group not found with groupAccount: ' + targetGroupAccount);
                }}
            }}
        }}
    }} catch(e) {{
        result.error = e.message;
        result.debug.push('Error: ' + e.message);
    }}
    
    return JSON.stringify(result);
}})()";
                
                var response = await ExecuteScriptWithResultAsync(script);
                Log($"[DEBUG] SwitchToGroupChatAsync response: {response}");
                
                if (!string.IsNullOrEmpty(response) && response.Contains("\"success\":true"))
                    {
                    // Extract group name for logging
                    var nameMatch = System.Text.RegularExpressions.Regex.Match(response, @"""groupName""\s*:\s*""([^""]+)""");
                    if (nameMatch.Success)
                    {
                        Log($"Successfully switched to group: {nameMatch.Groups[1].Value}");
                    }
                    return true;
                }
                
                Log("Failed to switch to group chat");
                return false;
            }
            catch (Exception ex)
            {
                Log($"SwitchToGroupChatAsync error: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 获取当前打开的聊天窗口中的群成员列表
        /// </summary>
        public async Task<List<ContactInfo>> GetCurrentChatMembersAsync()
        {
            var list = new List<ContactInfo>();
            if (!IsConnected)
            {
                Log("未连接，无法获取群成员");
                return list;
            }
            
            try
            {
                Log("[DEBUG] 正在获取当前聊天窗口的群成员...");
                
                // 注入脚本获取当前聊天窗口的群成员
                var script = @"
(function() {
    var result = [];
    var debug = [];
    
    debug.push('开始扫描当前聊天窗口...');
    debug.push('URL: ' + location.href);
    
    // 获取页面所有文本用于分析
    var bodyText = document.body.innerText || '';
    debug.push('页面文本长度: ' + bodyText.length);
    
    // 方法1：查找群成员头像区域
    var avatarAreas = document.querySelectorAll('[class*=""avatar""], [class*=""member""], [class*=""user""]');
    debug.push('头像/成员区域: ' + avatarAreas.length);
    
    avatarAreas.forEach(function(area) {
        var title = area.getAttribute('title') || '';
        var name = title || (area.innerText || '').split('\n')[0].trim();
        
        if (name && name.length > 0 && name.length < 30) {
            // 过滤无效名称
            if (name.match(/^[\u4e00-\u9fa5a-zA-Z0-9_]+$/) && 
                !name.match(/发送|消息|设置|确定|取消/)) {
                if (!result.find(r => r.name === name)) {
                    result.push({ name: name, id: '', type: 'avatar' });
                }
            }
        }
    });
    
    // 方法2：从消息发送者提取
    var messages = document.querySelectorAll('[class*=""message""], [class*=""msg""], [class*=""chat-item""]');
    debug.push('消息元素: ' + messages.length);
    
    messages.forEach(function(msg) {
        // 查找发送者名称
        var senderEl = msg.querySelector('[class*=""nick""], [class*=""name""], [class*=""sender""], [class*=""from""]');
        if (senderEl) {
            var name = senderEl.innerText.trim();
            if (name && name.length > 0 && name.length < 30 && 
                !result.find(r => r.name === name)) {
                result.push({ name: name, id: '', type: 'sender' });
            }
        }
    });
    
    // 方法3：查找右侧群成员面板
    var rightPanels = document.querySelectorAll('[class*=""right""], [class*=""detail""], [class*=""sidebar""]');
    debug.push('右侧面板: ' + rightPanels.length);
    
    rightPanels.forEach(function(panel) {
        var items = panel.querySelectorAll('[class*=""item""], [class*=""user""], .flex');
        items.forEach(function(item) {
            var text = (item.innerText || '').trim();
            var lines = text.split('\n');
            var name = lines[0] ? lines[0].trim() : '';
            
            if (name && name.length > 0 && name.length < 30 &&
                !name.match(/群成员|成员列表|设置/) &&
                !result.find(r => r.name === name)) {
                result.push({ name: name, id: '', type: 'panel' });
            }
        });
    });
    
    // 方法4：查找所有包含用户信息的flex容器
    var flexItems = document.querySelectorAll('.flex.items-center');
    debug.push('flex容器: ' + flexItems.length);
    
    flexItems.forEach(function(item) {
        var text = (item.innerText || '').trim();
        if (text.length > 0 && text.length < 50) {
            var name = text.split('\n')[0].trim();
            if (name && name.length > 0 && name.length < 20 &&
                !name.match(/发送|消息|设置|确定|取消|群成员/) &&
                !result.find(r => r.name === name)) {
                result.push({ name: name, id: '', type: 'flex' });
            }
        }
    });
    
    // 方法5：尝试从Vue获取
    try {
        var app = document.querySelector('#app');
        if (app && app.__vue__) {
            var vm = app.__vue__;
            debug.push('找到Vue实例');
            
            // 查找当前会话的成员数据
            function findMembers(obj, path, depth) {
                if (depth > 5 || !obj) return;
                
                if (Array.isArray(obj)) {
                    obj.forEach(function(item) {
                        if (item && (item.account || item.nick || item.accid)) {
                            var name = item.nick || item.name || item.account || '';
                            var id = item.account || item.accid || '';
                            if (name && !result.find(r => r.name === name)) {
                                result.push({ name: name, id: id, type: 'vue-' + path });
                            }
                        }
                    });
                }
                
                if (typeof obj === 'object' && obj !== null) {
                    Object.keys(obj).slice(0, 20).forEach(function(key) {
                        if (key.match(/member|user|team|group/i)) {
                            findMembers(obj[key], key, depth + 1);
                        }
                    });
                }
            }
            
            if (vm.$store && vm.$store.state) {
                findMembers(vm.$store.state, 'store', 0);
            }
        }
    } catch(e) {
        debug.push('Vue获取失败: ' + e.message);
    }
    
    debug.push('共找到: ' + result.length + ' 个成员');
    
    return JSON.stringify({ members: result, debug: debug });
})();";
                
                var response = await ExecuteScriptWithResultAsync(script);
                Log($"[DEBUG] 群成员数据: {response}");
                
                if (!string.IsNullOrEmpty(response))
                {
                    // 解析调试信息
                    var debugMatch = System.Text.RegularExpressions.Regex.Match(response, @"""debug""\s*:\s*\[(.*?)\]");
                    if (debugMatch.Success)
                    {
                        Log($"[DEBUG] 脚本调试: {debugMatch.Groups[1].Value}");
                    }
                    
                    // 解析成员
                    var memberMatches = System.Text.RegularExpressions.Regex.Matches(
                        response, 
                        @"\{\s*""name""\s*:\s*""([^""]*)""\s*,\s*""id""\s*:\s*""([^""]*)""\s*,\s*""type""\s*:\s*""([^""]*)""");
                    
                    foreach (System.Text.RegularExpressions.Match match in memberMatches)
                    {
                        var name = match.Groups[1].Value;
                        var id = match.Groups[2].Value;
                        var type = match.Groups[3].Value;
                        
                        // 过滤无效数据
                        if (string.IsNullOrEmpty(name)) continue;
                        if (name.Contains("设置") || name.Contains("确定") || name.Contains("取消")) continue;
                        if (name.Contains("群成员") || name.Contains("成员列表")) continue;
                        
                        var member = new ContactInfo
                        {
                            Name = name,
                            WangShangId = id,
                            Type = type
                        };
                        
                        // 避免重复
                        if (!list.Exists(m => m.Name == name))
                        {
                            list.Add(member);
                            Log($"  - {name} ({id}) [{type}]");
                        }
                    }
                }
                
                Log($"共获取到 {list.Count} 个群成员");
            }
            catch (Exception ex)
            {
                Log($"获取群成员失败: {ex.Message}");
            }
            
            return list;
        }
        
        /// <summary>
        /// 获取群成员列表
        /// </summary>
        public async Task<List<ContactInfo>> GetGroupMembersAsync(string groupId)
        {
            var list = new List<ContactInfo>();
            if (!IsConnected)
            {
                Log("未连接，无法获取群成员");
                return list;
            }
            
            try
            {
                Log($"[DEBUG] 正在获取群 {groupId} 的成员列表...");
                
                // 先获取页面DOM结构用于调试
                var debugScript = @"
(function() {
    var info = {
        url: window.location.href,
        title: document.title
    };
    
    // 查找所有可能包含成员信息的元素
    var selectors = [
        '[class*=""member""]',
        '[class*=""team""]',
        '[class*=""group""]',
        '[class*=""user""]',
        '[class*=""avatar""]',
        '[class*=""chat-header""]',
        '[class*=""session-header""]',
        '.flex.items-center',
        '[data-account]',
        '[data-id]'
    ];
    
    var found = {};
    selectors.forEach(function(sel) {
        try {
            var els = document.querySelectorAll(sel);
            if (els.length > 0) {
                found[sel] = els.length;
            }
        } catch(e) {}
    });
    
    info.selectors = found;
    
    // 获取右侧面板内容（群成员通常在这里）
    var rightPanel = document.querySelector('[class*=""right""], [class*=""detail""], [class*=""info""]');
    if (rightPanel) {
        info.rightPanelText = rightPanel.innerText.substring(0, 500);
    }
    
    // 获取聊天头部信息
    var header = document.querySelector('[class*=""header""]');
    if (header) {
        info.headerText = header.innerText.substring(0, 200);
    }
    
    return JSON.stringify(info);
})();";
                
                var debugInfo = await ExecuteScriptWithResultAsync(debugScript);
                Log($"[DEBUG] 页面信息: {debugInfo}");
                
                // 注入脚本获取群成员
                var script = @"
(function() {
    var result = [];
    var debug = [];
    
    // 方法1：从群成员列表面板获取（右侧面板）
    var memberPanels = document.querySelectorAll('[class*=""member""], [class*=""team-member""], [class*=""group-member""]');
    debug.push('member面板: ' + memberPanels.length);
    
    memberPanels.forEach(function(panel) {
        // 获取面板内的用户项
        var items = panel.querySelectorAll('[class*=""item""], [class*=""user""], .flex');
        items.forEach(function(item) {
            var text = item.innerText.trim();
            var name = text.split('\n')[0].trim();
            var id = item.getAttribute('data-account') || item.getAttribute('data-id') || '';
            
            // 尝试从Vue获取
            try {
                if (item.__vue__ && item.__vue__.member) {
                    id = item.__vue__.member.account || id;
                    name = item.__vue__.member.nick || name;
                }
            } catch(e) {}
            
            if (name && name.length > 0 && name.length < 30 && !result.find(r => r.name === name)) {
                result.push({ name: name, id: id || 'unknown', type: 'panel' });
            }
        });
    });
    
    // 方法2：从头像列表获取
    var avatarItems = document.querySelectorAll('[class*=""avatar""][class*=""item""], [class*=""avatar-wrapper""]');
    debug.push('avatar元素: ' + avatarItems.length);
    
    avatarItems.forEach(function(item) {
        var name = item.getAttribute('title') || item.getAttribute('alt') || '';
        var id = item.getAttribute('data-account') || '';
        
        // 检查父元素
        var parent = item.parentElement;
        if (parent) {
            var nameEl = parent.querySelector('[class*=""name""], [class*=""nick""]');
            if (nameEl) name = nameEl.innerText.trim();
        }
        
        if (name && name.length > 0 && name.length < 30 && !result.find(r => r.name === name)) {
            result.push({ name: name, id: id || 'unknown', type: 'avatar' });
        }
    });
    
    // 方法3：从聊天消息中提取发送者
    var msgItems = document.querySelectorAll('[class*=""message""], [class*=""msg-item""], [class*=""chat-item""]');
    debug.push('message元素: ' + msgItems.length);
    
    msgItems.forEach(function(item) {
        var senderEl = item.querySelector('[class*=""sender""], [class*=""nick""], [class*=""name""], [class*=""from""]');
        if (senderEl) {
            var name = senderEl.innerText.trim();
            var id = item.getAttribute('data-from') || item.getAttribute('data-sender') || '';
            
            // 尝试从Vue获取
            try {
                if (item.__vue__ && item.__vue__.msg) {
                    id = item.__vue__.msg.from || id;
                    name = item.__vue__.msg.fromNick || name;
                }
            } catch(e) {}
            
            if (name && name.length > 0 && name.length < 30 && !result.find(r => r.name === name)) {
                result.push({ name: name, id: id || 'unknown', type: 'message' });
            }
        }
    });
    
    // 方法4：从flex布局的用户列表获取
    var flexItems = document.querySelectorAll('.flex.items-center, .flex.justify-between');
    debug.push('flex元素: ' + flexItems.length);
    
    flexItems.forEach(function(item) {
        var text = item.innerText.trim();
        // 过滤掉太长或包含特殊内容的
        if (text.length > 50 || text.includes('发送') || text.includes('消息')) return;
        
        var lines = text.split('\n').filter(l => l.trim().length > 0);
        if (lines.length >= 1) {
            var name = lines[0].trim();
            var id = item.getAttribute('data-account') || item.getAttribute('data-id') || '';
            
            // 检查是否像用户名（不是按钮、菜单等）
            if (name && name.length > 0 && name.length < 20 && 
                !name.includes('设置') && !name.includes('确定') && !name.includes('取消') &&
                !result.find(r => r.name === name)) {
                result.push({ name: name, id: id || 'unknown', type: 'flex' });
            }
        }
    });
    
    // 方法5：尝试从Vue/Pinia获取群成员数据
    try {
        var app = document.querySelector('#app');
        if (app && app.__vue__) {
            var vm = app.__vue__;
            debug.push('找到Vue实例');
            
            // 递归查找成员数据
            function searchMembers(obj, path, depth) {
                if (depth > 6 || !obj) return;
                
                // 检查是否是成员数组
                if (Array.isArray(obj) && obj.length > 0) {
                    var first = obj[0];
                    if (first && (first.account || first.accid || first.uid)) {
                        debug.push('在 ' + path + ' 找到成员数组，长度: ' + obj.length);
                        obj.forEach(function(m) {
                            var id = m.account || m.accid || m.uid || '';
                            var name = m.nick || m.nickname || m.name || id;
                            if (name && !result.find(r => r.id === id)) {
                                result.push({ name: name, id: id, type: 'vue-' + path });
                            }
                        });
                    }
                }
                
                // 继续搜索对象属性
                if (typeof obj === 'object' && obj !== null) {
                    var keys = Object.keys(obj).slice(0, 50);
                    for (var i = 0; i < keys.length; i++) {
                        var key = keys[i];
                        if (key.match(/member|user|team|group|friend/i)) {
                            searchMembers(obj[key], path + '.' + key, depth + 1);
                        }
                    }
                }
            }
            
            // 从$store搜索
            if (vm.$store && vm.$store.state) {
                searchMembers(vm.$store.state, '$store.state', 0);
            }
            
            // 从_data搜索
            if (vm._data) {
                searchMembers(vm._data, '_data', 0);
            }
        }
    } catch(e) {
        debug.push('Vue搜索失败: ' + e.message);
    }
    
    debug.push('总共找到 ' + result.length + ' 个成员');
    
    return JSON.stringify({ members: result, debug: debug });
})();";
                
                var response = await ExecuteScriptWithResultAsync(script);
                Log($"[DEBUG] 群成员数据: {response}");
                
                if (!string.IsNullOrEmpty(response))
                {
                    // 解析调试信息
                    var debugMatch = System.Text.RegularExpressions.Regex.Match(response, @"""debug""\s*:\s*\[(.*?)\]");
                    if (debugMatch.Success)
                    {
                        Log($"[DEBUG] 脚本调试: {debugMatch.Groups[1].Value}");
                    }
                    
                    // 解析成员
                    var memberMatches = System.Text.RegularExpressions.Regex.Matches(
                        response, 
                        @"\{\s*""name""\s*:\s*""([^""]*)""\s*,\s*""id""\s*:\s*""([^""]*)""\s*,\s*""type""\s*:\s*""([^""]*)""");
                    
                    foreach (System.Text.RegularExpressions.Match match in memberMatches)
                    {
                        var name = match.Groups[1].Value;
                        var id = match.Groups[2].Value;
                        var type = match.Groups[3].Value;
                        
                        // 过滤无效数据
                        if (string.IsNullOrEmpty(name) || name == "unknown") continue;
                        if (name.Contains("设置") || name.Contains("确定") || name.Contains("取消")) continue;
                        
                        var member = new ContactInfo
                        {
                            Name = name,
                            WangShangId = id,
                            Type = type
                        };
                        
                        // 避免重复
                        if (!list.Exists(m => m.Name == name || (m.WangShangId == id && id != "unknown")))
                        {
                            list.Add(member);
                            Log($"  - {name} ({id}) [{type}]");
                        }
                    }
                }
                
                Log($"共获取到 {list.Count} 个群成员");
            }
            catch (Exception ex)
            {
                Log($"获取群成员失败: {ex.Message}");
                Log($"[DEBUG] 异常详情: {ex.StackTrace}");
            }
            
            return list;
        }
        
        /// <summary>
        /// 执行脚本并获取返回值（通过pending请求机制）
        /// </summary>
        private async Task<string> ExecuteScriptWithResultAsync(string script)
        {
            return await ExecuteScriptWithResultAsync(script, false);
        }
        
        /// <summary>
        /// 执行脚本并获取返回值（通过pending请求机制，支持Promise等待）
        /// </summary>
        /// <param name="script">要执行的JavaScript脚本</param>
        /// <param name="awaitPromise">是否等待Promise完成（用于async函数）</param>
        private async Task<string> ExecuteScriptWithResultAsync(string script, bool awaitPromise)
        {
            if (!IsConnected || _webSocket.State != WebSocketState.Open)
            {
                Log("[DEBUG] WebSocket未连接");
                return null;
            }
            
            var id = ++_messageId;
            var escapedScript = EscapeJson(script);
            
            // Build CDP command with optional awaitPromise parameter
            string message;
            if (awaitPromise)
            {
                message = $"{{\"id\":{id},\"method\":\"Runtime.evaluate\",\"params\":{{\"expression\":\"{escapedScript}\",\"returnByValue\":true,\"awaitPromise\":true}}}}";
            }
            else
            {
                message = $"{{\"id\":{id},\"method\":\"Runtime.evaluate\",\"params\":{{\"expression\":\"{escapedScript}\",\"returnByValue\":true}}}}";
            }
            
            Log($"[DEBUG] 发送CDP命令 ID={id}, awaitPromise={awaitPromise}");
            
            // 创建等待任务
            var tcs = new TaskCompletionSource<string>();
            lock (_pendingRequests)
            {
                _pendingRequests[id] = tcs;
            }
            
            try
            {
                // 发送命令
                var sendBuffer = Encoding.UTF8.GetBytes(message);
                await _webSocket.SendAsync(new ArraySegment<byte>(sendBuffer), 
                    WebSocketMessageType.Text, true, _cts.Token);
                
                // 等待响应（由ReceiveLoopAsync处理）- Promise需要更长超时
                // Mute/unmute scripts with retry logic can take up to 60+ seconds
                var timeoutMs = awaitPromise ? 90000 : 15000;
                var timeoutTask = Task.Delay(timeoutMs);
                var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);
                
                if (completedTask == tcs.Task)
                {
                    var result = await tcs.Task;
                    Log($"[DEBUG] 获取到结果，长度={result?.Length ?? 0}");
                    return result;
                }
                else
                {
                    Log("[DEBUG] 等待响应超时");
                    return null;
                }
            }
            finally
            {
                lock (_pendingRequests)
                {
                    _pendingRequests.Remove(id);
                }
            }
        }
        
        // 存储待处理的请求
        private Dictionary<int, TaskCompletionSource<string>> _pendingRequests = 
            new Dictionary<int, TaskCompletionSource<string>>();
        
        /// <summary>
        /// 执行JavaScript脚本（正确等待CDP响应）
        /// </summary>
        private async Task<string> ExecuteScriptAsync(string script)
        {
            if (_webSocket == null || _webSocket.State != WebSocketState.Open)
            {
                Log("WebSocket未连接，无法执行脚本");
                return "ERROR: WebSocket not connected";
            }
            
            var id = ++_messageId;
            var tcs = new TaskCompletionSource<string>();
            
            // Register pending request to receive response
            lock (_pendingRequests)
            {
                _pendingRequests[id] = tcs;
            }
            
            try
            {
            var message = $"{{\"id\":{id},\"method\":\"Runtime.evaluate\",\"params\":{{\"expression\":\"{EscapeJson(script)}\",\"awaitPromise\":true,\"returnByValue\":true}}}}";
            
            var buffer = Encoding.UTF8.GetBytes(message);
            await _webSocket.SendAsync(new ArraySegment<byte>(buffer), 
                WebSocketMessageType.Text, true, _cts.Token);
            
                // Wait for response with timeout (5 seconds)
                var timeoutTask = Task.Delay(5000);
                var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);
                
                if (completedTask == timeoutTask)
                {
                    Log($"脚本执行超时 (ID={id})");
                    return "SUCCESS"; // Assume success if timeout (script likely executed)
                }
                
                var result = await tcs.Task;
                Log($"脚本执行结果: {(result?.Length > 100 ? result.Substring(0, 100) + "..." : result)}");
                return result ?? "SUCCESS";
            }
            catch (Exception ex)
            {
                Log($"执行脚本异常: {ex.Message}");
                return "ERROR: " + ex.Message;
            }
            finally
            {
                // Clean up pending request
                lock (_pendingRequests)
                {
                    _pendingRequests.Remove(id);
                }
            }
        }
        
        /// <summary>
        /// 启用网络监听
        /// </summary>
        private async Task EnableNetworkMonitoring()
        {
            var id = ++_messageId;
            var message = $"{{\"id\":{id},\"method\":\"Network.enable\"}}";
            var buffer = Encoding.UTF8.GetBytes(message);
            await _webSocket.SendAsync(new ArraySegment<byte>(buffer), 
                WebSocketMessageType.Text, true, _cts.Token);
        }
        
        /// <summary>
        /// 注入消息监听脚本（监听DOM变化和新消息）
        /// </summary>
        private async Task InjectMessageListener()
        {
            var script = @"
(function() {
    if (window.__chatBotInjected) {
        console.log('[ChatBot] 已经注入过，跳过');
        return 'ALREADY_INJECTED';
    }
window.__chatBotInjected = true;
    window.__chatBotMessages = [];
    window.__chatBotLastMessageCount = 0;
    
    console.log('[ChatBot] 开始注入消息监听...');
    
    // Method 1: MutationObserver to monitor DOM changes for new messages
    function setupMutationObserver() {
        var observer = new MutationObserver(function(mutations) {
            mutations.forEach(function(mutation) {
                if (mutation.addedNodes.length > 0) {
                    mutation.addedNodes.forEach(function(node) {
                        if (node.nodeType === 1) {
                            // Check if this is a message element
                            var msgText = node.innerText || node.textContent;
                            if (msgText && msgText.length > 0 && msgText.length < 5000) {
                                var classList = node.className || '';
                                if (classList.includes('message') || classList.includes('msg') || 
                                    classList.includes('chat') || classList.includes('bubble')) {
                                    console.log('[ChatBot] New message detected: ' + msgText.substring(0, 100));
                                    window.__chatBotMessages.push({
                                        text: msgText,
                                        time: new Date().toISOString(),
                                        element: classList
                                    });
                                }
                            }
                        }
                    });
                }
            });
        });
        
        observer.observe(document.body, {
            childList: true,
            subtree: true
        });
        console.log('[ChatBot] MutationObserver 已启动');
    }
    
    // Method 2: Intercept XMLHttpRequest to capture API responses
    function interceptXHR() {
        var originalOpen = XMLHttpRequest.prototype.open;
        var originalSend = XMLHttpRequest.prototype.send;
        
        XMLHttpRequest.prototype.open = function(method, url) {
            this._chatBotUrl = url;
            return originalOpen.apply(this, arguments);
        };
        
        XMLHttpRequest.prototype.send = function(body) {
            var self = this;
            this.addEventListener('load', function() {
                if (self._chatBotUrl && (self._chatBotUrl.includes('message') || 
                    self._chatBotUrl.includes('chat') || self._chatBotUrl.includes('msg'))) {
                    try {
                        console.log('[ChatBot] XHR Response from: ' + self._chatBotUrl);
                    } catch(e) {}
                }
            });
            return originalSend.apply(this, arguments);
        };
        console.log('[ChatBot] XHR 拦截已启动');
    }
    
    // Method 3: Intercept fetch for modern API calls
    function interceptFetch() {
        var originalFetch = window.fetch;
        window.fetch = function(url, options) {
            return originalFetch.apply(this, arguments).then(function(response) {
                var urlStr = typeof url === 'string' ? url : url.url;
                if (urlStr && (urlStr.includes('message') || urlStr.includes('chat'))) {
                    console.log('[ChatBot] Fetch Response from: ' + urlStr);
                }
                return response;
            });
        };
        console.log('[ChatBot] Fetch 拦截已启动');
    }
    
    // Initialize all listeners
    try {
        setupMutationObserver();
        interceptXHR();
        interceptFetch();
        console.log('[ChatBot] 消息监听注入完成！');
        return 'SUCCESS';
    } catch(e) {
        console.error('[ChatBot] 注入失败: ' + e.message);
        return 'ERROR: ' + e.message;
    }
})();
";
            var result = await ExecuteScriptAsync(script);
            Log($"消息监听注入结果: {result}");
        }
        
        /// <summary>
        /// 消息接收循环
        /// </summary>
        private async Task ReceiveLoopAsync()
        {
            var buffer = new byte[8192];
            var messageBuilder = new StringBuilder();
            
            try
            {
                while (!_cts.IsCancellationRequested && 
                       _webSocket.State == WebSocketState.Open)
                {
                    var result = await _webSocket.ReceiveAsync(
                        new ArraySegment<byte>(buffer), _cts.Token);
                    
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        break;
                    }
                    
                    messageBuilder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                    
                    if (result.EndOfMessage)
                    {
                        var message = messageBuilder.ToString();
                        messageBuilder.Clear();
                        
                        // 处理接收到的消息
                        ProcessReceivedMessage(message);
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Log($"接收消息异常: {ex.Message}");
            }
            
            IsConnected = false;
            OnConnectionChanged?.Invoke(false);
        }
        
        /// <summary>
        /// 处理接收到的消息
        /// </summary>
        private void ProcessReceivedMessage(string message)
        {
            try
            {
                // 解析响应ID
                var idMatch = System.Text.RegularExpressions.Regex.Match(message, @"""id""\s*:\s*(\d+)");
                if (idMatch.Success)
                {
                    var id = int.Parse(idMatch.Groups[1].Value);
                    
                    TaskCompletionSource<string> tcs = null;
                    lock (_pendingRequests)
                    {
                        _pendingRequests.TryGetValue(id, out tcs);
                    }
                    
                    // 检查是否有等待此响应的请求
                    if (tcs != null)
                    {
                        Log($"[DEBUG] 收到响应 ID={id}，长度={message.Length}");
                        
                        // 提取value - 支持字符串和对象
                        // 格式1: "value":"xxx"
                        // 格式2: "value":{...}
                        string value = null;
                        
                        // 先尝试字符串格式
                        var stringMatch = System.Text.RegularExpressions.Regex.Match(
                            message, @"""value""\s*:\s*""((?:[^""\\]|\\.)*)""",
                            System.Text.RegularExpressions.RegexOptions.Singleline);
                        
                        if (stringMatch.Success)
                        {
                            value = stringMatch.Groups[1].Value;
                            // 解码转义字符
                            value = System.Text.RegularExpressions.Regex.Unescape(value);
                            Log($"[DEBUG] 提取字符串value，长度={value.Length}");
                        }
                        else
                        {
                            // 尝试对象格式
                            var objMatch = System.Text.RegularExpressions.Regex.Match(
                                message, @"""value""\s*:\s*(\{.*\}|\[.*\])",
                                System.Text.RegularExpressions.RegexOptions.Singleline);
                            if (objMatch.Success)
                            {
                                value = objMatch.Groups[1].Value;
                                Log($"[DEBUG] 提取对象value，长度={value.Length}");
                            }
                            else
                            {
                                Log($"[DEBUG] 未找到value，响应前500字符: {message.Substring(0, Math.Min(500, message.Length))}");
                            }
                        }
                        
                        tcs.TrySetResult(value);
                    }
                }
                
                // 检查是否包含聊天消息事件
                if (message.Contains("[ChatBot]"))
                {
                    Log($"收到事件: {message.Substring(0, Math.Min(200, message.Length))}");
                }
            }
            catch (Exception ex)
            {
                Log($"处理消息异常: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 转义JSON字符串
        /// </summary>
        private string EscapeJson(string str)
        {
            return str.Replace("\\", "\\\\")
                     .Replace("\"", "\\\"")
                     .Replace("\n", "\\n")
                     .Replace("\r", "\\r")
                     .Replace("\t", "\\t");
        }

        /// <summary>
        /// Wrap string as a JSON string literal (or "null")
        /// </summary>
        private string ToJsonString(string value)
        {
            if (value == null) return "null";
            return $"\"{EscapeJson(value)}\"";
        }

        /// <summary>
        /// Extract a string field from a JSON-like response by regex (best-effort).
        /// </summary>
        private string ExtractJsonField(string json, string fieldName)
        {
            if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(fieldName)) return null;
            var match = System.Text.RegularExpressions.Regex.Match(
                json,
                $"\"{System.Text.RegularExpressions.Regex.Escape(fieldName)}\"\\s*:\\s*\"((?:[^\"\\\\]|\\\\.)*)\"",
                System.Text.RegularExpressions.RegexOptions.Singleline);
            if (!match.Success) return null;
            return System.Text.RegularExpressions.Regex.Unescape(match.Groups[1].Value);
        }
        
        private void Log(string message)
        {
            Logger.Info($"[ChatService] {message}");
            OnLog?.Invoke(message);
        }
        
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
                        if (_processedMessageHashes.Contains(hash) || isSent)
                        {
                            continue;
                        }
                        
                        // Mark as processed
                        _processedMessageHashes.Add(hash);
                        
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
        
        #region UI Automation 连接方式
        
        /// <summary>
        /// 使用 UI Automation 连接到旺商聊（不需要调试端口）
        /// </summary>
        public async Task<bool> ConnectWithUIAutomationAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    Log("========== 使用 UI Automation 连接 ==========");
                    
                    // 查找旺商聊进程
                    var processes = Process.GetProcessesByName("wangshangliao_win_online");
                    if (processes.Length == 0)
                    {
                        Log("✗ 旺商聊未运行");
                        return false;
                    }
                    
                    var process = processes[0];
                    _mainWindowHandle = process.MainWindowHandle;
                    
                    if (_mainWindowHandle == IntPtr.Zero)
                    {
                        Log("✗ 无法获取旺商聊主窗口句柄");
                        return false;
                    }
                    
                    Log(string.Format("✓ 找到旺商聊窗口 (句柄: {0})", _mainWindowHandle));
                    
                    // 获取 UI Automation 元素
                    _mainWindow = AutomationElement.FromHandle(_mainWindowHandle);
                    if (_mainWindow == null)
                    {
                        Log("✗ 无法获取 UI Automation 元素");
                        return false;
                    }
                    
                    Log("✓ UI Automation 连接成功！");
                    
                    IsConnected = true;
                    Mode = ConnectionMode.UIAutomation;
                    OnConnectionChanged?.Invoke(true);
                    
                    return true;
                }
                catch (Exception ex)
                {
                    Log(string.Format("UI Automation 连接失败: {0}", ex.Message));
                    return false;
                }
            });
        }
        
        /// <summary>
        /// 通过 UI Automation 获取联系人列表
        /// </summary>
        public List<ContactInfo> GetContactsWithUIAutomation()
        {
            var contacts = new List<ContactInfo>();
            
            if (_mainWindow == null)
            {
                Log("UI Automation 未连接");
                return contacts;
            }
            
            try
            {
                // 查找列表元素
                var listCondition = new PropertyCondition(
                    AutomationElement.ControlTypeProperty, ControlType.List);
                var lists = _mainWindow.FindAll(TreeScope.Descendants, listCondition);
                
                Log(string.Format("找到 {0} 个列表控件", lists.Count));
                
                foreach (AutomationElement list in lists)
                {
                    // 获取列表项
                    var itemCondition = new PropertyCondition(
                        AutomationElement.ControlTypeProperty, ControlType.ListItem);
                    var items = list.FindAll(TreeScope.Children, itemCondition);
                    
                    foreach (AutomationElement item in items)
                    {
                        var name = item.Current.Name;
                        if (!string.IsNullOrEmpty(name))
                        {
                            contacts.Add(new ContactInfo
                            {
                                Name = name,
                                WangShangId = ""
                            });
                        }
                    }
                }
                
                Log(string.Format("通过 UI Automation 获取到 {0} 个联系人", contacts.Count));
            }
            catch (Exception ex)
            {
                Log(string.Format("获取联系人失败: {0}", ex.Message));
            }
            
            return contacts;
        }
        
        /// <summary>
        /// 通过 UI Automation 发送消息（模拟输入）
        /// </summary>
        public bool SendMessageWithUIAutomation(string message)
        {
            if (_mainWindow == null)
            {
                Log("UI Automation 未连接");
                return false;
            }
            
            try
            {
                // 查找输入框
                var editCondition = new PropertyCondition(
                    AutomationElement.ControlTypeProperty, ControlType.Edit);
                var edits = _mainWindow.FindAll(TreeScope.Descendants, editCondition);
                
                AutomationElement inputBox = null;
                foreach (AutomationElement edit in edits)
                {
                    var name = edit.Current.Name ?? "";
                    if (name.Contains("消息") || name.Contains("输入"))
                    {
                        inputBox = edit;
                        break;
                    }
                }
                
                if (inputBox == null && edits.Count > 0)
                {
                    // 使用最后一个输入框
                    inputBox = edits[edits.Count - 1];
                }
                
                if (inputBox == null)
                {
                    Log("找不到输入框");
                    return false;
                }
                
                // 设置焦点
                inputBox.SetFocus();
                
                // 尝试使用 ValuePattern 设置文本
                object pattern;
                if (inputBox.TryGetCurrentPattern(ValuePattern.Pattern, out pattern))
                {
                    ((ValuePattern)pattern).SetValue(message);
                }
                else
                {
                    // 使用键盘模拟输入
                    System.Windows.Forms.SendKeys.SendWait(message);
                }
                
                // 查找发送按钮并点击
                var buttonCondition = new PropertyCondition(
                    AutomationElement.ControlTypeProperty, ControlType.Button);
                var buttons = _mainWindow.FindAll(TreeScope.Descendants, buttonCondition);
                
                foreach (AutomationElement button in buttons)
                {
                    var name = button.Current.Name ?? "";
                    if (name.Contains("发送"))
                    {
                        object invokePattern;
                        if (button.TryGetCurrentPattern(InvokePattern.Pattern, out invokePattern))
                        {
                            ((InvokePattern)invokePattern).Invoke();
                            Log("消息已发送 (UI Automation)");
                            return true;
                        }
                    }
                }
                
                // 如果没找到发送按钮，按回车发送
                System.Windows.Forms.SendKeys.SendWait("{ENTER}");
                Log("消息已发送 (回车)");
                return true;
            }
            catch (Exception ex)
            {
                Log(string.Format("发送消息失败: {0}", ex.Message));
                return false;
            }
        }

        // =====================================================================
        // Team member moderation - mute / kick (CDP + NIM SDK)
        // =====================================================================

        /// <summary>
        /// Mute/unmute a specific team member.
        /// Notes:
        /// - NIM Web SDK uses different method names in different builds; we try multiple.
        /// - Duration is managed by upper layer (we just set mute true/false).
        /// </summary>
        public async Task<(bool Success, string Message)> MuteTeamMemberAsync(string teamId, string accountId, bool mute)
        {
            if (!IsConnected || Mode != ConnectionMode.CDP)
                return (false, "未连接或非CDP模式");
            if (string.IsNullOrWhiteSpace(teamId) || string.IsNullOrWhiteSpace(accountId))
                return (false, "teamId/accountId为空");

            try
            {
                var teamJson = ToJsonString(teamId.Trim());
                var accJson = ToJsonString(accountId.Trim());
                var muteJs = mute ? "true" : "false";

                var script = $@"
(async function() {{
  var result = {{ success:false, message:'', error:null }};
  try {{
    if (!window.nim) {{
      result.error = 'NIM SDK not found';
      result.message = result.error;
      return JSON.stringify(result);
    }}
    var teamId = String({teamJson});
    var account = String({accJson});
    var mute = {muteJs};

    function call(fnName, payload) {{
      return new Promise(function(resolve) {{
        if (typeof window.nim[fnName] !== 'function') return resolve({{ ok:false, err:'notfound' }});
        payload.done = function(err, obj) {{
          if (err) resolve({{ ok:false, err: err.message || String(err), code: err.code || null }});
          else resolve({{ ok:true }});
        }};
        try {{ window.nim[fnName](payload); }} catch(e) {{ resolve({{ ok:false, err: e.message }}); }}
        setTimeout(function() {{ resolve({{ ok:false, err:'Timeout' }}); }}, 6000);
      }});
    }}

    // Try common APIs
    var r = await call('muteTeamMember', {{ teamId: teamId, account: account, mute: mute }});
    if (!r.ok) r = await call('muteTeamMembers', {{ teamId: teamId, accounts: [account], mute: mute }});
    if (!r.ok) r = await call('muteMember', {{ teamId: teamId, account: account, mute: mute }});

    if (r.ok) {{
      result.success = true;
      result.message = mute ? 'Muted' : 'Unmuted';
    }} else {{
      result.error = r.err;
      result.message = r.err;
    }}
  }} catch(e) {{
    result.error = e.message;
    result.message = 'Exception: ' + e.message;
  }}
  return JSON.stringify(result);
}})();";

                var resp = await ExecuteScriptWithResultAsync(script, true);
                var ok = !string.IsNullOrEmpty(resp) && resp.Contains("\"success\":true");
                var msg = ExtractJsonField(resp, "message") ?? resp;
                var err = ExtractJsonField(resp, "error");
                return ok ? (true, msg) : (false, !string.IsNullOrEmpty(err) ? err : msg);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        /// <summary>
        /// Kick one member from a team (group chat).
        /// </summary>
        public async Task<(bool Success, string Message)> KickTeamMemberAsync(string teamId, string accountId)
        {
            if (!IsConnected || Mode != ConnectionMode.CDP)
                return (false, "未连接或非CDP模式");
            if (string.IsNullOrWhiteSpace(teamId) || string.IsNullOrWhiteSpace(accountId))
                return (false, "teamId/accountId为空");

            try
            {
                var teamJson = ToJsonString(teamId.Trim());
                var accJson = ToJsonString(accountId.Trim());

                var script = $@"
(async function() {{
  var result = {{ success:false, message:'', error:null }};
  try {{
    if (!window.nim) {{
      result.error = 'NIM SDK not found';
      result.message = result.error;
      return JSON.stringify(result);
    }}
    var teamId = String({teamJson});
    var account = String({accJson});
    function call(fnName, payload) {{
      return new Promise(function(resolve) {{
        if (typeof window.nim[fnName] !== 'function') return resolve({{ ok:false, err:'notfound' }});
        payload.done = function(err, obj) {{
          if (err) resolve({{ ok:false, err: err.message || String(err), code: err.code || null }});
          else resolve({{ ok:true }});
        }};
        try {{ window.nim[fnName](payload); }} catch(e) {{ resolve({{ ok:false, err: e.message }}); }}
        setTimeout(function() {{ resolve({{ ok:false, err:'Timeout' }}); }}, 8000);
      }});
    }}
    var r = await call('removeTeamMembers', {{ teamId: teamId, accounts: [account] }});
    if (!r.ok) r = await call('kickTeamMembers', {{ teamId: teamId, accounts: [account] }});
    if (r.ok) {{
      result.success = true;
      result.message = 'Kicked';
    }} else {{
      result.error = r.err;
      result.message = r.err;
    }}
  }} catch(e) {{
    result.error = e.message;
    result.message = 'Exception: ' + e.message;
  }}
  return JSON.stringify(result);
}})();";

                var resp = await ExecuteScriptWithResultAsync(script, true);
                var ok = !string.IsNullOrEmpty(resp) && resp.Contains("\"success\":true");
                var msg = ExtractJsonField(resp, "message") ?? resp;
                var err = ExtractJsonField(resp, "error");
                return ok ? (true, msg) : (false, !string.IsNullOrEmpty(err) ? err : msg);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        /// <summary>
        /// Recall (delete) a message by idClient.
        /// 撤回消息 - 根据消息的 idClient 撤回
        /// </summary>
        /// <param name="msg">ChatMessage with IdClient, GroupId/SenderId</param>
        /// <returns>(Success, Message)</returns>
        public async Task<(bool Success, string Message)> RecallMessageAsync(ChatMessage msg)
        {
            if (!IsConnected || Mode != ConnectionMode.CDP)
                return (false, "未连接或非CDP模式");
            if (msg == null || string.IsNullOrWhiteSpace(msg.IdClient))
                return (false, "消息ID为空");

            try
            {
                var idClientJson = ToJsonString(msg.IdClient);
                var toJson = ToJsonString(msg.IsGroupMessage ? msg.GroupId : msg.SenderId);
                var sceneStr = msg.IsGroupMessage ? "team" : "p2p";

                var script = $@"
(async function() {{
  var result = {{ success:false, message:'', error:null }};
  try {{
    if (!window.nim) {{
      result.error = 'NIM SDK not found';
      return JSON.stringify(result);
    }}
    var msgObj = {{ idClient: {idClientJson}, to: {toJson}, scene: '{sceneStr}' }};
    function call(fnName, payload) {{
      return new Promise(function(resolve) {{
        if (typeof window.nim[fnName] !== 'function') return resolve({{ ok:false, err:'notfound' }});
        payload.done = function(err, obj) {{
          if (err) resolve({{ ok:false, err: err.message || String(err) }});
          else resolve({{ ok:true }});
        }};
        try {{ window.nim[fnName](payload); }} catch(e) {{ resolve({{ ok:false, err: e.message }}); }}
        setTimeout(function() {{ resolve({{ ok:false, err:'Timeout' }}); }}, 5000);
      }});
    }}
    // Try different recall methods
    var r = await call('deleteMsg', {{ msg: msgObj }});
    if (!r.ok) r = await call('recallMsg', {{ msg: msgObj }});
    if (!r.ok) r = await call('deleteMsgSelf', {{ msg: msgObj }});
    if (r.ok) {{
      result.success = true;
      result.message = 'Recalled';
    }} else {{
      result.error = r.err;
      result.message = r.err;
    }}
  }} catch(e) {{
    result.error = e.message;
  }}
  return JSON.stringify(result);
}})();";

                var resp = await ExecuteScriptWithResultAsync(script, true);
                var ok = !string.IsNullOrEmpty(resp) && resp.Contains("\"success\":true");
                var errMsg = ExtractJsonField(resp, "error") ?? ExtractJsonField(resp, "message") ?? resp;
                return ok ? (true, "Recalled") : (false, errMsg);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }
        
        #endregion
        
        #region Group Management - Mute/Unmute
        
        /// <summary>
        /// Mute all members in a group chat by simulating UI switch click
        /// 全体禁言 - 通过模拟点击界面开关实现（无频率限制）
        /// </summary>
        /// <param name="groupAccount">群号（如 3962369093），为空则使用当前会话的群</param>
        /// <returns>(success, groupName, message)</returns>
        public async Task<(bool Success, string GroupName, string Message)> MuteAllAsync(string groupAccount = null)
        {
            if (!IsConnected || Mode != ConnectionMode.CDP)
            {
                Log("未连接或非CDP模式，无法执行全体禁言");
                return (false, null, "未连接或非CDP模式");
            }
            
            try
            {
                Log($"开始执行全体禁言... 群号: {groupAccount ?? "当前会话"}");
                
                // DOM AUTOMATION: Click the mute switch directly (most reliable method)
                // This ensures both server and UI state are properly synchronized
                var groupAccountSearch = string.IsNullOrEmpty(groupAccount) ? "null" : ToJsonString(groupAccount);
                
                // FULL AUTOMATION v3: Dropdown -> View Card -> Settings -> Mute switch
                var clickScript = @"
(async function() {
    var result = { success: false, groupName: null, message: '', clicked: false, isNowMuted: null, panelOpen: false, method: 'dom' };
    function sleep(ms) { return new Promise(function(r) { setTimeout(r, ms); }); }
    
    var muteText = '\u5168\u5458\u7981\u8a00';  // 全员禁言
    var viewCardText = '\u67e5\u770b\u7fa4\u540d\u7247';  // 查看群名片
    var settingsText = '\u8bbe\u7f6e';  // 设置
    
    // Helper to find mute switch by checking parent/grandparent/container context
    function findMuteSwitch() {
        var switches = document.querySelectorAll('[class*=""switch""]');
        for (var i = 0; i < switches.length; i++) {
            var sw = switches[i];
            var rect = sw.getBoundingClientRect();
            if (rect.width === 0 || rect.height === 0) continue;
            
            var parent = sw.parentElement;
            var grandparent = parent ? parent.parentElement : null;
            var container = grandparent ? grandparent.parentElement : null;
            var context = (parent ? parent.innerText : '') + ' ' + (grandparent ? grandparent.innerText : '') + ' ' + (container ? container.innerText : '');
            
            if (context.indexOf(muteText) >= 0) {
                return sw;
            }
        }
        return null;
    }
    
    // Helper to get current group name from Pinia
    function getGroupName() {
        try {
            var app = document.querySelector('#app');
            var gp = app && app.__vue_app__ && app.__vue_app__.config && app.__vue_app__.config.globalProperties;
            var pinia = gp && gp.$pinia;
            var appStore = pinia && pinia._s && pinia._s.get && pinia._s.get('app');
            var groupInfoRes = appStore ? appStore.groupInfoRes : null;
            if (groupInfoRes && groupInfoRes.groupInfo) {
                return groupInfoRes.groupInfo.name || groupInfoRes.groupInfo.groupName;
            }
        } catch(e) {}
        return null;
    }
    
    // CRITICAL: Ensure session context is set for backend API (fixes GroupId > 0 error)
    function ensureSessionContext() {
        try {
            var app = document.querySelector('#app');
            var gp = app && app.__vue_app__ && app.__vue_app__.config && app.__vue_app__.config.globalProperties;
            var pinia = gp && gp.$pinia;
            var appStore = pinia && pinia._s && pinia._s.get && pinia._s.get('app');
            
            if (!appStore) return false;
            
            var groupInfoRes = appStore.groupInfoRes;
            var groupInfo = groupInfoRes ? groupInfoRes.groupInfo : null;
            
            if (groupInfo && groupInfo.groupId && appStore.setCurrentSession) {
                appStore.setCurrentSession({
                    id: groupInfo.groupCloudId || groupInfo.teamId,
                    scene: 'team',
                    to: groupInfo.groupCloudId || groupInfo.teamId,
                    groupId: groupInfo.groupId,
                    groupAccount: groupInfo.groupAccount,
                    groupCloudId: groupInfo.groupCloudId,
                    name: groupInfo.name || groupInfo.groupName
                });
                return true;
            }
            return false;
        } catch(e) {
            return false;
        }
    }
    
    try {
        result.groupName = getGroupName();
        
        // IMPORTANT: Set session context before mute operation
        ensureSessionContext();
        
        // STEP 1: Check if mute switch is already visible
        var muteSwitch = findMuteSwitch();
        
        if (!muteSwitch) {
            // STEP 2: Find and click header dropdown (... button)
            var dropdowns = document.querySelectorAll('.el-dropdown');
            var headerDropdown = null;
            
            for (var d = 0; d < dropdowns.length; d++) {
                var dd = dropdowns[d];
                var rect = dd.getBoundingClientRect();
                if (rect.width > 0 && rect.height > 0 && rect.y < 150 && rect.x > 400) {
                    headerDropdown = dd;
                    break;
                }
            }
            
            if (!headerDropdown) {
                result.message = '\u65e0\u6cd5\u627e\u5230\u4e0b\u62c9\u83dc\u5355\u6309\u94ae';
                return JSON.stringify(result);
            }
            
            headerDropdown.click();
            await sleep(800);
            
            // STEP 3: Find and click View Group Card using correct selector
            var menuItems = document.querySelectorAll('.el-dropdown-menu__item');
            var viewCardEl = null;
            
            for (var m = 0; m < menuItems.length; m++) {
                var item = menuItems[m];
                var text = (item.innerText || item.textContent || '').trim();
                if (text.indexOf(viewCardText) >= 0) {
                    viewCardEl = item;
                    break;
                }
            }
            
            if (!viewCardEl) {
                result.message = '\u65e0\u6cd5\u627e\u5230\u67e5\u770b\u7fa4\u540d\u7247\u83dc\u5355\u9879';
                return JSON.stringify(result);
            }
            
            viewCardEl.click();
            await sleep(2000);
            
            // CRITICAL: Wait for groupInfoRes to be populated with valid groupId before proceeding
            var groupIdReady = false;
            for (var gRetry = 0; gRetry < 15; gRetry++) {
                try {
                    var app2 = document.querySelector('#app');
                    var gp2 = app2 && app2.__vue_app__ && app2.__vue_app__.config && app2.__vue_app__.config.globalProperties;
                    var pinia2 = gp2 && gp2.$pinia;
                    var appStore2 = pinia2 && pinia2._s && pinia2._s.get && pinia2._s.get('app');
                    var gi2 = appStore2 && appStore2.groupInfoRes && appStore2.groupInfoRes.groupInfo;
                    if (gi2 && gi2.groupId && gi2.groupId > 0) {
                        groupIdReady = true;
                        result.groupId = gi2.groupId;
                        break;
                    }
                } catch(e2) {}
                await sleep(500);
            }
            
            if (!groupIdReady) {
                result.message = 'GroupId\u672a\u5c31\u7eea\uff0c\u8bf7\u7a0d\u540e\u91cd\u8bd5';
                return JSON.stringify(result);
            }
            
            // STEP 4: Find and click Settings button in sidebar
            var allElements = document.querySelectorAll('*');
            var settingsEl = null;
            
            for (var e = 0; e < allElements.length; e++) {
                var el = allElements[e];
                var text = (el.innerText || el.textContent || '').trim();
                if (text === settingsText) {
                    var rect = el.getBoundingClientRect();
                    if (rect.width > 50 && rect.height > 20 && rect.height < 60 && rect.x < 400 && rect.x > 150) {
                        settingsEl = el;
                        break;
                    }
                }
            }
            
            if (!settingsEl) {
                result.message = '\u65e0\u6cd5\u627e\u5230\u8bbe\u7f6e\u6309\u94ae';
                return JSON.stringify(result);
            }
            
            settingsEl.click();
            await sleep(1000);
            
            // STEP 5: Wait for mute switch to appear
            for (var retry = 0; retry < 10; retry++) {
                muteSwitch = findMuteSwitch();
                if (muteSwitch) break;
                await sleep(300);
            }
        }
        
        if (!muteSwitch) {
            result.message = '\u65e0\u6cd5\u627e\u5230\u7981\u8a00\u5f00\u5173';
            return JSON.stringify(result);
        }
        
        result.panelOpen = true;
        
        // STEP 6: Check current state and click to ENABLE mute
        var isCurrentlyOn = muteSwitch.classList.contains('is-checked') || 
                           muteSwitch.getAttribute('aria-checked') === 'true' ||
                           (muteSwitch.querySelector('.is-checked') !== null);
        
        if (!isCurrentlyOn) {
            muteSwitch.click();
            result.clicked = true;
            await sleep(1500);
            
            var isNowOn = muteSwitch.classList.contains('is-checked') || 
                         muteSwitch.getAttribute('aria-checked') === 'true' ||
                         (muteSwitch.querySelector('.is-checked') !== null);
            
            result.isNowMuted = isNowOn;
            result.success = isNowOn;
            result.message = isNowOn ? '\u5168\u5458\u7981\u8a00\u5df2\u5f00\u542f' : '\u70b9\u51fb\u5931\u8d25\uff0c\u8bf7\u91cd\u8bd5';
        } else {
            result.success = true;
            result.isNowMuted = true;
            result.message = '\u5168\u5458\u7981\u8a00\u5df2\u5904\u4e8e\u5f00\u542f\u72b6\u6001';
        }
    } catch(e) {
        result.message = '\u9519\u8bef: ' + e.message;
    }
    return JSON.stringify(result);
})();";
                
                var clickResponse = await ExecuteScriptWithResultAsync(clickScript, true);
                Log($"DOM点击禁言结果: {clickResponse}");
                
                if (!string.IsNullOrEmpty(clickResponse))
                {
                    var clickMessage = ExtractJsonField(clickResponse, "message");
                    var clickGroupName = ExtractJsonField(clickResponse, "groupName");
                    
                    // Check if success
                    if (clickResponse.Contains("\"success\":true"))
                    {
                        return (true, clickGroupName, clickMessage ?? "全体禁言已开启");
                    }
                    
                    // Return the specific error message from the auto-open process
                    if (!string.IsNullOrEmpty(clickMessage))
                    {
                        return (false, clickGroupName, clickMessage);
                    }
                }
                
                // If DOM click didn't work, fall back to API method
                Log("DOM点击方式未成功，尝试API方式...");
                
                var script = $@"
(async function() {{
    function sleep(ms) {{ return new Promise(function(r) {{ setTimeout(r, ms); }}); }}
    async function callUpdateTeamMuteTypeWithRetry(teamId, mute) {{
        var last = null;
        // muteType: 0=unmute all, 2=mute all
        var muteType = mute ? 2 : 0;
        var delays = [0, 1000, 3000, 6000, 10000];
        for (var attempt = 0; attempt < delays.length; attempt++) {{
            if (attempt > 0) await sleep(delays[attempt]);
            last = await new Promise(function(resolve) {{
                try {{
                    if (!window.nim || typeof window.nim.updateTeam !== 'function') {{
                        resolve({{ success: false, error: 'updateTeam not available' }});
                        return;
                    }}
                    window.nim.updateTeam({{
                        teamId: String(teamId),
                        muteType: muteType,
                        done: function(err, obj) {{
                            if (err) resolve({{ success: false, error: err.message || String(err), code: err.code || null }});
                            else resolve({{ success: true }});
                        }}
                    }});
                }} catch(e) {{
                    resolve({{ success: false, error: e.message }});
                }}
                setTimeout(function() {{ resolve({{ success: false, error: 'Timeout' }}); }}, 8000);
            }});
            if (last && last.success) return {{ success: true, attempts: attempt + 1, via: 'updateTeam' }};
            var isRate = last && (last.code === 416 || (last.error && last.error.indexOf('频率') >= 0));
            if (!isRate) return last;
        }}
        return last || {{ success: false, error: 'Unknown error' }};
    }}
    async function callMuteTeamAllWithRetry(teamId, mute) {{
        var last = null;
        // progressive backoff for rate-limit (416)
        // progressive backoff for rate-limit (416)
        var delays = [0, 1000, 3000, 6000, 10000];
        for (var attempt = 0; attempt < delays.length; attempt++) {{
            if (attempt > 0) await sleep(delays[attempt]);
            last = await new Promise(function(resolve) {{
                window.nim.muteTeamAll({{
                    teamId: String(teamId),
                    mute: !!mute,
                    done: function(err, obj) {{
                        if (err) resolve({{ success: false, error: err.message || String(err), code: err.code || null }});
                        else resolve({{ success: true }});
                    }}
                }});
                setTimeout(function() {{ resolve({{ success: false, error: 'Timeout' }}); }}, 8000);
            }});
            if (last && last.success) return {{ success: true, attempts: attempt + 1 }};
            var isRate = last && (last.code === 416 || (last.error && last.error.indexOf('频率') >= 0));
            if (!isRate) return last;
        }}
        // Fallback: updateTeam(muteType) may be routed differently and sometimes bypasses muteTeamAll throttling.
        try {{
            var fb = await callUpdateTeamMuteTypeWithRetry(teamId, mute);
            if (fb && fb.success) return fb;
        }} catch(e) {{}}
        return last || {{ success: false, error: 'Unknown error' }};
    }}

    var result = {{ success: false, message: '', teamId: null, groupId: 0, groupName: null, groupAccount: null, meRole: null, meIsMute: null, error: null, code: null, attempts: 0, confirmedMute: null, teamMuteRaw: null }};
    var targetGroupAccount = {groupAccountSearch};
    
    try {{
        // Prefer Pinia currentSession (most accurate) -> groupList by groupAccount -> URL fallback -> getTeams fallback
        var teamId = null;
        var groupId = 0;
        var groupName = null;
        var groupAccount = null;

        var app = document.querySelector('#app');
        var gp = app && app.__vue_app__ && app.__vue_app__.config && app.__vue_app__.config.globalProperties;
        var pinia = gp && gp.$pinia;
        var appStore = pinia && pinia._s && pinia._s.get && pinia._s.get('app');
        var cacheStore = pinia && pinia._s && pinia._s.get && pinia._s.get('cache');

        function isTeamMuted(team) {{
            if (!team) return null;
            try {{
                // Prefer muteType if present: 2=mute all, 0=unmute all
                if (team.muteType !== undefined && team.muteType !== null) {{
                    return Number(team.muteType) === 2;
                }}
                if (team.muteAll !== undefined && team.muteAll !== null) {{
                    return (team.muteAll === true || team.muteAll === 1 || team.muteAll === '1');
                }}
                var v = team.mute;
                return (v === true || v === 1 || v === '1');
            }} catch(e) {{
                return null;
            }}
        }}

        function teamMuteSnapshot(team) {{
            try {{
                return JSON.stringify({{ mute: team.mute, muteAll: team.muteAll, muteType: team.muteType }}).substring(0, 200);
            }} catch(e) {{
                return null;
            }}
        }}

        async function refreshGroupInfo(gid) {{
            try {{
                if (!gid || Number(gid) <= 0) return;
                if (!cacheStore || typeof cacheStore.getGroupInfo !== 'function') return;
                // Try multiple calling conventions
                try {{ await Promise.resolve(cacheStore.getGroupInfo(Number(gid))); }} catch(e) {{}}
                try {{ await Promise.resolve(cacheStore.getGroupInfo({{ groupId: Number(gid) }})); }} catch(e) {{}}
            }} catch(e) {{}}
        }}

        // current session
        try {{
            var s = appStore ? (appStore.currentSession || appStore.currSession) : null;
            if (s && (s.scene === 'team' || s.sessionType === 'team') && s.to) {{
                teamId = String(s.to);
                if (s.group) {{
                    groupId = Number(s.group.groupId || groupId || 0);
                    groupName = s.group.groupName || s.group.name || groupName;
                    groupAccount = s.group.groupAccount || groupAccount;
                    if (s.group.me) {{
                        result.meRole = s.group.me.role || result.meRole;
                        if (s.group.me.isMute !== undefined) result.meIsMute = s.group.me.isMute;
                    }}
                }}
            }}
        }} catch(e) {{}}

        // groupList search by groupAccount (if provided)
        if (targetGroupAccount && appStore && appStore.groupList) {{
            try {{
                var all = [];
                if (Array.isArray(appStore.groupList.owner)) all = all.concat(appStore.groupList.owner);
                if (Array.isArray(appStore.groupList.member)) all = all.concat(appStore.groupList.member);
                var found = all.find(function(g) {{ return String(g.groupAccount || '') === String(targetGroupAccount); }});
                if (found) {{
                    teamId = String(found.groupCloudId || found.teamId || found.nimGroupId || teamId);
                    groupId = Number(found.groupId || groupId || 0);
                    groupName = found.groupName || found.name || groupName;
                    groupAccount = found.groupAccount || groupAccount;
                    if (found.me) {{
                        result.meRole = found.me.role || result.meRole;
                        if (found.me.isMute !== undefined) result.meIsMute = found.me.isMute;
                    }}
                }}
            }} catch(e) {{}}
        }}

        // URL fallback
        if (!teamId) {{
            try {{
                var urlMatch = window.location.href.match(/team-(\d+)/);
                teamId = urlMatch ? urlMatch[1] : null;
            }} catch(e) {{}}
        }}
        
        // If no teamId from URL, try to get from groups list
        if (!teamId) {{
            var teams = await new Promise(function(resolve) {{
                window.nim.getTeams({{
                    done: function(err, teams) {{
                        resolve(err ? [] : teams);
                    }}
                }});
                setTimeout(function() {{ resolve([]); }}, 3000);
            }});
            
            if (teams.length > 0) {{
                // Use first owned team
                teamId = teams[0].teamId;
                groupName = teams[0].name;
            }}
        }}
        
        if (!teamId) {{
            result.error = 'No teamId found';
            result.message = 'Please open a group chat first';
            return JSON.stringify(result);
        }}

        result.teamId = String(teamId);
        result.groupId = Number(groupId || 0);
        result.groupName = groupName;
        result.groupAccount = groupAccount;
        
        // CRITICAL: Set current session BEFORE calling mute API
        // WangShangLiao backend extracts groupId from currentSession, so we must set it first
        if (groupId > 0 && appStore && typeof appStore.setCurrentSession === 'function') {{
            try {{
                var groupObj = null;
                if (appStore.groupList) {{
                    var allGroups = [];
                    if (Array.isArray(appStore.groupList.owner)) allGroups = allGroups.concat(appStore.groupList.owner);
                    if (Array.isArray(appStore.groupList.member)) allGroups = allGroups.concat(appStore.groupList.member);
                    groupObj = allGroups.find(function(g) {{ return Number(g.groupId) === groupId; }});
                }}
                if (groupObj) {{
                    var groupSession = {{
                        id: 'team-' + teamId,
                        scene: 'team',
                        to: teamId,
                        group: groupObj
                    }};
                    await Promise.resolve(appStore.setCurrentSession(groupSession));
                    result.debug = (result.debug || '') + '[setCurrentSession:ok]';
                }}
            }} catch(e) {{
                result.debug = (result.debug || '') + '[setCurrentSession:' + e.message + ']';
            }}
            await sleep(300);  // Brief wait for session to propagate
        }}
        
        // Get current team info (refresh name + current mute)
        var teamInfo = await new Promise(function(resolve) {{
            window.nim.getTeam({{
                teamId: teamId,
                done: function(err, team) {{
                    resolve(err ? null : team);
                }}
            }});
            setTimeout(function() {{ resolve(null); }}, 3000);
        }});
        
        if (teamInfo) {{
            groupName = teamInfo.name || groupName;
            result.groupName = groupName;
            
            // Check if already muted
            var isMuted = isTeamMuted(teamInfo);
            result.teamMuteRaw = teamMuteSnapshot(teamInfo);
            if (isMuted) {{
                result.success = true;
                result.message = 'Already muted';
                result.confirmedMute = 'true';
                return JSON.stringify(result);
            }}
        }}
        
        // Call muteTeamAll API (with retry for frequency control)
        var apiResult = await callMuteTeamAllWithRetry(teamId, true);
        result.attempts = apiResult.attempts || 1;
        
        if (apiResult.success) {{
            result.success = true;
            result.message = 'Muted successfully';
            // Confirm mute state by polling getTeam a few times (UI may lag)
            try {{
                for (var i=0;i<6;i++) {{
                    var t = await new Promise(function(resolve) {{
                        window.nim.getTeam({{
                            teamId: teamId,
                            done: function(err, team) {{ resolve(err ? null : team); }}
                        }});
                        setTimeout(function() {{ resolve(null); }}, 1200);
                    }});
                    if (t) {{
                        result.teamMuteRaw = teamMuteSnapshot(t) || result.teamMuteRaw;
                        var mv = isTeamMuted(t);
                        if (mv === true) {{ result.confirmedMute = 'true'; break; }}
                    }}
                    await new Promise(function(r){{ setTimeout(r, 300); }});
                }}
            }} catch(e) {{}}
            // Force refresh group info cache so the UI switch can reflect quickly.
            try {{ await refreshGroupInfo(result.groupId); }} catch(e) {{}}
        }} else {{
            result.error = apiResult.error;
            result.code = apiResult.code;
            // Check for frequency control error
            if (apiResult.code === 416 || (apiResult.error && apiResult.error.indexOf('频率') >= 0)) {{
                result.message = '频率限制(416)，已自动重试 ' + (result.attempts || 1) + ' 次，仍未成功；请稍后再试';
            }} else {{
                result.message = apiResult.error;
            }}
        }}
        
    }} catch(e) {{
        result.error = e.message;
        result.message = 'Exception: ' + e.message;
    }}
    
    return JSON.stringify(result);
}})();";
                
                var response = await ExecuteScriptWithResultAsync(script, true);
                Log($"全体禁言执行结果: {response}");
                
                // Parse result
                var groupName = ExtractJsonField(response, "groupName");
                var message = ExtractJsonField(response, "message") ?? response;
                var error = ExtractJsonField(response, "error");
                var confirmedMute = ExtractJsonField(response, "confirmedMute");
                var attempts = ExtractJsonField(response, "attempts");
                var meRole = ExtractJsonField(response, "meRole");
                var meIsMute = ExtractJsonField(response, "meIsMute");
                var teamMuteRaw = ExtractJsonField(response, "teamMuteRaw");
                
                if (response != null && response.Contains("\"success\":true"))
                {
                    // If we managed to confirm, append it for clarity in UI toast.
                    var msg = message;
                    if (!string.IsNullOrEmpty(confirmedMute))
                    {
                        msg = $"{message} (confirmedMute={confirmedMute})";
                    }
                    if (!string.IsNullOrEmpty(attempts))
                    {
                        msg = $"{msg} (attempts={attempts})";
                    }
                    if (!string.IsNullOrEmpty(meRole))
                    {
                        msg = $"{msg} (meRole={meRole})";
                    }
                    if (!string.IsNullOrEmpty(meIsMute))
                    {
                        msg = $"{msg} (meIsMute={meIsMute})";
                    }
                    if (!string.IsNullOrEmpty(teamMuteRaw))
                    {
                        msg = $"{msg} (teamMuteRaw={teamMuteRaw})";
                    }
                    Log($"全体禁言成功: {groupName}");
                    return (true, groupName, msg);
                }
                else
                {
                    // Return error message if available, otherwise return message
                    var errorMsg = !string.IsNullOrEmpty(error) ? error : message;
                    Log($"全体禁言失败: {errorMsg}");
                    return (false, groupName, errorMsg);
                }
            }
            catch (Exception ex)
            {
                Log($"全体禁言异常: {ex.Message}");
                return (false, null, ex.Message);
            }
        }

        /// <summary>
        /// Mute all members by explicit teamId/groupCloudId (no need to open the group chat UI)
        /// 通过 groupCloudId(teamId) 执行全体禁言（推荐用于集成）
        /// </summary>
        public async Task<(bool Success, string GroupName, string Message)> MuteAllByGroupCloudIdAsync(string groupCloudId)
        {
            if (!IsConnected || Mode != ConnectionMode.CDP)
            {
                Log("未连接或非CDP模式，无法执行全体禁言(groupCloudId)");
                return (false, null, "未连接或非CDP模式");
            }

            if (string.IsNullOrWhiteSpace(groupCloudId))
            {
                return (false, null, "groupCloudId 为空");
            }

            try
            {
                Log($"开始执行全体禁言(groupCloudId)... teamId={groupCloudId}");
                var teamIdJson = ToJsonString(groupCloudId);
                var script = $@"
(async function() {{
    var result = {{ success: false, message: '', teamId: null, groupName: null, error: null, code: null }};
    try {{
        if (!window.nim || typeof window.nim.muteTeamAll !== 'function') {{
            result.error = 'window.nim.muteTeamAll not available';
            result.message = result.error;
            return JSON.stringify(result);
        }}

        var teamId = String({teamIdJson});
        result.teamId = teamId;

        // Resolve group name (best-effort)
        try {{
            var teamInfo = await new Promise(function(resolve) {{
                window.nim.getTeam({{
                    teamId: teamId,
                    done: function(err, team) {{ resolve(err ? null : team); }}
                }});
                setTimeout(function() {{ resolve(null); }}, 3000);
            }});
            if (teamInfo && teamInfo.name) result.groupName = teamInfo.name;
        }} catch(e) {{}}

        var apiResult = await new Promise(function(resolve) {{
            window.nim.muteTeamAll({{
                teamId: teamId,
                mute: true,
                done: function(err, obj) {{
                    if (err) resolve({{ success: false, error: err.message || String(err), code: err.code || null }});
                    else resolve({{ success: true }});
                }}
            }});
            setTimeout(function() {{ resolve({{ success: false, error: 'Timeout' }}); }}, 8000);
        }});

        if (apiResult.success) {{
            result.success = true;
            result.message = 'Muted successfully';
        }} else {{
            result.error = apiResult.error;
            result.code = apiResult.code;
            result.message = apiResult.error;
        }}
    }} catch(e) {{
        result.error = e.message;
        result.message = 'Exception: ' + e.message;
    }}
    return JSON.stringify(result);
}})();";

                var response = await ExecuteScriptWithResultAsync(script, true);
                Log($"全体禁言(groupCloudId)结果: {response}");

                var success = !string.IsNullOrEmpty(response) && response.Contains("\"success\":true");
                var groupName = ExtractJsonField(response, "groupName");
                var message = ExtractJsonField(response, "message") ?? response;
                var error = ExtractJsonField(response, "error");

                return success
                    ? (true, groupName, message)
                    : (false, groupName, !string.IsNullOrEmpty(error) ? error : message);
            }
            catch (Exception ex)
            {
                Log($"全体禁言(groupCloudId)异常: {ex.Message}");
                return (false, null, ex.Message);
            }
        }
        
        /// <summary>
        /// Unmute all members in a group chat by group account number
        /// 全体解禁 - 使用 NIM SDK updateTeam 方法设置 muteType=0
        /// </summary>
        /// <param name="groupAccount">群号（如 3962369093），为空则使用当前会话的群</param>
        /// <returns>(success, groupName, message)</returns>
        public async Task<(bool Success, string GroupName, string Message)> UnmuteAllAsync(string groupAccount = null)
        {
            if (!IsConnected || Mode != ConnectionMode.CDP)
            {
                Log("未连接或非CDP模式，无法执行全体解禁");
                return (false, null, "未连接或非CDP模式");
            }
            
            try
            {
                Log($"开始执行全体解禁... 群号: {groupAccount ?? "当前会话"}");
                
                // DOM AUTOMATION: Click the mute switch directly (most reliable method)
                // This ensures both server and UI state are properly synchronized
                var groupAccountSearch = string.IsNullOrEmpty(groupAccount) ? "null" : ToJsonString(groupAccount);
                
                // FULL AUTOMATION v3: Dropdown -> View Card -> Settings -> Unmute switch
                var clickScript = @"
(async function() {
    var result = { success: false, groupName: null, message: '', clicked: false, isNowMuted: null, panelOpen: false, method: 'dom' };
    function sleep(ms) { return new Promise(function(r) { setTimeout(r, ms); }); }
    
    var muteText = '\u5168\u5458\u7981\u8a00';  // 全员禁言
    var viewCardText = '\u67e5\u770b\u7fa4\u540d\u7247';  // 查看群名片
    var settingsText = '\u8bbe\u7f6e';  // 设置
    
    // Helper to find mute switch by checking parent/grandparent/container context
    function findMuteSwitch() {
        var switches = document.querySelectorAll('[class*=""switch""]');
        for (var i = 0; i < switches.length; i++) {
            var sw = switches[i];
            var rect = sw.getBoundingClientRect();
            if (rect.width === 0 || rect.height === 0) continue;
            
            var parent = sw.parentElement;
            var grandparent = parent ? parent.parentElement : null;
            var container = grandparent ? grandparent.parentElement : null;
            var context = (parent ? parent.innerText : '') + ' ' + (grandparent ? grandparent.innerText : '') + ' ' + (container ? container.innerText : '');
            
            if (context.indexOf(muteText) >= 0) {
                return sw;
            }
        }
        return null;
    }
    
    // Helper to get current group name from Pinia
    function getGroupName() {
        try {
            var app = document.querySelector('#app');
            var gp = app && app.__vue_app__ && app.__vue_app__.config && app.__vue_app__.config.globalProperties;
            var pinia = gp && gp.$pinia;
            var appStore = pinia && pinia._s && pinia._s.get && pinia._s.get('app');
            var groupInfoRes = appStore ? appStore.groupInfoRes : null;
            if (groupInfoRes && groupInfoRes.groupInfo) {
                return groupInfoRes.groupInfo.name || groupInfoRes.groupInfo.groupName;
            }
        } catch(e) {}
        return null;
    }
    
    // CRITICAL: Ensure session context is set for backend API (fixes GroupId > 0 error)
    function ensureSessionContext() {
        try {
            var app = document.querySelector('#app');
            var gp = app && app.__vue_app__ && app.__vue_app__.config && app.__vue_app__.config.globalProperties;
            var pinia = gp && gp.$pinia;
            var appStore = pinia && pinia._s && pinia._s.get && pinia._s.get('app');
            
            if (!appStore) return false;
            
            var groupInfoRes = appStore.groupInfoRes;
            var groupInfo = groupInfoRes ? groupInfoRes.groupInfo : null;
            
            if (groupInfo && groupInfo.groupId && appStore.setCurrentSession) {
                appStore.setCurrentSession({
                    id: groupInfo.groupCloudId || groupInfo.teamId,
                    scene: 'team',
                    to: groupInfo.groupCloudId || groupInfo.teamId,
                    groupId: groupInfo.groupId,
                    groupAccount: groupInfo.groupAccount,
                    groupCloudId: groupInfo.groupCloudId,
                    name: groupInfo.name || groupInfo.groupName
                });
                return true;
            }
            return false;
        } catch(e) {
            return false;
        }
    }
    
    try {
        result.groupName = getGroupName();
        
        // IMPORTANT: Set session context before unmute operation
        ensureSessionContext();
        
        // STEP 1: Check if mute switch is already visible
        var muteSwitch = findMuteSwitch();
        
        if (!muteSwitch) {
            // STEP 2: Find and click header dropdown (... button)
            var dropdowns = document.querySelectorAll('.el-dropdown');
            var headerDropdown = null;
            
            for (var d = 0; d < dropdowns.length; d++) {
                var dd = dropdowns[d];
                var rect = dd.getBoundingClientRect();
                if (rect.width > 0 && rect.height > 0 && rect.y < 150 && rect.x > 400) {
                    headerDropdown = dd;
                    break;
                }
            }
            
            if (!headerDropdown) {
                result.message = '\u65e0\u6cd5\u627e\u5230\u4e0b\u62c9\u83dc\u5355\u6309\u94ae';
                return JSON.stringify(result);
            }
            
            headerDropdown.click();
            await sleep(800);
            
            // STEP 3: Find and click View Group Card using correct selector
            var menuItems = document.querySelectorAll('.el-dropdown-menu__item');
            var viewCardEl = null;
            
            for (var m = 0; m < menuItems.length; m++) {
                var item = menuItems[m];
                var text = (item.innerText || item.textContent || '').trim();
                if (text.indexOf(viewCardText) >= 0) {
                    viewCardEl = item;
                    break;
                }
            }
            
            if (!viewCardEl) {
                result.message = '\u65e0\u6cd5\u627e\u5230\u67e5\u770b\u7fa4\u540d\u7247\u83dc\u5355\u9879';
                return JSON.stringify(result);
            }
            
            viewCardEl.click();
            await sleep(2000);
            
            // CRITICAL: Wait for groupInfoRes to be populated with valid groupId before proceeding
            var groupIdReady = false;
            for (var gRetry = 0; gRetry < 15; gRetry++) {
                try {
                    var app2 = document.querySelector('#app');
                    var gp2 = app2 && app2.__vue_app__ && app2.__vue_app__.config && app2.__vue_app__.config.globalProperties;
                    var pinia2 = gp2 && gp2.$pinia;
                    var appStore2 = pinia2 && pinia2._s && pinia2._s.get && pinia2._s.get('app');
                    var gi2 = appStore2 && appStore2.groupInfoRes && appStore2.groupInfoRes.groupInfo;
                    if (gi2 && gi2.groupId && gi2.groupId > 0) {
                        groupIdReady = true;
                        result.groupId = gi2.groupId;
                        break;
                    }
                } catch(e2) {}
                await sleep(500);
            }
            
            if (!groupIdReady) {
                result.message = 'GroupId\u672a\u5c31\u7eea\uff0c\u8bf7\u7a0d\u540e\u91cd\u8bd5';
                return JSON.stringify(result);
            }
            
            // STEP 4: Find and click Settings button in sidebar
            var allElements = document.querySelectorAll('*');
            var settingsEl = null;
            
            for (var e = 0; e < allElements.length; e++) {
                var el = allElements[e];
                var text = (el.innerText || el.textContent || '').trim();
                if (text === settingsText) {
                    var rect = el.getBoundingClientRect();
                    if (rect.width > 50 && rect.height > 20 && rect.height < 60 && rect.x < 400 && rect.x > 150) {
                        settingsEl = el;
                        break;
                    }
                }
            }
            
            if (!settingsEl) {
                result.message = '\u65e0\u6cd5\u627e\u5230\u8bbe\u7f6e\u6309\u94ae';
                return JSON.stringify(result);
            }
            
            settingsEl.click();
            await sleep(1000);
            
            // STEP 5: Wait for mute switch to appear
            for (var retry = 0; retry < 10; retry++) {
                muteSwitch = findMuteSwitch();
                if (muteSwitch) break;
                await sleep(300);
            }
        }
        
        if (!muteSwitch) {
            result.message = '\u65e0\u6cd5\u627e\u5230\u7981\u8a00\u5f00\u5173';
            return JSON.stringify(result);
        }
        
        result.panelOpen = true;
        
        // STEP 6: Check current state and click to DISABLE mute (unmute)
        var isCurrentlyOn = muteSwitch.classList.contains('is-checked') || 
                           muteSwitch.getAttribute('aria-checked') === 'true' ||
                           (muteSwitch.querySelector('.is-checked') !== null);
        
        if (isCurrentlyOn) {
            muteSwitch.click();
            result.clicked = true;
            await sleep(1500);
            
            var isNowOn = muteSwitch.classList.contains('is-checked') || 
                         muteSwitch.getAttribute('aria-checked') === 'true' ||
                         (muteSwitch.querySelector('.is-checked') !== null);
            
            result.isNowMuted = isNowOn;
            result.success = !isNowOn;  // Success if now OFF
            result.message = !isNowOn ? '\u5168\u5458\u7981\u8a00\u5df2\u89e3\u9664' : '\u70b9\u51fb\u5931\u8d25\uff0c\u8bf7\u91cd\u8bd5';
        } else {
            result.success = true;
            result.isNowMuted = false;
            result.message = '\u5168\u5458\u7981\u8a00\u5df2\u5904\u4e8e\u5173\u95ed\u72b6\u6001';
        }
    } catch(e) {
        result.message = '\u9519\u8bef: ' + e.message;
    }
    return JSON.stringify(result);
})();";
                
                var clickResponse = await ExecuteScriptWithResultAsync(clickScript, true);
                Log($"DOM点击解禁结果: {clickResponse}");
                
                if (!string.IsNullOrEmpty(clickResponse))
                {
                    var clickMessage = ExtractJsonField(clickResponse, "message");
                    var clickGroupName = ExtractJsonField(clickResponse, "groupName");
                    
                    // Check if success
                    if (clickResponse.Contains("\"success\":true"))
                    {
                        return (true, clickGroupName, clickMessage ?? "全员禁言已解除");
                    }
                    
                    // Return the specific error message from the auto-open process
                    if (!string.IsNullOrEmpty(clickMessage))
                    {
                        return (false, clickGroupName, clickMessage);
                    }
                }
                
                // If DOM click didn't work, fall back to API method
                Log("DOM点击方式未成功，尝试API方式...");
                
                // Use muteTeamAll API to unmute group (fallback)
                var script = $@"
(async function() {{
    function sleep(ms) {{ return new Promise(function(r) {{ setTimeout(r, ms); }}); }}
    async function callUpdateTeamMuteTypeWithRetry(teamId, mute) {{
        var last = null;
        var muteType = mute ? 2 : 0;
        var delays = [0, 1000, 3000, 6000, 10000];
        for (var attempt = 0; attempt < delays.length; attempt++) {{
            if (attempt > 0) await sleep(delays[attempt]);
            last = await new Promise(function(resolve) {{
                try {{
                    if (!window.nim || typeof window.nim.updateTeam !== 'function') {{
                        resolve({{ success: false, error: 'updateTeam not available' }});
                        return;
                    }}
                    window.nim.updateTeam({{
                        teamId: String(teamId),
                        muteType: muteType,
                        done: function(err, obj) {{
                            if (err) resolve({{ success: false, error: err.message || String(err), code: err.code || null }});
                            else resolve({{ success: true }});
                        }}
                    }});
                }} catch(e) {{
                    resolve({{ success: false, error: e.message }});
                }}
                setTimeout(function() {{ resolve({{ success: false, error: 'Timeout' }}); }}, 8000);
            }});
            if (last && last.success) return {{ success: true, attempts: attempt + 1, via: 'updateTeam' }};
            var isRate = last && (last.code === 416 || (last.error && last.error.indexOf('频率') >= 0));
            if (!isRate) return last;
        }}
        return last || {{ success: false, error: 'Unknown error' }};
    }}
    async function callMuteTeamAllWithRetry(teamId, mute) {{
        var last = null;
        var delays = [0, 1000, 3000, 6000, 10000];
        for (var attempt = 0; attempt < delays.length; attempt++) {{
            if (attempt > 0) await sleep(delays[attempt]);
            last = await new Promise(function(resolve) {{
                window.nim.muteTeamAll({{
                    teamId: String(teamId),
                    mute: !!mute,
                    done: function(err, obj) {{
                        if (err) resolve({{ success: false, error: err.message || String(err), code: err.code || null }});
                        else resolve({{ success: true }});
                    }}
                }});
                setTimeout(function() {{ resolve({{ success: false, error: 'Timeout' }}); }}, 8000);
            }});
            if (last && last.success) return {{ success: true, attempts: attempt + 1 }};
            var isRate = last && (last.code === 416 || (last.error && last.error.indexOf('频率') >= 0));
            if (!isRate) return last;
        }}
        try {{
            var fb = await callUpdateTeamMuteTypeWithRetry(teamId, mute);
            if (fb && fb.success) return fb;
        }} catch(e) {{}}
        return last || {{ success: false, error: 'Unknown error' }};
    }}

    var result = {{ success: false, message: '', teamId: null, groupId: 0, groupName: null, groupAccount: null, meRole: null, meIsMute: null, error: null, code: null, attempts: 0, confirmedMute: null, teamMuteRaw: null }};
    var targetGroupAccount = {groupAccountSearch};
    
    try {{
        // Prefer Pinia currentSession (most accurate) -> groupList by groupAccount -> URL fallback -> getTeams fallback
        var teamId = null;
        var groupId = 0;
        var groupName = null;
        var groupAccount = null;

        var app = document.querySelector('#app');
        var gp = app && app.__vue_app__ && app.__vue_app__.config && app.__vue_app__.config.globalProperties;
        var pinia = gp && gp.$pinia;
        var appStore = pinia && pinia._s && pinia._s.get && pinia._s.get('app');
        var cacheStore = pinia && pinia._s && pinia._s.get && pinia._s.get('cache');

        function isTeamMuted(team) {{
            if (!team) return null;
            try {{
                if (team.muteType !== undefined && team.muteType !== null) {{
                    return Number(team.muteType) === 2;
                }}
                if (team.muteAll !== undefined && team.muteAll !== null) {{
                    return (team.muteAll === true || team.muteAll === 1 || team.muteAll === '1');
                }}
                var v = team.mute;
                return (v === true || v === 1 || v === '1');
            }} catch(e) {{
                return null;
            }}
        }}

        function teamMuteSnapshot(team) {{
            try {{
                return JSON.stringify({{ mute: team.mute, muteAll: team.muteAll, muteType: team.muteType }}).substring(0, 200);
            }} catch(e) {{
                return null;
            }}
        }}

        async function refreshGroupInfo(gid) {{
            try {{
                if (!gid || Number(gid) <= 0) return;
                if (!cacheStore || typeof cacheStore.getGroupInfo !== 'function') return;
                try {{ await Promise.resolve(cacheStore.getGroupInfo(Number(gid))); }} catch(e) {{}}
                try {{ await Promise.resolve(cacheStore.getGroupInfo({{ groupId: Number(gid) }})); }} catch(e) {{}}
            }} catch(e) {{}}
        }}

        // current session
        try {{
            var s = appStore ? (appStore.currentSession || appStore.currSession) : null;
            if (s && (s.scene === 'team' || s.sessionType === 'team') && s.to) {{
                teamId = String(s.to);
                if (s.group) {{
                    groupId = Number(s.group.groupId || groupId || 0);
                    groupName = s.group.groupName || s.group.name || groupName;
                    groupAccount = s.group.groupAccount || groupAccount;
                    if (s.group.me) {{
                        result.meRole = s.group.me.role || result.meRole;
                        if (s.group.me.isMute !== undefined) result.meIsMute = s.group.me.isMute;
                    }}
                }}
            }}
        }} catch(e) {{}}

        // groupList search by groupAccount (if provided)
        if (targetGroupAccount && appStore && appStore.groupList) {{
            try {{
                var all = [];
                if (Array.isArray(appStore.groupList.owner)) all = all.concat(appStore.groupList.owner);
                if (Array.isArray(appStore.groupList.member)) all = all.concat(appStore.groupList.member);
                var found = all.find(function(g) {{ return String(g.groupAccount || '') === String(targetGroupAccount); }});
                if (found) {{
                    teamId = String(found.groupCloudId || found.teamId || found.nimGroupId || teamId);
                    groupId = Number(found.groupId || groupId || 0);
                    groupName = found.groupName || found.name || groupName;
                    groupAccount = found.groupAccount || groupAccount;
                    if (found.me) {{
                        result.meRole = found.me.role || result.meRole;
                        if (found.me.isMute !== undefined) result.meIsMute = found.me.isMute;
                    }}
                }}
            }} catch(e) {{}}
        }}

        // URL fallback
        if (!teamId) {{
            try {{
                var urlMatch = window.location.href.match(/team-(\d+)/);
                teamId = urlMatch ? urlMatch[1] : null;
            }} catch(e) {{}}
        }}
        
        // If no teamId from URL, try to get from groups list
        if (!teamId) {{
            var teams = await new Promise(function(resolve) {{
                window.nim.getTeams({{
                    done: function(err, teams) {{
                        resolve(err ? [] : teams);
                    }}
                }});
                setTimeout(function() {{ resolve([]); }}, 3000);
            }});
            
            if (teams.length > 0) {{
                teamId = teams[0].teamId;
                groupName = teams[0].name;
            }}
        }}
        
        if (!teamId) {{
            result.error = 'No teamId found';
            result.message = 'Please open a group chat first';
            return JSON.stringify(result);
        }}

        result.teamId = String(teamId);
        result.groupId = Number(groupId || 0);
        result.groupName = groupName;
        result.groupAccount = groupAccount;
        
        // CRITICAL: Set current session BEFORE calling unmute API
        // WangShangLiao backend extracts groupId from currentSession, so we must set it first
        if (groupId > 0 && appStore && typeof appStore.setCurrentSession === 'function') {{
            try {{
                var groupObj = null;
                if (appStore.groupList) {{
                    var allGroups = [];
                    if (Array.isArray(appStore.groupList.owner)) allGroups = allGroups.concat(appStore.groupList.owner);
                    if (Array.isArray(appStore.groupList.member)) allGroups = allGroups.concat(appStore.groupList.member);
                    groupObj = allGroups.find(function(g) {{ return Number(g.groupId) === groupId; }});
                }}
                if (groupObj) {{
                    var groupSession = {{
                        id: 'team-' + teamId,
                        scene: 'team',
                        to: teamId,
                        group: groupObj
                    }};
                    await Promise.resolve(appStore.setCurrentSession(groupSession));
                    result.debug = (result.debug || '') + '[setCurrentSession:ok]';
                }}
            }} catch(e) {{
                result.debug = (result.debug || '') + '[setCurrentSession:' + e.message + ']';
            }}
            await sleep(300);  // Brief wait for session to propagate
        }}
        
        // Get current team info
        var teamInfo = await new Promise(function(resolve) {{
            window.nim.getTeam({{
                teamId: teamId,
                done: function(err, team) {{
                    resolve(err ? null : team);
                }}
            }});
            setTimeout(function() {{ resolve(null); }}, 3000);
        }});
        
        if (teamInfo) {{
            groupName = teamInfo.name || groupName;
            result.groupName = groupName;
            
            // Check if already unmuted
            var isMuted = isTeamMuted(teamInfo);
            result.teamMuteRaw = teamMuteSnapshot(teamInfo);
            if (!isMuted) {{
                result.success = true;
                result.message = 'Already unmuted';
                result.confirmedMute = 'false';
                return JSON.stringify(result);
            }}
        }}
        
        // Call muteTeamAll API with mute=false (retry on 416)
        var apiResult = await callMuteTeamAllWithRetry(teamId, false);
        result.attempts = apiResult.attempts || 1;
        
        if (apiResult.success) {{
            result.success = true;
            result.message = 'Unmuted successfully';
            // Confirm mute state by polling getTeam a few times
            try {{
                for (var i=0;i<6;i++) {{
                    var t = await new Promise(function(resolve) {{
                        window.nim.getTeam({{
                            teamId: teamId,
                            done: function(err, team) {{ resolve(err ? null : team); }}
                        }});
                        setTimeout(function() {{ resolve(null); }}, 1200);
                    }});
                    if (t) {{
                        result.teamMuteRaw = teamMuteSnapshot(t) || result.teamMuteRaw;
                        var mv = isTeamMuted(t);
                        if (mv === false) {{ result.confirmedMute = 'false'; break; }}
                    }}
                    await new Promise(function(r){{ setTimeout(r, 300); }});
                }}
            }} catch(e) {{}}
            // Force refresh group info cache so the UI switch can reflect quickly.
            try {{ await refreshGroupInfo(result.groupId); }} catch(e) {{}}
        }} else {{
            result.error = apiResult.error;
            result.code = apiResult.code;
            // Check for frequency control error
            if (apiResult.code === 416 || (apiResult.error && apiResult.error.indexOf('频率') >= 0)) {{
                result.message = '频率限制(416)，已自动重试 ' + (result.attempts || 1) + ' 次，仍未成功；请稍后再试';
            }} else {{
                result.message = apiResult.error;
            }}
        }}
        
    }} catch(e) {{
        result.error = e.message;
        result.message = 'Exception: ' + e.message;
    }}
    
    return JSON.stringify(result);
}})();";
                
                var response = await ExecuteScriptWithResultAsync(script, true);
                Log($"全体解禁执行结果: {response}");
                
                // Parse result
                var groupName = ExtractJsonField(response, "groupName");
                var message = ExtractJsonField(response, "message") ?? response;
                var error = ExtractJsonField(response, "error");
                var confirmedMute = ExtractJsonField(response, "confirmedMute");
                var attempts = ExtractJsonField(response, "attempts");
                var meRole = ExtractJsonField(response, "meRole");
                var meIsMute = ExtractJsonField(response, "meIsMute");
                var teamMuteRaw = ExtractJsonField(response, "teamMuteRaw");
                
                if (response != null && response.Contains("\"success\":true"))
                {
                    Log($"全体解禁成功: {groupName}");
                    var msg = message;
                    if (!string.IsNullOrEmpty(confirmedMute))
                    {
                        msg = $"{message} (confirmedMute={confirmedMute})";
                    }
                    if (!string.IsNullOrEmpty(attempts))
                    {
                        msg = $"{msg} (attempts={attempts})";
                    }
                    if (!string.IsNullOrEmpty(meRole))
                    {
                        msg = $"{msg} (meRole={meRole})";
                    }
                    if (!string.IsNullOrEmpty(meIsMute))
                    {
                        msg = $"{msg} (meIsMute={meIsMute})";
                    }
                    if (!string.IsNullOrEmpty(teamMuteRaw))
                    {
                        msg = $"{msg} (teamMuteRaw={teamMuteRaw})";
                    }
                    return (true, groupName, msg);
                }
                else
                {
                    // Return error message if available, otherwise return message
                    var errorMsg = !string.IsNullOrEmpty(error) ? error : message;
                    Log($"全体解禁失败: {errorMsg}");
                    return (false, groupName, errorMsg);
                }
            }
            catch (Exception ex)
            {
                Log($"全体解禁异常: {ex.Message}");
                return (false, null, ex.Message);
            }
        }

        /// <summary>
        /// Unmute all members by explicit teamId/groupCloudId (no need to open the group chat UI)
        /// 通过 groupCloudId(teamId) 执行全体解禁（推荐用于集成）
        /// </summary>
        public async Task<(bool Success, string GroupName, string Message)> UnmuteAllByGroupCloudIdAsync(string groupCloudId)
        {
            if (!IsConnected || Mode != ConnectionMode.CDP)
            {
                Log("未连接或非CDP模式，无法执行全体解禁(groupCloudId)");
                return (false, null, "未连接或非CDP模式");
            }

            if (string.IsNullOrWhiteSpace(groupCloudId))
            {
                return (false, null, "groupCloudId 为空");
            }

            try
            {
                Log($"开始执行全体解禁(groupCloudId)... teamId={groupCloudId}");
                var teamIdJson = ToJsonString(groupCloudId);
                var script = $@"
(async function() {{
    var result = {{ success: false, message: '', teamId: null, groupName: null, error: null, code: null }};
    try {{
        if (!window.nim || typeof window.nim.muteTeamAll !== 'function') {{
            result.error = 'window.nim.muteTeamAll not available';
            result.message = result.error;
            return JSON.stringify(result);
        }}

        var teamId = String({teamIdJson});
        result.teamId = teamId;

        // Resolve group name (best-effort)
        try {{
            var teamInfo = await new Promise(function(resolve) {{
                window.nim.getTeam({{
                    teamId: teamId,
                    done: function(err, team) {{ resolve(err ? null : team); }}
                }});
                setTimeout(function() {{ resolve(null); }}, 3000);
            }});
            if (teamInfo && teamInfo.name) result.groupName = teamInfo.name;
        }} catch(e) {{}}

        var apiResult = await new Promise(function(resolve) {{
            window.nim.muteTeamAll({{
                teamId: teamId,
                mute: false,
                done: function(err, obj) {{
                    if (err) resolve({{ success: false, error: err.message || String(err), code: err.code || null }});
                    else resolve({{ success: true }});
                }}
            }});
            setTimeout(function() {{ resolve({{ success: false, error: 'Timeout' }}); }}, 8000);
        }});

        if (apiResult.success) {{
            result.success = true;
            result.message = 'Unmuted successfully';
        }} else {{
            result.error = apiResult.error;
            result.code = apiResult.code;
            result.message = apiResult.error;
        }}
    }} catch(e) {{
        result.error = e.message;
        result.message = 'Exception: ' + e.message;
    }}
    return JSON.stringify(result);
}})();";

                var response = await ExecuteScriptWithResultAsync(script, true);
                Log($"全体解禁(groupCloudId)结果: {response}");

                var success = !string.IsNullOrEmpty(response) && response.Contains("\"success\":true");
                var groupName = ExtractJsonField(response, "groupName");
                var message = ExtractJsonField(response, "message") ?? response;
                var error = ExtractJsonField(response, "error");

                return success
                    ? (true, groupName, message)
                    : (false, groupName, !string.IsNullOrEmpty(error) ? error : message);
            }
            catch (Exception ex)
            {
                Log($"全体解禁(groupCloudId)异常: {ex.Message}");
                return (false, null, ex.Message);
            }
        }

        /// <summary>
        /// Resolve group identifiers from Pinia store by groupAccount (external group number).
        /// 根据群号(groupAccount，如 3962369093)解析 groupId(数值) 与 groupCloudId(teamId)
        /// </summary>
        public async Task<(bool Success, string GroupName, string GroupAccount, long GroupId, string GroupCloudId, string Message)> ResolveGroupIdentityAsync(string groupAccount)
        {
            if (!IsConnected || Mode != ConnectionMode.CDP)
            {
                return (false, null, groupAccount, 0, null, "未连接或非CDP模式");
            }

            if (string.IsNullOrWhiteSpace(groupAccount))
            {
                return (false, null, null, 0, null, "群号为空");
            }

            try
            {
                var ga = ToJsonString(groupAccount);
                var script = $@"
(async function() {{
    var result = {{ success:false, groupName:null, groupAccount:null, groupId:0, groupCloudId:null, error:null }};
    try {{
        var app = document.querySelector('#app');
        var gp = app && app.__vue_app__ && app.__vue_app__.config && app.__vue_app__.config.globalProperties;
        var pinia = gp && gp.$pinia;
        var appStore = pinia && pinia._s && pinia._s.get && pinia._s.get('app');
        if (!appStore || !appStore.groupList) {{
            result.error = 'Pinia appStore.groupList not available';
            return JSON.stringify(result);
        }}

        var target = String({ga});
        var all = [];
        if (Array.isArray(appStore.groupList.owner)) all = all.concat(appStore.groupList.owner);
        if (Array.isArray(appStore.groupList.member)) all = all.concat(appStore.groupList.member);
        var found = all.find(function(g) {{ return String(g.groupAccount || '') === target; }});

        // fallback: currentSession
        if (!found) {{
            try {{
                var s = appStore.currentSession || appStore.currSession;
                if (s && s.group && String(s.group.groupAccount || '') === target) found = s.group;
            }} catch(e) {{}}
        }}

        if (!found) {{
            result.error = 'Group not found by groupAccount=' + target;
            return JSON.stringify(result);
        }}

        result.success = true;
        result.groupAccount = String(found.groupAccount || target);
        result.groupId = Number(found.groupId || 0);
        result.groupCloudId = String(found.groupCloudId || found.teamId || found.nimGroupId || '');
        result.groupName = found.groupName || found.name || null;
    }} catch(e) {{
        result.error = e.message;
    }}
    return JSON.stringify(result);
}})();";

                var response = await ExecuteScriptWithResultAsync(script, true);
                Log($"解析群标识结果: {response}");

                var ok = !string.IsNullOrEmpty(response) && response.Contains("\"success\":true");
                var name = ExtractJsonField(response, "groupName");
                var acc = ExtractJsonField(response, "groupAccount") ?? groupAccount;
                var cloud = ExtractJsonField(response, "groupCloudId");
                var err = ExtractJsonField(response, "error");

                long gid = 0;
                var gidStr = ExtractJsonField(response, "groupId");
                if (!string.IsNullOrWhiteSpace(gidStr)) long.TryParse(gidStr, out gid);

                return ok
                    ? (true, name, acc, gid, cloud, "OK")
                    : (false, name, acc, gid, cloud, err ?? response);
            }
            catch (Exception ex)
            {
                Log($"解析群标识异常: {ex.Message}");
                return (false, null, groupAccount, 0, null, ex.Message);
            }
        }

        /// <summary>
        /// Get group info via Pinia cacheStore.getGroupInfo using numeric groupId (NOT groupCloudId).
        /// 通过 cacheStore.getGroupInfo 获取群信息（修复 GroupId=0 导致的“无效GetGroupInfo请求”）
        /// </summary>
        public async Task<(bool Success, string GroupName, string Message)> GetGroupInfoByGroupAccountAsync(string groupAccount)
        {
            if (!IsConnected || Mode != ConnectionMode.CDP)
            {
                return (false, null, "未连接或非CDP模式");
            }

            var identity = await ResolveGroupIdentityAsync(groupAccount);
            if (!identity.Success || identity.GroupId <= 0)
            {
                return (false, identity.GroupName, $"解析群失败或 groupId 无效: {identity.GroupId}，{identity.Message}");
            }

            try
            {
                var gid = identity.GroupId.ToString();
                var script = $@"
(async function() {{
    var result = {{ success:false, groupId:{gid}, groupName:null, error:null }};
    try {{
        var app = document.querySelector('#app');
        var gp = app && app.__vue_app__ && app.__vue_app__.config && app.__vue_app__.config.globalProperties;
        var pinia = gp && gp.$pinia;
        var cacheStore = pinia && pinia._s && pinia._s.get && pinia._s.get('cache');
        if (!cacheStore || typeof cacheStore.getGroupInfo !== 'function') {{
            result.error = 'cacheStore.getGroupInfo not available';
            return JSON.stringify(result);
        }}

        // Try multiple calling conventions (the app may accept number or object).
        var info = null;
        try {{ info = await Promise.resolve(cacheStore.getGroupInfo({gid})); }} catch(e) {{}}
        if (!info) {{
            try {{ info = await Promise.resolve(cacheStore.getGroupInfo({{ groupId: {gid} }})); }} catch(e) {{}}
        }}
        if (!info) {{
            try {{ info = await Promise.resolve(cacheStore.getGroupInfo({{ groupId: {gid}, force: true }})); }} catch(e) {{}}
        }}

        if (!info) {{
            result.error = 'getGroupInfo returned null';
            return JSON.stringify(result);
        }}

        // Try to find a human name from returned payload.
        try {{
            result.groupName = info.groupName || info.name || (info.data && (info.data.groupName || info.data.name)) || null;
        }} catch(e) {{}}

        result.success = true;
        // Return a small snippet to avoid huge payloads.
        result.snippet = (function() {{
            try {{ return JSON.stringify(info).substring(0, 1500); }} catch(e) {{ return String(info).substring(0, 1500); }}
        }})();
    }} catch(e) {{
        result.error = e.message;
    }}
    return JSON.stringify(result);
}})();";

                var response = await ExecuteScriptWithResultAsync(script, true);
                Log($"GetGroupInfo结果: {response}");

                var ok = !string.IsNullOrEmpty(response) && response.Contains("\"success\":true");
                var name = ExtractJsonField(response, "groupName") ?? identity.GroupName;
                var err = ExtractJsonField(response, "error");
                var snippet = ExtractJsonField(response, "snippet");
                return ok
                    ? (true, name, snippet ?? "OK")
                    : (false, name, err ?? response);
            }
            catch (Exception ex)
            {
                Log($"GetGroupInfo异常: {ex.Message}");
                return (false, identity.GroupName, ex.Message);
            }
        }
        
        #endregion
    }
    
    /// <summary>
    /// 连接模式
    /// </summary>
    public enum ConnectionMode
    {
        /// <summary>未连接</summary>
        None,
        /// <summary>CDP 调试模式</summary>
        CDP,
        /// <summary>UI Automation 模式</summary>
        UIAutomation
    }
}


