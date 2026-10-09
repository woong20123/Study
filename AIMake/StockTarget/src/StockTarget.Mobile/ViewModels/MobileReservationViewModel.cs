using System.Collections.ObjectModel;
using StockTarget.Core;
using StockTarget.Mobile.Localization;

namespace StockTarget.Mobile.ViewModels;

/// <summary>주문표 한 행. 종목명을 크게, 없으면 티커.</summary>
public sealed record MobileOrderRow(ReservationOrder Order, string Name)
{
    public string Symbol => Order.Symbol;
    public string Title => Name.Length > 0 ? Name : Symbol;
    public string Subtitle => (Name.Length > 0 ? Symbol + " · " : "") + L.Verdict(Order.Stage);
    public double Price => Order.Price;
    public string QuantityText => L.Format("Res_Qty", Order.Quantity);
    public double AmountKrw => Order.AmountKrw;

    /// <summary>단계 금액: 한국어는 원화, 영어는 달러만.</summary>
    public string AmountText => L.IsKorean ? $"₩{Order.AmountKrw:N0}" : Money.UsdText(Order.AmountUsd);
}

/// <summary>제외 목록 한 행. 종목명을 크게, 없으면 티커.</summary>
public sealed record MobileSkipRow(string Symbol, string Name, string Reason, bool BuyDone)
{
    public string Title => Name.Length > 0 ? Name : Symbol;
    public string Subtitle => Name.Length > 0 ? Symbol : "";
}

public sealed class MobileReservationViewModel : ObservableObject
{
    private readonly StockDatabase _db;
    private readonly StockService _stockService;

    private bool _isBusy;
    private string _status = L.Get("Status_Ready");
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

    public MobileReservationViewModel(StockDatabase db, StockService stockService)
    {
        _db = db;
        _stockService = stockService;

        RefreshPlanCommand = new AsyncCommand(LoadPlanAsync);
        MarkBuyDoneCommand = new AsyncCommand<string>(s => SetBuyDoneAsync(s, true));
        UnmarkBuyDoneCommand = new AsyncCommand<string>(s => SetBuyDoneAsync(s, false));

        _ = LoadPlanAsync();
    }

    public async Task LoadPlanAsync()
    {
        IsBusy = true;
        Status = L.Get("Res_Calculating");
        try
        {
            var plans = _db.GetTargets();
            var defaults = _db.GetDefaultAmounts();
            var usdQuote = await _stockService.GetUsdKrwAsync(force: false);
            var usdKrw = usdQuote.Value.Price;

            var (start, end) = ReservationPlanner.NextWeek(DateOnly.FromDateTime(DateTime.Today));

            var currentPrices = new Dictionary<string, double>();
            // 종목명: 한국어 기기면 캐시한 한글명, 아니면 시세의 영문명
            var names = L.IsKorean ? new Dictionary<string, string>(_db.GetStockNames()) : new Dictionary<string, string>();
            foreach (var p in plans)
            {
                try
                {
                    var q = await _stockService.GetQuoteAsync(p.Symbol, force: false);
                    currentPrices[p.Symbol] = q.Value.Price;
                    if (q.Value.Name is { Length: > 0 } en)
                        names.TryAdd(p.Symbol, en);
                }
                catch
                {
                    // 조회 실패 종목은 제외
                }
            }

            var planResult = ReservationPlanner.Plan(plans, start, end, usdKrw, defaults, currentPrices, _db.GetBuyDone());

            PeriodText = L.Format("Res_Period", planResult.Start, planResult.End);

            Orders.Clear();
            foreach (var o in planResult.Orders) Orders.Add(new MobileOrderRow(o, names.GetValueOrDefault(o.Symbol, "")));

            Excluded.Clear();
            foreach (var e in planResult.Skips)
                Excluded.Add(new MobileSkipRow(e.Symbol, names.GetValueOrDefault(e.Symbol, ""),
                    e.BuyDone ? L.Get("Res_BuyDoneReason") : e.Reason, e.BuyDone));

            Status = L.Format("Res_Summary", Orders.Count, Excluded.Count);
        }
        catch (Exception ex)
        {
            Status = L.Format("Res_Failed", ex.Message);
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
            ? L.Format("Res_MarkedDone", symbol)
            : L.Format("Res_Unmarked", symbol);
    }
}
