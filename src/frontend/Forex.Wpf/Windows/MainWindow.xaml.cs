namespace Forex.Wpf.Windows;

using Forex.Wpf.Common.Services;
using Forex.Wpf.Pages.Auth;
using Forex.Wpf.Pages.Home;
using System.Windows;
using System.Windows.Controls;

public partial class MainWindow : Window
{
    private readonly LoginViewModel _loginViewModel;

    public MainWindow(LoginViewModel loginViewModel)
    {
        _loginViewModel = loginViewModel;
        InitializeComponent();

        SpinnerService.Init(this);

        Loaded += OnMainWindowLoaded;
    }

    private async void OnMainWindowLoaded(object sender, RoutedEventArgs e)
    {
        var secureCreds = DevKeyService.TryGetSecureCredentials();

        if (secureCreds.HasValue)
        {
            var success = await _loginViewModel.LoginAsync(secureCreds.Value.login, secureCreds.Value.password);
            if (success)
            {
                NavigateTo(new HomePage());
                return;
            }
        }

        NavigateTo(new LoginPage());
    }


    public void NavigateTo(Page page)
    {
        System.Diagnostics.Debug.WriteLine($"Navigating to: {page.GetType().Name}");

        MainFrame.Navigate(page);
    }

    public void GoBack()
    {
        if (MainFrame.CanGoBack)
            MainFrame.GoBack();
    }
}