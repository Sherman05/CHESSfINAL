#!/usr/bin/env python3
"""
Build script for creating Windows executable using PyInstaller.
Run on Windows: python build_windows.py
"""

import PyInstaller.__main__
import os

src_dir = os.path.join(os.path.dirname(os.path.abspath(__file__)), "src")
icon_dir = os.path.join(src_dir, "icons")

PyInstaller.__main__.run([
    os.path.join(src_dir, "main.py"),
    "--name=chess-T1",
    "--onefile",
    "--windowed",
    f"--add-data={icon_dir}{os.pathsep}icons",
    "--noconfirm",
    "--clean",
])
