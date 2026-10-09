using StockTarget.Core;

namespace StockTarget.Tests;

public class ReservationPlannerTests
{
    private static readonly DateOnly Input = new(2026, 10, 8);

    // 목표 주가 120 직접 입력, 수익률 10%, 배당 2.92% → 2026Q4 매입 목표가 약 89.84
    private static TargetPlan Ko(BuyAmounts? amounts) =>
        new("KO", Input, 2030, 0, 0, 10, 2.92, null, amounts, 120);

    [Theory]
    [InlineData(2026, 10, 8, 2026, 10, 12)]  // 목 → 다음 주 월
    [InlineData(2026, 10, 12, 2026, 10, 19)] // 월 → 다음 주 월(이번 주 아님)
    [InlineData(2026, 10, 11, 2026, 10, 12)] // 일 → 바로 다음 날 월
    [InlineData(2026, 10, 10, 2026, 10, 12)] // 토
    public void NextWeekIsMondayToFriday(int y, int m, int d, int ey, int em, int ed)
    {
        var (start, end) = ReservationPlanner.NextWeek(new DateOnly(y, m, d));
        Assert.Equal(new DateOnly(ey, em, ed), start);
        Assert.Equal(DayOfWeek.Monday, start.DayOfWeek);
        Assert.Equal(start.AddDays(4), end);
    }

    [Fact]
    public void LimitPriceRoundsDownToCent()
    {
        Assert.Equal(89.86, ReservationPlanner.LimitPrice(89.8649, BuyStatus.Buy));
        Assert.Equal(80.87, ReservationPlanner.LimitPrice(89.8649, BuyStatus.MustBuy));   // 80.878 → 80.87
        Assert.Equal(71.89, ReservationPlanner.LimitPrice(89.8649, BuyStatus.StrongBuy)); // 71.892 → 71.89
        Assert.Equal(90.00, ReservationPlanner.LimitPrice(100, BuyStatus.MustBuy));       // 이진 오차로 89.99가 되지 않음
    }

    [Fact]
    public void IncrementsAreTiered()
    {
        var inc = ReservationPlanner.StageIncrements(new BuyAmounts(1_000_000, 2_000_000, 3_000_000));
        Assert.Equal([(BuyStatus.Buy, 1_000_000.0), (BuyStatus.MustBuy, 1_000_000.0), (BuyStatus.StrongBuy, 1_000_000.0)], inc);

        // 필수매수를 비우면 강력매수는 매수 금액과의 차액
        inc = ReservationPlanner.StageIncrements(new BuyAmounts(1_000_000, null, 3_000_000));
        Assert.Equal([(BuyStatus.Buy, 1_000_000.0), (BuyStatus.StrongBuy, 2_000_000.0)], inc);

        // 뒤 단계가 더 작으면 추가 주문 없음
        inc = ReservationPlanner.StageIncrements(new BuyAmounts(2_000_000, 1_000_000));
        Assert.Equal([(BuyStatus.Buy, 2_000_000.0), (BuyStatus.MustBuy, 0.0)], inc);
    }

    [Fact]
    public void BuyDoneTargetIsSkipped()
    {
        var start = new DateOnly(2026, 10, 12);
        var amounts = new BuyAmounts(1_000_000, 2_000_000, 3_000_000);
        var msft = new TargetPlan("MSFT", Input, 2030, 27.5, 25, 11, 0.8, null, amounts);
        var plan = ReservationPlanner.Plan([Ko(amounts), msft], start, start.AddDays(4), 1350,
            buyDone: new HashSet<string> { "KO" });

        Assert.All(plan.Orders, o => Assert.Equal("MSFT", o.Symbol));
        var skip = Assert.Single(plan.Skips);
        Assert.Equal("KO", skip.Symbol);
        Assert.True(skip.BuyDone);
        Assert.Equal(ReservationPlanner.BuyDoneReason, skip.Reason);
    }

