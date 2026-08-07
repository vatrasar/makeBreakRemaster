namespace makeBreak.Src.Features.Statistics.Domain.Models;

/// <summary>
/// Display data for a single bar in the statistics panel. A bar represents one day,
/// one week or one month depending on the currently selected statistics period.
/// </summary>
public sealed record WorkBarEntry(string Label, string WorkTimeLabel, double BarPercent, bool IsBest);