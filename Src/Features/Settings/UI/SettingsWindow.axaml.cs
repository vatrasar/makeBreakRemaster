using Avalonia.ReactiveUI;
using makeBreak.Src.Core.Domain.Models;
using makeBreak.Src.Features.Settings.Resources;
using ReactiveUI;

namespace makeBreak.Src.Features.Settings.UI;

/// <summary>
/// Settings dialog.
/// Purpose: lets the user configure break schedule values, voice notifications, and the target audio output.
/// Key UI elements: four NumericUpDown inputs, VoiceNotificationsCheckBox, AudioOutputDeviceComboBox, TestAudioButton, OK/Cancel buttons.
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

        Closing += (_, _) => ViewModel?.StopTestAudio();

        this.WhenActivated(disposables =>
        {
            this.Bind(ViewModel, vm => vm.TimeToStartLongBreakMinutes, view => view.TimeToStartLongBreakInput.Value, ToDecimal, ToInt);

            this.Bind(ViewModel, vm => vm.TimeForLongBreakMinutes, view => view.TimeForLongBreakInput.Value, ToDecimal, ToInt);

            this.Bind(ViewModel, vm => vm.TimeToStartShortBreakMinutes, view => view.TimeToStartShortBreakInput.Value, ToDecimal, ToInt);

            this.Bind(ViewModel, vm => vm.TimeForShortBreakSeconds, view => view.TimeForShortBreakInput.Value, ToDecimal, ToInt);

            this.Bind(ViewModel, vm => vm.AreVoiceNotificationsEnabled, view => view.VoiceNotificationsCheckBox.IsChecked, ToNullableBool, FromNullableBool);

            this.OneWayBind(ViewModel, vm => vm.AvailableAudioDevices, view => view.AudioOutputDeviceComboBox.ItemsSource);

            this.Bind(ViewModel, vm => vm.SelectedAudioDevice, view => view.AudioOutputDeviceComboBox.SelectedItem, dev => dev, obj => obj as AudioDevice);

            this.OneWayBind(ViewModel, vm => vm.AreVoiceNotificationsEnabled, view => view.AudioOutputDeviceComboBox.IsEnabled);

            this.OneWayBind(ViewModel, vm => vm.AreVoiceNotificationsEnabled, view => view.TestAudioButton.IsEnabled);

            this.OneWayBind(ViewModel, vm => vm.IsTestingAudio, view => view.TestAudioButton.Content, isTesting => isTesting ? SettingsStrings.StopTestAudioButton : SettingsStrings.TestAudioButton);

            this.BindCommand(ViewModel, vm => vm.ToggleTestAudioCommand, view => view.TestAudioButton);

            this.BindCommand(ViewModel, vm => vm.SaveSettingsCommand, view => view.OkButton);

            this.BindCommand(ViewModel, vm => vm.CancelCommand, view => view.CancelButton);
        });
    }

    private static decimal? ToDecimal(int value) => value;

    private static int ToInt(decimal? value) => value is { } v ? (int)Math.Max(1, v) : 1;

    private static bool? ToNullableBool(bool value) => value;

    private static bool FromNullableBool(bool? value) => value ?? true;
}