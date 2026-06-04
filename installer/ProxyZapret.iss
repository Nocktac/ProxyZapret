#define MyAppName "ProxyZapret"
#ifndef MyAppVersion
#define MyAppVersion "0.4.0"
#endif
#define MyAppPublisher "Nocktac"
#define MyAppExeName "ProxyZapret.exe"

[Setup]
AppId={{5C7DFD05-8A57-44F2-8E0F-EA4AC8A6B201}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=..\release
OutputBaseFilename=ProxyZapret-Setup-{#MyAppVersion}
SetupIconFile=..\assets\ProxyZapret.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
WizardStyle=modern
CloseApplications=yes
RestartApplications=no

[Files]
Source: "..\ProxyZapret.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\ProxyZapret.Updater.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\core\sing-box.exe"; DestDir: "{app}\core"; Flags: ignoreversion
Source: "..\config\settings.example.json"; DestDir: "{app}\config"; Flags: ignoreversion
Source: "..\config\routing-rules.json"; DestDir: "{app}\config"; Flags: ignoreversion
Source: "..\config\settings.production.json"; DestDir: "{commonappdata}\ProxyZapret\config"; DestName: "settings.local.json"; Flags: onlyifdoesntexist uninsneveruninstall

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; IconFilename: "{app}\{#MyAppExeName}"; AppUserModelID: "Nocktac.ProxyZapret.Client"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; IconFilename: "{app}\{#MyAppExeName}"; AppUserModelID: "Nocktac.ProxyZapret.Client"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Run {#MyAppName}"; Verb: "runas"; Flags: shellexec nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{commonappdata}\ProxyZapret\runtime"
