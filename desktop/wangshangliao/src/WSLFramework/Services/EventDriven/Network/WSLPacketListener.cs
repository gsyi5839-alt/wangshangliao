using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WSLFramework.Services.EventDriven.Network
{
    /// <summary>
    /// 数据包类型
    /// </summary>
    public enum WSLPacketType : byte
    {
        Unknown = 0x00,
        Heartbeat = 0x01,
        HeartbeatAck = 0x02,
        Login = 0x10,
        LoginAck = 0x11,
        Logout = 0x12,
        Message = 0x20,
        MessageAck = 0x21,
        GroupMessage = 0x22,
        PrivateMessage = 0x23,
        SystemNotify = 0x30,
        ApiRequest = 0x40,
        ApiResponse = 0x41,
        Error = 0xFF
    }

    /// <summary>
    /// WSL数据包结构
    /// 协议格式:
    /// [包头 4字节: 长度(含包头)] [类型 1字节] [序列号 4字节] [负载数据]
    /// </summary>
    public class WSLPacket
    {
        public const int HEADER_SIZE = 9; // 4字节长度 + 1字节类型 + 4字节序列号

        public uint Length { get; set; }
        public WSLPacketType Type { get; set; }
        public uint Sequence { get; set; }
        public byte[] Payload { get; set; }

        public WSLPacket()
        {
            Payload = new byte[0];
        }

        public WSLPacket(WSLPacketType type, byte[] payload, uint sequence = 0)
        {
            Type = type;
            Payload = payload ?? new byte[0];
            Sequence = sequence;
            Length = (uint)(HEADER_SIZE + Payload.Length);
        }

        /// <summary>
        /// 序列化为字节数组
        /// </summary>
        public byte[] Serialize()
        {
            byte[] data = new byte[Length];
            
            // 长度 (大端序)
            data[0] = (byte)((Length >> 24) & 0xFF);
            data[1] = (byte)((Length >> 16) & 0xFF);
            data[2] = (byte)((Length >> 8) & 0xFF);
            data[3] = (byte)(Length & 0xFF);
            
            // 类型
            data[4] = (byte)Type;
            
            // 序列号 (大端序)
            data[5] = (byte)((Sequence >> 24) & 0xFF);
            data[6] = (byte)((Sequence >> 16) & 0xFF);
            data[7] = (byte)((Sequence >> 8) & 0xFF);
            data[8] = (byte)(Sequence & 0xFF);
            
            // 负载
            if (Payload?.Length > 0)
            {
                Buffer.BlockCopy(Payload, 0, data, HEADER_SIZE, Payload.Length);
            }
            
            return data;
        }

        /// <summary>
        /// 从字节数组反序列化
        /// </summary>
        public static WSLPacket Deserialize(byte[] data, int offset, int length)
        {
            if (length < HEADER_SIZE)
            {
                throw new ArgumentException($"数据长度不足: {length} < {HEADER_SIZE}");
            }

            var packet = new WSLPacket
            {
                Length = (uint)((data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3]),
                Type = (WSLPacketType)data[offset + 4],
                Sequence = (uint)((data[offset + 5] << 24) | (data[offset + 6] << 16) | (data[offset + 7] << 8) | data[offset + 8])
            };

            if (length > HEADER_SIZE)
            {
                packet.Payload = new byte[length - HEADER_SIZE];
                Buffer.BlockCopy(data, offset + HEADER_SIZE, packet.Payload, 0, length - HEADER_SIZE);
            }

            return packet;
        }

        /// <summary>
        /// 创建心跳包
        /// </summary>
        public static WSLPacket CreateHeartbeat(uint sequence)
        {
            return new WSLPacket(WSLPacketType.Heartbeat, null, sequence);
        }

        /// <summary>
        /// 创建登录包
        /// </summary>
        public static WSLPacket CreateLogin(string account, string token, uint sequence)
        {
            string json = $"{{\"account\":\"{account}\",\"token\":\"{token}\"}}";
            return new WSLPacket(WSLPacketType.Login, Encoding.UTF8.GetBytes(json), sequence);
        }

        /// <summary>
        /// 获取负载字符串
        /// </summary>
        public string GetPayloadString()
        {
            if (Payload == null || Payload.Length == 0)
                return string.Empty;
            return Encoding.UTF8.GetString(Payload);
        }

        public override string ToString()
        {
            string payload = GetPayloadString();
            string preview = payload.Length > 50 ? payload.Substring(0, 50) + "..." : payload;
            return $"WSLPacket[Type={Type}, Seq={Sequence}, Len={Length}, Payload={preview}]";
        }
    }

    /// <summary>
    /// WSL数据包监听器 - 实现具体协议解析
    /// 针对 .NET Framework 4.7.2 兼容
    /// </summary>
    public class WSLPacketListener : WSLClientListener
    {
        /// <summary>包头大小 (4字节用于长度)</summary>
        public override uint HeaderSize => 4;

        /// <summary>收到数据包事件</summary>
        public event Action<WSLPacket> OnPacketReceived;

        /// <summary>断开连接事件</summary>
        public event Action<string> OnDisconnected;

        /// <summary>错误事件</summary>
        public event Action<Exception, byte[]> OnError;

        /// <summary>日志事件</summary>
        public event Action<string> OnLog;

        private int _sequence = 0;
        private Timer _heartbeatTimer;
        private readonly int _heartbeatInterval;

        public WSLPacketListener(int heartbeatIntervalMs = 30000)
        {
            _heartbeatInterval = heartbeatIntervalMs;
        }

        #region 实现抽象方法

        /// <summary>
        /// 从包头解析数据包长度
        /// </summary>
        public override uint GetPacketLength(byte[] header, int offset, int length)
        {
            if (length < 4)
                throw new ArgumentException("包头长度不足");

            // 大端序解析
            return (uint)((header[offset] << 24) | (header[offset + 1] << 16) | (header[offset + 2] << 8) | header[offset + 3]);
        }

        /// <summary>
        /// 处理收到的数据包
        /// </summary>
        public override void OnRecvPacket(byte[] packet, int offset, int length)
        {
            try
            {
                var wslPacket = WSLPacket.Deserialize(packet, offset, length);
                
                Log($"收到数据包: {wslPacket.Type}, Seq={wslPacket.Sequence}, Len={wslPacket.Length}");

                // 处理心跳响应
                if (wslPacket.Type == WSLPacketType.HeartbeatAck)
                {
                    Log("收到心跳响应");
                    return;
                }

                // 触发事件
                OnPacketReceived?.Invoke(wslPacket);
            }
            catch (Exception e)
            {
                Log($"解析数据包失败: {e.Message}");
                byte[] copy = new byte[length];
                Buffer.BlockCopy(packet, offset, copy, 0, length);
                OnError?.Invoke(e, copy);
            }
        }

        /// <summary>
        /// 断开连接回调
        /// </summary>
        public override void OnDisconnect()
        {
            Log("连接已断开");
            StopHeartbeat();
            OnDisconnected?.Invoke("连接断开");
        }

        /// <summary>
        /// Socket错误回调
        /// </summary>
        public override void OnSocketError(Exception e, byte[] data = null)
        {
            Log($"Socket错误: {e.Message}");
            OnError?.Invoke(e, data);
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 连接并启动
        /// </summary>
        public async Task<bool> ConnectAndStartAsync(string host, int port)
        {
            Log($"正在连接到 {host}:{port}...");
            
            bool connected = await ConnectAsync(host, port);
            if (connected)
            {
                Log("连接成功");
                StartHeartbeat();
            }
            else
            {
                Log("连接失败");
            }
            
            return connected;
        }

        /// <summary>
        /// 发送数据包
        /// </summary>
        public async Task<bool> SendPacketAsync(WSLPacket packet)
        {
            if (!Connected)
            {
                Log("未连接，无法发送数据包");
                return false;
            }

            byte[] data = packet.Serialize();
            Log($"发送数据包: {packet.Type}, Seq={packet.Sequence}, Len={data.Length}");
            
            return await SendAsync(data);
        }

        /// <summary>
        /// 发送登录请求
        /// </summary>
        public async Task<bool> LoginAsync(string account, string token)
        {
            var packet = WSLPacket.CreateLogin(account, token, GetNextSequence());
            return await SendPacketAsync(packet);
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        public async Task<bool> SendMessageAsync(string targetId, string content, bool isGroup)
        {
            var type = isGroup ? WSLPacketType.GroupMessage : WSLPacketType.PrivateMessage;
            string json = $"{{\"target\":\"{targetId}\",\"content\":\"{EscapeJson(content)}\"}}";
            var packet = new WSLPacket(type, Encoding.UTF8.GetBytes(json), GetNextSequence());
            return await SendPacketAsync(packet);
        }

        /// <summary>
        /// 发送API请求
        /// </summary>
        public async Task<bool> SendApiRequestAsync(string apiName, string parameters)
        {
            string json = $"{{\"api\":\"{apiName}\",\"params\":{parameters}}}";
            var packet = new WSLPacket(WSLPacketType.ApiRequest, Encoding.UTF8.GetBytes(json), GetNextSequence());
            return await SendPacketAsync(packet);
        }

        /// <summary>
        /// 关闭连接
        /// </summary>
        public void Close()
        {
            StopHeartbeat();
            Disconnect();
        }

        #endregion

        #region 私有方法

        private uint GetNextSequence()
        {
            return (uint)Interlocked.Increment(ref _sequence);
        }

        private void StartHeartbeat()
        {
            StopHeartbeat();
            // BUG修复: Timer回调中使用 Task.Run 包装 async 操作，确保异常被正确处理
            _heartbeatTimer = new Timer(_ =>
            {
                // 不要使用 async lambda，改为 Task.Run
                Task.Run(async () =>
                {
                    try
                    {
                        if (Connected)
                        {
                            var packet = WSLPacket.CreateHeartbeat(GetNextSequence());
                            await SendPacketAsync(packet);
                        }
                    }
                    catch (ObjectDisposedException)
                    {
                        // Timer已被销毁，忽略
                    }
                    catch (Exception ex)
                    {
                        Log($"心跳发送异常: {ex.Message}");
                    }
                });
            }, null, _heartbeatInterval, _heartbeatInterval);
            
            Log($"心跳定时器已启动，间隔: {_heartbeatInterval}ms");
        }

        private void StopHeartbeat()
        {
            _heartbeatTimer?.Dispose();
            _heartbeatTimer = null;
        }

        private void Log(string message)
        {
            OnLog?.Invoke($"[WSLPacketListener] {message}");
        }

        private string EscapeJson(string str)
        {
            return str?.Replace("\\", "\\\\")
                      .Replace("\"", "\\\"")
                      .Replace("\n", "\\n")
                      .Replace("\r", "\\r")
                      .Replace("\t", "\\t") ?? "";
        }

        #endregion
    }
}
