using System.Text.Json;
using System.Text.Json.Serialization;

namespace StockTarget.Core;

/// <summary>
/// 기기 간에 옮길 수 있는 백업(JSON 파일 · Firebase 공용 형식). 시세 캐시는 넣지 않는다.
/// 도메인 레코드를 그대로 직렬화하지 않고 필드를 명시해, 코드가 바뀌어도 형식이 유지되게 한다.
/// </summary>
public sealed record BackupData
{
    public const string FormatName = "stocktarget-backup";
    public const int CurrentVersion = 1;

    public string Format { get; init; } = FormatName;
    public int Version { get; init; } = CurrentVersion;
    public DateTimeOffset ExportedAt { get; init; }

    // Firebase Realtime Database는 빈 배열을 저장하지 않으므로 읽을 때 null일 수 있다
    public List<BackupTarget>? Targets { get; init; } = [];
    public List<BackupCheck>? PriceChecks { get; init; } = [];
    public List<BackupReservation>? Reservations { get; init; } = [];

    [JsonIgnore]
    public string Summary =>
        $"목표 {Targets?.Count ?? 0}개 · 확인 이력 {PriceChecks?.Count ?? 0}건 · 예약 이력 {Reservations?.Count ?? 0}건";

    public static BackupData Create(
        IEnumerable<TargetPlan> targets, IEnumerable<PriceCheck> checks, IEnumerable<ReservationRecord> reservations,
        DateTimeOffset exportedAt) => new()
        {
            ExportedAt = exportedAt,
            Targets = targets.Select(BackupTarget.From).ToList(),
            PriceChecks = checks.Select(BackupCheck.From).ToList(),
            Reservations = reservations.Select(BackupReservation.From).ToList(),
        };

    public IReadOnlyList<TargetPlan> ToTargets()
    {
        var list = (Targets ?? []).Select(t => t.ToPlan()).ToList();
        foreach (var t in list)
        {
            try
            {
                TargetCalculator.Validate(t);
            }
            catch (ArgumentException e)
            {
                throw new BackupFormatException($"{t.Symbol}: {e.Message}");
            }
        }
        var dup = list.GroupBy(t => t.Symbol).FirstOrDefault(g => g.Count() > 1);
        if (dup is not null)
            throw new BackupFormatException($"목표 티커가 중복됩니다: {dup.Key}");
        return list;
    }

    public IReadOnlyList<PriceCheck> ToChecks() => (PriceChecks ?? []).Select(c => c.ToCheck()).ToList();

    public IReadOnlyList<ReservationRecord> ToReservations() => (Reservations ?? []).Select(r => r.ToRecord()).ToList();
}

public sealed record BackupTarget(
    string Symbol,
    DateOnly InputDate,
    int TargetYear,
    double Eps,
    double Per,
    double ReturnPct,
    double DividendYieldPct,
    string? Memo,
    double? TargetPrice,
    double? BuyKrw,
    double? MustBuyKrw,
    double? StrongBuyKrw)
{
    public static BackupTarget From(TargetPlan t) => new(
        t.Symbol, t.InputDate, t.TargetYear, t.Eps, t.Per, t.ReturnPct, t.DividendYieldPct, t.Memo,
        t.TargetPriceInput, t.BuyAmounts.BuyKrw, t.BuyAmounts.MustBuyKrw, t.BuyAmounts.StrongBuyKrw);

    public TargetPlan ToPlan()
    {
        if (string.IsNullOrWhiteSpace(Symbol))
            throw new BackupFormatException("티커가 비어 있는 목표가 있습니다");
        var amounts = new BuyAmounts(BuyKrw, MustBuyKrw, StrongBuyKrw);
        return new TargetPlan(StockService.Normalize(Symbol), InputDate, TargetYear, Eps, Per, ReturnPct, DividendYieldPct,
            Memo, amounts.IsEmpty ? null : amounts, TargetPrice);
    }
}

public sealed record BackupCheck(string Symbol, DateOnly CheckDate, string Quarter, double Price, double BuyPrice)
{
    public static BackupCheck From(PriceCheck p) => new(p.Symbol, p.CheckDate, p.Quarter, p.Price, p.BuyPrice);

    public PriceCheck ToCheck() => new(Symbol, CheckDate, Quarter, Price, BuyPrice);
}

public sealed record BackupReservation(
    string Symbol,
    string Stage,
    DateOnly Start,
    DateOnly End,
    double Price,
    int Quantity,
    double AmountKrw,
    string Env,
    string ReservationNo,
    string ScheduledDate,
    DateTimeOffset CreatedAt)
{
    public static BackupReservation From(ReservationRecord r) => new(
        r.Symbol, r.Stage, r.Start, r.End, r.Price, r.Quantity, r.AmountKrw, r.Env, r.ReservationNo, r.ScheduledDate, r.CreatedAt);

    public ReservationRecord ToRecord() =>
        new(Symbol, Stage, Start, End, Price, Quantity, AmountKrw, Env, ReservationNo, ScheduledDate, CreatedAt);
}

/// <summary>백업 JSON ↔ BackupData. camelCase, null 값은 생략.</summary>
public static class BackupSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
    };

    public static string ToJson(BackupData data) => JsonSerializer.Serialize(data, Options);

    public static BackupData FromJson(string json)
    {
        BackupData? data;
        try
        {
            data = JsonSerializer.Deserialize<BackupData>(json, Options);
        }
        catch (JsonException e)
        {
            throw new BackupFormatException($"백업 JSON을 읽을 수 없습니다: {e.Message}");
        }
        if (data is null || data.Format != BackupData.FormatName)
            throw new BackupFormatException("StockTarget 백업 파일이 아닙니다");
        if (data.Version > BackupData.CurrentVersion)
            throw new BackupFormatException($"더 새 버전(v{data.Version})의 백업입니다. 앱을 업데이트하세요");
        return data;
    }
}

/// <summary>백업 형식 오류(다른 파일, 깨진 JSON, 잘못된 값).</summary>
public sealed class BackupFormatException(string message) : Exception(message);
