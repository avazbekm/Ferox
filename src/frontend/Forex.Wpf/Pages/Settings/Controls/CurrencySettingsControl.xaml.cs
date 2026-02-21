namespace Forex.Wpf.Pages.Settings.Controls;

using Forex.Wpf.Pages.Settings.ViewModels;
using Forex.Wpf.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

public partial class CurrencySettingsControl : UserControl
{
    private readonly CurrencySettingsViewModel vm;

    public CurrencySettingsControl()
    {
        InitializeComponent();
        vm = App.AppHost!.Services.GetRequiredService<CurrencySettingsViewModel>();
        DataContext = vm;
    }

    private CurrencyViewModel? _draggedItem;

    private void DragHandle_MouseDown(object sender, MouseButtonEventArgs e)
    {
        var element = sender as FrameworkElement;
        _draggedItem = element?.DataContext as CurrencyViewModel;
    }

    private void DragHandle_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && _draggedItem != null)
        {
            DragDrop.DoDragDrop((DependencyObject)sender, _draggedItem, DragDropEffects.Move);
        }
    }

    private void DragHandle_MouseUp(object sender, MouseButtonEventArgs e)
    {
        _draggedItem = null;
    }

    private void ItemsControl_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(typeof(CurrencyViewModel)) is CurrencyViewModel draggedItem &&
            ((FrameworkElement)e.OriginalSource).DataContext is CurrencyViewModel targetItem &&
            draggedItem != targetItem)
        {
            vm.MoveItem(draggedItem, targetItem);
        }
    }
}
