namespace Forex.Wpf.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using Forex.ClientService.Enums;
using Forex.Wpf.Pages.Common;

public partial class ProductEntryViewModel : ViewModelBase
{
    public long Id { get; set; }

    [ObservableProperty] private ProductViewModel? product;
    [ObservableProperty] private uint? count;
    [ObservableProperty] private uint? availableCount;
    [ObservableProperty] private ProductionOrigin? productionOrigin;
    [ObservableProperty] private string productionOriginName = string.Empty;
    [ObservableProperty] private uint? bundleItemCount;
    [ObservableProperty] private uint? bundleCount;
    [ObservableProperty] private decimal? unitPrice;
    [ObservableProperty] private ProductTypeViewModel? productType;
    [ObservableProperty] private DateTime date = DateTime.Now;
    [ObservableProperty] private decimal? costPrice;
    [ObservableProperty] private decimal? preparationCostPerUnit;
    [ObservableProperty] private decimal? totalAmount;

    public bool IsNewProduct => Product is not null && Product.Id == 0;
}