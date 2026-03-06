@echo off
setlocal
if "%~1"=="" (
  echo Uso: configurar-ngrok-token.bat SEU_AUTHTOKEN
  exit /b 1
)
if not defined NGROK_PATH set NGROK_PATH=ngrok
%NGROK_PATH% config add-authtoken %~1
endlocal
