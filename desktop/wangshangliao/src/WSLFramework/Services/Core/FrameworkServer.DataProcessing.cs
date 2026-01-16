using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HPSocket;
using HPSocket.Tcp;
using WSLFramework.Models;
using WSLFramework.Protocol;
using WSLFramework.Utils;
using WSLFramework.Services.EventDriven;

namespace WSLFramework.Services
{
    /// <summary>
    /// 框架服务端 - 使用 HPSocket PACK 模式实现
    /// 完全匹配招财狗ZCG协议，增强异常处理和线程安全
    /// </summary>
    public partial class FrameworkServer : IDisposable
    {
        #region 数据处理
        
        /// <summary>
        /// 处理接收到的数据
        /// </summary>
        private async Task ProcessReceivedDataAsync(IntPtr connId, byte[] data)
        {
            if (data == null || data.Length == 0)
                return;

            try
            {
                string content = Encoding.UTF8.GetString(data);

                // 尝试解析为 JSON 格式
                var message = FrameworkMessage.FromJson(content);
                if (message != null)
                {
                    Log($"收到JSON消息 (ConnID={connId}, 类型={message.Type}): {content.Substring(0, Math.Min(200, content.Length))}...");
                    OnMessageReceived?.Invoke(connId, message);
                    await ProcessFrameworkMessageAsync(connId, message);
                    return;
                }
                    
                // 尝试解析为 ZCG 插件投递格式 (机器人账号=X，主动账号=Y...)
                if (content.Contains("机器人账号=") || content.Contains("主动账号="))
                {
                    Log($"收到插件投递消息 (ConnID={connId}): {content.Substring(0, Math.Min(200, content.Length))}...");
                    await ProcessPluginMessageAsync(connId, content);
                    return;
                }
                    
                // 尝试解析为 ZCG API 格式 (API名称|参数1|参数2|...)
                if (content.Contains("|"))
                {
                    Log($"收到API消息 (ConnID={connId}): {content.Substring(0, Math.Min(200, content.Length))}...");
                    await ProcessZCGApiMessageAsync(connId, content);
                    return;
                }

                // 尝试解析为消息队列格式
                if (content.StartsWith(ZCGMessageQueue.PREFIX) || content.Contains("★"))
                {
                    Log($"收到消息队列 (ConnID={connId}): {content.Substring(0, Math.Min(200, content.Length))}...");
                    ProcessMessageQueue(connId, content);
                    return;
                }

                // 未知格式
                Log($"收到未知格式消息 (ConnID={connId}): {content.Substring(0, Math.Min(100, content.Length))}...");
            }
            catch (Exception ex)
                {
                Log($"处理消息异常 (ConnID={connId}): {ex.Message}");
            }
        }
        
