namespace Forex.Wpf.Pages.Products.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Forex.ClientService;
using Forex.ClientService.Enums;
using Forex.ClientService.Extensions;
using Forex.ClientService.Models.Commons;
using Forex.ClientService.Models.Requests;
using Forex.Wpf.Common.Extensions;
using Forex.Wpf.Common.Messages;
using Forex.Wpf.Pages.Common;
using Forex.Wpf.ViewModels;
using MapsterMapper;
using System.Collections.ObjectModel;
using System.ComponentModel;

public partial class ProductPageViewModel : ViewModelBase
{
    private readonly ForexClient client;
    private readonly IMapper mapper;
    private ProductEntryViewModel? backupEntry;
    private int backupIndex = -1;
    private int currentPage = 1;
    private bool hasMoreItems = true;
    private const int PageSize = 35;
    [ObservableProperty] private bool isNewProductMode;

    public ProductPageViewModel(IMapper mapper, ForexClient client)
    {
        this.mapper = mapper;
        this.client = client;
        CurrentProductEntry.PropertyChanged += OnCurrentEntryPropertyChanged;
        _ = LoadDataAsync();
    }

    [ObservableProperty] private ObservableCollection<ProductViewModel> availableProducts = [];
    [ObservableProperty] private ObservableCollection<ProductEntryViewModel> productEntries = [];
    [ObservableProperty] private ProductEntryViewModel? selectedProductEntry;
    [ObservableProperty] private string productType = string.Empty;
    [ObservableProperty] private string productCode = string.Empty;
    [ObservableProperty] private ProductEntryViewModel currentProductEntry = new();



    public static IEnumerable<ProductionOrigin> ProductionOrigins => Enum.GetValues<ProductionOrigin>();

    #region Loading Data

    private async Task LoadDataAsync()
    {
        await Task.WhenAll(
            LoadProductsAsync(),
            LoadProductEntriesAsync());
    }

    private async Task LoadProductsAsync()
    {
        var response = await client.Products.GetAllAsync().Handle(l => IsLoading = l);
        if (response.IsSuccess) AvailableProducts = mapper.Map<ObservableCollection<ProductViewModel>>(response.Data);
        else ErrorMessage = response.Message ?? "Mahsulotlarni yuklashda xatolik!";
    }

    private async Task LoadProductEntriesAsync()
    {
        FilteringRequest request = new() 
        {
            Filters = new() { ["producttype"] = ["include:product"] },
            Descending = true,
            SortBy = "date",
            Page = currentPage,
            PageSize = PageSize
        };

        var response = await client.ProductEntries.Filter(request).Handle(l => IsLoading = l);
        if (response.IsSuccess)
        {
             ProductEntries.AddRange(mapper.Map<ObservableCollection<ProductEntryViewModel>>(response.Data));
             if (response.Data.Count < PageSize) hasMoreItems = false;
        }
        else ErrorMessage = response.Message ?? "Kirim tarixini yuklashda xatolik!";
    }

    [RelayCommand]
    private async Task LoadMoreEntries()
    {
        if (IsLoading || !hasMoreItems) return;

        currentPage++;
        await LoadProductEntriesAsync();
    }

    #endregion

    #region Event Handlers

    partial void OnProductCodeChanged(string? oldValue, string newValue)
    {
        var product = CurrentProductEntry.Product;
        AvailableProducts.Remove(AvailableProducts.FirstOrDefault(c => c.Code == oldValue && c.Id < 1)!);

        if (string.IsNullOrWhiteSpace(newValue) || product?.Code == newValue) return;

        var existing = AvailableProducts.FirstOrDefault(c => string.Equals(c.Code, newValue, StringComparison.OrdinalIgnoreCase));
        if (existing is not null) product = existing;
        else if (Confirm($"'{newValue}' yangi mahsulot sifatida qo'shilsinmi?"))
        {
            CurrentProductEntry.Product = new() { Code = newValue };
            IsNewProductMode = true;
        }
        else { ProductCode = string.Empty; WeakReferenceMessenger.Default.Send(new FocusControlMessage("ProductCode")); }
    }

