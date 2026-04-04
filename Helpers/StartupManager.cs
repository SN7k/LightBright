using Microsoft.Win32;

namespace BrightnessController.Helpers;

public static class StartupManager
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "BrightnessController";

    private static string ExePath => Environment.ProcessPath
        ?? System.Reflection.Assembly.GetExecutingAssembly().Location;


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
