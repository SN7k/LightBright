using Microsoft.Win32;
using System.Drawing;

namespace BrightnessController.Helpers;

/// <summary>Reads Windows personalisation settings (theme + accent colour) from the registry.</summary>
public static class ThemeHelper
{
    /// <summary>True when Windows apps use the light theme.</summary>
    public static bool IsLightTheme
    {
        get
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                var val = key?.GetValue("AppsUseLightTheme");
                return val is int i && i == 1;
            }
            catch { return false; }
        }
    }

    /// <summary>
    /// The user's Windows accent colour (from HKCU\Software\Microsoft\Windows\DWM\AccentColor).
    /// The registry value is stored as a 32-bit ABGR integer.
    /// Falls back to a neutral blue if the key cannot be read.
    /// </summary>
    public static Color AccentColor
    {
        get
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\DWM");
                if (key?.GetValue("AccentColor") is int abgr)
                {
                    // Windows stores as 0xAABBGGRR
                    int r = (abgr >>  0) & 0xFF;
                    int g = (abgr >>  8) & 0xFF;
                    int b = (abgr >> 16) & 0xFF;
                    return Color.FromArgb(r, g, b);
                }
            }
            catch { }
            return Color.FromArgb(0, 103, 192); // Windows default blue fallback
        }
    }

    /// <summary>
    /// A lighter / higher-luminance variant of the accent colour, suitable for
    /// text and icon tinting on dark backgrounds.
    /// </summary>
    public static Color AccentColorLight
    {
        get
        {
            var c = AccentColor;
            // Blend toward white by 35% to lighten without washing out hue
            return Blend(c, Color.White, 0.35f);
        }
    }

    /// <summary>
    /// A darker variant of the accent colour, suitable for borders/hover on light backgrounds.
    /// </summary>
    public static Color AccentColorDark
    {
        get
        {
            var c = AccentColor;
            return Blend(c, Color.Black, 0.25f);
        }
    }

    /// <summary>
    /// A very light tint of the accent colour, used for hovered menu items on a white background.
    /// </summary>
    public static Color AccentMenuHover
        => Blend(AccentColor, Color.White, 0.80f);

    private static Color Blend(Color a, Color b, float t)
        => Color.FromArgb(
            Math.Clamp((int)(a.R + (b.R - a.R) * t), 0, 255),
            Math.Clamp((int)(a.G + (b.G - a.G) * t), 0, 255),
            Math.Clamp((int)(a.B + (b.B - a.B) * t), 0, 255));
}
