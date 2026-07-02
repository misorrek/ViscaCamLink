namespace ViscaCamLink.Converters;

using System;
using System.Globalization;
using System.Windows.Data;
using ViscaCamLink.Visca.Types;

public class ConnectionStatusOkToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ConnectionStatus status)
        {
            return status == ConnectionStatus.Ok;
        }
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
