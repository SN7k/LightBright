using BrightnessController.Helpers;
using BrightnessController.Hotkeys;
using BrightnessController.Monitors;
using BrightnessController.Settings;
using BrightnessController.UI;

namespace BrightnessController;

public sealed class TrayApplicationContext : ApplicationContext
{
    // ── Core services ─────────────────────────────────────────────────────────
    private readonly MonitorManager _monitorManager = new();
    private readonly HotkeyManager  _hotkeyManager;
    private readonly NotifyIcon     _trayIcon       = new();
    private readonly BrightnessPanel _panel;

    private ContextMenuStrip _contextMenu = new();

    private SettingsForm? _settingsForm;

    private Point _lastTrayClickPosition;

    private Native.NativeMethods.LowLevelMouseProc? _mouseHookProc;  // keep delegate alive
    private IntPtr _mouseHook = IntPtr.Zero;
    private Point _trayIconCenter;
    private bool  _trayIconCenterKnown;

    private readonly System.Windows.Forms.Timer _tooltipResetTimer = new() { Interval = 2000 };


    public TrayApplicationContext()
    {
        _hotkeyManager = new HotkeyManager();
        _hotkeyManager.HotkeyPressed += OnHotkeyPressed;

        _monitorManager.Refresh();

        _panel = new BrightnessPanel(_monitorManager);
        _panel.SettingsRequested += OpenSettings;
        _ = _panel.Handle;

        SetupTrayIcon();
        _tooltipResetTimer.Tick += (_, _) => { _tooltipResetTimer.Stop(); _trayIcon.Text = "LiteBright"; };

        _mouseHookProc = MouseHookCallback;
        using var mod = System.Diagnostics.Process.GetCurrentProcess().MainModule!;
        _mouseHook = Native.NativeMethods.SetWindowsHookEx(
            Native.NativeMethods.WH_MOUSE_LL, _mouseHookProc,
            Native.NativeMethods.GetModuleHandle(mod.ModuleName!), 0);

        ApplyHotkeys(SettingsManager.Current);

        Helpers.StartupManager.Apply(SettingsManager.Current.StartWithWindows);

        SystemEvents_DisplaySettingsChanged(this, EventArgs.Empty);
        Microsoft.Win32.SystemEvents.DisplaySettingsChanged +=
            SystemEvents_DisplaySettingsChanged;
    }

    public static Icon LoadAppIcon(Size? size = null)
    {
        var asm    = System.Reflection.Assembly.GetExecutingAssembly();
        var stream = asm.GetManifestResourceStream("BrightnessController.public.icon.ico");
        if (stream == null) return IconHelper.CreateTrayIcon();
        var requestedSize = size ?? new Size(256, 256);
        return new Icon(stream, requestedSize);
    }

    private void SetupTrayIcon()
    {
        _trayIcon.Text            = "LiteBright";
        _trayIcon.Icon            = IconHelper.CreateTrayIcon();
        _trayIcon.Visible         = true;
        _trayIcon.MouseClick      += TrayIcon_MouseClick;
        _trayIcon.MouseDoubleClick+= (_, _) => ShowPanel();
        _trayIcon.MouseMove       += (_, _) =>
        {
            _trayIconCenter      = Cursor.Position;
            _trayIconCenterKnown = true;
        };

        BuildContextMenu();
        _trayIcon.ContextMenuStrip = _contextMenu;
    }

    private void BuildContextMenu()
    {
        _contextMenu.Dispose();

        bool light = BrightnessController.Helpers.ThemeHelper.IsLightTheme;
        Color menuBg  = light ? Color.FromArgb(249, 249, 249) : Color.FromArgb(35, 35, 35);
        Color menuFg  = light ? Color.FromArgb(20,  20,  20)  : Color.FromArgb(220, 220, 220);
        Color hover   = light ? BrightnessController.Helpers.ThemeHelper.AccentMenuHover : Color.FromArgb(60, 60, 60);
        Color border  = light ? Color.FromArgb(200, 200, 205) : Color.FromArgb(60, 60, 60);
        Color sepCol  = light ? Color.FromArgb(220, 220, 224) : Color.FromArgb(60, 60, 60);
        Color disabledFg = light ? Color.FromArgb(130, 130, 140) : Color.Gray;

        _contextMenu = new ContextMenuStrip
        {
            BackColor = menuBg,
            ForeColor = menuFg,
            Renderer  = new ThemedMenuRenderer(menuBg, hover, menuFg, border, sepCol, disabledFg),
        };

        var monitors = _monitorManager.Monitors;

        if (monitors.Count == 0)
        {
            _contextMenu.Items.Add(new ToolStripMenuItem("(No monitors detected)")
                { Enabled = false });
        }
        else
        {
            foreach (var mon in monitors)
            {
                var monItem = new ToolStripMenuItem($"☀  {mon.Name}")
                {
                    Font    = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                    Enabled = false,
                };
                _contextMenu.Items.Add(monItem);

                _contextMenu.Items.Add(new ToolStripSeparator());
            }
        }

        _contextMenu.Items.Add(new ToolStripMenuItem("⚙  Settings",
            null, (_, _) => OpenSettings()));
        _contextMenu.Items.Add(new ToolStripSeparator());
        _contextMenu.Items.Add(new ToolStripMenuItem("✕  Exit",
            null, (_, _) => ExitApplication()));

        _trayIcon.ContextMenuStrip = _contextMenu;
    }


