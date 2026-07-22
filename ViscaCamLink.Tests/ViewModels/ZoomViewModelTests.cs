namespace ViscaCamLink.Tests.ViewModels;

using System.Collections.Generic;

using Moq;

using Shouldly;

using ViscaCamLink.Services;
using ViscaCamLink.ViewModels;

using Xunit;

public sealed class ZoomViewModelTests
{
    private readonly Mock<ICameraMovementService> _movementService = new();
    private readonly Mock<ISettingsService> _settings = new();
    private readonly Mock<IHotKeyService> _hotKeyService = new();
    private readonly ZoomViewModel _viewModel;

    public ZoomViewModelTests()
    {
        _movementService.Setup(m => m.MaxZoomSpeed).Returns(7);
        _settings.SetupProperty(s => s.ZoomSpeed, 3);

        _viewModel = new ZoomViewModel(_movementService.Object, _settings.Object, _hotKeyService.Object);
    }

    [Fact]
    public void Constructor_Success()
    {
        _hotKeyService.Verify(h => h.RegisterActions(It.IsAny<IEnumerable<HotKeyActionRegistration>>()), Times.Once);
    }

    [Fact]
    public void MaximalZoomSpeed_Success()
    {
        _viewModel.MaximalZoomSpeed.ShouldBe(7);
    }

    [Fact]
    public void ZoomSpeed_Success()
    {
        var raised = false;

        _viewModel.PropertyChanged += (_, args) => raised |= args.PropertyName == nameof(ZoomViewModel.ZoomSpeed);

        _viewModel.ZoomSpeed = 5;

        _viewModel.ZoomSpeed.ShouldBe(5);
        _settings.Object.ZoomSpeed.ShouldBe(5);
        raised.ShouldBeTrue();
    }

    [Fact]
    public void ZoomSpeedIncreaseCommand_Success()
    {
        _viewModel.ZoomSpeedIncreaseCommand.Execute(null);

        _settings.Object.ZoomSpeed.ShouldBe(4);
    }

    [Fact]
    public void ZoomSpeedIncreaseCommand_WhenAtMaximum_DoesNotIncrease()
    {
        _settings.Object.ZoomSpeed = 7;

        _viewModel.ZoomSpeedIncreaseCommand.Execute(null);

        _settings.Object.ZoomSpeed.ShouldBe(7);
    }

    [Fact]
    public void ZoomSpeedDecreaseCommand_Success()
    {
        _viewModel.ZoomSpeedDecreaseCommand.Execute(null);

        _settings.Object.ZoomSpeed.ShouldBe(2);
    }

    [Fact]
    public void ZoomSpeedDecreaseCommand_WhenAtMinimum_DoesNotDecrease()
    {
        _settings.Object.ZoomSpeed = 1;

        _viewModel.ZoomSpeedDecreaseCommand.Execute(null);

        _settings.Object.ZoomSpeed.ShouldBe(1);
    }
}
