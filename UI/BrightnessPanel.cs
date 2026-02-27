using System.ComponentModel;
using System.Drawing.Drawing2D;
using BrightnessController.Monitors;
using BrightnessController.Settings;

namespace BrightnessController.UI;

public sealed class BrightnessPanel : Form
{
    private readonly MonitorManager _monitors;
    public event Action? SettingsRequested;

    //  Colors — refreshed by ApplyTheme() each time the panel opens
    static Color ColForm      = Color.FromArgb(26,  26,  28);
    static Color ColCard      = Color.FromArgb(34,  34,  37);
    static Color ColBorderOut = Color.FromArgb(52,  52,  57);
    static Color ColBorderCrd = Color.FromArgb(44,  44,  48);
    static Color ColSep       = Color.FromArgb(42,  42,  46);
    static Color ColBlue      = Color.FromArgb(59,  130, 246);
    static Color ColTrackBg   = Color.FromArgb(50,  50,  55);
    static Color ColValBlue   = Color.FromArgb(100, 160, 250);
    static Color ColHeader    = Color.FromArgb(100, 100, 108);
    static Color ColMonIcon   = Color.FromArgb(70,  70,  78);
    static Color ColMonName   = Color.FromArgb(204, 204, 210);
    static Color ColLblIcon   = Color.FromArgb(130, 130, 140);
    static Color ColLblText   = Color.FromArgb(210, 210, 215);
    static Color ColFootTxt   = Color.FromArgb(72,  72,  80);
    static Color ColGearNorm  = Color.FromArgb(72,  72,  80);
    static Color ColGearHov   = Color.FromArgb(160, 160, 168);
    static Color ColGearBgHov = Color.FromArgb(40,  40,  44);

    public static void ApplyTheme()
    {
        bool light = BrightnessController.Helpers.ThemeHelper.IsLightTheme;
        // Live Windows accent colour
        var accent      = BrightnessController.Helpers.ThemeHelper.AccentColor;
        var accentLight = BrightnessController.Helpers.ThemeHelper.AccentColorLight;
        var accentDark  = BrightnessController.Helpers.ThemeHelper.AccentColorDark;

        if (light)
        {
            ColForm      = Color.FromArgb(245, 245, 247);
            ColCard      = Color.FromArgb(255, 255, 255);
            ColBorderOut = Color.FromArgb(210, 210, 215);
            ColBorderCrd = Color.FromArgb(225, 225, 228);
            ColSep       = Color.FromArgb(218, 218, 222);
            ColBlue      = accent;        // slider fill = accent
            ColTrackBg   = Color.FromArgb(200, 200, 210);
            ColValBlue   = accentDark;    // value label = slightly darker accent
            ColHeader    = Color.FromArgb(140, 140, 150);
            ColMonIcon   = Color.FromArgb(100, 100, 110);
            ColMonName   = Color.FromArgb(25,  25,  30);
            ColLblIcon   = Color.FromArgb(100, 100, 110);
            ColLblText   = Color.FromArgb(40,  40,  48);
            ColFootTxt   = Color.FromArgb(120, 120, 130);
            ColGearNorm  = Color.FromArgb(120, 120, 130);
            ColGearHov   = Color.FromArgb(40,  40,  50);
            ColGearBgHov = Color.FromArgb(228, 228, 232);
        }
        else
        {
            ColForm      = Color.FromArgb(26,  26,  28);
            ColCard      = Color.FromArgb(34,  34,  37);
            ColBorderOut = Color.FromArgb(52,  52,  57);
            ColBorderCrd = Color.FromArgb(44,  44,  48);
            ColSep       = Color.FromArgb(42,  42,  46);
            ColBlue      = accent;        // slider fill = accent
            ColTrackBg   = Color.FromArgb(50,  50,  55);
            ColValBlue   = accentLight;   // value label = lighter accent
            ColHeader    = Color.FromArgb(100, 100, 108);
            ColMonIcon   = Color.FromArgb(70,  70,  78);
            ColMonName   = Color.FromArgb(204, 204, 210);
            ColLblIcon   = Color.FromArgb(130, 130, 140);
            ColLblText   = Color.FromArgb(210, 210, 215);
            ColFootTxt   = Color.FromArgb(72,  72,  80);
            ColGearNorm  = Color.FromArgb(72,  72,  80);
            ColGearHov   = Color.FromArgb(160, 160, 168);
            ColGearBgHov = Color.FromArgb(40,  40,  44);
        }
    }

