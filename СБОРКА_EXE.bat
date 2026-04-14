@echo off
chcp 65001 >nul 2>&1
title chess-T1 — Сборка EXE

echo ============================================================
echo    chess-T1 — Сборка установщика
echo ============================================================
echo.
echo Этот скрипт создаст:
echo   1. chess-T1.exe (автономный, без Python)
echo   2. chess-T1-setup.exe (установщик для заказчика)
echo.
echo Требования: Python 3.10+ с pip (у тебя уже есть)
echo ============================================================
echo.

:: Step 1: Install build tools
echo [1/4] Устанавливаю PyInstaller...
pip install pyinstaller pillow --quiet --disable-pip-version-check
if errorlevel 1 (
    echo ОШИБКА: pip install не удался
    pause
    exit /b 1
)

:: Step 2: Build EXE
echo [2/4] Собираю chess-T1.exe ...
pyinstaller ^
    --name "chess-T1" ^
    --onefile ^
    --windowed ^
    --noconfirm ^
    --clean ^
    --add-data "src\icons;icons" ^
    --add-data "src\assets;assets" ^
    --hidden-import pieces ^
    --hidden-import board ^
    --hidden-import toolbar ^
    --hidden-import intro_page ^
    --hidden-import piece_tray ^
    --hidden-import dialogs ^
    --hidden-import game_session ^
    --paths src ^
    --distpath dist ^
    --workpath build ^
    --specpath build ^
    src\main.py

if errorlevel 1 (
    echo.
    echo ОШИБКА: Сборка не удалась. Проверьте вывод выше.
    pause
    exit /b 1
)

echo.
echo [3/4] chess-T1.exe создан: dist\chess-T1.exe

:: Check file size
for %%A in (dist\chess-T1.exe) do (
    set /a size_mb=%%~zA / 1048576
    echo       Размер: ~%%~zA байт
)

:: Step 3: Check for InnoSetup
echo.
echo [4/4] Проверяю InnoSetup для создания установщика...

set "ISCC="
if exist "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" set "ISCC=C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
if exist "C:\Program Files\Inno Setup 6\ISCC.exe" set "ISCC=C:\Program Files\Inno Setup 6\ISCC.exe"

if defined ISCC (
    echo       InnoSetup найден. Создаю установщик...
    "%ISCC%" "%~dp0installer.iss"
    if errorlevel 1 (
        echo ОШИБКА при создании установщика
    ) else (
        echo.
        echo ============================================================
        echo    ГОТОВО!
        echo.
        echo    Установщик: dist\chess-T1-setup.exe
        echo    Передайте этот файл заказчику.
        echo    Он запустит его, установит, и будет играть.
        echo ============================================================
    )
) else (
    echo       InnoSetup НЕ найден.
    echo.
    echo       Чтобы создать установщик (.exe как у Яндекс Браузера):
    echo       1. Скачайте InnoSetup: https://jrsoftware.org/isdl.php
    echo       2. Установите
    echo       3. Запустите этот скрипт снова
    echo.
    echo       Или передайте заказчику файл dist\chess-T1.exe напрямую.
    echo       Он работает без установки — просто двойной клик.
    echo ============================================================
)

echo.
pause
