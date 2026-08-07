using makeBreak.Src.Core.Domain.Models;

namespace makeBreak.Src.Core.Domain.RepositoryContracts;

/// <summary>
/// Loads and persists daily work time records in the SQLite database.
/// </summary>
public interface IWorkTimeRepository
{
    /// <summary>
    /// Adds the given number of work seconds to the record of the given day,
    /// creating the record when it does not exist yet.
    /// Invoked by <c>WorkTimeService</c> whenever accumulated work is flushed.
    /// </summary>
    void AddWorkSeconds(DateOnly date, int seconds);

    /// <summary>
    /// Returns the work time records whose date falls inside the given inclusive range.
    /// Invoked by <c>WorkTimeService</c> when reading the work time for the statistics window.
    /// </summary>
    IReadOnlyList<WorkDay> GetWorkDaysInRange(DateOnly fromDate, DateOnly toDate);

    /// <summary>
    /// Deletes every work time record older than the given date (exclusive).
    /// Invoked by <c>WorkTimeService</c> at application startup.
    /// </summary>
    void DeleteOlderThan(DateOnly cutoffDate);
}
