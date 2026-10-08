using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using StockTarget.Core;

namespace StockTarget.Tests;

public sealed class BackupTests : IDisposable
{
    private readonly List<string> _paths = [];
    private readonly DateTimeOffset _now = new(2026, 10, 8, 1, 0, 0, TimeSpan.Zero);

    public void Dispose()
    {
        foreach (var p in _paths.Where(File.Exists))
            File.Delete(p);
    }

    private StockDatabase NewDb()
    {
        var path = Path.Combine(Path.GetTempPath(), $"stocktarget_bak_{Guid.NewGuid():N}.db");
        _paths.Add(path);
        return new StockDatabase(path, () => _now);
    }

    private static readonly TargetPlan Ko = new("KO", new DateOnly(2026, 10, 8), 2030, 0, 0, 10, 2.92, "메모 \"따옴표\"",
        new BuyAmounts(1_000_000, null, 3_000_000), 120);

    private static readonly TargetPlan Msft = new("MSFT", new DateOnly(2026, 10, 8), 2030, 27.5, 25, 11, 0.8);

    private static StockDatabase Fill(StockDatabase db)
    {
        db.SaveTarget(Ko);
        db.SaveTarget(Msft);
        db.RecordCheck(new PriceCheck("KO", new DateOnly(2026, 10, 8), "2026Q4", 85.82, 89.86));
        db.RecordCheck(new PriceCheck("MSFT", new DateOnly(2026, 10, 8), "2026Q4", 529.76, 455.7));
        db.SaveReservation(new ReservationRecord("KO", "매수", new DateOnly(2026, 10, 12), new DateOnly(2026, 10, 16),
            89.85, 8, 1_000_000, "모의투자", "000000000026", "20261012", new DateTimeOffset(2026, 10, 8, 10, 0, 0, TimeSpan.FromHours(9))));
        return db;
    }

    [Fact]
    public void RoundTripThroughJsonToAnotherDevice()
    {
        var source = Fill(NewDb());
        var json = BackupSerializer.ToJson(source.ExportBackup());

        var target = NewDb(); // 다른 기기
        target.ReplaceWithBackup(BackupSerializer.FromJson(json));

        Assert.Equal(source.GetTargets(), target.GetTargets());
        Assert.Equal(source.GetChecks(null), target.GetChecks(null));
        Assert.Equal(source.GetReservations(), target.GetReservations());
        Assert.Equal(Ko, target.GetTargets().Single(t => t.Symbol == "KO")); // 직접 입력 목표 주가 · 금액 · 메모 유지
    }

    [Fact]
    public void JsonFormatIsStableAndExcludesCache()
    {
        var db = Fill(NewDb());
        db.PutCache("quote:KO", new Quote("KO", "", 1, 1, "USD", "", null));
        var json = BackupSerializer.ToJson(db.ExportBackup());
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal("stocktarget-backup", root.GetProperty("format").GetString());
        Assert.Equal(1, root.GetProperty("version").GetInt32());
        Assert.False(root.TryGetProperty("cache", out _));
        var ko = root.GetProperty("targets").EnumerateArray().First(t => t.GetProperty("symbol").GetString() == "KO");
        Assert.Equal("2026-10-08", ko.GetProperty("inputDate").GetString());
        Assert.Equal(120, ko.GetProperty("targetPrice").GetDouble());
        Assert.False(ko.TryGetProperty("mustBuyKrw", out _)); // null은 생략
        Assert.False(ko.TryGetProperty("growthPct", out _));  // 계산 값은 넣지 않음
    }

    [Fact]
    public void ReplaceRemovesDataNotInBackup()
    {
        var db = Fill(NewDb());
        var onlyMsft = new BackupData { ExportedAt = _now, Targets = [BackupTarget.From(Msft)] };
        db.ReplaceWithBackup(onlyMsft);

        Assert.Equal("MSFT", Assert.Single(db.GetTargets()).Symbol);
        Assert.Empty(db.GetChecks(null));
        Assert.Empty(db.GetReservations());
    }

