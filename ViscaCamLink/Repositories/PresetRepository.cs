namespace ViscaCamLink.Repositories;

using System.IO;
using System.Text.Json;

public sealed class PresetRepository(string filePath) : IPresetRepository
{
    private const int MaxCameraSlots = 256;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    public PresetRepository() : this(GetDefaultFilePath()) { }

    public PresetData Load()
    {
        if (!File.Exists(filePath))
        {
            return CreateDefault();
        }

        try
        {
            var json = File.ReadAllText(filePath);
            var data = JsonSerializer.Deserialize<PresetData>(json, JsonOptions);

            if (IsValid(data))
            {
                return data!;
            }
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            // Invalid user-local state should not prevent the app from starting.
        }

        BackupRejectedFile();

        return CreateDefault();
    }

    public void Save(PresetData data)
    {
        var directory = Path.GetDirectoryName(filePath);

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(data, JsonOptions);
        var tempPath = Path.Combine(directory ?? string.Empty, $"{Path.GetFileName(filePath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            File.WriteAllText(tempPath, json);
            File.Move(tempPath, filePath, overwrite: true);
        }
        finally
        {
            TryDelete(tempPath);
        }
    }

    private static bool IsValid(PresetData? data)
    {
        if (data?.Groups is not { Count: > 0 })
        {
            return false;
        }

        var groupIds = new HashSet<string>(StringComparer.Ordinal);
        var usedSlots = new HashSet<int>();

        foreach (var group in data.Groups)
        {
            if (group is null || string.IsNullOrWhiteSpace(group.Id) || group.Presets is null)
            {
                return false;
            }

            if (!groupIds.Add(group.Id))
            {
                return false;
            }

            foreach (var preset in group.Presets)
            {
                if (preset is null ||
                    preset.GroupId != group.Id ||
                    preset.SlotIndex < 0 ||
                    preset.SlotIndex >= MaxCameraSlots ||
                    !usedSlots.Add(preset.SlotIndex))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private void BackupRejectedFile()
    {
        try
        {
            File.Copy(filePath, $"{filePath}.bak", overwrite: true);
        }
        catch
        {
            // Best effort only; startup recovery should not depend on backup success.
        }
    }

    private static void TryDelete(string path)
    {
        try { File.Delete(path); } catch { /* best effort */ }
    }

    private static string GetDefaultFilePath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        return Path.Combine(appData, "ViscaCamLink", "presets.json"); //TODO : Provider for app location
    }

    private static PresetData CreateDefault()
    {
        const string defaultGroupId = "default";
        const int presetsPerGroup = 10;

        var presets = new List<PresetMetadata>(presetsPerGroup);

        for (var i = 0; i < presetsPerGroup; i++)
        {
            presets.Add(new PresetMetadata
            {
                GroupId = defaultGroupId,
                SlotIndex = i,
                Name = i.ToString(),
            });
        }

        return new PresetData
        {
            Groups =
            [
                new PresetGroup
                {
                    Id = defaultGroupId,
                    Name = "Default",
                    Presets = presets,
                },
            ],
        };
    }
}
