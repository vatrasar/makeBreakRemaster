using Avalonia.ReactiveUI;
using ReactiveUI;

namespace makeBreak.Src.Features.Work.UI.StartWork;

/// <summary>
/// The start-work screen.
/// Purpose: shows the app icon and the button that starts the work session.
/// Key UI elements: app icon, start-work button.
/// Navigate From: MainShell (initial routed screen, and after a break ends).
/// Navigate To: Break screen when a break starts during the work session.
/// </summary>
public partial class StartWorkView : ReactiveUserControl<StartWorkViewModel>
{
    public StartWorkView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.BindCommand(ViewModel, vm => vm.StartWorkCommand, view => view.StartWorkButton);
        });
    }
}