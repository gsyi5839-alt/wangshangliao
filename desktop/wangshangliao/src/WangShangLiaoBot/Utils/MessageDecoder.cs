using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace WangShangLiaoBot.Utils
{
    /// <summary>
    /// Message decoder for custom NIM messages
    /// Handles Base64 decoding, binary protocol parsing, and competitor bot format
    /// Based on analysis of competitor bot message formats
    /// </summary>
    public static class MessageDecoder
    {
        // =====================================================
        // Bot Detection Patterns
        // =====================================================
        
        // Known bot account patterns (MD5 hash nicknames)
        private static readonly Regex BotNicknamePattern = new Regex(@"^[0-9a-f]{32}$", RegexOptions.IgnoreCase);
        
        // Known bot keywords in nickname
        private static readonly string[] BotKeywords = { "机器", "客服", "管理员", "系统" };
        
        // =====================================================
        // Gameplay Code Definitions (Based on competitor analysis)
        // =====================================================
        
        /// <summary>
        /// Gameplay code mapping (competitor format)
        /// DA=大, X=小, D=单, S=双, XS=小双, XD=小单, DAS=大双, DAD=大单, SZ=数字
        /// </summary>
        public static readonly Dictionary<string, string> GameplayCodes = new Dictionary<string, string>
        {
            { "DA", "大" },
            { "X", "小" },
            { "D", "单" },
            { "S", "双" },
            { "XS", "小双" },
            { "XD", "小单" },
            { "DAS", "大双" },
            { "DAD", "大单" },
            { "SZ", "数字" },
            { "T", "特码" }
        };
        
        // Regex to match gameplay codes with amounts (e.g., "DA76 XS58" or "X270 DAS290")
        private static readonly Regex GameplayCodePattern = new Regex(
            @"(DA|DAD|DAS|XS|XD|X|D|S|SZ|T)(\d+)", 
            RegexOptions.IgnoreCase);
        
        // =====================================================
        // Message Type Patterns (Competitor Bot Format)
        // =====================================================
        
        // Rules message: contains "====" and "请认准"
        private static readonly Regex RulesMessagePattern = new Regex(
            @"={3,}.*?(天谕|规则|请认准|群号)", 
            RegexOptions.Singleline | RegexOptions.IgnoreCase);
        
        // Settlement message: "核对:" or "城对:" followed by player list
        private static readonly Regex SettlementMessagePattern = new Regex(
            @"(核对|城对)\s*[:：]?\s*[-]+\s*(.+)", 
            RegexOptions.Singleline);
        
        // Settlement line: "昵称 总额 - [玩法] _ 结余"
        private static readonly Regex SettlementLinePattern = new Regex(
            @"(\S+)\s+(\d+)\s*-\s*\[([^\]]+)\]\s*_\s*(\d+)");
        
        // Lottery result: "期号 取餐码: X + Y + Z = 结果"
        private static readonly Regex LotteryResultPattern = new Regex(
            @"(\d+)期\s*[\r\n]*取餐码\s*[:：]\s*(\d+)\s*\+\s*(\d+)\s*\+\s*(\d+)\s*=\s*(\d+)");
        
        // History: "历史: XX XX XX..."
        private static readonly Regex HistoryPattern = new Regex(
            @"历史\s*[:：]\s*([\d\s]+)");
        
        // Attack reply: "昵称 攻击: 玩法 ,$:金额"
        private static readonly Regex AttackReplyPattern = new Regex(
            @"(\S+)\s*[\r\n]*攻击\s*[:：]\s*(.+?)\s*,?\s*\$\s*[:：]?\s*(\d+(?:\.\d+)?)");
        
        // Balance query reply: "昵称] $:余额"
        private static readonly Regex BalanceReplyPattern = new Regex(
            @"(\S+)\s*\]\s*\$\s*[:：]?\s*(\d+(?:\.\d+)?)");
        
        // Insufficient balance: "您攻击的[下注总额]分数不能高于余粮"
        private static readonly Regex InsufficientBalancePattern = new Regex(
            @"(\S+)\s+您攻击的.*?分数不能高于余粮\s*(\d+)");
        
        // Mute notification patterns
        private static readonly Regex MuteEnablePattern = new Regex(
            @"(管理员开启了禁言|mMuteAllMember.*?true)", RegexOptions.IgnoreCase);
        private static readonly Regex MuteDisablePattern = new Regex(
            @"(管理员关闭了禁言|mMuteAllMember.*?false)", RegexOptions.IgnoreCase);
        
        // =====================================================
        // Bot Detection Methods
        // =====================================================
        
        /// <summary>
        /// Check if a nickname indicates a bot account (MD5 hash pattern)
        /// </summary>
        public static bool IsBotNickname(string nickname)
        {
            if (string.IsNullOrEmpty(nickname))
                return false;
            
            // Check MD5 hash pattern
            if (BotNicknamePattern.IsMatch(nickname))
                return true;
            
            // Check bot keywords
            foreach (var keyword in BotKeywords)
            {
                if (nickname.Contains(keyword))
                    return true;
            }
            
            return false;
        }
        
        // =====================================================
        // Base64 Decoding Methods
        // =====================================================
        
        /// <summary>
        /// Decode Base64 content from custom message
        /// Handles URL-safe Base64 variant
        /// </summary>
        public static string DecodeBase64Content(string base64Content)
        {
            if (string.IsNullOrEmpty(base64Content))
                return string.Empty;
            
            try
            {
                // Convert URL-safe Base64 to standard Base64
                var standardB64 = base64Content
                    .Replace('-', '+')
                    .Replace('_', '/');
                
                // Add padding if needed
                var mod = standardB64.Length % 4;
                if (mod > 0)
                    standardB64 += new string('=', 4 - mod);
                
                var bytes = Convert.FromBase64String(standardB64);
                return ExtractReadableText(bytes);
            }
            catch
            {
                return string.Empty;
            }
        }
        
        /// <summary>
        /// Extract readable Chinese text from binary data using UTF-8 decoding
        /// </summary>
        private static string ExtractReadableText(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
                return string.Empty;
            
            var results = new List<string>();
            
            // Try decoding from different starting positions
            // Binary protocol may have header bytes before actual content
            for (int startOffset = 0; startOffset < Math.Min(50, bytes.Length); startOffset++)
            {
                try
                {
                    var subBytes = new byte[bytes.Length - startOffset];
                    Array.Copy(bytes, startOffset, subBytes, 0, subBytes.Length);
                    
                    var text = Encoding.UTF8.GetString(subBytes);
                    
                    // Extract Chinese characters and other readable content
                    var chineseMatches = Regex.Matches(text, @"[\u4e00-\u9fff]+");
                    foreach (Match m in chineseMatches)
                    {
                        if (m.Value.Length >= 2 && !results.Contains(m.Value))
                            results.Add(m.Value);
                    }
                }
                catch { }
            }
            
            return string.Join(" ", results);
        }
        
        /// <summary>
        /// Parse custom message content field
        /// Format: {"b":"BASE64_ENCODED_DATA"}
        /// </summary>
        public static CustomMessageContent ParseCustomContent(string contentJson)
        {
            var result = new CustomMessageContent();
            
            if (string.IsNullOrEmpty(contentJson))
                return result;
            
            try
            {
                // Extract "b" field value using regex (avoid JSON dependency)
                var match = Regex.Match(contentJson, @"""b""\s*:\s*""([^""]+)""");
                if (match.Success)
                {
                    result.Base64Data = match.Groups[1].Value;
                    result.DecodedText = DecodeBase64Content(result.Base64Data);
                }
                
                // Check for mute notification
                if (contentJson.Contains("MuteAllMember"))
                {
                    result.IsMuteNotification = true;
                    result.IsMuteEnabled = contentJson.Contains("\"mMuteAllMember\":true");
                }
            }
            catch { }
            
            return result;
        }
        
        // =====================================================
        // Message Type Classification (Competitor Format)
        // =====================================================
        
        /// <summary>
        /// Classify message type based on content (competitor bot format)
        /// </summary>
        public static CompetitorMessageType ClassifyMessage(string content)
        {
            if (string.IsNullOrEmpty(content))
                return CompetitorMessageType.Unknown;
            
            // Check mute notifications first
            if (MuteEnablePattern.IsMatch(content))
                return CompetitorMessageType.MuteEnable;
            if (MuteDisablePattern.IsMatch(content))
                return CompetitorMessageType.MuteDisable;
            
            // Check rules message
            if (RulesMessagePattern.IsMatch(content))
                return CompetitorMessageType.Rules;
            
            // Check settlement message
            if (SettlementMessagePattern.IsMatch(content))
                return CompetitorMessageType.Settlement;
            
            // Check lottery result
            if (LotteryResultPattern.IsMatch(content))
                return CompetitorMessageType.LotteryResult;
            
            // Check history
            if (HistoryPattern.IsMatch(content))
                return CompetitorMessageType.History;
            
            // Check attack reply
            if (AttackReplyPattern.IsMatch(content))
                return CompetitorMessageType.AttackReply;
            
            // Check balance reply
            if (BalanceReplyPattern.IsMatch(content))
                return CompetitorMessageType.BalanceReply;
            
            // Check insufficient balance
            if (InsufficientBalancePattern.IsMatch(content))
                return CompetitorMessageType.InsufficientBalance;
            
            return CompetitorMessageType.Unknown;
        }
        
        // =====================================================
        // Message Parsing Methods (Competitor Format)
        // =====================================================
        
        /// <summary>
        /// Parse lottery result message
        /// Returns: (期号, 数字1, 数字2, 数字3, 结果)
        /// </summary>
        public static LotteryResultInfo ParseLotteryResult(string content)
        {
            var match = LotteryResultPattern.Match(content ?? "");
            if (!match.Success)
                return null;
            
            return new LotteryResultInfo
            {
                Period = match.Groups[1].Value,
                Number1 = int.Parse(match.Groups[2].Value),
                Number2 = int.Parse(match.Groups[3].Value),
                Number3 = int.Parse(match.Groups[4].Value),
                Result = int.Parse(match.Groups[5].Value)
            };
        }
        
        /// <summary>
        /// Parse history message
        /// Returns: list of result numbers
        /// </summary>
        public static List<int> ParseHistory(string content)
        {
            var result = new List<int>();
            var match = HistoryPattern.Match(content ?? "");
            if (!match.Success)
                return result;
            
            var numbersStr = match.Groups[1].Value;
            var numbers = Regex.Matches(numbersStr, @"\d+");
            foreach (Match n in numbers)
            {
                if (int.TryParse(n.Value, out var num))
                    result.Add(num);
            }
            
            return result;
        }
        
        /// <summary>
        /// Parse attack reply message
        /// Returns: (玩家名, 玩法详情, 金额)
        /// </summary>
        public static AttackReplyInfo ParseAttackReply(string content)
        {
            var match = AttackReplyPattern.Match(content ?? "");
            if (!match.Success)
                return null;
            
            var playerName = match.Groups[1].Value.Trim();
            var gameplayStr = match.Groups[2].Value.Trim();
            var amount = decimal.Parse(match.Groups[3].Value);
            
            // Parse gameplay codes
            var gameplayList = ParseGameplayCodes(gameplayStr);
            
            return new AttackReplyInfo
            {
                PlayerName = playerName,
                GameplayString = gameplayStr,
                GameplayList = gameplayList,
                Amount = amount
            };
        }
        
        /// <summary>
        /// Parse settlement message
        /// Returns: list of settlement entries
        /// </summary>
        public static List<CompetitorSettlementEntry> ParseSettlement(string content)
        {
            var result = new List<CompetitorSettlementEntry>();
            if (string.IsNullOrEmpty(content))
                return result;
            
            var matches = SettlementLinePattern.Matches(content);
            foreach (Match m in matches)
            {
                var entry = new CompetitorSettlementEntry
                {
                    PlayerName = m.Groups[1].Value.Trim(),
                    TotalBet = decimal.Parse(m.Groups[2].Value),
                    GameplayString = m.Groups[3].Value.Trim(),
                    Balance = decimal.Parse(m.Groups[4].Value)
                };
                
                // Parse gameplay codes
                entry.GameplayList = ParseGameplayCodes(entry.GameplayString);
                
                result.Add(entry);
            }
            
            return result;
        }
        
        /// <summary>
        /// Parse gameplay codes string (e.g., "DA76 XS58")
        /// Returns: list of (code, chineseName, amount)
        /// </summary>
        public static List<GameplayItem> ParseGameplayCodes(string gameplayStr)
        {
            var result = new List<GameplayItem>();
            if (string.IsNullOrEmpty(gameplayStr))
                return result;
            
            var matches = GameplayCodePattern.Matches(gameplayStr);
            foreach (Match m in matches)
            {
                var code = m.Groups[1].Value.ToUpper();
                var amount = int.Parse(m.Groups[2].Value);
                
                var chineseName = GameplayCodes.ContainsKey(code) 
                    ? GameplayCodes[code] 
                    : code;
                
                result.Add(new GameplayItem
                {
                    Code = code,
                    ChineseName = chineseName,
                    Amount = amount
                });
            }
            
            return result;
        }
        
        /// <summary>
        /// Convert gameplay codes to Chinese (e.g., "DA76 XS58" -> "大76 小双58")
        /// </summary>
        public static string TranslateGameplayCodes(string gameplayStr)
        {
            if (string.IsNullOrEmpty(gameplayStr))
                return gameplayStr;
            
            var result = gameplayStr;
            
            // Replace codes in order of length (longer first to avoid partial matches)
            var sortedCodes = new List<KeyValuePair<string, string>>(GameplayCodes);
            sortedCodes.Sort((a, b) => b.Key.Length.CompareTo(a.Key.Length));
            
            foreach (var kvp in sortedCodes)
            {
                result = Regex.Replace(
                    result, 
                    $@"(?i)\b{kvp.Key}(\d+)", 
                    $"{kvp.Value}$1");
            }
            
            return result;
        }
        
        // =====================================================
        // Message Analysis Methods
        // =====================================================
        
        /// <summary>
        /// Analyze message features for logging
        /// </summary>
        public static MessageFeatures AnalyzeMessage(string content, string nickname, string messageType)
        {
            var features = new MessageFeatures();
            
            // Check bot nickname
            features.IsBot = IsBotNickname(nickname);
            if (features.IsBot)
                features.Tags.Add("机器人");
            
            // Check message type
            features.IsCustomMessage = string.Equals(messageType, "custom", StringComparison.OrdinalIgnoreCase);
            if (features.IsCustomMessage)
                features.Tags.Add("自定义");
            
            // Classify message type (competitor format)
            features.CompetitorType = ClassifyMessage(content);
            
            // Add tags based on competitor message type
            switch (features.CompetitorType)
            {
                case CompetitorMessageType.MuteEnable:
                    features.IsMuteNotification = true;
                    features.Tags.Add("封盘");
                    break;
                case CompetitorMessageType.MuteDisable:
                    features.IsMuteNotification = true;
                    features.Tags.Add("开盘");
                    break;
                case CompetitorMessageType.Rules:
                    features.Tags.Add("规则");
                    break;
                case CompetitorMessageType.Settlement:
                    features.Tags.Add("结算");
                    break;
                case CompetitorMessageType.LotteryResult:
                    features.IsLotteryResult = true;
                    features.Tags.Add("开奖");
                    break;
                case CompetitorMessageType.History:
                    features.Tags.Add("历史");
                    break;
                case CompetitorMessageType.AttackReply:
                    features.Tags.Add("下注确认");
                    break;
                case CompetitorMessageType.BalanceReply:
                    features.Tags.Add("余额查询");
                    break;
                case CompetitorMessageType.InsufficientBalance:
                    features.Tags.Add("余额不足");
                    break;
            }
            
            return features;
        }
        
        /// <summary>
        /// Decode message for display in run log
        /// Returns decoded content for custom messages, original content otherwise
        /// </summary>
        public static string GetDisplayContent(string originalContent, string contentJson, string messageType)
        {
            // For custom messages, try to decode
            if (string.Equals(messageType, "custom", StringComparison.OrdinalIgnoreCase))
            {
                var parsed = ParseCustomContent(contentJson);
                if (!string.IsNullOrEmpty(parsed.DecodedText))
                    return $"[解码] {parsed.DecodedText}";
                
                // Return original if decode failed
                return string.IsNullOrEmpty(originalContent) 
                    ? "[自定义消息-无法解码]" 
                    : originalContent;
            }
            
            return originalContent ?? string.Empty;
        }
        
        /// <summary>
        /// Format message for run log display (competitor format)
        /// </summary>
        public static string FormatForRunLog(string content, CompetitorMessageType msgType)
        {
            switch (msgType)
            {
                case CompetitorMessageType.LotteryResult:
                    var lottery = ParseLotteryResult(content);
                    if (lottery != null)
                        return $"[开奖] {lottery.Period}期 | {lottery.Number1}+{lottery.Number2}+{lottery.Number3}={lottery.Result}";
                    break;
                
                case CompetitorMessageType.AttackReply:
                    var attack = ParseAttackReply(content);
                    if (attack != null)
                    {
                        var translated = TranslateGameplayCodes(attack.GameplayString);
                        return $"[下注] {attack.PlayerName} | {translated} | ${attack.Amount}";
                    }
                    break;
                
                case CompetitorMessageType.MuteEnable:
                    return "[封盘] 管理员开启了禁言";
                
                case CompetitorMessageType.MuteDisable:
                    return "[开盘] 管理员关闭了禁言";
                
                case CompetitorMessageType.History:
                    var history = ParseHistory(content);
                    if (history.Count > 0)
                        return $"[历史] {string.Join(" ", history)}";
                    break;
            }
            
            // Return original content with type prefix
            var prefix = msgType != CompetitorMessageType.Unknown 
                ? $"[{msgType}] " 
                : "";
            return prefix + (content?.Length > 100 ? content.Substring(0, 100) + "..." : content);
        }
    }
    
    // =====================================================
    // Data Classes
    // =====================================================
    
    /// <summary>
    /// Competitor message types
    /// </summary>
    public enum CompetitorMessageType
    {
        Unknown,
        Rules,           // 规则说明
        Settlement,      // 结算账单
        LotteryResult,   // 开奖结果
        History,         // 历史记录
        AttackReply,     // 攻击回复（下注确认）
        BalanceReply,    // 余额查询回复
        InsufficientBalance, // 余额不足提示
        MuteEnable,      // 封盘（禁言）
        MuteDisable      // 开盘（解禁）
    }
    
    /// <summary>
    /// Parsed custom message content
    /// </summary>
    public class CustomMessageContent
    {
        public string Base64Data { get; set; } = string.Empty;
        public string DecodedText { get; set; } = string.Empty;
        public bool IsMuteNotification { get; set; }
        public bool IsMuteEnabled { get; set; }
    }
    
    /// <summary>
    /// Message analysis features
    /// </summary>
    public class MessageFeatures
    {
        public bool IsBot { get; set; }
        public bool IsCustomMessage { get; set; }
        public bool IsMuteNotification { get; set; }
        public bool IsLotteryResult { get; set; }
        public CompetitorMessageType CompetitorType { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
        
        public string GetTagsString()
        {
            return Tags.Count > 0 ? $"[{string.Join(",", Tags)}]" : string.Empty;
        }
    }
    
    /// <summary>
    /// Lottery result info
    /// </summary>
    public class LotteryResultInfo
    {
        public string Period { get; set; }
        public int Number1 { get; set; }
        public int Number2 { get; set; }
        public int Number3 { get; set; }
        public int Result { get; set; }
        
        public override string ToString()
        {
            return $"{Period}期: {Number1}+{Number2}+{Number3}={Result}";
        }
    }
    
    /// <summary>
    /// Attack reply info
    /// </summary>
    public class AttackReplyInfo
    {
        public string PlayerName { get; set; }
        public string GameplayString { get; set; }
        public List<GameplayItem> GameplayList { get; set; } = new List<GameplayItem>();
        public decimal Amount { get; set; }
    }
    
    /// <summary>
    /// Competitor settlement entry (parsed from competitor bot messages)
    /// </summary>
    public class CompetitorSettlementEntry
    {
        public string PlayerName { get; set; }
        public decimal TotalBet { get; set; }
        public string GameplayString { get; set; }
        public List<GameplayItem> GameplayList { get; set; } = new List<GameplayItem>();
        public decimal Balance { get; set; }
    }
    
    /// <summary>
    /// Gameplay item (code + amount)
    /// </summary>
    public class GameplayItem
    {
        public string Code { get; set; }
        public string ChineseName { get; set; }
        public int Amount { get; set; }
        
        public override string ToString()
        {
            return $"{ChineseName}{Amount}";
        }
    }
}
