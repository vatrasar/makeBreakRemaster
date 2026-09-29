using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using makeBreak.Src.Core.Domain.Interfaces;
using makeBreak.Src.Core.Domain.Models;

namespace makeBreak.Src.Infrastructure.Audio;

/// <summary>
/// Discovers audio output devices via PipeWire and WirePlumber utilities (pw-dump and wpctl).
/// </summary>
public sealed class SystemAudioDeviceService : IAudioDeviceService
{
    private static readonly Regex WpctlSinkRegex = new(@"\b(\d+)\.\s+(.+?)(?:\s+\[vol:|$)", RegexOptions.Compiled);

    /// <summary>
    /// Returns the collection of currently detected physical and logical audio output devices.
    /// Invoked by <c>SystemAudioPlayer</c> and <c>SettingsViewModel</c>.
    /// </summary>
    public IReadOnlyList<AudioDevice> GetOutputDevices()
    {
        List<AudioDevice> devices = QueryPipeWireDevices();
        if (devices.Count > 0)
        {
            return devices;
        }

        return QueryWirePlumberDevices();
    }

    private List<AudioDevice> QueryPipeWireDevices()
    {
        List<AudioDevice> devices = new();

        try
        {
            using Process? process = StartCommandProcess("pw-dump", "Node");
            if (process == null)
            {
                return devices;
            }

            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(2000);

            if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
            {
                ParsePipeWireNodes(output, devices);
            }
        }
        catch (Exception ex)
        {
            Trace.TraceWarning("Failed to query audio devices via pw-dump: {0}", ex);
        }

        return devices;
    }

    private static void ParsePipeWireNodes(string output, List<AudioDevice> devices)
    {
        using JsonDocument document = JsonDocument.Parse(output);
        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (JsonElement element in document.RootElement.EnumerateArray())
        {
            TryAppendPipeWireDevice(element, devices);
        }
    }

    private static void TryAppendPipeWireDevice(JsonElement element, List<AudioDevice> devices)
    {
        AudioDevice? device = ExtractAudioDevice(element);
        if (device != null && devices.All(d => d.Id != device.Id))
        {
            devices.Add(device);
        }
    }

    private static AudioDevice? ExtractAudioDevice(JsonElement element)
    {
        if (!element.TryGetProperty("info", out JsonElement info) ||
            !info.TryGetProperty("props", out JsonElement props))
        {
            return null;
        }

        if (!props.TryGetProperty("media.class", out JsonElement mediaClass) ||
            mediaClass.GetString() != "Audio/Sink")
        {
            return null;
        }

        if (!props.TryGetProperty("node.name", out JsonElement nodeNameElement) ||
            string.IsNullOrWhiteSpace(nodeNameElement.GetString()))
        {
            return null;
        }

        string nodeName = nodeNameElement.GetString()!;
        string displayName = ResolveDisplayName(props, nodeName);

        return new AudioDevice
        {
            Id = nodeName,
            Name = displayName,
        };
    }

    private static string ResolveDisplayName(JsonElement props, string fallback)
    {
        if (props.TryGetProperty("node.description", out JsonElement desc) &&
            !string.IsNullOrWhiteSpace(desc.GetString()))
        {
            return desc.GetString()!;
        }

        if (props.TryGetProperty("node.nick", out JsonElement nick) &&
            !string.IsNullOrWhiteSpace(nick.GetString()))
        {
            return nick.GetString()!;
        }

        return fallback;
    }

    private List<AudioDevice> QueryWirePlumberDevices()
    {
        List<AudioDevice> devices = new();

        try
        {
            using Process? process = StartCommandProcess("wpctl", "status");
            if (process == null)
            {
                return devices;
            }

            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(2000);

            if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
            {
                ParseWirePlumberSinks(output, devices);
            }
        }
        catch (Exception ex)
        {
            Trace.TraceWarning("Failed to query audio devices via wpctl: {0}", ex);
        }

        return devices;
    }

    private static void ParseWirePlumberSinks(string output, List<AudioDevice> devices)
    {
        bool inSinksSection = false;
        string[] lines = output.Split('\n');

        foreach (string line in lines)
        {
            inSinksSection = ProcessWirePlumberLine(line, inSinksSection, devices);
        }
    }

    private static bool ProcessWirePlumberLine(string line, bool inSinksSection, List<AudioDevice> devices)
    {
        if (line.Contains("Sinks:"))
        {
            return true;
        }

        if (!inSinksSection)
        {
            return false;
        }

        if (IsEndOfSinksSection(line))
        {
            return false;
        }

        Match match = WpctlSinkRegex.Match(line);
        if (match.Success)
        {
            string sinkId = match.Groups[1].Value.Trim();
            string sinkName = match.Groups[2].Value.Trim();

            if (devices.All(d => d.Id != sinkId))
            {
                devices.Add(new AudioDevice { Id = sinkId, Name = sinkName });
            }
        }

        return true;
    }

    private static bool IsEndOfSinksSection(string line) =>
        line.Contains("Sink endpoints:") ||
        line.Contains("Sources:") ||
        (line.Contains("├─") && !line.Contains("Sinks")) ||
        (line.Contains("└─") && !line.Contains("Sinks"));

    private static Process? StartCommandProcess(string fileName, string argument)
    {
        return Process.Start(new ProcessStartInfo
        {
            FileName = fileName,
            ArgumentList = { argument },
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        });
    }
}
