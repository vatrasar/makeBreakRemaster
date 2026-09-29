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
    /// Gets whether voice alerts are temporarily muted for the current break session.
    /// </summary>
    bool IsMutedForCurrentBreak { get; }

    /// <summary>
    /// Mutes voice alerts for the current break session and halts active playback.
    /// </summary>
    void MuteCurrentBreak();

    /// <summary>
    /// Unmutes voice alerts for the current break session.
    /// </summary>
    void UnmuteCurrentBreak();

    /// <summary>
    /// Toggles the mute state for the current break session.
    /// </summary>
    void ToggleMuteCurrentBreak();

    /// <summary>
    /// Determines the voice prompt stage corresponding to the break duration and elapsed overtime.
    /// </summary>
    VoicePromptStage DetermineStage(int breakDurationSeconds, int overtimeSeconds);
}
