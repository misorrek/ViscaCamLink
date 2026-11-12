namespace ViscaCamLink.Simulator;

using System;

/// <summary>
/// Parses incoming VISCA packets and produces appropriate responses while updating camera state.
/// </summary>
public class ViscaCommandHandler(CameraState state, Action onStateChanged, Action<string> log)
{
    public byte[]? ProcessPacket(byte[] packet)
    {
        if (packet.Length < ViscaConstants.MinPacketLength || packet[0] != ViscaConstants.CameraAddress)
        {
            return SyntaxError();
        }

        var length = packet.Length - 1;

        return packet[1] switch
        {
            ViscaConstants.CommandPrefix => ProcessCommand(packet, length),
            ViscaConstants.InquiryPrefix => ProcessInquiry(packet, length),
            _ => SyntaxError()
        };
    }

    private static bool IsInVariableRange(byte argument, byte rangeStart) =>
        argument >= rangeStart && argument < rangeStart + ViscaConstants.VariableRangeEnd;

    private static byte[] Done() => [ViscaConstants.ResponseAddress, ViscaConstants.ResponseDone, ViscaConstants.Terminator];

    private static byte[] SyntaxError() => ErrorResponse(ViscaConstants.ErrorSyntax);

    private static byte[] CommandNotExecutable() => ErrorResponse(ViscaConstants.ErrorNotExecutable);

    private static byte[] ErrorResponse(byte errorCode) =>
        [ViscaConstants.ResponseAddress, ViscaConstants.ResponseError, errorCode, ViscaConstants.Terminator];

    private static byte[] QueryResponse(params byte[] data) =>
        [ViscaConstants.ResponseAddress, ViscaConstants.ResponseDone, .. data, ViscaConstants.Terminator];

    private static short DecodeInt16(byte[] data, int offset) =>
        (short)((data[offset] << 12) | (data[offset + 1] << 8) | (data[offset + 2] << 4) | data[offset + 3]);

    private static byte[] EncodeInt16(short value) =>
    [
        (byte)((value >> 12) & 0x0F),
        (byte)((value >> 8) & 0x0F),
        (byte)((value >> 4) & 0x0F),
        (byte)(value & 0x0F)
    ];

    private byte[] ProcessCommand(byte[] packet, int length)
    {
        return packet[2] switch
        {
            ViscaConstants.CategoryCamera => ProcessCameraCommand(packet, length),
            ViscaConstants.CategoryPanTilt => ProcessPanTiltCommand(packet, length),
            _ => CommandNotExecutable()
        };
    }

    private byte[] ProcessCameraCommand(byte[] packet, int length)
    {
        switch (packet[3])
        {
            case ViscaConstants.CmdPower:
                return length < ViscaConstants.ArgumentPacketLength ? SyntaxError() : ProcessPower(packet[4]);

            case ViscaConstants.CmdZoomVariable:
                return length < ViscaConstants.ArgumentPacketLength ? SyntaxError() : ProcessZoomVariable(packet[4]);

            case ViscaConstants.CmdZoomDirect:
                if (length < ViscaConstants.DirectPacketLength)
                {
                    return SyntaxError();
                }

                state.Zoom = Math.Clamp(DecodeInt16(packet, 4), ViscaConstants.MinZoom, ViscaConstants.MaxZoom);
                state.ZoomVelocity = 0;
                log($"CMD: Zoom Direct pos={state.Zoom}");
                onStateChanged();

                return Done();

            case ViscaConstants.CmdMemoryReset:
                return length < ViscaConstants.MemoryPacketLength ? SyntaxError() : ProcessMemory(packet[4], packet[5]);

            case ViscaConstants.CmdFocus:
                return length < ViscaConstants.ArgumentPacketLength ? SyntaxError() : ProcessFocusVariable(packet[4]);

            case ViscaConstants.CmdFocusDirect:
                if (length < ViscaConstants.DirectPacketLength)
                {
                    return SyntaxError();
                }

                state.Focus = (ushort)DecodeInt16(packet, 4);
                state.FocusVelocity = 0;
                log($"CMD: Focus Direct pos={state.Focus}");
                onStateChanged();

                return Done();

            case ViscaConstants.CmdFocusMode:
                return length < ViscaConstants.ArgumentPacketLength ? SyntaxError() : ProcessFocusMode(packet[4]);

            case ViscaConstants.CmdFocusOnePush:
                log("CMD: One-push AF trigger");

                return Done();

            case ViscaConstants.CmdIris:
                return length < ViscaConstants.ArgumentPacketLength ? SyntaxError() : ProcessIris(packet[4]);

            case ViscaConstants.CmdIrisDirect:
                return length < ViscaConstants.DirectPacketLength ? SyntaxError() : ProcessIrisDirect(packet);

            case ViscaConstants.CmdExposureMode:
                if (length < ViscaConstants.ArgumentPacketLength)
                {
                    return SyntaxError();
                }

                state.ExposureMode = packet[4];
                log($"CMD: AE mode 0x{packet[4]:X2}");
                onStateChanged();

                return Done();

            default:
                log($"CMD: Unknown camera cmd 0x{packet[3]:X2}");

                return CommandNotExecutable();
        }
    }

