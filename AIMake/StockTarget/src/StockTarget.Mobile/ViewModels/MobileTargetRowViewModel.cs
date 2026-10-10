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

    private bool _isBuyDone;

    /// <summary>'매수 완료'로 표시했는지. 켜 두면 '매수 필요만' 필터에서 빠진다(저장은 메인 화면 뷰모델이 한다).</summary>
    public bool IsBuyDone
    {
        get => _isBuyDone;
        set => Set(ref _isBuyDone, value);
    }

    /// <summary>지금 매수 단계(1~3단계)인지.</summary>
    public bool NeedsBuy => Status is BuyStatus.Buy or BuyStatus.MustBuy or BuyStatus.StrongBuy;

    /// <summary>매수 완료 관리 화면의 둘째 줄: 티커 · 판정 · 단가 × 주식 수.</summary>
    public string BuyDoneSubtitle => string.Join(" · ",
        new[] { TickerText, Error is null ? L.Verdict(Status) : Verdict, BuySharesText }.Where(t => t.Length > 0));

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

    /// <summary>목록 카드 제목 옆 작은 티커. 종목명이 없어 제목이 티커면 비운다.</summary>
    public string TickerText => Name.Length > 0 ? Symbol : "";

    private Views.PriceGaugeDrawable? _gauge;

    /// <summary>
    /// 목록 카드의 가격 게이지(매입 목표가 대비 현재가 위치). 괴리 · 판정이 그대로면 같은 객체를 돌려줘 다시 그리지 않고,
    /// 바뀌었을 때만 새로 만들어 GraphicsView가 다시 그리게 한다.
    /// </summary>
    public IDrawable Gauge
    {
        get
        {
            var gap = Error is null ? GapPct : null;
            if (_gauge is null || _gauge.GapPct != gap || _gauge.Status != Status)
                _gauge = new Views.PriceGaugeDrawable(gap, Status);
            return _gauge;
        }
    }

    public BuyStatus Status => Error is not null ? BuyStatus.None : TargetCalculator.Classify(Price, CurrentBuyPrice);

    public string Verdict => Error is not null ? L.Get("Row_FetchFailed") : L.VerdictShort(Status);

    public string BuyAmountText =>
        Error is null && Amounts.For(Status) is { } krw && krw > 0
            ? AmountText(krw) + (Plan.BuyAmounts.IsDefault(Status, Defaults) ? L.Get("Row_DefaultSuffix") : "")
            : "";

    /// <summary>금액 표시: 한국어 "₩1,000,000 ($745.77)", 영어는 달러만 "$745.77"(환율 전이면 빈칸).</summary>
    private string AmountText(double krw) =>
        L.IsKorean ? Money.Text(krw, UsdKrw) : Money.KrwToUsd(krw, UsdKrw) is { } usd ? Money.UsdText(usd) : "";

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
        return L.Format(n == 1 ? "Row_Share1" : "Row_Shares", price, n);
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
        OnPropertyChanged(nameof(TickerText));
        OnPropertyChanged(nameof(BuyDoneSubtitle));
    }

    private void RaiseDerived()
    {
        OnPropertyChanged(nameof(Price));
        OnPropertyChanged(nameof(PriceText));
        OnPropertyChanged(nameof(BuyPriceText));
        OnPropertyChanged(nameof(GapPct));
        OnPropertyChanged(nameof(GapText));
        OnPropertyChanged(nameof(Gauge));
        OnPropertyChanged(nameof(Status));
        OnPropertyChanged(nameof(NeedsBuy));
        OnPropertyChanged(nameof(Verdict));
        OnPropertyChanged(nameof(BuyAmountText));
        OnPropertyChanged(nameof(BuySharesText));
        OnPropertyChanged(nameof(BuyDoneSubtitle));
    }
}
