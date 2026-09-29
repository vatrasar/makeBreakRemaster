using makeBreak.Src.Core.Domain.Interfaces;
using makeBreak.Src.Infrastructure.Audio;
using Moq;
using Xunit;

namespace makeBreak.Tests.CoreTests;

public class SystemAudioPlayerTests
{
    [Fact]
    public void Stop_WhenNotPlaying_DoesNotThrow()
    {
        var deviceService = new Mock<IAudioDeviceService>();
        using var player = new SystemAudioPlayer(deviceService.Object);

        var exception = Record.Exception(() => player.Stop());

        Assert.Null(exception);
        Assert.False(player.IsPlaying);
    }

    [Fact]
    public void Play_WhenFileDoesNotExist_DoesNotThrow()
    {
        var deviceService = new Mock<IAudioDeviceService>();
        using var player = new SystemAudioPlayer(deviceService.Object);

        var exception = Record.Exception(() => player.Play("/non/existent/path.mp3", null, 50));

        Assert.Null(exception);
        Assert.False(player.IsPlaying);
    }
}
