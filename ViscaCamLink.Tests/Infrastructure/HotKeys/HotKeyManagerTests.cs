namespace ViscaCamLink.Tests.Infrastructure.HotKeys;

using System.Windows.Input;

using Shouldly;

using ViscaCamLink.Infrastructure.HotKeys;

using Xunit;

public sealed class HotKeyManagerTests
{
    [Theory]
    [InlineData(ModifierKeys.None, 0)]
    [InlineData(ModifierKeys.Alt, 1)]
    [InlineData(ModifierKeys.Control | ModifierKeys.Shift, 6)]
    public void ToModifierCode_ReturnsWindowsModifierFlags(ModifierKeys modifier, uint expected)
    {
        HotKeyMapping.ToModifierCode(modifier).ShouldBe(expected);
    }

    [Theory]
    [InlineData(Key.A, 0x41)]
    [InlineData(Key.NumPad0, 0x60)]
    public void ToVirtualKeyCode_ReturnsWindowsVirtualKey(Key key, uint expected)
    {
        HotKeyMapping.ToVirtualKeyCode(key).ShouldBe(expected);
    }

    [Fact]
    public void HoldKeyState_TracksPressReleaseAndReleaseAll()
    {
        var state = new HoldKeyState<int>();

        state.Press(1).ShouldBeTrue();
        state.Press(1).ShouldBeFalse();
        state.Release(2).ShouldBeFalse();
        state.Release(1).ShouldBeTrue();
        state.Release(1).ShouldBeFalse();

        state.Press(3);
        state.Press(4);
        var releasedKeys = state.ReleaseAll();
        releasedKeys.Length.ShouldBe(2);
        releasedKeys.ShouldContain(3);
        releasedKeys.ShouldContain(4);
        state.ReleaseAll().ShouldBeEmpty();
    }
}
