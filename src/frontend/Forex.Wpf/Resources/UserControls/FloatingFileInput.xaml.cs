namespace Forex.Wpf.Resources.UserControls;

using Forex.Wpf.Services;
using System.IO;
using System.Windows;
using System.Windows.Controls;

public partial class FloatingFileInput : UserControl
{
    public FloatingFileInput()
    {
        InitializeComponent();
    }

    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(FloatingFileInput), new PropertyMetadata(""));

    public static readonly DependencyProperty FileNameProperty =
        DependencyProperty.Register(
            nameof(FileName),
            typeof(string),
            typeof(FloatingFileInput),
            new FrameworkPropertyMetadata(
                string.Empty,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnFileNameChanged));

    private static void OnFileNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (FloatingFileInput)d;
        if (e.NewValue is null)
            control.input.Text = string.Empty;
    }

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string FileName
    {
        get => (string)GetValue(FileNameProperty);
        set => SetValue(FileNameProperty, value);
    }

    private async void BtnBrowse_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Surat tanlang",
            Filter = "Rasmlar (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg"
        };

        if (dialog.ShowDialog() != true) return;

        try
        {
            button.IsEnabled = false;
            button.Content = "⏳";

            using var fileStream = File.OpenRead(dialog.FileName);
            using var compressedStream = await ImageCompressionService.CompressImageAsync(fileStream);

            var tempFileName = $"temp_{Guid.NewGuid():N}.jpg";
            var tempPath = Path.Combine(Path.GetTempPath(), tempFileName);

            using (var tempFile = File.Create(tempPath))
            {
                await compressedStream.CopyToAsync(tempFile);
            }

            FileName = tempPath;
        }
        catch
        {
            MessageBox.Show("Rasmni yuklashda xatolik!", "Xatolik", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            button.IsEnabled = true;
            button.Content = "📁";
        }
    }
}
