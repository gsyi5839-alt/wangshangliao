using System;
using System.Threading.Tasks;
using WangShangLiaoBot.Models;

namespace WangShangLiaoBot.Services.Bot.Handlers
{
    /// <summary>
    /// 返点/夜宵处理器 - 处理返点和夜宵相关命令
    /// </summary>
    public class BonusHandler : IMessageHandler
    {
        public string Name => "返点处理器";
        public int Priority => 45; // 低优先级

        private readonly BonusService _bonusService;
        private readonly MessageTemplateService _templateService;

        public BonusHandler()
        {
            _bonusService = BonusService.Instance;
            _templateService = MessageTemplateService.Instance;
        }

        public bool CanHandle(MessageContext context)
        {
            if (context.IsFromBot) return false;

            var text = context.Text.Trim();

            // 返水/回水命令
            if (_bonusService.IsRebateCommand(text))
                return true;

            // 夜宵命令
            if (text == "夜宵" || text == "领夜宵" || text == "yx")
                return true;

            // 流水查询
            if (text == "流水" || text == "我的流水" || text == "ls")
                return true;

            return false;
        }

        public async Task<HandlerResult> HandleAsync(MessageContext context)
        {
            var text = context.Text.Trim();

            // 返水命令
            if (_bonusService.IsRebateCommand(text))
            {
                return await HandleRebate(context);
            }

            // 夜宵命令
            if (text == "夜宵" || text == "领夜宵" || text == "yx")
            {
                return await HandleNightSnack(context);
            }

            // 流水查询
            if (text == "流水" || text == "我的流水" || text == "ls")
            {
                return await HandleQueryTurnover(context);
            }

            return HandlerResult.NotHandled();
        }

        private async Task<HandlerResult> HandleRebate(MessageContext context)
        {
            var result = _bonusService.CalculateTurnoverRebate(context.SenderId, context.SenderNick);

            var reply = FormatReply(result.Message, context.SenderNick);
            return HandlerResult.Handled(reply);
        }

        private async Task<HandlerResult> HandleNightSnack(MessageContext context)
        {
            var config = _bonusService.GetConfig();

            NightSnackResult result;
            if (config.CalculationMethod == BonusCalculationMethod.ByWinLose)
            {
                result = _bonusService.CalculateNightSnackByWinLose(context.SenderId, context.SenderNick);
            }
            else
            {
                result = _bonusService.CalculateNightSnack(context.SenderId, context.SenderNick);
            }

            string reply;
            if (result.Success)
            {
                reply = $"@{context.SenderNick} 🎉 恭喜获得夜宵奖励 {result.Bonus:F2}\n" +
                        $"今日流水: {result.TotalTurnover:F2}\n" +
                        $"下注次数: {result.TotalBets}把";
            }
            else
            {
                reply = $"@{context.SenderNick} {result.Message}\n" +
                        $"今日流水: {result.TotalTurnover:F2}\n" +
                        $"下注次数: {result.TotalBets}把";
            }

            return HandlerResult.Handled(reply);
        }

        private async Task<HandlerResult> HandleQueryTurnover(MessageContext context)
        {
            // 获取今日统计
            var balance = ScoreService.Instance.GetBalance(context.SenderId);
            var transactions = ScoreService.Instance.GetTransactions(
                context.SenderId,
                DateTime.Today,
                DateTime.Today.AddDays(1));

            var totalBet = 0m;
            var totalWin = 0m;
            var betCount = 0;

            foreach (var t in transactions)
            {
                if (t.Type == ScoreTransactionType.Bet)
                {
                    totalBet += Math.Abs(t.Amount);
                    betCount++;
                }
                else if (t.Type == ScoreTransactionType.Win)
                {
                    totalWin += t.Amount;
                }
            }

            var netProfit = totalWin - totalBet;
            var profitText = netProfit >= 0 ? $"盈利 {netProfit:F2}" : $"亏损 {Math.Abs(netProfit):F2}";

            var reply = $"@{context.SenderNick} 📊 今日数据\n" +
                        $"════════════\n" +
                        $"💰 余额: {balance:F2}\n" +
                        $"📈 流水: {totalBet:F2}\n" +
                        $"🎲 把数: {betCount}把\n" +
                        $"📉 {profitText}";

            return HandlerResult.Handled(reply);
        }

        private string FormatReply(string template, string nick)
        {
            if (string.IsNullOrEmpty(template)) return "";

            var balance = ScoreService.Instance.GetBalance(nick);

            return template
                .Replace("[艾特]", $"@{nick}")
                .Replace("[旺旺]", nick)
                .Replace("[余粮]", balance.ToString("F2"))
                .Replace("[换行]", "\n");
        }
    }
}
