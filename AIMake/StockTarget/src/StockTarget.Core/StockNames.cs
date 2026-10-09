using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace StockTarget.Core;

/// <summary>
/// 네이버 증권 자동완성 검색(ac.stock.naver.com)으로 미국 종목의 한글명을 찾는다.
/// 비공식 API라 형식이 바뀌거나 막힐 수 있다. 찾지 못하면 null.
/// </summary>
public sealed partial class NaverStockNameClient : IDisposable
{
    private const string BaseUrl = "https://ac.stock.naver.com/ac?target=stock&q=";
    private readonly HttpClient _http;

    public NaverStockNameClient(HttpMessageHandler? handler = null)
    {
        handler ??= new HttpClientHandler { AutomaticDecompression = DecompressionMethods.All };
        _http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };
        _http.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0 Safari/537.36");
    }

    public async Task<string?> GetNameAsync(string symbol, CancellationToken ct = default)
    {
        using var resp = await _http.GetAsync(BaseUrl + Uri.EscapeDataString(ToQuery(symbol)), ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        return ParseName(symbol, body);
    }

    /// <summary>
    /// 검색어. 네이버는 클래스주를 키움식(BRKb)으로만 찾으므로 BRK.B · BRK-B · BRK/B를 BRKb로 바꾼다.
    /// </summary>
    public static string ToQuery(string symbol)
    {
        symbol = symbol.Trim();
        var m = ClassShare().Match(symbol);
        return m.Success ? m.Groups[1].Value + char.ToLowerInvariant(m.Groups[2].Value[0]) : symbol;
    }

    /// <summary>검색 결과 JSON에서 티커가 같은 미국 종목의 이름. 없으면 null(테스트에서 직접 호출).</summary>
    public static string? ParseName(string symbol, string json)
    {
        var key = Key(symbol);
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array)
                return null;
            foreach (var item in items.EnumerateArray())
            {
                if (Get(item, "nationCode") != "USA" || Get(item, "name") is not { Length: > 0 } name)
                    continue;
                // code: "BRK B" · reutersCode: "BRKb", "SCHD.K"(거래소 접미사)
                var reuters = Get(item, "reutersCode")?.Split('.')[0];
                if (Key(Get(item, "code")) == key || Key(reuters) == key)
                    return name;
            }
        }
        catch (JsonException)
        {
        }
        return null;
    }

    /// <summary>비교용 티커: 영문·숫자만 대문자로(BRK.B · BRK B · BRKb → BRKB).</summary>
    private static string Key(string? s) =>
        s is null ? "" : new string(s.Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());

    private static string? Get(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    [GeneratedRegex(@"^([A-Za-z]+)[.\-/ ]([A-Za-z])$")]
    private static partial Regex ClassShare();

    public void Dispose() => _http.Dispose();
}

/// <summary>
/// 티커 → 한글 종목명. 조회 결과(찾지 못함 포함)를 7일간 DB에 캐시한다.
/// 조회에 실패하면 예전에 받은 이름을 쓰고, 그것도 없으면 null(화면은 영문명으로 대신한다).
/// </summary>
public sealed class StockNameService(StockDatabase db, NaverStockNameClient client)
{
    public static readonly TimeSpan CacheTtl = TimeSpan.FromDays(7);

    public async Task<string?> GetNameAsync(string symbol, bool force = false, CancellationToken ct = default)
    {
        symbol = StockService.Normalize(symbol);
        var cached = db.GetStockName(symbol);
        if (!force && cached is { } c && c.Age < CacheTtl)
            return c.Name;
        try
        {
            var name = await client.GetNameAsync(symbol, ct).ConfigureAwait(false);
            db.PutStockName(symbol, name);
            return name;
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            return cached?.Name;
        }
    }

    /// <summary>캐시에 있는 한글명(기간과 상관없이). 네트워크 없이 바로 표시할 때 쓴다.</summary>
    public IReadOnlyDictionary<string, string> CachedNames() => db.GetStockNames();
}
