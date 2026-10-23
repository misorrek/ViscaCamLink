namespace ViscaCamLink.Tests.Converters;

using System;
using System.Globalization;

using Shouldly;

using ViscaCamLink.Converters;
using ViscaCamLink.Visca.Types;

using Xunit;

public sealed class PowerStatusToStatusLevelConverterTests
{
    private readonly PowerStatusToStatusLevelConverter _converter = new();

    [Fact]
    public void Convert_Success()
    {
        Convert(PowerStatus.Unknown).ShouldBe(StatusLevel.Unknown);
        Convert(PowerStatus.On).ShouldBe(StatusLevel.Ok);
        Convert(PowerStatus.Standby).ShouldBe(StatusLevel.Warning);
        Convert(PowerStatus.InternalPowerCircuitError).ShouldBe(StatusLevel.Error);
    }

    [Fact]
    public void Convert_WhenValueIsNotPowerStatus_ReturnsUnknown()
    {
        Convert("not a status").ShouldBe(StatusLevel.Unknown);
    }

    [Fact]
    public void ConvertBack_ThrowsNotSupportedException()
    {
        void act() => _converter.ConvertBack(StatusLevel.Ok, typeof(PowerStatus), null!, CultureInfo.InvariantCulture);

        Should.Throw<NotSupportedException>(act);
    }

    private object Convert(object value)
    {
        return _converter.Convert(value, typeof(StatusLevel), null!, CultureInfo.InvariantCulture);
    }
}
