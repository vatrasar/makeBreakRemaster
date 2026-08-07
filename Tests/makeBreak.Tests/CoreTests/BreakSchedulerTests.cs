using makeBreak.Src.Core.Domain.Enums;
using makeBreak.Src.Core.Domain.Models;
using makeBreak.Src.Core.Domain.Services;
using Xunit;

namespace makeBreak.Tests.CoreTests;

public class BreakSchedulerTests
{
    private static BreakConfig DefaultConfig => new()
    {
        TimeToStartLongBreak = 60,
        TimeForLongBreak = 10,
        TimeToStartShortBreak = 6,
        TimeForShortBreak = 2,
    };

    [Fact]
    public void Start_setsStateToWorking()
    {
        var scheduler = new BreakScheduler();
        scheduler.ApplyConfig(DefaultConfig);

        scheduler.Start();

        Assert.Equal(SessionState.Working, scheduler.State);
    }

    [Fact]
    public void Tick_beforeShortInterval_doesNotStartBreak()
    {
        var scheduler = new BreakScheduler();
        scheduler.ApplyConfig(DefaultConfig);
        scheduler.Start();
        bool started = false;
        scheduler.BreakStarted += (_, _) => started = true;

        TickFor(scheduler, DefaultConfig.TimeToStartShortBreak - 1);

        Assert.False(started);
        Assert.Equal(SessionState.Working, scheduler.State);
    }

    [Fact]
    public void ShortBreak_startsAfterShortIntervalElapsed()
    {
        var scheduler = new BreakScheduler();
        scheduler.ApplyConfig(DefaultConfig);
        scheduler.Start();

        TickFor(scheduler, DefaultConfig.TimeToStartShortBreak);

        Assert.Equal(BreakKind.Short, scheduler.CurrentBreakKind);
        Assert.Equal(SessionState.OnShortBreak, scheduler.State);
    }

    [Fact]
    public void ShortBreak_cannotConfirmBeforeCountdownEnds()
    {
        var scheduler = new BreakScheduler();
        scheduler.ApplyConfig(DefaultConfig);
        scheduler.Start();
        TickFor(scheduler, DefaultConfig.TimeToStartShortBreak);

        Assert.False(scheduler.CanConfirmBreak);

        TickFor(scheduler, DefaultConfig.TimeForShortBreak);

        Assert.True(scheduler.CanConfirmBreak);
    }

    [Fact]
    public void Break_onlyEndsWhenConfirmed()
    {
        var scheduler = new BreakScheduler();
        scheduler.ApplyConfig(DefaultConfig);
        scheduler.Start();
        TickFor(scheduler, DefaultConfig.TimeToStartShortBreak);
        int ended = 0;
        scheduler.BreakEnded += (_, _) => ended++;

        TickFor(scheduler, DefaultConfig.TimeForShortBreak);

        Assert.Equal(0, ended);
        Assert.Equal(SessionState.OnShortBreak, scheduler.State);

        scheduler.ConfirmBreak();

        Assert.Equal(1, ended);
        Assert.Equal(SessionState.Working, scheduler.State);
    }

    [Fact]
    public void ConfirmBreak_withoutConfirmation_endsNothing()
    {
        var scheduler = new BreakScheduler();
        scheduler.ApplyConfig(DefaultConfig);
        scheduler.Start();
        TickFor(scheduler, DefaultConfig.TimeToStartShortBreak);
        int ended = 0;
        scheduler.BreakEnded += (_, _) => ended++;

        scheduler.ConfirmBreak();

        Assert.Equal(0, ended);
        Assert.Equal(SessionState.OnShortBreak, scheduler.State);
    }

    [Fact]
    public void LongBreak_triggersOncePerWorkSession()
    {
        BreakConfig config = new()
        {
            TimeToStartLongBreak = 10,
            TimeForLongBreak = 2,
            TimeToStartShortBreak = 20,
            TimeForShortBreak = 2,
        };
        var scheduler = new BreakScheduler();
        scheduler.ApplyConfig(config);
        scheduler.Start();
        int longBreaks = 0;
        scheduler.BreakStarted += (_, _) =>
        {
            if (scheduler.CurrentBreakKind == BreakKind.Long) longBreaks++;
        };

        TickFor(scheduler, config.TimeToStartLongBreak);
        scheduler.ConfirmBreak();

        Assert.Equal(SessionState.OnLongBreak, scheduler.State);

        TickFor(scheduler, config.TimeForLongBreak);
        scheduler.ConfirmBreak();

        Assert.Equal(SessionState.Working, scheduler.State);

        TickFor(scheduler, config.TimeToStartLongBreak);

        Assert.Equal(1, longBreaks);
    }

