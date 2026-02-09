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

public partial class UnitMeasureSettingsViewModel : ViewModelBase
{
    private readonly IMapper mapper;
    private readonly IServiceProvider services;

    public UnitMeasureSettingsViewModel(IServiceProvider services)
    {
        this.services = services;
        mapper = services.GetRequiredService<IMapper>();

        _ = LoadUnitMeasures();
    }

    [ObservableProperty] private ObservableCollection<UnitMeasuerViewModel> unitMeasures = [];

    [RelayCommand]
    private void AddUnitMeasure()
    {
        UnitMeasures.Add(new());
    }

    [RelayCommand]
    private void RemoveUnitMeasure(UnitMeasuerViewModel unitMeasure)
    {
        UnitMeasures.Remove(unitMeasure);
    }

    [RelayCommand]
    private async Task SaveUnitMeasures()
    {
        if (UnitMeasures is null || UnitMeasures.Count == 0)
        {
            WarningMessage = "Saqlash uchun o'lchov birligi yo'q";
            return;
        }

        var client = services.GetRequiredService<IApiUnitMeasures>();
        var dtoList = mapper.Map<List<UnitMeasureRequest>>(UnitMeasures);

        var response = await client.SaveAllAsync(dtoList)
            .Handle(isLoading => IsLoading = isLoading);

        if (response.IsSuccess) SuccessMessage = "O'zgarishlar muvaffaqiyatli saqlandi";
        else ErrorMessage = response.Message ?? "O'lchov birliklarini saqlashda xatolik";
    }

    private async Task LoadUnitMeasures()
    {
        var client = services.GetRequiredService<IApiUnitMeasures>();
        var response = await client.GetAllAsync().Handle(isLoading => IsLoading = isLoading);

        if (response.IsSuccess)
            UnitMeasures = mapper.Map<ObservableCollection<UnitMeasuerViewModel>>(response.Data);
        else ErrorMessage = response.Message ?? "O'lchov birliklarini yuklashda xatolik";
    }

    public void MoveItem(UnitMeasuerViewModel draggedItem, UnitMeasuerViewModel targetItem)
    {
        if (draggedItem == targetItem) return;

        int oldIndex = UnitMeasures.IndexOf(draggedItem);
        int newIndex = UnitMeasures.IndexOf(targetItem);

        UnitMeasures.Move(oldIndex, newIndex);

        for (int i = 0; i < UnitMeasures.Count; i++)
        {
            UnitMeasures[i].Position = i + 1;
        }
    }
}
