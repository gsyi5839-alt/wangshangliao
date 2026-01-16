using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WangShangLiaoBot.Models;
using WangShangLiaoBot.Services.Betting;

namespace WangShangLiaoBot.Services
{
    /// <summary>
    /// Admin command service - handles all admin commands from chat messages
    /// Processes commands like: 开机/关机, 查询, 黑名单, 改名, 上下分, @艾特 etc.
    /// </summary>
    public sealed class AdminCommandService
    {
        private static AdminCommandService _instance;
        public static AdminCommandService Instance => _instance ?? (_instance = new AdminCommandService());

        /// <summary>Log event for UI display</summary>
        public event Action<string> OnLog;

        /// <summary>Bot running state (controlled by 开机/关机 commands)</summary>
        public bool IsBotRunning { get; private set; } = true;

        /// <summary>State changed event</summary>
        public event Action<bool> OnBotStateChanged;
        
        /// <summary>Event fired when a new pending score request is added (wangWangId, amount, reason, type)</summary>
        public event Action<string, decimal, string, string> OnPendingRequestAdded;

        #region Rate Limiting (命令频率限制)
        
        /// <summary>Track command timestamps per user (senderId -> timestamps)</summary>
        private readonly Dictionary<string, Queue<DateTime>> _commandHistory = new Dictionary<string, Queue<DateTime>>();
        private readonly object _rateLimitLock = new object();
        
        /// <summary>Max commands per time window</summary>
        private const int RateLimitMaxCommands = 10;
        
        /// <summary>Time window in seconds</summary>
        private const int RateLimitWindowSeconds = 60;
        
        /// <summary>Last cleanup time for memory leak prevention</summary>
        private DateTime _lastCleanupTime = DateTime.Now;
        
        /// <summary>Cleanup interval in minutes</summary>
        private const int CleanupIntervalMinutes = 10;
        
        /// <summary>
        /// Check if user exceeded rate limit
        /// </summary>
        private bool IsRateLimited(string senderId)
        {
            if (string.IsNullOrEmpty(senderId)) return false;
            
            lock (_rateLimitLock)
            {
                var now = DateTime.Now;
                var windowStart = now.AddSeconds(-RateLimitWindowSeconds);
                
                // Periodic cleanup to prevent memory leak from inactive users
                if ((now - _lastCleanupTime).TotalMinutes >= CleanupIntervalMinutes)
                {
                    CleanupInactiveUsers(windowStart);
                    _lastCleanupTime = now;
                }
                
                if (!_commandHistory.TryGetValue(senderId, out var history))
                {
                    history = new Queue<DateTime>();
                    _commandHistory[senderId] = history;
                }
                
                // Remove old entries outside the window
                while (history.Count > 0 && history.Peek() < windowStart)
                    history.Dequeue();
                
                // Check if exceeded limit
                if (history.Count >= RateLimitMaxCommands)
                {
                    Log($"用户 {senderId} 命令频率过高，已忽略");
                    return true;
                }
                
                // Record this command
                history.Enqueue(now);
                return false;
            }
        }
        
        /// <summary>
        /// Remove inactive users from command history to prevent memory leak
        /// </summary>
        private void CleanupInactiveUsers(DateTime windowStart)
        {
            var toRemove = new List<string>();
            foreach (var kvp in _commandHistory)
            {
                // Clean old entries
                while (kvp.Value.Count > 0 && kvp.Value.Peek() < windowStart)
                    kvp.Value.Dequeue();
                
                // Mark for removal if empty
                if (kvp.Value.Count == 0)
                    toRemove.Add(kvp.Key);
            }
            
            foreach (var key in toRemove)
                _commandHistory.Remove(key);
            
            if (toRemove.Count > 0)
                Logger.Debug($"[RateLimit] 清理了 {toRemove.Count} 个不活跃用户的记录");
        }
        
        #endregion
        
        private AdminCommandService() { }

