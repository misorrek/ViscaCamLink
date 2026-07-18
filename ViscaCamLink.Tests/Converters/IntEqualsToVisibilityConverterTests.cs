namespace ViscaCamLink.Tests.Converters;

using System;
using System.Globalization;
using System.Windows;

using Shouldly;

using ViscaCamLink.Converters;

using Xunit;

public sealed class IntEqualsToVisibilityConverterTests
{
    private readonly IntEqualsToVisibilityConverter _converter = new();

    [Theory]
    [InlineData(1, 1, Visibility.Collapsed)]
    [InlineData(1, 2, Visibility.Visible)]
    public void Convert_Success(int first, int second, Visibility expected)
    {
        object[] values = [first, second];

        var result = _converter.Convert(values, typeof(Visibility), null!, CultureInfo.InvariantCulture);

        result.ShouldBe(expected);
    }

    [Fact]
    public void Convert_WhenValuesAreNotTwoInts_ReturnsFallbackValue()
    {
        object[] values = ["not", "ints"];

        var result = _converter.Convert(values, typeof(Visibility), null!, CultureInfo.InvariantCulture);

        result.ShouldBe(_converter.FallbackValue);
    }

    [Fact]
    public void Convert_WhenValueCountIsNotTwo_ReturnsFallbackValue()
    {
        object[] values = [1];

        var result = _converter.Convert(values, typeof(Visibility), null!, CultureInfo.InvariantCulture);

        result.ShouldBe(_converter.FallbackValue);
    }

    [Fact]
    public void ConvertBack_ThrowsNotSupportedException()
    {
        void act() => _converter.ConvertBack(Visibility.Visible, [typeof(int)], null!, CultureInfo.InvariantCulture);

        Should.Throw<NotSupportedException>(act);
    }
}
