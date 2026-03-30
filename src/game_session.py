"""
Game session management for chess-T1.
Handles saving/restoring sessions and position screenshots.
"""

import json
import os
import time
from pieces import Piece, copy_position, WHITE, BLACK


CONFIG_DIR = os.path.join(os.path.expanduser("~"), ".chesst1")
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


def get_screenshot_name(move_number, white_turn):
    """Generate screenshot filename based on move indicator.
    White turn: '1. __ хб'
    Black turn: '8 … __ хч'
    """
    if white_turn:
        return f"{move_number}. __ хб.png"
    else:
        return f"{move_number} … __ хч.png"


def get_screenshot_dir(party_folder):
    """Get directory for saving screenshots."""
    if party_folder and os.path.isdir(party_folder):
        return party_folder

    # Fallback: Pictures directory
    pictures = os.path.join(os.path.expanduser("~"), "Pictures")
    if not os.path.exists(pictures):
        pictures = os.path.join(os.path.expanduser("~"), "Изображения")
    if not os.path.exists(pictures):
        pictures = os.path.expanduser("~")
    return pictures


def save_screenshot(widget, filepath):
    """Save a widget screenshot as PNG.
    Uses PostScript export and conversion, or direct grab.
    """
    try:
        # Try using Pillow's ImageGrab
        from PIL import ImageGrab

        x = widget.winfo_rootx()
        y = widget.winfo_rooty()
        w = widget.winfo_width()
        h = widget.winfo_height()

        image = ImageGrab.grab(bbox=(x, y, x + w, y + h))
        image.save(filepath, "PNG")
        return True
    except ImportError:
        pass

    try:
        # Fallback: PostScript -> PNG via Pillow
        import tempfile
        from PIL import Image

        ps_path = filepath + ".ps"
        widget.postscript(file=ps_path, colormode="color")
        img = Image.open(ps_path)
        img.save(filepath, "PNG")
        os.remove(ps_path)
        return True
    except Exception:
        pass

    try:
        # Last resort: save PostScript
        ps_path = filepath.replace(".png", ".ps")
        widget.postscript(file=ps_path, colormode="color")
        return True
    except Exception:
        return False
