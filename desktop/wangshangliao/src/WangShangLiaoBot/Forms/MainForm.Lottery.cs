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
        private void InitializeLotteryService()
        {
            // Subscribe to lottery service events
            LotteryService.Instance.OnResultUpdated += OnLotteryResultUpdated;
            LotteryService.Instance.OnCountdownUpdated += OnCountdownUpdated;
            LotteryService.Instance.OnError += OnLotteryError;
            
            // Start lottery service
            LotteryService.Instance.Start();
        }

        private void OnLotteryResultUpdated(LotteryResult result)
        {
            if (InvokeRequired)
            {
                try { Invoke(new Action(() => OnLotteryResultUpdated(result))); }
                catch { }
                return;
            }
            
            // Update UI with new lottery result
            lblPeriodNumber.Text = result.Period;
            lblNextPeriodNumber.Text = result.NextPeriod;
            lblResult1.Text = result.Number1.ToString();
            lblResult2.Text = result.Number2.ToString();
            lblResult3.Text = result.Number3.ToString();
            lblResultSum.Text = result.Sum.ToString();
            
            lblStatus.Text = string.Format("开奖更新: 第{0}期 {1}+{2}+{3}={4}", result.Period, result.Number1, result.Number2, result.Number3, result.Sum);
        }

        private async System.Threading.Tasks.Task ExecuteAutoUnmuteAfterLotteryAsync(string period)
        {
            try
            {
                // 检查副框架连接
                if (!Services.HPSocket.FrameworkClient.Instance.IsConnected)
                {
                    Logger.Info("[MainForm] Auto unmute skipped - framework not connected");
                    return;
                }
                
                var groupId = AccountService.Instance.CurrentAccount?.GroupId ?? "";
                if (string.IsNullOrEmpty(groupId))
                {
                    Logger.Info("[MainForm] Auto unmute skipped - no group ID");
                    return;
                }
                
                Logger.Info($"[MainForm] Auto unmute triggered after lottery period {period}, group={groupId}");
                
                // Small delay to allow result message to be sent first
                await System.Threading.Tasks.Task.Delay(1000);
                
                // 通过副框架执行自动解禁
                var (success, message) = await Services.HPSocket.FrameworkClient.Instance.UnmuteAllViaFrameworkAsync(groupId, true);
                if (success)
                {
                    Logger.Info("[MainForm] Auto unmute after lottery - request sent to framework");
                    
                    // Update UI on main thread
                    if (InvokeRequired)
                    {
                        Invoke(new Action(() => {
                            _muteGroupChanging = true;
                            chkMuteGroup.Checked = false;
                            _muteGroupChanging = false;
                            lblStatus.Text = $"第 {period} 期开奖后自动解禁请求已发送";
                        }));
                    }
                    else
                    {
                        _muteGroupChanging = true;
                        chkMuteGroup.Checked = false;
                        _muteGroupChanging = false;
                        lblStatus.Text = $"第 {period} 期开奖后自动解禁请求已发送";
                    }
                }
                else
                {
                    Logger.Error($"[MainForm] Auto unmute after lottery failed: {message}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[MainForm] Auto unmute error: {ex.Message}");
            }
        }

        private async System.Threading.Tasks.Task SendLotteryNotifyAsync(LotteryResult result)
        {
            try
            {
                // 检查是否可以通过任一方式发送消息 (CDP 或 副框架)
                if (!ChatService.Instance.CanSendMessage)
                {
                    Logger.Info("[MainForm] Lottery notify skipped - no connection available (CDP or Framework)");
                    return;
                }
                
                var config = ConfigService.Instance.Config;
                var teamId = config.GroupId ?? "";
                
                if (string.IsNullOrWhiteSpace(teamId))
                {
                    Logger.Info("[MainForm] Lottery notify skipped - no group ID configured");
                    return;
                }
                
                // Build lottery message using template or default format
                string lotteryMsg;
                
                // Check if there's a custom template
                if (!string.IsNullOrWhiteSpace(config.LotterySendContent))
                {
                    // Use template engine to render custom content
                    var ctx = new TemplateEngine.RenderContext
                    {
                        Message = new ChatMessage { GroupId = teamId },
                        Today = DateTime.Today
                    };
                    lotteryMsg = TemplateEngine.Render(config.LotterySendContent, ctx);
                }
                else
                {
                    // Default format
                    var bs = result.Sum >= 14 ? "大" : "小";
                    var ds = result.Sum % 2 == 0 ? "双" : "单";
                    lotteryMsg = $"【第{result.Period}期开奖】\n" +
                                 $"{result.Number1}+{result.Number2}+{result.Number3}={result.Sum}\n" +
                                 $"[{bs}{ds}]";
                    
                    // Include 8 info if configured
                    if (config.LotteryWith8)
                    {
                        bool has8 = result.Number1 == 8 || result.Number2 == 8 || result.Number3 == 8;
                        if (has8) lotteryMsg += " 带8";
                    }
                }
                
                Logger.Info($"[MainForm] Sending lottery notify for period {result.Period}");
                
                // LotteryImageSend - 开奖图片发送功能
                if (config.LotteryImageSend)
                {
                    var imagePath = ImageGeneratorService.Instance.GenerateLotteryImage(
                        result.Period, result.Number1, result.Number2, result.Number3, result.Sum);
                    if (!string.IsNullOrEmpty(imagePath))
                    {
                        Logger.Info($"[MainForm] Lottery image generated: {imagePath}");
                        // Note: Image file sending requires NIM SDK file upload which is not yet implemented
                        // For now, send text message and log image path
                        lotteryMsg += $"\n[图片已生成: {System.IO.Path.GetFileName(imagePath)}]";
                    }
                }
                
                var sendResult = await ChatService.Instance.SendTextAsync("team", teamId, lotteryMsg);
                if (sendResult.Success)
                {
                    Logger.Info("[MainForm] Lottery notify sent successfully");
                    
                    // Send feedback if enabled
                    await SendFeedbackAsync("开奖发送", $"第{result.Period}期开奖通知已发送");
                }
                else
                {
                    Logger.Error($"[MainForm] Lottery notify failed: {sendResult.Message}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[MainForm] Lottery notify error: {ex.Message}");
            }
        }

        private void OnCountdownUpdated(int countdown)
        {
            if (InvokeRequired)
            {
                try { Invoke(new Action(() => OnCountdownUpdated(countdown))); }
                catch { }
                return;
            }
            
            lblCountdown.Text = countdown.ToString();
            
            // Change color based on countdown value
            if (countdown <= 10)
                lblCountdown.ForeColor = Color.Red;
            else
                lblCountdown.ForeColor = Color.Black;
            
            // 禁言提前功能 - Auto mute before lottery based on MuteBeforeSeconds setting
            var config = ConfigService.Instance.Config;
            var muteBeforeSeconds = config.MuteBeforeSeconds;
            var currentPeriod = LotteryService.Instance.CurrentPeriod ?? "";
            
            // Reset mute trigger for new period
            if (_lastMutedPeriod != currentPeriod)
            {
                _autoMuteTriggered = false;
                _lastMutedPeriod = currentPeriod;
            }
            
            // Check if we should auto-mute
            if (muteBeforeSeconds > 0 && countdown > 0 && countdown <= muteBeforeSeconds && !_autoMuteTriggered)
            {
                _autoMuteTriggered = true;
                _ = ExecuteAutoMuteBeforeLotteryAsync(countdown);
            }
        }

        private async System.Threading.Tasks.Task ExecuteAutoMuteBeforeLotteryAsync(int countdown)
        {
            try
            {
                // 检查副框架连接
                if (!Services.HPSocket.FrameworkClient.Instance.IsConnected)
                {
                    Logger.Info("[MainForm] Auto mute skipped - framework not connected");
                    return;
                }
                
                var groupId = AccountService.Instance.CurrentAccount?.GroupId ?? "";
                if (string.IsNullOrEmpty(groupId))
                {
                    Logger.Info("[MainForm] Auto mute skipped - no group ID");
                    return;
                }
                
                Logger.Info($"[MainForm] Auto mute triggered at countdown {countdown}s, group={groupId}");
                
                // 通过副框架执行自动禁言
                var (success, message) = await Services.HPSocket.FrameworkClient.Instance.MuteAllViaFrameworkAsync(groupId, true);
                if (success)
                {
                    Logger.Info("[MainForm] Auto mute before lottery - request sent to framework");
                    
                    // Update UI on main thread
                    if (InvokeRequired)
                    {
                        Invoke(new Action(() => {
                            _muteGroupChanging = true;
                            chkMuteGroup.Checked = true;
                            _muteGroupChanging = false;
                            lblStatus.Text = $"开奖前 {countdown}秒 自动禁言请求已发送";
                        }));
                    }
                    else
                    {
                        _muteGroupChanging = true;
                        chkMuteGroup.Checked = true;
                        _muteGroupChanging = false;
                        lblStatus.Text = $"开奖前 {countdown}秒 自动禁言请求已发送";
                    }
                }
                else
                {
                    Logger.Error($"[MainForm] Auto mute before lottery failed: {message}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[MainForm] Auto mute error: {ex.Message}");
            }
        }

        private void OnLotteryError(string error)
        {
            if (InvokeRequired)
            {
                try { Invoke(new Action(() => OnLotteryError(error))); }
                catch { }
                return;
            }
            
            lblStatus.Text = "开奖接口错误: " + error;
        }

        private void btnRefreshLottery_Click(object sender, EventArgs e)
        {
            // Check if settlement is running - must stop first
            if (Services.Betting.BetSettlementService.Instance.IsRunning)
            {
                MessageBox.Show("正在算账中，请先点击「停止算账」后再刷新开奖", 
                    "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            lblStatus.Text = "正在刷新开奖数据...";
            LotteryService.Instance.Refresh();
        }

        private void btnSyncTime_Click(object sender, EventArgs e)
        {
            try
            {
                var currentCountdown = LotteryService.Instance.Countdown;
                var currentPeriod = LotteryService.Instance.CurrentPeriod ?? "";
                var nextPeriod = LotteryService.Instance.NextPeriod ?? "";
                
                // Create calibration dialog
                using (var form = new Form())
                {
                    form.Text = "校准开奖时间";
                    form.Size = new System.Drawing.Size(320, 220);
                    form.FormBorderStyle = FormBorderStyle.FixedDialog;
                    form.StartPosition = FormStartPosition.CenterParent;
                    form.MaximizeBox = false;
                    form.MinimizeBox = false;
                    
                    // Current info
                    var lblInfo = new Label();
                    lblInfo.Text = $"当前期: {currentPeriod}\n下一期: {nextPeriod}\n当前倒计时: {currentCountdown} 秒";
                    lblInfo.Location = new System.Drawing.Point(15, 15);
                    lblInfo.Size = new System.Drawing.Size(280, 50);
                    form.Controls.Add(lblInfo);
                    
                    // Countdown input
                    var lblCountdown = new Label { Text = "设置倒计时(秒):", Location = new System.Drawing.Point(15, 75), AutoSize = true };
                    var numCountdown = new NumericUpDown 
                    { 
                        Minimum = 0, 
                        Maximum = 210, 
                        Location = new System.Drawing.Point(120, 72), 
                        Width = 80 
                    };
                    // Set Value AFTER Minimum/Maximum to avoid "Value out of range" error
                    numCountdown.Value = Math.Min(Math.Max(currentCountdown, 0), 210);
                    form.Controls.Add(lblCountdown);
                    form.Controls.Add(numCountdown);
                    
                    // Quick buttons
                    var lblQuick = new Label { Text = "快速设置:", Location = new System.Drawing.Point(15, 105), AutoSize = true };
                    form.Controls.Add(lblQuick);
                    
                    var btn30 = new Button { Text = "30秒", Location = new System.Drawing.Point(90, 102), Size = new System.Drawing.Size(50, 25) };
                    btn30.Click += (s, args) => numCountdown.Value = 30;
                    form.Controls.Add(btn30);
                    
                    var btn60 = new Button { Text = "60秒", Location = new System.Drawing.Point(145, 102), Size = new System.Drawing.Size(50, 25) };
                    btn60.Click += (s, args) => numCountdown.Value = 60;
                    form.Controls.Add(btn60);
                    
                    var btn120 = new Button { Text = "120秒", Location = new System.Drawing.Point(200, 102), Size = new System.Drawing.Size(55, 25) };
                    btn120.Click += (s, args) => numCountdown.Value = 120;
                    form.Controls.Add(btn120);
                    
                    var btn180 = new Button { Text = "180秒", Location = new System.Drawing.Point(260, 102), Size = new System.Drawing.Size(55, 25) };
                    btn180.Click += (s, args) => numCountdown.Value = 180;
                    form.Controls.Add(btn180);
                    
                    // Buttons
                    var btnOK = new Button { Text = "确定校准", DialogResult = DialogResult.OK, Location = new System.Drawing.Point(70, 145), Width = 80 };
                    var btnRefresh = new Button { Text = "刷新获取", Location = new System.Drawing.Point(160, 145), Width = 80 };
                    btnRefresh.Click += (s, args) =>
                    {
                        LotteryService.Instance.Refresh();
                        System.Threading.Thread.Sleep(500); // Wait for API response
                        numCountdown.Value = LotteryService.Instance.Countdown;
                        lblInfo.Text = $"当前期: {LotteryService.Instance.CurrentPeriod}\n下一期: {LotteryService.Instance.NextPeriod}\n当前倒计时: {LotteryService.Instance.Countdown} 秒";
                    };
                    form.Controls.Add(btnOK);
                    form.Controls.Add(btnRefresh);
                    form.AcceptButton = btnOK;
                    
                    if (form.ShowDialog(this) == DialogResult.OK)
                    {
                        var newCountdown = (int)numCountdown.Value;
                        
                        // Apply calibration
                        LotteryService.Instance.SetCountdown(newCountdown);
                        
                        lblStatus.Text = $"倒计时已校准: {currentCountdown}秒 -> {newCountdown}秒";
                        MessageBox.Show(
                            $"开奖时间已校准\n\n原倒计时: {currentCountdown} 秒\n新倒计时: {newCountdown} 秒",
                            "校准成功",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[MainForm] Sync time error: {ex.Message}");
                MessageBox.Show($"校准时间失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnFixLottery_Click(object sender, EventArgs e)
        {
            try
            {
                // Get current lottery info
                var currentPeriod = LotteryService.Instance.CurrentPeriod ?? "";
                var currentNum1 = LotteryService.Instance.Number1;
                var currentNum2 = LotteryService.Instance.Number2;
                var currentNum3 = LotteryService.Instance.Number3;
                var currentSum = LotteryService.Instance.Sum;
                
                // Create a custom input dialog
                using (var form = new Form())
                {
                    form.Text = "修正开奖数据";
                    form.Size = new System.Drawing.Size(350, 250);
                    form.FormBorderStyle = FormBorderStyle.FixedDialog;
                    form.StartPosition = FormStartPosition.CenterParent;
                    form.MaximizeBox = false;
                    form.MinimizeBox = false;
                    
                    // Current info label
                    var lblCurrent = new Label();
                    lblCurrent.Text = $"当前开奖: {currentPeriod}期\n取餐码: {currentNum1}+{currentNum2}+{currentNum3}={currentSum}";
                    lblCurrent.Location = new System.Drawing.Point(15, 15);
                    lblCurrent.Size = new System.Drawing.Size(300, 40);
                    form.Controls.Add(lblCurrent);
                    
                    // Period input
                    var lblPeriod = new Label { Text = "期号:", Location = new System.Drawing.Point(15, 65), AutoSize = true };
                    var txtPeriod = new TextBox { Text = currentPeriod, Location = new System.Drawing.Point(100, 62), Width = 150 };
                    form.Controls.Add(lblPeriod);
                    form.Controls.Add(txtPeriod);
                    
                    // Number 1 input
                    var lblNum1 = new Label { Text = "取餐码1:", Location = new System.Drawing.Point(15, 95), AutoSize = true };
                    var numNum1 = new NumericUpDown { Value = currentNum1, Minimum = 0, Maximum = 9, Location = new System.Drawing.Point(100, 92), Width = 60 };
                    form.Controls.Add(lblNum1);
                    form.Controls.Add(numNum1);
                    
                    // Number 2 input
                    var lblNum2 = new Label { Text = "取餐码2:", Location = new System.Drawing.Point(15, 125), AutoSize = true };
                    var numNum2 = new NumericUpDown { Value = currentNum2, Minimum = 0, Maximum = 9, Location = new System.Drawing.Point(100, 122), Width = 60 };
                    form.Controls.Add(lblNum2);
                    form.Controls.Add(numNum2);
                    
                    // Number 3 input
                    var lblNum3 = new Label { Text = "取餐码3:", Location = new System.Drawing.Point(15, 155), AutoSize = true };
                    var numNum3 = new NumericUpDown { Value = currentNum3, Minimum = 0, Maximum = 9, Location = new System.Drawing.Point(100, 152), Width = 60 };
                    form.Controls.Add(lblNum3);
                    form.Controls.Add(numNum3);
                    
                    // Sum preview label
                    var lblSum = new Label { Text = $"和值: {currentSum}", Location = new System.Drawing.Point(180, 125), AutoSize = true };
                    form.Controls.Add(lblSum);
                    
                    // Update sum preview when numbers change
                    EventHandler updateSum = (s, args) =>
                    {
                        var sum = (int)numNum1.Value + (int)numNum2.Value + (int)numNum3.Value;
                        lblSum.Text = $"和值: {sum}";
                    };
                    numNum1.ValueChanged += updateSum;
                    numNum2.ValueChanged += updateSum;
                    numNum3.ValueChanged += updateSum;
                    
                    // Buttons
                    var btnOK = new Button { Text = "确定修正", DialogResult = DialogResult.OK, Location = new System.Drawing.Point(80, 180), Width = 80 };
                    var btnCancel = new Button { Text = "取消", DialogResult = DialogResult.Cancel, Location = new System.Drawing.Point(170, 180), Width = 80 };
                    form.Controls.Add(btnOK);
                    form.Controls.Add(btnCancel);
                    form.AcceptButton = btnOK;
                    form.CancelButton = btnCancel;
                    
                    if (form.ShowDialog(this) == DialogResult.OK)
                    {
                        var newPeriod = txtPeriod.Text.Trim();
                        var newNum1 = (int)numNum1.Value;
                        var newNum2 = (int)numNum2.Value;
                        var newNum3 = (int)numNum3.Value;
                        var newSum = newNum1 + newNum2 + newNum3;
                        
                        if (string.IsNullOrEmpty(newPeriod))
                        {
                            MessageBox.Show("请输入期号", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                        
                        // Confirm the change
                        var confirmMsg = $"确认修正开奖数据?\n\n" +
                                        $"原数据: {currentPeriod}期 {currentNum1}+{currentNum2}+{currentNum3}={currentSum}\n" +
                                        $"新数据: {newPeriod}期 {newNum1}+{newNum2}+{newNum3}={newSum}\n\n" +
                                        "修正后将触发重新结算!";
                        
                        if (MessageBox.Show(confirmMsg, "确认修正", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                        {
                            // Apply the correction
                            LotteryService.Instance.ManualSetResult(newPeriod, newNum1, newNum2, newNum3);
                            
                            // Update UI
                            lblStatus.Text = $"已修正开奖: {newPeriod}期 {newNum1}+{newNum2}+{newNum3}={newSum}";
                            MessageBox.Show($"开奖数据已修正\n\n{newPeriod}期: {newNum1}+{newNum2}+{newNum3}={newSum}", 
                                          "修正成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            
                            // Log the correction
                            RunLogService.Instance.LogLottery(newPeriod, newSum, $"手动修正: {currentNum1}+{currentNum2}+{currentNum3}->{newNum1}+{newNum2}+{newNum3}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[MainForm] Fix lottery error: {ex.Message}");
                MessageBox.Show($"修正开奖失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

    }
}
