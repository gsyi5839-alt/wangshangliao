using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WangShangLiaoBot.Services.PureClient
{
    /// <summary>
    /// 旺商聊TCP协议实现
    /// 基于逆向分析的自定义二进制协议
    /// </summary>
    public class WslTcpProtocol : IDisposable
    {
        #region 协议常量

        // 协议魔数 (从xclient分析)
        private const ushort PROTOCOL_MAGIC = 0xABCD;
        
        // 协议版本
        private const byte PROTOCOL_VERSION = 0x01;
        
        // 消息类型
        public const byte MSG_TYPE_REQUEST = 0x01;
        public const byte MSG_TYPE_RESPONSE = 0x02;
        public const byte MSG_TYPE_PUSH = 0x03;
        public const byte MSG_TYPE_HEARTBEAT = 0x04;
        public const byte MSG_TYPE_HANDSHAKE = 0x05;
        public const byte MSG_TYPE_ENCRYPT = 0x06;
        public const byte MSG_TYPE_DECRYPT = 0x07;

        // 头部大小
        private const int HEADER_SIZE = 16;

        #endregion

        private TcpClient _client;
        private NetworkStream _stream;
        private CancellationTokenSource _cts;
        private Task _receiveTask;
        private int _sequenceId;
        private readonly ConcurrentDictionary<int, TaskCompletionSource<WslPacket>> _pendingRequests;
        private readonly byte[] _encryptionKey;
        private bool _isConnected;

        public event Action<WslPacket> OnPacketReceived;
        public event Action<Exception> OnError;
        public event Action OnConnected;
        public event Action OnDisconnected;

        public bool IsConnected => _isConnected && _client?.Connected == true;

        public WslTcpProtocol(byte[] encryptionKey = null)
        {
            _encryptionKey = encryptionKey;
            _pendingRequests = new ConcurrentDictionary<int, TaskCompletionSource<WslPacket>>();
        }

        /// <summary>
        /// 连接到服务器
        /// </summary>
        public async Task ConnectAsync(string host, int port)
        {
            if (_isConnected)
            {
                throw new InvalidOperationException("Already connected");
            }

            _client = new TcpClient();
            _client.ReceiveTimeout = 30000;
            _client.SendTimeout = 10000;
            _client.NoDelay = true;

            await _client.ConnectAsync(host, port);
            _stream = _client.GetStream();
            _isConnected = true;

            _cts = new CancellationTokenSource();
            _receiveTask = Task.Run(() => ReceiveLoop(_cts.Token));

            OnConnected?.Invoke();

            // 发送握手包
            await SendHandshakeAsync();
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        public void Disconnect()
        {
            if (!_isConnected) return;

            _isConnected = false;
            _cts?.Cancel();

            try
            {
                _stream?.Close();
                _client?.Close();
            }
            catch { }

            _stream = null;
            _client = null;

            // 取消所有等待中的请求
            foreach (var kvp in _pendingRequests)
            {
                kvp.Value.TrySetCanceled();
            }
            _pendingRequests.Clear();

            OnDisconnected?.Invoke();
        }

        /// <summary>
        /// 发送请求并等待响应
        /// </summary>
        public async Task<WslPacket> SendRequestAsync(byte[] data, byte msgType = MSG_TYPE_REQUEST, int timeoutMs = 30000)
        {
            var seqId = Interlocked.Increment(ref _sequenceId);
            var tcs = new TaskCompletionSource<WslPacket>();
            _pendingRequests[seqId] = tcs;

            try
            {
                var packet = CreatePacket(msgType, seqId, data);
                await SendPacketAsync(packet);

                using (var cts = new CancellationTokenSource(timeoutMs))
                {
                    cts.Token.Register(() => tcs.TrySetException(new TimeoutException("Request timeout")));
                    return await tcs.Task;
                }
            }
            finally
            {
                _pendingRequests.TryRemove(seqId, out _);
            }
        }

        /// <summary>
        /// 发送数据 (不等待响应)
        /// </summary>
        public async Task SendAsync(byte[] data, byte msgType = MSG_TYPE_REQUEST)
        {
            var seqId = Interlocked.Increment(ref _sequenceId);
            var packet = CreatePacket(msgType, seqId, data);
            await SendPacketAsync(packet);
        }

        /// <summary>
        /// 发送心跳
        /// </summary>
        public async Task SendHeartbeatAsync()
        {
            var packet = CreatePacket(MSG_TYPE_HEARTBEAT, 0, new byte[0]);
            await SendPacketAsync(packet);
        }

        /// <summary>
        /// 发送API请求
        /// </summary>
        public async Task<WslPacket> SendApiRequestAsync(string url, string paramsJson)
        {
            var requestData = WslProtobuf.EncodeRequest(url, paramsJson);
            return await SendRequestAsync(requestData, MSG_TYPE_REQUEST);
        }

        #region 私有方法

        private async Task SendHandshakeAsync()
        {
            // 握手数据: 版本 + 密钥信息
            using (var ms = new MemoryStream())
            {
                var writer = new BinaryWriter(ms);
                writer.Write(PROTOCOL_VERSION);
                
                if (_encryptionKey != null)
                {
                    writer.Write((byte)1); // 使用加密
                    writer.Write(_encryptionKey.Length);
                    writer.Write(_encryptionKey);
                }
                else
                {
                    writer.Write((byte)0); // 不使用加密
                }

                var data = ms.ToArray();
                var packet = CreatePacket(MSG_TYPE_HANDSHAKE, 0, data);
                await SendPacketAsync(packet);
            }
        }

        private WslPacket CreatePacket(byte msgType, int seqId, byte[] data)
        {
            return new WslPacket
            {
                Magic = PROTOCOL_MAGIC,
                Version = PROTOCOL_VERSION,
                MessageType = msgType,
                SequenceId = seqId,
                Data = data
            };
        }

        private async Task SendPacketAsync(WslPacket packet)
        {
            if (!_isConnected || _stream == null)
            {
                throw new InvalidOperationException("Not connected");
            }

            var bytes = packet.Serialize(_encryptionKey);
            await _stream.WriteAsync(bytes, 0, bytes.Length);
            await _stream.FlushAsync();
        }

        private async Task ReceiveLoop(CancellationToken token)
        {
            var headerBuffer = new byte[HEADER_SIZE];

            try
            {
                while (!token.IsCancellationRequested && _isConnected)
                {
                    // 读取头部
                    var bytesRead = await ReadExactAsync(_stream, headerBuffer, 0, HEADER_SIZE, token);
                    if (bytesRead < HEADER_SIZE)
                    {
                        break;
                    }

                    // 解析头部
                    var packet = WslPacket.ParseHeader(headerBuffer);
                    
                    // 读取数据
                    if (packet.DataLength > 0)
                    {
                        packet.Data = new byte[packet.DataLength];
                        bytesRead = await ReadExactAsync(_stream, packet.Data, 0, packet.DataLength, token);
                        if (bytesRead < packet.DataLength)
                        {
                            break;
                        }

                        // 解密数据
                        if (packet.IsEncrypted && _encryptionKey != null)
                        {
                            try
                            {
                                packet.Data = WslCrypto.Decrypt(packet.Data, _encryptionKey);
                            }
                            catch (Exception ex)
                            {
                                OnError?.Invoke(new Exception($"Decryption failed: {ex.Message}"));
                                continue;
                            }
                        }
                    }

                    // 处理响应
                    if (_pendingRequests.TryGetValue(packet.SequenceId, out var tcs))
                    {
                        tcs.TrySetResult(packet);
                    }
                    else
                    {
                        // 推送消息
                        OnPacketReceived?.Invoke(packet);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 正常取消
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex);
            }
            finally
            {
                if (_isConnected)
                {
                    Disconnect();
                }
            }
        }

        private async Task<int> ReadExactAsync(NetworkStream stream, byte[] buffer, int offset, int count, CancellationToken token)
        {
            var totalRead = 0;
            while (totalRead < count)
            {
                var read = await stream.ReadAsync(buffer, offset + totalRead, count - totalRead, token);
                if (read == 0)
                {
                    break; // 连接关闭
                }
                totalRead += read;
            }
            return totalRead;
        }

        #endregion

        public void Dispose()
        {
            Disconnect();
            _cts?.Dispose();
        }
    }

    /// <summary>
    /// 协议数据包
    /// </summary>
    public class WslPacket
    {
        /// <summary>魔数</summary>
        public ushort Magic { get; set; }
        
        /// <summary>协议版本</summary>
        public byte Version { get; set; }
        
        /// <summary>消息类型</summary>
        public byte MessageType { get; set; }
        
        /// <summary>标志位</summary>
        public byte Flags { get; set; }
        
        /// <summary>是否加密</summary>
        public bool IsEncrypted => (Flags & 0x01) != 0;
        
        /// <summary>序列号</summary>
        public int SequenceId { get; set; }
        
        /// <summary>数据长度</summary>
        public int DataLength { get; set; }
        
        /// <summary>数据</summary>
        public byte[] Data { get; set; }

        /// <summary>
        /// 序列化数据包
        /// </summary>
        public byte[] Serialize(byte[] encryptionKey = null)
        {
            byte[] dataToSend = Data ?? new byte[0];
            byte flags = 0;

            // 加密数据
            if (encryptionKey != null && dataToSend.Length > 0)
            {
                var nonce = WslCrypto.GenerateNonce();
                dataToSend = WslCrypto.Encrypt(dataToSend, encryptionKey, nonce);
                flags |= 0x01; // 设置加密标志
            }

            using (var ms = new MemoryStream())
            {
                var writer = new BinaryWriter(ms);
                
                // 写入头部 (16字节)
                writer.Write(Magic);           // 2 bytes
                writer.Write(Version);         // 1 byte
                writer.Write(MessageType);     // 1 byte
                writer.Write(flags);           // 1 byte
                writer.Write((byte)0);         // 1 byte reserved
                writer.Write((short)0);        // 2 bytes reserved
                writer.Write(SequenceId);      // 4 bytes
                writer.Write(dataToSend.Length); // 4 bytes

                // 写入数据
                writer.Write(dataToSend);

                return ms.ToArray();
            }
        }

        /// <summary>
        /// 解析头部
        /// </summary>
        public static WslPacket ParseHeader(byte[] header)
        {
            using (var ms = new MemoryStream(header))
            {
                var reader = new BinaryReader(ms);
                return new WslPacket
                {
                    Magic = reader.ReadUInt16(),
                    Version = reader.ReadByte(),
                    MessageType = reader.ReadByte(),
                    Flags = reader.ReadByte(),
                    // 跳过3字节保留
                    SequenceId = (reader.ReadByte(), reader.ReadInt16(), reader.ReadInt32()).Item3,
                    DataLength = reader.ReadInt32()
                };
            }
        }

        /// <summary>
        /// 获取数据为UTF8字符串
        /// </summary>
        public string GetDataAsString()
        {
            if (Data == null || Data.Length == 0) return string.Empty;
            return Encoding.UTF8.GetString(Data);
        }
    }
}
