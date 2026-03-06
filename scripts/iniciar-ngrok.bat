@echo off
setlocal
if not defined NGROK_PATH set NGROK_PATH=ngrok
%NGROK_PATH% http 5000
endlocal

