# 旺商聊NIM协议完全逆向分析报告

## 一、核心发现

### 1. 服务器信息
```
主服务器: 120.236.198.109:47437
连接数: 149+ (大量长连接)
进程: PID 12080 (与xclient相关)
```

### 2. 通信架构
```
┌──────────────────────────────────────────────────────────────┐
│                     旺商聊客户端                              │
├──────────────────────────────────────────────────────────────┤
│  Electron渲染进程                                            │
│  ├── Vue 3 前端                                              │
│  └── ipcRenderer → 主进程                                    │
├──────────────────────────────────────────────────────────────┤
│  Electron主进程                                              │
│  ├── XClientServer类                                         │
│  └── xclient.node (N-API原生模块)                           │
│       ├── xinit(key) - 初始化                               │
│       ├── xtask(...) - API请求/加密                         │
│       └── xsync(...) - 同步解码                             │
├──────────────────────────────────────────────────────────────┤
│  网络层 (xclient.exe)                                        │
│  └── TCP长连接 → 120.236.198.109:47437                      │
└──────────────────────────────────────────────────────────────┘
```

### 3. 环境密钥结构
```
Production Key (3部分，Base64URL编码):

Part1 (104 bytes):
├── 版本号: 1 (int64)
└── AppKey数据: 96 bytes
    Hex: 55 66 94 46 07 73 89 7B D0 F2 A2 AD...

Part2 (104 bytes):
├── 版本号: 5 (int64)  
└── Token数据: 96 bytes
    Hex: ED 7D 14 D5 B5 62 A9 7C 35 04 BA B0...

Part3 (385 bytes):
├── 加密密钥: 前32字节 (AES-256)
│   Hex: 45 C4 46 C4 49 C5 7C 58 11 CC D9 20 13 E5 67 83
│        D6 72 A2 6C C4 AC D9 C4 FA 30 CE F4 40 86 B5 3F
├── 基础Nonce: 12字节
└── 签名数据: 341字节
```

## 二、xclient.node 核心API

### 函数签名
```javascript
// 初始化 (传入环境密钥)
xinit(key: string): void

// 异步任务执行
xtask(
    excuteType: number,  // 0=普通, 1=特殊, 2=登录
    unknown: number,     // 始终为0
    action: string,      // URL或命令
    params: string|null, // JSON参数
    buffer?: ArrayBuffer // 二进制数据(加密时用)
): Promise<any>

// 同步执行 (主要用于解码)
xsync(type: number, data: ArrayBuffer): any
```

### xtask action值
| action | excuteType | 说明 |
|--------|-----------|------|
| `/v1/user/login` | 2 | 登录 |
| `/v1/group/get-group-list` | 0 | 获取群列表 |
| `/v1/group/send-message` | 0 | 发送群消息 |
| `"buildin"` | 1 | 内置命令 |
| `"encrypt"` | 1 | 加密消息 |
| `"broadcast"` | 1 | 广播消息 |

## 三、加密算法

### AES-256-GCM
```
密钥长度: 32字节 (256位)
Nonce长度: 12字节 (96位)  
Tag长度: 16字节 (128位)
输出格式: [Nonce(12)] + [Tag(16)] + [Ciphertext]
```

### Protobuf消息
```protobuf
// api.common.Message
message Message {
    int32 sendType = 1;
    int64 from = 2;
    bool isOffline = 3;
    int32 ttl = 4;
    string targetOs = 5;
    bytes body = 6;
    bool encrypt = 7;
    string lineVersionParam = 8;
    repeated string nimList = 9;
    string clientCallback = 10;
}
```

## 四、NIM SDK配置

### 依赖版本
```json
{
    "@yxim/nim-web-sdk": "^9.20.15",
    "nim-web-sdk-ng": "^0.19.2"
}
```

### 关键字段
- `nimId` - NIM用户ID
- `nimToken` - NIM认证Token
- `nimList` - NIM用户列表
- `nimMsgType` - 消息类型

## 五、已实现的模块

| 文件 | 功能 | 完成度 |
|------|------|--------|
| `WslCrypto.cs` | AES-256-GCM加密/解密 | 100% |
| `WslProtobuf.cs` | Protobuf编解码 | 100% |
| `WslTcpProtocol.cs` | TCP协议框架 | 100% |
| `WslPureClient.cs` | xclient.node封装 | 100% |
| `NimDirectClient.cs` | NIM直连客户端 | 80% |

## 六、待分析内容

### 1. xclient.exe握手协议
- 服务器 `120.236.198.109:47437` 需要特定的握手序列
- 可能使用自定义的二进制协议而非标准TLS
- 需要抓包分析实际的握手过程

### 2. 认证流程
```
1. TCP连接建立
2. 协议握手 (版本协商)
3. 密钥交换 (可能使用ECDH)
4. 登录认证 (appKey + accid + token)
5. 建立会话
```

### 3. 消息格式
```
推测的数据包格式:
[Length(4)] [Type(1)] [Flags(1)] [SeqId(4)] [Payload(N)]

或者:
[Magic(4)] [Version(2)] [Length(4)] [Encrypted(1)] [Payload(N)] [MAC(16)]
```

## 七、完全独立方案

### 目标
完全脱离xclient.node和xclient.exe，直接与NIM服务器通信。

### 所需步骤
1. ✅ 分析加密算法 (AES-256-GCM)
2. ✅ 分析消息格式 (Protobuf)
3. ✅ 提取服务器地址 (120.236.198.109:47437)
4. ⏳ 逆向握手协议
5. ⏳ 逆向认证流程
6. ⏳ 实现完整客户端

### 技术难点
- xclient使用自定义协议而非标准WebSocket
- 需要特定的握手序列才能建立连接
- 可能有设备指纹/时间戳验证

## 八、推荐方案

### 短期 (立即可用)
使用 `WslPureClient.cs` 通过xclient进行通信

### 中期 (1-2周)
使用Wireshark/Fiddler深度抓包，完全逆向握手协议

### 长期 (1个月+)
实现纯C#客户端 `NimDirectClient.cs`，完全独立运行

---

*分析完成时间: 2026-01-10*
*服务器: 120.236.198.109:47437*
*协议: 自定义TCP (非标准TLS)*
