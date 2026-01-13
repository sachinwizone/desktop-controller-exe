; Desktop Controller Pro - Inno Setup Script
; This script creates a professional MSI/EXE installer

#define MyAppName "Desktop Controller Pro"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Wizone IT Network India Private Limited"
#define MyAppURL "https://www.wizone.in"
#define MyAppExeName "DesktopController.exe"
#define MyAppId "{{12345678-1234-1234-1234-123456789012}"

[Setup]
; Application Info
AppId={#MyAppId}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}

; Installation Settings
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes

; Output Settings
OutputDir=installer_output
OutputBaseFilename=DesktopControllerPro_Setup_v{#MyAppVersion}
SetupIconFile=app.ico
Compression=lzma2/ultra64
SolidCompression=yes

; Visual Settings
WizardStyle=modern
WizardSizePercent=110

; Privileges (require admin for proper system control)
PrivilegesRequired=admin
PrivilegesRequiredOverridesAllowed=dialog

; Uninstall Settings
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName}

; Architecture
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64

; Restart Settings
AlwaysRestart=no
RestartIfNeededByRun=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: checkedonce
Name: "autostart"; Description: "Start automatically with Windows"; GroupDescription: "Startup Options:"; Flags: checkedonce

[Files]
; Main Application Files
Source: "publish_installer\DesktopController.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "publish_installer\*.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "publish_installer\*.pdb"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist

; Icon file
Source: "app.ico"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist

[Dirs]
Name: "{app}\logs"; Permissions: users-full
Name: "{app}\data"; Permissions: users-full

[Icons]
; Start Menu Shortcut
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Comment: "Launch Desktop Controller Pro"

; Desktop Shortcut
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon; Comment: "Launch Desktop Controller Pro"

; Uninstall Shortcut
Name: "{group}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"

[Registry]
; Auto-start with Windows
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "DesktopControllerPro"; ValueData: """{app}\{#MyAppExeName}"" --autostart"; Flags: uninsdeletevalue; Tasks: autostart

; Application registration
Root: HKLM; Subkey: "SOFTWARE\DesktopController"; ValueType: string; ValueName: "InstallPath"; ValueData: "{app}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "SOFTWARE\DesktopController"; ValueType: string; ValueName: "Version"; ValueData: "{#MyAppVersion}"; Flags: uninsdeletekey

[Run]
; Launch after install
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent runascurrentuser

[UninstallRun]
; Stop the application before uninstall
Filename: "taskkill"; Parameters: "/F /IM DesktopController.exe"; Flags: runhidden; RunOnceId: "StopApp"

[UninstallDelete]
; Clean up application data on uninstall
Type: filesandordirs; Name: "{localappdata}\DesktopController"
Type: filesandordirs; Name: "{app}\logs"
Type: filesandordirs; Name: "{app}\data"

[Code]
// Check if application is running before installation
function InitializeSetup(): Boolean;
var
  ResultCode: Integer;
begin
  Result := True;
  
  // Try to stop any running instance
  Exec('taskkill', '/F /IM DesktopController.exe', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  
  // Wait a moment for the process to fully terminate
  Sleep(1000);
end;

// Check before uninstall
function InitializeUninstall(): Boolean;
var
  ResultCode: Integer;
begin
  Result := True;
  
  // Stop the application
  Exec('taskkill', '/F /IM DesktopController.exe', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Sleep(1000);
end;

// Custom message after installation
procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    // Installation completed
    Log('Desktop Controller Pro installed successfully');
  end;
end;
