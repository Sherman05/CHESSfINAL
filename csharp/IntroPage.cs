using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace ChessT1
{
    /// <summary>
    /// Intro/About page form matching the PDF mockup
    /// "\u0412\u0432\u043e\u0434\u043d\u0430\u044f \u0441\u0442\u0440\u0430\u043d\u0438\u0446\u0430 \u0413\u0418 chess-\u04221 2026".
    /// Shows on every launch unless "skip and don't show" was saved.
    /// Accessible via Menu -> "\u041e \u043f\u0440\u043e\u0433\u0440\u0430\u043c\u043c\u0435".
    /// </summary>
    public class IntroPage : Form
    {
        // Colors matching the PDF mockup
        private static readonly Color ColorFrameBg = Color.FromArgb(74, 144, 200);   // #4A90C8
        private static readonly Color ColorTextBg = Color.FromArgb(212, 201, 168);    // #D4C9A8
        private static readonly Color ColorTextFg = Color.FromArgb(26, 26, 26);       // #1A1A1A
        private static readonly Color ColorBtnMain = Color.FromArgb(93, 173, 226);    // #5DADE2
        private static readonly Color ColorBtnSkip = Color.FromArgb(127, 179, 216);   // #7FB3D8
        private static readonly Color ColorBtnCtrl = Color.FromArgb(91, 155, 213);    // #5B9BD5
        private static readonly Color ColorBtnClose = Color.FromArgb(192, 57, 43);    // #C0392B

        private RichTextBox _textBox;
        private bool _skipForever;

        /// <summary>
        /// Callback invoked when "don't show again" is chosen.
        /// Host should persist this preference.
        /// </summary>
        public event EventHandler SkipForeverRequested;

        /// <summary>
        /// True if user chose "don't show again".
        /// </summary>
        public bool SkipForever
        {
            get { return _skipForever; }
        }

        /// <summary>
        /// Create the intro page. Size matches parentSize if provided.
        /// </summary>
        public IntroPage(Size parentSize, Point parentLocation)
        {
            _skipForever = false;
            InitializeComponent(parentSize, parentLocation);
        }

        /// <summary>
        /// Create the intro page with default 800x600 size.
        /// </summary>
        public IntroPage() : this(new Size(800, 600), Point.Empty)
        {
        }

        private void InitializeComponent(Size parentSize, Point parentLocation)
        {
            this.Text = "chess-T1 \u2014 \u041e \u043f\u0440\u043e\u0433\u0440\u0430\u043c\u043c\u0435";
            this.BackColor = ColorFrameBg;
            this.StartPosition = FormStartPosition.CenterParent;

            if (parentSize.Width > 0 && parentSize.Height > 0)
            {
                this.ClientSize = parentSize;
                if (parentLocation != Point.Empty)
                {
                    this.StartPosition = FormStartPosition.Manual;
                    this.Location = parentLocation;
                }
            }
            else
            {
                this.ClientSize = new Size(800, 600);
            }

            this.MinimumSize = new Size(500, 400);
            this.KeyPreview = true;
            this.KeyDown += new KeyEventHandler(OnKeyDown);

            // Main layout panel
            Panel outer = new Panel();
            outer.Dock = DockStyle.Fill;
            outer.BackColor = ColorFrameBg;
            outer.Padding = new Padding(8);
            this.Controls.Add(outer);

            // We use a TableLayoutPanel for vertical stacking with proper resizing
            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.BackColor = ColorFrameBg;
            layout.ColumnCount = 1;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowCount = 4;
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));  // top bar
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));  // skip bar
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));  // text area
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));  // grip
            outer.Controls.Add(layout);

            // ---- Row 0: Top bar ----
            Panel topBar = new Panel();
            topBar.Dock = DockStyle.Fill;
            topBar.BackColor = ColorFrameBg;
            layout.Controls.Add(topBar, 0, 0);

            // Program symbol (chess king)
            Label lblSymbol = new Label();
            lblSymbol.Text = "\u265a";
            lblSymbol.Font = new Font("Arial", 26);
            lblSymbol.ForeColor = Color.FromArgb(26, 35, 126); // #1A237E
            lblSymbol.BackColor = ColorFrameBg;
            lblSymbol.AutoSize = true;
            lblSymbol.Location = new Point(5, 4);
            topBar.Controls.Add(lblSymbol);

            // "\u041e\u0441\u043d\u043e\u0432\u043d\u043e\u0439 \u0440\u0435\u0436\u0438\u043c" button
            Button btnMain = new Button();
            btnMain.Text = "\u041e\u0441\u043d\u043e\u0432\u043d\u043e\u0439 \u0440\u0435\u0436\u0438\u043c";
            btnMain.Font = new Font("Arial", 10, FontStyle.Bold);
            btnMain.BackColor = ColorBtnMain;
            btnMain.ForeColor = Color.White;
            btnMain.FlatStyle = FlatStyle.Flat;
            btnMain.FlatAppearance.BorderSize = 0;
            btnMain.Size = new Size(150, 32);
            btnMain.Location = new Point(55, 10);
            btnMain.Cursor = Cursors.Hand;
            btnMain.Click += new EventHandler(OnGoToMain);
            topBar.Controls.Add(btnMain);

            // Control buttons (right side): Minimize, OnTop, Close
            int ctrlX = topBar.Width; // will be repositioned on resize
            Button btnMinimize = CreateCtrlButton("\u2014", ColorBtnCtrl);
            btnMinimize.Click += delegate { this.WindowState = FormWindowState.Minimized; };
            topBar.Controls.Add(btnMinimize);

            Button btnOnTop = CreateCtrlButton("\u25a0", ColorBtnCtrl);
            btnOnTop.Click += delegate
            {
                this.TopMost = !this.TopMost;
                btnOnTop.FlatAppearance.BorderColor = this.TopMost ? Color.Yellow : ColorBtnCtrl;
            };
            topBar.Controls.Add(btnOnTop);

            Button btnClose = CreateCtrlButton("\u2715", ColorBtnClose);
            btnClose.Click += new EventHandler(OnCloseClick);
            topBar.Controls.Add(btnClose);

            // Reposition control buttons on resize
            topBar.Resize += delegate
            {
                int rx = topBar.Width;
                btnClose.Location = new Point(rx - 38, 8);
                btnOnTop.Location = new Point(rx - 76, 8);
                btnMinimize.Location = new Point(rx - 114, 8);
            };

            // ---- Row 1: Skip bar ----
            Panel skipBar = new Panel();
            skipBar.Dock = DockStyle.Fill;
            skipBar.BackColor = ColorFrameBg;
            layout.Controls.Add(skipBar, 0, 1);

            Button btnSkip = new Button();
            btnSkip.Text = "\u041f\u0440\u043e\u043f\u0443\u0441\u0442\u0438\u0442\u044c \u0432\u0432\u043e\u0434\u043d\u043e\u0439 \u0442\u0435\u043a\u0441\u0442";
            btnSkip.Font = new Font("Arial", 8);
            btnSkip.BackColor = ColorBtnSkip;
            btnSkip.ForeColor = Color.FromArgb(51, 51, 51);
            btnSkip.FlatStyle = FlatStyle.Flat;
            btnSkip.FlatAppearance.BorderSize = 1;
            btnSkip.Size = new Size(185, 28);
            btnSkip.Location = new Point(5, 4);
            btnSkip.Cursor = Cursors.Hand;
            btnSkip.Click += new EventHandler(OnSkip);
            skipBar.Controls.Add(btnSkip);

            Button btnSkipForever = new Button();
            btnSkipForever.Text = "\u041f\u0440\u043e\u043f\u0443\u0441\u0442\u0438\u0442\u044c \u0438 \u043d\u0435 \u043f\u043e\u043a\u0430\u0437\u044b\u0432\u0430\u0442\u044c";
            btnSkipForever.Font = new Font("Arial", 8);
            btnSkipForever.BackColor = ColorBtnSkip;
            btnSkipForever.ForeColor = Color.FromArgb(51, 51, 51);
            btnSkipForever.FlatStyle = FlatStyle.Flat;
            btnSkipForever.FlatAppearance.BorderSize = 1;
            btnSkipForever.Size = new Size(200, 28);
            btnSkipForever.Location = new Point(200, 4);
            btnSkipForever.Cursor = Cursors.Hand;
            btnSkipForever.Click += new EventHandler(OnSkipForever);
            skipBar.Controls.Add(btnSkipForever);

            // ---- Row 2: Text area (beige) ----
            Panel textPanel = new Panel();
            textPanel.Dock = DockStyle.Fill;
            textPanel.BackColor = ColorTextBg;
            textPanel.BorderStyle = BorderStyle.Fixed3D;
            layout.Controls.Add(textPanel, 0, 2);

            _textBox = new RichTextBox();
            _textBox.Dock = DockStyle.Fill;
            _textBox.BackColor = ColorTextBg;
            _textBox.ForeColor = ColorTextFg;
            _textBox.Font = new Font("Arial", 11);
            _textBox.ReadOnly = true;
            _textBox.BorderStyle = BorderStyle.None;
            _textBox.ScrollBars = RichTextBoxScrollBars.Vertical;
            _textBox.WordWrap = true;
            _textBox.Text = LoadIntroText();
            textPanel.Controls.Add(_textBox);

            // ---- Row 3: Resize grip ----
            Panel gripBar = new Panel();
            gripBar.Dock = DockStyle.Fill;
            gripBar.BackColor = ColorFrameBg;
            layout.Controls.Add(gripBar, 0, 3);

            Label grip = new Label();
            grip.Text = "\u21f2";
            grip.Font = new Font("Arial", 14, FontStyle.Bold);
            grip.ForeColor = Color.FromArgb(44, 95, 138);
            grip.BackColor = ColorFrameBg;
            grip.AutoSize = true;
            grip.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
            grip.Cursor = Cursors.SizeNWSE;
            gripBar.Controls.Add(grip);
            gripBar.Resize += delegate
            {
                grip.Location = new Point(gripBar.Width - grip.Width - 2, 0);
            };
        }

        private Button CreateCtrlButton(string text, Color bgColor)
        {
            Button btn = new Button();
            btn.Text = text;
            btn.Font = new Font("Arial", 11, FontStyle.Bold);
            btn.BackColor = bgColor;
            btn.ForeColor = Color.White;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.Size = new Size(34, 30);
            btn.Cursor = Cursors.Hand;
            return btn;
        }

        /// <summary>
        /// Loads intro text from assets\intro_text.txt if it exists,
        /// otherwise returns built-in INTRO_TEXT.
        /// </summary>
        private static string LoadIntroText()
        {
            // Try several locations for assets\intro_text.txt
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string[] candidates = new string[]
            {
                Path.Combine(baseDir, "assets", "intro_text.txt"),
                Path.Combine(baseDir, "..", "assets", "intro_text.txt"),
                Path.Combine(baseDir, "..", "..", "assets", "intro_text.txt")
            };

            foreach (string path in candidates)
            {
                if (File.Exists(path))
                {
                    try
                    {
                        return File.ReadAllText(path, System.Text.Encoding.UTF8);
                    }
                    catch
                    {
                        // fall through to built-in
                    }
                }
            }

            return INTRO_TEXT;
        }

        // ---- Built-in intro text (same content as Python version) ----
        private const string INTRO_TEXT =
