namespace Forex.Wpf.Pages.Products.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Forex.ClientService;
using Forex.ClientService.Enums;
using Forex.ClientService.Extensions;
using Forex.ClientService.Models.Commons;
using Forex.ClientService.Models.Requests;
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

    public ProductPageViewModel(IMapper mapper, ForexClient client)
    {
        this.mapper = mapper;
        this.client = client;
        
        CurrentProductEntry = new();
        _ = LoadDataAsync();
    }

    [ObservableProperty] private ObservableCollection<ProductViewModel> availableProducts = [];
    [ObservableProperty] private ObservableCollection<ProductEntryViewModel> productEntries = [];
    [ObservableProperty] private ProductEntryViewModel? selectedProductEntry;
    
    private ProductEntryViewModel currentProductEntry = new();
    public ProductEntryViewModel CurrentProductEntry
    {
        get => currentProductEntry;
        set
        {
            if (currentProductEntry != value)
            {
                if (currentProductEntry != null)
                {
                    currentProductEntry.PropertyChanged -= OnCurrentEntryPropertyChanged;
                    if (currentProductEntry.Product != null)
                        currentProductEntry.Product.PropertyChanged -= OnProductPropertyChanged;
                }

                SetProperty(ref currentProductEntry, value);

                if (currentProductEntry != null)
                {
                    currentProductEntry.PropertyChanged += OnCurrentEntryPropertyChanged;
                    if (currentProductEntry.Product != null)
                        currentProductEntry.Product.PropertyChanged += OnProductPropertyChanged;
                }
            }
        }
    }

    [ObservableProperty] private bool isNewProductMode;
    
    public IEnumerable<ProductionOrigin> ProductionOrigins => Enum.GetValues<ProductionOrigin>();

    #region Loading Data

    private async Task LoadDataAsync()
    {
        await LoadProductsAsync();
        await LoadProductEntriesAsync();
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
            Page = 1,
            PageSize = 20
        };

        var response = await client.ProductEntries.Filter(request).Handle(l => IsLoading = l);
        if (response.IsSuccess) ProductEntries = mapper.Map<ObservableCollection<ProductEntryViewModel>>(response.Data);
        else ErrorMessage = response.Message ?? "Kirim tarixini yuklashda xatolik!";
    }

    #endregion

    #region Event Handlers

    private void OnCurrentEntryPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ProductEntryViewModel.BundleCount) or nameof(ProductEntryViewModel.BundleItemCount))
        {
            if (CurrentProductEntry.BundleCount.HasValue && CurrentProductEntry.BundleItemCount.HasValue)
            {
                CurrentProductEntry.Count = CurrentProductEntry.BundleCount * CurrentProductEntry.BundleItemCount;
            }
        }
    }

    private void OnProductPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ProductViewModel.Code) && !IsNewProductMode && !IsEditing)
            CheckProductCode(CurrentProductEntry.Product?.Code);
        else if (e.PropertyName == nameof(ProductViewModel.SelectedType) && CurrentProductEntry.Product?.SelectedType != null)
             UpdateEntryCalculations();
    }

    private void CheckProductCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return;

        var existingProduct = AvailableProducts.FirstOrDefault(p => p.Code.Equals(code, StringComparison.OrdinalIgnoreCase));

        if (existingProduct != null)
        {
            if (CurrentProductEntry.Product != null) 
                CurrentProductEntry.Product.PropertyChanged -= OnProductPropertyChanged;

            CurrentProductEntry.Product = existingProduct;
            
            CurrentProductEntry.Product.PropertyChanged += OnProductPropertyChanged;
            
            if (CurrentProductEntry.Product.SelectedType == null && CurrentProductEntry.Product.ProductTypes?.Any() == true)
                CurrentProductEntry.Product.SelectedType = CurrentProductEntry.Product.ProductTypes.First();
            
            UpdateEntryCalculations();
        }
        else
        {
            if (Confirm($"'{code}' kodli mahsulot topilmadi. Yangi mahsulot qo'shasizmi?"))
            {
                IsNewProductMode = true;
                var newProduct = new ProductViewModel 
                { 
                    Code = code, 
                    Name = "",
                    SelectedType = new ProductTypeViewModel { Type = "Standard" } 
                };
                newProduct.ProductTypes.Add(newProduct.SelectedType);
                
                CurrentProductEntry.Product.PropertyChanged -= OnProductPropertyChanged;
                CurrentProductEntry.Product = newProduct;
                CurrentProductEntry.Product.PropertyChanged += OnProductPropertyChanged;
            }
            else if(CurrentProductEntry.Product != null) CurrentProductEntry.Product.Code = string.Empty;
        }
    }

    private void UpdateEntryCalculations()
    {
        if (CurrentProductEntry.Product?.SelectedType != null)
        {
            CurrentProductEntry.UnitPrice = CurrentProductEntry.Product.SelectedType.UnitPrice;
            CurrentProductEntry.BundleItemCount = CurrentProductEntry.Product.SelectedType.BundleItemCount;
      
            if (CurrentProductEntry.BundleCount.HasValue && CurrentProductEntry.BundleItemCount.HasValue)
                CurrentProductEntry.Count = CurrentProductEntry.BundleCount * CurrentProductEntry.BundleItemCount;
        }
    }

    #endregion

    #region Commands

    [RelayCommand]
    private async Task Save()
    {
        if (!Validate()) return;

        string? uploadedImagePath = null;
        if (IsNewProductMode && !string.IsNullOrWhiteSpace(CurrentProductEntry.Product?.SelectedImageFile))
        {
            uploadedImagePath = await client.FileStorage.UploadFileAsync(CurrentProductEntry.Product.SelectedImageFile);
            if (uploadedImagePath is null)
            {
                ErrorMessage = "Rasm yuklashda xatolik! Qaytadan urinib ko'ring.";
                return;
            }
        }

        var request = GenerateRequest(uploadedImagePath);

        if (IsEditing && CurrentProductEntry.Id > 0)
        {
            var response = await client.ProductEntries.Update(request).Handle(l => IsLoading = l);
            if (response.IsSuccess)
            {
               CurrentProductEntry.Id = response.Data;

                var updatedEntry = mapper.Map<ProductEntryViewModel>(CurrentProductEntry);

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
                CurrentProductEntry.Id = response.Data;
                var newEntry = mapper.Map<ProductEntryViewModel>(CurrentProductEntry);

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
        if (SelectedProductEntry == null) return;
        
        if (IsEditing && !Confirm("Hozirgi tahrirlash jarayoni bekor qilinadi. Davom etasizmi?")) return;

        Cancel();

        IsEditing = true;
        IsNewProductMode = false;
        
        backupIndex = ProductEntries.IndexOf(SelectedProductEntry);
        backupEntry = mapper.Map<ProductEntryViewModel>(SelectedProductEntry);

        CurrentProductEntry = mapper.Map<ProductEntryViewModel>(SelectedProductEntry);

        if (CurrentProductEntry.Product != null)
        {
            var existingProd = AvailableProducts.FirstOrDefault(p => p.Code == CurrentProductEntry.Product.Code);
            if (existingProd != null)
                CurrentProductEntry.Product = existingProd;
        }

        ProductEntries.Remove(SelectedProductEntry);
        SelectedProductEntry = null;
    }

    [RelayCommand]
    private void Cancel()
    {
        if (IsEditing && backupEntry != null)
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

    private ProductEntryRequest GenerateRequest(string? imagePath)
    {
        var request = mapper.Map<ProductEntryRequest>(CurrentProductEntry);
        
        if (IsNewProductMode)
        {
            request.Product.Id = 0;
            if (request.Product.ProductTypes != null)
                foreach(var type in request.Product.ProductTypes) type.Id = 0;

            if (!string.IsNullOrEmpty(imagePath)) request.Product.ImagePath = imagePath;
        }
        else request.Product.Id = CurrentProductEntry.Product!.Id;
        
        
        return request;
    }

    private void CleanupAfterSave()
    {
        IsEditing = false;
        IsNewProductMode = false;
        backupEntry = null;
        backupIndex = -1;
        CurrentProductEntry = new();
    }

    private bool Validate()
    {
        if (CurrentProductEntry.Product == null) return SetWarning("Mahsulot tanlanmagan!");
        if (string.IsNullOrWhiteSpace(CurrentProductEntry.Product.Code)) return SetWarning("Mahsulot kodi kiritilmagan!");
        
        if (IsNewProductMode)
        {
            if (string.IsNullOrWhiteSpace(CurrentProductEntry.Product.Name)) return SetWarning("Mahsulot nomi kiritilmagan!");
        }

        if (CurrentProductEntry.Product.SelectedType == null) return SetWarning("Razmer/Type tanlanmagan!");
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
}