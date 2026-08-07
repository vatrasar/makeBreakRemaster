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

    public void AddWorkSeconds(DateOnly date, int seconds)
    {
        WorkDayEntity? entity = _dbContext.WorkDays.FirstOrDefault(workDay => workDay.Date == date);

        if (entity is null)
        {
            _dbContext.WorkDays.Add(new WorkDayEntity { Date = date, WorkSeconds = seconds });
        }
        else
        {
            entity.WorkSeconds += seconds;
        }

        _dbContext.SaveChanges();
    }

    public IReadOnlyList<WorkDay> GetWorkDaysInRange(DateOnly fromDate, DateOnly toDate)
    {
        return _dbContext.WorkDays
            .Where(workDay => workDay.Date >= fromDate && workDay.Date <= toDate)
            .OrderBy(workDay => workDay.Date)
            .Select(workDay => new WorkDay(workDay.Date, workDay.WorkSeconds))
            .ToList();
    }

    public void DeleteOlderThan(DateOnly cutoffDate)
    {
        var oldDays = _dbContext.WorkDays.Where(workDay => workDay.Date < cutoffDate);

        _dbContext.WorkDays.RemoveRange(oldDays);
        _dbContext.SaveChanges();
    }
}
