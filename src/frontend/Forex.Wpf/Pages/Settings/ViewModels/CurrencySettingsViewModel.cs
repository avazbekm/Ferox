namespace Forex.Wpf.Pages.Settings.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Forex.ClientService.Extensions;
using Forex.ClientService.Interfaces;
using Forex.ClientService.Models.Requests;
using Forex.Wpf.Pages.Common;
using Forex.Wpf.ViewModels;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;

public partial class CurrencySettingsViewModel : ViewModelBase
{
    private readonly IMapper mapper;
    private readonly IServiceProvider services;

    public CurrencySettingsViewModel(IServiceProvider services)
    {
        this.services = services;
        mapper = services.GetRequiredService<IMapper>();

        _ = LoadCurrencies();
    }

    [ObservableProperty] private ObservableCollection<CurrencyViewModel> currencies = [];

    [RelayCommand]
    private void AddCurrency()
    {
        Currencies.Add(new());
    }

    [RelayCommand]
    private void RemoveCurrency(CurrencyViewModel currency)
    {
        Currencies.Remove(currency);
    }

    [RelayCommand]
    private async Task SaveCurrencies()
    {
        if (Currencies is null || Currencies.Count == 0)
        {
            WarningMessage = "Saqlash uchun valyuta yo'q";
            return;
        }

        var client = services.GetRequiredService<IApiCurrency>();
        var dtoList = mapper.Map<List<CurrencyRequest>>(Currencies);

        var response = await client.SaveAllAsync(dtoList)
            .Handle(isLoading => IsLoading = isLoading);

        if (response.IsSuccess) SuccessMessage = "O'zgarishlar muvaffaqiyatli saqlandi";
        else ErrorMessage = response.Message ?? "Valyutalarni saqlashda xatolik";
    }

    private async Task LoadCurrencies()
    {
        var client = services.GetRequiredService<IApiCurrency>();
        var response = await client.GetAllAsync().Handle(isLoading => IsLoading = isLoading);

        if (response.IsSuccess)
            Currencies = mapper.Map<ObservableCollection<CurrencyViewModel>>(response.Data);
        else ErrorMessage = response.Message ?? "Valyutalarni yuklashda xatolik";
    }

    public void MoveItem(CurrencyViewModel draggedItem, CurrencyViewModel targetItem)
    {
        if (draggedItem == targetItem) return;

        int oldIndex = Currencies.IndexOf(draggedItem);
        int newIndex = Currencies.IndexOf(targetItem);

        Currencies.Move(oldIndex, newIndex);

        for (int i = 0; i < Currencies.Count; i++)
        {
            Currencies[i].Position = i + 1;
        }
    }
}
