namespace ViscaCamLink.Tests.ViewModels;

using System;
using System.Threading;
using System.Threading.Tasks;

using Moq;

using Shouldly;

using ViscaCamLink.Services;
using ViscaCamLink.ViewModels;

using Xunit;

public sealed class UpdateDownloadViewModelTests
{
    private static readonly UpdateInfo TestUpdateInfo = new(
        Version: new Version(2, 0, 0),
        ReleaseNotes: string.Empty,
        InstallerAssetUrl: string.Empty,
        PortableAssetUrl: null,
        HtmlUrl: "https://example.test/releases/v2.0.0");

    private readonly Mock<IUpdateService> _updateService = new();
    private readonly UpdateDownloadViewModel _viewModel;

    private bool _closed;

    public UpdateDownloadViewModelTests()
    {
        _viewModel = new UpdateDownloadViewModel(TestUpdateInfo, _updateService.Object, () => _closed = true);
    }

    [Fact]
    public void VersionText_Success()
    {
        _viewModel.VersionText.ShouldBe("v2.0.0");
    }

    [Fact]
    public void IsIndeterminate_Success()
    {
        _viewModel.IsIndeterminate.ShouldBeTrue();
        _viewModel.IsDownloading.ShouldBeTrue();
    }

    [Fact]
    public async Task StartDownloadAsync_Success()
    {
        IProgress<int>? capturedProgress = null;

        _updateService
            .Setup(u => u.DownloadAndApplyAsync(It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
            .Callback<IProgress<int>?, CancellationToken>((progress, _) => capturedProgress = progress)
            .Returns(Task.CompletedTask);

        await _viewModel.StartDownloadAsync();

        capturedProgress.ShouldNotBeNull();
        _viewModel.HasError.ShouldBeFalse();
        _updateService.Verify(u => u.DownloadAndApplyAsync(It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StartDownloadAsync_WhenDownloadIsCancelled_InvokesCloseHandler()
    {
        _updateService
            .Setup(u => u.DownloadAndApplyAsync(It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        await _viewModel.StartDownloadAsync();

        _closed.ShouldBeTrue();
        _viewModel.HasError.ShouldBeFalse();
    }

    [Fact]
    public async Task StartDownloadAsync_WhenDownloadFails_SetsErrorState()
    {
        _updateService
            .Setup(u => u.DownloadAndApplyAsync(It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("network broke"));

        await _viewModel.StartDownloadAsync();

        _viewModel.HasError.ShouldBeTrue();
        _viewModel.ErrorMessage.ShouldBe("network broke");
        _viewModel.IsDownloading.ShouldBeFalse();
        _viewModel.IsIndeterminate.ShouldBeFalse();
        _closed.ShouldBeFalse();
    }

    [Fact]
    public void CancelCommand_Success()
    {
        _viewModel.CancelCommand.Execute(null);

        _closed.ShouldBeTrue();
    }

    [Fact]
    public void RetryCommand_Success()
    {
        _updateService
            .Setup(u => u.DownloadAndApplyAsync(It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("still broken"));

        _viewModel.RetryCommand.Execute(null);

        _updateService.Verify(u => u.DownloadAndApplyAsync(It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()), Times.Once);
        _viewModel.HasError.ShouldBeTrue();
    }
}