        /// <summary>
        /// 处理 ZCG 插件投递格式消息
        /// 格式: 机器人账号=X，主动账号=Y，被动账号=Z，群号=G，内容=C，消息ID=M，消息类型=T，消息时间=TM，消息子类型=ST，原始消息=JSON
        /// </summary>
        private async Task ProcessPluginMessageAsync(IntPtr connId, string content)
        {
            try
            {
                var pluginMsg = Protocol.ZCGPluginMessage.FromPluginFormat(content);
                    
                // 根据旺商聊深度连接协议第十三节 - 处理禁言/解禁通知
                // NOTIFY_TYPE_GROUP_MUTE_1 = 禁言开启
                // NOTIFY_TYPE_GROUP_MUTE_0 = 禁言解除
                if (!string.IsNullOrEmpty(pluginMsg.SubType))
                {
                    if (pluginMsg.SubType == "NOTIFY_TYPE_GROUP_MUTE_1" || pluginMsg.SubType == "1")
                    {
                        Log($"收到群禁言通知: 群={pluginMsg.GroupId}");
                        GroupManagementService.Instance.HandleGroupNotification(
                            "NOTIFY_TYPE_GROUP_MUTE_1", pluginMsg.GroupId, null);
                        return;
                    }
                    else if (pluginMsg.SubType == "NOTIFY_TYPE_GROUP_MUTE_0" || pluginMsg.SubType == "0" && pluginMsg.MsgType == 5)
                    {
                        // MsgType=5 表示通知消息
                        Log($"收到群解禁通知: 群={pluginMsg.GroupId}");
                        GroupManagementService.Instance.HandleGroupNotification(
                            "NOTIFY_TYPE_GROUP_MUTE_0", pluginMsg.GroupId, null);
                        return;
                    }
                }
                
                if (string.IsNullOrEmpty(pluginMsg.Content))
                        {
                    Log($"插件消息内容为空");
                    return;
                }
                
                Log($"解析插件消息: 机器人={pluginMsg.RobotAccount}, 发送者={pluginMsg.ActiveAccount}, 群={pluginMsg.GroupId}, 内容={pluginMsg.Content}");
                            
                // 检查是否是下注/上下分/查询消息
                var betParser = BetParserService.Instance;
                var parseResult = betParser.Parse(pluginMsg.Content);
                
                if (parseResult != null && parseResult.IsValid)
                {
                    // 处理下注/上下分/查询
                    if (parseResult.IsQuery)
                    {
                        // 余额查询
                        await HandleBalanceQueryAsync(connId, pluginMsg);
                        }
                    else if (parseResult.IsUp)
                        {
                        // 上分请求
                        await HandleUpRequestAsync(connId, pluginMsg, (int)parseResult.Amount);
                        }
                    else if (parseResult.IsDown)
                    {
                        // 下分请求
                        await HandleDownRequestAsync(connId, pluginMsg, (int)parseResult.Amount);
                }
                else
                {
                        // 下注请求
                        await HandleBetRequestAsync(connId, pluginMsg, parseResult);
                    }
                }
                else
                {
                    // 检查自动回复
                    var replyService = AutoReplyService.Instance;
                    var reply = replyService.GetReply(pluginMsg.Content);
                    if (!string.IsNullOrEmpty(reply))
                    {
                        await SendGroupMessageViaCDPAsync(pluginMsg.GroupId, reply);
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"处理插件消息异常: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 处理余额查询
        /// </summary>
        private async Task HandleBalanceQueryAsync(IntPtr connId, Protocol.ZCGPluginMessage pluginMsg)
        {
            try
            {
                var scoreService = ScoreService.Instance;
                var balance = scoreService.GetBalance(pluginMsg.ActiveAccount);
                var response = ZCGResponseFormatter.FormatBalanceQuery(pluginMsg.ActiveAccount, pluginMsg.Nickname, balance);
                await SendGroupMessageViaCDPAsync(pluginMsg.GroupId, response);
                }
            catch (Exception ex)
                {
                Log($"处理余额查询异常: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 处理上分请求
        /// </summary>
        private async Task HandleUpRequestAsync(IntPtr connId, Protocol.ZCGPluginMessage pluginMsg, int amount)
        {
            try
            {
                var scoreService = ScoreService.Instance;
                var cmd = new ScoreCommand
                {
                    PlayerId = pluginMsg.ActiveAccount,
                    IsUp = true,
                    Amount = amount
                };
                var result = scoreService.ProcessCommand(cmd);
                
                string response;
                if (result.Success)
                {
                    response = ZCGResponseFormatter.FormatUpSuccess(pluginMsg.ActiveAccount, amount, result.NewBalance);
                }
                else
                {
                    response = ZCGResponseFormatter.FormatUpRequest(pluginMsg.ActiveAccount, amount);
                }
                await SendGroupMessageViaCDPAsync(pluginMsg.GroupId, response);
            }
            catch (Exception ex)
                {
                Log($"处理上分请求异常: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 处理下分请求
        /// </summary>
        private async Task HandleDownRequestAsync(IntPtr connId, Protocol.ZCGPluginMessage pluginMsg, int amount)
                    {
            try
            {
                var scoreService = ScoreService.Instance;
                var balance = scoreService.GetBalance(pluginMsg.ActiveAccount);
                
                if (balance < amount)
                {
                    var response = ZCGResponseFormatter.FormatInsufficientBalance(pluginMsg.ActiveAccount, amount, balance);
                    await SendGroupMessageViaCDPAsync(pluginMsg.GroupId, response);
                    return;
                }
                
                var cmd = new ScoreCommand
                {
                    PlayerId = pluginMsg.ActiveAccount,
                    IsUp = false,
                    Amount = amount
                };
                var result = scoreService.ProcessCommand(cmd);
                
                if (result.Success)
                {
                    var response = ZCGResponseFormatter.FormatDownSuccess(pluginMsg.ActiveAccount, amount, result.NewBalance);
                    await SendGroupMessageViaCDPAsync(pluginMsg.GroupId, response);
                }
            }
            catch (Exception ex)
            {
                Log($"处理下分请求异常: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 处理下注请求
        /// </summary>
        private async Task HandleBetRequestAsync(IntPtr connId, Protocol.ZCGPluginMessage pluginMsg, BetResult bet)
        {
            try
            {
                var scoreService = ScoreService.Instance;
                var balance = scoreService.GetBalance(pluginMsg.ActiveAccount);
                
                // 检查余额
                if (balance < (int)bet.Amount)
                {
                    var response = ZCGResponseFormatter.FormatBetInsufficientBalance(
                        pluginMsg.ActiveAccount, bet.BetType, (int)bet.Amount, balance);
                    await SendGroupMessageViaCDPAsync(pluginMsg.GroupId, response);
                    return;
                }
                
                // 扣除下注金额
                var deductResult = scoreService.DeductBet(pluginMsg.ActiveAccount, (int)bet.Amount);
                if (!deductResult)
                {
                    Log($"扣除下注金额失败: {pluginMsg.ActiveAccount}");
                    return;
                }
                
                // 记录下注 (这里应该调用结算服务记录)
                var newBalance = scoreService.GetBalance(pluginMsg.ActiveAccount);
                var response2 = ZCGResponseFormatter.FormatBetSuccess(
                    pluginMsg.ActiveAccount, bet.BetType, (int)bet.Amount, newBalance);
                await SendGroupMessageViaCDPAsync(pluginMsg.GroupId, response2);
                
                Log($"下注成功: {pluginMsg.ActiveAccount} -> {bet.BetType}{bet.Amount}");
            }
            catch (Exception ex)
            {
                Log($"处理下注请求异常: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 发送群消息 - 强制优先使用 NIM（不依赖旺商聊UI，避免切换会话发错人）
        /// 架构: 副框架 → NimDirectClient → 云信服务器
        /// </summary>
        private async Task<bool> SendGroupMessageViaCDPAsync(string groupId, string message)
        {
            var resolvedGroupId = ResolveGroupIdForSend(groupId);
            if (string.IsNullOrEmpty(resolvedGroupId))
            {
                Log($"[发送群消息] 群ID为空且无法解析。candidate={groupId ?? "(null)"} active={_activeGroupId ?? "(null)"}");
                return false;
            }

            // 方案1: 使用 NIM 直连客户端 (最优先)
            var nimDirect = NimDirectClient.Instance;

            // ★★★ 注意: 不要在这里调用LoginWithCDPAsync，会覆盖机器人凭证 ★★★
            // 如果NIM未登录，使用已有的发送方式（NIMService或CDP）
            // 补登录逻辑已经在TryAutoLoginAsync中处理

            if (nimDirect.IsLoggedIn)
            {
                try
                {
                    Log($"[NIM直连] 发送群消息: groupId={resolvedGroupId}");
                    var result = await nimDirect.SendTeamMessageAsync(resolvedGroupId, message);
                    if (result)
                    {
                        Log($"[NIM直连] ✓ 发送成功: {message.Substring(0, Math.Min(50, message.Length))}...");
                        return true;
                    }
                    Log($"[NIM直连] 发送失败，尝试其他方式");
                }
                catch (Exception ex)
                {
                    Log($"[NIM直连] 异常: {ex.Message}");
                }
            }
            
            // 方案2: 使用旧 NIM SDK
            if (_useNIMForSending && _nimService != null && _nimService.IsLoggedIn)
            {
                try
                {
                    Log($"[NIM SDK] 发送群消息: groupId={resolvedGroupId}");
                    var result = await _nimService.SendGroupMessageAsync(resolvedGroupId, message);
                    if (result)
                    {
                        Log($"[NIM SDK] ✓ 发送成功");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Log($"[NIM SDK] 异常: {ex.Message}");
                }
            }

            // 【关键】不允许用 CDP 群发，避免因用户切换聊天窗口导致发错人/发私聊
            Log($"[发送群消息] NIM未就绪或发送失败，已禁止CDP群发以避免发错。groupId={resolvedGroupId}");
            return false;
        }

        /// <summary>
        /// 解析实际要发送的群号（优先：入参 → 活跃群 → 当前账号绑定群 → 账号列表中的任意绑定群）
        /// </summary>
        private string ResolveGroupIdForSend(string candidateGroupId)
        {
            if (!string.IsNullOrWhiteSpace(candidateGroupId))
                return candidateGroupId.Trim();

            if (!string.IsNullOrWhiteSpace(_activeGroupId))
                return _activeGroupId.Trim();

            try
            {
                var acc = BotLoginService.Instance.CurrentAccount;
                if (!string.IsNullOrWhiteSpace(acc?.GroupId))
                    return acc.GroupId.Trim();
            }
            catch { }

            try
            {
                var accounts = AccountManager.Instance?.Accounts;
                if (accounts != null)
                {
                    foreach (var a in accounts)
                    {
                        if (!string.IsNullOrWhiteSpace(a?.GroupId))
                            return a.GroupId.Trim();
                    }
                }
            }
            catch { }

            return null;
        }
        
        /// <summary>
        /// 处理框架消息
        /// </summary>
        private async Task ProcessFrameworkMessageAsync(IntPtr connId, FrameworkMessage message)
        {
            FrameworkMessage response = null;
            
            try
            {
                switch (message.Type)
                {
                    case FrameworkMessageType.Login:
                        response = await HandleLoginAsync(connId, message);
                        break;
                        
                    case FrameworkMessageType.Heartbeat:
                        response = new FrameworkMessage(FrameworkMessageType.Heartbeat) { Id = message.Id, Success = true };
                        break;
                        
                    case FrameworkMessageType.ApiRequest:
                        response = await HandleApiRequestAsync(connId, message);
                        break;
                        
                    case FrameworkMessageType.SendGroupMessage:
                        response = await HandleSendGroupMessageAsync(message);
                        break;

                    case FrameworkMessageType.SendPrivateMessage:
                        response = await HandleSendPrivateMessageAsync(message);
                        break;

                    case FrameworkMessageType.GroupOperation:
                        response = await HandleGroupOperationAsync(message);
                        break;

                    case FrameworkMessageType.CDPCommand:
                        response = await HandleCDPCommandAsync(message);
                        break;
                        
                    case FrameworkMessageType.StartAccounting:
                        response = await HandleStartAccountingAsync(connId, message);
                        break;
                        
                    case FrameworkMessageType.StopAccounting:
                        response = HandleStopAccounting(connId, message);
                        break;
                        
                    case FrameworkMessageType.SetActiveGroup:
                        response = HandleSetActiveGroup(connId, message);
                        break;
                    
                    case FrameworkMessageType.GetBoundGroup:
                        response = HandleGetBoundGroup(message);
                        break;
                    
                    case FrameworkMessageType.GetAccountInfo:
                        response = await HandleGetAccountInfoAsync(message);
                        break;
                    
                    // ===== 配置同步消息处理 =====
                    case FrameworkMessageType.SyncFullConfig:
                        response = HandleSyncFullConfig(message);
                        break;
                        
                    case FrameworkMessageType.SyncOddsConfig:
                        response = HandleSyncOddsConfig(message);
                        break;
                        
                    case FrameworkMessageType.SyncSealingConfig:
                        response = HandleSyncSealingConfig(message);
                        break;
                        
                    case FrameworkMessageType.SyncTrusteeConfig:
                        response = HandleSyncTrusteeConfig(message);
                        break;
                        
                    case FrameworkMessageType.SyncAutoReplyConfig:
                        response = HandleSyncAutoReplyConfig(message);
                        break;
                        
                    case FrameworkMessageType.SyncTemplateConfig:
                        response = HandleSyncTemplateConfig(message);
                        break;
                        
                    case FrameworkMessageType.SyncBasicConfig:
                        response = HandleSyncBasicConfig(message);
                        break;
                    
                    // ===== 开奖相关消息处理 =====
                    case FrameworkMessageType.LotteryResult:
                        response = HandleLotteryResult(message);
                        break;
                        
                    case FrameworkMessageType.SealingNotify:
                        response = HandleSealingNotify(message);
                        break;
                        
                    case FrameworkMessageType.ReminderNotify:
                        response = HandleReminderNotify(message);
                        break;
                        
                    case FrameworkMessageType.PeriodUpdate:
                        response = HandlePeriodUpdate(message);
                        break;
                        
                    default:
                        response = FrameworkMessage.CreateError(message.Id, $"未知消息类型: {message.Type}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Log($"处理消息异常: {ex.Message}");
                response = FrameworkMessage.CreateError(message.Id, ex.Message);
            }
            
            if (response != null)
            {
                SendToClient(connId, response);
            }
        }
        
        /// <summary>
        /// 处理ZCG API格式消息
        /// </summary>
        private async Task ProcessZCGApiMessageAsync(IntPtr connId, string content)
        {
            try
            {
                var parts = content.Split(ZCGProtocol.API_SEPARATOR);
                if (parts.Length < 1)
                    return;

                var apiName = parts[0].Trim();
                var apiParams = new string[parts.Length - 1];
                Array.Copy(parts, 1, apiParams, 0, apiParams.Length);

                // 移除最后一个参数中的 "返回结果:" 部分 (如果是请求)
                for (int i = 0; i < apiParams.Length; i++)
                {
                    if (apiParams[i].StartsWith("返回结果:"))
                {
                        apiParams = new string[i];
                        Array.Copy(parts, 1, apiParams, 0, i);
                        break;
                    }
                }

                // 调用API
                OnApiCall?.Invoke(apiName, apiParams, "");

                string result = await ExecuteApiAsync(apiName, apiParams);

                // 发送响应
                var response = $"{apiName}|{string.Join("|", apiParams)}|返回结果:{result}";
                SendRawString(connId, response);
            }
            catch (Exception ex)
        {
                Log($"处理API消息异常: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 处理消息队列
        /// </summary>
        private void ProcessMessageQueue(IntPtr connId, string content)
        {
            try
            {
                var queue = ZCGMessageQueue.FromZCGFormat(content);
                if (queue != null)
                {
                    OnMessageQueueReceived?.Invoke(queue);

                    // 广播给其他客户端
                    var message = FrameworkMessage.CreateMessageQueue(queue);
                    BroadcastExcept(connId, message);
                }
            }
            catch (Exception ex)
            {
                Log($"处理消息队列异常: {ex.Message}");
            }
        }

        #endregion

    }
}
