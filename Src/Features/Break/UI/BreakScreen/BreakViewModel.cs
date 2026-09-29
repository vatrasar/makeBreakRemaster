using System.Reactive.Disposables;
using System.Reactive.Linq;
using makeBreak.Src.Core.Domain.Services;
using makeBreak.Src.Core.Mvvm;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace makeBreak.Src.Features.Break.UI.BreakScreen;

public sealed record BreakState
{
    public string CountdownNumber { get; init; } = "0";

    public int CountdownProgress { get; init; } = 100;

    public bool IsCountdownVisible { get; init; }

    public bool IsFinishedVisible { get; init; }

    public string OvertimeNumber { get; init; } = "+00:00";

    public bool CanConfirm { get; init; }

    public bool IsMuteButtonVisible { get; init; }

    public bool IsMuted { get; init; }
}

/// <summary>
/// The fullscreen break screen. Shows the remaining break countdown and, once the
/// countdown ends, enables a button confirming the break is over and displays the
/// overtime elapsed since the countdown ended. The break only ends when the user presses that button.
/// </summary>
public sealed partial class BreakViewModel : ViewModelBase<BreakState>, IRoutableViewModel, IActivatableViewModel
{
    private readonly BreakCoordinator _coordinator;
    private readonly makeBreak.Src.Core.Domain.Interfaces.IBreakVoiceAlertService? _voiceAlertService;

    public BreakViewModel(
        IScreen hostScreen,
        BreakCoordinator coordinator,
        makeBreak.Src.Core.Domain.Interfaces.IBreakVoiceAlertService? voiceAlertService = null) : base(new BreakState())
    {
        HostScreen = hostScreen;
        _coordinator = coordinator;
        _voiceAlertService = voiceAlertService;

        this.WhenActivated(disposables =>
        {
            Observable.FromEventPattern<EventHandler, EventArgs>(
                    h => _coordinator.BreakCountdownChanged += h,
                    h => _coordinator.BreakCountdownChanged -= h)
                .Subscribe(_ => RefreshState())
                .DisposeWith(disposables);

            Observable.FromEventPattern<EventHandler, EventArgs>(
                    h => _coordinator.ConfirmationEnabledChanged += h,
                    h => _coordinator.ConfirmationEnabledChanged -= h)
                .Subscribe(_ => RefreshState())
                .DisposeWith(disposables);

            if (_voiceAlertService != null)
            {
                Observable.FromEventPattern<EventHandler, EventArgs>(
                        h => _voiceAlertService.MuteStateChanged += h,
                        h => _voiceAlertService.MuteStateChanged -= h)
                    .Subscribe(_ => RefreshState())
                    .DisposeWith(disposables);
            }

            RefreshState();
        });
    }

    public ViewModelActivator Activator { get; } = new();

    public string? UrlPathSegment => "break";

    public IScreen HostScreen { get; }

    [ReactiveCommand]
    private void ConfirmBreak() => _coordinator.ConfirmBreak();

    [ReactiveCommand]
    private void ToggleMuteVoiceAlerts() => _voiceAlertService?.ToggleMuteCurrentBreak();

    private void RefreshState()
    {
        int remaining = _coordinator.Scheduler.RemainingBreakSeconds;
        bool finished = remaining == 0;
        int countdownLabel = _coordinator.Scheduler.BreakDurationSeconds;
        int progress = countdownLabel > 0 ? (int)Math.Round((double)remaining / countdownLabel * 100) : 0;
        int overtime = _coordinator.Scheduler.OvertimeBreakSeconds;
        bool isVoiceEnabled = _coordinator.CurrentConfig.AreVoiceNotificationsEnabled;
        bool isMuted = _voiceAlertService?.IsMutedForCurrentBreak ?? false;

        UpdateState(s => s with
        {
            CountdownNumber = remaining.ToString(),
            CountdownProgress = progress,
            IsCountdownVisible = !finished,
            IsFinishedVisible = finished,
            OvertimeNumber = FormatOvertime(overtime),
            CanConfirm = finished,
            IsMuteButtonVisible = isVoiceEnabled,
            IsMuted = isMuted,
        });
    }

    private static string FormatOvertime(int totalSeconds)
    {
        int hours = totalSeconds / 3600;
        int minutes = totalSeconds % 3600 / 60;
        int seconds = totalSeconds % 60;

        if (hours > 0)
        {
            return $"+{hours}:{minutes:D2}:{seconds:D2}";
        }

        return $"+{minutes:D2}:{seconds:D2}";
    }
}