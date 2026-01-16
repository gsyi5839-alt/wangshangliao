using System;
using System.Threading.Tasks;

namespace WSLFramework.Services.EventDriven
{
    /// <summary>
    /// 使用示例 - 直接使用软件预设内容发送消息
    /// 不需要消息构建器，所有回复内容来自 ConfigService 和 ZCGResponseFormatter
    /// </summary>
    public static class Examples
    {
        /// <summary>
        /// 示例1: 基础消息发送 - 使用软件预设内容
        /// </summary>
        public static async Task BasicSendExample()
        {
            Console.WriteLine("=== 基础消息发送示例 ===\n");

            var bot = WSLBotFactory.CreateWithTCP("bot001");
            bot.OnLog += Console.WriteLine;

            await bot.StartAsync();

            string groupId = "123456";
            string playerId = "user001";
            string nickname = "玩家1";

            // 1. 发送纯文本
            await bot.SendGroupMessageAsync(groupId, "欢迎光临！");

            // 2. 发送余额查询回复 (使用ZCGResponseFormatter格式)
            await bot.SendBalanceReplyAsync(groupId, playerId, nickname, 1000);
            // 输出: [LQ:@user001] (0001)\n欢迎查询，玩家1！\n当前余额:1000

            // 3. 发送上分成功回复
            await bot.SendUpSuccessAsync(groupId, playerId, 500, 1500);
            // 输出: [LQ:@user001] (0001)\n亲，您的上分已到账，祝你天天好运~\n上分金额: 500\n当前余额: 1500

            // 4. 发送下分成功回复
            await bot.SendDownSuccessAsync(groupId, playerId, 200, 1300);

            // 5. 发送下注成功回复
            await bot.SendBetSuccessAsync(groupId, playerId, "BIG", 100, 1200);
            // 输出: [LQ:@user001] (0001)\n下注大100，成功录取\n余额: 1200

            // 6. 发送开奖结果
            await bot.SendOpenResultAsync(groupId, 4, 5, 6, "3382926");
            // 输出: 开:4+5+6=15 DA 期3382926期

            Console.WriteLine("按回车键退出...");
            Console.ReadLine();
            bot.Dispose();
        }

        /// <summary>
        /// 示例2: 定时消息发送 - 封盘/倒计时
        /// </summary>
        public static async Task TimedMessageExample()
        {
            Console.WriteLine("=== 定时消息发送示例 ===\n");

            var bot = WSLBotFactory.CreateWithTCP("bot002");
            bot.OnLog += Console.WriteLine;

            await bot.StartAsync();

            string groupId = "123456";

            // 1. 发送40秒倒计时 (使用ConfigService.SealRemindContent)
            await bot.SendCountdownAsync(groupId, 40);
            // 输出: --距离封盘时间还有40秒--\n改注加注带改 或者 加

            await Task.Delay(1000);

            // 2. 发送30秒倒计时
            await bot.SendCountdownAsync(groupId, 30);
            // 输出: --距离封盘时间还有30秒--

            await Task.Delay(1000);

            // 3. 发送完整封盘流程
            await bot.SendSealingSequenceAsync(groupId);
            // 依次发送:
            // ==加封盘线==\n以上有钱的都接\n==庄显为准==
            // 本群如遇卡奖情况，十分钟官网没开奖，本期无效，无需纠结！！！！

            Console.WriteLine("按回车键退出...");
            Console.ReadLine();
            bot.Dispose();
        }

        /// <summary>
        /// 示例3: 自动回复处理 - 使用 AutoReplyService
        /// </summary>
        public static async Task AutoReplyExample()
        {
            Console.WriteLine("=== 自动回复示例 ===\n");

            var bot = WSLBotFactory.CreateWithTCP("bot003");
            bot.OnLog += Console.WriteLine;

            // 注册群消息处理器 - 自动回复
            bot.Invoker.OnGroupMessageReceived += async (ctx, e) =>
            {
                string content = e.ToPreviewText();
                Console.WriteLine($"收到消息: {content}");

                // 尝试自动回复 (匹配AutoReplyService中的关键词)
                bool replied = await ctx.SendAutoReplyIfMatchedAsync(e.GroupId, content);
                
                if (replied)
                {
                    Console.WriteLine("已触发自动回复");
                }
                else
                {
                    // 没有匹配的自动回复，可以进行其他处理
                    Console.WriteLine("无匹配的自动回复");
                }
            };

            await bot.StartAsync();

            Console.WriteLine("机器人运行中，会自动回复关键词...");
            Console.WriteLine("支持的关键词: 余额、历史、规则、帮助 等");
            Console.WriteLine("按回车键退出...");
            Console.ReadLine();

            bot.Dispose();
        }

