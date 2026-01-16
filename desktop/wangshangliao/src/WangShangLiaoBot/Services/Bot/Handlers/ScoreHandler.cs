using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WangShangLiaoBot.Services.Bot.Handlers
{
    /// <summary>
    /// 上下分处理器 - 处理上分、下分、查分请求
    /// 基于招财狗(ZCG)的上下分关键词系统
    /// </summary>
    public class ScoreHandler : IMessageHandler
    {
        public int Priority => 20;

        private ScoreHandlerConfig _config;

        public ScoreHandler()
        {
            _config = ScoreHandlerConfig.CreateDefault();
        }

        public void SetConfig(ScoreHandlerConfig config)
        {
            _config = config ?? ScoreHandlerConfig.CreateDefault();
        }

        public bool CanHandle(MessageContext context)
        {
            if (!context.IsGroupMessage) return false;

            var content = context.Content.Trim();

            // 检查上分关键词
            if (_config.DepositEnabled && MatchKeywords(content, _config.DepositKeywords))
                return true;

            // 检查下分关键词
            if (_config.WithdrawEnabled && MatchKeywords(content, _config.WithdrawKeywords))
                return true;

            // 检查查分关键词 (发1, 1)
            if (_config.QueryEnabled && MatchKeywords(content, _config.QueryKeywords))
                return true;

            // 检查回水关键词
            if (_config.RebateEnabled && MatchKeywords(content, _config.RebateKeywords))
                return true;

            return false;
        }

        public async Task<HandlerResult> HandleAsync(MessageContext context)
        {
            await Task.Run(() => ProcessScore(context));
            return HandlerResult.Handled();
        }

        private void ProcessScore(MessageContext context)
        {
            var content = context.Content.Trim();
            var playerId = context.SenderId;
            var playerNick = context.SenderNick;

            // 处理查分 (发1, 1, 查)
            if (_config.QueryEnabled && MatchKeywords(content, _config.QueryKeywords))
            {
                HandleQuery(context, playerId, playerNick);
                return;
            }

            // 处理上分请求
            if (_config.DepositEnabled && MatchKeywords(content, _config.DepositKeywords))
            {
                HandleDepositRequest(context, playerId, playerNick, content);
                return;
            }

            // 处理下分请求
            if (_config.WithdrawEnabled && MatchKeywords(content, _config.WithdrawKeywords))
            {
                HandleWithdrawRequest(context, playerId, playerNick, content);
                return;
            }

            // 处理回水请求
            if (_config.RebateEnabled && MatchKeywords(content, _config.RebateKeywords))
            {
                HandleRebateRequest(context, playerId, playerNick);
                return;
            }
        }

        #region 查分处理

        private void HandleQuery(MessageContext context, string playerId, string playerNick)
        {
            var balance = ScoreService.Instance.GetBalance(playerId);
            var todayBets = AutoSettlementService.Instance.GetPeriodBets(SealingService.Instance.GetCurrentPeriod())
                .FindAll(b => b.PlayerId == playerId);

            string templateKey;
            string betContent = "";

            if (balance <= 0)
            {
                templateKey = "发1_0分";
            }
            else if (todayBets.Count == 0)
            {
                templateKey = "发1_有分无攻击";
            }
            else
            {
                templateKey = "发1_有分有攻击";
                betContent = string.Join(" ", todayBets.ConvertAll(b => b.NormalizedText));
            }

            var vars = new Dictionary<string, string>
            {
                ["艾特"] = $"@{playerNick}",
                ["旺旺"] = playerNick,
                ["昵称"] = playerNick,
                ["余粮"] = balance.ToString("F2", CultureInfo.InvariantCulture),
                ["余额"] = balance.ToString("F2", CultureInfo.InvariantCulture),
                ["下注"] = betContent
            };

            var msg = MessageTemplateService.Instance.Render(templateKey, vars);
            context.Reply(msg);
            context.Handled = true;
        }

        #endregion

        #region 上分处理

        private void HandleDepositRequest(MessageContext context, string playerId, string playerNick, string content)
        {
            // 解析上分金额: "上100", "上分100", "+100", "100到"
            var amount = ParseAmount(content, _config.DepositPatterns);

            if (amount <= 0)
            {
                // 没有金额，只是上分请求通知
                var msg = MessageTemplateService.Instance.Render("客户上分回复", new Dictionary<string, string>
                {
                    ["艾特"] = $"@{playerNick}",
                    ["旺旺"] = playerNick
                });
                context.Reply(msg);
                context.Handled = true;
                return;
            }

            // 如果启用了自动上分
            if (_config.AutoDeposit)
            {
                var balanceBefore = ScoreService.Instance.GetBalance(playerId);
                var newBalance = ScoreService.Instance.AddScore(playerId, amount, "玩家上分", playerNick: playerNick);

                var vars = MessageTemplateService.Instance.CreateScoreVariables(playerNick, amount, balanceBefore, newBalance);
                var msg = MessageTemplateService.Instance.Render("上分到词", vars);
                context.Reply(msg);

                // 发送第二条消息
                var msg2 = MessageTemplateService.Instance.GetTemplate("上分到词_第二条");
                if (!string.IsNullOrEmpty(msg2))
                {
                    context.Reply(msg2);
                }
            }
            else
            {
                // 需要人工审核
                var msg = MessageTemplateService.Instance.Render("客户上分回复", new Dictionary<string, string>
                {
                    ["艾特"] = $"@{playerNick}",
                    ["旺旺"] = playerNick,
                    ["分数"] = amount.ToString("F2", CultureInfo.InvariantCulture)
                });
                context.Reply(msg);

                // 触发上分审核事件 (可由UI处理)
                OnDepositRequest?.Invoke(playerId, playerNick, amount, context.TeamId);
            }

            context.Handled = true;
        }

        #endregion

        #region 下分处理

        private void HandleWithdrawRequest(MessageContext context, string playerId, string playerNick, string content)
        {
            // 检查是否正在下注
            var currentBets = AutoSettlementService.Instance.GetPeriodBets(SealingService.Instance.GetCurrentPeriod())
                .FindAll(b => b.PlayerId == playerId);
            if (currentBets.Count > 0)
            {
                var msg = MessageTemplateService.Instance.Render("下注不能下分", new Dictionary<string, string>
                {
                    ["艾特"] = $"@{playerNick}",
                    ["旺旺"] = playerNick
                });
                context.Reply(msg);
                context.Handled = true;
                return;
            }

            // 检查下注次数是否达标
            var todayBetCount = ScoreService.Instance.GetTodayBetCount(playerId);
            if (_config.MinBetCountForWithdraw > 0 && todayBetCount < _config.MinBetCountForWithdraw)
            {
                var msg = MessageTemplateService.Instance.Render("下分最少下注次数", new Dictionary<string, string>
                {
                    ["艾特"] = $"@{playerNick}",
                    ["旺旺"] = playerNick,
                    ["目标次数"] = _config.MinBetCountForWithdraw.ToString(),
                    ["下注次数"] = todayBetCount.ToString()
                });
                context.Reply(msg);
                context.Handled = true;
                return;
            }

            // 解析下分金额
            var amount = ParseAmount(content, _config.WithdrawPatterns);
            var balance = ScoreService.Instance.GetBalance(playerId);

            // 如果没有指定金额，下全部
            if (amount <= 0) amount = balance;

            if (amount <= 0 || balance <= 0)
            {
                context.ReplyWithAt("余额不足，无法下分！");
                context.Handled = true;
                return;
            }

            if (amount > balance)
            {
                context.ReplyWithAt($"余额不足！当前余额:{balance:F2}");
                context.Handled = true;
                return;
            }

            // 检查最低下分金额
            if (_config.MinWithdrawAmount > 0 && amount < _config.MinWithdrawAmount)
            {
                var msg = MessageTemplateService.Instance.Render("下分一次性回", new Dictionary<string, string>
                {
                    ["艾特"] = $"@{playerNick}",
                    ["旺旺"] = playerNick,
                    ["最低下分"] = _config.MinWithdrawAmount.ToString("F0")
                });
                context.Reply(msg);
                context.Handled = true;
                return;
            }

            if (_config.AutoWithdraw)
            {
                // 自动处理下分
                var (success, newBalance, error) = ScoreService.Instance.DeductScore(playerId, amount, "玩家下分");
                if (!success)
                {
                    context.ReplyWithAt(error ?? "下分失败");
                    context.Handled = true;
                    return;
                }

                var vars = MessageTemplateService.Instance.CreateScoreVariables(playerNick, amount, balance, newBalance);
                vars["留分"] = newBalance.ToString("F2", CultureInfo.InvariantCulture);
                var msg = MessageTemplateService.Instance.Render("下分查分词", vars);
                context.Reply(msg);

                var msg2 = MessageTemplateService.Instance.GetTemplate("下分查分词_第二条");
                if (!string.IsNullOrEmpty(msg2))
                {
                    context.Reply(msg2);
                }
            }
            else
            {
                // 需要人工审核
                var msg = MessageTemplateService.Instance.Render("客户下分回复", new Dictionary<string, string>
                {
                    ["艾特"] = $"@{playerNick}",
                    ["旺旺"] = playerNick,
                    ["分数"] = amount.ToString("F2", CultureInfo.InvariantCulture)
                });
                context.Reply(msg);

                OnWithdrawRequest?.Invoke(playerId, playerNick, amount, context.TeamId);
            }

            context.Handled = true;
        }

        #endregion

        #region 回水处理

        private void HandleRebateRequest(MessageContext context, string playerId, string playerNick)
        {
            var stats = ScoreService.Instance.GetTodayStats(playerId);

            var (success, rebate, message) = RebateService.Instance.ProcessRebate(
                playerId,
                playerNick,
                stats.TotalBet,
                stats.BetCount,
                -stats.NetProfit // 输分为正
            );

            if (!string.IsNullOrEmpty(message))
            {
                context.Reply(message);
            }
            else if (rebate > 0)
            {
                context.ReplyWithAt($"回水成功！回水金额:{rebate:F2}");
            }
            else
            {
                var msg = MessageTemplateService.Instance.Render("返点_无回水回复", new Dictionary<string, string>
                {
                    ["艾特"] = $"@{playerNick}",
                    ["旺旺"] = playerNick
                });
                context.Reply(msg);
            }

            context.Handled = true;
        }

        #endregion

        #region 辅助方法

        private bool MatchKeywords(string content, string[] keywords)
        {
            if (keywords == null) return false;
            foreach (var kw in keywords)
            {
                if (string.IsNullOrEmpty(kw)) continue;
                if (content.Contains(kw) || content == kw)
                    return true;
            }
            return false;
        }

        private decimal ParseAmount(string content, string[] patterns)
        {
            foreach (var pattern in patterns)
            {
                var match = Regex.Match(content, pattern);
                if (match.Success && match.Groups.Count > 1)
                {
                    if (decimal.TryParse(match.Groups[1].Value, out var amount))
                        return amount;
                }
            }
            return 0m;
        }

        #endregion

        #region 事件

        /// <summary>
        /// 上分请求事件 (需要人工审核时触发)
        /// </summary>
        public event Action<string, string, decimal, string> OnDepositRequest; // playerId, nick, amount, teamId

        /// <summary>
        /// 下分请求事件 (需要人工审核时触发)
        /// </summary>
        public event Action<string, string, decimal, string> OnWithdrawRequest; // playerId, nick, amount, teamId

        #endregion

        // 占位引用，实际使用时替换
        private static class AutoSettlementService
        {
            public static Betting.AutoSettlementService Instance => Betting.AutoSettlementService.Instance;
        }
    }

    /// <summary>
    /// 上下分处理器配置
    /// </summary>
    public class ScoreHandlerConfig
    {
        public bool DepositEnabled { get; set; } = true;
        public bool WithdrawEnabled { get; set; } = true;
        public bool QueryEnabled { get; set; } = true;
        public bool RebateEnabled { get; set; } = true;

        public bool AutoDeposit { get; set; } = false;  // 自动上分
        public bool AutoWithdraw { get; set; } = false; // 自动下分

        public int MinBetCountForWithdraw { get; set; } = 1; // 下分最少下注次数
        public decimal MinWithdrawAmount { get; set; } = 100; // 最低下分金额

        public string[] DepositKeywords { get; set; } = new[] { "上分", "上芬", "+", "到", "充值" };
        public string[] WithdrawKeywords { get; set; } = new[] { "下分", "下芬", "回", "回芬", "提现", "兑换" };
        public string[] QueryKeywords { get; set; } = new[] { "发1", "1", "查", "查分", "余额" };
        public string[] RebateKeywords { get; set; } = new[] { "回水", "返水", "返点" };

        public string[] DepositPatterns { get; set; } = new[] { @"[上\+到](\d+)", @"(\d+)到" };
        public string[] WithdrawPatterns { get; set; } = new[] { @"[下回](\d+)", @"(\d+)回" };

        public static ScoreHandlerConfig CreateDefault() => new ScoreHandlerConfig();
    }
}
