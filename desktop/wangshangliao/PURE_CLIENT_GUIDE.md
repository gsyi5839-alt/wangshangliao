# 旺商聊纯C#客户端指南

## 概述

本指南介绍如何使用完全逆向实现的纯C#客户端连接旺商聊，无需依赖Electron、CDP或任何外部进程。

## 架构

```
┌─────────────────────────────────────────────────────────────┐
│                    WslPureClient                            │
│  (纯C#实现的旺商聊客户端)                                    │
├─────────────────────────────────────────────────────────────┤
│  WslTcpProtocol     │  WslProtobuf     │  WslCrypto         │
│  (TCP协议层)        │  (消息编解码)    │  (AES-256-GCM)     │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
                    ┌──────────────────┐
                    │   xclient.exe    │
                    │   (端口21303)    │
                    └──────────────────┘
                              │
                              ▼
                    ┌──────────────────┐
                    │  NIM云信服务器   │
                    └──────────────────┘
```

## 文件说明

| 文件 | 说明 |
|------|------|
| `WslCrypto.cs` | AES-256-GCM 加密/解密、密钥解析、HKDF派生 |
| `WslProtobuf.cs` | Protobuf消息编码/解码 (api.common.Message) |
| `WslTcpProtocol.cs` | TCP协议层实现 (数据包序列化/反序列化) |
| `WslPureClient.cs` | 高级客户端API (群组、好友、设置) |
| `WslPureClientTest.cs` | 测试程序 |

## 快速开始

### 1. 基本使用

```csharp
using WangShangLiaoBot.Services.PureClient;

// 创建客户端
using var client = new WslPureClient("production");

// 注册事件
client.OnGroupMessage += (groupId, senderId, message) =>
{
    Console.WriteLine($"群消息 [{groupId}] {senderId}: {message}");
};

client.OnPrivateMessage += (senderId, message) =>
{
    Console.WriteLine($"私聊 {senderId}: {message}");
};

// 连接
await client.ConnectAsync();

// 登录 (如果需要)
await client.LoginAsync("账号", "密码");

// 发送群消息
await client.SendGroupMessageAsync("群ID", "Hello!");

// 发送私聊消息
await client.SendPrivateMessageAsync("好友ID", "Hello!");
```

### 2. 群组操作

```csharp
// 获取群列表
var groups = await client.GetGroupListAsync();

// 获取群成员
var members = await client.GetGroupMembersAsync("群ID");

// 禁言成员 (300秒)
await client.MuteGroupMemberAsync("群ID", "成员ID", 300);

// 踢出成员
await client.KickGroupMemberAsync("群ID", "成员ID");

// 撤回消息
await client.RecallGroupMessageAsync("群ID", "消息ID");

// 设置群公告
await client.SetGroupNoticeAsync("群ID", "标题", "内容");
```

### 3. 好友操作

```csharp
// 获取好友列表
var friends = await client.GetFriendListAsync();

// 发送私聊
await client.SendPrivateMessageAsync("好友ID", "消息内容");

// 处理好友申请
await client.HandleFriendApplyAsync("申请ID", true); // 接受
await client.HandleFriendApplyAsync("申请ID", false); // 拒绝
```

### 4. 设置操作

```csharp
// 获取系统设置
var settings = await client.GetSystemSettingsAsync();

// 设置自动回复
await client.SetAutoReplyAsync("我正在忙，稍后回复", true);

// 获取敏感词
var sensitiveWords = await client.GetSensitiveWordsAsync();
```

## 密钥结构

从逆向分析中提取的环境密钥结构：

```
格式: Part1.Part2.Part3 (Base64URL编码)

Part1 (104 bytes):
├── 头8字节: 版本号 (int64) = 1
└── 后96字节: AppKey数据

Part2 (104 bytes):
├── 头8字节: 版本号 (int64) = 5
└── 后96字节: Token数据

Part3 (385 bytes):
├── 前32字节: 可能是AES-256加密密钥
├── 接下来12字节: 可能是基础Nonce
└── 剩余341字节: 签名/认证数据
```

## 协议格式

### 数据包结构

