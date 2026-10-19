namespace ViscaCamLink.Repositories.HotKeys;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

using Microsoft.Extensions.Logging;

using ViscaCamLink.Infrastructure;
using ViscaCamLink.Repositories;

public partial class HotKeyRepository(string filePath, ILogger<HotKeyRepository> logger) : IHotKeyRepository
{
    public HotKeyRepository(ILogger<HotKeyRepository> logger) : this(AppPaths.HotKeysFile, logger)
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
        catch (JsonException exception)
        {
            LogLoadFailed(logger, filePath, exception);

            return HotKeyDefinitions.CreateDefaultBindings();
        }
    }

    public void Save(IReadOnlyList<HotKeyBinding> bindings)
    {
        var directory = Path.GetDirectoryName(filePath);
        var bindingsList = bindings is List<HotKeyBinding> list ? list : [.. bindings];

        try
        {
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using var stream = File.Create(filePath);
            JsonSerializer.Serialize(stream, bindingsList, RepositoryJsonContext.Default.ListHotKeyBinding);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            LogSaveFailed(logger, filePath, exception);

            throw;
        }
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

    [LoggerMessage(EventId = 3101, Level = LogLevel.Warning, Message = "Hotkey file '{FilePath}' could not be read; falling back to default bindings")]
    private static partial void LogLoadFailed(ILogger logger, string filePath, Exception exception);

    [LoggerMessage(EventId = 3102, Level = LogLevel.Error, Message = "Hotkey file '{FilePath}' could not be saved")]
    private static partial void LogSaveFailed(ILogger logger, string filePath, Exception exception);
}
