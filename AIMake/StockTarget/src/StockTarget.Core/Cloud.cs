using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace StockTarget.Core;

/// <summary>
/// Firebase 프로젝트 설정(%LOCALAPPDATA%\StockTarget\firebase.json). 비밀값이 아니라 프로젝트 식별 정보다.
/// <list type="bullet">
/// <item>ApiKey             : Firebase 프로젝트 설정 → 웹 API 키</item>
/// <item>DatabaseUrl        : Realtime Database 주소 (https://프로젝트-default-rtdb.지역.firebasedatabase.app)</item>
/// <item>GoogleClientId     : Google Cloud 콘솔 OAuth 클라이언트 ID(유형: 데스크톱 앱)</item>
/// <item>GoogleClientSecret : 같은 클라이언트의 보안 비밀(데스크톱 앱은 공개 클라이언트라 숨길 수 없는 값)</item>
/// </list>
/// </summary>
public sealed record FirebaseConfig(string ApiKey, string DatabaseUrl, string GoogleClientId, string GoogleClientSecret)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public bool IsComplete =>
        !string.IsNullOrWhiteSpace(ApiKey) && !string.IsNullOrWhiteSpace(DatabaseUrl) &&
        !string.IsNullOrWhiteSpace(GoogleClientId) && !string.IsNullOrWhiteSpace(GoogleClientSecret);

    public static string DefaultPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "StockTarget", "firebase.json");

    /// <summary>설정 파일을 읽는다. 없으면 빈 양식을 만들어 두고 null.</summary>
    public static FirebaseConfig? Load(string path)
    {
        if (!File.Exists(path))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(new FirebaseConfig("", "", "", ""), JsonOptions));
            return null;
        }
        try
        {
            var c = JsonSerializer.Deserialize<FirebaseConfig>(File.ReadAllText(path));
            return c is { IsComplete: true } ? c with { DatabaseUrl = c.DatabaseUrl.Trim().TrimEnd('/') } : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    // 웹 API 키 · 클라이언트 보안 비밀이 로그에 찍히지 않도록
    public override string ToString() => $"FirebaseConfig({DatabaseUrl})";
}

/// <summary>
/// Firebase 로그인 상태. Uid는 Firebase가 Google 계정마다 하나씩 발급하는 고유 키로, 저장 경로 users/{Uid}에 쓴다.
/// GoogleId는 Google 계정 고유번호(sub, 숫자)다. 같은 Google 계정이면 어느 기기에서 로그인해도 둘 다 같다.
/// </summary>
public sealed record FirebaseSession(
    string Uid,
    string? Email,
    string? GoogleId,
    string IdToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt)
{
    public bool IsExpired(DateTimeOffset now) => now >= ExpiresAt - TimeSpan.FromMinutes(5);

    // 토큰이 로그에 찍히지 않도록
    public override string ToString() => $"FirebaseSession({Email}, {Uid})";
}

/// <summary>Google OAuth(데스크톱 앱 · PKCE · 루프백 리디렉션) 중 플랫폼과 무관한 부분.</summary>
public static class GoogleOAuth
{
    public const string AuthEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
    public const string TokenEndpoint = "https://oauth2.googleapis.com/token";

