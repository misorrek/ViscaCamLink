namespace ViscaCamLink.Tests.Infrastructure.Localization;

using System.Windows.Data;

using Shouldly;

using ViscaCamLink.Infrastructure.Localization;
using ViscaCamLink.Resources;

using Xunit;

public sealed class TranslationSourceTests
{
    [Fact]
    public void Indexer_Success()
    {
        var source = new TranslationSource();

        source["Language_English"].ShouldBe(Strings.Language_English);
    }

    [Fact]
    public void Indexer_WhenKeyIsUnknown_ReturnsKey()
    {
        var source = new TranslationSource();

        source["Key_That_Does_Not_Exist"].ShouldBe("Key_That_Does_Not_Exist");
    }

    [Fact]
    public void NotifyLanguageChanged_Success()
    {
        var source = new TranslationSource();
        string? changedPropertyName = null;
        var languageChangedRaised = false;

        source.PropertyChanged += (_, args) => changedPropertyName = args.PropertyName;
        source.LanguageChanged += (_, _) => languageChangedRaised = true;

        source.NotifyLanguageChanged();

        changedPropertyName.ShouldBe(Binding.IndexerName);
        languageChangedRaised.ShouldBeTrue();
    }
}
