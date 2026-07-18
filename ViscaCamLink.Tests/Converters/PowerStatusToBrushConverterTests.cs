namespace ViscaCamLink.Tests.Converters;

using System;
using System.Globalization;
using System.Windows.Media;

using Shouldly;

using ViscaCamLink.Converters;
using ViscaCamLink.Visca.Types;

using Xunit;

public sealed class PowerStatusToBrushConverterTests
{
    private readonly PowerStatusToBrushConverter _converter = new();

    [Fact]
    public void Convert_Success()
    {
        Convert(PowerStatus.Unknown).ShouldBeSameAs(StatusBrushConstants.StatusGray);
        Convert(PowerStatus.On).ShouldBeSameAs(StatusBrushConstants.StatusGreen);
        Convert(PowerStatus.Standby).ShouldBeSameAs(StatusBrushConstants.StatusYellow);
        Convert(PowerStatus.InternalPowerCircuitError).ShouldBeSameAs(StatusBrushConstants.StatusRed);
    }

    [Fact]
    public void Convert_WhenValueIsNotPowerStatus_ReturnsGray()
    {
        Convert("not a status").ShouldBeSameAs(StatusBrushConstants.StatusGray);
    }

    [Fact]
    public void ConvertBack_ThrowsNotSupportedException()
    {
        void act() => _converter.ConvertBack(StatusBrushConstants.StatusGreen, typeof(PowerStatus), null!, CultureInfo.InvariantCulture);

        Should.Throw<NotSupportedException>(act);
    }

    private object Convert(object value)
    {
        return _converter.Convert(value, typeof(SolidColorBrush), null!, CultureInfo.InvariantCulture);
    }
}