    /// <summary>PKCE: (code_verifier, code_challenge = BASE64URL(SHA256(verifier))).</summary>
    public static (string Verifier, string Challenge) CreatePkce()
    {
        var verifier = Base64Url(RandomNumberGenerator.GetBytes(32));
        return (verifier, Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier))));
    }

    public static string CreateState() => Base64Url(RandomNumberGenerator.GetBytes(16));

    public static string BuildAuthUrl(string clientId, string redirectUri, string codeChallenge, string state) =>
        AuthEndpoint + "?" + Query(
            ("client_id", clientId),
            ("redirect_uri", redirectUri),
            ("response_type", "code"),
            ("scope", "openid email"),
            ("code_challenge", codeChallenge),
            ("code_challenge_method", "S256"),
            ("state", state),
            ("prompt", "select_account"));

    /// <summary>인가 코드 → Google ID 토큰(JWT).</summary>
    public static async Task<string> ExchangeCodeAsync(
        HttpClient http, FirebaseConfig config, string code, string verifier, string redirectUri, CancellationToken ct = default)
    {
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = config.GoogleClientId,
            ["client_secret"] = config.GoogleClientSecret,
            ["code"] = code,
            ["code_verifier"] = verifier,
            ["redirect_uri"] = redirectUri,
            ["grant_type"] = "authorization_code",
        });
        using var resp = await http.PostAsync(TokenEndpoint, content, ct).ConfigureAwait(false);
        var root = await CloudJson.ReadAsync(resp, "Google 토큰 교환", ct).ConfigureAwait(false);
        return CloudJson.GetString(root, "id_token") ?? throw new CloudException("Google 응답에 id_token이 없습니다");
    }

    internal static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static string Query(params (string Key, string Value)[] items) =>
        string.Join("&", items.Select(i => $"{Uri.EscapeDataString(i.Key)}={Uri.EscapeDataString(i.Value)}"));
}

/// <summary>
/// Firebase Authentication(REST) + Realtime Database(REST).
/// 백업은 users/{uid}/stocktarget 한 곳에 통째로 저장한다(기기 간 공유, 마지막 저장이 이긴다).
/// 보안 규칙으로 본인 uid 경로만 읽고 쓰게 해야 한다(문서 참고).
/// </summary>
public sealed class FirebaseClient : IDisposable
{
    private const string SignInUrl = "https://identitytoolkit.googleapis.com/v1/accounts:signInWithIdp";
    private const string RefreshUrl = "https://securetoken.googleapis.com/v1/token";

    private readonly FirebaseConfig _config;
    private readonly HttpClient _http;
    private readonly Func<DateTimeOffset> _now;

    public FirebaseClient(FirebaseConfig config, HttpMessageHandler? handler = null, Func<DateTimeOffset>? now = null)
    {
        _config = config;
        _http = handler is null ? new HttpClient() : new HttpClient(handler);
        _http.Timeout = TimeSpan.FromSeconds(30);
        _now = now ?? (() => DateTimeOffset.UtcNow);
    }

    public HttpClient Http => _http;

    /// <summary>Google ID 토큰으로 Firebase에 로그인한다. 처음이면 Firebase가 uid를 새로 발급한다.</summary>
    public async Task<FirebaseSession> SignInWithGoogleAsync(string googleIdToken, CancellationToken ct = default)
    {
        var body = new
        {
            postBody = $"id_token={Uri.EscapeDataString(googleIdToken)}&providerId=google.com",
            requestUri = "http://localhost",
            returnSecureToken = true,
            returnIdpCredential = true,
        };
        using var resp = await _http.PostAsync($"{SignInUrl}?key={Uri.EscapeDataString(_config.ApiKey)}",
            JsonContent(body), ct).ConfigureAwait(false);
        var root = await CloudJson.ReadAsync(resp, "Firebase 로그인", ct).ConfigureAwait(false);

        // federatedId = "https://accounts.google.com/{sub}"
        var federated = CloudJson.GetString(root, "federatedId");
        return new FirebaseSession(
            CloudJson.GetString(root, "localId") ?? throw new CloudException("Firebase 응답에 uid(localId)가 없습니다"),
            CloudJson.GetString(root, "email"),
            federated?[(federated.LastIndexOf('/') + 1)..],
            CloudJson.GetString(root, "idToken") ?? throw new CloudException("Firebase 응답에 idToken이 없습니다"),
            CloudJson.GetString(root, "refreshToken") ?? throw new CloudException("Firebase 응답에 refreshToken이 없습니다"),
            _now().AddSeconds(ParseSeconds(CloudJson.GetString(root, "expiresIn"))));
    }

