@echo off
chcp 65001 >nul
title 旺商聊 - 调试模式启动器

echo ═══════════════════════════════════════════════════════════
echo           旺商聊 - 调试模式启动器
echo ═══════════════════════════════════════════════════════════
echo.
echo 此脚本将以调试模式启动旺商聊应用
echo 调试端口: 9222
echo.
echo 启动后，运行 ChatAutoBot.exe 即可连接并控制
echo.
echo ═══════════════════════════════════════════════════════════

set "APP_PATH=C:\旺商聊\wangshangliao_win_online\wangshangliao_win_online.exe"

if not exist "%APP_PATH%" (
    echo [错误] 找不到应用程序: %APP_PATH%
    echo 请修改此脚本中的路径
    pause
    exit /b 1
)

echo 正在启动: %APP_PATH%
echo 参数: --remote-debugging-port=9222
echo.

start "" "%APP_PATH%" --remote-debugging-port=9222

echo.
echo [提示] 应用已启动，请等待几秒钟让应用完全加载
echo [提示] 然后运行 ChatAutoBot.exe 并选择 "Electron模式"
echo.
pause