    private byte[] ProcessPower(byte argument)
    {
        if (argument == ViscaConstants.PowerOnArg)
        {
            state.IsPoweredOn = true;
            log("CMD: Power ON");
        }
        else if (argument == ViscaConstants.PowerOffArg)
        {
            state.IsPoweredOn = false;
            state.StopMovement();
            log("CMD: Power OFF");
        }

        onStateChanged();

        return Done();
    }

    private byte[] ProcessZoomVariable(byte argument)
    {
        var speed = argument & ViscaConstants.SpeedMask;

        if (argument == ViscaConstants.ZoomStop)
        {
            state.ZoomVelocity = 0;
            log("CMD: Zoom STOP");
        }
        else if (IsInVariableRange(argument, ViscaConstants.ZoomInMask))
        {
            state.ZoomVelocity = (sbyte)speed;
            log($"CMD: Zoom IN speed={speed}");
        }
        else if (IsInVariableRange(argument, ViscaConstants.ZoomOutMask))
        {
            state.ZoomVelocity = (sbyte)-speed;
            log($"CMD: Zoom OUT speed={speed}");
        }
        else if (argument == ViscaConstants.ZoomInStandard)
        {
            state.ZoomVelocity = (sbyte)ViscaConstants.MaxZoomSpeed;
            log("CMD: Zoom IN (standard)");
        }
        else if (argument == ViscaConstants.ZoomOutStandard)
        {
            state.ZoomVelocity = (sbyte)-ViscaConstants.MaxZoomSpeed;
            log("CMD: Zoom OUT (standard)");
        }

        onStateChanged();

        return Done();
    }

    private byte[] ProcessMemory(byte subCommand, byte slot)
    {
        if (subCommand == ViscaConstants.MemorySubSet)
        {
            state.Presets[slot] = (state.Pan, state.Tilt, state.Zoom);
            state.LastSavedPreset = slot;
            log($"CMD: Preset SAVE slot={slot}");
        }
        else if (subCommand == ViscaConstants.MemorySubRecall)
        {
            if (state.Presets.TryGetValue(slot, out var position))
            {
                state.Pan = position.Pan;
                state.Tilt = position.Tilt;
                state.Zoom = position.Zoom;
            }

            state.LastRecalledPreset = slot;
            state.StopMovement();
            log($"CMD: Preset RECALL slot={slot}");
        }

        onStateChanged();

        return Done();
    }

    private byte[] ProcessFocusVariable(byte argument)
    {
        var speed = (argument & ViscaConstants.SpeedMask) + 1;

        if (argument == ViscaConstants.FocusStop)
        {
            state.FocusVelocity = 0;
            log("CMD: Focus STOP");
        }
        else if (argument == ViscaConstants.FocusFarStandard || IsInVariableRange(argument, ViscaConstants.FocusFarVariable))
        {
            state.FocusVelocity = (sbyte)(argument == ViscaConstants.FocusFarStandard ? 1 : speed);
            log($"CMD: Focus FAR arg=0x{argument:X2}");
        }
        else if (argument == ViscaConstants.FocusNearStandard || IsInVariableRange(argument, ViscaConstants.FocusNearVariable))
        {
            state.FocusVelocity = (sbyte)-(argument == ViscaConstants.FocusNearStandard ? 1 : speed);
            log($"CMD: Focus NEAR arg=0x{argument:X2}");
        }
        else
        {
            return SyntaxError();
        }

        onStateChanged();

        return Done();
    }

    private byte[] ProcessFocusMode(byte argument)
    {
        switch (argument)
        {
            case ViscaConstants.FocusModeAuto:
                state.IsAutoFocus = true;
                break;
            case ViscaConstants.FocusModeManual:
                state.IsAutoFocus = false;
                break;
            case ViscaConstants.FocusModeToggle:
                state.IsAutoFocus = !state.IsAutoFocus;
                break;
            default:
                return SyntaxError();
        }

        log($"CMD: Focus mode {(state.IsAutoFocus ? "AUTO" : "MANUAL")}");
        onStateChanged();

        return Done();
    }

