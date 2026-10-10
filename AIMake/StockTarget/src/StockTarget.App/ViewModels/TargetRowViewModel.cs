using StockTarget.Core;

namespace StockTarget.App.ViewModels;

/// <summary>목표 목록의 한 행: 저장된 목표 + 현재가 비교.</summary>
public sealed class TargetRowViewModel(TargetPlan plan) : ObservableObject
{
    private Quote? _quote;
    private double? _cacheAge;
    private string? _error;
    private double? _usdKrw;
    private BuyAmounts? _defaults;

    public TargetPlan Plan { get; private set; } = plan;

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
    public string InputDate => Plan.InputDate.ToString("yyyy-MM-dd");

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
                RaiseQuoteDerived();
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
            {
                OnPropertyChanged(nameof(Status));
                OnPropertyChanged(nameof(Verdict));
                OnPropertyChanged(nameof(BuyAmountText));
                OnPropertyChanged(nameof(BuySharesText));
            }
        }
    }

    /// <summary>USD/KRW 환율(1달러당 원). 매수금액 달러 환산에 쓴다.</summary>
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

    /// <summary>기본 매수금액(목표에서 비운 단계에 쓴다).</summary>
    public BuyAmounts? Defaults
    {
        get => _defaults;
        set
        {
            if (Set(ref _defaults, value))
            {
                OnPropertyChanged(nameof(BuyAmountText));
                OnPropertyChanged(nameof(BuySharesText));
            }
        }
    }

    /// <summary>실제로 쓰는 매수금액 = 목표에 입력한 값, 비운 단계는 기본 매수금액.</summary>
    public BuyAmounts Amounts => Plan.EffectiveAmounts(Defaults);

    public double? Price => Quote?.Price;

    /// <summary>현재가가 이번 분기 매입 목표가보다 몇 % 높은지(음수면 아래).</summary>
    public double? GapPct => Price is { } p && CurrentBuyPrice is { } b && b > 0 ? (p / b - 1) * 100 : null;

    public bool? BuyCondition => Price is { } p && CurrentBuyPrice is { } b ? p <= b : null;

    /// <summary>매수 3·2·1단계(−20% · −10% · ≤ 매입 목표가) / 매입 대기(3% 이내) / 대기(3% 초과). 화면에는 매수 · 대기 두 가지로 보인다.</summary>
    public BuyStatus Status => Error is not null ? BuyStatus.None : TargetCalculator.Classify(Price, CurrentBuyPrice);

    public string Verdict => Error is not null ? "조회 실패" : Status.VerdictText();

    /// <summary>지금 매수 단계(1~3단계)인지.</summary>
    public bool NeedsBuy => Status is BuyStatus.Buy or BuyStatus.MustBuy or BuyStatus.StrongBuy;

    private bool _isBuyDone;

    /// <summary>'매수 완료'로 표시했는지(저장은 MainViewModel.SetBuyDone).</summary>
    public bool IsBuyDone
    {
        get => _isBuyDone;
        set
        {
            if (Set(ref _isBuyDone, value))
                OnPropertyChanged(nameof(BuyDoneMark));
        }
    }

    /// <summary>목록의 '매수 완료' 칸.</summary>
    public string BuyDoneMark => IsBuyDone ? "✓" : "";

    /// <summary>
    /// 현재 판정 단계의 매수금액 "₩1,000,000 ($747.12)", 기본 매수금액이면 뒤에 "· 기본".
    /// 매수 단계가 아니거나 금액이 없으면(0 포함) 빈 문자열.
    /// </summary>
    public string BuyAmountText =>
        Error is null && Amounts.For(Status) is { } krw && krw > 0
            ? Money.Text(krw, UsdKrw) + (Plan.BuyAmounts.IsDefault(Status, Defaults) ? " · 기본" : "")
            : "";

    /// <summary>현재 판정 단계의 매수단가와 살 주식 수 "222.00 × 3주". 매수 단계가 아니거나 계산할 수 없으면 빈 문자열.</summary>
    public string BuySharesText =>
        Error is null && Amounts.For(Status) is { } krw && krw > 0 && SharesText(Status, krw) is { } text ? text : "";

    /// <summary>매수 단계의 매수단가 = 이번 분기 매입 목표가 × (1 − 단계 할인율), LOC 주문가와 같다.</summary>
    public double? StagePrice(BuyStatus stage) =>
        CurrentBuyPrice is { } b ? ReservationPlanner.LimitPrice(b, stage) : null;

    /// <summary>
    /// 단계 금액(원)으로 그 단계 매수단가에 살 수 있는 주식 수(내림, 최소 1주) "222.00 × 3주".
    /// 달러 종목이 아니거나(시세 조회 전에는 달러로 본다) 환율 · 매입 목표가가 없으면 null.
    /// </summary>
    public string? SharesText(BuyStatus stage, double krw)
    {
        if (Quote is { } q && q.Currency != "USD")
            return null;
        if (StagePrice(stage) is not { } price || Money.DisplayShares(krw, UsdKrw, price) is not { } n)
            return null;
        return $"{price:N2} × {n:N0}주";
    }

    public string CacheText => CacheAge is { } a ? $"캐시 {Format.Age(a)}" : "실시간";

    public void Update(TargetPlan plan)
    {
        Plan = plan;
        OnPropertyChanged(string.Empty); // 모든 속성 갱신
    }

    private void RaiseQuoteDerived()
    {
        OnPropertyChanged(nameof(Price));
        OnPropertyChanged(nameof(GapPct));
        OnPropertyChanged(nameof(BuyCondition));
        OnPropertyChanged(nameof(Status));
        OnPropertyChanged(nameof(NeedsBuy));
        OnPropertyChanged(nameof(Verdict));
        OnPropertyChanged(nameof(BuyAmountText));
        OnPropertyChanged(nameof(BuySharesText));
    }
}

public static class Format
{
    public static string Age(double seconds) => seconds >= 60 ? $"{(int)(seconds / 60)}분 전" : $"{(int)seconds}초 전";
}
