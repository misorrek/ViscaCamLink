namespace ViscaCamLink.Util;

using System;
using System.Globalization;
using System.Windows.Data;

public class StatusOkToBoolConverter : IValueConverter
{
    public Object Convert(Object value, Type targetType, Object parameter, CultureInfo culture)
    {
        if (value is Status status)
        {
            return status == Status.Ok;
        }
        return false;
    }

    public Object ConvertBack(Object value, Type targetType, Object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
