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

    public bool CanConfirm { get; init; }
}

/// <summary>
/// The fullscreen break screen. Shows the remaining break countdown and, once the
/// countdown ends, enables a button confirming the break is over. The break only
/// ends when the user presses that button.
/// </summary>
public sealed partial class BreakViewModel : ViewModelBase<BreakState>, IRoutableViewModel, IActivatableViewModel
{
    private readonly BreakCoordinator _coordinator;

    public BreakViewModel(IScreen hostScreen, BreakCoordinator coordinator) : base(new BreakState())
    {
        HostScreen = hostScreen;
        _coordinator = coordinator;

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

            RefreshState();
        });
    }

    public ViewModelActivator Activator { get; } = new();

    public string? UrlPathSegment => "break";

    public IScreen HostScreen { get; }

    [ReactiveCommand]
    private void ConfirmBreak() => _coordinator.ConfirmBreak();

    private void RefreshState()
    {
        int remaining = _coordinator.Scheduler.RemainingBreakSeconds;
        bool finished = remaining == 0;
        int countdownLabel = _coordinator.Scheduler.BreakDurationSeconds;
        int progress = countdownLabel > 0 ? (int)Math.Round((double)remaining / countdownLabel * 100) : 0;

        UpdateState(s => s with
        {
            CountdownNumber = remaining.ToString(),
            CountdownProgress = progress,
            IsCountdownVisible = !finished,
            IsFinishedVisible = finished,
            CanConfirm = finished,
        });
    }
}