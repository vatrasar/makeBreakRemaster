using makeBreak.Src.Core.Domain.Entities;
using makeBreak.Src.Core.Domain.Models;
using makeBreak.Src.Core.Domain.RepositoryContracts;
using Microsoft.EntityFrameworkCore;

namespace makeBreak.Src.Infrastructure.Data.Repositories;

/// <summary>
/// Stores and reads daily work time records in the SQLite database.
/// </summary>
public sealed class WorkTimeRepository : IWorkTimeRepository
{
    private readonly MakeBreakDbContext _dbContext;

    public WorkTimeRepository(MakeBreakDbContext dbContext) => _dbContext = dbContext;

    /// <summary>
    /// Adds work seconds to the record of the given day, creating it if absent.
    /// Invoked by <c>WorkTimeService</c>.
    /// </summary>
    public void AddWorkSeconds(DateOnly date, int seconds)
    {
        WorkDayEntity? entity = _dbContext.WorkDays.FirstOrDefault(workDay => workDay.Date == date);

        if (entity is null)
        {
            _dbContext.WorkDays.Add(new WorkDayEntity { Date = date, WorkSeconds = seconds, WastedSeconds = 0 });
        }
        else
        {
            entity.WorkSeconds += seconds;
        }

        _dbContext.SaveChanges();
    }

    /// <summary>
    /// Adds wasted seconds to the record of the given day, creating it if absent.
    /// Invoked by <c>WorkTimeService</c>.
    /// </summary>
    public void AddWastedSeconds(DateOnly date, int seconds)
    {
        WorkDayEntity? entity = _dbContext.WorkDays.FirstOrDefault(workDay => workDay.Date == date);

        if (entity is null)
        {
            _dbContext.WorkDays.Add(new WorkDayEntity { Date = date, WorkSeconds = 0, WastedSeconds = seconds });
        }
        else
        {
            entity.WastedSeconds += seconds;
        }

        _dbContext.SaveChanges();
    }

    /// <summary>
    /// Returns daily records within the specified inclusive date range.
    /// Invoked by <c>WorkTimeService</c>.
    /// </summary>
    public IReadOnlyList<WorkDay> GetWorkDaysInRange(DateOnly fromDate, DateOnly toDate)
    {
        return _dbContext.WorkDays
            .Where(workDay => workDay.Date >= fromDate && workDay.Date <= toDate)
            .OrderBy(workDay => workDay.Date)
            .Select(workDay => new WorkDay(workDay.Date, workDay.WorkSeconds, workDay.WastedSeconds))
            .ToList();
    }

    /// <summary>
    /// Deletes all records older than the specified cutoff date.
    /// Invoked by <c>WorkTimeService</c>.
    /// </summary>
    public void DeleteOlderThan(DateOnly cutoffDate)
    {
        var oldDays = _dbContext.WorkDays.Where(workDay => workDay.Date < cutoffDate);

        _dbContext.WorkDays.RemoveRange(oldDays);
        _dbContext.SaveChanges();
    }
}
