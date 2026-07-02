namespace ViscaCamLink.Repositories.Presets;

using System.Text.Json.Serialization;

public sealed class PresetMetadata
{
    [JsonPropertyName("groupId")]
    public string GroupId { get; set; } = string.Empty;

    [JsonPropertyName("slotIndex")]
    public int SlotIndex { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}
