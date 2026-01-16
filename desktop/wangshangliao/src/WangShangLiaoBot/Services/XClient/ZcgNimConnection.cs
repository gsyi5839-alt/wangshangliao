using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace WangShangLiaoBot.Services.XClient
{
    /// <summary>
    /// ZCG NIM连接方式 - 基于逆向分析的云信SDK连接实现
    /// 
    /// ZCG连接架构:
    /// ┌──────────────┐    TCP:5749    ┌──────────────┐
    /// │ 621705120.exe│◄──────────────►│  xplugin.exe │
    /// │  (云信客户端)│  HPSocket Pack │  (协议代理)  │
    /// └──────┬───────┘                └──────────────┘
    ///        │
    ///        │ nim.dll
    ///        ▼
    /// ┌──────────────┐
    /// │   旺商聊服务器 │
    /// └──────────────┘
    /// 
    /// 核心配置来自 YX_Client.dll (INI格式):
    /// - APP_KEY: 云信应用Key
    /// - USER_ID: 云信accid
    /// - S_TOKEN: 签名Token
    /// </summary>
    public sealed class ZcgNimConnection
    {
        #region 单例

        private static ZcgNimConnection _instance;
        private static readonly object _lock = new object();

        public static ZcgNimConnection Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                            _instance = new ZcgNimConnection();
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region NIM SDK P/Invoke

        /// <summary>
        /// NIM SDK初始化 (nim_client_init)
        /// </summary>
        [DllImport("nim.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern bool nim_client_init(
            [MarshalAs(UnmanagedType.LPStr)] string appKey,
            [MarshalAs(UnmanagedType.LPStr)] string appDataDir,
            [MarshalAs(UnmanagedType.LPStr)] string appInstallDir,
            [MarshalAs(UnmanagedType.LPStr)] string jsonExtension);

        /// <summary>
        /// NIM SDK登录 (nim_client_login)
        /// </summary>
        [DllImport("nim.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern void nim_client_login(
            [MarshalAs(UnmanagedType.LPStr)] string appKey,
            [MarshalAs(UnmanagedType.LPStr)] string accid,
            [MarshalAs(UnmanagedType.LPStr)] string token,
            [MarshalAs(UnmanagedType.LPStr)] string jsonExtension,
            NimLoginCallback callback,
            IntPtr userData);

        /// <summary>
        /// 获取登录状态
        /// </summary>
        [DllImport("nim.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int nim_client_get_login_state([MarshalAs(UnmanagedType.LPStr)] string jsonExtension);

        /// <summary>
        /// 登出
        /// </summary>
        [DllImport("nim.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern void nim_client_logout(
            int logoutType,
            [MarshalAs(UnmanagedType.LPStr)] string jsonExtension,
            NimLogoutCallback callback,
            IntPtr userData);

        /// <summary>
        /// 清理SDK
        /// </summary>
        [DllImport("nim.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern void nim_client_cleanup([MarshalAs(UnmanagedType.LPStr)] string jsonExtension);

        /// <summary>
        /// 注册断开连接回调
        /// </summary>
        [DllImport("nim.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern void nim_client_reg_disconnect_cb(
            [MarshalAs(UnmanagedType.LPStr)] string jsonExtension,
            NimDisconnectCallback callback,
            IntPtr userData);

        /// <summary>
        /// 注册被踢回调
        /// </summary>
        [DllImport("nim.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern void nim_client_reg_kickout_cb(
            [MarshalAs(UnmanagedType.LPStr)] string jsonExtension,
            NimKickoutCallback callback,
            IntPtr userData);

        /// <summary>
        /// 注册多端登录回调
        /// </summary>
        [DllImport("nim.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern void nim_client_reg_multispot_login_notify_cb(
            [MarshalAs(UnmanagedType.LPStr)] string jsonExtension,
            NimMultispotLoginCallback callback,
            IntPtr userData);

        // 回调委托
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void NimLoginCallback(IntPtr result, IntPtr userData);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void NimLogoutCallback(int code, IntPtr userData);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void NimDisconnectCallback(IntPtr userData);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void NimKickoutCallback(IntPtr result, IntPtr userData);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void NimMultispotLoginCallback(IntPtr result, IntPtr userData);

        #endregion

        #region 属性

        /// <summary>是否已初始化</summary>
        public bool IsInitialized { get; private set; }

        /// <summary>是否已登录</summary>
        public bool IsLoggedIn { get; private set; }

        /// <summary>APP_KEY</summary>
        public string AppKey { get; private set; }

        /// <summary>云信accid</summary>
        public string Accid { get; private set; }

        /// <summary>旺商聊号</summary>
        public string WangShangId { get; private set; }

        /// <summary>UID</summary>
        public string Uid { get; private set; }

        /// <summary>NIM SDK路径</summary>
        public string NimSdkPath { get; set; }

        #endregion

        #region 事件

        public event Action<string> OnLog;
        public event Action OnLoginSuccess;
        public event Action<string> OnLoginFailed;
        public event Action OnDisconnected;
        public event Action<string> OnKickedOut;

        #endregion

        // 保持回调引用，防止GC
        private NimLoginCallback _loginCallback;
        private NimLogoutCallback _logoutCallback;
        private NimDisconnectCallback _disconnectCallback;
        private NimKickoutCallback _kickoutCallback;
        private NimMultispotLoginCallback _multispotCallback;

        private ZcgNimConnection()
        {
        }

        private void Log(string message)
        {
            OnLog?.Invoke($"[ZcgNimConnection] {message}");
        }

        #region 配置读取

        /// <summary>
        /// 从ZCG的YX_Client.dll读取配置
        /// </summary>
        public bool LoadConfigFromZcg(string zcgPath, string accountId)
        {
            try
            {
                // YX_Client.dll实际是INI文件
                var configPath = Path.Combine(zcgPath, "YX_Clinent", accountId, "YX_Client.dll");
                if (!File.Exists(configPath))
                {
                    Log($"配置文件不存在: {configPath}");
                    return false;
                }

                var config = ParseIniFile(configPath);

                if (!config.TryGetValue("SVRINFO", out var section))
                {
                    Log("配置文件中没有[SVRINFO]节");
                    return false;
                }

                AppKey = section.GetValueOrDefault("APP_KEY", "");
                Accid = section.GetValueOrDefault("USER_ID", "");
                WangShangId = section.GetValueOrDefault("OLD_USER", "");
                Uid = section.GetValueOrDefault("UID", "");

                // NIM SDK路径
                NimSdkPath = Path.Combine(zcgPath, "YX_Clinent", accountId);

                Log($"配置加载成功: AppKey={AppKey}, Accid={Accid}, WangShangId={WangShangId}");
                return true;
            }
            catch (Exception ex)
            {
                Log($"加载配置失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 手动设置配置
        /// </summary>
        public void SetConfig(string appKey, string accid, string token, string wangshangId)
        {
            AppKey = appKey;
            Accid = accid;
            WangShangId = wangshangId;
            Log($"配置已设置: AppKey={appKey}, Accid={accid}");
        }

        private Dictionary<string, Dictionary<string, string>> ParseIniFile(string path)
        {
            var result = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            var lines = File.ReadAllLines(path);
            string currentSection = null;

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith(";") || trimmed.StartsWith("#"))
                    continue;

                if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                {
                    currentSection = trimmed.Substring(1, trimmed.Length - 2);
                    if (!result.ContainsKey(currentSection))
                        result[currentSection] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                }
                else if (currentSection != null && trimmed.Contains("="))
                {
                    var idx = trimmed.IndexOf('=');
                    var key = trimmed.Substring(0, idx).Trim();
                    var value = trimmed.Substring(idx + 1).Trim();
                    result[currentSection][key] = value;
                }
            }

            return result;
        }

        #endregion

        #region SDK初始化

        /// <summary>
        /// 初始化NIM SDK (与ZCG相同方式)
        /// </summary>
        public bool Initialize(string sdkPath = null)
        {
            if (IsInitialized)
            {
                Log("SDK已初始化");
                return true;
            }

            try
            {
                sdkPath = sdkPath ?? NimSdkPath ?? Environment.CurrentDirectory;

                // ZCG的global_config配置
                var globalConfig = @"{
                    ""global_config"": {
                        ""db_encrypt_key"": ""YXSDK"",
                        ""use_https"": true,
                        ""sdk_log_level"": 5,
                        ""comm_enca"": 1,
                        ""nego_key_neca"": 1,
                        ""hand_shake_type"": 1,
                        ""preload_attach"": true,
                        ""sync_session_ack"": true,
                        ""team_msg_ack"": false,
                        ""client_antispam"": false,
                        ""ip_protocol_version"": 0,
                        ""login_retry_max_times"": 0
                    }
                }";

                Log($"初始化NIM SDK: AppKey={AppKey}, SdkPath={sdkPath}");

                // 注册回调
                RegisterCallbacks();

                // 初始化SDK
                var result = nim_client_init(AppKey, sdkPath, sdkPath, globalConfig);

                if (result)
                {
                    IsInitialized = true;
                    Log("NIM SDK初始化成功");
                }
                else
                {
                    Log("NIM SDK初始化失败");
                }

                return result;
            }
            catch (DllNotFoundException ex)
            {
                Log($"找不到nim.dll: {ex.Message}");
                Log("请确保nim.dll在程序目录或ZCG目录中");
                return false;
            }
            catch (Exception ex)
            {
                Log($"初始化异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 注册SDK回调
        /// </summary>
        private void RegisterCallbacks()
        {
            // 保持委托引用
            _disconnectCallback = (userData) =>
            {
                Log("连接断开");
                IsLoggedIn = false;
                OnDisconnected?.Invoke();
            };

            _kickoutCallback = (result, userData) =>
            {
                Log("被踢出");
                IsLoggedIn = false;
                OnKickedOut?.Invoke("被其他设备登录");
            };

            _multispotCallback = (result, userData) =>
            {
                Log("多端登录通知");
            };

            nim_client_reg_disconnect_cb("", _disconnectCallback, IntPtr.Zero);
            nim_client_reg_kickout_cb("", _kickoutCallback, IntPtr.Zero);
            nim_client_reg_multispot_login_notify_cb("", _multispotCallback, IntPtr.Zero);
        }

        #endregion

        #region 登录

        /// <summary>
        /// 登录 (使用token)
        /// </summary>
        public Task<bool> LoginAsync(string token)
        {
            var tcs = new TaskCompletionSource<bool>();

            if (!IsInitialized)
            {
                Log("SDK未初始化");
                tcs.SetResult(false);
                return tcs.Task;
            }

            try
            {
                Log($"正在登录: Accid={Accid}");

                _loginCallback = (result, userData) =>
                {
                    try
                    {
                        var resultStr = Marshal.PtrToStringAnsi(result);
                        Log($"登录结果: {resultStr}");

                        // 解析结果
                        if (resultStr != null && resultStr.Contains("\"res_code\":200"))
                        {
                            IsLoggedIn = true;
                            Log("登录成功");
                            OnLoginSuccess?.Invoke();
                            tcs.TrySetResult(true);
                        }
                        else
                        {
                            IsLoggedIn = false;
                            Log($"登录失败: {resultStr}");
                            OnLoginFailed?.Invoke(resultStr);
                            tcs.TrySetResult(false);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log($"处理登录结果异常: {ex.Message}");
                        tcs.TrySetResult(false);
                    }
                };

                nim_client_login(AppKey, Accid, token, "", _loginCallback, IntPtr.Zero);
            }
            catch (Exception ex)
            {
                Log($"登录异常: {ex.Message}");
                tcs.SetResult(false);
            }

            return tcs.Task;
        }

        /// <summary>
        /// 获取登录状态
        /// </summary>
        public int GetLoginState()
        {
            if (!IsInitialized) return -1;
            return nim_client_get_login_state("");
        }

        #endregion

        #region 清理

        /// <summary>
        /// 登出
        /// </summary>
        public Task LogoutAsync()
        {
            var tcs = new TaskCompletionSource<bool>();

            if (!IsLoggedIn)
            {
                tcs.SetResult(true);
                return tcs.Task;
            }

            try
            {
                _logoutCallback = (code, userData) =>
                {
                    IsLoggedIn = false;
                    Log($"已登出, code={code}");
                    tcs.TrySetResult(true);
                };

                nim_client_logout(1, "", _logoutCallback, IntPtr.Zero);
            }
            catch (Exception ex)
            {
                Log($"登出异常: {ex.Message}");
                tcs.SetResult(false);
            }

            return tcs.Task;
        }

        /// <summary>
        /// 清理SDK
        /// </summary>
        public void Cleanup()
        {
            if (!IsInitialized) return;

            try
            {
                nim_client_cleanup("");
                IsInitialized = false;
                IsLoggedIn = false;
                Log("SDK已清理");
            }
            catch (Exception ex)
            {
                Log($"清理异常: {ex.Message}");
            }
        }

        #endregion
    }

    /// <summary>
    /// 字典扩展方法
    /// </summary>
    internal static class DictionaryExtensions
    {
        public static TValue GetValueOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue defaultValue = default)
        {
            return dict.TryGetValue(key, out var value) ? value : defaultValue;
        }
    }
}
