using System;
using System.Collections.Generic;
using System.Linq;
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

        public void Start()
        {
            if (IsRunning) return;
            ChatService.Instance.OnMessageReceived += Handle;
            IsRunning = true;
        }

        public void Stop()
        {
            if (!IsRunning) return;
            ChatService.Instance.OnMessageReceived -= Handle;
            IsRunning = false;
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

            // 3) Bill @ score mute (best-effort)
            // The screenshot indicates: "账单@分除了上分，其他发言一律禁言"
            // We implement: if text contains "@分" and doesn't contain "上分", then mute.
            if (settings.BillAtMute)
            {
                var t = msg.Content ?? "";
                if (t.Contains("@分") && !t.Contains("上分"))
                {
                    _ = ExecuteActionAsync(msg, SpamAction.Mute, reason: "@分");
                    return;
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
                    var r = await ChatService.Instance.KickTeamMemberAsync(msg.GroupId, msg.SenderId);
                    Logger.Info($"[Spam] Kick {msg.SenderId} team={msg.GroupId} reason={reason} ok={r.Success}");

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