    //  Layout 
    const int W           = 360;   // total width
    const int Pad         = 20;    // outer horizontal & vertical padding
    const int Rout        = 12;    // outer form corner radius
    const int Rcard       = 8;     // card corner radius
    const int CrdPad      = 20;    // card inner padding = p-5
    // Header
    const int HdrH        = 16;
    const int HdrMB       = 20;    // mb-5
    // Card – name row (p-5=20 card padding, pb-3=12, mb-4=16)
    const int NameH       = 20;    // icon+name row height  (text-[12px], ~20px line)
    const int NamePB      = 12;    // pb-3 = 12px below name before sep
    const int SepH        = 1;
    const int SepMT       = 16;    // mb-4 = 16px gap after sep before first field
    // Slider field (mb-1.5=6, h-4=16)
    const int LblH        = 24;    // label row
    const int LblMB       = 6;     // mb-1.5 = 6px between label and track
    const int TrkH        = 16;    // h-4 = 16px track container
    const int FieldH      = 46;    // LblH+LblMB+TrkH = 24+6+16
    const int SpaceY      = 16;    // space-y-4 between slider fields
    const int CardBotPad  = 20;    // p-5 = 20px bottom card padding
    // Footer
    const int CardGap     = 4;     // space-y-1 = 4px between cards
    const int FootMT      = 20;    // mt-5
    const int FootSepH    = 1;
    const int FootPT      = 16;    // pt-4 = 16px
    const int FootRowH    = 26;
    const int BotPad      = 20;    // outer bottom = p-5

