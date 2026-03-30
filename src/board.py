"""
Game board widget for chess-T1.
8x8 board with castle cells, piece rendering, and drag & drop.
"""

import tkinter as tk
import os
import sys
import math
from pieces import (
    Piece, WHITE, BLACK, RAZVEDCHIK,
    cell_to_coords, coords_to_cell, copy_position,
    check_knekht_promotion, check_ver_knekht_promotion,
    check_prince_promotion, is_razvedchik_exchange,
    WHITE_CASTLE, BLACK_CASTLE, ALL_PIECE_TYPES,
    PIECE_FULL_NAMES, ICON_FILES
)

# Colors — matching the PDF mockup "Расширенный вид ГИ"
COLOR_WHITE_CELL = "#FFFFFF"      # White cells (pure white per mockup)
COLOR_BLACK_CELL = "#C0C0C0"      # Black cells (grey per mockup)
COLOR_CASTLE_CELL = "#E8E8E8"     # Castle cells (light grey, distinct from both)
COLOR_HIGHLIGHT_START = "#FFFF00"
COLOR_HIGHLIGHT_HOVER = "#90EE90"
COLOR_HIGHLIGHT_LAST = "#CED26B"
COLOR_BORDER = "#4A90C8"          # Blue border (matching mockup frame)
COLOR_NOTATION = "#1A237E"        # Dark blue notation text


