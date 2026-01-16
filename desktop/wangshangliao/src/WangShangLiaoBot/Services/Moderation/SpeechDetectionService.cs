using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace WangShangLiaoBot.Services
{
    /// <summary>
    /// 发言检测服务 - 基于招财狗(ZCG)的发言检测系统
    /// 支持字数/行数限制、图片检测、违规撤回、自动禁言/踢人
    /// </summary>
    public sealed class SpeechDetectionService
    {
        private static SpeechDetectionService _instance;
        public static SpeechDetectionService Instance => _instance ?? (_instance = new SpeechDetectionService());

        private SpeechDetectionConfig _config;
        private readonly Dictionary<string, PlayerViolation> _violations = new Dictionary<string, PlayerViolation>();
        private readonly HashSet<string> _blacklist = new HashSet<string>();
        private readonly object _lock = new object();

        // 事件
        public event Action<string, string, int> OnMutePlayer;       // teamId, playerId, minutes
        public event Action<string, string> OnKickPlayer;             // teamId, playerId
        public event Action<string, string> OnWithdrawMessage;        // teamId, messageId
        public event Action<string, string, string> OnSendWarning;    // teamId, playerId, message
        public event Action<string, string> OnAddBlacklist;           // teamId, playerId
        public event Action<string> OnLog;

        private SpeechDetectionService()
        {
            LoadConfig();
            LoadBlacklist();
        }

        private string ConfigPath => Path.Combine(DataService.Instance.DatabaseDir, "speech-detection-config.ini");
        private string BlacklistPath => Path.Combine(DataService.Instance.DatabaseDir, "blacklist.txt");

        #region 配置管理

        public SpeechDetectionConfig GetConfig()
        {
            lock (_lock)
            {
                return _config ?? SpeechDetectionConfig.CreateDefault();
            }
        }

        public void SaveConfig(SpeechDetectionConfig config)
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
                    _config = SpeechDetectionConfig.CreateDefault();
                    return;
                }

                var config = new SpeechDetectionConfig();
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
                        case "MuteCharLimit":
                            if (int.TryParse(value, out var mcl)) config.MuteCharLimit = mcl;
                            break;
                        case "KickCharLimit":
                            if (int.TryParse(value, out var kcl)) config.KickCharLimit = kcl;
                            break;
                        case "MuteLineLimit":
                            if (int.TryParse(value, out var mll)) config.MuteLineLimit = mll;
                            break;
                        case "ImageMuteEnabled":
                            config.ImageMuteEnabled = value == "true" || value == "1" || value == "真";
                            break;
                        case "ImageKickCount":
                            if (int.TryParse(value, out var ikc)) config.ImageKickCount = ikc;
                            break;
                        case "MuteDuration":
                            if (int.TryParse(value, out var md)) config.MuteDuration = md;
                            break;
                        case "WithdrawViolation":
                            config.WithdrawViolation = value == "true" || value == "1" || value == "真";
                            break;
                        case "ZeroBalanceMuteIfNotDeposit":
                            config.ZeroBalanceMuteIfNotDeposit = value == "true" || value == "1" || value == "真";
                            break;
                        case "AutoBlacklistOnKick":
                            config.AutoBlacklistOnKick = value == "true" || value == "1" || value == "真";
                            break;
                        case "AutoBlacklistOnAdminKick":
                            config.AutoBlacklistOnAdminKick = value == "true" || value == "1" || value == "真";
                            break;
                        case "ForbiddenWords":
                            config.ForbiddenWords = value.Split(new[] { '|', ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                            break;
                    }
                }

                _config = config;
            }
            catch
            {
                _config = SpeechDetectionConfig.CreateDefault();
            }
        }

        private void SaveConfigToFile(SpeechDetectionConfig config)
        {
            try
            {
                Directory.CreateDirectory(DataService.Instance.DatabaseDir);
                var sb = new StringBuilder();
                sb.AppendLine("# 发言检测配置 - 自动生成");
                sb.AppendLine($"Enabled={config.Enabled}");
                sb.AppendLine($"MuteCharLimit={config.MuteCharLimit}");
                sb.AppendLine($"KickCharLimit={config.KickCharLimit}");
                sb.AppendLine($"MuteLineLimit={config.MuteLineLimit}");
                sb.AppendLine($"ImageMuteEnabled={config.ImageMuteEnabled}");
                sb.AppendLine($"ImageKickCount={config.ImageKickCount}");
                sb.AppendLine($"MuteDuration={config.MuteDuration}");
                sb.AppendLine($"WithdrawViolation={config.WithdrawViolation}");
                sb.AppendLine($"ZeroBalanceMuteIfNotDeposit={config.ZeroBalanceMuteIfNotDeposit}");
                sb.AppendLine($"AutoBlacklistOnKick={config.AutoBlacklistOnKick}");
                sb.AppendLine($"AutoBlacklistOnAdminKick={config.AutoBlacklistOnAdminKick}");
                sb.AppendLine($"ForbiddenWords={string.Join("|", config.ForbiddenWords)}");

                File.WriteAllText(ConfigPath, sb.ToString(), Encoding.UTF8);
            }
            catch { }
        }

        private void LoadBlacklist()
        {
            try
            {
                if (File.Exists(BlacklistPath))
                {
                    var lines = File.ReadAllLines(BlacklistPath);
                    foreach (var line in lines)
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                            _blacklist.Add(line.Trim());
                    }
                }
            }
            catch { }
        }

        private void SaveBlacklist()
        {
            try
            {
                Directory.CreateDirectory(DataService.Instance.DatabaseDir);
                File.WriteAllLines(BlacklistPath, _blacklist);
            }
            catch { }
        }

        #endregion

        #region 发言检测

        /// <summary>
        /// 检测消息是否违规
        /// </summary>
        public ViolationResult CheckMessage(string teamId, string playerId, string playerNick, 
            string message, string messageId, bool isImage = false)
        {
            var config = GetConfig();
            if (!config.Enabled)
            {
                return new ViolationResult { IsViolation = false };
            }

            // 检查黑名单
            if (IsBlacklisted(playerId))
            {
                return new ViolationResult
                {
                    IsViolation = true,
                    Type = ViolationType.Blacklisted,
                    Action = ViolationAction.Kick,
                    Message = "黑名单用户"
                };
            }

            var result = new ViolationResult { PlayerId = playerId, PlayerNick = playerNick };

            // 图片检测
            if (isImage && config.ImageMuteEnabled)
            {
                return CheckImageViolation(teamId, playerId, playerNick, messageId);
            }

            // 字数检测
            var charCount = message.Length;
            if (charCount >= config.KickCharLimit)
            {
                result.IsViolation = true;
                result.Type = ViolationType.TooManyChars;
                result.Action = ViolationAction.Kick;
                result.Message = $"发言字数{charCount}超过踢出限制{config.KickCharLimit}";
            }
            else if (charCount >= config.MuteCharLimit)
            {
                result.IsViolation = true;
                result.Type = ViolationType.TooManyChars;
                result.Action = ViolationAction.Mute;
                result.Message = $"发言字数{charCount}超过禁言限制{config.MuteCharLimit}";
            }

            // 行数检测
            if (!result.IsViolation)
            {
                var lineCount = message.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
                if (lineCount >= config.MuteLineLimit)
                {
                    result.IsViolation = true;
                    result.Type = ViolationType.TooManyLines;
                    result.Action = ViolationAction.Mute;
                    result.Message = $"发言行数{lineCount}超过限制{config.MuteLineLimit}";
                }
            }

            // 敏感词检测
            if (!result.IsViolation && config.ForbiddenWords.Count > 0)
            {
                var messageLower = message.ToLowerInvariant();
                var foundWord = config.ForbiddenWords.FirstOrDefault(w => 
                    messageLower.Contains(w.ToLowerInvariant()));
                if (foundWord != null)
                {
                    result.IsViolation = true;
                    result.Type = ViolationType.ForbiddenWord;
                    result.Action = ViolationAction.Mute;
                    result.Message = $"发言包含敏感词";
                }
            }

            // 执行处罚
            if (result.IsViolation)
            {
                ExecuteAction(teamId, playerId, playerNick, messageId, result, config);
            }

            return result;
        }

        private ViolationResult CheckImageViolation(string teamId, string playerId, string playerNick, string messageId)
        {
            var config = GetConfig();
            var result = new ViolationResult
            {
                PlayerId = playerId,
                PlayerNick = playerNick,
                IsViolation = true,
                Type = ViolationType.Image
            };

            lock (_lock)
            {
                if (!_violations.TryGetValue(playerId, out var violation))
                {
                    violation = new PlayerViolation { PlayerId = playerId };
                    _violations[playerId] = violation;
                }

                violation.ImageCount++;

                if (violation.ImageCount >= config.ImageKickCount)
                {
                    result.Action = ViolationAction.Kick;
                    result.Message = $"发送图片{violation.ImageCount}次，达到踢出限制";
                    violation.ImageCount = 0; // 重置
                }
                else
                {
                    result.Action = ViolationAction.Mute;
                    result.Message = $"发送图片第{violation.ImageCount}次，禁言警告";
                }
            }

            ExecuteAction(teamId, playerId, playerNick, messageId, result, config);
            return result;
        }

        private void ExecuteAction(string teamId, string playerId, string playerNick, 
            string messageId, ViolationResult result, SpeechDetectionConfig config)
        {
            // 撤回消息
            if (config.WithdrawViolation && !string.IsNullOrEmpty(messageId))
            {
                OnWithdrawMessage?.Invoke(teamId, messageId);
                Log($"[发言检测] 撤回 {playerNick} 的消息");
            }

            // 执行处罚
            switch (result.Action)
            {
                case ViolationAction.Mute:
                    OnMutePlayer?.Invoke(teamId, playerId, config.MuteDuration);
                    OnSendWarning?.Invoke(teamId, playerId, $"@{playerNick} {result.Message}，禁言{config.MuteDuration}分钟");
                    Log($"[发言检测] 禁言 {playerNick} {config.MuteDuration}分钟: {result.Message}");
                    break;

                case ViolationAction.Kick:
                    OnKickPlayer?.Invoke(teamId, playerId);
                    Log($"[发言检测] 踢出 {playerNick}: {result.Message}");

                    // 加入黑名单
                    if (config.AutoBlacklistOnKick)
                    {
                        AddToBlacklist(playerId);
                        OnAddBlacklist?.Invoke(teamId, playerId);
                    }
                    break;
            }
        }

        /// <summary>
        /// 检查0分玩家发言 (只能上分，否则禁言)
        /// </summary>
        public ViolationResult CheckZeroBalanceMessage(string teamId, string playerId, string playerNick, 
            string message, string messageId)
        {
            var config = GetConfig();
            if (!config.ZeroBalanceMuteIfNotDeposit)
            {
                return new ViolationResult { IsViolation = false };
            }

            var balance = ScoreService.Instance.GetBalance(playerId);
            if (balance > 0)
            {
                return new ViolationResult { IsViolation = false };
            }

            // 检查是否为上分消息
            var depositPatterns = new[] { "上", "+", "到", "充", "加" };
            var isDepositMessage = depositPatterns.Any(p => message.Contains(p));

            if (!isDepositMessage)
            {
                var result = new ViolationResult
                {
                    IsViolation = true,
                    Type = ViolationType.ZeroBalanceNotDeposit,
                    Action = ViolationAction.Mute,
                    Message = "0分玩家只能发送上分消息"
                };
                ExecuteAction(teamId, playerId, playerNick, messageId, result, config);
                return result;
            }

            return new ViolationResult { IsViolation = false };
        }

        #endregion

        #region 黑名单管理

        public bool IsBlacklisted(string playerId)
        {
            lock (_lock)
            {
                return _blacklist.Contains(playerId);
            }
        }

        public void AddToBlacklist(string playerId)
        {
            lock (_lock)
            {
                if (_blacklist.Add(playerId))
                {
                    SaveBlacklist();
                    Log($"[黑名单] 添加: {playerId}");
                }
            }
        }

        public void RemoveFromBlacklist(string playerId)
        {
            lock (_lock)
            {
                if (_blacklist.Remove(playerId))
                {
                    SaveBlacklist();
                    Log($"[黑名单] 移除: {playerId}");
                }
            }
        }

        public List<string> GetBlacklist()
        {
            lock (_lock)
            {
                return _blacklist.ToList();
            }
        }

        /// <summary>
        /// 管理员踢人时加入黑名单
        /// </summary>
        public void OnAdminKicked(string playerId)
        {
            var config = GetConfig();
            if (config.AutoBlacklistOnAdminKick)
            {
                AddToBlacklist(playerId);
            }
        }

        #endregion

        #region 违规记录

        public PlayerViolation GetViolationRecord(string playerId)
        {
            lock (_lock)
            {
                return _violations.TryGetValue(playerId, out var v) ? v : null;
            }
        }

        public void ResetViolations(string playerId)
        {
            lock (_lock)
            {
                _violations.Remove(playerId);
            }
        }

        public void ResetAllViolations()
        {
            lock (_lock)
            {
                _violations.Clear();
            }
        }

        #endregion

        private void Log(string message)
        {
            OnLog?.Invoke(message);
            Logger.Info(message);
        }
    }

    #region 发言检测配置和模型

    /// <summary>
    /// 发言检测配置
    /// </summary>
    public class SpeechDetectionConfig
    {
        /// <summary>是否启用</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>禁言字数限制</summary>
        public int MuteCharLimit { get; set; } = 100;

        /// <summary>踢出字数限制</summary>
        public int KickCharLimit { get; set; } = 200;

        /// <summary>禁言行数限制</summary>
        public int MuteLineLimit { get; set; } = 4;

        /// <summary>图片禁言开关</summary>
        public bool ImageMuteEnabled { get; set; } = true;

        /// <summary>图片踢出次数</summary>
        public int ImageKickCount { get; set; } = 3;

        /// <summary>禁言时长(分钟)</summary>
        public int MuteDuration { get; set; } = 10;

        /// <summary>违规撤回</summary>
        public bool WithdrawViolation { get; set; } = true;

        /// <summary>0分玩家只能上分否则禁言</summary>
        public bool ZeroBalanceMuteIfNotDeposit { get; set; } = false;

        /// <summary>被机器人踢出自动加黑名单</summary>
        public bool AutoBlacklistOnKick { get; set; } = true;

        /// <summary>被管理员踢出自动加黑名单</summary>
        public bool AutoBlacklistOnAdminKick { get; set; } = true;

        /// <summary>敏感词列表</summary>
        public List<string> ForbiddenWords { get; set; } = new List<string>();

        public static SpeechDetectionConfig CreateDefault()
        {
            return new SpeechDetectionConfig
            {
                Enabled = true,
                MuteCharLimit = 100,
                KickCharLimit = 200,
                MuteLineLimit = 4,
                ImageMuteEnabled = true,
                ImageKickCount = 3,
                MuteDuration = 10,
                WithdrawViolation = true,
                ZeroBalanceMuteIfNotDeposit = false,
                AutoBlacklistOnKick = true,
                AutoBlacklistOnAdminKick = true,
                ForbiddenWords = new List<string>()
            };
        }
    }

    /// <summary>
    /// 违规类型
    /// </summary>
    public enum ViolationType
    {
        None,
        TooManyChars,
        TooManyLines,
        Image,
        ForbiddenWord,
        ZeroBalanceNotDeposit,
        Blacklisted
    }

    /// <summary>
    /// 处罚动作
    /// </summary>
    public enum ViolationAction
    {
        None,
        Warning,
        Mute,
        Kick
    }

    /// <summary>
    /// 违规检测结果
    /// </summary>
    public class ViolationResult
    {
        public bool IsViolation { get; set; }
        public string PlayerId { get; set; }
        public string PlayerNick { get; set; }
        public ViolationType Type { get; set; }
        public ViolationAction Action { get; set; }
        public string Message { get; set; }
    }

    /// <summary>
    /// 玩家违规记录
    /// </summary>
    public class PlayerViolation
    {
        public string PlayerId { get; set; }
        public int ImageCount { get; set; }
        public int TotalViolations { get; set; }
        public DateTime LastViolationTime { get; set; }
    }

    #endregion
}
