using StockTarget.Core;

namespace StockTarget.Tests;

public class StageAlertTests
{
    private static readonly IReadOnlySet<string> NoneDone = new HashSet<string>();

    [Fact]
    public void NotifiesOnlyWhenStageRises()
    {
        var previous = new Dictionary<string, BuyStatus>
        {
            ["KO"] = BuyStatus.Wait,     // 대기 → 매수: 알림
            ["NKE"] = BuyStatus.Buy,     // 1단계 → 3단계: 알림
            ["PEP"] = BuyStatus.MustBuy, // 그대로: 알림 없음
            ["MKL"] = BuyStatus.StrongBuy, // 3단계 → 1단계(내려감): 알림 없음
        };
        var current = new Dictionary<string, BuyStatus>
        {
            ["KO"] = BuyStatus.Buy,
            ["NKE"] = BuyStatus.StrongBuy,
            ["PEP"] = BuyStatus.MustBuy,
            ["MKL"] = BuyStatus.Buy,
            ["AAPL"] = BuyStatus.Near,  // 매수 단계 아님
            ["SPGI"] = BuyStatus.Buy,   // 처음 보는 종목: 알림
        };

        var alerts = StageAlerts.NewlyReached(previous, current, NoneDone);

        Assert.Equal(["KO", "NKE", "SPGI"], alerts.Select(a => a.Symbol).Order());
        Assert.Contains(("NKE", BuyStatus.StrongBuy), alerts);
    }

    [Fact]
    public void SkipsBoughtStocks()
    {
        var current = new Dictionary<string, BuyStatus> { ["KO"] = BuyStatus.Buy, ["NKE"] = BuyStatus.StrongBuy };
        var alerts = StageAlerts.NewlyReached(new Dictionary<string, BuyStatus>(), current, new HashSet<string> { "NKE" });
        Assert.Equal([("KO", BuyStatus.Buy)], alerts);
    }

    [Fact]
    public void FetchFailureKeepsPreviousStage()
    {
        var previous = new Dictionary<string, BuyStatus> { ["KO"] = BuyStatus.Buy, ["OLD"] = BuyStatus.Buy };
        var current = new Dictionary<string, BuyStatus> { ["KO"] = BuyStatus.None, ["NKE"] = BuyStatus.Wait };

        var next = StageAlerts.Remember(previous, current);

        Assert.Equal(BuyStatus.Buy, next["KO"]);  // 조회 실패: 지난 단계 유지 → 다음 날 같은 알림이 다시 오지 않는다
        Assert.Equal(BuyStatus.Wait, next["NKE"]);
        Assert.False(next.ContainsKey("OLD"));     // 목록에서 지운 종목은 버린다
        Assert.Empty(StageAlerts.NewlyReached(next, new Dictionary<string, BuyStatus> { ["KO"] = BuyStatus.Buy }, NoneDone));
    }

    [Fact]
    public void SerializeRoundTrip()
    {
        var stages = new Dictionary<string, BuyStatus> { ["NKE"] = BuyStatus.StrongBuy, ["KO"] = BuyStatus.Buy };
        var text = StageAlerts.Serialize(stages);
        Assert.Equal("KO=Buy;NKE=StrongBuy", text);
        Assert.Equal(stages, StageAlerts.Deserialize(text));
        Assert.Empty(StageAlerts.Deserialize(null));
        Assert.Equal(BuyStatus.Buy, StageAlerts.Deserialize("KO=Buy;bad;X=Nope")["KO"]);
    }
}
