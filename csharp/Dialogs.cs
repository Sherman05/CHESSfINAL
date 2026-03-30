using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace ChessT1
{
    // ================================================================
    // Result types for dialogs
    // ================================================================

    /// <summary>
    /// Result of CloseAppDialog.
    /// </summary>
    public enum CloseAppAction
    {
        Cancel,
        EndAndClose,
        CloseKeepSession
    }

    /// <summary>
    /// Result structure for CloseAppDialog.
    /// </summary>
    public class CloseAppResult
    {
        public CloseAppAction Action;
        public bool SavePosition;

        public CloseAppResult(CloseAppAction action, bool savePosition)
        {
            Action = action;
            SavePosition = savePosition;
        }
    }

    /// <summary>
    /// Result structure for EndSessionDialog.
    /// </summary>
    public class EndSessionResult
    {
        public bool Ended;
        public bool SavePosition;

        public EndSessionResult(bool ended, bool savePosition)
        {
            Ended = ended;
            SavePosition = savePosition;
        }
    }

    // ================================================================
    // 1. CreateFolderDialog
    // ================================================================

    /// <summary>
    /// Dialog for creating a game session folder.
    /// Title: "\u0421\u043e\u0437\u0434\u0430\u0442\u044c \u043f\u0430\u043f\u043a\u0443 \u0434\u043b\u044f \u043f\u0430\u0440\u0442\u0438\u0438"
    /// Returns folder path or null on cancel.
    /// </summary>
    public class CreateFolderDialog : Form
    {
        private TextBox _locationBox;
        private TextBox _nameBox;
        private Button _btnCreate;
        private Button _btnCancel;
        private Button _btnBrowse;

        private string _resultPath;

        /// <summary>
        /// The created folder path, or null if cancelled.
        /// </summary>
        public string ResultPath
        {
            get { return _resultPath; }
        }

        public CreateFolderDialog()
        {
            _resultPath = null;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "\u0421\u043e\u0437\u0434\u0430\u0442\u044c \u043f\u0430\u043f\u043a\u0443 \u0434\u043b\u044f \u043f\u0430\u0440\u0442\u0438\u0438";
            this.ClientSize = new Size(500, 200);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(245, 245, 245);
            this.KeyPreview = true;
            this.KeyDown += new KeyEventHandler(OnKeyDown);

            // Title label
            Label lblTitle = new Label();
            lblTitle.Text = "\u0421\u043e\u0437\u0434\u0430\u0442\u044c \u043f\u0430\u043f\u043a\u0443 \u0434\u043b\u044f \u043f\u0430\u0440\u0442\u0438\u0438";
            lblTitle.Font = new Font("Arial", 14, FontStyle.Bold);
            lblTitle.Location = new Point(20, 15);
            lblTitle.AutoSize = true;
            this.Controls.Add(lblTitle);

            // Location label
            Label lblLocation = new Label();
            lblLocation.Text = "\u0420\u0430\u0441\u043f\u043e\u043b\u043e\u0436\u0435\u043d\u0438\u0435:";
            lblLocation.Font = new Font("Arial", 10);
            lblLocation.Location = new Point(20, 55);
            lblLocation.AutoSize = true;
            this.Controls.Add(lblLocation);

            // Location text box
            _locationBox = new TextBox();
            _locationBox.Font = new Font("Arial", 10);
            _locationBox.Location = new Point(140, 53);
            _locationBox.Width = 260;
            _locationBox.Text = GetDesktopPath();
            this.Controls.Add(_locationBox);

            // Browse button
            _btnBrowse = new Button();
            _btnBrowse.Text = "\u041e\u0431\u0437\u043e\u0440...";
            _btnBrowse.Font = new Font("Arial", 9);
            _btnBrowse.Location = new Point(410, 51);
            _btnBrowse.Size = new Size(70, 28);
            _btnBrowse.Click += new EventHandler(OnBrowseClick);
            this.Controls.Add(_btnBrowse);

            // Name label
            Label lblName = new Label();
            lblName.Text = "\u041d\u0430\u0437\u0432\u0430\u043d\u0438\u0435 \u043f\u0430\u0440\u0442\u0438\u0438:";
            lblName.Font = new Font("Arial", 10);
            lblName.Location = new Point(20, 95);
            lblName.AutoSize = true;
            this.Controls.Add(lblName);

            // Name text box
            _nameBox = new TextBox();
            _nameBox.Font = new Font("Arial", 10);
            _nameBox.Location = new Point(165, 93);
            _nameBox.Width = 235;
            _nameBox.Text = "\u041d\u043e\u0432\u0430\u044f \u043f\u0430\u0440\u0442\u0438\u044f";
            _nameBox.SelectAll();
            this.Controls.Add(_nameBox);

            // Create button
            _btnCreate = new Button();
            _btnCreate.Text = "\u0421\u043e\u0437\u0434\u0430\u0442\u044c";
            _btnCreate.Font = new Font("Arial", 11);
            _btnCreate.BackColor = Color.FromArgb(76, 175, 80);
            _btnCreate.ForeColor = Color.White;
            _btnCreate.FlatStyle = FlatStyle.Flat;
            _btnCreate.Size = new Size(100, 32);
            _btnCreate.Location = new Point(380, 150);
            _btnCreate.Click += new EventHandler(OnCreateClick);
            this.Controls.Add(_btnCreate);

            // Cancel button
            _btnCancel = new Button();
            _btnCancel.Text = "\u041e\u0442\u043c\u0435\u043d\u0430";
            _btnCancel.Font = new Font("Arial", 11);
            _btnCancel.Size = new Size(100, 32);
            _btnCancel.Location = new Point(270, 150);
            _btnCancel.Click += new EventHandler(OnCancelClick);
            this.Controls.Add(_btnCancel);

            this.AcceptButton = _btnCreate;
            this.CancelButton = _btnCancel;
        }

        private static string GetDesktopPath()
        {
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            if (!string.IsNullOrEmpty(desktop) && Directory.Exists(desktop))
                return desktop;
            desktop = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Desktop");
            if (Directory.Exists(desktop))
                return desktop;
            return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }

        private void OnBrowseClick(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.Description = "\u0412\u044b\u0431\u0435\u0440\u0438\u0442\u0435 \u0440\u0430\u0441\u043f\u043e\u043b\u043e\u0436\u0435\u043d\u0438\u0435";
            if (Directory.Exists(_locationBox.Text))
                fbd.SelectedPath = _locationBox.Text;
            if (fbd.ShowDialog(this) == DialogResult.OK)
            {
                _locationBox.Text = fbd.SelectedPath;
            }
        }

        private void OnCreateClick(object sender, EventArgs e)
        {
            string name = _nameBox.Text.Trim();
            string location = _locationBox.Text.Trim();

            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show(this, "\u0412\u0432\u0435\u0434\u0438\u0442\u0435 \u043d\u0430\u0437\u0432\u0430\u043d\u0438\u0435 \u043f\u0430\u0440\u0442\u0438\u0438", "\u041e\u0448\u0438\u0431\u043a\u0430",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string folderPath = Path.Combine(location, name);
            try
            {
                Directory.CreateDirectory(folderPath);
                _resultPath = folderPath;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    "\u041d\u0435 \u0443\u0434\u0430\u043b\u043e\u0441\u044c \u0441\u043e\u0437\u0434\u0430\u0442\u044c \u043f\u0430\u043f\u043a\u0443:\n" + ex.Message,
                    "\u041e\u0448\u0438\u0431\u043a\u0430", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnCancelClick(object sender, EventArgs e)
        {
            _resultPath = null;
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                OnCancelClick(sender, e);
            }
        }
    }

    // ================================================================
    // 2. PromotionDialog
    // ================================================================

    /// <summary>
    /// Dialog for piece promotion choice.
    /// Borderless, TopMost, dark blue background.
    /// Shows piece options as clickable panels with text labels.
    /// </summary>
    public class PromotionDialog : Form
    {
        private PieceType _chosenType;
        private PieceType[] _options;
        private PieceColor _pieceColor;

        /// <summary>
        /// The chosen piece type after dialog closes.
        /// </summary>
        public PieceType ChosenType
        {
            get { return _chosenType; }
        }

        /// <summary>
        /// Create a promotion dialog.
        /// </summary>
        /// <param name="pieceColor">Color of the promoting piece.</param>
        /// <param name="options">Array of PieceType options to choose from.</param>
        /// <param name="parentBounds">Bounds of the parent form for positioning.</param>
        public PromotionDialog(PieceColor pieceColor, PieceType[] options, Rectangle parentBounds)
        {
            _pieceColor = pieceColor;
            _options = options;
            _chosenType = options[0]; // default fallback
            InitializeComponent(parentBounds);
        }

        private void InitializeComponent(Rectangle parentBounds)
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.TopMost = true;
            this.StartPosition = FormStartPosition.Manual;
            this.BackColor = Color.FromArgb(44, 62, 107); // #2C3E6B
            this.ShowInTaskbar = false;
            this.KeyPreview = true;

            // Build content
            int panelWidth = 80;
            int panelHeight = 80;
            int padding = 8;
            int labelHeight = 25;

            // Top label
            Label lblTitle = new Label();
            lblTitle.Text = "\u041f\u0440\u0435\u0432\u0440\u0430\u0449\u0435\u043d\u0438\u0435:";
            lblTitle.Font = new Font("Arial", 10);
            lblTitle.ForeColor = Color.White;
            lblTitle.BackColor = Color.FromArgb(44, 62, 107);
            lblTitle.Location = new Point(padding, padding);
            lblTitle.AutoSize = true;
            this.Controls.Add(lblTitle);

            int startY = padding + labelHeight;
            int totalWidth = _options.Length * (panelWidth + padding) + padding;
            int totalHeight = startY + panelHeight + 20 + padding;

            for (int i = 0; i < _options.Length; i++)
            {
                PieceType pt = _options[i];
                string shortName = PieceData.ShortNames[pt];

                // Clickable panel
                Panel pnl = new Panel();
                pnl.Size = new Size(panelWidth, panelHeight);
                pnl.Location = new Point(padding + i * (panelWidth + padding), startY);
                pnl.BackColor = Color.FromArgb(74, 90, 138); // #4A5A8A
                pnl.Cursor = Cursors.Hand;
                pnl.Tag = pt;
                pnl.Paint += new PaintEventHandler(OnPiecePanelPaint);
                pnl.Click += new EventHandler(OnPieceClick);
                this.Controls.Add(pnl);

                // Label below panel
                Label lblName = new Label();
                lblName.Text = shortName;
                lblName.Font = new Font("Arial", 8);
                lblName.ForeColor = Color.FromArgb(204, 204, 204);
                lblName.BackColor = Color.FromArgb(44, 62, 107);
                lblName.TextAlign = ContentAlignment.MiddleCenter;
                lblName.Size = new Size(panelWidth, 16);
                lblName.Location = new Point(padding + i * (panelWidth + padding), startY + panelHeight + 2);
                this.Controls.Add(lblName);
            }

            this.ClientSize = new Size(totalWidth, totalHeight);

            // Position: top-right for white, bottom-right for black
            int x = parentBounds.Right - totalWidth - 20;
            int y;
            if (_pieceColor == PieceColor.White)
            {
                y = parentBounds.Top + 50;
            }
            else
            {
                y = parentBounds.Bottom - totalHeight - 50;
            }
            this.Location = new Point(x, y);
        }

        private void OnPiecePanelPaint(object sender, PaintEventArgs e)
        {
            Panel pnl = (Panel)sender;
            PieceType pt = (PieceType)pnl.Tag;
            string shortName = PieceData.ShortNames[pt];

            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // Draw circle at ~80% of panel size
            int circleSize = (int)(Math.Min(pnl.Width, pnl.Height) * 0.8f);
            int cx = (pnl.Width - circleSize) / 2;
            int cy = (pnl.Height - circleSize) / 2;

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

            using (SolidBrush brush = new SolidBrush(circleColor))
            {
                g.FillEllipse(brush, cx, cy, circleSize, circleSize);
            }
            using (Pen pen = new Pen(Color.FromArgb(100, 100, 100), 1))
            {
                g.DrawEllipse(pen, cx, cy, circleSize, circleSize);
            }

            // Draw abbreviation text centered in circle
            using (Font f = new Font("Arial", 14, FontStyle.Bold))
            using (SolidBrush tb = new SolidBrush(textColor))
            {
                StringFormat sf = new StringFormat();
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Center;
                RectangleF textRect = new RectangleF(cx, cy, circleSize, circleSize);
                g.DrawString(shortName, f, tb, textRect, sf);
            }
        }

        private void OnPieceClick(object sender, EventArgs e)
        {
            Panel pnl = (Panel)sender;
            _chosenType = (PieceType)pnl.Tag;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }

    // ================================================================
    // 3. EndSessionDialog
    // ================================================================

    /// <summary>
    /// Dialog for ending a game session.
    /// Returns (ended, savePosition).
    /// </summary>
    public class EndSessionDialog : Form
    {
        private CheckBox _chkSave;
        private Button _btnEnd;
        private Button _btnCancel;
        private EndSessionResult _result;

        /// <summary>
        /// The result after dialog closes.
        /// </summary>
        public EndSessionResult Result
        {
            get { return _result; }
        }

        public EndSessionDialog()
        {
            _result = new EndSessionResult(false, false);
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "\u0417\u0430\u0432\u0435\u0440\u0448\u0438\u0442\u044c \u043f\u0430\u0440\u0442\u0438\u044e";
            this.ClientSize = new Size(400, 180);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(245, 245, 245);
            this.KeyPreview = true;
            this.KeyDown += new KeyEventHandler(OnKeyDown);

            // Title label
            Label lblTitle = new Label();
            lblTitle.Text = "\u0417\u0430\u0432\u0435\u0440\u0448\u0438\u0442\u044c \u043f\u0430\u0440\u0442\u0438\u044e?";
            lblTitle.Font = new Font("Arial", 14, FontStyle.Bold);
            lblTitle.Location = new Point(20, 15);
            lblTitle.AutoSize = true;
            this.Controls.Add(lblTitle);

            // Save checkbox
            _chkSave = new CheckBox();
            _chkSave.Text = "\u0421\u043e\u0445\u0440\u0430\u043d\u0438\u0442\u044c \u043f\u043e\u0437\u0438\u0446\u0438\u044e";
            _chkSave.Font = new Font("Arial", 11);
            _chkSave.Checked = true;
            _chkSave.Location = new Point(20, 60);
            _chkSave.AutoSize = true;
            this.Controls.Add(_chkSave);

            // End button
            _btnEnd = new Button();
            _btnEnd.Text = "\u0417\u0430\u0432\u0435\u0440\u0448\u0438\u0442\u044c \u043f\u0430\u0440\u0442\u0438\u044e";
            _btnEnd.Font = new Font("Arial", 11);
            _btnEnd.BackColor = Color.FromArgb(229, 57, 53); // #E53935
            _btnEnd.ForeColor = Color.White;
            _btnEnd.FlatStyle = FlatStyle.Flat;
            _btnEnd.Size = new Size(170, 35);
            _btnEnd.Location = new Point(210, 125);
            _btnEnd.Click += new EventHandler(OnEndClick);
            this.Controls.Add(_btnEnd);

            // Cancel button
            _btnCancel = new Button();
            _btnCancel.Text = "\u041e\u0442\u043c\u0435\u043d\u0430";
            _btnCancel.Font = new Font("Arial", 11);
            _btnCancel.Size = new Size(100, 35);
            _btnCancel.Location = new Point(100, 125);
            _btnCancel.Click += new EventHandler(OnCancelClick);
            this.Controls.Add(_btnCancel);

            this.AcceptButton = _btnEnd;
            this.CancelButton = _btnCancel;
        }

        private void OnEndClick(object sender, EventArgs e)
        {
            _result = new EndSessionResult(true, _chkSave.Checked);
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void OnCancelClick(object sender, EventArgs e)
        {
            _result = new EndSessionResult(false, false);
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                OnCancelClick(sender, e);
            }
        }
    }

    // ================================================================
    // 4. CloseAppDialog
    // ================================================================

    /// <summary>
    /// Dialog shown when closing app during active session.
    /// Returns CloseAppResult with action and save booleans.
    /// </summary>
    public class CloseAppDialog : Form
    {
        private CheckBox _chkSave1;
        private CheckBox _chkSave2;
        private Button _btnEndAndClose;
        private Button _btnCloseKeep;
        private Button _btnCancel;
        private CloseAppResult _result;

        /// <summary>
        /// The result after dialog closes.
        /// </summary>
        public CloseAppResult Result
        {
            get { return _result; }
        }

        public CloseAppDialog()
        {
            _result = new CloseAppResult(CloseAppAction.Cancel, false);
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "\u0417\u0430\u043a\u0440\u044b\u0442\u044c \u043f\u0440\u043e\u0433\u0440\u0430\u043c\u043c\u0443";
            this.ClientSize = new Size(480, 250);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(245, 245, 245);
            this.KeyPreview = true;
            this.KeyDown += new KeyEventHandler(OnKeyDown);

            // Title
            Label lblTitle = new Label();
            lblTitle.Text = "\u0417\u0430\u043a\u0440\u044b\u0442\u044c \u043f\u0440\u043e\u0433\u0440\u0430\u043c\u043c\u0443?";
            lblTitle.Font = new Font("Arial", 14, FontStyle.Bold);
            lblTitle.Location = new Point(20, 15);
            lblTitle.AutoSize = true;
            this.Controls.Add(lblTitle);

            // Subtitle
            Label lblSub = new Label();
            lblSub.Text = "\u0421\u0435\u0430\u043d\u0441 \u043f\u0430\u0440\u0442\u0438\u0438 \u043d\u0435 \u0437\u0430\u0432\u0435\u0440\u0448\u0451\u043d.";
            lblSub.Font = new Font("Arial", 11);
            lblSub.ForeColor = Color.FromArgb(102, 102, 102);
            lblSub.Location = new Point(20, 45);
            lblSub.AutoSize = true;
            this.Controls.Add(lblSub);

            // Option 1: End and close
            _btnEndAndClose = new Button();
            _btnEndAndClose.Text = "\u0417\u0430\u0432\u0435\u0440\u0448\u0438\u0442\u044c \u043f\u0430\u0440\u0442\u0438\u044e \u0438 \u0437\u0430\u043a\u0440\u044b\u0442\u044c";
            _btnEndAndClose.Font = new Font("Arial", 10);
            _btnEndAndClose.BackColor = Color.FromArgb(229, 57, 53); // #E53935
            _btnEndAndClose.ForeColor = Color.White;
            _btnEndAndClose.FlatStyle = FlatStyle.Flat;
            _btnEndAndClose.Size = new Size(240, 30);
            _btnEndAndClose.Location = new Point(20, 85);
            _btnEndAndClose.Click += new EventHandler(OnEndAndCloseClick);
            this.Controls.Add(_btnEndAndClose);

            _chkSave1 = new CheckBox();
            _chkSave1.Text = "\u0421\u043e\u0445\u0440\u0430\u043d\u0438\u0442\u044c \u043f\u043e\u0437\u0438\u0446\u0438\u044e";
            _chkSave1.Font = new Font("Arial", 10);
            _chkSave1.Checked = true;
            _chkSave1.Location = new Point(270, 88);
            _chkSave1.AutoSize = true;
            this.Controls.Add(_chkSave1);

            // Option 2: Close without ending
            _btnCloseKeep = new Button();
            _btnCloseKeep.Text = "\u0417\u0430\u043a\u0440\u044b\u0442\u044c \u2014 \u043d\u0435 \u0437\u0430\u0432\u0435\u0440\u0448\u0430\u044f \u043f\u0430\u0440\u0442\u0438\u044e";
            _btnCloseKeep.Font = new Font("Arial", 10);
            _btnCloseKeep.BackColor = Color.FromArgb(255, 152, 0); // #FF9800
            _btnCloseKeep.ForeColor = Color.White;
            _btnCloseKeep.FlatStyle = FlatStyle.Flat;
            _btnCloseKeep.Size = new Size(240, 30);
            _btnCloseKeep.Location = new Point(20, 130);
            _btnCloseKeep.Click += new EventHandler(OnCloseKeepClick);
            this.Controls.Add(_btnCloseKeep);

            _chkSave2 = new CheckBox();
            _chkSave2.Text = "\u0421\u043e\u0445\u0440\u0430\u043d\u0438\u0442\u044c \u043f\u043e\u0437\u0438\u0446\u0438\u044e";
            _chkSave2.Font = new Font("Arial", 10);
            _chkSave2.Checked = true;
            _chkSave2.Location = new Point(270, 133);
            _chkSave2.AutoSize = true;
            this.Controls.Add(_chkSave2);

            // Cancel button
            _btnCancel = new Button();
            _btnCancel.Text = "\u041e\u0442\u043c\u0435\u043d\u0430";
            _btnCancel.Font = new Font("Arial", 11);
            _btnCancel.Size = new Size(100, 35);
            _btnCancel.Location = new Point(360, 195);
            _btnCancel.Click += new EventHandler(OnCancelClick);
            this.Controls.Add(_btnCancel);

            this.CancelButton = _btnCancel;
        }

        private void OnEndAndCloseClick(object sender, EventArgs e)
        {
            _result = new CloseAppResult(CloseAppAction.EndAndClose, _chkSave1.Checked);
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void OnCloseKeepClick(object sender, EventArgs e)
        {
            _result = new CloseAppResult(CloseAppAction.CloseKeepSession, _chkSave2.Checked);
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void OnCancelClick(object sender, EventArgs e)
        {
            _result = new CloseAppResult(CloseAppAction.Cancel, false);
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                OnCancelClick(sender, e);
            }
        }
    }

    // ================================================================
    // 5. SaveAsDialog
    // ================================================================

    /// <summary>
    /// Dialog for "Save position as" with custom filename.
    /// Returns filename or null on cancel.
    /// </summary>
    public class SaveAsDialog : Form
    {
        private TextBox _nameBox;
        private Button _btnSave;
        private Button _btnCancel;
        private string _resultFilename;

        /// <summary>
        /// The chosen filename, or null if cancelled.
        /// </summary>
        public string ResultFilename
        {
            get { return _resultFilename; }
        }

        /// <summary>
        /// Create a SaveAsDialog.
        /// </summary>
        /// <param name="defaultName">Default filename (e.g. indicator text).</param>
        public SaveAsDialog(string defaultName)
        {
            _resultFilename = null;
            InitializeComponent(defaultName);
        }

        private void InitializeComponent(string defaultName)
        {
            this.Text = "\u0421\u043e\u0445\u0440\u0430\u043d\u0438\u0442\u044c \u043f\u043e\u0437\u0438\u0446\u0438\u044e \u043a\u0430\u043a";
            this.ClientSize = new Size(450, 150);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(245, 245, 245);
            this.KeyPreview = true;
            this.KeyDown += new KeyEventHandler(OnKeyDown);

            // Label
            Label lblFile = new Label();
            lblFile.Text = "\u0418\u043c\u044f \u0444\u0430\u0439\u043b\u0430:";
            lblFile.Font = new Font("Arial", 11);
            lblFile.Location = new Point(20, 20);
            lblFile.AutoSize = true;
            this.Controls.Add(lblFile);

            // Filename text box
            _nameBox = new TextBox();
            _nameBox.Font = new Font("Arial", 11);
            _nameBox.Location = new Point(20, 48);
            _nameBox.Width = 410;
            _nameBox.Text = defaultName ?? "";
            _nameBox.SelectAll();
            this.Controls.Add(_nameBox);

            // Save button
            _btnSave = new Button();
            _btnSave.Text = "\u0421\u043e\u0445\u0440\u0430\u043d\u0438\u0442\u044c";
            _btnSave.Font = new Font("Arial", 11);
            _btnSave.BackColor = Color.FromArgb(76, 175, 80);
            _btnSave.ForeColor = Color.White;
            _btnSave.FlatStyle = FlatStyle.Flat;
            _btnSave.Size = new Size(110, 32);
            _btnSave.Location = new Point(320, 100);
            _btnSave.Click += new EventHandler(OnSaveClick);
            this.Controls.Add(_btnSave);

            // Cancel button
            _btnCancel = new Button();
            _btnCancel.Text = "\u041e\u0442\u043c\u0435\u043d\u0430";
            _btnCancel.Font = new Font("Arial", 11);
            _btnCancel.Size = new Size(100, 32);
            _btnCancel.Location = new Point(205, 100);
            _btnCancel.Click += new EventHandler(OnCancelClick);
            this.Controls.Add(_btnCancel);

            this.AcceptButton = _btnSave;
            this.CancelButton = _btnCancel;
        }

        private void OnSaveClick(object sender, EventArgs e)
        {
            string name = _nameBox.Text.Trim();
            if (string.IsNullOrEmpty(name))
                return;
            if (!name.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                name += ".png";
            _resultFilename = name;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void OnCancelClick(object sender, EventArgs e)
        {
            _resultFilename = null;
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                OnCancelClick(sender, e);
            }
        }
    }
}
