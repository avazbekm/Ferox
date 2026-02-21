namespace Forex.Wpf.Pages.Settings.Controls;

using Forex.Wpf.Pages.Settings.ViewModels;
using Forex.Wpf.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

public partial class UnitMeasureSettingsControl : UserControl
{
    private readonly UnitMeasureSettingsViewModel vm;

    public UnitMeasureSettingsControl()
    {
        InitializeComponent();
        vm = App.AppHost!.Services.GetRequiredService<UnitMeasureSettingsViewModel>();
        DataContext = vm;
    }

    private UnitMeasuerViewModel? _draggedItem;

    private void DragHandle_MouseDown(object sender, MouseButtonEventArgs e)
    {
        var element = sender as FrameworkElement;
        _draggedItem = element?.DataContext as UnitMeasuerViewModel;
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
        if (e.Data.GetData(typeof(UnitMeasuerViewModel)) is UnitMeasuerViewModel draggedItem &&
            ((FrameworkElement)e.OriginalSource).DataContext is UnitMeasuerViewModel targetItem &&
            draggedItem != targetItem)
        {
            vm.MoveItem(draggedItem, targetItem);
        }
    }
}
