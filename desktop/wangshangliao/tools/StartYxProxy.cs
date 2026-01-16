using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using HPSocket;
using HPSocket.Tcp;

/// <summary>
/// YX SDK代理服务器 - 自主实现版
/// 协议格式: | Length(4字节LE) | Data |
/// 消息格式: | Type(1) | TimeLen(1) | Time(GB2312) | MsgType(4) | JsonLen(4) | Json |
/// </summary>
class StartYxProxy
{
    static TcpPackServer server;
    static IntPtr clientConnId = IntPtr.Zero;
    static bool isRunning = false;
    static int messageCount = 0;
    
    // NIM凭证
    static string appKey = "";
    static string userId = "";
    static string userPwd = "";
    static string oldUser = "";
    static string sToken = "";
    static string uid = "";
    
    static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.WriteLine("========================================");
        Console.WriteLine("  YX SDK代理服务器 (自主实现版)");
        Console.WriteLine("========================================");
        Console.WriteLine("");
        
        LoadNimCredentials();
        
        if (StartServer(5749))
        {
            Console.WriteLine("");
            Console.WriteLine("[提示] 按Q退出");
            Console.WriteLine("");
            
            while (true)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.Q) break;
                }
                Thread.Sleep(100);
            }
            
            StopServer();
        }
        else
        {
            Console.WriteLine("[错误] 启动失败!");
            Console.WriteLine("按任意键退出...");
            Console.ReadKey();
        }
    }
    
    static void LoadNimCredentials()
    {
        try
        {
            var configPath = @"C:\Users\Administrator\Desktop\zcg25.2.15\YX_Clinent\621705120\YX_Client.dll";
            if (!File.Exists(configPath)) return;
            
            Console.WriteLine("[加载] NIM凭证...");
            var lines = File.ReadAllLines(configPath);
            foreach (var line in lines)
            {
                if (line.Contains("="))
                {
                    var parts = line.Split(new char[] { '=' }, 2);
                    if (parts.Length == 2)
                    {
                        var k = parts[0].Trim();
                        var v = parts[1].Trim();
                        switch (k)
                        {
                            case "APP_KEY": appKey = v; break;
                            case "USER_ID": userId = v; break;
                            case "USER_PWD": userPwd = v; break;
                            case "OLD_USER": oldUser = v; break;
                            case "S_TOKEN": sToken = v; break;
                            case "UID": uid = v; break;
                        }
                    }
                }
            }
            Console.WriteLine("  USER_ID: " + userId);
            Console.WriteLine("  OLD_USER: " + oldUser);
        }
        catch (Exception ex)
        {
            Console.WriteLine("[错误] " + ex.Message);
        }
    }
    
    static bool StartServer(ushort port)
    {
        try
        {
            server = new TcpPackServer();
            
            // ★★★ 关键: PackHeaderFlag = 0 表示无包头标志 ★★★
            // 协议格式: | Length(4字节) | Data |
            server.PackHeaderFlag = 0x0000;  // 无包头标志！
            server.MaxPackSize = 0x100000;   // 1MB
            
            Console.WriteLine("[配置] PackHeaderFlag: 0x0000 (无包头)");
            Console.WriteLine("[配置] MaxPackSize: 1MB");
            Console.WriteLine("[配置] 端口: " + port);
            
            server.OnPrepareListen += OnPrepareListen;
            server.OnAccept += OnAccept;
            server.OnReceive += OnReceive;
            server.OnClose += OnClose;
            
            server.Address = "0.0.0.0";
            server.Port = port;
            
            if (server.Start())
            {
                isRunning = true;
                Console.WriteLine("[成功] 服务器已启动!");
                return true;
            }
            else
            {
                Console.WriteLine("[失败] " + server.ErrorMessage);
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("[异常] " + ex.Message);
            return false;
        }
    }
    
    static void StopServer()
    {
        if (server != null)
        {
            server.Stop();
            server.Dispose();
        }
        isRunning = false;
    }
    
    static HandleResult OnPrepareListen(IServer sender, IntPtr listen)
    {
        Console.WriteLine("[监听] 准备就绪");
        return HandleResult.Ok;
    }
    
    static HandleResult OnAccept(IServer sender, IntPtr connId, IntPtr client)
    {
        clientConnId = connId;
        messageCount = 0;
        Console.WriteLine("");
        Console.WriteLine("★★★ 客户端已连接! ConnId=" + connId + " ★★★");
        Console.WriteLine("");
        return HandleResult.Ok;
    }
    
    static HandleResult OnReceive(IServer sender, IntPtr connId, byte[] data)
    {
        messageCount++;
        Console.WriteLine("┌─────────────────────────────────────────");
        Console.WriteLine("│ [收到] 消息 #" + messageCount + ", " + data.Length + " 字节");
        
        // 解析自定义协议
        ParseAndRespond(connId, data);
        
        Console.WriteLine("└─────────────────────────────────────────");
        return HandleResult.Ok;
    }
    
    static HandleResult OnClose(IServer sender, IntPtr connId, SocketOperation op, int err)
    {
        Console.WriteLine("[断开] ConnId=" + connId + ", 错误=" + err);
        if (connId == clientConnId) clientConnId = IntPtr.Zero;
        return HandleResult.Ok;
    }
    
    /// <summary>
    /// 解析消息并响应
    /// 格式: | Type(1) | TimeLen(1) | Time | MsgType(4) | JsonLen(4) | Json |
    /// </summary>
    static void ParseAndRespond(IntPtr connId, byte[] data)
    {
        try
        {
            if (data.Length < 10)
            {
                Console.WriteLine("│ [错误] 数据太短");
                return;
            }
            
            int pos = 0;
            
            // Type (1字节)
            byte type = data[pos++];
            Console.WriteLine("│ Type: " + type);
            
            // TimeLen (1字节)
            byte timeLen = data[pos++];
            Console.WriteLine("│ TimeLen: " + timeLen);
            
            // Time (GB2312编码)
            if (pos + timeLen > data.Length)
            {
                Console.WriteLine("│ [错误] 时间字段越界");
                return;
            }
            string timeStr = Encoding.GetEncoding("GB2312").GetString(data, pos, timeLen);
            pos += timeLen;
            Console.WriteLine("│ Time: " + timeStr);
            
            // MsgType (4字节, little endian)
            if (pos + 4 > data.Length)
            {
                Console.WriteLine("│ [错误] MsgType越界");
                return;
            }
            int msgType = BitConverter.ToInt32(data, pos);
            pos += 4;
            Console.WriteLine("│ MsgType: " + msgType);
            
            // JsonLen (4字节)
            if (pos + 4 > data.Length)
            {
                Console.WriteLine("│ [错误] JsonLen越界");
                return;
            }
            int jsonLen = BitConverter.ToInt32(data, pos);
            pos += 4;
            Console.WriteLine("│ JsonLen: " + jsonLen);
            
            // Json
            if (pos + jsonLen > data.Length)
            {
                Console.WriteLine("│ [警告] JSON可能被截断");
                jsonLen = data.Length - pos;
            }
            string json = Encoding.UTF8.GetString(data, pos, jsonLen);
            Console.WriteLine("│ JSON: " + json);
            
            // 解析JSON
            var serializer = new JavaScriptSerializer();
            var jsonData = serializer.Deserialize<Dictionary<string, object>>(json);
            
            // 根据消息类型响应
            if (jsonData != null)
            {
                string ret = jsonData.ContainsKey("ret") ? Convert.ToString(jsonData["ret"]) : "";
                string id = jsonData.ContainsKey("id") ? Convert.ToString(jsonData["id"]) : "";
                
                Console.WriteLine("│ ");
                Console.WriteLine("│ ★ 客户端消息: " + ret);
                Console.WriteLine("│ ");
                
                // 发送响应
                SendResponse(connId, msgType, id);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("│ [解析错误] " + ex.Message);
            
            // 回退: 发送简单确认
            SendSimpleAck(connId);
        }
    }
    
    /// <summary>
    /// 发送响应
    /// </summary>
    static void SendResponse(IntPtr connId, int msgType, string id)
    {
        Console.WriteLine("│ [响应] 构建响应消息...");
        
        // 构建响应JSON
        var responseJson = new Dictionary<string, object>
        {
            { "id", id },
            { "code", 0 },
            { "ret", "ok" },
            { "msg", "success" }
        };
        
        var serializer = new JavaScriptSerializer();
        string jsonStr = serializer.Serialize(responseJson);
        byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonStr);
        
        // 构建时间戳
        string timeStr = DateTime.Now.ToString("yyyy年M月d日H时m分s秒");
        byte[] timeBytes = Encoding.GetEncoding("GB2312").GetBytes(timeStr);
        
        // 构建消息
        // | Type(1) | TimeLen(1) | Time | MsgType(4) | JsonLen(4) | Json |
        using (var ms = new MemoryStream())
        {
            ms.WriteByte(0x00);  // Type
            ms.WriteByte((byte)timeBytes.Length);  // TimeLen
            ms.Write(timeBytes, 0, timeBytes.Length);  // Time
            ms.Write(BitConverter.GetBytes(msgType), 0, 4);  // MsgType
            ms.Write(BitConverter.GetBytes(jsonBytes.Length), 0, 4);  // JsonLen
            ms.Write(jsonBytes, 0, jsonBytes.Length);  // Json
            
            byte[] msg = ms.ToArray();
            
            Console.WriteLine("│ [发送] " + msg.Length + " 字节");
            Console.WriteLine("│ [HEX ] " + BitConverter.ToString(msg).Replace("-", " "));
            
            if (server.Send(connId, msg, msg.Length))
            {
                Console.WriteLine("│ [发送] 成功!");
            }
            else
            {
                Console.WriteLine("│ [发送] 失败: " + server.ErrorMessage);
            }
        }
    }
    
    /// <summary>
    /// 发送简单确认
    /// </summary>
    static void SendSimpleAck(IntPtr connId)
    {
        var responseJson = new Dictionary<string, object>
        {
            { "code", 0 },
            { "msg", "ok" }
        };
        
        var serializer = new JavaScriptSerializer();
        string jsonStr = serializer.Serialize(responseJson);
        byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonStr);
        
        string timeStr = DateTime.Now.ToString("yyyy年M月d日H时m分s秒");
        byte[] timeBytes = Encoding.GetEncoding("GB2312").GetBytes(timeStr);
        
        using (var ms = new MemoryStream())
        {
            ms.WriteByte(0x00);
            ms.WriteByte((byte)timeBytes.Length);
            ms.Write(timeBytes, 0, timeBytes.Length);
            ms.Write(BitConverter.GetBytes(0), 0, 4);
            ms.Write(BitConverter.GetBytes(jsonBytes.Length), 0, 4);
            ms.Write(jsonBytes, 0, jsonBytes.Length);
            
            byte[] msg = ms.ToArray();
            server.Send(connId, msg, msg.Length);
            Console.WriteLine("│ [发送] 简单确认 " + msg.Length + " 字节");
        }
    }
}
