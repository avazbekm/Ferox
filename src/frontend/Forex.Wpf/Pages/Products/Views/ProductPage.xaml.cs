namespace Forex.Wpf.Pages.Products;

using CommunityToolkit.Mvvm.Messaging;
using Forex.Wpf.Common.Interfaces;
using Forex.Wpf.Common.Messages;
using Forex.Wpf.Common.Services;
using Forex.Wpf.Pages.Home;
using Forex.Wpf.Pages.Products.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

public partial class ProductPage : Page
{
    private ProductPageViewModel vm;
    private INavigationService navigation;

    public ProductPage()
    {
        InitializeComponent();
        vm = App.AppHost!.Services.GetRequiredService<ProductPageViewModel>();
        navigation = App.AppHost!.Services.GetRequiredService<INavigationService>();
        DataContext = vm;

        WeakReferenceMessenger.Default.Register<FocusControlMessage>(this, (r, m) =>
        {
            OnFocusRequestReceived(m.ControlName);
        });

        Loaded += ProductPage_Loaded;
    }

    private void OnFocusRequestReceived(string controlName)
    {
        if (controlName == "ProductCode")
            FocusNavigator.FocusElement(productCombo.ComboBoxControl);
    }

    private void ProductPage_Loaded(object sender, RoutedEventArgs e)
    {
        this.ResizeWindow(1300, 700);
        RegisterFocusNavigation();
        RegisterGlobalShortcuts();
    }

    private void RegisterFocusNavigation()
    {
        FocusNavigator.RegisterElements([
            date.input,
            productCombo.input,
            tbxCode.input,
            tbxName.input,
            cbxProductionOrigin.combo,
            cbxProductType.combo,
            tbxBundle.input,
            tbxBundleItemCount.input,
            tbxQuantity.input,
            tbxCostPrice.input,
            fileInput.input,
            fileInput.button,
            btnAdd,
            btnCancel
        ]);
        FocusNavigator.SetFocusRedirect(btnAdd, productCombo.ComboBoxControl);
    }

    private void RegisterGlobalShortcuts()
    {
        ShortcutAttacher.RegisterShortcut(
            targetButton: btnBack,
            key: Key.Escape
        );

        ShortcutAttacher.RegisterShortcut(
            targetButton: btnAdd,
            key: Key.Enter,
            modifiers: ModifierKeys.Control
        );

        //ShortcutAttacher.RegisterShortcut(
        //    targetElement: this,
        //    key: Key.E,
        //    modifiers: ModifierKeys.Control,
        //    targetAction: () => _ = vm.Edit()
        //);
    }

    private void BtnBack_Click(object sender, RoutedEventArgs e)
    {
        if (NavigationService?.CanGoBack == true)
            NavigationService.GoBack();
        else
            navigation.NavigateTo(new HomePage());
    }

    private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        //_ = vm.Edit();
    }

    private void DataGrid_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (e.VerticalChange <= 0) return;

        var scrollViewer = e.OriginalSource as ScrollViewer;
        if (scrollViewer == null) return;

        double scrollPosition = scrollViewer.VerticalOffset;
        double scrollHeight = scrollViewer.ScrollableHeight;

        if (scrollHeight > 0 && scrollPosition >= scrollHeight * 0.9)
        {
            //_ = vm.LoadMoreProductEntriesCommand.ExecuteAsync(null);
        }
    }
}
