using makeBreak.Src.Core.Domain.Models;
using makeBreak.Src.Infrastructure.Audio;
using Xunit;

namespace makeBreak.Tests.CoreTests;

public class SystemAudioDeviceServiceTests
{
    [Fact]
    public void GetOutputDevices_ReturnsNonNullCollection()
    {
        var service = new SystemAudioDeviceService();

        IReadOnlyList<AudioDevice> devices = service.GetOutputDevices();

        Assert.NotNull(devices);
        foreach (AudioDevice device in devices)
        {
            Assert.False(string.IsNullOrWhiteSpace(device.Id));
            Assert.False(string.IsNullOrWhiteSpace(device.Name));
        }
    }
}
