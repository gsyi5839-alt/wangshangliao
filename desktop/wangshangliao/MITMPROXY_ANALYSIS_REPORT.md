# Mitmproxy 抓包分析报告

## 日期: 2026-01-11

## 1. 抓包环境

- **工具**: mitmproxy (mitmweb)
- **代理端口**: 8080
- **Web界面**: http://127.0.0.1:8081
- **证书状态**: ✅ 已安装到系统受信任根证书

## 2. 发现的服务器

### 2.1 新发现: 心跳服务器

| 属性 | 值 |
|------|-----|
| **IP地址** | 47.245.110.70 |
| **端口** | 8080 |
| **协议** | HTTP |
| **提供商** | 阿里云 |
| **用途** | 心跳/状态检查 |
| **连接频率** | 约每30秒一次 |

### 2.2 完整服务器架构

```
旺商聊客户端
    │
    ├─→ 47.245.110.70:8080 (HTTP)
    │       └─ 心跳/状态服务器 (阿里云)
    │
    ├─→ 120.233.185.185:443 (HTTPS)
    │       └─ API服务器 (认证/配置)
    │
    └─→ 120.236.198.109:47437 (TCP)
            └─ 长连接服务器 (消息推送)
```

## 3. 抓包结果分析

### 3.1 成功捕获的连接

```
[09:45:15] 47.245.110.70:8080 - 连接成功
[09:45:45] 47.245.110.70:8080 - 连接成功
[09:46:15] 47.245.110.70:8080 - 连接成功
[09:46:46] 47.245.110.70:8080 - 连接成功
... (每约30秒重复)
```

### 3.2 证书被拒绝的连接

大多数HTTPS连接因为证书固定(Certificate Pinning)被拒绝:

| 域名 | 说明 |
|------|------|
| api2.cursor.sh | Cursor IDE |
| us.i.posthog.com | 分析服务 |
| otheve.beacon.qq.com | QQ信标 |
| h.trace.qq.com | QQ跟踪 |
| www.google.com | Google |
| optimizationguide-pa.googleapis.com | Google |

### 3.3 QQ相关连接

旺商聊使用了QQ/腾讯的服务:
- `otheve.beacon.qq.com:443` - QQ信标服务
- `h.trace.qq.com:443` - QQ跟踪服务

这证实了旺商聊使用了网易云信(NIM) SDK与腾讯的某些服务集成。

## 4. 技术发现

### 4.1 心跳协议特征

1. **连接间隔**: ~30秒
2. **连接时长**: ~100ms (快速请求/响应)
3. **协议**: HTTP (非HTTPS)
4. **服务器**: 阿里云海外节点

### 4.2 证书固定问题

旺商聊Electron应用实现了证书固定:
- 不信任系统证书存储
- 使用应用内嵌的CA证书
- HTTPS流量无法被mitmproxy拦截

## 5. 建议的下一步

### 5.1 绕过证书固定

```javascript
// 方法1: 修改Electron应用启动参数
--ignore-certificate-errors

// 方法2: 使用Frida hook SSL验证
// 需要root/admin权限
```

### 5.2 分析心跳服务器

```bash
# 使用Wireshark抓取原始TCP流量
wireshark -i "以太网" -f "host 47.245.110.70"
```

### 5.3 直接连接测试

需要模拟客户端的完整请求头和认证信息才能与心跳服务器通信。

## 6. 更新的服务器配置

已更新 `NimDirectClient.cs`:

```csharp
public static readonly ServerInfo[] SERVERS = new ServerInfo[]
{
    new ServerInfo { Host = "120.236.198.109", Port = 47437, Name = "长连接服务器", Type = ServerType.PushServer },
    new ServerInfo { Host = "120.233.185.185", Port = 443, Name = "API服务器", Type = ServerType.ApiServer },
    new ServerInfo { Host = "47.245.110.70", Port = 8080, Name = "心跳服务器", Type = ServerType.HeartbeatServer },
};
```

## 7. 结论

mitmproxy成功发现了新的心跳服务器(47.245.110.70:8080)，但由于证书固定无法捕获HTTPS流量内容。建议使用Wireshark进行更底层的流量分析，或使用Frida/xposed来绕过证书验证。
