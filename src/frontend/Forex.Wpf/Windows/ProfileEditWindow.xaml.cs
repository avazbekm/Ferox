namespace Forex.Wpf.Windows;

using Forex.ClientService;
using Forex.Wpf.Windows.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

public partial class ProfileEditWindow : Window
{
    private readonly ProfileEditViewModel vm;
    private readonly ForexClient client;
    private string? uploadedTmpImagePath;

    public ProfileEditWindow()
    {
        InitializeComponent();
        client = App.AppHost!.Services.GetRequiredService<ForexClient>();
        vm = new ProfileEditViewModel(client);
        DataContext = vm;

        Loaded += ProfileEditWindow_Loaded;
    }

    private async void ProfileEditWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await vm.LoadUserDataAsync();
    }

    private async void Avatar_Click(object sender, MouseButtonEventArgs e)
    {
        var openFileDialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Rasm fayllari (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png",
            Title = "Profil rasmini tanlang"
        };

        if (openFileDialog.ShowDialog() == true)
        {
            // Show preview
            imgProfilePreview.ImageSource = new BitmapImage(new Uri(openFileDialog.FileName));

            // Upload to MinIO
            btnSave.IsEnabled = false;
            uploadedTmpImagePath = await client.FileStorage.UploadFileAsync(openFileDialog.FileName);
            btnSave.IsEnabled = true;

            if (uploadedTmpImagePath == null)
            {
                MessageBox.Show("Rasm yuklashda xatolik!", "Xatolik", MessageBoxButton.OK, MessageBoxImage.Error);
                imgProfilePreview.ImageSource = new BitmapImage(new Uri("/Resources/Assets/profile.jpg", UriKind.Relative));
            }
        }
    }

    private async void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(pwdNewPassword.Password))
        {
            if (pwdNewPassword.Password.Length < 4)
            {
                MessageBox.Show("Parol kamida 4 ta belgidan iborat bo'lishi kerak!", "Xatolik", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (pwdNewPassword.Password != pwdConfirmPassword.Password)
            {
                MessageBox.Show("Parollar mos kelmayapti!", "Xatolik", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            vm.NewPassword = pwdNewPassword.Password;
        }

        // Pass uploaded image path
        if (!string.IsNullOrWhiteSpace(uploadedTmpImagePath))
        {
            vm.TmpImagePath = uploadedTmpImagePath;
        }

        btnSave.IsEnabled = false;
        var success = await vm.SaveAsync();
        btnSave.IsEnabled = true;

        if (success)
        {
            MessageBox.Show("Profil muvaffaqiyatli yangilandi!", "Muvaffaqiyat", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }
        else
        {
            MessageBox.Show(vm.ErrorMessage ?? "Saqlashda xatolik!", "Xatolik", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
