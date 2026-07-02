namespace ViscaCamLink.Repositories.AppSettings;

using System.Text.Json.Serialization;

[JsonSourceGenerationOptions(WriteIndented = true, UseStringEnumConverter = true)]
[JsonSerializable(typeof(AppSettings))]
[JsonSerializable(typeof(WindowPlacementData))]
internal sealed partial class AppSettingsJsonContext : JsonSerializerContext
{
}