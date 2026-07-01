namespace ViscaCamLink.Tests.Services;

using System.IO;
using System.Windows.Input;

using Shouldly;
using ViscaCamLink.Repositories;
using ViscaCamLink.Services;

public sealed class HotKeyRepositoryTests : IDisposable
{
    private readonly string _filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");

    [Fact]
    public void Load_WhenFileDoesNotExist_ReturnsDefaultBindings()
    {
        var repository = new HotKeyRepository(_filePath);

        var bindings = repository.Load();

        bindings.Count.ShouldBe(HotKeyDefinitions.CreateDefaultBindings().Count);
        bindings[0].Action.ShouldBe(HotKeyAction.Preset0);
        bindings[0].Key.ShouldBe(Key.NumPad0);
    }

    [Fact]
    public void SaveAndLoad_RoundTripsBindings()
    {
        var repository = new HotKeyRepository(_filePath);
        var bindings = HotKeyDefinitions.CreateDefaultBindings()
            .Select(binding => binding.Copy())
            .ToList();
        bindings[0].Modifier = ModifierKeys.Control;
        bindings[0].Key = Key.D0;

        repository.Save(bindings);
        var loadedBindings = repository.Load();

        loadedBindings.ShouldContain(binding =>
            binding.Action == HotKeyAction.Preset0 &&
            binding.Modifier == ModifierKeys.Control &&
            binding.Key == Key.D0);
    }

    [Fact]
    public void Save_WritesEnumsAsStrings()
    {
        var repository = new HotKeyRepository(_filePath);
        var bindings = HotKeyDefinitions.CreateDefaultBindings()
            .Select(binding => binding.Copy())
            .ToList();

        repository.Save(bindings);
        var json = File.ReadAllText(_filePath);

        json.ShouldContain("\"Action\": \"Preset0\"");
        json.ShouldContain("\"Modifier\": \"None\"");
        json.ShouldContain("\"Key\": \"NumPad0\"");
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

        var repository = new HotKeyRepository(_filePath);
        var bindings = repository.Load();

        bindings.Count.ShouldBe(HotKeyDefinitions.CreateDefaultBindings().Count);
        bindings[0].Action.ShouldBe(HotKeyAction.Preset0);
        bindings[0].Modifier.ShouldBe(ModifierKeys.None);
        bindings[0].Key.ShouldBe(Key.NumPad0);
    }

    public void Dispose()
    {
        if (File.Exists(_filePath))
        {
            File.Delete(_filePath);
        }
    }
}