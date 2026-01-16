using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using WangShangLiaoBot.Models;

namespace WangShangLiaoBot.Services
{
    /// <summary>
    /// 数据管理服务 - 管理所有数据文件
    /// </summary>
    public class DataService
    {
        private static DataService _instance;
        public static DataService Instance => _instance ?? (_instance = new DataService());
        
        // Data directory path
        private readonly string _dataDir;
        
        // Data file paths
        private readonly string _scoreDbPath;
        private readonly string _blacklistDbPath;
        private readonly string _billDbPath;
        private readonly string _settingsIniPath;
        
        // Folder paths
        public string GroupMemberCacheDir { get; }
        public string BackupDir { get; }
        public string DatabaseDir { get; }
        public string ImageDir { get; }
        public string PlayerDataDir { get; }
        public string MessageLogDir { get; }
        
        // Message log lock
        private readonly object _msgLogLock = new object();
        
        private DataService()
        {
            // Get base directory (exe location)
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            _dataDir = Path.Combine(baseDir, "Data");
            
            // Ensure directories exist
            Directory.CreateDirectory(_dataDir);
            GroupMemberCacheDir = Path.Combine(_dataDir, "群成员缓存");
            BackupDir = Path.Combine(_dataDir, "备份");
            DatabaseDir = Path.Combine(_dataDir, "数据库");
            ImageDir = Path.Combine(_dataDir, "图片");
            PlayerDataDir = Path.Combine(_dataDir, "玩家资料");
            MessageLogDir = Path.Combine(_dataDir, "收发日志");
            
            Directory.CreateDirectory(GroupMemberCacheDir);
            Directory.CreateDirectory(BackupDir);
            Directory.CreateDirectory(DatabaseDir);
            Directory.CreateDirectory(ImageDir);
            Directory.CreateDirectory(PlayerDataDir);
            Directory.CreateDirectory(MessageLogDir);
            
            // Data file paths
            _scoreDbPath = Path.Combine(_dataDir, "上下分.db");
            _blacklistDbPath = Path.Combine(_dataDir, "攻击.db");
            _billDbPath = Path.Combine(_dataDir, "账单.db");
            _settingsIniPath = Path.Combine(_dataDir, "设置.ini");
            
            Logger.Info("数据服务初始化完成");
        }
        
        #region Group Member Cache
        
