namespace Forex.Wpf.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using Forex.Wpf.Pages.Common;
using System.Globalization;

public partial class UserAccountViewModel : ViewModelBase
{
    public long Id { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal Discount { get; set; }
    public decimal Balance { get; set; }
    public string? Description { get; set; } = string.Empty;


    public long UserId { get; set; }
    public UserViewModel User { get; set; } = default!;
    public long CurrencyId { get; set; }
    public CurrencyViewModel Currency { get; set; } = default!;

    private DateTime? _dueDate;
    public DateTime? DueDate
    {
        get => _dueDate;
        set
        {
            if (_dueDate != value)
            {
                _dueDate = value;

                if (_dueDate.HasValue)
                    DueDateString = _dueDate.Value.ToString("dd.MM.yyyy");
                else
                    DueDateString = string.Empty;

                OnPropertyChanged(nameof(DueDate));
                OnPropertyChanged(nameof(DueDateString));
            }
        }
    }


    [ObservableProperty] private string dueDateString = string.Empty;

    partial void OnDueDateStringChanged(string value)
    {
        string numericText = new string(value.Where(char.IsDigit).ToArray());

        if (numericText.Length > 8)
            numericText = numericText.Substring(0, 8);

        if (numericText.Length >= 2) numericText = numericText.Insert(2, ".");
        if (numericText.Length >= 5) numericText = numericText.Insert(5, ".");

        if (value != numericText)
        {
            dueDateString = numericText;
            OnPropertyChanged(nameof(DueDateString));

            if (string.IsNullOrEmpty(numericText) || numericText.Length < 10)
            {
                _dueDate = null;
                OnPropertyChanged(nameof(DueDate));
                return;
            }
        }

        if (DateTime.TryParseExact(numericText, "dd.MM.yyyy", null, DateTimeStyles.None, out DateTime parsedDate))
        {
            if (parsedDate.Date >= DateTime.Today.Date)
            {
                _dueDate = parsedDate;
                OnPropertyChanged(nameof(DueDate));
            }
            else
            {
                _dueDate = null;
                OnPropertyChanged(nameof(DueDate));
                dueDateString = string.Empty;
                OnPropertyChanged(nameof(DueDateString));
            }
        }
        else if (string.IsNullOrWhiteSpace(value))
        {
            _dueDate = null;
            OnPropertyChanged(nameof(DueDate));
        }
        else if (numericText.Length == 10)
        {
            _dueDate = null;
            OnPropertyChanged(nameof(DueDate));
            dueDateString = string.Empty;
            OnPropertyChanged(nameof(DueDateString));
        }
    }
}