@"Как пользоваться программой ГИ chess-T1
(вводный текст)

Настоящая программа ГИ chess-T1 (Графический интерфейс) пользователям игры chess-T1 предоставляет возможности с помощью компьютера:

(1) последовательно (пользователям) проводить анализ партий и позиций игры chess-T1.

(2) двум пользователям играть между собой в игру chess-T1.

Предполагается, что пользователи знают и соблюдают при использовании ГИ chess-T1 правила игры.

ОСНОВНЫЕ ЭЛЕМЕНТЫ ИНТЕРФЕЙСА

Игровое поле — доска 8×8 с буквенно-цифровой нотацией (вертикали a–h, горизонтали 1–8).

Клетки замков — выделены светло-серым цветом:
  • Замок белых: c1, d1, e1, f1
  • Замок чёрных: c8, d8, e8, f8

ФИГУРЫ

В игре 7 видов фигур для каждого цвета:
  • Король (Кр) — королевская фигура
  • Коннет (Кт) — королевская фигура
  • Принц (Пр) — королевская фигура, может превращаться
  • Риттер (Рт) — боевая фигура
  • Кнехт (Кн) — пехота, превращается в Вер Кнехта
  • Вер Кнехт (ВК) — ветеран, превращается далее
  • Разведчик (Рк) — особый ход: взятие с разменом

