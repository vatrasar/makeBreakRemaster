using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using makeBreak.Src.Features.Shell;
using makeBreak.Src.Features.Shell.Resources;
using makeBreak.Src.Infrastructure.Data;
using makeBreak.Src.Infrastructure.DependencyInjection;
using makeBreak.Src.Infrastructure.Navigation;
using makeBreak.Src.Core.Domain.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;

namespace makeBreak;

public partial class App : Application
{
    private ShellCoordinator? _coordinator;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            IServiceProvider services = BuildServiceProvider();

            services.GetRequiredService<DatabaseInitializer>().Initialize();
            services.GetRequiredService<WorkTimeService>().Cleanup();

            _coordinator = services.GetRequiredService<ShellCoordinator>();

            MainWindow mainWindow = services.GetRequiredService<MainWindow>();
            desktop.MainWindow = mainWindow;

            makeBreak.Src.Infrastructure.Navigation.ViewLocator viewLocator = services.GetRequiredService<makeBreak.Src.Infrastructure.Navigation.ViewLocator>();
            Splat.Locator.CurrentMutable.Register(() => viewLocator, typeof(IViewLocator));

            BreakTicker ticker = services.GetRequiredService<BreakTicker>();
            ticker.Start();

            AttachTrayMenu(services);

            _coordinator.ShowMainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void AttachTrayMenu(IServiceProvider services)
    {
        TrayState trayState = services.GetRequiredService<TrayState>();

        if (GetValue(TrayIcon.IconsProperty) is { Count: > 0 } icons && icons[0].Menu is { } menu)
        {
            trayState.Attach(FindMenuItem(menu, ShellStrings.TrayStop)!, FindMenuItem(menu, ShellStrings.TrayResume)!);
        }
    }

    private static NativeMenuItem? FindMenuItem(NativeMenu menu, string header) =>
        menu.Items.OfType<NativeMenuItem>().FirstOrDefault(item => item.Header == header);

    private void TraySettings_OnClick(object? sender, EventArgs e) => _coordinator?.ShowSettings();

    private void TrayShowProgress_OnClick(object? sender, EventArgs e) => _coordinator?.ShowProgress();

    private void TrayShowStatistics_OnClick(object? sender, EventArgs e) => _coordinator?.ShowStatistics();

    private void TrayStop_OnClick(object? sender, EventArgs e) => _coordinator?.Stop();

    private void TrayResume_OnClick(object? sender, EventArgs e) => _coordinator?.Resume();

    private void TrayExit_OnClick(object? sender, EventArgs e) => _coordinator?.Shutdown();

    private static IServiceProvider BuildServiceProvider()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(System.AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .Build();

        string configDirectory = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "makeBreak");
        System.IO.Directory.CreateDirectory(configDirectory);

        string configFilePath = System.IO.Path.Combine(configDirectory, configuration["AppConfig:ConfigFileName"] ?? "conf.txt");

        string databaseFilePath = System.IO.Path.Combine(configDirectory, configuration["AppConfig:WorkTimeDatabaseFileName"] ?? "worktime.db");

        return AppBootstrapper.BuildServiceProvider(services => services.AddApplicationServices(configuration, configFilePath, databaseFilePath));
    }
}