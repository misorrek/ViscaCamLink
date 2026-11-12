namespace ViscaCamLink.Simulator;

using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

public class BoolToColorConverter : IValueConverter
{
    public Color TrueColor { get; set; } = Colors.Green;

    public Color FalseColor { get; set; } = Colors.Red;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var color = value is true ? TrueColor : FalseColor;

        if (targetType == typeof(Brush) || targetType == typeof(SolidColorBrush))
        {
            return new SolidColorBrush(color);
        }

        return color;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
