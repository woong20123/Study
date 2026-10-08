using System.Collections.ObjectModel;
using StockTarget.Core;
using StockTarget.Mobile.Views;

namespace StockTarget.Mobile.ViewModels;

public sealed class MobileMainViewModel : ObservableObject
{
    private readonly StockDatabase _db;
    private readonly StockService _stockService;

    private bool _isBusy;
    private bool _isRefreshing;
    private string _status = "준비";
    private double? _usdKrw;
    private BuyAmounts _defaults = BuyAmounts.Empty;

    public ObservableCollection<MobileTargetRowViewModel> Targets { get; } = [];

    public bool IsBusy
    {
        get => _isBusy;
        set => Set(ref _isBusy, value);
    }

    public bool IsRefreshing
    {
        get => _isRefreshing;
        set => Set(ref _isRefreshing, value);
    }

    public string Status
    {
        get => _status;
        set => Set(ref _status, value);
    }

    public double? UsdKrw
    {
        get => _usdKrw;
        private set
        {
            if (Set(ref _usdKrw, value))
            {
                OnPropertyChanged(nameof(UsdKrwText));
                foreach (var row in Targets)
                    row.UsdKrw = value;
            }
        }
    }

    public string UsdKrwText => UsdKrw is { } r ? $"환율: ₩{r:N2}/$" : "환율: —";

    public AsyncCommand RefreshCommand { get; }
    public AsyncCommand AddTargetCommand { get; }
    public AsyncCommand<MobileTargetRowViewModel> SelectTargetCommand { get; }
    public AsyncCommand<MobileTargetRowViewModel> DeleteTargetCommand { get; }

    public MobileMainViewModel(StockDatabase db, StockService stockService)
    {
        _db = db;
        _stockService = stockService;

        RefreshCommand = new AsyncCommand(RefreshAsync);
        AddTargetCommand = new AsyncCommand(NavigateToAddAsync);
        SelectTargetCommand = new AsyncCommand<MobileTargetRowViewModel>(NavigateToDetailAsync);
        DeleteTargetCommand = new AsyncCommand<MobileTargetRowViewModel>(DeleteAsync);

        _ = LoadInitialDataAsync();
    }

    public async Task LoadInitialDataAsync()
    {
        IsBusy = true;
        Status = "데이터 로딩 중...";
        try
        {
            _defaults = _db.GetDefaultAmounts();
            var plans = _db.GetTargets();
            Targets.Clear();
            foreach (var plan in plans)
            {
                Targets.Add(new MobileTargetRowViewModel(plan)
                {
                    Defaults = _defaults,
                    UsdKrw = UsdKrw,
                    SelectCommand = SelectTargetCommand
                });
            }

            await RefreshQuotesAsync(force: false);
            Status = $"총 {Targets.Count}개 목표 종목";
        }
        catch (Exception ex)
        {
            Status = $"로딩 실패: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RefreshAsync()
    {
        IsRefreshing = true;
        try
        {
            await RefreshQuotesAsync(force: true);
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    public async Task RefreshQuotesAsync(bool force)
    {
        try
        {
            var usdQuote = await _stockService.GetUsdKrwAsync(force);
            UsdKrw = usdQuote.Value.Price;
        }
        catch
        {
            // 환율 조회 실패 시 기존 값 유지
        }

        var tasks = Targets.Select(async row =>
        {
            try
            {
                var cached = await _stockService.GetQuoteAsync(row.Symbol, force);
                row.Quote = cached.Value;
                row.CacheAge = cached.CacheAgeSeconds;
                row.Error = null;

                _stockService.RecordCheck(row.Plan, cached.Value);
            }
            catch (Exception ex)
            {
                row.Error = ex.Message;
            }
        });

        await Task.WhenAll(tasks);
    }

    private async Task NavigateToAddAsync()
    {
        await Shell.Current.GoToAsync(nameof(TargetEditPage));
    }

    private async Task NavigateToDetailAsync(MobileTargetRowViewModel? row)
    {
        if (row is null) return;
        var navigationParameter = new Dictionary<string, object>
        {
            { "TargetRow", row }
        };
        await Shell.Current.GoToAsync(nameof(TargetDetailPage), navigationParameter);
    }

    private async Task DeleteAsync(MobileTargetRowViewModel? row)
    {
        if (row is null) return;
        bool confirm = await Shell.Current.DisplayAlert("목표 삭제", $"{row.Symbol} 목표를 삭제하시겠습니까?", "삭제", "취소");
        if (!confirm) return;

        _db.DeleteTarget(row.Symbol);
        Targets.Remove(row);
        Status = $"{row.Symbol} 삭제됨";
    }
}
