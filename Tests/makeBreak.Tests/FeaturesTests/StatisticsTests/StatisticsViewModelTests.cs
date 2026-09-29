using makeBreak.Src.Core.Domain.Enums;
using makeBreak.Src.Core.Domain.Interfaces;
using makeBreak.Src.Core.Domain.Models;
using makeBreak.Src.Core.Domain.RepositoryContracts;
using makeBreak.Src.Core.Domain.Services;
using makeBreak.Src.Features.Statistics.Domain.Enums;
using makeBreak.Src.Features.Statistics.Domain.Models;
using makeBreak.Src.Features.Statistics.UI;
using Moq;
using ReactiveUI;
using System.Reactive;
using Xunit;

namespace makeBreak.Tests.FeaturesTests.StatisticsTests;

public class StatisticsViewModelTests
{
    private static readonly DateOnly FixedToday = new(2026, 3, 10);

    [Fact]
    public void Constructor_DefaultsToWeekPeriodAndWorkMode_ReturnsSevenDailyBars()
    {
        StatisticsViewModel viewModel = CreateViewModel(Array.Empty<WorkDay>(), FixedToday);

        Assert.Equal(StatisticsPeriod.Week, viewModel.State.Period);
        Assert.Equal(StatisticsMode.WorkTime, viewModel.State.Mode);
        Assert.Equal(7, viewModel.State.Bars.Count);
        Assert.Contains("Total work time:", viewModel.State.TotalTimeLabel);
    }

    [Fact]
    public void SelectWastedMode_SwitchesModeAndFormatsTotalWastedTime()
    {
        StatisticsViewModel viewModel = CreateViewModel(Array.Empty<WorkDay>(), FixedToday);

        Execute(viewModel.SelectWastedModeCommand);

        Assert.Equal(StatisticsMode.WastedTime, viewModel.State.Mode);
        Assert.Contains("Total wasted time:", viewModel.State.TotalTimeLabel);
    }

    [Fact]
    public void SelectWastedMode_Week_AggregatesWastedSecondsAndSetsWorstBar()
    {
        WorkDay[] days = [
            new(FixedToday, 3600, 1200),
            new(FixedToday.AddDays(-1), 7200, 300)
        ];
        StatisticsViewModel viewModel = CreateViewModel(days, FixedToday);

        Execute(viewModel.SelectWastedModeCommand);

        Assert.Equal(7, viewModel.State.Bars.Count);
        WorkBarEntry todayBar = viewModel.State.Bars.Last();
        Assert.Equal("0h 20min", todayBar.WorkTimeLabel);
        Assert.True(todayBar.IsWorst);
        Assert.False(todayBar.IsBest);
    }

    [Fact]
    public void SelectWastedMode_Month_AggregatesWastedSecondsPerWeek()
    {
        DateOnly monday = new(FixedToday.Year, FixedToday.Month, 2);
        WorkDay[] days = [
            new(monday, 0, 180),
            new(monday.AddDays(1), 0, 120)
        ];
        StatisticsViewModel viewModel = CreateViewModel(days, FixedToday);

        Execute(viewModel.SelectWastedModeCommand);
        Execute(viewModel.SelectMonthCommand);

        Assert.Contains(viewModel.State.Bars, bar => bar.WorkTimeLabel == "0h 5min");
    }

    [Fact]
    public void SelectWastedMode_Year_AggregatesWastedSecondsPerMonth()
    {
        DateOnly monthDay = new(FixedToday.Year, FixedToday.Month, 15);
        WorkDay[] days = [
            new(monthDay, 0, 3600),
            new(monthDay.AddDays(1), 0, 1800)
        ];
        StatisticsViewModel viewModel = CreateViewModel(days, FixedToday);

        Execute(viewModel.SelectWastedModeCommand);
        Execute(viewModel.SelectYearCommand);

        Assert.Equal(12, viewModel.State.Bars.Count);
        Assert.Contains(viewModel.State.Bars, bar => bar.WorkTimeLabel == "1h 30min");
    }

    [Fact]
    public void SelectWorkMode_SwitchesBackFromWastedMode()
    {
        WorkDay[] days = [new(FixedToday, 3600, 600)];
        StatisticsViewModel viewModel = CreateViewModel(days, FixedToday);

        Execute(viewModel.SelectWastedModeCommand);
        Assert.Equal(StatisticsMode.WastedTime, viewModel.State.Mode);

        Execute(viewModel.SelectWorkModeCommand);
        Assert.Equal(StatisticsMode.WorkTime, viewModel.State.Mode);
        Assert.Equal("1h 0min", viewModel.State.Bars.Last().WorkTimeLabel);
        Assert.Contains("Total work time:", viewModel.State.TotalTimeLabel);
    }

    [Fact]
    public void SelectWeek_ReturnsSevenDailyBars()
    {
        StatisticsViewModel viewModel = CreateViewModel(Array.Empty<WorkDay>(), FixedToday);

        Execute(viewModel.SelectWeekCommand);

        Assert.Equal(StatisticsPeriod.Week, viewModel.State.Period);
        Assert.Equal(7, viewModel.State.Bars.Count);
    }

