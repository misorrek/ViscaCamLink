namespace ViscaCamLink.Repositories.AppSettings;

using System.Collections.Generic;
using System.Text.Json.Serialization;

[JsonSourceGenerationOptions(WriteIndented = true, UseStringEnumConverter = true)]
[JsonSerializable(typeof(AppSettings))]
[JsonSerializable(typeof(CameraProfile))]
[JsonSerializable(typeof(List<CameraProfile>))]
[JsonSerializable(typeof(WindowPlacementData))]
internal partial class AppSettingsJsonContext : JsonSerializerContext
{
}