        /// <summary>
        /// 示例4: 使用 WSLMessageSender 直接发送
        /// </summary>
        public static async Task DirectSenderExample()
        {
            Console.WriteLine("=== 直接使用MessageSender示例 ===\n");

            var bot = WSLBotFactory.CreateWithTCP("bot004");
            await bot.StartAsync();

            var sender = bot.Sender;
            string groupId = "123456";
            string shortId = "0001";
            string nickname = "玩家1";

            // 使用配置模板发送 (来自ConfigService)
            
            // 1. 下注显示回复
            await sender.SendBetShowReplyAsync(groupId, nickname, shortId, "大100", 900);
            // 使用 ConfigService.ReplyBetShow 模板

            // 2. 余额查询回复
            await sender.SendQueryReplyAsync(groupId, nickname, shortId, 1000);
            // 使用 ConfigService.ReplyQueryHasScore 模板

            // 3. 封盘下注无效回复
            await sender.SendBetClosedReplyAsync(groupId, nickname, shortId, 25);
            // 使用 ConfigService.ReplyBetClosed 模板

            // 4. 上分到账回复
            await sender.SendUpArrivedReplyAsync(groupId, nickname, 500, 1500);
            // 使用 ConfigService.ReplyUpArrived 模板

            // 5. 取消下注回复
            await sender.SendBetCancelledReplyAsync(groupId, nickname, shortId);
            // 使用 ConfigService.ReplyBetCancelled 模板

            // 6. 余额不足回复
            await sender.SendAttackValidReplyAsync(groupId, nickname, shortId, "大100");
            // 使用 ConfigService.ReplyAttackValid 模板

            Console.WriteLine("按回车键退出...");
            Console.ReadLine();
            bot.Dispose();
        }

        /// <summary>
        /// 示例5: 完整的自动回复机器人 (使用软件预设)
        /// </summary>
        public static async Task CompleteBotExample()
        {
            Console.WriteLine("=== 完整自动回复机器人示例 ===\n");

            var bot = WSLBotFactory.CreateWithTCP("complete_bot", "127.0.0.1", 8899);
            
            // 注册日志处理
            bot.OnLog += msg => Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {msg}");
            
            // 注册连接状态处理
            bot.Invoker.OnConnectionStateChanged += (ctx, e) =>
            {
                Console.WriteLine($"连接状态: {(e.IsConnected ? "✓ 在线" : "✗ 离线")}");
            };

            // 注册群消息处理
            bot.Invoker.OnGroupMessageReceived += async (ctx, e) =>
            {
                string content = e.ToPreviewText();
                string groupId = e.GroupId;
                string playerId = e.SenderId;
                string shortId = playerId.Length > 4 ? playerId.Substring(playerId.Length - 4) : playerId;

                // 1. 先尝试自动回复 (关键词匹配)
                if (await ctx.SendAutoReplyIfMatchedAsync(groupId, content))
                {
                    return;
                }

                // 2. 处理查询命令
                if (content.Contains("余额") || content == "1")
                {
                    int balance = 1000; // 实际从数据库获取
                    await ctx.SendBalanceReplyAsync(groupId, playerId, e.SenderNick ?? shortId, balance);
                    return;
                }

                // 3. 处理下注 (简化示例)
                if (content.StartsWith("大") || content.StartsWith("小"))
                {
                    int amount = 100; // 实际解析金额
                    int balance = 900; // 实际计算
                    await ctx.SendBetSuccessAsync(groupId, playerId, "BIG", amount, balance);
                    return;
                }

                // 4. 其他消息不处理
            };

            // 启动
            bool connected = await bot.StartAsync();
            Console.WriteLine($"机器人启动: {(connected ? "成功" : "失败")}");

            if (connected)
            {
                Console.WriteLine("机器人运行中...");
                Console.WriteLine("按回车键退出...");
                Console.ReadLine();
            }

            bot.Dispose();
        }

