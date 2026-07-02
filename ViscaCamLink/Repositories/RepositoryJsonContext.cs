namespace ViscaCamLink.Repositories;

using System.Text.Json.Serialization;
using ViscaCamLink.Repositories.HotKeys;
using ViscaCamLink.Repositories.Presets;

[JsonSourceGenerationOptions(WriteIndented = true, UseStringEnumConverter = true)]
[JsonSerializable(typeof(List<HotKeyBinding>))]
[JsonSerializable(typeof(PresetData))]
internal sealed partial class RepositoryJsonContext : JsonSerializerContext
{
}