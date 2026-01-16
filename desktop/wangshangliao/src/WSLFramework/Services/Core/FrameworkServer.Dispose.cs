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
        #region IDisposable
        
        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            try
            {
                _cts?.Cancel();
            StopAsync().Wait(5000);
            }
            catch { }

            _server?.Dispose();
            _cdpBridge?.Dispose();
            _apiHandler?.Dispose();
            _cdpLock?.Dispose();
            _cts?.Dispose();
        }

        #endregion

    }
}
