using System.Net;
using System.Text;
using System.Text.Json;
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
        Assert.Equal("매수금액 미입력", plan.Skips.Single(s => s.Symbol == "MSFT").Reason);
        Assert.Contains("1주", plan.Skips.Single(s => s.Symbol == "KO").Reason);
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

/// <summary>키움 API 요청 형식과 응답 처리. 가짜 HTTP 처리기를 써서 실제로는 아무것도 보내지 않는다.</summary>
public sealed class KiwoomClientTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"stocktarget_rsv_{Guid.NewGuid():N}.db");
    private readonly FakeKiwoom _fake = new();

    public void Dispose()
    {
        if (File.Exists(_path))
            File.Delete(_path);
    }

    private static ReservationOrder Order(BuyStatus stage = BuyStatus.Buy, double price = 89.86, int qty = 8) =>
        new("KO", stage, "2026Q4", new DateOnly(2026, 10, 12), new DateOnly(2026, 10, 16), 89.86, price, qty, 1_000_000, 740.74);

    private KiwoomClient Client(bool mock = true) => new(new KiwoomOptions("APPKEY", "SECRET", mock), _fake);

    [Fact]
    public async Task ReserveBuySendsPeriodLocRequest()
    {
        using var client = Client();
        var receipt = await client.ReserveBuyLocAsync(Order(), "NY");

        Assert.Equal("000000000026", receipt.ReservationNo);
        Assert.Equal("20261012", receipt.ScheduledDate);

        var token = _fake.Requests[0];
        Assert.Equal("au10001", token.ApiId);
        Assert.Equal("https://mockapi.kiwoom.com/oauth2/token", token.Url);
        Assert.Equal("client_credentials", token.Body.GetProperty("grant_type").GetString());
        Assert.Null(token.Authorization);

        var order = _fake.Requests[1];
        Assert.Equal("ust21200", order.ApiId);
        Assert.Equal("https://mockapi.kiwoom.com/api/us/ordr", order.Url);
        Assert.Equal("Bearer TOKEN-1", order.Authorization);
        var b = order.Body;
        Assert.Equal("2", b.GetProperty("rsrv_ord_tp").GetString());          // 기간예약(잔량주문)
        Assert.Equal("20261012", b.GetProperty("rsrv_strt_dt").GetString());
        Assert.Equal("20261016", b.GetProperty("rsrv_end_dt").GetString());
        Assert.Equal("NY", b.GetProperty("stex_tp").GetString());
        Assert.Equal("KO", b.GetProperty("stk_cd").GetString());
        Assert.Equal("8", b.GetProperty("ord_qty").GetString());
        Assert.Equal("89.86", b.GetProperty("ord_uv").GetString());
        Assert.Equal("30", b.GetProperty("trde_tp").GetString());             // LOC
    }

    [Fact]
    public async Task TokenIsReusedAndRealUsesApiDomain()
    {
        using var client = Client(mock: false);
        Assert.Equal("NY", await client.GetExchangeAsync("KO"));
        await client.ReserveBuyLocAsync(Order(), "NY");
        Assert.Single(_fake.Requests, r => r.ApiId == "au10001");             // 토큰은 한 번만
        Assert.All(_fake.Requests, r => Assert.StartsWith("https://api.kiwoom.com/", r.Url));
        Assert.Equal("usa10098", _fake.Requests[1].ApiId);
        Assert.Equal("KO", _fake.Requests[1].Body.GetProperty("stk_cd").GetString());
    }

    [Fact]
    public async Task ErrorReturnCodeThrows()
    {
        _fake.OrderResponse = """{"return_code": 20, "return_msg": "주문가능금액이 부족합니다"}""";
        using var client = Client();
        var e = await Assert.ThrowsAsync<KiwoomException>(() => client.ReserveBuyLocAsync(Order(), "NY"));
        Assert.Equal(20, e.Code);
        Assert.Contains("주문가능금액이 부족합니다", e.Message);
    }

    [Fact]
    public void OptionsToStringHidesKeys()
    {
        var o = new KiwoomOptions("APPKEY", "SECRET", true);
        Assert.DoesNotContain("SECRET", o.ToString());
        Assert.DoesNotContain("APPKEY", o.ToString());
    }

    [Fact]
    public async Task ServiceSkipsDuplicatesAndContinuesAfterFailure()
    {
        var db = new StockDatabase(_path);
        using var client = Client();
        var service = new ReservationService(db, client) { RequestDelay = TimeSpan.Zero };
        var orders = new[] { Order(BuyStatus.Buy), Order(BuyStatus.MustBuy, 80.87, 9) };

        var first = await service.SubmitAsync(orders);
        Assert.All(first, r => Assert.True(r.Submitted));
        Assert.Equal(2, db.GetReservations().Count);
        Assert.Single(_fake.Requests, r => r.ApiId == "usa10098"); // 같은 티커 거래소 조회는 한 번

        // 다시 보내면 DB에 있는 건은 건너뛴다(키움에 두 번 접수하지 않음)
        var sent = _fake.Requests.Count(r => r.ApiId == "ust21200");
        var again = await service.SubmitAsync(orders);
        Assert.All(again, r => Assert.StartsWith("이미 접수됨", r.Message));
        Assert.Equal(sent, _fake.Requests.Count(r => r.ApiId == "ust21200"));

        // 실패한 건은 기록하지 않고 다음 건을 계속 보낸다
        _fake.OrderResponse = """{"return_code": 20, "return_msg": "거부"}""";
        var failed = await service.SubmitAsync([Order(BuyStatus.StrongBuy, 71.89, 10)]);
        Assert.StartsWith("실패", failed[0].Message);
        Assert.Equal(2, db.GetReservations().Count);
    }

    private sealed record Captured(string ApiId, string Url, string? Authorization, JsonElement Body);

    private sealed class FakeKiwoom : HttpMessageHandler
    {
        public List<Captured> Requests { get; } = [];
        public string OrderResponse { get; set; } =
            """{"rsrv_ord_no": "000000000026", "frcs_dt": "20261012", "return_code": 0, "return_msg": "미국 예약주문 입력이 완료되었습니다."}""";

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var apiId = request.Headers.GetValues("api-id").Single();
            var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(ct)).RootElement.Clone();
            Requests.Add(new Captured(apiId, request.RequestUri!.ToString(), request.Headers.Authorization?.ToString(), body));
            var json = apiId switch
            {
                "au10001" => """{"expires_dt": "29991231235959", "token_type": "bearer", "token": "TOKEN-1", "return_code": 0}""",
                "usa10098" => """{"list": [{"stex_tp": "NY", "stk_cd": "KO", "stk_nm": "코카콜라"}], "return_code": 0}""",
                "ust21200" => OrderResponse,
                _ => """{"return_code": 1, "return_msg": "unknown"}""",
            };
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        }
    }
}
