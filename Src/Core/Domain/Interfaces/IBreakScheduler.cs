using makeBreak.Src.Core.Domain.Enums;
using makeBreak.Src.Core.Domain.Models;

namespace makeBreak.Src.Core.Domain.Interfaces;

/// <summary>
/// Drives the break schedule using one-second <see cref="Tick"/> calls.
/// </summary>
public interface IBreakScheduler
{
    event EventHandler? BreakStarted;

    event EventHandler? BreakCountdownChanged;

    event EventHandler? ConfirmationEnabledChanged;

    event EventHandler? BreakEnded;

    event EventHandler? StateChanged;

    event EventHandler? ProgressChanged;

    BreakConfig Config { get; }

    SessionState State { get; }

    BreakKind CurrentBreakKind { get; }

    int RemainingBreakSeconds { get; }

    int BreakDurationSeconds { get; }

    bool CanConfirmBreak { get; }

    int ShortProgressPercent { get; }

    int LongProgressPercent { get; }

    void ApplyConfig(BreakConfig config);

    void Start();

    void Stop();

    void Pause();

    void Resume();

    void ConfirmBreak();

    void Tick();
}