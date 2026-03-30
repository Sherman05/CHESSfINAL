; InnoSetup Script for chess-T1
; Creates a Windows installer package with desktop shortcut option
;
; Usage:
;   1. Build the .exe first: python build_windows.py
;   2. Install InnoSetup from https://jrsoftware.org/isinfo.php
;   3. Open this file in InnoSetup Compiler and click Build
;   OR run from command line: iscc installer.iss

[Setup]
AppName=chess-T1
AppVersion=1.0
AppPublisher=chess-T1
DefaultDirName={autopf}\chess-T1
DefaultGroupName=chess-T1
OutputDir=dist
OutputBaseFilename=chess-T1-setup
Compression=lzma2/ultra64
SolidCompression=yes
; Installer size target: 10-15 MB
ArchitecturesAllowed=x86 x64
ArchitecturesInstallIn64BitMode=x64
; Minimum Windows version: XP
MinVersion=5.1
; Russian language
LanguageDetectionMethod=uilanguage
ShowLanguageDialog=no

; Uncomment and set path if you have an icon file:
; SetupIconFile=src\assets\app_icon.ico
; UninstallDisplayIcon={app}\chess-T1.exe

[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"

[Tasks]
Name: "desktopicon"; Description: "Создать ярлык на рабочем столе"; GroupDescription: "Дополнительные задачи:"; Flags: checked

[Files]
; Main executable (built by PyInstaller)
Source: "dist\chess-T1.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
; Start Menu shortcut
Name: "{group}\chess-T1"; Filename: "{app}\chess-T1.exe"
Name: "{group}\Удалить chess-T1"; Filename: "{uninstallexe}"
; Desktop shortcut (optional)
Name: "{autodesktop}\chess-T1"; Filename: "{app}\chess-T1.exe"; Tasks: desktopicon

[Run]
; Launch after install
Filename: "{app}\chess-T1.exe"; Description: "Запустить chess-T1"; Flags: nowait postinstall skipifsilent
