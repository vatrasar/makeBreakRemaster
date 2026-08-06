using makeBreak.Src.Features.Break.UI.BreakScreen;
using makeBreak.Src.Infrastructure.Navigation;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;

namespace makeBreak.Src.Features.Break;

public class BreakModule : IFeatureModule
{
    public void Register(IServiceCollection services)
    {
        services.AddTransient<IViewFor<BreakViewModel>, BreakView>();
    }
}