using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using WangShangLiaoBot.Models.Spam;

namespace WangShangLiaoBot.Services.Spam
{
    /// <summary>
    /// Settings persistence for Blacklist/Spam detection tab.
    /// Uses DataService settings + flat files to avoid expanding AppConfig further.
    /// </summary>
    public sealed class SpamSettingsService
    {
        private static SpamSettingsService _instance;
        public static SpamSettingsService Instance => _instance ?? (_instance = new SpamSettingsService());

        private SpamSettingsService() { }

        private const string KeyAutoAddBotKick = "Spam:AutoAddBlacklist:BotKick";
        private const string KeyAutoAddAdminKick = "Spam:AutoAddBlacklist:AdminKick";
        private const string KeyMaxCharsMute = "Spam:MaxCharsMute";
        private const string KeyMaxCharsKick = "Spam:MaxCharsKick";
        private const string KeyMaxLinesMute = "Spam:MaxLinesMute";
        private const string KeyImageEmojiKickCount = "Spam:ImageEmojiKickCount";
        private const string KeyMuteMinutes = "Spam:MuteMinutes";
        private const string KeyBillAtMute = "Spam:BillAtMute";
        private const string KeyFollowBotAutoRecall = "Spam:FollowBotAutoRecall";
        private const string KeyImageEmojiEnabled = "Spam:ImageEmojiEnabled";

        private string KeywordFile => Path.Combine(DataService.Instance.DatabaseDir, "spam-keywords.txt");
        private string CandidateFile => Path.Combine(DataService.Instance.DatabaseDir, "spam-blacklist-candidates.txt");

        public bool AutoAddBlacklistOnBotKick
        {
            get => GetBool(KeyAutoAddBotKick, defaultValue: true);
            set => SetBool(KeyAutoAddBotKick, value);
        }

        public bool AutoAddBlacklistOnAdminKick
        {
            get => GetBool(KeyAutoAddAdminKick, defaultValue: false);
            set => SetBool(KeyAutoAddAdminKick, value);
        }

        public int MaxCharsMute
        {
            get => GetInt(KeyMaxCharsMute, 60);
            set => SetInt(KeyMaxCharsMute, value);
        }

        public int MaxCharsKick
        {
            get => GetInt(KeyMaxCharsKick, 80);
            set => SetInt(KeyMaxCharsKick, value);
        }

        public int MaxLinesMute
        {
            get => GetInt(KeyMaxLinesMute, 4);
            set => SetInt(KeyMaxLinesMute, value);
        }

        public int ImageEmojiKickCount
        {
            get => GetInt(KeyImageEmojiKickCount, 6);
            set => SetInt(KeyImageEmojiKickCount, value);
        }

        public int MuteMinutes
        {
            get => GetInt(KeyMuteMinutes, 10);
            set => SetInt(KeyMuteMinutes, value);
        }

        public bool BillAtMute
        {
            get => GetBool(KeyBillAtMute, false);
            set => SetBool(KeyBillAtMute, value);
        }

        public bool FollowBotAutoRecall
        {
            get => GetBool(KeyFollowBotAutoRecall, false);
            set => SetBool(KeyFollowBotAutoRecall, value);
        }

        public bool ImageEmojiEnabled
        {
            get => GetBool(KeyImageEmojiEnabled, true);
            set => SetBool(KeyImageEmojiEnabled, value);
        }

        /// <summary>
        /// Load keyword rules from file.
        /// File format: keyword[TAB]Mute|Kick
        /// </summary>
        public List<SpamKeywordRule> LoadKeywordRules()
        {
            var list = new List<SpamKeywordRule>();
            try
            {
                if (!File.Exists(KeywordFile)) return list;
                var lines = File.ReadAllLines(KeywordFile, Encoding.UTF8);
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var parts = line.Split('\t');
                    if (parts.Length < 2) continue;
                    var keyword = parts[0].Trim();
                    var actionStr = parts[1].Trim();
                    if (string.IsNullOrEmpty(keyword)) continue;
                    var action = string.Equals(actionStr, "Kick", StringComparison.OrdinalIgnoreCase) ? SpamAction.Kick : SpamAction.Mute;
                    list.Add(new SpamKeywordRule { Keyword = keyword, Action = action });
                }
            }
            catch { }
            return list;
        }

        public void SaveKeywordRules(List<SpamKeywordRule> rules)
        {
            try
            {
                Directory.CreateDirectory(DataService.Instance.DatabaseDir);
                var sb = new StringBuilder();
                foreach (var r in rules ?? new List<SpamKeywordRule>())
                {
                    if (r == null) continue;
                    var k = (r.Keyword ?? "").Trim();
                    if (string.IsNullOrEmpty(k)) continue;
                    var a = r.Action == SpamAction.Kick ? "Kick" : "Mute";
                    sb.AppendLine(k + "\t" + a);
                }
                File.WriteAllText(KeywordFile, sb.ToString(), Encoding.UTF8);
            }
            catch { }
        }

        /// <summary>
        /// Candidate list shown in UI for manual adding to blacklist.
        /// </summary>
        public List<string> LoadBlacklistCandidates(int max = 200)
        {
            try
            {
                if (!File.Exists(CandidateFile)) return new List<string>();
                var lines = File.ReadAllLines(CandidateFile, Encoding.UTF8)
                    .Select(l => (l ?? "").Trim())
                    .Where(l => !string.IsNullOrEmpty(l))
                    .Distinct()
                    .ToList();
                if (lines.Count > max) lines = lines.Skip(lines.Count - max).ToList();
                return lines;
            }
            catch
            {
                return new List<string>();
            }
        }

        public void AppendBlacklistCandidate(string userId)
        {
            try
            {
                userId = (userId ?? "").Trim();
                if (string.IsNullOrEmpty(userId)) return;
                Directory.CreateDirectory(DataService.Instance.DatabaseDir);
                File.AppendAllText(CandidateFile, userId + Environment.NewLine, Encoding.UTF8);
            }
            catch { }
        }

        public void ClearBlacklistCandidates()
        {
            try
            {
                if (File.Exists(CandidateFile))
                    File.Delete(CandidateFile);
            }
            catch { }
        }

        private bool GetBool(string key, bool defaultValue)
        {
            var s = DataService.Instance.GetSetting(key, defaultValue ? "1" : "0");
            if (string.Equals(s, "1")) return true;
            if (string.Equals(s, "0")) return false;
            if (bool.TryParse(s, out var b)) return b;
            return defaultValue;
        }

        private void SetBool(string key, bool value)
        {
            DataService.Instance.SaveSetting(key, value ? "1" : "0");
        }

        private int GetInt(string key, int defaultValue)
        {
            var s = DataService.Instance.GetSetting(key, defaultValue.ToString(CultureInfo.InvariantCulture));
            if (int.TryParse(s, out var v)) return v;
            return defaultValue;
        }

        private void SetInt(string key, int value)
        {
            DataService.Instance.SaveSetting(key, value.ToString(CultureInfo.InvariantCulture));
        }
    }
}


