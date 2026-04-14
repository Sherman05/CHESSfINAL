@echo off
chcp 65001 >nul 2>&1

:: Try portable Python first
if exist "%~dp0python\python.exe" (
    "%~dp0python\python.exe" "%~dp0run.py"
    if errorlevel 1 pause
    exit /b 0
)

:: Fallback: system Python
python --version >nul 2>&1
if errorlevel 1 (
    echo Python не найден.
    echo Запустите СБОРКА.bat для создания автономного пакета.
    pause
    exit /b 1
)

python -m pip install pillow --quiet --disable-pip-version-check >nul 2>&1
python "%~dp0run.py"
if errorlevel 1 pause
