namespace ViscaCamLink.Tests.Visca;

using System.Runtime.CompilerServices;

using Shouldly;

using Moq;
using Microsoft.Extensions.Logging.Abstractions;

using ViscaCamLink.Visca;
using ViscaCamLink.Visca.Types;

public sealed class ViscaControllerTests
{
    private readonly Mock<IViscaClient> _viscaClient = new();
    private readonly ViscaController _controller;

    public ViscaControllerTests()
    {
        _viscaClient
            .Setup(c => c.SendAsync(It.IsAny<ViscaPacket>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateAckPacket());
        _controller = new ViscaController(_viscaClient.Object, NullLogger.Instance);
    }

    // --- Connection ---

    [Fact]
    public void Connected_DelegatesToClient()
    {
        _viscaClient.Setup(c => c.IsConnected()).Returns(true);

        _controller.Connected.ShouldBe(true);
    }

    [Fact]
    public async Task Reconnect_DelegatesToClient()
    {
        _viscaClient
            .Setup(c => c.Reconnect(It.IsAny<CancellationToken>(), "10.0.0.1", 5678))
            .Returns(Task.CompletedTask)
            .Verifiable();

        await _controller.Reconnect(CancellationToken.None, "10.0.0.1", 5678);

        _viscaClient.Verify();
    }

    // --- Speed limits ---

    [Fact]
    public void MaxPanSpeed_ReturnsProtocolConstant()
    {
        ((IViscaController)_controller).MaxPanSpeed.ShouldBe(ViscaProtocol.MaxPanSpeed);
    }

    [Fact]
    public void MaxTiltSpeed_ReturnsProtocolConstant()
    {
        ((IViscaController)_controller).MaxTiltSpeed.ShouldBe(ViscaProtocol.MaxTiltSpeed);
    }

    [Fact]
    public void MaxZoomSpeed_ReturnsProtocolConstant()
    {
        ((IViscaController)_controller).MaxZoomSpeed.ShouldBe(ViscaProtocol.MaxZoomSpeed);
    }

    // --- Power ---

    [Fact]
    public async Task PowerOn_SendsCorrectPacket()
    {
        var sent = CapturePacket();

        await _controller.PowerOn();

        sent.Value.ShouldNotBeNull();
        AssertPacketBytes(sent.Value!.Value, [
            ViscaProtocol.CameraAddress, ViscaProtocol.CommandPrefix, ViscaProtocol.CategoryCamera,
            ViscaProtocol.CmdPower, ViscaProtocol.PowerOnArg, ViscaProtocol.Terminator]);
    }

    [Fact]
    public async Task PowerOff_SendsCorrectPacket()
    {
        var sent = CapturePacket();

        await _controller.PowerOff();

        sent.Value.ShouldNotBeNull();
        AssertPacketBytes(sent.Value!.Value, [
            ViscaProtocol.CameraAddress, ViscaProtocol.CommandPrefix, ViscaProtocol.CategoryCamera,
            ViscaProtocol.CmdPower, ViscaProtocol.PowerOffArg, ViscaProtocol.Terminator]);
    }

    [Fact]
    public async Task GetPowerStatus_SendsInquiryAndParsesResponse()
    {
        var response = ViscaPacket.FromBytes([0x90, 0x50, (byte)PowerStatus.On, 0xff], 0, 4);
        _viscaClient
            .Setup(c => c.SendAsync(It.IsAny<ViscaPacket>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var status = await _controller.GetPowerStatus();

        status.ShouldBe(PowerStatus.On);
    }

    [Fact]
    public async Task GetPowerStatus_StandbyResponse()
    {
        var response = ViscaPacket.FromBytes([0x90, 0x50, (byte)PowerStatus.Standby, 0xff], 0, 4);
        _viscaClient
            .Setup(c => c.SendAsync(It.IsAny<ViscaPacket>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var status = await _controller.GetPowerStatus();

        status.ShouldBe(PowerStatus.Standby);
    }

    [Fact]
    public async Task GetPowerStatus_ThrowsWhenResponseIsTooShort()
    {
        var response = ViscaPacket.FromBytes([0x90, 0x50, 0xff], 0, 3);
        _viscaClient
            .Setup(c => c.SendAsync(It.IsAny<ViscaPacket>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var act = () => _controller.GetPowerStatus();

        await Should.ThrowAsync<ViscaProtocolException>(act);
    }

    [Fact]
    public async Task GetPowerStatus_ThrowsWhenStatusValueIsUnknown()
    {
        var response = ViscaPacket.FromBytes([0x90, 0x50, 0x7f, 0xff], 0, 4);
        _viscaClient
            .Setup(c => c.SendAsync(It.IsAny<ViscaPacket>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var act = () => _controller.GetPowerStatus();

        await Should.ThrowAsync<ViscaProtocolException>(act);
    }

    // --- Memory ---

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    public async Task MemorySet_SendsCorrectPacket(byte slot)
    {
        var sent = CapturePacket();

        await _controller.MemorySet(slot);

        sent.Value.ShouldNotBeNull();
        AssertPacketBytes(sent.Value!.Value, [
            ViscaProtocol.CameraAddress, ViscaProtocol.CommandPrefix, ViscaProtocol.CategoryCamera,
            ViscaProtocol.CmdMemoryReset, ViscaProtocol.MemorySubSet, slot, ViscaProtocol.Terminator]);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    public async Task MemoryRecall_SendsCorrectPacket(byte slot)
    {
        var sent = CapturePacket();

        await _controller.MemoryRecall(slot);

        sent.Value.ShouldNotBeNull();
        AssertPacketBytes(sent.Value!.Value, [
            ViscaProtocol.CameraAddress, ViscaProtocol.CommandPrefix, ViscaProtocol.CategoryCamera,
            ViscaProtocol.CmdMemoryReset, ViscaProtocol.MemorySubRecall, slot, ViscaProtocol.Terminator]);
    }

    // --- Pan/Tilt ---

    [Fact]
    public async Task GoHome_SendsCorrectPacket()
    {
        var sent = CapturePacket();

        await _controller.GoHome();

        sent.Value.ShouldNotBeNull();
        AssertPacketBytes(sent.Value!.Value, [
            ViscaProtocol.CameraAddress, ViscaProtocol.CommandPrefix, ViscaProtocol.CategoryPanTilt,
            ViscaProtocol.CmdHome, ViscaProtocol.Terminator]);
    }

    [Fact]
    public async Task ContinuousPanTilt_PanRightTiltUp_SendsCorrectDirectionBytes()
    {
        var sent = CapturePacket();

        await _controller.ContinuousPanTilt(PanTiltDirection.PanRightTiltUp, 0x10, 0x08);

        sent.Value.ShouldNotBeNull();
        AssertPacketBytes(sent.Value!.Value, [
            ViscaProtocol.CameraAddress, ViscaProtocol.CommandPrefix, ViscaProtocol.CategoryPanTilt,
            ViscaProtocol.CmdContinuousPanTilt, 0x10, 0x08,
            ViscaProtocol.DirectionPositive, ViscaProtocol.DirectionNegative, ViscaProtocol.Terminator]);
    }

    [Fact]
    public async Task ContinuousPanTilt_PanLeftTiltDown_SendsCorrectDirectionBytes()
    {
        var sent = CapturePacket();

        await _controller.ContinuousPanTilt(PanTiltDirection.PanLeftTiltDown, 0x05, 0x03);

        sent.Value.ShouldNotBeNull();
        AssertPacketBytes(sent.Value!.Value, [
            ViscaProtocol.CameraAddress, ViscaProtocol.CommandPrefix, ViscaProtocol.CategoryPanTilt,
            ViscaProtocol.CmdContinuousPanTilt, 0x05, 0x03,
            ViscaProtocol.DirectionNegative, ViscaProtocol.DirectionPositive, ViscaProtocol.Terminator]);
    }

    [Fact]
    public async Task ContinuousPanTilt_Stop_SendsStopDirections()
    {
        var sent = CapturePacket();

        await _controller.ContinuousPanTilt(PanTiltDirection.None, 0, 0);

        sent.Value.ShouldNotBeNull();
        AssertPacketBytes(sent.Value!.Value, [
            ViscaProtocol.CameraAddress, ViscaProtocol.CommandPrefix, ViscaProtocol.CategoryPanTilt,
            ViscaProtocol.CmdContinuousPanTilt, 0x00, 0x00,
            ViscaProtocol.DirectionStop, ViscaProtocol.DirectionStop, ViscaProtocol.Terminator]);
    }

    // --- Zoom ---

    [Fact]
    public async Task ContinuousZoom_ZoomIn_SendsCorrectPacket()
    {
        var sent = CapturePacket();

        await _controller.ContinuousZoom(ZoomDirection.In, 0x05);

        sent.Value.ShouldNotBeNull();
        AssertPacketBytes(sent.Value!.Value, [
            ViscaProtocol.CameraAddress, ViscaProtocol.CommandPrefix, ViscaProtocol.CategoryCamera,
            ViscaProtocol.CmdZoomVariable, ViscaProtocol.ZoomInMask | 0x05, ViscaProtocol.Terminator]);
    }

    [Fact]
    public async Task ContinuousZoom_ZoomOut_SendsCorrectPacket()
    {
        var sent = CapturePacket();

        await _controller.ContinuousZoom(ZoomDirection.Out, 0x03);

        sent.Value.ShouldNotBeNull();
        AssertPacketBytes(sent.Value!.Value, [
            ViscaProtocol.CameraAddress, ViscaProtocol.CommandPrefix, ViscaProtocol.CategoryCamera,
            ViscaProtocol.CmdZoomVariable, ViscaProtocol.ZoomOutMask | 0x03, ViscaProtocol.Terminator]);
    }

    [Fact]
    public async Task ContinuousZoom_Stop_SendsZeroParameter()
    {
        var sent = CapturePacket();

        await _controller.ContinuousZoom(ZoomDirection.None, 0x00);

        sent.Value.ShouldNotBeNull();
        AssertPacketBytes(sent.Value!.Value, [
            ViscaProtocol.CameraAddress, ViscaProtocol.CommandPrefix, ViscaProtocol.CategoryCamera,
            ViscaProtocol.CmdZoomVariable, ViscaProtocol.ZoomStop, ViscaProtocol.Terminator]);
    }

    // --- Dispose ---

    [Fact]
    public void Dispose_DisposesClient()
    {
        _controller.Dispose();

        _viscaClient.Verify(c => c.Dispose(), Times.Once);
    }

    // --- Helpers ---

    private StrongBox<ViscaPacket?> CapturePacket()
    {
        var box = new StrongBox<ViscaPacket?>(null);
        _viscaClient
            .Setup(c => c.SendAsync(It.IsAny<ViscaPacket>(), It.IsAny<CancellationToken>()))
            .Callback<ViscaPacket, CancellationToken>((p, _) => box.Value = p)
            .ReturnsAsync(CreateAckPacket());
        return box;
    }

    private static ViscaPacket CreateAckPacket() =>
        ViscaPacket.FromBytes([0x90, 0x41, ViscaProtocol.Terminator], 0, 3);

    private static void AssertPacketBytes(ViscaPacket packet, byte[] expected)
    {
        packet.Length.ShouldBe(expected.Length);
        for (var i = 0; i < expected.Length; i++)
        {
            packet.GetByte(i).ShouldBe(expected[i], $"byte at index {i} should be 0x{expected[i]:x2}");
        }
    }
}
