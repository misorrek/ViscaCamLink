namespace ViscaCamLink.Tests.Infrastructure.Localization;

using System.Globalization;
using System.Threading;

using Shouldly;

using ViscaCamLink.Infrastructure.Localization;
using ViscaCamLink.Resources;

public class LocalizationHelperTests
{
    [Theory]
    [InlineData("de-AT", "de-DE")]
    [InlineData("de-CH", "de-DE")]
    [InlineData("de-DE", "de-DE")]
    [InlineData("de", "de-DE")]
    [InlineData("en-GB", "en-US")]
    [InlineData("en-US", "en-US")]
    [InlineData("en", "en-US")]
    [InlineData("fr-FR", "")]
    public void ApplyLocalization_SystemLanguage_UsesNearestSupportedResourceCulture(
        string currentCultureName,
        string expectedResourceCultureName)
    {
        var originalCulture = Thread.CurrentThread.CurrentCulture;
        var originalUiCulture = Thread.CurrentThread.CurrentUICulture;
        var originalStringsCulture = Strings.Culture;

        try
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo(currentCultureName);

            LocalizationHelper.ApplyLocalization(Language.System);

            var expectedResourceCulture = string.IsNullOrEmpty(expectedResourceCultureName)
                ? CultureInfo.InvariantCulture
                : CultureInfo.GetCultureInfo(expectedResourceCultureName);

            Strings.Culture.ShouldBe(expectedResourceCulture);
            Thread.CurrentThread.CurrentUICulture.ShouldBe(expectedResourceCulture);
            Thread.CurrentThread.CurrentCulture.ShouldBe(CultureInfo.GetCultureInfo(currentCultureName));
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = originalCulture;
            Thread.CurrentThread.CurrentUICulture = originalUiCulture;
            Strings.Culture = originalStringsCulture;
        }
    }

    [Theory]
    [InlineData(Language.English, "en-US")]
    [InlineData(Language.German, "de-DE")]
    public void ApplyLocalization_ExplicitLanguage_UsesCultureFromLanguage(Language language, string expectedCultureName)
    {
        var originalCulture = Thread.CurrentThread.CurrentCulture;
        var originalUiCulture = Thread.CurrentThread.CurrentUICulture;
        var originalStringsCulture = Strings.Culture;

        try
        {
            LocalizationHelper.ApplyLocalization(language);

            var expectedCulture = CultureInfo.GetCultureInfo(expectedCultureName);

            Strings.Culture.ShouldBe(expectedCulture);
            Thread.CurrentThread.CurrentUICulture.ShouldBe(expectedCulture);
            Thread.CurrentThread.CurrentCulture.ShouldBe(expectedCulture);
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = originalCulture;
            Thread.CurrentThread.CurrentUICulture = originalUiCulture;
            Strings.Culture = originalStringsCulture;
        }
    }
}
