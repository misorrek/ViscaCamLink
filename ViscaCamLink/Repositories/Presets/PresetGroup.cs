namespace ViscaCamLink.Repositories.Presets;

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

public class PresetGroup
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("presets")]
    public List<PresetMetadata> Presets { get; set; } = [];
}
