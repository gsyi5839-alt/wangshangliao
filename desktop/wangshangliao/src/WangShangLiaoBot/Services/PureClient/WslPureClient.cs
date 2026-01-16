using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WangShangLiaoBot.Services.PureClient
{
    /// <summary>
    /// 旺商聊纯C#客户端
    /// 完全独立实现，不依赖Electron或CDP
    /// </summary>
    public class WslPureClient : IDisposable
    {
        #region 环境密钥

        // 从逆向分析提取的密钥
        private static readonly Dictionary<string, string> ENVIRONMENT_KEYS = new Dictionary<string, string>
        {
            ["development"] = "AgAAAAAAAAB7w0xkTV3nnZ3HEzpDESHyFwCvFX-b3YhdCUwPtb2PAXZgWsH3Y7clf7E0x0mTwFnvkcDX4xRrc_R83VsxxKjnrPSE6gY43TeB9QGj4XjKTgN7wgUDlRWi6z_WnHOJ1w8.BgAAAAAAAACNs15mtTPLWrfGKfiDgqzGVxkMi-UbWEYxU0c6JKqO-BkT8LiCBX36YFbio4zqa5g3lexRzffG-it8h-8wuLbgkRogdcHZRNjxzXVwJYdAJRKuRS9LVwePvL743KzvLQQ.RlBIxOkAOFiT5Ri-QE79e9GsPiZWoJCL5HEkBLtd90gR0Fl8T43QqycK_BlhjvqiTdUVncVXBK_jEIWbI6R3WqqFG8s1eQm_NjktbMZ-cGZ8lG4oiFT1kmEL7QGZFlAL9Lw2GloykmDwYg9QX8q0Bc0Kjp8oxzUmO8QGWJDoNGGWGU9Fojjx8-Iugwg1pSTnpauHfS1Kg23cQ7J_rnvgBQJ_k0WcuPYVxhtj0mXjVz3s-C-iVIcvGrQ9aXcImgZsiMRDORHYiAIRTgOg3wJEFwIHqIoN-qRBT2KGU0d5Lz4YGkV14p21IGrzcfjk1T37fPv0_mGfia0GM91-gPfaNo3PJB4-AEeNlPmLtkWZ5y_tcMe1lp9s1ae-iw0fjDsQYM6qbcToYwvJIzyILncG7_LmNUATJp68Ms-BIgWic8gpP_cJscQqTttypZ2gmLdjbKBmwhbGjd7uzGKs4RHmfsw7APrZKg",
            ["staging"] = "AQAAAAAAAABVZpRGB3OJe9Dyoq00-TJOa2pd5S6a67DOgJ99jytqiIhObUjAyyD77MjQh7N_S-I8aUuWn3Q8Ay-rJmQainc91KXKn4ZOdyXYysCqCnsn7cy4kDddZI4lFB15zsLJmAw.FQAAAAAAAAB6xn0T-tdhh0jp-3HZyaNVHt7y-EShR5GATUWFO58I_N_NmhJngEr-rGhhc3-A1XaM85WjFogQcoQp1ddthLL3fUz1zmff_Ts7CFdtjdLImZzE18FuXXvHUF7aOsqPLww.VfCHxLmsjUkUNkBKxoB6xk7_fNk63vEj5A8L-vq1iaHbhHmTFfTw_ZoyNv83kU3jWa2K7L1AryIPHLrGfo8I17BTAWHoXS2ArnABzbDlvju7xkpZmBc8kAvNsCBgbegJ1Fbzl2jeGs6ecc11or4f1VWVX5WuxlS10Hso9oHBY-1va-BETYRITRRx4-lvFE8ZU8N77NPf37lewR5q9swWB4Zb_ZguyzSPr-LiDAiW_LgxuadXWkuvVZB61UUxqpCVjfMlgKcvO6OSUjDSjre1OMrc5KqjOrxuxHoncRqiFEs1qYUYS6oZ1-loLwUPsut6T2n3jwvqnVg0ByKgRMvMEV88di8Nd0lYjKhv3hJFlVRo39xl1P1dwg_hrecZLrTcnlxT0AEmrKLFNSHzZUCUr7ply6Jf_1GdlPWExjn5zvtUNiUV2ISzchc6c0sd4vehKp1Q_NP1ESfIq6jJX1w2cCk2pHoybaDp0LGMAaI",
            ["production"] = "AQAAAAAAAABVZpRGB3OJe9Dyoq00-TJOa2pd5S6a67DOgJ99jytqiIhObUjAyyD77MjQh7N_S-I8aUuWn3Q8Ay-rJmQainc91KXKn4ZOdyXYysCqCnsn7cy4kDddZI4lFB15zsLJmAw.BQAAAAAAAADtfRTVtWKpfDUEurA5Fcnfc0RyzFFihon90jVIMwP5_ONYiaD0hpuhNNi0SC6gXccyvszPCW3rb1KEaJ1vNMXW1O66AF7-6kiWsFPYhbCZ2j0WUXXNvo-TR8mK-hvALQY.RcRGxEnFfFgRzNkgE-Vng9ZyomzErNnE-jDO9ECGtT_DQH-PZe1jifFjJkB84trVoofwUWOTg8_b2p9S9036GXMRAPK6m_crEPu-ynoLZC8q0iJR9uR-6qhAAeCPjmwMhmecSgaYiixrj9l1twdmS18CwYkwe_G73fsBVaIZnP4xDsCtGG176nkPhstf1MN_qGi2joHRx7saPI9C2kY0WZjjChqOVdZ3zIT_lmtmJbypOYztWrnCV5sCr2FV1Ph_j3kniDDWJNpCFAsq9a-oDf4FGVm96Vgl_4DJ8eF8Q4WSdFbm7JR1H4-HkJNZ7pPrZ-esB8Heo_c-LeHYEleJHaapqFgos9gPDd5TOItaBH4SdbOOiBxmSqMqIpIWQEbfP-Bk3Z-SjqAxcSxrQeGtd0KMBXd2GbV2NfoCJU9_nmrTFzlQfG_Ei40eNk0BDJ7htD2Iscy8_ObJcTSzgy7Ju40WkKqQHsDVM9iaasRJ6_87XVzeocCaaWrt1vYH1doRBQ"
        };

        // xclient默认端口
        private const int XCLIENT_PORT = 21303;
        private const string XCLIENT_HOST = "127.0.0.1";

        #endregion

        private WslTcpProtocol _protocol;
        private WslKeyInfo _keyInfo;
        private string _environment;
        private Timer _heartbeatTimer;
        private bool _isLoggedIn;
        private string _currentUserId;
        private string _currentToken;

        // 事件
        public event Action<string, string, string> OnGroupMessage;     // groupId, senderId, message
        public event Action<string, string> OnPrivateMessage;           // senderId, message
        public event Action<JObject> OnRawMessage;                      // 原始消息
        public event Action<string> OnError;
        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action OnLoggedIn;

        public bool IsConnected => _protocol?.IsConnected ?? false;
        public bool IsLoggedIn => _isLoggedIn;
        public string CurrentUserId => _currentUserId;

        /// <summary>
        /// 初始化客户端
        /// </summary>
        public WslPureClient(string environment = "production")
        {
            _environment = environment;
            if (!ENVIRONMENT_KEYS.ContainsKey(environment))
            {
                throw new ArgumentException($"Unknown environment: {environment}");
            }

            // 解析密钥
            _keyInfo = WslCrypto.ParseKey(ENVIRONMENT_KEYS[environment]);
            Console.WriteLine($"[WslPureClient] Initialized with {environment} environment");
            Console.WriteLine($"  Key Version1: {_keyInfo.Version1}");
            Console.WriteLine($"  Key Version2: {_keyInfo.Version2}");
        }

        /// <summary>
        /// 连接到xclient
        /// </summary>
        public async Task ConnectAsync(string host = XCLIENT_HOST, int port = XCLIENT_PORT)
        {
            Console.WriteLine($"[WslPureClient] Connecting to {host}:{port}...");

            _protocol = new WslTcpProtocol(_keyInfo.EncryptionKey);
            _protocol.OnPacketReceived += HandlePacket;
            _protocol.OnError += ex => OnError?.Invoke(ex.Message);
            _protocol.OnConnected += () =>
            {
                Console.WriteLine("[WslPureClient] TCP Connected");
                OnConnected?.Invoke();
                StartHeartbeat();
            };
            _protocol.OnDisconnected += () =>
            {
                Console.WriteLine("[WslPureClient] TCP Disconnected");
                StopHeartbeat();
                OnDisconnected?.Invoke();
            };

            await _protocol.ConnectAsync(host, port);
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        public void Disconnect()
        {
            StopHeartbeat();
            _protocol?.Disconnect();
            _isLoggedIn = false;
        }

        /// <summary>
        /// 登录
        /// </summary>
        public async Task<bool> LoginAsync(string account, string password)
        {
            Console.WriteLine($"[WslPureClient] Login attempt: {account}");

            var result = await SendApiRequestAsync("/v1/user/login", new
            {
                account = account,
                password = password
            });

            if (result != null && result["code"]?.Value<int>() == 0)
            {
                var data = result["data"];
                _currentUserId = data?["userId"]?.ToString();
                _currentToken = data?["token"]?.ToString();
                _isLoggedIn = true;

                Console.WriteLine($"[WslPureClient] Login successful: userId={_currentUserId}");
                OnLoggedIn?.Invoke();
                return true;
            }

            var errorMsg = result?["msg"]?.ToString() ?? "Unknown error";
            Console.WriteLine($"[WslPureClient] Login failed: {errorMsg}");
            OnError?.Invoke($"Login failed: {errorMsg}");
            return false;
        }

        #region 群组API

        /// <summary>
        /// 获取群列表
        /// </summary>
        public async Task<JArray> GetGroupListAsync()
        {
            var result = await SendApiRequestAsync("/v1/group/get-group-list", new { });
            return result?["data"] as JArray;
        }

        /// <summary>
        /// 获取群成员
        /// </summary>
        public async Task<JArray> GetGroupMembersAsync(string groupId)
        {
            var result = await SendApiRequestAsync("/v1/group/get-group-members", new { gid = groupId });
            return result?["data"] as JArray;
        }

        /// <summary>
        /// 发送群消息
        /// </summary>
        public async Task<bool> SendGroupMessageAsync(string groupId, string message)
        {
            Console.WriteLine($"[WslPureClient] Sending group message to {groupId}: {message.Substring(0, Math.Min(50, message.Length))}...");

            // 构建NIM消息
            var nimMessage = WslMessage.CreateTextMessage("team", groupId, message);
            var encodedMsg = WslProtobuf.EncodeMessage(nimMessage);

            // 发送加密请求
            var packet = await _protocol.SendRequestAsync(encodedMsg, WslTcpProtocol.MSG_TYPE_ENCRYPT);
            
            return packet != null;
        }

        /// <summary>
        /// 禁言群成员
        /// </summary>
        public async Task<bool> MuteGroupMemberAsync(string groupId, string memberId, int durationSeconds)
        {
            var result = await SendApiRequestAsync("/v1/group/set-member-mute", new
            {
                gid = groupId,
                uid = memberId,
                duration = durationSeconds
            });
            return result?["code"]?.Value<int>() == 0;
        }

        /// <summary>
        /// 踢出群成员
        /// </summary>
        public async Task<bool> KickGroupMemberAsync(string groupId, string memberId)
        {
            var result = await SendApiRequestAsync("/v1/group/remove-group-member", new
            {
                gid = groupId,
                uid = memberId
            });
            return result?["code"]?.Value<int>() == 0;
        }

        /// <summary>
        /// 撤回群消息
        /// </summary>
        public async Task<bool> RecallGroupMessageAsync(string groupId, string messageId)
        {
            var result = await SendApiRequestAsync("/v1/group/message-rollback", new
            {
                gid = groupId,
                msgId = messageId
            });
            return result?["code"]?.Value<int>() == 0;
        }

        /// <summary>
        /// 设置群公告
        /// </summary>
        public async Task<bool> SetGroupNoticeAsync(string groupId, string title, string content)
        {
            var result = await SendApiRequestAsync("/v1/group/add-notice", new
            {
                gid = groupId,
                title = title,
                content = content
            });
            return result?["code"]?.Value<int>() == 0;
        }

        #endregion

        #region 好友API

        /// <summary>
        /// 获取好友列表
        /// </summary>
        public async Task<JArray> GetFriendListAsync()
        {
            var result = await SendApiRequestAsync("/v1/friend/get-friend-list", new { });
            return result?["data"] as JArray;
        }

        /// <summary>
        /// 发送私聊消息
        /// </summary>
        public async Task<bool> SendPrivateMessageAsync(string friendId, string message)
        {
            Console.WriteLine($"[WslPureClient] Sending private message to {friendId}");

            var nimMessage = WslMessage.CreateTextMessage("p2p", friendId, message);
            var encodedMsg = WslProtobuf.EncodeMessage(nimMessage);

            var packet = await _protocol.SendRequestAsync(encodedMsg, WslTcpProtocol.MSG_TYPE_ENCRYPT);
            return packet != null;
        }

        /// <summary>
        /// 处理好友申请
        /// </summary>
        public async Task<bool> HandleFriendApplyAsync(string applyId, bool accept)
        {
            var result = await SendApiRequestAsync("/v1/friend/friend-apply-handler", new
            {
                applyId = applyId,
                type = accept ? 1 : 2
            });
            return result?["code"]?.Value<int>() == 0;
        }

        #endregion

        #region 设置API

        /// <summary>
        /// 获取系统设置
        /// </summary>
        public async Task<JObject> GetSystemSettingsAsync()
        {
            return await SendApiRequestAsync("/v1/settings/get-system-setting", new { });
        }

        /// <summary>
        /// 设置自动回复
        /// </summary>
        public async Task<bool> SetAutoReplyAsync(string content, bool enabled)
        {
            var result = await SendApiRequestAsync("/v1/settings/set-auto-reply", new
            {
                content = content,
                enabled = enabled
            });
            return result?["code"]?.Value<int>() == 0;
        }

        /// <summary>
        /// 获取敏感词列表
        /// </summary>
        public async Task<JArray> GetSensitiveWordsAsync()
        {
            var result = await SendApiRequestAsync("/v1/settings/get-sensitive-words", new { });
            return result?["data"] as JArray;
        }

        #endregion

        #region 私有方法

        private async Task<JObject> SendApiRequestAsync(string url, object parameters)
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("Not connected");
            }

            try
            {
                var paramsJson = JsonConvert.SerializeObject(parameters);
                var packet = await _protocol.SendApiRequestAsync(url, paramsJson);

                if (packet?.Data != null)
                {
                    var responseJson = packet.GetDataAsString();
                    return JObject.Parse(responseJson);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WslPureClient] API request failed: {url} - {ex.Message}");
                OnError?.Invoke($"API request failed: {ex.Message}");
            }

            return null;
        }

        private void HandlePacket(WslPacket packet)
        {
            try
            {
                // 处理推送消息
                if (packet.MessageType == WslTcpProtocol.MSG_TYPE_PUSH && packet.Data != null)
                {
                    // 尝试解码为NIM消息
                    var message = WslProtobuf.DecodeMessage(packet.Data);
                    
                    if (message.Body != null)
                    {
                        // 解析消息内容
                        var bodyStr = Encoding.UTF8.GetString(message.Body);
                        
                        try
                        {
                            var msgObj = JObject.Parse(bodyStr);
                            OnRawMessage?.Invoke(msgObj);

                            var scene = msgObj["scene"]?.ToString();
                            var from = msgObj["from"]?.ToString();
                            var to = msgObj["to"]?.ToString();
                            var text = msgObj["text"]?.ToString() ?? msgObj["body"]?.ToString();

                            if (!string.IsNullOrEmpty(text))
                            {
                                if (scene == "team")
                                {
                                    OnGroupMessage?.Invoke(to, from, text);
                                }
                                else if (scene == "p2p")
                                {
                                    OnPrivateMessage?.Invoke(from, text);
                                }
                            }
                        }
                        catch
                        {
                            // 不是JSON格式，可能是纯文本
                            Console.WriteLine($"[WslPureClient] Received non-JSON message: {bodyStr.Substring(0, Math.Min(100, bodyStr.Length))}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WslPureClient] Error handling packet: {ex.Message}");
            }
        }

        private void StartHeartbeat()
        {
            _heartbeatTimer = new Timer(async _ =>
            {
                try
                {
                    if (IsConnected)
                    {
                        await _protocol.SendHeartbeatAsync();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[WslPureClient] Heartbeat failed: {ex.Message}");
                }
            }, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
        }

        private void StopHeartbeat()
        {
            _heartbeatTimer?.Dispose();
            _heartbeatTimer = null;
        }

        #endregion

        public void Dispose()
        {
            Disconnect();
            _protocol?.Dispose();
        }
    }
}
