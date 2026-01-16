using System;
using System.Threading.Tasks;
using WSLFramework.Services.EventDriven.Network;

namespace WSLFramework.Services.EventDriven
{
    /// <summary>
    /// 旺商聊机器人上下文 - 参考 Lagrange.Core 的 BotContext
    /// 使用纯TCP长连接，无CDP依赖
    /// </summary>
    public class WSLBotContext : IDisposable
    {
        #region 公共属性

        /// <summary>事件调度器</summary>
        public WSLEventInvoker Invoker { get; }

        /// <summary>网络连接管理器 - 替代CDP</summary>
        public WSLConnectionManager Connection { get; private set; }

        /// <summary>消息发送器 - 直接使用软件预设内容</summary>
        public WSLMessageSender Sender { get; private set; }

        /// <summary>配置</summary>
        public WSLBotConfig Config { get; }

        /// <summary>机器人ID</summary>
        public string BotId => Config?.BotId;

        /// <summary>机器人昵称</summary>
        public string BotNick { get; set; }

        /// <summary>是否已连接</summary>
        public bool IsConnected => Connection?.IsConnected ?? false;

        /// <summary>是否已登录</summary>
        public bool IsLoggedIn { get; private set; }

        /// <summary>当前连接状态</summary>
        public ConnectionState CurrentState => Connection?.State ?? ConnectionState.Disconnected;

        #endregion

        #region 事件

        /// <summary>日志事件</summary>
        public event Action<string> OnLog;

        #endregion

        private bool _disposed;

        public WSLBotContext(WSLBotConfig config)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Invoker = new WSLEventInvoker(this);

            // 创建连接管理器
            Connection = new WSLConnectionManager(new WSLConnectionConfig
            {
                Host = Config.ServerHost,
                Port = Config.ServerPort,
                Account = Config.BotId,
                Token = Config.Token,
                HeartbeatIntervalMs = Config.HeartbeatInterval,
                ReconnectDelayMs = Config.ReconnectInterval,
                MaxReconnectAttempts = Config.MaxReconnectAttempts
            });

            // 创建消息发送器 - 直接使用软件预设内容
            Sender = new WSLMessageSender(Connection, msg => Log("Sender", LogLevel.Debug, msg));

            // 注册网络事件
            Connection.OnStateChanged += OnConnectionStateChanged;
            Connection.OnGroupMessageReceived += OnGroupMessage;
            Connection.OnPrivateMessageReceived += OnPrivateMessage;
            Connection.OnSystemNotify += OnSystemNotify;
            Connection.OnLog += msg => Log("Network", LogLevel.Debug, msg);

            // 注册内部事件处理
            Invoker.OnBotLogEvent += (ctx, e) => OnLog?.Invoke(e.ToString());
            Invoker.OnBotOnlineEvent += OnBotOnline;
            Invoker.OnBotOfflineEvent += OnBotOffline;
        }

        #region 连接方法

        /// <summary>
        /// 启动机器人 - 连接服务器
        /// </summary>
        public async Task<bool> StartAsync()
        {
            Log("WSLBotContext", LogLevel.Info, "正在启动...");

            bool connected = await Connection.StartAsync();
            
            if (connected)
            {
                Log("WSLBotContext", LogLevel.Info, "启动成功");
                // BUG修复: 不在这里触发BotOnlineEvent，由OnConnectionStateChanged统一处理
                // 避免重复触发事件
            }
            else
            {
                Log("WSLBotContext", LogLevel.Error, "启动失败");
            }

            return connected;
        }

        /// <summary>
        /// 停止机器人
        /// </summary>
        public void Stop()
        {
            Log("WSLBotContext", LogLevel.Info, "正在停止...");
            Connection?.Stop();
            IsLoggedIn = false;
            Invoker.PostEvent(new BotOfflineEvent(BotId, BotOfflineEvent.OfflineReason.Logout, "用户主动停止"));
        }