```
┌────────────────────────────────────────────────────────────┐
│                        Header (16 bytes)                   │
├────────────────────────────────────────────────────────────┤
│ Magic (2) │ Ver (1) │ Type (1) │ Flags (1) │ Reserved (3) │
├────────────────────────────────────────────────────────────┤
│              SequenceId (4)        │   DataLength (4)     │
├────────────────────────────────────────────────────────────┤
│                        Data (可变长度)                     │
│                   (可能加密: AES-256-GCM)                  │
└────────────────────────────────────────────────────────────┘
```

### 消息类型

| 类型值 | 名称 | 说明 |
|--------|------|------|
| 0x01 | REQUEST | API请求 |
| 0x02 | RESPONSE | API响应 |
| 0x03 | PUSH | 推送消息 |
| 0x04 | HEARTBEAT | 心跳 |
| 0x05 | HANDSHAKE | 握手 |
| 0x06 | ENCRYPT | 加密请求 |
| 0x07 | DECRYPT | 解密请求 |

## 加密算法

### AES-256-GCM

- **密钥长度**: 32字节 (256位)
- **Nonce长度**: 12字节 (96位)
- **Tag长度**: 16字节 (128位)
- **输出格式**: `Nonce (12) + Tag (16) + Ciphertext`

```csharp
// 加密
var nonce = WslCrypto.GenerateNonce();
var encrypted = WslCrypto.Encrypt(plaintext, key, nonce);

// 解密 (自动从数据中提取nonce和tag)
var decrypted = WslCrypto.Decrypt(encrypted, key);
```

## Protobuf消息

### api.common.Message 字段

| 字段号 | 名称 | 类型 | 说明 |
|--------|------|------|------|
| 1 | sendType | int32 | 发送类型 |
| 2 | from | int64 | 发送者ID |
| 3 | isOffline | bool | 是否离线消息 |
| 4 | ttl | int32 | 存活时间 |
| 5 | targetOs | string | 目标OS |
| 6 | body | bytes | 消息体 |
| 7 | encrypt | bool | 是否加密 |
| 9 | nimList | repeated string | NIM列表 |

## 运行测试

```csharp
// 在程序入口点运行测试
await WslPureClientTest.RunTestAsync();
```

输出示例：
```
╔══════════════════════════════════════════════════════════╗
║      旺商聊纯C#客户端测试程序 v1.0                       ║
╚══════════════════════════════════════════════════════════╝

=== 测试1: 密钥解析 ===
  Version1: 1
  Version2: 5
  AppKey长度: 96 bytes
  Token长度: 96 bytes
  EncryptionKey: 45C446C449C57C5811CCD92013E56783...
  ✓ 密钥解析成功

=== 测试2: AES-256-GCM 加密/解密 ===
  原文: Hello, 旺商聊!
  密钥长度: 32 bytes
  Nonce长度: 12 bytes
  密文长度: 45 bytes
  解密: Hello, 旺商聊!
  ✓ 加密/解密成功

=== 测试3: Protobuf 编解码 ===
  原始消息: SendType=0, From=12345
  编码后: 25 bytes
  Hex: 08-00-10-B9-60-18-00-20-AC-02-2A-05-77-69-6E-33-32-32-...
  解码后: SendType=0, From=12345
  ✓ Protobuf编解码成功

=== 测试4: TCP连接 (需要xclient运行) ===
  正在连接到 127.0.0.1:21303...
  ✓ TCP连接成功
  ✓ 连接成功

=== 测试完成 ===
```

## 注意事项

1. **运行要求**: 需要旺商聊客户端和xclient.exe正在运行
2. **端口**: 默认连接到 `127.0.0.1:21303`
3. **加密**: 所有敏感数据使用AES-256-GCM加密
4. **心跳**: 客户端每30秒自动发送心跳
5. **超时**: API请求默认30秒超时

## 错误处理

```csharp
client.OnError += errorMessage =>
{
    Console.WriteLine($"错误: {errorMessage}");
    // 处理错误，如重连
};

client.OnDisconnected += () =>
{
    Console.WriteLine("断开连接，尝试重连...");
    Task.Delay(5000).ContinueWith(_ => client.ConnectAsync());
};
```

## 进一步开发

如需完全独立运行(不依赖xclient)，需要：

1. 逆向xclient.exe的完整协议
2. 实现与NIM云信服务器的直接通信
3. 处理WebSocket长连接和消息推送

---

*文档生成时间: 2026-01-10*
