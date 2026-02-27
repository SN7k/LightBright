using BrightnessController.Helpers;
using BrightnessController.Hotkeys;
using BrightnessController.Monitors;
using BrightnessController.Settings;
using BrightnessController;
using System.Drawing.Drawing2D;

namespace BrightnessController.UI;

/// <summary>Windows 11-style settings form.</summary>
public sealed class SettingsForm : Form
{
    public event Action<AppSettings>? SettingsSaved;

    private readonly MonitorManager  _monitorManager;
    private AppSettings              _draft;
    private readonly List<HotkeyRow> _hotkeyRows = new();

    // Palette — refreshed by ApplyTheme() at construction time
    static Color Bg       = Color.FromArgb(32,  32,  32);
    static Color Surface  = Color.FromArgb(44,  44,  46);
    static Color Surface2 = Color.FromArgb(58,  58,  62);
    static Color Border   = Color.FromArgb(68,  68,  74);
    static Color FgMain   = Color.FromArgb(230, 230, 235);
    static Color FgSub    = Color.FromArgb(155, 155, 165);
    static Color Accent   = Color.FromArgb(0,   103, 192);
    static Color AccentLt = Color.FromArgb(59,  130, 246);
    // Extra inner-widget theme colors
    static Color S_ToggleOff         = Color.FromArgb(90,  90,  98);
    static Color S_SpinnerHovBg      = Color.FromArgb(70,  70,  76);
    static Color S_SpinnerChevron    = Color.FromArgb(160, 160, 165);
    static Color S_SpinnerChevronHot = Color.FromArgb(220, 220, 225);
    static Color S_KeyBoxBg          = Color.FromArgb(52,  52,  58);
    static Color S_ClearBtnBg        = Color.FromArgb(58,  52,  56);
    static Color S_ClearBtnFg        = Color.FromArgb(248, 113, 113);
    static Color S_ClearBtnBorder    = Color.FromArgb(90,  68,  72);

    /// <summary>Read Windows theme and update all palette statics.</summary>
    public static void ApplyTheme()
    {
        bool light = BrightnessController.Helpers.ThemeHelper.IsLightTheme;
        // Read live Windows accent colour
        var winAccent      = BrightnessController.Helpers.ThemeHelper.AccentColor;
        var winAccentLight = BrightnessController.Helpers.ThemeHelper.AccentColorLight;
        var winAccentDark  = BrightnessController.Helpers.ThemeHelper.AccentColorDark;

        if (light)
        {
            Bg       = Color.FromArgb(243, 243, 243);
            Surface  = Color.FromArgb(255, 255, 255);
            Surface2 = Color.FromArgb(233, 233, 236);
            Border   = Color.FromArgb(210, 210, 215);
            FgMain   = Color.FromArgb(25,  25,  30);
            FgSub    = Color.FromArgb(100, 100, 110);
            Accent   = winAccentDark;   // darker for button bg on white
            AccentLt = winAccent;       // pure accent for spinner value / highlights
            S_ToggleOff         = Color.FromArgb(185, 185, 195);
            S_SpinnerHovBg      = Color.FromArgb(218, 218, 225);
            S_SpinnerChevron    = Color.FromArgb(90,  90,  100);
            S_SpinnerChevronHot = Color.FromArgb(30,  30,  40);
            S_KeyBoxBg          = Color.FromArgb(248, 248, 252);
            S_ClearBtnBg        = Color.FromArgb(255, 242, 244);
            S_ClearBtnFg        = Color.FromArgb(200, 50,  50);
            S_ClearBtnBorder    = Color.FromArgb(220, 185, 185);
        }
        else
        {
            Bg       = Color.FromArgb(32,  32,  32);
            Surface  = Color.FromArgb(44,  44,  46);
            Surface2 = Color.FromArgb(58,  58,  62);
            Border   = Color.FromArgb(68,  68,  74);
            FgMain   = Color.FromArgb(230, 230, 235);
            FgSub    = Color.FromArgb(155, 155, 165);
            Accent   = winAccent;       // pure accent for button bg on dark
            AccentLt = winAccentLight;  // lighter for spinner value / highlights
            S_ToggleOff         = Color.FromArgb(90,  90,  98);
            S_SpinnerHovBg      = Color.FromArgb(70,  70,  76);
            S_SpinnerChevron    = Color.FromArgb(160, 160, 165);
            S_SpinnerChevronHot = Color.FromArgb(220, 220, 225);
            S_KeyBoxBg          = Color.FromArgb(52,  52,  58);
            S_ClearBtnBg        = Color.FromArgb(58,  52,  56);
            S_ClearBtnFg        = Color.FromArgb(248, 113, 113);
            S_ClearBtnBorder    = Color.FromArgb(90,  68,  72);
        }
    }

