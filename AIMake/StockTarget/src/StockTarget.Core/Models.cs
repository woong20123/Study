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

/// <summary>저장되는 목표. 배당수익률·필요 상승률은 입력 시점 값으로 고정한다.</summary>
public sealed record TargetPlan(
    string Symbol,
    DateOnly InputDate,
    int TargetYear,
    double Eps,
    double Per,
    double ReturnPct,
    double DividendYieldPct,
    string? Memo = null)
{
    /// <summary>목표일 = 목표 연도 12월 31일.</summary>
    public DateOnly TargetDate => new(TargetYear, 12, 31);

    /// <summary>목표 주가 = EPS × PER.</summary>
    public double TargetPrice => Eps * Per;

    /// <summary>필요 주가 상승률(연 %) = 목표 수익률 − 5년 평균 배당수익률.</summary>
    public double GrowthPct => ReturnPct - DividendYieldPct;
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
    /// <summary>매입 목표가 &lt; 현재가 ≤ 매입 목표가 × (1 + 5%).</summary>
    Near,
    /// <summary>현재가 &gt; 매입 목표가 × (1 + 5%).</summary>
    Wait,
}

public static class BuyStatusText
{
    public static string ToText(this BuyStatus s) => s switch
    {
        BuyStatus.StrongBuy => "강력매수",
        BuyStatus.MustBuy => "필수매수",
        BuyStatus.Buy => "매수",
        BuyStatus.Near => "매입 대기",
        BuyStatus.Wait => "대기",
        _ => "-",
    };
}
