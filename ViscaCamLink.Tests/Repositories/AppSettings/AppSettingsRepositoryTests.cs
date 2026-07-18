namespace ViscaCamLink.Tests.Repositories.AppSettings;

using System;
using System.IO;

using Microsoft.Extensions.Logging;

using Shouldly;

using ViscaCamLink.Infrastructure.Localization;
using ViscaCamLink.Infrastructure.Theming;
using ViscaCamLink.Repositories.AppSettings;

using Xunit;

public sealed class AppSettingsRepositoryTests : IDisposable
{
    private readonly string _rootDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    private readonly string _legacyRoot;
    private readonly string _settingsFilePath;
    private readonly AppSettingsRepository _repository;

    public AppSettingsRepositoryTests()
    {
        _legacyRoot = Path.Combine(_rootDirectory, "legacy");
        _settingsFilePath = Path.Combine(_rootDirectory, "settings.json");

        Directory.CreateDirectory(_legacyRoot);

        _repository = new AppSettingsRepository(_settingsFilePath, _legacyRoot);
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootDirectory))
        {
            Directory.Delete(_rootDirectory, recursive: true);
        }
    }

    [Fact]
    public void Load_WhenSettingsFileDoesNotExist_ReturnsDefaultsAndCreatesFile()
    {
        var settings = _repository.Load();

        settings.Language.ShouldBe(Language.System);
        settings.CameraProfiles.ShouldBeEmpty();
        settings.ActiveCameraProfileId.ShouldBe(Guid.Empty);
        File.Exists(_settingsFilePath).ShouldBeTrue();
    }

    [Fact]
    public void Load_WhenLegacyUserConfigExists_MigratesValues()
    {
        var legacyConfigDirectory = Path.Combine(_legacyRoot, "ViscaCamLink_Url_test", "1.0.0.0");

        Directory.CreateDirectory(legacyConfigDirectory);
        File.WriteAllText(Path.Combine(legacyConfigDirectory, "user.config"), """
            <?xml version="1.0" encoding="utf-8"?>
            <configuration>
              <userSettings>
                <ViscaCamLink.Properties.Settings>
                  <setting name="Ip" serializeAs="String"><value>172.16.0.10</value></setting>
                  <setting name="Port" serializeAs="String"><value>4321</value></setting>
                  <setting name="MemoryContainerVisible" serializeAs="String"><value>False</value></setting>
                  <setting name="MoveContainerVisible" serializeAs="String"><value>True</value></setting>
                  <setting name="ZoomContainerVisible" serializeAs="String"><value>False</value></setting>
                  <setting name="PanTiltSpeed" serializeAs="String"><value>7</value></setting>
                  <setting name="ZoomSpeed" serializeAs="String"><value>8</value></setting>
                  <setting name="Language" serializeAs="String"><value>2</value></setting>
                </ViscaCamLink.Properties.Settings>
              </userSettings>
            </configuration>
            """);

        var settings = _repository.Load();

        settings.CameraProfiles.Count.ShouldBe(1);
        settings.CameraProfiles[0].Ip.ShouldBe("172.16.0.10");
        settings.CameraProfiles[0].Port.ShouldBe(4321);
        settings.ActiveCameraProfileId.ShouldBe(settings.CameraProfiles[0].Id);
        settings.MemoryContainerVisible.ShouldBeFalse();
        settings.MoveContainerVisible.ShouldBeTrue();
        settings.ZoomContainerVisible.ShouldBeFalse();
        settings.PanTiltSpeed.ShouldBe(7);
        settings.ZoomSpeed.ShouldBe(8);
        settings.Language.ShouldBe(Language.German);
        File.Exists(_settingsFilePath).ShouldBeTrue();
        Directory.Exists(_legacyRoot).ShouldBeFalse();
    }

    [Fact]
    public void Save_Success()
    {
        var windowPlacement = new WindowPlacementData
        {
            ShowCmd = 2,
            NormalLeft = 100,
            NormalTop = 200,
            NormalRight = 300,
            NormalBottom = 600,
        };
        var camera = new CameraProfile
        {
            Name = "Test",
            Ip = "10.1.2.3",
            Port = 1234
        };
        var settings = new AppSettings
        {
            WindowPlacement = windowPlacement,
            LogLevel = LogLevel.Debug,
            Language = Language.German,
            Theme = Theme.Dark,
            CameraProfiles = [camera],
            ActiveCameraProfileId = camera.Id,
            ConnectionContainerVisible = false,
            MemoryContainerVisible = false,
            MoveContainerVisible = false,
            ZoomContainerVisible = false,
            PanTiltSpeed = 4,
            ZoomSpeed = 5,
            UseCompactView = false,
            UsePresetGroups = false,
            UseNumpadLayout = false,
            UseGlobalHotKeys = false
        };

        _repository.Save(settings);

        var loadedSettings = _repository.Load();

        loadedSettings.WindowPlacement.ShouldNotBeNull();
        loadedSettings.WindowPlacement.NormalLeft.ShouldBe(100);
        loadedSettings.WindowPlacement.NormalTop.ShouldBe(200);
        loadedSettings.WindowPlacement.NormalRight.ShouldBe(300);
        loadedSettings.LogLevel.ShouldBe(LogLevel.Debug);
        loadedSettings.Language.ShouldBe(Language.German);
        loadedSettings.Theme.ShouldBe(Theme.Dark);
        loadedSettings.CameraProfiles.Count.ShouldBe(1);
        loadedSettings.CameraProfiles[0].Name.ShouldBe("Test");
        loadedSettings.CameraProfiles[0].Ip.ShouldBe("10.1.2.3");
        loadedSettings.CameraProfiles[0].Port.ShouldBe(1234);
        loadedSettings.ConnectionContainerVisible.ShouldBeFalse();
        loadedSettings.MemoryContainerVisible.ShouldBeFalse();
        loadedSettings.MoveContainerVisible.ShouldBeFalse();
        loadedSettings.ZoomContainerVisible.ShouldBeFalse();
        loadedSettings.PanTiltSpeed.ShouldBe(4);
        loadedSettings.ZoomSpeed.ShouldBe(5);
        loadedSettings.UseCompactView.ShouldBeFalse();
        loadedSettings.UsePresetGroups.ShouldBeFalse();
        loadedSettings.UseNumpadLayout.ShouldBeFalse();
        loadedSettings.UseGlobalHotKeys.ShouldBeFalse();
    }
}
