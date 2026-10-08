namespace StockTarget.Core;

/// <summary>
/// 목표 주가 역산 → 분기별 매입 목표가.
/// <code>
/// 목표 주가          = EPS × PER (목표 연도 12/31 기준)
/// 필요 주가 상승률 g = 목표 수익률 − 최근 5년 평균 배당수익률
/// 매입 목표가(기준일) = 목표 주가 ÷ (1 + g)^(기준일 → 목표일 남은 년수)
/// </code>
/// 매입 목표가 이하로 사면 '주가 상승 + 배당'으로 목표 수익률을 기대할 수 있다.
/// </summary>
public static class TargetCalculator
{
    public const double DaysPerYear = 365.25;

    public static int QuarterOf(DateOnly d) => (d.Month - 1) / 3 + 1;

    public static string QuarterLabel(DateOnly d) => $"{d.Year}Q{QuarterOf(d)}";

    /// <summary>입력일이 속한 분기부터 목표일이 속한 분기까지. 첫 분기 기준일은 입력일, 이후는 분기 첫날.</summary>
    public static IReadOnlyList<(string Quarter, DateOnly BaseDate)> Quarters(DateOnly inputDate, DateOnly targetDate)
    {
        var rows = new List<(string, DateOnly)>();
        int y = inputDate.Year, q = QuarterOf(inputDate);
        while (true)
        {
            var start = new DateOnly(y, 3 * (q - 1) + 1, 1);
            if (start > targetDate)
                break;
            rows.Add(($"{y}Q{q}", start < inputDate ? inputDate : start));
            (y, q) = q == 4 ? (y + 1, 1) : (y, q + 1);
        }
        return rows;
    }

    /// <summary>기준일에 사서 목표일에 targetPrice가 되려면 허용되는 최대 매입가.</summary>
    public static double BuyPrice(double targetPrice, double growthPct, DateOnly on, DateOnly targetDate)
    {
        var yearsLeft = Math.Max(targetDate.DayNumber - on.DayNumber, 0) / DaysPerYear;
        return targetPrice / Math.Pow(1 + growthPct / 100, yearsLeft);
    }

    public static IReadOnlyList<ScheduleRow> Schedule(TargetPlan plan) =>
        Quarters(plan.InputDate, plan.TargetDate)
            .Select(x => new ScheduleRow(x.Quarter, x.BaseDate, BuyPrice(plan.TargetPrice, plan.GrowthPct, x.BaseDate, plan.TargetDate)))
            .ToList();

    /// <summary>today가 속한 분기의 행. 일정 범위 밖이면 null.</summary>
    public static ScheduleRow? CurrentRow(TargetPlan plan, DateOnly today) =>
        Schedule(plan).FirstOrDefault(r => r.Quarter == QuarterLabel(today));

    /// <summary>매입 대기로 보는 범위: 매입 목표가 위로 이 비율(%)까지.</summary>
    public const double NearPct = 5.0;

    /// <summary>필수매수: 매입 목표가보다 이 비율(%) 이상 낮을 때.</summary>
    public const double MustBuyPct = 10.0;

    /// <summary>강력매수: 매입 목표가보다 이 비율(%) 이상 낮을 때.</summary>
    public const double StrongBuyPct = 20.0;

    /// <summary>
    /// 판정 (현재가 vs 매입 목표가 b, 경계값은 해당 단계에 포함)
    /// <code>
    /// 현재가 ≤ b × 0.80  → 강력매수
    /// 현재가 ≤ b × 0.90  → 필수매수
    /// 현재가 ≤ b         → 매수
    /// 현재가 ≤ b × 1.05  → 매입 대기
    /// 그 위              → 대기
    /// </code>
    /// </summary>
    public static BuyStatus Classify(double? price, double? buyPrice)
    {
        if (price is not { } p || buyPrice is not { } b || b <= 0)
            return BuyStatus.None;
        if (p <= b * (1 - StrongBuyPct / 100))
            return BuyStatus.StrongBuy;
        if (p <= b * (1 - MustBuyPct / 100))
            return BuyStatus.MustBuy;
        if (p <= b)
            return BuyStatus.Buy;
        return p <= b * (1 + NearPct / 100) ? BuyStatus.Near : BuyStatus.Wait;
    }

