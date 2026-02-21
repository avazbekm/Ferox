namespace Forex.Wpf.Pages.Processes;

using Forex.Wpf.Common.Services;
using Forex.Wpf.Pages.Home;
using Forex.Wpf.Pages.Processes.ViewModels;
using Forex.Wpf.Windows;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;


/// <summary>
/// Interaction logic for ProcessPage.xaml
/// </summary>
public partial class ProcessPage : Page
{
    private static MainWindow Main => (MainWindow)Application.Current.MainWindow;
    private ProcessPageViewModel vm;

    public ProcessPage()
    {
        InitializeComponent();
        vm = App.AppHost!.Services.GetRequiredService<ProcessPageViewModel>();
        DataContext = vm;

        Loaded += ProcessPage_Loaded;
    }

    private void ProcessPage_Loaded(object sender, RoutedEventArgs e)
    {
        RegisterFocusNavigation();
        RegisterGlobalShortcuts();
    }

    private void RegisterFocusNavigation()
    {
        //FocusNavigator.RegisterElements([
        //    tbcQuantity
        //]);
    }

    private void RegisterGlobalShortcuts()
    {
        ShortcutAttacher.RegisterShortcut(btnBack, Key.Escape);
    }

    private void BtnBack_Click(object sender, RoutedEventArgs e)
    {
        if (NavigationService?.CanGoBack == true)
            NavigationService.GoBack();
        else
            Main.NavigateTo(new HomePage());
    }
}
