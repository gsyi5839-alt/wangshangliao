using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

class RawTcpServer
{
    static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.WriteLine("启动原始TCP服务器 (端口5749)...\n");
        
        var listener = new TcpListener(IPAddress.Any, 5749);
        listener.Start();
        Console.WriteLine("✓ 监听端口5749\n等待621705120.exe连接...\n");
        
        while (true)
        {
            var client = listener.AcceptTcpClient();
            Console.WriteLine("★ 客户端已连接!");
            Console.WriteLine("  远程地址: " + client.Client.RemoteEndPoint);
            
            var stream = client.GetStream();
            var buffer = new byte[4096];
            
            try
            {
                // 等待数据
                stream.ReadTimeout = 5000;
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                
                if (bytesRead > 0)
                {
                    Console.WriteLine("\n收到 " + bytesRead + " 字节:");
                    
                    // 十六进制
                    Console.Write("HEX: ");
                    for (int i = 0; i < Math.Min(bytesRead, 64); i++)
                        Console.Write(buffer[i].ToString("X2") + " ");
                    Console.WriteLine(bytesRead > 64 ? "..." : "");
                    
                    // ASCII
                    var ascii = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                    Console.WriteLine("ASCII: " + ascii.Substring(0, Math.Min(100, ascii.Length)));
                    
                    // UTF8
                    try { 
                        var utf8 = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        Console.WriteLine("UTF8: " + utf8.Substring(0, Math.Min(100, utf8.Length)));
                    } catch { }
                    
                    // 分析前几个字节
                    if (bytesRead >= 4)
                    {
                        Console.WriteLine("\n前4字节分析:");
                        Console.WriteLine("  作为int32 (LE): " + BitConverter.ToInt32(buffer, 0));
                        Console.WriteLine("  作为int32 (BE): " + IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buffer, 0)));
                        Console.WriteLine("  作为ushort[0] (LE): " + BitConverter.ToUInt16(buffer, 0));
                        Console.WriteLine("  作为ushort[1] (LE): " + BitConverter.ToUInt16(buffer, 2));
                    }
                }
                else
                {
                    Console.WriteLine("客户端没有发送数据");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("读取超时或错误: " + ex.Message);
            }
            
            client.Close();
            Console.WriteLine("\n连接已关闭，继续等待...\n");
        }
    }
}
