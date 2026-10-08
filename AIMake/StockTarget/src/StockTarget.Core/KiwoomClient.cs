using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace StockTarget.Core;

/// <summary>
/// 키움 REST API 접속 정보. 앱키·시크릿키는 코드나 DB에 두지 않고 사용자 환경변수에서 읽는다.
/// <list type="bullet">
/// <item>KIWOOM_APPKEY, KIWOOM_SECRETKEY : 키움 REST API 앱키 · 시크릿키</item>
/// <item>KIWOOM_ENV : real 이면 실전(api.kiwoom.com), 그 외(기본)는 모의투자(mockapi.kiwoom.com)</item>
/// </list>
/// 구동 옵션 <c>--kiwoom off|mock|real</c>(<see cref="KiwoomMode"/>)이 있으면 KIWOOM_ENV보다 우선한다.
/// </summary>
public sealed record KiwoomOptions(string AppKey, string SecretKey, bool IsMock)
{
    public const string AppKeyVariable = "KIWOOM_APPKEY";
    public const string SecretKeyVariable = "KIWOOM_SECRETKEY";
    public const string EnvVariable = "KIWOOM_ENV";

    public string BaseUrl => IsMock ? "https://mockapi.kiwoom.com" : "https://api.kiwoom.com";

    public string EnvText => IsMock ? "모의투자" : "실전";

    /// <summary>
    /// 환경변수에서 읽는다. 연동을 껐거나(<see cref="KiwoomMode.Off"/>) 키가 없으면 null.
    /// 실전 · 모의투자는 구동 옵션(mode)이 우선이고, Auto면 KIWOOM_ENV를 따른다.
    /// </summary>
    public static KiwoomOptions? FromEnvironment(KiwoomMode mode = KiwoomMode.Auto, Func<string, string?>? get = null)
    {
        get ??= Get;
        if (mode == KiwoomMode.Off)
            return null;
        var appKey = get(AppKeyVariable);
        var secretKey = get(SecretKeyVariable);
        if (string.IsNullOrWhiteSpace(appKey) || string.IsNullOrWhiteSpace(secretKey))
            return null;
        var isReal = mode switch
        {
            KiwoomMode.Real => true,
            KiwoomMode.Mock => false,
            _ => string.Equals(get(EnvVariable)?.Trim(), "real", StringComparison.OrdinalIgnoreCase),
        };
        return new KiwoomOptions(appKey.Trim(), secretKey.Trim(), !isReal);
    }

    /// <summary>상태 표시줄용 한 줄: "키움: 모의투자" / "키움: 실전(--kiwoom real)" / "키움: 꺼짐(--kiwoom off)" / "키움: 키 미설정".</summary>
    public static string StatusText(KiwoomMode mode, KiwoomOptions? options) => (mode, options) switch
    {
        (KiwoomMode.Off, _) => "키움: 꺼짐(--kiwoom off)",
        (_, null) => "키움: 키 미설정",
        (KiwoomMode.Auto, { } o) => $"키움: {o.EnvText}",
        (_, { } o) => $"키움: {o.EnvText}(--kiwoom {(o.IsMock ? "mock" : "real")})",
    };

    // 앱 실행 후 setx로 설정한 값도 읽도록 프로세스 → 사용자 환경변수 순으로 찾는다
    private static string? Get(string name) =>
        Environment.GetEnvironmentVariable(name) ??
        (OperatingSystem.IsWindows() ? Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.User) : null);

    // 키가 로그·예외 메시지에 찍히지 않도록 레코드 기본 ToString을 막는다
    public override string ToString() => $"KiwoomOptions({EnvText})";
}

/// <summary>예약 매수 접수 결과.</summary>
public sealed record ReservationReceipt(string ReservationNo, string ScheduledDate);

/// <summary>
/// 키움 REST API 클라이언트(미국주식 예약 매수에 필요한 것만).
/// 모든 요청은 POST + JSON 이고, 호출할 기능은 헤더 api-id(TR 코드)로 정한다.
/// <list type="bullet">
/// <item>au10001  접근토큰 발급   POST /oauth2/token</item>
/// <item>usa10098 거래소구분 조회 POST /api/us/stkinfo</item>
/// <item>ust21200 예약 매수주문   POST /api/us/ordr</item>
/// </list>
/// </summary>
public sealed class KiwoomClient : IDisposable
{
    /// <summary>예약주문구분: 2 = 기간예약(잔량주문). 기간 동안 매일 남은 수량만 다시 주문하고, 다 체결되면 끝난다.</summary>
    public const string PeriodRemainingOrder = "2";

    /// <summary>매매구분 30 = LOC(Limit On Close).</summary>
    public const string TradeTypeLoc = "30";

    private static readonly TimeSpan TokenMargin = TimeSpan.FromMinutes(5);

