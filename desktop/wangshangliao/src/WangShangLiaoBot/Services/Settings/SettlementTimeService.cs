using System;

namespace WangShangLiaoBot.Services
{
    /// <summary>
    /// 结算时间服务 - 管理每日结算时间和查询间隔
    /// </summary>
    public sealed class SettlementTimeService
    {
        private static SettlementTimeService _instance;
        public static SettlementTimeService Instance => 
            _instance ?? (_instance = new SettlementTimeService());

        private SettlementTimeService() { }

        private const string Prefix = "Settlement:";

        /// <summary>结算时间（时:分:秒）</summary>
        public TimeSpan SettlementTime
        {
            get
            {
                var s = GetString(Prefix + "Time", "20:00:00");
                if (TimeSpan.TryParse(s, out var ts))
                    return ts;
                return new TimeSpan(20, 0, 0);
            }
            set => SetString(Prefix + "Time", value.ToString(@"hh\:mm\:ss"));
        }

        /// <summary>玩家查询今天数据是否启用</summary>
        public bool PlayerQueryEnabled
        {
            get => GetBool(Prefix + "PlayerQueryEnabled", true);
            set => SetBool(Prefix + "PlayerQueryEnabled", value);
        }

        /// <summary>玩家查询间隔（分钟）</summary>
        public int PlayerQueryIntervalMinutes
        {
            get => GetInt(Prefix + "PlayerQueryInterval", 10);
            set => SetInt(Prefix + "PlayerQueryInterval", value);
        }

        /// <summary>检查当前时间是否已过结算时间</summary>
        public bool IsPastSettlementTime()
        {
            return DateTime.Now.TimeOfDay >= SettlementTime;
        }

        /// <summary>获取今天的结算时间点</summary>
        public DateTime GetTodaySettlementDateTime()
        {
            return DateTime.Today.Add(SettlementTime);
        }

        /// <summary>检查是否应该查询今天的数据（基于结算时间）</summary>
        public bool ShouldQueryTodayData()
        {
            // If past settlement time, query today's data
            // Otherwise query yesterday's data
            return IsPastSettlementTime();
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

        private int GetInt(string key, int defaultValue)
        {
            var s = DataService.Instance.GetSetting(key, defaultValue.ToString());
            return int.TryParse(s, out var v) ? v : defaultValue;
        }

        private void SetInt(string key, int value)
        {
            DataService.Instance.SaveSetting(key, value.ToString());
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

