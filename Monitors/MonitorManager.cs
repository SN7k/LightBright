using System.Runtime.InteropServices;
using BrightnessController.Native;

namespace BrightnessController.Monitors;

/// <summary>
/// Enumerates physical displays and provides unified brightness / contrast
/// read-write access regardless of whether the panel is WMI-controlled (internal)
/// or DDC/CI-controlled (external).
/// </summary>
public sealed class MonitorManager : IDisposable
{
    private List<MonitorInfo> _monitors = new();
    private bool _disposed;

    // Expose the current snapshot; callers must call Refresh() first.
    public IReadOnlyList<MonitorInfo> Monitors => _monitors;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    public MonitorManager() { }

    /// <summary>
    /// (Re-)enumerates all connected displays and reads their current
    /// brightness / contrast values. Call once at startup and whenever the
    /// display configuration changes (WM_DISPLAYCHANGE).
    /// </summary>
    public void Refresh()
    {
        // Release existing DDC/CI handles before re-enumerating.
        DisposeMonitors();

        var newList = new List<MonitorInfo>();
        bool wmiAvailable = WmiMonitorHelper.IsAvailable();
        int index = 0;

        // ── Walk all HMONITORs ───────────────────────────────────────────────
        NativeMethods.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero,
            (hMonitor, _, ref _, _) =>
            {
                var info = BuildMonitorInfo(hMonitor, index, wmiAvailable);
                if (info != null)
                {
                    newList.Add(info);
                    index++;
                }
                return true;
            }, IntPtr.Zero);

        _monitors = newList;
    }

    // ── Brightness ───────────────────────────────────────────────────────────

    /// <summary>Sets brightness as a 0-100 percentage for the given monitor.</summary>
    public bool SetBrightness(MonitorInfo mon, int percent)
    {
        percent = Math.Clamp(percent, 0, 100);

        if (mon.IsInternal)
        {
            bool ok = WmiMonitorHelper.SetBrightness(percent);
            if (ok) mon.Brightness = percent;
            return ok;
        }
        else
        {
            uint raw = DdcCiHelper.PercentToRaw(percent,
                (uint)mon.MinBrightness, (uint)mon.MaxBrightness);
            bool ok = DdcCiHelper.SetBrightness(mon.PhysicalHandle, raw);
            if (ok) mon.Brightness = (int)raw;
            return ok;
        }
    }

    /// <summary>Steps brightness up (+) or down (-) by <paramref name="stepPercent"/> points.</summary>
    public bool StepBrightness(MonitorInfo mon, int stepPercent)
    {
        int current = mon.IsInternal
            ? mon.Brightness
            : mon.BrightnessPercent;
        return SetBrightness(mon, current + stepPercent);
    }

    // ── Contrast ─────────────────────────────────────────────────────────────

    /// <summary>Sets contrast as a 0-100 percentage for the given monitor.</summary>
    public bool SetContrast(MonitorInfo mon, int percent)
    {
        if (mon.IsInternal) return false; // WMI has no contrast API

        percent = Math.Clamp(percent, 0, 100);
        uint raw = DdcCiHelper.PercentToRaw(percent,
            (uint)mon.MinContrast, (uint)mon.MaxContrast);
        bool ok = DdcCiHelper.SetContrast(mon.PhysicalHandle, raw);
        if (ok) mon.Contrast = (int)raw;
        return ok;
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static MonitorInfo? BuildMonitorInfo(IntPtr hMonitor, int index, bool wmiAvailable)
    {
        // Retrieve GDI monitor info (device name, flags).
        var mi = new NativeMethods.MONITORINFOEX
        {
            cbSize = (uint)Marshal.SizeOf<NativeMethods.MONITORINFOEX>()
        };
        if (!NativeMethods.GetMonitorInfo(hMonitor, ref mi))
            return null;

        bool isPrimary = (mi.dwFlags & NativeMethods.MONITORINFOF_PRIMARY) != 0;
        bool isInternal = isPrimary && wmiAvailable;

        if (isInternal)
        {
            // Internal / laptop display — controlled via WMI
            int brightness = WmiMonitorHelper.GetBrightness();
            if (brightness < 0) brightness = 100;

            return new MonitorInfo
            {
                Index         = index,
                Name          = $"Built-in Display ({mi.szDevice.TrimEnd('\0')})",
                DeviceName    = mi.szDevice.TrimEnd('\0'),
                IsInternal    = true,
                Brightness    = brightness,
                MinBrightness = 0,
                MaxBrightness = 100,
                Contrast      = 50,
                MinContrast   = 0,
                MaxContrast   = 100,
            };
        }
        else
        {
            // External display — controlled via DDC/CI
            var physArr = DdcCiHelper.GetPhysicalMonitors(hMonitor);
            if (physArr.Length == 0)
            {
                // DDC/CI not supported; create a placeholder that reports failure gracefully.
                return new MonitorInfo
                {
                    Index         = index,
                    Name          = $"Monitor {index + 1} ({mi.szDevice.TrimEnd('\0')}) [DDC/CI N/A]",
                    DeviceName    = mi.szDevice.TrimEnd('\0'),
                    IsInternal    = false,
                    Brightness    = 100,
                    MinBrightness = 0,
                    MaxBrightness = 100,
                };
            }

            IntPtr hPhysical = physArr[0].hPhysicalMonitor;
            string monName   = $"{physArr[0].szPhysicalMonitorDescription.TrimEnd('\0')} ({mi.szDevice.TrimEnd('\0')})";

            // Brightness
            uint min = 0, cur = 75, max = 100;
            DdcCiHelper.TryGetBrightness(hPhysical, out min, out cur, out max);

            // Contrast
            uint cMin = 0, cCur = 50, cMax = 100;
            DdcCiHelper.TryGetContrast(hPhysical, out cMin, out cCur, out cMax);

            return new MonitorInfo
            {
                Index                = index,
                Name                 = monName,
                DeviceName           = mi.szDevice.TrimEnd('\0'),
                IsInternal           = false,
                PhysicalHandle       = hPhysical,
                PhysicalMonitorArray = physArr,
                Brightness           = (int)cur,
                MinBrightness        = (int)min,
                MaxBrightness        = (int)max,
                Contrast             = (int)cCur,
                MinContrast          = (int)cMin,
                MaxContrast          = (int)cMax,
            };
        }
    }

    // ── Dispose ───────────────────────────────────────────────────────────────

    private void DisposeMonitors()
    {
        foreach (var m in _monitors)
            m.Dispose();
        _monitors.Clear();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        DisposeMonitors();
    }
}
