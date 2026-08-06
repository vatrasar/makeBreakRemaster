using System.Reactive.Linq;
using makeBreak.Src.Core.Domain.Services;
using makeBreak.Src.Core.Mvvm;
using makeBreak.Src.Features.Break.UI.BreakScreen;
using makeBreak.Src.Features.Work.UI.StartWork;
using ReactiveUI;

namespace makeBreak.Src.Features.Shell.UI.Host;

/// <summary>
/// The shell view model owns routing (serves as <c>IScreen</c>) and coordinates
/// navigation between the routed start-work and break screens based on break
/// schedule events raised by <see cref="BreakCoordinator"/>.
/// </summary>
public sealed class MainShellViewModel : ViewModelBase<MainShellState>, IScreen
{
    private readonly BreakCoordinator _coordinator;

    public MainShellViewModel(BreakCoordinator coordinator) : base(new MainShellState())
    {
        _coordinator = coordinator;
        Router = new RoutingState();

        _coordinator.BreakStarted += (_, _) => Router.Navigate.Execute(new BreakViewModel(this, _coordinator));
        _coordinator.BreakEnded += (_, _) => Router.Navigate.Execute(new StartWorkViewModel(this, _coordinator));

        Router.Navigate.Execute(new StartWorkViewModel(this, _coordinator));
    }

    public RoutingState Router { get; }
}

public sealed record MainShellState;