using StockTarget.Core;

namespace StockTarget.Tests;

public class TargetCalculatorTests
{
    // 예시: KO 2030년 EPS 10.5 × PER 18, 목표 수익률 10%, 5년 평균 배당 2.92%, 입력일 2026-10-08
    private static readonly TargetPlan Ko = new("KO", new DateOnly(2026, 10, 8), 2030, 10.5, 18, 10.0, 2.92);

    [Fact]
    public void TargetPriceAndGrowth()
    {
        Assert.Equal(189.0, Ko.TargetPrice, 6);
        Assert.Equal(7.08, Ko.GrowthPct, 6);
        Assert.Equal(new DateOnly(2030, 12, 31), Ko.TargetDate);
    }

    [Fact]
    public void QuartersStartAtInputQuarter()
    {
        var q = TargetCalculator.Quarters(Ko.InputDate, Ko.TargetDate);
        Assert.Equal(("2026Q4", new DateOnly(2026, 10, 8)), q[0]); // 첫 분기 기준일은 입력일
        Assert.Equal(("2027Q1", new DateOnly(2027, 1, 1)), q[1]);
        Assert.Equal(("2030Q4", new DateOnly(2030, 10, 1)), q[^1]);
        Assert.Equal(17, q.Count);
    }

    [Fact]
    public void BuyPriceMatchesHandCalculation()
    {
        // 2026-10-08 → 2030-12-31 = 1545일, 189 / 1.0708^(1545/365.25) = 141.5129
        // (Python 화면의 141.52는 배당수익률을 반올림 전 값 2.92063…으로 계산한 141.5164 — 공식은 동일)
        var expected = 189.0 / Math.Pow(1.0708, 1545 / 365.25);
        Assert.Equal(expected, TargetCalculator.BuyPrice(189, 7.08, Ko.InputDate, Ko.TargetDate), 9);
        Assert.Equal(141.51, Math.Round(expected, 2));
        Assert.Equal(141.52, Math.Round(TargetCalculator.BuyPrice(189, 10 - 2.920630977086624, Ko.InputDate, Ko.TargetDate), 2));
    }

    [Fact]
    public void BuyPriceRisesEachQuarter()
    {
        var p = TargetCalculator.Schedule(Ko).Select(r => r.BuyPrice).ToList();
        Assert.True(p.Zip(p.Skip(1)).All(x => x.First < x.Second));
        Assert.True(p[^1] < 189);
        Assert.Equal(185.81, Math.Round(p[^1], 2));
    }

    [Fact]
    public void CurrentRow()
    {
        Assert.Equal("2026Q4", TargetCalculator.CurrentRow(Ko, new DateOnly(2026, 11, 30))!.Quarter);
        Assert.Null(TargetCalculator.CurrentRow(Ko, new DateOnly(2031, 1, 5)));
    }

    [Fact]
    public void DividendAboveReturnGivesNegativeGrowth()
    {
        var t = new TargetPlan("X", new DateOnly(2026, 10, 8), 2030, 10, 10, 2, 3);
        Assert.True(t.GrowthPct < 0);
        Assert.True(TargetCalculator.Schedule(t)[0].BuyPrice > t.TargetPrice);
    }

    [Theory]
    [InlineData(0, 18, 2030)]
    [InlineData(10, 0, 2030)]
    [InlineData(10, 18, 2025)]
    public void ValidateRejects(double eps, double per, int year)
    {
        var t = new TargetPlan("KO", new DateOnly(2026, 10, 8), year, eps, per, 10, 3);
        Assert.Throws<ArgumentException>(() => TargetCalculator.Validate(t));
    }
}

