using System.Diagnostics;
using System.Globalization;
using makeBreak.Src.Core.Domain.Interfaces;
using makeBreak.Src.Core.Domain.Models;

namespace makeBreak.Src.Infrastructure.Audio;

/// <summary>
/// Plays audio files via Linux media player utilities (PipeWire pw-play, GStreamer gst-play-1.0, or ffplay).
/// </summary>
public sealed class SystemAudioPlayer : IAudioPlayer, IDisposable
{
    private readonly object _lock = new();
    private readonly string? _playerExecutable;
    private readonly IAudioDeviceService _audioDeviceService;
    private readonly List<Process> _currentProcesses = new();

    public SystemAudioPlayer() : this(new SystemAudioDeviceService())
    {
    }

    public SystemAudioPlayer(IAudioDeviceService audioDeviceService)
    {
        _audioDeviceService = audioDeviceService;
        _playerExecutable = ResolvePlayerBinary();
    }

    public event EventHandler? PlaybackFinished;

    public bool IsPlaying
    {
        get
        {
            lock (_lock)
            {
                return _currentProcesses.Count > 0;
            }
        }
    }

    /// <summary>
    /// Plays the audio file located at the specified file path using default routing.
    /// Invoked by <c>BreakVoiceAlertService</c>.
    /// </summary>
    public void Play(string filePath) => Play(filePath, null, 100);

    /// <summary>
    /// Plays the audio file located at the specified file path directed to a specific audio output device or "all".
    /// Invoked by <c>BreakVoiceAlertService</c>.
    /// </summary>
    public void Play(string filePath, string? targetDeviceId) => Play(filePath, targetDeviceId, 100);

    /// <summary>
    /// Plays the audio file located at the specified file path directed to a specific audio output device or "all" at the given volume percentage (0-100).
    /// Invoked by <c>BreakVoiceAlertService</c>.
    /// </summary>
    public void Play(string filePath, string? targetDeviceId, int volumePercent)
    {
        if (string.IsNullOrWhiteSpace(_playerExecutable) || !File.Exists(filePath))
        {
            return;
        }

        int clampedVolume = Math.Clamp(volumePercent, 0, 100);

        lock (_lock)
        {
            StopInternal(notifyFinished: false);
            RoutePlayback(filePath, targetDeviceId, clampedVolume);
        }
    }

    /// <summary>
    /// Stops any ongoing audio playback.
    /// Invoked by <c>BreakVoiceAlertService</c> and <c>SettingsViewModel</c>.
    /// </summary>
    public void Stop()
    {
        lock (_lock)
        {
            StopInternal(notifyFinished: true);
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            StopInternal(notifyFinished: true);
        }
    }

    private void RoutePlayback(string filePath, string? targetDeviceId, int volumePercent)
    {
        if (string.Equals(targetDeviceId, AudioDevice.AllDevicesId, StringComparison.OrdinalIgnoreCase))
        {
            PlayToAllOutputs(filePath, volumePercent);
            return;
        }

        if (string.IsNullOrWhiteSpace(targetDeviceId) ||
            string.Equals(targetDeviceId, AudioDevice.DefaultDeviceId, StringComparison.OrdinalIgnoreCase))
        {
            StartProcess(filePath, null, volumePercent);
            return;
        }

        PlayToSingleOutput(filePath, targetDeviceId, volumePercent);
    }

    private void PlayToAllOutputs(string filePath, int volumePercent)
    {
        IReadOnlyList<AudioDevice> devices = _audioDeviceService.GetOutputDevices();
        if (devices.Count == 0)
        {
            StartProcess(filePath, null, volumePercent);
            return;
        }

        foreach (AudioDevice device in devices)
        {
            StartProcess(filePath, device.Id, volumePercent);
        }
    }

    private void PlayToSingleOutput(string filePath, string targetDeviceId, int volumePercent)
    {
        IReadOnlyList<AudioDevice> devices = _audioDeviceService.GetOutputDevices();
        if (devices.Count > 0 && devices.All(d => !string.Equals(d.Id, targetDeviceId, StringComparison.OrdinalIgnoreCase)))
        {
            Trace.TraceWarning("Configured audio output device {0} not found, falling back to default.", targetDeviceId);
            StartProcess(filePath, null, volumePercent);
            return;
        }

        StartProcess(filePath, targetDeviceId, volumePercent);
    }

