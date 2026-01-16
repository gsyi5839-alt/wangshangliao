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
        #region 服务端控制

        /// <summary>
        /// 启动服务端
        /// </summary>
        public async Task<bool> StartAsync()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(FrameworkServer));

            if (_isStarting || IsRunning)
                return IsRunning;

            lock (_serverLock)
            {
                if (_isStarting || IsRunning)
                    return IsRunning;
                _isStarting = true;
            }

            try
            {
                Log($"正在启动 HPSocket PACK 服务端，端口: {Port}...");
                Log($"  - 配置: PackHeaderFlag=0x{PackHeaderFlag:X4}, MaxPackSize={MaxPackSize}");
                Log($"  - _server 是否为空: {_server == null}");

                if (_server == null)
                {
                    Log("✗ HPSocket 服务器未初始化");
                    return false;
                }

                _server.Address = "0.0.0.0";
                _server.Port = Port;
                
                Log($"  - 地址: {_server.Address}:{_server.Port}");

                bool started = _server.Start();
                Log($"  - Start() 返回: {started}");

                if (started)
                {
                    Log($"✓ HPSocket PACK 服务端已启动，监听端口: {Port}");

                    // 连接 CDP
                    if (_cdpBridge != null)
                    {
                        Log("正在连接旺商聊 CDP...");
                        var cdpResult = await _cdpBridge.ConnectAsync();
                        if (cdpResult)
                        {
                            Log("✓ CDP 连接成功");
                            await FetchWangShangLiaoInfoAsync();
                        }
                        else
                        {
                            Log("! CDP 连接失败，将在客户端请求时重试");
                        }
                    }

                    return true;
                }
                else
                {
                    // 获取详细错误信息
                    var errorCode = _server.ErrorCode;
                    var errorMsg = _server.ErrorMessage;
                    Log($"✗ 服务端启动失败: 错误码={errorCode}, 错误信息={errorMsg}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log($"启动异常: {ex.Message}");
                return false;
            }
            finally
            {
                _isStarting = false;
            }
        }

        /// <summary>
        /// 【已废弃】手动连接 CDP - 改用 BotLoginService
        /// </summary>
        [Obsolete("CDP已废弃，请使用 BotLoginService 登录")]
        public async Task<bool> ConnectCDPAsync()
        {
            // 【已废弃】CDP 已被 BotLoginService 替代
            Log("⚠ CDP 已废弃，请使用账号密码登录旺商聊");
            Log("请在【账号列表】中添加账号并登录");
            
            // 返回 BotLoginService 的登录状态
            return BotLoginService.Instance.IsLoggedIn;
        }
        
        /// <summary>
        /// 【旧方法保留】手动连接 CDP (已废弃)
        /// </summary>
        private async Task<bool> ConnectCDPAsync_Deprecated()
        {
            if (_cdpBridge == null)
            {
                Log("CDP 未初始化");
                return false;
            }
            
            if (IsCDPConnected)
            {
                Log("CDP 已经连接");
                return true;
            }
            
            await _cdpLock.WaitAsync();
            try
            {
                Log("正在连接旺商聊 CDP...");
                var result = await _cdpBridge.ConnectAsync();
                
                if (result)
                {
                    Log("✓ CDP 连接成功");
                    await FetchWangShangLiaoInfoAsync();
                }
                else
                {
                    Log("✗ CDP 连接失败，请确保旺商聊已启动并开启调试端口 9222");
                }
                
                return result;
            }
            finally
            {
                _cdpLock.Release();
            }
        }

        /// <summary>
        /// 获取旺商聊账号和群组信息
        /// </summary>
        private async Task FetchWangShangLiaoInfoAsync()
        {
            try
            {
                Log("正在获取旺商聊账号信息...");

                var userInfo = await _cdpBridge.GetCurrentUserInfoAsync();
                if (userInfo != null && string.IsNullOrEmpty(userInfo.error))
                {
                    Log($"✓ 获取用户信息: {userInfo.nickname} (wwid={userInfo.wwid})");
                    CurrentLoginAccount = userInfo.wwid;
                    
                    // 更新心跳服务的用户ID和名称
                    try
                    {
                        if (long.TryParse(userInfo.wwid, out long userId))
                        {
                            HeartbeatService.Instance.CurrentUserId = userId;
                            HeartbeatService.Instance.CurrentUserName = userInfo.nickname;
                            HeartbeatService.Instance.SetOnlineStatus(true);
                            Log($"✓ 心跳服务已更新: 用户ID={userId}, 名称={userInfo.nickname}");
                        }
                    }
                    catch { }
                }
                else
                {
                    Log("! 未能获取用户信息");
                    userInfo = new WangShangLiaoUserInfo
                    {
                        nickname = "旺商聊用户",
                        wwid = "",
                        account = ""
                    };
                }

                // 群聊使用用户添加账户时指定的固定群号，不再自动获取所有群列表
                var groups = new WangShangLiaoGroupInfo[0];

                OnWangShangLiaoConnected?.Invoke(userInfo, groups);
            }
            catch (Exception ex)
            {
                Log($"获取旺商聊信息失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 停止服务端
        /// </summary>
        public async Task StopAsync()
        {
            try
            {
                Log("正在停止服务端...");

                _cts?.Cancel();

                if (_cdpBridge != null)
                {
                    await _cdpBridge.DisconnectAsync();
                }

                lock (_serverLock)
                {
                    if (_server?.HasStarted == true)
                    {
                        _server.Stop();
                    }
                }

                _clients.Clear();
                Log("服务端已停止");
            }
            catch (Exception ex)
            {
                Log($"停止异常: {ex.Message}");
            }
        }
        
        #endregion

    }
}
