using makeBreak.Src.Features.Progress.UI;
using makeBreak.Src.Infrastructure.Navigation;
using Microsoft.Extensions.DependencyInjection;

namespace makeBreak.Src.Features.Progress;

public class ProgressModule : IFeatureModule
{
    public void Register(IServiceCollection services)
    {
        services.AddTransient<ProgressViewModel>();
        services.AddTransient<ProgressWindow>();
    }
}