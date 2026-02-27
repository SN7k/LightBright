using Microsoft.Win32;

namespace BrightnessController.Helpers;

/// <summary>
/// Manages the "run at Windows startup" Registry entry under
/// HKCU\Software\Microsoft\Windows\CurrentVersion\Run.
/// Uses HKCU (no admin rights required).
/// </summary>
public static class StartupManager
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "BrightnessController";

    private static string ExePath => Environment.ProcessPath
        ?? System.Reflection.Assembly.GetExecutingAssembly().Location;

    // ── Read ─────────────────────────────────────────────────────────────────

    public static bool IsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: false);
            return key?.GetValue(ValueName) is string existing
                && existing.Equals($"\"{ExePath}\"", StringComparison.OrdinalIgnoreCase);
        }
        catch { return false; }
    }

    // ── Write ────────────────────────────────────────────────────────────────

    public static void Enable()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
            key?.SetValue(ValueName, $"\"{ExePath}\"");
        }
        catch { }
    }

    public static void Disable()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
            key?.DeleteValue(ValueName, throwOnMissingValue: false);
        }
        catch { }
    }

    public static void Apply(bool enable)
    {
        if (enable) Enable();
        else        Disable();
    }
}
