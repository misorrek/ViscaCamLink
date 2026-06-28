namespace ViscaCamLink.Simulator;

/// <summary>
/// Parses incoming VISCA packets and produces appropriate responses while updating camera state.
/// </summary>
public class ViscaCommandHandler
{
    private readonly CameraState _state;
    private readonly Action _onStateChanged;
    private readonly Action<string> _log;

    public ViscaCommandHandler(CameraState state, Action onStateChanged, Action<string> log)
    {
        _state = state;
        _onStateChanged = onStateChanged;
        _log = log;
    }

    public byte[]? ProcessPacket(byte[] packet)
    {
        // Minimum packet: address + prefix + category + command + terminator = 5 bytes
        if (packet.Length < 5 || packet[0] != ViscaConstants.CameraAddress)
        {
            return SyntaxError();
        }

        // Remove terminator for easier processing
        int len = packet.Length - 1; // exclude 0xFF terminator

        return packet[1] switch
        {
            ViscaConstants.CommandPrefix => ProcessCommand(packet, len),
            ViscaConstants.InquiryPrefix => ProcessInquiry(packet, len),
            _ => SyntaxError()
        };
    }

    private byte[] ProcessCommand(byte[] packet, int len)
    {
        return packet[2] switch
        {
            ViscaConstants.CategoryCamera => ProcessCameraCommand(packet, len),
            ViscaConstants.CategoryPanTilt => ProcessPanTiltCommand(packet, len),
            _ => CommandNotExecutable()
        };
    }

    private byte[] ProcessCameraCommand(byte[] packet, int len)
    {
        switch (packet[3])
        {
            // Power
            case ViscaConstants.CmdPower:
                if (len < 5) return SyntaxError();
                if (packet[4] == ViscaConstants.PowerOnArg)
                {
                    _state.IsPoweredOn = true;
                    _log("CMD: Power ON");
                }
                else if (packet[4] == ViscaConstants.PowerOffArg)
                {
                    _state.IsPoweredOn = false;
                    _state.PanVelocity = 0;
                    _state.TiltVelocity = 0;
                    _state.ZoomVelocity = 0;
                    _log("CMD: Power OFF");
                }
                _onStateChanged();
                return Done();

            // Zoom variable (continuous)
            case ViscaConstants.CmdZoomVariable:
                if (len < 5) return SyntaxError();
                byte zoomArg = packet[4];
                if (zoomArg == ViscaConstants.ZoomStop)
                {
                    _state.ZoomVelocity = 0;
                    _log("CMD: Zoom STOP");
                }
                else if (zoomArg >= 0x20 && zoomArg < 0x30)
                {
                    _state.ZoomVelocity = (sbyte)(zoomArg & 0x0F);
                    _log($"CMD: Zoom IN speed={zoomArg & 0x0F}");
                }
                else if (zoomArg >= 0x30 && zoomArg < 0x40)
                {
                    _state.ZoomVelocity = (sbyte)-(zoomArg & 0x0F);
                    _log($"CMD: Zoom OUT speed={zoomArg & 0x0F}");
                }
                else if (zoomArg == 0x02)
                {
                    _state.ZoomVelocity = (sbyte)ViscaConstants.MaxZoomSpeed;
                    _log("CMD: Zoom IN (standard)");
                }
                else if (zoomArg == 0x03)
                {
                    _state.ZoomVelocity = (sbyte)-ViscaConstants.MaxZoomSpeed;
                    _log("CMD: Zoom OUT (standard)");
                }
                _onStateChanged();
                return Done();

            // Zoom direct (absolute)
            case ViscaConstants.CmdZoomDirect:
                if (len < 8) return SyntaxError();
                short zoomPos = DecodeInt16(packet, 4);
                zoomPos = Math.Clamp(zoomPos, ViscaConstants.MinZoom, ViscaConstants.MaxZoom);
                _state.Zoom = zoomPos;
                _state.ZoomVelocity = 0;
                _log($"CMD: Zoom Direct pos={zoomPos}");
                _onStateChanged();
                return Done();

            // Memory (preset)
            case ViscaConstants.CmdMemoryReset:
                if (len < 6) return SyntaxError();
                byte memorySub = packet[4];
                byte slot = packet[5];

                if (memorySub == ViscaConstants.MemorySubSet)
                {
                    _state.Presets[slot] = (_state.Pan, _state.Tilt, _state.Zoom);
                    _state.LastSavedPreset = slot;
                    _log($"CMD: Preset SAVE slot={slot}");
                }
                else if (memorySub == ViscaConstants.MemorySubRecall)
                {
                    if (_state.Presets.TryGetValue(slot, out var pos))
                    {
                        _state.Pan = pos.Pan;
                        _state.Tilt = pos.Tilt;
                        _state.Zoom = pos.Zoom;
                    }
                    _state.LastRecalledPreset = slot;
                    _state.PanVelocity = 0;
                    _state.TiltVelocity = 0;
                    _state.ZoomVelocity = 0;
                    _log($"CMD: Preset RECALL slot={slot}");
                }
                _onStateChanged();
                return Done();

            default:
                _log($"CMD: Unknown camera cmd 0x{packet[3]:X2}");
                return CommandNotExecutable();
        }
    }

