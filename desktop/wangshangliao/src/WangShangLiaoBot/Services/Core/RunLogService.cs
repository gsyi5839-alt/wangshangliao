using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using WangShangLiaoBot.Models;
using WangShangLiaoBot.Utils;

namespace WangShangLiaoBot.Services
{
    /// <summary>
    /// 运行日志服务 - 记录我方机器人运行日志
    /// 包括：开奖通知、封盘/开盘、下注确认、结算账单、规则公告等
    /// 使用消息分类系统记录所有机器人操作
    /// </summary>
    public sealed class RunLogService
    {
        private static RunLogService _instance;
        private static readonly object _lock = new object();
        
        public static RunLogService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                            _instance = new RunLogService();
                    }
                }
                return _instance;
            }
        }
        
        // Log entries cache (keep last 1000 entries in memory)
        private readonly List<RunLogEntry> _entries = new List<RunLogEntry>();
        private readonly object _entriesLock = new object();
        private const int MaxEntriesInMemory = 1000;
        
        // Auto-increment ID
        private int _nextId = 1;
        
        // Current period number
        private string _currentPeriod = "";
        
        // Running state
        public bool IsRunning { get; private set; }
        
        // Log file path
        private string _logFilePath;
        
        // Event for new log entry
        public event Action<RunLogEntry> OnNewEntry;
        
        // Statistics for competitor analysis
        private int _sealCount = 0;      // 封盘次数
        private int _unsealCount = 0;    // 开盘次数
        private int _betCount = 0;       // 下注次数
        private int _lotteryCount = 0;   // 开奖次数
        
        private RunLogService()
        {
            var logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "运行日志");
            if (!Directory.Exists(logDir))
                Directory.CreateDirectory(logDir);
                
            var today = DateTime.Now.ToString("yyyy-MM-dd");
            _logFilePath = Path.Combine(logDir, $"{today}.log");
            
            // Load existing entries count
            LoadExistingCount();
        }
        
        /// <summary>
        /// Start the log service - subscribe to ChatService events
        /// </summary>
        public void Start()
        {
            if (IsRunning) return;
            
            ChatService.Instance.OnMessageReceived += HandleMessageReceived;
            ChatService.Instance.OnLog += HandleSystemLog;
            
            IsRunning = true;
            AddEntry(RunLogType.System, "日志服务已启动", "");
        }
        
        /// <summary>
        /// Stop the log service
        /// </summary>
        public void Stop()
        {
            if (!IsRunning) return;
            
            ChatService.Instance.OnMessageReceived -= HandleMessageReceived;
            ChatService.Instance.OnLog -= HandleSystemLog;
            
            // Log statistics
            var stats = $"统计: 封盘{_sealCount}次, 开盘{_unsealCount}次, 下注{_betCount}次, 开奖{_lotteryCount}次";
            AddEntry(RunLogType.System, $"日志服务已停止 | {stats}", "");
            IsRunning = false;
        }
        
        /// <summary>
        /// Handle incoming chat messages
        /// 记录我方机器人收到的消息，使用消息分类系统
        /// </summary>
        private void HandleMessageReceived(ChatMessage msg)
        {
            if (msg == null) return;
            
            var logType = msg.IsGroupMessage ? RunLogType.ReceiveGroup : RunLogType.ReceiveFriend;
            
            // 使用 MessageDecoder 分类消息类型（适用于我方机器人发送的消息）
            var messageType = MessageDecoder.ClassifyMessage(msg.Content);
            var features = MessageDecoder.AnalyzeMessage(msg.Content, msg.SenderName, 
                msg.Type == MessageType.Custom ? "custom" : "text");
            
            // 根据消息类型格式化显示
            string formattedContent;
            string period = _currentPeriod;
            
            switch (messageType)
            {
                case CompetitorMessageType.LotteryResult:
                    // 解析开奖结果
                    var lottery = MessageDecoder.ParseLotteryResult(msg.Content);
                    if (lottery != null)
                    {
                        formattedContent = $"[开奖] {lottery.Period}期 | 取餐码: {lottery.Number1}+{lottery.Number2}+{lottery.Number3}={lottery.Result}";
                        period = $"{lottery.Period}期";
                        _currentPeriod = period;
                        _lotteryCount++;
                    }
                    else
                    {
                        formattedContent = $"[开奖] {msg.Content}";
                    }
                    break;
                
                case CompetitorMessageType.AttackReply:
                    // 解析下注确认
                    var attack = MessageDecoder.ParseAttackReply(msg.Content);
                    if (attack != null)
                    {
                        var translated = MessageDecoder.TranslateGameplayCodes(attack.GameplayString);
                        formattedContent = $"[下注确认] {attack.PlayerName} | {translated} | ${attack.Amount}";
                        _betCount++;
                    }
                    else
                    {
                        formattedContent = $"[下注] {msg.Content}";
                    }
                    break;
                
                case CompetitorMessageType.MuteEnable:
                    formattedContent = "[封盘] 管理员开启了禁言";
                    _sealCount++;
                    logType = RunLogType.Seal;
                    break;
                
                case CompetitorMessageType.MuteDisable:
                    formattedContent = "[开盘] 管理员关闭了禁言";
                    _unsealCount++;
                    logType = RunLogType.Unseal;
                    break;
                
                case CompetitorMessageType.History:
                    var history = MessageDecoder.ParseHistory(msg.Content);
                    formattedContent = $"[历史] {string.Join(" ", history)}";
                    break;
                
                case CompetitorMessageType.Settlement:
                    var settlements = MessageDecoder.ParseSettlement(msg.Content);
                    if (settlements.Count > 0)
                    {
                        formattedContent = $"[结算] 共{settlements.Count}人 | 第一名: {settlements[0].PlayerName} ${settlements[0].Balance}";
                    }
                    else
                    {
                        formattedContent = "[结算] " + (msg.Content?.Length > 50 ? msg.Content.Substring(0, 50) + "..." : msg.Content);
                    }
                    break;
                
                case CompetitorMessageType.Rules:
                    formattedContent = "[规则] " + (msg.Content?.Length > 100 ? msg.Content.Substring(0, 100) + "..." : msg.Content);
                    break;
                
                case CompetitorMessageType.BalanceReply:
                    formattedContent = $"[余额查询] {msg.Content}";
                    break;
                
                case CompetitorMessageType.InsufficientBalance:
                    formattedContent = $"[余额不足] {msg.Content}";
                    break;
                
                default:
                    // 未分类消息 - 使用原始格式
                    if (msg.IsGroupMessage)
                    {
                        var botMarker = features.IsBot ? "🤖" : "";
                        var tags = features.GetTagsString();
                        formattedContent = $"(群{msg.GroupId}) {botMarker}{msg.SenderName}: {msg.Content}{tags}";
                    }
                    else
                    {
                        formattedContent = $"(私聊) {msg.SenderName}: {msg.Content}";
                        period = $"好友[{msg.SenderId}]";
                    }
                    break;
            }
            
            var entry = new RunLogEntry
            {
                Id = Interlocked.Increment(ref _nextId),
                Time = msg.Time,
                Period = period,
                LogType = logType,
                Message = formattedContent,
                GroupId = msg.GroupId,
                SenderId = msg.SenderId,
                SenderName = msg.SenderName,
                // 保留增强字段
                IsBot = features.IsBot,
                Tags = features.GetTagsString(),
                CompetitorType = messageType
            };
            
            AddEntryInternal(entry);
        }
        
        /// <summary>
        /// Handle system log messages from ChatService
        /// </summary>
        private void HandleSystemLog(string message)
        {
            if (string.IsNullOrEmpty(message)) return;
            
            // Determine log type based on message content
            var logType = RunLogType.System;
            
            // Skip DEBUG messages from being classified as send success/failed
            // [DEBUG] messages are internal CDP/system logs, not actual message sends
            var isDebugLog = message.StartsWith("[DEBUG]") || message.Contains("[DEBUG]");
            
            if (!isDebugLog && (message.Contains("发送") || message.Contains("投递")))
            {
                // Only classify as send success/failed if it's an actual message send log
                logType = message.Contains("成功") ? RunLogType.SendSuccess : RunLogType.SendFailed;
            }
            else if (message.Contains("Hook") || message.Contains("hook"))
            {
                logType = RunLogType.Hook;
            }
            else if (message.Contains("插件"))
            {
                logType = RunLogType.Plugin;
            }
            
            AddEntry(logType, message, "");
        }
        
        /// <summary>
        /// Log a seal event (封盘)
        /// </summary>
        public void LogSeal(string groupId)
        {
            _sealCount++;
            var entry = new RunLogEntry
            {
                Id = Interlocked.Increment(ref _nextId),
                Time = DateTime.Now,
                Period = _currentPeriod,
                LogType = RunLogType.Seal,
                Message = "[封盘] 系统封盘",
                GroupId = groupId,
                CompetitorType = CompetitorMessageType.MuteEnable
            };
            AddEntryInternal(entry);
        }
        
        /// <summary>
        /// Log an unseal event (开盘)
        /// </summary>
        public void LogUnseal(string groupId)
        {
            _unsealCount++;
            var entry = new RunLogEntry
            {
                Id = Interlocked.Increment(ref _nextId),
                Time = DateTime.Now,
                Period = _currentPeriod,
                LogType = RunLogType.Unseal,
                Message = "[开盘] 系统开盘",
                GroupId = groupId,
                CompetitorType = CompetitorMessageType.MuteDisable
            };
            AddEntryInternal(entry);
        }
        
        /// <summary>
        /// Log a lottery result (开奖)
        /// </summary>
        public void LogLottery(string period, int result, string detail = "")
        {
            _lotteryCount++;
            _currentPeriod = $"{period}期";
            
            var entry = new RunLogEntry
            {
                Id = Interlocked.Increment(ref _nextId),
                Time = DateTime.Now,
                Period = _currentPeriod,
                LogType = RunLogType.Lottery,
                Message = $"[开奖] {period}期 | 结果: {result:D2}" + (string.IsNullOrEmpty(detail) ? "" : $" | {detail}"),
                CompetitorType = CompetitorMessageType.LotteryResult
            };
            AddEntryInternal(entry);
        }
        
        /// <summary>
        /// Log a bet confirmation (下注确认)
        /// </summary>
        public void LogBetConfirm(string playerName, string gameplay, decimal amount, string groupId = "")
        {
            _betCount++;
            var translated = MessageDecoder.TranslateGameplayCodes(gameplay);
            
            var entry = new RunLogEntry
            {
                Id = Interlocked.Increment(ref _nextId),
                Time = DateTime.Now,
                Period = _currentPeriod,
                LogType = RunLogType.BetConfirm,
                Message = $"[下注确认] {playerName} | {translated} | ${amount}",
                GroupId = groupId,
                SenderName = playerName,
                CompetitorType = CompetitorMessageType.AttackReply
            };
            AddEntryInternal(entry);
        }
        
        /// <summary>
        /// Add a log entry for message send
        /// </summary>
        public void LogSend(string groupId, string message, bool success, string detail = "")
        {
            var logType = success ? RunLogType.SendSuccess : RunLogType.SendFailed;
            var shortMsg = message.Length > 50 ? message.Substring(0, 50) + "..." : message;
            var content = success 
                ? $"[发送成功] (群{groupId}) {shortMsg}"
                : $"[发送失败] (群{groupId}) {shortMsg}";
            if (!string.IsNullOrEmpty(detail))
                content += $" | {detail}";
                
            var entry = new RunLogEntry
            {
                Id = Interlocked.Increment(ref _nextId),
                Time = DateTime.Now,
                Period = _currentPeriod,
                LogType = logType,
                Message = content,
                GroupId = groupId
            };
            
            AddEntryInternal(entry);
        }
        
        /// <summary>
        /// Add a custom log entry
        /// </summary>
        public void AddEntry(RunLogType logType, string message, string groupId)
        {
            var entry = new RunLogEntry
            {
                Id = Interlocked.Increment(ref _nextId),
                Time = DateTime.Now,
                Period = _currentPeriod,
                LogType = logType,
                Message = message,
                GroupId = groupId
            };
            
            AddEntryInternal(entry);
        }
        
        /// <summary>
        /// Add entry to memory cache and file
        /// </summary>
        private void AddEntryInternal(RunLogEntry entry)
        {
            lock (_entriesLock)
            {
                _entries.Add(entry);
                
                // Keep only last N entries in memory
                while (_entries.Count > MaxEntriesInMemory)
                    _entries.RemoveAt(0);
            }
            
            // Write to file
            WriteToFile(entry);
            
            // Notify subscribers
            OnNewEntry?.Invoke(entry);
        }
        
        /// <summary>
        /// Write entry to log file
        /// Format: ID|Time|Period|Type|GroupId|SenderId|SenderName|IsBot|Tags|CompetitorType|Message
        /// </summary>
        private void WriteToFile(RunLogEntry entry)
        {
            try
            {
                // Check if date changed, create new file
                var today = DateTime.Now.ToString("yyyy-MM-dd");
                var expectedPath = Path.Combine(
                    Path.GetDirectoryName(_logFilePath), 
                    $"{today}.log");
                    
                if (_logFilePath != expectedPath)
                    _logFilePath = expectedPath;
                
                // 增强格式：包含消息类型分类
                var line = $"{entry.Id}|{entry.Time:yyyy-MM-dd HH:mm:ss}|{entry.Period}|" +
                          $"{(int)entry.LogType}|{entry.GroupId}|{entry.SenderId}|" +
                          $"{entry.SenderName}|{(entry.IsBot ? "1" : "0")}|{entry.Tags}|" +
                          $"{(int)entry.CompetitorType}|{entry.Message?.Replace("\n", "\\n")}";
                          
                File.AppendAllText(_logFilePath, line + Environment.NewLine, Encoding.UTF8);
            }
            catch { /* Ignore file write errors */ }
        }
        
        /// <summary>
        /// Load existing entry count from file
        /// </summary>
        private void LoadExistingCount()
        {
            try
            {
                if (File.Exists(_logFilePath))
                {
                    var lines = File.ReadAllLines(_logFilePath);
                    _nextId = lines.Length + 1;
                }
            }
            catch { }
        }
        
        /// <summary>
        /// Get all entries in memory
        /// </summary>
        public List<RunLogEntry> GetEntries()
        {
            lock (_entriesLock)
            {
                return new List<RunLogEntry>(_entries);
            }
        }
        
        /// <summary>
        /// Get entries filtered by message type
        /// </summary>
        public List<RunLogEntry> GetEntriesByType(CompetitorMessageType type)
        {
            lock (_entriesLock)
            {
                return _entries.FindAll(e => e.CompetitorType == type);
            }
        }
        
        /// <summary>
        /// Get entries from file for a specific date
        /// </summary>
        public List<RunLogEntry> GetEntriesFromFile(DateTime date)
        {
            var entries = new List<RunLogEntry>();
            var logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "运行日志");
            var filePath = Path.Combine(logDir, $"{date:yyyy-MM-dd}.log");
            
            if (!File.Exists(filePath))
                return entries;
                
            try
            {
                var lines = File.ReadAllLines(filePath);
                foreach (var line in lines)
                {
                    var parts = line.Split('|');
                    if (parts.Length >= 11)
                    {
                        // 增强格式（带消息类型）
                        entries.Add(new RunLogEntry
                        {
                            Id = int.TryParse(parts[0], out int id) ? id : 0,
                            Time = DateTime.TryParse(parts[1], out DateTime t) ? t : DateTime.Now,
                            Period = parts[2],
                            LogType = Enum.TryParse(parts[3], out RunLogType lt) ? lt : RunLogType.System,
                            GroupId = parts[4],
                            SenderId = parts[5],
                            SenderName = parts[6],
                            IsBot = parts[7] == "1",
                            Tags = parts[8],
                            CompetitorType = Enum.TryParse(parts[9], out CompetitorMessageType ct) ? ct : CompetitorMessageType.Unknown,
                            Message = parts[10].Replace("\\n", "\n")
                        });
                    }
                    else if (parts.Length >= 8)
                    {
                        // Old format (backwards compatible)
                        entries.Add(new RunLogEntry
                        {
                            Id = int.TryParse(parts[0], out int id) ? id : 0,
                            Time = DateTime.TryParse(parts[1], out DateTime t) ? t : DateTime.Now,
                            Period = parts[2],
                            LogType = Enum.TryParse(parts[3], out RunLogType lt) ? lt : RunLogType.System,
                            GroupId = parts[4],
                            SenderId = parts[5],
                            SenderName = parts[6],
                            Message = parts[7].Replace("\\n", "\n")
                        });
                    }
                }
            }
            catch { }
            
            return entries;
        }
        
        /// <summary>
        /// Get statistics
        /// </summary>
        public (int SealCount, int UnsealCount, int BetCount, int LotteryCount) GetStatistics()
        {
            return (_sealCount, _unsealCount, _betCount, _lotteryCount);
        }
        
        /// <summary>
        /// Update current period number
        /// </summary>
        public void SetCurrentPeriod(string period)
        {
            _currentPeriod = period ?? "";
        }
        
        /// <summary>
        /// Clear all entries
        /// </summary>
        public void Clear()
        {
            lock (_entriesLock)
            {
                _entries.Clear();
            }
            
            try
            {
                if (File.Exists(_logFilePath))
                    File.WriteAllText(_logFilePath, "");
            }
            catch { }
            
            _nextId = 1;
            _sealCount = 0;
            _unsealCount = 0;
            _betCount = 0;
            _lotteryCount = 0;
        }
        

    }
}
