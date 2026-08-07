namespace makeBreak.Src.Features.Statistics.Domain.Enums;

/// <summary>
/// Time window displayed in the statistics panel: a week of days, the current
/// calendar month split into weeks, or the last twelve months split by calendar month.
/// </summary>
public enum StatisticsPeriod
{
    Week,
    Month,
    Year,
}