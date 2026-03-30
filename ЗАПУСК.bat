@echo off
chcp 65001 >nul 2>&1

:: Try portable Python first (from СБОРКА.bat)
if exist "%~dp0python\python.exe" (
    start "" "%~dp0python\pythonw.exe" "%~dp0run.py"
    exit /b 0
)

:: Try portable Python in parent (if running from dist_standalone)
if exist "%~dp0..\python\python.exe" (
    start "" "%~dp0..\python\pythonw.exe" "%~dp0run.py"
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
