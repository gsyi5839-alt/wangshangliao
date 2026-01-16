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
        #region 辅助方法
        
        /// <summary>
        /// 获取已连接客户端数量
        /// </summary>
        public int GetClientCount() => _clients.Count;
        
        /// <summary>
        /// 获取所有客户端连接ID
        /// </summary>
        public IntPtr[] GetAllConnIds()
        {
            var ids = new IntPtr[_clients.Count];
            _clients.Keys.CopyTo(ids, 0);
            return ids;
        }
        
        /// <summary>
        /// 获取客户端信息
        /// </summary>
        public ClientInfo GetClientInfo(IntPtr connId)
        {
            _clients.TryGetValue(connId, out var info);
            return info;
        }
        
        /// <summary>
        /// 断开指定客户端
        /// </summary>
        public bool DisconnectClient(IntPtr connId)
        {
            return _server?.Disconnect(connId) ?? false;
        }
        
        private void Log(string message)
        {
            Logger.Info(message);
            OnLog?.Invoke(message);
        }
        
        /// <summary>
        /// 简单JSON值提取
        /// </summary>
        private string ExtractJsonValue(string json, string key)
        {
            try
            {
                // 查找 "key":"value" 或 "key":value
                var pattern = $"\"{key}\":\"?([^\",}}]+)\"?";
                var match = System.Text.RegularExpressions.Regex.Match(json, pattern);
                if (match.Success && match.Groups.Count > 1)
                {
                    return match.Groups[1].Value.Trim('"');
                }
            }
            catch { }
            return null;
        }
        
        #endregion

    }
}
