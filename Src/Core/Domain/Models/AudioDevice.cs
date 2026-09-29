namespace makeBreak.Src.Core.Domain.Models;

/// <summary>
/// Represents an audio output device or target selection.
/// </summary>
public sealed record AudioDevice
{
    public const string DefaultDeviceId = "default";
    public const string AllDevicesId = "all";

    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public override string ToString() => Name;
}