    private byte[] ProcessIris(byte argument)
    {
        if (!IsIrisControllable())
        {
            log("CMD: Iris rejected (AE mode is not manual/iris priority)");

            return CommandNotExecutable();
        }

        switch (argument)
        {
            case ViscaConstants.IrisReset:
                state.Iris = ViscaConstants.DefaultIris;
                break;
            case ViscaConstants.IrisUp:
                state.Iris = (byte)Math.Min(state.Iris + 1, ViscaConstants.MaxIris);
                break;
            case ViscaConstants.IrisDown:
                state.Iris = (byte)Math.Max(state.Iris - 1, 0);
                break;
            default:
                return SyntaxError();
        }

        log($"CMD: Iris 0x{argument:X2} → {state.Iris}");
        onStateChanged();

        return Done();
    }

    private byte[] ProcessIrisDirect(byte[] packet)
    {
        if (!IsIrisControllable())
        {
            log("CMD: Iris direct rejected (AE mode is not manual/iris priority)");

            return CommandNotExecutable();
        }

        state.Iris = (byte)Math.Min((packet[6] << 4) | packet[7], ViscaConstants.MaxIris);
        log($"CMD: Iris Direct pos={state.Iris}");
        onStateChanged();

        return Done();
    }

    private byte[] ProcessPanTiltCommand(byte[] packet, int length)
    {
        switch (packet[3])
        {
            case ViscaConstants.CmdContinuousPanTilt:
                return length < ViscaConstants.DirectPacketLength ? SyntaxError() : ProcessContinuousPanTilt(packet);

            case ViscaConstants.CmdAbsolutePosition:
                if (length < ViscaConstants.PositionPacketLength)
                {
                    return SyntaxError();
                }

                state.Pan = Math.Clamp(DecodeInt16(packet, 6), ViscaConstants.MinPan, ViscaConstants.MaxPan);
                state.Tilt = Math.Clamp(DecodeInt16(packet, 10), ViscaConstants.MinTilt, ViscaConstants.MaxTilt);
                state.PanVelocity = 0;
                state.TiltVelocity = 0;
                log($"CMD: Absolute pos=({state.Pan},{state.Tilt})");
                onStateChanged();

                return Done();

            case ViscaConstants.CmdRelativePosition:
                if (length < ViscaConstants.PositionPacketLength)
                {
                    return SyntaxError();
                }

                var deltaPan = DecodeInt16(packet, 6);
                var deltaTilt = DecodeInt16(packet, 10);

                state.Pan = Math.Clamp((short)(state.Pan + deltaPan), ViscaConstants.MinPan, ViscaConstants.MaxPan);
                state.Tilt = Math.Clamp((short)(state.Tilt + deltaTilt), ViscaConstants.MinTilt, ViscaConstants.MaxTilt);
                state.PanVelocity = 0;
                state.TiltVelocity = 0;
                log($"CMD: Relative delta=({deltaPan},{deltaTilt}) → pos=({state.Pan},{state.Tilt})");
                onStateChanged();

                return Done();

            case ViscaConstants.CmdHome:
                state.ResetPosition();
                log("CMD: HOME");
                onStateChanged();

                return Done();

            default:
                log($"CMD: Unknown pan/tilt cmd 0x{packet[3]:X2}");

                return CommandNotExecutable();
        }
    }

    private byte[] ProcessContinuousPanTilt(byte[] packet)
    {
        var panSign = packet[6] switch
        {
            ViscaConstants.DirectionNegative => -1,
            ViscaConstants.DirectionPositive => 1,
            _ => 0
        };

        // Tilt direction is inverted in the VISCA protocol
        var tiltSign = packet[7] switch
        {
            ViscaConstants.DirectionNegative => 1,
            ViscaConstants.DirectionPositive => -1,
            _ => 0
        };

        state.PanVelocity = (sbyte)(packet[4] * panSign);
        state.TiltVelocity = (sbyte)(packet[5] * tiltSign);
        log($"CMD: Pan/Tilt vel=({state.PanVelocity},{state.TiltVelocity})");
        onStateChanged();

        return Done();
    }

    private byte[] ProcessInquiry(byte[] packet, int length)
    {
        if (length < ViscaConstants.MinInquiryLength)
        {
            return SyntaxError();
        }

        return (packet[2], packet[3]) switch
        {
            (ViscaConstants.CategoryCamera, ViscaConstants.CmdPower) =>
                QueryResponse(state.IsPoweredOn ? ViscaConstants.PowerStatusOn : ViscaConstants.PowerStatusOff),
            (ViscaConstants.CategoryCamera, ViscaConstants.CmdZoomDirect) =>
                QueryResponse(EncodeInt16(state.Zoom)),
            (ViscaConstants.CategoryPanTilt, ViscaConstants.InquiryPanTiltPosition) =>
                QueryResponse([.. EncodeInt16(state.Pan), .. EncodeInt16(state.Tilt)]),
            _ => CommandNotExecutable()
        };
    }

    private bool IsIrisControllable() =>
        state.ExposureMode is ViscaConstants.ExposureModeManual or ViscaConstants.ExposureModeIrisPriority;
}
