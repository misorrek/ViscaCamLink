namespace ViscaCamLink.Tests.Repositories.Presets;

using System;
using System.IO;
using System.Linq;

using Shouldly;

using ViscaCamLink.Repositories.Presets;

using Xunit;

public sealed class PresetRepositoryTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), $"viscacamlink_test_{Guid.NewGuid():N}");
    private readonly Guid _cameraId = Guid.NewGuid();
    private readonly PresetRepository _repository;

    public PresetRepositoryTests()
    {
        Directory.CreateDirectory(_tempDirectory);

        _repository = new PresetRepository(_tempDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    private string PresetsFilePath => Path.Combine(_tempDirectory, $"presets-{_cameraId:N}.json");

    [Fact]
    public void LoadForCameraProfile_WhenFileDoesNotExist_ReturnsDefaultData()
    {
        var data = _repository.LoadForCameraProfile(_cameraId);

        data.ShouldNotBeNull();
        data.Groups.Count.ShouldBe(1);
        data.Groups[0].Id.ShouldNotBe(Guid.Empty);
        data.Groups[0].Presets.Count.ShouldBe(PresetLayout.PresetsPerGroup);

        for (var i = 0; i < PresetLayout.PresetsPerGroup; i++)
        {
            data.Groups[0].Presets[i].SlotIndex.ShouldBe(i);
            data.Groups[0].Presets[i].Name.ShouldBe(i.ToString());
            data.Groups[0].Presets[i].GroupId.ShouldBe(data.Groups[0].Id);
        }
    }

    [Fact]
    public void LoadForCameraProfile_WhenJsonIsMalformed_ReturnsDefaultAndBacksUpRejectedFile()
    {
        File.WriteAllText(PresetsFilePath, "{ this is not valid json");

        var data = _repository.LoadForCameraProfile(_cameraId);

        data.Groups.Count.ShouldBe(1);
        data.Groups[0].Id.ShouldNotBe(Guid.Empty);
        File.Exists($"{PresetsFilePath}.bak").ShouldBeTrue();
    }

    [Fact]
    public void LoadForCameraProfile_WhenGroupsAreEmpty_ReturnsDefaultAndBacksUpRejectedFile()
    {
        File.WriteAllText(PresetsFilePath, "{ \"groups\": [] }");

        var data = _repository.LoadForCameraProfile(_cameraId);

        data.Groups.Count.ShouldBe(1);
        data.Groups[0].Id.ShouldNotBe(Guid.Empty);
        File.Exists($"{PresetsFilePath}.bak").ShouldBeTrue();
    }

    [Fact]
    public void LoadForCameraProfile_WhenPresetSlotIsOutOfRange_ReturnsDefaultAndBacksUpRejectedFile()
    {
        File.WriteAllText(PresetsFilePath, """
            {
                "groups": [
                    {
                        "id": "11111111-1111-1111-1111-111111111111",
                        "name": "Default",
                        "presets": [
                            { "groupId": "11111111-1111-1111-1111-111111111111", "slotIndex": 256, "name": "Bad" }
                        ]
                    }
                ]
            }
            """);

        var data = _repository.LoadForCameraProfile(_cameraId);

        data.Groups.Count.ShouldBe(1);
        data.Groups[0].Id.ShouldNotBe(Guid.Empty);
        File.Exists($"{PresetsFilePath}.bak").ShouldBeTrue();
    }

    [Fact]
    public void LoadForCameraProfile_WhenSlotsAreDuplicatedAcrossGroups_ReturnsDefaultAndBacksUpRejectedFile()
    {
        File.WriteAllText(PresetsFilePath, """
            {
                "groups": [
                    {
                        "id": "11111111-1111-1111-1111-111111111111",
                        "name": "First",
                        "presets": [
                            { "groupId": "11111111-1111-1111-1111-111111111111", "slotIndex": 0, "name": "0" }
                        ]
                    },
                    {
                        "id": "22222222-2222-2222-2222-222222222222",
                        "name": "Second",
                        "presets": [
                            { "groupId": "22222222-2222-2222-2222-222222222222", "slotIndex": 0, "name": "0" }
                        ]
                    }
                ]
            }
            """);

        var data = _repository.LoadForCameraProfile(_cameraId);

        data.Groups.Count.ShouldBe(1);
        File.Exists($"{PresetsFilePath}.bak").ShouldBeTrue();
    }

    [Fact]
    public void SaveForCameraProfile_Success()
    {
        var data = _repository.LoadForCameraProfile(_cameraId);

        data.Groups[0].Presets[0].Name = "Home";
        data.Groups[0].Presets[5].Name = "Stage Left";

        _repository.SaveForCameraProfile(_cameraId, data);

        var loaded = _repository.LoadForCameraProfile(_cameraId);

        loaded.Groups[0].Presets[0].Name.ShouldBe("Home");
        loaded.Groups[0].Presets[5].Name.ShouldBe("Stage Left");
    }

    [Fact]
    public void SaveForCameraProfile_WhenDirectoryDoesNotExist_CreatesDirectory()
    {
        var nestedDirectory = Path.Combine(_tempDirectory, "nested", "sub");
        var repository = new PresetRepository(nestedDirectory);
        var data = repository.LoadForCameraProfile(_cameraId);

        repository.SaveForCameraProfile(_cameraId, data);

        File.Exists(Path.Combine(nestedDirectory, $"presets-{_cameraId:N}.json")).ShouldBeTrue();
    }

    [Fact]
    public void SaveForCameraProfile_WhenMultipleGroupsExist_RoundTripsAllGroups()
    {
        var secondGroupId = Guid.NewGuid();
        var data = _repository.LoadForCameraProfile(_cameraId);

        data.Groups.Add(new PresetGroup
        {
            Id = secondGroupId,
            Name = "Second",
            Presets = [.. Enumerable.Range(10, 10).Select(i => new PresetMetadata { GroupId = secondGroupId, SlotIndex = i, Name = i.ToString() })],
        });

        _repository.SaveForCameraProfile(_cameraId, data);

        var loaded = _repository.LoadForCameraProfile(_cameraId);

        loaded.Groups.Count.ShouldBe(2);
        loaded.Groups[1].Id.ShouldBe(secondGroupId);
        loaded.Groups[1].Name.ShouldBe("Second");
        loaded.Groups[1].Presets.Count.ShouldBe(10);
        loaded.Groups[1].Presets[0].SlotIndex.ShouldBe(10);
    }

    [Fact]
    public void SaveForCameraProfile_WhenActiveGroupIdIsSet_RoundTripsActiveGroupId()
    {
        var secondGroupId = Guid.NewGuid();
        var data = _repository.LoadForCameraProfile(_cameraId);

        data.Groups.Add(new PresetGroup
        {
            Id = secondGroupId,
            Name = "Second",
            Presets = [.. Enumerable.Range(10, 10).Select(i => new PresetMetadata { GroupId = secondGroupId, SlotIndex = i, Name = i.ToString() })],
        });
        data.ActiveGroupId = secondGroupId;

        _repository.SaveForCameraProfile(_cameraId, data);

        var loaded = _repository.LoadForCameraProfile(_cameraId);

        loaded.ActiveGroupId.ShouldBe(secondGroupId);
    }
}
