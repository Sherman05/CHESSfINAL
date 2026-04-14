#!/usr/bin/env python3
"""Launcher for chess-T1 application."""

import sys
import os
import traceback

# Add src to path
src_dir = os.path.join(os.path.dirname(os.path.abspath(__file__)), "src")
sys.path.insert(0, src_dir)

try:
    from main import main
    main()
except Exception as e:
    # Show error in a window so user can see it
    try:
        import tkinter as tk
        from tkinter import messagebox
        root = tk.Tk()
        root.withdraw()
        messagebox.showerror("chess-T1 — Ошибка", f"{e}\n\n{traceback.format_exc()}")
        root.destroy()
    except Exception:
        pass
    # Also print to console
    print(f"\nОШИБКА: {e}")
    traceback.print_exc()
    input("\nНажмите Enter для выхода...")
