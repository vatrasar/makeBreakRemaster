using makeBreak.Src.Features.Settings.UI;
using makeBreak.Src.Infrastructure.Navigation;
using Microsoft.Extensions.DependencyInjection;

namespace makeBreak.Src.Features.Settings;

public class SettingsModule : IFeatureModule
{
    public void Register(IServiceCollection services)
    {
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<SettingsWindow>();
    }
}