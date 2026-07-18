namespace ViscaCamLink.Tests.Extensions;

using System;

using Shouldly;

using ViscaCamLink.Extensions;
using ViscaCamLink.Infrastructure.Localization;

using Xunit;

public sealed class EnumExtensionTests
{
    [Theory]
    [InlineData(Language.English, "en")]
    [InlineData(Language.German, "de")]
    public void GetDescription_Success(Language language, string expectedDescription)
    {
        var description = language.GetDescription();

        description.ShouldBe(expectedDescription);
    }

    [Fact]
    public void GetDescription_WhenValueIsNull_ThrowsArgumentNullException()
    {
        static void act() => EnumExtension.GetDescription<object?>(null);

        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void GetDescription_WhenValueIsNotEnum_ThrowsArgumentException()
    {
        static void act() => "not an enum".GetDescription();

        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void GetDescription_WhenValueHasNoDescriptionAttribute_ThrowsArgumentException()
    {
        static void act() => Language.System.GetDescription();

        Should.Throw<ArgumentException>(act);
    }
}
