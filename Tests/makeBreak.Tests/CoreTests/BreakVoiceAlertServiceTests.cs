using makeBreak.Src.Core.Config;
using makeBreak.Src.Core.Domain.Enums;
using makeBreak.Src.Core.Domain.Interfaces;
using makeBreak.Src.Core.Domain.Models;
using makeBreak.Src.Core.Domain.RepositoryContracts;
using makeBreak.Src.Core.Domain.Services;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace makeBreak.Tests.CoreTests;

public class BreakVoiceAlertServiceTests
{
    private static BreakCoordinator CreateCoordinator(BreakConfig config)
    {
        var repository = new Mock<IConfigRepository>();
        repository.Setup(r => r.Load()).Returns((BreakConfig?)null);
        var configService = new ConfigService(Options.Create(new AppConfig()), repository.Object);
        var scheduler = new BreakScheduler();
        var coordinator = new BreakCoordinator(scheduler, configService);
        var safeConfig = config with
        {
            TimeToStartLongBreak = config.TimeToStartLongBreak > 0 ? config.TimeToStartLongBreak : 9999,
            TimeForLongBreak = config.TimeForLongBreak > 0 ? config.TimeForLongBreak : 9999,
        };
        coordinator.SaveSettings(safeConfig);
        return coordinator;
    }

    [Theory]
    [InlineData(120, 0, VoicePromptStage.Initial)]
    [InlineData(120, 60, VoicePromptStage.Initial)]
    [InlineData(120, 119, VoicePromptStage.Initial)]
    [InlineData(120, 120, VoicePromptStage.Overtime100Percent)]
    [InlineData(120, 180, VoicePromptStage.Overtime100Percent)]
    [InlineData(120, 239, VoicePromptStage.Overtime100Percent)]
    [InlineData(120, 240, VoicePromptStage.Overtime200Percent)]
    [InlineData(120, 360, VoicePromptStage.Overtime200Percent)]
    [InlineData(0, 10, VoicePromptStage.Initial)]
    public void DetermineStage_ReturnsExpectedStage(int breakDuration, int overtime, VoicePromptStage expectedStage)
    {
        var coordinator = CreateCoordinator(new BreakConfig());
        var player = new Mock<IAudioPlayer>();
        var service = new BreakVoiceAlertService(coordinator, player.Object, Options.Create(new AppConfig()));

        VoicePromptStage result = service.DetermineStage(breakDuration, overtime);

        Assert.Equal(expectedStage, result);
    }

    [Fact]
    public void ConfirmationEnabled_WhenVoiceEnabled_PlaysInitialAudio()
    {
        var config = new BreakConfig
        {
            TimeToStartShortBreak = 2,
            TimeForShortBreak = 2,
            AreVoiceNotificationsEnabled = true,
        };
        BreakCoordinator coordinator = CreateCoordinator(config);
        var player = new Mock<IAudioPlayer>();
        string? playedPath = null;
        player.Setup(p => p.Play(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<int>()))
              .Callback<string, string?, int>((path, _, _) => playedPath = path);

        var service = new BreakVoiceAlertService(coordinator, player.Object, Options.Create(new AppConfig()));

        coordinator.StartWork();
        for (int i = 0; i < 4; i++)
        {
            coordinator.Tick();
        }

        Assert.NotNull(playedPath);
        Assert.Contains("FirstPrompt", playedPath);
    }

