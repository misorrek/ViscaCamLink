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