public class DirectTargetPriceTests
{
    [Fact]
    public void DirectPriceOverridesEpsPer()
    {
        var t = new TargetPlan("KO", new DateOnly(2026, 10, 8), 2030, 0, 0, 10, 3, TargetPriceInput: 150);
        Assert.True(t.IsDirectTargetPrice);
        Assert.Equal(150, t.TargetPrice);
        TargetCalculator.Validate(t); // EPS·PER이 0이어도 통과
        var last = TargetCalculator.Schedule(t)[^1];
        Assert.Equal(TargetCalculator.BuyPrice(150, t.GrowthPct, last.BaseDate, t.TargetDate), last.BuyPrice);

        var calc = new TargetPlan("KO", new DateOnly(2026, 10, 8), 2030, 6, 25, 10, 3);
        Assert.False(calc.IsDirectTargetPrice);
        Assert.Equal(150, calc.TargetPrice);
        Assert.Equal(TargetCalculator.Schedule(calc), TargetCalculator.Schedule(t)); // 같은 목표 주가면 같은 일정
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void DirectPriceMustBePositive(double price)
    {
        var t = new TargetPlan("KO", new DateOnly(2026, 10, 8), 2030, 0, 0, 10, 3, TargetPriceInput: price);
        Assert.Throws<ArgumentException>(() => TargetCalculator.Validate(t));
    }
}

public class BuyStatusTests
{
    [Theory]
    [InlineData(70.0, 100.0, BuyStatus.StrongBuy)]  // -30%
    [InlineData(80.0, 100.0, BuyStatus.StrongBuy)]  // 정확히 -20% → 강력매수
    [InlineData(80.01, 100.0, BuyStatus.MustBuy)]
    [InlineData(90.0, 100.0, BuyStatus.MustBuy)]    // 정확히 -10% → 필수매수
    [InlineData(90.01, 100.0, BuyStatus.Buy)]
    [InlineData(100.0, 100.0, BuyStatus.Buy)]       // 같으면 매수
    [InlineData(102.0, 100.0, BuyStatus.Near)]    // 3% 이내
    [InlineData(103.0, 100.0, BuyStatus.Near)]    // 정확히 3%까지 매입 대기
    [InlineData(103.01, 100.0, BuyStatus.Wait)]   // 3% 초과
    [InlineData(105.0, 100.0, BuyStatus.Wait)]    // 이전 기준(5%)이면 매입 대기였던 값
    [InlineData(150.0, 100.0, BuyStatus.Wait)]
    public void Classify(double price, double buy, BuyStatus expected) =>
        Assert.Equal(expected, TargetCalculator.Classify(price, buy));

    [Fact]
    public void ClassifyWithoutValues()
    {
        Assert.Equal(BuyStatus.None, TargetCalculator.Classify(null, 100));
        Assert.Equal(BuyStatus.None, TargetCalculator.Classify(100, null));
        Assert.Equal(BuyStatus.None, TargetCalculator.Classify(100, 0));
    }

    [Fact]
    public void Texts()
    {
        Assert.Equal("매수 3단계", BuyStatus.StrongBuy.ToText());
        Assert.Equal("매수 2단계", BuyStatus.MustBuy.ToText());
        Assert.Equal("매수 1단계", BuyStatus.Buy.ToText());
        Assert.Equal("대기", BuyStatus.Near.ToText()); // 매입 대기는 대기에 합친다
        Assert.Equal("대기", BuyStatus.Wait.ToText());
        Assert.Equal("-", BuyStatus.None.ToText());
    }

    [Theory]
    [InlineData(BuyStatus.StrongBuy, 3, BuyStatus.Buy, "매수")]
    [InlineData(BuyStatus.MustBuy, 2, BuyStatus.Buy, "매수")]
    [InlineData(BuyStatus.Buy, 1, BuyStatus.Buy, "매수")]
    [InlineData(BuyStatus.Near, 0, BuyStatus.Wait, "대기")] // 매입 대기는 대기에 합친다
    [InlineData(BuyStatus.Wait, 0, BuyStatus.Wait, "대기")]
    [InlineData(BuyStatus.None, 0, BuyStatus.None, "-")]
    public void VerdictMergesStagesIntoBuyAndWait(BuyStatus s, int level, BuyStatus verdict, string text)
    {
        Assert.Equal(level, s.BuyLevel());
        Assert.Equal(verdict, s.Verdict());
        Assert.Equal(text, s.VerdictText());
    }

