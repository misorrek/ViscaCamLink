namespace ViscaCamLink.Util;

using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

using CameraControl.Visca;

[ValueConversion(typeof(PowerStatus), typeof(SolidColorBrush))]
public sealed class PowerStatusToBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush StatusGray = SolidColorBrushFromRgb(200, 200, 200);
    private static readonly SolidColorBrush StatusRed = SolidColorBrushFromRgb(255, 68, 68);
    private static readonly SolidColorBrush StatusYellow = SolidColorBrushFromRgb(252, 253, 100);
    private static readonly SolidColorBrush StatusGreen = SolidColorBrushFromRgb(80, 220, 72);

    public Object Convert(Object value, Type targetType, Object parameter, CultureInfo culture)
    {
        if (value is not PowerStatus status)
        {
            return StatusRed;
        }
        
        return status switch
        {
            PowerStatus.Unknown => StatusGray,
            PowerStatus.On => StatusGreen,
            PowerStatus.Standby => StatusYellow,
            PowerStatus.InternalPowerCircuitError => StatusRed,
            _ => StatusRed
        };
    }

    public Object ConvertBack(Object value, Type targetType, Object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    private static SolidColorBrush SolidColorBrushFromRgb(Byte r, Byte g, Byte b)
    {
        var color = new Color
        {
            A = 255,
            R = r,
            G = g,
            B = b,
        };
        return new SolidColorBrush(color);
    }
}
