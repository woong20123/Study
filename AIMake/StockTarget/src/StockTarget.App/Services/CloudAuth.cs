using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using StockTarget.Core;

namespace StockTarget.App.Services;

/// <summary>
/// 기본 브라우저로 Google 로그인 → 127.0.0.1 임시 포트로 인가 코드를 받는다(데스크톱 앱 루프백 방식 + PKCE).
/// </summary>
public static class GoogleLoopbackSignIn
{
    private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(3);

    /// <summary>Google ID 토큰을 돌려준다. 3분 안에 로그인하지 않거나 취소하면 CloudException.</summary>
    public static async Task<string> GetGoogleIdTokenAsync(FirebaseConfig config, HttpClient http, CancellationToken ct = default)
    {
        var redirectUri = $"http://127.0.0.1:{FreePort()}/";
        var (verifier, challenge) = GoogleOAuth.CreatePkce();
        var state = GoogleOAuth.CreateState();

        using var listener = new HttpListener();
        listener.Prefixes.Add(redirectUri);
        listener.Start();
        Process.Start(new ProcessStartInfo(GoogleOAuth.BuildAuthUrl(config.GoogleClientId, redirectUri, challenge, state))
        {
            UseShellExecute = true,
        });

        var contextTask = listener.GetContextAsync();
        if (await Task.WhenAny(contextTask, Task.Delay(Timeout, ct)) != contextTask)
            throw new CloudException("Google 로그인 시간이 초과됐습니다(3분)");
        var context = await contextTask;

        var query = context.Request.QueryString;
        var ok = query["error"] is null && query["state"] == state && !string.IsNullOrEmpty(query["code"]);
        await RespondAsync(context, ok
            ? "로그인되었습니다. 이 창을 닫고 StockTarget으로 돌아가세요."
            : "로그인하지 못했습니다. StockTarget에서 다시 시도하세요.");
        if (query["error"] is { } error)
            throw new CloudException($"Google 로그인 취소 또는 실패: {error}");
        if (!ok)
            throw new CloudException("Google 로그인 응답이 올바르지 않습니다(state 불일치)");

        return await GoogleOAuth.ExchangeCodeAsync(http, config, query["code"]!, verifier, redirectUri, ct);
    }

    private static int FreePort()
    {
        var l = new TcpListener(IPAddress.Loopback, 0);
        l.Start();
        var port = ((IPEndPoint)l.LocalEndpoint).Port;
        l.Stop();
        return port;
    }

    private static async Task RespondAsync(HttpListenerContext context, string message)
    {
        var html = $"<!doctype html><meta charset=\"utf-8\"><title>StockTarget</title>" +
                   $"<body style=\"font-family:sans-serif;padding:40px\"><h3>{WebUtility.HtmlEncode(message)}</h3></body>";
        var bytes = Encoding.UTF8.GetBytes(html);
        context.Response.ContentType = "text/html; charset=utf-8";
        context.Response.ContentLength64 = bytes.Length;
        await context.Response.OutputStream.WriteAsync(bytes);
        context.Response.Close();
    }
}

/// <summary>
/// 로그인 상태(갱신 토큰 포함)를 Windows DPAPI(현재 사용자)로 암호화해 파일에 둔다.
/// 다른 Windows 사용자나 다른 PC로 파일을 복사해도 풀 수 없다.
/// </summary>
public sealed class CloudSessionStore(string path)
{
    public static string DefaultPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "StockTarget", "firebase-session.dat");

    public FirebaseSession? Load()
    {
        try
        {
            if (!File.Exists(path))
                return null;
            var json = ProtectedData.Unprotect(File.ReadAllBytes(path), null, DataProtectionScope.CurrentUser);
            return JsonSerializer.Deserialize<FirebaseSession>(json);
        }
        catch (Exception e) when (e is CryptographicException or JsonException or IOException)
        {
            return null; // 손상되었거나 다른 사용자 것이면 다시 로그인
        }
    }

    public void Save(FirebaseSession session)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var bytes = ProtectedData.Protect(JsonSerializer.SerializeToUtf8Bytes(session), null, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(path, bytes);
    }

    public void Clear()
    {
        if (File.Exists(path))
            File.Delete(path);
    }
}
