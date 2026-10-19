namespace ViscaCamLink.Repositories.AppSettings;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;

using Microsoft.Extensions.Logging;

public partial class AppSettingsRepository(
    string settingsFilePath,
    string legacySearchRoot,
    ILogger<AppSettingsRepository> logger)
{
    public AppSettings Load()
    {
        if (File.Exists(settingsFilePath))
        {
            return ReadSettings(settingsFilePath);
        }

        var migratedSettings = TryLoadLegacySettings();

        if (migratedSettings is not null)
        {
            Save(migratedSettings);
            DeleteLegacyUserDataPath();

            return migratedSettings;
        }

        var defaultSettings = new AppSettings();

        Save(defaultSettings);

        return defaultSettings;
    }

    public void Save(AppSettings settings)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(settingsFilePath)!);

            using var stream = File.Create(settingsFilePath);
            JsonSerializer.Serialize(stream, settings, AppSettingsJsonContext.Default.AppSettings);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            LogSaveFailed(logger, settingsFilePath, exception);

            throw;
        }
    }

    private AppSettings ReadSettings(string filePath)
    {
        try
        {
            using var stream = File.OpenRead(filePath);
            var settings = JsonSerializer.Deserialize(stream, AppSettingsJsonContext.Default.AppSettings);

            return settings ?? new AppSettings();
        }
        catch (Exception exception) when (exception is JsonException or IOException or UnauthorizedAccessException)
        {
            LogLoadFailed(logger, filePath, exception);

            var defaultSettings = new AppSettings();

            Save(defaultSettings);

            return defaultSettings;
        }
    }

    private AppSettings? TryLoadLegacySettings()
    {
        if (!Directory.Exists(legacySearchRoot))
        {
            LogNoLegacySettings(logger, legacySearchRoot);

            return null;
        }

        var legacyFile = Directory
            .EnumerateFiles(legacySearchRoot, "user.config", SearchOption.AllDirectories)
            .Select(path => new FileInfo(path))
            .OrderByDescending(fileInfo => fileInfo.LastWriteTimeUtc)
            .FirstOrDefault();

        if (legacyFile is null)
        {
            LogNoLegacySettings(logger, legacySearchRoot);

            return null;
        }

        try
        {
            var document = XDocument.Load(legacyFile.FullName);
            var settings = new AppSettings();
            var values = document
                .Descendants()
                .Where(element => element.Name.LocalName == "setting")
                .Select(element => new
                {
                    Name = element.Attribute("name")?.Value,
                    Value = element.Descendants().FirstOrDefault(child => child.Name.LocalName == "value")?.Value,
                })
                .Where(item => !string.IsNullOrWhiteSpace(item.Name))
                .ToDictionary(item => item.Name!, item => item.Value ?? string.Empty, StringComparer.OrdinalIgnoreCase);

            MigrateCameraProfile(settings, values);

            ApplyLegacyValue(settings, values, nameof(AppSettings.MemoryContainerVisible));
            ApplyLegacyValue(settings, values, nameof(AppSettings.MoveContainerVisible));
            ApplyLegacyValue(settings, values, nameof(AppSettings.ZoomContainerVisible));
            ApplyLegacyValue(settings, values, nameof(AppSettings.PanTiltSpeed));
            ApplyLegacyValue(settings, values, nameof(AppSettings.ZoomSpeed));
            ApplyLegacyValue(settings, values, nameof(AppSettings.Language));

            LogLegacySettingsMigrated(logger, legacyFile.FullName, values.Count);

            return settings;
        }
        catch (Exception exception) when (exception is XmlException or IOException or UnauthorizedAccessException or ArgumentException)
        {
            // Unreadable legacy settings are not worth failing startup for; fresh defaults are used instead.
            LogLegacyMigrationFailed(logger, legacyFile.FullName, exception);

            return null;
        }
    }

    private void DeleteLegacyUserDataPath()
    {
        try
        {
            if (Directory.Exists(legacySearchRoot))
            {
                Directory.Delete(legacySearchRoot, recursive: true);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // The legacy directory is only cleaned up as a courtesy; leaving it behind is harmless.
            LogLegacyCleanupFailed(logger, legacySearchRoot, exception);
        }
    }

    private static void MigrateCameraProfile(AppSettings settings, Dictionary<string, string> values)
    {
        var profile = new CameraProfile();

        if (values.TryGetValue("Ip", out var legacyIp) && !string.IsNullOrWhiteSpace(legacyIp))
        {
            profile.Ip = legacyIp;
        }

        if (values.TryGetValue("Port", out var legacyPort)
            && int.TryParse(legacyPort, NumberStyles.Integer, CultureInfo.InvariantCulture, out var legacyPortNumber))
        {
            profile.Port = legacyPortNumber;
        }

        settings.CameraProfiles.Add(profile);
        settings.ActiveCameraProfileId = profile.Id;
    }

    private static void ApplyLegacyValue(AppSettings settings, Dictionary<string, string> values, string propertyName)
    {
        if (!values.TryGetValue(propertyName, out var rawValue))
        {
            return;
        }

        var property = typeof(AppSettings).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);

        if (property is null || !property.CanWrite)
        {
            return;
        }

        object? convertedValue = property.PropertyType switch
        {
            var type when type == typeof(string) => rawValue,
            var type when type == typeof(int) => int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue) ? intValue : null,
            var type when type == typeof(bool) => bool.TryParse(rawValue, out var boolValue) ? boolValue : null,
            var type when type.IsEnum => ParseEnum(type, rawValue),
            _ => null,
        };

        if (convertedValue is not null)
        {
            property.SetValue(settings, convertedValue);
        }
    }

    private static object? ParseEnum(Type enumType, string rawValue)
    {
        if (int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numericValue))
        {
            return Enum.ToObject(enumType, numericValue);
        }

        return Enum.TryParse(enumType, rawValue, ignoreCase: true, out var parsedValue) ? parsedValue : null;
    }

    [LoggerMessage(EventId = 3001, Level = LogLevel.Warning, Message = "Settings file '{FilePath}' could not be read; falling back to defaults")]
    private static partial void LogLoadFailed(ILogger logger, string filePath, Exception exception);

    [LoggerMessage(EventId = 3002, Level = LogLevel.Error, Message = "Settings file '{FilePath}' could not be saved")]
    private static partial void LogSaveFailed(ILogger logger, string filePath, Exception exception);

    [LoggerMessage(EventId = 3003, Level = LogLevel.Debug, Message = "No legacy user.config found under '{SearchRoot}'")]
    private static partial void LogNoLegacySettings(ILogger logger, string searchRoot);

    [LoggerMessage(EventId = 3004, Level = LogLevel.Information, Message = "Migrated legacy settings from '{FilePath}' ({ValueCount} values found)")]
    private static partial void LogLegacySettingsMigrated(ILogger logger, string filePath, int valueCount);

    [LoggerMessage(EventId = 3005, Level = LogLevel.Warning, Message = "Legacy settings '{FilePath}' could not be migrated; using defaults")]
    private static partial void LogLegacyMigrationFailed(ILogger logger, string filePath, Exception exception);

    [LoggerMessage(EventId = 3006, Level = LogLevel.Warning, Message = "Legacy user data directory '{DirectoryPath}' could not be deleted")]
    private static partial void LogLegacyCleanupFailed(ILogger logger, string directoryPath, Exception exception);
}
