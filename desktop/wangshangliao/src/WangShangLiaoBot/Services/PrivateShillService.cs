using System;
using System.Collections.Generic;
using System.Linq;

namespace WangShangLiaoBot.Services
{
    /// <summary>
    /// 私聊版托服务 - 管理私聊托号自动下注
    /// </summary>
    public sealed class PrivateShillService
    {
        private static PrivateShillService _instance;
        public static PrivateShillService Instance => _instance ?? (_instance = new PrivateShillService());

        private PrivateShillService() { }

        private const string Prefix = "PrivateShill:";

        /// <summary>私聊版托开关</summary>
        public bool Enabled
        {
            get => GetBool(Prefix + "Enabled", false);
            set => SetBool(Prefix + "Enabled", value);
        }

        /// <summary>开奖后N秒内不下注</summary>
        public int AfterLotteryDelay
        {
            get => GetInt(Prefix + "AfterLotteryDelay", 20);
            set => SetInt(Prefix + "AfterLotteryDelay", value);
        }

        /// <summary>封盘前停止下注时间</summary>
        public string BeforeSealTime
        {
            get => GetString(Prefix + "BeforeSealTime", "wan20");
            set => SetString(Prefix + "BeforeSealTime", value);
        }

        /// <summary>托号列表文本（一行一个）</summary>
        public string ShillListText
        {
            get => GetString(Prefix + "ShillListText", "");
            set => SetString(Prefix + "ShillListText", value);
        }

        // Bet Range 1: 50-400
        public int BetRange1Min { get => GetInt(Prefix + "BetRange1Min", 50); set => SetInt(Prefix + "BetRange1Min", value); }
        public int BetRange1Max { get => GetInt(Prefix + "BetRange1Max", 400); set => SetInt(Prefix + "BetRange1Max", value); }
        public string BetRange1Bets { get => GetString(Prefix + "BetRange1Bets", "68x62xs|35xd65ds|da75dd25|53ds32dd|xd45|d"); set => SetString(Prefix + "BetRange1Bets", value); }

        // Bet Range 2: 401-800
        public int BetRange2Min { get => GetInt(Prefix + "BetRange2Min", 401); set => SetInt(Prefix + "BetRange2Min", value); }
        public int BetRange2Max { get => GetInt(Prefix + "BetRange2Max", 800); set => SetInt(Prefix + "BetRange2Max", value); }
        public string BetRange2Bets { get => GetString(Prefix + "BetRange2Bets", "30s110da|sz30dd91|85a95xs|76da58xs|d38dd5"); set => SetString(Prefix + "BetRange2Bets", value); }

        // Bet Range 3: 801-1300
        public int BetRange3Min { get => GetInt(Prefix + "BetRange3Min", 801); set => SetInt(Prefix + "BetRange3Min", value); }
        public int BetRange3Max { get => GetInt(Prefix + "BetRange3Max", 1300); set => SetInt(Prefix + "BetRange3Max", value); }
        public string BetRange3Bets { get => GetString(Prefix + "BetRange3Bets", "165xd85ds90da|dd138x155|169xd188ds|192ds1"); set => SetString(Prefix + "BetRange3Bets", value); }

        // Bet Range 4: 1301-10000
        public int BetRange4Min { get => GetInt(Prefix + "BetRange4Min", 1301); set => SetInt(Prefix + "BetRange4Min", value); }
        public int BetRange4Max { get => GetInt(Prefix + "BetRange4Max", 10000); set => SetInt(Prefix + "BetRange4Max", value); }
        public string BetRange4Bets { get => GetString(Prefix + "BetRange4Bets", "xs190da270|200da230xd|ds198x280|ds250xd28"); set => SetString(Prefix + "BetRange4Bets", value); }

        // Score settings
        public int ScoreLowThreshold { get => GetInt(Prefix + "ScoreLowThreshold", 100); set => SetInt(Prefix + "ScoreLowThreshold", value); }
        public string ScoreLowMessages { get => GetString(Prefix + "ScoreLowMessages", "zfb288|c360|zfb380|198查|查430|zfb308|c800"); set => SetString(Prefix + "ScoreLowMessages", value); }
        public int ScoreHighThreshold { get => GetInt(Prefix + "ScoreHighThreshold", 5999); set => SetInt(Prefix + "ScoreHighThreshold", value); }
        public string ScoreHighMessages { get => GetString(Prefix + "ScoreHighMessages", "全回|全回支付|全回zfb"); set => SetString(Prefix + "ScoreHighMessages", value); }

        // Speed settings
        public int SpeedMin { get => GetInt(Prefix + "SpeedMin", 3); set => SetInt(Prefix + "SpeedMin", value); }
        public int SpeedMax { get => GetInt(Prefix + "SpeedMax", 10); set => SetInt(Prefix + "SpeedMax", value); }

        /// <summary>获取托号列表</summary>
        public List<string> GetShillList()
        {
            var text = ShillListText;
            if (string.IsNullOrWhiteSpace(text)) return new List<string>();
            return text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();
        }

        /// <summary>根据分数获取下注内容</summary>
        public string GetRandomBet(int score)
        {
            string bets = null;
            
            if (score >= BetRange1Min && score <= BetRange1Max)
                bets = BetRange1Bets;
            else if (score >= BetRange2Min && score <= BetRange2Max)
                bets = BetRange2Bets;
            else if (score >= BetRange3Min && score <= BetRange3Max)
                bets = BetRange3Bets;
            else if (score >= BetRange4Min && score <= BetRange4Max)
                bets = BetRange4Bets;

            if (string.IsNullOrWhiteSpace(bets)) return null;

            var options = bets.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            if (options.Length == 0) return null;

            var rnd = new Random();
            return options[rnd.Next(options.Length)].Trim();
        }

        /// <summary>根据分数获取上下分消息</summary>
        public string GetScoreMessage(int score)
        {
            string messages = null;

            if (score < ScoreLowThreshold)
                messages = ScoreLowMessages;
            else if (score > ScoreHighThreshold)
                messages = ScoreHighMessages;
            else
                return null;

            if (string.IsNullOrWhiteSpace(messages)) return null;

            var options = messages.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            if (options.Length == 0) return null;

            var rnd = new Random();
            return options[rnd.Next(options.Length)].Trim();
        }

        /// <summary>获取随机延迟秒数</summary>
        public int GetRandomDelay()
        {
            var rnd = new Random();
            return rnd.Next(SpeedMin, SpeedMax + 1);
        }

        /// <summary>检查是否为私聊托号</summary>
        public bool IsPrivateShill(string accountId)
        {
            if (string.IsNullOrWhiteSpace(accountId)) return false;
            return GetShillList().Contains(accountId.Trim());
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

