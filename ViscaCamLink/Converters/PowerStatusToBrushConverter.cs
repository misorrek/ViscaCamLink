namespace ViscaCamLink.Converters;

using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

using ViscaCamLink.Visca.Types;

[ValueConversion(typeof(PowerStatus), typeof(SolidColorBrush))]
public class PowerStatusToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
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

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
