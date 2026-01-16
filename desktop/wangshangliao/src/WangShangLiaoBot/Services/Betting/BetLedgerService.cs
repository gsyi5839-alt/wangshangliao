using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WangShangLiaoBot.Models;
using WangShangLiaoBot.Models.Betting;

namespace WangShangLiaoBot.Services.Betting
{
    /// <summary>
    /// Capture bet messages from group chat and persist as real bet ledgers.
    /// Source: ChatService.OnMessageReceived (hook polling in CDP mode).
    /// </summary>
    public sealed class BetLedgerService
    {
        private static BetLedgerService _instance;
        public static BetLedgerService Instance => _instance ?? (_instance = new BetLedgerService());

        private BetLedgerService() { }

        public bool IsRunning { get; private set; }

        public void Start()
        {
            if (IsRunning) return;
            ChatService.Instance.OnMessageReceived += HandleMessage;
            IsRunning = true;
        }

        public void Stop()
        {
            if (!IsRunning) return;
            ChatService.Instance.OnMessageReceived -= HandleMessage;
            IsRunning = false;
        }

        /// <summary>
        /// Handle incoming chat message asynchronously
        /// Uses async void for event handler pattern (safe for fire-and-forget event handlers)
        /// </summary>
        private async void HandleMessage(ChatMessage msg)
        {
            try
            {
                if (!IsRunning) return;
                if (msg == null) return;
                if (msg.IsSelf) return;
                if (msg.Type != MessageType.Text) return;
                
                var betSettings = BetProcessSettingsService.Instance;
                
                // 检查消息来源 - 群聊或私聊
                if (msg.IsGroupMessage)
                {
                    // 接收群聊下注 - 如果未启用则不处理群聊下注
                    if (!betSettings.ReceiveGroupBet)
                    {
                        // 默认不启用时仍处理群聊下注（兼容旧逻辑）
                        // 当明确设置为不接收时才跳过
                    }
                }
                else
                {
                    // 私聊消息 - 检查是否启用好友私聊下注
                    if (!betSettings.EnableFriendChat)
                    {
                        return; // 未启用好友私聊下注，跳过
                    }
                    
                    // 只接群成员下注 - 检查发送者是否为群成员 (OnlyMemberBet check)
                    if (betSettings.OnlyMemberBet)
                    {
                        var appConfig = ConfigService.Instance.Config;
                        var groupIdForCheck = appConfig.GroupId;
                        
                        if (!string.IsNullOrEmpty(groupIdForCheck) && !string.IsNullOrEmpty(msg.SenderId))
                        {
                            // Use await instead of .Result to avoid potential deadlock
                            var isMember = await ChatService.Instance.IsTeamMemberAsync(groupIdForCheck, msg.SenderId);
                            if (!isMember)
                            {
                                Logger.Info($"[BetLedger] OnlyMemberBet rejected: {msg.SenderId} is not a group member");
                                return; // 不是群成员，不处理下注
                            }
                            Logger.Info($"[BetLedger] OnlyMemberBet check passed: {msg.SenderId} is a group member");
                        }
                    }
                }

                var config = ConfigService.Instance.Config;
                var player = DataService.Instance.GetOrCreatePlayer(msg.SenderId, msg.SenderName);
                
                // Check for cancel bet command (取消下注)
                if (IsCancelBetCommand(msg.Content))
                {
                    // Check if cancel is prohibited (禁止取消)
                    if (betSettings.ProhibitCancel)
                    {
                        Logger.Info($"[BetLedger] Cancel prohibited from {msg.SenderName}");
                        SendInternalReply(msg, player, "本群禁止取消下注", "禁止取消");
                        return;
                    }
                    SendInternalReply(msg, player, config.InternalCancelBet, "取消下注");
                    return;
                }
                
                // Check if message is a bet
                if (!BetMessageParser.TryParse(msg.Content, out var items, out var total, out var normalized))
                    return;

                // Mark message as processed to prevent AutoReplyService from duplicate handling
                msg.IsProcessed = true;

                // Determine period: default to next period for pre-bet
                var period = LotteryService.Instance.NextPeriod;
                if (string.IsNullOrEmpty(period))
                    period = LotteryService.Instance.CurrentPeriod ?? "";
                if (string.IsNullOrEmpty(period)) return;

                var record = new BetRecord
                {
                    Time = msg.Time == default ? DateTime.Now : msg.Time,
                    Period = period,
                    TeamId = msg.GroupId ?? "",
                    PlayerId = msg.SenderId ?? "",
                    PlayerNick = msg.SenderName ?? "",
                    RawText = msg.Content ?? "",
                    NormalizedText = normalized ?? "",
                    Items = items ?? new List<BetItem>(),
                    TotalAmount = total,
                    ScoreBefore = player?.Score ?? 0m
                };

                // Check for modify bet (改注/加注) commands
                bool isModifyBet = IsModifyBetCommand(msg.Content);
                if (isModifyBet && !betSettings.AllowModifyBet)
                {
                    Logger.Info($"[BetLedger] Modify bet prohibited from {msg.SenderName}");
                    SendInternalReply(msg, player, "本群禁止改加注", "禁止改注");
                    return;
                }
                
                // Send internal reply based on different scenarios (内部回复)
                var replyResult = DetermineInternalReply(msg, player, record, config);
                
                // If bet is rejected (sealed, no score), don't save the record
                if (replyResult.RejectBet)
                {
                    SendInternalReply(msg, player, replyResult.Template, replyResult.Reason);
                    return;
                }
                
                // 重复下注处理 - Handle repeat bets based on RepeatBetMode
                record = HandleRepeatBet(record, betSettings.RepeatBetMode);
                if (record == null)
                {
                    Logger.Info($"[BetLedger] Repeat bet ignored from {msg.SenderName} (mode={betSettings.RepeatBetMode})");
                    return; // Bet was ignored due to repeat handling
                }
                
                // Save bet record
                AppendBetRecord(record);
                
                // Update player bet count (下分最少次数检查需要)
                if (player != null)
                {
                    player.BetCount++;
                    // Reset today bet count if it's a new day
                    if (player.LastBetDate.Date != DateTime.Today)
                    {
                        player.TodayBetCount = 0;
                        player.LastBetDate = DateTime.Today;
                    }
                    player.TodayBetCount++;
                    DataService.Instance.SavePlayer(player);
                }

                // Track stake for today's stats (no hard-coded values; derived from captured bet amount)
                var dateKey = DateTime.Today.ToString("yyyy-MM-dd");
                var stake = DataService.Instance.GetDailyDecimal(dateKey, record.PlayerId, "Stake", 0m);
                DataService.Instance.SetDailyDecimal(dateKey, record.PlayerId, "Stake", stake + record.TotalAmount);
                
                // Send success reply
                SendInternalReply(msg, player, replyResult.Template, replyResult.Reason);
            }
            catch (Exception ex)
            {
                // Log but don't break message loop
                Logger.Error($"[BetLedger] HandleMessage error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Check if message is a cancel bet command
        /// </summary>
        private bool IsCancelBetCommand(string content)
        {
            if (string.IsNullOrEmpty(content)) return false;
            var txt = content.Trim().ToLower();
            return txt == "取消" || txt == "取消下注" || txt == "撤销" || txt == "撤销下注" ||
                   txt == "cancel" || txt == "qx";
        }
        
        /// <summary>
        /// Check if message is a modify bet command (改注/加注)
        /// </summary>
        private bool IsModifyBetCommand(string content)
        {
            if (string.IsNullOrEmpty(content)) return false;
            var txt = content.Trim().ToLower();
            return txt.Contains("改") || txt.Contains("加") || txt.Contains("+") ||
                   txt.StartsWith("改注") || txt.StartsWith("加注");
        }
        
        /// <summary>
        /// Handle repeat bet based on RepeatBetMode
        /// 0=算成加注(不推荐), 1=同注不算 不同等于加注, 2=算最后一次下注(推荐), 3=算前第一次下注
        /// </summary>
        private BetRecord HandleRepeatBet(BetRecord newRecord, int mode)
        {
            try
            {
                // Read existing bets for this period and player
                var existingBets = ReadBets(DateTime.Today, newRecord.TeamId, newRecord.Period)
                    .Where(b => b.PlayerId == newRecord.PlayerId)
                    .ToList();
                
                if (existingBets.Count == 0)
                    return newRecord; // No existing bets, accept new one
                
                switch (mode)
                {
                    case 0: // 算成加注 - add to existing
                        // Just accept new bet, it will be added
                        return newRecord;
                        
                    case 1: // 同注不算 不同等于加注
                        // Check if same bet exists
                        bool hasSameBet = existingBets.Any(b => 
                            b.NormalizedText == newRecord.NormalizedText || 
                            b.RawText == newRecord.RawText);
                        if (hasSameBet)
                        {
                            Logger.Info($"[BetLedger] Same bet ignored: {newRecord.RawText}");
                            return null; // Ignore same bet
                        }
                        return newRecord; // Accept different bet
                        
                    case 2: // 算最后一次下注 - use last bet only
                        // Delete existing bets for this player/period (handled in AppendBetRecord)
                        ClearPlayerBets(DateTime.Today, newRecord.TeamId, newRecord.Period, newRecord.PlayerId);
                        return newRecord;
                        
                    case 3: // 算前第一次下注 - keep first bet only
                        // Already has bet, ignore new one
                        Logger.Info($"[BetLedger] First bet kept, ignoring new: {newRecord.RawText}");
                        return null;
                        
                    default:
                        return newRecord;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[BetLedger] HandleRepeatBet error: {ex.Message}");
                return newRecord; // On error, accept the bet
            }
        }
        
        /// <summary>
        /// Clear bets for a specific player in a period (used for "算最后一次下注" mode)
        /// </summary>
        private void ClearPlayerBets(DateTime day, string teamId, string period, string playerId)
        {
            try
            {
                var file = GetBetFile(day, teamId, period);
                if (!File.Exists(file)) return;
                
                var lines = File.ReadAllLines(file, Encoding.UTF8);
                var filteredLines = lines.Where(line => 
                {
                    if (string.IsNullOrWhiteSpace(line)) return false;
                    var parts = line.Split('\t');
                    if (parts.Length < 4) return true;
                    return parts[3] != playerId; // Keep lines from other players
                }).ToList();
                
                File.WriteAllLines(file, filteredLines, Encoding.UTF8);
                Logger.Info($"[BetLedger] Cleared bets for {playerId} in period {period}");
            }
            catch (Exception ex)
            {
                Logger.Error($"[BetLedger] ClearPlayerBets error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Result of internal reply determination
        /// </summary>
        private class InternalReplyResult
        {
            public string Template { get; set; }
            public string Reason { get; set; }
            public bool RejectBet { get; set; }
        }
        
        /// <summary>
        /// Determine which internal reply template to use based on bet scenario
        /// </summary>
        private InternalReplyResult DetermineInternalReply(ChatMessage msg, Player player, BetRecord record, AppConfig config)
        {
            var countdown = LotteryService.Instance.Countdown;
            bool isSealed = countdown <= 0;
            decimal playerScore = player?.Score ?? 0m;
            var betSettings = BetProcessSettingsService.Instance;
            
            // Scenario 1: 已封盘未处理 - lottery is sealed
            if (isSealed)
            {
                Logger.Info($"[BetLedger] Sealed bet rejected from {msg.SenderName}: {msg.Content}");
                return new InternalReplyResult
                {
                    Template = config.InternalSealedUnprocessed,
                    Reason = "已封盘",
                    RejectBet = true
                };
            }
            
            // Scenario 2: 无分下注 - player has no score
            if (playerScore <= 0)
            {
                Logger.Info($"[BetLedger] No score bet rejected from {msg.SenderName}: {msg.Content}");
                return new InternalReplyResult
                {
                    Template = config.InternalBetNoDown,
                    Reason = "无分下注",
                    RejectBet = true
                };
            }
            
            // Scenario 3: 分数不足 - player score is less than bet amount
            if (playerScore < record.TotalAmount)
            {
                Logger.Info($"[BetLedger] Insufficient score bet from {msg.SenderName}: score={playerScore}, bet={record.TotalAmount}");
                return new InternalReplyResult
                {
                    Template = config.InternalBetNoDown2,
                    Reason = "分数不足",
                    RejectBet = true
                };
            }
            
            // Scenario 4: 仅支持拼音下注 - check if bet contains Chinese characters
            if (betSettings.PinyinBetOnly && ContainsChinese(record.RawText))
            {
                Logger.Info($"[BetLedger] Chinese bet rejected (PinyinOnly) from {msg.SenderName}: {msg.Content}");
                
                // ChineseBetMode: 0=有效并提醒, 1=无效并提醒
                bool rejectBet = betSettings.ChineseBetMode == 1;
                return new InternalReplyResult
                {
                    Template = betSettings.PinyinRemindContent,
                    Reason = "中文下注",
                    RejectBet = rejectBet
                };
            }
            
            // Scenario 5: 无账单下注提醒 - player has no existing bill/record
            if (betSettings.NoBillRemindEnabled && player != null)
            {
                // Check if player has any previous records
                var hasPreviousBets = DataService.Instance.GetDailyDecimal(
                    DateTime.Today.ToString("yyyy-MM-dd"), player.WangWangId, "Stake", 0m) > 0;
                
                if (!hasPreviousBets && playerScore <= 0)
                {
                    Logger.Info($"[BetLedger] No bill reminder for {msg.SenderName}: {msg.Content}");
                    var remindMsg = betSettings.NoBillRemindContent
                        .Replace("@QQ", $"@{msg.SenderName}")
                        .Replace("@qq", $"@{msg.SenderName}")
                        .Replace("[下注内容]", record.RawText);
                    return new InternalReplyResult
                    {
                        Template = remindMsg,
                        Reason = "无账单提醒",
                        RejectBet = true
                    };
                }
            }
            
            // Scenario 6: 杀组合下注无效 - check for opposite/combination bets
            if (betSettings.CombinationInvalidEnabled && record.Items != null)
            {
                // Check for opposite bets (e.g., both 大 and 小)
                var codes = record.Items.Select(i => i.Code?.ToUpper()).ToList();
                bool hasOpposite = (codes.Contains("D") && codes.Contains("X")) ||
                                   (codes.Contains("S") && codes.Contains("DS")) ||
                                   (codes.Contains("大") && codes.Contains("小")) ||
                                   (codes.Contains("单") && codes.Contains("双"));
                
                if (hasOpposite)
                {
                    Logger.Info($"[BetLedger] Opposite bet rejected from {msg.SenderName}: {msg.Content}");
                    return new InternalReplyResult
                    {
                        Template = betSettings.CombinationInvalidMsg,
                        Reason = "杀组合无效",
                        RejectBet = true
                    };
                }
            }
            
            // Scenario 6.5: 单注反下注无效 - single opposite bet
            if (betSettings.SingleOppositeInvalidEnabled && record.Items != null)
            {
                // Check if bet is a single opposite bet (only one item that's opposite type)
                if (record.Items.Count == 1)
                {
                    var code = record.Items[0].Code?.ToUpper() ?? "";
                    bool isOppositeType = code == "D" || code == "X" || code == "S" || code == "DS" ||
                                         code == "大" || code == "小" || code == "单" || code == "双";
                    if (isOppositeType)
                    {
                        // Check if player has existing opposite bet
                        var existingBets = ReadBets(DateTime.Today, record.TeamId, record.Period)
                            .Where(b => b.PlayerId == record.PlayerId)
                            .ToList();
                        
                        foreach (var existing in existingBets)
                        {
                            var existingCodes = existing.Items?.Select(i => i.Code?.ToUpper()).ToList() ?? new List<string>();
                            bool hasOppositeExisting = 
                                (code == "D" && existingCodes.Contains("X")) ||
                                (code == "X" && existingCodes.Contains("D")) ||
                                (code == "S" && existingCodes.Contains("DS")) ||
                                (code == "DS" && existingCodes.Contains("S")) ||
                                (code == "大" && existingCodes.Contains("小")) ||
                                (code == "小" && existingCodes.Contains("大")) ||
                                (code == "单" && existingCodes.Contains("双")) ||
                                (code == "双" && existingCodes.Contains("单"));
                            
                            if (hasOppositeExisting)
                            {
                                Logger.Info($"[BetLedger] Single opposite bet rejected from {msg.SenderName}: {msg.Content}");
                                var singleMsg = betSettings.SingleOppositeInvalidMsg
                                    .Replace("@qq", $"@{msg.SenderName}");
                                return new InternalReplyResult
                                {
                                    Template = singleMsg,
                                    Reason = "单注反下注无效",
                                    RejectBet = true
                                };
                            }
                        }
                    }
                }
            }
            
            // Scenario 7: 多组合下注无效 - check for too many combinations
            if (betSettings.MultiCombinationInvalidEnabled && record.Items != null)
            {
                var uniqueTypes = record.Items.Select(i => GetBetType(i.Code)).Distinct().Count();
                if (uniqueTypes > 2) // More than 2 different bet types
                {
                    Logger.Info($"[BetLedger] Multi-combination bet rejected from {msg.SenderName}: {msg.Content}");
                    return new InternalReplyResult
                    {
                        Template = betSettings.MultiCombinationInvalidMsg,
                        Reason = "多组合无效",
                        RejectBet = true
                    };
                }
            }
            
            // Scenario 7: 组合数量限制 - check max combination count
            if (betSettings.MaxCombinationEnabled && record.Items != null)
            {
                if (record.Items.Count > betSettings.MaxCombinationCount)
                {
                    Logger.Info($"[BetLedger] Max combination exceeded from {msg.SenderName}: {record.Items.Count} > {betSettings.MaxCombinationCount}");
                    return new InternalReplyResult
                    {
                        Template = betSettings.MaxCombinationMsg,
                        Reason = "组合超限",
                        RejectBet = true
                    };
                }
            }
            
            // Scenario 8: 禁止点09 - check if 09 bets are forbidden
            if (!string.IsNullOrEmpty(config.InternalForbid09))
            {
                var forbids09 = record.Items?.Any(i => 
                    i.Code?.Contains("09") == true || 
                    i.Code?.Equals("0") == true || 
                    i.Code?.Equals("9") == true) ?? false;
                
                if (forbids09)
                {
                    Logger.Info($"[BetLedger] Forbidden 09 bet from {msg.SenderName}: {msg.Content}");
                    return new InternalReplyResult
                    {
                        Template = config.InternalForbid09,
                        Reason = "禁止点09",
                        RejectBet = true
                    };
                }
            }
            
            // Scenario 9: 攻击范围超限 - check bet amount range limits
            var rangeSettings = BetAttackRangeSettingsService.Instance;
            foreach (var item in record.Items ?? new List<BetItem>())
            {
                var rangeError = rangeSettings.ValidateBetAmount(item.Code, item.Amount);
                if (!string.IsNullOrEmpty(rangeError))
                {
                    Logger.Info($"[BetLedger] Out of range bet from {msg.SenderName}: {item.Code} {item.Amount}");
                    return new InternalReplyResult
                    {
                        Template = rangeError,
                        Reason = "超范围",
                        RejectBet = true
                    };
                }
            }
            
            // Scenario 10: 总额封顶超限 - check total amount limit
            var totalError = rangeSettings.ValidateTotalAmount(record.TotalAmount, record.NormalizedText);
            if (!string.IsNullOrEmpty(totalError))
            {
                Logger.Info($"[BetLedger] Total limit exceeded from {msg.SenderName}: {record.TotalAmount}");
                return new InternalReplyResult
                {
                    Template = totalError,
                    Reason = "总额超限",
                    RejectBet = true
                };
            }
            
            // Scenario 11: 模糊提醒 - bet might be ambiguous
            if (betSettings.FuzzyMatchEnabled && betSettings.FuzzyMatchSupportRemind && 
                !string.IsNullOrEmpty(config.InternalFuzzyRemind))
            {
                bool isFuzzy = (record.Items?.Count == 1 && record.TotalAmount < 100) ||
                               (record.RawText?.Length < 5);
                
                if (isFuzzy)
                {
                    Logger.Info($"[BetLedger] Fuzzy bet from {msg.SenderName}: {msg.Content}");
                    return new InternalReplyResult
                    {
                        Template = config.InternalFuzzyRemind,
                        Reason = "模糊提醒",
                        RejectBet = false
                    };
                }
            }
            
            // Scenario 12: 下注显示 - normal bet accepted
            // Check if ShowBet is enabled
            if (!betSettings.ShowBet)
            {
                // Don't send reply but still accept the bet
                return new InternalReplyResult
                {
                    Template = null, // No reply
                    Reason = "下注成功(静默)",
                    RejectBet = false
                };
            }
            
            return new InternalReplyResult
            {
                Template = config.InternalBetDisplay,
                Reason = "下注成功",
                RejectBet = false
            };
        }
        
        /// <summary>
        /// Check if text contains Chinese characters
        /// </summary>
        private bool ContainsChinese(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            return text.Any(c => c >= 0x4E00 && c <= 0x9FFF);
        }
        
        /// <summary>
        /// Get bet type category from code
        /// </summary>
        private string GetBetType(string code)
        {
            if (string.IsNullOrEmpty(code)) return "other";
            code = code.ToUpper();
            
            if (code == "D" || code == "X" || code == "大" || code == "小") return "dx";
            if (code == "S" || code == "DS" || code == "单" || code == "双") return "ds";
            if (code == "DD" || code == "XD" || code == "XS" || code == "大单" || code == "大双" || code == "小单" || code == "小双") return "dxds";
            if (int.TryParse(code, out _)) return "digit";
            return "other";
        }

        /// <summary>
        /// Get bet file path for a day+period.
        /// </summary>
        public string GetBetFile(DateTime day, string teamId, string period)
        {
            var safeTeam = string.IsNullOrWhiteSpace(teamId) ? "unknown-team" : teamId.Trim();
            var dir = Path.Combine(DataService.Instance.DatabaseDir, "Bets", day.ToString("yyyy-MM-dd"), safeTeam);
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, $"bets-{period}.txt");
        }

        /// <summary>
        /// Append one bet record to ledger file (tab separated).
        /// </summary>
        private void AppendBetRecord(BetRecord r)
        {
            var file = GetBetFile(DateTime.Today, r.TeamId, r.Period);

            // Format (TSV):
            // time  period  teamId  playerId  nick  scoreBefore  total  normalized  raw
            var line = string.Join("\t", new[]
            {
                r.Time.ToString("HH:mm:ss"),
                r.Period ?? "",
                r.TeamId ?? "",
                r.PlayerId ?? "",
                (r.PlayerNick ?? "").Replace("\t"," "),
                r.ScoreBefore.ToString(CultureInfo.InvariantCulture),
                r.TotalAmount.ToString(CultureInfo.InvariantCulture),
                (r.NormalizedText ?? "").Replace("\t"," "),
                (r.RawText ?? "").Replace("\t"," ")
            });

            File.AppendAllText(file, line + Environment.NewLine, Encoding.UTF8);
        }

        /// <summary>
        /// Read bet records for a day+period.
        /// This is used by settlement and template variables like [下注核对].
        /// </summary>
        public List<BetRecord> ReadBets(DateTime day, string period)
        {
            // Backward compatible: read all groups for the period (rarely used by templates now).
            return ReadBets(day, teamId: null, period: period);
        }

        /// <summary>
        /// Read bet records for a day+teamId+period.
        /// </summary>
        public List<BetRecord> ReadBets(DateTime day, string teamId, string period)
        {
            var list = new List<BetRecord>();
            try
            {
                var baseDir = Path.Combine(DataService.Instance.DatabaseDir, "Bets", day.ToString("yyyy-MM-dd"));
                if (!Directory.Exists(baseDir)) return list;

                // If teamId not specified, read from all team folders
                var teamDirs = string.IsNullOrWhiteSpace(teamId)
                    ? Directory.GetDirectories(baseDir)
                    : new[] { Path.Combine(baseDir, teamId.Trim()) };

                foreach (var dir in teamDirs)
                {
                    var file = Path.Combine(dir, $"bets-{period}.txt");
                    if (!File.Exists(file)) continue;

                    var lines = File.ReadAllLines(file, Encoding.UTF8);
                    foreach (var line in lines.Where(l => !string.IsNullOrWhiteSpace(l)))
                    {
                        var parts = line.Split('\t');
                        if (parts.Length < 9) continue;
                        decimal.TryParse(parts[5], NumberStyles.Any, CultureInfo.InvariantCulture, out var scoreBefore);
                        decimal.TryParse(parts[6], NumberStyles.Any, CultureInfo.InvariantCulture, out var total);
                        list.Add(new BetRecord
                        {
                            Time = DateTime.Today,
                            Period = parts[1],
                            TeamId = parts[2],
                            PlayerId = parts[3],
                            PlayerNick = parts[4],
                            ScoreBefore = scoreBefore,
                            TotalAmount = total,
                            NormalizedText = parts[7],
                            RawText = parts[8]
                        });
                    }
                }
            }
            catch { }
            return list;
        }
        
        /// <summary>
        /// Clear all bet records for today (all teams, all periods).
        /// </summary>
        public int ClearTodayBets()
        {
            return ClearBets(DateTime.Today, teamId: null, period: null);
        }
        
        /// <summary>
        /// Clear bet records for a specific day/team/period.
        /// If teamId is null, clears all teams. If period is null, clears all periods.
        /// Returns number of files deleted.
        /// </summary>
        public int ClearBets(DateTime day, string teamId, string period)
        {
            var count = 0;
            try
            {
                var baseDir = Path.Combine(DataService.Instance.DatabaseDir, "Bets", day.ToString("yyyy-MM-dd"));
                if (!Directory.Exists(baseDir)) return 0;
                
                // Get team directories to process
                var teamDirs = string.IsNullOrWhiteSpace(teamId)
                    ? Directory.GetDirectories(baseDir)
                    : new[] { Path.Combine(baseDir, teamId.Trim()) };
                
                foreach (var dir in teamDirs)
                {
                    if (!Directory.Exists(dir)) continue;
                    
                    if (string.IsNullOrWhiteSpace(period))
                    {
                        // Delete all bet files in this team folder
                        var files = Directory.GetFiles(dir, "bets-*.txt");
                        foreach (var file in files)
                        {
                            File.Delete(file);
                            count++;
                        }
                    }
                    else
                    {
                        // Delete specific period file
                        var file = Path.Combine(dir, $"bets-{period}.txt");
                        if (File.Exists(file))
                        {
                            File.Delete(file);
                            count++;
                        }
                    }
                }
                
                Logger.Info($"[BetLedger] Cleared {count} bet file(s) for {day:yyyy-MM-dd}");
            }
            catch (Exception ex)
            {
                Logger.Error($"[BetLedger] ClearBets error: {ex.Message}");
            }
            return count;
        }
        
        /// <summary>
        /// Clear bet records for current period only.
        /// </summary>
        public int ClearCurrentPeriodBets(string teamId)
        {
            var period = LotteryService.Instance.NextPeriod;
            if (string.IsNullOrEmpty(period))
                period = LotteryService.Instance.CurrentPeriod ?? "";
            
            if (string.IsNullOrEmpty(period)) return 0;
            return ClearBets(DateTime.Today, teamId, period);
        }
        
        /// <summary>
        /// Send internal reply message (内部回复功能)
        /// </summary>
        private async void SendInternalReply(ChatMessage msg, Player player, string template, string reason)
        {
            try
            {
                // If no template configured, skip reply
                if (string.IsNullOrEmpty(template)) return;
                
                var config = ConfigService.Instance.Config;
                var betSettings = BetProcessSettingsService.Instance;
                
                // Render template with player context
                var reply = TemplateEngine.Render(template, new TemplateEngine.RenderContext
                {
                    Message = msg,
                    Player = player,
                    Today = DateTime.Today
                });
                
                if (string.IsNullOrEmpty(reply)) return;
                
                // 私聊下注不在群内反馈
                if (!msg.IsGroupMessage && betSettings.FriendBetNotInGroup)
                {
                    // Send to private chat instead
                    if (!string.IsNullOrEmpty(msg.SenderId))
                    {
                        var pvtResult = await ChatService.Instance.SendTextAsync("p2p", msg.SenderId, reply);
                        if (pvtResult.Success)
                        {
                            Logger.Info($"[BetLedger] Private reply sent ({reason}): {reply}");
                        }
                    }
                    return;
                }
                
                // Send reply to group
                string scene = "team";
                string to = msg.GroupId;
                
                if (!string.IsNullOrEmpty(to))
                {
                    // 全局图片发送功能
                    if (betSettings.GlobalImageSendEnabled && reply.Length > 10)
                    {
                        try
                        {
                            var imagePath = ImageGeneratorService.Instance.GenerateBillImage(reply, reason);
                            if (!string.IsNullOrEmpty(imagePath) && System.IO.File.Exists(imagePath))
                            {
                                var imgResult = await ChatService.Instance.SendImageAsync(scene, to, imagePath);
                                if (imgResult.Success)
                                {
                                    Logger.Info($"[BetLedger] Internal reply image sent ({reason})");
                                    return;
                                }
                                Logger.Error($"[BetLedger] Image send failed, falling back to text: {imgResult.Message}");
                            }
                        }
                        catch (Exception imgEx)
                        {
                            Logger.Error($"[BetLedger] Image generation error: {imgEx.Message}");
                        }
                    }
                    
                    var result = await ChatService.Instance.SendTextAsync(scene, to, reply);
                    if (result.Success)
                    {
                        Logger.Info($"[BetLedger] Internal reply sent ({reason}): {reply}");
                        
                        // BillSecondReply - 秒顺回复功能: 发送私聊确认给玩家
                        if (config.BillSecondReply && !string.IsNullOrEmpty(msg.SenderId) && reason == "下注成功")
                        {
                            await SendBillSecondReplyAsync(msg, player);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[BetLedger] SendInternalReply error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Send bill second reply - 账单秒顺回复 (私聊确认)
        /// </summary>
        private async System.Threading.Tasks.Task SendBillSecondReplyAsync(ChatMessage msg, Player player)
        {
            try
            {
                if (string.IsNullOrEmpty(msg.SenderId)) return;
                
                // Build confirmation message
                var period = LotteryService.Instance.NextPeriod ?? LotteryService.Instance.CurrentPeriod ?? "";
                var confirmMsg = $"✓ 下注确认\n" +
                                 $"期号: {period}\n" +
                                 $"内容: {msg.Content}\n" +
                                 $"余额: {player?.Score ?? 0}";
                
                // Send private message to player
                var result = await ChatService.Instance.SendTextAsync("p2p", msg.SenderId, confirmMsg);
                if (result.Success)
                {
                    Logger.Info($"[BetLedger] Bill second reply sent to {msg.SenderName}");
                }
                else
                {
                    Logger.Error($"[BetLedger] Bill second reply failed: {result.Message}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[BetLedger] SendBillSecondReplyAsync error: {ex.Message}");
            }
        }
    }
}


