namespace ViscaCamLink.Tests.ViewModels;

using System.Collections.Generic;

using Moq;

using Shouldly;

using ViscaCamLink.Services;
using ViscaCamLink.ViewModels;

using Xunit;

public sealed class MovementViewModelTests
{
    private readonly Mock<ICameraMovementService> _movementService = new();
    private readonly Mock<ISettingsService> _settings = new();
    private readonly Mock<IHotKeyService> _hotKeyService = new();
    private readonly MovementViewModel _viewModel;

    public MovementViewModelTests()
    {
        _movementService.Setup(m => m.MaxPanTiltSpeed).Returns(24);
        _settings.SetupProperty(s => s.PanTiltSpeed, 3);

        _viewModel = new MovementViewModel(_movementService.Object, _settings.Object, _hotKeyService.Object);
    }

    [Fact]
    public void Constructor_Success()
    {
        _hotKeyService.Verify(h => h.RegisterActions(It.IsAny<IEnumerable<HotKeyActionRegistration>>()), Times.Once);
    }

    [Fact]
    public void MaximalPanTiltSpeed_Success()
    {
        _viewModel.MaximalPanTiltSpeed.ShouldBe(24);
    }

    [Fact]
    public void PanTiltSpeed_Success()
    {
        var raised = false;

        _viewModel.PropertyChanged += (_, args) => raised |= args.PropertyName == nameof(MovementViewModel.PanTiltSpeed);

        _viewModel.PanTiltSpeed = 5;

        _viewModel.PanTiltSpeed.ShouldBe(5);
        _settings.Object.PanTiltSpeed.ShouldBe(5);
        raised.ShouldBeTrue();
    }

    [Fact]
    public void MoveSpeedIncreaseCommand_Success()
    {
        _viewModel.MoveSpeedIncreaseCommand.Execute(null);

        _settings.Object.PanTiltSpeed.ShouldBe(4);
    }

    [Fact]
    public void MoveSpeedIncreaseCommand_WhenAtMaximum_DoesNotIncrease()
    {
        _settings.Object.PanTiltSpeed = 24;

        _viewModel.MoveSpeedIncreaseCommand.Execute(null);

        _settings.Object.PanTiltSpeed.ShouldBe(24);
    }

    [Fact]
    public void MoveSpeedDecreaseCommand_Success()
    {
        _viewModel.MoveSpeedDecreaseCommand.Execute(null);

        _settings.Object.PanTiltSpeed.ShouldBe(2);
    }

    [Fact]
    public void MoveSpeedDecreaseCommand_WhenAtMinimum_DoesNotDecrease()
    {
        _settings.Object.PanTiltSpeed = 1;

        _viewModel.MoveSpeedDecreaseCommand.Execute(null);

        _settings.Object.PanTiltSpeed.ShouldBe(1);
    }

    [Fact]
    public void HomeCommand_Success()
    {
        var homeExecutedRaised = false;

        _viewModel.HomeExecuted += () => homeExecutedRaised = true;

        _viewModel.HomeCommand.Execute(null);

        homeExecutedRaised.ShouldBeTrue();
        _movementService.Verify(m => m.GoHomeAsync(), Times.Once);
    }

    [Fact]
    public void MoveEndCommand_Success()
    {
        _viewModel.MoveEndCommand.Execute(null);

        _movementService.Verify(m => m.StopPanTiltAsync(), Times.Once);
    }

    [Fact]
    public void MousePanEndCommand_Success()
    {
        _viewModel.MousePanEndCommand.Execute(null);

        _viewModel.IsMousePanning.ShouldBeFalse();
        _movementService.Verify(m => m.StopPanTiltAsync(), Times.Once);
    }
}