    private void StartProcess(string filePath, string? targetDeviceId, int volumePercent)
    {
        try
        {
            ProcessStartInfo startInfo = CreateStartInfo(filePath, targetDeviceId, volumePercent);
            Process? process = new()
            {
                StartInfo = startInfo,
                EnableRaisingEvents = true,
            };

            process.Exited += (_, _) => HandleProcessExited(process);

            if (process.Start())
            {
                _currentProcesses.Add(process);
            }
            else
            {
                process.Dispose();
            }
        }
        catch (Exception ex)
        {
            Trace.TraceError("Failed to start audio player process: {0}", ex);
        }
    }

    private void HandleProcessExited(Process process)
    {
        bool hasFinishedAll = false;

        lock (_lock)
        {
            if (_currentProcesses.Remove(process))
            {
                SafelyDisposeProcess(process);
                hasFinishedAll = _currentProcesses.Count == 0;
            }
        }

        if (hasFinishedAll)
        {
            PlaybackFinished?.Invoke(this, EventArgs.Empty);
        }
    }

    private ProcessStartInfo CreateStartInfo(string filePath, string? targetDeviceId, int volumePercent)
    {
        ProcessStartInfo startInfo = new()
        {
            FileName = _playerExecutable!,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        startInfo.Environment["LC_ALL"] = "C";
        startInfo.Environment["LC_NUMERIC"] = "C";

        AppendPlayerArguments(startInfo, filePath, targetDeviceId, volumePercent);
        return startInfo;
    }

    private void AppendPlayerArguments(ProcessStartInfo startInfo, string filePath, string? targetDeviceId, int volumePercent)
    {
        string binaryName = Path.GetFileName(_playerExecutable!);

        switch (binaryName)
        {
            case "gst-play-1.0":
                startInfo.ArgumentList.Add("--no-interactive");
                startInfo.ArgumentList.Add("-q");
                startInfo.ArgumentList.Add($"--volume={(volumePercent / 100.0).ToString(CultureInfo.InvariantCulture)}");
                if (!string.IsNullOrWhiteSpace(targetDeviceId))
                {
                    startInfo.ArgumentList.Add($"--audiosink=pipewiresink target={targetDeviceId}");
                }
                startInfo.ArgumentList.Add(filePath);
                break;
            case "ffplay":
                startInfo.ArgumentList.Add("-nodisp");
                startInfo.ArgumentList.Add("-autoexit");
                startInfo.ArgumentList.Add("-loglevel");
                startInfo.ArgumentList.Add("quiet");
                startInfo.ArgumentList.Add("-volume");
                startInfo.ArgumentList.Add(volumePercent.ToString(CultureInfo.InvariantCulture));
                startInfo.ArgumentList.Add(filePath);
                break;
            default:
                if (!string.IsNullOrWhiteSpace(targetDeviceId))
                {
                    startInfo.ArgumentList.Add("--target");
                    startInfo.ArgumentList.Add(targetDeviceId);
                }
                startInfo.ArgumentList.Add("--volume");
                startInfo.ArgumentList.Add((volumePercent / 100.0).ToString(CultureInfo.InvariantCulture));
                startInfo.ArgumentList.Add(filePath);
                break;
        }
    }

    private void StopInternal(bool notifyFinished)
    {
        if (_currentProcesses.Count == 0)
        {
            return;
        }

        foreach (Process process in _currentProcesses)
        {
            TerminateProcess(process);
        }

        _currentProcesses.Clear();

        if (notifyFinished)
        {
            PlaybackFinished?.Invoke(this, EventArgs.Empty);
        }
    }

    private static void TerminateProcess(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill();
            }
        }
        catch (Exception ex)
        {
            Trace.TraceWarning("Failed to kill audio player process: {0}", ex);
        }
        finally
        {
            process.Dispose();
        }
    }

    private static void SafelyDisposeProcess(Process process)
    {
        try
        {
            process.Dispose();
        }
        catch (Exception ex)
        {
            Trace.TraceWarning("Failed to dispose process: {0}", ex);
        }
    }

    private static string? ResolvePlayerBinary()
    {
        string[] candidates = { "pw-play", "gst-play-1.0", "ffplay" };

        foreach (string candidate in candidates)
        {
            if (IsCommandAvailable(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static bool IsCommandAvailable(string command)
    {
        try
        {
            using Process? process = Process.Start(new ProcessStartInfo
            {
                FileName = "which",
                ArgumentList = { command },
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            });

            if (process == null)
            {
                return false;
            }

            process.WaitForExit();
            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            Trace.TraceWarning("Error executing which for command {0}: {1}", command, ex);
            return false;
        }
    }
}
