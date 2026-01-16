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
        private void MainForm_Shown(object sender, EventArgs e)
        {
            this.Shown -= MainForm_Shown;

            // 【性能修复】不要在 Shown 里同步做重活，否则窗口会“显示很慢/未响应”
            // 让窗口先绘制出来，然后再异步初始化/后台加载数据
            BeginInvoke(new Action(async () =>
            {
                try
                {
                    lblStatus.Text = "正在初始化...";

                    // 初始化事件/热键（轻量）
                    InitializeEvents();
                    RegisterGlobalHotKeys();

                    // 初始化彩票服务 & 连接副框架（内部有 async，不阻塞窗口显示）
                    InitializeLotteryService();
                    InitializeFrameworkConnection();

                    // 加载配置（轻量，控件未初始化时会跳过）
                    LoadConfig();

                    // 后台加载玩家数据，避免卡 UI
                    await LoadPlayerDataAsync();
                }
                catch (Exception ex)
                {
                    Logger.Error($"[MainForm] 初始化失败: {ex.Message}");
                    lblStatus.Text = $"初始化失败: {ex.Message}";
                }
            }));
        }

        private async System.Threading.Tasks.Task LoadPlayerDataAsync()
        {
            try
            {
                lblStatus.Text = "正在加载玩家...";
                var players = await System.Threading.Tasks.Task.Run(() => LoadPlayersFromBoundGroup());
                _players = players ?? new List<Player>();
                RefreshPlayerList();
                lblStatus.Text = $"就绪 - 共 {_players?.Count ?? 0} 个玩家";
            }
            catch (Exception ex)
            {
                Logger.Error($"[MainForm] LoadPlayerDataAsync失败: {ex.Message}");
                lblStatus.Text = "玩家加载失败";
            }
        }

        private void RegisterGlobalHotKeys()
        {
            try
            {
                // Register F10 to hide window
                RegisterHotKey(this.Handle, HOTKEY_ID_F10, 0, VK_F10);
                // Register F12 to show window
                RegisterHotKey(this.Handle, HOTKEY_ID_F12, 0, VK_F12);
                Logger.Info("[MainForm] Global hotkeys registered: F10=Hide, F12=Show");
            }
            catch (Exception ex)
            {
                Logger.Error($"[MainForm] Failed to register hotkeys: {ex.Message}");
            }
        }

        private void UnregisterGlobalHotKeys()
        {
            try
            {
                UnregisterHotKey(this.Handle, HOTKEY_ID_F10);
                UnregisterHotKey(this.Handle, HOTKEY_ID_F12);
                Logger.Info("[MainForm] Global hotkeys unregistered");
            }
            catch (Exception ex)
            {
                Logger.Error($"[MainForm] Failed to unregister hotkeys: {ex.Message}");
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY)
            {
                int hotkeyId = m.WParam.ToInt32();
                
                if (hotkeyId == HOTKEY_ID_F10)
                {
                    // F10 - Hide window
                    this.Hide();
                    Logger.Info("[MainForm] Window hidden by F10");
                }
                else if (hotkeyId == HOTKEY_ID_F12)
                {
                    // F12 - Show window
                    this.Show();
                    this.WindowState = FormWindowState.Normal;
                    this.Activate();
                    Logger.Info("[MainForm] Window shown by F12");
                }
            }
            
            base.WndProc(ref m);
        }

        private void SafeInvoke(Action action)
        {
            try
            {
                if (this.IsDisposed || !this.IsHandleCreated)
                    return;
                    
                if (this.InvokeRequired)
                    this.Invoke(action);
                else
                    action();
            }
            catch (ObjectDisposedException)
            {
                // 窗体已释放，忽略
            }
            catch (InvalidOperationException)
            {
                // Handle未创建，忽略
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.F11)
            {
                btnScoreWindow_Click(null, null);
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Unregister global hotkeys
            UnregisterGlobalHotKeys();
            
            // Stop lottery service
            LotteryService.Instance.Stop();
            
            AutoReplyService.Instance.Stop();
            // IMPORTANT: Do not block UI thread on async calls (can cause freeze on exit).
            // Fire-and-forget disconnect because the app is closing anyway.
            try
            {
                System.Threading.Tasks.Task.Run(async () =>
                {
                    try { await ChatService.Instance.DisconnectAsync(); } catch { /* ignore on shutdown */ }
                });
            }
            catch
            {
                // ignore
            }
            base.OnFormClosing(e);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            
            // 计算内容区域大小
            int contentWidth = this.ClientSize.Width;
            int contentHeight = this.ClientSize.Height - menuStrip.Height - statusStrip.Height;
            
            // 调整设置页面大小
            if (tabSettings.Visible)
            {
                tabSettings.Size = new System.Drawing.Size(contentWidth, contentHeight);
            }
            
            // 调整封盘设置页面大小
            if (tabSealSettings.Visible)
            {
                tabSealSettings.Size = new System.Drawing.Size(contentWidth, contentHeight);
            }
            
            // 调整回水工具页面大小
            if (pnlRebateTool.Visible)
            {
                pnlRebateTool.Size = new System.Drawing.Size(contentWidth, contentHeight);
            }
        }

    }
}
