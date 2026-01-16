@echo off
chcp 65001 >nul
title 旺商聊机器人 - 重新编译并启动
color 0B

echo.
echo  ╔═══════════════════════════════════════════════════════════════════╗
echo  ║                    重新编译并启动                                 ║
echo  ╚═══════════════════════════════════════════════════════════════════╝
echo.

cd /d "%~dp0"

:: 停止旧服务
echo  [1/4] 停止旧服务...
taskkill /F /IM "旺商聊框架.exe" >nul 2>&1
taskkill /F /IM "旺商聊机器人.exe" >nul 2>&1
echo        √ 完成
echo.

:: 查找 MSBuild
set MSBUILD=
if exist "%ProgramFiles%\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" (
    set "MSBUILD=%ProgramFiles%\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
) else if exist "%ProgramFiles%\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe" (
    set "MSBUILD=%ProgramFiles%\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe"
) else if exist "%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe" (
    set "MSBUILD=%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe"
) else if exist "%windir%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe" (
    set "MSBUILD=%windir%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe"
)

if "%MSBUILD%"=="" (
    echo  [错误] 找不到 MSBuild
    echo         请安装 Visual Studio 或 .NET Framework SDK
    pause
    exit /b 1
)

:: 编译副框架
echo  [2/4] 编译副框架 (WSLFramework)...
"%MSBUILD%" "src\WSLFramework\WSLFramework.csproj" /p:Configuration=Release /v:minimal /nologo
if %errorlevel% neq 0 (
    echo        × 编译失败
    pause
    exit /b 1
)
echo        √ 完成
echo.

:: 编译主框架
echo  [3/4] 编译主框架 (WangShangLiaoBot)...
"%MSBUILD%" "src\WangShangLiaoBot\WangShangLiaoBot.csproj" /p:Configuration=Release /v:minimal /nologo
if %errorlevel% neq 0 (
    echo        × 编译失败
    pause
    exit /b 1
)
echo        √ 完成
echo.

:: 启动服务
echo  [4/4] 启动服务...
call startup.cmd
