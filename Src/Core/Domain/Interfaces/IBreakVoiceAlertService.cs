using makeBreak.Src.Core.Domain.Enums;

namespace makeBreak.Src.Core.Domain.Interfaces;

/// <summary>
/// Manages voice notifications for finished breaks and break overtime.
/// </summary>
public interface IBreakVoiceAlertService
{
    /// <summary>
    /// Occurs when the mute state for the current break changes.
    /// </summary>
    event EventHandler? MuteStateChanged;

    /// <summary>
    /// Occurs when audio playback finishes naturally or is stopped.
    /// Subscribed to by <c>SettingsViewModel</c>.
    /// </summary>
    event EventHandler? PlaybackFinished;

    /// <summary>
    /// Gets whether voice alerts are temporarily muted for the current break session.
    /// </summary>
    bool IsMutedForCurrentBreak { get; }

    /// <summary>
    /// Gets whether audio playback is currently active.
    /// Checked by <c>SettingsViewModel</c>.
    /// </summary>
    bool IsAudioPlaying { get; }

    /// <summary>
    /// Mutes voice alerts for the current break session and halts active playback.
    /// Invoked by <c>BreakViewModel</c>.
    /// </summary>
    void MuteCurrentBreak();

    /// <summary>
    /// Unmutes voice alerts for the current break session.
    /// Invoked by <c>BreakViewModel</c>.
    /// </summary>
    void UnmuteCurrentBreak();

    /// <summary>
    /// Toggles the mute state for the current break session.
    /// Invoked by <c>BreakViewModel</c>.
    /// </summary>
    void ToggleMuteCurrentBreak();

    /// <summary>
    /// Determines the voice prompt stage corresponding to the break duration and elapsed overtime.
    /// Invoked by <c>BreakVoiceAlertService</c> and test suites.
    /// </summary>
    VoicePromptStage DetermineStage(int breakDurationSeconds, int overtimeSeconds);

    /// <summary>
    /// Plays a sample voice prompt directed to the specified audio output device or "all".
    /// Invoked by <c>SettingsViewModel</c>.
    /// </summary>
    void PlayTestAlert(string? targetDeviceId = null);

    /// <summary>
    /// Stops any currently playing voice alert or test alert.
    /// Invoked by <c>SettingsViewModel</c>.
    /// </summary>
    void StopTestAlert();
}
