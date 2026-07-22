namespace ViscaCamLink.Tests.Services;

using System;
using System.Collections.Generic;
using System.IO;

using Shouldly;

using ViscaCamLink.Infrastructure.Localization;
using ViscaCamLink.Infrastructure.Theming;
using ViscaCamLink.Repositories.AppSettings;
using ViscaCamLink.Services;

using Xunit;

public sealed class SettingsServiceTests : IDisposable
{
    private readonly string _rootDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    private readonly string _settingsFilePath;
    private readonly AppSettings _settings = new();
    private readonly List<Language> _appliedLanguages = [];
    private readonly List<Theme> _appliedThemes = [];
    private readonly SettingsService _settingsService;

    public SettingsServiceTests()
    {
        _settingsFilePath = Path.Combine(_rootDirectory, "settings.json");

        var repository = new AppSettingsRepository(_settingsFilePath, Path.Combine(_rootDirectory, "legacy"));

        _settingsService = new SettingsService(_settings, repository, _appliedLanguages.Add, _appliedThemes.Add);
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootDirectory))
        {
            Directory.Delete(_rootDirectory, recursive: true);
        }
    }

    [Fact]
    public void AddCameraProfile_Success()
    {
        var profile = CreateProfile("Cam A");

        _settingsService.AddCameraProfile(profile);

        _settingsService.CameraProfiles.ShouldHaveSingleItem().ShouldBeSameAs(profile);
        _settingsService.ActiveCameraProfileId.ShouldBe(profile.Id);
        File.Exists(_settingsFilePath).ShouldBeTrue();
    }

    [Fact]
    public void AddCameraProfile_WhenAnotherProfileIsActive_KeepsActiveProfile()
    {
        var first = CreateProfile("Cam A");
        var second = CreateProfile("Cam B");

        _settingsService.AddCameraProfile(first);
        _settingsService.AddCameraProfile(second);

        _settingsService.ActiveCameraProfileId.ShouldBe(first.Id);
    }

    [Fact]
    public void AddCameraProfile_WhenIdAlreadyExists_ThrowsInvalidOperationException()
    {
        var profile = CreateProfile("Cam A");

        _settingsService.AddCameraProfile(profile);

        void act() => _settingsService.AddCameraProfile(profile);

        Should.Throw<InvalidOperationException>(act);
    }

    [Fact]
    public void RemoveCameraProfile_Success()
    {
        var first = CreateProfile("Cam A");
        var second = CreateProfile("Cam B");

        _settingsService.AddCameraProfile(first);
        _settingsService.AddCameraProfile(second);

        _settingsService.RemoveCameraProfile(first.Id);

        _settingsService.CameraProfiles.ShouldHaveSingleItem().ShouldBeSameAs(second);
        _settingsService.ActiveCameraProfileId.ShouldBe(second.Id);
    }

    [Fact]
    public void RemoveCameraProfile_WhenIdIsUnknown_DoesNothing()
    {
        var profile = CreateProfile("Cam A");

        _settingsService.AddCameraProfile(profile);

        _settingsService.RemoveCameraProfile(Guid.NewGuid());

        _settingsService.CameraProfiles.ShouldHaveSingleItem().ShouldBeSameAs(profile);
    }

    [Fact]
    public void RemoveCameraProfile_WhenLastProfileIsRemoved_SetsActiveToEmpty()
    {
        var profile = CreateProfile("Cam A");

        _settingsService.AddCameraProfile(profile);

        _settingsService.RemoveCameraProfile(profile.Id);

        _settingsService.CameraProfiles.ShouldBeEmpty();
        _settingsService.ActiveCameraProfileId.ShouldBe(Guid.Empty);
    }

    [Fact]
    public void UpdateCameraProfile_Success()
    {
        var profile = CreateProfile("Cam A");

        _settingsService.AddCameraProfile(profile);

        var updatedProfile = new CameraProfile { Id = profile.Id, Name = "Renamed", Ip = "10.0.0.2", Port = 2 };

        _settingsService.UpdateCameraProfile(updatedProfile);

        _settingsService.CameraProfiles.ShouldHaveSingleItem().ShouldBeSameAs(updatedProfile);
    }

    [Fact]
    public void UpdateCameraProfile_WhenIdIsUnknown_DoesNothing()
    {
        var profile = CreateProfile("Cam A");

        _settingsService.AddCameraProfile(profile);

        _settingsService.UpdateCameraProfile(CreateProfile("Unknown"));

        _settingsService.CameraProfiles.ShouldHaveSingleItem().ShouldBeSameAs(profile);
    }

    [Fact]
    public void SetActiveCameraProfile_Success()
    {
        var first = CreateProfile("Cam A");
        var second = CreateProfile("Cam B");

        _settingsService.AddCameraProfile(first);
        _settingsService.AddCameraProfile(second);

        _settingsService.SetActiveCameraProfile(second.Id);

        _settingsService.ActiveCameraProfileId.ShouldBe(second.Id);
    }

    [Fact]
    public void SetActiveCameraProfile_WhenIdIsUnknown_DoesNothing()
    {
        var profile = CreateProfile("Cam A");

        _settingsService.AddCameraProfile(profile);

        _settingsService.SetActiveCameraProfile(Guid.NewGuid());

        _settingsService.ActiveCameraProfileId.ShouldBe(profile.Id);
    }

    [Fact]
    public void ActiveCameraProfile_WhenActiveIdIsUnknown_ReturnsFirstProfile()
    {
        var first = CreateProfile("Cam A");
        var second = CreateProfile("Cam B");

        _settingsService.AddCameraProfile(first);
        _settingsService.AddCameraProfile(second);

        _settings.ActiveCameraProfileId = Guid.NewGuid();

        _settingsService.ActiveCameraProfile.ShouldBeSameAs(first);
    }

    [Fact]
    public void ApplyOptions_Success()
    {
        _settingsService.ApplyOptions(
            Language.German,
            numpadLayout: false,
            globalHotKeys: false,
            usePresetGroups: false,
            minimizeToCompactWindow: false,
            theme: Theme.Dark);

        _settingsService.Language.ShouldBe(Language.German);
        _settingsService.UseNumpadLayout.ShouldBeFalse();
        _settingsService.UseGlobalHotKeys.ShouldBeFalse();
        _settingsService.UsePresetGroups.ShouldBeFalse();
        _settingsService.UseCompactView.ShouldBeFalse();
        _settingsService.Theme.ShouldBe(Theme.Dark);
        _appliedLanguages.ShouldBe([Language.German]);
        _appliedThemes.ShouldBe([Theme.Dark]);
        File.Exists(_settingsFilePath).ShouldBeTrue();
    }

    [Fact]
    public void ApplyOptions_WhenNothingChanged_DoesNotSaveOrApplyCallbacks()
    {
        _settingsService.ApplyOptions(
            Language.System,
            numpadLayout: true,
            globalHotKeys: true,
            usePresetGroups: true,
            minimizeToCompactWindow: true,
            theme: Theme.System);

        _appliedLanguages.ShouldBeEmpty();
        _appliedThemes.ShouldBeEmpty();
        File.Exists(_settingsFilePath).ShouldBeFalse();
    }

    private static CameraProfile CreateProfile(string name)
    {
        return new CameraProfile { Name = name, Ip = "10.0.0.1", Port = 1 };
    }
}