    public BrightnessPanel(MonitorManager monitors)
    {
        _monitors       = monitors;
        FormBorderStyle = FormBorderStyle.None;
        StartPosition   = FormStartPosition.Manual;
        ShowInTaskbar   = false;
        TopMost         = true;
        Width           = W;
        Height          = 10;
        BackColor       = ColForm;
        Deactivate     += (_, _) => Hide();
        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer, true);
    }

    public void ShowAtTray(Point anchor)
    {
        ApplyTheme();
        BackColor = ColForm;
        BuildUI();
        PositionAtTray(anchor);
        Show();
        Activate();
    }

    //  Build UI 
    private void BuildUI()
    {
        Controls.Clear();

        int cw = W - Pad * 2;   // inner content width
        int y  = Pad;

        //  Header "LITEBRIGHT" 
        Controls.Add(Lbl("LITEBRIGHT",
            new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ColHeader, Pad + 4, y, cw - 4, HdrH,
            ContentAlignment.MiddleLeft, ColForm));
        y += HdrH + HdrMB;

        //  Cards 
        bool showContrast = SettingsManager.Current.EnableContrastSlider;
        var mons = _monitors.Monitors;
        for (int i = 0; i < mons.Count; i++)
        {
            var mon = mons[i];
            var m   = mon;
            bool isLast = i == mons.Count - 1;

            int contentH = NameH + NamePB + SepH + SepMT
                         + FieldH
                         + (showContrast ? SpaceY + FieldH : 0)
                         + CardBotPad;
            int cardH    = CrdPad + contentH;
            int cx       = Pad;

            var card = new CardCtl(cardH, Rcard) { Left = cx, Top = y, Width = cw, Height = cardH };

            int cy = CrdPad;
            int iw = cw - CrdPad * 2;  // inner content width (card p-5 on each side)

            // Monitor icon (custom drawn) + name
            var iconLbl = new MonitorIconCtl
            { Left = CrdPad, Top = cy, Width = 18, Height = NameH };
            card.Controls.Add(iconLbl);

            card.Controls.Add(Lbl(CleanName(mon.Name),
                new Font("Segoe UI", 11f, FontStyle.Bold),
                ColMonName, CrdPad + 20, cy, iw - 20, NameH,
                ContentAlignment.MiddleLeft, Color.Transparent));
            cy += NameH + NamePB;

            // Separator
            card.Controls.Add(new Panel { Left = CrdPad, Top = cy, Width = iw, Height = SepH, BackColor = ColSep });
            cy += SepH + SepMT;

            // Brightness field — Left=CrdPad, width=iw
            var bf = MakeField(cy, iw, "BRIGHTNESS", SliderIcon.Sun,
                mon.BrightnessPercent, pct => _monitors.SetBrightness(m, pct));
            bf.Left = CrdPad;
            card.Controls.Add(bf);
            cy += FieldH;

            // Contrast field — only when enabled in settings
            if (showContrast)
            {
                cy += SpaceY;
                var cf = MakeField(cy, iw, "CONTRAST", SliderIcon.Contrast,
                    mon.IsInternal ? 50 : mon.ContrastPercent,
                    pct => { if (!m.IsInternal) _monitors.SetContrast(m, pct); });
                cf.Left = CrdPad;
                card.Controls.Add(cf);
            }

            Controls.Add(card);
            y += cardH + (isLast ? 0 : CardGap);
        }

        //  Footer 
        y += FootMT;
        Controls.Add(new Panel { Left = Pad, Top = y, Width = cw, Height = FootSepH, BackColor = ColSep });
        y += FootSepH + FootPT;

        Controls.Add(Lbl("PARAMETERS",
            new Font("Segoe UI", 8f, FontStyle.Bold),
            ColFootTxt, Pad + 4, y, cw - 38, FootRowH,
            ContentAlignment.MiddleLeft, ColForm));

        var gear = new GearCtl { Left = Pad + cw - 30, Top = y + (FootRowH - 26) / 2 };
        gear.Click += (_, _) => { Hide(); SettingsRequested?.Invoke(); };
        Controls.Add(gear);

        y += FootRowH + BotPad;
        Height = y;
        Region = MakeRgn(W, Height, Rout);
    }

    //  Form border — drawn AFTER all children via WndProc so it's never covered 
    private const int WM_PAINT = 0x000F;

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        // Background only — border is drawn in WndProc after children
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var fill = new SolidBrush(ColForm);
        using var path = RndPath(0, 0, W, Height, Rout);
        g.FillPath(fill, path);
    }

    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);
        if (m.Msg == WM_PAINT)
        {
            using var g = Graphics.FromHwnd(Handle);
            g.SmoothingMode    = SmoothingMode.AntiAlias;
            g.PixelOffsetMode  = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            using var pen  = new Pen(ColBorderOut, 1.5f);
            using var path = RndPath(1, 1, W - 2, Height - 2, Rout - 1);
            g.DrawPath(pen, path);
        }
    }

    //  Slider field factory 
    enum SliderIcon { Sun, Contrast }

    static Panel MakeField(int y, int w, string label, SliderIcon icon, int init, Action<int> onChange)
    {
        // w = inner card width (card width minus 2*CrdPad)
        var p = new TransCtl { Left = 0, Top = y, Width = w, Height = FieldH };

        // icon — 12px lucide-style, opacity-60, gap-2(8px) before label
        p.Controls.Add(new SldIconCtl(icon) { Left = 0, Top = 0, Width = 16, Height = LblH });

        // label — px-0.5 ≈ 2px, so label starts at 16+8=24... but icon is 16, gap=4 visual
        p.Controls.Add(Lbl(label,
            new Font("Segoe UI", 9f, FontStyle.Bold),
            ColLblText, 20, 0, w - 20 - 42, LblH,
            ContentAlignment.MiddleLeft, Color.Transparent));

        // value — right-aligned, tabular, blue
        var val = Lbl($"{init}",
            new Font("Segoe UI", 12f, FontStyle.Bold),
            ColValBlue, w - 40, 0, 40, LblH,
            ContentAlignment.MiddleRight, Color.Transparent);

        // slider — full inner width, 2px track
        var sl = new Track { Left = 0, Top = LblH + LblMB, Width = w, Height = TrkH, Value = init };
        sl.ValueChanged += v => { val.Text = $"{v}"; onChange(v); };

        p.Controls.Add(val);
        p.Controls.Add(sl);
        return p;
    }

    //  Label helper 
    static Label Lbl(string text, Font f, Color fg, int x, int y, int w, int h,
        ContentAlignment align, Color bg)
        => new Label
        {
            Text = text, Font = f, ForeColor = fg,
            AutoSize = false, Left = x, Top = y, Width = w, Height = h,
            TextAlign = align, BackColor = bg,
        };

    //  Positioning 
    private void PositionAtTray(Point anchor)
    {
        var wa = Screen.FromPoint(anchor).WorkingArea;
        int x  = Math.Clamp(anchor.X - Width / 2,  wa.Left + 8, wa.Right  - Width  - 8);
        int y  = anchor.Y >= wa.Bottom - 2 ? wa.Bottom - Height - 10
               : anchor.Y <= wa.Top    + 2 ? wa.Top    + 10
               : anchor.Y - Height;
        y = Math.Clamp(y, wa.Top + 8, wa.Bottom - Height - 8);
        Location = new Point(x, y);
    }

    static string CleanName(string s) { int p = s.IndexOf('('); return p > 0 ? s[..p].Trim() : s.Trim(); }

    static Region MakeRgn(int w, int h, int r) => new Region(RndPath(0, 0, w, h, r));

    static GraphicsPath RndPath(int x, int y, int w, int h, int r)
    {
        var p = new GraphicsPath();
        if (r <= 0) { p.AddRectangle(new Rectangle(x, y, w, h)); return p; }
        p.AddArc(x,         y,         r*2, r*2, 180, 90);
        p.AddArc(x+w-r*2,   y,         r*2, r*2, 270, 90);
        p.AddArc(x+w-r*2,   y+h-r*2,   r*2, r*2,   0, 90);
        p.AddArc(x,         y+h-r*2,   r*2, r*2,  90, 90);
        p.CloseFigure();
        return p;
    }

    // 
    // CardCtl — rounded card with solid bg + border
    // 
    sealed class CardCtl : Panel
    {
        readonly int _r;
        public CardCtl(int h, int r)
        {
            _r = r; BackColor = ColCard;
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);
        }
        protected override void OnResize(EventArgs e)
        { base.OnResize(e); Region = new Region(RndPath(0, 0, Width, Height, _r)); }
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // Paint parent (form) bg into corners so no dark pixels show at antialiased edge
            e.Graphics.Clear(ColForm);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
            using var fb = new SolidBrush(ColCard);
            using var fp = RndPath(0, 0, Width, Height, _r);
            g.FillPath(fb, fp);
            // Border drawn in WndProc after children
        }
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (m.Msg == 0x000F) // WM_PAINT
            {
                using var g   = Graphics.FromHwnd(Handle);
                g.SmoothingMode   = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                using var pen = new Pen(ColBorderCrd, 1.5f);
                using var bp  = RndPath(1, 1, Width - 2, Height - 2, _r - 1);
                g.DrawPath(pen, bp);
            }
        }
    }

    // 
    // TransCtl — transparent container
    // 
    sealed class TransCtl : Panel
    {
        public TransCtl()
        {
            SetStyle(ControlStyles.SupportsTransparentBackColor |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer, true);
            BackColor = Color.Transparent;
        }
    }

    // 
    // MonitorIconCtl — draws a simple Lucide-style monitor at small size
    // 
    sealed class MonitorIconCtl : Control
    {
        public MonitorIconCtl()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
            // Center the icon
            int ox = (Width  - 13) / 2;
            int oy = (Height - 11) / 2;
            using var p = new Pen(ColMonIcon, 1.3f);
            // Screen rectangle
            g.DrawRectangle(p, ox, oy, 13, 9);
            // Stand line
            g.DrawLine(p, ox + 4, oy + 9, ox + 9, oy + 9);  // base
            g.DrawLine(p, ox + 6, oy + 9, ox + 7, oy + 11); // stem
            g.DrawLine(p, ox + 4, oy + 11, ox + 9, oy + 11);// foot
        }
    }

    // 
    // SldIconCtl — draws tiny  or  icon
    // 
    sealed class SldIconCtl : Control
    {
        readonly SliderIcon _kind;
        public SldIconCtl(SliderIcon kind)
        {
            _kind = kind;
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
            using var p  = new Pen(ColLblIcon, 1.2f);
            using var br = new SolidBrush(ColLblIcon);
            int cx = Width  / 2, cy = Height / 2;

            if (_kind == SliderIcon.Sun)
            {
                // small sun: circle + 8 rays
                g.DrawEllipse(p, cx-3, cy-3, 6, 6);
                for (int a = 0; a < 360; a += 45)
                {
                    double rad = a * Math.PI / 180;
                    float x1=(float)(cx+Math.Cos(rad)*4.5f), y1=(float)(cy+Math.Sin(rad)*4.5f);
                    float x2=(float)(cx+Math.Cos(rad)*6.5f), y2=(float)(cy+Math.Sin(rad)*6.5f);
                    g.DrawLine(p, x1, y1, x2, y2);
                }
            }
            else
            {
                // contrast: circle, fill left half
                var rc = new RectangleF(cx-5, cy-5, 10, 10);
                g.DrawEllipse(p, rc);
                var clip = new Region(new RectangleF(cx-5, cy-5, 5, 10));
                g.Clip = clip;
                g.FillEllipse(br, rc);
                g.ResetClip();
            }
        }
    }

    // 
    // GearCtl — settings gear button
    // 
    sealed class GearCtl : Control
    {
        bool _hov;
        public GearCtl()
        {
            Width = 30; Height = 26; Cursor = Cursors.Hand;
            BackColor = ColForm;
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer, true);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(_hov ? ColGearBgHov : ColForm);
            if (_hov)
            {
                using var rr = RndPath(0, 0, Width, Height, 4);
                using var fill = new SolidBrush(ColGearBgHov);
                g.FillPath(fill, rr);
            }
            // Draw gear icon
            var col = _hov ? ColGearHov : ColGearNorm;
            using var pen  = new Pen(col, 1.5f);
            using var solB = new SolidBrush(col);
            int ox = Width/2, oy = Height/2;
            // outer teeth
            for (int a = 0; a < 360; a += 45)
            {
                double r = a * Math.PI / 180;
                float x1=(float)(ox+Math.Cos(r)*5), y1=(float)(oy+Math.Sin(r)*5);
                float x2=(float)(ox+Math.Cos(r)*7.5f), y2=(float)(oy+Math.Sin(r)*7.5f);
                g.DrawLine(pen, x1, y1, x2, y2);
            }
            g.DrawEllipse(pen, ox-5, oy-5, 10, 10);
            g.FillEllipse(new SolidBrush(_hov ? ColGearBgHov : ColForm), ox-3, oy-3, 6, 6);
        }
        protected override void OnMouseEnter(EventArgs e){_hov=true; Invalidate();}
        protected override void OnMouseLeave(EventArgs e){_hov=false;Invalidate();}
    }

    // 
    // Track — 2px slider track + 12px white/blue-ring thumb
    // 
    sealed class Track : Control
    {
        int  _v = 50;
        bool _drag, _hov;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int Value
        {
            get => _v;
            set { int n=Math.Clamp(value,0,100); if(n==_v)return; _v=n; Invalidate(); }
        }
        public event Action<int>? ValueChanged;

        // React CSS: height/width=12px, border=2.5px solid blue
        // → total radius = 6f, white inner radius = 3.5f
        const int TrackPx  = 2;
        const float TotalR = 6f;    // total thumb radius = 6 (12px diam)
        const float InnerR = 3.5f;  // white fill radius (12 - 2*2.5 = 7px diam)
        const int HP       = 6;     // horizontal padding for thumb overhang

        public Track()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.SupportsTransparentBackColor |
                     ControlStyles.Selectable, true);
            BackColor = Color.Transparent; Cursor = Cursors.Hand;
            TabStop = false;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(ColCard);  // card surface — makes track look native to the card

            int tl=HP, tr=Width-HP, tw=tr-tl, cy=Height/2;
            int tx=tl+(int)Math.Round(_v/100.0*tw);
            tx=Math.Clamp(tx,tl,tr);

            // Empty track (h-[2px] rounded-full)
            FillR(g, ColTrackBg, tl, cy-1, tw, TrackPx);
            // Blue filled portion
            if(tx-tl>0) FillR(g, ColBlue, tl, cy-1, tx-tl, TrackPx);

            // Subtle hover glow (same diameter as thumb)
            if(_drag||_hov)
            {
                using var gb = new SolidBrush(Color.FromArgb(45,59,130,246));
                float gr = TotalR + 3;
                g.FillEllipse(gb, tx-gr, cy-gr, gr*2, gr*2);
            }

            // Thumb: blue ring circle first, then white fill on top
            float scale = _drag ? 1.08f : 1.0f;
            float tr2   = TotalR * scale;
            float ir    = InnerR * scale;
            using var rb = new SolidBrush(ColBlue);
            g.FillEllipse(rb, tx-tr2, cy-tr2, tr2*2, tr2*2);
            using var wb = new SolidBrush(Color.FromArgb(255,255,255));
            g.FillEllipse(wb, tx-ir, cy-ir, ir*2, ir*2);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        { if(e.Button!=MouseButtons.Left)return; _drag=true; Capture=true; SetV(e.X); }
        protected override void OnMouseMove(MouseEventArgs e){if(_drag)SetV(e.X);}
        protected override void OnMouseUp(MouseEventArgs e){_drag=false;Capture=false;Refresh();}
        protected override void OnMouseEnter(EventArgs e){_hov=true; Focus(); Invalidate();}
        protected override void OnMouseLeave(EventArgs e){_hov=false; Invalidate();}
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            int step  = Settings.SettingsManager.Current.BrightnessStep;
            int delta = e.Delta > 0 ? step : -step;
            int nv    = Math.Clamp(_v + delta, 0, 100);
            if(nv==_v) return;
            _v=nv; Refresh(); ValueChanged?.Invoke(_v);
        }

        void SetV(int x)
        {
            int tw=Width-HP*2; if(tw<=0)return;
            int p=Math.Clamp((int)Math.Round((x-HP)*100.0/tw),0,100);
            if(p==_v)return; _v=p; Refresh(); ValueChanged?.Invoke(_v);
        }

        static void FillR(Graphics g, Color c, int x, int y, int w, int h)
        {
            if(w<=0)return;
            using var b=new SolidBrush(c);
            g.FillRectangle(b, x, y, w, h);
        }
    }
}