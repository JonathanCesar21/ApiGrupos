@echo off
setlocal
cd /d %~dp0..

echo Publicando API...
dotnet publish -c Release -r win-x64 --self-contained true -o .\dist\ApiGrupos
if errorlevel 1 exit /b 1

echo Publicando Painel de Controle...
dotnet publish .\ApiGrupos.Manager\ApiGrupos.Manager.csproj -c Release -r win-x64 --self-contained true -o .\dist\PainelControle
if errorlevel 1 exit /b 1

echo Copiando cloudflared...
set "CF_SRC="
if exist "%LOCALAPPDATA%\Microsoft\WinGet\Packages\Cloudflare.cloudflared_Microsoft.Winget.Source_8wekyb3d8bbwe\cloudflared.exe" set "CF_SRC=%LOCALAPPDATA%\Microsoft\WinGet\Packages\Cloudflare.cloudflared_Microsoft.Winget.Source_8wekyb3d8bbwe\cloudflared.exe"
if "%CF_SRC%"=="" if exist "%LOCALAPPDATA%\Programs\Cloudflare\cloudflared\cloudflared.exe" set "CF_SRC=%LOCALAPPDATA%\Programs\Cloudflare\cloudflared\cloudflared.exe"
if "%CF_SRC%"=="" (
  echo cloudflared.exe nao encontrado. Instale com:
  echo winget install --id Cloudflare.cloudflared -e
  exit /b 1
)

if not exist ".\dist\Cloudflared" mkdir ".\dist\Cloudflared"
copy /Y "%CF_SRC%" ".\dist\Cloudflared\cloudflared.exe" >nul
if errorlevel 1 exit /b 1

echo.
echo Publicacao concluida.
echo API: .\dist\ApiGrupos
echo Painel: .\dist\PainelControle
echo Cloudflared: .\dist\Cloudflared

endlocal
