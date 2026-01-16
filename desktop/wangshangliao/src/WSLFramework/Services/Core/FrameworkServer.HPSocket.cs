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
        #region HPSocket 事件处理
        
        private HandleResult OnPrepareListen(IServer sender, IntPtr listen)
        {
            Log("服务端准备监听...");
            return HandleResult.Ok;
        }
        
        private HandleResult OnAccept(IServer sender, IntPtr connId, IntPtr client)
        {
            try
        {
            string ip = "";
            ushort port = 0;
            sender.GetRemoteAddress(connId, out ip, out port);
            
            var clientInfo = new ClientInfo
            {
                ConnId = connId,
                Address = ip,
                Port = port,
                ConnectTime = DateTime.Now
            };
            
            _clients[connId] = clientInfo;
            
            Log($"客户端连接: {ip}:{port} (ConnID={connId})");
            OnClientConnectionChanged?.Invoke(connId, true);
            }
            catch (Exception ex)
            {
                Log($"处理客户端连接异常: {ex.Message}");
            }
            
            return HandleResult.Ok;
        }
        
        private HandleResult OnReceive(IServer sender, IntPtr connId, byte[] data)
        {
            try
            {
                // PACK 模式: data 已经是完整的数据包
                ProcessReceivedDataAsync(connId, data).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log($"接收数据异常 (ConnID={connId}): {ex.Message}");
            }
            
            return HandleResult.Ok;
        }
        
        private HandleResult OnClose(IServer sender, IntPtr connId, SocketOperation operation, int errorCode)
        {
            try
        {
            if (_clients.TryRemove(connId, out var clientInfo))
            {
                Log($"客户端断开: {clientInfo.Address}:{clientInfo.Port} (ConnID={connId}, 操作={operation}, 错误码={errorCode})");
            }
            else
            {
                Log($"客户端断开: ConnID={connId}, 操作={operation}, 错误码={errorCode}");
            }
            
            OnClientConnectionChanged?.Invoke(connId, false);
            }
            catch (Exception ex)
            {
                Log($"处理客户端断开异常: {ex.Message}");
            }

            return HandleResult.Ok;
        }
        
        private HandleResult OnShutdown(IServer sender)
        {
            Log("服务端已关闭");
            _clients.Clear();
            return HandleResult.Ok;
        }
        
        #endregion

    }
}
