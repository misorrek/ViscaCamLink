namespace ViscaCamLink.Tests.Services;

using System.IO;

using Shouldly;
using ViscaCamLink.Repositories.Presets;

public sealed class PresetRepositoryTests : IDisposable
{
    private readonly string _tempDir;
    private readonly Guid _cameraId = Guid.NewGuid();
    private readonly PresetRepository _repository;

    public PresetRepositoryTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"viscacamlink_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _repository = new PresetRepository(_tempDir);
    }

    private string TempFile => Path.Combine(_tempDir, $"presets-{_cameraId:N}.json");

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    [Fact]
    public void Load_WhenFileDoesNotExist_ReturnsDefault()
    {
        var data = _repository.LoadForCameraProfile(_cameraId);

        data.ShouldNotBeNull();
        data.Groups.Count.ShouldBe(1);
        data.Groups[0].Id.ShouldBe("default");
        data.Groups[0].Presets.Count.ShouldBe(10);
    }

    [Fact]
    public void Save_ThenLoad_RoundTrips()
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
    public void Save_CreatesDirectoryIfNeeded()
    {
        var nestedDir = Path.Combine(Path.GetTempPath(), $"vcl_test_{Guid.NewGuid():N}", "sub");
        var repo = new PresetRepository(nestedDir);
        var id = Guid.NewGuid();

        var data = repo.LoadForCameraProfile(id);
        repo.SaveForCameraProfile(id, data);

        File.Exists(Path.Combine(nestedDir, $"presets-{id:N}.json")).ShouldBeTrue();

        // Cleanup
        Directory.Delete(Path.GetDirectoryName(nestedDir)!, true);
    }

    [Fact]
    public void Load_DefaultPresets_HaveCorrectSlotIndices()
    {
        var data = _repository.LoadForCameraProfile(_cameraId);

        for (var i = 0; i < 10; i++)
        {
            data.Groups[0].Presets[i].SlotIndex.ShouldBe(i);
            data.Groups[0].Presets[i].Name.ShouldBe(i.ToString());
            data.Groups[0].Presets[i].GroupId.ShouldBe("default");
        }
    }

    [Fact]
    public void Save_ThenLoad_MultipleGroups_RoundTrips()
    {
        var data = _repository.LoadForCameraProfile(_cameraId);
        data.Groups.Add(new PresetGroup
        {
            Id = "second",
            Name = "Second",
            Presets = Enumerable.Range(10, 10)
                .Select(i => new PresetMetadata { GroupId = "second", SlotIndex = i, Name = i.ToString() })
                .ToList(),
        });

        _repository.SaveForCameraProfile(_cameraId, data);
        var loaded = _repository.LoadForCameraProfile(_cameraId);

        loaded.Groups.Count.ShouldBe(2);
        loaded.Groups[1].Id.ShouldBe("second");
        loaded.Groups[1].Name.ShouldBe("Second");
        loaded.Groups[1].Presets.Count.ShouldBe(10);
        loaded.Groups[1].Presets[0].SlotIndex.ShouldBe(10);
    }

    [Fact]
    public void Load_WhenJsonIsMalformed_ReturnsDefaultAndBacksUpRejectedFile()
    {
        File.WriteAllText(TempFile, "{ this is not valid json");
        Directory.CreateDirectory(_tempDir);

        var data = _repository.LoadForCameraProfile(_cameraId);

        data.Groups.Count.ShouldBe(1);
        data.Groups[0].Id.ShouldBe("default");
        File.Exists($"{TempFile}.bak").ShouldBeTrue();
    }

    [Fact]
    public void Load_WhenGroupsAreEmpty_ReturnsDefaultAndBacksUpRejectedFile()
    {
        File.WriteAllText(TempFile, "{ \"groups\": [] }");

        var data = _repository.LoadForCameraProfile(_cameraId);

        data.Groups.Count.ShouldBe(1);
        data.Groups[0].Id.ShouldBe("default");
        File.Exists($"{TempFile}.bak").ShouldBeTrue();
    }

    [Fact]
    public void Load_WhenPresetSlotIsOutOfRange_ReturnsDefaultAndBacksUpRejectedFile()
    {
        File.WriteAllText(TempFile, """
            {
                "groups": [
                    {
                        "id": "default",
                        "name": "Default",
                        "presets": [
                            { "groupId": "default", "slotIndex": 256, "name": "Bad" }
                        ]
                    }
                ]
            }
            """);

        var data = _repository.LoadForCameraProfile(_cameraId);

        data.Groups.Count.ShouldBe(1);
        data.Groups[0].Id.ShouldBe("default");
        File.Exists($"{TempFile}.bak").ShouldBeTrue();
    }

    [Fact]
    public void Load_WhenSlotsAreDuplicatedAcrossGroups_ReturnsDefaultAndBacksUpRejectedFile()
    {
        File.WriteAllText(TempFile, """
            {
                "groups": [
                    {
                        "id": "first",
                        "name": "First",
                        "presets": [
                            { "groupId": "first", "slotIndex": 0, "name": "0" }
                        ]
                    },
                    {
                        "id": "second",
                        "name": "Second",
                        "presets": [
                            { "groupId": "second", "slotIndex": 0, "name": "0" }
                        ]
                    }
                ]
            }
            """);

        var data = _repository.LoadForCameraProfile(_cameraId);

        data.Groups.Count.ShouldBe(1);
        data.Groups[0].Id.ShouldBe("default");
        File.Exists($"{TempFile}.bak").ShouldBeTrue();
    }
}
