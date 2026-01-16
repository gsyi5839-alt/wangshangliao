using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using WSLFramework.Utils;

namespace WSLFramework.Services
{
    /// <summary>
    /// 自动回复服务 - 匹配招财狗自动回复功能
    /// 线程安全优化版本
    /// </summary>
    public class AutoReplyService : IDisposable
    {
        #region 单例

        private static readonly Lazy<AutoReplyService> _lazy = 
            new Lazy<AutoReplyService>(() => new AutoReplyService(), LazyThreadSafetyMode.ExecutionAndPublication);

        public static AutoReplyService Instance => _lazy.Value;

        #endregion

        #region 私有字段

        // 使用线程安全集合
        private readonly ConcurrentDictionary<string, ReplyRule> _rules;
        private readonly ConcurrentBag<KeywordRule> _keywordRules;
        private readonly ReaderWriterLockSlim _rulesLock;
        
        private volatile bool _disposed;

        #endregion

        #region 事件

        public event Action<string> OnLog;

        #endregion

        #region 构造函数

        private AutoReplyService()
        {
            _rules = new ConcurrentDictionary<string, ReplyRule>(StringComparer.OrdinalIgnoreCase);
            _keywordRules = new ConcurrentBag<KeywordRule>();
            _rulesLock = new ReaderWriterLockSlim();
            InitializeDefaultRules();
        }

        #endregion

        #region 初始化默认规则

        /// <summary>
        /// 初始化默认规则
        /// </summary>
        private void InitializeDefaultRules()
        {
            // 撤回通知关键字
            AddKeywordRule("撤回通知", new[] { "撤回", "收回", "撤销", "撤单" },
                "[消息撤回提醒]\n玩家已撤回消息");

            // 支付宝收款关键字
            AddKeywordRule("支付宝收款", new[] { "支付宝", "zfb", "ZFB", "阿里", "支付宝转账" },
                "支付宝收款信息已收到，请等待处理");

            // 微信收款关键字
            AddKeywordRule("微信收款", new[] { "微信", "wx", "WX", "微信转账" },
                "微信收款信息已收到，请等待处理");

            // 财付通收款关键字
            AddKeywordRule("财付通收款", new[] { "财付通", "+财付通", "qq钱包", "QQ钱包" },
                "财付通收款信息已收到，请等待处理");

            // 历史查询关键字
            AddKeywordRule("历史查询", new[] { "历史", "记录", "账单", "查账", "+历史", "+记录" },
                null); // 动态生成

            // 余额查询关键字
            AddKeywordRule("余额查询", new[] { "余额", "分数", "查分", "查询", "+余额", "+查分" },
                null); // 动态生成

            // 规则帮助
            AddKeywordRule("规则帮助", new[] { "规则", "玩法", "帮助", "说明" },
                GetHelpText());

            // PC网站关键字
            AddKeywordRule("PC网站", new[] { "网站", "网址", "link", "LINK" },
                null); // 从配置读取

            // 手机端网站关键字
            AddKeywordRule("手机端网站", new[] { "手机", "移动端", "mobile" },
                null); // 从配置读取

            Log("默认自动回复规则已初始化");
        }

        #endregion

        #region 规则管理

        /// <summary>
        /// 添加关键字规则
        /// </summary>
        public void AddKeywordRule(string name, string[] keywords, string reply, bool enabled = true)
        {
            if (string.IsNullOrEmpty(name) || keywords == null || keywords.Length == 0)
                return;

            var rule = new KeywordRule
            {
                Name = name,
                Keywords = keywords,
                Reply = reply,
                Enabled = enabled
            };

            _keywordRules.Add(rule);
            Log($"添加关键字规则: {name} (关键字: {string.Join(",", keywords)})");
        }

