namespace Forex.Wpf.Pages.Products.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Forex.ClientService;
using Forex.ClientService.Enums;
using Forex.ClientService.Extensions;
using Forex.ClientService.Models.Commons;
using Forex.ClientService.Models.Requests;
using Forex.Wpf.Common.Extensions;
using Forex.Wpf.Common.Interfaces;
using Forex.Wpf.Pages.Common;
using Forex.Wpf.ViewModels;
using MapsterMapper;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;

public partial class ProductPageViewModel : ViewModelBase, INavigationAware
{
    private readonly ForexClient client;
    private readonly IMapper mapper;
    private readonly bool isFillingForm;
    private ProductEntryViewModel? backupEntry;
    private int currentPage = 1;
    private readonly int pageSize = 20;
    private bool hasMoreItems = true;

    public ProductPageViewModel(IMapper mapper, ForexClient client)
    {
        this.mapper = mapper;
        this.client = client;
        CurrentProductEntry = new();
        CurrentProductEntry.PropertyChanged += OnCurrentEntryPropertyChanged;
        _ = LoadDataAsync();
    }

    [ObservableProperty] private ObservableCollection<ProductViewModel> availableProducts = [];
    [ObservableProperty] private ObservableCollection<ProductEntryViewModel> productEntries = [];
    [ObservableProperty] private ProductEntryViewModel? selectedProductEntry;
    [ObservableProperty] private ProductEntryViewModel currentProductEntry;

    [ObservableProperty] private bool isProductComboEditable = true;
    [ObservableProperty] private bool isProductionOriginEnabled = true;
    [ObservableProperty] private bool isTypeEditable = true;
    [ObservableProperty] private bool showImageInput;
    [ObservableProperty] private bool showProductCodeInput;
    [ObservableProperty] private bool showProductNameInput;

    [ObservableProperty] private string productCode = string.Empty;
    [ObservableProperty] private string productTypeName = string.Empty;

    public string[] ProductionOrigins { get; } = Enum.GetNames<ProductionOrigin>();

    #region Loading Data

