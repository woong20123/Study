namespace StockTarget.Core;

/// <summary>일별 종가 한 개. Close는 분할 반영(현재 주식 수 기준), 배당 미반영 종가다.</summary>
public sealed record PriceBar(DateOnly Date, double Close);

/// <summary>배당 지급 한 건. Amount는 분할 반영(현재 주식 수 기준) 금액이다.</summary>
public sealed record DividendEvent(DateOnly Date, double Amount);

/// <summary>주식 분할 한 건. Ratio = 새 주식 수 ÷ 기존 주식 수 (10:1 분할이면 10, 1:10 역분할이면 0.1).</summary>
public sealed record SplitEvent(DateOnly Date, double Ratio)
{
    /// <summary>"10:1", "1:10 (역분할)" 형식 표기.</summary>
    public string RatioText => Ratio >= 1 ? $"{Ratio:0.###}:1" : $"1:{1 / Ratio:0.###} (역분할)";
}

/// <summary>Yahoo chart API 한 번 조회 결과(가격 이력 + 배당 + 분할).</summary>
public sealed record ChartData(
    string Symbol,
    string Currency,
    string Exchange,
    string Name,
    double? RegularMarketPrice,
    double? PreviousClose,
    IReadOnlyList<PriceBar> Bars,
    IReadOnlyList<DividendEvent> Dividends,
    IReadOnlyList<SplitEvent> Splits);

/// <summary>현재 시세.</summary>
public sealed record Quote(
    string Symbol,
    string Name,
    double Price,
    double? PreviousClose,
    string Currency,
    string Exchange,
    string? SplitNote)
{
    public double? Change => PreviousClose is { } p && p != 0 ? Price - p : null;
    public double? ChangePct => PreviousClose is { } p && p != 0 ? (Price / p - 1) * 100 : null;
}

/// <summary>배당수익률 1년 구간.</summary>
public sealed record DividendPeriod(DateOnly From, DateOnly To, int Count, double Dividend, double AvgPrice)
{
    public double? YieldPct => AvgPrice > 0 ? Dividend / AvgPrice * 100 : null;
}

/// <summary>최근 N년 배당수익률 결과.</summary>
public sealed record DividendYieldResult(
    string Symbol,
    IReadOnlyList<DividendPeriod> Periods,
    IReadOnlyList<SplitEvent> Splits)
{
    public int Years => Periods.Count;

    /// <summary>구간별 수익률의 평균(%).</summary>
    public double AvgYieldPct
    {
        get
        {
            var ys = Periods.Where(p => p.YieldPct.HasValue).Select(p => p.YieldPct!.Value).ToList();
            return ys.Count == 0 ? 0 : ys.Average();
        }
    }

    public double AvgDividend => Periods.Count == 0 ? 0 : Periods.Average(p => p.Dividend);
    public double AvgPrice => Periods.Count == 0 ? 0 : Periods.Average(p => p.AvgPrice);
}

/// <summary>
/// 저장되는 목표. 배당수익률·필요 상승률은 입력 시점 값으로 고정한다.
/// 목표 주가는 EPS × PER로 계산하거나, TargetPriceInput으로 직접 입력한다(이때 EPS·PER은 0).
/// </summary>
public sealed record TargetPlan(
    string Symbol,
    DateOnly InputDate,
    int TargetYear,
    double Eps,
    double Per,
    double ReturnPct,
    double DividendYieldPct,
    string? Memo = null,
    BuyAmounts? Amounts = null,
    double? TargetPriceInput = null)
{
    /// <summary>목표 주가를 EPS × PER 대신 직접 입력했는지.</summary>
    public bool IsDirectTargetPrice => TargetPriceInput.HasValue;

    /// <summary>매수 단계별 매수금액(원). 입력하지 않았으면 모든 단계가 비어 있다.</summary>
    public BuyAmounts BuyAmounts => Amounts ?? BuyAmounts.Empty;

    /// <summary>실제로 쓰는 매수금액 = 목표에 입력한 값, 비운 단계는 기본 매수금액.</summary>
    public BuyAmounts EffectiveAmounts(BuyAmounts? defaults) => BuyAmounts.WithDefaults(defaults);

    /// <summary>목표일 = 목표 연도 12월 31일.</summary>
    public DateOnly TargetDate => new(TargetYear, 12, 31);

    /// <summary>목표 주가 = 직접 입력값, 없으면 EPS × PER.</summary>
    public double TargetPrice => TargetPriceInput ?? Eps * Per;

    /// <summary>필요 주가 상승률(연 %) = 목표 수익률 − 5년 평균 배당수익률.</summary>
    public double GrowthPct => ReturnPct - DividendYieldPct;
}

