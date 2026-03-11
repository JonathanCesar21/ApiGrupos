#define MyAppName "ApiGrupos Controle"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "ApiGrupos"
#define MyAppURL "https://api.apiaob.com.br"
#define MyAppExeName "ApiGrupos.Manager.exe"

[Setup]
AppId={{7F6CF43C-4F4F-430F-9390-1D8A6E08684A}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\ApiGrupos
DisableProgramGroupPage=yes
OutputDir=..\dist\Installer
OutputBaseFilename=ApiGrupos-Setup
Compression=lzma
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64compatible

[Languages]
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[Files]
Source: "..\dist\ApiGrupos\*"; DestDir: "{app}\Api"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\dist\PainelControle\*"; DestDir: "{app}\Painel"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\dist\Cloudflared\*"; DestDir: "{app}\Cloudflared"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\installer\start-api.bat"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\installer\stop-api.bat"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\installer\abrir-painel.bat"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\installer\start-tunnel.bat"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\installer\stop-tunnel.bat"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\ApiGrupos\Painel de Controle"; Filename: "{app}\Painel\{#MyAppExeName}"
Name: "{autoprograms}\ApiGrupos\Iniciar API"; Filename: "{app}\start-api.bat"
Name: "{autoprograms}\ApiGrupos\Parar API"; Filename: "{app}\stop-api.bat"
Name: "{autoprograms}\ApiGrupos\Iniciar Tunnel"; Filename: "{app}\start-tunnel.bat"
Name: "{autoprograms}\ApiGrupos\Parar Tunnel"; Filename: "{app}\stop-tunnel.bat"
Name: "{autodesktop}\Painel ApiGrupos"; Filename: "{app}\Painel\{#MyAppExeName}"

[Run]
Filename: "{app}\Painel\{#MyAppExeName}"; Description: "Abrir Painel de Controle"; Flags: nowait postinstall skipifsilent
