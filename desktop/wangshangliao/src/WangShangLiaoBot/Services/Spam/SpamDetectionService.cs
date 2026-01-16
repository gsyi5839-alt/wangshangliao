using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using WangShangLiaoBot.Models;
using WangShangLiaoBot.Models.Spam;

namespace WangShangLiaoBot.Services.Spam
{
    /// <summary>
    /// Spam detection service for group chats:
    /// - Length/line thresholds -> mute/kick
    /// - Image/emoji count -> mute/kick
    /// - Keyword rules -> mute/kick
    /// - Optional: auto add to blacklist when kicked by bot
    /// - Bill 0 score mute (账单0分除了上分，其他发言一律禁言)
    /// - Auto add to blacklist when kicked by admin (被群管理踢出自动加黑名单)
    /// </summary>
    public sealed class SpamDetectionService
    {
        private static SpamDetectionService _instance;
        public static SpamDetectionService Instance => _instance ?? (_instance = new SpamDetectionService());

        private SpamDetectionService() { }

        public bool IsRunning { get; private set; }

        // key: teamId|userId -> count
        private readonly Dictionary<string, int> _imageEmojiCount = new Dictionary<string, int>();
        // key: teamId|userId -> last action time (cooldown)
        private readonly Dictionary<string, DateTime> _lastAction = new Dictionary<string, DateTime>();
        
        // Track users kicked by bot (to distinguish from admin kicks)
        // key: account -> kick time
        private readonly Dictionary<string, DateTime> _botKickedUsers = new Dictionary<string, DateTime>();
        
        // Timer for checking admin-kicked members
        private Timer _adminKickCheckTimer;
        
        // Regex pattern for detecting up-score requests like "上100", "上分200", "上 50"
        private static readonly Regex UpScorePattern = new Regex(@"上(?:分)?[\s]*\d+", RegexOptions.Compiled);

        public void Start()
        {
            if (IsRunning) return;
            ChatService.Instance.OnMessageReceived += Handle;
            
            // Start timer to check admin-kicked members every 3 seconds
            _adminKickCheckTimer = new Timer(
                async _ => await CheckAdminKickedMembersAsync(),
                null,
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(3));
            
            IsRunning = true;
            Logger.Info("[SpamDetectionService] Started with admin-kick monitoring");
        }

        public void Stop()
        {
            if (!IsRunning) return;
            ChatService.Instance.OnMessageReceived -= Handle;
            
            // Stop and dispose timer
            if (_adminKickCheckTimer != null)
            {
                _adminKickCheckTimer.Dispose();
                _adminKickCheckTimer = null;
            }
            
            IsRunning = false;
            Logger.Info("[SpamDetectionService] Stopped");
        }
        
        /// <summary>
        /// Record that bot kicked a user (to distinguish from admin kicks)
        /// </summary>
        public void RecordBotKick(string account)
        {
            if (string.IsNullOrEmpty(account)) return;
            _botKickedUsers[account] = DateTime.Now;
            
            // Clean up old entries (older than 30 seconds)
            var cutoff = DateTime.Now.AddSeconds(-30);
            var keysToRemove = _botKickedUsers.Where(kv => kv.Value < cutoff).Select(kv => kv.Key).ToList();
            foreach (var key in keysToRemove)
            {
                _botKickedUsers.Remove(key);
            }
        }
        
        /// <summary>
        /// Check if a user was recently kicked by the bot
        /// </summary>
        private bool WasKickedByBot(string account)
        {
            if (string.IsNullOrEmpty(account)) return false;
            if (!_botKickedUsers.ContainsKey(account)) return false;
            
            // Consider it a bot kick if within last 30 seconds
            var kickTime = _botKickedUsers[account];
            return (DateTime.Now - kickTime).TotalSeconds < 30;
        }
        
