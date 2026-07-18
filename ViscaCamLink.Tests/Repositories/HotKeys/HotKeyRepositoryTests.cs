namespace ViscaCamLink.Tests.Repositories.HotKeys;

using System;
using System.IO;
using System.Linq;
using System.Windows.Input;

using Shouldly;

using ViscaCamLink.Repositories.HotKeys;

using Xunit;

public sealed class HotKeyRepositoryTests : IDisposable
{
    private readonly string _filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");
    private readonly HotKeyRepository _repository;

    public HotKeyRepositoryTests()
    {
        _repository = new HotKeyRepository(_filePath);
    }

    public void Dispose()
    {
        if (File.Exists(_filePath))
        {
            File.Delete(_filePath);
        }
    }

    [Fact]
    public void Load_WhenFileDoesNotExist_ReturnsDefaultBindings()
    {
        var bindings = _repository.Load();

        bindings.Count.ShouldBe(HotKeyDefinitions.CreateDefaultBindings().Count);
        bindings[0].Action.ShouldBe(HotKeyAction.Preset0);
        bindings[0].Key.ShouldBe(Key.NumPad0);
    }

    [Fact]
    public void Load_WhenEnumValueIsUnknown_ReturnsDefaultBindings()
    {
        File.WriteAllText(_filePath, """
            [
              {
                "Action": "NotARealAction",
                "Modifier": "Control",
                "Key": "D0"
              }
            ]
            """);

        var bindings = _repository.Load();

        bindings.Count.ShouldBe(HotKeyDefinitions.CreateDefaultBindings().Count);
        bindings[0].Action.ShouldBe(HotKeyAction.Preset0);
        bindings[0].Modifier.ShouldBe(ModifierKeys.None);
        bindings[0].Key.ShouldBe(Key.NumPad0);
    }

    [Fact]
    public void Save_Success()
    {
        var bindings = HotKeyDefinitions.CreateDefaultBindings()
            .Select(binding => binding.Copy())
            .ToList();

        bindings[0].Modifier = ModifierKeys.Control;
        bindings[0].Key = Key.D0;

        _repository.Save(bindings);

        var json = File.ReadAllText(_filePath);
        var loadedBindings = _repository.Load();

        json.ShouldContain("\"Action\": \"Preset0\"");
        json.ShouldContain("\"Modifier\": \"Control\"");
        json.ShouldContain("\"Key\": \"D0\"");
        loadedBindings.ShouldContain(binding =>
            binding.Action == HotKeyAction.Preset0 &&
            binding.Modifier == ModifierKeys.Control &&
            binding.Key == Key.D0);
    }
}
