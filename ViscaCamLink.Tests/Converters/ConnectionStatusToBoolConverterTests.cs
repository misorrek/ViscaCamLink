namespace ViscaCamLink.Tests.Converters;

using System;
using System.Globalization;

using Shouldly;

using ViscaCamLink.Converters;
using ViscaCamLink.Visca.Types;

using Xunit;

public sealed class ConnectionStatusToBoolConverterTests
{
    private readonly ConnectionStatusToBoolConverter _converter = new();

    [Theory]
    [InlineData(ConnectionStatus.Ok, true)]
    [InlineData(ConnectionStatus.Failed, false)]
    [InlineData(ConnectionStatus.Working, false)]
    public void Convert_Success(ConnectionStatus status, bool expected)
    {
        var result = _converter.Convert(status, typeof(bool), null!, CultureInfo.InvariantCulture);

        result.ShouldBe(expected);
    }

    [Fact]
    public void Convert_WhenCompareValueIsCustomized_ComparesAgainstIt()
    {
        _converter.CompareValue = ConnectionStatus.Working;

        var result = _converter.Convert(ConnectionStatus.Working, typeof(bool), null!, CultureInfo.InvariantCulture);

        result.ShouldBe(true);
    }

    [Fact]
    public void Convert_WhenValueIsNotConnectionStatus_ReturnsFalse()
    {
        var result = _converter.Convert("not a status", typeof(bool), null!, CultureInfo.InvariantCulture);

        result.ShouldBe(false);
    }

    [Fact]
    public void ConvertBack_ThrowsNotSupportedException()
    {
        void act() => _converter.ConvertBack(true, typeof(ConnectionStatus), null!, CultureInfo.InvariantCulture);

        Should.Throw<NotSupportedException>(act);
    }
}
