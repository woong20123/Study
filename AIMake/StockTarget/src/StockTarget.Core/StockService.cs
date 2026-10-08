namespace StockTarget.Core;

/// <summary>조회 결과와 캐시 사용 여부.</summary>
public sealed record Fetched<T>(T Value, double? CacheAgeSeconds)
{
    public bool FromCache => CacheAgeSeconds.HasValue;
}

/// <summary>
/// 시세·배당 조회 + SQLite 캐시(최대 1시간).
/// 분할 직후로 보이는 시세는 분할 이력을 확인해 전일 종가를 보정하고, 그 티커의 캐시(분할 전 기준일 수 있음)를 버린다.
/// </summary>
public sealed class StockService(StockDatabase db, YahooChartClient client, Func<DateOnly>? today = null)
{
    private readonly Func<DateOnly> _today = today ?? (() => DateOnly.FromDateTime(DateTime.Today));

    public int CacheTtlSeconds { get; set; } = StockDatabase.MaxCacheTtlSeconds;

    public async Task<Fetched<Quote>> GetQuoteAsync(string symbol, bool force = false, CancellationToken ct = default)
    {
        symbol = Normalize(symbol);
        if (!force)
        {
            var (cached, age) = db.GetCache<Quote>($"quote:{symbol}", CacheTtlSeconds);
            if (cached is not null)
                return new Fetched<Quote>(cached, age);
        }

        var chart = await client.GetChartAsync(symbol, "1d", withEvents: false, ct).ConfigureAwait(false);
        if (chart.RegularMarketPrice is not { } price)
            throw new StockDataException($"{symbol}: 현재가 없음 (티커 확인 필요)");

        var prev = chart.PreviousClose;
        string? splitNote = null;
        if (SplitAdjuster.NeedsCheck(price, prev))
        {
            var recent = await client.GetChartAsync(symbol, "1mo", withEvents: true, ct).ConfigureAwait(false);
            var (adjusted, applied) = SplitAdjuster.Adjust(price, prev, recent.Splits, _today());
            if (applied is not null)
            {
                prev = adjusted;
                splitNote = $"{applied.Date:yyyy-MM-dd} {applied.RatioText} 분할로 전일 종가 보정";
                db.InvalidateCache(symbol);
            }
        }

        var quote = new Quote(symbol, chart.Name, price, prev, chart.Currency, chart.Exchange, splitNote);
        db.PutCache($"quote:{symbol}", quote);
        return new Fetched<Quote>(quote, null);
    }

    public async Task<Fetched<DividendYieldResult>> GetDividendYieldAsync(string symbol, bool force = false, CancellationToken ct = default)
    {
        symbol = Normalize(symbol);
        if (!force)
        {
            var (cached, age) = db.GetCache<DividendYieldResult>($"dividend:{symbol}", CacheTtlSeconds);
            if (cached is not null)
                return new Fetched<DividendYieldResult>(cached, age);
        }

        // 5년 구간 + 여유 1년
        var chart = await client.GetChartAsync(symbol, "6y", withEvents: true, ct).ConfigureAwait(false);
        if (chart.Bars.Count == 0)
            throw new StockDataException($"{symbol}: 주가 이력 없음 (티커 확인 필요)");

        var result = DividendYieldCalculator.Calculate(chart);
        db.PutCache($"dividend:{symbol}", result);
        return new Fetched<DividendYieldResult>(result, null);
    }

    /// <summary>USD/KRW 환율(1달러당 원). 시세와 같은 캐시를 쓴다.</summary>
    public Task<Fetched<Quote>> GetUsdKrwAsync(bool force = false, CancellationToken ct = default) =>
        GetQuoteAsync(Money.UsdKrwSymbol, force, ct);

    /// <summary>현재가로 이번 분기 매입 목표가와 비교하고 확인 이력에 남긴다. 이번 분기가 일정 밖이면 null.</summary>
    public PriceCheck? RecordCheck(TargetPlan plan, Quote quote)
    {
        var today = _today();
        var row = TargetCalculator.CurrentRow(plan, today);
        if (row is null)
            return null;
        var check = new PriceCheck(plan.Symbol, today, row.Quarter, quote.Price, row.BuyPrice);
        db.RecordCheck(check);
        return check;
    }

    public static string Normalize(string symbol) => symbol.Trim().ToUpperInvariant();
}
