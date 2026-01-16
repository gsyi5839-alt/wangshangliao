using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace WangShangLiaoBot.Services
{
    /// <summary>
    /// 消息模板服务 - 基于招财狗(ZCG)的消息模板系统
    /// 支持变量替换：[艾特]、[旺旺]、[余粮]、[玩家攻击]、[期数]、[开奖号码]等
    /// </summary>
    public sealed class MessageTemplateService
    {
        private static MessageTemplateService _instance;
        public static MessageTemplateService Instance => _instance ?? (_instance = new MessageTemplateService());

        private Dictionary<string, string> _templates = new Dictionary<string, string>();
        private readonly object _lock = new object();

        private MessageTemplateService()
        {
            LoadTemplates();
        }

        private string TemplatePath => Path.Combine(DataService.Instance.DatabaseDir, "message-templates.ini");

        #region 模板定义

        /// <summary>
        /// 默认模板配置
        /// </summary>
        private static readonly Dictionary<string, string> DefaultTemplates = new Dictionary<string, string>
        {
            // 下注相关
            ["下注显示"] = "[艾特]([旺旺])\n本次攻擊:[玩家攻击],余粮:[余粮]",
            ["重复下注"] = "[艾特]([旺旺])\n您已下注过了，请勿重复下注！",
            ["余粮不足"] = "[艾特]([旺旺])\n余粮不足，请先上分！当前余额:[余粮]",
            ["攻击上分有效"] = "[艾特]([旺旺])\n余粮不足，上芬后录取，[下注内容]",
            ["超出范围"] = "[艾特]([旺旺])\n您攻击的[下注内容]分数不能[高低]，请及时修改攻击",
            ["取消下注"] = "[艾特]([旺旺])\n取消了下注！！！",
            ["模糊匹配提醒"] = "[艾特]([旺旺])\n已为您模糊匹配攻擊:[模糊攻击],如果不对请重新发起攻擊",
            ["已封盘下注无效"] = "[艾特]([旺旺])\n攻擊慢了，攻擊要快，姿势要帅！\n未处理：[未处理内容]",

            // 托管相关
            ["托管成功"] = "[艾特]([旺旺])\n已为您托管：[托管内容]\n下局自动为您攻击\n如果失败则自动取消托管",
            ["取消托管"] = "[艾特]([旺旺])\n已为您取消托管成功，请重新发起下注！",

            // 上分相关
            ["上分到词"] = "[艾特] [分数]到\n粮库:[余粮]\n您的分已为您上到游戏中，祝您大吉大利，大放异彩，旗开得胜！！",
            ["上分到词_0分"] = "[艾特][分数]到\n粮库:[余粮]\n您的分已为您上到游戏中，祝您大吉大利，大放异彩，旗开得胜！！",
            ["上分到词_第二条"] = "恭喜老板上分成功,祝您多多爆庄",
            ["上分没到词"] = "[艾特] 没到，请您联系接单核实原因！",
            ["客户上分回复"] = "[艾特]([旺旺])\n亲，您的上分请求我们正在火速审核~",

            // 下分相关
            ["下分查分词"] = "[艾特] [分数]查\n粮库:[留分]\n老板请查收及核实，已经转账到您的账号了，感谢老板对我们的支持，祝您一路发！！！",
            ["下分查分词_第二条"] = "欢迎老板再次提款~~",
            ["下分拒绝词"] = "[艾特] 拒绝吓芬，请您联系接单核实原因！",
            ["下分勿催词"] = "[艾特] 您的提款请求我们正在火速审核，请不要催促，耐心稍等！",
            ["客户下分回复"] = "[艾特]([旺旺])\n已收到回芬[分数]请求\n亲，您的提款请求我们正在火速审核~~",
            ["下分正在处理"] = "[艾特]([旺旺])\n下分正在处理中，请稍等！",
            ["下注不能下分"] = "[艾特]([旺旺])\n正在攻擊，不能下芬。",
            ["下分不能下注"] = "[艾特]([旺旺])\n正在下芬，禁止攻擊。",
            ["下分最少下注次数"] = "[艾特]([旺旺])\n补给回粮后至少攻擊[目标次数]把起才可回粮,当前攻擊了[下注次数]把",
            ["下分一次性回"] = "[艾特]([旺旺])\n低于[最低下分],请一次性回芬！",

            // 封盘相关
            ["封盘提示"] = "--距离封盘时间还有[封盘倒计时]秒--\n改注加注带改 或者 加",
            ["封盘内容"] = "========封盘线=======\n以上有钱的都接\n=====庄显为准=======",
            ["发送规矩内容"] = "本群如遇卡奖情况，十分钟官网没开奖，本期无效，无需纠结！！！！",

            // 开奖相关
            ["开奖发送"] = "开:[一区]+[二区]+[三区]=[开奖号码] [大小单双] 第[期数]期",
            ["账单发送"] = "開:[一区] + [二区] + [三区] = [开奖号码] [大小单双] [豹顺对子] [龙虎豹]\n人數:[客户人数]  總分:[总分数]",

            // 查询相关
            ["发1_0分"] = "[艾特]([旺旺])\n老板，您的账户余额不足！\n当前余粮:[余粮]",
            ["发1_有分无攻击"] = "[艾特]([旺旺])\n您本次暂无攻擊,余粮:[余粮]",
            ["发1_有分有攻击"] = "[艾特]([旺旺])\n本次攻擊:[下注],余粮:[余粮]",

            // 回水相关
            ["返点_有回水回复"] = "[艾特]([旺旺])\n本次回水[分数],余粮：[余粮]",
            ["返点_无回水回复"] = "[艾特]([旺旺])\n本次回水0",
            ["返点_把数不达标回复"] = "[艾特]([旺旺])\n把数不足[把数]把",

            // 其他
            ["进群私聊玩家"] = "恭喜发财，私聊都是骗子，请认准管理。",
            ["私聊尾巴_未封盘"] = "离作业做题结束还有[封盘倒计时]秒",
            ["私聊尾巴_已封盘"] = "作业做题已结束",
            ["禁止点09"] = "禁止点09",

            // 自动回复关键词配置
            ["自动回复_历史关键词"] = "历史|发历史|开奖历史|历史发|发下历史|2",
            ["自动回复_账单关键词"] = "账单|数据|我有下注吗|下注情况|1",
            ["自动回复_财付通关键词"] = "财富|发财富|财付通|发财付通|cf|CF|caif|财付",
            ["自动回复_支付宝关键词"] = "支付|支付宝|发支付宝|发支付|zf|ZF",
            ["自动回复_微信关键词"] = "微信|发微信|微信多少|微信号"
        };

        #endregion

        #region 模板加载/保存

        private void LoadTemplates()
        {
            lock (_lock)
            {
                _templates = new Dictionary<string, string>(DefaultTemplates);

                try
                {
                    if (!File.Exists(TemplatePath)) return;

                    var lines = File.ReadAllLines(TemplatePath, Encoding.UTF8);
                    string currentKey = null;
                    var currentValue = new StringBuilder();

                    foreach (var line in lines)
                    {
                        if (line.StartsWith("#")) continue;

                        // 检测新的key
                        var match = Regex.Match(line, @"^\[(.+)\]$");
                        if (match.Success)
                        {
                            // 保存之前的模板
                            if (currentKey != null)
                            {
                                _templates[currentKey] = currentValue.ToString().TrimEnd('\r', '\n');
                            }

                            currentKey = match.Groups[1].Value;
                            currentValue.Clear();
                        }
                        else if (currentKey != null)
                        {
                            if (currentValue.Length > 0)
                                currentValue.AppendLine();
                            currentValue.Append(line);
                        }
                    }

                    // 保存最后一个模板
                    if (currentKey != null)
                    {
                        _templates[currentKey] = currentValue.ToString().TrimEnd('\r', '\n');
                    }
                }
                catch
                {
                    // 保持默认模板
                }
            }
        }

        /// <summary>
        /// 保存所有模板
        /// </summary>
        public void SaveTemplates()
        {
            lock (_lock)
            {
                try
                {
                    Directory.CreateDirectory(DataService.Instance.DatabaseDir);
                    var sb = new StringBuilder();
                    sb.AppendLine("# 消息模板配置 - 自动生成");
                    sb.AppendLine("# 支持变量: [艾特] [旺旺] [余粮] [玩家攻击] [期数] [开奖号码] 等");
                    sb.AppendLine();

                    foreach (var kv in _templates)
                    {
                        sb.AppendLine($"[{kv.Key}]");
                        sb.AppendLine(kv.Value);
                        sb.AppendLine();
                    }

                    File.WriteAllText(TemplatePath, sb.ToString(), Encoding.UTF8);
                }
                catch
                {
                    // ignore
                }
            }
        }

        #endregion

        #region 模板获取/设置

        /// <summary>
        /// 获取模板
        /// </summary>
        public string GetTemplate(string key)
        {
            lock (_lock)
            {
                return _templates.TryGetValue(key, out var template) ? template : "";
            }
        }

        /// <summary>
        /// 设置模板
        /// </summary>
        public void SetTemplate(string key, string template)
        {
            lock (_lock)
            {
                _templates[key] = template ?? "";
            }
        }

        /// <summary>
        /// 获取所有模板
        /// </summary>
        public Dictionary<string, string> GetAllTemplates()
        {
            lock (_lock)
            {
                return new Dictionary<string, string>(_templates);
            }
        }

        #endregion

        #region 模板渲染

        /// <summary>
        /// 渲染模板，替换所有变量
        /// </summary>
        public string Render(string templateKey, Dictionary<string, string> variables)
        {
            var template = GetTemplate(templateKey);
            if (string.IsNullOrEmpty(template)) return "";

            return RenderText(template, variables);
        }

        /// <summary>
        /// 直接渲染文本
        /// </summary>
        public string RenderText(string text, Dictionary<string, string> variables)
        {
            if (string.IsNullOrEmpty(text)) return "";
            if (variables == null || variables.Count == 0) return text;

            var result = text;

            foreach (var kv in variables)
            {
                // 支持 [变量名] 和 {变量名} 两种格式
                result = result.Replace($"[{kv.Key}]", kv.Value ?? "");
                result = result.Replace($"{{{kv.Key}}}", kv.Value ?? "");
            }

            // 处理特殊变量
            result = result.Replace("[换行]", "\n");
            result = result.Replace("[空格]", " ");

            return result;
        }

        /// <summary>
        /// 创建下注成功的变量字典
        /// </summary>
        public Dictionary<string, string> CreateBetVariables(
            string playerNick,
            string playerId,
            string betContent,
            decimal balance,
            decimal betAmount)
        {
            return new Dictionary<string, string>
            {
                ["艾特"] = $"@{playerNick}",
                ["旺旺"] = playerNick,
                ["昵称"] = playerNick,
                ["玩家ID"] = playerId,
                ["玩家攻击"] = betContent,
                ["下注内容"] = betContent,
                ["下注"] = betContent,
                ["余粮"] = balance.ToString("F2", CultureInfo.InvariantCulture),
                ["余额"] = balance.ToString("F2", CultureInfo.InvariantCulture),
                ["分数"] = betAmount.ToString("F2", CultureInfo.InvariantCulture),
                ["金额"] = betAmount.ToString("F2", CultureInfo.InvariantCulture)
            };
        }

        /// <summary>
        /// 创建上下分的变量字典
        /// </summary>
        public Dictionary<string, string> CreateScoreVariables(
            string playerNick,
            decimal amount,
            decimal balanceBefore,
            decimal balanceAfter)
        {
            return new Dictionary<string, string>
            {
                ["艾特"] = $"@{playerNick}",
                ["旺旺"] = playerNick,
                ["昵称"] = playerNick,
                ["分数"] = amount.ToString("F2", CultureInfo.InvariantCulture),
                ["金额"] = amount.ToString("F2", CultureInfo.InvariantCulture),
                ["余粮"] = balanceAfter.ToString("F2", CultureInfo.InvariantCulture),
                ["余额"] = balanceAfter.ToString("F2", CultureInfo.InvariantCulture),
                ["留分"] = balanceAfter.ToString("F2", CultureInfo.InvariantCulture),
                ["原余额"] = balanceBefore.ToString("F2", CultureInfo.InvariantCulture)
            };
        }

        /// <summary>
        /// 创建开奖的变量字典
        /// </summary>
        public Dictionary<string, string> CreateLotteryVariables(
            string period,
            int d1, int d2, int d3, int sum,
            int playerCount,
            decimal totalBet)
        {
            bool isBig = sum >= 14;
            bool isOdd = sum % 2 == 1;

            string bigSmall = isBig ? "大" : "小";
            string oddEven = isOdd ? "单" : "双";
            string dxds = bigSmall + oddEven;

            // 判断特殊类型
            string special = "";
            if (d1 == d2 && d2 == d3) special = "豹子";
            else
            {
                var sorted = new[] { d1, d2, d3 };
                Array.Sort(sorted);
                if (sorted[2] - sorted[1] == 1 && sorted[1] - sorted[0] == 1) special = "顺子";
                else if (d1 == d2 || d2 == d3 || d1 == d3) special = "对子";
            }

            // 龙虎豹
            string dragonTiger = "";
            if (sum % 3 == 0) dragonTiger = "龙";
            else if (sum % 3 == 1) dragonTiger = "虎";
            else dragonTiger = "豹";

            return new Dictionary<string, string>
            {
                ["期数"] = period,
                ["期号"] = period,
                ["一区"] = d1.ToString(),
                ["二区"] = d2.ToString(),
                ["三区"] = d3.ToString(),
                ["开奖号码"] = sum.ToString(),
                ["和值"] = sum.ToString(),
                ["大小"] = bigSmall,
                ["单双"] = oddEven,
                ["大小单双"] = dxds,
                ["豹顺对子"] = special,
                ["特殊类型"] = special,
                ["龙虎豹"] = dragonTiger,
                ["龙虎"] = dragonTiger,
                ["客户人数"] = playerCount.ToString(),
                ["人数"] = playerCount.ToString(),
                ["总分数"] = totalBet.ToString("F2", CultureInfo.InvariantCulture),
                ["总下注"] = totalBet.ToString("F2", CultureInfo.InvariantCulture)
            };
        }

        #endregion

        #region 自动回复关键词检测

        /// <summary>
        /// 检查消息是否匹配自动回复关键词
        /// </summary>
        /// <returns>匹配的关键词类型，null表示不匹配</returns>
        public string CheckAutoReplyKeyword(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return null;

            var keywordTypes = new[]
            {
                ("自动回复_历史关键词", "历史"),
                ("自动回复_账单关键词", "账单"),
                ("自动回复_财付通关键词", "财付通"),
                ("自动回复_支付宝关键词", "支付宝"),
                ("自动回复_微信关键词", "微信")
            };

            foreach (var (templateKey, replyType) in keywordTypes)
            {
                var keywords = GetTemplate(templateKey);
                if (string.IsNullOrEmpty(keywords)) continue;

                var keywordList = keywords.Split('|');
                foreach (var kw in keywordList)
                {
                    if (string.IsNullOrEmpty(kw)) continue;
                    if (message.Contains(kw.Trim()))
                        return replyType;
                }
            }

            return null;
        }

        #endregion
    }
}