    [Fact]
    public void MissingArraysFromRealtimeDatabaseAreEmpty()
    {
        // Realtime Database는 빈 배열을 저장하지 않아 키가 아예 없다
        var data = BackupSerializer.FromJson("""{"format":"stocktarget-backup","version":1,"exportedAt":"2026-10-08T01:00:00+00:00"}""");
        Assert.Empty(data.ToTargets());
        Assert.Empty(data.ToChecks());
        Assert.Empty(data.ToReservations());
        Assert.Equal("목표 0개 · 확인 이력 0건 · 예약 이력 0건", data.Summary);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("""{"format":"other","version":1}""")]
    [InlineData("""{"format":"stocktarget-backup","version":99}""")]
    public void RejectsForeignOrNewerFiles(string json) =>
        Assert.Throws<BackupFormatException>(() => BackupSerializer.FromJson(json));

    [Fact]
    public void InvalidBackupLeavesDatabaseUntouched()
    {
        var db = Fill(NewDb());
        var bad = new BackupData
        {
            ExportedAt = _now,
            Targets = [BackupTarget.From(Ko), BackupTarget.From(Ko)], // 티커 중복
        };
        Assert.Throws<BackupFormatException>(() => db.ReplaceWithBackup(bad));
        var invalid = new BackupData { ExportedAt = _now, Targets = [BackupTarget.From(Msft with { Eps = 0 })] };
        Assert.Throws<BackupFormatException>(() => db.ReplaceWithBackup(invalid));
        Assert.Equal(2, db.GetTargets().Count); // 그대로
    }
}

public sealed class CloudTests
{
    private static readonly FirebaseConfig Config =
        new("WEB-API-KEY", "https://demo-default-rtdb.asia-southeast1.firebasedatabase.app", "CLIENT.apps.googleusercontent.com", "CLIENT-SECRET");

    private DateTimeOffset _now = new(2026, 10, 8, 1, 0, 0, TimeSpan.Zero);
    private readonly FakeCloud _fake = new();

    private FirebaseClient Client() => new(Config, _fake, () => _now);

