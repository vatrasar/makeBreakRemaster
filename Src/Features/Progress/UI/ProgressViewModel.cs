using System.Reactive.Disposables;
using System.Reactive.Linq;
using makeBreak.Src.Core.Domain.Services;
using makeBreak.Src.Core.Mvvm;
using ReactiveUI;

namespace makeBreak.Src.Features.Progress.UI;

public sealed record ProgressState
{
    public int ShortProgressPercent { get; init; }

    public int LongProgressPercent { get; init; }
}

/// <summary>
/// View model for the progress dialog. Shows the live progress of the short
/// and long work intervals, refreshed every second via scheduler events.
/// </summary>
public sealed class ProgressViewModel : ViewModelBase<ProgressState>, IActivatableViewModel
{
    private readonly BreakCoordinator _coordinator;

    public ProgressViewModel(BreakCoordinator coordinator) : base(new ProgressState())
    {
        _coordinator = coordinator;
        Refresh();

        this.WhenActivated(disposables =>
        {
            Observable.FromEventPattern<EventHandler, EventArgs>(
                    h => _coordinator.ProgressChanged += h,
                    h => _coordinator.ProgressChanged -= h)
                .Subscribe(_ => Refresh())
                .DisposeWith(disposables);
        });
    }

    public ViewModelActivator Activator { get; } = new();

    private void Refresh()
    {
        UpdateState(s => s with
        {
            ShortProgressPercent = _coordinator.Scheduler.ShortProgressPercent,
            LongProgressPercent = _coordinator.Scheduler.LongProgressPercent,
        });
    }
}