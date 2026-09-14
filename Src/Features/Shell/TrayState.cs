using Avalonia.Controls;
using makeBreak.Src.Core.Domain.Enums;
using makeBreak.Src.Core.Domain.Services;

namespace makeBreak.Src.Features.Shell;

/// <summary>
/// Keeps the tray menu enabled states in sync with the break schedule.
/// "Stop" is enabled while working or relaxing; "Resume" is enabled while paused;
/// "Relax" reflects whether the relax mode is active.
/// </summary>
public sealed class TrayState
{
    private readonly BreakCoordinator _coordinator;
    private NativeMenuItem? _stopMenuItem;
    private NativeMenuItem? _resumeMenuItem;
    private NativeMenuItem? _relaxMenuItem;

    public TrayState(BreakCoordinator coordinator)
    {
        _coordinator = coordinator;
        _coordinator.StateChanged += (_, _) => Refresh();
    }

    /// <summary>
    /// Attaches the native menu items to this tray state tracker.
    /// Invoked by App during initialization.
    /// </summary>
    public void Attach(NativeMenuItem stopMenuItem, NativeMenuItem resumeMenuItem, NativeMenuItem relaxMenuItem)
    {
        _stopMenuItem = stopMenuItem;
        _resumeMenuItem = resumeMenuItem;
        _relaxMenuItem = relaxMenuItem;
        Refresh();
    }

    private void Refresh()
    {
        SessionState state = _coordinator.Scheduler.State;

        if (_stopMenuItem is not null)
        {
            _stopMenuItem.IsEnabled = state is SessionState.Working or SessionState.Relaxing;
        }

        if (_resumeMenuItem is not null)
        {
            _resumeMenuItem.IsEnabled = state == SessionState.Paused;
        }

        if (_relaxMenuItem is not null)
        {
            _relaxMenuItem.IsChecked = _coordinator.Scheduler.IsRelaxMode;
        }
    }
}