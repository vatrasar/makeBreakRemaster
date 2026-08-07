namespace makeBreak.Src.Core.Domain.Entities;

/// <summary>
/// Database entity representing the total work seconds accumulated on a single day.
/// </summary>
public sealed class WorkDayEntity
{
    public int Id { get; set; }

    public DateOnly Date { get; set; }

    public int WorkSeconds { get; set; }
}
