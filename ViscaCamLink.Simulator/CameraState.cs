namespace ViscaCamLink.Simulator;

/// <summary>
/// Represents the simulated camera's internal state.
/// </summary>
public class CameraState
{
    public bool IsPoweredOn { get; set; } = true;
    public short Pan { get; set; }
    public short Tilt { get; set; }
    public short Zoom { get; set; }
    public sbyte PanVelocity { get; set; }
    public sbyte TiltVelocity { get; set; }
    public sbyte ZoomVelocity { get; set; }
    public int LastRecalledPreset { get; set; } = -1;
    public int LastSavedPreset { get; set; } = -1;

    /// <summary>
    /// Stored preset positions (slot → pan, tilt, zoom).
    /// </summary>
    public Dictionary<int, (short Pan, short Tilt, short Zoom)> Presets { get; } = new();
}
