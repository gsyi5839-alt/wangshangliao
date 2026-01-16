using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using WSLFramework.Utils;

namespace WSLFramework.Services
{
    /// <summary>
    /// NIM SDK 服务 - 直接调用 nim.dll 发送消息
    /// 按照旧程序架构: ZCG → HPSocket → xplugin → nim.dll → 云信服务器
    /// </summary>
    public class NIMService : IDisposable
    {
        #region 单例模式
        private static readonly Lazy<NIMService> _instance = 
            new Lazy<NIMService>(() => new NIMService(), LazyThreadSafetyMode.ExecutionAndPublication);
        public static NIMService Instance => _instance.Value;
        #endregion

        #region DLL 导入
        
        // nim.dll 函数签名
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void nim_client_init_callback(IntPtr result, IntPtr user_data);
        
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void nim_login_callback([MarshalAs(UnmanagedType.LPStr)] string result, IntPtr user_data);
        
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void nim_logout_callback(int error_code, IntPtr user_data);
        
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void nim_send_msg_callback([MarshalAs(UnmanagedType.LPStr)] string result, IntPtr user_data);
        
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void nim_receive_msg_callback([MarshalAs(UnmanagedType.LPStr)] string content, 
            [MarshalAs(UnmanagedType.LPStr)] string json_extension, IntPtr user_data);

        // 函数指针
        private delegate bool nim_client_init_delegate(
            [MarshalAs(UnmanagedType.LPStr)] string app_data_dir,
            [MarshalAs(UnmanagedType.LPStr)] string app_install_dir,
            [MarshalAs(UnmanagedType.LPStr)] string json_extension);
        
        private delegate void nim_client_login_delegate(
            [MarshalAs(UnmanagedType.LPStr)] string app_key,
            [MarshalAs(UnmanagedType.LPStr)] string accid,
            [MarshalAs(UnmanagedType.LPStr)] string token,
            [MarshalAs(UnmanagedType.LPStr)] string json_extension,
            nim_login_callback callback,
            IntPtr user_data);
        
        private delegate void nim_client_logout_delegate(
            int logout_type,
            [MarshalAs(UnmanagedType.LPStr)] string json_extension,
            nim_logout_callback callback,
            IntPtr user_data);
        
        private delegate void nim_talk_send_msg_delegate(
            [MarshalAs(UnmanagedType.LPStr)] string json_msg,
            [MarshalAs(UnmanagedType.LPStr)] string json_extension,
            nim_send_msg_callback callback,
            IntPtr user_data);
        
        private delegate void nim_talk_reg_receive_cb_delegate(
            [MarshalAs(UnmanagedType.LPStr)] string json_extension,
            nim_receive_msg_callback callback,
            IntPtr user_data);

        private delegate void nim_client_cleanup_delegate(
            [MarshalAs(UnmanagedType.LPStr)] string json_extension);

        #endregion

        #region 字段
        
        private IntPtr _nimDll;
        private bool _initialized;
        private bool _loggedIn;
        private string _accid;
        private string _nimId;
        private string _token;
        
        // 函数指针
        private nim_client_init_delegate _nim_client_init;
        private nim_client_login_delegate _nim_client_login;
        private nim_client_logout_delegate _nim_client_logout;
        private nim_talk_send_msg_delegate _nim_talk_send_msg;
        private nim_talk_reg_receive_cb_delegate _nim_talk_reg_receive_cb;
        private nim_client_cleanup_delegate _nim_client_cleanup;
        
        // 保持回调引用防止被GC
        private nim_login_callback _loginCallback;
        private nim_logout_callback _logoutCallback;
        private nim_send_msg_callback _sendCallback;
        private nim_receive_msg_callback _receiveCallback;
        
        // 消息队列
        private readonly ConcurrentQueue<NIMServiceMessage> _messageQueue;
        private readonly ConcurrentDictionary<string, TaskCompletionSource<bool>> _pendingSends;
        
        // 配置 - 使用ZCG的AppKey，避免与旺商聊客户端冲突
        public string AppKey { get; set; } = "b03cfcd909dbf05c25163cc8c7e7b6cf";  // ZCG使用的AppKey
        public string NimDllPath { get; set; }
        
