namespace ViscaCamLink.Repositories.Presets;

using System;
using System.Text.Json.Serialization;

public class PresetMetadata
{
    [JsonPropertyName("groupId")]
    public Guid GroupId { get; set; }

    [JsonPropertyName("slotIndex")]
    public int SlotIndex { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}
