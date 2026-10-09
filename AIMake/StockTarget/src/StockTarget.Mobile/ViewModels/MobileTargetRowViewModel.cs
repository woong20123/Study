using StockTarget.Core;
using StockTarget.Mobile.Localization;

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
                RaiseNames();
        }
    }

    /// <summary>표시용 종목명 = 한국어 기기면 한글명, 아니면(또는 한글명이 없으면) 시세의 영문명.</summary>
    public string Name => (L.IsKorean ? KoreanName : null) ?? Quote?.Name ?? "";

    /// <summary>목록 제목: 종목명을 크게, 없으면 티커.</summary>
    public string Title => Name is { Length: > 0 } n ? n : Symbol;

    /// <summary>제목 아래 작은 글씨: 티커 · (목표 연도).</summary>
    public string Subtitle => (Name.Length > 0 ? Symbol + "  " : "") + L.Format("Row_TargetYear", TargetYear);

    public int TargetYear => Plan.TargetYear;
    public string TargetYearLine => L.Format("Detail_TargetYear", TargetYear);
    public string TargetPriceLine => L.Format("Detail_FinalTarget", TargetPrice);
    public string ReturnLine => L.Format("Detail_Return", ReturnPct);
    public string DividendLine => L.Format("Detail_Dividend", DividendYieldPct);
    public string GrowthLine => L.Format("Detail_Growth", GrowthPct);
    public double TargetPrice => Plan.TargetPrice;
    public double ReturnPct => Plan.ReturnPct;
    public double DividendYieldPct => Plan.DividendYieldPct;
    public double GrowthPct => Plan.GrowthPct;

    public ScheduleRow? CurrentRow => TargetCalculator.CurrentRow(Plan, DateOnly.FromDateTime(DateTime.Today));
    public double? CurrentBuyPrice => CurrentRow?.BuyPrice;
    public string CurrentQuarter => CurrentRow?.Quarter ?? L.Get("Row_OutOfRange");
    public string QuarterBuyLabel => L.Format("Row_QuarterBuy", CurrentQuarter);

    public Quote? Quote
    {
        get => _quote;
        set
        {
            if (Set(ref _quote, value))
            {
                RaiseDerived();
                RaiseNames();
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

    public string Verdict => Error is not null ? L.Get("Row_FetchFailed") : L.Verdict(Status);

    public string BuyAmountText =>
        Error is null && Amounts.For(Status) is { } krw && krw > 0
            ? Money.Text(krw, UsdKrw) + (Plan.BuyAmounts.IsDefault(Status, Defaults) ? L.Get("Row_DefaultSuffix") : "")
            : "";

    public string BuySharesText =>
        Error is null && Amounts.For(Status) is { } krw && krw > 0 && SharesText(Status, krw) is { } text ? text : "";

    public string CacheText => CacheAge is { } a
        ? (a >= 60 ? L.Format("Row_CacheMin", (int)(a / 60)) : L.Format("Row_CacheSec", (int)a))
        : L.Get("Row_CacheLive");

    public double? StagePrice(BuyStatus stage) =>
        CurrentBuyPrice is { } b ? ReservationPlanner.LimitPrice(b, stage) : null;

    public string? SharesText(BuyStatus stage, double krw)
    {
        if (Quote is { } q && q.Currency != "USD")
            return null;
        if (StagePrice(stage) is not { } price || Money.DisplayShares(krw, UsdKrw, price) is not { } n)
            return null;
        return L.Format("Row_Shares", price, n);
    }

    public void Update(TargetPlan plan)
    {
        Plan = plan;
        OnPropertyChanged(string.Empty);
    }

    private void RaiseNames()
    {
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(Subtitle));
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