КАК ХОДИТЬ

Перетаскивание (Drag & Drop): нажмите и удерживайте левую кнопку мыши на фигуре, перетащите на целевую клетку и отпустите. Фигура автоматически встанет по центру клетки.

Подсветка:
  • При начале хода подсвечивается стартовая клетка
  • При перетаскивании подсвечивается клетка под курсором
  • После хода целевая клетка слабо подсвечена

РЕЖИМЫ ИГРЫ

1. Партия — режим игры двух игроков:
   • Нажмите кнопку «Партия»
   • Создайте папку для сохранения партии
   • Играйте, соблюдая очерёдность ходов

2. Анализ — режим анализа позиций:
   • Нажмите кнопку «Анализ»
   • Расставьте фигуры из касс (перетаскивание)
   • Выберите, кто ходит первым
   • Нажмите «Готово» для начала анализа

ПРЕВРАЩЕНИЯ ФИГУР

• Кнехт автоматически превращается в Вер Кнехта:
  — Белый Кнехт на 6-й горизонтали → белый ВК
  — Чёрный Кнехт на 3-й горизонтали → чёрный ВК

• Вер Кнехт превращается по выбору на крайних клетках
• Принц превращается по выбору на центральных клетках замка

СПЕЦИАЛЬНЫЙ ХОД РАЗВЕДЧИКА

