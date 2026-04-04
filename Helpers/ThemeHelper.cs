using Microsoft.Win32;
using System.Drawing;

namespace BrightnessController.Helpers;

public static class ThemeHelper
{
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
                    int r = (abgr >>  0) & 0xFF;
                    int g = (abgr >>  8) & 0xFF;
                    int b = (abgr >> 16) & 0xFF;
                    return Color.FromArgb(r, g, b);
                }
            }
            catch { }
            return Color.FromArgb(0, 103, 192);
        }
    }

    public static Color AccentColorLight
    {
        get
        {
            var c = AccentColor;
            return Blend(c, Color.White, 0.35f);
        }
    }

    public static Color AccentColorDark
    {
        get
        {
            var c = AccentColor;
            return Blend(c, Color.Black, 0.25f);
        }
    }

    public static Color AccentMenuHover
        => Blend(AccentColor, Color.White, 0.80f);

    private static Color Blend(Color a, Color b, float t)
        => Color.FromArgb(
            Math.Clamp((int)(a.R + (b.R - a.R) * t), 0, 255),
            Math.Clamp((int)(a.G + (b.G - a.G) * t), 0, 255),
            Math.Clamp((int)(a.B + (b.B - a.B) * t), 0, 255));
}
