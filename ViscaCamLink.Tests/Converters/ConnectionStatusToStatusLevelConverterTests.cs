namespace ViscaCamLink.Tests.Converters;

using System;
using System.Globalization;

using Shouldly;

using ViscaCamLink.Converters;
using ViscaCamLink.Visca.Types;

using Xunit;

public sealed class ConnectionStatusToStatusLevelConverterTests
{
    private readonly ConnectionStatusToStatusLevelConverter _converter = new();

    [Fact]
    public void Convert_Success()
    {
        Convert(ConnectionStatus.Failed).ShouldBe(StatusLevel.Error);
        Convert(ConnectionStatus.Working).ShouldBe(StatusLevel.Warning);
        Convert(ConnectionStatus.Ok).ShouldBe(StatusLevel.Ok);
    }

    [Fact]
    public void Convert_WhenValueIsNotConnectionStatus_ReturnsUnknown()
    {
        Convert("not a status").ShouldBe(StatusLevel.Unknown);
    }

    [Fact]
    public void ConvertBack_ThrowsNotSupportedException()
    {
        void act() => _converter.ConvertBack(StatusLevel.Ok, typeof(ConnectionStatus), null!, CultureInfo.InvariantCulture);

        Should.Throw<NotSupportedException>(act);
    }

    private object Convert(object value)
    {
        return _converter.Convert(value, typeof(StatusLevel), null!, CultureInfo.InvariantCulture);
    }
}
