using System;
using System.Threading.Tasks;
using WangShangLiaoBot.Models;

namespace WangShangLiaoBot.Services.Bot.Handlers
{
    /// <summary>
    /// 托管处理器 - 处理托管相关命令
    /// </summary>
    public class TrusteeHandler : IMessageHandler
    {
        public string Name => "托管处理器";
        public int Priority => 60; // 中等优先级

        private readonly TrusteeService _trusteeService;
        private readonly MessageTemplateService _templateService;

        public TrusteeHandler()
        {
            _trusteeService = TrusteeService.Instance;
            _templateService = MessageTemplateService.Instance;
        }

        public bool CanHandle(MessageContext context)
        {
            if (context.IsFromBot) return false;
            if (context.IsSealed) return false;

            var text = context.Text.Trim();

            // 托管命令
            if (text.StartsWith("托管") || text.StartsWith("托") || text.StartsWith("tg"))
                return true;

            // 取消托管
            if (text == "取消托管" || text == "取消" || text == "qx" || text == "qxtg")
                return true;

            // 查询托管状态
            if (text == "托管状态" || text == "tgzt")
                return true;

            return false;
        }

        public async Task<HandlerResult> HandleAsync(MessageContext context)
        {
            var text = context.Text.Trim();

            // 取消托管
            if (text == "取消托管" || text == "取消" || text == "qx" || text == "qxtg")
            {
                return await HandleCancelTrustee(context);
            }

            // 查询托管状态
            if (text == "托管状态" || text == "tgzt")
            {
                return await HandleQueryStatus(context);
            }

            // 开启托管
            return await HandleStartTrustee(context);
        }

        private async Task<HandlerResult> HandleStartTrustee(MessageContext context)
        {
            var text = context.Text.Trim();

            // 解析自定义下注内容 (托管 da100 x100)
            string customBet = null;
            if (text.Length > 2)
            {
                var betPart = text.Substring(2).Trim();
                if (!string.IsNullOrEmpty(betPart))
                {
                    customBet = betPart;
                }
            }

            var success = _trusteeService.AddTrustee(
                context.SenderId,
                context.SenderNick,
                context.TeamId,
                customBet);

            string reply;
            if (success)
            {
                var template = _templateService.GetTemplate("托管成功");
                if (!string.IsNullOrEmpty(template))
                {
                    reply = template
                        .Replace("[艾特]", $"@{context.SenderNick}")
                        .Replace("[旺旺]", context.SenderNick)
                        .Replace("[托管内容]", customBet ?? "自动策略")
                        .Replace("[换行]", "\n");
                }
                else
                {
                    reply = $"@{context.SenderNick} 已为您开启托管\n下注内容: {customBet ?? "自动根据余额选择"}\n下局自动为您下注";
                }
            }
            else
            {
                reply = $"@{context.SenderNick} 您已在托管中，如需修改请先取消托管";
            }

            return HandlerResult.Handled(reply);
        }

        private async Task<HandlerResult> HandleCancelTrustee(MessageContext context)
        {
            var success = _trusteeService.RemoveTrustee(context.SenderId);

            var template = _templateService.GetTemplate("取消托管");
            string reply;

            if (success)
            {
                if (!string.IsNullOrEmpty(template))
                {
                    reply = template
                        .Replace("[艾特]", $"@{context.SenderNick}")
                        .Replace("[旺旺]", context.SenderNick)
                        .Replace("[换行]", "\n");
                }
                else
                {
                    reply = $"@{context.SenderNick} 已为您取消托管，请重新发起下注！";
                }
            }
            else
            {
                reply = $"@{context.SenderNick} 您当前没有托管";
            }

            return HandlerResult.Handled(reply);
        }

        private async Task<HandlerResult> HandleQueryStatus(MessageContext context)
        {
            var isTrustee = _trusteeService.IsTrustee(context.SenderId);
            var trustees = _trusteeService.GetTrustees();
            var myTrustee = trustees.Find(t => t.PlayerId == context.SenderId);

            string reply;
            if (myTrustee != null && myTrustee.IsActive)
            {
                reply = $"@{context.SenderNick} 托管状态: 运行中\n" +
                        $"开始时间: {myTrustee.StartTime:HH:mm:ss}\n" +
                        $"已下注: {myTrustee.TotalBets}次\n" +
                        $"下注内容: {myTrustee.CustomBetContent ?? "自动策略"}";
            }
            else
            {
                reply = $"@{context.SenderNick} 您当前没有托管\n发送「托管」开启自动下注";
            }

            return HandlerResult.Handled(reply);
        }
    }
}
