#define MyAppName "KeySwitcher"
#define MyAppVersion "0.5.0"
#define MyAppPublisher "by koda"
#define MyAppExeName "KeySwitcher.exe"

[Setup]
AppId={{9B7618F3-8E3A-44D5-A841-73F972AF651C}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\KeySwitcher
DefaultGroupName=KeySwitcher
OutputDir=output
OutputBaseFilename=KeySwitcher-0.5.0-Setup
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=lowest
UninstallDisplayIcon={app}\{#MyAppExeName}
CloseApplications=yes

[Files]
Source: "..\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\KeySwitcher"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\KeySwitcher"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Создать значок на рабочем столе"; GroupDescription: "Дополнительные значки:"
Name: "startup"; Description: "Запускать вместе с Windows"; GroupDescription: "Автозапуск:"

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "KeySwitcher"; ValueData: """{app}\{#MyAppExeName}"""; Flags: uninsdeletevalue; Tasks: startup

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Запустить KeySwitcher"; Flags: nowait postinstall skipifsilent
