using System.Management;

namespace BrightnessController.Monitors;

internal static class WmiMonitorHelper
{
    private const string WmiScope            = @"root\WMI";
    private const string BrightnessClass     = "WmiMonitorBrightness";
    private const string BrightnessMethodCls = "WmiMonitorBrightnessMethods";


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
