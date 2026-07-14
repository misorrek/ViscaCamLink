namespace ViscaCamLink.Tests.Infrastructure.Localization;

using System.Globalization;
using System.Threading;

using Shouldly;

using ViscaCamLink.Infrastructure.Localization;
using ViscaCamLink.Resources;

public class LocalizationHelperTests
{
    [Theory]
    [InlineData("de-AT")]
    [InlineData("de-CH")]
    [InlineData("de-DE")]
    [InlineData("de")]
    [InlineData("en-GB")]
    [InlineData("en-US")]
    [InlineData("en")]
    [InlineData("fr-FR")]
    public void ApplyLocalization_SystemLanguage_UsesCurrentCulture(string currentCultureName)
    {
        var originalCulture = Thread.CurrentThread.CurrentCulture;
        var originalUiCulture = Thread.CurrentThread.CurrentUICulture;
        var originalStringsCulture = Strings.Culture;

        try
        {
            var currentCulture = CultureInfo.GetCultureInfo(currentCultureName);
            Thread.CurrentThread.CurrentCulture = currentCulture;

            LocalizationHelper.ApplyLocalization(Language.System);

            Strings.Culture.ShouldBe(currentCulture);
            Thread.CurrentThread.CurrentUICulture.ShouldBe(currentCulture);
            Thread.CurrentThread.CurrentCulture.ShouldBe(currentCulture);
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = originalCulture;
            Thread.CurrentThread.CurrentUICulture = originalUiCulture;
            Strings.Culture = originalStringsCulture;
        }
    }

    [Theory]
    [InlineData(Language.English, "en")]
    [InlineData(Language.German, "de")]
    public void ApplyLocalization_ExplicitLanguage_UsesNeutralCultureFromLanguage(Language language, string expectedCultureName)
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
