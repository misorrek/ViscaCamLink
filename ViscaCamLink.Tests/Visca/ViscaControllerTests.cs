namespace ViscaCamLink.Tests.Visca;

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using Shouldly;

using ViscaCamLink.Visca;
using ViscaCamLink.Visca.Types;

using Xunit;

public sealed class ViscaControllerTests
{
    private readonly Mock<IViscaClient> _viscaClient = new();
    private readonly ViscaController _controller;

    public ViscaControllerTests()
    {
        _viscaClient
            .Setup(c => c.SendAsync(It.IsAny<ViscaPacket>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateAckPacket());

        _controller = new ViscaController(_viscaClient.Object, NullLogger<ViscaController>.Instance);
    }

    [Fact]
    public void Connected_Success()
    {
        _viscaClient.Setup(c => c.IsConnected()).Returns(true);

        _controller.Connected.ShouldBe(true);
    }

    [Fact]
    public void MaxPanSpeed_Success()
    {
        ((IViscaController)_controller).MaxPanSpeed.ShouldBe(ViscaProtocol.MaxPanSpeed);
    }

    [Fact]
    public void MaxTiltSpeed_Success()
    {
        ((IViscaController)_controller).MaxTiltSpeed.ShouldBe(ViscaProtocol.MaxTiltSpeed);
    }

    [Fact]
    public void MaxZoomSpeed_Success()
    {
        ((IViscaController)_controller).MaxZoomSpeed.ShouldBe(ViscaProtocol.MaxZoomSpeed);
    }

    [Fact]
    public async Task ReconnectAsync_Success()
    {
        await _controller.ReconnectAsync("10.0.0.1", 5678);

        _viscaClient.Verify(c => c.ReconnectAsync("10.0.0.1", 5678, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PowerOnAsync_Success()
    {
        var sent = CapturePacket();

        await _controller.PowerOnAsync();

        sent.Value.ShouldNotBeNull();
        AssertPacketBytes(sent.Value!.Value, [
            ViscaProtocol.CameraAddress, ViscaProtocol.CommandPrefix, ViscaProtocol.CategoryCamera,
            ViscaProtocol.CmdPower, ViscaProtocol.PowerOnArg, ViscaProtocol.Terminator]);
    }

    [Fact]
    public async Task PowerOffAsync_Success()
    {
        var sent = CapturePacket();

        await _controller.PowerOffAsync();

        sent.Value.ShouldNotBeNull();
        AssertPacketBytes(sent.Value!.Value, [
            ViscaProtocol.CameraAddress, ViscaProtocol.CommandPrefix, ViscaProtocol.CategoryCamera,
            ViscaProtocol.CmdPower, ViscaProtocol.PowerOffArg, ViscaProtocol.Terminator]);
    }

    [Theory]
    [InlineData(PowerStatus.On)]
    [InlineData(PowerStatus.Standby)]
    public async Task GetPowerStatusAsync_Success(PowerStatus powerStatus)
    {
        SetupResponse([0x90, 0x50, (byte)powerStatus, 0xff]);

        var status = await _controller.GetPowerStatusAsync();

        status.ShouldBe(powerStatus);
    }

    [Fact]
    public async Task GetPowerStatusAsync_WhenResponseIsTooShort_ThrowsViscaProtocolException()
    {
        SetupResponse([0x90, 0x50, 0xff]);

        Task<PowerStatus> act() => _controller.GetPowerStatusAsync();

        await Should.ThrowAsync<ViscaProtocolException>((Func<Task<PowerStatus>>)act);
    }

    [Fact]
    public async Task GetPowerStatusAsync_WhenStatusValueIsUnknown_ThrowsViscaProtocolException()
    {
        SetupResponse([0x90, 0x50, 0x7f, 0xff]);

        Task<PowerStatus> act() => _controller.GetPowerStatusAsync();

        await Should.ThrowAsync<ViscaProtocolException>((Func<Task<PowerStatus>>)act);
    }

    [Fact]
    public async Task GetUpdatedPowerStatusAsync_Success()
    {
        SetupResponse([0x90, 0x50, (byte)PowerStatus.Standby, 0xff]);

        var status = await _controller.GetUpdatedPowerStatusAsync(PowerStatus.On);

        status.ShouldBe(PowerStatus.Standby);
    }

    [Fact]
    public async Task GetUpdatedPowerStatusAsync_WhenCancelledWhileStatusIsUnchanged_ThrowsOperationCanceledException()
    {
        SetupResponse([0x90, 0x50, (byte)PowerStatus.On, 0xff]);

        using var cancellationSource = new CancellationTokenSource();

        cancellationSource.Cancel();

        Task<PowerStatus> act() => _controller.GetUpdatedPowerStatusAsync(PowerStatus.On, cancellationSource.Token);

        await Should.ThrowAsync<OperationCanceledException>((Func<Task<PowerStatus>>)act);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    public async Task MemorySetAsync_Success(byte slot)
    {
        var sent = CapturePacket();

        await _controller.MemorySetAsync(slot);

        sent.Value.ShouldNotBeNull();
        AssertPacketBytes(sent.Value!.Value, [
            ViscaProtocol.CameraAddress, ViscaProtocol.CommandPrefix, ViscaProtocol.CategoryCamera,
            ViscaProtocol.CmdMemoryReset, ViscaProtocol.MemorySubSet, slot, ViscaProtocol.Terminator]);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    public async Task MemoryRecallAsync_Success(byte slot)
    {
        var sent = CapturePacket();

        await _controller.MemoryRecallAsync(slot);

        sent.Value.ShouldNotBeNull();
        AssertPacketBytes(sent.Value!.Value, [
            ViscaProtocol.CameraAddress, ViscaProtocol.CommandPrefix, ViscaProtocol.CategoryCamera,
            ViscaProtocol.CmdMemoryReset, ViscaProtocol.MemorySubRecall, slot, ViscaProtocol.Terminator]);
    }

    [Fact]
    public async Task GoHomeAsync_Success()
    {
        var sent = CapturePacket();

        await _controller.GoHomeAsync();

        sent.Value.ShouldNotBeNull();
        AssertPacketBytes(sent.Value!.Value, [
            ViscaProtocol.CameraAddress, ViscaProtocol.CommandPrefix, ViscaProtocol.CategoryPanTilt,
            ViscaProtocol.CmdHome, ViscaProtocol.Terminator]);
    }

    [Theory]
    [InlineData(PanTiltDirection.PanRightTiltUp, 0x10, 0x08, ViscaProtocol.DirectionPositive, ViscaProtocol.DirectionNegative)]
    [InlineData(PanTiltDirection.PanLeftTiltDown, 0x05, 0x03, ViscaProtocol.DirectionNegative, ViscaProtocol.DirectionPositive)]
    [InlineData(PanTiltDirection.None, 0x00, 0x00, ViscaProtocol.DirectionStop, ViscaProtocol.DirectionStop)]
    public async Task ContinuousPanTiltAsync_Success(
        PanTiltDirection direction,
        byte panSpeed,
        byte tiltSpeed,
        byte expectedPanByte,
        byte expectedTiltByte)
    {
        var sent = CapturePacket();

        await _controller.ContinuousPanTiltAsync(direction, panSpeed, tiltSpeed);

        sent.Value.ShouldNotBeNull();
        AssertPacketBytes(sent.Value!.Value, [
            ViscaProtocol.CameraAddress, ViscaProtocol.CommandPrefix, ViscaProtocol.CategoryPanTilt,
            ViscaProtocol.CmdContinuousPanTilt, panSpeed, tiltSpeed,
            expectedPanByte, expectedTiltByte, ViscaProtocol.Terminator]);
    }

    [Theory]
    [InlineData(ZoomDirection.In, 0x05, ViscaProtocol.ZoomInMask | 0x05)]
    [InlineData(ZoomDirection.Out, 0x03, ViscaProtocol.ZoomOutMask | 0x03)]
    [InlineData(ZoomDirection.None, 0x00, ViscaProtocol.ZoomStop)]
    public async Task ContinuousZoomAsync_Success(ZoomDirection direction, byte zoomSpeed, byte expectedZoomByte)
    {
        var sent = CapturePacket();

        await _controller.ContinuousZoomAsync(direction, zoomSpeed);

        sent.Value.ShouldNotBeNull();
        AssertPacketBytes(sent.Value!.Value, [
            ViscaProtocol.CameraAddress, ViscaProtocol.CommandPrefix, ViscaProtocol.CategoryCamera,
            ViscaProtocol.CmdZoomVariable, expectedZoomByte, ViscaProtocol.Terminator]);
    }

    [Fact]
    public void MaxFocusSpeed_Success()
    {
        ((IViscaController)_controller).MaxFocusSpeed.ShouldBe(ViscaProtocol.MaxFocusSpeed);
    }

    [Theory]
    [InlineData(FocusDirection.Far, 0x05, 0x25)]
    [InlineData(FocusDirection.Near, 0x03, 0x33)]
    [InlineData(FocusDirection.Far, 0x00, 0x20)]
    [InlineData(FocusDirection.None, 0x04, 0x00)]
    public async Task ContinuousFocusAsync_Success(FocusDirection direction, byte focusSpeed, byte expectedFocusByte)
    {
        var sent = CapturePacket();

        await _controller.ContinuousFocusAsync(direction, focusSpeed);

        sent.Value.ShouldNotBeNull();
        AssertPacketBytes(sent.Value!.Value, [0x81, 0x01, 0x04, 0x08, expectedFocusByte, 0xff]);
    }

    [Theory]
    [InlineData(FocusDirection.Far, 0x27)]
    [InlineData(FocusDirection.Near, 0x37)]
    public async Task ContinuousFocusAsync_WhenSpeedExceedsMaximum_ClampsToMaxFocusSpeed(FocusDirection direction, byte expectedFocusByte)
    {
        var sent = CapturePacket();

        await _controller.ContinuousFocusAsync(direction, 0xff);

        sent.Value.ShouldNotBeNull();
        AssertPacketBytes(sent.Value!.Value, [0x81, 0x01, 0x04, 0x08, expectedFocusByte, 0xff]);
    }

    [Fact]
    public async Task ContinuousFocusAsync_WhenDirectionIsUndefined_ThrowsArgumentOutOfRangeException()
    {
        Task act() => _controller.ContinuousFocusAsync((FocusDirection)99, 1);

        await Should.ThrowAsync<ArgumentOutOfRangeException>(act);
        _viscaClient.Verify(c => c.SendAsync(It.IsAny<ViscaPacket>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(FocusMode.Auto, 0x02)]
    [InlineData(FocusMode.Manual, 0x03)]
    [InlineData(FocusMode.Toggle, 0x10)]
    public async Task SetFocusModeAsync_Success(FocusMode focusMode, byte expectedModeByte)
    {
        var sent = CapturePacket();

        await _controller.SetFocusModeAsync(focusMode);

        sent.Value.ShouldNotBeNull();
        AssertPacketBytes(sent.Value!.Value, [0x81, 0x01, 0x04, 0x38, expectedModeByte, 0xff]);
    }

    [Fact]
    public async Task SetFocusModeAsync_WhenModeIsUndefined_ThrowsArgumentOutOfRangeException()
    {
        Task act() => _controller.SetFocusModeAsync((FocusMode)99);

        await Should.ThrowAsync<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public async Task TriggerOnePushFocusAsync_Success()
    {
        var sent = CapturePacket();

        await _controller.TriggerOnePushFocusAsync();

        sent.Value.ShouldNotBeNull();
        AssertPacketBytes(sent.Value!.Value, [0x81, 0x01, 0x04, 0x18, 0x01, 0xff]);
    }

    [Theory]
    [InlineData(0x0000, 0x00, 0x00, 0x00, 0x00)]
    [InlineData(0x1234, 0x01, 0x02, 0x03, 0x04)]
    [InlineData(0xffff, 0x0f, 0x0f, 0x0f, 0x0f)]
    public async Task SetFocusPositionAsync_Success(int focusPosition, byte p, byte q, byte r, byte s)
    {
        var sent = CapturePacket();

        await _controller.SetFocusPositionAsync((ushort)focusPosition);

        sent.Value.ShouldNotBeNull();
        AssertPacketBytes(sent.Value!.Value, [0x81, 0x01, 0x04, 0x48, p, q, r, s, 0xff]);
    }

    [Theory]
    [InlineData(IrisDirection.Reset, 0x00)]
    [InlineData(IrisDirection.Up, 0x02)]
    [InlineData(IrisDirection.Down, 0x03)]
    public async Task AdjustIrisAsync_Success(IrisDirection irisDirection, byte expectedIrisByte)
    {
        var sent = CapturePacket();

        await _controller.AdjustIrisAsync(irisDirection);

        sent.Value.ShouldNotBeNull();
        AssertPacketBytes(sent.Value!.Value, [0x81, 0x01, 0x04, 0x0b, expectedIrisByte, 0xff]);
    }

    [Fact]
    public async Task AdjustIrisAsync_WhenDirectionIsUndefined_ThrowsArgumentOutOfRangeException()
    {
        Task act() => _controller.AdjustIrisAsync((IrisDirection)99);

        await Should.ThrowAsync<ArgumentOutOfRangeException>(act);
    }

    [Theory]
    [InlineData(0x00, 0x00, 0x00)]
    [InlineData(0x11, 0x01, 0x01)]
    [InlineData(0xa5, 0x0a, 0x05)]
    public async Task SetIrisPositionAsync_Success(byte irisPosition, byte p, byte q)
    {
        var sent = CapturePacket();

        await _controller.SetIrisPositionAsync(irisPosition);

        sent.Value.ShouldNotBeNull();
        AssertPacketBytes(sent.Value!.Value, [0x81, 0x01, 0x04, 0x4b, 0x00, 0x00, p, q, 0xff]);
    }

    [Theory]
    [InlineData(ExposureMode.FullAuto, 0x00)]
    [InlineData(ExposureMode.Manual, 0x03)]
    [InlineData(ExposureMode.ShutterPriority, 0x0a)]
    [InlineData(ExposureMode.IrisPriority, 0x0b)]
    [InlineData(ExposureMode.Bright, 0x0d)]
    public async Task SetExposureModeAsync_Success(ExposureMode exposureMode, byte expectedModeByte)
    {
        var sent = CapturePacket();

        await _controller.SetExposureModeAsync(exposureMode);

        sent.Value.ShouldNotBeNull();
        AssertPacketBytes(sent.Value!.Value, [0x81, 0x01, 0x04, 0x39, expectedModeByte, 0xff]);
    }

    [Fact]
    public async Task SetExposureModeAsync_WhenModeIsUndefined_ThrowsArgumentOutOfRangeException()
    {
        Task act() => _controller.SetExposureModeAsync((ExposureMode)99);

        await Should.ThrowAsync<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void Dispose_Success()
    {
        _controller.Dispose();

        _viscaClient.Verify(c => c.Dispose(), Times.Once);
    }

    private void SetupResponse(byte[] responseBytes)
    {
        _viscaClient
            .Setup(c => c.SendAsync(It.IsAny<ViscaPacket>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ViscaPacket.FromBytes(responseBytes, 0, responseBytes.Length));
    }

    private StrongBox<ViscaPacket?> CapturePacket()
    {
        var box = new StrongBox<ViscaPacket?>(null);

        _viscaClient
            .Setup(c => c.SendAsync(It.IsAny<ViscaPacket>(), It.IsAny<CancellationToken>()))
            .Callback<ViscaPacket, CancellationToken>((packet, _) => box.Value = packet)
            .ReturnsAsync(CreateAckPacket());

        return box;
    }

    private static ViscaPacket CreateAckPacket()
    {
        return ViscaPacket.FromBytes([0x90, 0x41, ViscaProtocol.Terminator], 0, 3);
    }

    private static void AssertPacketBytes(ViscaPacket packet, byte[] expected)
    {
        packet.Length.ShouldBe(expected.Length);

        for (var i = 0; i < expected.Length; i++)
        {
            packet.GetByte(i).ShouldBe(expected[i], $"byte at index {i} should be 0x{expected[i]:x2}");
        }
    }
}
