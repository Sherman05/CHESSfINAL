"""
Toolbar buttons for chess-T1.
Top row and bottom row of control buttons.
"""

import tkinter as tk


# Button style constants
BTN_BG = "#37474F"
BTN_FG = "white"
BTN_ACTIVE_BG = "#546E7A"
BTN_DISABLED_BG = "#78909C"
BTN_HIGHLIGHT_BG = "#1B5E20"
BTN_FONT = ("Arial", 10)
BTN_FONT_SMALL = ("Arial", 9)
TOOLBAR_BG = "#263238"


class ToolButton(tk.Button):
    """Custom styled button with tooltip and freeze/highlight support."""

    def __init__(self, parent, text="", symbol="", tooltip="",
                 command=None, **kwargs):
        self._tooltip_text = tooltip
        self._frozen = False
        self._highlighted = False
        self._original_bg = kwargs.pop("bg", BTN_BG)
        self._original_fg = kwargs.pop("fg", BTN_FG)

        display_text = symbol if symbol else text
        super().__init__(
            parent, text=display_text, command=command,
            font=kwargs.pop("font", BTN_FONT),
            bg=self._original_bg, fg=self._original_fg,
            activebackground=BTN_ACTIVE_BG,
            activeforeground="white",
            relief="flat", bd=1, padx=8, pady=4,
            cursor="hand2",
            **kwargs
        )

        self._tooltip_window = None
        self.bind("<Enter>", self._show_tooltip)
        self.bind("<Leave>", self._hide_tooltip)

    def freeze(self):
        """Disable button (frozen state)."""
        self._frozen = True
        self.config(state="disabled", bg=BTN_DISABLED_BG, cursor="arrow")

    def unfreeze(self):
        """Enable button."""
        self._frozen = False
        bg = BTN_HIGHLIGHT_BG if self._highlighted else self._original_bg
        self.config(state="normal", bg=bg, cursor="hand2")

    def highlight(self, on=True):
        """Set highlight (active mode indicator)."""
        self._highlighted = on
        if not self._frozen:
            self.config(bg=BTN_HIGHLIGHT_BG if on else self._original_bg)

    def _show_tooltip(self, event):
        if not self._tooltip_text:
            return
        x = self.winfo_rootx() + self.winfo_width() // 2
        y = self.winfo_rooty() + self.winfo_height() + 5

        self._tooltip_window = tw = tk.Toplevel(self)
        tw.wm_overrideredirect(True)
        tw.wm_geometry(f"+{x}+{y}")
        tw.attributes("-topmost", True)

        label = tk.Label(
            tw, text=self._tooltip_text,
            bg="#FFFDE7", fg="#333", font=("Arial", 9),
            relief="solid", bd=1, padx=6, pady=3
        )
        label.pack()

    def _hide_tooltip(self, event):
        if self._tooltip_window:
            self._tooltip_window.destroy()
            self._tooltip_window = None


class TopToolbar(tk.Frame):
    """Top row of control buttons."""

    def __init__(self, parent, app, **kwargs):
        super().__init__(parent, bg=TOOLBAR_BG, **kwargs)
        self.app = app

        # Buttons match the PDF mockup "Расширенный вид ГИ"
        self.btn_initial = ToolButton(
            self, text="Начальная\nрасстановка", tooltip="Начальная расстановка",
            command=app.reset_to_initial, font=BTN_FONT_SMALL
        )
        self.btn_initial.pack(side="left", padx=2, pady=4)

        self.btn_party = ToolButton(
            self, text="Партия", tooltip="Режим Партия",
            command=app.start_party_mode, bg="#2E7D32"
        )
        self.btn_party.pack(side="left", padx=2, pady=4)

        self.btn_analysis = ToolButton(
            self, text="Анализ", tooltip="Режим Анализ",
            command=app.start_analysis_mode, bg="#1565C0"
        )
        self.btn_analysis.pack(side="left", padx=2, pady=4)

        # Spacer
        tk.Frame(self, bg=TOOLBAR_BG).pack(side="left", fill="x", expand=True)

        self.btn_minimize = ToolButton(
            self, symbol="\u2014", tooltip="Свернуть",
            command=app.minimize_window
        )
        self.btn_minimize.pack(side="left", padx=2, pady=4)

        self.btn_on_top = ToolButton(
            self, symbol="\u25a0", tooltip="Поверх всех окон",
            command=app.toggle_always_on_top
        )
        self.btn_on_top.pack(side="left", padx=2, pady=4)

        self.btn_close = ToolButton(
            self, symbol="\u2715", tooltip="Закрыть программу",
            command=app.close_app, bg="#C62828"
        )
        self.btn_close.pack(side="left", padx=2, pady=4)

    def update_states(self, mode, stage):
        """Update button states based on current mode/stage."""
        # Initial button: frozen in startup
        if stage == "startup":
            self.btn_initial.freeze()
        else:
            self.btn_initial.unfreeze()

        # Party button (sec 8.2):
        # In party mode: frozen + highlighted
        # In analysis setup: frozen
        # In analysis game: active (ends session on click)
        # In startup: active
        self.btn_party.highlight(False)
        if mode == "party":
            self.btn_party.freeze()
            self.btn_party.highlight(True)
            self.btn_party.config(bg=BTN_HIGHLIGHT_BG)
        elif mode == "analysis" and stage == "setup_position":
            self.btn_party.freeze()
        else:
            self.btn_party.unfreeze()

        # Analysis button (sec 8.3):
        # In analysis mode (any stage): frozen + highlighted
        # In party mode: active (ends session on click)
        # In startup: active
        self.btn_analysis.highlight(False)
        if mode == "analysis":
            self.btn_analysis.freeze()
            self.btn_analysis.highlight(True)
            self.btn_analysis.config(bg=BTN_HIGHLIGHT_BG)
        else:
            self.btn_analysis.unfreeze()