        /// <summary>
        /// Save group members to cache
        /// </summary>
        public void SaveGroupMembersCache(string groupId, List<ContactInfo> members)
        {
            try
            {
                var filePath = Path.Combine(GroupMemberCacheDir, $"{groupId}.txt");
                var lines = new List<string>
                {
                    $"# 群成员缓存 - 群号: {groupId}",
                    $"# 更新时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                    $"# 格式: 旺商号|昵称|类型",
                    ""
                };
                
                foreach (var m in members)
                {
                    lines.Add($"{m.WangShangId}|{m.Name}|{m.Type}");
                }
                File.WriteAllLines(filePath, lines, Encoding.UTF8);
                Logger.Info($"群成员缓存已保存: {groupId}, 共 {members.Count} 人");
            }
            catch (Exception ex)
            {
                Logger.Error($"保存群成员缓存失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Load group members from cache
        /// </summary>
        public List<ContactInfo> LoadGroupMembersCache(string groupId)
        {
            var list = new List<ContactInfo>();
            try
            {
                var filePath = Path.Combine(GroupMemberCacheDir, $"{groupId}.txt");
                if (File.Exists(filePath))
                {
                    var lines = File.ReadAllLines(filePath, Encoding.UTF8);
                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;
                        
                        var parts = line.Split('|');
                        if (parts.Length >= 2)
                        {
                            list.Add(new ContactInfo
                            {
                                WangShangId = parts[0].Trim(),
                                Name = parts[1].Trim(),
                                Type = parts.Length > 2 ? parts[2].Trim() : "cache"
                            });
                        }
                    }
                    Logger.Info($"加载群成员缓存: {groupId}, 共 {list.Count} 人");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"加载群成员缓存失败: {ex.Message}");
            }
            return list;
        }
        
        /// <summary>
        /// Get all cached group IDs
        /// </summary>
        public List<string> GetCachedGroupIds()
        {
            var list = new List<string>();
            try
            {
                var files = Directory.GetFiles(GroupMemberCacheDir, "*.txt");
                foreach (var f in files)
                {
                    list.Add(Path.GetFileNameWithoutExtension(f));
                }
            }
            catch { }
            return list;
        }
        
        #endregion
        
        #region Player Management
        
        /// <summary>
        /// Check if a string is an MD5 hash (32 hex characters)
        /// WangShangLiao encrypts nicknames to MD5 hashes like "b061f94a6050d3c5e8e23d3d82cebdc1"
        /// </summary>
        private static bool IsMd5Hash(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length != 32)
                return false;
            
            // Check if all characters are hex digits (0-9, a-f, A-F)
            foreach (char c in value)
            {
                if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F')))
                    return false;
            }
            return true;
        }
        
        /// <summary>
        /// Get or create player
        /// </summary>
        public Player GetOrCreatePlayer(string wangWangId, string nickname = "")
        {
            var player = GetPlayer(wangWangId);
            if (player == null)
            {
                // Don't save MD5 hash as nickname - WangShangLiao encrypts some nicknames
                var safeNickname = IsMd5Hash(nickname) ? "" : nickname;
                
                player = new Player
                {
                    WangWangId = wangWangId,
                    Nickname = safeNickname,
                    Score = 0
                };
                SavePlayer(player);
            }
            else if (!string.IsNullOrEmpty(nickname) && !IsMd5Hash(nickname))
            {
                // Update nickname if current is empty/hash and new is plaintext
                if (string.IsNullOrEmpty(player.Nickname) || IsMd5Hash(player.Nickname))
                {
                    player.Nickname = nickname;
                    SavePlayer(player);
                    Logger.Info($"[DataService] Nickname updated from hash to plaintext: {wangWangId} -> {nickname}");
                }
            }
            return player;
        }
        
        /// <summary>
        /// Update player nickname if different from current - 艾特变昵称
        /// </summary>
        /// <param name="wangWangId">Player ID</param>
        /// <param name="newNickname">New nickname from @ mention</param>
        /// <param name="enableUpdate">Whether to actually update (from MainForm.EnableAtNicknameUpdate)</param>
        /// <returns>True if nickname was updated</returns>
        public bool UpdateNicknameIfChanged(string wangWangId, string newNickname, bool enableUpdate = true)
        {
            if (string.IsNullOrWhiteSpace(wangWangId) || string.IsNullOrWhiteSpace(newNickname))
                return false;
            
            if (!enableUpdate)
                return false;
            
            // Skip MD5 hash nicknames - they are encrypted by WangShangLiao
            if (IsMd5Hash(newNickname))
            {
                Logger.Info($"[DataService] Skipped hash nickname: {wangWangId} -> {newNickname}");
                return false;
            }
            
            var player = GetPlayer(wangWangId);
            if (player == null)
                return false;
            
            // Check if nickname changed
            var oldNickname = player.Nickname ?? "";
            if (string.Equals(oldNickname, newNickname, StringComparison.Ordinal))
                return false;
            
            // Update nickname
            player.Nickname = newNickname;
            SavePlayer(player);
            
            Logger.Info($"[DataService] Nickname auto-updated: {wangWangId} [{oldNickname}] -> [{newNickname}]");
            return true;
        }
        
        /// <summary>
        /// Get player by WangWangId
        /// </summary>
        public Player GetPlayer(string wangWangId)
        {
            try
            {
                var filePath = Path.Combine(PlayerDataDir, $"{wangWangId}.txt");
                if (File.Exists(filePath))
                {
                    var lines = File.ReadAllLines(filePath, Encoding.UTF8);
                    var player = new Player { WangWangId = wangWangId };
                    
                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;
                        
                        var parts = line.Split(new[] { '=' }, 2);
                        if (parts.Length == 2)
                        {
                            var key = parts[0].Trim();
                            var value = parts[1].Trim();
                            
                            switch (key)
                            {
                                case "Nickname": player.Nickname = value; break;
                                case "Remark": player.Remark = value; break;
                                case "Score": decimal.TryParse(value, out var s); player.Score = s; break;
                                case "ReservedScore": decimal.TryParse(value, out var r); player.ReservedScore = r; break;
                                case "IsTuo": bool.TryParse(value, out var t); player.IsTuo = t; break;
                                case "IsBlacklisted": bool.TryParse(value, out var b); player.IsBlacklisted = b; break;
                            }
                        }
                    }
                    return player;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"获取玩家失败: {ex.Message}");
            }
            return null;
        }
        
