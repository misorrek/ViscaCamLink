namespace ViscaCamLink.Tests.Converters;

using System;
using System.Globalization;

using Shouldly;

using ViscaCamLink.Converters;

using Xunit;

public sealed class InverseBooleanConverterTests
{
    private readonly InverseBooleanConverter _converter = new();

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void Convert_Success(bool value, bool expected)
    {
        var result = _converter.Convert(value, typeof(bool), null!, CultureInfo.InvariantCulture);

        result.ShouldBe(expected);
    }

    [Fact]
    public void Convert_WhenValueIsNotBool_ThrowsInvalidOperationException()
    {
        void act() => _converter.Convert("not a bool", typeof(bool), null!, CultureInfo.InvariantCulture);

        Should.Throw<InvalidOperationException>(act);
    }

    [Fact]
    public void ConvertBack_ThrowsNotSupportedException()
    {
        void act() => _converter.ConvertBack(true, typeof(bool), null!, CultureInfo.InvariantCulture);

        Should.Throw<NotSupportedException>(act);
    }
}
