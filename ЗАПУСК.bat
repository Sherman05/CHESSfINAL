@echo off
chcp 65001 >nul 2>&1
title chess-T1

echo ============================================
echo    chess-T1 — Запуск программы
echo ============================================
echo.

:: Check Python
python --version >nul 2>&1
if errorlevel 1 (
    echo [ОШИБКА] Python не найден!
    echo.
    echo Скачайте Python с https://www.python.org/downloads/
    echo При установке ОБЯЗАТЕЛЬНО поставьте галочку "Add Python to PATH"
    echo.
    pause
    exit /b 1
)

:: Install dependencies silently
echo Проверка зависимостей...
python -m pip install pillow --quiet --disable-pip-version-check >nul 2>&1

:: Launch
echo Запуск chess-T1...
python "%~dp0run.py"
if errorlevel 1 (
    echo.
    echo [ОШИБКА] Программа завершилась с ошибкой.
    pause
)
