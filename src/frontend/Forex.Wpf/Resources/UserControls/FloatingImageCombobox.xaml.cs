namespace Forex.Wpf.Resources.UserControls;

using System;
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

public partial class FloatingImageComboBox : UserControl
{
    private bool _isUpdatingInternally;

    public FloatingImageComboBox()
    {
        InitializeComponent();
    }

    // Dependency Properties
    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(nameof(Label), typeof(string), typeof(FloatingImageComboBox), new PropertyMetadata(string.Empty));
    public string Label { get => (string)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

    public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(FloatingImageComboBox), new PropertyMetadata(null, OnItemsSourceChanged));
    public IEnumerable? ItemsSource { get => (IEnumerable?)GetValue(ItemsSourceProperty); set => SetValue(ItemsSourceProperty, value); }

    public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register(nameof(SelectedItem), typeof(object), typeof(FloatingImageComboBox), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedItemChanged));
    public object? SelectedItem { get => GetValue(SelectedItemProperty); set => SetValue(SelectedItemProperty, value); }

    private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctrl = (FloatingImageComboBox)d;
        if (ctrl._isUpdatingInternally) return;

        if (e.NewValue != null)
        {
            var val = GetPropertyValue(e.NewValue, ctrl.PrimaryTextMemberPath)?.ToString();
            if (val != null)
            {
                ctrl._isUpdatingInternally = true;
                ctrl.Text = val;
                ctrl._isUpdatingInternally = false;
            }
        }
    }

    public static readonly DependencyProperty IsEditableProperty = DependencyProperty.Register(nameof(IsEditable), typeof(bool), typeof(FloatingImageComboBox), new PropertyMetadata(false));
    public bool IsEditable { get => (bool)GetValue(IsEditableProperty); set => SetValue(IsEditableProperty, value); }

    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(nameof(Text), typeof(string), typeof(FloatingImageComboBox), new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnTextChanged));
    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }

    public static readonly DependencyProperty IsSearchEnabledProperty = DependencyProperty.Register(nameof(IsSearchEnabled), typeof(bool), typeof(FloatingImageComboBox), new PropertyMetadata(true));
    public bool IsSearchEnabled { get => (bool)GetValue(IsSearchEnabledProperty); set => SetValue(IsSearchEnabledProperty, value); }

    public static readonly DependencyProperty PrimaryTextMemberPathProperty = DependencyProperty.Register(nameof(PrimaryTextMemberPath), typeof(string), typeof(FloatingImageComboBox), new PropertyMetadata(string.Empty, (d, e) =>
    {
        var ctrl = (FloatingImageComboBox)d;
        TextSearch.SetTextPath(ctrl.combo, (string)e.NewValue);
    }));
    public string PrimaryTextMemberPath { get => (string)GetValue(PrimaryTextMemberPathProperty); set => SetValue(PrimaryTextMemberPathProperty, value); }

    public static readonly DependencyProperty SecondaryTextMemberPathProperty = DependencyProperty.Register(nameof(SecondaryTextMemberPath), typeof(string), typeof(FloatingImageComboBox));
    public string SecondaryTextMemberPath { get => (string)GetValue(SecondaryTextMemberPathProperty); set => SetValue(SecondaryTextMemberPathProperty, value); }

    public static readonly DependencyProperty ImageMemberPathProperty = DependencyProperty.Register(nameof(ImageMemberPath), typeof(string), typeof(FloatingImageComboBox));
    public string ImageMemberPath { get => (string)GetValue(ImageMemberPathProperty); set => SetValue(ImageMemberPathProperty, value); }

    public static readonly DependencyProperty ImageWidthProperty = DependencyProperty.Register(nameof(ImageWidth), typeof(double), typeof(FloatingImageComboBox), new PropertyMetadata(24.0));
    public double ImageWidth { get => (double)GetValue(ImageWidthProperty); set => SetValue(ImageWidthProperty, value); }

    public static readonly DependencyProperty ImageHeightProperty = DependencyProperty.Register(nameof(ImageHeight), typeof(double), typeof(FloatingImageComboBox), new PropertyMetadata(24.0));
    public double ImageHeight { get => (double)GetValue(ImageHeightProperty); set => SetValue(ImageHeightProperty, value); }

    public static readonly DependencyProperty ShowImageProperty = DependencyProperty.Register(nameof(ShowImage), typeof(bool), typeof(FloatingImageComboBox), new PropertyMetadata(true));
    public bool ShowImage { get => (bool)GetValue(ShowImageProperty); set => SetValue(ShowImageProperty, value); }

    public ComboBox ComboBoxControl => combo;

    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctrl = (FloatingImageComboBox)d;
        if (ctrl._isUpdatingInternally || !ctrl.IsLoaded) return;

        if (ctrl.IsSearchEnabled) ctrl.ApplyFilter();

        var currentText = e.NewValue?.ToString();
        var match = ctrl.FindMatch(currentText);

        ctrl._isUpdatingInternally = true;
        ctrl.SelectedItem = match;
        ctrl._isUpdatingInternally = false;
    }

    private object? FindMatch(string? text)
    {
        if (string.IsNullOrEmpty(text) || ItemsSource == null) return null;
        foreach (var item in ItemsSource)
        {
            var val = GetPropertyValue(item, PrimaryTextMemberPath)?.ToString();
            if (string.Equals(val, text, StringComparison.OrdinalIgnoreCase))
                return item;
        }
        return null;
    }

    private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((FloatingImageComboBox)d).ApplyFilter();

    private void ApplyFilter()
    {
        if (ItemsSource == null) return;
        var view = CollectionViewSource.GetDefaultView(ItemsSource);
        if (view == null) return;

        if (!IsSearchEnabled || string.IsNullOrWhiteSpace(Text))
        {
            view.Filter = null;
        }
        else
        {
            var filter = Text.ToLower();
            view.Filter = item =>
            {
                if (item == null) return false;
                var primary = GetPropertyValue(item, PrimaryTextMemberPath)?.ToString()?.ToLower() ?? "";
                var secondary = GetPropertyValue(item, SecondaryTextMemberPath)?.ToString()?.ToLower() ?? "";
                return primary.Contains(filter) || secondary.Contains(filter);
            };
        }
        view.Refresh();
    }

    private static object? GetPropertyValue(object item, string path)
    {
        if (string.IsNullOrEmpty(path) || item == null) return null;
        try
        {
            foreach (var part in path.Split('.'))
            {
                if (item == null) return null;
                var prop = item.GetType().GetProperty(part);
                if (prop == null) return null;
                item = prop.GetValue(item);
            }
            return item;
        }
        catch { return null; }
    }

    private void ComboBox_GotFocus(object sender, RoutedEventArgs e)
    {
        if (!combo.IsDropDownOpen) combo.IsDropDownOpen = true;
    }

    private void ComboBox_LostFocus(object sender, RoutedEventArgs e)
    {
        var view = CollectionViewSource.GetDefaultView(ItemsSource);
        if (view != null) view.Filter = null;
    }
}
