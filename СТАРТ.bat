@echo off
chcp 65001 >nul 2>&1

:: Check if already compiled
if exist "%~dp0dist\chess-T1.exe" (
    start "" "%~dp0dist\chess-T1.exe"
    exit /b 0
)

:: Find C# compiler
set "CSC="
if exist "%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not defined CSC if exist "%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not defined CSC if exist "%WINDIR%\Microsoft.NET\Framework\v3.5\csc.exe" set "CSC=%WINDIR%\Microsoft.NET\Framework\v3.5\csc.exe"

if not defined CSC (
    echo .NET Framework не найден
    pause
    exit /b 1
)

if not exist "%~dp0dist" mkdir "%~dp0dist"

echo Компиляция...
"%CSC%" /nologo /target:winexe /out:"%~dp0dist\chess-T1.exe" /optimize+ ^
    /reference:System.dll ^
    /reference:System.Drawing.dll ^
    /reference:System.Windows.Forms.dll ^
    "%~dp0csharp\Program.cs" ^
    "%~dp0csharp\Pieces.cs" ^
    "%~dp0csharp\GameSession.cs" ^
    "%~dp0csharp\BoardControl.cs" ^
    "%~dp0csharp\MainForm.cs" ^
    "%~dp0csharp\Dialogs.cs" ^
    "%~dp0csharp\IntroPage.cs" ^
    "%~dp0csharp\PieceTray.cs" ^
    "%~dp0csharp\Properties\AssemblyInfo.cs"

if errorlevel 1 (
    echo Ошибка компиляции
    pause
    exit /b 1
)

echo Запуск...
start "" "%~dp0dist\chess-T1.exe"
