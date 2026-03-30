@echo off
chcp 65001 >nul 2>&1
title chess-T1 — Сборка C# EXE

echo ============================================================
echo    chess-T1 — Сборка C# приложения
echo    Результат: chess-T1.exe (работает на Windows XP-11)
echo ============================================================
echo.

:: Find csc.exe (.NET Framework compiler)
set "CSC="

:: Try .NET Framework 4.x first (most common)
if exist "%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe" (
    set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
)
:: Try .NET Framework 3.5
if not defined CSC (
    if exist "%WINDIR%\Microsoft.NET\Framework\v3.5\csc.exe" (
        set "CSC=%WINDIR%\Microsoft.NET\Framework\v3.5\csc.exe"
    )
)
:: Try .NET Framework 2.0
if not defined CSC (
    if exist "%WINDIR%\Microsoft.NET\Framework\v2.0.50727\csc.exe" (
        set "CSC=%WINDIR%\Microsoft.NET\Framework\v2.0.50727\csc.exe"
    )
)

if not defined CSC (
    echo ОШИБКА: Компилятор C# (csc.exe) не найден!
    echo Установите .NET Framework (обычно уже есть в Windows).
    pause
    exit /b 1
)

echo Компилятор: %CSC%
echo.

:: Create output directory
if not exist dist mkdir dist

:: Compile
echo Компиляция...
"%CSC%" /nologo /target:winexe /out:dist\chess-T1.exe /optimize+ ^
    /reference:System.dll ^
    /reference:System.Drawing.dll ^
    /reference:System.Windows.Forms.dll ^
    /reference:System.Web.Extensions.dll ^
    csharp\Program.cs ^
    csharp\Pieces.cs ^
    csharp\GameSession.cs ^
    csharp\BoardControl.cs ^
    csharp\MainForm.cs ^
    csharp\Dialogs.cs ^
    csharp\IntroPage.cs ^
    csharp\PieceTray.cs ^
    csharp\Properties\AssemblyInfo.cs

if errorlevel 1 (
    echo.
    echo ОШИБКА компиляции! Проверьте вывод выше.
    pause
    exit /b 1
)

echo.
echo ============================================================
echo    ГОТОВО!
echo
echo    Файл: dist\chess-T1.exe
echo    Размер:
for %%A in (dist\chess-T1.exe) do echo       %%~zA байт
echo.
echo    Этот файл работает на Windows XP, 7, 10, 11
echo    Ничего дополнительно ставить НЕ нужно.
echo    Просто передайте chess-T1.exe заказчику.
echo ============================================================
echo.
pause
