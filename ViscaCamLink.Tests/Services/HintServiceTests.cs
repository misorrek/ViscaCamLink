namespace ViscaCamLink.Tests.Services;

using System.Collections.Generic;

using Moq;

using Shouldly;

using ViscaCamLink.Services;

using Xunit;

public sealed class HintServiceTests
{
    private readonly List<string> _dismissedHints = [];
    private readonly Mock<ISettingsService> _settingsService = new();
    private readonly HintService _hintService;

    public HintServiceTests()
    {
        _settingsService.Setup(s => s.DismissedHints).Returns(_dismissedHints);
        _settingsService
            .Setup(s => s.DismissHint(It.IsAny<string>()))
            .Callback<string>(_dismissedHints.Add);
        _settingsService
            .Setup(s => s.ResetDismissedHints())
            .Callback(_dismissedHints.Clear);

        _hintService = new HintService(_settingsService.Object);
    }

    [Fact]
    public void IsDismissed_Success()
    {
        _dismissedHints.Add(nameof(HintId.PresetRename));

        _hintService.IsDismissed(HintId.PresetRename).ShouldBeTrue();
    }

    [Fact]
    public void IsDismissed_WhenNothingIsDismissed_ReturnsFalse()
    {
        _hintService.IsDismissed(HintId.PresetRename).ShouldBeFalse();
    }

    [Fact]
    public void IsDismissed_WhenOnlyUnknownIdsAreStored_ReturnsFalse()
    {
        _dismissedHints.Add("RemovedHint");

        _hintService.IsDismissed(HintId.PresetRename).ShouldBeFalse();
    }

    [Fact]
    public void Dismiss_Success()
    {
        var raised = false;

        _hintService.HintsChanged += () => raised = true;

        _hintService.Dismiss(HintId.PresetRename);

        _hintService.IsDismissed(HintId.PresetRename).ShouldBeTrue();
        raised.ShouldBeTrue();
        _settingsService.Verify(s => s.DismissHint(nameof(HintId.PresetRename)), Times.Once);
    }

    [Fact]
    public void Dismiss_WhenAlreadyDismissed_DoesNothing()
    {
        var raised = false;

        _dismissedHints.Add(nameof(HintId.PresetRename));
        _hintService.HintsChanged += () => raised = true;

        _hintService.Dismiss(HintId.PresetRename);

        raised.ShouldBeFalse();
        _settingsService.Verify(s => s.DismissHint(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void ResetAll_Success()
    {
        var raised = false;

        _dismissedHints.Add(nameof(HintId.PresetRename));
        _hintService.HintsChanged += () => raised = true;

        _hintService.ResetAll();

        _hintService.IsDismissed(HintId.PresetRename).ShouldBeFalse();
        raised.ShouldBeTrue();
        _settingsService.Verify(s => s.ResetDismissedHints(), Times.Once);
    }
}
