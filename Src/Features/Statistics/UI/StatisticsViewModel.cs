using System.Collections.Immutable;
using System.Globalization;
using makeBreak.Src.Core.Domain.Models;
using makeBreak.Src.Core.Domain.Services;
using makeBreak.Src.Core.Mvvm;
using makeBreak.Src.Features.Statistics.Domain.Enums;
using makeBreak.Src.Features.Statistics.Domain.Models;
using makeBreak.Src.Features.Statistics.Resources;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace makeBreak.Src.Features.Statistics.UI;

public sealed record StatisticsState
{
    public StatisticsPeriod Period { get; init; } = StatisticsPeriod.Week;

    public string SectionTitle { get; init; } = string.Empty;

    public ImmutableList<WorkBarEntry> Bars { get; init; } = ImmutableList<WorkBarEntry>.Empty;

    public string TotalWorkTimeLabel { get; init; } = string.Empty;
}

/// <summary>
/// View model for the statistics window. Builds the work time bars for the selected
/// period (week, current month or last twelve months) from the SQLite-backed work
/// time service, zero-filling periods without work.
/// </summary>
public sealed partial class StatisticsViewModel : ViewModelBase<StatisticsState>, IActivatableViewModel
{
    private const int MaxBarPercent = 100;
    private const int WeekDays = 7;
    private const int YearMonths = 12;

    private static readonly CultureInfo PolishCulture = CultureInfo.GetCultureInfo("pl-PL");

    private readonly WorkTimeService _workTimeService;
    private readonly Func<DateOnly> _todayProvider;

    public StatisticsViewModel(WorkTimeService workTimeService) : this(workTimeService, () => DateOnly.FromDateTime(DateTime.Now))
    {
    }

    public StatisticsViewModel(WorkTimeService workTimeService, Func<DateOnly> todayProvider) : base(new StatisticsState())
    {
        _workTimeService = workTimeService;
        _todayProvider = todayProvider;
        Refresh();
    }

    public ViewModelActivator Activator { get; } = new();

    [ReactiveCommand]
    private void SelectWeek() => SelectPeriod(StatisticsPeriod.Week);

    [ReactiveCommand]
    private void SelectMonth() => SelectPeriod(StatisticsPeriod.Month);

    [ReactiveCommand]
    private void SelectYear() => SelectPeriod(StatisticsPeriod.Year);

    private void SelectPeriod(StatisticsPeriod period)
    {
        UpdateState(s => s with { Period = period });
        Refresh();
    }

    private void Refresh()
    {
        DateOnly today = _todayProvider();

        IReadOnlyList<WorkBucket> buckets = State.Period switch
        {
            StatisticsPeriod.Week => BuildWeekBuckets(today),
            StatisticsPeriod.Month => BuildMonthBuckets(today),
            StatisticsPeriod.Year => BuildYearBuckets(today),
            _ => throw new ArgumentOutOfRangeException(),
        };

        int maxSeconds = buckets.Count == 0 ? 0 : buckets.Max(bucket => bucket.Seconds);
        ImmutableList<WorkBarEntry> entries = buckets
            .Select(bucket => BuildEntry(bucket, maxSeconds))
            .ToImmutableList();
        int totalSeconds = buckets.Sum(bucket => bucket.Seconds);

        UpdateState(s => s with
        {
            Period = State.Period,
            SectionTitle = BuildSectionTitle(today),
            Bars = entries,
            TotalWorkTimeLabel = string.Format(StatisticsStrings.TotalTimeFormat, FormatDuration(totalSeconds)),
        });
    }

    private IReadOnlyList<WorkBucket> BuildWeekBuckets(DateOnly today)
    {
        DateOnly fromDate = today.AddDays(-(WeekDays - 1));

        return _workTimeService
            .GetWorkDaysInRange(fromDate, today)
            .Select(day => new WorkBucket(BuildDayLabel(day.Date), day.WorkSeconds))
            .ToList();
    }

    private IReadOnlyList<WorkBucket> BuildMonthBuckets(DateOnly today)
    {
        DateOnly firstOfMonth = new(today.Year, today.Month, 1);
        DateOnly lastOfMonth = firstOfMonth.AddMonths(1).AddDays(-1);
        IReadOnlyList<WorkDay> days = _workTimeService.GetWorkDaysInRange(firstOfMonth, lastOfMonth);

        return GroupByWeek(days);
    }

    private IReadOnlyList<WorkBucket> BuildYearBuckets(DateOnly today)
    {
        DateOnly endMonth = new(today.Year, today.Month, 1);
        DateOnly startMonth = endMonth.AddMonths(-(YearMonths - 1));
        DateOnly firstDay = startMonth;
        DateOnly lastDay = endMonth.AddMonths(1).AddDays(-1);
        IReadOnlyList<WorkDay> days = _workTimeService.GetWorkDaysInRange(firstDay, lastDay);

        var buckets = new List<WorkBucket>(YearMonths);

        for (DateOnly month = startMonth; month <= endMonth; month = month.AddMonths(1))
        {
            int seconds = days
                .Where(day => day.Date.Year == month.Year && day.Date.Month == month.Month)
                .Sum(day => day.WorkSeconds);
            buckets.Add(new WorkBucket(BuildMonthLabel(month), seconds));
        }

        return buckets;
    }

    private IReadOnlyList<WorkBucket> GroupByWeek(IReadOnlyList<WorkDay> days)
    {
        return days
            .GroupBy(day => StartOfWeek(day.Date))
            .Select(week => new WorkBucket(
                BuildWeekLabel(week.Min(day => day.Date), week.Max(day => day.Date)),
                week.Sum(day => day.WorkSeconds)))
            .ToList();
    }

    private static string BuildDayLabel(DateOnly date)
    {
        string dayName = PolishCulture.DateTimeFormat.GetAbbreviatedDayName(date.DayOfWeek);
        return $"{dayName} {date.ToString("dd.MM", PolishCulture)}";
    }

    private static string BuildWeekLabel(DateOnly firstDay, DateOnly lastDay)
    {
        return $"{firstDay.ToString("dd.MM", PolishCulture)} - {lastDay.ToString("dd.MM", PolishCulture)}";
    }

    private static string BuildMonthLabel(DateOnly month)
    {
        return PolishCulture.DateTimeFormat.GetAbbreviatedMonthName(month.Month);
    }

    private string BuildSectionTitle(DateOnly today)
    {
        return State.Period switch
        {
            StatisticsPeriod.Week => StatisticsStrings.LastDaysLabel,
            StatisticsPeriod.Month => $"{PolishCulture.DateTimeFormat.GetMonthName(today.Month)} {today.Year}",
            StatisticsPeriod.Year => StatisticsStrings.LastMonthsLabel,
            _ => throw new ArgumentOutOfRangeException(),
        };
    }

    private static DateOnly StartOfWeek(DateOnly date)
    {
        int daysSinceMonday = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + WeekDays) % WeekDays;
        return date.AddDays(-daysSinceMonday);
    }

    private static WorkBarEntry BuildEntry(WorkBucket bucket, int maxSeconds)
    {
        double barPercent = maxSeconds == 0 ? 0 : (double)bucket.Seconds / maxSeconds * MaxBarPercent;
        bool isBest = maxSeconds > 0 && bucket.Seconds == maxSeconds;

        return new WorkBarEntry(bucket.Label, FormatDuration(bucket.Seconds), barPercent, isBest);
    }

    private static string FormatDuration(int seconds)
    {
        int hours = seconds / 3600;
        int minutes = seconds % 3600 / 60;
        return $"{hours}h {minutes}min";
    }

    private sealed record WorkBucket(string Label, int Seconds);
}
