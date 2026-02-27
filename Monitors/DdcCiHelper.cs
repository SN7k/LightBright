using BrightnessController.Native;

namespace BrightnessController.Monitors;

/// <summary>
/// Controls brightness and contrast for external monitors via the DDC/CI protocol
/// (Display Data Channel / Command Interface) using the dxva2.dll Win32 API.
/// </summary>
internal static class DdcCiHelper
{
    // ── Brightness ───────────────────────────────────────────────────────────

    /// <summary>
    /// Reads minimum, current and maximum brightness.
    /// Returns false if the monitor does not support DDC/CI brightness.
    /// </summary>
    public static bool TryGetBrightness(IntPtr hPhysical,
        out uint min, out uint current, out uint max)
    {
        return NativeMethods.GetMonitorBrightness(hPhysical, out min, out current, out max);
    }

    /// <summary>Sets brightness. <paramref name="value"/> must be in the monitor's raw range.</summary>
    public static bool SetBrightness(IntPtr hPhysical, uint value)
    {
        return NativeMethods.SetMonitorBrightness(hPhysical, value);
    }

    /// <summary>Converts a 0-100 percentage to the monitor's raw DDC brightness range.</summary>
    public static uint PercentToRaw(int percent, uint min, uint max)
    {
        percent = Math.Clamp(percent, 0, 100);
        return min + (uint)Math.Round((max - min) * percent / 100.0);
    }

    /// <summary>Converts a raw DDC value back to a 0-100 percentage.</summary>
    public static int RawToPercent(uint raw, uint min, uint max)
    {
        if (max == min) return 100;
        return (int)Math.Round((raw - min) * 100.0 / (max - min));
    }

    // ── Contrast ─────────────────────────────────────────────────────────────

    public static bool TryGetContrast(IntPtr hPhysical,
        out uint min, out uint current, out uint max)
    {
        return NativeMethods.GetMonitorContrast(hPhysical, out min, out current, out max);
    }

    public static bool SetContrast(IntPtr hPhysical, uint value)
    {
        return NativeMethods.SetMonitorContrast(hPhysical, value);
    }

    // ── Physical monitor handle helpers ─────────────────────────────────────

    /// <summary>
    /// Retrieves an array of <see cref="NativeMethods.PHYSICAL_MONITOR"/> for the
    /// given HMONITOR. Returns an empty array on failure.
    /// </summary>
    public static NativeMethods.PHYSICAL_MONITOR[] GetPhysicalMonitors(IntPtr hMonitor)
    {
        if (!NativeMethods.GetNumberOfPhysicalMonitorsFromHMONITOR(hMonitor, out uint count)
            || count == 0)
            return Array.Empty<NativeMethods.PHYSICAL_MONITOR>();

        var arr = new NativeMethods.PHYSICAL_MONITOR[count];
        return NativeMethods.GetPhysicalMonitorsFromHMONITOR(hMonitor, count, arr)
            ? arr
            : Array.Empty<NativeMethods.PHYSICAL_MONITOR>();
    }
}