    const int R = 8;

    public SettingsForm(MonitorManager monitorManager)
    {
        _monitorManager = monitorManager;
        _draft          = Clone(SettingsManager.Current);

        ApplyTheme();

        Text             = "LiteBright - Settings";
        FormBorderStyle  = FormBorderStyle.FixedDialog;
        MaximizeBox      = false;
        MinimizeBox      = false;
        StartPosition    = FormStartPosition.CenterScreen;
        BackColor        = Bg;
        ForeColor        = FgMain;
        Font             = new Font("Segoe UI", 9.5f);
        Width            = 540;
        AutoScroll       = true;
        ShowInTaskbar    = true;
        Icon             = TrayApplicationContext.LoadAppIcon();

        BuildUI();
    }

    private void BuildUI()
    {
        int y  = 16;
        int lp = 20;
        int cw = Width - 18;

        // SECTION: General
        y += AddSectionHeader("General", lp, y);

        var generalCard = new RoundedPanel(R)
        {
            Left = lp, Top = y, Width = cw - lp * 2 + 2, Height = 120,
            BackColor = Surface,
        };
        Controls.Add(generalCard);

        var toggleStartup = new ToggleSwitch(_draft.StartWithWindows, Accent, Surface);
        AddSettingRow(generalCard, "Start with Windows",
            "Launch LiteBright automatically when you sign in.",
            0, 0, generalCard.Width, 60, toggleStartup);
        toggleStartup.ValueChanged += v => _draft.StartWithWindows = v;

        generalCard.Controls.Add(new Panel
            { Left = 14, Top = 59, Width = generalCard.Width - 28, Height = 1, BackColor = Border });

        var toggleContrast = new ToggleSwitch(_draft.EnableContrastSlider, Accent, Surface);
        AddSettingRow(generalCard, "Show contrast slider",
            "Display a contrast adjustment under each brightness slider.",
            0, 60, generalCard.Width, 60, toggleContrast);
        toggleContrast.ValueChanged += v => _draft.EnableContrastSlider = v;

        y += generalCard.Height + 16;

        // SECTION: Brightness
        y += AddSectionHeader("Brightness", lp, y);

        var stepCard = new RoundedPanel(R)
        {
            Left = lp, Top = y, Width = cw - lp * 2 + 2, Height = 64,
            BackColor = Surface,
        };
        Controls.Add(stepCard);

        int stepVal = Math.Clamp(_draft.BrightnessStep, 1, 10);
        var spinner = new Win11Spinner(stepVal, 1, 10, Surface2, Border, FgMain, AccentLt)
        {
            Width = 120, Height = 32,
        };
        spinner.ValueChanged += v => _draft.BrightnessStep = v;

        AddSettingRow(stepCard, "Step size",
            "Applied to scroll wheel, hotkeys and panel slider.",
            0, 0, stepCard.Width, 64, spinner);

        y += stepCard.Height + 16;

        // SECTION: Hotkeys
        y += AddSectionHeader("Global Hotkeys", lp, y);

        Controls.Add(new Label
        {
            Text = "Click a key box then press your desired combination.",
            Left = lp, Top = y, Width = cw - lp * 2, Height = 18,
            ForeColor = FgSub, BackColor = Color.Transparent,
            Font = new Font("Segoe UI", 8.5f),
        });
        y += 24;

        var monitors = _monitorManager.Monitors;
        for (int mi = 0; mi < Math.Min(monitors.Count, 4); mi++)
        {
            var mon = monitors[mi];
            string monLabel = mon.Name;
            int pi = monLabel.LastIndexOf(" (", StringComparison.Ordinal);
            if (pi > 0) monLabel = monLabel[..pi];

            var monCard = new RoundedPanel(R)
            {
                Left = lp, Top = y, Width = cw - lp * 2 + 2, Height = 36 + 36 + 36 + 1,
                BackColor = Surface,
            };
            Controls.Add(monCard);

            monCard.Controls.Add(new Label
            {
                Text = monLabel, Left = 14, Top = 0,
                Width = monCard.Width - 28, Height = 35,
                ForeColor = FgMain, BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
            });
            monCard.Controls.Add(new Panel
                { Left = 14, Top = 35, Width = monCard.Width - 28, Height = 1, BackColor = Border });

            foreach (bool isUp in new[] { true, false })
            {
                var action   = (HotkeyAction)(mi * 2 + (isUp ? 0 : 1));
                var existing = _draft.Hotkeys
                    .FirstOrDefault(h => h.Action == action)
                    ?.ToDefinition() ?? new HotkeyDefinition { Action = action };

                int rowTop = isUp ? 36 : 72;
                var row = new HotkeyRow(action, existing, Surface, Border, FgMain, FgSub, AccentLt);
                row.Left = 1; row.Top = rowTop; row.Width = monCard.Width - 2;
                monCard.Controls.Add(row);
                _hotkeyRows.Add(row);

                if (!isUp)
                    monCard.Controls.Add(new Panel
                        { Left = 14, Top = rowTop - 1, Width = monCard.Width - 28, Height = 1, BackColor = Border });
            }

            y += monCard.Height + 10;
        }
        y += 10;

        // Footer separator
        Controls.Add(new Panel { Left = 0, Top = y, Width = cw + 4, Height = 1, BackColor = Border });
        y += 16;

        var btnSave = MakeButton("Save", cw - 196, y, 94, Accent, Color.White, bold: true);
        btnSave.Click += OnSave;
        Controls.Add(btnSave);

        var btnCancel = MakeButton("Cancel", cw - 94, y, 86, Surface2, FgSub);
        btnCancel.Click += (_, _) => Close();
        Controls.Add(btnCancel);

        y += 48;
        ClientSize = new Size(Width - 16, y);
        Height     = Math.Min(y + 40, Screen.PrimaryScreen!.WorkingArea.Height - 80);
    }