/// <summary>매수 단계(매수 · 필수매수 · 강력매수)별 매수금액(원). 비운 단계는 null.</summary>
public sealed record BuyAmounts(double? BuyKrw = null, double? MustBuyKrw = null, double? StrongBuyKrw = null)
{
    public static readonly BuyAmounts Empty = new();

    public bool IsEmpty => BuyKrw is null && MustBuyKrw is null && StrongBuyKrw is null;

    /// <summary>비운 단계를 기본 매수금액으로 채운다(0은 '이 단계는 사지 않음'이라 기본값으로 바꾸지 않는다).</summary>
    public BuyAmounts WithDefaults(BuyAmounts? defaults) => defaults is null || defaults.IsEmpty
        ? this
        : new(BuyKrw ?? defaults.BuyKrw, MustBuyKrw ?? defaults.MustBuyKrw, StrongBuyKrw ?? defaults.StrongBuyKrw);

    /// <summary>기본 매수금액과 같은 단계는 비운다(목표에는 기본값과 다른 단계만 저장한다).</summary>
    public BuyAmounts ExceptDefaults(BuyAmounts? defaults) => defaults is null
        ? this
        : new(BuyKrw == defaults.BuyKrw ? null : BuyKrw,
              MustBuyKrw == defaults.MustBuyKrw ? null : MustBuyKrw,
              StrongBuyKrw == defaults.StrongBuyKrw ? null : StrongBuyKrw);

    /// <summary>단계 금액이 목표에 직접 입력한 값이 아니라 기본 매수금액에서 온 것인지.</summary>
    public bool IsDefault(BuyStatus s, BuyAmounts? defaults) => For(s) is null && defaults?.For(s) is not null;

    /// <summary>판정 단계에 해당하는 매수금액. 매수 단계가 아니거나(매입 대기·대기) 비어 있으면 null.</summary>
    public double? For(BuyStatus s) => s switch
    {
        BuyStatus.StrongBuy => StrongBuyKrw,
        BuyStatus.MustBuy => MustBuyKrw,
        BuyStatus.Buy => BuyKrw,
        _ => null,
    };
}

/// <summary>분기별 매입 목표가 한 행.</summary>
public sealed record ScheduleRow(string Quarter, DateOnly BaseDate, double BuyPrice);

/// <summary>현재가와 이번 분기 매입 목표가 비교 판정.</summary>
public enum BuyStatus
{
    /// <summary>비교할 값이 없다(조회 실패, 일정 범위 밖).</summary>
    None,
    /// <summary>현재가 ≤ 매입 목표가 × (1 − 20%).</summary>
    StrongBuy,
    /// <summary>현재가 ≤ 매입 목표가 × (1 − 10%).</summary>
    MustBuy,
    /// <summary>현재가 ≤ 매입 목표가.</summary>
    Buy,
    /// <summary>매입 목표가 &lt; 현재가 ≤ 매입 목표가 × (1 + 3%).</summary>
    Near,
    /// <summary>현재가 &gt; 매입 목표가 × (1 + 3%).</summary>
    Wait,
}

public static class BuyStatusText
{
    /// <summary>단계 이름(금액 칸 · 예약 주문표 · 상세). 매수 강도는 1~3단계, 매입 대기는 대기에 합친다.</summary>
    public static string ToText(this BuyStatus s) => s switch
    {
        BuyStatus.StrongBuy => "매수 3단계",
        BuyStatus.MustBuy => "매수 2단계",
        BuyStatus.Buy => "매수 1단계",
        BuyStatus.Near => "대기",
        BuyStatus.Wait => "대기",
        _ => "-",
    };

    /// <summary>매수 강도. 강력매수 3 · 필수매수 2 · 매수 1 · 그 밖(매입 대기 · 대기 · 없음) 0.</summary>
    public static int BuyLevel(this BuyStatus s) => s switch
    {
        BuyStatus.StrongBuy => 3,
        BuyStatus.MustBuy => 2,
        BuyStatus.Buy => 1,
        _ => 0,
    };

    /// <summary>
    /// 목록에 보이는 판정 = 없음 · 매수 · 대기 세 가지. 매수 단계(1~3)는 Buy로, 매입 대기는 Wait로 합친다(강도는 표식 · 색으로).
    /// 화면별 문구(윈도우 <see cref="VerdictText"/>, 모바일 리소스)는 모두 이 결과를 쓴다.
    /// </summary>
    public static BuyStatus Verdict(this BuyStatus s) => s switch
    {
        BuyStatus.None => BuyStatus.None,
        _ when s.BuyLevel() > 0 => BuyStatus.Buy,
        _ => BuyStatus.Wait,
    };

    /// <summary>목록 판정 문구: "매수" · "대기" · "-".</summary>
    public static string VerdictText(this BuyStatus s) => s.Verdict() switch
    {
        BuyStatus.Buy => "매수",
        BuyStatus.Wait => "대기",
        _ => "-",
    };
}
