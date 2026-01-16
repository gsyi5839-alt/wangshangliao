using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WangShangLiaoBot.Models;

namespace WangShangLiaoBot.Services
{
    /// <summary>
    /// 自动回复服务 - 处理自动回复、关键词回复等
    /// </summary>
    public class AutoReplyService
    {
        private static AutoReplyService _instance;
        private static readonly object _lock = new object();
        
        /// <summary>单例实例</summary>
        public static AutoReplyService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                            _instance = new AutoReplyService();
                    }
                }
                return _instance;
            }
        }
        
        private CancellationTokenSource _cts;
        private bool _isRunning;
        
        /// <summary>是否正在运行</summary>
        public bool IsRunning => _isRunning;
        
        /// <summary>处理的消息计数</summary>
        public int ProcessedCount { get; private set; }
        
        /// <summary>状态变更事件</summary>
        public event Action<bool> OnStatusChanged;
        
        /// <summary>日志事件</summary>
        public event Action<string> OnLog;
        
        private AutoReplyService() { }
        
        /// <summary>
        /// 启动自动回复服务
        /// </summary>
        public void Start()
        {
            if (_isRunning) return;
            
            _cts = new CancellationTokenSource();
            _isRunning = true;
            ProcessedCount = 0;
            
            // 订阅消息事件
            ChatService.Instance.OnMessageReceived += HandleMessage;
            
            OnStatusChanged?.Invoke(true);
            Log("自动回复服务已启动");
        }
        
        /// <summary>
        /// 停止自动回复服务
        /// </summary>
        public void Stop()
        {
            if (!_isRunning) return;
            
            _cts?.Cancel();
            _isRunning = false;
            
            // 取消订阅
            ChatService.Instance.OnMessageReceived -= HandleMessage;
            
            OnStatusChanged?.Invoke(false);
            Log("自动回复服务已停止");
        }
        
        /// <summary>
        /// 处理接收到的消息
        /// </summary>
        private async void HandleMessage(ChatMessage message)
        {
            if (!_isRunning || message.IsProcessed) return;
            
            // Skip self messages to prevent reply loops
            if (message.IsSelf) return;
            
            try
            {
                var config = ConfigService.Instance.Config;
                
                // Check blacklist (with null safety)
                if (config.Blacklist != null && !string.IsNullOrEmpty(message.SenderId) 
                    && config.Blacklist.Contains(message.SenderId))
                {
                    Log($"忽略黑名单用户消息: {message.SenderId}");
                    return;
                }
                
                // ===== 艾特变昵称: Auto-update player nickname from message =====
                if (!string.IsNullOrEmpty(message.SenderId) && !string.IsNullOrEmpty(message.SenderName))
                {
                    // Check if feature is enabled via MainForm.EnableAtNicknameUpdate
                    var enableNickUpdate = Forms.MainForm.EnableAtNicknameUpdate;
                    if (enableNickUpdate)
                    {
                        var updated = DataService.Instance.UpdateNicknameIfChanged(
                            message.SenderId, message.SenderName, enableNickUpdate);
                        if (updated)
                        {
                            Log($"艾特变昵称: {message.SenderId} -> {message.SenderName}");
                        }
                    }
                }
                
                // ===== Process admin commands first =====
                var cmdResult = await AdminCommandService.Instance.ProcessCommandAsync(message);
                if (cmdResult.Handled)
                {
                    if (!string.IsNullOrEmpty(cmdResult.Reply))
                    {
                        // Send command response
                        string scene = message.IsGroupMessage ? "team" : "p2p";
                        string to = message.IsGroupMessage ? message.GroupId : message.SenderId;
                        
                        if (!string.IsNullOrEmpty(to))
                        {
                            var result = await ChatService.Instance.SendTextAsync(scene, to, cmdResult.Reply);
                            if (result.Success)
                            {
                                ProcessedCount++;
                                var replyPreview = cmdResult.Reply.Length > 50 
                                    ? cmdResult.Reply.Substring(0, 50) + "..." 
                                    : cmdResult.Reply.Replace("\n", " ");
                                Log($"命令回复 [{message.SenderName}]: {replyPreview}");
                            }
                        }
                    }
                    return; // Command handled, skip normal reply
                }
                
                // If bot is not running (via 关机 command), skip further processing
                if (!AdminCommandService.Instance.IsBotRunning) return;
                
                // ===== Check for score up/down keywords and add to pending requests =====
                CheckScoreKeywords(message, config);
                
                string reply = null;
                
                // 1. Check custom keyword rules first
                reply = CheckKeywordReply(message.Content, config.KeywordRules);
                
                // 2. Check preset templates (CaiFuTong, ZhiFuBao, WeiXin)
                if (string.IsNullOrEmpty(reply))
                {
                    reply = CheckPresetTemplateReply(message.Content, config);
                }
                
                // 3. Check internal reply settings (个人数据反馈, 进群/群规 etc.)
                if (string.IsNullOrEmpty(reply))
                {
                    var player = DataService.Instance.GetOrCreatePlayer(message.SenderId, message.SenderName);
                    reply = CheckInternalReply(message.Content, config, player);
                }
                
                // 4. Fall back to default auto-reply if no keyword matched
                if (string.IsNullOrEmpty(reply) && config.EnableAutoReply)
                {
                    reply = config.AutoReplyContent;
                }
                
                // 3. Send reply via NIM SDK (more stable than DOM manipulation)
                if (!string.IsNullOrEmpty(reply))
                {
                    // Render via unified template engine
                    var player = DataService.Instance.GetOrCreatePlayer(message.SenderId, message.SenderName);
                    reply = TemplateEngine.Render(reply, new TemplateEngine.RenderContext
                    {
                        Message = message,
                        Player = player,
                        Today = DateTime.Today
                    });
                    
                    // Use NIM SDK SendTextAsync for reliable message delivery
                    // - Group message: reply to group (scene="team", to=groupId)
                    // - Private message: reply to sender (scene="p2p", to=senderId)
                    bool success = false;
                    string scene = message.IsGroupMessage ? "team" : "p2p";
                    string to = message.IsGroupMessage ? message.GroupId : message.SenderId;
                    
                    if (!string.IsNullOrEmpty(to))
                    {
                        var result = await ChatService.Instance.SendTextAsync(scene, to, reply);
                        success = result.Success;
                        
                        if (!success)
                        {
                            Log($"NIM SDK发送失败({result.Message})，尝试DOM方式...");
                            // Fallback to DOM manipulation if NIM SDK fails
                            success = await ChatService.Instance.SendMessageAsync(reply);
                        }
                    }
                    else
                    {
                        // No target info, fall back to current session
                        var result = await ChatService.Instance.SendTextToCurrentSessionAsync(reply);
                        success = result.Success;
                        
                        if (!success)
                        {
                            Log($"当前会话发送失败({result.Message})，尝试DOM方式...");
                            success = await ChatService.Instance.SendMessageAsync(reply);
                        }
                    }
                    
                    if (success)
                    {
                        ProcessedCount++;
                        Log($"自动回复 [{message.SenderName}] ({scene}:{to}): {reply}");
                    }
                    else
                    {
                        Log($"自动回复发送失败 [{message.SenderName}]: {reply}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"处理消息异常: {ex.Message}");
            }
            finally
            {
                // Always mark as processed to prevent duplicate processing
                message.IsProcessed = true;
            }
        }
        
        /// <summary>
        /// Check for score up/down keywords and add to pending requests queue
        /// Detects messages like "C30", "查30", "回200" and adds them to ScoreForm
        /// </summary>
        private void CheckScoreKeywords(ChatMessage message, AppConfig config)
        {
            if (string.IsNullOrEmpty(message?.Content) || string.IsNullOrEmpty(message.SenderId))
                return;
            
            var content = message.Content.Trim();
            var contentLower = content.ToLowerInvariant();
            
            // Get keywords from config (default: 上分="查|c|。", 下分="回")
            var upKeywords = (config.UpScoreKeywords ?? "查|c|。").Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            var downKeywords = (config.DownScoreKeywords ?? "回").Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            
            // Pattern: [关键字][数字] or [数字][关键字] - e.g. "C30", "查30", "30查", "回200"
            
            foreach (var keyword in upKeywords)
            {
                if (string.IsNullOrWhiteSpace(keyword)) continue;
                var kw = keyword.Trim();
                var kwLower = kw.ToLowerInvariant();
                
                // Case-insensitive match (C and c both match)
                if (contentLower.Contains(kwLower))
                {
                    // Try to extract amount from message
                    var amount = ExtractAmount(content, kw);
                    var player = DataService.Instance.GetOrCreatePlayer(message.SenderId, message.SenderName);
                    
                    // Add to pending score up requests - pass original content as reason for display
                    AdminCommandService.Instance.AddPendingRequest(
                        message.SenderId,
                        amount,
                        content, // Pass original message content for "喊话内容" display
                        "上分"
                    );
                    
                    Log($"检测到上分请求: {message.SenderName}({message.SenderId}) 金额:{amount} 内容:{content}");
                    return; // Only add once per message
                }
            }
            
            foreach (var keyword in downKeywords)
            {
                if (string.IsNullOrWhiteSpace(keyword)) continue;
                var kw = keyword.Trim();
                var kwLower = kw.ToLowerInvariant();
                
                // Case-insensitive match
                if (contentLower.Contains(kwLower))
                {
                    // Try to extract amount from message
                    var amount = ExtractAmount(content, kw);
                    var player = DataService.Instance.GetOrCreatePlayer(message.SenderId, message.SenderName);
                    
                    // Check minimum bet count requirement (下分最少次数: 上分后X把起才可下分)
                    var minBetCount = config.MinRoundsBeforeDownScore;
                    if (minBetCount > 0 && player.BetCount < minBetCount)
                    {
                        // Player hasn't met minimum bet count requirement
                        var replyTemplate = config.InternalUpDownMin;
                        if (!string.IsNullOrEmpty(replyTemplate))
                        {
                            _ = SendDownScoreReplyAsync(message, player, replyTemplate);
                            Log($"下分次数不足: {message.SenderName}({message.SenderId}) 下注次数:{player.BetCount} 最少要求:{minBetCount}");
                        }
                        return;
                    }
                    
                    // Check single withdrawal rule (X分以下下分只能一次回)
                    // If player's score is below minAmount, they can only withdraw everything at once
                    var minScoreForSingleDown = config.MinScoreForSingleDown;
                    if (minScoreForSingleDown > 0 && player.Score > 0 && player.Score < minScoreForSingleDown)
                    {
                        // Player's balance is below threshold, must withdraw all at once
                        if (amount > 0 && amount < player.Score)
                        {
                            // Player is trying to withdraw less than full balance - not allowed
                            var replyTemplate = config.InternalUpDownMin2;
                            if (!string.IsNullOrEmpty(replyTemplate))
                            {
                                _ = SendDownScoreReplyAsync(message, player, replyTemplate);
                                Log($"余额{player.Score}低于{minScoreForSingleDown}，只能一次性全下: {message.SenderName}({message.SenderId})");
                            }
                            return;
                        }
                    }
                    
                    // Add to pending score down requests - pass original content as reason for display
                    AdminCommandService.Instance.AddPendingRequest(
                        message.SenderId,
                        amount,
                        content, // Pass original message content for "喊话内容" display
                        "下分"
                    );
                    
                    // Send confirmation reply if configured (下分一次回)
                    if (!string.IsNullOrEmpty(config.InternalUpDownMax))
                    {
                        _ = SendDownScoreReplyAsync(message, player, config.InternalUpDownMax);
                    }
                    
                    Log($"检测到下分请求: {message.SenderName}({message.SenderId}) 金额:{amount} 内容:{content}");
                    return; // Only add once per message
                }
            }
        }
        
        /// <summary>
        /// Send down score related reply asynchronously
        /// </summary>
        private async Task SendDownScoreReplyAsync(ChatMessage message, Player player, string template)
        {
            try
            {
                var reply = TemplateEngine.Render(template, new TemplateEngine.RenderContext
                {
                    Message = message,
                    Player = player,
                    Today = DateTime.Today
                });
                
                if (!string.IsNullOrEmpty(reply))
                {
                    string scene = message.IsGroupMessage ? "team" : "p2p";
                    string to = message.IsGroupMessage ? message.GroupId : message.SenderId;
                    
                    if (!string.IsNullOrEmpty(to))
                    {
                        await ChatService.Instance.SendTextAsync(scene, to, reply);
                        Log($"下分回复 [{message.SenderName}]: {reply}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"发送下分回复失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Extract numeric amount from content around keyword
        /// E.g. "100查" -> 100, "回200" -> 200, "查" -> 0
        /// </summary>
        private decimal ExtractAmount(string content, string keyword)
        {
            try
            {
                // Remove keyword and extract numbers
                var withoutKeyword = content.Replace(keyword, " ");
                var match = System.Text.RegularExpressions.Regex.Match(withoutKeyword, @"\d+");
                if (match.Success && decimal.TryParse(match.Value, out var amount))
                {
                    return amount;
                }
            }
            catch { }
            return 0m;
        }
        
        /// <summary>
        /// 检查关键词回复
        /// </summary>
        private string CheckKeywordReply(string content, List<KeywordReplyRule> rules)
        {
            if (string.IsNullOrEmpty(content) || rules == null) return null;
            
            foreach (var rule in rules.Where(r => r.Enabled))
            {
                // 支持多关键词，用|分隔
                var keywords = rule.Keyword.Split('|');
                foreach (var keyword in keywords)
                {
                    if (content.Contains(keyword.Trim()))
                    {
                        return rule.Reply;
                    }
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// Check internal reply settings (个人数据反馈, 进群/群规 etc.)
        /// </summary>
        private string CheckInternalReply(string content, AppConfig config, Player player)
        {
            if (string.IsNullOrEmpty(content)) return null;
            
            // Check personal data feedback keywords (个人数据反馈)
            if (!string.IsNullOrEmpty(config.InternalDataKeyword))
            {
                var keywords = config.InternalDataKeyword.Split('|');
                foreach (var keyword in keywords)
                {
                    if (!string.IsNullOrEmpty(keyword) && content.Contains(keyword.Trim()))
                    {
                        // Determine which reply to use based on player state
                        string reply = null;
                        decimal score = player?.Score ?? 0;
                        bool hasAttack = !string.IsNullOrEmpty(player?.Remark);
                        
                        if (score == 0)
                        {
                            // 账单0分 - player has no score
                            reply = config.InternalDataBill;
                        }
                        else if (!hasAttack)
                        {
                            // 有分无攻击 - player has score but no attack
                            reply = config.InternalDataNoAttack;
                        }
                        else
                        {
                            // 有分有攻击 - player has both score and attack
                            reply = config.InternalDataHasAttack;
                        }
                        
                        if (!string.IsNullOrEmpty(reply))
                        {
                            Log($"匹配个人数据反馈关键词: {keyword} (分数:{score}, 攻击:{hasAttack})");
                            return reply;
                        }
                    }
                }
            }
            
            // Check group rules keywords (进群/群规)
            // Keywords are configurable via InternalGroupRulesKeyword (separated by |)
            if (!string.IsNullOrEmpty(config.InternalGroupRules) && !string.IsNullOrEmpty(config.InternalGroupRulesKeyword))
            {
                var groupRulesKeywords = config.InternalGroupRulesKeyword.Split('|');
                foreach (var keyword in groupRulesKeywords)
                {
                    if (!string.IsNullOrEmpty(keyword) && content.Contains(keyword.Trim()))
                    {
                        Log($"匹配进群/群规关键词: {keyword}");
                        return config.InternalGroupRules;
                    }
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// Check preset template replies (CaiFuTong, ZhiFuBao, WeiXin)
        /// These templates have keyword lists and corresponding reply content
        /// </summary>
        private string CheckPresetTemplateReply(string content, AppConfig config)
        {
            if (string.IsNullOrEmpty(content)) return null;
            
            // Check CaiFuTong template keywords
            if (!string.IsNullOrEmpty(config.CftReplyKeywords))
            {
                var keywords = config.CftReplyKeywords.Split('|');
                foreach (var keyword in keywords)
                {
                    if (!string.IsNullOrEmpty(keyword) && content.Contains(keyword.Trim()))
                    {
                        // Return CftText if set, otherwise CftSendText
                        var reply = !string.IsNullOrEmpty(config.CftText) ? config.CftText : config.CftSendText;
                        if (!string.IsNullOrEmpty(reply))
                        {
                            Log($"匹配财付通关键词: {keyword}");
                            return reply;
                        }
                    }
                }
            }
            
            // Check ZhiFuBao template keywords
            if (!string.IsNullOrEmpty(config.ZfbReplyKeywords))
            {
                var keywords = config.ZfbReplyKeywords.Split('|');
                foreach (var keyword in keywords)
                {
                    if (!string.IsNullOrEmpty(keyword) && content.Contains(keyword.Trim()))
                    {
                        var reply = !string.IsNullOrEmpty(config.ZfbText) ? config.ZfbText : config.ZfbSendText;
                        if (!string.IsNullOrEmpty(reply))
                        {
                            Log($"匹配支付宝关键词: {keyword}");
                            return reply;
                        }
                    }
                }
            }
            
            // Check WeiXin template keywords
            if (!string.IsNullOrEmpty(config.WxReplyKeywords))
            {
                var keywords = config.WxReplyKeywords.Split('|');
                foreach (var keyword in keywords)
                {
                    if (!string.IsNullOrEmpty(keyword) && content.Contains(keyword.Trim()))
                    {
                        var reply = !string.IsNullOrEmpty(config.WxText) ? config.WxText : config.WxSendText;
                        if (!string.IsNullOrEmpty(reply))
                        {
                            Log($"匹配微信关键词: {keyword}");
                            return reply;
                        }
                    }
                }
            }
            
            return null;
        }
        
        // NOTE: Variable replacement is now unified in TemplateEngine.
        
        /// <summary>
        /// 添加关键词规则
        /// </summary>
        public void AddKeywordRule(string keyword, string reply)
        {
            var config = ConfigService.Instance.Config;
            config.KeywordRules.Add(new KeywordReplyRule
            {
                Keyword = keyword,
                Reply = reply,
                Enabled = true
            });
            ConfigService.Instance.SaveConfig();
            Log($"添加关键词规则: {keyword} -> {reply}");
        }
        
        /// <summary>
        /// 删除关键词规则
        /// </summary>
        public void RemoveKeywordRule(string keyword)
        {
            var config = ConfigService.Instance.Config;
            var rule = config.KeywordRules.FirstOrDefault(r => r.Keyword == keyword);
            if (rule != null)
            {
                config.KeywordRules.Remove(rule);
                ConfigService.Instance.SaveConfig();
                Log($"删除关键词规则: {keyword}");
            }
        }
        
        private void Log(string message)
        {
            Logger.Info($"[AutoReply] {message}");
            OnLog?.Invoke(message);
        }
    }
}