    private static void AddSettingRow(Panel card, string title, string sub,
        int rx, int ry, int rw, int rh, Control ctl)
    {
        int textW = rw - 32 - ctl.Width - 16;
        card.Controls.Add(new Label
        {
            Text = title, Left = rx + 16, Top = ry + 10,
            Width = textW, Height = 18,
            ForeColor = FgMain, BackColor = Color.Transparent,
            Font = new Font("Segoe UI", 9.5f),
            TextAlign = ContentAlignment.MiddleLeft,
        });
        card.Controls.Add(new Label
        {
            Text = sub, Left = rx + 16, Top = ry + 30,
            Width = textW, Height = 16,
            ForeColor = FgSub, BackColor = Color.Transparent,
            Font = new Font("Segoe UI", 8f),
            TextAlign = ContentAlignment.MiddleLeft,
        });
        ctl.Left = rx + rw - ctl.Width - 14;
        ctl.Top  = ry + (rh - ctl.Height) / 2;
        card.Controls.Add(ctl);
    }

    private int AddSectionHeader(string text, int x, int y)
    {
        Controls.Add(new Label
        {
            Text = text, Left = x, Top = y,
            Width = 300, Height = 22,
            ForeColor = FgMain,
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            BackColor = Color.Transparent,
        });
        return 30;
    }