        /// <summary>
        /// 重新连接
        /// </summary>
        public async Task<bool> ReconnectAsync()
        {
            Stop();
            await Task.Delay(1000);
            return await StartAsync();
        }

        #endregion

        #region 消息发送 - 基础方法

        /// <summary>
        /// 发送群消息 (纯文本)
        /// </summary>
        public async Task<bool> SendGroupMessageAsync(string groupId, string content)
        {
            return await Sender.SendGroupAsync(groupId, content);
        }

        /// <summary>
        /// 发送私聊消息 (纯文本)
        /// </summary>
        public async Task<bool> SendFriendMessageAsync(string friendId, string content)
        {
            return await Sender.SendPrivateAsync(friendId, content);
        }

        /// <summary>
        /// 向多个群发送消息
        /// </summary>
        public async Task SendToGroupsAsync(string[] groupIds, string content, int delayMs = 100)
        {
            await Sender.SendToGroupsAsync(groupIds, content, delayMs);
        }

        #endregion

        #region 消息发送 - 使用软件预设模板

        /// <summary>
        /// 发送余额查询回复
        /// </summary>
        public async Task<bool> SendBalanceReplyAsync(string groupId, string playerId, string nickname, int balance)
        {
            return await Sender.SendBalanceReplyAsync(groupId, playerId, nickname, balance);
        }

        /// <summary>
        /// 发送上分成功回复
        /// </summary>
        public async Task<bool> SendUpSuccessAsync(string groupId, string playerId, int amount, int newBalance)
        {
            return await Sender.SendUpSuccessAsync(groupId, playerId, amount, newBalance);
        }

        /// <summary>
        /// 发送下分成功回复
        /// </summary>
        public async Task<bool> SendDownSuccessAsync(string groupId, string playerId, int amount, int newBalance)
        {
            return await Sender.SendDownSuccessAsync(groupId, playerId, amount, newBalance);
        }

        /// <summary>
        /// 发送下注成功回复
        /// </summary>
        public async Task<bool> SendBetSuccessAsync(string groupId, string playerId, string betType, int amount, int newBalance)
        {
            return await Sender.SendBetSuccessAsync(groupId, playerId, betType, amount, newBalance);
        }

        /// <summary>
        /// 发送开奖结果
        /// </summary>
        public async Task<bool> SendOpenResultAsync(string groupId, int num1, int num2, int num3, string period)
        {
            return await Sender.SendOpenResultAsync(groupId, num1, num2, num3, period);
        }

        /// <summary>
        /// 发送倒计时提醒
        /// </summary>
        public async Task<bool> SendCountdownAsync(string groupId, int seconds)
        {
            return await Sender.SendCountdownAsync(groupId, seconds);
        }

        /// <summary>
        /// 发送封盘流程
        /// </summary>
        public async Task SendSealingSequenceAsync(string groupId)
        {
            await Sender.SendSealingSequenceAsync(groupId);
        }

        /// <summary>
        /// 发送自动回复 (如果匹配关键词)
        /// </summary>
        public async Task<bool> SendAutoReplyIfMatchedAsync(string groupId, string content)
        {
            return await Sender.SendAutoReplyIfMatchedAsync(groupId, content);
        }

        #endregion

        #region 私聊消息处理

        // 缓存倒计时(由外部定时更新)
        private int _currentCountdown = 120;

        /// <summary>
        /// 设置当前倒计时秒数 (由 LotteryService 或 TimedMessageService 更新)
        /// </summary>
        public void SetCountdown(int seconds)
        {
            _currentCountdown = seconds;
        }

        /// <summary>
        /// 获取当前倒计时秒数
        /// </summary>
        public int GetCountdown()
        {
            // 如果没有设置，使用计算方式
            if (_currentCountdown <= 0)
            {
                return CalculateCountdown();
            }
            return _currentCountdown;
        }

