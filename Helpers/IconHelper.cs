namespace BrightnessController.Helpers;

/// <summary>
/// Generates tray icons at runtime.
/// Draws a white sun symbol — always visible on Windows 11's dark taskbar.
/// </summary>
public static class IconHelper
{
    /// <summary>
    /// Creates a 32×32 white sun icon suitable for the system tray.
    /// </summary>
    public static Icon CreateTrayIcon()
    {
        const int size  = 32;
        const float cx  = size / 2f;
        const float cy  = size / 2f;

        using var bmp = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using var g   = Graphics.FromImage(bmp);

        g.Clear(Color.Transparent);
        g.SmoothingMode      = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.PixelOffsetMode    = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
        g.InterpolationMode  = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

        var white = Color.White;
        using var fill = new SolidBrush(white);
        using var pen  = new Pen(white, 2.2f) { StartCap = System.Drawing.Drawing2D.LineCap.Round,
                                                EndCap   = System.Drawing.Drawing2D.LineCap.Round };

        // Core circle
        float coreR = 6.5f;
        g.FillEllipse(fill, cx - coreR, cy - coreR, coreR * 2, coreR * 2);

        // 8 rays
        float innerR = coreR + 2.5f;
        float outerR = cx - 1.5f;
        for (int i = 0; i < 8; i++)
        {
            double angle = i * Math.PI / 4;
            float x1 = cx + (float)(Math.Cos(angle) * innerR);
            float y1 = cy + (float)(Math.Sin(angle) * innerR);
            float x2 = cx + (float)(Math.Cos(angle) * outerR);
            float y2 = cy + (float)(Math.Sin(angle) * outerR);
            g.DrawLine(pen, x1, y1, x2, y2);
        }

        return Icon.FromHandle(bmp.GetHicon());
    }
}
