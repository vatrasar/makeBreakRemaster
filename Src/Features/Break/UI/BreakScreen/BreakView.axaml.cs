using System.Reactive;
using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using Material.Icons;
using makeBreak.Src.Features.Break.Resources;
using ReactiveUI;

namespace makeBreak.Src.Features.Break.UI.BreakScreen;

/// <summary>
/// Fullscreen break screen showing the countdown, overtime counter, mute voice alerts icon button and the confirmation button.
/// Purpose: enforces the break, tracks break overtime, allows muting voice alerts for the session, and lets the user confirm its end.
/// Key UI elements: countdown number label, countdown caption, finished label, overtime number label, overtime caption, progress bar, mute voice alerts icon button, confirm button.
/// Navigate From: MainShell (routed when a break starts).
/// Navigate To: none (routed back to StartWork on confirm).
/// </summary>
public partial class BreakView : ReactiveUserControl<BreakViewModel>
{
    public BreakView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.OneWayBind(ViewModel, vm => vm.State.CountdownNumber, view => view.CountdownNumberTextBlock.Text);

            this.OneWayBind(ViewModel, vm => vm.State.CountdownProgress, view => view.CountdownProgressBar.Value);

            this.OneWayBind(ViewModel, vm => vm.State.IsCountdownVisible, view => view.CountdownNumberTextBlock.IsVisible);

            this.OneWayBind(ViewModel, vm => vm.State.IsCountdownVisible, view => view.CountdownCaptionTextBlock.IsVisible);

            this.OneWayBind(ViewModel, vm => vm.State.IsCountdownVisible, view => view.CountdownProgressBar.IsVisible);

            this.OneWayBind(ViewModel, vm => vm.State.IsFinishedVisible, view => view.FinishedTextBlock.IsVisible);

            this.OneWayBind(ViewModel, vm => vm.State.IsFinishedVisible, view => view.OvertimeNumberTextBlock.IsVisible);

            this.OneWayBind(ViewModel, vm => vm.State.IsFinishedVisible, view => view.OvertimeCaptionTextBlock.IsVisible);

            this.OneWayBind(ViewModel, vm => vm.State.OvertimeNumber, view => view.OvertimeNumberTextBlock.Text);

            this.OneWayBind(ViewModel, vm => vm.State.CanConfirm, view => view.ConfirmBreakButton.IsEnabled);

            this.OneWayBind(ViewModel, vm => vm.State.IsMuteButtonVisible, view => view.MuteVoiceAlertsButton.IsVisible);

            this.WhenAnyValue(x => x.ViewModel!.State.IsMuted)
                .Subscribe(Observer.Create<bool>(isMuted =>
                {
                    MuteVoiceAlertsIcon.Kind = isMuted ? MaterialIconKind.VolumeOff : MaterialIconKind.VolumeHigh;
                    ToolTip.SetTip(MuteVoiceAlertsButton, isMuted ? BreakStrings.UnmuteVoiceAlertsButton : BreakStrings.MuteVoiceAlertsButton);
                    MuteVoiceAlertsButton.Classes.Set("muted", isMuted);
                }))
                .DisposeWith(disposables);

            this.BindCommand(ViewModel, vm => vm.ConfirmBreakCommand, view => view.ConfirmBreakButton);

            this.BindCommand(ViewModel, vm => vm.ToggleMuteVoiceAlertsCommand, view => view.MuteVoiceAlertsButton);
        });
    }
}