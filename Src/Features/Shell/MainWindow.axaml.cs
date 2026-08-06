using Avalonia.ReactiveUI;
using makeBreak.Src.Features.Shell.UI.Host;

namespace makeBreak.Src.Features.Shell;

/// <summary>
/// The application's single host window. It displays the Host screen
/// which contains the routed view host.
/// </summary>
public partial class MainWindow : ReactiveWindow<MainShellViewModel>
{
    public MainWindow(MainShellViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = viewModel;
    }
}