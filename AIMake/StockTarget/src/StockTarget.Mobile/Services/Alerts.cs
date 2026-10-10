using StockTarget.Core;
using StockTarget.Mobile.Localization;

namespace StockTarget.Mobile.Services;

/// <summary>알림 한 건(제목 · 본문). 플랫폼 코드가 이걸 받아 띄운다.</summary>
public sealed record AlertMessage(int Id, string Title, string Body);

/// <summary>
/// 알림 설정과 내용 만들기. 앱이 꺼져 있을 때 백그라운드 작업(Platforms/Android/AlertWork.cs)이 부르므로 화면 객체를 쓰지 않는다.
/// <code>
/// 주간 예약 알림 : 매주 일요일 20:00 — 매수 필요(매수 단계 · 매수 완료 아님) 종목을 알린다. 없으면 보내지 않는다.
/// 매수 단계 알림 : 매일 07:00(미국장 마감 뒤) — 단계가 새로 올라간 종목만(StageAlerts). 매수 완료 종목은 뺀다.
/// </code>
/// 시각은 안드로이드 배터리 절약 때문에 조금 늦을 수 있다(정확한 알람 권한은 쓰지 않는다).
/// </summary>
public static class Alerts
{
    public const int WeeklyId = 1001;
    public const int StageId = 1002;

    public const DayOfWeek WeeklyDay = DayOfWeek.Sunday;
    public static readonly TimeSpan WeeklyTime = new(20, 0, 0);
    public static readonly TimeSpan DailyTime = new(7, 0, 0);

    private const string WeeklyKey = "alert_weekly";
    private const string DailyKey = "alert_daily";
    private const string StagesKey = "alert_stages";

    public static bool WeeklyEnabled
    {
        get => Preferences.Default.Get(WeeklyKey, false);
        set => Preferences.Default.Set(WeeklyKey, value);
    }

    public static bool DailyEnabled
    {
        get => Preferences.Default.Get(DailyKey, false);
        set
        {
            Preferences.Default.Set(DailyKey, value);
            if (!value)
                Preferences.Default.Remove(StagesKey); // 다시 켜면 그때 매수 단계인 종목부터 새로 알린다
        }
    }

    /// <summary>from 이후 처음 오는 (요일, 시각)까지 남은 시간. 요일이 null이면 매일.</summary>
    public static TimeSpan DelayUntil(DateTime from, TimeSpan time, DayOfWeek? day = null)
    {
        var next = from.Date + time;
        if (day is { } d)
            next = next.AddDays(((int)d - (int)from.DayOfWeek + 7) % 7);
        if (next <= from)
            next = next.AddDays(day is null ? 1 : 7);
        return next - from;
    }

    /// <summary>목표 하나의 지금 상태.</summary>
    private sealed record StockStage(string Symbol, string Name, BuyStatus Stage, double? Price, double? BuyPrice)
    {
        public string Title => Name.Length > 0 ? Name : Symbol;
    }

    private static async Task<List<StockStage>> FetchStagesAsync(StockDatabase db, StockService service, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var korean = L.IsKorean ? db.GetStockNames() : new Dictionary<string, string>();
        var list = new List<StockStage>();
        foreach (var plan in db.GetTargets())
        {
            var buyPrice = TargetCalculator.CurrentRow(plan, today)?.BuyPrice;
            try
            {
                var quote = (await service.GetQuoteAsync(plan.Symbol, force: false, ct)).Value;
                var name = korean.GetValueOrDefault(plan.Symbol) ?? quote.Name ?? "";
                list.Add(new StockStage(plan.Symbol, name, TargetCalculator.Classify(quote.Price, buyPrice), quote.Price, buyPrice));
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                list.Add(new StockStage(plan.Symbol, korean.GetValueOrDefault(plan.Symbol) ?? "", BuyStatus.None, null, buyPrice));
            }
        }
        return list;
    }

    /// <summary>매수 단계 알림. 새로 올라간 종목이 없으면 null.</summary>
    public static async Task<AlertMessage?> StageCheckAsync(StockDatabase db, StockService service, CancellationToken ct = default)
    {
        db.ClearStaleBuyDone();
        var stages = await FetchStagesAsync(db, service, ct);
        var current = stages.ToDictionary(s => s.Symbol, s => s.Stage);
        var previous = StageAlerts.Deserialize(Preferences.Default.Get(StagesKey, ""));
        var reached = StageAlerts.NewlyReached(previous, current, db.GetBuyDone());
        Preferences.Default.Set(StagesKey, StageAlerts.Serialize(StageAlerts.Remember(previous, current)));
        if (reached.Count == 0)
            return null;

        var hits = reached.Select(r => stages.First(s => s.Symbol == r.Symbol)).OrderByDescending(s => s.Stage.BuyLevel()).ToList();
        if (hits.Count == 1)
        {
            var s = hits[0];
            var gap = s.Price is { } p && s.BuyPrice is { } b && b > 0 ? $"{(p / b - 1) * 100:+0.0;-0.0}%" : "";
            return new AlertMessage(StageId, L.Format("Notif_StageOneTitle", s.Title, L.Verdict(s.Stage)),
                L.Format("Notif_StageOneBody", s.Price ?? 0, s.BuyPrice ?? 0, gap));
        }
        return new AlertMessage(StageId, L.Format("Notif_StageManyTitle", hits.Count),
            string.Join(" · ", hits.Select(s => $"{s.Title} {L.Verdict(s.Stage)}")));
    }

    /// <summary>주간 예약 알림. 매수 필요 종목이 없으면 null(force면 없다고 알린다 — 테스트용).</summary>
    public static async Task<AlertMessage?> WeeklySummaryAsync(StockDatabase db, StockService service, bool force = false, CancellationToken ct = default)
    {
        db.ClearStaleBuyDone();
        var stages = await FetchStagesAsync(db, service, ct);
        var done = db.GetBuyDone();
        var toBuy = stages.Where(s => s.Stage.BuyLevel() > 0 && !done.Contains(s.Symbol))
            .OrderByDescending(s => s.Stage.BuyLevel()).ToList();
        if (toBuy.Count > 0)
            return new AlertMessage(WeeklyId, L.Get("Notif_WeeklyTitle"),
                L.Format("Notif_WeeklyBody", toBuy.Count, string.Join(" · ", toBuy.Select(s => $"{s.Title} {L.Verdict(s.Stage)}"))));
        if (stages.Count > 0 && stages.All(s => s.Stage == BuyStatus.None))
            return new AlertMessage(WeeklyId, L.Get("Notif_WeeklyTitle"), L.Get("Notif_WeeklyNoData")); // 시세를 하나도 못 받음
        return force ? new AlertMessage(WeeklyId, L.Get("Notif_WeeklyTitle"), L.Get("Main_EmptyFiltered")) : null;
    }
}
