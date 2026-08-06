using makeBreak.Src.Core.Domain.Models;
using makeBreak.Src.Core.Domain.Services;
using makeBreak.Src.Core.Mvvm;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace makeBreak.Src.Features.Settings.UI;

/// <summary>
/// View model for the settings dialog. Captures the four schedule values in
/// display units (long-break values in minutes, short-break duration in seconds),
/// saves them through the break coordinator, and notifies listeners when
/// the dialog is saved or cancelled.
/// </summary>
public partial class SettingsViewModel : ViewModelBase
{
    private readonly BreakCoordinator _coordinator;

    public SettingsViewModel(BreakCoordinator coordinator)
    {
        _coordinator = coordinator;
        BreakConfig config = coordinator.CurrentConfig;

        TimeToStartLongBreakMinutes = SecondsToMinutes(config.TimeToStartLongBreak);
        TimeForLongBreakMinutes = SecondsToMinutes(config.TimeForLongBreak);
        TimeToStartShortBreakMinutes = SecondsToMinutes(config.TimeToStartShortBreak);
        TimeForShortBreakSeconds = config.TimeForShortBreak;
    }

    public event EventHandler? Saved;

    public event EventHandler? Cancelled;

    [Reactive]
    private int _timeToStartLongBreakMinutes;

    [Reactive]
    private int _timeForLongBreakMinutes;

    [Reactive]
    private int _timeToStartShortBreakMinutes;

    [Reactive]
    private int _timeForShortBreakSeconds;

    [ReactiveCommand]
    private void Cancel() => Cancelled?.Invoke(this, EventArgs.Empty);

    [ReactiveCommand]
    private void SaveSettings()
    {
        _coordinator.SaveSettings(new BreakConfig
        {
            TimeToStartLongBreak = MinutesToSeconds(TimeToStartLongBreakMinutes),
            TimeForLongBreak = MinutesToSeconds(TimeForLongBreakMinutes),
            TimeToStartShortBreak = MinutesToSeconds(TimeToStartShortBreakMinutes),
            TimeForShortBreak = TimeForShortBreakSeconds,
        });

        Saved?.Invoke(this, EventArgs.Empty);
    }

    private static int SecondsToMinutes(int seconds) => (int)Math.Ceiling(seconds / 60.0);

    private static int MinutesToSeconds(int minutes) => minutes * 60;
}