    /// <summary>만료가 가까우면 갱신 토큰으로 ID 토큰을 다시 받는다(브라우저 로그인 없이).</summary>
    public async Task<FirebaseSession> EnsureFreshAsync(FirebaseSession session, CancellationToken ct = default)
    {
        if (!session.IsExpired(_now()))
            return session;
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = session.RefreshToken,
        });
        using var resp = await _http.PostAsync($"{RefreshUrl}?key={Uri.EscapeDataString(_config.ApiKey)}", content, ct)
            .ConfigureAwait(false);
        var root = await CloudJson.ReadAsync(resp, "Firebase 토큰 갱신", ct).ConfigureAwait(false);
        return session with
        {
            IdToken = CloudJson.GetString(root, "id_token") ?? throw new CloudException("갱신 응답에 id_token이 없습니다"),
            RefreshToken = CloudJson.GetString(root, "refresh_token") ?? session.RefreshToken,
            ExpiresAt = _now().AddSeconds(ParseSeconds(CloudJson.GetString(root, "expires_in"))),
        };
    }

    /// <summary>백업을 users/{uid}/stocktarget 에 덮어쓴다.</summary>
    public async Task SaveBackupAsync(FirebaseSession session, BackupData backup, CancellationToken ct = default)
    {
        using var content = new StringContent(BackupSerializer.ToJson(backup), Encoding.UTF8, "application/json");
        using var resp = await _http.PutAsync(BackupUrl(session), content, ct).ConfigureAwait(false);
        await CloudJson.ReadAsync(resp, "Firebase 저장", ct).ConfigureAwait(false);
    }

    /// <summary>users/{uid}/stocktarget 의 백업. 아직 저장한 적이 없으면 null.</summary>
    public async Task<BackupData?> LoadBackupAsync(FirebaseSession session, CancellationToken ct = default)
    {
        using var resp = await _http.GetAsync(BackupUrl(session), ct).ConfigureAwait(false);
        var text = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode)
            throw new CloudException($"Firebase 읽기 실패 (HTTP {(int)resp.StatusCode}): {CloudJson.ErrorMessage(text)}");
        return text.Trim() == "null" ? null : BackupSerializer.FromJson(text);
    }

    public void Dispose() => _http.Dispose();

    private string BackupUrl(FirebaseSession session) =>
        $"{_config.DatabaseUrl}/users/{Uri.EscapeDataString(session.Uid)}/stocktarget.json?auth={Uri.EscapeDataString(session.IdToken)}";

    private static StringContent JsonContent(object body) =>
        new(JsonSerializer.Serialize(body), Encoding.UTF8, new MediaTypeHeaderValue("application/json").MediaType!);

    private static double ParseSeconds(string? s) => double.TryParse(s, out var v) && v > 0 ? v : 3600;
}

/// <summary>Google · Firebase 응답 공통 처리.</summary>
internal static class CloudJson
{
    public static async Task<JsonElement> ReadAsync(HttpResponseMessage resp, string what, CancellationToken ct)
    {
        var text = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode)
            throw new CloudException($"{what} 실패 (HTTP {(int)resp.StatusCode}): {ErrorMessage(text)}");
        try
        {
            using var doc = JsonDocument.Parse(text);
            return doc.RootElement.Clone();
        }
        catch (JsonException)
        {
            throw new CloudException($"{what}: 응답을 해석할 수 없습니다");
        }
    }

    public static string? GetString(JsonElement e, string name) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString()
            : null;

    /// <summary>{"error":{"message":".."}} (Google) / {"error":".."} (Realtime Database) / 그 외 원문 일부.</summary>
    public static string ErrorMessage(string text)
    {
        try
        {
            using var doc = JsonDocument.Parse(text);
            if (doc.RootElement.TryGetProperty("error", out var err))
            {
                if (err.ValueKind == JsonValueKind.String)
                    return err.GetString() ?? "";
                if (err.ValueKind == JsonValueKind.Object)
                    return GetString(err, "message") ?? err.ToString();
            }
            if (GetString(doc.RootElement, "error_description") is { } desc)
                return desc;
        }
        catch (JsonException)
        {
        }
        return text.Length > 200 ? text[..200] : text;
    }
}

/// <summary>Google · Firebase 통신 오류.</summary>
public sealed class CloudException(string message) : Exception(message);

