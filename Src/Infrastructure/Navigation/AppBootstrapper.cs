using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace makeBreak.Src.Infrastructure.Navigation;

/// <summary>
/// Builds the dependency injection container and registers all feature modules
/// discovered via reflection.
/// </summary>
public static class AppBootstrapper
{
    public static IServiceProvider BuildServiceProvider(Action<IServiceCollection> configure)
    {
        ServiceCollection services = new();

        configure(services);

        List<IFeatureModule> modules = DiscoverModules();
        foreach (IFeatureModule module in modules)
        {
            module.Register(services);
        }

        return services.BuildServiceProvider();
    }

    private static List<IFeatureModule> DiscoverModules()
    {
        IEnumerable<Type> moduleTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(type => !type.IsAbstract && typeof(IFeatureModule).IsAssignableFrom(type));

        return moduleTypes.Select(Activator.CreateInstance).Cast<IFeatureModule>().ToList();
    }
}