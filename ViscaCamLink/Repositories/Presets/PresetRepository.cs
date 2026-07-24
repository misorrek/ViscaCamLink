namespace ViscaCamLink.Repositories.Presets;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

using ViscaCamLink.Infrastructure;
using ViscaCamLink.Repositories;
using ViscaCamLink.Resources;

public class PresetRepository(string directory) : IPresetRepository
{
    public PresetRepository() : this(AppPaths.PresetsDirectory)
    {
    }

    public PresetData LoadForCameraProfile(Guid profileId)
    {
        var filePath = GetPresetFilePath(profileId);

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
        catch (Exception exception) when (exception is JsonException or IOException or UnauthorizedAccessException)
        {
            // Invalid user-local state should not prevent the app from starting.
        }

        BackupRejectedFile(filePath);

        return CreateDefault();
    }

    public void SaveForCameraProfile(Guid profileId, PresetData presetData)
    {
        var filePath = GetPresetFilePath(profileId);
        var temporaryPath = $"{filePath}.{Guid.NewGuid():N}.tmp";

        Directory.CreateDirectory(directory);

        try
        {
            using (var stream = File.Create(temporaryPath))
            {
                JsonSerializer.Serialize(stream, presetData, RepositoryJsonContext.Default.PresetData);
            }

            File.Move(temporaryPath, filePath, overwrite: true);
        }
        finally
        {
            TryDelete(temporaryPath);
        }
    }

    private string GetPresetFilePath(Guid profileId) => Path.Combine(directory, $"presets-{profileId:N}.json");

    private static bool IsValid(PresetData? data)
    {
        if (data?.Groups is not { Count: > 0 })
        {
            return false;
        }

        var groupIds = new HashSet<Guid>();
        var usedSlots = new HashSet<int>();

        foreach (var group in data.Groups)
        {
            if (group is null || group.Id == Guid.Empty || group.Presets is null)
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
                    preset.SlotIndex >= PresetLayout.MaxCameraSlots ||
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
        try
        {
            File.Delete(path);
        }
        catch
        {
            // Best effort only; a leftover temp file is harmless.
        }
    }

    private static PresetData CreateDefault()
    {
        var groupId = Guid.NewGuid();
        var presets = new List<PresetMetadata>(PresetLayout.PresetsPerGroup);

        for (var slotIndex = 0; slotIndex < PresetLayout.PresetsPerGroup; slotIndex++)
        {
            presets.Add(new PresetMetadata
            {
                GroupId = groupId,
                SlotIndex = slotIndex,
                Name = slotIndex.ToString(),
            });
        }

        return new PresetData
        {
            Groups =
            [
                new PresetGroup
                {
                    Id = groupId,
                    Name = Strings.PresetGroup_DefaultName,
                    Presets = presets,
                },
            ],
        };
    }
}
