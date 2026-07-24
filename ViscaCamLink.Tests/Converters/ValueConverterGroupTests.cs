namespace ViscaCamLink.Tests.Converters;

using System;
using System.Globalization;
using System.Windows;

using Shouldly;

using ViscaCamLink.Converters;

using Xunit;

public sealed class ValueConverterGroupTests
{
    [Fact]
    public void Convert_Success()
    {
        var group = new ValueConverterGroup
        {
            new InverseBooleanConverter(),
            new BoolToVisibilityConverter(),
        };

        var result = group.Convert(true, typeof(Visibility), null!, CultureInfo.InvariantCulture);

        result.ShouldBe(Visibility.Hidden);
    }

    [Fact]
    public void Convert_WhenGroupIsEmpty_ReturnsInputUnchanged()
    {
        var group = new ValueConverterGroup();

        var result = group.Convert("unchanged", typeof(string), null!, CultureInfo.InvariantCulture);

        result.ShouldBe("unchanged");
    }

    [Fact]
    public void ConvertBack_ThrowsNotSupportedException()
    {
        var group = new ValueConverterGroup();

        void act() => group.ConvertBack(true, typeof(bool), null!, CultureInfo.InvariantCulture);

        Should.Throw<NotSupportedException>(act);
    }
}
