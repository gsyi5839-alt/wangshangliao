using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using HPSocket;
using HPSocket.Tcp;
using WangShangLiaoBot.Models;
using WangShangLiaoBot.Utils;

namespace WangShangLiaoBot.Services.XClient
{
    /// <summary>
    /// YX SDK代理服务器 - 模拟xplugin.exe的HPSocket服务
    /// 
    /// 破解分析 (基于逆向621705120.exe):
    /// ┌─────────────────────────────────────────────────────────────────┐
    /// │ 621705120.exe 使用 HPSocket TcpPackClient 连接 xplugin.exe     │
    /// │                                                                 │
    /// │ 协议: HPSocket PACK 模式                                        │
    /// │ - PackHeaderFlag = 0xFF                                         │
    /// │ - MaxPackSize = 16MB                                            │
    /// │ - 编码: UTF-8                                                   │
    /// │                                                                 │
    /// │ 消息格式 (JSON):                                                │
    /// │ {                                                               │
    /// │   "type": "消息类型",                                           │
    /// │   "data": { ... }                                               │
    /// │ }                                                               │
    /// │                                                                 │
    /// │ 消息类型:                                                       │
    /// │ - auth_req       认证请求 (621705120→xplugin)                  │
    /// │ - auth_resp      认证响应 (xplugin→621705120)                  │
    /// │ - nim_login      NIM登录请求                                    │
    /// │ - nim_login_resp NIM登录响应                                    │
    /// │ - send_msg       发送消息请求                                   │
    /// │ - recv_msg       接收消息推送                                   │
    /// │ - heartbeat      心跳                                           │
    /// └─────────────────────────────────────────────────────────────────┘
    /// </summary>
    public sealed class YxSdkProxyServer : IDisposable
    {
        #region 单例

        private static YxSdkProxyServer _instance;
        private static readonly object _lock = new object();

        public static YxSdkProxyServer Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                            _instance = new YxSdkProxyServer();
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region 常量

        /// <summary>xplugin监听端口 (从YX_Client.dll配置读取: SVR_PORT=5749)</summary>
        public const ushort XPLUGIN_PORT = 5749;
        
        /// <summary>Pack协议头标志</summary>
        public const ushort PACK_HEADER_FLAG = 0xFF;
        
        /// <summary>最大包大小</summary>
        public const uint MAX_PACK_SIZE = 0x1000000; // 16MB

        #endregion

        #region 字段

        private TcpPackServer _server;
        private bool _isRunning;
        private IntPtr _clientConnId;
        private readonly ConcurrentDictionary<IntPtr, YxClientInfo> _clients = new ConcurrentDictionary<IntPtr, YxClientInfo>();
        
        // NIM凭证 (从YX_Client.dll读取)
        private string _appKey;
        private string _userId;     // accid
        private string _userPwd;    // token
        private string _oldUser;    // 旺商聊号
        private string _sToken;     // 签名token
        private string _uid;        // 内部用户ID

        #endregion

        #region 属性

        public bool IsRunning => _isRunning;
        public int ClientCount => _clients.Count;
        public IntPtr ActiveClientId => _clientConnId;

        #endregion

        #region 事件

        public event Action<string> OnLog;
        public event Action<IntPtr, string, object> OnClientMessage;
        public event Action<IntPtr> OnClientConnected;
        public event Action<IntPtr> OnClientDisconnected;
        public event Action<string, string, string> OnNimLoginRequest; // appKey, accid, token
        public event Action<string, string, string> OnSendMessageRequest; // scene, targetId, content

        #endregion

        private YxSdkProxyServer()
        {
        }

        private void Log(string message)
        {
            var msg = $"[YxSdkProxy] {message}";
            OnLog?.Invoke(msg);
            Logger.Info(msg);
            Console.WriteLine(msg);
        }

        #region 配置加载