    private void OnSave(object? sender, EventArgs e)
    {
        _draft.Hotkeys.Clear();
        foreach (var row in _hotkeyRows)
        {
            var def = row.GetDefinition();
            if (def.IsValid)
                _draft.Hotkeys.Add(new AppSettings.HotkeyEntry
                    { Action = def.Action, Modifiers = def.Modifiers, Key = def.Key });
        }
        StartupManager.Apply(_draft.StartWithWindows);
        SettingsManager.Save(_draft);
        SettingsSaved?.Invoke(_draft);
        Close();
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        var capturing = _hotkeyRows.FirstOrDefault(r => r.IsCapturing);
        if (capturing != null) { capturing.HandleKey(new KeyEventArgs(keyData)); return true; }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    // Returns a Win11-style rounded owner-drawn button
    private static RndBtn MakeButton(string text, int x, int y, int w,
        Color bg, Color fg, bool bold = false)
    {
        var b = new RndBtn(bg, fg, bold) { Text = text, Left = x, Top = y, Width = w, Height = 34 };
        b.TabStop = false;
        return b;
    }

    private static AppSettings Clone(AppSettings src)
    {
        var dst = new AppSettings
        {
            StartWithWindows     = src.StartWithWindows,
            EnableContrastSlider = src.EnableContrastSlider,
            BrightnessStep       = src.BrightnessStep,
        };
        foreach (var h in src.Hotkeys)
            dst.Hotkeys.Add(new AppSettings.HotkeyEntry
                { Action = h.Action, Modifiers = h.Modifiers, Key = h.Key });
        return dst;
    }

    //  RndBtn: Win11-style owner-drawn rounded button 
    private sealed class RndBtn : Control
    {
        private bool _hot, _pressed;
        private readonly Color _bg, _bgHot, _bgPress, _fg, _borderCol;
        private readonly bool  _bold;
        private const int Rad = 6;

        public RndBtn(Color bg, Color fg, bool bold = false)
        {
            _bg      = bg;
            _bgHot   = Lighten(bg, 18);
            _bgPress = Darken(bg, 12);
            _fg      = fg;
            _borderCol = Darken(bg, 30);
            _bold    = bold;
            DoubleBuffered = true;
            Cursor = Cursors.Hand;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint
                   | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode     = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            Color bg = _pressed ? _bgPress : _hot ? _bgHot : _bg;

            using (var path = RndRect(rect, Rad))
            {
                g.FillPath(new SolidBrush(bg), path);
                using var pen = new Pen(_borderCol, 1f);
                g.DrawPath(pen, path);
            }

            using var sf   = new StringFormat
                { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            using var font = new Font("Segoe UI", 9.5f, _bold ? FontStyle.Bold : FontStyle.Regular);
            g.DrawString(Text, font, new SolidBrush(_fg),
                new RectangleF(0, 0, Width, Height), sf);
        }

        protected override void OnMouseEnter(EventArgs e)  { _hot = true;           Invalidate(); }
        protected override void OnMouseLeave(EventArgs e)  { _hot = _pressed = false; Invalidate(); }
        protected override void OnMouseDown(MouseEventArgs e)
        { if (e.Button == MouseButtons.Left) { _pressed = true;  Invalidate(); } }
        protected override void OnMouseUp(MouseEventArgs e)
        { if (e.Button == MouseButtons.Left) { _pressed = false; Invalidate(); } }
        protected override void OnTextChanged(EventArgs e) { base.OnTextChanged(e); Invalidate(); }

        // Fire Click on mouse-up over the control (mirrors Button behaviour)
        protected override void OnMouseClick(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) base.OnMouseClick(e);
        }

        private static GraphicsPath RndRect(Rectangle r, int rad)
        {
            var p = new GraphicsPath();
            p.AddArc(r.X,            r.Y,             rad*2, rad*2, 180, 90);
            p.AddArc(r.Right-rad*2,  r.Y,             rad*2, rad*2, 270, 90);
            p.AddArc(r.Right-rad*2,  r.Bottom-rad*2,  rad*2, rad*2,   0, 90);
            p.AddArc(r.X,            r.Bottom-rad*2,  rad*2, rad*2,  90, 90);
            p.CloseFigure();
            return p;
        }

        private static Color Lighten(Color c, int a)
            => Color.FromArgb(Math.Min(255,c.R+a), Math.Min(255,c.G+a), Math.Min(255,c.B+a));
        private static Color Darken(Color c, int a)
            => Color.FromArgb(Math.Max(0,c.R-a), Math.Max(0,c.G-a), Math.Max(0,c.B-a));
    }

    //  RoundedPanel 
    // The card itself fills the rounded background and clips children.
    // A sibling BorderOverlay (WS_EX_TRANSPARENT) is created automatically and paints
    // the border AFTER all sibling/child windows — including native child HWNDs — so the
    // rounded border is never covered regardless of what children paint.
    private sealed class RoundedPanel : Panel
    {
        private readonly int _r;
        private BorderOverlay? _overlay;

        public RoundedPanel(int radius) { _r = radius; DoubleBuffered = true; }

        protected override void OnPaintBackground(PaintEventArgs e)
            => e.Graphics.Clear(Parent?.BackColor ?? BackColor);

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var path = RoundRect(new Rectangle(0, 0, Width - 1, Height - 1), _r);
            g.FillPath(new SolidBrush(BackColor), path);
            // Border is drawn by BorderOverlay on top of all children — not here.
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            using var path = RoundRect(new Rectangle(0, 0, Width, Height), _r);
            Region = new Region(path);
            if (_overlay != null) { _overlay.Bounds = Bounds; _overlay.Invalidate(); }
            Invalidate();
        }

        protected override void OnLocationChanged(EventArgs e)
        {
            base.OnLocationChanged(e);
            if (_overlay != null) _overlay.Bounds = Bounds;
        }

        // When added to a parent, create and inject a transparent border overlay
        // as a sibling so it paints on top of this card AND all its native children.
        protected override void OnParentChanged(EventArgs e)
        {
            base.OnParentChanged(e);
            if (Parent != null && _overlay == null)
            {
                _overlay        = new BorderOverlay(_r);
                _overlay.Bounds = Bounds;
                Parent.Controls.Add(_overlay);
            }
        }

        public static GraphicsPath RoundRect(Rectangle r, int rad)
        {
            var p = new GraphicsPath();
            p.AddArc(r.X,              r.Y,               rad * 2, rad * 2, 180, 90);
            p.AddArc(r.Right - rad*2,  r.Y,               rad * 2, rad * 2, 270, 90);
            p.AddArc(r.Right - rad*2,  r.Bottom - rad*2,  rad * 2, rad * 2,   0, 90);
            p.AddArc(r.X,              r.Bottom - rad*2,  rad * 2, rad * 2,  90, 90);
            p.CloseFigure();
            return p;
        }
    }

