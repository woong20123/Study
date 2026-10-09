using StockTarget.Core;

namespace StockTarget.Mobile.ViewModels;

public sealed class MobileTargetRowViewModel(TargetPlan plan) : ObservableObject
{
    private Quote? _quote;
    private double? _cacheAge;
    private string? _error;
    private double? _usdKrw;
    private BuyAmounts? _defaults;

    public TargetPlan Plan { get; private set; } = plan;
    public System.Windows.Input.ICommand? SelectCommand { get; set; }

    public string Symbol => Plan.Symbol;

    private string? _koreanName;

    /// <summary>네이버에서 받은 한글 종목명. 없으면 null.</summary>
    public string? KoreanName
    {
        get => _koreanName;
        set
        {
            if (Set(ref _koreanName, value))
                OnPropertyChanged(nameof(Name));
        }
    }

    /// <summary>표시용 종목명 = 한글명, 없으면 시세의 영문명.</summary>
    public string Name => KoreanName ?? Quote?.Name ?? "";
    public int TargetYear => Plan.TargetYear;
    public double TargetPrice => Plan.TargetPrice;
    public double ReturnPct => Plan.ReturnPct;
    public double DividendYieldPct => Plan.DividendYieldPct;
    public double GrowthPct => Plan.GrowthPct;

    public ScheduleRow? CurrentRow => TargetCalculator.CurrentRow(Plan, DateOnly.FromDateTime(DateTime.Today));
    public double? CurrentBuyPrice => CurrentRow?.BuyPrice;
    public string CurrentQuarter => CurrentRow?.Quarter ?? "범위 밖";

    public Quote? Quote
    {
        get => _quote;
        set
        {
            if (Set(ref _quote, value))
            {
                RaiseDerived();
                OnPropertyChanged(nameof(Name));
            }
        }
    }

    public double? CacheAge
    {
        get => _cacheAge;
        set
        {
            if (Set(ref _cacheAge, value))
                OnPropertyChanged(nameof(CacheText));
        }
    }

    public string? Error
    {
        get => _error;
        set
        {
            if (Set(ref _error, value))
                RaiseDerived();
        }
    }

    public double? UsdKrw
    {
        get => _usdKrw;
        set
        {
            if (Set(ref _usdKrw, value))
            {
                OnPropertyChanged(nameof(BuyAmountText));
                OnPropertyChanged(nameof(BuySharesText));
            }
        }
    }

    public BuyAmounts? Defaults
    {
        get => _defaults;
        set
        {
            if (Set(ref _defaults, value))
            {
                OnPropertyChanged(nameof(Amounts));
                OnPropertyChanged(nameof(BuyAmountText));
                OnPropertyChanged(nameof(BuySharesText));
            }
        }
    }

    public BuyAmounts Amounts => Plan.EffectiveAmounts(Defaults);

    public double? Price => Quote?.Price;

    public double? GapPct => Price is { } p && CurrentBuyPrice is { } b && b > 0 ? (p / b - 1) * 100 : null;

    public string PriceText => Price is { } p ? $"${p:N2}" : "—";
    public string BuyPriceText => CurrentBuyPrice is { } b ? $"${b:N2}" : "—";

    public string GapText => GapPct is { } g ? $"{(g >= 0 ? "+" : "")}{g:N1}%" : "";

    public BuyStatus Status => Error is not null ? BuyStatus.None : TargetCalculator.Classify(Price, CurrentBuyPrice);

    public string Verdict => Error is not null ? "조회 실패" : Status.ToText();

    public string BuyAmountText =>
        Error is null && Amounts.For(Status) is { } krw && krw > 0
            ? Money.Text(krw, UsdKrw) + (Plan.BuyAmounts.IsDefault(Status, Defaults) ? " · 기본" : "")
            : "";

    public string BuySharesText =>
        Error is null && Amounts.For(Status) is { } krw && krw > 0 && SharesText(Status, krw) is { } text ? text : "";

    public string CacheText => CacheAge is { } a ? (a >= 60 ? $"{(int)(a / 60)}분 전" : $"{(int)a}초 전") : "실시간";

    public double? StagePrice(BuyStatus stage) =>
        CurrentBuyPrice is { } b ? ReservationPlanner.LimitPrice(b, stage) : null;

    public string? SharesText(BuyStatus stage, double krw)
    {
        if (Quote is { } q && q.Currency != "USD")
            return null;
        if (StagePrice(stage) is not { } price || Money.Shares(krw, UsdKrw, price) is not { } n)
            return null;
        return $"{price:N2} × {n:N0}주";
    }

    public void Update(TargetPlan plan)
    {
        Plan = plan;
        OnPropertyChanged(string.Empty);
    }

    private void RaiseDerived()
    {
        OnPropertyChanged(nameof(Price));
        OnPropertyChanged(nameof(PriceText));
        OnPropertyChanged(nameof(BuyPriceText));
        OnPropertyChanged(nameof(GapPct));
        OnPropertyChanged(nameof(GapText));
        OnPropertyChanged(nameof(Status));
        OnPropertyChanged(nameof(Verdict));
        OnPropertyChanged(nameof(BuyAmountText));
        OnPropertyChanged(nameof(BuySharesText));
    }
}
