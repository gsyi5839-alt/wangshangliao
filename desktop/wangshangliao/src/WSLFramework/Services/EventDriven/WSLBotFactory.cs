using System;
using System.Threading.Tasks;

namespace WSLFramework.Services.EventDriven
{
    /// <summary>
    /// 旺商聊机器人工厂 - 参考 Lagrange.Core 的 BotFactory
    /// 使用纯TCP长连接，无CDP依赖
    /// </summary>
    public static class WSLBotFactory
    {
        /// <summary>
        /// 创建使用TCP长连接的机器人 (推荐，最稳定)
        /// 参考 Lagrange.Core 的连接方式
        /// </summary>
        public static WSLBotContext CreateWithTCP(string botId, string host = "127.0.0.1", int port = 8899)
        {
            var config = new WSLBotConfig
            {
                BotId = botId,
                ServerHost = host,
                ServerPort = port,
                HeartbeatInterval = 30000,
                AutoReconnect = true,
                ReconnectInterval = 5000,
                MaxReconnectAttempts = 10
            };

            return new WSLBotContext(config);
        }

        /// <summary>
        /// 创建使用NIM直连的机器人
        /// </summary>
        public static WSLBotContext CreateWithNIM(string botId, string appKey, string accId, string token)
        {
            var config = new WSLBotConfig
            {
                BotId = botId,
                AppKey = appKey,
                AccId = accId,
                Token = token,
                AutoReconnect = true
            };

            return new WSLBotContext(config);
        }

        /// <summary>
        /// 创建使用HTTP API的机器人
        /// </summary>
        public static WSLBotContext CreateWithHttpApi(string botId, string apiBaseUrl)
        {
            var config = new WSLBotConfig
            {
                BotId = botId,
                ApiBaseUrl = apiBaseUrl,
                AutoReconnect = true
            };

            return new WSLBotContext(config);
        }

        /// <summary>
        /// 使用自定义配置创建机器人
        /// </summary>
        public static WSLBotContext Create(WSLBotConfig config)
        {
            return new WSLBotContext(config);
        }
        
        /// <summary>
        /// 创建默认机器人 (TCP长连接)
        /// </summary>
        public static WSLBotContext CreateDefault(string botId)
        {
            return CreateWithTCP(botId);
        }
    }

    /// <summary>
    /// 扩展方法 - 提供流畅API
    /// </summary>
    public static class WSLBotContextExtensions
    {
        /// <summary>
        /// 注册群消息处理器
        /// </summary>
        public static WSLBotContext OnGroupMessage(this WSLBotContext ctx, Action<WSLBotContext, GroupMessageEvent> handler)
        {
            ctx.Invoker.OnGroupMessageReceived += (c, e) => handler(c, e);
            return ctx;
        }

        /// <summary>
        /// 注册私聊消息处理器
        /// </summary>
        public static WSLBotContext OnFriendMessage(this WSLBotContext ctx, Action<WSLBotContext, FriendMessageEvent> handler)
        {
            ctx.Invoker.OnFriendMessageReceived += (c, e) => handler(c, e);
            return ctx;
        }

        /// <summary>
        /// 注册禁言事件处理器
        /// </summary>
        public static WSLBotContext OnGroupMute(this WSLBotContext ctx, Action<WSLBotContext, GroupMuteEvent> handler)
        {
            ctx.Invoker.OnGroupMuteEvent += (c, e) => handler(c, e);
            return ctx;
        }

        /// <summary>
        /// 注册开奖结果处理器
        /// </summary>
        public static WSLBotContext OnLotteryResult(this WSLBotContext ctx, Action<WSLBotContext, LotteryResultEvent> handler)
        {
            ctx.Invoker.OnLotteryResultReceived += (c, e) => handler(c, e);
            return ctx;
        }

        /// <summary>
        /// 注册下注事件处理器
        /// </summary>
        public static WSLBotContext OnBet(this WSLBotContext ctx, Action<WSLBotContext, BetEvent> handler)
        {
            ctx.Invoker.OnBetReceived += (c, e) => handler(c, e);
            return ctx;
        }

        /// <summary>
        /// 注册上下分事件处理器
        /// </summary>
        public static WSLBotContext OnScoreChange(this WSLBotContext ctx, Action<WSLBotContext, ScoreChangeEvent> handler)
        {
            ctx.Invoker.OnScoreChangeReceived += (c, e) => handler(c, e);
            return ctx;
        }

        /// <summary>
        /// 注册日志处理器
        /// </summary>
        public static WSLBotContext OnLog(this WSLBotContext ctx, Action<string> handler)
        {
            ctx.OnLog += handler;
            return ctx;
        }

        /// <summary>
        /// 注册连接状态处理器
        /// </summary>
        public static WSLBotContext OnConnectionState(this WSLBotContext ctx, Action<WSLBotContext, ConnectionStateEvent> handler)
        {
            ctx.Invoker.OnConnectionStateChanged += (c, e) => handler(c, e);
            return ctx;
        }

        /// <summary>
        /// 连接并启动
        /// </summary>
        public static async Task<WSLBotContext> ConnectAsync(this WSLBotContext ctx)
        {
            await ctx.StartAsync();
            return ctx;
        }

        /// <summary>
        /// 注册自动回复处理 (使用软件预设关键词)
        /// </summary>
        public static WSLBotContext WithAutoReply(this WSLBotContext ctx)
        {
            ctx.Invoker.OnGroupMessageReceived += async (c, e) =>
            {
                await c.SendAutoReplyIfMatchedAsync(e.GroupId, e.ToPreviewText());
            };
            return ctx;
        }

        /// <summary>
        /// 注册消息发送成功回调
        /// </summary>
        public static WSLBotContext OnMessageSent(this WSLBotContext ctx, Action<string, string> handler)
        {
            ctx.Sender.OnMessageSent += handler;
            return ctx;
        }

        /// <summary>
        /// 注册消息发送失败回调
        /// </summary>
        public static WSLBotContext OnSendFailed(this WSLBotContext ctx, Action<string, string, string> handler)
        {
            ctx.Sender.OnSendFailed += handler;
            return ctx;
        }
    }
}
