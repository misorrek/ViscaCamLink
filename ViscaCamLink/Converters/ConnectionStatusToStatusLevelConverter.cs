namespace ViscaCamLink.Converters;

using System;
using System.Globalization;
using System.Windows.Data;

using ViscaCamLink.Visca.Types;

[ValueConversion(typeof(ConnectionStatus), typeof(StatusLevel))]
public class ConnectionStatusToStatusLevelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not ConnectionStatus status)
        {
            return StatusLevel.Unknown;
        }

        return status switch
        {
            ConnectionStatus.Failed => StatusLevel.Error,
            ConnectionStatus.Working => StatusLevel.Warning,
            ConnectionStatus.Ok => StatusLevel.Ok,
            _ => StatusLevel.Unknown
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
