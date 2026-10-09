namespace StockTarget.Core;

/// <summary>
/// 앱 구동 옵션.
/// <code>
/// StockTarget.App.exe [--db &lt;경로&gt;]
/// </code>
/// </summary>
public sealed record StartupOptions(string? DbPath = null)
{
    public const string Usage = "StockTarget.App.exe [--db <경로>]";

    /// <summary>명령줄 인자를 해석한다. 모르는 옵션이나 잘못된 값이면 ArgumentException.</summary>
    public static StartupOptions Parse(IReadOnlyList<string> args)
    {
        var options = new StartupOptions();
        for (var i = 0; i < args.Count; i++)
        {
            var name = args[i].ToLowerInvariant();
            if (name is not "--db")
                throw new ArgumentException($"알 수 없는 구동 옵션: '{args[i]}'");
            if (i + 1 >= args.Count || args[i + 1].StartsWith("--", StringComparison.Ordinal))
                throw new ArgumentException($"{name} 옵션에 값이 없습니다");
            options = options with { DbPath = args[++i] };
        }
        return options;
    }
}