        // 状态
        public bool IsInitialized => _initialized;
        public bool IsLoggedIn => _loggedIn;
        public string CurrentAccid => _accid;
        public string CurrentNimId => _nimId;
        
        // 事件
        public event Action<string> OnLog;
        public event Action<bool> OnLoginStateChanged;
        public event Action<NIMServiceMessage> OnMessageReceived;
        
        #endregion

        #region 构造函数
        
        private NIMService()
        {
            _messageQueue = new ConcurrentQueue<NIMServiceMessage>();
            _pendingSends = new ConcurrentDictionary<string, TaskCompletionSource<bool>>();
            
            // 查找 nim.dll 路径
            FindNimDll();
        }
        
        /// <summary>
        /// 当前账号ID (用于YX_Clinent目录)
        /// </summary>
        public string CurrentAccountId { get; set; } = "82840376";
        
        /// <summary>
        /// 获取账号的YX_Clinent目录 (ZCG风格)
        /// </summary>
        public string GetAccountYxDir(string accountId = null)
        {
            accountId = accountId ?? CurrentAccountId;
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "YX_Clinent", accountId);
        }
        
        /// <summary>
        /// 从YX_Client.dll配置文件加载NIM凭证 (ZCG格式)
        /// </summary>
        public (string appKey, string userId, string userPwd, string oldUser) LoadYxClientConfig(string accountId = null)
        {
            var yxDir = GetAccountYxDir(accountId);
            var configPath = Path.Combine(yxDir, "YX_Client.dll");
            
            if (!File.Exists(configPath))
            {
                Log($"YX_Client.dll 不存在: {configPath}");
                return (null, null, null, null);
            }
            
            try
            {
                var lines = File.ReadAllLines(configPath);
                string appKey = null, userId = null, userPwd = null, oldUser = null;
                
                foreach (var line in lines)
                {
                    if (line.StartsWith("APP_KEY=")) appKey = line.Substring(8).Trim();
                    else if (line.StartsWith("USER_ID=")) userId = line.Substring(8).Trim();
                    else if (line.StartsWith("USER_PWD=")) userPwd = line.Substring(9).Trim();
                    else if (line.StartsWith("OLD_USER=")) oldUser = line.Substring(9).Trim();
                }
                
                Log($"从 YX_Client.dll 加载: USER_ID={userId}, OLD_USER={oldUser}");
                return (appKey, userId, userPwd, oldUser);
            }
            catch (Exception ex)
            {
                Log($"读取 YX_Client.dll 失败: {ex.Message}");
                return (null, null, null, null);
            }
        }
        
