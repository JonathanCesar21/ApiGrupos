@echo off
setlocal
if not exist "%~dp0Cloudflared\cloudflared.exe" (
  echo cloudflared.exe nao encontrado em %~dp0Cloudflared
  exit /b 1
)

if not exist "%USERPROFILE%\.cloudflared\config.yml" (
  echo Arquivo de configuracao nao encontrado em %USERPROFILE%\.cloudflared\config.yml
  echo Copie seu config.yml e credenciais do tunnel para esta pasta.
  exit /b 1
)

start "Cloudflare Tunnel" "%~dp0Cloudflared\cloudflared.exe" --config "%USERPROFILE%\.cloudflared\config.yml" tunnel run apigrupos
endlocal
