#!/usr/bin/env python3
"""
Build script for creating Windows executable using PyInstaller.
Run on Windows: python build_windows.py

Requirements:
  pip install pyinstaller pillow cairosvg

Output: dist/chess-T1.exe (single file, ~10-15 MB)
"""

import os
import sys
import subprocess

ROOT_DIR = os.path.dirname(os.path.abspath(__file__))
SRC_DIR = os.path.join(ROOT_DIR, "src")
ICON_DIR = os.path.join(SRC_DIR, "icons")
DIST_DIR = os.path.join(ROOT_DIR, "dist")
BUILD_DIR = os.path.join(ROOT_DIR, "build")
APP_ICON = os.path.join(SRC_DIR, "assets", "app_icon.ico")


def build():
    args = [
        sys.executable, "-m", "PyInstaller",
        "--name=chess-T1",
        "--onefile",
        "--windowed",
        "--noconfirm",
        "--clean",
        # Bundle icon files
        f"--add-data={ICON_DIR}{os.pathsep}icons",
        # Hidden imports that PyInstaller may miss
        "--hidden-import=pieces",
        "--hidden-import=board",
        "--hidden-import=toolbar",
        "--hidden-import=intro_page",
        "--hidden-import=piece_tray",
        "--hidden-import=dialogs",
        "--hidden-import=game_session",
        # Paths
        f"--paths={SRC_DIR}",
        f"--distpath={DIST_DIR}",
        f"--workpath={BUILD_DIR}",
        f"--specpath={ROOT_DIR}",
    ]

    # Add app icon if available
    if os.path.exists(APP_ICON):
        args.append(f"--icon={APP_ICON}")

    # Entry point
    args.append(os.path.join(SRC_DIR, "main.py"))

    print("Building chess-T1...")
    print(f"Command: {' '.join(args)}")

    result = subprocess.run(args, cwd=ROOT_DIR)

    if result.returncode == 0:
        exe_path = os.path.join(DIST_DIR, "chess-T1.exe")
        if os.path.exists(exe_path):
            size_mb = os.path.getsize(exe_path) / (1024 * 1024)
            print(f"\nBuild successful!")
            print(f"Executable: {exe_path}")
            print(f"Size: {size_mb:.1f} MB")
        else:
            print("\nBuild completed but exe not found at expected path.")
    else:
        print(f"\nBuild failed with exit code {result.returncode}")
        sys.exit(1)


if __name__ == "__main__":
    build()
