@echo off
chcp 65001 >nul 2>&1
title chess-T1 — Сборка автономного пакета

echo ============================================================
echo    chess-T1 — Создание автономного дистрибутива
echo    (Python будет встроен, заказчику ничего не нужно ставить)
echo ============================================================
echo.

set "BUILD_DIR=%~dp0dist_standalone"
set "PYTHON_DIR=%BUILD_DIR%\python"
set "APP_DIR=%BUILD_DIR%"
set "PY_VERSION=3.11.9"
set "PY_URL=https://www.python.org/ftp/python/%PY_VERSION%/python-%PY_VERSION%-embed-amd64.zip"
set "PIP_URL=https://bootstrap.pypa.io/get-pip.py"

:: Clean
if exist "%BUILD_DIR%" rmdir /s /q "%BUILD_DIR%"
mkdir "%BUILD_DIR%"
mkdir "%PYTHON_DIR%"

:: Step 1: Download embedded Python
echo [1/5] Скачиваю портативный Python %PY_VERSION%...
powershell -Command "Invoke-WebRequest -Uri '%PY_URL%' -OutFile '%TEMP%\python_embed.zip'" 2>nul
if errorlevel 1 (
    curl -L -o "%TEMP%\python_embed.zip" "%PY_URL%" 2>nul
)
if not exist "%TEMP%\python_embed.zip" (
    echo ОШИБКА: Не удалось скачать Python. Проверьте интернет.
    pause
    exit /b 1
)

echo [2/5] Распаковываю Python...
powershell -Command "Expand-Archive -Path '%TEMP%\python_embed.zip' -DestinationPath '%PYTHON_DIR%' -Force"

:: Enable pip in embedded Python (uncomment import site)
echo [3/5] Настраиваю pip...
powershell -Command "(Get-Content '%PYTHON_DIR%\python311._pth') -replace '#import site','import site' | Set-Content '%PYTHON_DIR%\python311._pth'"

:: Download and run get-pip.py
powershell -Command "Invoke-WebRequest -Uri '%PIP_URL%' -OutFile '%TEMP%\get-pip.py'" 2>nul
if errorlevel 1 (
    curl -L -o "%TEMP%\get-pip.py" "%PIP_URL%" 2>nul
)
"%PYTHON_DIR%\python.exe" "%TEMP%\get-pip.py" --no-warn-script-location >nul 2>&1

:: Step 4: Install Pillow
echo [4/5] Устанавливаю зависимости (Pillow)...
"%PYTHON_DIR%\python.exe" -m pip install pillow --quiet --no-warn-script-location >nul 2>&1

:: Step 5: Copy app files
echo [5/5] Копирую файлы приложения...
xcopy /E /I /Q "%~dp0src" "%APP_DIR%\src" >nul
copy "%~dp0run.py" "%APP_DIR%\" >nul

:: Create launcher
(
echo @echo off
echo chcp 65001 ^>nul 2^>^&1
echo start "" "%%~dp0python\python.exe" "%%~dp0run.py"
) > "%APP_DIR%\chess-T1.bat"

:: Create VBS launcher (no console window)
(
echo Set WshShell = CreateObject("WScript.Shell"^)
echo WshShell.Run Chr(34^) ^& CreateObject("Scripting.FileSystemObject"^).GetParentFolderName(WScript.ScriptFullName^) ^& "\python\pythonw.exe" ^& Chr(34^) ^& " " ^& Chr(34^) ^& CreateObject("Scripting.FileSystemObject"^).GetParentFolderName(WScript.ScriptFullName^) ^& "\run.py" ^& Chr(34^), 0
) > "%APP_DIR%\chess-T1.vbs"

:: Cleanup
del "%TEMP%\python_embed.zip" 2>nul
del "%TEMP%\get-pip.py" 2>nul

echo.
echo ============================================================
echo    ГОТОВО! Дистрибутив создан в папке:
echo    %BUILD_DIR%
echo.
echo    Для запуска: двойной клик по chess-T1.bat
echo    (или chess-T1.vbs для запуска без консоли)
echo.
echo    Эту папку можно скопировать на любой ПК
echo    с Windows — Python ставить НЕ нужно.
echo ============================================================
pause