        /// <summary>
        /// Process admin command from chat message
        /// Returns (handled, reply) - if handled is true, reply contains response message
        /// </summary>
        public async Task<(bool Handled, string Reply)> ProcessCommandAsync(ChatMessage msg)
        {
            if (msg == null || string.IsNullOrWhiteSpace(msg.Content)) 
                return (false, null);

            var content = msg.Content.Trim();
            var senderId = msg.SenderId ?? "";
            var senderName = msg.SenderName ?? "";
            var isGroup = msg.IsGroupMessage;
            var groupId = msg.GroupId ?? "";

            // Check if sender is admin
            var config = ConfigService.Instance.Config;
            // Support multiple separators: comma, pipe, @ (as shown in UI tip), and space
            var adminIds = (config.AdminWangWangId ?? "").Split(new[] { ',', '|', '@', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var isAdmin = adminIds.Any(id => id.Trim().Equals(senderId, StringComparison.OrdinalIgnoreCase));
            
            // ===== Rate limit check (防止恶意刷命令) =====
            if (IsRateLimited(senderId))
                return (false, null); // Silently ignore rate-limited commands

            // ===== 1. Start/Stop commands (admin only) =====
            if (isAdmin)
            {
                var startStopResult = ProcessStartStopCommand(content);
                if (startStopResult.Handled) return startStopResult;
            }

            // If bot is not running, ignore all other commands
            if (!IsBotRunning) return (false, null);

            // ===== 2. Query commands (today/yesterday data) =====
            var queryResult = await ProcessQueryCommandAsync(content, senderId);
            if (queryResult.Handled) return queryResult;
            
            // ===== 2.1 Profit query commands (期盈利/盈利) =====
            var profitResult = ProcessProfitQueryCommand(content);
            if (profitResult.Handled) return profitResult;

            // ===== 3. Blacklist commands (admin only) =====
            if (isAdmin)
            {
                var blacklistResult = ProcessBlacklistCommand(content);
                if (blacklistResult.Handled) return blacklistResult;
            }

            // ===== 4. Rename commands (admin only) =====
            if (isAdmin)
            {
                var renameResult = ProcessRenameCommand(content);
                if (renameResult.Handled) return renameResult;
            }

            // ===== 5. Score adjustment commands (admin only) =====
            if (isAdmin)
            {
                var scoreResult = ProcessScoreCommand(content);
                if (scoreResult.Handled) return scoreResult;
            }

            // ===== 6. Invitation query commands =====
            var inviteResult = ProcessInviteQueryCommand(content, senderId);
            if (inviteResult.Handled) return inviteResult;

            // ===== 7. @ mention commands (admin only, group only) =====
            if (isAdmin && isGroup)
            {
                var atResult = await ProcessAtCommandAsync(content, groupId);
                if (atResult.Handled) return atResult;
            }

            // ===== 8. Private chat score commands (admin only, private chat only) =====
            if (isAdmin && !isGroup)
            {
                var privateResult = ProcessPrivateScoreCommand(content, senderId);
                if (privateResult.Handled) return privateResult;
            }

            // ===== 9. Switch robot command (admin only) =====
            if (isAdmin)
            {
                var switchResult = ProcessSwitchRobotCommand(content);
                if (switchResult.Handled) return switchResult;
            }

            return (false, null);
        }

        #region 1. Start/Stop Commands (开机/关机)

        /// <summary>
        /// Process start/stop commands: 开机, 关机, 开始程序, 停止程序, etc.
        /// </summary>
        private (bool Handled, string Reply) ProcessStartStopCommand(string content)
        {
            // Start commands
            if (Regex.IsMatch(content, @"^(开机|开始程序|开始游戏)$"))
            {
                if (IsBotRunning)
                    return (true, "机器人已经在运行中");

                IsBotRunning = true;
                OnBotStateChanged?.Invoke(true);
                Log("管理员执行开机命令");
                return (true, "机器人已开机，服务启动中...");
            }

            // Stop commands
            if (Regex.IsMatch(content, @"^(关机|停止程序|停止游戏)$"))
            {
                if (!IsBotRunning)
                    return (true, "机器人已经停止");

                IsBotRunning = false;
                OnBotStateChanged?.Invoke(false);
                Log("管理员执行关机命令");
                return (true, "机器人已关机，服务已停止");
            }

            return (false, null);
        }

        #endregion

        #region 2. Query Commands (查询命令)

        /// <summary>
        /// Process query commands like: 今天下注=旺旺号, 昨天数据=旺旺号, etc.
        /// Supported types: 下注, 上下分, 艾特分, 统计, 百分比, 数据
        /// Also supports date range: 2020-02-17至2020-02-18[类型]=旺旺号
        /// </summary>
        private async Task<(bool Handled, string Reply)> ProcessQueryCommandAsync(string content, string defaultWangWangId)
        {
            // First check for date range pattern: 日期至日期[类型]=旺旺号
            var rangeMatch = Regex.Match(content, @"^(\d{4}-\d{2}-\d{2})(?:-(\d{1,2})-(\d{1,2}))?至(\d{4}-\d{2}-\d{2})(?:-(\d{1,2})-(\d{1,2}))?(下注|上下分|艾特分|统计|百分比|数据)?=?(\d+)?$");
            if (rangeMatch.Success)
            {
                return await ProcessDateRangeQueryAsync(rangeMatch, defaultWangWangId);
            }
            
            // Pattern: 今天[类型]=旺旺号 or 昨天[类型]=旺旺号 or 日期[类型]=旺旺号
            var queryMatch = Regex.Match(content, @"^(今天|昨天|\d{4}-\d{2}-\d{2})(下注|上下分|艾特分|统计|百分比|数据)?=?(\d+)?$");
            
            if (!queryMatch.Success) return (false, null);

            var dateStr = queryMatch.Groups[1].Value;
            var queryType = queryMatch.Groups[2].Success ? queryMatch.Groups[2].Value : "";
            var wangWangId = queryMatch.Groups[3].Success ? queryMatch.Groups[3].Value : defaultWangWangId;

            // Parse date
            DateTime queryDate;
            if (dateStr == "今天")
                queryDate = DateTime.Today;
            else if (dateStr == "昨天")
                queryDate = DateTime.Today.AddDays(-1);
            else if (!DateTime.TryParseExact(dateStr, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out queryDate))
                return (true, "日期格式错误，请使用 yyyy-MM-dd 格式");

            if (string.IsNullOrEmpty(wangWangId))
                return (true, "请提供旺旺号");

            var sb = new StringBuilder();
            var player = DataService.Instance.GetPlayer(wangWangId);
            var nickname = player?.Nickname ?? "玩家";
            sb.AppendLine($"【{dateStr}查询】{nickname}({wangWangId})");

            // Query based on type
            switch (queryType)
            {
                case "下注":
                    sb.AppendLine(await GetBetQueryAsync(queryDate, wangWangId));
                    break;
                case "上下分":
                    sb.AppendLine(GetScoreChangeQuery(queryDate, wangWangId));
                    break;
                case "艾特分":
                    sb.AppendLine(GetAtScoreQuery(queryDate, wangWangId));
                    break;
                case "统计":
                    sb.AppendLine(GetStatsQuery(queryDate, wangWangId));
                    break;
                case "百分比":
                    sb.AppendLine(GetPercentQuery(queryDate, wangWangId));
                    break;
                case "数据":
                default:
                    // Show all data
                    sb.AppendLine($"当前分数: {player?.Score ?? 0}");
                    sb.AppendLine($"留分: {player?.ReservedScore ?? 0}");
                    sb.AppendLine(GetStatsQuery(queryDate, wangWangId));
                    break;
            }

            return (true, sb.ToString().TrimEnd());
        }

        private Task<string> GetBetQueryAsync(DateTime date, string wangWangId)
        {
            try
            {
                var teamId = ConfigService.Instance.Config.GroupId ?? "";
                var period = LotteryService.Instance.CurrentPeriod ?? "";
                var bets = BetLedgerService.Instance.ReadBets(date, teamId, period)
                    .Where(b => b.PlayerId == wangWangId)
                    .ToList();

                if (bets.Count == 0) return Task.FromResult("暂无下注记录");

                var sb = new StringBuilder();
                sb.AppendLine($"下注记录 ({bets.Count}条):");
                foreach (var bet in bets.Take(10))
                {
                    sb.AppendLine($"  {bet.Period}: {bet.RawText} (金额:{bet.TotalAmount})");
                }
                if (bets.Count > 10) sb.AppendLine($"  ... 还有 {bets.Count - 10} 条");
                return Task.FromResult(sb.ToString());
            }
            catch
            {
                return Task.FromResult("查询下注记录失败");
            }
        }

        private string GetScoreChangeQuery(DateTime date, string wangWangId)
        {
            try
            {
                var logs = DataService.Instance.GetScoreLogs(date, wangWangId);
                if (logs == null || logs.Count == 0) return "暂无上下分记录";

                var sb = new StringBuilder();
                decimal totalUp = 0, totalDown = 0;
                foreach (var log in logs.Take(10))
                {
                    if (log.Amount > 0) totalUp += log.Amount;
                    else totalDown += Math.Abs(log.Amount);
                    sb.AppendLine($"  {log.Time:HH:mm} {(log.Amount >= 0 ? "+" : "")}{log.Amount} {log.Reason}");
                }
                sb.AppendLine($"总上分: +{totalUp}, 总下分: -{totalDown}");
                return sb.ToString();
            }
            catch
            {
                return "查询上下分记录失败";
            }
        }

        private string GetAtScoreQuery(DateTime date, string wangWangId)
        {
            try
            {
                var logs = DataService.Instance.GetAtScoreLogs(date, wangWangId);
                if (logs == null || logs.Count == 0) return "暂无艾特分记录";

                var sb = new StringBuilder();
                foreach (var log in logs.Take(10))
                {
                    sb.AppendLine($"  {log.Time:HH:mm} {(log.Amount >= 0 ? "+" : "")}{log.Amount}");
                }
                return sb.ToString();
            }
            catch
            {
                return "查询艾特分记录失败";
            }
        }

        private string GetStatsQuery(DateTime date, string wangWangId)
        {
            try
            {
                var dateKey = date.ToString("yyyy-MM-dd");
                var ds = DataService.Instance;
                var startScore = ds.GetDailyDecimal(dateKey, wangWangId, "StartScore", 0m);
                var flow = ds.GetDailyDecimal(dateKey, wangWangId, "Flow", 0m);
                var player = ds.GetPlayer(wangWangId);
                var currentScore = player?.Score ?? 0m;
                var profit = currentScore - startScore;

                return $"盈利: {profit}, 流水: {flow}";
            }
            catch
            {
                return "统计查询失败";
            }
        }

        private string GetPercentQuery(DateTime date, string wangWangId)
        {
            try
            {
                var dateKey = date.ToString("yyyy-MM-dd");
                var ds = DataService.Instance;
                var flow = ds.GetDailyDecimal(dateKey, wangWangId, "Flow", 0m);
                var startScore = ds.GetDailyDecimal(dateKey, wangWangId, "StartScore", 0m);
                var player = ds.GetPlayer(wangWangId);
                var currentScore = player?.Score ?? 0m;
                
                // BUG FIX: Calculate actual profit (current - start), not just current score
                var profit = currentScore - startScore;

                if (flow == 0) return "流水为0，无法计算百分比";
                var percent = (profit / flow) * 100m;
                return $"盈利: {profit}, 流水: {flow}, 百分比: {percent:F2}%";
            }
            catch
            {
                return "百分比查询失败";
            }
        }

        /// <summary>
        /// Process date range query: 2020-02-17至2020-02-18[类型]=旺旺号
        /// </summary>
        private async Task<(bool Handled, string Reply)> ProcessDateRangeQueryAsync(Match match, string defaultWangWangId)
        {
            try
            {
                var startDateStr = match.Groups[1].Value;
                var startHour = match.Groups[2].Success ? match.Groups[2].Value : "";
                var startMin = match.Groups[3].Success ? match.Groups[3].Value : "";
                var endDateStr = match.Groups[4].Value;
                var endHour = match.Groups[5].Success ? match.Groups[5].Value : "";
                var endMin = match.Groups[6].Success ? match.Groups[6].Value : "";
                var queryType = match.Groups[7].Success ? match.Groups[7].Value : "";
                var wangWangId = match.Groups[8].Success ? match.Groups[8].Value : defaultWangWangId;

                // Parse dates
                if (!DateTime.TryParseExact(startDateStr, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var startDate))
                    return (true, "起始日期格式错误");
                if (!DateTime.TryParseExact(endDateStr, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var endDate))
                    return (true, "结束日期格式错误");

                // Add time if specified
                if (!string.IsNullOrEmpty(startHour) && !string.IsNullOrEmpty(startMin))
                {
                    startDate = startDate.AddHours(int.Parse(startHour)).AddMinutes(int.Parse(startMin));
                }
                if (!string.IsNullOrEmpty(endHour) && !string.IsNullOrEmpty(endMin))
                {
                    endDate = endDate.AddHours(int.Parse(endHour)).AddMinutes(int.Parse(endMin));
                }
                else
                {
                    // Default to end of day
                    endDate = endDate.AddDays(1).AddSeconds(-1);
                }

                if (string.IsNullOrEmpty(wangWangId))
                    return (true, "请提供旺旺号");

                var sb = new StringBuilder();
                var player = DataService.Instance.GetPlayer(wangWangId);
                var nickname = player?.Nickname ?? "玩家";
                sb.AppendLine($"【{startDateStr} 至 {endDateStr} 查询】{nickname}({wangWangId})");

                // Aggregate data across date range
                decimal totalFlow = 0m;
                decimal totalProfit = 0m;
                int betCount = 0;
                int scoreChangeCount = 0;

                for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
                {
                    var dateKey = date.ToString("yyyy-MM-dd");
                    var flow = DataService.Instance.GetDailyDecimal(dateKey, wangWangId, "Flow", 0m);
                    var startScore = DataService.Instance.GetDailyDecimal(dateKey, wangWangId, "StartScore", 0m);
                    var lastScore = DataService.Instance.GetDailyDecimal(dateKey, wangWangId, "LastScore", 0m);
                    
                    totalFlow += flow;
                    totalProfit += (lastScore - startScore);
                    
                    // Count bets
                    var teamId = ConfigService.Instance.Config.GroupId ?? "";
                    var bets = BetLedgerService.Instance.ReadBets(date, teamId, null)
                        .Where(b => b.PlayerId == wangWangId)
                        .ToList();
                    betCount += bets.Count;
                    
                    // Count score changes
                    var logs = DataService.Instance.GetScoreLogs(date, wangWangId);
                    scoreChangeCount += logs?.Count ?? 0;
                }

                switch (queryType)
                {
                    case "下注":
                        sb.AppendLine($"下注次数: {betCount}");
                        break;
                    case "上下分":
                        sb.AppendLine($"上下分次数: {scoreChangeCount}");
                        break;
                    case "统计":
                    case "":
                    default:
                        sb.AppendLine($"总流水: {totalFlow}");
                        sb.AppendLine($"总盈亏: {(totalProfit >= 0 ? "+" : "")}{totalProfit}");
                        sb.AppendLine($"下注次数: {betCount}");
                        sb.AppendLine($"上下分次数: {scoreChangeCount}");
                        if (totalFlow > 0)
                        {
                            var percent = (totalProfit / totalFlow) * 100m;
                            sb.AppendLine($"盈利率: {percent:F2}%");
                        }
                        break;
                }

                return (true, sb.ToString().TrimEnd());
            }
            catch (Exception ex)
            {
                Logger.Error($"[AdminCmd] ProcessDateRangeQueryAsync error: {ex.Message}");
                return (true, "日期范围查询失败");
            }
        }

        #endregion

        #region 2.1 Profit Query Commands (期盈利/盈利)

        /// <summary>
        /// Process profit query commands: 今天期盈利, 昨天盈利, etc.
        /// </summary>
        private (bool Handled, string Reply) ProcessProfitQueryCommand(string content)
        {
            // Pattern: [日期]期盈利 or [日期]盈利
            var profitMatch = Regex.Match(content, @"^(今天|昨天|\d{4}-\d{2}-\d{2})(期盈利|盈利)$");
            if (!profitMatch.Success) return (false, null);

            var dateStr = profitMatch.Groups[1].Value;
            var queryType = profitMatch.Groups[2].Value;

            // Parse date
            DateTime queryDate;
            if (dateStr == "今天")
                queryDate = DateTime.Today;
            else if (dateStr == "昨天")
                queryDate = DateTime.Today.AddDays(-1);
            else if (!DateTime.TryParseExact(dateStr, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out queryDate))
                return (true, "日期格式错误，请使用 yyyy-MM-dd 格式");

            if (queryType == "期盈利")
            {
                // Query profit per period
                return (true, GetPeriodProfitQuery(queryDate));
            }
            else
            {
                // Query total profit for the day
                return (true, GetTotalProfitQuery(queryDate));
            }
        }

        /// <summary>
        /// Get period-by-period profit summary
        /// </summary>
        private string GetPeriodProfitQuery(DateTime date)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine($"【{date:yyyy-MM-dd} 期盈利统计】");
                
                // Get settlement files for the day
                var settlementDir = Path.Combine(DataService.Instance.DatabaseDir, "Settlements", date.ToString("yyyy-MM-dd"));
                if (!Directory.Exists(settlementDir))
                {
                    return $"{date:yyyy-MM-dd} 暂无结算记录";
                }
                
                var files = Directory.GetFiles(settlementDir, "settlement-*.txt");
                if (files.Length == 0)
                {
                    return $"{date:yyyy-MM-dd} 暂无结算记录";
                }
                
                decimal totalProfit = 0m;
                int periodCount = 0;
                
                foreach (var file in files.OrderBy(f => f))
                {
                    var filename = Path.GetFileNameWithoutExtension(file);
                    var period = filename.Replace("settlement-", "");
                    
                    // Read settlement file to calculate profit
                    var lines = File.ReadAllLines(file, Encoding.UTF8);
                    decimal periodProfit = 0m;
                    
                    foreach (var line in lines.Where(l => !string.IsNullOrWhiteSpace(l)))
                    {
                        // Format: playerId|nickname|stake|profit|final
                        var parts = line.Split('|');
                        if (parts.Length >= 4)
                        {
                            if (decimal.TryParse(parts[3], NumberStyles.Any, CultureInfo.InvariantCulture, out var profit))
                            {
                                periodProfit += profit;
                            }
                        }
                    }
                    
                    totalProfit += periodProfit;
                    periodCount++;
                    
                    if (periodCount <= 20) // Only show first 20 periods
                    {
                        sb.AppendLine($"  {period}: {(periodProfit >= 0 ? "+" : "")}{periodProfit}");
                    }
                }
                
                if (periodCount > 20)
                {
                    sb.AppendLine($"  ... 还有 {periodCount - 20} 期");
                }
                
                sb.AppendLine($"共 {periodCount} 期，总盈利: {(totalProfit >= 0 ? "+" : "")}{totalProfit}");
                return sb.ToString().TrimEnd();
            }
            catch (Exception ex)
            {
                Logger.Error($"[AdminCmd] GetPeriodProfitQuery error: {ex.Message}");
                return "查询期盈利失败";
            }
        }

