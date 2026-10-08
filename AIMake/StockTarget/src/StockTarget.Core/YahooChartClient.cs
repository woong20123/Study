using System.Net;
using System.Text.Json;

namespace StockTarget.Core;

/// <summary>
/// Yahoo Finance chart API(v8) 클라이언트. 한 번의 호출로 현재가·일별 종가·배당·분할을 받는다.
/// .NET HttpClient는 Windows 인증서 저장소를 쓰므로 회사망 SSL 검사 환경에서도 별도 CA 설정이 필요 없다.
/// 비공식 API라 형식이 바뀌거나 일시적으로 실패할 수 있다.
/// </summary>
public sealed class YahooChartClient : IDisposable
{
    private const string BaseUrl = "https://query2.finance.yahoo.com/v8/finance/chart/";
    private readonly HttpClient _http;

    public YahooChartClient(HttpMessageHandler? handler = null)
    {
        handler ??= new HttpClientHandler
        {
            CookieContainer = new CookieContainer(),
            AutomaticDecompression = DecompressionMethods.All,
        };
        _http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(20) };
        // 기본 UA로는 Yahoo가 429를 돌려준다
        _http.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0 Safari/537.36");
    }

    /// <param name="range">1d, 5d, 1mo, 6y 등</param>
    public async Task<ChartData> GetChartAsync(string symbol, string range, bool withEvents, CancellationToken ct = default)
    {
        var url = $"{BaseUrl}{Uri.EscapeDataString(symbol)}?range={range}&interval=1d"
                  + (withEvents ? "&events=div,split" : "");
        using var resp = await _http.GetAsync(url, ct).ConfigureAwait(false);
        var body = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        return Parse(symbol, body, (int)resp.StatusCode);
    }

    /// <summary>chart API 응답 JSON을 ChartData로 바꾼다(테스트에서 직접 호출).</summary>
    public static ChartData Parse(string symbol, string json, int statusCode = 200)
    {
        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(json);
        }
        catch (JsonException)
        {
            throw new StockDataException($"{symbol}: 응답을 해석할 수 없습니다 (HTTP {statusCode})");
        }

        using (doc)
        {
            var chart = doc.RootElement.GetProperty("chart");
            if (chart.TryGetProperty("error", out var err) && err.ValueKind == JsonValueKind.Object)
            {
                var desc = err.TryGetProperty("description", out var d) ? d.GetString() : "알 수 없는 오류";
                throw new StockDataException($"{symbol}: 시세 없음 (티커 확인 필요) — {desc}");
            }

            var result = chart.GetProperty("result")[0];
            var meta = result.GetProperty("meta");
            var offset = TimeSpan.FromSeconds(GetDouble(meta, "gmtoffset") ?? 0);
            DateOnly ToDate(long ts) => DateOnly.FromDateTime(DateTimeOffset.FromUnixTimeSeconds(ts).ToOffset(offset).DateTime);

            var bars = new List<PriceBar>();
            if (result.TryGetProperty("timestamp", out var tsArr) &&
                result.TryGetProperty("indicators", out var ind) &&
                ind.TryGetProperty("quote", out var quoteArr) && quoteArr.GetArrayLength() > 0 &&
                quoteArr[0].TryGetProperty("close", out var closes))
            {
                for (int i = 0; i < tsArr.GetArrayLength() && i < closes.GetArrayLength(); i++)
                {
                    if (closes[i].ValueKind == JsonValueKind.Number)
                        bars.Add(new PriceBar(ToDate(tsArr[i].GetInt64()), closes[i].GetDouble()));
                }
            }

            var dividends = new List<DividendEvent>();
            var splits = new List<SplitEvent>();
            if (result.TryGetProperty("events", out var events))
            {
                if (events.TryGetProperty("dividends", out var divs))
                {
                    foreach (var p in divs.EnumerateObject())
                        dividends.Add(new DividendEvent(ToDate(p.Value.GetProperty("date").GetInt64()), p.Value.GetProperty("amount").GetDouble()));
                }
                if (events.TryGetProperty("splits", out var sp))
                {
                    foreach (var p in sp.EnumerateObject())
                    {
                        var num = p.Value.GetProperty("numerator").GetDouble();
                        var den = p.Value.GetProperty("denominator").GetDouble();
                        if (num > 0 && den > 0)
                            splits.Add(new SplitEvent(ToDate(p.Value.GetProperty("date").GetInt64()), num / den));
                    }
                }
            }

            return new ChartData(
                Symbol: GetString(meta, "symbol") ?? symbol,
                Currency: GetString(meta, "currency") ?? "",
                Exchange: GetString(meta, "exchangeName") ?? "",
                Name: GetString(meta, "longName") ?? GetString(meta, "shortName") ?? "",
                RegularMarketPrice: GetDouble(meta, "regularMarketPrice"),
                PreviousClose: GetDouble(meta, "chartPreviousClose") ?? GetDouble(meta, "previousClose"),
                Bars: bars.OrderBy(b => b.Date).ToList(),
                Dividends: dividends.OrderBy(d => d.Date).ToList(),
                Splits: splits.OrderBy(s => s.Date).ToList());
        }
    }

    private static string? GetString(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static double? GetDouble(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDouble() : null;

    public void Dispose() => _http.Dispose();
}

/// <summary>시세 데이터를 얻지 못했을 때(없는 티커, 응답 형식 오류 등).</summary>
public sealed class StockDataException(string message) : Exception(message);
