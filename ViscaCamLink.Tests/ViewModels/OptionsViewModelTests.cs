namespace ViscaCamLink.Tests.ViewModels;

using System;
using System.Collections.Generic;
using System.Linq;

using Moq;

using Shouldly;

using ViscaCamLink.Infrastructure.Localization;
using ViscaCamLink.Infrastructure.Theming;
using ViscaCamLink.Repositories.HotKeys;
using ViscaCamLink.Services;
using ViscaCamLink.ViewModels;

using Xunit;

public sealed class OptionsViewModelTests
{
    private readonly Mock<ISettingsService> _settings = new();
    private readonly Mock<IHotKeyService> _hotKeyService = new();
    private readonly OptionsViewModel _viewModel;

    private bool _closed;

    public OptionsViewModelTests()
    {
        _settings.Setup(s => s.Language).Returns(Language.English);
        _settings.Setup(s => s.Theme).Returns(Theme.Dark);
        _settings.Setup(s => s.UseNumpadLayout).Returns(true);
        _settings.Setup(s => s.UseGlobalHotKeys).Returns(false);
        _settings.Setup(s => s.UsePresetGroups).Returns(true);
        _settings.Setup(s => s.UseCompactView).Returns(false);
        _hotKeyService.Setup(h => h.Bindings).Returns(HotKeyDefinitions.CreateDefaultBindings());
        _hotKeyService
            .Setup(h => h.ValidateBindings(It.IsAny<IEnumerable<HotKeyBinding>>()))
            .Returns(HotKeyBindingValidationResult.Valid());

        _viewModel = CreateSut();
    }

    [Fact]
    public void Constructor_Success()
    {
        _viewModel.SelectedLanguage.ShouldBe(Language.English);
        _viewModel.SelectedTheme.ShouldBe(Theme.Dark);
        _viewModel.UseNumpadLayout.ShouldBeTrue();
        _viewModel.UseGlobalHotKeys.ShouldBeFalse();
        _viewModel.UsePresetGroups.ShouldBeTrue();
        _viewModel.UseCompactView.ShouldBeFalse();
        _viewModel.LanguageItems.Select(item => item.LanguageValue).ShouldBe(Enum.GetValues<Language>());
        _viewModel.ThemeItems.Select(item => item.ThemeValue).ShouldBe(Enum.GetValues<Theme>());
        _viewModel.HotKeyBindings.Count.ShouldBe(HotKeyDefinitions.CreateDefaultBindings().Count);
        _viewModel.HasHotKeyConflicts.ShouldBeFalse();
    }

    [Fact]
    public void Constructor_WhenDuplicateGesturesExist_MarksConflictingBindings()
    {
        var bindings = HotKeyDefinitions.CreateDefaultBindings()
            .Select(binding => binding.Copy())
            .ToList();

        bindings[1].Key = bindings[0].Key;

        _hotKeyService.Setup(h => h.Bindings).Returns(bindings);

        var viewModel = CreateSut();

        viewModel.HotKeyBindings[0].HasConflict.ShouldBeTrue();
        viewModel.HotKeyBindings[1].HasConflict.ShouldBeTrue();
        viewModel.HotKeyBindings[2].HasConflict.ShouldBeFalse();
    }

    [Fact]
    public void OkCommand_Success()
    {
        _viewModel.SelectedLanguage = Language.German;
        _viewModel.SelectedTheme = Theme.Light;
        _viewModel.UseNumpadLayout = false;

        _viewModel.OkCommand.Execute(null);

        _settings.Verify(s => s.ApplyOptions(
            Language.German,
            false,
            false,
            true,
            false,
            Theme.Light), Times.Once);
        _hotKeyService.Verify(h => h.ApplyBindings(It.IsAny<IReadOnlyList<HotKeyBinding>>()), Times.Once);
        _closed.ShouldBeTrue();
    }

    [Fact]
    public void OkCommand_WhenBindingsAreInvalid_DoesNotApplyOrClose()
    {
        _hotKeyService
            .Setup(h => h.ValidateBindings(It.IsAny<IEnumerable<HotKeyBinding>>()))
            .Returns(HotKeyBindingValidationResult.Invalid("conflict"));

        var viewModel = CreateSut();

        viewModel.HasHotKeyConflicts.ShouldBeTrue();
        viewModel.HotKeyValidationMessage.ShouldBe("conflict");
        viewModel.OkCommand.CanExecute(null).ShouldBeFalse();

        viewModel.OkCommand.Execute(null);

        _settings.Verify(s => s.ApplyOptions(
            It.IsAny<Language>(),
            It.IsAny<bool>(),
            It.IsAny<bool>(),
            It.IsAny<bool>(),
            It.IsAny<bool>(),
            It.IsAny<Theme>()), Times.Never);
        _closed.ShouldBeFalse();
    }

    [Fact]
    public void CancelCommand_Success()
    {
        _viewModel.CancelCommand.Execute(null);

        _closed.ShouldBeTrue();
        _settings.Verify(s => s.ApplyOptions(
            It.IsAny<Language>(),
            It.IsAny<bool>(),
            It.IsAny<bool>(),
            It.IsAny<bool>(),
            It.IsAny<bool>(),
            It.IsAny<Theme>()), Times.Never);
    }

    [Fact]
    public void HotKeyCaptureCommand_Success()
    {
        var first = _viewModel.HotKeyBindings[0];
        var second = _viewModel.HotKeyBindings[1];

        _viewModel.HotKeyCaptureCommand.Execute(first);

        first.IsCapturing.ShouldBeTrue();

        _viewModel.HotKeyCaptureCommand.Execute(second);

        first.IsCapturing.ShouldBeFalse();
        second.IsCapturing.ShouldBeTrue();
    }

    private OptionsViewModel CreateSut()
    {
        return new OptionsViewModel(_settings.Object, _hotKeyService.Object, () => _closed = true);
    }
}
