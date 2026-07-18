namespace ViscaCamLink.Tests.Converters;

using System.Globalization;
using System.Windows;

using Shouldly;

using ViscaCamLink.Converters;

using Xunit;

public sealed class BoolToVisibilityConverterTests
{
    private readonly BoolToVisibilityConverter _converter = new();

    [Theory]
    [InlineData(true, Visibility.Visible)]
    [InlineData(false, Visibility.Hidden)]
    public void Convert_Success(bool value, Visibility expected)
    {
        var result = _converter.Convert(value, typeof(Visibility), null!, CultureInfo.InvariantCulture);

        result.ShouldBe(expected);
    }

    [Fact]
    public void Convert_WhenCustomValuesAreConfigured_ReturnsCustomValues()
    {
        _converter.TrueValue = Visibility.Collapsed;
        _converter.FalseValue = Visibility.Visible;

        var trueResult = _converter.Convert(true, typeof(Visibility), null!, CultureInfo.InvariantCulture);
        var falseResult = _converter.Convert(false, typeof(Visibility), null!, CultureInfo.InvariantCulture);

        trueResult.ShouldBe(Visibility.Collapsed);
        falseResult.ShouldBe(Visibility.Visible);
    }

    [Fact]
    public void Convert_WhenValueIsNotBool_ReturnsFallbackValue()
    {
        var result = _converter.Convert("not a bool", typeof(Visibility), null!, CultureInfo.InvariantCulture);

        result.ShouldBe(_converter.FallbackValue);
    }

    [Theory]
    [InlineData(Visibility.Visible, true)]
    [InlineData(Visibility.Hidden, false)]
    public void ConvertBack_Success(Visibility value, bool expected)
    {
        var result = _converter.ConvertBack(value, typeof(bool), null!, CultureInfo.InvariantCulture);

        result.ShouldBe(expected);
    }

    [Fact]
    public void ConvertBack_WhenValueMatchesNeitherConfiguredValue_ReturnsFallbackValue()
    {
        var result = _converter.ConvertBack(Visibility.Collapsed, typeof(bool), null!, CultureInfo.InvariantCulture);

        result.ShouldBe(_converter.FallbackValue);
    }
}
