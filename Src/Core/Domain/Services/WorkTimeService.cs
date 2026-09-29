using makeBreak.Src.Core.Domain.Enums;
using makeBreak.Src.Core.Domain.Interfaces;
using makeBreak.Src.Core.Domain.Models;
using makeBreak.Src.Core.Domain.RepositoryContracts;

namespace makeBreak.Src.Core.Domain.Services;

/// <summary>
/// Tracks accumulated work time (seconds spent working) and wasted time (overtime seconds
/// during breaks), providing zero-filled per-day records for any inclusive date range,
/// persisted in SQLite.
/// </summary>
public sealed class WorkTimeService
{
    private const int DataRetentionDays = 366;
    private const int FlushCadenceSeconds = 30;

    private readonly IBreakScheduler _scheduler;
    private readonly IWorkTimeRepository _repository;

    private int _pendingWorkSeconds;
    private int _pendingWastedSeconds;
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
    /// Counts one second of wasted time when a break has exceeded its scheduled duration.
    /// Invoked once per second by the main ticker.
    /// </summary>
    public void RecordWastedSecond()
    {
        if (_scheduler.State is not (SessionState.OnShortBreak or SessionState.OnLongBreak))
        {
            return;
        }

        if (_scheduler.OvertimeBreakSeconds <= 0)
        {
            return;
        }

        HandleDayRollover();
        _pendingWastedSeconds++;
        _secondsSinceFlush++;

        if (_secondsSinceFlush >= FlushCadenceSeconds)
        {
            FlushPendingWork();
        }
    }

    /// <summary>
    /// Returns zero-filled work and wasted time records for every day in the given inclusive range.
    /// Invoked by the statistics window.
    /// </summary>
    public IReadOnlyList<WorkDay> GetWorkDaysInRange(DateOnly fromDate, DateOnly toDate)
    {
        FlushPendingWork();

        Dictionary<DateOnly, WorkDay> daysByDate = _repository
            .GetWorkDaysInRange(fromDate, toDate)
            .ToDictionary(day => day.Date);

        var result = new List<WorkDay>();

        for (DateOnly day = fromDate; day <= toDate; day = day.AddDays(1))
        {
            if (daysByDate.TryGetValue(day, out WorkDay? existingDay))
            {
                result.Add(existingDay);
            }
            else
            {
                result.Add(new WorkDay(day, 0, 0));
            }
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
        FlushPendingWork();
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
        if (_pendingWorkSeconds > 0)
        {
            _repository.AddWorkSeconds(_currentDay, _pendingWorkSeconds);
            _pendingWorkSeconds = 0;
        }

        if (_pendingWastedSeconds > 0)
        {
            _repository.AddWastedSeconds(_currentDay, _pendingWastedSeconds);
            _pendingWastedSeconds = 0;
        }

        _secondsSinceFlush = 0;
    }
}