namespace makeBreak.Src.Core.Domain.Interfaces;

/// <summary>
/// Plays and stops audio files.
/// </summary>
public interface IAudioPlayer
{
    /// <summary>
    /// Plays the audio file located at the specified file path.
    /// </summary>
    void Play(string filePath);

    /// <summary>
    /// Stops any ongoing audio playback.
    /// </summary>
    void Stop();
}
