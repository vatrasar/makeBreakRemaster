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
}