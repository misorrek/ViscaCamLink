namespace ViscaCamLink.Tests.Repositories.HotKeys;

using System.Windows.Input;

using Shouldly;

using ViscaCamLink.Infrastructure.Localization;
using ViscaCamLink.Repositories.HotKeys;

using Xunit;

public sealed class HotKeyDefinitionsTests
{
    [Fact]
    public void CreateDefaultBindings_Success()
    {
        var bindings = HotKeyDefinitions.CreateDefaultBindings();

        bindings.Count.ShouldBe(20);
        bindings.ShouldAllBe(binding => binding.Modifier == ModifierKeys.None);

        for (var i = 0; i < 10; i++)
        {
            bindings[i].Action.ShouldBe(HotKeyAction.Preset0 + i);
            bindings[i].Key.ShouldBe(Key.NumPad0 + i);
        }

        for (var i = 10; i < bindings.Count; i++)
        {
            bindings[i].Key.ShouldBe(Key.None);
        }
    }

    [Theory]
    [InlineData(HotKeyAction.Preset0, 0)]
    [InlineData(HotKeyAction.Preset5, 5)]
    [InlineData(HotKeyAction.Preset9, 9)]
    public void TryGetPresetPosition_Success(HotKeyAction action, byte expectedPosition)
    {
        var result = HotKeyDefinitions.TryGetPresetPosition(action, out var presetPosition);

        result.ShouldBeTrue();
        presetPosition.ShouldBe(expectedPosition);
    }

    [Theory]
    [InlineData(HotKeyAction.MoveUp)]
    [InlineData(HotKeyAction.PresetGroupNext)]
    public void TryGetPresetPosition_WhenActionIsNotPreset_ReturnsFalse(HotKeyAction action)
    {
        var result = HotKeyDefinitions.TryGetPresetPosition(action, out var presetPosition);

        result.ShouldBeFalse();
        presetPosition.ShouldBe((byte)0);
    }

    [Fact]
    public void GetDisplayName_Success()
    {
        var expectedPresetName = string.Format(TranslationSource.Instance["HotKeyAction_Preset"], 3);

        HotKeyDefinitions.GetDisplayName(HotKeyAction.Preset3).ShouldBe(expectedPresetName);
        HotKeyDefinitions.GetDisplayName(HotKeyAction.MoveUp).ShouldBe(TranslationSource.Instance["HotKeyAction_MoveUp"]);
        HotKeyDefinitions.GetDisplayName(HotKeyAction.PresetGroupNext).ShouldBe(TranslationSource.Instance["HotKeyAction_PresetGroupNext"]);
    }

    [Fact]
    public void PresetActions_Success()
    {
        HotKeyDefinitions.PresetActions.ShouldBe([
            HotKeyAction.Preset0,
            HotKeyAction.Preset1,
            HotKeyAction.Preset2,
            HotKeyAction.Preset3,
            HotKeyAction.Preset4,
            HotKeyAction.Preset5,
            HotKeyAction.Preset6,
            HotKeyAction.Preset7,
            HotKeyAction.Preset8,
            HotKeyAction.Preset9,
        ]);
    }

    [Fact]
    public void MovementActions_Success()
    {
        HotKeyDefinitions.MovementActions.ShouldBe([
            HotKeyAction.MoveUp,
            HotKeyAction.MoveDown,
            HotKeyAction.MoveLeft,
            HotKeyAction.MoveRight,
            HotKeyAction.ZoomIn,
            HotKeyAction.ZoomOut,
        ]);
    }
}
