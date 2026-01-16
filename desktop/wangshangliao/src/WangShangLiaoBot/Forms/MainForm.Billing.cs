using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Timers;
using WangShangLiaoBot.Models;
using WangShangLiaoBot.Services;
using WangShangLiaoBot.Services.HPSocket;
using WangShangLiaoBot.Forms.Settings;
using WangShangLiaoBot.Controls;
using WangShangLiaoBot.Controls.BetProcess;
using WangShangLiaoBot.Controls.Odds;

namespace WangShangLiaoBot.Forms
{
    public partial class MainForm : Form
    {
        private async void OnSettlementComplete(string period, int playerCount, decimal totalProfit)
        {
            try
            {
                var config = ConfigService.Instance.Config;
                
                // 开奖盈利反馈 - Send profit feedback
                if (config.ProfitFeedback && ChatService.Instance.IsConnected)
                {
                    var profitStr = totalProfit >= 0 ? $"+{totalProfit}" : $"{totalProfit}";
                    await SendFeedbackAsync("盈利", $"第{period}期盈利: {profitStr}, 玩家数: {playerCount}");
                }
                
                // 下注数据延迟发送功能 - Send bet data after delay
                if (config.BetDataDelaySeconds > 0 && ChatService.Instance.IsConnected)
                {
                    Logger.Info($"[MainForm] Scheduling bet data send after {config.BetDataDelaySeconds}s delay for period {period}");
                    _ = SendBetDataAfterDelayAsync(period, config.BetDataDelaySeconds);
                }
                
                // 群作业发送功能 - Send group task bill
                if (config.GroupTaskSend && ChatService.Instance.IsConnected)
                {
                    _ = SendGroupTaskBillAsync(period, playerCount, totalProfit);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[MainForm] OnSettlementComplete common error: {ex.Message}");
            }
            
            // Check if "开完本期停" is enabled
            if (!chkStopAfterPeriod.Checked)
                return;
            
            try
            {
                Logger.Info($"[MainForm] Settlement complete for period {period}, players={playerCount}, profit={totalProfit}. Executing 开完本期停...");
                
                // Execute on UI thread
                if (InvokeRequired)
                {
                    Invoke(new Action(async () => await ExecuteStopAfterPeriodAsync(period)));
                }
                else
                {
                    await ExecuteStopAfterPeriodAsync(period);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[MainForm] OnSettlementComplete error: {ex.Message}");
            }
        }

        private async System.Threading.Tasks.Task ExecuteStopAfterPeriodAsync(string period)
        {
            try
            {
                // 1. Mute group
                if (ChatService.Instance.IsConnected)
                {
                    lblStatus.Text = $"第 {period} 期结算完成，正在执行全体禁言...";
                    var muteResult = await ChatService.Instance.MuteAllAsync();
                    
                    if (muteResult.Success)
                    {
                        Logger.Info($"[MainForm] Auto mute success after period {period}");
                        
                        // Update checkbox state (without triggering event)
                        _muteGroupChanging = true;
                        chkMuteGroup.Checked = true;
                        _muteGroupChanging = false;
                    }
                    else
                    {
                        Logger.Error($"[MainForm] Auto mute failed: {muteResult.Message}");
                    }
                }
                
                // 2. Stop calculation and bet ledger
                if (Services.Betting.BetSettlementService.Instance.IsRunning)
                {
                    Services.Betting.BetSettlementService.Instance.Stop();
                    Services.Betting.BetLedgerService.Instance.Stop();  // 也停止下注记录服务
                    btnStopCalc.Text = "开始算账";
                    btnStopCalc.BackColor = System.Drawing.Color.LightGreen;
                    Logger.Info($"[MainForm] Auto stop calculation and bet ledger after period {period}");
                }
                
                lblStatus.Text = $"第 {period} 期完成: 已禁言 + 已停止算账";
                
                // Show notification
                MessageBox.Show(
                    $"第 {period} 期结算完成\n\n" +
                    "✅ 群已禁言\n" +
                    "✅ 算账已停止\n\n" +
                    "（开完本期停 功能已执行）",
                    "开完本期停",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Logger.Error($"[MainForm] ExecuteStopAfterPeriodAsync error: {ex.Message}");
            }
        }

        private async void btnStopCalc_Click(object sender, EventArgs e)
        {
            if (Services.Betting.BetSettlementService.Instance.IsRunning)
            {
                // Stop calculation
                Services.Betting.BetSettlementService.Instance.Stop();
                Services.Betting.BetLedgerService.Instance.Stop();  // 停止下注记录服务
                btnStopCalc.Text = "开始算账";
                btnStopCalc.BackColor = System.Drawing.Color.LightGreen;
                lblStatus.Text = "算账已停止，可以刷新开奖";
                Logger.Info("[MainForm] BetSettlementService 和 BetLedgerService 已停止");
                
                // 通知副框架停止算账
                try
                {
                    await Services.HPSocket.FrameworkClient.Instance.StopAccountingAsync();
                    Logger.Info("[MainForm] 已通知副框架停止算账");
                }
                catch (Exception ex)
                {
                    Logger.Error($"[MainForm] 通知副框架停止算账失败: {ex.Message}");
                }
            }
            else
            {
                // Start calculation
                Services.Betting.BetSettlementService.Instance.Start();
                Services.Betting.BetLedgerService.Instance.Start();  // 启动下注记录服务 - 自动存储群里下注数据
                btnStopCalc.Text = "停止算账";
                btnStopCalc.BackColor = System.Drawing.Color.Yellow;
                lblStatus.Text = "算账服务已启动，下注自动存储中...";
                Logger.Info("[MainForm] BetSettlementService 和 BetLedgerService 已启动");
                
                // 通知副框架开始算账 (接管群聊)
                try
                {
                    // 获取当前设置的群号
                    var groupId = ConfigService.Instance.Config?.GroupId;
                    if (string.IsNullOrEmpty(groupId))
                    {
                        Logger.Info($"[MainForm] 主框架未配置群号，将使用副框架绑定的群号");
                    }
                    await Services.HPSocket.FrameworkClient.Instance.StartAccountingAsync(groupId);
                    Logger.Info($"[MainForm] 已通知副框架开始算账, 群号: {(string.IsNullOrEmpty(groupId) ? "(使用副框架配置)" : groupId)}");
                }
                catch (Exception ex)
                {
                    Logger.Error($"[MainForm] 通知副框架开始算账失败: {ex.Message}");
                }
            }
        }

        private void btnManualCalc_Click(object sender, EventArgs e)
        {
            MessageBox.Show("手动算账功能", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnImportBill_Click(object sender, EventArgs e)
        {
            try
            {
                // Show import dialog
                using (var form = new Form())
                {
                    form.Text = "导入账单";
                    form.Size = new System.Drawing.Size(500, 400);
                    form.StartPosition = FormStartPosition.CenterParent;
                    form.FormBorderStyle = FormBorderStyle.FixedDialog;
                    form.MaximizeBox = false;
                    form.MinimizeBox = false;
                    
                    var lblTip = new Label
                    {
                        Text = "请粘贴账单内容，支持格式:\n• 昵称(旺旺号)=分数\n• 昵称=分数 (旺旺号自动生成)",
                        Location = new System.Drawing.Point(10, 10),
                        Size = new System.Drawing.Size(460, 45),
                        AutoSize = false
                    };
                    
                    var txtContent = new TextBox
                    {
                        Multiline = true,
                        ScrollBars = ScrollBars.Both,
                        Location = new System.Drawing.Point(10, 60),
                        Size = new System.Drawing.Size(460, 240),
                        AcceptsReturn = true
                    };
                    
                    var btnFromFile = new Button
                    {
                        Text = "从文件导入",
                        Location = new System.Drawing.Point(10, 310),
                        Size = new System.Drawing.Size(100, 30)
                    };
                    btnFromFile.Click += (s, args) =>
                    {
                        using (var ofd = new OpenFileDialog())
                        {
                            ofd.Filter = "文本文件|*.txt|所有文件|*.*";
                            ofd.Title = "选择账单文件";
                            if (ofd.ShowDialog() == DialogResult.OK)
                            {
                                txtContent.Text = System.IO.File.ReadAllText(ofd.FileName, System.Text.Encoding.UTF8);
                            }
                        }
                    };
                    
                    var btnImport = new Button
                    {
                        Text = "导入",
                        Location = new System.Drawing.Point(280, 310),
                        Size = new System.Drawing.Size(90, 30),
                        DialogResult = DialogResult.OK
                    };
                    
                    var btnCancel = new Button
                    {
                        Text = "取消",
                        Location = new System.Drawing.Point(380, 310),
                        Size = new System.Drawing.Size(90, 30),
                        DialogResult = DialogResult.Cancel
                    };
                    
                    form.Controls.AddRange(new Control[] { lblTip, txtContent, btnFromFile, btnImport, btnCancel });
                    form.AcceptButton = btnImport;
                    form.CancelButton = btnCancel;
                    
                    if (form.ShowDialog() == DialogResult.OK)
                    {
                        var content = txtContent.Text.Trim();
                        if (string.IsNullOrEmpty(content))
                        {
                            MessageBox.Show("请输入账单内容", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                        
                        // Parse and import
                        var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        var importCount = 0;
                        var autoIdIndex = 1;
                        
                        foreach (var line in lines)
                        {
                            // Format 1: 昵称(旺旺号)=分数 or 昵称(旺旺号$)=分数
                            var match1 = System.Text.RegularExpressions.Regex.Match(
                                line.Trim(),
                                @"(.+?)\(([^)]+)\)\s*=\s*(-?\d+\.?\d*)");
                            
                            if (match1.Success)
                            {
                                var nickname = match1.Groups[1].Value.Trim();
                                var wangwangId = match1.Groups[2].Value.Trim().Replace("$", "");
                                if (decimal.TryParse(match1.Groups[3].Value, out var score))
                                {
                                    var player = DataService.Instance.GetOrCreatePlayer(wangwangId, nickname);
                                    player.Score = score;
                                    player.LastActiveTime = DateTime.Now;
                                    DataService.Instance.SavePlayer(player);
                                    importCount++;
                                }
                                continue;
                            }
                            
                            // Format 2: 昵称=分数 (auto generate ID)
                            var match2 = System.Text.RegularExpressions.Regex.Match(
                                line.Trim(),
                                @"(.+?)\s*=\s*(-?\d+\.?\d*)");
                            
                            if (match2.Success)
                            {
                                var nickname = match2.Groups[1].Value.Trim();
                                if (decimal.TryParse(match2.Groups[2].Value, out var score))
                                {
                                    // Generate ID from nickname hash or auto increment
                                    var wangwangId = $"AUTO{autoIdIndex++:D6}";
                                    var player = DataService.Instance.GetOrCreatePlayer(wangwangId, nickname);
                                    player.Score = score;
                                    player.LastActiveTime = DateTime.Now;
                                    DataService.Instance.SavePlayer(player);
                                    importCount++;
                                }
                            }
                        }
                        
                        if (importCount > 0)
                        {
                            lblStatus.Text = $"成功导入 {importCount} 条账单记录";
                            MessageBox.Show($"成功导入 {importCount} 条账单记录", "导入成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            MessageBox.Show("未能解析任何账单记录\n\n请检查格式是否正确:\n昵称(旺旺号)=分数\n或 昵称=分数", "导入失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[MainForm] Import bill error: {ex.Message}");
                MessageBox.Show($"导入账单失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void btnSendBill_Click(object sender, EventArgs e)
        {
            try
            {
                if (!ChatService.Instance.IsConnected)
                {
                    MessageBox.Show("未连接到旺商聊，请先连接", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                
                // Get current session info (teamId)
                var sessionInfo = await ChatService.Instance.GetCurrentSessionInfoAsync();
                var teamId = sessionInfo.TeamId;
                
                // Fallback: try to get from account config
                if (string.IsNullOrEmpty(teamId))
                {
                    var account = AccountService.Instance.CurrentAccount;
                    teamId = account?.GroupId;
                }
                
                // Last resort: try config
                if (string.IsNullOrEmpty(teamId))
                {
                    teamId = ConfigService.Instance.Config?.GroupId;
                }
                
                if (string.IsNullOrEmpty(teamId))
                {
                    MessageBox.Show("无法获取当前群ID，请确保已打开群聊窗口", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                
                // Get period (next period for pre-bet, or current period)
                var period = LotteryService.Instance.NextPeriod;
                if (string.IsNullOrEmpty(period))
                    period = LotteryService.Instance.CurrentPeriod ?? "";
                    
                if (string.IsNullOrEmpty(period))
                {
                    MessageBox.Show("无法获取当前期号", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                
                // Read bets for current group and period
                var bets = Services.Betting.BetLedgerService.Instance.ReadBets(DateTime.Today, teamId, period);
                
                if (bets == null || bets.Count == 0)
                {
                    MessageBox.Show($"当前期 {period} 暂无下注记录", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                
                // Use TemplateEngine to render bill with template from settings
                // Default template uses [下注核对] which renders bet check list sorted by score
                var template = $"📋 第 {period} 期 下注核对\n[日期] [时间]\n━━━━━━━━━━━━━━━\n[下注核对]\n━━━━━━━━━━━━━━━";
                
                // Create render context with group message info for TemplateEngine to resolve teamId
                var ctx = new TemplateEngine.RenderContext
                {
                    Today = DateTime.Today,
                    Message = new Models.ChatMessage
                    {
                        IsGroupMessage = true,
                        GroupId = teamId
                    }
                };
                
                var billText = TemplateEngine.Render(template, ctx);
                
                // Append summary
                var playerCount = bets.GroupBy(b => b.PlayerId).Count();
                var totalStake = bets.Sum(b => b.TotalAmount);
                billText += $"\n共 {playerCount} 人下注，总注额: {totalStake}";
                
                // Send to group
                lblStatus.Text = "正在发送账单...";
                btnSendBill.Enabled = false;
                
                var result = await ChatService.Instance.SendTextAsync("team", teamId, billText);
                
                if (result.Success)
                {
                    lblStatus.Text = $"账单已发送 (期号:{period})";
                    Logger.Info($"[MainForm] Bill sent to group {teamId} for period {period}");
                }
                else
                {
                    lblStatus.Text = $"账单发送失败: {result.Message}";
                    MessageBox.Show($"发送失败: {result.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"发送账单异常: {ex.Message}";
                Logger.Error($"[MainForm] Send bill error: {ex.Message}");
                MessageBox.Show($"发送异常: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnSendBill.Enabled = true;
            }
        }

        private void btnCopyBill_Click(object sender, EventArgs e)
        {
            try
            {
                // Get teamId from various sources
                var teamId = ConfigService.Instance.Config?.GroupId ?? "";
                if (string.IsNullOrEmpty(teamId))
                {
                    var account = AccountService.Instance.CurrentAccount;
                    teamId = account?.GroupId ?? "";
                }
                
                // Get period (next period for pre-bet, or current period)
                var period = LotteryService.Instance.NextPeriod;
                if (string.IsNullOrEmpty(period))
                    period = LotteryService.Instance.CurrentPeriod ?? "";
                
                // Build bill content using template engine
                // Template includes: period, date, time, bet check, player count, total amount
                var template = $"📋 第 {period} 期 下注核对\n[日期] [时间]\n━━━━━━━━━━━━━━━\n[下注核对]\n━━━━━━━━━━━━━━━\n共 [客户人数] 人下注";
                
                // Create a dummy message with GroupId to pass teamId context
                var dummyMsg = new ChatMessage { IsGroupMessage = true, GroupId = teamId };
                
                var billContent = TemplateEngine.Render(template, new TemplateEngine.RenderContext
                {
                    Today = DateTime.Today,
                    Message = dummyMsg,
                });
                
                if (string.IsNullOrWhiteSpace(billContent) || !billContent.Contains("期"))
                {
                    // Try alternative - just get raw bill
                    template = "[账单2]";
                    billContent = TemplateEngine.Render(template, new TemplateEngine.RenderContext
                    {
                        Today = DateTime.Today,
                        Message = dummyMsg,
                    });
                }
                
                if (string.IsNullOrWhiteSpace(billContent))
                {
                    MessageBox.Show("当前没有账单内容可复制\n\n请确保有下注记录或开奖数据", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                
                Clipboard.SetText(billContent);
                lblStatus.Text = "账单已复制到剪贴板";
                MessageBox.Show("账单已复制到剪贴板", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Logger.Error($"[MainForm] Copy bill error: {ex.Message}");
                MessageBox.Show($"复制账单失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnClearBet_Click(object sender, EventArgs e)
        {
            try
            {
                // Get current period info for display
                var period = LotteryService.Instance.NextPeriod;
                if (string.IsNullOrEmpty(period))
                    period = LotteryService.Instance.CurrentPeriod ?? "";
                
                var teamId = ConfigService.Instance.Config?.GroupId ?? "";
                if (string.IsNullOrEmpty(teamId))
                {
                    var account = AccountService.Instance.CurrentAccount;
                    teamId = account?.GroupId ?? "";
                }
                
                // Get current bet count for confirmation
                var bets = Services.Betting.BetLedgerService.Instance.ReadBets(DateTime.Today, teamId, period);
                var betCount = bets?.Count ?? 0;
                var playerCount = bets?.Select(b => b.PlayerId).Distinct().Count() ?? 0;
                
                // Ask user what to clear
                var msg = $"当前期号: {period}\n下注人数: {playerCount} 人\n下注记录: {betCount} 条\n\n请选择清空范围:\n\n" +
                          "[是] 只清空当期下注\n[否] 清空今日全部下注\n[取消] 不清空";
                
                var result = MessageBox.Show(msg, "清空下注确认", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                
                if (result == DialogResult.Cancel)
                    return;
                
                int cleared;
                if (result == DialogResult.Yes)
                {
                    // Clear current period only
                    cleared = Services.Betting.BetLedgerService.Instance.ClearBets(DateTime.Today, teamId, period);
                    lblStatus.Text = $"已清空第 {period} 期下注记录";
                    MessageBox.Show($"已清空第 {period} 期下注记录\n\n清空文件数: {cleared}", "清空完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    // Clear all today's bets
                    cleared = Services.Betting.BetLedgerService.Instance.ClearTodayBets();
                    lblStatus.Text = "已清空今日全部下注记录";
                    MessageBox.Show($"已清空今日全部下注记录\n\n清空文件数: {cleared}", "清空完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[MainForm] Clear bet error: {ex.Message}");
                MessageBox.Show($"清空下注失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnClearZero_Click(object sender, EventArgs e)
        {
            try
            {
                var players = DataService.Instance.GetAllPlayers();
                if (players == null || players.Count == 0)
                {
                    MessageBox.Show("当前没有玩家数据", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                
                var zeroCount = players.Count(p => p.Score == 0);
                var totalScore = players.Sum(p => p.Score);
                
                var msg = $"当前玩家总数: {players.Count} 人\n" +
                          $"零分玩家数: {zeroCount} 人\n" +
                          $"总分数: {totalScore}\n\n" +
                          "请选择操作:\n\n" +
                          "[是] 删除零分玩家\n" +
                          "[否] 所有玩家分数清零\n" +
                          "[取消] 不操作";
                
                var result = MessageBox.Show(msg, "清除零分确认", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                
                if (result == DialogResult.Cancel)
                    return;
                
                if (result == DialogResult.Yes)
                {
                    // Remove zero score players
                    var removed = 0;
                    foreach (var p in players.Where(p => p.Score == 0).ToList())
                    {
                        DataService.Instance.DeletePlayer(p.WangWangId);
                        removed++;
                    }
                    lblStatus.Text = $"已删除 {removed} 个零分玩家";
                    MessageBox.Show($"已删除 {removed} 个零分玩家", "清除完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    // Reset all scores to zero
                    var resetCount = 0;
                    foreach (var p in players)
                    {
                        p.Score = 0;
                        DataService.Instance.SavePlayer(p);
                        resetCount++;
                    }
                    lblStatus.Text = $"已将 {resetCount} 个玩家分数清零";
                    MessageBox.Show($"已将 {resetCount} 个玩家分数清零", "清除完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[MainForm] Clear zero error: {ex.Message}");
                MessageBox.Show($"清除失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnBetSummary_Click(object sender, EventArgs e)
        {
            try
            {
                // Get teamId from various sources
                var teamId = ConfigService.Instance.Config?.GroupId ?? "";
                if (string.IsNullOrEmpty(teamId))
                {
                    var account = AccountService.Instance.CurrentAccount;
                    teamId = account?.GroupId ?? "";
                }
                
                // Get period (next period for pre-bet, or current period)
                var period = LotteryService.Instance.NextPeriod;
                if (string.IsNullOrEmpty(period))
                    period = LotteryService.Instance.CurrentPeriod ?? "";
                
                if (string.IsNullOrEmpty(period))
                {
                    MessageBox.Show("无法获取当前期号", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                
                // Read bets for current period
                var bets = Services.Betting.BetLedgerService.Instance.ReadBets(DateTime.Today, teamId, period);
                
                if (bets == null || bets.Count == 0)
                {
                    MessageBox.Show($"第 {period} 期暂无下注记录", "下注汇总", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                
                // Calculate summary
                var playerCount = bets.Select(b => b.PlayerId).Distinct().Count();
                var totalAmount = bets.Sum(b => b.TotalAmount);
                var betCount = bets.Count;
                
                // Group by player for detailed view
                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"📊 第 {period} 期 下注汇总");
                sb.AppendLine($"━━━━━━━━━━━━━━━");
                sb.AppendLine($"下注人数: {playerCount} 人");
                sb.AppendLine($"下注笔数: {betCount} 笔");
                sb.AppendLine($"总注额: {totalAmount}");
                sb.AppendLine($"━━━━━━━━━━━━━━━");
                sb.AppendLine("下注详情:");
                
                foreach (var g in bets.GroupBy(b => b.PlayerId).OrderByDescending(g => g.Sum(x => x.TotalAmount)))
                {
                    var playerId = g.Key ?? "";
                    var nick = g.Select(x => x.PlayerNick).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? "玩家";
                    var playerTotal = g.Sum(x => x.TotalAmount);
                    var betTexts = string.Join(" ", g.Select(x => x.RawText).Where(x => !string.IsNullOrWhiteSpace(x)));
                    
                    // Show first 4 digits of ID
                    var shortId = playerId.Length >= 4 ? playerId.Substring(0, 4) : playerId;
                    sb.AppendLine($"  {nick}({shortId}): {betTexts} = {playerTotal}");
                }
                
                var summaryText = sb.ToString();
                
                // Show in message box and offer to copy or send
                var result = MessageBox.Show(
                    summaryText + "\n\n[是] 发送到群 | [否] 复制到剪贴板 | [取消] 关闭", 
                    "下注汇总", 
                    MessageBoxButtons.YesNoCancel, 
                    MessageBoxIcon.Information);
                
                if (result == DialogResult.Yes)
                {
                    // Send to group
                    if (ChatService.Instance.IsConnected && !string.IsNullOrEmpty(teamId))
                    {
                        _ = SendBetSummaryToGroupAsync(teamId, period, summaryText);
                    }
                    else
                    {
                        MessageBox.Show("未连接或无群ID，无法发送", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                else if (result == DialogResult.No)
                {
                    Clipboard.SetText(summaryText);
                    lblStatus.Text = "下注汇总已复制到剪贴板";
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[MainForm] Bet summary error: {ex.Message}");
                MessageBox.Show($"获取下注汇总失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async System.Threading.Tasks.Task SendBetSummaryToGroupAsync(string teamId, string period, string summaryText)
        {
            try
            {
                lblStatus.Text = "正在发送下注汇总...";
                var sendResult = await ChatService.Instance.SendTextAsync("team", teamId, summaryText);
                
                if (sendResult.Success)
                {
                    lblStatus.Text = "下注汇总已发送到群";
                    await SendFeedbackAsync("下注汇总", $"第{period}期下注汇总已发送");
                }
                else
                {
                    lblStatus.Text = "下注汇总发送失败";
                    MessageBox.Show($"发送失败: {sendResult.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[MainForm] SendBetSummaryToGroupAsync error: {ex.Message}");
                lblStatus.Text = "下注汇总发送异常";
            }
        }

        private void btnDetailProfit_Click(object sender, EventArgs e)
        {
            try
            {
                // Get teamId from various sources
                var teamId = ConfigService.Instance.Config?.GroupId ?? "";
                if (string.IsNullOrEmpty(teamId))
                {
                    var account = AccountService.Instance.CurrentAccount;
                    teamId = account?.GroupId ?? "";
                }
                
                // Get current period (latest result)
                var period = LotteryService.Instance.CurrentPeriod ?? "";
                
                if (string.IsNullOrEmpty(period))
                {
                    MessageBox.Show("无法获取当前期号", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                
                // Read settlement data for current period
                var settlementText = Services.Betting.BetSettlementService.Instance.ReadWinnersText(DateTime.Today, teamId, period);
                
                if (string.IsNullOrWhiteSpace(settlementText))
                {
                    // No settlement yet, show bets and calculate expected profit
                    var bets = Services.Betting.BetLedgerService.Instance.ReadBets(DateTime.Today, teamId, period);
                    
                    if (bets == null || bets.Count == 0)
                    {
                        MessageBox.Show($"第 {period} 期暂无盈利数据\n\n可能原因:\n1. 当期尚未开奖结算\n2. 当期没有下注记录", "详细盈利", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                    
                    // Show pre-settlement view (bet data without result)
                    var sb = new System.Text.StringBuilder();
                    sb.AppendLine($"📈 第 {period} 期 盈利详情");
                    sb.AppendLine($"⏳ 尚未开奖结算");
                    sb.AppendLine($"━━━━━━━━━━━━━━━");
                    sb.AppendLine($"当前下注人数: {bets.Select(b => b.PlayerId).Distinct().Count()} 人");
                    sb.AppendLine($"当前总注额: {bets.Sum(b => b.TotalAmount)}");
                    sb.AppendLine($"━━━━━━━━━━━━━━━");
                    sb.AppendLine("下注玩家:");
                    
                    foreach (var g in bets.GroupBy(b => b.PlayerId))
                    {
                        var playerId = g.Key ?? "";
                        var nick = g.Select(x => x.PlayerNick).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? "玩家";
                        var player = DataService.Instance.GetPlayer(playerId);
                        var currentScore = player?.Score ?? 0m;
                        var playerTotal = g.Sum(x => x.TotalAmount);
                        var shortId = playerId.Length >= 4 ? playerId.Substring(0, 4) : playerId;
                        sb.AppendLine($"  {nick}({shortId}) 下注:{playerTotal} 当前分:{currentScore}");
                    }
                    
                    var preText = sb.ToString();
                    var result = MessageBox.Show(
                        preText + "\n\n点击[是]复制到剪贴板",
                        "详细盈利 (未结算)",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Information);
                    
                    if (result == DialogResult.Yes)
                    {
                        Clipboard.SetText(preText);
                        lblStatus.Text = "盈利详情已复制到剪贴板";
                    }
                    return;
                }
                
                // Show settlement result
                var sb2 = new System.Text.StringBuilder();
                sb2.AppendLine($"📈 第 {period} 期 详细盈利");
                sb2.AppendLine($"━━━━━━━━━━━━━━━");
                sb2.AppendLine(settlementText);
                
                var detailText = sb2.ToString();
                var result2 = MessageBox.Show(
                    detailText + "\n\n点击[是]复制到剪贴板",
                    "详细盈利",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);
                
                if (result2 == DialogResult.Yes)
                {
                    Clipboard.SetText(detailText);
                    lblStatus.Text = "盈利详情已复制到剪贴板";
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[MainForm] Detail profit error: {ex.Message}");
                MessageBox.Show($"获取详细盈利失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnDeleteBill_Click(object sender, EventArgs e)
        {
            try
            {
                // Get teamId from various sources
                var teamId = ConfigService.Instance.Config?.GroupId ?? "";
                if (string.IsNullOrEmpty(teamId))
                {
                    var account = AccountService.Instance.CurrentAccount;
                    teamId = account?.GroupId ?? "";
                }
                
                // Get current period
                var period = LotteryService.Instance.NextPeriod;
                if (string.IsNullOrEmpty(period))
                    period = LotteryService.Instance.CurrentPeriod ?? "";
                
                if (string.IsNullOrEmpty(period))
                {
                    MessageBox.Show("无法获取当前期号", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                
                // Get current bet count for display
                var bets = Services.Betting.BetLedgerService.Instance.ReadBets(DateTime.Today, teamId, period);
                var betCount = bets?.Count ?? 0;
                var playerCount = bets?.Select(b => b.PlayerId).Distinct().Count() ?? 0;
                var totalAmount = bets?.Sum(b => b.TotalAmount) ?? 0;
                
                // Confirm deletion
                var msg = $"确定要删除本期账单吗？\n\n" +
                          $"期号: {period}\n" +
                          $"下注人数: {playerCount} 人\n" +
                          $"下注记录: {betCount} 条\n" +
                          $"总注额: {totalAmount}\n\n" +
                          "此操作将删除:\n" +
                          "• 本期下注记录\n" +
                          "• 本期结算数据";
                
                var result = MessageBox.Show(msg, "删除账单确认", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                
                if (result != DialogResult.Yes)
                    return;
                
                // Delete bet records
                var deletedBets = Services.Betting.BetLedgerService.Instance.ClearBets(DateTime.Today, teamId, period);
                
                // Delete settlement file
                var settlementDir = System.IO.Path.Combine(DataService.Instance.DatabaseDir, "Bets", DateTime.Today.ToString("yyyy-MM-dd"), teamId ?? "unknown-team");
                var settlementFile = System.IO.Path.Combine(settlementDir, $"settle-{period}.txt");
                var deletedSettlement = false;
                if (System.IO.File.Exists(settlementFile))
                {
                    System.IO.File.Delete(settlementFile);
                    deletedSettlement = true;
                }
                
                lblStatus.Text = $"已删除第 {period} 期账单";
                MessageBox.Show(
                    $"第 {period} 期账单已删除\n\n" +
                    $"删除下注文件: {deletedBets} 个\n" +
                    $"删除结算文件: {(deletedSettlement ? "是" : "否")}",
                    "删除完成",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                
                Logger.Info($"[MainForm] Deleted bill for period {period}: {deletedBets} bet files, settlement={deletedSettlement}");
            }
            catch (Exception ex)
            {
                Logger.Error($"[MainForm] Delete bill error: {ex.Message}");
                MessageBox.Show($"删除账单失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnHistoryBill_Click(object sender, EventArgs e)
        {
            try
            {
                // Read lottery history file
                var historyFile = DataService.Instance.GetLotteryHistoryFile(DateTime.Today);
                var historyLines = new List<string>();
                
                if (System.IO.File.Exists(historyFile))
                {
                    historyLines = System.IO.File.ReadAllLines(historyFile, System.Text.Encoding.UTF8)
                        .Where(l => !string.IsNullOrWhiteSpace(l))
                        .ToList();
                }
                
                if (historyLines.Count == 0)
                {
                    MessageBox.Show("今日暂无开奖历史记录", "历史账单", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                
                // Create history dialog
                using (var form = new Form())
                {
                    form.Text = $"开奖历史 - {DateTime.Today:yyyy-MM-dd}";
                    form.Size = new System.Drawing.Size(450, 500);
                    form.FormBorderStyle = FormBorderStyle.Sizable;
                    form.StartPosition = FormStartPosition.CenterParent;
                    form.MinimumSize = new System.Drawing.Size(350, 300);
                    
                    // Info panel at top
                    var pnlInfo = new Panel();
                    pnlInfo.Dock = DockStyle.Top;
                    pnlInfo.Height = 50;
                    pnlInfo.Padding = new Padding(10);
                    
                    var lblInfo = new Label();
                    lblInfo.Text = $"📊 今日共开奖 {historyLines.Count} 期\n最新: {(historyLines.Count > 0 ? historyLines.Last() : "")}";
                    lblInfo.Dock = DockStyle.Fill;
                    lblInfo.Font = new System.Drawing.Font("Microsoft YaHei", 9F);
                    pnlInfo.Controls.Add(lblInfo);
                    form.Controls.Add(pnlInfo);
                    
                    // History list (show newest first)
                    var listBox = new ListBox();
                    listBox.Dock = DockStyle.Fill;
                    listBox.Font = new System.Drawing.Font("Consolas", 10F);
                    listBox.IntegralHeight = false;
                    
                    // Add items in reverse order (newest first)
                    for (int i = historyLines.Count - 1; i >= 0; i--)
                    {
                        listBox.Items.Add(historyLines[i]);
                    }
                    form.Controls.Add(listBox);
                    
                    // Ensure listBox is below pnlInfo
                    listBox.BringToFront();
                    pnlInfo.BringToFront();
                    
                    // Button panel at bottom
                    var pnlButtons = new Panel();
                    pnlButtons.Dock = DockStyle.Bottom;
                    pnlButtons.Height = 45;
                    pnlButtons.Padding = new Padding(10);
                    
                    var btnCopy = new Button();
                    btnCopy.Text = "复制全部";
                    btnCopy.Location = new System.Drawing.Point(10, 8);
                    btnCopy.Size = new System.Drawing.Size(80, 28);
                    btnCopy.Click += (s, args) =>
                    {
                        var content = string.Join(Environment.NewLine, historyLines);
                        Clipboard.SetText(content);
                        MessageBox.Show("已复制到剪贴板", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    };
                    pnlButtons.Controls.Add(btnCopy);
                    
                    var btnCopySelected = new Button();
                    btnCopySelected.Text = "复制选中";
                    btnCopySelected.Location = new System.Drawing.Point(100, 8);
                    btnCopySelected.Size = new System.Drawing.Size(80, 28);
                    btnCopySelected.Click += (s, args) =>
                    {
                        if (listBox.SelectedItem != null)
                        {
                            Clipboard.SetText(listBox.SelectedItem.ToString());
                            MessageBox.Show("已复制选中项", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    };
                    pnlButtons.Controls.Add(btnCopySelected);
                    
                    var btnYesterday = new Button();
                    btnYesterday.Text = "昨日记录";
                    btnYesterday.Location = new System.Drawing.Point(190, 8);
                    btnYesterday.Size = new System.Drawing.Size(80, 28);
                    btnYesterday.Click += (s, args) =>
                    {
                        var yesterdayFile = DataService.Instance.GetLotteryHistoryFile(DateTime.Today.AddDays(-1));
                        if (System.IO.File.Exists(yesterdayFile))
                        {
                            var yesterdayLines = System.IO.File.ReadAllLines(yesterdayFile, System.Text.Encoding.UTF8)
                                .Where(l => !string.IsNullOrWhiteSpace(l))
                                .ToList();
                            
                            if (yesterdayLines.Count > 0)
                            {
                                listBox.Items.Clear();
                                for (int i = yesterdayLines.Count - 1; i >= 0; i--)
                                {
                                    listBox.Items.Add(yesterdayLines[i]);
                                }
                                lblInfo.Text = $"📊 昨日共开奖 {yesterdayLines.Count} 期\n最新: {yesterdayLines.Last()}";
                                form.Text = $"开奖历史 - {DateTime.Today.AddDays(-1):yyyy-MM-dd}";
                            }
                            else
                            {
                                MessageBox.Show("昨日无开奖记录", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                        }
                        else
                        {
                            MessageBox.Show("昨日无开奖记录", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    };
                    pnlButtons.Controls.Add(btnYesterday);
                    
                    var btnClose = new Button();
                    btnClose.Text = "关闭";
                    btnClose.Location = new System.Drawing.Point(350, 8);
                    btnClose.Size = new System.Drawing.Size(70, 28);
                    btnClose.DialogResult = DialogResult.OK;
                    pnlButtons.Controls.Add(btnClose);
                    
                    form.Controls.Add(pnlButtons);
                    form.AcceptButton = btnClose;
                    
                    form.ShowDialog(this);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[MainForm] History bill error: {ex.Message}");
                MessageBox.Show($"获取开奖历史失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void btnMuteAll_Click(object sender, EventArgs e)
        {
            lblStatus.Text = "正在执行全体禁言...";
            
            // 检查副框架连接状态（主框架通过副框架执行操作）
            var frameworkClient = Services.HPSocket.FrameworkClient.Instance;
            if (!frameworkClient.IsConnected)
            {
                MessageBox.Show("请先连接副框架！\n\n副框架（招财狗框架）需要先启动并连接旺商聊", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            // 获取群号
            string groupId = ConfigService.Instance.Config?.GroupId;
            if (string.IsNullOrEmpty(groupId))
            {
                MessageBox.Show("请先设置绑定群号！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            Logger.Info($"[全体禁言] 发送禁言指令到副框架，群号: {groupId}");
            
            // 发送禁言指令到副框架
            var result = await frameworkClient.SendGroupOperationAsync("mute_all", groupId, null);
            
            if (result.Success)
            {
                lblStatus.Text = "全体禁言成功";
                Logger.Info($"[全体禁言] 执行成功");
            }
            else
            {
                lblStatus.Text = $"禁言失败: {result.Message}";
                Logger.Error($"[全体禁言] 执行失败: {result.Message}");
            }
        }

        private async void btnUnmuteAll_Click(object sender, EventArgs e)
        {
            lblStatus.Text = "正在执行全体解禁...";
            
            // 检查副框架连接状态（主框架通过副框架执行操作）
            var frameworkClient = Services.HPSocket.FrameworkClient.Instance;
            if (!frameworkClient.IsConnected)
            {
                MessageBox.Show("请先连接副框架！\n\n副框架（招财狗框架）需要先启动并连接旺商聊", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            // 获取群号
            string groupId = ConfigService.Instance.Config?.GroupId;
            if (string.IsNullOrEmpty(groupId))
            {
                MessageBox.Show("请先设置绑定群号！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            Logger.Info($"[全体解禁] 发送解禁指令到副框架，群号: {groupId}");
            
            // 发送解禁指令到副框架
            var result = await frameworkClient.SendGroupOperationAsync("unmute_all", groupId, null);
            
            if (result.Success)
            {
                lblStatus.Text = "全体解禁成功";
                Logger.Info($"[全体解禁] 执行成功");
            }
            else
            {
                lblStatus.Text = $"解禁失败: {result.Message}";
                Logger.Error($"[全体解禁] 执行失败: {result.Message}");
            }
        }

        private void btnExportBill_Click(object sender, EventArgs e)
        {
            try
            {
                // Get teamId from various sources
                var teamId = ConfigService.Instance.Config?.GroupId ?? "";
                if (string.IsNullOrEmpty(teamId))
                {
                    var account = AccountService.Instance.CurrentAccount;
                    teamId = account?.GroupId ?? "";
                }
                
                // Get current period info
                var period = LotteryService.Instance.CurrentPeriod ?? "";
                var nextPeriod = LotteryService.Instance.NextPeriod ?? "";
                var num1 = LotteryService.Instance.Number1;
                var num2 = LotteryService.Instance.Number2;
                var num3 = LotteryService.Instance.Number3;
                var sum = LotteryService.Instance.Sum;
                
                // Create a dummy message with GroupId to pass teamId context
                var dummyMsg = new ChatMessage { IsGroupMessage = true, GroupId = teamId };
                
                // Build full bill content using template - includes lottery result, bets, and settlement
                var template = "📊 第 [期数] 期 账单\n" +
                              "[日期] [时间]\n" +
                              "━━━━━━━━━━━━━━━\n" +
                              "开奖号码: " + num1 + "+" + num2 + "+" + num3 + "=" + sum + "\n" +
                              "━━━━━━━━━━━━━━━\n" +
                              "[账单]\n" +
                              "━━━━━━━━━━━━━━━\n" +
                              "本期盈利: [本期盈利]";
                
                var billContent = TemplateEngine.Render(template, new TemplateEngine.RenderContext
                {
                    Today = DateTime.Today,
                    Message = dummyMsg,
                });
                
                // If no bill content, try alternative templates
                if (string.IsNullOrWhiteSpace(billContent) || billContent.Contains("[账单]"))
                {
                    // Try account bill template
                    template = "[账单2]";
                    billContent = TemplateEngine.Render(template, new TemplateEngine.RenderContext
                    {
                        Today = DateTime.Today,
                        Message = dummyMsg,
                    });
                }
                
                if (string.IsNullOrWhiteSpace(billContent))
                {
                    // Fallback: generate bill from player data
                    var players = DataService.Instance.GetAllPlayers();
                    if (players != null && players.Count > 0)
                    {
                        var sb = new System.Text.StringBuilder();
                        sb.AppendLine($"📊 第 {period} 期 账单");
                        sb.AppendLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                        sb.AppendLine("━━━━━━━━━━━━━━━");
                        sb.AppendLine($"开奖号码: {num1}+{num2}+{num3}={sum}");
                        sb.AppendLine("━━━━━━━━━━━━━━━");
                        
                        decimal totalScore = 0;
                        foreach (var p in players.OrderByDescending(x => Math.Abs(x.Score)))
                        {
                            var scoreStr = p.Score >= 0 ? $"+{p.Score}" : $"{p.Score}";
                            sb.AppendLine($"{p.Nickname ?? p.WangWangId}({p.WangWangId})={scoreStr}");
                            totalScore += p.Score;
                        }
                        
                        sb.AppendLine("━━━━━━━━━━━━━━━");
                        var profitStr = totalScore >= 0 ? $"+{totalScore}" : $"{totalScore}";
                        sb.AppendLine($"共 {players.Count} 人 | 总分: {profitStr}");
                        
                        billContent = sb.ToString();
                    }
                    else
                    {
                        MessageBox.Show("当前没有账单内容可导出\n\n请确保有玩家数据或开奖记录", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                }
                
                Clipboard.SetText(billContent);
                lblStatus.Text = "账单已导出到剪贴板";
                MessageBox.Show($"账单已导出到剪贴板\n\n{(billContent.Length > 200 ? billContent.Substring(0, 200) + "..." : billContent)}", 
                              "导出成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Logger.Error($"[MainForm] Export bill error: {ex.Message}");
                MessageBox.Show($"导出账单失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void btnSendImage_Click(object sender, EventArgs e)
        {
            try
            {
                if (!ChatService.Instance.IsConnected)
                {
                    MessageBox.Show("未连接到旺商聊，请先连接", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                
                // Get current session info
                var sessionInfo = await ChatService.Instance.GetCurrentSessionInfoAsync();
                var scene = sessionInfo.Scene;
                var to = sessionInfo.TeamId;
                
                // Validate session
                if (string.IsNullOrEmpty(scene) || string.IsNullOrEmpty(to))
                {
                    // Try to get from account config
                    var account = AccountService.Instance.CurrentAccount;
                    if (account != null && !string.IsNullOrEmpty(account.GroupId))
                    {
                        scene = "team";
                        to = account.GroupId;
                    }
                    else
                    {
                        MessageBox.Show("无法获取当前会话信息\n\n请确保已打开聊天窗口", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }
                
                // Open file dialog to select image
                using (var openFileDialog = new OpenFileDialog())
                {
                    openFileDialog.Title = "选择要发送的图片";
                    openFileDialog.Filter = "图片文件 (*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.webp)|*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.webp|所有文件 (*.*)|*.*";
                    openFileDialog.FilterIndex = 1;
                    openFileDialog.RestoreDirectory = true;
                    
                    if (openFileDialog.ShowDialog() != DialogResult.OK)
                    {
                        return; // User cancelled
                    }
                    
                    var imagePath = openFileDialog.FileName;
                    var fileName = Path.GetFileName(imagePath);
                    var fileSize = new FileInfo(imagePath).Length;
                    
                    // Confirm send
                    var confirmMsg = $"确认发送图片？\n\n文件: {fileName}\n大小: {(fileSize / 1024.0):F1} KB\n目标: {(scene == "team" ? "群聊" : "私聊")} ({to})";
                    if (MessageBox.Show(confirmMsg, "确认发送", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                    {
                        return;
                    }
                    
                    // Update UI
                    btnSendImage.Enabled = false;
                    btnSendImage.Text = "发送中...";
                    lblStatus.Text = $"正在发送图片: {fileName}...";
                    
                    try
                    {
                        // Send image via ChatService
                        var (success, message, msgId) = await ChatService.Instance.SendImageAsync(scene, to, imagePath);
                        
                        if (success)
                        {
                            lblStatus.Text = $"图片发送成功: {fileName}";
                            MessageBox.Show($"图片发送成功！\n\n文件: {fileName}\n消息ID: {msgId ?? "N/A"}", 
                                          "发送成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            lblStatus.Text = $"图片发送失败: {message}";
                            MessageBox.Show($"图片发送失败\n\n原因: {message}", 
                                          "发送失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    finally
                    {
                        btnSendImage.Enabled = true;
                        btnSendImage.Text = "发送图片";
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[MainForm] Send image error: {ex.Message}");
                MessageBox.Show($"发送图片失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnSendImage.Enabled = true;
                btnSendImage.Text = "发送图片";
            }
        }

        private async System.Threading.Tasks.Task SendBetDataAfterDelayAsync(string period, int delaySeconds)
        {
            try
            {
                // Wait for specified delay
                await System.Threading.Tasks.Task.Delay(delaySeconds * 1000);
                
                var config = ConfigService.Instance.Config;
                var teamId = config.GroupId ?? "";
                
                if (string.IsNullOrWhiteSpace(teamId))
                {
                    Logger.Info("[MainForm] Bet data send skipped - no group ID");
                    return;
                }
                
                // Build bet check content using template
                var ctx = new TemplateEngine.RenderContext
                {
                    Message = new ChatMessage { GroupId = teamId },
                    Today = DateTime.Today
                };
                
                var betCheckContent = TemplateEngine.Render("[下注核对]", ctx);
                
                if (string.IsNullOrWhiteSpace(betCheckContent))
                {
                    Logger.Info($"[MainForm] No bet data for period {period}");
                    return;
                }
                
                Logger.Info($"[MainForm] Sending delayed bet data for period {period}");
                
                // Check if image send is enabled
                if (config.BetDataImageSend)
                {
                    // Generate and send image version of bet data
                    try
                    {
                        var imagePath = ImageGeneratorService.Instance.GenerateBetDataImage(period, betCheckContent);
                        if (!string.IsNullOrEmpty(imagePath) && System.IO.File.Exists(imagePath))
                        {
                            Logger.Info($"[MainForm] Bet data image generated: {imagePath}");
                            var imgResult = await ChatService.Instance.SendImageAsync("team", teamId, imagePath);
                            if (imgResult.Success)
                            {
                                Logger.Info("[MainForm] Delayed bet data image sent successfully");
                                await SendFeedbackAsync("下注核对", $"第{period}期下注数据(图片)已发送");
                                return; // Image sent, skip text version
                            }
                            else
                            {
                                Logger.Error($"[MainForm] Bet data image send failed: {imgResult.Message}, falling back to text");
                            }
                        }
                    }
                    catch (Exception imgEx)
                    {
                        Logger.Error($"[MainForm] Bet data image generation error: {imgEx.Message}, falling back to text");
                    }
                }
                
                // Send as text if image not enabled or failed
                var sendResult = await ChatService.Instance.SendTextAsync("team", teamId, $"【第{period}期下注数据】\n{betCheckContent}");
                if (sendResult.Success)
                {
                    Logger.Info("[MainForm] Delayed bet data sent successfully");
                    await SendFeedbackAsync("下注核对", $"第{period}期下注数据已发送");
                }
                else
                {
                    Logger.Error($"[MainForm] Delayed bet data send failed: {sendResult.Message}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[MainForm] SendBetDataAfterDelayAsync error: {ex.Message}");
            }
        }

        private async System.Threading.Tasks.Task SendGroupTaskBillAsync(string period, int playerCount, decimal totalProfit)
        {
            try
            {
                var config = ConfigService.Instance.Config;
                var teamId = config.GroupId ?? "";
                
                if (string.IsNullOrWhiteSpace(teamId))
                {
                    Logger.Info("[MainForm] Group task send skipped - no group ID");
                    return;
                }
                
                // Build bill content using template
                var ctx = new TemplateEngine.RenderContext
                {
                    Message = new ChatMessage { GroupId = teamId },
                    Today = DateTime.Today
                };
                
                var billContent = TemplateEngine.Render("[账单]", ctx);
                
                if (string.IsNullOrWhiteSpace(billContent))
                {
                    Logger.Info($"[MainForm] No bill data for period {period}");
                    return;
                }
                
                var groupTaskMsg = $"【第{period}期群作业】\n" +
                                   $"玩家数: {playerCount} | 总盈亏: {(totalProfit >= 0 ? "+" : "")}{totalProfit}\n" +
                                   $"{billContent}";
                
                Logger.Info($"[MainForm] Sending group task bill for period {period}");
                
                var sendResult = await ChatService.Instance.SendTextAsync("team", teamId, groupTaskMsg);
                if (sendResult.Success)
                {
                    Logger.Info("[MainForm] Group task bill sent successfully");
                    
                    // KeepRecent10Tasks - 只保留近10期群作业
                    if (config.KeepRecent10Tasks)
                    {
                        CleanupOldGroupTasks(teamId, 10);
                    }
                    
                    // Notify if enabled
                    if (config.GroupTaskNotify)
                    {
                        await SendFeedbackAsync("群作业", $"第{period}期群作业已发送");
                    }
                }
                else
                {
                    Logger.Error($"[MainForm] Group task bill send failed: {sendResult.Message}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[MainForm] SendGroupTaskBillAsync error: {ex.Message}");
            }
        }

        private void CleanupOldGroupTasks(string teamId, int keepCount)
        {
            try
            {
                var today = DateTime.Today;
                var betsDir = System.IO.Path.Combine(DataService.Instance.DatabaseDir, "Bets", today.ToString("yyyy-MM-dd"), teamId);
                
                if (!System.IO.Directory.Exists(betsDir))
                    return;
                
                // Get all settlement files
                var settleFiles = System.IO.Directory.GetFiles(betsDir, "settle-*.txt")
                    .OrderByDescending(f => f)
                    .ToList();
                
                // Keep only the most recent N files
                if (settleFiles.Count > keepCount)
                {
                    var filesToDelete = settleFiles.Skip(keepCount);
                    foreach (var file in filesToDelete)
                    {
                        try
                        {
                            System.IO.File.Delete(file);
                            Logger.Info($"[MainForm] Cleaned up old group task file: {System.IO.Path.GetFileName(file)}");
                        }
                        catch (Exception ex)
                        {
                            Logger.Error($"[MainForm] Failed to delete {file}: {ex.Message}");
                        }
                    }
                }
                
                // Also clean up old bet files for deleted settlements
                var betFiles = System.IO.Directory.GetFiles(betsDir, "bets-*.txt")
                    .OrderByDescending(f => f)
                    .ToList();
                
                if (betFiles.Count > keepCount)
                {
                    var betFilesToDelete = betFiles.Skip(keepCount);
                    foreach (var file in betFilesToDelete)
                    {
                        try
                        {
                            System.IO.File.Delete(file);
                        }
                        catch { /* ignore */ }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[MainForm] CleanupOldGroupTasks error: {ex.Message}");
            }
        }

        private async System.Threading.Tasks.Task SendFeedbackAsync(string type, string message)
        {
            try
            {
                var config = ConfigService.Instance.Config;
                
                // Check if feedback is enabled for this type
                bool shouldSend = false;
                switch (type)
                {
                    case "下注核对":
                        shouldSend = config.BetCheckFeedback;
                        break;
                    case "下注汇总":
                        shouldSend = config.BetSummaryFeedback;
                        break;
                    case "盈利":
                        shouldSend = config.ProfitFeedback;
                        break;
                    case "发送账单":
                    case "开奖发送":
                    case "群作业":
                        shouldSend = config.BillSendFeedback;
                        break;
                    default:
                        shouldSend = true; // Generic feedback
                        break;
                }
                
                if (!shouldSend)
                {
                    return;
                }
                
                var feedbackMsg = $"[{type}] {message}";
                
                // Send to WangWang (private chat)
                if (config.FeedbackToWangWang && !string.IsNullOrWhiteSpace(config.FeedbackWangWangId))
                {
                    var result = await ChatService.Instance.SendTextAsync("p2p", config.FeedbackWangWangId, feedbackMsg);
                    if (!result.Success)
                    {
                        Logger.Error($"[MainForm] Feedback to WangWang failed: {result.Message}");
                    }
                }
                
                // Send to Group
                if (config.FeedbackToGroup && !string.IsNullOrWhiteSpace(config.FeedbackGroupId))
                {
                    var result = await ChatService.Instance.SendTextAsync("team", config.FeedbackGroupId, feedbackMsg);
                    if (!result.Success)
                    {
                        Logger.Error($"[MainForm] Feedback to group failed: {result.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[MainForm] SendFeedbackAsync error: {ex.Message}");
            }
        }

        private void chkSupportNickChange_CheckedChanged(object sender, EventArgs e)
        {
            EnableAtNicknameUpdate = chkSupportNickChange.Checked;
            lblStatus.Text = chkSupportNickChange.Checked ? "艾特变昵称: 已启用" : "艾特变昵称: 已关闭";
            Logger.Info($"[MainForm] EnableAtNicknameUpdate = {EnableAtNicknameUpdate}");
        }

        private async void chkMuteGroup_CheckedChanged(object sender, EventArgs e)
        {
            if (_muteGroupChanging) return;
            
            try
            {
                _muteGroupChanging = true;
                
                if (!ChatService.Instance.IsConnected)
                {
                    MessageBox.Show("未连接到旺商聊，无法执行禁言操作", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    // Revert the checkbox
                    chkMuteGroup.Checked = !chkMuteGroup.Checked;
                    return;
                }
                
                var shouldMute = chkMuteGroup.Checked;
                lblStatus.Text = shouldMute ? "正在执行全体禁言..." : "正在执行全体解禁...";
                
                (bool Success, string GroupName, string Message) result;
                
                if (shouldMute)
                {
                    result = await ChatService.Instance.MuteAllAsync();
                }
                else
                {
                    result = await ChatService.Instance.UnmuteAllAsync();
                }
                
                if (result.Success)
                {
                    var action = shouldMute ? "全体禁言" : "全体解禁";
                    lblStatus.Text = $"{action}成功: {result.GroupName ?? "当前群"}";
                    Logger.Info($"[MainForm] {action} success: {result.GroupName}");
                }
                else
                {
                    var action = shouldMute ? "禁言" : "解禁";
                    lblStatus.Text = $"{action}失败: {result.Message}";
                    MessageBox.Show($"{action}失败: {result.Message}", "操作失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    
                    // Revert the checkbox on failure
                    chkMuteGroup.Checked = !shouldMute;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[MainForm] Mute group error: {ex.Message}");
                MessageBox.Show($"禁言操作失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                chkMuteGroup.Checked = !chkMuteGroup.Checked;
            }
            finally
            {
                _muteGroupChanging = false;
            }
        }

    }
}
