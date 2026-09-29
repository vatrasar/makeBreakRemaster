using System.Diagnostics;
using makeBreak.Src.Core.Domain.Interfaces;

namespace makeBreak.Src.Infrastructure.Audio;

/// <summary>
/// Plays audio files via Linux media player utilities (PipeWire pw-play, GStreamer gst-play-1.0, or ffplay).
/// </summary>
public sealed class SystemAudioPlayer : IAudioPlayer, IDisposable
{
    private readonly object _lock = new();
    private readonly string? _playerExecutable;
    private Process? _currentProcess;

    public SystemAudioPlayer()
    {
        _playerExecutable = ResolvePlayerBinary();
    }

    /// <summary>
    /// Plays the audio file located at the specified file path.
    /// </summary>
    public void Play(string filePath)
    {
        if (string.IsNullOrWhiteSpace(_playerExecutable) || !File.Exists(filePath))
        {
            return;
        }

        lock (_lock)
        {
            StopInternal();
            StartProcess(filePath);
        }
    }

    /// <summary>
    /// Stops any ongoing audio playback.
    /// </summary>
    public void Stop()
    {
        lock (_lock)
        {
            StopInternal();
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            StopInternal();
        }
    }

    private void StartProcess(string filePath)
    {
        try
        {
            ProcessStartInfo startInfo = CreateStartInfo(filePath);
            _currentProcess = Process.Start(startInfo);
        }
        catch (Exception ex)
        {
            Trace.TraceError("Failed to start audio player process: {0}", ex);
            _currentProcess = null;
        }
    }

    private ProcessStartInfo CreateStartInfo(string filePath)
    {
        ProcessStartInfo startInfo = new()
        {
            FileName = _playerExecutable!,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        AppendPlayerArguments(startInfo, filePath);
        return startInfo;
    }

    private void AppendPlayerArguments(ProcessStartInfo startInfo, string filePath)
    {
        string binaryName = Path.GetFileName(_playerExecutable!);

        switch (binaryName)
        {
            case "gst-play-1.0":
                startInfo.ArgumentList.Add("--no-interactive");
                startInfo.ArgumentList.Add("-q");
                startInfo.ArgumentList.Add(filePath);
                break;
            case "ffplay":
                startInfo.ArgumentList.Add("-nodisp");
                startInfo.ArgumentList.Add("-autoexit");
                startInfo.ArgumentList.Add("-loglevel");
                startInfo.ArgumentList.Add("quiet");
                startInfo.ArgumentList.Add(filePath);
                break;
            default:
                startInfo.ArgumentList.Add(filePath);
                break;
        }
    }

    private void StopInternal()
    {
        if (_currentProcess == null)
        {
            return;
        }

        try
        {
            if (!_currentProcess.HasExited)
            {
                _currentProcess.Kill();
            }
        }
        catch (Exception ex)
        {
            Trace.TraceWarning("Failed to kill audio player process: {0}", ex);
        }
        finally
        {
            _currentProcess.Dispose();
            _currentProcess = null;
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
