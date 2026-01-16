# 第三方库说明

## 1. HPSocket - 高性能网络通信框架

### 源码仓库
- **位置:** `libs/HP-Socket/`
- **GitHub:** https://github.com/ldcsaa/HP-Socket
- **官网:** https://hpsocket.cn/

### C# NuGet 包
- **位置:** `src/WangShangLiaoBot/packages/HPSocket.Net.6.0.7.1/`
- **NuGet:** https://www.nuget.org/packages/HPSocket.Net
- **版本:** 6.0.7.1

### 支持的 .NET 版本
- .NET Framework 4.0+
- .NET Core 2.0+
- .NET 6.0/7.0/8.0/9.0

### 使用方法

#### 在项目中引用
在 `WangShangLiaoBot.csproj` 中添加引用:
```xml
<Reference Include="HPSocket.Net">
  <HintPath>packages\HPSocket.Net.6.0.7.1\lib\net48\HPSocket.Net.dll</HintPath>
</Reference>
```

#### 基本示例
```csharp
using HPSocket;
using HPSocket.Tcp;

// TCP 客户端示例
using (var client = new TcpClient())
{
    client.OnConnect += (sender) => {
        Console.WriteLine("已连接");
        return HandleResult.Ok;
    };
    
    client.OnReceive += (sender, data) => {
        Console.WriteLine($"收到数据: {Encoding.UTF8.GetString(data)}");
        return HandleResult.Ok;
    };
    
    client.Connect("127.0.0.1", 14745);
}

// TCP 服务端示例
using (var server = new TcpServer())
{
    server.OnAccept += (sender, connId, client) => {
        Console.WriteLine($"新连接: {connId}");
        return HandleResult.Ok;
    };
    
    server.OnReceive += (sender, connId, data) => {
        // 处理数据
        server.Send(connId, data, data.Length);
        return HandleResult.Ok;
    };
    
    server.Start("0.0.0.0", 14745);
}
```

### 主要功能
- TCP/UDP Server/Client/Agent
- SSL/TLS 支持
- HTTP/HTTPS 支持
- WebSocket 支持
- Pack 模式（自动分包）
- Pull 模式（拉取数据）

---

## 2. QX(千寻)框架

### ⚠️ 重要说明
**QX(千寻)框架是闭源商业框架，没有公开的 GitHub 仓库。**

该框架是专门为旺商聊/类似IM机器人设计的插件框架，主要功能包括:
- 36个核心API函数 (群操作、好友操作、红包转账等)
- 50+消息回调函数
- 插件热加载机制
- 与旺商聊客户端的本地通信 (端口 14745)

### QX框架API (根据逆向分析)
详细API列表请参考:
- `C:\Users\Administrator\Desktop\zcg25.12.11\源代码恢复方案\招财狗源代码恢复报告.md`
- `C:\Users\Administrator\Desktop\zcg25.12.11\逆向分析结果\QX框架接口文档.md`

### 替代方案
由于QX框架闭源，建议使用以下方式实现相同功能:
1. **Chrome DevTools Protocol (CDP)** - 当前项目使用的方案
2. **HPSocket** - 直接与旺商聊客户端通信
3. **网易云信 NIM SDK** - 直接调用IM接口

---

## 文件结构

```
libs/
├── HP-Socket/               # HPSocket 完整源码
│   ├── Doc/                 # 文档和示例图
│   ├── Windows/             # Windows 版本
│   │   ├── Demo/            # 示例代码
│   │   ├── Include/         # 头文件
│   │   └── Src/             # 源代码
│   ├── Linux/               # Linux 版本
│   └── DotNet/              # .NET 版本说明
├── nuget.exe                # NuGet CLI 工具
└── README.md                # 本文件

src/WangShangLiaoBot/packages/
└── HPSocket.Net.6.0.7.1/    # C# NuGet 包
    └── lib/
        ├── net40/           # .NET Framework 4.0
        ├── net48/           # .NET Framework 4.8 (推荐)
        ├── net6.0/          # .NET 6.0
        └── ...
```

---

*更新时间: 2026-01-11*
