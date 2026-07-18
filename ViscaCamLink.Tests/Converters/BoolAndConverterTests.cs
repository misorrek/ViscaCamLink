namespace ViscaCamLink.Tests.Converters;

using System;
using System.Globalization;

using Shouldly;

using ViscaCamLink.Converters;

using Xunit;

public sealed class BoolAndConverterTests
{
    private readonly BoolAndConverter _converter = new();

    [Fact]
    public void Convert_Success()
    {
        object[] values = [true, true, true];

        var result = _converter.Convert(values, typeof(bool), null!, CultureInfo.InvariantCulture);

        result.ShouldBe(true);
    }

    [Fact]
    public void Convert_WhenAnyValueIsFalse_ReturnsFalse()
    {
        object[] values = [true, false, true];

        var result = _converter.Convert(values, typeof(bool), null!, CultureInfo.InvariantCulture);

        result.ShouldBe(false);
    }

    [Fact]
    public void Convert_WhenValuesAreEmpty_ReturnsTrue()
    {
        var result = _converter.Convert([], typeof(bool), null!, CultureInfo.InvariantCulture);

        result.ShouldBe(true);
    }

    [Fact]
    public void ConvertBack_ThrowsNotSupportedException()
    {
        void act() => _converter.ConvertBack(true, [typeof(bool)], null!, CultureInfo.InvariantCulture);

        Should.Throw<NotSupportedException>(act);
    }
}