    [Fact]
    public void ConfirmationEnabled_WhenVoiceDisabledInConfig_DoesNotPlayAudio()
    {
        var config = new BreakConfig
        {
            TimeToStartShortBreak = 2,
            TimeForShortBreak = 2,
            AreVoiceNotificationsEnabled = false,
        };
        BreakCoordinator coordinator = CreateCoordinator(config);
        var player = new Mock<IAudioPlayer>();
        var service = new BreakVoiceAlertService(coordinator, player.Object, Options.Create(new AppConfig()));

        coordinator.StartWork();
        for (int i = 0; i < 4; i++)
        {
            coordinator.Tick();
        }

        player.Verify(p => p.Play(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public void MuteCurrentBreak_StopsAudio_AndSuppressesFutureIntervals()
    {
        var config = new BreakConfig
        {
            TimeToStartShortBreak = 2,
            TimeForShortBreak = 2,
            AreVoiceNotificationsEnabled = true,
        };
        BreakCoordinator coordinator = CreateCoordinator(config);
        var player = new Mock<IAudioPlayer>();
        var service = new BreakVoiceAlertService(coordinator, player.Object, Options.Create(new AppConfig()));

        coordinator.StartWork();
        for (int i = 0; i < 4; i++)
        {
            coordinator.Tick();
        }

        player.Verify(p => p.Play(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<int>()), Times.Once);

        service.MuteCurrentBreak();

        player.Verify(p => p.Stop(), Times.Once);
        Assert.True(service.IsMutedForCurrentBreak);

        for (int i = 0; i < 60; i++)
        {
            coordinator.Tick();
        }

        player.Verify(p => p.Play(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public void OvertimeTicks_EscalateStageAndRepeatEveryMinute()
    {
        var config = new BreakConfig
        {
            TimeToStartLongBreak = 9999,
            TimeForLongBreak = 9999,
            TimeToStartShortBreak = 2,
            TimeForShortBreak = 60,
            AreVoiceNotificationsEnabled = true,
        };
        BreakCoordinator coordinator = CreateCoordinator(config);
        var player = new Mock<IAudioPlayer>();
        List<string> playedPaths = new();
        player.Setup(p => p.Play(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<int>()))
              .Callback<string, string?, int>((path, _, _) => playedPaths.Add(path));

        var service = new BreakVoiceAlertService(coordinator, player.Object, Options.Create(new AppConfig()));

        coordinator.StartWork();
        for (int i = 0; i < 62; i++)
        {
            coordinator.Tick();
        }

        Assert.Single(playedPaths);
        Assert.Contains("FirstPrompt", playedPaths[0]);

        for (int i = 0; i < 60; i++)
        {
            coordinator.Tick();
        }

        Assert.Equal(2, playedPaths.Count);
        Assert.Contains("SecondPrompt", playedPaths[1]);

        for (int i = 0; i < 60; i++)
        {
            coordinator.Tick();
        }

        Assert.Equal(3, playedPaths.Count);
        Assert.Contains("ThirdPrompt", playedPaths[2]);
    }

    [Fact]
    public void NextBreak_ResetsMuteState()
    {
        var config = new BreakConfig
        {
            TimeToStartShortBreak = 2,
            TimeForShortBreak = 2,
            AreVoiceNotificationsEnabled = true,
        };
        BreakCoordinator coordinator = CreateCoordinator(config);
        var player = new Mock<IAudioPlayer>();
        var service = new BreakVoiceAlertService(coordinator, player.Object, Options.Create(new AppConfig()));

        coordinator.StartWork();
        for (int i = 0; i < 4; i++)
        {
            coordinator.Tick();
        }

        service.MuteCurrentBreak();
        Assert.True(service.IsMutedForCurrentBreak);

        coordinator.ConfirmBreak();
        for (int i = 0; i < 2; i++)
        {
            coordinator.Tick();
        }

        Assert.False(service.IsMutedForCurrentBreak);
    }

    [Fact]
    public void MuteCurrentBreak_DuringCountdown_SuppressesInitialAndSubsequentAudio()
    {
        var config = new BreakConfig
        {
            TimeToStartShortBreak = 2,
            TimeForShortBreak = 5,
            AreVoiceNotificationsEnabled = true,
        };
        BreakCoordinator coordinator = CreateCoordinator(config);
        var player = new Mock<IAudioPlayer>();
        var service = new BreakVoiceAlertService(coordinator, player.Object, Options.Create(new AppConfig()));

        coordinator.StartWork();
        for (int i = 0; i < 2; i++)
        {
            coordinator.Tick();
        }

        coordinator.Tick();

        service.MuteCurrentBreak();
        Assert.True(service.IsMutedForCurrentBreak);

        for (int i = 0; i < 70; i++)
        {
            coordinator.Tick();
        }

        player.Verify(p => p.Play(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public void ConfirmationEnabled_WhenCustomAudioDeviceConfigured_PlaysAudioToTargetDevice()
    {
        var config = new BreakConfig
        {
            TimeToStartShortBreak = 2,
            TimeForShortBreak = 2,
            AreVoiceNotificationsEnabled = true,
            AudioOutputDeviceId = "alsa_output.headphones",
        };
        BreakCoordinator coordinator = CreateCoordinator(config);
        var player = new Mock<IAudioPlayer>();
        string? targetDevice = null;
        player.Setup(p => p.Play(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<int>()))
              .Callback<string, string?, int>((_, device, _) => targetDevice = device);

        var service = new BreakVoiceAlertService(coordinator, player.Object, Options.Create(new AppConfig()));

        coordinator.StartWork();
        for (int i = 0; i < 4; i++)
        {
            coordinator.Tick();
        }

        Assert.Equal("alsa_output.headphones", targetDevice);
    }

    [Fact]
    public void ConfirmationEnabled_WhenCustomVoiceVolumeConfigured_PlaysAudioWithConfiguredVolume()
    {
        var config = new BreakConfig
        {
            TimeToStartShortBreak = 2,
            TimeForShortBreak = 2,
            AreVoiceNotificationsEnabled = true,
            VoiceVolumePercent = 45,
        };
        BreakCoordinator coordinator = CreateCoordinator(config);
        var player = new Mock<IAudioPlayer>();
        int playedVolume = -1;
        player.Setup(p => p.Play(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<int>()))
              .Callback<string, string?, int>((_, _, vol) => playedVolume = vol);

        var service = new BreakVoiceAlertService(coordinator, player.Object, Options.Create(new AppConfig()));

        coordinator.StartWork();
        for (int i = 0; i < 4; i++)
        {
            coordinator.Tick();
        }

        Assert.Equal(45, playedVolume);
    }

    [Fact]
    public void PlayTestAlert_PlaysAudioToSpecifiedTargetDevice()
    {
        var coordinator = CreateCoordinator(new BreakConfig());
        var player = new Mock<IAudioPlayer>();
        string? targetDevice = null;
        player.Setup(p => p.Play(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<int>()))
              .Callback<string, string?, int>((_, device, _) => targetDevice = device);

        var service = new BreakVoiceAlertService(coordinator, player.Object, Options.Create(new AppConfig()));

        service.PlayTestAlert("all");

        Assert.Equal("all", targetDevice);
    }

    [Fact]
    public void PlayTestAlert_WhenCustomVolumeSpecified_PlaysAudioWithSpecifiedVolume()
    {
        var coordinator = CreateCoordinator(new BreakConfig { VoiceVolumePercent = 30 });
        var player = new Mock<IAudioPlayer>();
        int playedVolume = -1;
        player.Setup(p => p.Play(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<int>()))
              .Callback<string, string?, int>((_, _, vol) => playedVolume = vol);

        var service = new BreakVoiceAlertService(coordinator, player.Object, Options.Create(new AppConfig()));

        service.PlayTestAlert("default", 75);

        Assert.Equal(75, playedVolume);
    }

    [Fact]
    public void StopTestAlert_InvokesAudioPlayerStop()
    {
        var coordinator = CreateCoordinator(new BreakConfig());
        var player = new Mock<IAudioPlayer>();
        var service = new BreakVoiceAlertService(coordinator, player.Object, Options.Create(new AppConfig()));

        service.StopTestAlert();

        player.Verify(p => p.Stop(), Times.Once);
    }
}
