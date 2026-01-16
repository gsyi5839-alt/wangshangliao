using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

class YxProxyTest
{
    [DllImport("HPSocket4C.dll", CallingConvention = CallingConvention.StdCall)]
    static extern IntPtr Create_HP_TcpPackServer(IntPtr pListener);
    
    [DllImport("HPSocket4C.dll", CallingConvention = CallingConvention.StdCall)]
    static extern IntPtr Create_HP_TcpPackServerListener();
    
    [DllImport("HPSocket4C.dll", CallingConvention = CallingConvention.StdCall)]
    static extern void HP_Set_FN_Server_OnAccept(IntPtr pListener, OnAcceptCallback fn);
    
    [DllImport("HPSocket4C.dll", CallingConvention = CallingConvention.StdCall)]
    static extern void HP_Set_FN_Server_OnReceive(IntPtr pListener, OnReceiveCallback fn);
    
    [DllImport("HPSocket4C.dll", CallingConvention = CallingConvention.StdCall)]
    static extern void HP_Set_FN_Server_OnClose(IntPtr pListener, OnCloseCallback fn);
    
    [DllImport("HPSocket4C.dll", CallingConvention = CallingConvention.StdCall)]
    static extern bool HP_Server_Start(IntPtr pServer, string lpszBindAddress, ushort usPort);
    
    [DllImport("HPSocket4C.dll", CallingConvention = CallingConvention.StdCall)]
    static extern bool HP_Server_Stop(IntPtr pServer);
    
    [DllImport("HPSocket4C.dll", CallingConvention = CallingConvention.StdCall)]
    static extern bool HP_Server_Send(IntPtr pServer, IntPtr dwConnID, byte[] pBuffer, int iLength);
    
    [DllImport("HPSocket4C.dll", CallingConvention = CallingConvention.StdCall)]
    static extern void HP_TcpPackServer_SetMaxPackSize(IntPtr pServer, uint dwMaxPackSize);
    
    [DllImport("HPSocket4C.dll", CallingConvention = CallingConvention.StdCall)]
    static extern void HP_TcpPackServer_SetPackHeaderFlag(IntPtr pServer, ushort usPackHeaderFlag);
    
    delegate int OnAcceptCallback(IntPtr pSender, IntPtr dwConnID, IntPtr pClient);
    delegate int OnReceiveCallback(IntPtr pSender, IntPtr dwConnID, byte[] pData, int iLength);
    delegate int OnCloseCallback(IntPtr pSender, IntPtr dwConnID, int enOperation, int iErrorCode);
    
    static IntPtr server;
    static IntPtr listener;
    
    static OnAcceptCallback onAccept;
    static OnReceiveCallback onReceive;
    static OnCloseCallback onClose;
    
    static int HandleAccept(IntPtr pSender, IntPtr dwConnID, IntPtr pClient)
    {
        Console.WriteLine("[代理] 客户端已连接: " + dwConnID.ToString());
        return 0;
    }
    
    static int HandleReceive(IntPtr pSender, IntPtr dwConnID, byte[] pData, int iLength)
    {
        string msg = Encoding.UTF8.GetString(pData, 0, iLength);
        Console.WriteLine("[代理] 收到数据 (" + iLength + "字节): " + msg);
        
        string response = "{\"code\":0,\"msg\":\"ok\"}";
        byte[] respBytes = Encoding.UTF8.GetBytes(response);
        HP_Server_Send(server, dwConnID, respBytes, respBytes.Length);
        
        return 0;
    }
    
    static int HandleClose(IntPtr pSender, IntPtr dwConnID, int enOperation, int iErrorCode)
    {
        Console.WriteLine("[代理] 客户端断开: " + dwConnID.ToString());
        return 0;
    }
    
    static void Main()
    {
        Console.WriteLine("HPSocket Pack服务器测试");
        Console.WriteLine("监听端口: 5749");
        
        onAccept = HandleAccept;
        onReceive = HandleReceive;
        onClose = HandleClose;
        
        listener = Create_HP_TcpPackServerListener();
        server = Create_HP_TcpPackServer(listener);
        
        HP_Set_FN_Server_OnAccept(listener, onAccept);
        HP_Set_FN_Server_OnReceive(listener, onReceive);
        HP_Set_FN_Server_OnClose(listener, onClose);
        
        HP_TcpPackServer_SetMaxPackSize(server, 0x1000000);
        HP_TcpPackServer_SetPackHeaderFlag(server, 0xFF);
        
        if (HP_Server_Start(server, "0.0.0.0", 5749))
        {
            Console.WriteLine("服务器启动成功! 等待621705120.exe连接...");
            Console.WriteLine("按任意键停止...");
            Console.ReadKey();
        }
        else
        {
            Console.WriteLine("服务器启动失败!");
        }
        
        HP_Server_Stop(server);
    }
}
