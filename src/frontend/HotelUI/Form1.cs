using System.Data;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Reflection;

namespace HotelUI;

internal static class Theme
{
    public static readonly Color Sidebar = Color.FromArgb(24, 33, 56);
    public static readonly Color SidebarHover = Color.FromArgb(44, 58, 94);
    public static readonly Color Accent = Color.FromArgb(46, 196, 182);
    public static readonly Color Bg = Color.FromArgb(243, 245, 250);
    public static readonly Color Card = Color.White;
    public static readonly Color Text = Color.FromArgb(28, 38, 64);
    public static readonly Color Muted = Color.FromArgb(125, 135, 156);
    public static readonly Color Green = Color.FromArgb(39, 174, 96);
    public static readonly Color Red = Color.FromArgb(214, 69, 65);
    public static readonly Color Orange = Color.FromArgb(230, 126, 34);
    public static readonly Color Border = Color.FromArgb(228, 232, 240);

    public static Color Lerp(Color a, Color b, float t) => Color.FromArgb(
        (int)(a.R + (b.R - a.R) * t),
        (int)(a.G + (b.G - a.G) * t),
        (int)(a.B + (b.B - a.B) * t));

    public static GraphicsPath RoundRect(Rectangle r, int radius)
    {
        int d = radius * 2;
        var p = new GraphicsPath();
        p.AddArc(r.X, r.Y, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }

    public static float EaseOut(float t) => 1f - (float)Math.Pow(1 - t, 3);
}

internal sealed class NavButton : Control
{
    private static readonly Font ItemFont = new("Segoe UI", 11);
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 15 };
    private readonly bool _accentText;
    private float _level;
    private bool _hover;
    private bool _active;

    public NavButton(string text, bool accentText = false)
    {
        Text = text;
        _accentText = accentText;
        Dock = DockStyle.Top;
        Height = 52;
        Cursor = Cursors.Hand;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        _timer.Tick += (s, e) => Step();
    }

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public bool Active
    {
        get => _active;
        set { _active = value; _timer.Start(); }
    }

    private float Target => _active ? 1f : _hover ? 0.55f : 0f;

    private void Step()
    {
        float target = Target;
        _level += (target - _level) * 0.25f;
        if (Math.Abs(target - _level) < 0.01f)
        {
            _level = target;
            _timer.Stop();
        }
        Invalidate();
    }

    protected override void OnMouseEnter(EventArgs e) { _hover = true; _timer.Start(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hover = false; _timer.Start(); base.OnMouseLeave(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Theme.Sidebar);

        var rect = new Rectangle(14, 4, Width - 28, Height - 8);
        using (var path = Theme.RoundRect(rect, 10))
        using (var brush = new SolidBrush(Theme.Lerp(Theme.Sidebar, Theme.SidebarHover, _level)))
            g.FillPath(brush, path);

        if (_active)
        {
            using var bar = new SolidBrush(Theme.Accent);
            g.FillRectangle(bar, 0, 12, 4, Height - 24);
        }

        var color = _accentText
            ? Theme.Accent
            : Theme.Lerp(Color.FromArgb(170, 180, 205), Color.White, _level);
        TextRenderer.DrawText(g, Text, ItemFont, new Rectangle(34, 0, Width - 40, Height), color,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _timer.Dispose();
        base.Dispose(disposing);
    }
}

internal sealed class RoundedPanel : Panel
{
    public RoundedPanel()
    {
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = Theme.Bg;
        Padding = new Padding(14);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = Theme.RoundRect(new Rectangle(0, 0, Width - 1, Height - 1), 14);
        using var brush = new SolidBrush(Theme.Card);
        using var pen = new Pen(Theme.Border);
        g.FillPath(brush, path);
        g.DrawPath(pen, path);
    }
}

internal sealed class StatCard : Control
{
    private static readonly Font ValueFont = new("Segoe UI", 24, FontStyle.Bold);
    private static readonly Font CaptionFont = new("Segoe UI", 10);
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 15 };
    private string _caption = "";
    private Color _accent = Theme.Accent;
    private int _target;
    private float _shown;
    private DateTime _start;

    public StatCard()
    {
        Size = new Size(230, 96);
        Margin = new Padding(0, 0, 18, 0);
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        _timer.Tick += (s, e) =>
        {
            float t = Math.Min(1f, (float)(DateTime.Now - _start).TotalMilliseconds / 700f);
            _shown = _target * Theme.EaseOut(t);
            if (t >= 1f) _timer.Stop();
            Invalidate();
        };
    }

    public void Set(string caption, int value, Color accent)
    {
        _caption = caption;
        _accent = accent;
        _target = value;
        _shown = 0;
        _start = DateTime.Now;
        _timer.Start();
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Theme.Bg);

        using var path = Theme.RoundRect(new Rectangle(0, 0, Width - 1, Height - 1), 14);
        using (var brush = new SolidBrush(Theme.Card)) g.FillPath(brush, path);

        g.SetClip(path);
        using (var bar = new SolidBrush(_accent)) g.FillRectangle(bar, 0, 0, 6, Height);
        g.ResetClip();

        using (var pen = new Pen(Theme.Border)) g.DrawPath(pen, path);

        var flags = TextFormatFlags.Left | TextFormatFlags.VerticalCenter;
        TextRenderer.DrawText(g, ((int)Math.Round(_shown)).ToString(), ValueFont,
            new Rectangle(22, 10, Width - 30, 50), _accent, flags);
        TextRenderer.DrawText(g, _caption, CaptionFont,
            new Rectangle(22, 58, Width - 30, 26), Theme.Muted, flags);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _timer.Dispose();
        base.Dispose(disposing);
    }
}

