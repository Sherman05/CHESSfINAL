"""
chess-T1 — Graphical Interface for the chess-T1 board game.
Main application entry point.
"""

import tkinter as tk
from tkinter import messagebox
import os
import sys

from pieces import (
    Piece, WHITE, BLACK, get_initial_position, copy_position,
    KONNET, PRINCE, RITTER, RAZVEDCHIK, KNEKHT, VER_KNEKHT, KING,
    PIECE_SHORT_NAMES, ALL_PIECE_TYPES
)
from board import GameBoard
from toolbar import TopToolbar, BottomToolbar
from intro_page import IntroPage
from piece_tray import PieceTray
from dialogs import (
    CreateFolderDialog, PromotionDialog, EndSessionDialog,
    CloseAppDialog, SaveAsDialog
)
from game_session import (
    load_config, save_config, save_session, load_session,
    clear_session, get_screenshot_name, get_screenshot_dir,
    save_screenshot, get_indicator_text
)


def _get_base_dir():
    """Get the base directory of the application.
    Handles both normal Python execution and PyInstaller frozen bundle.
    """
    if getattr(sys, 'frozen', False):
        # Running as PyInstaller bundle
        return sys._MEIPASS
    return os.path.dirname(os.path.abspath(__file__))


def _get_executable_path():
    """Get path to the executable/script for shortcuts."""
    if getattr(sys, 'frozen', False):
        return sys.executable
    return os.path.abspath(__file__)


def _get_desktop_path():
    """Get the user's Desktop path cross-platform."""
    if sys.platform == "win32":
        try:
            import winreg
            key = winreg.OpenKey(
                winreg.HKEY_CURRENT_USER,
                r"Software\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders"
            )
            desktop, _ = winreg.QueryValueEx(key, "Desktop")
            winreg.CloseKey(key)
            if os.path.isdir(desktop):
                return desktop
        except Exception:
            pass
    desktop = os.path.join(os.path.expanduser("~"), "Desktop")
    if os.path.isdir(desktop):
        return desktop
    return os.path.expanduser("~")


def _create_windows_shortcut(shortcut_path, target_path):
    """Create a Windows .lnk shortcut using COM."""
    try:
        # Try pythoncom/win32com (PyWin32)
        from win32com.client import Dispatch
        shell = Dispatch("WScript.Shell")
        shortcut = shell.CreateShortCut(shortcut_path)
        shortcut.Targetpath = target_path
        shortcut.WorkingDirectory = os.path.dirname(target_path)
        shortcut.Description = "chess-T1"
        shortcut.save()
    except ImportError:
        # Fallback: create a .bat launcher if COM not available
        bat_path = shortcut_path.replace(".lnk", ".bat")
        with open(bat_path, "w", encoding="cp1251") as f:
            if getattr(sys, 'frozen', False):
                f.write(f'@echo off\nstart "" "{target_path}"\n')
            else:
                f.write(f'@echo off\npython "{target_path}"\n')


