namespace ViscaCamLink.Repositories.Presets;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

using Microsoft.Extensions.Logging;

using ViscaCamLink.Infrastructure;
using ViscaCamLink.Repositories;
using ViscaCamLink.Resources;

public partial class PresetRepository(string directory, ILogger<PresetRepository> logger) : IPresetRepository
{
    public PresetRepository(ILogger<PresetRepository> logger) : this(AppPaths.PresetsDirectory, logger)
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

            LogInvalidPresetData(logger, filePath);
        }
        catch (Exception exception) when (exception is JsonException or IOException or UnauthorizedAccessException)
        {
            // Invalid user-local state should not prevent the app from starting.
            LogLoadFailed(logger, filePath, exception);
        }

        BackupRejectedFile(filePath);

        return CreateDefault();
    }

    public void SaveForCameraProfile(Guid profileId, PresetData presetData)
    {
        var filePath = GetPresetFilePath(profileId);
        var temporaryPath = $"{filePath}.{Guid.NewGuid():N}.tmp";

        try
        {
            Directory.CreateDirectory(directory);

            using (var stream = File.Create(temporaryPath))
            {
                JsonSerializer.Serialize(stream, presetData, RepositoryJsonContext.Default.PresetData);
            }

            File.Move(temporaryPath, filePath, overwrite: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            LogSaveFailed(logger, filePath, exception);

            throw;
        }
        finally
        {
            TryDelete(temporaryPath);
        }
    }

    private string GetPresetFilePath(Guid profileId) => Path.Combine(directory, $"presets-{profileId:N}.json");

    private void BackupRejectedFile(string filePath)
    {
        try
        {
            File.Copy(filePath, $"{filePath}.bak", overwrite: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Best effort only; startup recovery should not depend on backup success.
            LogBackupFailed(logger, filePath, exception);
        }
    }

    private void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Best effort only; a leftover temp file is harmless.
            LogTemporaryFileDeleteFailed(logger, path, exception);
        }
    }

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

    [LoggerMessage(EventId = 3201, Level = LogLevel.Warning, Message = "Preset file '{FilePath}' could not be read; falling back to defaults")]
    private static partial void LogLoadFailed(ILogger logger, string filePath, Exception exception);

    [LoggerMessage(EventId = 3202, Level = LogLevel.Warning, Message = "Preset file '{FilePath}' contains invalid data; falling back to defaults")]
    private static partial void LogInvalidPresetData(ILogger logger, string filePath);

    [LoggerMessage(EventId = 3203, Level = LogLevel.Error, Message = "Preset file '{FilePath}' could not be saved")]
    private static partial void LogSaveFailed(ILogger logger, string filePath, Exception exception);

    [LoggerMessage(EventId = 3204, Level = LogLevel.Warning, Message = "Rejected preset file '{FilePath}' could not be backed up")]
    private static partial void LogBackupFailed(ILogger logger, string filePath, Exception exception);

    [LoggerMessage(EventId = 3205, Level = LogLevel.Debug, Message = "Temporary preset file '{FilePath}' could not be deleted")]
    private static partial void LogTemporaryFileDeleteFailed(ILogger logger, string filePath, Exception exception);
}
