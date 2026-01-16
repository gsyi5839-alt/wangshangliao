using System;
using System.Threading.Tasks;
using WSLFramework.Models;

namespace WSLFramework.Services
{
    /// <summary>
    /// 机器人登录服务 - 双账号架构
    /// 
    /// ★★★ 架构说明 ★★★
    /// 1. CDP连接 (监控账号) - 旺商聊客户端登录的账号，用于：
    ///    - 获取群列表、群成员信息
    ///    - 消息监控和轮询
    ///    - 界面操作
    ///    
    /// 2. NIM SDK直连 (机器人账号) - 独立的NIM凭证，用于：
    ///    - 精准发送群消息
    ///    - 精准发送私聊消息
    ///    - 不依赖客户端，速度快
    /// </summary>
    public class BotLoginService
    {
        private static BotLoginService _instance;
        public static BotLoginService Instance => _instance ?? (_instance = new BotLoginService());
        
        /// <summary>当前登录的账号 (机器人账号)</summary>
        public BotAccount CurrentAccount { get; private set; }
        
        /// <summary>监控账号 (CDP连接的客户端账号)</summary>
        public BotAccount MonitorAccount { get; private set; }
        
        /// <summary>是否已登录</summary>
        public bool IsLoggedIn { get; private set; }
        
        /// <summary>NIM SDK是否已连接 (机器人账号)</summary>
        public bool IsNimConnected => _nimService?.IsLoggedIn == true;
        
        /// <summary>CDP是否已连接 (监控账号)</summary>
        public bool IsCdpConnected => _cdpBridgeForPolling?.IsConnected == true;
        
        /// <summary>登录状态</summary>
        public string LoginStatus { get; private set; } = "未登录";
        
        // 服务依赖
        private NimDirectClient _nimClient;
        private NIMService _nimService; // nim.dll直接调用 (机器人账号)
        private CDPBridge _cdpBridgeForPolling; // CDP连接 (监控账号)
        private bool _useNimDll = true; // 优先使用nim.dll
        
        // ★★★ 双账号架构标志 ★★★
        private bool _dualAccountMode = false;
        private string _botNimAccid;  // 机器人NIM ID
        private string _botNimToken;  // 机器人NIM Token
        
        /// <summary>是否处于双账号模式 (CDP监控 + NIM SDK发送)</summary>
        public bool IsDualAccountMode => _dualAccountMode;
        
        // 事件
        public event Action<string> OnLog;
        public event Action<bool, string> OnLoginStateChanged;
        public event Action<string, string, string> OnGroupMessage; // groupId, fromId, content
        public event Action<string, string, string> OnPrivateMessage; // fromId, toId, content
        
        private BotLoginService()
        {
            // 加载账号列表
            AccountManager.Instance.Load();
        }
        
        #region ★★★ 双账号架构 - 核心方法 ★★★
        
        /// <summary>
        /// 双账号架构初始化
        /// 步骤1: CDP连接监控账号
        /// 步骤2: NIM SDK连接机器人账号
        /// </summary>
        public async Task<bool> InitDualAccountModeAsync(CDPBridge cdpBridge, BotAccount botAccount)
        {
            Log("=== 初始化双账号架构 ===");
            
            // 步骤1: CDP连接 (监控账号)
            if (cdpBridge == null || !cdpBridge.IsConnected)
            {
                Log("CDP未连接，尝试连接...");
                if (cdpBridge == null) cdpBridge = new CDPBridge();
                cdpBridge.OnLog += msg => Log($"[CDP] {msg}");
                
                if (!await cdpBridge.ConnectAsync())
                {
                    Log("CDP连接失败");
                    return false;
                }
            }
            
            _cdpBridgeForPolling = cdpBridge;
            
            // 获取监控账号信息
            var monitorInfo = await cdpBridge.GetCurrentUserInfoAsync();
            if (monitorInfo != null)
            {
                MonitorAccount = new BotAccount
                {
                    Wwid = monitorInfo.wwid,
                    Nickname = monitorInfo.nickname,
                    NimAccid = monitorInfo.nimId,
                    NimToken = monitorInfo.nimToken
                };
                Log($"✓ CDP监控账号: {monitorInfo.nickname} (wwid: {monitorInfo.wwid})");
            }
            
            // 步骤2: NIM SDK连接机器人账号
            if (botAccount == null || string.IsNullOrEmpty(botAccount.NimAccid))
            {
                Log("机器人账号NIM凭证为空，尝试从已保存的账号加载...");
                botAccount = AccountManager.Instance.GetAutoLoginAccount();
            }
            
            if (botAccount != null && !string.IsNullOrEmpty(botAccount.NimAccid) && !string.IsNullOrEmpty(botAccount.NimToken))
            {
                Log($"机器人账号: {botAccount.BotName} (NimAccid: {botAccount.NimAccid})");
                
                // 检查机器人账号是否与监控账号相同
                if (MonitorAccount != null && botAccount.NimAccid == MonitorAccount.NimAccid)
                {
                    Log("⚠️ 机器人账号与监控账号相同，无法双账号模式");
                    Log("⚠️ 将使用CDP模式发送消息");
                    _dualAccountMode = false;
                    
                    // 启动CDP消息轮询
                    cdpBridge.OnGroupMessage -= HandleCDPGroupMessage;
                    cdpBridge.OnGroupMessage += HandleCDPGroupMessage;
                    cdpBridge.StartMessagePolling(500);
                    
                    IsLoggedIn = true;
                    CurrentAccount = botAccount;
                    LoginStatus = "已登录 (CDP单账号模式)";
                    OnLoginStateChanged?.Invoke(true, LoginStatus);
                    return true;
                }
                
                // 尝试NIM SDK直连
                var nimConnected = await ConnectBotNimAsync(botAccount.NimAccid, botAccount.NimToken);
                
                if (nimConnected)
                {
                    _dualAccountMode = true;
                    _botNimAccid = botAccount.NimAccid;
                    _botNimToken = botAccount.NimToken;
                    CurrentAccount = botAccount;
                    
                    Log($"✓✓✓ 双账号架构启动成功! ✓✓✓");
                    Log($"  监控账号: {MonitorAccount?.Nickname} (CDP)");
                    Log($"  机器人账号: {botAccount.BotName} (NIM SDK直连)");
                    
                    // 启动CDP消息轮询 (监控)
                    cdpBridge.OnGroupMessage -= HandleCDPGroupMessage;
                    cdpBridge.OnGroupMessage += HandleCDPGroupMessage;
                    cdpBridge.StartMessagePolling(500);
                    
                    IsLoggedIn = true;
                    LoginStatus = "已登录 (双账号模式: CDP+NIM)";
                    OnLoginStateChanged?.Invoke(true, LoginStatus);
                    return true;
                }
                else
                {
                    Log("NIM SDK连接失败，回退到CDP单账号模式");
                }
            }
            
            // 回退: CDP单账号模式
            _dualAccountMode = false;
            cdpBridge.OnGroupMessage -= HandleCDPGroupMessage;
            cdpBridge.OnGroupMessage += HandleCDPGroupMessage;
            cdpBridge.StartMessagePolling(500);
            
            IsLoggedIn = true;
            CurrentAccount = botAccount ?? MonitorAccount as BotAccount;
            LoginStatus = "已登录 (CDP单账号模式)";
            OnLoginStateChanged?.Invoke(true, LoginStatus);
            return true;
        }
        
