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
        private async void InitializeFrameworkConnection()
        {
            try
            {
                Logger.Info("[MainForm] 尝试连接到副框架服务端...");
                Logger.Info("[MainForm] 注意: 主框架只连接副框架，不直接连接旺商聊");
                
                var frameworkClient = Services.HPSocket.FrameworkClient.Instance;
                
                // 订阅事件
                frameworkClient.OnConnectionChanged += OnFrameworkConnectionChanged;
                frameworkClient.OnMessageReceived += OnFrameworkMessageReceived;
                frameworkClient.OnAccountChanged += OnFrameworkAccountChanged;
                frameworkClient.OnLog += (msg) => Logger.Info(msg);
                
                // 更新UI状态 - 连接中
                SafeInvoke(() =>
                {
                    if (lblConnectionStatus != null)
                    {
                        lblConnectionStatus.Text = "连接中...";
                        lblConnectionStatus.ForeColor = Color.Orange;
                    }
                });
                
                // 【优化】不再固定等待2秒，改为快速尝试连接
                // 如果副框架已启动，可以立即连接成功
                // 如果副框架未启动，自动重连机制会处理
                
                // 先快速尝试一次连接（无延迟）
                var connected = await frameworkClient.ConnectAsync("127.0.0.1", 14746);
                
                if (connected)
                {
                    Logger.Info("[MainForm] ✓ 成功连接到副框架服务端");
                    UpdateFrameworkStatus(true);
                    
                    // 发送登录请求到副框架
                    await SendLoginToFrameworkAsync();
                }
                else
                {
                    Logger.Info("[MainForm] × 副框架未运行，启动自动重连...");
                    UpdateFrameworkStatus(false);
                    
                    // 启动自动重连（后台异步进行，不阻塞UI）
                    StartFrameworkReconnect();
                }
                
                // 【已移除】主框架不再直接连接旺商聊
                // 所有旺商聊连接由副框架处理
            }
            catch (Exception ex)
            {
                Logger.Error($"[MainForm] 连接副框架异常: {ex.Message}");
                UpdateFrameworkStatus(false);
                
                // 连接异常也启动自动重连
                StartFrameworkReconnect();
            }
        }

        private async Task SendLoginToFrameworkAsync()
        {
            try
            {
                var frameworkClient = Services.HPSocket.FrameworkClient.Instance;
                if (!frameworkClient.IsConnected) return;
                
                Logger.Info("[MainForm] 发送登录请求到副框架...");
                
                // 创建登录消息
                var loginMessage = new Services.HPSocket.FrameworkMessage(
                    Services.HPSocket.FrameworkMessageType.Login, 
                    "MainForm登录"
                );
                
                await frameworkClient.SendAsync(loginMessage);
            }
            catch (Exception ex)
            {
                Logger.Error($"[MainForm] 发送登录请求失败: {ex.Message}");
            }
        }

        private System.Threading.Timer _threadingReconnectTimer;
        
        private void StartFrameworkReconnect()
        {
            if (_threadingReconnectTimer != null) return;
            
            Logger.Info("[MainForm] 启动副框架自动重连...");
            _reconnectAttempts = 0;
            
            _threadingReconnectTimer = new System.Threading.Timer(async _ =>
            {
                if (_reconnectAttempts >= MaxReconnectAttempts)
                {
                    Logger.Info("[MainForm] 达到最大重连次数，停止重连");
                    StopFrameworkReconnect();
                    return;
                }
                
                var frameworkClient = Services.HPSocket.FrameworkClient.Instance;
                if (frameworkClient.IsConnected)
                {
                    Logger.Info("[MainForm] 副框架已连接，停止重连");
                    StopFrameworkReconnect();
                    return;
                }
                
                _reconnectAttempts++;
                Logger.Info($"[MainForm] 重连副框架 ({_reconnectAttempts}/{MaxReconnectAttempts})...");
                
                var connected = await frameworkClient.ConnectAsync("127.0.0.1", 14746);
                if (connected)
                {
                    Logger.Info("[MainForm] ✓ 重连副框架成功");
                    SafeInvoke(() => UpdateFrameworkStatus(true));
                    await SendLoginToFrameworkAsync();
                    StopFrameworkReconnect();
                }
            }, null, 3000, 5000); // 3秒后开始，每5秒重试
        }

        private void StopFrameworkReconnect()
        {
            _threadingReconnectTimer?.Dispose();
            _threadingReconnectTimer = null;
        }

        private async System.Threading.Tasks.Task AutoConnectToWangShangLiaoAsync()
        {
            // 此方法已废弃，主框架只连接副框架
            Logger.Info("[MainForm] AutoConnectToWangShangLiaoAsync 已废弃，主框架只连接副框架");
            await System.Threading.Tasks.Task.CompletedTask;
        }

        private void OnFrameworkConnectionChanged(bool connected)
        {
            if (InvokeRequired)
            {
                try { Invoke(new Action(() => OnFrameworkConnectionChanged(connected))); }
                catch { }
                return;
            }
            
            // 更新UI状态
            UpdateFrameworkStatus(connected);
            
            if (connected)
            {
                Logger.Info("[MainForm] ✓ 副框架已连接，消息收发通过副框架进行");
                // 停止重连
                StopFrameworkReconnect();
            }
            else
            {
                Logger.Info("[MainForm] × 副框架已断开，尝试重连...");
                // 启动自动重连
                StartFrameworkReconnect();
            }
        }

        private void OnFrameworkMessageReceived(Services.HPSocket.FrameworkMessage message)
        {
            if (InvokeRequired)
            {
                try { Invoke(new Action(() => OnFrameworkMessageReceived(message))); }
                catch { }
                return;
            }
            
            // 处理从副框架接收到的消息
            switch (message.Type)
            {
                case Services.HPSocket.FrameworkMessageType.ReceiveMessage:
                case Services.HPSocket.FrameworkMessageType.ReceiveGroupMessage:
                    Logger.Info($"[MainForm] 收到副框架群消息: GroupId={message.GroupId}, From={message.SenderId}, Content={message.Content}");
                    
                    // 将消息转发给 ChatService 进行处理（触发自动回复、下注处理等）
                    ProcessFrameworkGroupMessage(message);
                    break;
                    
                case Services.HPSocket.FrameworkMessageType.Notification:
                    Logger.Info($"[MainForm] 副框架通知: {message.Content}");
                    break;
            }
        }
        
        /// <summary>
        /// 副框架账号变化事件处理
        /// </summary>
        private void OnFrameworkAccountChanged(Services.HPSocket.FrameworkAccountInfo info)
        {
            if (InvokeRequired)
            {
                try { Invoke(new Action(() => OnFrameworkAccountChanged(info))); }
                catch { }
                return;
            }
            
            if (info == null) return;
            
            Logger.Info($"[MainForm] ★ 副框架账号变化: {info.Nickname} ({info.AccountId}), 群号: {info.GroupId}");
            
            // 更新本地配置
            var config = ConfigService.Instance.Config;
            if (config != null)
            {
                // 只更新非空值
                if (!string.IsNullOrEmpty(info.GroupId))
                {
                    config.GroupId = info.GroupId;
                    config.GroupName = info.GroupName;
                }
                ConfigService.Instance.SaveConfig();
                Logger.Info($"[MainForm] 已同步账号配置: 群号={info.GroupId}, 群名={info.GroupName}");
            }
            
            // 更新状态栏显示
            if (info.IsLoggedIn)
            {
                lblStatus.Text = $"已登录: {info.Nickname} | 绑定群: {info.GroupName ?? info.GroupId}";
            }
            else
            {
                lblStatus.Text = "副框架已连接，账号未登录";
            }
        }

        private void ProcessFrameworkGroupMessage(Services.HPSocket.FrameworkMessage message)
        {
            try
            {
                var groupId = message.GroupId ?? message.ReceiverId ?? "";
                var senderId = message.SenderId ?? "";
                var content = message.Content ?? "";
                var senderNick = senderId;
                
                // 尝试从 Extra 中获取昵称
                if (!string.IsNullOrEmpty(message.Extra))
                {
                    try
                    {
                        var extra = new System.Web.Script.Serialization.JavaScriptSerializer()
                            .Deserialize<System.Collections.Generic.Dictionary<string, object>>(message.Extra);
                        
                        if (extra != null && extra.TryGetValue("FromNick", out var nick) && nick != null)
                            senderNick = nick.ToString();
                    }
                    catch { }
                }
                
                Logger.Info($"[MainForm] 处理群消息: GroupId={groupId}, From={senderId}({senderNick}), Content={content}");
                
                // 构建 ChatMessage 对象
                var chatMessage = new ChatMessage
                {
                    Id = message.Id.ToString(),
                    Content = content,
                    SenderId = senderId,
                    SenderName = senderNick,
                    GroupId = groupId,
                    Time = DateTime.Now
                };
                
                // 触发 ChatService 的消息处理流程
                _ = ProcessChatMessageAsync(chatMessage, groupId, senderNick);
            }
            catch (Exception ex)
            {
                Logger.Error($"[MainForm] 处理副框架群消息异常: {ex.Message}");
            }
        }

        private async Task ProcessChatMessageAsync(ChatMessage chatMessage, string groupId, string senderNick)
        {
            try
            {
                // 1. 记录运行日志
                Logger.Info($"[副框架消息] GroupId={groupId}, From={chatMessage.SenderId}({senderNick}), Content={chatMessage.Content}");
                
                // 设置群聊场景
                chatMessage.Scene = "team";
                chatMessage.IsGroupMessage = true;
                chatMessage.GroupId = groupId;
                
                // 2. 触发 ChatService 的 OnMessageReceived 事件
                // 这样 AutoReplyService 可以正确处理消息
                ChatService.Instance.TriggerMessageReceived(chatMessage);
                
                // 3. 检查是否是余额查询 (发送 1、2 或 查)
                var content = chatMessage.Content?.Trim() ?? "";
                var config = ConfigService.Instance.Config;
                
                // 余额查询关键词 (默认: "1|2|查|余额")
                string[] balanceKeywords = { "1", "2", "查", "余额" };
                bool isBalanceQuery = balanceKeywords.Any(k => content.Equals(k, StringComparison.OrdinalIgnoreCase));
                
                if (isBalanceQuery)
                {
                    // 获取玩家余额
                    var player = DataService.Instance.GetOrCreatePlayer(chatMessage.SenderId, senderNick);
                    
                    // 使用模板引擎渲染回复
                    var template = config?.InternalDataBill ?? "@qq ([旺旺])\n老板，您的账户有余额！\n当前余额:[分数]";
                    var reply = TemplateEngine.Render(template, new TemplateEngine.RenderContext
                    {
                        Message = chatMessage,
                        Player = player,
                        Today = DateTime.Today
                    });
                    
                    // 通过副框架发送回复
                    if (!string.IsNullOrEmpty(reply))
                    {
                        await Services.HPSocket.FrameworkClient.Instance.SendGroupMessageAsync(groupId, reply);
                        Logger.Info($"[MainForm] 余额查询回复: {reply}");
                    }
                }
                
                // 4. 下注处理 (如果结算服务已启动)
                if (Services.Betting.BetSettlementService.Instance.IsRunning)
                {
                    Logger.Debug($"[MainForm] 结算服务运行中，消息已转发给 AutoReplyService 处理");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[MainForm] 处理聊天消息异常: {ex.Message}");
            }
        }

        private void UpdateFrameworkStatus(bool connected)
        {
            if (InvokeRequired)
            {
                try { Invoke(new Action(() => UpdateFrameworkStatus(connected))); }
                catch { }
                return;
            }
            
            // 更新连接状态显示
            // 主框架只连接副框架，状态只有两种：已连接/未连接
            if (connected)
            {
                lblConnectionStatus.Text = "已连框架";
                lblConnectionStatus.ForeColor = Color.Green;
                lblConnectionStatus.BackColor = Color.FromArgb(220, 255, 220);
                lblStatus.Text = "副框架已连接，消息通过副框架收发";
            }
            else
            {
                lblConnectionStatus.Text = "未连框架";
                lblConnectionStatus.ForeColor = Color.Red;
                lblConnectionStatus.BackColor = SystemColors.Control;
                lblStatus.Text = "请先启动副框架 (旺商聊框架.exe)";
            }
        }

        private void OnConnectionChanged(bool connected)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnConnectionChanged(connected)));
                return;
            }
            
            if (connected)
            {
                lblConnectionStatus.Text = "已连框架";
                lblConnectionStatus.ForeColor = Color.Green;
                
                // Auto-start services when connected
                try
                {
                    // =====================================================
                    // CRITICAL: Install NIM message hook first (CDP mode)
                    // This is REQUIRED for receiving messages!
                    // =====================================================
                    if (ChatService.Instance.Mode == ConnectionMode.CDP)
                    {
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                var hookResult = await ChatService.Instance.InstallMessageHookAsync();
                                Logger.Info($"[MainForm] Message hook installed: {hookResult.Success}, {hookResult.Message}");
                                
                                if (hookResult.Success)
                                {
                                    ChatService.Instance.StartHookedMessagePolling(1000);
                                    Logger.Info("[MainForm] Hooked message polling started");
                                    
                                    // Start system message polling for AutoApprovePlayer feature
                                    if (ConfigService.Instance.Config.AutoApprovePlayer)
                                    {
                                        ChatService.Instance.StartSystemMessagePolling();
                                        Logger.Info("[MainForm] System message polling started (AutoApprovePlayer)");
                                    }
                                    
                                    // Sync member plaintext nicknames from WangShangLiao
                                    _ = SyncMemberNicknamesAsync();
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.Error($"[MainForm] Failed to install message hook: {ex.Message}");
                            }
                        });
                    }
                    
                    // Start run log service
                    Services.RunLogService.Instance.Start();
                    Logger.Info("[MainForm] RunLogService started on connection");
                    
                    // Start spam detection service
                    Services.Spam.SpamDetectionService.Instance.Start();
                    Logger.Info("[MainForm] SpamDetectionService started on connection");
                    
                    // =====================================================
                    // 算账服务不自动启动，等用户手动点击"开始算账"按钮
                    // BetLedgerService 和 BetSettlementService 需要手动启动
                    // =====================================================
                    // Subscribe to settlement complete event for "开完本期停" feature (预先订阅)
                    Services.Betting.BetSettlementService.Instance.OnSettlementComplete -= OnSettlementComplete;
                    Services.Betting.BetSettlementService.Instance.OnSettlementComplete += OnSettlementComplete;
                    
                    Logger.Info("[MainForm] Connection established, waiting for user to start accounting manually");
                    
                    // 保持按钮状态为"开始算账"，等待用户手动点击
                    btnStopCalc.Text = "开始算账";
                    btnStopCalc.BackColor = System.Drawing.Color.LightGreen;
                    
                    // =====================================================
                    // Start AutoReplyService for automatic message replies
                    // Note: Even if EnableAutoReply=false, start the service
                    // so keyword rules can still trigger replies
                    // =====================================================
                    AutoReplyService.Instance.Start();
                    Logger.Info($"[MainForm] AutoReplyService started (EnableAutoReply={ConfigService.Instance.Config.EnableAutoReply}, KeywordRules={ConfigService.Instance.Config.KeywordRules?.Count ?? 0})");
                }
                catch (Exception ex)
                {
                    Logger.Error($"[MainForm] Failed to start services: {ex.Message}");
                }
            }
            else
            {
                lblConnectionStatus.Text = "未连框架";
                lblConnectionStatus.ForeColor = Color.Red;
                
                // Stop services when disconnected
                try
                {
                    // Stop message polling
                    ChatService.Instance.StopHookedMessagePolling();
                    
                    // Stop all services
                    AutoReplyService.Instance.Stop();
                    Services.RunLogService.Instance.Stop();
                    Services.Spam.SpamDetectionService.Instance.Stop();
                    Services.Betting.BetLedgerService.Instance.Stop();
                    Services.Betting.BetSettlementService.Instance.Stop();
                    Logger.Info("[MainForm] All services stopped on disconnection");
                }
                catch { }
            }
        }

        /// <summary>
        /// 点击连接状态标签 - 【已修改】只连接副框架，不直接连接旺商聊
        /// 主框架通过副框架通信，副框架负责连接旺商聊
        /// </summary>
        private async void lblConnectionStatus_Click(object sender, EventArgs e)
        {
            var frameworkClient = Services.HPSocket.FrameworkClient.Instance;
            
            if (frameworkClient.IsConnected)
            {
                // 断开副框架连接
                lblConnectionStatus.Text = "断开中...";
                lblConnectionStatus.ForeColor = Color.Orange;
                lblStatus.Text = "正在断开与副框架的连接...";
                
                frameworkClient.Disconnect();
                
                lblConnectionStatus.Text = "未连框架";
                lblConnectionStatus.ForeColor = Color.Red;
                lblStatus.Text = "已断开";
                
                MessageBox.Show("已断开与副框架的连接\n\n注意：旺商聊连接由副框架管理", "断开连接", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                // 连接副框架（不直接连接旺商聊）
                lblConnectionStatus.Text = "连接中...";
                lblConnectionStatus.ForeColor = Color.Orange;
                lblStatus.Text = "正在连接副框架服务端...";
                
                var success = await frameworkClient.ConnectAsync("127.0.0.1", 14746);
                
                if (success)
                {
                    lblConnectionStatus.Text = "已连框架";
                    lblConnectionStatus.ForeColor = Color.Green;
                    lblStatus.Text = "已连接副框架 (端口: 14746)";
                    
                    // 发送登录请求
                    await SendLoginToFrameworkAsync();
                    
                    // 同步配置到副框架
                    await SyncConfigToFrameworkAsync();
                    
                    var msg = "✓ 已成功连接副框架服务端！\n\n";
                    msg += "连接模式: 直连副框架 (HPSocket)\n";
                    msg += "端口: 14746\n\n";
                    msg += "说明: 旺商聊连接由副框架管理\n";
                    msg += "主框架负责发送配置和自动回复规则到副框架";
                    
                    MessageBox.Show(msg, "连接成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    lblConnectionStatus.Text = "未连框架";
                    lblConnectionStatus.ForeColor = Color.Red;
                    lblStatus.Text = "连接副框架失败";
                    
                    MessageBox.Show(
                        "无法连接到副框架服务端。\n\n" +
                        "请确保：\n" +
                        "1. 副框架（旺商聊框架.exe）已启动\n" +
                        "2. 副框架监听端口为 14746\n\n" +
                        "主框架只负责发送配置，旺商聊连接由副框架处理。",
                        "连接失败",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }
        }
        
        /// <summary>
        /// 同步配置到副框架
        /// </summary>
        private async Task SyncConfigToFrameworkAsync()
        {
            try
            {
                var frameworkClient = Services.HPSocket.FrameworkClient.Instance;
                if (!frameworkClient.IsConnected) return;
                
                Logger.Info("[MainForm] 同步配置到副框架...");
                
                // 同步自动回复配置
                var config = ConfigService.Instance.Config;
                if (config.EnableAutoReply && config.KeywordRules != null && config.KeywordRules.Count > 0)
                {
                    var rulesJson = new System.Web.Script.Serialization.JavaScriptSerializer()
                        .Serialize(config.KeywordRules);
                    await frameworkClient.SyncAutoReplyConfigAsync(true, rulesJson);
                    Logger.Info($"[MainForm] 已同步自动回复配置 ({config.KeywordRules.Count} 条规则)");
                }
                
                // 同步基本配置
                await frameworkClient.SyncBasicConfigAsync(
                    config.GroupId ?? "",
                    config.AdminWangWangId ?? "",
                    config.MyWangShangId ?? "",
                    config.DebugPort
                );
                Logger.Info("[MainForm] 已同步基本配置");
            }
            catch (Exception ex)
            {
                Logger.Error($"[MainForm] 同步配置失败: {ex.Message}");
            }
        }

    }
}
