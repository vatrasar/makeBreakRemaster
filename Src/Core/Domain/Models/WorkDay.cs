namespace makeBreak.Src.Core.Domain.Models;

/// <summary>
/// Total work seconds accumulated on a single day.
/// </summary>
public sealed record WorkDay(DateOnly Date, int WorkSeconds);