        /// <summary>
        /// 连接机器人NIM账号 (独立于客户端)
        /// </summary>
        private async Task<bool> ConnectBotNimAsync(string nimAccid, string nimToken)
        {
            try
            {
                Log($"[NIM] 连接机器人账号: {nimAccid}");
                
                _nimService = NIMService.Instance;
                
                if (!_nimService.IsInitialized)
                {
                    Log("[NIM] 初始化 nim.dll...");
                    if (!_nimService.Initialize())
                    {
                        Log("[NIM] nim.dll 初始化失败");
                        return false;
                    }
                }
                
                // 注册消息接收回调
                _nimService.OnMessageReceived -= HandleNimMessage;
                _nimService.OnMessageReceived += HandleNimMessage;
                
                // 登录
                var result = await _nimService.LoginAsync(nimAccid, nimToken);
                
                if (result)
                {
                    Log($"[NIM] ✓ 机器人账号登录成功: {nimAccid}");
                }
                else
                {
                    Log($"[NIM] 机器人账号登录失败 (可能Token无效或过期)");
                }
                
                return result;
            }
            catch (Exception ex)
            {
                Log($"[NIM] 连接异常: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 处理NIM消息 (机器人账号收到的消息)
        /// </summary>
        private void HandleNimMessage(NIMServiceMessage msg)
        {
            if (msg == null) return;
            
            if (msg.IsGroupMessage)
            {
                Log($"[NIM群消息] 群:{msg.ToId} 发送者:{msg.FromId} 内容:{msg.MsgBody}");
                OnGroupMessage?.Invoke(msg.ToId, msg.FromId, msg.MsgBody);
            }
            else
            {
                Log($"[NIM私聊] 发送者:{msg.FromId} 内容:{msg.MsgBody}");
                OnPrivateMessage?.Invoke(msg.FromId, msg.ToId, msg.MsgBody);
            }
        }
        
        /// <summary>
        /// 保存机器人NIM凭证 (用于下次启动)
        /// </summary>
        public void SaveBotCredentials(string nimAccid, string nimToken, string botName, string groupId)
        {
            var account = new BotAccount
            {
                NimAccid = nimAccid,
                NimToken = nimToken,
                BotName = botName,
                GroupId = groupId,
                AutoLogin = true
            };
            AccountManager.Instance.AddAccount(account);
            Log($"✓ 机器人凭证已保存: {botName} (NimAccid: {nimAccid})");
        }
        
        /// <summary>
        /// 从CDP提取并保存机器人凭证
        /// 使用方法: 用机器人账号登录客户端，调用此方法，然后切换回监控账号
        /// </summary>
        public async Task<bool> ExtractAndSaveBotCredentialsAsync(CDPBridge cdpBridge, string botName, string groupId)
        {
            if (cdpBridge == null || !cdpBridge.IsConnected)
            {
                Log("CDP未连接");
                return false;
            }
            
            var userInfo = await cdpBridge.GetCurrentUserInfoAsync();
            if (userInfo == null || string.IsNullOrEmpty(userInfo.nimId))
            {
                Log("无法获取NIM凭证");
                return false;
            }
            
            SaveBotCredentials(userInfo.nimId, userInfo.nimToken, botName, groupId);
            Log($"✓ 机器人凭证提取成功: NimAccid={userInfo.nimId}");
            Log($"  现在可以切换到监控账号，重启框架使用双账号模式");
            
            return true;
        }
        
        #endregion
        
        /// <summary>
        /// 使用指定账号登录 - 支持双账号架构
        /// 登录流程：先关闭客户端 → NIM直连 → 重启客户端CDP监控
        /// </summary>
        public async Task<bool> LoginAsync(BotAccount account)
        {
            if (account == null)
            {
                Log("登录失败: 账号为空");
                return false;
            }
            
            Log($"=== 尝试登录机器人账号: {account.Account} ===");
            
            // ★ 步骤1: 如果机器人账号已有保存的 NIM 凭证
            if (!string.IsNullOrEmpty(account.NimAccid) && !string.IsNullOrEmpty(account.NimToken))
            {
                Log($"发现已保存的 NIM 凭证: {account.Account} (NimAccid: {account.NimAccid})");
                
                // 检测并关闭旺商聊客户端（释放 Token 占用）
                if (IsWangShangLiaoRunning())
                {
                    Log("检测到旺商聊客户端正在运行，正在关闭以释放 Token...");
                    await CloseWangShangLiaoClientAsync();
                    await Task.Delay(2000); // 等待进程完全退出
                }
                
                // NIM 直连登录
                Log("正在使用 NIM 直连登录...");
                var result = await LoginWithNimTokenAsync(account.NimAccid, account.NimToken);
                if (result)
                {
                    CurrentAccount = account;
                    account.IsLoggedIn = true;
                    account.LoginStatus = "已登录(NIM直连)";
                    LoginStatus = "已登录(NIM直连)";
                    Log($"✓ NIM 直连登录成功！");
                    
                    // 登录成功后，重新启动客户端用于 CDP 监控
                    Log("正在启动旺商聊客户端（CDP监控模式）...");
                    await StartWangShangLiaoClientAsync();
                    
                    // 设置监控账号
                    await Task.Delay(5000); // 等待客户端启动
                    SetupMonitorAccountFromLocalStorage();
                    
                    OnLoginStateChanged?.Invoke(true, LoginStatus);
                    return true;
                }
                else
                {
                    Log("NIM 直连登录失败，尝试其他方式...");
                }
            }
            
            // ★ 步骤2: 检查本地存储 - 提取机器人凭证
            Log("检查旺商聊本地存储...");
            var localStorage = WangShangLiaoLocalStorage.Instance;
            
            if (localStorage.ConfigExists())
            {
                var clientLoginInfo = localStorage.ReadLoginInfo();
                if (clientLoginInfo != null && clientLoginInfo.IsValid)
                {
                    Log($"本地存储账号: {clientLoginInfo.NickName} (UID: {clientLoginInfo.Uid})");
                    
                    // 检查本地存储的账号是否就是要登录的机器人账号
                    // 比较 UID、Wwid 和 AccountId
                    bool isSameAccount = clientLoginInfo.Uid.ToString() == account.Account ||
                                         clientLoginInfo.Uid.ToString() == account.Wwid ||
                                         clientLoginInfo.AccountId.ToString() == account.Account;
                    
                    if (isSameAccount)
                    {
                        Log($"✓ 本地存储中找到机器人账号凭证，正在保存...");
                        
                        // 保存凭证
                        account.NimAccid = clientLoginInfo.NimId.ToString();
                        account.NimToken = clientLoginInfo.NimToken;
                        account.Nickname = clientLoginInfo.NickName;
                        account.Wwid = clientLoginInfo.Uid.ToString();
                        AccountManager.Instance.AddAccount(account);
                        
                        // 关闭客户端并使用 NIM 直连
                        if (IsWangShangLiaoRunning())
                        {
                            Log("关闭客户端以释放 Token...");
                            await CloseWangShangLiaoClientAsync();
                            await Task.Delay(2000);
                        }
                        
                        // NIM 直连登录
                        var nimResult = await LoginWithNimTokenAsync(account.NimAccid, account.NimToken);
                        if (nimResult)
                        {
                            CurrentAccount = account;
                            account.IsLoggedIn = true;
                            account.LoginStatus = "已登录(NIM直连)";
                            LoginStatus = "已登录(NIM直连)";
                            Log($"✓ NIM 直连登录成功！");
                            
                            // 重启客户端用于监控
                            Log("正在启动旺商聊客户端（CDP监控模式）...");
                            await StartWangShangLiaoClientAsync();
                            
                            OnLoginStateChanged?.Invoke(true, LoginStatus);
                            return true;
                        }
                    }
                    else
                    {
                        // 本地存储的是其他账号（如群主），设置为监控账号
                        MonitorAccount = new BotAccount
                        {
                            Wwid = clientLoginInfo.Uid.ToString(),
                            Nickname = clientLoginInfo.NickName,
                            NimAccid = clientLoginInfo.NimId.ToString(),
                            NimToken = clientLoginInfo.NimToken,
                            IsLoggedIn = true
                        };
                        Log($"监控账号: {clientLoginInfo.NickName} (群主)");
                        Log($"★ 双账号模式: 需要获取机器人账号 {account.Account} 的凭证");
                        
                        // ★ 尝试通过 CDP 获取机器人凭证
                        if (!string.IsNullOrEmpty(account.GetPassword()))
                        {
                            Log("尝试通过 CDP 自动获取机器人凭证...");
                            var botCredentials = await GetBotCredentialsViaCDPAsync(account.Account, account.GetPassword());
                            if (botCredentials != null && botCredentials.HasValidNimCredentials)
                            {
                                Log($"✓ 成功获取机器人凭证: {botCredentials.Nickname} (NimId: {botCredentials.NimId})");
                                
                                // 保存凭证
                                account.NimAccid = botCredentials.NimId;
                                account.NimToken = botCredentials.NimToken;
                                account.Nickname = botCredentials.Nickname;
                                account.Wwid = botCredentials.Uid.ToString();
                                AccountManager.Instance.AddAccount(account);
                                
                                // 关闭客户端，使用 NIM 直连
                                if (IsWangShangLiaoRunning())
                                {
                                    Log("关闭客户端以释放 Token...");
                                    await CloseWangShangLiaoClientAsync();
                                    await Task.Delay(2000);
                                }
                                
                                // NIM 直连登录
                                var nimResult = await LoginWithNimTokenAsync(account.NimAccid, account.NimToken);
                                if (nimResult)
                                {
                                    CurrentAccount = account;
                                    account.IsLoggedIn = true;
                                    account.LoginStatus = "已登录(NIM直连-双账号)";
                                    LoginStatus = "已登录(NIM直连-双账号)";
                                    Log($"✓ NIM 直连登录成功（双账号模式）！");
                                    
                                    // 重启客户端用于监控
                                    Log("正在启动旺商聊客户端（CDP监控模式）...");
                                    await StartWangShangLiaoClientAsync();
                                    
                                    OnLoginStateChanged?.Invoke(true, LoginStatus);
                                    return true;
                                }
                            }
                            else
                            {
                                Log($"通过 CDP 获取机器人凭证失败: {botCredentials?.Error ?? "未知错误"}");
                            }
                        }
                        else
                        {
                            Log("机器人账号没有配置密码，无法自动获取凭证");
                        }
                    }
                }
            }
            
            // ★ 步骤3: 没有凭证，自动启动旺商聊客户端获取凭证
            Log("没有找到有效凭证，正在启动旺商聊客户端获取凭证...");
            LoginStatus = "正在获取凭证...";
            OnLoginStateChanged?.Invoke(false, LoginStatus);
            
            // 先关闭已有的客户端实例
            if (IsWangShangLiaoRunning())
            {
                Log("关闭现有客户端实例...");
                await CloseWangShangLiaoClientAsync();
                await Task.Delay(2000);
            }
            
            // 启动旺商聊客户端（CDP模式）
            Log("启动旺商聊客户端（CDP模式）...");
            await StartWangShangLiaoClientAsync();
            
            // 等待客户端启动和用户登录
            Log("等待旺商聊客户端启动...");
            int maxWaitSeconds = 60; // 最长等待60秒
            int waitedSeconds = 0;
            bool credentialsObtained = false;
            
            while (waitedSeconds < maxWaitSeconds && !credentialsObtained)
            {
                await Task.Delay(3000); // 每3秒检查一次
                waitedSeconds += 3;
                
                Log($"检查登录状态... ({waitedSeconds}/{maxWaitSeconds}秒)");
                LoginStatus = $"等待登录... ({waitedSeconds}s)";
                
                // 检查 CDP 是否可连接
                if (await TryConnectCDPAsync())
                {
                    // 尝试获取用户信息
                    var cdpService = CDPService.Instance;
                    var userInfo = await cdpService.GetCurrentUserAsync();
                    
                    if (userInfo != null && !string.IsNullOrEmpty(userInfo.NimToken))
                    {
                        Log($"✓ 检测到用户登录: {userInfo.Nickname} (UID: {userInfo.Uid})");
                        
                        // 检查是否是目标机器人账号
                        bool isTargetAccount = userInfo.AccountId == account.Account ||
                                               userInfo.Wwid == account.Account ||
                                               userInfo.Uid.ToString() == account.Wwid;
                        
                        if (isTargetAccount || string.IsNullOrEmpty(account.Account))
                        {
                            Log($"✓ 成功获取机器人凭证！");
                            
                            // 保存凭证
                            account.NimAccid = userInfo.NimId;
                            account.NimToken = userInfo.NimToken;
                            account.Nickname = userInfo.Nickname;
                            account.Wwid = userInfo.Wwid;
                            if (string.IsNullOrEmpty(account.Account))
                                account.Account = userInfo.AccountId;
                            AccountManager.Instance.AddAccount(account);
                            
                            credentialsObtained = true;
                            
                            // 关闭客户端释放 Token
                            Log("关闭客户端以释放 Token...");
                            await CloseWangShangLiaoClientAsync();
                            await Task.Delay(2000);
                            
                            // NIM 直连登录
                            Log("使用获取的凭证进行 NIM 直连...");
                            var nimResult = await LoginWithNimTokenAsync(account.NimAccid, account.NimToken);
                            if (nimResult)
                            {
                                CurrentAccount = account;
                                account.IsLoggedIn = true;
                                account.LoginStatus = "已登录(NIM直连)";
                                LoginStatus = "已登录(NIM直连)";
                                Log($"✓ NIM 直连登录成功！");
                                
                                // 重启客户端用于监控
                                Log("重新启动旺商聊客户端（CDP监控模式）...");
                                await StartWangShangLiaoClientAsync();
                                
                                OnLoginStateChanged?.Invoke(true, LoginStatus);
                                return true;
                            }
                            else
                            {
                                // NIM 失败，使用 CDP 模式
                                Log("NIM 直连失败，使用 CDP 模式...");
                                await StartWangShangLiaoClientAsync();
                                await Task.Delay(5000);
                                
                                if (await TryConnectCDPAsync())
                                {
                                    CurrentAccount = account;
                                    account.IsLoggedIn = true;
                                    account.LoginStatus = "已登录(CDP模式)";
                                    LoginStatus = "已登录(CDP模式)";
                                    Log($"✓ CDP 模式登录成功");
                                    OnLoginStateChanged?.Invoke(true, LoginStatus);
                                    return true;
                                }
                            }
                        }
                        else
                        {
                            // 登录的不是目标账号，提示切换
                            Log($"★ 当前登录账号 ({userInfo.AccountId}) 不是目标机器人账号 ({account.Account})");
                            Log("请在客户端切换到正确的机器人账号...");
                            LoginStatus = $"请登录账号 {account.Account}";
                        }
                    }
                }
            }
            
            // 超时或失败
            if (!credentialsObtained)
            {
                Log("========================================");
                Log("获取凭证超时！请确保：");
                Log($"1. 在旺商聊客户端登录机器人账号 ({account.Account})");
                Log("2. 登录成功后框架会自动检测");
                Log("3. 或者手动重启框架重试");
                Log("========================================");
                
                // 最后尝试 CDP 模式
                if (await TryConnectCDPAsync())
                {
                    CurrentAccount = account;
                    account.IsLoggedIn = true;
                    account.LoginStatus = "已登录(CDP模式-待验证)";
                    LoginStatus = "已登录(CDP模式-待验证)";
                    Log($"✓ CDP 模式连接成功（请确认登录的是正确账号）");
                    OnLoginStateChanged?.Invoke(true, LoginStatus);
                    return true;
                }
                
                LoginStatus = "登录超时，请重试";
                OnLoginStateChanged?.Invoke(false, LoginStatus);
                return false;
            }
            
            return false;
        }
        
        /// <summary>
        /// 检测旺商聊客户端是否正在运行
        /// </summary>
        private bool IsWangShangLiaoRunning()
        {
            try
            {
                var processes = System.Diagnostics.Process.GetProcessesByName("wangshangliao_win_online");
                bool isRunning = processes.Length > 0;
                // 释放进程资源
                foreach (var proc in processes)
                {
                    proc.Dispose();
                }
                return isRunning;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// 关闭旺商聊客户端
        /// </summary>
        private async Task CloseWangShangLiaoClientAsync()
        {
            try
            {
                var processes = System.Diagnostics.Process.GetProcessesByName("wangshangliao_win_online");
                foreach (var proc in processes)
                {
                    try
                    {
                        proc.Kill();
                        Log($"已终止进程: {proc.Id}");
                    }
                    catch { }
                }
                await Task.Delay(1000);
            }
            catch (Exception ex)
            {
                Log($"关闭客户端异常: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 启动旺商聊客户端（CDP模式）
        /// </summary>
        private async Task StartWangShangLiaoClientAsync()
        {
            try
            {
                // 查找旺商聊可执行文件
                var exePaths = new[]
                {
                    @"C:\Program Files\wangshangliao_win_online\wangshangliao_win_online.exe",
                    @"C:\旺商聊\wangshangliao_win_online\wangshangliao_win_online.exe"
                };
                
                string exePath = null;
                foreach (var path in exePaths)
                {
                    if (System.IO.File.Exists(path))
                    {
                        exePath = path;
                        break;
                    }
                }
                
                if (exePath != null)
                {
                    // 启动时带上远程调试参数
                    var startInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = exePath,
                        Arguments = "--remote-debugging-port=9222",
                        UseShellExecute = true
                    };
                    System.Diagnostics.Process.Start(startInfo);
                    Log($"已启动旺商聊客户端（CDP模式）: {exePath}");
                    await Task.Delay(3000); // 等待启动
                }
                else
                {
                    Log("未找到旺商聊客户端可执行文件");
                }
            }
            catch (Exception ex)
            {
                Log($"启动客户端异常: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 从本地存储设置监控账号
        /// </summary>
        private void SetupMonitorAccountFromLocalStorage()
        {
            var localStorage = WangShangLiaoLocalStorage.Instance;
            if (localStorage.ConfigExists())
            {
                var clientInfo = localStorage.ReadLoginInfo();
                if (clientInfo != null && clientInfo.IsValid)
                {
                    MonitorAccount = new BotAccount
                    {
                        Wwid = clientInfo.Uid.ToString(),
                        Nickname = clientInfo.NickName,
                        NimAccid = clientInfo.NimId.ToString()
                    };
                    Log($"监控账号: {clientInfo.NickName} (来自客户端)");
                }
            }
        }
        
        /// <summary>
        /// 通过 CDP 获取机器人账号凭证
        /// </summary>
        private async Task<BotLoginResult> GetBotCredentialsViaCDPAsync(string accountId, string password)
        {
            try
            {
                // 确保 CDP 可用
                if (!await TryConnectCDPAsync())
                {
                    // CDP 未连接，尝试启动客户端
                    Log("CDP 未连接，正在启动旺商聊客户端...");
                    await StartWangShangLiaoClientAsync();
                    await Task.Delay(8000); // 等待客户端完全启动
                    
                    if (!await TryConnectCDPAsync())
                    {
                        return new BotLoginResult { Success = false, Error = "无法连接 CDP" };
                    }
                }
                
                var cdpService = CDPService.Instance;
                
                // 尝试简化方式
                Log($"通过 CDP 登录获取账号 {accountId} 的凭证...");
                var result = await cdpService.LoginAccountSimpleAsync(accountId, password);
                
                return result;
            }
            catch (Exception ex)
            {
                Log($"获取机器人凭证异常: {ex.Message}");
                return new BotLoginResult { Success = false, Error = ex.Message };
            }
        }
        
        /// <summary>
        /// 尝试连接 CDP
        /// </summary>
        private async Task<bool> TryConnectCDPAsync()
        {
            try
            {
                var cdpService = CDPService.Instance;
                if (await cdpService.CheckConnectionAsync())
                {
                    Log($"CDP 已连接: {cdpService.WebSocketUrl}");
                    // 注意：这里不设置 IsLoggedIn，CDP连接成功不等于登录成功
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log($"CDP 连接失败: {ex.Message}");
            }
            return false;
        }
        
        /// <summary>
        /// 使用账号密码登录 (快捷方法)
        /// </summary>
        public async Task<bool> LoginAsync(string account, string password, string groupId, string botName = "机器人")
        {
            var botAccount = new BotAccount
            {
                Account = account,
                BotName = botName,
                GroupId = groupId,
                RememberPassword = true
            };
            botAccount.SetPassword(password);
            
            return await LoginAsync(botAccount);
        }
        
        /// <summary>
        /// 自动登录 (使用保存的账号)
        /// </summary>
        public async Task<bool> AutoLoginAsync()
        {
            var account = AccountManager.Instance.GetAutoLoginAccount();
            if (account == null)
            {
                Log("没有自动登录账号");
                return false;
            }
            
            Log($"自动登录: {account.Account}");
            return await LoginAsync(account);
        }
        
        #region 账号更新处理
        
        /// <summary>
        /// 更新账号配置并处理登录
        /// 当用户修改机器人账号或群号时调用
        /// </summary>
        /// <param name="oldAccountId">旧账号ID（如果是更换账号）</param>
        /// <param name="newAccount">新账号配置</param>
        /// <returns>更新结果和是否需要重新登录</returns>
        public async Task<AccountChangeResult> UpdateAccountAsync(string oldAccountId, BotAccount newAccount)
        {
            var result = new AccountChangeResult();
            
            Log($"=== 更新账号配置 ===");
            Log($"旧账号: {oldAccountId ?? "(无)"}, 新账号: {newAccount.Account}, 群号: {newAccount.GroupId}");
            
            // 1. 使用 AccountManager 处理账号更新
            var updateResult = AccountManager.Instance.UpdateAccountConfig(oldAccountId, newAccount);
            result.UpdateResult = updateResult;
            
            // 2. 如果账号没变且只是群号变更，更新当前会话
            if (!updateResult.AccountChanged && updateResult.GroupChanged)
            {
                Log("仅群号变更，更新当前配置...");
                if (CurrentAccount != null)
                {
                    CurrentAccount.GroupId = newAccount.GroupId;
                    CurrentAccount.GroupName = newAccount.GroupName;
                }
                result.Success = true;
                result.Message = "群号已更新";
                return result;
            }
            
            // 3. 如果账号变更但有保存的凭证，尝试直接登录
            if (updateResult.AccountChanged && updateResult.HasCredentials)
            {
                Log("账号已变更，使用保存的凭证登录...");
                
                // 先退出当前登录
                await LogoutAsync();
                
                // 使用新账号登录
                result.Success = await LoginAsync(newAccount);
                result.Message = result.Success ? "已切换到新账号" : "新账号登录失败";
                return result;
            }
            
            // 4. 如果需要获取新凭证
            if (updateResult.NeedCredentials)
            {
                Log("新账号需要获取凭证...");
                
                // 尝试通过 CDP 获取凭证（如果有密码）
                if (!string.IsNullOrEmpty(newAccount.GetPassword()))
                {
                    Log("尝试通过 CDP 自动获取凭证...");
                    var credentials = await GetBotCredentialsViaCDPAsync(newAccount.Account, newAccount.GetPassword());
                    
                    if (credentials != null && credentials.HasValidNimCredentials)
                    {
                        Log($"✓ 成功获取凭证: {credentials.Nickname}");
                        
                        // 保存凭证
                        newAccount.NimAccid = credentials.NimId;
                        newAccount.NimToken = credentials.NimToken;
                        newAccount.Nickname = credentials.Nickname;
                        newAccount.Wwid = credentials.Uid.ToString();
                        AccountManager.Instance.AddAccount(newAccount);
                        
                        // 先退出当前登录
                        await LogoutAsync();
                        
                        // 使用新凭证登录
                        result.Success = await LoginAsync(newAccount);
                        result.Message = result.Success ? "已切换到新账号（自动获取凭证）" : "登录失败";
                        return result;
                    }
                    else
                    {
                        Log($"自动获取凭证失败: {credentials?.Error ?? "未知错误"}");
                    }
                }
                
                // 自动获取失败，需要手动获取
                result.Success = false;
                result.NeedManualCredentials = true;
                result.Message = "需要手动获取凭证：请用新机器人账号登录旺商聊客户端一次";
                
                Log("========================================");
                Log("需要手动获取凭证：");
                Log("1. 退出当前旺商聊客户端");
                Log($"2. 用新机器人账号 ({newAccount.Account}) 登录客户端");
                Log("3. 登录成功后重启框架");
                Log("========================================");
                
                return result;
            }
            
            // 5. 其他情况，直接尝试登录
            await LogoutAsync();
            result.Success = await LoginAsync(newAccount);
            result.Message = result.Success ? "配置已更新并重新登录" : "重新登录失败";
            
            return result;
        }
        
        /// <summary>
        /// 强制刷新凭证（清除旧凭证并重新获取）
        /// </summary>
        public async Task<bool> RefreshCredentialsAsync(BotAccount account)
        {
            Log($"强制刷新凭证: {account.Account}");
            
            // 清除现有凭证
            AccountManager.Instance.ClearCredentials(account.Account);
            
            // 退出当前登录
            await LogoutAsync();
            
            // 重新登录（会触发凭证获取流程）
            return await LoginAsync(account);
        }
        
        /// <summary>
        /// 退出登录
        /// </summary>
        public async Task LogoutAsync()
        {
            Log("退出登录...");
            
            if (CurrentAccount != null)
            {
                CurrentAccount.IsLoggedIn = false;
                CurrentAccount.LoginStatus = "已退出";
            }
            
            IsLoggedIn = false;
            LoginStatus = "已退出";
            
            // 停止 CDP 轮询
            try
            {
                // CDPService 会在下次登录时重新初始化
                _cdpBridgeForPolling = null;
            }
            catch { }
            
            // 停止 NIM 连接
            try
            {
                _nimClient?.Disconnect();
                _nimClient = null;
            }
            catch { }
            
            // 停止 NIM Service (nim.dll)
            try
            {
                if (_nimService != null)
                {
                    _nimService.OnMessageReceived -= HandleNimServiceMessage;
                    _ = _nimService.LogoutAsync(); // 异步登出
                    _nimService = null;
                }
                _nimServiceEventRegistered = false;
            }
            catch { }
            
            // 重置双账号模式标志
            _dualAccountMode = false;
            _botNimAccid = null;
            _botNimToken = null;
            
            // 注意：_nimClientEventRegistered 不重置，因为 NimDirectClient 是单例
            
            OnLoginStateChanged?.Invoke(false, "已退出");
            
            await Task.Delay(500);
        }
        
        #endregion
        
        // ★★★ 事件处理器（避免重复注册）★★★
        private bool _nimServiceEventRegistered = false;
        private bool _nimClientEventRegistered = false;
        
        /// <summary>
        /// 使用 NIM Token 直接登录（从 CDP 获取的凭证）
        /// 优先使用 nim.dll，失败则回退到 NimDirect
        /// </summary>
        public async Task<bool> LoginWithNimTokenAsync(string nimAccid, string nimToken)
        {
            try
            {
                if (string.IsNullOrEmpty(nimAccid) || string.IsNullOrEmpty(nimToken))
                {
                    Log("NIM 凭证为空，无法登录");
                    return false;
                }
                
                Log($"使用 NIM Token 登录: accid={nimAccid}");
                LoginStatus = "连接NIM...";
                
                bool nimConnected = false;
                
                // ★★★ 优先使用 nim.dll (NIMService) ★★★
                if (_useNimDll)
                {
                    Log("[NIM] 尝试使用 nim.dll 登录...");
                    _nimService = NIMService.Instance;
                    
                    if (_nimService.IsInitialized)
                    {
                        // 注册消息接收回调（避免重复注册）
                        if (!_nimServiceEventRegistered)
                        {
                            _nimService.OnMessageReceived += HandleNimServiceMessage;
                            _nimServiceEventRegistered = true;
                        }
                        
                        nimConnected = await _nimService.LoginAsync(nimAccid, nimToken);
                        
                        if (nimConnected)
                        {
                            Log("[NIM] ✓ nim.dll 登录成功!");
                        }
                        else
                        {
                            Log("[NIM] nim.dll 登录失败，尝试 NimDirect...");
                        }
                    }
                    else
                    {
                        Log("[NIM] nim.dll 未初始化，尝试 NimDirect...");
                    }
                }
                
                // ★★★ 回退到 NimDirect (TCP/SSL) ★★★
                if (!nimConnected)
                {
                    Log("[NIM] 使用 NimDirect (TCP/SSL) 登录...");
                    _nimClient = NimDirectClient.Instance;
                    
                    // 注册事件（避免重复注册）
                    if (!_nimClientEventRegistered)
                    {
                        _nimClient.OnLog += HandleNimClientLog;
                        _nimClient.OnMessageReceived += HandleNimClientMessage;
                        _nimClientEventRegistered = true;
                    }
                    
                    // 连接NIM
                    var appKey = "45c6af3c98409b18a84451215d0bdd6e"; // 旺商聊默认 AppKey
                    nimConnected = await _nimClient.LoginAsync(appKey, nimAccid, nimToken);
                }
                
                if (nimConnected)
                {
                    IsLoggedIn = true;
                    LoginStatus = $"已登录 (NIM Token)";
                    
                    if (CurrentAccount != null)
                    {
                        CurrentAccount.NimAccid = nimAccid;
                        CurrentAccount.NimToken = nimToken;
                        CurrentAccount.IsLoggedIn = true;
                        CurrentAccount.LoginStatus = "已登录";
                        
                        // 设置活跃群
                        if (!string.IsNullOrEmpty(CurrentAccount.GroupId) && _nimClient != null)
                        {
                            _nimClient.SetActiveGroup(CurrentAccount.GroupId);
                        }
                        
                        // ★★★ 获取群名称 (解决群名称为空问题) ★★★
                        await FetchAndUpdateGroupNameAsync(CurrentAccount);
                    }
                    
                    Log($"✓ NIM Token 登录成功: {nimAccid}");
                    OnLoginStateChanged?.Invoke(true, LoginStatus);
                    return true;
                }
                else
                {
                    // ★★★ NIM登录失败，自动切换到CDP模式 ★★★
                    // 因为旺商聊客户端已登录，可以通过CDP转发消息
                    Log("NIM登录失败(Token已被旺商聊客户端使用)，切换到CDP模式");
                    
                    // 检查CDP是否可用 - 尝试连接
                    if (_cdpBridgeForPolling == null)
                    {
                        _cdpBridgeForPolling = new CDPBridge();
                        _cdpBridgeForPolling.OnLog += msg => Log($"[CDP] {msg}");
                        await _cdpBridgeForPolling.ConnectAsync();
                    }
                    
                    if (_cdpBridgeForPolling != null && _cdpBridgeForPolling.IsConnected)
                    {
                        IsLoggedIn = true;
                        LoginStatus = "已登录 (CDP模式)";
                        
                        if (CurrentAccount != null)
                        {
                            CurrentAccount.NimAccid = nimAccid;
                            CurrentAccount.NimToken = nimToken;
                            CurrentAccount.IsLoggedIn = true;
                            CurrentAccount.LoginStatus = "已登录(CDP)";
                        }
                        
                        Log($"✓ 切换到CDP模式成功，通过旺商聊客户端发送消息");
                        OnLoginStateChanged?.Invoke(true, LoginStatus);
                        return true;
                    }
                    else
                    {
                        LoginStatus = "NIM连接失败，CDP不可用";
                        Log("NIM Token 登录失败，且CDP不可用");
                        OnLoginStateChanged?.Invoke(false, LoginStatus);
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                LoginStatus = $"登录异常: {ex.Message}";
                Log($"NIM Token 登录异常: {ex.Message}");
                OnLoginStateChanged?.Invoke(false, LoginStatus);
                return false;
            }
        }
        
        /// <summary>
        /// 登出
        /// </summary>
        public void Logout()
        {
            try
            {
                if (_nimClient != null)
                {
                    _nimClient.Disconnect();
                }
                
                if (CurrentAccount != null)
                {
                    CurrentAccount.IsLoggedIn = false;
                    CurrentAccount.LoginStatus = "已登出";
                }
                
                IsLoggedIn = false;
                LoginStatus = "已登出";
                CurrentAccount = null;
                
                Log("已登出");
                OnLoginStateChanged?.Invoke(false, LoginStatus);
            }
            catch (Exception ex)
            {
                Log($"登出异常: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 发送群消息
        /// ★★★ 双账号模式: 优先使用NIM SDK直连 (精准绑定群号) ★★★
        /// </summary>
        public async Task<bool> SendGroupMessageAsync(string groupId, string content)
        {
            if (!IsLoggedIn)
            {
                Log("发送失败: 未登录");
                return false;
            }
            
            try
            {
                // ★★★ 优先使用NIM SDK直连 (机器人账号) ★★★
                // 精准绑定群号，速度快，不依赖客户端
                if (_nimService != null && _nimService.IsLoggedIn)
                {
                    Log($"[NIM直连] 发送群消息到: {groupId}");
                    var result = await _nimService.SendGroupMessageAsync(groupId, content);
                    if (result)
                    {
                        Log($"[NIM直连] ✓ 群消息发送成功: {groupId}");
                    }
                    else
                    {
                        Log($"[NIM直连] 群消息发送失败，尝试CDP备用...");
                        // 回退到CDP
                        if (_cdpBridgeForPolling != null && _cdpBridgeForPolling.IsConnected)
                        {
                            return await _cdpBridgeForPolling.SendGroupMessageAsync(groupId, content);
                        }
                    }
                    return result;
                }
                // 回退到NimDirect
                else if (_nimClient != null && _nimClient.IsLoggedIn)
                {
                    Log($"[NimDirect] 发送群消息到: {groupId}");
                    return await _nimClient.SendTeamMessageAsync(groupId, content);
                }
                // 回退到CDP模式
                else if (_cdpBridgeForPolling != null && _cdpBridgeForPolling.IsConnected)
                {
                    Log($"[CDP模式] 发送群消息到: {groupId}");
                    return await _cdpBridgeForPolling.SendGroupMessageAsync(groupId, content);
                }
                else
                {
                    Log("发送失败: 没有可用的发送通道");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log($"发送群消息失败: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 发送私聊消息
        /// ★★★ 双账号模式: 优先使用NIM SDK直连 (精准发送) ★★★
        /// </summary>
        public async Task<bool> SendPrivateMessageAsync(string toId, string content)
        {
            if (!IsLoggedIn)
            {
                Log("发送失败: 未登录");
                return false;
            }
            
            try
            {
                // ★★★ 优先使用NIM SDK直连 (机器人账号) ★★★
                if (_nimService != null && _nimService.IsLoggedIn)
                {
                    Log($"[NIM直连] 发送私聊消息到: {toId}");
                    var result = await _nimService.SendPrivateMessageAsync(toId, content);
                    if (result)
                    {
                        Log($"[NIM直连] ✓ 私聊消息发送成功: {toId}");
                    }
                    else
                    {
                        Log($"[NIM直连] 私聊消息发送失败，尝试CDP备用...");
                        if (_cdpBridgeForPolling != null && _cdpBridgeForPolling.IsConnected)
                        {
                            return await _cdpBridgeForPolling.SendPrivateMessageAsync(toId, content);
                        }
                    }
                    return result;
                }
                // 回退到NimDirect
                else if (_nimClient != null && _nimClient.IsLoggedIn)
                {
                    Log($"[NimDirect] 发送私聊消息到: {toId}");
                    return await _nimClient.SendP2PMessageAsync(toId, content);
                }
                // 回退到CDP模式
                else if (_cdpBridgeForPolling != null && _cdpBridgeForPolling.IsConnected)
                {
                    Log($"[CDP模式] 发送私聊消息到: {toId}");
                    return await _cdpBridgeForPolling.SendPrivateMessageAsync(toId, content);
                }
                else
                {
                    Log("发送失败: 没有可用的发送通道");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log($"发送私聊消息失败: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 获取当前发送模式
        /// </summary>
        public string GetSendMode()
        {
            if (_nimService != null && _nimService.IsLoggedIn)
                return "NIM SDK直连 (精准)";
            if (_nimClient != null && _nimClient.IsLoggedIn)
                return "NimDirect TCP/SSL";
            if (_cdpBridgeForPolling != null && _cdpBridgeForPolling.IsConnected)
                return "CDP模式 (通过客户端)";
            return "未连接";
        }
        
        /// <summary>
        /// 获取当前绑定的群号
        /// </summary>
        public string GetCurrentGroupId()
        {
            return CurrentAccount?.GroupId ?? "";
        }
        
        /// <summary>
        /// 通过 CDP 连接旺商聊客户端，提取 NIM Token 后直接连接 NIM
        /// 流程: CDP连接 -> 提取NIM Token -> 直连NIM SDK
        /// </summary>
        public async Task<bool> LoginWithCDPAsync(BotAccount account, CDPBridge cdpBridge)
        {
            if (account == null)
            {
                Log("登录失败: 账号为空");
                return false;
            }
            
            if (cdpBridge == null || !cdpBridge.IsConnected)
            {
                Log("登录失败: CDP 未连接，请先启动旺商聊客户端");
                return false;
            }
            
            try
            {
                LoginStatus = "正在从旺商聊提取凭证...";
                Log("=== 通过 CDP 提取 NIM Token ===");
                
                // 第一步: 从 CDP 获取当前登录用户的 NIM 凭证
                var userInfo = await cdpBridge.GetCurrentUserInfoAsync();
                if (userInfo == null)
                {
                    Log("无法获取用户信息，请确保旺商聊已登录");
                    LoginStatus = "获取凭证失败";
                    OnLoginStateChanged?.Invoke(false, LoginStatus);
                    return false;
                }
                
                Log($"CDP 获取到客户端用户: {userInfo.nickname} (wwid: {userInfo.wwid}, nimId: {userInfo.nimId})");
                
                // ★★★ 双账号模式检查 ★★★
                // 如果机器人账号已有NIM凭证，且与客户端不同，不要覆盖
                bool hasBotCredentials = !string.IsNullOrEmpty(account.NimAccid) && !string.IsNullOrEmpty(account.NimToken);
                bool isDualAccountMode = hasBotCredentials && account.NimAccid != userInfo.nimId;
                
                if (isDualAccountMode)
                {
                    Log($"★★★ 双账号模式检测 ★★★");
                    Log($"  客户端账号 (监控): nimId={userInfo.nimId}");
                    Log($"  机器人账号 (发送): nimId={account.NimAccid}");
                    Log($"  保留机器人NIM凭证，不覆盖");
                    
                    // 只更新非NIM相关字段
                    account.LastLoginTime = DateTime.Now;
                    
                    // 使用机器人自己的NIM凭证登录
                    Log($"使用机器人NIM凭证: {account.NimAccid}");
                }
                else
                {
                    // 单账号模式：使用CDP获取的凭证
                    // 验证 NIM Token
                    if (string.IsNullOrEmpty(userInfo.nimId) || string.IsNullOrEmpty(userInfo.nimToken))
                    {
                        // 尝试从 config.json 补充
                        Log("CDP 未获取到 NIM Token，尝试从配置文件读取...");
                        var configToken = TryReadNimTokenFromConfig();
                        if (!string.IsNullOrEmpty(configToken.nimToken))
                        {
                            userInfo.nimId = configToken.nimId;
                            userInfo.nimToken = configToken.nimToken;
                            Log($"从配置文件获取到 NIM Token: accid={userInfo.nimId}");
                        }
                        else
                        {
                            Log("NIM Token 为空，无法连接");
                            LoginStatus = "NIM凭证不完整";
                            OnLoginStateChanged?.Invoke(false, LoginStatus);
                            return false;
                        }
                    }
                    
                    // 更新账号信息 (单账号模式)
                    account.Wwid = userInfo.wwid;
                    account.Nickname = userInfo.nickname;
                    account.NimAccid = userInfo.nimId;
                    account.NimToken = userInfo.nimToken;
                    account.LastLoginTime = DateTime.Now;
                    
                    Log($"✓ NIM 凭证提取成功 (单账号模式): accid={account.NimAccid}");
                }
                
                // 第二步: 使用 NIM Token 直接连接 NIM SDK
                LoginStatus = "连接 NIM SDK...";
                
                _nimClient = NimDirectClient.Instance;
                _nimClient.OnLog += msg => Log($"[NIM] {msg}");
                
                // 注册消息接收事件
                _nimClient.OnMessageReceived += (msg) =>
                {
                    if (msg == null) return;
                    
                    var isGroupMessage = msg.Scene == "team";
                    
                    if (isGroupMessage)
                    {
                        var groupId = msg.To;
                        var fromId = msg.From;
                        var content = msg.Body;
                        
                        // 检查是否是绑定的群
                        if (string.IsNullOrEmpty(account.GroupId) || account.GroupId == groupId)
                        {
                            Log($"[群消息] 群:{groupId} 发送者:{fromId} 内容:{content}");
                            OnGroupMessage?.Invoke(groupId, fromId, content);
                        }
                    }
                    else
                    {
                        var fromId = msg.From;
                        var toId = msg.To;
                        var content = msg.Body;
                        
                        Log($"[私聊] 发送者:{fromId} 内容:{content}");
                        OnPrivateMessage?.Invoke(fromId, toId, content);
                    }
                };
                
                // 尝试连接 NIM SDK（可能失败，因为协议可能已更新）
                var appKey = "45c6af3c98409b18a84451215d0bdd6e"; // 旺商聊默认 AppKey
                var nimConnected = false;
                
                try
                {
                    nimConnected = await _nimClient.LoginAsync(appKey, account.NimAccid, account.NimToken);
                }
                catch (Exception nimEx)
                {
                    Log($"NIM 直连尝试失败: {nimEx.Message}");
                }
                
                if (!nimConnected)
                {
                    // NIM 直连失败，改用 CDP 消息轮询模式
                    Log("NIM SDK 直连失败，改用 CDP 消息轮询模式");
                    Log("=== 启用 CDP 消息轮询 ===");
                    
                    // 保存 CDP 引用供消息轮询使用
                    _cdpBridgeForPolling = cdpBridge;
                    
                    // 注册 CDP 群消息事件
                    cdpBridge.OnGroupMessage -= HandleCDPGroupMessage;
                    cdpBridge.OnGroupMessage += HandleCDPGroupMessage;
                    
                    // 启动 CDP 消息轮询
                    cdpBridge.StartMessagePolling(500); // 500ms 轮询间隔
                    Log("✓ CDP 消息轮询已启动");
                }
                else
                {
                    Log("✓ NIM SDK 直连成功");
                    
                    // 设置活跃群
                    if (!string.IsNullOrEmpty(account.GroupId))
                    {
                        _nimClient.SetActiveGroup(account.GroupId);
                        Log($"✓ 设置活跃群: {account.GroupId}");
                    }
                }
                
                // 登录成功（无论是 NIM 直连还是 CDP 轮询）
                IsLoggedIn = true;
                CurrentAccount = account;
                account.IsLoggedIn = true;
                account.LoginStatus = nimConnected ? "NIM直连" : "CDP轮询";
                LoginStatus = $"已登录: {account.Nickname} ({account.LoginStatus})";
                
                // 保存账号信息
                AccountManager.Instance.AddAccount(account);
                
                Log($"✓ CDP + NIM 登录成功: {account.Nickname} ({account.Wwid}) 绑定群:{account.GroupId}");
                OnLoginStateChanged?.Invoke(true, LoginStatus);
                
                return true;
            }
            catch (Exception ex)
            {
                LoginStatus = $"登录异常: {ex.Message}";
                Log($"CDP 登录异常: {ex.Message}");
                OnLoginStateChanged?.Invoke(false, LoginStatus);
                return false;
            }
        }
        
        /// <summary>
        /// 从旺商聊客户端配置文件读取 NIM Token
        /// </summary>
        private (string nimId, string nimToken) TryReadNimTokenFromConfig()
        {
            try
            {
                var configPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "wangshangliao",
                    "config.json"
                );
                
                if (!System.IO.File.Exists(configPath))
                {
                    return ("", "");
                }
                
                var configText = System.IO.File.ReadAllText(configPath, System.Text.Encoding.UTF8);
                
                // 提取 nimId
                var nimIdMatch = System.Text.RegularExpressions.Regex.Match(
                    configText, @"""nimId""\s*:\s*(\d+)");
                var nimId = nimIdMatch.Success ? nimIdMatch.Groups[1].Value : "";
                
                // 提取 nimToken
                var nimTokenMatch = System.Text.RegularExpressions.Regex.Match(
                    configText, @"""nimToken""\s*:\s*""([^""]+)""");
                var nimToken = nimTokenMatch.Success ? nimTokenMatch.Groups[1].Value : "";
                
                return (nimId, nimToken);
            }
            catch
            {
                return ("", "");
            }
        }
        
        #region ★★★ 群名称获取 (解决群名称为空问题) ★★★
        
        /// <summary>
        /// 获取并更新群名称
        /// </summary>
        private async Task FetchAndUpdateGroupNameAsync(BotAccount account)
        {
            if (account == null || string.IsNullOrEmpty(account.GroupId))
            {
                Log("[群名称] 账号或群号为空，跳过获取群名称");
                return;
            }
            
            try
            {
                // 检查 CDP 是否可用
                if (_cdpBridgeForPolling == null || !_cdpBridgeForPolling.IsConnected)
                {
                    Log("[群名称] CDP未连接，尝试连接...");
                    if (_cdpBridgeForPolling == null)
                    {
                        _cdpBridgeForPolling = new CDPBridge();
                        _cdpBridgeForPolling.OnLog += msg => Log($"[CDP] {msg}");
                    }
                    await _cdpBridgeForPolling.ConnectAsync();
                }
                
                if (_cdpBridgeForPolling != null && _cdpBridgeForPolling.IsConnected)
                {
                    Log($"[群名称] 获取群 {account.GroupId} 的信息...");
                    var groupInfo = await _cdpBridgeForPolling.GetTeamInfoAsync(account.GroupId);
                    
                    if (groupInfo != null && !string.IsNullOrEmpty(groupInfo.groupName))
                    {
                        account.GroupName = groupInfo.groupName;
                        Log($"[群名称] 成功获取群名称: {groupInfo.groupName}");
                        
                        // 保存更新后的账号信息
                        AccountManager.Instance.AddAccount(account);
                    }
                    else
                    {
                        Log("[群名称] 未能获取群名称，可能是群不存在或加密数据无法解密");
                    }
                }
                else
                {
                    Log("[群名称] CDP连接失败，无法获取群名称");
                }
            }
            catch (Exception ex)
            {
                Log($"[群名称] 获取群名称异常: {ex.Message}");
            }
        }
        
        #endregion
        
        /// <summary>
        /// 处理 CDP 群消息事件
        /// </summary>
        private void HandleCDPGroupMessage(GroupMessageEvent msg)
        {
            if (msg == null) return;
            
            var groupId = msg.GroupId;
            var fromId = msg.SenderId;
            var content = msg.Content;
            
            // 检查是否是绑定的群
            if (CurrentAccount != null && 
                (string.IsNullOrEmpty(CurrentAccount.GroupId) || CurrentAccount.GroupId == groupId))
            {
                Log($"[CDP群消息] 群:{groupId} 发送者:{fromId} 内容:{content}");
                OnGroupMessage?.Invoke(groupId, fromId, content);
            }
        }
        
        #region NIM 事件处理器（避免重复注册）
        
        /// <summary>
        /// NIMService (nim.dll) 消息处理器
        /// </summary>
        private void HandleNimServiceMessage(NIMServiceMessage msg)
        {
            if (msg == null) return;
            
            if (msg.IsGroupMessage) // 群消息
            {
                Log($"[群消息] 群:{msg.ToId} 发送者:{msg.FromId} 内容:{msg.MsgBody}");
                OnGroupMessage?.Invoke(msg.ToId, msg.FromId, msg.MsgBody);
            }
            else // 私聊
            {
                Log($"[私聊] 发送者:{msg.FromId} 内容:{msg.MsgBody}");
                OnPrivateMessage?.Invoke(msg.FromId, msg.ToId, msg.MsgBody);
            }
        }
        
        /// <summary>
        /// NimDirectClient 日志处理器
        /// </summary>
        private void HandleNimClientLog(string msg)
        {
            Log($"[NIM] {msg}");
        }
        
        /// <summary>
        /// NimDirectClient 消息处理器
        /// </summary>
        private void HandleNimClientMessage(NimDirectMessage msg)
        {
            if (msg == null) return;
            
            var isGroupMessage = msg.Scene == "team";
            
            if (isGroupMessage)
            {
                var groupId = msg.To;
                var fromId = msg.From;
                var content = msg.Body;
                
                Log($"[群消息] 群:{groupId} 发送者:{fromId} 内容:{content}");
                OnGroupMessage?.Invoke(groupId, fromId, content);
            }
            else
            {
                var fromId = msg.From;
                var toId = msg.To;
                var content = msg.Body;
                
                Log($"[私聊] 发送者:{fromId} 内容:{content}");
                OnPrivateMessage?.Invoke(fromId, toId, content);
            }
        }
        
        #endregion
        
        private void Log(string msg)
        {
            OnLog?.Invoke($"[BotLogin] {msg}");
        }
    }
    
    /// <summary>
    /// 账号变更结果
    /// </summary>
    public class AccountChangeResult
    {
        /// <summary>操作是否成功</summary>
        public bool Success { get; set; }
        
        /// <summary>结果消息</summary>
        public string Message { get; set; }
        
        /// <summary>是否需要手动获取凭证</summary>
        public bool NeedManualCredentials { get; set; }
        
        /// <summary>账号更新详情</summary>
        public AccountUpdateResult UpdateResult { get; set; }
        
        public override string ToString() => Message ?? (Success ? "成功" : "失败");
    }
}