    [Fact]
    public void PlanBuildsThreeLocOrdersPerTarget()
    {
        var start = new DateOnly(2026, 10, 12);
        var plan = ReservationPlanner.Plan([Ko(new BuyAmounts(1_000_000, 2_000_000, 3_000_000))], start, start.AddDays(4), 1350);

        Assert.Empty(plan.Skips);
        Assert.Equal(3, plan.Orders.Count);
        var b = TargetCalculator.CurrentRow(Ko(null), start)!.BuyPrice;
        Assert.All(plan.Orders, o =>
        {
            Assert.Equal("2026Q4", o.Quarter);
            Assert.Equal(start, o.Start);
            Assert.Equal(new DateOnly(2026, 10, 16), o.End);
            Assert.Equal(b, o.BuyPrice);
            Assert.Equal(1_000_000, o.AmountKrw);
            Assert.True(o.OrderUsd <= o.AmountUsd);                 // 1주 단위 내림
            Assert.True(o.OrderUsd + o.Price > o.AmountUsd);        // 한 주 더 사면 금액 초과
        });
        // b ≈ 89.849 → 주문가 89.84 / 80.86 / 71.87, $740.74 ÷ 주문가 = 8.2 / 9.2 / 10.3 → 8 / 9 / 10주
        Assert.Equal([Math.Floor(b * 100) / 100, Math.Floor(b * 90) / 100, Math.Floor(b * 80) / 100], plan.Orders.Select(o => o.Price));
        Assert.Equal([89.84, 80.86, 71.87], plan.Orders.Select(o => o.Price));
        Assert.Equal([8, 9, 10], plan.Orders.Select(o => o.Quantity));
        Assert.Equal("89.84", plan.Orders[0].PriceText);
    }

    [Fact]
    public void PlanSkipsTargetsWithoutAmountsOrTooSmall()
    {
        var start = new DateOnly(2026, 10, 12);
        var msft = new TargetPlan("MSFT", Input, 2030, 27.5, 25, 11, 0.8); // 실제 DB 상태: 매수금액 없음
        var tiny = Ko(new BuyAmounts(BuyKrw: 50_000));                    // $37 < 1주 $89.86
        var plan = ReservationPlanner.Plan([msft, tiny], start, start.AddDays(4), 1350);

        Assert.Empty(plan.Orders);
        Assert.Equal("매수금액 미입력(기본 매수금액도 없음)", plan.Skips.Single(s => s.Symbol == "MSFT").Reason);
        Assert.Contains("1주", plan.Skips.Single(s => s.Symbol == "KO").Reason);
    }

    [Fact]
    public void PlanUsesDefaultAmountsForEmptyStages()
    {
        var start = new DateOnly(2026, 10, 12);
        var defaults = new BuyAmounts(1_000_000, 2_000_000, 3_000_000);
        var followsDefault = Ko(null);
        var withOverride = Ko(new BuyAmounts(BuyKrw: 0, StrongBuyKrw: 4_000_000)) with { Symbol = "PEP" };

        var plan = ReservationPlanner.Plan([followsDefault, withOverride], start, start.AddDays(4), 1350, defaults);

        Assert.Empty(plan.Skips);
        // 기본값만 쓰는 목표: 100만 / +100만 / +100만
        Assert.Equal([1_000_000d, 1_000_000, 1_000_000], plan.Orders.Where(o => o.Symbol == "KO").Select(o => o.AmountKrw));
        // 매수 0(사지 않음) · 필수매수 기본 200만 · 강력매수 개별 400만 → 필수 200만 / 강력 +200만
        var pep = plan.Orders.Where(o => o.Symbol == "PEP").ToList();
        Assert.Equal([BuyStatus.MustBuy, BuyStatus.StrongBuy], pep.Select(o => o.Stage));
        Assert.Equal([2_000_000d, 2_000_000], pep.Select(o => o.AmountKrw));
    }

    [Theory]
    [InlineData(100.0, new BuyStatus[0], "매수 1단계 · 매수 2단계 · 매수 3단계 미도달: 현재가 100.00 > 주문가 89.84 · 80.86 · 71.87 (+11.3%)")]
    [InlineData(89.84, new[] { BuyStatus.Buy }, "매수 2단계 · 매수 3단계 미도달: 현재가 89.84 > 주문가 80.86 · 71.87 (+11.1%)")] // 주문가와 같으면 도달
    [InlineData(85.82, new[] { BuyStatus.Buy }, "매수 2단계 · 매수 3단계 미도달: 현재가 85.82 > 주문가 80.86 · 71.87 (+6.1%)")]
    [InlineData(75.0, new[] { BuyStatus.Buy, BuyStatus.MustBuy }, "매수 3단계 미도달: 현재가 75.00 > 주문가 71.87 (+4.4%)")]
    [InlineData(70.0, new[] { BuyStatus.Buy, BuyStatus.MustBuy, BuyStatus.StrongBuy }, null)]
    public void PlanKeepsOnlyStagesReachedByCurrentPrice(double current, BuyStatus[] expected, string? skip)
    {
        var start = new DateOnly(2026, 10, 12);
        var prices = new Dictionary<string, double> { ["KO"] = current };
        var plan = ReservationPlanner.Plan([Ko(new BuyAmounts(1_000_000, 2_000_000, 3_000_000))],
            start, start.AddDays(4), 1350, currentPrices: prices);

        Assert.Equal(expected, plan.Orders.Select(o => o.Stage));
        Assert.All(plan.Orders, o => Assert.True(current <= o.Price));
        Assert.All(plan.Orders, o => Assert.Equal(1_000_000, o.AmountKrw)); // 계단식 금액은 그대로
        Assert.Equal(skip is null ? [] : [skip], plan.Skips.Select(s => s.Reason));
    }

