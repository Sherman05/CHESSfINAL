using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ChessT1
{
    /// <summary>
    /// Piece tray panel for analysis mode position setup.
    /// Shows all 7 piece types for one color, allowing drag-copy to board.
    /// Vertical layout with circle+abbreviation for each piece.
    /// </summary>
    public class PieceTray : Panel
    {
        private PieceColor _color;
        private Control _board;
        private List<Label> _pieceLabels;

        // Drag state
        private Piece _dragPiece;
        private Point _dragStartScreen;
        private bool _isDragging;
        private Form _dragOverlay;
        private PieceType[] _allTypes;

        // Colors
        private Color _bgColor;
        private Color _fgColor;

        /// <summary>
        /// Event raised when a piece is dropped onto a board cell.
        /// The handler receives the Piece and the screen drop point.
        /// The board should convert screen coordinates to a cell and place the piece.
        /// </summary>
        public event PieceDroppedHandler PieceDropped;
        public delegate void PieceDroppedHandler(Piece piece, Point screenPoint);

        /// <summary>
        /// Create a piece tray for one color.
        /// </summary>
        /// <param name="color">PieceColor.White or PieceColor.Black.</param>
        /// <param name="board">The board control, used for coordinate mapping during drag.
        /// Can be null if PieceDropped event is used instead.</param>
        public PieceTray(PieceColor color, Control board)
        {
            _color = color;
            _board = board;
            _pieceLabels = new List<Label>();
            _isDragging = false;

            _allTypes = new PieceType[]
            {
                PieceType.King,
                PieceType.Konnet,
                PieceType.Prince,
                PieceType.Ritter,
                PieceType.Knekht,
                PieceType.VerKnekht,
                PieceType.Razvedchik
            };

            // Dark background for black pieces, light for white
            if (_color == PieceColor.Black)
            {
                _bgColor = Color.FromArgb(58, 58, 58);  // #3A3A3A
                _fgColor = Color.White;
            }
            else
            {
                _bgColor = Color.FromArgb(224, 228, 232); // #E0E4E8
                _fgColor = Color.FromArgb(51, 51, 51);
            }

            this.BackColor = _bgColor;
            this.BorderStyle = BorderStyle.FixedSingle;
            this.Width = 90;
            this.AutoScroll = false;

            BuildUI();
        }

        private void BuildUI()
        {
            // Title label
            string title = (_color == PieceColor.Black) ? "\u0427\u0451\u0440\u043d\u044b\u0435" : "\u0411\u0435\u043b\u044b\u0435";
            Label lblTitle = new Label();
            lblTitle.Text = title;
            lblTitle.Font = new Font("Arial", 10, FontStyle.Bold);
            lblTitle.ForeColor = _fgColor;
            lblTitle.BackColor = _bgColor;
            lblTitle.TextAlign = ContentAlignment.MiddleCenter;
            lblTitle.Size = new Size(this.Width - 2, 24);
            lblTitle.Location = new Point(0, 5);
            this.Controls.Add(lblTitle);

            int yOffset = 34;

            for (int i = 0; i < _allTypes.Length; i++)
            {
                PieceType pt = _allTypes[i];
                string shortName = PieceData.ShortNames[pt];

                // Container panel for one piece entry
                Panel entry = new Panel();
                entry.Size = new Size(this.Width - 4, 50);
                entry.Location = new Point(2, yOffset);
                entry.BackColor = _bgColor;
                this.Controls.Add(entry);

                // Custom-painted piece circle label
                PieceCircleLabel circleLabel = new PieceCircleLabel(_color, shortName);
                circleLabel.Size = new Size(40, 40);
                circleLabel.Location = new Point(4, 5);
                circleLabel.BackColor = _bgColor;
                circleLabel.Cursor = Cursors.Hand;
                circleLabel.Tag = pt;
                entry.Controls.Add(circleLabel);

                // Text label with abbreviation
                Label nameLabel = new Label();
                nameLabel.Text = shortName;
                nameLabel.Font = new Font("Arial", 9);
                nameLabel.ForeColor = _fgColor;
                nameLabel.BackColor = _bgColor;
                nameLabel.AutoSize = true;
                nameLabel.Location = new Point(48, 15);
                entry.Controls.Add(nameLabel);

                // Bind drag events on the circle
                circleLabel.MouseDown += new MouseEventHandler(OnPieceMouseDown);
                circleLabel.MouseMove += new MouseEventHandler(OnPieceMouseMove);
                circleLabel.MouseUp += new MouseEventHandler(OnPieceMouseUp);

                _pieceLabels.Add(circleLabel);
                yOffset += 52;
            }

            // Set preferred height
            this.Height = yOffset + 10;
        }

        /// <summary>
        /// Rebuild the tray (e.g. after a resize).
        /// </summary>
        public void RebuildUI()
        {
            this.Controls.Clear();
            _pieceLabels.Clear();
            BuildUI();
        }

        // ---- Drag handling ----

        private void OnPieceMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;

            Control ctrl = (Control)sender;
            PieceType pt = (PieceType)ctrl.Tag;
            _dragPiece = new Piece(_color, pt);
            _dragStartScreen = ctrl.PointToScreen(e.Location);
            _isDragging = false;
        }

        private void OnPieceMouseMove(object sender, MouseEventArgs e)
        {
            if (_dragPiece == null)
                return;

            Control ctrl = (Control)sender;
            Point currentScreen = ctrl.PointToScreen(e.Location);

            // Start drag after a small threshold (3 pixels)
            if (!_isDragging)
            {
                int dx = Math.Abs(currentScreen.X - _dragStartScreen.X);
                int dy = Math.Abs(currentScreen.Y - _dragStartScreen.Y);
                if (dx < 3 && dy < 3)
                    return;

                _isDragging = true;
                CreateDragOverlay();
            }

            // Move the drag overlay
            if (_dragOverlay != null)
            {
                _dragOverlay.Location = new Point(
                    currentScreen.X - _dragOverlay.Width / 2,
                    currentScreen.Y - _dragOverlay.Height / 2
                );
            }
        }

        private void OnPieceMouseUp(object sender, MouseEventArgs e)
        {
            if (_dragPiece == null)
                return;

            Control ctrl = (Control)sender;
            Point dropScreen = ctrl.PointToScreen(e.Location);

            // Destroy drag overlay
            if (_dragOverlay != null)
            {
                _dragOverlay.Close();
                _dragOverlay.Dispose();
                _dragOverlay = null;
            }

            if (_isDragging)
            {
                // Notify via event: the board should handle placement
                if (PieceDropped != null)
                {
                    PieceDropped(_dragPiece.Copy(), dropScreen);
                }
            }

            _dragPiece = null;
            _isDragging = false;
        }

        /// <summary>
        /// Creates a small transparent overlay form that follows the cursor during drag.
        /// Shows the piece abbreviation in a circle.
        /// </summary>
        private void CreateDragOverlay()
        {
            if (_dragPiece == null)
                return;

            _dragOverlay = new Form();
            _dragOverlay.FormBorderStyle = FormBorderStyle.None;
            _dragOverlay.ShowInTaskbar = false;
            _dragOverlay.TopMost = true;
            _dragOverlay.Size = new Size(48, 48);
            _dragOverlay.StartPosition = FormStartPosition.Manual;
            _dragOverlay.Location = new Point(
                _dragStartScreen.X - 24,
                _dragStartScreen.Y - 24
            );
            _dragOverlay.BackColor = Color.Magenta;
            _dragOverlay.TransparencyKey = Color.Magenta;

            string shortName = PieceData.ShortNames[_dragPiece.Type];
            PieceColor pc = _dragPiece.Color;

            _dragOverlay.Paint += delegate(object s, PaintEventArgs pe)
            {
                Graphics g = pe.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                Color circleColor = (pc == PieceColor.White) ? Color.White : Color.FromArgb(60, 60, 60);
                Color textColor = (pc == PieceColor.White) ? Color.Black : Color.White;

                using (SolidBrush brush = new SolidBrush(circleColor))
                {
                    g.FillEllipse(brush, 2, 2, 44, 44);
                }
                using (Pen pen = new Pen(Color.Gray, 1))
                {
                    g.DrawEllipse(pen, 2, 2, 44, 44);
                }
                using (Font f = new Font("Arial", 12, FontStyle.Bold))
                using (SolidBrush tb = new SolidBrush(textColor))
                {
                    StringFormat sf = new StringFormat();
                    sf.Alignment = StringAlignment.Center;
                    sf.LineAlignment = StringAlignment.Center;
                    g.DrawString(shortName, f, tb, new RectangleF(2, 2, 44, 44), sf);
                }
            };

            _dragOverlay.Show();
        }
    }

    // ================================================================
    // Helper: custom-painted label that draws a circle with abbreviation
    // ================================================================

    /// <summary>
    /// A label control that draws a filled circle with a piece abbreviation.
    /// Used inside PieceTray for each piece type.
    /// </summary>
    internal class PieceCircleLabel : Label
    {
        private PieceColor _pieceColor;
        private string _shortName;

        public PieceCircleLabel(PieceColor pieceColor, string shortName)
        {
            _pieceColor = pieceColor;
            _shortName = shortName;
            this.SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint
                          | ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int size = Math.Min(this.Width, this.Height) - 2;
            int x = (this.Width - size) / 2;
            int y = (this.Height - size) / 2;

            Color circleColor;
            Color textColor;
            if (_pieceColor == PieceColor.White)
            {
                circleColor = Color.White;
                textColor = Color.Black;
            }
            else
            {
                circleColor = Color.FromArgb(60, 60, 60);
                textColor = Color.White;
            }

            // Fill circle
            using (SolidBrush brush = new SolidBrush(circleColor))
            {
                g.FillEllipse(brush, x, y, size, size);
            }

            // Circle border
            using (Pen pen = new Pen(Color.FromArgb(100, 100, 100), 1))
            {
                g.DrawEllipse(pen, x, y, size, size);
            }

            // Abbreviation text
            using (Font f = new Font("Arial", 11, FontStyle.Bold))
            using (SolidBrush tb = new SolidBrush(textColor))
            {
                StringFormat sf = new StringFormat();
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Center;
                RectangleF textRect = new RectangleF(x, y, size, size);
                g.DrawString(_shortName, f, tb, textRect, sf);
            }
        }
    }
}
