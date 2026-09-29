namespace makeBreak.Src.Core.Domain.Interfaces;

/// <summary>
/// Plays and stops audio files.
/// </summary>
public interface IAudioPlayer
{
    /// <summary>
    /// Occurs when playback finishes naturally or is stopped.
    /// Subscribed to by <c>BreakVoiceAlertService</c> and <c>SettingsViewModel</c>.
    /// </summary>
    event EventHandler? PlaybackFinished;

    /// <summary>
    /// Gets whether audio playback is currently in progress.
    /// Accessed by <c>BreakVoiceAlertService</c> and <c>SettingsViewModel</c>.
    /// </summary>
    bool IsPlaying { get; }

    /// <summary>
    /// Plays the audio file located at the specified file path using default routing.
    /// Invoked by <c>BreakVoiceAlertService</c>.
    /// </summary>
    void Play(string filePath);

    /// <summary>
    /// Plays the audio file located at the specified file path directed to a specific audio output device or "all".
    /// Invoked by <c>BreakVoiceAlertService</c>.
    /// </summary>
    void Play(string filePath, string? targetDeviceId);

    /// <summary>
    /// Plays the audio file located at the specified file path directed to a specific audio output device or "all" at the given volume percentage (0-100).
    /// Invoked by <c>BreakVoiceAlertService</c>.
    /// </summary>
    void Play(string filePath, string? targetDeviceId, int volumePercent);

    /// <summary>
    /// Stops any ongoing audio playback.
    /// Invoked by <c>BreakVoiceAlertService</c> and <c>SettingsViewModel</c>.
    /// </summary>
    void Stop();
}
