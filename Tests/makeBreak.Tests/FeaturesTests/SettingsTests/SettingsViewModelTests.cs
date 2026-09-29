using makeBreak.Src.Core.Config;
using makeBreak.Src.Core.Domain.Interfaces;
using makeBreak.Src.Core.Domain.Models;
using makeBreak.Src.Core.Domain.RepositoryContracts;
using makeBreak.Src.Core.Domain.Services;
using makeBreak.Src.Features.Settings.UI;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace makeBreak.Tests.FeaturesTests.SettingsTests;

public class SettingsViewModelTests
{
    private static BreakCoordinator CreateCoordinator(BreakConfig config)
    {
        var mockRepo = new Mock<IConfigRepository>();
        mockRepo.Setup(r => r.Load()).Returns(config);
        var configService = new ConfigService(Options.Create(new AppConfig()), mockRepo.Object);
        var scheduler = new BreakScheduler();
        return new BreakCoordinator(scheduler, configService);
    }

    [Fact]
    public void Constructor_PopulatesAvailableAudioDevices_WithDefaultAllAndDetected()
    {
        var coordinator = CreateCoordinator(new BreakConfig());
        var voiceAlertMock = new Mock<IBreakVoiceAlertService>();
        var audioDeviceMock = new Mock<IAudioDeviceService>();
        audioDeviceMock.Setup(a => a.GetOutputDevices()).Returns(new List<AudioDevice>
        {
            new() { Id = "alsa_output.speakers", Name = "Built-in Speakers" },
            new() { Id = "alsa_output.headphones", Name = "Headphones" },
        });

        var vm = new SettingsViewModel(coordinator, voiceAlertMock.Object, audioDeviceMock.Object);

        Assert.Equal(4, vm.AvailableAudioDevices.Count);
        Assert.Equal(AudioDevice.DefaultDeviceId, vm.AvailableAudioDevices[0].Id);
        Assert.Equal(AudioDevice.AllDevicesId, vm.AvailableAudioDevices[1].Id);
        Assert.Equal("alsa_output.speakers", vm.AvailableAudioDevices[2].Id);
        Assert.Equal("alsa_output.headphones", vm.AvailableAudioDevices[3].Id);
        Assert.Equal(AudioDevice.DefaultDeviceId, vm.SelectedAudioDevice?.Id);
    }

    [Fact]
    public void Constructor_SelectsConfiguredAudioDevice()
    {
        var config = new BreakConfig
        {
            AudioOutputDeviceId = "alsa_output.headphones",
        };
        var coordinator = CreateCoordinator(config);
        var voiceAlertMock = new Mock<IBreakVoiceAlertService>();
        var audioDeviceMock = new Mock<IAudioDeviceService>();
        audioDeviceMock.Setup(a => a.GetOutputDevices()).Returns(new List<AudioDevice>
        {
            new() { Id = "alsa_output.speakers", Name = "Built-in Speakers" },
            new() { Id = "alsa_output.headphones", Name = "Headphones" },
        });

        var vm = new SettingsViewModel(coordinator, voiceAlertMock.Object, audioDeviceMock.Object);

        Assert.NotNull(vm.SelectedAudioDevice);
        Assert.Equal("alsa_output.headphones", vm.SelectedAudioDevice!.Id);
    }

    [Fact]
    public void SaveSettings_PersistsSelectedAudioDevice()
    {
        var mockRepo = new Mock<IConfigRepository>();
        mockRepo.Setup(r => r.Load()).Returns(new BreakConfig());
        var configService = new ConfigService(Options.Create(new AppConfig()), mockRepo.Object);
        var scheduler = new BreakScheduler();
        var coordinator = new BreakCoordinator(scheduler, configService);

        var voiceAlertMock = new Mock<IBreakVoiceAlertService>();
        var audioDeviceMock = new Mock<IAudioDeviceService>();
        audioDeviceMock.Setup(a => a.GetOutputDevices()).Returns(new List<AudioDevice>
        {
            new() { Id = "alsa_output.speakers", Name = "Built-in Speakers" },
        });

        var vm = new SettingsViewModel(coordinator, voiceAlertMock.Object, audioDeviceMock.Object);
        vm.SelectedAudioDevice = vm.AvailableAudioDevices[1]; // All audio outputs

        bool savedFired = false;
        vm.Saved += (_, _) => savedFired = true;

        vm.SaveSettingsCommand.Execute().Subscribe();

        Assert.True(savedFired);
        Assert.Equal(AudioDevice.AllDevicesId, coordinator.CurrentConfig.AudioOutputDeviceId);
        mockRepo.Verify(r => r.Save(It.Is<BreakConfig>(c => c.AudioOutputDeviceId == AudioDevice.AllDevicesId)), Times.Once);
    }

    [Fact]
    public void ToggleTestAudio_PlaysAndStopsTestAudio()
    {
        var coordinator = CreateCoordinator(new BreakConfig());
        var voiceAlertMock = new Mock<IBreakVoiceAlertService>();
        var audioDeviceMock = new Mock<IAudioDeviceService>();
        audioDeviceMock.Setup(a => a.GetOutputDevices()).Returns(new List<AudioDevice>
        {
            new() { Id = "alsa_output.speakers", Name = "Built-in Speakers" },
        });

        var vm = new SettingsViewModel(coordinator, voiceAlertMock.Object, audioDeviceMock.Object);
        vm.SelectedAudioDevice = vm.AvailableAudioDevices[2]; // Speakers

        vm.ToggleTestAudioCommand.Execute().Subscribe();

        voiceAlertMock.Verify(v => v.PlayTestAlert("alsa_output.speakers"), Times.Once);
        Assert.True(vm.IsTestingAudio);

        vm.ToggleTestAudioCommand.Execute().Subscribe();

        voiceAlertMock.Verify(v => v.StopTestAlert(), Times.Once);
        Assert.False(vm.IsTestingAudio);
    }

    [Fact]
    public void Cancel_StopsTestAudio_AndFiresCancelled()
    {
        var coordinator = CreateCoordinator(new BreakConfig());
        var voiceAlertMock = new Mock<IBreakVoiceAlertService>();
        var audioDeviceMock = new Mock<IAudioDeviceService>();

        var vm = new SettingsViewModel(coordinator, voiceAlertMock.Object, audioDeviceMock.Object);

        bool cancelledFired = false;
        vm.Cancelled += (_, _) => cancelledFired = true;

        vm.ToggleTestAudioCommand.Execute().Subscribe();
        Assert.True(vm.IsTestingAudio);

        vm.CancelCommand.Execute().Subscribe();

        Assert.True(cancelledFired);
        Assert.False(vm.IsTestingAudio);
        voiceAlertMock.Verify(v => v.StopTestAlert(), Times.Once);
    }
}
