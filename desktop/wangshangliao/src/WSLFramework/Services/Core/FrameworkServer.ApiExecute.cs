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
        #region API执行

        /// <summary>
        /// 执行API调用
        /// </summary>
        private async Task<string> ExecuteApiAsync(string apiName, string[] apiParams)
        {
            try
            {
                switch (apiName)
                {
                    case "取群群":
                        return await ExecuteGetGroupsAsync(apiParams);

                    case "插件_获取所有账号":
                        return await ExecuteGetAccountsAsync();

                    case "发送群消息(文本版)":
                        return await ExecuteSendGroupMessageAsync(apiParams);

                    case "发送好友消息":
                        return await ExecuteSendPrivateMessageAsync(apiParams);

                    case "ww_群禁言解禁":
                        return await ExecuteGroupMuteAsync(apiParams);

                    case "ww_获取群成员":
                        return await ExecuteGetGroupMembersAsync(apiParams);

                    case "ww_ID资料":
                        return await ExecuteGetUserInfoAsync(apiParams);

                    default:
                        Log($"未知API: {apiName}");
                        return ZCGProtocol.EncryptApiResult("未知API");
                }
            }
            catch (Exception ex)
            {
                Log($"执行API异常 ({apiName}): {ex.Message}");
                return ZCGProtocol.EncryptApiResult($"错误: {ex.Message}");
            }
        }

        private async Task<string> ExecuteGetGroupsAsync(string[] apiParams)
        {
            if (_cdpBridge == null || !IsCDPConnected)
                return ZCGProtocol.EncryptApiResult("CDP未连接");

            var groups = await _cdpBridge.GetGroupListAsync();
            var sb = new StringBuilder();
            if (groups != null)
            {
                foreach (var g in groups)
                {
                    sb.AppendLine($"{g.groupId}|{g.groupName}");
                }
            }
            return ZCGProtocol.EncryptApiResult(sb.ToString());
        }

        private async Task<string> ExecuteGetAccountsAsync()
        {
            if (_cdpBridge == null || !IsCDPConnected)
                return ZCGProtocol.EncryptApiResult("CDP未连接");

            var userInfo = await _cdpBridge.GetCurrentUserInfoAsync();
            if (userInfo != null && string.IsNullOrEmpty(userInfo.error))
            {
                return ZCGProtocol.EncryptApiResult($"{userInfo.wwid}|{userInfo.nickname}");
            }
            return ZCGProtocol.EncryptApiResult("无账号");
        }

        private async Task<string> ExecuteSendGroupMessageAsync(string[] apiParams)
        {
            // 格式: 账号|内容|群号|类型|标志
            if (apiParams.Length < 3)
                return ZCGProtocol.EncryptApiResult("参数不足");

            var content = apiParams[1];
            var groupId = apiParams[2];

            if (_cdpBridge == null || !IsCDPConnected)
                return ZCGProtocol.EncryptApiResult("CDP未连接");

            var success = await _cdpBridge.SendGroupMessageAsync(groupId, content);
            return ZCGProtocol.EncryptApiResult(success ? "发送成功" : "发送失败");
        }

        private async Task<string> ExecuteSendPrivateMessageAsync(string[] apiParams)
        {
            // 格式: 账号|内容|好友ID
            if (apiParams.Length < 3)
                return ZCGProtocol.EncryptApiResult("参数不足");

            var content = apiParams[1];
            var friendId = apiParams[2];

            if (_cdpBridge == null || !IsCDPConnected)
                return ZCGProtocol.EncryptApiResult("CDP未连接");

            var success = await _cdpBridge.SendPrivateMessageAsync(friendId, content);
            return ZCGProtocol.EncryptApiResult(success ? "发送成功" : "发送失败");
        }

        private async Task<string> ExecuteGroupMuteAsync(string[] apiParams)
        {
            // 格式: 账号|群号|模式 (1=禁言,2=解禁) 或 账号|群号|模式|成员ID (成员禁言)
            if (apiParams.Length < 3)
                return ZCGProtocol.EncryptApiResult("参数不足");

            var groupId = apiParams[1];
            var mode = apiParams[2];
            var mute = mode == "1";
            var memberId = apiParams.Length > 3 ? apiParams[3] : null;

            if (_cdpBridge == null || !IsCDPConnected)
                return ZCGProtocol.EncryptApiResult("CDP未连接");

            bool success = false;
            
            // 方案1: 优先使用API代理方式（更可靠）
            try
            {
                if (!string.IsNullOrEmpty(memberId))
                {
                    // 成员禁言
                    Log($"[禁言] 通过API代理禁言成员: 群={groupId}, 成员={memberId}");
                    var apiResult = await _cdpBridge.SetMemberMuteViaApiAsync(groupId, memberId, mute ? 0 : -1);
                    if (apiResult.Success)
                    {
                        success = true;
                        Log($"[禁言] API代理成功");
                    }
                    else
                    {
                        Log($"[禁言] API代理失败: {apiResult.Message}, 尝试NIM SDK方式");
                    }
                }
                else
                {
                    // 全体禁言
                    Log($"[禁言] 通过API代理设置群禁言: 群={groupId}, 禁言={mute}");
                    var apiResult = await _cdpBridge.SetGroupMuteViaApiAsync(groupId, mute);
                    if (apiResult.Success)
                    {
                        success = true;
                        Log($"[禁言] API代理成功");
                    }
                    else
                    {
                        Log($"[禁言] API代理失败: {apiResult.Message}, 尝试NIM SDK方式");
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"[禁言] API代理异常: {ex.Message}");
            }
            
            // 方案2: 备用 - 使用NIM SDK方式
            if (!success)
            {
                try
                {
                    if (!string.IsNullOrEmpty(memberId))
                    {
                        // 成员禁言
                        success = mute 
                            ? await _cdpBridge.MuteMemberAsync(groupId, memberId, 0)
                            : await _cdpBridge.UnmuteMemberAsync(groupId, memberId);
                    }
                    else
                    {
                        // 全体禁言
                        success = await _cdpBridge.SetGroupMuteAsync(groupId, mute);
                    }
                    
                    if (success)
                        Log($"[禁言] NIM SDK方式成功");
                }
                catch (Exception ex)
                {
                    Log($"[禁言] NIM SDK方式失败: {ex.Message}");
                }
            }
            
            return ZCGProtocol.EncryptApiResult(success ? "操作成功" : "操作失败");
        }

        private async Task<string> ExecuteGetGroupMembersAsync(string[] apiParams)
        {
            // 格式: 账号|群号
            if (apiParams.Length < 2)
                return ZCGProtocol.EncryptApiResult("参数不足");

            var groupId = apiParams[1];

            if (_cdpBridge == null || !IsCDPConnected)
                return ZCGProtocol.EncryptApiResult("CDP未连接");

            var members = await _cdpBridge.GetGroupMembersAsync(groupId);
            var sb = new StringBuilder();
            if (members != null)
            {
                foreach (var m in members)
                {
                    sb.AppendLine($"{m.memberId}|{m.nickname}|{m.card}");
                }
            }
            return ZCGProtocol.EncryptApiResult(sb.ToString());
        }

        private async Task<string> ExecuteGetUserInfoAsync(string[] apiParams)
        {
            // 格式: 账号|用户ID
            if (apiParams.Length < 2)
                return ZCGProtocol.EncryptApiResult("参数不足");

            var userId = apiParams[1];

            if (_cdpBridge == null || !IsCDPConnected)
                return ZCGProtocol.EncryptApiResult("CDP未连接");

            var userInfo = await _cdpBridge.GetUserInfoAsync(userId);
            if (userInfo != null)
            {
                return ZCGProtocol.EncryptApiResult($"{userInfo.wwid}|{userInfo.nickname}|{userInfo.avatar}");
            }
            return ZCGProtocol.EncryptApiResult("用户不存在");
        }

        #endregion

    }
}
