@echo off
setlocal
cd /d %~dp0
start "ApiGrupos" cmd /k iniciar-api.bat
start "Ngrok" cmd /k iniciar-ngrok.bat
endlocal
