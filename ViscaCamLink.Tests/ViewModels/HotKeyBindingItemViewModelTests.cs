namespace ViscaCamLink.Tests.ViewModels;

using System.Collections.Generic;
using System.Windows.Input;

using Shouldly;

using ViscaCamLink.Infrastructure.Localization;
using ViscaCamLink.Repositories.HotKeys;
using ViscaCamLink.Resources;
using ViscaCamLink.ViewModels;

using Xunit;

public sealed class HotKeyBindingItemViewModelTests
{
    [Fact]
    public void GestureText_Success()
    {
        var viewModel = CreateViewModel(ModifierKeys.Control, Key.A);

        viewModel.GestureText.ShouldBe("Control+A");
    }

    [Fact]
    public void GestureText_WhenModifierIsNone_ReturnsKeyOnly()
    {
        var viewModel = CreateViewModel(ModifierKeys.None, Key.A);

        viewModel.GestureText.ShouldBe("A");
    }

    [Fact]
    public void GestureText_WhenKeyIsNone_ReturnsNotSetText()
    {
        var viewModel = CreateViewModel(ModifierKeys.None, Key.None);

        viewModel.GestureText.ShouldBe(TranslationSource.Instance[nameof(Strings.HotKey_NotSet)]);
    }

    [Fact]
    public void ButtonText_Success()
    {
        var viewModel = CreateViewModel(ModifierKeys.None, Key.A);

        viewModel.ButtonText.ShouldBe(viewModel.GestureText);
    }

    [Fact]
    public void ButtonText_WhenCapturing_ReturnsCapturePrompt()
    {
        var viewModel = CreateViewModel(ModifierKeys.None, Key.A);

        viewModel.IsCapturing = true;

        viewModel.ButtonText.ShouldBe(Strings.HotKey_PressKey);
    }

    [Fact]
    public void Key_Success()
    {
        var viewModel = CreateViewModel(ModifierKeys.None, Key.A);
        var changedProperties = new List<string?>();

        viewModel.PropertyChanged += (_, args) => changedProperties.Add(args.PropertyName);

        viewModel.Key = Key.B;

        viewModel.Key.ShouldBe(Key.B);
        changedProperties.ShouldBe([
            nameof(HotKeyBindingItemViewModel.Key),
            nameof(HotKeyBindingItemViewModel.GestureText),
            nameof(HotKeyBindingItemViewModel.ButtonText),
        ]);
    }

    [Fact]
    public void Modifier_Success()
    {
        var viewModel = CreateViewModel(ModifierKeys.None, Key.A);
        var changedProperties = new List<string?>();

        viewModel.PropertyChanged += (_, args) => changedProperties.Add(args.PropertyName);

        viewModel.Modifier = ModifierKeys.Alt;

        viewModel.Modifier.ShouldBe(ModifierKeys.Alt);
        changedProperties.ShouldBe([
            nameof(HotKeyBindingItemViewModel.Modifier),
            nameof(HotKeyBindingItemViewModel.GestureText),
            nameof(HotKeyBindingItemViewModel.ButtonText),
        ]);
    }

    [Fact]
    public void ToBinding_Success()
    {
        var viewModel = CreateViewModel(ModifierKeys.Control, Key.D5);

        var binding = viewModel.ToBinding();

        binding.Action.ShouldBe(HotKeyAction.Preset0);
        binding.Modifier.ShouldBe(ModifierKeys.Control);
        binding.Key.ShouldBe(Key.D5);
    }

    private static HotKeyBindingItemViewModel CreateViewModel(ModifierKeys modifier, Key key)
    {
        return new HotKeyBindingItemViewModel(new HotKeyBinding
        {
            Action = HotKeyAction.Preset0,
            Modifier = modifier,
            Key = key,
        });
    }
}
