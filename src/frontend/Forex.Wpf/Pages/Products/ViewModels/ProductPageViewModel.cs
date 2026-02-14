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
using Mapster;
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
    [ObservableProperty] private ProductViewModel currentProduct;

    public ProductPageViewModel(IMapper mapper, ForexClient client)
    {
        this.mapper = mapper;
        this.client = client;
        CurrentProductEntry = new();
        CurrentProduct = new();
        _ = LoadDataAsync();
    }

    [ObservableProperty] private ObservableCollection<ProductViewModel> availableProducts = [];
    [ObservableProperty] private ObservableCollection<ProductEntryViewModel> productEntries = [];
    [ObservableProperty] private ProductEntryViewModel? selectedProductEntry;
    [ObservableProperty] private string productType = string.Empty;
    [ObservableProperty] private string productCode = string.Empty;
    [ObservableProperty] private ProductEntryViewModel currentProductEntry;

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

    partial void OnCurrentProductEntryChanged(ProductEntryViewModel? oldValue, ProductEntryViewModel newValue)
    {
        if (oldValue is not null)
            oldValue.PropertyChanged -= OnCurrentEntryPropertyChanged;

        if (newValue is not null)
            newValue.PropertyChanged += OnCurrentEntryPropertyChanged;
    }

    partial void OnCurrentProductChanged(ProductViewModel? oldValue, ProductViewModel newValue)
    {
        if (oldValue is not null)
            oldValue.PropertyChanged -= OnProductPropertyChanged;

        if (newValue is not null)
        {
            newValue.PropertyChanged += OnProductPropertyChanged;
            CurrentProductEntry.ProductionOrigin = newValue.ProductionOrigin;

            if (ProductCode != newValue.Code)
                ProductCode = newValue.Code;
        }
    }

    private void OnCurrentEntryPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ProductEntryViewModel.BundleCount) or nameof(ProductEntryViewModel.BundleItemCount))
        {
            if (CurrentProductEntry.BundleCount.HasValue && CurrentProductEntry.BundleItemCount.HasValue)
                CurrentProductEntry.Count = CurrentProductEntry.BundleCount * CurrentProductEntry.BundleItemCount;
        }
    }

    private void OnProductPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ProductViewModel.SelectedType) && CurrentProduct.SelectedType is not null)
            UpdateEntryCalculations();
    }

    partial void OnProductCodeChanged(string? oldValue, string newValue)
    {
        if (!string.IsNullOrEmpty(oldValue))
        {
            var toRemove = AvailableProducts.FirstOrDefault(c => c.Code == oldValue && c.Id < 1);
            if (toRemove != null)
            {
                AvailableProducts.Remove(toRemove);
            }
        }

        if (string.IsNullOrWhiteSpace(newValue) || (CurrentProduct != null && CurrentProduct.Code == newValue))
            return;

        var existing = AvailableProducts.FirstOrDefault(c => string.Equals(c.Code, newValue, StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
        {
            CurrentProduct = existing;
            IsNewProductMode = false;
        }
        else if (Confirm($"'{newValue}' yangi mahsulot sifatida qo'shilsinmi?"))
        {
            CurrentProduct = new ProductViewModel { Code = newValue };
            IsNewProductMode = true;
            CurrentProduct.ImagePath = string.Empty;
        }
        else
        {
            ProductCode = string.Empty;
            WeakReferenceMessenger.Default.Send(new FocusControlMessage("ProductCode"));
        }
    }

    partial void OnProductTypeChanged(string? oldValue, string newValue)
    {
        var type = CurrentProduct.SelectedType;
        var types = CurrentProduct.ProductTypes;
        types.Remove(types.FirstOrDefault(c => c.Type == oldValue && c.Id < 1)!);

        if (string.IsNullOrWhiteSpace(newValue) || type?.Type == newValue) return;

        var existing = types.FirstOrDefault(c => string.Equals(c.Type, newValue, StringComparison.OrdinalIgnoreCase));
        if (existing is not null) CurrentProduct.SelectedType = existing;
        else if (Confirm($"'{newValue}' yangi razmer sifatida qo'shilsinmi?"))
        {
            CurrentProduct.SelectedType = new() { Type = newValue };
            types.Add(CurrentProduct.SelectedType);

            CurrentProduct.ProductTypes = new ObservableCollection<ProductTypeViewModel>(types);
            OnPropertyChanged(nameof(CurrentProduct));
        }
        else { ProductType = string.Empty; WeakReferenceMessenger.Default.Send(new FocusControlMessage("ProductType")); }
    }

    #endregion

    #region Commands

    [RelayCommand]
    private async Task Save()
    {
        if (!Validate()) return;

        ProductEntryRequest request = mapper.Map<ProductEntryRequest>(CurrentProductEntry);
        request.Product = mapper.Map<ProductRequest>(CurrentProduct);

        if (CurrentProduct.SelectedType is not null)
            request.Product.ProductTypes = [CurrentProduct.SelectedType.Adapt<ProductTypeRequest>()];

        if (IsNewProductMode && !string.IsNullOrWhiteSpace(CurrentProduct.ImagePath))
        {
            var uploadedImagePath = await client.FileStorage.UploadFileAsync(CurrentProduct.ImagePath);
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
                updatedEntry.ProductType = CurrentProduct.SelectedType!;
                updatedEntry.ProductType.Product = CurrentProduct;

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
                newEntry.ProductType = CurrentProduct.SelectedType!;
                newEntry.ProductType.Product = CurrentProduct;

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

        var product = AvailableProducts.FirstOrDefault(p => p.Id == SelectedProductEntry.ProductType!.ProductId);
        if (product is null && SelectedProductEntry.ProductType?.Product is not null)
            product = AvailableProducts.FirstOrDefault(p => p.Code == SelectedProductEntry.ProductType.Product.Code);

        if (product is not null)
        {
            CurrentProduct = product;
            ProductCode = CurrentProduct.Code;

            var type = CurrentProduct.ProductTypes.FirstOrDefault(pt => pt.Type == SelectedProductEntry.ProductType!.Type);
            if (type is not null)
                CurrentProduct.SelectedType = type;
            else
            {
                CurrentProduct.SelectedType = SelectedProductEntry.ProductType!;
                if (!CurrentProduct.ProductTypes.Any(pt => pt.Type == SelectedProductEntry.ProductType!.Type))
                    CurrentProduct.ProductTypes.Add(SelectedProductEntry.ProductType!);
            }
        }
        else
        {
            CurrentProduct = new ProductViewModel();
            ProductCode = string.Empty;
        }

        ProductEntries.Remove(SelectedProductEntry);
        SelectedProductEntry = null;
    }

    [RelayCommand]
    private async Task Delete(ProductEntryViewModel? entry)
    {
        if (entry is null) return;

        if (!Confirm("Ushbu kirimni o'chirmoqchimisiz?")) return;

        var response = await client.ProductEntries.Delete(entry.Id).Handle(l => IsLoading = l);

        if (response.IsSuccess)
        {
            ProductEntries.Remove(entry);
            SuccessMessage = "Muvaffaqiyatli o'chirildi!";
        }
        else
        {
            ErrorMessage = response.Message ?? "O'chirishda xatolik!";
        }
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
        CurrentProduct = new();
        ProductCode = string.Empty;
    }

    private bool Validate()
    {
        if (CurrentProduct is null) return SetWarning("Mahsulot tanlanmagan!");
        if (string.IsNullOrWhiteSpace(CurrentProduct.Code)) return SetWarning("Mahsulot kodi kiritilmagan!");
        if (CurrentProduct.SelectedType is null) return SetWarning("Mahsulot turi tanlanmagan!");

        if (IsNewProductMode)
        {
            if (string.IsNullOrWhiteSpace(CurrentProduct.Name)) return SetWarning("Mahsulot nomi kiritilmagan!");
        }

        if (string.IsNullOrWhiteSpace(CurrentProduct.SelectedType.Type)) return SetWarning("Razmer nomi kiritilmagan!");

        if (!CurrentProductEntry.Count.HasValue || CurrentProductEntry.Count <= 0) return SetWarning("Son (Count) kiritilmagan!");

        return true;
    }

    private bool SetWarning(string msg)
    {
        WarningMessage = msg;
        return false;
    }

    private void UpdateEntryCalculations()
    {
        if (CurrentProduct?.SelectedType is not null)
        {
            CurrentProductEntry.UnitPrice = CurrentProduct.SelectedType.UnitPrice;
            CurrentProductEntry.BundleItemCount = CurrentProduct.SelectedType.BundleItemCount;

            if (CurrentProductEntry.BundleCount.HasValue && CurrentProductEntry.BundleItemCount.HasValue)
                CurrentProductEntry.Count = CurrentProductEntry.BundleCount * CurrentProductEntry.BundleItemCount;
        }
    }

    #endregion Helpers
}