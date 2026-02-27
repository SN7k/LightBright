using BrightnessController.Helpers;
using BrightnessController.Hotkeys;
using BrightnessController.Monitors;
using BrightnessController.Settings;
using BrightnessController.UI;

namespace BrightnessController;

/// <summary>
/// Central application context.  Owns the NotifyIcon, MonitorManager,
/// HotkeyManager and BrightnessPanel.  No main window ever appears.
/// </summary>
public sealed class TrayApplicationContext : ApplicationContext
{
    // ── Core services ─────────────────────────────────────────────────────────
    private readonly MonitorManager _monitorManager = new();
    private readonly HotkeyManager  _hotkeyManager;
    private readonly NotifyIcon     _trayIcon       = new();
    private readonly BrightnessPanel _panel;

    // ── Per-monitor context-menu items (refreshed when monitors change) ───────
    private ContextMenuStrip _contextMenu = new();

    // ── Singleton settings form ───────────────────────────────────────────────
    private SettingsForm? _settingsForm;

    // ── Tray icon click tracking ──────────────────────────────────────────────
    private Point _lastTrayClickPosition;

    // ── Tray scroll (low-level mouse hook) ────────────────────────────────────
    private Native.NativeMethods.LowLevelMouseProc? _mouseHookProc;  // keep delegate alive
    private IntPtr _mouseHook = IntPtr.Zero;
    // Center of the tray icon on screen — updated by NotifyIcon.MouseMove.
    // Used to verify the cursor is actually over the icon when a scroll arrives.
    private Point _trayIconCenter;
    private bool  _trayIconCenterKnown;

    // ── Tooltip auto-reset after scroll ──────────────────────────────────────
    private readonly System.Windows.Forms.Timer _tooltipResetTimer = new() { Interval = 2000 };

    // ─────────────────────────────────────────────────────────────────────────

    public TrayApplicationContext()
    {
        // Must construct HotkeyManager on UI thread (after message loop starts).
        _hotkeyManager = new HotkeyManager();
        _hotkeyManager.HotkeyPressed += OnHotkeyPressed;

        // Refresh monitor list on startup.
        _monitorManager.Refresh();

        // Panel (lazy-built sliders; reuses form instance for speed).
        _panel = new BrightnessPanel(_monitorManager);
        _panel.SettingsRequested += OpenSettings;
        _ = _panel.Handle; // Force Win32 HWND now — required for BeginInvoke from hook thread

        // Tray icon
        SetupTrayIcon();
        _tooltipResetTimer.Tick += (_, _) => { _tooltipResetTimer.Stop(); _trayIcon.Text = "LiteBright"; };

        // Low-level mouse hook for scroll-over-tray-icon
        _mouseHookProc = MouseHookCallback;
        using var mod = System.Diagnostics.Process.GetCurrentProcess().MainModule!;
        _mouseHook = Native.NativeMethods.SetWindowsHookEx(
            Native.NativeMethods.WH_MOUSE_LL, _mouseHookProc,
            Native.NativeMethods.GetModuleHandle(mod.ModuleName!), 0);

        // Apply saved hotkeys
        ApplyHotkeys(SettingsManager.Current);

        // Ensure startup registry is consistent with saved setting
        Helpers.StartupManager.Apply(SettingsManager.Current.StartWithWindows);

        // Listen for display configuration changes (monitor plugged/unplugged).
        SystemEvents_DisplaySettingsChanged(this, EventArgs.Empty);
        Microsoft.Win32.SystemEvents.DisplaySettingsChanged +=
            SystemEvents_DisplaySettingsChanged;
    }

    // ── Tray icon setup ───────────────────────────────────────────────────────

    // Load the embedded icon.ico at the specified size (used for app/settings window icon).
    // Requests 256×256 — the maximum size Windows renders for taskbar/Alt+Tab/Explorer.
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
            // Record the icon's screen position every time the cursor is over it.
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

    // ── Tray click handler ────────────────────────────────────────────────────

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

    // ── Hotkey handler ────────────────────────────────────────────────────────

    private void OnHotkeyPressed(HotkeyDefinition def)
    {
        int monitorIndex = HotkeyDefinition.MonitorIndex(def.Action);
        bool isUp        = HotkeyDefinition.IsIncrease(def.Action);
        int step         = SettingsManager.Current.BrightnessStep;

        var monitors = _monitorManager.Monitors;
        if (monitorIndex >= monitors.Count) return;

        var mon = monitors[monitorIndex];
        bool ok = _monitorManager.StepBrightness(mon, isUp ? step : -step);

        // Update tray tooltip when brightness changes via hotkey
        if (ok)
        {
            int pct = mon.IsInternal ? mon.Brightness : mon.BrightnessPercent;
            _trayIcon.Text = $"LiteBright\n{mon.Name}: {pct}%";
        }
    }

    // ── Settings ──────────────────────────────────────────────────────────────

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

    // ── Display change ────────────────────────────────────────────────────────

    private void SystemEvents_DisplaySettingsChanged(object? sender, EventArgs e)
    {
        _monitorManager.Refresh();
        BuildContextMenu();
    }

    // ── Exit ──────────────────────────────────────────────────────────────────

    // ── Tray mouse-wheel hook callback ────────────────────────────────────────

    private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int msg = (int)wParam;
            var hs  = System.Runtime.InteropServices.Marshal
                      .PtrToStructure<Native.NativeMethods.MSLLHOOKSTRUCT>(lParam);

            if (msg == Native.NativeMethods.WM_MOUSEWHEEL)
            {
                // Only act when the cursor is currently inside the tray icon's
                // bounding box.  We use the last-known icon center (recorded by
                // NotifyIcon.MouseMove) plus a ±20 px tolerance to cover the
                // icon area.  This means scrolling anywhere else on the taskbar
                // — even right after clicking the icon — has no effect.
                var cur = Cursor.Position;
                bool overIcon = _trayIconCenterKnown
                    && Math.Abs(cur.X - _trayIconCenter.X) <= 20
                    && Math.Abs(cur.Y - _trayIconCenter.Y) <= 20;

                if (overIcon)
                {
                    // hi-word: positive = scroll up (increase), negative = down
                    int wheelDelta = (short)(hs.mouseData >> 16);
                    int step       = SettingsManager.Current.BrightnessStep;
                    int change     = wheelDelta > 0 ? step : -step;

                    var mons = _monitorManager.Monitors;
                    if (mons.Count > 0)
                    {
                        _monitorManager.StepBrightness(mons[0], change);
                        int  newPct = mons[0].BrightnessPercent;
                        bool inc    = change > 0;

                        // Update the tray icon tooltip — visible while the user hovers
                        string monName = mons[0].Name;
                        // Strip trailing " (\\.\ DISPLAYx)" device path if present
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

    // ── Dispose ───────────────────────────────────────────────────────────────

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

    // ─────────────────────────────────────────────────────────────────────────
    // Dark theme renderer for context menus
    // ─────────────────────────────────────────────────────────────────────────

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
