"""
Dialog windows for chess-T1.
"""

import tkinter as tk
from tkinter import filedialog, messagebox
import os
import sys


def _get_desktop_path():
    """Get user's Desktop path, Windows-aware."""
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
    if not os.path.isdir(desktop):
        desktop = os.path.expanduser("~")
    return desktop


class CreateFolderDialog(tk.Toplevel):
    """Dialog for creating a party folder."""

    def __init__(self, parent, on_created=None, on_cancel=None):
        super().__init__(parent)
        self.on_created = on_created
        self.on_cancel = on_cancel
        self.result_path = None

        self.title("Создать папку для партии")
        self.geometry("500x200")
        self.resizable(False, False)
        self.transient(parent)
        self.grab_set()
        self.configure(bg="#F5F5F5")

        self.protocol("WM_DELETE_WINDOW", self._cancel)

        self._build_ui()
        self.wait_visibility()
        self.name_entry.focus_set()

    def _build_ui(self):
        frame = tk.Frame(self, bg="#F5F5F5", padx=20, pady=20)
        frame.pack(fill="both", expand=True)

        tk.Label(frame, text="Создать папку для партии",
                 font=("Arial", 14, "bold"), bg="#F5F5F5").pack(anchor="w")

        # Location
        loc_frame = tk.Frame(frame, bg="#F5F5F5")
        loc_frame.pack(fill="x", pady=(15, 5))

        tk.Label(loc_frame, text="Расположение:", font=("Arial", 10),
                 bg="#F5F5F5").pack(side="left")

        desktop = _get_desktop_path()

        self.location_var = tk.StringVar(value=desktop)
        self.loc_entry = tk.Entry(loc_frame, textvariable=self.location_var,
                                   font=("Arial", 10), width=35)
        self.loc_entry.pack(side="left", padx=(10, 5))

        btn_browse = tk.Button(loc_frame, text="Обзор...",
                                command=self._browse, font=("Arial", 9))
        btn_browse.pack(side="left")

        # Folder name
        name_frame = tk.Frame(frame, bg="#F5F5F5")
        name_frame.pack(fill="x", pady=(10, 5))

        tk.Label(name_frame, text="Название партии:", font=("Arial", 10),
                 bg="#F5F5F5").pack(side="left")

        self.name_var = tk.StringVar(value="Новая партия")
        self.name_entry = tk.Entry(name_frame, textvariable=self.name_var,
                                    font=("Arial", 10), width=30)
        self.name_entry.pack(side="left", padx=(10, 0))
        self.name_entry.select_range(0, tk.END)

        # Buttons
        btn_frame = tk.Frame(frame, bg="#F5F5F5")
        btn_frame.pack(fill="x", pady=(15, 0))

        tk.Button(btn_frame, text="Создать", font=("Arial", 11),
                  command=self._create, bg="#4CAF50", fg="white",
                  padx=20, pady=3).pack(side="right", padx=5)

        tk.Button(btn_frame, text="Отмена", font=("Arial", 11),
                  command=self._cancel, padx=20, pady=3).pack(side="right", padx=5)

    def _browse(self):
        path = filedialog.askdirectory(
            parent=self, initialdir=self.location_var.get(),
            title="Выберите расположение"
        )
        if path:
            self.location_var.set(path)

    def _create(self):
        name = self.name_var.get().strip()
        location = self.location_var.get().strip()

        if not name:
            messagebox.showwarning("Ошибка", "Введите название партии",
                                    parent=self)
            return

        folder_path = os.path.join(location, name)
        try:
            os.makedirs(folder_path, exist_ok=True)
            self.result_path = folder_path
            self.grab_release()
            self.destroy()
            if self.on_created:
                self.on_created(folder_path)
        except OSError as e:
            messagebox.showerror("Ошибка", f"Не удалось создать папку:\n{e}",
                                  parent=self)

    def _cancel(self):
        self.grab_release()
        self.destroy()
        if self.on_cancel:
            self.on_cancel()


