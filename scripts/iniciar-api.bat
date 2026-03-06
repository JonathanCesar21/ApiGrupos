@echo off
setlocal
cd /d %~dp0..
set ASPNETCORE_URLS=http://0.0.0.0:5000
ApiGrupos.exe
endlocal

