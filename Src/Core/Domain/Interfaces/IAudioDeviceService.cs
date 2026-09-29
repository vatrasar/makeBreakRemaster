using makeBreak.Src.Core.Domain.Models;

namespace makeBreak.Src.Core.Domain.Interfaces;

/// <summary>
/// Discovers available audio output devices on the host system.
/// </summary>
public interface IAudioDeviceService
{
    /// <summary>
    /// Returns the collection of currently detected physical and logical audio output devices.
    /// Invoked by <c>SystemAudioPlayer</c> and <c>SettingsViewModel</c>.
    /// </summary>
    IReadOnlyList<AudioDevice> GetOutputDevices();
}
