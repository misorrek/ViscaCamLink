namespace ViscaCamLink.Repositories;

using System.Text.Json.Serialization;

[JsonSourceGenerationOptions(WriteIndented = true, UseStringEnumConverter = true)]
[JsonSerializable(typeof(List<HotKeyBinding>))]
[JsonSerializable(typeof(PresetData))]
internal sealed partial class RepositoryJsonContext : JsonSerializerContext
{
}