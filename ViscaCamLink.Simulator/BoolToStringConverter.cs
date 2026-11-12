namespace ViscaCamLink.Simulator;

using System;
using System.Globalization;
using System.Windows.Data;

public class BoolToStringConverter : IValueConverter
{
    public string TrueString { get; set; } = "True";

    public string FalseString { get; set; } = "False";

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? TrueString : FalseString;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
