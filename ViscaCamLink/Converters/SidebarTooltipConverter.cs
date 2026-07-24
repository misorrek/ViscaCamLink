namespace ViscaCamLink.Converters;

using System;
using System.Globalization;
using System.Windows.Data;

[ValueConversion(typeof(object[]), typeof(string))]
public class SidebarTooltipConverter : IMultiValueConverter
{
    private const int ExpectedValueCount = 4;

    private const int VisibilityIndex = 0;

    private const int ShowTemplateIndex = 1;

    private const int HideTemplateIndex = 2;

    private const int ControlNameIndex = 3;

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < ExpectedValueCount)
        {
            return string.Empty;
        }

        var isVisible = values[VisibilityIndex] is bool boolValue && boolValue;
        var template = isVisible
            ? values[HideTemplateIndex] as string
            : values[ShowTemplateIndex] as string;
        var controlName = values[ControlNameIndex] as string;

        if (string.IsNullOrEmpty(template) || string.IsNullOrEmpty(controlName))
        {
            return string.Empty;
        }

        return string.Format(culture, template, controlName);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}