    private void TrayIcon_MouseClick(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _lastTrayClickPosition = Cursor.Position;
            ShowPanel();
        }
    }

    private void ShowPanel()
    {
        if (_panel.Visible)
        {
            _panel.Hide();
            return;
        }
        _monitorManager.Refresh();
        _panel.ShowAtTray(_lastTrayClickPosition);
    }


    private void OnHotkeyPressed(HotkeyDefinition def)
    {
        int monitorIndex = HotkeyDefinition.MonitorIndex(def.Action);
        bool isUp        = HotkeyDefinition.IsIncrease(def.Action);
        int step         = SettingsManager.Current.BrightnessStep;

        var monitors = _monitorManager.Monitors;
        if (monitorIndex >= monitors.Count) return;

        var mon = monitors[monitorIndex];
        bool ok = _monitorManager.StepBrightness(mon, isUp ? step : -step);

        if (ok)
        {
            int pct = mon.IsInternal ? mon.Brightness : mon.BrightnessPercent;
            _trayIcon.Text = $"LiteBright\n{mon.Name}: {pct}%";
        }
    }


    private void OpenSettings()
    {
        if (_settingsForm != null && !_settingsForm.IsDisposed)
        {
            _settingsForm.Focus();
            return;
        }

        _settingsForm = new SettingsForm(_monitorManager);
        _settingsForm.SettingsSaved += settings =>
        {
            ApplyHotkeys(settings);
            BuildContextMenu();
        };
        _settingsForm.FormClosed += (_, _) => _settingsForm = null;
        _settingsForm.Show();
    }

    private void ApplyHotkeys(AppSettings settings)
    {
        var defs = settings.Hotkeys.Select(h => h.ToDefinition());
        _hotkeyManager.ApplyBindings(defs);
    }

    private void SystemEvents_DisplaySettingsChanged(object? sender, EventArgs e)
    {
        _monitorManager.Refresh();
        BuildContextMenu();
    }


    private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int msg = (int)wParam;
            var hs  = System.Runtime.InteropServices.Marshal
                      .PtrToStructure<Native.NativeMethods.MSLLHOOKSTRUCT>(lParam);

            if (msg == Native.NativeMethods.WM_MOUSEWHEEL)
            {
            
                var cur = Cursor.Position;
                bool overIcon = _trayIconCenterKnown
                    && Math.Abs(cur.X - _trayIconCenter.X) <= 20
                    && Math.Abs(cur.Y - _trayIconCenter.Y) <= 20;

                if (overIcon)
                {
                    int wheelDelta = (short)(hs.mouseData >> 16);
                    int step       = SettingsManager.Current.BrightnessStep;
                    int change     = wheelDelta > 0 ? step : -step;

                    var mons = _monitorManager.Monitors;
                    if (mons.Count > 0)
                    {
                        _monitorManager.StepBrightness(mons[0], change);
                        int  newPct = mons[0].BrightnessPercent;
                        bool inc    = change > 0;

                        string monName = mons[0].Name;
                        int parenIdx = monName.LastIndexOf(" (", StringComparison.Ordinal);
                        if (parenIdx > 0) monName = monName[..parenIdx];
                        _panel.BeginInvoke(() =>
                        {
                            _trayIcon.Text = $"LiteBright ({monName}): {newPct}%";
                            _tooltipResetTimer.Stop();
                            _tooltipResetTimer.Start();
                        });
                    }

                    return (IntPtr)1; // swallow — don't scroll other windows
                }
            }
        }
        return Native.NativeMethods.CallNextHookEx(_mouseHook, nCode, wParam, lParam);
    }

    private void ExitApplication()
    {
        _trayIcon.Visible = false;
        _panel.Hide();
        _settingsForm?.Close();
        Microsoft.Win32.SystemEvents.DisplaySettingsChanged -=
            SystemEvents_DisplaySettingsChanged;
        ExitThread();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_mouseHook != IntPtr.Zero)
            {
                Native.NativeMethods.UnhookWindowsHookEx(_mouseHook);
                _mouseHook = IntPtr.Zero;
            }
            _trayIcon.Dispose();
            _panel.Dispose();
            _tooltipResetTimer.Dispose();
            _hotkeyManager.Dispose();
            _monitorManager.Dispose();
            _contextMenu.Dispose();
        }
        base.Dispose(disposing);
    }

    private sealed class ThemedMenuRenderer : ToolStripProfessionalRenderer
    {
        private readonly Color _bg, _hover, _fg, _disabled;

        public ThemedMenuRenderer(Color bg, Color hover, Color fg, Color border, Color sep, Color disabled)
            : base(new ThemedColorTable(bg, border, sep))
        {
            _bg = bg; _hover = hover; _fg = fg; _disabled = disabled;
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = e.Item.Enabled ? _fg : _disabled;
            base.OnRenderItemText(e);
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            var rect  = new Rectangle(Point.Empty, e.Item.Size);
            var color = e.Item.Selected && e.Item.Enabled ? _hover : _bg;
            e.Graphics.FillRectangle(new SolidBrush(color), rect);
        }
    }

    private sealed class ThemedColorTable : ProfessionalColorTable
    {
        private readonly Color _bg, _border, _sep;
        public ThemedColorTable(Color bg, Color border, Color sep)
        { _bg = bg; _border = border; _sep = sep; }

        public override Color MenuBorder                   => _border;
        public override Color ToolStripDropDownBackground  => _bg;
        public override Color ImageMarginGradientBegin     => _bg;
        public override Color ImageMarginGradientMiddle    => _bg;
        public override Color ImageMarginGradientEnd       => _bg;
        public override Color SeparatorDark                => _sep;
        public override Color SeparatorLight               => _sep;
    }
}
