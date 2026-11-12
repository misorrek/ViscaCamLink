namespace ViscaCamLink.Simulator;

using System.Collections.Generic;

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
    public bool IsAutoFocus { get; set; } = true;
    public ushort Focus { get; set; }
    public sbyte FocusVelocity { get; set; }
    public byte Iris { get; set; } = ViscaConstants.DefaultIris;
    public byte ExposureMode { get; set; }
    public int LastRecalledPreset { get; set; } = -1;
    public int LastSavedPreset { get; set; } = -1;

    /// <summary>
    /// Stored preset positions (slot → pan, tilt, zoom).
    /// </summary>
    public Dictionary<int, (short Pan, short Tilt, short Zoom)> Presets { get; } = [];

    public void StopMovement()
    {
        PanVelocity = 0;
        TiltVelocity = 0;
        ZoomVelocity = 0;
    }

    public void ResetPosition()
    {
        Pan = 0;
        Tilt = 0;
        Zoom = 0;
        StopMovement();
    }
}