        /// <summary>
        /// 示例6: 配置说明 - 软件预设内容来源
        /// </summary>
        public static void ConfigExplanation()
        {
            Console.WriteLine("=== 软件预设内容来源 ===\n");

            Console.WriteLine("1. ConfigService (配置服务):");
            Console.WriteLine("   - SealRemindContent: 封盘40秒提醒内容");
            Console.WriteLine("   - SealContent: 封盘线消息");
            Console.WriteLine("   - RuleContent: 卡奖提醒内容");
            Console.WriteLine("   - BetDataContent: 核对消息");
            Console.WriteLine("   - ReplyBetShow: 下注显示模板");
            Console.WriteLine("   - ReplyQuery0Score: 零余额查询模板");
            Console.WriteLine("   - ReplyQueryHasScore: 有余额查询模板");
            Console.WriteLine("   - ReplyBetClosed: 封盘下注无效模板");
            Console.WriteLine("   - ReplyUpArrived: 上分到账模板");
            Console.WriteLine("   - 等等...");
            Console.WriteLine();

            Console.WriteLine("2. ZCGResponseFormatter (响应格式化器):");
            Console.WriteLine("   - FormatBalanceQuery: 余额查询响应");
            Console.WriteLine("   - FormatUpSuccess: 上分成功响应");
            Console.WriteLine("   - FormatDownSuccess: 下分成功响应");
            Console.WriteLine("   - FormatBetSuccess: 下注成功响应");
            Console.WriteLine("   - FormatOpenResult: 开奖结果");
            Console.WriteLine("   - FormatCountdown: 倒计时消息");
            Console.WriteLine("   - FormatCloseLine: 封盘线消息");
            Console.WriteLine("   - 等等...");
            Console.WriteLine();

            Console.WriteLine("3. AutoReplyService (自动回复服务):");
            Console.WriteLine("   - 关键词匹配: 余额、历史、规则、帮助");
            Console.WriteLine("   - 精确匹配: 自定义命令");
            Console.WriteLine("   - 正则匹配: 复杂模式");
            Console.WriteLine();

            Console.WriteLine("所有消息内容都来自软件配置，无需手动构建！");
        }

        /// <summary>
        /// 示例7: 与旧代码对比
        /// </summary>
        public static void ComparisonWithOld()
        {
            Console.WriteLine("=== 新旧代码对比 ===\n");

            Console.WriteLine("【旧方式 - 使用CDP桥接】");
            Console.WriteLine(@"
// 需要CDP连接
var result = await _cdpBridge.SendGroupMessageAsync(groupId, content);

// 或者通过TimedMessageService
await TimedMessageService.Instance.SendToGroupAsync(groupId, message);
");

            Console.WriteLine("【新方式 - 使用TCP长连接 + 预设内容】");
            Console.WriteLine(@"
// 直接使用BotContext
await bot.SendGroupMessageAsync(groupId, content);

// 使用预设回复
await bot.SendBalanceReplyAsync(groupId, playerId, nickname, balance);
await bot.SendBetSuccessAsync(groupId, playerId, betType, amount, balance);
await bot.SendCountdownAsync(groupId, 40);
await bot.SendSealingSequenceAsync(groupId);

// 使用Sender直接发送配置模板
await bot.Sender.SendBetShowReplyAsync(groupId, nickname, shortId, betContent, balance);
await bot.Sender.SendQueryReplyAsync(groupId, nickname, shortId, balance);
");

            Console.WriteLine("【优势】");
            Console.WriteLine("  1. 无CDP依赖 - 不需要Chrome浏览器");
            Console.WriteLine("  2. 更低延迟 - TCP长连接替代轮询");
            Console.WriteLine("  3. 预设内容 - 直接使用软件配置，无需手动构建");
            Console.WriteLine("  4. 统一接口 - 所有发送都通过BotContext");
        }
    }
}
