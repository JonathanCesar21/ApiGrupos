@echo off
setlocal
cd /d %~dp0..

echo Publicando API...
dotnet publish -c Release -r win-x64 --self-contained true -o .\dist\ApiGrupos
if errorlevel 1 exit /b 1

echo Publicando Painel de Controle...
dotnet publish .\ApiGrupos.Manager\ApiGrupos.Manager.csproj -c Release -r win-x64 --self-contained true -o .\dist\PainelControle
if errorlevel 1 exit /b 1

echo.
echo Publicacao concluida.
echo API: .\dist\ApiGrupos
echo Painel: .\dist\PainelControle

endlocal
