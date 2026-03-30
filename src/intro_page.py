"""
Intro page (A00) for chess-T1.
Design matches the mockup: "Вводная страница ГИ chess-Т1 2026"
"""

import tkinter as tk
from tkinter import font as tkfont
import os
import sys


INTRO_TEXT = """Как пользоваться программой ГИ chess-T1
(вводный текст)

Настоящая программа ГИ chess-T1 (Графический интерфейс) пользователям игры chess-T1 предоставляет возможности с помощью компьютера:

(1) последовательно (пользователям) проводить анализ партий и позиций игры chess-T1.

(2) двум пользователям играть между собой в игру chess-T1.

Предполагается, что пользователи знают и соблюдают при использовании ГИ chess-T1 правила игры.

ОСНОВНЫЕ ЭЛЕМЕНТЫ ИНТЕРФЕЙСА

Игровое поле — доска 8×8 с буквенно-цифровой нотацией (вертикали a–h, горизонтали 1–8).

Клетки замков — выделены светло-серым цветом:
  \u2022 Замок белых: c1, d1, e1, f1
  \u2022 Замок чёрных: c8, d8, e8, f8

ФИГУРЫ

В игре 7 видов фигур для каждого цвета:
  \u2022 Король (Кр) — королевская фигура
  \u2022 Коннет (Кт) — королевская фигура
  \u2022 Принц (Пр) — королевская фигура, может превращаться
  \u2022 Риттер (Рт) — боевая фигура
  \u2022 Кнехт (Кн) — пехота, превращается в Вер Кнехта
  \u2022 Вер Кнехт (ВК) — ветеран, превращается далее
  \u2022 Разведчик (Рк) — особый ход: взятие с разменом

КАК ХОДИТЬ

Перетаскивание (Drag & Drop): нажмите и удерживайте левую кнопку мыши на фигуре, перетащите на целевую клетку и отпустите. Фигура автоматически встанет по центру клетки.

Подсветка:
  \u2022 При начале хода подсвечивается стартовая клетка
  \u2022 При перетаскивании подсвечивается клетка под курсором
  \u2022 После хода целевая клетка слабо подсвечена

РЕЖИМЫ ИГРЫ

1. Партия — режим игры двух игроков:
   \u2022 Нажмите кнопку «Партия»
   \u2022 Создайте папку для сохранения партии
   \u2022 Играйте, соблюдая очерёдность ходов

2. Анализ — режим анализа позиций:
   \u2022 Нажмите кнопку «Анализ»
   \u2022 Расставьте фигуры из касс (перетаскивание)
   \u2022 Выберите, кто ходит первым
   \u2022 Нажмите «Готово» для начала анализа

ПРЕВРАЩЕНИЯ ФИГУР

\u2022 Кнехт автоматически превращается в Вер Кнехта:
  — Белый Кнехт на 6-й горизонтали \u2192 белый ВК
  — Чёрный Кнехт на 3-й горизонтали \u2192 чёрный ВК

\u2022 Вер Кнехт превращается по выбору на крайних клетках
\u2022 Принц превращается по выбору на центральных клетках замка

СПЕЦИАЛЬНЫЙ ХОД РАЗВЕДЧИКА

Взятие с разменом: когда Разведчик ходит на клетку замка, занятую фигурой противника, обе фигуры исчезают с доски.

КНОПКИ УПРАВЛЕНИЯ

Верхний ряд:
  \u2022 Начальная расстановка — сбросить фигуры в начальную позицию
  \u2022 Партия — начать режим партии
  \u2022 Анализ — начать режим анализа
  \u2022 Свернуть / Поверх всех окон / Закрыть

Нижний ряд:
  \u2022 Меню — дополнительные команды
  \u2022 \u25c0 Предыдущий ход / Следующий ход \u25b6
  \u2022 Удалить фигуру
  \u2022 Реверс — перевернуть доску
  \u2022 Изменить размер

МЕНЮ

  \u2022 О программе — показать эту страницу
  \u2022 Сохранить позицию — скриншот текущей позиции
  \u2022 Сохранить позицию как — скриншот с выбором имени
  \u2022 Завершить партию — завершить текущий сеанс
  \u2022 Создать ярлык — ярлык на рабочем столе
  \u2022 Выход — закрыть программу

Приятной игры!
"""


def _load_intro_text():
    """Load intro text from external file if available.
    Looks for 'intro_text.txt' in assets/ directory.
    Falls back to built-in INTRO_TEXT.
    """
    if getattr(sys, 'frozen', False):
        base = sys._MEIPASS
    else:
        base = os.path.dirname(os.path.abspath(__file__))

    for path in [
        os.path.join(base, "assets", "intro_text.txt"),
        os.path.join(base, "..", "assets", "intro_text.txt"),
    ]:
        if os.path.isfile(path):
            try:
                with open(path, "r", encoding="utf-8") as f:
                    return f.read()
            except Exception:
                pass
    return INTRO_TEXT


# Colors matching the PDF mockup
COLOR_FRAME_BG = "#4A90C8"       # Blue frame background
COLOR_TEXT_BG = "#D4C9A8"        # Beige/tan text area background
COLOR_TEXT_FG = "#1A1A1A"        # Dark text
COLOR_BTN_MAIN = "#5DADE2"       # "Основной режим" button (cyan/blue)
COLOR_BTN_SKIP = "#7FB3D8"       # Skip buttons
COLOR_BTN_CTRL = "#5B9BD5"       # Window control buttons
COLOR_BTN_CLOSE = "#C0392B"      # Close button red


