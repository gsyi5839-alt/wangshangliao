using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace WangShangLiaoBot.Services.PureClient
{
    /// <summary>
    /// 协议握手分析器
    /// 用于分析xclient与服务器之间的握手协议
    /// </summary>
    public class HandshakeAnalyzer
    {
        // 从网络分析发现的服务器配置
        public const string SERVER_IP = "120.236.198.109";
        public const int SERVER_PORT = 47437;
        
        // xclient本地端口
        public const int XCLIENT_LOCAL_PORT = 21303;
        public const int XCLIENT_HTTP_PORT = 21308;

        /// <summary>
        /// 分析握手协议
        /// </summary>
        public static async Task<HandshakeResult> AnalyzeHandshakeAsync()
        {
            var result = new HandshakeResult();
            
            Console.WriteLine("=== 握手协议分析 ===");
            
            // 1. 测试本地xclient端口
            await TestLocalXClientAsync(result);
            
            // 2. 测试远程服务器
            await TestRemoteServerAsync(result);
            
            return result;
        }

        private static async Task TestLocalXClientAsync(HandshakeResult result)
        {
            Console.WriteLine("\n[1. 测试本地xclient端口]");
            
            // 测试端口21303 (主通信端口)
            try
            {
                using (var client = new TcpClient())
                {
                    client.ReceiveTimeout = 5000;
                    await client.ConnectAsync("127.0.0.1", XCLIENT_LOCAL_PORT);
                    
                    result.LocalPort21303Available = true;
                    Console.WriteLine($"  端口 {XCLIENT_LOCAL_PORT}: 可用");
                    
                    var stream = client.GetStream();
                    var buffer = new byte[4096];
                    
                    // 等待服务器初始数据
                    await Task.Delay(500);
                    
                    if (stream.DataAvailable)
                    {
                        var read = await stream.ReadAsync(buffer, 0, buffer.Length);
                        result.InitialDataFromXClient = new byte[read];
                        Array.Copy(buffer, result.InitialDataFromXClient, read);
                        Console.WriteLine($"  收到初始数据: {read} bytes");
                        Console.WriteLine($"  Hex: {BitConverter.ToString(buffer, 0, Math.Min(read, 32))}");
                    }
                    else
                    {
                        Console.WriteLine("  无初始数据 - 客户端需先发送请求");
                        
                        // 尝试发送不同格式的请求
                        var testPatterns = new List<(string name, byte[] data)>
                        {
                            ("JSON ping", Encoding.UTF8.GetBytes("{\"cmd\":\"ping\"}\n")),
                            ("Binary header", new byte[] { 0x00, 0x00, 0x00, 0x0D, 0x7B, 0x22, 0x63, 0x6D, 0x64, 0x22, 0x3A, 0x22, 0x70, 0x69, 0x6E, 0x67, 0x22, 0x7D }),
                            ("Protobuf varint", new byte[] { 0x0A, 0x04, 0x70, 0x69, 0x6E, 0x67 })
                        };

                        foreach (var (name, data) in testPatterns)
                        {
                            try
                            {
                                await stream.WriteAsync(data, 0, data.Length);
                                await stream.FlushAsync();
                                await Task.Delay(500);
                                
                                if (stream.DataAvailable)
                                {
                                    var read = await stream.ReadAsync(buffer, 0, buffer.Length);
                                    Console.WriteLine($"  {name}: 收到响应 {read} bytes");
                                    result.WorkingPattern = name;
                                    result.ResponseData = new byte[read];
                                    Array.Copy(buffer, result.ResponseData, read);
                                    break;
                                }
                            }
                            catch { }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  端口 {XCLIENT_LOCAL_PORT}: 错误 - {ex.Message}");
            }
            
            // 测试端口21308 (HTTP端口)
            try
            {
                using (var client = new TcpClient())
                {
                    await client.ConnectAsync("127.0.0.1", XCLIENT_HTTP_PORT);
                    result.LocalPort21308Available = true;
                    Console.WriteLine($"  端口 {XCLIENT_HTTP_PORT}: 可用 (HTTP)");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  端口 {XCLIENT_HTTP_PORT}: {ex.Message}");
            }
        }

        private static async Task TestRemoteServerAsync(HandshakeResult result)
        {
            Console.WriteLine("\n[2. 测试远程服务器]");
            Console.WriteLine($"  目标: {SERVER_IP}:{SERVER_PORT}");
            
            try
            {
                using (var client = new TcpClient())
                {
                    client.ReceiveTimeout = 10000;
                    client.SendTimeout = 5000;
                    
                    var connectTask = client.ConnectAsync(SERVER_IP, SERVER_PORT);
                    if (await Task.WhenAny(connectTask, Task.Delay(10000)) == connectTask)
                    {
                        result.RemoteServerAvailable = true;
                        Console.WriteLine("  连接成功!");
                        
                        var stream = client.GetStream();
                        var buffer = new byte[8192];
                        
                        // 检查服务器是否主动发送数据
                        await Task.Delay(2000);
                        
                        if (stream.DataAvailable)
                        {
                            var read = await stream.ReadAsync(buffer, 0, buffer.Length);
                            result.ServerInitialData = new byte[read];
                            Array.Copy(buffer, result.ServerInitialData, read);
                            Console.WriteLine($"  服务器主动发送: {read} bytes");
                            Console.WriteLine($"  Hex: {BitConverter.ToString(buffer, 0, Math.Min(read, 64))}");
                            
                            // 分析数据格式
                            AnalyzeDataFormat(buffer, read, result);
                        }
                        else
                        {
                            Console.WriteLine("  服务器等待客户端先发送");
                            result.ServerWaitsForClient = true;
                            
                            // 尝试TLS握手
                            Console.WriteLine("  尝试TLS握手...");
                            var tlsClientHello = BuildTlsClientHello();
                            await stream.WriteAsync(tlsClientHello, 0, tlsClientHello.Length);
                            await stream.FlushAsync();
                            
                            await Task.Delay(2000);
                            
                            if (stream.DataAvailable)
                            {
                                var read = await stream.ReadAsync(buffer, 0, buffer.Length);
                                result.TlsHandshakeResponse = new byte[read];
                                Array.Copy(buffer, result.TlsHandshakeResponse, read);
                                Console.WriteLine($"  TLS响应: {read} bytes");
                                Console.WriteLine($"  Hex: {BitConverter.ToString(buffer, 0, Math.Min(read, 64))}");
                                
                                // 检查是否是TLS ServerHello
                                if (buffer[0] == 0x16) // TLS Handshake
                                {
                                    result.UsesTls = true;
                                    Console.WriteLine("  确认使用TLS!");
                                }
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("  连接超时");
                        result.RemoteServerAvailable = false;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  错误: {ex.Message}");
            }
        }

        private static byte[] BuildTlsClientHello()
        {
            // 构建最小化的TLS 1.2 ClientHello
            using (var ms = new MemoryStream())
            {
                var writer = new BinaryWriter(ms);
                
                // TLS Record Layer
                writer.Write((byte)0x16);  // Content Type: Handshake
                writer.Write((byte)0x03);  // Version: TLS 1.0 (for compatibility)
                writer.Write((byte)0x01);
                
                // 长度占位符 (稍后填充)
                var lengthPos = ms.Position;
                writer.Write((short)0);
                
                // Handshake Protocol
                writer.Write((byte)0x01);  // Handshake Type: ClientHello
                
                // 握手长度占位符
                var hsLengthPos = ms.Position;
                writer.Write((byte)0);
                writer.Write((short)0);
                
                // Client Version: TLS 1.2
                writer.Write((byte)0x03);
                writer.Write((byte)0x03);
                
                // Random (32 bytes)
                var random = new byte[32];
                new Random().NextBytes(random);
                writer.Write(random);
                
                // Session ID Length: 0
                writer.Write((byte)0);
                
                // Cipher Suites
                writer.Write((short)4);  // 2 cipher suites
                writer.Write((short)0xC02F);  // TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256
                writer.Write((short)0x009E);  // TLS_DHE_RSA_WITH_AES_128_GCM_SHA256
                
                // Compression Methods
                writer.Write((byte)1);
                writer.Write((byte)0);  // null compression
                
                // Extensions Length: 0
                writer.Write((short)0);
                
                // 回填长度
                var totalLength = ms.Length - 5;
                var hsLength = ms.Length - hsLengthPos - 3;
                
                ms.Position = lengthPos;
                writer.Write((byte)((totalLength >> 8) & 0xFF));
                writer.Write((byte)(totalLength & 0xFF));
                
                ms.Position = hsLengthPos;
                writer.Write((byte)((hsLength >> 16) & 0xFF));
                writer.Write((byte)((hsLength >> 8) & 0xFF));
                writer.Write((byte)(hsLength & 0xFF));
                
                return ms.ToArray();
            }
        }

        private static void AnalyzeDataFormat(byte[] data, int length, HandshakeResult result)
        {
            if (length < 4) return;
            
            // 检查常见协议标识
            if (data[0] == 0x16 && data[1] == 0x03)
            {
                result.ProtocolType = "TLS";
                result.UsesTls = true;
            }
            else if (data[0] == '{')
            {
                result.ProtocolType = "JSON";
            }
            else if (data[0] == 0x08 || data[0] == 0x0A)
            {
                result.ProtocolType = "Protobuf (可能)";
            }
            else if (BitConverter.ToUInt32(data, 0) == 0x4E494D00)
            {
                result.ProtocolType = "NIM魔数";
            }
            else
            {
                result.ProtocolType = "未知二进制";
            }
            
            Console.WriteLine($"  协议类型: {result.ProtocolType}");
        }

        /// <summary>
        /// 运行握手分析测试
        /// </summary>
        public static async Task RunTestAsync()
        {
            Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║           旺商聊握手协议分析器 v1.0                        ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
            
            var result = await AnalyzeHandshakeAsync();
            
            Console.WriteLine("\n=== 分析结果 ===");
            Console.WriteLine($"  本地端口21303可用: {result.LocalPort21303Available}");
            Console.WriteLine($"  本地端口21308可用: {result.LocalPort21308Available}");
            Console.WriteLine($"  远程服务器可用: {result.RemoteServerAvailable}");
            Console.WriteLine($"  使用TLS: {result.UsesTls}");
            Console.WriteLine($"  协议类型: {result.ProtocolType ?? "未确定"}");
            Console.WriteLine($"  有效请求格式: {result.WorkingPattern ?? "未找到"}");
        }
    }

    /// <summary>
    /// 握手分析结果
    /// </summary>
    public class HandshakeResult
    {
        public bool LocalPort21303Available { get; set; }
        public bool LocalPort21308Available { get; set; }
        public bool RemoteServerAvailable { get; set; }
        public bool UsesTls { get; set; }
        public bool ServerWaitsForClient { get; set; }
        public string ProtocolType { get; set; }
        public string WorkingPattern { get; set; }
        public byte[] InitialDataFromXClient { get; set; }
        public byte[] ResponseData { get; set; }
        public byte[] ServerInitialData { get; set; }
        public byte[] TlsHandshakeResponse { get; set; }
    }
}