    private byte[] ProcessPanTiltCommand(byte[] packet, int len)
    {
        switch (packet[3])
        {
            // Continuous pan/tilt
            case ViscaConstants.CmdContinuousPanTilt:
                if (len < 8) return SyntaxError();
                byte panSpeed = packet[4];
                byte tiltSpeed = packet[5];
                byte panDir = packet[6];
                byte tiltDir = packet[7];

                int panSign = panDir switch
                {
                    ViscaConstants.DirectionNegative => -1,
                    ViscaConstants.DirectionPositive => 1,
                    ViscaConstants.DirectionStop => 0,
                    _ => 0
                };
                // Tilt direction is inverted in VISCA protocol
                int tiltSign = tiltDir switch
                {
                    ViscaConstants.DirectionNegative => 1,
                    ViscaConstants.DirectionPositive => -1,
                    ViscaConstants.DirectionStop => 0,
                    _ => 0
                };

                _state.PanVelocity = (sbyte)(panSpeed * panSign);
                _state.TiltVelocity = (sbyte)(tiltSpeed * tiltSign);
                _log($"CMD: Pan/Tilt vel=({_state.PanVelocity},{_state.TiltVelocity})");
                _onStateChanged();
                return Done();

            // Absolute position
            case ViscaConstants.CmdAbsolutePosition:
                if (len < 14) return SyntaxError();
                short absPan = DecodeInt16(packet, 6);
                short absTilt = DecodeInt16(packet, 10);
                absPan = Math.Clamp(absPan, ViscaConstants.MinPan, ViscaConstants.MaxPan);
                absTilt = Math.Clamp(absTilt, ViscaConstants.MinTilt, ViscaConstants.MaxTilt);
                _state.Pan = absPan;
                _state.Tilt = absTilt;
                _state.PanVelocity = 0;
                _state.TiltVelocity = 0;
                _log($"CMD: Absolute pos=({absPan},{absTilt})");
                _onStateChanged();
                return Done();

            // Relative position
            case ViscaConstants.CmdRelativePosition:
                if (len < 14) return SyntaxError();
                short relPan = DecodeInt16(packet, 6);
                short relTilt = DecodeInt16(packet, 10);
                _state.Pan = Math.Clamp((short)(_state.Pan + relPan), ViscaConstants.MinPan, ViscaConstants.MaxPan);
                _state.Tilt = Math.Clamp((short)(_state.Tilt + relTilt), ViscaConstants.MinTilt, ViscaConstants.MaxTilt);
                _state.PanVelocity = 0;
                _state.TiltVelocity = 0;
                _log($"CMD: Relative delta=({relPan},{relTilt}) → pos=({_state.Pan},{_state.Tilt})");
                _onStateChanged();
                return Done();

            // Home
            case ViscaConstants.CmdHome:
                _state.Pan = 0;
                _state.Tilt = 0;
                _state.Zoom = 0;
                _state.PanVelocity = 0;
                _state.TiltVelocity = 0;
                _state.ZoomVelocity = 0;
                _log("CMD: HOME");
                _onStateChanged();
                return Done();

            default:
                _log($"CMD: Unknown pan/tilt cmd 0x{packet[3]:X2}");
                return CommandNotExecutable();
        }
    }

    private byte[] ProcessInquiry(byte[] packet, int len)
    {
        if (len < 4) return SyntaxError();

        return (packet[2], packet[3]) switch
        {
            // Power status
            (ViscaConstants.CategoryCamera, ViscaConstants.CmdPower) =>
                QueryResponse(_state.IsPoweredOn ? (byte)0x02 : (byte)0x03),

            // Zoom position
            (ViscaConstants.CategoryCamera, ViscaConstants.CmdZoomDirect) =>
                QueryResponseInt16(_state.Zoom),

            // Pan/Tilt position
            (ViscaConstants.CategoryPanTilt, ViscaConstants.InquiryPanTiltPosition) =>
                QueryResponseInt16Int16(_state.Pan, _state.Tilt),

            _ => CommandNotExecutable()
        };
    }

    // --- Response helpers ---

    private static byte[] Done() => [0x90, 0x50, 0xFF];

    private static byte[] SyntaxError() => [0x90, 0x60, 0x02, 0xFF];

    private static byte[] CommandNotExecutable() => [0x90, 0x60, 0x41, 0xFF];

    private static byte[] QueryResponse(params byte[] data)
    {
        var result = new byte[2 + data.Length + 1];
        result[0] = 0x90;
        result[1] = 0x50;
        Array.Copy(data, 0, result, 2, data.Length);
        result[^1] = 0xFF;
        return result;
    }

    private static byte[] QueryResponseInt16(short value)
    {
        var nibbles = EncodeInt16(value);
        return QueryResponse(nibbles);
    }

    private static byte[] QueryResponseInt16Int16(short v1, short v2)
    {
        var n1 = EncodeInt16(v1);
        var n2 = EncodeInt16(v2);
        var data = new byte[8];
        Array.Copy(n1, 0, data, 0, 4);
        Array.Copy(n2, 0, data, 4, 4);
        return QueryResponse(data);
    }

    private static short DecodeInt16(byte[] data, int offset)
    {
        return (short)(
            (data[offset] << 12) |
            (data[offset + 1] << 8) |
            (data[offset + 2] << 4) |
            data[offset + 3]);
    }

    private static byte[] EncodeInt16(short value)
    {
        return
        [
            (byte)((value >> 12) & 0x0F),
            (byte)((value >> 8) & 0x0F),
            (byte)((value >> 4) & 0x0F),
            (byte)(value & 0x0F)
        ];
    }
}
