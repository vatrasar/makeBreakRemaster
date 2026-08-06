using Microsoft.Extensions.DependencyInjection;

namespace makeBreak.Src.Infrastructure.Navigation;

/// <summary>
/// Base contract for feature registration into the dependency injection container.
/// </summary>
public interface IFeatureModule
{
    void Register(IServiceCollection services);
}