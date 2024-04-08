namespace ViscaCamLink.Util;

using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

using CameraControl.Visca;

public static class StatusBrushConstants
{
    public static readonly SolidColorBrush StatusGray = SolidColorBrushFromRgb(200, 200, 200);
    public static readonly SolidColorBrush StatusRed = SolidColorBrushFromRgb(255, 80, 80);
    public static readonly SolidColorBrush StatusYellow = SolidColorBrushFromRgb(255, 235, 80);
    public static readonly SolidColorBrush StatusGreen = SolidColorBrushFromRgb(80, 255, 80);

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

[ValueConversion(typeof(ConnectionStatus), typeof(SolidColorBrush))]
public sealed class ConnectionStatusToBrushConverter : IValueConverter
{
    public Object Convert(Object value, Type targetType, Object parameter, CultureInfo culture)
    {
        if (value is not ConnectionStatus status)
        {
            return StatusBrushConstants.StatusGray;
        }
        
        return status switch
        {
            ConnectionStatus.Failed => StatusBrushConstants.StatusRed,
            ConnectionStatus.Working => StatusBrushConstants.StatusYellow,
            ConnectionStatus.Ok => StatusBrushConstants.StatusGreen,
            _ => StatusBrushConstants.StatusGray
        };
    }

    public Object ConvertBack(Object value, Type targetType, Object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

[ValueConversion(typeof(PowerStatus), typeof(SolidColorBrush))]
public sealed class PowerStatusToBrushConverter : IValueConverter
{
    public Object Convert(Object value, Type targetType, Object parameter, CultureInfo culture)
    {
        if (value is not PowerStatus status)
        {
            return StatusBrushConstants.StatusGray;
        }

        return status switch
        {
            PowerStatus.Unknown => StatusBrushConstants.StatusGray,
            PowerStatus.On => StatusBrushConstants.StatusGreen,
            PowerStatus.Standby => StatusBrushConstants.StatusYellow,
            PowerStatus.InternalPowerCircuitError => StatusBrushConstants.StatusRed,
            _ => StatusBrushConstants.StatusGray
        };
    }

    public Object ConvertBack(Object value, Type targetType, Object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
