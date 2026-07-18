namespace ViscaCamLink.Tests.Converters;

using System;
using System.Globalization;
using System.Windows;

using Shouldly;

using ViscaCamLink.Converters;

using Xunit;

public sealed class StringToVisibilityConverterTests
{
    private readonly StringToVisibilityConverter _converter = new();

    [Fact]
    public void Convert_Success()
    {
        var result = _converter.Convert("some text", typeof(Visibility), null!, CultureInfo.InvariantCulture);

        result.ShouldBe(Visibility.Visible);
    }

    [Fact]
    public void Convert_WhenStringIsEmpty_ReturnsFalseValue()
    {
        var result = _converter.Convert(string.Empty, typeof(Visibility), null!, CultureInfo.InvariantCulture);

        result.ShouldBe(_converter.FalseValue);
    }

    [Theory]
    [InlineData(42)]
    [InlineData(null)]
    public void Convert_WhenValueIsNotString_ReturnsFallbackValue(object? value)
    {
        var result = _converter.Convert(value!, typeof(Visibility), null!, CultureInfo.InvariantCulture);

        result.ShouldBe(_converter.FallbackValue);
    }

    [Fact]
    public void ConvertBack_ThrowsNotSupportedException()
    {
        void act() => _converter.ConvertBack(Visibility.Visible, typeof(string), null!, CultureInfo.InvariantCulture);

        Should.Throw<NotSupportedException>(act);
    }
}
