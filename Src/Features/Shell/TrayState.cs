using Avalonia.Controls;
using makeBreak.Src.Core.Domain.Enums;
using makeBreak.Src.Core.Domain.Services;

namespace makeBreak.Src.Features.Shell;

/// <summary>
/// Keeps the tray menu enabled states in sync with the break schedule.
/// "Zatrzymaj" is enabled while working; "Wznów" is enabled while paused.
/// </summary>
public sealed class TrayState
{
    private readonly BreakCoordinator _coordinator;
    private NativeMenuItem? _stopMenuItem;
    private NativeMenuItem? _resumeMenuItem;

    public TrayState(BreakCoordinator coordinator)
    {
        _coordinator = coordinator;
        _coordinator.StateChanged += (_, _) => Refresh();
    }

    public void Attach(NativeMenuItem stopMenuItem, NativeMenuItem resumeMenuItem)
    {
        _stopMenuItem = stopMenuItem;
        _resumeMenuItem = resumeMenuItem;
        Refresh();
    }

    private void Refresh()
    {
        SessionState state = _coordinator.Scheduler.State;

        if (_stopMenuItem is not null)
        {
            _stopMenuItem.IsEnabled = state == SessionState.Working;
        }

        if (_resumeMenuItem is not null)
        {
            _resumeMenuItem.IsEnabled = state == SessionState.Paused;
        }
    }
}