    [Fact]
    public void PriceCheckUsesSameRule()
    {
        // KO 2026-10-08: 현재가 85.82 / 매입 목표가 141.52 (-39.4%) → 강력매수, AAPL 336.67 / 245.14 → 대기
        Assert.Equal(BuyStatus.StrongBuy, new PriceCheck("KO", new DateOnly(2026, 10, 8), "2026Q4", 85.82, 141.52).Status);
        Assert.Equal(BuyStatus.MustBuy, new PriceCheck("X", new DateOnly(2026, 10, 8), "2026Q4", 85, 100).Status);
        Assert.Equal(BuyStatus.Near, new PriceCheck("X", new DateOnly(2026, 10, 8), "2026Q4", 102, 100).Status); // +2% → 매입 대기(3% 이내)
        Assert.Equal(BuyStatus.Wait, new PriceCheck("X", new DateOnly(2026, 10, 8), "2026Q4", 104, 100).Status); // +4% → 대기
        Assert.Equal("대기", new PriceCheck("AAPL", new DateOnly(2026, 10, 8), "2026Q4", 336.67, 245.14).Verdict);
    }
}

public class DividendYieldCalculatorTests
{
    [Fact]
    public void TrailingYearWindows()
    {
        // 2021-10-08 ~ 2026-10-07 매일 종가 100, 분기마다 배당 0.75 → 연 3.00
        var end = new DateOnly(2026, 10, 7);
        var bars = Enumerable.Range(0, 365 * 6).Select(i => new PriceBar(end.AddDays(-i), 100)).ToList();
        var divs = Enumerable.Range(0, 24).Select(i => new DividendEvent(end.AddDays(-15 - 91 * i), 0.75)).ToList();
        var r = DividendYieldCalculator.Calculate(new ChartData("T", "USD", "", "", 100, 100, bars, divs, []));

        Assert.Equal(5, r.Years);
        Assert.Equal(new DateOnly(2025, 10, 8), r.Periods[0].From);
        Assert.Equal(end, r.Periods[0].To);
        Assert.All(r.Periods, p => Assert.InRange(p.Count, 4, 5));
        Assert.InRange(r.AvgYieldPct, 2.9, 3.2);
    }

    [Fact]
    public void SplitsInsideWindowAreReported()
    {
        var end = new DateOnly(2026, 10, 7);
        var bars = Enumerable.Range(0, 365 * 6).Select(i => new PriceBar(end.AddDays(-i), 50)).ToList();
        var splits = new List<SplitEvent> { new(new DateOnly(2024, 6, 10), 10), new(new DateOnly(2021, 7, 20), 4) };
        var r = DividendYieldCalculator.Calculate(new ChartData("NVDA", "USD", "", "", 50, 50, bars, [], splits));

        Assert.Single(r.Splits); // 2021-07-20은 5년 구간(2021-10-08~) 밖
        Assert.Equal("10:1", r.Splits[0].RatioText);
        Assert.Equal(0, r.AvgYieldPct);
    }

    [Fact]
    public void RatioText()
    {
        Assert.Equal("4:1", new SplitEvent(default, 4).RatioText);
        Assert.Equal("1:10 (역분할)", new SplitEvent(default, 0.1).RatioText);
    }
}

public class SplitAdjusterTests
{
    private static readonly DateOnly Today = new(2026, 10, 8);

    [Fact]
    public void ForwardSplitNotYetAdjusted()
    {
        var (prev, applied) = SplitAdjuster.Adjust(121, 1200, [new SplitEvent(Today.AddDays(-1), 10)], Today);
        Assert.Equal(120, prev!.Value, 9);
        Assert.NotNull(applied);
    }

    [Fact]
    public void AlreadyAdjustedIsLeftAlone()
    {
        var (prev, applied) = SplitAdjuster.Adjust(121, 120, [new SplitEvent(Today.AddDays(-1), 10)], Today);
        Assert.Equal(120, prev);
        Assert.Null(applied);
    }

