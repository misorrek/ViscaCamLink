namespace ViscaCamLink.Converters;

using System.Windows.Media;

public static class StatusBrushConstants
{
    public static readonly SolidColorBrush StatusGray = SolidColorBrushFromRgb(200, 200, 200);
    public static readonly SolidColorBrush StatusRed = SolidColorBrushFromRgb(255, 80, 80);
    public static readonly SolidColorBrush StatusYellow = SolidColorBrushFromRgb(255, 235, 80);
    public static readonly SolidColorBrush StatusGreen = SolidColorBrushFromRgb(80, 255, 80);

    private static SolidColorBrush SolidColorBrushFromRgb(byte red, byte green, byte blue)
    {
        var color = new Color
        {
            A = byte.MaxValue,
            R = red,
            G = green,
            B = blue,
        };

        return new SolidColorBrush(color);
    }
}
