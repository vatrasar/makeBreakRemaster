using makeBreak.Src.Core.Domain.Enums;
using makeBreak.Src.Core.Domain.Interfaces;
using makeBreak.Src.Core.Domain.Models;

namespace makeBreak.Src.Core.Domain.Services;

/// <summary>
/// Engine implementing the break schedule. Runs on one-second <see cref="Tick"/> pulses.
/// </summary>
public sealed class BreakScheduler : IBreakScheduler
{
    private const int ZeroPercent = 0;

    private BreakConfig _config = new();

    private SessionState _state = SessionState.Paused;

    private BreakKind _currentBreakKind;

    private int _longWorkSeconds;

    private int _shortWorkSeconds;

    private int _breakElapsedSeconds;

    private bool _longBreakConsumed;

    private bool _canConfirmBreak;

    public event EventHandler? BreakStarted;

    public event EventHandler? BreakCountdownChanged;

    public event EventHandler? ConfirmationEnabledChanged;

    public event EventHandler? BreakEnded;

    public event EventHandler? StateChanged;

    public event EventHandler? ProgressChanged;

    public BreakConfig Config => _config;

    public SessionState State => _state;

    public BreakKind CurrentBreakKind => _currentBreakKind;

    public int BreakDurationSeconds => _currentBreakKind == BreakKind.Long ? _config.TimeForLongBreak : _config.TimeForShortBreak;

    public int RemainingBreakSeconds => Math.Max(BreakDurationSeconds - _breakElapsedSeconds, 0);

    public bool CanConfirmBreak => _canConfirmBreak;

    public int ShortProgressPercent => AsPercent(_shortWorkSeconds, _config.TimeToStartShortBreak);

    public int LongProgressPercent => AsPercent(_longWorkSeconds, _config.TimeToStartLongBreak);

    public void ApplyConfig(BreakConfig config)
    {
        _config = config;
        ResetAllCounters();
        _longBreakConsumed = false;
        _canConfirmBreak = false;
        RaiseProgressChanged();
    }

    public void Start()
    {
        ResetAllCounters();
        _longBreakConsumed = false;
        _canConfirmBreak = false;
        SetState(SessionState.Working);
        RaiseProgressChanged();
    }

    public void Pause()
    {
        if (_state is SessionState.OnShortBreak or SessionState.OnLongBreak || _state == SessionState.Paused)
        {
            return;
        }

        SetState(SessionState.Paused);
    }

    public void Stop()
    {
        bool wasOnBreak = _state is SessionState.OnShortBreak or SessionState.OnLongBreak;

        ResetAllCounters();
        _longBreakConsumed = false;
        _canConfirmBreak = false;
        SetState(SessionState.Paused);
        RaiseProgressChanged();

        if (wasOnBreak)
        {
            BreakEnded?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Resume()
    {
        ResetAllCounters();
        _longBreakConsumed = false;
        _canConfirmBreak = false;
        SetState(SessionState.Working);
        RaiseProgressChanged();
    }

    public void ConfirmBreak()
    {
        if (_state is not (SessionState.OnShortBreak or SessionState.OnLongBreak) || !_canConfirmBreak)
        {
            return;
        }

        ResetCountersAfterBreak();

        if (_currentBreakKind == BreakKind.Long)
        {
            _longBreakConsumed = false;
        }

        _canConfirmBreak = false;
        SetState(SessionState.Working);
        BreakEnded?.Invoke(this, EventArgs.Empty);
        RaiseProgressChanged();
    }

    public void Tick()
    {
        switch (_state)
        {
            case SessionState.Working:
                AdvanceWorkTime();
                break;
            case SessionState.OnShortBreak:
            case SessionState.OnLongBreak:
                AdvanceBreakCountdown();
                break;
        }
    }

    private void AdvanceWorkTime()
    {
        _longWorkSeconds++;
        _shortWorkSeconds++;
        RaiseProgressChanged();

        if (!_longBreakConsumed && _longWorkSeconds >= _config.TimeToStartLongBreak)
        {
            _longBreakConsumed = true;
            BeginBreak(BreakKind.Long);
        }
        else if (_shortWorkSeconds >= _config.TimeToStartShortBreak && CanScheduleShortBreak())
        {
            BeginBreak(BreakKind.Short);
        }
    }

    private void AdvanceBreakCountdown()
    {
        _breakElapsedSeconds++;

        if (!_canConfirmBreak && RemainingBreakSeconds == 0)
        {
            _canConfirmBreak = true;
            ConfirmationEnabledChanged?.Invoke(this, EventArgs.Empty);
        }

        BreakCountdownChanged?.Invoke(this, EventArgs.Empty);
    }

    private void BeginBreak(BreakKind kind)
    {
        _currentBreakKind = kind;
        _breakElapsedSeconds = 0;
        _canConfirmBreak = false;
        SetState(kind == BreakKind.Long ? SessionState.OnLongBreak : SessionState.OnShortBreak);
        BreakStarted?.Invoke(this, EventArgs.Empty);
    }

    private bool CanScheduleShortBreak()
    {
        return _config.TimeToStartLongBreak - _longWorkSeconds - _config.TimeForShortBreak > 0;
    }

    private void SetState(SessionState newState)
    {
        if (_state == newState)
        {
            return;
        }

        _state = newState;
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ResetAllCounters()
    {
        _longWorkSeconds = 0;
        _shortWorkSeconds = 0;
        _breakElapsedSeconds = 0;
    }

    private void ResetCountersAfterBreak()
    {
        _breakElapsedSeconds = 0;

        if (_currentBreakKind == BreakKind.Short)
        {
            _shortWorkSeconds = 0;
        }
        else
        {
            _longWorkSeconds = 0;
            _shortWorkSeconds = 0;
        }
    }

    private void RaiseProgressChanged() => ProgressChanged?.Invoke(this, EventArgs.Empty);

    private static int AsPercent(int elapsedSeconds, int totalSeconds)
    {
        if (totalSeconds <= 0)
        {
            return ZeroPercent;
        }

        return (int)Math.Clamp((double)elapsedSeconds / totalSeconds * 100, 0, 100);
    }
}