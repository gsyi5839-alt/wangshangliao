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
        #region 配置同步处理

        /// <summary>
        /// 处理全量配置同步
        /// </summary>
        private FrameworkMessage HandleSyncFullConfig(FrameworkMessage message)
        {
            try
            {
                var configJson = message.Content;
                Log($"[ConfigSync] 收到全量配置同步 ({configJson?.Length ?? 0} 字节)");
                
                var success = BotConfigSyncService.Instance.SyncFromJson(configJson);
                
                if (success)
                {
                    Log("[ConfigSync] 全量配置同步成功");
                    return FrameworkMessage.CreateSuccess(message.Id, FrameworkMessageType.SyncConfigResponse, "配置同步成功");
                }
                else
                {
                    return FrameworkMessage.CreateError(message.Id, "配置同步失败");
                }
            }
            catch (Exception ex)
            {
                Log($"[ConfigSync] 全量配置同步异常: {ex.Message}");
                return FrameworkMessage.CreateError(message.Id, ex.Message);
            }
        }

        /// <summary>
        /// 处理赔率配置同步
        /// </summary>
        private FrameworkMessage HandleSyncOddsConfig(FrameworkMessage message)
        {
            try
            {
                var oddsJson = message.Content;
                Log("[ConfigSync] 收到赔率配置同步");
                
                var success = BotConfigSyncService.Instance.SyncOddsConfig(oddsJson);
                
                return success
                    ? FrameworkMessage.CreateSuccess(message.Id, FrameworkMessageType.SyncConfigResponse, "赔率配置同步成功")
                    : FrameworkMessage.CreateError(message.Id, "赔率配置同步失败");
            }
            catch (Exception ex)
            {
                Log($"[ConfigSync] 赔率配置同步异常: {ex.Message}");
                return FrameworkMessage.CreateError(message.Id, ex.Message);
            }
        }

        /// <summary>
        /// 处理封盘配置同步
        /// </summary>
        private FrameworkMessage HandleSyncSealingConfig(FrameworkMessage message)
        {
            try
            {
                var sealingJson = message.Content;
                Log("[ConfigSync] 收到封盘配置同步");
                
                var success = BotConfigSyncService.Instance.SyncSealingConfig(sealingJson);
                
                return success
                    ? FrameworkMessage.CreateSuccess(message.Id, FrameworkMessageType.SyncConfigResponse, "封盘配置同步成功")
                    : FrameworkMessage.CreateError(message.Id, "封盘配置同步失败");
            }
            catch (Exception ex)
            {
                Log($"[ConfigSync] 封盘配置同步异常: {ex.Message}");
                return FrameworkMessage.CreateError(message.Id, ex.Message);
            }
        }

        /// <summary>
        /// 处理托管配置同步
        /// </summary>
        private FrameworkMessage HandleSyncTrusteeConfig(FrameworkMessage message)
        {
            try
            {
                var trusteeJson = message.Content;
                Log("[ConfigSync] 收到托管配置同步");
                
                var success = BotConfigSyncService.Instance.SyncTrusteeConfig(trusteeJson);
                
                return success
                    ? FrameworkMessage.CreateSuccess(message.Id, FrameworkMessageType.SyncConfigResponse, "托管配置同步成功")
                    : FrameworkMessage.CreateError(message.Id, "托管配置同步失败");
            }
            catch (Exception ex)
            {
                Log($"[ConfigSync] 托管配置同步异常: {ex.Message}");
                return FrameworkMessage.CreateError(message.Id, ex.Message);
            }
        }

        /// <summary>
        /// 处理自动回复配置同步
        /// </summary>
        private FrameworkMessage HandleSyncAutoReplyConfig(FrameworkMessage message)
        {
            try
            {
                var rulesJson = message.Content;
                var enabled = message.Success;
                Log($"[ConfigSync] 收到自动回复配置同步 (启用={enabled}, JSON长度={rulesJson?.Length ?? 0})");
                
                // 存储原始JSON，由BotConfigSyncService处理
                var success = BotConfigSyncService.Instance.SyncAutoReplyConfigJson(enabled, rulesJson);
                
                return success
                    ? FrameworkMessage.CreateSuccess(message.Id, FrameworkMessageType.SyncConfigResponse, "自动回复配置同步成功")
                    : FrameworkMessage.CreateError(message.Id, "自动回复配置同步失败");
            }
            catch (Exception ex)
            {
                Log($"[ConfigSync] 自动回复配置同步异常: {ex.Message}");
                return FrameworkMessage.CreateError(message.Id, ex.Message);
            }
        }

        /// <summary>
        /// 处理话术模板配置同步
        /// </summary>
        private FrameworkMessage HandleSyncTemplateConfig(FrameworkMessage message)
        {
            try
            {
                var templateJson = message.Content;
                Log($"[ConfigSync] 收到话术模板配置同步 (JSON长度={templateJson?.Length ?? 0})");
                
                // 存储原始JSON，由BotConfigSyncService处理
                var success = BotConfigSyncService.Instance.SyncTemplateConfigJson(templateJson);
                
                return success
                    ? FrameworkMessage.CreateSuccess(message.Id, FrameworkMessageType.SyncConfigResponse, "话术模板配置同步成功")
                    : FrameworkMessage.CreateError(message.Id, "话术模板配置同步失败");
            }
            catch (Exception ex)
            {
                Log($"[ConfigSync] 话术模板配置同步异常: {ex.Message}");
                return FrameworkMessage.CreateError(message.Id, ex.Message);
            }
        }

        /// <summary>
        /// 处理基本设置同步
        /// </summary>
        private FrameworkMessage HandleSyncBasicConfig(FrameworkMessage message)
        {
            try
            {
                var content = message.Content;
                var parts = content?.Split('|') ?? new string[0];
                
                if (parts.Length >= 4)
                {
                    var groupId = parts[0];
                    var adminId = parts[1];
                    var myWwid = parts[2];
                    int.TryParse(parts[3], out int debugPort);
                    
                    Log($"[ConfigSync] 收到基本配置同步 (群号={groupId}, 管理员={adminId})");
                    
                    var success = BotConfigSyncService.Instance.SyncBasicConfig(groupId, adminId, myWwid, debugPort);
                    
                    // 更新活跃群
                    if (!string.IsNullOrEmpty(groupId))
                    {
                        _activeGroupId = groupId;
                        TimedMessageService.Instance.AddActiveGroup(groupId);
                    }
                    
                    return success
                        ? FrameworkMessage.CreateSuccess(message.Id, FrameworkMessageType.SyncConfigResponse, "基本配置同步成功")
                        : FrameworkMessage.CreateError(message.Id, "基本配置同步失败");
                }
                
                return FrameworkMessage.CreateError(message.Id, "基本配置参数不足");
            }
            catch (Exception ex)
            {
                Log($"[ConfigSync] 基本配置同步异常: {ex.Message}");
                return FrameworkMessage.CreateError(message.Id, ex.Message);
            }
        }

        #endregion

    }
}