class PromotionDialog(tk.Toplevel):
    """Dialog for piece promotion choice."""

    def __init__(self, parent, piece, options, piece_images, cell_size,
                 position="top_right", on_chosen=None):
        super().__init__(parent)
        self.on_chosen = on_chosen
        self.chosen = None
        self._scaled_images = []  # prevent GC of scaled images

        self.overrideredirect(True)
        self.configure(bg="#2C3E6B", bd=3, relief="raised")
        self.attributes("-topmost", True)

        frame = tk.Frame(self, bg="#2C3E6B", padx=8, pady=8)
        frame.pack()

        tk.Label(frame, text="Превращение:", font=("Arial", 10),
                 fg="white", bg="#2C3E6B").pack(pady=(0, 5))

        icons_frame = tk.Frame(frame, bg="#2C3E6B")
        icons_frame.pack()

        from pieces import PIECE_SHORT_NAMES

        for ptype in options:
            btn_frame = tk.Frame(icons_frame, bg="#2C3E6B")
            btn_frame.pack(side="left", padx=4)

            key = (piece.color, ptype)
            if key in piece_images:
                # Scale to ~80% of board icon size (spec 11)
                orig = piece_images[key]
                scaled = self._scale_photo_image(orig, 0.8)
                self._scaled_images.append(scaled)
                btn = tk.Button(
                    btn_frame, image=scaled,
                    command=lambda pt=ptype: self._choose(pt),
                    bg="#4A5A8A", activebackground="#6A7AAA",
                    relief="raised", bd=2
                )
            else:
                btn = tk.Button(
                    btn_frame, text=PIECE_SHORT_NAMES[ptype],
                    font=("Arial", 14, "bold"),
                    command=lambda pt=ptype: self._choose(pt),
                    bg="#4A5A8A", fg="white",
                    activebackground="#6A7AAA",
                    width=4, height=2
                )
            btn.pack()

            tk.Label(btn_frame, text=PIECE_SHORT_NAMES[ptype],
                     font=("Arial", 8), fg="#CCC", bg="#2C3E6B").pack()

        # Position the dialog
        self.update_idletasks()
        pw = parent.winfo_rootx()
        ph = parent.winfo_rooty()
        parent_w = parent.winfo_width()
        parent_h = parent.winfo_height()
        dw = self.winfo_width()
        dh = self.winfo_height()

        if "top" in position:
            y = ph + 50
        else:
            y = ph + parent_h - dh - 50

        if "right" in position:
            x = pw + parent_w - dw - 20
        else:
            x = pw + 20

        self.geometry(f"+{x}+{y}")
        self.grab_set()

    @staticmethod
    def _scale_photo_image(photo_image, scale):
        """Scale a tkinter PhotoImage to ~80% using zoom+subsample trick.
        For 80%: zoom 4x then subsample 5x = 4/5 = 0.8 exactly.
        """
        if abs(scale - 0.8) < 0.05:
            # 80%: zoom 4, subsample 5 -> exact 4/5 ratio
            zoomed = photo_image.zoom(4, 4)
            return zoomed.subsample(5, 5)
        elif abs(scale - 0.5) < 0.05:
            return photo_image.subsample(2, 2)
        elif abs(scale - 0.75) < 0.05:
            zoomed = photo_image.zoom(3, 3)
            return zoomed.subsample(4, 4)
        else:
            # General case: approximate via nearest integer ratio
            num = max(1, round(scale * 5))
            zoomed = photo_image.zoom(num, num)
            return zoomed.subsample(5, 5)

    def _choose(self, ptype):
        self.chosen = ptype
        self.grab_release()
        self.destroy()
        if self.on_chosen:
            self.on_chosen(ptype)


class EndSessionDialog(tk.Toplevel):
    """Dialog for ending a party/session."""

    def __init__(self, parent, on_end=None, on_cancel=None):
        super().__init__(parent)
        self.on_end = on_end
        self.on_cancel = on_cancel

        self.title("Завершить партию")
        self.geometry("400x180")
        self.resizable(False, False)
        self.transient(parent)
        self.grab_set()
        self.configure(bg="#F5F5F5")
        self.protocol("WM_DELETE_WINDOW", self._cancel)

        self._build_ui()

    def _build_ui(self):
        frame = tk.Frame(self, bg="#F5F5F5", padx=20, pady=20)
        frame.pack(fill="both", expand=True)

        tk.Label(frame, text="Завершить партию?",
                 font=("Arial", 14, "bold"), bg="#F5F5F5").pack(anchor="w")

        self.save_var = tk.BooleanVar(value=True)
        tk.Checkbutton(frame, text="Сохранить позицию",
                        variable=self.save_var, font=("Arial", 11),
                        bg="#F5F5F5").pack(anchor="w", pady=(15, 5))

        btn_frame = tk.Frame(frame, bg="#F5F5F5")
        btn_frame.pack(fill="x", pady=(15, 0))

        tk.Button(btn_frame, text="Завершить партию",
                  font=("Arial", 11), command=self._end,
                  bg="#E53935", fg="white", padx=15, pady=3).pack(side="right", padx=5)

        tk.Button(btn_frame, text="Отмена",
                  font=("Arial", 11), command=self._cancel,
                  padx=15, pady=3).pack(side="right", padx=5)

    def _end(self):
        save = self.save_var.get()
        self.grab_release()
        self.destroy()
        if self.on_end:
            self.on_end(save_position=save)

    def _cancel(self):
        self.grab_release()
        self.destroy()
        if self.on_cancel:
            self.on_cancel()