    [Fact]
    public void ReverseSplit()
    {
        var (prev, applied) = SplitAdjuster.Adjust(19.5, 2, [new SplitEvent(Today.AddDays(-2), 0.1)], Today);
        Assert.Equal(20, prev!.Value, 9);
        Assert.NotNull(applied);
    }

    [Fact]
    public void OldSplitIgnored()
    {
        var (prev, applied) = SplitAdjuster.Adjust(60, 100, [new SplitEvent(Today.AddDays(-30), 2)], Today);
        Assert.Equal(100, prev);
        Assert.Null(applied);
    }

    [Fact]
    public void NeedsCheckOnlyOnLargeMoves()
    {
        Assert.False(SplitAdjuster.NeedsCheck(101, 100));
        Assert.True(SplitAdjuster.NeedsCheck(10, 100));
        Assert.False(SplitAdjuster.NeedsCheck(10, null));
    }
}

public class YahooParseTests
{
    [Fact]
    public void ParsesPriceDividendSplit()
    {
        const string json = """
        {"chart":{"result":[{"meta":{"currency":"USD","symbol":"NVDA","exchangeName":"NMS","longName":"NVIDIA",
          "regularMarketPrice":237.47,"chartPreviousClose":240.43,"gmtoffset":-14400},
          "timestamp":[1718026200,1718112600],
          "events":{"dividends":{"1718112600":{"amount":0.01,"date":1718112600}},
                    "splits":{"1718026200":{"date":1718026200,"numerator":10.0,"denominator":1.0,"splitRatio":"10:1"}}},
          "indicators":{"quote":[{"close":[121.79,null]}]}}],"error":null}}
        """;
        var c = YahooChartClient.Parse("NVDA", json);
        Assert.Equal(237.47, c.RegularMarketPrice);
        Assert.Equal(240.43, c.PreviousClose);
        Assert.Single(c.Bars); // null 종가는 건너뜀
        Assert.Equal(new DateOnly(2024, 6, 10), c.Bars[0].Date); // 거래소 현지 날짜
        Assert.Equal(0.01, c.Dividends[0].Amount);
        Assert.Equal(10, c.Splits[0].Ratio);
    }

    [Fact]
    public void ErrorResponseThrows()
    {
        const string json = """{"chart":{"result":null,"error":{"code":"Not Found","description":"No data found, symbol may be delisted"}}}""";
        var ex = Assert.Throws<StockDataException>(() => YahooChartClient.Parse("BADXXQ", json, 404));
        Assert.Contains("티커 확인", ex.Message);
    }

    [Fact]
    public void NonJsonThrows() =>
        Assert.Throws<StockDataException>(() => YahooChartClient.Parse("X", "<html>Too Many Requests</html>", 429));
}

public class MoneyTests
{
    [Theory]
    [InlineData("1,000,000", 1_000_000)]
    [InlineData("₩1000000", 1_000_000)]
    [InlineData("100만", 1_000_000)]
    [InlineData("150만원", 1_500_000)]
    [InlineData("1.5억", 150_000_000)]
    [InlineData(" 0 ", 0)]
    public void ParseKrw(string text, double expected) => Assert.Equal(expected, Money.ParseKrw(text, "매수"));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ParseKrwBlankIsNull(string? text) => Assert.Null(Money.ParseKrw(text, "매수"));

    [Theory]
    [InlineData("abc")]
    [InlineData("-1000")]
    [InlineData("만")]
    public void ParseKrwInvalidThrows(string text) => Assert.Throws<ArgumentException>(() => Money.ParseKrw(text, "매수"));

