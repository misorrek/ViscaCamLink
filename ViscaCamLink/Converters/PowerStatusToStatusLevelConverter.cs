namespace ViscaCamLink.Converters;

using System;
using System.Globalization;
using System.Windows.Data;

using ViscaCamLink.Visca.Types;

[ValueConversion(typeof(PowerStatus), typeof(StatusLevel))]
public class PowerStatusToStatusLevelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not PowerStatus status)
        {
            return StatusLevel.Unknown;
        }

        return status switch
        {
            PowerStatus.On => StatusLevel.Ok,
            PowerStatus.Standby => StatusLevel.Warning,
            PowerStatus.InternalPowerCircuitError => StatusLevel.Error,
            _ => StatusLevel.Unknown
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
