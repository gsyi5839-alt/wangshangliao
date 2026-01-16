using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using WangShangLiaoBot.Models.Betting;
using WangShangLiaoBot.Services.Betting;

namespace WangShangLiaoBot.Services.Bot.Handlers
{
    /// <summary>
    /// 下注消息处理器 - 处理用户的下注请求
    /// </summary>
    public class BetHandler : IMessageHandler
    {
        public int Priority => 10;

        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, byte> _enabledTeams = 
            new System.Collections.Concurrent.ConcurrentDictionary<string, byte>();
        private string _currentPeriod;

        /// <summary>
        /// 设置当前期号
        /// </summary>
        public void SetCurrentPeriod(string period)
        {
            _currentPeriod = period;
        }

        /// <summary>
        /// 启用群下注功能
        /// </summary>
        public void EnableTeam(string teamId)
        {
            _enabledTeams.TryAdd(teamId, 0);
        }

        /// <summary>
        /// 禁用群下注功能
        /// </summary>
        public void DisableTeam(string teamId)
        {
            _enabledTeams.TryRemove(teamId, out _);
        }

        public bool CanHandle(MessageContext context)
        {
            // 只处理群消息
            if (!context.IsGroupMessage) return false;

            // 检查是否启用了该群
            if (!_enabledTeams.ContainsKey(context.TeamId)) return false;

            // 检查是否已封盘
            if (SealingService.Instance.IsSealed()) return false;

            // 尝试解析下注内容
            return BetMessageParser.TryParse(context.Content, out _, out _, out _);
        }

        public async Task<HandlerResult> HandleAsync(MessageContext context)
        {
            await Task.Run(() => ProcessBet(context));
            return HandlerResult.Handled();
        }

        private void ProcessBet(MessageContext context)
        {
            try
            {
                // 1. 解析下注内容
                if (!BetMessageParser.TryParse(context.Content, out var items, out var total, out var normalized))
                {
                    return;
                }

                var playerId = context.SenderId;
                var playerNick = context.SenderNick;

                // 2. 检查余额
                var balance = ScoreService.Instance.GetBalance(playerId);
                if (balance < total)
                {
                    var vars = MessageTemplateService.Instance.CreateBetVariables(
                        playerNick, playerId, normalized, balance, total);
                    var msg = MessageTemplateService.Instance.Render("余粮不足", vars);
                    context.Reply(msg);
                    context.Handled = true;
                    return;
                }

                // 3. 验证限额
                foreach (var item in items)
                {
                    var (valid, error) = OddsService.Instance.ValidateBetAmount(item.Kind, item.Amount);
                    if (!valid)
                    {
                        var vars = new Dictionary<string, string>
                        {
                            ["艾特"] = $"@{playerNick}",
                            ["旺旺"] = playerNick,
                            ["下注内容"] = $"{CodeToChinese(item.Code, item.Kind)}{item.Amount}",
                            ["高低"] = error.Contains("低") ? "低于下限" : "高于上限"
                        };
                        var msg = MessageTemplateService.Instance.Render("超出范围", vars);
                        context.Reply(msg);
                        context.Handled = true;
                        return;
                    }
                }

                // 4. 验证总额
                var (totalValid, totalError) = OddsService.Instance.ValidateTotalBet(total);
                if (!totalValid)
                {
                    context.ReplyWithAt(totalError);
                    context.Handled = true;
                    return;
                }

                // 5. 扣除余额
                var (success, newBalance, deductError) = ScoreService.Instance.DeductBet(playerId, total, _currentPeriod);
                if (!success)
                {
                    context.ReplyWithAt(deductError ?? "扣款失败");
                    context.Handled = true;
                    return;
                }

                // 6. 记录下注
                var betRecord = new BetRecord
                {
                    Time = DateTime.Now,
                    Period = _currentPeriod,
                    TeamId = context.TeamId,
                    PlayerId = playerId,
                    PlayerNick = playerNick,
                    RawText = context.Content,
                    NormalizedText = normalized,
                    Items = items,
                    TotalAmount = total,
                    ScoreBefore = balance
                };
                AutoSettlementService.Instance.AddBetRecord(betRecord);

                // 7. 发送确认消息
                var betVars = MessageTemplateService.Instance.CreateBetVariables(
                    playerNick, playerId, normalized, newBalance, total);
                var confirmMsg = MessageTemplateService.Instance.Render("下注显示", betVars);
                context.Reply(confirmMsg);

                context.Handled = true;
            }
            catch (Exception ex)
            {
                Logger.Error($"[下注处理] 异常: {ex.Message}");
            }
        }

        private string CodeToChinese(string code, BetKind kind)
        {
            switch (code)
            {
                case "D": return "大";
                case "X": return "小";
                case "DD": return "大单";
                case "DS": return "大双";
                case "XD": return "小单";
                case "XS": return "小双";
                case "DZ": return "对子";
                case "SZ": return "顺子";
                case "BZ": return "豹子";
                default: return kind == BetKind.Digit ? code : code;
            }
        }
    }
}
