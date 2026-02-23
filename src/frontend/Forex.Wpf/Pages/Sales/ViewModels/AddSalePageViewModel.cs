namespace Forex.Wpf.Pages.Sales.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Forex.ClientService;
using Forex.ClientService.Extensions;
using Forex.ClientService.Models.Commons;
using Forex.ClientService.Models.Requests;
using Forex.Wpf.Common.Interfaces;
using Forex.Wpf.Pages.Common;
using Forex.Wpf.ViewModels;
using MapsterMapper;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;

public partial class AddSalePageViewModel : ViewModelBase
{
    private readonly ForexClient client;
    private readonly IMapper mapper;
    private readonly INavigationService navigation;

    // Initialization state tracking
    private Task? _initializationTask;

    public AddSalePageViewModel(ForexClient client, IMapper mapper, INavigationService navigation)
    {
        this.client = client;
        this.mapper = mapper;
        this.navigation = navigation;
        CurrentSaleItem.PropertyChanged += SaleItemPropertyChanged;

        _initializationTask = LoadDataAsync();
    }

    [ObservableProperty] private DateTime date = DateTime.Now;
    [ObservableProperty] private decimal? totalAmount;
    [ObservableProperty] private decimal? finalAmount;
    [ObservableProperty] private decimal? totalAmountWithUserBalance;
    [ObservableProperty] private string note = string.Empty;

    [ObservableProperty] private SaleItemViewModel currentSaleItem = new();
    [ObservableProperty] private ObservableCollection<SaleItemViewModel> saleItems = [];
    [ObservableProperty] private SaleItemViewModel? selectedSaleItem = default;

    [ObservableProperty] private UserViewModel? customer;
    [ObservableProperty] private ObservableCollection<UserViewModel> availableCustomers = [];
    [ObservableProperty] private ObservableCollection<ProductViewModel> availableProducts = [];

    [ObservableProperty] private long editingSaleId = 0;

    #region Initialization

    private async Task EnsureInitializedAsync()
    {
        if (_initializationTask is not null)
        {
            await _initializationTask;
        }
    }

    private async Task LoadDataAsync()
    {
        await Task.WhenAll(
            LoadUsersAsync(),
            LoadProductsAsync()
        );
    }

    public async Task LoadUsersAsync()
    {
        FilteringRequest request = new()
        {
            Filters = new()
            {
                ["role"] = ["mijoz"],
                ["accounts"] = ["include:currency"]
            }
        };

        var response = await client.Users.Filter(request).Handle(isLoading => IsLoading = isLoading);
        if (response.IsSuccess)
            AvailableCustomers = mapper.Map<ObservableCollection<UserViewModel>>(response.Data!);
        else
            ErrorMessage = response.Message ?? "Mahsulot turlarini yuklashda xatolik.";
    }

    public async Task LoadProductsAsync()
    {
        FilteringRequest request = new()
        {
            Filters = new()
            {
                ["ProductType"] = ["include:Product"],
                ["Count"] = [">0"]
            }
        };

        var response = await client.ProductResidues.Filter(request).Handle(isLoading => IsLoading = isLoading);

        if (!response.IsSuccess)
        {
            ErrorMessage = response.Message ?? "Mahsulotlarni yuklashda xatolik.";
            return;
        }

        var productResidues = mapper.Map<ObservableCollection<ProductResidueViewModel>>(response.Data!);

        var allTypes = productResidues.Select(pr => pr.ProductType)
            .Where(pt => pt is not null && pt.Product is not null)
            .ToList();

        var grouped = allTypes.GroupBy(pt => pt.Product.Id);

        var products = new ObservableCollection<ProductViewModel>();

        foreach (var group in grouped)
        {
            var sampleType = group.First();
            var product = sampleType.Product;

            product.ProductTypes = new ObservableCollection<ProductTypeViewModel>(group);
            products.Add(product);
        }

        AvailableProducts = products;
    }

    #endregion

    #region Commands

