using makeBreak.Src.Features.Statistics.UI;
using makeBreak.Src.Infrastructure.Navigation;
using Microsoft.Extensions.DependencyInjection;

namespace makeBreak.Src.Features.Statistics;

public class StatisticsModule : IFeatureModule
{
    public void Register(IServiceCollection services)
    {
        services.AddTransient<StatisticsViewModel>();
        services.AddTransient<StatisticsWindow>();
    }
}