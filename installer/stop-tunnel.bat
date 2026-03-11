@echo off
setlocal
taskkill /IM cloudflared.exe /F >nul 2>&1
endlocal
