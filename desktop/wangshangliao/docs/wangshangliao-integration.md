### 旺商聊集成：CDP + `window.nim`（推荐路线）

你要的核心能力（私聊/群聊发送 + 全体禁言/全体解禁）在旺商聊 Electron 渲染进程里已经由 **`window.nim`** 暴露出来；我们通过 **CDP（remote debugging port）** 注入 JS 调用它，比“找输入框点发送”稳定得多。

---

### 1) 必备前提：旺商聊必须开启调试端口

本机探测到旺商聊的启动参数类似：
- `--remote-debugging-port=9333`

你也可以在 Windows 上用 PowerShell 查看：
- `Get-CimInstance Win32_Process -Filter "name='wangshangliao_win_online.exe'" | select ProcessId,CommandLine`

---

### 2) 关键参数：群聊 teamId = groupCloudId

从 `out/wangshangliao-session-*.json`（脚本 `probe_wangshangliao_session.ps1` 导出）可以看到：
- **群聊**：`scene = "team"`，`to = "21654357327"`（就是 **groupCloudId/teamId**）
- **会话 id**：`id = "team-21654357327"`

因此你在代码里“全体禁言/解禁”的输入参数应该用 **groupCloudId**（字符串）。

---

### 3) 已集成到 `ChatService` 的方法（C#）

文件：`src/WangShangLiaoBot/Services/ChatService.cs`

- **私聊/群聊发送（SDK 发送）**
  - `SendTextAsync(string scene, string to, string text)`
  - `SendTextToCurrentSessionAsync(string text)`（自动从 Pinia currentSession 取 `scene/to`）

- **全体禁言/全体解禁（SDK 发送）**
  - `MuteAllByGroupCloudIdAsync(string groupCloudId)`（推荐集成用：不依赖当前打开群）
  - `UnmuteAllByGroupCloudIdAsync(string groupCloudId)`

> 说明：项目里原来的 `SendMessageAsync(string content)` 是 DOM/按键方式发送；新方法是 **`window.nim.sendText`** 发送。

---

### 4) 回归测试脚本（PowerShell）

这些脚本会真实调用旺商聊并产生实际效果（请在测试群/测试号上用）：

- **发消息**
  - `test_nim_sendtext.ps1`
  - 示例：
    - `./test_nim_sendtext.ps1 -Port 9333 -Scene team -To 21654357327 -Text "hello"`

- **全体禁言/解禁**
  - `test_nim_mute_teamall.ps1`
  - 示例：
    - `./test_nim_mute_teamall.ps1 -Port 9333 -TeamId 21654357327 -Mute $true`
    - `./test_nim_mute_teamall.ps1 -Port 9333 -TeamId 21654357327 -Mute $false`

---

### 5) 功能结构“地图”产物（用于继续扩展功能）

都在 `out/`：
- `wangshangliao-api-*.json`：全量 API 枚举（含 `window.nim` 与 Pinia stores）
- `wangshangliao-structure-*.json`：路由表 + 可点击元素样本 + nim 关键签名
- `wangshangliao-route-map-*.json`：自动导航 13 个 `/home/*` 路由的按钮/标签统计（用于功能分类）