Взятие с разменом: когда Разведчик ходит на клетку замка, занятую фигурой противника, обе фигуры исчезают с доски.

КНОПКИ УПРАВЛЕНИЯ

Верхний ряд:
  • Начальная расстановка — сбросить фигуры в начальную позицию
  • Партия — начать режим партии
  • Анализ — начать режим анализа
  • Свернуть / Поверх всех окон / Закрыть

Нижний ряд:
  • Меню — дополнительные команды
  • ◀ Предыдущий ход / Следующий ход ▶
  • Удалить фигуру
  • Реверс — перевернуть доску
  • Изменить размер

МЕНЮ

  • О программе — показать эту страницу
  • Сохранить позицию — скриншот текущей позиции
  • Сохранить позицию как — скриншот с выбором имени
  • Завершить партию — завершить текущий сеанс
  • Создать ярлык — ярлык на рабочем столе
  • Выход — закрыть программу

Приятной игры!";

        // ---- Event handlers ----

        private void OnGoToMain(object sender, EventArgs e)
        {
            CloseIntro();
        }

        private void OnSkip(object sender, EventArgs e)
        {
            CloseIntro();
        }

        private void OnSkipForever(object sender, EventArgs e)
        {
            _skipForever = true;
            if (SkipForeverRequested != null)
                SkipForeverRequested(this, EventArgs.Empty);
            CloseIntro();
        }

        private void OnCloseClick(object sender, EventArgs e)
        {
            CloseIntro();
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                CloseIntro();
            }
        }

        private void CloseIntro()
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
