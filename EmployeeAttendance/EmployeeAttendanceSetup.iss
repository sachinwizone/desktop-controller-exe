; Employee Attendance - Installer with Progress Bar Only
#define MyAppName "Employee Attendance"
#define MyAppVersion "1.1.7"
#define MyAppPublisher "Wizone AI Labs"
#define MyAppExeName "EmployeeAttendance.exe"

[Setup]
AppId={{A5B7C9D1-E3F5-4A6B-8C0D-2E4F6A8B0C2D}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={localappdata}\{#MyAppName}
DisableDirPage=yes
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=publish_final
OutputBaseFilename=EmployeeAttendance_Setup_v1.1.7
Compression=lzma
SolidCompression=yes
PrivilegesRequired=lowest
DisableWelcomePage=yes
DisableReadyPage=yes
DisableReadyMemo=yes
DisableFinishedPage=no
WizardStyle=modern
UninstallDisplayIcon={app}\{#MyAppExeName}
CreateUninstallRegKey=yes
ShowComponentSizes=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "publish_final\EmployeeAttendance.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "publish_final\*.dll"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "publish_final\*.config"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "logo ai.png"; DestDir: "{app}"; DestName: "logo.png"; Flags: ignoreversion
Source: "logo_3d_animation.png"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"

[Registry]
Root: HKCU; Subkey: "SOFTWARE\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "{#MyAppName}"; ValueData: """{app}\{#MyAppExeName}"" --silent"; Flags: uninsdeletevalue

[Run]
Filename: "{app}\{#MyAppExeName}"; Flags: nowait postinstall

[UninstallRun]
Filename: "taskkill"; Parameters: "/f /im {#MyAppExeName}"; Flags: runhidden; RunOnceId: "StopApp"

[UninstallDelete]
Type: filesandordirs; Name: "{localappdata}\EmployeeAttendance"

[Messages]
FinishedHeadingLabel=Installation Complete!
FinishedLabel={#MyAppName} has been installed successfully.
ClickFinish=Click Launch to start the application.
ButtonFinish=Launch

[Code]
function InitializeUninstall: Boolean;
var
  ResultCode: Integer;
  TempFile: String;
  Lines: TArrayOfString;
  ProcessFound: Boolean;
begin
  Result := True;
  ProcessFound := False;

  // Kill the process forcefully before uninstall
  Exec('taskkill', '/F /IM EmployeeAttendance.exe', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);

  // Wait a moment for process to terminate
  Sleep(1000);

  // Double check if still running
  TempFile := ExpandConstant('{tmp}\tasklist.txt');
  if Exec('cmd.exe', '/c tasklist /FI "IMAGENAME eq EmployeeAttendance.exe" > "' + TempFile + '"', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
  begin
    if LoadStringsFromFile(TempFile, Lines) then
    begin
      if GetArrayLength(Lines) > 3 then
        ProcessFound := True;
    end;
    DeleteFile(TempFile);
  end;

  // If still running after force kill, show error
  if ProcessFound then
  begin
    MsgBox('Unable to stop Employee Attendance. Please restart your computer and try again.', mbError, MB_OK);
    Result := False;
  end;
end;

var
  InstallPage: TWizardPage;
  ProgressBar: TNewProgressBar;
  PercentLabel: TNewStaticText;
  CompanyLabel: TNewStaticText;
  InstallButton: TNewButton;
  LogoPictureBox: TBitmapImage;

procedure InstallButtonClick(Sender: TObject);
begin
  InstallButton.Enabled := False;
  InstallButton.Caption := 'Installing...';
  WizardForm.NextButton.OnClick(WizardForm.NextButton);
end;

procedure InitializeWizard();
var
  LogoPath: String;
begin
  { Change window title to company name }
  WizardForm.Caption := 'WIZONE AI LAB';
  
  InstallPage := CreateCustomPage(wpWelcome, '', '');
  
  LogoPictureBox := TBitmapImage.Create(WizardForm);
  LogoPictureBox.Parent := InstallPage.Surface;
  LogoPictureBox.Left := 10;
  LogoPictureBox.Top := 15;
  LogoPictureBox.Width := 120;
  LogoPictureBox.Height := 90;
  LogoPictureBox.Stretch := True;
  
  LogoPath := ExpandConstant('{src}\logo ai.png');
  if FileExists(LogoPath) then
    LogoPictureBox.Bitmap.LoadFromFile(LogoPath);
  
  PercentLabel := TNewStaticText.Create(WizardForm);
  PercentLabel.Parent := InstallPage.Surface;
  PercentLabel.Caption := 'Employee Attendance System' + #13#13 + 'Click Install to begin installation';
  PercentLabel.Font.Size := 10;
  PercentLabel.Left := 10;
  PercentLabel.Top := 115;
  PercentLabel.Width := InstallPage.SurfaceWidth - 20;
  
  ProgressBar := TNewProgressBar.Create(WizardForm);
  ProgressBar.Parent := InstallPage.Surface;
  ProgressBar.Left := 10;
  ProgressBar.Top := 210;
  ProgressBar.Width := InstallPage.SurfaceWidth - 20;
  ProgressBar.Height := 25;
  ProgressBar.Min := 0;
  ProgressBar.Max := 100;
  ProgressBar.Position := 0;
  ProgressBar.Visible := False;
  
  { Create CompanyLabel that will be shown during installation }
  CompanyLabel := TNewStaticText.Create(WizardForm);
  CompanyLabel.Parent := WizardForm;
  CompanyLabel.Caption := 'WIZONE AI LAB';
  CompanyLabel.Font.Size := 16;
  CompanyLabel.Font.Style := [fsBold];
  CompanyLabel.Left := 100;
  CompanyLabel.Top := 120;
  CompanyLabel.Width := WizardForm.ClientWidth - 200;
  CompanyLabel.Visible := False;
  
  InstallButton := TNewButton.Create(WizardForm);
  InstallButton.Parent := InstallPage.Surface;
  InstallButton.Caption := 'Install';
  InstallButton.Left := (InstallPage.SurfaceWidth - 150) div 2;
  InstallButton.Top := 260;
  InstallButton.Width := 150;
  InstallButton.Height := 45;
  InstallButton.Font.Size := 12;
  InstallButton.Font.Style := [fsBold];
  InstallButton.OnClick := @InstallButtonClick;
  
  WizardForm.NextButton.Visible := False;
  WizardForm.BackButton.Visible := False;
  WizardForm.CancelButton.Visible := False;
end;

function InitializeSetup(): Boolean;
var
  ResultCode: Integer;
begin
  Result := True;

  // Force kill any running instance before installation
  Exec('taskkill', '/F /IM EmployeeAttendance.exe', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);

  // Wait for process to fully terminate
  Sleep(1000);
end;

procedure CurPageChanged(CurPageID: Integer);
var
  i: Integer;
begin
  if CurPageID = InstallPage.ID then
  begin
    WizardForm.NextButton.Visible := False;
    WizardForm.BackButton.Visible := False;
    WizardForm.CancelButton.Visible := False;
  end
  else if CurPageID = wpInstalling then
  begin
    { Completely hide all Inno Setup default UI elements }
    WizardForm.PageNameLabel.Visible := False;
    WizardForm.PageDescriptionLabel.Visible := False;
    WizardForm.StatusLabel.Visible := False;
    WizardForm.FilenameLabel.Visible := False;
    
    { Hide top/bottom bar areas }
    WizardForm.InnerNotebook.Visible := False;
    
    { Hide all buttons }
    WizardForm.BackButton.Visible := False;
    WizardForm.NextButton.Visible := False;
    WizardForm.CancelButton.Visible := False;
    
    { Clear any text that might be set }
    WizardForm.StatusLabel.Caption := '';
    WizardForm.FilenameLabel.Caption := '';
    
    { Show company label that was created in InitializeWizard }
    CompanyLabel.Visible := True;
    
    { Reposition and show our progress components on the form itself }
    ProgressBar.Parent := WizardForm;
    ProgressBar.Left := 100;
    ProgressBar.Top := 230;
    ProgressBar.Width := WizardForm.ClientWidth - 200;
    ProgressBar.Height := 35;
    ProgressBar.Min := 0;
    ProgressBar.Max := 100;
    ProgressBar.Position := 0;
    ProgressBar.Visible := True;
    
    PercentLabel.Parent := WizardForm;
    PercentLabel.Left := 100;
    PercentLabel.Top := 180;
    PercentLabel.Width := WizardForm.ClientWidth - 200;
    PercentLabel.Caption := 'Installing... 0%';
    PercentLabel.Font.Size := 16;
    PercentLabel.Font.Style := [fsBold];
    PercentLabel.Visible := True;
  end
  else if CurPageID = wpFinished then
  begin
    WizardForm.NextButton.Visible := True;
    WizardForm.NextButton.Caption := 'Launch';
    WizardForm.CancelButton.Visible := False;
    WizardForm.BackButton.Visible := False;
  end;
end;

procedure CurInstallProgressChanged(CurProgress, MaxProgress: Integer);
var
  Percent: Integer;
begin
  if MaxProgress > 0 then
  begin
    Percent := (CurProgress * 100) div MaxProgress;
    ProgressBar.Position := Percent;
    PercentLabel.Caption := Format('%d%%', [Percent]);
  end;
end;

function ShouldSkipPage(PageID: Integer): Boolean;
begin
  Result := (PageID = wpWelcome) or (PageID = wpSelectDir) or (PageID = wpSelectTasks) or 
            (PageID = wpReady) or (PageID = wpSelectProgramGroup) or (PageID = wpLicense);
end;
