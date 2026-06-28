namespace ViscaCamLink.Tests.Services;

using System.IO;
using System.Windows.Input;

using FluentAssertions;

using ViscaCamLink.Services;

public sealed class HotKeyRepositoryTests : IDisposable
{
    private readonly string _filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");

    [Fact]
    public void Load_WhenFileDoesNotExist_ReturnsDefaultBindings()
    {
        var repository = new HotKeyRepository(_filePath);

        var bindings = repository.Load();

        bindings.Should().HaveCount(10);
        bindings[0].Action.Should().Be(HotKeyAction.Preset0);
        bindings[0].Key.Should().Be(Key.NumPad0);
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

        loadedBindings.Should().ContainSingle(binding =>
            binding.Action == HotKeyAction.Preset0 &&
            binding.Modifier == ModifierKeys.Control &&
            binding.Key == Key.D0);
    }

    public void Dispose()
    {
        if (File.Exists(_filePath))
        {
            File.Delete(_filePath);
        }
    }
}