    private readonly KiwoomOptions _options;
    private readonly HttpClient _http;
    private string? _token;
    private DateTime _tokenExpires;

    public KiwoomClient(KiwoomOptions options, HttpMessageHandler? handler = null)
    {
        _options = options;
        _http = handler is null ? new HttpClient() : new HttpClient(handler);
        _http.BaseAddress = new Uri(options.BaseUrl);
        _http.Timeout = TimeSpan.FromSeconds(20);
    }

    public KiwoomOptions Options => _options;

    /// <summary>티커의 거래소구분(ND: NASDAQ, NY: NYSE, NA: AMEX).</summary>
    public async Task<string> GetExchangeAsync(string symbol, CancellationToken ct = default)
    {
        var root = await PostAsync("usa10098", "/api/us/stkinfo", new { stk_cd = symbol }, ct).ConfigureAwait(false);
        if (root.TryGetProperty("list", out var list) && list.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in list.EnumerateArray())
            {
                if (string.Equals(GetString(item, "stk_cd"), symbol, StringComparison.OrdinalIgnoreCase) &&
                    GetString(item, "stex_tp") is { Length: > 0 } stex)
                    return stex;
            }
        }
        throw new KiwoomException(null, $"{symbol}: 키움 미국주식 종목을 찾지 못했습니다");
    }

    /// <summary>LOC 기간예약(잔량주문) 매수. 같은 주문을 두 번 보내면 두 건이 된다(중복 방지는 호출하는 쪽에서).</summary>
    public async Task<ReservationReceipt> ReserveBuyLocAsync(ReservationOrder order, string exchange, CancellationToken ct = default)
    {
        var body = new
        {
            rsrv_ord_tp = PeriodRemainingOrder,
            rsrv_strt_dt = order.Start.ToString("yyyyMMdd", CultureInfo.InvariantCulture),
            rsrv_end_dt = order.End.ToString("yyyyMMdd", CultureInfo.InvariantCulture),
            stex_tp = exchange,
            stk_cd = order.Symbol,
            ord_qty = order.Quantity.ToString(CultureInfo.InvariantCulture),
            ord_uv = order.PriceText,
            trde_tp = TradeTypeLoc,
        };
        var root = await PostAsync("ust21200", "/api/us/ordr", body, ct).ConfigureAwait(false);
        return new ReservationReceipt(GetString(root, "rsrv_ord_no") ?? "", GetString(root, "frcs_dt") ?? "");
    }

    public void Dispose() => _http.Dispose();

    private async Task<string> GetTokenAsync(CancellationToken ct)
    {
        if (_token is not null && DateTime.Now < _tokenExpires - TokenMargin)
            return _token;

        var body = new { grant_type = "client_credentials", appkey = _options.AppKey, secretkey = _options.SecretKey };
        var root = await SendAsync("au10001", "/oauth2/token", body, token: null, ct).ConfigureAwait(false);
        _token = GetString(root, "token") ?? throw new KiwoomException(null, "접근토큰 응답에 token이 없습니다");
        _tokenExpires = DateTime.TryParseExact(GetString(root, "expires_dt"), "yyyyMMddHHmmss",
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var exp)
            ? exp
            : DateTime.Now.AddHours(1);
        return _token;
    }

    private async Task<JsonElement> PostAsync(string apiId, string path, object body, CancellationToken ct)
    {
        var token = await GetTokenAsync(ct).ConfigureAwait(false);
        return await SendAsync(apiId, path, body, token, ct).ConfigureAwait(false);
    }

    private async Task<JsonElement> SendAsync(string apiId, string path, object body, string? token, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"),
        };
        req.Headers.Add("api-id", apiId);
        if (token is not null)
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var resp = await _http.SendAsync(req, ct).ConfigureAwait(false);
        var text = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        JsonElement root;
        try
        {
            using var doc = JsonDocument.Parse(text);
            root = doc.RootElement.Clone();
        }
        catch (JsonException)
        {
            throw new KiwoomException(null, $"{apiId}: 응답을 해석할 수 없습니다 (HTTP {(int)resp.StatusCode})");
        }

        // return_code 0 = 정상. 그 외는 키움 오류(메시지에 사유)
        var code = root.TryGetProperty("return_code", out var c) && c.ValueKind == JsonValueKind.Number ? c.GetInt32() : (int?)null;
        if (!resp.IsSuccessStatusCode || code is not (null or 0))
            throw new KiwoomException(code, $"{apiId}: {GetString(root, "return_msg") ?? $"HTTP {(int)resp.StatusCode}"}");
        return root;
    }

    private static string? GetString(JsonElement e, string name) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString()
            : null;
}

/// <summary>키움 API 오류(return_code ≠ 0, HTTP 오류, 응답 형식 오류).</summary>
public sealed class KiwoomException(int? code, string message) : Exception(message)
{
    public int? Code { get; } = code;
}