    [RelayCommand]
    private void Add()
    {
        if (CurrentSaleItem.Product is null ||
            CurrentSaleItem.BundleCount == null ||
            CurrentSaleItem.ProductType is null ||
            CurrentSaleItem.UnitPrice is null)
        {
            WarningMessage = "Mahsulot tanlanmagan yoki miqdor noto'g'ri!";
            return;
        }

        SaleItemViewModel item = new()
        {
            Product = CurrentSaleItem.Product,
            ProductType = CurrentSaleItem.ProductType,
            BundleCount = CurrentSaleItem.BundleCount,
            UnitPrice = CurrentSaleItem.UnitPrice,
            Amount = CurrentSaleItem.Amount,
            TotalCount = CurrentSaleItem.TotalCount,
        };

        item.PropertyChanged += SaleItemPropertyChanged;
        SaleItems.Add(item);

        ClearCurrentSaleItem();
        RecalculateTotals();
    }

    [RelayCommand]
    private void Edit()
    {
        if (SelectedSaleItem is null)
            return;

        bool hasCurrentData = CurrentSaleItem.Product is not null ||
                             CurrentSaleItem.BundleCount.HasValue;

        if (hasCurrentData)
        {
            var result = MessageBox.Show(
                "Hozirgi kiritilgan ma'lumotlar o'chib ketadi. Davom etmoqchimisiz?",
                "Ogohlantirish",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.No)
                return;
        }

        CurrentSaleItem.PropertyChanged -= SaleItemPropertyChanged;

        try
        {
            CurrentSaleItem.Product = SelectedSaleItem.Product;
            CurrentSaleItem.ProductType = SelectedSaleItem.ProductType;
            CurrentSaleItem.BundleCount = SelectedSaleItem.BundleCount;
            CurrentSaleItem.UnitPrice = SelectedSaleItem.UnitPrice;
            CurrentSaleItem.Amount = SelectedSaleItem.Amount;
            CurrentSaleItem.TotalCount = SelectedSaleItem.TotalCount;

            SaleItems.Remove(SelectedSaleItem);
            SelectedSaleItem = null;

            RecalculateTotals();
        }
        finally
        {
            CurrentSaleItem.PropertyChanged += SaleItemPropertyChanged;
        }
    }

    [RelayCommand]
    private void DeleteItem(SaleItemViewModel item)
    {
        if (item is null)
            return;

        var result = MessageBox.Show(
            $"Mahsulotni o'chirishni tasdiqlaysizmi?",
            "O'chirishni tasdiqlash",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.No)
            return;

        item.PropertyChanged -= SaleItemPropertyChanged;
        SaleItems.Remove(item);
        RecalculateTotals();
    }

    [RelayCommand]
    private async Task Submit()
    {
        if (SaleItems.Count == 0)
        {
            WarningMessage = "Hech qanday mahsulot kiritilmagan!";
            return;
        }

        SaleRequest request = new()
        {
            Date = Date == DateTime.Today ? DateTime.Now : Date,
            CustomerId = Customer?.Id ?? 0,
            TotalAmount = FinalAmount ?? 0,
            Note = Note,
            SaleItems = [.. SaleItems.Select(item => new SaleItemRequest
            {
                ProductTypeId = item.ProductType.Id,
                BundleCount = (int)item.BundleCount!,
                UnitPrice = (decimal)item.UnitPrice!,
                Amount = (decimal)item.Amount!
            })]
        };

        bool isSuccess;

        if (EditingSaleId > 0)
        {
            request.Id = EditingSaleId;
            var response = await client.Sales.Update(request).Handle(isLoading => IsLoading = isLoading);

            if (isSuccess = response.IsSuccess)
                SuccessMessage = "Savdo muvaffaqiyatli yangilandi!";
            else ErrorMessage = response.Message ?? "Savdoni yangilashda xatolik!";
        }
        else
        {
            var response = await client.Sales.Create(request).Handle(isLoading => IsLoading = isLoading);

            if (isSuccess = response.IsSuccess)
            {
                SuccessMessage = $"Savdo muvaffaqiyatli yuborildi. Mahsulotlar soni: {SaleItems.Count}";

                var result = MessageBox.Show(
                    "Savdo muvaffaqiyatli saqlandi!\n\nChop etishni xohlaysizmi?",
                    "Muvaffaqiyat",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                    ShowPrintPreview();
            }
            else ErrorMessage = response.Message ?? "Savdoni ro'yxatga olishda xatolik!";
        }

        if (isSuccess)
        {
            Clear();
            navigation.GoBack();
        }
    }

    private static readonly Dictionary<string, BitmapSource> _imageCache = new();

    private BitmapSource GetOptimizedImage(string path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;

        if (_imageCache.ContainsKey(path)) return _imageCache[path];

        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(path);
            // Senior tip: Rasmni PDF katagi uchun kichraytirib yuklash (Xotirani tejaydi)
            bitmap.DecodePixelWidth = 100;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
            bitmap.EndInit();
            bitmap.Freeze(); // Multi-threading uchun muhim

            _imageCache[path] = bitmap;
            return bitmap;
        }
        catch
        {
            return null;
        }
    }


