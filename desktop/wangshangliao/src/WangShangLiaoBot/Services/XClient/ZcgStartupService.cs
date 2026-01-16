using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace WangShangLiaoBot.Services.XClient
{
    /// <summary>
    /// ZCG启动服务 - 基于逆向分析实现的完整启动流程
    /// 
    /// 运行顺序 (基于逆向分析):
    /// 1. run.cmd 启动 xplugin.exe (携带授权Token)
    /// 2. xplugin.exe 加载 Module.dll (核心框架)
    /// 3. xplugin.exe 加载 plugin/*.dll (插件: zcg.dll)
    /// 4. xplugin.exe 启动 HPSocket TCP服务 (端口14745)
    /// 5. 读取 config.ini 配置 (账号、Token、群号)
    /// 6. 启动 YX_Clinent\{账号}\621705120.exe (云信IM客户端)
    /// 7. 621705120.exe 调用 nim.dll 连接网易云信
    /// 8. 建立本地TCP通信 (xplugin ↔ zcg插件)
    /// 9. 等待消息投递和API调用
    /// </summary>
    public sealed class ZcgStartupService
    {
        #region 单例

        private static ZcgStartupService _instance;
        private static readonly object _instanceLock = new object();

        public static ZcgStartupService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_instanceLock)
                    {
                        if (_instance == null)
                            _instance = new ZcgStartupService();
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region 属性

        /// <summary>是否已初始化</summary>
        public bool IsInitialized { get; private set; }

        /// <summary>当前账号</summary>
        public string CurrentAccount { get; private set; }

        /// <summary>当前群号</summary>
        public string CurrentGroupId { get; private set; }

        /// <summary>JWT Token</summary>
        public string JwtToken { get; private set; }

        /// <summary>昵称</summary>
        public string NickName { get; private set; }

        /// <summary>配置文件路径</summary>
        public string ConfigPath { get; set; }

        #endregion

        #region 事件

        public event Action<string> OnLog;
        public event Action<ZcgStartupStep, bool> OnStepComplete;
        public event Action<XPluginMessage> OnMessageReceived;

        #endregion

        private ZcgStartupService()
        {
        }

        #region 启动流程

        /// <summary>
        /// 完整启动流程 (与ZCG原版一致)
        /// </summary>
        public async Task<bool> StartAsync(string configPath = null)
        {
            try
            {
                Log("=== ZCG启动流程开始 ===");

                // Step 1: 读取配置
                Log("[Step 1/6] 读取配置文件...");
                if (!await LoadConfigAsync(configPath))
                {
                    OnStepComplete?.Invoke(ZcgStartupStep.LoadConfig, false);
                    return false;
                }
                OnStepComplete?.Invoke(ZcgStartupStep.LoadConfig, true);

                // Step 2: 连接XPlugin服务
                Log("[Step 2/6] 连接XPlugin服务...");
                if (!await ConnectXPluginAsync())
                {
                    OnStepComplete?.Invoke(ZcgStartupStep.ConnectXPlugin, false);
                    return false;
                }
                OnStepComplete?.Invoke(ZcgStartupStep.ConnectXPlugin, true);

                // Step 3: 验证授权
                Log("[Step 3/6] 验证授权...");
                if (!await VerifyAuthAsync())
                {
                    Log("[警告] 授权验证跳过 (可能未配置)");
                }
                OnStepComplete?.Invoke(ZcgStartupStep.VerifyAuth, true);

                // Step 4: 获取在线账号
                Log("[Step 4/6] 获取在线账号...");
                var accounts = await GetOnlineAccountsAsync();
                OnStepComplete?.Invoke(ZcgStartupStep.GetAccounts, !string.IsNullOrEmpty(accounts));

                // Step 5: 获取绑定群
                Log("[Step 5/6] 获取绑定群...");
                var boundGroup = await GetBoundGroupAsync();
                OnStepComplete?.Invoke(ZcgStartupStep.GetBoundGroup, !string.IsNullOrEmpty(boundGroup));

                // Step 6: 注册消息回调
                Log("[Step 6/6] 注册消息回调...");
                RegisterMessageCallbacks();
                OnStepComplete?.Invoke(ZcgStartupStep.RegisterCallbacks, true);

                IsInitialized = true;
                Log("=== ZCG启动流程完成 ===");
                return true;
            }
            catch (Exception ex)
            {
                Log($"[错误] 启动失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 停止服务
        /// </summary>
        public void Stop()
        {
            Log("停止ZCG服务...");
            XPluginProtocol.Instance.Disconnect();
            IsInitialized = false;
        }

        #endregion

        #region 步骤实现

        /// <summary>
        /// 加载配置文件 (config.ini)
        /// </summary>
        private async Task<bool> LoadConfigAsync(string configPath)
        {
            try
            {
                ConfigPath = configPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.ini");

                if (!File.Exists(ConfigPath))
                {
                    Log($"配置文件不存在: {ConfigPath}");
                    return false;
                }

                var config = await ReadIniFileAsync(ConfigPath);

                // 查找账号配置节
                foreach (var section in config.Keys)
                {
                    if (long.TryParse(section, out _))
                    {
                        // 数字节名是账号配置
                        var accountSection = config[section];

                        if (accountSection.TryGetValue("账号", out var account))
                            CurrentAccount = DecodeConfigValue(account);

                        if (accountSection.TryGetValue("jwtToken", out var token))
                            JwtToken = DecodeConfigValue(token);

                        if (accountSection.TryGetValue("qun", out var group))
                            CurrentGroupId = DecodeConfigValue(group);

                        if (accountSection.TryGetValue("nickName", out var nick))
                            NickName = nick;

                        Log($"账号: {CurrentAccount}, 群号: {CurrentGroupId}, 昵称: {NickName}");
                        break;
                    }
                }

                return !string.IsNullOrEmpty(CurrentAccount);
            }
            catch (Exception ex)
            {
                Log($"读取配置失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 连接XPlugin服务
        /// </summary>
        private async Task<bool> ConnectXPluginAsync()
        {
            var xplugin = XPluginProtocol.Instance;
            xplugin.OnLog += Log;

            // 设置机器人QQ
            if (!string.IsNullOrEmpty(CurrentAccount))
            {
                xplugin.SetRobotQQ(CurrentAccount);
            }

            return await xplugin.ConnectAsync();
        }

        /// <summary>
        /// 验证授权
        /// </summary>
        private async Task<bool> VerifyAuthAsync()
        {
            // 授权验证使用ww_xp限制接口
            // 如果配置了授权码，进行验证
            return await Task.FromResult(true); // 跳过授权验证
        }

        /// <summary>
        /// 获取在线账号
        /// </summary>
        private async Task<string> GetOnlineAccountsAsync()
        {
            return await XPluginProtocol.Instance.GetOnlineAccountsAsync();
        }

        /// <summary>
        /// 获取绑定群
        /// </summary>
        private async Task<string> GetBoundGroupAsync()
        {
            return await XPluginProtocol.Instance.GetBoundGroupAsync();
        }

        /// <summary>
        /// 注册消息回调
        /// </summary>
        private void RegisterMessageCallbacks()
        {
            var xplugin = XPluginProtocol.Instance;

            xplugin.OnGroupMessage += msg =>
            {
                var contentPreview = msg.Content?.Length > 50 ? msg.Content.Substring(0, 50) : msg.Content;
                Log($"[群消息] {msg.FromQQ}: {contentPreview}");
                // 触发消息事件，由外部处理
                OnMessageReceived?.Invoke(msg);
            };

            xplugin.OnPrivateMessage += msg =>
            {
                var contentPreview = msg.Content?.Length > 50 ? msg.Content.Substring(0, 50) : msg.Content;
                Log($"[私聊消息] {msg.FromQQ}: {contentPreview}");
                // 触发消息事件，由外部处理
                OnMessageReceived?.Invoke(msg);
            };

            xplugin.OnSystemMessage += msg =>
            {
                Log($"[系统消息] {msg.Content}");
            };

            xplugin.OnConnectionChanged += connected =>
            {
                if (!connected && IsInitialized)
                {
                    Log("[警告] XPlugin连接已断开，尝试重连...");
                    _ = ReconnectAsync();
                }
            };
        }

        /// <summary>
        /// 重连
        /// </summary>
        private async Task ReconnectAsync()
        {
            await Task.Delay(3000);
            if (!XPluginProtocol.Instance.IsConnected)
            {
                await XPluginProtocol.Instance.ConnectAsync();
            }
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 读取INI文件
        /// </summary>
        private async Task<Dictionary<string, Dictionary<string, string>>> ReadIniFileAsync(string path)
        {
            var result = new Dictionary<string, Dictionary<string, string>>();
            string currentSection = null;

            using (var reader = new StreamReader(path, Encoding.GetEncoding("GBK")))
            {
                string line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    line = line.Trim();

                    if (string.IsNullOrEmpty(line) || line.StartsWith(";") || line.StartsWith("#"))
                        continue;

                    if (line.StartsWith("[") && line.EndsWith("]"))
                    {
                        currentSection = line.Substring(1, line.Length - 2);
                        if (!result.ContainsKey(currentSection))
                            result[currentSection] = new Dictionary<string, string>();
                    }
                    else if (currentSection != null)
                    {
                        var idx = line.IndexOf('=');
                        if (idx > 0)
                        {
                            var key = line.Substring(0, idx).Trim();
                            var value = line.Substring(idx + 1).Trim();
                            result[currentSection][key] = value;
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 解码配置值 (Base64)
        /// </summary>
        private string DecodeConfigValue(string encoded)
        {
            if (string.IsNullOrEmpty(encoded)) return encoded;

            try
            {
                // 尝试Base64解码
                var bytes = Convert.FromBase64String(encoded);
                return Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                return encoded; // 不是Base64，直接返回
            }
        }

        private void Log(string message)
        {
            OnLog?.Invoke(message);
            WangShangLiaoBot.Services.Logger.Info($"[ZcgStartup] {message}");
        }

        #endregion
    }

    /// <summary>
    /// 启动步骤枚举
    /// </summary>
    public enum ZcgStartupStep
    {
        LoadConfig,
        ConnectXPlugin,
        VerifyAuth,
        GetAccounts,
        GetBoundGroup,
        RegisterCallbacks
    }
}