class GameBoard(tk.Canvas):
    """Interactive 8x8 game board with drag & drop."""

    def __init__(self, parent, app, **kwargs):
        super().__init__(parent, **kwargs)
        self.app = app
        self.cell_size = 70
        self.board_x = 40  # offset for notation labels
        self.board_y = 40
        self.position = {}  # {cell_name: Piece}
        self.reversed = False
        self.piece_images = {}  # cache for PhotoImage
        self.cell_items = {}   # canvas item ids for cells
        self.piece_items = {}  # canvas item ids for pieces
        self.last_move_cell = None

        # Drag state
        self.drag_piece = None
        self.drag_from_cell = None
        self.drag_image_id = None
        self.drag_offset_x = 0
        self.drag_offset_y = 0
        self.hover_cell = None

        # Analysis mode drag from tray
        self.drag_from_tray = False
        self.drag_tray_piece = None

        # Piece deletion mode
        self.selected_for_deletion = None
        self.deletion_mode = False

        self.bind("<Configure>", self._on_resize)
        self.bind("<Button-1>", self._on_click)
        self.bind("<B1-Motion>", self._on_drag)
        self.bind("<ButtonRelease-1>", self._on_drop)

    def set_cell_size(self, size):
        """Set cell size and redraw."""
        self.cell_size = size
        self.board_x = int(size * 0.57)
        self.board_y = int(size * 0.57)
        self.redraw()

    def _on_resize(self, event):
        """Handle canvas resize."""
        available = min(event.width, event.height)
        new_size = max(40, int((available - 100) / 8))
        if abs(new_size - self.cell_size) > 5:
            self.cell_size = new_size
            self.board_x = int(new_size * 0.57)
            self.board_y = int(new_size * 0.57)
            self.load_piece_images()
            self.redraw()

    def load_piece_images(self):
        """Load and scale piece images."""
        self.piece_images.clear()
        # Support both normal run and PyInstaller frozen bundle
        if getattr(sys, 'frozen', False):
            base_dir = sys._MEIPASS
        else:
            base_dir = os.path.dirname(os.path.abspath(__file__))
        icon_dir = os.path.join(base_dir, "icons")
        icon_height = int(self.cell_size * 0.9)  # 10:9 ratio

        for (color, ptype), filename in ICON_FILES.items():
            filepath = os.path.join(icon_dir, filename)
            if os.path.exists(filepath):
                try:
                    img = self._load_svg(filepath, icon_height)
                    if img:
                        self.piece_images[(color, ptype)] = img
                except Exception:
                    self._create_text_icon(color, ptype, icon_height)
            else:
                self._create_text_icon(color, ptype, icon_height)

    def _load_svg(self, filepath, target_height):
        """Load SVG file and convert to PhotoImage."""
        try:
            import cairosvg
            from PIL import Image
            import io

            png_data = cairosvg.svg2png(
                url=filepath,
                output_height=target_height
            )
            image = Image.open(io.BytesIO(png_data))
            return tk.PhotoImage(data=self._pil_to_png_data(image))
        except ImportError:
            try:
                from PIL import Image
                import subprocess
                import tempfile

                with tempfile.NamedTemporaryFile(suffix='.png', delete=False) as tmp:
                    tmp_path = tmp.name

                try:
                    subprocess.run(
                        ['rsvg-convert', '-h', str(target_height), filepath, '-o', tmp_path],
                        capture_output=True, check=True
                    )
                    image = Image.open(tmp_path)
                    return tk.PhotoImage(data=self._pil_to_png_data(image))
                except (subprocess.CalledProcessError, FileNotFoundError):
                    pass
                finally:
                    if os.path.exists(tmp_path):
                        os.unlink(tmp_path)
            except ImportError:
                pass
        return None

    def _pil_to_png_data(self, pil_image):
        """Convert PIL image to PNG bytes for tkinter."""
        import io
        buf = io.BytesIO()
        pil_image.save(buf, format='PNG')
        import base64
        return base64.b64encode(buf.getvalue())

    def _create_text_icon(self, color, ptype, icon_height):
        """Create a simple circle icon as fallback using row-based put for speed."""
        size = max(icon_height, 30)
        img = tk.PhotoImage(width=size, height=size)

        fill_color = "#FFFFFF" if color == WHITE else "#333333"
        outline_color = "#333333" if color == WHITE else "#FFFFFF"
        transparent = ""
        r = size // 2 - 2
        cx, cy = size // 2, size // 2

        # Build image row by row (much faster than pixel by pixel)
        for y in range(size):
            row = []
            for x in range(size):
                dx = x - cx
                dy = y - cy
                dist_sq = dx * dx + dy * dy
                if dist_sq <= r * r:
                    row.append(fill_color)
                elif dist_sq <= (r + 2) * (r + 2):
                    row.append(outline_color)
                else:
                    row.append(transparent)
            img.put("{" + " ".join(row) + "}", to=(0, y))

        self.piece_images[(color, ptype)] = img

    def get_cell_rect(self, col, row):
        """Get pixel rectangle for a board cell (considering board orientation)."""
        if self.reversed:
            display_col = 7 - col
            display_row = row
        else:
            display_col = col
            display_row = 7 - row

        x1 = self.board_x + display_col * self.cell_size
        y1 = self.board_y + display_row * self.cell_size
        x2 = x1 + self.cell_size
        y2 = y1 + self.cell_size
        return x1, y1, x2, y2

    def pixel_to_cell(self, px, py):
        """Convert pixel coordinates to cell name, or None if outside board."""
        display_col = int((px - self.board_x) / self.cell_size)
        display_row = int((py - self.board_y) / self.cell_size)

        if not (0 <= display_col < 8 and 0 <= display_row < 8):
            return None

        if self.reversed:
            col = 7 - display_col
            row = display_row
        else:
            col = display_col
            row = 7 - display_row

        return coords_to_cell(col, row)

    def redraw(self):
        """Redraw the entire board."""
        self.delete("all")
        self.cell_items.clear()
        self.piece_items.clear()

        total_w = self.board_x + 8 * self.cell_size + self.board_x
        total_h = self.board_y + 8 * self.cell_size + self.board_y

        # Border
        self.create_rectangle(
            self.board_x - 3, self.board_y - 3,
            self.board_x + 8 * self.cell_size + 3,
            self.board_y + 8 * self.cell_size + 3,
            outline=COLOR_BORDER, width=3
        )

        # Draw cells
        for row in range(8):
            for col in range(8):
                cell_name = coords_to_cell(col, row)
                x1, y1, x2, y2 = self.get_cell_rect(col, row)

                # Determine cell color
                if cell_name in WHITE_CASTLE or cell_name in BLACK_CASTLE:
                    fill = COLOR_CASTLE_CELL
                elif (col + row) % 2 == 0:
                    fill = COLOR_BLACK_CELL
                else:
                    fill = COLOR_WHITE_CELL

                rect_id = self.create_rectangle(x1, y1, x2, y2, fill=fill, outline="")
                self.cell_items[cell_name] = rect_id

        # Highlight last move
        if self.last_move_cell and self.last_move_cell in self.cell_items:
            col, row = cell_to_coords(self.last_move_cell)
            x1, y1, x2, y2 = self.get_cell_rect(col, row)
            self.create_rectangle(x1, y1, x2, y2, fill=COLOR_HIGHLIGHT_LAST, outline="")

        # Notation labels
        font_size = max(10, int(self.cell_size * 0.2))
        notation_font = ("Arial", font_size)

        for col in range(8):
            letter = chr(ord('a') + col) if not self.reversed else chr(ord('h') - col)
            x = self.board_x + col * self.cell_size + self.cell_size // 2
            # Bottom
            self.create_text(x, self.board_y + 8 * self.cell_size + font_size + 5,
                             text=letter, font=notation_font, fill=COLOR_NOTATION)
            # Top
            self.create_text(x, self.board_y - font_size - 2,
                             text=letter, font=notation_font, fill=COLOR_NOTATION)

        for row in range(8):
            if self.reversed:
                num = str(row + 1)
            else:
                num = str(8 - row)
            y = self.board_y + row * self.cell_size + self.cell_size // 2
            # Left
            self.create_text(self.board_x - font_size - 5, y,
                             text=num, font=notation_font, fill=COLOR_NOTATION)
            # Right
            self.create_text(self.board_x + 8 * self.cell_size + font_size + 5, y,
                             text=num, font=notation_font, fill=COLOR_NOTATION)

        # Draw pieces
        self._draw_pieces()

    def _draw_pieces(self):
        """Draw all pieces on the board."""
        for cell_name, piece in self.position.items():
            self._draw_piece(cell_name, piece)

    def _draw_piece(self, cell_name, piece):
        """Draw a single piece on the board."""
        col, row = cell_to_coords(cell_name)
        x1, y1, x2, y2 = self.get_cell_rect(col, row)
        cx = (x1 + x2) // 2
        cy = (y1 + y2) // 2

        key = (piece.color, piece.piece_type)
        if key in self.piece_images:
            img = self.piece_images[key]
            # Spec 5.2: bottom edge with offset from cell bottom
            bottom_offset = max(2, int(self.cell_size * 0.05))
            item_id = self.create_image(cx, y2 - bottom_offset, image=img, anchor="s")
        else:
            from pieces import PIECE_SHORT_NAMES
            text_color = "#FFFFFF" if piece.color == BLACK else "#000000"
            bg_color = "#333333" if piece.color == BLACK else "#EEEEEE"
            r = self.cell_size * 0.35
            self.create_oval(cx - r, cy - r, cx + r, cy + r, fill=bg_color, outline="#666666", width=2)
            item_id = self.create_text(cx, cy, text=PIECE_SHORT_NAMES[piece.piece_type],
                                        font=("Arial", max(10, int(self.cell_size * 0.25)), "bold"),
                                        fill=text_color)

        self.piece_items[cell_name] = item_id

    def highlight_cell(self, cell_name, color):
        """Highlight a cell with the given color."""
        if cell_name is None:
            return
        col, row = cell_to_coords(cell_name)
        x1, y1, x2, y2 = self.get_cell_rect(col, row)
        self.create_rectangle(x1, y1, x2, y2, fill=color, outline="",
                              stipple="gray50", tags="highlight")

    def clear_highlights(self):
        """Remove all highlights."""
        self.delete("highlight")

    def _on_click(self, event):
        """Handle mouse click on board."""
        cell = self.pixel_to_cell(event.x, event.y)

        # Block all interaction in startup mode (display only)
        if self.app.stage == "startup":
            return

        # Handle piece deletion: click on a piece to SELECT it for deletion
        if self.deletion_mode:
            if cell and cell in self.position:
                self.select_piece_for_deletion(cell)
            else:
                # Click on empty cell — deselect
                self.selected_for_deletion = None
                self.deletion_mode = False
                self.redraw()
            return

        # Handle already-selected piece for deletion (clicking elsewhere deselects)
        if self.selected_for_deletion is not None:
            self.selected_for_deletion = None
            self.redraw()
            return

        if cell is None or cell not in self.position:
            return

        piece = self.position[cell]

        # In game mode, check turn (not in setup_position — free movement)
        if self.app.stage == "game":
            expected_color = WHITE if self.app.white_turn else BLACK
            if piece.color != expected_color:
                return

        # Start drag
        self.drag_piece = piece
        self.drag_from_cell = cell
        self.drag_from_tray = False

        col, row = cell_to_coords(cell)
        x1, y1, x2, y2 = self.get_cell_rect(col, row)
        cx = (x1 + x2) // 2
        cy = (y1 + y2) // 2
        self.drag_offset_x = cx - event.x
        self.drag_offset_y = cy - event.y

        # Remove piece from display
        if cell in self.piece_items:
            self.delete(self.piece_items[cell])
            del self.piece_items[cell]

        # Highlight start cell
        self.highlight_cell(cell, COLOR_HIGHLIGHT_START)

        # Create dragging image
        key = (piece.color, piece.piece_type)
        if key in self.piece_images:
            self.drag_image_id = self.create_image(
                event.x + self.drag_offset_x,
                event.y + self.drag_offset_y,
                image=self.piece_images[key], anchor="s"
            )
        else:
            from pieces import PIECE_SHORT_NAMES
            text_color = "#FFFFFF" if piece.color == BLACK else "#000000"
            self.drag_image_id = self.create_text(
                event.x, event.y,
                text=PIECE_SHORT_NAMES[piece.piece_type],
                font=("Arial", int(self.cell_size * 0.3), "bold"),
                fill=text_color
            )

    def _on_drag(self, event):
        """Handle mouse drag."""
        if self.drag_piece is None:
            return

        # Move dragged piece
        if self.drag_image_id:
            self.coords(self.drag_image_id,
                        event.x + self.drag_offset_x,
                        event.y + self.drag_offset_y)

        # Highlight cell under cursor
        cell = self.pixel_to_cell(event.x, event.y)
        if cell != self.hover_cell:
            self.clear_highlights()
            if self.drag_from_cell:
                self.highlight_cell(self.drag_from_cell, COLOR_HIGHLIGHT_START)
            if cell:
                self.highlight_cell(cell, COLOR_HIGHLIGHT_HOVER)
            self.hover_cell = cell

    def _on_drop(self, event):
        """Handle mouse release (drop piece)."""
        if self.drag_piece is None:
            return

        # Clean up drag visuals
        if self.drag_image_id:
            self.delete(self.drag_image_id)
            self.drag_image_id = None
        self.clear_highlights()

        target_cell = self.pixel_to_cell(event.x, event.y)
        # Spec 7.6: piece dropped between cells snaps to last highlighted cell
        if target_cell is None and self.hover_cell is not None:
            target_cell = self.hover_cell
        piece = self.drag_piece
        from_cell = self.drag_from_cell
        from_tray = self.drag_from_tray

        self.drag_piece = None
        self.drag_from_cell = None
        self.hover_cell = None

        if target_cell is None:
            # Dropped outside board
            if from_tray:
                # From tray - just ignore
                self.redraw()
                return
            else:
                # Piece exits board (disappears per spec section 7.6)
                if from_cell and from_cell in self.position:
                    del self.position[from_cell]
                self.redraw()
                # Show horizontal double arrow at nearest board edge
                self._show_exit_arrow(event.x, event.y)
                self.app.on_piece_exited_board()
                return

        if from_tray:
            # Analysis mode: place piece from tray
            self.position[target_cell] = piece
            self.redraw()
            return

        if target_cell == from_cell:
            # Dropped on same cell - cancel move
            self.redraw()
            return

        # Check if target has own piece (forbidden)
        target_piece = self.position.get(target_cell)
        if target_piece and target_piece.color == piece.color:
            # Return piece to original position
            self.redraw()
            return

        # Execute move
        self._execute_move(from_cell, target_cell, piece, target_piece)

    def _execute_move(self, from_cell, target_cell, piece, target_piece):
        """Execute a move and handle special rules."""
        # Check for Razvedchik exchange
        razvedchik_exchange = is_razvedchik_exchange(piece, target_cell, target_piece)

        # Remove piece from source
        if from_cell in self.position:
            del self.position[from_cell]

        if razvedchik_exchange:
            # Both pieces disappear (section 12)
            if target_cell in self.position:
                del self.position[target_cell]
            self.last_move_cell = target_cell
            self.redraw()
            self.app.on_move_made(from_cell, target_cell, exchange=True)
            return

        # Normal move or capture
        self.position[target_cell] = piece
        self.last_move_cell = target_cell

        # Check Knekht auto-promotion
        promoted = check_knekht_promotion(target_cell, piece)
        if promoted:
            self.position[target_cell] = promoted
            piece = promoted

        # Check Ver Knekht promotion choice
        vk_options = check_ver_knekht_promotion(target_cell, piece)
        if vk_options:
            self.redraw()
            self.app.show_promotion_dialog(target_cell, piece, vk_options)
            return

        # Check Prince promotion choice
        pr_options = check_prince_promotion(target_cell, piece)
        if pr_options:
            self.redraw()
            self.app.show_promotion_dialog(target_cell, piece, pr_options)
            return

        self.redraw()
        self.app.on_move_made(from_cell, target_cell)

    def enter_deletion_mode(self):
        """Enter deletion mode — next click on a piece selects it."""
        self.deletion_mode = True
        self.selected_for_deletion = None

    def select_piece_for_deletion(self, cell):
        """Highlight a piece for deletion."""
        self.selected_for_deletion = cell
        self.deletion_mode = False
        self.redraw()
        if cell:
            self.highlight_cell(cell, "#FF6666")

    def delete_selected_piece(self):
        """Delete the currently selected piece."""
        if self.selected_for_deletion and self.selected_for_deletion in self.position:
            del self.position[self.selected_for_deletion]
            self.selected_for_deletion = None
            self.deletion_mode = False
            self.redraw()
            return True
        return False

    def _show_exit_arrow(self, px, py):
        """Show horizontal double arrow at the nearest board edge (spec 7.6)."""
        board_left = self.board_x
        board_right = self.board_x + 8 * self.cell_size
        board_top = self.board_y
        board_bottom = self.board_y + 8 * self.cell_size

        # Find nearest edge point
        cx = max(board_left, min(px, board_right))
        cy = max(board_top, min(py, board_bottom))
        # Clamp to edge
        if px < board_left:
            cx = board_left
        elif px > board_right:
            cx = board_right
        if py < board_top:
            cy = board_top
        elif py > board_bottom:
            cy = board_bottom

        arrow_size = self.cell_size // 2
        self.create_text(
            cx, cy, text="\u2194", font=("Arial", arrow_size, "bold"),
            fill="#CC0000", tags="exit_arrow"
        )
        # Auto-remove after 2 seconds
        self.after(2000, lambda: self.delete("exit_arrow"))

    def set_position(self, position):
        """Set board position."""
        self.position = copy_position(position) if position else {}
        self.last_move_cell = None
        self.redraw()

    def get_position(self):
        """Get current position as a deep copy."""
        return copy_position(self.position)

    def clear_board(self):
        """Remove all pieces."""
        self.position.clear()
        self.last_move_cell = None
        self.redraw()

    def reverse(self):
        """Flip board orientation."""
        self.reversed = not self.reversed
        self.redraw()
