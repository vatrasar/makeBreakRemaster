using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using makeBreak.Src.Core.Config;
using makeBreak.Src.Core.Domain.Interfaces;
using makeBreak.Src.Core.Domain.RepositoryContracts;
using makeBreak.Src.Core.Domain.Services;
using makeBreak.Src.Features.Shell;
using makeBreak.Src.Features.Shell.UI.Host;
using makeBreak.Src.Infrastructure.Data.Repositories;
using makeBreak.Src.Infrastructure.Navigation;

namespace makeBreak.Src.Infrastructure.DependencyInjection;

/// <summary>
/// Registers the application-wide (core + infrastructure + shell) services into the DI container.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration, string configFilePath)
    {
        services.Configure<AppConfig>(configuration.GetSection(AppConfig.SectionName));

        services.AddSingleton<IConfigRepository>(_ => new ConfigFileRepository(configFilePath));
        services.AddSingleton<ConfigService>();
        services.AddSingleton<IBreakScheduler, BreakScheduler>();
        services.AddSingleton<BreakCoordinator>();

        services.AddSingleton<BreakTicker>();
        services.AddSingleton<MainShellViewModel>();
        services.AddSingleton<ShellCoordinator>();
        services.AddSingleton<TrayState>();
        services.AddSingleton<ViewLocator>();
        services.AddSingleton<MainWindow>();

        return services;
    }
}