class IntroPage(tk.Toplevel):
    """Intro/About page window — matches PDF mockup design."""

    def __init__(self, parent, app, on_close=None, skip_callback=None):
        super().__init__(parent)
        self.app = app
        self.on_close_callback = on_close
        self.skip_callback = skip_callback

        self.title("chess-T1 — О программе")
        self.configure(bg=COLOR_FRAME_BG)

        # Match parent window size (spec: width equals main view width)
        try:
            pw = parent.winfo_width()
            ph = parent.winfo_height()
            self.geometry(f"{pw}x{ph}+{parent.winfo_x()}+{parent.winfo_y()}")
        except Exception:
            self.geometry("800x600")

        self.resizable(True, True)
        self.transient(parent)
        self.grab_set()
        self.bind("<Escape>", lambda e: self._close())

        self._build_ui()

    def _build_ui(self):
        """Build intro page matching the PDF mockup."""
        # Outer blue frame with padding
        outer = tk.Frame(self, bg=COLOR_FRAME_BG, padx=8, pady=8)
        outer.pack(fill="both", expand=True)

        # Top bar: symbol + buttons
        top_bar = tk.Frame(outer, bg=COLOR_FRAME_BG)
        top_bar.pack(fill="x", pady=(0, 6))

        # Program symbol (decorative, left side) — chess piece icon
        symbol_label = tk.Label(
            top_bar, text="\u265a", font=("Arial", 28),
            fg="#1A237E", bg=COLOR_FRAME_BG
        )
        symbol_label.pack(side="left", padx=(5, 10))

        # "Основной режим" button (closes intro, opens main view)
        btn_main = tk.Button(
            top_bar, text="Основной режим",
            font=("Arial", 10, "bold"), command=self._go_to_main,
            bg=COLOR_BTN_MAIN, fg="white", relief="raised",
            padx=12, pady=4, bd=2
        )
        btn_main.pack(side="left", padx=5)

        # Window control buttons (right side): Minimize, OnTop, Close
        ctrl_frame = tk.Frame(top_bar, bg=COLOR_FRAME_BG)
        ctrl_frame.pack(side="right")

        btn_minimize = tk.Button(
            ctrl_frame, text="\u2014", font=("Arial", 11, "bold"),
            command=self._minimize, width=3,
            bg=COLOR_BTN_CTRL, fg="white", relief="raised", bd=1
        )
        btn_minimize.pack(side="left", padx=2)

        self._on_top = False
        self.btn_ontop = tk.Button(
            ctrl_frame, text="\u25a0", font=("Arial", 10),
            command=self._toggle_on_top, width=3,
            bg=COLOR_BTN_CTRL, fg="white", relief="raised", bd=1
        )
        self.btn_ontop.pack(side="left", padx=2)

        btn_close = tk.Button(
            ctrl_frame, text="\u2715", font=("Arial", 11, "bold"),
            command=self._close, width=3,
            bg=COLOR_BTN_CLOSE, fg="white", relief="raised", bd=1
        )
        btn_close.pack(side="left", padx=2)

        # Skip buttons row
        skip_bar = tk.Frame(outer, bg=COLOR_FRAME_BG)
        skip_bar.pack(fill="x", pady=(0, 6))

        btn_skip = tk.Button(
            skip_bar, text="Пропустить вводный текст /\nперейти в основной режим",
            font=("Arial", 8), command=self._skip,
            bg=COLOR_BTN_SKIP, fg="#333", padx=8, pady=2, relief="raised", bd=1
        )
        btn_skip.pack(side="left", padx=5)

        btn_skip_forever = tk.Button(
            skip_bar, text="Пропустить и не\nпоказывать больше",
            font=("Arial", 8), command=self._skip_forever,
            bg=COLOR_BTN_SKIP, fg="#333", padx=8, pady=2, relief="raised", bd=1
        )
        btn_skip_forever.pack(side="left", padx=5)

        # Scrollable text area (beige/tan background matching mockup)
        text_frame = tk.Frame(outer, bg=COLOR_TEXT_BG, bd=2, relief="sunken")
        text_frame.pack(fill="both", expand=True)

        scrollbar = tk.Scrollbar(text_frame)
        scrollbar.pack(side="right", fill="y")

        self.text_widget = tk.Text(
            text_frame, wrap="word", font=("Arial", 11),
            bg=COLOR_TEXT_BG, fg=COLOR_TEXT_FG, padx=20, pady=15,
            yscrollcommand=scrollbar.set, state="normal",
            spacing1=2, spacing3=2, relief="flat",
            selectbackground="#5DADE2", insertwidth=0
        )
        self.text_widget.pack(fill="both", expand=True)
        scrollbar.config(command=self.text_widget.yview)

        self.text_widget.insert("1.0", _load_intro_text())
        self.text_widget.config(state="disabled")

        # Resize grip (bottom right corner)
        grip_bar = tk.Frame(outer, bg=COLOR_FRAME_BG)
        grip_bar.pack(fill="x")

        grip = tk.Label(
            grip_bar, text="\u21f2", font=("Arial", 14, "bold"),
            fg="#2C5F8A", bg=COLOR_FRAME_BG, cursor="bottom_right_corner"
        )
        grip.pack(side="right", anchor="se", padx=2, pady=2)

    def _go_to_main(self):
        self._close()

    def _skip(self):
        self._close()

    def _skip_forever(self):
        if self.skip_callback:
            self.skip_callback()
        self._close()

    def _minimize(self):
        self.iconify()

    def _toggle_on_top(self):
        self._on_top = not self._on_top
        self.attributes("-topmost", self._on_top)

    def _close(self):
        self.grab_release()
        self.destroy()
        if self.on_close_callback:
            self.on_close_callback()
