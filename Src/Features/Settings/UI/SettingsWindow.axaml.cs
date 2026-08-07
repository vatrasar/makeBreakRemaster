using Avalonia.ReactiveUI;
using ReactiveUI;

namespace makeBreak.Src.Features.Settings.UI;

/// <summary>
/// Settings dialog.
/// Purpose: lets the user configure the four break schedule values.
/// Key UI elements: four NumericUpDown inputs, OK/Cancel buttons.
/// Navigate From: system tray menu (Settings).
/// Navigate To: none.
/// </summary>
public partial class SettingsWindow : ReactiveWindow<SettingsViewModel>
{
    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = viewModel;

        this.WhenActivated(disposables =>
        {
            this.Bind(ViewModel, vm => vm.TimeToStartLongBreakMinutes, view => view.TimeToStartLongBreakInput.Value, ToDecimal, ToInt);

            this.Bind(ViewModel, vm => vm.TimeForLongBreakMinutes, view => view.TimeForLongBreakInput.Value, ToDecimal, ToInt);

            this.Bind(ViewModel, vm => vm.TimeToStartShortBreakMinutes, view => view.TimeToStartShortBreakInput.Value, ToDecimal, ToInt);

            this.Bind(ViewModel, vm => vm.TimeForShortBreakSeconds, view => view.TimeForShortBreakInput.Value, ToDecimal, ToInt);

            this.BindCommand(ViewModel, vm => vm.SaveSettingsCommand, view => view.OkButton);

            this.BindCommand(ViewModel, vm => vm.CancelCommand, view => view.CancelButton);
        });
    }

    private static decimal? ToDecimal(int value) => value;

    private static int ToInt(decimal? value) => value is { } v ? (int)Math.Max(1, v) : 1;
}