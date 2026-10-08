using System.Globalization;

namespace StockTarget.Core;

/// <summary>
/// 키움 미국주식 예약 매수(LOC) 한 건.
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

    /// <summary>키움 API 주문단가 문자열(소수 2자리).</summary>
    public string PriceText => Price.ToString("0.00", CultureInfo.InvariantCulture);
}

/// <summary>주문표에서 빠진 목표와 사유.</summary>
public sealed record ReservationSkip(string Symbol, string Reason);

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
/// 수량          : 금액(원) ÷ 환율 ÷ 주문가, 1주 단위 내림
/// </code>
/// 종가가 강력매수 가격 이하면 세 건이 모두 체결돼 총 체결액이 강력매수 금액이 된다.
/// 기간이 분기 경계를 넘으면 분기마다 b가 달라 분기별로 나눠 주문한다.
/// </summary>
public static class ReservationPlanner
{
    private static readonly BuyStatus[] Stages = [BuyStatus.Buy, BuyStatus.MustBuy, BuyStatus.StrongBuy];

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

    public static ReservationPlan Plan(IEnumerable<TargetPlan> targets, DateOnly start, DateOnly end, double usdKrw)
    {
        if (usdKrw <= 0)
            throw new ArgumentException("환율이 올바르지 않습니다");
        if (end < start)
            throw new ArgumentException("예약 종료일이 시작일보다 빠릅니다");

        var orders = new List<ReservationOrder>();
        var skips = new List<ReservationSkip>();
        foreach (var t in targets)
        {
            if (t.BuyAmounts.IsEmpty)
            {
                skips.Add(new ReservationSkip(t.Symbol, "매수금액 미입력"));
                continue;
            }

            var schedule = TargetCalculator.Schedule(t);
            foreach (var (quarter, segStart, segEnd) in QuarterSegments(start, end))
            {
                var row = schedule.FirstOrDefault(r => r.Quarter == quarter);
                if (row is null)
                {
                    skips.Add(new ReservationSkip(t.Symbol, $"{quarter}는 목표 일정 밖"));
                    continue;
                }
                foreach (var (stage, krw) in StageIncrements(t.BuyAmounts))
                {
                    if (krw <= 0)
                        continue;
                    var price = LimitPrice(row.BuyPrice, stage);
                    var usd = krw / usdKrw;
                    var qty = price > 0 ? (int)Math.Floor(usd / price) : 0;
                    if (qty <= 0)
                    {
                        skips.Add(new ReservationSkip(t.Symbol,
                            $"{stage.ToText()} {Money.Text(krw, usdKrw)}로는 1주({Money.UsdText(price)})를 살 수 없음"));
                        continue;
                    }
                    orders.Add(new ReservationOrder(t.Symbol, stage, quarter, segStart, segEnd, row.BuyPrice, price, qty, krw, usd));
                }
            }
        }
        return new ReservationPlan(start, end, usdKrw, orders, skips);
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

/// <summary>예약 한 건의 접수 결과.</summary>
public sealed record ReservationResult(ReservationOrder Order, bool Submitted, string Message);

/// <summary>
/// 주문표를 키움에 접수한다. 같은 티커·단계·기간·환경으로 이미 접수한 건은 다시 보내지 않는다.
/// 한 건이 실패해도 나머지는 계속 보낸다.
/// </summary>
public sealed class ReservationService(StockDatabase db, KiwoomClient kiwoom, Func<DateTimeOffset>? now = null)
{
    private readonly Func<DateTimeOffset> _now = now ?? (() => DateTimeOffset.Now);

    /// <summary>요청 사이 간격(키움 호출 제한 대비).</summary>
    public TimeSpan RequestDelay { get; set; } = TimeSpan.FromMilliseconds(300);

    public string Env => kiwoom.Options.EnvText;

    public bool IsAlreadySubmitted(ReservationOrder o) =>
        db.FindReservation(o.Symbol, o.StageText, o.Start, o.End, Env) is not null;

    public async Task<IReadOnlyList<ReservationResult>> SubmitAsync(
        IEnumerable<ReservationOrder> orders, IProgress<ReservationResult>? progress = null, CancellationToken ct = default)
    {
        var results = new List<ReservationResult>();
        var exchanges = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var o in orders)
        {
            ReservationResult result;
            if (db.FindReservation(o.Symbol, o.StageText, o.Start, o.End, Env) is { } prev)
            {
                result = new ReservationResult(o, false, $"이미 접수됨 (예약번호 {prev.ReservationNo})");
            }
            else
            {
                try
                {
                    if (!exchanges.TryGetValue(o.Symbol, out var stex))
                    {
                        stex = await kiwoom.GetExchangeAsync(o.Symbol, ct).ConfigureAwait(false);
                        exchanges[o.Symbol] = stex;
                    }
                    var receipt = await kiwoom.ReserveBuyLocAsync(o, stex, ct).ConfigureAwait(false);
                    db.SaveReservation(new ReservationRecord(o.Symbol, o.StageText, o.Start, o.End, o.Price, o.Quantity,
                        o.AmountKrw, Env, receipt.ReservationNo, receipt.ScheduledDate, _now()));
                    result = new ReservationResult(o, true, $"접수 (예약번호 {receipt.ReservationNo}, 주문예정일 {receipt.ScheduledDate})");
                }
                catch (Exception e) when (e is KiwoomException or HttpRequestException or TaskCanceledException)
                {
                    result = new ReservationResult(o, false, "실패: " + e.Message);
                }
                await Task.Delay(RequestDelay, ct).ConfigureAwait(false);
            }
            results.Add(result);
            progress?.Report(result);
        }
        return results;
    }
}
