using Avalonia.Threading;
using makeBreak.Src.Core.Domain.Services;

namespace makeBreak.Src.Infrastructure.Navigation;

/// <summary>
/// Drives the break schedule one tick per second while the application runs.
/// </summary>
public sealed class BreakTicker : IDisposable
{
    private readonly BreakCoordinator _coordinator;
    private readonly DispatcherTimer _timer;

    public BreakTicker(BreakCoordinator coordinator)
    {
        _coordinator = coordinator;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += OnTick;
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Tick -= OnTick;
    }

    public void Start() => _timer.Start();

    private void OnTick(object? sender, EventArgs e) => _coordinator.Tick();
}