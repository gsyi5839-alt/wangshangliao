using System.Globalization;

namespace WangShangLiaoBot.Services
{
    /// <summary>
    /// 群名片设置服务
    /// </summary>
    public sealed class CardSettingsService
    {
        private static CardSettingsService _instance;
        public static CardSettingsService Instance => _instance ?? (_instance = new CardSettingsService());

        private CardSettingsService() { }

        private const string KeyNotifyToGroup = "Card:NotifyToGroup";
        private const string KeyNotifyToAdmin = "Card:NotifyToAdmin";
        private const string KeyLockCardEnabled = "Card:LockCardEnabled";
        private const string KeyKickOnRenameEnabled = "Card:KickOnRenameEnabled";
        private const string KeyRenameLimit = "Card:RenameLimit";
        private const string KeyNoNotifyOnJoin = "Card:NoNotifyOnJoin";

        /// <summary>
        /// 提醒发送到群里
        /// </summary>
        public bool NotifyToGroup
        {
            get => GetBool(KeyNotifyToGroup, true);
            set => SetBool(KeyNotifyToGroup, value);
        }

        /// <summary>
        /// 提醒发送到管理号
        /// </summary>
        public bool NotifyToAdmin
        {
            get => GetBool(KeyNotifyToAdmin, false);
            set => SetBool(KeyNotifyToAdmin, value);
        }

        /// <summary>
        /// 锁名片开关
        /// </summary>
        public bool LockCardEnabled
        {
            get => GetBool(KeyLockCardEnabled, true);
            set => SetBool(KeyLockCardEnabled, value);
        }

        /// <summary>
        /// 改名次数超过N次踢出并拉黑
        /// </summary>
        public bool KickOnRenameEnabled
        {
            get => GetBool(KeyKickOnRenameEnabled, true);
            set => SetBool(KeyKickOnRenameEnabled, value);
        }

        /// <summary>
        /// 改名次数限制
        /// </summary>
        public int RenameLimit
        {
            get => GetInt(KeyRenameLimit, 3);
            set => SetInt(KeyRenameLimit, value);
        }

        /// <summary>
        /// 进群改名片不提醒
        /// </summary>
        public bool NoNotifyOnJoin
        {
            get => GetBool(KeyNoNotifyOnJoin, false);
            set => SetBool(KeyNoNotifyOnJoin, value);
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

