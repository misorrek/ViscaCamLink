namespace ViscaCamLink.Converters;

using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

using ViscaCamLink.Visca.Types;

[ValueConversion(typeof(ConnectionStatus), typeof(SolidColorBrush))]
public class ConnectionStatusToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not ConnectionStatus status)
        {
            return StatusBrushConstants.StatusGray;
        }

        return status switch
        {
            ConnectionStatus.Failed => StatusBrushConstants.StatusRed,
            ConnectionStatus.Working => StatusBrushConstants.StatusYellow,
            ConnectionStatus.Ok => StatusBrushConstants.StatusGreen,
            _ => StatusBrushConstants.StatusGray
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
