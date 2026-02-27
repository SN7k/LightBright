using BrightnessController.Native;

namespace BrightnessController.Hotkeys;

/// <summary>
/// Registers and unregisters global hotkeys using a message-only hidden window.
/// Fires <see cref="HotkeyPressed"/> on the UI thread via the supplied
/// <see cref="SynchronizationContext"/>.
/// </summary>
public sealed class HotkeyManager : IDisposable
{
    // ── Events ────────────────────────────────────────────────────────────────

    /// <summary>Fired when a registered hotkey is triggered. Always on the UI thread.</summary>
    public event Action<HotkeyDefinition>? HotkeyPressed;

    // ── State ─────────────────────────────────────────────────────────────────

    private readonly HotkeyWindow           _window;
    private readonly SynchronizationContext _uiContext;
    private readonly Dictionary<int, HotkeyDefinition> _registered = new();
    private bool _disposed;

    // ── Construction ──────────────────────────────────────────────────────────

    public HotkeyManager()
    {
        _uiContext = SynchronizationContext.Current
            ?? throw new InvalidOperationException(
                "HotkeyManager must be created on the UI thread.");

        _window = new HotkeyWindow();
        _window.HotkeyReceived += OnHotkeyReceived;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Registers a hotkey. Returns false if the combination is already taken.</summary>
    public bool Register(HotkeyDefinition def)
    {
        if (!def.IsValid) return false;

        // Unregister first in case the binding changed.
        Unregister(def.Action);

        bool ok = NativeMethods.RegisterHotKey(
            _window.Handle,
            def.Id,
            (uint)def.Modifiers | NativeMethods.MOD_NOREPEAT,
            (uint)def.Key);

        if (ok)
            _registered[def.Id] = def;

        return ok;
    }

    /// <summary>Unregisters the hotkey bound to <paramref name="action"/>.</summary>
    public void Unregister(HotkeyAction action)
    {
        int id = (int)action + 1000;
        if (_registered.ContainsKey(id))
        {
            NativeMethods.UnregisterHotKey(_window.Handle, id);
            _registered.Remove(id);
        }
    }

    /// <summary>Replaces all registered hotkeys with <paramref name="definitions"/>.</summary>
    public void ApplyBindings(IEnumerable<HotkeyDefinition> definitions)
    {
        UnregisterAll();
        foreach (var def in definitions)
            Register(def);
    }

    /// <summary>Unregisters every currently registered hotkey.</summary>
    public void UnregisterAll()
    {
        foreach (var id in _registered.Keys.ToList())
            NativeMethods.UnregisterHotKey(_window.Handle, id);
        _registered.Clear();
    }

    // ── Private ───────────────────────────────────────────────────────────────

    private void OnHotkeyReceived(int id)
    {
        if (!_registered.TryGetValue(id, out var def)) return;

        _uiContext.Post(_ => HotkeyPressed?.Invoke(def), null);
    }

    // ── Dispose ───────────────────────────────────────────────────────────────

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        UnregisterAll();
        _window.DestroyHandle();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Inner helper: message-only window that catches WM_HOTKEY
    // ─────────────────────────────────────────────────────────────────────────

    private sealed class HotkeyWindow : NativeWindow
    {
        public event Action<int>? HotkeyReceived;

        public HotkeyWindow()
        {
            var cp = new CreateParams
            {
                // HWND_MESSAGE — message-only window, never visible
                Parent = new IntPtr(-3)
            };
            CreateHandle(cp);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == NativeMethods.WM_HOTKEY)
            {
                int id = m.WParam.ToInt32();
                HotkeyReceived?.Invoke(id);
                return;
            }
            base.WndProc(ref m);
        }
    }
}