        /// <summary>
        /// 添加精确匹配规则
        /// </summary>
        public void AddExactRule(string trigger, string reply)
        {
            if (string.IsNullOrEmpty(trigger))
                return;

            var rule = new ReplyRule
            {
                Trigger = trigger,
                Reply = reply,
                MatchType = MatchType.Exact,
                Enabled = true
            };

            _rules[trigger.ToLower()] = rule;
            Log($"添加精确匹配规则: {trigger}");
        }

        /// <summary>
        /// 添加正则匹配规则
        /// </summary>
        public void AddRegexRule(string pattern, string reply)
        {
            if (string.IsNullOrEmpty(pattern))
                return;

            var rule = new ReplyRule
            {
                Trigger = pattern,
                Reply = reply,
                MatchType = MatchType.Regex,
                Enabled = true
            };

            _rules[$"regex:{pattern}"] = rule;
            Log($"添加正则匹配规则: {pattern}");
        }

        /// <summary>
        /// 移除规则
        /// </summary>
        public bool RemoveRule(string trigger)
        {
            return _rules.TryRemove(trigger.ToLower(), out _);
        }

        /// <summary>
        /// 清空所有规则
        /// </summary>
        public void ClearRules()
        {
            _rules.Clear();
            while (_keywordRules.TryTake(out _)) { }
            InitializeDefaultRules();
        }

        /// <summary>
        /// 启用/禁用规则
        /// </summary>
        public void SetRuleEnabled(string name, bool enabled)
        {
            // 查找关键字规则
            foreach (var rule in _keywordRules)
            {
                if (rule.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    rule.Enabled = enabled;
                    Log($"规则 {name} 已{(enabled ? "启用" : "禁用")}");
                    return;
                }
            }

            // 查找精确匹配规则
            if (_rules.TryGetValue(name.ToLower(), out var exactRule))
            {
                exactRule.Enabled = enabled;
                Log($"规则 {name} 已{(enabled ? "启用" : "禁用")}");
            }
        }

        /// <summary>
        /// 获取所有关键字规则
        /// </summary>
        public IEnumerable<KeywordRule> GetAllKeywordRules()
        {
            return _keywordRules.ToArray();
        }

        /// <summary>
        /// 获取所有精确匹配规则
        /// </summary>
        public IEnumerable<ReplyRule> GetAllExactRules()
        {
            return _rules.Values.ToArray();
        }

        #endregion

        #region 回复匹配

        /// <summary>
        /// 获取自动回复
        /// </summary>
        public string GetReply(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return null;

            var lowerContent = content.ToLower().Trim();

            // 1. 精确匹配
            if (_rules.TryGetValue(lowerContent, out var exactRule) && exactRule.Enabled)
            {
                Log($"触发精确匹配: {exactRule.Trigger}");
                return exactRule.Reply;
            }

            // 2. 关键字匹配
            foreach (var keywordRule in _keywordRules)
            {
                if (!keywordRule.Enabled) continue;

                foreach (var keyword in keywordRule.Keywords)
                {
                    if (lowerContent.Contains(keyword.ToLower()))
                    {
                        Log($"触发关键字匹配: {keywordRule.Name} ({keyword})");
                        return keywordRule.Reply;
                    }
                }
            }

            // 3. 正则匹配
            foreach (var rule in _rules.Values)
            {
                if (!rule.Enabled || rule.MatchType != MatchType.Regex) continue;

                try
                {
                    if (Regex.IsMatch(content, rule.Trigger, RegexOptions.IgnoreCase))
                    {
                        Log($"触发正则匹配: {rule.Trigger}");
                        return rule.Reply;
                    }
                }
                catch (Exception ex)
                {
                    Log($"正则匹配异常 ({rule.Trigger}): {ex.Message}");
                }
            }

            return null;
        }

