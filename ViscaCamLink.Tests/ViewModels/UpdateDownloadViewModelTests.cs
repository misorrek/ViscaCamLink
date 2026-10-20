namespace ViscaCamLink.Tests.ViewModels;

using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using Shouldly;

using ViscaCamLink.Resources;
using ViscaCamLink.Services;
using ViscaCamLink.ViewModels;

using Xunit;

public sealed class UpdateDownloadViewModelTests
{
    private static readonly UpdateInfo TestUpdateInfo = new(
        Version: new Version(2, 0, 0),
        ReleaseNotes: string.Empty,
        HtmlUrl: "https://example.test/releases/v2.0.0");

    private readonly Mock<IUpdateService> _updateService = new();
    private readonly UpdateDownloadViewModel _viewModel;

    private int _closeCount;

    public UpdateDownloadViewModelTests()
    {
        _viewModel = new UpdateDownloadViewModel(
            TestUpdateInfo,
            _updateService.Object,
            () => _closeCount++,
            NullLogger<UpdateDownloadViewModel>.Instance);
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
        _closeCount.ShouldBe(1);
        _updateService.Verify(u => u.DownloadAndApplyAsync(It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StartDownloadAsync_WhenCancelledByUser_ClosesOnceWithoutError()
    {
        var capturedToken = CancellationToken.None;

        _updateService
            .Setup(u => u.DownloadAndApplyAsync(It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
            .Returns<IProgress<int>?, CancellationToken>(async (_, cancellationToken) =>
            {
                capturedToken = cancellationToken;
                _viewModel.CancelCommand.Execute(null);
                await Task.Yield();
                cancellationToken.ThrowIfCancellationRequested();
            });

        await _viewModel.StartDownloadAsync();

        capturedToken.IsCancellationRequested.ShouldBeTrue();
        _closeCount.ShouldBe(1);
        _viewModel.HasError.ShouldBeFalse();
    }

    [Fact]
    public async Task StartDownloadAsync_WhenCancelledWithoutUser_SetsErrorState()
    {
        _updateService
            .Setup(u => u.DownloadAndApplyAsync(It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TaskCanceledException());

        await _viewModel.StartDownloadAsync();

        _viewModel.HasError.ShouldBeTrue();
        _closeCount.ShouldBe(0);
    }

    [Fact]
    public async Task StartDownloadAsync_WhenDownloadFails_SetsErrorState()
    {
        _updateService
            .Setup(u => u.DownloadAndApplyAsync(It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("network broke"));

        await _viewModel.StartDownloadAsync();

        _viewModel.HasError.ShouldBeTrue();
        _viewModel.ErrorMessage.ShouldBe(Strings.Updater_DownloadErrorDetail);
        _viewModel.IsDownloading.ShouldBeFalse();
        _viewModel.IsIndeterminate.ShouldBeFalse();
        _closeCount.ShouldBe(0);
    }

    [Fact]
    public void CancelCommand_Success()
    {
        _viewModel.CancelCommand.Execute(null);

        _closeCount.ShouldBe(1);
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
