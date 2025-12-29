# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 开发规范

- **始终使用中文回复**
- **不要写兼容性代码** - 直接修改代码，不需要考虑向后兼容，不保留旧的实现
- **代码完整性** - 把代码写全补全，不要省略或用占位符
- **注释规范** - 为所有功能代码写上英文注释
- **文件大小限制** - 单个文件不能超过500行代码，超过时应拆分为多个文件
- **代码管理** - 保持代码结构清晰，避免混乱

## Project Overview

This is a chat automation framework that extends EasyHook (a Windows API hooking library) to build chat bot automation tools. The repository contains:

1. **EasyHook Library** (`EasyHook/`) - The core managed C# API hooking library (MIT licensed)
2. **ChatAutoBot Suite** - A chat automation framework with two approaches:
   - **EasyHook Injection** - Traditional DLL injection for hooking native Windows APIs
   - **Electron/CDP Mode** - Chrome DevTools Protocol for automating Electron-based chat apps
3. **WangShangLiaoBot** (`src/WangShangLiaoBot/`) - A WinForms application for chat automation

## Build Commands

```bash
# Build main solution (Visual Studio 2022 required)
build.bat

# Or manually with MSBuild
msbuild WangShangLiaoBot.sln /t:Rebuild /p:Configuration=Debug

# Build ChatAutoBot solution
msbuild ChatAutoBot.sln /t:Rebuild /p:Configuration=Debug
```

**Prerequisites:**
- Visual Studio 2022 with .NET Framework 4.7.2
- Windows SDK

## Architecture

### EasyHook Core Library (EasyHook/)

The managed C# wrapper over native hooking functionality:

- **LocalHook.cs** - Creates hooks for functions in the current process
  - `LocalHook.Create(targetAddress, hookDelegate, callback)` - Install a hook
  - `hook.ThreadACL.SetExclusiveACL()` - Control which threads are intercepted
- **RemoteHook.cs** - Injects hooks into remote processes
  - `RemoteHooking.Inject(pid, dllPath, channelName)` - Inject into existing process
  - `RemoteHooking.CreateAndInject(exePath, ...)` - Launch and inject into new process
  - `IEntryPoint` interface - Implement in injected DLLs for entry point

### ChatAutoBot Components

**ChatAutoBot** (`Examples/ChatAutoBot/`):
- Controller application that manages injection and IPC
- Supports two modes: EasyHook injection and Electron/CDP

**ChatAutoBotInject** (`Examples/ChatAutoBotInject/`):
- The injected DLL implementing `IEntryPoint`
- Hooks network APIs (`send`, `recv`, `WSASend`, `WSARecv`) and UI APIs (`SendMessageW`)
- Communicates with controller via IPC

**ChatAutoBotInterface** (`Examples/ChatAutoBotInterface/`):
- Shared types: `ChatMessage`, `AutoReplyRule`, `BroadcastTask`
- IPC interface `IChatBotController` for host-to-injected communication

### ElectronInjector (ChatAutoBot/ElectronInjector.cs)

For Electron apps (like WangShangLiao), uses Chrome DevTools Protocol:
- Connects via WebSocket to `--remote-debugging-port=9222`
- Executes JavaScript to hook message events
- Supports auto-reply, batch messaging, keyword-based replies

### WangShangLiaoBot (src/WangShangLiaoBot/)

WinForms application with:
- **Forms/**: LoginForm, MainForm, SettingsForm, ScoreForm
- **Services/**: ConfigService, ChatService, AutoReplyService, LotteryService
- **Models/**: AppConfig, Player, ChatMessage, BotAccount
- **Controls/**: Various settings controls (BillSendSettings, GroupTaskSettings, etc.)

## Key Patterns

### Creating an EasyHook-based hook:

```csharp
// 1. Define delegate matching the target function signature
[UnmanagedFunctionPointer(CallingConvention.StdCall)]
delegate int SendDelegate(IntPtr socket, IntPtr buffer, int length, int flags);

// 2. Import the original function
[DllImport("ws2_32.dll")]
static extern int send(IntPtr socket, IntPtr buffer, int length, int flags);

// 3. Create hook with your callback
var hook = LocalHook.Create(
    LocalHook.GetProcAddress("ws2_32.dll", "send"),
    new SendDelegate(MyHookCallback),
    this);

// 4. Configure thread ACL (exclude current thread from hooking)
hook.ThreadACL.SetExclusiveACL(new int[] { 0 });
```

### IPC Communication Pattern:

```csharp
// Host side - create IPC server
RemoteHooking.IpcCreateServer<ChatBotControllerBase>(
    ref channelName, WellKnownObjectMode.Singleton, controllerInstance);

// Injected side - connect to IPC server
var controller = RemoteHooking.IpcConnectClient<ChatBotControllerBase>(channelName);
controller.OnMessageReceived(message);
```

### Implementing IEntryPoint for injection:

```csharp
public class EntryPoint : IEntryPoint
{
    public EntryPoint(RemoteHooking.IContext context, string channelName)
    {
        // Constructor - connect to host IPC
    }

    public void Run(RemoteHooking.IContext context, string channelName)
    {
        // Main logic - install hooks, run message loop
        RemoteHooking.WakeUpProcess(); // Required if using CreateAndInject
    }
}
```

## Project Configuration

- Target Framework: .NET Framework 4.7.2
- EasyHook requires `AllowUnsafeBlocks: true`
- EasyHook assembly is strong-name signed (StrongName.snk)
- Build configurations: Debug, netfx3.5-Debug, netfx4-Release, etc.
