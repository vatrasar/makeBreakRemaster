using System.Reactive.Concurrency;
using makeBreak.Src.Core.Domain.Interfaces;
using makeBreak.Src.Core.Domain.Models;
using makeBreak.Src.Core.Domain.Services;
using makeBreak.Src.Core.Mvvm;
using makeBreak.Src.Features.Settings.Resources;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace makeBreak.Src.Features.Settings.UI;

/// <summary>
/// View model for the settings dialog. Captures schedule values, voice notification preference,
/// and audio output device selection. Allows triggering a test alert and persists settings through
/// the break coordinator.
/// </summary>
public partial class SettingsViewModel : ViewModelBase
{
    private readonly BreakCoordinator _coordinator;
    private readonly IBreakVoiceAlertService _voiceAlertService;
    private readonly IAudioDeviceService _audioDeviceService;

    public SettingsViewModel(
        BreakCoordinator coordinator,
        IBreakVoiceAlertService voiceAlertService,
        IAudioDeviceService audioDeviceService)
    {
        _coordinator = coordinator;
        _voiceAlertService = voiceAlertService;
        _audioDeviceService = audioDeviceService;

        BreakConfig config = coordinator.CurrentConfig;

        TimeToStartLongBreakMinutes = SecondsToMinutes(config.TimeToStartLongBreak);
        TimeForLongBreakMinutes = SecondsToMinutes(config.TimeForLongBreak);
        TimeToStartShortBreakMinutes = SecondsToMinutes(config.TimeToStartShortBreak);
        TimeForShortBreakSeconds = config.TimeForShortBreak;
        AreVoiceNotificationsEnabled = config.AreVoiceNotificationsEnabled;

        AvailableAudioDevices = BuildAvailableAudioDevices(_audioDeviceService.GetOutputDevices(), config.AudioOutputDeviceId);
        SelectedAudioDevice = ResolveInitialDevice(AvailableAudioDevices, config.AudioOutputDeviceId);

        _voiceAlertService.PlaybackFinished += (_, _) =>
        {
            RxApp.MainThreadScheduler.Schedule(() => IsTestingAudio = false);
        };
    }

    public IReadOnlyList<AudioDevice> AvailableAudioDevices { get; }

    public event EventHandler? Saved;

    public event EventHandler? Cancelled;

    [Reactive]
    private int _timeToStartLongBreakMinutes;

    [Reactive]
    private int _timeForLongBreakMinutes;

    [Reactive]
    private int _timeToStartShortBreakMinutes;

    [Reactive]
    private int _timeForShortBreakSeconds;

    [Reactive]
    private bool _areVoiceNotificationsEnabled;

    [Reactive]
    private AudioDevice? _selectedAudioDevice;

    [Reactive]
    private bool _isTestingAudio;

    [ReactiveCommand]
    private void Cancel()
    {
        StopTestAudio();
        Cancelled?.Invoke(this, EventArgs.Empty);
    }

    [ReactiveCommand]
    private void SaveSettings()
    {
        StopTestAudio();
        _coordinator.SaveSettings(new BreakConfig
        {
            TimeToStartLongBreak = MinutesToSeconds(TimeToStartLongBreakMinutes),
            TimeForLongBreak = MinutesToSeconds(TimeForLongBreakMinutes),
            TimeToStartShortBreak = MinutesToSeconds(TimeToStartShortBreakMinutes),
            TimeForShortBreak = TimeForShortBreakSeconds,
            AreVoiceNotificationsEnabled = AreVoiceNotificationsEnabled,
            AudioOutputDeviceId = SelectedAudioDevice?.Id ?? AudioDevice.DefaultDeviceId,
        });

        Saved?.Invoke(this, EventArgs.Empty);
    }

    [ReactiveCommand]
    private void ToggleTestAudio()
    {
        if (_isTestingAudio)
        {
            StopTestAudio();
            return;
        }

        string targetDeviceId = SelectedAudioDevice?.Id ?? AudioDevice.DefaultDeviceId;
        _voiceAlertService.PlayTestAlert(targetDeviceId);
        IsTestingAudio = true;
    }

    public void StopTestAudio()
    {
        if (_isTestingAudio)
        {
            _voiceAlertService.StopTestAlert();
            IsTestingAudio = false;
        }
    }

    private static IReadOnlyList<AudioDevice> BuildAvailableAudioDevices(IReadOnlyList<AudioDevice>? detectedDevices, string? configuredDeviceId)
    {
        List<AudioDevice> list = new()
        {
            new AudioDevice { Id = AudioDevice.DefaultDeviceId, Name = SettingsStrings.DefaultAudioOutput },
            new AudioDevice { Id = AudioDevice.AllDevicesId, Name = SettingsStrings.AllAudioOutputs },
        };

        if (detectedDevices != null)
        {
            foreach (AudioDevice device in detectedDevices)
            {
                if (list.All(d => !string.Equals(d.Id, device.Id, StringComparison.OrdinalIgnoreCase)))
                {
                    list.Add(device);
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(configuredDeviceId) &&
            list.All(d => !string.Equals(d.Id, configuredDeviceId, StringComparison.OrdinalIgnoreCase)))
        {
            list.Add(new AudioDevice { Id = configuredDeviceId, Name = configuredDeviceId });
        }

        return list;
    }

    private static AudioDevice ResolveInitialDevice(IReadOnlyList<AudioDevice> devices, string? configuredDeviceId)
    {
        string targetId = string.IsNullOrWhiteSpace(configuredDeviceId) ? AudioDevice.DefaultDeviceId : configuredDeviceId;
        AudioDevice? found = devices.FirstOrDefault(d => string.Equals(d.Id, targetId, StringComparison.OrdinalIgnoreCase));
        return found ?? devices[0];
    }

    private static int SecondsToMinutes(int seconds) => (int)Math.Ceiling(seconds / 60.0);

    private static int MinutesToSeconds(int minutes) => minutes * 60;
}