@echo off
chcp 65001 >nul
title 旺商聊机器人 - 编译

echo ╔══════════════════════════════════════════════════════════════╗
echo ║                      编译项目                                 ║
echo ╚══════════════════════════════════════════════════════════════╝
echo.

cd /d "%~dp0"

:: 查找 MSBuild
set MSBUILD=
if exist "%ProgramFiles%\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" (
    set "MSBUILD=%ProgramFiles%\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
) else if exist "%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe" (
    set "MSBUILD=%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe"
) else if exist "%windir%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe" (
    set "MSBUILD=%windir%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe"
)

if "%MSBUILD%"=="" (
    echo [错误] 找不到 MSBuild，请安装 Visual Studio 或 .NET Framework SDK
    pause
    exit /b 1
)

echo 使用 MSBuild: %MSBUILD%
echo.

:: 编译副框架
echo [1/2] 编译副框架 (WSLFramework)...
"%MSBUILD%" "src\WSLFramework\WSLFramework.csproj" /p:Configuration=Release /v:minimal /nologo
if %errorlevel% neq 0 (
    echo       × 编译失败
    pause
    exit /b 1
)
echo       √ 完成
echo.

:: 编译主框架
echo [2/2] 编译主框架 (WangShangLiaoBot)...
"%MSBUILD%" "src\WangShangLiaoBot\WangShangLiaoBot.csproj" /p:Configuration=Release /v:minimal /nologo
if %errorlevel% neq 0 (
    echo       × 编译失败
    pause
    exit /b 1
)
echo       √ 完成
echo.

echo ╔══════════════════════════════════════════════════════════════╗
echo ║                      编译完成!                                ║
echo ╠══════════════════════════════════════════════════════════════╣
echo ║  副框架: src\WSLFramework\bin\Release\旺商聊框架.exe          ║
echo ║  主框架: src\WangShangLiaoBot\bin\Release\旺商聊机器人.exe    ║
echo ╚══════════════════════════════════════════════════════════════╝
echo.
echo 现在可以运行 startup.cmd 启动程序
echo.
pause
