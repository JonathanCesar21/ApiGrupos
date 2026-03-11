@echo off
setlocal
cd /d %~dp0..

if not exist ".\dist\PainelControle\ApiGrupos.Manager.exe" (
  echo Painel nao encontrado em .\dist\PainelControle\ApiGrupos.Manager.exe
  echo Execute primeiro: scripts\publicar-ambiente.bat
  exit /b 1
)

start "Painel ApiGrupos" ".\dist\PainelControle\ApiGrupos.Manager.exe"

endlocal