        /// <summary>
        /// 保存NIM凭证到YX_Client.dll (ZCG格式)
        /// </summary>
        public void SaveYxClientConfig(string accountId, string nimAccid, string nimToken, string appKey = null)
        {
            var yxDir = GetAccountYxDir(accountId);
            Directory.CreateDirectory(yxDir);
            
            var configPath = Path.Combine(yxDir, "YX_Client.dll");
            appKey = appKey ?? AppKey;
            
            var config = string.Format(@"[SVRINFO]
SVR_IP=127.0.0.1
SVR_PORT=5749
APP_KEY={0}
USER_ID={1}
USER_PWD={2}
OLD_USER={3}
OLD_PASS=
S_PASS=[S_PASS]
S_TOKEN=
UID=", appKey, nimAccid, nimToken, accountId);
            
            File.WriteAllText(configPath, config);
            Log($"已保存 YX_Client.dll: {configPath}");
        }
        
        private void FindNimDll()
        {
            // ★★★ 优先从账号的YX_Clinent目录查找nim.dll (ZCG风格) ★★★
            var accountYxDir = GetAccountYxDir();
            var possiblePaths = new[]
            {
                Path.Combine(accountYxDir, "nim.dll"),  // 优先: YX_Clinent/{账号}/nim.dll
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "nim.dll"),
                @"C:\Users\Administrator\Desktop\zcg25.2.15\YX_Clinent\621705120\nim.dll",
            };
            
            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    NimDllPath = path;
                    Log($"找到 nim.dll: {path}");
                    return;
                }
            }
            
            Log("警告: 未找到 nim.dll");
        }
        
        #endregion

        #region 初始化
        
        /// <summary>
        /// 初始化 NIM SDK
        /// ★★★ 参考ZCG架构：使用独立的数据目录和AppKey，避免与客户端冲突 ★★★
        /// </summary>
        public bool Initialize(string dataDir = null)
        {
            if (_initialized)
            {
                Log("NIM SDK 已初始化");
                return true;
            }
            
            if (string.IsNullOrEmpty(NimDllPath) || !File.Exists(NimDllPath))
            {
                Log($"错误: nim.dll 不存在: {NimDllPath}");
                return false;
            }
            
            try
            {
                Log($"正在加载 nim.dll: {NimDllPath}");
                
                // 加载 DLL
                _nimDll = LoadLibrary(NimDllPath);
                if (_nimDll == IntPtr.Zero)
                {
                    var error = Marshal.GetLastWin32Error();
                    Log($"加载 nim.dll 失败, 错误码: {error}");
                    return false;
                }
                
                // 获取函数指针
                if (!LoadFunctions())
                {
                    Log("获取 NIM SDK 函数失败");
                    FreeLibrary(_nimDll);
                    _nimDll = IntPtr.Zero;
                    return false;
                }
                
                // ★★★ 使用ZCG风格的目录结构: YX_Clinent\{账号}\ ★★★
                dataDir = dataDir ?? GetAccountYxDir();
                Directory.CreateDirectory(dataDir);
                
                // ★★★ 使用ZCG的配置格式，支持独立运行 ★★★
                var config = new JavaScriptSerializer().Serialize(new
                {
                    app_key = AppKey,
                    global_config = new
                    {
                        db_encrypt_key = "",
                        preload_attach = true,
                        sdk_log_level = 2,
                        // ★★★ 关键：允许多端登录（如果服务端支持）★★★
                        login_max_retry_times = 0,
                        custom_timeout = 30000,
                        // ★★★ 使用独立的设备ID，避免与客户端冲突 ★★★
                        device_id = "wsl_bot_" + Environment.MachineName
                    }
                });
                
                Log($"NIM SDK 初始化配置: AppKey={AppKey.Substring(0, 8)}..., DataDir={dataDir}");
                
                var result = _nim_client_init(dataDir, "", config);
                if (!result)
                {
                    Log("NIM SDK 初始化失败");
                    return false;
                }
                
                // 注册消息接收回调
                RegisterReceiveCallback();
                
                _initialized = true;
                Log("✓ NIM SDK 初始化成功");
                return true;
            }
            catch (Exception ex)
            {
                Log($"初始化异常: {ex.Message}");
                return false;
            }
        }
        
        private bool LoadFunctions()
        {
            try
            {
                _nim_client_init = GetFunction<nim_client_init_delegate>("nim_client_init");
                _nim_client_login = GetFunction<nim_client_login_delegate>("nim_client_login");
                _nim_client_logout = GetFunction<nim_client_logout_delegate>("nim_client_logout");
                _nim_talk_send_msg = GetFunction<nim_talk_send_msg_delegate>("nim_talk_send_msg");
                _nim_talk_reg_receive_cb = GetFunction<nim_talk_reg_receive_cb_delegate>("nim_talk_reg_receive_cb");
                _nim_client_cleanup = GetFunction<nim_client_cleanup_delegate>("nim_client_cleanup");
                
                return _nim_client_init != null && 
                       _nim_client_login != null && 
                       _nim_talk_send_msg != null;
            }
            catch (Exception ex)
            {
                Log($"加载函数失败: {ex.Message}");
                return false;
            }
        }
        
        private T GetFunction<T>(string name) where T : Delegate
        {
            var ptr = GetProcAddress(_nimDll, name);
            if (ptr == IntPtr.Zero)
            {
                Log($"未找到函数: {name}");
                return null;
            }
            return Marshal.GetDelegateForFunctionPointer<T>(ptr);
        }
        
        private void RegisterReceiveCallback()
        {
            _receiveCallback = OnReceiveMessage;
            _nim_talk_reg_receive_cb?.Invoke("", _receiveCallback, IntPtr.Zero);
            Log("已注册消息接收回调");
        }
        
        private void OnReceiveMessage(string content, string jsonExtension, IntPtr userData)
        {
            try
            {
                var logContent = content?.Length > 300 ? content.Substring(0, 300) + "..." : content;
                Log($"收到消息: {logContent}");
                
                var serializer = new JavaScriptSerializer();
                var data = serializer.Deserialize<Dictionary<string, object>>(content);
                
                if (data == null) return;
                
                // NIM SDK 消息回调格式 (从ZCG逆向分析):
                // {
                //   "content": {
                //     "msg_type": 0,
                //     "to_type": 1,
                //     "from_id": "发送者accid",
                //     "talk_id": "群ID或接收者accid",
                //     "msg_body": "消息内容",
                //     "msg_attach": "附件JSON",
                //     "client_msg_id": "xxx",
                //     "msg_id_server": "xxx",
                //     "time": 时间戳,
                //     "timetag": 时间戳,
                //     "from_nick": "发送者昵称"
                //   }
                // }
                
                // 尝试从 content 字段获取消息
                Dictionary<string, object> msgContent = null;
                
                if (data.ContainsKey("content"))
                {
                    msgContent = data["content"] as Dictionary<string, object>;
                }
                else
                {
                    // 有时候消息直接在根级别
                    msgContent = data;
                }
                
                if (msgContent == null) return;
                
                var msg = new NIMServiceMessage
                {
                    ClientMsgId = GetDictValue(msgContent, "client_msg_id"),
                    FromId = GetDictValue(msgContent, "from_id"),
                    ToId = GetDictValue(msgContent, "talk_id"),
                    ToType = GetDictIntValue(msgContent, "to_type"),
                    MsgType = GetDictIntValue(msgContent, "msg_type"),
                    MsgBody = GetDictValue(msgContent, "msg_body"),
                    MsgAttach = GetDictValue(msgContent, "msg_attach"),
                    Time = GetDictLongValue(msgContent, "time") > 0 
                           ? GetDictLongValue(msgContent, "time") 
                           : GetDictLongValue(msgContent, "timetag"),
                    FromNick = GetDictValue(msgContent, "from_nick"),
                    ServerId = GetDictValue(msgContent, "msg_id_server")
                };
                
                // 忽略自己发送的消息
                if (msg.FromId == _accid)
                {
                    Log($"忽略自己发送的消息");
                    return;
                }
                
                // 记录详细信息
                var typeDesc = msg.ToType == 1 ? "群消息" : "私聊";
                Log($"[{typeDesc}] 从:{msg.FromId}({msg.FromNick}) 到:{msg.ToId} 内容:{msg.MsgBody}");
                
                _messageQueue.Enqueue(msg);
                OnMessageReceived?.Invoke(msg);
            }
            catch (Exception ex)
            {
                Log($"解析消息异常: {ex.Message}");
            }
        }
        
        #endregion

        #region 登录/登出
        
        /// <summary>
        /// 登录 NIM
        /// ★★★ 支持双账号模式: 如果要切换账号，先登出当前账号 ★★★
        /// </summary>
        public Task<bool> LoginAsync(string accid, string token)
        {
            var tcs = new TaskCompletionSource<bool>();
            
            if (!_initialized)
            {
                Log("错误: NIM SDK 未初始化");
                tcs.SetResult(false);
                return tcs.Task;
            }
            
            // 已登录同一账号，直接返回
            if (_loggedIn && _accid == accid)
            {
                Log($"已登录: {accid}");
                tcs.SetResult(true);
                return tcs.Task;
            }
            
            // ★★★ 如果已登录其他账号或尝试登录过其他账号，先登出 ★★★
            if (!string.IsNullOrEmpty(_accid) && _accid != accid)
            {
                Log($"切换账号: {_accid} -> {accid}，先登出...");
                try
                {
                    // 同步登出（非异步，避免复杂性）
                    _nim_client_logout?.Invoke(1, "", null, IntPtr.Zero);
                    _loggedIn = false;
                    System.Threading.Thread.Sleep(500); // 等待登出完成
                }
                catch (Exception ex)
                {
                    Log($"登出异常: {ex.Message}");
                }
            }
            
            _accid = accid;
            _token = token;
            
            Log($"正在登录 NIM: accid={accid}");
            
            _loginCallback = (result, userData) =>
            {
                try
                {
                    Log($"登录回调: {result}");
                    var serializer = new JavaScriptSerializer();
                    var data = serializer.Deserialize<Dictionary<string, object>>(result);
                    
                    if (data != null)
                    {
                        var errCode = data.ContainsKey("err_code") ? Convert.ToInt32(data["err_code"]) : -1;
                        var loginStep = data.ContainsKey("login_step") ? Convert.ToInt32(data["login_step"]) : 0;
                        
                        if (errCode == 200 && loginStep == 3)
                        {
                            _loggedIn = true;
                            _nimId = accid;
                            Log($"✓ NIM 登录成功: {accid}");
                            OnLoginStateChanged?.Invoke(true);
                            tcs.TrySetResult(true);
                        }
                        else if (errCode != 200)
                        {
                            Log($"NIM 登录失败: errCode={errCode}");
                            tcs.TrySetResult(false);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log($"登录回调异常: {ex.Message}");
                    tcs.TrySetResult(false);
                }
            };
            
            var loginConfig = new JavaScriptSerializer().Serialize(new
            {
                login_token = token,
                login_type = 0
            });
            
            _nim_client_login(AppKey, accid, token, loginConfig, _loginCallback, IntPtr.Zero);
            
            // 超时处理
            Task.Delay(15000).ContinueWith(_ =>
            {
                if (!tcs.Task.IsCompleted)
                {
                    Log("登录超时");
                    tcs.TrySetResult(false);
                }
            });
            
            return tcs.Task;
        }
        
        /// <summary>
        /// 登出 NIM
        /// </summary>
        public Task LogoutAsync()
        {
            var tcs = new TaskCompletionSource<bool>();
            
            if (!_loggedIn)
            {
                tcs.SetResult(true);
                return tcs.Task;
            }
            
            Log($"正在登出 NIM: {_accid}");
            
            _logoutCallback = (errorCode, userData) =>
            {
                _loggedIn = false;
                Log($"已登出 NIM: errorCode={errorCode}");
                OnLoginStateChanged?.Invoke(false);
                tcs.TrySetResult(true);
            };
            
            _nim_client_logout?.Invoke(1, "", _logoutCallback, IntPtr.Zero);
            
            return tcs.Task;
        }
        
        #endregion

        #region 发送消息
        
        /// <summary>
        /// 发送群消息 (文本)
        /// to_type: 1=群聊 (team)
        /// msg_type: 0=文本消息
        /// </summary>
        public Task<bool> SendGroupMessageAsync(string tid, string text)
        {
            // NIM SDK 群消息格式: to_type=1 (群), msg_type=0 (文本)
            return SendMessageAsync(tid, 1, 0, text, "");
        }
        
        /// <summary>
        /// 发送群消息 (自定义消息，带 msg_attach)
        /// msg_type: 100=自定义消息
        /// </summary>
        public Task<bool> SendGroupCustomMessageAsync(string tid, string msgAttach)
        {
            return SendMessageAsync(tid, 1, 100, "", msgAttach);
        }
        
        /// <summary>
        /// 发送私聊消息
        /// to_type: 0=私聊 (p2p)
        /// msg_type: 0=文本消息
        /// </summary>
        public Task<bool> SendPrivateMessageAsync(string toAccid, string text)
        {
            return SendMessageAsync(toAccid, 0, 0, text, "");
        }
        
        /// <summary>
        /// 发送消息
        /// 
        /// NIM SDK nim_talk_send_msg 参数格式 (从ZCG逆向分析):
        /// {
        ///   "msg_type": 0,           // 0=文本, 1=图片, 2=语音, 3=视频, 4=地理位置, 5=通知, 6=文件, 10=提示, 100=自定义
        ///   "to_type": 1,            // 0=私聊(p2p), 1=群聊(team)
        ///   "talk_id": "群号或对方accid",
        ///   "msg_body": "消息内容",   // 文本消息内容
        ///   "msg_attach": "{}",       // 附件JSON (自定义消息时使用)
        ///   "client_msg_id": "uuid",  // 客户端消息ID
        ///   "resend_flag": 0,         // 重发标志
        ///   "msg_setting": {          // 可选: 消息设置
        ///     "push_enable": true,
        ///     "need_push_nick": true
        ///   }
        /// }
        /// </summary>
        public Task<bool> SendMessageAsync(string toId, int toType, int msgType, string msgBody, string msgAttach)
        {
            var tcs = new TaskCompletionSource<bool>();
            
            if (!_loggedIn)
            {
                Log("错误: NIM 未登录");
                tcs.SetResult(false);
                return tcs.Task;
            }
            
            var clientMsgId = Guid.NewGuid().ToString("N"); // 32位无连字符
            _pendingSends[clientMsgId] = tcs;
            
            // ★★★ 按照ZCG逆向分析的NIM消息格式构建 ★★★
            var msgDict = new Dictionary<string, object>
            {
                // 必填字段
                { "msg_type", msgType },        // 消息类型
                { "to_type", toType },          // 会话类型: 0=私聊, 1=群聊
                { "talk_id", toId },            // 接收者ID (群ID或用户accid)
                { "msg_body", msgBody ?? "" },  // 消息内容
                { "client_msg_id", clientMsgId },// 客户端消息ID
                { "resend_flag", 0 },           // 重发标志
                
                // 时间戳
                { "timetag", DateTimeOffset.Now.ToUnixTimeMilliseconds() },
                
                // 推送设置
                { "push_enable", true },
                { "need_push_nick", true },
                { "need_badge", true },
                
                // 消息设置
                { "anti_spam_enable", false },
                { "client_anti_spam", false },
            };
            
            // 添加附件 (自定义消息)
            if (!string.IsNullOrEmpty(msgAttach))
            {
                msgDict["msg_attach"] = msgAttach;
            }
            
            // 发送者信息 (可选)
            if (!string.IsNullOrEmpty(_accid))
            {
                msgDict["from_id"] = _accid;
            }
            
            var jsonMsg = new JavaScriptSerializer().Serialize(msgDict);
            
            var logContent = msgBody?.Length > 50 ? msgBody.Substring(0, 50) + "..." : msgBody;
            Log($"发送消息: to={toId}, type={(toType == 1 ? "群聊" : "私聊")}, msgType={msgType}, body={logContent}");
            
            _sendCallback = (result, userData) =>
            {
                try
                {
                    var logResult = result?.Length > 150 ? result.Substring(0, 150) + "..." : result;
                    Log($"发送回调: {logResult}");
                    
                    var serializer = new JavaScriptSerializer();
                    var data = serializer.Deserialize<Dictionary<string, object>>(result);
                    
                    if (data != null)
                    {
                        // NIM SDK 返回格式: {"rescode": 200, "msg_id_server": "xxx", "timetag": xxx}
                        var rescode = data.ContainsKey("rescode") ? Convert.ToInt32(data["rescode"]) : -1;
                        var success = rescode == 200;
                        
                        if (_pendingSends.TryRemove(clientMsgId, out var pendingTcs))
                        {
                            pendingTcs.TrySetResult(success);
                        }
                        
                        if (success)
                        {
                            var serverId = data.ContainsKey("msg_id_server") ? data["msg_id_server"]?.ToString() : "";
                            Log($"✓ 消息发送成功: {toId}, serverId={serverId}");
                        }
                        else
                        {
                            var errMsg = data.ContainsKey("error_msg") ? data["error_msg"]?.ToString() : "";
                            Log($"消息发送失败: rescode={rescode}, error={errMsg}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log($"发送回调异常: {ex.Message}");
                    if (_pendingSends.TryRemove(clientMsgId, out var pendingTcs))
                    {
                        pendingTcs.TrySetResult(false);
                    }
                }
            };
            
            _nim_talk_send_msg(jsonMsg, "", _sendCallback, IntPtr.Zero);
            
            // 超时处理
            Task.Delay(10000).ContinueWith(_ =>
            {
                if (_pendingSends.TryRemove(clientMsgId, out var pendingTcs))
                {
                    Log("发送超时");
                    pendingTcs.TrySetResult(false);
                }
            });
            
            return tcs.Task;
        }
        
        /// <summary>
        /// 发送图片消息
        /// msg_type: 1=图片消息
        /// </summary>
        public Task<bool> SendImageMessageAsync(string toId, int toType, string imageUrl, int width, int height)
        {
            var attach = new JavaScriptSerializer().Serialize(new
            {
                url = imageUrl,
                w = width,
                h = height,
                ext = "jpg"
            });
            return SendMessageAsync(toId, toType, 1, "", attach);
        }
        
        /// <summary>
        /// 发送提示消息
        /// msg_type: 10=提示消息 (系统提示，不计入未读)
        /// </summary>
        public Task<bool> SendTipMessageAsync(string toId, int toType, string tipContent)
        {
            return SendMessageAsync(toId, toType, 10, tipContent, "");
        }
        
        #endregion

        #region 辅助方法
        
        private void Log(string message)
        {
            var logMsg = $"[NIM] {message}";
            Logger.Info(logMsg);
            OnLog?.Invoke(logMsg);
        }
        
        private string GetDictValue(Dictionary<string, object> dict, string key)
        {
            return dict.TryGetValue(key, out var value) ? value?.ToString() : "";
        }
        
        private int GetDictIntValue(Dictionary<string, object> dict, string key)
        {
            return dict.TryGetValue(key, out var value) ? Convert.ToInt32(value) : 0;
        }
        
        private long GetDictLongValue(Dictionary<string, object> dict, string key)
        {
            return dict.TryGetValue(key, out var value) ? Convert.ToInt64(value) : 0;
        }
        
        public void Dispose()
        {
            if (_loggedIn)
            {
                LogoutAsync().Wait(5000);
            }
            
            _nim_client_cleanup?.Invoke("");
            
            if (_nimDll != IntPtr.Zero)
            {
                FreeLibrary(_nimDll);
                _nimDll = IntPtr.Zero;
            }
            
            _initialized = false;
            Log("NIM SDK 已释放");
        }
        
        #endregion

        #region Win32 API
        
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr LoadLibrary(string lpFileName);
        
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeLibrary(IntPtr hModule);
        
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);
        
        #endregion
    }
    
    #region 辅助类
    
    // NIMMessage 类已在 NIMMessageParser.cs 中定义，这里不再重复
    // 使用 NIMServiceMessage 作为服务内部消息类
    
    /// <summary>
    /// NIM 服务消息 (内部使用)
    /// 消息类型 msg_type:
    ///   0=文本, 1=图片, 2=语音, 3=视频, 4=地理位置, 
    ///   5=通知, 6=文件, 10=提示, 100=自定义
    /// 会话类型 to_type:
    ///   0=私聊(p2p), 1=群聊(team)
    /// </summary>
    public class NIMServiceMessage
    {
        public string ClientMsgId { get; set; }
        public string ServerId { get; set; }
        public string FromId { get; set; }
        public string FromNick { get; set; }
        public string ToId { get; set; }
        public int ToType { get; set; }  // 0=私聊, 1=群聊
        public int MsgType { get; set; }  // 0=文本, 1=图片, 2=语音, 100=自定义
        public string MsgBody { get; set; }
        public string MsgAttach { get; set; }
        public long Time { get; set; }
        
        // 便捷属性
        public bool IsGroupMessage => ToType == 1;
        public bool IsPrivateMessage => ToType == 0;
        public bool IsTextMessage => MsgType == 0;
        public bool IsImageMessage => MsgType == 1;
        public bool IsVoiceMessage => MsgType == 2;
        public bool IsCustomMessage => MsgType == 100;
        public bool IsTipMessage => MsgType == 10;
        
        /// <summary>获取消息类型描述</summary>
        public string MsgTypeDesc
        {
            get
            {
                switch (MsgType)
                {
                    case 0: return "文本";
                    case 1: return "图片";
                    case 2: return "语音";
                    case 3: return "视频";
                    case 4: return "位置";
                    case 5: return "通知";
                    case 6: return "文件";
                    case 10: return "提示";
                    case 100: return "自定义";
                    default: return $"未知({MsgType})";
                }
            }
        }
    }
    
    #endregion
}
