namespace ViscaCamLink.Tests.ViewModels;

using System;

using Shouldly;

using ViscaCamLink.Resources;
using ViscaCamLink.Updater;
using ViscaCamLink.ViewModels;

using Xunit;

public sealed class UpdateViewModelTests
{
    private readonly UpdateInfo _updateInfo = new(
        new Version(2, 0, 0),
        "release notes",
        "https://example.test/installer.exe",
        null,
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
            info => _shownDownloadInfo = info);
    }

    [Fact]
    public void VersionText_Success()
    {
        _viewModel.VersionText.ShouldBe($"v2.0.0 ({Strings.Updater_CurrentVersion} v1.2.3)");
    }

    [Fact]
    public void ChangelogUrl_Success()
    {
        _viewModel.ChangelogUrl.ShouldBe("https://example.test/release");
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
