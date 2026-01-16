@echo off
chcp 65001 >nul
echo ===== 构建生产目录 =====

REM 获取输出目录 (参数1)
set OUTDIR=%1
if "%OUTDIR%"=="" set OUTDIR=bin\Debug\

echo 输出目录: %OUTDIR%

REM 切换到输出目录
cd /d "%OUTDIR%"

REM 创建目录结构
echo 创建目录结构...
if not exist "plugin" mkdir "plugin"
if not exist "YX_Clinent" mkdir "YX_Clinent"
if not exist "zcg" mkdir "zcg"
if not exist "zcg服务端收发日志" mkdir "zcg服务端收发日志"
if not exist "zcg收发日志" mkdir "zcg收发日志"
if not exist "旺旺号资料" mkdir "旺旺号资料"
if not exist "logs" mkdir "logs"

REM 创建run.cmd启动脚本
echo 创建启动脚本...
if not exist "run.cmd" (
    echo @echo off> "run.cmd"
    echo chcp 65001 ^>nul>> "run.cmd"
    echo title 旺商聊框架>> "run.cmd"
    echo echo ========================================>> "run.cmd"
    echo echo   旺商聊框架 启动中...>> "run.cmd"
    echo echo ========================================>> "run.cmd"
    echo start "" "旺商聊框架.exe">> "run.cmd"
)

REM 创建config.ini
echo 创建配置文件...
if not exist "config.ini" (
    echo [程序配置]> "config.ini"
    echo 程序名=旺商聊框架>> "config.ini"
    echo 非当天缓存清除=真>> "config.ini"
    echo.>> "config.ini"
    echo [nim]>> "config.ini"
    echo 版本=1>> "config.ini"
    echo.>> "config.ini"
    echo [环境]>> "config.ini"
    echo 线上环境=真>> "config.ini"
)

REM 创建Plugin.ini
if not exist "Plugin.ini" (
    echo [Plugin]> "Plugin.ini"
)

REM 创建zcg端口.ini
if not exist "zcg端口.ini" (
    echo [端口]> "zcg端口.ini"
    echo 端口=14745>> "zcg端口.ini"
)

REM 创建zcg目录下的配置文件
if not exist "zcg\登录配置.ini" (
    echo [程序配置]> "zcg\登录配置.ini"
    echo 版本=1>> "zcg\登录配置.ini"
)

REM 创建空数据库文件
if not exist "zcg\攻击.db" type nul > "zcg\攻击.db"
if not exist "zcg\上下分.db" type nul > "zcg\上下分.db"
if not exist "zcg\设置.db" type nul > "zcg\设置.db"
if not exist "zcg\玩家姓名.db" type nul > "zcg\玩家姓名.db"
if not exist "zcg\邀请记录.db" type nul > "zcg\邀请记录.db"
if not exist "zcg\账单.db" type nul > "zcg\账单.db"

echo ===== 生产目录构建完成 =====
