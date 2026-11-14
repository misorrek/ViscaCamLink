namespace ViscaCamLink.Tests.ViewModels;

using System;
using System.Globalization;

using Shouldly;

using SemanticVersion = Velopack.SemanticVersion;

using ViscaCamLink.Resources;
using ViscaCamLink.Services;
using ViscaCamLink.ViewModels;

using Xunit;

public sealed class UpdateViewModelTests
{
    private readonly UpdateInfo _updateInfo = new(
        SemanticVersion.Parse("2.0.0"),
        "release notes",
        "https://example.test/release");

    private readonly UpdateViewModel _viewModel;

    private bool _closed;
    private UpdateInfo? _shownDownloadInfo;

    public UpdateViewModelTests()
    {
        _viewModel = new UpdateViewModel(
            _updateInfo,
            new Version(1, 2, 3),
            () => _closed = true,
            info => _shownDownloadInfo = info,
            CultureInfo.GetCultureInfo("en-US"));
    }

    [Fact]
    public void VersionText_Success()
    {
        _viewModel.VersionText.ShouldBe($"v2.0.0 ({Strings.Updater_CurrentVersion} v1.2.3)");
    }

    [Theory]
    [InlineData("en-US", "RELEASE-NOTES-2.0.0.md")]
    [InlineData("de-DE", "RELEASE-NOTES-2.0.0-de.md")]
    [InlineData("de-AT", "RELEASE-NOTES-2.0.0-de.md")]
    [InlineData("fr-FR", "RELEASE-NOTES-2.0.0.md")]
    public void ChangelogUrl_UsesSelectedCulture(string cultureName, string fileName)
    {
        var viewModel = new UpdateViewModel(
            _updateInfo, null, () => { }, _ => { }, CultureInfo.GetCultureInfo(cultureName));

        viewModel.ChangelogUrl.ShouldBe($"{VelopackUpdateManager.RepositoryUrl}/blob/v2.0.0/Docs/{fileName}");
    }

    [Fact]
    public void ChangelogUrl_PreservesPrereleaseAndBuildMetadata()
    {
        var update = new UpdateInfo(SemanticVersion.Parse("2.1.0-rc.2+build.7"), "", "https://example.test/release");
        var viewModel = new UpdateViewModel(update, null, () => { }, _ => { }, CultureInfo.GetCultureInfo("de"));

        viewModel.ChangelogUrl.ShouldBe(
            $"{VelopackUpdateManager.RepositoryUrl}/blob/v2.1.0-rc.2%2Bbuild.7/Docs/RELEASE-NOTES-2.1.0-rc.2%2Bbuild.7-de.md");
        viewModel.IsChangelogUrl(viewModel.ChangelogUrl).ShouldBeTrue();
    }

    [Fact]
    public void IsChangelogUrl_OnlyAllowsNotesForTargetRelease()
    {
        _viewModel.IsChangelogUrl($"{VelopackUpdateManager.RepositoryUrl}/blob/v2.0.0/Docs/RELEASE-NOTES-2.0.0.md#highlights").ShouldBeTrue();
        _viewModel.IsChangelogUrl($"{VelopackUpdateManager.RepositoryUrl}/blob/v2.0.0/Docs/RELEASE-NOTES-2.0.0-de.md").ShouldBeTrue();
        _viewModel.IsChangelogUrl($"{VelopackUpdateManager.RepositoryUrl}/blob/v2.0.0/Docs/other.md").ShouldBeFalse();
        _viewModel.IsChangelogUrl("https://github.com.evil.test/misorrek/ViscaCamLink/blob/v2.0.0/Docs/RELEASE-NOTES-2.0.0.md").ShouldBeFalse();
    }

    [Fact]
    public void ShowReleasePage_UpdatesFallbackUrl()
    {
        string? changedProperty = null;
        _viewModel.PropertyChanged += (_, args) => changedProperty = args.PropertyName;

        _viewModel.ShowReleasePage();

        _viewModel.ChangelogUrl.ShouldBe("https://example.test/release");
        changedProperty.ShouldBe(nameof(UpdateViewModel.ChangelogUrl));
    }

    [Fact]
    public void UpdateCommand_Success()
    {
        _viewModel.UpdateCommand.Execute(null);

        _closed.ShouldBeTrue();
        _shownDownloadInfo.ShouldBeSameAs(_updateInfo);
    }

    [Fact]
    public void CancelCommand_Success()
    {
        _viewModel.CancelCommand.Execute(null);

        _closed.ShouldBeTrue();
        _shownDownloadInfo.ShouldBeNull();
    }
}
