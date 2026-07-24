namespace ViscaCamLink.Repositories.Presets;

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

public class PresetData
{
    [JsonPropertyName("activeGroupId")]
    public Guid ActiveGroupId { get; set; }

    [JsonPropertyName("groups")]
    public List<PresetGroup> Groups { get; set; } = [];
}
