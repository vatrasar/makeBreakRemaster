using makeBreak.Src.Core.Domain.Enums;
using makeBreak.Src.Core.Domain.Interfaces;
using makeBreak.Src.Core.Domain.Models;
using makeBreak.Src.Core.Domain.RepositoryContracts;

namespace makeBreak.Src.Core.Domain.Services;

/// <summary>
/// Tracks accumulated work time (only seconds spent in the <see cref="SessionState.Working"/>
/// state, so breaks and pauses never count) and provides zero-filled per-day work time records
/// for any inclusive date range, persisted in SQLite.
/// </summary>
public sealed class WorkTimeService
{
    private const int DataRetentionDays = 366;
    private const int FlushCadenceSeconds = 30;

    private readonly IBreakScheduler _scheduler;
    private readonly IWorkTimeRepository _repository;

    private int _pendingWorkSeconds;
    private int _secondsSinceFlush;
    private DateOnly _currentDay;

    public WorkTimeService(IBreakScheduler scheduler, IWorkTimeRepository repository)
    {
        _scheduler = scheduler;
        _repository = repository;
        _currentDay = DateOnly.FromDateTime(DateTime.Now);

        _scheduler.StateChanged += (_, _) => OnStateChanged();
    }

    /// <summary>
    /// Counts one second of work when the schedule is currently in the working state.
    /// Invoked once per second by the main ticker.
    /// </summary>
    public void RecordWorkSecond()
    {
        if (_scheduler.State != SessionState.Working)
        {
            return;
        }

        HandleDayRollover();
        _pendingWorkSeconds++;
        _secondsSinceFlush++;

        if (_secondsSinceFlush >= FlushCadenceSeconds)
        {
            FlushPendingWork();
        }
    }

    /// <summary>
    /// Returns zero-filled work time records for every day in the given inclusive range.
    /// Invoked by the statistics window.
    /// </summary>
    public IReadOnlyList<WorkDay> GetWorkDaysInRange(DateOnly fromDate, DateOnly toDate)
    {
        FlushPendingWork();

        Dictionary<DateOnly, int> secondsByDay = _repository
            .GetWorkDaysInRange(fromDate, toDate)
            .ToDictionary(day => day.Date, day => day.WorkSeconds);

        var result = new List<WorkDay>();

        for (DateOnly day = fromDate; day <= toDate; day = day.AddDays(1))
        {
            secondsByDay.TryGetValue(day, out int weekdaySeconds);
            result.Add(new WorkDay(day, weekdaySeconds));
        }

        return result;
    }

    /// <summary>
    /// Deletes work time records older than the retained window and persists any pending work.
    /// Invoked at application startup.
    /// </summary>
    public void Cleanup()
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.Now);
        _repository.DeleteOlderThan(today.AddDays(-(DataRetentionDays - 1)));
        FlushPendingWork();
    }

    private void OnStateChanged()
    {
        if (_scheduler.State != SessionState.Working)
        {
            FlushPendingWork();
        }
    }

    private void HandleDayRollover()
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.Now);

        if (today == _currentDay)
        {
            return;
        }

        FlushPendingWork();
        _currentDay = today;
    }

    private void FlushPendingWork()
    {
        if (_pendingWorkSeconds <= 0)
        {
            return;
        }

        _repository.AddWorkSeconds(_currentDay, _pendingWorkSeconds);
        _pendingWorkSeconds = 0;
        _secondsSinceFlush = 0;
    }
}