class CloseAppDialog(tk.Toplevel):
    """Dialog shown when closing app during active session."""

    def __init__(self, parent, on_close_end=None, on_close_keep=None, on_cancel=None):
        super().__init__(parent)
        self.on_close_end = on_close_end
        self.on_close_keep = on_close_keep
        self.on_cancel = on_cancel

        self.title("Закрыть программу")
        self.geometry("480x250")
        self.resizable(False, False)
        self.transient(parent)
        self.grab_set()
        self.configure(bg="#F5F5F5")
        self.protocol("WM_DELETE_WINDOW", self._cancel)

        self._build_ui()

    def _build_ui(self):
        frame = tk.Frame(self, bg="#F5F5F5", padx=20, pady=20)
        frame.pack(fill="both", expand=True)

        tk.Label(frame, text="Закрыть программу?",
                 font=("Arial", 14, "bold"), bg="#F5F5F5").pack(anchor="w")

        tk.Label(frame, text="Сеанс партии не завершён.",
                 font=("Arial", 11), bg="#F5F5F5", fg="#666").pack(anchor="w", pady=(5, 10))

        # Option 1
        opt1_frame = tk.Frame(frame, bg="#F5F5F5")
        opt1_frame.pack(fill="x", pady=3)

        self.save1_var = tk.BooleanVar(value=True)
        tk.Button(opt1_frame, text="Завершить партию и закрыть",
                  font=("Arial", 10), command=self._close_end,
                  bg="#E53935", fg="white", padx=10, pady=2).pack(side="left")
        tk.Checkbutton(opt1_frame, text="Сохранить позицию",
                        variable=self.save1_var, font=("Arial", 10),
                        bg="#F5F5F5").pack(side="left", padx=(10, 0))

        # Option 2
        opt2_frame = tk.Frame(frame, bg="#F5F5F5")
        opt2_frame.pack(fill="x", pady=3)

        self.save2_var = tk.BooleanVar(value=True)
        tk.Button(opt2_frame, text="Закрыть — не завершая партию",
                  font=("Arial", 10), command=self._close_keep,
                  bg="#FF9800", fg="white", padx=10, pady=2).pack(side="left")
        tk.Checkbutton(opt2_frame, text="Сохранить позицию",
                        variable=self.save2_var, font=("Arial", 10),
                        bg="#F5F5F5").pack(side="left", padx=(10, 0))

        # Cancel
        btn_frame = tk.Frame(frame, bg="#F5F5F5")
        btn_frame.pack(fill="x", pady=(15, 0))

        tk.Button(btn_frame, text="Отмена",
                  font=("Arial", 11), command=self._cancel,
                  padx=15, pady=3).pack(side="right")

    def _close_end(self):
        save = self.save1_var.get()
        self.grab_release()
        self.destroy()
        if self.on_close_end:
            self.on_close_end(save_position=save)

    def _close_keep(self):
        save = self.save2_var.get()
        self.grab_release()
        self.destroy()
        if self.on_close_keep:
            self.on_close_keep(save_position=save)

    def _cancel(self):
        self.grab_release()
        self.destroy()
        if self.on_cancel:
            self.on_cancel()


class SaveAsDialog(tk.Toplevel):
    """Dialog for 'Save position as' with custom filename."""

    def __init__(self, parent, default_name="", default_dir="", on_save=None):
        super().__init__(parent)
        self.on_save = on_save

        self.title("Сохранить позицию как")
        self.geometry("450x150")
        self.resizable(False, False)
        self.transient(parent)
        self.grab_set()
        self.configure(bg="#F5F5F5")
        self.protocol("WM_DELETE_WINDOW", self._cancel)

        frame = tk.Frame(self, bg="#F5F5F5", padx=20, pady=20)
        frame.pack(fill="both", expand=True)

        tk.Label(frame, text="Имя файла:", font=("Arial", 11),
                 bg="#F5F5F5").pack(anchor="w")

        self.name_var = tk.StringVar(value=default_name)
        self.name_entry = tk.Entry(frame, textvariable=self.name_var,
                                    font=("Arial", 11), width=40)
        self.name_entry.pack(fill="x", pady=(5, 10))
        self.name_entry.select_range(0, tk.END)
        self.name_entry.focus_set()

        self.dir_path = default_dir

        btn_frame = tk.Frame(frame, bg="#F5F5F5")
        btn_frame.pack(fill="x")

        tk.Button(btn_frame, text="Сохранить", font=("Arial", 11),
                  command=self._save, bg="#4CAF50", fg="white",
                  padx=15).pack(side="right", padx=5)

        tk.Button(btn_frame, text="Отмена", font=("Arial", 11),
                  command=self._cancel, padx=15).pack(side="right")

    def _save(self):
        name = self.name_var.get().strip()
        if not name:
            return
        if not name.endswith(".png"):
            name += ".png"
        self.grab_release()
        self.destroy()
        if self.on_save:
            self.on_save(name, self.dir_path)

    def _cancel(self):
        self.grab_release()
        self.destroy()