    partial void OnProductTypeChanged(string? oldValue, string newValue)
    {
        var type = CurrentProductEntry.Product.SelectedType;
        var types = CurrentProductEntry.Product!.ProductTypes;
        types.Remove(types.FirstOrDefault(c => c.Type == oldValue && c.Id < 1)!);

        if (string.IsNullOrWhiteSpace(newValue) || type?.Type == newValue) return;

        var existing = types.FirstOrDefault(c => string.Equals(c.Type, newValue, StringComparison.OrdinalIgnoreCase));
        if (existing is not null) type = existing;
        else if (Confirm($"'{newValue}' yangi razmer sifatida qo'shilsinmi?"))
        {
            types.Add(new() { Type = newValue });
            type = null;
        }
        else { ProductType = string.Empty; WeakReferenceMessenger.Default.Send(new FocusControlMessage("ProductType")); }
    }

    private void OnCurrentEntryPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ProductEntryViewModel.BundleCount) or nameof(ProductEntryViewModel.BundleItemCount))
        {
            if (CurrentProductEntry.BundleCount.HasValue && CurrentProductEntry.BundleItemCount.HasValue)
                CurrentProductEntry.Count = CurrentProductEntry.BundleCount * CurrentProductEntry.BundleItemCount;
        }
        else if (e.PropertyName is nameof(ProductEntryViewModel.Product) && CurrentProductEntry.Product is not null)
        {
            CurrentProductEntry.ProductionOrigin = CurrentProductEntry.Product!.ProductionOrigin;
            if (ProductCode != CurrentProductEntry.Product.Code)
                ProductCode = CurrentProductEntry.Product.Code;
        }
    }

    private void OnProductPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ProductViewModel.SelectedType) && CurrentProductEntry.Product?.SelectedType is not null)
             UpdateEntryCalculations();
    }

    #endregion

    #region Commands

    [RelayCommand]
    private async Task Save()
    {
        if (!Validate()) return;

        ProductEntryRequest request = mapper.Map<ProductEntryRequest>(CurrentProductEntry);
        if (IsNewProductMode && !string.IsNullOrWhiteSpace(CurrentProductEntry.Product?.SelectedImageFile))
        {
            var uploadedImagePath = await client.FileStorage.UploadFileAsync(CurrentProductEntry.Product.SelectedImageFile);
            if (uploadedImagePath is null)
            {
                ErrorMessage = "Rasm yuklashda xatolik! Qaytadan urinib ko'ring.";
                return;
            }
            request.Product.ImagePath = uploadedImagePath;
        }

        if (IsEditing && CurrentProductEntry.Id > 0)
        {
            var response = await client.ProductEntries.Update(request).Handle(l => IsLoading = l);
            if (response.IsSuccess)
            {
                var updatedEntry = mapper.Map<ProductEntryViewModel>(CurrentProductEntry);
                updatedEntry.Id = response.Data;
                updatedEntry.ProductType = CurrentProductEntry.Product!.SelectedType!;
                updatedEntry.ProductType.Product = CurrentProductEntry.Product;

                if (backupIndex >= 0 && backupIndex < ProductEntries.Count)
                    ProductEntries[backupIndex] = updatedEntry;
                else
                    ProductEntries.Add(updatedEntry);

                SuccessMessage = "Muvaffaqiyatli yangilandi!";
                CleanupAfterSave();
            }
            else ErrorMessage = response.Message ?? "Yangilashda xatolik!";
        }
        else
        {
            var response = await client.ProductEntries.Create(request).Handle(l => IsLoading = l);
            if (response.IsSuccess)
            {
                var newEntry = mapper.Map<ProductEntryViewModel>(CurrentProductEntry);
                newEntry.Id = response.Data;
                newEntry.ProductType = CurrentProductEntry.Product!.SelectedType!;
                newEntry.ProductType.Product = CurrentProductEntry.Product;

                ProductEntries.Insert(0, newEntry);
                SuccessMessage = "Muvaffaqiyatli saqlandi!";

                if (IsNewProductMode)
                    await LoadProductsAsync();

                CleanupAfterSave();
            }
            else ErrorMessage = response.Message ?? "Saqlashda xatolik!";
        }
    }

    [RelayCommand]
    private async Task Edit()
    {
        if (SelectedProductEntry is null) return;
        
        if (IsEditing && !Confirm("Hozirgi tahrirlash jarayoni bekor qilinadi. Davom etasizmi?")) return;

        Cancel();

        IsEditing = true;
        IsNewProductMode = false;
        
        backupIndex = ProductEntries.IndexOf(SelectedProductEntry);
        backupEntry = mapper.Map<ProductEntryViewModel>(SelectedProductEntry);

        CurrentProductEntry = mapper.Map<ProductEntryViewModel>(SelectedProductEntry);
        CurrentProductEntry.Product = AvailableProducts.FirstOrDefault(p => p.Id == SelectedProductEntry.ProductType!.ProductId);
        ProductCode = CurrentProductEntry.Product?.Code ?? string.Empty;
        CurrentProductEntry.Product!.SelectedType = CurrentProductEntry.Product.ProductTypes.FirstOrDefault(pt => pt.Type == SelectedProductEntry.ProductType!.Type);

        ProductEntries.Remove(SelectedProductEntry);
        SelectedProductEntry = null;
    }

    [RelayCommand]
    private void Cancel()
    {
        if (IsEditing && backupEntry is not null)
        {
            if (backupIndex >= 0 && backupIndex <= ProductEntries.Count)
                ProductEntries.Insert(backupIndex, backupEntry);
            else
                ProductEntries.Add(backupEntry);
        }
        
        CleanupAfterSave();
    }

    #endregion

    #region Helpers

    private void CleanupAfterSave()
    {
        IsEditing = false;
        IsNewProductMode = false;
        backupEntry = null;
        backupIndex = -1;
        CurrentProductEntry = new();
        ProductCode = string.Empty;
    }

    private bool Validate()
    {
        if (CurrentProductEntry.Product is null) return SetWarning("Mahsulot tanlanmagan!");
        if (string.IsNullOrWhiteSpace(CurrentProductEntry.Product.Code)) return SetWarning("Mahsulot kodi kiritilmagan!");
        if (CurrentProductEntry.Product.SelectedType is null) return SetWarning("Mahsulot turi tanlanmagan!");

        if (IsNewProductMode)
        {
            if (string.IsNullOrWhiteSpace(CurrentProductEntry.Product.Name)) return SetWarning("Mahsulot nomi kiritilmagan!");
        }

        if (CurrentProductEntry.Product.SelectedType is null) return SetWarning("Razmer/Type tanlanmagan!");
        if (string.IsNullOrWhiteSpace(CurrentProductEntry.Product.SelectedType.Type)) return SetWarning("Razmer nomi kiritilmagan!");
        
        if (!CurrentProductEntry.Count.HasValue || CurrentProductEntry.Count <= 0) return SetWarning("Son (Count) kiritilmagan!");
        
        return true;
    }

    private bool SetWarning(string msg)
    {
        WarningMessage = msg;
        return false;
    }

    #endregion

    #region Helpers

    private void UpdateEntryCalculations()
    {
        if (CurrentProductEntry.Product?.SelectedType is not null)
        {
            CurrentProductEntry.UnitPrice = CurrentProductEntry.Product.SelectedType.UnitPrice;
            CurrentProductEntry.BundleItemCount = CurrentProductEntry.Product.SelectedType.BundleItemCount;

            if (CurrentProductEntry.BundleCount.HasValue && CurrentProductEntry.BundleItemCount.HasValue)
                CurrentProductEntry.Count = CurrentProductEntry.BundleCount * CurrentProductEntry.BundleItemCount;
        }
    }
    #endregion Helpers
}