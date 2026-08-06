using Avalonia.ReactiveUI;
using ReactiveUI;

namespace makeBreak.Src.Features.Shell.UI.Host;

/// <summary>
/// The Host screen is the single routed view host of the application.
/// It renders the currently active routed screen (start work or break).
/// Purpose: hosts ReactiveUI routing for the application.
/// Key UI elements: RoutedViewHost.
/// Navigate From: MainWindow.
/// Navigate To: routed child screens.
/// </summary>
public partial class HostView : ReactiveUserControl<MainShellViewModel>
{
    public HostView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.OneWayBind(ViewModel, vm => vm.Router, view => view.HostRoutedViewHost.Router);
        });
    }
}