    // Transparent sibling overlay that draws ONLY the rounded border.
    // WS_EX_TRANSPARENT guarantees it composites after every sibling window (and their
    // native child HWNDs) so the border is always visible on top.
    private sealed class BorderOverlay : Control
    {
        private readonly int _r;
        public BorderOverlay(int r)
        {
            _r = r;
            SetStyle(ControlStyles.SupportsTransparentBackColor |
                     ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer, true);
            BackColor = Color.Transparent;
        }

        // WS_EX_TRANSPARENT: this window is painted after all its siblings.
        protected override CreateParams CreateParams
        {
            get { var cp = base.CreateParams; cp.ExStyle |= 0x00000020; return cp; }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var path = RoundedPanel.RoundRect(new Rectangle(0, 0, Width - 1, Height - 1), _r);
            using var pen  = new Pen(Border, 1f);
            g.DrawPath(pen, path);
        }
    }

    //  ToggleSwitch 
    private sealed class ToggleSwitch : Control
    {
        public event Action<bool>? ValueChanged;

        private bool  _on;
        private float _thumb;
        private readonly System.Windows.Forms.Timer _anim = new() { Interval = 15 };
        private readonly Color _onColor;
        private readonly Color _parentBg;

        public ToggleSwitch(bool on, Color onColor, Color parentBg)
        {
            _on       = on;
            _thumb    = on ? 1f : 0f;
            _onColor  = onColor;
            _parentBg = parentBg;
            Width     = 44; Height = 24;
            Cursor    = Cursors.Hand;
            DoubleBuffered = true;
            SetStyle(ControlStyles.SupportsTransparentBackColor
                   | ControlStyles.UserPaint
                   | ControlStyles.AllPaintingInWmPaint, true);
            BackColor = Color.Transparent;

            _anim.Tick += (_, _) =>
            {
                float target = _on ? 1f : 0f;
                float delta  = (target - _thumb);
                _thumb += delta * 0.25f;
                if (Math.Abs(_thumb - target) < 0.005f) { _thumb = target; _anim.Stop(); }
                Invalidate();
            };
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            _on = !_on;
            _anim.Stop();
            _anim.Start();
            ValueChanged?.Invoke(_on);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
            => e.Graphics.Clear(_parentBg);

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            const int th = 20;
            float travel = Width - Height;
            float ty     = (Height - th) / 2f;
            float tx     = (Height - th) / 2f + _thumb * travel;

            Color pillBg = Lerp(S_ToggleOff, _onColor, _thumb);
            using (var path = Pill(0, 0, Width, Height))
                g.FillPath(new SolidBrush(pillBg), path);

            g.FillEllipse(Brushes.White, tx, ty, th, th);
        }

        private static Color Lerp(Color a, Color b, float t)
            => Color.FromArgb(
                Math.Clamp((int)(a.R + (b.R - a.R) * t), 0, 255),
                Math.Clamp((int)(a.G + (b.G - a.G) * t), 0, 255),
                Math.Clamp((int)(a.B + (b.B - a.B) * t), 0, 255));

        private static GraphicsPath Pill(int x, int y, int w, int h)
        {
            int r = h / 2;
            var p = new GraphicsPath();
            p.AddArc(x,        y, r * 2, h, 90, 180);
            p.AddArc(x+w-r*2,  y, r * 2, h, 270, 180);
            p.CloseFigure();
            return p;
        }
    }

    //  Win11Spinner 
    private sealed class Win11Spinner : Control
    {
        public event Action<int>? ValueChanged;
        private int _value, _min, _max;
        private readonly Color _bg, _border, _fg, _accent;
        private bool _upHot, _downHot;

