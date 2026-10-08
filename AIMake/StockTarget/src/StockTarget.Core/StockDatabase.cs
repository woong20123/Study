using System.Globalization;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace StockTarget.Core;

/// <summary>분기 확인 이력 한 행(조회한 날의 현재가와 그 분기 매입 목표가).</summary>
public sealed record PriceCheck(string Symbol, DateOnly CheckDate, string Quarter, double Price, double BuyPrice)
{
    public double GapPct => BuyPrice > 0 ? (Price / BuyPrice - 1) * 100 : 0;
    public bool BuyCondition => Price <= BuyPrice;
    public BuyStatus Status => TargetCalculator.Classify(Price, BuyPrice);
    public string StatusText => Status.ToText();
}

/// <summary>
/// SQLite 저장소.
/// <list type="bullet">
/// <item>targets      : 목표(티커당 1개, 다시 저장하면 갱신)</item>
/// <item>price_checks : 분기 확인 이력(티커·날짜당 1행, 같은 날 다시 조회하면 갱신)</item>
/// <item>cache        : 시세·배당 조회 결과 캐시(최대 1시간)</item>
/// </list>
/// </summary>
public sealed class StockDatabase
{
    public const int MaxCacheTtlSeconds = 3600;

    private readonly string _connectionString;
    private readonly Func<DateTimeOffset> _now;

    public string Path { get; }

