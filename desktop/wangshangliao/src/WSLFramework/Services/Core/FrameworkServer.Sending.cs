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
        #region 发送消息
        
        /// <summary>
        /// 发送消息给客户端
        /// </summary>
        public bool SendToClient(IntPtr connId, FrameworkMessage message)
        {
            if (_isDisposed) return false;

            try
            {
                if (_server?.HasStarted != true)
                {
                    Log("服务端未启动，无法发送消息");
                    return false;
                }
                
                string json = message.ToJson();
                byte[] data = Encoding.UTF8.GetBytes(json);
                
                bool sent = _server.Send(connId, data, data.Length);
                
                if (sent)
                {
                    Log($"发送消息到 ConnID={connId}: {json.Substring(0, Math.Min(100, json.Length))}...");
                }
                else
                {
                    Log($"发送消息失败 ConnID={connId}");
                }
                
                return sent;
            }
            catch (Exception ex)
            {
                Log($"发送异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 发送原始字符串
        /// </summary>
        public bool SendRawString(IntPtr connId, string data)
        {
            if (_isDisposed || _server?.HasStarted != true) return false;

            try
            {
                byte[] bytes = Encoding.UTF8.GetBytes(data);
                return _server.Send(connId, bytes, bytes.Length);
            }
            catch (Exception ex)
            {
                Log($"发送原始数据异常: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 广播消息给所有客户端
        /// </summary>
        public void Broadcast(FrameworkMessage message)
        {
            foreach (var connId in _clients.Keys)
            {
                SendToClient(connId, message);
            }
        }

        /// <summary>
        /// 广播消息给除指定客户端外的所有客户端
        /// </summary>
        public void BroadcastExcept(IntPtr exceptConnId, FrameworkMessage message)
        {
            foreach (var connId in _clients.Keys)
            {
                if (connId != exceptConnId)
                {
                    SendToClient(connId, message);
                }
            }
        }

        /// <summary>
        /// 广播原始字符串
        /// </summary>
        public void BroadcastRawString(string data)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(data);
            foreach (var connId in _clients.Keys)
            {
                try
                {
                    _server?.Send(connId, bytes, bytes.Length);
                }
                catch { }
            }
        }
        
        /// <summary>
        /// 广播 NIM 直连消息给所有主框架客户端
        /// </summary>
        public void BroadcastNimMessage(NimDirectMessage msg)
        {
            if (msg == null) return;
            
            var frameworkMsg = new FrameworkMessage
            {
                Type = FrameworkMessageType.ReceiveGroupMessage,
                GroupId = msg.To,
                SenderId = msg.From,
                Content = msg.Body,
                Timestamp = msg.Time  // 已经是毫秒时间戳
            };
            
            Log($"[NIM广播] 群:{msg.To} 发送者:{msg.From} 内容:{msg.Body?.Substring(0, Math.Min(50, msg.Body?.Length ?? 0))}...");
            Broadcast(frameworkMsg);
        }
        
        /// <summary>
        /// 发送原始字节数据
        /// </summary>
        public bool SendRaw(IntPtr connId, byte[] data)
        {
            if (_isDisposed || _server?.HasStarted != true) return false;
            return _server.Send(connId, data, data.Length);
        }
        
        #endregion

    }
}