    [Fact]
    public void KrwToUsdAndText()
    {
        Assert.Equal(1000, Money.KrwToUsd(1_350_000, 1350));
        Assert.Null(Money.KrwToUsd(1_350_000, null));
        Assert.Null(Money.KrwToUsd(null, 1350));
        Assert.Equal("₩1,350,000 ($1,000.00)", Money.Text(1_350_000, 1350));
        Assert.Equal("₩1,350,000", Money.Text(1_350_000, null)); // 환율 없으면 원화만
    }

    [Fact]
    public void AmountForStatus()
    {
        var a = new BuyAmounts(BuyKrw: 1_000_000, StrongBuyKrw: 3_000_000);
        Assert.Equal(1_000_000, a.For(BuyStatus.Buy));
        Assert.Null(a.For(BuyStatus.MustBuy)); // 비운 단계
        Assert.Equal(3_000_000, a.For(BuyStatus.StrongBuy));
        Assert.Null(a.For(BuyStatus.Near));
        Assert.Null(a.For(BuyStatus.Wait));
        Assert.True(BuyAmounts.Empty.IsEmpty);
        Assert.Same(BuyAmounts.Empty, new TargetPlan("KO", new DateOnly(2026, 10, 8), 2030, 1, 1, 10, 3).BuyAmounts);
    }

    [Fact]
    public void DefaultAmountsFillOnlyEmptyStages()
    {
        var defaults = new BuyAmounts(1_000_000, 2_000_000, 3_000_000);
        var own = new BuyAmounts(BuyKrw: 0, StrongBuyKrw: 5_000_000); // 매수 단계는 0 = 사지 않음

        Assert.Equal(new BuyAmounts(0, 2_000_000, 5_000_000), own.WithDefaults(defaults));
        Assert.Equal(defaults, BuyAmounts.Empty.WithDefaults(defaults));
        Assert.Same(own, own.WithDefaults(null));
        Assert.Same(own, own.WithDefaults(BuyAmounts.Empty));

        Assert.False(own.IsDefault(BuyStatus.Buy, defaults));      // 0으로 직접 입력
        Assert.True(own.IsDefault(BuyStatus.MustBuy, defaults));   // 비워서 기본값
        Assert.False(own.IsDefault(BuyStatus.StrongBuy, defaults));
        Assert.False(own.IsDefault(BuyStatus.MustBuy, BuyAmounts.Empty)); // 기본값도 없음

        var plan = new TargetPlan("KO", new DateOnly(2026, 10, 8), 2030, 1, 1, 10, 3, null, own);
        Assert.Equal(new BuyAmounts(0, 2_000_000, 5_000_000), plan.EffectiveAmounts(defaults));
    }

    [Fact]
    public void OnlyAmountsDifferentFromDefaultsAreKept()
    {
        var defaults = new BuyAmounts(1_000_000, 2_000_000, 3_000_000);
        // 입력칸은 기본값으로 채워지므로, 손대지 않으면 아무것도 저장하지 않는다
        Assert.True(defaults.ExceptDefaults(defaults).IsEmpty);
        // 매수는 0(사지 않음), 필수매수는 비움, 강력매수만 변경
        Assert.Equal(new BuyAmounts(0, null, 5_000_000),
            new BuyAmounts(0, null, 5_000_000).ExceptDefaults(defaults));
        Assert.Equal(new BuyAmounts(null, null, 5_000_000),
            new BuyAmounts(1_000_000, 2_000_000, 5_000_000).ExceptDefaults(defaults));
        // 기본값이 없으면 입력값을 그대로 저장
        var input = new BuyAmounts(1_000_000);
        Assert.Equal(input, input.ExceptDefaults(BuyAmounts.Empty));
        Assert.Same(input, input.ExceptDefaults(null));
        // 저장한 값 + 기본값 = 입력한 값
        var saved = new BuyAmounts(1_000_000, null, 5_000_000).ExceptDefaults(defaults);
        Assert.Equal(new BuyAmounts(1_000_000, 2_000_000, 5_000_000), saved.WithDefaults(defaults));
    }

