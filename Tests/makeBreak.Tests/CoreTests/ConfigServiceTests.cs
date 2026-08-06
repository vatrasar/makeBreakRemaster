using makeBreak.Src.Core.Config;
using makeBreak.Src.Core.Domain.Models;
using makeBreak.Src.Core.Domain.RepositoryContracts;
using makeBreak.Src.Core.Domain.Services;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace makeBreak.Tests.CoreTests;

public class ConfigServiceTests
{
    private static AppConfig Defaults => new()
    {
        TimeForLongBreakSeconds = 300,
        TimeForShortBreakSeconds = 120,
        TimeToStartLongBreakSeconds = 900,
        TimeToStartShortBreakSeconds = 300,
    };

    [Fact]
    public void GetCurrentConfig_usesFileValuesWhenPresent()
    {
        BreakConfig fileConfig = new()
        {
            TimeForLongBreak = 60,
            TimeForShortBreak = 30,
            TimeToStartLongBreak = 120,
            TimeToStartShortBreak = 60,
        };
        var repository = new Mock<IConfigRepository>();
        repository.Setup(r => r.Load()).Returns(fileConfig);
        var service = new ConfigService(Options.Create(Defaults), repository.Object);

        BreakConfig result = service.GetCurrentConfig();

        Assert.Equal(60, result.TimeForLongBreak);
        Assert.Equal(30, result.TimeForShortBreak);
        Assert.Equal(120, result.TimeToStartLongBreak);
        Assert.Equal(60, result.TimeToStartShortBreak);
    }

    [Fact]
    public void GetCurrentConfig_usesAppSettingsDefaultsWhenNoFile()
    {
        var repository = new Mock<IConfigRepository>();
        repository.Setup(r => r.Load()).Returns((BreakConfig?)null);
        var service = new ConfigService(Options.Create(Defaults), repository.Object);

        BreakConfig result = service.GetCurrentConfig();

        Assert.Equal(300, result.TimeForLongBreak);
        Assert.Equal(120, result.TimeForShortBreak);
        Assert.Equal(900, result.TimeToStartLongBreak);
        Assert.Equal(300, result.TimeToStartShortBreak);
    }

    [Fact]
    public void Save_delegatesToRepository()
    {
        BreakConfig config = new()
        {
            TimeForLongBreak = 300,
            TimeForShortBreak = 120,
            TimeToStartLongBreak = 900,
            TimeToStartShortBreak = 300,
        };
        var repository = new Mock<IConfigRepository>();
        var service = new ConfigService(Options.Create(Defaults), repository.Object);

        service.Save(config);

        repository.Verify(r => r.Save(config), Times.Once);
    }
}