public partial class Form1 : Form
{
    private static readonly Font BadgeFont = new("Segoe UI", 9, FontStyle.Bold);

    private readonly Label _title = new()
    {
        Dock = DockStyle.Top,
        Height = 42,
        ForeColor = Theme.Text,
        Font = new Font("Segoe UI", 22, FontStyle.Bold)
    };
    private readonly Label _subtitle = new()
    {
        Dock = DockStyle.Top,
        Height = 26,
        ForeColor = Theme.Muted,
        Font = new Font("Segoe UI", 10.5f)
    };
    private readonly TextBox _search = new()
    {
        PlaceholderText = "Поиск...",
        Width = 240,
        BorderStyle = BorderStyle.FixedSingle,
        Font = new Font("Segoe UI", 10.5f)
    };
    private readonly DataGridView _grid = new();
    private readonly Panel _holder = new() { Dock = DockStyle.Fill, BackColor = Theme.Bg };
    private readonly StatCard[] _cards = { new StatCard(), new StatCard(), new StatCard() };
    private readonly List<NavButton> _nav = new();
    private readonly System.Windows.Forms.Timer _slideTimer = new() { Interval = 15 };

    private DateTime _slideStart;
    private DataTable _table = new();
    private string[] _columns = Array.Empty<string>();

    public Form1()
    {
        InitializeComponent();
        Text = "Автоматизация администрирования отеля";
        Font = new Font("Segoe UI", 10);
        Width = 1150;
        Height = 700;
        MinimumSize = new Size(950, 600);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Theme.Bg;
        DoubleBuffered = true;
        Opacity = 0;

        Controls.Add(BuildContent());
        Controls.Add(BuildSidebar());

        _search.TextChanged += (s, e) => ApplyFilter();
        _slideTimer.Tick += (s, e) => SlideStep();
        Shown += (s, e) => FadeIn();

        _nav[0].Active = true;
        ShowRooms();
    }

    private void FadeIn()
    {
        var timer = new System.Windows.Forms.Timer { Interval = 15 };
        timer.Tick += (s, e) =>
        {
            Opacity += 0.08;
            if (Opacity >= 1)
            {
                Opacity = 1;
                timer.Stop();
                timer.Dispose();
            }
        };
        timer.Start();
    }

    private Control BuildSidebar()
    {
        var panel = new Panel { Dock = DockStyle.Left, Width = 240, BackColor = Theme.Sidebar };

        var login = new NavButton("Вход администратора", accentText: true) { Dock = DockStyle.Bottom };
        login.Click += (s, e) => MessageBox.Show("Здесь будет окно входа администратора");
        panel.Controls.Add(login);

        panel.Controls.Add(new Label
        {
            Text = "v0.1 · черновик дизайна",
            Dock = DockStyle.Bottom,
            Height = 36,
            ForeColor = Theme.Muted,
            TextAlign = ContentAlignment.MiddleCenter
        });

        var items = new (string Name, Action Open)[]
        {
            ("Номера", ShowRooms),
            ("Бронирования", ShowBookings),
            ("Гости", ShowGuests),
            ("Услуги", ShowServices),
        };
        for (int i = items.Length - 1; i >= 0; i--)
        {
            var item = items[i];
            var button = new NavButton(item.Name);
            button.Click += (s, e) =>
            {
                foreach (var n in _nav) n.Active = n == button;
                item.Open();
            };
            _nav.Insert(0, button);
            panel.Controls.Add(button);
        }

        panel.Controls.Add(new Label
        {
            Text = "HOTEL ADMIN",
            Dock = DockStyle.Top,
            Height = 100,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 17, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter
        });

        return panel;
    }