    [Fact]
    public void SelectMonth_ReturnsOneBarPerWeekWithinCurrentMonth()
    {
        StatisticsViewModel viewModel = CreateViewModel(Array.Empty<WorkDay>(), FixedToday);

        Execute(viewModel.SelectMonthCommand);

        Assert.Equal(StatisticsPeriod.Month, viewModel.State.Period);
        Assert.True(viewModel.State.Bars.Count >= 4);
        Assert.True(viewModel.State.Bars.Count <= 6);
    }

    [Fact]
    public void SelectMonth_AggregatesSecondsPerWeekInCurrentMonth()
    {
        StatisticsViewModel viewModel = CreateViewModel(BuildMonthDays(FixedToday), FixedToday);

        Execute(viewModel.SelectMonthCommand);

        Assert.Contains(viewModel.State.Bars, bar => bar.WorkTimeLabel == "0h 1min");
    }

    [Fact]
    public void SelectMonth_ExcludesDaysOutsideCurrentMonth()
    {
        WorkDay[] sentinelDays = { new(new DateOnly(2026, 4, 15), 60) };
        StatisticsViewModel viewModel = CreateViewModel(sentinelDays, FixedToday);

        Execute(viewModel.SelectMonthCommand);

        Assert.True(viewModel.State.Bars.All(bar => bar.WorkTimeLabel == "0h 0min"));
    }

    [Fact]
    public void SelectYear_ReturnsTwelveMonthlyBars()
    {
        StatisticsViewModel viewModel = CreateViewModel(Array.Empty<WorkDay>(), FixedToday);

        Execute(viewModel.SelectYearCommand);

        Assert.Equal(StatisticsPeriod.Year, viewModel.State.Period);
        Assert.Equal(12, viewModel.State.Bars.Count);
    }

    [Fact]
    public void SelectYear_AggregatesSecondsPerMonth()
    {
        StatisticsViewModel viewModel = CreateViewModel(BuildYearDays(FixedToday), FixedToday);

        Execute(viewModel.SelectYearCommand);

        Assert.Equal(12, viewModel.State.Bars.Count);
        Assert.Contains(viewModel.State.Bars, bar => bar.WorkTimeLabel == "3h 0min");
    }

    [Fact]
    public void SelectYear_ExcludesMonthsOutsideTwelveMonthWindow()
    {
        WorkDay[] sentinelDays = [new(new DateOnly(2026, 4, 15), 7200)];
        StatisticsViewModel viewModel = CreateViewModel(sentinelDays, FixedToday);

        Execute(viewModel.SelectYearCommand);

        Assert.Equal(12, viewModel.State.Bars.Count);
        Assert.True(viewModel.State.Bars.All(bar => bar.WorkTimeLabel == "0h 0min"));
    }

    [Fact]
    public void SelectYear_WhenExecuted_ReturnsMonthlyBarsSortedFromNewestToOldest()
    {
        WorkDay[] days = [
            new(FixedToday, 3600),
            new(new DateOnly(2025, 4, 15), 1800)
        ];
        StatisticsViewModel viewModel = CreateViewModel(days, FixedToday);

        Execute(viewModel.SelectYearCommand);

        Assert.Equal(12, viewModel.State.Bars.Count);
        Assert.Equal("1h 0min", viewModel.State.Bars.First().WorkTimeLabel);
        Assert.Equal("0h 30min", viewModel.State.Bars.Last().WorkTimeLabel);
    }

    private static void Execute(ReactiveCommand<Unit, Unit> command) => command.Execute(Unit.Default).Subscribe();

    private static StatisticsViewModel CreateViewModel(IReadOnlyList<WorkDay> days, DateOnly today)
    {
        var scheduler = new Mock<IBreakScheduler>();
        scheduler.Setup(s => s.State).Returns(SessionState.Paused);

        var repository = new Mock<IWorkTimeRepository>();
        repository.Setup(r => r.GetWorkDaysInRange(It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
            .Returns<DateOnly, DateOnly>((fromDate, toDate) =>
                days.Where(day => day.Date >= fromDate && day.Date <= toDate).ToArray());

        var service = new WorkTimeService(scheduler.Object, repository.Object);
        return new StatisticsViewModel(service, () => today);
    }

    private static WorkDay[] BuildMonthDays(DateOnly today)
    {
        DateOnly firstOfMonth = new(today.Year, today.Month, 1);
        int daysInMonth = DateTime.DaysInMonth(today.Year, today.Month);

        return Enumerable.Range(0, daysInMonth)
            .Select(offset => new WorkDay(firstOfMonth.AddDays(offset), 60))
            .ToArray();
    }

    private static WorkDay[] BuildYearDays(DateOnly today)
    {
        DateOnly endMonth = new(today.Year, today.Month, 1);
        DateOnly startMonth = endMonth.AddMonths(-11);
        var days = new List<WorkDay>();

        for (DateOnly month = startMonth; month <= endMonth; month = month.AddMonths(1))
        {
            days.Add(new WorkDay(new(month.Year, month.Month, 15), 7200));
            days.Add(new WorkDay(new(month.Year, month.Month, 16), 3600));
        }

        return days.ToArray();
    }
}