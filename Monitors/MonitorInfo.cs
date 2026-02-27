namespace BrightnessController.Monitors;

/// <summary>Represents a single physical display with its current brightness/contrast state.</summary>
public sealed class MonitorInfo : IDisposable
{
    // ── Identity ─────────────────────────────────────────────────────────────
    /// <summary>Friendly display name, e.g. "Generic PnP Monitor (DISPLAY1)".</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>GDI device name such as \\.\DISPLAY1.</summary>
    public string DeviceName { get; init; } = string.Empty;

    /// <summary>Sequential index (0-based) used for hotkey IDs and settings keys.</summary>
    public int Index { get; init; }

    /// <summary>True when this is the laptop's built-in display (WMI-controlled).</summary>
    public bool IsInternal { get; init; }

    // ── State ────────────────────────────────────────────────────────────────
    public int Brightness    { get; set; } = 100;
    public int MinBrightness { get; set; } = 0;
    public int MaxBrightness { get; set; } = 100;

    public int Contrast      { get; set; } = 50;
    public int MinContrast   { get; set; } = 0;
    public int MaxContrast   { get; set; } = 100;

    // ── DDC/CI handle (null for WMI-controlled internal monitors) ────────────
    public IntPtr PhysicalHandle { get; set; } = IntPtr.Zero;

    // ── Backing physical monitor array (kept for DestroyPhysicalMonitors) ────
    internal Native.NativeMethods.PHYSICAL_MONITOR[]? PhysicalMonitorArray { get; set; }

    /// <summary>Normalised brightness as a 0–100 percentage regardless of raw DDC range.</summary>
    public int BrightnessPercent =>
        MaxBrightness > MinBrightness
            ? (int)Math.Round((Brightness - MinBrightness) * 100.0 / (MaxBrightness - MinBrightness))
            : Brightness;

    /// <summary>Normalised contrast as a 0–100 percentage regardless of raw DDC range.</summary>
    public int ContrastPercent =>
        MaxContrast > MinContrast
            ? (int)Math.Round((Contrast - MinContrast) * 100.0 / (MaxContrast - MinContrast))
            : Contrast;

    public override string ToString() => Name;

    // ─────────────────────────────────────────────────────────────────────────
    public void Dispose()
    {
        if (PhysicalMonitorArray is { Length: > 0 })
        {
            Native.NativeMethods.DestroyPhysicalMonitors(
                (uint)PhysicalMonitorArray.Length, PhysicalMonitorArray);
            PhysicalMonitorArray = null;
        }
    }
}
