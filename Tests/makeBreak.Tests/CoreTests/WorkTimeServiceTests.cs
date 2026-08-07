using makeBreak.Src.Core.Domain.Enums;
using makeBreak.Src.Core.Domain.Interfaces;
using makeBreak.Src.Core.Domain.Models;
using makeBreak.Src.Core.Domain.RepositoryContracts;
using makeBreak.Src.Core.Domain.Services;
using Moq;
using Xunit;

namespace makeBreak.Tests.CoreTests;

public class WorkTimeServiceTests
{
    [Fact]
    public void RecordWorkSecond_whenNotWorking_doesNotAccumulate()
    {
        var scheduler = new Mock<IBreakScheduler>();
        scheduler.SetupGet(s => s.State).Returns(SessionState.OnShortBreak);
        var repository = new Mock<IWorkTimeRepository>();
        var service = new WorkTimeService(scheduler.Object, repository.Object);

        service.RecordWorkSecond();
        scheduler.Raise(s => s.StateChanged += null, null, EventArgs.Empty);

        repository.Verify(r => r.AddWorkSeconds(It.IsAny<DateOnly>(), It.IsAny<int>()), Times.Never());
    }

    [Fact]
    public void RecordWorkSecond_whenWorking_flushesOnLeavingWorking()
    {
        SessionState state = SessionState.Working;
        var scheduler = new Mock<IBreakScheduler>();
        scheduler.Setup(s => s.State).Returns(() => state);
        var repository = new Mock<IWorkTimeRepository>();
        var service = new WorkTimeService(scheduler.Object, repository.Object);
        DateOnly today = DateOnly.FromDateTime(DateTime.Now);

        for (int i = 0; i < 10; i++)
        {
            service.RecordWorkSecond();
        }

        state = SessionState.OnLongBreak;
        scheduler.Raise(s => s.StateChanged += null, null, EventArgs.Empty);

        repository.Verify(r => r.AddWorkSeconds(today, 10), Times.Once());
    }

    [Fact]
    public void RecordWorkSecond_whenWorking_flushesPeriodically()
    {
        var scheduler = new Mock<IBreakScheduler>();
        scheduler.Setup(s => s.State).Returns(SessionState.Working);
        var repository = new Mock<IWorkTimeRepository>();
        var service = new WorkTimeService(scheduler.Object, repository.Object);
        DateOnly today = DateOnly.FromDateTime(DateTime.Now);

        for (int i = 0; i < 30; i++)
        {
            service.RecordWorkSecond();
        }

        repository.Verify(r => r.AddWorkSeconds(today, 30), Times.Once());
    }

    [Fact]
    public void Cleanup_deletesOlderThanRetainedWindow()
    {
        var scheduler = new Mock<IBreakScheduler>();
        scheduler.Setup(s => s.State).Returns(SessionState.Paused);
        var repository = new Mock<IWorkTimeRepository>();
        var service = new WorkTimeService(scheduler.Object, repository.Object);
        DateOnly expectedCutoff = DateOnly.FromDateTime(DateTime.Now.AddDays(-365));

        service.Cleanup();

        repository.Verify(r => r.DeleteOlderThan(expectedCutoff), Times.Once());
    }

    [Fact]
    public void GetWorkDaysInRange_zeroFillsDaysWithoutRecords()
    {
        var scheduler = new Mock<IBreakScheduler>();
        scheduler.Setup(s => s.State).Returns(SessionState.Paused);
        DateOnly today = DateOnly.FromDateTime(DateTime.Now);
        DateOnly fromDate = today.AddDays(-6);
        var repository = new Mock<IWorkTimeRepository>();
        repository.Setup(r => r.GetWorkDaysInRange(It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
            .Returns(new[] { new WorkDay(today, 3600), new WorkDay(today.AddDays(-2), 1800) });
        var service = new WorkTimeService(scheduler.Object, repository.Object);

        IReadOnlyList<WorkDay> summary = service.GetWorkDaysInRange(fromDate, today);

        Assert.Equal(7, summary.Count);
        Assert.Equal(3600, summary.First(day => day.Date == today).WorkSeconds);
        Assert.Equal(1800, summary.First(day => day.Date == today.AddDays(-2)).WorkSeconds);
        Assert.Equal(0, summary.First(day => day.Date == today.AddDays(-1)).WorkSeconds);
    }
}