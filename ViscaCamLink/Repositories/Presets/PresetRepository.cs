namespace ViscaCamLink.Repositories.Presets;

using System.IO;
using System.Text.Json;
using ViscaCamLink.Infrastructure;

public sealed class PresetRepository : IPresetRepository
{
    private const int MaxCameraSlots = 256;

    private readonly Func<Guid, string> _pathForCamera;

    public PresetRepository() : this(AppPaths.PresetsForCamera) { }

    // TODO : PathProvider to not duplicate path logic
    public PresetRepository(string directory)
        : this(id => Path.Combine(directory, $"presets-{id:N}.json")) { }

    private PresetRepository(Func<Guid, string> pathForCamera)
    {
        _pathForCamera = pathForCamera;
    }

    public PresetData LoadForCamera(Guid cameraId)
    {
        var filePath = _pathForCamera(cameraId);

        if (!File.Exists(filePath))
        {
            return CreateDefault();
        }

        try
        {
            using var stream = File.OpenRead(filePath);
            var data = JsonSerializer.Deserialize(stream, RepositoryJsonContext.Default.PresetData);

            if (IsValid(data))
            {
                return data!;
            }
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            // Invalid user-local state should not prevent the app from starting.
        }

        BackupRejectedFile(filePath);

        return CreateDefault();
    }

    public void SaveForCameraProfile(Guid cameraId, PresetData presetData)
    {
        var filePath = _pathForCamera(cameraId);
        var directory = Path.GetDirectoryName(filePath);

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var tempPath = Path.Combine(directory ?? string.Empty, $"{Path.GetFileName(filePath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            using (var stream = File.Create(tempPath))
            {
                JsonSerializer.Serialize(stream, data, RepositoryJsonContext.Default.PresetData);
            }

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

    private static void BackupRejectedFile(string filePath)
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
