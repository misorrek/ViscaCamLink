namespace ViscaCamLink.Tests.Infrastructure.Theming;

using Shouldly;

using ViscaCamLink.Infrastructure.Theming;
using ViscaCamLink.Resources;

using Xunit;

public sealed class ThemeExtensionTests
{
    [Fact]
    public void ToLocalizedString_Success()
    {
        Theme.System.ToLocalizedString().ShouldBe(Strings.Theme_System);
        Theme.Light.ToLocalizedString().ShouldBe(Strings.Theme_Light);
        Theme.Dark.ToLocalizedString().ShouldBe(Strings.Theme_Dark);
    }

    [Fact]
    public void ToLocalizedString_WhenValueIsUndefined_ReturnsEnumValueAsString()
    {
        ((Theme)99).ToLocalizedString().ShouldBe("99");
    }
}