    private async Task LoadDataAsync() => await Task.WhenAll(LoadProductsAsync(), LoadProductEntriesAsync());

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
            PageSize = pageSize
        };

        var response = await client.ProductEntries.Filter(request).Handle(l => IsLoading = l);
        if (response.IsSuccess)
        {
            ProductEntries.AddRange(mapper.Map<ObservableCollection<ProductEntryViewModel>>(response.Data));
            hasMoreItems = response.Data.Count >= pageSize;
        }
        else
        {
            hasMoreItems = false;
            if (!response.IsSuccess)
                ErrorMessage = response.Message ?? (pageSize != 20 ? "Qo'shimcha ma'lumotlarni yuklashda xatolik!" : "Mahsulot kirimlarini yuklashda xatolik!");
        }
    }

    private async Task LoadProductTypesForProduct(ProductViewModel product)
    {
        if (product.ProductTypes?.Any() == true || product.Id <= 0) return;

        FilteringRequest request = new() { Filters = new() { ["productid"] = [product.Id.ToString()] } };
        var response = await client.ProductTypes.Filter(request).Handle(l => IsLoading = l);

        if (response.IsSuccess) product.ProductTypes = mapper.Map<ObservableCollection<ProductTypeViewModel>>(response.Data);
        else ErrorMessage = response.Message ?? "Mahsulot turlarini yuklashda xatolik";
    }

    private async Task LoadProductTypesAndSetDefault(ProductViewModel product)
    {
        await LoadProductTypesForProduct(product);
        if (product.SelectedType is null && product.ProductTypes?.Any() == true)
        {
            product.SelectedType = product.ProductTypes.First();
            ProductTypeName = product.SelectedType.Type;
        }
    }

    #endregion Loading Data

    #region Commands

    [RelayCommand]
    private async Task Save()
    {
        if (!ValidateEntry()) return;

        string? uploadedImagePath = null;
        if (CurrentProductEntry.IsNewProduct && CurrentProductEntry.Product != null && !string.IsNullOrWhiteSpace(CurrentProductEntry.Product.SelectedImageFile))
        {
            uploadedImagePath = await client.FileStorage.UploadFileAsync(CurrentProductEntry.Product.SelectedImageFile);
            if (uploadedImagePath is null)
            {
                ErrorMessage = "Rasm yuklashda xatolik yuz berdi.";
                return;
            }
        }

        var entryRequest = BuildProductEntryRequest(uploadedImagePath);

        if (IsEditing && CurrentProductEntry.Id > 0)
        {
            var response = await client.ProductEntries.Update(entryRequest).Handle(l => IsLoading = l);
            HandleResponse(response.IsSuccess, response.Message, "Mahsulot kirimi yangilandi!", true);
        }
        else
        {
            var response = await client.ProductEntries.Create(entryRequest).Handle(l => IsLoading = l);
            HandleResponse(response.IsSuccess, response.Message, "Mahsulot kirimi saqlandi!", false);
        }
    }

    [RelayCommand]
    public async Task Edit()
    {
        if (SelectedProductEntry is null) return;

        if (IsEditing && HasUnsavedChanges() && !Confirm("Hozirgi ma'lumotlar o'chib ketadi. Davom etasizmi?", MessageBoxImage.Warning))
            return;

        backupEntry = mapper.Map<ProductEntryViewModel>(SelectedProductEntry);

        if (!AvailableProducts.Any()) await LoadProductsAsync();

        CurrentProductEntry.PropertyChanged -= OnCurrentEntryPropertyChanged;
        try
        {
            await PrepareEntryForEdit(SelectedProductEntry);
            SetModeEdit();
            IsEditing = true;
            ProductEntries.Remove(SelectedProductEntry);
            SelectedProductEntry = null;
        }
        finally
        {
            CurrentProductEntry.PropertyChanged += OnCurrentEntryPropertyChanged;
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        if (backupEntry != null)
        {
            ProductEntries.Add(backupEntry);
            backupEntry = null;
        }
        ClearCurrentEntry();
        IsEditing = false;
    }

    [RelayCommand]
    private async Task Delete(ProductEntryViewModel entry)
    {
        if (entry == null || !Confirm($"'{entry.Product?.Name}' kirimini o'chirishni xohlaysizmi?")) return;

        var response = await client.ProductEntries.Delete(entry.Id).Handle(l => IsLoading = l);
        if (response.IsSuccess)
        {
            ProductEntries.Remove(entry);
            SuccessMessage = "Muvaffaqiyatli o'chirildi!";
        }
        else ErrorMessage = response.Message ?? "O'chirishda xatolik!";
    }

    [RelayCommand]
    private async Task LoadMoreProductEntries()
    {
        if (!hasMoreItems || IsLoading) return;
        currentPage++;

        await LoadProductEntriesAsync();
    }

    #endregion Commands

    #region Property Changed

    partial void OnProductTypeNameChanged(string value)
    {
        if (!CurrentProductEntry.IsNewProduct && CurrentProductEntry?.Product != null)
        {
            var matchingType = CurrentProductEntry.Product.ProductTypes?.FirstOrDefault(t => string.Equals(t.Type, value, StringComparison.OrdinalIgnoreCase));
            if (matchingType != null && CurrentProductEntry.Product.SelectedType != matchingType)
            {
                CurrentProductEntry.Product.SelectedType = matchingType;
            }
        }
    }

    private void OnCurrentEntryPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ProductEntryViewModel.Product) && CurrentProductEntry.Product != null)
        {
            if (!isFillingForm)
            {
                if (CurrentProductEntry.Product.Id > 0)
                {
                    SetModeNormal();
                    _ = LoadProductTypesAndSetDefault(CurrentProductEntry.Product);
                }
                CurrentProductEntry.Product.PropertyChanged += OnProductViewModelPropertyChanged;
                RecalculateCount();
            }
        }

        if (e.PropertyName == nameof(ProductEntryViewModel.BundleCount))
        {
            RecalculateCount();
        }

        if (e.PropertyName == nameof(ProductEntryViewModel.ProductionOriginName))
        {
            if (Enum.TryParse<ProductionOrigin>(CurrentProductEntry.ProductionOriginName, out var origin))
            {
                CurrentProductEntry.ProductionOrigin = origin;
                if (CurrentProductEntry.Product != null)
                    CurrentProductEntry.Product.ProductionOrigin = origin;
            }
        }
    }

    private void OnProductViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ProductViewModel.SelectedType) && CurrentProductEntry.Product?.SelectedType != null)
        {
            CurrentProductEntry.Product.SelectedType.PropertyChanged += OnProductTypePropertyChanged;
            RecalculateCount();
        }
    }

    private void OnProductTypePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ProductTypeViewModel.BundleItemCount))
        {
            RecalculateCount();
        }
    }

    private void RecalculateCount()
    {
        if (CurrentProductEntry.BundleCount != null && CurrentProductEntry.Product?.SelectedType?.BundleItemCount != null)
        {
            CurrentProductEntry.Count = CurrentProductEntry.BundleCount * CurrentProductEntry.Product.SelectedType.BundleItemCount;
        }
    }

    #endregion Property Changed

    #region Helpers

    private void HandleResponse(bool isSuccess, string? errorMessage, string successMessage, bool isUpdate)
    {
        if (isSuccess)
        {
            SuccessMessage = successMessage;
            if (isUpdate) IsEditing = false;
            ClearCurrentEntry();
            _ = LoadDataAsync();
        }
        else ErrorMessage = errorMessage ?? "Amalni bajarishda xatolik!";
    }

    private bool ValidateEntry()
    {
        if (CurrentProductEntry.Product is null) return SetWarning("Mahsulot tanlanmagan!");

        if (CurrentProductEntry.IsNewProduct && string.IsNullOrWhiteSpace(CurrentProductEntry.Product.Name))
            return SetWarning("Mahsulot nomini kiriting!");

        if (!CurrentProductEntry.IsNewProduct && CurrentProductEntry.Product.SelectedType is null && !string.IsNullOrWhiteSpace(ProductTypeName))
        {
            if (Confirm($"Yangi '{ProductTypeName}' razmeri qo'shilsinmi?", MessageBoxImage.Question))
            {
                var newType = new ProductTypeViewModel { Type = ProductTypeName, Product = CurrentProductEntry.Product };
                CurrentProductEntry.Product.ProductTypes ??= [];
                CurrentProductEntry.Product.ProductTypes.Add(newType);
                CurrentProductEntry.Product.SelectedType = newType;
            }
            else return false;
        }

        if (CurrentProductEntry.Product.SelectedType is null) return SetWarning("Razmer tanlanmagan!");
        if (string.IsNullOrWhiteSpace(CurrentProductEntry.Product.SelectedType.Type)) return SetWarning("Razmer nomi bo'sh bo'lishi mumkin emas!");
        if (CurrentProductEntry.BundleCount == null || CurrentProductEntry.BundleCount <= 0) return SetWarning("Qop sonini kiriting!");
        if (CurrentProductEntry.Product.SelectedType.BundleItemCount == null || CurrentProductEntry.Product.SelectedType.BundleItemCount <= 0) return SetWarning("Qopdagi sonni kiriting!");
        if (CurrentProductEntry.Product.SelectedType.UnitPrice == null || CurrentProductEntry.Product.SelectedType.UnitPrice <= 0) return SetWarning("Tannarxni kiriting!");

        return true;
    }

    private bool SetWarning(string message)
    {
        WarningMessage = message;
        return false;
    }

    private ProductEntryRequest BuildProductEntryRequest(string? uploadedImagePath)
    {
            var product = CurrentProductEntry.Product!;
        var selectedType = product.SelectedType!;

        return new ProductEntryRequest
        {
            Date = CurrentProductEntry.Date.Date == DateTime.Today ? DateTime.Now : CurrentProductEntry.Date.Date,
            Count = (int)(CurrentProductEntry.Count ?? 0),
            BundleItemCount = (int)(selectedType.BundleItemCount ?? 0),
            PreparationCostPerUnit = 0,
            UnitPrice = selectedType.UnitPrice ?? 0,
            ProductionOrigin = CurrentProductEntry.ProductionOrigin ?? ProductionOrigin.Tayyor,
            Product = new ProductRequest
            {
                Id = CurrentProductEntry.IsNewProduct ? 0 : product.Id,
                Code = product.Code,
                Name = product.Name,
                ProductionOrigin = CurrentProductEntry.ProductionOrigin ?? ProductionOrigin.Tayyor,
                ImagePath = !string.IsNullOrEmpty(uploadedImagePath) ? uploadedImagePath : product.ImagePath,
                ProductTypes =
                [
                    new() {
                        Id = CurrentProductEntry.IsNewProduct ? 0 : selectedType.Id,
                        Type = selectedType.Type
                    }
                ]
            }
        };
    }

    private async Task PrepareEntryForEdit(ProductEntryViewModel entry)
    {
        var productCode = entry.ProductType?.Product?.Code;
        if (string.IsNullOrEmpty(productCode))
        {
            WarningMessage = "Mahsulot ma'lumotlari topilmadi!";
            return;
        }

        var matchingProduct = AvailableProducts.FirstOrDefault(p => p.Code == productCode);
        if (matchingProduct is null)
        {
            matchingProduct = new ProductViewModel
            {
                Id = entry.ProductType?.Product?.Id ?? 0,
                Code = productCode,
                Name = entry.ProductType?.Product?.Name ?? "",
                ProductionOrigin = entry.ProductionOrigin ?? ProductionOrigin.Tayyor,
                ProductTypes = []
            };
            if (matchingProduct.Id > 0) await LoadProductTypesForProduct(matchingProduct);
            AvailableProducts.Add(matchingProduct);
        }
        else
        {
            await LoadProductTypesForProduct(matchingProduct);
        }

        CurrentProductEntry.Id = entry.Id;
        CurrentProductEntry.Date = entry.Date;
        CurrentProductEntry.BundleCount = entry.BundleCount;
        CurrentProductEntry.Count = entry.Count;
        CurrentProductEntry.ProductionOrigin = entry.ProductionOrigin ?? ProductionOrigin.Tayyor;
        CurrentProductEntry.ProductionOriginName = (CurrentProductEntry.ProductionOrigin ?? ProductionOrigin.Tayyor).ToString();
        CurrentProductEntry.Product = matchingProduct;

        if (entry.ProductType is not null)
        {
            var matchingType = matchingProduct.ProductTypes?.FirstOrDefault(t => t.Type == entry.ProductType.Type);
            if (matchingType is null)
            {
                matchingType = mapper.Map<ProductTypeViewModel>(entry.ProductType);
                matchingProduct.ProductTypes ??= [];
                matchingProduct.ProductTypes.Add(matchingType);
            }
            else
            {
                matchingType.BundleItemCount = entry.ProductType.BundleItemCount;
                matchingType.UnitPrice = entry.ProductType.UnitPrice;
            }
            matchingProduct.SelectedType = matchingType;
            ProductTypeName = matchingType.Type;
        }
    }

    private void ClearCurrentEntry()
    {
        CurrentProductEntry.PropertyChanged -= OnCurrentEntryPropertyChanged;
        CurrentProductEntry = new ProductEntryViewModel();
        SetModeNormal();
        CurrentProductEntry.PropertyChanged += OnCurrentEntryPropertyChanged;
    }

    private bool HasUnsavedChanges() => CurrentProductEntry.Product?.Id > 0 || !string.IsNullOrEmpty(CurrentProductEntry.Product?.Code);

    private void SetModeNormal()
    {
        IsProductComboEditable = true;
        IsProductionOriginEnabled = true;
        IsTypeEditable = true;
        ShowImageInput = false;
        ShowProductCodeInput = false;
        ShowProductNameInput = false;
    }

    private void SetModeEdit()
    {
        IsProductComboEditable = false;
        IsProductionOriginEnabled = false;
        IsTypeEditable = true;
        ShowImageInput = false;
        ShowProductCodeInput = false;
        ShowProductNameInput = false;
    }

    public void OnNavigatedTo() { }
    public void OnNavigatedFrom() { }

    #endregion Helpers
}