    [Fact]
    public void LongBreak_startsAfterLongIntervalElapsed()
    {
        BreakConfig config = new()
        {
            TimeToStartLongBreak = 10,
            TimeForLongBreak = 2,
            TimeToStartShortBreak = 20,
            TimeForShortBreak = 2,
        };
        var scheduler = new BreakScheduler();
        scheduler.ApplyConfig(config);
        scheduler.Start();

        TickFor(scheduler, config.TimeToStartLongBreak);

        Assert.Equal(BreakKind.Long, scheduler.CurrentBreakKind);
    }

    [Fact]
    public void Pause_stopsScheduling_untilResume()
    {
        var scheduler = new BreakScheduler();
        scheduler.ApplyConfig(DefaultConfig);
        scheduler.Start();

        scheduler.Pause();

        Assert.Equal(SessionState.Paused, scheduler.State);

        int started = 0;
        scheduler.BreakStarted += (_, _) => started++;
        TickFor(scheduler, DefaultConfig.TimeToStartShortBreak);

        Assert.Equal(0, started);

        scheduler.Resume();

        Assert.Equal(SessionState.Working, scheduler.State);
    }

    [Fact]
    public void ShortProgressPercent_startsFromZero_thenGrows()
    {
        var scheduler = new BreakScheduler();
        scheduler.ApplyConfig(DefaultConfig);
        scheduler.Start();

        Assert.Equal(0, scheduler.ShortProgressPercent);

        TickFor(scheduler, DefaultConfig.TimeToStartShortBreak / 2);

        Assert.InRange(scheduler.ShortProgressPercent, 1, 99);
    }

    [Fact]
    public void ShortBreak_skippedWhenNotEnoughTimeBeforeLong()
    {
        BreakConfig config = new()
        {
            TimeToStartLongBreak = 8,
            TimeForLongBreak = 2,
            TimeToStartShortBreak = 4,
            TimeForShortBreak = 6,
        };
        var scheduler = new BreakScheduler();
        scheduler.ApplyConfig(config);
        scheduler.Start();
        int shortBreaks = 0;
        scheduler.BreakStarted += (_, _) =>
        {
            if (scheduler.CurrentBreakKind == BreakKind.Short) shortBreaks++;
        };

        TickFor(scheduler, config.TimeToStartShortBreak);

        Assert.Equal(0, shortBreaks);
        Assert.Equal(SessionState.Working, scheduler.State);
    }

    [Fact]
    public void Stop_whileOnBreak_endsBreakAndPauses()
    {
        var scheduler = new BreakScheduler();
        scheduler.ApplyConfig(DefaultConfig);
        scheduler.Start();
        TickFor(scheduler, DefaultConfig.TimeToStartShortBreak);
        int ended = 0;
        scheduler.BreakEnded += (_, _) => ended++;

        scheduler.Stop();

        Assert.Equal(1, ended);
        Assert.Equal(SessionState.Paused, scheduler.State);
    }

    [Fact]
    public void Stop_whileWorking_pausesSchedule()
    {
        var scheduler = new BreakScheduler();
        scheduler.ApplyConfig(DefaultConfig);
        scheduler.Start();
        int ended = 0;
        scheduler.BreakEnded += (_, _) => ended++;

        scheduler.Stop();

        Assert.Equal(0, ended);
        Assert.Equal(SessionState.Paused, scheduler.State);
    }

    [Fact]
    public void ShortBreak_notSkippedWhenItStillFitsBeforeLongBreak()
    {
        BreakConfig config = new()
        {
            TimeToStartLongBreak = 14,
            TimeForLongBreak = 2,
            TimeToStartShortBreak = 4,
            TimeForShortBreak = 2,
        };
        var scheduler = new BreakScheduler();
        scheduler.ApplyConfig(config);
        scheduler.Start();
        int shortBreaks = 0;
        scheduler.BreakStarted += (_, _) =>
        {
            if (scheduler.CurrentBreakKind == BreakKind.Short) shortBreaks++;
        };

        for (int i = 0; i < config.TimeToStartLongBreak; i++)
        {
            scheduler.Tick();
            if (scheduler.State == SessionState.OnShortBreak && scheduler.CanConfirmBreak)
            {
                scheduler.ConfirmBreak();
            }
        }

        Assert.Equal(2, shortBreaks);
    }

    private static void TickFor(BreakScheduler scheduler, int seconds)
    {
        for (int i = 0; i < seconds; i++)
        {
            scheduler.Tick();
        }
    }
}