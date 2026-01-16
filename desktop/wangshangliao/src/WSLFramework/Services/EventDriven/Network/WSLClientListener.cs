using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace WSLFramework.Services.EventDriven.Network
{
    /// <summary>
    /// 客户端监听器接口 - 参考 Lagrange.Core 的 IClientListener
    /// 针对 .NET Framework 4.7.2 兼容修改
    /// </summary>
    public interface IWSLClientListener
    {
        /// <summary>包头大小</summary>
        uint HeaderSize { get; }

        /// <summary>解析包长度</summary>
        uint GetPacketLength(byte[] header, int offset, int length);

        /// <summary>收到数据包回调</summary>
        void OnRecvPacket(byte[] packet, int offset, int length);

        /// <summary>断开连接回调</summary>
        void OnDisconnect();

        /// <summary>Socket错误回调</summary>
        void OnSocketError(Exception e, byte[] data = null);
    }

    /// <summary>
    /// TCP客户端监听器 - 参考 Lagrange.Core 的 ClientListener
    /// 纯Socket长连接，针对 .NET Framework 4.7.2 兼容
    /// </summary>
    public abstract class WSLClientListener : IWSLClientListener
    {
        #region Socket会话

        /// <summary>
        /// Socket会话 - 参考 Lagrange.Core.SocketSession
        /// </summary>
        protected sealed class SocketSession : IDisposable
        {
            public Socket Socket { get; }
            private CancellationTokenSource _cts;
            public CancellationToken Token { get; }

            public SocketSession()
            {
                Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                Socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);
                _cts = new CancellationTokenSource();
                Token = _cts.Token;
            }

            public void Dispose()
            {
                var cts = Interlocked.Exchange(ref _cts, null);
                if (cts == null) return;

                cts.Cancel();
                cts.Dispose();
                try { Socket.Shutdown(SocketShutdown.Both); } catch { }
                Socket.Close();
                Socket.Dispose();
            }
        }

        #endregion

        #region 属性

        /// <summary>是否已连接</summary>
        public bool Connected
        {
            get
            {
                try
                {
                    var session = _session;
                    return session?.Socket?.Connected ?? false;
                }
                catch (ObjectDisposedException)
                {
                    return false;
                }
            }
        }

        /// <summary>包头大小 (子类实现)</summary>
        public abstract uint HeaderSize { get; }

        protected SocketSession _session;
        private readonly object _sessionLock = new object();

        #endregion

        #region 连接方法

        /// <summary>
        /// 连接到服务器
        /// </summary>
        public Task<bool> ConnectAsync(string host, int port)
        {
            lock (_sessionLock)
            {
                if (_session != null)
                {
                    return Task.FromResult(false);
                }
                _session = new SocketSession();
            }

            return InternalConnectAsync(_session, host, port);
        }

        private async Task<bool> InternalConnectAsync(SocketSession session, string host, int port)
        {
            try
            {
                await Task.Factory.FromAsync(
                    session.Socket.BeginConnect(host, port, null, null),
                    session.Socket.EndConnect);
                
                // 启动接收循环
                _ = ReceiveLoopAsync(session);
                
                return true;
            }
            catch (Exception e)
            {
                RemoveSession(session);
                OnSocketError(e);
                return false;
            }
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        public void Disconnect()
        {
            lock (_sessionLock)
            {
                if (_session != null)
                {
                    RemoveSession(_session);
                }
            }
        }

        private void RemoveSession(SocketSession session)
        {
            lock (_sessionLock)
            {
                if (_session == session)
                {
                    _session = null;
                    session.Dispose();
                    OnDisconnect();
                }
            }
        }

        #endregion

        #region 发送方法

        /// <summary>
        /// 发送数据
        /// </summary>
        public async Task<bool> SendAsync(byte[] buffer, int timeout = -1)
        {
            try
            {
                var session = _session;
                if (session == null) return false;

                var tcs = new TaskCompletionSource<int>();
                CancellationTokenSource timeoutCts = null;

                if (timeout > 0)
                {
                    timeoutCts = new CancellationTokenSource(timeout);
                    timeoutCts.Token.Register(() => tcs.TrySetCanceled());
                }

                try
                {
                    session.Socket.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, ar =>
                    {
                        try
                        {
                            int sentBytes = session.Socket.EndSend(ar);
                            tcs.TrySetResult(sentBytes);
                        }
                        catch (Exception ex)
                        {
                            tcs.TrySetException(ex);
                        }
                    }, null);

                    int sentResult = await tcs.Task;
                    return sentResult == buffer.Length;
                }
                finally
                {
                    timeoutCts?.Dispose();
                }
            }
            catch (Exception e)
            {
                OnSocketError(e, buffer);
                return false;
            }
        }

        #endregion

        #region 接收循环

        /// <summary>
        /// 接收循环 - 核心方法
        /// </summary>
        private async Task ReceiveLoopAsync(SocketSession session)
        {
            try
            {
                Socket socket = session.Socket;
                int headerSize = (int)HeaderSize;
                byte[] buffer = new byte[Math.Max(headerSize, 4096)];
                CancellationToken token = session.Token;

                while (!token.IsCancellationRequested && socket.Connected)
                {
                    // 1. 读取包头
                    await ReceiveFullyAsync(socket, buffer, 0, headerSize, token);

                    // 2. 解析包长度
                    int packetLength = (int)GetPacketLength(buffer, 0, headerSize);
                    
                    if (packetLength <= 0 || packetLength > 64 * 1024 * 1024) // 最大64MB
                    {
                        throw new InvalidOperationException($"无效的包长度: {packetLength}");
                    }

                    // 3. 扩展缓冲区
                    if (packetLength > buffer.Length)
                    {
                        byte[] newBuffer = new byte[packetLength];
                        Buffer.BlockCopy(buffer, 0, newBuffer, 0, headerSize);
                        buffer = newBuffer;
                    }

                    // 4. 读取包体
                    await ReceiveFullyAsync(socket, buffer, headerSize, packetLength - headerSize, token);

                    // 5. 处理数据包
                    try
                    {
                        OnRecvPacket(buffer, 0, packetLength);
                    }
                    catch (Exception e)
                    {
                        byte[] packetCopy = new byte[packetLength];
                        Buffer.BlockCopy(buffer, 0, packetCopy, 0, packetLength);
                        OnSocketError(e, packetCopy);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 正常取消
            }
            catch (SocketException e) when (e.SocketErrorCode == SocketError.OperationAborted)
            {
                // Socket被关闭
            }
            catch (Exception e)
            {
                OnSocketError(e);
            }
            finally
            {
                RemoveSession(session);
            }
        }

        /// <summary>
        /// 完整接收指定长度数据
        /// </summary>
        private async Task ReceiveFullyAsync(Socket socket, byte[] buffer, int offset, int size, CancellationToken token)
        {
            int totalReceived = 0;
            while (totalReceived < size)
            {
                // BUG修复: 检查取消状态
                token.ThrowIfCancellationRequested();

                var tcs = new TaskCompletionSource<int>();
                
                // BUG修复: 使用IDisposable来管理取消注册，避免内存泄漏
                CancellationTokenRegistration registration = default;
                try
                {
                    registration = token.Register(() => tcs.TrySetCanceled());

                    socket.BeginReceive(buffer, offset + totalReceived, size - totalReceived, SocketFlags.None, ar =>
                    {
                        try
                        {
                            int recvBytes = socket.EndReceive(ar);
                            tcs.TrySetResult(recvBytes);
                        }
                        catch (Exception ex)
                        {
                            tcs.TrySetException(ex);
                        }
                    }, null);

                    int recvResult = await tcs.Task;

                    if (recvResult == 0)
                    {
                        throw new SocketException(10054); // 连接被远程主机关闭
                    }

                    totalReceived += recvResult;
                }
                finally
                {
                    // BUG修复: 确保取消注册回调
                    registration.Dispose();
                }
            }
        }

        #endregion

        #region 抽象方法 (子类实现)

        /// <summary>解析包长度</summary>
        public abstract uint GetPacketLength(byte[] header, int offset, int length);

        /// <summary>处理收到的数据包</summary>
        public abstract void OnRecvPacket(byte[] packet, int offset, int length);

        /// <summary>断开连接回调</summary>
        public abstract void OnDisconnect();

        /// <summary>Socket错误回调</summary>
        public abstract void OnSocketError(Exception e, byte[] data = null);

        #endregion
    }

    /// <summary>
    /// 回调式客户端监听器
    /// </summary>
    public sealed class WSLCallbackClientListener : WSLClientListener
    {
        private readonly IWSLClientListener _listener;

        public override uint HeaderSize => _listener.HeaderSize;

        public WSLCallbackClientListener(IWSLClientListener listener)
        {
            _listener = listener ?? throw new ArgumentNullException(nameof(listener));
        }

        public override uint GetPacketLength(byte[] header, int offset, int length) => _listener.GetPacketLength(header, offset, length);
        public override void OnRecvPacket(byte[] packet, int offset, int length) => _listener.OnRecvPacket(packet, offset, length);
        public override void OnDisconnect() => _listener.OnDisconnect();
        public override void OnSocketError(Exception e, byte[] data = null) => _listener.OnSocketError(e, data);
    }
}
