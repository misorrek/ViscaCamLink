namespace ViscaCamLink.Converters;

using System;
using System.Globalization;
using System.Windows.Data;

using ViscaCamLink.Visca.Types;

[ValueConversion(typeof(ConnectionStatus), typeof(bool))]
public class ConnectionStatusToBoolConverter : IValueConverter
{
    public ConnectionStatus CompareValue { get; set; } = ConnectionStatus.Ok;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ConnectionStatus status)
        {
            return status == CompareValue;
        }

        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
