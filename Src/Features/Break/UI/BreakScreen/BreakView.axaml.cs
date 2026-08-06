using Avalonia.ReactiveUI;
using ReactiveUI;

namespace makeBreak.Src.Features.Break.UI.BreakScreen;

/// <summary>
/// Fullscreen break screen showing the countdown and the confirmation button.
/// Purpose: enforces the break and lets the user confirm its end.
/// Key UI elements: countdown label, finished label, confirm button.
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

            this.OneWayBind(ViewModel, vm => vm.State.CanConfirm, view => view.ConfirmBreakButton.IsEnabled);

            this.BindCommand(ViewModel, vm => vm.ConfirmBreakCommand, view => view.ConfirmBreakButton);
        });
    }
}