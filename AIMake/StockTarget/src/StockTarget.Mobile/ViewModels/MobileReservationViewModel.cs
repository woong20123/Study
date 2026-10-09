using System.Collections.ObjectModel;
using StockTarget.Core;

namespace StockTarget.Mobile.ViewModels;

/// <summary>주문표 한 행(종목명 표시용).</summary>
public sealed record MobileOrderRow(ReservationOrder Order, string Name)
{
    public string Symbol => Order.Symbol;
    public string StageText => Order.StageText;
    public double Price => Order.Price;
    public int Quantity => Order.Quantity;
    public double AmountKrw => Order.AmountKrw;
}

/// <summary>제외 목록 한 행(종목명 표시용).</summary>
public sealed record MobileSkipRow(string Symbol, string Name, string Reason, bool BuyDone);

public sealed class MobileReservationViewModel : ObservableObject
{
    private readonly StockDatabase _db;
    private readonly StockService _stockService;
    private readonly ReservationService _reservationService;

    private bool _isBusy;
    private string _status = "준비";
    private string _periodText = "";

    public ObservableCollection<MobileOrderRow> Orders { get; } = [];
    public ObservableCollection<MobileSkipRow> Excluded { get; } = [];

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

    /// <summary>티커(CommandParameter)를 '매수 완료'로 표시해 해제할 때까지 주문표에서 뺀다.</summary>
    public AsyncCommand<string> MarkBuyDoneCommand { get; }

    /// <summary>'매수 완료' 표시를 해제해 다시 주문표에 넣는다.</summary>
    public AsyncCommand<string> UnmarkBuyDoneCommand { get; }

    public MobileReservationViewModel(StockDatabase db, StockService stockService, ReservationService reservationService)
    {
        _db = db;
        _stockService = stockService;
        _reservationService = reservationService;

        RefreshPlanCommand = new AsyncCommand(LoadPlanAsync);
        MarkBuyDoneCommand = new AsyncCommand<string>(s => SetBuyDoneAsync(s, true));
        UnmarkBuyDoneCommand = new AsyncCommand<string>(s => SetBuyDoneAsync(s, false));

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

            var planResult = ReservationPlanner.Plan(plans, start, end, usdKrw, defaults, currentPrices, _db.GetBuyDone());

            PeriodText = $"예약 기간: {planResult.Start:yyyy-MM-dd} ~ {planResult.End:yyyy-MM-dd} (다음 주 월~금)";

            var names = _db.GetStockNames();
            Orders.Clear();
            foreach (var o in planResult.Orders) Orders.Add(new MobileOrderRow(o, names.GetValueOrDefault(o.Symbol, "")));

            Excluded.Clear();
            foreach (var e in planResult.Skips)
                Excluded.Add(new MobileSkipRow(e.Symbol, names.GetValueOrDefault(e.Symbol, ""), e.Reason, e.BuyDone));

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

    private async Task SetBuyDoneAsync(string? symbol, bool done)
    {
        if (string.IsNullOrEmpty(symbol))
            return;
        _db.SetBuyDone(symbol, done);
        await LoadPlanAsync();
        Status = done
            ? $"{symbol} 매수 완료 — 해제할 때까지 주문표에서 제외"
            : $"{symbol} 매수 완료 해제 — 다시 주문표에 넣음";
    }
}
