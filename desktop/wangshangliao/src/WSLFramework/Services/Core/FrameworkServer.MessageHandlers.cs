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
        #region 消息处理器

        /// <summary>
        /// 处理登录请求
        /// 登录流程:
        /// 1. 解析主框架发送的登录信息
        /// 2. 连接CDP获取旺商聊凭证
        /// 3. 使用NIM Token登录云信服务器
        /// 4. 返回登录结果给主框架
        /// </summary>
        private async Task<FrameworkMessage> HandleLoginAsync(IntPtr connId, FrameworkMessage message)
        {
            Log($"[登录] ===== 处理登录请求 (ConnID={connId}) =====");
            Log($"[登录] 消息内容: Content={message.Content}, Extra={message.Extra}");
            
            AccountLoginInfo loginInfo = null;
            try
            {
                if (!string.IsNullOrEmpty(message.Extra))
                {
                    loginInfo = AccountLoginInfo.FromJson(message.Extra);
                }
                if (loginInfo == null && !string.IsNullOrEmpty(message.Content))
                {
                    loginInfo = AccountLoginInfo.FromJson(message.Content);
                }
            }
            catch (Exception ex)
            {
                Log($"[登录] 解析登录信息异常: {ex.Message}");
            }
            
            if (loginInfo == null)
            {
                loginInfo = new AccountLoginInfo
                {
                    Nickname = message.SenderId ?? "未知",
                    Wwid = message.SenderId ?? "",
                    GroupId = message.GroupId ?? "",
                    Account = message.LoginAccount ?? message.SenderId ?? "",
                    Status = "登录成功"
                };
            }
            else
            {
                loginInfo.Status = "登录成功";
            }
            
            // 更新客户端信息 (线程安全)
            if (_clients.TryGetValue(connId, out var clientInfo))
            {
                lock (clientInfo)
                {
                    clientInfo.LoggedIn = true;
                    clientInfo.UserId = message.SenderId;
                    clientInfo.Nickname = loginInfo.Nickname;
                    clientInfo.Wwid = loginInfo.Wwid;
                    clientInfo.GroupId = loginInfo.GroupId;
                    clientInfo.Account = loginInfo.Account;
                }
            }

            // 更新当前登录账号
            CurrentLoginAccount = loginInfo.Account;
            
            // 步骤1: 连接CDP获取旺商聊凭证
            string nimId = null;
            string nimToken = null;
            
            if (_cdpBridge != null && !IsCDPConnected)
            {
                await _cdpLock.WaitAsync();
                try
                {
                    if (!IsCDPConnected)
                    {
                        Log($"[登录] 正在连接CDP...");
                        var connected = await _cdpBridge.ConnectAsync();
                        if (!connected)
                        {
                            loginInfo.Status = "CDP连接失败";
                            OnClientLoggedIn?.Invoke(connId, loginInfo);
                            Log($"[登录] ✗ CDP连接失败");
                            return FrameworkMessage.CreateError(message.Id, "无法连接到旺商聊，请确保旺商聊已启动");
                        }
                        Log($"[登录] ✓ CDP连接成功");
                    }
                }
                finally
                {
                    _cdpLock.Release();
                }
            }
            
            // 步骤2: 从CDP获取NIM Token
            if (_cdpBridge != null && IsCDPConnected)
            {
                try
                {
                    Log($"[登录] 正在从CDP获取NIM凭证...");
                    var userInfo = await _cdpBridge.GetCurrentUserInfoAsync();
                    if (userInfo != null)
                    {
                        nimId = userInfo.nimId;
                        nimToken = userInfo.nimToken; // 从用户信息中获取 NIM Token
                        loginInfo.Nickname = userInfo.nickname ?? loginInfo.Nickname;
                        loginInfo.Wwid = userInfo.wwid ?? loginInfo.Wwid;
                        loginInfo.Account = userInfo.account ?? loginInfo.Account;
                        Log($"[登录] ✓ 获取用户信息成功: nickname={userInfo.nickname}, nimId={nimId}");
                        
                        if (!string.IsNullOrEmpty(nimToken))
                        {
                            Log($"[登录] ✓ 获取NIM Token成功");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log($"[登录] 获取NIM凭证异常: {ex.Message}");
                }
            }
            
            // 步骤3: 登录NIM SDK (如果有Token)
            if (_nimService != null && !string.IsNullOrEmpty(nimId) && !string.IsNullOrEmpty(nimToken))
            {
                try
                {
                    if (!_nimService.IsLoggedIn)
                    {
                        Log($"[登录] 正在登录NIM SDK...");
                        var nimLoginResult = await _nimService.LoginAsync(nimId, nimToken);
                        if (nimLoginResult)
                        {
                            Log($"[登录] ✓ NIM SDK登录成功");
                        }
                        else
                        {
                            Log($"[登录] ✗ NIM SDK登录失败");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log($"[登录] NIM SDK登录异常: {ex.Message}");
                }
            }
            
            // 步骤4: 尝试登录NIM直连客户端 (优先级更高)
            var nimDirect = NimDirectClient.Instance;
            if (!nimDirect.IsLoggedIn && !string.IsNullOrEmpty(nimId) && !string.IsNullOrEmpty(nimToken))
            {
                try
                {
                    Log($"[登录] 正在登录NIM直连客户端...");
                    var directResult = await nimDirect.LoginWithTokenAsync(nimId, nimToken);
                    if (directResult)
                    {
                        Log($"[登录] ✓ NIM直连客户端登录成功");
                    }
                }
                catch (Exception ex)
                {
                    Log($"[登录] NIM直连客户端登录异常: {ex.Message}");
                }
            }
            
            // 触发登录成功事件
            OnClientLoggedIn?.Invoke(connId, loginInfo);
            Log($"[登录] ✓ 客户端登录成功: {loginInfo.Nickname} (wwid={loginInfo.Wwid}, 群号={loginInfo.GroupId})");
            
            // 构建登录响应 (包含NIM信息)
            var response = FrameworkMessage.CreateSuccess(message.Id, FrameworkMessageType.LoginResult, "登录成功");
            
            // Extra字段包含详细登录信息 (参考框架通信架构说明.md)
            var extraInfo = new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(new
            {
                nimId = nimId ?? "",
                nickname = loginInfo.Nickname,
                groupId = loginInfo.GroupId,
                wwid = loginInfo.Wwid,
                account = loginInfo.Account,
                nimConnected = _nimService?.IsLoggedIn ?? false,
                nimDirectConnected = nimDirect.IsLoggedIn,
                cdpConnected = IsCDPConnected
            });
            response.Extra = extraInfo;
            
            return response;
        }
        
        /// <summary>
        /// 处理API请求
        /// </summary>
        private async Task<FrameworkMessage> HandleApiRequestAsync(IntPtr connId, FrameworkMessage message)
        {
            try
            {
                var apiName = message.ApiName;
                var apiParams = message.ApiParams ?? new string[0];

                if (string.IsNullOrEmpty(apiName))
                {
                    // 尝试从Content解析
                    if (!string.IsNullOrEmpty(message.Content) && message.Content.Contains("|"))
                    {
                        var parts = message.Content.Split(ZCGProtocol.API_SEPARATOR);
                        apiName = parts[0];
                        apiParams = new string[parts.Length - 1];
                        Array.Copy(parts, 1, apiParams, 0, apiParams.Length);
                    }
                }

                string result = await ExecuteApiAsync(apiName, apiParams);

                return FrameworkMessage.CreateApiResponse(message.Id, apiName, result);
            }
            catch (Exception ex)
            {
                return FrameworkMessage.CreateError(message.Id, ex.Message);
            }
        }

        /// <summary>
        /// 处理发送群消息 - 优先使用 NIM 直连（支持AES加密）
        /// 发送优先级: NimDirectClient > NIM SDK > CDP
        /// </summary>
        private async Task<FrameworkMessage> HandleSendGroupMessageAsync(FrameworkMessage message)
        {
            var groupId = ResolveGroupIdForSend(message.GroupId ?? message.ReceiverId);
            var content = message.Content;
            
            if (string.IsNullOrEmpty(groupId) || string.IsNullOrEmpty(content))
            {
                return FrameworkMessage.CreateError(message.Id, "群ID或消息内容为空");
            }
            
            // 使用统一的发送方法（自动选择最优发送方式）
            var success = await SendGroupMessageViaCDPAsync(groupId, content);

                // 记录API调用
            OnApiCall?.Invoke("发送群消息(文本版)", new[] { message.LoginAccount, content, groupId, "1", "0" }, success ? "成功" : "失败");
            
            return success
                    ? FrameworkMessage.CreateSuccess(message.Id, FrameworkMessageType.SendGroupMessage, "消息已发送")
                : FrameworkMessage.CreateError(message.Id, "发送消息失败");
        }
        
        /// <summary>
        /// 处理发送私聊消息 - 优先使用 NIM 直连（支持AES加密）
        /// </summary>
        private async Task<FrameworkMessage> HandleSendPrivateMessageAsync(FrameworkMessage message)
        {
            var toId = message.ReceiverId;
            var content = message.Content;
            
            if (string.IsNullOrEmpty(toId) || string.IsNullOrEmpty(content))
            {
                return FrameworkMessage.CreateError(message.Id, "接收者ID或消息内容为空");
            }
            
            bool success = false;
            
            // 优先使用 NIM 直连
            var nimDirect = NimDirectClient.Instance;
            if (nimDirect.IsLoggedIn)
            {
            try
            {
                    success = await nimDirect.SendP2PMessageAsync(toId, content);
                    if (success)
                    {
                        Log($"[NIM直连] ✓ 私聊消息发送成功: to={toId}");
                    }
                }
                catch (Exception ex)
                {
                    Log($"[NIM直连] 私聊消息发送异常: {ex.Message}");
                }
            }
            
            // 备用: 使用 CDP
            if (!success && _cdpBridge != null && IsCDPConnected)
            {
                await _cdpLock.WaitAsync();
                try
                {
                    success = await _cdpBridge.SendPrivateMessageAsync(toId, content);
                    if (success)
                    {
                        Log($"[CDP] ✓ 私聊消息发送成功: to={toId}");
                    }
            }
            finally
            {
                _cdpLock.Release();
            }
            }

            OnApiCall?.Invoke("发送好友消息", new[] { message.LoginAccount, content, toId }, success ? "成功" : "失败");

            return success
                ? FrameworkMessage.CreateSuccess(message.Id, FrameworkMessageType.SendPrivateMessage, "消息已发送")
                : FrameworkMessage.CreateError(message.Id, "发送消息失败");
        }

        /// <summary>
        /// 处理群操作 (禁言/解禁等)
        /// 优先使用 HTTP API，备用 NimDirectClient
        /// </summary>
        private async Task<FrameworkMessage> HandleGroupOperationAsync(FrameworkMessage message)
        {
            // 1. 首先尝试解析新格式的消息 (JSON: Operation, GroupId, MemberId)
            string operation = null;
            string groupId = null;
            string memberId = null;
            
            try
            {
                if (!string.IsNullOrEmpty(message.Content))
                {
                    operation = ExtractJsonValue(message.Content, "Operation");
                    groupId = ExtractJsonValue(message.Content, "GroupId");
                    memberId = ExtractJsonValue(message.Content, "MemberId");
                }
            }
            catch { }
            
            // 如果是新格式消息
            if (!string.IsNullOrEmpty(operation))
            {
                groupId = groupId ?? _activeGroupId;
                
                if (string.IsNullOrEmpty(groupId))
                {
                    Log($"[群操作] 失败: 群号为空");
                    return FrameworkMessage.CreateError(message.Id, "群号为空，请先设置绑定群号");
                }
                
                Log($"[群操作] 收到指令: {operation}, 群号: {groupId}, 成员: {memberId ?? "(全体)"}");
                
                bool success = false;
                string resultMsg = "";
                
                // 方法1: 使用 HTTP API（静默执行，无弹窗）
                try
                {
                    var httpApi = WangShangLiaoHttpApi.Instance;
                    
                    // 解析群号为 long
                    if (!long.TryParse(groupId, out long groupIdLong))
                    {
                        // 尝试去掉前缀
                        var cleanId = groupId.Replace("team-", "");
                        if (!long.TryParse(cleanId, out groupIdLong))
                        {
                            Log($"[群操作] 群号格式错误: {groupId}");
                            return FrameworkMessage.CreateError(message.Id, $"群号格式错误: {groupId}");
                        }
                    }
                    
                    ApiResponse apiResult = null;
                    
                    switch (operation.ToLower())
                    {
                        case "mute_all":
                            Log($"[群操作] 执行全体禁言 (HTTP API): 群号={groupIdLong}");
                            apiResult = await httpApi.MuteAllAsync(groupIdLong);
                            resultMsg = "全体禁言";
                            break;
                            
                        case "unmute_all":
                            Log($"[群操作] 执行全体解禁 (HTTP API): 群号={groupIdLong}");
                            apiResult = await httpApi.UnmuteAllAsync(groupIdLong);
                            resultMsg = "全体解禁";
                            break;
                            
                        case "mute_member":
                            if (string.IsNullOrEmpty(memberId))
                            {
                                return FrameworkMessage.CreateError(message.Id, "禁言成员需要指定成员ID");
                            }
                            if (long.TryParse(memberId, out long memberIdLong))
                            {
                                Log($"[群操作] 执行成员禁言 (HTTP API): 群号={groupIdLong}, 成员={memberIdLong}");
                                apiResult = await httpApi.MuteMemberAsync(groupIdLong, memberIdLong);
                                resultMsg = "成员禁言";
                            }
                            break;
                            
                        default:
                            Log($"[群操作] 未知操作: {operation}");
                            return FrameworkMessage.CreateError(message.Id, $"未知操作: {operation}");
                    }
                    
                    if (apiResult != null && apiResult.Success)
                    {
                        success = true;
                        Log($"[群操作] ✓ {resultMsg}成功 (HTTP API)");
                    }
                    else
                    {
                        Log($"[群操作] HTTP API 返回: {apiResult?.Message ?? "无响应"}");
                    }
                }
                catch (Exception ex)
                {
                    Log($"[群操作] HTTP API 异常: {ex.Message}");
                }
                
                // 方法2: 如果 HTTP API 失败，尝试 NimDirectClient
                if (!success)
                {
                    var nimClient = NimDirectClient.Instance;
                    if (nimClient.IsLoggedIn || BotLoginService.Instance.IsLoggedIn)
                    {
                        try
                        {
                            switch (operation.ToLower())
                            {
                                case "mute_all":
                                    success = await nimClient.MuteTeamAllAsync(groupId, true);
                                    break;
                                case "unmute_all":
                                    success = await nimClient.MuteTeamAllAsync(groupId, false);
                                    break;
                                case "mute_member":
                                    if (!string.IsNullOrEmpty(memberId))
                                        success = await nimClient.MuteTeamMemberAsync(groupId, memberId, 0);
                                    break;
                            }
                            
                            if (success)
                            {
                                Log($"[群操作] ✓ {resultMsg}成功 (NIM直连)");
                            }
                        }
                        catch (Exception ex)
                        {
                            Log($"[群操作] NIM直连异常: {ex.Message}");
                        }
                    }
                }
                
                OnApiCall?.Invoke("ww_群禁言解禁", new[] { operation, groupId, memberId ?? "全体" }, success ? "成功" : "失败");
                
                return success 
                    ? FrameworkMessage.CreateSuccess(message.Id, FrameworkMessageType.GroupOperation, $"{resultMsg}成功")
                    : FrameworkMessage.CreateError(message.Id, $"{resultMsg}失败，可能需要管理员权限");
            }
            
            // 2. 兼容旧格式 (Extra = "mute" 或 "unmute")
            var nimClientOld = NimDirectClient.Instance;
            
            if (message.Extra == "mute" || message.Extra == "unmute")
            {
                bool isMute = message.Extra == "mute";
                var action = isMute ? "禁言" : "解禁";
                var oldGroupId = message.GroupId ?? _activeGroupId;
                var oldMemberId = ExtractJsonValue(message.Content, "memberId");
                
                Log($"[群操作] 旧格式: {action} | 群号={oldGroupId} | 成员={oldMemberId ?? "全体"}");
                
                bool oldSuccess = false;
                try
                {
                    if (!string.IsNullOrEmpty(oldMemberId))
                    {
                        int seconds = isMute ? 0 : -1;
                        oldSuccess = await nimClientOld.MuteTeamMemberAsync(oldGroupId, oldMemberId, seconds);
                    }
                    else
                    {
                        oldSuccess = await nimClientOld.MuteTeamAllAsync(oldGroupId, isMute);
                    }
                }
                catch (Exception ex)
                {
                    Log($"[群操作] {action}异常: {ex.Message}");
                }
                
                return oldSuccess 
                    ? FrameworkMessage.CreateSuccess(message.Id, FrameworkMessageType.GroupOperation, $"{action}成功")
                    : FrameworkMessage.CreateError(message.Id, $"{action}失败");
            }
            
            return FrameworkMessage.CreateError(message.Id, "无效的群操作请求");
        }

        /// <summary>
        /// 处理CDP命令
        /// </summary>
        private async Task<FrameworkMessage> HandleCDPCommandAsync(FrameworkMessage message)
        {
            if (_cdpBridge == null || !IsCDPConnected)
            {
                return FrameworkMessage.CreateError(message.Id, "CDP 未连接");
            }

            await _cdpLock.WaitAsync();
            try
            {
                var result = await _cdpBridge.EvaluateAsync(message.Content);
                return FrameworkMessage.CreateSuccess(message.Id, FrameworkMessageType.CDPResponse, result);
            }
            catch (Exception ex)
            {
                return FrameworkMessage.CreateError(message.Id, ex.Message);
            }
            finally
            {
                _cdpLock.Release();
            }
        }
        
        /// <summary>
        /// 处理开始算账命令 - 接管群聊
        /// 开始算账流程:
        /// 1. 验证群号
        /// 2. 检查CDP/NIM连接状态
        /// 3. 添加到活跃群列表
        /// 4. 启动周期管理器和定时消息服务
        /// 5. 开始自动发送倒计时、封盘提醒、开奖通知等消息
        /// </summary>
        private async Task<FrameworkMessage> HandleStartAccountingAsync(IntPtr connId, FrameworkMessage message)
        {
            try
            {
                Log($"[算账] ===== 收到开始算账命令 =====");
                Log($"[算账] ConnID={connId}");
                Log($"[算账] 消息格式: {{Type:60, GroupId:\"{message.GroupId}\", Content:\"{message.Content}\"}}");
                
                // 从消息中获取群号 (消息格式参考 框架通信架构说明.md)
                var groupId = message.GroupId ?? message.Content;
                
                // 如果主框架未传入群号，尝试从多个来源获取
                if (string.IsNullOrEmpty(groupId))
                {
                    groupId = _activeGroupId;
                    Log($"[算账] 主框架未传入群号，尝试使用活跃群号: {groupId ?? "(空)"}");
                }
                
                if (string.IsNullOrEmpty(groupId))
                {
                    // 从 BotLoginService 获取当前账号绑定的群号
                    groupId = BotLoginService.Instance?.GetCurrentGroupId();
                    Log($"[算账] 尝试从 BotLoginService 获取: {groupId ?? "(空)"}");
                }
                
                if (string.IsNullOrEmpty(groupId))
                {
                    // 从 AccountManager 获取第一个有群号的账号
                    var accounts = Models.AccountManager.Instance?.Accounts;
                    if (accounts != null && accounts.Count > 0)
                    {
                        var accountWithGroup = accounts.FirstOrDefault(a => !string.IsNullOrEmpty(a.GroupId));
                        if (accountWithGroup != null)
                        {
                            groupId = accountWithGroup.GroupId;
                            Log($"[算账] 从 AccountManager 获取: {groupId}");
                        }
                    }
                }
                
                if (string.IsNullOrEmpty(groupId))
                {
                    Log($"[算账] 错误: 未指定群号，且无法从副框架配置获取");
                    return FrameworkMessage.CreateError(message.Id, "未指定群号，请先在副框架配置绑定群号");
                }
                
                Log($"[算账] 目标群号: {groupId}");
                
                // 步骤1: 检查 CDP 连接状态
                if (_cdpBridge == null)
                {
                    Log($"[算账] 警告: CDPBridge 为空，尝试初始化...");
                    InitializeCDP();
                }
                
                if (_cdpBridge != null && !_cdpBridge.IsConnected)
                {
                    Log($"[算账] CDP 未连接，尝试连接...");
                    var connected = await _cdpBridge.ConnectAsync();
                    Log($"[算账] CDP 连接结果: {(connected ? "成功" : "失败")}");
                }
                
                // 步骤2: 登录 NIM SDK (如果未登录)
                if (_nimService != null && _nimService.IsInitialized && !_nimService.IsLoggedIn)
                {
                    Log($"[算账] NIM SDK 未登录，尝试从 CDP 获取凭证...");
                    await LoginNIMFromCDPAsync();
                }
                
                // 步骤3: 登录 NIM 直连客户端 (如果未登录)
                var nimDirect = NimDirectClient.Instance;
                if (!nimDirect.IsLoggedIn && _cdpBridge != null && IsCDPConnected)
                {
                    try
                    {
                        var userInfo = await _cdpBridge.GetCurrentUserInfoAsync();
                        if (userInfo != null && !string.IsNullOrEmpty(userInfo.nimToken))
                        {
                            Log($"[算账] 正在登录NIM直连客户端...");
                            // 使用旺商聊默认 AppKey
                            var appKey = "45c6af3c98409b18a84451215d0bdd6e";
                            await nimDirect.LoginAsync(appKey, userInfo.nimId, userInfo.nimToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log($"[算账] NIM直连客户端登录异常: {ex.Message}");
                    }
                }
                
                _activeGroupId = groupId;
                _isAccountingStarted = true;
                
                // 步骤4: 添加到活跃群列表
                Log($"[算账] 添加群 {groupId} 到活跃群列表...");
                TimedMessageService.Instance.AddActiveGroup(groupId);
                var activeGroups = TimedMessageService.Instance.GetActiveGroups();
                Log($"[算账] 当前活跃群列表: [{string.Join(", ", activeGroups)}]");
                
                // 步骤5: 启动周期管理器
                if (_periodManager != null && !_periodManager.IsRunning)
                {
                    _periodManager.Start();
                    Log($"[算账] ✓ 周期管理器已启动");
                }
                else if (_periodManager == null)
                {
                    Log($"[算账] 警告: 周期管理器为空，请检查初始化!");
                }
                else
                {
                    Log($"[算账] 周期管理器已在运行中");
                }
                
                // 步骤6: 启用定时消息服务 (倒计时、封盘提醒、开奖通知)
                TimedMessageService.Instance.CountdownEnabled = true;
                TimedMessageService.Instance.CloseNotifyEnabled = true;
                TimedMessageService.Instance.OpenNotifyEnabled = true;
                Log($"[算账] ✓ 定时消息服务已启用");
                Log($"[算账]   - 倒计时播报: 启用");
                Log($"[算账]   - 封盘通知: 启用");
                Log($"[算账]   - 开奖通知: 启用");
                
                // 步骤7: 设置NIM直连客户端活跃群
                if (nimDirect.IsLoggedIn)
                {
                    nimDirect.SetActiveGroup(groupId);
                    Log($"[算账] ✓ NIM直连客户端已设置活跃群: {groupId}");
                }
                
                Log($"[算账] ==========================================");
                Log($"[算账] ✓ 开始算账成功，已接管群: {groupId}");
                Log($"[算账]   - CDP连接: {(IsCDPConnected ? "已连接" : "未连接")}");
                Log($"[算账]   - NIM SDK: {(_nimService?.IsLoggedIn == true ? "已登录" : "未登录")}");
                Log($"[算账]   - NIM直连: {(nimDirect.IsLoggedIn ? "已登录" : "未登录")}");
                Log($"[算账] ==========================================");
                
                // 构建响应 (包含详细状态信息)
                var response = FrameworkMessage.CreateSuccess(message.Id, FrameworkMessageType.StartAccounting, $"已接管群聊: {groupId}");
                response.GroupId = groupId;
                response.Extra = new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(new
                {
                    groupId = groupId,
                    cdpConnected = IsCDPConnected,
                    nimSdkConnected = _nimService?.IsLoggedIn ?? false,
                    nimDirectConnected = nimDirect.IsLoggedIn,
                    periodManagerRunning = _periodManager?.IsRunning ?? false,
                    timedServiceEnabled = true
                });
                
                return response;
            }
            catch (Exception ex)
            {
                Log($"[算账] 失败: {ex.Message}");
                Log($"[算账] 堆栈: {ex.StackTrace}");
                return FrameworkMessage.CreateError(message.Id, ex.Message);
            }
        }
        
        /// <summary>
        /// 处理停止算账命令
        /// </summary>
        private FrameworkMessage HandleStopAccounting(IntPtr connId, FrameworkMessage message)
        {
            try
            {
                Log($"收到停止算账命令 (ConnID={connId})");
                
                _isAccountingStarted = false;
                
                // 停止周期管理器
                if (_periodManager != null && _periodManager.IsRunning)
                {
                    _periodManager.Stop();
                    Log("周期管理器已停止");
                }
                
                // 清空活跃群列表
                foreach (var groupId in TimedMessageService.Instance.GetActiveGroups().ToList())
                {
                    TimedMessageService.Instance.RemoveActiveGroup(groupId);
                }
                
                Log("✓ 停止算账成功");
                
                return FrameworkMessage.CreateSuccess(message.Id, FrameworkMessageType.StopAccounting, "已停止算账");
            }
            catch (Exception ex)
            {
                Log($"停止算账失败: {ex.Message}");
                return FrameworkMessage.CreateError(message.Id, ex.Message);
            }
        }
        
        /// <summary>
        /// 处理设置活跃群命令
        /// </summary>
        private FrameworkMessage HandleSetActiveGroup(IntPtr connId, FrameworkMessage message)
        {
            try
            {
                var groupId = message.GroupId ?? message.Content;
                if (string.IsNullOrEmpty(groupId))
                {
                    return FrameworkMessage.CreateError(message.Id, "群号不能为空");
                }
                
                _activeGroupId = groupId;
                TimedMessageService.Instance.AddActiveGroup(groupId);
                
                Log($"✓ 设置活跃群: {groupId}");
                
                return FrameworkMessage.CreateSuccess(message.Id, FrameworkMessageType.SetActiveGroup, $"活跃群已设置: {groupId}");
            }
            catch (Exception ex)
            {
                Log($"设置活跃群失败: {ex.Message}");
                return FrameworkMessage.CreateError(message.Id, ex.Message);
            }
        }

        /// <summary>
        /// 处理获取绑定群号请求
        /// </summary>
        private FrameworkMessage HandleGetBoundGroup(FrameworkMessage message)
        {
            try
            {
                // 优先从活跃群ID获取
                var groupId = _activeGroupId;
                var groupName = "";
                
                Log($"[获取绑定群] 来源1 - 活跃群ID: {groupId ?? "(空)"}");
                
                // 如果没有活跃群，从登录账号获取
                if (string.IsNullOrEmpty(groupId))
                {
                    groupId = BotLoginService.Instance.GetCurrentGroupId();
                    Log($"[获取绑定群] 来源2 - BotLoginService: {groupId ?? "(空)"}");
                }
                
                // 如果还没有，从AccountManager获取
                if (string.IsNullOrEmpty(groupId))
                {
                    var account = Models.AccountManager.Instance.Accounts.FirstOrDefault(a => !string.IsNullOrEmpty(a.GroupId));
                    if (account != null)
                    {
                        groupId = account.GroupId;
                        Log($"[获取绑定群] 来源3 - AccountManager: {groupId}");
                    }
                }
                
                // 如果还没有，从ConfigService获取
                if (string.IsNullOrEmpty(groupId))
                {
                    var config = ConfigService.Instance;
                    groupId = config?.BindGroupId;
                    if (string.IsNullOrEmpty(groupId))
                    {
                        groupId = config?.GroupId;
                    }
                    Log($"[获取绑定群] 来源4 - ConfigService: {groupId ?? "(空)"}");
                }
                
                // 如果还没有，从CDP群列表中获取第一个群作为默认群
                if (string.IsNullOrEmpty(groupId) && _cdpBridge != null && _cdpBridge.IsConnected)
                {
                    try
                    {
                        var groups = _cdpBridge.GetGroupListAsync().Result;
                        if (groups != null && groups.Length > 0)
                        {
                            // 使用第一个群作为默认绑定群
                            var firstGroup = groups[0];
                            // groupId 格式可能是 "team-xxxxx" 或纯数字
                            var gid = firstGroup.groupId ?? "";
                            if (gid.StartsWith("team-"))
                            {
                                groupId = gid.Substring(5);
                            }
                            else if (!string.IsNullOrEmpty(gid))
                            {
                                groupId = gid;
                            }
                            groupName = firstGroup.groupName ?? "";
                            Log($"[获取绑定群] 来源5 - CDP群列表首个群: {groupId}, 群名: {groupName}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log($"[获取绑定群] 从CDP获取群列表失败: {ex.Message}");
                    }
                }
                
                // 尝试获取群名（从CDP群列表获取）
                if (!string.IsNullOrEmpty(groupId) && string.IsNullOrEmpty(groupName) && _cdpBridge != null && _cdpBridge.IsConnected)
                {
                    try
                    {
                        var groups = _cdpBridge.GetGroupListAsync().Result;
                        if (groups != null)
                        {
                            var matchingGroup = groups.FirstOrDefault(g => 
                                g.groupId == groupId || 
                                g.groupId == $"team-{groupId}" || 
                                g.groupId?.Replace("team-", "") == groupId);
                            if (matchingGroup != null)
                            {
                                groupName = matchingGroup.groupName ?? "";
                                Log($"[获取绑定群] 从群列表获取到群名: {groupName}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log($"[获取绑定群] 获取群名失败: {ex.Message}");
                    }
                }
                
                Log($"[获取绑定群] 最终结果 - 群号: {groupId ?? "(空)"}, 群名: {groupName}");
                
                // 返回格式: groupId|groupName
                var result = $"{groupId ?? ""}|{groupName}";
                return FrameworkMessage.CreateSuccess(message.Id, FrameworkMessageType.GetBoundGroup, result);
            }
            catch (Exception ex)
            {
                Log($"获取绑定群号失败: {ex.Message}");
                return FrameworkMessage.CreateError(message.Id, ex.Message);
            }
        }

        /// <summary>
        /// 处理获取账号信息请求
        /// ★★★ 优先使用已保存的账号信息，CDP为补充 ★★★
        /// </summary>
        private async Task<FrameworkMessage> HandleGetAccountInfoAsync(FrameworkMessage message)
        {
            try
            {
                Log("[获取账号信息] 开始获取...");
                
                // 从 BotLoginService 获取当前登录账号
                var loginService = BotLoginService.Instance;
                var currentAccount = loginService.CurrentAccount;
                
                // ★★★ 如果没有CurrentAccount，尝试从AccountManager获取 ★★★
                if (currentAccount == null)
                {
                    currentAccount = Models.AccountManager.Instance.GetAutoLoginAccount();
                    Log($"[获取账号信息] 从AccountManager获取账号: {currentAccount?.Account ?? "null"}");
                }
                
                // 从 CDP 获取最新的用户信息 (作为补充)
                var cdp = CDPService.Instance;
                WslUserInfo userInfo = null;
                if (cdp.IsConnected)
                {
                    userInfo = await cdp.GetCurrentUserAsync();
                }
                
                // 获取绑定群号和群名
                var groupId = _activeGroupId ?? currentAccount?.GroupId ?? "";
                var groupName = currentAccount?.GroupName ?? ""; // ★★★ 优先使用已保存的群名称
                
                // 如果没有保存的群名称，尝试从CDP获取
                if (string.IsNullOrEmpty(groupName) && !string.IsNullOrEmpty(groupId) && cdp != null && cdp.IsConnected)
                {
                    var groups = await cdp.GetGroupListAsync();
                    var group = groups?.FirstOrDefault(g => g.GroupId == groupId || g.InternalId == groupId);
                    if (group != null)
                    {
                        groupName = group.Name;
                        // 保存群名称到账号配置
                        if (currentAccount != null)
                        {
                            currentAccount.GroupName = groupName;
                            Models.AccountManager.Instance.Save();
                        }
                    }
                }
                
                // ★★★ 构建账号信息 - 优先使用已保存的账号 ★★★
                var accountInfo = new
                {
                    AccountId = !string.IsNullOrEmpty(currentAccount?.Account) ? currentAccount.Account : (userInfo?.AccountId ?? ""),
                    Wwid = !string.IsNullOrEmpty(currentAccount?.Wwid) ? currentAccount.Wwid : (userInfo?.Wwid ?? ""),
                    Nickname = !string.IsNullOrEmpty(currentAccount?.Nickname) ? currentAccount.Nickname : (userInfo?.Nickname ?? ""),
                    GroupId = groupId,
                    GroupName = groupName,
                    NimId = !string.IsNullOrEmpty(currentAccount?.NimAccid) ? currentAccount.NimAccid : (userInfo?.NimId ?? ""),
                    IsLoggedIn = loginService.IsLoggedIn
                };
                
                var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
                var jsonContent = serializer.Serialize(accountInfo);
                
                Log($"[获取账号信息] 返回: AccountId={accountInfo.AccountId}, Nickname={accountInfo.Nickname}, GroupId={groupId}");
                
                return new FrameworkMessage
                {
                    Id = message.Id,
                    Type = FrameworkMessageType.AccountInfo,
                    Content = jsonContent,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                Log($"获取账号信息失败: {ex.Message}");
                return FrameworkMessage.CreateError(message.Id, ex.Message);
            }
        }

        #endregion

    }
}
