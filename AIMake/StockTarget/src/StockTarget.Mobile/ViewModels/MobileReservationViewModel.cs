using System.Collections.ObjectModel;
using StockTarget.Core;

namespace StockTarget.Mobile.ViewModels;

public sealed class MobileReservationViewModel : ObservableObject
{
    private readonly StockDatabase _db;
    private readonly StockService _stockService;
    private readonly ReservationService _reservationService;

    private bool _isBusy;
    private string _status = "준비";
    private string _periodText = "";

    public ObservableCollection<ReservationOrder> Orders { get; } = [];
    public ObservableCollection<ReservationSkip> Excluded { get; } = [];

    public bool IsBusy
    {
        get => _isBusy;
        set => Set(ref _isBusy, value);
    }

    public string Status
    {
        get => _status;
        set => Set(ref _status, value);
    }

    public string PeriodText
    {
        get => _periodText;
        set => Set(ref _periodText, value);
    }

    public AsyncCommand RefreshPlanCommand { get; }

    public MobileReservationViewModel(StockDatabase db, StockService stockService, ReservationService reservationService)
    {
        _db = db;
        _stockService = stockService;
        _reservationService = reservationService;

        RefreshPlanCommand = new AsyncCommand(LoadPlanAsync);

        _ = LoadPlanAsync();
    }

    public async Task LoadPlanAsync()
    {
        IsBusy = true;
        Status = "예약 주문표 계산 중...";
        try
        {
            var plans = _db.GetTargets();
            var defaults = _db.GetDefaultAmounts();
            var usdQuote = await _stockService.GetUsdKrwAsync(force: false);
            var usdKrw = usdQuote.Value.Price;

            var (start, end) = ReservationPlanner.NextWeek(DateOnly.FromDateTime(DateTime.Today));

            var currentPrices = new Dictionary<string, double>();
            foreach (var p in plans)
            {
                try
                {
                    var q = await _stockService.GetQuoteAsync(p.Symbol, force: false);
                    currentPrices[p.Symbol] = q.Value.Price;
                }
                catch
                {
                    // 조회 실패 종목은 제외
                }
            }

            var planResult = ReservationPlanner.Plan(plans, start, end, usdKrw, defaults, currentPrices);

            PeriodText = $"예약 기간: {planResult.Start:yyyy-MM-dd} ~ {planResult.End:yyyy-MM-dd} (다음 주 월~금)";

            Orders.Clear();
            foreach (var o in planResult.Orders) Orders.Add(o);

            Excluded.Clear();
            foreach (var e in planResult.Skips) Excluded.Add(e);

            Status = $"주문 가능 {Orders.Count}건 / 제외 {Excluded.Count}건";
        }
        catch (Exception ex)
        {
            Status = $"주문표 생성 실패: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
