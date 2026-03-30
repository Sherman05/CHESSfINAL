#!/usr/bin/env python3
"""Launcher for chess-T1 application."""

import sys
import os

# Add src to path
src_dir = os.path.join(os.path.dirname(os.path.abspath(__file__)), "src")
sys.path.insert(0, src_dir)

from main import main

if __name__ == "__main__":
    main()