        /// <summary>
        /// Check for admin-kicked members and auto add to blacklist
        /// 检查被群管理踢出的成员并自动加黑名单
        /// </summary>
        private async Task CheckAdminKickedMembersAsync()
        {
            if (!IsRunning) return;
            
            var settings = SpamSettingsService.Instance;
            if (!settings.AutoAddBlacklistOnAdminKick) return;
            
            try
            {
                var json = await ChatService.Instance.GetRemovedMembersAsync(clearAfterGet: true);
                if (string.IsNullOrEmpty(json) || json == "[]") return;
                
                // Parse removed members data
                // Format: [{"time":123,"teamId":"xxx","accounts":["acc1","acc2"],"removedBySelf":false}]
                var matches = Regex.Matches(json, @"""accounts""\s*:\s*\[(.*?)\]", RegexOptions.Singleline);
                
                foreach (Match m in matches)
                {
                    var accountsStr = m.Groups[1].Value;
                    var accountMatches = Regex.Matches(accountsStr, @"""([^""]+)""");
                    
                    foreach (Match am in accountMatches)
                    {
                        var account = am.Groups[1].Value;
                        if (string.IsNullOrEmpty(account)) continue;
                        
                        // Skip if this was kicked by the bot itself
                        if (WasKickedByBot(account))
                        {
                            Logger.Info("[Spam] Skipping self-kicked user: " + account);
                            continue;
                        }
                        
                        // Admin kicked - add to blacklist
                        var cfg = ConfigService.Instance.Config;
                        if (cfg != null)
                        {
                            if (cfg.Blacklist == null) cfg.Blacklist = new List<string>();
                            if (!cfg.Blacklist.Contains(account))
                            {
                                cfg.Blacklist.Add(account);
                                ConfigService.Instance.SaveConfig();
                                Logger.Info("[Spam] Admin-kicked user added to blacklist: " + account);
                                
                                // Also add to candidates for UI display
                                SpamSettingsService.Instance.AppendBlacklistCandidate(account);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("[Spam] CheckAdminKickedMembersAsync error: " + ex.Message);
            }
        }

        private void Handle(ChatMessage msg)
        {
            if (!IsRunning) return;
            if (msg == null) return;
            if (msg.IsSelf) return;
            if (!msg.IsGroupMessage) return;
            if (string.IsNullOrWhiteSpace(msg.GroupId)) return;
            if (string.IsNullOrWhiteSpace(msg.SenderId)) return;

            var config = ConfigService.Instance.Config;
            if (config != null && config.Blacklist != null && config.Blacklist.Contains(msg.SenderId))
            {
                // Already blacklisted: ignore further processing
                return;
            }

            // Cooldown to avoid repeated punishment for the same user within a short time
            var actionKey = msg.GroupId + "|" + msg.SenderId;
            if (_lastAction.TryGetValue(actionKey, out var last) && (DateTime.Now - last).TotalSeconds < 5)
                return;

            var settings = SpamSettingsService.Instance;

            // 1) Keyword rules
            if (msg.Type == MessageType.Text)
            {
                var text = msg.Content ?? "";
                var rules = settings.LoadKeywordRules();
                foreach (var r in rules)
                {
                    if (r == null) continue;
                    var k = (r.Keyword ?? "").Trim();
                    if (string.IsNullOrEmpty(k)) continue;
                    if (text.Contains(k))
                    {
                        _ = ExecuteActionAsync(msg, r.Action, reason: "Keyword:" + k);
                        return;
                    }
                }
            }

            // 2) Image/emoji punishment (only if enabled)
            if (settings.ImageEmojiEnabled && (msg.Type == MessageType.Image || msg.Type == MessageType.Emoji))
            {
                var countKey = actionKey;
                _imageEmojiCount[countKey] = (_imageEmojiCount.ContainsKey(countKey) ? _imageEmojiCount[countKey] : 0) + 1;
                if (_imageEmojiCount[countKey] > settings.ImageEmojiKickCount)
                    _ = ExecuteActionAsync(msg, SpamAction.Kick, reason: "Image/Emoji>=" + settings.ImageEmojiKickCount);
                else
                    _ = ExecuteActionAsync(msg, SpamAction.Mute, reason: "Image/Emoji");
                return;
            }

            if (msg.Type != MessageType.Text) return;

            // 3) Bill 0 score mute (账单0分除了上分，其他发言一律禁言)
            // Logic: If player's score is 0, mute all messages except up-score requests
            if (settings.BillAtMute)
            {
                var player = DataService.Instance.GetOrCreatePlayer(msg.SenderId, msg.SenderName);
                if (player != null && player.Score == 0)
                {
                    var msgContent = msg.Content ?? "";
                    var isUpScoreRequest = IsUpScoreRequest(msgContent);
                    
                    if (!isUpScoreRequest)
                    {
                        _ = ExecuteActionAsync(msg, SpamAction.Mute, reason: "账单0分");
                        Logger.Info("[Spam] 账单0分禁言: " + msg.SenderId + " (分数=0, 非上分请求)");
                        return;
                    }
                }
            }

            // 4) Length/line thresholds
            var content = msg.Content ?? "";
            var weightedLen = GetWeightedLength(content);
            var lines = CountLines(content);

            if (weightedLen > settings.MaxCharsKick)
            {
                _ = ExecuteActionAsync(msg, SpamAction.Kick, reason: "Chars>" + settings.MaxCharsKick);
                return;
            }

            if (weightedLen > settings.MaxCharsMute)
            {
                _ = ExecuteActionAsync(msg, SpamAction.Mute, reason: "Chars>" + settings.MaxCharsMute);
                return;
            }

            if (lines > settings.MaxLinesMute)
            {
                _ = ExecuteActionAsync(msg, SpamAction.Mute, reason: "Lines>" + settings.MaxLinesMute);
                return;
            }
        }

        /// <summary>
        /// Weighted length: one Chinese char=2, ASCII=1 (per screenshot rule).
        /// </summary>
        private int GetWeightedLength(string s)
        {
            if (string.IsNullOrEmpty(s)) return 0;
            var len = 0;
            foreach (var ch in s)
            {
                len += ch <= 127 ? 1 : 2;
            }
            return len;
        }

        private int CountLines(string s)
        {
            if (string.IsNullOrEmpty(s)) return 0;
            // Normalize newlines; treat any newline as line break
            var lines = s.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
            return lines.Length;
        }

        /// <summary>
        /// Check if a message is an up-score request (上分请求).
        /// Checks against configured keywords (UpScoreKeywords) and common patterns like "上100", "上分200".
        /// </summary>
        private bool IsUpScoreRequest(string content)
        {
            if (string.IsNullOrEmpty(content)) return false;

            // Check configured up-score keywords
            var config = ConfigService.Instance.Config;
            if (config != null && !string.IsNullOrEmpty(config.UpScoreKeywords))
            {
                var keywords = config.UpScoreKeywords.Split('|');
                foreach (var kw in keywords)
                {
                    var keyword = (kw ?? "").Trim();
                    if (!string.IsNullOrEmpty(keyword))
                    {
                        // Check if content starts with or contains the up-score pattern (keyword followed by number)
                        // e.g., "上100", "上 200", keyword + digits
                        if (Regex.IsMatch(content, Regex.Escape(keyword) + @"[\s]*\d+"))
                        {
                            return true;
                        }
                    }
                }
            }

            // Check common patterns: "上分100", "上100", "上 50" etc.
            if (UpScorePattern.IsMatch(content))
            {
                return true;
            }

            // Also check if content contains explicit "上分" followed by digits
            if (Regex.IsMatch(content, @"上分[\s]*\d+"))
            {
                return true;
            }

            return false;
        }

        private async Task ExecuteActionAsync(ChatMessage msg, SpamAction action, string reason)
        {
            try
            {
                var settings = SpamSettingsService.Instance;
                _lastAction[msg.GroupId + "|" + msg.SenderId] = DateTime.Now;

                // Auto recall the violating message if enabled
                if (settings.FollowBotAutoRecall && !string.IsNullOrEmpty(msg.IdClient))
                {
                    try
                    {
                        var recallResult = await ChatService.Instance.RecallMessageAsync(msg);
                        Logger.Info($"[Spam] Recall message {msg.IdClient} ok={recallResult.Success}");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"[Spam] Recall failed: {ex.Message}");
                    }
                }

                if (action == SpamAction.Mute)
                {
                    var minutes = Math.Max(1, settings.MuteMinutes);
                    var r = await ChatService.Instance.MuteTeamMemberAsync(msg.GroupId, msg.SenderId, true);

                    // Auto unmute after duration (best-effort)
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await Task.Delay(TimeSpan.FromMinutes(minutes));
                            await ChatService.Instance.MuteTeamMemberAsync(msg.GroupId, msg.SenderId, false);
                        }
                        catch { }
                    });

                    Logger.Info($"[Spam] Mute {msg.SenderId} team={msg.GroupId} reason={reason} ok={r.Success}");
                    return;
                }

                if (action == SpamAction.Kick)
                {
                    // Record that bot is kicking this user (to distinguish from admin kicks)
                    RecordBotKick(msg.SenderId);
                    
                    var r = await ChatService.Instance.KickTeamMemberAsync(msg.GroupId, msg.SenderId);
                    Logger.Info("[Spam] Kick " + msg.SenderId + " team=" + msg.GroupId + " reason=" + reason + " ok=" + r.Success);

                    // Add to candidates list for UI
                    SpamSettingsService.Instance.AppendBlacklistCandidate(msg.SenderId);

                    // Auto add blacklist when kicked by bot
                    if (r.Success && settings.AutoAddBlacklistOnBotKick)
                    {
                        var cfg = ConfigService.Instance.Config;
                        if (cfg != null)
                        {
                            if (cfg.Blacklist == null) cfg.Blacklist = new List<string>();
                            if (!cfg.Blacklist.Contains(msg.SenderId))
                            {
                                cfg.Blacklist.Add(msg.SenderId);
                                ConfigService.Instance.SaveConfig();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("[Spam] ExecuteAction failed: " + ex.Message);
            }
        }
    }
}


