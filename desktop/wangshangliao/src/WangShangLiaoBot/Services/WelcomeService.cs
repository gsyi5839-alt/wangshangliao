using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WangShangLiaoBot.Services
{
    /// <summary>
    /// 进群欢迎服务 - 基于招财狗(ZCG)的进群欢迎私聊功能
    /// 支持新成员进群自动私聊欢迎、好友申请自动同意等
    /// </summary>
    public sealed class WelcomeService
    {
        private static WelcomeService _instance;
        public static WelcomeService Instance => _instance ?? (_instance = new WelcomeService());

        private WelcomeConfig _config;
        private readonly HashSet<string> _welcomedPlayers = new HashSet<string>();
        private readonly Queue<PendingFriendRequest> _pendingRequests = new Queue<PendingFriendRequest>();
        private readonly object _lock = new object();

        // 事件
        public event Action<string, string> OnSendPrivateMessage;    // playerId, message
        public event Action<string, string> OnSendGroupMessage;      // teamId, message
        public event Action<string, bool> OnAcceptFriendRequest;     // requestId, accept
        public event Action<string, bool> OnAcceptJoinRequest;       // requestId, accept
        public event Action<string> OnLog;

        private WelcomeService()
        {
            LoadConfig();
        }

        private string ConfigPath => Path.Combine(DataService.Instance.DatabaseDir, "welcome-config.ini");

        #region 配置管理

        public WelcomeConfig GetConfig()
        {
            lock (_lock)
            {
                return _config ?? WelcomeConfig.CreateDefault();
            }
        }

        public void SaveConfig(WelcomeConfig config)
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
                    _config = WelcomeConfig.CreateDefault();
                    return;
                }

                var config = new WelcomeConfig();
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
                        case "PrivateWelcomeEnabled":
                            config.PrivateWelcomeEnabled = value == "true" || value == "1" || value == "真";
                            break;
                        case "GroupWelcomeEnabled":
                            config.GroupWelcomeEnabled = value == "true" || value == "1" || value == "真";
                            break;
                        case "PrivateWelcomeMessage":
                            config.PrivateWelcomeMessage = value.Replace("\\n", "\n");
                            break;
                        case "GroupWelcomeMessage":
                            config.GroupWelcomeMessage = value.Replace("\\n", "\n");
                            break;
                        case "WelcomeSuffixNotSealed":
                            config.WelcomeSuffixNotSealed = value.Replace("\\n", "\n");
                            break;
                        case "WelcomeSuffixSealed":
                            config.WelcomeSuffixSealed = value.Replace("\\n", "\n");
                            break;
                        case "AutoAcceptFriend":
                            config.AutoAcceptFriend = value == "true" || value == "1" || value == "真";
                            break;
                        case "AutoAcceptJoinFromBill":
                            config.AutoAcceptJoinFromBill = value == "true" || value == "1" || value == "真";
                            break;
                        case "AutoAcceptJoinFromTrustee":
                            config.AutoAcceptJoinFromTrustee = value == "true" || value == "1" || value == "真";
                            break;
                        case "WelcomeDelayMs":
                            if (int.TryParse(value, out var wd)) config.WelcomeDelayMs = wd;
                            break;
                    }
                }

                _config = config;
            }
            catch
            {
                _config = WelcomeConfig.CreateDefault();
            }
        }

        private void SaveConfigToFile(WelcomeConfig config)
        {
            try
            {
                Directory.CreateDirectory(DataService.Instance.DatabaseDir);
                var sb = new StringBuilder();
                sb.AppendLine("# 进群欢迎配置 - 自动生成");
                sb.AppendLine($"PrivateWelcomeEnabled={config.PrivateWelcomeEnabled}");
                sb.AppendLine($"GroupWelcomeEnabled={config.GroupWelcomeEnabled}");
                sb.AppendLine($"PrivateWelcomeMessage={config.PrivateWelcomeMessage.Replace("\n", "\\n")}");
                sb.AppendLine($"GroupWelcomeMessage={config.GroupWelcomeMessage.Replace("\n", "\\n")}");
                sb.AppendLine($"WelcomeSuffixNotSealed={config.WelcomeSuffixNotSealed.Replace("\n", "\\n")}");
                sb.AppendLine($"WelcomeSuffixSealed={config.WelcomeSuffixSealed.Replace("\n", "\\n")}");
                sb.AppendLine($"AutoAcceptFriend={config.AutoAcceptFriend}");
                sb.AppendLine($"AutoAcceptJoinFromBill={config.AutoAcceptJoinFromBill}");
                sb.AppendLine($"AutoAcceptJoinFromTrustee={config.AutoAcceptJoinFromTrustee}");
                sb.AppendLine($"WelcomeDelayMs={config.WelcomeDelayMs}");

                File.WriteAllText(ConfigPath, sb.ToString(), Encoding.UTF8);
            }
            catch { }
        }

        #endregion

        #region 进群欢迎

        /// <summary>
        /// 处理新成员进群
        /// </summary>
        public async Task OnMemberJoined(string teamId, string playerId, string playerNick, bool isSealed)
        {
            var config = GetConfig();

            // 检查是否已欢迎过
            lock (_lock)
            {
                var key = $"{teamId}_{playerId}";
                if (_welcomedPlayers.Contains(key))
                {
                    return;
                }
                _welcomedPlayers.Add(key);

                // 清理过多的记录
                if (_welcomedPlayers.Count > 10000)
                {
                    _welcomedPlayers.Clear();
                }
            }

            // 检查黑名单
            if (SpeechDetectionService.Instance.IsBlacklisted(playerId))
            {
                Log($"[进群欢迎] 黑名单用户 {playerNick} 进群，跳过欢迎");
                return;
            }

            // 注册名片
            CardLockService.Instance.RegisterCard(playerId, playerNick);

            // 延迟发送
            if (config.WelcomeDelayMs > 0)
            {
                await Task.Delay(config.WelcomeDelayMs);
            }

            // 私聊欢迎
            if (config.PrivateWelcomeEnabled)
            {
                var message = FormatWelcomeMessage(config.PrivateWelcomeMessage, playerNick, isSealed, config);
                OnSendPrivateMessage?.Invoke(playerId, message);
                Log($"[进群欢迎] 私聊欢迎 {playerNick}");
            }

            // 群内欢迎
            if (config.GroupWelcomeEnabled)
            {
                var message = FormatWelcomeMessage(config.GroupWelcomeMessage, playerNick, isSealed, config);
                message = $"@{playerNick} " + message;
                OnSendGroupMessage?.Invoke(teamId, message);
                Log($"[进群欢迎] 群内欢迎 {playerNick}");
            }
        }

        private string FormatWelcomeMessage(string template, string nick, bool isSealed, WelcomeConfig config)
        {
            var message = template
                .Replace("[旺旺]", nick)
                .Replace("[昵称]", nick);

            // 添加后缀
            if (isSealed)
            {
                message += config.WelcomeSuffixSealed;
            }
            else
            {
                message += config.WelcomeSuffixNotSealed;
            }

            return message;
        }

        #endregion

        #region 好友/入群申请

        /// <summary>
        /// 处理好友申请
        /// </summary>
        public void OnFriendRequest(string requestId, string playerId, string playerNick, string message)
        {
            var config = GetConfig();

            if (config.AutoAcceptFriend)
            {
                OnAcceptFriendRequest?.Invoke(requestId, true);
                Log($"[好友申请] 自动同意 {playerNick} 的好友申请");
            }
            else
            {
                // 加入待处理队列
                lock (_lock)
                {
                    _pendingRequests.Enqueue(new PendingFriendRequest
                    {
                        RequestId = requestId,
                        PlayerId = playerId,
                        PlayerNick = playerNick,
                        Message = message,
                        RequestTime = DateTime.Now,
                        Type = RequestType.Friend
                    });
                }
                Log($"[好友申请] 收到 {playerNick} 的好友申请，等待处理");
            }
        }

        /// <summary>
        /// 处理入群申请
        /// </summary>
        public void OnJoinRequest(string requestId, string teamId, string playerId, string playerNick, string inviterId)
        {
            var config = GetConfig();

            // 检查黑名单
            if (SpeechDetectionService.Instance.IsBlacklisted(playerId))
            {
                OnAcceptJoinRequest?.Invoke(requestId, false);
                Log($"[入群申请] 拒绝黑名单用户 {playerNick} 的入群申请");
                return;
            }

            // 检查是否为账单玩家
            if (config.AutoAcceptJoinFromBill)
            {
                var balance = ScoreService.Instance.GetBalance(playerId);
                if (balance > 0)
                {
                    OnAcceptJoinRequest?.Invoke(requestId, true);
                    Log($"[入群申请] 自动同意账单玩家 {playerNick} 的入群申请");
                    return;
                }
            }

            // 检查是否为托管玩家
            if (config.AutoAcceptJoinFromTrustee)
            {
                if (TrusteeService.Instance.IsTrustee(playerId))
                {
                    OnAcceptJoinRequest?.Invoke(requestId, true);
                    Log($"[入群申请] 自动同意托管玩家 {playerNick} 的入群申请");
                    return;
                }
            }

            // 加入待处理队列
            lock (_lock)
            {
                _pendingRequests.Enqueue(new PendingFriendRequest
                {
                    RequestId = requestId,
                    PlayerId = playerId,
                    PlayerNick = playerNick,
                    TeamId = teamId,
                    InviterId = inviterId,
                    RequestTime = DateTime.Now,
                    Type = RequestType.Join
                });
            }
            Log($"[入群申请] 收到 {playerNick} 的入群申请，等待处理");
        }

        /// <summary>
        /// 获取待处理申请
        /// </summary>
        public List<PendingFriendRequest> GetPendingRequests()
        {
            lock (_lock)
            {
                return _pendingRequests.ToList();
            }
        }

        /// <summary>
        /// 处理申请
        /// </summary>
        public void ProcessRequest(string requestId, bool accept)
        {
            lock (_lock)
            {
                var requests = _pendingRequests.ToArray();
                _pendingRequests.Clear();

                foreach (var req in requests)
                {
                    if (req.RequestId == requestId)
                    {
                        if (req.Type == RequestType.Friend)
                        {
                            OnAcceptFriendRequest?.Invoke(requestId, accept);
                        }
                        else
                        {
                            OnAcceptJoinRequest?.Invoke(requestId, accept);
                        }
                        Log($"[申请处理] {(accept ? "同意" : "拒绝")} {req.PlayerNick} 的申请");
                    }
                    else
                    {
                        _pendingRequests.Enqueue(req);
                    }
                }
            }
        }

        #endregion

        #region 成员离开

        /// <summary>
        /// 处理成员离开
        /// </summary>
        public void OnMemberLeft(string teamId, string playerId, string playerNick, bool isKicked, string operatorId)
        {
            // 如果是被管理员踢出，加入黑名单
            if (isKicked && !string.IsNullOrEmpty(operatorId))
            {
                SpeechDetectionService.Instance.OnAdminKicked(playerId);
            }

            // 移除名片记录
            CardLockService.Instance.RemoveCardInfo(playerId);

            // 清除欢迎记录
            lock (_lock)
            {
                _welcomedPlayers.Remove($"{teamId}_{playerId}");
            }

            Log($"[成员离开] {playerNick} {(isKicked ? "被踢出" : "退出")}群聊");
        }

        #endregion

        private void Log(string message)
        {
            OnLog?.Invoke(message);
            Logger.Info(message);
        }
    }

    #region 欢迎服务配置和模型

    /// <summary>
    /// 欢迎配置
    /// </summary>
    public class WelcomeConfig
    {
        /// <summary>私聊欢迎开关</summary>
        public bool PrivateWelcomeEnabled { get; set; } = true;

        /// <summary>群内欢迎开关</summary>
        public bool GroupWelcomeEnabled { get; set; } = false;

        /// <summary>私聊欢迎消息</summary>
        public string PrivateWelcomeMessage { get; set; } = "恭喜发财，私聊都是骗子，请认准管理。";

        /// <summary>群内欢迎消息</summary>
        public string GroupWelcomeMessage { get; set; } = "欢迎加入！";

        /// <summary>未封盘时的欢迎后缀</summary>
        public string WelcomeSuffixNotSealed { get; set; } = "\n当前可下注";

        /// <summary>已封盘时的欢迎后缀</summary>
        public string WelcomeSuffixSealed { get; set; } = "\n当前已封盘，请等待下期";

        /// <summary>自动同意好友申请</summary>
        public bool AutoAcceptFriend { get; set; } = true;

        /// <summary>自动同意账单玩家入群</summary>
        public bool AutoAcceptJoinFromBill { get; set; } = true;

        /// <summary>自动同意托管玩家入群</summary>
        public bool AutoAcceptJoinFromTrustee { get; set; } = true;

        /// <summary>欢迎延迟(毫秒)</summary>
        public int WelcomeDelayMs { get; set; } = 1000;

        public static WelcomeConfig CreateDefault()
        {
            return new WelcomeConfig
            {
                PrivateWelcomeEnabled = true,
                GroupWelcomeEnabled = false,
                PrivateWelcomeMessage = "恭喜发财，私聊都是骗子，请认准管理。",
                GroupWelcomeMessage = "欢迎加入！",
                WelcomeSuffixNotSealed = "\n当前可下注",
                WelcomeSuffixSealed = "\n当前已封盘，请等待下期",
                AutoAcceptFriend = true,
                AutoAcceptJoinFromBill = true,
                AutoAcceptJoinFromTrustee = true,
                WelcomeDelayMs = 1000
            };
        }
    }

    /// <summary>
    /// 申请类型
    /// </summary>
    public enum RequestType
    {
        Friend,
        Join
    }

    /// <summary>
    /// 待处理申请
    /// </summary>
    public class PendingFriendRequest
    {
        public string RequestId { get; set; }
        public string PlayerId { get; set; }
        public string PlayerNick { get; set; }
        public string TeamId { get; set; }
        public string InviterId { get; set; }
        public string Message { get; set; }
        public DateTime RequestTime { get; set; }
        public RequestType Type { get; set; }
    }

    #endregion
}
