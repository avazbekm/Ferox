namespace Forex.Wpf.Pages.Settings;

using Forex.Wpf.Common.Services;
using Forex.Wpf.Pages.Home;
using Forex.Wpf.Windows;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

public partial class SettingsPage : Page
{
    private static MainWindow Main => (MainWindow)Application.Current.MainWindow;

    public SettingsPage()
    {
        InitializeComponent();
        Loaded += Page_Loaded;
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        RegisterFocusNavigation();
        RegisterGlobalShortcuts();
    }

    private void RegisterFocusNavigation()
    {
        FocusNavigator.RegisterElements([
            tabControl
        ]);
    }

    private void RegisterGlobalShortcuts()
    {
        ShortcutAttacher.RegisterShortcut(btnBack, Key.Escape);

        ShortcutAttacher.RegisterShortcut(
            targetElement: this,
            key: Key.F1,
            targetAction: () => tabControl.SelectedIndex = 0,
            tooltipText: "Valyutalar sozlamalari (F1)"
        );

        ShortcutAttacher.RegisterShortcut(
            targetElement: this,
            key: Key.F2,
            targetAction: () => tabControl.SelectedIndex = 1,
            tooltipText: "O'lchov birliklari sozlamalari (F2)"
        );
    }

    private void BtnBack_Click(object sender, RoutedEventArgs e)
    {
        if (NavigationService?.CanGoBack == true)
            NavigationService.GoBack();
        else
            Main.NavigateTo(new HomePage());
    }
}