        /// <summary>
        /// Get total profit for a day
        /// </summary>
        private string GetTotalProfitQuery(DateTime date)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine($"【{date:yyyy-MM-dd} 盈利统计】");
                
                // Calculate total profit from all players' daily stats
                var dateKey = date.ToString("yyyy-MM-dd");
                var playersDir = DataService.Instance.PlayerDataDir;
                
                if (!Directory.Exists(playersDir))
                {
                    return $"{date:yyyy-MM-dd} 暂无数据";
                }
                
                decimal totalFlow = 0m;
                decimal totalProfit = 0m;
                int playerCount = 0;
                
                var playerFiles = Directory.GetFiles(playersDir, "*.json");
                foreach (var file in playerFiles)
                {
                    try
                    {
                        var wangWangId = Path.GetFileNameWithoutExtension(file);
                        var startScore = DataService.Instance.GetDailyDecimal(dateKey, wangWangId, "StartScore", 0m);
                        var lastScore = DataService.Instance.GetDailyDecimal(dateKey, wangWangId, "LastScore", 0m);
                        var flow = DataService.Instance.GetDailyDecimal(dateKey, wangWangId, "Flow", 0m);
                        
                        if (flow > 0 || startScore != lastScore)
                        {
                            var profit = lastScore - startScore;
                            totalProfit += profit;
                            totalFlow += flow;
                            playerCount++;
                        }
                    }
                    catch { }
                }
                
