@echo off
chcp 65001 >nul
echo ========================================
echo     启动旺商聊客户端（调试模式）
echo ========================================
echo.
echo 正在关闭现有旺商聊进程...
taskkill /F /IM "wangshangliao_win_online.exe" >nul 2>&1
timeout /t 2 >nul

echo 正在启动旺商聊客户端（CDP 端口: 9222）...
echo.

set WSL_PATH="C:\Program Files\wangshangliao_win_online\wangshangliao_win_online.exe"
if exist %WSL_PATH% (
    start "" %WSL_PATH% --remote-debugging-port=9222
    echo ✓ 旺商聊客户端已启动
    echo.
    echo ========================================
    echo     请在旺商聊中登录您的机器人账号
    echo     然后返回副框架，点击【登录】
    echo ========================================
) else (
    echo × 未找到旺商聊客户端
    echo 请检查路径: %WSL_PATH%
)
echo.
pause
