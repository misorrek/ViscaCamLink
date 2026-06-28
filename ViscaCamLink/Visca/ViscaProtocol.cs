namespace ViscaCamLink.Visca;

public static class ViscaProtocol
{
    public const byte CameraAddress = 0x81;
    public const byte Terminator = 0xff;

    public const byte CommandPrefix = 0x01;
    public const byte InquiryPrefix = 0x09;

    public const byte CategoryCamera = 0x04;
    public const byte CategoryPanTilt = 0x06;

    public const byte CmdPower = 0x00;
    public const byte CmdZoomVariable = 0x07;
    public const byte CmdZoomDirect = 0x47;
    public const byte CmdMemoryReset = 0x3f;

    public const byte MemorySubSet = 0x01;
    public const byte MemorySubRecall = 0x02;

    public const byte CmdContinuousPanTilt = 0x01;
    public const byte CmdAbsolutePosition = 0x02;
    public const byte CmdRelativePosition = 0x03;
    public const byte CmdHome = 0x04;
    public const byte CmdPanTiltInquiry = 0x12;

    public const byte PowerOnArg = 0x02;
    public const byte PowerOffArg = 0x03;

    public const byte DirectionPositive = 0x02;
    public const byte DirectionNegative = 0x01;
    public const byte DirectionStop = 0x03;

    public const byte ZoomInMask = 0x20;
    public const byte ZoomOutMask = 0x30;
    public const byte ZoomStop = 0x00;

    public const byte MinSpeed = 0x01;
    public const byte MaxPanSpeed = 0x18;
    public const byte MaxTiltSpeed = 0x14;
    public const byte MaxZoomSpeed = 0x07;

    public static readonly TimeSpan DefaultCommandTimeout = TimeSpan.FromSeconds(15);

    public const int PowerStatusPollAttempts = 20;

    public const int PerOperationTimeoutMs = 1000;
    public const int PerOperationDelayMs = 2000;
}