                sb.AppendLine($"活跃玩家: {playerCount} 人");
                sb.AppendLine($"总流水: {totalFlow}");
                sb.AppendLine($"总盈利: {(totalProfit >= 0 ? "+" : "")}{totalProfit}");
                
                if (totalFlow > 0)
                {
                    var percent = (totalProfit / totalFlow) * 100m;
                    sb.AppendLine($"盈利率: {percent:F2}%");
                }
                
                return sb.ToString().TrimEnd();
            }
            catch (Exception ex)
            {
                Logger.Error($"[AdminCmd] GetTotalProfitQuery error: {ex.Message}");
                return "查询盈利失败";
            }
        }

        #endregion

        #region 3. Blacklist Commands (黑名单)

        /// <summary>
        /// Process blacklist commands: 加黑名单旺旺号, 减黑名单旺旺号
        /// </summary>
        private (bool Handled, string Reply) ProcessBlacklistCommand(string content)
        {
            // Add to blacklist
            var addMatch = Regex.Match(content, @"^加黑名单(\d+)$");
            if (addMatch.Success)
            {
                var wangWangId = addMatch.Groups[1].Value;
                var config = ConfigService.Instance.Config;
                if (!config.Blacklist.Contains(wangWangId))
                {
                    config.Blacklist.Add(wangWangId);
                    ConfigService.Instance.SaveConfig();
                    Log($"添加黑名单: {wangWangId}");
                    return (true, $"已将 {wangWangId} 加入黑名单");
                }
                return (true, $"{wangWangId} 已在黑名单中");
            }

            // Remove from blacklist
            var removeMatch = Regex.Match(content, @"^减黑名单(\d+)$");
            if (removeMatch.Success)
            {
                var wangWangId = removeMatch.Groups[1].Value;
                var config = ConfigService.Instance.Config;
                if (config.Blacklist.Contains(wangWangId))
                {
                    config.Blacklist.Remove(wangWangId);
                    ConfigService.Instance.SaveConfig();
                    Log($"移除黑名单: {wangWangId}");
                    return (true, $"已将 {wangWangId} 从黑名单移除");
                }
                return (true, $"{wangWangId} 不在黑名单中");
            }

            return (false, null);
        }

        #endregion

        #region 4. Rename Commands (改名)

        /// <summary>
        /// Process rename commands: 改名旺旺号=新名片, 旺旺号改名=新名片
        /// </summary>
        private (bool Handled, string Reply) ProcessRenameCommand(string content)
        {
            // Pattern 1: 改名旺旺号=新名片
            var match1 = Regex.Match(content, @"^改名(\d+)=(.+)$");
            if (match1.Success)
            {
                var wangWangId = match1.Groups[1].Value;
                var newName = match1.Groups[2].Value.Trim();
                return DoRename(wangWangId, newName);
            }

            // Pattern 2: 旺旺号改名=新名片
            var match2 = Regex.Match(content, @"^(\d+)改名=(.+)$");
            if (match2.Success)
            {
                var wangWangId = match2.Groups[1].Value;
                var newName = match2.Groups[2].Value.Trim();
                return DoRename(wangWangId, newName);
            }

            return (false, null);
        }

        private (bool Handled, string Reply) DoRename(string wangWangId, string newName)
        {
            if (string.IsNullOrWhiteSpace(newName))
                return (true, "新名片不能为空");

            var player = DataService.Instance.GetPlayer(wangWangId);
            if (player == null)
            {
                // Create new player with the name
                player = DataService.Instance.GetOrCreatePlayer(wangWangId, newName);
            }
            else
            {
                var oldName = player.Nickname;
                player.Nickname = newName;
                DataService.Instance.SavePlayer(player);
                Log($"改名: {wangWangId} [{oldName}] -> [{newName}]");
            }

            return (true, $"已将 {wangWangId} 改名为: {newName}");
        }

        #endregion

        #region 5. Score Adjustment Commands (上下分)

        /// <summary>
        /// Process score commands: 旺旺号+分数理由, 旺旺号-分数理由, 旺旺号=分数理由
        /// Examples: 123456+100红包, 123456-50回水, 123456=500初始
        /// </summary>
        private (bool Handled, string Reply) ProcessScoreCommand(string content)
        {
            // Pattern: 旺旺号 [+-=] 分数 [理由]
            var match = Regex.Match(content, @"^(\d{4,})([+\-=])(\d+(?:\.\d+)?)\s*(.*)$");
            if (!match.Success) return (false, null);

            var wangWangId = match.Groups[1].Value;
            var op = match.Groups[2].Value;
            var amountStr = match.Groups[3].Value;
            var reason = match.Groups[4].Value.Trim();

            if (!decimal.TryParse(amountStr, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
                return (true, "分数格式错误");

            var player = DataService.Instance.GetOrCreatePlayer(wangWangId, wangWangId);
            var oldScore = player.Score;
            decimal newScore;
            string opName;
            string internalReplyTemplate = null;
            var config = ConfigService.Instance.Config;

            switch (op)
            {
                case "+":
                    newScore = oldScore + amount;
                    opName = "上分";
                    // 攻击上分有效 - 内部回复
                    internalReplyTemplate = config.InternalAttackValid;
                    break;
                case "-":
                    newScore = oldScore - amount;
                    opName = "下分";
                    // 下分处理中 - 内部回复
                    internalReplyTemplate = config.InternalDownProcessing;
                    amount = -amount; // For logging
                    break;
                case "=":
                    newScore = amount;
                    opName = "设分";
                    amount = newScore - oldScore; // Difference for logging
                    break;
                default:
                    return (false, null);
            }

            player.Score = newScore;
            DataService.Instance.SavePlayer(player);

            // Log the score change
            DataService.Instance.LogScoreChange(DateTime.Today, wangWangId, amount, reason);
            Log($"{opName}: {wangWangId} {oldScore} -> {newScore} ({reason})");
            
            // 上分后自动处理之前下注 (AutoProcessBeforeScore)
            if (op == "+" && BetProcessSettingsService.Instance.AutoProcessBeforeScore)
            {
                _ = ProcessPreviousBetsAfterScoreUpAsync(wangWangId, player);
            }

            // Use internal reply template if configured
            if (!string.IsNullOrEmpty(internalReplyTemplate))
            {
                var renderedReply = TemplateEngine.Render(internalReplyTemplate, new TemplateEngine.RenderContext
                {
                    Player = player,
                    Today = DateTime.Today
                });
                if (!string.IsNullOrEmpty(renderedReply))
                    return (true, renderedReply);
            }

            // Default reply
            var nickname = player.Nickname ?? "玩家";
            return (true, $"{nickname}({wangWangId}) {opName}成功\n{oldScore} -> {newScore}\n理由: {(string.IsNullOrEmpty(reason) ? "无" : reason)}");
        }

        #endregion

        #region 6. Invitation Query Commands (查询邀请)

        /// <summary>
        /// Process invitation query commands: 查询邀请旺旺号, 查询被邀请旺旺号
        /// </summary>
        private (bool Handled, string Reply) ProcessInviteQueryCommand(string content, string defaultWangWangId)
        {
            // Query who this person invited
            var inviteMatch = Regex.Match(content, @"^查询邀请(\d+)?$");
            if (inviteMatch.Success)
            {
                var wangWangId = inviteMatch.Groups[1].Success ? inviteMatch.Groups[1].Value : defaultWangWangId;
                if (string.IsNullOrEmpty(wangWangId))
                    return (true, "请提供旺旺号");

                var invited = DataService.Instance.GetInvitedBy(wangWangId);
                if (invited == null || invited.Count == 0)
                    return (true, $"{wangWangId} 暂无邀请记录");

                var sb = new StringBuilder();
                sb.AppendLine($"【{wangWangId} 邀请的人】共 {invited.Count} 人:");
                foreach (var id in invited.Take(20))
                {
                    var p = DataService.Instance.GetPlayer(id);
                    sb.AppendLine($"  {p?.Nickname ?? "玩家"}({id})");
                }
                if (invited.Count > 20) sb.AppendLine($"  ... 还有 {invited.Count - 20} 人");
                return (true, sb.ToString().TrimEnd());
            }

            // Query who invited this person
            var invitedByMatch = Regex.Match(content, @"^查询被邀请(\d+)?$");
            if (invitedByMatch.Success)
            {
                var wangWangId = invitedByMatch.Groups[1].Success ? invitedByMatch.Groups[1].Value : defaultWangWangId;
                if (string.IsNullOrEmpty(wangWangId))
                    return (true, "请提供旺旺号");

                var inviter = DataService.Instance.GetInviter(wangWangId);
                if (string.IsNullOrEmpty(inviter))
                    return (true, $"{wangWangId} 没有被邀请记录");

                var p = DataService.Instance.GetPlayer(inviter);
                return (true, $"{wangWangId} 被 {p?.Nickname ?? "玩家"}({inviter}) 邀请");
            }

            return (false, null);
        }

        #endregion

        #region 7. @ Mention Commands (艾特命令)

        /// <summary>
        /// Process @ mention commands from group chat
        /// Patterns:
        /// - @旺旺 (equals sending "1" for that player)
        /// - @旺旺1000到/@旺旺1000查 (score arrival/check)
        /// - @旺旺+分数 (score adjustment)
        /// - @旺旺改名=新名片 / 改名@旺旺=新名片
        /// - @旺旺加黑名单 / @旺旺减黑名单
        /// - @旺旺踢 (kick from group)
        /// - @旺旺禁言 / @旺旺解禁
        /// </summary>
        private async Task<(bool Handled, string Reply)> ProcessAtCommandAsync(string content, string groupId)
        {
            // Extract @ mentions - format could be @昵称 or @旺旺号
            // We'll parse the content for common patterns

            // Pattern: @xxx+分数 (score adjustment via @)
            var atScoreMatch = Regex.Match(content, @"@(\d+)([+\-])(\d+(?:\.\d+)?)");
            if (atScoreMatch.Success)
            {
                var wangWangId = atScoreMatch.Groups[1].Value;
                var op = atScoreMatch.Groups[2].Value;
                var amountStr = atScoreMatch.Groups[3].Value;

                if (decimal.TryParse(amountStr, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
                {
                    var player = DataService.Instance.GetOrCreatePlayer(wangWangId, wangWangId);
                    var oldScore = player.Score;
                    var config = ConfigService.Instance.Config;
                    string internalReplyTemplate = null;
                    
                    if (op == "-")
                    {
                        amount = -amount;
                        // 下分处理中 - 内部回复
                        internalReplyTemplate = config.InternalDownProcessing;
                    }
                    else
                    {
                        // 攻击上分有效 - 内部回复
                        internalReplyTemplate = config.InternalAttackValid;
                    }
                    
                    player.Score += amount;
                    DataService.Instance.SavePlayer(player);
                    DataService.Instance.LogAtScoreChange(DateTime.Today, wangWangId, amount);
                    
                    Log($"艾特上下分: {wangWangId} {oldScore} -> {player.Score}");
                    
                    // Use internal reply template if configured
                    if (!string.IsNullOrEmpty(internalReplyTemplate))
                    {
                        var renderedReply = TemplateEngine.Render(internalReplyTemplate, new TemplateEngine.RenderContext
                        {
                            Player = player,
                            Today = DateTime.Today
                        });
                        if (!string.IsNullOrEmpty(renderedReply))
                            return (true, renderedReply);
                    }
                    
                    return (true, $"@{player.Nickname ?? wangWangId} 分数: {oldScore} -> {player.Score}");
                }
            }

            // Pattern: @旺旺号 金额到 or @旺旺号 金额查 (score arrived/check)
            // Supports formats: @123456 1000到, @1234561000到 (assumes 4-6 digit wangwang, rest is amount)
            var atArriveMatch = Regex.Match(content, @"@(\d{4,6})\s*(\d+)(到|查)");
            if (atArriveMatch.Success)
            {
                var wangWangId = atArriveMatch.Groups[1].Value;
                var amountStr = atArriveMatch.Groups[2].Value;
                var action = atArriveMatch.Groups[3].Value;

                if (decimal.TryParse(amountStr, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
                {
                    var player = DataService.Instance.GetOrCreatePlayer(wangWangId, wangWangId);
                    
                    if (action == "到")
                    {
                        // Score arrived
                        player.Score += amount;
                        DataService.Instance.SavePlayer(player);
                        DataService.Instance.LogScoreChange(DateTime.Today, wangWangId, amount, "艾特到");
                        Log($"艾特分数到: {wangWangId} +{amount}");
                        return (true, $"@{player.Nickname ?? wangWangId} {amount}到\n当前分数: {player.Score}");
                    }
                    else // 查
                    {
                        return (true, $"@{player.Nickname ?? wangWangId} 当前分数: {player.Score}");
                    }
                }
            }

            // Pattern: @xxx改名=新名片 or 改名@xxx=新名片
            var atRenameMatch = Regex.Match(content, @"(?:@(\d+)改名=(.+)|改名@(\d+)=(.+))");
            if (atRenameMatch.Success)
            {
                var wangWangId = atRenameMatch.Groups[1].Success ? atRenameMatch.Groups[1].Value : atRenameMatch.Groups[3].Value;
                var newName = atRenameMatch.Groups[2].Success ? atRenameMatch.Groups[2].Value : atRenameMatch.Groups[4].Value;
                return DoRename(wangWangId, newName.Trim());
            }

            // Pattern: @xxx加黑名单 or @xxx减黑名单
            var atBlacklistMatch = Regex.Match(content, @"@(\d+)(加|减)黑名单");
            if (atBlacklistMatch.Success)
            {
                var wangWangId = atBlacklistMatch.Groups[1].Value;
                var action = atBlacklistMatch.Groups[2].Value;
                var cmd = action == "加" ? $"加黑名单{wangWangId}" : $"减黑名单{wangWangId}";
                return ProcessBlacklistCommand(cmd);
            }

            // Pattern: @xxx踢 (kick from group)
            var atKickMatch = Regex.Match(content, @"@(\d+)踢");
            if (atKickMatch.Success)
            {
                var wangWangId = atKickMatch.Groups[1].Value;
                
                if (string.IsNullOrEmpty(groupId))
                    return (true, "无法获取群ID，踢人失败");
                
                var result = await ChatService.Instance.KickTeamMemberAsync(groupId, wangWangId);
                Log($"踢出群成员: {wangWangId}, 结果: {result.Message}");
                
                if (result.Success)
                {
                    var player = DataService.Instance.GetPlayer(wangWangId);
                    return (true, $"已将 {player?.Nickname ?? wangWangId}({wangWangId}) 踢出群聊");
                }
                return (true, $"踢人失败: {result.Message}");
            }

            // Pattern: @xxx禁言 or @xxx解禁
            var atMuteMatch = Regex.Match(content, @"@(\d+)(禁言|解禁)");
            if (atMuteMatch.Success)
            {
                var wangWangId = atMuteMatch.Groups[1].Value;
                var action = atMuteMatch.Groups[2].Value;
                var mute = action == "禁言";
                
                if (string.IsNullOrEmpty(groupId))
                    return (true, $"无法获取群ID，{action}失败");
                
                var result = await ChatService.Instance.MuteTeamMemberAsync(groupId, wangWangId, mute);
                Log($"{action}成员: {wangWangId}, 结果: {result.Message}");
                
                if (result.Success)
                {
                    var player = DataService.Instance.GetPlayer(wangWangId);
                    return (true, $"已{action} {player?.Nickname ?? wangWangId}({wangWangId})");
                }
                return (true, $"{action}失败: {result.Message}");
            }

            // Pattern: Direct @ mention (equals sending "1" for betting)
            var directAtMatch = Regex.Match(content, @"^@(\d+)$");
            if (directAtMatch.Success)
            {
                var wangWangId = directAtMatch.Groups[1].Value;
                // This equals the player sending "1" in the group
                Log($"艾特等于发1: {wangWangId}");
                // Let the betting service handle this
                return (false, null); // Not handled here, let betting service process
            }

            return (false, null);
        }

        #endregion

        #region 8. Private Chat Score Commands (私聊上下分处理)

        /// <summary>
        /// Pending score requests (上分/下分请求队列)
        /// Key: requestId, Value: (wangWangId, amount, reason, time, type)
        /// </summary>
        private readonly Dictionary<int, (string WangWangId, decimal Amount, string Reason, DateTime Time, string Type)> _pendingScoreRequests 
            = new Dictionary<int, (string, decimal, string, DateTime, string)>();
        private int _nextRequestId = 1;
        private readonly object _requestLock = new object();

        /// <summary>
        /// Add a pending score request
        /// </summary>
        public void AddPendingRequest(string wangWangId, decimal amount, string reason, string type)
        {
            lock (_requestLock)
            {
                var id = _nextRequestId++;
                _pendingScoreRequests[id] = (wangWangId, amount, reason, DateTime.Now, type);
                Log($"添加{type}请求 #{id}: {wangWangId} {amount} {reason}");
            }
            
            // Notify subscribers (ScoreForm UI)
            OnPendingRequestAdded?.Invoke(wangWangId, amount, reason, type);
        }

        /// <summary>
        /// Process private chat score commands: 查看上分, 查看下分, 到的, 没到, 全到, 回钱, 拒绝, 全回
        /// </summary>
        private (bool Handled, string Reply) ProcessPrivateScoreCommand(string content, string senderId)
        {
            // View pending score up requests (查看上分)
            if (Regex.IsMatch(content, @"^查看上分$"))
            {
                return (true, GetPendingRequests("上分"));
            }

            // View pending score down requests (查看下分)
            if (Regex.IsMatch(content, @"^查看下分$"))
            {
                return (true, GetPendingRequests("下分"));
            }

            // Process score arrived commands: 到的[数字], 全到[数字], 没到[数字], 忽略查钱[数字]
            var arrivedMatch = Regex.Match(content, @"^(到的|没到|全到|忽略查钱)(\d*)$");
            if (arrivedMatch.Success)
            {
                var action = arrivedMatch.Groups[1].Value;
                var numStr = arrivedMatch.Groups[2].Value;
                int num = string.IsNullOrEmpty(numStr) ? 1 : int.Parse(numStr);

                return ProcessScoreArrivedCommand(action, num);
            }

            // Process money return commands: 回钱[数字], 拒绝[数字], 全回[数字], 忽略回钱[数字]
            var returnMatch = Regex.Match(content, @"^(回钱|拒绝|全回|忽略回钱)(\d*)$");
            if (returnMatch.Success)
            {
                var action = returnMatch.Groups[1].Value;
                var numStr = returnMatch.Groups[2].Value;
                int num = string.IsNullOrEmpty(numStr) ? 1 : int.Parse(numStr);

                return ProcessMoneyReturnCommand(action, num);
            }

            return (false, null);
        }

        /// <summary>
        /// Get pending requests list
        /// </summary>
        private string GetPendingRequests(string type)
        {
            lock (_requestLock)
            {
                var requests = _pendingScoreRequests
                    .Where(r => r.Value.Type == type)
                    .OrderBy(r => r.Key)
                    .ToList();

                if (requests.Count == 0)
                    return $"暂无待处理的{type}请求";

                var sb = new StringBuilder();
                sb.AppendLine($"【待处理{type}】共 {requests.Count} 条:");
                
                int index = 1;
                foreach (var kvp in requests.Take(10))
                {
                    var r = kvp.Value;
                    var player = DataService.Instance.GetPlayer(r.WangWangId);
                    sb.AppendLine($"{index}. {player?.Nickname ?? r.WangWangId}({r.WangWangId}) {r.Amount} {r.Reason} [{r.Time:HH:mm}]");
                    index++;
                }
                
                if (requests.Count > 10)
                    sb.AppendLine($"... 还有 {requests.Count - 10} 条");

                return sb.ToString().TrimEnd();
            }
        }

        /// <summary>
        /// Process score arrived commands (到的/没到/全到/忽略查钱)
        /// </summary>
        private (bool Handled, string Reply) ProcessScoreArrivedCommand(string action, int count)
        {
            lock (_requestLock)
            {
                var requests = _pendingScoreRequests
                    .Where(r => r.Value.Type == "上分")
                    .OrderBy(r => r.Key)
                    .Take(action == "全到" ? int.MaxValue : count)
                    .ToList();

                if (requests.Count == 0)
                    return (true, "暂无待处理的上分请求");

                var sb = new StringBuilder();
                int processed = 0;

                foreach (var kvp in requests)
                {
                    var r = kvp.Value;
                    var player = DataService.Instance.GetOrCreatePlayer(r.WangWangId, r.WangWangId);

                    switch (action)
                    {
                        case "到的":
                        case "全到":
                            // Confirm score arrived - add to player
                            player.Score += r.Amount;
                            DataService.Instance.SavePlayer(player);
                            DataService.Instance.LogScoreChange(DateTime.Today, r.WangWangId, r.Amount, r.Reason);
                            sb.AppendLine($"✓ {player.Nickname ?? r.WangWangId} +{r.Amount} 已到账");
                            _pendingScoreRequests.Remove(kvp.Key);
                            processed++;
                            break;

                        case "没到":
                            // Mark as not arrived - remove from queue
                            sb.AppendLine($"✗ {player.Nickname ?? r.WangWangId} +{r.Amount} 未到账已取消");
                            _pendingScoreRequests.Remove(kvp.Key);
                            processed++;
                            break;

                        case "忽略查钱":
                            // Ignore - remove from queue without processing
                            _pendingScoreRequests.Remove(kvp.Key);
                            processed++;
                            break;
                    }

                    if (action != "全到" && processed >= count)
                        break;
                }

                sb.AppendLine($"已处理 {processed} 条上分请求");
                Log($"私聊{action}: 处理了 {processed} 条上分请求");
                return (true, sb.ToString().TrimEnd());
            }
        }

        /// <summary>
        /// Process money return commands (回钱/拒绝/全回/忽略回钱)
        /// </summary>
        private (bool Handled, string Reply) ProcessMoneyReturnCommand(string action, int count)
        {
            lock (_requestLock)
            {
                var requests = _pendingScoreRequests
                    .Where(r => r.Value.Type == "下分")
                    .OrderBy(r => r.Key)
                    .Take(action == "全回" ? int.MaxValue : count)
                    .ToList();

                if (requests.Count == 0)
                    return (true, "暂无待处理的下分请求");

                var sb = new StringBuilder();
                int processed = 0;

                foreach (var kvp in requests)
                {
                    var r = kvp.Value;
                    var player = DataService.Instance.GetOrCreatePlayer(r.WangWangId, r.WangWangId);

                    switch (action)
                    {
                        case "回钱":
                        case "全回":
                            // Confirm money returned - deduct from player
                            player.Score -= Math.Abs(r.Amount);
                            DataService.Instance.SavePlayer(player);
                            DataService.Instance.LogScoreChange(DateTime.Today, r.WangWangId, -Math.Abs(r.Amount), r.Reason);
                            sb.AppendLine($"✓ {player.Nickname ?? r.WangWangId} -{Math.Abs(r.Amount)} 已回钱");
                            _pendingScoreRequests.Remove(kvp.Key);
                            processed++;
                            break;

                        case "拒绝":
                            // Reject - remove from queue
                            sb.AppendLine($"✗ {player.Nickname ?? r.WangWangId} -{Math.Abs(r.Amount)} 已拒绝");
                            _pendingScoreRequests.Remove(kvp.Key);
                            processed++;
                            break;

                        case "忽略回钱":
                            // Ignore - remove from queue without processing
                            _pendingScoreRequests.Remove(kvp.Key);
                            processed++;
                            break;
                    }

                    if (action != "全回" && processed >= count)
                        break;
                }

                sb.AppendLine($"已处理 {processed} 条下分请求");
                Log($"私聊{action}: 处理了 {processed} 条下分请求");
                return (true, sb.ToString().TrimEnd());
            }
        }

        #endregion

        #region 9. Switch Robot Command (换机器人)

        /// <summary>
        /// Process switch robot command: 换机器人
        /// </summary>
        private (bool Handled, string Reply) ProcessSwitchRobotCommand(string content)
        {
            if (Regex.IsMatch(content, @"^换机器人$"))
            {
                // This command sets the current sender as the active robot
                Log("执行换机器人命令");
                return (true, "机器人切换功能执行中...\n请在主窗口选择账号进行切换");
            }

            return (false, null);
        }

        #endregion

        private void Log(string message)
        {
            Logger.Info($"[AdminCmd] {message}");
            OnLog?.Invoke(message);
        }
        
        /// <summary>
        /// 上分后自动处理之前下注 (AutoProcessBeforeScore)
        /// When player gets score up, process any pending bets they had before having score
        /// </summary>
        private async System.Threading.Tasks.Task ProcessPreviousBetsAfterScoreUpAsync(string wangWangId, Models.Player player)
        {
            try
            {
                Log($"[AutoProcess] Checking for previous bets after score up for {wangWangId}");
                
                // Check if there are any rejected/pending bets for this player
                var period = LotteryService.Instance.NextPeriod ?? LotteryService.Instance.CurrentPeriod ?? "";
                if (string.IsNullOrEmpty(period))
                {
                    Log($"[AutoProcess] No current period, skipping");
                    return;
                }
                
                // Read existing bets for this player in current period
                var existingBets = Betting.BetLedgerService.Instance.ReadBets(DateTime.Today, null, period)
                    .Where(b => b.PlayerId == wangWangId)
                    .ToList();
                
                if (existingBets.Count == 0)
                {
                    Log($"[AutoProcess] No existing bets found for {wangWangId} in period {period}");
                    return;
                }
                
                // Calculate total bet amount
                var totalBet = existingBets.Sum(b => b.TotalAmount);
                
                // Check if player now has enough score
                if (player.Score >= totalBet)
                {
                    Log($"[AutoProcess] Player {wangWangId} now has enough score ({player.Score}) for bets ({totalBet})");
                    
                    // Send notification that bets are now active
                    if (ChatService.Instance.IsConnected)
                    {
                        var config = ConfigService.Instance.Config;
                        var groupId = config.GroupId;
                        if (!string.IsNullOrEmpty(groupId))
                        {
                            var msg = $"@{player.Nickname ?? wangWangId} 上分成功，您的下注 ({totalBet}) 已生效";
                            await ChatService.Instance.SendTextAsync("team", groupId, msg);
                            Log($"[AutoProcess] Sent bet activation notification for {wangWangId}");
                        }
                    }
                }
                else
                {
                    Log($"[AutoProcess] Player {wangWangId} still lacks score ({player.Score} < {totalBet})");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[AdminCmd] ProcessPreviousBetsAfterScoreUpAsync error: {ex.Message}");
            }
        }
    }
}

