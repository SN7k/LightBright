using BrightnessController.Native;

namespace BrightnessController.Monitors;

internal static class DdcCiHelper
{

    public static bool TryGetBrightness(IntPtr hPhysical,
        out uint min, out uint current, out uint max)
    {
        return NativeMethods.GetMonitorBrightness(hPhysical, out min, out current, out max);
    }

    public static bool SetBrightness(IntPtr hPhysical, uint value)
    {
        return NativeMethods.SetMonitorBrightness(hPhysical, value);
    }

    public static uint PercentToRaw(int percent, uint min, uint max)
    {
        percent = Math.Clamp(percent, 0, 100);
        return min + (uint)Math.Round((max - min) * percent / 100.0);
    }

    public static int RawToPercent(uint raw, uint min, uint max)
    {
        if (max == min) return 100;
        return (int)Math.Round((raw - min) * 100.0 / (max - min));
    }


    public static bool TryGetContrast(IntPtr hPhysical,
        out uint min, out uint current, out uint max)
    {
        return NativeMethods.GetMonitorContrast(hPhysical, out min, out current, out max);
    }

    public static bool SetContrast(IntPtr hPhysical, uint value)
    {
        return NativeMethods.SetMonitorContrast(hPhysical, value);
    }

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
