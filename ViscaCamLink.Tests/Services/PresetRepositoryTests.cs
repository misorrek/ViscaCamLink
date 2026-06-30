namespace ViscaCamLink.Tests.Services;

using System.IO;

using Shouldly;
using ViscaCamLink.Repositories;

public sealed class PresetRepositoryTests : IDisposable
{
    private readonly string _tempFile;
    private readonly PresetRepository _repository;

    public PresetRepositoryTests()
    {
        _tempFile = Path.Combine(Path.GetTempPath(), $"viscacamlink_test_{Guid.NewGuid()}.json");
        _repository = new PresetRepository(_tempFile);
    }

    public void Dispose()
    {
        if (File.Exists(_tempFile))
        {
            File.Delete(_tempFile);
        }

        if (File.Exists($"{_tempFile}.bak"))
        {
            File.Delete($"{_tempFile}.bak");
        }
    }

    [Fact]
    public void Load_WhenFileDoesNotExist_ReturnsDefault()
    {
        var data = _repository.Load();

        data.ShouldNotBeNull();
        data.Groups.Count.ShouldBe(1);
        data.Groups[0].Id.ShouldBe("default");
        data.Groups[0].Presets.Count.ShouldBe(10);
    }

    [Fact]
    public void Save_ThenLoad_RoundTrips()
    {
        var data = _repository.Load();
        data.Groups[0].Presets[0].Name = "Home";
        data.Groups[0].Presets[5].Name = "Stage Left";

        _repository.Save(data);
        var loaded = _repository.Load();

        loaded.Groups[0].Presets[0].Name.ShouldBe("Home");
        loaded.Groups[0].Presets[5].Name.ShouldBe("Stage Left");
    }

    [Fact]
    public void Save_CreatesDirectoryIfNeeded()
    {
        var nestedPath = Path.Combine(Path.GetTempPath(), $"vcl_test_{Guid.NewGuid()}", "sub", "presets.json");
        var repo = new PresetRepository(nestedPath);

        var data = repo.Load();
        repo.Save(data);

        File.Exists(nestedPath).ShouldBeTrue();

        // Cleanup
        Directory.Delete(Path.GetDirectoryName(nestedPath)!, true);
    }

    [Fact]
    public void Load_DefaultPresets_HaveCorrectSlotIndices()
    {
        var data = _repository.Load();

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
        var data = _repository.Load();
        data.Groups.Add(new PresetGroup
        {
            Id = "second",
            Name = "Second",
            Presets = Enumerable.Range(10, 10)
                .Select(i => new PresetMetadata { GroupId = "second", SlotIndex = i, Name = i.ToString() })
                .ToList(),
        });

        _repository.Save(data);
        var loaded = _repository.Load();

        loaded.Groups.Count.ShouldBe(2);
        loaded.Groups[1].Id.ShouldBe("second");
        loaded.Groups[1].Name.ShouldBe("Second");
        loaded.Groups[1].Presets.Count.ShouldBe(10);
        loaded.Groups[1].Presets[0].SlotIndex.ShouldBe(10);
    }

        [Fact]
        public void Load_WhenJsonIsMalformed_ReturnsDefaultAndBacksUpRejectedFile()
        {
                File.WriteAllText(_tempFile, "{ this is not valid json");

                var data = _repository.Load();

                data.Groups.Count.ShouldBe(1);
                data.Groups[0].Id.ShouldBe("default");
                File.Exists($"{_tempFile}.bak").ShouldBeTrue();
        }

        [Fact]
        public void Load_WhenGroupsAreEmpty_ReturnsDefaultAndBacksUpRejectedFile()
        {
                File.WriteAllText(_tempFile, "{ \"groups\": [] }");

                var data = _repository.Load();

                data.Groups.Count.ShouldBe(1);
                data.Groups[0].Id.ShouldBe("default");
                File.Exists($"{_tempFile}.bak").ShouldBeTrue();
        }

        [Fact]
        public void Load_WhenPresetSlotIsOutOfRange_ReturnsDefaultAndBacksUpRejectedFile()
        {
                File.WriteAllText(_tempFile, """
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

                var data = _repository.Load();

                data.Groups.Count.ShouldBe(1);
                data.Groups[0].Id.ShouldBe("default");
                File.Exists($"{_tempFile}.bak").ShouldBeTrue();
        }

        [Fact]
        public void Load_WhenSlotsAreDuplicatedAcrossGroups_ReturnsDefaultAndBacksUpRejectedFile()
        {
                File.WriteAllText(_tempFile, """
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

                var data = _repository.Load();

                data.Groups.Count.ShouldBe(1);
                data.Groups[0].Id.ShouldBe("default");
                File.Exists($"{_tempFile}.bak").ShouldBeTrue();
        }
}
