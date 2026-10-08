using System.Collections.ObjectModel;
using StockTarget.Core;

namespace StockTarget.Mobile.ViewModels;

[QueryProperty(nameof(TargetRow), "TargetRow")]
public sealed class MobileTargetDetailViewModel : ObservableObject
{
    private readonly StockDatabase _db;
    private readonly StockService _stockService;
    private MobileTargetRowViewModel? _targetRow;
    private bool _isBusy;
    private string _status = "";

    public ObservableCollection<ScheduleRow> Schedule { get; } = [];
    public ObservableCollection<DividendPeriod> DividendPeriods { get; } = [];
    public ObservableCollection<PriceCheck> PriceChecks { get; } = [];

    public MobileTargetRowViewModel? TargetRow
    {
        get => _targetRow;
        set
        {
            if (Set(ref _targetRow, value) && value != null)
            {
                _ = LoadDetailsAsync(value);
            }
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        set => Set(ref _isBusy, value);
    }

    public string Status
    {
        get => _status;
        set => Set(ref _status, value);
    }

    public MobileTargetDetailViewModel(StockDatabase db, StockService stockService)
    {
        _db = db;
        _stockService = stockService;
    }

    private async Task LoadDetailsAsync(MobileTargetRowViewModel row)
    {
        IsBusy = true;
        try
        {
            // 1. 분기별 매입 목표가 일정
            var schedule = TargetCalculator.Schedule(row.Plan);
            Schedule.Clear();
            foreach (var s in schedule) Schedule.Add(s);

            // 2. 과거 확인 이력
            var history = _db.GetChecks(row.Symbol);
            PriceChecks.Clear();
            foreach (var h in history) PriceChecks.Add(h);

            // 3. 5년 배당 내역 조회
            var divYield = await _stockService.GetDividendYieldAsync(row.Symbol, force: false);
            DividendPeriods.Clear();
            foreach (var p in divYield.Value.Periods) DividendPeriods.Add(p);

            Status = $"{row.Symbol} 상세 정보 로딩 완료";
        }
        catch (Exception ex)
        {
            Status = $"상세 조회 실패: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
