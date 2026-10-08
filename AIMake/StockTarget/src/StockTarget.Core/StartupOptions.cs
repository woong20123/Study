namespace StockTarget.Core;

/// <summary>구동 옵션으로 정하는 키움 연동 방식.</summary>
public enum KiwoomMode
{
    /// <summary>옵션 없음: 환경변수 KIWOOM_ENV를 따른다(real이면 실전, 그 외 모의투자).</summary>
    Auto,
    /// <summary>키움 연동 끔: 주문표 계산만 하고 접수하지 않는다.</summary>
    Off,
    /// <summary>KIWOOM_ENV와 상관없이 모의투자.</summary>
    Mock,
    /// <summary>KIWOOM_ENV와 상관없이 실전.</summary>
    Real,
}

/// <summary>
/// 앱 구동 옵션.
/// <code>
/// StockTarget.App.exe [--db &lt;경로&gt;] [--kiwoom off|mock|real]
/// </code>
/// </summary>
public sealed record StartupOptions(string? DbPath = null, KiwoomMode Kiwoom = KiwoomMode.Auto)
{
    public const string Usage = "StockTarget.App.exe [--db <경로>] [--kiwoom off|mock|real]";

    /// <summary>명령줄 인자를 해석한다. 모르는 옵션이나 잘못된 값이면 ArgumentException.</summary>
    public static StartupOptions Parse(IReadOnlyList<string> args)
    {
        var options = new StartupOptions();
        for (var i = 0; i < args.Count; i++)
        {
            var name = args[i].ToLowerInvariant();
            if (name is not ("--db" or "--kiwoom"))
                throw new ArgumentException($"알 수 없는 구동 옵션: '{args[i]}'");
            if (i + 1 >= args.Count || args[i + 1].StartsWith("--", StringComparison.Ordinal))
                throw new ArgumentException($"{name} 옵션에 값이 없습니다");
            var value = args[++i];
            options = name == "--db"
                ? options with { DbPath = value }
                : options with { Kiwoom = ParseKiwoom(value) };
        }
        return options;
    }

    private static KiwoomMode ParseKiwoom(string value) => value.Trim().ToLowerInvariant() switch
    {
        "off" => KiwoomMode.Off,
        "mock" => KiwoomMode.Mock,
        "real" => KiwoomMode.Real,
        "auto" => KiwoomMode.Auto,
        _ => throw new ArgumentException($"--kiwoom 값은 off · mock · real 중 하나입니다: '{value}'"),
    };
}
