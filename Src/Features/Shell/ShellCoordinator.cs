using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using makeBreak.Src.Core.Domain.Enums;
using makeBreak.Src.Core.Domain.Services;
using makeBreak.Src.Features.Progress.UI;
using makeBreak.Src.Features.Settings.UI;
using makeBreak.Src.Features.Statistics.UI;
using Microsoft.Extensions.DependencyInjection;

namespace makeBreak.Src.Features.Shell;

/// <summary>
/// Coordinates window lifecycle and tray actions with the break schedule.
/// Shows the main window (with start-work screen) on startup, hides it while
/// working, shows it fullscreen during a break, and hides it again after the
/// break is confirmed. Also opens the settings, progress and statistics dialogs.
/// </summary>
public sealed class ShellCoordinator
{
    private readonly IServiceProvider _services;
    private readonly BreakCoordinator _coordinator;
    private readonly MainWindow _mainWindow;

    private bool _isExplicitShutdown;

    public ShellCoordinator(IServiceProvider services, BreakCoordinator coordinator, MainWindow mainWindow)
    {
        _services = services;
        _coordinator = coordinator;
        _mainWindow = mainWindow;

        _coordinator.StateChanged += (_, _) => HandleStateChanged(_coordinator.Scheduler.State);
        _coordinator.BreakStarted += (_, _) => ShowMainWindowFullScreen();
        _coordinator.BreakEnded += (_, _) => HideMainWindow();

        _mainWindow.Closing += HandleMainWindowClosing;
    }

    public void ShowMainWindow() => _mainWindow.Show();

    public void HideMainWindow()
    {
        _mainWindow.Topmost = false;
        _mainWindow.WindowState = WindowState.Normal;
        _mainWindow.Hide();
    }

    public void ShowSettings()
    {
        SettingsWindow window = _services.GetRequiredService<SettingsWindow>();
        window.ViewModel.Saved += (_, _) => OnSettingsSaved(window);
        window.ViewModel.Cancelled += (_, _) => OnSettingsCancelled(window);
        window.Show();
    }

    public void ShowProgress() => _services.GetRequiredService<ProgressWindow>().Show();

    public void ShowStatistics() => _services.GetRequiredService<StatisticsWindow>().Show();

    public void StartWork() => _coordinator.StartWork();

    public void Stop() => _coordinator.StopSchedule();

    public void Resume() => _coordinator.ResumeSchedule();

    public void Shutdown()
    {
        _isExplicitShutdown = true;
        _coordinator.StopSchedule();

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            lifetime.Shutdown();
        }
        else
        {
            Environment.Exit(0);
        }
    }

    private void OnSettingsSaved(SettingsWindow window)
    {
        _coordinator.StopSchedule();
        window.Close();

        if (_mainWindow.IsVisible)
        {
            _mainWindow.Hide();
        }
        else
        {
            _mainWindow.Show();
        }
    }

    private void OnSettingsCancelled(SettingsWindow window)
    {
        window.Close();
        _mainWindow.Hide();
    }

    private void HandleStateChanged(SessionState state)
    {
        if (state == SessionState.Working)
        {
            _mainWindow.Hide();
        }
    }

    private void HandleMainWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_isExplicitShutdown)
        {
            return;
        }

        e.Cancel = true;

        if (_coordinator.Scheduler.State is SessionState.OnShortBreak or SessionState.OnLongBreak)
        {
            return;
        }

        _mainWindow.Hide();
    }

    private void ShowMainWindowFullScreen()
    {
        _mainWindow.Topmost = true;
        _mainWindow.WindowState = WindowState.FullScreen;
        _mainWindow.Show();
    }
}