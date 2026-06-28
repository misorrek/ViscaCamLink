namespace ViscaCamLink.Services;

using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

public sealed class HotKeyRepository : IHotKeyRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters =
        {
            new JsonStringEnumConverter(),
        },
    };

    private readonly string _filePath;

    public HotKeyRepository()
        : this(GetDefaultFilePath())
    {
    }

    public HotKeyRepository(string filePath)
    {
        _filePath = filePath;
    }

    public IReadOnlyList<HotKeyBinding> Load()
    {
        if (!File.Exists(_filePath))
        {
            return HotKeyDefinitions.CreateDefaultBindings();
        }

        try
        {
            var json = File.ReadAllText(_filePath);
            var loadedBindings = JsonSerializer.Deserialize<List<HotKeyBinding>>(json, JsonOptions);
            return MergeWithDefaults(loadedBindings ?? []);
        }
        catch (JsonException)
        {
            return HotKeyDefinitions.CreateDefaultBindings();
        }
    }

    public void Save(IReadOnlyList<HotKeyBinding> bindings)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(bindings, JsonOptions);
        File.WriteAllText(_filePath, json);
    }

    private static string GetDefaultFilePath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(appData, "ViscaCamLink", "hotkeys.json");
    }

    private static IReadOnlyList<HotKeyBinding> MergeWithDefaults(IReadOnlyList<HotKeyBinding> loadedBindings)
    {
        var loadedByAction = loadedBindings
            .GroupBy(binding => binding.Action)
            .ToDictionary(group => group.Key, group => group.First());

        return HotKeyDefinitions.CreateDefaultBindings()
            .Select(defaultBinding => loadedByAction.TryGetValue(defaultBinding.Action, out var loadedBinding)
                ? loadedBinding.Copy()
                : defaultBinding.Copy())
            .ToList();
    }
}