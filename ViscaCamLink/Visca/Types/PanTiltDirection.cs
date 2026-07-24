namespace ViscaCamLink.Visca.Types;

using System;

[Flags]
public enum PanTiltDirection
{
    None = 0,
    PanLeft = 1,
    TiltUp = 2,
    PanRight = 4,
    TiltDown = 8,
    PanLeftTiltUp = PanLeft | TiltUp,
    PanLeftTiltDown = PanLeft | TiltDown,
    PanRightTiltUp = PanRight | TiltUp,
    PanRightTiltDown = PanRight | TiltDown
}
