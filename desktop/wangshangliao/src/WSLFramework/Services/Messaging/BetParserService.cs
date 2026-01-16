using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using WSLFramework.Utils;

namespace WSLFramework.Services
{
    /// <summary>
    /// 下注解析服务 - 完全匹配招财狗下注规则
    /// </summary>
    public class BetParserService
    {
        private static BetParserService _instance;
        public static BetParserService Instance => _instance ?? (_instance = new BetParserService());
        
        // 下注类型正则
        private readonly Dictionary<string, Regex> _betPatterns;
        
        // 下注类型配置
        private readonly Dictionary<string, BetTypeConfig> _betTypes;
        
        public event Action<string> OnLog;
        
        private BetParserService()
        {
            _betPatterns = new Dictionary<string, Regex>();
            _betTypes = new Dictionary<string, BetTypeConfig>();
            InitializeBetTypes();
        }
        
        /// <summary>
        /// 初始化下注类型
        /// </summary>
        private void InitializeBetTypes()
        {
            // 大小单双
            AddBetType("大", new[] { "大", "da", "DA" }, 2.0m, "大");
            AddBetType("小", new[] { "小", "xiao", "XIAO" }, 2.0m, "小");
            AddBetType("单", new[] { "单", "dan", "DAN" }, 2.0m, "单");
            AddBetType("双", new[] { "双", "shuang", "SHUANG" }, 2.0m, "双");
            
            // 组合
            AddBetType("大单", new[] { "大单", "dd" }, 4.0m, "大单");
            AddBetType("大双", new[] { "大双", "ds" }, 4.0m, "大双");
            AddBetType("小单", new[] { "小单", "xd" }, 4.0m, "小单");
            AddBetType("小双", new[] { "小双", "xs" }, 4.0m, "小双");
            
            // 龙虎
            AddBetType("龙", new[] { "龙", "long", "LONG" }, 2.0m, "龙");
            AddBetType("虎", new[] { "虎", "hu", "HU" }, 2.0m, "虎");
            AddBetType("和", new[] { "和", "he", "HE" }, 8.0m, "和");
            
            // 顺子豹子
            AddBetType("顺子", new[] { "顺", "顺子", "shun", "sz" }, 6.0m, "顺子");
            AddBetType("豹子", new[] { "豹", "豹子", "bao", "bz" }, 18.0m, "豹子");
            AddBetType("对子", new[] { "对", "对子", "dui", "dz" }, 3.0m, "对子");
            
            // 尾数
            for (int i = 0; i <= 9; i++)
            {
                AddBetType($"尾{i}", new[] { $"尾{i}", $"w{i}" }, 10.0m, $"尾{i}");
            }
            
            // 数字下注 (0-27)
            for (int i = 0; i <= 27; i++)
            {
                var odds = GetNumberOdds(i);
                AddBetType(i.ToString(), new[] { i.ToString() }, odds, i.ToString());
            }
            
            // 初始化正则表达式
            InitializePatterns();
        }
        
        /// <summary>
        /// 根据数字获取赔率
        /// </summary>
        private decimal GetNumberOdds(int num)
        {
            // 0和27赔率最高, 中间数字赔率较低
            var baseOdds = new[] { 100, 50, 25, 16, 12, 10, 8, 7, 6, 5.5m, 5, 5, 5.5m, 6, 7, 8, 10, 12, 16, 25, 50, 100 };
            if (num >= 0 && num <= 27)
            {
                if (num <= 13) return baseOdds[num];
                return baseOdds[27 - num];
            }
            return 5m;
        }
        
        /// <summary>
        /// 添加下注类型
        /// </summary>
        private void AddBetType(string name, string[] aliases, decimal odds, string displayName)
        {
            _betTypes[name.ToLower()] = new BetTypeConfig
            {
                Name = name,
                Aliases = aliases,
                Odds = odds,
                DisplayName = displayName,
                Enabled = true
            };
        }
        
