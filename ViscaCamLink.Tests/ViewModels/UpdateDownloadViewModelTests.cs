namespace ViscaCamLink.Tests.ViewModels;

using System;
using System.Threading;
using System.Threading.Tasks;

using Moq;

using Shouldly;

using ViscaCamLink.Updater;
using ViscaCamLink.ViewModels;

using Xunit;

public sealed class UpdateDownloadViewModelTests
{
    private const string AssetUrl = "https://example.test/files/installer.exe";

    private readonly Mock<IUpdateDownloader> _downloader = new();
    private readonly Mock<IInstallerLauncher> _launcher = new();
    private readonly UpdateDownloadViewModel _viewModel;

    private bool _closed;

    public UpdateDownloadViewModelTests()
    {
        _viewModel = new UpdateDownloadViewModel(AssetUrl, _downloader.Object, _launcher.Object, () => _closed = true);
    }

    [Fact]
    public void AssetName_Success()
    {
        _viewModel.AssetName.ShouldBe("installer.exe");
    }

    [Fact]
    public void IsIndeterminate_Success()
    {
        _viewModel.IsIndeterminate.ShouldBeTrue();
        _viewModel.IsDownloading.ShouldBeTrue();
    }

    [Fact]
    public async Task StartDownloadAsync_WhenDownloadIsCancelled_InvokesCloseHandler()
    {
        _downloader
            .Setup(d => d.DownloadAsync(AssetUrl, "installer.exe", It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        await _viewModel.StartDownloadAsync();

        _closed.ShouldBeTrue();
        _viewModel.HasError.ShouldBeFalse();
    }

    [Fact]
    public async Task StartDownloadAsync_WhenDownloadFails_SetsErrorState()
    {
        _downloader
            .Setup(d => d.DownloadAsync(AssetUrl, "installer.exe", It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
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
        _downloader
            .Setup(d => d.DownloadAsync(AssetUrl, "installer.exe", It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("still broken"));

        _viewModel.RetryCommand.Execute(null);

        _downloader.Verify(
            d => d.DownloadAsync(AssetUrl, "installer.exe", It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _viewModel.HasError.ShouldBeTrue();
    }
}