class ChessT1App:
    """Main application class for chess-T1."""

    def __init__(self):
        self.root = tk.Tk()
        self.root.title("chess-T1")
        self.root.configure(bg="#263238")

        # Maximize window
        try:
            self.root.state("zoomed")
        except tk.TclError:
            try:
                self.root.attributes("-zoomed", True)
            except tk.TclError:
                self.root.geometry("1024x768")

        self.root.minsize(700, 500)
        self.root.protocol("WM_DELETE_WINDOW", self.close_app)

        # Application state
        self.mode = None  # None, "party", "analysis"
        self.stage = "startup"  # "startup", "game", "setup_position"
        self.session_active = False
        self.always_on_top = False
        self.board_reversed = False
        self.move_number = 1
        self.white_turn = True
        self.move_history = []
        self.history_index = -1
        self.party_folder = None
        self.analysis_first_white = True
        self.promotion_pending = False

        # Config
        self.config = load_config()

        # Build UI
        self._build_ui()

        # Load piece images
        self.board.load_piece_images()

        # Check for saved session (section 10.4)
        saved_session = load_session()
        if saved_session:
            self._restore_session(saved_session)
        elif not self.config.get("skip_intro", False):
            self.root.after(200, self.show_intro_page)
        else:
            self._enter_startup()

        self._update_all_states()

    def _build_ui(self):
        """Build the main UI layout."""
        # Main container
        self.main_frame = tk.Frame(self.root, bg="#263238")
        self.main_frame.pack(fill="both", expand=True)

        # Top toolbar
        self.top_toolbar = TopToolbar(self.main_frame, self)
        self.top_toolbar.pack(fill="x", side="top")

        # Bottom toolbar
        self.bottom_toolbar = BottomToolbar(self.main_frame, self)
        self.bottom_toolbar.pack(fill="x", side="bottom")

        # Center area (trays + board)
        self.center_frame = tk.Frame(self.main_frame, bg="#263238")
        self.center_frame.pack(fill="both", expand=True)

        # Board
        self.board = GameBoard(
            self.center_frame, self,
            bg="#263238", highlightthickness=0
        )
        self.board.pack(fill="both", expand=True, padx=10, pady=10)

        # Trays (created but not shown)
        self.left_tray = None
        self.right_tray = None

    def _update_all_states(self):
        """Update all button states and indicators."""
        self.top_toolbar.update_states(self.mode, self.stage)
        self.bottom_toolbar.update_states(self.mode, self.stage)
        self._update_indicator()

    def _update_indicator(self):
        """Update the move indicator string."""
        if self.stage != "game":
            self.bottom_toolbar.set_indicator("")
            return

        text = get_indicator_text(self.move_number, self.white_turn)
        self.bottom_toolbar.set_indicator(text)

    def _enter_startup(self):
        """Enter startup state (no active mode)."""
        self.mode = None
        self.stage = "startup"
        self.session_active = False
        self.board.set_position(get_initial_position())
        self._hide_trays()
        self._update_all_states()

    # ---- Intro Page ----

    def show_intro_page(self):
        """Show the intro/about page."""
        IntroPage(
            self.root, self,
            on_close=self._on_intro_closed,
            skip_callback=self._skip_intro_forever
        )

    def _on_intro_closed(self):
        """Called when intro page is closed.
        Returns to the same mode and position that was before (spec 3.2).
        """
        if self.mode is not None:
            # Already in a mode (party/analysis) — just return, don't reset
            return
        if not self.session_active:
            self._enter_startup()

    def _skip_intro_forever(self):
        """Save preference to skip intro."""
        self.config["skip_intro"] = True
        save_config(self.config)

    # ---- Party Mode (Section 8) ----

    def start_party_mode(self):
        """Start Party mode."""
        if self.mode == "party":
            return

        if self.mode == "analysis" and self.stage == "game":
            self._end_session_no_dialog()
            return

        if self.mode == "analysis" and self.stage == "setup_position":
            return  # Frozen

        # From startup: open folder dialog
        CreateFolderDialog(
            self.root,
            on_created=self._on_party_folder_created,
            on_cancel=lambda: self._on_party_folder_created(None)
            # Spec 8.1: game starts even if folder dialog cancelled
        )

    def _on_party_folder_created(self, folder_path):
        """Party folder created, enter game mode."""
        self.party_folder = folder_path
        self.mode = "party"
        self.stage = "game"
        self.session_active = True
        self.move_number = 1
        self.white_turn = True
        self.move_history = [copy_position(get_initial_position())]
        self.history_index = 0

        self.board.set_position(get_initial_position())
        self._hide_trays()
        self._update_all_states()

    # ---- Analysis Mode (Section 9) ----

    def start_analysis_mode(self):
        """Start Analysis mode."""
        if self.mode == "analysis":
            return

        if self.mode == "party":
            self._end_session_no_dialog()
            return

        # From startup: enter setup position stage
        self.mode = "analysis"
        self.stage = "setup_position"
        self.session_active = False
        self.analysis_first_white = True

        self.board.clear_board()
        self._show_trays()
        self._update_all_states()
        self._update_first_move_button()

    def _show_trays(self):
        """Show piece trays for analysis mode."""
        self.board.pack_forget()

        left_color = WHITE if not self.board_reversed else BLACK
        right_color = BLACK if not self.board_reversed else WHITE

        self.left_tray = PieceTray(
            self.center_frame, left_color, self.board,
            self.board.piece_images, self.board.cell_size
        )
        self.left_tray.pack(side="left", fill="y", padx=5, pady=10)

        self.board.pack(side="left", fill="both", expand=True, padx=10, pady=10)

        self.right_tray = PieceTray(
            self.center_frame, right_color, self.board,
            self.board.piece_images, self.board.cell_size
        )
        self.right_tray.pack(side="left", fill="y", padx=5, pady=10)

    def _hide_trays(self):
        """Hide piece trays."""
        if self.left_tray:
            self.left_tray.destroy()
            self.left_tray = None
        if self.right_tray:
            self.right_tray.destroy()
            self.right_tray = None

        self.board.pack_forget()
        self.board.pack(fill="both", expand=True, padx=10, pady=10)

    def analysis_reset(self):
        """Clear the board in analysis setup."""
        if self.stage == "setup_position":
            self.board.clear_board()

    def analysis_toggle_first_move(self):
        """Toggle who makes the first move in analysis."""
        self.analysis_first_white = not self.analysis_first_white
        self._update_first_move_button()

    def _update_first_move_button(self):
        """Update the first move button appearance."""
        if self.analysis_first_white:
            self.bottom_toolbar.btn_first_move.config(
                text="1-й ход: Б", bg="#EEEEEE", fg="#333"
            )
        else:
            self.bottom_toolbar.btn_first_move.config(
                text="1-й ход: Ч", bg="#333333", fg="white"
            )

    def analysis_confirm(self):
        """Confirm position setup and enter game stage."""
        if self.stage != "setup_position":
            return

        position = self.board.get_position()
        if not position:
            messagebox.showwarning(
                "Пустое поле",
                "Расставьте фигуры перед началом анализа.",
                parent=self.root
            )
            return

        CreateFolderDialog(
            self.root,
            on_created=lambda path: self._on_analysis_confirmed(path, position),
            on_cancel=None  # Stay in setup_position if cancelled
        )

    def _on_analysis_confirmed(self, folder_path, position):
        """Analysis position confirmed, enter game stage."""
        self.party_folder = folder_path
        self.stage = "game"
        self.session_active = True
        self.white_turn = self.analysis_first_white
        self.move_number = 1
        self.move_history = [copy_position(position)]
        self.history_index = 0

        self._hide_trays()
        self.board.set_position(position)
        self._update_all_states()

    # ---- Board Actions ----

    def reset_to_initial(self):
        """Reset to initial position (section 6.1).
        In Analysis setup: sets initial position but STAYS in setup (sec 9.1).
        In Party/Analysis game: ends session, goes to startup.
        """
        if self.stage == "startup":
            return

        # Analysis setup: place initial position, stay in extended view
        if self.mode == "analysis" and self.stage == "setup_position":
            self.board.set_position(get_initial_position())
            return

        if self.session_active:
            self._end_session_no_dialog()
            return

        self._enter_startup()

    def reverse_board(self):
        """Reverse board orientation (section 6.2)."""
        if self.stage == "startup":
            return

        self.board_reversed = not self.board_reversed
        self.board.reverse()

        if self.stage == "setup_position":
            self._hide_trays()
            self._show_trays()

    def prev_move(self):
        """Go to previous move in history."""
        if self.stage != "game":
            return
        if self.history_index <= 0:
            return

        self.history_index -= 1
        self.board.set_position(self.move_history[self.history_index])
        self._recalculate_turn_from_history()
        self._update_indicator()

    def next_move(self):
        """Go to next move in history."""
        if self.stage != "game":
            return
        if self.history_index >= len(self.move_history) - 1:
            return

        self.history_index += 1
        self.board.set_position(self.move_history[self.history_index])
        self._recalculate_turn_from_history()
        self._update_indicator()

    def _recalculate_turn_from_history(self):
        """Recalculate move number and turn from history index."""
        idx = self.history_index
        if self.mode == "analysis" and not self.analysis_first_white:
            self.white_turn = (idx % 2 == 1)
            self.move_number = (idx // 2) + 1
        else:
            self.white_turn = (idx % 2 == 0)
            self.move_number = (idx // 2) + 1

    def delete_piece_mode(self):
        """Enter piece deletion mode."""
        if self.board.selected_for_deletion:
            self.board.delete_selected_piece()
        else:
            messagebox.showinfo(
                "Удаление фигуры",
                "Нажмите на фигуру, которую хотите удалить,\nзатем нажмите эту кнопку снова.",
                parent=self.root
            )

    # ---- Move Handling ----

    def on_move_made(self, from_cell, to_cell, exchange=False):
        """Called by board after a move is made."""
        if self.stage != "game":
            return

        # Trim history on new move after rollback (section 7.7)
        if self.history_index < len(self.move_history) - 1:
            self.move_history = self.move_history[:self.history_index + 1]

        self.move_history.append(self.board.get_position())
        self.history_index = len(self.move_history) - 1

        self.white_turn = not self.white_turn
        if self.white_turn:
            self.move_number += 1

        self._update_indicator()
        self._auto_save_session()

    def on_piece_exited_board(self):
        """Called when a piece exits the board (section 7.6)."""
        if self.stage == "game":
            self.on_move_made(None, None)

    def show_promotion_dialog(self, cell, piece, options):
        """Show piece promotion choice dialog (section 11)."""
        self.promotion_pending = True
        self.top_toolbar.btn_minimize.freeze()

        position = "top_right" if piece.color == WHITE else "bottom_right"

        PromotionDialog(
            self.root, piece, options,
            self.board.piece_images, self.board.cell_size,
            position=position,
            on_chosen=lambda ptype: self._on_promotion_chosen(cell, piece, ptype)
        )

    def _on_promotion_chosen(self, cell, piece, chosen_type):
        """Handle promotion choice."""
        self.promotion_pending = False
        self.top_toolbar.btn_minimize.unfreeze()

        new_piece = Piece(piece.color, chosen_type)
        self.board.position[cell] = new_piece
        self.board.last_move_cell = cell
        self.board.redraw()
        self.on_move_made(None, cell)

    # ---- Menu ----

    def show_menu(self):
        """Show the menu popup (section 10.1)."""
        menu = tk.Menu(self.root, tearoff=0, font=("Arial", 11))

        menu.add_command(label="О программе", command=self.show_intro_page)
        menu.add_separator()

        state = "normal" if self.stage == "game" else "disabled"
        menu.add_command(label="Сохранить позицию",
                         command=self.save_position, state=state)
        menu.add_command(label="Сохранить позицию как...",
                         command=self.save_position_as, state=state)
        menu.add_separator()

        end_state = "normal" if self.session_active and self.stage == "game" else "disabled"
        menu.add_command(label="Завершить партию",
                         command=self.end_session_dialog, state=end_state)
        menu.add_separator()

        menu.add_command(label="Создать ярлык", command=self.create_shortcut)
        menu.add_separator()
        menu.add_command(label="Выход", command=self.close_app)

        # Position menu: right edge at board's left notation column (spec 10.1)
        # "раскрывается влево за пределы окна ГИ. Правый край — впритык к колонке цифр"
        menu.update_idletasks()
        menu_width = menu.winfo_reqwidth()
        menu_height = menu.winfo_reqheight()

        # Right edge of menu = left edge of board notation (board_x)
        board_screen_x = self.board.winfo_rootx() + self.board.board_x
        x = board_screen_x - menu_width
        if x < 0:
            x = 0

        # Vertical: open upward from bottom toolbar
        btn = self.bottom_toolbar.btn_menu
        y = btn.winfo_rooty() - menu_height
        if y < 0:
            y = btn.winfo_rooty() + btn.winfo_height()
        try:
            menu.tk_popup(x, y, 0)
        finally:
            menu.grab_release()

    def save_position(self):
        """Save position screenshot (section 10.3)."""
        if self.stage != "game":
            return

        filename = get_screenshot_name(self.move_number, self.white_turn)
        save_dir = get_screenshot_dir(self.party_folder)
        filepath = os.path.join(save_dir, filename)

        if os.path.exists(filepath):
            replace = messagebox.askyesno(
                "Файл существует",
                f"Файл '{filename}' уже существует.\nЗаменить?",
                parent=self.root
            )
            if not replace:
                return

        success = save_screenshot(self.board, filepath)
        if success:
            self._show_temp_message("Текущая позиция сохранена")

    def save_position_as(self):
        """Save position with custom filename."""
        if self.stage != "game":
            return

        default_name = get_screenshot_name(self.move_number, self.white_turn)
        save_dir = get_screenshot_dir(self.party_folder)

        SaveAsDialog(
            self.root,
            default_name=default_name.replace(".png", ""),
            default_dir=save_dir,
            on_save=self._do_save_as
        )

    def _do_save_as(self, filename, directory):
        filepath = os.path.join(directory, filename)
        if os.path.exists(filepath):
            replace = messagebox.askyesno(
                "Файл существует",
                f"Файл '{filename}' уже существует.\nЗаменить?",
                parent=self.root
            )
            if not replace:
                return
        success = save_screenshot(self.board, filepath)
        if success:
            self._show_temp_message("Текущая позиция сохранена")

    def _show_temp_message(self, text, duration=1500):
        """Show temporary message overlay (~1.5 sec per spec)."""
        msg = tk.Toplevel(self.root)
        msg.overrideredirect(True)
        msg.attributes("-topmost", True)

        label = tk.Label(msg, text=text, font=("Arial", 14, "bold"),
                         fg="white", bg="#4CAF50", padx=20, pady=10)
        label.pack()

        msg.update_idletasks()
        x = self.root.winfo_rootx() + (self.root.winfo_width() - msg.winfo_width()) // 2
        y = self.root.winfo_rooty() + (self.root.winfo_height() - msg.winfo_height()) // 2
        msg.geometry(f"+{x}+{y}")
        self.root.after(duration, msg.destroy)

    def end_session_dialog(self):
        """Show end session dialog."""
        EndSessionDialog(
            self.root,
            on_end=self._on_end_session,
            on_cancel=None
        )

    def _on_end_session(self, save_position=False):
        if save_position:
            self.save_position()
        self._end_session_no_dialog()

    def _end_session_no_dialog(self):
        self.session_active = False
        clear_session()
        self._enter_startup()

    def create_shortcut(self):
        """Create desktop shortcut (section 10.1)."""
        desktop = _get_desktop_path()

        if sys.platform == "win32":
            shortcut_path = os.path.join(desktop, "chess-T1.lnk")
            if os.path.exists(shortcut_path):
                return  # Already exists — do nothing (spec 10.1)
            try:
                exe_path = _get_executable_path()
                _create_windows_shortcut(shortcut_path, exe_path)
                messagebox.showinfo("Ярлык", "Ярлык создан на рабочем столе.", parent=self.root)
            except Exception as e:
                messagebox.showerror("Ошибка", f"Не удалось создать ярлык:\n{e}", parent=self.root)
        else:
            shortcut_path = os.path.join(desktop, "chess-T1.desktop")
            if os.path.exists(shortcut_path):
                return  # Already exists — do nothing (spec 10.1)
            try:
                exe_path = _get_executable_path()
                with open(shortcut_path, "w") as f:
                    f.write(f"[Desktop Entry]\nType=Application\nName=chess-T1\n"
                            f"Exec=python3 {exe_path}\nTerminal=false\n"
                            f"Categories=Game;\n")
                os.chmod(shortcut_path, 0o755)
                messagebox.showinfo("Ярлык", "Ярлык создан на рабочем столе.", parent=self.root)
            except Exception as e:
                messagebox.showerror("Ошибка", f"Не удалось создать ярлык:\n{e}", parent=self.root)

    # ---- Window Management ----

    def minimize_window(self):
        if self.promotion_pending:
            return
        self.root.iconify()

    def toggle_always_on_top(self):
        self.always_on_top = not self.always_on_top
        self.root.attributes("-topmost", self.always_on_top)
        self.top_toolbar.btn_on_top.highlight(self.always_on_top)

    def close_app(self):
        """Close the application (section 10.2)."""
        if self.session_active:
            CloseAppDialog(
                self.root,
                on_close_end=self._close_with_end,
                on_close_keep=self._close_without_end,
                on_cancel=None
            )
        else:
            clear_session()
            self.root.destroy()

    def _close_with_end(self, save_position=False):
        if save_position:
            self.save_position()
        clear_session()
        self.root.destroy()

    def _close_without_end(self, save_position=False):
        if save_position:
            self.save_position()
        self._auto_save_session()
        self.root.destroy()

    # ---- Session Persistence ----

    def _auto_save_session(self):
        if not self.session_active:
            return
        save_session({
            "mode": self.mode,
            "position": self.board.get_position(),
            "white_turn": self.white_turn,
            "move_number": self.move_number,
            "party_folder": self.party_folder,
            "board_reversed": self.board_reversed,
        })

    def _restore_session(self, data):
        """Restore previously saved session (section 10.4)."""
        self.mode = data.get("mode", "party")
        self.stage = "game"
        self.session_active = True
        self.white_turn = data.get("white_turn", True)
        self.move_number = data.get("move_number", 1)
        self.party_folder = data.get("party_folder")
        self.board_reversed = data.get("board_reversed", False)

        position = data.get("position", {})
        self.board.reversed = self.board_reversed
        self.board.set_position(position)

        self.move_history = [copy_position(position)]
        self.history_index = 0
        self._update_all_states()

    def run(self):
        self.root.mainloop()


def main():
    app = ChessT1App()
    app.run()


if __name__ == "__main__":
    main()
