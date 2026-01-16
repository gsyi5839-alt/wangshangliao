using System;
using System.Threading.Tasks;
using WangShangLiaoBot.Models;

namespace WangShangLiaoBot.Services.Bot.Handlers
{
    /// <summary>
    /// 发言检测处理器 - 处理违规发言检测
    /// 优先级最高，所有消息首先经过此处理器
    /// </summary>
    public class SpeechDetectionHandler : IMessageHandler
    {
        public string Name => "发言检测处理器";
        public int Priority => 1000; // 最高优先级

        private readonly SpeechDetectionService _detectionService;

        public SpeechDetectionHandler()
        {
            _detectionService = SpeechDetectionService.Instance;
        }

        public bool CanHandle(MessageContext context)
        {
            // 所有非机器人消息都需要检测
            return !context.IsFromBot;
        }

        public async Task<HandlerResult> HandleAsync(MessageContext context)
        {
            var config = _detectionService.GetConfig();
            if (!config.Enabled)
            {
                return HandlerResult.Continue(); // 继续下一个处理器
            }

            // 检测消息
            var result = _detectionService.CheckMessage(
                context.TeamId,
                context.SenderId,
                context.SenderNick,
                context.Text,
                context.MessageId,
                context.IsImage);

            // 如果是违规消息
            if (result.IsViolation)
            {
                // 不需要额外回复，SpeechDetectionService已经处理了警告/禁言/踢人
                // 返回已处理，阻止后续处理器处理此消息
                return HandlerResult.Handled(null);
            }

            // 检查0分玩家发言
            if (config.ZeroBalanceMuteIfNotDeposit)
            {
                var zeroResult = _detectionService.CheckZeroBalanceMessage(
                    context.TeamId,
                    context.SenderId,
                    context.SenderNick,
                    context.Text,
                    context.MessageId);

                if (zeroResult.IsViolation)
                {
                    return HandlerResult.Handled(null);
                }
            }

            // 继续下一个处理器
            return HandlerResult.Continue();
        }
    }
}
