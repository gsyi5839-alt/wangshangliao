# xclient协议破解报告

## 日期: 2026-01-11

## 1. 协议概述

xclient是旺商聊Electron应用的核心通信组件，运行在端口21303。

### 1.1 架构

```
旺商聊 Electron App
    │
    ├── main process (main/index.js)
    │       │
    │       ├── xclient.node (Node原生模块)
    │       │       ├── xinit() - 初始化
    │       │       ├── xsync() - 同步解码
    │       │       ├── xtask() - 异步任务
    │       │       └── xquit() - 退出
    │       │
    │       └── IPC Handler ("xclient")
    │
    └── renderer process (zh-cn-*.js)
            │
            └── ipcRenderer.send("xclient", {...})
```

### 1.2 端口

| 端口 | 用途 | 协议 |
|------|------|------|
| 21303 | IPC通信 | 自定义二进制 |
| 21308 | HTTP服务 | HTTP (404) |

## 2. 协议格式

### 2.1 请求格式

```
[Type:1字节] [Flags:1字节] [Length:4字节BE] [JSON数据]
```

| 字段 | 大小 | 说明 |
|------|------|------|
| Type | 1 byte | 消息类型 (1=请求) |
| Flags | 1 byte | 标志 (0) |
| Length | 4 bytes | JSON长度 (大端序) |
| JSON | 变长 | JSON负载 |

### 2.2 响应格式

```
[Type:1字节] [Flags:1字节] [Length:4字节BE] [Payload]
```

| Type值 | 含义 |
|--------|------|
| 5 | 错误响应 |
| 其他 | 待确认 |

### 2.3 IPC消息格式

```javascript
{
    type: "request",      // 类型: request, decodes, buildin, broadcast
    requestId: "uuid",    // 请求ID
    url: "/v1/xxx",       // API路径
    excuteType: 0,        // 0=普通, 2=登录
    params: "{}",         // JSON字符串参数
    key: "request"        // 响应key
}
```

## 3. 已发现的API

### 3.1 用户相关

| API | 路径 |
|-----|------|
| 登录 | `/v1/user/login` |
| 登出 | `/v1/user/logout` |
| 更新会话 | `/v1/user/update-session` |
| 刷新Token | `/v1/user/RefreshToken` |
| 获取自动回复状态 | `/v1/user/get-auto-replies-online-state` |

### 3.2 好友相关

| API | 路径 |
|-----|------|
| 获取好友列表 | `/v1/friend/get-friend-list` |

### 3.3 设置相关

| API | 路径 |
|-----|------|
| 设置头像 | `/v1/settings/avatar` |
| 设置昵称 | `/v1/settings/self-nick-name` |
| 查询应用设置 | `/v1/settings/query-app-settings` |
| 设置自动回复 | `/v1/settings/set-auto-reply` |
| 获取系统设置 | `/v1/settings/get-system-setting` |
| 设置通知状态 | `/v1/settings/set-notify-state` |
| 设置会话置顶 | `/v1/settings/set-session-top` |
| 设置P2P铃声 | `/v1/settings/ring-p2p` |
| 设置群组铃声 | `/v1/settings/ring-group` |

## 4. 初始化密钥

xclient需要KEYS进行初始化：

```javascript
KEYS = {
    development: "AgAAAAA...",
    staging: "AQAAAAA...",
    production: "AQAAAAA..."  // 生产环境
}

// 初始化调用
xinit(KEYS[appEnv])
```

## 5. 当前状态

### 5.1 已验证

- ✅ 协议格式: Type + Flags + Length(BE) + JSON
- ✅ 端口: 21303
- ✅ Type=1 请求有响应
- ✅ Type=5 表示错误

### 5.2 待解决

- ⚠️ 独立连接返回Type=5错误
- ⚠️ 需要先通过xinit初始化
- ⚠️ 可能需要认证token

### 5.3 解决方案

1. **方案A: 注入到旺商聊进程**
   - 修改preload.js暴露API
   - 使用CDP调试协议

2. **方案B: 复用已初始化的xclient**
   - 模拟旺商聊主进程的IPC消息
   - 需要获取正确的认证凭证

3. **方案C: 完全逆向xclient.node**
   - 分析xinit的验证逻辑
   - 生成有效的初始化参数

## 6. C#实现

已创建 `XClientProtocol.cs`:

```csharp
// 发送请求
var (success, response) = await XClientProtocol.SendRequestAsync(
    ApiUrl.GetFriendList,
    new { }
);

// 检查xclient状态
if (XClientProtocol.IsXClientRunning())
{
    // xclient正在运行
}
```

## 7. 下一步

1. 分析xclient.node的xinit验证逻辑
2. 尝试通过CDP注入代码
3. 或修改旺商聊的preload.js