        /// <summary>
        /// 检查是否匹配任何规则
        /// </summary>
        public bool IsMatch(string content, out string ruleName)
        {
            ruleName = null;

            if (string.IsNullOrWhiteSpace(content))
                return false;

            var lowerContent = content.ToLower().Trim();

            // 精确匹配
            if (_rules.TryGetValue(lowerContent, out var exactRule) && exactRule.Enabled)
            {
                ruleName = exactRule.Trigger;
                return true;
            }

            // 关键字匹配
            foreach (var keywordRule in _keywordRules)
            {
                if (!keywordRule.Enabled) continue;

                foreach (var keyword in keywordRule.Keywords)
                {
                    if (lowerContent.Contains(keyword.ToLower()))
                    {
                        ruleName = keywordRule.Name;
                        return true;
                    }
                }
            }

            return false;
        }

        #endregion

        #region 预设回复模板

        /// <summary>
        /// 获取玩家余额回复
        /// </summary>
        public string GetBalanceReply(string playerId, decimal balance)
        {
            var shortId = GetShortId(playerId);
            return $"({shortId})\n" +
                   $"老板，您的账户有余额！\n" +
                   $"当前余额:{balance}";
        }

        /// <summary>
        /// 获取下注提示回复
        /// </summary>
        public string GetBetHintReply(string playerId, decimal balance = 0)
        {
            var shortId = GetShortId(playerId);
            return $"@{shortId} ({shortId})\n" +
                   $"老板，您的账户有余额！\n" +
                   $"当前余额:{balance}";
        }

        /// <summary>
        /// 获取托管成功回复
        /// </summary>
        public string GetTrusteeSuccessReply()
        {
            return "托管成功！\n" +
                   "托管期间将自动处理下注、上下分等操作\n" +
                   "如需取消托管，请回复\"取消托管\"";
        }

        /// <summary>
        /// 获取取消托管回复
        /// </summary>
        public string GetTrusteeStopReply()
        {
            return "托管已取消\n" +
                   "如需重新开启，请使用相关指令";
        }

        /// <summary>
        /// 获取取消下注回复
        /// </summary>
        public string GetCancelBetReply()
        {
            return "取消下注成功！！！";
        }

        /// <summary>
        /// 获取封盘提示
        /// </summary>
        public string GetSealingReply()
        {
            return "封盘中，下期再来！";
        }

        /// <summary>
        /// 获取下注成功回复
        /// </summary>
        public string GetBetSuccessReply(string playerId, string betType, decimal amount)
        {
            var shortId = GetShortId(playerId);
            return $"@{shortId} ({shortId})\n" +
                   $"下注成功: {betType} {amount}";
        }

        /// <summary>
        /// 获取下注一览回复
        /// </summary>
        public string GetBetOverviewReply()
        {
            return "==账分排单==\n" +
                   "加钱的都是谁\n" +
                   "==庄家为准==";
        }

        /// <summary>
        /// 获取上分成功回复
        /// </summary>
        public string GetTopUpSuccessReply(string playerId, decimal amount, decimal balance)
        {
            var shortId = GetShortId(playerId);
            return $"@{shortId}\n" +
                   $"上分成功！\n" +
                   $"上分金额: {amount}\n" +
                   $"当前余额: {balance}";
        }

        /// <summary>
        /// 获取下分成功回复
        /// </summary>
        public string GetCashOutSuccessReply(string playerId, decimal amount, decimal balance)
        {
            var shortId = GetShortId(playerId);
            return $"@{shortId}\n" +
                   $"下分成功！\n" +
                   $"下分金额: {amount}\n" +
                   $"当前余额: {balance}";
        }

        /// <summary>
        /// 获取上分失败回复
        /// </summary>
        public string GetTopUpFailedReply(string reason = "系统繁忙")
        {
            return $"上分失败\n原因: {reason}\n请联系管理员处理";
        }

        /// <summary>
        /// 获取下分失败回复
        /// </summary>
        public string GetCashOutFailedReply(string reason = "余额不足")
        {
            return $"下分失败\n原因: {reason}";
        }

