using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace WangShangLiaoBot.Services.Bot.Handlers
{
    /// <summary>
    /// 自动回复处理器 - 处理关键词触发的自动回复
    /// 基于招财狗(ZCG)的自动回复系统
    /// </summary>
    public class AutoReplyHandler : IMessageHandler
    {
        public int Priority => 50; // 较低优先级，其他处理器先处理

        private readonly List<AutoReplyRule> _rules = new List<AutoReplyRule>();
        private readonly object _lock = new object();

        public AutoReplyHandler()
        {
            LoadDefaultRules();
        }

        #region 规则管理

        /// <summary>
        /// 加载默认规则
        /// </summary>
        private void LoadDefaultRules()
        {
            // 从招财狗提取的自动回复规则
            AddRule(new AutoReplyRule
            {
                Name = "财付通",
                Keywords = "财富|发财富|财付通|发财付通|cf|CF|caif|财付|财富账户".Split('|'),
                ReplyType = AutoReplyType.Template,
                TemplateKey = "自动回复_财付通发送",
                ReplyContent = "私聊前排接单",
                SendImage = true,
                ImageFolder = "财付通二维码"
            });

            AddRule(new AutoReplyRule
            {
                Name = "支付宝",
                Keywords = "支付|支付宝|发支付宝|发支付|zf|ZF|支付宝多少".Split('|'),
                ReplyType = AutoReplyType.Template,
                TemplateKey = "自动回复_支付宝发送",
                ReplyContent = "私聊前排接单",
                SendImage = true,
                ImageFolder = "支付宝二维码"
            });

            AddRule(new AutoReplyRule
            {
                Name = "微信",
                Keywords = "微信|发微信|微信多少|微信号".Split('|'),
                ReplyType = AutoReplyType.Template,
                TemplateKey = "自动回复_微信发送",
                ReplyContent = "私聊前排接单",
                SendImage = true,
                ImageFolder = "微信二维码"
            });

            AddRule(new AutoReplyRule
            {
                Name = "历史",
                Keywords = "历史|发历史|开奖历史|历史发|发下历史|2".Split('|'),
                ReplyType = AutoReplyType.Action,
                ActionType = "SendHistory"
            });

            AddRule(new AutoReplyRule
            {
                Name = "个人数据",
                Keywords = "账单|数据|我有下注吗|下注情况|1".Split('|'),
                ReplyType = AutoReplyType.Action,
                ActionType = "SendPersonalData"
            });

            AddRule(new AutoReplyRule
            {
                Name = "规则",
                Keywords = "规则|玩法|怎么玩|教程".Split('|'),
                ReplyType = AutoReplyType.Action,
                ActionType = "SendRules"
            });
        }

        /// <summary>
        /// 添加规则
        /// </summary>
        public void AddRule(AutoReplyRule rule)
        {
            lock (_lock)
            {
                _rules.Add(rule);
            }
        }

        /// <summary>
        /// 清除所有规则
        /// </summary>
        public void ClearRules()
        {
            lock (_lock)
            {
                _rules.Clear();
            }
        }

        /// <summary>
        /// 获取所有规则
        /// </summary>
        public List<AutoReplyRule> GetRules()
        {
            lock (_lock)
            {
                return new List<AutoReplyRule>(_rules);
            }
        }

        #endregion

        public bool CanHandle(MessageContext context)
        {
            // 检查是否匹配任何规则
            lock (_lock)
            {
                foreach (var rule in _rules)
                {
                    if (!rule.Enabled) continue;
                    if (MatchRule(context.Content, rule))
                        return true;
                }
            }
            return false;
        }

        public async Task<HandlerResult> HandleAsync(MessageContext context)
        {
            await Task.Run(() => ProcessAutoReply(context));
            return HandlerResult.Handled();
        }

        private void ProcessAutoReply(MessageContext context)
        {
            AutoReplyRule matchedRule = null;

            lock (_lock)
            {
                foreach (var rule in _rules)
                {
                    if (!rule.Enabled) continue;
                    if (MatchRule(context.Content, rule))
                    {
                        matchedRule = rule;
                        break;
                    }
                }
            }

            if (matchedRule == null) return;

            try
            {
                switch (matchedRule.ReplyType)
                {
                    case AutoReplyType.Text:
                        SendTextReply(context, matchedRule);
                        break;

                    case AutoReplyType.Template:
                        SendTemplateReply(context, matchedRule);
                        break;

                    case AutoReplyType.Action:
                        ExecuteAction(context, matchedRule);
                        break;
                }

                context.Handled = true;
            }
            catch (Exception ex)
            {
                Logger.Error($"[自动回复] 处理异常: {ex.Message}");
            }
        }

        private bool MatchRule(string content, AutoReplyRule rule)
        {
            if (rule.Keywords == null) return false;

            foreach (var keyword in rule.Keywords)
            {
                if (string.IsNullOrEmpty(keyword)) continue;

                if (rule.ExactMatch)
                {
                    if (content == keyword) return true;
                }
                else
                {
                    if (content.Contains(keyword)) return true;
                }
            }
            return false;
        }

        private void SendTextReply(MessageContext context, AutoReplyRule rule)
        {
            if (string.IsNullOrEmpty(rule.ReplyContent)) return;
            context.Reply(rule.ReplyContent);
        }

        private void SendTemplateReply(MessageContext context, AutoReplyRule rule)
        {
            var content = rule.ReplyContent;

            // 尝试从模板获取
            if (!string.IsNullOrEmpty(rule.TemplateKey))
            {
                var template = MessageTemplateService.Instance.GetTemplate(rule.TemplateKey);
                if (!string.IsNullOrEmpty(template))
                {
                    content = template;
                }
            }

            if (!string.IsNullOrEmpty(content))
            {
                var vars = new Dictionary<string, string>
                {
                    ["艾特"] = $"@{context.SenderNick}",
                    ["旺旺"] = context.SenderNick,
                    ["昵称"] = context.SenderNick
                };
                content = MessageTemplateService.Instance.RenderText(content, vars);
                context.Reply(content);
            }

            // 发送图片
            if (rule.SendImage && !string.IsNullOrEmpty(rule.ImageFolder))
            {
                OnSendImage?.Invoke(context.TeamId, rule.ImageFolder);
            }
        }

        private void ExecuteAction(MessageContext context, AutoReplyRule rule)
        {
            switch (rule.ActionType)
            {
                case "SendHistory":
                    SendLotteryHistory(context);
                    break;

                case "SendPersonalData":
                    SendPersonalData(context);
                    break;

                case "SendRules":
                    SendGameRules(context);
                    break;
            }
        }

        private void SendLotteryHistory(MessageContext context)
        {
            // 获取最近开奖历史
            var history = OnGetLotteryHistory?.Invoke(11); // 默认11期
            if (!string.IsNullOrEmpty(history))
            {
                context.Reply(history);
            }
        }

        private void SendPersonalData(MessageContext context)
        {
            var playerId = context.SenderId;
            var playerNick = context.SenderNick;

            var balance = ScoreService.Instance.GetBalance(playerId);
            var stats = ScoreService.Instance.GetTodayStats(playerId);

            var sb = new StringBuilder();
            sb.AppendLine($"@{playerNick} 个人数据");
            sb.AppendLine($"当前余额: {balance:F2}");
            sb.AppendLine($"今日下注: {stats.TotalBet:F2}");
            sb.AppendLine($"今日盈亏: {stats.NetProfit:F2}");
            sb.AppendLine($"下注次数: {stats.BetCount}");

            context.Reply(sb.ToString());
        }

        private void SendGameRules(MessageContext context)
        {
            var rulesContent = MessageTemplateService.Instance.GetTemplate("发送规矩内容");
            if (!string.IsNullOrEmpty(rulesContent))
            {
                context.Reply(rulesContent);
            }
            else
            {
                context.Reply("暂无游戏规则，请联系管理员！");
            }
        }

        #region 事件

        /// <summary>
        /// 发送图片事件
        /// </summary>
        public event Action<string, string> OnSendImage; // teamId, imageFolder

        /// <summary>
        /// 获取开奖历史
        /// </summary>
        public event Func<int, string> OnGetLotteryHistory; // count -> historyText

        #endregion
    }

    #region 规则模型

    /// <summary>
    /// 自动回复规则
    /// </summary>
    public class AutoReplyRule
    {
        public string Name { get; set; }
        public bool Enabled { get; set; } = true;
        public string[] Keywords { get; set; }
        public bool ExactMatch { get; set; } = false; // 是否精确匹配
        public AutoReplyType ReplyType { get; set; } = AutoReplyType.Text;
        public string ReplyContent { get; set; }
        public string TemplateKey { get; set; }
        public string ActionType { get; set; }
        public bool SendImage { get; set; } = false;
        public string ImageFolder { get; set; }
    }

    /// <summary>
    /// 回复类型
    /// </summary>
    public enum AutoReplyType
    {
        /// <summary>纯文本回复</summary>
        Text,
        /// <summary>模板回复</summary>
        Template,
        /// <summary>执行动作</summary>
        Action
    }

    #endregion
}
