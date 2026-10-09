using System.Globalization;

namespace StockTarget.Core;

/// <summary>
/// 미국주식 LOC 예약 매수 한 건(증권사 앱에 직접 넣을 때 참고하는 주문표 한 줄).
/// 기간(Start~End) 동안 매일 장 마감 때 종가가 Price 이하이면 체결된다(기간예약 잔량주문: 다 체결되면 끝).
/// </summary>
public sealed record ReservationOrder(
    string Symbol,
    BuyStatus Stage,
    string Quarter,
    DateOnly Start,
    DateOnly End,
    double BuyPrice,
    double Price,
    int Quantity,
    double AmountKrw,
    double AmountUsd)
{
    /// <summary>실제 주문 금액(달러) = 주문가 × 수량. 1주 단위로 내림하므로 AmountUsd 이하.</summary>
    public double OrderUsd => Price * Quantity;

    public string StageText => Stage.ToText();

    /// <summary>주문단가 문자열(소수 2자리).</summary>
    public string PriceText => Price.ToString("0.00", CultureInfo.InvariantCulture);
}

/// <summary>주문표에서 빠진 목표와 사유. BuyDone이면 사용자가 '매수 완료'로 표시해 뺀 것(해제하면 다시 들어온다).</summary>
public sealed record ReservationSkip(string Symbol, string Reason, bool BuyDone = false);

public sealed record ReservationPlan(
    DateOnly Start,
    DateOnly End,
    double UsdKrw,
    IReadOnlyList<ReservationOrder> Orders,
    IReadOnlyList<ReservationSkip> Skips);

/// <summary>
/// 등록된 목표 → 다음 주 LOC 예약 매수 주문표.
/// <code>
/// 단계별 주문가 : 매수 b · 필수매수 b × 0.90 · 강력매수 b × 0.80   (b = 그 분기 매입 목표가, 센트 단위 내림)
/// 단계별 금액   : 매수 금액 / 필수매수 − 매수 / 강력매수 − 필수매수  (계단식 · 총액 맞춤)
/// 금액          : 목표에 입력한 값, 비운 단계는 기본 매수금액(0이면 그 단계는 주문하지 않음)
/// 매수 완료     : 사용자가 '매수 완료'로 표시한 종목은 해제할 때까지 통째로 뺀다
/// 대상 단계     : 현재가가 주문가 이하로 이미 내려온 단계만(현재가를 주면). 나머지는 제외 목록에 사유와 함께
/// 수량          : 금액(원) ÷ 환율 ÷ 주문가, 1주 단위 내림(1주도 못 사면 제외)
/// </code>
/// 종가가 강력매수 가격 이하면 세 건이 모두 체결돼 총 체결액이 강력매수 금액이 된다.
/// 기간이 분기 경계를 넘으면 분기마다 b가 달라 분기별로 나눠 주문한다.
/// </summary>
public static class ReservationPlanner
{
    private static readonly BuyStatus[] Stages = [BuyStatus.Buy, BuyStatus.MustBuy, BuyStatus.StrongBuy];

    public const string BuyDoneReason = "매수 완료 — 해제할 때까지 주문표에서 제외";

    /// <summary>today 기준 다음 주 월요일 ~ 금요일.</summary>
    public static (DateOnly Start, DateOnly End) NextWeek(DateOnly today)
    {
        var daysToMonday = ((int)DayOfWeek.Monday - (int)today.DayOfWeek + 7) % 7;
        var monday = today.AddDays(daysToMonday == 0 ? 7 : daysToMonday);
        return (monday, monday.AddDays(4));
    }

    /// <summary>단계별 LOC 주문가: b × (1 − 단계 할인율), 센트 단위 내림(매수 가격이라 위로 반올림하지 않는다).</summary>
    public static double LimitPrice(double buyPrice, BuyStatus stage)
    {
        var pct = stage switch
        {
            BuyStatus.MustBuy => TargetCalculator.MustBuyPct,
            BuyStatus.StrongBuy => TargetCalculator.StrongBuyPct,
            _ => 0,
        };
        // 1e-9: 0.1 같은 이진 표현 오차로 한 센트 내려가는 것을 막는다
        return Math.Floor(buyPrice * (1 - pct / 100) * 100 + 1e-9) / 100;
    }

    /// <summary>계단식 단계별 추가 금액(원). 비운 단계는 0, 앞 단계보다 작으면 0.</summary>
    public static IReadOnlyList<(BuyStatus Stage, double Krw)> StageIncrements(BuyAmounts amounts)
    {
        var list = new List<(BuyStatus, double)>();
        double cumulative = 0;
        foreach (var stage in Stages)
        {
            if (amounts.For(stage) is not { } krw)
                continue;
            list.Add((stage, Math.Max(0, krw - cumulative)));
            cumulative = Math.Max(cumulative, krw);
        }
        return list;
    }

