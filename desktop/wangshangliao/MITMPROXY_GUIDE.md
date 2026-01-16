# mitmproxy HTTPS抓包指南

## 快速开始

### 1. 安装mitmproxy

在命令行中运行：
```bash
pip install mitmproxy
```

### 2. 启动抓包

**方式A: 命令行模式**
```bash
双击运行: start_mitmproxy.bat
```

**方式B: Web可视化界面 (推荐)**
```bash
双击运行: start_mitmweb.bat
```

### 3. 设置系统代理

**Windows设置:**
1. 打开 "设置" → "网络和Internet" → "代理"
2. 开启 "使用代理服务器"
3. 地址: `127.0.0.1`
4. 端口: `8080`
5. 保存

**或者使用命令行:**
```powershell
# 设置代理
netsh winhttp set proxy 127.0.0.1:8080

# 取消代理
netsh winhttp reset proxy
```

### 4. 安装CA证书 (首次使用)

1. 设置好代理后，用浏览器访问: http://mitm.it
2. 点击 "Windows" 下载证书
3. 双击安装证书
4. 选择 "本地计算机" → "将所有证书放入下列存储" → "受信任的根证书颁发机构"

### 5. 开始抓包

1. 启动mitmproxy
2. 打开旺商聊客户端
3. 观察控制台输出或Web界面

## 文件说明

| 文件 | 说明 |
|------|------|
| `mitmproxy_capture.py` | 抓包脚本，过滤旺商聊相关流量 |
| `start_mitmproxy.bat` | 命令行模式启动脚本 |
| `start_mitmweb.bat` | Web界面模式启动脚本 |
| `captured_traffic/` | 抓包数据保存目录 |

## 目标服务器

脚本会自动过滤以下服务器的流量：
- `120.233.185.185` - API服务器 (HTTPS)
- `120.236.198.109` - 长连接服务器
- 包含 `qixin` 或 `wangshangliao` 的域名

## 输出格式

抓包数据保存为JSON格式：
```json
{
  "timestamp": "2026-01-11T...",
  "type": "request",
  "method": "POST",
  "url": "https://120.233.185.185/v1/user/login",
  "headers": {...},
  "body_json": {...}
}
```

## 手动运行命令

```bash
# 基础模式
mitmdump -s mitmproxy_capture.py -p 8080 --ssl-insecure

# Web界面
mitmweb -s mitmproxy_capture.py -p 8080 --ssl-insecure --web-port 8081

# 透明代理模式 (需要管理员权限)
mitmdump -s mitmproxy_capture.py -p 8080 --mode transparent --ssl-insecure
```

## 常见问题

### Q: 旺商聊无法连接
A: 检查代理设置是否正确，确认mitmproxy正在运行

### Q: HTTPS证书错误
A: 需要安装mitmproxy的CA证书，访问 http://mitm.it

### Q: 看不到流量
A: 确认旺商聊使用的是系统代理，而不是直连

## 抓包完成后

1. 关闭mitmproxy (Ctrl+C)
2. 取消系统代理设置
3. 查看 `captured_traffic/` 目录中的JSON文件
4. 分析API调用和握手流程
