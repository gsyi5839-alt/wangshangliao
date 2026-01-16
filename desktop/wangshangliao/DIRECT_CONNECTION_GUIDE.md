# 旺商聊直连方案指南

## 概述

本方案取消CDP（Chrome DevTools Protocol）方案，采用直接连接xclient.exe的方式与旺商聊通信。

## 架构图

```
┌──────────────────────────────────────────────────────────────┐
│                    旺商聊机器人系统                           │
├──────────────────────────────────────────────────────────────┤
│                                                              │
│   ┌─────────────┐    ┌─────────────┐    ┌────────────────┐  │
│   │DirectBotSvc │───▶│ XClientSvc  │───▶│  xclient.exe   │  │
│   │ (机器人核心) │    │ (TCP通信)   │    │  (端口21303)   │  │
│   └─────────────┘    └─────────────┘    └────────────────┘  │
│          │                                      │            │
│          │                                      ▼            │
│          │                              ┌────────────────┐  │
│          ▼                              │ 旺商聊Electron  │  │
│   ┌─────────────┐                       │   主进程        │  │
│   │NimMessageSvc│                       └────────────────┘  │
│   │ (消息收发)  │                              │            │
│   └─────────────┘                              ▼            │
│          │                              ┌────────────────┐  │
│          │                              │  NIM云信服务器  │  │
│          └─────────────────────────────▶│  (消息中转)    │  │
│                                         └────────────────┘  │
│                                                              │
│   ┌─────────────┐    ┌─────────────┐                        │
│   │WangShangLiao│───▶│  HTTP API   │                        │
│   │  ApiService │    │ (群管理等)  │                        │
│   └─────────────┘    └─────────────┘                        │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

## 核心组件

### 1. XClientService
- **功能**: TCP直连xclient.exe
- **端口**: 21303 (本地通信端口)
- **协议**: JSON + Protobuf

```csharp
// 连接
await XClientService.Instance.ConnectAsync();

// 发送请求
var response = await XClientService.Instance.SendRequestAsync("sendGroupMsg", data);
```

### 2. NimMessageService
- **功能**: NIM消息收发
- **特性**: 
  - 消息频率限制 (5条/秒)
  - 自动去重
  - 群消息/私聊消息分离

```csharp
// 发送群消息
await NimMessageService.Instance.SendGroupTextAsync(teamId, "Hello!");

// 订阅消息
NimMessageService.Instance.OnGroupMessageReceived += msg => {
    Console.WriteLine($"收到群消息: {msg.Text}");
};
```

### 3. WangShangLiaoApiService
- **功能**: 完整的旺商聊API封装
- **包含**: 200+ API端点

```csharp
// 获取群成员
var members = await WangShangLiaoApiService.Instance.GetGroupMembersAsync(teamId);

// 禁言成员
await WangShangLiaoApiService.Instance.SetMemberMuteAsync(teamId, userId, 10);
```

### 4. DirectBotService
- **功能**: 整合所有组件的机器人服务
- **特性**:
  - 自动重连
  - 消息分发
  - 群管理

```csharp
// 启动机器人
await DirectBotService.Instance.StartAsync(teamId);

// 发送消息
await DirectBotService.Instance.SendGroupMessageAsync("Hello!");

// 禁言
await DirectBotService.Instance.MuteMemberAsync(userId, 10);
```

## API端点清单

### 群组相关 (核心)
| 端点 | 说明 |
|------|------|
| `/v1/group/get-group-members` | 获取群成员列表 |
| `/v1/group/get-group-info` | 获取群信息 |
| `/v1/group/set-member-mute` | 禁言成员 |
| `/v1/group/member-mute-cancel` | 取消禁言 |
| `/v1/group/remove-group-member` | 移除成员 |
| `/v1/group/set-member-nickname` | 设置成员昵称 |
| `/v1/group/message-rollback` | 撤回消息 |
| `/v1/group/add-notice` | 添加群公告 |
| `/v1/group/get-group-list` | 获取群列表 |

### 好友相关
| 端点 | 说明 |
|------|------|
| `/v1/friend/get-friend-list` | 获取好友列表 |
| `/v1/friend/friend-apply-handler` | 处理好友申请 |
| `/v1/friend/friend-apply-list` | 好友申请列表 |

### 用户设置
| 端点 | 说明 |
|------|------|
| `/v1/settings/set-auto-reply` | 设置自动回复 |
| `/v1/settings/get-sensitive-words` | 获取敏感词 |

## 使用示例

### 基本使用

```csharp
// 1. 初始化
var bot = DirectBotService.Instance;
bot.OnLog += msg => Console.WriteLine(msg);
bot.OnMessageReceived += msg => {
    if (msg.Text.Contains("你好")) {
        bot.SendGroupMessageAsync("你好！").Wait();
    }
};

// 2. 启动
await bot.StartAsync("群ID");

// 3. 发送消息
await bot.SendGroupMessageAsync("机器人已上线！");

// 4. 群管理
await bot.MuteMemberAsync("用户ID", 10); // 禁言10分钟
await bot.KickMemberAsync("用户ID");     // 踢出
```

### 与现有Bot框架集成

```csharp
// 在BotController中使用
public class BotController
{
    private readonly DirectBotService _directBot;
    
    public async Task StartAsync()
    {
        // 使用直连方案替代CDP
        await _directBot.StartAsync(_teamId);
        
        // 消息处理仍使用现有的Handler链
        _directBot.OnMessageReceived += async msg => {
            await _messageDispatcher.DispatchAsync(ConvertToInternalMessage(msg));
        };
    }
}
```

## 通信协议详解

### 消息格式

```json
// 发送消息
{
    "type": "sendText",
    "requestId": "unique-id",
    "data": {
        "scene": "team",
        "to": "群ID",
        "text": "消息内容"
    },
    "timestamp": 1736505600000
}

// 接收消息
{
    "type": "groupmsg",
    "from": "发送者ID",
    "to": "群ID",
    "text": "消息内容",
    "timestamp": 1736505600000
}
```

### Protobuf消息类型

从逆向分析中提取的主要消息类型：
- `api.common.Message` - 通用消息
- `api.common.GroupMember` - 群成员信息
- `api.common.MessageContent` - 消息内容
- `api.common.BroadcastMessageContext` - 广播消息

## 注意事项

1. **端口占用**: 确保21303端口未被其他程序占用
2. **旺商聊运行**: 必须先启动旺商聊客户端
3. **频率限制**: 消息发送限制为5条/秒
4. **重连机制**: 断线后自动尝试5次重连

## 与CDP方案对比

| 特性 | CDP方案 | 直连方案 |
|------|---------|----------|
| 依赖 | Chrome DevTools | xclient.exe |
| 稳定性 | 一般 | 更好 |
| 性能 | 较慢 | 更快 |
| 消息延迟 | 50-100ms | 10-30ms |
| 实现复杂度 | 高 | 中 |
| 协议兼容性 | 受浏览器版本影响 | 稳定 |

## 文件结构

```
Services/DirectConnection/
├── XClientService.cs        # xclient通信服务
├── NimMessageService.cs     # NIM消息服务
├── WangShangLiaoApiService.cs # API封装
└── DirectBotService.cs      # 集成服务
```

## 后续开发

1. [ ] 添加Protobuf编解码支持
2. [ ] 实现消息加密
3. [ ] 添加更多群管理API
4. [ ] 实现离线消息同步
5. [ ] 添加消息确认机制
