﻿namespace Forex.Wpf.Pages.Reports;
using Forex.Wpf.Pages.Home;
using Forex.Wpf.Pages.Reports.ViewModels;
using Forex.Wpf.Windows;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;

/// <summary>
/// Interaction logic for ReportsPage.xaml
/// </summary>
public partial class ReportsPage : Page
{
    private static MainWindow Main => (MainWindow)Application.Current.MainWindow;
    public ReportsPage()
    {
        InitializeComponent();
        DataContext = App.AppHost!.Services.GetRequiredService<ReportsPageViewModel>();
    }
    private void BtnBack_Click(object sender, RoutedEventArgs e)
    {
        if (NavigationService?.CanGoBack == true)
            NavigationService.GoBack();
        else
            Main.NavigateTo(new HomePage());
    }
}