        /// <summary>
        /// 计算下一期开奖倒计时 (加拿大28每210秒一期)
        /// </summary>
        private int CalculateCountdown()
        {
            var now = DateTime.Now;
            var secondsInDay = (int)(now - now.Date).TotalSeconds;
            var periodSeconds = 210;
            return periodSeconds - (secondsInDay % periodSeconds);
        }

        /// <summary>
        /// 处理私聊消息 - 数字1/2查询
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="nickname">用户昵称</param>
        /// <param name="content">消息内容</param>
        /// <returns>是否处理成功</returns>
        public async Task<bool> HandlePrivateQueryAsync(string userId, string nickname, string content)
        {
            return await Sender.HandlePrivateQueryAsync(userId, nickname, content, GetCountdown());
        }

        /// <summary>
        /// 处理私聊消息事件 - 自动处理数字1/2查询
        /// </summary>
        public async Task ProcessPrivateMessageAsync(FriendMessageEvent e)
        {
            if (e == null || string.IsNullOrEmpty(e.Content))
                return;

            var content = e.Content.Trim();
            
            // 检查是否为查询命令 (1=余额, 2=历史)
            if (content == "1" || content == "2" || 
                content == "查" || content == "余额" || 
                content == "历史" || content == "记录")
            {
                Log("WSLBotContext", LogLevel.Info, $"处理私聊查询: User={e.FriendUin}, Content={content}");
                await HandlePrivateQueryAsync(e.FriendUin, e.SenderNickname, content);
            }
        }

        #endregion

        #region 网络事件处理

        private void OnConnectionStateChanged(ConnectionState state, string message)
        {
            Log("WSLBotContext", LogLevel.Info, $"连接状态变更: {state} - {message}");

            // BUG修复: 记录之前的登录状态，避免重复触发事件
            bool wasLoggedIn = IsLoggedIn;

            switch (state)
            {
                case ConnectionState.Connected:
                    // 已连接，等待认证 (如果没有配置Token则直接认为已连接)
                    if (string.IsNullOrEmpty(Config?.Token))
                    {
                        IsLoggedIn = true;
                        if (!wasLoggedIn)
                        {
                            Invoker.PostEvent(new BotOnlineEvent(BotId, BotNick, BotOnlineEvent.OnlineReason.Login));
                        }
                    }
                    break;

                case ConnectionState.Authenticated:
                    IsLoggedIn = true;
                    // BUG修复: 只在之前未登录时才触发上线事件
                    if (!wasLoggedIn)
                    {
                        Invoker.PostEvent(new BotOnlineEvent(BotId, BotNick, BotOnlineEvent.OnlineReason.Login));
                    }
                    break;

                case ConnectionState.Disconnected:
                    // BUG修复: 只在之前已登录时才触发下线事件
                    if (wasLoggedIn)
                    {
                        IsLoggedIn = false;
                        Invoker.PostEvent(new BotOfflineEvent(BotId, BotOfflineEvent.OfflineReason.NetworkError, message));
                    }
                    break;

                case ConnectionState.Reconnecting:
                    // BUG修复: 只在之前已登录时才触发重连事件
                    if (wasLoggedIn)
                    {
                        IsLoggedIn = false;
                        Invoker.PostEvent(new BotOfflineEvent(BotId, BotOfflineEvent.OfflineReason.Reconnecting, message));
                    }
                    break;
            }

            Invoker.PostEvent(new ConnectionStateEvent(
                state == ConnectionState.Connected || state == ConnectionState.Authenticated,
                ConnectionStateEvent.ConnectionType.TCP
            ));
        }

