namespace makeBreak.Src.Core.Domain.Models;

/// <summary>
/// Configuration values controlling the break schedule, stored in seconds.
/// </summary>
public sealed record BreakConfig
{
    public int TimeToStartLongBreak { get; init; }

    public int TimeForLongBreak { get; init; }

    public int TimeToStartShortBreak { get; init; }

    public int TimeForShortBreak { get; init; }
}