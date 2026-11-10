namespace ViscaCamLink.Tests.Repositories.HotKeys;

using System;
using System.IO;
using System.Linq;
using System.Windows.Input;

using Microsoft.Extensions.Logging.Abstractions;

using Shouldly;

using ViscaCamLink.Repositories.HotKeys;

using Xunit;

public sealed class HotKeyRepositoryTests : IDisposable
{
    private readonly string _rootDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    private readonly string _filePath;
    private readonly HotKeyRepository _repository;

    public HotKeyRepositoryTests()
    {
        Directory.CreateDirectory(_rootDirectory);
        _filePath = Path.Combine(_rootDirectory, "hotkeys.json");
        _repository = new HotKeyRepository(_filePath, NullLogger<HotKeyRepository>.Instance);
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootDirectory))
        {
            Directory.Delete(_rootDirectory, recursive: true);
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
    public void Load_WhenListContainsNullEntries_IgnoresThem()
    {
        File.WriteAllText(_filePath, "[null]");

        var bindings = _repository.Load();

        bindings.Count.ShouldBe(HotKeyDefinitions.CreateDefaultBindings().Count);
        bindings[0].Action.ShouldBe(HotKeyAction.Preset0);
    }

    [Fact]
    public void Load_WhenFileIsCorrupt_PreservesIt()
    {
        File.WriteAllText(_filePath, "[");

        var bindings = _repository.Load();

        bindings.Count.ShouldBe(HotKeyDefinitions.CreateDefaultBindings().Count);
        var rejectedFiles = Directory.GetFiles(_rootDirectory, "hotkeys.json.corrupt-*");
        rejectedFiles.Length.ShouldBe(1);
        File.ReadAllText(rejectedFiles[0]).ShouldBe("[");
    }

    [Fact]
    public void Load_WhenFileCannotBeRead_DoesNotOverwriteIt()
    {
        File.WriteAllText(_filePath, "[]");

        using (new FileStream(_filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            var bindings = _repository.Load();

            bindings.Count.ShouldBe(HotKeyDefinitions.CreateDefaultBindings().Count);
        }

        File.ReadAllText(_filePath).ShouldBe("[]");
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

    [Fact]
    public void Save_WhenReplacingExistingBindings_CreatesBackup()
    {
        var bindings = HotKeyDefinitions.CreateDefaultBindings()
            .Select(binding => binding.Copy())
            .ToList();
        _repository.Save(bindings);
        bindings[0].Key = Key.D0;

        _repository.Save(bindings);

        File.ReadAllText($"{_filePath}.bak").ShouldContain("\"Key\": \"NumPad0\"");
        _repository.Load().First().Key.ShouldBe(Key.D0);
    }
}