        private void OnGroupMessage(MessageReceivedEventArgs args)
        {
            Log("WSLBotContext", LogLevel.Debug, $"收到群消息: GroupId={args.ToId}, From={args.FromId}");

            // BUG修复: 验证时间戳有效性
            DateTime time;
            if (args.Timestamp > 0)
            {
                try
                {
                    time = DateTimeOffset.FromUnixTimeMilliseconds(args.Timestamp).LocalDateTime;
                }
                catch
                {
                    time = DateTime.Now;
                }
            }
            else
            {
                time = DateTime.Now;
            }

            // 创建群消息事件
            var msgEvent = new GroupMessageEvent
            {
                GroupUin = args.ToId,
                FriendUin = args.FromId,
                MessageId = args.MessageId ?? Guid.NewGuid().ToString(),
                Time = time,
                Content = args.Content ?? "",
                RawData = args.RawData
            };

            // 通过Invoker分发事件
            Invoker.PostEvent(msgEvent);
        }

        private void OnPrivateMessage(MessageReceivedEventArgs args)
        {
            Log("WSLBotContext", LogLevel.Debug, $"收到私聊消息: From={args.FromId}");

            // BUG修复: 验证时间戳有效性
            DateTime time;
            if (args.Timestamp > 0)
            {
                try
                {
                    time = DateTimeOffset.FromUnixTimeMilliseconds(args.Timestamp).LocalDateTime;
                }
                catch
                {
                    time = DateTime.Now;
                }
            }
            else
            {
                time = DateTime.Now;
            }

            // 创建私聊消息事件
            var msgEvent = new FriendMessageEvent
            {
                FriendUin = args.FromId,
                SenderNickname = args.SenderNick ?? "",  // 设置发送者昵称
                MessageId = args.MessageId ?? Guid.NewGuid().ToString(),
                Time = time,
                Content = args.Content ?? "",
                RawData = args.RawData
            };

            // 通过Invoker分发事件
            Invoker.PostEvent(msgEvent);
        }

        private void OnSystemNotify(string type, string data)
        {
            Log("WSLBotContext", LogLevel.Debug, $"收到系统通知: Type={type}");
            // 可扩展处理不同类型的系统通知
        }

        #endregion

        #region Bot事件处理

        private void OnBotOnline(WSLBotContext ctx, BotOnlineEvent e)
        {
            IsLoggedIn = true;
            Log("WSLBotContext", LogLevel.Info, $"机器人已上线: {e.BotId}");
        }

        private void OnBotOffline(WSLBotContext ctx, BotOfflineEvent e)
        {
            IsLoggedIn = false;
            Log("WSLBotContext", LogLevel.Warning, $"机器人已下线: {e.Reason} - {e.Message}");
        }

        #endregion

        #region 辅助方法

        private void Log(string tag, LogLevel level, string message)
        {
            Invoker.PostEvent(new BotLogEvent(tag, level, message));
        }

        #endregion

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            Stop();
            Connection?.Dispose();
            Invoker?.Dispose();

            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// 机器人配置 - 简化版，无CDP
    /// </summary>
    public class WSLBotConfig
    {
        /// <summary>机器人ID (旺商聊号)</summary>
        public string BotId { get; set; }

        /// <summary>认证Token</summary>
        public string Token { get; set; }

        /// <summary>NIM AppKey (可选，用于NIM直连)</summary>
        public string AppKey { get; set; }

        /// <summary>NIM AccId (可选)</summary>
        public string AccId { get; set; }

        /// <summary>服务器地址</summary>
        public string ServerHost { get; set; } = "127.0.0.1";

        /// <summary>服务器端口</summary>
        public int ServerPort { get; set; } = 8899;

        /// <summary>心跳间隔(毫秒)</summary>
        public int HeartbeatInterval { get; set; } = 30000;

        /// <summary>重连间隔(毫秒)</summary>
        public int ReconnectInterval { get; set; } = 5000;

        /// <summary>最大重连尝试次数</summary>
        public int MaxReconnectAttempts { get; set; } = 10;

        /// <summary>自动重连</summary>
        public bool AutoReconnect { get; set; } = true;

        /// <summary>后端API地址</summary>
        public string ApiBaseUrl { get; set; } = "http://localhost:3000/api";
    }
}
