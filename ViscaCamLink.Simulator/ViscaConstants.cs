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
    public const byte CmdFocus = 0x08;
    public const byte CmdFocusDirect = 0x48;
    public const byte CmdFocusMode = 0x38;
    public const byte CmdFocusOnePush = 0x18;
    public const byte CmdIris = 0x0B;
    public const byte CmdIrisDirect = 0x4B;
    public const byte CmdExposureMode = 0x39;

    // Focus / iris / exposure arguments
    public const byte FocusModeAuto = 0x02;
    public const byte FocusModeManual = 0x03;
    public const byte FocusModeToggle = 0x10;
    public const byte IrisReset = 0x00;
    public const byte IrisUp = 0x02;
    public const byte IrisDown = 0x03;
    public const byte ExposureModeManual = 0x03;
    public const byte ExposureModeIrisPriority = 0x0B;
    public const byte MaxIris = 0x11;
    public const byte DefaultIris = 0x0A;

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

    // Variable speed arguments
    public const byte SpeedMask = 0x0F;
    public const byte ZoomInStandard = 0x02;
    public const byte ZoomOutStandard = 0x03;
    public const byte FocusStop = 0x00;
    public const byte FocusFarStandard = 0x02;
    public const byte FocusNearStandard = 0x03;
    public const byte FocusFarVariable = 0x20;
    public const byte FocusNearVariable = 0x30;
    public const byte VariableRangeEnd = 0x10;

    // Responses
    public const byte ResponseAddress = 0x90;
    public const byte ResponseDone = 0x50;
    public const byte ResponseError = 0x60;
    public const byte ErrorSyntax = 0x02;
    public const byte ErrorNotExecutable = 0x41;
    public const byte PowerStatusOn = 0x02;
    public const byte PowerStatusOff = 0x03;

    // Packet lengths (without terminator)
    public const int MinPacketLength = 5;
    public const int MinInquiryLength = 4;
    public const int ArgumentPacketLength = 5;
    public const int MemoryPacketLength = 6;
    public const int DirectPacketLength = 8;
    public const int PositionPacketLength = 14;

    // Simulator defaults
    public const int DefaultPort = 5678;
}
