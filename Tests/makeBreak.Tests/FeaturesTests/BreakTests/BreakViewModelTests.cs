using makeBreak.Src.Core.Config;
using makeBreak.Src.Core.Domain.Models;
using makeBreak.Src.Core.Domain.RepositoryContracts;
using makeBreak.Src.Core.Domain.Services;
using makeBreak.Src.Features.Break.UI.BreakScreen;
using Microsoft.Extensions.Options;
using Moq;
using ReactiveUI;
using Xunit;

namespace makeBreak.Tests.FeaturesTests.BreakTests;

public class BreakViewModelTests
{
    private static readonly BreakConfig TestConfig = new()
    {
        TimeToStartShortBreak = 5,
        TimeForShortBreak = 3,
        TimeToStartLongBreak = 30,
        TimeForLongBreak = 10,
    };

    private static BreakCoordinator CreateCoordinator(out BreakScheduler scheduler)
    {
        var repository = new Mock<IConfigRepository>();
        repository.Setup(r => r.Load()).Returns((BreakConfig?)null);
        var configService = new ConfigService(Options.Create(new AppConfig()), repository.Object);
        scheduler = new BreakScheduler();
        var coordinator = new BreakCoordinator(scheduler, configService);
        coordinator.SaveSettings(TestConfig);
        return coordinator;
    }

    [Fact]
    public void RefreshState_DuringCountdown_SetsCountdownVisibleAndOvertimeDefault()
    {
        BreakCoordinator coordinator = CreateCoordinator(out BreakScheduler scheduler);
        var hostScreen = new Mock<IScreen>();
        var viewModel = new BreakViewModel(hostScreen.Object, coordinator);
        using var activation = viewModel.Activator.Activate();

        scheduler.Start();
        for (int i = 0; i < TestConfig.TimeToStartShortBreak; i++)
        {
            scheduler.Tick();
        }

        Assert.True(viewModel.State.IsCountdownVisible);
        Assert.False(viewModel.State.IsFinishedVisible);
        Assert.False(viewModel.State.CanConfirm);
        Assert.Equal("3", viewModel.State.CountdownNumber);
        Assert.Equal("+00:00", viewModel.State.OvertimeNumber);
    }

    [Fact]
    public void RefreshState_WhenCountdownEnds_SetsFinishedVisibleAndZeroOvertime()
    {
        BreakCoordinator coordinator = CreateCoordinator(out BreakScheduler scheduler);
        var hostScreen = new Mock<IScreen>();
        var viewModel = new BreakViewModel(hostScreen.Object, coordinator);
        using var activation = viewModel.Activator.Activate();

        scheduler.Start();
        for (int i = 0; i < TestConfig.TimeToStartShortBreak + TestConfig.TimeForShortBreak; i++)
        {
            scheduler.Tick();
        }

        Assert.False(viewModel.State.IsCountdownVisible);
        Assert.True(viewModel.State.IsFinishedVisible);
        Assert.True(viewModel.State.CanConfirm);
        Assert.Equal("+00:00", viewModel.State.OvertimeNumber);
    }

    [Fact]
    public void RefreshState_WhenInOvertime_FormatsOvertimeCorrectly()
    {
        BreakCoordinator coordinator = CreateCoordinator(out BreakScheduler scheduler);
        var hostScreen = new Mock<IScreen>();
        var viewModel = new BreakViewModel(hostScreen.Object, coordinator);
        using var activation = viewModel.Activator.Activate();

        scheduler.Start();
        int totalTicksToOvertime = TestConfig.TimeToStartShortBreak + TestConfig.TimeForShortBreak + 65;
        for (int i = 0; i < totalTicksToOvertime; i++)
        {
            scheduler.Tick();
        }

        Assert.False(viewModel.State.IsCountdownVisible);
        Assert.True(viewModel.State.IsFinishedVisible);
        Assert.True(viewModel.State.CanConfirm);
        Assert.Equal("+01:05", viewModel.State.OvertimeNumber);
    }

    [Fact]
    public void ConfirmBreak_WhenExecuted_DelegatesToCoordinator()
    {
        BreakCoordinator coordinator = CreateCoordinator(out BreakScheduler scheduler);
        var hostScreen = new Mock<IScreen>();
        var viewModel = new BreakViewModel(hostScreen.Object, coordinator);
        using var activation = viewModel.Activator.Activate();

        scheduler.Start();
        for (int i = 0; i < TestConfig.TimeToStartShortBreak + TestConfig.TimeForShortBreak; i++)
        {
            scheduler.Tick();
        }

        Assert.True(viewModel.State.CanConfirm);

        viewModel.ConfirmBreakCommand.Execute().Subscribe();

        Assert.False(scheduler.CanConfirmBreak);
        Assert.Equal(0, scheduler.OvertimeBreakSeconds);
    }
}
