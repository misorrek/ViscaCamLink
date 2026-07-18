namespace ViscaCamLink.Tests.Infrastructure.Localization;

using Shouldly;

using ViscaCamLink.Infrastructure.Localization;
using ViscaCamLink.Resources;

using Xunit;

public sealed class LanguageExtensionTests
{
    [Fact]
    public void ToLocalizedString_Success()
    {
        Language.System.ToLocalizedString().ShouldBe(Strings.Language_System);
        Language.English.ToLocalizedString().ShouldBe(Strings.Language_English);
        Language.German.ToLocalizedString().ShouldBe(Strings.Language_German);
    }

    [Fact]
    public void ToLocalizedString_WhenValueIsUndefined_ReturnsEnumValueAsString()
    {
        ((Language)99).ToLocalizedString().ShouldBe("99");
    }
}