        /// <summary>
        /// 从ZCG配置加载NIM凭证
        /// </summary>
        public void LoadNimCredentials(string zcgPath)
        {
            try
            {
                var configPath = System.IO.Path.Combine(zcgPath, "YX_Clinent", "621705120", "YX_Client.dll");
                if (!System.IO.File.Exists(configPath))
                {
                    Log($"配置文件不存在: {configPath}");
                    return;
                }

                var lines = System.IO.File.ReadAllLines(configPath);
                foreach (var line in lines)
                {
                    if (line.Contains("="))
                    {
                        var parts = line.Split(new[] { '=' }, 2);
                        if (parts.Length == 2)
                        {
                            var key = parts[0].Trim();
                            var value = parts[1].Trim();
                            
                            switch (key)
                            {
                                case "APP_KEY": _appKey = value; break;
                                case "USER_ID": _userId = value; break;
                                case "USER_PWD": _userPwd = value; break;
                                case "OLD_USER": _oldUser = value; break;
                                case "S_TOKEN": _sToken = value; break;
                                case "UID": _uid = value; break;
                            }
                        }
                    }
                }

                Log($"NIM凭证已加载: AppKey={_appKey?.Substring(0, 8)}..., UserId={_userId}, OldUser={_oldUser}");
            }
            catch (Exception ex)
            {
                Log($"加载NIM凭证失败: {ex.Message}");
            }
        }

        #endregion

        #region 服务控制

