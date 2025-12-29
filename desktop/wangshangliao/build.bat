@echo off
call "C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\Tools\VsDevCmd.bat"
cd /d C:\EasyHook-1
msbuild WangShangLiaoBot.sln /t:Rebuild /p:Configuration=Debug /v:minimal
