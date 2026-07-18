namespace ViscaCamLink.Tests.Infrastructure.Localization;

using System;
using System.Globalization;
using System.Threading;

using Shouldly;

using ViscaCamLink.Infrastructure.Localization;
using ViscaCamLink.Resources;

using Xunit;

public sealed class LocalizationHelperTests
{
    [Theory]
    [InlineData(Language.English, "en")]
    [InlineData(Language.German, "de")]
    public void ApplyLocalization_Success(Language language, string expectedCultureName)
    {
        RunWithRestoredCulture(() =>
        {
            var expectedCulture = CultureInfo.GetCultureInfo(expectedCultureName);

            LocalizationHelper.ApplyLocalization(language);

            Strings.Culture.ShouldBe(expectedCulture);
            Thread.CurrentThread.CurrentUICulture.ShouldBe(expectedCulture);
            Thread.CurrentThread.CurrentCulture.ShouldBe(expectedCulture);
        });
    }

    [Theory]
    [InlineData("de-AT")]
    [InlineData("de-CH")]
    [InlineData("de-DE")]
    [InlineData("de")]
    [InlineData("en-GB")]
    [InlineData("en-US")]
    [InlineData("en")]
    [InlineData("fr-FR")]
    public void ApplyLocalization_WhenLanguageIsSystem_UsesCurrentCulture(string currentCultureName)
    {
        RunWithRestoredCulture(() =>
        {
            var currentCulture = CultureInfo.GetCultureInfo(currentCultureName);

            Thread.CurrentThread.CurrentCulture = currentCulture;

            LocalizationHelper.ApplyLocalization(Language.System);

            Strings.Culture.ShouldBe(currentCulture);
            Thread.CurrentThread.CurrentUICulture.ShouldBe(currentCulture);
            Thread.CurrentThread.CurrentCulture.ShouldBe(currentCulture);
        });
    }

    private static void RunWithRestoredCulture(Action test)
    {
        var originalCulture = Thread.CurrentThread.CurrentCulture;
        var originalUiCulture = Thread.CurrentThread.CurrentUICulture;
        var originalStringsCulture = Strings.Culture;

        try
        {
            test();
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = originalCulture;
            Thread.CurrentThread.CurrentUICulture = originalUiCulture;
            Strings.Culture = originalStringsCulture;
        }
    }
}
