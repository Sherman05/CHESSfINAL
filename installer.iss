; InnoSetup Script for chess-T1
; Создаёт установщик chess-T1-setup.exe
;
; Автоматически запускается из СБОРКА_EXE.bat
; Или вручную: откройте этот файл в InnoSetup → Build

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
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
; Минимум Windows 7
MinVersion=6.1
; Русский язык
LanguageDetectionMethod=uilanguage
ShowLanguageDialog=no
; Показывать прогресс
DisableWelcomePage=no
WizardStyle=modern

[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"

[Messages]
russian.WelcomeLabel1=Установка chess-T1
russian.WelcomeLabel2=Программа установит графический интерфейс для игры chess-T1 на ваш компьютер.%n%nНажмите «Далее» для продолжения.
russian.FinishedLabel=Установка chess-T1 завершена. Для запуска используйте ярлык на рабочем столе или в меню Пуск.

[Tasks]
Name: "desktopicon"; Description: "Создать ярлык на рабочем столе"; GroupDescription: "Дополнительно:"; Flags: checked

[Files]
Source: "dist\chess-T1.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
; Меню Пуск
Name: "{group}\chess-T1"; Filename: "{app}\chess-T1.exe"
Name: "{group}\Удалить chess-T1"; Filename: "{uninstallexe}"
; Рабочий стол
Name: "{autodesktop}\chess-T1"; Filename: "{app}\chess-T1.exe"; Tasks: desktopicon

[Run]
; Запустить после установки
Filename: "{app}\chess-T1.exe"; Description: "Запустить chess-T1"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{app}"