    private Control BuildContent()
    {
        var content = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(28, 22, 28, 24),
            BackColor = Theme.Bg
        };

        StyleGrid();
        var card = new RoundedPanel { Dock = DockStyle.Fill };
        card.Controls.Add(_grid);
        _holder.Controls.Add(card);

        var cards = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 112,
            BackColor = Theme.Bg,
            Padding = new Padding(0, 8, 0, 8)
        };
        cards.Controls.AddRange(_cards);

        var header = new Panel { Dock = DockStyle.Top, Height = 78, BackColor = Theme.Bg };
        header.Controls.Add(_subtitle);
        header.Controls.Add(_title);
        header.Controls.Add(_search);
        _search.Location = new Point(header.Width - _search.Width, 6);
        header.Resize += (s, e) => _search.Location = new Point(header.Width - _search.Width, 6);
        _search.BringToFront();

        content.Controls.Add(_holder);
        content.Controls.Add(cards);
        content.Controls.Add(header);
        return content;
    }

    private void StyleGrid()
    {
        typeof(DataGridView)
            .GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic)?
            .SetValue(_grid, true);

        _grid.Dock = DockStyle.Fill;
        _grid.BackgroundColor = Color.White;
        _grid.BorderStyle = BorderStyle.None;
        _grid.ReadOnly = true;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToResizeRows = false;
        _grid.RowHeadersVisible = false;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        _grid.GridColor = Color.FromArgb(234, 237, 243);
        _grid.EnableHeadersVisualStyles = false;
        _grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        _grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        _grid.ColumnHeadersHeight = 44;
        _grid.ColumnHeadersDefaultCellStyle.BackColor = Color.White;
        _grid.ColumnHeadersDefaultCellStyle.ForeColor = Theme.Muted;
        _grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.White;
        _grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        _grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
        _grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(8, 0, 0, 0);
        _grid.DefaultCellStyle.ForeColor = Theme.Text;
        _grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(225, 247, 244);
        _grid.DefaultCellStyle.SelectionForeColor = Theme.Text;
        _grid.DefaultCellStyle.Padding = new Padding(8, 0, 0, 0);
        _grid.RowTemplate.Height = 46;

        _grid.CellPainting += (s, e) =>
        {
            if (e.RowIndex < 0 || e.Value is not string text ||
                _grid.Columns[e.ColumnIndex].HeaderText != "Статус") return;

            e.Paint(e.CellBounds, DataGridViewPaintParts.All & ~DataGridViewPaintParts.ContentForeground);

            var (fore, back) = StatusColors(text);
            var size = TextRenderer.MeasureText(text, BadgeFont);
            var rect = new Rectangle(e.CellBounds.X + 10,
                e.CellBounds.Y + (e.CellBounds.Height - 26) / 2, size.Width + 22, 26);

            e.Graphics!.SmoothingMode = SmoothingMode.AntiAlias;
            using var path = Theme.RoundRect(rect, 13);
            using var brush = new SolidBrush(back);
            e.Graphics.FillPath(brush, path);
            TextRenderer.DrawText(e.Graphics, text, BadgeFont, rect, fore,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            e.Handled = true;
        };
    }

    private static (Color Fore, Color Back) StatusColors(string status) => status switch
    {
        "Свободен" or "Заселён" or "Завершена" => (Theme.Green, Color.FromArgb(226, 246, 235)),
        "Занят" or "Отменена" => (Theme.Red, Color.FromArgb(251, 232, 231)),
        _ => (Theme.Orange, Color.FromArgb(253, 240, 224))
    };

    private void ApplyFilter()
    {
        try
        {
            var q = _search.Text.Trim();
            if (q.Length == 0)
            {
                _table.DefaultView.RowFilter = "";
                return;
            }
            q = q.Replace("'", "''").Replace("[", "[[]").Replace("%", "[%]").Replace("*", "[*]");
            _table.DefaultView.RowFilter =
                string.Join(" OR ", _columns.Select(c => $"[{c}] LIKE '%{q}%'"));
        }
        catch (Exception)
        {
            _table.DefaultView.RowFilter = "";
        }
    }

    private void SlideIn()
    {
        _slideStart = DateTime.Now;
        _holder.Padding = new Padding(0, 36, 0, 0);
        _slideTimer.Start();
    }

    private void SlideStep()
    {
        float t = Math.Min(1f, (float)(DateTime.Now - _slideStart).TotalMilliseconds / 320f);
        _holder.Padding = new Padding(0, (int)(36 * (1 - Theme.EaseOut(t))), 0, 0);
        if (t >= 1f) _slideTimer.Stop();
    }

    private void ShowPage(string title, string subtitle, string[] columns, object[][] rows,
        params (string Caption, int Value, Color Accent)[] stats)
    {
        _title.Text = title;
        _subtitle.Text = subtitle;
        _search.Clear();

        _columns = columns;
        _table = new DataTable();
        foreach (var c in columns) _table.Columns.Add(c);
        foreach (var r in rows) _table.Rows.Add(r);

        _grid.DataSource = _table.DefaultView;
        _grid.ClearSelection();

        for (int i = 0; i < _cards.Length; i++)
        {
            _cards[i].Visible = i < stats.Length;
            if (i < stats.Length) _cards[i].Set(stats[i].Caption, stats[i].Value, stats[i].Accent);
        }

        SlideIn();
    }

    private static int Count(object[][] rows, string status) =>
        rows.Count(r => r[r.Length - 1].ToString() == status);

    private void ShowRooms()
    {
        var rows = new[]
        {
            new object[] { "101", "Стандарт", "3 500 ₽", "Свободен" },
            new object[] { "102", "Стандарт", "3 500 ₽", "Занят" },
            new object[] { "120", "Люкс", "7 800 ₽", "Свободен" },
            new object[] { "201", "Люкс", "7 800 ₽", "Свободен" },
            new object[] { "202", "Комфорт", "5 200 ₽", "Занят" },
        };
        ShowPage("Номера", "Состояние номерного фонда",
            new[] { "Номер", "Категория", "Цена за ночь", "Статус" }, rows,
            ("Всего номеров", rows.Length, Theme.Accent),
            ("Свободно", Count(rows, "Свободен"), Theme.Green),
            ("Занято", Count(rows, "Занят"), Theme.Red));
    }

    private void ShowBookings()
    {
        var rows = new[]
        {
            new object[] { "1", "Петров И. С.", "102", "01.10.2026", "04.10.2026", "Заселён" },
            new object[] { "2", "Смирнова А. В.", "201", "05.10.2026", "08.10.2026", "Новая" },
            new object[] { "3", "Кузнецов Д. А.", "101", "20.09.2026", "22.09.2026", "Завершена" },
            new object[] { "4", "Иванова М. П.", "120", "10.10.2026", "12.10.2026", "Новая" },
        };
        ShowPage("Бронирования", "Текущие и будущие заселения",
            new[] { "№", "Гость", "Номер", "Заезд", "Выезд", "Статус" }, rows,
            ("Всего броней", rows.Length, Theme.Accent),
            ("Заселены", Count(rows, "Заселён"), Theme.Green),
            ("Новые", Count(rows, "Новая"), Theme.Orange));
    }

    private void ShowGuests()
    {
        var rows = new[]
        {
            new object[] { "Петров Иван Сергеевич", "4510 123456", "+7 900 111-22-33" },
            new object[] { "Смирнова Анна Викторовна", "4511 654321", "+7 900 444-55-66" },
            new object[] { "Кузнецов Дмитрий Алексеевич", "4512 111222", "+7 900 777-88-99" },
        };
        ShowPage("Гости", "База постояльцев отеля",
            new[] { "ФИО", "Паспорт", "Телефон" }, rows,
            ("Всего гостей", rows.Length, Theme.Accent));
    }

    private void ShowServices()
    {
        var rows = new[]
        {
            new object[] { "Завтрак", "500 ₽", "за человека" },
            new object[] { "Трансфер", "1 200 ₽", "за поездку" },
            new object[] { "Спа-процедуры", "2 500 ₽", "за сеанс" },
        };
        ShowPage("Услуги", "Дополнительные услуги отеля",
            new[] { "Услуга", "Цена", "Единица" }, rows,
            ("Всего услуг", rows.Length, Theme.Accent));
    }
}