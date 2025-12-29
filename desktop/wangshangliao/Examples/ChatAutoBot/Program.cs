// ChatAutoBot - 主控制程序
// 用于注入目标进程并控制自动聊天机器人
// 支持两种模式：EasyHook注入 和 Electron调试协议

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Remoting;
using System.Threading;
using System.Threading.Tasks;
using EasyHook;
using ChatAutoBotInterface;

namespace ChatAutoBot
{
    class Program
    {
        // IPC通道名称
        private static string _channelName = null;
        
        // 控制器实例
        private static ChatBotControllerBase _controller;
        
        // 消息历史记录
        private static List<ChatMessage> _messageHistory = new List<ChatMessage>();
        
        // 是否启用详细日志
        private static bool _verboseLog = true;
        
        // Electron注入器
        private static ElectronInjector _electronInjector;
        
        // 运行模式
        private static bool _isElectronMode = false;

        // 旺商聊默认路径
        private const string WANGSHANGLIAO_PATH = @"C:\旺商聊\wangshangliao_win_online\wangshangliao_win_online.exe";

        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.Title = "ChatAutoBot - 聊天自动化机器人";

            PrintBanner();

            int targetPid = 0;
            string targetExe = null;

            // 解析命令行参数
            if (args.Length >= 1)
            {
                if (args[0].ToLower() == "-electron" || args[0].ToLower() == "-e")
                {
                    _isElectronMode = true;
                    if (args.Length >= 2)
                        targetExe = args[1];
                }
                else if (int.TryParse(args[0], out targetPid))
                {
                    // 参数是PID
                }
                else if (File.Exists(args[0]))
                {
                    targetExe = args[0];
                }
            }

            // 如果没有提供参数，交互式选择
            while (targetPid == 0 && targetExe == null && !_isElectronMode)
            {
                Console.WriteLine("\n请选择操作模式:");
                Console.WriteLine("  1. [推荐] Electron模式 - 网上聊/旺信等Electron应用");
                Console.WriteLine("  2. EasyHook注入模式 - 传统Windows程序");
                Console.WriteLine("  3. 列出可能的目标进程");
                Console.WriteLine("  0. 退出");
                Console.Write("\n请输入选项: ");

                string input = Console.ReadLine()?.Trim();

                if (input == "0") return;
                
                if (input == "1")
                {
                    _isElectronMode = true;
                    
                    Console.WriteLine("\n检测到网上聊安装路径...");
                    if (File.Exists(WANGSHANGLIAO_PATH))
                    {
                        Console.WriteLine($"  找到: {WANGSHANGLIAO_PATH}");
                        Console.Write("使用此路径? (Y/n): ");
                        var confirm = Console.ReadLine()?.Trim().ToLower();
                        if (confirm != "n")
                        {
                            targetExe = WANGSHANGLIAO_PATH;
                            break;
                        }
                    }
                    
                    Console.Write("请输入Electron应用路径 (或直接回车连接已运行的实例): ");
                    string path = Console.ReadLine()?.Trim();
                    if (string.IsNullOrEmpty(path))
                    {
                        // 连接已运行的实例
                        break;
                    }
                    if (File.Exists(path))
                    {
                        targetExe = path;
                        break;
                    }
                    Console.WriteLine("文件不存在!");
                    _isElectronMode = false;
                }
                else if (input == "2")
                {
                    Console.WriteLine("\n  a. 注入到现有进程 (输入PID)");
                    Console.WriteLine("  b. 启动新进程并注入 (输入路径)");
                    Console.Write("请选择: ");
                    
                    var subInput = Console.ReadLine()?.Trim().ToLower();
                    if (subInput == "a")
                    {
                        Console.Write("请输入目标进程PID: ");
                        if (int.TryParse(Console.ReadLine(), out targetPid))
                        {
                            break;
                        }
                        Console.WriteLine("无效的PID!");
                    }
                    else if (subInput == "b")
                    {
                        Console.Write("请输入可执行文件路径: ");
                        string path = Console.ReadLine()?.Trim();
                        if (File.Exists(path))
                        {
                            targetExe = path;
                            break;
                        }
                        Console.WriteLine("文件不存在!");
                    }
                }
                else if (input == "3")
                {
                    ListPossibleTargets();
                }
            }
            
