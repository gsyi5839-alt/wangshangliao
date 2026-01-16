@echo off
echo 正在取消系统代理...
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Internet Settings" /v ProxyEnable /t REG_DWORD /d 0 /f
echo 系统代理已取消
pause