    [Fact]
    public void SharesAreRoundedDown()
    {
        // ₩1,000,000 ÷ 1,337.18 = $747.84 → 매수단가 222.00이면 3.37주 → 3주
        Assert.Equal(3, Money.Shares(1_000_000, 1337.18, 222.00));
        // 강력매수 단가 = 222 × 0.8 = 177.60 → ₩3,000,000 = $2,243.53 → 12.63주 → 12주
        Assert.Equal(12, Money.Shares(3_000_000, 1337.18, ReservationPlanner.LimitPrice(222, BuyStatus.StrongBuy)));
        // 딱 나누어떨어지면 그 수 그대로(이진 오차로 1주 모자라지 않음)
        Assert.Equal(10, Money.Shares(1_350_000, 1350, 100));
        Assert.Equal(3, Money.Shares(0.3 * 1350, 1350, 0.1));
        // 1주도 못 사면 0, 환율 · 가격이 없으면 계산하지 않음
        Assert.Equal(0, Money.Shares(50_000, 1350, 89.86));
        Assert.Null(Money.Shares(1_000_000, null, 222));
        Assert.Null(Money.Shares(1_000_000, 1350, 0));
    }

    [Fact]
    public void DisplaySharesIsAtLeastOne()
    {
        // 1주 가격이 금액보다 큰 고가주(마켈 등)도 화면에는 1주
        Assert.Equal(1, Money.DisplayShares(1_000_000, 1350, 1_900));
        Assert.Equal(3, Money.DisplayShares(1_000_000, 1337.18, 222.00));
        Assert.Null(Money.DisplayShares(1_000_000, null, 1_900));
    }
}

public sealed class StockDatabaseTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"stocktarget_test_{Guid.NewGuid():N}.db");
    private DateTimeOffset _now = new(2026, 10, 8, 0, 0, 0, TimeSpan.Zero);
    private StockDatabase Db() => new(_path, () => _now);

    public void Dispose()
    {
        if (File.Exists(_path))
            File.Delete(_path);
    }

    [Fact]
    public void TargetUpsertAndDelete()
    {
        var db = Db();
        db.SaveTarget(new TargetPlan("KO", new DateOnly(2026, 10, 8), 2030, 10.5, 18, 10, 2.92, "메모"));
        db.SaveTarget(new TargetPlan("KO", new DateOnly(2026, 10, 9), 2031, 11, 19, 9, 2.9)); // 같은 티커 → 갱신
        var all = Db().GetTargets(); // 새 인스턴스(=앱 재실행)에서 읽기
        var ko = Assert.Single(all);
        Assert.Equal(2031, ko.TargetYear);
        Assert.Null(ko.Memo);

        db.RecordCheck(new PriceCheck("KO", new DateOnly(2026, 10, 9), "2026Q4", 85, 141));
        Assert.True(db.DeleteTarget("KO"));
        Assert.Empty(db.GetTargets());
        Assert.Empty(db.GetChecks("KO")); // 이력도 함께 삭제
        Assert.False(db.DeleteTarget("KO"));
    }

    [Fact]
    public void BuyAmountsRoundTrip()
    {
        var db = Db();
        var amounts = new BuyAmounts(1_000_000, null, 3_000_000);
        db.SaveTarget(new TargetPlan("KO", new DateOnly(2026, 10, 8), 2030, 10.5, 18, 10, 2.92, null, amounts));
        db.SaveTarget(new TargetPlan("NVDA", new DateOnly(2026, 10, 8), 2030, 10, 30, 15, 0.03));
        var all = Db().GetTargets();
        Assert.Equal(amounts, all.Single(t => t.Symbol == "KO").Amounts);
        Assert.Null(all.Single(t => t.Symbol == "NVDA").Amounts);

        db.SaveTarget(new TargetPlan("KO", new DateOnly(2026, 10, 8), 2030, 10.5, 18, 10, 2.92)); // 금액 비우고 갱신
        Assert.True(Db().GetTargets().Single(t => t.Symbol == "KO").BuyAmounts.IsEmpty);
    }

    [Fact]
    public void DefaultAmountsRoundTrip()
    {
        var db = Db();
        Assert.True(db.GetDefaultAmounts().IsEmpty); // 처음에는 설정 없음

        var defaults = new BuyAmounts(1_000_000, null, 3_500_000.5);
        db.SaveDefaultAmounts(defaults);
        Assert.Equal(defaults, Db().GetDefaultAmounts()); // 앱 재실행 후에도 유지

        db.SaveDefaultAmounts(new BuyAmounts(MustBuyKrw: 0)); // 비운 단계는 지운다
        Assert.Equal(new BuyAmounts(null, 0, null), Db().GetDefaultAmounts());
        db.SaveDefaultAmounts(BuyAmounts.Empty);
        Assert.True(Db().GetDefaultAmounts().IsEmpty);
    }

    [Fact]
    public void OldDatabaseGetsAmountColumns()
    {
        // 매수금액 컬럼이 없던 이전 버전 스키마
        using (var c = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={_path};Pooling=False"))
        {
            c.Open();
            using var cmd = c.CreateCommand();
            cmd.CommandText = """
                CREATE TABLE targets (symbol TEXT PRIMARY KEY, input_date TEXT NOT NULL, target_year INTEGER NOT NULL,
                    eps REAL NOT NULL, per REAL NOT NULL, return_pct REAL NOT NULL, dividend_yield_pct REAL NOT NULL,
                    memo TEXT, updated_at TEXT NOT NULL);
                INSERT INTO targets VALUES ('KO', '2026-10-08', 2030, 10.5, 18, 10, 2.92, NULL, '2026-10-08');
                """;
            cmd.ExecuteNonQuery();
        }

        var db = Db();
        var ko = Assert.Single(db.GetTargets());
        Assert.Null(ko.Amounts); // 기존 행은 금액 없음
        db.SaveTarget(ko with { Amounts = new BuyAmounts(2_000_000) });
        Assert.Equal(2_000_000, Db().GetTargets()[0].BuyAmounts.BuyKrw);
        Assert.False(Db().GetTargets()[0].IsDirectTargetPrice);
    }

    [Fact]
    public void DirectTargetPriceRoundTrips()
    {
        var db = Db();
        db.SaveTarget(new TargetPlan("KO", new DateOnly(2026, 10, 8), 2030, 0, 0, 10, 2.92, TargetPriceInput: 150.5));
        var ko = Assert.Single(Db().GetTargets());
        Assert.Equal(150.5, ko.TargetPriceInput);
        Assert.Equal(150.5, ko.TargetPrice);

        db.SaveTarget(ko with { Eps = 6, Per = 25, TargetPriceInput = null }); // EPS × PER로 되돌리기
        ko = Assert.Single(Db().GetTargets());
        Assert.False(ko.IsDirectTargetPrice);
        Assert.Equal(150, ko.TargetPrice);
    }

    [Fact]
    public void ChecksOnePerDay()
    {
        var db = Db();
        db.RecordCheck(new PriceCheck("KO", new DateOnly(2026, 10, 8), "2026Q4", 85, 141));
        db.RecordCheck(new PriceCheck("KO", new DateOnly(2026, 10, 8), "2026Q4", 86, 141)); // 같은 날 → 갱신
        db.RecordCheck(new PriceCheck("KO", new DateOnly(2027, 1, 5), "2027Q1", 150, 143));
        var c = db.GetChecks("KO");
        Assert.Equal(2, c.Count);
        Assert.Equal("2027Q1", c[0].Quarter); // 최신 순
        Assert.False(c[0].BuyCondition);
        Assert.Equal(86, c[1].Price);
        Assert.True(c[1].BuyCondition);
    }

    [Fact]
    public void CacheExpiresAfterOneHour()
    {
        var db = Db();
        var q = new Quote("KO", "Coca-Cola", 85.82, 86.42, "USD", "NYQ", null);
        db.PutCache("quote:KO", q);

        _now = _now.AddMinutes(59);
        var (hit, age) = db.GetCache<Quote>("quote:KO");
        Assert.Equal(q, hit);
        Assert.Equal(59 * 60, age);

        _now = _now.AddMinutes(1); // 정확히 1시간
        Assert.Null(db.GetCache<Quote>("quote:KO").Value);
    }

    [Fact]
    public void CacheTtlIsCappedAndInvalidated()
    {
        var db = Db();
        db.PutCache("quote:KO", new Quote("KO", "", 1, 1, "", "", null));
        _now = _now.AddMinutes(61);
        Assert.Null(db.GetCache<Quote>("quote:KO", ttlSeconds: 99999).Value); // 1시간 상한

        _now = _now.AddMinutes(-61);
        db.InvalidateCache("KO");
        Assert.Null(db.GetCache<Quote>("quote:KO").Value);
    }

    [Fact]
    public void DividendResultRoundTrips()
    {
        var db = Db();
        var r = new DividendYieldResult("KO",
            [new DividendPeriod(new DateOnly(2025, 10, 8), new DateOnly(2026, 10, 7), 4, 2.1, 78.16)],
            [new SplitEvent(new DateOnly(2024, 6, 10), 10)]);
        db.PutCache("dividend:KO", r);
        var back = db.GetCache<DividendYieldResult>("dividend:KO").Value!;
        Assert.Equal(r.AvgYieldPct, back.AvgYieldPct, 9);
        Assert.Equal("10:1", back.Splits[0].RatioText);
    }
}

