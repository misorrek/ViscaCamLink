namespace ViscaCamLink.Repositories;

using System.Text.Json.Serialization;

public sealed class PresetData
{
    [JsonPropertyName("groups")]
    public List<PresetGroup> Groups { get; set; } = [];
}
