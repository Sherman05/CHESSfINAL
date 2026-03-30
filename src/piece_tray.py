"""
Piece tray (касса фигур) for Analysis mode position setup.
Shows all piece types for one color, allowing drag to board.
"""

import tkinter as tk
from pieces import ALL_PIECE_TYPES, PIECE_SHORT_NAMES, WHITE, BLACK, Piece


class PieceTray(tk.Frame):
    """Tray showing all pieces of one color for drag & drop to board."""

    def __init__(self, parent, color, board, piece_images, cell_size, **kwargs):
        bg = "#1A237E" if color == BLACK else "#E8EAF6"
        super().__init__(parent, bg=bg, bd=2, relief="raised", **kwargs)
        self.color = color
        self.board = board
        self.piece_images = piece_images
        self.cell_size = cell_size
        self.piece_buttons = []

        self._build_ui()

    def _build_ui(self):
        """Build tray with piece icons."""
        bg = self["bg"]
        fg = "white" if self.color == BLACK else "#333"

        title = "Чёрные" if self.color == BLACK else "Белые"
        tk.Label(self, text=title, font=("Arial", 10, "bold"),
                 fg=fg, bg=bg).pack(pady=(5, 3))

        for ptype in ALL_PIECE_TYPES:
            frame = tk.Frame(self, bg=bg)
            frame.pack(pady=2, padx=5, fill="x")

            key = (self.color, ptype)
            piece = Piece(self.color, ptype)

            if key in self.piece_images:
                btn = tk.Label(
                    frame, image=self.piece_images[key],
                    bg=bg, cursor="hand2"
                )
            else:
                btn_bg = "#555" if self.color == BLACK else "#DDD"
                btn_fg = "white" if self.color == BLACK else "black"
                btn = tk.Label(
                    frame, text=PIECE_SHORT_NAMES[ptype],
                    font=("Arial", 12, "bold"),
                    fg=btn_fg, bg=btn_bg, width=4, height=1,
                    relief="raised", cursor="hand2"
                )
            btn.pack(side="left")

            name_label = tk.Label(
                frame, text=f" {PIECE_SHORT_NAMES[ptype]}",
                font=("Arial", 9), fg=fg, bg=bg
            )
            name_label.pack(side="left", padx=(5, 0))

            # Bind drag start
            btn.bind("<Button-1>", lambda e, p=piece: self._start_drag(e, p))
            btn.bind("<B1-Motion>", self._on_drag)
            btn.bind("<ButtonRelease-1>", self._on_drop)

            self.piece_buttons.append(btn)

    def _start_drag(self, event, piece):
        """Start dragging a piece from tray to board."""
        self.board.drag_piece = piece.copy()
        self.board.drag_from_cell = None
        self.board.drag_from_tray = True
        self.board.drag_offset_x = 0
        self.board.drag_offset_y = 0

        # Create drag image on board canvas
        key = (piece.color, piece.piece_type)
        bx = self.board.winfo_rootx()
        by = self.board.winfo_rooty()
        mx = event.x_root - bx
        my = event.y_root - by

        if key in self.board.piece_images:
            self.board.drag_image_id = self.board.create_image(
                mx, my, image=self.board.piece_images[key], anchor="center"
            )
        else:
            self.board.drag_image_id = self.board.create_text(
                mx, my, text=PIECE_SHORT_NAMES[piece.piece_type],
                font=("Arial", 14, "bold"), fill="red"
            )

    def _on_drag(self, event):
        """Handle drag motion."""
        if self.board.drag_piece is None:
            return

        bx = self.board.winfo_rootx()
        by = self.board.winfo_rooty()
        mx = event.x_root - bx
        my = event.y_root - by

        if self.board.drag_image_id:
            self.board.coords(self.board.drag_image_id, mx, my)

        cell = self.board.pixel_to_cell(mx, my)
        if cell != self.board.hover_cell:
            self.board.clear_highlights()
            if cell:
                self.board.highlight_cell(cell, "#90EE90")
            self.board.hover_cell = cell

    def _on_drop(self, event):
        """Handle drop on board."""
        if self.board.drag_piece is None:
            return

        if self.board.drag_image_id:
            self.board.delete(self.board.drag_image_id)
            self.board.drag_image_id = None
        self.board.clear_highlights()

        bx = self.board.winfo_rootx()
        by = self.board.winfo_rooty()
        mx = event.x_root - bx
        my = event.y_root - by

        target = self.board.pixel_to_cell(mx, my)
        piece = self.board.drag_piece

        self.board.drag_piece = None
        self.board.hover_cell = None
        self.board.drag_from_tray = False

        if target:
            self.board.position[target] = piece
            self.board.redraw()

    def update_images(self, piece_images, cell_size):
        """Update with new images after resize."""
        self.piece_images = piece_images
        self.cell_size = cell_size
        # Rebuild
        for w in self.winfo_children():
            w.destroy()
        self.piece_buttons.clear()
        self._build_ui()
