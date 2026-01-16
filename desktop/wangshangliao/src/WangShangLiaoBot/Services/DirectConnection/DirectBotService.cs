using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WangShangLiaoBot.Services.Bot;

namespace WangShangLiaoBot.Services.DirectConnection
{
    /// <summary>
    /// 直连机器人服务 - 整合所有直连组件
    /// 
    /// 架构说明:
    /// ┌─────────────────┐     ┌─────────────┐     ┌──────────────┐
    /// │  DirectBotSvc   │────▶│  XClient    │────▶│  xclient.exe │
    /// │  (机器人逻辑)    │     │  Service    │     │  (旺商聊核心) │
    /// └─────────────────┘     └─────────────┘     └──────────────┘
    ///        │                                           │
    ///        │                                           ▼
    ///        │                                    ┌──────────────┐
    ///        └───────────────────────────────────▶│  NIM云信服务  │
    ///            (通过API服务)                     └──────────────┘
    /// 
    /// 通信协议:
    /// 1. TCP连接到xclient.exe (端口21303)
    /// 2. JSON格式消息
    /// 3. Protobuf编码的消息内容
    /// </summary>
    public sealed class DirectBotService : IDisposable
    {
        private static DirectBotService _instance;
        public static DirectBotService Instance => _instance ?? (_instance = new DirectBotService());

        // 核心服务
        private readonly XClientService _xclient;
        private readonly NimMessageService _nimMessage;
        private readonly WangShangLiaoApiService _api;

        // 消息处理
        private readonly MessageDispatcher _dispatcher;
        private CancellationTokenSource _cts;

        // 状态
        private bool _isRunning;
        private string _currentTeamId;
        private string _botNimId;

        // 事件
        public event Action<string> OnLog;
        public event Action<bool> OnStatusChanged;
        public event Action<NimChatMessage> OnMessageReceived;

        public bool IsRunning => _isRunning;
        public bool IsConnected => _xclient?.IsConnected ?? false;

        private DirectBotService()
        {
            _xclient = XClientService.Instance;
            _nimMessage = NimMessageService.Instance;
            _api = WangShangLiaoApiService.Instance;
            _dispatcher = new MessageDispatcher();

            // 订阅消息事件
            _nimMessage.OnGroupMessageReceived += HandleGroupMessage;
            _nimMessage.OnPrivateMessageReceived += HandlePrivateMessage;

            // 订阅连接状态
            _xclient.OnConnectionStateChanged += OnConnectionChanged;
            _xclient.OnLog += Log;
        }

        #region 启动和停止

        /// <summary>
        /// 启动机器人
        /// </summary>
        public async Task<bool> StartAsync(string teamId, string botNimId = null)
        {
            if (_isRunning)
            {
                Log("[DirectBot] 机器人已在运行");
                return true;
            }

            _currentTeamId = teamId;
            _botNimId = botNimId;
            _cts = new CancellationTokenSource();

            try
            {
                // 连接到xclient
                Log("[DirectBot] 正在连接到xclient...");
                if (!await _xclient.ConnectAsync())
                {
                    Log("[DirectBot] 连接xclient失败");
                    return false;
                }

                // 启动消息处理循环
                _isRunning = true;
                _ = Task.Run(() => MessageCleanupLoopAsync(_cts.Token));

                Log($"[DirectBot] 机器人启动成功，监听群: {teamId}");
                OnStatusChanged?.Invoke(true);

                return true;
            }
            catch (Exception ex)
            {
                Log($"[DirectBot] 启动失败: {ex.Message}");
                _isRunning = false;
                return false;
            }
        }

        /// <summary>
        /// 停止机器人
        /// </summary>
        public void Stop()
        {
            if (!_isRunning) return;

            _cts?.Cancel();
            _xclient.Disconnect();
            _isRunning = false;

            Log("[DirectBot] 机器人已停止");
            OnStatusChanged?.Invoke(false);
        }

        #endregion

        #region 消息发送

        /// <summary>
        /// 发送群消息
        /// </summary>
        public async Task<bool> SendGroupMessageAsync(string text, string teamId = null)
        {
            teamId ??= _currentTeamId;
            if (string.IsNullOrEmpty(teamId))
            {
                Log("[DirectBot] 未指定群ID");
                return false;
            }

            var result = await _nimMessage.SendGroupTextAsync(teamId, text);
            return result.Success;
        }

        /// <summary>
        /// 发送私聊消息
        /// </summary>
        public async Task<bool> SendPrivateMessageAsync(string userId, string text)
        {
            var result = await _nimMessage.SendPrivateTextAsync(userId, text);
            return result.Success;
        }

        /// <summary>
        /// 回复消息
        /// </summary>
        public async Task<bool> ReplyAsync(NimChatMessage originalMsg, string replyText)
        {
            if (originalMsg.Scene == "team")
            {
                return await SendGroupMessageAsync(replyText, originalMsg.To);
            }
            else
            {
                return await SendPrivateMessageAsync(originalMsg.From, replyText);
            }
        }

