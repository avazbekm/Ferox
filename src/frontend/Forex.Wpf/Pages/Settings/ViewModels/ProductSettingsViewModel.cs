namespace Forex.Wpf.Pages.Settings.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Forex.ClientService;
using Forex.ClientService.Extensions;
using Forex.ClientService.Models.Requests;
using Forex.Wpf.Common.Interfaces;
using Forex.Wpf.Pages.Common;
using Forex.Wpf.ViewModels;
using Mapster;
using MapsterMapper;
using Microsoft.Win32;
using System.Collections.ObjectModel;

public partial class ProductSettingsViewModel : ViewModelBase
{
    private readonly ForexClient client;
    private readonly IMapper mapper;
    private readonly IDialogService dialogService;

    public ProductSettingsViewModel(ForexClient client, IMapper mapper, IDialogService dialogService)
    {
        this.client = client;
        this.mapper = mapper;
        this.dialogService = dialogService;
        _ = LoadProductsAsync();
    }

    [ObservableProperty] private ObservableCollection<ProductViewModel> products = [];
    [ObservableProperty] private ProductViewModel? selectedProduct;
    [ObservableProperty] private string searchText = string.Empty;

    partial void OnSelectedProductChanged(ProductViewModel? value)
    {
        OnPropertyChanged(nameof(HasSelectedProduct));
    }

    public bool HasSelectedProduct => SelectedProduct is not null;

    private async Task LoadProductsAsync()
    {
        var response = await client.Products.GetAllAsync().Handle(l => IsLoading = l);
        if (response.IsSuccess)
            Products = mapper.Map<ObservableCollection<ProductViewModel>>(response.Data);
        else
            ErrorMessage = response.Message ?? "Mahsulotlarni yuklashda xatolik!";
    }

    [RelayCommand]
    private void AddProduct()
    {
        var newProduct = new ProductViewModel { Name = "Yangi mahsulot", Code = "" };
        Products.Insert(0, newProduct);
        SelectedProduct = newProduct;
    }

    [RelayCommand]
    private async Task SaveProduct()
    {
        if (SelectedProduct is null) return;

        if (string.IsNullOrWhiteSpace(SelectedProduct.Code))
        {
            WarningMessage = "Mahsulot kodi kiritilmagan!";
            return;
        }

        if (string.IsNullOrWhiteSpace(SelectedProduct.Name))
        {
            WarningMessage = "Mahsulot nomi kiritilmagan!";
            return;
        }

        var request = mapper.Map<ProductRequest>(SelectedProduct);

        if (SelectedProduct.ProductTypes.Count > 0)
            request.ProductTypes = SelectedProduct.ProductTypes.Adapt<List<ProductTypeRequest>>();

        if (SelectedProduct.Id > 0)
        {
            // UPDATE — backend rasmni boshqaradi (temp→permanent, eski rasm o'chirish)
            var response = await client.Products.Update(request).Handle(l => IsLoading = l);
            if (response.IsSuccess)
                SuccessMessage = "Mahsulot muvaffaqiyatli yangilandi!";
            else
                ErrorMessage = response.Message ?? "Yangilashda xatolik!";
        }
        else
        {
            // CREATE — backend rasmni temp dan ko'chiradi
            var response = await client.Products.Create(request).Handle(l => IsLoading = l);
            if (response.IsSuccess)
            {
                SelectedProduct.Id = response.Data ?? 0;
                SuccessMessage = "Mahsulot muvaffaqiyatli saqlandi!";
            }
            else
            {
                ErrorMessage = response.Message ?? "Saqlashda xatolik!";
            }
        }
    }

    [RelayCommand]
    private async Task DeleteProduct()
    {
        if (SelectedProduct is null) return;
        if (SelectedProduct.Id <= 0)
        {
            Products.Remove(SelectedProduct);
            SelectedProduct = null;
            return;
        }

        if (!await dialogService.ShowYesNoAsync($"'{SelectedProduct.Name}' mahsulotini o'chirmoqchimisiz?"))
            return;

        var response = await client.Products.Delete(SelectedProduct.Id).Handle(l => IsLoading = l);

        if (response.IsSuccess)
        {
            Products.Remove(SelectedProduct);
            SelectedProduct = null;
            SuccessMessage = "Mahsulot muvaffaqiyatli o'chirildi!";
        }
        else
        {
            ErrorMessage = response.Message ?? "O'chirishda xatolik! Mahsulot savdoda qatnashgan bo'lishi mumkin.";
        }
    }

    [RelayCommand]
    private void AddProductType()
    {
        if (SelectedProduct is null) return;

        var newType = new ProductTypeViewModel
        {
            Type = "Yangi tur",
            ProductId = SelectedProduct.Id,
            Product = SelectedProduct
        };

        SelectedProduct.ProductTypes.Add(newType);
    }

    [RelayCommand]
    private async Task DeleteProductType(ProductTypeViewModel? type)
    {
        if (type is null || SelectedProduct is null) return;

        if (!await dialogService.ShowYesNoAsync($"'{type.Type}' turini o'chirmoqchimisiz?"))
            return;

        if (type.Id <= 0)
        {
            SelectedProduct.ProductTypes.Remove(type);
            return;
        }

        var response = await client.ProductTypes.Delete(type.Id).Handle(l => IsLoading = l);

        if (response.IsSuccess)
        {
            SelectedProduct.ProductTypes.Remove(type);
            SuccessMessage = "Tur muvaffaqiyatli o'chirildi!";
        }
        else
        {
            ErrorMessage = response.Message ?? "Turni o'chirishda xatolik! U savdoda qatnashgan bo'lishi mumkin.";
        }
    }

    [RelayCommand]
    private async Task UploadImage()
    {
        if (SelectedProduct is null) return;

        var dialog = new OpenFileDialog
        {
            Filter = "Rasmlar (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg",
            Title = "Mahsulot rasmi tanlash"
        };

        if (dialog.ShowDialog() == true)
        {
            // Rasmni serverga yuklaymiz (temp folderga tushadi)
            var uploadedPath = await client.FileStorage.UploadFileAsync(dialog.FileName);

            if (!string.IsNullOrEmpty(uploadedPath))
            {
                SelectedProduct.ImagePath = uploadedPath;
                SuccessMessage = "Rasm yuklandi! Saqlash tugmasini bosishni unutmang.";
            }
            else
            {
                ErrorMessage = "Rasm yuklashda xatolik!";
            }
        }
    }

    [RelayCommand]
    private void DeleteImage()
    {
        if (SelectedProduct is null) return;

        // ImagePath ni bo'shatamiz, saqlash bosilganda backend eski faylni o'chiradi
        SelectedProduct.ImagePath = string.Empty;
        SuccessMessage = "Rasm o'chirildi! Saqlash tugmasini bosing.";
    }
}
