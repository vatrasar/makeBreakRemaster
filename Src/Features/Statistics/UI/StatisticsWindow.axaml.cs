using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.ReactiveUI;
using makeBreak.Src.Features.Statistics.Domain.Enums;
using ReactiveUI;

namespace makeBreak.Src.Features.Statistics.UI;

/// <summary>
/// Statistics window.
/// Purpose: shows the accumulated work time or wasted time for the selected period
/// (last week, current month split into weeks, or last twelve months split by month)
/// and the period total, read from the SQLite-backed work time database.
/// Available Functionalities: toggling between work time and wasted time views, switching
/// between week, month, and year periods, viewing time bars and total time.
/// Key UI elements: top navigation tabs (work time vs wasted time), header with section title
/// and compact period segmented control, bar list with proportional progress bars, and summary total badge.
/// Navigate From: system tray menu (Statistics).
/// Navigate To: none.
/// </summary>
public partial class StatisticsWindow : ReactiveWindow<StatisticsViewModel>
{
    public StatisticsWindow(StatisticsViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = viewModel;

        this.WhenActivated(disposables =>
        {
            this.BindCommand(ViewModel, vm => vm.SelectWorkModeCommand, view => view.WorkModeButton);
            this.BindCommand(ViewModel, vm => vm.SelectWastedModeCommand, view => view.WastedModeButton);

            this.BindCommand(ViewModel, vm => vm.SelectWeekCommand, view => view.WeekPeriodButton);
            this.BindCommand(ViewModel, vm => vm.SelectMonthCommand, view => view.MonthPeriodButton);
            this.BindCommand(ViewModel, vm => vm.SelectYearCommand, view => view.YearPeriodButton);

            this.OneWayBind(ViewModel, vm => vm.State.SectionTitle, view => view.SectionTitleTextBlock.Text);
            this.OneWayBind(ViewModel, vm => vm.State.Bars, view => view.BarsList.ItemsSource);
            this.OneWayBind(ViewModel, vm => vm.State.TotalTimeLabel, view => view.TotalTimeTextBlock.Text);

            this.WhenAnyValue(x => x.ViewModel!.State.Mode)
                .Subscribe(Observer.Create<StatisticsMode>(mode =>
                {
                    WorkModeButton.IsChecked = mode == StatisticsMode.WorkTime;
                    WastedModeButton.IsChecked = mode == StatisticsMode.WastedTime;
                }))
                .DisposeWith(disposables);

            this.WhenAnyValue(x => x.ViewModel!.State.Period)
                .Subscribe(Observer.Create<StatisticsPeriod>(period =>
                {
                    WeekPeriodButton.IsChecked = period == StatisticsPeriod.Week;
                    MonthPeriodButton.IsChecked = period == StatisticsPeriod.Month;
                    YearPeriodButton.IsChecked = period == StatisticsPeriod.Year;
                }))
                .DisposeWith(disposables);
        });
    }
}
