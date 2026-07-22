namespace ViscaCamLink.Tests.ViewModels;

using Moq;

using Shouldly;

using ViscaCamLink.Repositories.AppSettings;
using ViscaCamLink.Services;
using ViscaCamLink.ViewModels;

using Xunit;

public sealed class CameraProfilesViewModelTests
{
    private readonly Mock<ISettingsService> _settings = new();
    private readonly Mock<ICameraConnectionService> _connectionService = new();
    private readonly CameraProfile _firstProfile = new() { Name = "Cam A", Ip = "10.0.0.1", Port = 1 };
    private readonly CameraProfile _secondProfile = new() { Name = "Cam B", Ip = "10.0.0.2", Port = 2 };
    private readonly CameraProfilesViewModel _viewModel;

    private bool _closed;

    public CameraProfilesViewModelTests()
    {
        _settings.Setup(s => s.CameraProfiles).Returns([_firstProfile, _secondProfile]);
        _settings.Setup(s => s.ActiveCameraProfileId).Returns(_secondProfile.Id);
        _connectionService.Setup(c => c.IsSwitchingCameraProfile).Returns(false);

        _viewModel = CreateSut();
    }

    [Fact]
    public void Constructor_Success()
    {
        _viewModel.Cameras.Count.ShouldBe(2);
        _viewModel.Cameras[0].Name.ShouldBe("Cam A");
        _viewModel.Cameras[1].Name.ShouldBe("Cam B");
        _viewModel.SelectedCamera.ShouldNotBeNull();
        _viewModel.SelectedCamera!.Id.ShouldBe(_secondProfile.Id);
        _viewModel.IsEditing.ShouldBeFalse();
    }

    [Theory]
    [InlineData("192.168.0.1", true)]
    [InlineData("1.2.3.4", true)]
    [InlineData("", false)]
    [InlineData("not an ip", false)]
    [InlineData("1.2.3", false)]
    [InlineData("256.1.1.1", false)]
    [InlineData("999.999.999.999", false)]
    public void IsEditIpValid_Success(string ip, bool expected)
    {
        _viewModel.EditIp = ip;

        _viewModel.IsEditIpValid.ShouldBe(expected);
    }

    [Theory]
    [InlineData("1", true)]
    [InlineData("65535", true)]
    [InlineData("0", false)]
    [InlineData("65536", false)]
    [InlineData("abc", false)]
    [InlineData("", false)]
    public void IsEditPortValid_Success(string port, bool expected)
    {
        _viewModel.EditPort = port;

        _viewModel.IsEditPortValid.ShouldBe(expected);
    }

    [Theory]
    [InlineData("Cam", true)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    public void IsEditNameValid_Success(string name, bool expected)
    {
        _viewModel.EditName = name;

        _viewModel.IsEditNameValid.ShouldBe(expected);
    }

    [Fact]
    public void AddCommand_Success()
    {
        _viewModel.AddCommand.Execute(null);

        _viewModel.IsEditing.ShouldBeTrue();
        _viewModel.EditName.ShouldBe("Camera");
        _viewModel.EditIp.ShouldBe("192.168.0.1");
        _viewModel.EditPort.ShouldBe("5678");
        _viewModel.AddCommand.CanExecute(null).ShouldBeFalse();
    }

    [Fact]
    public void EditCommand_Success()
    {
        _viewModel.EditCommand.Execute(null);

        _viewModel.IsEditing.ShouldBeTrue();
        _viewModel.EditName.ShouldBe("Cam B");
        _viewModel.EditIp.ShouldBe("10.0.0.2");
        _viewModel.EditPort.ShouldBe("2");
    }

    [Fact]
    public void SaveEditCommand_Success()
    {
        _viewModel.AddCommand.Execute(null);

        _viewModel.EditName = "New Cam";
        _viewModel.EditIp = "10.1.1.1";
        _viewModel.EditPort = "999";

        _viewModel.SaveEditCommand.Execute(null);

        _settings.Verify(s => s.AddCameraProfile(It.Is<CameraProfile>(p =>
            p.Name == "New Cam" &&
            p.Ip == "10.1.1.1" &&
            p.Port == 999)), Times.Once);
        _viewModel.Cameras.Count.ShouldBe(3);
        _viewModel.SelectedCamera.ShouldBeSameAs(_viewModel.Cameras[2]);
        _viewModel.IsEditing.ShouldBeFalse();
    }

    [Fact]
    public void SaveEditCommand_WhenEditingExistingProfile_UpdatesProfile()
    {
        _viewModel.EditCommand.Execute(null);

        _viewModel.EditName = "Renamed";

        _viewModel.SaveEditCommand.Execute(null);

        _settings.Verify(s => s.UpdateCameraProfile(It.Is<CameraProfile>(p =>
            p.Id == _secondProfile.Id &&
            p.Name == "Renamed")), Times.Once);
        _viewModel.Cameras.Count.ShouldBe(2);
        _viewModel.Cameras[1].Name.ShouldBe("Renamed");
        _viewModel.IsEditing.ShouldBeFalse();
    }

    [Fact]
    public void DeleteCommand_Success()
    {
        _viewModel.DeleteCommand.Execute(null);

        _settings.Verify(s => s.RemoveCameraProfile(_secondProfile.Id), Times.Once);
        _viewModel.Cameras.ShouldHaveSingleItem().Id.ShouldBe(_firstProfile.Id);
    }

    [Fact]
    public void DeleteCommand_WhenOnlyOneCameraExists_CannotExecute()
    {
        _settings.Setup(s => s.CameraProfiles).Returns([_firstProfile]);

        var viewModel = CreateSut();

        viewModel.DeleteCommand.CanExecute(null).ShouldBeFalse();
    }

    [Fact]
    public void ConnectCommand_Success()
    {
        _viewModel.ConnectCommand.Execute(null);

        _connectionService.Verify(c => c.SwitchCameraProfileAsync(_secondProfile), Times.Once);
        _closed.ShouldBeTrue();
    }

    private CameraProfilesViewModel CreateSut()
    {
        return new CameraProfilesViewModel(_settings.Object, _connectionService.Object, () => _closed = true);
    }
}
