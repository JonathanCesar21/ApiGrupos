@echo off
setlocal
cd /d "%~dp0Api"
set ASPNETCORE_URLS=http://0.0.0.0:5000
start "ApiGrupos" "%~dp0Api\ApiGrupos.exe"
endlocal
