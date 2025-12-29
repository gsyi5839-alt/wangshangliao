using System;
using System.Collections.Generic;
using System.Linq;

namespace WangShangLiaoBot.Services
{
    /// <summary>
    /// 艾特分原因服务 - 管理艾特加分的原因配置
    /// </summary>
    public sealed class AtReasonService
    {
        private static AtReasonService _instance;
        public static AtReasonService Instance => _instance ?? (_instance = new AtReasonService());

        private AtReasonService() { }

        private const string Prefix = "AtReason:";

        /// <summary>艾特加分原因列表（用|分隔）</summary>
        public string Reasons
        {
            get => GetString(Prefix + "Reasons", "红包|回水|其他|邀请");
            set => SetString(Prefix + "Reasons", value);
        }

        /// <summary>私聊加分群内提醒</summary>
        public bool PrivateChatNotifyInGroup
        {
            get => GetBool(Prefix + "PrivateChatNotifyInGroup", true);
            set => SetBool(Prefix + "PrivateChatNotifyInGroup", value);
        }

        /// <summary>私聊加分允许无理由</summary>
        public bool PrivateChatAllowNoReason
        {
            get => GetBool(Prefix + "PrivateChatAllowNoReason", true);
            set => SetBool(Prefix + "PrivateChatAllowNoReason", value);
        }

        /// <summary>艾特分统计为活动分的原因（用|分隔）</summary>
        public string ActivityReasons
        {
            get => GetString(Prefix + "ActivityReasons", "");
            set => SetString(Prefix + "ActivityReasons", value);
        }

        /// <summary>获取原因列表</summary>
        public List<string> GetReasonList()
        {
            var reasons = Reasons;
            if (string.IsNullOrWhiteSpace(reasons)) return new List<string>();
            return reasons.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();
        }

        /// <summary>获取活动分原因列表</summary>
        public List<string> GetActivityReasonList()
        {
            var reasons = ActivityReasons;
            if (string.IsNullOrWhiteSpace(reasons)) return new List<string>();
            return reasons.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();
        }

        /// <summary>检查原因是否有效</summary>
        public bool IsValidReason(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason)) return PrivateChatAllowNoReason;
            return GetReasonList().Any(r => 
                r.Equals(reason, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>检查是否为活动分原因</summary>
        public bool IsActivityReason(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason)) return false;
            return GetActivityReasonList().Any(r => 
                r.Equals(reason, StringComparison.OrdinalIgnoreCase));
        }

        // Setting helpers
        private bool GetBool(string key, bool defaultValue)
        {
            var s = DataService.Instance.GetSetting(key, defaultValue ? "1" : "0");
            return s == "1" || (bool.TryParse(s, out var b) && b);
        }

        private void SetBool(string key, bool value)
        {
            DataService.Instance.SaveSetting(key, value ? "1" : "0");
        }

        private string GetString(string key, string defaultValue)
        {
            return DataService.Instance.GetSetting(key, defaultValue);
        }

        private void SetString(string key, string value)
        {
            DataService.Instance.SaveSetting(key, value ?? "");
        }
    }
}

