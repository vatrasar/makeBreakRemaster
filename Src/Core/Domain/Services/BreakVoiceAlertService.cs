using makeBreak.Src.Core.Config;
using makeBreak.Src.Core.Domain.Enums;
using makeBreak.Src.Core.Domain.Interfaces;
using makeBreak.Src.Core.Domain.Models;
using Microsoft.Extensions.Options;

namespace makeBreak.Src.Core.Domain.Services;

/// <summary>
/// Service coordinating voice prompt audio alerts upon break completion and recurring overtime intervals.
/// </summary>
public sealed class BreakVoiceAlertService : IBreakVoiceAlertService
{
    private const string InitialFolder = "FirstPrompt";
    private const string Overtime100Folder = "SecondPrompt";
    private const string Overtime200Folder = "ThirdPrompt";
    private const int SecondsPerMinute = 60;

    private readonly BreakCoordinator _coordinator;
    private readonly IAudioPlayer _audioPlayer;
    private readonly IOptions<AppConfig> _appConfig;
    private readonly Random _random;

    private bool _isMutedForCurrentBreak;
    private int _lastPlayedOvertimeMinute = -1;

    public BreakVoiceAlertService(
        BreakCoordinator coordinator,
        IAudioPlayer audioPlayer,
        IOptions<AppConfig> appConfig,
        Random? random = null)
    {
        _coordinator = coordinator;
        _audioPlayer = audioPlayer;
        _appConfig = appConfig;
        _random = random ?? Random.Shared;

        _coordinator.BreakStarted += (_, _) => OnBreakStarted();
        _coordinator.ConfirmationEnabledChanged += (_, _) => OnConfirmationEnabledChanged();
        _coordinator.BreakCountdownChanged += (_, _) => OnBreakCountdownChanged();
        _coordinator.BreakEnded += (_, _) => OnBreakEnded();
        _audioPlayer.PlaybackFinished += (_, _) => PlaybackFinished?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? MuteStateChanged;

    public event EventHandler? PlaybackFinished;

    public bool IsMutedForCurrentBreak => _isMutedForCurrentBreak;

    public bool IsAudioPlaying => _audioPlayer.IsPlaying;

    /// <summary>
    /// Mutes voice alerts for the active break session and stops currently playing audio.
    /// Invoked by <c>BreakViewModel</c>.
    /// </summary>
    public void MuteCurrentBreak()
    {
        if (_isMutedForCurrentBreak)
        {
            return;
        }

        _isMutedForCurrentBreak = true;
        _audioPlayer.Stop();
        MuteStateChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Unmutes voice alerts for the active break session.
    /// Invoked by <c>BreakViewModel</c>.
    /// </summary>
    public void UnmuteCurrentBreak()
    {
        if (!_isMutedForCurrentBreak)
        {
            return;
        }

        _isMutedForCurrentBreak = false;
        MuteStateChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Toggles the temporary mute state for the active break session.
    /// Invoked by <c>BreakViewModel</c>.
    /// </summary>
    public void ToggleMuteCurrentBreak()
    {
        if (_isMutedForCurrentBreak)
        {
            UnmuteCurrentBreak();
            return;
        }

        MuteCurrentBreak();
    }

    /// <summary>
    /// Determines the voice prompt stage corresponding to the break duration and elapsed overtime.
    /// Invoked by <c>BreakVoiceAlertService</c> and test suites.
    /// </summary>
    public VoicePromptStage DetermineStage(int breakDurationSeconds, int overtimeSeconds)
    {
        if (breakDurationSeconds <= 0)
        {
            return VoicePromptStage.Initial;
        }

        if (overtimeSeconds >= 2 * breakDurationSeconds)
        {
            return VoicePromptStage.Overtime200Percent;
        }

        if (overtimeSeconds >= breakDurationSeconds)
        {
            return VoicePromptStage.Overtime100Percent;
        }

        return VoicePromptStage.Initial;
    }

    /// <summary>
    /// Plays a sample voice prompt directed to the specified audio output device or "all".
    /// Invoked by <c>SettingsViewModel</c>.
    /// </summary>
    public void PlayTestAlert(string? targetDeviceId = null)
    {
        string? audioPath = SelectRandomFile(InitialFolder);
        if (audioPath == null)
        {
            return;
        }

        PlayToTarget(audioPath, targetDeviceId);
    }

    /// <summary>
    /// Stops any currently playing voice alert or test alert.
    /// Invoked by <c>SettingsViewModel</c>.
    /// </summary>
    public void StopTestAlert() => _audioPlayer.Stop();

    private void OnBreakStarted()
    {
        _isMutedForCurrentBreak = false;
        _lastPlayedOvertimeMinute = -1;
        MuteStateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnConfirmationEnabledChanged()
    {
        if (!_coordinator.Scheduler.CanConfirmBreak)
        {
            return;
        }

        if (!_coordinator.CurrentConfig.AreVoiceNotificationsEnabled || _isMutedForCurrentBreak)
        {
            return;
        }

        PlayStageAudio(VoicePromptStage.Initial);
        _lastPlayedOvertimeMinute = 0;
    }

    private void OnBreakCountdownChanged()
    {
        if (!_coordinator.Scheduler.CanConfirmBreak)
        {
            return;
        }

        int overtime = _coordinator.Scheduler.OvertimeBreakSeconds;
        if (overtime <= 0)
        {
            return;
        }

        CheckAndPlayRecurringAlert(overtime);
    }

    private void CheckAndPlayRecurringAlert(int overtime)
    {
        if (overtime % SecondsPerMinute != 0)
        {
            return;
        }

        int currentMinute = overtime / SecondsPerMinute;
        if (currentMinute == _lastPlayedOvertimeMinute)
        {
            return;
        }

        _lastPlayedOvertimeMinute = currentMinute;

        if (!_coordinator.CurrentConfig.AreVoiceNotificationsEnabled || _isMutedForCurrentBreak)
        {
            return;
        }

        VoicePromptStage stage = DetermineStage(_coordinator.Scheduler.BreakDurationSeconds, overtime);
        PlayStageAudio(stage);
    }

    private void OnBreakEnded()
    {
        _audioPlayer.Stop();
        _lastPlayedOvertimeMinute = -1;
    }

    private void PlayStageAudio(VoicePromptStage stage)
    {
        string folderName = GetFolderForStage(stage);
        string? audioPath = SelectRandomFile(folderName);

        if (audioPath != null)
        {
            PlayToTarget(audioPath, _coordinator.CurrentConfig.AudioOutputDeviceId);
        }
    }

    private void PlayToTarget(string audioPath, string? targetDeviceId)
    {
        if (string.IsNullOrWhiteSpace(targetDeviceId) || string.Equals(targetDeviceId, AudioDevice.DefaultDeviceId, StringComparison.OrdinalIgnoreCase))
        {
            _audioPlayer.Play(audioPath);
            return;
        }

        _audioPlayer.Play(audioPath, targetDeviceId);
    }

    private static string GetFolderForStage(VoicePromptStage stage) => stage switch
    {
        VoicePromptStage.Initial => InitialFolder,
        VoicePromptStage.Overtime100Percent => Overtime100Folder,
        VoicePromptStage.Overtime200Percent => Overtime200Folder,
        _ => InitialFolder,
    };

    private string? SelectRandomFile(string folderName)
    {
        string? directory = ResolveDirectory(folderName);
        if (directory == null)
        {
            return null;
        }

        string[] files = Directory.GetFiles(directory, "*.mp3");
        if (files.Length == 0)
        {
            return null;
        }

        int index = _random.Next(files.Length);
        return files[index];
    }

    private string? ResolveDirectory(string folderName)
    {
        string configuredPath = Path.Combine(AppContext.BaseDirectory, _appConfig.Value.VoicePromptsDirectory, folderName);
        if (Directory.Exists(configuredPath))
        {
            return configuredPath;
        }

        string directPath = Path.Combine(AppContext.BaseDirectory, "Assets", "VoicePrompts", folderName);
        if (Directory.Exists(directPath))
        {
            return directPath;
        }

        string rootFallback = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Assets", "VoicePrompts", folderName);
        if (Directory.Exists(rootFallback))
        {
            return Path.GetFullPath(rootFallback);
        }

        string legacyName = GetLegacyFolderName(folderName);
        string repoFallback = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "komunikatyGłosowe", legacyName);
        if (Directory.Exists(repoFallback))
        {
            return Path.GetFullPath(repoFallback);
        }

        return null;
    }

    private static string GetLegacyFolderName(string folderName) => folderName switch
    {
        InitialFolder => "PierwszyKomunikat",
        Overtime100Folder => "drugiKomunikat",
        Overtime200Folder => "trzeci",
        _ => folderName,
    };
}
