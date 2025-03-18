namespace ViscaCamLink.Common.Converter;

using System;
using System.Windows.Data;

[ValueConversion(typeof(Boolean), typeof(Boolean))]
public sealed class InverseBooleanConverter : IValueConverter
{
    public Object Convert(Object value, Type targetType, Object parameter, System.Globalization.CultureInfo culture)
    {
        if (targetType != typeof(Boolean))
        {
            throw new InvalidOperationException("The target must be a boolean");
        }
        return !(Boolean)value;
    }

    public Object ConvertBack(Object value, Type targetType, Object parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
