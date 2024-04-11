namespace ViscaCamLink.Util;

using System;
using System.Windows.Data;

[ValueConversion(typeof(Boolean), typeof(Boolean))]
public sealed class InverseBooleanConverter : IValueConverter
{
    public Object Convert(Object value, Type targetType, Object parameter, System.Globalization.CultureInfo culture)
    {
        if (value is Boolean boolValue)
        {
            return !boolValue;
        }
        throw new InvalidOperationException("The target must be a boolean");
    }

    public Object ConvertBack(Object value, Type targetType, Object parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