        /// <summary>
        /// 获取开奖结果回复
        /// </summary>
        public string GetLotteryResultReply(string period, int[] numbers, int total, string result)
        {
            if (numbers == null || numbers.Length < 3)
                return $"期:{period} 结果:{result}";

            return $"期:{numbers[0]}+{numbers[1]}+{numbers[2]}={total} {result} 【{period}】";
        }

        /// <summary>
        /// 获取开奖详情回复
        /// </summary>
        public string GetLotteryDetailReply(string period, int[] numbers, int total, string result, decimal winAmount, decimal loseAmount)
        {
            if (numbers == null || numbers.Length < 3)
                return "";

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"开:{numbers[0]} + {numbers[1]} + {numbers[2]} = {total} {result} -- {GetSizeResult(total)}");
            sb.AppendLine($"杀账:0  盈利:0");
            sb.AppendLine("----------------------");
            sb.AppendLine();
            sb.AppendLine("----------------------");
            return sb.ToString();
        }

        /// <summary>
        /// 获取历史记录回复
        /// </summary>
        public string GetHistoryReply(int[] lastResults, string[] lastTypes, int[] lastTails)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("ls：");
            if (lastResults != null)
            {
                foreach (var r in lastResults)
                {
                    sb.Append($"{r:00} ");
                }
            }
            sb.AppendLine();

            sb.Append("大小单双ls：");
            if (lastTypes != null)
            {
                foreach (var t in lastTypes)
                {
                    sb.Append($"{t} ");
                }
            }
            sb.AppendLine();

            sb.Append("尾数ls：");
            if (lastTails != null)
            {
                foreach (var t in lastTails)
                {
                    sb.Append($"{t} ");
                }
            }
            sb.AppendLine();

            sb.Append("杀顺连龙史分别 ");
            return sb.ToString();
        }

        /// <summary>
        /// 获取帮助文本
        /// </summary>
        public string GetHelpText()
        {
            return "==玩法说明==\n" +
                   "大小单双、龙虎、顺子等\n" +
                   "==下注格式==\n" +
                   "直接发送: 大100 或 100大\n" +
                   "==查询指令==\n" +
                   "1 - 查询余额\n" +
                   "2 - 查询历史\n" +
                   "==上下分==\n" +
                   "请联系管理员";
        }

        /// <summary>
        /// 获取封盘提醒回复
        /// </summary>
        public string GetSealingReminderReply(int remainingSeconds)
        {
            return $"--距离封盘时间还有{remainingSeconds}秒--\n" +
                   "下注中注单来 大小 双";
        }

        #endregion

        #region 辅助方法

        private string GetShortId(string playerId)
        {
            if (string.IsNullOrEmpty(playerId))
                return "0000";
            return playerId.Length > 4 ? playerId.Substring(playerId.Length - 4) : playerId;
        }

        private string GetSizeResult(int total)
        {
            // 3-10: 小, 11-18: 大
            if (total >= 3 && total <= 10)
                return "XD"; // 小单/小双
            if (total >= 11 && total <= 18)
                return "DD"; // 大单/大双
            return "";
        }

        private void Log(string message)
        {
            Logger.Info($"[AutoReply] {message}");
            OnLog?.Invoke(message);
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _rulesLock?.Dispose();
        }

        #endregion
    }

    #region 规则类

    /// <summary>
    /// 回复规则
    /// </summary>
    public class ReplyRule
    {
        public string Trigger { get; set; }
        public string Reply { get; set; }
        public MatchType MatchType { get; set; }
        public volatile bool Enabled;
    }

    /// <summary>
    /// 关键字规则
    /// </summary>
    public class KeywordRule
    {
        public string Name { get; set; }
        public string[] Keywords { get; set; }
        public string Reply { get; set; }
        public volatile bool Enabled;
    }

    /// <summary>
    /// 匹配类型
    /// </summary>
    public enum MatchType
    {
        Exact,      // 精确匹配
        Contains,   // 包含匹配
        Regex       // 正则匹配
    }

    #endregion
}
