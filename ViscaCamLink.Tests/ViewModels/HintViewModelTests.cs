namespace ViscaCamLink.Tests.ViewModels;

using System.Collections.Generic;

using Moq;

using Shouldly;

using ViscaCamLink.Resources;
using ViscaCamLink.Services;
using ViscaCamLink.ViewModels;

using Xunit;

public sealed class HintViewModelTests
{
    private readonly Mock<IHintService> _hintService = new();
    private readonly HintViewModel _viewModel;

    public HintViewModelTests()
    {
        _viewModel = new HintViewModel(_hintService.Object, HintId.PresetRename, nameof(Strings.Hint_PresetRename));
    }

    [Fact]
    public void Message_Success()
    {
        _viewModel.Message.ShouldBe(Strings.Hint_PresetRename);
    }

    [Fact]
    public void IsVisible_Success()
    {
        _hintService.Setup(h => h.IsDismissed(HintId.PresetRename)).Returns(false);

        _viewModel.IsVisible.ShouldBeTrue();
    }

    [Fact]
    public void IsVisible_WhenDismissed_ReturnsFalse()
    {
        _hintService.Setup(h => h.IsDismissed(HintId.PresetRename)).Returns(true);

        _viewModel.IsVisible.ShouldBeFalse();
    }

    [Fact]
    public void DismissCommand_Success()
    {
        _viewModel.DismissCommand.Execute(null);

        _hintService.Verify(h => h.Dismiss(HintId.PresetRename), Times.Once);
    }

    [Fact]
    public void HintsChanged_RaisesIsVisibleChanged()
    {
        var changedProperties = new List<string?>();

        _viewModel.PropertyChanged += (_, eventArgs) => changedProperties.Add(eventArgs.PropertyName);

        _hintService.Raise(h => h.HintsChanged += null);

        changedProperties.ShouldBe([nameof(HintViewModel.IsVisible)]);
    }
}
