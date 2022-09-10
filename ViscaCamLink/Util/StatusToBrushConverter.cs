namespace ViscaCamLink.Util
{
    using System;
    using System.Globalization;
    using System.Windows.Data;
    using System.Windows.Media;

    [ValueConversion(typeof(Status), typeof(SolidColorBrush))]
    public sealed class StatusToBrushConverter : IValueConverter
    {
        private static readonly SolidColorBrush StatusRed = SolidColorBrushFromRgb(255, 68, 68);
        private static readonly SolidColorBrush StatusYellow = SolidColorBrushFromRgb(252, 253, 100);
        private static readonly SolidColorBrush StatusGreen = SolidColorBrushFromRgb(80, 220, 72);

        public Object Convert(Object value, Type targetType, Object parameter, CultureInfo culture)
        {
            if (value is not Status status)
            {
                return StatusRed;
            }
            
            return status switch
            {
                Status.Failed => StatusRed,
                Status.Working => StatusYellow,
                Status.Ok => StatusGreen,
                _ => StatusRed
            };
        }

        public Object ConvertBack(Object value, Type targetType, Object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        private static SolidColorBrush SolidColorBrushFromRgb(Byte r, Byte g, Byte b)
        {
            var color = new Color
            {
                A = 255,
                R = r,
                G = g,
                B = b,
            };
            return new SolidColorBrush(color);
        }
    }
}