        /// <summary>
        /// 初始化解析正则
        /// </summary>
        private void InitializePatterns()
        {
            // 格式1: 类型+金额 (大100, 龙500, D100, DD100)
            _betPatterns["type_amount"] = new Regex(@"^([大小单双龙虎和顺豹对]|大单|大双|小单|小双|顺子|豹子|对子|尾\d|[dxDX][dDsS]?|极大|极小)(\d+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            
            // 格式2: 金额+类型 (100大, 500龙)
            _betPatterns["amount_type"] = new Regex(@"^(\d+)([大小单双龙虎和顺豹对]|大单|大双|小单|小双|顺子|豹子|对子|尾\d)$", RegexOptions.Compiled);
            
            // 格式3: 数字下注 (18/100 或 18下100 或 18压100 或 单独数字18)
            _betPatterns["number_bet"] = new Regex(@"^(\d{1,2})(?:[/下压押])?(\d+)$", RegexOptions.Compiled);
            
            // 格式4: 尾数下注 (尾5/100 或 W5/100)
            _betPatterns["tail_bet"] = new Regex(@"^[尾wW](\d)[/下]?(\d+)$", RegexOptions.Compiled);
            
            // 格式5: 多重下注 (大100小200)
            _betPatterns["multi_bet"] = new Regex(@"([大小单双龙虎和顺豹对]|[dxDX][dDsS]?)(\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            
            // 格式6: 上分请求 (上500, +500, c500, C500)
            _betPatterns["up"] = new Regex(@"^[上\+cC](\d+)$", RegexOptions.Compiled);
            
            // 格式7: 下分请求 (查500, 下500, -500)
            _betPatterns["down"] = new Regex(@"^[查下\-](\d+)$", RegexOptions.Compiled);
            
            // 格式8: 余额查询 (1, 2, 3, 查, 余额)
            _betPatterns["query"] = new Regex(@"^([123]|查|余额|YE)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }
        
        /// <summary>
        /// 解析下注内容
        /// </summary>
        public BetResult Parse(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return null;
                
            content = content.Trim();
            var normalizedContent = content.ToLower();
            
            // 移除@和空格
            normalizedContent = Regex.Replace(normalizedContent, @"@\S+\s*", "");
            normalizedContent = normalizedContent.Replace(" ", "");
            
            // 优先检查上下分和查询
            var upDownResult = TryParseUpDown(normalizedContent);
            if (upDownResult != null) return upDownResult;
            
            var queryResult = TryParseQuery(normalizedContent);
            if (queryResult != null) return queryResult;
            
            // 尝试各种下注格式
            var result = TryParseTypeAmount(normalizedContent);
            if (result != null) return result;
            
            result = TryParseAmountType(normalizedContent);
            if (result != null) return result;
            
            result = TryParseNumberBet(normalizedContent);
            if (result != null) return result;
            
            result = TryParseTailBet(normalizedContent);
            if (result != null) return result;
            
            result = TryParseMultiBet(normalizedContent);
            if (result != null) return result;
            
            return null;
        }
        
        /// <summary>
        /// 解析上分/下分请求
        /// </summary>
        private BetResult TryParseUpDown(string content)
        {
            // 上分: 上500, +500, c500, C500
            var upMatch = _betPatterns["up"].Match(content);
            if (upMatch.Success && decimal.TryParse(upMatch.Groups[1].Value, out var upAmount))
            {
                return new BetResult
                {
                    IsValid = true,
                    BetType = "UP",
                    Amount = upAmount,
                    IsUpDown = true,
                    Bets = new List<SingleBet> { new SingleBet { BetType = "UP", Amount = upAmount } }
                };
            }
            
            // 下分: 查500, 下500, -500
            var downMatch = _betPatterns["down"].Match(content);
            if (downMatch.Success && decimal.TryParse(downMatch.Groups[1].Value, out var downAmount))
            {
                return new BetResult
                {
                    IsValid = true,
                    BetType = "DOWN",
                    Amount = downAmount,
                    IsUpDown = true,
                    Bets = new List<SingleBet> { new SingleBet { BetType = "DOWN", Amount = downAmount } }
                };
            }
            
            return null;
        }
        
        /// <summary>
        /// 解析余额查询
        /// </summary>
        private BetResult TryParseQuery(string content)
        {
            if (_betPatterns["query"].IsMatch(content))
            {
                return new BetResult
                {
                    IsValid = true,
                    BetType = "QUERY",
                    IsQuery = true,
                    Bets = new List<SingleBet>()
                };
            }
            return null;
        }
        
        /// <summary>
        /// 解析: 类型+金额
        /// </summary>
        private BetResult TryParseTypeAmount(string content)
        {
            var match = _betPatterns["type_amount"].Match(content);
            if (!match.Success) return null;
            
            var betType = NormalizeBetType(match.Groups[1].Value);
            if (!decimal.TryParse(match.Groups[2].Value, out var amount)) return null;
            
            return CreateBetResult(betType, amount);
        }
        
        /// <summary>
        /// 解析: 金额+类型
        /// </summary>
        private BetResult TryParseAmountType(string content)
        {
            var match = _betPatterns["amount_type"].Match(content);
            if (!match.Success) return null;
            
            if (!decimal.TryParse(match.Groups[1].Value, out var amount)) return null;
            var betType = NormalizeBetType(match.Groups[2].Value);
            
            return CreateBetResult(betType, amount);
        }
        
        /// <summary>
        /// 解析: 数字下注
        /// </summary>
        private BetResult TryParseNumberBet(string content)
        {
            var match = _betPatterns["number_bet"].Match(content);
            if (!match.Success) return null;
            
            if (!int.TryParse(match.Groups[1].Value, out var num)) return null;
            if (num < 0 || num > 27) return null;
            if (!decimal.TryParse(match.Groups[2].Value, out var amount)) return null;
            
            return CreateBetResult(num.ToString(), amount);
        }
        
        /// <summary>
        /// 解析: 尾数下注
        /// </summary>
        private BetResult TryParseTailBet(string content)
        {
            var match = _betPatterns["tail_bet"].Match(content);
            if (!match.Success) return null;
            
            var tailNum = match.Groups[1].Value;
            if (!decimal.TryParse(match.Groups[2].Value, out var amount)) return null;
            
            return CreateBetResult($"尾{tailNum}", amount);
        }
        
        /// <summary>
        /// 解析: 多重下注
        /// </summary>
        private BetResult TryParseMultiBet(string content)
        {
            var matches = _betPatterns["multi_bet"].Matches(content);
            if (matches.Count == 0) return null;
            
            var result = new BetResult { IsValid = true, Bets = new List<SingleBet>() };
            
            foreach (Match match in matches)
            {
                var betType = NormalizeBetType(match.Groups[1].Value);
                if (!decimal.TryParse(match.Groups[2].Value, out var amount)) continue;
                
                if (_betTypes.TryGetValue(betType.ToLower(), out var config))
                {
                    result.Bets.Add(new SingleBet
                    {
                        BetType = betType,
                        Amount = amount,
                        Odds = config.Odds
                    });
                }
            }
            
            if (result.Bets.Count > 0)
            {
                result.BetType = string.Join("+", result.Bets.ConvertAll(b => b.BetType));
                result.Amount = result.Bets.ConvertAll(b => b.Amount).Sum();
                return result;
            }
            
            return null;
        }
        
        /// <summary>
        /// 创建下注结果
        /// </summary>
        private BetResult CreateBetResult(string betType, decimal amount)
        {
            var normalizedType = betType.ToLower();
            
            if (_betTypes.TryGetValue(normalizedType, out var config) && config.Enabled)
            {
                return new BetResult
                {
                    IsValid = true,
                    BetType = config.DisplayName,
                    Amount = amount,
                    Odds = config.Odds,
                    Bets = new List<SingleBet>
                    {
                        new SingleBet { BetType = config.DisplayName, Amount = amount, Odds = config.Odds }
                    }
                };
            }
            
            return null;
        }
        
        /// <summary>
        /// 标准化下注类型
        /// </summary>
        private string NormalizeBetType(string input)
        {
            var mapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                // 中文简写
                { "顺", "顺子" }, { "豹", "豹子" }, { "对", "对子" },
                // 英文缩写 (ZCG标准格式)
                { "d", "大" }, { "x", "小" }, 
                { "dd", "大单" }, { "ds", "大双" }, { "xd", "小单" }, { "xs", "小双" },
                { "da", "大" }, { "xiao", "小" }, { "dan", "单" }, { "shuang", "双" },
                { "long", "龙" }, { "hu", "虎" }, { "he", "和" },
                // 极值
                { "极大", "极大" }, { "极小", "极小" },
                { "jd", "极大" }, { "jx", "极小" },
                // 尾数
                { "w0", "尾0" }, { "w1", "尾1" }, { "w2", "尾2" }, { "w3", "尾3" }, { "w4", "尾4" },
                { "w5", "尾5" }, { "w6", "尾6" }, { "w7", "尾7" }, { "w8", "尾8" }, { "w9", "尾9" }
            };
            
            if (mapping.TryGetValue(input, out var mapped))
                return mapped;
                
            return input;
        }
        
        /// <summary>
        /// 获取下注类型配置
        /// </summary>
        public BetTypeConfig GetBetTypeConfig(string betType)
        {
            _betTypes.TryGetValue(betType.ToLower(), out var config);
            return config;
        }
        
        /// <summary>
        /// 设置下注类型启用状态
        /// </summary>
        public void SetBetTypeEnabled(string betType, bool enabled)
        {
            if (_betTypes.ContainsKey(betType.ToLower()))
            {
                _betTypes[betType.ToLower()].Enabled = enabled;
            }
        }
        
        /// <summary>
        /// 设置下注类型赔率
        /// </summary>
        public void SetBetTypeOdds(string betType, decimal odds)
        {
            if (_betTypes.ContainsKey(betType.ToLower()))
            {
                _betTypes[betType.ToLower()].Odds = odds;
            }
        }
        
        private void Log(string message)
        {
            Logger.Info($"[BetParser] {message}");
            OnLog?.Invoke(message);
        }
    }
    
    /// <summary>
    /// 下注结果
    /// </summary>
    public class BetResult
    {
        public bool IsValid { get; set; }
        public string BetType { get; set; }
        public decimal Amount { get; set; }
        public decimal Odds { get; set; }
        public List<SingleBet> Bets { get; set; }
        
        /// <summary>是否为上下分请求</summary>
        public bool IsUpDown { get; set; }
        
        /// <summary>是否为余额查询</summary>
        public bool IsQuery { get; set; }
        
        public decimal PotentialWin => Amount * Odds;
        
        /// <summary>是否为上分</summary>
        public bool IsUp => BetType == "UP";
        
        /// <summary>是否为下分</summary>
        public bool IsDown => BetType == "DOWN";
    }
    
    /// <summary>
    /// 单次下注
    /// </summary>
    public class SingleBet
    {
        public string BetType { get; set; }
        public decimal Amount { get; set; }
        public decimal Odds { get; set; }
    }
    
    /// <summary>
    /// 下注类型配置
    /// </summary>
    public class BetTypeConfig
    {
        public string Name { get; set; }
        public string[] Aliases { get; set; }
        public decimal Odds { get; set; }
        public string DisplayName { get; set; }
        public bool Enabled { get; set; }
    }
    
    /// <summary>
    /// List扩展
    /// </summary>
    public static class ListExtensions
    {
        public static decimal Sum(this List<decimal> list)
        {
            decimal sum = 0;
            foreach (var item in list)
                sum += item;
            return sum;
        }
    }
}
