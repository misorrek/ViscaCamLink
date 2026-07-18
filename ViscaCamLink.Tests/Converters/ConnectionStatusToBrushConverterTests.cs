namespace ViscaCamLink.Tests.Converters;

using System;
using System.Globalization;
using System.Windows.Media;

using Shouldly;

using ViscaCamLink.Converters;
using ViscaCamLink.Visca.Types;

using Xunit;

public sealed class ConnectionStatusToBrushConverterTests
{
    private readonly ConnectionStatusToBrushConverter _converter = new();

    [Fact]
    public void Convert_Success()
    {
        Convert(ConnectionStatus.Failed).ShouldBeSameAs(StatusBrushConstants.StatusRed);
        Convert(ConnectionStatus.Working).ShouldBeSameAs(StatusBrushConstants.StatusYellow);
        Convert(ConnectionStatus.Ok).ShouldBeSameAs(StatusBrushConstants.StatusGreen);
    }

    [Fact]
    public void Convert_WhenValueIsNotConnectionStatus_ReturnsGray()
    {
        Convert("not a status").ShouldBeSameAs(StatusBrushConstants.StatusGray);
    }

    [Fact]
    public void ConvertBack_ThrowsNotSupportedException()
    {
        void act() => _converter.ConvertBack(StatusBrushConstants.StatusGreen, typeof(ConnectionStatus), null!, CultureInfo.InvariantCulture);

        Should.Throw<NotSupportedException>(act);
    }

    private object Convert(object value)
    {
        return _converter.Convert(value, typeof(SolidColorBrush), null!, CultureInfo.InvariantCulture);
    }
}
