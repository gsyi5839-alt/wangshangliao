@echo off
chcp 65001 >nul
title 旺商聊机器人 - 重启服务
color 0E

echo.
echo  ╔═══════════════════════════════════════════════════════════════════╗
echo  ║                       重启服务                                    ║
echo  ╚═══════════════════════════════════════════════════════════════════╝
echo.

cd /d "%~dp0"

:: 停止服务
echo  [1/2] 停止旧服务...
taskkill /F /IM "旺商聊机器人.exe" >nul 2>&1
taskkill /F /IM "旺商聊框架.exe" >nul 2>&1
echo        √ 已停止
timeout /t 2 /nobreak >nul
echo.

:: 启动服务
echo  [2/2] 启动新服务...
call startup.cmd
