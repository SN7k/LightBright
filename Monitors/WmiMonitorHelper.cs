using System.Management;

namespace BrightnessController.Monitors;

/// <summary>
/// Controls brightness for the laptop's built-in display via the WMI
/// WmiMonitorBrightness / WmiMonitorBrightnessMethods classes.
/// Only works for panels driven by the laptop driver stack.
/// </summary>
internal static class WmiMonitorHelper
{
    private const string WmiScope            = @"root\WMI";
    private const string BrightnessClass     = "WmiMonitorBrightness";
    private const string BrightnessMethodCls = "WmiMonitorBrightnessMethods";

    // ── Read ─────────────────────────────────────────────────────────────────

    /// <summary>Returns current brightness (0-100). Returns -1 on failure.</summary>
    public static int GetBrightness()
    {
        try
        {
            using var mc = new ManagementClass(WmiScope, BrightnessClass, null);
            foreach (ManagementObject mo in mc.GetInstances())
            {
                using (mo)
                {
                    var val = mo["CurrentBrightness"];
                    if (val != null)
                        return Convert.ToInt32(val);
                }
            }
        }
        catch { /* WMI not available (desktop PC) */ }
        return -1;
    }

    // ── Write ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Sets brightness (0-100).
    /// <paramref name="timeout"/> is the persistence timeout in seconds; 0 = immediate permanent.
    /// </summary>
    public static bool SetBrightness(int value, uint timeout = 1)
    {
        value = Math.Clamp(value, 0, 100);
        try
        {
            using var mc = new ManagementClass(WmiScope, BrightnessMethodCls, null);
            foreach (ManagementObject mo in mc.GetInstances())
            {
                using (mo)
                {
                    // WmiSetBrightness(Timeout, Brightness)
                    mo.InvokeMethod("WmiSetBrightness", new object[] { timeout, (byte)value });
                    return true;
                }
            }
        }
        catch { /* WMI not available */ }
        return false;
    }

    // ── Availability check ───────────────────────────────────────────────────

    /// <summary>Returns true if a WMI-controlled internal display is present.</summary>
    public static bool IsAvailable()
    {
        try
        {
            using var mc = new ManagementClass(WmiScope, BrightnessClass, null);
            foreach (ManagementObject mo in mc.GetInstances())
            {
                using (mo) return true;
            }
        }
        catch { }
        return false;
    }
}
