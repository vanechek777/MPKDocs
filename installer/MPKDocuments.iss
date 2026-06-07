; Установщик Windows для МПК.Документы (Inno Setup 6+)
; 1) Соберите приложение:  .\installer\build-windows.ps1
; 2) Положите логотип:     MPKDocumentsMAUI\Resources\AppIcon\appicon.ico
; 3) Откройте этот файл в Inno Setup Compiler и нажмите Compile (F9)

#define MyAppName "МПК.Документы"
#define MyAppPublisher "МПК"
#define MyAppExeName "MPKDocumentsMAUI.exe"
#define MyAppVersion "1.0.0"
#define MyPublishDir "..\publish\MPKDocumentsMAUI-win-x64"
#define MyAppIcon "..\MPKDocumentsMAUI\MPKDocumentsMAUI\Resources\AppIcon\appicon.ico"

[Setup]
AppId={{A8F3C2E1-9B4D-4F6A-8C2E-1D5E7F9A0B3C}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=output
OutputBaseFilename=MPKDocuments-Setup-{#MyAppVersion}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
ShowLanguageDialog=auto

#ifexist MyAppIcon
SetupIconFile={#MyAppIcon}
UninstallDisplayIcon={#MyAppIcon}
#endif

[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#MyPublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon; WorkingDir: "{app}"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Messages]
russian.WelcomeLabel2=Программа установит [name/ver] на ваш компьютер.%n%nДокументооборот и подписание для сотрудников МПК.%n%nТребуется .NET 9 Desktop Runtime (x64), если ещё не установлен.

[Code]
function InitializeSetup: Boolean;
begin
  Result := True;
end;