            // 根据模式执行
            if (_isElectronMode)
            {
                RunElectronMode(targetExe).GetAwaiter().GetResult();
            }
            else
            {
                RunEasyHookMode(targetPid, targetExe);
            }
        }

        /// <summary>
        /// Electron 模式运行
        /// </summary>
        static async Task RunElectronMode(string targetExe)
        {
            Console.WriteLine("\n═══════════════════════════════════════════════════════════════");
            Console.WriteLine("              Electron 模式 (Chrome DevTools Protocol)");
            Console.WriteLine("═══════════════════════════════════════════════════════════════\n");

            _electronInjector = new ElectronInjector();
            
            // 注册事件
            _electronInjector.OnLog += msg => 
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine($"[LOG] {msg}");
                Console.ResetColor();
            };
            
            _electronInjector.OnMessageReceived += msg =>
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[MSG] {msg}");
                Console.ResetColor();
            };
            
            _electronInjector.OnError += msg =>
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[ERR] {msg}");
                Console.ResetColor();
            };

            bool connected = false;
            
            if (!string.IsNullOrEmpty(targetExe))
            {
                // 启动新进程
                connected = await _electronInjector.LaunchWithDebug(targetExe);
            }
            else
            {
                // 尝试连接已运行的实例
                Console.WriteLine("尝试连接到已运行的实例 (端口 9222)...");
                Console.WriteLine("提示: 如果连接失败，请运行 '启动调试模式.bat' 先启动应用\n");
                connected = await _electronInjector.AttachToProcess(9222);
            }

            if (!connected)
            {
                Console.WriteLine("\n连接失败!");
                Console.WriteLine("请确保:");
                Console.WriteLine("  1. 目标应用以调试模式运行 (--remote-debugging-port=9222)");
                Console.WriteLine("  2. 或者运行 '启动调试模式.bat' 启动应用");
                Console.ReadKey();
                return;
            }

            Console.WriteLine("\n连接成功! 正在初始化...\n");

            // 启用控制台监听
            await _electronInjector.EnableConsoleListener();
            
            // 注入消息监听脚本
            await _electronInjector.InjectMessageListener();
            
            // 获取页面信息
            var pageInfo = await _electronInjector.GetPageInfo();
            Console.WriteLine($"页面信息: {pageInfo}\n");

            PrintElectronMenu();

            // 主循环
            while (true)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true);
                    
                    switch (key.KeyChar)
                    {
                        case 'q':
                        case 'Q':
                            _electronInjector.Dispose();
                            return;
                            
                        case 's':
                        case 'S':
                            Console.Write("\n输入要发送的消息: ");
                            var msg = Console.ReadLine();
                            if (!string.IsNullOrEmpty(msg))
                            {
                                await _electronInjector.SendChatMessage(msg);
                            }
                            break;
                            
                        case 'i':
                        case 'I':
                            await _electronInjector.InjectMessageListener();
                            break;
                            
                        case 'p':
                        case 'P':
                            var info = await _electronInjector.GetPageInfo();
                            Console.WriteLine($"\n页面信息: {info}\n");
                            break;
                            
                        case 'c':
                        case 'C':
                            var contacts = await _electronInjector.GetContactListFormatted();
                            Console.WriteLine($"\n═══ 联系人列表 ═══\n{contacts}\n");
                            break;
                            
                        case 'e':
                        case 'E':
                            Console.Write("\n输入要执行的JS代码: ");
                            var js = Console.ReadLine();
                            if (!string.IsNullOrEmpty(js))
                            {
                                var result = await _electronInjector.ExecuteScript(js);
                                Console.WriteLine($"结果: {result}\n");
                            }
                            break;
                        
                        // ===== 新增功能 =====
                        
                        case 'a':
                        case 'A':
                            // 自动回复功能
                            await HandleAutoReplyMenu();
                            break;
                            
                        case 'b':
                        case 'B':
                            // 批量群发功能
                            await HandleBatchSendMenu();
                            break;
                            
                        case 'd':
                        case 'D':
                            // 关键词回复功能
                            await HandleKeywordReplyMenu();
                            break;
                            
                        case 'h':
                        case 'H':
                        case '?':
                            PrintElectronMenu();
                            break;
                    }
                }
                
                await Task.Delay(100);
            }
        }

        /// <summary>
        /// 打印 Electron 模式菜单
        /// </summary>
        static void PrintElectronMenu()
        {
            Console.WriteLine("\n╔═══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                      功能菜单                                 ║");
            Console.WriteLine("╠═══════════════════════════════════════════════════════════════╣");
            Console.WriteLine("║  基础操作:                                                    ║");
            Console.WriteLine("║    s - 发送消息        p - 页面信息        c - 联系人列表    ║");
            Console.WriteLine("║    i - 重新注入脚本    e - 执行JS代码      q - 退出程序      ║");
            Console.WriteLine("╠═══════════════════════════════════════════════════════════════╣");
            Console.WriteLine("║  自动化功能:                                                  ║");
            Console.WriteLine("║    A - 自动回复设置    B - 批量群发        D - 关键词回复    ║");
            Console.WriteLine("╠═══════════════════════════════════════════════════════════════╣");
            Console.WriteLine("║    h/? - 显示此菜单                                           ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝\n");
        }

        /// <summary>
        /// 处理自动回复菜单
        /// </summary>
        static async Task HandleAutoReplyMenu()
        {
            Console.WriteLine("\n═══ 自动回复设置 ═══");
            Console.WriteLine("  1. 开启自动回复");
            Console.WriteLine("  2. 关闭自动回复");
            Console.WriteLine("  3. 设置回复内容");
            Console.WriteLine("  0. 返回");
            Console.Write("请选择: ");
            
            var choice = Console.ReadLine()?.Trim();
            
            switch (choice)
            {
                case "1":
                    Console.Write("请输入自动回复内容 (直接回车使用默认): ");
                    var replyMsg = Console.ReadLine();
                    if (string.IsNullOrEmpty(replyMsg))
                        replyMsg = "您好，我现在忙，稍后回复您！";
                    _electronInjector.EnableAutoReply(replyMsg);
                    Console.WriteLine($"自动回复已开启! 回复内容: {replyMsg}");
                    break;
                    
                case "2":
                    _electronInjector.DisableAutoReply();
                    Console.WriteLine("自动回复已关闭!");
                    break;
                    
                case "3":
                    Console.Write("请输入新的回复内容: ");
                    var newReply = Console.ReadLine();
                    if (!string.IsNullOrEmpty(newReply))
                    {
                        _electronInjector.EnableAutoReply(newReply);
                        Console.WriteLine($"回复内容已更新: {newReply}");
                    }
                    break;
            }
        }

        /// <summary>
        /// 处理批量群发菜单
        /// </summary>
        static async Task HandleBatchSendMenu()
        {
            Console.WriteLine("\n═══ 批量群发设置 ═══");
            Console.WriteLine("  1. 发送给所有联系人");
            Console.WriteLine("  2. 发送给指定联系人");
            Console.WriteLine("  3. 查看联系人列表");
            Console.WriteLine("  0. 返回");
            Console.Write("请选择: ");
            
            var choice = Console.ReadLine()?.Trim();
            
            switch (choice)
            {
                case "1":
                    Console.Write("请输入要群发的消息内容: ");
                    var msgAll = Console.ReadLine();
                    if (!string.IsNullOrEmpty(msgAll))
                    {
                        Console.Write("每条消息间隔秒数 (默认2): ");
                        var delayStr = Console.ReadLine();
                        int delay = 2000;
                        if (int.TryParse(delayStr, out int d) && d > 0)
                            delay = d * 1000;
                        
                        Console.WriteLine("开始批量群发...");
                        await _electronInjector.BatchSendMessage(msgAll, null, delay);
                    }
                    break;
                    
                case "2":
                    Console.Write("请输入联系人ID或名称 (多个用逗号分隔): ");
                    var contactStr = Console.ReadLine();
                    if (!string.IsNullOrEmpty(contactStr))
                    {
                        var contacts = new List<string>(contactStr.Split(new char[] { ',', '，', ' ' }, StringSplitOptions.RemoveEmptyEntries));
                        Console.Write("请输入要发送的消息: ");
                        var msgSome = Console.ReadLine();
                        if (!string.IsNullOrEmpty(msgSome))
                        {
                            Console.WriteLine($"开始发送给 {contacts.Count} 个联系人...");
                            await _electronInjector.BatchSendMessage(msgSome, contacts, 2000);
                        }
                    }
                    break;
                    
                case "3":
                    var list = await _electronInjector.GetContactListFormatted();
                    Console.WriteLine($"\n═══ 联系人列表 ═══\n{list}\n");
                    break;
            }
        }

        /// <summary>
        /// 处理关键词回复菜单
        /// </summary>
        static async Task HandleKeywordReplyMenu()
        {
            Console.WriteLine("\n═══ 关键词回复设置 ═══");
            Console.WriteLine("  1. 添加关键词规则");
            Console.WriteLine("  2. 删除关键词规则");
            Console.WriteLine("  3. 查看所有规则");
            Console.WriteLine("  4. 开启关键词回复");
            Console.WriteLine("  5. 关闭关键词回复");
            Console.WriteLine("  0. 返回");
            Console.Write("请选择: ");
            
            var choice = Console.ReadLine()?.Trim();
            
            switch (choice)
            {
                case "1":
                    Console.Write("请输入关键词: ");
                    var keyword = Console.ReadLine();
                    if (!string.IsNullOrEmpty(keyword))
                    {
                        Console.Write("请输入对应的回复内容: ");
                        var reply = Console.ReadLine();
                        if (!string.IsNullOrEmpty(reply))
                        {
                            _electronInjector.AddKeywordReply(keyword, reply);
                            Console.WriteLine($"已添加规则: '{keyword}' -> '{reply}'");
                        }
                    }
                    break;
                    
                case "2":
                    Console.Write("请输入要删除的关键词: ");
                    var kwToRemove = Console.ReadLine();
                    if (!string.IsNullOrEmpty(kwToRemove))
                    {
                        _electronInjector.RemoveKeywordReply(kwToRemove);
                    }
                    break;
                    
                case "3":
                    var rules = _electronInjector.ListKeywordRules();
                    Console.WriteLine($"\n═══ 关键词规则列表 ═══\n{rules}");
                    break;
                    
                case "4":
                    _electronInjector.EnableKeywordReply();
                    Console.WriteLine("关键词回复已开启!");
                    break;
                    
                case "5":
                    _electronInjector.DisableKeywordReply();
                    Console.WriteLine("关键词回复已关闭!");
                    break;
            }
        }

        /// <summary>
        /// EasyHook 模式运行
        /// </summary>
        static void RunEasyHookMode(int targetPid, string targetExe)
        {

            Console.WriteLine("\n═══════════════════════════════════════════════════════════════");
            Console.WriteLine("                 EasyHook 注入模式");
            Console.WriteLine("═══════════════════════════════════════════════════════════════\n");

            try
            {
                // 初始化IPC服务
                InitializeIpcServer();
                
                // 设置示例自动回复规则
                SetupDefaultRules();
                
                // 注入目标进程
                string injectionDll = Path.Combine(
                    Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
                    "ChatAutoBotInject.dll");

                if (!File.Exists(injectionDll))
                {
                    Console.WriteLine($"\n[错误] 找不到注入DLL: {injectionDll}");
                    Console.WriteLine("请确保 ChatAutoBotInject.dll 与本程序在同一目录。");
                    Console.ReadKey();
                    return;
                }

                Console.WriteLine($"注入DLL: {injectionDll}");

                if (!string.IsNullOrEmpty(targetExe))
                {
                    // 创建新进程并注入
                    Console.WriteLine($"正在启动并注入: {targetExe}");
                    
                    RemoteHooking.CreateAndInject(
                        targetExe,
                        "",
                        0,
                        InjectionOptions.DoNotRequireStrongName,
                        injectionDll,
                        injectionDll,
                        out targetPid,
                        _channelName);

                    Console.WriteLine($"已创建进程 PID: {targetPid}");
                }
                else
                {
                    // 注入到现有进程
                    Console.WriteLine($"正在注入到进程 PID: {targetPid}");
                    
                    RemoteHooking.Inject(
                        targetPid,
                        InjectionOptions.DoNotRequireStrongName,
                        injectionDll,
                        injectionDll,
                        _channelName);
                }

                Console.WriteLine("\n注入成功! 正在监控...\n");
                Console.WriteLine("═══════════════════════════════════════════════════════════════");
                Console.WriteLine("命令: q=退出, r=添加规则, l=列出规则, b=群发, v=切换详细日志");
                Console.WriteLine("═══════════════════════════════════════════════════════════════\n");

                // 主循环
                MainLoop();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n[错误] {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }

            Console.WriteLine("\n按任意键退出...");
            Console.ReadKey();
        }

        /// <summary>
        /// 打印横幅
        /// </summary>
        static void PrintBanner()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(@"
╔═══════════════════════════════════════════════════════════════╗
║       ______ __            __  ___         __        ____     ║
║      / ____// /_   ____ _ / /_/   | __  __/ /_ ____ / __ )____║
║     / /    / __ \ / __ `// __/ /| |/ / / / __// __ \\ __  / _ \║
║    / /___ / / / // /_/ // /_/ ___ / /_/ / /_ / /_/ / /_/ / __/║
║    \____//_/ /_/ \__,_/ \__/_/  |_\__,_/\__/ \____/\____/\___/║
║                                                               ║
║           基于 EasyHook 的聊天自动化机器人框架                  ║
╚═══════════════════════════════════════════════════════════════╝
");
            Console.ResetColor();
        }

        /// <summary>
        /// 列出可能的目标进程
        /// </summary>
        static void ListPossibleTargets()
        {
            Console.WriteLine("\n可能的目标进程:");
            Console.WriteLine("─────────────────────────────────────────");

            // 常见的聊天软件进程名
            string[] keywords = { "wangxin", "wx", "chat", "im", "messenger", "qq", "wechat", "weixin" };
            
            foreach (var proc in Process.GetProcesses())
            {
                try
                {
                    string name = proc.ProcessName.ToLower();
                    bool match = false;
                    
                    foreach (var kw in keywords)
                    {
                        if (name.Contains(kw))
                        {
                            match = true;
                            break;
                        }
                    }

                    // 显示所有有窗口标题的进程
                    if (match || !string.IsNullOrEmpty(proc.MainWindowTitle))
                    {
                        Console.WriteLine($"  PID: {proc.Id,-8} 名称: {proc.ProcessName,-20} 标题: {proc.MainWindowTitle}");
                    }
                }
                catch { }
            }
            Console.WriteLine("─────────────────────────────────────────");
        }

        /// <summary>
        /// 初始化IPC服务器
        /// </summary>
        static void InitializeIpcServer()
        {
            _controller = new ChatBotControllerBase();
            
            // 注册事件处理
            _controller.Injected += OnInjected;
            _controller.MessageReceived += OnMessageReceived;
            _controller.MessageSent += OnMessageSent;
            _controller.Error += OnError;
            _controller.Log += OnLog;
            
            // 设置AI回复处理（可选）
            _controller.SetAIReplyHandler(GetAIReply);

            // 创建IPC服务器
            RemoteHooking.IpcCreateServer<ChatBotControllerBase>(
                ref _channelName,
                WellKnownObjectMode.Singleton,
                _controller);

            Console.WriteLine($"IPC通道已创建: {_channelName}");
        }

        /// <summary>
        /// 设置默认的自动回复规则
        /// </summary>
        static void SetupDefaultRules()
        {
            // 示例规则1: 关键词回复
            _controller.AddAutoReplyRule(new AutoReplyRule
            {
                RuleId = "rule_1",
                Name = "打招呼",
                Enabled = true,
                TriggerKeywords = new List<string> { "你好", "hi", "hello", "在吗" },
                ReplyContents = new List<string> 
                { 
                    "你好！有什么可以帮助你的吗？",
                    "Hi~ 我在的",
                    "你好呀，有什么事吗？"
                },
                DelayMs = 1500,
                UseAI = false
            });

            // 示例规则2: 问价格
            _controller.AddAutoReplyRule(new AutoReplyRule
            {
                RuleId = "rule_2",
                Name = "询价回复",
                Enabled = true,
                TriggerKeywords = new List<string> { "多少钱", "价格", "怎么卖", "费用" },
                ReplyContents = new List<string> 
                { 
                    "请稍等，我帮您查一下价格~",
                    "关于价格的问题，请您稍等，我马上回复您"
                },
                DelayMs = 2000,
                UseAI = false
            });

            // 示例规则3: AI智能回复
            _controller.AddAutoReplyRule(new AutoReplyRule
            {
                RuleId = "rule_ai",
                Name = "AI智能回复",
                Enabled = false, // 默认关闭，需要配置API Key后启用
                TriggerKeywords = new List<string> { ".*" }, // 匹配所有消息
                UseAI = true,
                AIPrompt = "你是一个友好的客服助手，请用简洁专业的语言回复用户的问题。",
                DelayMs = 3000
            });

            Console.WriteLine("已加载 3 条自动回复规则");
        }

        /// <summary>
        /// 主循环 - 处理用户命令
        /// </summary>
        static void MainLoop()
        {
            while (true)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true);
                    
                    switch (key.KeyChar)
                    {
                        case 'q':
                        case 'Q':
                            return;
                            
                        case 'r':
                        case 'R':
                            AddNewRule();
                            break;
                            
                        case 'l':
                        case 'L':
                            ListRules();
                            break;
                            
                        case 'b':
                        case 'B':
                            CreateBroadcastTask();
                            break;
                            
                        case 'v':
                        case 'V':
                            _verboseLog = !_verboseLog;
                            Console.WriteLine($"\n详细日志: {(_verboseLog ? "开启" : "关闭")}");
                            break;
                            
                        case 'h':
                        case 'H':
                            ShowHistory();
                            break;
                    }
                }
                
                Thread.Sleep(100);
            }
        }

        #region 事件处理

        static void OnInjected(int processId, string processName)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[注入成功] 进程: {processName} (PID: {processId})");
            Console.ResetColor();
        }

        static void OnMessageReceived(ChatMessage message)
        {
            lock (_messageHistory)
            {
                _messageHistory.Add(message);
                if (_messageHistory.Count > 1000)
                {
                    _messageHistory.RemoveAt(0);
                }
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[收到消息] {message}");
            Console.ResetColor();
        }

        static void OnMessageSent(ChatMessage message)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"[发送消息] {message}");
            Console.ResetColor();
        }

        static void OnError(Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[错误] {ex.Message}");
            Console.ResetColor();
        }

        static void OnLog(string level, string message)
        {
            if (!_verboseLog && (level == "DEBUG" || level == "RECV" || level == "SEND"))
                return;

            ConsoleColor color = ConsoleColor.Gray;
            switch (level)
            {
                case "INFO": color = ConsoleColor.White; break;
                case "WARN": color = ConsoleColor.Yellow; break;
                case "ERROR": color = ConsoleColor.Red; break;
                case "DEBUG": color = ConsoleColor.DarkGray; break;
                case "RECV": color = ConsoleColor.DarkCyan; break;
                case "SEND": color = ConsoleColor.DarkMagenta; break;
                case "UI": color = ConsoleColor.DarkYellow; break;
            }

            Console.ForegroundColor = color;
            Console.WriteLine($"[{level}] {message}");
            Console.ResetColor();
        }

        #endregion

        #region 命令处理

        /// <summary>
        /// 添加新的自动回复规则
        /// </summary>
        static void AddNewRule()
        {
            Console.WriteLine("\n─── 添加自动回复规则 ───");
            
            Console.Write("规则名称: ");
            string name = Console.ReadLine();
            
            Console.Write("触发关键词 (多个用逗号分隔): ");
            string keywords = Console.ReadLine();
            
            Console.Write("回复内容 (多个用 | 分隔): ");
            string replies = Console.ReadLine();
            
            Console.Write("延迟时间(毫秒, 默认1500): ");
            if (!int.TryParse(Console.ReadLine(), out int delay))
                delay = 1500;

            var rule = new AutoReplyRule
            {
                RuleId = Guid.NewGuid().ToString(),
                Name = name,
                Enabled = true,
                TriggerKeywords = new List<string>(keywords.Split(',')),
                ReplyContents = new List<string>(replies.Split('|')),
                DelayMs = delay,
                UseAI = false
            };

            _controller.AddAutoReplyRule(rule);
            Console.WriteLine($"规则 '{name}' 已添加\n");
        }

        /// <summary>
        /// 列出所有规则
        /// </summary>
        static void ListRules()
        {
            var rules = _controller.GetAutoReplyRules();
            
            Console.WriteLine("\n─── 自动回复规则列表 ───");
            foreach (var rule in rules)
            {
                string status = rule.Enabled ? "启用" : "禁用";
                Console.WriteLine($"  [{status}] {rule.Name}");
                Console.WriteLine($"        关键词: {string.Join(", ", rule.TriggerKeywords)}");
                Console.WriteLine($"        回复数: {rule.ReplyContents.Count} 条");
            }
            Console.WriteLine();
        }

        /// <summary>
        /// 创建群发任务
        /// </summary>
        static void CreateBroadcastTask()
        {
            Console.WriteLine("\n─── 创建群发任务 ───");
            
            Console.Write("任务名称: ");
            string name = Console.ReadLine();
            
            Console.Write("目标ID列表 (逗号分隔): ");
            string targets = Console.ReadLine();
            
            Console.Write("发送内容: ");
            string content = Console.ReadLine();
            
            Console.Write("间隔时间(毫秒, 默认2000): ");
            if (!int.TryParse(Console.ReadLine(), out int interval))
                interval = 2000;

            var task = new BroadcastTask
            {
                TaskId = Guid.NewGuid().ToString(),
                Name = name,
                TargetIds = new List<string>(targets.Split(',')),
                Content = content,
                IntervalMs = interval
            };

            _controller.AddBroadcastTask(task);
            Console.WriteLine($"群发任务 '{name}' 已添加到队列\n");
        }

        /// <summary>
        /// 显示消息历史
        /// </summary>
        static void ShowHistory()
        {
            lock (_messageHistory)
            {
                Console.WriteLine("\n─── 最近消息历史 ───");
                int start = Math.Max(0, _messageHistory.Count - 20);
                for (int i = start; i < _messageHistory.Count; i++)
                {
                    Console.WriteLine($"  {_messageHistory[i]}");
                }
                Console.WriteLine();
            }
        }

        #endregion

        #region AI回复

        /// <summary>
        /// 获取AI回复（示例实现）
        /// 实际使用时请替换为真实的AI API调用
        /// </summary>
        static string GetAIReply(string prompt, string userMessage)
        {
            // TODO: 在这里集成真实的AI API
            // 例如: OpenAI, Claude, 文心一言, 通义千问等
            
            // 示例: 使用本地简单回复
            Console.WriteLine($"[AI请求] Prompt: {prompt}");
            Console.WriteLine($"[AI请求] User: {userMessage}");
            
            // 返回null表示不使用AI回复
            return null;

            /*
            // OpenAI示例代码 (需要安装OpenAI NuGet包)
            using var client = new OpenAIClient("your-api-key");
            var response = await client.GetCompletionAsync(new CompletionRequest
            {
                Model = "gpt-3.5-turbo",
                Messages = new[]
                {
                    new ChatMessage("system", prompt),
                    new ChatMessage("user", userMessage)
                }
            });
            return response.Choices[0].Message.Content;
            */
        }

        #endregion
    }
}

