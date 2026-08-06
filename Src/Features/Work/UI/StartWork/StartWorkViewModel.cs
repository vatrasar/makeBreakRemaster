using makeBreak.Src.Core.Domain.Services;
using makeBreak.Src.Core.Mvvm;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace makeBreak.Src.Features.Work.UI.StartWork;

public sealed record StartWorkState;

/// <summary>
/// The routed start-work screen. It shows the app icon and a button that
/// begins the work session via the break coordinator.
/// </summary>
public sealed partial class StartWorkViewModel : ViewModelBase<StartWorkState>, IRoutableViewModel
{
    private readonly BreakCoordinator _coordinator;

    public StartWorkViewModel(IScreen hostScreen, BreakCoordinator coordinator) : base(new StartWorkState())
    {
        HostScreen = hostScreen;
        _coordinator = coordinator;
    }

    public string? UrlPathSegment => "start-work";

    public IScreen HostScreen { get; }

    [ReactiveCommand]
    private void StartWork() => _coordinator.StartWork();
}