/// <summary>실제 Yahoo 조회. STOCKTARGET_LIVE=1 일 때만 의미 있게 동작한다(아니면 즉시 통과).</summary>
public class LiveTests
{
    private static bool Live => Environment.GetEnvironmentVariable("STOCKTARGET_LIVE") == "1";

    [Fact]
    public async Task KoDividendYieldMatchesPythonVersion()
    {
        if (!Live) return;
        using var client = new YahooChartClient();
        var chart = await client.GetChartAsync("KO", "6y", withEvents: true);
        var r = DividendYieldCalculator.Calculate(chart);
        // Python(yfinance) 버전 결과(2026-10-08): 2.69/2.97/3.08/3.00/2.87 → 평균 2.92%
        Assert.Equal(5, r.Years);
        Assert.InRange(r.AvgYieldPct, 2.85, 3.0);
    }

    [Fact]
    public async Task NvdaDividendsAreSplitAdjusted()
    {
        if (!Live) return;
        using var client = new YahooChartClient();
        var chart = await client.GetChartAsync("NVDA", "6y", withEvents: true);
        // 2024-06-10 10:1 분할 이전 배당($0.04)은 0.004로 환산돼 있어야 한다
        var before = chart.Dividends.Where(d => d.Date < new DateOnly(2024, 6, 1) && d.Date > new DateOnly(2022, 1, 1)).ToList();
        Assert.NotEmpty(before);
        Assert.All(before, d => Assert.InRange(d.Amount, 0.003, 0.005));
        Assert.Contains(chart.Splits, s => s.Ratio == 10);
    }
}

public class SymbolTests
{
    [Theory]
    [InlineData(" BRKb ", "BRKb")]
    [InlineData("aapl", "aapl")]
    [InlineData("KO", "KO")]
    public void Normalize_공백만_지우고_대소문자_유지(string input, string expected) =>
        Assert.Equal(expected, StockService.Normalize(input));

    [Theory]
    [InlineData("BRKb", "BRK-B")]
    [InlineData("BFb", "BF-B")]
    [InlineData("BRK.B", "BRK-B")]
    [InlineData("BRK/B", "BRK-B")]
    [InlineData("BRK-B", "BRK-B")]
    [InlineData("aapl", "AAPL")]
    [InlineData("KO", "KO")]
    [InlineData("KRW=X", "KRW=X")]
    public void ToYahooSymbol_클래스주_표기_변환(string input, string expected) =>
        Assert.Equal(expected, StockService.ToYahooSymbol(input));
}
