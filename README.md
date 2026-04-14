# chess-T1

Графический интерфейс для настольной игры chess-T1.

## Запуск

```bash
python3 run.py
```

## Требования

- Python 3.10+
- tkinter (обычно идёт в составе Python)
- Pillow (опционально, для сохранения скриншотов)
- cairosvg (опционально, для рендеринга SVG иконок)

```bash
pip install -r requirements.txt
```

## Структура проекта

```
src/
  main.py          - главный модуль приложения
  board.py         - виджет игровой доски 8x8
  pieces.py        - определения фигур и игровая логика
  toolbar.py       - панели кнопок управления
  intro_page.py    - вводная страница (А00)
  piece_tray.py    - кассы фигур для режима Анализ
  dialogs.py       - диалоговые окна
  game_session.py  - управление сеансами и сохранение
  icons/           - SVG иконки фигур (14 шт.)
```

## Сборка Windows .exe

```bash
pip install pyinstaller
python build_windows.py
```