        /// <summary>
        /// 启动代理服务器
        /// </summary>
        public bool Start(ushort port = XPLUGIN_PORT)
        {
            if (_isRunning)
            {
                Log("服务已在运行");
                return true;
            }

            try
            {
                _server = new TcpPackServer();
                
                // ★★★ 关键: Pack协议配置必须匹配621705120.exe的客户端设置 ★★★
                _server.PackHeaderFlag = PACK_HEADER_FLAG;
                _server.MaxPackSize = MAX_PACK_SIZE;
                
                // 设置地址和端口 (HPSocket.Net的正确方式)
                _server.Address = "0.0.0.0";
                _server.Port = port;
                
                // 绑定事件
                _server.OnPrepareListen += Server_OnPrepareListen;
                _server.OnAccept += Server_OnAccept;
                _server.OnReceive += Server_OnReceive;
                _server.OnClose += Server_OnClose;
                _server.OnShutdown += Server_OnShutdown;

                // 启动服务 (无参数调用)
                if (!_server.Start())
                {
                    Log($"启动失败: {_server.ErrorMessage} (错误码: {_server.ErrorCode})");
                    return false;
                }

                _isRunning = true;
                Log($"========================================");
                Log($"  YX SDK代理服务器已启动");
                Log($"  监听端口: {port}");
                Log($"  PackHeaderFlag: 0x{PACK_HEADER_FLAG:X2}");
                Log($"  MaxPackSize: {MAX_PACK_SIZE / 1024 / 1024}MB");
                Log($"  等待621705120.exe连接...");
                Log($"========================================");
                return true;
            }
            catch (Exception ex)
            {
                Log($"启动异常: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// 停止服务器
        /// </summary>
        public void Stop()
        {
            if (!_isRunning) return;

            try
            {
                _server?.Stop();
                _server?.Dispose();
                _server = null;
                _clients.Clear();
                _clientConnId = IntPtr.Zero;
                _isRunning = false;
                Log("代理服务器已停止");
            }
            catch (Exception ex)
            {
                Log($"停止异常: {ex.Message}");
            }
        }

        #endregion

        #region HPSocket事件处理

        private HandleResult Server_OnPrepareListen(IServer sender, IntPtr listen)
        {
            Log($"服务器准备监听: {listen}");
            return HandleResult.Ok;
        }

        private HandleResult Server_OnAccept(IServer sender, IntPtr connId, IntPtr client)
        {
            var clientInfo = new YxClientInfo
            {
                ConnId = connId,
                ConnectTime = DateTime.Now,
                IsAuthenticated = false
            };
            
            _clients[connId] = clientInfo;
            _clientConnId = connId;
            
            Log($"★ 客户端已连接: ConnId={connId}");
            OnClientConnected?.Invoke(connId);
            
            // 立即发送认证响应 (模拟xplugin已准备好)
            SendAuthResponse(connId, true);
            
            return HandleResult.Ok;
        }

        private HandleResult Server_OnReceive(IServer sender, IntPtr connId, byte[] data)
        {
            try
            {
                var message = Encoding.UTF8.GetString(data);
                Log($"[收到] ConnId={connId}, Length={data.Length}");
                Log($"[数据] {message}");
                
                // 解析并处理消息
                ProcessMessage(connId, message);
            }
            catch (Exception ex)
            {
                Log($"处理消息异常: {ex.Message}");
            }
            
            return HandleResult.Ok;
        }

        private HandleResult Server_OnClose(IServer sender, IntPtr connId, SocketOperation socketOperation, int errorCode)
        {
            _clients.TryRemove(connId, out _);
            
            if (connId == _clientConnId)
                _clientConnId = IntPtr.Zero;
            
            Log($"客户端断开: ConnId={connId}, Operation={socketOperation}, Error={errorCode}");
            OnClientDisconnected?.Invoke(connId);
            
            return HandleResult.Ok;
        }

        private HandleResult Server_OnShutdown(IServer sender)
        {
            Log("服务器已关闭");
            _isRunning = false;
            return HandleResult.Ok;
        }

        #endregion

        #region 消息处理

        /// <summary>
        /// 处理客户端消息
        /// </summary>
        private void ProcessMessage(IntPtr connId, string message)
        {
            try
            {
                // 尝试JSON解析
                if (message.TrimStart().StartsWith("{"))
                {
                    var serializer = new JavaScriptSerializer();
                    var root = serializer.Deserialize<Dictionary<string, object>>(message);
                    
                    string msgType = "";
                    if (root.ContainsKey("type"))
                        msgType = root["type"]?.ToString();
                    else if (root.ContainsKey("cmd"))
                        msgType = root["cmd"]?.ToString();
                    else if (root.ContainsKey("action"))
                        msgType = root["action"]?.ToString();
                    
                    Log($"[消息类型] {msgType}");
                    OnClientMessage?.Invoke(connId, msgType, root);
                    
                    // 根据消息类型处理
                    switch (msgType?.ToLower())
                    {
                        case "auth":
                        case "auth_req":
                        case "login":
                            HandleAuthRequest(connId, root);
                            break;
                            
                        case "nim_login":
                        case "nim_connect":
                            HandleNimLoginRequest(connId, root);
                            break;
                            
                        case "send_msg":
                        case "send":
                        case "message":
                            HandleSendMessage(connId, root);
                            break;
                            
                        case "heartbeat":
                        case "ping":
                            HandleHeartbeat(connId);
                            break;
                            
                        case "get_info":
                        case "query":
                            HandleQuery(connId, root);
                            break;
                            
                        default:
                            // 未知消息类型，返回通用成功响应
                            SendResponse(connId, msgType, 0, "ok");
                            break;
                    }
                }
                else
                {
                    // 非JSON格式，可能是文本命令
                    HandleTextCommand(connId, message);
                }
            }
            catch (ArgumentException)
            {
                // JSON解析失败，尝试作为文本命令处理
                HandleTextCommand(connId, message);
            }
            catch (Exception ex)
            {
                Log($"处理消息异常: {ex.Message}");
                SendResponse(connId, "error", -1, ex.Message);
            }
        }

        /// <summary>
        /// 处理认证请求
        /// </summary>
        private void HandleAuthRequest(IntPtr connId, Dictionary<string, object> root)
        {
            Log("[处理] 认证请求");
            
            if (_clients.TryGetValue(connId, out var client))
            {
                client.IsAuthenticated = true;
            }
            
            SendAuthResponse(connId, true);
        }

        /// <summary>
        /// 处理NIM登录请求
        /// </summary>
        private void HandleNimLoginRequest(IntPtr connId, Dictionary<string, object> root)
        {
            Log("[处理] NIM登录请求");
            
            string appKey = _appKey;
            string accid = _userId;
            string token = _userPwd;
            
            // 尝试从请求中获取凭证
            if (root.ContainsKey("app_key"))
                appKey = root["app_key"]?.ToString();
            if (root.ContainsKey("accid"))
                accid = root["accid"]?.ToString();
            if (root.ContainsKey("token"))
                token = root["token"]?.ToString();
            
            Log($"NIM登录: AppKey={appKey?.Substring(0, 8)}..., Accid={accid}");
            
            // 触发NIM登录事件
            OnNimLoginRequest?.Invoke(appKey, accid, token);
            
            // 返回登录成功响应
            var response = new Dictionary<string, object>
            {
                { "type", "nim_login_resp" },
                { "code", 0 },
                { "msg", "login success" },
                { "data", new Dictionary<string, object>
                    {
                        { "accid", accid },
                        { "old_user", _oldUser },
                        { "uid", _uid }
                    }
                }
            };
            
            SendJson(connId, response);
        }

        /// <summary>
        /// 处理发送消息请求
        /// </summary>
        private void HandleSendMessage(IntPtr connId, Dictionary<string, object> root)
        {
            Log("[处理] 发送消息请求");
            
            string scene = "team";  // 默认群消息
            string targetId = "";
            string content = "";
            
            if (root.ContainsKey("scene"))
                scene = root["scene"]?.ToString();
            if (root.ContainsKey("to"))
                targetId = root["to"]?.ToString();
            if (root.ContainsKey("target"))
                targetId = root["target"]?.ToString();
            if (root.ContainsKey("content"))
                content = root["content"]?.ToString();
            if (root.ContainsKey("msg"))
                content = root["msg"]?.ToString();
            
            Log($"发送消息: Scene={scene}, Target={targetId}, Content={content?.Substring(0, Math.Min(50, content?.Length ?? 0))}...");
            
            // 触发发送消息事件
            OnSendMessageRequest?.Invoke(scene, targetId, content);
            
            // 返回发送成功响应
            SendResponse(connId, "send_msg_resp", 0, "send success");
        }

        /// <summary>
        /// 处理心跳
        /// </summary>
        private void HandleHeartbeat(IntPtr connId)
        {
            var response = new Dictionary<string, object>
            {
                { "type", "heartbeat_ack" },
                { "code", 0 },
                { "time", DateTimeOffset.Now.ToUnixTimeMilliseconds() }
            };
            
            SendJson(connId, response);
        }

        /// <summary>
        /// 处理查询请求
        /// </summary>
        private void HandleQuery(IntPtr connId, Dictionary<string, object> root)
        {
            Log("[处理] 查询请求");
            
            var response = new Dictionary<string, object>
            {
                { "type", "query_resp" },
                { "code", 0 },
                { "data", new Dictionary<string, object>
                    {
                        { "old_user", _oldUser },
                        { "uid", _uid },
                        { "status", "online" }
                    }
                }
            };
            
            SendJson(connId, response);
        }

        /// <summary>
        /// 处理文本命令 (ZCG API格式)
        /// </summary>
        private void HandleTextCommand(IntPtr connId, string command)
        {
            Log($"[文本命令] {command}");
            
            // ZCG API格式: 命令|参数1|参数2|...
            var parts = command.Split('|');
            if (parts.Length == 0) return;
            
            var cmd = parts[0];
            
            switch (cmd)
            {
                case "发送群消息":
                case "发送群消息（文本）":
                case "Group_SendMsg":
                    if (parts.Length >= 4)
                    {
                        // 发送群消息（文本）|{机器人号}|{内容}|{群号}|1|0
                        var robotQQ = parts.Length > 1 ? parts[1] : "";
                        var content = parts.Length > 2 ? parts[2] : "";
                        var groupId = parts.Length > 3 ? parts[3] : "";
                        
                        OnSendMessageRequest?.Invoke("team", groupId, content);
                        SendTextResponse(connId, "0");
                    }
                    break;
                    
                case "发送好友消息":
                case "P2P_SendMsg":
                    if (parts.Length >= 4)
                    {
                        var robotQQ = parts.Length > 1 ? parts[1] : "";
                        var content = parts.Length > 2 ? parts[2] : "";
                        var targetId = parts.Length > 3 ? parts[3] : "";
                        
                        OnSendMessageRequest?.Invoke("p2p", targetId, content);
                        SendTextResponse(connId, "0");
                    }
                    break;
                    
                default:
                    // 返回通用成功
                    SendTextResponse(connId, "0");
                    break;
            }
        }

        #endregion

        #region 发送方法

        /// <summary>
        /// 发送认证响应
        /// </summary>
        private void SendAuthResponse(IntPtr connId, bool success)
        {
            var response = new Dictionary<string, object>
            {
                { "type", "auth_resp" },
                { "code", success ? 0 : -1 },
                { "msg", success ? "ok" : "auth failed" },
                { "data", new Dictionary<string, object>
                    {
                        { "version", "1.0.0" },
                        { "server", "YxSdkProxy" }
                    }
                }
            };
            
            SendJson(connId, response);
            Log($"已发送认证响应: success={success}");
        }

        /// <summary>
        /// 发送通用响应
        /// </summary>
        private void SendResponse(IntPtr connId, string type, int code, string msg)
        {
            var response = new Dictionary<string, object>
            {
                { "type", type },
                { "code", code },
                { "msg", msg }
            };
            
            SendJson(connId, response);
        }

        /// <summary>
        /// 发送JSON消息
        /// </summary>
        public bool SendJson(IntPtr connId, object obj)
        {
            if (!_isRunning || _server == null) return false;
            
            try
            {
                var serializer = new JavaScriptSerializer();
                var json = serializer.Serialize(obj);
                var data = Encoding.UTF8.GetBytes(json);
                
                Log($"[发送] ConnId={connId}, Data={json}");
                return _server.Send(connId, data, data.Length);
            }
            catch (Exception ex)
            {
                Log($"发送JSON失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 发送文本响应
        /// </summary>
        public bool SendTextResponse(IntPtr connId, string text)
        {
            if (!_isRunning || _server == null) return false;
            
            try
            {
                var data = Encoding.UTF8.GetBytes(text);
                return _server.Send(connId, data, data.Length);
            }
            catch (Exception ex)
            {
                Log($"发送文本失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 推送消息到621705120.exe (接收到的消息)
        /// </summary>
        public void PushReceivedMessage(string scene, string fromId, string targetId, string content, long timestamp)
        {
            if (_clientConnId == IntPtr.Zero) return;
            
            var message = new Dictionary<string, object>
            {
                { "type", "recv_msg" },
                { "data", new Dictionary<string, object>
                    {
                        { "scene", scene },
                        { "from", fromId },
                        { "to", targetId },
                        { "content", content },
                        { "time", timestamp }
                    }
                }
            };
            
            SendJson(_clientConnId, message);
        }

        /// <summary>
        /// 广播消息给所有客户端
        /// </summary>
        public void Broadcast(object message)
        {
            foreach (var client in _clients.Keys)
            {
                SendJson(client, message);
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            Stop();
        }

        #endregion

        #region 内部类

        private class YxClientInfo
        {
            public IntPtr ConnId { get; set; }
            public DateTime ConnectTime { get; set; }
            public bool IsAuthenticated { get; set; }
            public string RobotId { get; set; }
        }

        #endregion
    }
}
