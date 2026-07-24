namespace ViscaCamLink.Repositories.Presets;

using System.Collections.Generic;
using System.Text.Json.Serialization;

public class PresetData
{
    [JsonPropertyName("groups")]
    public List<PresetGroup> Groups { get; set; } = [];
}
