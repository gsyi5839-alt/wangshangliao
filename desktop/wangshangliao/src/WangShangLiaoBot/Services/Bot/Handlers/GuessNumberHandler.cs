using System;
using System.Linq;
using System.Threading.Tasks;
using WangShangLiaoBot.Models;

namespace WangShangLiaoBot.Services.Bot.Handlers
{
    /// <summary>
    /// 猜数字处理器 - 处理猜数字游戏
    /// </summary>
    public class GuessNumberHandler : IMessageHandler
    {
        public string Name => "猜数字处理器";
        public int Priority => 55; // 中等偏低优先级

        private readonly GuessNumberService _guessService;

        public GuessNumberHandler()
        {
            _guessService = GuessNumberService.Instance;
        }

        public bool CanHandle(MessageContext context)
        {
            if (context.IsFromBot) return false;

            var config = _guessService.GetConfig();
            if (!config.Enabled) return false;

            var text = context.Text.Trim();

            // 检查关键词
            if (!string.IsNullOrEmpty(config.Keyword))
            {
                return text.Contains(config.Keyword);
            }

            // 默认检查 "猜" 关键词
            return text.Contains("猜");
        }

        public async Task<HandlerResult> HandleAsync(MessageContext context)
        {
            var config = _guessService.GetConfig();

            // 获取当前期号
            var periodNumber = GetCurrentPeriod();

            // 尝试猜测
            var result = _guessService.TryGuess(
                context.SenderId,
                context.SenderNick,
                context.Text,
                periodNumber);

            if (!result.Success)
            {
                if (!string.IsNullOrEmpty(result.Message))
                {
                    return HandlerResult.Handled($"@{context.SenderNick} {result.Message}");
                }
                return HandlerResult.NotHandled();
            }

            // 猜测成功
            var reply = $"@{context.SenderNick} 第{periodNumber}期\n" +
                        $"已记录猜测: {string.Join(",", result.Numbers)}\n" +
                        $"开奖后自动结算";

            return HandlerResult.Handled(reply);
        }

        private string GetCurrentPeriod()
        {
            // 使用SealingService获取期号
            var lotteryType = SealingService.Instance.GetConfig().CurrentLotteryType;
            return SealingService.CalculatePeriodNumber(lotteryType, DateTime.Now);
        }
    }
}
