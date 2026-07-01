namespace ViscaCamLink.ViewModels;

/// <summary>
/// Describes the visual state of the last-recalled preset indicator dot.
/// </summary>
public enum PresetIndicatorState
{
    /// <summary>No preset has been recalled, or the indicator was explicitly cleared.</summary>
    None,

    /// <summary>The camera is (or was last) positioned at the recalled preset. Shown as green.</summary>
    Active,

    /// <summary>The camera has moved/zoomed since the last preset recall. Shown as yellow.</summary>
    Moved,
}
