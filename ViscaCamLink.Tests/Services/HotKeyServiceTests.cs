namespace ViscaCamLink.Tests.Services;

using System.Windows.Input;

using Shouldly;

using Moq;
using ViscaCamLink.Repositories;
using ViscaCamLink.Services;
using ViscaCamLink.Util;

public sealed class HotKeyServiceTests
{
    private readonly Mock<IGlobalHotKeyManager> _hotKeyManager = new();
    private readonly Mock<IHotKeyRepository> _repository = new();
    private readonly HotKeyService _hotKeyService;

    public HotKeyServiceTests()
    {
        _repository.Setup(r => r.Load()).Returns(HotKeyDefinitions.CreateDefaultBindings());
        _hotKeyService = new HotKeyService(_hotKeyManager.Object, _repository.Object);
    }

    [Fact]
    public void RegisterHotKey_DelegatesToManager_ReturnsTrue()
    {
        Action action = () => { };
        _hotKeyManager
            .Setup(m => m.RegisterHotKey(ModifierKeys.Control, Key.A, action))
            .Returns(true);

        var result = _hotKeyService.RegisterHotKey(ModifierKeys.Control, Key.A, action);

        result.ShouldBeTrue();
    }

    [Fact]
    public void RegisterHotKey_WhenManagerFails_ReturnsFalse()
    {
        Action action = () => { };
        _hotKeyManager
            .Setup(m => m.RegisterHotKey(ModifierKeys.Alt, Key.F1, action))
            .Returns(false);

        var result = _hotKeyService.RegisterHotKey(ModifierKeys.Alt, Key.F1, action);

        result.ShouldBeFalse();
    }

    [Fact]
    public void Dispose_DisposesManager()
    {
        _hotKeyService.Dispose();

        _hotKeyManager.Verify(m => m.Dispose(), Times.Once);
    }

    [Fact]
    public void RegisterActions_UnregistersExistingHotKeysAndRegistersConfiguredBindings()
    {
        Action callback = () => { };

        _hotKeyService.RegisterActions([new HotKeyActionRegistration(HotKeyAction.Preset1, callback)]);

        _hotKeyManager.Verify(m => m.UnregisterAll(), Times.Once);
        _hotKeyManager.Verify(m => m.RegisterHotKey(ModifierKeys.None, Key.NumPad1, callback), Times.Once);
    }

    [Fact]
    public void ApplyBindings_WhenValid_SavesAndReregistersBindings()
    {
        var bindings = HotKeyDefinitions.CreateDefaultBindings()
            .Select(binding => binding.Copy())
            .ToList();
        bindings[0].Modifier = ModifierKeys.Control;
        bindings[0].Key = Key.D0;

        var result = _hotKeyService.ApplyBindings(bindings);

        result.ShouldBeTrue();
        _repository.Verify(r => r.Save(It.Is<IReadOnlyList<HotKeyBinding>>(saved =>
            saved.Any(binding =>
                binding.Action == HotKeyAction.Preset0 &&
                binding.Modifier == ModifierKeys.Control &&
                binding.Key == Key.D0))), Times.Once);
        _hotKeyManager.Verify(m => m.UnregisterAll(), Times.Once);
    }

    [Fact]
    public void ApplyBindings_WhenDuplicateKeyExists_DoesNotSaveOrRegister()
    {
        var bindings = HotKeyDefinitions.CreateDefaultBindings()
            .Select(binding => binding.Copy())
            .ToList();
        bindings[1].Key = bindings[0].Key;

        var result = _hotKeyService.ApplyBindings(bindings);

        result.ShouldBeFalse();
        _repository.Verify(r => r.Save(It.IsAny<IReadOnlyList<HotKeyBinding>>()), Times.Never);
        _hotKeyManager.Verify(m => m.UnregisterAll(), Times.Never);
    }
}