    /// <param name="defaults">기본 매수금액. 목표에서 비운 단계에 쓴다.</param>
    /// <param name="currentPrices">
    /// 티커별 현재가. 주면 현재가 ≤ 주문가인(이미 도달한) 단계만 주문하고, 현재가가 없는 목표는 제외한다.
    /// null이면 도달 여부를 보지 않고 모든 단계를 주문한다.
    /// </param>
    /// <param name="buyDone">'매수 완료'로 표시한 티커. 주문하지 않고 제외 목록에 넣는다.</param>
    public static ReservationPlan Plan(
        IEnumerable<TargetPlan> targets, DateOnly start, DateOnly end, double usdKrw, BuyAmounts? defaults = null,
        IReadOnlyDictionary<string, double>? currentPrices = null, IReadOnlySet<string>? buyDone = null)
    {
        if (usdKrw <= 0)
            throw new ArgumentException("환율이 올바르지 않습니다");
        if (end < start)
            throw new ArgumentException("예약 종료일이 시작일보다 빠릅니다");

        var orders = new List<ReservationOrder>();
        var skips = new List<ReservationSkip>();
        foreach (var t in targets)
        {
            if (buyDone is not null && buyDone.Contains(t.Symbol))
            {
                skips.Add(new ReservationSkip(t.Symbol, BuyDoneReason, BuyDone: true));
                continue;
            }

            var amounts = t.EffectiveAmounts(defaults);
            if (amounts.IsEmpty)
            {
                skips.Add(new ReservationSkip(t.Symbol, "매수금액 미입력(기본 매수금액도 없음)"));
                continue;
            }

            double? current = null;
            if (currentPrices is not null)
            {
                if (!currentPrices.TryGetValue(t.Symbol, out var c) || c <= 0)
                {
                    skips.Add(new ReservationSkip(t.Symbol, "현재가 없음(시세 조회 실패) — 도달한 단계를 알 수 없음"));
                    continue;
                }
                current = c;
            }

            var schedule = TargetCalculator.Schedule(t);
            var segments = QuarterSegments(start, end);
            foreach (var (quarter, segStart, segEnd) in segments)
            {
                var row = schedule.FirstOrDefault(r => r.Quarter == quarter);
                if (row is null)
                {
                    skips.Add(new ReservationSkip(t.Symbol, $"{quarter}는 목표 일정 밖"));
                    continue;
                }
                var unreached = new List<(BuyStatus Stage, double Price)>();
                foreach (var (stage, krw) in StageIncrements(amounts))
                {
                    if (krw <= 0)
                        continue;
                    var price = LimitPrice(row.BuyPrice, stage);
                    if (current is { } cur && cur > price)
                    {
                        unreached.Add((stage, price));
                        continue;
                    }
                    var usd = krw / usdKrw;
                    var qty = Money.Shares(krw, usdKrw, price) ?? 0;
                    if (qty <= 0)
                    {
                        skips.Add(new ReservationSkip(t.Symbol,
                            $"{stage.ToText()} {Money.Text(krw, usdKrw)}로는 1주({Money.UsdText(price)})를 살 수 없음"));
                        continue;
                    }
                    orders.Add(new ReservationOrder(t.Symbol, stage, quarter, segStart, segEnd, row.BuyPrice, price, qty, krw, usd));
                }
                if (unreached.Count > 0 && current is { } now)
                    skips.Add(new ReservationSkip(t.Symbol, UnreachedReason(unreached, now, segments.Count > 1 ? quarter : null)));
            }
        }
        return new ReservationPlan(start, end, usdKrw, orders, skips);
    }

    /// <summary>"필수매수 · 강력매수 미도달: 현재가 85.82 > 주문가 80.86 · 71.87 (+6.1%)". 괴리는 가장 가까운 단계 기준.</summary>
    private static string UnreachedReason(List<(BuyStatus Stage, double Price)> unreached, double current, string? quarter)
    {
        var stages = string.Join(" · ", unreached.Select(u => u.Stage.ToText()));
        var prices = string.Join(" · ", unreached.Select(u => u.Price.ToString("0.00", CultureInfo.InvariantCulture)));
        var gap = (current / unreached[0].Price - 1) * 100;
        var prefix = quarter is null ? "" : quarter + " ";
        return $"{prefix}{stages} 미도달: 현재가 {current.ToString("0.00", CultureInfo.InvariantCulture)} > 주문가 {prices} " +
               $"({gap.ToString("+0.0", CultureInfo.InvariantCulture)}%)";
    }

    /// <summary>기간을 분기별로 나눈다(주말은 양 끝에서 뺀다).</summary>
    public static IReadOnlyList<(string Quarter, DateOnly Start, DateOnly End)> QuarterSegments(DateOnly start, DateOnly end)
    {
        var segments = new List<(string, DateOnly, DateOnly)>();
        var days = Enumerable.Range(0, end.DayNumber - start.DayNumber + 1)
            .Select(start.AddDays)
            .Where(d => d.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday))
            .ToList();
        foreach (var g in days.GroupBy(TargetCalculator.QuarterLabel))
            segments.Add((g.Key, g.First(), g.Last()));
        return segments;
    }
}
