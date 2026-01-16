using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WangShangLiaoBot.Services.QXFramework
{
    /// <summary>
    /// QX框架服务封装 - 高级API接口
    /// 提供类似招财狗的通信能力
    /// </summary>
    public sealed class QXService
    {
        private static QXService _instance;
        public static QXService Instance => _instance ?? (_instance = new QXService());

        private string _robotQQ;
        private bool _isInitialized;
        private readonly object _lock = new object();

        // 事件（预留接口，将来实现消息回调时使用）
        public event Action<string> OnLog;
        #pragma warning disable CS0067 // 事件预留接口
        public event Action<string, string, string> OnGroupMessage;   // groupId, senderId, message
        public event Action<string, string> OnPrivateMessage;         // senderId, message
        public event Action<string, string> OnMemberJoined;           // groupId, userId
        public event Action<string, string, bool> OnMemberLeft;       // groupId, userId, isKicked
        public event Action<string, string, string> OnCardChanged;    // groupId, userId, newCard
        #pragma warning restore CS0067

        private QXService()
        {
        }

        #region 初始化

        /// <summary>
        /// 初始化QX服务
        /// </summary>
        public bool Initialize(string robotQQ)
        {
            lock (_lock)
            {
                try
                {
                    _robotQQ = robotQQ;

                    // 验证Module.dll是否可用
                    var appDir = QXNativeMethods.PtrToString(QXNativeMethods.QX_Get_Directory_App());
                    if (string.IsNullOrEmpty(appDir))
                    {
                        Log("[QX] Module.dll未加载或不可用");
                        return false;
                    }

                    _isInitialized = true;
                    Log($"[QX] 初始化成功，机器人QQ: {robotQQ}");
                    return true;
                }
                catch (Exception ex)
                {
                    Log($"[QX] 初始化失败: {ex.Message}");
                    return false;
                }
            }
        }

        /// <summary>
        /// 检查是否已初始化
        /// </summary>
        public bool IsInitialized => _isInitialized;

        #endregion

        #region 群消息

        /// <summary>
        /// 发送群消息
        /// </summary>
        public async Task<bool> SendGroupMessageAsync(string groupId, string message)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (!_isInitialized) return false;

                    var result = QXNativeMethods.QX_Group_SendMsg(_robotQQ, groupId, message);
                    if (result == 0)
                    {
                        Log($"[QX] 发送群消息成功: {groupId}");
                        return true;
                    }
                    else
                    {
                        Log($"[QX] 发送群消息失败，错误码: {result}");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    Log($"[QX] 发送群消息异常: {ex.Message}");
                    return false;
                }
            });
        }

        /// <summary>
        /// 发送群图片
        /// </summary>
        public async Task<bool> SendGroupImageAsync(string groupId, string imagePath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (!_isInitialized) return false;

                    var result = QXNativeMethods.QX_Group_SendMsgPhoto(_robotQQ, groupId, imagePath);
                    return result == 0;
                }
                catch (Exception ex)
                {
                    Log($"[QX] 发送群图片异常: {ex.Message}");
                    return false;
                }
            });
        }

        /// <summary>
        /// 撤回群消息
        /// </summary>
        public async Task<bool> WithdrawGroupMessageAsync(string groupId, string messageId)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (!_isInitialized) return false;

                    var result = QXNativeMethods.QX_Group_WithdrawMessage(_robotQQ, groupId, messageId);
                    return result == 0;
                }
                catch (Exception ex)
                {
                    Log($"[QX] 撤回消息异常: {ex.Message}");
                    return false;
                }
            });
        }

        #endregion

        #region 私聊消息

        /// <summary>
        /// 发送私聊消息
        /// </summary>
        public async Task<bool> SendPrivateMessageAsync(string userId, string message)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (!_isInitialized) return false;

                    var result = QXNativeMethods.QX_Friend_SendMsg(_robotQQ, userId, message);
                    if (result == 0)
                    {
                        Log($"[QX] 发送私聊消息成功: {userId}");
                        return true;
                    }
                    else
                    {
                        Log($"[QX] 发送私聊消息失败，错误码: {result}");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    Log($"[QX] 发送私聊消息异常: {ex.Message}");
                    return false;
                }
            });
        }

        /// <summary>
        /// 发送私聊图片
        /// </summary>
        public async Task<bool> SendPrivateImageAsync(string userId, string imagePath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (!_isInitialized) return false;

                    var result = QXNativeMethods.QX_Friend_SendMsgPhoto(_robotQQ, userId, imagePath);
                    return result == 0;
                }
                catch (Exception ex)
                {
                    Log($"[QX] 发送私聊图片异常: {ex.Message}");
                    return false;
                }
            });
        }

        #endregion

        #region 群管理

        /// <summary>
        /// 禁言群成员
        /// </summary>
        public async Task<bool> MuteMemberAsync(string groupId, string userId, int minutes)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (!_isInitialized) return false;

                    var result = QXNativeMethods.QX_Group_UserSayState(_robotQQ, groupId, userId, minutes);
                    if (result == 0)
                    {
                        Log($"[QX] 禁言成功: {userId} {minutes}分钟");
                        return true;
                    }
                    return false;
                }
                catch (Exception ex)
                {
                    Log($"[QX] 禁言异常: {ex.Message}");
                    return false;
                }
            });
        }

        /// <summary>
        /// 解除禁言
        /// </summary>
        public Task<bool> UnmuteMemberAsync(string groupId, string userId)
        {
            return MuteMemberAsync(groupId, userId, 0);
        }

        /// <summary>
        /// 全群禁言
        /// </summary>
        public async Task<bool> MuteGroupAsync(string groupId, bool enable)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (!_isInitialized) return false;

                    var result = QXNativeMethods.QX_Group_SayState(_robotQQ, groupId, enable ? 1 : 0);
                    return result == 0;
                }
                catch (Exception ex)
                {
                    Log($"[QX] 全群禁言异常: {ex.Message}");
                    return false;
                }
            });
        }

        /// <summary>
        /// 踢出群成员
        /// </summary>
        public async Task<bool> KickMemberAsync(string groupId, string userId)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (!_isInitialized) return false;

                    var result = QXNativeMethods.QX_Group_DelteUser(_robotQQ, groupId, userId);
                    if (result == 0)
                    {
                        Log($"[QX] 踢出成功: {userId}");
                        return true;
                    }
                    return false;
                }
                catch (Exception ex)
                {
                    Log($"[QX] 踢出异常: {ex.Message}");
                    return false;
                }
            });
        }

        /// <summary>
        /// 设置群名片
        /// </summary>
        public async Task<bool> SetMemberCardAsync(string groupId, string userId, string cardName)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (!_isInitialized) return false;

                    var result = QXNativeMethods.QX_Group_UserSetCardName(_robotQQ, groupId, userId, cardName);
                    return result == 0;
                }
                catch (Exception ex)
                {
                    Log($"[QX] 设置名片异常: {ex.Message}");
                    return false;
                }
            });
        }

        /// <summary>
        /// 获取群成员列表
        /// </summary>
        public async Task<string> GetGroupMembersAsync(string groupId)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (!_isInitialized) return null;

                    var ptr = QXNativeMethods.QX_Group_GetUserList(_robotQQ, groupId);
                    return QXNativeMethods.PtrToString(ptr);
                }
                catch (Exception ex)
                {
                    Log($"[QX] 获取群成员异常: {ex.Message}");
                    return null;
                }
            });
        }

        /// <summary>
        /// 获取群列表
        /// </summary>
        public async Task<string> GetGroupListAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (!_isInitialized) return null;

                    var ptr = QXNativeMethods.QX_Group_Getlist(_robotQQ);
                    return QXNativeMethods.PtrToString(ptr);
                }
                catch (Exception ex)
                {
                    Log($"[QX] 获取群列表异常: {ex.Message}");
                    return null;
                }
            });
        }

        #endregion

        #region 好友管理

        /// <summary>
        /// 处理好友申请
        /// </summary>
        public async Task<bool> HandleFriendRequestAsync(string requestId, bool accept)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (!_isInitialized) return false;

                    var result = QXNativeMethods.QX_Friend_SetApply(_robotQQ, requestId, accept ? 1 : 0);
                    return result == 0;
                }
                catch (Exception ex)
                {
                    Log($"[QX] 处理好友申请异常: {ex.Message}");
                    return false;
                }
            });
        }

        /// <summary>
        /// 处理入群申请
        /// </summary>
        public async Task<bool> HandleJoinRequestAsync(string requestId, bool accept)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (!_isInitialized) return false;

                    var result = QXNativeMethods.QX_Group_UserSetApply(_robotQQ, requestId, accept ? 1 : 0);
                    return result == 0;
                }
                catch (Exception ex)
                {
                    Log($"[QX] 处理入群申请异常: {ex.Message}");
                    return false;
                }
            });
        }

        /// <summary>
        /// 获取好友列表
        /// </summary>
        public async Task<string> GetFriendListAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (!_isInitialized) return null;

                    var ptr = QXNativeMethods.QX_Friend_GetList(_robotQQ);
                    return QXNativeMethods.PtrToString(ptr);
                }
                catch (Exception ex)
                {
                    Log($"[QX] 获取好友列表异常: {ex.Message}");
                    return null;
                }
            });
        }

        #endregion

        #region 转账/红包

        /// <summary>
        /// 发起转账
        /// </summary>
        public async Task<bool> SendTransferAsync(string userId, int amount, string message)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (!_isInitialized) return false;

                    var result = QXNativeMethods.QX_Transfer_Send(_robotQQ, userId, amount, message);
                    return result == 0;
                }
                catch (Exception ex)
                {
                    Log($"[QX] 发起转账异常: {ex.Message}");
                    return false;
                }
            });
        }

        /// <summary>
        /// 领取转账
        /// </summary>
        public async Task<bool> GrabTransferAsync(string transferId)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (!_isInitialized) return false;

                    var result = QXNativeMethods.QX_Transfer_Grab(_robotQQ, transferId);
                    return result == 0;
                }
                catch (Exception ex)
                {
                    Log($"[QX] 领取转账异常: {ex.Message}");
                    return false;
                }
            });
        }

        /// <summary>
        /// 抢红包
        /// </summary>
        public async Task<bool> GrabRedEnvelopeAsync(string redEnvelopeId)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (!_isInitialized) return false;

                    var result = QXNativeMethods.QX_RobRedEnvelope(_robotQQ, redEnvelopeId);
                    return result == 0;
                }
                catch (Exception ex)
                {
                    Log($"[QX] 抢红包异常: {ex.Message}");
                    return false;
                }
            });
        }

        #endregion

        #region 系统信息

        /// <summary>
        /// 获取账号信息
        /// </summary>
        public string GetAccountInfo()
        {
            try
            {
                if (!_isInitialized) return null;

                var ptr = QXNativeMethods.QX_GetInfo(_robotQQ);
                return QXNativeMethods.PtrToString(ptr);
            }
            catch (Exception ex)
            {
                Log($"[QX] 获取账号信息异常: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 获取NIM在线ID
        /// </summary>
        public string GetNimIdOnline()
        {
            try
            {
                if (!_isInitialized) return null;

                var ptr = QXNativeMethods.QX_Get_nimIdOnline(_robotQQ);
                return QXNativeMethods.PtrToString(ptr);
            }
            catch (Exception ex)
            {
                Log($"[QX] 获取NIM ID异常: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 获取签名Token
        /// </summary>
        public string GetSigToken()
        {
            try
            {
                if (!_isInitialized) return null;

                var ptr = QXNativeMethods.QX_Get_sig_token(_robotQQ);
                return QXNativeMethods.PtrToString(ptr);
            }
            catch (Exception ex)
            {
                Log($"[QX] 获取Token异常: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 上传图片
        /// </summary>
        public async Task<string> UploadImageAsync(string imagePath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (!_isInitialized) return null;

                    var ptr = QXNativeMethods.QX_Upload_photo(_robotQQ, imagePath);
                    return QXNativeMethods.PtrToString(ptr);
                }
                catch (Exception ex)
                {
                    Log($"[QX] 上传图片异常: {ex.Message}");
                    return null;
                }
            });
        }

        #endregion

        private void Log(string message)
        {
            OnLog?.Invoke(message);
            Logger.Info(message);
        }
    }
}
