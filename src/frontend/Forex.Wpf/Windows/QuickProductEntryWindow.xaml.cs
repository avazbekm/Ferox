namespace Forex.Wpf.Windows;

using Forex.ClientService;
using Forex.ClientService.Extensions;
using Forex.ClientService.Models.Requests;
using Forex.Wpf.ViewModels;
using System.Windows;

public partial class QuickProductEntryWindow : Window
{
    private readonly ForexClient client;
    private readonly ProductViewModel product;
    private readonly ProductTypeViewModel productType;
    private readonly DateTime _maxDate;

    public int EnteredCount { get; private set; }

    public QuickProductEntryWindow(
        ProductViewModel product,
        ProductTypeViewModel productType,
        int needed,
        int stock,
        DateTime saleDate,
        ForexClient client)
    {
        InitializeComponent();
        Owner = Application.Current.MainWindow;
        this.client = client;
        this.product = product;
        this.productType = productType;
        _maxDate = saleDate.Date;

        txtProductName.Text = product.Name;
        txtProductCode.Text = product.Code;
        txtProductType.Text = productType.Type;
        txtUnitPrice.Text = (productType.UnitPrice ?? 0).ToString("N0");

        int bundleItemCount = productType.BundleItemCount ?? 1;
        int neededBundles = bundleItemCount > 0 ? (int)Math.Ceiling((double)needed / bundleItemCount) : needed;
        int stockBundles = bundleItemCount > 0 ? stock / bundleItemCount : stock;

        txtNeeded.Text = $"{neededBundles} qop ({needed:N0} ta)";
        txtStock.Text = $"{stockBundles} qop ({stock:N0} ta)";
        txtBundleInfo.Text = $"{bundleItemCount} ta mahsulot";

        entryDate.SelectedDate = _maxDate;

        int shortfall = Math.Max(0, needed - stock);
        int defaultBundleCount = bundleItemCount > 0
            ? (int)Math.Ceiling((double)shortfall / bundleItemCount)
            : 1;
        txtBundleCount.Text = Math.Max(1, defaultBundleCount).ToString();

        txtBundleCount.Focus();
        txtBundleCount.SelectAll();
    }

    private async void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        if (entryDate.SelectedDate is not DateTime selectedDate)
        {
            MessageBox.Show("Kirim sanasi kiritilmagan!", "Xatolik", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (selectedDate.Date > _maxDate)
        {
            MessageBox.Show(
                $"Kirim sanasi savdo sanasidan ({_maxDate:dd.MM.yyyy}) kech bo'lishi mumkin emas!",
                "Noto'g'ri sana",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        if (!int.TryParse(txtBundleCount.Text, out int bundleCount) || bundleCount <= 0)
        {
            MessageBox.Show("Qop soni noto'g'ri kiritilgan!", "Xatolik", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!decimal.TryParse(txtUnitPrice.Text.Replace(" ", "").Replace(",", ""), out decimal unitPrice) || unitPrice <= 0)
        {
            MessageBox.Show("Tan narxi noto'g'ri kiritilgan!", "Xatolik", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        int bundleItemCount = productType.BundleItemCount ?? 1;
        int totalCount = bundleCount * bundleItemCount;

        var entryDateTime = selectedDate.Date == DateTime.Today ? DateTime.Now : selectedDate.Date;

        var request = new ProductEntryRequest
        {
            Date = entryDateTime,
            BundleItemCount = bundleItemCount,
            Count = totalCount,
            UnitPrice = unitPrice,
            ProductionOrigin = product.ProductionOrigin,
            ProductTypeId = productType.Id,
            Product = new ProductRequest
            {
                Id = product.Id,
                Code = product.Code,
                Name = product.Name,
                ProductionOrigin = product.ProductionOrigin,
                ProductTypes =
                [
                    new ProductTypeRequest
                    {
                        Id = productType.Id,
                        Type = productType.Type,
                        BundleItemCount = bundleItemCount,
                        UnitPrice = unitPrice
                    }
                ]
            }
        };

        btnSave.IsEnabled = false;

        var response = await client.ProductEntries.Create(request).Handle();

        if (response.IsSuccess)
        {
            EnteredCount = totalCount;
            DialogResult = true;
            Close();
        }
        else
        {
            MessageBox.Show(response.Message ?? "Kirimda xatolik!", "Xatolik", MessageBoxButton.OK, MessageBoxImage.Error);
            btnSave.IsEnabled = true;
        }
    }
}

