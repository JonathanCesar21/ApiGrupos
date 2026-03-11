@echo off
setlocal EnableDelayedExpansion
cd /d %~dp0..

call scripts\publicar-ambiente.bat
if errorlevel 1 exit /b 1

set "ISCC_PATH=C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
if not exist "!ISCC_PATH!" set "ISCC_PATH=%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe"

if not exist "!ISCC_PATH!" (
  echo Inno Setup nao encontrado em:
  echo !ISCC_PATH!
  echo.
  echo Instale com:
  echo winget install --id JRSoftware.InnoSetup -e
  exit /b 1
)

"!ISCC_PATH!" installer\ApiGruposSetup.iss
if errorlevel 1 exit /b 1

echo.
echo Instalador gerado em:
echo .\dist\Installer\ApiGrupos-Setup.exe

endlocal
