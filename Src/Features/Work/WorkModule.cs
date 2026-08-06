using makeBreak.Src.Features.Work.UI.StartWork;
using makeBreak.Src.Infrastructure.Navigation;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;

namespace makeBreak.Src.Features.Work;

public class WorkModule : IFeatureModule
{
    public void Register(IServiceCollection services)
    {
        services.AddTransient<IViewFor<StartWorkViewModel>, StartWorkView>();
    }
}