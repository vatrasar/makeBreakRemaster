using Avalonia.ReactiveUI;
using ReactiveUI;

namespace makeBreak.Src.Features.Progress.UI;

/// <summary>
/// Progress dialog.
/// Purpose: shows live progress of the short and long work intervals.
/// Key UI elements: two progress bars.
/// Navigate From: system tray menu (Pokaż postęp).
/// Navigate To: none.
/// </summary>
public partial class ProgressWindow : ReactiveWindow<ProgressViewModel>
{
    public ProgressWindow(ProgressViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = viewModel;

        this.WhenActivated(disposables =>
        {
            this.OneWayBind(ViewModel, vm => vm.State.ShortProgressPercent, view => view.ShortProgressBar.Value);

            this.OneWayBind(ViewModel, vm => vm.State.LongProgressPercent, view => view.LongProgressBar.Value);
        });
    }
}