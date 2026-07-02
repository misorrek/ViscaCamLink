namespace ViscaCamLink.Tests.Repositories;

using System.IO;

using Shouldly;
using ViscaCamLink.Infrastructure.Localization;
using ViscaCamLink.Repositories.AppSettings;

public sealed class AppSettingsRepositoryTests : IDisposable
{
    private readonly string _rootDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    private readonly string _legacyRoot;
    private readonly string _settingsFilePath;

    public AppSettingsRepositoryTests()
    {
        _legacyRoot = Path.Combine(_rootDirectory, "legacy");
        Directory.CreateDirectory(_legacyRoot);
        _settingsFilePath = Path.Combine(_rootDirectory, "settings.json");
    }

    [Fact]
    public void Load_WhenSettingsFileDoesNotExist_ReturnsDefaultsAndCreatesFile()
    {
        var repository = new AppSettingsRepository(_settingsFilePath, _legacyRoot);

        var settings = repository.Load();

        settings.Ip.ShouldBe("192.168.0.1");
        settings.Port.ShouldBe(5678);
        settings.Language.ShouldBe(Language.System);
        File.Exists(_settingsFilePath).ShouldBeTrue();
    }

    [Fact]
    public void SaveAndLoad_RoundTripsValues()
    {
        var repository = new AppSettingsRepository(_settingsFilePath, _legacyRoot);
        var settings = new AppSettings
        {
            LogLevel = Microsoft.Extensions.Logging.LogLevel.Debug,
            Ip = "10.1.2.3",
            Port = 1234,
            MemoryContainerVisible = false,
            MoveContainerVisible = false,
            ZoomContainerVisible = false,
            PanTiltSpeed = 4,
            ZoomSpeed = 5,
            Language = Language.German,
            NumpadLayout = false,
        };

        repository.Save(settings);
        var loadedSettings = repository.Load();

        loadedSettings.LogLevel.ShouldBe(Microsoft.Extensions.Logging.LogLevel.Debug);
        loadedSettings.Ip.ShouldBe("10.1.2.3");
        loadedSettings.Port.ShouldBe(1234);
        loadedSettings.MemoryContainerVisible.ShouldBeFalse();
        loadedSettings.MoveContainerVisible.ShouldBeFalse();
        loadedSettings.ZoomContainerVisible.ShouldBeFalse();
        loadedSettings.PanTiltSpeed.ShouldBe(4);
        loadedSettings.ZoomSpeed.ShouldBe(5);
        loadedSettings.Language.ShouldBe(Language.German);
        loadedSettings.NumpadLayout.ShouldBeFalse();
    }

    [Fact]
    public void Load_WhenLegacyUserConfigExists_MigratesValues()
    {
        var legacyRoot = Path.Combine(_legacyRoot, "ViscaCamLink_Url_test", "1.0.0.0");
        Directory.CreateDirectory(legacyRoot);
        File.WriteAllText(Path.Combine(legacyRoot, "user.config"), """
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
                  <setting name="NumpadLayout" serializeAs="String"><value>False</value></setting>
                </ViscaCamLink.Properties.Settings>
              </userSettings>
            </configuration>
            """);

        var repository = new AppSettingsRepository(_settingsFilePath, _legacyRoot);

        var settings = repository.Load();

        settings.Ip.ShouldBe("172.16.0.10");
        settings.Port.ShouldBe(4321);
        settings.MemoryContainerVisible.ShouldBeFalse();
        settings.MoveContainerVisible.ShouldBeTrue();
        settings.ZoomContainerVisible.ShouldBeFalse();
        settings.PanTiltSpeed.ShouldBe(7);
        settings.ZoomSpeed.ShouldBe(8);
        settings.Language.ShouldBe(Language.German);
        settings.NumpadLayout.ShouldBeFalse();
        File.Exists(_settingsFilePath).ShouldBeTrue();
        Directory.Exists(_legacyRoot).ShouldBeFalse();
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootDirectory))
        {
            Directory.Delete(_rootDirectory, recursive: true);
        }
    }
}