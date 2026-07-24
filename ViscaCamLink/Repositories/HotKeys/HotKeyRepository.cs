namespace ViscaCamLink.Repositories.HotKeys;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

using ViscaCamLink.Infrastructure;
using ViscaCamLink.Repositories;

public class HotKeyRepository(string filePath) : IHotKeyRepository
{
    public HotKeyRepository() : this(AppPaths.HotKeysFile)
    {
    }

    public IReadOnlyList<HotKeyBinding> Load()
    {
        if (!File.Exists(filePath))
        {
            return HotKeyDefinitions.CreateDefaultBindings();
        }

        try
        {
            using var stream = File.OpenRead(filePath);
            var loadedBindings = JsonSerializer.Deserialize(stream, RepositoryJsonContext.Default.ListHotKeyBinding);

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

        var bindingsList = bindings is List<HotKeyBinding> list ? list : [.. bindings];

        using var stream = File.Create(filePath);
        JsonSerializer.Serialize(stream, bindingsList, RepositoryJsonContext.Default.ListHotKeyBinding);
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
