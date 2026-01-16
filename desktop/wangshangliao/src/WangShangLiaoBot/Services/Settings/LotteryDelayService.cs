using System;

namespace WangShangLiaoBot.Services
{
    /// <summary>
    /// 开奖延迟服务 - 管理各彩种开奖延迟和时间自动调整
    /// </summary>
    public sealed class LotteryDelayService
    {
        private static LotteryDelayService _instance;
        public static LotteryDelayService Instance => 
            _instance ?? (_instance = new LotteryDelayService());

        private LotteryDelayService() { }

        private const string Prefix = "LotteryDelay:";

        /// <summary>延迟开关</summary>
        public bool DelayEnabled
        {
            get => GetBool(Prefix + "DelayEnabled", false);
            set => SetBool(Prefix + "DelayEnabled", value);
        }

        /// <summary>延迟模式：true=倒计时, false=开奖</summary>
        public bool IsCountdownMode
        {
            get => GetBool(Prefix + "IsCountdownMode", true);
            set => SetBool(Prefix + "IsCountdownMode", value);
        }

        /// <summary>PC蛋蛋延迟秒数</summary>
        public int PcEggDelay
        {
            get => GetInt(Prefix + "PcEggDelay", 0);
            set => SetInt(Prefix + "PcEggDelay", value);
        }

        /// <summary>加拿大延迟秒数</summary>
        public int CanadaDelay
        {
            get => GetInt(Prefix + "CanadaDelay", 0);
            set => SetInt(Prefix + "CanadaDelay", value);
        }

        /// <summary>比特延迟秒数</summary>
        public int BitDelay
        {
            get => GetInt(Prefix + "BitDelay", 0);
            set => SetInt(Prefix + "BitDelay", value);
        }

        /// <summary>北京延迟秒数</summary>
        public int BeijingDelay
        {
            get => GetInt(Prefix + "BeijingDelay", 0);
            set => SetInt(Prefix + "BeijingDelay", value);
        }

        /// <summary>自动调整时间开关</summary>
        public bool AutoAdjustTimeEnabled
        {
            get => GetBool(Prefix + "AutoAdjustTimeEnabled", false);
            set => SetBool(Prefix + "AutoAdjustTimeEnabled", value);
        }

        /// <summary>自动调整时间间隔（小时）</summary>
        public int AutoAdjustIntervalHours
        {
            get => GetInt(Prefix + "AutoAdjustIntervalHours", 12);
            set => SetInt(Prefix + "AutoAdjustIntervalHours", value);
        }

        /// <summary>获取指定彩种的延迟秒数</summary>
        public int GetDelay(string lotteryType)
        {
            if (!DelayEnabled) return 0;
            
            switch (lotteryType?.ToLower())
            {
                case "pcegg":
                case "pc蛋蛋":
                    return PcEggDelay;
                case "canada":
                case "加拿大":
                    return CanadaDelay;
                case "bit":
                case "比特":
                    return BitDelay;
                case "beijing":
                case "北京":
                    return BeijingDelay;
                default:
                    return 0;
            }
        }

        /// <summary>计算延迟后的时间</summary>
        public DateTime ApplyDelay(DateTime originalTime, string lotteryType)
        {
            var delay = GetDelay(lotteryType);
            if (delay == 0) return originalTime;
            
            if (IsCountdownMode)
            {
                // Countdown mode: subtract delay from countdown
                return originalTime.AddSeconds(-delay);
            }
            else
            {
                // Lottery mode: add delay to lottery time
                return originalTime.AddSeconds(delay);
            }
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
    }
}

