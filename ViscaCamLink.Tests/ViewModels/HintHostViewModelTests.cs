namespace ViscaCamLink.Tests.ViewModels;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Moq;

using Shouldly;

using ViscaCamLink.Infrastructure.Interface;
using ViscaCamLink.Resources;
using ViscaCamLink.Services;
using ViscaCamLink.ViewModels;

using Xunit;

public sealed class HintHostViewModelTests
{
    private readonly Mock<IHintService> _hintService = new();
    private readonly Mock<IUiDispatcher> _uiDispatcher = new();

    public HintHostViewModelTests()
    {
        _uiDispatcher
            .Setup(d => d.Post(It.IsAny<Action>()))
            .Callback<Action>(action => action());
    }

    [Fact]
    public void CurrentHint_WhenNotDismissed_Success()
    {
        _hintService.Setup(h => h.IsDismissed(HintId.PresetRename)).Returns(false);

        var viewModel = CreateViewModel(TimeSpan.Zero);

        viewModel.HasHint.ShouldBeTrue();
        viewModel.CurrentHint.ShouldNotBeNull();
        viewModel.CurrentHint.Message.ShouldBe(Strings.Hint_PresetRename);
    }

    [Fact]
    public void CurrentHint_WhenAllDismissed_IsNull()
    {
        _hintService.Setup(h => h.IsDismissed(It.IsAny<HintId>())).Returns(true);

        var viewModel = CreateViewModel(TimeSpan.Zero);

        viewModel.HasHint.ShouldBeFalse();
        viewModel.CurrentHint.ShouldBeNull();
    }

    [Fact]
    public async Task CurrentHint_BeforeStartupDelay_IsNullThenShown()
    {
        _hintService.Setup(h => h.IsDismissed(It.IsAny<HintId>())).Returns(false);

        var viewModel = CreateViewModel(TimeSpan.FromMilliseconds(200));
        var changedProperties = new List<string?>();
        viewModel.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName);

        viewModel.HasHint.ShouldBeFalse();

        await Task.Delay(1000);

        viewModel.HasHint.ShouldBeTrue();
        changedProperties.ShouldContain(nameof(HintHostViewModel.CurrentHint));
    }

    [Fact]
    public void Dismiss_HidesHintImmediately()
    {
        var dismissed = false;
        _hintService.Setup(h => h.IsDismissed(It.IsAny<HintId>())).Returns(() => dismissed);
        var viewModel = CreateViewModel(TimeSpan.Zero, TimeSpan.FromMinutes(1));
        var changedProperties = new List<string?>();
        viewModel.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName);
        viewModel.HasHint.ShouldBeTrue();

        dismissed = true;
        _hintService.Raise(h => h.HintsChanged += null);

        viewModel.HasHint.ShouldBeFalse();
        changedProperties.ShouldBe([nameof(HintHostViewModel.CurrentHint), nameof(HintHostViewModel.HasHint)]);
    }

    [Fact]
    public async Task Dismiss_WhenAnotherHintRemains_ShowsItAfterBetweenDelay()
    {
        // Calls: initial show, dismissed check, "any remaining" check, show after delay.
        _hintService.SetupSequence(h => h.IsDismissed(It.IsAny<HintId>()))
            .Returns(false)
            .Returns(true)
            .Returns(false)
            .Returns(false);
        var viewModel = CreateViewModel(TimeSpan.Zero, TimeSpan.FromMilliseconds(200));

        _hintService.Raise(h => h.HintsChanged += null);

        viewModel.HasHint.ShouldBeFalse();

        await Task.Delay(1000);

        viewModel.HasHint.ShouldBeTrue();
    }

    private HintHostViewModel CreateViewModel(TimeSpan startupDelay, TimeSpan? betweenDelay = null) =>
        new(_hintService.Object, _uiDispatcher.Object, startupDelay, betweenDelay ?? TimeSpan.Zero);
}