    public void ShowPrintPreview()
    {
        if (SaleItems == null || !SaleItems.Any())
        {
            MessageBox.Show("Ko'rsatish uchun ma'lumot yo'q.", "Eslatma", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var fixedDoc = CreateFixedDocumentForPrint();

        var toolbar = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(10)
        };

        var shareButton = new Button
        {
            Content = "Telegram’da ulashish",
            Padding = new Thickness(15, 5, 15, 5),
            Background = new SolidColorBrush(Color.FromRgb(0, 136, 204)),
            Foreground = Brushes.White,
            FontSize = 14,
            FontWeight = FontWeights.Bold,
            Cursor = Cursors.Hand,
            BorderThickness = new Thickness(0)
        };

        shareButton.Click += (s, e) =>
        {
            try
            {
                string docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string folder = Path.Combine(docs, "Forex", "Savdolar");
                Directory.CreateDirectory(folder);

                string customerName = (Customer?.Name ?? "Naqd").Replace(" ", "_");
                string fileName = $"Savdo_{customerName}_{DateTime.Now:dd_MM_yyyy}.pdf";
                string path = Path.Combine(folder, fileName);

                SaveFixedDocumentToPdf(fixedDoc, path, 96);

                if (File.Exists(path))
                {
                    Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{path}\"") { UseShellExecute = true });
                    Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ulashishda xatolik: {ex.Message}");
            }
        };

        toolbar.Children.Add(shareButton);

        var viewer = new DocumentViewer
        {
            Document = fixedDoc,
            Margin = new Thickness(8)
        };

        var layout = new DockPanel();
        DockPanel.SetDock(toolbar, Dock.Top);
        layout.Children.Add(toolbar);
        layout.Children.Add(viewer);

        var previewWindow = new Window
        {
            Title = "Sotuv cheki - Oldindan ko'rish",
            Width = 1050, // Rasm ustuni uchun biroz kengaytirdik
            Height = 850,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            WindowStyle = WindowStyle.SingleBorderWindow,
            ResizeMode = ResizeMode.CanResizeWithGrip,
            Content = layout
        };

        previewWindow.ShowDialog();
    }
    private FixedDocument CreateFixedDocumentForPrint()
    {
        var fixedDoc = new FixedDocument();
        double pageWidth = 793.7;
        double pageHeight = 1122.5;
        double marginTop = 40;
        double marginBottom = 40;
        double marginLeft = 30;
        double marginRight = 30;
        double tableWorkingWidth = pageWidth - marginLeft - marginRight;

        // 1. Kod bo'yicha saralash va guruhlash
        var groupedItems = SaleItems
            .OrderBy(i => i.Product.Code)
            .GroupBy(i => i.Product.Code)
            .ToList();

        var flatItems = groupedItems.SelectMany(g => g).ToList();

        decimal totalAmountSum = flatItems.Sum(i => i.Amount) ?? 0;
        double totalBundleCountSum = flatItems.Sum(i => i.BundleCount) ?? 0;
        double totalTotalCountSum = flatItems.Sum(i => i.TotalCount) ?? 0;

        string[] headers = { "T/r", "Rasm", "Kod", "Nomi", "Razmer", "Qop soni", "Jami soni", "Narxi", "Jami summa" };
        double[] finalWidths = { 35, 70, 60, 165, 60, 60, 70, 70, 143.7 };

        int maxRowsPerPage = 22; // Rasm borligi uchun qator sonini biroz kamaytirdik
        int totalPages = (int)Math.Ceiling((double)(flatItems.Count + 1) / maxRowsPerPage);

        int processedIndex = 0;
        int globalTr = 0;

        for (int pageIndex = 0; pageIndex < totalPages; pageIndex++)
        {
            var page = new FixedPage { Width = pageWidth, Height = pageHeight, Background = Brushes.White };
            var gridContainer = new StackPanel { Margin = new Thickness(marginLeft, marginTop, marginRight, 0) };

            gridContainer.Children.Add(new TextBlock
            {
                Text = "Sotilgan mahsulotlar ro'yxati",
                FontSize = 22,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 5)
            });

            gridContainer.Children.Add(new TextBlock
            {
                Text = $"Mijoz: {Customer?.Name.ToUpper() ?? "Naqd"} | Sana: {Date:dd.MM.yyyy}",
                FontSize = 14,
                Foreground = Brushes.Gray,
                Margin = new Thickness(0, 0, 0, 10)
            });

            var grid = new Grid { Width = tableWorkingWidth, HorizontalAlignment = HorizontalAlignment.Left };
            for (int i = 0; i < finalWidths.Length; i++)
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(finalWidths[i]) });

            // Header
            AddRow(grid, true, 0, headers);
            int currentRow = 1;

            int pageCount = Math.Min(maxRowsPerPage, flatItems.Count - processedIndex);
            var pageItems = flatItems.Skip(processedIndex).Take(pageCount).ToList();

            // Sahifadagi itemlarni kod bo'yicha guruhlab chiqish (RowSpan uchun)
            var pageGroups = pageItems.GroupBy(i => i.Product.Code).ToList();

            foreach (var group in pageGroups)
            {
                int groupSize = group.Count();
                bool imageRendered = false;

                foreach (var item in group)
                {
                    globalTr++;
                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                    // 1. T/R
                    AddCellToGrid(grid, globalTr.ToString(), currentRow, 0, false, TextAlignment.Center);

                    // 2. RASM (Faqat guruhning birinchi qatorida va RowSpan bilan)
                    if (!imageRendered)
                    {
                        var imageBorder = CreateImageCell(item.Product.DisplayImagePath);
                        Grid.SetRow(imageBorder, currentRow);
                        Grid.SetColumn(imageBorder, 1);
                        Grid.SetRowSpan(imageBorder, groupSize);
                        grid.Children.Add(imageBorder);
                        imageRendered = true;
                    }

                    // Qolgan ustunlar
                    AddCellToGrid(grid, item.Product.Code ?? "", currentRow, 2, false, TextAlignment.Left);
                    AddCellToGrid(grid, item.Product.Name ?? "", currentRow, 3, false, TextAlignment.Left);
                    AddCellToGrid(grid, item.ProductType.Type ?? "", currentRow, 4, false, TextAlignment.Center);
                    AddCellToGrid(grid, item.BundleCount?.ToString("N0") ?? "0", currentRow, 5, false, TextAlignment.Right);
                    AddCellToGrid(grid, item.TotalCount?.ToString("N0") ?? "0", currentRow, 6, false, TextAlignment.Right);
                    AddCellToGrid(grid, item.UnitPrice?.ToString("N2") ?? "0.00", currentRow, 7, false, TextAlignment.Right);
                    AddCellToGrid(grid, item.Amount?.ToString("N2") ?? "0.00", currentRow, 8, false, TextAlignment.Right);

                    currentRow++;
                }
            }

            processedIndex += pageItems.Count;

            // Jami qatori (Faqat oxirgi sahifada)
            if (pageIndex == totalPages - 1)
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                var totalLabel = CreateCell("Jami:", true, TextAlignment.Left);
                totalLabel.Padding = new Thickness(10, 5, 4, 5);
                Grid.SetRow(totalLabel, currentRow);
                Grid.SetColumn(totalLabel, 0);
                Grid.SetColumnSpan(totalLabel, 5); // T/r dan Razmergacha
                grid.Children.Add(totalLabel);

                AddCellToGrid(grid, totalBundleCountSum.ToString("N0"), currentRow, 5, true, TextAlignment.Right);
                AddCellToGrid(grid, totalTotalCountSum.ToString("N0"), currentRow, 6, true, TextAlignment.Right);

                var totalAmountCell = CreateCell(totalAmountSum.ToString("N2"), true, TextAlignment.Right);
                Grid.SetRow(totalAmountCell, currentRow);
                Grid.SetColumn(totalAmountCell, 7);
                Grid.SetColumnSpan(totalAmountCell, 2);
                if (totalAmountCell.Child is TextBlock tbSum)
                {
                    tbSum.FontSize = 14; tbSum.Foreground = new SolidColorBrush(Color.FromRgb(0, 50, 150));
                }
                grid.Children.Add(totalAmountCell);
            }

            gridContainer.Children.Add(grid);
            page.Children.Add(gridContainer);

            var pageNum = new TextBlock
            {
                Text = $"{pageIndex + 1} - bet / {totalPages}",
                FontSize = 11,
                Foreground = Brushes.Gray
            };
            FixedPage.SetRight(pageNum, marginRight);
            FixedPage.SetBottom(pageNum, marginBottom);
            page.Children.Add(pageNum);

            var pc = new PageContent();
            ((IAddChild)pc).AddChild(page);
            fixedDoc.Pages.Add(pc);
        }
        return fixedDoc;
    }

    private static readonly HttpClient _httpClient = new HttpClient();

    private BitmapSource GetImageFromMinio(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;

        try
        {
            // 1. Agar rasm URL bo'lsa (MinIO manzili)
            if (url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                // Senior tip: GetByteArrayAsync rasmni to'liq yuklab bo'lmaguncha kutadi
                var task = Task.Run(() => _httpClient.GetByteArrayAsync(url));
                task.Wait(TimeSpan.FromSeconds(5)); // 5 soniya kutish (timeout)

                if (!task.IsCompletedSuccessfully) return null;

                byte[] imageData = task.Result;
                using (var ms = new MemoryStream(imageData))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad; // Rasmni xotiraga "muhrlaydi"
                    bitmap.StreamSource = ms;
                    bitmap.DecodePixelWidth = 120; // Katakcha kichik bo'lgani uchun o'lchamni cheklaymiz
                    bitmap.EndInit();
                    bitmap.Freeze(); // UI threaddan tashqarida ham xavfsiz ishlash uchun
                    return bitmap;
                }
            }
            // 2. Agar rasm lokal fayl bo'lsa
            else if (File.Exists(url))
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(url, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.DecodePixelWidth = 120;
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
        }
        catch (Exception ex)
        {
            // Xatolikni kuzatish uchun (Output oynasida ko'rinadi)
            System.Diagnostics.Debug.WriteLine($"MinIO rasm xatosi: {ex.Message}");
        }

        return null;
    }
    private Border CreateImageCell(string imagePath)
    {
        // Katakcha kvadrat shaklda (PDF uchun ideal o'lcham)
        var border = new Border
        {
            Width = 65,
            Height = 65,
            BorderBrush = Brushes.LightGray,
            BorderThickness = new Thickness(0.5),
            Background = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Padding = new Thickness(1)
        };

        var bitmap = GetImageFromMinio(imagePath);

        if (bitmap != null)
        {
            var img = new Image
            {
                Source = bitmap,
                Stretch = Stretch.Uniform, // Rasm cho'zilib ketmaydi, kvadrat ichiga moslashadi
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            // Render sifatini oshirish (Senior tip)
            RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.HighQuality);
            border.Child = img;
        }
        else
        {
            // Rasm yo'q bo'lsa, chiroyliroq fallback ko'rsatamiz
            border.Child = new TextBlock
            {
                Text = "Rasm yo'q",
                FontSize = 7,
                Foreground = Brushes.Silver,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
        }

        return border;
    }
    private BitmapSource GetImageFromUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;

        // Agar bu lokal fayl bo'lsa (masalan C:\...)
        if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            if (File.Exists(url))
            {
                var bitmap = new BitmapImage(new Uri(url));
                bitmap.Freeze();
                return bitmap;
            }
            return null;
        }

        try
        {
            // Senior yondashuv: URL dan rasmni yuklash
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(url, UriKind.Absolute);
            // Internetdan yuklanayotganda dastur qotib qolmasligi uchun
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze(); // UI thread bloklanmasligi uchun
            return bitmap;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Rasm yuklashda xato: {ex.Message}");
            return null;
        }
    }

    // Yordamchi metod: Rasm o'rniga matn chiqarish
    private TextBlock CreateFallbackText(string text)
    {
        return new TextBlock
        {
            Text = text,
            FontSize = 7,
            Foreground = Brushes.Red,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextWrapping = TextWrapping.Wrap
        };
    }
    private void AddCellToGrid(Grid grid, string text, int row, int col, bool isHeader, TextAlignment align)
    {
        var cell = CreateCell(text, isHeader, align);
        Grid.SetRow(cell, row);
        Grid.SetColumn(cell, col);
        grid.Children.Add(cell);
    }
    private void AddRow(Grid grid, bool isHeader, int row, params string[] values)
    {
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        TextAlignment[] alignments = {
            TextAlignment.Center, TextAlignment.Center, TextAlignment.Left,
            TextAlignment.Left, TextAlignment.Center, TextAlignment.Right,
            TextAlignment.Right, TextAlignment.Right, TextAlignment.Right
        };

        for (int i = 0; i < values.Length; i++)
        {
            TextAlignment finalAlignment = isHeader ? TextAlignment.Center : alignments[i];
            var cell = CreateCell(values[i], isHeader, finalAlignment);
            Grid.SetRow(cell, row);
            Grid.SetColumn(cell, i);
            grid.Children.Add(cell);
        }
    }
    private Border CreateCell(string text, bool isHeader, TextAlignment alignment = TextAlignment.Left)
    {
        var border = new Border
        {
            BorderBrush = Brushes.Gray,
            BorderThickness = new Thickness(0.5),
            Background = isHeader ? new SolidColorBrush(Color.FromRgb(235, 235, 235)) : Brushes.White,
            Padding = new Thickness(4, 5, 4, 5)
        };

        var tb = new TextBlock
        {
            Text = text,
            FontSize = isHeader ? 13 : 12,
            FontWeight = isHeader ? FontWeights.Bold : FontWeights.Normal,
            TextAlignment = alignment,
            VerticalAlignment = VerticalAlignment.Center
        };
        border.Child = tb;
        return border;
    }
    private void SaveFixedDocumentToPdf(FixedDocument doc, string path, int dpi = 300)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
            using var pdfDoc = new PdfSharp.Pdf.PdfDocument();

            foreach (var pageContent in doc.Pages)
            {
                var fixedPage = pageContent.GetPageRoot(false);
                if (fixedPage == null) continue;

                fixedPage.Measure(new Size(fixedPage.Width, fixedPage.Height));
                fixedPage.Arrange(new Rect(0, 0, fixedPage.Width, fixedPage.Height));
                fixedPage.UpdateLayout();

                double scale = dpi / 96.0;
                var bitmap = new RenderTargetBitmap(
                    (int)(fixedPage.Width * scale),
                    (int)(fixedPage.Height * scale),
                    dpi, dpi, PixelFormats.Pbgra32);

                var dv = new DrawingVisual();
                using (var dc = dv.RenderOpen())
                {
                    dc.PushTransform(new ScaleTransform(scale, scale));
                    dc.DrawRectangle(new VisualBrush(fixedPage), null, new Rect(0, 0, fixedPage.Width, fixedPage.Height));
                }
                bitmap.Render(dv);

                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                using var ms = new MemoryStream();
                encoder.Save(ms);
                ms.Position = 0;

                var pdfPage = pdfDoc.AddPage();
                pdfPage.Width = PdfSharp.Drawing.XUnit.FromMillimeter(210);
                pdfPage.Height = PdfSharp.Drawing.XUnit.FromMillimeter(297);

                using var xgfx = PdfSharp.Drawing.XGraphics.FromPdfPage(pdfPage);
                using var ximg = PdfSharp.Drawing.XImage.FromStream(ms);

                double ratio = Math.Min(pdfPage.Width.Point / ximg.PointWidth, pdfPage.Height.Point / ximg.PointHeight);
                double w = ximg.PointWidth * ratio;
                double h = ximg.PointHeight * ratio;

                xgfx.DrawImage(ximg, (pdfPage.Width.Point - w) / 2, (pdfPage.Height.Point - h) / 2, w, h);
            }
            pdfDoc.Save(path);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"PDF saqlashda xatolik: {ex.Message}", "Xatolik", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    private void Clear()
    {
        SaleItems.Clear();
        Customer = null;
        TotalAmount = null;
        FinalAmount = null;
        Note = string.Empty;
        TotalAmountWithUserBalance = null;
        EditingSaleId = 0;
        ClearCurrentSaleItem();
    }
    private void ClearCurrentSaleItem()
    {
        CurrentSaleItem.PropertyChanged -= SaleItemPropertyChanged;
        CurrentSaleItem = new SaleItemViewModel();
        CurrentSaleItem.PropertyChanged += SaleItemPropertyChanged;
    }

    #endregion

    #region Property Changes

    partial void OnCustomerChanged(UserViewModel? value) => RecalculateTotalAmountWithUserBalance();

    private void SaleItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SaleItemViewModel.Amount))
        {
            RecalculateTotals();
        }
    }

    partial void OnFinalAmountChanged(decimal? value)
    {
        if (Customer is not null)
            TotalAmountWithUserBalance = Customer.Balance - FinalAmount;
    }

    partial void OnEditingSaleIdChanged(long value)
    {
        IsEditing = value > 0;
    }

    #endregion

    #region Private Helpers

    private void RecalculateTotals()
    {
        TotalAmount = SaleItems.Sum(x => x.Amount);
        FinalAmount = TotalAmount;
    }

    private void RecalculateTotalAmountWithUserBalance()
    {
        if (Customer is not null)
            TotalAmountWithUserBalance = Customer.Balance - TotalAmount;
    }

    #endregion

    #region Public Methods for External Use

    /// <summary>
    /// Loads sale data for editing. Ensures initialization is complete first.
    /// </summary>
    public async Task LoadSaleForEditAsync(long saleId)
    {
        // Ma'lumotlar yuklanishini kutamiz
        await EnsureInitializedAsync();

        FilteringRequest request = new()
        {
            Filters = new()
            {
                ["id"] = [saleId.ToString()],
                ["saleItems"] = ["include:productType.product"]
            }
        };

        var response = await client.Sales.Filter(request).Handle(isLoading => IsLoading = isLoading);

        if (!response.IsSuccess)
        {
            ErrorMessage = response.Message ?? "Savdoni yuklashda xatolik!";
            return;
        }

        var sale = mapper.Map<SaleViewModel>(response.Data.First());

        EditingSaleId = sale.Id;
        Date = sale.Date;
        Note = sale.Note ?? string.Empty;

        // Endi AvailableCustomers to'liq yuklangan
        var customer = AvailableCustomers.FirstOrDefault(c => c.Id == sale.CustomerId);
        if (customer is not null)
        {
            Customer = customer;
        }

        SaleItems.Clear();
        if (sale.SaleItems is not null)
        {
            foreach (var saleItem in sale.SaleItems)
            {
                var product = AvailableProducts.FirstOrDefault(p =>
                    p.Id == saleItem.ProductType?.Product?.Id);

                if (product == null && saleItem.ProductType?.Product is not null)
                {
                    product = mapper.Map<ProductViewModel>(saleItem.ProductType.Product);
                    product.ProductTypes = [];
                    AvailableProducts.Add(product);
                }

                ProductTypeViewModel? productType = null;
                if (product is not null && saleItem.ProductType is not null)
                {
                    productType = product.ProductTypes?.FirstOrDefault(pt =>
                        pt.Id == saleItem.ProductType.Id);

                    if (productType == null)
                    {
                        productType = mapper.Map<ProductTypeViewModel>(saleItem.ProductType);
                        product.ProductTypes ??= [];
                        product.ProductTypes.Add(productType);
                    }
                }

                var item = new SaleItemViewModel
                {
                    Product = product!,
                    ProductType = productType!,
                    BundleCount = saleItem.BundleCount,
                    UnitPrice = saleItem.UnitPrice,
                    Amount = saleItem.Amount,
                    TotalCount = saleItem.TotalCount
                };

                item.PropertyChanged += SaleItemPropertyChanged;
                SaleItems.Add(item);
            }
        }

        RecalculateTotals();
        SuccessMessage = "Savdo tahrirlash uchun yuklandi.";
    }

    #endregion
}