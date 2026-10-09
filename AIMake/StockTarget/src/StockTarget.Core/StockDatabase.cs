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

/// <summary>예전 버전에서 키움에 접수한 LOC 예약 매수 한 건. 지금은 새로 쌓지 않고 기존 이력 · 백업 호환을 위해 남긴다.</summary>
public sealed record ReservationRecord(
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
    DateTimeOffset CreatedAt);

/// <summary>
/// SQLite 저장소.
/// <list type="bullet">
/// <item>targets      : 목표(티커당 1개, 다시 저장하면 갱신)</item>
/// <item>price_checks : 분기 확인 이력(티커·날짜당 1행, 같은 날 다시 조회하면 갱신)</item>
/// <item>cache        : 시세·배당 조회 결과 캐시(최대 1시간)</item>
/// <item>reservations : 예전 버전에서 키움에 접수한 LOC 예약 매수 이력(티커·단계·기간·환경당 1건)</item>
/// <item>settings     : 앱 설정(키·값). 기본 매수금액 등</item>
/// <item>buy_done     : '매수 완료'로 표시한 티커(해제할 때까지 예약 주문표에서 뺀다)</item>
/// <item>stock_names  : 네이버에서 받은 한글 종목명(찾지 못함은 name NULL). 7일 지나면 다시 조회</item>
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
                updated_at         TEXT NOT NULL,
                buy_krw            REAL,
                must_buy_krw       REAL,
                strong_buy_krw     REAL,
                target_price       REAL
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
            CREATE TABLE IF NOT EXISTS reservations (
                symbol         TEXT NOT NULL,
                stage          TEXT NOT NULL,
                start_date     TEXT NOT NULL,
                end_date       TEXT NOT NULL,
                price          REAL NOT NULL,
                quantity       INTEGER NOT NULL,
                amount_krw     REAL NOT NULL,
                env            TEXT NOT NULL,
                reservation_no TEXT NOT NULL,
                scheduled_date TEXT NOT NULL,
                created_at     TEXT NOT NULL,
                PRIMARY KEY (symbol, stage, start_date, end_date, env)
            );
            CREATE TABLE IF NOT EXISTS settings (
                key   TEXT PRIMARY KEY,
                value TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS buy_done (
                symbol    TEXT PRIMARY KEY,
                marked_at TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS stock_names (
                symbol     TEXT PRIMARY KEY,
                name       TEXT,
                fetched_at INTEGER NOT NULL
            );
            """;
        cmd.ExecuteNonQuery();

        // 매수금액·목표 주가 직접 입력 컬럼이 없던 이전 버전 DB에 컬럼을 추가한다
        cmd.CommandText = "SELECT name FROM pragma_table_info('targets')";
        var columns = new HashSet<string>();
        using (var r = cmd.ExecuteReader())
        {
            while (r.Read())
                columns.Add(r.GetString(0));
        }
        foreach (var col in new[] { "buy_krw", "must_buy_krw", "strong_buy_krw", "target_price" })
        {
            if (columns.Contains(col))
                continue;
            cmd.CommandText = $"ALTER TABLE targets ADD COLUMN {col} REAL";
            cmd.ExecuteNonQuery();
        }
    }

    // ---------------------------------------------------------------- targets
    public void SaveTarget(TargetPlan t)
    {
        using var c = Open();
        UpsertTarget(c, null, t, _now());
    }

    private static void UpsertTarget(SqliteConnection c, SqliteTransaction? tx, TargetPlan t, DateTimeOffset updatedAt)
    {
        using var cmd = c.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = """
            INSERT INTO targets (symbol, input_date, target_year, eps, per, return_pct, dividend_yield_pct, memo, updated_at,
                                 buy_krw, must_buy_krw, strong_buy_krw, target_price)
            VALUES ($s, $d, $y, $eps, $per, $r, $div, $memo, $u, $buy, $must, $strong, $tp)
            ON CONFLICT(symbol) DO UPDATE SET
                input_date = excluded.input_date, target_year = excluded.target_year, eps = excluded.eps,
                per = excluded.per, return_pct = excluded.return_pct, dividend_yield_pct = excluded.dividend_yield_pct,
                memo = excluded.memo, updated_at = excluded.updated_at,
                buy_krw = excluded.buy_krw, must_buy_krw = excluded.must_buy_krw, strong_buy_krw = excluded.strong_buy_krw,
                target_price = excluded.target_price
            """;
        cmd.Parameters.AddWithValue("$s", t.Symbol);
        cmd.Parameters.AddWithValue("$d", t.InputDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        cmd.Parameters.AddWithValue("$y", t.TargetYear);
        cmd.Parameters.AddWithValue("$eps", t.Eps);
        cmd.Parameters.AddWithValue("$per", t.Per);
        cmd.Parameters.AddWithValue("$r", t.ReturnPct);
        cmd.Parameters.AddWithValue("$div", t.DividendYieldPct);
        cmd.Parameters.AddWithValue("$memo", (object?)t.Memo ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$u", updatedAt.ToString("o"));
        var a = t.BuyAmounts;
        cmd.Parameters.AddWithValue("$buy", (object?)a.BuyKrw ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$must", (object?)a.MustBuyKrw ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$strong", (object?)a.StrongBuyKrw ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$tp", (object?)t.TargetPriceInput ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    public IReadOnlyList<TargetPlan> GetTargets()
    {
        using var c = Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = """
            SELECT symbol, input_date, target_year, eps, per, return_pct, dividend_yield_pct, memo,
                   buy_krw, must_buy_krw, strong_buy_krw, target_price
            FROM targets ORDER BY symbol
            """;
        using var r = cmd.ExecuteReader();
        var list = new List<TargetPlan>();
        while (r.Read())
        {
            list.Add(new TargetPlan(
                r.GetString(0),
                DateOnly.ParseExact(r.GetString(1), "yyyy-MM-dd", CultureInfo.InvariantCulture),
                r.GetInt32(2), r.GetDouble(3), r.GetDouble(4), r.GetDouble(5), r.GetDouble(6),
                r.IsDBNull(7) ? null : r.GetString(7),
                ReadAmounts(r),
                r.IsDBNull(11) ? null : r.GetDouble(11)));
        }
        return list;
    }

    /// <summary>buy_krw(8) · must_buy_krw(9) · strong_buy_krw(10). 모두 비어 있으면 null.</summary>
    private static BuyAmounts? ReadAmounts(SqliteDataReader r)
    {
        double? Get(int i) => r.IsDBNull(i) ? null : r.GetDouble(i);
        var a = new BuyAmounts(Get(8), Get(9), Get(10));
        return a.IsEmpty ? null : a;
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
        cmd.CommandText = "DELETE FROM buy_done WHERE symbol = $s";
        cmd.ExecuteNonQuery();
        tx.Commit();
        return n > 0;
    }

    // --------------------------------------------------------------- buy_done
    /// <summary>'매수 완료'로 표시한 티커.</summary>
    public IReadOnlySet<string> GetBuyDone()
    {
        using var c = Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT symbol FROM buy_done ORDER BY symbol";
        using var r = cmd.ExecuteReader();
        var set = new SortedSet<string>(StringComparer.Ordinal);
        while (r.Read())
            set.Add(r.GetString(0));
        return set;
    }

    /// <summary>티커를 '매수 완료'로 표시하거나(done) 해제한다.</summary>
    public void SetBuyDone(string symbol, bool done)
    {
        using var c = Open();
        WriteBuyDone(c, null, symbol, done, _now());
    }

    private static void WriteBuyDone(SqliteConnection c, SqliteTransaction? tx, string symbol, bool done, DateTimeOffset now)
    {
        using var cmd = c.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = done
            ? "INSERT INTO buy_done (symbol, marked_at) VALUES ($s, $t) ON CONFLICT(symbol) DO NOTHING"
            : "DELETE FROM buy_done WHERE symbol = $s";
        cmd.Parameters.AddWithValue("$s", symbol);
        cmd.Parameters.AddWithValue("$t", now.ToString("o"));
        cmd.ExecuteNonQuery();
    }

    // --------------------------------------------------------------- settings
    private const string DefaultBuyKey = "default_buy_krw";
    private const string DefaultMustBuyKey = "default_must_buy_krw";
    private const string DefaultStrongBuyKey = "default_strong_buy_krw";

    /// <summary>기본 매수금액(목표에서 비운 단계에 쓴다). 설정하지 않았으면 모든 단계가 비어 있다.</summary>
    public BuyAmounts GetDefaultAmounts()
    {
        using var c = Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT key, value FROM settings WHERE key IN ($b, $m, $s)";
        cmd.Parameters.AddWithValue("$b", DefaultBuyKey);
        cmd.Parameters.AddWithValue("$m", DefaultMustBuyKey);
        cmd.Parameters.AddWithValue("$s", DefaultStrongBuyKey);
        var values = new Dictionary<string, double>();
        using (var r = cmd.ExecuteReader())
        {
            while (r.Read())
            {
                if (double.TryParse(r.GetString(1), NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
                    values[r.GetString(0)] = v;
            }
        }
        double? Get(string key) => values.TryGetValue(key, out var v) ? v : null;
        return new BuyAmounts(Get(DefaultBuyKey), Get(DefaultMustBuyKey), Get(DefaultStrongBuyKey));
    }

    public void SaveDefaultAmounts(BuyAmounts amounts)
    {
        using var c = Open();
        using var tx = c.BeginTransaction();
        WriteDefaultAmounts(c, tx, amounts);
        tx.Commit();
    }

    private static void WriteDefaultAmounts(SqliteConnection c, SqliteTransaction tx, BuyAmounts a)
    {
        foreach (var (key, value) in new[] { (DefaultBuyKey, a.BuyKrw), (DefaultMustBuyKey, a.MustBuyKrw), (DefaultStrongBuyKey, a.StrongBuyKrw) })
        {
            using var cmd = c.CreateCommand();
            cmd.Transaction = tx;
            cmd.Parameters.AddWithValue("$k", key);
            if (value is { } v)
            {
                cmd.CommandText = """
                    INSERT INTO settings (key, value) VALUES ($k, $v)
                    ON CONFLICT(key) DO UPDATE SET value = excluded.value
                    """;
                cmd.Parameters.AddWithValue("$v", v.ToString("R", CultureInfo.InvariantCulture));
            }
            else
            {
                cmd.CommandText = "DELETE FROM settings WHERE key = $k";
            }
            cmd.ExecuteNonQuery();
        }
    }

    // ----------------------------------------------------------- price_checks
    public void RecordCheck(PriceCheck p)
    {
        using var c = Open();
        UpsertCheck(c, null, p);
    }

    private static void UpsertCheck(SqliteConnection c, SqliteTransaction? tx, PriceCheck p)
    {
        using var cmd = c.CreateCommand();
        cmd.Transaction = tx;
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

    /// <summary>티커의 확인 이력(최신 순). symbol이 null이면 전체(티커 · 날짜 순).</summary>
    public IReadOnlyList<PriceCheck> GetChecks(string? symbol)
    {
        using var c = Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = symbol is null
            ? "SELECT symbol, check_date, quarter, price, buy_price FROM price_checks ORDER BY symbol, check_date"
            : "SELECT symbol, check_date, quarter, price, buy_price FROM price_checks WHERE symbol = $s ORDER BY check_date DESC";
        if (symbol is not null)
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

    // ----------------------------------------------------------- reservations
    public void SaveReservation(ReservationRecord r)
    {
        using var c = Open();
        InsertReservation(c, null, r);
    }

    private static void InsertReservation(SqliteConnection c, SqliteTransaction? tx, ReservationRecord r)
    {
        using var cmd = c.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = """
            INSERT INTO reservations (symbol, stage, start_date, end_date, price, quantity, amount_krw, env,
                                      reservation_no, scheduled_date, created_at)
            VALUES ($s, $st, $sd, $ed, $p, $q, $a, $env, $no, $sch, $c)
            """;
        cmd.Parameters.AddWithValue("$s", r.Symbol);
        cmd.Parameters.AddWithValue("$st", r.Stage);
        cmd.Parameters.AddWithValue("$sd", r.Start.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        cmd.Parameters.AddWithValue("$ed", r.End.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        cmd.Parameters.AddWithValue("$p", r.Price);
        cmd.Parameters.AddWithValue("$q", r.Quantity);
        cmd.Parameters.AddWithValue("$a", r.AmountKrw);
        cmd.Parameters.AddWithValue("$env", r.Env);
        cmd.Parameters.AddWithValue("$no", r.ReservationNo);
        cmd.Parameters.AddWithValue("$sch", r.ScheduledDate);
        cmd.Parameters.AddWithValue("$c", r.CreatedAt.ToString("o"));
        cmd.ExecuteNonQuery();
    }

    /// <summary>같은 티커·단계·기간·환경으로 이미 접수한 예약.</summary>
    public ReservationRecord? FindReservation(string symbol, string stage, DateOnly start, DateOnly end, string env) =>
        QueryReservations(
            "WHERE symbol = $s AND stage = $st AND start_date = $sd AND end_date = $ed AND env = $env",
            ("$s", symbol), ("$st", stage),
            ("$sd", start.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
            ("$ed", end.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)), ("$env", env))
        .FirstOrDefault();

    /// <summary>접수 이력(최근 순).</summary>
    public IReadOnlyList<ReservationRecord> GetReservations() => QueryReservations("");

    private List<ReservationRecord> QueryReservations(string where, params (string Name, object Value)[] args)
    {
        using var c = Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = $"""
            SELECT symbol, stage, start_date, end_date, price, quantity, amount_krw, env, reservation_no, scheduled_date, created_at
            FROM reservations {where} ORDER BY created_at DESC, symbol
            """;
        foreach (var (name, value) in args)
            cmd.Parameters.AddWithValue(name, value);
        using var r = cmd.ExecuteReader();
        var list = new List<ReservationRecord>();
        while (r.Read())
        {
            list.Add(new ReservationRecord(
                r.GetString(0), r.GetString(1),
                DateOnly.ParseExact(r.GetString(2), "yyyy-MM-dd", CultureInfo.InvariantCulture),
                DateOnly.ParseExact(r.GetString(3), "yyyy-MM-dd", CultureInfo.InvariantCulture),
                r.GetDouble(4), r.GetInt32(5), r.GetDouble(6), r.GetString(7), r.GetString(8), r.GetString(9),
                DateTimeOffset.Parse(r.GetString(10), CultureInfo.InvariantCulture)));
        }
        return list;
    }

    // ----------------------------------------------------------------- backup
    /// <summary>캐시를 뺀 사용자 데이터 전체(목표 · 확인 이력 · 예약 접수 이력 · 기본 매수금액 · 매수 완료).</summary>
    public BackupData ExportBackup() =>
        BackupData.Create(GetTargets(), GetChecks(null), GetReservations(), _now(), GetDefaultAmounts(), GetBuyDone());

    /// <summary>
    /// 백업으로 사용자 데이터를 통째로 바꾼다(캐시는 그대로). 한 트랜잭션이라 중간에 실패하면 아무것도 바뀌지 않는다.
    /// 기본 매수금액이 없는 옛 백업이면 현재 기본 매수금액을 그대로 둔다.
    /// </summary>
    public void ReplaceWithBackup(BackupData backup)
    {
        var targets = backup.ToTargets();
        var checks = backup.ToChecks();
        var reservations = backup.ToReservations();
        var defaults = backup.ToDefaultAmounts();
        var buyDone = backup.ToBuyDone(targets);

        using var c = Open();
        using var tx = c.BeginTransaction();
        using (var cmd = c.CreateCommand())
        {
            cmd.Transaction = tx;
            cmd.CommandText = "DELETE FROM targets; DELETE FROM price_checks; DELETE FROM reservations; DELETE FROM buy_done;";
            cmd.ExecuteNonQuery();
        }
        var now = _now();
        foreach (var t in targets)
            UpsertTarget(c, tx, t, now);
        foreach (var s in buyDone)
            WriteBuyDone(c, tx, s, true, now);
        foreach (var p in checks)
            UpsertCheck(c, tx, p);
        foreach (var r in reservations)
            InsertReservation(c, tx, r);
        if (defaults is not null)
            WriteDefaultAmounts(c, tx, defaults);
        tx.Commit();
    }

    // ------------------------------------------------------------ stock_names
    /// <summary>캐시한 한글 종목명과 받은 뒤 지난 시간. 조회한 적이 없으면 null(찾지 못함이면 Name만 null).</summary>
    public (string? Name, TimeSpan Age)? GetStockName(string symbol)
    {
        using var c = Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT name, fetched_at FROM stock_names WHERE symbol = $s";
        cmd.Parameters.AddWithValue("$s", symbol);
        using var r = cmd.ExecuteReader();
        if (!r.Read())
            return null;
        var age = TimeSpan.FromSeconds(_now().ToUnixTimeSeconds() - r.GetInt64(1));
        return (r.IsDBNull(0) ? null : r.GetString(0), age);
    }

    public void PutStockName(string symbol, string? name)
    {
        using var c = Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = """
            INSERT INTO stock_names (symbol, name, fetched_at) VALUES ($s, $n, $t)
            ON CONFLICT(symbol) DO UPDATE SET name = excluded.name, fetched_at = excluded.fetched_at
            """;
        cmd.Parameters.AddWithValue("$s", symbol);
        cmd.Parameters.AddWithValue("$n", (object?)name ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$t", _now().ToUnixTimeSeconds());
        cmd.ExecuteNonQuery();
    }

    /// <summary>캐시한 한글 종목명 전체(찾지 못한 티커는 뺀다).</summary>
    public IReadOnlyDictionary<string, string> GetStockNames()
    {
        using var c = Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT symbol, name FROM stock_names WHERE name IS NOT NULL";
        using var r = cmd.ExecuteReader();
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        while (r.Read())
            map[r.GetString(0)] = r.GetString(1);
        return map;
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