        #endregion

        #region 群管理

        /// <summary>
        /// 禁言成员
        /// </summary>
        public async Task<bool> MuteMemberAsync(string userId, int minutes, string teamId = null)
        {
            teamId ??= _currentTeamId;
            var response = await _api.SetMemberMuteAsync(teamId, userId, minutes);
            if (response.Success)
            {
                Log($"[DirectBot] 已禁言 {userId} {minutes}分钟");
            }
            return response.Success;
        }

        /// <summary>
        /// 取消禁言
        /// </summary>
        public async Task<bool> UnmuteMemberAsync(string userId, string teamId = null)
        {
            teamId ??= _currentTeamId;
            var response = await _api.CancelMemberMuteAsync(teamId, userId);
            if (response.Success)
            {
                Log($"[DirectBot] 已解除 {userId} 禁言");
            }
            return response.Success;
        }

        /// <summary>
        /// 踢出成员
        /// </summary>
        public async Task<bool> KickMemberAsync(string userId, string teamId = null)
        {
            teamId ??= _currentTeamId;
            var response = await _api.RemoveGroupMemberAsync(teamId, userId);
            if (response.Success)
            {
                Log($"[DirectBot] 已踢出 {userId}");
            }
            return response.Success;
        }

        /// <summary>
        /// 撤回消息
        /// </summary>
        public async Task<bool> RecallMessageAsync(string msgId, string teamId = null)
        {
            teamId ??= _currentTeamId;
            var response = await _api.MessageRollbackAsync(teamId, msgId);
            return response.Success;
        }

        /// <summary>
        /// 获取群成员列表
        /// </summary>
        public async Task<List<GroupMemberInfo>> GetGroupMembersAsync(string teamId = null)
        {
            teamId ??= _currentTeamId;
            var response = await _api.GetGroupMembersAsync(teamId);
            return response.Success ? response.Data?.Members : null;
        }

        /// <summary>
        /// 设置群公告
        /// </summary>
        public async Task<bool> SetGroupNoticeAsync(string content, string teamId = null)
        {
            teamId ??= _currentTeamId;
            var response = await _api.AddGroupNoticeAsync(teamId, content);
            return response.Success;
        }

        #endregion

        #region 消息处理

        private void HandleGroupMessage(NimChatMessage msg)
        {
            try
            {
                // 过滤非目标群消息
                if (!string.IsNullOrEmpty(_currentTeamId) && msg.To != _currentTeamId)
                {
                    return;
                }

                // 过滤自己发送的消息
                if (!string.IsNullOrEmpty(_botNimId) && msg.From == _botNimId)
                {
                    return;
                }

                // 触发消息事件
                OnMessageReceived?.Invoke(msg);

                // 通过消息分发器处理
                _dispatcher.DispatchAsync(new Models.ChatMessage
                {
                    Id = msg.MessageId,
                    Scene = "team",
                    From = msg.From,
                    TeamId = msg.To,
                    Text = msg.Text,
                    Time = msg.Timestamp,
                    FromNickname = msg.FromNickname
                }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log($"[DirectBot] 处理群消息错误: {ex.Message}");
            }
        }

        private void HandlePrivateMessage(NimChatMessage msg)
        {
            try
            {
                // 过滤自己发送的消息
                if (!string.IsNullOrEmpty(_botNimId) && msg.From == _botNimId)
                {
                    return;
                }

                OnMessageReceived?.Invoke(msg);

                _dispatcher.DispatchAsync(new Models.ChatMessage
                {
                    Id = msg.MessageId,
                    Scene = "p2p",
                    From = msg.From,
                    Text = msg.Text,
                    Time = msg.Timestamp,
                    FromNickname = msg.FromNickname
                }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log($"[DirectBot] 处理私聊消息错误: {ex.Message}");
            }
        }

        private void OnConnectionChanged(bool connected)
        {
            if (!connected && _isRunning)
            {
                Log("[DirectBot] 连接断开，尝试重连...");
                _ = ReconnectAsync();
            }
        }

        private async Task ReconnectAsync()
        {
            for (int i = 0; i < 5; i++)
            {
                await Task.Delay(3000);
                if (await _xclient.ConnectAsync())
                {
                    Log("[DirectBot] 重连成功");
                    return;
                }
                Log($"[DirectBot] 重连失败，重试 {i + 1}/5");
            }

            Log("[DirectBot] 重连失败，停止机器人");
            Stop();
        }

        private async Task MessageCleanupLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(60000, ct);
                    _nimMessage.CleanupOldMessages();
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        #endregion

        private void Log(string message)
        {
            OnLog?.Invoke(message);
            Utils.Logger.Info(message);
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
