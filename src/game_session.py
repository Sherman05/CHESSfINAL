"""
Game session management for chess-T1.
Handles saving/restoring sessions and position screenshots.
"""

import json
import os
import sys
import time
from pieces import Piece, copy_position, WHITE, BLACK


def _get_appdata_dir():
    """Get OS-appropriate application data directory."""
    if sys.platform == "win32":
        # Windows: use %APPDATA%\chesst1
        appdata = os.environ.get("APPDATA")
        if appdata:
            return os.path.join(appdata, "chesst1")
        return os.path.join(os.path.expanduser("~"), "AppData", "Roaming", "chesst1")
    return os.path.join(os.path.expanduser("~"), ".chesst1")


CONFIG_DIR = _get_appdata_dir()
CONFIG_FILE = os.path.join(CONFIG_DIR, "config.json")
SESSION_FILE = os.path.join(CONFIG_DIR, "session.json")


def ensure_config_dir():
    """Create config directory if it doesn't exist."""
    os.makedirs(CONFIG_DIR, exist_ok=True)


def load_config():
    """Load application config."""
    ensure_config_dir()
    try:
        with open(CONFIG_FILE, "r", encoding="utf-8") as f:
            return json.load(f)
    except (FileNotFoundError, json.JSONDecodeError):
        return {
            "skip_intro": False,
            "window_width": 900,
            "window_height": 700,
        }


def save_config(config):
    """Save application config."""
    ensure_config_dir()
    with open(CONFIG_FILE, "w", encoding="utf-8") as f:
        json.dump(config, f, indent=2, ensure_ascii=False)


def save_session(session_data):
    """Save current session state for restoration on next launch.
    session_data should contain:
    - mode: "party" or "analysis"
    - position: dict of {cell: (color, piece_type)}
    - white_turn: bool
    - move_number: int
    - party_folder: str or None
    - board_reversed: bool
    """
    ensure_config_dir()
    # Convert position to serializable format
    data = dict(session_data)
    if "position" in data and data["position"]:
        pos = {}
        for cell, piece in data["position"].items():
            if isinstance(piece, Piece):
                pos[cell] = {"color": piece.color, "type": piece.piece_type}
            else:
                pos[cell] = piece
        data["position"] = pos

    with open(SESSION_FILE, "w", encoding="utf-8") as f:
        json.dump(data, f, indent=2, ensure_ascii=False)


def load_session():
    """Load saved session. Returns dict or None."""
    try:
        with open(SESSION_FILE, "r", encoding="utf-8") as f:
            data = json.load(f)

        # Convert position back to Piece objects
        if "position" in data and data["position"]:
            pos = {}
            for cell, pdata in data["position"].items():
                if isinstance(pdata, dict):
                    pos[cell] = Piece(pdata["color"], pdata["type"])
            data["position"] = pos

        return data
    except (FileNotFoundError, json.JSONDecodeError, KeyError):
        return None


def clear_session():
    """Remove saved session file."""
    try:
        os.remove(SESSION_FILE)
    except FileNotFoundError:
        pass


def get_indicator_text(move_number, white_turn):
    """Get display text for the move indicator (used in UI)."""
    if white_turn:
        return f"{move_number}. __ хб"
    else:
        return f"{move_number} \u2026 __ хч"


def get_screenshot_name(move_number, white_turn):
    """Generate screenshot filename based on move indicator text (spec 10.3).
    Sanitizes characters forbidden in Windows filenames.
    """
    name = get_indicator_text(move_number, white_turn)
    # Replace Windows-forbidden characters: < > : " / \ | ? *
    # Also replace … (ellipsis) with ... for filesystem compatibility
    forbidden = {
        '\u2026': '...',
        ':': '',
        '<': '', '>': '', '"': '', '/': '', '\\': '', '|': '', '?': '', '*': '',
    }
    for char, replacement in forbidden.items():
        name = name.replace(char, replacement)
    return f"{name}.png"


def get_screenshot_dir(party_folder):
    """Get directory for saving screenshots."""
    if party_folder and os.path.isdir(party_folder):
        return party_folder

    # Windows: use Pictures folder
    if sys.platform == "win32":
        # Try known shell folder
        try:
            import winreg
            key = winreg.OpenKey(
                winreg.HKEY_CURRENT_USER,
                r"Software\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders"
            )
            pictures, _ = winreg.QueryValueEx(key, "My Pictures")
            winreg.CloseKey(key)
            if os.path.isdir(pictures):
                return pictures
        except Exception:
            pass

    # Cross-platform fallbacks
    for name in ("Pictures", "Изображения"):
        pictures = os.path.join(os.path.expanduser("~"), name)
        if os.path.exists(pictures):
            return pictures

    return os.path.expanduser("~")


def save_screenshot(widget, filepath):
    """Save a widget screenshot as PNG.
    Uses Pillow ImageGrab on Windows, PostScript fallback on other OS.
    """
    # Ensure target directory exists
    os.makedirs(os.path.dirname(filepath), exist_ok=True)

    try:
        from PIL import ImageGrab

        x = widget.winfo_rootx()
        y = widget.winfo_rooty()
        w = widget.winfo_width()
        h = widget.winfo_height()

        image = ImageGrab.grab(bbox=(x, y, x + w, y + h))
        image.save(filepath, "PNG")
        return True
    except (ImportError, OSError):
        pass

    try:
        # Fallback: Canvas PostScript -> PNG via Pillow
        from PIL import Image
        import tempfile

        with tempfile.NamedTemporaryFile(suffix=".ps", delete=False) as tmp:
            ps_path = tmp.name

        try:
            widget.postscript(file=ps_path, colormode="color")
            img = Image.open(ps_path)
            img.save(filepath, "PNG")
            return True
        finally:
            try:
                os.remove(ps_path)
            except OSError:
                pass
    except Exception:
        pass

    # All PNG methods failed
    return False
