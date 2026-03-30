"""
Piece definitions and management for chess-T1.
"""

# Piece type constants
KING = "king"           # Король (Кр)
KONNET = "konnet"       # Коннет (Кт)
PRINCE = "prince"       # Принц (Пр)
RITTER = "ritter"       # Риттер (Рт)
KNEKHT = "knekht"      # Кнехт (Кн)
VER_KNEKHT = "ver_knekht"  # Вер Кнехт (ВК)
RAZVEDCHIK = "razvedchik"  # Разведчик (Рк)

# Colors
WHITE = "white"
BLACK = "black"

# Russian short names
PIECE_SHORT_NAMES = {
    KING: "Кр",
    KONNET: "Кт",
    PRINCE: "Пр",
    RITTER: "Рт",
    KNEKHT: "Кн",
    VER_KNEKHT: "ВК",
    RAZVEDCHIK: "Рк",
}

PIECE_FULL_NAMES = {
    KING: "Король",
    KONNET: "Коннет",
    PRINCE: "Принц",
    RITTER: "Риттер",
    KNEKHT: "Кнехт",
    VER_KNEKHT: "Вер Кнехт",
    RAZVEDCHIK: "Разведчик",
}

# All piece types
ALL_PIECE_TYPES = [KING, KONNET, PRINCE, RITTER, KNEKHT, VER_KNEKHT, RAZVEDCHIK]

# Castle cells
WHITE_CASTLE = {"c1", "d1", "e1", "f1"}
BLACK_CASTLE = {"c8", "d8", "e8", "f8"}
ALL_CASTLE = WHITE_CASTLE | BLACK_CASTLE

# Icon file mapping
ICON_FILES = {}
for color in [WHITE, BLACK]:
    for ptype in ALL_PIECE_TYPES:
        ICON_FILES[(color, ptype)] = f"{color}_{ptype}.svg"


class Piece:
    """Represents a single game piece."""

    def __init__(self, color, piece_type):
        self.color = color
        self.piece_type = piece_type

    @property
    def short_name(self):
        return PIECE_SHORT_NAMES[self.piece_type]

    @property
    def full_name(self):
        return PIECE_FULL_NAMES[self.piece_type]

    @property
    def icon_file(self):
        return ICON_FILES[(self.color, self.piece_type)]

    def copy(self):
        return Piece(self.color, self.piece_type)

    def __repr__(self):
        c = "Б" if self.color == WHITE else "Ч"
        return f"{c}{self.short_name}"

    def __eq__(self, other):
        if not isinstance(other, Piece):
            return False
        return self.color == other.color and self.piece_type == other.piece_type

    def __hash__(self):
        return hash((self.color, self.piece_type))


def get_initial_position():
    """Return the initial board position as dict {cell_name: Piece}."""
    position = {}

    # Row 1 (white): Рт, Рк, Пр, Кт, Кр, Пр, Рк, Рт
    row1_types = [RITTER, RAZVEDCHIK, PRINCE, KONNET, KING, PRINCE, RAZVEDCHIK, RITTER]
    for i, ptype in enumerate(row1_types):
        col = chr(ord('a') + i)
        position[f"{col}1"] = Piece(WHITE, ptype)

    # Row 2 (white knechts)
    for i in range(8):
        col = chr(ord('a') + i)
        position[f"{col}2"] = Piece(WHITE, KNEKHT)

    # Row 7 (black knechts)
    for i in range(8):
        col = chr(ord('a') + i)
        position[f"{col}7"] = Piece(BLACK, KNEKHT)

    # Row 8 (black): Рт, Рк, Пр, Кт, Кр, Пр, Рк, Рт
    row8_types = [RITTER, RAZVEDCHIK, PRINCE, KONNET, KING, PRINCE, RAZVEDCHIK, RITTER]
    for i, ptype in enumerate(row8_types):
        col = chr(ord('a') + i)
        position[f"{col}8"] = Piece(BLACK, ptype)

    return position


def cell_to_coords(cell_name):
    """Convert cell name like 'a1' to (col_index, row_index) 0-based."""
    col = ord(cell_name[0]) - ord('a')
    row = int(cell_name[1]) - 1
    return col, row


def coords_to_cell(col, row):
    """Convert (col_index, row_index) 0-based to cell name like 'a1'."""
    return f"{chr(ord('a') + col)}{row + 1}"


def copy_position(position):
    """Deep copy a position dict."""
    return {cell: piece.copy() for cell, piece in position.items()}


def check_knekht_promotion(cell, piece):
    """Check if a Knekht should auto-promote to Ver Knekht.
    White Knekht on row 6 -> White VK
    Black Knekht on row 3 -> Black VK
    """
    if piece.piece_type != KNEKHT:
        return None
    row = int(cell[1])
    if piece.color == WHITE and row == 6:
        return Piece(WHITE, VER_KNEKHT)
    if piece.color == BLACK and row == 3:
        return Piece(BLACK, VER_KNEKHT)
    return None


def check_ver_knekht_promotion(cell, piece):
    """Check if Ver Knekht needs promotion choice.
    Returns list of possible promotions or None.

    White VK on a8,b8,g8,h8 -> choose from [Кт, Пр, Рт, Рк]
    White VK on c8,d8,e8,f8 -> choose from [Кт, Пр]
    Black VK on a1,b1,g1,h1 -> choose from [Кт, Пр, Рт, Рк]
    Black VK on c1,d1,e1,f1 -> choose from [Кт, Пр]
    """
    if piece.piece_type != VER_KNEKHT:
        return None

    col = cell[0]
    row = int(cell[1])

    if piece.color == WHITE and row == 8:
        if col in ('c', 'd', 'e', 'f'):
            return [KONNET, PRINCE]
        elif col in ('a', 'b', 'g', 'h'):
            return [KONNET, PRINCE, RITTER, RAZVEDCHIK]

    if piece.color == BLACK and row == 1:
        if col in ('c', 'd', 'e', 'f'):
            return [KONNET, PRINCE]
        elif col in ('a', 'b', 'g', 'h'):
            return [KONNET, PRINCE, RITTER, RAZVEDCHIK]

    return None


def check_prince_promotion(cell, piece):
    """Check if Prince needs promotion choice.
    White Prince on c8,d8,e8,f8 -> choose from [Кт, Пр]
    Black Prince on c1,d1,e1,f1 -> choose from [Кт, Пр]
    """
    if piece.piece_type != PRINCE:
        return None

    row = int(cell[1])
    col = cell[0]

    if piece.color == WHITE and row == 8 and col in ('c', 'd', 'e', 'f'):
        return [KONNET, PRINCE]
    if piece.color == BLACK and row == 1 and col in ('c', 'd', 'e', 'f'):
        return [KONNET, PRINCE]

    return None


def is_razvedchik_exchange(piece, target_cell, target_piece):
    """Check if this is a Razvedchik 'capture with exchange' special move.
    Conditions: piece is Razvedchik, target cell is any castle cell,
    target has opponent's piece.
    """
    if piece.piece_type != RAZVEDCHIK:
        return False
    if target_cell not in ALL_CASTLE:
        return False
    if target_piece is None:
        return False
    if target_piece.color == piece.color:
        return False
    return True
