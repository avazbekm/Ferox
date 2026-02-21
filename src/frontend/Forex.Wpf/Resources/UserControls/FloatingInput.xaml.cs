namespace Forex.Wpf.Resources.UserControls;

using System.Windows;
using System.Windows.Controls;

public partial class FloatingInput : UserControl
{
    public FloatingInput()
    {
        InitializeComponent();
    }

    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(nameof(Label), typeof(string), typeof(FloatingInput), new PropertyMetadata(""));
    public string Label { get => (string)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(nameof(Text), typeof(object), typeof(FloatingInput), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
    public object? Text { get => GetValue(TextProperty); set => SetValue(TextProperty, value); }

    public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register(nameof(IsReadOnly), typeof(bool), typeof(FloatingInput), new PropertyMetadata(false));
    public bool IsReadOnly { get => (bool)GetValue(IsReadOnlyProperty); set => SetValue(IsReadOnlyProperty, value); }
}
