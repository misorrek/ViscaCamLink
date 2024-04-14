namespace ViscaCamLink.Util;

using System;
using System.Globalization;
using System.Windows.Data;

public class ConnectionStatusOkToBoolConverter : IValueConverter
{
    public Object Convert(Object value, Type targetType, Object parameter, CultureInfo culture)
    {
        if (value is ConnectionStatus status)
        {
            return status == ConnectionStatus.Ok;
        }
        return false;
    }

    public Object ConvertBack(Object value, Type targetType, Object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
