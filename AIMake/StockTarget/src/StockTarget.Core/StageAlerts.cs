namespace StockTarget.Core;

/// <summary>
/// 매수 단계 알림을 보낼 종목 고르기(모바일 아침 확인 작업이 쓴다).
/// <code>
/// 알림  : 지금 매수 단계(1~3)이고, 지난번 확인 때보다 단계가 올라갔으며, 매수 완료로 표시하지 않은 종목
///         (대기 → 매수 1단계, 매수 1단계 → 3단계 …). 같은 단계에 머물거나 내려가면 다시 알리지 않는다.
/// 기억  : 이번에 본 단계를 저장해 다음 확인과 비교한다. 시세를 못 받은 종목(None)은 지난 단계를 그대로 둔다
///         (조회 실패 한 번으로 단계가 0이 되면 다음 날 같은 알림이 또 온다).
/// </code>
/// </summary>
public static class StageAlerts
{
    public static IReadOnlyList<(string Symbol, BuyStatus Stage)> NewlyReached(
        IReadOnlyDictionary<string, BuyStatus> previous,
        IReadOnlyDictionary<string, BuyStatus> current,
        IReadOnlySet<string> buyDone)
    {
        var list = new List<(string, BuyStatus)>();
        foreach (var (symbol, stage) in current)
        {
            if (stage.BuyLevel() == 0 || buyDone.Contains(symbol))
                continue;
            var before = previous.GetValueOrDefault(symbol, BuyStatus.None);
            if (stage.BuyLevel() > before.BuyLevel())
                list.Add((symbol, stage));
        }
        return list;
    }

    /// <summary>다음 비교에 쓸 단계: 이번에 받은 단계, 못 받았으면 지난 단계. 목록에서 지운 종목은 버린다.</summary>
    public static Dictionary<string, BuyStatus> Remember(
        IReadOnlyDictionary<string, BuyStatus> previous, IReadOnlyDictionary<string, BuyStatus> current)
    {
        var next = new Dictionary<string, BuyStatus>();
        foreach (var (symbol, stage) in current)
            next[symbol] = stage == BuyStatus.None ? previous.GetValueOrDefault(symbol, BuyStatus.None) : stage;
        return next;
    }

    /// <summary>"KO=Buy;NKE=StrongBuy" 형식으로 저장한다(기기 설정 한 줄에 넣는다).</summary>
    public static string Serialize(IReadOnlyDictionary<string, BuyStatus> stages) =>
        string.Join(";", stages.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => $"{p.Key}={p.Value}"));

    /// <summary>읽을 수 없는 항목은 건너뛴다(처음이면 빈 값 → 지금 매수 단계인 종목을 모두 알린다).</summary>
    public static Dictionary<string, BuyStatus> Deserialize(string? text)
    {
        var map = new Dictionary<string, BuyStatus>();
        foreach (var part in (text ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = part.Split('=', 2);
            if (kv.Length == 2 && Enum.TryParse<BuyStatus>(kv[1], out var stage))
                map[kv[0]] = stage;
        }
        return map;
    }
}
