namespace ViscaCamLink.Repositories.AppSettings;

using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Xml.Linq;

public sealed class AppSettingsRepository
{
    private readonly string _settingsFilePath;
    private readonly string _legacySearchRoot;

    public AppSettingsRepository(string settingsFilePath, string legacySearchRoot)
    {
        _settingsFilePath = settingsFilePath;
        _legacySearchRoot = legacySearchRoot;
    }

    public AppSettings Load()
    {
        if (File.Exists(_settingsFilePath))
        {
            return ReadSettings(_settingsFilePath);
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
        Directory.CreateDirectory(Path.GetDirectoryName(_settingsFilePath)!);

        using var stream = File.Create(_settingsFilePath);
        JsonSerializer.Serialize(stream, settings, AppSettingsJsonContext.Default.AppSettings);
    }

    private AppSettings ReadSettings(string filePath)
    {
        try
        {
            using var stream = File.OpenRead(filePath);
            var settings = JsonSerializer.Deserialize(stream, AppSettingsJsonContext.Default.AppSettings);
            return settings ?? new AppSettings();
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            var defaultSettings = new AppSettings();
            Save(defaultSettings);
            return defaultSettings;
        }
    }

    private AppSettings? TryLoadLegacySettings()
    {
        var legacyRoot = _legacySearchRoot;
        if (!Directory.Exists(legacyRoot))
        {
            return null;
        }

        var legacyFile = Directory
            .EnumerateFiles(legacyRoot, "user.config", SearchOption.AllDirectories)
            .Select(path => new FileInfo(path))
            .OrderByDescending(fileInfo => fileInfo.LastWriteTimeUtc)
            .FirstOrDefault();

        if (legacyFile is null)
        {
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

            ApplyLegacyValue(settings, values, nameof(AppSettings.Ip));
            ApplyLegacyValue(settings, values, nameof(AppSettings.Port));
            ApplyLegacyValue(settings, values, nameof(AppSettings.MemoryContainerVisible));
            ApplyLegacyValue(settings, values, nameof(AppSettings.MoveContainerVisible));
            ApplyLegacyValue(settings, values, nameof(AppSettings.ZoomContainerVisible));
            ApplyLegacyValue(settings, values, nameof(AppSettings.PanTiltSpeed));
            ApplyLegacyValue(settings, values, nameof(AppSettings.ZoomSpeed));
            ApplyLegacyValue(settings, values, nameof(AppSettings.Language));
            ApplyLegacyValue(settings, values, nameof(AppSettings.NumpadLayout));

            return settings;
        }
        catch
        {
            return null;
        }
    }

    private static void ApplyLegacyValue(AppSettings settings, IReadOnlyDictionary<string, string> values, string propertyName)
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

    private void DeleteLegacyUserDataPath()
    {
        try
        {
            if (Directory.Exists(_legacySearchRoot))
            {
                Directory.Delete(_legacySearchRoot, recursive: true);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }
}