using makeBreak.Src.Core.Config;
using makeBreak.Src.Core.Domain.Models;
using makeBreak.Src.Core.Domain.RepositoryContracts;
using makeBreak.Src.Core.Domain.Services;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace makeBreak.Tests.CoreTests;

public class BreakCoordinatorTests
{
    private static AppConfig Defaults => new()
    {
        TimeForLongBreakSeconds = 300,
        TimeForShortBreakSeconds = 120,
        TimeToStartLongBreakSeconds = 900,
        TimeToStartShortBreakSeconds = 300,
    };

    private static BreakCoordinator CreateCoordinator(out BreakScheduler scheduler)
    {
        var repository = new Mock<IConfigRepository>();
        repository.Setup(r => r.Load()).Returns((BreakConfig?)null);
        var configService = new ConfigService(Options.Create(Defaults), repository.Object);
        scheduler = new BreakScheduler();
        return new BreakCoordinator(scheduler, configService);
    }

    [Fact]
    public void Constructor_appliesLoadedConfigToScheduler()
    {
        BreakCoordinator coordinator = CreateCoordinator(out BreakScheduler scheduler);

        Assert.Equal(300, scheduler.Config.TimeForLongBreak);
        Assert.Equal(120, scheduler.Config.TimeForShortBreak);
    }

    [Fact]
    public void SaveSettings_appliesNewConfigToScheduler()
    {
        BreakCoordinator coordinator = CreateCoordinator(out BreakScheduler scheduler);
        BreakConfig newConfig = new()
        {
            TimeForLongBreak = 60,
            TimeForShortBreak = 10,
            TimeToStartLongBreak = 120,
            TimeToStartShortBreak = 30,
        };

        coordinator.SaveSettings(newConfig);

        Assert.Same(newConfig, scheduler.Config);
        Assert.Equal(60, coordinator.CurrentConfig.TimeForLongBreak);
    }
}