    [Fact]
    public void PkceChallengeIsSha256OfVerifier()
    {
        var (verifier, challenge) = GoogleOAuth.CreatePkce();
        Assert.Matches("^[A-Za-z0-9_-]{43}$", verifier);
        var expected = Convert.ToBase64String(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        Assert.Equal(expected, challenge);
        Assert.NotEqual(GoogleOAuth.CreatePkce().Verifier, verifier);
    }

    [Fact]
    public void AuthUrlHasPkceAndLoopbackRedirect()
    {
        var url = GoogleOAuth.BuildAuthUrl(Config.GoogleClientId, "http://127.0.0.1:51234/", "CHALLENGE", "STATE");
        Assert.StartsWith("https://accounts.google.com/o/oauth2/v2/auth?", url);
        Assert.Contains("redirect_uri=http%3A%2F%2F127.0.0.1%3A51234%2F", url);
        Assert.Contains("code_challenge=CHALLENGE", url);
        Assert.Contains("code_challenge_method=S256", url);
        Assert.Contains("scope=openid%20email", url);
        Assert.Contains("state=STATE", url);
    }

    [Fact]
    public async Task ExchangeCodeReturnsGoogleIdToken()
    {
        using var http = new HttpClient(_fake);
        var token = await GoogleOAuth.ExchangeCodeAsync(http, Config, "CODE", "VERIFIER", "http://127.0.0.1:51234/");
        Assert.Equal("GOOGLE-ID-TOKEN", token);
        var form = _fake.Requests.Single().Body;
        Assert.Contains("code_verifier=VERIFIER", form);
        Assert.Contains("grant_type=authorization_code", form);
    }

    [Fact]
    public async Task SignInGivesUidAndGoogleNumber()
    {
        using var client = Client();
        var s = await client.SignInWithGoogleAsync("GOOGLE-ID-TOKEN");

        Assert.Equal("FIREBASE-UID-1", s.Uid);
        Assert.Equal("110248495921238986420", s.GoogleId); // federatedId 끝의 Google 계정 번호
        Assert.Equal("user@example.com", s.Email);
        Assert.Equal(_now.AddSeconds(3600), s.ExpiresAt);
        var req = _fake.Requests.Single();
        Assert.Equal("https://identitytoolkit.googleapis.com/v1/accounts:signInWithIdp?key=WEB-API-KEY", req.Url);
        Assert.Contains("id_token=GOOGLE-ID-TOKEN", req.Body);
        Assert.Contains("providerId=google.com", req.Body);
        Assert.DoesNotContain("ID-TOKEN-1", s.ToString()); // 토큰은 ToString에 나오지 않음
    }

    [Fact]
    public async Task SaveAndLoadUseOwnUidPath()
    {
        using var client = Client();
        var s = await client.SignInWithGoogleAsync("GOOGLE-ID-TOKEN");
        var backup = new BackupData { ExportedAt = _now, Targets = [BackupTarget.From(new TargetPlan("KO", new DateOnly(2026, 10, 8), 2030, 4, 30, 10, 3))] };

        Assert.Null(await client.LoadBackupAsync(s)); // 아직 없음
        await client.SaveBackupAsync(s, backup);
        var loaded = await client.LoadBackupAsync(s);

        Assert.Equal("KO", Assert.Single(loaded!.ToTargets()).Symbol);
        var put = _fake.Requests.Single(r => r.Method == "PUT");
        Assert.Equal("https://demo-default-rtdb.asia-southeast1.firebasedatabase.app/users/FIREBASE-UID-1/stocktarget.json?auth=ID-TOKEN-1", put.Url);
    }

    [Fact]
    public async Task ExpiredTokenIsRefreshedWithoutBrowser()
    {
        using var client = Client();
        var s = await client.SignInWithGoogleAsync("GOOGLE-ID-TOKEN");
        Assert.Same(s, await client.EnsureFreshAsync(s)); // 아직 유효

        _now = _now.AddMinutes(56); // 만료 5분 전 안쪽
        var fresh = await client.EnsureFreshAsync(s);
        Assert.Equal("ID-TOKEN-2", fresh.IdToken);
        Assert.Equal("REFRESH-2", fresh.RefreshToken);
        Assert.Equal(s.Uid, fresh.Uid);
        var req = _fake.Requests.Last();
        Assert.StartsWith("https://securetoken.googleapis.com/v1/token?key=WEB-API-KEY", req.Url);
        Assert.Contains("refresh_token=REFRESH-1", req.Body);
    }

    [Fact]
    public async Task PermissionDeniedIsReported()
    {
        using var client = Client();
        var s = await client.SignInWithGoogleAsync("GOOGLE-ID-TOKEN");
        _fake.DenyDatabase = true; // 보안 규칙에 막힘
        var e = await Assert.ThrowsAsync<CloudException>(() => client.LoadBackupAsync(s));
        Assert.Contains("Permission denied", e.Message);
    }

    [Fact]
    public void ConfigToStringHidesSecrets()
    {
        Assert.DoesNotContain("CLIENT-SECRET", Config.ToString());
        Assert.DoesNotContain("WEB-API-KEY", Config.ToString());
        Assert.False((Config with { ApiKey = "" }).IsComplete);
    }

    private sealed record Captured(string Method, string Url, string Body);

    private sealed class FakeCloud : HttpMessageHandler
    {
        private string? _stored;

        public List<Captured> Requests { get; } = [];
        public bool DenyDatabase { get; set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var url = request.RequestUri!.ToString();
            var body = request.Content is null ? "" : await request.Content.ReadAsStringAsync(ct);
            Requests.Add(new Captured(request.Method.Method, url, body));

            if (url.Contains("firebasedatabase.app") && DenyDatabase)
                return Json(HttpStatusCode.Unauthorized, """{"error":"Permission denied"}""");
            if (url.StartsWith("https://oauth2.googleapis.com/token"))
                return Json(HttpStatusCode.OK, """{"id_token":"GOOGLE-ID-TOKEN","access_token":"x"}""");
            if (url.Contains("accounts:signInWithIdp"))
                return Json(HttpStatusCode.OK, """
                    {"localId":"FIREBASE-UID-1","email":"user@example.com","idToken":"ID-TOKEN-1","refreshToken":"REFRESH-1",
                     "expiresIn":"3600","federatedId":"https://accounts.google.com/110248495921238986420"}
                    """);
            if (url.StartsWith("https://securetoken.googleapis.com"))
                return Json(HttpStatusCode.OK, """{"id_token":"ID-TOKEN-2","refresh_token":"REFRESH-2","expires_in":"3600","user_id":"FIREBASE-UID-1"}""");
            if (url.Contains("/users/FIREBASE-UID-1/stocktarget.json"))
            {
                if (request.Method == HttpMethod.Put)
                    _stored = body;
                return Json(HttpStatusCode.OK, request.Method == HttpMethod.Put ? body : _stored ?? "null");
            }
            return Json(HttpStatusCode.NotFound, """{"error":"not found"}""");
        }

        private static HttpResponseMessage Json(HttpStatusCode code, string json) =>
            new(code) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
    }
}
