using System.Net;
using System.Text;
using StockTarget.Core;

namespace StockTarget.Tests;

public sealed class StockNameTests : IDisposable
{
    private readonly List<string> _paths = [];
    private DateTimeOffset _now = new(2026, 10, 9, 1, 0, 0, TimeSpan.Zero);

    public void Dispose()
    {
        foreach (var p in _paths.Where(File.Exists))
            File.Delete(p);
    }

    private StockDatabase NewDb()
    {
        var path = Path.Combine(Path.GetTempPath(), $"stocktarget_name_{Guid.NewGuid():N}.db");
        _paths.Add(path);
        return new StockDatabase(path, () => _now);
    }

    // ac.stock.naver.com 실제 응답에서 필요한 필드만 남긴 것
    private const string KoJson = """
        {"query":"KO","items":[
          {"code":"KO","name":"코카콜라","typeCode":"NYSE","reutersCode":"KO","nationCode":"USA"},
          {"code":"252670","name":"KODEX 200선물인버스2X","typeCode":"KOSPI","reutersCode":"252670","nationCode":"KOR"}]}
        """;

    [Theory]
    [InlineData("BRK.B", "BRKb")]
    [InlineData("BRK-B", "BRKb")]
    [InlineData("BRKb", "BRKb")]
    [InlineData(" KO ", "KO")]
    public void QueryUsesKiwoomStyleClassShare(string symbol, string query) =>
        Assert.Equal(query, NaverStockNameClient.ToQuery(symbol));

    [Fact]
    public void ParsePicksUsStockWithSameTicker()
    {
        Assert.Equal("코카콜라", NaverStockNameClient.ParseName("KO", KoJson));
        Assert.Null(NaverStockNameClient.ParseName("KOF", KoJson)); // 앞부분만 같은 티커는 아님

        // 클래스주(code "BRK B") · 거래소 접미사(reutersCode "SCHD.K")
        const string json = """
            {"items":[
              {"code":"BRK B","name":"버크셔 해서웨이 Class B","reutersCode":"BRKb","nationCode":"USA"},
              {"code":"SCHD","name":"Schwab US Dividend Equity ETF","reutersCode":"SCHD.K","nationCode":"USA"}]}
            """;
        Assert.Equal("버크셔 해서웨이 Class B", NaverStockNameClient.ParseName("BRK.B", json));
        Assert.Equal("Schwab US Dividend Equity ETF", NaverStockNameClient.ParseName("SCHD", json));
        Assert.Null(NaverStockNameClient.ParseName("KO", "not json"));
    }

    [Fact]
    public async Task NameIsCachedForSevenDays()
    {
        var db = NewDb();
        var handler = new FakeNaver(KoJson);
        using var client = new NaverStockNameClient(handler);
        var service = new StockNameService(db, client);

        Assert.Equal("코카콜라", await service.GetNameAsync("KO"));
        _now = _now.AddDays(6);
        Assert.Equal("코카콜라", await service.GetNameAsync("KO"));
        Assert.Equal(1, handler.Calls);
        Assert.Equal("코카콜라", service.CachedNames()["KO"]);

        // 7일이 지나면 다시 받는다. 실패하면 예전 이름을 쓴다
        _now = _now.AddDays(2);
        handler.Fail = true;
        Assert.Equal("코카콜라", await service.GetNameAsync("KO"));
        Assert.Equal(2, handler.Calls);
    }

    [Fact]
    public async Task NotFoundIsCachedToo()
    {
        var db = NewDb();
        var handler = new FakeNaver("""{"items":[]}""");
        using var client = new NaverStockNameClient(handler);
        var service = new StockNameService(db, client);

        Assert.Null(await service.GetNameAsync("ZZZZ"));
        Assert.Null(await service.GetNameAsync("ZZZZ"));
        Assert.Equal(1, handler.Calls);
        Assert.Empty(service.CachedNames());
    }

    private sealed class FakeNaver(string json) : HttpMessageHandler
    {
        public int Calls { get; private set; }
        public bool Fail { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Calls++;
            return Task.FromResult(Fail
                ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                : new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") });
        }
    }
}