    [Fact]
    public void PlanSkipsTargetWithoutCurrentPrice()
    {
        var start = new DateOnly(2026, 10, 12);
        var pep = Ko(new BuyAmounts(BuyKrw: 1_000_000)) with { Symbol = "PEP" };
        var plan = ReservationPlanner.Plan([Ko(new BuyAmounts(BuyKrw: 1_000_000)), pep], start, start.AddDays(4), 1350,
            currentPrices: new Dictionary<string, double> { ["KO"] = 80 }); // PEP 시세 조회 실패

        Assert.Equal("KO", Assert.Single(plan.Orders).Symbol);
        Assert.Equal("현재가 없음(시세 조회 실패) — 도달한 단계를 알 수 없음", plan.Skips.Single(s => s.Symbol == "PEP").Reason);
    }

    [Fact]
    public void UnreachedReasonNamesQuarterWhenPeriodIsSplit()
    {
        // 분기 경계 주: Q4(b≈89.85)에는 도달, Q1(b가 더 높음)에도 도달 / 현재가 95면 둘 다 미도달
        var plan = ReservationPlanner.Plan([Ko(new BuyAmounts(BuyKrw: 1_000_000))],
            new DateOnly(2026, 12, 28), new DateOnly(2027, 1, 1), 1350, currentPrices: new Dictionary<string, double> { ["KO"] = 95 });
        Assert.Empty(plan.Orders);
        Assert.Equal(["2026Q4", "2027Q1"], plan.Skips.Select(s => s.Reason[..6]));
    }

    [Fact]
    public void PlanSplitsAtQuarterBoundary()
    {
        // 2026-12-28(월) ~ 2027-01-01(금): Q4 4일 + Q1 1일 → 분기마다 b가 다르다
        var plan = ReservationPlanner.Plan([Ko(new BuyAmounts(BuyKrw: 1_000_000))],
            new DateOnly(2026, 12, 28), new DateOnly(2027, 1, 1), 1350);
        Assert.Equal(["2026Q4", "2027Q1"], plan.Orders.Select(o => o.Quarter));
        Assert.Equal(new DateOnly(2026, 12, 31), plan.Orders[0].End);
        Assert.Equal(new DateOnly(2027, 1, 1), plan.Orders[1].Start);
        Assert.True(plan.Orders[1].BuyPrice > plan.Orders[0].BuyPrice);
    }

    [Fact]
    public void QuarterSegmentsDropWeekends()
    {
        var seg = Assert.Single(ReservationPlanner.QuarterSegments(new DateOnly(2026, 10, 10), new DateOnly(2026, 10, 18)));
        Assert.Equal(new DateOnly(2026, 10, 12), seg.Start); // 토·일 제외
        Assert.Equal(new DateOnly(2026, 10, 16), seg.End);
    }
}

public class StartupOptionsTests
{
    [Fact]
    public void ParsesDb()
    {
        Assert.Equal(new StartupOptions(), StartupOptions.Parse([]));
        Assert.Equal(new StartupOptions(@"C:\t\a.db"), StartupOptions.Parse(["--db", @"C:\t\a.db"]));
        Assert.Equal(@"C:\t\a.db", StartupOptions.Parse(["--DB", @"C:\t\a.db"]).DbPath); // 대소문자 무시
    }

    [Theory]
    [InlineData("--db")]            // 값 없음
    [InlineData("--db", "--db")]    // 다음 옵션을 값으로 착각하지 않음
    [InlineData("--kiwoom", "off")] // 키움 연동은 없앴으므로 모르는 옵션
    [InlineData("--real")]          // 모르는 옵션
    public void RejectsBadOptions(params string[] args) =>
        Assert.Throws<ArgumentException>(() => StartupOptions.Parse(args));
}
