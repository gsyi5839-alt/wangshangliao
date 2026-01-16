@echo off
chcp 65001 >nul
title 旺商聊机器人 - 运行状态
color 0F

echo.
echo  ╔═══════════════════════════════════════════════════════════════════╗
echo  ║                       运行状态检查                                ║
echo  ╚═══════════════════════════════════════════════════════════════════╝
echo.

cd /d "%~dp0"

:: 读取端口配置
set PORT=14746
if exist "wsl端口.ini" (
    for /f "tokens=2 delims==" %%a in ('findstr /i "端口=" "wsl端口.ini"') do set PORT=%%a
)

echo  ┌─────────────────────────────────────────────────────────────────┐
echo  │ 进程状态                                                        │
echo  ├─────────────────────────────────────────────────────────────────┤

:: 检查副框架
tasklist | findstr /I "旺商聊框架.exe" >nul
if %errorlevel%==0 (
    echo  │ [√] 副框架 (旺商聊框架.exe)      - 运行中                      │
) else (
    echo  │ [×] 副框架 (旺商聊框架.exe)      - 未运行                      │
)

:: 检查主框架
tasklist | findstr /I "旺商聊机器人.exe" >nul
if %errorlevel%==0 (
    echo  │ [√] 主框架 (旺商聊机器人.exe)    - 运行中                      │
) else (
    echo  │ [×] 主框架 (旺商聊机器人.exe)    - 未运行                      │
)

echo  └─────────────────────────────────────────────────────────────────┘
echo.

echo  ┌─────────────────────────────────────────────────────────────────┐
echo  │ 端口状态                                                        │
echo  ├─────────────────────────────────────────────────────────────────┤

:: 检查端口
netstat -ano | findstr ":%PORT%" | findstr "LISTENING" >nul
if %errorlevel%==0 (
    echo  │ [√] 端口 %PORT%                     - 监听中                    │
) else (
    echo  │ [×] 端口 %PORT%                     - 未监听                    │
)

echo  └─────────────────────────────────────────────────────────────────┘
echo.

echo  ┌─────────────────────────────────────────────────────────────────┐
echo  │ 配置文件                                                        │
echo  ├─────────────────────────────────────────────────────────────────┤

if exist "config.ini" (
    echo  │ [√] config.ini                   - 存在                        │
) else (
    echo  │ [×] config.ini                   - 不存在                      │
)

if exist "wsl端口.ini" (
    echo  │ [√] wsl端口.ini                  - 存在 (端口: %PORT%)          │
) else (
    echo  │ [×] wsl端口.ini                  - 不存在                      │
)

if exist "Plugin.ini" (
    echo  │ [√] Plugin.ini                   - 存在                        │
) else (
    echo  │ [×] Plugin.ini                   - 不存在                      │
)

echo  └─────────────────────────────────────────────────────────────────┘
echo.

echo  ┌─────────────────────────────────────────────────────────────────┐
echo  │ 可执行文件                                                      │
echo  ├─────────────────────────────────────────────────────────────────┤

if exist "src\WSLFramework\bin\Release\旺商聊框架.exe" (
    echo  │ [√] 旺商聊框架.exe               - 已编译                      │
) else (
    echo  │ [×] 旺商聊框架.exe               - 未编译                      │
)

if exist "src\WangShangLiaoBot\bin\Release\旺商聊机器人.exe" (
    echo  │ [√] 旺商聊机器人.exe             - 已编译                      │
) else (
    echo  │ [×] 旺商聊机器人.exe             - 未编译                      │
)

echo  └─────────────────────────────────────────────────────────────────┘
echo.

echo  快捷命令:
echo    startup.cmd  - 启动服务
echo    stop.cmd     - 停止服务
echo    restart.cmd  - 重启服务
echo    build.cmd    - 编译项目
echo    run.cmd      - 静默启动
echo.
pause
