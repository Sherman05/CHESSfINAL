using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace ChessT1
{
    /// <summary>
    /// Main application window for chess-T1.
    /// Contains top toolbar, board control, bottom toolbar, and side trays.
    /// </summary>
    public class MainForm : Form
    {
        // ---- Color constants ----
        private static readonly Color TopBarColor = Color.FromArgb(91, 163, 217);    // #5BA3D9
        private static readonly Color BottomBarColor = Color.FromArgb(200, 206, 212); // #C8CED4
        private static readonly Color FormBgColor = Color.FromArgb(232, 236, 240);

        private static readonly Color BtnBg = Color.FromArgb(192, 200, 208);
        private static readonly Color BtnActiveBg = Color.FromArgb(168, 180, 192);
        private static readonly Color BtnDisabledBg = Color.FromArgb(216, 221, 226);
        private static readonly Color BtnHighlightBg = Color.FromArgb(74, 144, 200);
        private static readonly Color BtnPartyBg = Color.FromArgb(46, 125, 50);
        private static readonly Color BtnAnalysisBg = Color.FromArgb(21, 101, 192);
        private static readonly Color BtnCloseBg = Color.FromArgb(198, 40, 40);
        private static readonly Color IndicatorColor = Color.FromArgb(26, 35, 126);

        // ---- UI elements ----
        private Panel _topPanel;
        private Panel _bottomPanel;
        private Panel _leftTrayPanel;
        private Panel _rightTrayPanel;
        private BoardControl _board;
        private ToolTip _toolTip;

        // Top toolbar buttons
        private Button _btnInitial;
        private Button _btnParty;
        private Button _btnAnalysis;
        private Button _btnMinimize;
        private Button _btnOnTop;
        private Button _btnClose;

        // Bottom toolbar buttons/controls
        private Button _btnMenu;
        private Label _indicatorLabel;
        private Button _btnPrev;
        private Button _btnNext;
        private Button _btnDelete;
        private Button _btnReverse;
        private Label _resizeGrip;

        // Analysis-specific bottom buttons (hidden by default)
        private Button _btnReset;
        private Button _btnFirstMove;
        private Button _btnOk;

        // ---- Application state ----
        private GameSession _session;

        private string _mode;        // null, "party", "analysis"
        private string _stage;       // "startup", "game", "setup_position"
        private bool _sessionActive;
        private bool _alwaysOnTop;
        private bool _boardReversed;
        private int _moveNumber;
        private bool _whiteTurn;
        private List<Dictionary<string, Piece>> _moveHistory;
        private int _historyIndex;
        private string _partyFolder;
        private bool _analysisFirstWhite;
        private bool _promotionPending;

        // ---- Constructor ----

        public MainForm()
        {
            InitializeFormProperties();
            InitializeState();
            BuildUI();
            WireEvents();

            // Enter startup state
            EnterStartup();
        }

        private void InitializeFormProperties()
        {
            this.Text = "chess-T1";
            this.MinimumSize = new Size(700, 500);
            this.WindowState = FormWindowState.Maximized;
            this.BackColor = FormBgColor;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.DoubleBuffered = true;
        }

        private void InitializeState()
        {
            _mode = null;
            _stage = "startup";
            _sessionActive = false;
            _alwaysOnTop = false;
            _boardReversed = false;
            _moveNumber = 1;
            _whiteTurn = true;
            _moveHistory = new List<Dictionary<string, Piece>>();
            _historyIndex = -1;
            _partyFolder = null;
            _analysisFirstWhite = true;
            _promotionPending = false;

            _toolTip = new ToolTip();
            _toolTip.AutoPopDelay = 5000;
            _toolTip.InitialDelay = 500;
            _toolTip.ReshowDelay = 200;
        }

        // ---- UI Construction ----

        private void BuildUI()
        {
            // Top panel (blue)
            _topPanel = new Panel();
            _topPanel.Dock = DockStyle.Top;
            _topPanel.Height = 40;
            _topPanel.BackColor = TopBarColor;
            BuildTopToolbar();
            this.Controls.Add(_topPanel);

            // Bottom panel (grey)
            _bottomPanel = new Panel();
            _bottomPanel.Dock = DockStyle.Bottom;
            _bottomPanel.Height = 40;
            _bottomPanel.BackColor = BottomBarColor;
            BuildBottomToolbar();
            this.Controls.Add(_bottomPanel);

            // Left tray panel (hidden by default)
            _leftTrayPanel = new Panel();
            _leftTrayPanel.Dock = DockStyle.Left;
            _leftTrayPanel.Width = 80;
            _leftTrayPanel.BackColor = FormBgColor;
            _leftTrayPanel.Visible = false;
            this.Controls.Add(_leftTrayPanel);

            // Right tray panel (hidden by default)
            _rightTrayPanel = new Panel();
            _rightTrayPanel.Dock = DockStyle.Right;
            _rightTrayPanel.Width = 80;
            _rightTrayPanel.BackColor = FormBgColor;
            _rightTrayPanel.Visible = false;
            this.Controls.Add(_rightTrayPanel);

            // Board control (fills center)
            _board = new BoardControl();
            _board.Dock = DockStyle.Fill;
            _board.BackColor = FormBgColor;
            this.Controls.Add(_board);
        }

        private Button CreateToolButton(string text, string tooltip, Color bgColor, EventHandler click)
        {
            Button btn = new Button();
            btn.Text = text;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = Color.FromArgb(100, 100, 100);
            btn.BackColor = bgColor;
            btn.ForeColor = Color.FromArgb(26, 26, 26);
            btn.Font = new Font("Arial", 9f);
            btn.Cursor = Cursors.Hand;
            btn.Margin = new Padding(2, 4, 2, 4);
            btn.Padding = new Padding(4, 2, 4, 2);
            btn.AutoSize = true;
            btn.MinimumSize = new Size(32, 28);

            if (click != null)
                btn.Click += click;

            if (!string.IsNullOrEmpty(tooltip))
                _toolTip.SetToolTip(btn, tooltip);

            return btn;
        }

        private void BuildTopToolbar()
        {
            FlowLayoutPanel flow = new FlowLayoutPanel();
            flow.Dock = DockStyle.Fill;
            flow.FlowDirection = FlowDirection.LeftToRight;
            flow.WrapContents = false;
            flow.BackColor = TopBarColor;
            flow.Padding = new Padding(2);

            _btnInitial = CreateToolButton("Начальная\nрасстановка",
                "Начальная расстановка", BtnBg, delegate { ResetToInitial(); });
            _btnInitial.Font = new Font("Arial", 8f);
            flow.Controls.Add(_btnInitial);

            _btnParty = CreateToolButton("Партия",
                "Режим Партия", BtnPartyBg, delegate { StartPartyMode(); });
            _btnParty.ForeColor = Color.White;
            flow.Controls.Add(_btnParty);

            _btnAnalysis = CreateToolButton("Анализ",
                "Режим Анализ", BtnAnalysisBg, delegate { StartAnalysisMode(); });
            _btnAnalysis.ForeColor = Color.White;
            flow.Controls.Add(_btnAnalysis);

            // Spacer
            Panel spacer = new Panel();
            spacer.Width = 10;
            spacer.AutoSize = false;
            // We use a hack: set the spacer to fill remaining space
            // FlowLayoutPanel doesn't have a true spacer, so we use Anchor trick later
            flow.Controls.Add(spacer);

            // Right-side buttons will be added to a separate right-aligned panel
            _topPanel.Controls.Add(flow);

            // Right-aligned buttons
            FlowLayoutPanel rightFlow = new FlowLayoutPanel();
            rightFlow.Dock = DockStyle.Right;
            rightFlow.FlowDirection = FlowDirection.RightToLeft;
            rightFlow.WrapContents = false;
            rightFlow.BackColor = TopBarColor;
            rightFlow.AutoSize = true;
            rightFlow.Padding = new Padding(2);

            _btnClose = CreateToolButton("\u2715",
                "Закрыть программу", BtnCloseBg, delegate { CloseApp(); });
            _btnClose.ForeColor = Color.White;
            rightFlow.Controls.Add(_btnClose);

            _btnOnTop = CreateToolButton("\u25A0",
                "Поверх всех окон", BtnBg, delegate { ToggleAlwaysOnTop(); });
            rightFlow.Controls.Add(_btnOnTop);

            _btnMinimize = CreateToolButton("\u2014",
                "Свернуть", BtnBg, delegate { MinimizeWindow(); });
            rightFlow.Controls.Add(_btnMinimize);

            _topPanel.Controls.Add(rightFlow);
        }

        private void BuildBottomToolbar()
        {
            FlowLayoutPanel flow = new FlowLayoutPanel();
            flow.Dock = DockStyle.Fill;
            flow.FlowDirection = FlowDirection.LeftToRight;
            flow.WrapContents = false;
            flow.BackColor = BottomBarColor;
            flow.Padding = new Padding(2);

            _btnMenu = CreateToolButton("\u2630",
                "Меню", BtnBg, delegate { ShowMenu(); });
            flow.Controls.Add(_btnMenu);

            _indicatorLabel = new Label();
            _indicatorLabel.Text = "";
            _indicatorLabel.Font = new Font("Arial", 11f, FontStyle.Bold);
            _indicatorLabel.ForeColor = IndicatorColor;
            _indicatorLabel.BackColor = BottomBarColor;
            _indicatorLabel.AutoSize = true;
            _indicatorLabel.Margin = new Padding(5, 8, 5, 4);
            flow.Controls.Add(_indicatorLabel);

            _btnPrev = CreateToolButton("\u25C0",
                "Предыдущий ход", BtnBg, delegate { PrevMove(); });
            flow.Controls.Add(_btnPrev);

            _btnNext = CreateToolButton("\u25B6",
                "Следующий ход", BtnBg, delegate { NextMove(); });
            flow.Controls.Add(_btnNext);

            _btnDelete = CreateToolButton("\u2716",
                "Удалить фигуру", BtnBg, delegate { DeletePieceMode(); });
            flow.Controls.Add(_btnDelete);

            _btnReverse = CreateToolButton("\u21C5",
                "Реверс (перевернуть доску)", BtnBg, delegate { ReverseBoard(); });
            flow.Controls.Add(_btnReverse);

            // Analysis-specific buttons (hidden by default)
            _btnReset = CreateToolButton("Сброс",
                "Очистить поле", BtnBg, delegate { AnalysisReset(); });
            _btnReset.Font = new Font("Arial", 8f);
            _btnReset.Visible = false;
            flow.Controls.Add(_btnReset);

            _btnFirstMove = CreateToolButton("1-й ход",
                "Выбор очерёдности первого хода", BtnBg,
                delegate { AnalysisToggleFirstMove(); });
            _btnFirstMove.Visible = false;
            flow.Controls.Add(_btnFirstMove);

            _btnOk = CreateToolButton("Ok",
                "Зафиксировать позицию", BtnHighlightBg,
                delegate { AnalysisConfirm(); });
            _btnOk.Font = new Font("Arial", 11f, FontStyle.Bold);
            _btnOk.ForeColor = Color.White;
            _btnOk.Visible = false;
            flow.Controls.Add(_btnOk);

            _bottomPanel.Controls.Add(flow);

            // Resize grip (right-aligned)
            _resizeGrip = new Label();
            _resizeGrip.Dock = DockStyle.Right;
            _resizeGrip.Text = "\u21F2";
            _resizeGrip.Font = new Font("Arial", 14f);
            _resizeGrip.ForeColor = Color.FromArgb(136, 136, 136);
            _resizeGrip.BackColor = BottomBarColor;
            _resizeGrip.AutoSize = true;
            _resizeGrip.Cursor = Cursors.SizeNWSE;
            _resizeGrip.Padding = new Padding(5, 4, 5, 4);
            _bottomPanel.Controls.Add(_resizeGrip);
        }

        // ---- Event wiring ----

        private void WireEvents()
        {
            _board.MoveMade += OnMoveMade;
            _board.PromotionNeeded += OnPromotionNeeded;
            _board.PieceExitedBoard += OnPieceExited;
            _board.RazvedchikExchange += OnRazvedchikExchange;

            this.FormClosing += delegate(object sender, FormClosingEventArgs e)
            {
                if (_sessionActive)
                {
                    e.Cancel = true;
                    CloseApp();
                }
            };
        }

        // ---- State management ----

        private void EnterStartup()
        {
            _mode = null;
            _stage = "startup";
            _sessionActive = false;
            _board.AppStage = "startup";
            _board.SetPosition(PieceData.GetInitialPosition());
            HideTrays();
            UpdateButtonStates();
            UpdateIndicator();
        }

        private void UpdateButtonStates()
        {
            // Initial button
            SetButtonEnabled(_btnInitial, _stage != "startup");

            // Party button
            if (_mode == "party")
            {
                SetButtonEnabled(_btnParty, false);
                _btnParty.BackColor = Color.FromArgb(102, 187, 106); // highlighted green
            }
            else if (_mode == "analysis" && _stage == "setup_position")
            {
                SetButtonEnabled(_btnParty, false);
                _btnParty.BackColor = BtnPartyBg;
            }
            else
            {
                SetButtonEnabled(_btnParty, true);
                _btnParty.BackColor = BtnPartyBg;
            }

            // Analysis button
            if (_mode == "analysis")
            {
                SetButtonEnabled(_btnAnalysis, false);
                _btnAnalysis.BackColor = Color.FromArgb(30, 136, 229); // highlighted blue
            }
            else
            {
                SetButtonEnabled(_btnAnalysis, true);
                _btnAnalysis.BackColor = BtnAnalysisBg;
            }

            // Prev/Next
            bool navEnabled = _stage == "game";
            SetButtonEnabled(_btnPrev, navEnabled);
            SetButtonEnabled(_btnNext, navEnabled);

            // Reverse
            SetButtonEnabled(_btnReverse, _stage != "startup");

            // Delete
            SetButtonEnabled(_btnDelete, _stage != "startup");

            // Analysis-specific buttons
            bool showAnalysis = _stage == "setup_position";
            _btnReset.Visible = showAnalysis;
            _btnFirstMove.Visible = showAnalysis;
            _btnOk.Visible = showAnalysis;

            // Indicator: only in game
            _indicatorLabel.Visible = _stage == "game";

            // Update first move button appearance
            if (showAnalysis)
                UpdateFirstMoveButton();
        }

        private void SetButtonEnabled(Button btn, bool enabled)
        {
            btn.Enabled = enabled;
            if (!enabled)
            {
                btn.BackColor = BtnDisabledBg;
                btn.Cursor = Cursors.Default;
            }
            else
            {
                btn.Cursor = Cursors.Hand;
                // Restore original colors handled by caller or defaults
            }
        }

        private void UpdateIndicator()
        {
            if (_stage != "game")
            {
                _indicatorLabel.Text = "";
                return;
            }

            _indicatorLabel.Text = GetIndicatorText(_moveNumber, _whiteTurn);
        }

        private static string GetIndicatorText(int moveNumber, bool whiteTurn)
        {
            if (whiteTurn)
                return string.Format("{0}. __ \u0445\u0431", moveNumber);
            else
                return string.Format("{0} \u2026 __ \u0445\u0447", moveNumber);
        }

        private void UpdateFirstMoveButton()
        {
            if (_analysisFirstWhite)
            {
                _btnFirstMove.Text = "\u25CF\u2588  \u2591";
                _btnFirstMove.BackColor = Color.FromArgb(221, 221, 221);
                _btnFirstMove.ForeColor = Color.FromArgb(34, 34, 34);
            }
            else
            {
                _btnFirstMove.Text = "\u2591  \u2588\u25CF";
                _btnFirstMove.BackColor = Color.FromArgb(68, 68, 68);
                _btnFirstMove.ForeColor = Color.White;
            }
        }

        // ---- Button handlers ----

        private void ResetToInitial()
        {
            if (_stage == "startup")
                return;

            // Analysis setup: place initial position, stay in setup
            if (_mode == "analysis" && _stage == "setup_position")
            {
                _board.SetPosition(PieceData.GetInitialPosition());
                return;
            }

            if (_sessionActive)
            {
                EndSessionNoDialog();
                return;
            }

            EnterStartup();
        }

        private void StartPartyMode()
        {
            if (_mode == "party")
                return;

            if (_mode == "analysis" && _stage == "game")
            {
                EndSessionNoDialog();
                return;
            }

            if (_mode == "analysis" && _stage == "setup_position")
                return; // Frozen

            // From startup: ask for folder via FolderBrowserDialog
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                fbd.Description = "Выберите папку для сохранения партии";
                string folder = null;
                if (fbd.ShowDialog(this) == DialogResult.OK)
                {
                    folder = fbd.SelectedPath;
                }
                // Game starts even if dialog cancelled (per spec 8.1)
                OnPartyFolderCreated(folder);
            }
        }

        private void OnPartyFolderCreated(string folderPath)
        {
            _partyFolder = folderPath;
            _mode = "party";
            _stage = "game";
            _sessionActive = true;
            _moveNumber = 1;
            _whiteTurn = true;

            Dictionary<string, Piece> initialPos = PieceData.GetInitialPosition();
            _moveHistory = new List<Dictionary<string, Piece>>();
            _moveHistory.Add(PieceData.CopyPosition(initialPos));
            _historyIndex = 0;

            _board.AppStage = "game";
            _board.CurrentTurnColor = PieceColor.White;
            _board.SetPosition(initialPos);
            HideTrays();
            UpdateButtonStates();
            UpdateIndicator();
        }

        private void StartAnalysisMode()
        {
            if (_mode == "analysis")
                return;

            if (_mode == "party")
            {
                EndSessionNoDialog();
                return;
            }

            // From startup: enter setup position stage
            _mode = "analysis";
            _stage = "setup_position";
            _sessionActive = false;
            _analysisFirstWhite = true;

            _board.AppStage = "setup_position";
            _board.ClearBoard();
            ShowTrays();
            UpdateButtonStates();
            UpdateFirstMoveButton();
        }

        private void ShowMenu()
        {
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Font = new Font("Arial", 11f);

            menu.Items.Add("О программе", null, delegate { /* Show about/intro */ });
            menu.Items.Add(new ToolStripSeparator());

            ToolStripMenuItem saveItem = new ToolStripMenuItem("Сохранить позицию");
            saveItem.Enabled = _stage == "game";
            saveItem.Click += delegate { SavePosition(); };
            menu.Items.Add(saveItem);

            ToolStripMenuItem saveAsItem = new ToolStripMenuItem("Сохранить позицию как...");
            saveAsItem.Enabled = _stage == "game";
            saveAsItem.Click += delegate { SavePositionAs(); };
            menu.Items.Add(saveAsItem);

            menu.Items.Add(new ToolStripSeparator());

            ToolStripMenuItem endItem = new ToolStripMenuItem("Завершить партию");
            endItem.Enabled = _sessionActive && _stage == "game";
            endItem.Click += delegate { EndSessionDialog(); };
            menu.Items.Add(endItem);

            menu.Items.Add(new ToolStripSeparator());

            menu.Items.Add("Создать ярлык", null, delegate { /* Create shortcut */ });
            menu.Items.Add(new ToolStripSeparator());

            menu.Items.Add("Выход", null, delegate { CloseApp(); });

            // Position: right edge at board's left notation column
            Point boardScreenPos = _board.PointToScreen(Point.Empty);
            int menuX = boardScreenPos.X - menu.Width;
            int menuY = _btnMenu.PointToScreen(Point.Empty).Y - menu.Height;
            if (menuY < 0)
                menuY = _btnMenu.PointToScreen(Point.Empty).Y + _btnMenu.Height;

            menu.Show(new Point(menuX, menuY));
        }

        private void PrevMove()
        {
            if (_stage != "game" || _historyIndex <= 0)
                return;

            _historyIndex--;
            _board.SetPosition(_moveHistory[_historyIndex]);
            RecalculateTurnFromHistory();
            UpdateIndicator();
        }

        private void NextMove()
        {
            if (_stage != "game" || _historyIndex >= _moveHistory.Count - 1)
                return;

            _historyIndex++;
            _board.SetPosition(_moveHistory[_historyIndex]);
            RecalculateTurnFromHistory();
            UpdateIndicator();
        }

        private void DeletePieceMode()
        {
            if (_stage == "startup")
                return;

            if (_board.SelectedForDeletion != null)
            {
                // Step 2: piece already selected, delete it
                _board.DeleteSelected();
            }
            else
            {
                // Step 1: enter deletion mode
                _board.EnterDeletionMode();
            }
        }

        private void ReverseBoard()
        {
            if (_stage == "startup")
                return;

            _boardReversed = !_boardReversed;
            _board.Reverse();

            if (_stage == "setup_position")
            {
                // Swap trays
                HideTrays();
                ShowTrays();
            }
        }

        private void MinimizeWindow()
        {
            if (_promotionPending)
                return;
            this.WindowState = FormWindowState.Minimized;
        }

        private void ToggleAlwaysOnTop()
        {
            _alwaysOnTop = !_alwaysOnTop;
            this.TopMost = _alwaysOnTop;

            // Visual feedback on button
            _btnOnTop.BackColor = _alwaysOnTop ? BtnHighlightBg : BtnBg;
            _btnOnTop.ForeColor = _alwaysOnTop ? Color.White : Color.FromArgb(26, 26, 26);
        }

        private void CloseApp()
        {
            if (_sessionActive)
            {
                DialogResult result = MessageBox.Show(
                    "Завершить текущую партию перед закрытием?",
                    "Закрытие программы",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Cancel)
                    return;

                if (result == DialogResult.Yes)
                {
                    // End session, then close
                    _sessionActive = false;
                }
                // DialogResult.No: close without ending (session can be restored)
            }

            // Allow FormClosing to proceed
            this.FormClosing -= null; // Remove the guard handler
            Application.Exit();
        }

        // ---- Analysis-specific handlers ----

        private void AnalysisReset()
        {
            if (_stage == "setup_position")
                _board.ClearBoard();
        }

        private void AnalysisToggleFirstMove()
        {
            _analysisFirstWhite = !_analysisFirstWhite;
            UpdateFirstMoveButton();
        }

        private void AnalysisConfirm()
        {
            if (_stage != "setup_position")
                return;

            Dictionary<string, Piece> position = _board.GetPosition();
            if (position.Count == 0)
            {
                MessageBox.Show(
                    "Расставьте фигуры перед началом анализа.",
                    "Пустое поле",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            // Ask for folder
            string folder = null;
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                fbd.Description = "Выберите папку для сохранения анализа";
                if (fbd.ShowDialog(this) == DialogResult.OK)
                    folder = fbd.SelectedPath;
                else
                    return; // Stay in setup if cancelled
            }

            OnAnalysisConfirmed(folder, position);
        }

        private void OnAnalysisConfirmed(string folderPath, Dictionary<string, Piece> position)
        {
            _partyFolder = folderPath;
            _stage = "game";
            _sessionActive = true;
            _whiteTurn = _analysisFirstWhite;
            _moveNumber = 1;

            _moveHistory = new List<Dictionary<string, Piece>>();
            _moveHistory.Add(PieceData.CopyPosition(position));
            _historyIndex = 0;

            _board.AppStage = "game";
            _board.CurrentTurnColor = _whiteTurn ? PieceColor.White : PieceColor.Black;
            _board.SetPosition(position);
            HideTrays();
            UpdateButtonStates();
            UpdateIndicator();
        }

        // ---- Tray management ----

        private void ShowTrays()
        {
            _leftTrayPanel.Visible = true;
            _rightTrayPanel.Visible = true;
            // Left tray: white pieces if not reversed, else black
            // Right tray: opposite
            // Tray population would be done by dedicated tray controls
        }

        private void HideTrays()
        {
            _leftTrayPanel.Visible = false;
            _rightTrayPanel.Visible = false;
        }

        // ---- Move handling ----

        private void OnMoveMade(string fromCell, string toCell)
        {
            if (_stage != "game")
                return;

            // Trim history on new move after rollback
            if (_historyIndex < _moveHistory.Count - 1)
            {
                _moveHistory.RemoveRange(_historyIndex + 1,
                    _moveHistory.Count - _historyIndex - 1);
            }

            _moveHistory.Add(_board.GetPosition());
            _historyIndex = _moveHistory.Count - 1;

            RecalculateTurnFromHistory();
            _board.CurrentTurnColor = _whiteTurn ? PieceColor.White : PieceColor.Black;
            UpdateIndicator();
        }

        private void OnPromotionNeeded(string cell, Piece piece, PieceType[] options)
        {
            _promotionPending = true;

            // Build a simple promotion dialog
            Form dlg = new Form();
            dlg.Text = "Выбор превращения";
            dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
            dlg.StartPosition = FormStartPosition.CenterParent;
            dlg.MaximizeBox = false;
            dlg.MinimizeBox = false;
            dlg.AutoSize = true;
            dlg.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            dlg.Padding = new Padding(10);

            FlowLayoutPanel panel = new FlowLayoutPanel();
            panel.AutoSize = true;
            panel.FlowDirection = FlowDirection.LeftToRight;
            panel.Padding = new Padding(5);

            PieceType chosenType = options[0];

            foreach (PieceType pt in options)
            {
                PieceType captured = pt; // capture for closure
                Button btn = new Button();
                btn.Text = PieceData.ShortNames[captured];
                btn.Font = new Font("Arial", 14f, FontStyle.Bold);
                btn.Width = 60;
                btn.Height = 60;
                btn.FlatStyle = FlatStyle.Flat;
                btn.Cursor = Cursors.Hand;
                btn.Click += delegate
                {
                    chosenType = captured;
                    dlg.DialogResult = DialogResult.OK;
                    dlg.Close();
                };
                panel.Controls.Add(btn);
            }

            dlg.Controls.Add(panel);
            dlg.ShowDialog(this);

            // Apply promotion
            Piece newPiece = new Piece(piece.Color, chosenType);
            _board.Position[cell] = newPiece;
            _board.LastMoveCell = cell;
            _board.Invalidate();

            _promotionPending = false;
            OnMoveMade(null, cell);
        }

        private void OnPieceExited()
        {
            if (_stage == "game")
                OnMoveMade(null, null);
        }

        private void OnRazvedchikExchange(string fromCell, string toCell, bool exchange)
        {
            // The move is already handled via MoveMade in BoardControl.ExecuteMove
        }

        // ---- Turn calculation ----

        private void RecalculateTurnFromHistory()
        {
            int idx = _historyIndex;
            if (_mode == "analysis" && !_analysisFirstWhite)
            {
                // Black starts
                _whiteTurn = (idx % 2 == 1);
                _moveNumber = ((idx + 1) / 2) + 1;
            }
            else
            {
                // White starts (standard)
                _whiteTurn = (idx % 2 == 0);
                _moveNumber = (idx / 2) + 1;
            }
        }

        // ---- Session management ----

        private void EndSessionDialog()
        {
            DialogResult result = MessageBox.Show(
                "Завершить текущую партию?",
                "Завершение партии",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
                EndSessionNoDialog();
        }

        private void EndSessionNoDialog()
        {
            _sessionActive = false;
            EnterStartup();
        }

        // ---- Save position ----

        private void SavePosition()
        {
            if (_stage != "game")
                return;

            // Generate filename from indicator text
            string filename = GetIndicatorText(_moveNumber, _whiteTurn) + ".png";
            // Sanitize
            foreach (char c in System.IO.Path.GetInvalidFileNameChars())
            {
                filename = filename.Replace(c.ToString(), "");
            }
            filename = filename.Replace("\u2026", "...");

            string dir = _partyFolder ?? Environment.GetFolderPath(
                Environment.SpecialFolder.MyPictures);

            string filepath = System.IO.Path.Combine(dir, filename);

            // Save board as bitmap
            try
            {
                using (Bitmap bmp = new Bitmap(_board.Width, _board.Height))
                {
                    _board.DrawToBitmap(bmp, new Rectangle(0, 0, _board.Width, _board.Height));
                    bmp.Save(filepath, System.Drawing.Imaging.ImageFormat.Png);
                }
                ShowTempMessage("Текущая позиция сохранена");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка сохранения: " + ex.Message,
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SavePositionAs()
        {
            if (_stage != "game")
                return;

            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "PNG Image|*.png";
                sfd.Title = "Сохранить позицию как...";
                sfd.InitialDirectory = _partyFolder ?? Environment.GetFolderPath(
                    Environment.SpecialFolder.MyPictures);

                if (sfd.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        using (Bitmap bmp = new Bitmap(_board.Width, _board.Height))
                        {
                            _board.DrawToBitmap(bmp, new Rectangle(0, 0, _board.Width, _board.Height));
                            bmp.Save(sfd.FileName, System.Drawing.Imaging.ImageFormat.Png);
                        }
                        ShowTempMessage("Текущая позиция сохранена");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Ошибка сохранения: " + ex.Message,
                            "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void ShowTempMessage(string text)
        {
            Label msg = new Label();
            msg.Text = text;
            msg.Font = new Font("Arial", 14f, FontStyle.Bold);
            msg.ForeColor = Color.White;
            msg.BackColor = Color.FromArgb(76, 175, 80);
            msg.AutoSize = true;
            msg.Padding = new Padding(20, 10, 20, 10);
            msg.TextAlign = ContentAlignment.MiddleCenter;

            msg.Left = (this.ClientSize.Width - msg.PreferredWidth) / 2;
            msg.Top = (this.ClientSize.Height - msg.PreferredHeight) / 2;
            msg.BringToFront();

            this.Controls.Add(msg);
            msg.BringToFront();

            Timer timer = new Timer();
            timer.Interval = 1500;
            timer.Tick += delegate
            {
                timer.Stop();
                timer.Dispose();
                this.Controls.Remove(msg);
                msg.Dispose();
            };
            timer.Start();
        }

    }
}