        public Win11Spinner(int value, int min, int max,
            Color bg, Color border, Color fg, Color accent)
        {
            _value  = value; _min = min; _max = max;
            _bg = bg; _border = border; _fg = fg; _accent = accent;
            DoubleBuffered = true;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint
                   | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            int btnW = 26;
            int mid  = Width - btnW;

            using (var path = RoundRect(rect, 5))
            {
                g.FillPath(new SolidBrush(_bg), path);
                g.DrawPath(new Pen(_border), path);
            }

            g.DrawLine(new Pen(_border), mid, 1, mid, Height - 2);

            int btnMid = Height / 2;
            g.DrawLine(new Pen(_border), mid + 1, btnMid, Width - 2, btnMid);

            if (_upHot)
                g.FillRectangle(new SolidBrush(S_SpinnerHovBg),
                    mid + 1, 1, btnW - 2, btnMid - 1);

            if (_downHot)
                g.FillRectangle(new SolidBrush(S_SpinnerHovBg),
                    mid + 1, btnMid + 1, btnW - 2, Height - btnMid - 3);

            string txt = _value.ToString();
            using var valFont = new Font("Segoe UI", 10f, FontStyle.Bold);
            var valRect = new Rectangle(0, 0, mid, Height);
            g.DrawString(txt, valFont, new SolidBrush(_accent), valRect,
                new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });

            DrawChevron(g, mid + btnW / 2, btnMid / 2, up: true,
                _upHot ? S_SpinnerChevronHot : S_SpinnerChevron);
            DrawChevron(g, mid + btnW / 2, btnMid + (Height - btnMid) / 2, up: false,
                _downHot ? S_SpinnerChevronHot : S_SpinnerChevron);
        }

        private static void DrawChevron(Graphics g, int cx, int cy, bool up, Color col)
        {
            float half = 3.5f, h = 2.5f;
            PointF[] pts = up
                ? new[] { new PointF(cx - half, cy + h * .5f), new PointF(cx, cy - h * .5f), new PointF(cx + half, cy + h * .5f) }
                : new[] { new PointF(cx - half, cy - h * .5f), new PointF(cx, cy + h * .5f), new PointF(cx + half, cy - h * .5f) };
            using var pen = new Pen(col, 1.5f) { LineJoin = LineJoin.Round };
            g.DrawLines(pen, pts);
        }

        private static GraphicsPath RoundRect(Rectangle r, int rad)
        {
            var p = new GraphicsPath();
            p.AddArc(r.X,             r.Y,              rad*2, rad*2, 180, 90);
            p.AddArc(r.Right-rad*2,   r.Y,              rad*2, rad*2, 270, 90);
            p.AddArc(r.Right-rad*2,   r.Bottom-rad*2,   rad*2, rad*2,   0, 90);
            p.AddArc(r.X,             r.Bottom-rad*2,   rad*2, rad*2,  90, 90);
            p.CloseFigure();
            return p;
        }

