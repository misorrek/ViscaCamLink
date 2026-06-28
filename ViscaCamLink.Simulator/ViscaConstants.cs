namespace ViscaCamLink.Simulator;

/// <summary>
/// VISCA protocol constants mirrored from the main app for independent operation.
/// </summary>
public static class ViscaConstants
{
    // Address & framing
    public const byte CameraAddress = 0x81;
    public const byte Terminator = 0xFF;

    // Prefix bytes
    public const byte CommandPrefix = 0x01;
    public const byte InquiryPrefix = 0x09;

    // Command categories
    public const byte CategoryCamera = 0x04;
    public const byte CategoryPanTilt = 0x06;

    // Camera commands
    public const byte CmdPower = 0x00;
    public const byte CmdZoomVariable = 0x07;
    public const byte CmdZoomDirect = 0x47;
    public const byte CmdMemoryReset = 0x3F;

    // Memory sub-commands
    public const byte MemorySubSet = 0x01;
    public const byte MemorySubRecall = 0x02;

    // Pan/Tilt commands
    public const byte CmdContinuousPanTilt = 0x01;
    public const byte CmdAbsolutePosition = 0x02;
    public const byte CmdRelativePosition = 0x03;
    public const byte CmdHome = 0x04;

    // Power arguments
    public const byte PowerOnArg = 0x02;
    public const byte PowerOffArg = 0x03;

    // Direction encoding
    public const byte DirectionPositive = 0x02;
    public const byte DirectionNegative = 0x01;
    public const byte DirectionStop = 0x03;

    // Zoom masks
    public const byte ZoomInMask = 0x20;
    public const byte ZoomOutMask = 0x30;
    public const byte ZoomStop = 0x00;

    // Speed limits
    public const byte MinSpeed = 0x01;
    public const byte MaxPanSpeed = 0x18;
    public const byte MaxTiltSpeed = 0x14;
    public const byte MaxZoomSpeed = 0x07;

    // Position limits (PTZOptics PT30X-NDI)
    public const short MinPan = -2448;
    public const short MaxPan = 2448;
    public const short MinTilt = -432;
    public const short MaxTilt = 1296;
    public const short MinZoom = 0;
    public const short MaxZoom = 16384;

    // Pan/Tilt inquiry
    public const byte InquiryPanTiltPosition = 0x12;
}
