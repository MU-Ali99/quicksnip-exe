#define MyAppName "QuickSnip"
#define MyAppVersion "0.10.0"
#define MyAppPublisher "MU-Ali99"
#define MyAppExeName "QuickSnip.exe"

[Setup]
AppId={{B61E72D9-A872-48A5-BD38-5CB85F28D0D7}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL=https://github.com/MU-Ali99/quicksnip-exe
AppSupportURL=https://github.com/MU-Ali99/quicksnip-exe/issues
AppUpdatesURL=https://github.com/MU-Ali99/quicksnip-exe/releases
DefaultDirName={localappdata}\Programs\QuickSnip
DefaultGroupName=QuickSnip
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
OutputDir=..\artifacts\installer
OutputBaseFilename=QuickSnip-Setup-0.10.0-win-x64
SetupIconFile=..\Assets\QuickSnip.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern dynamic
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
CloseApplications=yes
RestartApplications=no
VersionInfoVersion={#MyAppVersion}.0
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription=QuickSnip installer
VersionInfoProductName={#MyAppName}
VersionInfoProductVersion={#MyAppVersion}

[Files]
Source: "..\artifacts\publish\win-x64\QuickSnip.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\QuickSnip"; Filename: "{app}\QuickSnip.exe"; WorkingDir: "{app}"
Name: "{group}\Uninstall QuickSnip"; Filename: "{uninstallexe}"

[Run]
Filename: "{app}\QuickSnip.exe"; Parameters: "--register-jump-list"; Flags: runhidden waituntilterminated
Filename: "{app}\QuickSnip.exe"; Description: "Open QuickSnip and view taskbar pinning guidance"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{app}"

[Code]
function InitializeUninstall(): Boolean;
begin
  Result := True;
end;
