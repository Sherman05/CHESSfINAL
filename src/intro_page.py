"""
Intro page (A00) for chess-T1.
Shows introductory text about how to use the program.
"""

import tkinter as tk
from tkinter import font as tkfont


INTRO_TEXT = """Как пользоваться программой ГИ chess-T1

Добро пожаловать в графический интерфейс для настольной игры chess-T1!

Эта программа позволяет:
• Играть вдвоём за одним компьютером
• Анализировать позиции и партии

ОСНОВНЫЕ ЭЛЕМЕНТЫ ИНТЕРФЕЙСА

Игровое поле — доска 8×8 с буквенно-цифровой нотацией (вертикали a–h, горизонтали 1–8).

Клетки замков — выделены светло-серым цветом:
  • Замок белых: c1, d1, e1, f1
  • Замок чёрных: c8, d8, e8, f8

ФИГУРЫ

В игре 7 видов фигур для каждого цвета:
  • Король (Кр) — королевская фигура
  • Коннет (Кт) — королевская фигура
  • Принц (Пр) — королевская фигура, может превращаться
  • Риттер (Рт) — боевая фигура
  • Кнехт (Кн) — пехота, превращается в Вер Кнехта
  • Вер Кнехт (ВК) — ветеран, превращается далее
  • Разведчик (Рк) — особый ход: взятие с разменом

КАК ХОДИТЬ

Перетаскивание (Drag & Drop): нажмите и удерживайте левую кнопку мыши на фигуре, перетащите на целевую клетку и отпустите. Фигура автоматически встанет по центру клетки.

Подсветка:
  • При начале хода подсвечивается стартовая клетка
  • При перетаскивании подсвечивается клетка под курсором
  • После хода целевая клетка слабо подсвечена

РЕЖИМЫ ИГРЫ

1. Партия — режим игры двух игроков:
   • Нажмите кнопку «Партия»
   • Создайте папку для сохранения партии
   • Играйте, соблюдая очерёдность ходов

2. Анализ — режим анализа позиций:
   • Нажмите кнопку «Анализ»
   • Расставьте фигуры из касс (перетаскивание)
   • Выберите, кто ходит первым
   • Нажмите «Готово» для начала анализа

ПРЕВРАЩЕНИЯ ФИГУР

• Кнехт автоматически превращается в Вер Кнехта:
  — Белый Кнехт на 6-й горизонтали → белый ВК
  — Чёрный Кнехт на 3-й горизонтали → чёрный ВК

• Вер Кнехт превращается по выбору на крайних клетках
• Принц превращается по выбору на центральных клетках замка

СПЕЦИАЛЬНЫЙ ХОД РАЗВЕДЧИКА

Взятие с разменом: когда Разведчик ходит на клетку замка, занятую фигурой противника, обе фигуры исчезают с доски.

КНОПКИ УПРАВЛЕНИЯ

Верхний ряд:
  • Начальная расстановка — сбросить фигуры в начальную позицию
  • Партия — начать режим партии
  • Анализ — начать режим анализа
  • Свернуть / Поверх всех окон / Закрыть

Нижний ряд:
  • Меню — дополнительные команды
  • ◀ Предыдущий ход / Следующий ход ▶
  • Удалить фигуру
  • Реверс — перевернуть доску
  • Изменить размер

МЕНЮ

  • О программе — показать эту страницу
  • Сохранить позицию — скриншот текущей позиции
  • Сохранить позицию как — скриншот с выбором имени
  • Завершить партию — завершить текущий сеанс
  • Создать ярлык — ярлык на рабочем столе
  • Выход — закрыть программу

Приятной игры!
"""