class BottomToolbar(tk.Frame):
    """Bottom row of control buttons."""

    def __init__(self, parent, app, **kwargs):
        super().__init__(parent, bg=TOOLBAR_BG, **kwargs)
        self.app = app

        # Menu button: circle with 3 horizontal lines (hamburger icon from SVG)
        self.btn_menu = ToolButton(
            self, symbol="\u2630", tooltip="Меню",
            command=app.show_menu
        )
        self.btn_menu.pack(side="left", padx=2, pady=4)

        # Move indicator
        self.indicator_var = tk.StringVar(value="")
        self.indicator_label = tk.Label(
            self, textvariable=self.indicator_var,
            font=("Arial", 11, "bold"), fg="#FFD700", bg=TOOLBAR_BG,
            padx=10
        )
        self.indicator_label.pack(side="left", padx=5)

        self.btn_prev = ToolButton(
            self, symbol="\u25c0", tooltip="Предыдущий ход",
            command=app.prev_move
        )
        self.btn_prev.pack(side="left", padx=2, pady=4)

        self.btn_next = ToolButton(
            self, symbol="\u25b6", tooltip="Следующий ход",
            command=app.next_move
        )
        self.btn_next.pack(side="left", padx=2, pady=4)

        self.btn_delete = ToolButton(
            self, symbol="\u2716", tooltip="Удалить фигуру",
            command=app.delete_piece_mode
        )
        self.btn_delete.pack(side="left", padx=2, pady=4)

        self.btn_reverse = ToolButton(
            self, symbol="\u21c5", tooltip="Реверс (перевернуть доску)",
            command=app.reverse_board
        )
        self.btn_reverse.pack(side="left", padx=2, pady=4)

        # Analysis-specific buttons (hidden by default)
        self.btn_reset = ToolButton(
            self, text="Сброс", tooltip="Очистить поле",
            command=app.analysis_reset, font=BTN_FONT_SMALL
        )

        # "1-й ход" button: shows white/black rectangles with indicator (from SVG mockup)
        self.btn_first_move = ToolButton(
            self, text="1-й ход", tooltip="Выбор очерёдности первого хода",
            command=app.analysis_toggle_first_move, font=BTN_FONT_SMALL
        )

        # "Ok" button: matches SVG circle with "Ok" text
        self.btn_ok = ToolButton(
            self, text="Ok", tooltip="Зафиксировать позицию",
            command=app.analysis_confirm, bg="#4A90C8",
            font=("Arial", 11, "bold")
        )

        # Spacer
        tk.Frame(self, bg=TOOLBAR_BG).pack(side="left", fill="x", expand=True)

        # Resize grip
        self.grip = tk.Label(
            self, text="⇲", font=("Arial", 14),
            fg="#888", bg=TOOLBAR_BG, cursor="bottom_right_corner"
        )
        self.grip.pack(side="right", padx=5)

    def show_analysis_buttons(self, show=True):
        """Show/hide analysis-specific buttons."""
        if show:
            self.btn_reset.pack(side="left", padx=2, pady=4, after=self.btn_delete)
            self.btn_first_move.pack(side="left", padx=2, pady=4, after=self.btn_reset)
            self.btn_ok.pack(side="left", padx=2, pady=4, after=self.btn_first_move)
        else:
            self.btn_reset.pack_forget()
            self.btn_first_move.pack_forget()
            self.btn_ok.pack_forget()

    def update_states(self, mode, stage):
        """Update button states based on current mode/stage."""
        # Prev/Next: frozen in startup and setup_position
        if stage in ("startup", "setup_position"):
            self.btn_prev.freeze()
            self.btn_next.freeze()
        else:
            self.btn_prev.unfreeze()
            self.btn_next.unfreeze()

        # Reverse: frozen in startup
        if stage == "startup":
            self.btn_reverse.freeze()
        else:
            self.btn_reverse.unfreeze()

        # Delete: available in analysis mode and in startup (if razvedchik not implemented via special move)
        self.btn_delete.unfreeze()

        # Show analysis buttons only in setup_position stage
        self.show_analysis_buttons(stage == "setup_position")

        # Move indicator: only in game stage
        if stage == "game":
            self.indicator_label.pack(side="left", padx=5, after=self.btn_menu)
        else:
            self.indicator_var.set("")

    def set_indicator(self, text):
        """Set the move indicator text."""
        self.indicator_var.set(text)
