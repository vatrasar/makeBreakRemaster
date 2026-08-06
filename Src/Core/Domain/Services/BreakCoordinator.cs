using makeBreak.Src.Core.Domain.Interfaces;
using makeBreak.Src.Core.Domain.Models;

namespace makeBreak.Src.Core.Domain.Services;

/// <summary>
/// Coordinates the break schedule with configuration persistence.
/// Raises events that the UI layer reacts to. The UI layer is responsible
/// for driving one-second ticks via <see cref="TickScheduler"/>.
/// </summary>
public sealed class BreakCoordinator
{
    private readonly IBreakScheduler _scheduler;
    private readonly ConfigService _configService;

    public BreakCoordinator(IBreakScheduler scheduler, ConfigService configService)
    {
        _scheduler = scheduler;
        _configService = configService;

        scheduler.BreakStarted += (_, _) => BreakStarted?.Invoke(this, EventArgs.Empty);
        scheduler.BreakEnded += (_, _) => BreakEnded?.Invoke(this, EventArgs.Empty);
        scheduler.StateChanged += (_, _) => StateChanged?.Invoke(this, EventArgs.Empty);
        scheduler.ConfirmationEnabledChanged += (_, _) => ConfirmationEnabledChanged?.Invoke(this, EventArgs.Empty);
        scheduler.BreakCountdownChanged += (_, _) => BreakCountdownChanged?.Invoke(this, EventArgs.Empty);
        scheduler.ProgressChanged += (_, _) => ProgressChanged?.Invoke(this, EventArgs.Empty);

        CurrentConfig = _configService.GetCurrentConfig();
        _scheduler.ApplyConfig(CurrentConfig);
    }

    public event EventHandler? BreakStarted;

    public event EventHandler? BreakEnded;

    public event EventHandler? StateChanged;

    public event EventHandler? ConfirmationEnabledChanged;

    public event EventHandler? BreakCountdownChanged;

    public event EventHandler? ProgressChanged;

    public IBreakScheduler Scheduler => _scheduler;

    public BreakConfig CurrentConfig { get; private set; }

    public void StartWork() => _scheduler.Start();

    public void StopSchedule() => _scheduler.Stop();

    public void ResumeSchedule() => _scheduler.Resume();

    public void ConfirmBreak() => _scheduler.ConfirmBreak();

    /// <summary>
    /// Advances the schedule by one second. Invoked by the UI ticker each second.
    /// </summary>
    public void Tick() => _scheduler.Tick();

    /// <summary>
    /// Persists the given schedule values to disk and applies them to the scheduler.
    /// Invoked when settings are saved.
    /// </summary>
    public void SaveSettings(BreakConfig config)
    {
        _configService.Save(config);
        CurrentConfig = config;
        _scheduler.ApplyConfig(config);
    }
}