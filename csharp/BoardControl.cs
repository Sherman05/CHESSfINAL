using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ChessT1
{
    /// <summary>
    /// Custom UserControl for the 8x8 chess-T1 game board.
    /// Supports rendering, drag-and-drop, piece deletion, and board reversal.
    /// </summary>
    public class BoardControl : UserControl
    {
        // ---- Colors ----
        private static readonly Color ColorWhiteCell = Color.White;
        private static readonly Color ColorBlackCell = Color.Gray; // RGB(128,128,128)
        private static readonly Color ColorCastleCell = Color.FromArgb(200, 200, 200);
        private static readonly Color ColorCastleHatch = Color.FromArgb(160, 160, 160);
        private static readonly Color ColorBorder = Color.FromArgb(74, 144, 200);
        private static readonly Color ColorNotation = Color.FromArgb(208, 216, 232);
        private static readonly Color ColorHighlightStart = Color.Yellow;
        private static readonly Color ColorHighlightHover = Color.FromArgb(144, 238, 144);
        private static readonly Color ColorHighlightLast = Color.FromArgb(206, 210, 107);
        private static readonly Color ColorDeletionHighlight = Color.FromArgb(255, 102, 102);

        // Castle cells
        private static readonly HashSet<string> WhiteCastle = new HashSet<string> { "c1", "d1", "e1", "f1" };
        private static readonly HashSet<string> BlackCastle = new HashSet<string> { "c8", "d8", "e8", "f8" };

        // ---- Events ----
        public event Action<string, string> MoveMade;
        public event Action<string, Piece, PieceType[]> PromotionNeeded;
        public event Action PieceExitedBoard;
        public event Action<string, string, bool> RazvedchikExchange;

        // ---- Public properties ----
        private Dictionary<string, Piece> _position = new Dictionary<string, Piece>();
        public Dictionary<string, Piece> Position
        {
            get { return _position; }
            set { _position = value ?? new Dictionary<string, Piece>(); }
        }

        private bool _reversed;
        public bool Reversed
        {
            get { return _reversed; }
            set { _reversed = value; Invalidate(); }
        }

        private string _lastMoveCell;
        public string LastMoveCell
        {
            get { return _lastMoveCell; }
            set { _lastMoveCell = value; Invalidate(); }
        }

        private bool _deletionMode;
        public bool DeletionMode
        {
            get { return _deletionMode; }
            set { _deletionMode = value; }
        }

        private string _selectedForDeletion;
        public string SelectedForDeletion
        {
            get { return _selectedForDeletion; }
            set { _selectedForDeletion = value; }
        }

        /// <summary>
        /// Reference to the parent form's app stage for turn checking.
        /// Set by MainForm. Values: "startup", "game", "setup_position".
        /// </summary>
        public string AppStage = "startup";

        /// <summary>
        /// Current turn color. Set by MainForm during game mode.
        /// </summary>
        public PieceColor CurrentTurnColor = PieceColor.White;

        // ---- Computed layout ----
        private int _cellSize;
        public int CellSize
        {
            get { return _cellSize; }
        }

        private int _boardX;
        private int _boardY;

        // ---- Drag state ----
        private Piece _dragPiece;
        private string _dragFromCell;
        private Point _dragOffset;
        private Point _dragCurrentPos;
        private bool _isDragging;
        private string _hoverCell;

        // ---- Drag from tray (analysis setup) ----
        private bool _dragFromTray;
        private Piece _dragTrayPiece;

        // ---- Exit arrow ----
        private Point? _exitArrowPos;
        private Timer _exitArrowTimer;

        public BoardControl()
        {
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint |
                          ControlStyles.UserPaint |
                          ControlStyles.OptimizedDoubleBuffer, true);

            _exitArrowTimer = new Timer();
            _exitArrowTimer.Interval = 2000;
            _exitArrowTimer.Tick += delegate
            {
                _exitArrowTimer.Stop();
                _exitArrowPos = null;
                Invalidate();
            };

            RecalculateLayout();
        }

        // ---- Layout ----

        private void RecalculateLayout()
        {
            int available = Math.Min(Width, Height);
            _cellSize = Math.Max(30, (available - 100) / 8);
            _boardX = (int)(_cellSize * 0.57);
            _boardY = (int)(_cellSize * 0.57);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            RecalculateLayout();
            Invalidate();
        }

        // ---- Position management ----

        public void SetPosition(Dictionary<string, Piece> position)
        {
            _position = PieceData.CopyPosition(position);
            _lastMoveCell = null;
            _selectedForDeletion = null;
            _deletionMode = false;
            Invalidate();
        }

        public Dictionary<string, Piece> GetPosition()
        {
            return PieceData.CopyPosition(_position);
        }

        public void ClearBoard()
        {
            _position.Clear();
            _lastMoveCell = null;
            _selectedForDeletion = null;
            _deletionMode = false;
            Invalidate();
        }

        public void Reverse()
        {
            _reversed = !_reversed;
            Invalidate();
        }

        // ---- Tray drag support ----

        /// <summary>
        /// Begin dragging a piece from a tray onto the board.
        /// Called by the tray control in analysis setup.
        /// </summary>
        public void BeginTrayDrag(Piece piece, Point screenPos)
        {
            _dragFromTray = true;
            _dragTrayPiece = piece;
            _dragPiece = piece;
            _dragFromCell = null;
            _isDragging = true;
            _dragCurrentPos = PointToClient(screenPos);
            _hoverCell = null;
            Invalidate();
        }

        // ---- Coordinate helpers ----

        private static void CellToCoords(string cell, out int col, out int row)
        {
            col = cell[0] - 'a';
            row = cell[1] - '1';
        }

        private static string CoordsToCell(int col, int row)
        {
            return string.Format("{0}{1}", (char)('a' + col), row + 1);
        }

        private Rectangle GetCellRect(int col, int row)
        {
            int displayCol, displayRow;
            if (_reversed)
            {
                displayCol = 7 - col;
                displayRow = row;
            }
            else
            {
                displayCol = col;
                displayRow = 7 - row;
            }

            int x = _boardX + displayCol * _cellSize;
            int y = _boardY + displayRow * _cellSize;
            return new Rectangle(x, y, _cellSize, _cellSize);
        }

        private string PixelToCell(int px, int py)
        {
            int displayCol = (px - _boardX) / _cellSize;
            int displayRow = (py - _boardY) / _cellSize;

            if (displayCol < 0 || displayCol >= 8 || displayRow < 0 || displayRow >= 8)
                return null;

            int col, row;
            if (_reversed)
            {
                col = 7 - displayCol;
                row = displayRow;
            }
            else
            {
                col = displayCol;
                row = 7 - displayRow;
            }

            if (col < 0 || col >= 8 || row < 0 || row >= 8)
                return null;

            return CoordsToCell(col, row);
        }

        private static bool IsCastleCell(string cell)
        {
            return WhiteCastle.Contains(cell) || BlackCastle.Contains(cell);
        }

        // ---- Rendering ----

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            RecalculateLayout();

            // Blue border
            using (Pen borderPen = new Pen(ColorBorder, 3f))
            {
                g.DrawRectangle(borderPen,
                    _boardX - 3, _boardY - 3,
                    8 * _cellSize + 6, 8 * _cellSize + 6);
            }

            // Draw cells
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    string cellName = CoordsToCell(col, row);
                    Rectangle rect = GetCellRect(col, row);

                    // Determine cell color
                    Color cellColor;
                    bool isCastle = IsCastleCell(cellName);
                    if (isCastle)
                    {
                        cellColor = ColorCastleCell;
                    }
                    else if ((col + row) % 2 == 0)
                    {
                        cellColor = ColorBlackCell;
                    }
                    else
                    {
                        cellColor = ColorWhiteCell;
                    }

                    using (SolidBrush cellBrush = new SolidBrush(cellColor))
                    {
                        g.FillRectangle(cellBrush, rect);
                    }

                    // Castle hatching
                    if (isCastle)
                    {
                        using (Pen hatchPen = new Pen(ColorCastleHatch, 1f))
                        {
                            int step = Math.Max(8, _cellSize / 6);
                            // Clip to cell
                            Region oldClip = g.Clip;
                            g.SetClip(rect);
                            for (int offset = -_cellSize; offset < _cellSize * 2; offset += step)
                            {
                                g.DrawLine(hatchPen,
                                    rect.X + offset, rect.Y,
                                    rect.X + offset + _cellSize, rect.Y + _cellSize);
                            }
                            g.Clip = oldClip;
                        }
                    }
                }
            }

            // Highlight last move cell
            if (!string.IsNullOrEmpty(_lastMoveCell))
            {
                int lc, lr;
                CellToCoords(_lastMoveCell, out lc, out lr);
                Rectangle lastRect = GetCellRect(lc, lr);
                using (SolidBrush lastBrush = new SolidBrush(Color.FromArgb(128, ColorHighlightLast)))
                {
                    g.FillRectangle(lastBrush, lastRect);
                }
            }

            // Highlight deletion selection
            if (!string.IsNullOrEmpty(_selectedForDeletion))
            {
                int dc, dr;
                CellToCoords(_selectedForDeletion, out dc, out dr);
                Rectangle delRect = GetCellRect(dc, dr);
                using (SolidBrush delBrush = new SolidBrush(Color.FromArgb(160, ColorDeletionHighlight)))
                {
                    g.FillRectangle(delBrush, delRect);
                }
            }

            // Drag highlights
            if (_isDragging)
            {
                // Highlight start cell (yellow)
                if (!string.IsNullOrEmpty(_dragFromCell))
                {
                    int sc, sr;
                    CellToCoords(_dragFromCell, out sc, out sr);
                    Rectangle startRect = GetCellRect(sc, sr);
                    using (SolidBrush startBrush = new SolidBrush(Color.FromArgb(128, ColorHighlightStart)))
                    {
                        g.FillRectangle(startBrush, startRect);
                    }
                }

                // Highlight hover cell (green)
                if (!string.IsNullOrEmpty(_hoverCell))
                {
                    int hc, hr;
                    CellToCoords(_hoverCell, out hc, out hr);
                    Rectangle hoverRect = GetCellRect(hc, hr);
                    using (SolidBrush hoverBrush = new SolidBrush(Color.FromArgb(128, ColorHighlightHover)))
                    {
                        g.FillRectangle(hoverBrush, hoverRect);
                    }
                }
            }

            // Notation labels
            int fontSize = Math.Max(8, (int)(_cellSize * 0.2));
            using (Font notationFont = new Font("Arial", fontSize))
            using (SolidBrush notationBrush = new SolidBrush(ColorNotation))
            {
                StringFormat sf = new StringFormat();
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Center;

                for (int col = 0; col < 8; col++)
                {
                    char letter = _reversed ? (char)('h' - col) : (char)('a' + col);
                    string s = letter.ToString();
                    float x = _boardX + col * _cellSize + _cellSize / 2f;

                    // Bottom
                    g.DrawString(s, notationFont, notationBrush,
                        x, _boardY + 8 * _cellSize + fontSize + 5, sf);
                    // Top
                    g.DrawString(s, notationFont, notationBrush,
                        x, _boardY - fontSize - 2, sf);
                }

                for (int row = 0; row < 8; row++)
                {
                    string num = _reversed ? (row + 1).ToString() : (8 - row).ToString();
                    float y = _boardY + row * _cellSize + _cellSize / 2f;

                    // Left
                    g.DrawString(num, notationFont, notationBrush,
                        _boardX - fontSize - 5, y, sf);
                    // Right
                    g.DrawString(num, notationFont, notationBrush,
                        _boardX + 8 * _cellSize + fontSize + 5, y, sf);
                }
            }

            // Draw pieces
            DrawPieces(g);

            // Draw dragged piece at cursor
            if (_isDragging && _dragPiece != null)
            {
                DrawPieceAt(g, _dragPiece,
                    _dragCurrentPos.X + _dragOffset.X,
                    _dragCurrentPos.Y + _dragOffset.Y);
            }

            // Draw exit arrow
            if (_exitArrowPos.HasValue)
            {
                DrawExitArrow(g, _exitArrowPos.Value);
            }
        }

        private void DrawPieces(Graphics g)
        {
            foreach (KeyValuePair<string, Piece> kvp in _position)
            {
                // Skip the piece being dragged from its cell
                if (_isDragging && kvp.Key == _dragFromCell)
                    continue;

                int col, row;
                CellToCoords(kvp.Key, out col, out row);
                Rectangle rect = GetCellRect(col, row);

                int cx = rect.X + rect.Width / 2;
                int cy = rect.Y + rect.Height / 2;

                DrawPieceAt(g, kvp.Value, cx, cy);
            }
        }

        private void DrawPieceAt(Graphics g, Piece piece, int cx, int cy)
        {
            int r = (int)(_cellSize * 0.35);
            Rectangle oval = new Rectangle(cx - r, cy - r, r * 2, r * 2);

            Color fillColor, outlineColor, textColor;
            if (piece.Color == PieceColor.White)
            {
                fillColor = Color.FromArgb(240, 240, 240);
                outlineColor = Color.FromArgb(60, 60, 60);
                textColor = Color.FromArgb(30, 30, 30);
            }
            else
            {
                fillColor = Color.FromArgb(50, 50, 50);
                outlineColor = Color.FromArgb(200, 200, 200);
                textColor = Color.FromArgb(230, 230, 230);
            }

            using (SolidBrush fill = new SolidBrush(fillColor))
            {
                g.FillEllipse(fill, oval);
            }
            using (Pen outline = new Pen(outlineColor, 2f))
            {
                g.DrawEllipse(outline, oval);
            }

            string abbr = PieceData.ShortNames[piece.Type];
            int textSize = Math.Max(8, _cellSize / 4);
            using (Font textFont = new Font("Arial", textSize, FontStyle.Bold))
            using (SolidBrush textBrush = new SolidBrush(textColor))
            {
                StringFormat sf = new StringFormat();
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Center;
                g.DrawString(abbr, textFont, textBrush, cx, cy, sf);
            }
        }

        private void DrawExitArrow(Graphics g, Point pos)
        {
            int half = _cellSize / 3;
            using (Pen arrowPen = new Pen(Color.FromArgb(204, 0, 0), 3f))
            {
                arrowPen.StartCap = LineCap.ArrowAnchor;
                arrowPen.EndCap = LineCap.ArrowAnchor;
                g.DrawLine(arrowPen, pos.X - half, pos.Y, pos.X + half, pos.Y);
            }
        }

        // ---- Mouse events: Drag & Drop ----

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;

            string cell = PixelToCell(e.X, e.Y);

            // Block all interaction in startup mode
            if (AppStage == "startup")
                return;

            // Handle piece deletion mode
            if (_deletionMode)
            {
                if (cell != null && _position.ContainsKey(cell))
                {
                    SelectPieceForDeletion(cell);
                }
                else
                {
                    _selectedForDeletion = null;
                    _deletionMode = false;
                    Invalidate();
                }
                return;
            }

            // Handle already-selected piece for deletion (click elsewhere deselects)
            if (_selectedForDeletion != null)
            {
                _selectedForDeletion = null;
                Invalidate();
                return;
            }

            if (cell == null || !_position.ContainsKey(cell))
                return;

            Piece piece = _position[cell];

            // In game mode, check turn
            if (AppStage == "game")
            {
                if (piece.Color != CurrentTurnColor)
                    return;
            }

            // Start drag
            _dragPiece = piece;
            _dragFromCell = cell;
            _dragFromTray = false;
            _isDragging = true;

            int col, row;
            CellToCoords(cell, out col, out row);
            Rectangle rect = GetCellRect(col, row);
            int cx = rect.X + rect.Width / 2;
            int cy = rect.Y + rect.Height / 2;
            _dragOffset = new Point(cx - e.X, cy - e.Y);
            _dragCurrentPos = new Point(e.X, e.Y);
            _hoverCell = null;

            Invalidate();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (!_isDragging || _dragPiece == null)
                return;

            _dragCurrentPos = new Point(e.X, e.Y);

            string cell = PixelToCell(e.X, e.Y);
            if (cell != _hoverCell)
            {
                _hoverCell = cell;
            }

            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (!_isDragging || _dragPiece == null)
                return;

            string targetCell = PixelToCell(e.X, e.Y);

            // Snap to last hovered cell if dropped between cells
            if (targetCell == null && _hoverCell != null)
                targetCell = _hoverCell;

            Piece piece = _dragPiece;
            string fromCell = _dragFromCell;
            bool fromTray = _dragFromTray;

            // Reset drag state
            _dragPiece = null;
            _dragFromCell = null;
            _isDragging = false;
            _hoverCell = null;
            _dragFromTray = false;

            if (targetCell == null)
            {
                // Dropped outside board
                if (fromTray)
                {
                    // From tray - just ignore
                    Invalidate();
                    return;
                }

                // Piece exits board
                if (fromCell != null && _position.ContainsKey(fromCell))
                {
                    _position.Remove(fromCell);
                }
                Invalidate();

                // Show exit arrow at nearest board edge
                ShowExitArrow(e.X, e.Y);

                if (PieceExitedBoard != null)
                    PieceExitedBoard();
                return;
            }

            if (fromTray)
            {
                // Analysis mode: place piece from tray
                _position[targetCell] = piece;
                Invalidate();
                return;
            }

            if (targetCell == fromCell)
            {
                // Dropped on same cell - cancel
                Invalidate();
                return;
            }

            // Check if target has own piece
            Piece targetPiece = null;
            if (_position.ContainsKey(targetCell))
                targetPiece = _position[targetCell];

            if (targetPiece != null && targetPiece.Color == piece.Color)
            {
                // Own piece - cancel
                Invalidate();
                return;
            }

            // Execute move
            ExecuteMove(fromCell, targetCell, piece, targetPiece);
        }

        private void ExecuteMove(string fromCell, string targetCell, Piece piece, Piece targetPiece)
        {
            // Check for Razvedchik exchange
            bool isExchange = PieceData.IsRazvedchikExchange(piece, targetCell, targetPiece);

            // Remove piece from source
            if (fromCell != null && _position.ContainsKey(fromCell))
                _position.Remove(fromCell);

            if (isExchange)
            {
                // Both pieces disappear
                if (_position.ContainsKey(targetCell))
                    _position.Remove(targetCell);

                _lastMoveCell = targetCell;
                Invalidate();

                if (RazvedchikExchange != null)
                    RazvedchikExchange(fromCell, targetCell, true);
                if (MoveMade != null)
                    MoveMade(fromCell, targetCell);
                return;
            }

            // Normal move or capture (opponent piece removed by overwrite)
            _position[targetCell] = piece;
            _lastMoveCell = targetCell;

            // Check Knekht auto-promotion to Ver Knekht
            Piece promoted = PieceData.CheckKnechtPromotion(targetCell, piece);
            if (promoted != null)
            {
                _position[targetCell] = promoted;
                piece = promoted;
            }

            // Check Ver Knekht promotion choice
            PieceType[] vkOptions = PieceData.CheckVerKnechtPromotion(targetCell, piece);
            if (vkOptions != null)
            {
                Invalidate();
                if (PromotionNeeded != null)
                    PromotionNeeded(targetCell, piece, vkOptions);
                return;
            }

            // Check Prince promotion choice
            PieceType[] prOptions = PieceData.CheckPrincePromotion(targetCell, piece);
            if (prOptions != null)
            {
                Invalidate();
                if (PromotionNeeded != null)
                    PromotionNeeded(targetCell, piece, prOptions);
                return;
            }

            Invalidate();
            if (MoveMade != null)
                MoveMade(fromCell, targetCell);
        }

        // ---- Deletion ----

        public void EnterDeletionMode()
        {
            _deletionMode = true;
            _selectedForDeletion = null;
        }

        private void SelectPieceForDeletion(string cell)
        {
            _selectedForDeletion = cell;
            _deletionMode = false;
            Invalidate();
        }

        public bool DeleteSelected()
        {
            if (_selectedForDeletion != null && _position.ContainsKey(_selectedForDeletion))
            {
                _position.Remove(_selectedForDeletion);
                _selectedForDeletion = null;
                _deletionMode = false;
                Invalidate();
                return true;
            }
            return false;
        }

        // ---- Exit arrow ----

        private void ShowExitArrow(int px, int py)
        {
            int boardLeft = _boardX;
            int boardRight = _boardX + 8 * _cellSize;
            int boardTop = _boardY;
            int boardBottom = _boardY + 8 * _cellSize;

            int cx = Math.Max(boardLeft, Math.Min(px, boardRight));
            int cy = Math.Max(boardTop, Math.Min(py, boardBottom));

            _exitArrowPos = new Point(cx, cy);
            _exitArrowTimer.Stop();
            _exitArrowTimer.Start();
            Invalidate();
        }
    }
}
