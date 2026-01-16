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
        #region CDP消息处理

        /// <summary>
        /// 处理CDP消息
        /// </summary>
        private void HandleCDPMessage(string nimJson)
        {
            try
            {
                // 解析NIM消息
                var queue = ZCGMessageQueue.FromNIMMessage(nimJson, CurrentLoginAccount);
                if (queue != null)
                {
                    OnMessageQueueReceived?.Invoke(queue);

                    // 广播给所有客户端
                    var message = FrameworkMessage.CreateMessageQueue(queue);
                    Broadcast(message);

                    // 同时发送ZCG格式
                    BroadcastRawString(queue.ToZCGFormat());
                }
            }
            catch (Exception ex)
            {
                Log($"处理CDP消息异常: {ex.Message}");
            }
        }

        #endregion

    }
}