class IntroPage(tk.Toplevel):
    """Intro/About page window."""

    def __init__(self, parent, app, on_close=None, skip_callback=None):
        super().__init__(parent)
        self.app = app
        self.on_close_callback = on_close
        self.skip_callback = skip_callback

        self.title("chess-T1 — О программе")
        self.configure(bg="#2C3E6B")

        # Try to match parent window size
        try:
            pw = parent.winfo_width()
            ph = parent.winfo_height()
            self.geometry(f"{pw}x{ph}+{parent.winfo_x()}+{parent.winfo_y()}")
        except Exception:
            self.geometry("800x600")

        self.resizable(True, True)
        self.transient(parent)
        self.grab_set()

        self._build_ui()

    def _build_ui(self):
        """Build intro page UI."""
        main_frame = tk.Frame(self, bg="#2C3E6B", padx=15, pady=15)
        main_frame.pack(fill="both", expand=True)

        # Header with program symbol and title
        header = tk.Frame(main_frame, bg="#2C3E6B")
        header.pack(fill="x", pady=(0, 10))

        # Program symbol (decorative)
        symbol_label = tk.Label(
            header, text="♚", font=("Arial", 36),
            fg="#FFD700", bg="#2C3E6B"
        )
        symbol_label.pack(side="left", padx=(10, 15))

        title_label = tk.Label(
            header, text="chess-T1",
            font=("Arial", 24, "bold"), fg="white", bg="#2C3E6B"
        )
        title_label.pack(side="left")

        # Top buttons
        btn_frame_top = tk.Frame(main_frame, bg="#2C3E6B")
        btn_frame_top.pack(fill="x", pady=(0, 10))

        btn_main = tk.Button(
            btn_frame_top, text="Основной режим",
            font=("Arial", 11), command=self._go_to_main,
            bg="#4CAF50", fg="white", relief="raised",
            padx=15, pady=5
        )
        btn_main.pack(side="left", padx=5)

        btn_skip = tk.Button(
            btn_frame_top, text="Пропустить вводный текст",
            font=("Arial", 10), command=self._skip,
            bg="#607D8B", fg="white", padx=10, pady=5
        )
        btn_skip.pack(side="left", padx=5)

        btn_skip_forever = tk.Button(
            btn_frame_top, text="Пропустить и не показывать больше",
            font=("Arial", 10), command=self._skip_forever,
            bg="#795548", fg="white", padx=10, pady=5
        )
        btn_skip_forever.pack(side="left", padx=5)

        # Window control buttons
        ctrl_frame = tk.Frame(btn_frame_top, bg="#2C3E6B")
        ctrl_frame.pack(side="right")

        btn_minimize = tk.Button(
            ctrl_frame, text="—", font=("Arial", 12),
            command=self._minimize, width=3, bg="#455A64", fg="white"
        )
        btn_minimize.pack(side="left", padx=2)

        self._on_top = False
        self.btn_ontop = tk.Button(
            ctrl_frame, text="📌", font=("Arial", 10),
            command=self._toggle_on_top, width=3, bg="#455A64", fg="white"
        )
        self.btn_ontop.pack(side="left", padx=2)

        btn_close = tk.Button(
            ctrl_frame, text="✕", font=("Arial", 12),
            command=self._close, width=3, bg="#C62828", fg="white"
        )
        btn_close.pack(side="left", padx=2)

        # Scrollable text area
        text_frame = tk.Frame(main_frame, bg="white", bd=2, relief="sunken")
        text_frame.pack(fill="both", expand=True)

        scrollbar = tk.Scrollbar(text_frame)
        scrollbar.pack(side="right", fill="y")

        self.text_widget = tk.Text(
            text_frame, wrap="word", font=("Arial", 12),
            bg="white", fg="#333333", padx=20, pady=15,
            yscrollcommand=scrollbar.set, state="normal",
            spacing1=2, spacing3=2
        )
        self.text_widget.pack(fill="both", expand=True)
        scrollbar.config(command=self.text_widget.yview)

        self.text_widget.insert("1.0", INTRO_TEXT)
        self.text_widget.config(state="disabled")

        # Resize grip
        grip = tk.Label(main_frame, text="⇲", font=("Arial", 14),
                        fg="#AAA", bg="#2C3E6B", cursor="bottom_right_corner")
        grip.pack(side="right", anchor="se")

    def _go_to_main(self):
        """Close intro and go to main view."""
        self._close()

    def _skip(self):
        """Skip intro without saving preference."""
        self._close()

    def _skip_forever(self):
        """Skip intro and remember preference."""
        if self.skip_callback:
            self.skip_callback()
        self._close()

    def _minimize(self):
        self.iconify()

    def _toggle_on_top(self):
        self._on_top = not self._on_top
        self.attributes("-topmost", self._on_top)

    def _close(self):
        """Close intro page."""
        self.grab_release()
        self.destroy()
        if self.on_close_callback:
            self.on_close_callback()