        /// <summary>
        /// Save player info
        /// </summary>
        public void SavePlayer(Player player)
        {
            try
            {
                var filePath = Path.Combine(PlayerDataDir, $"{player.WangWangId}.txt");
                var lines = new List<string>
                {
                    $"# 玩家资料 - {player.WangWangId}",
                    $"# 更新时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                    "",
                    $"Nickname={player.Nickname ?? ""}",
                    $"Remark={player.Remark ?? ""}",
                    $"Score={player.Score}",
                    $"ReservedScore={player.ReservedScore}",
                    $"IsTuo={player.IsTuo}",
                    $"IsBlacklisted={player.IsBlacklisted}"
                };
                File.WriteAllLines(filePath, lines, Encoding.UTF8);

                // Update daily stats snapshot (for template engine variables like [今天统计*])
                UpdateDailyStats(player);
            }
            catch (Exception ex)
            {
                Logger.Error($"保存玩家失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Get all players
        /// </summary>
        public List<Player> GetAllPlayers()
        {
            var list = new List<Player>();
            try
            {
                var files = Directory.GetFiles(PlayerDataDir, "*.txt");
                foreach (var f in files)
                {
                    var wangWangId = Path.GetFileNameWithoutExtension(f);
                    var player = GetPlayer(wangWangId);
                    if (player != null)
                    {
                        list.Add(player);
                    }
                }
                list.Sort((a, b) => b.Score.CompareTo(a.Score));
            }
            catch (Exception ex)
            {
                Logger.Error($"获取所有玩家失败: {ex.Message}");
            }
            return list;
        }
        
        /// <summary>
        /// Delete player by WangWangId
        /// </summary>
        public bool DeletePlayer(string wangWangId)
        {
            try
            {
                var filePath = Path.Combine(PlayerDataDir, $"{wangWangId}.txt");
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    Logger.Info($"已删除玩家: {wangWangId}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"删除玩家失败: {ex.Message}");
            }
            return false;
        }
        
        #endregion
        
        #region Score Records
        
        /// <summary>
        /// Add score record
        /// </summary>
        public void AddScoreRecord(string wangWangId, string playerName, string type, decimal amount, decimal balance, string remark = "")
        {
            try
            {
                var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}|{wangWangId}|{playerName}|{type}|{amount}|{balance}|{remark}";
                File.AppendAllText(_scoreDbPath, line + Environment.NewLine, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Logger.Error($"添加积分记录失败: {ex.Message}");
            }
        }
        
        #endregion
        
        #region Blacklist
        
        /// <summary>
        /// Add to blacklist
        /// </summary>
        public void AddToBlacklist(string wangWangId, string reason = "")
        {
            try
            {
                if (!IsInBlacklist(wangWangId))
                {
                    var line = $"{wangWangId}|{reason}|{DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                    File.AppendAllText(_blacklistDbPath, line + Environment.NewLine, Encoding.UTF8);
                    Logger.Info($"已添加黑名单: {wangWangId}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"添加黑名单失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Remove from blacklist
        /// </summary>
        public void RemoveFromBlacklist(string wangWangId)
        {
            try
            {
                if (File.Exists(_blacklistDbPath))
                {
                    var lines = File.ReadAllLines(_blacklistDbPath, Encoding.UTF8);
                    var newLines = new List<string>();
                    foreach (var line in lines)
                    {
                        if (!line.StartsWith(wangWangId + "|"))
                        {
                            newLines.Add(line);
                        }
                    }
                    File.WriteAllLines(_blacklistDbPath, newLines, Encoding.UTF8);
                    Logger.Info($"已移除黑名单: {wangWangId}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"移除黑名单失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Check if in blacklist
        /// </summary>
        public bool IsInBlacklist(string wangWangId)
        {
            try
            {
                if (File.Exists(_blacklistDbPath))
                {
                    var lines = File.ReadAllLines(_blacklistDbPath, Encoding.UTF8);
                    foreach (var line in lines)
                    {
                        if (line.StartsWith(wangWangId + "|"))
                            return true;
                    }
                }
            }
            catch { }
            return false;
        }
        
        /// <summary>
        /// Get all blacklist entries
        /// </summary>
        public List<string> GetBlacklist()
        {
            var list = new List<string>();
            try
            {
                if (File.Exists(_blacklistDbPath))
                {
                    var lines = File.ReadAllLines(_blacklistDbPath, Encoding.UTF8);
                    foreach (var line in lines)
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            var parts = line.Split('|');
                            if (parts.Length > 0) list.Add(parts[0]);
                        }
                    }
                }
            }
            catch { }
            return list;
        }
        
        #endregion
        
        #region Settings
        
        /// <summary>
        /// Save setting
        /// </summary>
        public void SaveSetting(string key, string value)
        {
            try
            {
                var dict = LoadAllSettings();
                dict[key] = value;
                
                var lines = new List<string>();
                foreach (var kv in dict)
                {
                    lines.Add($"{kv.Key}={kv.Value}");
                }
                File.WriteAllLines(_settingsIniPath, lines, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Logger.Error($"保存设置失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Get setting
        /// </summary>
        public string GetSetting(string key, string defaultValue = "")
        {
            try
            {
                var dict = LoadAllSettings();
                return dict.ContainsKey(key) ? dict[key] : defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }
        
        private Dictionary<string, string> LoadAllSettings()
        {
            var dict = new Dictionary<string, string>();
            try
            {
                if (File.Exists(_settingsIniPath))
                {
                    var lines = File.ReadAllLines(_settingsIniPath, Encoding.UTF8);
                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;
                        
                        var idx = line.IndexOf('=');
                        if (idx > 0)
                        {
                            dict[line.Substring(0, idx).Trim()] = line.Substring(idx + 1).Trim();
                        }
                    }
                }
            }
            catch { }
            return dict;
        }

        // ===== Daily stats helpers (for template engine) =====

        private static string DailyKey(string dateKey, string wangWangId, string field)
            => $"Daily:{dateKey}:{wangWangId}:{field}";

        private static string DailyGlobalKey(string dateKey, string field)
            => $"Daily:{dateKey}:{field}";

        public decimal GetDailyDecimal(string dateKey, string wangWangId, string field, decimal defaultValue = 0m)
        {
            var str = GetSetting(DailyKey(dateKey, wangWangId, field), "");
            if (decimal.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out var v))
                return v;
            if (decimal.TryParse(str, out v)) return v;
            return defaultValue;
        }

        public void SetDailyDecimal(string dateKey, string wangWangId, string field, decimal value)
        {
            SaveSetting(DailyKey(dateKey, wangWangId, field), value.ToString(CultureInfo.InvariantCulture));
        }

        public int GetDailyInt(string dateKey, string key, int defaultValue = 0)
        {
            var str = GetSetting(DailyGlobalKey(dateKey, key), "");
            if (int.TryParse(str, out var v)) return v;
            return defaultValue;
        }

        public void SetDailyInt(string dateKey, string key, int value)
        {
            SaveSetting(DailyGlobalKey(dateKey, key), value.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Update daily stats for a player (called when SavePlayer is invoked).
        /// Tracks StartScore/LastScore and Flow (sum of abs score changes).
        /// </summary>
        private void UpdateDailyStats(Player player)
        {
            try
            {
                if (player == null || string.IsNullOrEmpty(player.WangWangId)) return;
                var dateKey = DateTime.Today.ToString("yyyy-MM-dd");

                var startKey = DailyKey(dateKey, player.WangWangId, "StartScore");
                var lastKey = DailyKey(dateKey, player.WangWangId, "LastScore");
                var flowKey = DailyKey(dateKey, player.WangWangId, "Flow");

                var startStr = GetSetting(startKey, "");
                if (string.IsNullOrEmpty(startStr))
                    SaveSetting(startKey, player.Score.ToString(CultureInfo.InvariantCulture));

                var lastStr = GetSetting(lastKey, "");
                if (string.IsNullOrEmpty(lastStr))
                {
                    SaveSetting(lastKey, player.Score.ToString(CultureInfo.InvariantCulture));
                    SaveSetting(flowKey, "0");
                    return;
                }

                if (!decimal.TryParse(lastStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var lastScore))
                    decimal.TryParse(lastStr, out lastScore);

                var delta = player.Score - lastScore;
                var abs = delta < 0 ? -delta : delta;

                var flowStr = GetSetting(flowKey, "0");
                if (!decimal.TryParse(flowStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var flow))
                    decimal.TryParse(flowStr, out flow);

                flow += abs;
                SaveSetting(flowKey, flow.ToString(CultureInfo.InvariantCulture));
                SaveSetting(lastKey, player.Score.ToString(CultureInfo.InvariantCulture));
            }
            catch { }
        }

        /// <summary>
        /// Lottery history file path for the given day (used by [开奖历史]).
        /// </summary>
        public string GetLotteryHistoryFile(DateTime day)
        {
            return Path.Combine(DatabaseDir, $"开奖历史-{day:yyyy-MM-dd}.txt");
        }

        /// <summary>
        /// Append one line to daily lottery history and increment daily period count.
        /// </summary>
        public void AppendLotteryHistory(DateTime day, string line)
        {
            try
            {
                Directory.CreateDirectory(DatabaseDir);
                var file = GetLotteryHistoryFile(day);
                File.AppendAllText(file, line + Environment.NewLine, Encoding.UTF8);

                var dateKey = day.ToString("yyyy-MM-dd");
                var count = GetDailyInt(dateKey, key: "LotteryPeriodCount", defaultValue: 0);
                SetDailyInt(dateKey, key: "LotteryPeriodCount", value: count + 1);
            }
            catch { }
        }
        
        #endregion
        
        #region Message Log (收发日志)
        
        /// <summary>
        /// Log sent message (记录发送的消息)
        /// </summary>
        public void LogSentMessage(string toId, string toName, string content, string msgType = "text")
        {
            try
            {
                var logEntry = new MessageLogEntry
                {
                    Time = DateTime.Now,
                    Direction = "发送",
                    ContactId = toId,
                    ContactName = toName,
                    Content = content,
                    MessageType = msgType
                };
                WriteMessageLog(logEntry);
            }
            catch (Exception ex)
            {
                Logger.Error($"记录发送消息失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Log received message (记录接收的消息)
        /// </summary>
        public void LogReceivedMessage(string fromId, string fromName, string content, string msgType = "text")
        {
            try
            {
                var logEntry = new MessageLogEntry
                {
                    Time = DateTime.Now,
                    Direction = "接收",
                    ContactId = fromId,
                    ContactName = fromName,
                    Content = content,
                    MessageType = msgType
                };
                WriteMessageLog(logEntry);
            }
            catch (Exception ex)
            {
                Logger.Error($"记录接收消息失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Write message log entry to file
        /// </summary>
        private void WriteMessageLog(MessageLogEntry entry)
        {
            lock (_msgLogLock)
            {
                try
                {
                    // Create daily log file
                    var fileName = $"{DateTime.Now:yyyy-MM-dd}.txt";
                    var filePath = Path.Combine(MessageLogDir, fileName);
                    
                    // Format log line
                    var logLine = $"[{entry.Time:HH:mm:ss}] [{entry.Direction}] " +
                                  $"[{entry.ContactName}({entry.ContactId})] " +
                                  $"[{entry.MessageType}] {entry.Content}";
                    
                    // Append to file
                    File.AppendAllText(filePath, logLine + Environment.NewLine, Encoding.UTF8);
                }
                catch { }
            }
        }
        
        /// <summary>
        /// Get message logs for a specific date
        /// </summary>
        public List<MessageLogEntry> GetMessageLogs(DateTime date, string contactId = null)
        {
            var list = new List<MessageLogEntry>();
            try
            {
                var fileName = $"{date:yyyy-MM-dd}.txt";
                var filePath = Path.Combine(MessageLogDir, fileName);
                
                if (File.Exists(filePath))
                {
                    var lines = File.ReadAllLines(filePath, Encoding.UTF8);
                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        
                        var entry = ParseMessageLogLine(line);
                        if (entry != null)
                        {
                            // Filter by contactId if specified
                            if (string.IsNullOrEmpty(contactId) || entry.ContactId == contactId)
                            {
                                list.Add(entry);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"读取消息日志失败: {ex.Message}");
            }
            return list;
        }
        
        /// <summary>
        /// Parse a message log line
        /// </summary>
        private MessageLogEntry ParseMessageLogLine(string line)
        {
            try
            {
                // Format: [HH:mm:ss] [Direction] [Name(Id)] [Type] Content
                var entry = new MessageLogEntry();
                
                // Extract time
                var timeMatch = System.Text.RegularExpressions.Regex.Match(line, @"\[(\d{2}:\d{2}:\d{2})\]");
                if (timeMatch.Success)
                {
                    var timePart = timeMatch.Groups[1].Value;
                    entry.Time = DateTime.Today.Add(TimeSpan.Parse(timePart));
                }
                
                // Extract direction
                if (line.Contains("[发送]"))
                    entry.Direction = "发送";
                else if (line.Contains("[接收]"))
                    entry.Direction = "接收";
                
                // Extract contact info
                var contactMatch = System.Text.RegularExpressions.Regex.Match(line, @"\[([^\]]+)\(([^\)]+)\)\]");
                if (contactMatch.Success)
                {
                    entry.ContactName = contactMatch.Groups[1].Value;
                    entry.ContactId = contactMatch.Groups[2].Value;
                }
                
                // Extract message type
                var typeMatch = System.Text.RegularExpressions.Regex.Match(line, @"\]\s*\[(\w+)\]\s*");
                if (typeMatch.Success)
                {
                    entry.MessageType = typeMatch.Groups[1].Value;
                }
                
                // Extract content (everything after the last ])
                var lastBracket = line.LastIndexOf(']');
                if (lastBracket > 0 && lastBracket < line.Length - 1)
                {
                    entry.Content = line.Substring(lastBracket + 1).Trim();
                }
                
                return entry;
            }
            catch
            {
                return null;
            }
        }
        
        /// <summary>
        /// Get today's message count
        /// </summary>
        public (int sent, int received) GetTodayMessageCount()
        {
            int sent = 0, received = 0;
            try
            {
                var logs = GetMessageLogs(DateTime.Today);
                foreach (var log in logs)
                {
                    if (log.Direction == "发送") sent++;
                    else if (log.Direction == "接收") received++;
                }
            }
            catch { }
            return (sent, received);
        }
        
        /// <summary>
        /// Get available log dates
        /// </summary>
        public List<DateTime> GetAvailableLogDates()
        {
            var dates = new List<DateTime>();
            try
            {
                var files = Directory.GetFiles(MessageLogDir, "*.txt");
                foreach (var file in files)
                {
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    if (DateTime.TryParse(fileName, out var date))
                    {
                        dates.Add(date);
                    }
                }
                dates.Sort((a, b) => b.CompareTo(a)); // Newest first
            }
            catch { }
            return dates;
        }
        
        #endregion
        
        #region Score Log (上下分日志)
        
        /// <summary>
        /// Log score change for a player
        /// </summary>
        public void LogScoreChange(DateTime date, string wangWangId, decimal amount, string reason)
        {
            try
            {
                var fileName = $"上下分-{date:yyyy-MM-dd}.txt";
                var filePath = Path.Combine(DatabaseDir, fileName);
                var logLine = $"{DateTime.Now:HH:mm:ss}|{wangWangId}|{amount}|{reason}";
                File.AppendAllText(filePath, logLine + Environment.NewLine, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Logger.Error($"记录上下分日志失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Get score change logs for a player on a specific date
        /// </summary>
        public List<ScoreLogEntry> GetScoreLogs(DateTime date, string wangWangId)
        {
            var list = new List<ScoreLogEntry>();
            try
            {
                var fileName = $"上下分-{date:yyyy-MM-dd}.txt";
                var filePath = Path.Combine(DatabaseDir, fileName);
                if (!File.Exists(filePath)) return list;
                
                var lines = File.ReadAllLines(filePath, Encoding.UTF8);
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var parts = line.Split('|');
                    if (parts.Length >= 3 && parts[1] == wangWangId)
                    {
                        var entry = new ScoreLogEntry
                        {
                            WangWangId = parts[1],
                            Reason = parts.Length > 3 ? parts[3] : ""
                        };
                        if (TimeSpan.TryParse(parts[0], out var time))
                            entry.Time = date.Add(time);
                        if (decimal.TryParse(parts[2], NumberStyles.Any, CultureInfo.InvariantCulture, out var amt))
                            entry.Amount = amt;
                        list.Add(entry);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"读取上下分日志失败: {ex.Message}");
            }
            return list;
        }
        
        #endregion
        
        #region At Score Log (艾特分日志)
        
        /// <summary>
        /// Log @ score change for a player
        /// </summary>
        public void LogAtScoreChange(DateTime date, string wangWangId, decimal amount)
        {
            try
            {
                var fileName = $"艾特分-{date:yyyy-MM-dd}.txt";
                var filePath = Path.Combine(DatabaseDir, fileName);
                var logLine = $"{DateTime.Now:HH:mm:ss}|{wangWangId}|{amount}";
                File.AppendAllText(filePath, logLine + Environment.NewLine, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Logger.Error($"记录艾特分日志失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Get @ score change logs for a player on a specific date
        /// </summary>
        public List<ScoreLogEntry> GetAtScoreLogs(DateTime date, string wangWangId)
        {
            var list = new List<ScoreLogEntry>();
            try
            {
                var fileName = $"艾特分-{date:yyyy-MM-dd}.txt";
                var filePath = Path.Combine(DatabaseDir, fileName);
                if (!File.Exists(filePath)) return list;
                
                var lines = File.ReadAllLines(filePath, Encoding.UTF8);
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var parts = line.Split('|');
                    if (parts.Length >= 3 && parts[1] == wangWangId)
                    {
                        var entry = new ScoreLogEntry { WangWangId = parts[1] };
                        if (TimeSpan.TryParse(parts[0], out var time))
                            entry.Time = date.Add(time);
                        if (decimal.TryParse(parts[2], NumberStyles.Any, CultureInfo.InvariantCulture, out var amt))
                            entry.Amount = amt;
                        list.Add(entry);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"读取艾特分日志失败: {ex.Message}");
            }
            return list;
        }
        
        #endregion
        
        #region Invitation (邀请记录)
        
        /// <summary>
        /// Get list of players invited by a specific player
        /// </summary>
        public List<string> GetInvitedBy(string inviterId)
        {
            var list = new List<string>();
            try
            {
                var filePath = Path.Combine(DatabaseDir, "邀请记录.txt");
                if (!File.Exists(filePath)) return list;
                
                var lines = File.ReadAllLines(filePath, Encoding.UTF8);
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var parts = line.Split('|');
                    // Format: 被邀请人|邀请人|时间
                    if (parts.Length >= 2 && parts[1] == inviterId)
                    {
                        list.Add(parts[0]);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"读取邀请记录失败: {ex.Message}");
            }
            return list;
        }
        
        /// <summary>
        /// Get the inviter of a specific player
        /// </summary>
        public string GetInviter(string inviteeId)
        {
            try
            {
                var filePath = Path.Combine(DatabaseDir, "邀请记录.txt");
                if (!File.Exists(filePath)) return null;
                
                var lines = File.ReadAllLines(filePath, Encoding.UTF8);
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var parts = line.Split('|');
                    // Format: 被邀请人|邀请人|时间
                    if (parts.Length >= 2 && parts[0] == inviteeId)
                    {
                        return parts[1];
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"读取邀请记录失败: {ex.Message}");
            }
            return null;
        }
        
        /// <summary>
        /// Record an invitation
        /// </summary>
        public void RecordInvitation(string inviteeId, string inviterId)
        {
            try
            {
                var filePath = Path.Combine(DatabaseDir, "邀请记录.txt");
                var logLine = $"{inviteeId}|{inviterId}|{DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                File.AppendAllText(filePath, logLine + Environment.NewLine, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Logger.Error($"记录邀请失败: {ex.Message}");
            }
        }
        
        #endregion
    }
    
    /// <summary>
    /// Score log entry model (上下分日志条目)
    /// </summary>
    public class ScoreLogEntry
    {
        public DateTime Time { get; set; }
        public string WangWangId { get; set; }
        public decimal Amount { get; set; }
        public string Reason { get; set; }
    }
    
    /// <summary>
    /// Message log entry model (消息日志条目)
    /// </summary>
    public class MessageLogEntry
    {
        public DateTime Time { get; set; }
        public string Direction { get; set; } // 发送/接收
        public string ContactId { get; set; }
        public string ContactName { get; set; }
        public string Content { get; set; }
        public string MessageType { get; set; } // text/image/file
    }
}