    public static void Validate(TargetPlan plan)
    {
        if (string.IsNullOrWhiteSpace(plan.Symbol)) throw new ArgumentException("티커를 입력하세요");
        if (plan.Eps <= 0) throw new ArgumentException("EPS는 0보다 커야 합니다");
        if (plan.Per <= 0) throw new ArgumentException("PER은 0보다 커야 합니다");
        if (plan.TargetDate <= plan.InputDate) throw new ArgumentException($"목표 연도({plan.TargetYear})는 입력일 이후여야 합니다");
    }
}

/// <summary>
/// 최근 N년 배당수익률 = 1년 구간별 (배당금 합계 ÷ 구간 일별 종가 평균)의 평균.
/// 구간은 마지막 거래일부터 1년씩 거슬러 올라간다(달력 연도가 아니므로 진행 중인 올해 때문에 왜곡되지 않는다).
/// 종가·배당 모두 분할 반영(현재 주식 수 기준) 값이라 분할이 있어도 수익률은 왜곡되지 않는다.
/// </summary>
public static class DividendYieldCalculator
{
    public static DividendYieldResult Calculate(ChartData chart, int years = 5)
    {
        var periods = new List<DividendPeriod>();
        if (chart.Bars.Count == 0)
            return new DividendYieldResult(chart.Symbol, periods, []);

        var end = chart.Bars.Max(b => b.Date);
        for (int i = 0; i < years; i++)
        {
            var hi = end.AddYears(-i);
            var lo = end.AddYears(-(i + 1));
            var bars = chart.Bars.Where(b => b.Date > lo && b.Date <= hi).ToList();
            if (bars.Count == 0)
                break;
            var divs = chart.Dividends.Where(d => d.Date > lo && d.Date <= hi).ToList();
            periods.Add(new DividendPeriod(lo.AddDays(1), hi, divs.Count, divs.Sum(d => d.Amount), bars.Average(b => b.Close)));
        }

        var start = end.AddYears(-years);
        var splits = chart.Splits.Where(s => s.Date > start && s.Date <= end).OrderBy(s => s.Date).ToList();
        return new DividendYieldResult(chart.Symbol, periods, splits);
    }
}

/// <summary>
/// 분할 직후 전일 종가가 분할 전 기준으로 남아 있으면 분할 비율로 나눠 현재 주식 수 기준으로 맞춘다.
/// 최근 RecentDays 일 안의 분할이 있고, 보정 후 변동률이 보정 전보다 0에 가까울 때만 보정한다
/// (이미 보정된 값을 또 나누면 오히려 멀어지므로 건드리지 않는다).
/// </summary>
public static class SplitAdjuster
{
    public const double CheckChangePct = 30.0;
    public const int RecentDays = 5;

    public static bool NeedsCheck(double price, double? prevClose) =>
        prevClose is { } p && p > 0 && Math.Abs(price / p - 1) * 100 >= CheckChangePct;

    public static (double? PrevClose, SplitEvent? Applied) Adjust(double price, double? prevClose, IEnumerable<SplitEvent> splits, DateOnly today)
    {
        if (prevClose is not { } prev || prev <= 0)
            return (prevClose, null);

        var recent = splits
            .Where(s => s.Ratio > 0 && today.DayNumber - s.Date.DayNumber is >= 0 and <= RecentDays)
            .OrderByDescending(s => s.Date)
            .FirstOrDefault();
        if (recent is null)
            return (prevClose, null);

        var adjusted = prev / recent.Ratio;
        return Math.Abs(price / adjusted - 1) < Math.Abs(price / prev - 1)
            ? (adjusted, recent)
            : (prevClose, null);
    }
}
