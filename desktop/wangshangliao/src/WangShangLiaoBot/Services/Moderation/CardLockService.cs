using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace WangShangLiaoBot.Services
{
    /// <summary>
    /// 锁名片服务 - 基于招财狗(ZCG)的锁名片功能
    /// 防止玩家频繁修改群名片，超过次数自动踢出
    /// </summary>
    public sealed class CardLockService
    {
        private static CardLockService _instance;
        public static CardLockService Instance => _instance ?? (_instance = new CardLockService());

        private CardLockConfig _config;
        private readonly Dictionary<string, PlayerCardInfo> _cardInfo = new Dictionary<string, PlayerCardInfo>();
        private readonly object _lock = new object();

        // 事件
        public event Action<string, string, string, string> OnCardChanged;  // teamId, playerId, oldCard, newCard
        public event Action<string, string> OnKickPlayer;                    // teamId, playerId
        public event Action<string, string> OnSendWarning;                   // teamId, message
        public event Action<string, string, string> OnResetCard;             // teamId, playerId, originalCard
        public event Action<string> OnLog;

        private CardLockService()
        {
            LoadConfig();
            LoadCardData();
        }

        private string ConfigPath => Path.Combine(DataService.Instance.DatabaseDir, "card-lock-config.ini");
        private string CardDataPath => Path.Combine(DataService.Instance.DatabaseDir, "card-data.txt");

        #region 配置管理

        public CardLockConfig GetConfig()
        {
            lock (_lock)
            {
                return _config ?? CardLockConfig.CreateDefault();
            }
        }

        public void SaveConfig(CardLockConfig config)
        {
            lock (_lock)
            {
                _config = config;
                SaveConfigToFile(config);
            }
        }

        private void LoadConfig()
        {
            try
            {
                if (!File.Exists(ConfigPath))
                {
                    _config = CardLockConfig.CreateDefault();
                    return;
                }

                var config = new CardLockConfig();
                var lines = File.ReadAllLines(ConfigPath, Encoding.UTF8);

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;

                    var idx = line.IndexOf('=');
                    if (idx <= 0) continue;

                    var key = line.Substring(0, idx).Trim();
                    var value = line.Substring(idx + 1).Trim();

                    switch (key)
                    {
                        case "Enabled":
                            config.Enabled = value == "true" || value == "1" || value == "真";
                            break;
                        case "MaxChangeCount":
                            if (int.TryParse(value, out var mcc)) config.MaxChangeCount = mcc;
                            break;
                        case "KickOnExceed":
                            config.KickOnExceed = value == "true" || value == "1" || value == "真";
                            break;
                        case "NotifyInGroup":
                            config.NotifyInGroup = value == "true" || value == "1" || value == "真";
                            break;
                        case "AutoResetCard":
                            config.AutoResetCard = value == "true" || value == "1" || value == "真";
                            break;
                        case "ResetIntervalMinutes":
                            if (int.TryParse(value, out var rim)) config.ResetIntervalMinutes = rim;
                            break;
                        case "WarningTemplate":
                            config.WarningTemplate = value;
                            break;
                        case "KickTemplate":
                            config.KickTemplate = value;
                            break;
                    }
                }

                _config = config;
            }
            catch
            {
                _config = CardLockConfig.CreateDefault();
            }
        }

        private void SaveConfigToFile(CardLockConfig config)
        {
            try
            {
                Directory.CreateDirectory(DataService.Instance.DatabaseDir);
                var sb = new StringBuilder();
                sb.AppendLine("# 锁名片配置 - 自动生成");
                sb.AppendLine($"Enabled={config.Enabled}");
                sb.AppendLine($"MaxChangeCount={config.MaxChangeCount}");
                sb.AppendLine($"KickOnExceed={config.KickOnExceed}");
                sb.AppendLine($"NotifyInGroup={config.NotifyInGroup}");
                sb.AppendLine($"AutoResetCard={config.AutoResetCard}");
                sb.AppendLine($"ResetIntervalMinutes={config.ResetIntervalMinutes}");
                sb.AppendLine($"WarningTemplate={config.WarningTemplate}");
                sb.AppendLine($"KickTemplate={config.KickTemplate}");

                File.WriteAllText(ConfigPath, sb.ToString(), Encoding.UTF8);
            }
            catch { }
        }

        private void LoadCardData()
        {
            try
            {
                if (!File.Exists(CardDataPath)) return;

                var lines = File.ReadAllLines(CardDataPath, Encoding.UTF8);
                foreach (var line in lines)
                {
                    var parts = line.Split('|');
                    if (parts.Length >= 4)
                    {
                        _cardInfo[parts[0]] = new PlayerCardInfo
                        {
                            PlayerId = parts[0],
                            OriginalCard = parts[1],
                            CurrentCard = parts[2],
                            ChangeCount = int.Parse(parts[3]),
                            LastChangeTime = parts.Length > 4 ? DateTime.Parse(parts[4]) : DateTime.MinValue
                        };
                    }
                }
            }
            catch { }
        }

        private void SaveCardData()
        {
            try
            {
                Directory.CreateDirectory(DataService.Instance.DatabaseDir);
                var lines = _cardInfo.Values.Select(c =>
                    $"{c.PlayerId}|{c.OriginalCard}|{c.CurrentCard}|{c.ChangeCount}|{c.LastChangeTime:O}");
                File.WriteAllLines(CardDataPath, lines, Encoding.UTF8);
            }
            catch { }
        }

        #endregion

        #region 名片监控

        /// <summary>
        /// 记录玩家初始名片
        /// </summary>
        public void RegisterCard(string playerId, string card)
        {
            var config = GetConfig();
            if (!config.Enabled) return;

            lock (_lock)
            {
                if (!_cardInfo.ContainsKey(playerId))
                {
                    _cardInfo[playerId] = new PlayerCardInfo
                    {
                        PlayerId = playerId,
                        OriginalCard = card,
                        CurrentCard = card,
                        ChangeCount = 0,
                        RegisterTime = DateTime.Now
                    };
                    SaveCardData();
                    Log($"[锁名片] 记录玩家 {playerId} 初始名片: {card}");
                }
            }
        }

        /// <summary>
        /// 检测名片变化
        /// </summary>
        public CardChangeResult OnCardChange(string teamId, string playerId, string newCard)
        {
            var config = GetConfig();
            if (!config.Enabled)
            {
                return new CardChangeResult { Allowed = true };
            }

            lock (_lock)
            {
                if (!_cardInfo.TryGetValue(playerId, out var info))
                {
                    // 首次记录
                    info = new PlayerCardInfo
                    {
                        PlayerId = playerId,
                        OriginalCard = newCard,
                        CurrentCard = newCard,
                        ChangeCount = 0,
                        RegisterTime = DateTime.Now
                    };
                    _cardInfo[playerId] = info;
                    SaveCardData();
                    return new CardChangeResult { Allowed = true };
                }

                // 检查是否真的发生了变化
                if (info.CurrentCard == newCard)
                {
                    return new CardChangeResult { Allowed = true };
                }

                var oldCard = info.CurrentCard;
                info.CurrentCard = newCard;
                info.ChangeCount++;
                info.LastChangeTime = DateTime.Now;

                // 触发事件
                OnCardChanged?.Invoke(teamId, playerId, oldCard, newCard);
                Log($"[锁名片] 玩家 {playerId} 修改名片: {oldCard} -> {newCard} (第{info.ChangeCount}次)");

                // 检查是否超过限制
                if (info.ChangeCount >= config.MaxChangeCount)
                {
                    if (config.KickOnExceed)
                    {
                        // 踢出玩家
                        OnKickPlayer?.Invoke(teamId, playerId);

                        if (config.NotifyInGroup)
                        {
                            var msg = config.KickTemplate
                                .Replace("[旺旺]", newCard)
                                .Replace("[次数]", info.ChangeCount.ToString())
                                .Replace("[限制]", config.MaxChangeCount.ToString());
                            OnSendWarning?.Invoke(teamId, msg);
                        }

                        // 添加到黑名单
                        SpeechDetectionService.Instance.AddToBlacklist(playerId);

                        SaveCardData();
                        return new CardChangeResult
                        {
                            Allowed = false,
                            Action = CardLockAction.Kick,
                            Message = $"修改名片超过{config.MaxChangeCount}次，已踢出"
                        };
                    }
                }

                // 发送警告
                if (config.NotifyInGroup && info.ChangeCount > 0)
                {
                    var remaining = config.MaxChangeCount - info.ChangeCount;
                    var msg = config.WarningTemplate
                        .Replace("[旺旺]", newCard)
                        .Replace("[次数]", info.ChangeCount.ToString())
                        .Replace("[剩余]", remaining.ToString())
                        .Replace("[限制]", config.MaxChangeCount.ToString());
                    OnSendWarning?.Invoke(teamId, msg);
                }

                // 自动重置名片
                if (config.AutoResetCard)
                {
                    OnResetCard?.Invoke(teamId, playerId, info.OriginalCard);
                    info.CurrentCard = info.OriginalCard;
                }

                SaveCardData();
                return new CardChangeResult
                {
                    Allowed = true,
                    ChangeCount = info.ChangeCount,
                    RemainingChanges = config.MaxChangeCount - info.ChangeCount
                };
            }
        }

        /// <summary>
        /// 重置玩家名片修改次数
        /// </summary>
        public void ResetChangeCount(string playerId)
        {
            lock (_lock)
            {
                if (_cardInfo.TryGetValue(playerId, out var info))
                {
                    info.ChangeCount = 0;
                    SaveCardData();
                    Log($"[锁名片] 重置玩家 {playerId} 修改次数");
                }
            }
        }

        /// <summary>
        /// 重置所有玩家修改次数 (每日重置)
        /// </summary>
        public void ResetAllChangeCounts()
        {
            lock (_lock)
            {
                foreach (var info in _cardInfo.Values)
                {
                    info.ChangeCount = 0;
                }
                SaveCardData();
                Log("[锁名片] 重置所有玩家修改次数");
            }
        }

        /// <summary>
        /// 获取玩家名片信息
        /// </summary>
        public PlayerCardInfo GetCardInfo(string playerId)
        {
            lock (_lock)
            {
                return _cardInfo.TryGetValue(playerId, out var info) ? info : null;
            }
        }

        /// <summary>
        /// 获取所有锁定的名片
        /// </summary>
        public List<PlayerCardInfo> GetAllCardInfo()
        {
            lock (_lock)
            {
                return _cardInfo.Values.ToList();
            }
        }

        /// <summary>
        /// 移除玩家名片记录
        /// </summary>
        public void RemoveCardInfo(string playerId)
        {
            lock (_lock)
            {
                _cardInfo.Remove(playerId);
                SaveCardData();
            }
        }

        #endregion

        private void Log(string message)
        {
            OnLog?.Invoke(message);
            Logger.Info(message);
        }
    }

    #region 锁名片配置和模型

    /// <summary>
    /// 锁名片配置
    /// </summary>
    public class CardLockConfig
    {
        /// <summary>是否启用</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>最大修改次数</summary>
        public int MaxChangeCount { get; set; } = 5;

        /// <summary>超次数踢人</summary>
        public bool KickOnExceed { get; set; } = true;

        /// <summary>群内通知</summary>
        public bool NotifyInGroup { get; set; } = true;

        /// <summary>自动重置名片</summary>
        public bool AutoResetCard { get; set; } = false;

        /// <summary>重置间隔(分钟)</summary>
        public int ResetIntervalMinutes { get; set; } = 1440; // 24小时

        /// <summary>警告模板</summary>
        public string WarningTemplate { get; set; } = "[旺旺] 您已修改名片[次数]次，剩余[剩余]次机会";

        /// <summary>踢出模板</summary>
        public string KickTemplate { get; set; } = "[旺旺] 修改名片超过[限制]次，已被踢出群聊";

        public static CardLockConfig CreateDefault()
        {
            return new CardLockConfig
            {
                Enabled = true,
                MaxChangeCount = 5,
                KickOnExceed = true,
                NotifyInGroup = true,
                AutoResetCard = false,
                ResetIntervalMinutes = 1440,
                WarningTemplate = "[旺旺] 您已修改名片[次数]次，剩余[剩余]次机会",
                KickTemplate = "[旺旺] 修改名片超过[限制]次，已被踢出群聊"
            };
        }
    }

    /// <summary>
    /// 名片修改动作
    /// </summary>
    public enum CardLockAction
    {
        None,
        Warning,
        Reset,
        Kick
    }

    /// <summary>
    /// 名片修改结果
    /// </summary>
    public class CardChangeResult
    {
        public bool Allowed { get; set; }
        public CardLockAction Action { get; set; }
        public int ChangeCount { get; set; }
        public int RemainingChanges { get; set; }
        public string Message { get; set; }
    }

    /// <summary>
    /// 玩家名片信息
    /// </summary>
    public class PlayerCardInfo
    {
        public string PlayerId { get; set; }
        public string OriginalCard { get; set; }
        public string CurrentCard { get; set; }
        public int ChangeCount { get; set; }
        public DateTime RegisterTime { get; set; }
        public DateTime LastChangeTime { get; set; }
    }

    #endregion
}
