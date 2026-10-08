using StockTarget.Core;

namespace StockTarget.App.ViewModels;

/// <summary>목표 목록의 한 행: 저장된 목표 + 현재가 비교.</summary>
public sealed class TargetRowViewModel(TargetPlan plan) : ObservableObject
{
    private Quote? _quote;
    private double? _cacheAge;
    private string? _error;

    public TargetPlan Plan { get; private set; } = plan;

    public string Symbol => Plan.Symbol;
    public int TargetYear => Plan.TargetYear;
    public double TargetPrice => Plan.TargetPrice;
    public double ReturnPct => Plan.ReturnPct;
    public double DividendYieldPct => Plan.DividendYieldPct;
    public double GrowthPct => Plan.GrowthPct;
    public string InputDate => Plan.InputDate.ToString("yyyy-MM-dd");

    public ScheduleRow? CurrentRow => TargetCalculator.CurrentRow(Plan, DateOnly.FromDateTime(DateTime.Today));
    public double? CurrentBuyPrice => CurrentRow?.BuyPrice;
    public string CurrentQuarter => CurrentRow?.Quarter ?? "범위 밖";

    public Quote? Quote
    {
        get => _quote;
        set
        {
            if (Set(ref _quote, value))
                RaiseQuoteDerived();
        }
    }

    public double? CacheAge
    {
        get => _cacheAge;
        set
        {
            if (Set(ref _cacheAge, value))
                OnPropertyChanged(nameof(CacheText));
        }
    }

    public string? Error
    {
        get => _error;
        set
        {
            if (Set(ref _error, value))
            {
                OnPropertyChanged(nameof(Status));
                OnPropertyChanged(nameof(Verdict));
            }
        }
    }

    public double? Price => Quote?.Price;

    /// <summary>현재가가 이번 분기 매입 목표가보다 몇 % 높은지(음수면 아래).</summary>
    public double? GapPct => Price is { } p && CurrentBuyPrice is { } b && b > 0 ? (p / b - 1) * 100 : null;

    public bool? BuyCondition => Price is { } p && CurrentBuyPrice is { } b ? p <= b : null;

    /// <summary>매수(≤ 매입 목표가) / 매입 대기(5% 이내) / 대기(5% 초과).</summary>
    public BuyStatus Status => Error is not null ? BuyStatus.None : TargetCalculator.Classify(Price, CurrentBuyPrice);

    public string Verdict => Error is not null ? "조회 실패" : Status.ToText();

    public string CacheText => CacheAge is { } a ? $"캐시 {Format.Age(a)}" : "실시간";

    public void Update(TargetPlan plan)
    {
        Plan = plan;
        OnPropertyChanged(string.Empty); // 모든 속성 갱신
    }

    private void RaiseQuoteDerived()
    {
        OnPropertyChanged(nameof(Price));
        OnPropertyChanged(nameof(GapPct));
        OnPropertyChanged(nameof(BuyCondition));
        OnPropertyChanged(nameof(Status));
        OnPropertyChanged(nameof(Verdict));
    }
}

public static class Format
{
    public static string Age(double seconds) => seconds >= 60 ? $"{(int)(seconds / 60)}분 전" : $"{(int)seconds}초 전";
}