        private bool IsUpRegion(Point pt)   => pt.X >= Width - 26 && pt.Y < Height / 2;
        private bool IsDownRegion(Point pt) => pt.X >= Width - 26 && pt.Y >= Height / 2;

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (IsUpRegion(e.Location))
            { if (_value < _max) { _value++; ValueChanged?.Invoke(_value); Invalidate(); } }
            else if (IsDownRegion(e.Location))
            { if (_value > _min) { _value--; ValueChanged?.Invoke(_value); Invalidate(); } }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            bool u = IsUpRegion(e.Location), d = IsDownRegion(e.Location);
            if (u != _upHot || d != _downHot) { _upHot = u; _downHot = d; Invalidate(); }
            Cursor = (u || d) ? Cursors.Hand : Cursors.Default;
        }

        protected override void OnMouseLeave(EventArgs e)
        { if (_upHot || _downHot) { _upHot = _downHot = false; Invalidate(); } }
    }

    //  HotkeyRow 
    private sealed class HotkeyRow : Panel
    {
        private readonly HotkeyDefinition _def;
        private readonly TextBox _keyBox;
        private bool _capturing;

        public HotkeyRow(HotkeyAction action, HotkeyDefinition current,
            Color bg, Color border, Color fg, Color dim, Color accent)
        {
            _def = current; Height = 36;
            // Use the card surface color — avoids transparent bleed-through at corners
            BackColor = bg;

            bool isUp   = HotkeyDefinition.IsIncrease(action);
            Color badgeFg = isUp
                ? Color.FromArgb(52, 211, 153)
                : Color.FromArgb(248, 113, 113);

            Controls.Add(new Label
            {
                Text = isUp ? "+ Increase" : "- Decrease",
                Left = 16, Top = 0, Width = 110, Height = 36,
                ForeColor = badgeFg, BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
            });

            // Win11-style key input: TextBox inside a rounded-border wrapper
            _keyBox = new TextBox
            {
                Left = 6, Top = 3, Width = 183, Height = 20,
                ReadOnly = true,
                Text = current.IsValid ? current.ToString() : "(none)",
                BackColor = S_KeyBoxBg,
                ForeColor = current.IsValid ? fg : dim,
                Font = new Font("Segoe UI", 9f),
                BorderStyle = BorderStyle.None,
                Cursor = Cursors.Hand,
                TextAlign = HorizontalAlignment.Center,
            };

            // Rounded wrapper panel around the key box
            var kbWrap = new KeyBoxPanel(border)
            {
                Left = 132, Top = 4, Width = 197, Height = 28,
                BackColor = S_KeyBoxBg,
            };
            kbWrap.Controls.Add(_keyBox);
            kbWrap.Click += (_, _) => _keyBox.Focus();

            // Clear button — Win11 style (rounded, subtle border, red text)
            var clr = new RndBtn(S_ClearBtnBg, S_ClearBtnFg, S_ClearBtnBorder)
            {
                Text = "Clear", Left = 338, Top = 5, Width = 56, Height = 26,
            };
            clr.Click += (_, _) =>
            {
                _def.Key = Keys.None; _def.Modifiers = HotkeyModifiers.None;
                _keyBox.Text = "(none)"; _keyBox.ForeColor = dim;
            };

            _keyBox.Click    += (_, _) => StartCapture(accent);
            _keyBox.GotFocus += (_, _) => StartCapture(accent);
            _keyBox.LostFocus+= (_, _) => StopCapture(fg, dim);
            _keyBox.KeyDown  += OnKeyDown;
            _keyBox.KeyUp    += OnKeyUp;
            _keyBox.PreviewKeyDown += (_, e) => e.IsInputKey = true;

            Controls.AddRange(new Control[] { kbWrap, clr });
        }

        public bool IsCapturing => _capturing;

        private void StartCapture(Color accent)
        {
            if (_capturing) return;
            _capturing = true;
            _keyBox.Text = "Press keys...";
            _keyBox.ForeColor = accent;
        }

        private void StopCapture(Color fg, Color dim)
        {
            _capturing = false;
            _keyBox.ForeColor = _def.IsValid ? fg : dim;
            _keyBox.Text = _def.IsValid ? _def.ToString() : "(none)";
        }

        public void HandleKey(KeyEventArgs e)
        {
            if (!_capturing) return;
            var mod = BuildModifiers(e);
            if (e.KeyCode is Keys.ControlKey or Keys.ShiftKey or Keys.Menu
                             or Keys.LWin or Keys.RWin)
            {
                _keyBox.Text = mod == HotkeyModifiers.None
                    ? "Press keys..." : ModString(mod) + " + ...";
                return;
            }
            _def.Modifiers = mod; _def.Key = e.KeyCode;
            _keyBox.Text = _def.ToString();
            _keyBox.ForeColor = Parent?.ForeColor ?? FgMain;
            _capturing = false;
        }

        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            if (!_capturing) return;
            e.Handled = true; e.SuppressKeyPress = true;
            HandleKey(e);
        }

        private void OnKeyUp(object? sender, KeyEventArgs e)
        {
            if (!_capturing) return;
            var mod = BuildModifiers(e);
            _keyBox.Text = mod == HotkeyModifiers.None
                ? "Press keys..." : ModString(mod) + " + ...";
        }

        private static HotkeyModifiers BuildModifiers(KeyEventArgs e)
            => (e.Control ? HotkeyModifiers.Ctrl  : 0) |
               (e.Alt     ? HotkeyModifiers.Alt   : 0) |
               (e.Shift   ? HotkeyModifiers.Shift : 0);

        private static string ModString(HotkeyModifiers m)
        {
            var p = new List<string>();
            if ((m & HotkeyModifiers.Ctrl)  != 0) p.Add("Ctrl");
            if ((m & HotkeyModifiers.Alt)   != 0) p.Add("Alt");
            if ((m & HotkeyModifiers.Shift) != 0) p.Add("Shift");
            return string.Join(" + ", p);
        }

        public HotkeyDefinition GetDefinition() => _def;

        // RndBtn is re-declared locally to avoid scope issues — delegate to outer
        private sealed class RndBtn : Control
        {
            private bool _hot, _pressed;
            private readonly Color _bg, _bgHot, _bgPress, _fg, _borderCol;
            private const int Rad = 5;

            public RndBtn(Color bg, Color fg, Color? borderColor = null)
            {
                _bg = bg; _bgHot = Lighten(bg, 18); _bgPress = Darken(bg, 12);
                _fg = fg; _borderCol = borderColor ?? Color.FromArgb(Math.Max(0,bg.R-8), Math.Max(0,bg.G-8), Math.Max(0,bg.B-8));
                DoubleBuffered = true; Cursor = Cursors.Hand; TabStop = false;
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint
                       | ControlStyles.ResizeRedraw, true);
                SetStyle(ControlStyles.Selectable, false);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.SmoothingMode     = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                // Fill parent bg into corners so no white bleeds around the rounded rect
                g.Clear(Parent?.BackColor ?? Surface);
                var rect = new Rectangle(0, 0, Width - 1, Height - 1);
                Color bg = _pressed ? _bgPress : _hot ? _bgHot : _bg;
                using (var path = RndRect(rect, Rad))
                {
                    g.FillPath(new SolidBrush(bg), path);
                    g.DrawPath(new Pen(_borderCol, 1f), path);
                }
                using var sf   = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                using var font = new Font("Segoe UI", 8.5f);
                g.DrawString(Text, font, new SolidBrush(_fg), new RectangleF(0, 0, Width, Height), sf);
            }

            protected override void OnMouseEnter(EventArgs e)  { _hot = true;             Invalidate(); }
            protected override void OnMouseLeave(EventArgs e)  { _hot = _pressed = false;  Invalidate(); }
            protected override void OnMouseDown(MouseEventArgs e) { if (e.Button == MouseButtons.Left) { _pressed = true;  Invalidate(); } }
            protected override void OnMouseUp(MouseEventArgs e)   { if (e.Button == MouseButtons.Left) { _pressed = false; Invalidate(); } }
            protected override void OnMouseClick(MouseEventArgs e) { if (e.Button == MouseButtons.Left) base.OnMouseClick(e); }
            protected override void OnTextChanged(EventArgs e) { base.OnTextChanged(e); Invalidate(); }

            private static GraphicsPath RndRect(Rectangle r, int rad)
            {
                var p = new GraphicsPath();
                p.AddArc(r.X, r.Y, rad*2, rad*2, 180, 90);
                p.AddArc(r.Right-rad*2, r.Y, rad*2, rad*2, 270, 90);
                p.AddArc(r.Right-rad*2, r.Bottom-rad*2, rad*2, rad*2, 0, 90);
                p.AddArc(r.X, r.Bottom-rad*2, rad*2, rad*2, 90, 90);
                p.CloseFigure(); return p;
            }

            private static Color Lighten(Color c, int a) => Color.FromArgb(Math.Min(255,c.R+a), Math.Min(255,c.G+a), Math.Min(255,c.B+a));
            private static Color Darken(Color c, int a)  => Color.FromArgb(Math.Max(0,c.R-a), Math.Max(0,c.G-a), Math.Max(0,c.B-a));
        }

        // Rounded border wrapper for the TextBox
        private sealed class KeyBoxPanel : Panel
        {
            private readonly Color _border;
            public KeyBoxPanel(Color border)
            {
                _border = border;
                SetStyle(ControlStyles.AllPaintingInWmPaint |
                         ControlStyles.OptimizedDoubleBuffer |
                         ControlStyles.ResizeRedraw, true);
            }
            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(BackColor);
                using var path = RndRect(new Rectangle(0, 0, Width - 1, Height - 1), 5);
                g.FillPath(new SolidBrush(BackColor), path);
                using var pen = new Pen(_border, 1f);
                g.DrawPath(pen, path);
                // Also clip children to rounded shape
                using var clip = RndRect(new Rectangle(0, 0, Width, Height), 5);
                Region = new Region(clip);
            }
            private static GraphicsPath RndRect(Rectangle r, int rad)
            {
                var p = new GraphicsPath();
                p.AddArc(r.X, r.Y, rad*2, rad*2, 180, 90);
                p.AddArc(r.Right-rad*2, r.Y, rad*2, rad*2, 270, 90);
                p.AddArc(r.Right-rad*2, r.Bottom-rad*2, rad*2, rad*2, 0, 90);
                p.AddArc(r.X, r.Bottom-rad*2, rad*2, rad*2, 90, 90);
                p.CloseFigure(); return p;
            }
        }
    }
}