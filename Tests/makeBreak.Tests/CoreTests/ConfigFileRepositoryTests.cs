using makeBreak.Src.Core.Domain.Models;
using makeBreak.Src.Infrastructure.Data.Repositories;
using Xunit;

namespace makeBreak.Tests.CoreTests;

public class ConfigFileRepositoryTests
{
    [Fact]
    public void Save_thenLoad_roundTripsValues()
    {
        string path = Path.Combine(Path.GetTempPath(), $"conf_{Guid.NewGuid():N}.txt");

        try
        {
            var repository = new ConfigFileRepository(path);
            var config = new BreakConfig
            {
                TimeForLongBreak = 300,
                TimeForShortBreak = 120,
                TimeToStartLongBreak = 900,
                TimeToStartShortBreak = 300,
            };

            repository.Save(config);

            BreakConfig? loaded = repository.Load();

            Assert.NotNull(loaded);
            Assert.Equal(300, loaded!.TimeForLongBreak);
            Assert.Equal(120, loaded.TimeForShortBreak);
            Assert.Equal(900, loaded.TimeToStartLongBreak);
            Assert.Equal(300, loaded.TimeToStartShortBreak);
            Assert.True(loaded.AreVoiceNotificationsEnabled);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public void Save_thenLoad_roundTripsAreVoiceNotificationsEnabledFalse()
    {
        string path = Path.Combine(Path.GetTempPath(), $"conf_{Guid.NewGuid():N}.txt");

        try
        {
            var repository = new ConfigFileRepository(path);
            var config = new BreakConfig
            {
                TimeForLongBreak = 300,
                TimeForShortBreak = 120,
                TimeToStartLongBreak = 900,
                TimeToStartShortBreak = 300,
                AreVoiceNotificationsEnabled = false,
            };

            repository.Save(config);

            BreakConfig? loaded = repository.Load();

            Assert.NotNull(loaded);
            Assert.False(loaded!.AreVoiceNotificationsEnabled);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public void Load_whenFourLinesPresent_defaultsAreVoiceNotificationsEnabledToTrue()
    {
        string path = Path.Combine(Path.GetTempPath(), $"conf_{Guid.NewGuid():N}.txt");

        try
        {
            File.WriteAllLines(path, new[] { "300", "120", "900", "300" });

            var repository = new ConfigFileRepository(path);
            BreakConfig? loaded = repository.Load();

            Assert.NotNull(loaded);
            Assert.True(loaded!.AreVoiceNotificationsEnabled);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public void Load_whenFileMissing_returnsNull()
    {
        string path = Path.Combine(Path.GetTempPath(), $"missing_{Guid.NewGuid():N}.txt");

        var repository = new ConfigFileRepository(path);

        Assert.Null(repository.Load());
    }

    [Fact]
    public void Load_whenValuesInvalid_returnsNull()
    {
        string path = Path.Combine(Path.GetTempPath(), $"invalid_{Guid.NewGuid():N}.txt");

        try
        {
            File.WriteAllLines(path, new[] { "300", "not-a-number", "900", "300" });

            var repository = new ConfigFileRepository(path);

            Assert.Null(repository.Load());
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public void Load_whenValuesNotPositive_returnsNull()
    {
        string path = Path.Combine(Path.GetTempPath(), $"nonpositive_{Guid.NewGuid():N}.txt");

        try
        {
            File.WriteAllLines(path, new[] { "300", "-5", "900", "300" });

            var repository = new ConfigFileRepository(path);

            Assert.Null(repository.Load());
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public void Save_thenLoad_roundTripsAudioOutputDeviceId()
    {
        string path = Path.Combine(Path.GetTempPath(), $"conf_{Guid.NewGuid():N}.txt");

        try
        {
            var repository = new ConfigFileRepository(path);
            var config = new BreakConfig
            {
                TimeForLongBreak = 300,
                TimeForShortBreak = 120,
                TimeToStartLongBreak = 900,
                TimeToStartShortBreak = 300,
                AreVoiceNotificationsEnabled = true,
                AudioOutputDeviceId = "alsa_output.pci-0000_00_1f.3.analog-stereo",
            };

            repository.Save(config);

            BreakConfig? loaded = repository.Load();

            Assert.NotNull(loaded);
            Assert.Equal("alsa_output.pci-0000_00_1f.3.analog-stereo", loaded!.AudioOutputDeviceId);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public void Load_whenFiveLinesPresent_defaultsAudioOutputDeviceIdToDefault()
    {
        string path = Path.Combine(Path.GetTempPath(), $"conf_{Guid.NewGuid():N}.txt");

        try
        {
            File.WriteAllLines(path, new[] { "300", "120", "900", "300", "True" });

            var repository = new ConfigFileRepository(path);
            BreakConfig? loaded = repository.Load();

            Assert.NotNull(loaded);
            Assert.Equal("default", loaded!.AudioOutputDeviceId);
            Assert.Equal(100, loaded.VoiceVolumePercent);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public void Save_thenLoad_roundTripsVoiceVolumePercent()
    {
        string path = Path.Combine(Path.GetTempPath(), $"conf_{Guid.NewGuid():N}.txt");

        try
        {
            var repository = new ConfigFileRepository(path);
            var config = new BreakConfig
            {
                TimeForLongBreak = 300,
                TimeForShortBreak = 120,
                TimeToStartLongBreak = 900,
                TimeToStartShortBreak = 300,
                AreVoiceNotificationsEnabled = true,
                AudioOutputDeviceId = "default",
                VoiceVolumePercent = 65,
            };

            repository.Save(config);

            BreakConfig? loaded = repository.Load();

            Assert.NotNull(loaded);
            Assert.Equal(65, loaded!.VoiceVolumePercent);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public void Load_whenVolumeOutOfRange_clampsBetweenZeroAndHundred()
    {
        string path = Path.Combine(Path.GetTempPath(), $"conf_{Guid.NewGuid():N}.txt");

        try
        {
            File.WriteAllLines(path, new[] { "300", "120", "900", "300", "True", "default", "150" });

            var repository = new ConfigFileRepository(path);
            BreakConfig? loaded = repository.Load();

            Assert.NotNull(loaded);
            Assert.Equal(100, loaded!.VoiceVolumePercent);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}