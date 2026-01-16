@echo off
echo ============================================
echo   旺商聊HTTPS抓包工具
echo ============================================
echo.

REM 检查mitmproxy是否安装
where mitmdump >nul 2>&1
if %errorlevel% neq 0 (
    echo [!] mitmproxy未安装
    echo.
    echo 请先安装mitmproxy:
    echo   pip install mitmproxy
    echo.
    pause
    exit /b 1
)

echo [1] 启动mitmproxy代理服务器...
echo     代理端口: 8080
echo     目标服务器: 120.233.185.185, 120.236.198.109
echo.
echo [2] 请设置系统代理:
echo     地址: 127.0.0.1
echo     端口: 8080
echo.
echo [3] 如果是首次使用，需要安装CA证书:
echo     访问 http://mitm.it 下载并安装证书
echo.
echo 按任意键启动抓包...
pause >nul

echo.
echo 启动中...
mitmdump -s mitmproxy_capture.py -p 8080 --ssl-insecure --set console_eventlog_verbosity=info

pause
