@echo off
:: ============================================================
:: 旺商聊机器人 - 静默启动脚本
:: 参考 ZCG 架构设计
:: ============================================================
:: 使用方式:
::   双击运行 - 隐藏窗口静默启动
::   run.cmd show - 显示启动窗口
:: ============================================================

if "%1"=="h" goto begin
if "%1"=="show" goto show_mode
start mshta vbscript:createobject("wscript.shell").run("""%~nx0"" h",0)(window.close)&&exit

:begin
chcp 65001 >nul
cd /d "%~dp0"

:: 关闭旧进程
taskkill /F /IM "旺商聊框架.exe" >nul 2>&1
taskkill /F /IM "旺商聊机器人.exe" >nul 2>&1
timeout /t 1 /nobreak >nul

:: 启动副框架 (连接旺商聊)
cd /d "%~dp0src\WSLFramework\bin\Release"
if exist "旺商聊框架.exe" (
    start "" "旺商聊框架.exe"
    timeout /t 3 /nobreak >nul
)

:: 启动主框架 (UI界面)
cd /d "%~dp0src\WangShangLiaoBot\bin\Release"
if exist "旺商聊机器人.exe" (
    start "" "旺商聊机器人.exe"
)

goto :eof

:show_mode
chcp 65001 >nul
cd /d "%~dp0"
call startup.cmd