    public StockDatabase(string path, Func<DateTimeOffset>? now = null)
    {
        Path = path;
        _now = now ?? (() => DateTimeOffset.UtcNow);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(path))!);
        _connectionString = new SqliteConnectionStringBuilder { DataSource = path, Pooling = false }.ToString();
        EnsureSchema();
    }

    public static string DefaultPath =>
        System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "StockTarget", "stocktarget.db");

    private SqliteConnection Open()
    {
        var c = new SqliteConnection(_connectionString);
        c.Open();
        return c;
    }

    private void EnsureSchema()
    {
        using var c = Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS targets (
                symbol             TEXT PRIMARY KEY,
                input_date         TEXT NOT NULL,
                target_year        INTEGER NOT NULL,
                eps                REAL NOT NULL,
                per                REAL NOT NULL,
                return_pct         REAL NOT NULL,
                dividend_yield_pct REAL NOT NULL,
                memo               TEXT,
                updated_at         TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS price_checks (
                symbol     TEXT NOT NULL,
                check_date TEXT NOT NULL,
                quarter    TEXT NOT NULL,
                price      REAL NOT NULL,
                buy_price  REAL NOT NULL,
                PRIMARY KEY (symbol, check_date)
            );
            CREATE TABLE IF NOT EXISTS cache (
                key        TEXT PRIMARY KEY,
                fetched_at INTEGER NOT NULL,
                payload    TEXT NOT NULL
            );
            """;
        cmd.ExecuteNonQuery();
    }

    // ---------------------------------------------------------------- targets
    public void SaveTarget(TargetPlan t)
    {
        using var c = Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = """
            INSERT INTO targets (symbol, input_date, target_year, eps, per, return_pct, dividend_yield_pct, memo, updated_at)
            VALUES ($s, $d, $y, $eps, $per, $r, $div, $memo, $u)
            ON CONFLICT(symbol) DO UPDATE SET
                input_date = excluded.input_date, target_year = excluded.target_year, eps = excluded.eps,
                per = excluded.per, return_pct = excluded.return_pct, dividend_yield_pct = excluded.dividend_yield_pct,
                memo = excluded.memo, updated_at = excluded.updated_at
            """;
        cmd.Parameters.AddWithValue("$s", t.Symbol);
        cmd.Parameters.AddWithValue("$d", t.InputDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        cmd.Parameters.AddWithValue("$y", t.TargetYear);
        cmd.Parameters.AddWithValue("$eps", t.Eps);
        cmd.Parameters.AddWithValue("$per", t.Per);
        cmd.Parameters.AddWithValue("$r", t.ReturnPct);
        cmd.Parameters.AddWithValue("$div", t.DividendYieldPct);
        cmd.Parameters.AddWithValue("$memo", (object?)t.Memo ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$u", _now().ToString("o"));
        cmd.ExecuteNonQuery();
    }

    public IReadOnlyList<TargetPlan> GetTargets()
    {
        using var c = Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT symbol, input_date, target_year, eps, per, return_pct, dividend_yield_pct, memo FROM targets ORDER BY symbol";
        using var r = cmd.ExecuteReader();
        var list = new List<TargetPlan>();
        while (r.Read())
        {
            list.Add(new TargetPlan(
                r.GetString(0),
                DateOnly.ParseExact(r.GetString(1), "yyyy-MM-dd", CultureInfo.InvariantCulture),
                r.GetInt32(2), r.GetDouble(3), r.GetDouble(4), r.GetDouble(5), r.GetDouble(6),
                r.IsDBNull(7) ? null : r.GetString(7)));
        }
        return list;
    }

    public bool DeleteTarget(string symbol)
    {
        using var c = Open();
        using var tx = c.BeginTransaction();
        using var cmd = c.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = "DELETE FROM targets WHERE symbol = $s";
        cmd.Parameters.AddWithValue("$s", symbol);
        var n = cmd.ExecuteNonQuery();
        cmd.CommandText = "DELETE FROM price_checks WHERE symbol = $s";
        cmd.ExecuteNonQuery();
        tx.Commit();
        return n > 0;
    }

    // ----------------------------------------------------------- price_checks
    public void RecordCheck(PriceCheck p)
    {
        using var c = Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = """
            INSERT INTO price_checks (symbol, check_date, quarter, price, buy_price) VALUES ($s, $d, $q, $p, $b)
            ON CONFLICT(symbol, check_date) DO UPDATE SET quarter = excluded.quarter, price = excluded.price, buy_price = excluded.buy_price
            """;
        cmd.Parameters.AddWithValue("$s", p.Symbol);
        cmd.Parameters.AddWithValue("$d", p.CheckDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        cmd.Parameters.AddWithValue("$q", p.Quarter);
        cmd.Parameters.AddWithValue("$p", p.Price);
        cmd.Parameters.AddWithValue("$b", p.BuyPrice);
        cmd.ExecuteNonQuery();
    }

    public IReadOnlyList<PriceCheck> GetChecks(string symbol)
    {
        using var c = Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT symbol, check_date, quarter, price, buy_price FROM price_checks WHERE symbol = $s ORDER BY check_date DESC";
        cmd.Parameters.AddWithValue("$s", symbol);
        using var r = cmd.ExecuteReader();
        var list = new List<PriceCheck>();
        while (r.Read())
        {
            list.Add(new PriceCheck(r.GetString(0), DateOnly.ParseExact(r.GetString(1), "yyyy-MM-dd", CultureInfo.InvariantCulture),
                r.GetString(2), r.GetDouble(3), r.GetDouble(4)));
        }
        return list;
    }

    // ------------------------------------------------------------------ cache
    /// <summary>유효한 캐시면 (값, 경과 초), 없거나 만료면 (default, null).</summary>
    public (T? Value, double? AgeSeconds) GetCache<T>(string key, int ttlSeconds = MaxCacheTtlSeconds)
    {
        ttlSeconds = Math.Clamp(ttlSeconds, 0, MaxCacheTtlSeconds);
        using var c = Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT fetched_at, payload FROM cache WHERE key = $k";
        cmd.Parameters.AddWithValue("$k", key);
        using var r = cmd.ExecuteReader();
        if (!r.Read())
            return (default, null);
        var age = _now().ToUnixTimeSeconds() - r.GetInt64(0);
        if (age < 0 || age >= ttlSeconds)
            return (default, null);
        try
        {
            return (JsonSerializer.Deserialize<T>(r.GetString(1)), age);
        }
        catch (JsonException)
        {
            return (default, null); // 형식이 바뀐 옛 캐시는 무시
        }
    }

    public void PutCache<T>(string key, T value)
    {
        using var c = Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = """
            INSERT INTO cache (key, fetched_at, payload) VALUES ($k, $t, $p)
            ON CONFLICT(key) DO UPDATE SET fetched_at = excluded.fetched_at, payload = excluded.payload;
            DELETE FROM cache WHERE fetched_at < $expire;
            """;
        var now = _now().ToUnixTimeSeconds();
        cmd.Parameters.AddWithValue("$k", key);
        cmd.Parameters.AddWithValue("$t", now);
        cmd.Parameters.AddWithValue("$p", JsonSerializer.Serialize(value));
        cmd.Parameters.AddWithValue("$expire", now - MaxCacheTtlSeconds);
        cmd.ExecuteNonQuery();
    }

    public void InvalidateCache(string symbol)
    {
        using var c = Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "DELETE FROM cache WHERE key = 'quote:' || $s OR key = 'dividend:' || $s";
        cmd.Parameters.AddWithValue("$s", symbol);
        cmd.ExecuteNonQuery();
    }
}
