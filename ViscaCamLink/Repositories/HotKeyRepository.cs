namespace ViscaCamLink.Repositories;

using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

public sealed class HotKeyRepository(string filePath) : IHotKeyRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters =
        {
            new JsonStringEnumConverter(),
        },
    };

    public HotKeyRepository() : this(GetDefaultFilePath()) { }

    public IReadOnlyList<HotKeyBinding> Load()
    {
        if (!File.Exists(filePath))
        {
            return HotKeyDefinitions.CreateDefaultBindings();
        }

        try
        {
            var json = File.ReadAllText(filePath);
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
        var directory = Path.GetDirectoryName(filePath);

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(bindings, JsonOptions);

        File.WriteAllText(filePath, json);
    }

    private static string GetDefaultFilePath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        return Path.Combine(appData, "ViscaCamLink", "hotkeys.json"); //TODO : Provider for app location
    }

    private static List<HotKeyBinding> MergeWithDefaults(IReadOnlyList<HotKeyBinding> loadedBindings)
    {
        var loadedByAction = loadedBindings
            .GroupBy(binding => binding.Action)
            .ToDictionary(group => group.Key, group => group.First());

        return [.. HotKeyDefinitions.CreateDefaultBindings()
            .Select(defaultBinding => loadedByAction.TryGetValue(defaultBinding.Action, out var loadedBinding)
                ? loadedBinding.Copy()
                : defaultBinding.Copy())];
    }
}