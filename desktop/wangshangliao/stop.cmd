@echo off
chcp 65001 >nul
title 旺商聊机器人 - 停止服务
color 0C

echo.
echo  ╔═══════════════════════════════════════════════════════════════════╗
echo  ║                       停止所有服务                                ║
echo  ╚═══════════════════════════════════════════════════════════════════╝
echo.

cd /d "%~dp0"

:: 停止主框架
echo  [1/2] 停止主框架 (旺商聊机器人)...
tasklist | findstr /I "旺商聊机器人.exe" >nul
if %errorlevel%==0 (
    taskkill /F /IM "旺商聊机器人.exe" >nul 2>&1
    echo        √ 主框架已停止
) else (
    echo        - 主框架未运行
)
echo.

:: 停止副框架
echo  [2/2] 停止副框架 (旺商聊框架)...
tasklist | findstr /I "旺商聊框架.exe" >nul
if %errorlevel%==0 (
    taskkill /F /IM "旺商聊框架.exe" >nul 2>&1
    echo        √ 副框架已停止
) else (
    echo        - 副框架未运行
)
echo.

echo  ╔═══════════════════════════════════════════════════════════════════╗
echo  ║                      所有服务已停止                               ║
echo  ╚═══════════════════════════════════════════════════════════════════╝
echo.
pause
