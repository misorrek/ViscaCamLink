namespace ViscaCamLink.Repositories.Presets;

using System.Text.Json.Serialization;

public sealed class PresetGroup
{
    // TODO : Would it be better to use a Guid?
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("presets")]
    public List<PresetMetadata> Presets { get; set; } = [];
}
